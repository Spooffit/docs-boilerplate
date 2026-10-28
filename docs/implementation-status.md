---
title: "Implementation status"
description: "Maintainer documentation"
kind: guide
status: active
---

# Implementation status

## Implemented

- A baseline structural audit and architecture ADRs.
- Optional language-neutral HTTP contract/consumer guidance and PostgreSQL migration review templates added, with profile composition tests.
- Reusable Markdown catalog and available/planned technology profiles, including opt-in ASP.NET Core, MVC, FluentValidation, PostgreSQL, CQRS, MassTransit/RabbitMQ, Ardalis.Specification, bUnit, accessible UI forms, language-neutral integration reliability, and object storage.
- A .NET 10 + Spectre.Console CLI with interactive prompts and non-interactive flags, packable as a local .NET Tool.
- Dependency resolution, profile availability checks, dry-run planning and existing-file protection; reject symlinks and invalid catalog/output paths in single-writer scenarios.
- Preview-first updates using original generated file fingerprints, preflight conflict detection, additive files and no automatic deletion.
- Optional documentation modules: glossary, requirements, API, integrations, security, testing, operations.
- Optional agent skills: docs-review, docs-maintain, docs-architecture, docs-init, docs-testing.
- A canonical `docs/` source tree, generated content/navigation catalog and Docsify (single HTML shell). Root `llms.txt` discovers installed optional project documents; `AGENTS.md` routes agents to approved decisions, contracts and observed source evidence on demand.
- A curated AI-first template index explaining when each document is useful, replacing the synthesized index at the same path without increasing the generated footprint.
- A CLI validator for generated-file manifests, unresolved initialization placeholders, ordinary local Markdown links **including same-page and cross-file heading fragments**, kebab-case filenames, YAML frontmatter, folder indexes and llms.txt references. Authored folder indexes can replace synthetic indexes, with source ownership collisions still rejected.
- Incoming legacy Markdown references and deleted-file links are checked with a shared tested parser in Linux CI.
- GitHub Actions build and end-to-end smoke suite (including Docsify-shell checks, local .NET Tool installation, Windows CLI smoke tests and reusable-catalog hygiene checks).
- A separate CI-only Playwright/Chromium smoke test of real Docsify rendering, nested links, deep routes, page reloads, heading fragments and optional module pages; browser traces/screenshots are captured on failure.
- Fresh generated-artifact scope tests across **all available profiles, modules and skills**; check exact owned files and fingerprints, no predecessor-product identifiers, no historical/maintainer source trees and no extraneous site assets. Negative contamination cases verify guard failures.

The generator's source is split by responsibility; see [the CLI code map](architecture/generator-components.md).

## Known limitations

- Every original inventory entry now has a structural disposition, but **full technical/semantic migration is not complete**. Seven placeholder files and nineteen individually reviewed legacy .NET/testing standards were removed under [documented individual cleanup decisions](audit/legacy-removals.md). All substantive historical feature, module, question and API specifications remain intact.
- Focused no-extra-package .NET contract tests now cover profile resolution, navigation, metadata, hashes, safe paths and symlinks on Linux and Windows. A full dedicated testing-framework suite, property-based checks and detailed diagnostics remain outstanding.
- Safe hash-based updates are implemented. Full three-way content merging is deliberately not attempted; concurrent author and template changes require manual resolution.
- Markdown and frontmatter parsing are deliberately scoped: the validator checks common ATX/Setext heading anchors and inline links, but not every Docsify-specific slug, reference-style link, image/link dialect, dynamic HTML anchor or arbitrary YAML syntax. Focused real Chromium navigation is automated separately; it does not prove these unsupported parser cases are universally valid.
- Catalog preflight detects unknown/cyclic dependencies and case-insensitive destination collisions. Formal JSON Schemas now specify the catalogs and generated configuration, with schema-level checks in CI; the CLI itself still uses the built-in validation and full hostile-concurrent-filesystem safety remains outstanding.
- No automated publication or browser-based editing workflow; Docsify is only a browser-side reader.
- The Docsify browser view depends on CDN availability and a local/static HTTP server. Essential Chromium navigation is tested; wider cross-browser coverage, search behavior, accessibility, visual regression and independently hosted deployment verification remain outstanding.

The [targeted versioning/lifecycle review](audit/versioned-lifecycle-review.md) preserves the reusable documentation concerns without imposing the old product's runtime behavior. An additional [F16 staff-access review](audit/staff-access-evidence-review.md) clarifies the optional security-architecture template, accepted versus observed authentication state, and missing source evidence without introducing source-specific identity defaults.

Unit, integration, functional, architecture, end-to-end and test-environment guidance has been consolidated after source-specific reviews; the S09–S15 predecessors were retired after incoming-link reconciliation. Historical S04 remains because its mandatory cross-suite naming policy requires an owner decision. The historical S02/S03 files have additionally been retired after extracting state-integrity, validation and error rules into the conditional base and technology-specific profiles; legacy library mandates await owner review. S18/S19/S20 have now also been retired after migrating optional FluentValidation/Ardalis.Specification and architecture-dependent CQRS rules; no mandatory libraries or handler folder layout were introduced. S22 is retired after extracting transport topology into its optional MassTransit/RabbitMQ profile, while S21 remains intact pending the owner decision about its extensive example. S17 has been consolidated into the portable data-flow standard, while S16 stays available for an explicit decision about its opinionated MVC/CQRS examples and concurrency policy.

## Next steps

1. Perform semantic reviews and recorded extraction/rejection decisions for structurally screened documents; see [Q15/Q20 evidence and feature-contract review](audit/q15-q20-evidence-review.md) and [F16 staff-access evidence review](audit/staff-access-evidence-review.md) for cross-source examples. Distinguish accepted designs from actual source/test evidence, especially when links resolve outside the repository.
2. Resolve the [five retained historical technology-standard choices](audit/retained-standards-closeout.md) with the owner after engineering review; do not promote their opinionated conventions or delete source examples before approval.
3. Add robust tests for file and symlink safety, profile composition and package distribution.
4. Extend the catalog only with tested and documented technologies.
5. Review and remove historical project content only after extraction or recorded rejection.

The baseline now has a path-level inventory at `docs/audit/legacy-inventory.csv` and grouped [project-source triage](audit/project-source-triage.md). All entries are at least structurally screened; targeted extraction and completed source reviews are separate states. CI checks that listed sources exist and that newly added historical documents are inventoried. This inventory is not proof of content extraction.

The S05/S07 UI and accessibility sources have been consolidated into the portable form guidance and retired after link reconciliation; S06/S08 remain in the historical tree pending owner decisions on NSwag, bUnit/FSD and strict test naming. There are **five** retained substantive legacy technology standards after this cleanup, reconciled in the [technical closeout](audit/retained-standards-closeout.md). The browser smoke suite verifies Docsify navigation separately; source migration alone does not verify predecessor application behavior.
