---
title: "Backend feature: [Capability]"
description: "Reusable writing template"
kind: template
status: template
---

# Backend feature: [Capability]

> **Use:** Template for one feature's server responsibilities in a .NET project. Only include layers and libraries actually selected. Do not introduce DDD, CQRS, repositories, messaging, EF Core, OpenAPI or extra abstractions merely to complete a section.

## Scope and verified inputs

[Link the product requirement. State system boundary, deployable unit, confirmed dependencies, and known non-goals.]

## Domain behavior and invariants (when applicable)

[Describe entities, value objects, invariants, legal transitions and concurrency semantics only when these are real parts of the solution.]

### Data model

Use a separate row for each *confirmed* property, not a single free-text enumeration:

| Field | Type / nullability | Requiredness | Semantics | Constraints |
| --- | --- | --- | --- | --- |
| [Property] | [Exact type or explicitly unknown] | [Required / optional / conditional] | [Ownership and source] | [Known format, domain constraints and invariants] |

Keep external wire format separate from domain models where appropriate. Treat missing, null and empty collection differently when the contract does.

## Application use case

[What is the triggering command or query, or directly invoked application interface if no CQRS framework was chosen? Explain authorization, persistence behavior, sequencing, errors and observable result.]

## External interface

[Reference authoritative OpenAPI/message schema. Record only the transport decisions absent from the canonical machine-readable contract. Do not invent routes, DTO fields or status codes.]

| Interaction | Method / message | Contract reference | Access rule | Observable response |
| --- | --- | --- | --- | --- |
| [Operation] | [Verified] | [Real reference] | [Verified] | [Verified] |

## Internal data flow

1. [Input validation and authorization boundary, with source of truth.]
2. [Required reads and checks; note concurrency races.]
3. [State changes and actual transactional guarantees.]
4. [External effects and consistency/retry mechanism, if present.]
5. [Return and observability.]

## Persistence and integrations (when applicable)

[Which repository, DbContext or SQL adapter actually owns data? Define migrations, failure/retry policy, message contracts and outbox only when relevant.]

## Error and diagnostic contract

[Distinguish input, access, missing resource, conflict and infrastructure failure. Record safe logs/traces and request correlation.]

## Testing and acceptance

| Risk / behavior | SUT and test scope | Real or substituted dependency | Expected evidence |
| --- | --- | --- | --- |
| [Scenario] | [Unit / integration / functional / etc.] | [Specify] | [Externally observable assertion and CI location] |

## Open decisions

[Link open questions and ADRs for unresolved technical choices. Remove optional headings instead of filling them with fiction.]
