---
title: "Resumable data transfers and integrity"
description: "Optional guidance for streaming, content integrity, durable checkpoints and recovery"
kind: standard
status: active
---

# Resumable data transfers and integrity

**Applies when:** a project streams large data, supports partial downloads or requires recoverable transfer. This document does **not** require immutable business snapshots, entity-version lifecycles, a content registry, custom NDJSON framing or an SDK.

## Transfer contract

Document the actual transport, content type, size limits, credentials and component responsible for the body and destination. If the protocol defines a validator, transfer identifier or representation version, describe its meaning. A request identifier, byte offset and application cursor are not interchangeable.

## Integrity and bounded buffering

Where a checksum or signature is used, define the algorithm, expected source and exact bytes covered. A checksum cannot independently prove authorization or freshness. Use bounded buffering, incremental parsing and size limits. Verify complete units before marking progress as durable.

## Resume and recovery

Advance an application checkpoint only after the required bytes or records are validated and safely stored. If HTTP ranges are supported, verify `If-Range`, `206 Partial Content`, `416 Range Not Satisfiable` and unexpected full `200 OK` behavior for the actual representation.

For incomplete transfers, document retry, temporary-file cleanup and outcome confirmation after a lost acknowledgement. Do not auto-repeat a non-idempotent request merely because the client timed out. Do not expose partial data as an accepted complete result.

## Portable-client execution and platform limits (when applicable)

When one protocol implementation serves multiple runtimes, each adapter must preserve the agreed semantics for header availability, response ownership, single-reader streams, backpressure, cancellation and disposal. A callback delivering bytes is not proof they were parsed or committed.

Verify the selected runtime's actual native dependencies, memory limits and threading constraints. Compilation against a shared interface does not prove compatibility with a restricted, ahead-of-time or browser environment.

## Verification

Test truncated transfers, invalid lengths, mismatched checksums, stale validators, cancellation, timeouts, retry limits and safe resume using the selected transport and runtime.

References: [RFC 9110 HTTP range semantics](https://www.rfc-editor.org/rfc/rfc9110.html#section-14), [data-flow documentation](../data-flows.md).
