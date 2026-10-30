using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;

return await DocsCli.RunAsync(args);

static partial class DocsCli
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
                case "bootstrap-complete":
                    return await CompleteBootstrapAsync(flags, catalog);
                case "recover":
                    var output = Path.GetFullPath(flags.GetValueOrDefault("output") ?? ".");
                    var outcome = await UpdateRecovery.RecoverAsync(output);
                    AnsiConsole.MarkupLine(outcome == UpdateRecovery.Outcome.AlreadyCompleted
                        ? "[green]Recovered.[/] Completed update finalized; recovery journal removed."
                        : "[green]Recovered.[/] Interrupted update rolled back; original project files restored.");
                    return 0;
                case "doctor":
                    AnsiConsole.MarkupLine("[green]OK[/] Catalog loaded and profile paths validated.");
                    AnsiConsole.WriteLine($"Catalog: {catalog}");
                    return 0;
                default:
                    AnsiConsole.WriteLine("Commands: init, update, recover, bootstrap-complete, list, validate, doctor");
                    AnsiConsole.WriteLine("init: --name NAME --output PATH --profiles dotnet,ef-core,blazor,mudblazor [--skills docs-review,docs-maintain] [--modules glossary,testing] [--site docsify|none] [--language en|ru] [--dry-run] [--non-interactive] [--catalog PATH]");
                    AnsiConsole.WriteLine("validate: --output PATH [--catalog PATH]");
                    AnsiConsole.WriteLine("update: --output PATH [--apply] [--catalog PATH] (previews by default)");
                    AnsiConsole.WriteLine("recover: --output PATH [--catalog PATH] (inspect and recover an interrupted update)");
                    AnsiConsole.WriteLine("bootstrap-complete: --output PATH [--apply] (preview/delete temporary stage-two intake files)");
                    return command == "help" ? 0 : 2;
            }
        }
        catch (Exception error)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(error.Message)}");
            return 1;
        }
    }
}
