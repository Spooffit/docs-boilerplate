---
title: "Feature: [Concise name]"
description: "Reusable writing template"
kind: template
status: template
---

# Feature: [Concise name]

> **Use:** Describe a confirmed or explicitly proposed observable capability. Begin with purpose, scope, behavior and acceptance; keep additional sections only if supported by project evidence. This page is a writing aid, not a ready-to-publish requirement or a request to create implementation documents.

## Purpose and audience

[Who needs the capability? What problem does it solve? What observable result defines success?]

## Scope and exclusions

[State boundaries, preconditions and explicitly excluded responsibilities. Distinguish accepted scope from proposals.]

## Approval and delivery scope (when applicable)

[State what is accepted, proposed or deferred using existing project evidence. Name a delivery stage only if one has been adopted. Do not invent Demo/MVP/Production phases.]

## Observable behavior

[Describe the real trigger, expected outcome and essential conditions. Use a numbered scenario only when a sequence matters; do not invent UI steps or backend commands.]

## Exceptions and boundary behavior (when applicable)

[Document relevant failures, denied access or conflicting inputs only if they change the observable contract. Use **Unresolved** rather than invent a result.]

## Acceptance criteria

- [Given a verified precondition, when a specific action occurs, then an observable outcome must hold.]
- [Include relevant access, invalid input, retry/concurrency and boundary cases.]

## Data and contracts (when applicable)

[Link existing authoritative field definitions or contracts only if needed for this feature. Do not copy their contents or create new schemas merely to fill a section.]

## Technical references (when applicable)

[Link an existing technical specification if the feature needs one; do not create an implementation document solely because this section exists.]

## Dependencies and open decisions

[Link real parent-module, technical specification, milestone, ADR and question files only when they exist. Identify which document owns each contract or rule. Leave unresolved decisions explicitly pending rather than inventing routes, field types or permissions. When a question is resolved, update affected normative specifications and record that propagation.]
