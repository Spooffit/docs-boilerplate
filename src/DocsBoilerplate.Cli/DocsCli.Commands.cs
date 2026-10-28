using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;

static partial class DocsCli
{
    internal static string[] RemainingInteractiveProfiles(Catalog catalog, IEnumerable<string> offered)
    {
        var alreadyShown = new HashSet<string>(offered, StringComparer.Ordinal);
        return catalog.Profiles
            .Where(profile => profile.Status == "available" && !alreadyShown.Contains(profile.Id))
            .Select(profile => profile.Id)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] PromptProfiles(Catalog catalog)
    {
        var selected = new HashSet<string>(StringComparer.Ordinal);
        var offered = new HashSet<string>(StringComparer.Ordinal);
        var frontendChoice = "none";

        string PickOne(string title, string category)
        {
            var choices = catalog.Profiles.Where(x => x.Category == category && x.Status == "available")
                .Select(x => x.Id).Order(StringComparer.Ordinal).ToArray();
            offered.UnionWith(choices);
            return AnsiConsole.Prompt(new SelectionPrompt<string>().Title(title)
                .AddChoices(new[] { "none" }.Concat(choices)));
        }

        void PickMany(string title, string category)
        {
            var choices = catalog.Profiles.Where(x => x.Category == category && x.Status == "available"
                && !(x.Id == "bunit" && frontendChoice != "blazor"))
                .Select(x => x.Id).Order(StringComparer.Ordinal).ToArray();
            if (choices.Length == 0) return;
            offered.UnionWith(choices);
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

        // Profiles in independent categories (e.g. documentation policy, storage,
        // integration, HTTP consumption and UI practices) must remain discoverable
        // in interactive mode too. Dependencies are resolved after selection.
        var remaining = RemainingInteractiveProfiles(catalog, offered);
        if (remaining.Length > 0)
            selected.UnionWith(AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("Additional optional profiles (dependencies resolved automatically):")
                    .AddChoices(remaining)));

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
        UpdateRecovery.AssertNoPending(output);
        var selection = args.TryGetValue("profiles", out var listed)
            ? string.Equals(listed, "none", StringComparison.OrdinalIgnoreCase)
                ? []
                : listed.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
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

    private static Dictionary<string, string> ParseFlags(string[] args, string command)
    {
        string[] allowed = command switch
        {
            "init" => ["catalog", "name", "description", "output", "profiles", "skills", "modules", "site", "dry-run", "non-interactive"],
            "validate" => ["catalog", "output"],
            "update" => ["catalog", "output", "apply"],
            "recover" => ["catalog", "output"],
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
}
