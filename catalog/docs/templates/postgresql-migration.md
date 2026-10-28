---
title: "PostgreSQL migration rollout plan"
description: "Project-specific migration safety, rollout, validation and recovery plan"
kind: template
status: template
---

# PostgreSQL migration: [Approved change]

> **Use:** Optional plan for a PostgreSQL migration where operational impact, compatibility, data loss or rollout risk justify a separate review. Not every schema change needs this document. Retain only applicable steps and evidence; read the [PostgreSQL persistence standard](../standards/postgresql/persistence.md).

## Owners, versions and affected resources

- Migration identity and source: [Real repository path or ticket]
- Current schema and deployed application versions: [Verified]
- Database/schema owner and target environments: [Confirmed]
- Data retention, backup and restoration obligations: [Actual policy]

## Existing and target contracts

| Object | Current representation | Planned change | Impact on running consumers |
| --- | --- | --- | --- |
| [Actual object] | [Verified] | [Confirmed] | [Explicit compatibility] |

If this migration changes typed-value interpretation, explicitly compare old/new treatment of `NULL`, omitted input, Unicode length units, decimal precision, timezone/offset and any JSONB payload schema. A storage type conversion is not proof that historical values retain their meaning; document the conversion authority and unknown values before backfill.

## Planned change and rollout

[Document the actual migration sequence. Consider backup/restore, lock impact, data backfill, application compatibility and post-change verification **as applicable**. Expand/backfill/switch/contract is an optional technique for particular compatibility requirements, not a mandatory six-step plan.]

## Lock and failure review (when material)

| SQL operation | Lock/timeout risk | Rewrites or scans | Transaction-block support | Recovery |
| --- | --- | --- | --- | --- |
| [Statement] | [Specific PostgreSQL behavior] | [Actual estimate] | [Yes/no] | [Documented response] |

`CREATE INDEX CONCURRENTLY` cannot run in a transaction block. Verify invalid indexes after interrupted builds. Check whether `NOT VALID`/`VALIDATE CONSTRAINT` applies to this migration.

## Readiness and evidence

| Gate | Actual environment/test | Required proof | Recorded result |
| --- | --- | --- | --- |
| Production-like data | [Real fixture or restore] | [Preserved invariants] | Not yet verified |
| Old and new consumers | [Actual versions] | [Compatibility] | Not yet verified |
| Lock/latency impact | [Representative load] | [Accepted budget] | Not yet verified |
| Partial failure and recovery | [Scenario] | [Safe resume] | Not yet verified |
| Post-deploy constraints | [Actual SQL/check] | [Agreed state] | Not yet verified |

## Approval, rollout and rollback

[Record approved timing, real decision authorities, rollback/forward-recovery strategy and unresolved risks. The plan alone is not deployment evidence.]
