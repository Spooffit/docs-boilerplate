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
            var flags = ParseFlags(args.Skip(1).ToArray());
            var catalog = Path.GetFullPath(flags.GetValueOrDefault("catalog")
                ?? Path.Combine(AppContext.BaseDirectory, "catalog"));
            if (!Directory.Exists(catalog))
                catalog = Path.GetFullPath(flags.GetValueOrDefault("catalog") ?? "catalog");
            if (!Directory.Exists(catalog))
                throw new InvalidOperationException("Catalog not found. Supply --catalog PATH or run from the repository root.");
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
                case "doctor":
                    AnsiConsole.MarkupLine("[green]OK[/] Catalog loaded and profile paths validated.");
                    AnsiConsole.WriteLine($"Catalog: {catalog}");
                    return 0;
                default:
                    AnsiConsole.WriteLine("Commands: init, list, validate, doctor");
                    AnsiConsole.WriteLine("init: --name NAME --description TEXT --output PATH --profiles dotnet,ef-core,blazor,mudblazor [--skills docs-review,docs-maintain] [--modules glossary,testing] [--site zensical] [--dry-run] [--non-interactive] [--catalog PATH]");
                    AnsiConsole.WriteLine("validate: --output PATH [--catalog PATH]");
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

        string PickOne(string title, string category)
        {
            var choices = catalog.Profiles.Where(x => x.Category == category && x.Status == "available")
                .Select(x => x.Id).Order(StringComparer.Ordinal).ToArray();
            return AnsiConsole.Prompt(new SelectionPrompt<string>().Title(title)
                .AddChoices(new[] { "none" }.Concat(choices)));
        }

        void PickMany(string title, string category)
        {
            var choices = catalog.Profiles.Where(x => x.Category == category && x.Status == "available")
                .Select(x => x.Id).Order(StringComparer.Ordinal).ToArray();
            if (choices.Length == 0) return;
            var picks = AnsiConsole.Prompt(new MultiSelectionPrompt<string>().Title(title).AddChoices(choices));
            selected.UnionWith(picks);
        }

        var backend = PickOne("Backend technology:", "backend");
        if (backend != "none") selected.Add(backend);
        var frontend = PickOne("Frontend technology:", "frontend");
        if (frontend != "none") selected.Add(frontend);

        if (backend == "dotnet" || frontend == "blazor")
            PickMany("Data access technologies (optional):", "data-access");
        if (frontend == "blazor")
            PickMany("Blazor UI libraries (optional):", "ui");
        PickMany("Architecture styles (optional):", "architecture");
        PickMany("Domain modeling approaches (optional):", "modeling");
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
            ? "none"
            : AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Documentation site:")
                .AddChoices("none", "zensical")));
        if (site is not ("none" or "zensical"))
            throw new InvalidOperationException($"Unknown documentation site: {site}");
        var expected = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var full in Directory.GetFiles(Path.Combine(catalog, "base"), "*", SearchOption.AllDirectories))
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
                Add(path, await File.ReadAllTextAsync(Path.Combine(catalog, path.Replace('/', Path.DirectorySeparatorChar))));
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
        if (site == "zensical")
            Add("zensical.toml", (await File.ReadAllTextAsync(SafePath(catalog, "sites/zensical.toml")))
                .Replace("{{PROJECT_NAME_TOML}}", JsonSerializer.Serialize(name), StringComparison.Ordinal));
        var config = new ProjectConfig(name, description, ids.Order(StringComparer.Ordinal).ToArray(),
            skillIds.Order(StringComparer.Ordinal).ToArray(), moduleIds.Order(StringComparer.Ordinal).ToArray(),
            site, expected.Keys.Order(StringComparer.Ordinal).ToArray());
        Add(ConfigName, JsonSerializer.Serialize(config, Json) + Environment.NewLine);
        if (Directory.Exists(output) && new DirectoryInfo(output).Attributes.HasFlag(FileAttributes.ReparsePoint))
            throw new IOException("The destination must not be a symbolic link.");
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
        void Add(string path, string value)
        {
            SafePath(output, path);
            if (!expected.TryAdd(path, value))
                throw new InvalidOperationException($"Conflicting output file: {path}");
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
        var configPath = Path.Combine(output, ConfigName);
        if (!File.Exists(configPath))
            throw new IOException("Missing docs.config.json.");
        var config = JsonSerializer.Deserialize<ProjectConfig>(await File.ReadAllTextAsync(configPath), Json)
            ?? throw new IOException("Invalid docs.config.json.");
        var resolved = ResolveProfiles(config.Profiles, profiles);
        if (resolved.Count != config.Profiles.Length)
            throw new IOException("Invalid profile count.");
        var skillIds = ResolveSkills(config.Skills, skills);
        if (skillIds.Count != config.Skills.Length)
            throw new IOException("Duplicate skill IDs.");
        var moduleIds = ResolveModules(config.Modules, modules);
        if (moduleIds.Count != config.Modules.Length)
            throw new IOException("Duplicate documentation module IDs.");
        if (config.Site is not ("none" or "zensical"))
            throw new IOException($"Unknown site configuration: {config.Site}");
        var errors = new List<string>();
        var required = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in Directory.GetFiles(Path.Combine(catalog, "base"), "*", SearchOption.AllDirectories))
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
        if (config.Site == "zensical")
            if (!required.Add("zensical.toml"))
                errors.Add("Duplicate site configuration path: zensical.toml");
        var declared = new HashSet<string>(config.GeneratedFiles, StringComparer.OrdinalIgnoreCase);
        if (declared.Count != config.GeneratedFiles.Length || !declared.SetEquals(required))
            errors.Add("GeneratedFiles manifest does not match selected profiles, base documents and skills.");
        foreach (var path in config.GeneratedFiles)
        {
            var target = SafePath(output, path);
            if (!File.Exists(target)) errors.Add($"Missing generated file: {path}");
        }
        foreach (var file in Directory.GetFiles(output, "*.md", SearchOption.AllDirectories))
        {
            var text = await File.ReadAllTextAsync(file);
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
                else if (!File.Exists(target))
                    errors.Add($"Broken relative link in {file}: {link}");
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
        if (modules.Modules.GroupBy(x => x.Id, StringComparer.Ordinal).Any(x => x.Count() > 1))
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
        if (skills.Skills.GroupBy(x => x.Id, StringComparer.Ordinal).Any(x => x.Count() > 1))
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
        if (data.Profiles.GroupBy(x => x.Id, StringComparer.Ordinal).Any(x => x.Count() > 1))
            throw new InvalidOperationException("Duplicate profile IDs.");
        var categories = new HashSet<string>(StringComparer.Ordinal)
        {
            "backend", "frontend", "data-access", "ui", "architecture", "modeling", "testing"
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
    }

    private static string SafePath(string root, string relative)
    {
        if (Path.IsPathRooted(relative) || relative.Split('/', '\\').Any(x => x is "." or ".."))
            throw new IOException($"Unsafe relative path: {relative}");
        var path = Path.GetFullPath(Path.Combine(root, relative));
        var prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new IOException($"Path escapes output directory: {relative}");
        return path;
    }

    private static Dictionary<string, string> ParseFlags(string[] args)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal)) throw new ArgumentException($"Unknown argument: {args[i]}");
            var key = args[i][2..];
            if (!result.TryAdd(key, i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal) ? args[++i] : "true"))
                throw new ArgumentException($"Repeated option: {key}");
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
    private sealed record ProjectConfig(string Name, string Description, string[] Profiles, string[] Skills, string[] Modules, string Site, string[] GeneratedFiles);
}
