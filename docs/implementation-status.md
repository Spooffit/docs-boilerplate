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
- GitHub Actions build and end-to-end smoke suite (including Docsify-shell checks and local .NET Tool installation).

## Known limitations

- Full historical documentation has **not** been migrated or technically audited file-by-file.
- Unit test suite, property-based checks and detailed diagnostics remain limited.
- Safe hash-based updates are implemented. Full three-way content merging is deliberately not attempted; concurrent author and template changes require manual resolution.
- Markdown and frontmatter parsing are deliberately scoped; they do not fully verify Markdown anchors, every image/link dialect or arbitrary YAML syntax.
- Catalog preflight detects unknown or cyclic dependencies, case-insensitive destination collisions and reserved output paths. A formal JSON Schema and protection against concurrent hostile filesystem changes are still outstanding.
- No automated publication or browser-based editing workflow; Docsify is only a browser-side reader.
- The Docsify browser view depends on CDN availability and a local/static HTTP server; full headless-browser interaction tests remain outstanding.

## Next steps

1. Review remaining legacy testing, module/question templates and historical feature documents for useful reusable guidance.
2. Revisit unreviewed legacy engineering documents and decide whether to extract, refactor or drop each rule.
3. Add robust tests for file and symlink safety, profile composition and package distribution.
4. Extend the catalog only with tested and documented technologies.
5. Review and remove historical project content only after extraction or recorded rejection.

The baseline now has a path-level inventory at `docs/audit/legacy-inventory.csv`. Entries are unreviewed unless explicitly covered in a separate audit. This inventory is not proof of content extraction.
