using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

internal static class DocumentMetadata
{
    private static readonly Regex FilePattern = new("^[a-z0-9]+(?:-[a-z0-9]+)*\\.md$", RegexOptions.CultureInvariant);
    private static readonly HashSet<string> Kinds = ["index", "standard", "template", "project", "guide", "reference", "adr", "audit"];
    private static readonly HashSet<string> Statuses = ["active", "draft", "template", "superseded"];

    public static void AssertFileName(string path)
    {
        var filename = Path.GetFileName(path);
        if (filename is "README.md" or "AGENTS.md" or "SKILL.md" or "CONTRIBUTING.md")
            return;
        if (!FilePattern.IsMatch(filename))
            throw new InvalidOperationException($"Expected kebab-case Markdown filename: {path}");
    }

    public static string Prepare(string path, string content)
    {
        if (!path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            return content;
        var filename = Path.GetFileName(path);
        AssertFileName(path);
        // GitHub renders root README YAML frontmatter as a table above the H1.
        // Keep the repository landing page readable without relaxing docs/ metadata.
        if (path == "README.md")
        {
            if (!content.StartsWith("# ", StringComparison.Ordinal))
                throw new InvalidOperationException("Root README.md must start with a Markdown H1, not YAML frontmatter or other text.");
            return content;
        }
        if (IsValid(content, filename == "SKILL.md"))
            return EnsureDocumentId(path, content);
        if (content.StartsWith("---\n", StringComparison.Ordinal))
            throw new InvalidOperationException($"Malformed Markdown frontmatter: {path}");
        if (filename == "SKILL.md")
            throw new InvalidOperationException($"Invalid agent skill frontmatter: {path}");
        var title = content.Split('\n').FirstOrDefault(line => line.StartsWith("# ", StringComparison.Ordinal))?[2..].Trim()
            ?? Path.GetFileNameWithoutExtension(path);
        var kind = path.Contains("/templates/", StringComparison.Ordinal) ? "template"
            : path.Contains("/standards/", StringComparison.Ordinal) ? "standard"
            : filename == "index.md" ? "index"
            : path.Contains("/project/", StringComparison.Ordinal) ? "project" : "guide";
        var status = kind == "template" ? "template" : kind == "project" ? "draft" : "active";
        return EnsureDocumentId(path, Frontmatter(title, $"Documentation: {title}", kind, status) + content);
    }

    // A new document gets a stable deterministic identifier. Preserve it on moves.
    private static string EnsureDocumentId(string path, string content)
    {
        if (!path.StartsWith("docs/", StringComparison.Ordinal))
            return content;
        if (DocumentId(content) is not null)
            return content;
        if (Regex.IsMatch(content, @"(?m)^id\s*:"))
            throw new InvalidOperationException($"Invalid document id in {path}");
        if (!content.StartsWith("---\n", StringComparison.Ordinal))
            throw new InvalidOperationException($"Missing frontmatter for document ID: {path}");
        var bytes = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(path));
        var id = "doc-" + Convert.ToHexString(bytes.AsSpan(0, 8)).ToLowerInvariant();
        return content.Insert(4, "id: " + id + "\n");
    }

    public static string? DocumentId(string content)
    {
        var normalized = content.Replace("\r\n", "\n", StringComparison.Ordinal);
        if (!normalized.StartsWith("---\n", StringComparison.Ordinal)) return null;
        var end = normalized.IndexOf("\n---\n", 4, StringComparison.Ordinal);
        if (end < 0) return null;
        var matches = Regex.Matches(normalized[4..end], @"(?m)^id:\s*(?<id>[A-Za-z][A-Za-z0-9._-]{2,127})\s*$");
        return matches.Count == 1 ? matches[0].Groups["id"].Value : null;
    }

    public static string Frontmatter(string title, string description, string kind, string status)
    {
        return "---\n" +
               "title: " + JsonSerializer.Serialize(title) + "\n" +
               "description: " + JsonSerializer.Serialize(description) + "\n" +
               "kind: " + kind + "\n" +
               "status: " + status + "\n" +
               "---\n\n";
    }

    // A deliberate, narrow policy parser for a simple YAML subset. It does not
    // claim to validate arbitrary YAML documents or multidocument streams.
    public static bool IsValid(string text, bool skill)
    {
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal);
        if (!normalized.StartsWith("---\n", StringComparison.Ordinal)) return false;
        var end = normalized.IndexOf("\n---\n", 4, StringComparison.Ordinal);
        if (end < 0) return false;
        var rows = normalized[4..end].Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var keys = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            var colon = row.IndexOf(':');
            if (colon <= 0) return false;
            var key = row[..colon].Trim();
            if (!Regex.IsMatch(key, "^[a-z][a-z0-9-]*$") || !keys.TryAdd(key, row[(colon + 1)..].Trim()))
                return false;
        }
        if (skill)
            return keys.TryGetValue("name", out var name) && !string.IsNullOrWhiteSpace(name)
                && keys.TryGetValue("description", out var description) && !string.IsNullOrWhiteSpace(description);

        if (!keys.TryGetValue("title", out var title) || string.IsNullOrWhiteSpace(title)
            || !keys.TryGetValue("description", out var summary) || string.IsNullOrWhiteSpace(summary)
            || !keys.TryGetValue("kind", out var kind) || !Kinds.Contains(kind)
            || !keys.TryGetValue("status", out var status) || !Statuses.Contains(status))
            return false;
        return true;
    }
}
