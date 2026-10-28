---
title: "Initial historical placeholder cleanup"
description: "Reviewed removal of seven empty or placeholder-only legacy files"
kind: audit
status: active
---

# Initial historical cleanup: placeholders

- Date: 2026-10-27
- Scope: exactly **seven** files, all inspected directly before removal.
- Outcome: remove them from current `main` while preserving normal Git history. Remaining substantive historical documents are untouched.

## Removal decisions

| File | Actual content and reason for removal |
| --- | --- |
| `.ai/mcp/mcp.json` | Empty JSON file (zero bytes), provides no MCP configuration |
| `business/adr/README.md` | Short request to add an ADR index in the old project, not an ADR or standard |
| `business/archive/README.md` | Short request for an old-project archive index, not authoritative content |
| `technical/backend/adr/README.md` | Placeholder text only, no actual decision |
| `technical/backend/archive/README.md` | Placeholder text only, no actual archived source |
| `technical/frontend/adr/README.md` | Placeholder text only, no actual decision |
| `technical/frontend/archive/README.md` | Placeholder text only, no actual archived source |

The six README placeholders were the **only files** in their corresponding old `adr/` or `archive/` directories. The empty MCP file was likewise the only file in `.ai/mcp/`.

These files are replaced for new projects by intentional generator behavior: real `docs/index.md` navigation, `docs/templates/adr.md`, actual ADR records where chosen, and selected Agent Skills. No artificial empty folder is generated.

## Link and safety checks

The audit looked for explicit repository references to these removed filenames, and CI now checks that surviving Markdown does not link to a removed path. This covers conventional inline Markdown links; it is not a full general-purpose Markdown parser and does not imply all historical anchors resolve.

The legacy inventory retains the seven paths as `removed-cleanup` so subsequent audits cannot silently confuse historical removal with an original file that never existed.

This review does **not** authorize removal of any substantive feature, module, technical standard, question, product glossary or milestone. Later removals must receive individual disposition and evidence before editing this allowlist.

## Follow-up removal: C# formatter rule

- Removed path: `technical/backend/standards/S24-code-formatting.md`.
- Source inspection: the entire file was a 339-byte instruction to run `dotnet format --include` after changing C# files. Its title incorrectly used `S23` even though the filename was `S24`, colliding with the separate project-file standard.
- Inbound-reference check: the full Markdown graph reported **zero** incoming documents to the S24 path. The old root `standards.md` indexed S01–S23, not S24. The link checker does not prove absence of every plain-text or HTML mention.
- Migrated value: the formatting-and-verification rule now lives in `catalog/docs/standards/dotnet/conventions.md`, included only with the `.NET` profile. The new version respects actual repository formatter settings and requires reporting when formatting was not executed.
- Reason: no unique product contract or example needs preserving in the current documentation tree. The original text remains accessible through normal Git history.
- Deletion gate: no current recognized Markdown link may reference this path; `tests/legacy_link_graph.py` enforces it in CI.

## S01 and S23: migration of low-dependency .NET conventions

- Removed path: `technical/backend/standards/S01-dotnet-conventions.md`. The complete source described optional Domain/Application/Presentation/Infrastructure responsibilities, API/DTO mapping, identity distinctions and verification of actual current sources. These topics are now documented in [the selected .NET conventions profile](../../catalog/docs/standards/dotnet/conventions.md). The old file's only recognized inbound Markdown link came from `standards.md` and has been updated. The new catalog text does **not** require every .NET project to adopt the original layered architecture.
- Removed path: `technical/backend/standards/S23-csproj.md`. Its project-file rules covered XML formatting, inherited props/targets, necessary references, packaging, module boundaries and an EventBus/test-project convention. Those transferable rules are now present in [the selected .NET project-files standard](../../catalog/docs/standards/dotnet/project-files.md). The original mandatory EventBus folder pattern and source-specific two-space formatting are **conditional examples**, not universal rules. Its two recognized incoming Markdown references in `standards.md` and `technical/backend/architecture.md` were updated.
- Link assessment: the prior CI reference graph found **one** inbound Markdown document for S01 and **two** for S23. After migrating those links, `tests/legacy_link_graph.py` rejects any remaining recognized references to the removed paths. HTML, bare-text and dynamically constructed references still require manual review.
- Decision: each file was read in full, reusable content was extracted or intentionally scoped to the relevant optional profile, and neither file contained a unique predecessor product contract needing an active copy. They remain available through Git history; the rest of the original project is not approved for deletion by this decision.

## S09/S13: architecture and E2E test standards

