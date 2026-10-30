static partial class DocsCli
{
    internal static string ValidateLanguage(string? language)
    {
        if (language is "en" or "ru") return language;
        throw new InvalidOperationException(
            $"Unsupported documentation language: {language}. Supported languages: en, ru.");
    }

    // English files are the canonical source. A Russian selection MUST have
    // an explicit translation for every installed Markdown file. No silent
    // English fallbacks or network-dependent translation during initialization.
    internal static async Task<string> ReadLocalizedSourceAsync(
        string catalog, string source, string language)
    {
        language = ValidateLanguage(language);
        var path = language == "en"
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
