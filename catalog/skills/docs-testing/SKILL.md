---
name: docs-testing
description: Document testing scopes and evidence based on the system under test and selected technology standards without inventing tool requirements.
---

# Review testing documentation

1. Read `docs.config.json`, `docs/standards/dotnet/testing.md` if the .NET profile is selected, and the actual test project structure if available.
2. Identify the system under test for each suite: isolated component, real integration boundary, public application interface, UI component, deployed user journey or contract.
3. Compare intended risks to assertions, fixture isolation, cleanup, timeout behavior and CI evidence.
4. Treat TUnit, Testcontainers, bUnit, WebApplicationFactory and Aspire Testing as opt-in choices. Apply their profile standards only when selected.
5. Update `docs/testing/strategy.md` if the project uses the testing module; otherwise create a project document only when needed and avoid duplicating normative standards.
6. Distinguish tests that ran from tests merely proposed. Never claim coverage or platform compatibility without supporting results.
7. Run repository validation and provide concrete findings with paths and remaining evidence gaps.
