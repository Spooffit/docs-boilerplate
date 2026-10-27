---
title: "Versioned lifecycle and concurrency targeted extraction"
description: "Review of six historical source files for portable state, concurrency and history documentation principles"
kind: audit
status: active
---

# Versioning and lifecycle: targeted extraction

- Date: 2026-10-27
- Scope: `business/features/F02-create-schema-version.md`, `F03-publish-schema-version.md`, `F04-entity-instances.md`; `business/questions/Q08-keys-and-drafts.md`, `Q09-editing-and-deletion.md`, `Q36-entity-instance-lifecycle.md`.
- Method: studied versioning, transition, conflict, copying, deletion and acceptance sections against current catalog standards. This is a **targeted semantic review**, not a line-by-line audit of all technical/API mirrors and not proof that source-code behavior matches these decisions.
- Inventory: six entries promoted from `screened-structure` to `reviewed-targeted`, with no deletion authorization.

## Findings and dispositions

| Original sources | Observed original-project requirements | Reusable lesson |
| --- | --- | --- |
| F02 + Q08 | Multiple Drafts; Published baseline; Draft Save can be allowed when its Publish is blocked; business VersionNumber is distinct from optimistic token | Describe identity, baseline scope, stale edit policies and conflict outcomes without assuming a specific versioning scheme |
| F03 | Atomic publish gate verifies current basis, immutable result and sequential number; concurrent publishes can conflict | Explicitly separate validation, transition, immutable snapshot, transaction boundary and visibility |
| F04 + Q36 | Logical instance vs version ID; copying between schema versions, orphan/missing values; independent per-target freshness and global business numbering | Document copy matching, retained history and distinct sequence/concurrency scopes |
| Q09 + F02 | Previewed destructive dependency set can change before commit, even if the count stays equal; Retire, Archive and Withdraw have different meanings | Specify impact revalidation, history and lifecycle semantics instead of defaulting to CRUD |

## Implemented in the reusable catalog

- [Stateful workflow review](../../catalog/base/docs/standards/stateful-workflows.md), conditional on actual project needs.
- [Data-flow guidance](../../catalog/base/docs/standards/data-flows.md) points to that review.
- [Feature template](../../catalog/base/docs/templates/feature.md) and [technical feature template](../../catalog/base/docs/templates/technical-feature.md) add optional transition/history prompts.

## Deliberately not migrated

The original documents prescribe product-specific Draft/Published/Withdrawn states, fixed field identity rules, five-second confirmation, schema counters, hashes, temporal codecs and copy policies. None becomes a universal requirement. The historical files remain intact until detailed technical-contract review and an owner decision on retained examples.
