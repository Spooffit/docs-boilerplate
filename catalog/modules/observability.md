---
title: "Observability"
description: "Project-specific document scaffold"
kind: project
status: draft
---

# Observability

## Signals and ownership

Record where application logs, traces, metrics and alerts live and who operates each destination.

## Instrumentation

List real trace spans and metrics, with names and labels sourced from the implementation.

## Durable activity history (only when required)

A product's persistent history of confirmed user or system actions is **not the same thing** as application logs, traces, security alerts, a domain event store or a temporary UI notification. Define a separate activity-history requirement only when an actual consumer needs it. Link to the authoritative source workflow and any existing domain-owned history rather than creating a second copy of its business rules.

Document which **committed source outcomes** produce history and the granularity of each record. A button click, attempted command, failed operation without a durable result, retry of the same accepted operation or unchanged no-op must not be described as a successful new business effect. Conversely, a new authorized intent can create another record even when its content equals a previous one. Where a workflow commits several stages independently, distinguish each stage's outcome from the overall process result.

If a separate read model receives activity facts asynchronously, establish:

- The **source owner** and its durable capture boundary. A best-effort callback after a successful commit can lose the only record of the action; use a durable intent/outbox or an equivalent verified mechanism when completeness is required.
- Stable identity for the source fact, independent of the receiver's record ID. Define replay handling, duplicate detection and what happens if the same identity arrives with *different* facts. Acknowledgement and target append must agree at a durable boundary; do not assume a transaction spanning independent stores.
- How the user distinguishes the source operation's confirmed success from delayed, failed or missing read-model updates. An empty list during delivery lag is not proof that no action committed.
- The difference between the actor executing work and any **trusted initiating actor**, the exact affected resource/version, safe historical labels, and occurrence versus recording timestamps. Neither observation timestamp is automatically a physical commit time or total causal ordering.

Specify which fields are actually needed to support the audit purpose. Prefer a reviewed allowlist of identifiers and safe state summaries over full payloads, secrets, credentials, raw content, storage URLs or copied domain-owned explanations. A historical record should remain interpretable after a current label changes or the target is removed without granting access to protected target details.

## Activity-history reads and retention (when applicable)

Specify server-side visibility, classification, filtering, ordering and pagination based on the actual security policy. Enforce record visibility **before** page assembly, result counts, filter suggestions and cursor issuance. Permission to read an activity record is not automatically permission to open its target or a separate detailed reason. On identity/query changes, do not let stale client responses populate a different reader's context. A read-model cursor need not promise a frozen snapshot when new events arrive out of order.

Record lag/repair visibility, data minimization, retention and authorized deletion/anonymization requirements only as confirmed policy. Never infer perpetual retention or arbitrary purge intervals from the presence of an append-only implementation. Test duplicate and contradictory deliveries, capture/dispatch failure, delayed append, missing targets, permission leakage, pagination and historical-label integrity against the relevant boundaries.

## Service-level objectives

Document SLOs only after the team agrees on service-level indicators and measurement windows.

## Diagnostics

Link dashboards and runbooks for confirmed operational scenarios. Avoid hardcoded credentials and tokens.
