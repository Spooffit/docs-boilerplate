# F02. Schema Workspace и Draft SchemaVersion: фронтенд

[Бизнесовая спецификация](../../../business/features/F02-create-schema-version.md) · [Backend](../../backend/features/F02-create-schema-version.md) · [M01](../modules/M01-content-authoring.md) · [F01: общий SchemaEditor](F01-create-entity-schema.md)

Документ описывает согласованный F02 UI по [Q13](../questions/Q13-ui-scope.md) для .NET10, Blazor Standalone, MudBlazor, FSD и NSwag. Demo workspace реализован в текущих page/widgets/features; source map и scoped evidence приведены в §10–§11. Логические названия блоков связываются с actual paths, не требуют дополнительных FSD stores/слоёв. Вёрстка не задаётся.

## 1. Результат и границы

Пользователь открывает workspace одной EntitySchema, видит параллельные Draft и Published history, выбирает точную версию и продолжает работу в её контексте. Новый Draft начинается с чистого листа либо копированием выбранной версии; изменения явно сохраняются. Published content показывается только для чтения.

F02 включает workspace, создание/копирование/сохранение/удаление Draft, актуализацию Published-базы и contextual lifecycle actions. MVP добавляет Retire/Restore Published SchemaVersion и Archive/Restore логической EntitySchema. Допустимое физическое удаление логической схемы доступно только без Published history и любых logical instance owners, которые удаляются отдельным явным F04 действием, по [Q09](../../../business/questions/Q09-editing-and-deletion.md).

Публикация — [F03](F03-publish-schema-version.md), Withdrawal — [F13](F13-content-delivery.md), полноценный редактор экземпляра — [F04](F04-entity-instances.md). Навигация к экземплярам сохранена; новые Publish actions относятся к следующему F03, modern instance definition editor — к F04. Их формы/алгоритмы не дублируются. Общие type controls/create-режим SchemaEditor описаны в F01; ниже — отличия сохранённого Draft. Demo без входа/ролей/модерации; MVP Specialist определяется F16/Q10. Autosave, offline, browser storage формы и глобальный store не вводятся.

