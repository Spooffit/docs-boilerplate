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

## Focused S09/S13 source migration (2026-10-28)

The entire original `S09-architecture-tests.md` and `S13-e2e-tests.md` sources were inspected before removal.

- **S09:** enforce adopted architecture constraints without Docker, use real assembly/project dependency rules, keep domain-specific project names out of shared testing helpers and do not treat a testing-library dependency as architectural test coverage. Now explicitly preserved in `catalog/docs/standards/dotnet/testing.md`.
- **S13:** test the smallest real distributed consumer journey end-to-end with externally visible assertions, independent fixture lifecycle, bounded async conditions and cleanup. General boundary guidance is in `catalog/docs/standards/dotnet/testing.md`; AppHost-specific guidance is **only** in the opt-in `catalog/docs/standards/dotnet/aspire-testing.md`, and its package APIs are linked to current Microsoft documentation.

S09 had recognized inbound links from `standards.md`, `technical/backend/standards/S04-testing.md` and `technical/backend/features/F01-create-entity-schema.md`. S13's identified links from the root catalog and S04 were migrated. The legacy reference checker is an independent deletion gate for any additional incoming Markdown links. Removed files remain in Git history; this migration does not imply that older application tests actually ran.

## Focused S10/S11/S12 review (2026-10-28)

The remaining original unit, integration and functional testing standards were read in full and compared with the now-explicitly scoped guidance in `catalog/docs/standards/dotnet/testing.md`.

| Source | Retained guidance | Intentionally not made universal |
| --- | --- | --- |
| S10 Unit | Independent isolated logic, fakes only for component-level claims, no required infrastructure and parallel-friendly tests | Mandatory exact method-name-based test naming for every .NET repository |
| S11 Integration | Real provider fidelity; infrastructure-specific white/gray-box observations; focused graph, isolated disposable resources | Mandatory Testcontainers, PostgreSQL, RabbitMQ, or one container per method |
| S12 Functional | Public application interface, observable result, `WebApplicationFactory` where appropriate and bounded async observations | Mandatory ASP.NET Core, `WebApplicationFactory`, broker or ORM for all functional tests |

The three original sources remain in the historical tree at this stage. Their incoming references from S04, S11/S12, the old catalog and possibly other legacy documents must be rechecked and rewritten before deletion. The new guidance is a reusable interpretation, **not** a claim that code/tests from the predecessor repository were verified.

The proposed next cleanup is to migrate live incoming references to the new scope and remove S10–S12 only after the link-graph deletion gate reports no remaining references and the source-specific decision is recorded in the removal ledger.

## Completed S10/S11/S12 source migration (2026-10-28)

All three complete source files were compared with the consolidated .NET testing standard. The distinct claims of **isolated unit rules**, **real-provider integration behavior** and **one application's public functional behavior** have dedicated, stable headings `#unit-tests`, `#integration-tests` and `#functional-tests` under `catalog/docs/standards/dotnet/testing.md`.

All recognized references from surviving Markdown (S10: 6, S11: 6, S12: 6) were migrated to exact sections, not just the document root. The older source's unconditional Testcontainers/WebApplicationFactory choices and exact naming convention remain **optional** where appropriate, not global requirements. The three sources are marked `removed-cleanup`. Their previous tests were not rerun or certified here.


## Complete S14/S15 extraction, pending retirement (2026-10-28)

Both remaining original sources were inspected in full. S14 distinguishes physical versus logical fixture isolation (database per parallel worker, RabbitMQ vhost per worker when applicable), resource reuse with safe reset, cleanup after errors and separately budgeted heavy-suite parallelism. These rules are now in the core `catalog/docs/standards/dotnet/testing.md#test-environment-ownership-and-bounded-parallelism`, while provider-specific sharing recommendations are in the **optional** Testcontainers profile.

S15 addresses reusable **technical** test fixtures, eventual-consistency polling, independent data, incremental migration of old test suites and truthful validation of what ran. Its portable contents are now in `catalog/docs/standards/dotnet/testing.md#shared-testing-infrastructure-and-safe-migration`. Its fixed BuildingBlocks location, exact test naming and specific toolchain are explicitly **not** made mandatory for all .NET projects.

S14/S15 remain physically present until their incoming Markdown references and non-link mentions have been reconciled. This focused review is not execution of predecessor tests and does not authorize deletion without the link gate.

## Completed S15 source migration (2026-10-28)

The entire 6 KB test-engineering source was examined. Its fixture-sharing, async-polling, ownership, SUT-based migration and clean-run verification principles are now in `catalog/docs/standards/dotnet/testing.md#shared-testing-infrastructure-and-safe-migration`. The shared technical library is created only when reuse is justified; no prescribed BuildingBlocks directory, tester framework or universal naming shape is added.

Five recognized links across four surviving documents, including two frontend S08 mentions, were updated to the exact section. The old source was retired under the deletion gate, and the inventory records `removed-cleanup`. The remaining S14 environment standard retains its independent review/removal gate.

## S14 environment migration readiness (2026-10-28)

The complete original S14 source was compared against `catalog/docs/standards/dotnet/testing.md#test-environment-ownership-and-bounded-parallelism` and the optional `dotnet/testcontainers.md` profile. Core coverage now includes independent suites, no cross-suite prerequisite state, disposable and logically isolated resources, parallel database resets, RabbitMQ vhosts, bounded heavy-suite concurrency and cleanup under failure. TUnit-specific scheduling remains a conditional tooling choice. Retire S14 only after all incoming links are migrated and the deletion gate passes; the original file stays intact meanwhile.

S04 remains an independent historical decision because its mandatory test-name format is not a universal .NET rule and frontend S08 also references it. Whether to retain that opinionated naming convention is deferred to `owner-decision-backlog.md`, not silently decided during engineering cleanup.
