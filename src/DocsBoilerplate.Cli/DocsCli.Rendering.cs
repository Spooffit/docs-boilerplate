using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;

static partial class DocsCli
{
    private static async Task<Dictionary<string, string>> RenderAsync(
        string catalog, Catalog available, SkillCatalog skillCatalog, ModuleCatalog moduleCatalog,
        string name, string output, HashSet<string> ids,
        HashSet<string> skillIds, HashSet<string> moduleIds, string site, string language = "en")
    {
        var expected = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var full in SafeFiles.Enumerate(Path.Combine(catalog, "base")))
        {
            var relative = Path.GetRelativePath(Path.Combine(catalog, "base"), full).Replace('\\', '/');
            Add(relative, (await ReadLocalizedAsync(catalog, Path.GetRelativePath(catalog, full).Replace('\\', '/'), language))
                .Replace("{{PROJECT_NAME}}", name, StringComparison.Ordinal));
        }
        foreach (var id in ids)
        {
            var profile = available.Profiles.Single(x => x.Id == id);
            foreach (var path in profile.Files)
                Add(path, await ReadLocalizedAsync(catalog, path, language));
        }
        foreach (var id in skillIds)
        {
            var skill = skillCatalog.Skills.Single(x => x.Id == id);
            Add(skill.Destination, await ReadLocalizedAsync(catalog, skill.Source, language));
        }
        foreach (var id in moduleIds)
        {
            var module = moduleCatalog.Modules.Single(x => x.Id == id);
            foreach (var file in module.Files)
                Add(file.Destination, await ReadLocalizedAsync(catalog, file.Source, language));
        }
        if (site == "docsify")
        {
            // Docsify is a single browser-side shell, not a generated site tree.
            // Escape text for HTML separately from the JS/JSON-encoded values.
            var html = (await File.ReadAllTextAsync(SafePath(catalog, "sites/docsify.html")))
                .Replace("{{DOCUMENT_LANGUAGE}}", language, StringComparison.Ordinal)
                .Replace("{{PROJECT_NAME_HTML}}", System.Net.WebUtility.HtmlEncode(name), StringComparison.Ordinal)
                .Replace("{{SITE_LOADING_HTML}}", language == "ru" ? "Загрузка документации..." : "Loading documentation...", StringComparison.Ordinal)
                .Replace("{{SITE_SEARCH_PLACEHOLDER_JSON}}",
                    JsonSerializer.Serialize(language == "ru" ? "Поиск по документации" : "Search documentation"), StringComparison.Ordinal)
                .Replace("{{PROJECT_NAME_JSON}}", JsonSerializer.Serialize(name), StringComparison.Ordinal);
            Add("index.html", html);
        }
        // Every documentation directory has a human- and agent-readable index.
        AddDirectoryIndexes(expected, language);
        Add("docs/catalog.md", BuildDocumentCatalog(expected, ids, available, language));
        Add("llms.txt", BuildLlmsIndex(name, expected, language));
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
        string name, IEnumerable<string> ids, IEnumerable<string> skills,
        IEnumerable<string> modules, string site, Dictionary<string, string> content,
        IEnumerable<string>? retiredPaths = null, string language = "en")
    {
        var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (path, value) in content.OrderBy(x => x.Key, StringComparer.Ordinal))
            hashes.Add(path, Hash(value));
        var retired = retiredPaths?.Order(StringComparer.Ordinal).ToArray();
        return new ProjectConfig(name,
            ids.Order(StringComparer.Ordinal).ToArray(), skills.Order(StringComparer.Ordinal).ToArray(),
            modules.Order(StringComparer.Ordinal).ToArray(), site,
            content.Keys.Order(StringComparer.Ordinal).ToArray(), hashes,
            retired is { Length: > 0 } ? retired : null, language);
    }

    internal static string Hash(string content) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();

    internal static string[] DirectoryNames(IEnumerable<string> files) =>
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

    private static void AddDirectoryIndexes(Dictionary<string, string> files, string language = "en")
    {
        string DisplayName(string directory)
        {
            var name = directory.Split('/').Last().Replace('-', ' ');
            if (language != "ru") return name;
            return name switch
            {
                "architecture" => "Архитектура",
                "project" => "Проект",
                "standards" => "Стандарты",
                "templates" => "Шаблоны",
                "integrations" => "Интеграции",
                "testing" => "Тестирование",
                "operations" => "Эксплуатация",
                "infrastructure" => "Инфраструктура",
                "storage" => "Хранение",
                "security" => "Безопасность",
                "http" => "HTTP",
                "ui" => "Интерфейс",
                "dotnet" => ".NET",
                "aspnet" => "ASP.NET Core",
                "blazor" => "Blazor",
                "postgresql" => "PostgreSQL",
                _ => name
            };
        }

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

            var title = DisplayName(directory);
            var sb = new StringBuilder();
            sb.Append(DocumentMetadata.Frontmatter(language == "ru" ? $"Каталог: {title}" : $"Directory: {title}", language == "ru" ? "Указатель документов и подразделов" : "Index of documents and subdirectories", "index", "active"));
            sb.AppendLine($"# {title}");
            sb.AppendLine();
            sb.AppendLine(language == "ru" ? "Документы в этом каталоге:" : "Documents in this directory:");
            sb.AppendLine();
            foreach (var sub in subfolders)
                sb.AppendLine($"- [{DisplayName(sub)}]({sub.Split('/').Last()}/index.md)");
            foreach (var path in immediateFiles)
                sb.AppendLine($"- [{TitleFromMarkdown(files[path], path)}]({Path.GetFileName(path)})");
            files.Add(indexPath, DocumentMetadata.Prepare(indexPath, sb.ToString()));
        }
    }

    private static string TitleFromMarkdown(string text, string path)
    {
        var heading = text.Split('\n').FirstOrDefault(line => line.StartsWith("# ", StringComparison.Ordinal));
        return heading is null ? Path.GetFileNameWithoutExtension(path) : heading[2..].Trim();
    }

    internal static string BuildLlmsIndex(string projectName, Dictionary<string, string> files, string language = "en")
    {
        var sb = new StringBuilder();
        sb.AppendLine("# " + projectName.Replace('\n', ' ').Replace('\r', ' '));
        sb.AppendLine();
        sb.AppendLine(language == "ru" ? "Навигационный указатель: прочитайте AGENTS.md и соответствующий Markdown-файл, прежде чем утверждать, что решение согласовано или функция реализована." : "Navigation only: read AGENTS.md for instructions and the relevant Markdown source before claiming a project decision or implemented behavior.");
        sb.AppendLine(language == "ru" ? "Выбранные профили в docs.config.json отражают набор документации, а не утверждённые требования продукта." : "The project selection in docs.config.json identifies installed profiles, not approved product requirements.");
        sb.AppendLine();
        sb.AppendLine(language == "ru" ? "## Основное" : "## Essential");
        sb.AppendLine();
        foreach (var path in new[] { "AGENTS.md", "docs/index.md", "docs/catalog.md", "docs/project/overview.md", "docs/architecture/overview.md" })
            if (files.ContainsKey(path))
                sb.AppendLine($"- [{TitleFromMarkdown(files[path], path)}]({path})");
        sb.AppendLine();
        // Publish optional project/architecture documents only when they actually exist.
        // The main index, catalog, templates and selected standards have their own sections.
        var essential = new HashSet<string>(StringComparer.Ordinal)
        {
            "docs/index.md", "docs/catalog.md", "docs/project/overview.md", "docs/architecture/overview.md"
        };
        var projectDocs = files.Keys.Where(path => path.StartsWith("docs/", StringComparison.Ordinal)
                && path.EndsWith(".md", StringComparison.Ordinal)
                && !path.EndsWith("/index.md", StringComparison.Ordinal)
                && !path.StartsWith("docs/standards/", StringComparison.Ordinal)
                && !path.StartsWith("docs/templates/", StringComparison.Ordinal)
                && !essential.Contains(path))
            .Order(StringComparer.Ordinal).ToArray();
        if (projectDocs.Length > 0)
        {
            sb.AppendLine(language == "ru" ? "## Разделы проекта" : "## Project areas");
            sb.AppendLine();
            foreach (var path in projectDocs)
                sb.AppendLine($"- [{TitleFromMarkdown(files[path], path)}]({path})");
            sb.AppendLine();
        }
        sb.AppendLine(language == "ru" ? "## Справочник" : "## Reference");
        sb.AppendLine();
        foreach (var path in files.Keys.Where(path => path.StartsWith("docs/standards/", StringComparison.Ordinal)
                && path.EndsWith("/index.md", StringComparison.Ordinal)).Order(StringComparer.Ordinal))
            sb.AppendLine($"- [{path["docs/".Length..]}]({path})");
        if (files.ContainsKey("docs/templates/index.md"))
            sb.AppendLine(language == "ru" ? "- [Шаблоны документов](docs/templates/index.md)" : "- [Authoring templates](docs/templates/index.md)");
        return sb.ToString();
    }

    internal static string BuildDocumentCatalog(Dictionary<string, string> files, IEnumerable<string> selected, Catalog profiles, string language = "en")
    {
        var sb = new StringBuilder();
        sb.AppendLine(language == "ru" ? "# Каталог документации" : "# Documentation catalog");
        sb.AppendLine();
        sb.AppendLine(language == "ru" ? "Этот указатель создан на основе выбранных профилей и модулей. Здесь перечислены установленные документы; владелец проекта может расширить список." : "This index is generated from the selected profiles and modules. It lists the documents actually installed during initialization; project owners may extend it after initialization.");
        sb.AppendLine();
        sb.AppendLine(language == "ru" ? "## Выбранные профили" : "## Selected profiles");
        sb.AppendLine();
        var byId = profiles.Profiles.ToDictionary(p => p.Id, StringComparer.Ordinal);
        foreach (var id in selected.Order(StringComparer.Ordinal))
        {
            var name = byId[id].Name;
            if (language == "ru")
            {
                var source = byId[id].Files.FirstOrDefault(files.ContainsKey);
                if (source is not null)
                    name = TitleFromMarkdown(files[source], source);
            }
            sb.AppendLine($"- **{name}** (`{id}`)");
        }
        sb.AppendLine();
        sb.AppendLine(language == "ru" ? "## Доступные документы" : "## Available documents");
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
