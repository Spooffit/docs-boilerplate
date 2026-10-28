---
title: "HTTP validation, authorization and error handling"
description: "Technology-scoped engineering standard"
kind: standard
status: active
---

# HTTP validation, authorization and error handling

**Applies to:** ASP.NET Core HTTP applications.

## Three distinct checks

1. **Transport checks:** parsing, binding, required shape, length and format of an incoming DTO. For MVC controllers, `[ApiController]` can automatically produce validation responses when model state is invalid.
2. **Application checks:** existence, ownership, allowed state transitions, available capacity and permissions for an operation. Input syntax alone proves none of these.
3. **Domain and persistence invariants:** constraints that must hold despite concurrent requests or direct writes. Client-side validation and preflight existence checks cannot substitute for database constraints.

Choose one source for each rule; use adapters for representation without duplicating decision logic. A validation library is optional. FluentValidation conventions are selected through their own profile.

## Value normalization and partial operations

For each request value, define requiredness, length, permitted character set, encoding and any agreed case/Unicode/whitespace normalization. Do not rely on a maximum string length to reject forbidden characters, and do not invent normalization that changes consumer-visible identifiers. Validate nested objects against their real contract as well as their syntax.

A bulk endpoint must document whether it is atomic or supports per-item outcomes; when partial results are allowed, expose successes and failures explicitly rather than reporting an unconditional overall success. If a state-changing request times out after reaching the server, clients need a defined retry, status-query or idempotency strategy where applicable. Transport failure alone does not establish that the write was rolled back.

## Validation phases for structured or editable content (when applicable)

When a project supports incomplete authoring followed by an explicit activation, submission or publication step, document which invariants apply at **each** operation. A draft Save might allow a missing required business value while still rejecting malformed types, invalid definition structure or unauthorized edits; a later Publish might require complete nested values, valid references and stronger cross-record invariants. This is a **project-selected workflow**, not permission for every endpoint to accept invalid data and not a requirement to introduce drafts.

Separate an invalid **definition** (for example contradictory minimum/maximum bounds or an unsupported comparer) from an incomplete **instance** of an otherwise valid definition. Never silently repair submitted values by trimming, filling defaults, sorting, deduplicating, coercing numeric types or changing a time zone unless the public operation explicitly promises that transformation. Return addressable error locations for nested fields and list indexes where the adopted error format supports them.

A repeat of validation at the final state-changing boundary is still necessary after a previous preview or draft check: another writer may have changed referenced resources or permissions. Verify that the actual endpoint and application pipeline execute the correct validation level.

## Response semantics

- Distinguish invalid input, unauthenticated access, forbidden actions, missing or hidden resources, stale state, and transient dependency failures.
- Keep a documented error format. ASP.NET Core supports `ProblemDetails` and `ValidationProblemDetails`; the RFC 9457 format is a useful interoperable option, not proof of any particular product contract.
- Avoid changing a `409` into a `400` or `412` without reviewing the operation and its precondition semantics.
- Do not turn a failed operation into an empty successful response.
- Do not leak internal exceptions or sensitive data; include a safe trace/correlation reference where useful.

## Error status versus actual middleware behavior

Plan error outcomes for route misses, malformed bodies, failed authentication, permission denial, missing or hidden resources, application conflicts and dependency faults **at the layer where they arise**. A handler-specific result mapper cannot by itself transform every route, authentication or binding failure. Keep safe diagnostics and correlation where appropriate; don't publish internal exception details.

Distinguish a failed HTTP `If-Match` precondition from a business conflict. A `412` is normally appropriate for a false precondition; a `409` can remain a separate state conflict even when preconditions pass. A missing header warrants `428` only when the documented endpoint requires it. Invalid route constraints can yield `404` before model validation, so test actual requests instead of inferring behavior solely from controller attributes.


## Framework pipeline

Use centralized exception handling when unexpected failures cross the HTTP boundary. For ASP.NET Core's Problem Details service, `AddProblemDetails` and `UseExceptionHandler` are available; check content-negotiation behavior and configure a fallback correctly. `UseStatusCodePages` may populate error responses that have no body, subject to content type support and project requirements.

The authorization policy must be checked on the server. Resource-specific checks often require authorization after loading the resource; declarative route authorization alone may be insufficient.

Never mutate persistent state in an input validator as a side effect. Retries of operations require deliberate design when the outcome is uncertain.

## Verification

HTTP tests should include malformed body and route values, business errors, authentication and ownership boundaries, content negotiation, error schema, concurrency and unknown failures. Verify that the service does not expose stack traces in production-like environments.

References: [ASP.NET Core error handling](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0), [resource-based authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/resource-based?view=aspnetcore-10.0), [Problem Details RFC 9457](https://www.rfc-editor.org/rfc/rfc9457).
