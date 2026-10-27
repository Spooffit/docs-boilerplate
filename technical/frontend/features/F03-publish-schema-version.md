# F03. Publish SchemaVersion: фронтенд

[Бизнесовая спецификация](../../../business/features/F03-publish-schema-version.md) · [Backend](../../backend/features/F03-publish-schema-version.md) · [M01](../modules/M01-content-authoring.md) · [F02: Schema Workspace](F02-create-schema-version.md) · [F01: общий SchemaEditor](F01-create-entity-schema.md)

Документ задаёт целевое UI-состояние по [Q13](../questions/Q13-ui-scope.md) для .NET 10, Blazor Standalone, MudBlazor, FSD и NSwag. Имена блоков — логические обозначения, не утверждение о готовых классах. Исходники подтверждают только текущую реализацию; вёрстка не описывается.

## 1. Результат и границы

Пользователь публикует **выбранный сохранённый** Draft SchemaVersion и остаётся в контексте той же exact version, ставшей immutable Published contract. Номер `VersionNumber`, hash, PublishedAt и historical schema Name/Key назначает сервер. Успех означает подтверждённый Published DTO и новый ETag, а не предварительно изменённый badge.

F03 включает запуск Publish, ожидание, strict validation/concurrency/stale-base отказы и применение результата в workspace. Чтение, rail, explicit Save, редактор и актуализация основы переиспользуются из F02; определения полей и read-only свойства — из F01. Publish не выполняет скрытый Save. Нумерация, merge/rebase и повтор записи не выполняются клиентом.

SchemaVersion Publish не означает Product/Variant publication, Activation или доступность Delivery. F17 moderation не применяется. Demo без входа/ролей; MVP access принадлежит F16, не является зависимостью этого сценария. Для одиночного Publish новый preview endpoint, отдельный editor, глобальный store и browser persistence здесь не вводятся. Отдельный целевой batch preflight уточняется Q38.

MVP Publish повторно проверяет весь typed definition snapshot по [Q35](../../../business/questions/Q35-value-constraints.md), включая constraints каждого recursive List Item и collection/comparer settings. Inspector и настройки принадлежат F01/F02; F03 показывает их diagnostics и immutable Published результат. Demo не получает конструктор constraints или новые зависимости.

