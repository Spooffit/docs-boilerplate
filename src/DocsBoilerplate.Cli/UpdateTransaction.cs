using System.Security.Cryptography;

/// <summary>
/// Apply a reviewed update with best-effort rollback after ordinary failures.
/// This is not crash-atomic or safe against concurrent hostile writers.
/// </summary>
internal static class UpdateTransaction
{
    internal sealed record Write(string Path, byte[] Content, bool Existing, string? OriginalHash);

    // The optional callback is solely for deterministic fault-injection tests.
    internal static async Task ApplyAsync(
        string output, IReadOnlyList<Write> writes, Func<int, Task>? afterWrite = null)
    {
        SafeFiles.AssertNoLinks(output);
        var completed = new List<(string Path, byte[]? Original, string WrittenHash)>();
        var createdDirectories = new HashSet<string>(
            OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        try
        {
            foreach (var write in writes)
            {
                var target = SafeFiles.Child(output, write.Path);
                byte[]? before = null;
                if (write.Existing)
                {
                    before = await File.ReadAllBytesAsync(target);
                    if (HashBytes(before) != write.OriginalHash)
                        throw new IOException($"File changed after planning: {write.Path}");
                }
                else if (File.Exists(target) || Directory.Exists(target))
                    throw new IOException($"Unmanaged file appeared after planning: {write.Path}");

                EnsureParents(output, Path.GetDirectoryName(target)!, createdDirectories);
                await AtomicWriteAsync(target, write.Content, !write.Existing);
                completed.Add((target, before, HashBytes(write.Content)));
                if (afterWrite is not null)
                    await afterWrite(completed.Count);
            }
        }
        catch (Exception error)
        {
            var rollbackErrors = new List<string>();
            foreach (var (path, original, writtenHash) in completed.AsEnumerable().Reverse())
            {
                try
                {
                    SafeFiles.AssertNoLinks(path);
                    if (!File.Exists(path) || HashBytes(await File.ReadAllBytesAsync(path)) != writtenHash)
                    {
                        rollbackErrors.Add($"{path}: changed after write; left untouched");
                        continue;
                    }
                    if (original is null) File.Delete(path);
                    else await AtomicWriteAsync(path, original, createNew: false);
                }
                catch (Exception rollbackError)
                {
                    rollbackErrors.Add($"{path}: {rollbackError.Message}");
                }
            }
            foreach (var directory in createdDirectories.OrderByDescending(x => x.Length))
            {
                try
                {
                    SafeFiles.AssertNoLinks(directory);
                    if (Directory.Exists(directory))
                        Directory.Delete(directory); // Only remove an empty directory we created.
                }
                catch (Exception rollbackError)
                {
                    rollbackErrors.Add($"{directory}: {rollbackError.Message}");
                }
            }
            throw new IOException($"Update failed: {error.Message}. " +
                (rollbackErrors.Count == 0
                    ? "Completed changes were rolled back; new empty directories were removed."
                    : "Rollback was incomplete: " + string.Join("; ", rollbackErrors)), error);
        }
    }

    private static void EnsureParents(string output, string directory, HashSet<string> created)
    {
        var current = Path.GetFullPath(output);
        var relative = Path.GetRelativePath(current, directory);
        if (relative == ".") return;
        foreach (var segment in relative.Split(Path.DirectorySeparatorChar))
        {
            current = Path.Combine(current, segment);
            SafeFiles.AssertNoLinks(current);
            if (Directory.Exists(current)) continue;
            Directory.CreateDirectory(current);
            created.Add(current);
        }
    }

    private static async Task AtomicWriteAsync(string target, byte[] content, bool createNew)
    {
        var directory = Path.GetDirectoryName(target)!;
        SafeFiles.AssertNoLinks(directory);
        var temporary = Path.Combine(directory, $".docs-boilerplate-{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllBytesAsync(temporary, content);
            File.Move(temporary, target, overwrite: !createNew);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static string HashBytes(byte[] value) =>
        Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();
}
