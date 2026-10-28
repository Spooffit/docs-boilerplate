---
title: "MassTransit and RabbitMQ messaging"
description: "Technology-scoped engineering standard"
kind: standard
status: active
---

# MassTransit and RabbitMQ messaging

**Applies to:** .NET applications that deliberately select MassTransit with the RabbitMQ transport. Neither the broker nor this topology is required by .NET or CQRS.

## Optional three-project EventBus composition

A large .NET solution may explicitly adopt an `Integration/EventBus` project-and-solution-folder convention with three projects:

- `{Solution}.Integration.EventBus.Contracts`: transport-neutral serialized integration events, command/request/response contracts and simple contract DTOs, but no consumers, persistence or RabbitMQ topology.
- `{Solution}.Integration.EventBus.RabbitMq`: depends on Contracts and selected transport packages; owns shared MassTransit/RabbitMQ message and publish/send topology.
- `{Solution}.Integration.EventBus.Default`: references Contracts and RabbitMq and exposes the project's chosen composition registration, without duplicating messages or alternate broker rules.

The dependency direction is Contracts ← RabbitMq ← Default; consumer applications own their own receive endpoints. Contracts may use a transport-neutral abstraction package **if justified by the chosen contract APIs**; do not require it. Contract DTOs should not leak mutable domain entities.

This is an **optional architecture convention** pending project selection, not a universal directory scaffold. Register a central topology component only when the project benefits from it; naming it `{Context}TopologyRegistrar` is also optional. A pure composition project need not contain artificial service types. Verify actual package dependencies, dependency direction and solution folders when this variant is adopted.

## Contracts, adapters and topology ownership

- Put message DTOs and their compatibility policy in a contract boundary that does not depend on application/domain internals or RabbitMQ transport types.
- Keep business logic in the application or consumer handlers, not in broker topology configuration.
- Keep exchange, routing-key, queue and subscription configuration in a deliberate owner. Do not duplicate arbitrary exchange strings across publishers and consumers.
- A split into `Integration/EventBus/Contracts`, `RabbitMq`, and `Default` may help large modular solutions, but is an optional project convention, not a universal MassTransit requirement.
- Define consumer queue names and bindings by application responsibility. A shared queue means competing consumers, not broadcast to every service.

## Exchanges and stable message identity

RabbitMQ publish topology commonly uses exchanges associated with message types, with MassTransit defaults and override capabilities. MassTransit supports changing exchange entity names with `SetEntityName`, and controlling exchange types and bindings through publish/send and receive topology.

- Design stable external exchange identities and serialized contract versions independently from arbitrary CLR refactoring.
- Use `fanout`, `direct` or `topic` according to actual routing semantics. **Do not require `fanout` for every message.**
- Explicit queue bindings and routing keys must agree with the selected exchange type. With `topic`, the consumer binding pattern and publisher routing key form an actual protocol.
- Document durability, auto-delete and failure behaviors for production use; do not silently alter them in a routine refactor.
- The application consuming a message decides its endpoint queue and actual handler ownership. The publisher should not require knowing every consuming service.

## Explicit topology and correlation

If a message is intentionally published to a stable externally consumed exchange, keep its exchange identity in a documented, centralized place. A scheme such as `msg.{integration}.{purpose}` is a **candidate project naming rule**, not MassTransit's required default. Define renaming and coexistence policy so refactoring a CLR type or namespace does not silently rename the public broker identity. Where explicit identity is necessary, use the version-compatible MassTransit message topology API such as `SetEntityName`, or an approved entity-name formatter.

Define publish exchange type (`fanout`, `direct`, `topic`), durability and auto-delete from the actual integration contract. RabbitMQ's default MassTransit publish topology may already supply appropriate settings; **do not** blindly force each message to a copied `fanout` registrar. For `direct`/`topic`, verify publisher routing keys against actual consumer bindings; exchange declaration alone does not route a message to a specific subscriber.

Where correlation is part of the contract, document the source field, the message-envelope header mapping, and the distinction from message identity and domain aggregate identity. MassTransit supports a conventional `CorrelationId` and explicit `UseCorrelationId` mapping. Do not generate a new unrelated correlation value in a shared registrar when the agreed message supplies one.

Publish/message topology can be shared, but queue names, receive endpoint declarations, consumers and consumer-specific retry/redelivery configuration belong to their consuming application. Independent services require independent queues for broadcast-like publication; sharing a queue creates competing-consumer semantics.

## Reliability and delivery semantics

- At-least-once delivery means consumers may receive repeated messages. Make domain side effects idempotent within the chosen duplicate-detection window.
- Retry/redelivery, in-memory outbox, transactional bus outbox and transactional consumer outbox solve **different** failure modes. Choose one or more based on measured requirements and data ownership, not by habit.
- For a database update plus event publication, a transactional outbox may coordinate persistence with outgoing messages; message delivery and consumer commit still occur separately.
- Correlation IDs and message IDs assist diagnostics, but are not proof of exactly-once delivery.
- Use bounded retries, dead-letter/error handling, observability and poison-message recovery policies appropriate to the consumer.
- Verify ordering guarantees where they are contractually required; never assume global ordering in a horizontally scaled system.

## Outbox choices and contract evolution

When an application changes a database and publishes an event, document the dual-write failure boundary. A transactional outbox is a common solution but **not mandatory for every integration**; choose the existing project mechanism where its guarantees match the scenario. Distinguish in-memory outbox buffering, durable bus outbox, consumer outbox/inbox and broker retry. An outbox does not remove at-least-once delivery or guarantee exactly-once business effects.

Review serialized member names/types, correlation semantics, durable topology, exchange identity/type, routing keys and bindings as versioned integration-contract surfaces. Compatibility may require parallel old/new bindings, dual publication or staged consumer rollout; a package's semantic version alone cannot guarantee wire compatibility. Record which message changes need versioning for the project's real consumers, rather than demanding a fixed numbering scheme for every private message.

## Change management and tests

A rename of external message identity, exchange, routing key, binding or serialized member can break existing consumers. Treat it as an integration contract change with a migration/versioning plan.

Test contract serialization separately from live RabbitMQ routing. Use a disposable RabbitMQ instance for integration tests of exchanges, bindings and receive endpoints; verify duplicate delivery, retries and outbox recovery at the appropriate scope.

References: [MassTransit topology](https://masstransit.io/documentation/configuration/topology), [RabbitMQ transport](https://masstransit.io/documentation/configuration/transports/rabbitmq), [MassTransit outbox](https://masstransit.io/documentation/patterns/transactional-outbox).
