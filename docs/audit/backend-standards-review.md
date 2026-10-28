---
title: "Technical standards migration: .NET, HTTP APIs and persistence"
description: "Historic and ongoing source analysis"
kind: audit
status: active
---

# Technical standards migration: .NET, HTTP APIs and persistence

- Date: 2026-10-27
- Scope: focused content review of S01, S02, S03, S16, S18, S20, S23 and S24. Reviewed repository text and checked critical framework behavior against official documentation. This is **not** a full review of every feature or legacy standard.
- Destination: reusable base and technology-scoped documentation profiles in `catalog/`. Source documents were initially preserved; file-specific migrations are recorded below.

## Findings by original file

| Original | Decision | Destination / qualification |
| --- | --- | --- |
| `S01-dotnet-conventions.md` | Generalize | `dotnet/conventions.md`; layering rules are conditional on architecture |
| `S02-persistence.md` | Decompose | `dotnet/ef-core.md` and `postgresql/persistence.md`; Ardalis.Specification and the aggregate-constraints naming convention are not unconditional |
| `S03-validation-and-errors.md` | Decompose | `aspnet/errors-and-validation.md`; FluentValidation moves to an optional profile |
| `S16-rest.md` | Decompose | `aspnet/http-api.md` and `aspnet/mvc-controllers.md`; CQRS buses, result mapping and versioned routing remain opt-in application decisions |
| `S18-fluent-validation.md` | Retain selectively | `dotnet/fluentvalidation.md`, selectable separately; avoid obsolete MVC auto-validation assumptions |
| `S20-cqrs.md` | Generalize | `architecture/cqrs.md`; handler directory placement cannot be universally mandated |
| `S23-csproj.md` | Generalize | `dotnet/project-files.md`; keep deterministic XML/project conventions without requiring every project to be packaged |
| `S24-code-formatting.md` | Replace | Formatting should follow actual tool capabilities; source heading incorrectly says `S23` and cannot be mechanically reused |

## Key corrections

1. **Framework neutrality.** ASP.NET Core does not require MVC, CQRS buses, a particular response mapper, `FluentValidation`, or API URL versioning.
2. **Validation semantics.** The old standard requires FluentValidation for every .NET request. Current FluentValidation documentation states its older MVC auto-validation pipeline is not recommended for new projects and is incompatible with async rules. If used, explicit `ValidateAsync` is supported.
3. **Concurrency.** The old persistence rule assumes `<AggregateName>Constraints` and Ardalis.Specification. Those may be useful when selected, but they are not universal. EF Core optimistic concurrency tokens require a response strategy and unique constraint violations are not always concurrency exceptions.
4. **PostgreSQL.** Isolation and migration semantics belong in a database-specific profile, independently of the .NET provider selected.
5. **CQRS.** The old instruction that every query handler must live inside `EntityFramework.Persistence` is a particular deployment and layering decision, not a definition of CQRS.
6. **Build metadata.** The old csproj convention includes fixed property ordering and `PackageId` even for many internal projects; use only when the role warrants it.
7. **Authority.** Project-specific routes, broker names, permission policies and module identities must not enter general-purpose standards.

## Verified source documentation