MVP включает согласованные [Q35](../../../business/questions/Q35-value-constraints.md) typed constraints/comparer в snapshot выбранной SchemaVersion. Inspector использует [общие controls F01 §6.2](F01-create-entity-schema.md#62-состав-представления-и-формы); Demo ими не расширяется. Изменение constraints в новой версии само по себе не требует нового FieldId, не меняет Published history и не снимает identity/monotonic ограничения.

## 2. Страницы, экраны и навигация

| Страница / экран | Точка входа и контекст | Состав и режим | Завершение / переход |
| --- | --- | --- | --- |
| SC01. SchemaWorkspacePage | /schemas/{schemaId}; список F01 либо прямой URL | Breadcrumb, identity/header, постоянный version rail, выбранный Draft editor либо Published view | Версия → /schemas/{schemaId}/versions/{versionId}; экземпляры → SC02 |
| SC02. Связанные экземпляры | /schemas/{schemaId}/instances; link из header | Контекст схемы, её EntityInstance list и «Создать экземпляр» по F04 | Выбор/create открывает F04; возврат в Schema Workspace |
| SC03. Новый Draft | SC01, «Новый черновик» | «С чистого листа» / «На основе выбранной версии», Title, optional ChangeComment; во втором режиме exact source/status | Cancel закрывает dialog; подтверждённое создание выбирает exact новый Draft |
| SC04. Актуализация основы | Stale Draft, «Актуализировать основу» | Старая база, current Published, semantic diff; предупреждение об отсутствии merge | Explicit consent → accept-base; успех обновляет Draft/base/ETag |
| SC05. Удаление Draft | Overflow выбранного Draft | Delete-impact, blockers, exact число зависимых Draft instance versions, предупреждение о cascade | Confirmation → delete; Cancel без изменения |
| SC06. Подтверждение ухода | Dirty: смена версии/переход/Reload/Cancel | Save / Discard / Stay | Save → обычный Save и intent после успеха; Discard → уход; Stay → прежняя форма |
| SC07. Monotonic-настройка | Inspector saved field: Required true→false или Deprecated false→true | Предупреждение: после Save обратный переход для той же сохранённой identity запрещён | Confirmation меняет только local request; Cancel оставляет значение |
| SC08. Contextual metadata/lifecycle | Header схемы либо overflow Published | Сведения логической схемы, её Delete/Archive/Restore; Retire/Restore exact version в MVP | Успех обновляет нужного owner; F03/F13 через публичные границы своих действий |

Без versionId SC01 выбирает newest Draft по CreatedAt descending; без Draft — latest Published по VersionNumber descending. Rail показывает Draft выше Published с тем же порядком внутри групп. Draft различаются Title; автор/время/база показываются при наличии в контракте. Draft не получает business VersionNumber. Retired/Withdrawn Published остаются видимыми с badges и historical context.

Exact route выбирает именно versionId этой схемы. Не найденная версия не заменяется «последней»: остаются доступные schema context/rail и явная ошибка. Без выбранной версии и при пустой истории — отдельное empty state с созданием Draft. Breadcrumb использует Name для подписи и immutable ID для перехода.

Draft editor открыт в workspace. Published показывает обычный текст и свойства, а не disabled form-controls; в edit-mode не переключается. На desktop rail остаётся видимым при работе с выбранной версией. Refresh загружает saved exact state; local input не восстанавливается из browser storage. Dirty внешний уход/refresh использует доступное браузерное предупреждение, которое не обещает выполнить Save.

Clean SchemaDraft начинается как локальная форма без server ID/ETag и создаётся на сервере при первом структурно валидном Save. Пустая server version не появляется. Copy сразу создаёт server Draft из сохранённого exact источника. Withdrawn источник допускает чтение, Copy и Save; Publish и runtime delivery блокированы, withdrawal не снимается копированием. Правило Retired источника MVP определяется отдельно. Новый server resource создаётся с operation ID и durable replay того же payload, как F01.

## 3. Карта FSD-блоков

Page координирует route/чтение/результаты независимых действий. SchemaEditor связывает tree/inspector; features выполняют законченные изменения. Client entities представляют generated DTO, shared содержит generic controls/dialog/transport. Новые projects, stores и feature для каждого GET не требуются.

| Имя блока | Слой / срез / сегмент | Ответственность | Использование в этом изменении | Основное описание |
| --- | --- | --- | --- | --- |
| Routes/DI | app | /schemas exact routes, существующие clients | EntitySchemaPage exact routes, existing DI | §2, §8 |
| SchemaWorkspacePage | pages/schema-workspace, ui/model | Context, selected exact version, read models/request/dirty, композиция | EntitySchemaPage.razor + .razor.cs | §3–7 |
| SchemaVersionRail | widgets/schema-version-rail, ui | Группы/порядок/выбор истории | SchemaVersionRail.razor | §2, §4 |
| SchemaEditor | widgets/schema-editor, ui/model | Tree/inspector/переход к ошибке | Переиспользовать [F01](F01-create-entity-schema.md#3-карта-fsd-блоков), saved-field режим | §6.2 |
| Draft actions | features/create-schema-draft, copy-schema-draft, save-schema-draft, accept-schema-draft-base, delete-schema-draft; model/api/ui по необходимости | Dialog/checks/pending/одна mutation | SchemaWorkspace feature helpers + page orchestration | §7–9 |
| Schema/version lifecycle actions | features: contextual metadata/delete/archive/restore схемы и retire/restore версии | Изменить конкретного owner | Принятая область Demo/MVP | §7–8; wire — Q15 |
| Schema/version/field presentation | entities: entity-schema, schema-version, schema-field | Generated reads, badges, Published свойства | Переиспользовать без копии DTO | §4, §6 |
| Controls/dialog/errors, generated client/headers | shared, технические сегменты | Универсальный ввод/confirmation/transport | Переиспользовать | §5, §8–9 |
| Связанные экземпляры | pages/schema-workspace, contextual F04 representation | Schema-scoped list/create navigation | Публичная граница F04, без второго editor | §2, §4 |

| Блок | Входы | Выходы | Собственное состояние и lifetime | Зависимости / использование |
| --- | --- | --- | --- | --- |
| SchemaWorkspacePage | Route IDs; generated schema/summaries/exact version | Route, action requests, result → refresh | Read models, один generated update request, own ETags, dirty/intent; до смены context | Rail/editor/actions, query client/navigation/dialog; SC01/SC02 |
| SchemaVersionRail | Summaries, selected ID, loading/error | Selected(versionId) | Presentation; выбранный ID у page | Presentation helpers, без API/mutation; SC01 |
| Draft actions | Exact IDs, generated request, нужный ETag | Pending/errors/result | Текущий запуск; preview/consent/countdown у delete; dialog input у create | Generated mutation client, generic transport/dialog; SC03–SC06 |
| Lifecycle actions | Owner context, generated metadata request, own ETag | Pending/errors/result | Текущий dialog/action, не отдельный store | Generated API/confirmation; SC08 |

Карточка SchemaEditor остаётся в F01. Здесь он получает generated persisted definitions с обязательной identity и generated create/update/delete requests. Business input у workspace, selection/expanded nodes/UI keys у widget; API editor не вызывает. SchemaDefinitionEditor использует F01 create-режим или saved request-режим с SchemaSavedDefinitionEditor/Settings и Tree; generated requests имеют set-accessors, отдельной editable копии Domain дерева нет. Read-only Published не вводит editable копию дерева. Список экземпляров и publish/withdraw actions сохраняют владельцев F04/F03/F13.

## 4. Логическая компоновка

~~~text
SC01. SchemaWorkspacePage [pages]
  Breadcrumb: Схемы → Name
  SchemaHeader: Name, Key, Description, own lifecycle/withdrawal state
    Новый черновик / Экземпляры схемы / contextual schema actions
  SchemaVersionRail [widgets]
    Draft: Title, CreatedAt, база/context
    Published: VersionNumber/time, Retired/Withdrawn badges
  Selected version area
    Draft
      Title / ChangeComment
      Stale-base banner: база, current Published, diff/action
      SchemaEditor [общий F01 widget, saved-field режим]
        Tree: root/nested fields, selection, add/remove, Deprecated
        Inspector: разрешённые свойства node/definition
          [MVP] Применимые typed constraints / List collection, recursive Item и comparer
      Validation/PublishIssues summary + переход к field/path
      Save / Publish [F03] / overflow
    Published
      Read-only metadata/дерево/свойства выбранного field
      Создать Draft на основе этой версии
      Retire/Restore [MVP] / Withdrawal [F13]
  SC03–SC08: dialog только при запуске действия

SC02. Связанные экземпляры [page + F04]
  Breadcrumb/header одной схемы
  Её EntityInstance list
  Создать / открыть Instance Workspace [F04]
~~~

Tree и inspector видны вместе; inspector меняет содержимое при выборе node. Основное редактирование field в inspector, не modal. Deprecated siblings видимы после non-deprecated, внутри групп порядок детерминирован. Эта группировка не меняет canonical content/serialization/hash.

| Связь частей | Что передаётся | Кто координирует | Результат |
| --- | --- | --- | --- |
| Rail → page → selected area | Exact versionId | Page + dirty guard | URL/область меняются после разрешённого перехода; старый response игнорируется |
| Tree → inspector | Generated node/definition, UI path | SchemaEditor | Одно выбранное определение без mutable дубля |
| Editor/metadata → page | Changed request | Page | Dirty=true; ошибки изменённого input перепроверены |
| Summary/diff → editor | Field/path | Page через editor boundary | Раскрыть ancestors, выбрать node, focus к свойству |
| Action → page → rail/editor/header | Confirmed owner/result | Page | Согласованный read refresh/new own ETag; success после подтверждения |
| Header → SC02/F04 | SchemaId/selected InstanceId/create context | Page/navigation | Список/Instance Workspace, без nested instance editor |

## 5. Зависимости и публичные границы

Page передаёт rail summaries/selected ID и принимает callback выбора. SchemaEditor получает generated definitions/request, readonly/pending/errors, сообщает Changed и предоставляет переход к field/path через границу из F01. Action получает owner IDs/request/ETag и возвращает pending/error/result. Это logical interfaces, точные C# signatures определит реализация.

Widget не импортирует page и не вызывает соседнюю feature. Save/accept/delete/lifecycle независимы, результаты объединяет page. F03/F13 получают exact context через публичные границы своих действий. Shared dialog принимает текст/варианты/disabled state и возвращает выбор, а impact/cascade — ответственность delete action.

Generated DTO/client — граница данных браузера; Domain/EF не импортируются. Общий editor требует проверки обоих потребителей F01/F02. Store/дублирующий EditorModel для связи не вводится.

## 6. Данные и владение состоянием

### 6.1. Состояние

| Данные / состояние | Источник истины | Клиентский владелец / lifetime | Обновление / сброс |
| --- | --- | --- | --- |
| Schema metadata/lifecycle, summaries | Backend F01/F02 | Workspace, текущий SchemaId | Вход/refresh/confirmed mutation; поздний response старого context игнорируется |
| Exact version/content и ETag | Exact GET/response header | Workspace, выбранный versionId; ETag отдельно | Select/reload/success; list/schema ETag не подставляется |
| Draft input | Один generated update request + read-only loaded snapshot | Workspace, текущий Draft | Ввод/operations; success → read refresh/request; Discard → saved state |
| New Draft metadata | Generated create/copy request | Action/SC03 и локальная clean editor форма | Clean без server ID до first valid Save; Copy сразу persists; Cancel local без mutation |
| Selection/expanded/UI keys | UI-state | SchemaEditor, selected version | Select/add/remove; удалённый node больше не выбран |
| Dirty/intent | UI-state | Page | Input → dirty; success/discard → reset; error/Stay сохраняют |
| Pending/errors/impact/consent/countdown | Запуск action; preview сервера | Feature | Open/response/close; consent не переносится на другое delete |

Loaded snapshot, local operations, отправленный request и confirmed result различаются. Snapshot и update request имеют разные контрактные роли, это не две редактируемые копии дерева. UI не генерирует Id/VersionNumber/Version/ContentHash. UI keys/path bindings не сериализуются; универсальная field model с nullable Id/IsNew/fake GUID не нужна.

### 6.2. Состав представления и формы

| Сведение / поле | Представление / ввод | Правило |
| --- | --- | --- |
| Schema Name/Key/Description/lifecycle | Header/contextual metadata | Key immutable; Name/Description — logical metadata, не PUT Draft; contract backend/Q15 |
| Draft Title/ChangeComment | Editor header/SC03 | Title различает Draft; optional свободный comment без Jira field; limits сервера |
| Number/time/status | Rail/Published header | Number только Published; Retired/Withdrawn отдельны, content read-only |
| Published schema snapshot, Title/ChangeComment | Read-only version metadata | Исторический контекст выбранной версии; rename логической схемы не переписывает snapshot |
| Base/current Published/IsStale/diff | Banner/SC04 с separate diagnostic basis | Stale Save allowed; Publish blocked; Accept привязан к shown basis отдельно от own ETag |
| Saved field Id/Key/Type | Inspector | Id диагностически; Key/Type/type semantic identity и List.Item shape readonly |
| DateTime TemporalSemantics [MVP] | Read-only свойство exact definition/Item | Historical Naive сохраняется при Copy; новый DateTime — Offset. Mutable selector существующей semantic identity отсутствует |
| Saved Name/HelpText | Inspector | Mutable authoring metadata по F01/F02/Q34 |
| Saved Required/Deprecated | Inspector/tooltip/SC07 | Только true→false / false→true соответственно; Deprecated требует Required=false |
| Enum/Flag options, List OrderMode | Applicable inspector | Existing option Key immutable, Name mutable, add/monotonic Deprecated; только backend-allowed settings |
| Typed constraints / List comparer [MVP] | Общий inspector F01, settings выбранного definition/Item | Усиление/ослабление constraints внутри intrinsic типа без нового FieldId; Default без comparer, Ascending/Descending с supported comparer; Published только read-only |
| New fields/subtrees | Tree add + inspector | Generated create definitions F01 без ID; Key/Type выбираются до Save; IDs серверные |
| PublishIssues/validation | Summary/inspector | «Нельзя опубликовать: N проблем», переход к field/path; strict Publish у F03 |

Общие type controls/tooltips/recursive Object/List/List item-definition и все 15 типов — [F01 §6.2](F01-create-entity-schema.md#62-состав-представления-и-формы), [Q34](../../../business/questions/Q34-schema-field-semantics.md). F02 не возвращает InheritanceStrategy/ColorFormat/numeric options/runtime input/Asset picker в editor definition.

Save использует generated Creates/Updates/Deletes/Restores backend F02: create definitions без ID, existing update/delete с FieldId; recursive authoring boundary explicit union, не ручная FormModel с nullable fake ID. Для нового Key/Type/meaning или parent-definition chain требуется новая identity. Возврат исторически удалённого FieldId допустим с тем же contract/parent chain и global monotonic history; UI использует server identity admission, не присваивает ID новому field. Local incomplete input допустим, server Save требует минимум один root и structural validity. New subtree под unsaved parent отправляется одной ветвью; nested create address — ParentFieldId+ItemDepth; Definition.Item описывает recursive anonymous settings. Restores(SourceVersionId,FieldId) — серверный historical admission в Save; отдельной Restore GUI кнопки нет. History Copy сохраняет source identities; source snapshot не редактируется.

MVP constraints редактируются в том же generated update request workspace; widget владеет только selection/expanded/errors presentation. Модель определяется один раз в [backend F01 §2.1](../../backend/features/F01-create-entity-schema.md#21-aggregates-entities-и-value-objects). Сохранение отклоняет min > max, negative length/count, MultipleOf ≤ 0, constraint чужого типа и invalid comparer/path. ObjectFieldPath выбирается только по required Object chain к supported required scalar leaf, хранит ordered FieldIds и не проходит через List. Для новых ещё не сохранённых fields transport ссылки/назначение IDs остаётся Q15, fake client FieldId не вводится. Изменение Required/удаление path node может сделать comparer невалидным: ошибка показывается в inspector/summary, Save не исправляет путь молча.

Published view показывает замороженные constraints/Item/comparer как обычные свойства; edit-controls и изменение каталога правил отсутствуют. При Copy definitions переходят в новый Draft; изменение target constraints не orphan-ит type-compatible instance value само по себе: адресуемые issues и Save/Publish различаются по F04. Semantic bounds, equality/order и ограничения controls берутся из F01/Q35 без второй таблицы DTO или business FormModel.

DateTime следует [F01 §6.2](F01-create-entity-schema.md#62-состав-представления-и-формы) и [Q34 DateTime MVP](../../../business/questions/Q34-schema-field-semantics.md#datetime-mvp-temporal-semantics-и-совместимость). Naive→Offset требует удаления старого поля и добавления нового с новым server FieldId; editor не меняет TemporalSemantics у сохранённой identity и не переинтерпретирует historical definition, включая List.Item. Copy старой schema сохраняет Naive; при ручной замене новый Offset field получает новую identity. Нет автоматического преобразования по server/user timezone или переноса по имени/key. Последующая instance Copy оставляет старое value orphan, новое field незаполненным по F04; specialist вводит value и offset вручную. Constraint-only change same semantic definition не требует нового FieldId. Published history и `/1` остаются прежними.

### 6.3. Доставка ассетов в version workspace после Demo

Draft переиспользует [DeliveryMode inspector F01](F01-create-entity-schema.md#63-режим-доставки-assetreference-после-demo) у каждого AssetReference leaf, включая recursive ItemDefinition. Workspace владеет generated update request/dirty/ETag, widget — выделением и контролом; Changed → обычный Save, без отдельного endpoint или глобальной политики MediaAsset. Published inspector выводит сохранённый режим read-only. Изменение требует нового Draft/Published; старый exact snapshot не меняется. Исторический профиль без mode читается с effective OnDemand без перезаписи hashes.

В компоновке SC01 дополнительный контрол расположен в existing Inspector рядом с AssetKind и только для применимого leaf. Copy показывает значение exact source; unknown codec/profile не подменяется default. Проверки: Copy/Save/read сохраняют выбранный режим, nested List leaf доступен, Published не редактируется, ошибки адресуются к definition/path. Правило сохранения FieldId при mode-only edit и новый codec — [Q37](../questions/Q37-launcher-asset-delivery.md); интеграционный этап не расширяет Demo.

## 7. Действия и клиентские потоки

| Действие | Запуск | Исполнитель | Обработка / серверная операция | Результат |
| --- | --- | --- | --- | --- |
| UI01. Открыть / выбрать | Route/rail SC01 | Page | Schema/list/exact reads; default только без versionId | Правильный context/editor/read-only view |
| UI02. Clean Draft | SC03 clean | Page + CreateSchemaDraft | Локальная metadata/tree форма → first valid Save → POST с operation ID | Exact server Draft только после success; replay same ID/payload |
| UI03. Copy | SC03 copy exact source | CopySchemaDraft | Generated request; source Draft/Published own If-Match; копируются saved данные | Result ID выбран; source неизменён, field IDs/base определяет backend |
| UI04. Input/tree | SC01 Draft; MVP constraints/comparer выбранного definition/Item | Общий SchemaEditor + page | Local generated request, add/remove/select, typed applicability/path validation; SC07 monotonic warning | Dirty/inline issue; API только UI05 обычным Save с исходным own ETag; Published не редактируется |
| UI05. Save | SC01 либо SC06 Save | SaveSchemaDraft | Validate → PUT own Draft ETag → pending → result/errors | Refresh exact Draft/rail/new ETag, dirty=false; stale Save допустим |
| UI06. Publish / Withdraw / ReturnToDelivery | Contextual SC01 | F03/F13 | Publish blocked при dirty/stale/issues/withdrawal, no hidden Save; explicit Return checks Published | Result/read refresh F03/F13; Publish не снимает withdrawal |
| UI07. Accept base | SC04 | AcceptSchemaDraftBase | Diff/warning → explicit consent → accept-current-base own Draft ETag | Approved base; content не merge; error сохраняет context |
| UI08. Draft от current Published | Stale banner → SC03 | CopySchemaDraft через page | Explicit current Published source; ручной перенос пользователем | Отдельный Draft, stale Draft прежний |
| UI09. Delete Draft | SC05 | DeleteSchemaDraft | Impact/count/blockers → consent → DELETE own Draft ETag | Версия исчезает из rail; page выбирает default по §2, обновляет read models |
| UI10. Retire/Restore [MVP] | SC08 Published | Version lifecycle | Confirmation → own version If-Match | Badge/availability; content/number/history прежние, version в rail |
| UI11. Schema metadata/delete/archive/restore | Header/SC08 | Schema lifecycle | Metadata отдельно от Save Draft; delete только без Published history/с impact; Archive/Restore MVP; own schema ETag | Read refresh либо delete → /schemas; точный wire Q15 |
| UI12. Instances | SC01 header/SC02 | Page + F04 | Schema context → list/create F04 | Переход в Instance Workspace |
| UI13. Уход/Cancel/Reload | Dirty SC01 | Page + SC06 | Save/Discard/Stay; Save error не разрешает intent | Осознанный переход либо прежняя форма |

UI09 показывает exact число и состав зависимых Draft EntityInstanceVersion и необратимость cascade. При dependencies Delete disabled минимум 5 секунд после отображения impact; countdown не означает согласие. Нужен отдельный явный confirmation exact dependency set. Backend повторно вычисляет set; любое изменение отвергает действие и возвращает новый impact. UI снимает consent, показывает новый состав и заново проводит confirmation с 5 секундами для SchemaCascade. Published version физически не удаляется. Schema delete подтверждает весь logical impact, а не только selected Draft.

Copy не переносит unsaved input молча: dirty guard сначала разрешает Save source либо Discard local changes. Accept-base разрешает dirty intent сначала, затем показывает exact current Published basis и отправляет consent, привязанный к этому diagnostic basis, отдельно от own Draft If-Match. Если basis изменился, сервер отказывает, UI перечитывает diff и требует нового подтверждения. Accept не меняет payload и не выполняет merge или Publish.

Pending исключает повтор той же mutation/одновременную запись того же owner и изменение отправляемого input, без блокировки всей SPA. Старый response после смены context/ухода не подменяет новый выбор и не вызывает позднюю навигацию. Cancellation клиента не доказывает отмену server commit; unknown outcome не запускает auto-retry.

## 8. Использование API и NSwag

Backend F02 — источник target операций/DTO; таблица не копирует wire models. Generated methods получены из актуального OpenAPI; exact request/read owner — backend F02, remaining MVP contracts — [Q15](../../backend/questions/Q15-api-and-source-code.md).

| UI-поток | Серверная фича / операция | Вход | Результат | Владелец вызова |
| --- | --- | --- | --- | --- |
| UI01 header | Backend F01 exact schema GET | SchemaId | Metadata/current Published pointer/own schema ETag | Page/query client |
| UI01 rail | F02/DF01, GET /api/v1/entity-schemas/{schemaId}/versions | SchemaId/list request | Summaries, paging Q15 | Page/query client |
| UI01 exact | F02/DF02, GET /api/v1/entity-schemas/{schemaId}/versions/{versionId} | Exact IDs | DTO/content/base/issues/current Published context/ETag | Page/query client |
| UI02 clean | F02/DF03, POST /api/v1/entity-schemas/{schemaId}/versions | Локальный Draft до первого valid Save; generated full snapshot и operation ID, no If-Match для нового resource | Created exact Draft/result; replay same ID+payload | CreateSchemaDraft |
| UI03/UI08 copy | F02/DF04, POST /api/v1/entity-schemas/{schemaId}/versions/{sourceVersionId}/copies | Generated metadata; exact source Draft/Published If-Match | Created Draft + own ETag; original-result replay with same operation ID/input/source header | CopySchemaDraft |
| UI05 Save | F02/DF05, PUT /api/v1/entity-schemas/{schemaId}/versions/{versionId} | Generated update/own Draft If-Match | Confirmed DTO/new ETag/errors | SaveSchemaDraft |
| UI07 accept | F02/DF06, POST /api/v1/entity-schemas/{schemaId}/versions/{versionId}/accept-current-base | Draft ID/If-Match + показанная PublishedVersionId; без merge payload | Base либо blockers | AcceptSchemaDraftBase |
| UI09 impact/delete | F02/DF07–DF08: GET exact /delete-impact; DELETE /api/v1/entity-schemas/{schemaId}/versions/{versionId} | Exact ID, SchemaDeleteRequest(ConsentToken,Cascade) body, DELETE Draft If-Match | CanDelete/full count/composition/blockers/token; confirmed deleted ID | DeleteSchemaDraft |
| UI10 Retire/Restore | F02/DF09–DF10, POST exact version /retire либо /restore | Published ID/own If-Match | Lifecycle/new ETag | Version action |
| UI11 schema impact/delete | F02/DF13–DF14; GET /api/v1/entity-schemas/{schemaId}/delete-impact, DELETE exact schema | SchemaId, ConsentToken+Cascade body, own schema If-Match | Full impact/deleted owner; no instance-owner cleanup | Schema action |
| UI11 schema Archive/Restore [MVP] | Future logical lifecycle, не Demo | Own schema If-Match | Lifecycle/current owner | Future schema action |
| UI11 schema metadata | F02/DF15, PUT /api/v1/entity-schemas/{schemaId} | UpdateSchemaMetadataRequest(Name,Description), own schema If-Match | EntitySchemaDetailsDto/new schema ETag; Draft untouched | Schema action |
| UI06/UI12 | F03/F13/F04 | Exact context | Их result | Их публичные actions/reads |

Infrastructure: ContentManager.ApiClient.Generated.IEntitySchemaClient + IEntitySchemaVersionClient; src/ContentManager.ApiClient/nswag.json → OpenApi/Generated/ContentManagerApiClients.g.cs; ConfigureApi/AddContentManagerApiClients и ApiEnvelopeDelegatingHandler. Generated read/create/update/copy модели используются напрямую, generated classes вручную не редактируются. Неподходящий contract исправляется через server DTO/OpenAPI/regeneration, не через постоянный FormModel. Current generation задаёт writeAccessor=set. Regeneration, UI build, generated recursive runner и HtmlRenderer прошли; headers захватывает ApiResponseMetadataScope, результат mutation применяется из returned DTO/ETag без hidden HTTP GET. LegacyFields отображает LegacySchemaSnapshotFields read-only; nullable compatibility не превращается в editable modern definition и оставляет старый F04 instance editor рабочим до его отдельного обновления.

ETag — strong header отдельно от DTO у exact owner. Save/delete/accept — own Draft ETag; copy Draft/Published — exact source ETag; Retire/Restore — Published ETag; logical actions — EntitySchema ETag. Schema/другой Draft/list token не подставляется. Version/ExpectedVersion/Revision в HTTP body/query нет. Successful mutation применяет returned exact DTO/new own ETag; обновление rail/logical read models — отдельный refresh. Missing generated headers исправляются в общей generation/transport infrastructure по [S06](../standards/S06-api-client.md), скрытый GET не заменяет полный mutation response.

412 — изменение exact resource; 409 stale Published-base — отдельный Publish conflict F03. Machine codes/diff/issues/error paths определяет backend/Q15: UI не угадывает их по status/text. Field errors адресуются inspector/metadata, неизвестный path остаётся в summary.

## 9. Состояния, ошибки и доступ

| Ситуация | Представление | Действия | Данные / восстановление |
| --- | --- | --- | --- |
| Read loading/empty/error | Ожидание области; пустая history отличается от GET failure | New Draft либо Retry GET по состоянию | Старый response не подменяет context; safe loaded данные при refresh error не обязаны исчезать |
| 404 | Явно отсутствующий owner/version | Rail/возврат, если context доступен | Нет fallback exact на другую версию |
| Dirty/invalid input | Unsaved indication, field errors/summary | Исправить/Save/guard | Input/selection сохранены, неполный local tree не объявлен saved |
| Mutation pending/success | Ожидание action, затем короткое confirmation | Конкурирующая mutation того же owner blocked | Один запрос; read refresh/new ETag, toast не заменяет update |
| Validation/domain blockers | Inline/summary/banner нужной области | Исправить/отменить | Local input сохранён, false success нет |
| Invalid constraints/comparer [MVP] | Ошибка у typed setting/Item/path и summary | Исправить, explicit Save | Request/selection сохранены; invalid settings не корректируются автоматически |
| 412 Precondition Failed | Persistent «Версия на сервере изменена»; local форма видима | Explicit Reload/Reopen с осознанным discard | Не брать новый ETag для auto-retry, не merge. Copy conflict также сохраняет source editor |
| 428 Precondition Required | Безопасная общая ошибка | Сохранить context, исправить API/client flow | Implementation defect: header обязан отправляться, не штатный сценарий пользователя |
| Stale Published-база | Base/current Published/diff, Publish blocked | Save, copy current Published, explicit accept | Own Draft ETag актуален: Save разрешён; baseline update не merge |
| Delete blockers | Предметный refusal | Cancel/новый preview по необходимости | Старый count не authority, Published cascade нет |
| Changed dependencies после preview | Отказ и новый exact impact | Повторное подтверждение нового set; 5 секунд SchemaCascade заново | Прежний consent не разрешает расширенное или изменённое удаление |
| Timeout/network после mutation | «Результат операции не подтверждён» | Explicit проверка saved state; create recovery Q15 | Input сохранён; нет blind retry/ложного rollback |
| Archived schema [MVP] | Deep link readonly, Archived badge | Restore по lifecycle | Authoring blocked; Draft/history сохранены, child lifecycle не переписан |
| Retired/Withdrawn Published | Readonly historical badges | Withdrawn: read/Copy/Save; Publish/runtime blocked. Retired: MVP policy | Withdrawal не снимается новым Draft или Publish; historical identity сохранена |
| 403 | Отличим от 404 | Сохранить context/вернуться | Demo без login flow; MVP F16/Q10 отдельно |

Archive Schema блокирует authoring/new Draft/instances/new publishing; Restore возвращает доступность без Publish/Activate. Historical Delivery сохраняется, Withdrawal — F13. Retired остаётся в owner history, не общем Archive. Existing Draft instances на retired version не удаляются; их Publish ограничен F04/F03.

По [S05](../../../catalog/docs/standards/ui/forms-and-accessibility.md#form-state-and-user-feedback)/[S07](../../../catalog/docs/standards/ui/forms-and-accessibility.md#access-and-semantics) rail/tree/actions доступны с клавиатуры; selection/вложенность/errors ясны без цвета. Summary переводит focus к проблеме. Dialog объясняет scope/countdown/disabled; Cancel/Stay возвращают focus. Tooltip объясняет readonly identity/monotonic transition. UI availability не заменяет server checks.

## 10. Проверки и приёмка

Подтверждённый scope checkout 07.10.2026:

| Проверка | Наблюдаемый результат | Граница |
| --- | --- | --- |
| UI build/runner | Build прошёл; runner F01/F02/legacy и [F02Tests](../../../../contentmanager.ui/tests/ContentManager.Ui.Tests/F02Tests.cs) прошли | Generated requests/flow state/header/error/dirty/replay/confirmation checks; не весь browser flow |
| Real HtmlRenderer | [F02RenderingTests](../../../../contentmanager.ui/tests/ContentManager.Ui.Tests/F02RenderingTests.cs) прошли | Реальный Blazor render workspace/editor/recursive anonymous Item/accessibility labels/error state; API fixture |
| Actual browser Copy→Save→Reopen | Copy created exact Draft, Save persisted local edit, reopen showed confirmed result; source FieldIds сохранены | Real local API/UI; no source identity rewrite |
| Actual browser 412 | Concurrent real server change rejected stale Save; local form осталась видимой до explicit Discard/Reload | No auto-retry with new ETag/no silent merge |
| Actual browser clean Draft | Empty local Save blocked, valid first Save created exact server Draft | Local-first boundary, no persisted empty schema Draft |
| Actual browser delete preview | Full exact composition/count, separate unchecked consent, disabled irreversible action; Cancel closed preview | Permanent GUI delete не выполнялся |
| Real backend deletion | PostgreSQL Integration 36/36 и Functional 50/50 подтверждают commit/consent/FK/owner preservation | GUI irreversible click не заменён заявлением о browser commit |

Unit 427/427 (coverage 91.98% lines/87.72% branches) и scoped HTTP suite 50/50 также проверяют concurrent Save один 200 / один 412, Copy original-result replay, basis drift 409/Accept no merge, immutable Published metadata и nested create atomicity. Historical Restore HTTP прошёл: exact source FieldId + metadata в том же Save, historical source неизменен. Scoped App Publishing/Delivery 9/9 также прошли; они подтверждают совместимость выбранного контура, не весь будущий publishing workflow. Отдельной Restore GUI action нет; server Save Restores и history Copy — разные средства. Локальные приложения для browser checks остановлены; ports проверены свободными и temporary QA tab закрыт.

F02 Demo реализован и проверен в указанном scope; весь Demo, Publish F03, modern instance editor F04, MVP Retire/Archive и production deployment этим не объявляются готовыми.

Оставшаяся target matrix по [S08](../standards/S08-testing.md) ниже шире выполненного scope. API doubles подходят для orchestration; реальные identity/copy/concurrency/cascade подтверждаются backend, future MVP criteria не считаются автоматически запущенными.

| Сценарий | Условия / воздействие | Наблюдаемый результат | Граница |
| --- | --- | --- | --- |
| Rail/default/exact | Несколько Draft/Published, URL без/с versionId, invalid exact | Порядок §2, правильный default, exact без fallback | Page/query double |
| Dirty/late response | Смена A→B, Save/Discard/Stay, поздний A | Guard/context сохранены; A не подменяет B | Page/navigation |
| Create/copy | Ordinary Draft/Published source, Title/comment, dirty | Exact source/header, result ID выбран, source прежний | API/component; identity real backend |
| Saved/new identity | Existing/new/historical nodes, move между parents | Existing Key/Type/parent chain readonly; reintroduced FieldId same contract/global monotonic; move=new identity; create no fake ID | Editor/generated serialization + real historical admission |
| Monotonic/options | Required/Deprecated/existing option keys | Warning/tooltips/allowed transitions; Deprecated+Required invalid, numeric options нет | Editor/API boundary |
| Общий editor F01/F02 | Все types, Object/List/List<List>, saved/create | Recursive controls работают, F01 без IDs, F02 identity защищена | Widget/consumers |
| MVP constraints evolution | Усилить/ослабить bounds/Item constraints; invalid min/max/step/type/path; Copy Published | FieldId сохранён при одном изменении constraints; published snapshot прежний; typed controls/no duplicated FormModel; invalid Save с адресуемой issue | Shared editor/generated integration + real backend |
| MVP DateTime semantic identity | Copy historical Naive, new DateTime/Item, смена Naive→Offset | Naive readonly/copy прежний, new Offset fixed; только delete+add нового server FieldId, old history immutable/no migration; bounds 7digits/instant semantics F01 | Shared editor/generated transport + real identity/history fixtures; target criteria |
| MVP comparer | Default/ascending-descending; required Object FieldId path, optional/List/unsupported leaf | Supported selection, invalid/unsupported disabled с причиной; сохраняется полный typed snapshot | Editor + real round-trip; new-field transport после Q15 |
| Save/errors | Valid/0 roots/incomplete definitions/duplicate key | Own If-Match/operations, адресуемые errors, confirmed ETag/read refresh | Feature/double; real round-trip |
| 412/428 | Save/delete/accept/copy conflict, missing header response | 412 сохраняет form/selection без retry/merge; 428 defect | Transport/page |
| Stale-base | Другой Draft Published, own ETag актуален | Save доступен, Publish blocked, explicit accept без merge | UI/API; baseline real backend |
| Impact/consent | Exact count, clock до/после 5 s, Cancel/blocker | Disabled+отдельное consent, Cancel без DELETE, server recheck/нет Published cascade | UI clock/double; atomic effect real backend |
| Logical/Published lifecycle | Published history/no history, archived deep link, retired | Delete только допустимой схемы; Archive readonly; Restore не Publish; retired в rail/own ETag | Composition/API; backend |
| Neighbour boundaries | Instances, dirty Publish, withdrawal | F04 без nested editor, Publish не Save, exact contexts | Page composition |
| Async/accessibility | Double click/network/уход/keyboard | Один pending; unknown не auto-retry; focus/context/disabled reason ясны | API/component/browser |

Clean local→first valid Save, immediate server Copy и Withdrawn read/Copy/Save проверяются по принятым правилам. Retired MVP policy не выбирается молча. Scoped browser/runner/backend evidence приведён выше. Future expanded real-contour matrix должна сохранять exact Save/reopen/replay/identity/concurrency и changed-set repeated countdown; наличие дальнейшего критерия не означает его запуска.

## 11. Последовательность изменений и открытые вопросы

Backend F02/OpenAPI и regenerated NSwag/headers задают materialized Demo contract; saved-field режим общего editor F01, workspace rail/metadata/read-only/actions и local clean Draft реализованы. Guard/conflict/base/impact dialogs используют separate diagnostic basis. Проверяются оба editor consumers и реальные round-trip/replay/cascade. Массовый рефакторинг соседних фич не входит в порядок.

| Вопрос | Какие детали блокирует |
| --- | --- |
| [Q09](../../../business/questions/Q09-editing-and-deletion.md)/[Q15](../../backend/questions/Q15-api-and-source-code.md) | Local clean до первого valid Save, immediate Copy, exact-set rejection/reconfirmation и Withdrawn read/Copy/Save приняты; exact Demo wire материализован; Retired MVP policy остаётся отдельным backend lifecycle contract |
| [Q15](../../backend/questions/Q15-api-and-source-code.md) | Demo generated set-models/methods, error/ETag boundary, paging20/max200, ConsentToken+Cascade, metadata PUT и durable replay материализованы; future MVP Archive/Restore/typed settings отдельно |
| [Q13](../questions/Q13-ui-scope.md) | UI test contour/невыбранные list parameters; принятые routes/tree/inspector/Save/guard не открываются заново |
| [Q34](../../../business/questions/Q34-schema-field-semantics.md)/[Q35](../../../business/questions/Q35-value-constraints.md) | MVP constraints/comparer и immutable Naive/Offset согласованы; new-field ObjectFieldPath/OpenAPI temporal union — Q15, exact whole new-profile codec/carriers/golden bytes — Q19/Q31; Demo/saved-field rules сохранены |

Подтверждённая исходная точка UI 07.10.2026:

| Источник | Текущее назначение | Граница |
| --- | --- | --- |
| [EntitySchemaPage.razor](../../../../contentmanager.ui/src/ContentManager.Pages/Components/ContentModeling/EntitySchemas/EntitySchemaPage.razor), [code-behind](../../../../contentmanager.ui/src/ContentManager.Pages/Components/ContentModeling/EntitySchemas/EntitySchemaPage.razor.cs) | /schemas/{id} и exact /schemas/{id}/versions/{versionId}; legacy route alias; own schema/version ETags, rail paging, local clean editor, Copy/Save/base/metadata/delete dialogs | No HasDraft one-draft gate; Draft Save не меняет logical metadata; Publish/F04 следующие отдельные slices |
| [SchemaVersionRail](../../../../contentmanager.ui/src/ContentManager.Widgets/Components/ContentModeling/EntitySchemas/UI/SchemaVersionRail.razor) | История/selection/load-more/error presentation | Page владеет context/reads |
| [SchemaDefinitionEditor](../../../../contentmanager.ui/src/ContentManager.Widgets/Components/ContentModeling/EntitySchemas/UI/SchemaDefinitionEditor.razor), [saved editor](../../../../contentmanager.ui/src/ContentManager.Widgets/Components/ContentModeling/EntitySchemas/UI/SchemaSavedDefinitionEditor.razor), [settings](../../../../contentmanager.ui/src/ContentManager.Widgets/Components/ContentModeling/EntitySchemas/UI/SchemaSavedDefinitionSettings.razor) | Общий generated create/saved mode, recursive definitions, readonly identity/monotonic flags, operations request | No browser Domain/EF dependency; no duplicate editable definition model |
| [SchemaDraftEditing](../../../../contentmanager.ui/src/ContentManager.Features/SchemaWorkspace/SchemaDraftEditing.cs), [SchemaMutationFlow](../../../../contentmanager.ui/src/ContentManager.Features/SchemaWorkspace/SchemaMutationFlow.cs), [SchemaDirtyNavigation](../../../../contentmanager.ui/src/ContentManager.Features/SchemaWorkspace/SchemaDirtyNavigation.cs), [SchemaDeleteConfirmation](../../../../contentmanager.ui/src/ContentManager.Features/SchemaWorkspace/SchemaDeleteConfirmation.cs) | Saved operations, exact mutation result/unknown replay, Save/Discard/Stay, separate consent+five-second gate | Current action helpers/page lifetime, no global store |
| [LegacySchemaSnapshotFields](../../../../contentmanager.ui/src/ContentManager.Widgets/Components/ContentModeling/EntitySchemas/UI/LegacySchemaSnapshotFields.razor), existing legacy instance editor | Readonly LegacyFields compatibility; older F04 still consumes its own legacy models | No automatic modern conversion; modern instance definition editor next F04 |
| [nswag.json](../../../../contentmanager.ui/src/ContentManager.ApiClient/nswag.json), [generated clients](../../../../contentmanager.ui/src/ContentManager.ApiClient/OpenApi/Generated/ContentManagerApiClients.g.cs), [DI](../../../../contentmanager.ui/src/ContentManager.ApiClient/DependencyInjection/ApiClientServiceCollectionExtensions.cs) | writeAccessor=set, regenerated exact F02 request/read methods, existing envelope/header infrastructure | Generated source не редактируется вручную; backend contract owner §8 |

Source presence не заменяет evidence: реальные выполненные checks и их ограничения указаны в §10; documentation link/structure check не является приложением/API/browser test.
