using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;

return await DocsCli.RunAsync(args);

static class DocsCli
{
    private const string ConfigName = "docs.config.json";
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };

    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            var command = args.FirstOrDefault()?.ToLowerInvariant() ?? "help";
            var flags = ParseFlags(args.Skip(1).ToArray(), command);
            var catalog = Path.GetFullPath(flags.GetValueOrDefault("catalog")
                ?? Path.Combine(AppContext.BaseDirectory, "catalog"));
            if (!Directory.Exists(catalog))
                catalog = Path.GetFullPath(flags.GetValueOrDefault("catalog") ?? "catalog");
            if (!Directory.Exists(catalog))
                throw new InvalidOperationException("Catalog not found. Supply --catalog PATH or run from the repository root.");
            SafeFiles.AssertNoLinks(catalog);
            var profiles = JsonSerializer.Deserialize<Catalog>(await File.ReadAllTextAsync(Path.Combine(catalog, "profiles.json")), Json)
                ?? throw new InvalidOperationException("Invalid profile catalog.");
            var skills = JsonSerializer.Deserialize<SkillCatalog>(await File.ReadAllTextAsync(Path.Combine(catalog, "skills.json")), Json)
                ?? throw new InvalidOperationException("Invalid skill catalog.");
            var modules = JsonSerializer.Deserialize<ModuleCatalog>(await File.ReadAllTextAsync(Path.Combine(catalog, "modules.json")), Json)
                ?? throw new InvalidOperationException("Invalid documentation module catalog.");
            ValidateCatalog(catalog, profiles, skills, modules);
            switch (command)
            {
                case "list":
                    AnsiConsole.MarkupLine("[bold]Profiles[/]");
                    foreach (var profile in profiles.Profiles)
                        AnsiConsole.MarkupLine($"[{(profile.Status == "available" ? "green" : "yellow")}]{Markup.Escape(profile.Id)}[/]  {Markup.Escape(profile.Name)}  ({profile.Status})");
                    AnsiConsole.MarkupLine("[bold]Documentation modules[/]");
                    foreach (var module in modules.Modules)
                        AnsiConsole.MarkupLine($"[green]{Markup.Escape(module.Id)}[/]  {Markup.Escape(module.Name)}");
                    AnsiConsole.MarkupLine("[bold]Agent skills[/]");
                    foreach (var skill in skills.Skills)
                        AnsiConsole.MarkupLine($"[green]{Markup.Escape(skill.Id)}[/]  {Markup.Escape(skill.Name)}");
                    return 0;
                case "init":
                    return await InitializeAsync(flags, catalog, profiles, skills, modules);
                case "validate":
                    return await ValidateAsync(Path.GetFullPath(flags.GetValueOrDefault("output") ?? "."), catalog, profiles, skills, modules);
                case "update":
                    return await UpdateAsync(flags, catalog, profiles, skills, modules);
                case "doctor":
                    AnsiConsole.MarkupLine("[green]OK[/] Catalog loaded and profile paths validated.");
                    AnsiConsole.WriteLine($"Catalog: {catalog}");
                    return 0;
                default:
                    AnsiConsole.WriteLine("Commands: init, update, list, validate, doctor");
                    AnsiConsole.WriteLine("init: --name NAME --description TEXT --output PATH --profiles dotnet,ef-core,blazor,mudblazor [--skills docs-review,docs-maintain] [--modules glossary,testing] [--site docsify|none] [--dry-run] [--non-interactive] [--catalog PATH]");
                    AnsiConsole.WriteLine("validate: --output PATH [--catalog PATH]");
                    AnsiConsole.WriteLine("update: --output PATH [--apply] [--catalog PATH] (previews by default)");
                    return command == "help" ? 0 : 2;
            }
        }
        catch (Exception error)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(error.Message)}");
            return 1;
        }
    }

    private static string[] PromptProfiles(Catalog catalog)
    {
        var selected = new HashSet<string>(StringComparer.Ordinal);
        var frontendChoice = "none";

        string PickOne(string title, string category)
        {
            var choices = catalog.Profiles.Where(x => x.Category == category && x.Status == "available")
                .Select(x => x.Id).Order(StringComparer.Ordinal).ToArray();
            return AnsiConsole.Prompt(new SelectionPrompt<string>().Title(title)
                .AddChoices(new[] { "none" }.Concat(choices)));
        }

        void PickMany(string title, string category)
        {
            var choices = catalog.Profiles.Where(x => x.Category == category && x.Status == "available"
                && !(x.Id == "bunit" && frontendChoice != "blazor"))
                .Select(x => x.Id).Order(StringComparer.Ordinal).ToArray();
            if (choices.Length == 0) return;
            var picks = AnsiConsole.Prompt(new MultiSelectionPrompt<string>().Title(title).AddChoices(choices));
            selected.UnionWith(picks);
        }

        var backend = PickOne("Backend technology:", "backend");
        if (backend != "none") selected.Add(backend);
        var frontend = PickOne("Frontend technology:", "frontend");
        frontendChoice = frontend;
        if (frontend != "none") selected.Add(frontend);

        if (backend == "dotnet" || frontend == "blazor")
            PickMany("Data access technologies (optional):", "data-access");
        if (backend == "dotnet")
        {
            PickMany("HTTP API frameworks (optional):", "web");
            PickMany("Validation libraries (optional):", "validation");
            PickMany("Databases (optional):", "database");
            PickMany("Message transports / frameworks (optional):", "messaging");
            PickMany("Query composition libraries (optional):", "query-pattern");
        }
        if (frontend == "blazor")
            PickMany("Blazor UI libraries (optional):", "ui");
        PickMany("Architecture styles (optional):", "architecture");
        PickMany("Domain modeling approaches (optional):", "modeling");
        PickMany("Application patterns (optional):", "application-pattern");
        if (backend == "dotnet" || frontend == "blazor")
            PickMany("Testing libraries (optional):", "testing");

        return selected.Order(StringComparer.Ordinal).ToArray();
    }

    private static async Task<int> InitializeAsync(Dictionary<string, string> args, string catalog, Catalog available, SkillCatalog skillCatalog, ModuleCatalog moduleCatalog)
    {
        var unattended = args.ContainsKey("non-interactive");
        string Ask(string key, string prompt)
        {
            if (args.TryGetValue(key, out var supplied) && !string.IsNullOrWhiteSpace(supplied))
                return supplied;
            if (unattended || Console.IsInputRedirected)
                throw new InvalidOperationException($"--{key} is required for non-interactive initialization.");
            return AnsiConsole.Ask<string>(prompt);
        }
        var name = Ask("name", "Project name:");
        var description = Ask("description", "Short project description:");
        var output = Path.GetFullPath(Ask("output", "Destination directory:"));
        var selection = args.TryGetValue("profiles", out var listed)
            ? listed.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            : unattended || Console.IsInputRedirected
                ? throw new InvalidOperationException("--profiles is required for non-interactive initialization.")
                : PromptProfiles(available);
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description))
            throw new InvalidOperationException("Project name and description must be nonempty.");
        if (name.Contains("{{", StringComparison.Ordinal) || description.Contains("{{", StringComparison.Ordinal))
            throw new InvalidOperationException("Template placeholder markers are not permitted in input.");
        var ids = ResolveProfiles(selection, available);
        var requestedSkills = args.TryGetValue("skills", out var requested)
            ? requested.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            : unattended || Console.IsInputRedirected
                ? Array.Empty<string>()
                : AnsiConsole.Prompt(new MultiSelectionPrompt<string>().Title("Optional agent skills:")
                    .AddChoices(skillCatalog.Skills.Select(x => x.Id))).ToArray();
        var skillIds = ResolveSkills(requestedSkills, skillCatalog);
        var requestedModules = args.TryGetValue("modules", out var requestedModuleNames)
            ? requestedModuleNames.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            : unattended || Console.IsInputRedirected
                ? Array.Empty<string>()
                : AnsiConsole.Prompt(new MultiSelectionPrompt<string>().Title("Additional documentation modules:")
                    .AddChoices(moduleCatalog.Modules.Select(x => x.Id))).ToArray();
        var moduleIds = ResolveModules(requestedModules, moduleCatalog);
        var site = args.GetValueOrDefault("site") ?? (unattended || Console.IsInputRedirected
            ? "docsify"
            : AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Documentation site:")
                .AddChoices("docsify", "none")));
        if (site is not ("none" or "docsify"))
            throw new InvalidOperationException($"Unknown documentation site: {site}");
        var expected = await RenderAsync(catalog, available, skillCatalog, moduleCatalog,
            name, description, output, ids, skillIds, moduleIds, site);
        var config = BuildConfig(name, description, ids, skillIds, moduleIds, site, expected);
        expected.Add(ConfigName, JsonSerializer.Serialize(config, Json) + Environment.NewLine);
        SafeFiles.AssertNoLinks(output);
        if (Directory.Exists(output))
            _ = SafeFiles.Enumerate(output).ToArray(); // Refuse links anywhere in an existing destination.
        var existing = Directory.Exists(output) ? Directory.GetFileSystemEntries(output) : [];
        if (existing.Length > 0)
        {
            if (await IsIdenticalAsync(output, expected))
            {
                AnsiConsole.MarkupLine("[green]No changes.[/] Existing repository matches this configuration.");
                return 0;
            }
            throw new IOException($"Output directory is not empty: {output}. Refusing to overwrite any files.");
        }
        AnsiConsole.MarkupLine($"[blue]Plan:[/] {expected.Count} files for {Markup.Escape(name)}");
        foreach (var path in expected.Keys.Order(StringComparer.Ordinal))
            AnsiConsole.WriteLine($"  + {path}");
        if (args.ContainsKey("dry-run"))
            return 0;
        if (!unattended && !Console.IsInputRedirected && !AnsiConsole.Confirm("Create these files?", defaultValue: false))
            return 2;
        var created = new List<string>();
        try
        {
            foreach (var (relative, content) in expected.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                var target = SafePath(output, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                await using var stream = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                created.Add(target);
                await using var writer = new StreamWriter(stream);
                await writer.WriteAsync(content);
            }
        }
        catch
        {
            foreach (var path in created)
                if (File.Exists(path)) File.Delete(path);
            throw;
        }
        AnsiConsole.MarkupLine("[green]Generated.[/] Run validate to check the result.");
        return 0;

    }

    private static async Task<Dictionary<string, string>> RenderAsync(
        string catalog, Catalog available, SkillCatalog skillCatalog, ModuleCatalog moduleCatalog,
        string name, string description, string output, HashSet<string> ids,
        HashSet<string> skillIds, HashSet<string> moduleIds, string site)
    {
        var expected = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var full in SafeFiles.Enumerate(Path.Combine(catalog, "base")))
        {
            var relative = Path.GetRelativePath(Path.Combine(catalog, "base"), full).Replace('\\', '/');
            Add(relative, (await File.ReadAllTextAsync(full))
                .Replace("{{PROJECT_NAME}}", name, StringComparison.Ordinal)
                .Replace("{{PROJECT_DESCRIPTION}}", description, StringComparison.Ordinal));
        }
        foreach (var id in ids)
        {
            var profile = available.Profiles.Single(x => x.Id == id);
            foreach (var path in profile.Files)
                Add(path, await File.ReadAllTextAsync(SafePath(catalog, path)));
        }
        foreach (var id in skillIds)
        {
            var skill = skillCatalog.Skills.Single(x => x.Id == id);
            Add(skill.Destination, await File.ReadAllTextAsync(SafePath(catalog, skill.Source)));
        }
        foreach (var id in moduleIds)
        {
            var module = moduleCatalog.Modules.Single(x => x.Id == id);
            foreach (var file in module.Files)
                Add(file.Destination, await File.ReadAllTextAsync(SafePath(catalog, file.Source)));
        }
        if (site == "docsify")
            Add("index.html", (await File.ReadAllTextAsync(SafePath(catalog, "sites/docsify.html")))
                .Replace("{{PROJECT_NAME_JSON}}", JsonSerializer.Serialize(name), StringComparison.Ordinal));
        // Every documentation directory has a human- and agent-readable index.
        AddDirectoryIndexes(expected);
        Add("docs/catalog.md", BuildDocumentCatalog(expected, ids, available));
        Add("llms.txt", BuildLlmsIndex(name, description, expected));
        return expected;
        void Add(string path, string value)
        {
            SafePath(output, path);
            if (!expected.TryAdd(path, value))
                throw new InvalidOperationException($"Conflicting output file: {path}");
        }
    }

    private static ProjectConfig BuildConfig(
        string name, string description, IEnumerable<string> ids, IEnumerable<string> skills,
        IEnumerable<string> modules, string site, Dictionary<string, string> content)
    {
        var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (path, value) in content.OrderBy(x => x.Key, StringComparer.Ordinal))
            hashes.Add(path, Hash(value));
        return new ProjectConfig(name, description,
            ids.Order(StringComparer.Ordinal).ToArray(), skills.Order(StringComparer.Ordinal).ToArray(),
            modules.Order(StringComparer.Ordinal).ToArray(), site,
            content.Keys.Order(StringComparer.Ordinal).ToArray(), hashes);
    }

    private static string Hash(string content) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();

    private static string[] DirectoryNames(IEnumerable<string> files) =>
        files.Where(path => path.StartsWith("docs/", StringComparison.Ordinal)
            && path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetDirectoryName(path.Replace('/', Path.DirectorySeparatorChar))!
                .Replace(Path.DirectorySeparatorChar, '/'))
            .Where(folder => folder != "docs")
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static void AddDirectoryIndexes(Dictionary<string, string> files)
    {
        foreach (var directory in DirectoryNames(files.Keys))
        {
            var indexPath = directory + "/index.md";
            if (files.ContainsKey(indexPath)) continue;
            var immediateFiles = files.Keys.Where(path => path.StartsWith(directory + "/", StringComparison.Ordinal)
                && path != indexPath
                && !path[(directory.Length + 1)..].Contains('/')).Order(StringComparer.Ordinal).ToArray();
            var subfolders = DirectoryNames(files.Keys)
                .Where(path => path.StartsWith(directory + "/", StringComparison.Ordinal)
                    && !path[(directory.Length + 1)..].Contains('/')).ToArray();

            var title = directory.Split('/').Last().Replace('-', ' ');
            var sb = new StringBuilder();
            sb.Append(DocumentMetadata.Frontmatter($"Directory: {title}", "Index of documents and subdirectories", "index", "active"));
            sb.AppendLine($"# {title}");
            sb.AppendLine();
            sb.AppendLine("Documents in this directory:");
            sb.AppendLine();
            foreach (var sub in subfolders)
                sb.AppendLine($"- [{sub.Split('/').Last()}]({sub.Split('/').Last()}/index.md)");
            foreach (var path in immediateFiles)
                sb.AppendLine($"- [{TitleFromMarkdown(files[path], path)}]({Path.GetFileName(path)})");
            files.Add(indexPath, sb.ToString());
        }
    }

    private static string TitleFromMarkdown(string text, string path)
    {
        var heading = text.Split('\n').FirstOrDefault(line => line.StartsWith("# ", StringComparison.Ordinal));
        return heading is null ? Path.GetFileNameWithoutExtension(path) : heading[2..].Trim();
    }

    private static string BuildLlmsIndex(string projectName, string summary, Dictionary<string, string> files)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# " + projectName.Replace('\n', ' ').Replace('\r', ' '));
        sb.AppendLine();
        sb.AppendLine("> " + summary.Replace('\n', ' ').Replace('\r', ' '));
        sb.AppendLine();
        sb.AppendLine("Read AGENTS.md for agent instructions and docs/index.md for the project home page. These files are the authoritative Markdown sources.");
        sb.AppendLine();
        sb.AppendLine("## Essential");
        sb.AppendLine();
        foreach (var path in new[] { "AGENTS.md", "docs/index.md", "docs/catalog.md", "docs/project/overview.md", "docs/architecture/overview.md" })
            if (files.ContainsKey(path))
                sb.AppendLine($"- [{TitleFromMarkdown(files[path], path)}]({path})");
        sb.AppendLine();
        sb.AppendLine("## Reference");
        sb.AppendLine();
        foreach (var path in files.Keys.Where(path => path.StartsWith("docs/standards/", StringComparison.Ordinal)
                && path.EndsWith("/index.md", StringComparison.Ordinal)).Order(StringComparer.Ordinal))
            sb.AppendLine($"- [{path["docs/".Length..]}]({path})");
        if (files.ContainsKey("docs/templates/index.md"))
            sb.AppendLine("- [Authoring templates](docs/templates/index.md)");
        return sb.ToString();
    }

    private static string BuildDocumentCatalog(Dictionary<string, string> files, IEnumerable<string> selected, Catalog profiles)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Documentation catalog");
        sb.AppendLine();
        sb.AppendLine("This index is generated from the selected profiles and modules. It lists the documents actually installed during initialization; project owners may extend it after initialization.");
        sb.AppendLine();
        sb.AppendLine("## Selected profiles");
        sb.AppendLine();
        var byId = profiles.Profiles.ToDictionary(p => p.Id, StringComparer.Ordinal);
        foreach (var id in selected.Order(StringComparer.Ordinal))
            sb.AppendLine($"- **{byId[id].Name}** (`{id}`)");
        sb.AppendLine();
        sb.AppendLine("## Available documents");
        sb.AppendLine();
        foreach (var path in files.Keys.Where(p => p.StartsWith("docs/", StringComparison.Ordinal)
                && p.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                .Order(StringComparer.Ordinal))
        {
            var headline = files[path].Split('\n').FirstOrDefault(line => line.StartsWith("# ", StringComparison.Ordinal));
            var title = headline is null ? Path.GetFileNameWithoutExtension(path) : headline[2..].Trim();
            title = title.Replace("[", "\\[", StringComparison.Ordinal).Replace("]", "\\]", StringComparison.Ordinal);
            sb.AppendLine($"- [{title}]({path["docs/".Length..]})");
        }
        return sb.ToString();
    }

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
        if (oldConfig.Site is not ("none" or "docsify" or "zensical"))
            throw new IOException($"Unsupported site: {oldConfig.Site}");
        if (string.IsNullOrWhiteSpace(oldConfig.Name) || string.IsNullOrWhiteSpace(oldConfig.Description))
            throw new IOException("Missing project identity in configuration.");

        // Legacy Zensical projects can move to Docsify without touching author-owned Markdown.
        var site = oldConfig.Site == "zensical" ? "docsify" : oldConfig.Site;
        var expected = await RenderAsync(catalog, profiles, skills, modules,
            oldConfig.Name, oldConfig.Description, output, profileIds, skillIds, moduleIds, site);
        var newConfig = BuildConfig(oldConfig.Name, oldConfig.Description,
            profileIds, skillIds, moduleIds, site, expected);
        var newConfigContent = JsonSerializer.Serialize(newConfig, Json) + Environment.NewLine;

        var oldPaths = new HashSet<string>(oldConfig.GeneratedFiles, StringComparer.OrdinalIgnoreCase);
        var retired = oldPaths.Except(expected.Keys, StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.Ordinal).ToArray();
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

    private static async Task<int> ValidateAsync(string output, string catalog, Catalog profiles, SkillCatalog skills, ModuleCatalog modules)
    {
        SafeFiles.AssertNoLinks(output);
        // Inspect all entries first; recursion must never follow reparse points or symlinks.
        _ = SafeFiles.Enumerate(output).ToArray();
        var configPath = SafePath(output, ConfigName);
        if (!File.Exists(configPath))
            throw new IOException("Missing docs.config.json.");
        var config = JsonSerializer.Deserialize<ProjectConfig>(await File.ReadAllTextAsync(configPath), Json)
            ?? throw new IOException("Invalid docs.config.json.");
        if (config.GeneratedHashes is not null)
            AssertFingerprintManifest(config);
        var resolved = ResolveProfiles(config.Profiles, profiles);
        if (resolved.Count != config.Profiles.Length)
            throw new IOException("Invalid profile count.");
        var skillIds = ResolveSkills(config.Skills, skills);
        if (skillIds.Count != config.Skills.Length)
            throw new IOException("Duplicate skill IDs.");
        var moduleIds = ResolveModules(config.Modules, modules);
        if (moduleIds.Count != config.Modules.Length)
            throw new IOException("Duplicate documentation module IDs.");
        if (config.Site is not ("none" or "docsify" or "zensical"))
            throw new IOException($"Unknown site configuration: {config.Site}");
        var errors = new List<string>();
        var required = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in SafeFiles.Enumerate(Path.Combine(catalog, "base")))
            required.Add(Path.GetRelativePath(Path.Combine(catalog, "base"), path).Replace('\\', '/'));
        foreach (var id in resolved)
            foreach (var path in profiles.Profiles.Single(p => p.Id == id).Files)
                if (!required.Add(path)) errors.Add($"Duplicate catalog output path: {path}");
        foreach (var id in skillIds)
        {
            var skill = skills.Skills.Single(p => p.Id == id);
            if (!required.Add(skill.Destination)) errors.Add($"Duplicate skill output path: {skill.Destination}");
        }
        foreach (var id in moduleIds)
        {
            var module = modules.Modules.Single(p => p.Id == id);
            foreach (var file in module.Files)
                if (!required.Add(file.Destination))
                    errors.Add($"Duplicate module output path: {file.Destination}");
        }
        foreach (var directory in DirectoryNames(required))
            if (!required.Add($"{directory}/index.md"))
                errors.Add($"Duplicate generated directory index: {directory}/index.md");
        if (!required.Add("docs/catalog.md"))
            errors.Add("Duplicate generated documentation catalog path.");
        if (!required.Add("llms.txt"))
            errors.Add("Duplicate generated llms.txt path.");
        if (config.Site == "docsify" && !required.Add("index.html"))
            errors.Add("Duplicate site configuration path: index.html");
        if (config.Site == "zensical" && !required.Add("zensical.toml"))
            errors.Add("Duplicate legacy site configuration path: zensical.toml");
        var declared = new HashSet<string>(config.GeneratedFiles, StringComparer.OrdinalIgnoreCase);
        if (declared.Count != config.GeneratedFiles.Length || !declared.SetEquals(required))
            errors.Add("GeneratedFiles manifest does not match selected profiles, base documents and skills.");
        foreach (var path in config.GeneratedFiles)
        {
            var target = SafePath(output, path);
            if (!File.Exists(target)) errors.Add($"Missing generated file: {path}");
        }
        var allMarkdown = SafeFiles.Enumerate(output)
            .Where(x => x.EndsWith(".md", StringComparison.OrdinalIgnoreCase)).ToArray();
        foreach (var directory in DirectoryNames(allMarkdown.Select(x => Path.GetRelativePath(output, x).Replace('\\', '/'))))
            if (!File.Exists(SafePath(output, $"{directory}/index.md")))
                errors.Add($"Missing directory index: {directory}/index.md");
        foreach (var file in allMarkdown)
        {
            var text = await File.ReadAllTextAsync(file);
            var relativeName = Path.GetRelativePath(output, file).Replace('\\', '/');
            var basename = Path.GetFileName(relativeName);
            if (basename is not ("README.md" or "AGENTS.md" or "SKILL.md")
                && !System.Text.RegularExpressions.Regex.IsMatch(basename, "^[a-z0-9]+(?:-[a-z0-9]+)*\\.md$"))
                errors.Add($"Markdown filename must be kebab-case: {relativeName}");
            if (!DocumentMetadata.IsValid(text, basename == "SKILL.md"))
                errors.Add($"Invalid or missing YAML frontmatter: {relativeName}");
            if (text.Contains("{{PROJECT_", StringComparison.Ordinal))
                errors.Add($"Unresolved template field: {file}");
            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(text, @"\]\(([^)#]+)(?:#[^)]*)?\)"))
            {
                var link = match.Groups[1].Value;
                if (Uri.TryCreate(link, UriKind.Absolute, out _) || link.StartsWith('#') || link.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
                    continue;
                var decoded = Uri.UnescapeDataString(link);
                var target = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, decoded));
                var outputRoot = Path.GetFullPath(output).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (!target.StartsWith(outputRoot, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                    errors.Add($"Relative link escapes repository in {file}: {link}");
                else
                {
                    SafeFiles.AssertNoLinks(target);
                    if (!File.Exists(target))
                        errors.Add($"Broken relative link in {file}: {link}");
                }
            }
        }
        foreach (var error in errors)
            AnsiConsole.MarkupLine($"[red]ERROR[/] {Markup.Escape(error)}");
        if (errors.Count > 0) return 1;
        AnsiConsole.MarkupLine($"[green]Valid.[/] {config.GeneratedFiles.Length} generated files, {resolved.Count} profiles.");
        return 0;
    }

    private static HashSet<string> ResolveProfiles(IEnumerable<string> selected, Catalog catalog)
    {
        var byId = catalog.Profiles.ToDictionary(p => p.Id, StringComparer.Ordinal);
        var result = new HashSet<string>(StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in selected) Resolve(id);
        return result;
        void Resolve(string id)
        {
            if (!byId.TryGetValue(id, out var p))
                throw new InvalidOperationException($"Unknown profile: {id}");
            if (p.Status != "available")
                throw new InvalidOperationException($"Profile '{id}' is not implemented.");
            if (result.Contains(id)) return;
            if (!visiting.Add(id))
                throw new InvalidOperationException($"Cyclic profile dependency: {id}");
            foreach (var dependency in p.Requires) Resolve(dependency);
            visiting.Remove(id);
            result.Add(id);
        }
    }

    private static HashSet<string> ResolveSkills(IEnumerable<string> selected, SkillCatalog catalog)
    {
        var byId = catalog.Skills.ToDictionary(s => s.Id, StringComparer.Ordinal);
        var resolved = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in selected)
        {
            if (!byId.ContainsKey(id))
                throw new InvalidOperationException($"Unknown agent skill: {id}");
            if (!resolved.Add(id))
                throw new InvalidOperationException($"Duplicate agent skill: {id}");
        }
        return resolved;
    }

    private static HashSet<string> ResolveModules(IEnumerable<string> selected, ModuleCatalog catalog)
    {
        var byId = catalog.Modules.ToDictionary(m => m.Id, StringComparer.Ordinal);
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in selected)
        {
            if (!byId.ContainsKey(id))
                throw new InvalidOperationException($"Unknown documentation module: {id}");
            if (!result.Add(id))
                throw new InvalidOperationException($"Duplicate documentation module: {id}");
        }
        return result;
    }

    private static void ValidateCatalog(string catalog, Catalog data, SkillCatalog skills, ModuleCatalog modules)
    {
        if (data.SchemaVersion != 1 || skills.SchemaVersion != 1 || modules.SchemaVersion != 1)
            throw new InvalidOperationException("Unsupported catalog schema version.");
        if (modules.Modules.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1))
            throw new InvalidOperationException("Duplicate documentation module IDs.");
        foreach (var module in modules.Modules)
        {
            if (string.IsNullOrWhiteSpace(module.Id) || string.IsNullOrWhiteSpace(module.Name))
                throw new InvalidOperationException("Module identifiers and names must be nonempty.");
            foreach (var file in module.Files)
            {
                if (!file.Destination.StartsWith("docs/", StringComparison.Ordinal)
                    || !file.Destination.EndsWith(".md", StringComparison.Ordinal))
                    throw new InvalidOperationException($"Invalid module output path: {file.Destination}");
                if (!File.Exists(SafePath(catalog, file.Source)))
                    throw new IOException($"Missing documentation module source: {file.Source}");
            }
        }
        if (skills.Skills.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1))
            throw new InvalidOperationException("Duplicate skill IDs.");
        var destinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var skill in skills.Skills)
        {
            if (string.IsNullOrWhiteSpace(skill.Id) || string.IsNullOrWhiteSpace(skill.Name))
                throw new InvalidOperationException("Skill identifiers and names must be nonempty.");
            if (!destinations.Add(skill.Destination))
                throw new InvalidOperationException($"Duplicate skill destination: {skill.Destination}");
            if (!skill.Destination.StartsWith(".agents/skills/", StringComparison.Ordinal)
                || !skill.Destination.EndsWith("/SKILL.md", StringComparison.Ordinal))
                throw new InvalidOperationException($"Invalid skill destination: {skill.Destination}");
            if (!File.Exists(SafePath(catalog, skill.Source)))
                throw new IOException($"Missing agent skill source: {skill.Source}");
        }
        if (data.Profiles.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1))
            throw new InvalidOperationException("Duplicate profile IDs.");
        var categories = new HashSet<string>(StringComparer.Ordinal)
        {
            "backend", "frontend", "data-access", "ui", "architecture", "modeling", "testing",
            "web", "validation", "database", "application-pattern", "messaging", "query-pattern"
        };
        foreach (var profile in data.Profiles)
        {
            if (!categories.Contains(profile.Category))
                throw new InvalidOperationException($"Unknown profile category: {profile.Id} / {profile.Category}");
            if (profile.Status is not ("available" or "planned"))
                throw new InvalidOperationException($"Invalid profile status: {profile.Id}");
            if (profile.Status == "planned" && profile.Files.Length != 0)
                throw new InvalidOperationException($"Planned profile must not contain files: {profile.Id}");
            foreach (var path in profile.Files)
                if (!File.Exists(SafePath(catalog, path)))
                    throw new IOException($"Missing catalog file: {path}");
        }

        // Check the complete catalog, not only the profiles selected for this invocation.
        // Every available profile is composable, so two entries must never claim one destination.
        var owners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        void Own(string relative, string owner)
        {
            _ = SafePath(catalog, relative); // Validate portable relative path syntax.
            if (relative == ConfigName)
                throw new InvalidOperationException("docs.config.json is reserved for the generator.");
            if (!owners.TryAdd(relative, owner))
                throw new InvalidOperationException($"Duplicate output path '{relative}': {owners[relative]} and {owner}");
        }

        var baseDirectory = Path.Combine(catalog, "base");
        if (!Directory.Exists(baseDirectory))
            throw new DirectoryNotFoundException($"Missing catalog base: {baseDirectory}");
        foreach (var path in SafeFiles.Enumerate(baseDirectory))
        {
            var relative = Path.GetRelativePath(baseDirectory, path).Replace('\\', '/');
            if (relative.StartsWith("docs/", StringComparison.Ordinal) && relative.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                && Path.GetFileName(relative) != "index.md")
                DocumentMetadata.AssertFileName(relative);
            Own(relative, "base");
        }
        foreach (var profile in data.Profiles)
        {
            if (string.IsNullOrWhiteSpace(profile.Name)
                || !System.Text.RegularExpressions.Regex.IsMatch(profile.Id, "^[a-z][a-z0-9-]*$"))
                throw new InvalidOperationException($"Invalid profile ID or name: {profile.Id}");
            foreach (var path in profile.Files)
            {
                DocumentMetadata.AssertFileName(path);
                if (!path.StartsWith("docs/", StringComparison.Ordinal))
                    throw new InvalidOperationException($"Profile output must be under docs/: {profile.Id} / {path}");
                Own(path, $"profile {profile.Id}");
            }
        }
        foreach (var module in modules.Modules)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(module.Id, "^[a-z][a-z0-9-]*$"))
                throw new InvalidOperationException($"Invalid documentation module ID: {module.Id}");
            foreach (var file in module.Files)
            {
                DocumentMetadata.AssertFileName(file.Destination);
                Own(file.Destination, $"module {module.Id}");
            }
        }
        foreach (var skill in skills.Skills)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(skill.Id, "^[a-z][a-z0-9-]*$"))
                throw new InvalidOperationException($"Invalid skill ID: {skill.Id}");
            Own(skill.Destination, $"skill {skill.Id}");
        }
        Own("index.html", "site");
        Own("llms.txt", "agent index");

        var byId = data.Profiles.ToDictionary(x => x.Id, StringComparer.Ordinal);
        foreach (var profile in data.Profiles)
        {
            if (profile.Requires.Distinct(StringComparer.Ordinal).Count() != profile.Requires.Length)
                throw new InvalidOperationException($"Duplicate dependency in profile: {profile.Id}");
            foreach (var dependency in profile.Requires)
                if (!byId.ContainsKey(dependency))
                    throw new InvalidOperationException($"Unknown dependency '{dependency}' in profile '{profile.Id}'.");
        }
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var active = new HashSet<string>(StringComparer.Ordinal);
        void Visit(string id)
        {
            if (visited.Contains(id)) return;
            if (!active.Add(id))
                throw new InvalidOperationException($"Cyclic catalog dependency: {id}");
            foreach (var dependency in byId[id].Requires) Visit(dependency);
            active.Remove(id);
            visited.Add(id);
        }
        foreach (var profile in data.Profiles) Visit(profile.Id);

    }

    private static string SafePath(string root, string relative) => SafeFiles.Child(root, relative);

    private static Dictionary<string, string> ParseFlags(string[] args, string command)
    {
        string[] allowed = command switch
        {
            "init" => ["catalog", "name", "description", "output", "profiles", "skills", "modules", "site", "dry-run", "non-interactive"],
            "validate" => ["catalog", "output"],
            "update" => ["catalog", "output", "apply"],
            "list" or "doctor" or "help" => ["catalog"],
            _ => []
        };
        var booleans = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "dry-run", "non-interactive", "apply" };
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal) || args[i].Length <= 2)
                throw new ArgumentException($"Unexpected argument: {args[i]}");
            var key = args[i][2..];
            if (!allowed.Contains(key, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException($"Unknown option for {command}: --{key}");
            var isBoolean = booleans.Contains(key);
            if (isBoolean && i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException($"--{key} is a flag and does not accept a value.");
            if (!isBoolean && (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal)))
                throw new ArgumentException($"--{key} requires a value.");
            var value = isBoolean ? "true" : args[++i];
            if (!result.TryAdd(key, value))
                throw new ArgumentException($"Repeated option: --{key}");
        }
        return result;
    }

    private sealed record Catalog(int SchemaVersion, Profile[] Profiles);
    private sealed record Profile(string Id, string Name, string Status, string[] Requires, string[] Files, string Category);
    private sealed record SkillCatalog(int SchemaVersion, Skill[] Skills);
    private sealed record Skill(string Id, string Name, string Source, string Destination);
    private sealed record ModuleCatalog(int SchemaVersion, Module[] Modules);
    private sealed record Module(string Id, string Name, ModuleFile[] Files);
    private sealed record ModuleFile(string Source, string Destination);
    private sealed record ProjectConfig(string Name, string Description, string[] Profiles, string[] Skills, string[] Modules, string Site, string[] GeneratedFiles, Dictionary<string, string>? GeneratedHashes = null);
}
