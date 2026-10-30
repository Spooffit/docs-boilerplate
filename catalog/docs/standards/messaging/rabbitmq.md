---
title: "RabbitMQ transport"
description: "Optional technology-specific engineering standard"
kind: standard
status: active
---

# RabbitMQ transport

**Scope:** Only when this specific technology/profile is selected. This is reusable guidance, not a product requirement or proof of implementation.

## Design and ownership

RabbitMQ is a message broker, not a .NET client. Define topology ownership (exchange, queue, binding and routing key), publisher confirmations, acknowledgements, redelivery and dead-letter handling according to real consumer contracts. Avoid one shared queue for independent broadcast consumers. Do not infer exactly-once delivery or business models from choosing this transport. Choose MassTransit, Wolverine or a custom client separately.

## Evidence

Record actual installed versions and authoritative contracts. Keep unsettled decisions open. Validate with a real integration test where external behavior matters; do not claim generated documentation proves the application works.

## Delivery and topology checks

- Define durability for queues, messages and relevant exchange bindings; persistence settings do not replace publisher confirms or recovery testing. Consumers acknowledge only after the intended local handling boundary succeeds.
- Handle reject/nack, redelivery and dead-letter routing explicitly. Avoid unbounded requeue loops; record retry and poison-message ownership. Configure prefetch/concurrency based on observed workload and downstream capacity.
- Keep publish/subscribe fanout distinct from competing consumers of one queue. Define queue naming, routing keys and topology migration ownership without assuming an application-specific EventBus project.

## Verification

Test publisher disconnects, consumer crashes before/after acknowledgement, duplicate deliveries, dead-letter routes, restart and queue durability using a real broker. Measure backlog, unacked messages and connection pressure.
