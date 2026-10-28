---
title: "ADR 0005: Approved default documents and reusable template scope"
description: "Owner-approved initial documentation, template inventory, planned profiles and example policy"
kind: adr
status: active
---

# ADR 0005: Approved default documents and reusable template scope

- Status: Accepted
- Date: 2026-10-28
- Authority: Explicit repository-owner approval of the first five editorial choices

## Context

The generator is an AI-agent-first documentation boilerplate, not a copy of an application. The initial review confirmed that the current minimal defaults already match the owner-approved scope. Recording those choices prevents later catalog changes from silently introducing inherited business workflows, required artifacts or nonfunctional placeholder technologies.

## Decision

1. **Initial documentation:** Generate both `docs/project/overview.md` (Project Overview) and `docs/architecture/overview.md` (Architecture Overview). Each starts with `status: draft` and requires real project evidence before its contents can be treated as accepted facts. Their presence does not require additional feature or architecture documents.
2. **Template library:** Retain all seven general templates: Feature, Technical Feature, Module, Question, ADR, Standard and Milestone. Retain three technology-specific variants: .NET backend feature, Blazor frontend feature and PostgreSQL migration. The variants are installed only through their selected technology profiles; neither installation nor availability requires creating documents from them.
3. **Planned technologies:** Keep `go`, `angular`, `react`, `blazorise` and `custom-ui` visible as `planned`. They remain non-selectable, carry no generated standards and must not be treated as implemented. Do not silently promote or remove them.
4. **Examples:** Do not include a demonstration product or sample application in the boilerplate. Generic, reusable illustrations of engineering rules are permitted; they cannot introduce assumed business entities, product-specific processes or verified implementation claims.
5. **Data flows and milestones:** Keep documentation lightweight and conditional. Do not require global `DF-ID` identifiers or invent Demo/MVP/Production phases. Flow identifiers, diagrams, checkpoints and verification detail are used only when justified by the actual project.

## Scope and verification

- Initial documents and seven general templates live under `catalog/base/docs/`; the optional variants live under `catalog/docs/templates/` and are selected through `catalog/profiles.json`.
- Planned profile status and non-selection remain enforced by the catalog resolver and CLI tests.
- `catalog/base/docs/standards/data-flows.md`, `catalog/base/docs/templates/milestone.md` and the overview documents contain conditional authoring guidance, not a product workflow.
- `tests/catalog_hygiene.py` checks the approved inventory and lightweight rules. `tests/smoke.sh` checks generated draft overviews, optional selections, and rejection of unavailable profiles. CI must pass at the revision being evaluated.

## Consequences

- These approvals **confirm existing behavior**, not a request to add more generated files or profile dependencies.
- Existing generated repositories need no migration merely because this decision was recorded. Normal fingerprint-safe update behavior remains unchanged.
- Other editorial and technical conventions remain unresolved in [the owner decision backlog](owner-decision-backlog.md); this decision does not approve their proposed variants.
