# F16. Вход и доступ сотрудников: бэкенд

[Бизнесовая спецификация](../../../business/features/F16-staff-access.md) · [M06](../modules/M06-staff-workflow.md) · [Фронтенд](../../frontend/features/F16-staff-access.md) · [Q10](../../../business/questions/Q10-permissions-and-isolation.md) · [Q24](../../frontend/questions/Q24-admin-sign-in.md#f16-техническая-модель-доступа-и-восстановления).

## 1. Область реализации

MVP предоставляет сотрудникам собственной организации отдельную Identity CM, вход/выход, подтверждённый текущий профиль и административное предоставление/изменение/отзыв доступа. Специалисты равноправны и работают со всем общим контентом, без персонального Product/Variant/organization/team scope. Админ управляет сотрудниками и выполняет проверку F17; совмещение ролей допустимо, исключительные permissions не наследуются только из названия роли. Пользователь/лицензия VR School не являются StaffUser CM и не дают административный доступ.

Demo не зависит от Identity, ролей, входа, подготовленного пользователя или F16–F18. Сотрудники не получают source CRUD каталога: Product/ProductVariant остаются VR School. SSO/импорт пользователей VR School не входят в текущий объём.

IdentityAccess — отдельный BC на ASP.NET Core Identity с Guid user/role IDs; общая Authorization.Abstractions отделяет metadata/current-user/permission snapshot от ASP.NET Core adapters. На каждом защищённом запросе ONE fresh coherent committed active+staff session+roles+grants snapshot, без interrequest cache; старые cookie role claims не authority. Already-checked in-flight may finish, next request sees committed change.

Browser UI — independently hosted Standalone Blazor на другом HTTPS origin той же base domain: same-site subdomains, host-only Secure HttpOnly API cookie/defaultLax, credentials include/exact-origin CORS/antiforgery. Принятый [Q24 протокол](../../frontend/questions/Q24-admin-sign-in.md#принятый-протокол-identityaccess-и-восстановления-формы), [Q15 contracts](../questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities), [Q20 persistence](../questions/Q20-persistence-and-migration.md#identityaccess-persistence-и-authorization-snapshots); [архив источника](../../../archive/identity-access-architecture.md) сохраняет provenance, его illustrative tree/cache/TBD не заменяют принятый протокол.

Проверенная исходная точка 05.10.2026:

| Источник | Факт checkout | Граница вывода |
| --- | --- | --- |
| [Program.cs](../../../../contentmanager.api/src/App/ContentManager.Host/Program.cs), [Host module](../../../../contentmanager.api/src/App/ContentManager.Host/ContentManagerHostModule.cs), [web phases](../../../../contentmanager.api/src/BuildingBlocks/ContentManager.Modularity.Web/Extensions/ModularityWebApplicationExtensions.cs) | Host подключает Api/Application/EF и modular web phases | Эти вызовы не доказывают готовую Identity |
| [Api pipeline](../../../../contentmanager.api/src/BoundedContexts/Shared/ContentManager.Api.Shared/DependencyInjection/WebApplicationExtensions/ApiWebApplicationExtensions.cs) | Exception/status pages/forwarded headers/CORS/HTTPS/Scalar/controllers; auth middleware здесь не добавлен | MVP auth integration предстоит; Demo не считается Identity проверкой |
| Scoped поиск src без bin/obj/Migrations и csproj references | StaffUser, IdentityUser/IdentityDbContext, AddAuthentication/UseAuthentication/UseAuthorization и отдельный staff/auth API не найдены | Нет подтверждения локального provider/store/session реализации; не утверждение о поведении внешнего deployment |
| [Result mapping](../../../../contentmanager.api/src/BoundedContexts/Shared/ContentManager.Api.Shared/Extensions/ResultMappingExtensions.cs) | ResultError Unauthorized/Forbidden отображаются в401/403 | Общий mapper не является authn/authz enforcement |

## 2. Domain

### 2.1. Aggregates, entities и value objects

IdentityAccess — отдельный BC. ASP.NET Core Identity владеет users/roles/password hashing/stamps; infrastructure ContentManagerUser : IdentityUser<Guid> и ContentManagerRole : IdentityRole<Guid> не являются business Domain entities. Domain остаётся минимальным: fixed role semantics, last-ready-admin guard и реальные lifecycle rules. Нет дублирующего Domain.StaffUser, password/stamp repositories либо IdentityUser в Shared. Business Domain других BC не зависит от Identity/Authorization.AspNetCore.

Provider extension/own technical records ниже определены один раз здесь; поля наследуемого provider отмечены явно. Physical mapping EF (§6), repositories/query adapters EF.Persistence (§7), а не illustrative reversed tree источника.

| Модель | Роль | Состояние / lifecycle | Владелец / граница |
| --- | --- | --- | --- |
| ContentManagerUser | Provider user extension | Pending credential/ready, active/inactive; own Version | IdentityAccess infrastructure |
| ContentManagerRole | Provider fixed role | Specialist/Administrator, Guid identity | IdentityAccess infrastructure; server-fixed |
| RolePermission | Fixed persisted role grant | Read-only MVP mapping to registry | IdentityAccess; no permission editor |
| StaffSession | Technical session owner | Absolute expiry/first terminal revoke | IdentityAccess own mutable record |
| CredentialSetupAction | One-use provider setup/reset coordination |20min expiry, consumed/revoked terminal | IdentityAccess; no plaintext code store |
| PermissionSnapshot / metadata | Immutable request value | One fresh coherent committed snapshot | Authorization.Abstractions §3 |
| StaffRole | Logical fixed enum | Specialist, Administrator | Combined allowed, no implicit inheritance |

**ContentManagerUser (private provider extension)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | Guid | Да | Inherited provider user identity | Server-generated; stable actor ID, no foreign FK |
| UserName | string | Да | Immutable Login using provider field | Unique normalized login; format/normalization/limits Q15 |
| NormalizedUserName | string | Да | Provider-normalized unique login | Server provider normalization; never client authority |
| DisplayName | string | Да | Required human-readable label | No new profile-edit CRUD; limits Q15 |
| IsActive | bool | Да | Administrative access state | Inactive never starts protected operation |
| CredentialReady | bool | Да | Password setup successfully committed | Pending setup not login-ready; last admin counts only Ready |
| CredentialGeneration | long | Да | Own setup/reset security generation | Server monotonic; setup proof bound to exact generation |
| CreatedAt | DateTimeOffset | Да | Server creation time | Immutable |
| DeactivatedAt | DateTimeOffset? | Условно | Current lifecycle metadata | Repeat/reactivate metadata profile Q15 |
| Version | int | Да | Own concurrency token user/access state | Internal; provider user/memberships/state mutate coherently |
| PasswordHash | string? | Нет | Inherited provider private hash | ASP.NET Core Identity only; no DTO/audit/log copy |
| SecurityStamp | string? | Нет | Inherited provider private security metadata | Not grants authority/read contract; no separate store |
| ConcurrencyStamp | string? | Нет | Inherited provider concurrency metadata | Coordinate provider writes with own Version/transaction; no API exposure |
| AccessFailedCount | int | Да | Inherited private provider failed-attempt counter | Only fresh guarded own transaction updates; never old prepared value overwrite |
| LockoutEnd | DateTimeOffset? | Нет | Inherited provider lockout deadline | Private; guarded failure/reset writes |
| LockoutEnabled | bool | Да | Inherited provider lockout setting | Configured provider policy; no public editor |
| Email | string? | Нет | Inherited optional provider field | Not required; no email confirmation/reset-mail workflow selected |

**ContentManagerRole : IdentityRole<Guid> (private)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | Guid | Да | Provider fixed role identity | Server-owned |
| Name | string | Да | Fixed role name | Specialist or Administrator; exact wire literals Q15 |
| NormalizedName | string | Да | Provider normalized role name | Unique |
| ConcurrencyStamp | string? | Нет | Inherited provider metadata | Private; no custom-role mutations |

**RolePermission (private fixed mapping)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| RoleId | Guid | Да | Own provider role | Own FK only |
| Permission | PermissionName | Да | Grant from registered BC definition | Composite unique; server-fixed/read-only MVP; no direct user grants |

**StaffSession (own technical state)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | Guid | Да | Server-generated session identity | Cookie identity/session pointer, not standalone credential |
| UserId | Guid | Да | Exact provider user | Own IdentityAccess FK only |
| CreatedAt | DateTimeOffset | Да | Server admission time | Absolute lifetime starts once |
| ExpiresAt | DateTimeOffset | Да | CreatedAt + configured8h default | No sliding/refresh/RememberMe |
| AdmittedCredentialGeneration | long | Да | Exact generation validated by provider password proof | Final gate unchanged generation/stamp; cookie only for same committed session generation |
| RevokedAt | DateTimeOffset? | Нет | First terminal revocation | Logout current session; deactivation/reset all user's old sessions; never restore |
| RevocationReason | Уточняется [Q15](../questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities) | Условно | Safe terminal reason | No credential or provider secret |
| Version | int | Да | Own mutable session concurrency | Internal, no read DTO |

**CredentialSetupAction (private one-use state)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | Guid | Да | Server action identity | Exact provider proof binding, no secret URL identity |
| UserId | Guid | Да | Target provider user | Own FK |
| Purpose | CredentialSetupPurpose | Да | InitialSetup or Reset logical enum | Separate provider purpose from cookie/Delivery; exact codec Q15 |
| CredentialGeneration | long | Да | Captured exact own security generation | Newer generation invalidates older actions |
| CreatedAt | DateTimeOffset | Да | Server creation time | Own durable issuance |
| ExpiresAt | DateTimeOffset | Да | Configured20min default | Absolute, equality expired |
| ConsumedAt | DateTimeOffset? | Нет | First successful use | Atomic password/provider-state update; one winner |
| RevokedAt | DateTimeOffset? | Нет | Terminal explicit invalidation | Never reusable after consume/revoke |
| Version | int | Да | Own action preconditions | Internal; no plaintext setup/reset code or hash invented |

### 2.2. Методы и переходы

| Тип / boundary | Метод | Параметры | Предусловия | Изменение / результат |
| --- | --- | --- | --- | --- |
| Provider user adapter | ProvisionPending | Login/DisplayName/fixed roles | Admin or explicit operator bootstrap; unique Login | Own user pending setup; provider+membership+Version atomic |
| User access coordinator | ReplaceRoles / Deactivate / Reactivate | Target/full roles/expected Version | Fresh admin snapshot, guarded last-ready-admin check | Own coherent memberships/state/token; deactivation terminally revokes old sessions |
| Provider password coordinator | ConsumeSetup / Reset | Exact one-use proof/new password | Exact action/user/purpose/generation/unexpired; one winner | Atomic consume+Identity password+ready/state/stamps/Version+old-session invalidation |
| Staff session | Create / Revoke | Trusted user/time | Active+CredentialReady/provider valid proof | Absolute8h session; current logout terminal, other sessions remain |
| LastReadyAdministratorGuard | Check | Fresh own active/ready/membership values + proposed transition | First own global gate | Prevent losing final active ready Administrator; self-deactivate allowed if another |
| CapabilityResolver | Resolve | Fresh immutable snapshot + owned resource facts | Same request scope | Advisory AllowedActions; no mutation permit |

### 2.3. Инварианты и constraints

1. Fixed Specialist/Administrator combined; no inherited extra role permissions, custom roles, direct user grants or editor. Registered BC-owned permission definitions globally unique; persisted RolePermission must exist in registry. Numeric/literal catalog Q15.
2. Unique immutable Login and required DisplayName; Email optional. Stable Guid actor/history survives deactivate; no delete/profile-edit surface introduced.
3. Per-request ONE fresh coherent committed active+session+roles+grants snapshot; old cookie role claims/stamps/browser state do not authorize. No inter-request permission cache. Once-checked in-flight request may finish; next request sees committed changes.
4. StaffSession8h default absolute, no sliding/refresh/RememberMe. Logout current session only; deactivate/reset invalidate old sessions; reactivate requires new login, never reactivates old material.
5. Guarded admin mutation must preserve at least one IsActive+Administrator+CredentialReady account. Concurrent remove/deactivate/reset operations use same existing MVP guard within own IdentityAccess transaction; no cross-BC UoW.
6. Setup/reset provider proof one-use,20min default, bound purpose+user+generation; atomic consume/password/state. Admin does not know permanent password. No public register/email flow; bootstrap explicit manual operator, no seed password/automatic startup user.
7. Identity password hashing,15–128 characters/paste/no composition rules/weak-password check; defaults5 failed attempts/15min lockout configured via options, required rate limiting (numeric profile engineering). MFA/SSO future decision.
8. Mutation ExpectedLoginContext prevents form opened as A being submitted as B after another-tab login; fresh server identity always authority, context is precondition not credential.
9. Reauth form memory preserved in open SPA tab including File references; no page reload, same confirmed user, original owner baseline/ETag kept, explicit action only. Close/reload/browser restart excluded; server stores no snapshots.
10. Domain business invariants/resource authority remain owner checks independent of permission. AllowedActions changes by user/current auth without core Version change; no shared cache/304 capability response until representation boundary selected Q15.

### 2.4. Domain events

IdentityAccess commits normalized safe F18 activity intent atomically with successful own user/membership/lifecycle/setup change. Audit append/dispatch is separate owner boundary, never shared SQL transaction or audit record before source commit. Envelope/idempotency fields defined once [F18](F18-staff-activity-history.md); provider security diagnostics differ from business audit. No password/hash/stamp/code/cookie/body-secret/form payload; failed rolled-back mutation not successful activity. Staff deactivation does not revoke Q16 Delivery/license credentials.

## 3. Application.Abstractions

### 3.1. Commands

Logical target names below do not establish exact HTTP URI/provider API. Command handlers IdentityAccess.Application; framework-free Authorization.Abstractions metadata applies across BC commands/queries. Explicit anonymous metadata for login/setup/bootstrap appropriate boundaries, no implicit default allow.

| Command | Назначение | Result | Concurrency |
| --- | --- | --- | --- |
| SignInStaffCommand | Identity password login/absolute staff session | Safe metadata + cookie adapter | Fresh provider/user/state, no staff If-Match |
| SignOutStaffCommand | Revoke current session | Confirmed outcome | Trusted exact session, no arbitrary user body |
| CreateStaffUserCommand | Admin pending account provision | StaffUserDto + secure setup outcome profile Q15 | New Guid, normalized Login unique |
| ChangeStaffUserRolesCommand | Replace full known roles | StaffUserDto + own ETag | ExpectedVersion + ExpectedLoginContext |
| DeactivateStaffUserCommand / ReactivateStaffUserCommand | Administrative lifecycle | StaffUserDto + own ETag | Same own preconditions/last-admin guard |
| IssueCredentialSetupCommand | Admin one-use initial/reset proof | Secure issuance result Q15 | Own target Version/context; guarded generation |
| CompleteCredentialSetupCommand | User chooses permanent password | Safe confirmed setup outcome | Atomic single-use proof/action/generation |
| Bootstrap administrator use case | Explicit operator one-time setup | Safe operator outcome | Manual protected boundary, no auto-start/public bypass |

**SignInStaffCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Login | string | Да | Immutable normalized account identifier | Provider normalization/limits Q15; not Email requirement |
| Password | string | Да | Ephemeral secret provider proof | No logs/audit/body copy; paste supported |
| ExpectedLoginContext | Уточняется [Q15](../questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities) | Условно | Original tab login context for actor-changing action | Not authentication; exact anonymous/current context carrier Q15 |

**CreateStaffUserCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Login | string | Да | Immutable requested login | Unique normalized; no generated password |
| DisplayName | string | Да | Required safe name | Format/limits Q15 |
| Roles | IReadOnlySet<StaffRole> | Да | Explicit fixed membership | No custom roles/duplicate/inheritance; empty-set profile Q15 |
| ExpectedLoginContext | Уточняется [Q15](../questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities) | Да | Original admin tab context | Fresh current actor remains authority |

**ChangeStaffUserRolesCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| UserId | Guid | Да | Exact target | Own user identity |
| Roles | IReadOnlySet<StaffRole> | Да | Desired full fixed roles | Guard final ready Administrator |
| ExpectedVersion | int | Да | Own current-user precondition | Api If-Match, internal |
| ExpectedLoginContext | Уточняется [Q15](../questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities) | Да | Original tab actor context | Mismatch independent conflict, no actor body authority |

**DeactivateStaffUserCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| UserId | Guid | Да | Target staff account | Stable own identity |
| ExpectedVersion | int | Да | Own access-state precondition | Required If-Match |
| ExpectedLoginContext | Уточняется [Q15](../questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities) | Да | Original tab context | Fresh server identity always authority |

**ReactivateStaffUserCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| UserId | Guid | Да | Target staff account | Stable own identity |
| ExpectedVersion | int | Да | Own access-state precondition | Required If-Match |
| ExpectedLoginContext | Уточняется [Q15](../questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities) | Да | Original tab context | Fresh server identity always authority |

**IssueCredentialSetupCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| UserId | Guid | Да | Exact target | Admin only |
| Purpose | CredentialSetupPurpose | Да | InitialSetup or Reset | Provider distinct purpose; semantics not password-known-to-admin |
| ExpectedVersion | int | Да | Own target mutation precondition | First gate then fresh check |
| ExpectedLoginContext | Уточняется [Q15](../questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities) | Да | Original admin tab context | No arbitrary ActorId |

**CompleteCredentialSetupCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Code | string | Да | Ephemeral one-use provider code | No URL/logs/audit/secret body copy; bound action/user/purpose/generation |
| Password | string | Да | User-selected permanent password | Identity hashing/15–128/weak-password check; no admin knowledge |

SignOutStaffCommand has no business body: trusted current cookie/session and login-context precondition from adapter. Bootstrap uses same provision/setup rules through operator boundary; exact interface/proof/delivery Q15/Q24, no invented public route.

### 3.2. Queries

| Query | Назначение | Result |
| --- | --- | --- |
| GetCurrentStaffQuery | Current subject/profile from same fresh request snapshot | CurrentStaffDto |
| ListStaffUsersQuery | Admin directory | Safe paged StaffUserDto projection, carrier Q15 |
| GetStaffUserQuery | Exact own state | StaffUserDto + internal Version for own ETag |
| ListStaffRolesQuery / ListPermissionDefinitionsQuery | Fixed MVP catalog read | Fixed role/registered definition values |
| ReadAuthorizationSnapshot | Internal one coherent committed query | PermissionSnapshot, no private provider model |

GetCurrentStaffQuery and fixed catalog queries have no actor-selecting input. List/exact readers in IdentityAccess.EntityFramework.Persistence; request current snapshot reused by capabilities, not queried for every DTO/action.

**ListStaffUsersQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Parameters | StaffUserListParameters | Да | Server filtering/page/sort | No Product/team/created-by filter |

**GetStaffUserQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| UserId | Guid | Да | Exact own account target | Admin required |

### 3.3. DTO и parameter models

Exact JSON names/role serialization/context/header/setup result carrier remain Q15. Selected typed logical read values are immutable/nontracking; no Version/provider hash/stamp/secret/principal/EF entity escapes. CurrentStaffDto is user-dependent, not core mutation-validator payload.

**PermissionName (Authorization.Abstractions value)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Value | string | Да | BC-owned registered permission name | Globally unique; literal catalog Q15 |

**PermissionDefinition (Authorization.Abstractions immutable)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Name | PermissionName | Да | Stable registered definition | No client/server editor |
| DisplayName | string | Да | Safe definition label | Catalog localization/profile Q15 |
| Owner | string | Да | Defining BC/category | Registration metadata, not scope grant |

**AuthorizationMetadata (framework-free logical carrier)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| AllowAnonymous | bool | Да | Explicit public operation marker | Otherwise deny-by-default protected policy |
| RequiredPermissions | IReadOnlySet<PermissionName> | Да | Operation's coarse permission requirements | Composition/all-any exact metadata convention Q15; no AspNetCore dependency |

**PermissionSnapshot (Authorization.Abstractions immutable request value)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| UserId | Guid | Да | Fresh trusted provider identity | Not body/claim roles authority |
| SessionId | Guid | Да | Exact valid staff session | Expiry/revocation checked coherently |
| IsActive | bool | Да | Committed account state | Active+ready required |
| CredentialReady | bool | Да | Committed password readiness | No pending access |
| Roles | IReadOnlySet<string> | Да | Fresh committed role names, generic framework-free values | IdentityAccess maps fixed roles; no general BuildingBlock dependency on IdentityAccess enum |
| Permissions | IReadOnlySet<PermissionName> | Да | Derived registered fixed grants | Same coherent snapshot; no interrequest cache |
| ObservedAt | DateTimeOffset | Да | Server request check time | Not distributed/in-flight fence |

**ICurrentUser (safe immutable contract)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| UserId | Guid | Да | Authenticated authorized staff actor | Stable ID passed to business owners |
| SessionId | Guid | Да | Current session identity | Not credential; protected request only |
| Snapshot | PermissionSnapshot | Да | Single request current access | No unrestricted mid-command actor replacement |

**CurrentStaffDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | Guid | Да | Current verified user | Trusted snapshot |
| Login | string | Да | Safe immutable Login | Not secret |
| DisplayName | string | Да | Required display label | No profile CRUD |
| Roles | IReadOnlySet<StaffRole> | Да | Current fixed roles | Wire codec Q15; advisory |
| AllowedActions | IReadOnlySet<PermissionName> | Да | Current coarse allowed actions | User-dependent hint; exact resource actions belong owner's CapabilityResolver |
| LoginContext | Уточняется [Q15](../questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities) | Да | Nonsecret context to prevent cross-tab actor mismatch | Not credential/authorization; exact generation/carrier Q15 |

**StaffUserDto (safe own core read)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | Guid | Да | Staff identity | Stable actor |
| Login | string | Да | Immutable Login | Unique normalized |
| DisplayName | string | Да | Required label | No edit surface invented |
| IsActive | bool | Да | Committed administrative state | No optimistic success |
| CredentialReady | bool | Да | Safe setup status | No code/hash/stamp |
| Roles | IReadOnlySet<StaffRole> | Да | Own memberships | Same own Version mutation owner |
| CreatedAt | DateTimeOffset | Да | Creation time | Immutable |
| DeactivatedAt | DateTimeOffset? | Условно | Lifecycle time | Metadata repeat profile Q15 |

**StaffUserListParameters**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Search | string? | Нет | Safe Login/DisplayName search | Case/limits Q15 |
| IsActive | bool? | Нет | Lifecycle filter | Default Q15 |
| Role | StaffRole? | Нет | Known membership filter | No invented role |
| Page | Уточняется [Q15](../questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities) | Нет | Server pagination | Default/limits carrier open |
| Sort | Уточняется [Q15](../questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities) | Нет | Whitelist order | Stable Guid tie-breaker |

**StaffIdentityOptions (server configuration)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SessionLifetime | TimeSpan | Да | Default8h absolute | No sliding/refresh/RememberMe |
| SetupActionLifetime | TimeSpan | Да | Default20min one-use | Purpose/user/generation bound |
| MinimumPasswordLength | int | Да | Default15 characters | Identity/weak-password check |
| MaximumPasswordLength | int | Да | Default128 characters | Paste/no composition |
| MaximumFailedAttempts | int | Да | Default5 failures | Provider lockout |
| LockoutDuration | TimeSpan | Да | Default15min | Configurable defaults not measured SLA |
| LoginRateLimit | Уточняется [Q15](../questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities) | Да | Required rate-limit profile | Numeric limits/deployment engineering |

### 3.4. Ports

| Port | Назначение | Направление |
| --- | --- | --- |
| ICurrentUser / IPermissionChecker | Same request trusted identity/snapshot requirement check | General Authorization.Abstractions; no HttpContext |
| IPermissionDefinitionProvider / registry | Owning BC definitions/global duplicate validation | General framework-free registration |
| IAuthorizationSnapshotReader | One fresh coherent primary committed user+session+memberships+grants read | IdentityAccess public immutable values |
| IStaffIdentityProvider | ASP.NET Core Identity password/lockout/token/user/role adapter | Private IdentityAccess infrastructure; exact provider methods adapter-owned |
| IStaffSessionStore / ICredentialSetupActionStore | Own state coordination and query | Own EF.Persistence; no duplicate password/stamp repository |
| ILastReadyAdministratorReader | Guarded coherent count/existence of another ready administrator | Own primary IdentityAccess state |
| CapabilityResolver | PermissionSnapshot ∩ owned resource/domain state | Owning Application; advisory AllowedActions |
| TimeProvider / StaffIdentityOptions | Absolute deadlines/current time/configuration | Host |
| Safe activity intent F18 | Atomic own normalized activity intent | Foreign audit append after own commit |

Legacy logical ICurrentStaffContext references are adapted to ICurrentUser safe actor contract at composition boundary, never a second Identity store. Business BC receives no credential/provider-management API.

### 3.5. Specifications / read contracts

[S19](../standards/S19-specifications.md): atomic user-ID/active/known-role/safe-label predicates, same filters list/count, whitelist sort+stable Id; adapted provider query projection uses own EF.Persistence. Snapshot reader uses one coherent committed primary SQL/read scope for user/session/memberships/fixed grants, no torn multi-query ReadCommitted observations and no loaded old claims/long-lived cache. Exact SQL profile Q20; immutable result mapped once into request scope. Other BC reads no Identity tables/FK.

## 4. Application

### 4.1. Handlers / use cases

**StaffPasswordValidationEvidence (private Application value)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| UserId | Guid | Да | Exact provider user whose password was checked | Not arbitrary body actor |
| CredentialGeneration | long | Да | Generation captured with checked provider hash/state | Final own gate must still match |
| SecurityStamp | string? | Нет | Private captured provider stamp | Exact comparison at final guard; no DTO/log/audit copy |
| CapturedUserVersion | int | Да | Own Version captured with read-only verification basis | Fresh final guarded own precondition; drift re-evaluated, not stale provider save |
| PasswordVerified | bool | Да | Trusted read-only provider password verification outcome | False may require guarded conditional failure-count update; no raw password/hash |
| RequiresRehash | bool | Да | Successful verifier requests stronger hash | CPU preparation outside gate; rehash/provider save only inside own guarded transaction |



Authorization pipeline consumes framework-free metadata and one request PermissionSnapshot through IPermissionChecker. IdentityAccess command handlers use own provider adapter/session/setup stores; business handlers do not import AspNetCore/IdentityUser. Coarse permission check precedes use case; owner resource authorization and Domain invariants always repeat independently. CapabilityResolver uses the same snapshot for every resource DTO, no permission query per action.

SignInStaffHandler splits strictly read/CPU-only verification/evidence preparation outside gate from every provider write inside own guarded transaction. Captured StaffPasswordValidationEvidence binds checked SecurityStamp/credential generation/own Version; no provider auto-save during preparation. Final gate rereads provider active/ready/lockout/security state and own Version: drift aborts/re-verifies outside gate or conditionally reevaluates fresh state, never saves a stale provider object. Successful admission commits required rehash/provider stamp coordination and lockout/counter reset atomically with StaffSession bound to the resulting own committed generation; this may reflect only this transaction's authorized provider updates, never an intervening credential change. Reset/change racing expensive password verification cannot authorize old proof against new credential state; mismatch re-verifies outside gate or aborts. Cookie issuance outside transaction only after known committed exact Session. Temporary provider/database failure503, not InvalidCredential/revoke. GetCurrentStaffQuery reuses current snapshot; exact/list/catalog queries project private store to immutable read values in EF.Persistence.

CreateStaffUserHandler provisions provider user+fixed memberships+own Version+pending credential/setup coordination and safe F18 intent atomically. Permanent password never assigned/known by admin. Role/lifecycle/setup issuance handlers check fresh admin + ExpectedLoginContext, open own transaction first existing MVP global gate, then fresh target/provider memberships/Version and last-ready-admin guard. Deactivation/reset revoke old sessions in same own commit; Reactivate permits only new login after readiness, no session restore.

CompleteCredentialSetupHandler prepares strictly read-only provider proof and CPU/password hash work outside gate, without provider auto-save/counter/state update, then fresh own transaction/gate checks exact action/purpose/user/generation/expiry/not-consumed and provider prerequisites; atomic consume+provider password/stamps+ready/state/Version+old sessions+safe intent. No independently committed UserManager save followed by separate setup record commit. Provider method/transaction participation verified during implementation; atomicity mechanism is selected, exact SDK calls [Q20](../questions/Q20-persistence-and-migration.md#identityaccess-persistence-и-authorization-snapshots).

SignOutStaffHandler terminally revokes current session only, then clears API cookie outside commit; other sessions remain. Unknown commit outcome reconciles exact own session/action state, not invented success or blind recreate. Bootstrap is explicit operator workflow using same provision/setup invariants; repeat no new user. Emergency recovery distinct secured operator action, no startup backdoor.

### 4.2. Validators

[S18](../standards/S18-fluent-validation.md): LoginValidator, DisplayNameValidator, FixedRoleSetValidator, ExpectedLoginContextValidator, SetupProofShapeValidator, PasswordPolicyValidator compose named constraints. Login unique normalization checked by provider/index, not client lookup. Password15–128/paste/no composition/weak-password check; provider lockout defaults/config separate. No required Email/RememberMe/custom-role fields. Exact labels/normalization/context/codes [Q15](../questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities).

### 4.3. Mappers

ProviderUserReadMapper projects only safe user/core fields; no private Version/password/hash/stamp/cookie/setup code. PermissionSnapshotMapper derives fixed grants from same committed tuple once, generic role names in Authorization.Abstractions avoid dependency on IdentityAccess enum. CurrentStaffMapper maps trusted snapshot/profile/login context; API maps own Version to ETag, never JSON. CapabilityMapper composes advisory user-dependent AllowedActions from snapshot+owned resource state, no core Version bump. F18 mapper creates normalized safe intent before source commit, not foreign audit entity.

### 4.4. Services / algorithms

LastReadyAdministratorGuard evaluates proposed removal/deactivation/credential-readiness loss against fresh own active+Administrator+ready set under global gate; stale prior count no permission. Self action allowed if another exists; concurrent two admins cannot remove both. Bootstrap repeat reads original existing identity; no new duplicate first admin.

AuthorizationSnapshotFactory observes active+ready+unexpired/unrevoked same admitted credential-generation session+memberships+fixed registered grants once after cookie validation; request reuse only, never cross-request permission caching. Roles changes effective next request; already-checked in-flight may finish. Mutation login context is compared to fresh server identity/context before side effect, not used to choose actor. Permission and Domain denial remain distinct.

## 5. Api

### 5.1. Endpoints

Logical API surface below; exact URI/verbs/DTO/headers [Q15](../questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities). Standalone Blazor UI is independently hosted on another HTTPS origin under same base domain: different-origin, same-site subdomains. No mandatory reverse proxy/same-origin deployment. Browser credentials include; CORS exact configured UI origin, credentials allowed, never '*'.

| Method / boundary | Destination | Request | Response | Access |
| --- | --- | --- | --- | --- |
| HTTP command | Login | Login/password/context + antiforgery | Safe result + API-host cookie | Public login proof, rate limited |
| HTTP read | Current profile | Cookie | CurrentStaffDto | Current staff |
| HTTP read | Staff list/exact | Filter/Guid | Safe projection; exact core ETag | Administrator |
| HTTP command | Provision/roles/deactivate/reactivate | Selected models + required own If-Match where existing + context/antiforgery | Committed safe own result | Administrator |
| HTTP read | Fixed role/permission catalog | Read input | Server-fixed definitions | Scoped access [Q15](../questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities); no editor |
| HTTP command | Logout | Current context/antiforgery | Confirmed current logout + cookie clear | Current session |
| HTTP command | Issue initial/setup reset | Exact target/purpose/preconditions | Secure issuance delivery profile [Q15](../questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities) | Administrator |
| HTTP command | Complete setup/reset | One-use code/password + antiforgery | Safe confirmation | Provider purpose-bound proof |
| Operator | Bootstrap/emergency recovery | Secured manual operator input | Safe operational result | No public registration/bootstrap |

Cookie is host-only API Secure+HttpOnly, no parent-domain cookie; default SameSite=Lax for selected same-site deployment, no forced None. Antiforgery required for browser mutations including login/logout/setup/reset; auth cookie alone not CSRF protection. Exact antiforgery bootstrap/header/CORS origin configuration and cookie name [Q15](../questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities)/[Q20](../questions/Q20-persistence-and-migration.md#identityaccess-persistence-и-authorization-snapshots), not invented wire contract.

### 5.2. ETag / If-Match

Own user access Version covers provider user/core+memberships/coordinated state; exact core read and success mutation strong ETag, required If-Match→expected int, missing428/stale412/domain409. List not per-row token source. User-dependent AllowedActions/current profile may change without core Version: own mutation precondition is not validator for whole JSON with capabilities. Do not share-cache/reply304 such representation until [Q15](../questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities) carrier separates core token from entire representation. ExpectedLoginContext drift independent409, cannot fix by silently assigning actorB or fresh owner ETag.

### 5.3. OpenAPI и ошибки

Document selected cookie/CSRF+credentialed exact-origin CORS, anonymous login/setup proof, admin account/catalog surfaces and context/concurrency separately from Delivery Q16 runtime scheme.401 unauthenticated/expired session;403 authenticated permission/resource denial;503 temporary provider/database/snapshot failure;428/412 own preconditions;409 relevant context/last-admin/domain conflict. Exact machine codes/login privacy/lockout/rate-limit status/result recovery profiles [Q15](../questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities). No redirect/HTML200 replacing API challenge; no provider entities or secret proof echoed in ordinary response/log.

## 6. EntityFramework

### 6.1. DbContext ownership

IdentityAccessDbContext derives provider IdentityDbContext<ContentManagerUser,ContentManagerRole,Guid>; own schema identity. DbContext/configurations/migrations belong IdentityAccess.EntityFramework, not reversed illustrative source tree. Own users/roles/memberships/provider state+RolePermission+StaffSession+CredentialSetupAction+own normalized audit intents; no foreign Identity FK or other BC DbContext/UoW.

### 6.2. Configurations

| Entity | Table | Key/index | Concurrency | Mapping |
| --- | --- | --- | --- | --- |
| ContentManagerUser | Own identity user table; name [Q20](../questions/Q20-persistence-and-migration.md#identityaccess-persistence-и-authorization-snapshots) | Guid PK/unique NormalizedUserName | Own int Version coordinated provider ConcurrencyStamp | Required immutable Login/DisplayName/active/ready/generation; inherited hashes/stamps private |
| ContentManagerRole / membership | Own provider role/user-role tables | Guid role/user ownFK; unique membership | Provider concurrency coordinated own user Version | Fixed two roles; no custom editor |
| RolePermission | Own fixed role_permissions mapping | RoleId+registered Permission unique | Read-only MVP | Permission value conversion; BC-owned registry |
| StaffSession | Own session table | Guid PK/UserId+expiry lookup | int Version internal | Absolute CreatedAt/ExpiresAt/terminal revoke |
| CredentialSetupAction | Own setup/reset coordination | Guid PK/target+generation lookup | int Version internal | Purpose/generation/expiry/single consume/revoke; no plaintext code |
| Safe activity intent | Own source intent per F18 | Exact idempotency/profile F18 | Owning intent profile | Safe metadata only; audit append separate |

### 6.3. Constraints / indexes

Unique normalized immutable Login enforced SQL/provider; fixed known roles/grants reference own records only. Session/action userFK stays IdentityAccess. User+memberships own Version coherent despite provider's private stamps. One-use consume conditional update under own concurrency; same proof concurrent use has one committed winner. Last-ready-admin is guarded application invariant using fresh own read, not naive count-before-transaction or cross-BC constraint. Physical schema/index/profile [Q20](../questions/Q20-persistence-and-migration.md#identityaccess-persistence-и-authorization-snapshots).

## 7. EntityFramework.Persistence

### 7.1. Repositories / query adapters

Own provider store adapter coordinates ASP.NET Core Identity operations with IdentityAccessDbContext transaction. No duplicate custom password hash/security-stamp repository or Domain.StaffUser store. StaffSessionStore/CredentialSetupActionStore, primary AuthorizationSnapshotReader and list/exact/catalog query handlers live EF.Persistence. Public ports return immutable values/no Version/provider entities; scoped snapshot reused by general authorization adapters.

### 7.2. Transactions

IdentityAccess own ReadCommitted mutation transaction obtains existing stable global pg_advisory_xact_lock as standalone first statement on SAME owner connection before authoritative reads/locks, then fresh own user/provider/session/memberships/grants/setup action + own token/context/last-admin checks. Provider user/memberships/own Version/session/recovery state+normalized F18 intent commit atomically. No foreign ambient UoW/shared SQL transaction/audit append; exact provider enlistment is adapter implementation to verify, never two independent successful commits.

Outside-gate provider verification/evidence/hash preparation is strictly read/CPU-only: no auto-saving Identity validation/sign-in SDK call. Rehash/password/stamp/readiness, failure-count/lockout and success-reset writes belong the same fresh guarded IdentityAccess transaction as their own coordinated state. A failed attempt commits only its conditional counter/lockout transition on current provider state after captured generation/stamp/Version checks; mismatch does not overwrite latest count or attach old failure proof to changed credentials. Successful login commits its rehash/reset and Session together. Setup generation/action issuance/password preparation/DP keyring I/O outside gate then fresh under-gate recheck; cookie issuance/clear/operator output after commit. Acquisition bounded configured cancellation; provider/database failure technical503 not domain conflict/credential failure. Owner connection loss aborts, no reconnect continuation; lost ACK unknown → exact own session/action/user outcome reconciliation [Q15](../questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities)/[Q20](../questions/Q20-persistence-and-migration.md#identityaccess-persistence-и-authorization-snapshots). Last-admin check under new gate for every invalidating operation including reset/readiness transitions. Protected request read snapshot is one coherent committed read, not cross-BC write fence; already-checked in-flight allowed finish.

### 7.3. Migrations

Create own IdentityAccess schema/provider extension/fixed role grants/session/setup/intents with own indexes/concurrency. Use existing repo EF/Persistence placement; no production migration applied in docs task. Fixed registry/role bootstrap is server controlled; no seed password/newuser at startup. Exact DDL/initial role mapping/config deployment [Q20](../questions/Q20-persistence-and-migration.md#identityaccess-persistence-и-authorization-snapshots); illustrative source physical table names not binding.

## 8. External adapters

### 8.1. ASP.NET Core Identity / Authorization.AspNetCore / Host

Provider UserManager/RoleManager/password hashing/lockout/token services remain private infrastructure behind selected adapter. Stock SDK calls with possible writes cannot run in outside-gate validation: ASP.NET Core Identity v10 [UserManager.CheckPasswordAsync](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Identity/Extensions.Core/src/UserManager.cs) may rehash and save after successful verification; [SignInManager password checks](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Identity/Core/src/SignInManager.cs) may increment failed count/lockout or reset it. Adapter separates pure verification/CPU evidence from those writes, then enlists all provider persistence in own guarded transaction. Exact package/provider adaptation and auto-save suppression/integration profile Q20; no claim arbitrary SDK verification is pure. General Authorization.AspNetCore implements HttpCurrentUser, policy provider/requirement/handler and endpoint metadata adapters; Application metadata imports only Authorization.Abstractions, never AspNetCore/ClaimsPrincipal/HttpContext. Cookie validation+fresh snapshot occur once per protected request; full role/permission claim set not authoritative and not required in cookie.

Host orders actual authentication/authorization middleware and scoped request snapshot, registers provider own stores/user extension/options/session/action handlers, permission definitions/registry/checker/CapabilityResolvers, CORS credentials exact UI origin, antiforgery including login/reset and login rate limiter. Defaults8h/20min/5 failures15min/password15–128 options, no sliding/RememberMe/MFA dependency. Standalone same-site UI deployment preserved.

### 8.2. DataProtection / operational bootstrap / F18

Persistent protected keyring, shared among API replicas with same staff discriminator/versioned purposes; staff cookie/setup/reset purposes separated and distinct from Q16 Delivery replay. At-rest protection/restricted access required; physical volume/provider/config/key deployment [Q20](../questions/Q20-persistence-and-migration.md#identityaccess-persistence-и-authorization-snapshots). No ephemeral-container-only keyring, cookie/code URLs/logging/plaintext secret copies.

Manual operator bootstrap initializes first active ready-admin path via secure one-use setup, repeat no new user. Initial pending admin credential setup is completed before staff workload; emergency recovery separate operator workflow, no autostart seed password/public register/mail reset. Secure setup code delivery/bootstrap interface [Q15](../questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities)/Q24 engineering.

F18 source intent atomic inside IdentityAccess; asynchronous audit append and operational/security diagnostics remain separate. Other BC stores actor Guid without FK; no broker/VRS dependency merely to authenticate staff. Q16 runtime bearer/epochs/replay unrelated, Demo anonymous.

## 9. Data flows

IdentityAccess Api → framework-free authorization metadata/request snapshot → own Application/provider/EF.Persistence → atomic own state+safe intent → cookie/secure output after commit. Resource use cases continue in their own BC with trusted ICurrentUser and fresh resource/Domain rules.

| Поток | Транспорт | Метод / канал | Endpoint / destination | Доступ | Вход | Application action | Выход | Успех | Ошибки |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| DF01. Вход | Cookie browser HTTPS | Logical login command; URI Q15 | IdentityAccess login | Anonymous proof/rate-limit/antiforgery | Login/password/context | SignInStaffCommand | Safe result+API cookie | Own absolute session committed | Invalid proof/lockout/active readiness/temporary503 |
| DF02. Текущий сотрудник | Credentialed HTTP | Query | Current profile Q15 | Exact current staff | Cookie/session | GetCurrentStaffQuery | CurrentStaffDto/login context | Same fresh request snapshot |401/503; no old claims fallback |
| DF03. Каталог сотрудников | HTTP query | List/exact/fixed catalog | IdentityAccess readers | Admin/selected catalog permission | Filters/Guid | Own query handlers | Safe core/read definitions | Exact core ETag, catalog read-only |401/403/404/validation/read outage |
| DF04. Предоставить доступ | HTTP command | Provision Q15 | Staff accounts | Admin+CSRF/context | Login/DisplayName/fixed roles | CreateStaffUserCommand | Pending safe account/setup delivery outcome | Provider/membership/Version/setup/intent atomic | Duplicate/context/domain/unknown ACK |
| DF05. Роли / lifecycle | HTTP command | Own mutation Q15 | Exact target | Fresh Admin+CSRF/context | Guid/full roles/action/If-Match | Own access handlers | Safe confirmed core+ETag | Guarded last-ready-admin/coherent commit |428/412/409/403/503 |
| DF06. Выход | Credentialed HTTP | Logical logout Q15 | Current session only | Current context/CSRF | Trusted session | SignOutStaffCommand | Confirmed logout/cookie clear | Terminal revoke current; others remain | Unknown ACK/temporary failure |
| DF07. Первый админ / recovery | Manual operator | Secured operational interface Q15 | Own bootstrap/emergency | Explicit operator proof | Verified setup context | Own provision/setup workflow | Safe operational result | Repeat no new user, no seeded password | Missing proof/config/unknown outcome |
| DF08. Reauthentication формы | SPA memory + DF01/02 | Reauth without reload | Frontend form/owner API | Same confirmed actor | Local input/File/original baseline | No server autosave | Restored local form | Separate explicit save with original ETag/context | Different actor/current permission/resource conflict |
| DF09. Protected operation check | Server per-request | Cookie→snapshot→metadata→owner | Host/Authorization→own BC | Active+ready+valid session | Trusted material/operation | Snapshot factory/checker/resource guard | Trusted ICurrentUser or refusal | ONE coherent fresh snapshot/request |401/403/503; no cached permission fallback |
| DF10. Credential setup/reset | HTTPS proof + admin issuance | Logical issue/complete Q15 | Own setup boundary | Admin issuance / exact one-use proof+CSRF | Target/purpose/preconditions or code/password | Issue/CompleteCredentialSetupCommand | Safe setup confirmation | Atomic one-use/password/ready/stamps/session invalidation | Expired/used/wrong purpose/generation/conflict/503 |
| DF11. Capabilities/context guard | Request scope | Owned resource read/mutation | Own BC CapabilityResolver/pipeline | Fresh ICurrentUser snapshot | Resource state/original LoginContext | Advisory capability mapping/context check | AllowedActions or conflict | No extra snapshot per DTO; original actor bound | UI hint not permission, changed actor409 |

### DF01. Вход

1. Exact UI origin credentialed request/antiforgery; provider normalization/password policy/lockout/rate-limit verified, no secret logging. Pending/inactive user cannot log in.
2. Strictly read/CPU-only provider verification and evidence/hash/keyring preparation outside owner gate: no CheckPasswordAsync/SignInManager path that can auto-save rehash/lockout/counters. Capture exact generation/SecurityStamp/own Version. Final own transaction first gate, fresh active/ready/lockout/provider state and captured preconditions; success applies required provider rehash/lockout-counter reset + coordinated user Version/state + absolute8h Session atomically, bound to resulting own committed generation. If reset/change advanced generation/stamp while verification ran, abort or redo provider check outside gate; never stamp a Session with current generation after proving old hash. No RememberMe/sliding/refresh. Temporary provider/database failure503 distinct from invalid proof.
3. Failed verification records any required failed-attempt/lockout change only in a new own first-gate transaction conditional on captured security/Version basis and fresh counter state; concurrent newer provider state is preserved or checked anew, never overwritten by outside-gate prepared values. No failed Session/cookie; database failure remains503, not invented committed lockout.
4. Cookie protected by staff DP purpose issued only after committed session. Cookie is bound to exact known committed Session/generation; it carries minimum user/session identity, not authoritative roles/grants. Lost result reconciles chosen session/login outcome profile Q15, no false confirmed login.
5. Browser subsequent calls credentials include; host-only API cookie remains same-site across accepted HTTPS subdomains.

### DF02. Текущий сотрудник

1. Authentication validates cookie material and DF09 snapshot once; current query reuses same trusted UserId/session/snapshot, not body actor.
2. EF.Persistence projects required safe Login/DisplayName/current roles/login context; no hash/stamp/Version/principal/code.
3. User-dependent capabilities/context not shared-cached under core ETag. UI confirms same actor before exposing recovery form; expired401 and temporary503 differ.

### DF03. Каталог сотрудников

1. Coarse admin/catalog permission check from current snapshot; own reader applies server filters/list-count/sort stable Id, no per-content ownership filter.
2. Exact read maps safe user/memberships from own coherent state and exact core token; list/capabilities not source of own per-row ETag.
3. Fixed roles/registered permissions read-only MVP. No custom role/direct grant editor endpoint or provider private state.

### DF04. Предоставить доступ

1. Fresh server admin/context/CSRF checked; input Login/DisplayName/fixed roles, no permanent password from admin.
2. Own ReadCommitted transaction first global gate; fresh normalized unique Login/provider prerequisites. Create pending user/memberships/own Version/setup coordination and normalized F18 intent together.
3. Provider save participates same own SQL transaction; rollback leaves no authorized half-account. Commit then secure provider setup-code delivery outside SQL boundary, exact issuance/recovery contract Q15.
4. Safe account remains pending until DF10 successful one-use password setup; setup delivery failure is not a ready account or regenerated unrelated success. Unknown ACK reconcile own original state.

### DF05. Роли / lifecycle

1. Same request admin authority+original-tab ExpectedLoginContext; own transaction first gate then fresh target/memberships/provider readiness/Version. Missing428/stale412; changed actor independent409.
2. Evaluate proposed fixed-role/lifecycle/readiness transition against fresh other IsActive+Administrator+CredentialReady accounts. Concurrent operations serialize; self-deactivation allowed if another remains.
3. Update provider own state/memberships/Version and safe intent atomically. Deactivation terminally revokes old staff sessions; reactivation allows only new login, old sessions stay revoked.
4. Commit before output/audit dispatch; next protected request sees current memberships/state. Already-checked in-flight request may finish, no cancellation/rewrite of committed business work.

### DF06. Выход

1. Antiforgery/login-context boundary chooses exact trusted current StaffSession; no body selecting another user/session.
2. Own transaction first gate/fresh session; persist terminal revoke current session. Other staff sessions remain.
3. Clear cookie after own commit; local browser profile clearing alone not server proof. Unknown ACK inquiry exact own session; no content mutation/form persistence or Delivery credential revoke.

### DF07. Первый админ / recovery

1. Explicit operator secured manual invocation, no public register/bootstrap/autostart/seed password. Exact operator interface/proof and setup delivery engineering Q15/Q24.
2. Own normalized identity/provision transaction checks existing bootstrap identity; repeat no new user. Provider setup action gives user permanent password without operator knowing it.
3. Initial pending bootstrap permits zero ready users only in the explicit operator initialization phase; completing first setup establishes the ready Administrator before protected staff workload. Last-ready-admin guard prevents loss of an established ready account, not the initial creation of the first. Emergency recovery is a separate explicit operator operation, never a routine admin bypass or fresh bootstrap pretending original identity absent. Apply one-use purpose/generation rules and safe original-outcome recovery.
4. Safe success/logging contains no credentials/codes. Partial/unknown own outcome reconciled before new operation; setup not declared ready from local output alone.

### DF08. Reauthentication формы

1. SPA preserves typed input, File references, owner UserId/login context, route/exact resource and ORIGINAL baseline/ETag in memory while reauth overlay operates without page reload.
2. DF01/02 confirms same UserId; other-account input not shown/submitted. Closing/reloading/restarting tab not covered; no server snapshot storage/default secret persistence.
3. Fresh owner resource read confirms current access/state without overwriting local input/original baseline. Changed owner state explicit reconciliation, not fresh-token blind overwrite.
4. Explicit save only, sending original resource precondition and ExpectedLoginContext. No auto replay pending command; prior unknown mutation outcome queried separately.

### DF09. Protected operation check

1. ASP.NET adapter validates material/minimum user/session pointer; no role-claims authority. Missing/bad/expired material401; temporary provider/database503 not fake invalid credential or terminal revoke.
2. Own primary reader returns ONE coherent committed user active+ready, StaffSession expiry/revoke/admitted generation against current security generation, memberships and fixed registered grants; torn reads forbidden. Store unavailable never falls back claims/cache.
3. Request-scoped PermissionSnapshot/ICurrentUser reused by coarse metadata IPermissionChecker/capabilities. No interrequest cache or provider query per DTO/action.
4. Owner resource authorization/Domain invariants apply after coarse permission; known denial403. Business BC never touches IdentityUser/tables/HttpContext.
5. Subsequent request after committed role/state change sees new facts; in-flight already checked may finish. Request snapshot not global cross-BC commit fence.

### DF10. Credential setup/reset

1. Admin issuance verifies fresh permission/context/own Version/last-ready-admin guard under first own gate; target action/purpose/generation/20min default lifetime captured. Provider proof/keyring work outside gate with final generation recheck; secure delivery profile Q15.
2. Completion accepts ephemeral code/password via protected HTTPS body/antiforgery, never URL/log/body copy. Provider validates exact purpose/user/generation; prepare hashing/crypto outside gate.
3. NEW own ReadCommitted transaction first global gate; fresh provider/user/action/expiry/unconsumed/unrevoked/generation checks and concurrency. Atomically consume+provider password/hash/stamps+CredentialReady/state/Version+invalidate old StaffSessions+safe intent. One concurrent winner; no provider password commit followed by failed action consume.
4. Reset invalidates old sessions/actions through generation/state; restoration/new password requires new login. Temporary provider/DB failure503, not InvalidCredential/revoke assertion. Exact retry/unknown-ACK result/privacy profile Q15.

### DF11. Capabilities/context guard

1. Request snapshot is immutable generic Authorization value; resource reader projects owned current Domain facts and resolver computes intersection with registered permission requirements.
2. AllowedActions user-dependent advisory UX, not command token; no bump resource core Version for permission change, no shared304/cache until full representation-validator carrier selected Q15.
3. Mutation compares ExpectedLoginContext to fresh server login identity/context before side effect. A's tab after B's login returns conflict, never executes as B merely because cookie changed; server identity authoritative.
4. Actual command repeats permission/resource/Domain and own If-Match rules; capability denial and concurrency remain separate.

## 10. Ошибки и диагностика

| Ситуация | Принятый результат / сохранность |
| --- | --- |
| Missing/bad/expired staff session |401; no protected use case, form preserved in open SPA |
| Authenticated missing permission/resource refusal |403; no relogin loop/fake capability permit |
| Temporary provider/DB/snapshot failure |503; no cached permission allow or InvalidCredential/revoke fiction |
| Required own If-Match missing/stale |428/412, no mutation |
| Changed original-tab login context | Independent409; no A form command executed as B |
| Last ready-admin removal/deactivation/readiness loss | Conflict under own guarded fresh read; no concurrent zero-admin state |
| Duplicate normalized Login/invalid fixed role/password/setup proof | Typed validation/provider outcome Q15; no secret/guess account-privacy details |
| Setup expired/consumed/revoked/wrong purpose/generation | No password/state success; exact machine/privacy/retry profile Q15 |
| Own connection/commit ACK unknown | Abort/reconcile exact own outcome; no reconnect continuation/blind newtoken |
| Resource changed during reauth | Original content baseline retained, ordinary owner reconciliation |
| Another actor after reauth | Original input not exposed/applied; no silent actor switch |
| Missing operator/keyring deployment | MVP staff setup not ready; no default seed password/public bypass |

Diagnostics safe correlation/verified actor/target/session/action IDs and reason classes only, no passwords/hash/stamps/cookies/codes/DP payload/auth body/form snapshots. Security provider logs distinct from safe F18 business activity; detailed account-existence/lockout/rate-limit public messages/code profiles Q15.

## 11. Тестирование

[S04](../standards/S04-testing.md)/[S14](../../../catalog/docs/standards/dotnet/testing.md#test-environment-ownership-and-bounded-parallelism): unit framework-free permission registry/checker/CapabilityResolver/last-admin rules; real own PostgreSQL/provider integration for atomicity; functional API actual cookie/antiforgery/CORS; browser UI+API required for form recovery E2E. No InMemory substitute for SQL invariants, fake principal not proof login/session expiration.

| ID / поток | Уровень / SUT | Свои данные / воздействие | Ожидаемый результат |
| --- | --- | --- | --- |
| T01 / DF09,11 | Unit | Specialist/Admin/combined, registered permissions/resource states | No inheritance/owner-isolation; Domain refusal despite grant; hint no authority |
| T02 / DF09 | PostgreSQL/Functional | Stale cookie claims, committed role changes; count reader calls across many DTOs | ONE coherent fresh snapshot/request, no crossrequest cache/per-action queries |
| T03 / DF05,09 | PostgreSQL barriers | Change commit before next check versus already checked in-flight | Next request changed rights; prior request may finish |
| T04 / DF01,02 | Actual Identity API Functional | Login valid/invalid/pending/inactive/provider outage, VRS subject | Real cookie, readiness gate,401/403/503 distinctions, no source-key login |
| T05 / DF03,11 | Functional | Admin directory/catalog, direct unauthorized call, user-dependent AllowedActions/core token | Safe reads/catalog read-only; hint/core ETag separation/no shared304 |
| T06 / DF05 | PostgreSQL/HTTP | Same target concurrent admins/missing/stale If-Match/context |428/412/409 or coherent confirmed update, no actor substitution |
| T07 / DF04,10 | PostgreSQL/provider fault barriers | Provider user/membership/password save/action consume failure and lost ACK | One own atomic commit/no half-ready account, reconcile before retry |
| T08 / DF09 | PostgreSQL | Concurrent active/membership/session change during snapshot read | No torn tuple; unavailable503 no cached fallback |
| T09 / DF08 | Browser E2E | Dirty typed input+File, expiry, same-user overlay login | Open tab memory retained/original ETag, no reload/autosave |
| T10 / DF08,11 | Browser + owner API | Other-tab B login, concurrent resource change | A input not applied as B; context conflict/ordinary own reconciliation |
| T11 / DF06,07 | Provider integration | Two sessions logout one; bootstrap repeat/emergency boundary | Only current logout; no new bootstrap user/seed secret/public endpoint |
| T12 / F18 | PostgreSQL intent/audit integration | Success/failure setup/admin mutation | Intent atomic success only; separate audit append; no provider secrets |
| T13 / layers | Architecture | Assembly references/metadata/query adapters | No Domain→Identity/AspNetCore, no business→Identity EF; generic authorization no IdentityAccess enum dependency |
| T14 / stages | Functional startup | Demo no Identity seed; MVP actual setup | Demo anonymous independent; MVP needs actual selected provider/config |
| T15 / DF05,10 | Real PostgreSQL concurrent barriers | Two ready admins remove/deactivate/reset both, self action with another | Fresh first-gate invariant preserves one ready administrator |
| T16 / DF10 | Provider/PostgreSQL | Two consumers same code, expired20min, wrong purpose/user/generation | One atomic consume/password winner, no reuse/password known to admin |
| T17 / DF01,06,10 | Functional real clock/provider | Absolute8h/equality, no sliding/RememberMe, deactivate/reset/reactivate | Old sessions never restore, new login only, other current logout sessions remain |
| T18 / DF01,10 | Actual Identity adapter |15/128 boundaries/paste/weak password,5 failures15min/rate limiter | Selected configurable defaults; no composition/mandatory Email/MFA dependency |
| T19 / cookie/DF01,06,10 | Real two-origin same-site browser/API | credentials include/exact-origin CORS/antiforgery login/logout/reset; foreign origin | Host-only SecureHttpOnly/Lax API cookie; unauthorized CSRF/origin rejected, no wildcard |
| T22 / DF01 | Actual Identity/provider PostgreSQL transaction barriers | SuccessRehashNeeded, failed-attempt/lockout writes and concurrent reset/counter update before gate; rollback | No provider save before owner gate, current conditional counters preserved; rehash/success-reset/Session atomic, rollback no escaped provider write |
| T21 / DF01,10 | Provider/PostgreSQL deterministic barriers | Expensive old password verification races reset/new generation before session commit | Final unchanged generation/stamp check aborts or repeats outside gate; no old-proof new-generation Session/cookie |
| T20 / DP | Actual deployment integration | Shared protected persistent ring/two replicas/restart/purpose mismatch | Staff cookies/setup survive correct ring, staff/Delivery purposes isolated; secrets excluded |

Fixtures isolate own users/roles/sessions/actions/clock/barrier/database namespaces and clean own data. Real provider store transaction participation/password token services tested explicitly; mocks do not prove provider atomicity or browser same-site behavior. Tests not executed by this docs-only revision; no app/DB/browser/deployment/bootstrap run claimed.

## 12. Порядок реализации

1. Materialize selected IdentityAccess/Authorization boundaries and generic metadata/snapshot contracts; finalize exact wire/catalog/context/privacy profile Q15.
2. Own provider extension/EF schema+fixed roles/grants+session/setup/Version/intents and coherent primary read; verify provider enlistment/last-admin/one-use transaction with real PostgreSQL.
3. Application/provider handlers and snapshot/checker/CapabilityResolvers; scoped once-only authorization and owner-resource rules; no Demo dependencies.
4. Host cookie/CSRF/exact-origin credentialed CORS/rate limits/DP persistent shared protected ring for independently hosted same-site UI; actual configuration profiles Q20.
5. Operator bootstrap/secure setup delivery, standalone UI reauth memory/context precondition, API/OpenAPI verification before frontend generation.
6. Functional/provider/PostgreSQL/barrier tests then actual same-site browser/recovery E2E; no fake principal/mock proof of selected deployment.

## 13. Открытые технические вопросы

| Вопрос | Оставшаяся engineering граница |
| --- | --- |
| [Q24 accepted protocol](../../frontend/questions/Q24-admin-sign-in.md#принятый-протокол-identityaccess-и-восстановления-формы) | Mechanisms selected; exact secure setup delivery/operator/bootstrap/emergency interface and UI context codecs |
| [Q15](../questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities) | Exact URI/DTO/header/login normalization/limits/permissions catalog/antiforgery bootstrap/context carrier/representation validator/error privacy/retry profiles |
| [Q20](../questions/Q20-persistence-and-migration.md#identityaccess-persistence-и-authorization-snapshots) | Physical schema/Identity SDK transaction integration/coherent SQL snapshot/keyring provider/config deployment/provision unknown outcomes |
| [Q10](../../../business/questions/Q10-permissions-and-isolation.md) | BC-owned known action catalog; no new personal scope/editor |
| [F18](F18-staff-activity-history.md) | Defined normalized intent/envelope/idempotency/retention/security versus business journal boundary |

Selected mechanisms/defaults are not TBD: separate IdentityAccess/Identity Guid provider, absolute staff sessions, one-use setup, fresh snapshot, guarded last administrator, same-site independent UI, cookie/CSRF/DP and open-tab recovery. Exact physical/provider/wire profiles remain explicit engineering work.
