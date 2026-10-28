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

## Durable references and history

Where durable records refer to historical snapshots or other retained content, use constraints or an explicitly justified consistency process to prevent dangling references and silent loss of readable history. Referential integrity, scoped uniqueness and ownership need protection at the persistence boundary when concurrent writers can race; an application-side collection check alone is insufficient.

If a snapshot, its current pointer and a history record must become visible together, define a transaction that spans those **database** writes and verify recovery after failures. Do not extend PostgreSQL transaction guarantees to external object stores or message brokers. When effects cross stores, specify staged states, recovery and idempotency separately.

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

## Controlled production schema evolution

Before running a migration, establish the database owner, real existing data, supported application/client versions, deployment topology and backup/restore requirements. Do not assume data can be deleted simply because an old project once had disposable development environments.

Where rolling upgrades require compatibility, consider:

1. **Expand:** add compatible schema objects before new consumers require them.
2. **Backfill:** move existing data in bounded, restartable batches with verified constraints.
3. **Switch:** deploy writers and readers in a reviewed order, including background workers and older clients.
4. **Contract:** remove obsolete objects only after consumers and retention gates are satisfied.

This is an optional deployment strategy, not four mandatory steps for every migration. A migration that works on an empty database may fail or block on real data.

### PostgreSQL DDL safety

- Review the actual `ALTER TABLE` subcommands, their table/index locks, scans, potential rewrites and long transactions. Many forms require `ACCESS EXCLUSIVE` unless documented otherwise.
- Choose suitable `lock_timeout`/`statement_timeout`; monitor transaction duration, lock waits, disk/WAL, replication lag and any prolonged blocking.
- `CREATE INDEX CONCURRENTLY` is an option for live systems but cannot run inside a transaction block. Interrupted builds can leave invalid indexes that need explicit recovery and verification.
- Some constraints support `NOT VALID` followed by `VALIDATE CONSTRAINT`. Confirm support and behavior for the exact PostgreSQL version and type of constraint; don't generalize to every DDL statement.
- PostgreSQL's transactional DDL does **not** mean every migration operation can run inside one transaction. Plan exceptions explicitly.

### Execution, compatibility and rollback

Decide which deployment component owns the migration. Do not let multiple service replicas race to apply the same schema changes at startup. Independently owned schemas and migrations need explicit coordination where they share a database.

Preserve already-applied migrations, including historical data conversions and constraints. Code rollback does not automatically reverse a destructive schema or data change. Review backup restoration, forward recovery and compatibility across old/new application releases.

Test against a production-like database and representative existing rows. Record real evidence for migrations, query plans, constraints, data completeness, partial failures and rollback. An EF Core migration compiling is not proof that the provider SQL or old-client behavior is safe.

Use the [PostgreSQL migration plan](../../templates/postgresql-migration.md) for changes requiring a documented rollout. Its existence is not proof of successful execution.

References: [PostgreSQL ALTER TABLE](https://www.postgresql.org/docs/current/sql-altertable.html), [CREATE INDEX](https://www.postgresql.org/docs/current/sql-createindex.html), [EF Core migration deployment](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying).

## Schema changes and operations

Review migrations for data loss, table rewrites, lock impact, backward compatibility and rolling deployment sequencing. Do not auto-run destructive schema migrations in production simply because an application starts.

Use least-privilege credentials, backups and tested restore procedures. Monitor transaction duration, lock waits, replication behavior and slow queries using actual operational requirements.

## Verification

Run integration tests against a supported PostgreSQL version to validate SQL, database-specific mappings, constraints, isolation and migration behavior. An EF Core InMemory provider or SQLite substitute cannot establish PostgreSQL-specific guarantees.

References: [PostgreSQL transaction isolation](https://www.postgresql.org/docs/current/transaction-iso.html), [MVCC introduction](https://www.postgresql.org/docs/current/mvcc-intro.html), [explicit locking](https://www.postgresql.org/docs/current/explicit-locking.html).
