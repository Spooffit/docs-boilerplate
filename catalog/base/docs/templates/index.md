---
title: "Choose a documentation template"
description: "Smallest useful artifact, canonical ownership, and optional technology variants"
kind: index
status: active
---

# Choose a documentation template

**Start from the question, not a folder.** Templates are **blank writing aids**, not proof that a feature, decision or milestone exists. Copy only the template needed for the current task. Replace editorial placeholders and remove irrelevant sections before use; do not treat a template as a completed project document.

**First project pass:** Complete the existing [project overview](../project/overview.md) and [architecture overview](../architecture/overview.md) from verified requirements, observed code and accepted decisions. Both start `draft`. No separate mandatory intake file or full family of feature records is needed.

| Need | Start with | Do not create it merely because |
| --- | --- | --- |
| Define an observable product capability and acceptance | [Feature](feature.md) | A class or endpoint was added without a separately defined outcome |
| Explain a substantial implementation or interaction | [Technical feature](technical-feature.md) | An existing source contract and tests already explain a simple change |
| Group several capabilities and cross-feature ownership | [Module](module.md) | A new folder, project or runtime service exists |
| Record an unresolved choice and propagate its resolution | [Question](question.md) | The choice is already settled and documented |
| Record consequential architecture tradeoffs | [ADR](adr.md) | A routine detail has no real architectural alternatives |
| Specify reusable verifiable engineering rules | [Standard](standard.md) | One feature needs a one-off implementation behavior |
| Define a real delivery checkpoint and acceptance evidence | [Milestone](milestone.md) | Demo, MVP or Production was mentioned hypothetically |

## Minimal authoring rule

1. Identify the actual information gap. If a small addition to an existing authoritative document resolves it, prefer that over a new file.
2. Select **one** fitting template or technology-specific alternative. Begin with purpose, concrete facts and acceptance/verification. Remove non-applicable headings, example rows, status/date fields and instructional text rather than inventing filler.
3. Distinguish accepted requirements, proposals, inspected code and executed tests. Only retain real identifiers, owners, version numbers and document links. When no facts are known, state the uncertainty instead of fabricating a detailed document.
4. After writing, update the immediate directory index and `docs/catalog.md` where appropriate. A blank template remains `status: template`; a new unapproved project document should normally begin `status: draft`. Neither label establishes approval or implementation.

## Keep one canonical source

- Read [project overview](../project/overview.md), [architecture overview](../architecture/overview.md) and [authoring conventions](../standards/documentation.md) first.
- Put **product behavior** in a feature, **implementation detail** in a technical specification, **cross-feature grouping** in a module, **decisions** in a question/ADR, and **shared rules** in a standard. Link actual API/message schemas rather than recopying their field lists.
- Use relative Markdown links to existing documents. Never invent identifiers, owners, statuses, routes, implemented behavior or passing tests to fill a template.
- When a decision changes, update its authoritative document and dependent links; do not maintain duplicate rules in Feature, Technical Feature and ADR.
- **No separate module, question, milestone or technical specification is mandatory** unless the actual documentation process calls for it.

## Technology-specific variants

A selected profile may install specialized templates such as `dotnet-backend-feature.md`, `blazor-frontend-feature.md` or `postgresql-migration.md`. Discover **installed** variants in [the generated catalog](../catalog.md). Use a specialized feature template **instead of** the general technical-feature template for the same document; keep both only if they describe genuinely distinct scopes. Installed templates do not require adopting their example fields or creating new documents.

This directory is a reference library. Do not create empty feature/module/milestone directories or placeholder decision records just to mirror these template names.
