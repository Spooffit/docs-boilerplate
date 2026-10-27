---
title: "Domain-Driven Design"
description: "Applicable engineering standard"
kind: standard
status: active
---

# Domain-Driven Design

**Applies to:** projects deliberately using DDD modeling techniques.

- Model terms with domain experts and maintain a contextual glossary.
- Define bounded contexts from model and language boundaries, not arbitrary solution folders.
- Use aggregates and domain events where they enforce real business invariants or integration needs.
- DDD does not require microservices, event sourcing, CQRS, or a fixed layer count.

## Stable domain constraints

Keep a single authoritative definition for each *confirmed* domain rule. Reuse the rule consistently in the domain, application boundary, persistence mapping and API validation **where the project's dependency structure allows it**. Validation at one boundary does not replace the guarantees of another.

Some .NET projects organize constants in a `<AggregateName>Constraints` class. This is an optional **project convention**, not a DDD requirement. Avoid duplicating fixed limits across layers, but do not introduce one public static type for every aggregate merely to satisfy a template.

Distinguish domain-required state from optional fields on individual API operations. Preserve historical values within applied database migrations, and do not assume a current constant automatically updates an existing schema.
