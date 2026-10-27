# Generator CLI

Requires .NET 10 SDK. Repository-root usage:

```sh
dotnet run --project src/DocsBoilerplate.Cli -- list
dotnet run --project src/DocsBoilerplate.Cli -- doctor
dotnet run --project src/DocsBoilerplate.Cli -- init --name Example --description "Example docs" --output ./scratch/example --profiles dotnet,ef-core,blazor,mudblazor --modules testing,operations --skills docs-review,docs-maintain --site zensical --non-interactive --dry-run
dotnet run --project src/DocsBoilerplate.Cli -- init --name Example --description "Example docs" --output ./scratch/example --profiles dotnet,ef-core,blazor,mudblazor --modules testing,operations --skills docs-review,docs-maintain --site zensical --non-interactive
dotnet run --project src/DocsBoilerplate.Cli -- validate --output ./scratch/example
```

`--profiles` accepts comma-separated available profile IDs; dependencies are resolved automatically. `--modules` and `--skills` are independently optional. Use `list` for supported values and planned profiles. Omit flags for interactive prompts. The TUI first asks for backend and frontend, then only asks about data access, UI libraries and testing tools when relevant; architecture and modeling are independent. Unsupported profiles remain visible through `list` but cannot be selected.

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

## Optional backend guidance

Use `--profiles aspnet-core` for framework-neutral ASP.NET Core HTTP guidance. Select `aspnet-mvc` only when the application uses MVC controllers. FluentValidation, PostgreSQL and CQRS are independent opt-in profiles; dependencies (such as `aspnet-mvc` requiring `aspnet-core` and `dotnet`) are resolved automatically. A minimal `dotnet` project does not acquire MVC, a database or a command/query bus.

Example:

```sh
dotnet run --project src/DocsBoilerplate.Cli -- init --name Backend --description "Backend documentation" --output ./scratch/backend --profiles aspnet-mvc,ef-core,postgresql,fluentvalidation,cqrs --non-interactive --dry-run
```

Additional optional profiles: `masstransit-rabbitmq`, `ardalis-specification`, and `bunit`. Selecting bUnit requires Blazor, but not MudBlazor. Selecting MassTransit with RabbitMQ does not imply a database or a particular outbox implementation. Each profile installs only its applicable standard.

## Catalog integrity

`doctor` performs a whole-catalog preflight: all published profile dependencies must resolve without cycles, every profile/module/skill destination must be unique (case-insensitively for cross-platform use), and generated project paths cannot escape the repository. This validation occurs **before** output creation and also rejects collisions in currently unselected profiles.
