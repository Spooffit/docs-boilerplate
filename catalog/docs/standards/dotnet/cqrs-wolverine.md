---
title: "CQRS using Wolverine handlers"
description: "Optional technology-specific engineering standard"
kind: standard
status: active
---

# CQRS using Wolverine handlers

**Scope:** Select this option explicitly in the wizard; a catalog entry alone does not prove an implementation exists.

## Guidance

Use Wolverine local handler invocation only when the project already selected Wolverine for messaging. Local commands and queries do not require a broker hop; define which messages are local and which cross transport boundaries. Review transactional middleware, handler discovery, cancellation, idempotency and failure semantics in the installed version.

## Verification

Record actual versions, authoritative sources and executed tests. Do not import historical product-specific entities or workflows into reusable guidance.
