---
title: "Integration architecture"
description: "Project-specific document scaffold"
kind: project
status: draft
---

# Integration architecture

## Integration inventory

Identify actual external systems, direction of data flow, owner and contract location.

## Contracts and boundaries

Link the actual published schemas, protocols and integration owners. An integration document does not itself select a message broker, synchronization mechanism or `integration-reliability` technology profile. Include reliability guidance only when the project has explicitly selected it and the relevant failure modes exist.

## Delivery guarantees

Specify required ordering, retries, deduplication and eventual consistency only when supported by a confirmed design.

## Failure handling and operations

Record observed failure modes, relevant telemetry and recovery procedures.

## Open decisions

Link an existing decision or create an ADR only for a consequential unresolved tradeoff. Omit this section when no open choices exist.
