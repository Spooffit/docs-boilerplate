# F04. Экземпляры и версии данных: фронтенд


[Бизнесовая спецификация](../../../business/features/F04-entity-instances.md) · [Backend](../../backend/features/F04-entity-instances.md) · [M01](../modules/M01-content-authoring.md) · [Q36](../../../business/questions/Q36-entity-instance-lifecycle.md) · [Q34](../../../business/questions/Q34-schema-field-semantics.md)


Документ описывает целевой UI по [Q13](../questions/Q13-ui-scope.md) для .NET 10, Blazor Standalone, MudBlazor, FSD и NSwag. Названия будущих блоков — логические обозначения, не утверждение о наличии классов. Вёрстка не задаётся; исходная реализация и отличия приведены в §11.


## 1. Результат и границы


Пользователь находит логический EntityInstance, создаёт его вместе с первым Draft и независимо готовит несколько версий данных под точные SchemaVersion. Он явно сохраняет значения, видит данные вне текущей структуры, разрешает конфликт основы и публикует сохранённый Draft. Published data неизменяемы и доступны в истории.


F04 включает глобальный и schema-contextual списки, создание, Instance Workspace, clean/copy Draft, Save, явный discard orphan, accept-current-base, Publish, допустимое удаление Draft/логического экземпляра, переименование authoring Name и logical withdrawal. MVP добавляет logical Archive/Restore и version Retire/Restore. Archive, Restore и отдельный Retire Published instance version не входят в Demo; старое version-level Archive не переносится как Demo-требование.


Key — immutable runtime/alternate key, уникальный внутри **логической EntitySchema**, включая разные SchemaVersion. Demo вводит его явно до Create. MVP может предложить редактируемый Key из Name до создания; после создания suggestion/rename Key отсутствует. Name — mutable authoring label, меняется отдельным действием logical owner и не попадает автоматически в runtime payload. SchemaId logical owner и SchemaVersionId созданного Draft не редактируются.


Разрешены несколько Draft одного instance, в том числе на одну SchemaVersion. Draft не имеет business number; Published получает серверный следующий integer в общей sequence EntityInstance. Draft по Draft SchemaVersion разрешён; Publish требует доступную Published SchemaVersion. Схемы принадлежат [F01](F01-create-entity-schema.md)/[F02](F02-create-schema-version.md)/[F03](F03-publish-schema-version.md); ресурсный picker/upload — [F05](F05-media-assets.md), direct Delivery — [F13](F13-content-delivery.md). Product assignments/releases и модерация здесь не выполняются.


Demo без входа/ролей/проверки админом; MVP-доступ определяется F16/Q10 отдельно. Explicit Save, dirty guard и ошибки обязательны. Autosave, offline, browser storage несохранённых значений и глобальный store не вводятся. Дополнительные typed constraints и List comparer по [Q35](../../../business/questions/Q35-value-constraints.md) относятся только к MVP; Demo сохраняет базовые правила Q34.


## 2. Страницы, экраны и навигация


| Страница / экран | Точка входа и контекст | Состав и режим | Завершение / переход |
| --- | --- | --- | --- |
| SC01. EntityInstancesPage | /instances; пункт «Экземпляры» | Поиск Name/Key, Schema filter, summaries, server pagination/sort, Create | Выбор → /instances/{instanceId}; Create → SC03 |
| SC02. SchemaInstancesPage | /schemas/{schemaId}/instances из F02/прямой URL | Schema context/breadcrumb, тот же список с закреплённым SchemaId, Create | Instance → SC04; Schema → F02; Create → SC03 с предварительным выбором схемы |
| SC03. CreateInstancePage | /instances/new; SC01/SC02/прямой URL | Name, явный Key, Schema, exact Draft/Published SchemaVersion, dynamic editor, validation, Create/Cancel | Success → /instances/{instanceId}/versions/{instanceVersionId} по initial Draft ID сервера; Cancel → список с guard |
| SC04. InstanceWorkspacePage | /instances/{instanceId} либо /instances/{instanceId}/versions/{instanceVersionId} | Header, schema link, постоянный version rail, exact schema context, Draft editor либо Published view, actions | Rail меняет exact route; schema link → exact F02; результаты обновляют выбранного owner |
| SC05. Новый Draft | SC04 clean/copy выбранной Draft/Published | Режим, exact source/status для copy, exact target SchemaVersion; default target source для copy | Cancel без mutation; success выбирает exact новый Draft |
| SC06. Актуализация основы | Stale Draft SC04 | Baseline, latest Published **той же SchemaVersion**, semantic value diff, предупреждение no merge | Explicit consent → accept; либо новый Draft от current Published |
| SC07. Удаление | SC04 Draft overflow/logical header | Exact удаляемый owner; logical impact/dependencies и удаляемые разрешённые Draft | Cancel без DELETE; success меняет rail либо переводит в список |
| SC08. Подтверждение ухода | Dirty: смена version/target до Create, Reload, Cancel, navigation | Save / Discard / Stay | Save обычный Create/Save; только success разрешает intent; Discard разрешает уход; Stay сохраняет форму |
| SC09. Logical metadata/lifecycle | SC04 header | Name отдельно от values, immutable Key, withdrawal; Archive/Restore MVP | Logical result обновляет header/list; Published data прежние |
| SC10. Version lifecycle [MVP] | Published overflow SC04 | Retire/Restore exact version, влияние на новое использование | Badge/availability этой версии; history в workspace |


Список показывает Name, Key, Schema, число Draft, latest Published global VersionNumber либо отсутствие публикации, logical lifecycle/withdrawal и metadata изменения из server summary. Archived отсутствуют в обычном списке/picker. Search/filter/page/sort сохраняются в query string; поиск Name/Key server-side с debounce, смена поиска/фильтра начинает первую страницу. Query names, page size/debounce, sort/summary profile — Q15; полный каталог для локального поиска не загружается. Contextual SchemaId взят из route; SC02 повторно использует поведение списка.


Без versionId workspace выбирает newest Draft по CreatedAt descending, иначе latest Published по global VersionNumber descending. Draft в rail первыми, Published ниже с тем же порядком групп. Каждый item показывает exact SchemaVersion context; Draft различаются technical ID/временем. Отдельные Title/ChangeComment instance Draft не вводятся. Retired остаётся в rail owner, не глобальном Archive. Exact URL выбирает только указанный versionId этого instance: 404 не заменяется другой версией; доступный header/rail остаётся. Удаление последнего Draft оставляет logical parent с empty history и доступным созданием нового Draft; parent автоматически не удаляется.


SC03 после выбора exact SchemaVersion читает definition и показывает editor. До выбора — подсказка, не редактор неизвестной структуры. Смена Schema/SchemaVersion до Create проходит guard при dirty; сохранённый Draft не получает selector изменения target. Create/clean/copy открывают ID результата, не «самый новый» по времени.


Header, rail и selected version видны одновременно. Published — обычное read-only представление значений/свойств, не disabled editable форма. Breadcrumb подписывается Name, ссылки используют immutable ID. Schema link содержит exact target SchemaVersionId. Archived logical deep link [MVP] остаётся read-only с banner/ссылкой /archive/instances и contextual Restore, если разрешён; отдельная реализация глобального Archive здесь не дублируется.


Refresh читает saved state; unsaved values из browser storage не восстанавливаются. Внешний уход/refresh при dirty использует браузерное предупреждение, не обещающее Save. Context query SC02 → SC03 и возврата — Q15; новый route не изобретается.


## 3. Карта FSD-блоков


Page владеет route/read/edit context и координирует независимые actions. Widget объединяет recursive values presentation; features выполняют законченные действия. Entities представляют generated models; shared предоставляет controls/dialog/transport. Новые C# projects, widget на каждую строку и feature на каждый GET не требуются.


