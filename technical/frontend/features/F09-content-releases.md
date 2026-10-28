# F09. Релизы назначенного контента: фронтенд

[Бизнес](../../../business/features/F09-content-releases.md) · [Бэкенд](../../backend/features/F09-content-releases.md) · [M04](../modules/M04-publishing.md) · [Q13](../questions/Q13-ui-scope.md)

Документ описывает целевой MVP UI Content Manager: .NET 10, Blazor Standalone, MudBlazor, FSD и NSwag. Будущие блоки — логические обозначения, не существующие классы. Факты текущего UI отделены от цели в §11; вёрстка не задаётся.

## 1. Результат и границы

Специалист фиксирует сохранённый Product/Variant assignment как immutable release, читает историю и exact snapshot, выводит release из нового использования через Retire и возвращает через Restore. Snapshot хранит exact version IDs, ChangeComment и created metadata; изменение assignment его не меняет. Create не меняет source assignment и его ETag.

ProductRelease — общий зафиксированный набор. VariantRelease — **«Локальные отличия, не итоговый состав»**: additions, explicit overrides `A → B`, schema exclusions и instance exclusions. Target/result сохраняют stable identity, exact result version и captured schema context. PreparedAgainstProductContentReleaseId — optional provenance, не hard pin и не обещание совместимости. CreateVariantRelease требует явный validation context: exact immutable ProductRelease либо сознательно выбранный EmptyBase/variant-only. Current active base и live Product assignment не подставляются; missing/read failure/невыбранная база не означают EmptyBase. Semantic review и Create относятся к одному saved patch и тому же выбранному context. Create снова проверяет basis, текущую необходимую source/result eligibility и collision в этом context. VariantRelease хранит только patch; provenance не становится hard dependency. Повторное использование с другой базой разрешено через полную validation [F10](F10-publication-revisions.md). Названия context/basis обозначают логическую модель, точные carrier types/fields остаются Q15.

Assignment Save — [F07](F07-product-content-assignment.md)/[F08](F08-variant-content-assignment.md); current Product base, promotion Manual/Automatic, activation и fanout — [F11](F11-publication-lifecycle.md). F09 не хранит Product mode, не активирует публикацию, не редактирует content release и не вводит общий Release CRUD. Restore меняет lifecycle availability, не создаёт/активирует revision. Retired остаётся в owner history, не в глобальном Archive. Historical revisions/Delivery не меняются от Retire/Restore. Current active Product base Retire блокируется сервером F11.

