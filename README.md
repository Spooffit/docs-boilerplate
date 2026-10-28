---
title: "Docs Boilerplate"
description: "AI-first Docs-as-Code repository generator"
kind: index
status: active
---

# Docs Boilerplate

A Markdown-first **documentation repository generator** designed primarily for AI coding agents and secondarily for humans. The reusable source lives in [`catalog/`](catalog/), the .NET 10 CLI in [`src/`](src/), and generator tests in [`tests/`](tests/).

This is the **boilerplate itself**, not a working application's business specification. It contains no example product requirements, feature backlogs or historical project implementation documents. Technology-specific guidance is opt-in; a newly generated repository receives only the selected files.

Start with [`llms.txt`](llms.txt), [agent instructions](AGENTS.md), the [documentation index](docs/index.md), the [generator guide](docs/generator.md), and the [authoring standard](catalog/base/docs/standards/documentation.md).

## Quick start

Requires .NET SDK 10:

```sh
dotnet run --project src/DocsBoilerplate.Cli -- list
dotnet run --project src/DocsBoilerplate.Cli -- doctor
dotnet run --project src/DocsBoilerplate.Cli -- version
dotnet run --project src/DocsBoilerplate.Cli -- init --name Demo --description "Example documentation" --output ./scratch/demo --profiles dotnet,ef-core --non-interactive --dry-run
dotnet run --project src/DocsBoilerplate.Cli -- init --name Demo --description "Example documentation" --output ./scratch/demo --profiles dotnet,ef-core --non-interactive
dotnet run --project src/DocsBoilerplate.Cli -- validate --output ./scratch/demo
```

The CLI also packages as a local or global .NET Tool. From the boilerplate source, run `dotnet pack src/DocsBoilerplate.Cli -c Release -o ./artifacts` and install with `dotnet tool install DocsBoilerplate.Cli --tool-path ./tools --add-source ./artifacts --version 0.1.0`. Invoke `./tools/docs-boilerplate version` to inspect the installed CLI and supported manifest/catalog versions. A released package can be upgraded with `dotnet tool update DocsBoilerplate.Cli --tool-path ./tools --add-source ./artifacts --version <new-version>`; this does not automatically modify any generated project. These examples use locally built packages and do not claim a published NuGet release. See the [generator guide](docs/generator.md) for compatibility and upgrade rules.

Run `dotnet build src/DocsBoilerplate.Cli -c Release` and `bash tests/smoke.sh` after changing generator behavior. The cross-platform test workflow is [`.github/workflows/docs-generator.yml`](.github/workflows/docs-generator.yml).

To browse a generated repository, run `python -m http.server 3000` in its root and open `http://localhost:3000/`. Docsify renders the Markdown dynamically in the browser without a static-site build. Opening its `index.html` directly via `file://` is not a reliable mode.

## What the generator does

- Creates a minimal repository with `README.md`, `AGENTS.md`, `llms.txt`, `docs/`, a generated-file ownership manifest and an optional Docsify viewer.
- Selects composable, technology-scoped standards and templates rather than shipping one fixed stack. Available profiles include .NET, ASP.NET Core, MVC, PostgreSQL, Blazor, test frameworks, CQRS, messaging, HTTP contracts and other opt-in guidance.
- Offers optional project-document scaffolds and agent skills without imposing them on minimal projects.
- Supports dry-run, validation and preview-first updates with SHA-256 fingerprints, while protecting user-edited content.
- Maintains navigable Markdown and a concise `llms.txt` for agents; Docsify is an optional human reading layer.

## Boundaries

The CLI is a generator, not an implementation or a business requirement registry. Examples in templates are placeholders or explicitly optional technical illustrations; they are never evidence that a generated project's code exists. Planned technology profiles are visible but not selectable. Docsify requires browser network access to CDN assets unless self-hosted, and opening Markdown through a local web server is preferred.

See [implementation status](docs/implementation-status.md), [architectural decisions](docs/decisions/index.md), [pending owner choices](docs/decisions/owner-decision-backlog.md) and [contributing rules](CONTRIBUTING.md).

Earlier project's source specifications have been removed from the current branch; Git history retains past commits for reference.
