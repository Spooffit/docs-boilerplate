---
title: "F17 content review: cross-layer semantic evidence review"
description: "Targeted historical extraction of immutable review decisions, comparison bases and durable publication boundaries"
kind: audit
status: active
---

# F17 content review: decision and effect boundaries

- Date: 2026-10-28.
- Primary sources: [business F17](../../business/features/F17-content-review.md), accepted [Q28 review scope](../../business/questions/Q28-content-review-scope.md), [backend F17](../../technical/backend/features/F17-content-review.md) and [frontend F17](../../technical/frontend/features/F17-content-review.md).
- Context checked: [business F16](../../business/features/F16-staff-access.md), [F11 publication lifecycle](../../business/features/F11-publication-lifecycle.md), [F18 activity history](../../business/features/F18-staff-activity-history.md), and [M06 workflow ownership](../../business/modules/M06-staff-workflow.md). The underlying technical [Q15](../../technical/backend/questions/Q15-api-and-source-code.md) and [Q20](../../technical/backend/questions/Q20-persistence-and-migration.md) have existing [targeted evidence review](q15-q20-evidence-review.md); this review does not certify their complete source, PostgreSQL implementation or current API.
- Review depth: full business F17 and Q28, targeted backend F17 state/handler/HTTP/transaction/test sections and frontend F17 ownership/interactions/errors/testing; compare these to canonical standards. Both backend and frontend remain large, project-specific designs. No source-level line-by-line or executable certification is claimed.
- Inventory state: all **four primary sources** are `reviewed-targeted` after this pass. Backend F17 had that state already from Q15/Q20 analysis. Neither `reviewed-targeted` nor an accepted product decision authorizes deleting a historical document.

## Accepted predecessor behavior, separated from general lessons

| Topic | Accepted historical design | Reusable documentation rule |
| --- | --- | --- |
| Exact review target | One immutable `ProductVariantPublicationRevision`; no implied approval for schema versions, source releases, policy, unpublish or withdrawal | Name exactly what the reviewer examined and the version/scope to which the decision applies; content equality is not a transferable approval |
| Review iterations and comments | One Pending iteration per exact revision, retained Withdrawn/Rejected/Approved history, admin comments while Pending, meaningful rejection reason; no generic resubmission of a rejected unchanged revision | Define initial/new/reopened attempt semantics, decision and comment authority, terminal-state history and durable uniqueness |
| Permission boundary | Specialist submits/withdraws; administrator comments/decides; combined roles allow self-approval; no mandatory four-eyes rule in this product | Separate action permissions from resource eligibility; do not impose specific roles, review separation or access scopes on other projects |
| Decision and effects | Manual Approval authorizes a later F11 Activate; automatic Approval can trigger **durable conditional continuation**, with later activation and Delivery-effective states separately observable | Record acceptance, eligibility, durable continuation, owner-state commit and external visibility as distinct stages; do not show finished status early |
| Approval versus current eligibility | Stored Approved evidence remains attached to the exact revision, but later Withdrawal, dependency validity or scope changes may block a new activation | Historical decision truth and present entitlement to act are different facts; recheck current guards at the state-changing owner |
| Review context and comparison basis | An admin's semantic diff is against a specifically identified current **Delivery-effective** representation (or confirmed absence), not a stale active pointer or the review iteration itself | Capture/revalidate the basis of reviewer consent independently from the reviewed record's own concurrency token; an unavailable read is not confirmed empty |
| Parallel decisions | Competing final decisions must not both commit; one Pending per revision requires durable enforcement, not merely a read-before-write | Test races with a real durable writer and select an appropriate uniqueness/concurrency strategy for that storage engine |
| Retries and operation intent | Manual Submit uses a scoped `ClientOperationId`; a confirmed same-intent replay returns the **original** result, not another iteration or fresh activation; changed meaningful input conflicts | Specify idempotent outcome recovery versus a deliberately new action; never use a new operation ID to guess the result of a lost response |
| Automated review linkage | The F11 preparation atomically binds an exact revision and Pending review to a concrete operation; cancel/supersede does not rewrite human decision history or adopt a different Pending review by hash | If background continuation depends on a human result, bind the exact durable intent and recheck applicability; stale completion must not override a newer actor decision |
| Domain history versus audit | F17 iteration/comments/history remain authoritative for review; F18 activity journal is a different, eventually delivered view | Do not substitute asynchronous audit visibility for a transactional domain decision or treat an absent audit row as a failed action |

