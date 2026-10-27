# F07. Общий контент Product: фронтенд

[Бизнес](../../../business/features/F07-product-content-assignment.md) · [Бэкенд](../../backend/features/F07-product-content-assignment.md) · [M04](../modules/M04-publishing.md) · [Q13](../questions/Q13-ui-scope.md)

Документ описывает целевой MVP UI Content Manager: .NET 10, Blazor Standalone, MudBlazor, FSD и NSwag. Имена будущих блоков — логические обозначения, не существующие классы. Факты исходников отделены от цели в §11; вёрстка не задаётся.

## 1. Результат и границы

Специалист в Product Workspace прежде всего назначает схему, явно выбирает Published версию структуры и режим экземпляров: «Все экземпляры», «Все, кроме выбранных», «Только выбранные». Назначения показаны деревом Schema → Instances. Точечный exact-version picker используется внутри схемы для SelectedOnly, а не как единственный вход в назначение. AllExcept подхватывает новые Published данные при подготовке следующего release внутри фиксированной структуры; SelectedOnly сохраняет exact pins. Existing releases/revisions не меняются. [Q39](../../../business/questions/Q39-schema-content-selection.md).

Product и исходные поля принадлежат VR School и доступны только для чтения по [F06](F06-product-catalog.md). Save меняет mutable authoring state. Он не создаёт release/revision/activation/moderation item и не меняет Delivery или исторические snapshots. ProductRelease принадлежит [F09](F09-content-releases.md), Variant local content/exclusions/override — [F08](F08-variant-content-assignment.md).