| Имя блока | Слой / срез / сегмент | Ответственность | Использование в этом изменении | Основное описание |
| --- | --- | --- | --- | --- |
| Routes/DI | app | Принятые routes/clients | Адаптировать routes, переиспользовать DI | §2, §8 |
| EntityInstancesPage / SchemaInstancesPage | pages/instance-list, ui/model | Общий список global/schema context | Адаптировать список; contextual composition F02 | §2–4 |
| CreateInstancePage | pages/instance-create, ui/model | Один generated request/schema definition/dirty/navigation | Адаптировать new-режим page | §3–7 |
| InstanceWorkspacePage | pages/instance-workspace, ui/model | Header/rail/exact reads/один Draft request/отдельные ETags | Адаптировать existing page | §3–9 |
| InstanceVersionRail | widgets/instance-version-rail, ui | Группы/выбор/target/lifecycle | Выделить presentation selector | §2–4 |
| InstanceValuesEditor + OrphanSection | widgets/instance-values-editor, ui/model | Recursive controls и данные вне структуры | Generated contract; OrphanSection внутренняя часть | §3–6 |
| Instance/Draft actions | features: create-instance, create-instance-draft, copy-instance-draft, save-instance-draft, accept-instance-draft-base, publish-instance-version, delete-instance-draft | Mutation/pending/errors/result | Заменить HTTP orchestration page | §7–9 |
| Logical/version lifecycle | features: rename-instance, delete-instance, archive/restore-instance, withdraw-instance, retire/restore-instance-version | Конкретный owner/impact/consent | Contextual Demo/MVP scope | §7–9 |
| Instance/version/schema-field presentation | entities/entity-instance, entity-instance-version, schema-field; необходимые сегменты | Labels/badges/read-only generated DTO | Без Domain/DTO-копий | §5–6 |
| Resource picker/upload | F05 public feature/widget | Required-kind search/preview/upload/select AssetId | Переиспользовать F05 | §4–8 |
| Controls/dialog/errors/generated client/headers | shared | Generic ввод/confirmation/transport | Переиспользовать F01/F02 infrastructure | §5, §8–9 |


| Блок | Назначение и входы | Выходы | Владелец состояния / жизнь | Зависимости и использование |
| --- | --- | --- | --- | --- |
| List pages | Route query/optional SchemaId/generated summaries | Query navigation/InstanceId/Create intent | Page query/search/read page/loading/errors до смены context | Generated clients/navigation; SC01/SC02/F02 entry |
| CreateInstancePage | Пустой generated request/exact definition | Request → CreateInstance; confirmed IDs → workspace | Page request/schema selection/dirty/intent; editor UI-state | Editor/action/F02 reads; SC03/SC08 |
| InstanceWorkspacePage | Route IDs/logical/rail/exact generated reads | Selection/requests/own headers → actions; results → refresh/navigation | Page read models/one update request/ETags/dirty/intent | Rail/editor/actions/F02; SC04/SC06–SC10 |
| InstanceVersionRail | Summaries/selected ID/loading/error | Selected(versionId) callback | Presentation; selection у page | Entity presentation; без API; SC04 |
| InstanceValuesEditor | Generated definition/create-update values/orphan projection/readonly/errors; в MVP typed constraints/comparer | Changed same request/addressable discard/asset selection intent; в MVP explicit stable local sort | Только selection/expanded/focus/render keys и неполный transient input; business data у page | Controls/entity presentation/typed comparison helper в MVP; без HTTP; SC03/SC04 |
| Draft actions | IDs/generated request/own-source ETag/lifetime | Pending/errors/confirmed result | Feature текущего запуска; clean/copy target и accept consent dialog | Generated clients/transport/dialog; SC03/SC05–SC08 |
| Lifecycle actions | Logical/version owner/own ETag/Name-consent | Impact/blockers/pending/errors/result | Feature action/dialog; без mutable workspace store | Generated API/confirmation; SC07/SC09/SC10 |


