---
title: "Generator CLI"
description: "Maintainer documentation"
kind: guide
status: active
---

# Generator CLI

## Install and run without cloning

Once published, the installable CLI is available from nuget.org:

```powershell
dotnet tool install --global docs-boilerplate
docs-boilerplate doctor
docs-boilerplate list
docs-boilerplate init
dotnet tool update --global docs-boilerplate
```

**Before first publication**, download the private `docs-boilerplate-dotnet-tool` GitHub Actions artifact, extract its `.nupkg` into `C:\\Tools\\docs-boilerplate-feed`, and install:

```powershell
dotnet tool install --global docs-boilerplate --version 1.0.1 --add-source C:\\Tools\\docs-boilerplate-feed
```

The tagged release workflow and the initial NuGet.org Trusted Publishing setup are documented in [the release guide](releases.md).

The prompt sequence is **Backend → Backend options → Frontend → Frontend options → Infrastructure (last and optional)**. Logging, metrics, traces, deployment and DevOps repository location each have an independent single-choice prompt including `none`. A .NET-oriented orchestration prompt offers Aspire only when .NET has been chosen. Other portable policies remain independently selectable; the wizard never treats selected guidance as proof of an implementation. Noninteractive usage accepts the same profile IDs through `--profiles` and rejects conflicting infrastructure options (e.g. both Compose and Kubernetes).

The publication pipeline is ready, but it **does not publish on every commit**. NuGet.org availability depends on completing the one-time publisher identity setup and pushing a release tag. From a source checkout you can still run `dotnet pack src/docs-boilerplate -c Release -o ./artifacts/tool-feed`.

## Initialize from a source checkout

Requires .NET 10 SDK. Repository-root usage:

```sh
dotnet run --project src/docs-boilerplate -- list
dotnet run --project src/docs-boilerplate -- doctor
dotnet run --project src/docs-boilerplate -- init --name Example --description "Example docs" --output ./scratch/example --profiles dotnet,ef-core,blazor,mudblazor --modules testing,operations --skills docs-review,docs-maintain --site docsify --non-interactive --dry-run
dotnet run --project src/docs-boilerplate -- init --name Example --description "Example docs" --output ./scratch/example --profiles dotnet,ef-core,blazor,mudblazor --modules testing,operations --skills docs-review,docs-maintain --site docsify --non-interactive
dotnet run --project src/docs-boilerplate -- validate --output ./scratch/example
```

`--profiles` accepts comma-separated available profile IDs; dependencies are resolved automatically. Use **`--profiles none`** for a genuinely stack-neutral repository with only the base Markdown documentation and no selected technology profiles. `--modules` and `--skills` are independently optional. Use `list` for supported values and planned profiles. Omit flags for interactive prompts. The TUI first asks for backend and frontend, then only asks about data access, UI libraries and testing tools when relevant; architecture and modeling are independent. Unsupported profiles remain visible through `list` but cannot be selected. The final **additional optional profiles** prompt includes available independent policies (such as document identifiers, storage, HTTP consumption and UI conventions) not already offered in the earlier technology prompts. Dependencies are resolved automatically, even when a category was not part of the chosen backend.

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

Use `--profiles dotnet` for C#/.NET backend guidance. `aspnet-core` is a separate optional HTTP API profile; MVC-specific defaults are not selected. FluentValidation, PostgreSQL and CQRS remain independently opt-in. A minimal `dotnet` project does not acquire an MVC layer, database or command/query bus.

Example:

```sh
dotnet run --project src/docs-boilerplate -- init --name Backend --description "Backend documentation" --output ./scratch/backend --profiles dotnet,aspnet-core,ef-core,postgresql,fluentvalidation,cqrs --non-interactive --dry-run
```

Additional optional profiles: `masstransit-rabbitmq`, `ardalis-specification`, `bunit`, `http-contracts`, `integration-reliability`, and `object-storage`. These last three are language-neutral and do not imply .NET, an ORM or a particular server. Selecting bUnit requires Blazor, but not MudBlazor. Selecting MassTransit with RabbitMQ does not imply a database or a particular outbox implementation. Each profile installs only its applicable standard.

## Cross-platform HTTP and migration guidance

For clients consuming public HTTP APIs without selecting any backend framework, use `--profiles http-contracts --modules api`. It generates contract verification and client retry/concurrency guidance but no ASP.NET or .NET documents.

