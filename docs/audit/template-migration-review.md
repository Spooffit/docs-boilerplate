# Documentation template migration: first extraction

- Date: 2026-10-27
- Reviewed: the source business feature/module/question templates, shared ADR/standard templates, .NET backend feature template and Blazor frontend feature template.
- State: targeted review, not a complete cleanup of all old templates. Legacy content remains preserved.

## Decisions

- The old **backend feature** template documents aggregates, application abstractions, EF Core, CQRS and RabbitMQ with strong assumptions about the source solution. The new `docs/templates/dotnet-backend-feature.md` retains detailed model ownership, field-level contracts, data flows, failure modes and test strategy while making the optional layers conditional.
- The old **frontend feature** template contains useful state ownership, component boundaries and interaction flows, but presumes Standalone Blazor, MudBlazor, NSwag and FSD. The new `docs/templates/blazor-frontend-feature.md` preserves useful information without dictating component architecture or UI tooling.
- The original **business feature** was concise but repetitive. The revised generic template defines observable results, scope, alternates, acceptance evidence and open decisions.
- The original **module** and **question** templates correctly separated cross-feature outcomes and unresolved decisions. They are now available as permanent reusable blank templates without a requirement to create speculative module/question records.
- The **ADR** template uses a Proposed default and explicit drivers, tradeoffs and validation rather than granting an unstated acceptance.
- The **standard** template requires applicability and verifiable rules; source code and external technical facts belong to appropriate sources, not unfounded examples.

## Output boundaries

The generator copies only these reusable blank templates. They are **not automatically turned into project-specific approved requirements**. Their bracketed editorial instructions are intended for authors; the generator's unresolved initialization placeholders are a separate concept. Files selected by technology:
- Generic feature, ADR, module, question and standard templates: base.
- .NET backend feature template: selected `dotnet` (or a requiring profile).
- Blazor UI feature template: selected `blazor` (or a requiring profile).

## Remaining

Audit remaining templates and decide whether further project-specific examples deserve migration. Perform rendered Markdown reviews and any compile checks if future templates include actual source examples.
