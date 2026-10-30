using System.Text;

static partial class DocsCli
{
    private static void AssertLanguage(string language)
    {
        if (language is not ("en" or "ru"))
            throw new InvalidOperationException($"Unsupported documentation language: {language}. Choose en or ru.");
    }

    // The English catalog is the canonical source. A locale overlays only the
    // content, not output paths, profile IDs, manifest ownership or dependencies.
    // Missing translations fail closed: mixed-language instructions are unsafe.
    private static async Task<string> ReadLocalizedAsync(string catalog, string relativeSource, string language)
    {
        AssertLanguage(language);
        var source = language == "en"
            ? relativeSource
            : "locales/" + language + "/" + relativeSource;
        var full = SafePath(catalog, source);
        if (!File.Exists(full))
            throw new IOException($"Missing {language} translation for {relativeSource}. " +
                "A mixed-language repository will not be generated.");
        return await File.ReadAllTextAsync(full, Encoding.UTF8);
    }
}
