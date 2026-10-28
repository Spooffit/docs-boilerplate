---
name: docs-maintain
description: Update canonical project documentation while preserving agent discovery, Markdown conventions, and authored decisions.
---

# Maintain documentation

1. Start at `llms.txt` and `docs/index.md`, then read the affected canonical standard, feature, ADR or project specification.
2. Establish whether the change is a verified requirement, implementation observation, accepted decision, or merely a proposal. Do not promote a proposal to an approved decision.
3. Make the smallest edit to the authoritative document. Add relative links elsewhere rather than duplicating rules or external contract definitions.
4. When adding a document under `docs/`, use valid YAML frontmatter and a descriptive kebab-case name. Add it to the **immediate directory's** `index.md` and to `docs/catalog.md` using relative links. A new subdirectory also needs its own `index.md`. The catalog is the full discoverability map; `llms.txt` stays a **curated entry point**, not a mirror of every file. Add an `llms.txt` link only when the new page is important enough to be an entry point.
5. When renaming or moving a page, search all inbound Markdown references, rewrite those links (and fragments when headings change), and update its old/new directory indexes and `docs/catalog.md`. Remove obsolete catalog entries. Never silently replace a historical document with a different page just to satisfy a link.
6. Run `docs-boilerplate validate --output <repository-root>`. It checks missing targets and heading fragments, catalog coverage of all `docs/*.md` pages, and parent-index coverage of newly authored pages. It **does not** automatically repair navigation, parse all Markdown link dialects, or prove that a document is factually correct. Inspect the Git diff and actual Docsify navigation for important changes.
7. Use `docs-boilerplate update` to preview upstream template changes; `--apply` only after reviewing the plan. If an unfinished update is reported, inspect the working tree and run `docs-boilerplate recover --output <repository-root>` before another update or validation. Never delete the temporary recovery journal to bypass conflicting user edits. The originally generated catalog and folder indexes are initially fingerprint-owned. Editing them is legitimate, but an upstream change to the same file will require manual reconciliation: do not bypass a conflict or regenerate indexes by overwriting edits.
8. Report files changed, link/navigation repairs, tests that actually ran and remaining uncertainties.

Skills guide the editing process; technical standards stay in the selected `docs/standards/` files.
