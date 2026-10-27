---
title: "Documentation authoring conventions"
description: "Portable Markdown rules for human editors and AI agents"
kind: standard
status: active
---

# Documentation authoring conventions

**Applies to:** every generated documentation repository.

## Source of truth and discovery

- The **Markdown files**, not the website viewer, are the authoritative project documentation.
- Read the root `llms.txt` as a curated entry point and `AGENTS.md` for instructions. Neither replaces the referenced source documents or explicit project decisions.
- Every directory under `docs/` containing Markdown documents must have `index.md` summarizing its purpose and linking to its contents. Keep links relative to the containing document. The generator creates initial indexes; editors update them after creating, moving or deleting pages.
- Documentation files use lowercase **kebab-case** (e.g. `api-versioning.md`) and `index.md` for directory overviews. Established tool-specific entrypoints `README.md`, `AGENTS.md`, and `SKILL.md` are explicit exceptions; do not rename them.
- Do not invent a mandatory identifier scheme for features, ADRs or business requirements. Use existing project IDs where adopted; otherwise use descriptive kebab-case filenames.

## Frontmatter

Every regular Markdown page starts with a single YAML frontmatter block:

```yaml
---
title: "Example title"
description: "Short, factual summary"
kind: standard
status: active
---
```

Fields are required and have these meanings:

| Field | Meaning |
| --- | --- |
| `title` | Human-readable document name, consistent with the H1 |
| `description` | Short, factual orientation for search and AI agents |
| `kind` | `index`, `standard`, `template`, `project`, `guide`, `reference`, `adr` or `audit` |
| `status` | `active`, `draft`, `template` or `superseded` |

Do not add invented owner names, review dates, adoption status or unsupported technology version constraints. If ownership or dates become required, adopt a documented schema change and only record real values. Keep metadata concise.

`SKILL.md` is a **special case**: preserve its agent-skill specification's `name` and `description` frontmatter fields, which are not replaced by `title`/`kind`/`status`. Historical source documents still awaiting migration may remain in their old format until reviewed.

## Rules for linking

- Write relative paths, including the `.md` suffix, to existing documents.
- Update inbound references after moving a page.
- A document should link to the canonical standard, contract, ADR or feature rather than copy it.
- Technical standards describe verifiable rules and must state their applicability. Optional libraries require selected technology profiles.
- A template is guidance, not confirmation that a design has been approved or built.

## Viewer and local preview

Docsify is an optional browser-side reader, not a required build tool. Its `index.html` is deliberately separate from `docs/index.md`. It usually needs an HTTP server for local viewing; do not assume opening a `file://` page works.

## Checks

Run the generator's `validate` command. It verifies metadata, filenames, basic relative links, generated-file declarations and directory indexes. Markdown anchor semantics and all possible YAML expressions are **not** comprehensively parsed in this first implementation.

See the community [llms.txt proposal](https://llmstxt.org/) and [Docsify](https://docsify.js.org/) for their separate roles.
