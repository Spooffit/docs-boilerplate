# F01. Создание EntitySchema: фронтенд


[Бизнесовая спецификация](../../../business/features/F01-create-entity-schema.md) · [Backend](../../backend/features/F01-create-entity-schema.md) · [M01](../modules/M01-content-authoring.md) · [F02: Schema Workspace](F02-create-schema-version.md)


Документ задаёт целевое UI-состояние Demo и отдельное расширение MVP для .NET 10, Blazor Standalone, MudBlazor, FSD и NSwag по [обзору фронтенда](../README.md). Имена будущих блоков — проектные обозначения, а не утверждение о наличии таких классов. Верстка здесь не задаётся.


## 1. Результат и границы


Пользователь открывает список схем, переходит к созданию, вводит metadata логической схемы и первого Draft, собирает дерево новых полей и явно сохраняет его. Подтверждённый сервером результат открывается в readonly workspace **точной созданной версии** по возвращённым EntitySchemaId и EntitySchemaVersionId. Редактирование/Publish F02/F03 не включается в готовность F01; наличие legacy endpoints не добавляет их кнопки.


F01 включает список как точку входа, страницу создания, редактирование ещё не сохранённого дерева и обработку create response. Изменение сохранённого Draft относится к F02, публикация — к F03, ввод runtime values и выбор конкретного Asset — к F04. Создание не назначает схему Product/ProductVariant и не меняет Delivery.


Demo работает без входа, ролей и проверки админом. MVP-доступ Specialist определяется F16/Q10 и не является зависимостью Demo. Schema/field keys вводятся явно в camelCase; editable suggestion в kebab-case относится к MVP. Автосохранение, offline и глобальное хранилище формы не вводятся.

В MVP общий SchemaEditor настраивает typed constraints конкретного SchemaValueDefinition и сравнение List по [Q35](../../../business/questions/Q35-value-constraints.md). Это часть snapshot SchemaVersion, включая рекурсивный Item definition; опубликованный snapshot неизменяем. В Demo остаётся базовый набор Q34 без конструктора дополнительных constraints.


## 2. Страницы, экраны и навигация


Целевые маршруты приняты в [Q13](../questions/Q13-ui-scope.md), раздел «routes, navigation и frontend decomposition».


| Страница / экран | Точка входа и контекст | Состав и режим | Завершение / переход |
| --- | --- | --- | --- |
| SC01. EntitySchemasPage: список | /schemas; пункт «Схемы» | Поиск Name/Key, список summary, пагинация, «Создать схему» | Create → SC02; выбор схемы → /schemas/{schemaId} по F02 |
| SC02. CreateSchemaPage: новая схема | /schemas/new; SC01 либо прямой URL, сохранённых ID нет | Metadata схемы/первого Draft, tree + inspector, validation summary, Create/Cancel | Create → /schemas/{schemaId}/versions/{schemaVersionId} с IDs из ответа; Cancel → /schemas после dirty guard |
| SC03. Подтверждение ухода | SC02, Cancel или внутренний переход при dirty | Объяснение несохранённого ввода, Save / Discard / Stay | Save запускает обычный Create и при успехе открывает exact Draft; Discard разрешает уход; Stay сохраняет экран |


