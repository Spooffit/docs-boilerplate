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

## Value-type semantics and normalization

Compose reusable validators for genuinely shared *meaning*, not merely shared CLR field types. When a value has restrictions beyond length, validate its allowed characters, format and explicitly adopted normalization rule. Nested model validators and outer requiredness checks solve different problems; ensure both are tested when the child must exist.

## Atomic field rules and nested composition

- Reuse the smallest **meaningful** rule or indivisible cross-field invariant. This does not justify a wrapper around every built-in `NotNull`, `MaximumLength` or `Must` call. Creation, patch and update operations can share the value rule while owning different presence requirements.
- For recurring property semantics, use suitably typed `IRuleBuilder<T, TProperty>` extension methods. For a nested object, use `RuleFor(...).SetValidator(...)`; use `RuleForEach(...).SetValidator(...)` for collection elements. `Include(...)` is for validators of the **same root model type**, not arbitrary child types.
- A `null` child skips its child validator; the parent must explicitly require the child if needed. Enforce collection-level limits separately from per-item rules. Preserve field/property paths, including collection indexes, in surfaced validation failures.
- Keep field-level rules free of hidden network/database lookups. If a use case requires a fresh external fact, put it behind a consciously selected application check or a separately designed asynchronous boundary. Avoid repeating the same remote lookup in each child validator.
- Optional and omitted values need different semantics for patch-style operations; rule composition must not silently turn "not provided" into "invalid domain state."

References: [including rules](https://docs.fluentvalidation.net/en/latest/including-rules.html), [custom validators](https://docs.fluentvalidation.net/en/latest/custom-validators.html), [complex properties](https://docs.fluentvalidation.net/en/latest/start.html#complex-properties), [collections](https://docs.fluentvalidation.net/en/latest/collections.html).

## Typed constraints and validation stages

When a selected contract models an optional typed field, an omitted value and a present zero, `false` or empty collection must not automatically be treated alike. Use explicit presence/null rules and validate a supplied value's own constraints. If a project distinguishes draft acceptance from publication readiness, select and test separate validation rules or contexts for those operations; do not hide the distinction behind one validator that arbitrarily skips failures.

For numeric steps and bounds, preserve the adopted numeric domain (including decimal precision) without an accidental `decimal` to `double` conversion. For `List<T>`, collection size, item validity, uniqueness and ordering may require different checks, with nested property/index paths in diagnostics. Any comparer or normalizer must follow the contract rather than the machine's current culture. Do not silently sort, fill defaults or rewrite inputs from an input validator.

## Async rules and ASP.NET Core

If any rule is asynchronous (`MustAsync`, `CustomAsync`, `WhenAsync`), run `ValidateAsync` with a cancellation token. Running synchronous `Validate` on an async validator is unsupported and may throw.

**Do not adopt the legacy `FluentValidation.AspNetCore` MVC auto-validation pipeline for new projects.** Its synchronous ASP.NET validation pipeline is incompatible with asynchronous rules and the integration is no longer recommended for new projects. Use explicit async invocation in the application/request pipeline or an evaluated async-capable filter adapter if automatic behavior is required.

Do not duplicate all checks in MVC attributes and FluentValidation. Binding errors, transport validation, business authorization, domain invariants and database constraints have different responsibilities.

## Execution and test evidence

- Ensure the selected endpoint, handler or filter **actually invokes** the root validator before the action it protects. Correct standalone validator tests do not establish application-pipeline integration.
- Keep normalization/transformation outside the validator, consistently defined in the public model contract. A failed validation should not mutate inputs or have external side effects.
- Test reusable rules alone and in the real parent composition, including null/missing inputs, zero/empty collections, conditional branches, boundary values and correct property/index error paths.
- Exercise asynchronous rules with cancellation and a real `ValidateAsync` invocation where chosen. Provider uniqueness, concurrent state changes and permissions require their own integration/application evidence after input checks pass.

## Source of constraints

If an application uses a domain constraints container, share stable definitions intentionally, but do not require a specific `<Aggregate>Constraints` class for all projects. Database migrations must capture historical values appropriately; a change to a constant does not rewrite applied migrations.

## Domain-defined limits and safe regular expressions

When domain constraints are shared with request validators, apply the same **confirmed** limits and normalization semantics while keeping domain enforcement independent of FluentValidation. Separate a property's optional presence from validation of a non-null supplied value; `Length` alone does not make a string mandatory. Never use a `When` predicate that skips invalid supplied whitespace unless the public contract intentionally normalizes whitespace to absence.

For regex validation, apply the project's specified anchoring and character alphabet (such as ASCII digits versus Unicode categories); bound input lengths and regex execution time where relevant. Neither a generic string rule nor a regex wrapper should silently mutate source data. Verify the entire model's actual validator composition and the domain factory's invariant separately.

## Verification

Test invalid, boundary, optional and nested input cases; ensure errors identify the relevant field and that validation is actually executed in the chosen pipeline. Test concurrency/uniqueness behavior at the database boundary rather than assuming input-validation success guarantees it.

References: [FluentValidation ASP.NET Core integration](https://docs.fluentvalidation.net/en/latest/aspnet.html), [asynchronous validation](https://docs.fluentvalidation.net/en/latest/async.html), [collections](https://docs.fluentvalidation.net/en/latest/collections.html).
