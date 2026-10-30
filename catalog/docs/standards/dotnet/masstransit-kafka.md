---
title: "MassTransit Kafka rider"
description: "Optional technology-specific engineering standard"
kind: standard
status: active
---

# MassTransit Kafka rider

**Scope:** Only when this specific technology/profile is selected. This is reusable guidance, not a product requirement or proof of implementation.

## Design and ownership

The Kafka rider extends a MassTransit bus; distinguish Kafka topic endpoints and consumer groups from RabbitMQ receive endpoints. Register the rider using public APIs supported by the installed MassTransit version. Define offset/commit behavior, partitioning, retries, error strategy, message versioning and test boundaries. A rider's consumer contract does not automatically share the transactional semantics of the primary bus. Verify routing against disposable Kafka.

## Evidence

Record actual installed versions and authoritative contracts. Keep unsettled decisions open. Validate with a real integration test where external behavior matters; do not claim generated documentation proves the application works.
