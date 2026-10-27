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

## Response semantics

- Distinguish invalid input, unauthenticated access, forbidden actions, missing or hidden resources, stale state, and transient dependency failures.
- Keep a documented error format. ASP.NET Core supports `ProblemDetails` and `ValidationProblemDetails`; the RFC 9457 format is a useful interoperable option, not proof of any particular product contract.
- Avoid changing a `409` into a `400` or `412` without reviewing the operation and its precondition semantics.
- Do not turn a failed operation into an empty successful response.
- Do not leak internal exceptions or sensitive data; include a safe trace/correlation reference where useful.

## Framework pipeline

Use centralized exception handling when unexpected failures cross the HTTP boundary. For ASP.NET Core's Problem Details service, `AddProblemDetails` and `UseExceptionHandler` are available; check content-negotiation behavior and configure a fallback correctly. `UseStatusCodePages` may populate error responses that have no body, subject to content type support and project requirements.

The authorization policy must be checked on the server. Resource-specific checks often require authorization after loading the resource; declarative route authorization alone may be insufficient.

Never mutate persistent state in an input validator as a side effect. Retries of operations require deliberate design when the outcome is uncertain.

## Verification

HTTP tests should include malformed body and route values, business errors, authentication and ownership boundaries, content negotiation, error schema, concurrency and unknown failures. Verify that the service does not expose stack traces in production-like environments.

References: [ASP.NET Core error handling](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0), [resource-based authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/resource-based?view=aspnetcore-10.0), [Problem Details RFC 9457](https://www.rfc-editor.org/rfc/rfc9457).
