---
title: "PgBouncer connection pooling"
description: "Technology-scoped engineering standard"
kind: standard
status: active
---

# PgBouncer connection pooling

**Only applies when the matching profile is explicitly selected.**

## Rules

PgBouncer is a PostgreSQL connection pooler; choose session, transaction or statement mode based on real client behavior. Session pooling preserves session state; transaction pooling can break session-level features and requires careful review of prepared statements, temporary tables, session settings, advisory locks and ORM drivers. Define pool sizes and server limits from measured concurrency. Use TLS, least-privilege authentication and metrics; test failover, timeouts and migrations through the pooler. Source: https://www.pgbouncer.org/features.html

## Verification

Record actual versions, settings and executed test evidence; the template does not decide them for the project.
