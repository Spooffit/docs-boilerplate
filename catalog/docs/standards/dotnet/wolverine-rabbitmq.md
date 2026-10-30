---
title: "Wolverine messaging with RabbitMQ"
description: "Optional technology-specific engineering standard"
kind: standard
status: active
---

# Wolverine messaging with RabbitMQ

**Scope:** Only when this specific technology/profile is selected. This is reusable guidance, not a product requirement or proof of implementation.

## Design and ownership

Use Wolverine's RabbitMQ transport package and documented host registration. Define endpoints, exchanges, binding ownership and durable versus in-memory handling explicitly. Wolverine local messaging and external broker dispatch are different boundaries. Confirm outbox/inbox persistence requirements, retries, error routes and idempotency according to actual failures. Test real RabbitMQ topology and concurrent consumers.

## Evidence

Record actual installed versions and authoritative contracts. Keep unsettled decisions open. Validate with a real integration test where external behavior matters; do not claim generated documentation proves the application works.

## Reliability contract

- Document whether messages use durable endpoints and persistence or in-memory processing. Broker delivery, handler completion and database commit are distinct outcomes.
- Configure retries with finite limits and an observable failure destination; test poison messages without endless requeue.
- Verify transactional outbox/inbox configuration against the selected database and Wolverine version instead of assuming broker acknowledgement makes writes atomic.
- Test publisher interruption, consumer restart, duplicate delivery, topology changes and bounded concurrency with real RabbitMQ.
