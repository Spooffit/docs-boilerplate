---
title: "PgBouncer connection pooling"
description: "Technology-scoped engineering standard"
kind: standard
status: active
---

# PgBouncer connection pooling

**Applies to:** PostgreSQL deployments explicitly selecting PgBouncer. The document does not prescribe a production configuration without workload measurements.

## Pooling contract

- Choose `session`, `transaction` or `statement` mode explicitly. Session mode preserves connection-level state for a client session. Transaction pooling releases the server connection after each transaction; statement pooling has stricter transaction semantics and is unsuitable for workloads requiring multi-statement transactions.
- Audit session-specific behavior before transaction pooling: temporary tables, session `SET`, `LISTEN/NOTIFY`, session-level advisory locks and prepared statements. Prepared-statement compatibility depends on PgBouncer version, protocol and `max_prepared_statements`; do not assert blanket support or incompatibility.
- Test the actual Npgsql/EF Core/Dapper versions through PgBouncer, including auto-prepare, explicit transactions, cancellation, command timeouts and schema changes. Configure migration/administrative connection routing deliberately; a session/direct connection may be required for certain operations.

## Capacity and security

- Budget server-side connections across all pool instances, databases, users, application instances and reserved administrative connections. Tune `default_pool_size`, `max_client_conn` and other limits based on measured concurrency, queue time and PostgreSQL `max_connections`; no universal values are supplied.
- Define authentication, credential rotation, TLS on both relevant hops, secret handling and least-privilege database accounts. Do not publish passwords or permissive example credentials.
- Document readiness behavior, drain/restart and backend failover. A healthy PgBouncer listener alone does not prove backend reachability or application readiness.

## Verification

Run representative concurrency and failure tests: pool saturation, queue waiting, backend restart, transaction rollback, cancellation, prepared statements, migrations and any session-dependent SQL. Record exact pooler/client versions, selected mode, relevant settings, test commands, observed outcomes and operational metrics. Examine active/idle/waiting connections and server pressure. Passing a Markdown check does not validate pooling behavior.

References: [PgBouncer features](https://www.pgbouncer.org/features.html), [PgBouncer configuration](https://www.pgbouncer.org/config.html).
