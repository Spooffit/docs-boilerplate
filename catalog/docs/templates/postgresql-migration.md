---
title: "PostgreSQL migration rollout plan"
description: "Project-specific migration safety, rollout, validation and recovery plan"
kind: template
status: template
---

# PostgreSQL migration: [Approved change]

> Use only for a real migration that warrants a rollout review. Replace placeholders with evidence, not guesses. Read the [PostgreSQL persistence standard](../standards/postgresql/persistence.md).

## Owners, versions and affected resources

- Migration identity and source: [Real repository path or ticket]
- Current schema and deployed application versions: [Verified]
- Database/schema owner and target environments: [Confirmed]
- Data retention, backup and restoration obligations: [Actual policy]

## Existing and target contracts

| Object | Current representation | Planned change | Impact on running consumers |
| --- | --- | --- | --- |
| [Actual object] | [Verified] | [Confirmed] | [Explicit compatibility] |

## Planned steps

1. [Preflight backups, available disk/WAL, existing constraints and lock budget.]
2. [Expand: if compatibility requires.]
3. [Backfill: batches, progress tracking and restartability if required.]
4. [Switch: deployment ordering of all writers/readers.]
5. [Contract: removal after the compatibility/retention gate.]
6. [Post-deployment checks and monitoring.]

## Lock and failure review

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
