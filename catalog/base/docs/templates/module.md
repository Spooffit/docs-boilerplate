---
title: "Module: [Name]"
description: "Reusable writing template"
kind: template
status: template
---

# Module: [Name]

> **Use:** Group multiple related capabilities only when the grouping carries useful ownership or coordination information. A code folder, service or feature name is not by itself a reason to create this document. Remove optional sections without verified content.

## Purpose and outcomes

[What capability emerges from these related features? Which users or systems benefit?]

## Ownership and boundaries

[What belongs inside the module? Which neighboring modules or external systems does it depend on?]

## Capability map

| Capability | Existing source (if any) |
| --- | --- |
| [Confirmed capability] | [Actual relative link or explicitly not documented] |

## Cross-feature behavior

[Describe handoffs and sequencing only where needed to understand the combined result. Distinguish module-as-documentation grouping, module-as-runtime boundary and domain bounded context; they are not necessarily the same unit. Link each owned public interface to its authoritative contract instead of restating fields.]

## Delivery and ownership (when applicable)

[Describe real responsibilities or agreed checkpoints only when they span several capabilities. Do not imply a deployment boundary or mandatory lifecycle.]

## Related specifications (when applicable)

[Link only actual feature, API or technical documents; do not create one file per technology layer by convention.]

## Open decisions

[Record unresolved cross-cutting design decisions and link to their authoritative question/ADR.]
