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
