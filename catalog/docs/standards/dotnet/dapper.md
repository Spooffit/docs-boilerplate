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
