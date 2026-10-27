---
title: "Docs Boilerplate"
description: "Repository documentation and guidance"
kind: index
status: active
---

# Docs Boilerplate

A work-in-progress **Docs-as-Code repository generator**, not yet a fully migrated template.

## Current state

The reusable implementation lives in [`catalog/`](catalog/), [`src/`](src/) and [`docs/`](docs/). The original documentation repository is **intentionally still present** in `business/`, `technical/`, `milestones/` and older root indexes. Those documents remain input to the migration audit and **are not copied into generated projects**. Do not treat their project-specific claims as boilerplate.

Read the [baseline audit](docs/audit/2026-10-27.md), [architecture](docs/architecture/overview.md), [ADR](docs/decisions/0001-composable-profiles.md), [generator instructions](docs/generator.md), and [implementation status](docs/implementation-status.md).

## Quick start

Requires .NET SDK 10:

```sh
dotnet run --project src/DocsBoilerplate.Cli -- list
dotnet run --project src/DocsBoilerplate.Cli -- init --name Demo --description "Example project" --output ./scratch/demo --profiles dotnet,ef-core,blazor,mudblazor --non-interactive --dry-run
dotnet run --project src/DocsBoilerplate.Cli -- init --name Demo --description "Example project" --output ./scratch/demo --profiles dotnet,ef-core,blazor,mudblazor --non-interactive
dotnet run --project src/DocsBoilerplate.Cli -- validate --output ./scratch/demo
```

Run `dotnet build src/DocsBoilerplate.Cli` and `bash tests/smoke.sh` before relying on the generator. CI is defined at `.github/workflows/docs-generator.yml`.

## What it does

- Generates Markdown documentation from a minimal portable base.
- Includes only requested, available technology profiles and their prerequisites, with optional ASP.NET Core/MVC, PostgreSQL, FluentValidation, CQRS, MassTransit/RabbitMQ, Ardalis.Specification and bUnit standards.
- Lists planned, unavailable technologies explicitly.
- Supports interactive prompts, non-interactive flags, selectable documentation modules and optional Agent Skills.
- Offers a read-only dry run, preview-first updates with SHA-256 ownership checks, and refuses conflicting user edits.
- Validates generated-file presence, unresolved base fields and simple relative Markdown links.

## Current limitations

This repository is undergoing a staged migration. The legacy content still requires technical review. The first opt-in Zensical site is integrated. A full template library, comprehensive unit testing, advanced manifest constraints and automated upgrades are still pending. See [implementation status](docs/implementation-status.md).

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md) for the full development workflow and legacy preservation policy. Keep normative standards technology-scoped, project details in generated project documents, and agent workflows free of copied technical rules. New available profiles must include real content and tests.
