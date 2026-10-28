# F06. Продукты и конкретные варианты: фронтенд

[Бизнес](../../../business/features/F06-product-catalog.md) · [Бэкенд](../../backend/features/F06-product-catalog.md) · [M03](../modules/M03-catalog.md) · [Q11](../../../business/questions/Q11-catalog-ownership.md)

Документ описывает целевой MVP UI по [Q13](../questions/Q13-ui-scope.md): .NET 10, Blazor Standalone, MudBlazor, FSD и NSwag. Имена будущих блоков — логические обозначения, не существующие классы. Факты исходников и отличия от цели приведены в §11; вёрстка не задаётся.

## 1. Результат и границы

Специалист находит Product и его ProductVariant, видит применённые CM названия и родительскую связь, отдельно newest source lifecycle и processing state, причины ограничений и доступные действия. Он переходит из каталога в соответствующую рабочую область или выбирает существующий допустимый ID для сценария назначения. Каталог CM — локальная read-only проекция VR School; создание, переименование и изменение родителя выполняются только в VR School. Нет CM Delete/Archive/Restore, записи через VR School API и переключателя локального lifecycle каталога.

F06 относится к MVP. Demo не зависит от F06, VR School, broker, лицензий, входа и ролей. Новый каталог поступает обычными source events; CM формы подключения/bootstrap/import/sync нет. Принятый [Q23 V2 protocol](../../backend/questions/Q23-source-synchronization.md#принятый-протокол-каталога-v2) разделяет durable receiver и worker: ACK/accepted version не applied/readiness. Reconciliation запускается только protected operational VR School boundary для selected IDs либо all source, включая parents; scheduled/error report не CM-ready result. UI не вызывает этот trigger, не назначает public HTTP route или permission literal.

Receiver durably accepted v22 уже исключает apply v21, даже до worker assessment v22. Существует один newest candidate; old AppliedState сохраняется целиком либо до первого apply отсутствует целиком. Accepted/current-assessment/fault/readiness показываются раздельно; obsolete Issues не fresh basis. Missing parent — durable waiting без TTL/fake Product, Archived parent — отдельное lifecycle restriction. Trusted identifiable higher-version unsupported/malformed message создаёт scoped technical IngestionFault/unreadiness, не F15 manual conflict/empty catalog; неизвестная identity не блокирует guessed entity. Fault alone не отзывает historical materialized Delivery.

ProductId/ProductVariantId приходят из VR School, неизменяемы и не переиспользуются. Один Variant остаётся общей identity для разных организаций и лицензий. Полные назначения, release/revision/policy и Delivery относятся к [F07](F07-product-content-assignment.md), [F08](F08-variant-content-assignment.md), [F09](F09-content-releases.md), [F10](F10-publication-revisions.md), [F11](F11-publication-lifecycle.md), [F12](F12-content-entitlement-policy.md), [F13](F13-content-delivery.md). Подробное разрешение конфликта принадлежит [F15](F15-catalog-conflicts.md).

## 2. Страницы, экраны и навигация

| Страница / экран | Точка входа и контекст | Состав и режим | Завершение / переход |
| --- | --- | --- | --- |
| SC01. Каталог продуктов | `/products`, sidebar «Каталог и публикация → Продукты» | ProductCatalogPage: server list/search/page, source/eligibility summaries; только чтение | Строка → SC02 по ProductId |
| SC02. Каталожная часть Product Workspace | `/products/{productId}`; варианты — `/products/{productId}/variants` | Read-only Product summary, applied state и ограничения; contextual Variant list во вкладке «Варианты» | Вариант → SC03; соседние вкладки владельцев F07/F09/F12; возврат → SC01 |
| SC03. Каталожная часть Variant Workspace | `/products/{productId}/variants/{variantId}`; строка, deep link, переход F15; pending-only entry Q15 | Applied Variant/parent либо wholly absent applied, source accepted/assessment/fault/readiness diagnostics, restrictions; read-only | Applied parent → SC02 только если есть; conflict → F15; technical fault отдельно, no manual resolve/sync |
| SC04. Выбор получателя | Контекст родительского сценария F07/F08, требуемый тип Product или Variant, текущий ID | ChooseCatalogRecipient: server search/page только eligible кандидатов и временный выбор; не отдельный route | Confirm возвращает подходящий ID родителю; Cancel сохраняет прежний выбор |

SC02/SC03 — общие workspaces, F06 описывает только их каталог и навигацию. Overview не дублирует полные content, release, history и policy экраны. Варианты не имеют отдельного sidebar item. Licenses не входят в основной specialist workflow F06.

Routes утверждены Q13; это целевые адреса, а не текущие `@page`. Breadcrumb label использует применённое Name, route identity — immutable ID. Прямое открытие и refresh читают exact Product/Variant, а не требуют предварительно загрузить список. Для старого URL после **применённого** переноса Variant серверное чтение по VariantId даёт актуальный applied ProductId, и page переводит на canonical URL нового Product. При pending/conflict новый source parent ещё не становится canonical parent. Ссылка из F15 сохраняет VariantId; неизвестный, недоступный либо pending-only объект не подменяется другой записью.

Search/filter/page/sort списка хранятся в query string согласно Q13. Поиск с debounce и pagination выполняются сервером; весь каталог для фильтрации в браузер не загружается. Точные query names, фильтры source/eligibility, сортировки и числовые параметры — [Q15](../../backend/questions/Q15-api-and-source-code.md#f06-детализация-read-контракта). SC04 владеет временным query и кандидатом, не переписывает URL родительской формы. Сохранение дополнительных UI-секций между refresh не утверждено.

## 3. Карта FSD-блоков

### 3.1. Границы уровней

Page владеет route/read model и связывает чтение с переходами; entity показывает данные каталога; самостоятельная feature нужна для подтверждённого выбора получателя. Отдельный widget списка, feature для каждого GET и глобальный catalog store не требуются. Предупреждение F15 переиспользуется как составной блок. `app` подключает маршруты и navigation shell; `shared` предоставляет транспорт и общие controls без каталожных правил.

### 3.2. Состав

| Блок | Слой / логическое размещение | Ответственность | Использование | Основное описание |
| --- | --- | --- | --- | --- |
| ProductCatalogPage | pages / product-catalog / ui, model | Route/query списка, запрос и переход по ID | Изменить текущую каталоговую композицию для SC01 | §3.3 |
| ProductWorkspace: CatalogSection | pages / product-workspace / ui, model | Exact Product и contextual variants в SC02 | Каталожная часть общего workspace | §3.3; соседние части F07/F09/F12 |
| VariantWorkspace: CatalogSection | pages / variant-workspace / ui, model | Exact Variant, applied parent и canonical route в SC03 | Каталожная часть общего workspace | §3.3; соседние части F08/F10/F11/F12 |
| CatalogRecordView / catalog read operations | entities / product, product-variant / ui, api | Generated Product/Variant read models: ID/Name/Description/parent/lifecycle, accepted/applied/assessment/fault/readiness и eligibility | SC01–SC04; no duplicate DTO/VM/source form | §3.3 |
| ChooseCatalogRecipient | features / choose-catalog-recipient / ui, model | Поиск только eligible candidates, Confirm/Cancel допустимого ID | SC04 у родительского сценария | §3.3 |
| Предупреждение о конфликте | widgets / каталоговый конфликт | Persistent blocker и переход с identity | Переиспользовать в SC02/SC03; локальный compact summary | [F15: карта блоков](F15-catalog-conflicts.md#карта-блоков-и-состояния) |
| Общие controls/API/error presentation | shared / ui, api | Подписи, loading/error/retry, диалог и HTTP | Переиспользовать существующие средства | [S05](../../../catalog/docs/standards/ui/forms-and-accessibility.md#form-state-and-user-feedback), [S06](../standards/S06-api-client.md); §8 |

### 3.3. Публичные обязанности блоков

| Блок | Входы / назначение | Выходы и получатель | Собственное состояние | Данные / зависимости / состав | Жизненный цикл |
| --- | --- | --- | --- | --- | --- |
| ProductCatalogPage | Route/query; найти Product без изменения источника | Переход по selected ProductId в SC02 | Query, loaded page, pending read/error | Catalog read, CatalogRecordView, shared controls | Загрузка при входе/query change; уход отменяет чтение; поздний ответ старого query не применяется |
| ProductWorkspace: CatalogSection | ProductId и route section; показать applied Product и его Variants | Выбранный VariantId → SC03; conflict identity → F15 | Exact Product/read status; variant query/page только этой области | DF04/DF05, CatalogRecordView, предупреждение F15; соседние workspace части получают общий ProductId | ProductId change сбрасывает прежний context; после возвращения из F15 перечитать affected read model |
| VariantWorkspace: CatalogSection | VariantId и ProductId из URL; показать применённую связь | Canonical navigation по applied parent; переход к Product/F15 | Exact Variant/read status и разрешённый server context | DF06, при необходимости DF04; CatalogRecordView, предупреждение F15 | Чтение по VariantId при входе; parent URL проверяется по applied result; новый source snapshot сам по себе не переписывает маршрут |
| CatalogRecordView / read operations | Generated Product или Variant, полученные server metadata/ограничения, локальная настройка показа | Навигационное намерение по переданному ID; read result владельцу запроса | Не хранит конкурирующую mutable business-модель; локальное раскрытие diagnostics допустимо | NSwag-клиент и generated classes; view не вычисляет eligibility/conflict | При смене входа показывает новую model; source/pending/applicable различаются по контракту, не по догадке |
| ChooseCatalogRecipient | Тип получателя, context/eligibility родителя, исходный ID; выбрать один актуальный допустимый объект | Confirm: typed ProductId либо ProductVariantId → callback родителя; Cancel: без изменения | Временный query/page/candidate и read/error; исходное значение принадлежит родителю | Catalog read, CatalogRecordView, общие dialog/search controls; серверная eligibility | При открытии читает current candidates; при закрытии очищает временный выбор; устаревший ответ не меняет новый context |

F15-предупреждение получает server conflict identity/scope/reasons и freshness/current-assessment marker. F06 настраивает F15/stable Variant navigation, не копирует comparison/repair form. IngestionFault показывается отдельной read diagnostic той же catalog view, не этим conflict widget и не новым resolution action. Общий UI получает message/loading/retry/callbacks, не вычисляет Product/grant readiness.

## 4. Логическая компоновка

```text
SC01. ProductCatalogPage [pages; query/read state]
  Поиск и pagination [shared/ui]
  Product list [локальная композиция page]
    CatalogRecordView(Product) [entities]
  Loading / empty / error / retry [shared/ui]

SC02. ProductWorkspace [pages; ProductId/exact read]
  Breadcrumb и CatalogRecordView(Product) [entities + shared]
  Source state / CM eligibility [серверные сведения]
  Предупреждение F15 [widgets; условно]
  Вкладка «Варианты» [локальная композиция; query/page]
    CatalogRecordView(Variant) [entities; applied ProductId]
    Loading / empty / error / retry [shared/ui]
  Соседние workspace sections [F07/F09/F12; их собственные действия]

SC03. VariantWorkspace [pages; VariantId/exact read]
  Breadcrumb / applied parent link [entities + shared]
  CatalogRecordView(Variant) [entities; applied state]
  Source state / CM eligibility [серверные сведения]
  Предупреждение F15 [widgets; условно]
  Неприменённое newest source изменение [условно; отдельно от applied]
  Соседние workspace sections [F08/F10/F11/F12]

SC04. Родительский сценарий назначения [владеет saved recipient]
  ChooseCatalogRecipient [features; временный выбор]
    Поиск / server page [shared + catalog read]
    CatalogRecordView(Product либо Variant) [entities]
    Eligible кандидат / Confirm / Cancel; причина утраты eligibility у ранее выбранного ID [локальная композиция]
```

| Связь частей | Передаваемые сведения | Координатор | Наблюдаемый результат |
| --- | --- | --- | --- |
| SC01 → SC02; SC02 → SC03 | Immutable IDs из выбранной applied строки | Page navigation | Exact workspace, не выбор по Name |
| Exact read → summaries и F15 | Единая generated model, ограничения/conflict identity | Product/Variant Workspace | Имя, parent, причины относятся к одному подтверждённому контексту |
| ChooseCatalogRecipient → родитель | Typed выбранный ID по Confirm | Родительский сценарий | Его временный выбор обновляется; assignment/save выполняет владелец F07/F08 |
| F15 → affected workspace | Возврат с сохранённой object identity | Workspace | Повторное чтение меняет представление лишь по подтверждённому applied state |

Смена вкладки заменяет её рабочую область, summary/identity остаются контекстом workspace. SC04 открывается отдельно от исходной формы; отмена не меняет её получателя. Деревья задают UI-вложенность, а не разрешение dependencies между соседними features.

## 5. Зависимости и публичные границы

Pages используют catalog read и предметные views через публичные generated model/read interfaces и navigation/callbacks. ChooseCatalogRecipient получает исходный ID/context и возвращает результат без ссылки на класс родительской страницы. F07/F08 сохраняют назначения своими операциями; F06 не вызывает их mutation скрыто при выборе. F15-предупреждение сообщает navigation intent верхнему владельцу, который переводит в conflict workspace.

`entities` — клиентские representations, не Domain/EF/серверные агрегаты. Web UI не получает references на серверные Domain/Persistence и не обращается к VR School DB/API для управления каталогом. Shared не зависит от pages/features и не хранит business eligibility. Совместная координация остаётся у workspace, циклы и вызов внутренних типов соседних срезов не нужны.

Публичное расширение catalog view/picker затрагивает SC01–SC04 и потребителей F07/F08; изменение общего conflict notification проверяется также по F15. Точные namespaces будущих блоков не назначаются до реализации. Наличие текущей папки Catalog не является принятым target API всех слоёв.

## 6. Данные и владение состоянием

### 6.1. Источник и область жизни

| Данные / состояние | Источник истины | Клиентский владелец | Обновление / сброс |
| --- | --- | --- | --- |
| Applied Product/Variant и parent | CM read model DF03–DF06, применившая VR School snapshot | Page/workspace, generated model напрямую | Вход, query/ID change, явный refresh, возврат после F15 |
| Source lifecycle | VR School → CM projection: только Active/Archived; mapping Q23 | Полученное own lifecycle и отдельно ограничение parent | Новый server read; Hidden — presentation, не lifecycle/toggle |
| Accepted/applied/assessment/fault/readiness, eligibility и conflict | Backend F06 public diagnostic/read contract Q15/Q23 | Workspace generated read, отдельный F15 summary | Safe refresh; accepted watermark != worker/applied version, failure не empty/current Issues |
| List query/page/selected ID | URL для SC01/SC02 списка; временный query SC04 | Page либо ChooseCatalogRecipient | Navigation/back; SC04 reset при закрытии |
| Read loading/error, раскрытие diagnostics | Локальный presentation-state | Блок, выполняющий чтение | Завершение/отмена запроса; новый context |
| Подтверждённый получатель | Родительский сценарий F07/F08 | Родитель; picker хранит только кандидат | Confirm либо серверный результат родительского действия |

Глобальный store, browser storage, offline и autosave каталога не вводятся. Бизнесовые поля не копируются в ручной CatalogRow/ViewModel. В read-only F06 отсутствуют editable source fields и несохранённый CRUD-черновик; SC04 имеет временный выбор, а восстановление authoring form после повторного входа принадлежит F16/родительской фиче и Q24.

### 6.2. Состав представления

| Сведения | Использование | Правило |
| --- | --- | --- |
| ProductId / ProductVariantId, applied Name/Description | Ссылка/summary/read-only source caption | V2 Product Name1–120, Variant Name1–200; optional Description≤500. Source значения сохраняются, CM не trim/rename/edit; UI field form/CM mutation не вводится |
| Applied ProductId и parent Name | Breadcrumb и родительская связь Variant | Показывается applied parent; missing parent не заменяется фиктивным Product |
| Own source lifecycle, parent restrictions, CM eligibility/reasons | Active/Archived и допустимость новых действий | Archived parent запрещает новые операции, не меняя own Active Variant; eligibility получает сервер |
| Applied state/version; newest accepted version/candidate; current assessment; IngestionFault/readiness | Рабочие source fields/parent отдельно от durable accepted horizon и worker decision | ACK/version acceptance не apply; v22 rejects21 before worker; prior Issues stale до assessment newest. Applied wholly old/absent, no partial merged fields. Pending/Conflict/fault не lifecycle; exact diagnostic models backend F06 once |

Если Variant пришёл раньше Product, identity/newest candidate сохраняются, AppliedState целиком отсутствует до first apply; новый recipient недоступен. Exact stored-identity read200 показывает waiting, не404/fake applied fields/Product. Entry/DTO diagnostic representation при отсутствии canonical applied parent — Q15. Waiting durable без TTL/DLQ; resolver может продолжить без нового external message после появления parent. Archived parent показывается отдельно. Если уже есть Open episode, новый missing-parent target сохраняет тот же Open conflict + waiting/current-assessment state.

MVP принимает перенос с тем же VariantId при подтверждённой совместимости. Несовместимость сохраняет whole old AppliedState, один newest pending snapshot и F15 blockers affected new use. New accepted snapshot обновляет target того же Open episode; старый Issues assessment не маркируется свежим. Compatible source correction может атомарно full-apply newest и закрыть episode как System SourceChangedApply: это отдельный source result, не пользовательский repair/Resolve success. Оно не публикует/autoresume и не переписывает immutable history. Historical releases/revisions сами по себе не move blockers; UI показывает concrete mutable/in-flight closure. Override A→B без A в prospective base сохраняет B local, independent B + override result B даёт duplicate blocker. Archive/conflict сами не Unpublish/Withdrawal/revoke.

Archived Product/Variant доступны через exact/history read с причиной и разрешёнными действиями, но исключены из обычного нового выбора. Archived parent ограничивает own Active Variant, не перезаписывает его lifecycle. Разрешены чтение, сохранение неизменных references, removal и eligible replacement проблемных references без расширения состава; inherited-only exclusion считается удалением, replacement override — заменой. Open F15 отдельно запрещает ordinary affected writes; исправление выполняется отдельным corrective workflow F15, а не обычным Save.

Архив запрещает новые releases/revisions, обычную activation, rollback и automatic promotion. Safety actions (Unpublish, explicit Withdrawal) сохраняются по правам и scope. Уже materialized revisions и ранее подтверждённые entitlement допускают историческую Delivery, включая новые sessions, если нет отдельного запрета F12/F13. Restore приходит новым source snapshot: новые операции требуют own Active, доступного Active parent и отсутствия остальных blockers; нет auto activation, unwithdraw или grant.

## 7. Действия и клиентские потоки

| Действие | Начало / исполнитель | Обработка | Результат |
| --- | --- | --- | --- |
| UI01. Найти Product | SC01, ProductCatalogPage | Query в URL, debounce search, DF03; cancel/ignore старого запроса | Server page; пустой результат поиска отличим от ошибки |
| UI02. Открыть Product и варианты | SC01/SC02/deep link, ProductWorkspace | DF04 и DF05 с ProductId filter; error variants не скрывает безопасный Product summary | Applied Product и его contextual список |
| UI03. Открыть Variant | SC02/deep link/F15, VariantWorkspace | DF06 по VariantId; canonical URL проверяется по applied relation | Applied name/parent и отдельный blocker/pending summary |
| UI04. Выбрать получателя | SC04, ChooseCatalogRecipient | Чтение current candidates; Confirm допустимого ID; Cancel локально, без mutation | Callback родителю; source catalog и assignment ещё не изменены |
| UI05. Перейти к конфликту и вернуться | SC02/SC03, page + reused F15 notification | Передать conflict/object ID; resolution выполняет F15; после возврата перечитать affected exact/list | Ограничение снимается лишь по server result |
| UI06. Повторить чтение | Ошибка/refresh текущей области, её владелец | Тот же актуальный query/ID, DF03–DF06 | Подтверждённое обновление либо понятная ошибка с прежним безопасным context |

Обычные selectors показывают только eligible candidates. Exact workspace сохраняет ineligible identity с server reason и доступными действиями; список ineligible объектов не превращается в каталог selectable disabled rows. Новый выбор содержит только current applied доступные server-eligible IDs. Если выбранный кандидат при refresh стал недоступен, Confirm блокируется с причиной; другой ID не подставляется. Уже существующее историческое назначение сохраняет identity и контекст даже если объект больше не предлагается для нового выбора. Picker не меняет исторический recipient на active соседний объект. Допустимость итогового назначения вновь проверяет сервер родительской операции, включая изменения между Confirm и Save.

UI не обещает realtime каждого broker event и не вызывает receiver/reconciliation/worker. Retry доставки same EventId и equal-version/new-EventId/same normalized state no-op — server semantics, не кнопки страницы; contradictory state quarantined без overwrite. Internal pending resolver не требует нового external message. Operational reconciliation report не переводит page в ready; readiness только fresh CM read. Query/route/candidate change fences late replies; refresh повторяет read, неsource action.

## 8. Использование API и NSwag

| Потребность | Backend F06 / целевая операция | Вход | Результат / владелец |
| --- | --- | --- | --- |
| UI01; Product candidates UI04 | DF03, `GET /api/v1/products` | Согласованные search/filter/page/sort | Generated Product list, pagination; ProductCatalogPage / picker |
| UI02, Product summary; applied parent UI03 | DF04, `GET /api/v1/products/{productId}` | Exact ProductId | Generated exact Product/read state; workspace |
| UI02; Variant candidates UI04 | DF05, `GET /api/v1/product-variants`, ProductId filter | Applied ProductId по контексту и согласованный query | Generated Variant page/eligibility; ProductWorkspace / picker |
| UI03/возврат UI05/refresh UI06 | DF06, `GET /api/v1/product-variants/{productVariantId}` | Stable ProductVariantId | Exact applied relation и договорённые restrictions/pending; VariantWorkspace |
| Источник read model/diagnostics | Backend F06 receiver/worker + DF01/DF02 source snapshots; DF07 operational reconciliation | Trusted source identity/version/state, no browser source input | Own public read diagnostics; models/новые logical DF only backend F06; UI не consumer/producer/operational trigger |

Пути таблицы — agreed target F06 read surface, не newly implemented API. List `ApiResponse<OffsetPagedResultDto<T>>`, exact `ApiResponse<T>`; exact generated methods/query/caption/accepted-applied-assessment-fault/readiness carriers и OpenAPI — [Q15](../../backend/questions/Q15-api-and-source-code.md#f06-детализация-read-контракта). Diagnostic/current candidate models определяет backend F06 один раз. Stored pending-only identity read successful с absent applied/waiting; technical fault не false empty/404/manual conflict. Reconciliation protected operational VRS-only, no selected public HTTP route/permission/CM POST. UI не заменяет exact endpoint whole-list lookup/generated method fiction.

Текущий клиент — `ContentManager.ApiClient.Generated.ICatalogClient`; [`nswag.json`](../../../../contentmanager.ui/src/ContentManager.ApiClient/nswag.json) задаёт `{controller}Client`, generated interfaces/DTO, injected HttpClient, `SystemTextJson`, output `OpenApi/Generated/ContentManagerApiClients.g.cs`. Клиенты подключаются через [`AddContentManagerApiClients`](../../../../contentmanager.ui/src/ContentManager.ApiClient/DependencyInjection/ApiClientServiceCollectionExtensions.cs) с scoped lifetime. Target exact/read contracts сначала отражаются серверным OpenAPI и общей генерацией; generated-код вручную не изменяется. NSwag models используются напрямую, отдельны только presentation flags/query/кандидат.

F06 читает внешнюю проекцию и не изменяет exact mutable resource: обязательный `If-Match`/mutation ETag и формы optimistic concurrency F04/F05 здесь не нужны. SourceVersion служит серверному ordering snapshots, не токеном HTTP клиента и не вычисляемым браузером признаком applied успеха. Наличие SourceRevision в current generated DTO не утверждает target naming/version semantics. Серверные сообщения отображаются в области соответствующего чтения; полный DTO-файл фронтенд не дублирует.

## 9. Состояния, ошибки и доступ

| Ситуация | Наблюдаемое поведение | Действия / восстановление |
| --- | --- | --- |
| Initial loading | Область loading, без ложного «каталог пуст» | Дождаться/уйти; selection не подтверждается без current eligible data |
| Refresh | Сохранённое безопасное представление с признаком обновления | Не стирать весь workspace из-за отдельного list error; результат только текущего context |
| Нет обработанных записей | Каталог поступает из VR School; записей пока нет | Refresh; никаких Create/connect/import в CM |
| Поиск без совпадений | Показать отсутствие совпадений для текущего query | Изменить/reset query; не называть это отсутствием source каталога |
| Parent ещё не разрешён | Pending/unavailable с причиной по server contract | Чтение/повтор; недоступен новый recipient; нет фиктивного parent или TTL обещания |
| Accepted newer, assessment pending | Applied state целиком прежний/absent; newest accepted version отдельно, старый Issues marked stale | Safe refresh/read; no v21 use permit after accepted22, no synthetic ready from ACK |
| Scoped IngestionFault | Higher identifiable unsupported/malformed input: technical readiness unavailable, retained applied отдельно | No manual F15 Resolve/new conflict/empty-success; affected new use blocked; no guessed block for unknown identity |
| Compatible source correction | Full apply+Resolved System SourceChangedApply по server result | Reread canonical applied context/history, not user Resolve success; no publish/autoresume |
| Несовместимый перенос | Applied state и persistent F15 banner; incoming pending state отдельно | Перейти к F15; зависимые assignment/release/new revision/activation/promotion/policy действия ограничены в affected scope |
| Archived own/parent; Hidden presentation | Own Active/Archived, parent restriction и причина отдельно от Pending/Conflict | Exact/history читаются; selector исключает; limited maintenance/safety по scope, release/revision/activation/rollback/promotion blocked |
| Source restore | Новый source Active snapshot и актуальные blockers | Новые действия только own Active + eligible parent + no other blocker; без autoactivation/unwithdraw/grant |
| 404 exact object | Объект не найден, context ID сохранён | Назад/повтор; не выбирать первую строку списка |
| 403 | Явный запрет доступа, не пустой каталог | Разрешённая навигация; backend authority обязательна |
| Требуется повторный вход | Сообщение auth flow MVP | F16/Q24; после входа перечитать каталог, не подтверждать старого кандидата автоматически |
| Transient API/network failure | Error/retry в affected list/detail/picker | Повтор чтения сохраняет query/ID; ошибка не означает deletion/conflict resolution |
| Кандидат утратил eligibility | Показать прежний ID и конкретную причину | Confirm недоступен, исходный recipient родителя не заменяется |

Unresolved catalog conflict не блокирует чтение, resolution, историческую Delivery materialized revisions по ранее подтверждённому entitlement, включая новые sessions, сам по себе; withdrawal/revoke относятся к отдельным основаниям. UI не показывает pending перенос как успешное изменение рабочей базы. Disabled actions получают explanation; разрешения не обеспечиваются одним скрытием кнопки.

Основные действия и ссылки доступны с клавиатуры; labels/reasons не передаются только цветом. При открытии/закрытии SC04 управляется фокус и возврат к его инициатору. Source, applied, pending и historical контекст подписаны по смыслу. Подробности доступности — [S07](../../../catalog/docs/standards/ui/forms-and-accessibility.md#access-and-semantics); общий вид сообщений — [S05](../../../catalog/docs/standards/ui/forms-and-accessibility.md#form-state-and-user-feedback).

## 10. Проверки и приёмка

| Блок / сценарий | Условия и действие | Наблюдаемый результат | Граница проверки |
| --- | --- | --- | --- |
| SC01 + UI01 | API double: страницы, поиск без совпадений, transient error | Правильный query/page; error отдельно от empty; не загружен весь каталог | Компонент/композиция и API interaction |
| SC02 + UI02 | Product и несколько Variants; вариантный запрос с ProductId | Applied parent/grouping и stable links; независимая ошибка variants | Workspace composition с API double |
| SC03 + UI03 | Прямой URL; applied Variant переехал в другой Product | Чтение по VariantId, canonical redirect на applied parent; identity сохранена | Navigation/API interaction, затем реальный browser path |
| Applied / pending | Старый applied parent/name и newest несовместимый source snapshot | Старые значения не заменены pending; banner ведёт к exact F15 context | Generated contract fixtures + component composition |
| Variant-before-Product | Pending identity, parent отсутствует; затем parent разрешён | Нет fake Product/new recipient; после confirmed read применённая связь видима | UI contract check; broker/проекция проверяются backend |
| ChooseCatalogRecipient | Одинаковое Name у разных IDs; Product/Variant contexts; Confirm/Cancel | Правильный typed ID; Cancel сохраняет исходное значение; нет mutation | Feature и родительская композиция F07/F08 |
| Eligibility refresh | Кандидат Archived/conflicted либо parent Archived между чтениями/Save | Selectors только eligible; утраченный selection с причиной/blocked Confirm, exact workspace доступен; server refusal не success | API interaction F06 + затронутый потребитель |
| Lifecycle/read separation | Own Active Variant + Archived parent; newest lifecycle при unapplied relation; restore с other blockers | Own lifecycle не overwritten; applied/newest/processing различимы; нет synthetic merge/autoactivation | Generated read contract + workspace/browser |
| Archive maintenance/publication | Unchanged refs/remove/eligible replacement; Add/expansion/release/revision/activation/rollback/promotion; safety | Limited maintenance/read и safety разрешены по scope; expansion/ordinary publishing запрещены | F06 consumer guards + real backend |
| Existing entitlement/new session | Archived recipient, materialized revision, previously confirmed entitlement | History/Delivery включая новую session сохраняются; separate Withdrawal/revoke действует отдельно | Реальный Delivery/entitlement контур |
| Existing historical context | Назначение/revision на Variant, скрытый из нового выбора | History и identity остаются; нет автоматического revoke/withdrawal/замены | Workspace + соседние read views; delivery semantics backend |
| UI05 | Переход F15, возврат после ошибки/успешного resolution | Banner исчезает только после confirmed server state; affected views перечитаны | F06/F15 composition |
| Navigation/read race | Смена query/ID; поздний старый ответ; 403/404/401 | Новый context не затёрт; разные ошибки; повторный вход не подтверждает выбор | Клиентская логика и API double |
| Source → UI [MVP] | Реальные VR School/broker/CM: новый Product/Variant, duplicate/stale/newer snapshot, compatible/conflicting move | Единственная identity, актуальное applied read либо pending/F15, источник не редактируется в CM | E2E реального integration/browser контура, не цепочка mocks |
| Acceptance/assessment/readiness | Durable accepted22, worker ещё v21/old Issues; later assessment | Versions/stale Issues distinct, v21 never shown eligible basis; no ACK=apply/ready | UI contract fixtures + real receiver/worker barriers |
| Fault scope | Trusted entity+higher version malformed/unsupported versus unknown identity | Scoped technical unreadiness без new F15/empty; retained historical Delivery unaffected alone, unknown identity не guessed entityblock | Backend/F06 diagnostic fixtures + UI |
| Source correction/pending resolver/reconciliation | Open move→compatible source; parent delayed; operational report success/errors | SourceChangedApply/history distinct from user Resolve; internal continuation possible without newmessage; report не ready, no CM sync button | Real source/worker + read composition |
| V2 fields | Name Product1–120/Variant1–200, optionalDescription≤500, source rename/change | Read-only preserved source values, no CM field transformation/mutation | Source/read fixtures, not UI source validation form |
| Отрицательная поверхность и доступность | SC01–SC04, клавиатура; несколько организаций используют Variant | Нет Create/rename/Delete/Archive/Restore/VR School write; один Variant; понятные причины и возврат фокуса | UI composition/browser; права отдельно backend |

Проверки используют [S08](../standards/S08-testing.md) и не выбирают новую test library. Broker ordering/idempotency, отсутствие TTL/DLQ для обычного pending и сохранность projections — обязанности backend F06, их UI-double не доказывает. Demo проверяется независимо от F06/VR School/broker/licenses. Этот раздел — критерии целевой реализации, не отчёт о запуске приложения.

## 11. Последовательность изменений и открытые вопросы

### 11.1. Факты исходников и границы подтверждения

- [`CatalogPage.razor`](../../../../contentmanager.ui/src/ContentManager.Pages/Components/Catalog/CatalogPage.razor) использует `/catalog/{Kind}` и `ICatalogClient`; режимы products/variants/licenses, search/page в query, `includeDeleted`, Variant filter `productId`. Source fields read-only; кнопок локального CRUD нет.
- [`NavMenu.razor`](../../../../contentmanager.ui/src/ContentManager.Widgets/Components/Common/NavMenu.razor) сейчас имеет отдельные products/variants/licenses links. [`PublicationWorkspacePage.razor`](../../../../contentmanager.ui/src/ContentManager.Pages/Components/Publishing/PublicationWorkspacePage.razor) использует `/publishing/products/{OwnerId:guid}` и `/publishing/variants/{OwnerId:guid}`. Это current routes, не принятые contextual routes Q13; canonical redirect после applied move данными этих страниц не подтверждён.
- [`Generated client`](../../../../contentmanager.ui/src/ContentManager.ApiClient/OpenApi/Generated/ContentManagerApiClients.g.cs) предоставляет `ICatalogClient.ProductsAsync`/`VariantsAsync` → `api/v1/catalog/products`/`api/v1/catalog/variants` с skip/take/search/includeDeleted/productId и `CatalogProductDto`/`CatalogVariantDto`. Модели содержат Id, Name, Description, SourceRevision, IsDeleted; Variant дополнительно ProductId. В `ICatalogClient` нет exact Product/Variant read методов DF04/DF06; lifecycle/CM eligibility/newest pending/applied comparison contracts этих DTO не подтверждены.
- Current CatalogPage создаёт private `CatalogRow` и для variants последовательно загружает все страницы products в `_products`. Target использует generated models напрямую, server search/page и exact parent read; полный каталог в браузере и business model duplication не переносятся как договорённость.
- `SynchronizationAsync`, `BootstrapAsync` и license history metadata присутствуют в current client. Current `BootstrapAsync` не переносится в target: отдельный bootstrap F06 не требуется, DF07 reconciliation не утверждает его замену выбранным CM endpoint. Эти методы/metadata не доказывают контракт pending-parent/move F06 и не вводят обязательные licenses/readiness зависимости. Текущий `IsDeleted` не выбирает normalized lifecycle enum либо local Archive/Restore. Наличие API/UI чтения не является доказательством законченного target broker/conflict flow.

### 11.2. Порядок и вопросы

Сначала уточнить backend exact/list read contract и applied/newest pending/eligibility representation, затем отразить контракт в OpenAPI и штатной NSwag-генерации. Переиспользовать shared transport и F15 notification, собрать read-only pages/sections и выбор получателя с прямыми generated models. Подключить принятые Q13 routes/query/canonical navigation, согласовать координацию с F07/F08/F15 и проверить §10. Исходники и generated-код этим документом не изменяются.

| Вопрос | Что уточняется |
| --- | --- |
| [Q15: F06 read contract](../../backend/questions/Q15-api-and-source-code.md#f06-детализация-read-контракта) | Exact/list DTO, согласование current OpenAPI с target GET routes, query/sort, eligibility/reasons/conflict references, pending-only representation и отсутствие applied parent |
| [Q23: V2 protocol](../../backend/questions/Q23-source-synchronization.md#принятый-протокол-каталога-v2) | V2 fields/receiver-worker/newest horizon/one candidate/fault/missing-parent/source-correction/reconciliation ownership selected; exact wire normalization/DTO/registrations/configurable technical work+receipt retention engineering |
| [Q27](../../../business/questions/Q27-catalog-archiving.md) | Согласованные Active/Archived, parent restriction, maintenance, safety и restore; Hidden только presentation. Конкретная DTO/error форма — Q15/Q23 |
| [Q10](../../../business/questions/Q10-permissions-and-isolation.md), [Q24](../questions/Q24-admin-sign-in.md#принятый-протокол-identityaccess-и-восстановления-формы) | F16 IdentityAccess/fresh per-request checks и open-tab same-user recovery selected; exact permission/context carriers engineering; Demo без входа |
| [Q25](../../../business/questions/Q25-catalog-conflicts.md), [F15](F15-catalog-conflicts.md) | Согласованный corrective repair/отдельный Resolve; детали confirmation/rights/wire и координация newest source state; F06 не создаёт вторую resolution form |

Ownership Q11, MVP move с сохранением identity, read-only CM, отсутствие bootstrap и Demo зависимости уже определены. Routes/navigation/state ownership определены Q13 и не возвращаются в список неопределённых только из-за отставания current code. Применимые соглашения: [S05](../../../catalog/docs/standards/ui/forms-and-accessibility.md#form-state-and-user-feedback), [S06](../standards/S06-api-client.md), [S07](../../../catalog/docs/standards/ui/forms-and-accessibility.md#access-and-semantics), [S08](../standards/S08-testing.md).
