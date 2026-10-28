---
title: "Documenting transport and internal data flows"
description: "Reusable structure for external interactions, state changes and failure boundaries"
kind: standard
status: active
---

# Documenting transport and internal data flows

**Applies to:** technical specifications involving external calls, events, long-running work, persistence or meaningful cross-component interaction.

## Two separate descriptions

A **transport flow** documents the externally observable contract: who initiates it, how it enters, the authorized operation, input contract, output, success and failure semantics. An **internal flow** describes the implementation path: validation, state reads, business invariants, commits, external effects and recovery.

Link the two by a stable local flow name or identifier where needed. Never duplicate the entire machine-readable API schema inside a flow table.

## Transport interaction summary

| Interaction | Initiator and trust boundary | Transport / destination | Source contract | Authorization | Observable outcome |
| --- | --- | --- | --- | --- | --- |
| _Describe verified behavior_ | _Actual actor_ | _HTTP, broker, job, etc._ | _Link existing schema_ | _Adopted policy_ | _Success and expected failure_ |

Do not label a local domain event as an external broker message unless it is actually transported. An outbox write is not necessarily an independent network transport.

## Flow inventory and local traceability

For multi-stage features, an optional **local flow identifier** (for example `DF01`) can connect the external interaction, detailed implementation steps, unanswered requirements and proof/test evidence. Do not force globally numbered IDs on unrelated documents; choose an identifier system only if the project has adopted one.

When the contract is complex, the transport inventory should make it possible to locate: initiator, transport/channel and destination, authentication/permission policy, route/query/body/header or message payload, application action, expected response or event, observable success and meaningful failure. Name the real trigger/completion condition for jobs and background processing. Treat outbox persistence as a durable stage, **not automatically an independent network delivery**. Never infer that an in-process domain event is a broker publication.

A use-case flow can link to authoritative DTO, message and API schemas rather than duplicate them. Keep the transport inventory distinct from the implementation table: the latter names real state owners, reads, transformations, guards, commits, outgoing effects and failure recovery. Do not manufacture a five-step command/transaction workflow for a simple read.


## Internal data and state flow

For nontrivial cases, describe **only** the steps required by the actual use case:

1. Identify the input and the trusted context available to the handler.
2. Record necessary state reads and their staleness/concurrency assumptions.
3. Identify business checks, transformations and the owner of each invariant.
4. Mark each atomic transaction, commit and persistence boundary.
5. Record subsequent effects (messages, HTTP calls, file/object storage) and how failure is handled.
6. Describe when the caller can observe the result and whether a timeout can leave an unknown outcome.

A DB commit and a broker publish are not magically one atomic event. Multiple stores have independent commits unless a concrete coordination mechanism says otherwise. Do not claim exactly-once delivery by default.

## Stateful transitions

Where a workflow includes edits, immutable versions, publication or destructive changes, apply the [conditional stateful-workflow checklist](stateful-workflows.md) rather than assuming a basic CRUD lifecycle.

## Input trust and failure semantics

For each external operation, separate **transport validity** (format, length, encoding and allowed characters), **application authorization and state checks** (existence, ownership and allowed transition), and **durable invariants** (constraints that must survive concurrent writes). A syntactically valid identifier does not prove ownership; a checksum does not prove authorization; a claimed schema reference does not prove the payload conforms to that schema.

Document normalization explicitly: allowed alphabet, case or Unicode normalization, whitespace handling and missing-versus-null behavior must match the actual producer/consumer contract. Do not silently coerce invalid input into another business meaning.

Distinguish invalid input, denied access, an unknown resource, a stale state, a result not ready yet, a conflicting write and an unavailable dependency. Define which failure is observable to the caller and which internal details remain private. An exception from infrastructure is not automatically a public error contract. Do not return a successful empty response on failure.

For batch operations, specify whether processing is atomic or partial and make any per-item outcomes observable. An operation that promised all-or-nothing semantics must not acknowledge partial completion as success. A timed-out write or missing response is an **unknown outcome**, not proof that a commit did not occur.

## Conditional history and audit effects

Where a requirement calls for durable history of confirmed actions, identify the **source commit** that makes a fact true and the point when that fact reaches its history reader. A queued event, dispatched message or optimistic UI row is not a completed audit append. The history projection may lag without undoing the original committed operation. Specify stable fact identity, idempotent append, changes under replay, failure recovery and the distinction between one stage's committed result and an entire multi-stage workflow. An ordinary observability log is not automatically the authoritative history; do not require an activity subsystem when the project has no such requirement.

## Repeat and recovery semantics

For flows where retries matter, document idempotency scope, deduplication window, message acknowledgements, ordering, stale input and partial failure recovery. Explain how users or operators distinguish a failed write from a lost acknowledgement.

A client cancellation or HTTP timeout does not by itself prove server-side rollback.

## Traceable outcomes and unfinished design

For each high-risk behavior, link the actual input flow to the corresponding internal state/effect boundary and the suite that proves its **observable** result. A verification matrix can record flow ID, scenario, SUT, isolated setup, expected outcome and actual test/evidence. Classify tests by the system exercised, not by whether their class names contain “Integration” or “E2E”.

For reads, include filter/order/page/snapshot freshness when they materially affect correctness; read models may lag the authoritative writer. For writes, distinguish pre-commit cancellation, commit failure and cancellation after durable commit, plus the caller's knowledge of each outcome. For multi-store flows, list separate commits, compensation/reconciliation responsibilities and how partial progress becomes visible. Receiving a message, committing outbox intent, broker delivery, consumer durable acceptance and external visibility are different milestones.

Use **Unresolved** with a concrete issue reference for missing decisions and **Not applicable** for consciously absent mechanisms. Neither an unapproved suggestion nor an unverified implementation claim can silently become an accepted contract. A diagram or arrow sequence may supplement a table but is not evidence that the flow exists.


## Evidence and unknowns

Relate each high-risk flow to tests of its real SUT boundary. Name real contracts or explicitly record unresolved decisions; never invent queue names, URLs, DTO types, permissions or timings merely to fill a template.

Review existing interface and state documentation before duplicating details.
