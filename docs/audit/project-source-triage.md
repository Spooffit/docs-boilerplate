---
title: "Historical project source triage"
description: "Decision gates for separating reusable docs standards from predecessor product specifications"
kind: audit
status: active
---

# Historical project source: triage and deletion gates

- Date: 2026-10-27
- Inventory: [legacy-inventory.csv](legacy-inventory.csv)
- Scope: the **151** entries marked `project-source` in the original inventory; representative documents inspected. This is a **classification**, not a claim to have audited all 151 files line by line.

## What was inspected

Representative material includes `business/product.md`, the first business feature and open question, `technical/backend/architecture.md`, `technical/frontend/architecture.md` and `milestones/demo/demo.md`.

They describe a particular content-management product's routes, users, bounded contexts, license behavior, delivery features, technical integrations, and demo scope. Those are real product decisions **for that original product only**. Merely replacing the product name would not turn them into universal documentation standards.

## Grouped inventory

| Group | Inventory count | Initial disposition |
| --- | ---: | --- |
| Business feature specifications | 23 | Retain solely as historical examples until explicit acceptance/rejection |
| Business question records | 23 | Keep as source; extract generic **question template patterns**, not answers |
| Business module definitions | 7 | Keep as source; extract generic module format, not module identities |
| Business root and overview files | 5 | Do not copy product narrative to generated projects |
| Backend project documentation | 44 | Separate reusable technology rules from application-specific architecture and features |
| Frontend project documentation | 39 | Separate generic form/UI practices from concrete screens and feature flows |
| Milestone plans and artifacts | 6 | Keep only as examples of decision/acceptance structure if they pass review |
| Root legacy feature/module/question/standard lists | 4 | Treat as navigation for the old project, not current template navigation |

Totals count 151 entries. Files can be marked `screened-structure` after a scoped headings/intro review, but **this is not a complete semantic audit or deletion approval**. See [business screening](business-specification-screening.md), [technical features](technical-feature-screening.md) and [technical modules/questions](technical-module-question-screening.md) for precise scope. Only evidence-backed content review can advance files to `reviewed-targeted` or `reviewed-complete`; unresolved files remain `pending`.

## Deletion gate

A historical file can be removed from `main` only after:

1. Recording which genuinely reusable rules and template shapes have been extracted (or why nothing needs extraction).
2. Auditing incoming links with `tests/legacy_link_graph.py` and manually inspecting unsupported link variants; verifying no generated catalog files depend on it.
3. Checking that generated projects contain no historical product identifiers, feature numbers, roles, environment addresses or specific contracts.
4. Ensuring any example retained in the boilerplate is clearly anonymized, technology-relevant, optional and actually educational.
5. Updating the inventory with a definite disposition, removing stale references, and passing CI.
6. Confirming project-owner choices about contentious standards or examples when they affect semantics.

Deleting files from Git **does not erase them from repository history**. This audit does not authorize history rewriting.

## Decisions deferred for project owner

- Whether any former product feature documents should remain as sanitized, optional examples rather than live templates.
- Which highly prescriptive coding conventions should become opt-in technology profiles rather than universal rules.
- Whether large linked historical examples have enduring educational value or should remain only in history.

Do not ask for those choices during mechanical schema/test/inventory maintenance. Present them together when the engineering blocks are complete.

## Completion of structural screening

The inventory now has no unscreened entries, but `screened-structure` is explicitly **not** a completed content audit. The scope and result of the final 27-file pass are in [root and milestone screening](root-milestone-screening.md). Before cleanup, source owners must review substantive technical content, decide which examples merit retention, and satisfy each deletion gate above. Owner-dependent choices are collected in the [deferred decision backlog](owner-decision-backlog.md).

## First actual cleanup

Seven zero-value placeholders (an empty MCP file and six empty-directory README stubs) have been removed after their content was inspected. See [recorded removals](legacy-removals.md). The historical inventory retains their entries with state `removed-cleanup`, and CI checks explicit Markdown links to removed files. This does not change the retention policy for substantive source documents.
