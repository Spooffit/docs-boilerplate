---
title: "Microservices architecture"
description: "Optional technology-specific engineering standard"
kind: standard
status: active
---

# Microservices architecture

**Scope:** Only when this specific technology/profile is selected. This is reusable guidance, not a product requirement or proof of implementation.

## Design and ownership

Choose microservices only for explicitly independently deployable services with justified team, scaling or lifecycle boundaries. Define service ownership, data ownership, network contracts, version compatibility, failure isolation, observability and deployment coordination. Distributed transactions are not implied. Prefer clear synchronous/asynchronous interaction contracts; test degraded dependencies, idempotency and cross-service behavior. A documentation module or DDD bounded context does not automatically equal a microservice.

## Evidence

Record actual installed versions and authoritative contracts. Keep unsettled decisions open. Validate with a real integration test where external behavior matters; do not claim generated documentation proves the application works.
