---
title: "ADR 0006: Contextual technology-profile selection"
description: "Decisions on dependent technology choices in the documentation generator"
kind: adr
status: active
---

# ADR 0006: Contextual technology-profile selection

- Status: Accepted
- Updated: 2026-10-30
- Authority: Repository-owner decisions on documentation generator options

## Decision

Keep the repository Markdown-first, with a deterministic multi-stage technology decision flow and a pure validation layer. The CLI installs documentation standards only, not application code or third-party packages.

- The project name and destination directory are mandatory inputs; no project-description question or field is generated.
- DDD, accessible forms and direct HTTP request DTO form guidance are always present. Existing project data and actual requirements remain unverified until independently documented.
- .NET test frameworks are mutually exclusive (TUnit or xUnit). Blazor can additionally select bUnit.
- Data access chooses one implementation (EF Core or Dapper). Specification pattern choice is offered only after EF Core, choosing Ardalis.Specification or a manual implementation. Dapper and specifications cannot coexist in a selection.
- PostgreSQL optionally prompts for exactly one connection pooler (PgBouncer or Odyssey).
- Architecture chooses a modular monolith, microservices or a combined monolith/microservices deployment model.
- Messaging chooses enablement, RabbitMQ or Kafka, client (MassTransit, Wolverine or custom) and an optional organization strategy, in that order. A transport without a client and a client without a transport are invalid; the transport-specific behavior is documented separately.
- CQRS is optional; its handler technology may be manual, Wolverine (only when Wolverine is selected earlier), MediatR, martinothamar/Mediator or Requestum.
- Mapping selects exactly one of AutoMapper, Mapster, Mapperly and manual code.
- API documentation UI selects Swagger UI or Scalar after ASP.NET Core. A Blazor frontend may use handwritten REST clients or NSwag-generated clients.
- Infrastructure offers Grafana Alloy and Mimir independently of logs, traces, deployment and object-storage options.

## Implementation policy

Use a small explicit wizard flow and a deterministic `ResolveProfiles` validator instead of adding an external state-machine dependency. Dependency resolution and exclusivity apply to both the interactive UI and non-interactive `--profiles`.

Every optional profile must own a distinct Markdown source and an equivalent Russian translation. No feature assumes a business entity, product-specific lifecycle, API route, release process or historical project folder structure.

## Verification

CI checks all supported profile combinations in isolation, relevant incompatible pairs, source parity, English/Russian generation, file ownership and navigation. Failure to compile or exercise a selected profile must be reported rather than treated as verified.
