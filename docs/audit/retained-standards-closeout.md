---
title: "Retained technology standards: technical closeout"
description: "Evidence-based handoff for five historical standards awaiting owner decisions"
kind: audit
status: active
---

# Retained technology standards: technical closeout

- Reviewed: 2026-10-28
- Scope: **five** active historical `candidate-technology-standard` inventory entries, not the entire predecessor project.
- Status: technical extraction and disposition reviewed; **owner choices remain unapproved**. This is not authorization to delete the originals.
- Inventory: [legacy-inventory.csv](legacy-inventory.csv); historical cleanup: [removal ledger](legacy-removals.md); decisions: [owner backlog](owner-decision-backlog.md).

## Retained standard matrix

The paths in the first column are checked against the historical inventory by `tests/legacy_inventory.py`. A change in this list requires updating both the inventory and this record. **Zero incoming Markdown links does not imply safe deletion.**

| Historical path | Generalized destination already present | Residual policy requiring an owner choice |
| --- | --- | --- |
| `technical/backend/standards/S04-testing.md` | [.NET testing](../../catalog/docs/standards/dotnet/testing.md#unit-tests) classifies suites by SUT, requires independent real-infrastructure verification and treats naming as project-selected | Mandatory `MethodUnderTest_Scenario_ExpectedBehavior` for *all* test suites, including client and architecture tests; see strict naming decision |
| `technical/frontend/standards/S06-api-client.md` | [HTTP client contracts](../../catalog/docs/standards/http/client-contracts.md#optional-generated-client-and-ui-model-policy) describes generated-client ownership, UI adapters, HTTP metadata and [conditional writes](../../catalog/docs/standards/http/client-contracts.md#conditional-writes-and-version-tokens) | NSwag DTOs must be the direct edit/read models; no parallel form models; client-side ETag and recovery UX depend on the actual API contract |
| `technical/frontend/standards/S08-testing.md` | [.NET testing](../../catalog/docs/standards/dotnet/testing.md), [UI semantics](../../catalog/docs/standards/ui/forms-and-accessibility.md) and optional framework-specific selection preserve component/browser boundaries | bUnit mandatory for Blazor component tests, FSD-mirrored test layout and mandatory test-name shape; no automatic selection for all projects |
| `technical/backend/standards/S16-rest.md` | [HTTP API design](../../catalog/docs/standards/aspnet/http-api.md), [MVC controllers](../../catalog/docs/standards/aspnet/mvc-controllers.md) and [error semantics](../../catalog/docs/standards/aspnet/errors-and-validation.md) preserve the transport boundary, binding, response and concurrency rules | Exact MVC/CQRS buses, response envelope, mutation status conventions, route/version constants and `int Version` ETag policy; sample code uses project-owned abstractions |
| `technical/backend/standards/S21-aggregate-constraints.md` | [DDD constraints](../../catalog/docs/standards/architecture/ddd.md#aggregate-owned-rule-definitions), [EF Core mapping](../../catalog/docs/standards/dotnet/ef-core.md#mapping-domain-constraints-into-ef-core) and optional FluentValidation guidance cover the portable semantics | Mandatory nested public `<AggregateName>Constraints` type for every aggregate, plus a large cross-layer `AppealConstraints` example that has not been compiled as a standalone sample |

## Technical disposition

1. **No outstanding universal-rule extraction is identified in these five sources by this targeted comparison.** Each already has an applicable technology-scoped or neutral destination. This does not certify that every source code example compiles or that all historical feature contracts have been reviewed.
2. The shared principles are **not** owner decisions: test SUT and environment isolation, respecting authoritative HTTP contracts, preserving observable UI behavior, selecting an HTTP precondition policy, and keeping current domain/storage constraints consistent already have canonical coverage.
3. The remaining decisions are **deliberately opinionated defaults or historical examples**. Migrating them as universal requirements would silently alter generated projects. Do not select NSwag, bUnit, FSD, CQRS buses, a response envelope, a named constraints class, or mandatory test names merely because the predecessor selected them.
4. Retained documents remain historical input under `technical/`. The generator must not include them as selected project standards, and they must not be linked as normative instructions for newly generated projects.

## Evidence required before any opinionated profile is made available

| Area | Minimum verification before promoting a specific option |
| --- | --- |
| Naming policy (S04/S08) | Project-selected naming contract, representative Unit/Architecture/Integration/Component/E2E names and a practical enforcement rule that does not misclassify parameterized cases |
| Generated client + UI (S06) | Pinned OpenAPI/NSwag generation, a real edited form state example, response-header extraction and contract tests for stale `If-Match`, `412`, `428` and unknown commit outcomes where relevant |
| Blazor/FSD testing (S08) | Version-compatible bUnit test project, documented slice ownership, browser cases that bUnit cannot prove and an executable CI matrix for the chosen stack |
| MVC/CQRS/ETags (S16) | A compiling application with actual `ICommandBus`/`IQueryBus` and result mapper implementations, HTTP integration tests for binding/auth/status/error and strong-validator behavior |
| Aggregate constraints (S21) | A sanitized, compiling example with matching Domain, validator and EF mapping, migration-history tests and a review of whether its complexity provides reusable teaching value |

These are **promotion gates**, not claims that the verification has already been performed. The existing optional baseline profiles remain valid without these example applications.

## Deletion and consultation gates

- Keep all five sources marked `reviewed-targeted` and physically present until the relevant owner choices are recorded; do not equate no incoming links with redundant meaning.
- At consultation, record for each decision: selected option, scope (default/opt-in/example-only), rationale, affected profiles/templates and executable evidence required.
- Only after the decision, individually review source-specific examples and incoming Markdown references (including manual HTML, bare-text and dynamic forms), update the [removal ledger](legacy-removals.md) and inventory, and require a passing CI check.
- The wider `business/`, `technical/*/features/`, modules/questions and `milestones/` trees are **structurally screened, not semantically complete**; see [source triage](project-source-triage.md). Their cleanup is a separate content/evidence phase, not an automatic consequence of resolving these five standards.

## Verification limits of this closeout

The five retained texts, the canonical destinations, inventory, decision backlog and CI definitions were compared. Existing CI includes Linux generator/contract/schema/hygiene/inventory/link checks, Windows generator smoke tests and Chromium Docsify navigation checks. **This closeout itself did not execute the .NET CLI, Windows, or live browser suites.** CDN availability, broader browser/accessibility coverage and semantic reviews of historical features remain independent risks.
