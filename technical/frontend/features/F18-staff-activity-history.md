# F18. История действий сотрудников: фронтенд

[Бизнес](../../../business/features/F18-staff-activity-history.md) · [Бэкенд](../../backend/features/F18-staff-activity-history.md) · [M06](../modules/M06-staff-workflow.md).

## 1. Результат и границы

В MVP сотрудник открывает самостоятельный экран «История действий» и видит подтверждённые действия с контентом и настройками: кто, когда, какое действие выполнил, над каким объектом/точным состоянием и с каким результатом. Источник — сохранённая серверная audit/read model, доступная после перезагрузки. Нажатие кнопки, локальный snackbar, optimistic row и технический лог GET не доказывают выполненное действие.

Самостоятельный read-only экран `/admin/activity`, пункт «Администрирование → История действий», относится к MVP. По [Q10](../../../business/questions/Q10-permissions-and-isolation.md#f18-history-scope-и-visibility) common `audit.activity.read` доступен Specialist и Administrator; staff-access records требуют дополнительного `audit.staff-access.read` Administrator. Сервер применяет record visibility **до paging, counters и filter options**. Нет personal-only-my-history isolation; administrative placement не делает весь журнал admin-only. В Demo F18/Identity отсутствуют; обязательный второй журнал в каждой карточке не добавляется.

F18 представляет append-only аудит. История активаций [F11](F11-publication-lifecycle.md) объясняет публикационные переходы, история [F17](F17-content-review.md) хранит authoritative решения/комментарии точной moderation iteration. Общий журнал связывает их с действием сотрудника, но не заменяет их предметную модель и не меняет её. Approved, activated и Delivery effective — разные результаты; запись Approval не выдаётся за activation/delivery success.

Confirmed corrective patch repair [F15](F15-catalog-conflicts.md) и последующий Resolve отображаются как разные outcomes: сохранённый repair не означает применённый move. Source change/rejected Resolve не превращают repair record в автоматически отменённый. Exact Variant/Conflict references ведут к доступному owner context; details не копируют полный patch/source payload или технические tokens. Wire/taxonomy и safe detail остаются Q15/F18.

Actor kind явно различает Staff/System/Integration/Operator. Trusted staff initiator асинхронного workflow показан отдельно от System executor; external/operator action не получает вымышленного CM StaffId. Safe optional as-of captions и IDs берутся из captured audit facts, не current Identity directory/target joins. Rename/deactivate не переписывают прошлое; отсутствующий caption показывает identity fallback. Common контент не ограничивается CreatedBy.

Журнал содержит actual committed Create/Save/Publish, owner lifecycle/Delete, releases/revisions, promotion prepare/item outcomes, Activate/Rollback/Unpublish/Withdrawal/policy/Cancel, moderation Submit/Comment/Approve/Reject/Withdraw и независимые F15 repair/Resolve. Staff provision/roles/deactivate/reactivate/setup/reset/bootstrap/emergency видны только по staff-access permission. Whole Save даёт одну запись; atomic prepare — одну с exact related references; human Approval и Activation отдельны. Outcome Committed не означает успех всего workflow; partial business result получает корректный caption. GET/download/codegen, transport retry/failed security logs, genuine NoOp и replay confirmed ClientOperationId не создают новых activity rows. Полная taxonomy/typed metadata определена один раз backend F18.

Все audit records сохраняются в MVP без automatic purge/user delete. Export/archive/anonymization — future explicit scope, это не юридическое обещание вечного хранения. Full content/before-after/payload/URLs/ETags/secrets, realtime/polling/notifications не вводятся.

Ниже — целевая декомпозиция. Имена блоков проектные; наличие этих компонентов/API в текущих исходниках не утверждается. Проверенная граница кода приведена в §11.

## 2. Страницы, экраны и навигация

| Страница / экран | Точка входа и контекст | Состав и режим | Завершение / переход |
| --- | --- | --- | --- |
| SC01. StaffActivityPage | `/admin/activity`, MVP; current IdentityAccess staff/permissions через F16 | Read-only журнал, UTC query/filter options, keyset list, Next/Refresh, loading/empty/errors | Exact ActivityId открывает SC02; target navigation проверяет owner access отдельно |
| SC02. ExactActivityDetails | Выбранный `ActivityId` из SC01 либо согласованный deep-link selection | Exact record, actor/time/action/target/result; safe details, moderation reason/reference при Reject; отдельные состояния дополнительных reads | Закрытие возвращает к тому же query/page в живой SC01; переход открывает exact target/iteration, не latest |

SC02 — значимый режим той же страницы, а не новый обязательный route или отдельный FSD layer. Синтаксис ActivityId в URL/selection и сохранение query между reload/back остаются Q13/Q15; `/admin/activity/{id}` не назначается этим документом. Q13 требует deep links к activity object/state: они сохраняют immutable IDs и exact version/revision, не только label или выбранную строку в памяти.

Прямой вход и reload читают серверные данные с новой проверкой доступа. Сам route не подтверждает read permission. Если exact запись не доступна, SC02 показывает соответствующий отказ/отсутствие, не подставляет соседнюю запись. Недоступность цели не уничтожает доступную историческую запись; её чтение и переход к цели имеют разные результаты. Новую карточку цели с «последней» версией не открывать вместо исторического состояния.

## 3. Карта FSD-блоков

### 3.1. Состав

| Имя блока | Слой / логическое размещение | Ответственность | Использование / основное описание |
| --- | --- | --- | --- |
| StaffActivityPage | `pages/staff-activity/ui, model` | Entry, один владелец query/list/selection/exact reads, координация навигации и recovery | Создать для SC01/SC02, §3.2 |
| ActivityQueryControls | Локальные `pages/staff-activity/ui` | Actor/kind/action/target/time/correlation/operation filters, NextCursor/HasMore и manual Refresh | Создать внутри SC01, §3.2; без page-number/mandatory total/realtime |
| ActivityRecordPresentation | `entities/staff-activity/ui` | Read-only строка и exact представление safe generated record; actor/target identity без foreign joins | Создать в SC01/SC02, §3.2 |
| ActivityTargetReference | Локальная `pages/staff-activity/lib, ui` граница | Проверить поддержанный target type + IDs, согласовать owner route/read, показать unavailable | Создать для SC02, §3.2; публичные owner boundaries §5 |
| Staff/auth recovery boundary | Публичная граница F16 | Current staff, protected access/recovery и account isolation | Переиспользовать [F16](F16-staff-access.md), контекст применения §6/§9 |
| Generated API / transport | `shared` техническая граница существующего API-client проекта | HTTP/read/errors/cancellation без знания moderation semantics | Переиспользовать и регенерировать после OpenAPI, §8 |
| UI primitives | `shared/ui` | Подписанные controls, keyset Next, сообщение/retry и local time formatting | Переиспользовать, локальная семантика §9 |
| MVP shell/navigation | `app` и существующая shell композиция | Подключить route и Administration entry по F16 | Изменить после подтверждения контракта/прав; не отдельный activity store |

Самостоятельные widgets/features не требуются: состав уникален странице, самостоятельных mutation actions нет. Выделение widget/feature допустимо только при реальной ответственности/переиспользовании, без пустых срезов. Логические обозначения не являются утверждёнными C# namespaces/физическими путями.

### 3.2. Карточки изменяемых блоков

| Блок | Назначение и входы | Выходы / получатель | Собственное состояние / жизненный цикл | Данные, зависимости и состав |
| --- | --- | --- | --- | --- |
| StaffActivityPage | Read-only журнал; current staff/context, applied query/optional ActivityId | Query/list/exact/options детям, selection/navigation/recovery | Один applied query/keyset chain, visible list/read state, options, exact/reason/target states; filter/identity change и manual Refresh reset cursor; late replies fenced | Generated list/exact/filter-options API + F16; query/presentation/reference blocks; no browser source commands/outbox consumer |
| ActivityQueryControls | Selected filters, applied query, authorized options/validation, NextCursor/HasMore | Apply/Next/Refresh intent → page | Local input до Apply, applied query у page; UTC range validation не заменяет server validation; actor suggestions из visible history, actions/types из authorized registry | Shared controls/keyset Next/messages; no client-only whole-history filtering/Identity-directory actor lookup |
| ActivityRecordPresentation | Generated safe record, режим row/exact, supplied reason/ref read state и допустимые navigation intents | Select ActivityId или open typed reference → page | Нет mutable бизнес-копии/глобального состояния; смена record полностью меняет presentation; read status принадлежит page | Actor safe snapshot/ID, timestamps/action/target/result; shared formatting. Не получает StaffUsers list ради определения исторического автора |
| ActivityTargetReference | Supported target kind, immutable object/substate IDs, optional ModerationId/RevisionId; permissions/read status | Owner navigation либо явное unsupported/deleted/unavailable; reason read result → SC02 | Только presentation/resolution state текущего exact record у страницы; смена record/account очищает старый resolution | Public F01–F17 owner routes/read boundaries по виду цели; typed route construction; никаких raw event URL и прямого обращения к чужим внутренним компонентам |

Shared formatting принимает значение времени и согласованный presentation format; оно не выводит время действия из браузерного момента/порядка доставки. Общая auth/recovery карточка находится в F16, transport правила — в [S06](../standards/S06-api-client.md). F18 задаёт только свой read context.

## 4. Логическая компоновка

```text
SC01. StaffActivityPage [pages, query/list/selection owner]
  Заголовок и доступный current staff context [F16]
  ActivityQueryControls [локальный page block]
    Поддержанные фильтры / их validation [shared controls]
    Manual Refresh [reset keyset] / Next [NextCursor, HasMore]
  Список подтверждённых записей
    ActivityRecordPresentation [entities, row]
      Автор / время / действие / exact цель / результат
      Выбрать ActivityId → SC02
  Loading / empty / forbidden / read error + retry [shared]
  SC02. ExactActivityDetails [режим этой страницы]
    ActivityRecordPresentation [entities, exact]
    Safe summary/details и exact IDs
    Reject reason / ссылка на F17 exact iteration
    ActivityTargetReference [page-owned]
      Open exact object/state/history или unavailable
    Отдельные exact/reason/target loading/error состояния
    Закрыть → текущий query/list SC01
```

| Связь частей | Передаваемые сведения | Координатор | Наблюдаемый эффект |
| --- | --- | --- | --- |
| Controls → page → list/options | Applied query, NextCursor/HasMore, explicit Refresh | StaffActivityPage | Filter/context/Refresh reset keyset; server visibility applied before list/options; late prior-query result ignored |
| Row → page → SC02 | ActivityId, без замены exact DTO строкой | StaffActivityPage | Exact read выбранной записи; detail/reason не берутся от предыдущей строки |
| Exact → reference → owner | TargetType + immutable IDs; для moderation ModerationId и RevisionId | Page/public routing boundary | Исторический target/iteration; при отказе readable record сохраняется |
| F16 → page | Same current staff recovery/access либо новый account | Shell + StaffActivityPage | Same-user безопасный reread, другой staff не видит прежние данные/context |

SC01 и выбранный SC02 согласуют query/selection через одного владельца. Раздельные loading/error состояния list/exact/reason/target не требуют отдельных экранов и не превращают всю SPA в недоступную область. Текстовое дерево описывает взаимодействие, без layout/CSS.

## 5. Зависимости и публичные границы

Страница вызывает публичный read API generated client, передаёт generated record в entity presentation и принимает selection/navigation intent. Child не знает тип страницы; callback не создаёт зависимость нижнего слоя от `pages`. Shared transport/UI не определяют actor, permission, exact target или moderation decision. Браузер не зависит от backend Domain/EF и не читает foreign DbContext.

Переход к цели координирует page через разрешённый набор typed owner boundaries. Для Schema/Instance он сохраняет object ID и exact version ID; для release/revision — immutable release/revision ID; для moderation — конкретные ModerationId и RevisionId. Owner workspace может дополнительно требовать свои чтения/доступ. Если target kind не поддержан, ID отсутствует по контракту или owner route недоступен, отображается понятная reference с безопасными идентификаторами, а не произвольный URL из metadata. Открытие current version не считается открытием target audit record.

Предметное объяснение Reject находится у F17. F18 обеспечивает доступ к содержательному пояснению и связывает его с exact moderation iteration/revision: safe summary в audit может быть сокращённой, но authoritative reason доступен через public owner read/reference. Ошибка/отказ чтения пояснения явно показываются; нельзя молча представить Reject как запись без причины. Exact F17 переход не создаёт Submit/Comment/Reject command и не меняет решение.

Чтение истории автора не требует активного current profile. Captured optional actor/target captions и IDs не переписываются rename/deactivate; UI не join-ит Identity directory. Actor suggestions/filter options происходят из **доступной читателю audit history**, не admin-only staff directory. Audit permission не authorizes target/reason: gone/denied/read failure сохраняют доступный record и exact IDs, а не превращаются в пустое пояснение. Mutable Draft reference открывает current exact identity, а не обещает historical content copy.

## 6. Данные и владение состоянием

### 6.1. Один владелец каждого состояния

| Данные / состояние | Источник истины | Клиентский владелец и область жизни | Обновление / сброс |
| --- | --- | --- | --- |
| Сохранённые activity records | Backend F18 append-only read model | Page хранит полученную серверную страницу generated DTO | Entry/reload/query/page change; не создаёт local final record после кнопки в другой фиче |
| Applied query + keyset chain | Selected server query/cursor и допустимый input | StaffActivityPage на время живой страницы | Query/context change и manual Refresh reset cursor/head; Next только по same query/reader NextCursor/HasMore |
| Authorized filter options | Backend F18 visible-history actors и permitted registry actions/types | Отдельное options/read state у page | Entry/access change или explicit Refresh; failure отдельно, no Identity-directory substitution |
| Filter input/validation | Пользовательский ввод до применения + server validation | Controls, затем applied query у page | Apply после проверки; invalid input не создаёт «пустую историю» |
| ActivityId/exact record | Выбор immutable ID + DF04 | Page; row DTO не подменяет exact result | Новый ID очищает старые exact/reason/target states, reload снова читает exact record |
| Reason/reference и target availability | Public owner read F17/другой target owner через DF05 | Отдельные read states текущего exact selection у page | Смена ID/account сбрасывает; failure не меняет уже прочитанную историческую запись |
| Current staff/access и recovery | F16 backend/current account boundary | Shell F16; page получает актуальный контекст | Protected read проверяется вновь; другой account очищает чужое page state |

Все business поля берутся из NSwag generated models, без параллельного ActivityViewModel/DTO с теми же полями. Loading, request generation/context, selection, query draft, read error и target/reason resolution — presentation-state рядом с DTO. Browser storage, offline-cache, глобальный activity store и retention TTL не назначаются. Persistence означает серверный журнал, а не восстановленный local list.

### 6.2. Состав представления

| Сведения | Как интерпретируются UI |
| --- | --- |
| Actor kind и staff identity | Staff/System/Integration/Operator; safe optional as-of caption/identity fallback; System executor и trusted Staff initiator отдельны |
| SourceOccurrenceTime / AuditRecordTime | Серверные UTC occurrence/append observations показаны раздельно; UI local display согласован с UTC filters. Ни одно значение не physical commit timestamp/click time/global causal order |
| Action и result | Actual committed outcome, не текст кнопки; Committed отдельного stage не workflow-all-succeeded, typed partial business result объясним |
| Target identity | Kind + immutable object/state IDs и safe label; exact version/revision различима. Изменённое/удалённое имя не делает target другим |
| Summary/details | Safe typed related IDs/old-new state/roles/policy/counts/references; no full content/before-after/URL/ETag/secrets в presentation/logs/recovery |
| Reject explanation/reference | Содержательное F17 пояснение связано с ModerationId и RevisionId; short summary не заменяет доступ к полной причине |
| Недоступная цель/автор | Исторические safe факты остаются читаемыми в рамках record permission; unavailable target отличается от отсутствия activity record |

Filters: actor StaffId, actor kind Staff/System/Integration/Operator, action, target type/exact ID, UTC [From, To), correlation/operation. Default time cut отсутствует. Input local dates нормализуется к UTC range по выбранному contract; display timezone не меняет границы query. List ordered OccurredAt DESC, Id DESC; default50/max200, NextCursor/HasMore без mandatory total count/page numbers. Cursor bound к reader/query с fresh authorization; смена context/filter/manual Refresh reset keyset. Late/out-of-order append допустим: cursor traversal не stable whole-feed snapshot и не global causal order, manual Refresh начинает свежую выборку.

Query/list/exact/reason responses привязаны к original request context и staff. При смене query, selection или account поздний ответ не обновляет новый контекст; cancellation помогает, но не заменяет проверку принадлежности результата. Same-user sign-in recovery F16 может сохранить допустимые filter input/applied query/ActivityId и затем перечитать сервер; cross-account recovery не переносит данные предыдущего staff. F16/Q24 задаёт memory-only recovery той же живой SPA-вкладки без reload, original owner/context и очистку при другом user/explicit logout; back-navigation composition Q13, secrets исключены.

## 7. Действия и клиентские потоки

| Действие | Начальное состояние и запуск | Исполнитель | Обработка / server operation | Результат в интерфейсе |
| --- | --- | --- | --- | --- |
| UI01. Открыть журнал | SC01/direct `/admin/activity` с допустимым staff context | StaffActivityPage | F16 access + F18/DF03 server list по согласованному query | Loading → loaded/empty/error, persisted records после reload |
| UI02. Apply / Next | Controls SC01; новый query либо HasMore/NextCursor | Controls → page | Validate/apply reset keyset; Next передаёт server cursor только same reader/query; DF03, options read backend F18 | Visible server selection; no page number/mandatory total/client-only filtering; failure не empty |
| UI03. Manual Refresh / повторить read | SC01/SC02 explicit action/error | StaffActivityPage | Refresh list resets keyset/head; safe DF03/DF04/options/owner read своего context, pending duplicate blocked | Свежая выборка либо explicit read error; no realtime/poll/notification/source mutation |
| UI04. Выбрать exact record | Строка SC01 либо согласованный deep link | Presentation → page | ActivityId → DF04; очистить предыдущий detail context | SC02 exact record/отказ/absence, без выбора latest |
| UI05. Прочитать reason / открыть цель | SC02 с typed stable IDs | ActivityTargetReference → page/owner | DF05 публичный owner read/reference; exact moderation/revision либо иной exact target | Reason доступна или явный read failure; переход к правильному state либо unavailable |
| UI06. Закрыть detail / выйти | SC02 либо уход со страницы | StaffActivityPage | Локальная смена selection/navigation, без HTTP записи; reads отвязываются | Тот же live list/query; stale detail не показывается новой selection |
| UI07. Восстановить доступ | Protected read session401 | F16 + page | Same-user open-tab internal reauth без reload, fresh access/read; cursor reset при context change | Допустимые query/selection восстановлены; Logout/other user clears; no persistent page state/source replay |

F18 не предоставляет create/update/delete audit records. Обновление списка не пишет activity «прочитал историю» как successful change. Mutation другой фичи подтверждается её серверным owner: успешная команда может предшествовать доставке записи в audit read model. Отсутствие новой строки сразу после success означает только отсутствие в текущем read; оно не откатывает business success и не превращается в «действие не выполнено».

Source action не ждёт audit append, browser не создаёт local final row и не повторяет source mutation ради истории. Late/out-of-order records допустимы; пользователь делает manual Refresh. Empty feed не доказывает отсутствие всех source actions или drained capture backlog; read failure не empty. Unknown source outcome выясняется через owner contract, не отсутствием activity. Без отдельного authoritative delivery evidence UI не выдумывает pending/drained status либо deadline появления записи.

## 8. Использование API и NSwag

| Потребность / UI-поток | Backend owner / flow | Входные сведения | Используемый результат | Владелец вызова |
| --- | --- | --- | --- | --- |
| UI01/UI02/UI03 list | [F18 backend](../../backend/features/F18-staff-activity-history.md), DF03 | Selected filters/UTC range/limit/NextCursor | Safe visible records/NextCursor/HasMore, empty либо read error | Page api/model boundary |
| UI01/UI02/UI03 filter options | Backend F18 DF06 | Current reader scope, no admin directory request | Visible-history actors/authorized registry action-type options; unknown/error отдельно | Page api/model boundary |
| UI04/UI03 exact | F18/DF04 | ActivityId | Immutable exact display-safe record, access/absence | Page api/model boundary |
| UI05 reason/target/history | F18/DF05 → public target owner, для Reject [F17](../../backend/features/F17-content-review.md) | Typed target IDs, exact ModerationId/RevisionId | Authoritative reason/owner read; доступность exact historical target | Page/public owner boundary |
| UI07 protected read recovery | [F16](../../backend/features/F16-staff-access.md) + frontend F16 | Original staff/query/selection без credentials | Same-user/current permissions, затем safe read | Общая F16 boundary + page |

DF01 source capture/dispatch, DF02 consume/idempotency и DF07 conflicting duplicate серверные, UI не участвует в outbox/inbox/quarantine. Принят read-only API: `GET /api/v1/activity`, `GET /api/v1/activity/{activityId}`, `GET /api/v1/activity/filter-options`. DF03 list/DF04 exact/DF05 target-reason/DF06 options и все модели определены один раз backend F18. [Q15 activity contract](../../backend/questions/Q15-api-and-source-code.md#f18-принятый-activity-contract) задаёт оставшиеся exact codecs/literals/headers; routes выбраны, но существование live OpenAPI/generated methods не утверждается. Новые generated class/method names не выдумываются.

Подтверждена существующая [NSwag config](../../../../contentmanager.ui/src/ContentManager.ApiClient/nswag.json): generated interfaces/classes/DTOs, injected HttpClient, System.Text.Json, output [ContentManagerApiClients.g.cs](../../../../contentmanager.ui/src/ContentManager.ApiClient/OpenApi/Generated/ContentManagerApiClients.g.cs). [DI](../../../../contentmanager.ui/src/ContentManager.ApiClient/DependencyInjection/ApiClientServiceCollectionExtensions.cs) сканирует `IContentManagerApiClient` и подключает matching interfaces со scoped lifetime. Это общая транспортная основа, не доказательство F18 capability. После owner OpenAPI контрактов выполнить регенерацию; generated files не редактируются вручную.

Records immutable/read-only: F18 не требует ETag/If-Match mutation API и не создаёт fake concurrency controls. Token mutable ресурса при последующем owner action принадлежит той фиче и exact owner read; audit row/reason/history не дают ему If-Match. Добавлять технический Version в client record ради шаблона не нужно. Ошибки переводятся в UI-state соответствующего read без копирования business модели; недостаток display-safe данных исправляется server contract + regeneration согласно [S06](../standards/S06-api-client.md).

## 9. Состояния, ошибки и доступ

| Ситуация | Что видит пользователь | Доступные действия | Данные / восстановление |
| --- | --- | --- | --- |
| Demo | F18 не требуется в navigation | Demo content сценарии | Без скрытого staff/auth/audit prerequisite |
| List/exact loading | Ожидание соответствующего read и выбранный безопасный context | Навигация; без дубля pending read | Старый response не подменяет новый query/ID |
| Empty / no matches | Нет видимых записей в данной confirmed выборке | Filter/manual Refresh | Не доказательство отсутствия всех source actions, drained backlog или source failure |
| Loaded record / partial workflow outcome | Safe actor/two UTC times/action/exact target/committed stage result | Select/reference/read/navigation | Committed stage не allSucceeded; no edit/delete/export, retained historical facts |
| Filter options pending/failure | Отдельное состояние доступных suggestions | Safe retry/read | Не подставлять admin Identity directory и не трактовать options failure как empty feed |
| Cursor invalid/context drift | Нужно начать новую выборку | Explicit Refresh/reset | Fresh reader/query checks; no guessed next page/reuse другого account cursor |
| Invalid filter input | Ошибка конкретного параметра по контракту | Исправить ввод | Applied query остаётся отдельно; ошибка не маскируется пустой историей |
| List/exact network error | Ошибка затронутого read | Safe retry | Доступные прежние данные могут оставаться с явной непроверенной актуальностью; не объявляются новым server result |
| Audit lag после committed command | Подтверждённое business success отдельно от ещё не прочитанной activity | Safe refresh после согласованного owner result | Нет rollback success, local persisted record или дублирования source command; pending delivery только при подтверждённом основании |
| Reject short summary | Содержательная safe summary + exact reference | Прочитать/открыть authoritative reason F17 | ModerationId и RevisionId сохраняются; полный reason не теряется из-за сокращения summary |
| Reason read failure/forbidden/unavailable | Явное состояние недоступного пояснения, исторические action/IDs видимы в рамках права | Safe retry/переход при permission | Не показывать «нет причины», не подставлять reason другой/latest iteration |
| Target deleted/unavailable/unsupported | Историческая typed identity и понятная недоступность перехода | Разрешённый owner history/read либо закрыть detail | Record остаётся читаемой, если доступна; отсутствие target не объявляется отсутствием audit |
| Actor renamed/deactivated/deleted | Safe исторический snapshot и/или meaningful StaffId fallback | Read доступных historical facts | Нет current user substitute, profile lookup failure не удаляет attribution |
| 401 / требуется восстановление входа | F16 sign-in/recovery для read context | Same-user восстановление и reread | Query/selection допустимо сохранить; secrets нет; другой staff не получает прежние records/filters |
| 403 / права или account отозваны | Отказ этой области | Только актуально разрешённая navigation | Нет relogin loop, stale roles/list cache не дают permission и не раскрывают запрещённые данные |
| Exact record absence по контракту | Запись отсутствует/недоступна | Вернуться к списку, безопасный reread | Не заменять соседней записью, не маскировать access refusal произвольным not-found |
| Поздний response / другой account | Данные только нового разрешённого context | Его действия чтения | Старые list/detail/reason/results не попадают в новый selection/account |

Каждая protected staff HTTP операция — list, exact record и owner reason/target read — снова проверяет актуальные роли и active account через F16. Visibility navigation/controls и current staff cache только объясняют UI доступ. Результаты должны возвращать только разрешённые safe сведения; скрытие кнопки не защищает read DTO. Проверка не распространяется автоматически на SDK/Delivery credential flows, у которых отдельные контракты. Commit отзыва роли учитывается следующей проверкой; уже прошедший проверку запрос не обещается отменить.

Каждый list/exact/options HTTP read использует current IdentityAccess fixed-role permissions/F16 fresh request snapshot. `audit.activity.read` общий Specialist+Administrator, staff-access records/options требуют `audit.staff-access.read` Administrator. Серверная visibility действует до paging/counters/options, скрытых rows/suggestions/metadata нет в DTO. Нет personal-only-my-history isolation. Audit read не authorizes target/reason/staff management: owner проверяет своё право; unavailable target не удаляет record. Уже checked in-flight semantics остаются F16, stale browser role не authority.

По [S05](../standards/S05-ui-and-forms.md)/[S07](../standards/S07-accessibility.md) фильтры и действия имеют понятные подписи, error/pending/unavailable различимы без цвета. Selection, paging, retry и переход к exact target доступны с клавиатуры. Открытие/закрытие SC02 и восстановление входа сохраняют понятный focus context; неизвестные browser/device/key bindings/tooling не назначаются. Сообщения не требуют от пользователя знания outbox/transport и не раскрывают secrets.

## 10. Проверки и приёмка

| Проверяемый блок / сценарий | Исходные условия и воздействие | Наблюдаемый результат | Граница проверки |
| --- | --- | --- | --- |
| SC01 persisted list | Реальное отслеживаемое successful действие, audit delivery, reload `/admin/activity` | Actor/time/action/target/result прочитаны с сервера и пережили reload | E2E с backend source/audit persistence; local mocks не доказывают durability |
| SC01/controls keyset | Actor/kind/action/type/exactId/UTC[From,To)/correlation/operation; ties OccurredAt, late append | DESC time/Id cursor50 default200 max, HasMore/NextCursor, no default timecut/total; filters/Refresh reset, no whole-snapshot/global-order promise | Page/API doubles + real backend query |
| Visibility/filter options | Specialist/Admin/combined, staff-access rows и actors только в скрытых records | No hidden row/counter/suggestion; visible-history options без admin directory access | Real authz/query integration + page composition |
| Query validation/empty/failure | Invalid query, valid no matches, read timeout | Три состояния различимы, ввод не превращается в false empty | Controls/page/transport |
| SC02 exact selection | Две записи/iterations одной revision, новый ActivityId во время read | Exact record и reason/IDs выбранной iteration, no latest fallback | Page coordination/API doubles; реальные exact reads отдельно |
| Target reference | Version/revision меняется, target archived/deleted/forbidden, unsupported kind | Typed trusted route содержит historical IDs либо unavailable; record не исчезает | Navigation/presentation + owner integration |
| Moderation Reject | Значимое reason F17, short summary F18, затем reason read error | Доступ к exact reason; при error явная недоступность, no missing-reason/latest substitution | F17/F18 API composition; E2E для сохранённого reason |
| Staff attribution | Rename/deactivate/missing caption; System executor+staff initiator; Integration/Operator | As-of caption/ID fallback без current joins; actor kinds/trusted initiator отдельны, no invented StaffId | Presentation + real producer fixtures |
| Producer/delivery distinction | Committed source success, delayed audit consume, отдельная audit read ошибка | Нет false business failure/rollback/optimistic persisted row/duplicate command | UI/API coordination; durability/идемпотентность проверяет backend |
| Audit duplicate delivery | Один source event повторно доставлен | Пользователь видит одну сохранённую запись | Реальный backend idempotency/inbox, не client dedup замена |
| Shared content/target permission | Spec/Admin current read, owner target/reason denied/gone/readfail | Common records visible, staff records admin-only; no CreatedBy isolation; missing target retains record, reason failure не «нет причины» | Page + real F16/owner authz |
| Fresh access | Роль/active state изменены перед следующим read | List/exact/reason access отказали по актуальным правам; stale UI не раскрывает данные | Реальный server authz + UI handling |
| Same-user recovery | 401 с query/ActivityId, повторный вход того же staff | Допустимый original read context, server reread, no source mutation replay | F16 recovery/page/API doubles + реальный sign-in отдельно |
| Account/privacy/late response | A читает detail, вход B; старый response после switch | B не видит A records/reason/filter context; forbidden scope очищен | Page/shell async coordination |
| Read-only scope и safe content | Проверка controls/DTO presentation/recovery/logging | Нет edit/delete audit/export/payload/secrets, source owner mutation concurrency не берётся из audit | Source/contract review + UI presentation |
| Atomic action taxonomy | Whole Save/automatic prepare/replayed ClientOperationId/NoOp; human Approval then Activation; repair then failed Resolve | Save/prepare one record с exact related IDs, replay/noop no new row, separate committed stages не ложный workflow success | Backend capture fixtures + UI captions |
| Retention/times/lag | Old retained rows, UTC boundary/local display, out-of-order append/empty/readfail | No purge/userdelete, two time meanings no physicalcommit claim, no false drained/no-source-actions conclusion | Presentation + read persistence/query |
| Доступность | Keyboard selection/paging/retry/SC02 close, error/recovery | Понятные labels/state/focus, no colour-only сообщение | Component/ручная функциональная проверка без CSS acceptance |

Для будущего UI применим [S08](../standards/S08-testing.md), test library этим документом не назначается. Изолированные тесты используют generated API/auth doubles и проверяют observable callbacks/requests/state. Сквозной MVP путь требует реальных CM Identity, source command commit, durable capture/dispatch/consume, audit read persistence, owner target/reason и reload. Consumer delivery/idempotency, read authorization и survival после перезапуска не доказываются цепочкой browser mocks.

Это критерии будущей реализации. В рамках документа выполнены source inventory и проверки структуры/ссылок; application tests, browser flow, audit endpoints, Identity, PostgreSQL/outbox delivery не запускались. Наличие текста/NSwag инфраструктуры не означает реализованный журнал.

## 11. Последовательность изменений и открытые вопросы

Materialize selected backend read models/codecs/visibility/cursor/filter options и durable capture/append по [Q20](../../backend/questions/Q20-persistence-and-migration.md#f18-принятый-capture-append-и-cursor-read). После owner OpenAPI regenerate NSwag, подключить F16 protected read/open-tab context, SC01 selected UTC filters/keyset/manual Refresh и SC02 exact/reason states. Public owner references сохраняют exact identity, no mutable-Draft historical-content claim. Затем real visibility/options/late-append/cursor/account/lag tests; UI не компенсирует недолговечный capture, не ждёт audit после source success. Framework/recovery editor/component library не меняются.

Текущий source inventory: в `contentmanager.ui/src` среди `.cs`/`.razor` вне bin/obj не найдены Activity/moderation names или `/admin/activity` route. [NavMenu](../../../../contentmanager.ui/src/ContentManager.Widgets/Components/Common/NavMenu.razor) содержит Home, content modeling, media и catalog navigation; Administration/Activity отсутствуют. В [generated client](../../../../contentmanager.ui/src/ContentManager.ApiClient/OpenApi/Generated/ContentManagerApiClients.g.cs) не найдены Activity methods/models; подтверждена только общая NSwag/DI основа §8. Перечисленные здесь page/entity blocks и DF read operations — целевой состав, не описание существующих классов. Проверка ограничена данным source tree/search, не является запуском UI или live OpenAPI audit.

| Вопрос | Какие детали блокирует |
| --- | --- |
| [Q10 history scope](../../../business/questions/Q10-permissions-and-isolation.md#f18-history-scope-и-visibility) | Permissions/capture granularity/retention MVP приняты; future export/archive/anonymization/security log policy отдельны |
| [Q13. UI scope](../questions/Q13-ui-scope.md) | Exact selection/back-navigation composition; standalone /admin/activity/filters/keyset/manual Refresh выбраны |
| [Q15 activity contract](../../backend/questions/Q15-api-and-source-code.md#f18-принятый-activity-contract) | Final literal taxonomy/typed metadata/DTO/cursor codecs/time input/error/reference carrier/NSwag methods; selected three routes/visibility/query/defaults не TBD |
| [Q20 capture/append/cursor](../../backend/questions/Q20-persistence-and-migration.md#f18-принятый-capture-append-и-cursor-read) | Physical owner intent/transport/append/read mapping/indexes/retained inbox/projection/source sequence validation; no UI storage choice |
| [Q24. Staff sign-in](../questions/Q24-admin-sign-in.md#принятый-протокол-identityaccess-и-восстановления-формы) | Chosen F16 request access/open-tab memory/Logout/account privacy protocol; final transport context carriers/deployment engineering |

Полные вопросы остаются у владельцев. MVP records retain all/no autopurge; это не legal-forever commitment. Export/archive/anonymization/security failed logs/realtime/poll/notifications/full before-after и второй object-card journal не добавляются. M06 хранит связи, не копию моделей/декомпозиции.
