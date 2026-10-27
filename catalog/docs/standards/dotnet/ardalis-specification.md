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

## Database integration and testing

Ardalis.Specification can be paired with EF Core using the ecosystem's evaluator/repository support. Keep this an integration choice; selecting the base library does not itself select EF Core or PostgreSQL.

Test predicate semantics with small unit tests where appropriate and real database integration tests when SQL translation or provider behavior matters. Verify empty pages, null values, sorting stability and query performance for high-volume cases.

Reference: [Ardalis.Specification documentation and repository](https://github.com/ardalis/Specification).
