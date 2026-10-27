# M06. Работа сотрудников

## Назначение

M06 связывает Identity, moderation и audit в один пользовательский workflow. Это **модуль документации**, а не обязательный единый bounded context.

## Карта фич

| ID | Ownership |
| --- | --- |
| [F16](../features/F16-staff-access.md) | IdentityAccess BC / staff authentication, fixed system roles, sessions and provisioning |
| [F17](../features/F17-content-review.md) | Publishing BC moderation exact PublicationRevision |
| [F18](../features/F18-staff-activity-history.md) | Append-only Audit module/read model |

## Roles

`Specialist`:
- authoring/publishing work;
- moderation submit/withdraw;
- conflict resolution;
- activity read.

`Administrator`:
- staff access management;
- moderation comment/approve/reject;
- explicit revision Withdrawal;
- activity read.

Roles can be combined. Mandatory four-eyes/self-approval prohibition is absent MVP.
Specialist/Administrator — fixed system roles без implicit inheritance. Administrator не получает authoring Specialist role автоматически; combined user имеет permissions обеих. Permission catalog/initial RolePermission mapping server-fixed, custom roles/permission editor future.

## Identity boundary

CM Identity is independent from VR School.

IdentityAccess использует ASP.NET Core Identity и infrastructure `ContentManagerUser : IdentityUser<Guid>`. Duplicate Domain StaffUser/password/roles/stamps не вводятся. Authorization.Abstractions даёт независимые contracts/metadata, AspNetCore adapters связывают HTTP с Identity; resource/domain rules принадлежат owning BC. Detailed models/backend types определяются один раз в backend F16.

Один fresh coherent PermissionSnapshot на HTTP request, без cross-request cache. Role change виден в next request; checked in-flight может завершиться. AllowedActions от capability resolver — advisory UX, не permission token. Вкладка проверяет expected login context перед mutation; это не credential и не замена server access/CSRF.

Blazor Standalone UI/API независимо хостятся на HTTPS-поддоменах одного базового домена: different origins/same site. Shared same-origin reverse proxy не выбирается. Cookie host-only API, exact UI-origin credentialed CORS и antiforgery обязательны. State-changing Login/Logout/setup/reset/account requests защищаются от CSRF; protected persistent shared DP keys API replicas имеют separate staff/Delivery purposes.

No public registration.

Admin-only provision использует immutable unique Login и required DisplayName, без required email. Permanent password пользователь выбирает сам через purpose/user/security-generation-bound provider setup/reset code, default 20 минут и atomic one-use. Public register/email reset отсутствуют. Password Identity hash, 15–128 без composition, weak-password check/paste; lockout default 5 failures/15 минут, rate limiting обязателен с numeric deployment limits TBD, MFA future.

Initial administrator создаётся manual operator bootstrap без default password/autostart; repeat не creates нового user. Emergency recovery — отдельная процедура. Last-admin guard сохраняет хотя бы одного IsActive + Administrator + CredentialReady в той же Identity transaction, включая concurrent writes; self-deactivate только при другом ready admin.

StaffSession с own Guid имеет user/time/absolute expiry/terminal revocation, default 8 часов без sliding/refresh/RememberMe. Logout current оставляет другие sessions. Deactivate/reset invalidates previous sessions, Reactivate требует new Login. Runtime installed data/credentials и anonymous Demo staff lifecycle не затрагивает.

Recovery контентной формы только memory той же open SPA tab: internal reauth без reload, actual input/File objects, original owner/object/version/ETag/base/pending context сохраняются. Same-user fresh GET не перезаписывает baseline; autosave/blind retry unknown mutation нет. Different actor/manual Logout clears snapshot; reload/close/browser restart не гарантируются. Local tab identity notices помогают обнаружить actor change, expected login context защищает server mutation.

## Moderation

Only exact immutable ProductVariantPublicationRevision is mandatory review target.

```text
Revision
  → Pending moderation
  → Approved
      → Manual: specialist activates
      → Automatic: F11 continues activation
```

Rejected content creates a new revision/iteration after correction. Approval never transfers by hash.

## Audit

F18 — immutable persisted MVP Activity History /admin/activity, eventual append confirmed own business outcomes. Committed не означает всех promotion targets activated; typed details могут содержать partial/Failed item. Whole-tree Save/atomic PREPARE tuple one result, human decision/activation отдельно; no-op/replayed ClientOperationId не создают row, intentional new artifact equal content creates.

It does not replace:
- F11 activation history;
- F17 moderation history;
- domain state.

Auditable content/lifecycle/Publishing/F17/F15 результаты и meaningful automated Publishing/catalog conflict lifecycle определены [F18](../features/F18-staff-activity-history.md); обычные reads/Login/failedcommands/securitylogs/transport/inbox/retry/plain projections/Delivery materialization исключены. IdentityAccess provision/roles/deact/react/setup-reset initiation/completion/bootstrap/emergency записи имеют staff-access Admin-only classification.

Staff/System/Integration/Operator actors не смешиваются; automatic executor отдельно от persisted staff initiator. Optional safe as-of labels/typed exact IDs сохраняются без rewrite после rename/deactivation/delete. Metadata whitelist только state/role/policy/count/ID/operation refs; no secrets/full values/before-after/storage URLs/ETags. Full F17 reason остаётся exact authoritative owner reference, denied/gone/unavailable не empty remark.

Common journal Specialist/Administrator имеют audit.activity.read; staff-access только Administrator audit.staff-access.read. No personal ownership filter/target-read permission inference. Keyset descending OccurredAt/Id, default 50/max 200, filters/cursor bound reader context/fresh auth; visible rows/filter options до paging, author options из audit history, no required total/stable snapshot. UI manual refresh без realtime/poll/export.

Source intent атомарен с own commit; at-least-once dispatch сохраняет stable EventId/source-fact identity → Audit append/inbox own transaction с independently receiver-allocated row Id без cross-BC commit. Same source duplicate idempotent, changed facts quarantined. Source success не ждёт append, empty journal не доказательство отсутствия action. MVP no auto-purge/user FK cascade; exceptional privacy/archive/export policy позже, не legal forever promise. [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#f18-принятый-activity-contract)/[Q20](../../technical/backend/questions/Q20-persistence-and-migration.md#f18-принятый-capture-append-и-cursor-read) описывают exact engineering contracts; F16 rules не меняются.

## Persistence boundaries

- Identity: own `identity` schema/provider DbContext.
- Moderation: Publishing schema/DbContext.
- Audit: own `audit` schema/read model.

No cross-schema physical FK or shared cross-BC transaction.

## Demo / MVP

Demo excludes M06 entirely.

MVP enables authentication, roles, moderation and audit after core ContentModeling/Publishing paths work.

## Связи

[Q10 permissions](../questions/Q10-permissions-and-isolation.md) · [Q28 moderation](../questions/Q28-content-review-scope.md) · [Q24 sign-in](../../technical/frontend/questions/Q24-admin-sign-in.md).

Принятый [Q24 protocol](../../technical/frontend/questions/Q24-admin-sign-in.md#принятый-протокол-identityaccess-и-восстановления-формы); exact [Q15 contracts/capabilities](../../technical/backend/questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities) и [Q20 persistence/snapshots](../../technical/backend/questions/Q20-persistence-and-migration.md#identityaccess-persistence-и-authorization-snapshots). Provider/wire/setup operational details engineering; role/lifetime/recovery/topology semantics уже выбраны.
