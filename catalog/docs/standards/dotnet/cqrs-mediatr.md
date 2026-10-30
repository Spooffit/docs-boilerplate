---
title: "CQRS with MediatR"
description: "Optional technology-specific engineering standard"
kind: standard
status: active
---

# CQRS with MediatR

**Scope:** Select this option explicitly in the wizard; a catalog entry alone does not prove an implementation exists.

## Guidance

Use MediatR request/handler contracts for in-process command and query dispatch, not as proof of an external message bus. Define pipeline behaviors only for genuine cross-cutting concerns. Check the applicable MediatR license and packaging terms of the selected version before adopting. Test registration, one-handler semantics, cancellation and application behavior.

## Verification

Record actual versions, authoritative sources and executed tests. Do not import historical product-specific entities or workflows into reusable guidance.
