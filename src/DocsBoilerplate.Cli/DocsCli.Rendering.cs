using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;

static partial class DocsCli
{
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
            var prepared = DocumentMetadata.Prepare(path, value);
            if (!expected.TryAdd(path, prepared))
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
            .SelectMany(path =>
            {
                var dirs = new List<string>();
                var directory = Path.GetDirectoryName(path.Replace('/', Path.DirectorySeparatorChar));
                while (directory is not null)
                {
                    var portable = directory.Replace(Path.DirectorySeparatorChar, '/');
                    if (portable == "docs" || !portable.StartsWith("docs/", StringComparison.Ordinal))
                        break;
                    dirs.Add(portable);
                    directory = Path.GetDirectoryName(directory);
                }
                return dirs;
            })
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
}
