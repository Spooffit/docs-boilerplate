# F16. Вход и доступ сотрудников: фронтенд

[Бизнес](../../../business/features/F16-staff-access.md) · [Бэкенд](../../backend/features/F16-staff-access.md) · [M06](../modules/M06-staff-workflow.md).

## 1. Результат и границы

В MVP сотрудники собственной организации входят в отдельный IdentityAccess CM с ASP.NET Core Identity и Guid identifiers, независимо от VR School. Фиксированные системные роли Specialist и Administrator можно совмещать: Administrator сам по себе не даёт specialist authoring permissions. Специалисты работают со всем общим контентом без персональной области по продуктам/организациям. Админ предоставляет доступ, назначает фиксированные роли и выполняет [F17](F17-content-review.md). При обеих permissions self-approval допускается, four-eyes запрет не вводится.

UI — **Blazor Standalone**; UI и API независимо размещаются на разных HTTPS origins одного base domain, то есть same site. Browser transport включает credentials; API разрешает exact UI origin с AllowCredentials. Host-only API cookie Secure/HttpOnly/SameSite=Lax не читается SPA. Unsafe browser calls, включая login/logout/setup/reset, проходят CSRF protocol. Exact carriers и финальные routes — [Q15](../../backend/questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities). Миграция hosting model или same-origin proxy не требуются.

