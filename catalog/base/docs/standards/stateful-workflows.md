---
title: "Reviewing stateful workflows and versioned resources"
description: "Conditional checklist for resource states, immutable snapshots, stale edits, conflicts and history"
kind: standard
status: active
---

# Reviewing stateful workflows and versioned resources

**Applies when:** a project actually has mutable/immutable versions, multiple editors, publication stages, or dependent resources that may be removed. This document describes how to **specify and review** such behavior. It does not require projects to implement drafts, publishing, event sourcing, or a particular storage technology.

## Identity and history

For each resource, distinguish logical identity, individual version identity, human-facing label/key and any ordered business version. Define which IDs are stable, uniqueness scope and when any sequence or number is assigned.

When historical snapshots exist, identify what is immutable, how previous representations remain readable, and whether format/profile changes require new readers. Do not infer hash formats or canonical serialization unless a real adopted contract defines them.

## Durable snapshots and retained history

For systems that actually persist immutable revisions, distinguish the **snapshot's owned content**, its immutable identity, the active pointer, and any separately stored history. Materialize a stable copy of mutable inputs before the snapshot is made durable; an immutable wrapper around a mutable collection is not a durable snapshot guarantee.

Do not expose an active/ready pointer before all required snapshot parts are committed. When pointer changes and history entries describe one logical transition, specify how they remain consistent after failure. Use one atomic transaction where the stores permit it, or explicitly model pending/failed states and recovery where they do not. A successful intermediate write must not masquerade as a completed transition.

Before deleting or replacing dependencies needed for historical reads, establish the actual retention contract and references. Deletion or ownership checks performed only in memory are insufficient if parallel writers can change the protected set before commit. Define which uniqueness, ownership, referential and concurrency guarantees are enforced at a durable boundary and how conflicts are observed.

## Transitions and validation

Document only actual states and transitions. Where both editing and publication exist, distinguish requirements for local input, saved drafts, validation, approval, publication, activation and client visibility. These may be separate operations, but none is universal.

| Action | Required state / actor | Validation | Atomic change | Failure or conflict |
| --- | --- | --- | --- | --- |
| [Confirmed action] | [Existing source] | [Actual rule] | [State affected] | [Observable result] |

Do not claim a successful Save implies publication or that an accepted plan proves deployed behavior.

## Review decisions and effect boundaries (when applicable)

Some workflows require a human or automated **review/approval decision** before a later state transition. Document this only when it is an actual approved business requirement, not merely because the repository has a generic feature template.

- Define the **exact object and revision** reviewed. Equal field values or hashes are not automatically evidence that a different revision was reviewed. Distinguish a new review attempt from reopening a completed decision; record accepted transitions, reviewer authority, required reasons and retained history.
- Separate **decision recorded**, **action eligible**, **action performed**, and **externally effective/visible**. Approval may authorize a later action without performing it. Conversely, a selected automatic workflow may schedule a separate durable continuation. Do not claim that an emitted event, accepted decision or successful HTTP response proves the eventual effect.
- Capture any **comparison or consent basis** shown to a reviewer: which representation/source was compared, its identity or explicitly confirmed absence, and its freshness. That basis may change **without** changing the review record's own version/ETag. If a decision is invalid after such a change, recheck the basis at the enforcing boundary and require new consent rather than silently substituting newer data. Unavailable or incomplete reference data is not confirmed absence.
- Establish a protected, durable **single-decision boundary** for competing reviewers. An initial read, client-side disabled control or optimistic UI status cannot by itself prevent duplicate/contradictory terminal decisions; use constraints, concurrency control or a documented coordination mechanism matching the chosen store.
- If a reviewed object becomes withdrawn, blocked or otherwise ineligible, specify whether the **pending decision remains in history**, which actions are still allowed and whether earlier approvals are retained but unusable. A later eligibility check must not rewrite original review evidence.
- For retries after uncertain outcomes, distinguish a replay of an **already committed intent** from a genuinely new review iteration. Scope any idempotency key to the selected actor, operation and meaningful inputs. A fresh request with the same payload is not automatically an authorized continuation.

Keep these concepts technology-neutral. Do not impose moderation, an approval UI, four-eyes review, any particular status vocabulary, a fixed response code or a broker on a project that has not selected them.

## Concurrency and stale baselines

If concurrent writers are possible, clarify whether lost updates are prohibited, permitted, or otherwise controlled. Identify the **actual technical token or precondition**, its scope, its source and the server-side recheck. Keep a technical concurrency token separate from a business version number.

For drafts based on snapshots, specify which baseline makes an edit stale. A globally ordered version sequence can have a different scope than the baseline used for conflict detection. Describe whether Save, Publish and Rebase use different rules. Never promise automatic merging or conflict-free retries without evidence.

## Destructive changes and retention

For applicable operations, explicitly distinguish physical Delete, Archive, Retire, Withdraw/Revoke and Restore. Each may affect new authoring, publication, existing clients and historical reads differently. Do not mandate these operations if the project does not have them.

If an operation can delete dependent objects, describe the exact affected set, user confirmation, changes between preview and commit, transaction boundary and retained history. Comparing only the **count** of dependencies does not prove the same dependencies were confirmed. Do not prescribe arbitrary delays or cascades without a decision.

## Copying and schema evolution

If records can be copied between evolving schemas, document the matching rule (stable ID, path or an explicit migration), missing-versus-null behavior, incompatible values, orphaned data and how the user can resolve them. Do not invent implicit timezone conversion, type coercion, migration or backfill.

## Typed-definition evolution and old snapshots (when applicable)

For systems that store user-defined field structures or evolving document schemas, distinguish stable logical field identity from a display label, runtime key/type and container path. Copying a definition, resurrecting a removed field and introducing a new field with a familiar name are different operations; document which preserve historical identity and why. Do not infer equivalence from equal labels or equal values.

When constraints are strengthened or a previously valid option is deprecated, define the behavior of old immutable snapshots, current drafts and new publications separately. A deprecated value may need to remain readable even when new authoring discourages it. A copy into a newer definition may preserve a type-compatible value while reporting a new constraint violation; do not silently drop, coerce or reinterpret it. Any conversion between naive date/time and an offset-aware instant needs an explicit approved rule, not a local-time fallback.

If a schema allows drafts that are less complete than a publishable version, keep their validation statuses and enforcement boundaries distinct; this is conditional on the real product workflow, not a universal template requirement.

## Verification

For the stateful flows that exist, check competing edits, stale operations, original historical reads, retries after uncertain outcomes, changed destructive impact and partial batches. Connect every claim to actual SUT/test evidence; a precise specification is not proof of implementation.

See [data flows](data-flows.md) for transaction/effect boundaries and [traceability](specification-traceability.md) for accepted-versus-proposed decisions.
