# F10. Materialized PublicationRevision: фронтенд

[Бизнес](../../../business/features/F10-publication-revisions.md) · [Бэкенд](../../backend/features/F10-publication-revisions.md) · [M04](../modules/M04-publishing.md) · [Q13](../questions/Q13-ui-scope.md)

Документ описывает целевой MVP UI Content Manager: .NET 10, Blazor Standalone, MudBlazor, FSD и NSwag. Будущие блоки — логические обозначения, не существующие классы. Факты текущего UI отделены от цели в §11; вёрстка не задаётся.

## 1. Результат и границы

Специалист явно выбирает exact Product/Variant releases, получает серверный materialized preview и создаёт immutable итоговую PublicationRevision одного Variant. Затем читает saved composition/history и управляет собственным lifecycle revision. Create не означает Approval, Activation или доступность клиентам. Revision — единственный обязательный moderation target MVP [F17](F17-content-review.md); Activate/Rollback/Unpublish, Publishing active и Delivery effective принадлежат [F11](F11-publication-lifecycle.md).

Product-only, Variant-only и оба source допустимы. Хотя бы один конкретный release обязателен; итоговая composition непустая. Отсутствующий source — отсутствие слоя, не latest/current base/current assignment. Фиктивный пустой VariantRelease не создаётся. Unsaved F07/F08 assignment не становится входом revision: для новых изменений нужны отдельные Save и [F09](F09-content-releases.md) release. Чтение existing releases не сбрасывает/сохраняет автоматически формы соседних секций.

Явно созданные empty F09 snapshots разрешены как источники, но empty final composition всё равно блокирует F10. Намеренный новый Create из той же exact release-пары/content/Comment разрешён и получает новый RevisionId; одинаковый hash не переносит Approval и не deduplicate-ит intent. Повтор неизвестного результата использует прежний ClientOperationId/input, а не намеренно новый artifact.

VariantRelease содержит additions, explicit overrides `A → B`, schema exclusions и instance exclusions. Override использует stable IDs/captured context одной logical EntitySchema; B сохраняет собственные EntityInstanceId/EntityInstanceVersionId, даже при отсутствии A в базе. Exclusions действуют только на inherited layer. Все inherited override targets подавляются до вставки local results: `A → B`, `B → C` не удаляют local B каскадно. Final EntityInstanceId уникален; collision блокирует revision без LWW/dedup/deep JSON merge. Optional PreparedAgainstProductContentReleaseId — provenance F09, не hard pin/выбор базы. Сервер выполняет composition по правилам [Q07](../../../business/questions/Q07-exclusions-and-local-content.md); браузер показывает итог/причины.

