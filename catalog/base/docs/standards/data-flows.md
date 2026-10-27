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

## Repeat and recovery semantics

For flows where retries matter, document idempotency scope, deduplication window, message acknowledgements, ordering, stale input and partial failure recovery. Explain how users or operators distinguish a failed write from a lost acknowledgement.

A client cancellation or HTTP timeout does not by itself prove server-side rollback.

## Evidence and unknowns

Relate each high-risk flow to tests of its real SUT boundary. Name real contracts or explicitly record unresolved decisions; never invent queue names, URLs, DTO types, permissions or timings merely to fill a template.

Review existing interface and state documentation before duplicating details.
