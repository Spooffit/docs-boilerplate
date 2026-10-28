---
title: "Documentation template migration: first extraction"
description: "Historic and ongoing source analysis"
kind: audit
status: active
---

# Documentation template migration: first extraction

- Date: 2026-10-27
- Reviewed: the source business feature/module/question templates, shared ADR/standard templates, .NET backend feature template and Blazor frontend feature template.
- State: targeted review, not a complete cleanup of all old templates. Legacy content remains preserved.

## Decisions

- The old **backend feature** template documents aggregates, application abstractions, EF Core, CQRS and RabbitMQ with strong assumptions about the source solution. The new `docs/templates/dotnet-backend-feature.md` retains detailed model ownership, field-level contracts, data flows, failure modes and test strategy while making the optional layers conditional.
- The old **frontend feature** template contains useful state ownership, component boundaries and interaction flows, but presumes Standalone Blazor, MudBlazor, NSwag and FSD. The new `docs/templates/blazor-frontend-feature.md` preserves useful information without dictating component architecture or UI tooling.
- The original **business feature** was concise but repetitive. The revised generic template defines observable results, scope, alternates, acceptance evidence and open decisions.
- The original **module** and **question** templates correctly separated cross-feature outcomes and unresolved decisions. They are now available as permanent reusable blank templates without a requirement to create speculative module/question records.
- The **ADR** template uses a Proposed default and explicit drivers, tradeoffs and validation rather than granting an unstated acceptance.
- The **standard** template requires applicability and verifiable rules; source code and external technical facts belong to appropriate sources, not unfounded examples.

## Output boundaries

The generator copies only these reusable blank templates. They are **not automatically turned into project-specific approved requirements**. Their bracketed editorial instructions are intended for authors; the generator's unresolved initialization placeholders are a separate concept. Files selected by technology:
- Generic feature, ADR, module, question and standard templates: base.
- .NET backend feature template: selected `dotnet` (or a requiring profile).
- Blazor UI feature template: selected `blazor` (or a requiring profile).

## Remaining

Audit remaining templates and decide whether further project-specific examples deserve migration. Perform rendered Markdown reviews and any compile checks if future templates include actual source examples.

## Further template deduplication review (2026-10-27)

Both `technical/backend/templates/module.md` and `technical/frontend/templates/module.md` were checked and are textually identical. The same is true for the two `question.md` variants. These should **not** become separate generated canonical templates: the single base `docs/templates/module.md` and `docs/templates/question.md` cover their portable semantics.

The old business `adr.md` and `standard.md` are also covered by the new base `docs/templates/adr.md` and `docs/templates/standard.md`. Old template README files were reviewed for project-specific references and general authoring advice; source-specific ID conventions and fixed runtime technologies are not universally required.

Source files remain preserved until cleanup; `reviewed-targeted` indicates classification, **not** exhaustive verification of all linked project documents.

## Generated repository footprint audit (2026-10-28)

The **generated artifact**, not just the repository's reusable source catalog, now has an explicit smoke-test integrity check at `tests/generated_output_hygiene.py`. A new test case selects **every currently available** profile from `catalog/profiles.json`, every module from `catalog/modules.json`, and every agent skill from `catalog/skills.json`, then runs `init` and `validate`. Planned profiles are deliberately excluded.

The checker compares all newly written files to `docs.config.json`'s managed-file list and SHA-256 fingerprints, rejects predecessor identifiers and old `business/`/`technical/`/generator trees, ensures maintainer-only `docs/audit/` or `docs/legacy/` aren't emitted, and rejects extraneous HTML pages or generated JS/CSS bundles. The only supported site page is the optional Docsify `index.html`; the `--site none` case must have none. It also checks agent entrypoints. Separate **negative fixtures** add a forbidden predecessor name and an unwanted JS asset to prove these specific detections fail the audit.

This test is intended only for **freshly initialized** repositories. User-authored files and modifications are valid after initialization and remain protected by the CLI's normal `validate`/fingerprint update rules. The checker does not assert that every historical document is semantically reusable, that a third-party example compiles, or that Docsify JavaScript works in a real browser. Full-source cleanup remains subject to the project-source deletion gates and the owner's decisions.

## AI-first template navigation without extra files (2026-10-28)

The default generated `docs/templates/index.md` is now authored in `catalog/base/docs/templates/index.md` rather than synthesized as a generic directory list. It distinguishes which of the seven base templates answers a real documentation need, explicitly discourages speculative files, and tells agents to choose installed optional technology variants instead of duplicating a general technical specification.

The **output path and file count are unchanged**. `AddDirectoryIndexes` already respected an authored index, but `validate` mistakenly treated that file as a duplicate directory index. That bug is corrected while catalog preflight still rejects two different sources owning the same path.

The smoke suite checks index content, navigation from `llms.txt` and `docs/catalog.md`, an upgrade from a previously synthesized fingerprint-owned index to the authored one, and rejection of a profile trying to claim the base index. The current default seven-template breadth remains an unresolved project-owner decision; the change does not prune categories or create empty project folders.
