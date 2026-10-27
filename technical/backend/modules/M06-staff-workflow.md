# M06. Работа сотрудников

## Назначение

M06 связывает пользовательские workflow внутренних сотрудников в MVP. Это модуль документации, а не отдельный bounded context и не отдельный deployable service.

В Demo F16–F18 отсутствуют.

## Карта фич

| ID | Ответственность и owner |
| --- | --- |
| [F16](../features/F16-staff-access.md) | отдельный IdentityAccess BC на ASP.NET Core Identity: provider users/roles Guid, staff sessions/setup/access |
| [F17](../features/F17-content-review.md) | Moderation exact ProductVariantPublicationRevision; domain/persistence принадлежат Publishing BC |
| [F18](../features/F18-staff-activity-history.md) | append-only audit/read model в отдельном technical audit persistence contour |

## IdentityAccess и Authorization

[F16](../features/F16-staff-access.md) defines implementation models/commands/ports once. IdentityAccess separate BC owns infrastructure ContentManagerUser : IdentityUser<Guid>, fixed provider roles/RolePermission, StaffSession and one-use setup coordination; no duplicate Domain.StaffUser/password/stamp repository. General Authorization.Abstractions carries framework-free metadata/ICurrentUser/PermissionSnapshot/definitions/checker; ASP.NET Core adapters handle HTTP. Business Domain remains independent.

ONE fresh coherent committed active+ready+session+memberships+fixed grants snapshot per protected request, no interrequest permission cache; cookie role claims not authority. Fixed Specialist/Administrator combined, admin does not inherit specialist grants. Coarse permission then owner resource/Domain checks; CapabilityResolver advisory AllowedActions from same snapshot, no resource Version bump or shared304 capability cache. Mutation original login-context precondition prevents A's form executed as B after another-tab login, fresh server actor always authority.

Independent Standalone Blazor UI/API use HTTPS subdomains of same base domain: different-origin same-site, API host-only SecureHttpOnly cookie/defaultLax, credentials include/CORS exact UI origin/no wildcard; CSRF includes login/logout/setup/reset. No forced reverse proxy. Persistent protected shared DP ring/purpose isolation from Q16 Delivery runtime; physical deployment profiles Q20.

Staff sessions default8h absolute/no sliding/refresh/RememberMe. Current logout leaves others; deactivate/reset invalidate old sessions/reactivate new login only. Password proof bound to exact captured provider stamp/generation, final guarded reread must match before issuing Session/cookie. Admin pending provisioning and20min one-use provider setup/reset consume+password+state atomic, permanent password unknown to admin; manual operator bootstrap repeat no newuser, no seed password/public register/mail reset.

Guarded IdentityAccess own transaction preserves final IsActive+Administrator+CredentialReady account across concurrent role/lifecycle/readiness changes; self action allowed when another remains. Provider user/membership/Version/session/setup/normalized F18 intent atomic; no cross-BC UoW. Temporary provider/database failure503 not invalid credential/revoke. Already checked in-flight may finish; subsequent request uses new roles.

