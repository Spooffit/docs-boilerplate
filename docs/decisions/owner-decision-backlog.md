---
title: "Remaining decisions for template finalization"
description: "Unresolved owner choices after approval of the initial template scope"
kind: guide
status: draft
---

# Remaining template decisions

The first five scope choices were **approved on 2026-10-28** and are recorded in [ADR 0005](0005-approved-template-scope.md). The remaining **13 questions** below are **not approved requirements** and must not be selected by an AI agent. Revisit them in groups, preserving the neutral approach until the repository owner decides otherwise.

| Topic | Current neutral approach | Options for later owner approval |
| --- | --- | --- |
| File identifiers and workflow status | Descriptive names and explicit decision status are encouraged without mandatory numbered global IDs | Opt-in F/M/Q-like IDs; purely descriptive names; project-defined scheme |
| Code-example verification | Only evidence-backed, version-compatible examples should be called executable | Require CI-compiling samples; allow explicitly labeled pseudocode; maintain a separate library of verified examples |
| Technology selection | .NET/Blazor/PostgreSQL and related standards are opt-in and tool-neutral where practical | Keep neutral profiles; provide a separately selected opinionated stack with executable verification |
| Testing method names | Behavior-focused descriptions without a global enforced shape | Optional `MethodUnderTest_Scenario_ExpectedBehavior`; a different configured convention; no enforced form |
| Test UI tooling | UI/component/browser checks chosen by each project's stack | Opt-in Blazor+bUnit+FSD testing variant; lighter bUnit-only variant; no extra variant |
| .NET validation | Validation rules are separate from chosen libraries | Optional FluentValidation default within a prescriptive profile; choose per project |
| Persistence/query patterns | EF Core, Dapper, Ardalis.Specification and aggregate-constraints guidance remain separate selections | Offer an explicit opt-in DDD/persistence profile with tested samples; leave current separate guidance |
| MVC and CQRS API style | ASP.NET Core/MVC/CQRS are composable independent selections | Offer explicit MVC+CQRS conventions with real bus/result mapper samples; no specialized profile |
| Optimistic HTTP concurrency | Generic HTTP/client guidance requires selection of actual wire semantics | Optional strong ETag/If-Match reference profile; leave contracts fully project-defined |
| SDK platform adapter policy | Shared protocol behavior may use optional transport/storage/resource adapters; platform-specific threading and runtime constraints are verified per selected target | Keep only technology-neutral guidance; offer an opt-in SDK adapter template with explicit runtime acceptance checks; choose platform-specific requirements per project |
| Code generation verification examples | Generated models need contract fidelity and runtime proof, but the template does not ship an executable compiler/codegen example | Keep verification prompts; offer separately reviewed optional examples for chosen schema/codegen technologies; no default code generator |
| Generated API client state | Client generation and UI view/form state boundaries are defined by consuming application | Opinionated direct-generated-DTO form model; adapter/view-model approach; no default |
| Messaging topology | MassTransit/RabbitMQ guidance avoids hardcoding one project topology | Optional concrete contract/package/topology conventions after testing; stay tool/transport-neutral by default |

## Review sequence after the technical audit

Review existing questions in three passes, without treating the order as priority or consent:

1. **Remaining scope choices:** file identifiers and workflow statuses, and whether separately selected opinionated technology stacks are desirable. The approved default documents, template inventory, planned profile visibility, absence of a demo product and lightweight data flows/milestones are not up for implicit reinterpretation; see [ADR 0005](0005-approved-template-scope.md).
2. **Optional technology conventions:** testing method names, test UI tooling, .NET validation, persistence/query patterns, MVC/CQRS API style, optimistic HTTP concurrency and messaging topology. No specialized stack should be generated unless it is explicitly selected and verifiable.
3. **Optional integration and example policies:** code-example verification, SDK platform adapter policy, code generation verification examples and generated API client state. These are *candidates for review*, not evidence that the boilerplate needs an SDK, business schema, client cache or source generator.

The existing table is the decision register; the passes above are only a review route, not additional requirements. Closing a proposal as **not applicable** requires no replacement template. Keep all **remaining** entries pending until an explicit owner decision is recorded.

## Approval requirements

For each choice record the selected default versus opt-in scope, reason, affected manifest/template paths, implementation and test evidence, and any migration impact on previously generated docs. A historical pattern alone never grants authority to change generated defaults.

**All 13 choices in the table remain pending.** The five approved scope decisions are documented separately in [ADR 0005](0005-approved-template-scope.md). The active repository is for reusable writing guidance, not for any individual application's requirements. Only generic technical examples are within the approved scope; a demonstrator product is not approved.
