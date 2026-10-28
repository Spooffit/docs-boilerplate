---
title: "Remaining decisions for template finalization"
description: "Unresolved owner choices after approval of the initial template scope"
kind: guide
status: draft
---

# Remaining template decisions

The initial scope choices are recorded in [ADR 0005](0005-approved-template-scope.md). Eight technology decisions were approved on 2026-10-28 in [ADR 0006](0006-optional-technology-conventions.md). The remaining **5 questions** below are **not approved requirements** and must not be selected by an AI agent. Revisit them in groups, preserving the neutral approach until the repository owner decides otherwise.

| Topic | Current neutral approach | Options for later owner approval |
| --- | --- | --- |
| File identifiers and workflow status | Descriptive names and explicit decision status are encouraged without mandatory numbered global IDs | Opt-in F/M/Q-like IDs; purely descriptive names; project-defined scheme |
| Code-example verification | Only evidence-backed, version-compatible examples should be called executable | Require CI-compiling samples; allow explicitly labeled pseudocode; maintain a separate library of verified examples |
| SDK platform adapter policy | Shared protocol behavior may use optional transport/storage/resource adapters; platform-specific threading and runtime constraints are verified per selected target | Keep only technology-neutral guidance; offer an opt-in SDK adapter template with explicit runtime acceptance checks; choose platform-specific requirements per project |
| Code generation verification examples | Generated models need contract fidelity and runtime proof, but the template does not ship an executable compiler/codegen example | Keep verification prompts; offer separately reviewed optional examples for chosen schema/codegen technologies; no default code generator |
| Generated API client state | Client generation and UI view/form state boundaries are defined by consuming application | Opinionated direct-generated-DTO form model; adapter/view-model approach; no default |

## Review sequence after the technical audit

Review existing questions in three passes, without treating the order as priority or consent:

1. **Remaining scope choices:** file identifiers and workflow statuses, and whether separately selected opinionated technology stacks are desirable. The approved default documents, template inventory, planned profile visibility, absence of a demo product and lightweight data flows/milestones are not up for implicit reinterpretation; see [ADR 0005](0005-approved-template-scope.md).
2. **Technology conventions:** approved in [ADR 0006](0006-optional-technology-conventions.md); these are no longer pending. No specialized stack is generated without explicit selection.
3. **Optional integration and example policies:** code-example verification, SDK platform adapter policy, code generation verification examples and generated API client state. These are *candidates for review*, not evidence that the boilerplate needs an SDK, business schema, client cache or source generator.

The existing table is the decision register; the passes above are only a review route, not additional requirements. Closing a proposal as **not applicable** requires no replacement template. Keep all **remaining** entries pending until an explicit owner decision is recorded.

## Approval requirements

For each choice record the selected default versus opt-in scope, reason, affected manifest/template paths, implementation and test evidence, and any migration impact on previously generated docs. A historical pattern alone never grants authority to change generated defaults.

**All 5 choices in the table remain pending.** The five approved scope decisions are in [ADR 0005](0005-approved-template-scope.md) and eight approved technology choices are in [ADR 0006](0006-optional-technology-conventions.md). The active repository is for reusable writing guidance, not for any individual application's requirements. Only generic technical examples are within the approved scope; a demonstrator product is not approved.
