---
title: "Architecture and milestone source extraction"
description: "Targeted review of original product architecture, requirements and demonstration planning"
kind: audit
status: active
---

# Legacy architecture and milestone extraction

- Date: 2026-10-27
- Source documents read: `technical/backend/architecture.md`, `technical/frontend/architecture.md`, `business/product.md`, `milestones/demo/demo.md`.
- Scope: targeted extraction of reusable documentation **patterns**, without adopting predecessor business requirements or copying complete feature specifications.

## Source-specific dispositions

| Source | Portable knowledge retained | Not generalized |
| --- | --- | --- |
| Backend architecture | Module API boundaries, dependency direction, composition root ownership, explicit persistence and transaction ownership, architecture tests derived from selected design | Named bounded contexts, PostgreSQL schema list, exact advisory lock scheme, required ETag/428 policy, SDK runtime and deployment assumptions |
| Frontend architecture | Role-specific interfaces, local-versus-server authority, independent component and state ownership, client/server contract separation | Exact UI navigation, login/session settings, Blazor/MudBlazor/FSD/NSwag mandate, licensing flows and launcher/Unity behavior |
| Business product overview | Separate product purpose, user outcomes, responsibilities and explicit exclusions; link features instead of repeating details | Specific content-manager product identity, licenses, schema lifecycle, publishing and legacy integrations |
| Demo milestone | Distinct milestone purpose, included/excluded scope, reproducible verification, readiness blockers and truthful acceptance evidence | Feature IDs, Unity version, media protocols, implementation detail and specific release promises |

## Catalog changes

- [Modular monolith standard](../../catalog/docs/standards/architecture/modular-monolith.md): strengthened guidance on consumer-owned ports, dependency directions, persistence and cross-module consistency. These rules apply **only if the architecture profile is selected**.
- [Milestone template](../../catalog/base/docs/templates/milestone.md): reusable milestone and evidence structure, initialized as an optional **blank template**, not a required delivery phase.
- Existing frontend form/accessibility guidance and generic data-flow standard cover further applicable pieces; no new forced frontend library profile was created.

## Explicitly not decided here

Whether every module uses an independent database schema, whether cross-module calls must use a broker, whether exact HTTP routes require `If-Match`, and which SDK generator a future project should use are project-level choices. They must not enter the universal boilerplate without explicit owner approval.

The four historical sources remain in the repository and are classified `reviewed-targeted`, **not** `reviewed-complete`. This step does not imply that all their referenced project feature files have been audited.

## Remaining source work

Review the linked feature and open-question specifications individually or by carefully documented groups. Remove legacy files only when their reusable value and incoming references have been addressed according to the [deletion gate](project-source-triage.md).