F07 — полный Publishing MVP; Demo не зависит от Product assignment, VR School, broker и остальных механизмов полного Publishing. Для нового использования Draft, Retired, Withdrawn и контент под Archived logical owner/parent недоступны. Сервер проверяет lifecycle. Exact references, уже сохранённые и позднее ставшие Retired/Withdrawn/Archived/unavailable, разрешено сохранить без изменения: UI удерживает их ID, показывает диагностику и блокировку нового release. Add, новая позиция или замена exact reference — новое использование и требуют текущей eligibility; перестановка отображения новым использованием не считается. Silent cleanup и latest запрещены. При Archived Product разрешено ограниченное maintenance: удалить references либо заменить проблемные references на eligible, без additions и расширения состава. CatalogConflict F15 продолжает блокировать обычные writes; этот режим не обходит F15. Corrective workflow отдельно описан в F15 и имеет собственные permission/basis/guards; F07 Save не является repair. Принятое правило связано с [Q07](../../../business/questions/Q07-exclusions-and-local-content.md#f07-допустимость-рабочих-references).

## 2. Страницы, экраны и навигация

| Страница / экран | Точка входа и контекст | Состав и режим | Завершение / переход |
| --- | --- | --- | --- |
| SC01. Product Workspace, «Общий контент» | `/products/{productId}/content`; каталог F06, вкладка либо deep link | Read-only Product summary; рабочий assignment; группы EntitySchema; локальные изменения/Save; persistent blocker F15 | Save остаётся на вкладке; соседние overview/releases/variants/policy принадлежат общему workspace |
| SC02. Выбор Published версии | «Добавить» или «Заменить версию» строки SC01 | Logical instance → exact Published versions → явный выбор; Replace фиксирует logical owner | Confirm возвращает exact choice SC01; Cancel не меняет набор |

Route принят Q13, это цель, а не текущий `@page`. Общая каталожная часть workspace описана F06; отдельная глобальная Assignment page не нужна. Breadcrumb показывает Name, route использует ProductId. Прямой вход/refresh читает exact Product и assignment без предварительного списка. Неизвестный Product не заменяется первым доступным. Отсутствие assignment не является подтверждённым пустым набором и не вызывает UI-create.

Смена ProductId, уход и смена вкладки проходят guard dirty-изменений. Для browser refresh/закрытия применяется доступное браузеру предупреждение; восстановление формы после reload не обещается. Нет autosave/offline/browser storage. Возврат из F15 не затирает dirty набор. SC02 хранит временный query/page и не меняет URL родителя. Поиск кандидатов с debounce и pagination серверные; полный каталог не загружается для локального поиска. Query names, фильтры/limits — Q15.

## 3. Карта FSD-блоков

### 3.1. Необходимые уровни

Page координирует Product context/navigation. Widget объединяет saved baseline, локальный набор, picker и Save. Самостоятельные features — exact выбор и сохранение; Remove остаётся локальным действием widget. Entity представляет reference и публичное чтение F04. `app` подключает принятую вкладку/route, shared — controls/transport/guard без правил Publishing. Global assignment store не требуется.

### 3.2. Состав

| Блок | Слой / логическое размещение | Ответственность | Использование | Основное описание |
| --- | --- | --- | --- | --- |
| ProductWorkspace: CommonContentSection | pages / product-workspace / ui, model | ProductId, каталожный context и переходы | Изменить композицию SC01 | §3.3; каталог [F06](F06-product-catalog.md#3-карта-fsd-блоков) |
| ProductContentAssignment | widgets / product-content-assignment / ui, model | Whole-set state, baseline/ETag, группы и действия | Создать целевую секцию SC01 | §3.3 |
| SchemaSelectionTree | entities / schema-content-selection / ui | Generated schema rules/preview, policy, counts и child rows | F07/F08; expansion/paging UI-state, без business-копии DTO | §4 |
| SelectPublishedSchemaVersion | features / select-published-schema-version / ui, model, api | Exact Published structure selection через public F03 reads | Confirm generated exact target → assignment widget; Cancel без изменения | §4 |
| SelectPublishedInstanceVersion | features / select-published-instance-version / ui, model | Явный exact выбор Add/Replace | SC02; возможный потребитель F08 через public interface | §3.3 |
| SaveProductContentAssignment | features / save-product-content-assignment / model, api | Whole-set PUT с assignment If-Match | SC01/guard Save | §3.3 |
| ContentReferenceView / instance reads | entities / entity-instance / ui, api | Name/Key, exact version/schema context/availability | SC01/SC02; public reads F04 | §3.3; [F04](F04-entity-instances.md#8-использование-api-и-nswag) |
| CatalogRecordView и предупреждение F15 | entities / product; widgets / каталоговый конфликт | Read-only Product/persistent blocker | Переиспользовать в SC01 | [F06](F06-product-catalog.md#3-карта-fsd-блоков), [F15](F15-catalog-conflicts.md#карта-блоков-и-состояния) |
| Controls, guard, API/errors | shared / ui, api | Диалог/loading/errors/transport metadata | Переиспользовать; расширение общей инфраструктуры §8 | [S05](../standards/S05-ui-and-forms.md), [S06](../standards/S06-api-client.md), [S07](../standards/S07-accessibility.md) |

### 3.3. Ответственность и публичные интерфейсы

| Блок | Назначение / входы | Выходы и получатель | Собственное состояние | Данные, публичные зависимости и состав | Жизненный цикл |
| --- | --- | --- | --- | --- | --- |
| CommonContentSection | ProductId/route; общий контент в applied Product context | ProductId/ограничения → widget; navigation intent → shell | Exact Product/read status/route | Public F06 reads/views; assignment widget; F15 banner; shared navigation/guard | Exact read при входе; ID change после guard; ответ прежнего ID не применяется |
| ProductContentAssignment | ProductId/server restrictions; координация whole-set | Generated request + baseline ETag → Save; выбор → picker; dirty/pending → page guard | Saved generated DTO, generated editable request, own ETag, dirty/selected UI row; адресуемые SaveBlockers, ReleaseBlockers, Warnings и diagnostic basis рядом с DTO | DF01; ContentReferenceView; picker/Save callbacks; shared controls | Вход читает assignment; confirmed PUT заменяет baseline/ETag/request; Discard из baseline; выход очищает context |
| SelectPublishedInstanceVersion | Add/Replace, выбранные logical IDs; Replace требует fixed EntityInstanceId | Confirm: generated exact instance/version choice → widget; Cancel без результата | Temporary query/page/logical/version candidate/read status | Public F04 reads; ContentReferenceView; dialog controls; не читает internals assignment | Смена instance сбрасывает version choice; Confirm после явного выбора; закрытие очищает temporary state |
| SelectPublishedSchemaVersion | Add schema либо явная смена target | Generated exact SchemaVersion choice → widget | Временный поиск/страница/candidate; без assignment baseline | Public F03 reads и shared dialog; Widget проверяет mode-switch diff | Смена target не переносит pins автоматически; Cancel сохраняет прежний выбор |
| SchemaSelectionTree | Generated saved rules, live preview/basis и scope children | Mode/include/exclude/expand intents → widget | Только expanded IDs/child query-page/read status | Generated DTO/F03/F04 presentation, shared tree controls | Read errors не сбрасывают rule; stale async result не применяется к новому target |
| SaveProductContentAssignment | ProductId/generated whole-set request/assignment ETag/eligibility | Confirmed generated DTO + new ETag либо classified error → widget | Pending/captured request/context/error; без второй editable копии | DF02/generated client/shared transport; без ссылки на picker/page class | Один запрос; double click блокируется; late result только своему captured context |
| ContentReferenceView / instance reads | Generated assignment item либо F04 candidate/server availability | Exact navigation intent; read result владельцу | Нет mutable business-копии; раскрытие details — UI-state | Generated models/public F04 reads; display/copy controls | Обновление по входам; не выводит eligibility из одного status и не выбирает latest |

Модели assignment означают формы backend F07; окончательные generated names/editability/diagnostics — Q15. Saved read DTO и generated write request имеют разные роли; ручные AssignmentEditRow/FormModel/ViewModel с теми же бизнесовыми полями не создаются.

## 4. Логическая компоновка

### Schema-first редактор

Widget ProductContentAssignment владеет generated saved selector DTO, отдельным generated write request, own ETag/dirty и read-only resolved preview/basis. Не хранит вторую бизнесовую модель всех автоматически включённых instances. Логический SchemaSelectionTree отображает данные; временное раскрытие/страницы/поиск/выбранный узел — presentation state. Логическая feature SelectPublishedSchemaVersion использует public F03 reads, точечный picker F04 остаётся для exact включений. Смена policy изменяет request только после показа влияния на состав. Saved data и текущий live preview различимы.

```text
Product Workspace / Назначения
  Добавить схему → выбор logical Schema + exact Published SchemaVersion
  Дерево назначений
    Schema Name/Key · Published V3 · режим · включено/исключено/проблемы
      Все экземпляры / Все, кроме выбранных / Только выбранные
      Instance Name/Key · exact version/target · состояние · причина включения
      Instance без Published по target · не входит в состав
      Поиск / фильтр состояния / следующая серверная страница
  Диагностика Save / Release readiness
  Сохранить правила / Сбросить
  Подготовить выпуск → полный resolved состав / review / confirm
```

Значок parent selection отражает policy, а не число загруженных детей. Пустой exclusions = «Все экземпляры»; непустой = «Все, кроме выбранных». Снятие child в AllExcept добавляет stable ID в исключения, возврат снимает исключение. В SelectedOnly child включается exact выбранной Published version, снятие удаляет pin. Смена mode не означает инверсию одной видимой страницы. Полное влияние и пустой выбор отображаются явно; назначение схемы с нулём результата не исчезает.

Разворачивание грузит только детей exact schema target по server paging; parent counts/все выбранные IDs приходят от сервера для всего scope. Нельзя обещать «включены все» на основании текущей страницы или тихо пропустить failed page. Loading/retry/error сохраняют rule selection. Для AllExcept UI показывает, что новые публикации будут учтены **при подготовке нового release**, а структура V3 не сменится на V4. Недопустимый winner отображается blocker; UI не выбирает старую версию вместо него.

Tree/header счётчики берутся из resolved preview с basis и отделены от количества сохранённых exclusions/pins. Live обновление может изменить preview без dirty assignment: пользовательские правила прежние. Release review F09 показывает exact итог и требует нового подтверждения при stale basis. bUnit проверяет policy-dependent child actions, unloaded children, counts, empty schema, mode conversion, exact target, failed reads и отделение Save от release; server concurrency проверяется real API, не component doubles.

```text
SC01. ProductWorkspace: CommonContentSection [pages; ProductId/read context]
  Breadcrumb + CatalogRecordView(Product) [entities/shared; read-only]
  Навигация вкладок [общий workspace]
  Предупреждение F15 [widgets; persistent при blocker]
  ProductContentAssignment [widgets; baseline/request/ETag/dirty]
    Рабочий статус Saved / dirty [отдельно от пригодности release]
    Диагностика SaveBlockers / ReleaseBlockers / Warnings + basis [адрес rule/item]
    Добавить схему → SelectPublishedSchemaVersion
    SchemaSelectionTree [schema policy + fixed Published SchemaVersion]
      ContentReferenceView [entities]
        Instance Name/Key + exact version + SchemaVersion + lifecycle/problem
        Exact link / технические IDs в details-copy
      SelectedOnly: exact версия → SC02; AllExcept: включить/исключить stable instance
    SaveProductContentAssignment [features]
    Loading / empty / validation / conflict / unknown outcome [shared presentation]
  Guard Save / Discard / Stay [shared; при dirty-переходе]

SC02. SelectPublishedInstanceVersion [features; temporary выбор]
  Add: logical instance search / server page [public F04 reads]
  Replace: fixed logical instance [ContentReferenceView]
  Exact Published versions выбранного owner [public F04 reads]
    ContentReferenceView(version) + schema context + недоступность
  Explicit version selection / Confirm / Cancel [shared]
  Loading / no matches / read failure [shared presentation]
```

| Связь | Сведения | Координатор | Результат |
| --- | --- | --- | --- |
| Product read → header/banner/widget | ProductId/server restrictions | Page | Один applied context |
| Add/строка → picker | Add либо Replace fixed owner; текущие logical IDs | Widget | Temporary выбор, assignment прежний |
| Confirm → widget | Exact version ID и generated read context | Widget | Add/Replace локально; dirty/группы обновлены |
| Whole-set → Save → widget | Captured request/ETag; DTO/new ETag | Widget | Baseline/request заменяются, dirty сброшен лишь при confirmed success |
| Blocker/error → banner/actions | Server reason/affected scope | Page/widget | Affected mutations видимы disabled; read/history доступны |

SC02 открыт отдельно, SC01 сохраняет контекст. Source summary и рабочий набор видны вместе; соседняя вкладка заменяет рабочую область. Группировка по EntitySchema не означает saved business order: ручной SortOrder/«Порядок в релизе» исключён из target; локальная сортировка таблицы не записывает business order и не меняет manifest hash. UI-вложенность не разрешает dependencies к внутренним типам соседних срезов.

### Мастер «Подготовить выпуск»

В Product/Variant Workspace появляется явное действие «Подготовить выпуск». Это целевой сценарий [Q38](../../../business/questions/Q38-bulk-version-freeze-and-release-preparation.md), не текущий реализованный экран. Отдельные lists схем/экземпляров используют то же действие «Зафиксировать выбранные версии» без Product stages. Обычный Save assignment/редактора не запускает фиксацию.

```text
Product/Variant Workspace [pages, владелец выбранного контекста]
  ReleasePreparation [widgets, владелец открытого wizard и stage state]
    ExactVersionSelection [feature, exact Draft/Published selection]
    PreparationReview [entity/read presentation, dependencies и diagnostics]
    ConfirmPreparation [feature, explicit confirm/call через NSwag]
    PreparationResults [entity/read presentation, per-item и stage outcomes]
```

Эти логические обозначения не создают новые framework layers или копии server DTO. Page передаёт Product/Variant и принимает подтверждённые artifact IDs для обновления assignment/releases/revisions. Widget хранит только current step, selected exact IDs/tokens, busy/cancellation и generic response state; generated plan/result models приходят из backend F07. Selection сообщает выбор вверх; confirm не обращается к внутренностям соседней feature. Standalone lists композируют ContentModeling freeze flow без зависимости на Product page.

Пользователь последовательно видит: выбор exact версий → необходимые Draft схемы → preflight errors/warnings и planned composition → подтверждение → schema/instance результаты → явную подготовку assignment/releases/revision. Для нескольких конкурирующих Draft сервер не выбирает победителя; UI требует уточнить выбор. Stale token/изменившаяся dependency аннулирует review, сохраняет видимый выбор и требует повторного preflight/confirm. Уже подтверждённый own schema freeze отображается как ожидаемый stage result, а не как произвольная подмена target.

Показаны отдельно: зафиксировано, отклонено, не запускалось, исход неизвестен; серверный result — источник истины. После частичного commit immutable версии остаются, wizard не показывает «Выпуск готов» и не открывает автоматическую activation. После потери ответа перечитывается exact состояние/operation result согласно Q38, mutation не повторяется вслепую. Leaving/cancellation не обещает откат уже committed steps. API routes, job polling и continuation до Q38 не выдумываются; NSwag генерируется после server contract. Moderation, activation и Delivery readiness имеют собственные шаги F17/F11 и не скрываются за успешным созданием revision.

Приёмка: bUnit проверяет точный selection/dependencies/stage gates и partial results с API doubles; real API/browser contour отдельно подтверждает concurrency и recovery. No latest, no implicit Publish-on-Save, no duplicated edit models. Фиксация первой версии отображается отдельно от «Доступен для выдачи»: отсутствие withdrawal не доказывает наличие Published content.

## 5. Зависимости и публичные границы

Page использует public F06 reads/views и передаёт ProductId/ограничения widget. Widget открывает picker через параметры/Confirm/Cancel callbacks, передаёт request/ETag Save и принимает result/error. Picker не вызывает Save, Save не меняет picker state. Page координирует navigation/guard; callbacks не требуют ссылки на класс родителя.

Schema/version reads переиспользуются из F03, exact instance picker — из F04; schema-tree counts/paging/resolution используют согласуемый backend F07 contract Q39, не десятки клиентских запросов «получить всё». Новая feature на каждый GET не вводится. ContentReferenceView показывает metadata; backend подтверждает scope/identity. F15 banner получает reason/scope/navigation intent через public boundary; resolution form здесь не копируется.

Shared не знает Published eligibility/Product rules. Client entities не являются Domain/EF-моделью: серверные сборки, DbContext и VR School APIs не становятся зависимостями browser UI. Public chooser/read changes проверяются с потребителями F04/F08, generic headers/guard — с другими workspaces. Дополнительные слои/global store не нужны.

## 6. Данные и владение состоянием

### 6.1. Источник истины и область жизни

| Данные / состояние | Источник истины | Клиентский владелец | Обновление / сброс |
| --- | --- | --- | --- |
| Product/source lifecycle/eligibility/conflict | CM projection F06/F15 | Page, generated model | Exact read/refresh/ID change |
| Saved assignment | DF01 либо confirmed DF02 DTO | Widget, generated DTO напрямую | Вход/успех PUT/явное чтение после guard |
| Assignment ETag | DF01/DF02 response header | Generic API/UI-state рядом с DTO | Header того же exact assignment; не Product/version/list token |
| Editable весь набор exact version IDs | Generated write request до Save | Widget, одна editable форма | Add/Replace/Remove; confirmed result; Discard из baseline |
| Candidate instance/version/metadata | Public F04 reads | Picker, generated models | Query/page/instance change; закрытие |
| Диагностика SaveBlockers / ReleaseBlockers / Warnings, CanCreateRelease и basis | Server read/validation; логические категории, carrier Q15 | Widget: generated diagnostics + freshness UI-state | Baseline/form/source lifecycle меняет basis; own ETag не доказывает свежесть diagnostics |
| Dirty/pending/issues/UI selection | Чистое UI-state | Widget/feature по §3.3 | Действие/исход/выход; не DTO |
| Captured Save/context | Один запускаемый запрос | Save feature до завершения | Отправленный набор заморожен; не подменяется новым context |

ETag не добавляется в DTO; UI не вычисляет internal Version/ExpectedVersion. Temporary choice, editable набор, отправленный request и confirmed baseline различаются. Pending Save блокирует изменения этого набора; поздние ответы старого ID/query не применяются. Нет autosave/cache/browser persistence.

### 6.2. Состав представления и формы

| Сведение | Источник | Использование | Правило |
| --- | --- | --- | --- |
| Product Name/ID/source | F06 read | Header/breadcrumb/restriction | Только чтение |
| Instance Name/Key/ID | Assignment/candidate read | Primary label; exact navigation/details | Identity — EntityInstanceId |
| Exact version/номер | Server read | Показ/explicit выбор | Published для нового использования; no latest |
| EntitySchema/SchemaVersion | Authoritative server metadata | Группа/контекст | Не client authority для Save |
| Lifecycle/reference issue | Read/Save diagnostics/error | Badge/reason/exact link | Invalid existing ref видна; отсутствие metadata не скрывает exact ID |
| Набор version IDs | Generated write request | Whole-set Save | Один ID на logical instance; пустой набор — явное удаление всех |

Availability/read diagnostics/field addressing — [Q15](../../backend/questions/Q15-api-and-source-code.md#f07-read-write-и-reference-контракт). Недостающие business-поля решаются server DTO/OpenAPI/regeneration без ручной копии. IDs доступны в details/copy, не заменяют Name/Key как основную подпись.

## 7. Действия и клиентские потоки

| Действие | Запуск | Исполнитель | Обработка / запрос | Результат |
| --- | --- | --- | --- | --- |
| UI01. Открыть/перечитать | Route/direct entry/refresh | Page + widget | F06 exact Product, DF01; guard перед заменой dirty | Confirmed set/ETag либо distinct unavailable/error |
| UI02. Добавить | SC01, mutation доступна | Widget → picker | Logical instance → exact Published через DF04/DF05 → explicit Confirm | Add локально, dirty; без write |
| UI03. Заменить версию | Строка SC01 | Widget → picker | Fixed EntityInstanceId → explicit другой допустимый exact ID | Только эта позиция; Cancel ничего не меняет |
| UI04. Удалить | Строка SC01 | Widget | Remove из editable request; без HTTP | Dirty; ContentModeling object/history неизменны |
| UI05. Save | Dirty, baseline/ETag/eligibility | Save feature | Проверить local unique/choice → capture whole-set → один DF02 → await → result/error | DTO/new ETag из PUT; reset dirty; «Рабочий состав сохранён. Клиентская публикация не изменилась.» |
| UI06. Discard | Явная отмена dirty | Widget/page | Request из baseline; requested reload затем DF01 | Без PUT; только явно согласованная потеря ввода |
| UI07. Уйти/сменить context | Dirty route/tab/ID change | Page/shared guard | Save / Discard / Stay; Save ждёт UI05 | Save failure/412/unknown остаются; Stay сохраняет; Discard разрешает переход |
| UI08. Conflict/recovery | 412 или unknown UI05 | Widget | Сохранить local/sent выбор; явный DF01 отдельно для comparison | Server/current и local видны; нет auto-merge/retry |
| UI09. Диагностика | Problem item/F15 banner | Page/widget | Exact ID navigation в F04/F15, dirty guard | Read/problem context; набор не меняется |

Уже выбранный instance не создаёт вторую строку: UI предлагает Replace. Picker не выбирает первую/latest Published автоматически. Lifecycle может измениться между новым выбором и Save: серверный отказ сохраняет набор. Unchanged saved exact reference после lifecycle change не блокирует Save сама по себе; её ReleaseBlocker виден отдельно. Selectors показывают только eligible новые candidates. Existing invalid reference не включается в новый выбор, но видна SC01 с exact ID/reason/allowed actions до явного Remove/Replace.

Save отправляет только весь набор EntityInstanceVersionIds; backend восстанавливает identity/schema/metadata. На успехе принимаются returned DTO/new ETag; скрытого result GET нет. Независимый catalog refresh не повторяет mutation.

Save использует выбранный MVP writer protocol [Q20](../../backend/questions/Q20-persistence-and-migration.md): сервер после получения gate читает fresh owner/reference/catalog facts и защищает их до своего commit. Picker и диагностика остаются read-only observations, не commit permits; own assignment If-Match не гарантирует foreign eligibility. UI различает own stale412, подтверждённый foreign/domain409 и техническую недоступность координации/чтения. Техническая ошибка не создаёт CatalogConflict banner без подтверждённого conflict.

412 сохраняет edits. Reload не перебазирует набор автоматически и не подставляет новый ETag для повторного Save. Сетевой сбой после send означает неизвестный исход: сохраняется sent/local набор, явно инициируется exact check/comparison. Совпадение current set помогает понять актуальное состояние, но не доказывает авторство/исход именно запроса; при отличии возможен concurrent update. Нет обещания rollback/blind retry. Дальнейшая редактура основывается на явно принятом актуальном baseline; recovery profile — Q15.

## 8. Использование API и NSwag

| Потребность | Серверная операция | Вход | Результат | Владелец |
| --- | --- | --- | --- | --- |
| UI01 Product | [F06](../../backend/features/F06-product-catalog.md), exact Product read | Route ProductId | Read-only applied Product/eligibility/blocker | Page/public F06 read |
| UI01/08 assignment | [F07/DF01](../../backend/features/F07-product-content-assignment.md): GET `/api/v1/products/{productId}/content-assignment` | Exact ProductId | Generated DTO + strong numeric ETag; GET не создаёт assignment | Widget/api |
| UI05 | F07/DF02: PUT того же exact route | Generated whole-set IDs + обязательный saved `If-Match` | 200/current DTO + new strong numeric ETag; no result GET | Save feature/api |
| UI02 logical choice | F07/DF04 reuse [F04/DF01 и DF03](../../backend/features/F04-entity-instances.md) list/exact instance reads | Query либо fixed EntityInstanceId | Generated logical context/availability | Picker/public entity read |
| UI02/03 exact choice | F07/DF05 reuse F04/DF04 и DF05 version list/exact reads | Selected owner, затем exact version ID | Published exact version/schema context/availability | Picker/public entity read |

DF04 использует F04/DF01 `GET /api/v1/entity-instances`; DF05 — F04/DF04 `GET /api/v1/entity-instances/{instanceId}/versions`. При необходимости exact display context используются также публичные F04/DF03 и DF05 reads: `/api/v1/entity-instances/{instanceId}` и `/api/v1/entity-instances/{instanceId}/versions/{versionId}`. Новый assignment-only candidate endpoint не вводится. Query/filter/availability profile — Q15/F04. Initialization DF03 внутренняя; UI не вызывает setup и не создаёт assignment через GET. Механизм и missing-vs-pending semantics — [Q20](../../backend/questions/Q20-persistence-and-migration.md#f07-initialization-и-конкурентные-guards).

Current infrastructure: `ContentManager.ApiClient.Generated`, `nswag.json` → `OpenApi/Generated/ContentManagerApiClients.g.cs`, `ConfigureApi`/`AddContentManagerApiClients`/`ApiEnvelopeDelegatingHandler`. Current IPublishingClient не является целевым контрактом. Future generated names определяются OpenAPI, не выдумываются. Generated read/edit models используются напрямую; generated code вручную не меняется.

ETag рядом с DTO относится только к exact assignment. Caller получает DTO и success header; per-request If-Match не превращается в общий mutable DefaultRequestHeaders между владельцами. Если этого нет, меняется общая generation/transport infrastructure по [S06](../standards/S06-api-client.md). Отсутствие нового header после 200 — integration defect: success не откатывается/PUT не повторяется; следующий Save запрещён до явной exact проверки.

400 — request validation; 404 — отсутствующий Product/assignment/reference с разной областью сообщения; 409 — domain/catalog/lifecycle/reference conflict; 412 — stale ETag; 428 — missing If-Match. Codes/item addressing/envelope/availability/recovery — [Q15](../../backend/questions/Q15-api-and-source-code.md#f07-read-write-и-reference-контракт). Причина не угадывается по произвольному error text. 403 distinct access denial; конкретные permissions — full MVP access contract, не кнопки формы.

## 9. Состояния, ошибки и доступ

| Ситуация | Видимый результат | Действия | Данные / восстановление |
| --- | --- | --- | --- |
| Technical gate/read failure | Координация/проверка недоступна, не domain409/новый CatalogConflict | Сохранить ввод/context; safe read | Не empty valid result; lost write acknowledgement остаётся unknown по Q15, без blind retry/new token |
| Product/assignment loading | Loading affected area | Safe read/navigation | Без exact/ETag нет Save; late response не меняет context |
| Confirmed empty | «Контент не назначен» | Add при eligibility | Только empty200, не маска 404/failure |
| Picker loading/no matches/failure | Distinct chooser states | Query/page/Cancel/read retry | Assignment прежний |
| Dirty/pending/success | Unsaved/одна pending/confirmed сообщение | Pending блокирует competing input/write этого set | Error сохраняет set; success заменяет DTO/ETag |
| Existing unavailable ref | Exact ID/lifecycle/missing issue, ReleaseBlocker | Read/unchanged Save; explicit Remove/eligible Replace | Не filtered/removed/latest; replacement проверяется как new use |
| Archived Product | Причина и режим ограниченного maintenance | Remove refs/eligible Replace проблемных refs; без Add/расширения | Dirty сохранён; сервер проверяет тип изменения |
| CatalogConflict/F15 | Persistent reason/link banner | Ordinary writes disabled; read/history | Catalog blocker не обходится maintenance; dirty сохранён |
| 400/reference404/lifecycle409 | Item issue при известном адресе + summary | Repair/diagnostics | Набор сохранён; unknown issue видна summary |
| Product404/assignment unavailable | Distinct невозможность открыть set | Back/read retry | Не fake empty/create; Q20/Q15 semantics |
| 412 | «Рабочий состав изменился на сервере» | Explicit read/comparison/baseline acceptance | Edits сохранены; no auto-merge/retry |
| 428 | Ошибка обязательного precondition | Preserve context/fix transport | Пользователь не вводит token |
| 403 | Distinct denied | Permitted read/navigation | UI не заменяет authorization |
| Timeout после send | «Результат сохранения не подтверждён» | Explicit exact check | Sent/local сохранён; no rollback/blind retry |
| Refresh failure после success | Confirmed result + affected read error | Retry read | Mutation не повторяется |

Saved означает подтверждённую запись mutable assignment; диагностика и CanCreateRelease описывают его текущую пригодность отдельно. SaveBlockers блокируют Save, ReleaseBlockers — новый release, Warnings объясняют неблокирующие обстоятельства. Unchanged unavailable refs не превращаются в intrinsic SaveBlockers. Own ETag и diagnostic basis разделены; failure диагностики не показывается как пустой valid результат. Source restriction, F15, reference lifecycle и concurrency различаются. История releases/revisions доступна через соседние sections при блокировке новых mutations.

По [S05](../standards/S05-ui-and-forms.md)/[S07](../standards/S07-accessibility.md) labels, exact version, dirty/errors/disabled reasons понятны без цвета. Add/Replace/Remove/Save/Cancel доступны с клавиатуры; picker получает focus, закрытие возвращает его запуску; issue позволяет найти item context. Razor/HTML/CSS/MudBlazor attributes не задаются.

## 10. Проверки и приёмка

Target criteria по [S08](../standards/S08-testing.md), не результаты UI tests; библиотека не выбирается. API doubles проверяют orchestration; lifecycle/concurrency/исторические snapshots — real backend.

| Сценарий | Условия / воздействие | Наблюдаемый результат | Граница |
| --- | --- | --- | --- |
| Protected commit / technical failure | Foreign lifecycle/catalog/source changes между preview и commit; bounded gate wait/cancellation/read failure; lost ACK | Server revalidates protected facts; own412/domain409/technical failure различимы; ввод/basis/token сохранены, no false success/empty valid/blind retry | UI/transport doubles для representation; real PostgreSQL owner writers/barriers для guard, recovery contract Q15 отдельно |
| Direct/context | Route без списка; delayed old ID read | Exact context/no fallback/late overwrite | Page/double; real ownership |
| Empty/unavailable | Empty200/404/transient failure | Distinct states/no read-create | Page + real HTTP initialization |
| Explicit picker | Несколько Published + Draft/Retired/Withdrawn/Archived blockers | Logical→exact Confirm/no latest/no forbidden new use | Picker/doubles + real lifecycle |
| Duplicate/Replace/Cancel | Instance уже есть, выбрать версию/Cancel | One logical item/same-owner Replace/Cancel unchanged | Composition |
| Remove/whole-set | Remove одного/всех, Save | Один request только remaining exact IDs; explicit empty set | Widget/transport + real persistence |
| Generated boundary | Read/edit/selection | No manual business copies/metadata authority/no new picker API | Generated integration |
| ETag/success | Assignment/Product/version имеют разные tokens | Own If-Match/один PUT/DTO+newETag/reset dirty/no hidden GET | Transport + real HTTP |
| Concurrent Save | Два клиента/один ETag | 412 distinct409/edits preserved/no retry-merge | UI + real concurrency |
| New-use lifecycle/F15 race | Новый candidate стал unavailable до Save либо возник CatalogConflict | Reason/item/banner/set preserved/read-history; write refusal | UI + real guards |
| Retained exact references | Saved ref стала Retired/Withdrawn/Archived/unavailable; Save без замены; reorder отображения | Save allowed, exact IDs прежние; diagnostic ReleaseBlocker/CanCreateRelease false; no cleanup/latest | Widget + real guards |
| New position/replacement | Тот же unavailable exact ID добавлен новой позицией либо выбран вместо старого | New-use checks блокируют Save; retained baseline не даёт обхода | Picker/widget + real guards |
| Archived maintenance | Remove refs/eligible Replace проблемных refs либо Add/расширение; отдельно CatalogConflict | Maintenance allowed, expansion blocked; CatalogConflict blocks ordinary writes | UI + real guards |
| Diagnostic boundary | Saved DTO/own ETag при release blockers либо failure diagnostics | Saved показан отдельно; адресуемые категории/basis; no false empty valid diagnostics | Widget/transport |
| Dirty guard | Tab/ID/route change; Save success/failure/unknown | Wait confirmed success; failure остаётся; Stay сохраняет; Discard явно | Page/browser |
| Unknown outcome | Timeout, current set совпал/отличается | Sent/local preserved/explicit compare/no blind retry/авторство не доказано equality | Transport; real recovery отдельно |
| Header defect/403/428 | Success без ETag/denied/missing header | No new write без exact token/distinct errors | Transport/page |
| Lifetime/keyboard | Double click/late query/close-reopen/keys | One pending/right context/focus/temporary reset | Component/browser |
| Publishing boundary | Assignment Save при old releases/revisions | Working only/history/Delivery прежние | Real API/E2E |

Real MVP E2E: открыть Product → exact Published Add/Replace/Remove → explicit Save → reopen exact → подтвердить set → отдельно F09 release. F07 не выпускает release автоматически. Принятые Q07 правила retention/new-use/maintenance проверяются отдельно от незакрытых carrier деталей Q15. Markdown/link checks не тесты приложения.

## 11. Последовательность изменений и открытые вопросы

Массовый workflow и его FSD-композиция определены в §4. [Q38](../../../business/questions/Q38-bulk-version-freeze-and-release-preparation.md) блокирует точные generated API models, вызовы preflight/execute и continuation; до materialization эти controls остаются целевой аналитикой. Обычные exact single-version действия сохраняются.

Сначала уточняется backend F07/Q15/Q20 read/write/reference/initialization, затем OpenAPI/NSwag/общая header infrastructure. Переиспользуются Product F06, exact F04 reads и F15 warning; после этого рабочая секция/whole-set state, exact picker/Save, guard/recovery/errors. Проверяются local orchestration/transport и real lifecycle/concurrency/Publishing boundaries. Аналитика не меняет source/generated/соседние F-документы.

| Вопрос | Блокируемые детали |
| --- | --- |
| [Q15: F07](../../backend/questions/Q15-api-and-source-code.md#f07-read-write-и-reference-контракт) | Generated methods/models/editability/envelope/headers/availability/missing diagnostics/codes/item addressing/F04 candidate queries/limits/recovery, carrier категорий/basis/CanCreateRelease; target route и DTO+ETag success приняты |
| [Q20: F07](../../backend/questions/Q20-persistence-and-migration.md#f07-initialization-и-конкурентные-guards) | Выбранный общий writer protocol и internal setup; missing-vs-pending/read representation уточняются; UI не setup, GET не write |
| [Q07: рабочие references](../../../business/questions/Q07-exclusions-and-local-content.md#f07-допустимость-рабочих-references) | Retention, new-use и limited maintenance приняты; ссылка на основание правила, не блокирующий вопрос |
| [Q13](../questions/Q13-ui-scope.md) | Детали chooser/presentation/UI tests; route/grouping/exact/explicit Save приняты |

Подтверждённые исходники UI на 04.10.2026:

| Источник | Current implementation | Отличие target |
| --- | --- | --- |
| [PublicationWorkspacePage.razor](../../../../contentmanager.ui/src/ContentManager.Pages/Components/Publishing/PublicationWorkspacePage.razor), routes/Load | `/publishing/products/{OwnerId}`/variants в одной странице; IPublishingClient.Products2Async; assignment/release/policy/history workspace | Accepted Product route/working section; соседние сценарии F06/F08–F12 |
| PublicationWorkspacePage AddAssignment/SaveChanges | Duplicate instance rejected; manual AssignmentEditRow; ExpectedRevision/SortOrder; success затем `await Load()` | Generated direct/assignment ETag/If-Match/only exact IDs/PUT result/no hidden GET |
| [PublishedVersionPicker.razor](../../../../contentmanager.ui/src/ContentManager.Widgets/Components/Publishing/PublishedVersionPicker.razor), Load | EntityInstancesGET2Async Published filter/page20; Published versions flattened из aggregate DTO | Logical→exact/owner-fixed Replace/availability; page20/old query не target решение |
| [PublicationEditorModels.cs](../../../../contentmanager.ui/src/ContentManager.Widgets/Components/Publishing/PublicationEditorModels.cs) | PublishedInstanceChoice/manual AssignmentEditRow с metadata/SortOrder | Generated models напрямую; UI-state отдельно |
| [Generated client](../../../../contentmanager.ui/src/ContentManager.ApiClient/OpenApi/Generated/ContentManagerApiClients.g.cs), IPublishingClient/AssignmentsAsync/SaveAssignmentsRequest | PUT `api/v1/publishing/products/{id}/assignments`; ExpectedRevision body; envelope success; local headers_/ProcessResponse hook не предоставляет caller DTO+ETag/per-call If-Match | Backend target/regeneration/shared headers; current не proof готовности |
| [WorkspacePageBase.cs](../../../../contentmanager.ui/src/ContentManager.Shareds/Components/Shared/Infrastructure/WorkspacePageBase.cs) | Busy/Dirty/lifetime; Discard-only guard; любой409 получает «изменились на сервере» | Save/Discard/Stay; distinct412/domain409/unknown outcome |
| [nswag.json](../../../../contentmanager.ui/src/ContentManager.ApiClient/nswag.json), [client DI](../../../../contentmanager.ui/src/ContentManager.ApiClient/DependencyInjection/ApiClientServiceCollectionExtensions.cs), [ConfigureApi](../../../../contentmanager.ui/src/ContentManager.ApiClient/DependencyInjection/ApiClientWebAssemblyHostBuilderExtensions.cs), [ApiEnvelopeDelegatingHandler.cs](../../../../contentmanager.ui/src/ContentManager.ApiClient/Infrastructure/Http/ApiEnvelopeDelegatingHandler.cs) | Namespace/output/templates/init accessors/scoped HTTP/envelope infrastructure | Переиспользовать; наличие базы не доказывает target headers/read readiness |

Source evidence объясняет current→target изменения, не подтверждает frontend tests, серверную реализацию или выбранность прежнего internal event initialization.
