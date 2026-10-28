---
title: "Root, navigation and milestone structural screening"
description: "Final structural pass over root instructions, legacy index files, glossary and milestone plans"
kind: audit
status: active
---

# Root, navigation and milestone source screening

- Date: 2026-10-27
- Scope: the remaining **27** source inventory entries, comprising **21** predecessor documentation files and **6** root configuration or maintainer guidance files.
- Method: inspected each file's contents or top-level structure and intended role. This is **not** exhaustive technical/requirements validation or permission to delete.
- Result: all inventory entries now have at least a documented structural disposition, while only previously targeted entries have undergone selective rule extraction.

## Root and product-specific navigation

- `business/README.md`, `features.md`, `modules.md`, `questions.md`, and `standards.md` describe the predecessor project's F/M/Q identifiers and cross-layer document map.
- This is useful **as a pattern of traceable navigation**, not as reusable instance content. The generator already produces `docs/catalog.md`, `llms.txt` and per-folder `index.md` for the **selected** project.
- `business/glossary.md` contains real predecessor product terminology. The generator offers the independent optional project glossary module. Do **not** import old definitions or their domain actors into fresh projects.
- Historical `technical/backend/README.md`, `technical/frontend/README.md` and their `AGENTS.md` files prescribe that particular solution's runtime, layer diagram and documentation workflow. Their portable agent practice is reflected by the current root `AGENTS.md` and separate skills, without requiring the old technology choices.
- `business/adr/README.md`, `business/archive/README.md`, `technical/*/adr/README.md` and `technical/*/archive/README.md` contain brief directory placeholders. They do not define authoritative product behavior. They may be candidates for removal **after** the deletion gate and incoming-link check.

## Delivery milestones

The predecessor `milestones/README.md`, `milestones/demo/demo-acceptance.md`, `milestones/demo/demo-readiness.md`, `milestones/mvp/mvp.md` and `milestones/production/production.md` document stage scope, readiness, dependencies and measurable acceptance intentions.

They reinforce two genuinely portable distinctions: **agreed milestone scope is not current implementation evidence**, and a **prepared acceptance matrix is not a passed test report**. Those conventions were already preserved in `catalog/base/docs/templates/milestone.md`; no hard-coded Demo/MVP/Production stages or legacy feature IDs are needed.

## Root maintainer tooling

The inventory's six `root-config-or-guidance` entries are not all historical product documents:

- `.editorconfig` and `.gitignore` contain ordinary formatting and generated-output exclusions.
- Root `README.md`, `CONTRIBUTING.md`, `AGENTS.md` are active maintainer entrypoints, maintained separately from the generated project sources.
- `.ai/mcp/mcp.json` was empty **at the time of this structural screening**. It was subsequently removed as a zero-byte placeholder under the [recorded cleanup](legacy-removals.md). Neither its former presence nor its removal demonstrates a working MCP setup.

The six files are marked `screened-structure` to document classification. They are **not** approved for deletion merely because they originated in the baseline inventory.

## Outcome and limits

The original inventory had 196 entries. Following this pass, **43** are `reviewed-targeted` and **153** are `screened-structure`; **none** is marked `reviewed-complete`.

- `screened-structure`: scope, themes and possible reusable patterns examined; no full content verification.
- `reviewed-targeted`: selected portable rules or templates studied/extracted; not necessarily line-by-line review.
- `reviewed-complete`: reserved for a future explicit source-level disposition with actual verification and deletion/retention decision.

No historical material has been deleted by this screening. The remaining project is a **semantic migration and cleanup**, not another mass rename of files.