Dialog/guard/transport переиспользуются через границы [F01](F01-create-entity-schema.md#5-зависимости-и-публичные-границы)/[F02](F02-create-schema-version.md#5-зависимости-и-публичные-границы). Schema-field editor не становится values editor: здесь применяются его generated definition и универсальные controls, schema mutation ради instance Save не вызывается.


## 4. Логическая компоновка


~~~text
SC01/SC02. Список экземпляров [pages]
  Breadcrumb + Schema context [SC02]
  Search Name/Key, Schema filter [SC01], query state
  Instance summaries + pagination
  Создать экземпляр


SC03. CreateInstancePage [pages]
  Name / явный Key / Schema / exact SchemaVersion
  Exact schema definition/context [после выбора]
  InstanceValuesEditor [widgets]
    Recursive controls / Object sections / List items
      [MVP Offset] Date / time / visible offset / explicit fraction edit / derived UTC
      [MVP] Constraint hints/issues и явное «Сортировать» applicable initial Draft List
    Resource picker [F05, условно]
  Validation summary
  CreateInstance [features] / Cancel / dirty guard SC08


SC04. InstanceWorkspacePage [pages]
  Breadcrumb / Name / immutable Key / logical Schema link
  Rename / Delete / Withdraw / Archive-Restore [MVP]
  InstanceVersionRail [widgets]
    Draft: ID/time + exact target SchemaVersion
    Published: global VersionNumber/time + target + Retired [MVP]
  Selected exact version
    Exact target SchemaVersion link/status / baseline/issues
    Draft
      Stale banner → SC06
      Orphan warning/count → OrphanSection
      InstanceValuesEditor [widgets]
        Scalar / Object / recursive List controls
          [MVP Offset] Date / time / visible offset / explicit fraction edit / derived UTC
          [MVP] Constraint hints/issues и явное «Сортировать» applicable List Draft
        Resource picker [F05, условно]
        OrphanSection: old field identity/key/path/value, explicit discard
      Validation summary → адресуемая проблема
      Save / Publish / Draft overflow
    Published
      Read-only values / exact schema / server number/hash/time
      Создать Draft на основе этой версии / Retire-Restore [MVP]
  SC05–SC10 [условные dialog/action contexts]
~~~


Orphan warning перед основной формой ведёт к секции **после** неё. Orphan не скрывается collapse нормального control и не представляется обычным target field. Основной ввод в workspace; clean/copy/consent открываются отдельно. Resource picker получает exact field/list item вместе с required AssetKind и возвращает выбранный compatible AssetId/descriptor. Logical Name находится вне version values editor.


| Связь частей | Передаваемые сведения | Координатор | Изменение получателя |
| --- | --- | --- | --- |
| F02 → SC02 → SC03/SC04 | SchemaId/InstanceId/Create context | Page/navigation | Список этой схемы либо instance route без второго nested editor |
| Rail → page → selected area | Exact versionId | Workspace + guard | Route/exact read/definition, old response ignored |
| Editor → page | Changed request/local discard | Page | Dirty; Publish требует Save |
| Editor «Сортировать» [MVP] → page | Stable reorder того же generated List request, прежние ItemKey | Page владеет request/dirty/original ETag | Dirty, values/issue preview обновлены; HTTP только обычным Save |
| Summary/banner → editor | Field/path/orphan address | Page через editor boundary | Expanded context/issue/section/focus |
| Editor → F05 → editor | Field/item definition with AssetKind → confirmed compatible AssetId | Page/widget/F05 public boundary | Local value/dirty; upload не Save |
| Action → page → rail/header/editor | Confirmed owner DTO/own ETag/deleted ID | Page | Selected state/refresh, no wrong owner header |
| Stale context → SC06 → action | Same-target baseline/current/diff/consent | Workspace + accept | Base result без merge/auto Publish |
| Logical action → header/list | Confirmed logical result | Page/read refresh | Name/availability/badge, Published values прежние |


## 5. Зависимости и публичные границы


List composition принимает SchemaId/query и сообщает InstanceId/Create intent; F02 использует эту границу, не внутренности instance page. Workspace передаёт rail summaries/selected ID, editor generated definition/request/orphan projection/readonly/errors и Changed, action IDs/request/нужный ETag. Editor предоставляет адресуемый переход к field/path/orphan. Точные C# signatures определяются после OpenAPI.


Widgets не зависят от pages и не вызывают соседние features. Create/copy/Save/accept/Publish/lifecycle независимы; page связывает result/navigation. Copy/accept не запускают hidden Save: dirty intent разрешается guard. Shared dialog принимает text/choices/disabled и возвращает выбор; mapping/orphan/impact/baseline — business responsibilities, resource feature сохраняет владельца F05.


NSwag-generated models используются напрямую, включая запросы редактирования. Клиентские entities не являются Domain/EF-агрегатами, не импортируют серверные сборки и не создают параллельные ручные FormModel/EditorModel с теми же бизнесовыми полями. Отдельно допускается чистый UI-state. Если контракт не выражает presence, рекурсию, ItemKey, discard или diagnostics, сначала исправляются server DTO/OpenAPI и перегенерируется клиент; ручная бизнесовая модель не является обходом.

Typed constraints/ListComparerDefinition определены один раз в [backend F01 §2.1](../../backend/features/F01-create-entity-schema.md#21-aggregates-entities-и-value-objects); F04 читает generated snapshot exact target, его не редактирует. Чистый клиентский comparison/presentation helper принимает generated definition и values, возвращает доступность/diagnostics/stable order и не получает DbContext/Domain aggregates. Он не создаёт новую business FormModel, feature или серверную mutation. Page применяет локальный reorder через editor Changed; обычная SaveInstanceDraft сохраняет исходный own ETag до подтверждённого ответа, initial create сохраняет обычный no-own-token контракт. Сервер остаётся владельцем итоговой validation.


Общие controls/transport/guard F01/F02 и [S06](../standards/S06-api-client.md) проверяются у create/saved Draft F04; transport/guard также у F01/F02/F03. UI-вложенность OrphanSection не означает feature→page code dependency.


## 6. Данные и владение состоянием


### 6.1. Состояние


| Данные / состояние | Источник истины | Клиентский владелец / жизнь | Обновление / сброс |
| --- | --- | --- | --- |
| Summaries/total/Schema context | DF01/F02 reads | List page/current query | Query/read/result; old response ignored |
| Search/filter/page/sort | Query string; ввод до debounce local | SC01/SC02 | Navigation; contextual SchemaId из route |
| Create metadata/values | Один generated create request | SC03 до success/discard/exit | Ввод/target guard; initial Draft ID server |
| Logical DTO + logical ETag | DF03 exact GET/logical result | SC04/current InstanceId; header generic state | Logical read/result, не list/version token |
| Version summaries | DF04 | SC04/current instance | Load/confirmed result/refresh; не источник exact ETag |
| Exact version DTO + version ETag | DF05/version result | SC04/current VersionId; header рядом с model | Selection/result/explicit reload; не logical token |
| Exact target definition | F02 exact SchemaVersion | SC03/SC04/target | Read/reload; Draft definition mutable под тем же ID |
| Draft values/DiscardOrphanValueIds | Один generated update request | Workspace; snapshot и input различаются | Ввод/discard; success confirmed snapshot/new request/dirty=false |
| Stored orphan/derived projection/issues/base | Saved Content/OrphanValues + read projection | Workspace → editor/SC06 | Read/Save/accept/refusal; DTO→form сохраняет unknown values; GET read-only |
| Selection/expanded/focus/render keys | UI-state | Editor/current context | Не сериализуется как ItemKey/field ID |
| Dirty/guard intent | UI-state | Create/workspace page | Input→dirty; success/discard reset; failed Save preserves |
| Pending/errors/consent | Конкретный action | Feature/current capture | Start/result/cancel; не второй workspace store |


Logical/version ETag независимы даже при совпадении строк. ETag не в DTO; Version/ExpectedVersion/Revision отсутствуют в body/query. Source/edit/submitted snapshot не смешиваются. Pending блокирует competing write/input этого owner, не всю SPA.


Own version ETag защищает сохранённую версию и её mutation. Вычисляемые issues/stale/current-base имеют отдельный diagnostic basis: schema change или sibling Publish могут изменить их при прежнем own ETag. UI хранит эти tokens отдельно и не считает own token валидатором всей projection. Accept/discard consent привязываются к показанному basis; его смена требует перечитывания и нового решения. GET остаётся read-only.


### 6.2. Представление и значения


| Сведение | Источник / применение | Правило |
| --- | --- | --- |
| Name/Key/Schema | Generated create/logical DTO; Name edit separately | Key explicit/immutable/unique per Schema; Name authoring; limits/normalization Q15 |
| Target SchemaVersion | Request до DraftCreate, read после | Draft/Published same schema для Draft; immutable после; exact ID, не latest |
| Number/profile+digest/time | Published generated read | Server number, Draft без number; cm-instance-content/1/SHA-256 (64 lowercase hex без prefix) exact canonical values/frozen media, no instance identity/Key/version/orphans/ItemKey in value bytes; UI не computes/chooses hash/profile |
| Field metadata | Exact definition | Name/Key/Required/Deprecated/HelpText labels/tooltips; Deprecated после non-deprecated siblings — presentation order |
| Orphan | Generated records/read projection | Old field ID/key/path/value, saved server identity либо derived address Q15 |
| Base/current Published | Exact diagnostics/reads | Этот InstanceId **и этот SchemaVersionId**; missing baseline не read error |


Presence отличается от default: required = present + type-valid. Nullable scalar types в Demo отсутствуют; checkbox присутствия выражает отсутствие значения, отдельной null option нет. Present numeric/temporal/Color/Enum/Asset input начинается пустым и intrinsic-invalid до явного ввода; числовой 0 не подставляется. Boolean presence создаёт explicit false, FlagEnum — empty key set, String/Text — present empty string; empty String/Text сохраняются как incomplete Draft, required useful text проверяется при Publish. Intrinsic/transient invalid input блокирует Create/Save и остаётся локально. Ранее сохранённый raw null не стирается: editor показывает repair issue до явного ввода либо очистки поля. Empty Content разрешён в persisted InstanceDraft при clean create и Save; обязательные значения можно заполнить позже. Local clean до first valid Save относится только к SchemaDraft, не instance. Save разрешает missing required, preserved orphan и ранее сохранённые incompatible values в согласованном server recovery profile; newly entered type-invalid values отвергаются. Accept не преобразует payload, разрешает missing required/orphans и проверяет basis. Publish строго recursive. Full Save snapshot: optional absence очищает значение, collapsed/unrendered values сохраняются, orphan удаляется только explicit discard.


| Все 15 типов | Control / read view | Семантика Q34 |
| --- | --- | --- |
| String | Single-line/text | Max255; required непустая строка |
| Text | Rich-text editor/безопасное чтение | Restricted HTML p/br/lists/bold/italic, server sanitizes/returns result; semantic-empty invalid, operational cap Q34/Q22 после fixture отдельно от MVP MinTextLength/MaxTextLength |
| Integer | Integer numeric/число | Integer semantics; 0 valid |
| Float | Numeric/число | Finite, no NaN/Infinity; 0 valid |
| Decimal | Decimal numeric/число | Decimal, не Float; 0 valid; MVP bounds/точный MultipleOf, Precision/Scale отложены |
| Boolean | Boolean + отличимое отсутствие/Да-Нет | false и true present valid |
| DateTime | Exact definition выбирает controls: Naive дата/время; Offset отдельные дата, время, offset, fractional edit и derived UTC | Historical/Demo Naive: `yyyy-MM-ddTHH:mm:ss.fffffff`, DateTimeKind.Unspecified без Z/offset. MVP Offset: DateTimeOffset, invariant `yyyy-MM-dd'T'HH:mm:ss.fffffffzzz` либо UTC `yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'`; ровно 7 fractional digits, обязательный offset |
| Date | Date-only/календарная дата | Без времени/offset |
| TimeSpan | Duration/длительность | Zero/negative valid; exact wire backend |
| Enum | Single select/label+key | String Key; deprecated существующее значение valid с warning, скрыто при обычном новом выборе; numeric Value нет |
| FlagEnum | Multi-select/checklist/chips/set labels+keys | String key set, не bitmask; empty present valid; no business key order |
| Color | Picker+exact text/RGBA | #RRGGBBAA; #00000000 valid; no ColorFormat |
| Object | Collapsible nested section/read nested | Present object recursively checks required children |
| List | Recursive collection/ordered items | Scalar/Object/List definition; empty present valid; List<List<T>> |
| AssetReference | F05 picker/metadata-preview | Required kind из schema definition; only compatible Available assets; search/preview/upload/select, не manual GUID |


List<Object> использует сворачиваемые карточки элементов. Каждый List<Object> occurrence имеет client-generated opaque ItemKey, уникальный внутри своего списка; nested occurrence отдельный. Copy и reorder сохраняют ключи, новый элемент получает новый key. ItemKey отличается от rendering key и не является positional index. Field addresses используют SchemaFieldId и цепочку ItemKey; scalar lists не требуют такой identity. OrderMode.Default сохраняет авторский порядок и разрешает перестановку; Ascending/Descending round-trip сохраняется в Demo, supported comparer/строгая order validation задаются Q35 для MVP.

В MVP editor показывает applicable constraint hints и адресуемые issues выбранной exact definition/recursive Item. Optional absent value пропускает value constraints; Required проверяется отдельно. String length считается в Unicode code points (emoji = 1, combining mark отдельный), без normalization; Text length — decoded text nodes sanitized HTML в document order без layout separators. Sanitized результат приходит из F04 backend; semantic-empty и operational HTML cap отдельны. Numeric/temporal bounds inclusive и typed; Integer/Decimal MultipleOf точный positive step без Decimal→double/rounding; Float MultipleOf, Decimal Precision/Scale, regex и dynamic temporal expressions отсутствуют. [Q34 DateTime MVP](../../../business/questions/Q34-schema-field-semantics.md#datetime-mvp-temporal-semantics-и-совместимость) согласован: existing Naive `/1` не преобразуется, новое Offset имеет отдельный supported profile. Владелец definitions — [backend F01 §2.1](../../backend/features/F01-create-entity-schema.md#21-aggregates-entities-и-value-objects), parsing/authoring/Publish projection/codec — [backend F04](../../backend/features/F04-entity-instances.md).

Offset editor внутри InstanceValuesEditor получает exact generated definition и value/presence, возвращает изменённое value и Changed в один request страницы. Он владеет только transient input отдельных date/time/offset/fraction controls и inline errors; saved business value остаётся в generated request, persistence/API calls — у Create/Save actions. Новый ввод Offset начинается с видимого +00:00; наличие этого offset само по себе не создаёт отсутствующее optional value. Derived UTC instant — read-only пояснение. Изменение offset **сохраняет введённые local дату/время и меняет instant**; рядом показаны пояснение и новый UTC результат. Browser timezone binding, JavaScript Date и floating-point representation ticks не используются. Whole-minute offset ограничен −14:00…+14:00, local date и corresponding UTC instant оба должны укладываться в DateTimeOffset. Missing offset, −00:00, leap second, incorrect 7-digit wire и conversion overflow невалидны; transient неполный ввод остаётся локально, newly entered present parse/type/range-invalid value отклоняет Save.

Форма хранит все семь fractional digits (100ns) при открытии, Save и изменении других fields; явный fraction control позволяет изменить эту часть без скрытого обнуления date/time picker. Authoring Save/read сохраняет original offset и семь digits: 12:00 +03:00 читается как 12:00 +03:00. Read-only **authoring view** показывает original offset из authoring snapshot, read-only **Published runtime view** показывает canonical UTC из runtime projection. Нельзя считать authoring GET с original offset runtime payload. Publish пишет UTC `yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'`; 12:00 +03:00 и 09:00 Z при остальном равном имеют same runtime bytes/hash. Это объявленная projection нового profile, не исправление validator. SDK получает runtime UTC и не восстанавливает original offset по browser/server timezone. Региональные IANA/Windows zones/DST отсутствуют; Date/TimeSpan и system timestamps сохраняют свой смысл.

Offset Minimum/Maximum inclusive и сравниваются по UTC instant. Type-valid value вне bounds сохраняется в Draft с адресуемыми issues и блокирует Publish. Equality/UniqueItems и ItemValue/ObjectFieldPath comparer также используют UTC instant: разные offsets одного instant дают duplicate/full equality и equal sort keys, explicit stable Sort сохраняет authored tie order. Offset не secondary key; Naive/Offset не сравниваются через скрытую conversion.

Type-valid value, нарушающее дополнительный constraint, допускает Save и остаётся в request/result с адресуемой диагностикой, блокирующей Publish; правило одинаково для нового и ранее сохранённого value. MinItems/MaxItems/UniqueItems/order violations также сохраняются как issues. Неполный нечисловой numeric input остаётся transient UI-state без сериализации, newly entered present type-invalid value отклоняет Save согласно базовому F04. Validation не выполняет trim/coercion/default filling/clamp/rounding/dedup/sort. Publish строго проверяет presence/types/nested values/constraints/uniqueness/order и при любом нарушении отклоняет весь result без partial Publish.

UniqueItems сравнивает полный type-aware normalized runtime value по Q19, а не ItemKey либо comparer key: Object fields по FieldId, absent отличается от present default, FlagEnum — set keys, nested Lists учитывают порядок. ItemKey/orphan/authoring metadata исключены; равные Object с разными ItemKey — duplicates. Hash может только индексировать кандидатов, подтверждение — по full values/bytes. Invalid/incomplete items дают issues, UI не объявляет uniqueness доказанной для непроверенных values; partial Draft можно сохранить, Publish требует валидную проверку.

Ascending/Descending в MVP задаёт требование к **сохранённому авторскому array order**. ItemValue comparer поддерживает Integer/Float/Decimal/Boolean/String/Enum/Date/DateTime/TimeSpan: typed numeric/temporal, false < true, String/immutable Enum Key ordinal UTF-16 .NET semantics без culture/case folding/natural sort/Name. List<Object> использует только непустой ordered ObjectFieldPath по required SchemaFieldIds через Object, без List; scalar leaf required и supported. Direct Text/Color/FlagEnum/AssetReference/Object/List comparison, optional/composite/expression keys отсутствуют. Default не имеет comparer; Ascending/Descending требует supported definition.

Явное «Сортировать» доступно только у local editable Instance Draft List с Ascending/Descending, supported comparer и полностью валидными keys; Published read-only. Missing/invalid key в partial Draft даёт issue, объяснение недоступности сортировки и Publish blocker, без null/default substitution. Действие стабильно переставляет локальный authored array, сохраняет ItemKey и относительный порядок равных keys; Guid/ItemKey tie-break отсутствует. Equal keys допустимы, UniqueItems всё равно проверяет full value. Reorder ставит dirty. Cancel/Discard следует обычному guard; у saved Draft восстанавливается saved order. Затем выполняется обычный explicit Create либо Save: initial create не имеет собственного version token, existing saved Draft отправляет полный request с прежним own version If-Match (backend ExpectedVersion), без обновления baseline/token для обхода конфликта. Нет отдельного sort route/server mutation, default autosort, перестановок validator/canonicalizer/Delivery/SDK. Явный reorder меняет value/hash обычным образом при Publish.


Backend копирует совместимые значения по stable SchemaFieldId и identity всей parent-definition chain; перенос поля между parents означает новый FieldId. Исторически удалённый FieldId можно вернуть только с тем же контрактом и глобально monotonic semantics, без сброса истории Required/Deprecated/options. Новые target fields незаполнены, несовпавшие значения сохраняются orphan. GET после изменения Draft schema read-only: persisted orphan IDs устойчивы, derived incompatibility получает basis-bound opaque token и stable path из FieldId/ItemKey; UI не создаёт IDs и не теряет unknown/incompatible values.

MVP Copy сохраняет type-compatible value по stable FieldId и при нарушении target constraint показывает issue; одно такое нарушение не превращает value в orphan и не требует нового FieldId схемы. Ужесточение/ослабление constraints новой SchemaVersion не меняет Published history, Key/Type/container identity и monotonic Required/Deprecated остаются F02/F03. Issues изменившегося mutable Draft target показываются с актуальным diagnostic basis, независимо от own instance ETag.

Naive→Offset — смена semantic identity по [F02](F02-create-schema-version.md#62-состав-представления-и-формы): delete старого field + add нового server FieldId. Instance Copy по stable IDs оставляет removed Naive value orphan и новое Offset field незаполненным. Specialist вручную вводит date/time и offset в target и явно discard-ит orphan через обычный Save; нет auto migration, user/server timezone conversion, fuzzy/name/key mapping. Copy unchanged historical Naive сохраняет прежний смысл и precision.


Demo показывает старый field ID/key/path/value и явное удаление orphan. Persisted orphan передаётся через generated explicit discard ID, computed incompatibility — через возвращённый opaque token и diagnostic basis. UI не вычисляет token и не заменяет ItemKey индексом. Локальный discard делает форму dirty; запроса нет до Save. Cancel восстанавливает серверные данные; confirmed Save применяет discard. Basis mismatch отвергает request, сохраняет ввод и требует fresh diagnostics/new explicit discard. Publish блокирован при orphan и unsaved discard.


Ручное исправление означает отдельный ввод допустимого значения в выбранное target field и явный discard прежней orphan-работы с последующим Save. Это два осознанных локальных изменения одного request. Автоматический перенос в «похожее поле» и восстановление orphan в target не выбраны; отдельный editable orphan-input или endpoint для него не вводится.


## 7. Действия и клиентские потоки


| Действие | Начало / запуск | Исполнитель | Обработка / API | Результат |
| --- | --- | --- | --- | --- |
| UI01. List/search | SC01/SC02 query | List/query client | DF01; schema reads F02 | Summaries/total/empty/error/context |
| UI02. Create structure | SC03 schema/target | Page/query client | F02 list/exact definition + guard | Exact editor, no heuristic transfer |
| UI03. Create | SC03/SC08 Save | CreateInstance | Capture generated payload + operation ID → DF02 no If-Match | Server IDs → SC04; unknown outcome replay same ID/payload, mismatch conflict |
| UI04. Open/select version | SC04 route/rail | Workspace/query client | DF03/04/05 + exact target F02/guard | Own read/headers; Draft/Published mode |
| UI05. Clean Draft | SC05 clean/target | CreateInstanceDraft | Generated exact target → DF06, immediate server create с operation ID | Persisted Draft с empty Content/OrphanValues разрешён; server ID/base сразу. Local до first valid Save относится только к SchemaDraft F02 |
| UI06. Copy | SC05 saved Draft/Published source | CopyInstanceDraft | Target default source/allowed same schema → DF07/Draft source ETag | New identity/target/mapped-orphan result; source прежний |
| UI07. Input/collection/resource | Editable SC03/SC04 | Editor/F05 | Local generated values/presence/add/remove/reorder/AssetId | Dirty; upload не Save |
| UI08. Discard orphan | OrphanSection Draft | Editor→page | Local explicit IDs/address Q15, **без API** | Pending discard/dirty, Save required |
| UI09. Save | SC04/SC08 | SaveInstanceDraft | Draft validation → DF08 version ETag/values/discards | Exact DTO/new ETag/dirty=false/read refresh |
| UI10. Accept base | SC06 saved stale | AcceptInstanceDraftBase | Same-target diff/consent → DF09 Draft ETag | Base/header updated, content/orphan no merge, no auto Publish |
| UI11. Draft from current Published | SC06 alternative | Page→copy action | Exact same-target current Published → UI06 | New Draft; stale Draft preserved |
| UI12. Publish | Saved clean Draft/Published target/no blockers | PublishInstanceVersion | One DF10 Draft ETag, no hidden Save/merge | Exact Published/new ETag/readonly/server global number, refresh |
| UI13. Delete Draft | SC07 exact Draft | DeleteInstanceDraft | Warning/consent→DF11 version ETag | Deleted identity/default selection §2 |
| UI14. Delete instance | SC07 logical | DeleteInstance | DF12 impact/blockers/consent→DF13 logical ETag | List; history dependency не обходится |
| UI15. Rename Name | SC09 | RenameInstance | Generated Name→DF19/logical ETag; route Q15 | Header/list Name; Key/Published payload прежние |
| UI16. Archive/Restore [MVP] | SC09 | Logical lifecycle | Consent/context→DF14/15 logical ETag | Archive readonly/hidden; Restore authoring, не Publish |
| UI17. Retire/Restore [MVP] | SC10 Published | Version lifecycle | Consent/context→DF16/17 version ETag | Badge/new-use; history/content/number прежние |
| UI18. Withdraw / ReturnToDelivery | SC09 logical header | WithdrawInstance / contextual return action | Logical If-Match; Return — ContentModeling F04/DF20 с Published eligibility, exact transport Q15 | Withdraw: next sync Remove; Return: next NEW plan observes committed availability, no implicit Publish/reset |
| UI19. Leave/Cancel/Reload | Dirty/explicit check | Page/guard/query | Save/Discard/Stay; unknown→exact read, no repeat write | Intent after success/discard; old response не context replacement |
| UI20. Сортировать [MVP] | Local editable Draft SC03/SC04, List Ascending/Descending/supported comparer/valid keys | InstanceValuesEditor → page | Stable local reorder generated values/Changed, **без API** | Dirty/прежние ItemKey/stable ties; UI03 обычный Create либо UI09 Save с исходным own If-Match; Published недоступно |


Copy переносит сохранённый source: dirty guard позволяет явный Save либо Discard введённого source input. Accept не принимает unsaved values скрыто: dirty intent разрешается сначала. Stale Draft можно Save, нельзя Publish; Save не актуализирует baseline. Diff Added/Changed/Removed/collection/AssetReference сравнивает baseline и current Published того же target; окончательные semantic diagnostics/wire/addressing — Q15/Q19.


Область baseline отличается от области numbering: Publish по другой SchemaVersion увеличивает общий номер и не делает Draft stale. Publish sibling по тому же target делает stale независимо от own ETag. Для concurrent different-target Publish сервер коротко сериализует numbering; если обе relevant bases valid, оба succeed с distinct global VersionNumber. UI не назначает номер и не повторяет Publish автоматически; обычный counter race не подменяется same-target stale.


Publish blocked по Draft/Retired/unavailable/withdrawn target или withdrawn logical owner, orphan/invalid values/stale/dirty. Server authoritative, known blockers объясняют disabled reason. Mutation success applies exact DTO/header; related read refresh separate, hidden Save/GET не заменяет mutation result. Create captures operation ID+payload: durable same-ID/same-payload replay возвращает result, mismatch conflict; unknown outcome не creates новый ID для blind retry.


One pending capture IDs/ETag/request/lifetime предотвращает double click/competing write. Context change/exit не позволяет late result менять editor/route. Browser cancellation не доказывает отмену commit. Network/timeout не auto retry/merge и не proof отказа; explicit read выясняет result. Logical rename/lifecycle с dirty version проходят guard и не сбрасывают values молча.


## 8. Использование API и NSwag


Источник контрактов — [backend F04](../../backend/features/F04-entity-instances.md). Таблица описывает потребление, не копию DTO. Final generated methods/envelopes/headers/diagnostics — [Q15: F04](../../backend/questions/Q15-api-and-source-code.md#f04-детализация-целевого-контракта).


Save отправляет full snapshot через согласованный server recursive union: explicit variants сохраняют presence/type/ItemKey/orphans без ручной DTO-копии. Сервер возвращает полный согласованный результат, включая sanitized Text и diagnostics; HTTP representation/validator carrier закрепляет backend F04/Q15. UI применяет его напрямую без скрытого GET для обхода неполного mutation result.


| UI-поток | Серверная операция | Вход | Используемый результат | Владелец |
| --- | --- | --- | --- | --- |
| UI01 | F04/DF01 GET /api/v1/entity-instances | Query/search/schemaId/page/sort | Summaries/total | List/query client |
| UI03 | DF02 POST /api/v1/entity-instances | Generated metadata/schema/target/Content/OperationId; no If-Match | Exact initial Draft DTO с InstanceId/Id и собственным version ETag | CreateInstance |
| UI04 header | DF03 GET /api/v1/entity-instances/{instanceId} | InstanceId | Logical metadata/lifecycle/latest/own ETag | Workspace/query |
| UI04 rail | DF04 GET /api/v1/entity-instances/{instanceId}/versions | Instance/list query | Summaries/target context | Workspace/query |
| UI04/19 exact | DF05 GET /api/v1/entity-instances/{instanceId}/versions/{versionId} | Exact IDs | Content/orphan projection/issues/base/status/own ETag | Workspace/query |
| UI02/04 schema | F02/DF01 versions; F02/DF02 GET /api/v1/entity-schemas/{schemaId}/versions/{schemaVersionId} | Exact schema/target | Precise definition/status | Page/public F02 reads |
| UI05 | DF06 POST /api/v1/entity-instances/{instanceId}/versions | Generated target/new resource | Created Draft identity/DTO/header | CreateInstanceDraft |
| UI06/11 | DF07 POST /api/v1/entity-instances/{instanceId}/versions/{sourceVersionId}/copies | Generated target/Draft source If-Match | Created Draft/mapping/orphans/header | CopyInstanceDraft |
| UI09 | DF08 POST /api/v1/entity-instances/{instanceId}/versions/{versionId}/save | Full Content/discards/DiagnosticsBasisToken/version If-Match | Exact reconciled Draft/new ETag/issues | SaveInstanceDraft |
| UI10 | DF09 POST /api/v1/entity-instances/{instanceId}/versions/{versionId}/accept-current-base | Draft If-Match/shown PublishedInstanceVersionId/DiagnosticsBasisToken; no merge | Base/result/new ETag/blockers | AcceptInstanceDraftBase |
| UI12 | DF10 POST /api/v1/entity-instances/{instanceId}/versions/{versionId}/publish | Draft If-Match/no editor content/local number | Published exact DTO/new ETag/number/hash/time | PublishInstanceVersion |
| UI13 | DF11 DELETE /api/v1/entity-instances/{instanceId}/versions/{versionId} | Draft/version If-Match | Deleted identity | DeleteInstanceDraft |
| UI14 preview | DF12 GET /api/v1/entity-instances/{instanceId}/delete-impact | Logical ID | Allowed impact/count/blockers; final profile Q15 | DeleteInstance |
| UI14 delete | DF13 DELETE /api/v1/entity-instances/{instanceId} | Logical If-Match/consent | Deleted logical identity | DeleteInstance |
| UI16 [MVP] | DF14/15 POST /api/v1/entity-instances/{instanceId}/archive либо /restore | Logical If-Match | Logical lifecycle/new ETag | Logical lifecycle |
| UI17 [MVP] | DF16/17 POST /api/v1/entity-instances/{instanceId}/versions/{versionId}/retire либо /restore | Published version If-Match | Lifecycle/new version ETag | Version lifecycle |
| UI18 | F04/DF18 POST /api/v1/entity-instances/{instanceId}/withdraw; F04/DF20 POST /api/v1/entity-instances/{instanceId}/return | Logical If-Match; bodyless | Confirmed logical DTO/new ETag; Publish/history unchanged | WithdrawInstance / ReturnToDelivery |
| UI15 | DF19 PUT /api/v1/entity-instances/{instanceId}/metadata | Generated Name/logical If-Match | Metadata/new ETag | RenameInstance |
| UI07 resource | F05 public picker/upload/download/API | Required kind + search/file/AssetId | Compatible AssetId + descriptor/preview/download | F05 action/page receives selection |


Version-level delete-impact route не вводится: Draft использует DF11/server validation, logical — DF12. Последний Draft удаляется с сохранением empty logical parent. Любое изменение logical delete impact отвергает команду; новый exact set требует нового consent. Copy Draft/Published использует собственный ETag exact source; token другого owner не подставляется. Withdrawn owner/target допускает read/Copy/Save, блокирует Publish/runtime; отдельный ReturnToDelivery при наличии Published history требует полной eligibility; для ранее Withdrawn owner без Published снимает только own запрет как recovery и не выполняется Publish автоматически. Retired source/target — MVP policy.

UI20 не добавляет endpoint: initial Draft отправляется обычным UI03/DF02 Create без собственного version token; existing Draft — UI09/DF08 Save с исходным exact Draft If-Match, затем применение confirmed DTO/new ETag/issues. MVP constraints и comparer приходят через generated exact schema definitions; diagnostics сохраняются по существующим field/ItemKey paths. Wire/path contract уточняется Q15, business semantics Q35 уже согласованы.


Current infrastructure: ContentManager.ApiClient.Generated.IEntityInstanceClient/IEntityInstanceVersionClient/IEntitySchemaClient/IEntitySchemaVersionClient; src/ContentManager.ApiClient/nswag.json → OpenApi/Generated/ContentManagerApiClients.g.cs; ConfigureApi/AddContentManagerApiClients/ApiEnvelopeDelegatingHandler. F05 использует IMediaAssetClient через свой action. Generated read/create/update models используются напрямую с set-accessors; generated file no manual edits.

Demo materialization 08.10.2026: generated `InstanceValuesDto` содержит named fields; `InstanceValueDto` — closed Scalar/Object/List envelope. `ScalarJson` хранит typed JSON literal строкой, сохраняя Int64/Decimal/Naive precision через NSwag. Object несёт named fields, List — anonymous `InstanceListItemDto`; ItemKey задаётся только direct List<Object>. Адреса diagnostics: `/fields/{Guid:D}/items/@{UriEscapedItemKey}` и `/items/{index}` для anonymous scalar/list items. Полный Content клонируется в generated request; рендер не отбрасывает неотображённые значения. Server-returned IDs/computed tokens вместе с DiagnosticsBasisToken используются для explicit discard. До Create UI читает configured TextMaxLength из GET `/api/v1/entity-instances/authoring-settings`; после — из exact version result.

Generated методы имеют route-derived имена: logical `EntityInstancesGETAsync/GET2Async/POSTAsync/DELETEAsync`, `MetadataAsync`, `DeleteImpact3Async`, `WithdrawAsync`, `ReturnAsync`, `AuthoringSettingsAsync`; version `VersionsGET3Async/GET4Async/POST2Async/DELETE2Async`, `Copies2Async`, `SaveAsync`, `AcceptCurrentBase2Async`, `Publish2Async`. Эти имена проверены после configured generation и не выбираются по имени controller action. Минимальный media picker использует `MediaAssetsGETAsync(search, kind, skipCount, maxResultCount)` и server-confirmed string Kind/Status; MIME не определяет совместимость. Exact metadata — `MediaAssetsGET2Async`; download failure не меняет persisted AssetId. Полный gateway/library lifecycle F05 здесь не утверждается готовым.

Logical details и list summaries получают optional server `SchemaName`: Schema links показывают authoring Name без загрузки полного selector catalog и сохраняют immutable SchemaId в URL. Diagnostic buttons используют текущие field Names и positions List items; Schema, baseline и orphan имеют отдельные понятные group labels. Исходный exact Path сохраняется в `data-error-path` и focus callback; label не превращается в новую business identity. Orphan issues ведут к orphan section, остальные адресуемые issues — к recursive editor.

Подтверждённый Publish применяет version DTO/own ETag, затем отдельно обновляет logical details/own ETag лёгким root GET: Publish меняет logical publication counter и его Version. Это не hidden exact-version GET для восстановления неполного mutation result. Сбой root refresh оставляет подтверждённый Published результат, показывает отдельную read error и явный retry; прежний logical token больше не используется для logical mutations. Legacy read result несёт `LegacyFields` и original `LegacySchemaFields`: исторические names/nested values показываются read-only, raw ValueJson всегда escaped text, без HTML execution/normalization/sanitization либо автоматической конвертации. CopySelected/CopyCurrent для legacy source отключены и защищены в orchestration; поздно прочитанный legacy current-Published source также не запускает Copy.


Strong exact ETag в generic state рядом с model. Save/accept/Publish/Draft delete/Retire/Restore — version header; Rename/logical delete/Archive/Restore/Withdraw — logical. Missing success headers/per-request If-Match исправляются в общей generation/transport infrastructure по S06. ETag/ExpectedVersion DTO fields и hidden result GET не добавляются; rail/header refresh после confirmed result не повторяет mutation.


412 — изменение exact resource; 428 — отсутствие обязательного If-Match, integration defect; 409 instance-draft.base-outdated — same-target stale base. Schema-version.not-published/retired/dependency/lifecycle причины различимы по machine code. Полный status/code/field path/nested ItemKey/orphan diagnostic profile — Q15. UI не угадывает причину по произвольному error text; адресуемая field issue ведёт к control, неизвестный адрес остаётся видимым в summary.

По [Q19 v1](../../backend/questions/Q19-content-hashes.md#принятый-canonical-profile-v1) Published immutable bytes/profile+digest сохраняются вместе, no historical rehash. Absent/null/default различаются, strict typed canonical serializer не default-fill/rounds numbers; authoring UI не пересчитывает Published hash/не даёт profile picker. Frozen Asset descriptor/hash не live media enrichment; binary Asset SHA-256 отдельный.


## 9. Состояния, ошибки и доступ


| Ситуация | Видимый результат | Действия | Данные / восстановление |
| --- | --- | --- | --- |
| List loading/empty/no matches/failure | Distinct list states | Search/reset/retry/Create | Error не empty; query preserved |
| Header/rail/exact/definition loading | Affected area loading | Allowed read/navigation | No mutation without exact/ETag; failed refresh retains safe confirmed data |
| Exact404 | Missing selected owner/version | Rail/back/reload | No fallback latest/wrong owner |
| Dirty/incomplete Draft | Unsaved/errors | Input/Save/guard | Incomplete Draft allowed profile; no hidden Publish Save |
| Orphan/pending discard | Count/section/old value/local discard | View/discard/Save | No silent loss/no unsaved Published readiness |
| Changed Draft schema | Same ID/new definition/issues/projection | Read/repair/Save | GET read-only, no migration/cleanup; unknown preserved |
| Pending/success | Pending→confirmed notice | Competing owner input/write blocked | Captured one request/result/header/refresh |
| Value/required/asset validation | Inline/summary/asset availability | Repair/picker/Save | Input retained; byte download failure не changes AssetReference |
| Type-valid constraint violation [MVP] | Inline/summary по field/ItemKey и Publish reason | Explicit Save допустим, исправить/Save/Publish после valid saved result | Значение сохраняется с issue; нет coercion/dedup/autosort |
| Missing/invalid order key [MVP] | Issue у path/item, «Сортировать» недоступно с причиной | Ручной ввод/Save partial Draft | Publish blocked; null/default key не подставляется; invalid/incomplete uniqueness не объявляется proof |
| Local sort / 412 [MVP] | Новый local order/dirty либо server-changed | Ordinary Save / explicit Discard-Reopen | Stable ties/ItemKey сохранены, исходный If-Match; conflict сохраняет local order без fresh-token retry |
| 412 | Persistent server-changed | Explicit Reload/Reopen/guard | Nested input retained; no new ETag retry/merge |
| 409 stale-base | Same-target base/current/diff | Save/explicit accept/copy | Distinct412; other-target не stale |
| Cross-target counter race | Оба допустимых Publish сериализованы короткой server critical section | Принять два разных global VersionNumber при valid own baseline | Other-target Publish не делает Draft stale; UI не присваивает номер |
| Target Draft/Retired/unavailable | Exact target/Publish reason | Continue Draft/F02/new allowed Draft | Existing target immutable; schema не Published instance action |
| Delete blocked/impact changed | Dependency/current impact | Cancel/refresh preview | Preview не authority; consent/recheck Q15/Q36 |
| Archived logical [MVP] | Read-only/banner/Archive link | Restore if allowed | No new Draft/Publish/use; history retained, child status not rewritten |
| Retired version [MVP] | Badge/history/owner rail | Restore/allowed actions | No new use/historical refs intact; not global Archive |
| Withdrawn logical | Withdrawal badge; без Published — «Нет опубликованных версий» | Read/Copy/Save; explicit ReturnToDelivery после Published проверки либо recovery только собственного запрета без Published | Publish/runtime blocked пока withdrawn; Publish не снимает withdrawal; next sync Remove |
| 428 | Integration error | Preserve context/fix transport | No user workaround missing mandatory header |
| 403 | Access denial distinct404 | Back/preserve context | No Demo login/roles |
| Network/timeout after send | Result not confirmed | Explicit exact check; Create replay same operation ID+payload | Нет blind retry нового ID; durable same-ID replay, mismatch conflict |
| Refresh failure after success | Confirmed state + read error | Retry affected read | Success retained/mutation not repeated |


По [S05](../../../catalog/docs/standards/ui/forms-and-accessibility.md#form-state-and-user-feedback)/[S07](../../../catalog/docs/standards/ui/forms-and-accessibility.md#access-and-semantics) labels/presence/required/errors/disabled reasons понятны без цвета. Actions/recursive collections/picker/issue navigation доступны с клавиатуры; summary раскрывает адресуемый container/focus; dialogs возвращают focus. Orphan/list имеют path/item context, read-only позволяет изучать history. Реализация controls/focus не задаётся markup.


Для Draft-only/пустого экземпляра «Снять с выдачи» недоступно с причиной «Нет опубликованных версий»; отсутствие WithdrawnAt само по себе не отображается как готовность к выдаче. Уже возникший Withdrawn без Published допускает явное recovery «Вернуть в выдачу»: UI объясняет, что будет снят только запрет и Draft не появится у клиента. Own ETag/If-Match и серверный результат обязательны. Массовая фиксация exact Draft из списка использует общий target workflow [F07](F07-product-content-assignment.md#мастер-подготовить-выпуск); кнопка «Зафиксировать выбранные версии» не выбирает latest и не выполняется обычным Save.

## 10. Проверки и приёмка


Это target criteria по [S08](../standards/S08-testing.md), не результаты UI tests. Библиотека не выбирается. API doubles проверяют orchestration; validators/mapping/persistence/concurrency/history требуют real backend.


| Сценарий | Условия / воздействие | Наблюдаемый результат | Граница |
| --- | --- | --- | --- |
| Lists/context | Global/schema/query/browser/empty/error | Server query restored/context/routes/no full local catalog | Page/query double; real query separately |
| Create | Explicit Key/Draft-Published target/incomplete allowed data | One generated POST/no header/server initial ID/errors retained | Component/transport; uniqueness/atomicity real backend |
| Rail/exact | Multiple Draft/targets/global numbers/invalid exact | §2 order/default/target labels/Draft no number/no fallback | Page/query; real ownership |
| Generated boundary | Create/Save presence/recursion/discards | Direct generated/no copies/technical fields/correct serialization | Editor/generated integration |
| 15 types/defaults | Missing/0/false/zero-negative duration/empty sets-list/transparent RGBA | Presence/types/nonempty String-Text/finite Float/Decimal distinct | Widget/double + real round-trip |
| Recursive lists | Object/List<Object>/List<List<T>>/ItemKey/Default reorder | All levels/key identity/no GUID item ID/invented comparator | Editor/API; final wire Q15 |
| MVP Save vs Publish | Новые/сохранённые type-valid bounds/length/count/step/order/unique violations; новый type-invalid/transient numeric input | Constraint violations Save с addressed issues, Publish blocked целиком; type-invalid rejected/transient local; no coercion | UI/generated/transport doubles + real backend validation |
| MVP uniqueness | Equal Object с разными ItemKey, absent/default, reordered nested List, FlagEnum keys, hash collision candidates/invalid items | Full value equality, не comparer key/ItemKey; no false uniqueness proof для invalid values | Presentation/diagnostics + real runtime equality fixtures |
| MVP stable sort | Initial/saved Draft, supported scalar/required Object path, equal keys/missing-invalid keys/unsupported/Published | Explicit local reorder только Draft; stable ties/ItemKey; invalid-key unavailable; no default autosort/new route | Pure helper/editor/page + generated transport |
| MVP sort persistence/conflict | Sort→Cancel/Create/Save; concurrent saved version change | Ordinary initial Create без own token либо один PUT original If-Match; Discard saved order; 412 сохраняет local order, no token retry | UI/transport + real optimistic concurrency |
| MVP Copy/evolution | Type-compatible value нарушает stronger target constraint, same FieldId | Value retained + issue, не orphan; Published readonly/history прежняя; Save допускает, Publish blocks | UI/API + real Copy/SchemaVersion history |
| MVP Offset input/precision | New value +00:00; offset change; 100ns fraction; открытие/редактирование соседнего field | Visible offset/derived UTC, local date/time сохранены при смене offset, instant меняется с пояснением; все 7 digits round-trip, explicit fraction edit | Editor/generated transport + real authoring read; target criteria, не выполненные tests |
| MVP Offset intrinsic validation | ±14:00/date rollover, overflow local/UTC, missing offset/−00:00/leap second/invalid digits | Valid boundaries сохраняются; present parse/type/range-invalid Save rejected, input/errors retained | Editor/API doubles + pure codec/real backend fixtures |
| MVP Offset runtime/constraints/List | Equivalent instant offsets, out-of-bounds type-valid value, duplicate/full values, tied comparer keys | Authoring original offset, Published runtime UTC 7digits Z/same hash; bounds issues Save allowed/Publish blocked; duplicates issue/stable authored tie order | Two read views + real backend/new-profile golden fixtures |
| MVP temporal Copy | Delete Naive + new Offset FieldId, exact historical source | Old value orphan/new blank, ручной ввод+offset/discard Save; no timezone/name/key conversion, old `/1` unchanged | UI + real stable-ID Copy fixtures |
| Copy | Draft-Published/other same-schema target/renamed metadata/unmatched | Only SchemaFieldId/new empty/unmatched orphan/source ETag | UI/transport + real mapping |
| Orphan read/projection | Changed schema/read/form/Save | Stored IDs stable/derived visible/GET no write/unknown retained | Editor/query; real persistence |
| Discard | Local discard→Cancel/Save | Pending dirty/Cancel restores/confirmed Save removes/Publish blocked before Save | UI/double + real persistence |
| Save/headers | Logical A/version B/source C/stale own-current | POST Save B/copy C/logical A/stale Save allowed/full result/no hidden result GET | Transport + real HTTP |
| 412/428 | Save/copy/accept/delete conflict/header issue | Distinct/preserved nested input/explicit read/no retry-merge | Feature/page/double |
| Base/numbering | Same-target versus other-target concurrent Publish | Same-target stale; different-target serialized successes с distinct global numbers, если обе bases valid | UI + real concurrency |
| Accept | Saved stale/missing required/orphan; basis changes during consent | No payload transform; accepted incomplete Draft, mismatch отказ/new consent | UI/API + real baseline |
| Publish | Dirty/orphan/invalid/Draft-Retired target/valid saved | Reasons/no hidden Save/one ETag POST/exact readonly/server number-hash | UI/transport + real strict Publish |
| Delete | Draft/logical nohistory/Published dependencies | Own header/logical impact/consent/no invented version-preview/no forbidden delete | UI double + real atomic dependencies |
| Metadata/lifecycle | Rename/Archive-Restore/Withdraw/dirty input | Name independent/Key immutable/Demo-MVP/header/guard/history | UI/API + real direct sync |
| Version lifecycle [MVP] | Published/history/new-use | Version header/owner rail/no content rewrite/global Archive | UI/API + real lifecycle |
| Resource | Existing compatible selection/upload/Cancel/download failure | AssetId identity + resolved descriptor; kind enforced; dirty/upload не Save/no corrupt value | F05/F04 composition; media separately |
| Async/accessibility | Double click/late context/network/keyboard | One pending/no late route replacement/no blind retry/focus-context | Component/lifetime/browser |


Real E2E: instance Draft по Draft SchemaVersion → Save/reopen exact → schema Publish F03 → explicit instance Publish → read-only result → multiple clean/copy Draft → same-target stale/accept. Отдельно cross-target copy/orphans/discard-Save и different-target Publish без stale. Canonical/hash golden vectors и exact generated signatures не выбираются молча; принятые Text/ItemKey/addressing правила проверяются как target. MVP lifecycle не Demo acceptance; Markdown/link checks не tests приложения.


## 11. Последовательность изменений и открытые вопросы


Backend wire materialization предшествует configured OpenAPI/NSwag generation, затем адаптации lists/create/workspace/rail/editor/orphans/actions с раздельными owner ETags. Schema reads, controls, guard и transport переиспользуются. Local flows и real copy/Save/Publish/concurrency имеют разные verification boundaries; MVP lifecycle остаётся в своей области. Реализованная Demo поверхность и выполненные UI checks ниже отделены от полной приёмки реального backend и браузера.


| Вопрос | Блокируемые детали |
| --- | --- |
| [Q15: F04](../../backend/questions/Q15-api-and-source-code.md#f04-детализация-целевого-контракта) | Demo generated union/requests, ScalarJson, ItemKey paths, basis/discard tokens, own ETags/full results, configured Text cap, metadata/return routes и delete consent materialized; см. §8. Real transaction/commit/replay/concurrent impact authority подтверждается backend и integration отдельно |
| [Q36: оставшиеся границы F04](../../../business/questions/Q36-entity-instance-lifecycle.md#f04-оставшиеся-продуктовые-границы) | Save/Accept incomplete profile, Withdrawn authoring/return, последний Draft, changed exact impact, deprecated options и serialized cross-target numbering приняты; Retired MVP детали отдельно |
| [Q19 v1 values](../../backend/questions/Q19-content-hashes.md#принятый-canonical-profile-v1) | cm-instance-content/1/typed values/frozen descriptor/profile+digest selected; exact carriers/kernel/golden vectors engineering; UI hash не computes |
| [Q34](../../../business/questions/Q34-schema-field-semantics.md) / Q15 | Text restricted HTML/sanitized result/semantic-empty, historical Naive 7-digit Unspecified и MVP Offset semantics приняты; numeric operational cap определяется fixture/Q22; exact OpenAPI/NSwag temporal union/read-view carriers — Q15 |
| [Q35](../../../business/questions/Q35-value-constraints.md) | MVP typed constraints/Save-Publish-Copy/full equality/stable explicit Sort и Offset instant semantics согласованы; exact whole new-profile codec/extension carriers/literal golden bytes — engineering Q19/Q31; Demo не расширяется |
| [Q13](../questions/Q13-ui-scope.md) | UI testing/details; routes/explicit Save/guard/recursive form/Archive scope приняты |


Подтверждённая реализация UI 08.10.2026:

| Источник | Реализованная Demo поверхность | Граница проверки |
| --- | --- | --- |
| [EntityInstancesPage.razor](../../../../contentmanager.ui/src/ContentManager.Pages/Components/ContentModeling/EntityInstances/EntityInstancesPage.razor) | /instances и schema-contextual route, server Name/Key search, query pagination, schema search/filter, lightweight summaries/SchemaName/DraftCount/withdrawal | Compile и generated summary rendering; real query и browser отдельно |
| [EntityInstancePage.razor.cs](../../../../contentmanager.ui/src/ContentManager.Pages/Components/ContentModeling/EntityInstances/EntityInstancePage.razor.cs) | Exact header/rail/version reads, multiple Draft, clean/cross-target copy, immutable target/Key, atomic initial create, separate logical/version/source ETags | Generated transport + HtmlRenderer fixtures |
| EntityInstancePage Save/Publish/Accept/Delete/Metadata/Withdrawal | Full confirmed DTO без hidden exact GET, dirty/pending/unknown/412 guards, same-target consent/diff, orphan discard, exact logical impact, separate Name and withdrawal/return | PostgreSQL/concurrency/sync authority проверяется backend отдельно |
| [ModernInstanceValuesEditor.razor](../../../../contentmanager.ui/src/ContentManager.Widgets/Components/ContentModeling/EntityInstances/ModernInstanceValuesEditor.razor), InstanceFieldsEditor/InstanceValueControl | Direct generated recursion, absence через presence без null option, all15 types, intrinsic-invalid blank input/Save guard, Naive7digits, long/decimal, key Enum/FlagEnum, RGBA picker, restricted HTML input/server-cleaned read view, stable ItemKey/path/focus | All15 rendering/serialization fixtures; interactive browser отдельно |
| LegacyInstanceValuesView.razor — исторический snapshot 08.10.2026, файл отсутствует в текущем checkout | Original field names из LegacySchemaFields, recursive read-only LegacyFields, escaped raw ValueJson; no legacy editing/automatic conversion | Generated carrier/rendered safe raw HTML fixture |
| [InstanceDraftEditing.cs](../../../../contentmanager.ui/src/ContentManager.Features/InstanceWorkspace/InstanceDraftEditing.cs), InstanceScalarInput/InstanceBaseDiff/InstanceIssuePresentation | Full snapshot clone, explicit saved/computed orphan discard, transient invalid text, own precision parsing, semantic base diff excluding ItemKey, readable diagnostic labels retaining exact focus addresses | Focused runner checks |
| [InstanceAssetPicker.razor](../../../../contentmanager.ui/src/ContentManager.Widgets/Components/ContentModeling/EntityInstances/InstanceAssetPicker.razor), InstanceAssetView | Server search/page/verified Kind+Available selection, upload, selected metadata/download; failure preserves field reference | Generated HTTP doubles; real bytes/storage/browser отдельно |
| Generated client/metadata scope | Configured NSwag generation, mutable generated DTOs, scoped response ETag and per-request If-Match; no Domain/EF references | Build/generated transport; source no manual edits |

Published asset rendering после docs 414b725 использует generated `PublishedAssetReferences` exact version result через Page → ModernInstanceValuesEditor → InstanceFieldsEditor → InstanceValueControl → InstanceAssetView, включая nested Object/List. Непустой либо пустой collection задаёт Published context: view показывает пять frozen facts, а missing/conflicting descriptor — snapshot warning без live metadata GET/fallback. Draft picker остаётся по текущему compatible Available asset и сохраняет только AssetId. Current F05 download явно подписан отдельно; failure не меняет frozen facts или reference. Domain/HTTP и renderer regressions подтверждают это при изменённых/недоступных current media facts, повторяющемся AssetId и переходе Draft → Published с тем же ID. DeliveryMode остаётся post-Demo в Q37; новый read projection не добавляет mode/URL в runtime bytes.

Unused legacy InstanceValueEditModel/InstanceValuesEditor удалены после замены modern values editor. В snapshot 08.10.2026 отдельный LegacyInstanceValuesView сохранял read-only historical recovery; его текущая реализация не подтверждается прежним описанием. Publishing picker и MediaAssetsPage получили только необходимую адаптацию generated read signatures. Legacy persisted content распознаётся server IsLegacyContent и не преобразуется UI автоматически; MVP Offset/constraints/Archive/Retire не добавлены.

Executed verification: полный `ContentManager.UI.slnx` build без ошибок и console runner с существующими F01/F02/F03 плюс F04 scalar/snapshot/orphan/rail/transport/rendering/media-kind checks прошли после свежей configured generation с Legacy carriers. Rendered regressions подтверждают отсутствие literal error variable/retry при successful rail, friendly SchemaName в workspace/list, readable diagnostics с исходным exact focus Path, blank intrinsic input/disabled Save, сохранение raw null до typed repair, безопасный read-only legacy JSON, disabled/guarded legacy Copy и отдельный logical ETag refresh/retry после Publish без exact-version GET. `git diff --check` прошёл.

Реальный browser → UI → backend проверен 08.10.2026 для global list/exact workspace и legacy historical read с именами/raw values. Modern clean Draft: blank Integer блокировал Save; explicit 0 был сохранён, опубликован как server version #2, после Publish logical Rename прошёл с обновлённым owner ETag. SC03: explicit Key и exact Int64 maximum `9223372036854775807` создали saved initial Draft; blank intrinsic input блокировал Create. Последний legacy Copy guard дополнительно проверен renderer/harness после этого browser прогона. Это не доказательство полного all15/storage/concurrency/browser acceptance; MinIO bytes, cross-target races и полный direct sync проверяются в своих слоях. Исторические legacy browser scripts с запретом второго Draft не являются F04 acceptance. Проверочные UI/API процессы и временная browser tab закрыты после прогона.
