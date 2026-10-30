---
title: "CQRS with Requestum"
description: "Optional technology-specific engineering standard"
kind: standard
status: active
---

# CQRS with Requestum

**Scope:** Select this option explicitly in the wizard; a catalog entry alone does not prove an implementation exists.

## Guidance

Use Requestum's explicitly differentiated command, query and event contracts, handlers and middleware only after verifying current .NET target support. Do not conflate its in-process event abstraction with reliable external bus publication. Verify error propagation, DI registration, cancellation and timeout/retry policies in real tests.

## Verification

Record actual versions, authoritative sources and executed tests. Do not import historical product-specific entities or workflows into reusable guidance.
