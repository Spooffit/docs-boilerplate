---
title: "Integration architecture"
description: "Project-specific document scaffold"
kind: project
status: draft
---

# Integration architecture

## Integration inventory

Identify actual external systems, direction of data flow, owner and contract location.

## Applied profile and contracts

[If the project uses event synchronization, replay or resumable data exchange, consider selecting the independent `integration-reliability` profile. It documents reliability and representation boundaries without implying a programming language, broker, ORM or exact HTTP endpoints.]

## Delivery guarantees

Specify required ordering, retries, deduplication and eventual consistency only when supported by a confirmed design.

## Failure handling and operations

Record observed failure modes, relevant telemetry and recovery procedures.

## Open decisions

Use ADRs to resolve consequential transport or coupling choices.
