using System.Security.Cryptography;
using System.Text.Json;

/// <summary>
/// A transient, on-disk update journal. It records original and desired bytes
/// before the first project file changes, so process termination is recoverable.
/// Not a database transaction or a guarantee against power loss / hostile races.
/// </summary>
internal static class UpdateRecovery
{
    internal const string DirectoryName = ".docs-boilerplate-update";
    private const string PlanName = "plan.json";
    private sealed record Entry(string Path, bool Existing, string? BeforeHash, string AfterHash);
    private sealed record Plan(int Version, Entry[] Entries, string[] CreatedDirectories);
    internal enum Outcome { RolledBack, AlreadyCompleted }

    internal static bool HasPending(string output) =>
        File.Exists(Location(output)) || Directory.Exists(Location(output));

    internal static void AssertNoPending(string output)
    {
        if (HasPending(output))
            throw new IOException(
                $"An unfinished update is present at {DirectoryName}. " +
                "Do not run update/validate until it has been inspected. " +
                "Run docs-boilerplate recover --output <repository-root>.");
    }

    private static string Location(string output) => SafeFiles.Child(output, DirectoryName);
    private static string Snapshot(string root, string side, int index) =>
        SafeFiles.Child(root, $"{side}/{index:D6}.bin");
    private static string Hash(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    internal static async Task PrepareAsync(
        string output, IReadOnlyList<UpdateTransaction.Write> writes)
    {
        AssertNoPending(output);
        if (writes.Count == 0) return;
        var root = Location(output);
        Directory.CreateDirectory(root);
        try
        {
            Directory.CreateDirectory(SafeFiles.Child(root, "before"));
            Directory.CreateDirectory(SafeFiles.Child(root, "after"));
            var entries = new List<Entry>();
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var newDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < writes.Count; i++)
            {
                var write = writes[i];
                if (!paths.Add(write.Path) || write.Path.Equals(DirectoryName, StringComparison.OrdinalIgnoreCase)
                    || write.Path.StartsWith(DirectoryName + "/", StringComparison.OrdinalIgnoreCase))
                    throw new IOException($"Invalid duplicate or reserved update path: {write.Path}");

                var target = SafeFiles.Child(output, write.Path);
                byte[]? before = null;
                if (write.Existing)
                {
                    before = await File.ReadAllBytesAsync(target);
                    if (Hash(before) != write.OriginalHash)
                        throw new IOException($"File changed after planning: {write.Path}");
                }
                else if (File.Exists(target) || Directory.Exists(target))
                    throw new IOException($"Unmanaged file appeared after planning: {write.Path}");

                if (before is not null)
                    await File.WriteAllBytesAsync(Snapshot(root, "before", i), before);
                await File.WriteAllBytesAsync(Snapshot(root, "after", i), write.Content);
                entries.Add(new Entry(write.Path, write.Existing, before is null ? null : Hash(before), Hash(write.Content)));

                var parent = Path.GetDirectoryName(target)!;
                while (!parent.Equals(Path.GetFullPath(output),
                    OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                {
                    if (!Directory.Exists(parent))
                        newDirectories.Add(Path.GetRelativePath(output, parent).Replace('\\', '/'));
                    parent = Path.GetDirectoryName(parent)!;
                }
            }
            var plan = new Plan(1, entries.ToArray(), newDirectories.Order(StringComparer.Ordinal).ToArray());
            // A fully written plan is the commit point for beginning project writes.
            var path = SafeFiles.Child(root, PlanName);
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(plan));
        }
        catch
        {
            // Project files are unchanged until the complete plan exists.
            SafeFiles.AssertNoLinks(root);
            Directory.Delete(root, recursive: true);
            throw;
        }
    }

    internal static async Task ApplyStagedAsync(
        string output, int index, string relative, string expectedHash, bool existing)
    {
        var source = Snapshot(Location(output), "after", index);
        var bytes = await File.ReadAllBytesAsync(source);
        if (Hash(bytes) != expectedHash)
            throw new IOException($"Prepared update payload was changed: {relative}");
        var target = SafeFiles.Child(output, relative);
        File.Move(source, target, overwrite: existing);
    }

    internal static async Task<Outcome> RecoverAsync(string output, bool forceRollback = false)
    {
        var root = Location(output);
        if (!Directory.Exists(root))
            throw new IOException("No interrupted update journal exists.");
        SafeFiles.AssertNoLinks(root);
        var planPath = SafeFiles.Child(root, PlanName);
        if (!File.Exists(planPath))
            throw new IOException(
                "Update staging was interrupted before the journal was committed. " +
                $"No project files were written by that transaction. Inspect {DirectoryName} " +
                "and remove this staging folder manually; do not discard unknown files.");

        var plan = JsonSerializer.Deserialize<Plan>(await File.ReadAllTextAsync(planPath))
            ?? throw new IOException("Invalid update recovery journal.");
        if (plan.Version != 1 || plan.Entries is null || plan.Entries.Length == 0
            || plan.CreatedDirectories is null)
            throw new IOException("Unsupported or invalid update recovery journal.");

        var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var states = new List<(string Path, string? CurrentHash, Entry Entry)>();
        var allAfter = true;
        for (var i = 0; i < plan.Entries.Length; i++)
        {
            var entry = plan.Entries[i];
            if (entry.Path is null || !unique.Add(entry.Path)
                || entry.Path.Equals(DirectoryName, StringComparison.OrdinalIgnoreCase)
                || entry.Path.StartsWith(DirectoryName + "/", StringComparison.OrdinalIgnoreCase)
                || !ValidHash(entry.AfterHash)
                || (entry.Existing && !ValidHash(entry.BeforeHash))
                || (!entry.Existing && entry.BeforeHash is not null))
                throw new IOException("Invalid update recovery journal entry.");
            var target = SafeFiles.Child(output, entry.Path);
            var currentHash = File.Exists(target) ? Hash(await File.ReadAllBytesAsync(target)) : null;
            if (currentHash != entry.AfterHash) allAfter = false;
            if (currentHash != entry.AfterHash && currentHash != entry.BeforeHash
                && !(currentHash is null && !entry.Existing))
                throw new IOException($"Recovery blocked: file was independently changed or removed: {entry.Path}. " +
                    "The journal was retained and no files were modified.");

            states.Add((target, currentHash, entry));
        }
        // Verify recorded directories are genuine parents of recorded update targets.
        var possibleParents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in plan.Entries)
        {
            var parent = entry.Path.LastIndexOf('/');
            while (parent > 0)
            {
                var directory = entry.Path[..parent];
                possibleParents.Add(directory);
                parent = directory.LastIndexOf('/');
            }
        }
        var seenDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var directory in plan.CreatedDirectories)
            if (directory is null || !possibleParents.Contains(directory) || !seenDirectories.Add(directory))
                throw new IOException("Invalid created-directory entry in recovery journal.");

