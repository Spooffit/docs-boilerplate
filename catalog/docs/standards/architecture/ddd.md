---
title: "Domain-Driven Design"
description: "Optional domain modeling and bounded-context guidance"
kind: standard
status: active
---

# Domain-Driven Design

**Applies when:** a project deliberately uses Domain-Driven Design practices. DDD is not a requirement for .NET, HTTP APIs or the documentation repository.

## Domain language and boundaries

Work with domain experts to discover terms, behaviors, rules and conflicts in the language used by the business. Maintain context-specific terminology; the same word may have different meanings in different bounded contexts.

Define bounded contexts around coherent models and language boundaries, not solution folders or deployment units by default. Document relationships and integration contracts when contexts communicate. DDD does not require microservices, CQRS, event sourcing, repositories for every type or a fixed layer count.

## Entities, value objects and aggregates

Use entities when continuity of identity matters and value objects when equality by meaningful value is appropriate. Introduce aggregates only when they provide a justified consistency boundary. The aggregate's invariants must be enforced by the correct trusted boundary, including concurrent changes where relevant.

Keep domain behavior with its owner rather than scattering the same rule across unrelated validators or UI components. Application services may coordinate work; infrastructure adapters handle chosen external systems. The exact assemblies, persistence patterns and naming conventions remain architecture decisions.

## Rules and validation

Distinguish stable domain constraints from transport input checks, authorization and persistence integrity. Share identical domain rule definitions where dependency boundaries permit, but do not equate coincidentally equal numeric limits. The project may choose domain constraint classes, methods or value objects; none is a required file pattern.

For adopted domain values, define meaningful equality, normalization, missing/null behavior, precision and comparison only where those choices affect the actual model. Do not import generic field schemas, versioned entity editing, publishing, approval or historical snapshot lifecycles into a DDD standard.

## Events and integration, when applicable

Domain events describe facts within a model; integration events are externally communicated contracts. Neither is required for every aggregate. If events cross system boundaries, document delivery guarantees and how implementation handles retries and side effects rather than assuming one atomic transaction covers both persistence and external delivery.

## Verification

Use examples from the **current project's approved domain** to validate aggregate boundaries, invariant protection and language consistency. Use domain tests for observable rules and integration tests for real persistence or messaging guarantees. Do not introduce sample domain entities as reusable template requirements.
