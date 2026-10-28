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

## Portable SDK core and platform adapters (when applicable)

When multiple applications or runtimes consume the same service, document **which layer owns the public wire contract**, shared protocol behavior and platform integration. A portable SDK can share request coordination, typed interpretation, validation, idempotency/retry decisions and state-transition rules while exposing narrow interfaces for HTTP transport, persistence, time and diagnostics. Concrete adapters implement those capabilities for their actual environment. Do not create a multi-package SDK or force a particular layering pattern for a simple single-platform client.

Keep one owner for an operation's identity, original request intent and recovery logic. An HTTP adapter should not invent a new operation ID, silently change a resource version, or implement its own competing business retry algorithm after a timeout. When a client persists an operation or transfer, distinguish the durable store's logical identity, file or database locking, and the application's UI/session lifetime; an in-memory lock is not automatically cross-process coordination.

A shared C# library can be reused in C# hosts that support its real target framework and dependencies. It is **not** automatically a JavaScript, native or other-language SDK. The authoritative OpenAPI, JSON Schema or comparable public contract may be shared with separate language implementations; sharing a contract does not imply byte-for-byte runtime API compatibility or a working cross-language wrapper.

## Streaming, cancellation and resource ownership

For transports that stream response bodies, specify who owns the response and its disposal, when headers become available, how backpressure bounds buffered data and which component consumes the stream. A callback receiving a reusable buffer must not retain its contents unless they are copied into owned storage. A platform API's asynchronous callback or main-thread dispatch requirement is an adapter responsibility, not evidence that the shared SDK may block or buffer an entire response.

Define cancellation at the boundaries it can actually stop: request issuance, network read, parsing, durable staging, and an already committed remote operation are different. A timeout or cancellation must not silently manufacture a failed server commit, a fresh operation identity or a successful cleanup result. Client SDKs should expose meaningful pending/failed/unknown-outcome states and bounded recovery paths.

For native, UI-engine or platform-managed resources, declare the transfer of ownership explicitly: which component constructs, caches, releases or destroys each resource, on what thread and after which operation. A resource created before cancellation may still require disposal when its callback arrives late. Engine-specific asset systems, address resolution and rendering objects are optional integrations, not part of the HTTP wire contract.

## Authentication, authorization and secrets

Clients may supply credentials and identifiers, but server-side ownership and authorization cannot be proven by client validation or a cached hash. Avoid putting secrets in URLs, diagnostics, logs and exception messages. Token refresh, cookie policy, CORS and credentials depend on the actual deployment and threat model.

Distinguish user-visible authentication failure, insufficient authorization, nonexistence and an intentionally concealed resource. A client must not turn security failures into empty successful results.

## Optional generated-client and UI-model policy

For projects that choose NSwag or another OpenAPI generator, keep generated DTOs and operations reproducible from the pinned authoritative contract and generator settings. Do not hand-edit generated code, or maintain a second hand-written set of *identical wire fields* merely to bypass a generator limitation.

Some teams deliberately reuse generated DTOs directly in form read/edit state, with a separate small container for loading, dirty flags, selection and HTTP metadata. Other teams need explicit form/view models for isolation, validation, complex edits or contract evolution. **Neither choice is universal.** Review actual duplication and mapping value rather than banning adapters solely because fields currently match.

If the client generator does not expose headers needed by the selected API contract (such as `ETag`), update a supported generation setting or owned HTTP adapter and test the resulting code. Do not solve missing header access by inventing server-side version fields in DTOs.

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
