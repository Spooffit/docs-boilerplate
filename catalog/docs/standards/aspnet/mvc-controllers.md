---
title: "ASP.NET Core MVC controller conventions"
description: "Technology-scoped engineering standard"
kind: standard
status: active
---

# ASP.NET Core MVC controller conventions

**Applies to:** projects that explicitly choose MVC controllers for their HTTP APIs, rather than assuming all ASP.NET Core services use MVC.

## Controller responsibility

Prefer `ControllerBase` for API-only controllers and `[ApiController]` where automatic API conventions are desired. Controllers bind request inputs, invoke the chosen application interface and return transport-level responses. Keep persistence details, business rules and transactions outside the controller unless a documented architecture explicitly requires an exception.

- Use appropriate `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpPatch]` or `[HttpDelete]` attributes and stable route templates.
- Bind route, query, body and header parameters explicitly when ambiguity may affect compatibility; MVC `[ApiController]` performs binding-source inference but metadata should remain understandable.
- Use `CancellationToken` for cancellable operations; do not swallow cancellation or blindly retry writes.
- Keep OpenAPI endpoint summaries and response metadata consistent with actual behavior.
- Map application outcomes to HTTP consistently; custom `ApiResponse<T>`, `ToActionResult` or CQRS buses are **project-selected adapters**, not built-in ASP.NET Core components.

## API versioning is optional

ASP.NET API Versioning (`Asp.Versioning.Mvc`) can provide `[ApiVersion]` and route-constraint support, but it is not installed or required by the MVC framework itself. A centralized route prefix and shared endpoint group identifiers may be valuable for large versioned APIs, but the choice belongs to project architecture.

Do not change an existing route shape, identifier representation or version contract as a formatting cleanup.

## Examples and verification

This file deliberately avoids pretend-compilable code that references unknown DI abstractions or custom response types. Verify new controllers with a compiling project example and application-level tests for model binding, response codes, authorization and OpenAPI output.

See [HTTP API design](http-api.md) and [errors and validation](errors-and-validation.md).

References: [Create APIs with ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-10.0), [API versioning](https://github.com/dotnet/aspnet-api-versioning).
