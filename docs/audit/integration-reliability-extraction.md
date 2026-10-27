---
title: "Reliable synchronization and resumable contract extraction"
description: "Scoped semantic review of source-replication, canonical content, persistence and transfer decisions"
kind: audit
status: active
---

# Reliable synchronization and versioned transfer extraction

- Date: 2026-10-28
- Original source files: `Q19-content-hashes.md`, `Q20-persistence-and-migration.md`, `Q23-source-synchronization.md`, `Q30-content-synchronization.md` and `Q33-sync-resume-transport.md`, all under `technical/backend/questions/`.
- Review method: read the source introductions, selected contract/decision subsections, lifecycle and recovery descriptions. This was **targeted semantic extraction** of integration and synchronization boundaries. Long source documents (especially Q20) were **not** audited exhaustively, and any code-level claims are not verified against a running original application.
- Inventory result: five entries become `reviewed-targeted`, **not** `reviewed-complete`. All originals remain preserved.

## Reviewed source decisions and generalizable lessons

| Original document | Selected source content | What the new boilerplate retains |
| --- | --- | --- |
| Q23: Source synchronization | Snapshot events distinguish `EventId`, per-entity `Version` and observed/applied state; duplicate identity does not prove source freshness; contradictions go to durable fault handling; reconciliation republishes source snapshots | Separate transport identity, source-state version and contract version, with explicit stale/equal/conflicting input handling and reconciliation completeness guarantees |
| Q20: Persistence and migration | Receiver ACK after durable receive work; worker may apply projection separately; cross-owner facts require fresh observation; retries/leases/outbox have specific atomic boundaries | Name receipt versus projection application, transaction/ACK boundaries, durable work and error recovery; never presume one transaction spans services |
| Q19: Content hashes | Separate canonical content profiles, protocol versions and codegen versions; exact bytes, format evolution, older readers and literal test vectors are necessary to claim compatibility | Document independent version axes and byte-level conformance; do not assume hashing guarantees source authenticity, authorization or freshness |
| Q30: Content synchronization | Pinned snapshot and synchronization identity differ from mutable execution; server plans and client installed state have different owners | Separate immutable plan/data from progress, retries, worker state, credentials and client's installed result |
| Q33: Resume transport | Durable staging precedes application cursor advances; replay needs exact record identity/sequence; execution generation protects worker takeover; an HTTP stream fragment is not application progress | Explicit checkpoints, replay semantics, representation validators, stale workers and installed-state atomicity, without mandating the original HTTP routes |

## Reusable outputs

- [Reliable state synchronization](../../catalog/docs/standards/integrations/reliable-synchronization.md), covering ownership, duplicate/stale inputs, receive/apply and reconciliation.
- [Versioned representations and resumable transfers](../../catalog/docs/standards/integrations/versioned-transfers.md), covering canonical profiles, immutable plans, checkpoints, HTTP range as an optional separate feature and actual end-to-end test evidence.
- `integration-reliability`: a **standalone opt-in** technology-neutral profile with these two files. It does not implicitly select the source project's broker, ORM, language, publication lifecycle, storage or SDK.
- `catalog/modules/integrations.md` now suggests the optional profile where the project actually synchronizes independent durable state.

## Deliberately not transplanted

The original source defines Content Manager/VR School product entities, endpoints, event DTOs, version field widths, error codes, EventId retention, exact canonical bytes, hashing profile names, synchronizationId, custom cursor and NDJSON records, HMAC parameters, lock keys and worker-generation protocol. These remain **original-project contracts only**.

Do not convert the old `ProductSnapshotV2`, `cm-*/1` profile IDs, `Start/Data/Commit/Status` URL shapes or cross-BC advisory-lock policy into a universal API. The new standards ask implementers to document and prove *their own* chosen contracts.

## Independent verification

- [MassTransit outbox](https://masstransit.io/documentation/patterns/transactional-outbox) and [AWS outbox pattern](https://docs.aws.amazon.com/prescriptive-guidance/latest/cloud-design-patterns/transactional-outbox.html) support the outbox/duplicate delivery distinction.
- [RFC 9110, Range Requests](https://www.rfc-editor.org/rfc/rfc9110.html#section-14) is relevant only where the actual HTTP representation supports range resumes; application-specific cursors are separate.
- GitHub Actions verifies the new profile composes and generates valid documentation without importing any implicit .NET, ORM or broker profile. It **does not** prove the original synchronization protocol was implemented.

## Remaining before removal of the original sources

Assess the technical consumers, actual schemas and source contracts (including Q15 and F13), check cross-document incoming links, and decide whether any highly specialized canonical byte fixtures or SDK examples are worth preserving in a clearly isolated optional example. Owner judgment is required before deleting substantive historical source documents.
