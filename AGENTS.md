---
title: "Instructions for agents"
description: "Repository documentation and guidance"
kind: guide
status: active
---

# Instructions for agents

This repository is a **generator under migration**. Do not confuse historical source documents with the new canonical content.

- Read `llms.txt`, `README.md`, `docs/index.md`, `docs/architecture/overview.md` and `docs/implementation-status.md` first.
- New portable templates and standards belong under `catalog/`; generator implementation belongs under `src/`.
- `business/`, `technical/`, `milestones/`, `features.md`, `modules.md`, `questions.md` and `standards.md` are source material from the predecessor project. Preserve them pending file-by-file migration review.
- Do not copy predecessor-specific business rules, architecture, package names or APIs into portable standards.
- Never fabricate project decisions, compatibility, test outcomes or supported profile status.
- New profiles require an explicit available/planned status, dependency checks, manifest entries and smoke tests.
- After changes, build the CLI and run `bash tests/smoke.sh` in an environment with .NET SDK 10.
- Keep documentation in English, with relative links, YAML frontmatter and lowercase kebab-case names (except tool-recognized `README.md`, `AGENTS.md`, `SKILL.md` and `CONTRIBUTING.md`). Provide an `index.md` in every active documentation directory, except special format directories. See `catalog/base/docs/standards/documentation.md`. Do not silently overwrite existing generated project files.
- The agent may use `.agents/skills/docs-review/SKILL.md` to review documentation changes.
