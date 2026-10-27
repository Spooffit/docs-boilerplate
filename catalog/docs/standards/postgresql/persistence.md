---
title: "PostgreSQL persistence and migrations"
description: "Technology-scoped engineering standard"
kind: standard
status: active
---

# PostgreSQL persistence and migrations

**Applies to:** projects using PostgreSQL, independent of programming language and ORM.

## Model constraints and transactional invariants

Use database constraints for invariants that must remain true under concurrent transactions, such as unique keys, foreign keys, non-nullability and supported CHECK predicates. Application validation may improve error messages but cannot enforce uniqueness under races.

For operations requiring an atomic change to multiple rows or tables, use a database transaction with an explicit boundary. Avoid performing irreversible external effects in the same assumed transaction unless a distributed consistency design is in place.

## MVCC, isolation and concurrent access

- PostgreSQL uses multiversion concurrency control (MVCC). Transaction isolation and locking affect which reads and writes can conflict.
- PostgreSQL's `READ UNCOMMITTED` behaves like `READ COMMITTED`; do not assume dirty reads are possible.
- The default `READ COMMITTED` isolation takes a new snapshot for each statement, so repeated queries in a transaction can observe changed data.
- `REPEATABLE READ` uses a stable snapshot but does not replace deliberate serialization of every invariant.
- `SERIALIZABLE` may abort transactions with serialization failures. Retry the **entire transaction** only when the operation can be safely retried, and bound attempts with observability.
- Use `SELECT ... FOR UPDATE` or other row-level locks only when their locking semantics solve an identified race. Avoid long-running transactions that hold locks unnecessarily.

## Data access and SQL

- Always parameterize user-controlled values in data queries; schema identifiers require vetted allowlists or trustworthy composition.
- Check query plans and indexes for measured critical workloads; do not add indexes solely by convention.
- Avoid N+1 query patterns and unbounded result sets where they affect required performance.
- Distinguish empty, absent and nullable values in API/domain conversion.
- Use `timestamptz` for instants that represent absolute time; define time zone display policy separately.

## Schema changes and operations

Review migrations for data loss, table rewrites, lock impact, backward compatibility and rolling deployment sequencing. Do not auto-run destructive schema migrations in production simply because an application starts.

Use least-privilege credentials, backups and tested restore procedures. Monitor transaction duration, lock waits, replication behavior and slow queries using actual operational requirements.

## Verification

Run integration tests against a supported PostgreSQL version to validate SQL, database-specific mappings, constraints, isolation and migration behavior. An EF Core InMemory provider or SQLite substitute cannot establish PostgreSQL-specific guarantees.

References: [PostgreSQL transaction isolation](https://www.postgresql.org/docs/current/transaction-iso.html), [MVCC introduction](https://www.postgresql.org/docs/current/mvcc-intro.html), [explicit locking](https://www.postgresql.org/docs/current/explicit-locking.html).
