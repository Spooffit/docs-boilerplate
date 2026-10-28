---
title: "ADR 0006: Independent technology profiles with three opt-in conventions"
description: "Owner decisions for testing names, ETag concurrency and MassTransit EventBus composition"
kind: adr
status: active
---

# ADR 0006: Independent technology profiles with three opt-in conventions

- Status: Accepted
- Date: 2026-10-28
- Authority: Explicit repository-owner approval of technology decision block two

## Decisions and applicability

| Decision | Approved implementation |
| --- | --- |
| Technology selection: A | Keep independently composable profiles; no mandatory prebuilt stack. |
| Test method naming: B | Add selectable `test-naming-convention` for `MethodUnderTest_Scenario_ExpectedBehavior`, dependent only on `dotnet`. |
| Blazor UI tests: A | Keep `bunit` separate; browser tests depend on the actual application needs. |
| .NET validation: A | `fluentvalidation` stays independently selectable. |
| Persistence and DDD: A | `ef-core`, `dapper`, `ardalis-specification` and `ddd` remain separately selectable. |
| ASP.NET Core MVC + CQRS: A | Keep `aspnet-mvc` and `cqrs` independent; no universal command/query bus or response mapper. |
| HTTP concurrency: B | Add selectable `http-etag-concurrency`, dependent on `http-contracts`; document strong `ETag` / `If-Match` wire examples and verified scenarios. |
| MassTransit/RabbitMQ topology: B | Add selectable `masstransit-eventbus-composition`, dependent on `masstransit-rabbitmq`; provide Contracts/RabbitMq/Default integration project layout, example topology, compilation and broker-routing verification. |

## Boundaries

These conventions are **opt-in** and do not expand the default Markdown skeleton, require extra project documents, or install libraries into generated documentation repositories. A base profile does **not** select a more specialized variant; selected variants inherit only their declared dependencies. None of the test or EventBus examples declares project-specific entities, routes, system features or releases.

Live RabbitMQ routing tests run only in the boilerplate CI test harness, not in generated repositories. The reference examples do not replace verification against a consuming project’s actual topology or HTTP persistence.

## Evidence and migration

- The catalog is validated for conflicts and profile dependencies, and Linux CLI smoke generates and validates each available profile individually.
- `tests/optional_profile_contracts.py` checks the RFC-based HTTP scenarios and compiles/runs the extracted EventBus example against a disposable broker in CI. Source of the sample is the optional standard itself, so it cannot silently drift from a separate unverified demo app.
- Existing generated repositories retain their selected profiles and require no migration. A project opts into a new convention only through an explicitly regenerated selection or a reviewed manual adoption; `update` does not silently change selections.
- At the time of this decision, five further owner questions remained. They were subsequently resolved in [ADR 0007](0007-final-editorial-policies.md), and the [decision review](owner-decision-backlog.md) is now closed.
