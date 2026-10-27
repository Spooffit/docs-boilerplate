---
title: "Testing standard migration review"
description: "Historic and ongoing source analysis"
kind: audit
status: active
---

# Testing standard migration review

- Date: 2026-10-27
- Sources: original `technical/backend/standards/S04-testing.md`, `S09-architecture-tests.md`, `S10-unit-tests.md`, `S11-integration-tests.md`, `S12-functional-tests.md`, `S13-e2e-tests.md`, `S14-test-environments.md`, `S15-test-engineering.md` and `technical/frontend/standards/S08-testing.md`.
- Status: Targeted review and first consolidation. Not a complete line-by-line audit.

## Disposition

| Legacy source | Useful concept retained | Adjustment |
| --- | --- | --- |
| S04 | Classification by SUT and scope | Distinguish framework from runner; make MTP setup version-dependent |
| S09 | Explicit architecture constraints | Only assert boundaries adopted by an architecture decision |
| S10 | Isolated unit behavior and dependency substitution | Do not imply mocked infrastructure semantics are verified |
| S11 | Real database, broker and adapter verification | Use provider fidelity when correctness depends on it |
| S12 | One deployable application's public interface | `WebApplicationFactory` is one implementation choice |
| S13 | Complete consumer-visible journey | Do not mandate full distributed deployment if the scenario doesn't involve it |
| S14 | Fixture isolation, resource ownership and lifecycle | Preserve parallel CI safety; avoid static host ports |
| S15 | Shared technical testing helpers | Reuse only after real duplication, not as speculative infrastructure |
| Frontend S08 | Component and browser testing boundaries | Keep bUnit conditional on actual Blazor component test selection |

## Findings and corrections

1. Test categories must be driven by the SUT; test directory conventions are secondary.
2. `dotnet test` behavior depends on selected platform and SDK. Native MTP runner configuration is explicit for .NET 10; it should not be claimed as the default for all SDKs.
3. Testcontainers container naming, ports and cleanup should follow current official recommendations.
4. Different architectural styles must not inherit Clean Architecture rules just because the source project used them.
5. No platform should imply an arbitrary coverage percentage or a prescribed number of test projects.
6. The first consolidated baseline lives under `catalog/docs/standards/dotnet/testing.md`; TUnit and Testcontainers now have **optional** profiles. This leaves existing legacy files intact until the full migration is complete.

## Verified documentation sources

- https://learn.microsoft.com/en-us/dotnet/core/testing/test-platforms-overview
- https://learn.microsoft.com/en-us/dotnet/core/testing/migrating-vstest-microsoft-testing-platform
- https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
- https://dotnet.testcontainers.org/api/best_practices/
- https://dotnet.testcontainers.org/api/resource_reaper/

## Open review work

Evaluate the full text of each old standard for any residual contradictory requirements, including specific package APIs. Add compiling examples only with an executable demonstration suite, not unverified snippets.

## Inventory reconciliation (2026-10-27)

The eight original test-standard documents S04 and S09–S15 are now explicitly recorded as **reviewed-targeted** in the legacy inventory, with a common consolidated standard and opt-in runner/infrastructure profiles. This does not certify that every historical library snippet is compatible with its latest version; unverified code examples should remain source material until executable checks are added.
