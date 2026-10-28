---
title: "Implementation status"
description: "Implemented generator features, verification coverage and known limits"
kind: guide
status: active
---

# Implementation status

## Implemented

- A .NET 10 / Spectre.Console CLI with interactive and non-interactive `init`, `list`, `doctor`, `validate` and fingerprint-based `update` commands.
- A minimal Markdown-first base catalog with composable available/planned technology profiles, optional project-document modules and optional agent skills.
- Explicit dependency resolution, catalog path checks and refusal of conflicting user edits. Preview/dry-run is read-only; `update --apply` uses recorded SHA-256 generator-owned file fingerprints and preserves user-authored changes. Update preflight rejects case-only collisions with existing files and parent directories before writing.
- Generated `README.md`, `AGENTS.md`, `llms.txt`, navigable `docs/` and `docs.config.json` manifest. Generated output includes only selected profiles and optional modules.
- Optional Docsify browser shell using a single `index.html`. Markdown remains source of truth and no static-site build tree is created.
- A CLI validator for ordinary relative Markdown links and common heading fragments, frontmatter, directory indexes, **catalog coverage for every project Markdown page and local index coverage for newly authored pages**, managed paths and unresolved required placeholders.
- Cross-platform CI: Linux build, focused .NET contract tests, generation smoke and hygiene; Windows build and smoke; real Chromium/Playwright Docsify navigation checks.
- Repository and catalog hygiene checks for unwanted source trees, concrete non-template content, orphan links and unexpected generated assets.
- Initial project and architecture overviews remain `draft`, distinguish observed code from target decisions and prompt for evidence. The optional `docs-init` skill explains first-pass project intake; smoke tests verify generated onboarding content.

The generator's boundaries are documented in [the code map](architecture/generator-components.md). Selected profile configuration and operation examples are documented in [the generator guide](generator.md).

## Catalog coverage and remaining work

The currently usable catalog has **22 available technology/architecture profiles** and **five planned but non-selectable profiles**: `go`, `angular`, `react`, `blazorise` and `custom-ui`. The existing writing-aid library contains seven general-purpose document templates (feature, technical feature, module, question, ADR, standard and milestone) plus three optional technology-specific variants (.NET backend feature, Blazor frontend feature and PostgreSQL migration). These represent the **current implemented catalog**, not completion of every possible standard or template.

Every available profile maps to real catalog documents and participates in generation and validation tests. Further technology standards are not silently declared complete: planned profiles need actual content and coverage before promotion. Editorial/default choices remain pending in the [owner decision backlog](decisions/owner-decision-backlog.md), and no project-specific requirements are assumed. Conditional activity-history guidance now distinguishes committed source facts, separately delivered history, privacy, permission-filtered reads and delayed projections; it does not create an activity subsystem or prescribe a product-specific API. Cross-platform SDK and schema-derived source guidance similarly remains conditional: the boilerplate does not ship an SDK, source generator, binary cache or distribution pipeline.

## Known limitations

- Updates stage a temporary on-disk recovery journal with original and desired bytes before modifying project documents. Explicit `recover` repairs an interrupted partial update or finalizes an already completed one; incomplete staging and conflicting user changes fail closed. Ordinary write errors still trigger rollback. Focused tests cover forced process termination, restoration of prior bytes, new files and directories, and concurrent modifications. A newer independent edit is preserved rather than overwritten during rollback; crash-atomicity is not claimed.
- Retired profile documents remain on disk and are explicitly tracked as `RetiredFiles` in the generated manifest; they are excluded from current navigation without suppressing checks for genuinely new documents. The update mechanism deliberately does not perform three-way content merges, arbitrary profile-selection migrations or automatic deletion of retired user files. It assumes a single writer; concurrent hostile file-system changes and crash-atomic multi-file transactions are not guaranteed.
- Markdown parsing is deliberately scoped to common links/heading formats; not all Markdown extensions, reference-style/HTML links, dynamic Docsify anchors and YAML constructs are supported. Discoverability checks do not rewrite indexes or require every page to appear in the curated `llms.txt`.
- Docsify needs a local/static HTTP server and currently loads JavaScript and CSS from a CDN. Chromium smoke verifies selected navigation scenarios but not CDN uptime, all browsers, full accessibility, visual regression or real hosting integration.
- Focused .NET tests and broad CLI smoke do not replace a dedicated framework-level testing suite, property-based tests or deeper reliability testing.
- Planned technology profiles are discoverable but cannot be generated until they have supported standards and tests.

## Maintenance and next steps

1. Keep the catalog reusable and free of project-specific feature documents; do not add example applications unless the owner explicitly selects one.
2. Improve generator update and input/path safety, test diagnostics, browser coverage and generated-template cleanliness.
3. Expand technology guidance only when profile selection, compatibility and verification are grounded.
4. Resolve [deferred owner choices](decisions/owner-decision-backlog.md) before adding opinionated defaults or specialized example profiles.