- Removed `technical/backend/standards/S09-architecture-tests.md` after whole-file review. Accepted structural rules and no-Docker architecture tests moved to `catalog/docs/standards/dotnet/testing.md`; project-specific assembly inventories were explicitly not made universal. Replaced links in `standards.md`, `technical/backend/standards/S04-testing.md` and `technical/backend/features/F01-create-entity-schema.md`.
- Removed `technical/backend/standards/S13-e2e-tests.md` after whole-file review. Consumer-visible end-to-end testing, isolation and bounded asynchronous outcomes moved into the core .NET testing standard; Aspire/AppHost-specific orchestration is available only by opting into `aspire-testing`. Replaced known links in `standards.md` and `S04-testing.md`.
- This removal neither rewrites the historical application's tests nor validates its AppHost code. The incoming-link gate must pass, and the originals remain accessible in normal Git history.

- Follow-up reference fix: `technical/frontend/standards/S08-testing.md` also linked to the former S13. The full-repository CI deletion guard caught it; the link and associated E2E classification text now point to the general .NET testing standard. This was a real failing safety check, not a passing review assumption.

## S10/S11/S12: unit, integration and functional testing

- Removed `technical/backend/standards/S10-unit-tests.md` after whole-file review. Its isolated SUT, test-double limits, independent data and fast parallel execution remain in [Unit tests](../../catalog/docs/standards/dotnet/testing.md#unit-tests). The exact `MethodUnderTest_Scenario_ExpectedBehavior` name shape is conditional on project agreement, not globally mandatory.
- Removed `technical/backend/standards/S11-integration-tests.md` after whole-file review. Real-provider SQL/database/broker verification, technical white/gray-box assertions and isolation remain in [Integration tests](../../catalog/docs/standards/dotnet/testing.md#integration-tests). Broker routing and error topology are now named explicitly. Testcontainers is opt-in, not an inherent .NET requirement.
- Removed `technical/backend/standards/S12-functional-tests.md` after whole-file review. The one-deployable-app SUT, public HTTP/gRPC interface, observable result, optional `WebApplicationFactory` and bounded asynchronous waiting remain in [Functional tests](../../catalog/docs/standards/dotnet/testing.md#functional-tests). ASP.NET Core, PostgreSQL and RabbitMQ are not required of every project.
- **Incoming references:** the previous CI graph counted S10: 6, S11: 7 (including S10), S12: 6 distinct referring documents. Eighteen references in surviving Markdown were redirected to the appropriate canonical testing heading, and S10's outgoing reference disappeared with the reviewed source. The deletion gate prohibits recognized links to the retired originals.
- This documents source migration, not execution of the predecessor project's test suites. Historical copies remain in Git history. HTML/bare-text/dynamic references are outside automated link scanning and require manual review.


## S15: shared test engineering and migration

- Removed `technical/backend/standards/S15-test-engineering.md` after full source review. Shared **technical** fixtures and domain-specific builders remain distinct, async outcomes are awaited using observable conditions with bounded deadlines/cancellation, and suite migration must not silently skip tests or change production behavior.
- Portable rules now live in [shared testing infrastructure and safe migration](../../catalog/docs/standards/dotnet/testing.md#shared-testing-infrastructure-and-safe-migration). Fixed BuildingBlocks placement, mandatory individual test naming, one specific runner or mandatory AppHost layout are **not universal**.
- The last reference graph showed **four referring documents**. Five recognized Markdown links in `standards.md`, `S04-testing.md`, `S14-test-environments.md`, and `technical/frontend/standards/S08-testing.md` were redirected to the canonical section; S08 contained two separate links.
- The historical source remains in normal Git history. This source migration is not a claim that the predecessor suite ran or that all its historical CI requirements remain active.


## S14: independently provisioned test environments

- Removed path: `technical/backend/standards/S14-test-environments.md` after inspection of its complete original content and comparison with the now-canonical [test environment ownership rules](../../catalog/docs/standards/dotnet/testing.md#test-environment-ownership-and-bounded-parallelism), the optional [Testcontainers](../../catalog/docs/standards/dotnet/testcontainers.md) and [TUnit](../../catalog/docs/standards/dotnet/tunit.md) profiles.
- Preserved: independent Integration/Functional/E2E environment lifecycles, logical versus physical isolation, safe parallel PostgreSQL data scopes, RabbitMQ virtual-host ownership, bounded heavy-test concurrency, dynamically named resources, cleanup after setup/test failures, and no hidden dependence on externally prepared infrastructure. Framework-specific TUnit concurrency controls stay opt-in.
- Not made universal: the predecessor's exact TUnit tooling preference or example concurrency numbers. They require the project's own selected tooling and measured CI limits.
- Incoming-link reconciliation: the full GitHub Actions reference report showed one remaining incoming S14 document after the initial 20 migrations, `technical/backend/features/F01-create-entity-schema.md`. That last link is migrated in the same commit as retirement, for a total of **21** redirected referring documents. The removal must pass `tests/legacy_link_graph.py` and supported Linux/Windows CI.
- The original S14 source remains available in Git history; this is documentation migration, not execution of the predecessor application's infrastructure tests.

## S02/S03: persistence and validation boundary standards (2026-10-28)

- Retired `technical/backend/standards/S02-persistence.md` after full-source comparison. Durable snapshot/pointer/history atomicity, retained references, concurrent ownership and uniqueness, concurrency outcomes and safe migration rules were retained in conditional base `stateful-workflows.md`, EF Core and PostgreSQL profiles. Optional `integration-reliability` documents DB-and-broker delivery consistency and replay. The mandatory `Ardalis.Specification` and `<AggregateName>Constraints` conventions were **not** made universal; existing historical S19/S21 still document their old project's choices and the owner decision is registered.
- Retired `technical/backend/standards/S03-validation-and-errors.md` after full-source comparison. Portable trust boundaries, format/normalization, observable failure taxonomy, partial batches, side effects and unknown commit results were retained in base `data-flows.md`; ASP.NET-specific response semantics remain in its optional profile. The mandate that all .NET requests use FluentValidation was **not** made universal and is recorded in the owner decision backlog; composition/async behavior is available in the independently selected FluentValidation profile.
- Incoming-link review: the last passing reference graph found **four** referring Markdown documents per source. S02 incoming: `standards.md`, S17, S19 and S21. S03 incoming: `standards.md`, S16, S18 and S21. These eight links were individually redirected to the most relevant section and S02/S03 removed in the same atomic commit. Both original texts remain in Git history.
- Deletion gate: `tests/legacy_link_graph.py` must reject lingering ordinary Markdown links to either retired path. The checker is conservative; HTML, plain text, dynamic strings and code examples require separate review. This migration **does not** establish that any historical application's code or test suite passed.

## S18/S19/S20: validator, query specification and handler placement standards (2026-10-28)

- **S18** `technical/backend/standards/S18-fluent-validation.md`: reviewed the full original document. Its field-level rule reuse, `IRuleBuilder` extensions, `Include` same-type semantics, `SetValidator` for nested models, `RuleForEach` for collections, null/patch semantics, field-path diagnostics, pure validation boundaries, asynchronous `ValidateAsync`, and unit/application verification are preserved in [the opt-in FluentValidation profile](../../catalog/docs/standards/dotnet/fluentvalidation.md). A universal FluentValidation mandate is intentionally **not** adopted.
- **S19** `technical/backend/standards/S19-specifications.md`: reviewed the full source. Its atomic query-builder reuse, absence of built-in AND/OR/NOT composition of full specification objects, explicit predicate/ordering/projection/page boundaries, authorization scope, EF Core evaluator choice, SQL translation and count/paging semantics are preserved in [the opt-in Ardalis.Specification profile](../../catalog/docs/standards/dotnet/ardalis-specification.md). A universal specification-library mandate is **not** adopted. No unverified library example is presented as compiling.
- **S20** `technical/backend/standards/S20-cqrs.md`: the entire source consisted of a command-handler-in-Application rule and query-handler-in-`EntityFramework.Persistence` rule. The portable replacement is [CQRS handler responsibility and architecture-dependent placement](../../catalog/docs/standards/architecture/cqrs.md#handler-responsibilities-and-dependency-direction); exact assemblies and fixed placement are left to a project architecture decision.

**Inbound-link evidence:** the last passing reference-graph baseline reported 26 documents for S18, 21 for S19 and 18 for S20. The fully updated surviving Markdown now has **zero recognized inbound documents** for all three in the Linux Actions report for commit `da787f4a715fd8f73deb8331ce2e12080443d987`. Related code, historical feature documentation and their cross-links were not deleted; only these three fully reviewed legacy standards are retired. Existing original policy choices remain visible in the owner-decision backlog, and all removed source text remains available through Git history.

**Deletion gate:** the inventory entries become `removed-cleanup` and `tests/legacy_link_graph.py` must continue rejecting recognized incoming Markdown links. HTML, plain-text and dynamically constructed references are not conclusively checked by that parser. This is source-document consolidation, not proof that the predecessor project's application tests or the optional libraries' code samples were executed.
