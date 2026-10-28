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

## Aggregate-owned rule definitions

If multiple technical layers need the **same stable domain limit**, identify an authoritative domain-level definition or pure predicate rather than copy unrelated literals across validator, factory and ORM configurations. Share only genuinely identical *semantics*: two strings with equal maximum lengths do not become one domain concept. Ownership of an aggregate's rules stops at its boundary; a reference to another aggregate is not permission to depend on its implementation.

An optional C# convention uses a `<AggregateName>Constraints` class next to an aggregate, with nested types corresponding to owned value objects and properties. This is a project decision, **not** mandatory for DDD and not required for aggregates without such fixed limits. Keep these definitions static/pure: no DI, database calls, transport library or mutable public collection. Use `const` only for compile-time values; public constants are inlined into referencing assemblies and updates can require dependent packages to be rebuilt.

Distinguish stable field rules (allowed characters, min/max length or count, numeric bounds, normalization policy) from state- or operation-dependent rules (permission, uniqueness, allowed transition or external fact). The latter need application/domain logic and durable concurrency protection, not an unconditional `Required` constant.

### Missing, present and normalized values

Define the representation of omitted values, `null`, empty strings and whitespace separately. An optional value must still be validated if it **is** supplied. A patch request may omit a field even when the complete domain aggregate requires it. Decide whether to normalize before validation; never silently truncate to an allowed maximum or make one layer validate raw text while another validates a transformed representation.

In .NET, `string.Length` counts UTF-16 code units, not Unicode scalar values or grapheme clusters; specify the unit when it affects a business contract. A relational provider's character limit is not automatically equivalent to every application-level length unit. For regular expressions, decide culture/character-class semantics, matching of the whole value, input-size limits and execution timeouts; a .NET regex is not automatically a portable database `CHECK` expression.

## Stable domain constraints

Keep a single authoritative definition for each *confirmed* domain rule. Reuse the rule consistently in the domain, application boundary, persistence mapping and API validation **where the project's dependency structure allows it**. Validation at one boundary does not replace the guarantees of another.

Some .NET projects organize constants in a `<AggregateName>Constraints` class. This is an optional **project convention**, not a DDD requirement. Avoid duplicating fixed limits across layers, but do not introduce one public static type for every aggregate merely to satisfy a template.

Distinguish domain-required state from optional fields on individual API operations. Preserve historical values within applied database migrations, and do not assume a current constant automatically updates an existing schema.
