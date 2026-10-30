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