For PostgreSQL deployments, `--profiles postgresql` includes a migration rollout planning template at `docs/templates/postgresql-migration.md` alongside database standards. This does not install an ORM or select a release strategy. Confirm locks, data migration, compatibility and rollback for the actual deployed version before performing destructive changes.

## Catalog integrity

`doctor` performs a whole-catalog preflight: all available profile dependencies must resolve without cycles, every profile/module/skill destination must be unique (case-insensitively for cross-platform use), and generated project paths cannot escape the repository. This validation occurs **before** output creation and also rejects collisions in currently unselected profiles.

## Generated navigation

The generated `llms.txt` is a short, **installed-content-only** discovery index. It starts with project and architecture entrypoints, adds a `Project areas` section only for optional project-specific documents actually present in the generated output, and links to selected standards groups and the authored template guide. Planned or unselected modules are never linked. `AGENTS.md` explains which source establishes an approved decision, actual wire contract, or observed implementation; neither navigation index is proof of project adoption. Agents should follow only documents relevant to their current task.

Every new repository receives `docs/catalog.md`, a deterministic navigation list built from the actual selected profiles, optional modules and templates. An authored `docs/templates/index.md` helps agents pick the smallest applicable template without a second directory index. The documentation homepage links to the catalog. Planned or unselected standards never appear in this catalog. After initialization this is a normal Markdown document that project owners can maintain; no implicit update overwrites it.

### Adding, moving and discovering documents

When an author adds a Markdown document under `docs/`, also link it from its immediate directory `index.md` and from `docs/catalog.md`. `validate` now checks that every project Markdown page is reachable from `docs/catalog.md` and every **additional author-created non-index page** is linked from its immediate directory index. For an author-created nested directory, its `index.md` must also be linked from the parent directory index. All links remain relative. This deliberately does not enforce a particular feature/ADR naming scheme.

When renaming or moving a document, repair old/new directory indexes, its catalog entry and inbound links (including heading fragments). The validator detects broken existing links; it also detects the newly unlisted destination. The list in `llms.txt` is **curated**, not exhaustive: the root links to `docs/catalog.md`, so adding an ordinary page does not force another `llms.txt` entry. Update `llms.txt` manually for new major entry points only.

Navigation checks are read-only. Intentionally preserved `RetiredFiles` listed by prior `update` operations are exempt from current navigation coverage; they remain on disk without being presented as selected standards. A truly new author-created page is still required in its parent index and the catalog. The original generated indexes and `docs/catalog.md` remain fingerprint-owned for update safety. Authors may edit them, but if upstream also changes the same owned file, `update` will report a conflict requiring human reconciliation; it will not silently regenerate over an edited index. Generated optional technology templates may be discoverable through the catalog alone rather than through every authored category index.

## First project intake

Fresh repositories include `docs/project/overview.md` and `docs/architecture/overview.md`, both deliberately marked `draft`. These existing documents help an agent distinguish verified requirements, observed code, accepted target architecture and open decisions. They are not a completed system design.

After initialization, examine real project sources and fill these pages with traceable claims. Leave unknown facts as **Not yet verified** and keep the draft status until accepted. Technology-profile selection installs writing guidance, not evidence of implementation. The optional `docs-init` skill provides the same first-pass process.

Create Feature, Module, Question, ADR or Milestone documents only when needed. Run `validate` after changes: a successful structural check is not proof that product decisions or source claims are correct.

## Stage two: agent-assisted project discovery

Every newly initialized project contains four **temporary** files: `bootstrap.md`, `.agents/bootstrap/questionnaire.md`, `.agents/bootstrap/finish-checklist.md` and `.agents/skills/project-intake/SKILL.md`. These are intentionally not counted as permanent selected skills or generator-owned files. The questionnaire asks about product goals, existing and related repositories, integrations, actually planned milestones and initial terminology; follow-ups depend on owner answers, and nothing is assumed implemented.

Ask the AI agent to read `bootstrap.md`, conduct the interview in short rounds, update project and architecture overviews, create a glossary only when useful, and request explicit owner approval. The agent then previews `docs-boilerplate bootstrap-complete --output <project-root>`, and, only on approval, executes `docs-boilerplate bootstrap-complete --output <project-root> --apply`. The command refuses to delete an intake file that the owner modified; it never deletes permanent documents. The generated repository remains valid after cleanup and later template updates do not recreate temporary files.

