---
title: "Implementation status"
description: "Implemented generator features, verification coverage and known limits"
kind: guide
status: active
---

# Implementation status

## Implemented

- A packaged, installable .NET 10 tool (`docs-boilerplate`) using Spectre.Console with a three-stage backend/frontend/infrastructure wizard and separate optional installation choices.
- A minimal Markdown-first base catalog with composable available/planned technology profiles and all project-document modules and persistent agent skills installed by default. S3-compatible infrastructure and MinIO/AWS .NET client choices are **draft placeholders**, not validated deployment configurations.
- Explicit dependency resolution, catalog path checks and refusal of conflicting user edits. Preview/dry-run is read-only; `update --apply` uses recorded SHA-256 generator-owned file fingerprints and preserves user-authored changes. Update preflight rejects case-only collisions with existing files and parent directories before writing.
- A temporary second-stage intake questionnaire and Agent Skill removed by an explicit, guarded `bootstrap-complete --apply` after owner approval.
- Generated `README.md`, `AGENTS.md`, `llms.txt`, navigable `docs/` and `docs.config.json` manifest. Every `docs/` page gets a stable unique ID; baseline integration reliability and all default modules are included even without an optional technology profile.
- Optional Docsify browser shell using a single `index.html`. Markdown remains source of truth and no static-site build tree is created.
- English and Russian localization of all 80 generated-documentation Markdown sources, including baseline pages, optional profiles, persistent agent skills and temporary stage-two intake. `init --language en|ru` stores the locale in `docs.config.json`; updates and intake cleanup retain that choice. Missing locale files fail closed, and CI contracts check source/translation parity, frontmatter and template markers.
- A CLI validator for ordinary relative Markdown links and common heading fragments, frontmatter, directory indexes, **catalog coverage for every project Markdown page, local index coverage for authored pages and parent-index coverage for newly authored directories**, managed paths and unresolved required placeholders.
- Cross-platform CI: Linux build, focused .NET contract tests, generation smoke and hygiene; Windows build and smoke; real Chromium/Playwright Docsify navigation checks.
- Repository and catalog hygiene checks for unwanted source trees, concrete non-template content, orphan links and unexpected generated assets.
- Initial project and architecture overviews remain `draft`, distinguish observed code from target decisions and prompt for evidence. Generated agent instructions distinguish metadata status from verified facts, refer to the source checkout for CLI operations, and allow manual review when the generator is unavailable. The optional `docs-init` skill explains first-pass project intake; cross-platform smoke and guidance checks verify onboarding content.

The owner-approved minimal document and example scope is recorded in [ADR 0005](decisions/0005-approved-template-scope.md). The generator's boundaries are documented in [the code map](architecture/generator-components.md). Selected profile configuration and operation examples are documented in [the generator guide](generator.md).

## Catalog coverage and remaining work

The currently usable catalog has **37 available technology/architecture profiles** and **five planned but non-selectable profiles**: `go`, `angular`, `react`, `blazorise` and `custom-ui`. The existing writing-aid library contains seven general-purpose document templates (feature, technical feature, module, question, ADR, standard and milestone) plus three optional technology-specific variants (.NET backend feature, Blazor frontend feature and PostgreSQL migration). All now emphasize minimal authoring, omission of unused sections, and evidence-grounded identifiers and dates rather than suggesting one inherited project workflow. These represent the **current implemented catalog**, not completion of every possible standard or template.

Every available profile maps to real catalog documents and participates in generation and validation tests. The Linux smoke suite also generates and validates **each available profile individually** in headless mode and rejects any selectable profile that depends on a planned profile before generation. Documentation modules and permanent agent skills are default selections; their presence does not imply a project implements the documented feature. Further technology standards are not silently declared complete: planned profiles need actual content and coverage before promotion. All three owner-review blocks are resolved ([ADR 0005](decisions/0005-approved-template-scope.md), [ADR 0006](decisions/0006-optional-technology-conventions.md), [ADR 0007](decisions/0007-final-editorial-policies.md)). Opt-in convention profiles cover test names, HTTP ETag/If-Match, MassTransit EventBus composition and direct use of API request DTOs in UI forms; document IDs and integration reliability are now baseline, while formal approval statuses are not generated. Base technology profiles never implicitly enable these specialized variants. The former project's specialized client-library and source-model tooling are outside the template's scope; the boilerplate does not ship an application, source-code generator or library distribution pipeline. Existing HTTP and value-compatibility rules remain applicable only to selected contracts.

## Known limitations

- Updates stage a temporary on-disk recovery journal with original and desired bytes before modifying project documents. Explicit `recover` repairs an interrupted partial update or finalizes an already completed one; incomplete staging and conflicting user changes fail closed. Ordinary write errors still trigger rollback. Focused tests cover forced process termination, restoration of prior bytes, new files and directories, and concurrent modifications. A newer independent edit is preserved rather than overwritten during rollback; crash-atomicity is not claimed.
- Retired profile documents remain on disk and are explicitly tracked as `RetiredFiles` in the generated manifest; they are excluded from current navigation without suppressing checks for genuinely new documents. The update mechanism deliberately does not perform three-way content merges, arbitrary profile-selection migrations or automatic deletion of retired user files. It assumes a single writer; concurrent hostile file-system changes and crash-atomic multi-file transactions are not guaranteed.
- Markdown parsing is deliberately scoped to common links/heading formats; not all Markdown extensions, reference-style/HTML links, dynamic Docsify anchors and YAML constructs are supported. Discoverability checks do not rewrite indexes or require every page to appear in the curated `llms.txt`.
- Docsify needs a local/static HTTP server and currently loads JavaScript and CSS from a CDN. Chromium smoke verifies selected navigation scenarios but not CDN uptime, all browsers, full accessibility, visual regression or real hosting integration.
- Focused .NET tests and broad CLI smoke do not replace a dedicated framework-level testing suite, property-based tests or deeper reliability testing.
- Planned technology profiles are discoverable but cannot be generated until they have supported standards and tests.
- Version `1.0.1` of the `docs-boilerplate` .NET tool was published successfully to NuGet.org via GitHub Trusted Publishing (OIDC), with a matching [GitHub Release](https://github.com/Spooffit/docs-boilerplate/releases/tag/v1.0.1) and successful Linux, Windows, browser and integration checks. The older `v1.0.0` tag was not published and remains unchanged. Future package publishing remains tag-only.

## Readiness boundary

This repository is usable as a **documentation generator**, not as a proven design for any particular application. Freshly generated pages are scaffolds until filled and reviewed against project evidence. Successful CI establishes the behavior tested by the .NET contract suite, CLI smoke tests, generated-output hygiene, Windows tests and Chromium navigation; it does not establish factual correctness of project documentation or production hosting support. Check the GitHub Actions result for the exact commit under review before claiming current verification.

Technical correctness of the generator and acceptance of editorial defaults are separate. The approved first-block defaults are recorded in [ADR 0005](decisions/0005-approved-template-scope.md). The [owner decision review](decisions/owner-decision-backlog.md) is closed. Future proposals still require explicit selection, project-independent justification and verification; closure never authorizes inherited business workflows.

## Maintenance and next steps

1. Keep the catalog reusable and free of project-specific feature documents; do not add example applications unless the owner explicitly selects one.
2. Improve generator update and input/path safety, test diagnostics, browser coverage and generated-template cleanliness.
3. Expand technology guidance only when profile selection, compatibility and verification are grounded. The optional EventBus technical example is compiled and broker-tested in Linux CI, not shipped as a sample application.
4. Keep the [resolved owner decision register](decisions/owner-decision-backlog.md) current and require fresh approval before adding specialized conventions.

