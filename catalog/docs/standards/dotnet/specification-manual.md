---
title: "Custom query specifications on EF Core"
description: "Optional technology-specific engineering standard"
kind: standard
status: active
---

# Custom query specifications on EF Core

**Scope:** Only when this specific technology/profile is selected. This is reusable guidance, not a product requirement or proof of implementation.

## Design and ownership

Model reusable query intent as immutable predicates/projections and compose it through EF Core `IQueryable` before materialization. Separate filters, sorting, paging and authorization. Do not create a generic repository or arbitrary expression serializer by default. Verify SQL translation, stable ordering, cancellation and provider behavior in integration tests.

## Evidence

Record actual installed versions and authoritative contracts. Keep unsettled decisions open. Validate with a real integration test where external behavior matters; do not claim generated documentation proves the application works.
