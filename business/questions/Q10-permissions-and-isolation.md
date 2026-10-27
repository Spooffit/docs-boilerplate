# Q10. Полномочия и изоляция

## Вопрос

Кто работает в административном интерфейсе CM, какие действия выполняют сотрудники, нужен ли проверяющий и разделяется ли область данных?

## Роли и область данных

Работают только сотрудники собственной организации. Сотрудники организаций-покупателей используют приобретённые приложения, но не являются административными пользователями CM.

Нужны роли **специалиста** и **админа**. Специалист составляет контент. Админ проверяет его и либо одобряет, либо не одобряет и отправляет на доработку с пометкой о проблеме. Админ в CM предоставляет, меняет и отзывает административный доступ.

Все специалисты имеют одинаковые возможности и работают со всем контентом. Ограничений по отдельным организациям, продуктам, вариантам и командам для специалистов не вводится. Это не отменяет отдельно названные обязанности админа по проверке/доступу. Specialist и Administrator — fixed system roles, их можно комбинировать без implicit inheritance; Administrator не включает Specialist автоматически.

| Область | Принято |
| --- | --- |
| Подготовка и изменение контента | Одинаковые возможности у всех специалистов |
| Доступная область данных | Весь контент, без индивидуального разделения |
| Проверка, одобрение, возврат с замечаниями | Админ |
| Предоставление, изменение, отзыв доступа сотрудников | Админ в CM |
| CatalogConflict corrective repair и Resolve | Специалист; отдельная fresh проверка прав/scope/preconditions, не admin-only |
| Видимая история действий | Persisted immutable F18 /admin/activity; common journal Spec/Admin, staff-access Admin-only, no personal filter/MVP auto-purge |
| Demo | Без входа, авторизации, ролей, согласования и обязательного персонального аудита |

Identity отдельная от VR School. Сквозной вход сейчас не нужен и не планируется. Вход и восстановление формы описаны в [Q24](../../technical/frontend/questions/Q24-admin-sign-in.md), выбранное поведение вынесено в [F16](../features/F16-staff-access.md).

## IdentityAccess, authorization и sessions

IdentityAccess — separate BC; ASP.NET Core Identity infrastructure `ContentManagerUser : IdentityUser<Guid>` остаётся единственной учётной моделью без duplicate Domain StaffUser/password/roles/stamps. Authorization.Abstractions независим от Identity/HTTP и содержит contracts/metadata; AspNetCore adapter связывает проверку с web request. Domain/owning resource rules проверяются отдельно. AllowedActions capability observation не разрешает command без fresh access checks.

Permission catalog и initial RolePermission server-fixed; админ назначает сотрудникам fixed roles, не редактирует permissions. Custom roles/permission editor future. Один coherent fresh PermissionSnapshot на HTTP request, no cross-request permission cache; roles меняются в next read, checked in-flight может finish.

StaffSession own Guid имеет absolute expiry/terminal revocation, configurable default 8 часов без sliding/refresh/RememberMe. Current Logout не закрывает другие. Deactivate/reset invalidates previous sessions, Reactivate требует new Login. Runtime installed content/grants и anonymous Demo staff session не затрагивает.

Login unique immutable, DisplayName required, email optional. Admin-only provision/setup/reset uses one-use provider code default 20 минут, bound purpose/user/security generation и atomic consume; permanent password задаёт сам user. Public register/email reset отсутствуют. Operator bootstrap manual/repeat-safe без default password/autostart; emergency recovery отдельна. Last-admin invariant IsActive + Administrator + CredentialReady защищается вместе с affected writes в own Identity transaction, including concurrent changes; self-deactivate только при другом ready admin.

Identity password hashing, 15–128 characters без composition, weak-password check/paste разрешён. Lockout configurable default 5 failures/15 минут; mandatory login rate limiter numeric deployment limits ещё engineering, MFA future. CSRF обязателен для state-changing requests включая Login/Logout/setup/reset/account actions; persistent protected shared DP keys разделяют staff/Delivery purposes.

