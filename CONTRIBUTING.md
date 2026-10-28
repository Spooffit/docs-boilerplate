---
title: "Contributing to Docs Boilerplate"
description: "Maintenance workflow for the reusable repository generator"
kind: guide
status: active
---

# Contributing to Docs Boilerplate

This repository maintains a reusable Markdown-first documentation generator. The canonical catalog is `catalog/`, the CLI is `src/`, and tests are `tests/`. Do not add a project's real feature specifications to this repository.

## Change workflow

1. Read [implementation status](docs/implementation-status.md), the [architecture overview](docs/architecture/overview.md), and applicable [architectural decisions](docs/decisions/index.md).
2. Select the smallest owning source: `catalog/base/` for common generated documentation, `catalog/docs/` for opt-in technical profiles, `catalog/modules/` for optional project documents, and `catalog/skills/` for procedures.
3. Keep standards technology-scoped and templates free of product-specific entities, routes, roles and business rules. Use conditional examples rather than inventing requirements.
4. Modify the associated profile/module/skill manifest when adding selectable content; verify that dependencies and supported status are correct.
5. Add or adjust focused .NET contract tests, schema assertions, smoke cases and hygiene checks when behavior changes.
6. Run `dotnet build src/DocsBoilerplate.Cli -c Release`, `dotnet run -c Release --project tests/DocsBoilerplate.ContractTests`, `bash tests/smoke.sh`, `python tests/catalog_hygiene.py`, and `python tests/repository_hygiene.py`. Consult CI for Windows and Chromium results.
7. Inspect a newly generated output, including its `llms.txt`, navigation, ownership manifest, internal links and exact file set. Report only checks that actually ran.
8. Commit cohesive changes with conventional commit messages. Direct commits to `main` are permitted when authorized.

## Documentation ownership

- **Standard:** enforceable engineering rule, applicable only when the relevant technology/profile is selected.
- **Template:** a structure for documenting a real project's verified behavior; placeholders are not facts.
- **Optional module:** project-specific document scaffold installed only when requested.
- **ADR:** an accepted decision with reasoning and consequences, not a list of speculative alternatives.
- **Agent Skill:** a repeatable workflow that references canonical documents instead of copying standards.
- **Project config:** selected profiles, modules, skills, site and generator-owned output paths.

Write one authoritative definition for each rule. Distinguish accepted design, observed implementation, and tested behavior. Do not silently overwrite generated project files, follow untrusted paths, select unapproved profiles or generate static-site artifacts.

## Repository cleanliness

The active tree must not contain old project `business/`, `technical/`, `milestones/`, `shared/` folders, root F/M/Q catalogs or migration-only audits. Those materials were extraction inputs, not a supported example application; prior commits remain available in Git history. `tests/repository_hygiene.py` protects this boundary.

Product-specific examples require an explicit owner decision and a clearly separate, opt-in sample with usable verification; do not re-create a legacy source archive inside this boilerplate. Unresolved choices are recorded in the [owner decision backlog](docs/decisions/owner-decision-backlog.md).
