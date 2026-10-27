---
name: docs-init
description: Initialize and verify an agent-first Docs-as-Code project using explicit profiles, skills, and documentation modules.
---

# Initialize project documentation

1. Confirm the destination, project name, concise factual summary, and actual technologies. An unknown architecture or tool is an unresolved choice, not a default.
2. Run `docs-boilerplate list` and separate available profiles from planned options. Choose only implemented, applicable profiles and optional document modules and skills.
3. Preview `init --dry-run` before making files. Refuse collisions or attempted overwrites; never force a migration.
4. Run `init` with the reviewed flags and then `validate --output <directory>`. A default site selection creates one Docsify `index.html`; `--site none` creates no viewer. Neither adds site build artifacts.
5. Read `llms.txt`, `AGENTS.md`, `docs/index.md`, the generated `docs/catalog.md` and `docs.config.json`. Verify every selected technology is represented and no unsupported profile was silently added.
6. Fill in project-specific facts from actual sources. Preserve the YAML frontmatter on regular Markdown pages, kebab-case filenames and `index.md` for each documentation directory. Avoid adding unverified features, APIs, dependencies or approvals.
7. Report generated files, selected profiles, validation results, and unresolved business/architecture decisions.

The generator owns initial file planning and ownership hashes. Do not copy its logic into this skill or edit `docs.config.json` by hand.
