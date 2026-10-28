---
title: "Legacy standard reference graph and deletion readiness"
description: "Check incoming references before considering removal of old technology standards"
kind: audit
status: active
---

# Historical standard reference graph

This is a dependency assessment, not authorization to delete a document merely because its subject has been migrated.

## First measured result and disposition

The initial scan of **395 Markdown files** found **24 historical technology standards**. **23 had recognized incoming links**; only `technical/backend/standards/S24-code-formatting.md` had none. The number of incoming documents ranged from one (S01) to 31 (S04). All 23 linked standards remain in `main` pending content and dependency review.

S24's entire formatter instruction has now been migrated to the `.NET conventions` catalog source and the old file removed under a recorded individual disposition. After S01/S23 were fully reviewed and their one/two incoming links updated, future scans examine **21** surviving legacy technology standards. This is the only substantive historical standard removed in this phase.

## Automated check

The script `tests/legacy_link_graph.py` scans Markdown in the repository, resolves ordinary inline links and reference definitions, and reports the number of distinct incoming documents for each legacy Sxx technology standard. It rejects live Markdown links to files marked `removed-cleanup` in the historical inventory.

The earlier inline-link deletion check in `tests/legacy_inventory.py` was over-escaped and could fail to detect normal Markdown links. It now delegates to the shared parser, which is separately exercised in CI.

**Scope:** HTML links, dynamically generated paths, plain-text mentions and links within fenced code blocks are not counted. An unreferenced file is only a *review candidate*, never automatically safe to delete.

## Migration candidates by subject

| Historic group | Possible canonical replacement | Remaining condition |
| --- | --- | --- |
| S01, S23, S24 | Selected .NET conventions, project-files and formatting | Compare actual code/build rules before declaring equivalence |
| S02, S21 | Opt-in PostgreSQL, EF Core, DDD and stateful workflows | Preserve domain/storage guarantees specific to the predecessor |
| S03, S18 | Selected ASP.NET validation and FluentValidation | Do not force FluentValidation on projects not selecting it |
| S04, S09–S15 | Testing standards and optional TUnit/Testcontainers | Reconcile suite boundaries, fixture and environment rules |
| S16, frontend S06 | Selected ASP.NET rules and language-neutral http-contracts | Verify old endpoint/client requirements and source evidence |
| S17 | Base data flows | Preserve cross-owner and transport-specific documentation only where applicable |
| S19, S20 | Selected Ardalis.Specification and CQRS profiles | Don't silently migrate prescribed library architecture to all projects |
| S22 | MassTransit/RabbitMQ and integration-reliability profiles | Old topology is a predecessor-specific decision |
| Frontend S05, S07, S08 | UI accessibility, Blazor and bUnit where selected | Review concrete testing and UI requirements |

These are potential destinations, not completed line-by-line equivalence proofs.

## Deletion gate for substantive history

1. Review the complete source and document whether each reusable rule was preserved, improved or intentionally rejected.
2. Resolve incoming references to actual replacements, without changing meaning or dropping required project-specific constraints.
3. Check all supported references with the script and inspect unsupported HTML, plain text, examples and external/sibling repository links manually.
4. Require the project owner's decision on retaining sanitized examples whenever content has educational or architectural value.
5. Record each path and evidence in the removal ledger, update inventory only when physically removed, then pass CI.

**No substantive source is approved for deletion by this report.** Git history preserves removed file content, but broken links in current documents must still be repaired.

## Further measured test-standard retirement (2026-10-28)

The last passing baseline recorded **19** surviving technology standards; S10, S11 and S12 had respectively 6, 7 and 6 referring Markdown documents (including S10's own S11 reference). After complete source review, the remaining 18 incoming links were migrated to stable anchors of the consolidated .NET testing standard. The active count is now **16** historical standards if CI passes. This does not alter the original 24-standard baseline or certify the old project's test execution.


## S15 shared test engineering source retired (2026-10-28)

The reference graph reported four incoming documents for S15, with five recognized links. All were changed to `catalog/docs/standards/dotnet/testing.md#shared-testing-infrastructure-and-safe-migration` after full source review, before removal. **15** historical candidate-technology standards remain if the link gate and CI pass. The S14 environment standard is still retained due to its much wider incoming-link surface.

## S14 environment document removal (2026-10-28)

After 20 identified incoming-link migrations, the Linux Actions reference report still found **one** S14 referring document, `technical/backend/features/F01-create-entity-schema.md`. Its remaining link is updated as part of S14 retirement. Thus all **21** identified live incoming documents now point to `catalog/docs/standards/dotnet/testing.md#test-environment-ownership-and-bounded-parallelism`. The historical technology-standard count decreases from **15 to 14** pending the deletion-gate CI check. The S04 predecessor document stays in place because the strict cross-suite naming policy requires an owner decision.

## S02/S03 scoped retirements (2026-10-28)

Both complete sources were compared to the canonical baseline. Four known incoming documents each were redirected to typed, relevant anchors before the old files were retired in one commit. The known incoming lists and decision rationale are recorded in [the removal ledger](legacy-removals.md) and [backend technical review](backend-standards-review.md). The surviving historical standard count becomes **12**, subject to the CI link gate. The original project's mandatory Ardalis.Specification, named constraints and FluentValidation policies remain explicitly deferred for owner consultation; no universal dependency was created.

## S18/S19/S20 completed source-and-link migration (2026-10-28)

After full-source review and extraction into their selected profiles, S18/S19/S20 were redirected from the predecessor tree. The prior passing report counted 26/21/18 inbound **documents** respectively. The reference graph on commit `da787f4a715fd8f73deb8331ce2e12080443d987` reported **zero incoming documents** for each. Their deletion leaves **9** historical candidate-technology standards, subject to final CI and inventory checks. The portability decision is to keep `fluentvalidation`, `ardalis-specification` and `cqrs` opt-in; framework/library mandates and exact handler placement are deferred to owner review rather than silently made universal.

## S21/S22 incoming-link reconciliation (2026-10-28)

The last passing GitHub Actions run for commit `6e7afdb8d45f02abcd1e918bfd6c59256622803d` reported **zero** recognized incoming documents for each of S21 and S22, after redirecting references from **17** and **10** previously referring documents respectively. S22's normative topology guidance is now in the optional MassTransit/RabbitMQ profile and S22 is retired with its original text in history. S21 is **intentionally retained**, even without incoming links, because its uncompiled cross-layer example remains an unresolved content-retention choice in the owner backlog. The remaining historical candidate-technology standard count is **8** pending final deletion-gate CI.

## S16/S17 technical migration and S17 deletion gate (2026-10-28)

S16 and S17 were read in full and applicable semantics moved into selected ASP.NET Core and base data-flow documents. Incoming references to S17 were redirected in commit `c16a4dfbf8041fdd1ebe80af410f14a5ebb0c94b` (19 recognized Markdown links). S17 is eligible to retire **after** the CI reference graph confirms zero remaining inbound links; record the completed outcome in the removal ledger. S16 remains intentionally present because the historically mandatory MVC/CQRS/ETag/envelope style and substantial controller examples require owner decisions. The post-removal inventory should contain **7** historical standards, including retained S04, S16 and S21.
