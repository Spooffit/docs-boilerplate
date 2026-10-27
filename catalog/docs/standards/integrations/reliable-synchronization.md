---
title: "Reliable state synchronization and reconciliation"
description: "Technology-neutral guidance for snapshot consumers, deduplication, state freshness and recovery"
kind: standard
status: active
---

# Reliable state synchronization and reconciliation

**Applies when:** independent services or clients synchronize durable source state by events, snapshots, API polling or replay. This standard is **optional** and does not require RabbitMQ, MassTransit, PostgreSQL, any specific event schema or a distributed architecture.

## Authority and version scope

Document the authoritative owner of each source entity, the consumers permitted to maintain projections, the scope of source-version ordering, and the semantics of a full snapshot versus a delta. An event's transport identity and a source entity's **state version** answer different questions:

- **Event/message identity** determines whether the same delivery is being retried.
- **Source version or watermark** determines whether a candidate state is newer than one already accepted.
- **Occurred-at timestamp** is useful for diagnostics but must not silently substitute for an agreed version ordering.
- **Contract/schema version** describes the payload interpretation, independently of the state version.

Do not assume contiguous versions when the contract carries full snapshots. With deltas, document gaps, required predecessors and gap recovery explicitly. Distinguish *received*, *durably accepted*, *applied* and *visible* if the implementation separates these stages.

## The acceptance decision

Define the actual behavior for the following cases. The table is a review checklist, **not** an algorithm that every system must implement.

| Input | Documented question | Potential risk |
| --- | --- | --- |
| Same message identity, same immutable facts | Is replay acknowledged without repeating side effects? | Duplicate effects |
| Same message identity, contradictory facts | Is the conflict quarantined or rejected? | Identity reuse hides corruption |
| New message, older entity state | Does the consumer ignore or reconcile stale input? | State regression |
| Equal entity version and equal state | Is re-publication a safe no-op? | Unnecessary work |
| Equal entity version and conflicting state | What is the authoritative resolution path? | Silent overwrite |
| Higher version with unavailable dependency | Is pending work durable and resumable? | Lost update |
| Unsupported contract version | Is processing blocked with a meaningful diagnostic? | Misinterpreted data |

Choose deduplication retention and its failure/expiry behavior based on delivery guarantees. Do not claim that a finite inbox can recognize every duplicate forever.

## Durable boundaries and side effects

When durable projection changes and message consumption must agree, specify the atomic boundary: candidate/receipt/work state, derived projection state, and any outgoing message intent. Acknowledge incoming delivery only when the **required durable receive boundary** succeeds. Worker processing can be a separate stage, but then pending work and recovery must be durable.

A source database change plus network publish is a dual-write risk. A transactional outbox is one possible design; it can still deliver duplicate messages and requires idempotent consumers. Retry, redelivery, consumer inbox, in-memory outbox and transactional outbox serve **different purposes** and must not be called interchangeable exactly-once delivery.

Never assume that a single local database transaction spans separate stores, a broker or another service.

## Missing dependencies and reconciliation

Specify whether an out-of-order child state waits for a parent, is rejected or triggers repair. Define how a new candidate supersedes obsolete pending work without allowing an old worker to commit stale results.

A reconciliation process may republish authoritative snapshots, perform a complete listing with an explicit completeness marker or use a separately versioned repair API. Choose one only after documenting the actual contract. **Absence from a partial snapshot set is not proof of deletion**. Explicit lifecycle changes, a complete authoritative snapshot boundary or equivalent evidence are required before removing projected state.

Distinguish reconciliation being *scheduled* from source messages being *published*, consumer updates being *applied* and final state being *verified*.

## Durable operation receipts and staged continuation (when applicable)

Some multi-stage workflows need **command execution identity** independently of the entity, message, event or content hash. If an API supports original-result replay after an uncertain response, document the operation's scope, trusted actor, request identity, meaningful input comparison, retained original result and durable commit boundary.

A confirmed retry of the *same* accepted operation should not silently perform a fresh Create, publish, moderation, history append or other business mutation. A new user intent may legitimately create a second artifact with equal content, so content hashes must not be used as universal deduplication keys. Define operation-retention limits and the behavior when the original outcome is no longer known.

For workflows divided by a job, human approval, broker, or independent owner, identify each stage's durable identity/inputs, terminal outcomes, continuation prerequisites and separately committed side effects. If a worker crashes or another writer supersedes its input, the resumed worker must verify its accepted correlation and actual current constraints before changing state. A later stage cannot reuse a previous transaction's lock or assume another owner's facts remained fresh.

An owning resource token and a separate *user-visible comparison basis* may have different freshness semantics. Confirm business consent against the relevant versioned inputs when the actual use case requires it; matching own `ETag` alone does not prove cross-owner consistency.

## Operational evidence

Record poison/quarantined events, unresolved dependencies, retry budgets, last received/applied versions, lag, reconciliation attempts and recoverable checkpoints as appropriate. Do not expose credentials, raw personal data or sensitive event payloads through diagnostics.

Test out-of-order arrivals, duplicate event identities, contradictory equal versions, crash between receipt and apply, missing parents, replay after retention expiry and recovery from unacknowledged side effects. Use the actual transport and persistence systems for integration guarantees.

References: [MassTransit outbox concepts](https://masstransit.io/documentation/patterns/transactional-outbox), [AWS transactional outbox guidance](https://docs.aws.amazon.com/prescriptive-guidance/latest/cloud-design-patterns/transactional-outbox.html), [data-flow documentation](../data-flows.md).
