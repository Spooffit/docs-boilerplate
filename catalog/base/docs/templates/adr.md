---
title: "Architecture decision: [Title]"
description: "Reusable writing template"
kind: template
status: template
---

# Architecture decision: [Title]

> **Use:** Record a consequential architecture choice with genuine alternatives. Do not create a numbered ADR just because the template exists. Use the project's already adopted identifier format, if any. Keep a decision **Proposed** until an authorized person or process accepts it. Remove prompts that lack confirmed answers.

- **Decision status:** Proposed (until actually accepted)
- **Decision authority and date (if verified):** [Real authority/date or omit]

## Context and forces

[Describe the verified problem, constraints, implementation boundaries, alternatives already considered and sources of evidence.]

## Decision drivers (if useful)

[Which verified constraints or tradeoffs actually influence this decision? Avoid an exhaustive checklist when only one or two concerns matter.]

## Options evaluated

| Option | Benefits | Costs and risks | Reason for outcome |
| --- | --- | --- | --- |
| [Candidate] | [Grounded positive] | [Grounded negative] | [Chosen / rejected / pending] |

## Decision

[Describe the approved choice only when accepted. While proposed, write the intended decision and clearly mark its unapproved state.]

## Consequences

[What changes as a result of the selected or proposed approach? Document migration and rollback only if something currently deployed must actually change.]

## Verification (when applicable)

[What evidence would establish the decision works in the selected system? Do not mark planned tests as passed.]

## Related documentation

[Link existing architecture overview, standards, contracts and implementation records. Remove this section when there is nothing authoritative to link.]
