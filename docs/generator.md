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

`--dry-run` prints the file plan without writing. Initialization refuses changed generated files and does not overwrite user-created documents. An identical re-run succeeds even if the repository also contains unrelated user-authored files. The generated `docs.config.json` records exact selection and file ownership. `validate` checks selection consistency, missing owned files, unresolved template tokens, ordinary relative Markdown links, common heading fragments and generated `llms.txt` targets. It supports `#local-heading` and `file.md#heading` with fenced-code exclusion and duplicate heading suffixes. It does **not** implement every Markdown/Docsify anchor variant or a real browser-rendering check.

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

CI runs a .NET 10 Release build, CLI end-to-end smoke tests, Docsify static HTTP checks and negative validation tests. A separate Linux **Playwright + Chromium browser job** creates a real generated docs repository, serves it over HTTP, and checks Docsify rendering, nested relative links, hash-based deep routes, browser refresh, heading fragments and an optional project module. Browser failures upload trace and screenshot diagnostics.

Playwright and Chromium are installed **only in CI**; they never become dependencies, source files or build artifacts of the generated project. The viewer still depends on public Docsify 5 CDN assets and an HTTP server. Browser CI detects real loading problems but cannot guarantee CDN uptime or full compatibility across browsers and hosting platforms. Check the latest GitHub Actions result before claiming verification.

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

### Adding, moving and discovering documents

When an author adds a Markdown document under `docs/`, also link it from its immediate directory `index.md` and from `docs/catalog.md`. `validate` now checks that every project Markdown page is reachable from `docs/catalog.md` and every **additional author-created non-index page** is linked from its immediate directory index. All links remain relative. This deliberately does not enforce a particular feature/ADR naming scheme.

When renaming or moving a document, repair old/new directory indexes, its catalog entry and inbound links (including heading fragments). The validator detects broken existing links; it also detects the newly unlisted destination. The list in `llms.txt` is **curated**, not exhaustive: the root links to `docs/catalog.md`, so adding an ordinary page does not force another `llms.txt` entry. Update `llms.txt` manually for new major entry points only.

Navigation checks are read-only. Intentionally preserved `RetiredFiles` listed by prior `update` operations are exempt from current navigation coverage; they remain on disk without being presented as selected standards. A truly new author-created page is still required in its parent index and the catalog. The original generated indexes and `docs/catalog.md` remain fingerprint-owned for update safety. Authors may edit them, but if upstream also changes the same owned file, `update` will report a conflict requiring human reconciliation; it will not silently regenerate over an edited index. Generated optional technology templates may be discoverable through the catalog alone rather than through every authored category index.

## First project intake

Fresh repositories include `docs/project/overview.md` and `docs/architecture/overview.md`, both deliberately marked `draft`. These existing documents help an agent distinguish verified requirements, observed code, accepted target architecture and open decisions. They are not a completed system design.

After initialization, examine real project sources and fill these pages with traceable claims. Leave unknown facts as **Not yet verified** and keep the draft status until accepted. Technology-profile selection installs writing guidance, not evidence of implementation. The optional `docs-init` skill provides the same first-pass process.

Create Feature, Module, Question, ADR or Milestone documents only when needed. Run `validate` after changes: a successful structural check is not proof that product decisions or source claims are correct.

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
- Reject changes if both a user and the template modified the same file; **no writes occur when a preflight conflict exists**. The preview identifies each conflicting path and tells the owner to reconcile it in Git rather than edit manifest fingerprints.
- Reject collisions with newly proposed paths that are already user-owned, including file or parent-folder names that differ only by letter case, and existing files where a new profile needs a directory. This prevents case-sensitive/case-insensitive conflicts and failures partway through an update.
- Add new files when selected profiles gain documentation.
- Preserve retired files, but remove their paths from managed ownership and regenerated navigation. Their exact paths are carried as `RetiredFiles` in `docs.config.json`, so validation does not mistake them for newly authored, unindexed pages. No automatic deletion.
- Record new fingerprints and update `docs.config.json` last. If an ordinary write fails, restore original file bytes, delete newly added files and remove directories created by the failed update **only when empty**. When a file was independently modified after writing, report an incomplete rollback rather than overwrite that modification.

Repositories generated before fingerprints were introduced cannot be updated automatically. Their original bytes cannot be proven, so upgrade by initializing a new output and manually reviewing the diffs. There is no unconditional `--force`.

**Rollback verification:** Focused .NET tests inject failures after multiple successful writes, including immediately after the ownership manifest is written. They verify restoration of prior bytes, deletion of new files and empty directories, preservation of user-owned documents, and explicit reporting of concurrent edits that cannot safely be reversed. The fault-injection hook is internal to tests, not a user-facing CLI option.

**Limits:** This is a safe single-writer workflow, not a fully transactional filesystem migration. A system crash, sudden power loss, or a hostile process racing on the same filesystem may interrupt the operation. Commit project changes to Git and review the preview before applying.
