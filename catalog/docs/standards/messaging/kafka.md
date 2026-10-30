---
title: "Apache Kafka transport"
description: "Optional technology-specific engineering standard"
kind: standard
status: active
---

# Apache Kafka transport

**Scope:** Only when this specific technology/profile is selected. This is reusable guidance, not a product requirement or proof of implementation.

## Design and ownership

Treat Kafka as an append-only partitioned log, not a queue with RabbitMQ semantics. Specify topics, partition keys, consumer groups, offset commits, retention and schema compatibility according to real requirements. Ordering holds within a partition, not globally. Handle replay, rebalances and duplicate effects explicitly; avoid promising end-to-end exactly-once business actions. Select MassTransit rider, Wolverine or a custom client separately.

## Evidence

Record actual installed versions and authoritative contracts. Keep unsettled decisions open. Validate with a real integration test where external behavior matters; do not claim generated documentation proves the application works.

## Consumption verification

- Define partition keys according to required ordering and concurrency, not as a substitute for global ordering.
- Document when offsets are committed relative to database writes. Offset commits do not make external side effects atomic; replay may create duplicates.
- Establish retention, compatible schema evolution, poison-record processing and consumer lag monitoring according to the selected application's requirements.
- Exercise rebalances, replay, failed handlers, duplicates and recovery with a representative Kafka cluster. Record the observed behavior and installed client versions.
