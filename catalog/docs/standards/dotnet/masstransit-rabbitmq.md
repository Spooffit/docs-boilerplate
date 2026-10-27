# MassTransit and RabbitMQ messaging

**Applies to:** .NET applications that deliberately select MassTransit with the RabbitMQ transport. Neither the broker nor this topology is required by .NET or CQRS.

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

## Reliability and delivery semantics

- At-least-once delivery means consumers may receive repeated messages. Make domain side effects idempotent within the chosen duplicate-detection window.
- Retry/redelivery, in-memory outbox, transactional bus outbox and transactional consumer outbox solve **different** failure modes. Choose one or more based on measured requirements and data ownership, not by habit.
- For a database update plus event publication, a transactional outbox may coordinate persistence with outgoing messages; message delivery and consumer commit still occur separately.
- Correlation IDs and message IDs assist diagnostics, but are not proof of exactly-once delivery.
- Use bounded retries, dead-letter/error handling, observability and poison-message recovery policies appropriate to the consumer.
- Verify ordering guarantees where they are contractually required; never assume global ordering in a horizontally scaled system.

## Change management and tests

A rename of external message identity, exchange, routing key, binding or serialized member can break existing consumers. Treat it as an integration contract change with a migration/versioning plan.

Test contract serialization separately from live RabbitMQ routing. Use a disposable RabbitMQ instance for integration tests of exchanges, bindings and receive endpoints; verify duplicate delivery, retries and outbox recovery at the appropriate scope.

References: [MassTransit topology](https://masstransit.io/documentation/configuration/topology), [RabbitMQ transport](https://masstransit.io/documentation/configuration/transports/rabbitmq), [MassTransit outbox](https://masstransit.io/documentation/patterns/transactional-outbox).
