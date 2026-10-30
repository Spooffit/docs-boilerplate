---
title: "Custom query specifications on EF Core"
description: "Optional technology-specific engineering standard"
kind: standard
status: active
---

# Custom query specifications on EF Core

**Applies to:** projects selecting EF Core and the custom-specification option. This pattern is not applicable to a Dapper-only data-access selection. It is documentation guidance, not an implementation supplied by the generator.

## Scope and contract

- Create specifications for meaningful, reusable query intent, not one wrapper class per trivial query. A direct EF Core query remains valid when a specification adds no value.
- Define the contract before implementing it: entity/result type, predicates, ordering, paging, projections, includes, tracking policy and cancellation ownership. Keep public query inputs validated and immutable after construction.
- Query specifications must not execute I/O, mutate entities, call `SaveChanges`, depend on `HttpContext`, or hide authorization decisions in optional caller-controlled flags.
- Keep tenant/ownership authorization constraints enforced at a trusted data-access boundary, independently of caller-provided filters. Document how that invariant is preserved for every query path.

## Expression composition

- Build provider-translatable `Expression<Func<TEntity, bool>>` predicates or carefully bounded `IQueryable<TEntity>` transformations before materialization. Never switch silently to `IEnumerable`, `Compile()` or `AsEnumerable()` to make database filters work.
- Define explicit AND/OR grouping. Combining expressions with distinct parameter objects requires correct parameter rebinding; avoid `Expression.Invoke` unless translation by the actual provider is verified.
- Keep reusable predicates independent from includes, tracking and paging unless the contract explicitly represents a complete query specification.
- Apply deterministic sorting with a unique tie-breaker before offset pagination. If cursor pagination is selected, define a stable cursor contract and tie handling.
- Separate filtered total counts from paged results: counts must preserve relevant filters without inheriting `Skip`/`Take`. Query shape and projection must be explicit to prevent unexpected data exposure.

## EF Core integration

- Apply specification operations to the same provider-backed query before terminal methods such as `ToListAsync` and `CountAsync`. Use `AsNoTracking` intentionally for read-only results and tracking only for updates.
- Define a predictable composition order for filtering, projection, ordering and paging, and test the resulting SQL. Do not promise that every LINQ expression is translatable on every provider/version.
- Use short-lived `DbContext` instances with an appropriate scope. Do not run parallel operations against one context. Forward cancellation tokens to terminal async calls.
- Avoid a generic repository layer merely to host specifications; introduce an abstraction only when it represents an actual application boundary.

## Verification

Test nulls, empty results, AND/OR precedence, access scoping, stable page boundaries, projection, cancellation and SQL translation against the actual database provider. Include high-volume cases where query shape and indexing affect correctness or latency. Record the real EF Core/provider versions and executed tests; generated text does not establish conformance.

See [EF Core queries](https://learn.microsoft.com/en-us/ef/core/querying/), [client versus server evaluation](https://learn.microsoft.com/en-us/ef/core/querying/client-eval), and [EF Core async operations](https://learn.microsoft.com/en-us/ef/core/miscellaneous/async).
