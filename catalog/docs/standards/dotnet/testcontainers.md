---
title: "Testcontainers for .NET"
description: "Technology-scoped engineering standard"
kind: standard
status: active
---

# Testcontainers for .NET

**Applicability:** test suites that use Testcontainers for .NET with disposable container infrastructure.

## Resource naming and networking

- Use generated container, network and volume names rather than static names.
- Let Docker assign published host ports dynamically and retrieve the resulting mapped port.
- Connect to the test container via its reported host and mapped host port; do not hardcode `localhost`.
- For container-to-container communication, use network aliases and internal container ports.
- Pin container image versions. Review upgrades deliberately.

## Lifecycle and cleanup

- Start dependencies before exercising their clients and wait for meaningful readiness, not arbitrary startup delays.
- Prefer test fixtures with explicit resource ownership and teardown.
- Do not disable the Testcontainers Resource Reaper as a routine workaround. It cleans up abandoned test resources.
- Docker access, private image registry permissions and container runtime compatibility are explicit CI prerequisites.
- Avoid shared mutable databases, queues and schemas across parallel jobs without isolation.

## Logical isolation when sharing containers

One disposable PostgreSQL container may serve multiple tests **only if** each concurrently writing scope owns a distinct database, or another isolation mechanism demonstrably prevents collisions. Database reset tools are for safely sequenced scopes: do not reset a shared mutable database while other tests are still running against it.

A RabbitMQ test fixture can use a separate virtual host per independent parallel scope so production-like exchange/queue names can remain stable inside that scope. Isolate bindings and consumer state. Prefer a separate container for tests of broker-wide configuration, plugins, policies, node restarts or whole-broker recovery.

Start and dispose resources according to the **actual fixture owner**. Resource Reaper helps with abandoned containers but does not substitute for correctly disposing databases, vhosts and other logical resources. Choose a CI-wide concurrency budget after measuring the sum of Integration/Functional/E2E workloads, not from a fixed boilerplate number.

## Distinguish test scopes

Use real dependencies when testing provider behavior, migrations, SQL, routing, serialization and failure recovery. Not every unit test needs Testcontainers, and not every functional test needs the entire distributed application.

## Review

Check image pinning, unique resource names, readiness, mapped host ports, cleanup, CI prerequisites and negative-path assertions.

Official references: [best practices](https://dotnet.testcontainers.org/api/best_practices/), [Resource Reaper](https://dotnet.testcontainers.org/api/resource_reaper/).
