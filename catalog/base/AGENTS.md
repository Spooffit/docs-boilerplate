---
title: "Agent instructions"
description: "Read-on-demand navigation and source-of-truth boundaries for project documentation"
kind: guide
status: active
---

# Agent instructions

Treat this repository's **Markdown documents as sources**, not the website or generated navigation as authority. Start with [llms.txt](llms.txt) to find relevant documents and [documentation home](docs/index.md) for the project map. Read **only the applicable sources** before changing a claim; do not load every profile, example or template by default.

## Find the authoritative source

1. Read [project overview](docs/project/overview.md) for product context and [architecture overview](docs/architecture/overview.md) for recorded system boundaries. Both are *draft scaffolds* until filled with real evidence.
2. Use [the document catalog](docs/catalog.md) to locate installed standards and optional project areas. The [docs.config.json](docs.config.json) selection tells you which technology profiles, skills and modules were installed. **Selection is not approval of business decisions or proof of implementation.**
3. For an actual behavior or requirement, follow its owning feature/requirement and approved decisions; for an architecture choice, follow its accepted ADR; for a public API or message format, inspect the **adopted** schema/contract; for observed implementation, inspect the relevant code and executed tests. Never promote a proposed template, example or plan into a verified fact.
4. Read [applicable standards](docs/standards/index.md) only for the selected technology and problem. Do not import rules from an unselected library, historical example or unrelated project.
5. To create a document, use [the template selection guide](docs/templates/index.md) and prefer the smallest appropriate artifact. Do not create duplicate Feature, Technical Feature, Module, ADR or Question records without a real distinct owner or scope.

## First-pass project intake

Newly initialized `docs/project/overview.md` and `docs/architecture/overview.md` are **draft scaffolds**, not project facts. Establish the problem, relevant sources, implementation observations and accepted architecture before promoting them to `active`. Mark unknowns as unverified and distinguish a source file from proof a requirement or test passed. Markdown validation checks structure, not factual approval.

Use the optional `docs-init` skill when installed. Do not create feature/module/question/milestone documents solely because templates exist.

## Change and verify

- Record unknowns explicitly. Preserve accepted decisions, original source links and meaningful status; do not invent requirements, approvals, owners, endpoints or successful tests.
- Link to canonical material instead of copying normative rules. A new page under `docs/` needs a link in its immediate `index.md` and in `docs/catalog.md`. After a move or rename, repair **incoming links**, both affected directory indexes, catalog entries and linked heading fragments. `llms.txt` remains a concise route to the catalog; do not duplicate the full file list there. These navigation files may be authored, so never overwrite them with automatic regeneration.
- Use the [documentation authoring conventions](docs/standards/documentation.md): frontmatter on Markdown pages, lowercase kebab-case filenames, relative links and an `index.md` in every documentation directory. Keep recognized `README.md`, `AGENTS.md`, `SKILL.md` and `CONTRIBUTING.md` names.
- If `update` or `validate` reports an interrupted update, inspect the file changes and run `docs-boilerplate recover --output <repository-root>`. If recovery refuses due to unexpected edits or incomplete staging, preserve the temporary journal and resolve the conflict deliberately; never discard its snapshots or override user work to force a successful check.
- Run `docs-boilerplate validate --output <repository-root>` when the CLI is available, and report separately what the validator did **not** establish (for example, factual correctness, approval, complete Markdown anchor validity or browser behavior).
- Optional agent skills under `.agents/skills/` describe focused workflows. They do **not** override project decisions or selected standards.
