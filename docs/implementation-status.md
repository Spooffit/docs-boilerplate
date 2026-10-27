# Implementation status

## Implemented

- A baseline structural audit and architecture ADRs.
- Reusable Markdown catalog and available/planned technology profiles, including opt-in ASP.NET Core, MVC, FluentValidation, PostgreSQL, CQRS, MassTransit/RabbitMQ, Ardalis.Specification and bUnit.
- A .NET 10 + Spectre.Console CLI with interactive prompts and non-interactive flags, packable as a local .NET Tool.
- Dependency resolution, profile availability checks, dry-run planning and existing-file protection; reject symlinks and invalid catalog/output paths in single-writer scenarios.
- Optional documentation modules: glossary, requirements, API, integrations, security, testing, operations.
- Optional agent skills: docs-review, docs-maintain, docs-architecture, docs-init, docs-testing.
- A canonical `docs/` source tree and opt-in Zensical configuration.
- A CLI validator for generated-file manifests, unresolved initialization placeholders and basic local Markdown links.
- GitHub Actions build and end-to-end smoke suite (including optional site build and local .NET Tool installation).

## Known limitations

- Full historical documentation has **not** been migrated or technically audited file-by-file.
- Unit test suite, property-based checks and detailed diagnostics remain limited.
- No safe three-way update/merge process after initialization.
- Markdown parsing is intentionally simple; it does not fully verify Markdown anchors, image references or every link dialect.
- Schema/catalog validation needs more formal contracts and compatibility rules; TOCTOU races against concurrent hostile filesystem changes are not solved.
- No publication or browser-based editing workflow.
- Site tool remains an early-release Zensical dependency. Verify CI before claiming compatibility.

## Next steps

1. Continue detailed migration of architecture constraints, advanced integration contracts and historical feature templates.
2. Revisit unreviewed legacy engineering documents and decide whether to extract, refactor or drop each rule.
3. Add robust tests for file and symlink safety, profile composition and package distribution.
4. Extend the catalog only with tested and documented technologies.
5. Review and remove historical project content only after extraction or recorded rejection.

The baseline now has a path-level inventory at `docs/audit/legacy-inventory.csv`. Entries are unreviewed unless explicitly covered in a separate audit. This inventory is not proof of content extraction.
