---
title: "Ardalis.Specification conventions"
description: "Technology-scoped engineering standard"
kind: standard
status: active
---

# Ardalis.Specification conventions

**Applies to:** .NET applications that explicitly select the Ardalis.Specification library for composable query intent.

## Purpose and applicability

A specification describes selection, filtering, sorting, projection or paging as a reusable, testable query object. Use the pattern when related queries benefit from discoverability and composable semantics. Do not require specifications around every direct EF Core or Dapper query.

## Semantics and composition

- Give each specification a clear purpose. Avoid "universal query" objects exposing arbitrary transport inputs without business boundaries.
- Reuse stable, genuinely shared predicates, but do not combine distinct business meanings merely because CLR property types match.
- Keep authorization enforcement in trusted application/data-access logic. A caller-selected specification does not prove access.
- Be explicit about whether a specification selects entities or projected DTOs; verify provider translation, ordering and pagination.
- For database queries, confirm that composition remains server-translatable and does not fetch unbounded rows for client-side filtering.
- Check version-specific API behavior when composing specifications. Do not write code examples against assumed overloads or repository methods.

## Reusable predicates versus complete specifications

The library does **not** provide generic `specA.And(specB)`, `specA.Or(specB)` or `specA.Not()` composition of complete specifications. A specification may also own includes, ordering, projections, tracking behavior and pagination, so treating entire specifications as simple Boolean predicates loses their meaning.

- Prefer focused `ISpecificationBuilder<T>` extension methods for reusable atomic predicates. Have each operation-specific `Specification<T>` invoke the appropriate extensions and define its own result shape.
- A reusable predicate must not silently set tracking, sort, `Include`, `Skip`, `Take`, projection or permission scope. Keep those choices explicit in the final specification; do not create a giant specification with unrelated optional switches.
- Multiple `Where` predicates narrow the result together (AND). Express OR with an intentionally grouped predicate; adding two separate `Where` clauses does not mean OR.
- Pass filter inputs explicitly rather than reading ambient HTTP context or mutable global state. Specification construction describes a query, and should not open connections or perform writes.
- If using the EF Core integration, use the installed version's supported evaluator/adapter (for example `WithSpecification` where available). Do not materialize before filtering or `Compile()` a database predicate merely to make unsupported translation run in memory.
- Keep paging separate from total-match counts. Select `Specification<T, TResult>` for shaped projections when applicable, and enforce deterministic ordering for paging, including ties. Count/existence queries must retain the same **intended predicates** without inheriting a list's page limit.
- Permission and tenant scoping are **mandatory policy checks** at a trusted boundary, not user-selectable optional filters.

References: [composite-specification FAQ](https://specification.ardalis.com/getting-started/faq.html#how-do-i-use-composite-specifications), [specification extensions](https://specification.ardalis.com/extensions/extensions-for-specifications.html), [EF Core integration](https://specification.ardalis.com/usage/use-specification-dbcontext.html).

## Database integration and testing

Ardalis.Specification can be paired with EF Core using the ecosystem's evaluator/repository support. Keep this an integration choice; selecting the base library does not itself select EF Core or PostgreSQL.

Test predicate semantics with small unit tests where appropriate and real database integration tests when SQL translation or provider behavior matters. Verify empty pages, null values, sorting stability and query performance for high-volume cases.

Reference: [Ardalis.Specification documentation and repository](https://github.com/ardalis/Specification).