Blazor Standalone UI и API independently hosted на HTTPS-поддоменах одного базового домена (different origins/same site). Cookie host-only API, exact-origin CORS+credentials+antiforgery; same-origin reverse proxy не выбирается. Real hostnames/wire attributes/providers остаются engineering.

Memory recovery только open SPA tab без reload: actual input/File objects + original user/exact owner/object/version/ETag/base/pending context. Same-user fresh GET не overwrites baseline и не autosaves; unknown mutation требует outcome inquiry. Different actor/manual Logout clears snapshot; close/reload/browser restart не guaranteed. Tab identity notices и mutation expected-login-context precondition предотвращают stale-actor write; marker не credential.

Согласованные правила: [Q24](../../technical/frontend/questions/Q24-admin-sign-in.md#принятый-протокол-identityaccess-и-восстановления-формы). Exact normalization/length/wire/provider/setup operations: [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities), [Q20](../../technical/backend/questions/Q20-persistence-and-migration.md#identityaccess-persistence-и-authorization-snapshots). Полные models только backend F16.

## Оставшиеся уточнения

По [Q28](Q28-content-review-scope.md) проверяется exact ProductVariantPublicationRevision перед новой активацией. Одобрение связано с этой immutable revision; изменение проверяемого содержимого создаёт новый target. Принятый процесс и открытые уточнения описаны в [F17](../features/F17-content-review.md) и Q28.

Activity History /admin/activity, auditable scope, classification visibility, safe details и MVP retention определены в [F18](../features/F18-staff-activity-history.md). Exact typed wire/machine codes и storage/cursor implementation остаются engineering Q15/Q20; выбранные read permissions и отсутствие automatic purge не возвращаются в TBD.

Совмещение ролей и self-approval при обеих permissions разрешены F17; implicit inheritance отсутствует. Дополнительные исключительные права на политики, откаты и конфликты не придумываются; сохраняется равенство специалистов с оговорённой функцией админа.

## Применение

[F16](../features/F16-staff-access.md), [F17](../features/F17-content-review.md), [F18](../features/F18-staff-activity-history.md) и [M06](../modules/M06-staff-workflow.md) являются действующими спецификациями этой области. Связанные контентные фичи используют их правила, но не получают произвольно выбранного нового статуса вместо своего жизненного цикла.

Модель прав клиента-получателя, лицензии и grants остаются отдельной областью. Создание/редактирование Product/ProductVariant по-прежнему выполняется только в VR School. [Этапы](../../milestones/README.md).

## Уточнение блока 22: moderation permissions

MVP не вводит обязательный four-eyes rule. Если один сотрудник одновременно имеет permissions специалиста и админа, он может submit и approve собственную `ProductVariantPublicationRevision`; отдельная проверка `SubmittedBy != DecidedBy` не является бизнес-инвариантом.

Moderation backend всё равно отдельно проверяет permission на Submit/Withdraw и permission на Comment/Approve/Reject. Совмещение ролей не означает, что одна permission автоматически включает другую.

SubmittedBy и DecidedBy всегда сохраняются в moderation history, поэтому self-approval наблюдаем и может быть запрещён позднее отдельной policy без потери исторических данных.

Explicit Withdrawal revision относится к отдельному административному permission и не подменяется Approve/Reject F17.

F15 repair и Resolve доступны специалисту по [Q25](Q25-catalog-conflicts.md#f15-технические-границы-разрешения). Repair не разрешает ordinary writes при Open и не передаёт CM право менять source parent/lifecycle. Source correction выполняется уполномоченным пользователем VR School. F18 отражает repair commit и последующий Resolve как самостоятельные confirmed outcomes; successful repair не записывается как successful move. Точные permission names, DTO и audit taxonomy/reliability остаются Q15/Q20/F18. Safety Unpublish/Withdrawal при Archive сохраняют свои обычные permissions/preconditions, в том числе отдельный permission Withdrawal.

## F18: history scope и visibility

[Business F18](../features/F18-staff-activity-history.md), [backend](../../technical/backend/features/F18-staff-activity-history.md) и [frontend](../../technical/frontend/features/F18-staff-activity-history.md) используют standalone persisted immutable Activity History /admin/activity. Это не eventstore/undo/security log и не второй обязательный audit внутри каждого workspace; F11/F17 histories остаются предметными.

Auditable Create/actual Save/Publish/lifecycle/Delete своего owner, Publishing release/revision/promotion Start/PREPARE/item outcome/activation/rollback/unpublish/withdrawal/policy/Clear/Cancel, F17 Submit/Comment/Approve/Reject/Withdraw и separate F15 repair/Resolve. Staff provision/roles/deactivate/reactivate/setup-reset initiation/completion/bootstrap/emergency — Admin-only staff-access classification. Meaningful automatic Publishing/catalog conflict lifecycle included; GET/search/preview/download/codegen/Login/failed commands/securitycounter/transport/retry/inbox/plainprojection/Delivery materialization excluded.

Один row на independent committed result: whole-tree Save/PREPARE tuple one, human Approval/activation distinct. No-op и replayed ClientOperationId не дают новый row; intentional new equal-content artifact даёт. Committed означает own durable result; typed workflow details отражают partial/Failed item без обещания всех activated targets.

Common content journal permission audit.activity.read доступен Specialist/Administrator, staff-access audit.staff-access.read — только Administrator. Fixed role mapping F16, no personal ownership/SubmittedBy filter. Unknown visibility profile не safe-by-default. Current viewer permissions не берутся из роли автора, а audit-read не даёт target-read access. Visibility применяется до paging/count/filter options; actor options только visible audit history, не admin user directory.

Filters actor(kind + staff ID), action, target type/ID, UTC [From,To), correlation/operation; default temporal cut отсутствует. Keyset OccurredAt DESC, Id DESC, default 50/max 200; cursor filters/reader-context bound с fresh authorization каждого request. NextCursor/HasMore, no required total count/stable paging snapshot. Read-only GET collection /api/v1/activity, exact record и filter-options; no public mutation. UI manual refresh/list/filter/detail, no realtime/poll/export/notification.

Actor Staff/System/Integration/Operator берётся из trusted execution context; automatic executor отдельно от trusted persisted Staff initiator, external/operator не fake staff. Optional actor/target labels safe as-of, exact IDs typed; rename/deactivation/delete не rewrite/cascade records. SourceOccurredAt и AuditRecordedAt UTC не physical commit/causal clock. Mutable Draft link открывает current exact identity, не историческую версию содержимого.

Metadata whitelist — IDs/old-new state/roles/policy/counts/operation refs и safe optional labels; no full payload/values/before-after/storage URLs/ETags/secrets. Full F17 reason — authoritative exact owner ref; denied/gone/unavailable показывается явно, не empty remark/полный short summary.

Own source intent atomic с business commit → at-least-once dispatcher со stable EventId/source-fact identity → Audit append+inbox own commit, без cross-BC transaction. Audit row Id приёмник выделяет позже независимо от EventId. Duplicate same source facts idempotent, changed facts той же source identity quarantined без overwrite. Eventual append/source success не ждёт Audit: empty query не evidence отсутствия action, UI не создаёт local final row.

MVP retains all без automatic purge/user-delete/FK cascade. Export/archive/exceptional anonymization только поздняя explicit policy; это не legal forever-retention promise. Accepted contracts: [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#f18-принятый-activity-contract), [Q20](../../technical/backend/questions/Q20-persistence-and-migration.md#f18-принятый-capture-append-и-cursor-read). Exact CLR/wire/providers engineering; detail models только backend F18.
