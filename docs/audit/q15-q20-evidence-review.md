---
title: "Q15 and Q20 evidence and feature-contract review"
description: "Scoped contract and transaction-boundary analysis with missing legacy source evidence explicitly recorded"
kind: audit
status: active
---

# Q15 and Q20: contract and transaction evidence review

- Date: 2026-10-28.
- Primary sources: `technical/backend/questions/Q15-api-and-source-code.md` (API contracts) and `technical/backend/questions/Q20-persistence-and-migration.md` (storage/transaction coordination).
- Cross-checked slices: `technical/backend/features/F09-content-releases.md` (API §5), `F10-publication-revisions.md` (API §5), `F11-publication-lifecycle.md` (API §5) and `F17-content-review.md` (API §5 and Q20 linked workflow sections). All source files are retained.
- Method: inspect the Q15/Q20 section map, selected source/current-state/target-contract sections, workflow boundaries and each feature's API/precondition/error sections. **This is targeted semantic review**, not an exhaustive validation of Q15's 751 lines, Q20's 728 lines or the complete backend feature implementations.
- Scope of verification: the old documentation repository and its links only. Neither an original application checkout nor deployed API, database migration, compiled SDK or message consumer was verified.

## Intent versus actual state

The original source **explicitly** distinguishes a code snapshot dated 2026-10-01 from an accepted future contract. It reports some MVC API/DTO evidence and a generated UI API-client reference, but says the external Delivery controller/source SDK contracts are not confirmed.

Several later target API tables describe `If-Match`, `ClientOperationId`, endpoint names and lifecycle operations as *selected future behavior*. **Do not reinterpret those design tables as a current deployment contract.**

| Source | Verified from this docs review | Evidence still missing |
| --- | --- | --- |
| Q15 current-state and target sections | Historical statements differentiate existing routes from proposed public operations; technical concurrency, source freshness and independent business basis are distinct concepts | Date-pinned actual OpenAPI, server route tests, real response headers/DTO serialization, source files and generated-client regeneration |
| Q20 guard and command execution sections | A chosen target protocol distinguishes owner DB transaction, cross-owner fresh reads, durable command execution receipt and external side effects after commit | Actual PostgreSQL transaction/lock ordering, rollback/cancel behavior, DB uniqueness, relevant writer participation and fault tests |
| F09 §5 | Target Create differs from current combined publish/activation, and new release has its own resource token distinct from source assignment | Real adopted controller and generated client; exact success status, metadata and source-version tests |
| F10 §5 | Target pure preview, create and independent lifecycle preconditions differ from old `publishing/...` routes | Actual route/schema, immutability and withdrawal tests |
| F11 §5 | Target operation progress/item token differs from publication-state token; terminal/async outcomes are not guaranteed by an HTTP success | Actual route, item/result metadata, continuation and race tests |
| F17 §5 | Historical docs describe target moderation endpoints and explicitly note no source implementation was located | Real endpoint contracts, approval comparison-basis validation, moderation concurrency and deployment evidence |

## Three independent consistency checks

1. **Own state version** protects a resource's concurrent mutation. A matching `If-Match` token does **not** establish freshness of independently changing data from another owner.
2. **Comparison/review basis** describes what the user consented to or what a server used when computing a decision. A fresh owner version does not automatically validate an older cross-owner basis.
3. **Durable operation identity** lets a server recover the **original committed receipt** after a lost response where the project has adopted this design. An operation ID is not the resource ID, a representation checksum, or a permit to retry changed input.

This is why targeted Q15/Q20 decisions about transaction guards, original-result lookup, exact data ownership and continuation checks cannot be replaced by a simplistic global `409/412` status map.

## Durable multi-stage work

The original target documents divide a long workflow into owner-local durable stages: start/capture; prepare; external/human decision; continuation/activation; independent downstream delivery. They distinguish source success from dispatched message, consumer applied state and later public visibility.

Portable review rule: **each stage that can crash must leave sufficient durable progress and identity to resume without inventing a new business action**. A local database lock or transaction cannot span broker dispatch, a human review, or a different service's commit. Retrying a terminal event must not create a new published state/history entry without explicit authorization.

These are conditional engineering concerns, not a mandate that projects implement moderation, global locks, publication, or the exact predecessor state machine.

## Evidence-link audit: baseline

Q15 and Q20 contain **76 references** to source files under `../../../../contentmanager.api/` or `../../../../contentmanager.ui/`, which are **outside this repository**. Their existence, freshness and correctness cannot be established from the current `docs-boilerplate` checkout.

There are also **five distinct missing in-repository evidence files** referenced by the two documents:

- `archive/current-state-code-snapshot-2026-10-01.md`
- `archive/identity-access-architecture.md`
- `technical/backend/f03-demo-implementation-plan.md`
- `technical/backend/f05-demo-implementation-plan.md`
- `technical/backend/sync-wire-v1-vectors.md`

The source tree has many other legitimate links to existing business, technical and question documents. These five missing files are **recorded evidence debt**, not license to fabricate snapshots or silently drop citations. `tests/source_evidence_links.py` watches both documents for additional missing files or new references escaping the repository, while allowing missing baseline references to be resolved.

This test checks file paths, **not** Markdown anchors, live external sibling repositories or technical correctness.

## Changes to the reusable catalog

- [Specification traceability](../../catalog/base/docs/standards/specification-traceability.md) now distinguishes documented evidence authority and missing source references.
- [HTTP contract verification](../../catalog/docs/standards/http/contract-verification.md) adds a per-operation current-versus-target evidence matrix.
- [Integration reliability](../../catalog/docs/standards/integrations/reliable-synchronization.md) explicitly covers original receipts, durable multi-stage continuation and cross-owner read freshness.
- [Technical feature template](../../catalog/base/docs/templates/technical-feature.md) gains a short optional source-evidence table.

All four additions are **conditional, project-neutral authoring guidance**. No product-specific URL, SQL lock protocol, C# DTO or stage sequence is added to generated project documentation.

## Removal gate

Q15/Q20 and the F09/F10/F11/F17 technical features are now `reviewed-targeted`, **not** `reviewed-complete`. Before removing them from `main`, identify the exact still-useful original wire/SQL contracts, verify or explicitly reject any missing source evidence, check incoming references, and apply the eventual decision on sanitized optional examples. Their source links must not be called verified until those actual repositories and snapshots have been read.
