---
title: "ASP.NET Core HTTP API design"
description: "Technology-scoped engineering standard"
kind: standard
status: active
---

# ASP.NET Core HTTP API design

**Applies to:** ASP.NET Core applications exposing HTTP APIs. Covers both MVC controllers and Minimal APIs. A separate MVC profile specifies controller-only conventions.

## Public contract and transport boundaries

- Describe resources and operations from actual consumer use cases, not internal service or database class names.
- Select HTTP methods intentionally: reads should be safe; retries of state-changing operations require documented idempotency guarantees.
- Define request sources, required/optional inputs, validation failures, success and error statuses, authentication, authorization, pagination and concurrency semantics in the authoritative contract.
- Keep route naming and versioning stable for clients. Adopt URL, header or media-type versioning only as a conscious compatibility decision. A path like `api/v{version:apiVersion}` is **not** automatically required.
- Do not publish internal domain entities or persistence entities as API DTOs by default. Changes to a serializer or domain model must not silently alter the public contract.
- When OpenAPI is adopted, check the published specification against the actual application. The deployed HTTP behavior and approved contract, not historical examples, establish consumer expectations.

## Application boundaries and cancellation

Model binding parses transport input; it does not prove ownership, authorization or business validity. Endpoints should delegate use-case behavior to appropriate application services or handlers rather than hide business logic in HTTP adapters. Neither CQRS nor a particular bus abstraction is compulsory.

Accept a request `CancellationToken` where relevant and propagate it to cancellable I/O. A cancelled HTTP request does not automatically roll back a committed operation or prove a message was not delivered.

## Concurrency and conditional HTTP requests

- Support and document entity preconditions such as `ETag` and `If-Match` only when resources have defined version semantics.
- On `If-Match` failure, return `412 Precondition Failed` when the HTTP precondition is false. Use `428 Precondition Required` only if the API intentionally mandates preconditions.
- A conflict without an HTTP precondition may be expressed with `409 Conflict` where appropriate. Distinguish conflict from malformed input.
- Define whether clients can safely retry after timeouts or unknown outcomes; idempotency keys are one available design, not a default implementation.

## HTTP request, response and precondition fidelity

Distinguish **route matching** from input validation. A type constraint such as `{id:guid}` can cause an invalid path segment to miss an endpoint and return `404`, even when a different API contract expects `400` for malformed input. Choose route constraints for routing needs, not as a substitute for request validation; verify invalid inputs through the actual HTTP pipeline.

If clients use conditional mutations, document the source and meaning of the selected representation's `ETag`. For `If-Match`, the server uses **strong comparison** and checks the current representation before applying a change. A failed precondition normally returns `412`; `428` is for an API that **deliberately requires** conditional requests, not a default result for all updates. Keep transport representation validators separate from internal concurrency tokens, business versions and unrelated integration sequence numbers.

An ETag need not encode an integer counter. Mapping an internal `int Version` to `ETag: "7"` is one possible implementation, not an HTTP or .NET requirement. The wire format, accepted syntax, support for wildcard/list conditions and client recovery from a mismatch belong in the adopted contract. The server must enforce the precondition at a boundary that actually prevents stale writes; parsing a header alone is not concurrency protection.

A request can fail before any application handler runs, for instance during routing, authentication, authorization or binding. Document those response paths and verify that metadata describes what the deployed pipeline actually emits. `200` with a response envelope is legitimate for a selected contract, but it is **not a universal default for all writes**. `201`, `202` and `204` convey different meanings; `204` does not carry a JSON body. If a request queues asynchronous work and responds `202`, specify how the caller learns its eventual result when needed. Binary responses require their own media type and header contract, not an arbitrary JSON wrapper.

Official references: [ASP.NET Core routing](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing), [RFC 9110 conditional requests](https://www.rfc-editor.org/rfc/rfc9110#section-13), [HTTP status codes](https://www.rfc-editor.org/rfc/rfc9110#section-15).


## Status codes and errors

Use statuses matching behavior: `201` for created resources when applicable, `202` for accepted asynchronous work, `204` for successful responses with no body, `400` for invalid requests, `401` for missing/invalid authentication, `403` for denied authorization, `404` when a resource is absent or intentionally not disclosed, and `5xx` for server failures. These are context-sensitive examples, not an exhaustive mapping.

Use consistent structured error responses; see [validation and errors](errors-and-validation.md). Never expose stack traces, passwords or raw infrastructure exceptions to clients.

## OpenAPI and compatibility

ASP.NET Core has built-in OpenAPI generation via `Microsoft.AspNetCore.OpenApi`; other supported tools are alternatives. Only declare `ProducesResponseType` or equivalent metadata that matches real behavior. Review changes to route constraints, requiredness, enums, field types and error schemas as compatibility changes.

If publishing an OpenAPI endpoint, control access to it according to environment and policy. Generating the document does not guarantee every runtime scenario is correct.

## Verification

Use application-level HTTP tests for routing, binding, authorization, error contracts, cancellation and API compatibility. Test infrastructure/provider behavior separately and include reverse-proxy, CORS or authentication integration tests when the deployment demands them.

References: [ASP.NET Core OpenAPI](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0), [HTTP error handling](https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors?view=aspnetcore-10.0), [HTTP conditional requests (RFC 9110)](https://www.rfc-editor.org/rfc/rfc9110).
