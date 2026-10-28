---
title: "Backend and frontend technical feature structural screening"
description: "Technical-feature screening and neutral implementation template extraction from predecessor architecture"
kind: audit
status: active
---

# Backend and frontend technical feature screening

- Date: 2026-10-27
- Scope: **20** `technical/backend/features/F*` and **22** `technical/frontend/features/F*` files.
- Method: inspected each source's title, introductory references and high-level heading structure, with specific comparison against the already migrated .NET backend and Blazor feature templates. This is a **structural screening**, not line-by-line technical verification.
- Inventory status in this structural pass: `screened-structure` for these 42 files. Subsequent targeted reviews, including [backend/frontend F16](staff-access-evidence-review.md) and [F17 review/approval boundaries](content-review-evidence-review.md), update selected file states in the inventory; this original pass does not certify the remainder. No deletion authorization is implied.

## Backend feature group

The 20 backend specifications use a strongly prescriptive structure: Domain, Application.Abstractions, commands/queries, ports, specifications, handlers, validators, mappings, persistence and HTTP or integration adapters. These reflect a **specific modular .NET solution**, not universal architecture.

For example, F01/F02 describe versioned schema authoring while F13 defines a detailed synchronization protocol and failure-recovery strategy. Both are valid original-project topics, but a blank technical feature in a different project cannot require all those layers or protocols.

**Extracted:** provenance to the product requirement, confirmed contract boundaries, responsibility ownership, explicit state/transaction/failure behavior and verified acceptance evidence.

**Not adopted:** mandatory aggregate types, CQRS interfaces, fixed .NET project names, EntityFramework assumptions, exact version/ETag semantics, database schemas and broker transports.

## Frontend / client feature group

The 22 frontend specifications share sections for user-visible results, screens/routes, FSD block maps, logical composition, state ownership, interaction flows, API/NSwag, error/access behavior and acceptance.

The group is not limited to browser UI: F19 and F23 describe Unity SDK integration and an optional Addressables extension, while F20 is an experimental launcher/application. Treating all 22 as MudBlazor page templates would therefore be incorrect.

**Extracted:** a distinction between authoritative external contracts, UI/SDK state, navigation/interaction, failure feedback and implementation evidence.

**Not adopted:** mandatory Blazor Standalone, MudBlazor, NSwag, FSD, Unity runtime, specific routes, exact UI components or product-specific data.

## Cross-layer traceability

Many sources link the same feature across three different kinds of document:

1. **Business feature:** expected externally observable behavior and acceptance.
2. **Backend or implementation specification:** owners, persistence, integration contracts and application processing.
3. **Frontend or client specification:** user interaction, local state, display and transport.

The new [technology-neutral technical feature template](../../catalog/base/docs/templates/technical-feature.md) preserves this linkage without forcing projects to have all three documents. The existing specialized [.NET backend](../../catalog/docs/templates/dotnet-backend-feature.md) and [Blazor frontend](../../catalog/docs/templates/blazor-frontend-feature.md) templates remain opt-in alternatives for projects using those stacks.

## Remaining before cleanup

Each technically dense specification, particularly backend F01–F13 and frontend F19, still needs content-level classification before archive removal or sanitized example retention. The technical question files and per-module mirrors are separate, unreviewed sources. No project-specific API/message/SDK contract from these historical specifications has been promoted into the general catalog.
