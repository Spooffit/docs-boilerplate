---
title: "Architecture overview"
description: "Evidence-first current and target system architecture"
kind: project
status: draft
---

# Architecture overview

> **Initial scaffold.** An installed technology profile is documentation selection, not an architecture decision. Keep `status: draft` until important boundaries are established by approved decisions or observed sources.

## Context and responsibilities

Identify actual users, external systems, system boundary and responsibilities. Link the [project overview](../project/overview.md) instead of repeating requirements.

## Current system as observed

Record runtime components, deployment units, data stores and interactions **only if verified** in source, configuration, adopted contracts or a running system. Record the commit, version or environment. For greenfield work say **Not yet verified** rather than draw a fictional topology.

| Component / boundary | Observed responsibility | Evidence or gap |
| --- | --- | --- |
| Not yet verified | No topology inferred from templates | Awaiting inspection |

Remove this starter row once real sources are available.

## Accepted target architecture

Describe explicitly approved intended boundaries and how they differ from what exists. Link an [ADR](../templates/adr.md) for consequential choices. Keep competing proposals separate from adopted decisions, and never infer DDD, microservices, CQRS or an ORM from the template.

## Contracts, data and trust boundaries

Link actual API/message schemas and document ownership, permissions, dependent systems and operational boundaries where applicable. A documentation module is not automatically a deployment unit or bounded context.

## Risks and verification

List unresolved assumptions, important consistency and failure risks, and actual source/test evidence for guarantees. A passing Markdown validator cannot verify a running system.

## Open architecture decisions

Name unresolved tradeoffs and the authorized decision-maker **if known**. Do not fill blank sections with invented endpoints, components or performance guarantees.

**Completion check:** Reconcile current observations and accepted target with their sources, resolve important contradictions and remove starter rows before setting `status: active`.
