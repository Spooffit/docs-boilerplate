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
            "infra-devops", "infra-orchestration", "infra-storage", "infra-s3-client"
        };
        return RemainingInteractiveProfiles(catalog, offered)
            .Where(id => !infrastructureCategories.Contains(catalog.Profiles.Single(p => p.Id == id).Category))
            .Where(id => catalog.Profiles.Single(p => p.Id == id).Category is not ("test-framework" or "blazor-testing" or "infra-storage-policy"))
            .Where(id =>
            {
                var dependencies = ResolveProfiles([id], catalog);
                return (frontend != "none" || !dependencies.Contains("blazor"))
                    && (backend != "none" || frontend != "none" || !dependencies.Contains("dotnet"));
            })
            .ToArray();
    }

    // MassTransit uses either RabbitMQ transport or Kafka Rider; the other
    // client choices are transport-specific catalog profiles.
    internal static bool SupportsMessagingTransport(Profile profile, string transport) =>
        profile.Category == "messaging-client" &&
        (profile.Id == "masstransit" ||
         profile.Requires.Contains(transport, StringComparer.Ordinal));

    // A deterministic decision flow is easier to validate than a mutable
    // state-machine dependency. Every step adds at most one compatible choice,
    // and ResolveProfiles applies the same guards to noninteractive calls.
    private static string[] PromptProfiles(Catalog catalog)
    {
        var selected = new HashSet<string>(StringComparer.Ordinal);
        string Label(string id) => id == "none" ? "None / skip" : catalog.Profiles.Single(p => p.Id == id).Name;
        void Header(string title) => AnsiConsole.MarkupLine($"[bold blue]{Markup.Escape(title)}[/]");
        string ChooseOne(string title, string category, bool allowNone = true,
            Func<Profile, bool>? filter = null)
        {
            var choices = catalog.Profiles
                .Where(p => p.Status == "available" && p.Category == category && (filter?.Invoke(p) ?? true))
                .Select(p => p.Id).Order(StringComparer.Ordinal).ToArray();
            if (choices.Length == 0) return "none";
            Header(title);
            var prompt = new SelectionPrompt<string>()
                .Title("Choose one option:").PageSize(12).UseConverter(Label);
            prompt.AddChoices(allowNone ? new[] { "none" }.Concat(choices) : choices);
            return AnsiConsole.Prompt(prompt);
        }
        void AddOne(string title, string category, bool allowNone = true,
            Func<Profile, bool>? filter = null)
        {
            var choice = ChooseOne(title, category, allowNone, filter);
            if (choice != "none") selected.Add(choice);
        }
        void AddMany(string title, params string[] categories)
        {
            var choices = catalog.Profiles
                .Where(p => p.Status == "available" && categories.Contains(p.Category, StringComparer.Ordinal))
                .Where(p => p.Id is not ("ddd" or "ui-accessibility" or "ui-direct-dto-forms"
                    or "http-contracts"))
                .Select(p => p.Id).Order(StringComparer.Ordinal).ToArray();
            if (choices.Length == 0) return;
            Header(title);
            var picks = AnsiConsole.Prompt(new MultiSelectionPrompt<string>()
                .Title("Use Space to toggle and Enter to continue:")
                .PageSize(12).NotRequired().UseConverter(Label).AddChoices(choices));
            selected.UnionWith(picks);
        }

        Header("Step 1/3: Backend");
        var backend = ChooseOne("Backend technology", "backend");
        if (backend != "none")
        {
            selected.Add(backend);
            AddMany("HTTP and API", "web");
            if (selected.Contains("aspnet-core"))
                AddOne("OpenAPI documentation UI", "openapi-ui");
            AddMany("Validation", "validation");
            AddOne("Database", "database");
            if (selected.Contains("postgresql"))
                AddOne("PostgreSQL connection pooler", "postgres-pooler");
            var dataAccess = ChooseOne("Data access", "data-access");
            if (dataAccess != "none")
            {
                selected.Add(dataAccess);
                if (dataAccess != "dapper" && AnsiConsole.Confirm("Use query specifications?", defaultValue: false))
                    AddOne("Specification implementation", "query-pattern", allowNone: false);
            }

            // Transport -> client -> composition. No unrelated combinations
            // can be displayed and no client can be selected without a broker.
            if (AnsiConsole.Confirm("Use a message bus?", defaultValue: false))
            {
                var transport = ChooseOne("Message broker", "messaging-transport", allowNone: false);
                selected.Add(transport);
                AddOne("Messaging client", "messaging-client", allowNone: false,
                    p => SupportsMessagingTransport(p, transport));
                AddOne("Messaging organization", "messaging-organization");
            }

            AddOne("Architecture", "architecture");
            if (AnsiConsole.Confirm("Use CQRS?", defaultValue: false))
            {
                selected.Add("cqrs");
                var wolverine = selected.Contains("wolverine-rabbitmq") || selected.Contains("wolverine-kafka");
                AddOne("CQRS implementation", "cqrs-implementation", allowNone: false,
                    p => p.Id != "cqrs-wolverine" || wolverine);
            }
            AddOne("Object mapping", "mapper", allowNone: false);
            AddOne("Tests", "test-framework");
            AddMany("Testing tools and conventions", "testing");
        }

        Header("Step 2/3: Frontend");
        var frontend = ChooseOne("Frontend technology", "frontend");
        if (frontend != "none")
        {
            selected.Add(frontend);
            AddMany("UI component libraries", "ui");
            AddOne("REST API client", "rest-client", allowNone: false,
                p => p.Requires.Contains(frontend, StringComparer.Ordinal));
            if (backend == "none")
                AddOne("Tests", "test-framework");
            if (frontend == "blazor")
                AddMany("Blazor component testing", "blazor-testing");
        }

        Header("Step 3/3: Infrastructure");
        if (AnsiConsole.Confirm("Configure infrastructure documentation?", defaultValue: false))
        {
            AddOne("Object storage", "infra-storage");
            if (selected.Contains("infra-storage-s3") && (backend != "none" || frontend == "blazor"))
                AddOne("S3 client", "infra-s3-client");
            AddOne("Logging", "infra-logs");
            AddOne("Telemetry collector", "infra-collector");
            AddOne("Metrics collection", "infra-metrics");
            AddOne("Metrics backend", "infra-metrics-backend");
            AddOne("Tracing", "infra-traces");
            AddOne("Deployment", "infra-deploy");
            AddOne("DevOps configuration location", "infra-devops",
                filter: p => backend != "none" || p.Id != "infra-devops-backend-repo");
            if (selected.Contains("dotnet") || frontend == "blazor" || backend == "dotnet")
                AddOne("Application orchestration", "infra-orchestration");
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
        var output = Path.GetFullPath(Ask("output", "Destination directory:"));
        var language = args.GetValueOrDefault("language") ?? (unattended || Console.IsInputRedirected
            ? "en"
            : AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Documentation language:")
                .AddChoices("en", "ru")));
        AssertLanguage(language);
        UpdateRecovery.AssertNoPending(output);
        var selection = args.TryGetValue("profiles", out var listed)
            ? string.Equals(listed, "none", StringComparison.OrdinalIgnoreCase)
                ? []
                : listed.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            : unattended || Console.IsInputRedirected
                ? throw new InvalidOperationException("--profiles is required for non-interactive initialization.")
                : PromptProfiles(available);
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Project name must be nonempty.");
        if (name.Contains("{{", StringComparison.Ordinal))
            throw new InvalidOperationException("Template placeholder markers are not permitted in input.");
        var ids = ResolveProfiles(selection, available);
        // Default to every persistent skill, with explicit overrides for scripts.
        var requestedSkills = args.TryGetValue("skills", out var requested)
            ? requested.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            : skillCatalog.Skills.Select(x => x.Id).ToArray();
        var skillIds = ResolveSkills(requestedSkills, skillCatalog);
        // Default to all documentation scaffolds; the agent only fills what
        // the project owner has evidenced and confirmed.
        var requestedModules = args.TryGetValue("modules", out var requestedModuleNames)
            ? requestedModuleNames.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            : moduleCatalog.Modules.Select(x => x.Id).ToArray();
        var moduleIds = ResolveModules(requestedModules, moduleCatalog);
        var site = args.GetValueOrDefault("site") ?? (unattended || Console.IsInputRedirected
            ? "docsify"
            : AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Documentation site:")
                .AddChoices("docsify", "none")));
        if (site is not ("none" or "docsify"))
            throw new InvalidOperationException($"Unknown documentation site: {site}");
        var expected = await RenderAsync(catalog, available, skillCatalog, moduleCatalog,
            name, output, ids, skillIds, moduleIds, site, language);
        // Stage-two intake documents are deliberately unowned temporary files.
        // Generate them alongside the manifest, but never include them in
        // GeneratedFiles/GeneratedHashes so agent-approved cleanup stays valid.
        var config = BuildConfig(name, ids, skillIds, moduleIds, site, expected, language: language);
        expected.Add(ConfigName, JsonSerializer.Serialize(config, Json) + Environment.NewLine);
        foreach (var (path, content) in await LoadBootstrapAsync(catalog, language))
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
                return await ValidateAsync(output, catalog, available, skillCatalog, moduleCatalog);
            }
            throw new IOException($"Output directory is not empty: {output}. Refusing to overwrite any files.");
        }
        AnsiConsole.MarkupLine($"[bold]Selected configuration for {Markup.Escape(name)}[/]");
        var requestedProfiles = new HashSet<string>(selection, StringComparer.Ordinal);
        var selectedProfiles = ids.Where(requestedProfiles.Contains)
            .Select(id => available.Profiles.Single(p => p.Id == id)).ToArray();
        foreach (var group in selectedProfiles.GroupBy(p => p.Category)
            .OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            AnsiConsole.MarkupLine($"[blue]{Markup.Escape(group.Key)}[/]");
            foreach (var profile in group.OrderBy(p => p.Name, StringComparer.Ordinal))
                AnsiConsole.MarkupLine($"  [green]✓[/] {Markup.Escape(profile.Name)}");
        }
        if (selectedProfiles.Length == 0)
            AnsiConsole.MarkupLine("  No optional technology profiles");
        var dependencies = ids.Where(id => !requestedProfiles.Contains(id))
            .Select(id => available.Profiles.Single(p => p.Id == id).Name)
            .Order(StringComparer.Ordinal).ToArray();
        if (dependencies.Length > 0)
            AnsiConsole.MarkupLine($"[blue]Included dependencies:[/] {Markup.Escape(string.Join(", ", dependencies))}");
        AnsiConsole.MarkupLine($"[blue]Documentation:[/] {skillIds.Count} agent skills and {moduleIds.Count} modules (included)");
        AnsiConsole.MarkupLine($"[blue]Language:[/] {Markup.Escape(language)}");
        AnsiConsole.MarkupLine($"[blue]Site:[/] {Markup.Escape(site)}");
        AnsiConsole.MarkupLine($"[blue]Destination:[/] {Markup.Escape(output)}");
        if (args.ContainsKey("dry-run"))
            return 0;
        if (!unattended && !Console.IsInputRedirected && !AnsiConsole.Confirm("Generate documentation with these options?", defaultValue: false))
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
        AnsiConsole.MarkupLine("[green]Generated.[/] Validating repository...");
        return await ValidateAsync(output, catalog, available, skillCatalog, moduleCatalog);

    }

    private static Dictionary<string, string> ParseFlags(string[] args, string command)
    {
        string[] allowed = command switch
        {
            "init" => ["catalog", "name", "output", "profiles", "skills", "modules", "site", "language", "dry-run", "non-interactive"],
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
