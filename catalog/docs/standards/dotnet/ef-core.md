# Entity Framework Core persistence

**Applies to:** .NET projects that deliberately select EF Core. The target database is a separate choice; selecting EF Core does not imply PostgreSQL.

## Context lifecycle and async I/O

- Treat `DbContext` as a unit-of-work scoped dependency, not as a thread-safe singleton.
- **Do not run simultaneous operations on one `DbContext`**, including parallel async queries. Await work sequentially or create separate contexts with properly managed lifetimes.
- Propagate cancellation tokens to database operations where the provider supports them. Cancellation is cooperative and may not guarantee rollback of work already committed.
- Use tracking for intended updates; select projections and `AsNoTracking` deliberately for read-only queries, where they improve clarity and performance.

## Data shape and query correctness

- Define mapping, requiredness, value conversions, keys and relationships from actual domain and persistence needs.
- Avoid client-side filtering of unbounded datasets and N+1 query behavior. Profile relevant queries against production-like volumes.
- Avoid directly exposing tracked entities as public transport DTOs.
- Configure model constraints and database constraints consistently. Input validation never substitutes for concurrent uniqueness guarantees.

## Transactions and concurrency

- Define transaction boundaries around logically atomic changes, considering both default SaveChanges behavior and operations spanning multiple commits.
- Configure concurrency tokens when lost-update protection is required and handle `DbUpdateConcurrencyException` as a business-visible conflict or well-defined retry strategy.
- A uniqueness violation on insert usually surfaces as a provider error, **not** `DbUpdateConcurrencyException`. Translate known constraint failures intentionally.
- Don't automatically retry non-idempotent operations without verifying transaction and side-effect safety.
- A DB transaction cannot atomically commit a message to a separate broker without a coordination pattern; consider outbox for that failure model.

## Migration discipline

Review schema diffs and data transformation for loss, locks and deployment ordering. Choose a controlled application strategy (reviewed SQL, a migration bundle, or other deliberate deployment procedure). Idempotent scripts are provider-dependent.

Do not modify previously applied migration snapshots as a shortcut for changing a live schema; coordinate expand/contract migrations for zero-downtime needs.

## Test evidence

Test actual migrations, mappings, SQL translation, provider-specific features, transactions and concurrency against the intended database. An in-memory substitute is useful only for narrower logic checks, not database fidelity.

References: [EF async](https://learn.microsoft.com/en-us/ef/core/miscellaneous/async), [EF concurrency](https://learn.microsoft.com/en-us/ef/core/saving/concurrency), [applying migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying).
