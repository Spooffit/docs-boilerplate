---
title: "Deferred decisions for template finalization"
description: "Owner choices that remain open after boilerplate engineering work"
kind: guide
status: draft
---

# Deferred template decisions

These are **questions for the repository owner**, not approved engineering requirements or instructions for an AI agent to select a default. Address them together after the remaining generator, template and verification work.

| Topic | Current neutral approach | Options for later owner approval |
| --- | --- | --- |
| Planned profile visibility | Five technology profiles are explicitly marked `planned`, have no standards, and cannot be selected | Keep planned entries discoverable; hide them until implemented; remove unused planned entries |
| Permanent template set | Feature, technical feature, module, question, ADR, standard and milestone templates exist without requiring each one | Smaller core; keep the current templates; specialized optional templates |
| File identifiers and workflow status | Descriptive names and explicit decision status are encouraged without mandatory numbered global IDs | Opt-in F/M/Q-like IDs; purely descriptive names; project-defined scheme |
| Example policy | Active catalog contains generic editorial placeholders, not real business specifications | No product examples; a small isolated opt-in example; separate demonstrator repository with tests |
| Code-example verification | Only evidence-backed, version-compatible examples should be called executable | Require CI-compiling samples; allow explicitly labeled pseudocode; maintain a separate library of verified examples |
| Technology selection | .NET/Blazor/PostgreSQL and related standards are opt-in and tool-neutral where practical | Keep neutral profiles; provide a separately selected opinionated stack with executable verification |
| Testing method names | Behavior-focused descriptions without a global enforced shape | Optional `MethodUnderTest_Scenario_ExpectedBehavior`; a different configured convention; no enforced form |
| Test UI tooling | UI/component/browser checks chosen by each project's stack | Opt-in Blazor+bUnit+FSD testing variant; lighter bUnit-only variant; no extra variant |
| .NET validation | Validation rules are separate from chosen libraries | Optional FluentValidation default within a prescriptive profile; choose per project |
| Persistence/query patterns | EF Core, Dapper, Ardalis.Specification and aggregate-constraints guidance remain separate selections | Offer an explicit opt-in DDD/persistence profile with tested samples; leave current separate guidance |
| MVC and CQRS API style | ASP.NET Core/MVC/CQRS are composable independent selections | Offer explicit MVC+CQRS conventions with real bus/result mapper samples; no specialized profile |
| Optimistic HTTP concurrency | Generic HTTP/client guidance requires selection of actual wire semantics | Optional strong ETag/If-Match reference profile; leave contracts fully project-defined |
| SDK platform adapter policy | Shared protocol behavior may use optional transport/storage/resource adapters; platform-specific threading and runtime constraints are verified per selected target | Keep only technology-neutral guidance; offer an opt-in SDK adapter template with explicit runtime acceptance checks; choose platform-specific requirements per project |
| Typed-value semantics and draft validation | Standards now separate missing/null/default values, exact numeric and temporal semantics, ordered/unique collections, and conditional draft-versus-publish validation | Keep these as optional contract review checks; offer a dedicated schema-driven content modeling template only when a project needs it; make a specific validation lifecycle the default |
| Code generation verification examples | Generated models need contract fidelity and runtime proof, but the template does not ship an executable compiler/codegen example | Keep verification prompts; offer separately reviewed optional examples for chosen schema/codegen technologies; no default code generator |
| Generated API client state | Client generation and UI view/form state boundaries are defined by consuming application | Opinionated direct-generated-DTO form model; adapter/view-model approach; no default |
| Messaging topology | MassTransit/RabbitMQ guidance avoids hardcoding one project topology | Optional concrete contract/package/topology conventions after testing; stay tool/transport-neutral by default |
| Data-flow detail | Transport and internal flows are distinguished when applicable | Optional detailed DF-ID template; lightweight traceability only |
| Milestone examples | Stages are project-defined | Example Demo/MVP/Production plan as opt-in guidance; no prescribed stages |
| Persistent activity-history documentation | Security and observability scaffolds include conditional prompts for durable history without requiring an audit subsystem | Keep prompts within existing modules; add a dedicated opt-in activity-history writing template; omit history-specific guidance unless explicitly selected |
| Review/approval workflow examples | Generic stateful workflow checklist describes review only when applicable | Keep the checklist; add an opt-in review decision template; offer an independently verified example |

## Approval requirements

For each choice record the selected default versus opt-in scope, reason, affected manifest/template paths, implementation and test evidence, and any migration impact on previously generated docs. A historical pattern alone never grants authority to change generated defaults.

**All options remain pending.** The active repository is for reusable writing guidance, not for any individual application's requirements. Examples must be generic or explicitly opt-in and verified.