## Updating a generated repository

With a global tool installed, run `docs-boilerplate update --output <project-root>` to preview and `docs-boilerplate update --output <project-root> --apply` to write accepted changes. The following `dotnet run` alternatives are for a **boilerplate source checkout**; a generated project does not contain that source. Point `--output` at the generated repository.

New repositories record SHA-256 fingerprints for all generated files in `docs.config.json`. These fingerprints describe **original template bytes**, not the user's subsequent edits. Update re-renders the selected profiles, skills, modules and site option against a newer catalog.

```sh
dotnet run --project src/docs-boilerplate -- update --output ./my-docs
dotnet run --project src/docs-boilerplate -- update --output ./my-docs --apply
dotnet run --project src/docs-boilerplate -- validate --output ./my-docs

# Only if an interrupted update is detected:
dotnet run --project src/docs-boilerplate -- recover --output ./my-docs
```

`update` is preview-only by default. `--apply` must be specified to write changes. It will:

- Update a file only when it still matches the original recorded hash, or leave it unchanged if the new template is already present.
- Preserve local edits when the upstream template has not changed.
- Reject changes if both a user and the template modified the same file; **no writes occur when a preflight conflict exists**. The preview identifies each conflicting path and tells the owner to reconcile it in Git rather than edit manifest fingerprints.
- Reject collisions with newly proposed paths that are already user-owned, including file or parent-folder names that differ only by letter case, and existing files where a new profile needs a directory. This prevents case-sensitive/case-insensitive conflicts and failures partway through an update.
- Add new files when selected profiles gain documentation.
- Preserve retired files, but remove their paths from managed ownership and regenerated navigation. Their exact paths are carried as `RetiredFiles` in `docs.config.json`, so validation does not mistake them for newly authored, unindexed pages. No automatic deletion.
- Record new fingerprints and update `docs.config.json` last. Before writing any project file, stage the full desired content and verified original-file snapshots under the temporary `.docs-boilerplate-update/` directory. If an ordinary write fails, restore original bytes, delete additions and remove newly created directories **only when empty**. Concurrent changes are never silently overwritten.

### Recovery after process termination

The `.docs-boilerplate-update/` directory exists **only during an active or interrupted update**. It is temporary working state, not project documentation or an output artifact to commit. Its `plan.json` is written after all input and backup files are staged, and before the first document write. New document bytes are promoted by per-file rename. The ownership manifest is still written last.

When `update` or `validate` detects this directory, it stops rather than trusting a potentially half-updated project. Inspect the working tree, then run:

```sh
dotnet run --project src/docs-boilerplate -- recover --output ./my-docs
dotnet run --project src/docs-boilerplate -- validate --output ./my-docs
```

If only some planned changes were written, recovery verifies **all current target hashes and required backups before restoring anything**. It rolls back only content matching the journal's generated hash; unexpected user edits cause a safe refusal and the journal is retained for manual reconciliation. If *all* desired file hashes are present (including the new `docs.config.json`), recovery treats the update as completed and only removes the journal.

If termination happened during *staging* and no complete `plan.json` exists, the CLI cannot authenticate the temporary folder's contents. It will not delete it automatically; inspect the directory and remove it manually before trying again. No project writes begin before `plan.json` is created.

Recovery does not guarantee power-loss durability, concurrent-writer isolation or atomicity across multiple files. Back up or commit local changes before updates, and do not commit the temporary journal.

Repositories generated before fingerprints were introduced cannot be updated automatically. Their original bytes cannot be proven, so upgrade by initializing a new output and manually reviewing the diffs. There is no unconditional `--force`.

**Rollback verification:** Focused .NET tests inject failures after multiple successful writes, including immediately after the ownership manifest is written. They verify restoration of prior bytes, deletion of new files and empty directories, preservation of user-owned documents, and explicit reporting of concurrent edits that cannot safely be reversed. The fault-injection hook is internal to tests, not a user-facing CLI option.

**Limits:** This is a safe single-writer workflow, not a fully transactional filesystem migration. A system crash, sudden power loss, or a hostile process racing on the same filesystem may interrupt the operation. Commit project changes to Git and review the preview before applying.
