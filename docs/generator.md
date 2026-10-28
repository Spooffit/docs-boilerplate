---
title: "Generator CLI"
description: "Maintainer documentation"
kind: guide
status: active
---

# Generator CLI

Requires .NET 10 SDK. Repository-root usage:

```sh
dotnet run --project src/DocsBoilerplate.Cli -- list
dotnet run --project src/DocsBoilerplate.Cli -- doctor
dotnet run --project src/DocsBoilerplate.Cli -- init --name Example --description "Example docs" --output ./scratch/example --profiles dotnet,ef-core,blazor,mudblazor --modules testing,operations --skills docs-review,docs-maintain --site docsify --non-interactive --dry-run
dotnet run --project src/DocsBoilerplate.Cli -- init --name Example --description "Example docs" --output ./scratch/example --profiles dotnet,ef-core,blazor,mudblazor --modules testing,operations --skills docs-review,docs-maintain --site docsify --non-interactive
dotnet run --project src/DocsBoilerplate.Cli -- validate --output ./scratch/example
```

`--profiles` accepts comma-separated available profile IDs; dependencies are resolved automatically. `--modules` and `--skills` are independently optional. Use `list` for supported values and planned profiles. Omit flags for interactive prompts. The TUI first asks for backend and frontend, then only asks about data access, UI libraries and testing tools when relevant; architecture and modeling are independent. Unsupported profiles remain visible through `list` but cannot be selected.

## Safety

`--dry-run` prints the file plan without writing. Initialization refuses changed generated files and does not overwrite user-created documents. An identical re-run succeeds even if the repository also contains unrelated user-authored files. The generated `docs.config.json` records exact selection and file ownership. `validate` checks selection consistency, missing owned files, unresolved template tokens and basic relative Markdown links.

Updates with fingerprint-based conflict detection are supported (see below). Automatic three-way merges and arbitrary site-engine migrations are intentionally not supported.

## Optional website: Docsify

The default generated repository includes only a root `index.html` using Docsify and the existing Markdown under `docs/`. It adds **no build directory, Node dependencies, Python packages or generated HTML**. Pass `--site none` to omit the viewer.

Docsify requests Markdown over HTTP. **Do not rely on double-clicking `index.html` via `file://`**, which commonly fails due to modern browser CORS restrictions. For a local preview, from the generated repository root run:

```sh
python -m http.server 3000
```

Then open `http://localhost:3000/`. Python is used only to serve files and is not required by the generator. Alternatively host the unchanged repository on an ordinary static file server. Docsify's JavaScript and CSS load from a CDN, so the viewer needs network access unless assets are intentionally self-hosted. Markdown and agent workflows remain usable offline.

Only `docsify` and `none` are supported. If an older repository stores a different site identifier, initialize a fresh repository and review the Markdown diff manually; the CLI does not run or migrate retired site engines.

## Validation status

CI runs a .NET 10 Release build, non-interactive end-to-end smoke test, Docsify entrypoint checks and negative tests. Check the latest GitHub Actions result rather than assuming the source code has passed.

## Optional backend guidance

Use `--profiles aspnet-core` for framework-neutral ASP.NET Core HTTP guidance. Select `aspnet-mvc` only when the application uses MVC controllers. FluentValidation, PostgreSQL and CQRS are independent opt-in profiles; dependencies (such as `aspnet-mvc` requiring `aspnet-core` and `dotnet`) are resolved automatically. A minimal `dotnet` project does not acquire MVC, a database or a command/query bus.

Example:

```sh
dotnet run --project src/DocsBoilerplate.Cli -- init --name Backend --description "Backend documentation" --output ./scratch/backend --profiles aspnet-mvc,ef-core,postgresql,fluentvalidation,cqrs --non-interactive --dry-run
```

Additional optional profiles: `masstransit-rabbitmq`, `ardalis-specification`, `bunit`, `http-contracts`, `integration-reliability`, and `object-storage`. These last three are language-neutral and do not imply .NET, an ORM or a particular server. Selecting bUnit requires Blazor, but not MudBlazor. Selecting MassTransit with RabbitMQ does not imply a database or a particular outbox implementation. Each profile installs only its applicable standard.

## Cross-platform HTTP and migration guidance

For clients consuming public HTTP APIs without selecting any backend framework, use `--profiles http-contracts --modules api`. It generates contract verification and client retry/concurrency guidance but no ASP.NET or .NET documents.

For PostgreSQL deployments, `--profiles postgresql` includes a migration rollout planning template at `docs/templates/postgresql-migration.md` alongside database standards. This does not install an ORM or select a release strategy. Confirm locks, data migration, compatibility and rollback for the actual deployed version before performing destructive changes.

## Catalog integrity

`doctor` performs a whole-catalog preflight: all published profile dependencies must resolve without cycles, every profile/module/skill destination must be unique (case-insensitively for cross-platform use), and generated project paths cannot escape the repository. This validation occurs **before** output creation and also rejects collisions in currently unselected profiles.

## Generated navigation

The generated `llms.txt` is a short, **installed-content-only** discovery index. It starts with project and architecture entrypoints, adds a `Project areas` section only for optional project-specific documents actually present in the generated output, and links to selected standards groups and the authored template guide. Planned or unselected modules are never linked. `AGENTS.md` explains which source establishes an approved decision, actual wire contract, or observed implementation; neither navigation index is proof of project adoption. Agents should follow only documents relevant to their current task.

Every new repository receives `docs/catalog.md`, a deterministic navigation list built from the actual selected profiles, optional modules and templates. An authored `docs/templates/index.md` helps agents pick the smallest applicable template without a second directory index. The documentation homepage links to the catalog. Planned or unselected standards never appear in this catalog. After initialization this is a normal Markdown document that project owners can maintain; no implicit update overwrites it.

## Updating a generated repository

New repositories record SHA-256 fingerprints for all generated files in `docs.config.json`. These fingerprints describe **original template bytes**, not the user's subsequent edits. Update re-renders the selected profiles, skills, modules and site option against a newer catalog.

```sh
docs-boilerplate update --output ./my-docs
docs-boilerplate update --output ./my-docs --apply
docs-boilerplate validate --output ./my-docs
```

`update` is preview-only by default. `--apply` must be specified to write changes. It will:

- Update a file only when it still matches the original recorded hash, or leave it unchanged if the new template is already present.
- Preserve local edits when the upstream template has not changed.
- Reject changes if both a user and the template modified the same file; **no writes occur when a preflight conflict exists**.
- Reject collisions with newly proposed paths that are already user-owned.
- Add new files when selected profiles gain documentation.
- Preserve retired files, but remove their paths from managed ownership and regenerated navigation. No automatic deletion.
- Record new fingerprints and update `docs.config.json` last; attempt rollback of completed writes if a write fails.

Repositories generated before fingerprints were introduced cannot be updated automatically. Their original bytes cannot be proven, so upgrade by initializing a new output and manually reviewing the diffs. There is no unconditional `--force`.

**Limits:** This is a safe single-writer workflow, not a fully transactional filesystem migration. A system crash, sudden power loss, or a hostile process racing on the same filesystem may interrupt the operation. Commit project changes to Git and review the preview before applying.
