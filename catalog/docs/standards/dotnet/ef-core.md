---
title: "Entity Framework Core persistence"
description: "Technology-scoped engineering standard"
kind: standard
status: active
---

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

## Domain constraints and historical persistence metadata

If a project deliberately owns stable limits in its domain model, keep current model validation and database mapping consistent without creating unrelated, drifting copies. However, domain checks and ORM metadata are not interchangeable: distinguish required input, nullable columns and required nested/value-object state explicitly.

A project's named constraints class is an architectural choice, not an EF Core requirement. A migration and its model snapshot record the schema **at the time it was authored**. Do not bind previously applied migrations to mutable current-domain constants or rewrite their historical values when a new constraint changes. Review existing rows before increasing requiredness or tightening bounds.

## Transactions and concurrency

- Define transaction boundaries around logically atomic changes, considering both default SaveChanges behavior and operations spanning multiple commits.
- Configure concurrency tokens when lost-update protection is required and handle `DbUpdateConcurrencyException` as a business-visible conflict or well-defined retry strategy.
- A uniqueness violation on insert usually surfaces as a provider error, **not** `DbUpdateConcurrencyException`. Translate known constraint failures intentionally.
- Don't automatically retry non-idempotent operations without verifying transaction and side-effect safety.
- A DB transaction cannot atomically commit a message to a separate broker without a coordination pattern; consider outbox for that failure model.

## Mapping domain constraints into EF Core

Where the chosen model has authoritative fixed constraints, use them consistently in the **current** EF Core mapping where appropriate, without requiring all projects to create a static constraints container. `HasMaxLength` maps relational metadata/provider schema expectations; it is not a replacement for domain validation, and `IsRequired` prevents null at the mapped boundary, not empty or whitespace-only values. `MinLength`, regex policy and business ownership need separate enforcement.

Treat a required **owned or complex object** differently from required scalar fields inside an optional nested object. Verify the actual provider's owned-type/complex-type mapping, table splitting, converters and nullable navigation behavior. A required nested child must not inadvertently make an optional parent impossible to persist.

When a limit changes, review current model, validators, domain rules, existing rows, deployed clients and historical migrations together. Never rewrite applied migrations to read today's domain constants. EF Core model snapshots describe a schema revision and must not silently change when runtime constants change. Compile-time public `const` values may be inlined into other assemblies, so changing a shared definition can require rebuilding and republishing consumers.

## Migration discipline

Review schema diffs and data transformation for loss, locks and deployment ordering. Choose a controlled application strategy (reviewed SQL, a migration bundle, or other deliberate deployment procedure). Idempotent scripts are provider-dependent.

Do not modify previously applied migration snapshots as a shortcut for changing a live schema; coordinate expand/contract migrations for zero-downtime needs.

## Deployment-time migration review

Review the SQL generated for the actual database provider and the deployment impact before applying EF Core migrations. Valid C# and a changed `ModelSnapshot` do not prove that historical data, constraints, locks or existing consumers remain safe.

Separate migration authoring from production execution. Choose a reviewed SQL script, migration bundle or other controlled deployment procedure instead of assuming that every replica should run migrations at startup. When rolling deployment is required, test compatibility of old/new application versions with the intermediate schema.

Do not rewrite applied migrations or make their historic behavior depend on mutable current constants. Explicitly plan destructive data changes and remember that rolling back application code does not roll back deleted data. EF Core does not imply PostgreSQL or any one database-specific DDL rule.

## Test evidence

Test actual migrations, mappings, SQL translation, provider-specific features, transactions and concurrency against the intended database. An in-memory substitute is useful only for narrower logic checks, not database fidelity.

References: [EF async](https://learn.microsoft.com/en-us/ef/core/miscellaneous/async), [EF concurrency](https://learn.microsoft.com/en-us/ef/core/saving/concurrency), [applying migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying).


## Query execution and context boundaries

- Treat query specifications as optional EF Core-specific query organization, never as a requirement for Dapper. If selected, apply filters, projections and ordering before query materialization; authorization remains an independent trusted boundary.
- Prefer explicit projections for API read models. Inspect the generated SQL, indexes and query plans before selecting split versus single queries for collection relationships; each has performance and consistency trade-offs.
- When using context pooling or factory-created contexts, verify that per-request tenant or other scoped state cannot leak between reused instances. Context pooling is distinct from database connection pooling.
- Review provider-specific retry execution strategies and interaction with manually controlled transactions. An ambiguous commit outcome must not lead to silent duplication of writes.

## Provider-backed verification

Exercise uniqueness conflicts, concurrency tokens, transaction rollback, cancellation, migrations and schema compatibility against the actual provider. If a PostgreSQL connection pooler is selected, also test session-dependent features and migrations through the configured routing mode. Document versions and observed outcomes.
