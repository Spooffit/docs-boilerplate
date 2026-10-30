using System.Text.Json;

static partial class DocsCli
{
    internal static string ValidateLanguage(string? language)
    {
        if (language is "en" or "ru") return language;
        throw new InvalidOperationException(
            $"Unsupported documentation language: {language}. Supported languages: en, ru.");
    }

    internal static async Task<Catalog> LocalizedCatalogAsync(string catalog, Catalog original, string language)
    {
        language = ValidateLanguage(language);
        if (language == "en") return original;
        var path = SafePath(catalog, "locales/" + language + "/profiles.json");
        var names = JsonSerializer.Deserialize<Dictionary<string, string>>(await File.ReadAllTextAsync(path))
            ?? throw new IOException("Invalid localized profile names.");
        foreach (var profile in original.Profiles)
            if (!names.TryGetValue(profile.Id, out var title) || string.IsNullOrWhiteSpace(title))
                throw new IOException($"Missing {language} name for profile {profile.Id}.");
        return new Catalog(original.Profiles.Select(profile =>
            profile with { Name = names[profile.Id] }).ToArray());
    }

    // English files are the canonical source. A Russian selection MUST have
    // an explicit translation for every installed Markdown file. No silent
    // English fallbacks or network-dependent translation during initialization.
    internal static async Task<string> ReadLocalizedSourceAsync(
        string catalog, string source, string language)
    {
        language = ValidateLanguage(language);
        var path = language == "en" || !source.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            ? source
            : "locales/" + language + "/" + source;
        var full = SafePath(catalog, path);
        if (!File.Exists(full))
            throw new IOException(
                $"Missing {language} documentation translation: {source}. " +
                "No output was written. Add the translation before selecting this profile.");
        return await File.ReadAllTextAsync(full);
    }
}
