using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;

static partial class DocsCli
{
    internal static HashSet<string> ResolveProfiles(IEnumerable<string> selected, Catalog catalog)
    {
        var byId = catalog.Profiles.ToDictionary(p => p.Id, StringComparer.Ordinal);
        var result = new HashSet<string>(StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in selected) Resolve(id);
        return result;
        void Resolve(string id)
        {
            if (!byId.TryGetValue(id, out var p))
                throw new InvalidOperationException($"Unknown profile: {id}");
            if (p.Status != "available")
                throw new InvalidOperationException($"Profile '{id}' is not implemented.");
            if (result.Contains(id)) return;
            if (!visiting.Add(id))
                throw new InvalidOperationException($"Cyclic profile dependency: {id}");
            foreach (var dependency in p.Requires) Resolve(dependency);
            visiting.Remove(id);
            result.Add(id);
        }
    }

    internal static HashSet<string> ResolveSkills(IEnumerable<string> selected, SkillCatalog catalog)
    {
        var byId = catalog.Skills.ToDictionary(s => s.Id, StringComparer.Ordinal);
        var resolved = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in selected)
        {
            if (!byId.ContainsKey(id))
                throw new InvalidOperationException($"Unknown agent skill: {id}");
            if (!resolved.Add(id))
                throw new InvalidOperationException($"Duplicate agent skill: {id}");
        }
        return resolved;
    }

    internal static HashSet<string> ResolveModules(IEnumerable<string> selected, ModuleCatalog catalog)
    {
        var byId = catalog.Modules.ToDictionary(m => m.Id, StringComparer.Ordinal);
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in selected)
        {
            if (!byId.ContainsKey(id))
                throw new InvalidOperationException($"Unknown documentation module: {id}");
            if (!result.Add(id))
                throw new InvalidOperationException($"Duplicate documentation module: {id}");
        }
        return result;
    }

    private static void ValidateCatalog(string catalog, Catalog data, SkillCatalog skills, ModuleCatalog modules)
    {
        if (data.SchemaVersion != 1 || skills.SchemaVersion != 1 || modules.SchemaVersion != 1)
            throw new InvalidOperationException("Unsupported catalog schema version.");
        if (modules.Modules.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1))
            throw new InvalidOperationException("Duplicate documentation module IDs.");
        foreach (var module in modules.Modules)
        {
            if (string.IsNullOrWhiteSpace(module.Id) || string.IsNullOrWhiteSpace(module.Name))
                throw new InvalidOperationException("Module identifiers and names must be nonempty.");
            foreach (var file in module.Files)
            {
                if (!file.Destination.StartsWith("docs/", StringComparison.Ordinal)
                    || !file.Destination.EndsWith(".md", StringComparison.Ordinal))
                    throw new InvalidOperationException($"Invalid module output path: {file.Destination}");
                if (!File.Exists(SafePath(catalog, file.Source)))
                    throw new IOException($"Missing documentation module source: {file.Source}");
            }
        }
        if (skills.Skills.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1))
            throw new InvalidOperationException("Duplicate skill IDs.");
        var destinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var skill in skills.Skills)
        {
            if (string.IsNullOrWhiteSpace(skill.Id) || string.IsNullOrWhiteSpace(skill.Name))
                throw new InvalidOperationException("Skill identifiers and names must be nonempty.");
            if (!destinations.Add(skill.Destination))
                throw new InvalidOperationException($"Duplicate skill destination: {skill.Destination}");
            if (!skill.Destination.StartsWith(".agents/skills/", StringComparison.Ordinal)
                || !skill.Destination.EndsWith("/SKILL.md", StringComparison.Ordinal))
                throw new InvalidOperationException($"Invalid skill destination: {skill.Destination}");
            if (!File.Exists(SafePath(catalog, skill.Source)))
                throw new IOException($"Missing agent skill source: {skill.Source}");
        }
        if (data.Profiles.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1))
            throw new InvalidOperationException("Duplicate profile IDs.");
        var categories = new HashSet<string>(StringComparer.Ordinal)
        {
            "backend", "frontend", "data-access", "ui", "architecture", "modeling", "testing",
            "web", "validation", "database", "application-pattern", "messaging", "query-pattern", "frontend-practice"
        };
        foreach (var profile in data.Profiles)
        {
            if (!categories.Contains(profile.Category))
                throw new InvalidOperationException($"Unknown profile category: {profile.Id} / {profile.Category}");
            if (profile.Status is not ("available" or "planned"))
                throw new InvalidOperationException($"Invalid profile status: {profile.Id}");
            if (profile.Status == "planned" && profile.Files.Length != 0)
                throw new InvalidOperationException($"Planned profile must not contain files: {profile.Id}");
            foreach (var path in profile.Files)
                if (!File.Exists(SafePath(catalog, path)))
                    throw new IOException($"Missing catalog file: {path}");
        }

        // Check the complete catalog, not only the profiles selected for this invocation.
        // Every available profile is composable, so two entries must never claim one destination.
        var owners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        void Own(string relative, string owner)
        {
            _ = SafePath(catalog, relative); // Validate portable relative path syntax.
            if (relative == ConfigName)
                throw new InvalidOperationException("docs.config.json is reserved for the generator.");
            if (!owners.TryAdd(relative, owner))
                throw new InvalidOperationException($"Duplicate output path '{relative}': {owners[relative]} and {owner}");
        }

        var baseDirectory = Path.Combine(catalog, "base");
        if (!Directory.Exists(baseDirectory))
            throw new DirectoryNotFoundException($"Missing catalog base: {baseDirectory}");
        foreach (var path in SafeFiles.Enumerate(baseDirectory))
        {
            var relative = Path.GetRelativePath(baseDirectory, path).Replace('\\', '/');
            if (relative.StartsWith("docs/", StringComparison.Ordinal) && relative.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                && Path.GetFileName(relative) != "index.md")
                DocumentMetadata.AssertFileName(relative);
            Own(relative, "base");
        }
        foreach (var profile in data.Profiles)
        {
            if (string.IsNullOrWhiteSpace(profile.Name)
                || !System.Text.RegularExpressions.Regex.IsMatch(profile.Id, "^[a-z][a-z0-9-]*$"))
                throw new InvalidOperationException($"Invalid profile ID or name: {profile.Id}");
            foreach (var path in profile.Files)
            {
                DocumentMetadata.AssertFileName(path);
                if (!path.StartsWith("docs/", StringComparison.Ordinal))
                    throw new InvalidOperationException($"Profile output must be under docs/: {profile.Id} / {path}");
                Own(path, $"profile {profile.Id}");
            }
        }
        foreach (var module in modules.Modules)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(module.Id, "^[a-z][a-z0-9-]*$"))
                throw new InvalidOperationException($"Invalid documentation module ID: {module.Id}");
            foreach (var file in module.Files)
            {
                DocumentMetadata.AssertFileName(file.Destination);
                Own(file.Destination, $"module {module.Id}");
            }
        }
        foreach (var skill in skills.Skills)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(skill.Id, "^[a-z][a-z0-9-]*$"))
                throw new InvalidOperationException($"Invalid skill ID: {skill.Id}");
            Own(skill.Destination, $"skill {skill.Id}");
        }
        Own("index.html", "site");
        Own("llms.txt", "agent index");

        var byId = data.Profiles.ToDictionary(x => x.Id, StringComparer.Ordinal);
        foreach (var profile in data.Profiles)
        {
            if (profile.Requires.Distinct(StringComparer.Ordinal).Count() != profile.Requires.Length)
                throw new InvalidOperationException($"Duplicate dependency in profile: {profile.Id}");
            foreach (var dependency in profile.Requires)
                if (!byId.ContainsKey(dependency))
                    throw new InvalidOperationException($"Unknown dependency '{dependency}' in profile '{profile.Id}'.");
        }
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var active = new HashSet<string>(StringComparer.Ordinal);
        void Visit(string id)
        {
            if (visited.Contains(id)) return;
            if (!active.Add(id))
                throw new InvalidOperationException($"Cyclic catalog dependency: {id}");
            foreach (var dependency in byId[id].Requires) Visit(dependency);
            active.Remove(id);
            visited.Add(id);
        }
        foreach (var profile in data.Profiles) Visit(profile.Id);

    }

    private static string SafePath(string root, string relative) => SafeFiles.Child(root, relative);

    internal sealed record Catalog(int SchemaVersion, Profile[] Profiles);
    internal sealed record Profile(string Id, string Name, string Status, string[] Requires, string[] Files, string Category);
    internal sealed record SkillCatalog(int SchemaVersion, Skill[] Skills);
    internal sealed record Skill(string Id, string Name, string Source, string Destination);
    internal sealed record ModuleCatalog(int SchemaVersion, Module[] Modules);
    internal sealed record Module(string Id, string Name, ModuleFile[] Files);
    internal sealed record ModuleFile(string Source, string Destination);
    private sealed record ProjectConfig(string Name, string Description, string[] Profiles, string[] Skills, string[] Modules, string Site, string[] GeneratedFiles, Dictionary<string, string>? GeneratedHashes = null);
}
