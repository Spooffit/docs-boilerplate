# F08. Локальный контент ProductVariant: фронтенд

[Бизнес](../../../business/features/F08-variant-content-assignment.md) · [Бэкенд](../../backend/features/F08-variant-content-assignment.md) · [M04](../modules/M04-publishing.md) · [Q07](../../../business/questions/Q07-exclusions-and-local-content.md) · [Q13](../questions/Q13-ui-scope.md)

Документ описывает целевой MVP UI Content Manager: .NET 10, Blazor Standalone, MudBlazor, FSD и NSwag. Имена будущих блоков — логические обозначения, а не существующие классы. Факты текущего UI отделены от цели в §11; вёрстка не задаётся.

## 1. Результат и границы

Специалист в Variant Workspace редактирует и явно сохраняет только локальный patch ProductVariant: additions, explicit overrides, schema exclusions и instance exclusions. Общий контент Product виден как база для наследования, но не копируется в editable набор Variant. Save меняет mutable assignment, не создаёт release/revision/activation и не меняет Delivery или исторические snapshots. Фиксация patch в VariantRelease относится к [F09](F09-content-releases.md), композиция immutable releases — к [F10](F10-publication-revisions.md).

Override `A → B` отдельно хранит намерение заменить inherited EntityInstance A и точную Published EntityInstanceVersion результата B. A и B принадлежат одной логической EntitySchema; другая SchemaVersion допустима в пределах этой схемы. В результате B имеет собственные EntityInstanceId/EntityInstanceVersionId, а не identity A. Полевого merge и ContentSlot нет. Cross-schema изменение выражается exclusion + addition. Name/Key служат подписями, matching использует stable IDs.

Exclusions действуют только на inherited layer; local content применяется после них. Schema exclusion охватывает и будущие inherited позиции схемы. Local addition/override той же схемы или instance и exclusion могут сосуществовать; UI показывает избыточность как неблокирующее сообщение и не удаляет/нормализует правила. Удаление override возвращает inherited A только при отсутствии действующего exclusion. Все targets рассматриваются относительно исходного inherited layer и подавляют только его строки; results и additions включаются один раз. Result одного override не становится target каскадной замены другим правилом, порядок правил не задаёт приоритет. Цикл связей не является отдельным запретом, если final logical identities уникальны и остальные guards выполнены.

Если A исчезает из live Product assignment, relation A→B сохраняется, B остаётся local content. Возврат A снова применяет тот же override. Новая Published версия B не подставляется автоматически. Product base change меняет preview, но не patch и не его собственный ETag.

