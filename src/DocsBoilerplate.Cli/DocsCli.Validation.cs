using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;

static partial class DocsCli
{
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
        if (config.Site is not ("none" or "docsify"))
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
            if (basename is not ("README.md" or "AGENTS.md" or "SKILL.md" or "CONTRIBUTING.md")
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
        var llmsPath = SafePath(output, "llms.txt");
        if (File.Exists(llmsPath))
        {
            var llms = await File.ReadAllTextAsync(llmsPath);
            if (!llms.StartsWith("# ", StringComparison.Ordinal)
                || !llms.Contains("\n## ", StringComparison.Ordinal))
                errors.Add("llms.txt must contain a project H1 and curated H2 link sections.");
            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(
                llms, @"\[[^\]\r\n]+\]\((?<target>[^)\r\n]+)\)"))
            {
                var targetText = match.Groups["target"].Value;
                if (Uri.TryCreate(targetText, UriKind.Absolute, out var absoluteUri))
                {
                    if (absoluteUri.Scheme is not ("http" or "https"))
                        errors.Add($"Unsupported llms.txt link scheme: {targetText}");
                    continue;
                }
                try
                {
                    var target = SafePath(output, Uri.UnescapeDataString(targetText));
                    if (!File.Exists(target))
                        errors.Add($"Broken llms.txt link: {targetText}");
                }
                catch (IOException)
                {
                    errors.Add($"Invalid llms.txt link: {targetText}");
                }
            }
        }

        foreach (var error in errors)
            AnsiConsole.MarkupLine($"[red]ERROR[/] {Markup.Escape(error)}");
        if (errors.Count > 0) return 1;
        AnsiConsole.MarkupLine($"[green]Valid.[/] {config.GeneratedFiles.Length} generated files, {resolved.Count} profiles.");
        return 0;
    }
}
