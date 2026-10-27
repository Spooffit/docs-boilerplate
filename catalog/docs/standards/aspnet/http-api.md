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
- Use OpenAPI as a machine-readable description and check the emitted specification against the actual application. Generated clients are consumers of that contract, not a second source of truth.

## Application boundaries and cancellation

Model binding parses transport input; it does not prove ownership, authorization or business validity. Endpoints should delegate use-case behavior to appropriate application services or handlers rather than hide business logic in HTTP adapters. Neither CQRS nor a particular bus abstraction is compulsory.

Accept a request `CancellationToken` where relevant and propagate it to cancellable I/O. A cancelled HTTP request does not automatically roll back a committed operation or prove a message was not delivered.

## Concurrency and conditional HTTP requests

- Support and document entity preconditions such as `ETag` and `If-Match` only when resources have defined version semantics.
- On `If-Match` failure, return `412 Precondition Failed` when the HTTP precondition is false. Use `428 Precondition Required` only if the API intentionally mandates preconditions.
- A conflict without an HTTP precondition may be expressed with `409 Conflict` where appropriate. Distinguish conflict from malformed input.
- Define whether clients can safely retry after timeouts or unknown outcomes; idempotency keys are one available design, not a default implementation.

## Status codes and errors

Use statuses matching behavior: `201` for created resources when applicable, `202` for accepted asynchronous work, `204` for successful responses with no body, `400` for invalid requests, `401` for missing/invalid authentication, `403` for denied authorization, `404` when a resource is absent or intentionally not disclosed, and `5xx` for server failures. These are context-sensitive examples, not an exhaustive mapping.

Use consistent structured error responses; see [validation and errors](errors-and-validation.md). Never expose stack traces, passwords or raw infrastructure exceptions to clients.

## OpenAPI and compatibility

ASP.NET Core has built-in OpenAPI generation via `Microsoft.AspNetCore.OpenApi`; other supported tools are alternatives. Only declare `ProducesResponseType` or equivalent metadata that matches real behavior. Review changes to route constraints, requiredness, enums, field types and error schemas as compatibility changes.

If publishing an OpenAPI endpoint, control access to it according to environment and policy. Generating the document does not guarantee every runtime scenario is correct.

## Verification

Use application-level HTTP tests for routing, binding, authorization, error contracts, cancellation and API compatibility. Test infrastructure/provider behavior separately and include reverse-proxy, CORS or authentication integration tests when the deployment demands them.

References: [ASP.NET Core OpenAPI](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0), [HTTP error handling](https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors?view=aspnetcore-10.0), [HTTP conditional requests (RFC 9110)](https://www.rfc-editor.org/rfc/rfc9110).
