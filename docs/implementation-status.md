---
title: "Implementation status"
description: "Maintainer documentation"
kind: guide
status: active
---

# Implementation status

## Implemented

- A baseline structural audit and architecture ADRs.
- Reusable Markdown catalog and available/planned technology profiles, including opt-in ASP.NET Core, MVC, FluentValidation, PostgreSQL, CQRS, MassTransit/RabbitMQ, Ardalis.Specification, bUnit and accessible UI forms.
- A .NET 10 + Spectre.Console CLI with interactive prompts and non-interactive flags, packable as a local .NET Tool.
- Dependency resolution, profile availability checks, dry-run planning and existing-file protection; reject symlinks and invalid catalog/output paths in single-writer scenarios.
- Preview-first updates using original generated file fingerprints, preflight conflict detection, additive files and no automatic deletion.
- Optional documentation modules: glossary, requirements, API, integrations, security, testing, operations.
- Optional agent skills: docs-review, docs-maintain, docs-architecture, docs-init, docs-testing.
- A canonical `docs/` source tree, generated content/navigation catalog and Docsify (single HTML shell).
- A CLI validator for generated-file manifests, unresolved initialization placeholders, basic local Markdown links, kebab-case filenames, YAML frontmatter, folder indexes and llms.txt references.
- GitHub Actions build and end-to-end smoke suite (including Docsify-shell checks, local .NET Tool installation, Windows CLI smoke tests and reusable-catalog hygiene checks).

The generator's source is split by responsibility; see [the CLI code map](architecture/generator-components.md).

## Known limitations

- Every original inventory entry now has a structural disposition, but **full technical/semantic migration is not complete**. Only seven empty/placeholder files have been removed under a separate [documented cleanup](audit/legacy-removals.md); substantive historical specifications remain intact.
- Focused no-extra-package .NET contract tests now cover profile resolution, navigation, metadata, hashes, safe paths and symlinks on Linux and Windows. A full dedicated testing-framework suite, property-based checks and detailed diagnostics remain outstanding.
- Safe hash-based updates are implemented. Full three-way content merging is deliberately not attempted; concurrent author and template changes require manual resolution.
- Markdown and frontmatter parsing are deliberately scoped; they do not fully verify Markdown anchors, every image/link dialect or arbitrary YAML syntax.
- Catalog preflight detects unknown/cyclic dependencies and case-insensitive destination collisions. Formal JSON Schemas now specify the catalogs and generated configuration, with schema-level checks in CI; the CLI itself still uses the built-in validation and full hostile-concurrent-filesystem safety remains outstanding.
- No automated publication or browser-based editing workflow; Docsify is only a browser-side reader.
- The Docsify browser view depends on CDN availability and a local/static HTTP server; full headless-browser interaction tests remain outstanding.

The [targeted versioning/lifecycle review](audit/versioned-lifecycle-review.md) preserves the reusable documentation concerns without imposing the old product's runtime behavior.

## Next steps

1. Perform semantic reviews and recorded extraction/rejection decisions for structurally screened documents; distinguish accepted designs from actual source/test evidence.
2. Revisit unreviewed legacy engineering documents and decide whether to extract, refactor or drop each rule.
3. Add robust tests for file and symlink safety, profile composition and package distribution.
4. Extend the catalog only with tested and documented technologies.
5. Review and remove historical project content only after extraction or recorded rejection.

The baseline now has a path-level inventory at `docs/audit/legacy-inventory.csv` and grouped [project-source triage](audit/project-source-triage.md). All entries are at least structurally screened; targeted extraction and completed source reviews are separate states. CI checks that listed sources exist and that newly added historical documents are inventoried. This inventory is not proof of content extraction.