Open SPA-tab memory form/File recovery after same-user reauth without reload retains ORIGINAL resource baseline/ETag; explicit save only. Close/reload/restart excluded. Exact wire/context/setup-delivery/operator interface/config remain [Q24](../../frontend/questions/Q24-admin-sign-in.md#принятый-протокол-identityaccess-и-восстановления-формы), [Q15](../questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities), [Q20](../questions/Q20-persistence-and-migration.md#identityaccess-persistence-и-authorization-snapshots).

## Moderation

F17 является quality gate exact immutable `ProductVariantPublicationRevision`.

Moderation aggregate хранится в Publishing BC/schema, поскольку Approval непосредственно разрешает F11 activation.

Manual:

```text
Revision -> Moderation -> Approval -> Specialist Activate
```

Automatic:

```text
Revision -> Moderation -> Approval -> automatic F11 Activate
```

Approval не является отдельной клиентской публикацией и не заменяет Delivery effective transition.

## Audit

[F18](../features/F18-staff-activity-history.md) defines immutable technical Audit models/capture/append/query implementations once. Audit is not new business BC/event store. Each registered source owner atomically captures SourceActivityIntent with independent committed business result; after-commit dispatcher sends versioned event at-least-once with original EventId. Audit record+inbox separate own commit/unique SourceEventId; conflicting normalized fact quarantined/no overwrite, equality preserves original precision rather than lossy relational timestamp reserialization. No global source+Audit transaction or memory-only after-commit gap; Audit append does not take source global write gate.

One whole Save tree one fact, atomic Publishing prepare revision+moderation+bound item tuple one, human decision/activation and committed item outcomes separate. Genuine no-op/ClientOperationId replay none, deliberate new artifact same content new fact. Outcome Committed + typed BusinessResult can report Blocked/partial, never fake all Activated. F15 repair and Resolve separate owner commits; IdentityAccess guarded provider/last-admin atomicity preserved. GET/download/codegen/loginfail/securitycounter/projection/Delivery materialization/transport noise excluded.

Actual Staff/System/Integration/Operator actor; Guid staff only Staff, optional persisted original InitiatorStaffId for System not impersonation. Source UTC action time and audit UTC append time not physical commit/global cause order. Safe optional as-of captions/typed IDs/state/roles/policy/counts/operation refs; no raw content/full before-after/secrets/hash/ETag/storageURL. Full F17 reason stays exact authorized owner reference; unavailable differs from empty.

General /admin/activity: audit.activity.read Specialist+Administrator. Fixed ActionProfile staff-access records additionally require audit.staff-access.read Administrator only; producer cannot choose label to evade class. Fresh F16 ICurrentUser/PermissionSnapshot once/request; visibility before paging/count/options, author options from visible history not staff directory. Target/reason navigation independently authorized, no audit-granted content access.

Selected GET /api/v1/activity, /api/v1/activity/{activityId}, /api/v1/activity/filter-options read only. UTC [From,To), no default time cutoff; SQL exact source OccurredAt order via private UTC100ns bigint ticks DESC/Id DESC canonical UUID keyset, default50/max200, viewer+normalized-filter-bound opaque cursor/HasMore/NextCursor, no total required/frozen global snapshot promise. Manual refresh resets first page for late append, no optimistic local row/audit wait before source success.

All MVP records retained without auto purge/user deletion/cascade; future archive/export/anonymization explicit, not legal forever promise. Exact wire/cursor/canonical codec/physical registration engineering [Q15](../questions/Q15-api-and-source-code.md#f18-принятый-activity-contract)/[Q20](../questions/Q20-persistence-and-migration.md#f18-принятый-capture-append-и-cursor-read).

Предметные истории остаются у владельцев:
- activation history — Publishing F11;
- moderation iterations/comments — Publishing F17;
- policy history — Publishing F12.

## Persistence

Target schemas:

```text
identity    // IdentityAccess own provider/session/setup/grants/intent; physical mapping Q20
audit       // immutable StaffActivityRecord/inbox/quarantine; no foreign roots
publishing  // ModerationIteration/comments вместе с остальным Publishing
```

M06 не получает собственную `staff_workflow` schema только ради названия модуля.

## Межмодульные связи

- ContentModeling/MediaLibrary/Publishing/CatalogAccess получают actor через `ICurrentUser`/request PermissionSnapshot;
- Publishing F17 проверяет Staff permissions через authorization/Application context, но не читает Identity tables;
- registered independent committed business results capture own F18 intent, true no-op/replay/infrastructure noise excluded;
- frontend использует один раздел «Модерация» для Specialist/Admin с role-dependent actions.

## Связи

[F16](../features/F16-staff-access.md) · [F17](../features/F17-content-review.md) · [F18](../features/F18-staff-activity-history.md) · [Publishing M04](M04-publishing.md) · [Q10](../../../business/questions/Q10-permissions-and-isolation.md) · [Q28](../../../business/questions/Q28-content-review-scope.md)