        // Verify the entire journal and all current target states before any restoration.
        // The case where all files match desired content is a committed update whose
        // cleanup was interrupted. It is safer to finalize than undo that update.
        if (allAfter && !forceRollback)
        {
            Cleanup(root, plan);
            return Outcome.AlreadyCompleted;
        }

        // Earlier entries may have had their backup reads deferred while allAfter
        // was still plausible. Re-read all needed snapshots before touching targets.
        var originals = new Dictionary<int, byte[]>();
        for (var i = 0; i < states.Count; i++)
        {
            if (!states[i].Entry.Existing || states[i].CurrentHash != states[i].Entry.AfterHash) continue;
            var snapshot = await File.ReadAllBytesAsync(Snapshot(root, "before", i));
            if (Hash(snapshot) != states[i].Entry.BeforeHash)
                throw new IOException($"Recovery blocked: original snapshot is invalid: {states[i].Entry.Path}");
            originals.Add(i, snapshot);
        }
        var failures = new List<string>();
        for (var i = states.Count - 1; i >= 0; i--)
        {
            var (path, current, entry) = states[i];
            if (current != entry.AfterHash) continue;
            try
            {
                SafeFiles.AssertNoLinks(path);
                if (!File.Exists(path) || Hash(await File.ReadAllBytesAsync(path)) != entry.AfterHash)
                    throw new IOException("changed again during recovery");
                if (entry.Existing)
                    await AtomicReplaceAsync(path, originals[i]);
                else
                    File.Delete(path);
            }
            catch (Exception e) { failures.Add($"{entry.Path}: {e.Message}"); }
        }
        foreach (var dir in plan.CreatedDirectories.OrderByDescending(x => x.Length))
        {
            try
            {
                var full = SafeFiles.Child(output, dir);
                if (Directory.Exists(full)) Directory.Delete(full); // Only empty dirs.
            }
            catch (Exception e) { failures.Add($"{dir}: {e.Message}"); }
        }
        if (failures.Count != 0)
            throw new IOException("Rollback was incomplete: " + string.Join("; ", failures) +
                $". Journal retained at {DirectoryName}.");
        Cleanup(root, plan);
        return Outcome.RolledBack;
    }

    private static async Task AtomicReplaceAsync(string target, byte[] content)
    {
        var staged = Path.Combine(Path.GetDirectoryName(target)!,
            $".docs-boilerplate-{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllBytesAsync(staged, content);
            File.Move(staged, target, overwrite: true);
        }
        finally
        {
            if (File.Exists(staged)) File.Delete(staged);
        }
    }

    internal static void Complete(string output)
    {
        var root = Location(output);
        var planPath = SafeFiles.Child(root, PlanName);
        if (!File.Exists(planPath))
            throw new IOException($"Completed update is missing its cleanup journal at {DirectoryName}.");
        var plan = JsonSerializer.Deserialize<Plan>(File.ReadAllText(planPath))
            ?? throw new IOException("Invalid cleanup journal.");
        Cleanup(root, plan);
    }

    private static void Cleanup(string root, Plan plan)
    {
        SafeFiles.AssertNoLinks(root);
        for (var i = 0; i < plan.Entries.Length; i++)
        {
            var after = Snapshot(root, "after", i);
            if (File.Exists(after)) File.Delete(after);
            var before = Snapshot(root, "before", i);
            if (File.Exists(before)) File.Delete(before);
        }
        var afterDir = SafeFiles.Child(root, "after");
        var beforeDir = SafeFiles.Child(root, "before");
        if (Directory.Exists(afterDir)) Directory.Delete(afterDir);
        if (Directory.Exists(beforeDir)) Directory.Delete(beforeDir);
        File.Delete(SafeFiles.Child(root, PlanName)); // Plan is removed last.
        Directory.Delete(root);
    }

    private static bool ValidHash(string? hash) => hash is { Length: 64 }
        && hash.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
}
