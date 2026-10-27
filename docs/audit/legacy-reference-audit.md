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
