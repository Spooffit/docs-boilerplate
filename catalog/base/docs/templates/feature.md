---
title: "Feature: [Concise name]"
description: "Reusable writing template"
kind: template
status: template
---

# Feature: [Concise name]

> **Use:** Describe one observable capability from the product/business perspective. Replace this instruction and all bracketed guidance with verified information. Delete irrelevant optional sections. Do not record a discussion transcript here.

## Purpose and audience

[Who needs the capability? What problem does it solve? What observable result defines success?]

## Scope and exclusions

[State boundaries, preconditions and explicitly excluded responsibilities. Distinguish accepted scope from proposals.]

## Delivery stage and decision status

[Identify which behaviors are approved for the specific milestone or release. Mark deferred behaviors separately. A feature's future MVP scope is not automatically part of its current Demo or production contract. Record accepted decisions and unresolved choices with links to actual decision records.]

## Main scenario

1. [Trigger: user or system action with known preconditions.]
2. [Describe externally observable steps without prescribing an unapproved implementation.]
3. [Describe the completed result.]

## Alternate and failure scenarios

[What changes if input is invalid, access is denied, a resource is missing, state conflicts or a dependency fails? Define outcomes where known.]

## Acceptance criteria

- [Given a verified precondition, when a specific action occurs, then an observable outcome must hold.]
- [Include relevant access, invalid input, retry/concurrency and boundary cases.]

## Data and contracts

[Link the canonical domain glossary and contract only if they actually exist. Do not duplicate field definitions from the authoritative schema.]

## Technical implementation

[Link a technical-feature specification only if this capability needs separate implementation detail. For complex behavior, use the optional `technical-feature.md` template or a technology-specific variant. Do not duplicate implementation details in this business document.]

## Dependencies and open decisions

[Link real parent-module, technical specification, milestone, ADR and question files only when they exist. Identify which document owns each contract or rule. Leave unresolved decisions explicitly pending rather than inventing routes, field types or permissions. When a question is resolved, update affected normative specifications and record that propagation.]
