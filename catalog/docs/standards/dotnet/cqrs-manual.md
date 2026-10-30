---
title: "Manual CQRS handlers"
description: "Optional technology-specific engineering standard"
kind: standard
status: active
---

# Manual CQRS handlers

**Scope:** Select this option explicitly in the wizard; a catalog entry alone does not prove an implementation exists.

## Guidance

Implement command and query handlers directly through application services or Minimal API endpoints without a mediator dependency. Separate write intent from read-only queries, enforce authorization and domain invariants, and avoid oversized endpoints. Use clear cancellation and error results; add pipeline abstractions only for demonstrated cross-cutting needs.

## Verification

Record actual versions, authoritative sources and executed tests. Do not import historical product-specific entities or workflows into reusable guidance.
