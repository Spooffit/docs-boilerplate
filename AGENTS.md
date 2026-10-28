---
title: "Instructions for agents"
description: "Maintaining the reusable documentation generator"
kind: guide
status: active
---

# Instructions for agents

This repository is a reusable Docs-as-Code generator, **not** a business application or an example project's documentation.

1. Read `llms.txt`, `README.md`, `docs/index.md` and `docs/architecture/overview.md` before editing.
2. Put portable templates and selected technology standards in `catalog/`, generator code in `src/`, tests in `tests/`, and maintainer documentation in `docs/`.
3. Never restore predecessor-project `business/`, `technical/`, `milestones/`, `shared/` or root F/M/Q indexes. They were removed intentionally. Git history is sufficient for source archaeology.
4. Treat catalog templates as neutral scaffolds, not project requirements. Never infer user choices, endpoints, roles, state machines, libraries, business rules or existing code from an illustrative example.
5. Keep profiles explicit, composable and tested. Do not silently add a broker, ORM, architecture style, UI framework or other dependency to a minimal generated project.
6. Protect user-authored files. Dry-run and validation must not write; updates require explicit confirmation and fingerprint checks.
7. Write maintainer and generated guidance in English, use relative Markdown links and valid frontmatter, and follow [documentation conventions](catalog/base/docs/standards/documentation.md).
8. Validate changes with `dotnet build src/DocsBoilerplate.Cli -c Release`, `bash tests/smoke.sh`, `python tests/catalog_hygiene.py` and `python tests/repository_hygiene.py` where applicable. Do not claim tests passed without evidence.
9. Keep [pending template decisions](docs/decisions/owner-decision-backlog.md) unapproved until the owner chooses an option. Do not turn a historical implementation pattern into a default profile by assumption.

Use [the docs-review skill](.agents/skills/docs-review/SKILL.md) for documentation review.
