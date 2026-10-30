---
title: "MassTransit messaging client"
description: "Technology-scoped engineering standard"
kind: standard
status: active
---

# MassTransit messaging client

**Only applies when the matching profile is explicitly selected.**

## Rules

MassTransit is selected independently of RabbitMQ or Kafka. Configure the matching transport or Kafka rider in the application composition root using the installed package versions. Maintain external message contracts independent of application internals, and define consumer ownership, retries, outbox and idempotency based on real delivery guarantees. RabbitMQ receive endpoints and Kafka topic subscriptions have different semantics.

## Verification

Record actual versions, settings and executed test evidence; the template does not decide them for the project.

## Transport-specific composition and guarantees

- Select a transport separately from MassTransit itself. RabbitMQ uses receive endpoints and broker acknowledgements; Kafka requires a rider and topic/consumer-group configuration. Do not reuse RabbitMQ topology or retry assumptions for Kafka.
- Define the ownership of consumers, integration contract versions, endpoint names and subscriptions at application boundaries. No fixed three-project EventBus hierarchy is required.
- Distinguish consumer retry, delayed redelivery, fault/error transport, outbox persistence and duplicate suppression. A successfully acknowledged message is not proof that every downstream side effect committed, and at-least-once delivery does not imply exactly-once business effects.
- When the database transaction and outgoing message must be coordinated, select and test a supported transactional outbox strategy. An in-memory outbox alone does not make publishing and database commits atomic.
- Set bounded concurrency and retry policies for transient failures; classify permanent contract/validation failures separately. Ensure poison messages are visible and recoverable without endless hot-loop requeues.

## Integration verification

Run the installed MassTransit version against the selected broker. Test duplicate deliveries, consumer exceptions, restart/recovery, cancellation, outgoing-message failure and persistence/outbox consistency. Verify topology, subscriptions and error queues or Kafka failure handling in the actual transport.
