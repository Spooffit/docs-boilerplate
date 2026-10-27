# F16. Вход и доступ сотрудников

## Цель и границы

Предоставить сотрудникам собственной организации доступ к административному CM с отдельной Identity. Пользователи организаций-покупателей и их лицензии не являются учётными записями специалистов.

Фича относится к MVP. **В Demo нет входа, авторизации, ролей и управления пользователями.** Ограниченная демонстрационная цепочка не должна зависеть от реализации этой фичи. [Этапы](../../milestones/README.md).

## Участники и ответственность

| Участник | Обязанности |
| --- | --- |
| Специалист | Готовит и изменяет контент; имеет такие же возможности и область данных, как остальные специалисты |
| Админ | Предоставляет, меняет и отзывает доступ сотрудников в CM; выполняет проверку по F17 |

Все специалисты работают со всем контентом. Персонального разграничения по продуктам, вариантам или организациям нет. Проверка контента и управление доступом названы отдельными функциями админа; равенство специалистов не означает право обойти это разделение.

Создание и изменение Product/ProductVariant остаются в VR School. Администрирование сотрудников в CM не создаёт локальный редактор продуктового каталога.

## Источник учётных записей

Identity CM отдельная от VR School. Сквозной вход не требуется и не планируется в текущем объёме. Не нужен импорт административных пользователей из событий VR School как обязательная предпосылка входа.

Владение — отдельный IdentityAccess BC с ASP.NET Core Identity. Infrastructure `ContentManagerUser : IdentityUser<Guid>` является единственной учётной моделью; отдельный Domain StaffUser, дублирующие password/roles/security stamps не вводятся. Полные backend models определяются в F16 backend, а не повторяются здесь.

Login уникален и immutable, DisplayName обязателен, email не требуется. Exact normalization/length/wire поля остаются Q15. Роли Specialist и Administrator фиксированы и могут совмещаться; Administrator не наследует Specialist автоматически. Server-fixed permission catalog и initial RolePermission mapping не редактируются сотрудниками. Custom roles/permission editor относятся к future.

UI — Blazor Standalone, UI/API независимо хостятся на поддоменах одного базового домена: по HTTPS это разные origins одного site. Общий same-origin reverse proxy не требуется/не выбирается. Browser cookie принадлежит только API host, без Domain=.shared; credentialed cross-origin доступ ограничен exact UI origin и обязательной antiforgery защитой. Реальные hostname/cookie/CORS wire configuration относятся к engineering Q24/Q15.

## Основные сценарии

### Предоставление и изменение доступа

Только админ provision-ит сотрудника с Login/DisplayName и fixed system roles. Новый пользователь сам выбирает permanent password через одноразовый provider setup code с default 20 минут. Code связан с purpose/user/security generation и consumed атомарно; reset использует отдельный purpose и также одноразовый code. Админ не назначает постоянный пароль от имени сотрудника. Public register/email-reset не входят в MVP; точная безопасная передача code/provider относится к Q15/операционным деталям.

Первый админ создаётся только явным manual operator bootstrap: без default password в исходниках/migrations и без автоматического запуска при старте. Повтор bootstrap не создаёт нового пользователя. Emergency recovery — отдельная явно исполняемая процедура, не скрытая повторная регистрация первого админа.

Нельзя оставить систему без IsActive + Administrator + CredentialReady пользователя. Role/deactivation/reset и другие affected writes проверяют last-admin invariant в одной защищённой Identity transaction; concurrent решения не могут каждое сослаться на другого исчезающего админа. Self-deactivation допустима, если другой ready active Administrator остаётся. После подтверждения UI показывает committed результат, не локально переключённую роль как состоявшееся изменение.

### Вход и работа

Сотрудник входит через собственную Identity CM. Интерфейс получает подтверждённую идентичность и доступные действия. Он может работать со всем общим контентом в пределах выбранной роли; сотруднику организации-покупателя сам факт наличия лицензии административный доступ не даёт.

ASP.NET Identity password hashing используется без собственного алгоритма. Password имеет 15–128 characters, без composition требований, с weak-password check; paste разрешён. Configurable lockout default: 5 неуспешных попыток / 15 минут. Login rate limiting обязателен, конкретные deployment numeric limits ещё выбираются; MFA future. Эти defaults не являются измеренной SLA.

StaffSession имеет собственный Guid, user/time/absolute expiry и terminal revocation. Default lifetime 8 часов, без sliding/refresh/RememberMe. Logout завершает current session, остальные остаются. Deactivate/password reset делают предыдущие staff sessions недействительными; Reactivate требует нового Login и не resurrect-ит старые sessions.

### Изменение прав в открытой сессии

Каждый HTTP request получает один fresh coherent PermissionSnapshot без cross-request permission cache. Изменение roles отражается при следующем request/read; уже прошедший access check in-flight может завершиться. Session/account validity проверяется вместе с permissions; отсутствие новой snapshot не считается разрешением. Logout/Deactivate/reset сохраняют принятые последствия sessions, без обещания отменить уже проверенную команду посреди её исполнения.

Authorization.Abstractions предоставляет независимые permission/current-user contracts и operation metadata, AspNetCore adapters связывают их с HTTP/Identity. Permissions не являются Domain entity/invariant: owning resource rules и предметные проверки отдельно ограничивают действие. Capability resolver возвращает AllowedActions для UX, но это advisory observation, не command permit. Скрытая кнопка не заменяет server authorization; grants/installed runtime data и anonymous Demo не меняются staff role/session событиями.

