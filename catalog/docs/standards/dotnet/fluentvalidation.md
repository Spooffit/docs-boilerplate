---
title: "FluentValidation conventions"
description: "Technology-scoped engineering standard"
kind: standard
status: active
---

# FluentValidation conventions

**Applies to:** .NET applications that select FluentValidation. It is **not** mandatory for every .NET project.

## Rule ownership and composition

- Validate input semantics with `AbstractValidator<T>` and reuse genuinely identical rules instead of duplicating constraints.
- Distinguish optional and required properties; `SetValidator` on a nullable child does not by itself make the child mandatory.
- Use `RuleForEach` for collection elements and separately validate the collection as a whole when required.
- `Include` composes validators for the same model type; `SetValidator` composes nested model validators. Rule builder extension methods are appropriate for truly reusable field semantics.
- Do not infer identical rules from identical CLR types: two strings may have different domain meaning.
- Validation must not mutate its input, write to databases, publish events, or bypass authorization.

## Async rules and ASP.NET Core

If any rule is asynchronous (`MustAsync`, `CustomAsync`, `WhenAsync`), run `ValidateAsync` with a cancellation token. Running synchronous `Validate` on an async validator is unsupported and may throw.

**Do not adopt the legacy `FluentValidation.AspNetCore` MVC auto-validation pipeline for new projects.** Its synchronous ASP.NET validation pipeline is incompatible with asynchronous rules and the integration is no longer recommended for new projects. Use explicit async invocation in the application/request pipeline or an evaluated async-capable filter adapter if automatic behavior is required.

Do not duplicate all checks in MVC attributes and FluentValidation. Binding errors, transport validation, business authorization, domain invariants and database constraints have different responsibilities.

## Source of constraints

If an application uses a domain constraints container, share stable definitions intentionally, but do not require a specific `<Aggregate>Constraints` class for all projects. Database migrations must capture historical values appropriately; a change to a constant does not rewrite applied migrations.

## Verification

Test invalid, boundary, optional and nested input cases; ensure errors identify the relevant field and that validation is actually executed in the chosen pipeline. Test concurrency/uniqueness behavior at the database boundary rather than assuming input-validation success guarantees it.

References: [FluentValidation ASP.NET Core integration](https://docs.fluentvalidation.net/en/latest/aspnet.html), [asynchronous validation](https://docs.fluentvalidation.net/en/latest/async.html), [collections](https://docs.fluentvalidation.net/en/latest/collections.html).