SC01 показывает минимум Name, Key, число Draft, последнюю Published version, состояние выдачи/withdrawal и дату изменения. Для ещё не опубликованной схемы вместо номера показывается отсутствие публикации. Имя открывает workspace по immutable ID, не Name/Key. Список не редактирует схему inline. Целевое отдельное действие «Зафиксировать выбранные версии» выбирает exact Draft, а не все версии выбранной logical схемы; для нескольких Draft требует явного выбора. Владелец workflow и server preflight — [F03](F03-publish-schema-version.md) / [F07](F07-product-content-assignment.md#мастер-подготовить-выпуск). Обычный create/Save не запускает фиксацию.


Поиск Name/Key — server-side с debounce; query state использует Search/SkipCount/MaxResultCount/Sorting backend F01 §3.3 и сохраняется в URL для browser navigation/copy-link. Page size default20, server max200; sort Name либо ModificationTime ASC/DESC со stable Id tie-breaker. Изменение поиска начинает выдачу с SkipCount0. Debounce уточняется Q13; дополнительные filters и current major/minor не переносятся автоматически. Пустой каталог отличается от отсутствия совпадений.


SC02 открывается напрямую без GET существующей схемы. Начальная форма пустая, ещё не dirty. Refresh создаёт новую пустую форму: browser storage и восстановление после reload/закрытия вкладки не предусмотрены. В MVP session expiry/reauthentication внутри той же открытой SPA-вкладки использует обязательный [F16 recovery](F16-staff-access.md#7-действия-и-клиентские-потоки): owner Guid/actual input/exact context/original baseline сохраняются в памяти, без reload/автоматического Save. Другой user/explicit Logout очищают snapshot. Внешний уход/refresh при dirty использует доступное браузерное предупреждение; выполнение Save в браузерном диалоге не обещается.


На SC02 нет version rail. После успеха открывается readonly exact Draft по ID из результата Create. Logical details содержит максимум20 summaries и VersionsTotalCount; bounded summaries не считаются полным каталогом versions. Explicit versionId проверяется exact read, не заменяется latest или другим Draft при отсутствии в bounded list.


## 3. Карта FSD-блоков


### 3.1. Как различать уровни


Page владеет route context и композицией; feature выполняет Create; widget объединяет tree/inspector как общий редактор F01/F02. Tree и inspector — части widget, не отдельный слой screens. Клиентские entities используют generated DTO и presentation helpers, не являются серверными агрегатами. Shared предоставляет универсальные UI/transport-средства.


### 3.2. Состав именно этой фичи


| Имя блока | Слой / срез / сегмент | Ответственность | Использование в этом изменении | Основное описание |
| --- | --- | --- | --- | --- |
| Routes и DI | app | Целевые маршруты, существующая API infrastructure | Изменить routes, переиспользовать DI | §2, §8 |
| EntitySchemasPage | pages/schema-list, ui/model | Список, query context, загрузка read model, вход в создание | Адаптировать текущую страницу | §3.3, §4 |
| CreateSchemaPage | pages/schema-create, ui/model | Один request, metadata, dirty guard, композиция и навигация | Адаптировать текущую страницу | §3.3, §4 |
| CreateEntitySchema | features/create-entity-schema, model/api | Проверка формы, один POST, pending/result/errors | Целевая операция вместо HTTP orchestration внутри page | §3.3, §7–9 |
| SchemaEditor: SchemaFieldTree + SchemaFieldInspector | widgets/schema-editor, ui/model | Совместный рекурсивный редактор дерева/definition | Адаптировать текущий SchemaFieldsEditor; использовать публичную границу и в F02 | §3.3, §6.2 |
| Schema/field presentation | entities/entity-schema, entities/schema-field, необходимые части | Подписи типов/статусов, представление generated read DTO | Переиспользовать по необходимости, без новых stores | §5–6 |
| Generated client, transport, controls/dialog | shared, технические сегменты | HTTP/envelope/headers, ввод/ошибки/подтверждение без бизнес-оркестрации | Переиспользовать существующие средства | §5, §8 |


Логические обозначения не требуют новых C# projects или каталогов с этими именами. Существующие ContentManager.Pages/Widgets/ApiClient/Shareds — отправная точка размещения. Отдельный widget списка и feature на каждое поле не нужны.


### 3.3. Описание блоков


| Аспект | EntitySchemasPage |
| --- | --- |
| Назначение | Найти схему и начать создание/открытие workspace |
| Входы | Route query; generated list response backend F01/DF02 |
| Выходы | Навигация SC02/F02; query parameters при поиске/пагинации |
| Собственное состояние | Query, ввод поиска, read model страницы, loading/error; до выхода/смены query |
| Зависимости и использование | Generated query client, navigation/controls; только SC01, не владеет create request |


| Аспект | CreateSchemaPage |
| --- | --- |
| Назначение | Собрать единый несохранённый request и скоординировать создание |
| Входы | Пустой generated CreateEntitySchemaRequest с InitialDraft; существующая схема не загружается |
| Выходы | Request → CreateEntitySchema; подтверждённые IDs → F02 |
| Собственное состояние | Один request, dirty и intent ухода; editor UI-state у SchemaEditor |
| Зависимости и использование | Публичные границы editor/feature, dialog/navigation; SC02/SC03 |


| Аспект | CreateEntitySchema |
| --- | --- |
| Назначение | Выполнить Create с результатом либо различимой ошибкой |
| Входы | Ссылка на generated request страницы, lifetime/cancellation context |
| Выходы | Pending/errors; Created(EntitySchemaDetailsDto, ETag) для страницы |
| Собственное состояние | Pending operation и ошибки запуска; без второй FormModel |
| Зависимости и использование | Generated create client, общая transport/error infrastructure; UI07 и Save в SC03 |


| Аспект | SchemaEditor |
| --- | --- |
| Назначение | Согласованно показать и изменить дерево и выбранный definition; в MVP — только применимые этому типу constraints и supported List comparer |
| Входы | InitialDraft.Fields generated create DTO, readonly/pending, ошибки по путям |
| Выходы | Изменение того же generated дерева, включая typed constraints/Item/comparer в MVP, и Changed; selection → inspector; адресуемые ошибки settings |
| Собственное состояние | Selection, expanded nodes, локальные rendering keys; business data в request страницы |
| Зависимости и использование | Generated definitions, controls/presentation helpers; API не вызывает. SC02; F02 использует режим сохранённых fields без fake IDs |


## 4. Логическая компоновка


~~~text
SC01. EntitySchemasPage [pages]
  Поиск Name/Key и состояние текущего запроса
  Список schema summaries [generated read model + presentation]
  Пагинация
  Создать схему


SC02. CreateSchemaPage [pages]
  Заголовок «Новая схема», контекст «первый Draft»
  Metadata схемы: Name, Key, Description
  Metadata InitialDraft: Title, ChangeComment
  SchemaEditor [widgets]
    SchemaFieldTree: fields, nested definitions, выбор, add/remove
    SchemaFieldInspector: общие свойства + применимые type settings
      Enum/FlagEnum options / Object children / List item definition
      [MVP] Применимые typed constraints / List collection constraints и comparer
  Validation summary [shared presentation; business paths задаёт feature]
  CreateEntitySchema [features]: Create, pending, result/errors
  Cancel и dirty guard
  SC03. Подтверждение ухода [shared dialog, условно при dirty]
~~~


Metadata, tree, inspector и действия видны на одной странице. Inspector показывает один выбранный field или вложенный List item definition; type settings меняются с типом. Основное редактирование field не переносится в modal. Guard открывается отдельно поверх контекста формы.


| Связь частей | Какие сведения передаются | Кто координирует | Что меняется у получателя |
| --- | --- | --- | --- |
| Tree → inspector | Ссылка на generated node/definition и UI path | SchemaEditor | Свойства выбранного node без копирования |
| Inspector/tree → page | Changed того же дерева | Page через callback | Dirty=true; ошибки изменённого поля перепроверяются |
| Feature → editor/metadata | Pending и validation paths | Page | Ввод блокируется при POST; ошибки у своих controls |
| Summary → tree/inspector | Путь ошибочного node/setting | SchemaEditor | Раскрыть ancestors, выбрать node, focus на control |
| Feature → page → F02 | Подтверждённый DTO с schema/version IDs | Page | Dirty=false, exact route; F02 читает согласованную read model |
| Cancel → guard → page | Save / Discard / Stay | Page | Create, разрешение ухода или сохранение контекста |


## 5. Зависимости и публичные границы


Page использует SchemaEditor через generated fields/readonly/errors и callback изменения, CreateEntitySchema — через generated request и result/errors. Это логические публичные интерфейсы; точные C# signatures определяются реализацией и не выдаются здесь за существующие.


Widget не зависит от страницы и не вызывает соседнюю feature. Feature не управляет route и не импортирует widget; page принимает result и выполняет переход. Взаимодействие независимых действий F01/F02 координирует page/workspace.


Generated DTO/API-client остаются клиентской границей; Domain/EF-сборки в браузер не импортируются. Entities могут предоставлять presentation helpers, но не дублирующий request. Shared dialog получает текст/варианты и возвращает выбор, не знает о создании EntitySchema.

Владелец модели constraints/comparer — [backend F01 §2.1](../../backend/features/F01-create-entity-schema.md#21-aggregates-entities-и-value-objects). Inspector работает напрямую с соответствующими NSwag-generated definitions, без параллельной business FormModel. Общие controls показывают значения и inline errors; SchemaEditor определяет применимость controls по typed definition. Если OpenAPI не выражает эту рекурсию или comparer, исправляется контракт и регенерируется NSwag, а не создаётся ручная копия.


Общий editor затрагивает F02: изменение его публичного поведения требует проверки create-режима и режима сохранённого Draft. Переиспользование controls не разрешает менять immutable identity существующего field.


## 6. Данные и владение состоянием


### 6.1. Состояние


| Данные / состояние | Источник истины | Клиентский владелец и область жизни | Когда обновляется / сбрасывается |
| --- | --- | --- | --- |
| List summaries/total | Backend F01/DF02 | SC01, текущая server page | Query change/reload; старый response игнорируется |
| Search/page | Route query; текущий ввод до debounce | SC01 | Ввод/пагинация; восстановление из URL при browser navigation |
| Metadata/tree до Create | Один NSwag-generated CreateEntitySchemaRequest | SC02 | Ввод; сброс после discard/выхода/успеха |
| Selection/expanded/UI keys | Локальный UI-state | SchemaEditor, жизнь SC02 | Выбор/add/remove; selection очищается при удалении своего node |
| Dirty/intent ухода | UI-state | SC02 | Ввод → dirty; success/discard → сброс; Stay сохраняет |
| Pending/server errors | Create operation | CreateEntitySchema, текущий запуск | Запуск/ответ; поздний result не меняет новый context |
| Created DTO/ETag | Подтверждённый response либо original receipt replay | API result → SC02 | Exact readonly navigation; current resource читается отдельно, original replay ETag не становится current-state token |


UI не генерирует SchemaId, SchemaVersionId, SchemaFieldId, VersionNumber, ContentHash и technical Version. CreateSchemaFieldRequest не содержит Id, nullable Id, IsNew или подставной GUID. Rendering key хранится отдельно от DTO и не сериализуется. List item definition — определение значения, не field с фиктивным Key/Id.


### 6.2. Состав представления и формы


Контракты/серверные ограничения определяет [backend F01](../../backend/features/F01-create-entity-schema.md). Ниже описано использование generated полей, без второй DTO-модели.


| Сведение / поле | Источник | Использование | Правило и источник ограничения |
| --- | --- | --- | --- |
| Schema Name | Generated request | Обязательное «Название схемы» | 1–255 после внешнего Trim; backend F01 §2.3 |
| Schema Key | Generated request | Обязательный явный «Ключ схемы», hint camelCase | Demo key profile backend F01 §2.3: `^[a-z][a-zA-Z0-9]*$`, 1–50 после внешнего Trim, case-sensitive; global uniqueness — сервер; регистр сохраняется без Camelize/автогенерации |
| Description | Generated request | Необязательное описание | ≤1024 после внешнего Trim; blank → null; backend F01 §2.3 |
| InitialDraft.Title | Generated Draft DTO | Обязательное короткое «Название Draft» | 1–255 после внешнего Trim; отличается от Name схемы, используется для выбора версии в F02 |
| InitialDraft.ChangeComment | Generated Draft DTO | Необязательный комментарий | ≤1024 после внешнего Trim; blank → null; свободный текст, включая ссылку/номер задачи, без Jira API/control |
| InitialDraft.Fields | Generated create field DTO | Root tree и inspector | Минимум одно структурно валидное поле |
| Field Name/Key/HelpText | Generated field DTO | Название, явный camelCase-key, optional подсказка | Name 1–255, HelpText optional ≤1024 после внешнего Trim, blank HelpText → null; Key использует Demo profile и case-sensitive whole-tree uniqueness, включая fields внутри List<Object> |
| Enum/FlagEnum option Name | Generated option DTO | Обязательная подпись варианта | 1–255 после внешнего Trim; backend F01 §2.3, включая recursive definitions |
| Field IsRequired/Deprecated | Generated field DTO | Применимые переключатели с пояснениями | Deprecated field обязан быть optional; true/true запрещено |
| Field Definition | Generated recursive definition DTO | Тип и применимые settings | Все 15 Demo types; без InheritanceStrategy/ColorFormat |


В F01 все fields новые: Key/Type можно менять до первого сохранения. Ограничения эволюции существующей identity относятся к F02. Deprecated fields остаются видимыми с отметкой и после non-deprecated siblings; это UI grouping, не SortOrder/canonical order/drag-and-drop.

Local validation schema/field keys повторяет серверный профиль по значению после внешнего Trim: 1–50 символов, regex и case-sensitive whole-tree comparison. `fieldKey`/`fieldkey` различаются; повтор точного key в разных ветках даёт адресуемую ошибку. UI не исправляет регистр/формат и сохраняет введённый регистр при отправке.

Metadata controls проверяют required/length по значению после внешнего Trim, в UTF-16 code units как .NET String.Length/FluentValidation MaximumLength, по [backend F01 §2.3](../../backend/features/F01-create-entity-schema.md#23-инварианты-и-constraints). Required blank/whitespace и over-limit значения дают inline error и блокируют POST, включая Name вложенных fields/options. Перед отправкой удаляются только внешние whitespace; optional Description/ChangeComment/HelpText empty/whitespace передаются как null. Внутренние пробелы и регистр сохраняются, значения не обрезаются до лимита. Сервер повторяет эти проверки и возвращает сохранённую нормализованную metadata; UI не подставляет служебные комментарии/подсказки. Это нормализация authoring metadata, а не runtime values или typed constraints.


Tree показывает название, key, тип, вложенность, выбор и ошибки. Пустое дерево предлагает «Добавить поле» и сообщает minimum. Inspector до выбора предлагает выбрать/добавить field. У Object можно добавить child. У List настраивается item definition: Object-item содержит children, List-item — следующий item definition. Раскрытие и выбор не ставят dirty.


| Тип definition | Базовый состав inspector Demo | Граница создания схемы Demo |
| --- | --- | --- |
| String, Text | Тип и общие settings; пояснение короткого/длинного текста | String max 255 — ограничение value. Text — restricted HTML p/br/lists/bold/italic, server cleaned result/semantic-empty check; numeric operational cap по Q34/Q22 после representative fixture, не MVP per-field constraint |
| Integer, Float, Decimal, Boolean | Тип и общие settings | Без instance values и range/step/precision-конструктора Demo |
| DateTime, Date, TimeSpan | Тип/общие settings, пояснение date/time/duration | Без timezone/offset Demo и min/max input; zero/negative duration относятся к values F04 |
| Enum, FlagEnum | Options: Key, Name, предусмотренный contract Deprecated; add/remove | Минимум одна полная option, уникальные keys внутри definition; без Value/BitValue/GUID OptionId |
| Color | Один RGBA-contract #RRGGBBAA | Без выбора ColorFormat и runtime color picker |
| Object | Child fields с отдельными definitions и выбором в tree | Минимум один child перед Save; временная неполнота видна как ошибка |
| List | OrderMode Default/Ascending/Descending и рекурсивная Item definition | Без flat-only ListItemType, запрета List<List<T>>, comparator/MinItems/MaxItems Demo |
| AssetReference | Обязательный selector `AssetKind` (Image/Audio/Video) + общие settings; extensions/limits read-only hints из F05 policy при наличии; после Demo — selector `DeliveryMode` | Без AssetId picker/upload конкретного файла в F01; схема не редактирует allowlist; mode относится к definition, не к MediaAsset |


List<List<T>>, List<Object>, List<Enum> и List<AssetReference> используют ту же рекурсивную definition; AssetReference item также требует AssetKind. В Demo Ascending/Descending сохраняются и round-trip-ятся без клиентского неявного comparator complex values. IsRequired не означает «отличается от CLR default»; values экземпляра здесь не вводятся.

В MVP inspector выбранного field либо Item definition показывает следующий применимый набор; controls отсутствующих у типа constraints не предлагаются. Collection constraints List и constraints его Item редактируются раздельно в том же recursive editor.

| Typed definition | MVP controls и смысл |
| --- | --- |
| String | MinLength/MaxLength в Unicode code points; supplementary emoji = 1, combining mark отдельный, normalization нет. Intrinsic max 255 в тех же единицах; configured limits только сужают область |
| Text | MinTextLength/MaxTextLength полезного текста sanitized HTML после entity decoding, в Unicode code points; document-order concatenation без layout separators, `<p>a</p><p>b</p>` = 2, `<br>` = 0. Operational HTML cap и semantic-empty отдельны |
| Integer / Float / Decimal | Inclusive Minimum/Maximum: long / finite double / decimal; preset «Неотрицательное» задаёт Minimum=0. MultipleOf только positive Integer/Decimal с точной кратностью; Decimal не переводится в double, промежуточная арифметика без overflow |
| Date / TimeSpan | Inclusive typed min/max в текущем смысле; без dynamic today/now, relative и cross-field выражений |
| DateTime | Fixed TemporalSemantics из exact definition: новые MVP definitions — Offset, исторические — Naive. Inclusive Minimum/Maximum для Offset требуют offset и сравниваются по UTC instant; Naive использует прежние local components. Bounds вводятся без browser timezone conversion |
| List | MinItems/MaxItems, UniqueItems, OrderMode и comparer; отдельный recursive Item definition с применимыми constraints. Required List может быть empty без запрещающего MinItems |

List OrderMode.Default требует отсутствующего comparer. Ascending/Descending требует поддерживаемого comparer; выбор unsupported сочетания недоступен с объяснением. ItemValue поддерживает только Integer/Float/Decimal/Boolean/String/Enum/Date/DateTime/TimeSpan: типизированные numeric/temporal comparisons, false < true, String/immutable Enum Key — ordinal UTF-16 .NET semantics, без culture/case folding/natural sort/Name. Для List<Object> доступен только ObjectFieldPath: один непустой ordered path SchemaFieldId, переходы через Object, без List; все fields пути и scalar leaf required, leaf из того же supported набора. Selector показывает подписи, но сохраняет IDs; Name/ItemKey/expression/composite/optional-key не являются comparer.

Direct Text/Color/FlagEnum/AssetReference/Object/List comparison недоступно в первом MVP; nested List остаётся допустимой Item definition, но не scalar sort key. Regex, Float MultipleOf, Decimal Precision/Scale и новые Enum/FlagEnum/Color/AssetReference constraints отложены. UI не назначает скрытые настройки при смене типа. Invalid min/max, negative length/count, MultipleOf ≤ 0, несовместимый constraint и invalid comparer/path показываются у setting и отклоняют Schema Create/Save сервером. В ещё не сохранённом F01 дереве server FieldId не подставляется локальным GUID: если generated create contract не позволяет выразить выбранный path, необходим его корректный серверный transport по Q15 до доступности такой отправки.

Определения не являются validator исправлений: trim/coercion/default filling/clamp/rounding/dedup/sort отсутствуют. [DateTime MVP в Q34](../../../business/questions/Q34-schema-field-semantics.md#datetime-mvp-temporal-semantics-и-совместимость) закрывает смысл Offset: один FieldDataType.DateTime с immutable TemporalSemantics=Naive/Offset, включая recursive Item. Это read-only свойство definition, не переключатель семантики. Новые MVP DateTime definitions создаются Offset; Demo и historical `/1` остаются Naive. Владелец модели — [backend F01 §2.1](../../backend/features/F01-create-entity-schema.md#21-aggregates-entities-и-value-objects), parsing/formatting — [backend F04](../../backend/features/F04-entity-instances.md).

Offset controls для bounds используют отдельные дату, время, visible offset и явное редактирование семи fractional digits по [F04 §6.2](F04-entity-instances.md#62-представление-и-значения); начальный новый Offset — +00:00, показан derived UTC instant. Whole-minute offset допустим от −14:00 до +14:00; local date и UTC instant оба в диапазоне DateTimeOffset. Wire invariant: `yyyy-MM-dd'T'HH:mm:ss.fffffffzzz` либо `yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'`; ровно семь digits, offset обязателен, −00:00/leap second/overflow невалидны. Equivalent instant bounds имеют одинаковый смысл и schema hash нового profile; original offset остаётся authoring value. Invalid parse/range/min > max отклоняет definition Save. Региональные zone IDs/DST не вводятся; Date/TimeSpan и audit timestamps этим правилом не меняются.


Смена типа нового node переключает inspector и отправляемый definition: неиспользуемые children/options/legacy settings не уходят скрыто. Удаление field после подтверждения убирает всё несохранённое subtree; это не DELETE серверной схемы. Неизвестный тип не подставляется как String.


### 6.3. Режим доставки AssetReference после Demo

Общий SchemaEditor владеет inspector-контролом `DeliveryMode` для выделенного leaf definition типа `AssetReference`: `OnDemand` (default, скачивание при обращении), `Preload` (предварительно/фоном), `RequiredBeforeLaunch` (проверенный файл до запуска выбранного snapshot). Контейнеры Object/List и остальные типы этого свойства не имеют. Для List<AssetReference> контрол находится у recursive ItemDefinition; для List<Object> и List<List<AssetReference>> — у соответствующего конечного определения. `IsRequired` проверяет наличие значения экземпляра и не заменяет readiness медиа.

Входы inspector: selected generated definition, Draft/readonly/pending context и validation issues. Выход: Changed того же generated request страницы; selection/tree/inspector state остаётся у общего widget, business input — у page. Inspector не вызывает API, не создаёт самостоятельный store и не выбирает конкретный AssetId. При создании отсутствующий mode даёт effective OnDemand; неизвестный literal или mode у другого типа не исправляются клиентом молча. Create DTO не содержит client-generated SchemaFieldId; update существующего Draft сохраняет server identity. Exact DTO/codec/profile уточняются [Q37](../questions/Q37-launcher-asset-delivery.md), без ручного редактирования NSwag-кода.

Логическая компоновка SC02: SchemaEditor → selected AssetReference/ItemDefinition inspector → AssetKind → DeliveryMode с пояснением трёх вариантов → Changed → dirty request страницы → явный Save. Published view F02/F03 показывает mode обычным read-only свойством; его изменение требует новой версии. Старые Published artifacts без свойства имеют effective OnDemand без переписывания bytes/hashes. Это последующий интеграционный этап; базовая форма Demo не получает обязательную зависимость от launcher/preload.

Проверки нового контрола: default при создании; Save/read/Publish выбранных трёх режимов; recursive Item/leaf; неприменимый тип; readonly Published; серверный ID при Create; история старого profile без rehash. Для сохранения FieldId при смене только mode и выбора versioned metadata carrier ожидается решение Q37.

## 7. Действия и клиентские потоки

MVP: изменение constraint или comparer меняет тот же generated request страницы и ставит dirty через Changed. Переход summary к ошибке раскрывает Item/path и выбирает нужный typed setting. Create отправляет полный definition snapshot один раз; отказ сохраняет введённые constraints и selection, исправление требует обычного explicit Create, без автоматической коррекции definitions.


| Действие | Начальное состояние и запуск | Исполнитель | Обработка / серверная операция | Результат в интерфейсе |
| --- | --- | --- | --- | --- |
| UI01. Найти/загрузить | SC01, вход/смена search/page | EntitySchemasPage | Query → debounce при search → DF02; stale response игнорировать | Актуальные rows/total либо error/empty |
| UI02. Начать создание | SC01 Create либо direct URL | Navigation/page | SC02, пустой generated request; без GET existing schema | Metadata и пустое дерево |
| UI03. Ввести metadata | SC02, Name/Key/Description/Title/Comment | CreateSchemaPage | Изменить request, local validation; без HTTP | Dirty=true, применимые ошибки обновлены |
| UI04. Добавить/выбрать | Root/Object child/List definition в tree | SchemaEditor | Generated field/definition без ID; выбор без записи | Inspector показывает node; add → dirty, selection → без dirty |
| UI05. Изменить definition/options | Inspector выбранного node | SchemaEditor | Общие/type-specific проверки, recursion; без HTTP | Tree и inspector отражают одни данные |
| UI06. Удалить новое поле/option | SC02, действие node/option | SchemaEditor | Subtree confirmation, local removal и validation | Данные ушли из request; нарушение minimum видно |
| UI07. Создать | SC02 Create либо SC03 Save | CreateEntitySchema + page | Проверка → pending → DF01 POST → result/errors | Success → exact Draft F02; error сохраняет форму |
| UI08. Отменить/уйти | Cancel/внутренний переход | Page + guard | Clean → уход; dirty → Save/Discard/Stay | Обычный Create, discard либо сохранение ввода |
| UI09. Открыть ошибку | Validation summary SC02 | Page/editor | Path → ancestors → selection → focus | Проблемный control виден; HTTP нет |


UI07 проверяет required metadata, minimum fields, whole-tree uniqueness и completeness definitions. Local errors не запускают POST. Перед отправкой feature назначает non-empty OperationId один раз и сохраняет submitted request. Page блокирует редактирование и повторный Create при pending/unknown outcome; feature не присваивает schema/version/field IDs.


При success page проверяет EntitySchemaDetailsDto и summary первого Draft, берёт оба ID из response, сбрасывает dirty и открывает exact readonly route. Content snapshot/server field IDs загружает exact Draft GET; UI не реконструирует ID по позициям локального дерева. Успех Create не означает Publish или готовность editing F02.


Ошибка сохраняет metadata, subtree и selection. OperationId сохраняется с immutable submitted payload до выяснения исхода. Потерянный ответ восстанавливается явным повтором того же OperationId и того же normalized input: durable серверный replay после server restart возвращает original DTO и original logical ETag. Другой input с тем же ID →409 entity_schema.operation_input_conflict; занятый schema Key →409 entity_schema.duplicate_key. Lifetime cancellation не доказывает rollback; поздний response не уводит новый context. Смена ввода после неизвестного исхода требует сначала выяснить прежний результат; response replay не заменяет чтение current state.


## 8. Использование API и NSwag


| Потребность / UI-поток | Серверная фича и операция | Входные сведения | Используемый результат | Владелец вызова |
| --- | --- | --- | --- | --- |
| UI01 list/search/page | Backend F01/DF02, GET /api/v1/entity-schemas | Search/SkipCount/MaxResultCount/Sorting | Generated list rows/total, default20/max200, без trees/tokens | EntitySchemasPage через query client |
| UI07 Create | Backend F01/DF01, POST /api/v1/entity-schemas | Generated request: OperationId + InitialDraft, без field IDs | ApiResponse<EntitySchemaDetailsDto>, exact IDs, HTTP ETag logical schema | CreateEntitySchema |
| Открыть схему/created Draft readonly | Backend F01/DF03 root GET + DF04 exact Draft GET | Exact IDs выбранной строки/create result | Root metadata/≤20 summaries+total/own ETag; Draft Content/server IDs/own ETag | Workspace после перехода; не скрытый шаг POST |


Существующий client: ContentManager.ApiClient.Generated.IEntitySchemaClient. Конфигурация — src/ContentManager.ApiClient/nswag.json, output — OpenApi/Generated/ContentManagerApiClients.g.cs. ConfigureApi/AddContentManagerApiClients в ContentManager.ApiClient.DependencyInjection подключают clients и ApiEnvelopeDelegatingHandler. Это проверенная infrastructure, а точные target generated methods фиксируются после backend OpenAPI/regeneration.


Generated CreateEntitySchemaRequest, CreateSchemaDraftRequest, CreateSchemaFieldRequest и recursive definition models используются напрямую. Если current DTO не представляет recursion/InitialDraft metadata, сначала меняется server contract/OpenAPI, затем NSwag. Ручной EditorModel/FormModel и правка generated-кода не вводятся.


Create не требует If-Match: ресурс новый; 412/428 не штатные create errors. Response ETag относится к logical schema и хранится как transport metadata рядом с DTO. Это не ETag созданной версии. Дальнейшие mutations F02 получают ETag соответствующего exact resource по [S06](../standards/S06-api-client.md). Если NSwag не предоставляет headers, меняется общая generation/transport infrastructure.


Server validation paths связываются с metadata controls либо recursive tree/inspector; неадресуемая ошибка остаётся в summary. Duplicate schema key определяется предметным error code/полем, не любым 409. Machine codes и paths должны быть в backend contract, не выдумываются в UI.


## 9. Состояния, ошибки и доступ


| Ситуация | Что видит пользователь | Доступные действия | Что происходит с данными / восстановление |
| --- | --- | --- | --- |
| SC01 loading/query change | Ожидание списка для текущего query | Создать, менять search/navigation по pending query | Старый response не подменяет актуальный query |
| Пустой каталог | «Схем пока нет» | Создать | Не маскирует read error |
| Нет совпадений | Сообщение для текущего поиска | Изменить/сбросить search, создать | Без полной загрузки каталога |
| List error | Ошибка + «Повторить» | Повтор GET, изменить search, создать | Query сохранён, нет ложной пустой выдачи |
| Пустое дерево/неполный definition | Minimum/required errors и пояснения | Продолжить ввод, add child/option/item | Временная local неполнота допустима, Save не объявлен успешным |
| SC02 POST pending | Ожидание Create | Ввод/повтор Create/обычный Cancel заблокированы | Request не меняется, результат ещё неизвестен |
| Success | Workspace точного созданного Draft | Продолжить F02 | IDs серверные, Published number нет, Delivery без изменений |
| 400 invalid request/structure | Field/definition errors + summary | Исправить и Create | Ввод/selection сохранены, частичный результат не подтверждён |
| 409 duplicate schema key | Сообщение около Key и в summary | Изменить Key, Create | Остальной ввод сохранён; не «ETag устарел» |
| Другой domain 409 | Предметная ошибка формы | Исправление по error | Нет ложного success/auto-POST |
| Timeout/network/неясный failure после POST | «Результат создания не подтверждён» | Выяснить исход/replay того же operation ID и payload | Исходный ввод и captured request сохранены; cancellation не отменяет commit; новый ID не создаётся для слепого повтора |
| Dirty Cancel/internal navigation | Save / Discard / Stay | Выбор действия | Save error оставляет форму; Discard удаляет только local input |
| Неожиданный отказ доступа Demo | Ошибка конфигурации/контракта | Сохранить context, сообщить об ошибке | Не добавляет login flow; MVP отдельно |


При подтверждённом отказе после исправления разрешён новый ручной Create. Неизвестный исход не равен rollback; uniqueness key не делает POST idempotent. Success response без нужных IDs — ошибка contract: fake GUID или произвольная выбранная версия не подставляются.


По [S05](../standards/S05-ui-and-forms.md) и [S07](../standards/S07-accessibility.md) controls имеют понятные подписи, required/error не передаются только цветом. Основные add/select/remove/input/Create/Cancel доступны с клавиатуры; видны selection/вложенность. Summary переводит focus к ошибке; Stay возвращает focus к месту запуска guard. Блокировка кнопок — UX, не server validation/authorization.


## 10. Проверки и приёмка

MVP проверяет общий inspector отдельно от Demo: String Unicode/code points, Text decoded sanitized text без layout separators, inclusive typed bounds, exact Decimal/Integer MultipleOf, отсутствие Float step/regex/Precision/Scale, collection/recursive Item separation. Generated serialization сохраняет typed snapshot без второй business model; Default без comparer, Ascending/Descending с supported comparer, required Object FieldId path и disabled unsupported controls проверяются у F01/F02. Invalid definition не теряет local input и отклоняется реальным backend; UI double подтверждает адресуемые сообщения/dirty/одно Create, но не server validation. DateTime fixtures проверяют fixed Offset новых MVP definitions/Item и Naive history, offset bounds и equivalent instants, ±14:00/date rollover/overflow, обязательный offset/−00:00/leap second и 100ns round-trip; это критерии будущей проверки, не выполненные tests.


Проверки описывают target behavior по [S08](../standards/S08-testing.md). Библиотека tests здесь не выбирается; client doubles допустимы для изолированных flows, round-trip/server identities требуют реального backend. Наличие таблицы не означает запуск tests.


| Проверяемый блок / сценарий | Исходные условия и воздействие | Наблюдаемый результат | Граница проверки |
| --- | --- | --- | --- |
| SC01 search/page | Client double: query A/B, поздний A | B rows/total, query в URL, server search | Page/API boundary |
| SC01 empty/error | Empty catalog/search либо read failure | Различимые сообщения/действия | Component + API double |
| Metadata SC02 | Empty Name/Key/Title, затем правильный manual ввод | Нет POST при ошибке; Key не перезаписан из Name | Form behavior |
| Metadata limits/normalization | Required metadata 1/255/256 после Trim, blank/whitespace; optional 1024/1025/null/empty/whitespace; nested field/option Name | 1/255/1024 допустимы, required blank/256 и optional1025 блокируют POST с inline path; внешние whitespace удалены, optional blank → null, внутренние пробелы/регистр сохранены без truncation | Form + editor + generated request serialization; server guarantee отдельно real backend |
| Demo key profile | 1/50/51 символов, invalid first character/underscore/hyphen/Unicode, внешние whitespace, `fieldKey`/`fieldkey`, точный duplicate разных веток | Local validation согласована с backend profile; Trim без смены регистра; точный duplicate/invalid key блокируют POST, разные casing допустимы | Form + editor validation; server guarantee отдельно real backend |
| Create identity | Собрать и сериализовать request | Нет Id/IsNew/fake GUID/Version/VersionNumber; UI keys вне HTTP | Generated serialization |
| OperationId replay | Lost response/unknown outcome, server restart; тот же submitted request; changed input conflict | Ввод locked до выяснения, same OperationId/input возвращает original IDs/DTO/ETag; conflict distinct от duplicate Key; новый intent после confirmed outcome получает новый OperationId | Flow + real API/PostgreSQL отдельно |
| Readonly exact workspace | Exact created/older version ID вне20 summaries, wrong-owner ID, full recursive snapshot | Exact GET решает membership; нет latest fallback и Save/Publish controls; logical и Draft ETags раздельны | Workspace flow + API |
| Editor 15 types | Field каждого типа | Только applicable controls, без numeric/legacy/runtime values | Editor component |
| Recursion | List<List<T>>, List<Object>, nested Object/List | Полный request; item definition не fake field | Editor/serialization |
| Structural validation | Zero roots, empty Object/options, missing Item, duplicate key разных веток | POST не отправлен либо адресованная server error; summary выбирает node | Validation + API boundary |
| Type change/removal | Сменить тип, подтвердить removal selected subtree | Не отправляются старые settings, нет selection на удалённый node | Editor behavior |
| Create success | Client double: schema/initial Draft IDs | Один POST, exact route с обоими IDs, dirty=false | Feature/page/navigation |
| Pending/errors | Double click, 400, duplicate-key/other 409 | Один pending POST, ввод/selection сохранены, errors различимы | Feature + API double |
| Unknown outcome/late response | Timeout/response после ухода/replay ID | Тот же ID+payload возвращает прежний result; mismatch conflict; поздний result не navigates новый context | Async boundary + durable replay real backend |
| Dirty guard | Stay/Discard/Save, Save error | Ввод сохранён либо discarded; Save через тот же Create; error не уводит | Page/dialog |
| Accessibility | Keyboard, nested node, summary error/guard | Основные действия достижимы, focus/context понятны | Component/browser |
| Реальный round-trip | UI + backend, все types/recursion/OrderMode | Schema + первый ненумерованный Draft, exact tree/server IDs в F02, Delivery прежний | E2E реального контура |
| Общий editor F01/F02 | Create и saved-Draft consumers | Create без IDs; immutable existing identity защищена | Consumers widget |


Приёмка описания: состав SC01/SC02 связан с блоками; действия имеют исполнителя; request один; значимые states имеют владельца; API связан с backend DF; result/errors проверяемы без верстки.


## 11. Последовательность изменений и открытые вопросы


Backend F01 задаёт конкретные DTO/recursive definitions/OpenAPI §8. NSwag regeneration и response headers проверяются по этому contract; общий editor использует generated модели без дубля SchemaFieldEditModel. Routes/list/create/replay/readonly navigation проходят UI checks и real round-trip. Перед использованием editor в F02 separately проверяется saved-field режим; F01 не включает legacy mutations. Это порядок проверки, не утверждение, что все проверки уже пройдены.

| Текущий исходник | Что подтверждает наличие кода | Граница проверки |
| --- | --- | --- |
| [CreateEntitySchemaPage](../../../../contentmanager.ui/src/ContentManager.Pages/Components/ContentModeling/EntitySchemas/CreateEntitySchemaPage.razor), [CreateEntitySchemaFlow](../../../../contentmanager.ui/src/ContentManager.Features/SchemaCreation/CreateEntitySchemaFlow.cs) | Generated request/OperationId, pending/unknown outcome, сохранение submitted input, exact navigation | Source presence не доказывает browser flow/real API round-trip |
| [EntitySchemasPage](../../../../contentmanager.ui/src/ContentManager.Pages/Components/ContentModeling/EntitySchemas/EntitySchemasPage.razor) | List/search/page через generated client | SQL paging/contracts проверяются backend; browser state отдельно |
| [EntitySchemaPage](../../../../contentmanager.ui/src/ContentManager.Pages/Components/ContentModeling/EntitySchemas/EntitySchemaPage.razor) | Readonly exact workspace, root/version reads и recursive snapshot | No Save/Publish F02/F03 readiness claim; exact-ID ownership проверяет backend |
| [SchemaDefinitionEditor](../../../../contentmanager.ui/src/ContentManager.Widgets/Components/ContentModeling/EntitySchemas/UI/SchemaDefinitionEditor.razor), [SchemaSnapshotFields](../../../../contentmanager.ui/src/ContentManager.Widgets/Components/ContentModeling/EntitySchemas/UI/SchemaSnapshotFields.razor) | Create tree/inspector на generated recursive request и readonly saved snapshot | Все15 types/List recursion/validation focus проверяются отдельно, legacy editor не substitutes new contract |
| [NSwag configuration](../../../../contentmanager.ui/src/ContentManager.ApiClient/nswag.json), [generated client](../../../../contentmanager.ui/src/ContentManager.ApiClient/OpenApi/Generated/ContentManagerApiClients.g.cs) | Concrete editable request/recursive definitions/Details DTO client boundary | Successful regeneration/build/serialization только по выполненным checks |


| Вопрос | Какие детали блокирует |
| --- | --- |
| [Q13: UI scope](../questions/Q13-ui-scope.md) | UI test contour/debounce материализуются инженерно; Search/SkipCount/MaxResultCount/Sorting уже заданы server contract |
| [Q15: F01 contract](../../backend/questions/Q15-api-and-source-code.md#f01-детализация-целевого-контракта) | Metadata/recursive Demo request/Details bounds/OperationId durable replay/conflict codes/own ETags выбраны; проверить OpenAPI/NSwag/real API/browser, future F02/F03 отдельно |
| [Q34: field semantics](../../../business/questions/Q34-schema-field-semantics.md), [Q35: value constraints](../../../business/questions/Q35-value-constraints.md) | MVP typed constraints/comparer и DateTime temporal semantics согласованы; operational Text cap остаётся Q22, exact whole new-profile codec/carriers/golden bytes — Q19/Q31, OpenAPI/NSwag temporal union и new-field ObjectFieldPath transport — Q15; server ID ownership сохраняется |


Routes, tree + inspector, explicit Save, dirty guard и NSwag-models уже приняты в Q13 и не остаются открытыми. Metadata limits, machine errors и validation paths frontend получает из server contract, не назначает вторым источником истины.


Legacy редактор/DTO и маршруты F02/F03 не доказывают готовность их целевого поведения. F01 использует новый recursive create contract и readonly exact snapshot; дальнейшие изменения владельцев и публикация проверяются следующими фичами. Проверки документации не являются тестами приложения.

UI build и [F01 test runner](../../../../contentmanager.ui/tests/ContentManager.Ui.Tests/F01Tests.cs) прошли. В реальном browser/API контуре создан required List<AssetReference Audio> с HelpText и открыт exact первый ненумерованный Draft readonly; после backend restart replay подтвердил original body/ETag. Explicit versionId вне20 root summaries больше не отклоняется локально: принадлежность проверяет exact version GET; transport принимает только positive Int32 own ETag. [Backend verification](../../backend/features/F01-create-entity-schema.md#14-проверенное-поведение-demo-f01) фиксирует серверные suites и границы проверки. Это readiness F01, без Save/Publish F02/F03 и без утверждения готовности остальных Demo-фич.
