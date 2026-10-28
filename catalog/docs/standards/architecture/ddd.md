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

## Typed values and collection meaning (when applicable)

Requiredness, value validity and non-emptiness are **separate rules**. For an adopted type contract, a present `false`, numeric zero, zero duration or empty collection may be valid even when the property is required. Specify explicitly whether a missing member, JSON `null`, an empty string, a whitespace-only string or an empty collection are allowed. Do not silently replace absent values with a CLR default, synthetic identifier or the first enum choice.

For structured values, validate recursively against the **actual selected definition**. Collection-level size, element constraints, uniqueness and order are distinct. Define whether order is meaningful to equality, whether duplicate elements are compared by full semantic value or a chosen key, and whether identity used only by the editor participates in runtime equality. A hash may identify candidate duplicates, but a hash match alone is not necessarily sufficient to prove semantic equality. Never sort, deduplicate, clamp or round authored data as a hidden side effect of validation.

For numbers, specify integer range, floating-point finite-value policy, exact decimal precision/step behavior and overflow handling separately. Avoid comparing exact decimal values through an intermediate binary floating-point conversion. For text, define whether length is measured in bytes, UTF-16 code units, Unicode scalar values or grapheme clusters; sanitized rich-text length may require a distinct extraction rule from storage-size limits.

A calendar date, local wall-clock date/time, UTC instant, timestamp with offset and elapsed duration are **not interchangeable**. Record the accepted transport representation, precision, offset/time-zone interpretation, equality/comparison rule and any intentionally preserved authored offset. Converting a naive historical value to an instant requires an explicit source time zone or other approved migration rule; server or browser local time is not a safe default.

## Stable domain constraints

Keep a single authoritative definition for each *confirmed* domain rule. Reuse the rule consistently in the domain, application boundary, persistence mapping and API validation **where the project's dependency structure allows it**. Validation at one boundary does not replace the guarantees of another.

Some .NET projects organize constants in a `<AggregateName>Constraints` class. This is an optional **project convention**, not a DDD requirement. Avoid duplicating fixed limits across layers, but do not introduce one public static type for every aggregate merely to satisfy a template.

Distinguish domain-required state from optional fields on individual API operations. Preserve historical values within applied database migrations, and do not assume a current constant automatically updates an existing schema.
