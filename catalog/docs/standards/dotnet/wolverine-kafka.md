---
title: "Wolverine messaging with Kafka"
description: "Optional technology-specific engineering standard"
kind: standard
status: active
---

# Wolverine messaging with Kafka

**Scope:** Only when this specific technology/profile is selected. This is reusable guidance, not a product requirement or proof of implementation.

## Design and ownership

Use the `WolverineFx.Kafka` transport with the installed Wolverine version. Define topic and consumer-group ownership, partition key, offset handling, batching, back pressure and restart semantics. RabbitMQ-style requeue does not carry over to Kafka; review Wolverine's actual retry and dead-letter semantics. Treat handler durability, broker acknowledgment and business transaction as separate outcomes. Validate on a test Kafka cluster.

## Evidence

Record actual installed versions and authoritative contracts. Keep unsettled decisions open. Validate with a real integration test where external behavior matters; do not claim generated documentation proves the application works.
