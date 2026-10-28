---
title: "ADR 0007: Final documentation and UI conventions"
description: "Last five owner decisions, including explicit removal of inherited platform-specific workflows"
kind: adr
status: active
---

# ADR 0007: Final documentation and UI conventions

- Status: Accepted
- Date: 2026-10-28
- Authority: Explicit repository-owner approval of final block and later clarification of items 3 and 4

## Decisions

| Topic | Approved policy |
| --- | --- |
| Document identifiers and statuses: B | Add independently selectable `document-identifiers-and-statuses`; project-specific IDs, decision approval and workflow statuses never become mandatory across all projects. |
| Code-example verification: A | Examples advertised as executable must have actual build/test evidence in a supported environment. Incomplete fragments are explicitly labeled illustrative; no requirement to compile every excerpt. |
| SDK and platform adapters: removed | **Out of scope**. Do not ship architecture recommendations, templates, technology profiles or runtime assumptions inherited from the former client-library project. Keep ordinary HTTP contract, cancellation, streaming and cross-platform compatibility rules where independently useful. |
| Source-code/model generation: removed | **Out of scope**. Do not add generators, schema registries, generated-model verification processes or specialized examples. The docs-boilerplate CLI itself is a Markdown documentation generator, not a source-code generator. |
| Direct DTO form models: C | Add independently selectable `ui-direct-dto-forms`: use existing suitable API request DTOs directly as form models rather than requiring a second ViewModel by default. Exceptions require genuine contract or UI constraints. This is about UI state and **does not introduce any code-generation process**. |

## Resulting boundaries

The two new optional profiles add Markdown **standards**, not application code or new general template kinds. The baseline documentation shell, seven general templates, three existing technology variants and planned technology profiles are unchanged. The UI convention refers to DTO types that a project already uses, regardless of their origin; the boilerplate does not prescribe how those types are created.

Documentation identifiers and approvals are a project-level adoption choice: the base `status` values are document metadata and are not formal organizational acceptance states. No inherited business ID scheme or entity lifecycle is introduced.

The project examples remain generic; compilation/runtime claims require checked evidence. The existing opt-in EventBus contract fixture remains executable and tested, but no sample business application is installed into generated repositories.

## Verification and compatibility

- Catalog/profile hygiene checks enforce opt-in ownership and no implicit selection by unrelated profiles.
- Linux and Windows generation smoke verify both new profiles are selectable and do not appear without selection; Linux CI tests code-example evidence and active catalog scope.
- Existing generated projects are not modified automatically by this decision. Selection changes require explicit user action; the safe update mechanism does not silently add profiles.
- All owner decision topics from the three reviewed blocks are now resolved; see [decision review status](owner-decision-backlog.md).
