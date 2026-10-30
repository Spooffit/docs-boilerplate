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
| Document identifiers and statuses: revised | The later owner decision supersedes the opt-in profile. Every generated `docs/` document now has a stable ID by default; no organizational approval statuses are imposed. |
| Code-example verification: A | Examples advertised as executable must have actual build/test evidence in a supported environment. Incomplete fragments are explicitly labeled illustrative; no requirement to compile every excerpt. |
| SDK and platform adapters: removed | **Out of scope**. Do not ship architecture recommendations, templates, technology profiles or runtime assumptions inherited from the former client-library project. Keep ordinary HTTP contract, cancellation, streaming and cross-platform compatibility rules where independently useful. |
| Source-code/model generation: removed | **Out of scope**. Do not add generators, schema registries, generated-model verification processes or specialized examples. The docs-boilerplate CLI itself is a Markdown documentation generator, not a source-code generator. |
| Direct DTO form models: C | Add independently selectable `ui-direct-dto-forms`: use existing suitable API request DTOs directly as form models rather than requiring a second ViewModel by default. Exceptions require genuine contract or UI constraints. This is about UI state and **does not introduce any code-generation process**. |

## Resulting boundaries

The remaining independent direct-DTO profile adds Markdown **guidance**, not application code or new general template kinds. The baseline documentation shell, seven general templates, three existing technology variants and planned technology profiles are unchanged. The UI convention refers to DTO types that a project already uses, regardless of their origin; the boilerplate does not prescribe how those types are created.

Documentation identifiers are mandatory technical reference keys in `docs/`. Approval workflows remain outside the generator: base `status` values are ordinary maintenance metadata, not formal acceptance states. No inherited business ID scheme or entity lifecycle is introduced.

The project examples remain generic; compilation/runtime claims require checked evidence. The existing opt-in EventBus contract fixture remains executable and tested, but no sample business application is installed into generated repositories.

## Verification and compatibility

- Catalog/profile hygiene checks enforce opt-in ownership and no implicit selection by unrelated profiles.
- Linux and Windows smoke verify default document IDs, the remaining direct-DTO opt-in profile and consistent catalog scope.
- Existing generated projects are not modified automatically by this decision. Selection changes require explicit user action; the safe update mechanism does not silently add profiles.
- All owner decision topics from the three reviewed blocks are now resolved; see [decision review status](owner-decision-backlog.md).
