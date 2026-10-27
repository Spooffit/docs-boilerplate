---
name: docs-testing
description: Review testing strategy and evidence against the actual system under test without imposing unselected tooling.
---

# Review test strategy

1. Read `llms.txt`, `docs.config.json`, the project test strategy where selected and the applicable technology standards.
2. Identify the actual system under test for every suite: isolated rule, infrastructure interaction, public application behavior, UI component, integration contract or deployed journey.
3. Confirm asserted outcomes, failure paths, fixture lifecycle, isolation, retry/timeout behavior and CI evidence from actual code and jobs.
4. Treat TUnit, Testcontainers, bUnit, Aspire Testing and specific runner platforms as independently selected options. Never claim compatibility without executing the relevant toolchain.
5. Update `docs/testing/strategy.md` when present. For a new document, use YAML frontmatter, kebab-case naming and a parent `index.md` entry.
6. Distinguish passing evidence from proposed tests; coverage percentage alone does not establish behavior quality.
7. Run project documentation validation and, when available, actual test commands. Report observable results with paths and gaps, never invented passing checks.
