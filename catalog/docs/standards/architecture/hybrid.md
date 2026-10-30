---
title: "Hybrid monolith and microservices"
description: "Technology-scoped engineering standard"
kind: standard
status: active
---

# Hybrid monolith and microservices

**Only applies when the matching profile is explicitly selected.**

## Rules

Use a deployable monolith alongside independently deployable services only when specific scaling, organizational or integration requirements justify the hybrid. Explicitly define the monolith boundary, service responsibilities, data ownership, synchronous and asynchronous contracts, observability and deployment coordination. Avoid treating an internal module as a service until it has independent deployment and operational ownership. Test partial failure, retries and data consistency.

## Verification

Record actual versions, settings and executed test evidence; the template does not decide them for the project.

## Ownership and deployment boundaries

- Record which components are deployed together as the monolith and which are independently deployed services. A module or bounded context alone is not a microservice.
- Assign one authoritative write owner to each data set. Do not grant services direct writes to another component's owned tables as a shortcut; shared databases require explicitly documented ownership and migration constraints.
- Define contract ownership, versioning and compatibility for each HTTP, event or other cross-boundary interface. Distinguish an in-process call from a remote dependency when reasoning about latency, timeouts and failure.

## Reliability and evolution

- Make transaction boundaries explicit. A local database transaction cannot commit changes in another service or broker atomically. Use idempotent consumers, transactional outbox/inbox or compensating workflows when justified by actual consistency requirements.
- Define timeouts, bounded retries, backoff, circuit breaking where appropriate and behavior when a remote service is unavailable. Do not retry non-idempotent work blindly.
- Specify a backward-compatible deployment order for contracts and schema changes, including rollback limits. Avoid assuming simultaneous releases of monolith and services.
- Carry trace/correlation context across process boundaries without leaking secrets or sensitive payloads. Distinguish platform health checks from end-to-end readiness.

## Verification scenarios

Test contract compatibility while old/new versions coexist; degraded remote calls; duplicate and delayed messages where messaging exists; partial commits; dependency restoration; and independent deployment. Record observed evidence rather than treating the chosen architecture as already implemented.
