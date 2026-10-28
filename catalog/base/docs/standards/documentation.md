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
- Read the root `llms.txt` as a curated entry point and `AGENTS.md` for instructions. Neither replaces the referenced source documents or explicit project decisions. `docs/catalog.md` is the complete project-doc discovery map, including pages authored after initialization. `llms.txt` remains intentionally short and should link to the catalog; it need not repeat every new document. Initial generated indexes list installed documents, not pending or unselected technologies; navigation is not confirmation of requirements.
- Every directory under `docs/` containing Markdown documents must have `index.md` summarizing its purpose and linking to its contents. Keep links relative to the containing document. When adding a new Markdown page, link it from its immediate directory's `index.md` and from `docs/catalog.md`. Link each newly authored nested directory's `index.md` from its parent directory index as well. When moving/renaming pages or directories, repair affected indexes, catalog entries and inbound links. The generator creates initial indexes; it does **not** overwrite author edits to fix navigation.
- Documentation files use lowercase **kebab-case** (e.g. `api-versioning.md`) and `index.md` for directory overviews. Established tool-specific entrypoints `README.md`, `AGENTS.md`, `SKILL.md` and `CONTRIBUTING.md` are explicit well-known exceptions; do not rename them.
- Do not invent a mandatory identifier scheme for features, ADRs or business requirements. Use existing project IDs where adopted; otherwise use descriptive kebab-case filenames. The optional document-identifier and approval policy is installed only if its profile is selected.

## Frontmatter

Every regular Markdown page under `docs/` starts with a single YAML frontmatter block. The **root `README.md` is exempt**: it must start with its `#` heading because GitHub renders YAML frontmatter at the top of a README as an unwanted metadata table:

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

`SKILL.md` is a **special case**: preserve its agent-skill specification's `name` and `description` frontmatter fields, which are not replaced by `title`/`kind`/`status`. Newly generated project and architecture overviews are intentionally `draft` until verified. Passing generator validation proves structure, not factual or owner approval.

## Rules for linking

- Write relative paths, including the `.md` suffix, to existing documents.
- When linking to a section, use a matching Markdown fragment, e.g. `[scope](feature.md#scope)` or `[checks](#checks)`. Prefer short, stable heading names; duplicate headings acquire numeric suffixes such as `#setup-1`. Use percent-encoding for non-ASCII URL fragments where required.
- The CLI checks ordinary inline links, local fragment targets, and common ATX/Setext heading slugs while ignoring fenced code samples; it does **not** implement every CommonMark or Docsify anchor rule. Avoid relying on renderer-specific heading syntax without verifying the actual rendered page.
- Update inbound references after moving a page.
- A document should link to the canonical standard, contract, ADR or feature rather than copy it.
- Technical standards describe verifiable rules and must state their applicability. Optional libraries require selected technology profiles.
- A template is guidance, not confirmation that a design has been approved or built. The installed technology profile list is also not approval of any business requirement.
- For **accepted decisions**, use the canonical ADR/decision and requirements; for an external wire format use the adopted schema and deployed contract; for observed implementation verify source code and actual tests. If sources disagree, identify the conflict and the responsible owner instead of silently picking the most recent-looking document.
- Read only documents relevant to the current task. Use the generated catalog and template selection guide to navigate, not as a reason to expand every document category.

## Code examples and verification evidence

- Treat source examples as **illustrative** unless the exact snippet or a faithful complete version has been compiled, executed or contract-tested using an explicitly identified compatible toolchain. A syntactically plausible code block is not proof that it runs.
- When an example is intentionally incomplete (pseudocode, placeholder type, omitted imports, omitted surrounding application or elided assertions), **label it illustrative or partial** next to the example. Never present `...` or comments standing in for executable behavior as verified code.
- A claim of an **executable or verified** example needs a concrete build/test command, tool and dependency versions as applicable, and retrievable evidence from an actual successful run. Link the source of truth for test fixtures rather than duplicating a second example that can drift.
- Do **not** require a dedicated application, package or CI job for every small documentation snippet. Prefer focused real tests for examples that materially guide integration, compatibility, security or public behavior. If no verification ran, report it as *not verified*, not as a passing example.
- Build success by itself does not establish integration, runtime behavior, authorization, reliability or API compatibility. Test the claim at the appropriate boundary.

## Viewer and local preview

Docsify is an optional browser-side reader, not a required build tool. Its `index.html` is deliberately separate from `docs/index.md`. It usually needs an HTTP server for local viewing; do not assume opening a `file://` page works.

## Checks

If the boilerplate source checkout and .NET 10 SDK are available, run `dotnet run --project src/DocsBoilerplate.Cli -- validate --output <repository-root>` **from that checkout**. The generated documentation repository does not install or contain the CLI. Without the generator, check links, navigation, metadata and source evidence manually and report which automated checks were unavailable.

The validator verifies metadata, filenames, ordinary inline relative Markdown links **including common section anchors**, generated-file declarations, directory index presence, catalog discovery of every document under `docs/`, immediate-index discovery of additional author-created pages, and `llms.txt` paths/fragments. Reference-style links, dynamically generated links, advanced Markdown/HTML rendering and arbitrary YAML expressions are **not** comprehensively parsed. Verify critical cross-page navigation in the configured Docsify browser view; a passing source-level check cannot prove that every client-side renderer chooses the same slug.

See the community [llms.txt proposal](https://llmstxt.org/) and [Docsify](https://docsify.js.org/) for their separate roles.
