# Q24. IdentityAccess, вход сотрудников и восстановление формы

## Основание и границы

[Архитектурный источник IdentityAccess](../../../archive/identity-access-architecture.md) сохранён без изменений. Пользователь согласовал предложения 1–21, кроме первоначального пункта 8 о hosting: **Blazor Standalone UI и API — отдельные независимо размещаемые приложения на поддоменах одного базового домена**. Один origin/общий reverse proxy, Blazor Server/Web App/BFF и изменение frontend hosting mode не являются выбранными зависимостями.

IdentityAccess — отдельный MVP BC CM для внутренних сотрудников: ASP.NET Core Identity, Guid IDs, browser cookies и RBAC. Unity/Launcher licenses/Delivery Q16 независимы; Demo anonymous и не зависит от IdentityAccess. M06 связывает F16 с Publishing moderation F17 и technical Audit F18 без объединения persistence/transactions.

[Backend F16](../../backend/features/F16-staff-access.md) определяет полные модели/поля/handlers/layers/DF; [Frontend F16](../features/F16-staff-access.md) — страницы/guards/recovery. [Q15 contracts](../../backend/questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities) · [Q20 storage](../../backend/questions/Q20-persistence-and-migration.md#identityaccess-persistence-и-authorization-snapshots) · [Q10 roles](../../../business/questions/Q10-permissions-and-isolation.md).

## Принятый протокол IdentityAccess и восстановления формы

### Ownership и authorization

Infrastructure ContentManagerUser : IdentityUser<Guid> и ContentManagerRole : IdentityRole<Guid> принадлежат IdentityAccess. Не создаётся parallel Domain.StaffUser/password/roles/security-stamp store. Domain минимален для реальных правил; provider models не экспортируются business BC. Actor Guid хранится у других owners без FK в identity.users.

MVP имеет фиксированные системные Specialist/Administrator, combined допустим, implicit inheritance отсутствует. Админ назначает роли сотрудникам; code-based registry/серверный profile задают role definitions и начальные RolePermission. Таблица grants сохраняется, но arbitrary role CRUD, редактор permissions и direct user grants отложены. Предварительные role endpoints исходника не расширяют объём MVP.

Owning BC публикует permission definitions через providers; Host собирает registry. Authorization.Abstractions содержит ICurrentUser, IPermissionChecker, immutable PermissionSnapshot и CQRS authorization metadata без Identity/HTTP dependencies. Authorization.AspNetCore — adapters/requirements/handlers/endpoint attributes. Attribute Application command берётся из Abstractions, не ASP.NET assembly.

User-facing command/query явно имеет requirement либо public classification. Trusted background/source context не фиктивный Administrator. Coarse authorization предшествует business use case; resource checks и Domain invariants — у владельца фичи. Already-checked in-flight request может завершиться после изменения прав; cross-BC commit-time staff revoke fence не вводится.

PermissionSnapshot вычисляется **один раз на HTTP request из fresh coherent committed Session/account/roles/grants**, затем используется локально для всех DTO/actions. Interrequest permission cache и stale issued role claims как authority отсутствуют. Store unavailable — technical refusal, не empty permissions/InvalidCredential.

Capability resolver пересекает permissions и resource rules. AllowedActions в DTO — advisory snapshot, не token/permit. Missing permission скрывает действие; temporary business unavailability может оставлять disabled action с объяснением. Mutation заново проверяет доступ/own preconditions. Own concurrency token отделяется от whole-JSON representation/cache validator: AllowedActions меняется без resource Version. No shared capabilities cache/stale304 только по core numeric token; exact carrier Q15.

### Независимые UI/API origins

Иллюстративно https://cm.example.com и https://api.example.com — different origins, same schemeful site при HTTPS/общем registrable domain. Это не назначенные production hostnames. API не обязан проксироваться через UI /api и не раздаёт UI. Cookie **API host-only**, Secure/HttpOnly, SameSite=Lax для выбранной same-site схемы; Domain=.example.com не задаётся. Cross-site hosting требует отдельного пересмотра, SameSite=None не включается автоматически.

Browser API transport явно включает credentials, в том числе login; CORS разрешает exact configured UI origin(s), credentials и нужные методы/headers без wildcard/reflection. Preflight не требует staff cookie. CORS не заменяет CSRF/auth; policy корректно учитывает Origin. [CORS](https://developer.mozilla.org/en-US/docs/Web/HTTP/Guides/CORS), [credentials](https://developer.mozilla.org/en-US/docs/Web/API/Request/credentials), [cookie scope](https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Set-Cookie).

Antiforgery обязателен для state-changing browser requests, включая login/logout/account setup/reset. UI получает request token от API через согласованный response contract и возвращает header, не читает HttpOnly API cookie и не требует shared Domain cookie. Login/identity switch обновляет token; соседний same-site subdomain не получает доверие автоматически. URI/header names/DEV HTTPS origins/credentials handler выбираются Q15/deployment.

Persistent Data Protection keyring restricted/protected at rest; replicas совместимы по shared keys/discriminator. Staff cookies и Delivery replay имеют разные purposes/schemes. Physical path/provider/certificate/secret registration не выбираются Docker-volume примером.

### Staff sessions и defaults

Cookie содержит минимальный trusted identity/session context без full permissions. StaffSession хранится IdentityAccess: server SessionId/UserId, original time/absolute expiry/terminal revocation. Cookie cryptography/format выполняет ASP.NET Core, не custom JWT issuer. Session/account/provider stamp проверяются при protected request; restore не оживляет terminal session.

| Server option | MVP default | Constraints |
| --- | --- | --- |
| Absolute staff session lifetime | 8 часов | No sliding/refresh/Remember me; reauth new Session |
| Setup/reset code lifetime | 20 минут | One-use, purpose/user/security generation binding |
| Password accepted length | 15–128 символов | Identity hashing, no mandatory mixture; Unicode/length codec Q15 |
| Account lockout | 5 failed attempts / 15 минут | Provider options; не administrative deactivation |

Defaults configurable/startup validated, не measured SLA. Weak-password checking/paste поддерживаются; сотрудник выбирает permanent password. Login rate limiting обязателен, deployment numbers — Q22. MFA, email confirmation/delivery/reset by email и OIDC отложены.

Logout закрывает текущую Session/очищает browser cookie; другие устройства остаются. Deactivate и credential reset инвалидируют previous staff sessions; Reactivate требует new login. Role changes — next fresh check; уже проверенный in-flight request может завершиться. Staff change не license/Delivery revoke и не installed-data delete.

Password evidence привязано к exact captured provider stamp/credential generation. Final guarded Session commit требует неизменной basis; reset во время expensive password check не разрешает старому proof новую Session. Внешняя preparation строго read/CPU-only, без auto-save Identity validation; rehash/failed-attempt/lockout/provider security writes координируются own guarded transaction по Q20, а cookie выпускается после известного committed результата.

### Provisioning, setup, bootstrap и last-admin guard

Login unique/immutable, DisplayName required, email optional. Exact normalization/case/format/limits, display-edit scope/wire — Q15. User создаётся админом с выбранными system roles и не готов к обычному login до setup. Public self-registration отсутствует; админ не назначает постоянный пароль.

One-use provider setup/reset code связан с exact user/purpose/security generation/deadline. Completion атомарно потребляет action и сохраняет provider password/security/access state в own IdentityAccess transaction; stale/expired/consumed code не grant. Provider hash/stamps/token mechanics внутри adapters, parallel credential store запрещён. Reset не reactivate-ит inactive account/старые sessions.

Setup/recovery организует админ без обязательного email. Secrets не в URL/logs/read DTO/audit. Secure code выдача/передача и unknown-outcome inquiry — Q15/Q20: lost response не значит отсутствующий user/failed password commit, safe outcome readback до новой операции, no blind duplicate provision/replay mutation.

Первый Administrator — явная manual operator bootstrap command с setup, no startup auto-create/default password/secret в migrations. Повтор не создаёт второго user; durable original identity/outcome восстановимы. Emergency recovery — отдельная операторская операция, не public bypass; exact CLI/interfaces Q15/Q20.

Нельзя выполнить operation, оставляющую ноль IsActive + Administrator + CredentialReady users. Concurrent deactivate/role replacement/readiness changes заново проверяют guard внутри защищённой own IdentityAccess transaction по MVP consistency guard; partial provider/membership/access commit запрещён. Self-deactivate допустим при другом таком админе. Temporary provider lockout не permanent IsActive transition.

До первого successful setup initial pending bootstrap ещё не имеет ready Administrator: это отдельная операторская initialization phase, не обычный admin workflow. Она должна завершиться до protected staff работы; правило предотвращает потерю последнего уже готового админа, а не запрещает создание первого. Repeat bootstrap использует первоначальную durable identity; emergency recovery не выдаётся за повтор ordinary initialization.

### Recovery незавершённой формы

**In-memory в открытой SPA-вкладке**, internal reauth без reload. Snapshot содержит actual input/File objects, original owner UserId, exact object/version, original baseline/ETag и pending-operation context. Auth/password/setup secrets исключены. Persistent localStorage/sessionStorage/IndexedDB, autosave/offline queue не вводятся.

Гарантия действует пока живёт SPA instance; reload/закрытие вкладки/restart браузера исключены явно. Snapshot не evict-ится по cookie expiry до завершения принятого recovery. Same user → fresh me/access/exact resource read, затем восстановление original input/basis; new ETag не заменяет old baseline. Changed/gone/inaccessible/read-only resource требует feature-specific conflict/отказа без latest/ForceSave. Restore не autosave и не mutation replay; unknown previous command outcome устанавливается перед explicit повтором.

Different UserId либо explicit Logout очищает прежний snapshot, новый actor его не видит/не применяет. Local tab identity notifications помогают закрыть старый контекст. Mutation проверяет expected login context against trusted current session, чтобы запрос/форма A не выполнились от B после cookie switch. Это precondition, не credential/ActorId override. Exact carrier/stale-context reason Q15; notification alone не server guard.

## F16: техническая модель доступа и восстановления

Anchor сохранён. Полные модели и слой реализации — owner backend F16; public current-user/permission/capability read values immutable, no provider entities/internal Version/hash/stamp/cipher. Exact URI/DTO/status/casing/headers Q15; preliminary source API не автоматически опубликована.

401 invalid/expired authentication и403 known access refusal различимы; exact inactive/revoked precedence/login messages Q15.503 store/provider unavailable не InvalidPassword/empty permissions. UI не relogin-loop при confirmed inactivity/permission denial; CORS/network failure — Unknown transport result, не доказанный logout/failed mutation. API не выдаёт HTML login как successful JSON.

## Developer tooling: доступ CLI

[Q31 CLI](../../backend/questions/Q31-schema-code-generation.md#mvp-cli-конфигурация-и-файловый-workflow) согласовал staff/developer Registry access local tool cm-schema, без runtime activation keys/DeliverySession. Credentials не попадают в content-manager.json, lock/raw inputs, command arguments или diagnostics. Browser staff cookie/Blazor standalone flow не определяет вход консольного инструмента.

**Уточняется отдельным блоком:** конкретный console authentication/authorization flow, credential provider/storage/lifetime/revocation и noninteractive CI identity/permission scope. Этот ответ не выбирает PAT/JWT/OIDC/device login/token endpoint или новую staff form. Exact provider/wire механизм нужен до protected online restore; offline generate/check используют закреплённые локальные inputs. Реальный HTTP/access тест CLI обязателен, existing UI login не его proof.

## Приёмка и открытая инженерная детализация

Real PostgreSQL/HTTP/browser: fresh coherent permissions once/list, absent direct-grant/role-editor routes; Session logout/expiry/reset/deactivate/reactivate/no resurrection; last-admin/provision/role/setup races; half-commit/unknown ACK/durable bootstrap recovery; actual cross-origin same-site credentials/preflight/CSRF/token rotation; persistent keyring API restart/replica switch; actor-switch request fencing/local tabs; File/input/original ETag through internal reauth; changed resource/direct mutation/stale capabilities/cache304. Application tests не запускались при записи docs.

Открыты final wire/permission catalog/context precondition, normalization/Unicode length/blocklist/provider adaptation, code delivery/bootstrap/emergency UX/interfaces, physical schema/index/migrations/keyring/CSRF/CORS registrations и numeric rate limits. Ownership/mechanisms/defaults и independently-hosted same-site subdomains приняты; engineering gaps не переоткрывают их и не добавляют Demo dependencies.
