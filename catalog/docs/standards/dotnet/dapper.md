---
title: "Dapper data access"
description: "Technology-scoped engineering standard"
kind: standard
status: active
---

# Dapper data access

**Applies to:** .NET projects that deliberately select Dapper. Dapper is a lightweight object mapper; it does not supply full schema migration or ORM tracking behavior.

## SQL and input safety

Parameterize user-supplied values; do not concatenate strings directly into executable SQL. If selecting a table, column or sort expression dynamically, choose from a strict, reviewed allowlist. Use dialect-correct parameters and explicit mapping for ambiguous column names.

## Connection, lifetime and transactions

Own connection lifetimes and transaction scope explicitly; make cancellation and command timeouts part of expected operation behavior. Where supported by the chosen API and provider, use `CommandDefinition` to forward cancellation tokens.

A query that should participate in an ambient or explicit transaction must use the correct connection and transaction. When a multi-step change requires all-or-nothing behavior, validate rollback paths.

## Query quality and verification

Inspect SQL performance for representative workloads and define explicit pagination for large datasets. Test parameter binding, row mapping, null handling, transactions and constraint failures against the real provider.

Do not automatically require a repository abstraction around every Dapper call. Define abstraction boundaries according to the owning component and project architecture.

Reference: [Dapper repository](https://github.com/DapperLib/Dapper).

## Query boundaries and consistency

- Keep filtering, authorization scope, sorting and pagination in parameterized SQL where possible. Never use user-controlled column names or sort directions without an explicit allowlist; parameters protect values, not SQL identifiers.
- Use stable ordering with a unique tie-breaker for offset pagination, and verify the execution plan and indexes for representative volumes. Specify how total counts are computed when an API needs them.
- Differentiate zero rows, SQL NULL and missing mandatory fields in the mapping contract. Explicitly control multi-mapping and one-to-many result deduplication where used; do not assume joined rows map automatically into a complete object graph.
- Do not share an open connection across parallel operations unless the actual provider and ownership contract support that use. Ensure every command participating in an explicit transaction receives the correct connection and transaction.

## Reliability and error handling

- Propagate cancellation and configure command timeout deliberately; a timeout does not prove that a write was rolled back. Verify rollback and unknown-outcome behavior, particularly around network interruptions.
- Map unique-constraint, concurrency and deadlock errors at the data-access boundary without exposing raw SQL, credentials or connection strings. Retry writes only when their idempotency and transaction semantics are established.
- Use a dedicated migration tool or explicit schema-deployment procedure; Dapper itself does not manage database migrations.

## Acceptance checks

Execute provider-backed tests for typed parameters, null mapping, multi-mapping when used, sorting, pagination, transaction rollback, concurrency conflicts and query-plan regressions. Record the actual driver/database versions and successful commands; illustrative SQL is not evidence of production correctness.
