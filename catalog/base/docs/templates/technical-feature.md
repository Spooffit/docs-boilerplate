---
title: "Technical feature specification template"
description: "Technology-neutral implementation specification linking behavior to code, boundaries and verifiable evidence"
kind: template
status: template
---

# Technical feature: [Capability and implementation boundary]

> **Use:** Describe the technical responsibilities of a known feature. This is a **technology-neutral** template: remove irrelevant sections and never add architecture layers, brokers, ORMs, frontend frameworks or code generators solely to fill a heading. Read the business feature and selected standards first.

## Business outcome and provenance

[Link the existing business feature or confirmed requirement. Record its accepted stage and observable result. Do not paraphrase the entire source into a second competing requirement.]

| Decision or behavior | Source of authority | Implementation boundary |
| --- | --- | --- |
| [Verified requirement] | [Real document/schema/ADR link] | [Owning component] |

## Scope, assumptions and delivery stage

[Identify what this technical specification covers now, what belongs to later stages, and which unresolved items remain proposals. Do not turn a Demo target into an implicit production guarantee.]

## Components and responsibility boundaries

[Describe only components required by this feature: application handler, UI component, SDK adapter, persistence mapping, background worker or remote consumer as applicable.]

| Component / module | Responsibility | Public dependency or interface | State or resource owned |
| --- | --- | --- | --- |
| [Actual component] | [Purpose] | [Real contract] | [Actual owner] |

Do not require fixed Domain/Application/Infrastructure or FSD folder structures unless the selected project architecture explicitly adopts them.

## Contracts and authoritative models

[Link real OpenAPI, JSON Schema, message contract, generated client API or UI model definitions. Show precise shapes only when they are not already authoritative elsewhere. Separate transport DTOs from domain/storage models when those boundaries actually exist.]

## Behavior and data flow

1. [Validated inputs and trusted context.]
2. [Required reads and current-state/concurrency assumptions.]
3. [Business invariant checks and application actions.]
4. [Atomic write boundary and resulting state, if any.]
5. [External effects, delivery guarantees, retries and unknown outcomes, if any.]
6. [Observable response to user, client or consumer.]

Identify cancellation, timeout, idempotency and side-effect behavior where relevant. A client timeout is not proof that server writes rolled back.

## State and version history (when applicable)

[For actual versioned or multi-writer resources, describe the selected baseline, concurrent update behavior, history and destructive impact. See the [stateful-workflow checklist](../standards/stateful-workflows.md). Delete this section for stateless features.]

## State and failure handling

[For UI or SDK features, describe state ownership and user-visible progress. For backend/integration features, describe conflict handling, transaction rollback, external dependency failure and safe diagnostics. Distinguish invalid inputs, forbidden work, missing resources and concurrent changes.]

## Security, reliability and observability

[Record selected authentication, operation authorization, resource-specific constraints and data protection only when applicable. Distinguish server-enforced permissions from advisory UI capabilities, stale session/actor context and checks made before a concurrent change. If a session expires during an edit, define whether unsaved input, original concurrency baseline and unknown mutation outcomes can be recovered safely. Do not prescribe an identity provider, roles, cross-origin deployment or security architecture by default. Link the selected project security document, if present, instead of copying its decisions here. Specify actual logs, traces, metrics or audit needs if known.]

## Target design versus observed implementation

[Separate the accepted intended design from **verified current code**. If a repository snapshot, commit, migration, API document or test result was inspected, record its actual reference and scope. Otherwise write "Not verified" rather than describing a planned interface as implemented. A correct contract specification does not prove that client SDKs or deployments currently conform.]

## Source and contract evidence (when applicable)

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
