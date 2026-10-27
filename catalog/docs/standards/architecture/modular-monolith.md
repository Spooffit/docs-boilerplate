---
title: "Modular monolith"
description: "Applicable engineering standard"
kind: standard
status: active
---

# Modular monolith

**Applies to:** projects that explicitly select a modular-monolith architecture.

- Modules share a deployment boundary but retain explicit ownership and interfaces.
- Avoid direct use of another module's private persistence or domain internals.
- Record legal dependency directions and module contracts in project architecture documentation.
- Do not confuse bounded contexts, modules, and independently deployed services.

## Public boundaries and dependency direction

Write down each module's public contracts and the intended compile-time dependency direction. A module's application/domain core should not need another module's private persistence model. Where collaboration is synchronous, a consumer-owned interface and an integration adapter can preserve direction without adding unnecessary network hops.

The deployable host may reference concrete adapters to register dependency injection; this does not justify broad adapter dependencies inside core modules. Do not impose a fixed number of projects, assemblies or layers merely to imitate another solution's architecture.

## Data ownership and consistency

A shared physical database is compatible with a modular monolith, but it does not mean every module owns every table. Define write ownership and the permitted public read/use-case interfaces. Separate schemas, DbContexts and migration streams can help, but they are implementation choices, not axioms.

Transaction boundaries should follow business invariants and actual data ownership. A local database transaction does not atomically cover external message delivery. Cross-module workflows need an explicit coordination/consistency decision; outbox, idempotency and compensation are tools for particular failure models, not unconditional mandates.

## Concurrency and verification

Record which state transitions need optimistic concurrency, locks or serialized access only after identifying the race and its observable impact. API preconditions such as ETags are separate from internal version tokens and need their own HTTP contract.

Add architecture tests for **adopted**, verifiable module boundaries: allowed project references, no private cross-module DbContext access, host-only composition dependencies where chosen, and public contract ownership. Avoid hardcoding module names or technologies from a predecessor project into a general rule.
