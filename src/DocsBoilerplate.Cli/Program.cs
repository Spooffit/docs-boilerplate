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
            ValidateCatalog(catalog, profiles);
            switch (command)
            {
                case "list":
                    foreach (var profile in profiles.Profiles)
                        AnsiConsole.MarkupLine($"[{(profile.Status == "available" ? "green" : "yellow")}]{Markup.Escape(profile.Id)}[/]  {Markup.Escape(profile.Name)}  ({profile.Status})");
                    return 0;
                case "init":
                    return await InitializeAsync(flags, catalog, profiles);
                case "validate":
                    return await ValidateAsync(Path.GetFullPath(flags.GetValueOrDefault("output") ?? "."), catalog, profiles);
                case "doctor":
                    AnsiConsole.MarkupLine("[green]OK[/] Catalog loaded and profile paths validated.");
                    AnsiConsole.WriteLine($"Catalog: {catalog}");
                    return 0;
                default:
                    AnsiConsole.WriteLine("Commands: init, list, validate, doctor");
                    AnsiConsole.WriteLine("init: --name NAME --description TEXT --output PATH --profiles dotnet,ef-core,blazor,mudblazor [--dry-run] [--non-interactive] [--catalog PATH]");
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

    private static async Task<int> InitializeAsync(Dictionary<string, string> args, string catalog, Catalog available)
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
                : AnsiConsole.Prompt(new MultiSelectionPrompt<string>().Title("Technology and architecture profiles:")
                    .AddChoices(available.Profiles.Where(x => x.Status == "available").Select(x => x.Id))).ToArray();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description))
            throw new InvalidOperationException("Project name and description must be nonempty.");
        if (name.Contains("{{", StringComparison.Ordinal) || description.Contains("{{", StringComparison.Ordinal))
            throw new InvalidOperationException("Template placeholder markers are not permitted in input.");
        var ids = ResolveProfiles(selection, available);
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
        var config = new ProjectConfig(name, description, ids.Order(StringComparer.Ordinal).ToArray(), expected.Keys.Order(StringComparer.Ordinal).ToArray());
        Add(ConfigName, JsonSerializer.Serialize(config, Json) + Environment.NewLine);
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
        var files = Directory.GetFiles(output, "*", SearchOption.AllDirectories)
            .Select(p => Path.GetRelativePath(output, p).Replace('\\', '/')).Order(StringComparer.Ordinal).ToArray();
        if (!files.SequenceEqual(expected.Keys.Order(StringComparer.Ordinal), StringComparer.Ordinal))
            return false;
        foreach (var (path, content) in expected)
            if (await File.ReadAllTextAsync(SafePath(output, path)) != content)
                return false;
        return true;
    }

    private static async Task<int> ValidateAsync(string output, string catalog, Catalog profiles)
    {
        var configPath = Path.Combine(output, ConfigName);
        if (!File.Exists(configPath))
            throw new IOException("Missing docs.config.json.");
        var config = JsonSerializer.Deserialize<ProjectConfig>(await File.ReadAllTextAsync(configPath), Json)
            ?? throw new IOException("Invalid docs.config.json.");
        var resolved = ResolveProfiles(config.Profiles, profiles);
        if (resolved.Count != config.Profiles.Length)
            throw new IOException("Invalid profile count.");
        var errors = new List<string>();
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
                if (!File.Exists(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, decoded))))
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

    private static void ValidateCatalog(string catalog, Catalog data)
    {
        if (data.SchemaVersion != 1)
            throw new InvalidOperationException("Unsupported catalog schema version.");
        if (data.Profiles.GroupBy(x => x.Id, StringComparer.Ordinal).Any(x => x.Count() > 1))
            throw new InvalidOperationException("Duplicate profile IDs.");
        foreach (var profile in data.Profiles)
        {
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
    private sealed record Profile(string Id, string Name, string Status, string[] Requires, string[] Files);
    private sealed record ProjectConfig(string Name, string Description, string[] Profiles, string[] GeneratedFiles);
}
