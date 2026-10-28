---
title: "HTTP client contracts and reliable request handling"
description: "Language-neutral guidance for contract fidelity, access control, preconditions and retries"
kind: standard
status: active
---

# HTTP client contracts and reliable request handling

**Applies when:** a project consumes HTTP APIs. It does not require a generated client, SDK, particular framework, API versioning strategy or UI state architecture.

## Source contract and client representation

Use the adopted public HTTP contract to determine methods, routes, headers, request/response fields, nullability, errors and security behavior. Do not infer a public wire shape from an internal entity or historical controller sample.

Generated clients, when selected, should be reproducible from the authoritative schema and configuration. Keep transport metadata such as response headers accessible when the contract uses them. Hand-written adapters or presentation models may be appropriate, but a second inconsistent copy of the same wire contract is not.

## Transport, cancellation and ownership

For streamed responses, document header availability, body ownership, disposal and bounded buffering. Make cancellation behavior explicit: stopping the local wait does not necessarily stop a committed remote operation. Do not create a common SDK core, platform adapter package, local cache or UI model layer unless the chosen consumers need them.

Avoid leaking secrets through URLs, diagnostics or error messages. Treat authentication failures, authorization denial, missing resources and concealed resources according to the actual server contract; do not convert security failures to empty successful results.

## Conditional requests, when supported

If the server uses resource validators, preserve its exact `ETag` for the relevant representation. For `If-Match`, strong comparison is defined by HTTP semantics. Never invent a token from a UI field or silently refresh a stale validator and overwrite another change.

Review the contract's distinction between `412 Precondition Failed`, `428 Precondition Required` and `409 Conflict`; these statuses are not interchangeable. On success, use a returned new validator or reread the representation when required.

## Timeouts, retries and uncertain results

An HTTP timeout does not establish that a state change was rolled back. Distinguish requests known not to have been sent, confirmed successes, confirmed rejections and uncertain outcomes.

Safe/idempotent requests may be retried under bounded transport policy. For non-idempotent operations, retry only under an adopted idempotency or outcome-inquiry contract, preserving the agreed operation identity and meaningful request parameters. Equal request bodies do not necessarily represent equal user intent.

Only use `202 Accepted` as a completed result if that is what the contract promises; otherwise follow the real operation-status mechanism. Parse standardized error formats, such as Problem Details, only when the API actually publishes them.

## Verification

Test serialization, omitted versus null fields, authorization boundaries, expected statuses, required headers, cancellation, retries and uncertain outcomes against the actual service or a faithful contract test environment. Generated code compiling is not proof of runtime compatibility.

References: [RFC 9110 HTTP Semantics](https://www.rfc-editor.org/rfc/rfc9110), [Problem Details RFC 9457](https://www.rfc-editor.org/rfc/rfc9457).
