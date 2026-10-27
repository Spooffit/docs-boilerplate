---
title: "HTTP contracts, API consumers and database migration extraction"
description: "Targeted semantic review of legacy HTTP and storage specifications without importing obsolete project contracts"
kind: audit
status: active
---

# HTTP clients, API contracts and persistence migrations

- Date: 2026-10-28.
- Sources: `technical/backend/questions/Q15-api-and-source-code.md` **selected sections** on source evidence, application routes and idempotent operation replay; `technical/frontend/standards/S06-api-client.md`; `technical/backend/standards/S16-rest.md`; `technical/backend/standards/S02-persistence.md`; `technical/backend/questions/Q20-persistence-and-migration.md` **selected sections** on transaction guards, owner schemas and migration decisions.
- Scope: a **targeted semantic extraction**, **not** a complete line-by-line review of the extensive Q15 and Q20 specifications (whose detailed F01–F18 operation tables and actual code snapshots still require verification). No original services were deployed or tested.
- Inventory: Q15 moves to `reviewed-targeted`. The other four already had this status. None is approved for deletion or certified `reviewed-complete`.

## Observed source patterns

| Source | What was useful to retain | What remains a project choice |
| --- | --- | --- |
| Q15 | Historical code snapshot is not a current public API; contract entries need verified routes, DTO shapes, headers and actual server binding; an operation identity can permit recovery after lost write acknowledgements | Actual routes, `ClientOperationId` body carrier, custom `ApiResponse<T>`, response/status mapping, authorization and linked workflow actions |
| S06 | Generated clients derive from adopted transport contracts; headers like `ETag` need explicit handling; separate transport state and independent UI state; timeout can leave an unknown write outcome | Requiring NSwag or direct generated DTOs for all forms, mandatory preconditions, universal `412/428/409` conventions |
| S16 | HTTP methods, versioning, route binding, transport/application separation, cancellation, error contracts and compatibility testing | Mandatory MVC, CQRS bus, central versioned route prefix and project-specific response mapper |
| S02 | Preserve live data and historical invariants while evolving schemas; database constraints and concurrency guard real races, and DB commits cannot automatically include broker effects | Universal EF Core, Ardalis.Specification, domain constraints class or a single project layout |
| Q20 | Distinguish currently verified migrations and design targets; transaction owner, locks, writer coordination, retained data and operational recovery matter | One database-level advisory lock, named schemas/BC, current provider and permissive pre-launch destructive migration |

## Reusable changes

- [HTTP client contracts](../../catalog/docs/standards/http/client-contracts.md) and [contract verification](../../catalog/docs/standards/http/contract-verification.md) form an independent `http-contracts` profile. It has no ASP.NET Core, CQRS, frontend framework or code-generator prerequisite.
- [PostgreSQL persistence](../../catalog/docs/standards/postgresql/persistence.md) now requires deliberate consideration of DDL lock levels, table/index rewrites, timeouts, index builds, constraint validation and versioned consumer rollout.
- [PostgreSQL migration plan](../../catalog/docs/templates/postgresql-migration.md) is emitted **only with the PostgreSQL profile**. It is a decision/evidence checklist, not another database schema or mandatory multi-stage deployment.
- [EF Core persistence](../../catalog/docs/standards/dotnet/ef-core.md) distinguishes compiled migrations from provider-specific SQL and deployment safety.

## Standards verification versus source evidence

Protocol and vendor behaviors were checked against [RFC 9110](https://www.rfc-editor.org/rfc/rfc9110), [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457), [PostgreSQL ALTER TABLE](https://www.postgresql.org/docs/current/sql-altertable.html), [PostgreSQL CREATE INDEX](https://www.postgresql.org/docs/current/sql-createindex.html), and [EF Core applying migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying).

These external references describe **general technology behavior**, not proof that the old project implemented every accepted requirement or that any old client/endpoint passes integration tests.

## Out of scope and follow-up

Review Q15's feature-by-feature contract tables, actual OpenAPI and generated clients, and Q20's complete multi-owner storage/coordination sections before approving removal. Decide later whether the former project's highly specific examples are useful in isolated, anonymized optional material; do not convert them into normative universal guidance.