Пользовательская подпись действия — «Зафиксировать версию»; HTTP Publish/enum Published сохраняются. Список схем также предоставляет «Зафиксировать выбранные версии» для exact Draft selection по общему [workflow F07](F07-product-content-assignment.md#мастер-подготовить-выпуск). Target preflight показывает dependencies/blockers и own tokens; ordinary Save не запускает Publish. Профиль показывается в технических подробностях как «Профиль расчёта хеша», digest отдельно как «SHA-256 содержимого», без смешения profile ID и hash.

## 2. Страницы, экраны и навигация

| Страница / экран | Точка входа и контекст | Состав и режим | Завершение / переход |
| --- | --- | --- | --- |
| SC01. Выбранный Draft в SchemaWorkspacePage | /schemas/{schemaId}/versions/{versionId}; выбор rail по F02 | Identity/header, rail, Title/ChangeComment, tree + inspector, baseline/blockers, Save и «Зафиксировать версию» | Publish success → SC03 в том же exact route; error → SC01/SC02 |
| SC02. Диагностика Publish | Отказ публикации либо уже известные PublishIssues в SC01 | Summary, маркеры дерева, сообщения inspector/metadata; stale-base отдельно показывает baseline/current Published и semantic diff по F02 | Переход к проблеме; ручное исправление → explicit Save; stale recovery через F02 |
| SC03. Exact Published result | Подтверждённый Publish response выбранной версии | Read-only version metadata/структура/свойства, server number/time, historical schema snapshot, lifecycle badges | Основное продолжение «Создать черновик на основе версии» через F02; выбор history через rail |

SC02 — состояние области workspace, не новый route и не обязательный modal. Обычная валидная публикация не требует тяжёлого confirmation каждый раз. Краткое подтверждение не объявляется обязательным условием Publish. Предупреждения о monotonic изменениях находятся в F02 editor в момент изменения, а не переносятся в новый publish dialog.

Действие доступно, когда exact версия загружена, имеет Status=Draft, editor clean, известен её собственный актуальный ETag, нет известного stale-base/blocker или pending записи того же owner. Недоступность объясняется рядом с действием. Dirty требует обычного явного Save по F02, не автоматического сохранения при нажатии Publish. UI checks не заменяют повторные server checks.

Exact route после Publish сохраняет versionId из операции/подтверждённого результата; UI не выбирает произвольный latest Published. SC03 показывает обычный текст/свойства, а не disabled edit-controls, и не переключается в edit-mode. Defaults rail, отсутствие exact версии и dirty guard остаются правилами F02.

## 3. Карта FSD-блоков

| Имя блока | Слой / срез / сегмент | Ответственность | Использование в этом изменении | Основное описание |
| --- | --- | --- | --- | --- |
| SchemaWorkspacePage | pages/schema-workspace, ui/model | Exact selection, read models/ETags, редактор, координация action/result/refresh | Переиспользовать F02; добавить обработку Publish result | F02; §4–8 |
| PublishSchemaVersion | features/publish-schema-version, model/api; ui по необходимости | Доступность Publish, один bodyless запрос, pending/result/typed failure | Адаптировать current page orchestration | §3–9 |
| SchemaVersionRail | widgets/schema-version-rail, ui | Группы Draft/Published и exact выбор | Переиспользовать F02; обновление после Publish | F02 |
| SchemaEditor | widgets/schema-editor, ui/model | Tree/inspector и переход к issue | Переиспользовать F01 в saved Draft режиме F02 | F01/F02; §4, §6 |
| Schema/version/field presentation | entities: entity-schema, schema-version, schema-field | Generated read DTO, статус/metadata/read-only contract | Переиспользовать F01/F02 | §4, §6 |
| Generated client/headers/errors/controls | shared, технические сегменты | HTTP/ETag/envelope, общая доступность и показ ошибок | Переиспользовать общую infrastructure; расширить generation по target contract | §5, §8–9 |

| Блок | Входы | Выходы | Владелец состояния / lifetime | Зависимости и использование |
| --- | --- | --- | --- | --- |
| PublishSchemaVersion | Exact schemaId/versionId; own Draft ETag; loaded Draft, dirty/blockers и current context | Pending; Published DTO + ETag; различимый отказ/неподтверждённый исход | Feature: только текущий запуск/ошибки; captured IDs/ETag до ответа | Generated client, generic transport/error mapping; SC01/SC02 |
| SchemaWorkspacePage: локальная настройка F03 | Результат action, выбранный context, read queries F02 | Selected Published view; invalidation/refresh rail и logical schema context; errors → editor | Page: selected DTO/ETag и состояние чтения; до смены context | Публичные границы action/editor/rail/query; SC01–SC03 |

Publish feature не хранит editable дерево и не владеет route. Карточки общих rail/editor остаются в F01/F02; F03 задаёт только pending/diagnostic/result настройки. Не требуется отдельная feature на каждый read, отдельный слой screens или новый C# project ради этих обозначений.

## 4. Логическая компоновка

~~~text
SC01/SC02. SchemaWorkspacePage [F02 page]
  Breadcrumb + logical Schema header [F02]
  SchemaVersionRail [F02 widget]
    Draft и Published history, selected exact version
  Выбранный saved Draft
    Title / ChangeComment [F02]
    Baseline/current Published/stale banner [F02]
    SchemaEditor [F01 widget, saved-field режим F02]
      Tree: поля/вложенные definitions, selection, issue markers
      Inspector: свойства selected field/definition, inline issues
    PublishIssues summary: переход к node/свойству либо общая проблема
    Save [F02] / PublishSchemaVersion [F03] / contextual overflow
    Publish pending / отказ / неподтверждённый исход
  Актуализация основы / новый Draft от current Published [F02, по явному действию]

SC03. Та же selected area после success
  Published metadata: VersionNumber, Title, ChangeComment, PublishedAt
  Historical schema Name/Key contract snapshot
  Read-only структура и свойства [entities presentation, F01/F02]
  Lifecycle badges; technical details с ContentHash при наличии
  Создать Draft на основе этой версии [F02]
~~~

| Связь частей | Какие сведения передаются | Кто координирует | Наблюдаемый результат |
| --- | --- | --- | --- |
| Page → Publish feature | Exact IDs, loaded own ETag, clean/status/blockers | Page + feature | Действие объясняет недоступность; отправляется только выбранный saved Draft |
| Feature → page → editor/action | Pending/failure и diagnostics | Page | Повтор/изменение отправляемого owner blocked; errors видны в контексте |
| Summary → editor | Адрес проблемного field/definition/setting из server contract | Page через editor boundary F01 | Раскрытие ancestors, выбор node и focus к проблеме; неизвестный path остаётся summary |
| Feature → page → selected area | Returned exact Published DTO/new ETag | Page | Тот же ID, immutable view, server-assigned metadata |
| Page → rail/header/sibling Draft context | Invalidation и отдельные reads F02 | Page/query client | Published history/latest pointer обновлены; stale других Draft вычислен сервером |
| Stale refusal → F02 actions | Loaded baseline/current Published/diff | Page | Explicit accept-base либо новый Draft; сохранённый content не merge |

UI-вложенность не вводит code dependency feature на editor или соседнюю feature. Read refresh заголовка/rail может завершиться позднее mutation: confirmed Published result остаётся показан, ошибка refresh видна в своей области.

## 5. Зависимости и публичные границы

SchemaWorkspacePage использует PublishSchemaVersion через exact context и result/errors; feature вызывает generated API и возвращает outcome. Только page применяет DTO/ETag к selection и запускает refresh read models. Feature не импортирует page/widget и не вызывает Save/accept-base/create-Draft. F02 продолжения получают точную подтверждённую Published identity через page.

SchemaEditor принимает diagnostics и переход к field/path через публичную границу F01/F02. Для Publish он не сериализует editor input и не вычисляет strict evolution. Presentation helpers не дублируют server DTO; shared transport хранит headers рядом с generated result, без business-полей в новой FormModel.

Browser использует NSwag-generated clients/models напрямую; Domain/EF-сборки не импортируются. Общие правила [S06](../standards/S06-api-client.md) применяются к generation/headers, [S05](../standards/S05-ui-and-forms.md) — к состояниям действий. Точные C# signatures будущих блоков не объявляются готовыми.

## 6. Данные и владение состоянием

| Данные / состояние | Источник истины | Клиентский владелец / lifetime | Обновление / сброс |
| --- | --- | --- | --- |
| Logical Schema metadata/latest pointer и rail summaries | F01/F02 server reads | Workspace по schemaId | Вход/read refresh/confirmed mutation; token схемы не используется для Publish |
| Exact selected Draft/Published DTO и own ETag | F02 exact GET; F03 success response/header | Workspace по exact versionId, ETag отдельно | Select/explicit reload/success; поздний ответ старого context не подменяет новый |
| Draft input/dirty/selection | F02 generated update request и editor UI-state | Page/editor по F02 | Publish input не меняет; failure сохраняет context; success → read-only returned DTO |
| Publish pending/captured IDs/ETag/errors | Текущий запуск операции | Publish feature до завершения | Новый explicit запуск после восстановления условий; не глобальный store |
| PublishIssues/base/current Published/diff | Server read/error contract | Workspace; diagnostics текущего loaded state/action | Новые diagnostics/read; точный wire/mapping — Q15 |
| Returned Published metadata | Server response | Selected read model page | Применение целиком; номера/hash/time локально не дополняются |

Strong ETag хранится рядом с generated моделью в generic API/UI-state и относится к **выбранной version**, не к logical schema, rail summary, Published baseline или другому Draft. UI не выводит его из VersionNumber/hash. Version/ExpectedVersion/Revision не вводятся в DTO, query либо publish body.

SC03 использует returned VersionNumber/Title/ChangeComment/PublishedAt/historical schema Name-Key/lifecycle. Technical details могут показать server ContentHash **с возвращённым server profile (cm-schema-content/1 для Demo)**, SHA-256 exact canonical UTF-8 как64 lowercase hex без prefix; UI не назначает/пересчитывает bytes/hash. Semantic schema content excludes authoring labels/metadata по [Q19 v1](../../backend/questions/Q19-content-hashes.md#принятый-canonical-profile-v1), а Registry cm-json-schema/1 отдельный. Immutable bytes/digest/profile сохранены вместе; historical rename/profile evolution не rehash-ят старый contract. Neutral models/library определены один раз backend F03, DTO не копируются на frontend.

Полный состав type properties/immutable field identity берётся из F01, saved-field ограничения из F02/Q34; второй список DTO и форма Publish не нужны.

MVP presentation использует NSwag-generated typed definitions из [модели backend F01 §2.1](../../backend/features/F01-create-entity-schema.md#21-aggregates-entities-и-value-objects). Min > max, negative length/count, MultipleOf ≤ 0, несовместимый constraint, invalid comparer/path блокируют весь Publish; server повторяет guards даже после успешного Save. Default допускает только отсутствие comparer, Ascending/Descending — supported comparer; ObjectFieldPath требует required Object chain/required supported scalar leaf без List переходов. Изменение constraints само по себе не требует новой field identity; Key/Type/container identity и monotonic Required/Deprecated продолжают проверяться. Summary адресует definition/Item/setting/path, а action feature не хранит вторую editable модель и не исправляет definitions.

После успеха SC03 показывает замороженные constraints/comparer и DateTime TemporalSemantics обычными read-only свойствами exact definition. По [Q34 DateTime MVP](../../../business/questions/Q34-schema-field-semantics.md#datetime-mvp-temporal-semantics-и-совместимость) Naive/Offset — immutable semantic identity, не переключатель при Publish. Новый MVP profile set совместно включает Q35 constraints и Offset: schema-content, instance-content, JSON Schema и C# profiles; old `/1` bytes/history не переписываются. UI показывает profile+digest из server response без profile picker, клиентского rehash или предположения, что MVP уже имеет готовый `/2` codec. Equivalent Offset Minimum/Maximum сравниваются и канонизируются по UTC instant; их разные original offsets не меняют semantic schema hash. Значения экземпляров и их Publish UTC projection принадлежат [F04](F04-entity-instances.md#62-представление-и-значения), codec — [backend F04](../../backend/features/F04-entity-instances.md), model — [backend F01 §2.1](../../backend/features/F01-create-entity-schema.md#21-aggregates-entities-и-value-objects). Exact whole new-profile codecs/extension carriers/literal golden bytes остаются engineering Q19/Q31; temporal semantics уже согласованы. Group/inventory/manifest profiles меняются только при изменении собственного codec/layout/semantics, existing content-profile identifiers сохраняют границу вложенного содержимого.

Строгие structural/evolution проверки выполняет сервер: saved SchemaFieldId сохраняет Key/Type/type semantic identity; Required только true→false, Deprecated false→true, Deprecated optional; removed field может отсутствовать, new field имеет новую identity, все definitions publish-valid. Enum/Flag option identity и recursive definitions следуют [Q34](../../../business/questions/Q34-schema-field-semantics.md). UI показывает результат этих проверок, не копирует серверный validator.

В последующей интеграции Published view показывает `DeliveryMode` у AssetReference leaf/ItemDefinition read-only рядом с AssetKind. Publish фиксирует выбранный Draft mode; unknown/inapplicable value отображается как server issue с переходом к F01/F02 inspector. Historical versions без metadata имеют effective OnDemand без rehash; UI не пытается PATCH Published definition. Сам frozen runtime AssetReference из пяти полей не расширяется mode. Приёмка проверяет Draft→Publish→exact read, nested leaf и immutable историю. Versioned carrier/profile и mode-only FieldId compatibility — [Q37](../questions/Q37-launcher-asset-delivery.md); launcher readiness не становится условием публикации схемы или Demo.

## 7. Действия и клиентские потоки

| Действие | Начальное состояние / запуск | Исполнитель | Обработка / серверная операция | Результат в интерфейсе |
| --- | --- | --- | --- | --- |
| UI01. Получить publish context | Exact workspace либо выбор rail F02 | Page/query client | Header/rail/exact reads по §8; own version ETag | Saved content/status/base/issues доступны; availability пересчитана |
| UI02. Опубликовать | SC01 clean Draft, own ETag, нет известных blockers | PublishSchemaVersion | Capture IDs/ETag → pending → один POST F03/DF01 | Success → UI05; failure → SC02/§9; dirty не вызывает Save |
| UI03. Открыть проблему | SC02 summary/tree marker | Page + SchemaEditor | Локальный переход к server address, без mutation | Tree/inspector показывают проблему, context сохранён |
| UI04. Разрешить stale-base | SC02 domain stale-base | Page → F02 public actions | Base/current/diff → explicit accept-base либо создание Draft от current Published | F02 результат; content не merge; Publish не повторяется автоматически |
| UI05. Применить Publish result | 200 DTO + ETag для текущего captured context | Page | Selected state := returned Published; rail/header reads invalidated/refreshed | SC03 той же identity, read-only, server number/time; siblings имеют актуальный stale context |
| UI06. Перечитать после отказа/неизвестного исхода | Явный Reload/Reopen/check по §9 | Page/query client | Exact GET F02; dirty guard F02 при наличии input | Подтверждённое saved state/ETag; без повторной записи/merge |
| UI07. Продолжить authoring | SC03 «Создать черновик на основе версии» | Page → F02 copy action | Exact Published source → обычный F02 flow | Новый separate Draft, Published прежний |

Pending исключает double click и competing mutation выбранного owner; editor input этого owner блокируется. Смена выбранной version/уход обрабатываются workspace lifetime/guard F02. Старый Publish response не подменяет новый context и не выполняет позднюю навигацию. Отмена ожидания браузером не доказывает отмену server commit.

После concurrent Publish другого Draft этот Draft может получить stale-base даже с валидным own ETag: сервер выбирает одного победителя. UI не повышает номер проигравшего, не меняет его Status на Published и не публикует его автоматически как следующую version.

## 8. Использование API и NSwag

| Потребность / UI-поток | Серверная фича / операция | Входные сведения | Используемый результат | Владелец вызова |
| --- | --- | --- | --- | --- |
| UI01/UI05 logical header | Backend F01 exact schema GET | SchemaId | Metadata/latest Published pointer/context | Page/query client F02 |
| UI01/UI05 rail | [F02/DF01](../../backend/features/F02-create-schema-version.md), GET /api/v1/entity-schemas/{schemaId}/versions | SchemaId/list contract F02 | Generated version summaries/history | Page/query client F02 |
| UI01/UI06 exact context | F02/DF02, GET /api/v1/entity-schemas/{schemaId}/versions/{versionId} | Exact IDs | Version/content/base/issues и own ETag; прочие diagnostics Q15 | Page/query client F02 |
| UI02 Publish | [F03/DF01](../../backend/features/F03-publish-schema-version.md), POST /api/v1/entity-schemas/{schemaId}/versions/{versionId}/publish | Exact IDs; If-Match из own loaded Draft ETag; **без body** | **200 ApiResponse<SchemaVersionDetailsDto> + новый ETag** | PublishSchemaVersion |
| UI04/UI07 | F02 accept-current-base/copy | Контекст F02, его generated requests/own preconditions | Результат независимого действия F02 | F02 feature |

Используется ContentManager.ApiClient.Generated.IEntitySchemaVersionClient, существующий src/ContentManager.ApiClient/nswag.json → OpenApi/Generated/ContentManagerApiClients.g.cs и подключение ConfigureApi/AddContentManagerApiClients с ApiEnvelopeDelegatingHandler. Generated method определяется актуальным backend OpenAPI, не выводится из старого PublishAsync signature.

Generated models используются напрямую; generated classes вручную не редактируются. Общая generation/transport infrastructure должна передавать per-request If-Match и отдавать успешный ETag рядом с DTO. Если current signature не предоставляет headers, исправляется общая infrastructure по S06, не добавляется ручная DTO-копия или скрытый exact GET для получения результата. Business/diagnostic сведения, которых нет в контракте, добавляются через server DTO/OpenAPI и regeneration; конкретный wire — [Q15](../../backend/questions/Q15-api-and-source-code.md).

Publish не отправляет VersionNumber, ContentHash, PublishedAt, BasePublishedVersionId, technical Version/ExpectedVersion или editor content. Returned exact DTO/new ETag сразу формируют SC03. Refresh rail/logical schema/latest context — отдельные чтения, не замена полного Publish response. Другие Draft не получают подставленный ETag/baseline; их stale state приходит из server reads, включая перечитывание при следующем выборе.

412 concurrency.precondition_failed означает конфликт изменения exact resource;409 schema-draft.base-outdated — stale Published baseline;428 — пропущенный обязательный header. Demo strict structural refusal —400 schema-publish.structure-invalid, evolution refusal —409 schema-publish.evolution-invalid: issues содержат code/message/address, errors — dictionary address → messages. Generic ProblemDetails transport передаёт errors в summary/editor. Текущий сервер может сообщить первый strict отказ по общей области content; полный field/definition/option mapping остаётся детализацией Q15. Stale diagnostic basis/current Published/diff перечитываются через существующий exact GET отдельно от own ETag. UI не распознаёт причины по тексту ошибки и не придумывает preview endpoint. Unknown path остаётся видимой общей проблемой.

## 9. Состояния, ошибки и доступ

| Ситуация | Что видит пользователь | Доступные действия | Данные / восстановление |
| --- | --- | --- | --- |
| Exact read loading/error/нет версии | Состояние selected area по F02; отсутствие отличается от GET failure | Retry GET/rail/back при доступном context | Publish disabled без exact Draft/ETag; нет fallback на другую version |
| Dirty либо отсутствует own ETag | Причина недоступности Publish | Explicit Save по F02 либо exact read | Скрытого Save/использования schema ETag нет |
| Known stale-base/blockers | Banner либо summary+inline diagnostics | Исправить/Save; UI04 при stale-base | UI availability не гарантирует успех серверной проверки |
| Publish pending | Ожидание выбранного action | Другие разрешённые переходы по F02; owner mutations/input blocked | Captured exact context; один запрос |
| Confirmed success | Exact Published read-only, number/time/metadata, краткое confirmation | UI07/rail | Returned DTO/new ETag; dirty reset, без local numbering |
| Strict validation refusal | Summary + tree marker + inspector/metadata issue; общий blocker отдельно | UI03, ручное исправление и Save F02 | Draft/context/input сохранены, Published result не создан; Demo codes — §8, расширенная адресация — Q15 |
| 412 Precondition Failed | Persistent «Версия на сервере изменена» | Explicit Reload/Reopen/UI06, с dirty guard F02 | Нет auto-retry с новым ETag/merge; failure не подменяется stale-base |
| 409 schema-draft.base-outdated | Baseline/current Published, semantic diff F02 | Explicit UI04; Save stale Draft остаётся разрешён | Draft остаётся Draft, номер не назначается клиентом |
| 409 invalid-state/lifecycle/parent-state blocker | Предметная причина по structured server error | Explicit read/разрешённое contextual действие F02 | Не считается412 по одному слову concurrency; Archived/Withdrawn owner не публикуется. Parent metadata/lifecycle conflict не подменяется schema ETag; окончательные code/category — Q15 |
| 428 Precondition Required | Безопасная integration error | Сохранить context, исправить client/transport | Defect: нормальный flow обязан отправлять If-Match; не пользовательское правило обхода |
| 404 после запуска | Selected owner/version отсутствует | Explicit read/rail/back | False success/fallback отсутствуют |
| Timeout/network/уход после отправки | «Результат публикации не подтверждён» | Explicit check exact saved state UI06 | Нет blind retry/обещания rollback; read может подтвердить Published той же identity |
| Ошибка rail/header refresh после success | Published уже подтверждён; отдельная ошибка обновления read context | Retry соответствующих reads | Publish не повторяется; confirmed DTO не теряется |
| 403, если возвращён сервером | Отличимый отказ, без false success | Вернуться/сохранить context | Demo login flow не добавляется; MVP access F16 отдельно |

[Q15](../../backend/questions/Q15-api-and-source-code.md) определяет окончательное восстановление при lost response и адресацию строгих diagnostics; неизвестный DTO/status не заполняется догадкой. По [S05](../standards/S05-ui-and-forms.md)/[S07](../standards/S07-accessibility.md) действие и переход к issue доступны с клавиатуры, disabled reason/status/error понятны без цвета, summary переводит focus к адресуемой проблеме. F02 guard/dialog возвращает focus; новый обязательный dialog здесь не вводится.

## 10. Проверки и приёмка

Критерии по [S08](../standards/S08-testing.md) описывают target, не результаты тестов реализации. Тестовую библиотеку документ не выбирает. Подменённый API проверяет клиентскую координацию; strict validation/атомарность/concurrent winner требуют real backend.

| Проверяемый блок / сценарий | Исходные условия / воздействие | Наблюдаемый результат | Граница проверки |
| --- | --- | --- | --- |
| Publish availability | Clean saved Draft/dirty/Published/no own ETag/known blockers | Правильное enabled/reason; dirty click не Save, нет обязательного тяжёлого modal | Feature/page с API double |
| UI02 transport | Exact Draft с ETag A, у schema/другого Draft ETag B | Один bodyless POST exact route, If-Match=A; ни metadata/content/Version в body, ни подставленного B | Generated transport/request capture |
| UI05 result | 200 exact Published DTO/new ETag с server number/time/snapshot | Тот же versionId read-only, metadata из response; UI07 exact source | Page/action с API double |
| Refresh consistency | Успех, другой sibling Draft, delayed/failed rail/header reads | Server latest/history обновлены; sibling stale контекст перечитан; success сохранён при read failure | Page/query double; stale invariant real backend |
| 412 / stale-base409 | Stale own ETag либо другой Draft уже Published | Разные persistent outcomes; exact Reload explicit; stale UI04 без merge/automatic Publish | Feature/page double; real HTTP concurrency |
| Strict diagnostics | Ошибки поля/nested definition/option/metadata и неизвестный path | Summary видим, адресуемая проблема открывает inspector/focus; unknown остаётся summary | Editor/page после Q15 contract |
| MVP definition validation | Invalid typed bounds/step/collection/recursive Item/comparer/path после saved Draft | Publish целиком blocked; setting доступен через summary→общий inspector; snapshot не исправляется | Page/API double для diagnostics; real backend для strict recheck |
| MVP immutable result | Valid typed definitions → Publish → Copy/change constraints в новом Draft | Старый Published readonly/profile+digest сохранены; constraint-only change не создаёт новый FieldId | UI/generated integration + real backend history/new-profile fixtures |
| MVP DateTime identity/hash | Historical Naive, new Offset, equivalent instant bounds с разными offsets; попытка semantic switch same ID | Old `/1` immutable; fixed semantics видны read-only, invalid identity Publish blocked; equivalent bounds same schema hash | Page/diagnostic doubles + real backend/new-profile golden fixtures; критерий, не результат запуска |
| Concurrent Publish | Два Draft одной baseline | Один success; loser Draft/stale-base, номер не назначен локально и no retry | Real backend + UI/API integration |
| Repeat/network/context | Double click/timeout/сразу выбор другой version/уход | Один pending, unknown outcome, старый ответ не подменяет selection/route | Feature/page/lifetime doubles |
| Immutable continuation | Publish, затем создание Draft/rename logical Schema | Новый separate Draft; old Published contract/snapshot прежний | Composition; immutable data real backend |
| Errors/access | 404/428/lifecycle blockers/403 | Отличимые ошибки, сохранённый context, no false success/login/moderation Demo | Transport/page |
| Общий editor/доступность | Ошибка Publish в saved F02, create-режим F01, keyboard | Mapping не ломает других consumers; focus/readonly/disabled reason корректны | Widget/consumers/browser |

E2E реального контура подтверждает saved Draft → Publish → exact Published response/read → новый Draft; отдельный реальный concurrent сценарий отличает412 от stale-base409. До решения Q15 неизвестный error/path profile не выбирается в expected results. Проверка Markdown/ссылок не подтверждает работу приложения.

## 11. Последовательность изменений и открытые вопросы

Сначала materialize backend F03 diagnostics/accepted cm-schema-content/1 canonical kernel/integrity metadata; затем OpenAPI/NSwag/per-request headers. Переиспользуются workspace/read/editor/public F02 actions, Publish и returned DTO. Проверяются availability/errors/context/rail refresh/publish concurrency и canonical golden fixtures; UI profile picker/client hash recomputation не добавляются.

| Вопрос | Какие детали блокирует |
| --- | --- |
| [Q15: F03 contract](../../backend/questions/Q15-api-and-source-code.md#f03-детализация-целевого-контракта) | Расширенная strict diagnostics address FieldId/definition/option/metadata. Demo bodyless POST/200 SchemaVersionDetailsDto/own ETag,412/428/stale409, strict structure400/evolution409 и errors/issue carriers materialized. Diagnostic basis отдельно от own ETag; unknown outcome проверяется explicit exact GET без replay |
| [Q19 canonical v1](../../backend/questions/Q19-content-hashes.md#принятый-canonical-profile-v1) | Six profiles/bytes/numeric/Unicode/SHA rules selected; exact neutral CLR kernel/carriers/golden fixtures engineering, no UI hash choice/recompute |
| [Q35](../../../business/questions/Q35-value-constraints.md) / [Q34 DateTime MVP](../../../business/questions/Q34-schema-field-semantics.md#datetime-mvp-temporal-semantics-и-совместимость) | Typed constraints/comparer, strict Publish и Offset temporal semantics согласованы; exact whole new-profile codec/carriers/literal golden bytes — engineering Q19/Q31, `/1` history неизменяем |

Реализация Demo UI F03 от 08.10.2026:

| Источник | Поведение Demo | Граница |
| --- | --- | --- |
| [EntitySchemaPage.razor](../../../../contentmanager.ui/src/ContentManager.Pages/Components/ContentModeling/EntitySchemas/EntitySchemaPage.razor), [page orchestration](../../../../contentmanager.ui/src/ContentManager.Pages/Components/ContentModeling/EntitySchemas/EntitySchemaPage.razor.cs) | Publish рядом с Save; clean saved Draft/own ETag/stale/lifecycle/blockers/pending guard с объяснением. Exact route сохраняется; returned Published DTO применяется сразу; header/rail обновляются отдельно | Нет скрытого Save, обязательного confirmation, local numbering/hash или latest substitution |
| [SchemaPublishFlow](../../../../contentmanager.ui/src/ContentManager.Features/SchemaWorkspace/SchemaPublishFlow.cs) | Один captured schemaId/versionId/ETag; проверка returned owner/status/ETag; pending,412, machine-code stale409, structured errors и unknown outcome | Нет replay; неподтверждённый исход проверяется explicit exact GET. Publication failure не создаёт фиктивный dirty input |
| [Generated IEntitySchemaVersionClient](../../../../contentmanager.ui/src/ContentManager.ApiClient/OpenApi/Generated/ContentManagerApiClients.g.cs), [metadata scope](../../../../contentmanager.ui/src/ContentManager.ApiClient/Infrastructure/Responses/ApiResponseMetadataScope.cs) | Bodyless Publish с If-Match выбранного Draft,200 SchemaVersionDetailsDto; собственный ETag захватывается для конкретного запроса | Клиент генерируется configured .run/script из фактического OpenAPI; generated source не редактируется вручную |
| [Saved editor](../../../../contentmanager.ui/src/ContentManager.Widgets/Components/ContentModeling/EntitySchemas/UI/SchemaSavedDefinitionEditor.razor) | PublishIssues/error summary адресуют saved FieldId и recursive Item; tree markers/inspector focus переиспользуют editor. Unknown address остаётся summary | Strict validation/evolution вычисляет сервер; UI не изменяет definition для обхода отказа |
| [F03Tests](../../../../contentmanager.ui/tests/ContentManager.Ui.Tests/F03Tests.cs), [F03RenderingTests](../../../../contentmanager.ui/tests/ContentManager.Ui.Tests/F03RenderingTests.cs) | Runner проверяет captured publication, точный immutable result, no replay, dirty gate, stale/token preservation, explicit unknown reconciliation, refresh failure и late context | Изолированные transport/render fixtures не подтверждают PostgreSQL races, canonical bytes или browser round-trip |

Stale refusal читает diagnostic basis/current Published/diff отдельно от выбранного own ETag и editable input. Новый diagnostic token не используется для автоматического повтора или актуализации. Unknown outcome exact read может подтвердить Published той же identity либо показать сохранённый Draft; failed read сохраняет блокировку и неподтверждённое состояние. Confirmed response и последующий read refresh имеют отдельные границы успеха.

Эта Demo реализация не включает MVP constraints/Offset constructor, login/roles/moderation или Product Publishing. Current code не заменяет бизнесовые F03/Q08/Q34; реальные concurrency/canonical/browser проверки принадлежат соответствующему backend/integration контуру.
