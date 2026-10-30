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


## Service and data boundaries

- Each service must have a defined runtime owner, deployment unit, operational responsibility and authoritative write ownership for its data. A shared database is not an excuse for unrestricted cross-service writes.
- Publish explicit and versioned APIs or message contracts. Define backward-compatibility windows, schema evolution and consumer verification so services can deploy independently.
- Distinguish synchronous dependency availability from asynchronous eventual delivery. Document ordering, deduplication and consistency guarantees only where required by the actual contract.

## Failures and delivery

- Set bounded timeouts, cancellation propagation and retry budgets for network calls. Repeating a write requires a defined idempotency boundary and a policy for unknown outcomes.
- For reliable messages, evaluate durable outbox/inbox, acknowledgement, redelivery and poison-message handling according to the selected broker/client. Do not assume exactly-once effects from at-least-once delivery.
- Keep local transactions local. Where multiple service owners must coordinate, explicitly define eventual consistency or compensating actions; do not pretend that distributed atomicity comes for free.
- Define readiness, observability and trace correlation between services, with clear limits on sensitive information in logs and messages.

## Verification scenarios

Prove independent deployment and old/new contract compatibility. Exercise unavailable dependencies, duplicate/reordered messages where relevant, retry exhaustion, partial writes, recovery and access isolation with representative integration tests. Record real versions and evidence; documentation is not proof of implementation.
