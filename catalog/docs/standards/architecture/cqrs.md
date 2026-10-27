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

The original project placed command handlers in an Application assembly and query handlers in an EntityFramework.Persistence assembly. This is one **project convention**, not a universal CQRS requirement. Document actual dependency boundaries in the project architecture; avoid introducing Application-to-infrastructure references solely to satisfy a folder naming rule.

Use a mediator or command/query bus only if the project selects that abstraction. Neither `ICommandBus` nor `IQueryBus` is implied by CQRS alone.

## Consistency and messaging

If the read side is updated asynchronously, document freshness guarantees, retries, idempotency and what clients observe before projections catch up. Use outbox or inbox patterns only where the failure model justifies them.

## Verification

Test commands for unauthorized transitions, conflicts and invariant preservation. Test read models for filtering, projection correctness and consistency expectations. Architecture tests enforce only rules chosen for the actual solution.

Reference: [Microsoft CQRS pattern guidance](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs).
