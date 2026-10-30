---
title: "Custom RabbitMQ .NET client"
description: "Optional technology-specific engineering standard"
kind: standard
status: active
---

# Custom RabbitMQ .NET client

**Scope:** Only when this specific technology/profile is selected. This is reusable guidance, not a product requirement or proof of implementation.

## Design and ownership

Use a supported RabbitMQ .NET client rather than implementing the AMQP protocol. Own connection/channel lifetimes, topology declarations, publisher confirms, consumer acknowledgments, cancellation, bounded concurrency and recovery. Channels are not freely shareable across concurrent publishers. If building a reusable abstraction, avoid hiding delivery guarantees or error states. Test redelivery and reconnection against real RabbitMQ.

## Evidence

Record actual installed versions and authoritative contracts. Keep unsettled decisions open. Validate with a real integration test where external behavior matters; do not claim generated documentation proves the application works.
