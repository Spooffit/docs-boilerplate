---
title: "Technical feature specification template"
description: "Technology-neutral implementation specification linking behavior to code, boundaries and verifiable evidence"
kind: template
status: template
---

# Technical feature: [Capability and implementation boundary]

> **Use:** Document only the technical complexity that is not already clear from an authoritative contract, source code or tests. This template is **technology-neutral**. Begin with scope, relevant boundaries, behavior and evidence; delete unrelated sections rather than creating new services, layers or interfaces. A standalone business feature document is not mandatory.

## Business outcome and provenance

[Link the accepted requirement, issue or other actual source of the work. A separate business-feature document is optional; identify the observable outcome without copying the entire source.]

| Decision or behavior | Source of authority | Implementation boundary |
| --- | --- | --- |
| [Verified requirement] | [Real document/schema/ADR link] | [Owning component] |

## Scope and assumptions

[Record the chosen implementation boundary, confirmed non-goals and unresolved assumptions. Include delivery-stage distinctions only when the project actually defines them.]

## Components and responsibilities (when applicable)

[Describe only actual components and cross-component boundaries needed to understand this change. For a straightforward change within one component, omit the table.]

| Component / module | Responsibility | Public dependency or interface | State or resource owned |
| --- | --- | --- | --- |
| [Actual component] | [Purpose] | [Real contract] | [Actual owner] |

Do not require fixed Domain/Application/Infrastructure or FSD folder structures unless the selected project architecture explicitly adopts them.

## Contracts and authoritative models

[Link real adopted API or message contracts and actual application models. Show precise shapes only when they are not already authoritative elsewhere. Separate transport DTOs from domain/storage models when those boundaries actually exist.]

## Behavior and data flow

1. [Validated inputs and trusted context.]
2. [Required reads and current-state/concurrency assumptions.]
3. [Business invariant checks and application actions.]
4. [Atomic write boundary and resulting state, if any.]
5. [External effects, delivery guarantees, retries and unknown outcomes, if any.]
6. [Observable response to user, client or consumer.]

Identify cancellation, timeout, idempotency and side-effect behavior where relevant. A client timeout is not proof that server writes rolled back.

## State and failure handling

[For UI features, describe relevant form state and user-visible progress. For backend/integration features, describe conflict handling, transaction rollback, external dependency failure and safe diagnostics. Distinguish invalid inputs, forbidden work, missing resources and concurrent changes. Document additional business states only when supported by the project's approved requirements.]

## Security, reliability and observability (when applicable)

[Link existing security or operational requirements and document only feature-specific implications and risks. Do not invent roles, authentication systems, session workflows, audit history or instrumentation to fill this section.]

## Target design versus observed implementation

[Separate the accepted intended design from **verified current code**. If a repository snapshot, commit, migration, API document or test result was inspected, record its actual reference and scope. Otherwise write "Not verified" rather than describing a planned interface as implemented. A correct contract specification does not prove that consuming applications or deployments currently conform.]

## Source and contract evidence (when material)

| Claim | Accepted target authority | Observed code/contract (version) | Verified execution | Unresolved gap |
| --- | --- | --- | --- | --- |
| [One meaningful interface or state transition] | [Real decision link or Not established] | [Inspected commit/schema or Not verified] | [Test/build/deployment evidence or Not verified] | [Explicit discrepancy] |

[Do not equate a target API design or linked file path with implemented behavior. Check relative links, resolve external-repository source references and distinguish an unknown current implementation from an accepted future requirement. Remove this section for trivial changes without external contracts.]

## Testing and acceptance evidence

| Requirement or risk | Actual SUT | Test scope / dependency fidelity | Observable assertion | Evidence |
| --- | --- | --- | --- | --- |
| [Verified behavior] | [Component or system] | [Unit/integration/contract/E2E] | [Expected result] | [Real CI/test link or Not yet verified] |

Do not mark tests passed because the specification contains test cases.

## Dependencies, unresolved decisions and propagation

[Link actual parent feature, module, ADR, question or milestone sources. State which decision remains open, and which authoritative documents must change if it is resolved. Do not use fictional IDs or placeholder links to nonexistent files.]
