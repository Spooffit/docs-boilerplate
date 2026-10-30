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

## Client boundary and failure tests

- Give connections and channels explicit owners. Confirm thread-safety assumptions for the installed client; coordinate concurrent publication on channels rather than sharing them without protection.
- Use publisher confirms for delivery feedback and consumer acknowledgements after successful local processing. Handle uncertain confirmation, redelivery and deduplication explicitly.
- Define bounded prefetch, cancellation, shutdown, dead-letter routing and topology declaration ownership. Avoid immediate infinite requeue.
- Test network partition, connection recovery, consumer crash before acknowledgement and duplicate messages on a real broker.