Это полный Publishing MVP; Demo не требует releases, VR School и полного Publishing. Applied context/blockers поступают из [F06](F06-product-catalog.md)/[F15](F15-catalog-conflicts.md). Новый release с Retired InstanceVersion недопустим, existing history сохраняется по [Q09](../../../business/questions/Q09-editing-and-deletion.md#entityinstanceversion). Unchanged unavailable reference может быть сохранена в assignment, но блокирует новый release; Saved не равен CanCreateRelease. Live collision F08 даёт diagnostics для live basis и не определяет пригодность другого явно выбранного release context. Intrinsic invalid patch блокирует Save и release.

Пустой ProductRelease и полностью пустой VariantRelease разрешены как явно создаваемые source snapshots. Exclusion-only patch отдельно виден как непустой набор правил. F09 не требует непустого итогового состава; F10 требует хотя бы один explicit source и непустой final result. UI не создаёт фиктивный empty release для отсутствующего source. Намеренный повтор Create из того же сохранённого assignment, с одинаковым content/Comment, создаёт новый artifact с новым ID/ReleaseNumber; content hash не служит dedup identity.

Archived owner либо Archived parent ограничивает Create независимо от diagnostic live collision: создать новый release нельзя. Read/history сохраняются; F07/F08 limited maintenance и отдельный corrective repair F15 не являются release и не обходят этот guard. Archive не выполняет Retire/Unpublish/Withdrawal и не меняет historical Delivery по существующему entitlement. Restore source Active разрешает новый workflow только после own/parent/current blocker checks. Live collision остаётся scoped к выбранному review basis, а не blanket запретом открыть F09.

## 2. Страницы, экраны и навигация

| Страница / экран | Точка входа и контекст | Состав и режим | Завершение / переход |
| --- | --- | --- | --- |
| SC01. Product Workspace, «Релизы» | `/products/{productId}/releases`; F06, вкладка/deep link | Read-only Product, F15 blocker, saved source summary, история и создание | Created release показан exact; owner прежний |
| SC02. Variant Workspace, F09-секция «Публикация» | `/products/{productId}/variants/{variantId}/publishing`; F06/F08/deep link | Applied Variant/parent, saved patch, история и создание; F10/F11 рядом | Создание patch-only VariantRelease не запускает F10/F11 |
| SC03. Подготовка release | «Создать релиз из текущего состава» SC01/SC02 | Saved source; для Variant явный exact ProductRelease или EmptyBase; semantic review/diff и его basis; ChangeComment, Create/Cancel | Success возвращает DTO/own ETag; Cancel без mutation |
| SC04. Exact release | История либо confirmed create | Read-only content/четыре patch groups, metadata/provenance/lifecycle, Retire/Restore | Возврат к истории либо confirmed lifecycle result |

SC03/SC04 — режимы F09 workspace. Новый route/dialog не вводится ради названия экрана. Exact selection deep link/refresh и сохранение выбора **уточняются** в Q13. Общий workspace описан F06; здесь его F09-секции. Direct entry читает exact owner без предварительного списка. Missing owner/assignment не заменяется первым доступным или synthetic empty state.

Variant canonical route следует applied parent F06. Pending move сохраняет прежний applied Product/F15 blocker. После confirmed safe move применяется новый context, dirty patch того же Variant сохраняется по F08. Старый POST автоматически не повторяется с новым parent; parent error не подменяется own assignment 412 (mapping Q15).

Unsaved assignment edits перед созданием требуют **явного Save** F07/F08. После confirmed Save используются returned DTO и новый assignment ETag; summary/diff должны соответствовать saved baseline и явно выбранному validation context; patch change требует нового review, смена context также аннулирует прежний review. Failed/unknown Save outcome не запускает create. Переходы учитывают dirty guard content-вкладки; autosave/offline/browser storage нет. Comment при failed create сохраняется; Cancel/уход с Comment — Q13.

## 3. Карта FSD-блоков

### 3.1. Необходимые уровни

Page координирует owner/applied context и соседние сценарии. Один release widget используется с Product либо Variant context и владеет историей, подготовкой и exact selection. Самостоятельные features — create и lifecycle action. Entities предоставляют read-only views/reads. Shared содержит metadata/controls/errors; feature на каждый GET, новый global store и дополнительные слои не нужны. `app` подключает принятые workspace sections.

### 3.2. Состав

| Блок | Слой / логическое размещение | Ответственность | Использование | Основное описание |
| --- | --- | --- | --- | --- |
| ReleasesSection / ReleaseSection | pages / product-workspace, variant-workspace / ui, model | Owner/applied parent, ограничения, coordination F07/F08/F10/F11 | SC01/SC02 | §3.3; [F06](F06-product-catalog.md#3-карта-fsd-блоков) |
| ContentReleases | widgets / content-releases / ui, model | History, saved-source preparation, diff, exact selection | Общая F09 композиция | §3.3 |
| CreateContentRelease | features / create-content-release / model, api | Один source-guarded POST | SC03 | §3.3 |
| ChangeReleaseLifecycle | features / change-release-lifecycle / model, api | Retire/Restore с own release ETag | SC04 | §3.3 |
| ContentReleaseView / release reads | entities / content-release / ui, api | Read-only snapshot/metadata/lifecycle/provenance; history/exact reads | Product/Variant views без копии DTO | §3.3 |
| Assignment public state/Save; ContentReferenceView | widgets/features F07/F08; entities / entity-instance | Saved source, explicit Save, exact references | Переиспользовать public boundaries | [F07](F07-product-content-assignment.md#3-карта-fsd-блоков), [F08](F08-variant-content-assignment.md#3-карта-fsd-блоков) |
| CatalogRecordView / F15 banner; controls/API/errors | entities/widgets F06/F15; shared / ui, api | Applied context, blocker, технические состояния | Переиспользовать | [F06](F06-product-catalog.md), [F15](F15-catalog-conflicts.md), [S06](../standards/S06-api-client.md) |

### 3.3. Ответственность и публичные интерфейсы

| Блок | Назначение / входы | Выходы и получатель | Собственное состояние | Данные, зависимости и состав | Жизненный цикл |
| --- | --- | --- | --- | --- | --- |
| ReleasesSection / ReleaseSection | ProductId либо VariantId + applied ProductId/restrictions | Context → widget; explicit Save intent → F07/F08; selected release intent → F10/F11 | Catalog read/route/section context | Public F06/F15; widget; assignment public state/Save callbacks | Вход читает owner; переход учитывает dirty; late response не применяется новому context |
| ContentReleases | Owner, saved generated assignment DTO + ETag, dirty/pending/restrictions | Create/lifecycle result → views/history; navigation intent → page | Generated list/exact/source/review models и create request; selected validation context/review basis/read/error/freshness UI-state; own release ETag рядом с DTO | Assignment public result; release reads/view; Create/Lifecycle callbacks; shared; diff Q15 | Owner change очищает selection/read; source/validation context change invalidates review; success только captured context |
| CreateContentRelease | Captured owner/applied parent, source ETag, generated request с ClientOperationId/Comment/validation context/review basis | Original execution receipt + created identity/metadata либо distinct refusal → widget | Один intent: generated original request, original source If-Match/context/basis/actor, pending/unknown/error; без editable content copy | DF01/DF02/generated client/shared per-call headers; execution contract Q15; не вызывает Save/F11 | Double click блокируется; lost outcome сохраняет тот же ID/input; изменение intent требует нового ID |
| ChangeReleaseLifecycle | Exact ID/type, own ETag, Retire/Restore, restrictions | Confirmed generated DTO/new ETag/error → widget | Pending/captured ID/action/token/error | DF07/DF08/generated client/shared; без Create internals | Late result не заменяет другой selection; read после 412 отдельно от нового action |
| ContentReleaseView / reads | Generated Product/Variant DTO либо owner/release IDs для read | Read DTO/metadata → widget; reference navigation intent → page | Нет mutable business-copy; раскрытие групп — UI-state | DF03–DF06; ContentReferenceView/shared read-only controls | Обновление по входам; list/exact различаются; no latest substitution |

Переиспользуемые карточки не дублируются. Generated names/editability определяет OpenAPI/NSwag. Product/Variant могут иметь разные generated DTO: общий widget не требует искусственной объединённой business-модели.

## 4. Логическая компоновка

### Review разрешённого состава схем

Release review показывает дерево Schema → exact Instances с fixed SchemaVersion, policy источника, включениями/исключениями, count всего scope и диагностикой. Для AllExcept пояснение: «Учтены опубликованные экземпляры на момент этой проверки». Новые Published instances/versions внутри target могут изменить будущий release, но не existing release. SelectedOnly отображает pins. Parent count не выводится из текущей страницы детей.

Review widget владеет generated resolved preview/basis и captured assignment/context, Create feature передаёт именно этот reviewed basis вместе с обычными preconditions. При changed publication universe server refusal сохраняет selection, снимает readiness и требует повторного review/подтверждения; UI не показывает прежний «готов» по одному assignment ETag. Missing/failed children read не трактуется как zero. Variant review явно показывает logical schema replacement-mask и final count относительно exact ProductRelease/EmptyBase.

После Create exact release view читает frozen tree/materialization: live изменение rules или новая публикация не обновляет его rows/count. Редактирование выбора ведёт в assignment с созданием нового release. bUnit проверяет changed basis/review reset, policy labels и frozen read; API/concurrency acceptance — [backend F09](../../backend/features/F09-content-releases.md#разрешение-schema-selection-и-captured-release-basis). Новые DTO/tree routes остаются [Q39](../../../business/questions/Q39-schema-content-selection.md), не выдумываются на фронте.

```text
SC01. ProductWorkspace: ReleasesSection [pages; ProductId/context]
  CatalogRecordView(Product) + вкладки [общий workspace F06]
  F15 banner [при blocker]
  ContentReleases(Product) [widgets; history/source/selection]
    Saved assignment summary [public F07 state]
    Создать релиз из текущего состава → SC03
    Owner release history [ContentReleaseView summaries]
    Выбранный exact ProductRelease → SC04
    Read/loading/empty/error [shared]

SC02. VariantWorkspace: ReleaseSection [pages; VariantId/applied ProductId]
  CatalogRecordView(Variant + applied Product) + вкладки [F06]
  F15 banner [при blocker]
  ContentReleases(Variant) [widgets; history/patch source/selection]
    «Локальные отличия, не итоговый состав»
    Saved local patch summary + SaveBlockers/ReleaseBlockers/Warnings [public F08 state; diagnostic basis указан]
    Создать релиз из текущего состава → SC03
    Owner release history [ContentReleaseView summaries]
    Выбранный exact VariantRelease → SC04
  Соседние F10/F11 sections [их описание; не вызываются Create]

SC03. Подготовка [режим ContentReleases; captured saved source]
  Owner/source summary + explicit Save при dirty
  Variant validation context [владелец widget; initial selection отсутствует]
    Явный exact immutable ProductRelease [public F09 history/exact reads]
    Либо сознательный EmptyBase / Только локальные отличия [explicit choice, не fallback]
  Semantic review/diff Added / Changed version / Removed + basis [DF09; saved patch + выбранный context]
  Variant: schema selections и четыре legacy-группы + A → B/captured schema context
  ChangeComment + validation context/review basis [generated request carrier Q15; без Title]
  CreateContentRelease / Cancel [features/shared]
  Source stale / validation / unknown outcome [shared]

SC04. Exact release [режим ContentReleases; release DTO/own ETag]
  ContentReleaseView [entities; ID/created/comment/lifecycle]
    Product: immutable Items с exact versions
    Variant: additions / overrides / schema exclusions / instance exclusions
      PreparedAgainstProductContentReleaseId [optional provenance]
  Current Product base badge [public F11 state, если применим]
  ChangeReleaseLifecycle [features; Retire/Restore]
  Exact read / lifecycle error / pending [shared]
```

| Связь | Сведения | Координатор | Эффект |
| --- | --- | --- | --- |
| Page → widget | Owner/applied parent/restrictions | Page | History/preparation/exact в одном context |
| F07/F08 confirmed Save → preparation | Returned DTO + new source ETag | Page/widget | Старый diff недействителен; подготовка из saved source |
| Variant context selection → review | Exact ProductRelease DTO/ID либо explicit EmptyBase; saved patch/basis | Widget | Новый review именно этой пары; no implicit active/live base |
| Preparation → Create → widget | Comment/source If-Match + выбранный context/review basis; release DTO/own ETag | Widget | Server revalidation; exact patch snapshot/history обновлены; source token прежний |
| History → exact read | Release ID/type → DTO/own ETag | Widget | Lifecycle action с exact token |
| Lifecycle → views | Confirmed DTO/new ETag | Widget | Content прежний; lifecycle/history согласованы |

SC03 не подменяет saved source unsaved patch. Live diagnostics F08 сохраняют метку своего basis и не блокируют открытие SC03/context selection только из-за live collision. Widget отдельно показывает diagnostics/CanCreateRelease выбранной пары saved patch + exact ProductRelease/EmptyBase; Create confirm использует их и остальные intrinsic/access/new-use/F15 guards. Неизвестный или failed selected-context review не разрешает Create. SC04 read-only по content. UI-вложенность не создаёт dependencies features друг от друга и доступа к internal stores.

## 5. Зависимости и публичные границы

Page передаёт context/restrictions и public результат чтения/Save F07/F08. Widget вызывает public release reads, передаёт generated request/token Create и принимает result callback. Create не сохраняет assignment за пользователя и не вызывает Lifecycle/F11. Context selection переиспользует public ProductRelease history/exact reads F09; current F11 base badge не источник default selection. Source/comment/context/review basis принадлежат widget; Create получает captured inputs и не меняет их самостоятельно. Transport context/basis уточняется Q15, новый preview endpoint не назначается. Lifecycle получает exact ID/token от widget, возвращает result и не читает internals истории. Entities сообщают read/navigation result владельцу.

Save source и create release — разные explicit действия, координируемые page/widget. F10/F11 получают selected release ID через public navigation/input; дальнейшие commands принадлежат им. Current base badge не становится mutable Product mode.

Shared не знает правил Publishing. Browser не зависит от server Domain/EF, foreign DbContext или VR School API. Public API в C# — публичные components/parameters/callbacks/read services и закрытые internal детали срезов; `index.ts` неприменим. Общие headers/guards проверяются с F07/F08 и другими workspace потребителями.

## 6. Данные и владение состоянием

### 6.1. Источник истины и область жизни

| Данные / состояние | Источник истины | Клиентский владелец и жизнь | Обновление / сброс |
| --- | --- | --- | --- |
| Owner/applied parent/restrictions | F06/F15 exact read | Page, generated models | Вход/read/context change; pending parent не применяется |
| Saved source | F07/F08 exact read либо confirmed Save DTO | Assignment owner; widget получает public baseline/preparation | Read/Save; unsaved request не snapshot source |
| Assignment ETag | Header source read/Save | Generic API/UI-state рядом с DTO | New source result; не release/list/preview/F11 token |
| History | DF03/DF04 | Widget, generated list; owner lifetime | Read/refresh/confirmed mutation; pagination Q15 |
| Exact snapshot/lifecycle | DF05/DF06 либо confirmed mutation result | Widget, generated DTO | Selection/read/create/lifecycle result |
| Release ETag | Header exact read/create/lifecycle | Generic API/UI-state рядом с exact DTO | Только тот же release; list не источник token |
| Comment | Generated create request | Widget в SC03 | Ввод; сохранить при failure/412; Cancel/уход Q13 |
| Variant validation context | Явное решение пользователя + authoritative exact ProductRelease read либо explicit EmptyBase | Widget; generated types после Q15, UI selection-state | Initial none; context change invalidates review; failure/missing не EmptyBase |
| Review/captured source/basis | DF09: saved patch + выбранный context + exact predecessor последнего created same-owner ReleaseNumber, включая Retired; первого predecessor нет | Widget; generated result/basis, freshness UI-state отдельно | Patch/context change invalidates review; новая history row сама по себе не заменяет показанный predecessor; current guards повторяются при Create |
| Create intent | UI создаёт GUID ClientOperationId для одного подтверждаемого намерения; generated execution contract Q15 | Create feature хранит exact original input/source If-Match/context/review/Comment/actor до выяснения исхода | Same ID/input возвращает original receipt; другой payload означает новое намерение/new ID, без blind new ID после timeout |
| Pending/sent context/errors/selection | Чистое UI-state | Widget/feature по §3.3 | Send/result/owner change; late result проверяется по context |

Generated models используются напрямую для чтения/edit Comment request. Mutable ReleaseViewModel/FormModel и ручная копия patch groups не создаются. Submitted request, saved baseline и confirmed release различаются. Frontend не читает/вычисляет internal Version и не добавляет его в DTO. Source и release имеют отдельные strong numeric ETag по backend F09; UI передаёт полученный header без вычисления token. Нет нового global store/browser persistence.

### 6.2. Состав представления и формы

| Сведение | Источник | Использование | Ограничение |
| --- | --- | --- | --- |
| Owner Name/IDs/applied parent | F06 read | Context/breadcrumb | Read-only; route identity stable IDs |
| Release ID/created metadata | Read/result | Identity/история/details-copy | Не вводить ReleaseNumber/Title; legacy Number не target |
| ChangeComment? | Generated request/release DTO | Комментарий create; далее read-only | Optional; length/normalization/field errors Q15 |
| Variant validation context/review basis | Generated logical contract после Q15 + explicit UI choice | Exact ProductRelease либо EmptyBase и показ basis review | Нет default active/live/fallback; carrier fields/types не назначаются |
| Product Items | Snapshot DTO | Counts/schema groups/exact references | Не current assignment |
| Variant schema selections и четыре legacy-группы | Snapshot DTO | Правила/A → B с captured schema | Exclusions inherited-only; B сохраняет свою identity; не flattened итог |
| PreparedAgainstProductContentReleaseId? | Snapshot DTO | Optional audit context/exact link | Не compatibility lock/hard dependency; EmptyBase и historical missing provenance различаются от невыбранного context Create |
| Available/Retired/retired metadata | DTO | Badge/lifecycle action | Content immutable; historical refs сохраняются |
| Current active Product base | Public F11 state | Badge/причина недоступности Retire | Сервер перепроверяет при действии |
| Semantic changes | DF09 | Added/Changed version/Removed до create; exact previous release ID/ReleaseNumber либо отсутствие первого predecessor | Последний created same-owner release, включая Retired; после подтверждения baseline не переключается на новую history row; wire carrier Q15 |

Metadata берётся из authoritative read, не вычисляется по Key. Недостающая metadata не стирает exact snapshot IDs. Нехватка fields/editability/diagnostics решается server DTO/OpenAPI + regeneration.

## 7. Действия и клиентские потоки

| Действие | Начальное состояние / запуск | Исполнитель | Обработка / запрос | Результат |
| --- | --- | --- | --- | --- |
| UI01. Открыть историю | SC01/SC02, exact owner прочитан | Page/widget | DF03/DF04 + F06/F15; source для подготовки отдельно | Owner history либо distinct empty/read failure |
| UI02. Подготовить release | Создать из текущего состава | Widget | Dirty → explicit F07/F08 Save; saved source/ETag; Variant явно выбирает exact ProductRelease либо EmptyBase; DF09 на той же паре | SC03 без mutation release; failed Save/read/review не продолжает create |
| UI02a. Выбрать/сменить Variant context | SC03, saved patch | Widget | Public history/exact ProductRelease read либо explicit EmptyBase; сброс прежнего review; DF09 | Review basis и результат этой пары; active/live база не подставляются |
| UI03. Ввести Comment / Cancel | SC03 | Widget | Generated request/input; локально, без HTTP | Cancel без snapshot; Comment на Cancel/уход Q13 |
| UI04. Создать snapshot | SC03, saved source/ETag и valid review basis; Variant context выбран явно | Create feature | Один ClientOperationId → capture original IDs/source If-Match/Comment/context/predecessor/review basis/actor → DF01/DF02 | Original receipt созданного artifact; patch-only exact snapshot/history update; no hidden result GET |
| UI05. Открыть exact | Строка истории/выбранный ID | Release reads/widget | DF05/DF06; list token не используется | SC04 DTO + own ETag; source не меняется |
| UI06. Retire / Restore | SC04, own release ETag | Lifecycle feature | Capture ID/action/token → DF07/DF08 | Confirmed DTO/new ETag; content/history/Delivery прежние |
| UI07. Восстановить после 412 | Stale source либо release | Widget/page | Сохранить ввод; explicit exact read соответствующего owner | Новый baseline осознанно принят; mutation только новым действием |
| UI08. Перейти к revision/promotion | Confirmed selected release | Page | Public navigation/input F10/F11 | Их сценарий; F09 не активирует и не выполняет fanout |

Pending блокирует повторный click той же записи. Captured context не меняется в полёте; late response не подменяет другой owner/selection. Cancel/уход после send не доказывает отсутствие commit. Lost create response оставляет unknown intent с original ClientOperationId/input/If-Match/context/review/Comment/actor. Выяснение/повтор по [execution contract Q15](../../backend/questions/Q15-api-and-source-code.md#publishing-command-execution-и-clientoperationid) использует тот же ID и exact input; новый ID или fresh ETag не генерируется автоматически. Same ID/input после current authorization возвращает original receipt прежде новых source/lifecycle checks, даже если assignment уже изменился или release Retired. Receipt не подтверждает текущий lifecycle; для нового действия нужен exact read. Bound ID с изменённым input даёт отдельный409. Намеренный новый Create имеет новый ID и повторяет актуальные guards; два новых intent с одним unchanged assignment ETag могут создать два artifacts без повышения source token.

Source412 сохраняет Comment и edits F07/F08, требует explicit source read и новой подготовки. Нельзя просто прикрепить новый ETag к старому review. Изменившийся patch требует нового semantic review; context/source eligibility drift требует актуальной проверки basis, без автоматической подстановки базы. Lifecycle412 относится к exact release и требует отдельного read/нового решения. Нет blind merge/retry и автоматического old-parent POST/PUT. Поведение повторного Retire/Restore текущего lifecycle состояния не выбирается как обязательный no-op (§11).

## 8. Использование API и NSwag

Create/lifecycle используют выбранный MVP writer protocol [Q20](../../backend/questions/Q20-persistence-and-migration.md): source assignment set/version, выбранный validation context и current lifecycle/catalog/reference facts заново проверяются и защищены до owner commit. Semantic review не выдаёт commit permit; unchanged source token может оставаться действительным для повторного Create по принятой business policy, не превращая чтение source в его mutation. Own412 и foreign/context409 различимы; technical coordination/read failure не означает пустую базу, совместимость или новый CatalogConflict. Потерянный ответ сохраняет выбранную пару/Comment/token как unknown outcome; повтор возможен только по гарантиям Q15, без автоматической новой базы/token.

DF-ID соответствует backend F09; все операции ниже — целевой контракт, не current generated methods.

| Потребность / UI-поток | Backend F09 операция | Входы | Используемый результат | Владелец |
| --- | --- | --- | --- | --- |
| DF01. UI04 Product create | `POST /api/v1/products/{productId}/content-assignment/release` | Required GUID ClientOperationId в body; ChangeComment?; exact F07 source If-Match; captured predecessor/review basis carrier Q15 | Original execution receipt/created identity; success metadata shape Q15 | Create feature |
| DF02. UI04 Variant create | `POST /api/v1/products/{productId}/variants/{variantId}/content-assignment/release` | Required GUID ClientOperationId в body; applied IDs; ChangeComment?; own F08 source If-Match; explicit ProductRelease либо EmptyBase и captured predecessor/review basis | Original execution receipt/created Variant identity; wire Q15 | Create feature |
| DF03. UI01 Product history | `GET /api/v1/products/{productId}/content-releases` | ProductId; query shape Q15 | List DTO/counts/lifecycle/created; не own row ETag | Release reads → widget |
| DF04. UI01 Variant history | `GET /api/v1/products/{productId}/variants/{variantId}/content-releases` | Applied IDs; query shape Q15 | Variant list/provenance | Release reads → widget |
| DF05. UI05 Product exact | `GET /api/v1/product-content-releases/{releaseId}` | ReleaseId | Snapshot/lifecycle DTO + own ETag | Release reads → widget |
| DF06. UI05 Variant exact | `GET /api/v1/product-variant-content-releases/{releaseId}` | ReleaseId | Schema selections и четыре legacy-группы/provenance/lifecycle DTO + own ETag | Release reads → widget |
| DF07. UI06 Product lifecycle | `POST /api/v1/product-content-releases/{id}/retire` либо `.../{id}/restore` | Own release If-Match; выбранное действие | Confirmed DTO/new ETag; active-base409 для Retire | Lifecycle feature |
| DF08. UI06 Variant lifecycle | `POST /api/v1/product-variant-content-releases/{id}/retire` либо `.../{id}/restore` | Own release If-Match; выбранное действие | Confirmed DTO/new ETag | Lifecycle feature |
| DF09. UI02 semantic review | Saved assignment → previous release; для Variant validation saved patch + explicit selected context; external contract **уточняется** Q15 | Source/baseline; exact immutable ProductRelease либо explicit EmptyBase; necessary inputs | Summary/diff, addressable diagnostics и review basis той же пары | Widget через выбранный public contract; URL/method/carrier не выдумываются |
| Public F07/F08 read/Save | Их exact assignment GET/whole-set PUT | Owner/generated request/own assignment If-Match | Returned DTO/new source ETag | Их public owners; page координирует |

Exact read/mutation хранится как generated DTO + generic transport ETag рядом. Create защищает source assignment, lifecycle — release owner. Assignment ETag не повышается от create; release ETag не берётся из assignment/F11/list. History refresh допустим отдельно и не заменяет returned result скрытым exact GET. DF09 может использовать F07/F08 exact и DF05/DF06 read после согласования, но browser comparison не становится authority snapshot validation; нынешний F10 preview его не заменяет. UI передаёт explicit validation context и review basis DF02 через выбранный Q15 carrier; payload не считается уже выбранным Comment-only контрактом. Own assignment ETag отдельно от diagnostic/review basis: неизменный token не доказывает неизменность необходимых source eligibility/collision inputs. Create revalidation не создаёт dependency release на provenance.

Основа — `ContentManager.ApiClient.Generated`, [nswag.json](../../../../contentmanager.ui/src/ContentManager.ApiClient/nswag.json), [DI](../../../../contentmanager.ui/src/ContentManager.ApiClient/DependencyInjection/ApiClientServiceCollectionExtensions.cs), [ConfigureApi](../../../../contentmanager.ui/src/ContentManager.ApiClient/DependencyInjection/ApiClientWebAssemblyHostBuilderExtensions.cs). Целевые method/type names определит OpenAPI/regeneration. Generated classes не редактируются вручную; request DTO используется напрямую. Init-accessors/edit support — Q15.

Current generated methods собирают headers внутри, но caller получает body envelope без success ETag; per-call If-Match в signature отсутствует. Требуется общая generation/transport infrastructure, отдающая response metadata и связывающая token с **конкретным запросом**. Shared HttpClient default header не хранит mutable token для параллельных owners. Typed responses/header exposure ещё не подтверждены; решение — [Q15](../../backend/questions/Q15-api-and-source-code.md#f09-release-contracts-и-semantic-review). [ApiEnvelopeDelegatingHandler](../../../../contentmanager.ui/src/ContentManager.ApiClient/Infrastructure/Http/ApiEnvelopeDelegatingHandler.cs) и errors проверяются с source412/release412/domain409/428. Наличие handler не доказывает готовность contract. Правила — [S06](../standards/S06-api-client.md).

## 9. Состояния, ошибки и доступ

| Ситуация | Что видит пользователь | Доступные действия | Данные / восстановление |
| --- | --- | --- | --- |
| Technical gate/read failure | Координация/проверка недоступна, не domain409/новый CatalogConflict | Сохранить ввод/context; safe read | Не empty valid result; lost write acknowledgement остаётся unknown по Q15, без blind retry/new token |
| History/source/exact loading | Индикатор affected области | Read-only context; write ждёт input/token | Late response не применяется новому owner |
| Empty history / первый release | Пустая история и подтверждённое отсутствие predecessor | Подготовка при допустимом saved source, включая explicit empty | Нет fabricated baseline; exact first-review representation Q15 |
| Source not ready/missing | Snapshot нельзя подготовить | Read retry/navigation | Нет synthetic source/ETag или create-on-GET |
| Dirty assignment | Требование сохранить изменения | Explicit Save или F07/F08 Discard/Stay | Create после confirmed Save; no autosave |
| Sending/success | Pending именно действия; успех после response | Повторный click блокируется | Generated result принят; create source unchanged |
| Validation/reference/domain409 | SaveBlockers/ReleaseBlockers/Warnings с basis, affected reference/group | Исправить source F07/F08 либо явно выбрать иной context и выполнить review | Comment/patch/context сохранены; no cleanup/dedup/latest |
| Variant context not selected/read failure | Create недоступен с понятной причиной | Explicit exact release selection/read retry либо deliberate EmptyBase | Missing/failed read не даёт valid empty preview |
| Review stale/failed | Basis/result неактуальны либо не получены | Новый review после patch/context change; explicit read/repair | Не CanCreateRelease=true и не пустой valid result; source ETag отдельный |
| Archived owner/parent | Причина недоступности Create, applied/source lifecycle отдельно | Read/history и переход к разрешённому maintenance | Create blocked; restore повторяет guards, no auto publication |
| F15 blocker/pending move | Persistent reason/applied context | История/read; affected mutations disabled; F15 переход | После resolution read; нет old-parent retry |
| Active Product base Retire409 | Нельзя вывести текущую базу из нового использования | F11 смена/clear base по его правилам | Нет implicit clear/activation; history прежняя |
| Source412 | Source assignment изменился | Explicit source read/новая подготовка | Comment/edit сохранены; новый token не для старого diff |
| Release412 | Exact lifecycle state изменён | Exact read/новое решение | Content прежний; no automatic retry |
| 428 | Не получен/передан required token | Исправление client flow/read | Не подставлять token другого owner |
| 404 / отказ доступа | Объект отсутствует/доступ отказан | Разрешённая navigation/read retry | Не fake empty/success; права Q13/F14 |
| Network failure/lost response | Read error либо unknown write outcome различимы | Safe read; reconciliation Q15; новый write explicit | Comment/sent context сохранены; cancellation не rollback |
| Retired snapshot | Badge, read-only content в owner history | Restore при разрешении, historical reads | Не новое использование F10; old revisions/Delivery сохранены |

Disabled actions поясняют UX, не заменяют server guards/access. По [S05](../../../catalog/docs/standards/ui/forms-and-accessibility.md#form-state-and-user-feedback)/[S07](../../../catalog/docs/standards/ui/forms-and-accessibility.md#access-and-semantics): явные подписи Create/Retire/Restore, различимые ошибки/unknown outcome, основные действия с клавиатуры, управление фокусом при открытии/закрытии режима. Layout/focus API не назначается. Comment Cancel/уход, lifecycle confirmation и navigation details — Q13.

## 10. Проверки и приёмка

| Проверяемый блок / сценарий | Исходные условия и воздействие | Наблюдаемый результат | Граница проверки |
| --- | --- | --- | --- |
| Protected commit / technical failure | Foreign lifecycle/catalog/source changes между preview и commit; bounded gate wait/cancellation/read failure; lost ACK | Server revalidates protected facts; own412/domain409/technical failure различимы; ввод/basis/token сохранены, no false success/empty valid/blind retry | UI/transport doubles для representation; real PostgreSQL owner writers/barriers для guard, recovery contract Q15 отдельно |
| SC01/SC02 composition | Owner/applied parent, empty/history/read failure | Правильная history; no global CRUD/fake source | Page/component с подменёнными public reads |
| UI02 explicit Save | Dirty F07/F08; Save success/failure/unknown | No auto-Save; success использует returned DTO/new ETag; отказ не creates release | Page/widget coordination с public doubles |
| UI04 Create | Product/Variant saved baseline, Comment; Variant explicit context и review basis | Required GUID ClientOperationId + original source If-Match/context/basis/Comment; один intent/POST, original receipt без hidden GET | API-client с HTTP double; execution store отдельно real |
| Empty snapshots | Empty Product Items; fully empty Variant schema selections и четыре legacy-группы; exclusion-only patch | Explicit release artifacts разрешены; пустой итог F10 запрещён отдельно; no fake release для none | UI preparation + real F09/F10 validation |
| Review predecessor | First release; latest-created Retired; новый release после confirmed review без source/context change | No first predecessor; exact captured same-owner latest-created baseline сохранён; later row alone не требует rebasing/review | UI basis + real protected source capture |
| Create execution | Lost ACK; intentional same-content/new ID; same ID после source edit/Retire; bound ID changed If-Match/Comment | Same ID/input original receipt; new intent/new artifact allowed; changed input409; no blind new ID/token/hash proof | Client intent recovery + real execution-store barriers |
| Immutable view | Assignment изменён после create | Product exact versions/Variant schema selections и четыре legacy-группы/captured schema прежние; no content edit | Component; реальный API отдельно |
| Variant patch-only | A→B, exclusions, optional provenance, отсутствующий live target | Explicit target/result identity/schema selections и четыре legacy-группы/label; provenance не hard pin | View на generated models |
| UI05/UI06 lifecycle | List без exact token; exact read; success/412/428 | Exact read перед action; own release If-Match/new ETag; no list/assignment token substitution | Widget/client boundary |
| Active-base409 | Retire current Product base | Понятный отказ; no implicit clear/activation; history прежняя | Component error boundary; backend отдельно |
| Source412/domain409 | Stale source либо reference/composition/catalog отказ | Comment/edit сохранены; explicit read/new decision; no automatic retry | Isolated flow + page coordination |
| Async/unknown outcome | Double click, context/selection/parent change, timeout после send | Один write; late result не затирает новый context; no duplicate/old-parent retry | Client/component с управляемыми ответами |
| Explicit Variant context | Active/live база есть; selection нет; selected ProductRelease read failed; EmptyBase выбран явно | No implicit active/live/fallbackempty; EmptyBase только deliberate; patch-only release | Widget/composition + real validation |
| Semantic preparation/basis | Saved patch + selected exact ProductRelease/EmptyBase; затем patch/context/source eligibility drift | Review именно этой пары; patch change требует new review; Create revalidates basis/necessary eligibility/collision | UI doubles + real server; carrier Q15 |
| Context collision/provenance | Live base collision, иной selected immutable context valid; тот же VariantRelease с другой базой F10 | Saved status независим; selected context fully validated; no hard dependency/provenance pin, F10 revalidates other base | UI + real composer/F10 |
| Retained unavailable source | Assignment Save allowed; exact source/result теперь unavailable | Visible Saved + ReleaseBlocker; Create blocked; historical snapshots прежние | UI + real new-use guards |
| Full F09 flow | Реальный UI/API/Publishing storage, собственные данные | Save → create → exact → Retire/Restore; source token независим; snapshots/history сохранены | E2E MVP, не mocks-chain; framework Q13 |

По [S08](../standards/S08-testing.md) это будущие проверки, не результаты запуска. Shared per-call headers проверяются на параллельных owners и F07/F08 потребителях. Неизвестные policy не получают случайный expected result. Приёмка описания: один владелец source/comment/selection/token, согласованные SC01–SC04, понятные результат и ошибка каждого действия.

Приёмка lifecycle guard: Archived owner/parent запрещает Create при любой выбранной базе; history и уже materialized Delivery/новые sessions по подтверждённому entitlement сохраняются. После Restore Create требует свежего own/parent/blocker context. Limited maintenance/repair не вызывает Create автоматически.

## 11. Последовательность изменений и открытые вопросы

Сначала закрепить server contract/diff/results/ETag и recovery детали. Затем переиспользовать F06–F08 public boundaries, обновить общую NSwag generation/transport и регенерировать модели, собрать минимальные release views/widget и две action features, подключить workspace sections, проверить flows. F10/F11 не переносятся в create. Source/generated code при этой аналитике не изменяются.

| Вопрос | Какие детали блокирует |
| --- | --- |
| [Q15: F09 contracts](../../backend/questions/Q15-api-and-source-code.md#f09-release-contracts-и-semantic-review) | Success status/envelope/headers и lifecycle no-op; list fields/query/order/filter/pagination; Comment constraints; semantic review/patch diff transport/freshness, logical validation context/review basis carrier/diagnostics fields/types; per-call headers/typed response NSwag; idempotency/reconciliation; parent errors |
| [Q13](../questions/Q13-ui-scope.md) | Exact selection deep link/refresh; Cancel/уход с Comment, lifecycle confirmation; доступ/roles, будущие UI-test libraries |
| [Q05: пустой/повторный snapshot](../../../business/questions/Q05-initial-publication.md#f09-пустой-snapshot-и-повторное-создание) | Empty/intentional repeated snapshots разрешены; captured predecessor latest-created same-owner, включая Retired, первого predecessor нет; shape Q15 |
| [Q15: execution](../../backend/questions/Q15-api-and-source-code.md#publishing-command-execution-и-clientoperationid) | ClientOperationId и exact original intent/replay приняты; execution receipt/error/headers/recovery representation уточняется |
| [Q07: release validation](../../../business/questions/Q07-exclusions-and-local-content.md#f09-live-collision-и-release-validation) | Explicit immutable ProductRelease/EmptyBase context, same-pair review/Create revalidation и patch-only/no hard dependency приняты; ссылка на основание |
| [Q20: races](../../backend/questions/Q20-persistence-and-migration.md#f09-snapshot-creation-и-lifecycle-races) | Protected source capture, cross-BC references/catalog guards, creation/lifecycle races, result representation; общий writer protocol выбран, UI не выдаёт review за commit permit |

Closed A→B/exclusions/provenance, empty/intentional repeated snapshot и captured predecessor semantics не переоткрываются. История, original execution receipt и current new-use guards различаются. Lifecycle Retire/Restore repeat/no-op profile и точные wire/recovery representations остаются Q15.

### Наблюдаемое состояние исходников и отличие от цели

| Источник | Факт текущей реализации | Отличие от target F09 |
| --- | --- | --- |
| [PublicationWorkspacePage.razor](../../../../contentmanager.ui/src/ContentManager.Pages/Components/Publishing/PublicationWorkspacePage.razor) | Routes `/publishing/products/{OwnerId:guid}` и `/publishing/variants/{OwnerId:guid}`; workspace совмещает assignments/policy/releases/revisions/activation. Create запрещён при Dirty, получает workspace Revision/UpdateMode, после ответа вызывает Load | Target contextual routes, Comment/semantic preparation/source ETag; result без hidden GET; F11 mode/fanout отдельно |
| Тот же page, history/actions | `_workspace.Releases` показывает Number/PublishedAt/Items count; нет exact release view, Retire/Restore; Product create обещает automatic variants update | No ReleaseNumber invention; exact lifecycle token; four-group patch/provenance/history |
| [PublicationEditorModels.cs](../../../../contentmanager.ui/src/ContentManager.Widgets/Components/Publishing/PublicationEditorModels.cs) | AssignmentEditRow вручную копирует fields; PublicationDiffRow.Compare сравнивает `(SchemaId,EntityInstanceId)`, ContentHash/SortOrder/Source/SchemaVersionId с active revision | Generated primary models; current diff — F10 revision comparison, не F09 source→previous release/four-group patch review |
| [Generated client](../../../../contentmanager.ui/src/ContentManager.ApiClient/OpenApi/Generated/ContentManagerApiClients.g.cs), IPublishingClient/ReleasesAsync/Releases2Async | Legacy POST `api/v1/publishing/products/{id}/releases` и `api/v1/publishing/variants/{id}/releases`; PublishReleaseRequest(ExpectedRevision:long,UpdateMode); success200 envelope Release + Updates. ReleaseDto: Id/Number/PublishedAt/Items/Exclusions | Новый backend/OpenAPI; Comment/source action + Variant context/review basis carrier Q15/own release ETag; exact/list/lifecycle operations/four groups; status не наследуется из legacy |
| [NSwag config](../../../../contentmanager.ui/src/ContentManager.ApiClient/nswag.json), [DI](../../../../contentmanager.ui/src/ContentManager.ApiClient/DependencyInjection/ApiClientServiceCollectionExtensions.cs), [ConfigureApi](../../../../contentmanager.ui/src/ContentManager.ApiClient/DependencyInjection/ApiClientWebAssemblyHostBuilderExtensions.cs) | Namespace/templates/init-accessors, scoped clients/configured HTTP; caller success возвращает body без headers, If-Match параметра нет | Per-call token/typed metadata требуют общего решения/regeneration; наличие client не target readiness |
| [WorkspacePageBase.cs](../../../../contentmanager.ui/src/ContentManager.Shareds/Components/Shared/Infrastructure/WorkspacePageBase.cs) | Busy/Dirty/lifetime cancellation, Discard-only guard; любой409 получает «Данные изменились на сервере» | Source412/release412/domain409/428/unknown outcome различаются; explicit Save/recovery без blind retry |

Source inspection подтверждает current→target различия, не реализацию target F09, готовность diff/read representation или выбранного writer protocol или прохождение UI/E2E. Проверка этой правки — структура/ссылки документации; тесты приложения не запускались.
