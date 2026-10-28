---
name: docs-init
description: Initialize Docs-as-Code projects using explicit profiles and evidence-first project intake.
---

# Initialize project documentation

1. Confirm the destination, name and factual summary. Inspect actual requirements, decisions, source code, schemas and available test/deployment evidence. Never invent project facts to fill a template.
2. Ask for an essential missing choice only when needed; mark unknown technology, architecture and product behavior as **Not yet verified**.
3. Run `docs-boilerplate list`, select only available applicable profiles, modules and skills. Profile installation does not prove a product uses that technology.
4. Preview `init --dry-run` and refuse collisions or overwrites. Run the reviewed `init`, then `docs-boilerplate validate --output <directory>`. Docsify adds one root `index.html`; `--site none` omits the viewer.
5. Read the generated `llms.txt`, `AGENTS.md`, `docs/index.md`, `docs/catalog.md` and `docs.config.json`. Verify installed files and dependencies match the confirmed selection.
6. **First-pass project intake:** fill `docs/project/overview.md` and `docs/architecture/overview.md` from evidence. Separate observed implementation, accepted target and open decisions. Keep both pages `draft` until reviewed; unavailable facts are not assumptions.
7. Create only feature, technical-feature, module, question, milestone or ADR documents actually required; select the smallest relevant template from `docs/templates/index.md`. Do not prepopulate empty document families.
8. Run `validate` after edits; inspect navigation, selected scope and file ownership. A valid document is not automatically an accepted design. Do not edit `docs.config.json` by hand.
9. Report files and profiles installed, real evidence inspected, remaining drafts/open choices and tests actually performed.

Use the generator's preview-first updater for later changes; do not duplicate its ownership or hashing logic in this skill.
