---
name: docs-maintain
description: Update canonical project documentation while preserving agent discovery, Markdown conventions, and authored decisions.
---

# Maintain documentation

1. Start at `llms.txt` and `docs/index.md`, then read the affected canonical standard, feature, ADR or project specification.
2. Establish whether the change is a verified requirement, implementation observation, accepted decision, or merely a proposal. Do not promote a proposal to an approved decision.
3. Make the smallest edit to the authoritative document. Add relative links elsewhere rather than duplicating rules or external contract definitions.
4. For new documents, add valid YAML frontmatter, a descriptive kebab-case filename and the relevant parent `index.md` entry. Keep `docs/catalog.md` and `llms.txt` useful when the navigation of core documents changes; do not treat them as competing authorities.
5. When renaming/moving files, repair inbound Markdown references, directory indexes, and document summaries. Do not rename special tool-recognized entrypoint files.
6. Use `docs-boilerplate update` to preview upstream template changes; `--apply` only after reviewing the plan. A conflict requires manual resolution, never unconditional overwrite.
7. Run `validate`, inspect the diff, and report which requirements/decisions and checks were actually verified.

Skills guide the editing process; technical standards stay in the selected `docs/standards/` files.
