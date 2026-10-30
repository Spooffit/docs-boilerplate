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
