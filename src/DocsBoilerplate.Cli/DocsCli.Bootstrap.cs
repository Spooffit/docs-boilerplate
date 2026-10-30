using Spectre.Console;
using System.Text.Json;

static partial class DocsCli
{
    // Temporary intake instructions are created during init but intentionally
    // excluded from the ownership manifest. Stage two deletes them; ordinary
    // validate/update must still work afterwards without recreating them.
    internal static readonly string[] BootstrapPaths =
    [
        "bootstrap.md",
        ".agents/bootstrap/questionnaire.md",
        ".agents/bootstrap/finish-checklist.md",
        ".agents/skills/project-intake/SKILL.md"
    ];

    private static async Task<Dictionary<string, string>> LoadBootstrapAsync(string catalog, string language = "en")
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var path in BootstrapPaths)
        {
            var source = SafePath(catalog, "bootstrap/" + path);
            result.Add(path, await ReadLocalizedSourceAsync(catalog, "bootstrap/" + path, language));
        }
        return result;
    }

    private static async Task<int> CompleteBootstrapAsync(Dictionary<string, string> flags, string catalog)
    {
        var output = Path.GetFullPath(flags.GetValueOrDefault("output") ?? ".");
        SafeFiles.AssertNoLinks(output);
        UpdateRecovery.AssertNoPending(output);
        if (!File.Exists(SafePath(output, ConfigName)))
            throw new IOException("Not a generated documentation repository (missing docs.config.json).");

        var config = JsonSerializer.Deserialize<ProjectConfig>(
            await File.ReadAllTextAsync(SafePath(output, ConfigName)), Json)
            ?? throw new IOException("Invalid docs.config.json.");
        var baseline = await LoadBootstrapAsync(catalog, ValidateLanguage(config.Language));
        var found = new List<string>();
        foreach (var (path, expected) in baseline)
        {
            var file = SafePath(output, path);
            if (!File.Exists(file)) continue;
            if (!string.Equals(await File.ReadAllTextAsync(file), expected, StringComparison.Ordinal))
                throw new IOException($"Temporary intake file was edited: {path}. Preserve it and reconcile the change before cleanup.");
            found.Add(path);
        }
        AnsiConsole.MarkupLine("[bold]Stage-two cleanup preview[/]");
        foreach (var path in found.Order(StringComparer.Ordinal))
            AnsiConsole.WriteLine("  DELETE " + path);
        if (!flags.ContainsKey("apply"))
        {
            AnsiConsole.MarkupLine("[yellow]Preview only.[/] Request owner approval and use --apply to remove these temporary files.");
            return 0;
        }
        // All preconditions are checked first. Only listed and unchanged
        // temporary files may be deleted; permanent docs and skills are intact.
        foreach (var path in found)
            File.Delete(SafePath(output, path));
        foreach (var folder in new[]
        {
            ".agents/skills/project-intake", ".agents/bootstrap",
            ".agents/skills", ".agents"
        })
        {
            var directory = SafePath(output, folder);
            if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
                Directory.Delete(directory);
        }
        AnsiConsole.MarkupLine($"[green]Stage-two temporary files removed.[/] {found.Count} files. Keep your permanent project documentation under version control.");
        return 0;
    }
}
