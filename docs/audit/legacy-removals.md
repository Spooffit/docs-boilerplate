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

