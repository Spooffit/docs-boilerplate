---
title: "Messaging, query specifications and Blazor: migration decisions"
description: "Historic and ongoing source analysis"
kind: audit
status: active
---

# Messaging, query specifications and Blazor: migration decisions

- Date: 2026-10-27
- Scope: targeted review of the source standards `S19-specifications.md`, `S22-eventbus-topology.md`, `technical/frontend/standards/S06-api-client.md`, `S08-testing.md`; additionally checked the existing Blazor/MudBlazor profile content against current official documentation.
- Status: first generalized extracts; legacy documents remain for further analysis and traceability.

## Source review and outcomes

| Source | Kept | Changed |
| --- | --- | --- |
| `S19-specifications.md` | Predicate/query intent reuse, EF integration considerations | Ardalis.Specification is opt-in; it is not mandatory for all query handlers, and no unverified library API snippets are treated as executable |
| `S22-eventbus-topology.md` | Contract/transport separation, stable broker identities, endpoint ownership, duplicates and delivery semantics | Three-project `Contracts/RabbitMq/Default` physical layout is optional; `fanout`, naming prefix and use of a specific outbox are not global requirements |
| Frontend `S06-api-client.md` | Transport DTOs as authoritative public contracts, handling uncertain write outcomes | API client code-generation package or ETag behavior is conditional on actual client/server contracts |
| Frontend `S08-testing.md` | Distinction among component tests, browser tests and real client-server behavior | bUnit becomes optional, and bUnit should not be presumed compatible with TUnit until specifically checked |

## Findings to preserve

- **Stable messaging identity:** a broker exchange name and serialized contract should not be accidentally changed by a CLR rename. Application receive endpoint queues and bindings belong to the consumers.
- **Reliability:** retry, redelivery, idempotency, in-memory outbox, transactional bus outbox and consumer outbox must not be collapsed into one vague "exactly once" promise.
- **Testing:** a bUnit component test is not browser automation, and a mocked HTTP client does not establish API compatibility.
- **Query abstraction:** specifications are useful when they make query intent reusable; they do not need to replace every SQL query or EF LINQ expression.
- **Blazor lifecycle:** parameter changes, initialization, cancellation, disposal and prerendering have different semantics across hosting modes.

## Sources cross-checked

- [MassTransit topology](https://masstransit.io/documentation/configuration/topology)
- [MassTransit RabbitMQ transport](https://masstransit.io/documentation/configuration/transports/rabbitmq)
- [MassTransit transactional outbox](https://masstransit.io/documentation/patterns/transactional-outbox)
- [Ardalis.Specification](https://github.com/ardalis/Specification)
- [bUnit tests](https://bunit.dev/docs/getting-started/writing-tests.html)
- [Blazor component lifecycle](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/lifecycle?view=aspnetcore-10.0)

## Not yet done

Review the remaining frontend standard files comprehensively, assess optional API client tool profiles, and validate all examples against a real project. Do not delete any of the legacy source material until the complete audit is recorded.

## S21/S22 extraction review (2026-10-28)

The complete legacy S21 and S22 texts were compared with current canonical DDD, EF Core, FluentValidation and MassTransit/RabbitMQ documentation. S21's shared immutable domain limits, valid optional-field semantics, Unicode/string-length distinction, regex safety, owned-object requiredness, migration snapshot fidelity and public const inlining are now described in relevant selected profiles. The mandatory static class tree and long uncompiled example are **not** made global defaults. S22's independent contracts, deliberate dependency direction, optional three-project composition, stable exchange identity, correlation, consumer queue ownership, actual routing semantics, outbox tradeoffs and compatibility reviews were moved into the existing MassTransit profile. The universal requirement to create exactly three projects, use one exchange naming rule and enforce a particular outbox remains unapproved.

Retirement of either historical source still requires a measured incoming-link graph and a recorded removal decision; the old files also contain educational examples or opinionated architecture choices that may warrant explicit retention until the owner makes the related deferred decisions. This is not evidence that predecessor solution code builds or its RabbitMQ topology runs.