Saved content/source IDs/provenance/hash immutable. Historical exact read получает saved Items/metadata, без recomposition по текущей базе, retired sources или новому алгоритму. Available/Retired/Withdrawn — own lifecycle, отдельно от moderation/active/effective. Retire запрещает новые Activate/Rollback, сохраняя historical/frozen use; active Retire блокируется. Restore только Retired → Available, без approval/activation. Withdrawn terminal: дальнейшая серверная выдача включая Freeze запрещена, replacement отсутствует. Admin-read withdrawn history и distribution — разные политики [Q15](../../backend/questions/Q15-api-and-source-code.md#f10-revision-contracts-и-preview).

Withdrawal любой revision требует durable restriction propagation, включая inactive/frozen recipients. Для active revision backend дополнительно атомарно переводит Publishing active → null, добавляет историю Withdrawal/outbox. Один active-null факт не обеспечивает запрет произвольной historical revision. Transport/effectiveness — [Q17](../../backend/questions/Q17-publication-availability.md#f10-withdrawal-и-historical-distribution)/[Q20](../../backend/questions/Q20-persistence-and-migration.md#f10-composition-capture-и-withdrawal); browser не пишет в Delivery и не объявляет effective немедленно.

Applied owner/parent/blockers — [F06](F06-product-catalog.md)/[F15](F15-catalog-conflicts.md). Full Publishing/Moderation относится к MVP, не Demo. Не вводятся CRUD revision, browser composer, ContentSlot, revision number/title, global store.

Archived Variant либо parent запрещает новые revisions, обычную activation/rollback/automatic promotion, но сохраняет historical exact reads. Archive не выполняет Withdrawal/Unpublish и не запрещает уже materialized Delivery по ранее подтверждённому entitlement, включая новые sessions. Explicit safety Withdrawal проверяется отдельным lifecycle workflow по правам. Restore source Active не активирует revision, не снимает Withdrawal и не выдаёт grants; own/parent/other blockers перепроверяются.

## 2. Страницы, экраны и навигация

| Страница / экран | Вход / контекст | Состав / режим | Завершение |
| --- | --- | --- | --- |
| SC01. Variant Workspace, «Публикация» | `/products/{productId}/variants/{variantId}/publishing`; F06/F09/deep link | Applied Variant/parent, F15 blocker, Release → Revision → Moderation → Activation/Delivery, F10 preparation/history | Выбор source/exact revision в owner context |
| SC02. Подготовка revision | Явное действие SC01 | Exact source pickers/details, preview/schema groups/provenance/exclusions/issues, ChangeComment, Create/Cancel | Success → saved exact; Cancel без Create |
| SC03. Exact revision | History/confirmed Create/deep link moderation/activity | ID/sources/comments, saved Items/provenance/exclusions/hash/created/lifecycle; F17 status/link; F11 active/effective/history | Read-only content; lifecycle/F17/F11 action |
| SC04. Lifecycle confirmation | SC03 Retire/Restore/Withdrawal | Captured exact ID/lifecycle, последствия, reason по contract, Confirm/Cancel | Confirmed result → SC03; Cancel без mutation |

Это contextual section, не глобальный `/revisions`. Route identity — IDs, breadcrumb — Name/applied parent. Exact selection имеет deep-link ценность Q13; конкретный segment/encoding **уточняется**. Direct entry читает exact/authoritative owner, не выбирает первую строку. List order/filter/paging Q15; arbitrary page size не выбирается.

Applied move сохраняет identity Variant/несохранённую форму, но sources старого Product нельзя считать пригодными. Page разрешает applied parent/canonical URL F06; специалист явно пересматривает sources/preview. Pending move не applied. F15 блокирует affected writes; old-parent POST не повторяется автоматически. Cancel/уход с Comment/reason — Q13; silent discard/autosave не назначаются.

## 3. Карта FSD-блоков

### 3.1. Необходимые уровни

Page координирует route/context и F09/F10/F11/F17. Один widget объединяет preparation/history/exact; features выполняют Preview/Create и lifecycle; entities дают read-only views/reads; shared — generic API/headers/errors/controls. F-документ не равен одному feature slice. Отдельные stores/widgets для каждой группы не нужны.

### 3.2. Состав

| Блок | Слой / срез / сегменты | Ответственность / использование |
| --- | --- | --- |
| VariantWorkspace / PublicationSection | pages / variant-workspace / ui, model | Route/applied context/F15; SC01/coordination |
| PublicationRevisions | widgets / publication-revisions / ui, model | Sources/preparation/preview/history/exact; SC01–SC04 |
| PreparePublicationRevision | features / prepare-publication-revision / model, api | Preview/Create; SC02 |
| ChangeRevisionLifecycle | features / change-revision-lifecycle / model, api | Retire/Restore/Withdraw exact own token; SC04 |
| PublicationRevisionView / reads | entities / publication-revision / ui, api | Generated preview/exact/list views/read; SC02/SC03; public input для F17 |
| ContentReleaseView / reads | entities F09 / content-release / ui, api | Exact choice/details/availability/schema selections и четыре legacy-группы; [F09 §3](F09-content-releases.md#3-карта-fsd-блоков) |
| ContentReferenceView; CatalogRecordView/F15 banner | entities/widgets F04/F06/F15 | Exact versions/applied context/blocker; public reuse |
| API metadata/errors/confirmation/controls | shared / api, ui | Per-call headers/pending/errors/focus; [S05](../standards/S05-ui-and-forms.md)/[S06](../standards/S06-api-client.md) |

### 3.3. Ответственность и публичные интерфейсы

| Блок | Входы | Выходы / получатель | Состояние | Зависимости / область жизни |
| --- | --- | --- | --- | --- |
| Page/PublicationSection | VariantId/route ProductId; applied context/F15 restrictions | Context → widget; navigation → route; result → public F11/F17 refresh | Catalog/route/section; без composition copy | Public workspace/reads/banners/widget; вход/смена context; late old response rejected |
| PublicationRevisions | Applied owner/restrictions; public F11/F17 status/links | Generated requests → actions; results → views/history; navigation → page | Generated request/preview/list/exact; selection/context/dirty/pending/error UI-state; own ETag рядом с DTO | Release/revision reads/views/actions/shared; owner change resets; source change invalidates preview; same-Variant move preserves form with invalid context |
| PreparePublicationRevision | Captured applied IDs/exact sources/generated request/Comment; ClientOperationId для Create | Preview либо original Create receipt/result/error → widget | One intent: exact original request/context/Comment/actor, GUID ID и pending/unknown/error; no business-copy | DF01/DF02/shared; не Save/F09/F11/F17; Preview не создаёт intent записи; double click blocked; cancellation не rollback |
| ChangeRevisionLifecycle | Exact ID/own ETag/action/restrictions/generated reason request | Result/error → widget/page | Pending/captured ID/action/token/request/errors | DF05–DF07/shared; SC04 lifetime; late result не переносится new selection |
| Revision view/reads | Generated preview/exact либо IDs для read | DTO/read metadata → widget; navigation → page | Read-only input; раскрытие — UI-state | DF03/DF04/ContentReferenceView/shared; по входам, no recomposition/latest |

Generated names/editability задаёт OpenAPI/NSwag. Preview/exact режим принимает соответствующую generated model, без ручного union DTO или второй mutable business-модели.

## 4. Логическая компоновка

### Schema-first composition tree

Revision preview и exact view первыми показывают схемы, внутри — final exact instances с источником Product/Variant и target version. Replacement schema явно показывает «Собственный состав» и подавленную inherited группу; отдельный просмотр source intent объясняет AllExcept/SelectedOnly и exclusions, но не превращает их в live UI query. Product 20 + frozen Variant SelectedOnly 2 отображает final два.

Widget получает grouped projection из generated backend read result и хранит только expansion/paging/selection UI state. Source release IDs и review basis принадлежат existing Create flow; browser не перечисляет текущие instances схемы для самостоятельного recompute. Новая публикация после source release не появляется ни в preview, ни в historical revision. «Изменить выбор» открывает working assignment/новый release; immutable revision не редактируется чекбоксами. Пустая схема может быть видна в source audit, но не обходит непустой final guard.

bUnit проверяет hierarchy, count/source/version, 20→2 replacement и отсутствие запросов live membership при exact revision read. Real API отдельно проверяет frozen composition/canonical digest. Tree DTO/provenance codec и generated names — [Q39](../../../business/questions/Q39-schema-content-selection.md); расширение F14 и новый SDK runtime rule не вводятся.

```text
SC01. VariantWorkspace / PublicationSection [pages; applied context]
  CatalogRecordView(Variant + applied Product) / breadcrumb [F06]
  F15 blocker banner [affected writes]
  Release context [F09; «Локальные отличия, не итоговый состав»]
  PublicationRevisions [widgets]
    Подготовить revision → SC02
    Owner revision history / selected exact ID → SC03
    Loading / empty / read failure [shared]
  Moderation / Activation / Delivery effective [public F17/F11]

SC02. Подготовка [widget; exact sources]
  ProductRelease picker: exact Available либо «Без Product source» [F09]
  VariantRelease picker: exact Available либо «Без Variant source» [F09]
  Source summaries/comments/schema selections и legacy patch/optional provenance
  PreparePublicationRevision: Preview
  Server materialized preview [PublicationRevisionView]
    Итог по EntitySchema: exact SchemaVersion/InstanceVersion
    Provenance Product / Variant addition / Override A → B
    Collapsible excluded inherited summary
    Все issues / captured source context / outdated result
  ChangeComment [generated request; без Title]
  PreparePublicationRevision: Create / Cancel

SC03. Exact saved revision [widget; DTO + own ETag]
  PublicationRevisionView
    RevisionId / created / Comment / Available|Retired|Withdrawn
    Exact source IDs/details/comments
    Saved Items / provenance / exclusions / ContentHash [Q19]
  Moderation status / exact review link [F17]
  Publishing active / Delivery effective / activation-history [F11]
  Retire / Restore / advanced Withdrawal → SC04

SC04. Lifecycle confirmation [feature; captured exact ID]
  Identity / последствия / disabled reason
  Withdrawal reason [generated request; Q15]
  Confirm / Cancel / pending / domain/concurrency error [shared]
```

| Связь | Передача | Координатор | Эффект |
| --- | --- | --- | --- |
| Page → widget | Applied context/restrictions/F11/F17 input | Page | Один context |
| Selection → Preview → view | Exact IDs → server result/issues | Widget | Old result invalidated; current invalid блокирует Create |
| Create → exact/history | Confirmed identity/result | Widget/page | Saved selected/history refresh; no approval/activation claim |
| History → exact → lifecycle | ID → DTO/own ETag → captured action | Widget | List не row token; exact target |
| Lifecycle → views/F11/F17 | Confirmed result | Widget/page | Lifecycle/history/public reads обновлены; content прежний |

Подготовка показывает sources и итог одновременно; exact selection — saved read-only state. Confirmation не редактор composition. UI-вложенность не создаёт dependencies features друг от друга.

## 5. Зависимости и публичные границы

Page передаёт public parameters/context/restrictions и принимает navigation/result callbacks. Widget использует public reads и передаёт generated request/ID/token actions. Picker сообщает exact ID, не сохраняет assignment/release. View сообщает navigation intent, не вызывает commands самовольно. Prepare/Lifecycle независимы, координация в widget.

Lifecycle не вызывает F11 UI action для Withdrawal: active→null/history/outbox/restriction fact выполняет backend, page обновляет public F11 read. Browser не Delivery writer/fallback selector. Submission/review — public F17; Create не обещает queue side effect. Manual/Automatic orchestration F11/F17; trigger contract уточняется Q15/F17.

F17 переиспользует read-only view через generated exact input; entities не зависят от F17/features/pages. Shared не знает Publishing rules. C# public boundary — components/parameters/callbacks/read services, internal детали скрыты; `index.ts` не требуется. Browser не зависит от Domain/EF/DbContext/VR School API. Общие F09/headers/guards проверяются с другими потребителями.

## 6. Данные и владение состоянием

### 6.1. Источник истины и область жизни

| Данные | Источник истины | Клиентский владелец / жизнь | Обновление / сброс |
| --- | --- | --- | --- |
| Applied context/blockers | F06/F15 read | Page/workspace | Refresh/move; pending не applied |
| Releases/exact source metadata | F09 reads | Widget SC02/generated | Refresh; selected Retired видим/unavailable без replacement |
| Source IDs/Comment | Generated request/explicit input | Widget SC02 | Selection invalidates preview; Cancel/уход Q13 |
| Create intent | Generated request с required GUID ClientOperationId + exact original route/sources/Comment/actor | Prepare feature сохраняет один intent до выяснения outcome | Same ID/input replay original receipt; changed payload/new explicit action создаёт новый ID, unknown не blind new ID |
| Preview/issues | DF01 | Widget/generated + captured context UI-state | Source/context invalidates; late response ignored |
| History/selected exact | DF03/DF04 | Widget/owner context | Select/read/Create/Lifecycle/refresh; list не exact |
| Own ETag | Exact/create/lifecycle HTTP header | Widget рядом с generated DTO | Confirmed result/read; не foreign/list token |
| Reason/pending action | Generated request + UI-state | Lifecycle feature SC04 | Send/result; failure preserves; no token transfer |
| Moderation/active/effective | F17/F11 public read | Их владелец; page input | Refresh confirmed result; Available не effective |

Generated DTO/request используются напрямую read/edit. Нет RevisionViewModel/FormModel/EditorModel/business-copy patch/items. Отдельно только UI-state: loading/dirty/selection/раскрытие/captured context/pending/errors/ETag. Current form/submitted request/preview/confirmed revision различаются. Browser persistence/offline/autosave/global store не назначены.

### 6.2. Состав представления и формы

| Сведения | Источник / использование | Правило |
| --- | --- | --- |
| Names/IDs/applied parent | F06/context/breadcrumb | IDs route authority |
| ProductContentReleaseId?/ProductVariantContentReleaseId? | Generated request/exact F09 choice → Preview/Create | ≥1; null не latest; exact Available; backend guards |
| Source comments/schema selections и четыре legacy-группы/provenance | F09 DTO/read-only | PreparedAgainst optional, не hard pin |
| ChangeComment? | Generated Create request → saved DTO | Optional/no Title; limits Q15; не content identity |
| Preview Items/issues/exclusions/context | DF01/schema/provenance/все причины | Empty final invalid; browser не composer; shape/addressing Q15 |
| Saved Items/refs/metadata | Exact DTO/history/review | Saved authority, no recomposition |
| RevisionId/created/ContentHash+profile | Exact/create result/audit/details | cm-publication-manifest/1 runtime manifest digest/profile, не RevisionNumber/identity/approval; exact membership/versions/frozen aliases/hash references, values в immutable instance bytes |
| Lifecycle/metadata | Exact DTO/badge/actions | Content immutable; approval/active отдельно |
| Withdrawal reason | Generated request/confirmation/history | Обязательная содержательная Reason; limits/normalization/field addressing Q15 |

Недостающие labels не стирают IDs. Hash не exact identity/approval/access proof. Runtime manifest profile+digest исключает source provenance/override target/exclusions/release IDs/actor/comment/time/RevisionId по [Q19 v1](../../backend/questions/Q19-content-hashes.md#принятый-canonical-profile-v1); provenance остаётся separately saved evidence. Same runtime bytes получают same hash даже у разных artifacts, no dedup/Approval transfer. Values хранятся immutable instance canonical bytes, не пересериализуются из provenance/live sources. UI показывает server pair в details при необходимости, без profile picker/client rehash; exact DTO carriers OpenAPI+regeneration, no duplicate VM.

## 7. Действия и клиентские потоки

| Действие | Запуск / условия | Исполнитель | Обработка / API | Результат |
| --- | --- | --- | --- | --- |
| UI01. Открыть publishing/history | SC01/direct entry | Page/widget | Applied F06/F15 → DF03; F09 отдельно | History/empty/error; no first-row fallback |
| UI02. Выбрать sources | SC02 | Widget/F09 picker | Exact Available; смена/null локально + DF09 | Base-only/variant-only/both; old preview invalidated; neither Create unavailable |
| UI03. Preview | SC02, ≥1 source/context | Prepare feature | Capture IDs → DF01 → result/issues | Server result captured context; не authority будущего Create |
| UI04. Comment / Cancel | SC02 | Widget | Generated input/локально без mutation | Cancel не Create; lifetime Q13 |
| UI05. Create | SC02, current selection имеет допустимый preview | Prepare feature | Создать один ClientOperationId и capture exact original sources/route/Comment/actor → DF02; новый intent проходит fresh validation | Original receipt с created RevisionId/metadata, без hidden result GET; «Ревизия создана; модерация и активация — следующие отдельные этапы» |
| UI06. Exact read | History/deep link/created identity | Reads/widget | DF04; list не own ETag | Saved composition + own ETag; no recomposition |
| UI07. Retire / Restore | SC03→SC04/exact token | Lifecycle feature | Confirm → DF05/DF06 own If-Match | Confirmed lifecycle/token/read; Items прежние; Restore не activation |
| UI08. Withdrawal | Advanced SC03→SC04 | Lifecycle feature | Consequences/reason → DF07 own If-Match; DF08 backend | Withdrawn history; active atomic Publishing null; distribution/effective отдельно |
| UI09. После 412 | Stale own revision | Widget/feature | Preserve input/action; explicit DF04/compare | Новый token для нового явного action; no blind retry |
| UI10. Moderation/activation | Exact SC03 | Page/F17/F11 | Exact ID navigation/input; их guards/operations | Create/Available не Approval/effective |

Preview/Create имеют одинаковую полную server validation: ownership/applied parent, release lifecycle, patch/schema/collisions/nonempty, exact dependencies и F15. Create повторяет её и не доверяет preview. Sources content immutable, availability/metadata mutable; source Retire/guard/context change может отклонить Create. UI сохраняет IDs/Comment, не подставляет другой release. Mandatory source If-Match/workspace ExpectedRevision не выбран. Выбранный MVP writer protocol [Q20](../../backend/questions/Q20-persistence-and-migration.md) защищает fresh source/dependency facts до owner commit Create/lifecycle/Withdrawal; preview остаётся наблюдением, не permit. Own lifecycle412 отличается от foreign/domain409. Техническая недоступность gate/read не означает compatible source, пустой состав или новый CatalogConflict; lost commit confirmation остаётся unknown outcome по Q15 без слепого POST retry.

Current new-use checks применяются только к effective output instances и их schema/media closure. Suppressed inherited A или A-only asset не блокируют final B из-за Retire/Withdrawal/unavailability; dependency, требуемая самим B, проверяется строго, включая shared schema/asset. Source releases всё равно требуют Available/current owner, а retained target/exclusion evidence — trusted captured identity/schema, без требования текущей new-use eligibility evidence-only A. Retention шире runtime closure: source snapshots/provenance/evidence не удаляются. F17/F11/F13 используют тот же runtime dependency scope для approval/activation/distribution restrictions; historical read не recompute. UI показывает server diagnostics, не выводит безопасность из отсутствия A в строках preview.

Source change делает old preview неактуальным и не разрешает Create новой комбинации. Повтор Preview — read-only calculation, не retry записи. Pending Create/Lifecycle блокирует double click. Смена owner/selection/уход отменяет ожидание при поддержке transport, не rollback отправленного command. Old-context response не подменяет new selection; confirmed result сохраняет exact identity для read/history.

Timeout после Create сохраняет unknown intent с original ClientOperationId/exact input/context/Comment/actor. [Q15 execution](../../backend/questions/Q15-api-and-source-code.md#publishing-command-execution-и-clientoperationid) возвращает original receipt для same ID/input после current authorization, до checks нового исполнения; изменившийся source lifecycle либо уже Retired revision не превращают receipt replay в новый Create. Bound ID/different input даёт409. Intentional repeated Create требует нового явного ID и current guards; UI не генерирует новый ID и не меняет input для неизвестного результата. Receipt не утверждает current revision lifecycle, list/hash не доказывают исход попытки. Для unknown lifecycle explicit exact read показывает actual lifecycle; после active Withdrawal F11 history/state отдельно. Recovery и новое action различаются.

## 8. Использование API и NSwag

| Потребность | Backend / операция | Вход | Используемый результат / ограничение | Владелец |
| --- | --- | --- | --- | --- |
| DF01. Preview | `POST /api/v1/products/{productId}/variants/{variantId}/publication-revisions/preview` | Applied route + ProductContentReleaseId?/ProductVariantContentReleaseId? | Materialized result/issues/context; typed invalid vs error Q15 | Prepare feature |
| DF02. Create | `POST /api/v1/products/{productId}/variants/{variantId}/publication-revisions` | Required GUID ClientOperationId в body; те же exact sources + ChangeComment? | Original execution receipt/created identity/metadata; transport shape Q15; no hidden result GET | Prepare feature |
| DF03. History | `GET /api/v1/products/{productId}/variants/{variantId}/publication-revisions` | Applied owner; query/order/filter/paging Q15 | Summaries/selection; no row ETag | Reads/widget |
| DF04. Exact | `GET /api/v1/publication-revisions/{revisionId}` | Exact ID | Saved composition/metadata/lifecycle + own ETag | Reads/widget |
| DF05. Retire | `POST /api/v1/publication-revisions/{id}/retire` | Exact/own If-Match | Confirmed lifecycle; active Retire blocked | Lifecycle feature |
| DF06. Restore | `POST /api/v1/publication-revisions/{id}/restore` | Exact/own If-Match | Retired→Available; no approval/activation | Lifecycle feature |
| DF07. Withdraw | `POST /api/v1/publication-revisions/{id}/withdraw` | Exact/own If-Match; reason Q15 | Terminal Withdrawal; active history/null backend F11 | Lifecycle feature |
| DF08. Durable propagation | Publishing→Delivery; Q20/Q17 | Revision restriction; optional active-null | Ban inactive/frozen revision; optional effective null; contract/transport уточняется | Backend, не browser operation |
| DF09. Source choice/details | Public F09 Product/Variant lists/exact | Applied IDs/release IDs | Available exact sources/snapshot/provenance | Widget/F09 reads |

Public F17/F11 contracts дают moderation/active/effective/actions/history; page координирует их владельцев. DF01–DF07 routes приняты backend F10; DTO/type/method names/status/envelope/issues/withdrawn admin-read/lifecycle success/no-op — [Q15](../../backend/questions/Q15-api-and-source-code.md#f10-revision-contracts-и-preview). Preview не требует Comment, не сохраняет revision и не возвращает постоянный revision ID/own ETag как created artifact. Create не принимает browser Items/hash/preview token без решения; success обновляет exact view из returned DTO/own ETag без hidden GetWorkspace/result GET. Null encoding Q15; lifecycle/catalog/reference guards — [Q20](../../backend/questions/Q20-persistence-and-migration.md#f10-composition-capture-и-withdrawal).

Revision имеет собственный internal `int Version` для lifecycle. Exact GET возвращает strong numeric HTTP ETag; UI хранит/передаёт header рядом с generated DTO, не вычисляет Version/не добавляет token в DTO. List не row token; source/assignment/workspace/F11 token не own revision token. Active Withdrawal требует server-side согласованности active pointer, не обязательного второго browser F11 token без решения.

Переиспользуется NSwag infrastructure `ContentManager.ApiClient.Generated`/DI/ConfigureApi (§11). Shared transport предоставляет per-call If-Match/response ETag, без shared mutable default headers. OpenAPI/headers/init-accessors исправляются общей generation/transport + regeneration. Current IPublishingClient не готовый target. Lifecycle success применяет returned DTO/new token без hidden result GET; body/status profile Q15. Explicit exact read нужен для восстановления либо нового чтения, а не как скрытый обязательный шаг confirmed mutation. Confirmed запись без own token блокирует next lifecycle mutation до explicit exact read; command не повторяется.

## 9. Состояния, ошибки и доступ

| Ситуация | Что видно | Действия | Данные / восстановление |
| --- | --- | --- | --- |
| Technical gate/read failure | Координация/проверка недоступна, не domain409/новый CatalogConflict | Сохранить ввод/context; safe read | Не empty valid result; lost write acknowledgement остаётся unknown по Q15, без blind retry/new token |
| Loading/history empty | Ожидание либо «Ревизий ещё нет» | Разрешённая preparation/read/navigation | Empty не error/denied; refresh failure не стирает safe read |
| Нет sources | Отсутствующий слой/нужен ≥1 release | Exact choice/F09 отдельно | No latest/current assignment/fake release |
| Source Retired/unavailable | Exact ID/причина | Explicit choice/refresh | No auto-replace; Create blocked |
| Preview outdated/loading | Captured combination/актуальность | Edit/read/Preview | Old result не новые IDs; late response ignored |
| Invalid/collision/nonempty | Все refs/schema/provenance/причины | Sources либо assignment→F09 отдельно | Create blocked; no dedup/normalization |
| F15/pending move | Applied связь/affected actions | Read/history/resolution | Affected writes blocked; forms preserved; pending не applied |
| Parent mismatch/applied move | Context изменился | Applied read/canonical/explicit sources+preview | Code Q15; no old-parent POST retry |
| Historical/source Retired/base changed | Saved composition/provenance | Read/history/own lifecycle | No recomposition; new-use не automatic historical ban |
| Active Retire | Сначала сменить active либо Unpublish | F11/Cancel | Server перепроверяет |
| Retired / Withdrawn | Различимые badges/последствия | Restore только Retired/admin read по policy | Withdrawn не Restore/Activate/Rollback/Freeze; history retained |
| Withdrawal confirmation | Запрет серверной выдачи включая Freeze; active→null без fallback; local data не auto-delete | Confirm по contract/Cancel | Не Unpublish; reason preserved; Delivery не immediate claim |
| Moderation states | Exact status/action F17 | Разрешённый submit/review/F11 | Iteration withdraw не terminal revision Withdrawal; rejected content immutable |
| Domain/validation | Field/form/source/item причины | Исправить/explicit read | Не generic concurrency; Q15 addressing |
| 412 | Exact lifecycle changed | Read/compare/new action | Reason/action preserved; no auto token+retry |
| 428 | Missing precondition | Transport/exact recovery | User не вводит Version/ETag |
| 403 / 404 | Denied/missing различимы | Разрешённая navigation/read | Не empty/fallback |
| Timeout после send | Unknown Create/Lifecycle outcome | Explicit reconciliation/read | Current/submitted preserved; no false refusal/retry |
| Success/token/refresh failure | Запись confirmed/read неполон | Explicit read | No repeated command; next mutation ждёт ETag |

Backend guards permissions/lifecycle/ownership/dependencies/active/F15; disabled UX не authorization. Roles/permissions Q13/F17 не вводятся F10. По [S05](../standards/S05-ui-and-forms.md)/[S07](../standards/S07-accessibility.md) exact versions/A→B/provenance/exclusion scope/lifecycle/errors различимы без цвета; основные actions с клавиатуры, focus picker/confirmation возвращается к запуску. CSS/Razor/MudBlazor attributes не задаются.

## 10. Проверки и приёмка

Целевые критерии [S08](../standards/S08-testing.md), не результаты запуска UI. Test library не выбирается. Doubles — UI coordination/transport; materialization/lifecycle/Withdrawal/guards требуют real backend.

| Сценарий | Условия / воздействие | Наблюдаемый результат | Граница |
| --- | --- | --- | --- |
| Protected commit / technical failure | Foreign lifecycle/catalog/source changes между preview и commit; bounded gate wait/cancellation/read failure; lost ACK | Server revalidates protected facts; own412/domain409/technical failure различимы; ввод/basis/token сохранены, no false success/empty valid/blind retry | UI/transport doubles для representation; real PostgreSQL owner writers/barriers для guard, recovery contract Q15 отдельно |
| Direct/history | Exact deep link без list; late old owner | Applied/exact; no fallback/overwrite | Page/double + real ownership |
| Sources | Base-only/variant-only/both/neither/null | Explicit IDs/null слой; neither blocked/no fake | Widget/request + real validation |
| Dirty assignment | Unsaved F08/existing releases | No implicit Save/release; соседняя форма preserved | Workspace |
| Preview context | Source change/late response/Comment | Result tied IDs; old не разрешает new; Comment не identity | Widget/doubles |
| Patch/A→B | Exclusion+local/missing A/A→B+B→C | B identity; inherited-only/no local cascade | View + real composer |
| Collision/empty | Independent B/duplicate/cross-schema/empty | Все causes/Create blocked/server rejected/no LWW | UI + real composer |
| Provenance | PreparedAgainst absent/другая база same owner | Explicit/no hard pin/full validation | Widget + real compatibility |
| Preview/Create race | Preview→Retire/F15/dependency change | Server refusal/no preview authority; form preserved | Real guards/concurrency + UI |
| Effective closure | A suppressed/Withdrawn, A-only asset unavailable; B output with unavailable shared asset/Withdrawn schema; source release Retired | Evidence-only A не blocker; B closure и Available source строго проверены; retained evidence не удаляется, новый Media Withdrawal state не вводится | UI diagnostics + real F10/F17/F11/Delivery guards |
| Empty F09 sources | Explicit empty Product/Variant releases с empty либо nonempty итогом | Source artifacts допустимы; final empty rejected; ≥1 source/no fake release сохраняются | Source selection + real composer |
| Intent/replay | Same content/new IDs; lost ACK; same ID после Retire/source change; changed input под bound ID | Intentional new artifact; same ID/input original receipt; different input409; no blind new ID/hash adoption | Transport + real execution-store tests |
| Create boundary | Confirmed Create | Exact/history; no approval/activation/effective claim | Client + real API/E2E |
| Historical read | Source Retired/base/algorithm changed | Saved Items/hash/provenance/no recomposition | Real persistence/exact + UI |
| Own ETag | List/foreign token/412/428/two callers | Exact own If-Match; reason preserved/no retry/DTO token | Transport + real HTTP/concurrency |
| Retire/Restore | Active Retire/Retired Restore | Active blocked/content immutable/availability only | Real lifecycle + UI |
| Active Withdrawal | Own token/concurrent active pointer | Atomic revision/history/null/outbox/no fallback; effective отдельно | Real Publishing/Delivery + UI |
| Inactive/frozen Withdrawal | Frozen historical/admin read/recipient | History retained/durable distribution ban/no replacement | Real access/Delivery + UI; Q15 read |
| F17 boundary | Create/Rejected/Approved/iteration withdraw | Exact/no edit/no hash approval; iteration != lifecycle | F17/page + real workflow |
| Move/blocker | Pending/applied/old-parent/dirty | Applied/blocked/forms preserved/no retry | Page + real CatalogAccess |
| Recovery/lifetime | Double click/Cancel/уход/timeout/success-read failure | No duplicate/false failure; context/read/focus | Components/transport/browser; Q15 recovery |

MVP E2E: real releases→exact sources→Preview→Create→saved revision→F17/F11 отдельно→Retire/Restore/Withdrawal/history/distribution/effective. Mocks не доказывают capture/Delivery. Принятые effective-closure и intentional repeat/execution правила проверяются; неизвестные exact Q15/Q20/Q19 representations не выбираются в expected results молча. Приёмка описания: action связан с экраном/владельцем/API; state имеет одного владельца; history/new-use различены.

Приёмка archive/conflict guards: Create остаётся blocked при Archived own/parent и применимом Open F15; repair save/Resolve не создают revision. Historical immutable revisions сами по себе не blocker move; сохранённый состав/история не пересчитываются. Existing entitled Delivery/новая session продолжаются до отдельного запрета.

## 11. Последовательность изменений и открытые вопросы

Сначала backend contracts/capture/hash; затем OpenAPI/NSwag/shared headers. Переиспользуются F06/F09/F15 public context/reads/views, формируются generated request/preview/exact и две action features, подключаются history/details и public F17/F11 links/read refresh. Затем UI flows и real composer/lifecycle/withdrawal. Аналитика не изменяет source/generated/standards.

| Вопрос | Блокируемые детали |
| --- | --- |
| [Q15: contracts/preview](../../backend/questions/Q15-api-and-source-code.md#f10-revision-contracts-и-preview) | DTO/names/editability/issues/provenance/status/envelope/headers/list/paging/Comment/reason/domain/parent/no-op/result/admin read/recovery/idempotency; routes/own If-Match приняты |
| [Q20: capture/withdrawal](../../backend/questions/Q20-persistence-and-migration.md#f10-composition-capture-и-withdrawal) | Concurrent lifecycle/catalog/dependency guards/capture/freshness/active pointer/atomic F11 history/outbox |
| [Q19 canonical v1](../../backend/questions/Q19-content-hashes.md#принятый-canonical-profile-v1) | cm-publication-manifest/1/manifest vs instance bytes/provenance exclusions selected; exact metadata carrier/kernel/golden fixtures engineering, legacy bare ManifestHash не contract |
| [Q17: distribution](../../backend/questions/Q17-publication-availability.md#f10-withdrawal-и-historical-distribution) | Restriction fact/transport/effectiveness active/inactive/frozen; объяснимый отказ |
| [Q05: повтор Create](../../../business/questions/Q05-initial-publication.md#f10-повторное-создание-revision) | Intentional new artifact разрешён; hash не identity; sources/nonempty закрыты |
| [Q15: execution](../../backend/questions/Q15-api-and-source-code.md#publishing-command-execution-и-clientoperationid) | Required ClientOperationId/same exact input original receipt приняты; wire/status/recovery representation уточняется |
| [Q09: excluded dependencies](../../../business/questions/Q09-editing-and-deletion.md#f10-зависимости-исключённого-слоя) | Current eligibility effective instance/schema/media closure, trusted retained evidence; retention шире runtime restriction |
| [Q13](../questions/Q13-ui-scope.md) | Exact deep-link encoding/Cancel/уход/confirmations/permissions/presentation/test libraries |

Closed [Q07](../../../business/questions/Q07-exclusions-and-local-content.md)/[Q04](../../../business/questions/Q04-withdrawn-publications.md)/[Q28](../../../business/questions/Q28-content-review-scope.md) semantics не переоткрываются. F17/F11 владеют queue/submit triggers; Create не обещает enqueue side effect.

### Наблюдаемое состояние исходников и отличие от цели

| Источник | Подтверждённая реализация на 04.10.2026 | Отличие target F10 |
| --- | --- | --- |
| [PublicationWorkspacePage.razor](../../../../contentmanager.ui/src/ContentManager.Pages/Components/Publishing/PublicationWorkspacePage.razor), routes/Load/selectors | `/publishing/products/{OwnerId:guid}`, `/publishing/variants/{OwnerId:guid}`; combined workspace; Load выбирает ActiveReleaseId; «При пустом выборе используется текущий релиз» | Applied nested route/explicit exact/null слой/history/exact; no current/latest fallback |
| Тот же page, Prepare/Preview/Create | PrepareRevisionRequest ExpectedRevision `_workspace.Revision`, ProductReleaseId?/VariantReleaseId?; preview disabled Dirty/сравнивается с active; Create вызывает Load/activation advice | No workspace/source token F10; full preview/diagnostics/context/Comment; moderation/activation/effective separate |
| Тот же page, revision table/Revoke | Id/CreatedAt/IsRevoked + Activate/Revoke; shared reason; Revoke workspace ExpectedRevision; нет exact/Retire/Restore | Own ETag/Available-Retired-Withdrawn/details/Withdrawal/history |
| [PublicationEditorModels.cs](../../../../contentmanager.ui/src/ContentManager.Widgets/Components/Publishing/PublicationEditorModels.cs), PublicationDiffRow.Compare | Manual diff `(SchemaId, EntityInstanceId)`, ContentHash/SortOrder/Source/SchemaVersionId; AssignmentEditRow copies fields | Generated primary models/server composer; diff не full validation и не Q28 Delivery-effective baseline |
| [Generated client](../../../../contentmanager.ui/src/ContentManager.ApiClient/OpenApi/Generated/ContentManagerApiClients.g.cs), IPublishingClient/PreviewAsync/RevisionsAsync/RevokeAsync | POST `api/v1/publishing/variants/{id}/preview`, `/revisions`, `api/v1/publishing/revisions/{id}/revoke`; PrepareRevisionRequest(ExpectedRevision:long, ProductReleaseId?, VariantReleaseId?); success200 envelope; PublicationRevisionDto ManifestHash/IsRevoked/Items без Comment/target lifecycle; caller без per-call If-Match/response ETag | Target routes/models/headers/exact/list/lifecycle; legacy status/hash не target |
| [NSwag config](../../../../contentmanager.ui/src/ContentManager.ApiClient/nswag.json), [DI](../../../../contentmanager.ui/src/ContentManager.ApiClient/DependencyInjection/ApiClientServiceCollectionExtensions.cs), [ConfigureApi](../../../../contentmanager.ui/src/ContentManager.ApiClient/DependencyInjection/ApiClientWebAssemblyHostBuilderExtensions.cs) | Namespace/templates/init-accessors/scoped clients/configured HTTP | Reuse после contract/headers/regeneration; не readiness |
| [WorkspacePageBase.cs](../../../../contentmanager.ui/src/ContentManager.Shareds/Components/Shared/Infrastructure/WorkspacePageBase.cs) | Busy/Dirty/lifetime cancellation/Discard-only guard; generic409 concurrency message | Distinct domain/412/428/parent/F15/unknown; forms/context preserved/no blind POST retry |

Source inspection подтверждает current→target различия. Проверка правки — структура/ссылки документации; UI/API/E2E tests не запускались, готовность F10 не заявляется.