По [Q24](../questions/Q24-admin-sign-in.md#принятый-протокол-identityaccess-и-восстановления-формы) StaffSession живёт absolute 8 часов без sliding/refresh/RememberMe. Logout завершает current Session, остальные остаются. Deactivate/password reset инвалидируют старые sessions; Reactivate требует нового login. Сервер создаёт один fresh PermissionSnapshot на HTTP request без cross-request permission cache. Уже проверенный in-flight request может завершиться.

Восстановление actual unsaved content input обязательно для **того же сотрудника в той же открытой SPA-вкладке**. In-memory snapshot удерживает File objects, exact object/version, original baseline/ETag и pending operation context; reauthentication происходит внутри SPA без reload. Reload, закрытие вкладки и browser restart исключены из гарантии. Browser persistence/localStorage/IndexedDB, autosave и mutation autoreplay не вводятся.

В Demo нет входа, Identity, ролей и prerequisite current-staff read. F16 не управляет VR School customers/licenses и не заменяет runtime CmDeliverySession F13. Операторский bootstrap/recovery первого админа не становится frontend wizard. Архивный [источник](../../../archive/identity-access-architecture.md) объясняет архитектуру; его URI examples и альтернативы не заменяют принятые Q24/Q15 решения. Названия блоков — целевая декомпозиция, не доказательство готовых Razor-компонентов.

## 2. Страницы, экраны и навигация

| Экран | Точка входа и контекст | Состав и режим | Завершение |
| --- | --- | --- | --- |
| SC01. Вход | Первый protected MVP вход или session401; final route Q15 | SignInPage/SignIn: Login/password/pending/errors; reauth внутренний SPA режим | DF01, затем fresh DF02; current staff открывает разрешённый ReturnContext |
| SC02. Управление сотрудниками | Принятый /admin/users, management permission | StaffUsersPage: list, selected exact user, roles/lifecycle/credential-ready, read-only role/permission catalog | Confirmed mutation обновляет list/exact, refusal не success |
| SC03. Предоставить/изменить доступ | SC02 create либо exact staff Guid | StaffAccessForm: unique immutable Login при создании, required DisplayName, fixed roles, deactivate/reactivate, admin setup/reset issuance | Confirm/cancel → SC02, final wire/preconditions Q15 |
| SC04. Recovery формы | Исходный editor, owner Guid/exact IDs/original ETag | FormReauthentication + retained input/File references; locked/restored/conflict/denied | Same user получает ввод; explicit Save с current domain checks |
| SC05. Установить пароль | Полученный admin-issued setup/reset code | StaffCredentialForm: ephemeral code/password/pending/errors; сотрудник сам выбирает permanent password | Одноразовое consumption action; final routes backend F16/Q15 |

SC04 — состояние исходной страницы. Внутренний SignIn не уничтожает editor snapshot/File references. ReturnContext содержит только допустимый локальный маршрут и exact IDs, без credentials/code/token в URL. Недоступный exact объект не заменяется latest/newest. Explicit Logout или подтверждённый другой user очищают прежнюю работу.

Нет public registration, обязательного email, email confirmation/reset или MVP MFA. Setup/reset code выдаётся выбранным administrative/provider workflow; операторская передача и bootstrap относятся к backend Q24, UI не назначает email delivery либо новый wizard. Login/setup/reset URI examples архивного источника не утверждают final routes.

## 3. Карта FSD-блоков

| Уровень / проектный блок | Назначение и использование | Входы → выходы | Состояние / lifecycle | Зависимости |
| --- | --- | --- | --- | --- |
| app / StaffSessionCoordinator | Session identity/navigation/account boundary protected MVP | DF02/auth result/typed401/403/503/local identity notice → current staff/locked context/возврат | Один shell state; unknown скрывает content; another user/Logout clears recovery | CurrentStaff, SignIn/SignOut callbacks, public recovery boundary/transport |
| pages / SignInPage | SC01/ReturnContext | Local context → confirmed sign-in/refusal | Context у page, secrets у SignIn; no reload в SC04 | SignIn/shared fields/errors |
| pages / StaffUsersPage | SC02/exact selection/SC03 | List/exact/ETag → selected user/refreshed representations | Page owns list/selection/read, fences late previous-selection/account response | StaffUser read, StaffAccessForm, StaffCredentialForm/shared |
| features / SignIn, SignOut | Login/password/current-session Logout | Ephemeral credentials/exit intent → confirmed result/error | Action owns pending/errors; secrets не recovery; explicit Logout clears prior work | Generated F16/credentialed CSRF transport |
| features / StaffAccessForm | Admin provision/fixed roles/lifecycle/setup-reset issuance | Generated input/exact user/original ETag → target result/field/conflict/last-admin refusal | Local input/dirty/pending/errors; issuance secret не обычный profile | Backend F16 commands, StaffUser read, read-only server catalog |
| features / StaffCredentialForm | SC05 completion admin-issued setup/reset | Ephemeral code/user-chosen password → consumption/refusal | Secrets только action lifetime, cleared при завершении/закрытии; не snapshot | Generated F16 provider command/shared transport |
| features / FormReauthentication | Общий SC04 content editors | Owner Guid/actual input/File/exact baseline/pending context → restore/locked/conflict | Same open-tab owner-bound snapshot, не mutation queue | Public editor snapshot/restore, shell current identity |
| entities / CurrentStaff | Global identity/permissions read | DF02 generated representation → shell permission UX | Shell owns current read, не role authority/cross-request permission cache | NSwag/shared API |
| entities / StaffUser | List/exact profile/roles/lifecycle/credential-ready | DF03 representation → selected read | Read у page, editable inputs у form | NSwag/shared UI, не Domain/EF |
| shared / API transport, FormRecoveryState, fields/errors | Credentialed HTTP/CSRF/login-context, open-tab memory snapshot | Status/headers/typed snapshot → isolated state/events | No browser persistence/auth secrets; metadata рядом с DTO | Public lower-layer boundary; no business orchestration/features dependency |

Отдельный widget не требуется: композиция у StaffUsersPage/исходной content page. DTO не копируются в ручные business ViewModel. FormReauthentication переиспользуют [F01](F01-create-entity-schema.md), [F02](F02-create-schema-version.md), [F04](F04-entity-instances.md); F16 не переписывает их editor rules.

## 4. Логическая компоновка

```text
MVP shell [StaffSessionCoordinator]
  Navigation [CurrentStaff global permissions, SignOut]
  SC01. SignInPage / внутренний SignIn при reauthentication
    Login / password / pending / errors
  SC02. StaffUsersPage
    StaffUser list → selected exact StaffUser
    SC03. StaffAccessForm
      Login / DisplayName / fixed roles / lifecycle / setup-reset issuance
    Read-only role/permission catalog
  SC05. StaffCredentialForm
    Ephemeral code / new password / errors
  Исходный content workspace [его F-документ]
    Editor [actual input, exact context, original baseline]
    SC04. FormReauthentication [locked/restored/conflict]
      FormRecoveryState [in-memory, File references, no auth secrets]
```

| Связь | Передаваемые сведения | Координатор | Результат |
| --- | --- | --- | --- |
| SignIn → shell → page | Confirmed result/fresh DF02/local ReturnContext | Shell | Same-user restore либо новый user с cleared prior snapshot |
| List → selected user → form | Guid/exact read/coreETag/намерение | StaffUsersPage | Exact action без list-token substitution |
| Management/credential action → page | Confirmed target/result/refusal | StaffUsersPage | Fresh list/exact, не optimistic permission grant |
| Editor → recovery → editor | Actual values/File/owner/exact IDs/original baseline/pending context | Page/shell callbacks | Ввод восстановлен; fresh GET отдельный, baseline не заменяется |
| Account notice/typed denial → workspace | Identity change или auth uncertainty | Shell/page | Locked/cleared по причине, no cross-account apply |

SignIn и editor не обращаются к внутренностям соседнего feature. UI-вложенность не создаёт обратной зависимости shared. Auth unknown закрывает content view, сохраняя isolated same-owner snapshot; это не доказанный revoke и не основание login loop.

## 5. Зависимости и публичные границы

CurrentStaff permissions отвечают за global navigation/action permission. Resource DTO AllowedActions — advisory пересечение permissions с current owner/resource/domain rules. Missing permission скрывает действие; известный state restriction внутри доступного workspace показывает disabled с объяснением. Frontend guard/AllowedActions не authorization token: backend выполняет fresh permission/resource checks, Domain authority своих invariants.

Server registry/fixed role permissions read-only; admin назначает роли user, role editor/custom roles/direct user permissions не добавляются. PermissionSnapshot один на request, не DB lookup на каждую кнопку/DTO. UI может устареть после role commit; следующий request читает current permissions, уже проверенный in-flight может закончиться.

Actions общаются public parameters/callbacks; generic FormRecoveryState не определяет navigation/permissions/schema rules. IdentityAccess/Core Identity/Authorization building blocks раскрываются один раз в backend F16; browser не зависит от Domain/EF/ASP.NET Identity entities. Staff cookie не используется runtime Delivery/Demo.

## 6. Данные и владение состоянием

| Данные | Источник | Владелец / жизнь | Обновление / сброс |
| --- | --- | --- | --- |
| CurrentStaff Guid/global permissions/session metadata | DF02/DF09 | Shell read state | Login/explicit read/refusal; account switch/Logout clears prior context |
| Staff list/exact roles/lifecycle | DF03/confirmed result | StaffUsersPage | Selection/confirmed mutation; late account reply fenced |
| Own staff/core ETag | Exact concurrency header | Рядом с generated read/edit model | Original baseline; list/me не заменяют token |
| Resource AllowedActions | Owner DTO/capability resolver | Resource read representation | Fresh read; advisory, не mutation permission |
| UnsavedInput/File objects | Actual editor input | Editor + open-tab memory snapshot | До internal reauth; same-user restore; Logout/other user clears; reload/close loses |
| OriginalBaseline | Owner Guid/exact IDs/original ETag/base | Editor recovery context | Не заменяется fresh GET после login |
| CurrentResource | Fresh same exact GET | Page отдельно от baseline | Current access/lifecycle/data/conflict |
| PendingOperationContext/outcome | Submitted intent/observed result | Action/recovery state | До inquiry unknown outcome, не replay queue |
| Credentials/code/password | Ephemeral credential form | Только action | Никогда в recovery/diagnostics/browser persistence |

Login unique immutable, DisplayName required, email не mandatory. Password15–128 по provider policy без composition rules; weak-password checking обязателен, paste поддерживается. Lockout default5 failed attempts/15min configurable; exact rate limits/deployment Q24/Q20. Setup/reset code одноразовый20min, purpose/user/security-generation bound, consumption atomic. Сотрудник сам выбирает permanent password при completion admin-issued setup/reset action; отдельный authenticated self-change use case не вводится. Reset/deactivate old sessions не resurrects при Reactivate.

Fresh exact GET не подставляет новый ETag к старому input. Changed baseline → conflict; unchanged baseline может пройти original If-Match при explicit Save. Универсальный force/merge не вводится. Core own ETag не whole-JSON/capability cache tag: AllowedActions/current permissions могут меняться независимо от coreVersion. Representation carrier/cache validators Q15; staff/capability data не shared-cache и не reuse другого account.

Между вкладками используются local identity notices без snapshot/secret transfer. Mutation expected-login-context precondition отклоняет A-input под B-cookie; carrier Q15, не credential/authority. Unknown auth/provider store locks content, не guessed revoked state. Snapshot не содержит passwords/cookies/tokens/auth headers/setup-reset codes. Browser localStorage/sessionStorage/IndexedDB или server autosave не назначаются.

## 7. Действия и клиентские потоки

| UI | Запуск / исполнитель | Backend | Результат |
| --- | --- | --- | --- |
| UI01 Login | SC01 Login/password, pending duplicate blocked | DF01 затем fresh DF02; credentialed HTTP/CSRF | Только confirmed staff открывает разрешённый возврат |
| UI02 Staff list/exact | StaffUsersPage exact Guid | DF03/DF09 | List/exact/coreETag либо empty/denied/notfound |
| UI03 Provision | Admin StaffAccessForm | DF04 unique Login/required DisplayName/fixed roles | Confirmed user, refreshed list/exact, duplicate/field error |
| UI04 Roles/lifecycle | Exact target/original ETag | DF05/LastActiveAdminCredentialReady guard | Confirmed либо412/428/last-admin refusal; another ready Admin допускает self-deactivation |
| UI05 Setup/reset | Admin issuance либо StaffCredentialForm completion | F16 DF10, exact models Q15/backend | Одноразовый code/user-chosen password result; reset invalidates old sessions, secret не profile |
| UI06 Logout | Explicit SignOut | DF06 current Session | Prior local snapshot cleared; остальные sessions остаются; unknown не claimed success |
| UI07 Form reauth | Session401/expiry в editor | DF08 + DF01/02 + owner exact GET/DF09 | Same owner actual input/original baseline, отдельный Save |
| UI08 Access refresh | Login/explicit read/refusal/account notice | DF02/09 + owner DTO | Current UX; no relogin loop на403/inactive |
| UI09 Role/permission catalog | Staff admin workspace | Read-only backend registry/mapping | Диагностика, без role-edit mutation |

Editor фиксирует actual values/File references/owner/exact context/original ETag и pending context до internal SignIn. Unknown mutation остаётся Unknown: inquiry выбранного owner contract прежде retry. Login+fresh DF02 same Guid разрешают restore; different Guid clears prior state. Page читает тот же exact object/version, current lifecycle/access/data показывает отдельно. User явно выполняет Save по original baseline с fresh domain checks; новый ETag не маскирует conflict.

Cancelled/failed login в открытой вкладке сохраняет isolated same-owner snapshot. Inactive/forbidden не запускает login loop. Explicit Logout clears snapshot как намерение account exit, unknown server outcome отдельно объясняется. Bootstrap/операторская recovery не self-registration. Last-ready-admin refusal показывается явно; UI не угадывает достаточность другого Admin по stale list.

## 8. Использование API и NSwag

| Потребность | Серверная связь | Использование |
| --- | --- | --- |
| Login/current identity | [Backend F16](../../backend/features/F16-staff-access.md) DF01/02 | SignIn/shell generated inputs/results, no invented URI |
| Staff list/exact | DF03 | Page exact ETag отдельно от DTO/current capabilities |
| Provision/roles/lifecycle | DF04/05 | Form current permission + original owner precondition |
| Logout/operator recovery | DF06/07 | Current Session exit; bootstrap не frontend wizard |
| Reauth/protected calls | DF08/09 + exact owner feature | Local recovery; server auth/resource checks, no autosave |
| Setup/reset / catalog / capability context | Backend F16 DF10 / DF03 / DF11 | StaffCredentialForm/public catalog/context boundary; модели backend, UI не копирует DTO |
| Cookie/CSRF/capabilities/login-context | [Q15](../../backend/questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities) | Shared NSwag credentials include/antiforgery/preconditions; exact carriers before integration |

Generated files не редактируются вручную. ETag/headers/UI state рядом с generated DTO; code/password/cookie не listing fields. Final routes, error precedence/header names/provider bodies/AllowedActions representation Q15: source URI examples не contract. Login/replay/provider replies не public cache, logs исключают credentials/body/headers secrets. Client status401/403/503 сохраняется для shell без guessed provider result.

Antiforgery request token UI получает через API response contract и возвращает header на unsafe request; transport обновляет его после login/identity switch. SPA не читает HttpOnly API cookie через DOM и не требует shared Domain cookie. Token/bootstrap/header names Q15; credentials include и CORS не заменяют CSRF. Request token не content recovery snapshot.

## 9. Состояния, ошибки и доступность

| Состояние | Представление | Действия / данные |
| --- | --- | --- |
| Pending login/current-staff | Проверяется вход/доступ | Protected actions закрыты до confirmed identity |
| Generic invalid login401 | Не удалось войти | Исправить credentials, не раскрывать existence/Login state |
| Session401 в editor | Нужен повторный вход | Internal SPA SignIn, retained input/File/exact baseline |
| Known permission/inactive403 | Объяснимый refusal | No login loop/optimistic grant; content locked по scope |
| Provider/auth-store503/unknown | Доступ временно не подтверждён | Safe retry/read, snapshot isolated; не guessed Revoke/invalid password |
| Missing permission/state blocked | Hidden / disabled с причиной | Advisory UX, backend refusal всё ещё возможен |
| Last ready Admin refusal | Нельзя оставить без active credential-ready Admin | Input retained, no force |
| Setup/reset expired/used/wrong scope | Typed refusal | Consumed code не новая операция, secret lifecycle отдельно |
| Own412/428/resource changed | Baseline conflict | Old input/ETag retained, fresh read отдельно, no blind overwrite/latest substitution |
| Unknown mutation/Logout | Результат не подтверждён | Selected inquiry, no autoreplay/claimed rollback |
| Other user/explicit Logout | Prior context cleared | No чужого snapshot/File/callback, late responses fenced |
| Reload/close/restart | Recovery прекращено | Нет persisted draft guarantee |

Клавиатурный ввод/paste допускаются; поля имеют понятные подписи/ошибки, state не только цветом. Фокус переводится в internal SignIn/credential form и обратно в retained editor. CSS/modal/poll interval/test library не назначаются.

## 10. Проверки и приёмка

| Сценарий | Воздействие | Наблюдаемый результат / граница |
| --- | --- | --- |
| Demo | Open Home/content без auth | Нет login/Identity prerequisite |
| Independent origins | HTTPS UI/API subdomains, credentialed/CSRF calls | Exact-origin CORS/host-only cookie; other origin/unsafe без CSRF rejected, no forced proxy |
| Roles/capabilities | Specialist/admin-only/combined/resource block | Missing permission hidden, state disabled explained; direct API rechecks; admin-only no authoring |
| Permission freshness | Commitroles/deactivate before next request versus already checked request | Next snapshot current; earlier maycomplete; no cross-request cache |
| Sessions |8h expiry/read/Logoutcurrent/reset/deactivate/reactivate | No sliding/refresh/RememberMe; others survive current Logout; invalid old sessions not restored |
| Provision/credentials | Duplicate Login/missing DisplayName; setup/reset20min/used; password15–128/paste/lockout | Typed outcomes, one-use scope/generation, no mandatory email/public registration |
| Last ready Admin | Deactivate/remove last ready Admin; self with another | Server guarded refusal либо allowed confirmed change |
| Open-tab recovery | Actual changes/File objects/session401/same user | No reload, input/files/exact IDs/original ETag restored, explicit Save |
| Resource drift | Original A, fresh GET B/deleted/readonly | A remains baseline, conflict/denial, no B blindoverwrite/latest |
| Accounts/tabs | A-input/B-login/cookiechange/notice/late A mutation | Prior snapshot cleared; A under B precondition refusal plus actual authz |
| Unknown auth/outcome |503 identity/lost mutation ACK during reauth | Locked isolated form, no guessed revoke/login loop/autoreplay |
| Recovery boundary | Explicit Logout/reload/close/restart | Memory cleared/lost, no persistence promise |
| Representation | Permission/capability changes with same core ETag | Core concurrency separate from wholeJSON/cache; no shared cached capabilities |
| Late replies/accessibility | Selection/account change during request, keyboard errors/return | No old result in new context, understandable focus/errors |

Component/API doubles проверяют composition/state, не cookie/CORS/CSRF/provider/SQL concurrency. Нужны real backend/browser checks independent origins/Identity/one-use code/session expiry/last-admin/open-tab File recovery. Это критерии будущей реализации; проверка документа не runtime test.

## 11. Последовательность изменений и открытые вопросы

Materialize backend IdentityAccess/provider/session/permission/capability contracts/OpenAPI/Q15 carriers. Подключить credentialed/CSRF NSwag transport, shell current identity, staff/credential actions и local expected-login-context handling. Editor snapshot/restore в той же реализации обеспечивает mandatory same-user open-tab recovery с original baseline/File/unknown outcome. Затем real independent-origin/browser/server acceptance; hosting migration/mass forms rewrite не требуются.

| Вопрос | Остаточная инженерная детализация |
| --- | --- |
| [Q24](../questions/Q24-admin-sign-in.md#принятый-протокол-identityaccess-и-восстановления-формы) | Provider/normalization compatibility, exact rate-limit/deployment/operator instructions; session/roles/recovery semantics закрыты |
| [Q15](../../backend/questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities) | Final wire/routes/CSRF/login-context/error precedence, credential signatures/coreETag-capability representation |
| [Q20](../../backend/questions/Q20-persistence-and-migration.md#identityaccess-persistence-и-authorization-snapshots) | Physical Identity/storage/DP/registry/session registrations, atomic code consumption/security generation/last-admin guard |
| [Q13](../questions/Q13-ui-scope.md) | Общая navigation/list composition; /admin/users принят, новые credential routes не выдумываются |

[M06](../modules/M06-staff-workflow.md) связывает F16/F17/F18, не копирует их FSD/DTO. История выбора остаётся в Q/архиве, feature описывает текущую принятую цель.
