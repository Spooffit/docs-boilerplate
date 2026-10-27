---
title: "Business feature, module and question structural screening"
description: "Structural screening of predecessor product requirements and extraction of reusable documentation patterns"
kind: audit
status: active
---

# Business feature, module and question structural screening

- Date: 2026-10-27
- Scope: all **23** `business/features/F01–F23` files, all **7** `business/modules/M01–M07` files, and all **23** listed `business/questions/Q*` files.
- Method: inspected each file's available body, title, section structure and introduction, and compared representative scope/decision distinctions with the existing generic templates. This is **structural screening**, not a complete semantic, code or line-by-line audit.
- Inventory status: `screened-structure`; no deletion is authorized by this screening alone.

## Feature family overview

| Source group | Observed subject matter | Decision for reusable catalog |
| --- | --- | --- |
| F01–F04 | Schema, draft/published version, instance identity and lifecycle | Keep a neutral feature lifecycle and stage section, not concrete field types, version numbers or entity rules |
| F05 | Asset management, upload, usage, retention | Preserve questions about ownership and failure boundaries, not project-specific MIME/storage contracts |
| F06–F11 and F15 | Source catalog, assignments, release snapshots, publication lifecycle and conflicts | Explicitly distinguish save, release, activation, visibility and failure semantics **where a project actually has these steps** |
| F12–F14 | Entitlement, delivery, synchronization, deferred compatibility | Preserve stage scope, external-contract authority and observed vs planned behavior; do not add these integrations to all projects |
| F16–F18 | Staff access, moderation and activity history | Document permissions, authoritative enforcement and audit evidence only for applicable features |
| F19–F23 | Unity client, demo, registry, C# generation and optional Addressables | Keep source schemas/contracts and optional runtime components distinct. Do not impose Unity or code generation on the general template |

All 23 source features contain distinctive product terminology and dependencies. They **must not be copied as generic specification examples** merely by replacing project names. A passing docs linter would not change that conclusion.

## Seven module descriptions

The module files group the above features by authoring, media, catalog, publishing, delivery, staff workflow and schema tooling. Some modules are UX or documentation groups, while others describe technical runtime boundaries; there is no universal one-to-one mapping to a DDD bounded context.

The base `docs/templates/module.md` now makes the distinction between capability grouping, runtime boundary and domain bounded context explicit, and links parent/child requirements instead of replicating their contracts.

## Question records

The 23 source question documents are not uniformly open:

- Q14 and Q22 explicitly contain unanswered or deferred details.
- Several other Q records document accepted policy, versioning, identity or workflow decisions along with residual engineering work.
- Complex question records (for example Q07, Q26 and Q34) contain extensive scenario detail. Their size and links require a separate semantic review before any deletion.

Therefore the presence of `Qxx` or the word "question" in a filename must never be interpreted as `status: open`. The reusable question template now separates **decision status**, remaining unknowns and **propagation** to linked authoritative documents.

## Reusable outputs

- [Feature template](../../catalog/base/docs/templates/feature.md): explicit delivery-stage scope and decision status.
- [Module template](../../catalog/base/docs/templates/module.md): authority and ownership distinctions.
- [Question template](../../catalog/base/docs/templates/question.md): resolution lifecycle and propagation.
- [Specification traceability standard](../../catalog/base/docs/standards/specification-traceability.md): cross-document consistency checks.

These are portable authoring conventions, not imported product requirements.

## What remains before cleanup

Review technical/backend and technical/frontend feature mirrors, associated questions, their links and remaining project-only root documents. For the 53 structurally screened files, a source-level content review is still required to decide whether anything merits sanitization as an example or can remain only in Git history. Owner approval is still needed for semantic choices about retaining examples.
