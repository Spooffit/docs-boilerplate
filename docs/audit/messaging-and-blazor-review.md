---
title: "Messaging, query specifications and Blazor: migration decisions"
description: "Historic and ongoing source analysis"
kind: audit
status: active
---

# Messaging, query specifications and Blazor: migration decisions

- Date: 2026-10-27
- Scope: targeted review of the source standards `S19-specifications.md`, `S22-eventbus-topology.md`, `technical/frontend/standards/S06-api-client.md`, `S08-testing.md`; additionally checked the existing Blazor/MudBlazor profile content against current official documentation.
- Status: initial extraction snapshot. S19 and S22 were later individually retired after full-source and incoming-link review; S06 and S08 remain pending owner choices. See [removal ledger](legacy-removals.md) and [retained-standard closeout](retained-standards-closeout.md).

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

Both incoming-link graphs now report zero. S22 is retired under an individual removal record after completing the reusable MassTransit profile migration; S21 is deliberately retained because its extensive uncompiled educational example requires an owner retention decision. No predecessor .NET application or RabbitMQ integration test has been executed by this documentation review.

## S05–S08 full-source screening (2026-10-28)

S05 (forms), S06 (API clients), S07 (accessibility) and S08 (frontend testing) have been read in full. Reusable rules for accurate save/publish feedback, local row deletion versus durable Delete, server-owned IDs, lifecycle of unsaved input, hierarchy/selection semantics and accessible status messaging are consolidated in `catalog/docs/standards/ui/forms-and-accessibility.md`. Language-neutral generated-client practices and header transport metadata are in `http/client-contracts.md`; Blazor async UI interactions are in `blazor/components.md`. bUnit interaction tests, strict JS interop mocks, semantic assertions, bounded async waits, optional FSD ownership and browser-only coverage are in the separately selected `blazor/bunit.md` profile.

S05 and S07 have been retired together after the GitHub Actions graph showed zero S05 incoming links and one S07 incoming link **only from S05**. The removal evidence is recorded in the removal ledger. S06 and S08 contain substantial opinionated NSwag form-state, bUnit/FSD/Playwright guidance and an across-suite naming mandate: they remain in the predecessor tree until the owner resolves their optional-profile and example-retention options. No UI runtime, browser or predecessor app test was run by this documentation audit.
