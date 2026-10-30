---
title: "Reliable state synchronization and reconciliation"
description: "Baseline principles for delivery, ordering, deduplication and recovery across independent systems"
kind: standard
status: active
---

# Reliable state synchronization and reconciliation

**Applies when:** independently owned components exchange data and must recover from missing, duplicate, delayed or conflicting updates. It does not require events, entity version numbers, snapshots, a message broker or any particular persistence technology.

## Authority and consistency contract

Identify the authoritative source, consuming systems, data scope, update mechanism and required consistency. Document whether updates are complete representations, changes, notifications or queries; these mechanisms have different recovery requirements. Do not create a versioned business-entity model merely to satisfy this checklist.

If the adopted contract has message identifiers, revisions or watermarks, specify their meaning and ordering scope. A delivery identifier, source revision, timestamp and schema version are distinct concepts. Where none exists, document the actual conflict-resolution and recovery mechanism rather than inventing counters.

## Handling repetition and disorder

Define behavior for only the cases the selected transport and contract can encounter: duplicate delivery, older or reordered data, conflicting facts, unavailable dependencies and unsupported payload formats. A replay-safe consumer must not repeat non-idempotent effects outside its documented deduplication guarantees. Finite retention cannot provide infinite duplicate detection.

For incremental changes, determine whether processing requires predecessors or gap detection. For complete source representations, missing contiguous revision numbers may be irrelevant. Do not assume either approach by default.

## Persistence and failure boundaries

Specify where incoming work becomes durable, when it is acknowledged and how incomplete processing resumes. If local state changes and outgoing messages must be coordinated, review the dual-write risk. Transactional outbox, consumer inbox and broker retries address different concerns; none establishes universal exactly-once execution.

Do not assume one transaction covers a database, another system and a message broker. A timeout or lost acknowledgment does not prove the earlier operation failed. Use the real idempotency or status-inquiry contract before repeating a state-changing request.

## Reconciliation, when required

Choose a repair strategy appropriate to the adopted source: reread authoritative records, replay a retained log, request missing changes or compare complete datasets. **Absence in a partial response is not proof of deletion.** Document completeness guarantees and the authorization and freshness checks required before destructive reconciliation.

If workers can race or resume after a crash, verify that recovered work still targets the correct source state before applying it. Document retention, checkpoints, retries and manual intervention only where supported by the implementation.

## Verification

Test the relevant failure modes with actual transport and storage boundaries: duplicate or reordered input, partial failures, crash before acknowledgment, retry after uncertain results, conflicting versions if present and recovery after retention limits. Distinguish planned checks from executed evidence.

References: [MassTransit outbox](https://masstransit.io/documentation/patterns/transactional-outbox), [AWS transactional outbox guidance](https://docs.aws.amazon.com/prescriptive-guidance/latest/cloud-design-patterns/transactional-outbox.html), [data-flow documentation](../data-flows.md).
