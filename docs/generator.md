# Generator CLI

Requires .NET 10 SDK. Repository-root usage:

```sh
dotnet run --project src/DocsBoilerplate.Cli -- list
dotnet run --project src/DocsBoilerplate.Cli -- doctor
dotnet run --project src/DocsBoilerplate.Cli -- init --name Example --description "Example docs" --output ./scratch/example --profiles dotnet,ef-core,blazor,mudblazor --modules testing,operations --skills docs-review,docs-maintain --site zensical --non-interactive --dry-run
dotnet run --project src/DocsBoilerplate.Cli -- init --name Example --description "Example docs" --output ./scratch/example --profiles dotnet,ef-core,blazor,mudblazor --modules testing,operations --skills docs-review,docs-maintain --site zensical --non-interactive
dotnet run --project src/DocsBoilerplate.Cli -- validate --output ./scratch/example
```

`--profiles` accepts comma-separated available profile IDs; dependencies are resolved automatically. `--modules` and `--skills` are independently optional. Use `list` for supported values and planned profiles. Omit flags for interactive prompts.

## Safety

`--dry-run` prints the file plan without writing. Initialization refuses changed generated files and does not overwrite user-created documents. An identical re-run succeeds even if the repository also contains user-authored files or static-site build output. The generated `docs.config.json` records exact selection and file ownership. `validate` checks selection consistency, missing owned files, unresolved template tokens and basic relative Markdown links.

Advanced update/merge behavior is not yet implemented. Initialize into a fresh or previously initialized folder.

## Optional static site

Select `--site zensical` to produce `zensical.toml`. All documentation lives under `docs/`, including selected engineering standards; no duplicates are copied for the site. Install Zensical separately (CI currently pins 0.0.73), then use:

```sh
cd scratch/example
zensical serve
zensical build --strict
```

Static builds are local and are **not** automatically published. Zensical is currently an early-release dependency; pin and re-test on upgrades.

## Validation status

CI runs a .NET 10 Release build, non-interactive end-to-end smoke test, optional Zensical build and negative tests. Check the latest GitHub Actions result rather than assuming the source code has passed.
