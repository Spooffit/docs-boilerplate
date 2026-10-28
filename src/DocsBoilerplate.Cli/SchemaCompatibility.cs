/// <summary>
/// Independent versions for the input catalogs and generated project manifests.
/// A manifest without SchemaVersion is the original v1 shape, accepted only
/// when the existing fingerprint ownership checks pass.
/// </summary>
internal static class SchemaCompatibility
{
    internal const int CatalogVersion = 1;
    internal const int ProjectVersion = 1;

    internal static void AssertCatalogVersion(int profiles, int skills, int modules)
    {
        if (profiles != CatalogVersion || skills != CatalogVersion || modules != CatalogVersion)
            throw new InvalidOperationException(
                $"Unsupported catalog schema version. This CLI supports schemaVersion {CatalogVersion}. " +
                "Use a compatible generator release and catalog; no automatic catalog migration is available.");
    }

    internal static void AssertProjectVersion(int? version)
    {
        if (version is null || version == ProjectVersion) return;
        throw new InvalidOperationException(
            $"Unsupported docs.config.json SchemaVersion {version}. " +
            $"This CLI supports version {ProjectVersion}; absence of the field denotes legacy version 1. " +
            "Use a compatible generator release. Project files were not changed.");
    }
}
