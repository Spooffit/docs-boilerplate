---
title: "Resumable data transfers and integrity"
description: "Optional guidance for bounded streaming, integrity checks and recoverable transfers"
kind: standard
status: active
---

# Resumable data transfers and integrity

**Applies when:** a system transfers large payloads, supports partial reads or must resume interrupted transfers. No particular application architecture, storage product or custom framing is implied.

## Transfer contract

Record the adopted protocol, content type, expected size, authorization boundary, byte representation and the component that owns the response and destination. A transfer ID, HTTP validator, byte offset and application record cursor have different meanings. Introduce only identifiers supported by the actual contract.

## Streaming and integrity

Bound memory use and temporary storage; impose size and time limits based on real operating constraints. Where checksums or signatures exist, name the algorithm, trusted source and bytes covered. A checksum establishes neither access rights nor freshness by itself.

Treat incomplete or unverified data as incomplete. Advance durable progress only after the selected validation and persistence boundary succeeds. Specify who disposes of response streams and incomplete files.

## Resume and failure behavior

If HTTP byte ranges are supported, verify the selected representation and actual handling of `If-Range`, `206 Partial Content`, `416 Range Not Satisfiable` and fallback `200 OK`. Do not append a full response to an old partial file or assume a record cursor is a byte offset.

Document timeout, cancellation, retries, cleanup and uncertain completion behavior only for operations the system actually offers. Non-idempotent requests require an established replay or inquiry contract before automatic retry.

## Runtime constraints, when applicable

Where an application runs on multiple platforms, verify streaming, cancellation, resource ownership, concurrency and memory constraints in each actual runtime. A transport callback does not prove data was validated or durably stored.

## Verification

Test truncation, corrupted content, invalid length, stale validator, interrupted read, resumption, memory limits and final-content correctness as applicable. Use the real protocol and target runtime for compatibility claims.

Reference: [RFC 9110 HTTP range semantics](https://www.rfc-editor.org/rfc/rfc9110.html#section-14).