- [ASP.NET Core API errors](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0)
- [ASP.NET Core OpenAPI generation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0)
- [FluentValidation ASP.NET integration](https://docs.fluentvalidation.net/en/latest/aspnet.html)
- [FluentValidation asynchronous rules](https://docs.fluentvalidation.net/en/latest/async.html)
- [EF Core concurrency](https://learn.microsoft.com/en-us/ef/core/saving/concurrency)
- [EF Core async behavior](https://learn.microsoft.com/en-us/ef/core/miscellaneous/async)
- [PostgreSQL isolation](https://www.postgresql.org/docs/current/transaction-iso.html)

## Validation expectations

The smoke suite should generate combinations with MVC, its prerequisites, PostgreSQL, EF Core and FluentValidation, validate the repository and build an opt-in site. It must also generate plain ASP.NET Core without MVC, and PostgreSQL without a .NET runtime profile.

## Remaining migration work

- Review the remaining S04/S16/S17/S21/S22 and frontend S05-S08 standards for technical correctness, while preserving already extracted rules.
- Evaluate whether optional Ardalis.Specification, MassTransit and API Versioning deserve distinct profiles after checking their current APIs.
- Add compiling examples with dedicated build tests before describing them as tested examples.
- Preserve all legacy source content until the migration decisions are complete.

## S18/S19/S20 full-content comparison (2026-10-28)

The three source documents were inspected in full. S18's meaningful field-level rule reuse, typed validator composition, null/nested/collection behavior and async execution are now explicit in the **optional** `dotnet/fluentvalidation.md` profile. S19's lack of built-in composite full specifications, supported builder extensions, explicit query shape and real-provider verification are now explicit in the **optional** `dotnet/ardalis-specification.md` profile. Both remain technology-specific: no project inherits those libraries without selecting them. S20 is only two lines prescribing command handler placement in Application and query handler placement in `EntityFramework.Persistence`; the reusable CQRS guidance instead explains placement choices and dependency direction.

All three original prescriptions of mandatory libraries or fixed layering are recorded as **historical choices**, not adopted universal policy. The owner backlog already carries optional-versus-mandatory technology and architecture decisions; do not add duplicate approval questions. Original source retention/removal and incoming links are tracked independently of this content-level review. The refreshed guidance is grounded in the linked official FluentValidation, Ardalis.Specification and Microsoft documentation; runnable package examples are not claimed as verified.

## Complete S02/S03 source comparison (2026-10-28)

The complete S02 and S03 files were read, not just their headings. The reusable parts are now traceable:

| Source claim | Canonical location | Disposition |
| --- | --- | --- |
| S02 immutable snapshots, pointer/history atomicity, retained dependencies | `catalog/base/docs/standards/stateful-workflows.md#durable-snapshots-and-retained-history` | Generalized to conditional stateful systems |
| S02 DB constraints, concurrency and historical migrations | `catalog/docs/standards/postgresql/persistence.md` and `catalog/docs/standards/dotnet/ef-core.md` | Technology-scoped; storage product and ORM are independent |
| S02 DB+message consistency, projections and replay | `catalog/docs/standards/integrations/reliable-synchronization.md` | Only for systems selecting the integration-reliability profile |
| S02 mandatory Ardalis.Specification and `<AggregateName>Constraints` | Existing opt-in Ardalis profile, S21 historical source and owner-decision backlog | **Not** imposed on all projects |
| S03 format/domain/application trust boundaries, failures, partial results and uncertain write outcomes | `catalog/base/docs/standards/data-flows.md#input-trust-and-failure-semantics` | Portable flow contract checklist |
| S03 HTTP-facing errors, normalization and response mapping | `catalog/docs/standards/aspnet/errors-and-validation.md` | Only for selected ASP.NET Core projects |
| S03 mandatory FluentValidation and validator composition | `catalog/docs/standards/dotnet/fluentvalidation.md`, plus owner-decision backlog | Optional profile pending owner's policy choice |

The predecessor's application test execution, DB migrations and production contracts have **not** been verified by this documentation migration. S02 and S03 may be physically retired only after all incoming Markdown links are updated, the original-policy exceptions are recorded and the deletion guard and CI pass. Mandatory historical library choices are recorded for later owner consultation rather than silently approved or forced on the universal catalog.

## Completed S02/S03 retirement (2026-10-28)

Following a whole-file review and reusable-rule extraction, the four inbound links to each retired source were updated. S02 mapped to the base stateful-workflows review and selected EF Core conventions; S03 to base data-flow trust/failure rules and opt-in ASP.NET Core error semantics. Project-specific mandatory Ardalis, constraint-class naming and FluentValidation decisions are **not** presumed for every new project. The full source-to-target trace is in the preceding comparison section. The original documents remain available through Git history, and successful CI remains the deletion gate.

## S16/S17 full-content review and extraction (2026-10-28)

S16 (the original 791-line MVC/CQRS-oriented API profile) was read in full. Its reusable HTTP protocol requirements, route-constraint versus `400` behavior, conditional request semantics, response status fidelity, authorization/model-binding boundaries, selected app abstraction reuse and OpenAPI/middleware verification are now in `catalog/docs/standards/aspnet/http-api.md`, `mvc-controllers.md` and `errors-and-validation.md`. HTTP semantics were cross-checked against ASP.NET Core routing documentation and RFC 9110. S16's fixed buses, response mapper/envelope, `api/v{version:apiVersion}` route, 200-for-mutations policy and `int Version` ETag design are **not** universal defaults. The large illustrative controllers and owner-specific choices remain preserved in the old source until a decision is made about optional executable examples.

S17 (the original 89-line data-flow standard) was read in full. Its transport-versus-internal flow distinction, optional local flow IDs, rich input/output/authorization inventory, actual state/commit boundaries, retry and recovery semantics, explicit unknowns, and SUT-based verification matrix are now in `catalog/base/docs/standards/data-flows.md`. A fixed DF01/table shape is not required of every project. S17 is eligible for retirement **only once all its inbound Markdown references have been reconciled and the deletion check passes**.

Neither text migration certifies the original application's implementation, tests, published APIs or external links as operational evidence. The old project source remains a separate historical artifact from generated documentation.


## Completed S18/S19/S20 retirement (2026-10-28)

S18 and S19 were compared in full with their respective **optional** FluentValidation and Ardalis.Specification profiles; the specialized composition, execution, provider fidelity and validation requirements have been retained without a mandatory dependency for unselected projects. The entire two-line S20 source was reconciled into optional CQRS handler-placement guidance, with concrete architectural layering left to a real project decision. The deletion decision for each and evidence of zero recognized incoming links are recorded in [the historical removal ledger](legacy-removals.md). Updated documentation is not executable verification of the old project's tests or third-party library examples.
