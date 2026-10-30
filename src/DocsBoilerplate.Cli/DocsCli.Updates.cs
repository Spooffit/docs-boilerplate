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
        UpdateRecovery.AssertNoPending(output);
        if (!Directory.Exists(output))
            throw new DirectoryNotFoundException($"Output directory does not exist: {output}");
        var existingEntries = SafeFiles.EnumerateEntries(output)
            .Select(path => Path.GetRelativePath(output, path).Replace('\\', '/'))
            .ToArray();
        var existingFiles = existingEntries
            .Where(relative => File.Exists(Path.Combine(output, relative.Replace('/', Path.DirectorySeparatorChar))))
            .ToArray();
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
        var language = ValidateLanguage(oldConfig.Language);
        if (oldConfig.Site is not ("none" or "docsify"))
            throw new IOException($"Unsupported site: {oldConfig.Site}");
        if (string.IsNullOrWhiteSpace(oldConfig.Name) || string.IsNullOrWhiteSpace(oldConfig.Description))
            throw new IOException("Missing project identity in configuration.");

        var expected = await RenderAsync(catalog, profiles, skills, modules,
            oldConfig.Name, oldConfig.Description, output, profileIds, skillIds, moduleIds, oldConfig.Site, language);
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
            profileIds, skillIds, moduleIds, oldConfig.Site, expected, retained, language);
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
                var caseCollision = UpdatePathSafety.FindCaseOnlyCollision(relative, existingEntries);
                if (caseCollision is not null)
                    conflicts.Add($"Case-only path collision: {relative} conflicts with {caseCollision}. " +
                        "Rename the existing path (including parent directory casing) before retrying.");
                else if (UpdatePathSafety.FindBlockingFileAncestor(relative, existingFiles) is { } blocked)
                    conflicts.Add($"File blocks required directory: {blocked} prevents {relative}. " +
                        "Rename the existing file before retrying.");
                else if (File.Exists(target) || Directory.Exists(target))
                    conflicts.Add($"Unmanaged file collision: {relative}. Rename or reconcile the existing file before retrying.");
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
            AnsiConsole.WriteLine(
                "Resolve the listed paths in Git against the selected catalog, then preview again. " +
                "Do not manually change ownership hashes to bypass a conflict.");
            return 1;
        }
        if (!flags.ContainsKey("apply"))
        {
            AnsiConsole.MarkupLine("[yellow]Preview only.[/] Use --apply to make these changes.");
            return 0;
        }

        // The manifest was appended last. Rollback restores prior bytes and
        // removes additions without deleting unrelated user-owned files.
        await UpdateTransaction.ApplyAsync(output, writes.Select(write =>
            new UpdateTransaction.Write(write.Path, Encoding.UTF8.GetBytes(write.Content),
                write.Existing, write.OriginalHash)).ToArray());

        AnsiConsole.MarkupLine($"[green]Updated.[/] {writes.Count} files written. User edits were preserved.");
        return 0;
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
