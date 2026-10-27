---
title: "HTTP client contracts and reliable request handling"
description: "Language-neutral guidance for client models, preconditions, authorization and uncertain outcomes"
kind: standard
status: active
---

# HTTP client contracts and reliable request handling

**Applies when:** a project consumes HTTP APIs. This profile is language-neutral. It does not require ASP.NET Core, NSwag, a specific envelope/error format, an HTTP versioning scheme or a state-management library.

## Contract authority and representation

Use the **actual public API contract**, not an internal domain model, ORM entity or sample controller, to establish routes, methods, request/response fields, null/missing/empty semantics, HTTP headers and status codes.

Where clients are generated from an adopted OpenAPI description, treat generated code as an artifact of that contract. Do not edit it manually. Keep transport models and independent presentation state separate when that reflects the selected client's architecture. Avoid duplicating the *same wire-contract fields* in hand-written models without a concrete need, but adapters are legitimate for view state, domain isolation and version compatibility.

Do not infer that a generated client exposes every required response header: configure and verify access to `ETag`, correlation headers or pagination links when the protocol actually uses them.

## Authentication, authorization and secrets

Clients may supply credentials and identifiers, but server-side ownership and authorization cannot be proven by client validation or a cached hash. Avoid putting secrets in URLs, diagnostics, logs and exception messages. Token refresh, cookie policy, CORS and credentials depend on the actual deployment and threat model.

Distinguish user-visible authentication failure, insufficient authorization, nonexistence and an intentionally concealed resource. A client must not turn security failures into empty successful results.

## Conditional writes and version tokens

Only when a server exposes `ETag` and requires conditional mutations:

1. Retrieve the **exact resource representation** and its validator. A list entry does not automatically carry a validator for a separate detail resource.
2. Preserve the server-supplied token as transport metadata; do not fabricate it from a business version or infer it from modified UI fields.
3. Submit the appropriate precondition, e.g. `If-Match`, unchanged with the intended mutation. RFC 9110 requires strong entity-tag comparison for `If-Match`.
4. A failed precondition commonly results in `412 Precondition Failed`; `428 Precondition Required` may indicate that the API explicitly requires a missing precondition. `409 Conflict` can mean an independent domain conflict. These are distinct protocols, not universal mappings for every API.
5. After success, use the returned validator for the new representation when provided or reread that representation before the next conditional edit.

Do not silently retry a stale mutation by replacing the original validator with a fresh one: that may overwrite another user's change. The server must recheck concurrency at its authoritative state-changing boundary.

## Timeouts, retries and idempotency

A timeout or cancelled HTTP request does **not** prove that a server-side mutation was rolled back. Classify **known rejection**, **confirmed success**, **request never sent** and **unknown commit outcome** separately.

- Retry safe reads according to available deadlines and transport policy; avoid indefinite retry storms.
- A state-changing request may be retried only within a confirmed idempotency/replay contract. If an operation identity is adopted, preserve the **same identifier and same meaningful request parameters** when recovering an uncertain outcome.
- Never assume that two requests with equal payloads have the same business intent, nor that an idempotency key suppresses unrelated outgoing effects without verified coordination.
- Define how the client can inquire about an operation's result or reconcile its local state. Do not treat "not visible yet" as proof that an in-flight original request cannot still commit.

## Error behavior and presentation

Handle invalid input, authentication, authorization, missing resources, concurrent edits, rate limits, dependency failure and unknown outcomes separately as demanded by the actual API. Parse `Problem Details` only where the server publishes that shape and verify response content types rather than trusting that every error is JSON.

Keep recoverable user edits when appropriate; do not display success before the service reaches the agreed completion level. Long-running `202 Accepted` work needs an explicit completion/status contract if the consumer expects final outcome confirmation.

## Verification

Use consumer/HTTP-level tests against the authoritative schema and an actual or suitably faithful service. Verify binding of `ETag` headers, stale `If-Match`, status/error bodies, null versus absent fields, redirects, auth boundaries, duplicate requests and timeout-after-commit recovery. A generator compiling is not proof that deployed API behavior matches it.

References: [RFC 9110 HTTP Semantics](https://www.rfc-editor.org/rfc/rfc9110), [Problem Details RFC 9457](https://www.rfc-editor.org/rfc/rfc9457).
