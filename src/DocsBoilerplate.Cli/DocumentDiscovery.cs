/// <summary>
/// Read-only discovery checks for generated project documentation.
/// Missing backlinks matter: a valid Markdown file can still be invisible to agents.
/// This is not a general Markdown renderer and never rewrites owner-edited indexes.
/// </summary>
internal static class DocumentDiscovery
{
    internal static IReadOnlyList<string> FindMissingLinks(
        IReadOnlyDictionary<string, string> docs, IReadOnlySet<string> generatorOwned,
        IReadOnlySet<string>? retainedUnmanaged = null)
    {
        var errors = new List<string>();
        if (!docs.TryGetValue("docs/catalog.md", out var catalog))
            return errors; // The generated-file manifest check reports a missing catalog.

        var catalogLinks = LinkedPaths("docs/catalog.md", catalog);
        foreach (var path in docs.Keys.Where(p => p.StartsWith("docs/", StringComparison.Ordinal)
            && p.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            && p != "docs/catalog.md").Order(StringComparer.Ordinal))
        {
            // An upstream-retired document may remain intentionally on disk
            // without being advertised by the current selected profile.
            if (retainedUnmanaged?.Contains(path) == true) continue;
            if (!catalogLinks.Contains(path))
                errors.Add($"Not discoverable from docs/catalog.md: {path}");

            // An authored page must be linked from its immediate directory index.
            // For an authored nested index, require a link from the parent index
            // so the directory tree itself remains navigable. Generated/installed
            // indexes are exempt: curated template indexes need not enumerate
            // every selected technology document.
            if (generatorOwned.Contains(path)) continue;

            var directory = path[..path.LastIndexOf('/')];
            var indexPath = path.EndsWith("/index.md", StringComparison.OrdinalIgnoreCase)
                ? directory[..directory.LastIndexOf('/')] + "/index.md"
                : directory + "/index.md";
            if (docs.TryGetValue(indexPath, out var index)
                && !LinkedPaths(indexPath, index).Contains(path))
                errors.Add($"Not discoverable from {indexPath}: {path}");
            // An entirely missing index is already reported by the validator.
        }
        return errors;
    }

    internal static HashSet<string> LinkedPaths(string source, string markdown)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var parent = source.Split('/')[..^1];
        foreach (var raw in MarkdownNavigation.InlineTargets(markdown))
        {
            if (Uri.TryCreate(raw, UriKind.Absolute, out _))
                continue;
            var (target, _) = MarkdownNavigation.SplitLocalTarget(raw);
            if (target.Length == 0 || target.StartsWith('/') || target.Contains('\\')
                || target.Contains(':'))
                continue;
            var parts = new List<string>(parent);
            var insideRoot = true;
            foreach (var component in target.Split('/'))
            {
                if (component is "" or ".") continue;
                if (component == "..")
                {
                    if (parts.Count == 0) { insideRoot = false; break; }
                    parts.RemoveAt(parts.Count - 1);
                }
                else parts.Add(component);
            }
            if (insideRoot)
                paths.Add(string.Join("/", parts));
        }
        return paths;
    }
}
