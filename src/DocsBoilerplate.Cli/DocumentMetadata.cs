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
        if (filename is "README.md" or "AGENTS.md" or "SKILL.md")
            return;
        if (!FilePattern.IsMatch(filename))
            throw new InvalidOperationException($"Expected kebab-case Markdown filename: {path}");
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
