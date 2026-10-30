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

    internal static string[] RemainingContextualProfiles(Catalog catalog, IEnumerable<string> offered, string backend, string frontend)
    {
        var infrastructureCategories = new HashSet<string>(StringComparer.Ordinal)
        {
            "infra-logs", "infra-metrics", "infra-traces", "infra-deploy",
            "infra-devops", "infra-orchestration"
        };
        return RemainingInteractiveProfiles(catalog, offered)
            .Where(id => !infrastructureCategories.Contains(catalog.Profiles.Single(p => p.Id == id).Category))
            .Where(id =>
            {
                var dependencies = ResolveProfiles([id], catalog);
                return (frontend != "none" || !dependencies.Contains("blazor"))
                    && (backend != "none" || frontend != "none" || !dependencies.Contains("dotnet"));
            })
            .ToArray();
    }

    private static string[] PromptProfiles(Catalog catalog)
    {
        var selected = new HashSet<string>(StringComparer.Ordinal);
        var offered = new HashSet<string>(StringComparer.Ordinal);
        string Label(string id) => id == "none" ? "Skip (none)" : catalog.Profiles.Single(p => p.Id == id).Name;

        string ChooseOne(string title, string category, bool backendAvailable = true)
        {
            var choices = catalog.Profiles.Where(p => p.Status == "available" && p.Category == category
                && (backendAvailable || p.Id != "infra-devops-backend-repo"))
                .Select(p => p.Id).Order(StringComparer.Ordinal).ToArray();
            offered.UnionWith(choices);
            if (choices.Length == 0) return "none";
            return AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title(title).PageSize(12).UseConverter(Label).AddChoices(new[] { "none" }.Concat(choices)));
        }

        void ChooseMany(string title, params string[] categories)
        {
            var choices = catalog.Profiles.Where(p => p.Status == "available"
                && categories.Contains(p.Category, StringComparer.Ordinal)
                && p.Id != "bunit")
                .Select(p => p.Id).Order(StringComparer.Ordinal).ToArray();
            offered.UnionWith(choices);
            if (choices.Length == 0) return;
            var picks = AnsiConsole.Prompt(new MultiSelectionPrompt<string>()
                .Title(title).PageSize(12).NotRequired().UseConverter(Label).AddChoices(choices));
            selected.UnionWith(picks);
        }

        AnsiConsole.MarkupLine("[bold]Step 1/3: Backend[/] (optional)");
        var backend = ChooseOne("Backend technology:", "backend");
        if (backend != "none")
        {
            selected.Add(backend);
            ChooseMany("Backend libraries and options:", "web", "validation", "database",
                "data-access", "messaging", "query-pattern", "architecture",
                "modeling", "application-pattern", "testing");
        }

        AnsiConsole.MarkupLine("[bold]Step 2/3: Frontend[/] (optional)");
        var frontend = ChooseOne("Frontend technology:", "frontend");
        if (frontend != "none")
        {
            selected.Add(frontend);
            ChooseMany("Frontend libraries and options:", "ui", "frontend-practice");
            if (frontend == "blazor")
            {
                offered.Add("bunit");
                if (AnsiConsole.Confirm("Include bUnit component testing guidance?", false))
                    selected.Add("bunit");
            }
        }

        // Keep independent standards selectable without quietly adding them.
        // Infrastructure choices come at the end, even for frontend-only projects.
        var remaining = RemainingContextualProfiles(catalog, offered, backend, frontend);
        if (remaining.Length > 0)
            selected.UnionWith(AnsiConsole.Prompt(new MultiSelectionPrompt<string>()
                .Title("Other optional standards (independent selection):")
                .PageSize(15).NotRequired().UseConverter(Label).AddChoices(remaining)));

        AnsiConsole.MarkupLine("[bold]Step 3/3: Infrastructure[/] (optional)");
        if (AnsiConsole.Confirm("Configure infrastructure documentation?", defaultValue: false))
        {
            foreach (var (category, title) in new[]
            {
                ("infra-logs", "Logging technology:"),
                ("infra-metrics", "Metrics technology:"),
                ("infra-traces", "Tracing technology:"),
                ("infra-deploy", "Deployment approach:"),
                ("infra-devops", "DevOps definitions location:")
            })
            {
                var chosen = ChooseOne(title, category, backendAvailable: backend != "none");
                if (chosen != "none") selected.Add(chosen);
            }
            if (selected.Contains("dotnet") || frontend == "blazor" || backend == "dotnet")
            {
                var chosen = ChooseOne("Application orchestration:", "infra-orchestration");
                if (chosen != "none") selected.Add(chosen);
            }
        }

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
        // Stage-two intake documents are deliberately unowned temporary files.
        // Generate them alongside the manifest, but never include them in
        // GeneratedFiles/GeneratedHashes so agent-approved cleanup stays valid.
        var config = BuildConfig(name, description, ids, skillIds, moduleIds, site, expected);
        expected.Add(ConfigName, JsonSerializer.Serialize(config, Json) + Environment.NewLine);
        foreach (var (path, content) in await LoadBootstrapAsync(catalog))
            if (!expected.TryAdd(path, content))
                throw new InvalidOperationException($"Temporary bootstrap path conflicts with generated output: {path}");
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
            "bootstrap-complete" => ["catalog", "output", "apply"],
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