Повторный target, повторный result, result одновременно в addition/override, cross-schema relation и неподтверждённая identity — intrinsic SaveBlockers. Base-dependent collision, например independent inherited B плюс result A→B, не запрещает Save intrinsically valid patch: UI показывает Warning и ReleaseBlocker для этого diagnostic basis; текущий итог нельзя выдать за valid composition. Release/revision требуют полной проверки своей выбранной базы. Live ReleaseBlocker/CanCreateRelease относятся только к live basis: collision сама по себе не блокирует открытие F09 и выбор другого immutable ProductRelease либо EmptyBase при выполненных остальных guards. Create confirm определяется review выбранного context. Автоматические deduplication и last-write-wins запрещены. Принятое правило связано с [Q07](../../../business/questions/Q07-exclusions-and-local-content.md#f08-сохранение-patch-и-проверка-live-базы).

F08 относится к полному Publishing MVP; Demo не зависит от Variant patch, VR School, broker или полного Publishing. Каталожные исходные данные доступны только для чтения по [F06](F06-product-catalog.md). Для нового использования недоступны Draft/Retired/Withdrawn и unavailable logical owner/parent. Уже сохранённая exact reference, позднее ставшая Retired/Withdrawn/Archived/unavailable, остаётся видимой и может сохраниться unchanged с diagnostics и блокировкой нового release. Новая позиция/замена reference — new use и требует eligibility; display reorder новым использованием не считается. Archived Variant либо parent разрешают лишь limited maintenance: удалить references/правила или заменить проблемную reference на eligible, без additions/расширения контента. Maintenance определяется эффектом для контента: новый inherited-only exclusion, удаляющий проблемную inherited позицию, либо override, заменяющий её на eligible result, допустимы. Open CatalogConflict блокирует ordinary F08 Save и не обходится maintenance. Для конкретного blocker F15 предоставляет отдельный corrective Variant patch workflow: current applied/prospective newest-parent/proposed repaired preview, own patch If-Match и captured source/basis. Repair сохраняет patch без move/closure; ordinary workspace остаётся applied до отдельного Resolve. F08 не отправляет blocked Save как repair и не повторяет old-parent mutation. Silent cleanup/latest запрещены.

## 2. Страницы, экраны и навигация

| Страница / экран | Точка входа и контекст | Состав и режим | Завершение / переход |
| --- | --- | --- | --- |
| SC01. Variant Workspace, «Локальный контент» | `/products/{productId}/variants/{variantId}/content`; F06, вкладка, deep link или возврат из F15 | Applied Variant/parent summary, persistent blocker, read-only live composition, локальный patch, Save | Save остаётся в content; overview/publishing/history/policy принадлежат общему workspace |
| SC02. Выбор Published версии | Add, «Переопределить» inherited строки либо «Заменить версию» local result в SC01 | SelectPublishedInstanceVersion F07; logical instance → exact Published version; target A виден для override | Confirm возвращает явный exact choice SC01; Cancel ничего не меняет |

Route принят Q13, это цель, а не текущий `@page`. Прямой вход/refresh использует exact VariantId и applied parent по F06; предварительный список не требуется. Breadcrumb показывает applied Name и Product, IDs являются route identity. Неизвестный Variant не подменяется первым доступным. Отсутствие assignment не означает подтверждённый пустой patch и не вызывает UI-create.

Canonical URL следует **применённому** CatalogAccess parent. При pending move/F15 conflict сохраняется applied Product; пришедший source parent не становится базой и route. После подтверждённого safe move старый deep link разрешается по immutable VariantId и переходит на новый canonical URL. Dirty patch принадлежит тому же VariantId и сохраняется при коррекции ProductId в URL; новая база требует актуальной серверной проверки. Запрос к старому parent автоматически не повторяется: после read нового applied context пользователь явно решает следующий Save. HTTP-код parent mismatch — [Q15](../../backend/questions/Q15-api-and-source-code.md#f08-patch-preview-и-parent-route).

Смена VariantId/вкладки и уход проходят dirty guard Save / Discard / Stay: переход после Save ждёт подтверждённого успеха; отказ/unknown outcome оставляет форму. Discard восстанавливает baseline явно. Для browser refresh/закрытия используется доступное браузеру предупреждение; восстановление после reload, autosave/offline/browser storage не обещаются. Возврат из F15 не стирает dirty. SC02 хранит temporary query/page, не меняет URL родителя; server search/debounce/pagination переиспользуются из F07/F04, точные фильтры/limits — Q15.

## 3. Карта FSD-блоков

### 3.1. Необходимые уровни

Page координирует Variant identity, applied context и navigation. Один widget объединяет baseline, generated editable request, правила, composition read и Save. Самостоятельные features — переиспользуемый exact picker и whole-patch Save. Add/remove/exclude остаются локальными действиями widget, отдельный feature на каждую кнопку не требуется. Entities показывают server references/catalog context; shared предоставляет controls, guard и transport. `app` подключает принятую content route/вкладку. Global patch store и дополнительные слои не нужны.

### 3.2. Состав

| Блок | Слой / логическое размещение | Ответственность | Использование | Основное описание |
| --- | --- | --- | --- | --- |
| VariantWorkspace: LocalContentSection | pages / variant-workspace / ui, model | VariantId, applied ProductId, canonical route/guard | Изменить SC01 | §3.3; каталог [F06](F06-product-catalog.md#3-карта-fsd-блоков) |
| VariantContentAssignment | widgets / variant-content-assignment / ui, model | Whole-patch state, правила, preview и координация действий | Создать целевую секцию SC01 | §3.3 |
| SelectPublishedInstanceVersion | features / select-published-instance-version / ui, model | Explicit exact выбор Add/Override/Replace | Переиспользовать SC02; публичная same-schema настройка | [F07](F07-product-content-assignment.md#3-карта-fsd-блоков); отличия §3.3 |
| SaveVariantContentAssignment | features / save-variant-content-assignment / model, api | Whole-patch PUT с assignment If-Match | Создать для SC01/guard Save | §3.3 |
| ContentReferenceView / instance reads | entities / entity-instance / ui, api | Exact reference, schema/availability и публичные F04 reads | Переиспользовать в SC01/SC02 | [F07](F07-product-content-assignment.md#3-карта-fsd-блоков), [F04](F04-entity-instances.md#8-использование-api-и-nswag) |
| CatalogRecordView и предупреждение F15 | entities / product-variant, product; widgets / каталоговый конфликт | Applied summary и persistent blocker | Переиспользовать в SC01 | [F06](F06-product-catalog.md#3-карта-fsd-блоков), [F15](F15-catalog-conflicts.md#карта-блоков-и-состояния) |
| Controls, guard, API/errors | shared / ui, api | Общие controls, metadata, dirty/error presentation | Переиспользовать; header infrastructure §8 | [S05](../../../catalog/docs/standards/ui/forms-and-accessibility.md#form-state-and-user-feedback), [S06](../standards/S06-api-client.md), [S07](../../../catalog/docs/standards/ui/forms-and-accessibility.md#access-and-semantics) |

Логическое размещение не означает уже существующих namespace/путей.

### 3.3. Ответственность и публичные интерфейсы

| Блок | Назначение / входы | Выходы и получатель | Собственное состояние | Данные, публичные зависимости и состав | Жизненный цикл |
| --- | --- | --- | --- | --- | --- |
| LocalContentSection | VariantId/ProductId route; applied catalog context и ограничения | Applied IDs → widget; navigation intent → shell | Exact Variant/parent/read status и route | Public F06 reads/views; widget; F15 banner; shared guard | Exact read при входе; guard до смены VariantId; safe move меняет parent context, сохраняет dirty того же Variant; late read старого context не применяется |
| VariantContentAssignment | Applied IDs/ограничения; координация patch и отображения | Generated request + ETag → Save; mode/target → picker; dirty/pending → page | Saved generated DTO, один editable generated request, own ETag; dirty/selection и freshness UI-state, SaveBlockers/ReleaseBlockers/Warnings с diagnostic basis | DF01/DF06; server preview; ContentReferenceView; picker/Save callbacks; shared controls | Вход читает baseline; local action меняет request; confirmed PUT заменяет baseline/request/ETag; Discard из baseline; base read не затирает dirty |
| SelectPublishedInstanceVersion: настройка F08 | Add без target; Override с explicit текущим inherited A/SchemaId и same-schema server filter; Replace с fixed logical owner B | Confirm: generated exact instance/version choice → widget; Cancel без результата | Temporary query/page/candidate; owner state как в F07 | Public F04 reads, ContentReferenceView; не читает internals patch/Save | Открытие получает target context; смена owner сбрасывает version; closing очищает temporary state; same-schema не снимается незаметно |
| SaveVariantContentAssignment | Captured applied ProductId/VariantId, generated whole-patch request, own ETag и разрешения | Confirmed DTO/new ETag либо classified error → widget | Pending/captured request/context/error; без второй mutable бизнес-формы | DF02/generated client/shared headers; не зависит от picker/page class | Один запрос; pending блокирует редактирование patch; late result проверяется по captured Variant/parent/context; mismatch не вызывает retry старого PUT |

Карточки read/view/catalog/shared не дублируются: F08 передаёт им generated target/result/schema context и server reasons. Имена DTO/request/diagnostic fields и пригодность generated classes для редактирования — Q15. Ручной VariantPatchViewModel/FormModel или копия DTO ради строк/правил не вводится.

## 4. Логическая компоновка

### Дерево схем и режим наследования

Основной список назначений показывает Schema → Instances с exact версией структуры, source Product/Variant и счётчиками полного server scope. Schema tree/policy controls переиспользуют [F07](F07-product-content-assignment.md#schema-first-редактор); Variant widget дополнительно владеет generated local patch/request и inherited preview context. У каждого узла схемы явно показано «Наследовать Product» либо «Собственный состав — заменяет наследуемый». Второй режим использует «Все», «Все, кроме», «Только выбранные» и explicit Published SchemaVersion.

```text
Variant / Назначения
  Schema · источник/версия структуры · inherited count / final count
    Наследовать Product → inherited items и точечные inherited exclusions/overrides
    Собственный состав → выбранная структура + policy F07
      Included / excluded / unavailable instances · exact versions
    Вернуть наследование → показ diff → изменение локального request
  Whole-patch Save / Discard
```

Если Product даёт 20, а own SelectedOnly содержит два, показывается «Наследовано 20 → заменено собственными 2», final preview содержит два. Это не badge «добавлено 2» при оставшихся двадцати. Для schema replacement inherited children можно просмотреть как подавленную базу, но они не выглядят выбранными final items. Исключения собственного AllExcept подписаны отдельно от «Не наследовать экземпляр». Старые additions/overrides той же схемы при переключении показываются в conversion diff; ничего не удаляется молча до подтверждённого Save.

SelectedOnly никогда не хранит complement всех неотмеченных экземпляров; появление новых не меняет его выбор. AllExcept учитывает новые только при новом release review. Точечные старые A→B и inheritance exclusions вне replacement сохраняют existing actions. bUnit проверяет оба источника, 20→2, возврат inheritance, scope разных exclusions, mode switch/cancel и отсутствие hidden duplication. Конкретный tree DTO/paging/enum — [Q39](../../../business/questions/Q39-schema-content-selection.md), generated client не редактируется вручную.

```text
SC01. VariantWorkspace: LocalContentSection [pages; VariantId/applied parent]
  Breadcrumb + CatalogRecordView(Variant, applied Product) [entities/shared; read-only]
  Навигация вкладок [общий workspace]
  Предупреждение F15 [widgets; persistent при blocker]
    Applied связь / pending source связь / affected actions / переход к F15
  VariantContentAssignment [widgets; baseline/request/ETag/dirty]
    Статус patch: Saved / есть несохранённые изменения / отправляется
    SaveBlockers / ReleaseBlockers / Warnings + diagnostic basis / CanCreateRelease [отдельно от Saved]
    Live composition [server read; read-only, группы EntitySchema]
      ContentReferenceView + provenance: Унаследовано / Локально / Override / Исключено
      Inherited item → Переопределить / Исключить instance
      Schema group → Исключить наследование схемы
      Preview freshness/basis / collision причины и involved IDs; failure не empty valid preview
    Локальные правила [generated editable request]
      Additions: exact result → Заменить версию / Удалить
      Overrides: target A → exact result B; собственная identity B
        Target отсутствует в текущей базе / Заменить результат / Удалить правило
      Schema/instance exclusions → Удалить правило
      Неблокирующая избыточность / сохранённая unavailable reference
    Добавить local item → SC02
    SaveVariantContentAssignment [features]
    Loading / empty / validation / stale / unknown outcome [shared presentation]
  Guard Save / Discard / Stay [shared; dirty navigation]

SC02. SelectPublishedInstanceVersion [features; temporary выбор]
  Контекст: Add / target A и Schema для Override / fixed B для Replace
  Logical instance search / server page [public F04 reads; same-schema при Override]
  Exact Published versions выбранного owner [public F04 reads]
    ContentReferenceView + SchemaVersion/availability
  Explicit version selection / Confirm / Cancel [shared]
  Loading / no matches / read failure [shared presentation]
```

| Связь | Передаваемые сведения | Координатор | Наблюдаемый результат |
| --- | --- | --- | --- |
| F06 read → summary/widget/F15 | Applied IDs/Name/ограничения/pending | Page | Общий applied context; source и applied не смешаны |
| Inherited A/Add/result B → picker | Mode, target A + logical Schema либо fixed B | Widget | Temporary выбор; patch прежний до Confirm |
| Confirm → patch | Generated exact choice и explicit target при Override | Widget | Generated request изменён, dirty; A→B виден явно |
| Exclusion/remove → patch | Stable schema/instance/rule identity | Widget | Local action; другие правила сохраняются |
| DF01/DF06 → preview; DF02 → baseline | Server DTO/diagnostics/context; own ETag DF01/DF02 | Widget | Saved/candidate preview различаются; success заменяет baseline без result GET |
| New base/applied move → widget | Подтверждённый context/preview | Page/widget | Новый context/freshness; dirty patch не переписывается |

SC01 одновременно показывает контекст, preview и локальные намерения; SC02 открыт отдельно. Правило видно даже при отсутствии target в preview. Одна строка не сводится к одному exclusive badge: exclusion и override могут сосуществовать. UI-вложенность не разрешает dependencies к внутренним типам соседних срезов.

## 5. Зависимости и публичные границы

Page использует public F06 reads/views и передаёт applied IDs/ограничения widget. Widget передаёт picker mode, target и same-schema restriction через публичные параметры, принимает Confirm/Cancel callbacks; Save получает generated request/ETag и возвращает result/error. Picker не вызывает Save, Save не управляет picker; callbacks не требуют ссылок на класс page. Canonical route и dirty navigation координирует page.

SelectPublishedInstanceVersion описан в F07; F08 расширяет только публичную настройку контекста/фильтра и fixed owner при замене версии. Изменение проверяется и для Product Add/Replace. Logical/exact reads переиспользуются из F04, новый candidate-list endpoint не вводится. Shared отвечает за headers/errors/controls и не знает same-schema, Published eligibility и Publishing. F15 resolution form не копируется в F08.

Server composer является authority для live результата и blockers. Browser не выполняет authoritative merge и не подменяет его сортировкой/dedup. Client entities не являются Domain/EF-моделями; browser UI не зависит от серверных сборок, чужих DbContext или VR School API. Public header/guard изменения затрагивают другие workspaces, а не только F08.

## 6. Данные и владение состоянием

### 6.1. Источник истины и область жизни

| Данные / состояние | Источник истины | Клиентский владелец | Обновление / сброс |
| --- | --- | --- | --- |
| Variant/applied Product/eligibility/F15 | F06/F15 server read | Page, generated model | Вход/refresh/подтверждённый move/возврат F15 |
| Saved patch + server preview | DF01 либо confirmed DF02 DTO | Widget, generated DTO напрямую | Read/confirmed PUT; failure не стирает безопасный прежний результат |
| Own assignment ETag | Exact DF01/DF02 header | Transport/UI-state рядом с DTO | Header того же Variant assignment; не base/catalog/release token |
| Editable additions/overrides/exclusions | Generated whole-patch write request | Widget, одна форма на VariantId | Local action/Discard/confirmed result; база не меняет её |
| Candidate preview/diagnostics/basis | Server DF06, logical SaveBlockers/ReleaseBlockers/Warnings/CanCreateRelease; carrier Q15 | Widget, generated result + freshness presentation-state | Local edit/context/source change меняет basis; late result старого candidate не применяется; own ETag не freshness token |
| Candidate/query/page | Public F04 reads | Picker, generated models и UI-state | Search/owner/context change; закрытие |
| Captured sent request/context | Один запускаемый DF02 | Save до подтверждённого/unknown исхода | Не заменяется новым context; сохраняется для явного восстановления |
| Dirty/pending/issues/selection | Локальное UI-state | Widget/Save/page по §3.3 | Действие/исход/guard/выход |

Различаются baseline, пользовательская форма, отправленный request и confirmed DTO. Read/write generated types выполняют разные роли; mapping между ними не создаёт параллельную бизнес-модель. ETag не добавляется в DTO и не вычисляется из Version/ExpectedRevision. Pending Save блокирует изменения этого patch и повторную отправку; late reads/query/results не применяются к иному context. Global store/browser persistence не назначаются.

### 6.2. Состав представления и формы

| Сведение | Источник | Использование | Правило |
| --- | --- | --- | --- |
| Applied Variant/Product Name/IDs | F06 | Summary/breadcrumb/canonical context | Read-only; pending source parent отдельно |
| Inherited position/EntitySchema | Server composition/base metadata | Target/exclusion, группа | Stable EntityInstanceId/SchemaId authority; не строковый key |
| Addition result exact version | Generated request/read | Local item и выбор версии | Explicit выбор; exact ID не latest |
| Override target A → result B | Generated rule + authoritative metadata | Явная relation/диагностика | Одна logical Schema; B own identity; target и result разные роли |
| Schema/instance exclusions | Generated request/read | Независимые правила | Inherited only; не удаляют local item/override |
| SchemaVersion/reference lifecycle | Authoritative server metadata | Exact choice/unavailable reason | Не требовать latest SchemaVersion; сохранённые IDs не скрывать |
| Preview provenance/collision | Server read | Причины/затронутые позиции/переходы | No silent LWW/dedup; база не переписывает patch |

Missing metadata/unavailable не удаляет rule/reference из GET/формы: отображаются exact IDs и известная причина, доступны details/copy. Target, отсутствующий только в live Product base, отличается от target, который ContentModeling не может разрешить: в первом случае intent валиден и сервер сохраняет ранее captured logical Schema; сохранённое authoritative captured context удерживается, а неподтверждённая identity нового правила блокирует Save. Browser не восстанавливает target Schema по имени или по B. Новый override выбирает target только из текущего inherited состава; ранее сохранённый отсутствующий target не удаляется и не превращается в новый выбор. Unchanged unavailable exact refs разрешены для Save с ReleaseBlockers; replacement/new use требуют eligibility. Limited maintenance оценивается по удалению/eligible замене проблемного контента и не обходит F15. Поля/diagnostics/basis — Q15.

## 7. Действия и клиентские потоки

| Действие | Запуск | Исполнитель | Шаги / запрос | Результат |
| --- | --- | --- | --- | --- |
| UI01. Открыть/перечитать | Route/direct entry/refresh | Page + widget | Exact Variant/applied parent → DF01; guard до замены dirty baseline | DTO/own ETag/saved preview либо distinct read error |
| UI02. Add | SC01 при разрешённой mutation | Widget → picker | Add context → DF04/DF05 → explicit Published Confirm | Addition локально, dirty; без write |
| UI03. Новый Override | Текущий inherited A; normal use либо limited replacement maintenance | Widget → picker | Stable target A/logical Schema текущего inherited layer → same-schema eligible candidate → exact Confirm | Явный A→B; no arbitrary absent target/cross-schema; B own identity |
| UI04. Заменить result version | Local addition/override B | Widget → picker | Fixed EntityInstanceId B → другая допустимая exact version | Relation A/правила прежние; смена result owner — явный выбор в override context |
| UI05. Exclude schema/instance | Группа/позиция inherited | Widget | Добавить stable rule в generated request | Dirty; local сохранён; schema rule охватывает future inherited |
| UI06. Удалить item/rule | Local rule SC01 | Widget | Явно удалить только выбранное правило | Dirty; override removal не удаляет exclusion; inherited return подтверждает server preview |
| UI07. Save | Dirty, own token/context/известные ограничения | Save | Local intrinsic checks → capture весь patch → один DF02 → result/error | DTO/new ETag из PUT; dirty reset при confirmed success; «Локальные изменения сохранены, но ещё не опубликованы» |
| UI08. Проверить live composition | Вход/явное обновление/изменившаяся база/local edit | Widget | DF01 для saved patch; DF06 для candidate patch, transport Q15 | Saved/candidate различаются; dirty форма не затирается |
| UI09. Перейти/отменить | Tab/route/VariantId/F15 | Page | Save/Discard/Stay guard; pending/unknown учитываются | Confirmed переход/explicit discard; no silent loss |
| UI10. Исправить конфликт/parent | Banner/server refusal/safe move | Page | F15 либо exact applied-context read; сохранить VariantId/dirty | Canonical URL после applied move; no automatic old-parent PUT |

После local edit сохранённый preview не выдаётся за результат несохранённой формы. Требуется server-backed проверка candidate patch (DF06); method/URL/transport не выбраны. До согласования контракта UI не обещает готовый unsaved preview: показывает local intent и последнее server composition с пометкой об отличии. Candidate preview не сохраняет patch и не заменяет серверную проверку Save/release. Saved статус, intrinsic SaveBlockers, diagnostic ReleaseBlockers/Warnings и CanCreateRelease показываются отдельно: base-only collision оставляет Save доступным при остальных выполненных guards. Diagnostic basis относится к patch + base + необходимым source inputs; own patch ETag подтверждает concurrency ресурса, а не пригодность всего composition. Failure diagnostics/preview не равен пустому valid result. Browser не вычисляет authoritative composer.

При 412 сохраняются local/sent patch и старый baseline; explicit read получает новое состояние для сравнения. Новое ETag не прикрепляется к старой форме без явного принятия нового baseline/решения пользователя. Auto-merge/blind retry нет. При timeout после send исход неизвестен: сохраняются sent/current форма, выполняется explicit exact check; совпадение patch не доказывает авторство сохранения. Выяснение результата и следующий явный Save — разные действия.

## 8. Использование API и NSwag

Whole-patch Save использует выбранный MVP writer protocol [Q20](../../backend/questions/Q20-persistence-and-migration.md): fresh applied parent/base, reference lifecycle и F15 facts защищены сервером до owner commit. Read/DF06 preview не выдаёт commit permit, own patch ETag не заменяет проверку independently changed basis. Серверный own412 отличается от подтверждённого foreign/domain409; ожидание gate, cancellation и unavailable read не становятся коллизией, empty valid preview либо CatalogConflict. UI сохраняет patch/context и выясняет unknown write outcome по Q15, без повторного Save с новым token.

| Операция | Контракт / backend | Использование | Подтверждение / ограничения |
| --- | --- | --- | --- |
| DF01. Exact assignment read | `GET /api/v1/products/{productId}/variants/{variantId}/content-assignment` | Saved patch/server live preview/diagnostics | DTO + own ETag; GET не создаёт assignment |
| DF02. Whole-patch Save | `PUT /api/v1/products/{productId}/variants/{variantId}/content-assignment` | Весь additions/overrides/schema/instance exclusions request | Required If-Match; success DTO + new ETag; no hidden result GET |
| DF03. Internal setup | Backend F08; механизм **уточняется** | Assignment существующего Variant | Не UI-команда, не POST при missing GET |
| DF04. Instance candidates | Public F04 instance list | Logical selection с same-schema context/server pagination | Query/eligibility/limits Q15; без нового candidate-list endpoint |
| DF05. Exact versions | Public F04 version list; exact read при необходимости | Published versions fixed/selected owner; optional exact display | No latest; metadata/availability от сервера |
| DF06. Candidate patch/live preview | Required backend need; HTTP method/URL/transport **уточняются** Q15 | Проверить несохранённый generated patch с текущей базой/applied context | Не save/release, не browser merge; freshness Q20; POST не выбран |

Маршруты DF01/DF02 приняты прежним backend F08; names/DTO shape/envelope/headers/error codes уточняет [Q15](../../backend/questions/Q15-api-and-source-code.md#f08-patch-preview-и-parent-route). Route ProductId сервер проверяет по applied CatalogAccess parent, mismatch code UI не назначает. Изменение живой Product базы не меняет own patch token: own ETag не доказывает свежесть multi-input preview. Согласованность patch/base/catalog/lifecycle при чтении и result freshness DF02/DF06 — [Q20](../../backend/questions/Q20-persistence-and-migration.md#f08-patch-concurrency-и-preview-consistency). Область strong numeric ETag при DTO с меняющимся derived preview и отдельные freshness metadata требуют решения Q15/Q20: UI не считает неизменный ETag cache/304-доказательством неизменности всего preview. Base freshness не превращается без решения в обязательный второй base If-Match.

Generated NSwag DTO/request используются напрямую. Неподходящие shape/init-accessors/diagnostics решаются backend OpenAPI и regeneration, без ручной копии/редактирования generated code. Текущие SaveAssignmentsRequest/ExpectedRevision и release PreviewAsync не являются целевым F08 контрактом. Новые preview/candidate URLs не назначаются.

Shared API infrastructure предоставляет per-call If-Match и response ETag владельцу exact resource; способ настройки — Q15. Token не помещается в shared mutable default headers, где может попасть в чужой запрос. Confirmed PUT обновляет UI из returned DTO/new ETag без дополнительного result GET. Если success DTO получен без нового required ETag, запись подтверждена, дальнейшая mutation блокируется до явного exact read восстановления token; PUT не повторяется. GET без ETag не даёт основание для Save. Отдельный refresh preview не подменяет result PUT и не доказывает атомарную свежесть разных inputs.

## 9. Состояния, ошибки и доступ

| Ситуация | Что видит пользователь | Действия | Данные / восстановление |
| --- | --- | --- | --- |
| Technical gate/read failure | Координация/проверка недоступна, не domain409/новый CatalogConflict | Сохранить ввод/context; safe read | Не empty valid result; lost write acknowledgement остаётся unknown по Q15, без blind retry/new token |
| Loading/refresh | Ожидание affected области | Допустимые read/navigation | No mutation до baseline/context; safe old read сохраняется при refresh failure |
| Empty patch/base | Подтверждённые пустые local правила/база отдельно | Add при доступности | Не missing/initialization pending/error; shape Q15 |
| Dirty/preview | Несохранённые изменения; saved/candidate result context | Edit/Save/Discard/Stay | Old preview не выдаётся за текущую форму; DF06 contract open |
| Target A отсутствует в live base | «Target отсутствует в текущей базе», A→B и exact B | Разрешённое edit/remove | Relation/B/captured Schema сохраняются; return A снова применяет override |
| Target не разрешён ContentModeling | Exact target ID/причина отдельно от отсутствия в base | Retained captured context/read/explicit repair | Intent не удаляется автоматически; новый target authoritative/current inherited |
| Exclusion + local/override | Оба правила/неблокирующая избыточность | Сохранить либо явно удалить | No normalization/implicit removal |
| Intrinsic invalid patch | Target/result/schema причины inline | Явно исправить | No LWW/dedup; заведомо invalid patch не отправлять как допустимый |
| Base-dependent collision | Причины/все involved refs, Warning + ReleaseBlocker текущего basis | Save intrinsically valid patch доступен; исправить composition перед release/revision | Patch/base не переписываются; другой release basis проверяется полностью |
| Existing unavailable reference | Exact ID/version/lifecycle/availability reason, ReleaseBlocker | Unchanged Save/read/history; explicit Remove/eligible Replace | Exact IDs сохраняются; new position/replacement проверяется как new use |
| Archived Variant/parent | Limited maintenance и причина | Удаление проблемного контента, в том числе inherited-only exclusion; eligible replacement, в том числе override текущего inherited target | Без content addition/expansion; CatalogConflict блокирует ordinary writes |
| F15 pending move/blocker | Persistent причина, applied/source связь, affected actions | Reads/history/resolution | Affected writes blocked; pending parent не applied; dirty сохранён |
| Parent mismatch после move | Route context изменился | Exact applied read → canonical URL → явное решение | Код Q15; no auto old-parent PUT retry |
| Domain/validation refusal | Form/item reason, distinct от concurrency | Исправить/перечитать по причине | Form/sent context сохранён; addressing Q15 |
| 412 | «Локальные правила изменились на сервере» | Explicit read/compare/baseline acceptance | Dirty/sent сохранены; no auto-merge/retry |
| 428 | Ошибка обязательного precondition | Восстановить transport/token | Пользователь не вводит Version/ETag |
| 403 / 404 | Distinct denied/missing | Разрешённая навигация/read | Не empty patch/fallback к иному Variant |
| Timeout после send | «Результат сохранения не подтверждён» | Explicit exact check | No blind retry/rollback; sent/current preserved |
| Success без ETag | Save подтверждён; token отсутствует | Explicit exact read | Следующий Save blocked, запись не повторяется |

Authorization/lifecycle/F15 проверяет backend. Disabled действия видны с причиной; source restriction, preview collision и 412 не получают один общий текст. F15 не снимается закрытием banner. Limited maintenance не снимает CatalogConflict и не открывает additions/расширение контента. SaveBlockers блокируют Save; ReleaseBlockers — create для соответствующего basis; Warnings поясняют неблокирующие обстоятельства. Неизвестный/failed diagnostic result не выдаётся за CanCreateRelease=true.

По [S05](../../../catalog/docs/standards/ui/forms-and-accessibility.md#form-state-and-user-feedback)/[S07](../../../catalog/docs/standards/ui/forms-and-accessibility.md#access-and-semantics) provenance, A→B, exclusion scope, exact version, dirty/error/disabled reasons различимы без цвета. Основные действия доступны с клавиатуры; picker получает focus и возвращает запуску; ошибка позволяет найти rule/item. CSS/Razor/MudBlazor attributes не задаются.

## 10. Проверки и приёмка

Целевые критерии [S08](../standards/S08-testing.md), не результаты UI tests; библиотека не выбирается. API doubles проверяют клиентскую координацию; persistence/composer/lifecycle/concurrency и snapshots требуют реального backend.

| Сценарий | Условия / воздействие | Наблюдаемый результат | Граница |
| --- | --- | --- | --- |
| Protected commit / technical failure | Foreign lifecycle/catalog/source changes между preview и commit; bounded gate wait/cancellation/read failure; lost ACK | Server revalidates protected facts; own412/domain409/technical failure различимы; ввод/basis/token сохранены, no false success/empty valid/blind retry | UI/transport doubles для representation; real PostgreSQL owner writers/barriers для guard, recovery contract Q15 отдельно |
| Direct/context | Deep link без списка; late old Variant read | Exact/applied context; no fallback/late overwrite | Page/double + real ownership |
| Add/Replace/Cancel | Published и unavailable candidates | Explicit exact choice/fixed owner Replace/Cancel unchanged/no latest | Picker/double + real guards |
| Same-schema override | A→B, разные SchemaVersion; cross-schema candidate | Relation/B own identity; filter; server rejection cross-schema | UI + real resolver/composer |
| Missing/return target | Сохранить A→B; удалить/вернуть A в live base | B/intent/exact version сохранены; A снова suppressed | UI read + real persistence |
| Target authority/new selection | Сохранённый target отсутствует; попытка выбрать отсутствующий target для нового override | Retained intent/ID/captured schema видимы; новый выбор только current inherited и authoritative | UI + real resolver |
| Exclusions | Schema/instance exclusion + same-schema local/override; новая base позиция | Inherited only/local остаётся; future inherited excluded; coexistence без normalization | Widget + real composer |
| Remove intent/rule | Удалить override с/без exclusion | Только explicit rule; inherited return лишь без exclusion | Local request + real preview |
| Intrinsic invalid patch | Duplicate target/result; addition+override B; cross-schema/untrusted identity | Адресуемые SaveBlockers; Save blocked; no LWW/dedup | Widget + real mutation |
| Base collision/change | Independent inherited B + A→B; F07 меняет base | Intrinsically valid Save allowed, Warning/ReleaseBlocker; patch/own ETag не меняются от base change | UI + real composer/Save |
| Override chains/cycles/order | A→B/B→C либо A→B/B→A с unique final IDs; переставить rules | Original inherited suppression; каждый local result/addition один раз; no cascade/order priority; цикл сам по себе не ban | UI result + real composer |
| Retained/new-use references | Saved exact result стал unavailable; unchanged Save/display reorder; затем replacement/new position | Unchanged allowed + ReleaseBlocker; new use requires eligibility; no cleanup/latest | UI + real resolver |
| Limited maintenance/F15 | Archived Variant/parent; inherited exclusion/eligible replacement override; Add; Open conflict | Maintenance без expansion; normal Save blocked; отдельный F15 repair показывает три preview/own token/basis, сохраняет patch без move; Resolve отдельно | UI + real guards/DF07 |
| Diagnostic failure | Own ETag/DTO получены, diagnostics failed/stale | Saved/dirty отделены от diagnostics; addressable groups/basis; no empty valid preview | Widget/transport |
| Whole-patch success | Несколько типов правил/очистить patch | Один PUT/generated full request/own If-Match/DTO+newETag/no hidden GET | Transport + real HTTP/persistence |
| Token/412/428 | Base/catalog tokens отличаются; два клиента/missing token | Own token/edits preserved/distinct errors/no blind retry | Shared transport + real concurrency |
| Dirty preview | Local edit + saved base refresh; late DF06 result | Form preserved; results context/staleness visible; no browser authority | Widget/doubles; server contract Q15/Q20 |
| F15/move | Pending source parent → applied move; dirty/late old-parent response | Applied kept/affected writes blocked; canonical after applied; same Variant dirty survives; no old-parent retry | Page + real CatalogAccess/Publishing |
| Guard/lifetime | Tab/Variant change, Save failure/unknown, double click/query race | Save/Discard/Stay; confirmed transition/right context/temporary reset/focus | Components/browser |
| Recovery | Timeout; DTO без ETag; failed read после confirmed Save | Sent/current preserved; explicit read/compare; no repeated PUT/false failure | Transport/page; real recovery отдельно |
| Publishing boundary | Save при existing releases/revisions/Delivery | Working patch only; history/Delivery прежние | Real API/E2E |

Real MVP E2E: открыть Variant → Add/same-schema override/exclusions → Save → reopen exact → проверить patch/server preview → отдельно F09/F10. Принятые Q07 Save/retention/maintenance/composition правила проверяются; carrier и consistency детали Q15/Q20 не выбираются молча в expected result. Markdown/link checks не являются тестами приложения.

## 11. Последовательность изменений и открытые вопросы

Сначала закрываются backend F08/Q15/Q20 shape, headers, initialization, DF06 preview consistency с учётом принятых Q07 Save/retention policies; затем OpenAPI/NSwag/shared transport. Переиспользуются F06 applied context, F07 picker/F04 reads и F15 warning. После этого собираются generated whole-patch state/правила, Save/guard/recovery/server preview. Проверяются client orchestration, real composer/concurrency/lifecycle и границы immutable Publishing. Аналитика не изменяет source/generated/стандарты.

| Вопрос | Блокируемые детали |
| --- | --- |
| [Q15: F08](../../backend/questions/Q15-api-and-source-code.md#f08-patch-preview-и-parent-route) | DTO/request/generated names/editability/diagnostics/availability, headers/error addressing, parent mismatch code, F04 filters/limits, DF06 method/URL/transport, strong ETag/derived preview representation, carrier SaveBlockers/ReleaseBlockers/Warnings/CanCreateRelease и basis; GET/PUT route и PUT DTO+ETag приняты |
| [Q20: F08](../../backend/questions/Q20-persistence-and-migration.md#f08-patch-concurrency-и-preview-consistency) | Выбранный общий writer protocol; internal setup/missing-vs-pending и multi-input read/result representation уточняются; own patch ETag не base token |
| [Q07: F08](../../../business/questions/Q07-exclusions-and-local-content.md#f08-сохранение-patch-и-проверка-live-базы) | Save при base-only collision, retention/new-use, limited maintenance, current inherited new target и no-cascade приняты; основание правила |
| [Q13](../questions/Q13-ui-scope.md) | Детали picker/presentation/UI tests; route identity/generated direct/explicit Save приняты |

Подтверждённые исходники UI на 04.10.2026:

| Источник | Current implementation | Отличие target |
| --- | --- | --- |
| [PublicationWorkspacePage.razor](../../../../contentmanager.ui/src/ContentManager.Pages/Components/Publishing/PublicationWorkspacePage.razor), routes/Load/BaseItems | `/publishing/products/{OwnerId}` и `/publishing/variants/{OwnerId}`; assignment/releases/policy/history в одном workspace; Variants2Async → Products2Async; BaseItems из active ProductRelease | Accepted parent route/applied F06 context; live Product assignment preview; release preview отдельный F10 |
| PublicationWorkspacePage AddAssignment/AddExclusion/SaveChanges | Ручной AssignmentEditRow/SortOrder; duplicate instance отказ; exclusion SchemaId/nullable EntityInstanceId; ExpectedRevision; success затем `await Load()` | Generated direct patch; explicit override target; independent exclusions; own If-Match/DTO+ETag/no hidden result GET |
| PublicationWorkspacePage Preview/Prepare | Preview disabled при Dirty; PrepareRevisionRequest выбирает Product/VariantReleaseIds; PreviewAsync сравнивает release revision с active | Current release preview не является live/unsaved patch preview контрактом |
| [PublishedVersionPicker.razor](../../../../contentmanager.ui/src/ContentManager.Widgets/Components/Publishing/PublishedVersionPicker.razor), Load | EntityInstancesGET2Async Published filter/page20; версии flattened из aggregate DTO; Selected callback | F07 exact chooser с target/same-schema/fixed owner; page20 не target решение |
| [PublicationEditorModels.cs](../../../../contentmanager.ui/src/ContentManager.Widgets/Components/Publishing/PublicationEditorModels.cs) | Manual PublishedInstanceChoice/AssignmentEditRow; PublicationDiffRow сравнивает `(SchemaId, EntityInstanceId)` и SortOrder | Generated primary models; server composer authority; UI-state отдельно |
| [Generated client](../../../../contentmanager.ui/src/ContentManager.ApiClient/OpenApi/Generated/ContentManagerApiClients.g.cs), IPublishingClient/Assignments2Async/SaveAssignmentsRequest | PUT `api/v1/publishing/variants/{id}/assignments`; ExpectedRevision/Items/Exclusions; envelope DTO; caller signature без per-call If-Match/ETag результата | Target backend/OpenAPI/regeneration/shared headers ещё нужны |
| [WorkspacePageBase.cs](../../../../contentmanager.ui/src/ContentManager.Shareds/Components/Shared/Infrastructure/WorkspacePageBase.cs) | Busy/Dirty/lifetime; Discard-only guard; любой 409 получает «Данные изменились на сервере» | Save/Discard/Stay; distinct 412/domain409/parent/F15/unknown outcome |
| [nswag.json](../../../../contentmanager.ui/src/ContentManager.ApiClient/nswag.json), [client DI](../../../../contentmanager.ui/src/ContentManager.ApiClient/DependencyInjection/ApiClientServiceCollectionExtensions.cs), [ConfigureApi](../../../../contentmanager.ui/src/ContentManager.ApiClient/DependencyInjection/ApiClientWebAssemblyHostBuilderExtensions.cs), [ApiEnvelopeDelegatingHandler.cs](../../../../contentmanager.ui/src/ContentManager.ApiClient/Infrastructure/Http/ApiEnvelopeDelegatingHandler.cs) | Namespace/output/templates/init-accessors, scoped clients/configured HTTP/problem infrastructure | Переиспользовать после согласования contract/headers; наличие основы не доказательство готовности target |

Source inspection подтверждает current→target различия, а не готовность F08, выбранность internal setup или запуск UI/E2E tests.
