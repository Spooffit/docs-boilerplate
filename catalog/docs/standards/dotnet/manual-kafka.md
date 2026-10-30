---
title: "Custom Kafka .NET client"
description: "Optional technology-specific engineering standard"
kind: standard
status: active
---

# Custom Kafka .NET client

**Scope:** Only when this specific technology/profile is selected. This is reusable guidance, not a product requirement or proof of implementation.

## Design and ownership

Prefer a maintained Kafka client such as Confluent.Kafka; do not implement the wire protocol. Own producer/consumer lifetimes, subscription, group rebalancing, partition assignment, delivery reports and offset commit policy. Define serializer/schema handling and bounded retries. Do not conflate successful produce with a committed business transaction; test replay, duplicates and shutdown on real Kafka.

## Evidence

Record actual installed versions and authoritative contracts. Keep unsettled decisions open. Validate with a real integration test where external behavior matters; do not claim generated documentation proves the application works.

## Client boundary and failure tests

- Define producer delivery reports, partitioning strategy and consumer-group ownership. Choose offset commit timing in relation to local side effects and document ambiguous outcomes.
- Handle assignment/revocation, rebalances, bounded processing and orderly shutdown. Do not allow unbounded buffering to obscure overload.
- Treat replay and duplicate records as expected failure conditions; validate schema evolution and idempotent processing. Broker-side producer guarantees alone do not imply exactly-once external effects.
- Test broker outage, consumer restart, rebalance, offset replay and poison records on a representative Kafka cluster.
