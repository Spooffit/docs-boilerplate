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

## Transitions and validation

Document only actual states and transitions. Where both editing and publication exist, distinguish requirements for local input, saved drafts, validation, approval, publication, activation and client visibility. These may be separate operations, but none is universal.

| Action | Required state / actor | Validation | Atomic change | Failure or conflict |
| --- | --- | --- | --- | --- |
| [Confirmed action] | [Existing source] | [Actual rule] | [State affected] | [Observable result] |

Do not claim a successful Save implies publication or that an accepted plan proves deployed behavior.

## Concurrency and stale baselines

If concurrent writers are possible, clarify whether lost updates are prohibited, permitted, or otherwise controlled. Identify the **actual technical token or precondition**, its scope, its source and the server-side recheck. Keep a technical concurrency token separate from a business version number.

For drafts based on snapshots, specify which baseline makes an edit stale. A globally ordered version sequence can have a different scope than the baseline used for conflict detection. Describe whether Save, Publish and Rebase use different rules. Never promise automatic merging or conflict-free retries without evidence.

## Destructive changes and retention

For applicable operations, explicitly distinguish physical Delete, Archive, Retire, Withdraw/Revoke and Restore. Each may affect new authoring, publication, existing clients and historical reads differently. Do not mandate these operations if the project does not have them.

If an operation can delete dependent objects, describe the exact affected set, user confirmation, changes between preview and commit, transaction boundary and retained history. Comparing only the **count** of dependencies does not prove the same dependencies were confirmed. Do not prescribe arbitrary delays or cascades without a decision.

## Copying and schema evolution

If records can be copied between evolving schemas, document the matching rule (stable ID, path or an explicit migration), missing-versus-null behavior, incompatible values, orphaned data and how the user can resolve them. Do not invent implicit timezone conversion, type coercion, migration or backfill.

## Verification

For the stateful flows that exist, check competing edits, stale operations, original historical reads, retries after uncertain outcomes, changed destructive impact and partial batches. Connect every claim to actual SUT/test evidence; a precise specification is not proof of implementation.

See [data flows](data-flows.md) for transaction/effect boundaries and [traceability](specification-traceability.md) for accepted-versus-proposed decisions.
