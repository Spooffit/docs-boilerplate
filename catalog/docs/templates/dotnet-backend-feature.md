---
title: "Backend feature: [Capability]"
description: "Reusable writing template"
kind: template
status: template
---

# Backend feature: [Capability]

> **Use:** Optional .NET-specific alternative to the general technical-feature template when its server-side detail is useful. Start with scope, observable behavior, required interfaces and verification. Include only layers and libraries already selected; omit tables and headings without evidence. Do not create a parallel technical-feature document for the same scope.

## Scope and verified inputs

[Link the product requirement. State system boundary, deployable unit, confirmed dependencies, and known non-goals.]

## Domain behavior and invariants (when applicable)

[Describe entities, value objects, invariants, legal transitions and concurrency semantics only when these are real parts of the solution.]

### Data model (when needed)

Document a field table only when the selected authoritative model or contract does not already provide the necessary information. Include only verified fields:

| Field | Type / nullability | Requiredness | Semantics | Constraints |
| --- | --- | --- | --- | --- |
| [Property] | [Exact type or explicitly unknown] | [Required / optional / conditional] | [Ownership and source] | [Known format, domain constraints and invariants] |

Keep external wire format separate from domain models where appropriate. Treat missing, null, zero/false and empty collection differently when the adopted contract does. Apply specialized field-identity, ordering or numerical/time semantics only when the real domain calls for them. Do not invent a draft/publish workflow.

## Application use case

[What is the triggering command or query, or directly invoked application interface if no CQRS framework was chosen? Explain authorization, persistence behavior, sequencing, errors and observable result.]

## External interface

[Reference authoritative OpenAPI/message schema. Record only the transport decisions absent from the canonical machine-readable contract. Do not invent routes, DTO fields or status codes.]

| Interaction | Method / message | Contract reference | Access rule | Observable response |
| --- | --- | --- | --- | --- |
| [Operation] | [Verified] | [Real reference] | [Verified] | [Verified] |

## Behavior and data flow

[Describe the actual execution path at the granularity needed to understand the feature. For simple reads or synchronous behavior, do not manufacture transactions, messaging stages, or a fixed sequence of five steps. Record error, retry and concurrency boundaries only when applicable.]

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
