---
title: "F16 staff access: cross-layer semantic evidence review"
description: "Targeted historical review of staff identity, permissions, sessions and safe UI recovery"
kind: audit
status: active
---

# F16 staff access: semantic and evidence review

- Date: 2026-10-28.
- Scope: [business F16](../../business/features/F16-staff-access.md), [business Q10](../../business/questions/Q10-permissions-and-isolation.md), [business M06](../../business/modules/M06-staff-workflow.md), [backend F16](../../technical/backend/features/F16-staff-access.md), [frontend F16](../../technical/frontend/features/F16-staff-access.md) and the accepted [frontend Q24](../../technical/frontend/questions/Q24-admin-sign-in.md).
- Related source boundaries: targeted F16 sections of [backend Q15](../../technical/backend/questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities) and [backend Q20](../../technical/backend/questions/Q20-persistence-and-migration.md#identityaccess-persistence-и-authorization-snapshots). Their complete 751/728-line contract and persistence documents are **not** certified by this review.
- Review depth: targeted semantic comparison of the whole business F16/Q10/Q24, M06's ownership/identity sections and the backend/frontend F16 contract, data-flow, security, UX and verification sections. **Not** a line-by-line executable verification of the full 633-line backend source or every sibling feature.
- Classification: `reviewed-targeted` for the six primary sources. This status means extracted and compared relevant rules, **not** `reviewed-complete` or deletion approval.

## What the historical sources actually decide

| Boundary | Accepted predecessor-specific target, not a universal requirement | Reusable review lesson |
| --- | --- | --- |
| Staff identity versus customer access | Separate IdentityAccess model, fixed Specialist/Administrator roles, shared content scope, no implicit role inheritance; Demo deliberately unauthenticated | Distinguish identity populations, resource scope, deployment stages and permission ownership; never infer role inheritance or register users from a different identity system |
| Authorization versus UI hints | One fresh coherent permission/session/account snapshot per request, current owner-state checks, advisory `AllowedActions`; already-checked in-flight requests may complete | State where and when authorization is enforced, what client hints mean, and whether access revocation affects in-flight work |
| Browser trust and state | Independently hosted same-site but different-origin Blazor UI/API, API host-only cookie, credentialed CORS and antiforgery; selected HTTP and cookie settings belong to its Q15/Q24 contract | Describe **origin, site and credential boundaries** for the actual deployment. CORS or SameSite must not be assumed to replace CSRF prevention |
| Provisioning and session lifecycle | Manual first-admin bootstrap, one-use setup/reset, absolute sessions, last-ready-admin guard against concurrent removal, explicit logout/reset consequences | Record session/credential state transitions, recovery invariants and how concurrency preserves them **if** the project has those requirements |
| In-progress UI operations | Reauthentication in the same open SPA tab retains the same actor's input, File objects, original resource identity/ETag and pending operation; no blind replay, local persistence or cross-actor restore | Separate unsaved UI intent from confirmed server execution. A new read or new login does not prove old input may overwrite current state or that a timed-out write failed |
| F16/F17/F18 responsibility | M06 is a documentation workflow grouping identity, moderation and audit across distinct owners, not one bounded context | Treat a module index as navigation, not evidence of a shared data store, transaction or deployable service |

The old numeric settings (eight-hour session, 20-minute setup code, password and lockout policy), named libraries, exact roles, same-site hosting arrangement, GUID models, route patterns and user-interface choices are **not imported** to the generator. These are accepted targets for the predecessor and must remain so unless a new project independently chooses them.

## Canonical extraction and deliberate exclusions

- The **optional** [security architecture scaffold](../../catalog/modules/security.md) now asks for the permission/resource enforcement map, session and credential lifecycle, cross-origin/CSRF assumptions, actor-aware unsaved-work recovery, operational availability guards and concrete verification evidence. This does not add a new default file or profile.
- The [portable technical-feature template](../../catalog/base/docs/templates/technical-feature.md#security-reliability-and-observability) now distinguishes authentication, permissions, owning-resource rules, session/actor changes and uncertain in-progress mutations. Delete irrelevant guidance for unauthenticated or stateless projects.
- Existing [specification traceability](../../catalog/base/docs/standards/specification-traceability.md), [HTTP validation/authorization](../../catalog/docs/standards/aspnet/errors-and-validation.md), [client contracts](../../catalog/docs/standards/http/client-contracts.md) and [UI state](../../catalog/docs/standards/ui/forms-and-accessibility.md) already cover the general boundaries; do not copy F16's exact DTOs, cookie policy or internal implementation topology into those standards.
- No new universal IdentityAccess, ASP.NET Identity, Blazor or role-management profile has been declared available.

## Evidence and unresolved technical risks

The predecessor's backend F16 explicitly cites a scoped 2026-10-05 code search that did not find a deployed staff Identity provider, store or authentication API in the then-inspected checkout. This is **an assertion in the historical documentation**, not a new code inspection by this audit. Q24 and the F16 tests describe accepted intended behavior and verification plans, not proof of a working server or browser application.

The historical `archive/identity-access-architecture.md` linked from Q24/backend/frontend F16 is **absent from this repository** (GitHub fetch returned 404). It is a missing provenance dependency, not grounds to fabricate its contents. Backend F16 also links directly into a sibling `contentmanager.api` checkout outside this repository. Those paths have not been verified. The same evidence limitation applies to Q15/Q20 cross-repository references.

| Required evidence for any future reusable executable sample | Current conclusion |
| --- | --- |
| Version-pinned ASP.NET Identity/provider build and endpoint/OpenAPI contract | Not verified |
| Actual cookie/credentialed CORS/CSRF behavior across deployed origins | Not verified |
| Fresh grants versus resource authorization, stale capability and account-switch rejection | Source requirement only; no runtime proof |
| PostgreSQL concurrency of last-admin, setup consumption, revocation and session state | Test plan only; no barrier/fault run verified |
| Same-user open-tab form/File recovery, original ETag and unknown-outcome inquiry | UI requirement only; no browser E2E run verified |
| Current source/provenance for archived IdentityAccess architecture | Missing local evidence; do not silently repair with guessed content |

The optional scaffold is grounded against the public [OWASP authorization guidance](https://cheatsheetseries.owasp.org/cheatsheets/Authorization_Cheat_Sheet.html) and [OWASP CSRF guidance](https://cheatsheetseries.owasp.org/cheatsheets/Cross-Site_Request_Forgery_Prevention_Cheat_Sheet.html). ASP.NET Core antiforgery behavior must be checked for the actually selected .NET version and endpoint type; generic documentation does **not** assert that JSON APIs receive token validation automatically.

## Disposition and cleanup gate

Retain **all six** primary source documents. Mark their inventory entries `reviewed-targeted`, not `reviewed-complete`. Do not remove F16 or Q24 until missing links and external evidence are classified, full-source residual content is reviewed, relevant cross-document references are updated and each source receives a recorded disposition under [project-source deletion gates](project-source-triage.md).

The existing [owner decision backlog](owner-decision-backlog.md) already covers optional opinionated architectures, historical examples and default template breadth. No owner answer is inferred from this audit. If the owner later wants a standalone IdentityAccess sample, its framework, wire contract, sample scope and executable verification need an explicit decision first.

**Verification boundary:** this change only compares documentation and augments optional authoring scaffolds. It does not prove that the predecessor application is secure, that any F16 test passes or that current source code implements the selected target.
