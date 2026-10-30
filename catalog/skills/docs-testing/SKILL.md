---
name: docs-testing
description: Review testing strategy and evidence against the actual system under test without imposing unselected tooling.
---

# Review test strategy

1. Read `llms.txt`, `docs.config.json`, the project test strategy where selected and the applicable technology standards.
2. Identify the actual system under test for every suite: isolated rule, infrastructure interaction, public application behavior, UI component, integration contract or deployed journey.
3. Confirm asserted outcomes, failure paths, fixture lifecycle, isolation, retry/timeout behavior and CI evidence from actual code and jobs.
4. Treat TUnit, Testcontainers, bUnit, Aspire Testing and specific runner platforms as independently selected options. Never claim compatibility without executing the relevant toolchain.
5. Update `docs/testing/strategy.md` **only when present and relevant**; the testing scaffold is installed by default but does not prove tests exist or require a new testing document. When a new page is justified, use YAML frontmatter, kebab-case naming and links from the immediate `index.md` and `docs/catalog.md`.
6. Distinguish passing evidence from proposed tests; coverage percentage alone does not establish behavior quality.
7. For documentation validation, use `dotnet run --project src/DocsBoilerplate.Cli -- validate --output <repository-root>` only **from an available generator source checkout** with .NET 10 SDK; otherwise review manually and report the limitation. Run actual project test commands only when they exist and are appropriate to the selected technology. Report observable evidence, not invented passing checks.
