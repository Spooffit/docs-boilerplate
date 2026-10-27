using System.IO;

internal static class SafeFiles
{
    // Fail closed for symlinked/reparse-point path components, not just the final file.
    // This protects normal single-writer initialization. It does not eliminate TOCTOU races
    // with a hostile process concurrently modifying the filesystem.
    public static void AssertNoLinks(string path)
    {
        var absolute = Path.GetFullPath(path);
        var root = Path.GetPathRoot(absolute)!;
        var current = root;
        foreach (var segment in absolute[root.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            try
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException($"Symbolic links and reparse points are not permitted: {current}");
            }
            catch (FileNotFoundException) { } // Not-yet-created path segment.
            catch (DirectoryNotFoundException) { }
        }
    }

    public static string Child(string root, string relative)
    {
        if (string.IsNullOrWhiteSpace(relative)
            || relative.Contains('\\')
            || relative.Contains(':')
            || relative.Split('/').Any(s => string.IsNullOrWhiteSpace(s) || s is "." or ".."))
            throw new IOException($"Unsafe relative path: {relative}");
        var fullRoot = Path.GetFullPath(root);
        var fullPath = Path.GetFullPath(Path.Combine(fullRoot, relative.Replace('/', Path.DirectorySeparatorChar)));
        var prefix = fullRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(prefix, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new IOException($"Path escapes root: {relative}");
        AssertNoLinks(fullPath);
        return fullPath;
    }

    public static IEnumerable<string> Enumerate(string root)
    {
        AssertNoLinks(root);
        if (!Directory.Exists(root))
            yield break;
        var remaining = new Stack<string>();
        remaining.Push(root);
        while (remaining.Count > 0)
        {
            var dir = remaining.Pop();
            foreach (var path in Directory.EnumerateFileSystemEntries(dir).Order(StringComparer.Ordinal))
            {
                var attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException($"Symbolic links and reparse points are not permitted: {path}");
                if ((attributes & FileAttributes.Directory) != 0)
                    remaining.Push(path);
                else
                    yield return path;
            }
        }
    }
}
