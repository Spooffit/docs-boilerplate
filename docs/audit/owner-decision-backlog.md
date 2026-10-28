---
title: "Deferred owner decisions for template finalization"
description: "Questions requiring project-owner judgment after engineering and audit blocks are complete"
kind: audit
status: draft
---

# Deferred owner decisions

This is a **decision backlog**, not a questionnaire to answer during ongoing engineering work. Items are deliberately left unapproved. Resolve them with the repository owner **after** technical, test and audit work is complete, and update canonical templates/ADRs only when decisions are confirmed.

## Candidate decisions

| Topic | Why it needs owner input | Options to evaluate |
| --- | --- | --- |
| Historical examples | The predecessor source contains large, highly specific F/M/Q specifications that are poor universal defaults | Keep no examples; retain a small sanitized and isolated example project; preserve only via Git history |
| Prescriptive .NET architecture | Original backend documents prescribe Domain/Application.Abstractions/EF/Infrastructure, particular query placement and aggregate constraint classes; the current catalog is neutral | Keep only neutral architecture profiles; provide one or more explicitly optional opinionated profiles, after executable checks |
| Default template breadth | There is a base Feature, Module, Question, ADR, Standard, Technical Feature and Milestone set alongside .NET/Blazor variants | Keep a small permanent core; move more templates to opt-in modules; add specialized templates only with a real reuse case |
| Requirements identifiers and status | Original files use F/M/Q identifiers that are useful inside that project, but not necessarily universal | Descriptive filenames with optional project IDs; opt-in structured ID scheme; avoid mandatory global numbering |
| Example/code verification threshold | Former technical features contain large amounts of source-specific code, API shapes and contracts | Require compiling, version-pinned examples; permit clearly labeled pseudocode; keep examples separate from normative standards |
| Historical milestone structure | Original Demo/MVP/Production phases are project-specific; a neutral milestone template now exists | Leave stages fully project-defined; offer an optional curated stage example without implied commitments |
| Strict test naming convention | Historical backend S04 and frontend S08 require `MethodUnderTest_Scenario_ExpectedBehavior` for every test category; the portable .NET catalog deliberately treats the format as a project choice | Adopt the exact convention through an explicit optional opinionated profile; recommend descriptive behavior-focused names without mandatory shape; enforce a different project-approved convention |

## Decisions carried forward from S02/S03

| Topic | Legacy policy requiring a decision | Options to evaluate |
| --- | --- | --- |
| Mandatory FluentValidation | S03 requires FluentValidation for every incoming .NET model; the reusable baseline currently makes it an optional profile | Require it only in an explicitly opinionated .NET profile; leave validation-library selection project-specific; document a preferred alternative with tested integration |
| Mandatory Ardalis.Specification and named aggregate constraints | S02/S19 tie all entity filtering to Ardalis.Specification; S02/S21 require `<AggregateName>Constraints` | Make each an opt-in project convention; define a separately selected opinionated persistence profile; retain the tool-neutral baseline and let projects choose their own abstractions |

These are **not approvals**. Historic project documents may keep describing their original chosen conventions. New projects must not inherit them automatically without profile selection and verification.

## S21/S22 architecture options to decide after technical migration

| Topic | Historical prescriptive policy | Options for the owner |
| --- | --- | --- |
| Aggregate constraints catalog and sample style | S21 requires a public static `<AggregateName>Constraints` tree for **every** aggregate and embeds substantial illustrative C# examples | Keep a neutral DDD baseline and optional documented project convention; create a separate opinionated DDD profile; retain a sanitized example project only after compiling its examples |
| EventBus packaging/topology defaults | S22 requires `Integration/EventBus/{Contracts,RabbitMq,Default}`, a registrar, `msg.{integration}.{purpose}`, explicit publish topology, DB outbox and contract versioning for every EventBus application | Adopt as opt-in opinionated MassTransit/RabbitMQ profile; select only parts after compatibility testing; leave projects to set topology/naming/outbox based on their actual contracts |

Neither row is approval. Avoid changing the generator's default profile selections on the basis of these original-project documents alone.

## Required evidence before consultation

- The structural inventory and detailed migration notes are reconciled with source files.
- Necessary catalog profile and generator tests are passing on supported platforms.
- Remaining old links and generated-output leakage have been checked.
- Each option is illustrated by actual files or concise examples, not speculative project details.
- The cleanup plan states which legacy files would be removed under each accepted choice.

Refer to [project-source deletion gates](project-source-triage.md), [technical feature screening](technical-feature-screening.md) and [root/milestone screening](root-milestone-screening.md).
