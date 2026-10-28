/// <summary>
/// Check portable path casing before creating generator-owned files.
/// Linux permits spellings that would alias on Windows and macOS; reject these
/// mismatches in advance without changing any of the author's files.
/// </summary>
internal static class UpdatePathSafety
{
    internal static string? FindBlockingFileAncestor(
        string relative, IEnumerable<string> existingFiles) =>
        existingFiles.Order(StringComparer.Ordinal).FirstOrDefault(file =>
            relative.StartsWith(file + "/", StringComparison.OrdinalIgnoreCase));

    internal static string? FindCaseOnlyCollision(
        string relative, IEnumerable<string> existingEntries)
    {
        var desired = relative.Split('/');
        foreach (var candidate in existingEntries.Order(StringComparer.Ordinal))
        {
            var actual = candidate.Split('/');
            var shared = Math.Min(desired.Length, actual.Length);
            for (var i = 0; i < shared; i++)
            {
                if (!string.Equals(desired[i], actual[i], StringComparison.OrdinalIgnoreCase))
                    break;
                if (!string.Equals(desired[i], actual[i], StringComparison.Ordinal))
                    return candidate; // File name or any shared parent directory differs only by case.
            }
        }
        return null;
    }
}