CSRF обязателен для state-changing requests, включая Login/Logout/setup/reset/account actions. Data Protection keys persistent, protected и shared между API replicas; staff и Delivery purposes отделены. Exact provider/storage/token carriers остаются engineering, не повод вернуть cross-request cache или same-origin proxy.

### Истечение входа во время редактирования

1. Сотрудник вводит данные в открытую SPA tab; snapshot остаётся только в её памяти.
2. При требовании Login форма сохраняет original actor, exact owner/object/version, original ETag/baseline, actual input и pending operation/context; File objects удерживаются в той же tab.
3. Reauthentication выполняется внутри SPA без reload/navigation, которые уничтожили бы memory snapshot.
4. После Login того же user fresh GET получает current server state отдельно от original baseline; snapshot не перезаписывается прочитанным ETag/version/context.
5. Пользователь review-ит возможный concurrency/context change и отдельно сохраняет после current permissions/domain checks; нет autosave или blind command replay.
6. При unknown mutation outcome сначала выясняется committed результат, а не повторяется команда по restored форме.
7. Different actor или manual Logout очищают previous actor snapshot. Local tab identity notices помогают остановить stale вкладку, а mutation expected-login-context precondition предотвращает выполнение от другого actor; этот marker не является credential и не заменяет auth/CSRF.

**Recovery обязателен только внутри той же открытой SPA tab.** Reload/close/browser restart не гарантируют восстановление. В принятом сценарии File objects сохраняются в памяти; durable browser storage/offline и recovery после закрытия не вводятся. Это выбранная граница требования, а не перенос формы в localStorage.

## Ошибки и ограничения

При отказе входа не открывать рабочие действия как будто сотрудник авторизован. Different actor не получает чужой recovery snapshot. При изменении объекта original ETag/base сохраняются, new GET не маскирует conflict; domain/foreign basis и own concurrency errors остаются соответствующим фичам. Expected login context защищает вкладку от actor switch, не расширяет permissions.

Отзыв доступа сотрудника не является окончанием клиентской лицензии. Demo без входа не считается проверкой Identity MVP.

## Критерии приёмки

| Сценарий | Ожидаемый результат |
| --- | --- |
| Сотрудник входит в CM | Используется отдельная Identity без зависимости от входа VR School |
| Два специалиста работают с контентом | Одинаковые полномочия и общая область данных |
| Специалист пытается управлять доступом | Не получает функцию, отнесённую к админу |
| Админ меняет роль | Следующий request использует fresh snapshot; checked in-flight может завершиться |
| StaffSession истекла через absolute TTL | Новый Login, без sliding/refresh/RememberMe |
| Current Logout, Deactivate/reset, Reactivate | Logout не закрывает остальные; deactivate/reset invalidates previous; reactivate требует new Login |
| Два concurrent last-admin writes | Оба не могут оставить zero active ready Administrator; self-deactivate только при другом ready admin |
| Setup/reset code повторён/истёк/wrong purpose generation | Permanent password не устанавливается повторно; atomic one-use и default 20 минут |
| Password/lockout | Identity hash, 15–128 без composition, weak-password check/paste; default 5 failures/15 минут, rate limit отдельно |
| Provision/bootstrap | Admin-only; no public register/email reset/default password/autostart; repeat bootstrap не creates new user |
| Вход истёк в открытой SPA tab | Same-user internal reauth сохраняет actual input/File objects/original ETag/base/context |
| После восстановления объект изменён | Нет молчаливого затирания нового состояния |
| Different user/manual Logout/reload | Actor switch/logout clears snapshot; reload/close/restart не обещают memory recovery |
| AllowedActions либо hidden button | Backend всё равно проверяет current permissions/resource/domain; cached capability не permit |
| UI/API на поддоменах | Independent hosting, exact-origin credentialed CORS/CSRF, host-only API cookie; shared reverse proxy не обязательный |
| State-changing Login/Logout/account call | CSRF обязательна; persistent protected shared DP staff/Delivery purposes раздельны |
| Cross-tab actor change/unknown mutation | Expected login context не credential; no different-actor write/autosave/blind retry |
| Открывается Demo | Нет обязательной формы входа и зависимости от этой фичи |

## Связи и открытые вопросы

[Q10](../questions/Q10-permissions-and-isolation.md), [Q24](../../technical/frontend/questions/Q24-admin-sign-in.md), [F17](F17-content-review.md), [F18](F18-staff-activity-history.md), [M06](../modules/M06-staff-workflow.md).

Принятый протокол: [Q24](../../technical/frontend/questions/Q24-admin-sign-in.md#принятый-протокол-identityaccess-и-восстановления-формы). Открыты exact wire/login normalization/length/provider/setup operational details и numeric deployment rate limits: [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities), [Q20](../../technical/backend/questions/Q20-persistence-and-migration.md#identityaccess-persistence-и-authorization-snapshots). Detailed models только backend F16; выбранные lifetime/recovery/role rules не возвращаются в TBD.

[Бэкенд](../../technical/backend/features/F16-staff-access.md) · [Фронтенд](../../technical/frontend/features/F16-staff-access.md).
