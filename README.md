# Docs Boilerplate

A Markdown-first **documentation repository generator** designed primarily for AI coding agents and secondarily for humans. The reusable source lives in [`catalog/`](catalog/), the .NET 10 CLI in [`src/`](src/), and generator tests in [`tests/`](tests/).

This is the **boilerplate itself**, not a working application's business specification. It contains no example product requirements, feature backlogs or historical project implementation documents. Technology-specific guidance is opt-in; a newly generated repository receives only the selected files.

Start with [`llms.txt`](llms.txt), [agent instructions](AGENTS.md), the [documentation index](docs/index.md), the [generator guide](docs/generator.md), and the [authoring standard](catalog/base/docs/standards/documentation.md).

## Install the CLI as a .NET tool

Version **1.0.1** has been published to NuGet.org. Install or update the tool directly, without cloning this repository:

```powershell
dotnet tool install --global docs-boilerplate
docs-boilerplate doctor
docs-boilerplate list
dotnet tool update --global docs-boilerplate
```

The verified [v1.0.1 GitHub Release](https://github.com/Spooffit/docs-boilerplate/releases/tag/v1.0.1) contains the same `docs-boilerplate.1.0.1.nupkg` published to NuGet.org. The older `v1.0.0` tag was never published and remains unchanged.

For offline/local-feed installations, the normal CI workflow still uploads the `docs-boilerplate-dotnet-tool` package artifact. You can also build locally using `dotnet pack src/DocsBoilerplate.Cli -c Release -o ./artifacts/tool-feed`. The installed tool bundles its Markdown catalog.

New releases are created **only by version tags** (for example `v1.0.2`), not by ordinary pushes to `main`. See [the release and NuGet setup guide](docs/releases.md).

## Create a project

```powershell
docs-boilerplate init
```

Spectre.Console prompts for project name, description, destination and documentation language (`en` or `ru`), then a **three-stage wizard**: backend, frontend and optional infrastructure. Options are grouped into labeled blocks. Compatible tools use checkboxes; exclusive choices use a single-choice selector: **TUnit or xUnit**, storage type, **MinIO or AWS S3 client**, logs, metrics, traces and deployment. bUnit is a normal Blazor component-testing checkbox. At the end, the CLI shows the selected configuration instead of a file plan and **runs validation automatically after generation**.

For automation, pass explicit profiles and optionally `--language en` (default). The localization source tree lives at `catalog/locales/<language>/` with the same relative Markdown paths as `catalog/`. Russian localization is under development: `--language ru` fails on any missing translation rather than silently delivering mixed-language agent instructions.

For automation, pass explicit profiles:

```powershell
docs-boilerplate init --name MyProject --description "Project documentation" --output ./MyProject.Docs --profiles dotnet,aspnet-core,infra-logging-loki,infra-deploy-compose --site docsify --non-interactive
docs-boilerplate validate --output ./MyProject.Docs
```

**Stage two:** open the generated project with an AI coding agent and ask it to follow `bootstrap.md`. The temporary questionnaire and `project-intake` skill guide conditional questions about product purpose, adjacent projects, integrations, milestones and glossary. After your explicit approval:

```powershell
docs-boilerplate bootstrap-complete --output ./MyProject.Docs
docs-boilerplate bootstrap-complete --output ./MyProject.Docs --apply
docs-boilerplate validate --output ./MyProject.Docs
```

Only the four unchanged temporary intake files are deleted. The user-authored documentation, selected standards and persistent skills remain.

## Source checkout / development

Requires .NET SDK 10:

```sh
dotnet run --project src/DocsBoilerplate.Cli -- list
dotnet run --project src/DocsBoilerplate.Cli -- doctor
dotnet run --project src/DocsBoilerplate.Cli -- init --name Demo --description "Example documentation" --output ./scratch/demo --profiles dotnet,ef-core --non-interactive --dry-run
dotnet run --project src/DocsBoilerplate.Cli -- init --name Demo --description "Example documentation" --output ./scratch/demo --profiles dotnet,ef-core --non-interactive
dotnet run --project src/DocsBoilerplate.Cli -- validate --output ./scratch/demo
```

The `dotnet run` commands above are only for source-checkout development. An installed `docs-boilerplate` tool can be called from any directory. The generated project does not contain source code for the CLI.

Run `dotnet build src/DocsBoilerplate.Cli -c Release` and `bash tests/smoke.sh` after changing generator behavior. The cross-platform test workflow is [`.github/workflows/docs-generator.yml`](.github/workflows/docs-generator.yml).

To browse a generated repository, run `python -m http.server 3000` in its root and open `http://localhost:3000/`. Docsify renders the Markdown dynamically in the browser without a static-site build. Opening its `index.html` directly via `file://` is not a reliable mode.

To initialize only the shared Markdown documentation without any technology standards, use `--profiles none --site none` with the required project name, description and output path.

## What the generator does

- Creates a minimal repository with `README.md`, `AGENTS.md`, `llms.txt`, `docs/`, a generated-file ownership manifest and an optional Docsify viewer.
- Selects composable, technology-scoped standards and templates rather than shipping one fixed stack. Available profiles include .NET, ASP.NET Core, PostgreSQL, Blazor, test frameworks, CQRS, messaging, HTTP contracts and other opt-in guidance.
- Installs all documentation modules and persistent agent skills by default, together with baseline integration reliability and stable document identifiers. Project-specific claims stay draft until verified.
- Supports dry-run, validation and preview-first updates with SHA-256 fingerprints, while protecting user-edited content.
- Maintains navigable Markdown and a concise `llms.txt` for agents; Docsify is an optional human reading layer.

## Boundaries

The CLI is a generator, not an implementation or a business requirement registry. Examples in templates are placeholders or explicitly optional technical illustrations; they are never evidence that a generated project's code exists. Planned technology profiles are visible but not selectable. Docsify requires browser network access to CDN assets unless self-hosted, and opening Markdown through a local web server is preferred.

Technology conventions such as test-method naming, HTTP ETag/If-Match concurrency and three-project MassTransit EventBus composition are independently **opt-in** profiles. Their examples and verification do not add a demonstration application to the generated repository. See [ADR 0006](docs/decisions/0006-optional-technology-conventions.md).

The original editorial choices are recorded in [ADR 0007](docs/decisions/0007-final-editorial-policies.md); the latest defaults supersede its optional document-ID/approval choice. **Stable document IDs are mandatory, approval statuses are not**, while direct API request DTO form binding remains opt-in. The documentation catalog does not prescribe client-library development or source-model generation; incomplete code examples must be labeled illustrative and claims of executable examples require real test evidence.

See [implementation status](docs/implementation-status.md), [approved template scope](docs/decisions/0005-approved-template-scope.md), [architectural decisions](docs/decisions/index.md), [completed owner decision review](docs/decisions/owner-decision-backlog.md) and [contributing rules](CONTRIBUTING.md).

