---
title: "Separated contracts and messaging adapters"
description: "Technology-scoped engineering standard"
kind: standard
status: active
---

# Separated contracts and messaging adapters

**Only applies when the matching profile is explicitly selected.**

## Rules

Separate stable wire contracts, broker-specific adapters and application-owned consumers when this is justified by multiple deployments or teams. Contract assemblies cannot depend on MassTransit, Wolverine or private storage. Transport adapters depend on contracts. The composition root wires the concrete adapter; consumer ownership and queue/topic configuration stay explicit. No fixed project count or historic names are required.

## Verification

Record actual versions, settings and executed test evidence; the template does not decide them for the project.
