---
title: "Odyssey PostgreSQL connection pooling"
description: "Technology-scoped engineering standard"
kind: standard
status: active
---

# Odyssey PostgreSQL connection pooling

**Only applies when the matching profile is explicitly selected.**

## Rules

Odyssey is a multi-threaded PostgreSQL pooler and router. Define pooling mode, database/user rules, authentication and backend connection limits. Review behavior for session-bound PostgreSQL features under transaction pooling. Check deployment platform support and compatibility with the actual driver, ORM, prepared statements and migration executor. Observe active/waiting sessions and failure behavior. Source: https://github.com/yandex/odyssey

## Verification

Record actual versions, settings and executed test evidence; the template does not decide them for the project.

## Pooling and routing contract

- Define database/user routing, backend targets and the pooling mode explicitly for each route. Choose session or transaction pooling based on the application transaction boundary and any server-session state; do not assume all client features survive a server-connection switch.
- Check temporary tables, session-level settings, advisory locks, notifications and prepared statements against the exact Odyssey release and driver configuration. A transaction-mode client may use a different PostgreSQL backend after commit.
- Define a dedicated direct or session route for migration and administrative tools when their behavior depends on session affinity. Test the actual EF Core, Npgsql and Dapper operations, not only initial connectivity.

## Capacity and operational safety

- Set pool size, client limits, connection queue timeouts and backend lifetime limits from measured concurrency and PostgreSQL connection budgets across all instances. Verify parameter names and units against the deployed Odyssey version.
- Configure authentication, TLS where required, secrets and least-privilege credentials on each relevant hop. Plan credential rotation and route isolation.
- Define readiness and restart behavior. Test pool saturation, wait queues, backend failover, rollback after a broken connection, cancellation and recovery.

## Verification

Record the exact Odyssey version, routing and pooling mode, actual client/provider versions, reproducible test commands and observed results. Include tests for prepared statements, session state, migrations and failure recovery. Documentation alone is not proof of a validated production configuration.

Reference: [Odyssey project and documentation](https://github.com/yandex/odyssey).