**Clarification:** Q28's short automatic-flow wording says the revision is activated *after* approval. The later F17/F11 target qualifies that as a distinct **conditional durable continuation**, not a same-transaction activation or proof of immediate client availability. Do not flatten those stages when generating generic guidance.

## Extracted into the current reusable catalog

The existing [stateful-workflow standard](../../catalog/base/docs/standards/stateful-workflows.md#review-decisions-and-effect-boundaries-when-applicable) now includes a conditional review-decision checklist. It addresses exact target identity, accepted versus performed effects, independent comparison/consent basis, contested final decisions, invalidated Pending state, retained history and replay versus new review attempts.

The [business feature template](../../catalog/base/docs/templates/feature.md#lifecycle-and-history-when-applicable) now prompts for user-visible approval versus later effectiveness only when those stages actually exist. The [technical feature template](../../catalog/base/docs/templates/technical-feature.md#state-and-failure-handling) now prompts for correct decision, live basis, storage and concurrency boundaries without mandating a moderator or broker.

The earlier [specification traceability](../../catalog/base/docs/standards/specification-traceability.md), [data-flow standard](../../catalog/base/docs/standards/data-flows.md) and [F16 identity review](staff-access-evidence-review.md) already distinguish authoritative outcomes, security enforcement and implementation evidence; do not duplicate their normative rules.

**Not adopted globally:** the original project's `ProductVariantPublicationRevision`, `Delivery`, `Publishing`, FSD component tree, NSwag generated model policy, `ICommandBus`, F11 global database gate, two specific roles, permission inheritance/self-approval policy, `ClientOperationId` wire shape, HTTP status map, exact diff algorithm, partial SQL index, ETag-from-int convention or MassTransit topology. Those require a project's own approved contracts and tested technology selections.

## Target design versus current implementation evidence

Backend F17 cites a **2026-10-05 historical source search** in a sibling `contentmanager.api` checkout. That document says existing publishing revision and activation paths were observed, but a moderation persistence root, iteration/comments model, approval gate, controller operations and tests were not found in its scoped search. Frontend F17 similarly says the then-inspected `contentmanager.ui` generated client did not expose F17 operations and the existing Publishing workspace was not a moderation UI.

These statements are **historical claims within the source documents**, not observations repeated in this audit. The referenced source files live outside `docs-boilerplate`, and neither current checkout nor a deployed API/browser was inspected. Q15/Q20 tables express the selected future contract, but are not evidence that a real OpenAPI spec, DB migration, consumer or worker matches it.

| Required before a reusable executable example or source retirement | Current evidence |
| --- | --- |
| Version-pinned implementation and complete API/response-header contract | Not verified here; F17 routes and typed responses remain target designs |
| Real database test: one Pending, terminal-decision races, current dependency guards | Specifications and test plans, not an executed barrier/fault suite |
| Separate owner ETag and foreign comparison-basis drift; distinction between unavailable and confirmed-absent data | Accepted target behavior; no runtime verification |
| Same-intent recovery after lost acknowledgement without new iteration, effect or history | Accepted Q15/Q20 design; not executed |
| Independent review history, F18 journal, and conditional automatic continuation after approval | Planned boundaries; no deployed integration/e2e verification |
| Client UI credentials and role separation, stale state, correction workflow, keyboard and failure recovery | Future frontend acceptance cases, not a browser-tested product |

## Review and deletion outcome

- Retain all four primary sources, their original project-linked contract references and current milestone/ownership semantics. No blanket replacement of old content by universal code is safe.
- Mark only previously screened **business F17, Q28 and frontend F17** as `reviewed-targeted`; backend F17 remains `reviewed-targeted`. Related M06/F11/F18 documents are context, not silently elevated to `reviewed-complete`.
- Before deletion: finish any residual source-specific example evaluation; reconcile cross-repository/missing evidence paths, all inbound links and plain-text references; identify the chosen examples policy; record an individual disposition and pass the [legacy deletion gate](project-source-triage.md).
- A possible optional approval-workflow example/template is an **owner decision**, not an automatically available profile. It is recorded in the [deferred decision backlog](owner-decision-backlog.md). No answer has been assumed.

**Validation boundary:** only the docs and catalog prompts are changed by this migration. It does not prove that F17 exists in a current application, that a database invariant is implemented or that any acceptance test ran.
