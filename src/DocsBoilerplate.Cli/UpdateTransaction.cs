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
        UpdateRecovery.AssertNoPending(output);
        if (writes.Count == 0) return;

        // Persist verified snapshots and desired bytes before the first project write.
        await UpdateRecovery.PrepareAsync(output, writes);
        try
        {
            for (var i = 0; i < writes.Count; i++)
            {
                var write = writes[i];
                var target = SafeFiles.Child(output, write.Path);
                if (write.Existing)
                {
                    var before = await File.ReadAllBytesAsync(target);
                    if (HashBytes(before) != write.OriginalHash)
                        throw new IOException($"File changed after planning: {write.Path}");
                }
                else if (File.Exists(target) || Directory.Exists(target))
                    throw new IOException($"Unmanaged file appeared after planning: {write.Path}");

                EnsureParents(output, Path.GetDirectoryName(target)!);
                await UpdateRecovery.ApplyStagedAsync(
                    output, i, write.Path, HashBytes(write.Content), write.Existing);
                if (afterWrite is not null)
                    await afterWrite(i + 1);
            }
        }
        catch (Exception error)
        {
            try
            {
                await UpdateRecovery.RecoverAsync(output, forceRollback: true);
                throw new IOException($"Update failed: {error.Message}. " +
                    "Completed changes were rolled back; new empty directories were removed.", error);
            }
            catch (IOException recoveryError) when (recoveryError.InnerException != error)
            {
                throw new IOException($"Update failed: {error.Message}. " +
                    $"Rollback was incomplete: {recoveryError.Message}", error);
            }
        }
        // If cleanup itself is interrupted, the journal remains available to
        // finalize an already-completed update with 'recover'.
        UpdateRecovery.Complete(output);
    }

    private static void EnsureParents(string output, string directory)
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
        }
    }

    private static string HashBytes(byte[] value) =>
        Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();
}
