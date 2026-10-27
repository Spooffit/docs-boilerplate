---
title: "Contributing to Docs Boilerplate"
description: "Repository documentation and guidance"
kind: guide
status: active
---

# Contributing to Docs Boilerplate

This is a generator repository under staged migration. The reusable source lives in `catalog/`, the .NET CLI in `src/`, and test scripts in `tests/`.

## Change workflow

1. Check [implementation status](docs/implementation-status.md) and the accepted [architecture ADR](docs/decisions/0001-composable-profiles.md).
2. Identify what is canonical: `catalog/base/` for always-generated docs, `catalog/docs/standards/` for selectable technical standards, `catalog/modules/` for additional project documents, and `catalog/skills/` for optional agent procedures.
3. Modify the smallest appropriate source. CI also checks that reusable catalog files contain no predecessor-product identifiers (`tests/catalog_hygiene.py`). Do not copy project-specific requirements into standards or tool-dependent procedures into AGENTS.md.
4. Consult the [CLI code map](docs/architecture/generator-components.md) before changing generator behavior. Add or update catalog manifest entries for selectable content. All available profiles must have actual reusable files and clear dependencies.
5. Update tests for new profile combinations, negative cases, missing links and idempotency.
6. Run `dotnet build src/DocsBoilerplate.Cli -c Release` and `bash tests/smoke.sh`. For site changes, verify that Docsify's root index.html and Markdown routes work through a local HTTP server; the CI smoke test checks HTTP delivery.
7. Inspect the generated repository, not merely its individual templates. Never claim a check that has not been executed.
8. Commit a logically complete change using Conventional Commits.

## Documentation ownership

- **Standard:** reusable, technology-scoped normative rule in `catalog/docs/standards/`.
- **Template:** adaptable structure that does not assert project facts.
- **Documentation module:** optional project-specific document initialized with editorial guidance.
- **ADR:** reason for a technical decision, including alternatives and consequences.
- **Agent Skill:** a reproducible workflow referencing standards instead of copying them.
- **Project configuration:** explicit selected profiles, modules, skills, site and owned output file list.

Make normative requirements verifiable and explain their applicability. Do not require libraries or architecture styles solely because they exist in the catalog.

## Compatibility and safety

- Do not silently replace existing user-authored files.
- Keep template output deterministic. A dry-run must remain read-only.
- Treat untrusted paths, symlinks, file collisions and malformed manifest data as errors.
- Keep every generated internal Markdown link valid in the smallest profile combination.
- Keep optional dependencies optional; creating a Markdown-only repository must not require a site builder, Docker, AI or an interactive terminal.
- Keep platform-specific guidance gated by profile selection.
- No static-site output is generated. Keep test artifacts and experimental build outputs out of Git.

## Legacy source material

The repository originated from a different documentation project. Its old `business/`, `technical/`, `milestones/` and root catalogs are **source material**, not a normative part of the generator. See [legacy inventory](docs/audit/legacy-inventory.csv), [baseline audit](docs/audit/2026-10-27.md) and [testing review](docs/audit/testing-review.md).

Review [the project-source triage](docs/audit/project-source-triage.md) before removing any historical file. Every new historical file must be inventoried; CI checks source preservation and classifications.

The original contributing guide is preserved under `docs/legacy/CONTRIBUTING.md`. Preserve other legacy files until their content has been reviewed and decisions to extract, generalize or discard are recorded. Remove only after that review, never as indiscriminate cleanup.

## Pull requests and direct commits

Work may be committed directly to `main` where authorized, but all published commits must preserve history, run relevant checks and clearly distinguish implemented behavior from roadmap items.
