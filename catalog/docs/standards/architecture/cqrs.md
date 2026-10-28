---
title: "CQRS application pattern"
description: "Applicable engineering standard"
kind: standard
status: active
---

# CQRS application pattern

**Applies to:** systems that deliberately separate write commands from read queries. This pattern is optional and does not imply microservices, event sourcing or separate databases.

## Responsibility boundaries

A **command** represents an intention to change state; a **query** requests information without causing intentional business side effects. Do not hide mutations inside a query operation.

Separate command handling from read projection/query handling when the use case benefits from independent models, policy enforcement or performance strategies. A single application/database may implement CQRS without physically splitting read and write storage.

Handlers should honor application authorization and domain invariants. Query handlers should return purpose-built read models and minimize unexpected effects.

## Placement is architecture-dependent

Command and query handlers may be organized in the application layer, dedicated read-model components or chosen infrastructure adapters. Document actual dependency boundaries rather than copying a historical assembly layout.

Use a mediator or command/query bus only if the project selects that abstraction. Neither `ICommandBus` nor `IQueryBus` is implied by CQRS alone.

## Handler responsibilities and dependency direction

When a project uses an Application/Domain/Infrastructure layering model, command handlers commonly orchestrate the use case in Application, ask domain objects to enforce business invariants, and persist through infrastructure abstractions. Do not relocate business rules into handlers solely because a command changes state.

Query handlers can reside in Application, a read-model assembly, or an infrastructure adapter according to actual chosen dependencies and access strategy. Keep the *request/contract* independent of provider-specific implementation if the architecture requires it; avoid making Application depend directly on an EF Core-specific assembly to resolve a query. Moving a handler between assemblies does not change the CQRS semantics by itself.

Choose handler placement according to the project's real dependency graph. No project must use an ORM, mediator or separate database to implement CQRS.

Reference: [Microsoft .NET CQRS application-layer example](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/microservice-application-layer-implementation-web-api).

## Consistency and messaging

If the read side is updated asynchronously, document freshness guarantees, retries, idempotency and what clients observe before projections catch up. Use outbox or inbox patterns only where the failure model justifies them.

## Verification

Test commands for unauthorized transitions, conflicts and invariant preservation. Test read models for filtering, projection correctness and consistency expectations. Architecture tests enforce only rules chosen for the actual solution.

Reference: [Microsoft CQRS pattern guidance](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs).
