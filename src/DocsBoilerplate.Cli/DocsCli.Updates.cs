using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;

static partial class DocsCli
{
    private sealed record PlannedWrite(string Path, string Content, bool Existing, string? OriginalHash);

    private static void AssertFingerprintManifest(ProjectConfig config)
    {
        if (config.GeneratedHashes is null)
            throw new InvalidOperationException(
                "This documentation repository predates content fingerprints. Safe update is unavailable. " +
                "Preserve the existing project and initialize a separate repository for manual migration.");
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in config.GeneratedFiles)
        {
            if (!paths.Add(path))
                throw new InvalidOperationException($"Duplicate owned file in manifest: {path}");
        }
        var hashes = new Dictionary<string, string>(config.GeneratedHashes, StringComparer.OrdinalIgnoreCase);
        if (hashes.Count != config.GeneratedHashes.Count || hashes.Count != paths.Count
            || !paths.SetEquals(hashes.Keys))
            throw new InvalidOperationException("The ownership manifest and fingerprints do not match.");
        foreach (var (path, sha) in hashes)
        {
            if (path == ConfigName || sha.Length != 64 || sha.Any(c => c is not
                (>= '0' and <= '9' or >= 'a' and <= 'f')))
                throw new InvalidOperationException($"Invalid fingerprint: {path}");
        }
    }

    private static string HashBytes(byte[] bytes) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();

    private static async Task<int> UpdateAsync(
        Dictionary<string, string> flags, string catalog, Catalog profiles,
        SkillCatalog skills, ModuleCatalog modules)
    {
        var output = Path.GetFullPath(flags.GetValueOrDefault("output") ?? ".");
        SafeFiles.AssertNoLinks(output);
        if (!Directory.Exists(output))
            throw new DirectoryNotFoundException($"Output directory does not exist: {output}");
        _ = SafeFiles.Enumerate(output).ToArray();
        var configPath = SafePath(output, ConfigName);
        if (!File.Exists(configPath))
            throw new IOException($"Missing {ConfigName}. Refusing to update an unmanaged directory.");

        var oldConfigBytes = await File.ReadAllBytesAsync(configPath);
        var oldConfig = JsonSerializer.Deserialize<ProjectConfig>(oldConfigBytes, Json)
            ?? throw new IOException("Invalid docs.config.json.");
        AssertFingerprintManifest(oldConfig);
        var profileIds = ResolveProfiles(oldConfig.Profiles, profiles);
        if (profileIds.Count != oldConfig.Profiles.Length)
            throw new IOException("Duplicate profiles in configuration.");
        var skillIds = ResolveSkills(oldConfig.Skills, skills);
        var moduleIds = ResolveModules(oldConfig.Modules, modules);
        if (oldConfig.Site is not ("none" or "docsify"))
            throw new IOException($"Unsupported site: {oldConfig.Site}");
        if (string.IsNullOrWhiteSpace(oldConfig.Name) || string.IsNullOrWhiteSpace(oldConfig.Description))
            throw new IOException("Missing project identity in configuration.");

        var expected = await RenderAsync(catalog, profiles, skills, modules,
            oldConfig.Name, oldConfig.Description, output, profileIds, skillIds, moduleIds, oldConfig.Site);
        var oldPaths = new HashSet<string>(oldConfig.GeneratedFiles, StringComparer.OrdinalIgnoreCase);
        var retired = oldPaths.Except(expected.Keys, StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.Ordinal).ToArray();
        var priorRetired = oldConfig.RetiredFiles ?? [];
        var uniquePriorRetired = new HashSet<string>(priorRetired, StringComparer.OrdinalIgnoreCase);
        if (uniquePriorRetired.Count != priorRetired.Length || uniquePriorRetired.Overlaps(oldPaths))
            throw new IOException("Invalid or overlapping RetiredFiles manifest entries.");
        foreach (var path in priorRetired) _ = SafePath(output, path);
        // Retired sources are kept on disk without remaining part of generated navigation.
        // Carry their identity across subsequent updates; do not silently re-adopt a path.
        var retained = uniquePriorRetired.Union(retired, StringComparer.OrdinalIgnoreCase)
            .Except(expected.Keys, StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.Ordinal).ToArray();
        var newConfig = BuildConfig(oldConfig.Name, oldConfig.Description,
            profileIds, skillIds, moduleIds, oldConfig.Site, expected, retained);
        var newConfigContent = JsonSerializer.Serialize(newConfig, Json) + Environment.NewLine;
        var writes = new List<PlannedWrite>();
        var protectedFiles = new List<string>();
        var conflicts = new List<string>();

        foreach (var (relative, desired) in expected.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            var target = SafePath(output, relative);
            var newHash = Hash(desired);
            if (!oldPaths.Contains(relative))
            {
                if (File.Exists(target) || Directory.Exists(target))
                    conflicts.Add($"Unmanaged file collision: {relative}");
                else
                    writes.Add(new PlannedWrite(relative, desired, false, null));
                continue;
            }

            if (!File.Exists(target))
            {
                conflicts.Add($"Missing previously generated file: {relative}");
                continue;
            }
            var currentHash = HashBytes(await File.ReadAllBytesAsync(target));
            var baselineHash = oldConfig.GeneratedHashes![relative];
            if (currentHash == newHash)
                continue; // Already has the latest generated content.
            if (baselineHash == newHash)
            {
                protectedFiles.Add(relative); // Local edit, but the source template is unchanged.
                continue;
            }
            if (currentHash != baselineHash)
                conflicts.Add($"Both the user and the template changed: {relative}");
            else
                writes.Add(new PlannedWrite(relative, desired, true, currentHash));
        }

        var configHash = HashBytes(oldConfigBytes);
        if (Encoding.UTF8.GetString(oldConfigBytes) != newConfigContent)
            writes.Add(new PlannedWrite(ConfigName, newConfigContent, true, configHash));

        AnsiConsole.MarkupLine($"[bold]Update preview[/] for {Markup.Escape(output)}");
        foreach (var write in writes)
            AnsiConsole.WriteLine($"{(write.Existing ? "UPDATE" : "ADD   ")} {write.Path}");
        foreach (var file in protectedFiles)
            AnsiConsole.WriteLine($"KEEP   {file} (user edit; unchanged upstream)");
        foreach (var file in retired)
            AnsiConsole.WriteLine($"RETAIN {file} (no longer in source catalog; never auto-delete)");
        foreach (var conflict in conflicts)
            AnsiConsole.MarkupLine($"[red]CONFLICT[/] {Markup.Escape(conflict)}");
        if (conflicts.Count > 0)
        {
            AnsiConsole.MarkupLine("[red]Update blocked.[/] No files were changed.");
            return 1;
        }
        if (!flags.ContainsKey("apply"))
        {
            AnsiConsole.MarkupLine("[yellow]Preview only.[/] Use --apply to make these changes.");
            return 0;
        }

        var completed = new List<(string Path, byte[]? Original)>();
        var rollbackErrors = new List<string>();
        try
        {
            foreach (var write in writes)
            {
                var target = SafePath(output, write.Path);
                byte[]? before = null;
                if (write.Existing)
                {
                    before = await File.ReadAllBytesAsync(target);
                    if (HashBytes(before) != write.OriginalHash)
                        throw new IOException($"File changed after planning: {write.Path}");
                }
                else if (File.Exists(target) || Directory.Exists(target))
                    throw new IOException($"Unmanaged file appeared after planning: {write.Path}");

                await AtomicWriteAsync(target, Encoding.UTF8.GetBytes(write.Content), !write.Existing);
                completed.Add((target, before));
            }
        }
        catch (Exception error)
        {
            foreach (var (path, original) in completed.AsEnumerable().Reverse())
            {
                try
                {
                    SafeFiles.AssertNoLinks(path);
                    if (original is null) File.Delete(path);
                    else await AtomicWriteAsync(path, original, false);
                }
                catch (Exception rollbackError)
                {
                    rollbackErrors.Add($"{path}: {rollbackError.Message}");
                }
            }
            throw new IOException($"Update failed: {error.Message}. " +
                (rollbackErrors.Count == 0 ? "Completed changes were rolled back." :
                    "Rollback was incomplete: " + string.Join("; ", rollbackErrors)), error);
        }

        AnsiConsole.MarkupLine($"[green]Updated.[/] {writes.Count} files written. User edits were preserved.");
        return 0;
    }

    private static async Task AtomicWriteAsync(string target, byte[] content, bool createNew)
    {
        var directory = Path.GetDirectoryName(target)!;
        SafeFiles.AssertNoLinks(directory);
        Directory.CreateDirectory(directory);
        SafeFiles.AssertNoLinks(target);
        var temporary = Path.Combine(directory, $".docs-boilerplate-{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllBytesAsync(temporary, content);
            File.Move(temporary, target, overwrite: !createNew);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static async Task<bool> IsIdenticalAsync(string output, Dictionary<string, string> expected)
    {
        // User-authored additional files and site build outputs must not turn a safe no-op into a conflict.
        // Every owned/generated file still has to match byte-for-byte.
        foreach (var (path, content) in expected)
        {
            var file = SafePath(output, path);
            if (!File.Exists(file) || await File.ReadAllTextAsync(file) != content)
                return false;
        }
        return true;
    }
}
