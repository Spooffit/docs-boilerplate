# CLI (early working slice)

Requires .NET 10 SDK. Use the root working directory or pass `--catalog` to point to the reusable catalog:

```sh
dotnet run --project src/DocsBoilerplate.Cli -- list
dotnet run --project src/DocsBoilerplate.Cli -- doctor
dotnet run --project src/DocsBoilerplate.Cli -- init --name Example --description "Example docs" --output ./scratch/example --profiles dotnet,ef-core,blazor,mudblazor --non-interactive --dry-run
dotnet run --project src/DocsBoilerplate.Cli -- init --name Example --description "Example docs" --output ./scratch/example --profiles dotnet,ef-core,blazor,mudblazor --non-interactive
dotnet run --project src/DocsBoilerplate.Cli -- validate --output ./scratch/example
```

Omit init flags for interactive prompts. Profiles are resolved from `catalog/profiles.json` with dependencies, and unsupported profiles are explicitly rejected. A nonempty output directory is rejected unless contents match precisely. The dry-run prints planned files without writing anything.

This implementation is intentionally a **first vertical slice**, not the completed specification. Template authoring and full update/merge semantics, advanced validation, independent CLI test suite, static site, skills, and packaging verification remain pending. .NET 10 SDK is not available in the current execution environment; compilation and runtime tests have not yet been performed.
