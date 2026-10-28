---
title: "Data flows and UI standards migration review"
description: "Extracted general rules and deferred project-specific constraints"
kind: audit
status: active
---

# Data flows and UI standards: migration review

- Date: 2026-10-27
- Scope: source `technical/backend/standards/S17-data-flows.md`, `S21-aggregate-constraints.md`, `technical/frontend/standards/S05-ui-and-forms.md` and `S07-accessibility.md`.
- Status: initial targeted content review, not deletion authorization. Since this snapshot, S17, S05 and S07 were individually migrated and retired; S21 remains in the historical tree pending the owner decision. See [removal ledger](legacy-removals.md) and [retained-standard closeout](retained-standards-closeout.md).

## Decisions

| Original | Outcome | Target |
| --- | --- | --- |
| S17 Data flows | Extract generalized external transport and internal commit/failure boundaries; remove mandatory HTTP/CQRS/project DTO examples | `catalog/base/docs/standards/data-flows.md` |
| S21 Aggregate constraints | Preserve the single-source-of-truth idea, domain versus input requiredness, migration-history caveats and consistent normalization. The fixed `<AggregateName>Constraints` public class structure is a **project choice**, not a universal rule | `catalog/docs/standards/architecture/ddd.md` and relevant persistence/validation profiles |
| S05 UI and forms | Preserve truthful save status, pending/failed/unknown outcomes and explicit delete semantics; do not prescribe a particular UI or backend API | `catalog/docs/standards/ui/forms-and-accessibility.md` |
| S07 Accessibility | Preserve keyboard/label/focus/error fundamentals; reference actual WCAG criteria without claiming automated compliance | `catalog/docs/standards/ui/forms-and-accessibility.md` |

## Applicability

The data-flow documentation standard is a **base** authoring convention, applied only when the feature actually has external interactions, consequential state changes or asynchronous effects.

The UI forms/accessibility standard is exposed as the independent `ui-accessibility` profile. Blazor depends on it automatically. .NET backend-only and database-only projects do not receive it.

The nested Aggregate Constraints class shape, exact names, and universal FluentValidation requirement are intentionally **not** adopted across all DDD or .NET projects. Reconsider these as an additional opt-in coding-standard profile only if the owner approves the implementation-specific convention.

## Evidence and limits

- Source files listed above were read in the connected repository, including the S21 constraints content.
- UI guidance is cross-checked against [WCAG 2.2](https://www.w3.org/TR/WCAG22/) and [WAI form errors](https://www.w3.org/WAI/WCAG22/Understanding/error-identification).
- No claim is made that any particular project or UI is WCAG certified.
- Do not infer permission to remove remaining historical files from this initial review. Apply each documented removal gate to the specific source, especially the retained S21 teaching example.

## Follow-up

Review the remaining source templates and the large project-specific business/technical feature documents by class, and record extraction decisions. A future human design review may decide whether project-specific naming and aggregate constraint conventions deserve dedicated optional profiles.
