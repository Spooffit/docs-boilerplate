---
title: "Versioned representations and resumable transfers"
description: "Document immutable snapshots, canonical bytes, transfer checkpoints and client/server conformance"
kind: standard
status: active
---

# Versioned representations and resumable transfers

**Applies when:** clients or services exchange pinned/versioned snapshots, content-addressed data, resumable streams or partial downloads. Do not assume a project needs content hashing, custom NDJSON transport, Range support or client-side installation.

## Independent compatibility axes

Define and track separately, when present:

- **Resource version:** which business or domain state is being requested.
- **Representation/profile version:** how canonical content bytes and semantic values are interpreted.
- **Transport/protocol version:** message framing, envelopes, cursors and error responses.
- **Client SDK/schema version:** supported readers, generators and public APIs.
- **Execution generation/lease:** which worker currently owns mutable transfer work; not itself a content version.

A changed JSON field, ordering, default interpretation, date/time meaning or canonical byte encoding may require a new representation profile. Updating code or a library package does not automatically change a content profile; conversely a semantic change cannot be hidden under an old identifier.

Preserve exact historical representations when compatibility promises demand it. Do not silently reserialize or re-hash old published content with current rules.

## Representation and integrity contract

If content hashes are contractually significant, specify algorithm, normalized input, character encoding, property ordering, number representation, duplicate-key handling, null/missing/default differences, canonical timestamps, binary-to-text encoding and digest format. Provide literal golden vectors to check every SDK and language implementation against the same bytes.

Clarify *what* the digest proves. Content integrity is not user authorization, source authenticity, freshness or installation success by itself. Never invent an HMAC/signature scheme merely because a SHA-256 hash exists.

## Pinned snapshot versus mutable execution

If a transfer uses a generated plan, state whether selection and target content remain immutable throughout the session. Keep retry/worker generations, leases, progress and heartbeat distinct from the pinned data identity.

Document start/status/data/commit transitions **only if the actual protocol exposes them**. A plan identifier is not necessarily a network connection, commit receipt, stream offset or credential.

## Resume and durable checkpoints

Distinguish **bytes received**, **complete record parsed**, **record validated** and **record durably staged**. If correctness depends on durable replay, move the application checkpoint only after the required durable stage succeeds. A TCP fragment, callback or parsed-but-not-saved record is not durable progress.

Define whether the resume token is opaque, bound to a pinned plan and exact record sequence, or a byte offset in a particular representation. Do not use an application cursor as an HTTP byte range unless the specified transport deliberately maps one to the other.

If HTTP byte-range resumes are supported, specify the representation validator and handling of `206 Partial Content`, `416 Range Not Satisfiable`, `If-Range` and unexpected full `200 OK` responses. Range is an **optional HTTP feature**, not an automatic guarantee from using GET.

For streamed batches, specify maximum record sizes, incremental parsing, replay/duplicate records, record identity and integrity checks, and what happens after partial failure. A repeated sequence with changed content must not silently overwrite a verified staged record.

## Atomic installation and visible result

Clarify what is committed together: staged data, installed snapshot, group membership, removals and receipts. State which old version remains visible while a new group is incomplete or fails. Receipt acceptance does not prove a different client's local media or all downstream consumers are ready.

Verify cancellation, expiry, worker takeover, stale generation, interrupted streams, mismatched digests, replays, missing records and cross-version client compatibility against real implementations.

References: [RFC 9110 HTTP range semantics](https://www.rfc-editor.org/rfc/rfc9110.html#section-14), [stateful workflow review](../../stateful-workflows.md).
