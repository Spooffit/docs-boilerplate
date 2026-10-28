# F11. Publication lifecycle: фронтенд

[Бизнесовая спецификация](../../../business/features/F11-publication-lifecycle.md) · [Бэкенд](../../backend/features/F11-publication-lifecycle.md) · [F09](F09-content-releases.md) · [F10](F10-publication-revisions.md) · [F17](F17-content-review.md) · [M04](../modules/M04-publishing.md)

Документ описывает целевой MVP UI Content Manager: .NET 10, Blazor Standalone, MudBlazor, FSD и NSwag. Будущие блоки — логические обозначения, не существующие классы. Текущий UI и разрывы подтверждены исходниками в §11. Полный Publishing/Moderation не становится зависимостью Demo.

## 1. Результат и границы

Специалист выбирает текущую общую Product base из exact ProductContentRelease и режим Manual/Automatic; для Variant отдельно активирует, откатывает или снимает публикацию exact PublicationRevision. Интерфейс отдельно показывает Publishing active, полный committed Delivery-effective snapshot, processed/target source Sequence и подтверждение availability history. Current serving при разрешающих фактах может использовать committed snapshot, пока history confirmation Pending; это не объявляется готовой Freeze history.

Выбор/очистка Product base сам по себе не активирует и не снимает Variant. Manual оставляет их текущие публикации; Automatic запускает независимую обработку каждого Variant: подготовка/создание F10 → Pending F17 → Approved → backend Activation → асинхронная Delivery propagation. Одна строка AwaitingModeration/Blocked/Failed/Rejected не откатывает успешные остальные. Rejected сохраняет прежнюю active revision.

Ручные Activate и Rollback выбирают только eligible exact Approved Available revision нужного Variant: own lifecycle Active, доступный Active parent и отсутствие актуальных blockers, включая F15. Archived own/parent запрещает normal Activate/Rollback/Automatic promotion, не перезаписывая lifecycle Variant. Retired, Withdrawn, never-approved и rejected-only не предлагаются как допустимые цели. Rollback разрешает любую допустимую historical revision, а не только предыдущую. Retire старого source release сам по себе не запрещает historical materialized revision и не требует новой composition/moderation; актуальную допустимость revision проверяет сервер. Установка уже active revision — no-op без новой history; Unpublish переводит active в null и сохраняет history, повтор при null — no-op.

Создание release/изменение assignment относятся к F07–F09; immutable composition, own revision lifecycle и Withdrawal — к F10; решение модерации — к F17; entitlement/frozen access — к F12. Здесь не создаются revision CRUD, браузерный composer, global store, отдельная moderation action или прямые записи в Delivery. Withdrawal terminal запрещает серверную выдачу exact RevisionId, включая inactive/frozen: при active дополнительно возникает null/history, без автоматического fallback. UI не отправляет вслед второй Unpublish.

Archive/Conflict не выполняют Unpublish/Withdrawal/revoke и не останавливают historical materialized Delivery по previously confirmed entitlement, включая новые sessions. Safety Unpublish/explicit Withdrawal сохраняются по собственным permissions/scope. Restore source Active не активирует и не unwithdraw/grant; новые действия повторно проверяют own/parent/other blockers. Automatic continuation использует exact bound revision/iteration, current automatic head, captured Product state Version и Variant publication Sequence plus fresh parent/lifecycle/F15/runtime restriction facts. Возврат base A→B→A либо Variant R1→R2→R1 не восстанавливает старое автоматическое намерение. Cancelled/Superseded item терминален и не продолжается после Restore/Resolve; его Approved artifact может остаться допустимым для нового explicit manual action.

## 2. Страницы, экраны и навигация

| Страница / экран | Вход и контекст | Содержимое / режим | Завершение |
| --- | --- | --- | --- |
| SC01. Product Workspace, состояние публикации | Product из F06/F09; рядом с [историей Product releases](F09-content-releases.md#2-страницы-экраны-и-навигация) | Applied Product, F15 ограничения, текущая base, exact release choice, Manual/Automatic, independent operation items | Подтверждённая base/operation либо отказ; переход в Variant/revision/moderation |
| SC02. Variant Workspace, «Публикация» | `/products/{productId}/variants/{variantId}/publishing`; F06/F09/F10 | Applied Variant/parent, F15, F10 revision context, active/effective, Activate/Rollback/Unpublish, history | Подтверждённый Publishing transition; Delivery отдельно; контекст Variant сохранён |
| SC03. Подтверждение Product base | SC01: выбрана exact base и mode либо явная очистка | Текущая/выбранная base, mode, последствия для Variants; Confirm/Cancel | Confirm → серверная операция; Cancel без записи |
| SC04. Выбор и подтверждение Variant transition | SC02 или public action exact revision F10 | Action, текущая active, target exact revision и источники/Comment/moderation/lifecycle; reason по контракту; Confirm/Cancel | Confirm → exact state mutation; Cancel сохраняет публикацию |
| SC05. Результаты automatic promotion | SC01 после запуска/выбора known operation | Operation identity/base, per-Variant result, revision/review context и причина; ссылки в рабочие области | Перечитать результат/исправить предметный blocker через соответствующую фичу |

SC05 — представление внутри Product Workspace; отдельная глобальная страница операций не требуется. Конкретный Product publication UI route, encoding selected revision/operation и восстановление после browser refresh **уточняются** в [Q13](../questions/Q13-ui-scope.md) и [Q15](../../backend/questions/Q15-api-and-source-code.md#f11-publication-state-и-automatic-promotion). Текущие routes `/publishing/products/{OwnerId}` и `/publishing/variants/{OwnerId}` — факт реализации §11, не целевой контракт.

Direct entry читает authoritative applied owner/state; не выбирает первую history строку и не подменяет target на latest. При смене Variant owner очищаются прежние выбор, pending UI context и tokens. Applied move того же Variant обновляет parent/breadcrumb/canonical context через F06; несохранённую форму не выбрасывают молча, пригодность выбранной revision перепроверяют сервером. Pending move не является applied. Старый parent POST автоматически не повторяют.

## 3. Карта FSD-блоков

### 3.1. Необходимые уровни

Pages координируют context и соседние F09/F10/F17, два widgets связывают свои state/read/actions. Самостоятельные features выполняют Product base action и Variant transition; entities предоставляют read-only представления generated данных. Shared отвечает за per-call headers/errors/confirmation. Новые app providers и глобальное хранилище не требуются.

### 3.2. Состав

| Блок | Логическое размещение | Ответственность / использование | Основное описание |
| --- | --- | --- | --- |
| ProductWorkspace / VariantWorkspace, PublicationSection | pages / соответствующий workspace / ui, model | Applied context/F15, композиция SC01/SC02, refresh соседних секций | Этот документ §3–§4; F06/F09/F10 |
| ProductPublication | widgets / product-publication / ui, model | Current base/choice/mode и SC03/SC05 | §3.3 |
| VariantPublication | widgets / variant-publication / ui, model | Active/effective/history и SC04 | §3.3 |
| SelectProductBase | features / select-product-base / model, api | Выбрать/promote base с mode; явная очистка | UI01–UI02 / DF02; clear contract Q15 |
| TransitionVariantPublication | features / transition-variant-publication / model, api | Отдельные Activate/Rollback/Unpublish, captured state/token | UI03–UI05 / DF04–DF06 |
| CancelAutomaticItem | features / cancel-automatic-item / model, api | Явная отмена exact unfinished item со своим precondition; без publication transition | UI09 / SC05; wire Q15 |
| PublicationStateView / ActivationHistory / PromotionResults, reads | entities / publication-state, promotion-operation / ui, api | Read-only generated state/history/operation, navigation events | DF01/DF03/DF07–DF08 |
| ContentReleaseView / PublicationRevisionView / moderation link / Catalog context | public blocks F09/F10/F17/F06/F15 | Exact identity/источники/доступность/Approved/context | [F09 §3](F09-content-releases.md#3-карта-fsd-блоков), [F10 §3](F10-publication-revisions.md#3-карта-fsd-блоков), F17 |
| API headers/errors, controls, confirmation | shared / api, ui | DTO + отдельная metadata; generic pending/error/focus | [S05](../../../catalog/docs/standards/ui/forms-and-accessibility.md#form-state-and-user-feedback), [S06](../standards/S06-api-client.md), [S07](../../../catalog/docs/standards/ui/forms-and-accessibility.md#access-and-semantics) |

### 3.3. Ответственность и публичные интерфейсы

| Блок | Входы / источник | Выходы / получатель | Собственное состояние | Зависимости / жизнь |
| --- | --- | --- | --- | --- |
| Page/PublicationSection | Route IDs, applied owner и F15 restrictions | Context → widget; navigation; confirmed result → public refresh F09/F10/F17 | Owner/section/navigation, без второй state model | Public catalog/read/widgets; смена owner исключает late old response |
| ProductPublication | Applied Product/restrictions, release reads F09 | Generated action request → feature; confirmed state/operation → views/page | Generated Product state + own ETag; release selection/mode/form/pending/errors; operation DTO/ID | DF01/DF02/DF08, F09 reads/shared; owner lifetime; выбор без autosave |
| VariantPublication | Applied Variant/parent/restrictions; selected exact revision F10 | Generated transition request → feature; результат → state/history/page | Generated Publishing state + own ETag; separate Delivery read; history/target/action/reason UI-state | DF03–DF07/DF09, F10/F17 views/shared; owner lifetime; refetch не должен стирать другую форму |
| SelectProductBase | Captured ProductId/exact release/mode, own state ETag либо clear action; explicit subset для нового Automatic intent | Original Start receipt/state/operation identity либо отказ → ProductPublication | Generated original request/ClientOperationId/state If-Match/release/subset/actor до outcome; no business copy | DF02/clear Q15/shared; SC03 lifetime; no F09/F10/F17 HTTP orchestration; retry domain item создаёт новую operation |
| TransitionVariantPublication | Captured applied IDs/action/exact target/state ETag/permissions | Result/error → VariantPublication | Pending/captured generated request/token/context | DF04–DF06/shared; SC04 lifetime; выбор не изменяется незаметно во время отправки |
| CancelAutomaticItem | Exact OperationId/ItemId и own item precondition, current permission | Confirmed item outcome либо stale/domain refusal → ProductPublication/SC05 | Captured ID/token/pending/error; не содержит команды для всего Variant | Public cancel contract Q15; late ответ не меняет replacement item; state/history token не заменяет item token |
| State/history/result views и reads | Generated DTO или IDs для чтения | DTO + metadata → widget; target navigation → page | Read-only input, раскрытие/selected row UI-state | Shared transport; history по Sequence; operation независима от выбранного release |

Все будущие имена — проектные обозначения. Generated types/методы определяет accepted OpenAPI. Для edit/read напрямую используются NSwag models; отдельная ручная business DTO/ViewModel/EditorModel не вводится.

## 4. Логическая компоновка

```text
SC01. ProductWorkspace / PublicationSection [pages; applied Product]
  Catalog context / F15 restrictions [F06/F15]
  Release history / exact details [public F09; без Create в F11]
  ProductPublication [widgets; own Product state + ETag]
    Current Product base: exact release либо null
    Exact Available ProductRelease choice / source summary / Comment
    SelectProductBase: Manual | Automatic → SC03
    Явно очистить текущую base → SC03 [HTTP contract Q15]
    PromotionResults → SC05 [operation, не release-bound список]

SC03. Product base confirmation [feature; captured state]
  Product / current base / выбранная base либо null
  Mode / последствия / affected context
  Confirm / Cancel / pending / errors

SC02. VariantWorkspace / PublicationSection [pages; applied Variant/parent]
  Catalog context / F15 restrictions [F06/F15]
  Releases / materialized exact revision [public F09/F10]
  Moderation status / exact iteration link [public F17]
  VariantPublication [widgets]
    PublicationStateView
      Publishing active RevisionId либо null
      Committed Delivery-effective RevisionId либо null / own loading-error
      Target/processed source Sequence / effective outcome отдельно
      Visibility history confirmed/pending / gap / materialization failure / terminal ban
    Activate / Rollback / Unpublish → SC04
    ActivationHistory [server Sequence; read-only]

SC04. Variant transition [feature; captured exact state/token]
  Current active + Delivery effective
  Activate/Rollback: target exact Approved Available revision [F10/F17]
    Exact source IDs / ChangeComment / допустимость / summary изменений
  Unpublish: active → null; history/frozen semantics; не Withdrawal
  Reason по contract / Confirm / Cancel / pending / domain error

SC05. PromotionResults [entity view внутри ProductPublication]
  OperationId / captured Product base / operation context
  Variant rows [independent item state]
    Variant / status / reason
    Created exact revision / Pending iteration / decision links
    Publishing transition и Delivery propagation как разные результаты
    Открыть Variant / revision / moderation / перечитать operation
    Явно отменить exact unfinished item [CancelAutomaticItem]
    Выбрать исправленный Blocked/Failed subset → новая operation / SC03
```

SC01/SC02 постоянно показывают context; подтверждение SC03/SC04 открывается по действию. SC05 обновляется в своём operation context и не заменяет release/history forms. При отсутствии active/history показано отсутствие публикации, а не ошибка.

| Связь | Передача | Координатор | Наблюдаемый эффект |
| --- | --- | --- | --- |
| F09 release choice → SC03 | Exact release ID/detail + выбранный mode | Product widget | Confirm относится к выбранной base; новое release не выпускается |
| Base action → state/SC05 | Confirmed DTO/metadata + operation identity, если создана | Product widget/page | Current base обновлена; Variants не объявлены активными одной сменой base |
| F10 exact view → SC04 | Exact revision/context/допустимость | Variant page/widget | Target captured; F17 Approval не выполняется этой кнопкой |
| Transition → state/history | Confirmed Publishing result/new metadata | Variant widget/page | Active/history отражают commit; Delivery остаётся отдельным read |
| Delivery read → StateView | Effective exact ID / evidence propagation | Variant widget | Метка клиентской доступности меняется только по подтверждённому Delivery |
| SC05 row → соответствующий workspace | Applied Variant/RevisionId/iteration context | Page | Исправление причины; другие operation items не сбрасываются |
| F10 Withdrawal result → F11 reads | Confirmed lifecycle restriction/affected exact ID | Page | State/history/effective обновляются без второго Unpublish/fallback |

## 5. Зависимости и публичные границы

Page передаёт widgets owner context/restrictions, а widgets передают action features generated request и callback результата. Feature не вызывает другую feature, соседний widget или родительский компонент напрямую. Product UI не имитирует automatic backend pipeline последовательными F10/F17/Activate запросами из браузера.

Public reads/view F09 обеспечивают exact release выбор; F10 — saved revision/metadata/composition; F17 — moderation status/link. Обновление этих представлений координируется page по результату. History является read-only entity view и не мутируется редактором строк. Shared transport не знает смысл promote/rollback и не хранит общий «последний ETag» для всех ресурсов.

Browser не зависит от Domain/EF. UI state, revision lifecycle, moderation и Delivery имеют разные ресурсы/metadata. Блокировка действия объясняет UX и не заменяет серверную проверку permissions/owner/Approved/availability/conflicts. Ссылки F12/F17 не создают внутри F11 вторую форму политики или решения review.

## 6. Данные и владение состоянием

### 6.1. Состояние

| Данные / состояние | Источник истины | Клиентский владелец / жизнь | Обновление / сброс |
| --- | --- | --- | --- |
| Current Product base / own state ETag | Publishing Product publication state | ProductPublication; один Product | DF01/confirmed DF02; owner change |
| Publishing active / own state ETag | Publishing Variant publication state | VariantPublication; один Variant | DF03/confirmed transition/Withdrawal refresh |
| Delivery effective / propagation evidence | Delivery read model, контракт Q15 | VariantPublication; отдельное read state | Подтверждённое чтение; не вычисляется из active/history |
| Visibility confirmation / catch-up / restrictions | Generated public read backend F13; underlying models у владельца | VariantPublication read-only view рядом с committed effective, без копии DTO | Pending/history-ready, source gap, technical failure и terminal ban отдельно; stored VisibilityConfirmedAt не physical commit clock |
| Exact target/moderation/lifecycle | F10/F17 authoritative reads | Widget хранит generated exact DTO, view получает input | Выбор/refresh; пригодность сервер проверяет при action |
| Activation history | Append-only Publishing history | VariantPublication → ActivationHistory | DF07; mutation result/refresh; owner change; Sequence order |
| Operation/items | Durable server promotion operation | ProductPublication → SC05; captured OperationId | DF08; identity сохраняется отдельно от current base/release selection |
| Captured automatic inputs | Server start snapshot: ProductReleaseId, internal Product state Version/Variant Sequence, applied membership и exact latest-created local release IDs/work basis | Generated operation/item read показывает source identities/inputs; internal Version не добавляется в DTO и не вычисляется UI | Existing operation не обновляет inputs при новых releases/move/Variant creation; новая operation имеет отдельный intent |
| Own item precondition / current head applicability | Item read/result metadata по Q15; current head — server Publishing fact | Cancel feature хранит exact ItemId/token; progress owner хранит независимые item results | Item Version independent; чужой item update не делает все другие stale; summary derived server-side |
| Automatic Start intent | Required GUID ClientOperationId + original generated request/own Product If-Match/release/subset/actor | SelectProductBase до confirmed/unknown resolution | Same ID/input original receipt, new explicit intent/new ID; unknown не вызывает новую operation автоматически |
| Mode, target selection, reason, pending/error | Presentation-state и generated request до отправки | Соответствующий widget/feature | Ввод/Confirm/Cancel по правилам; no global store/autosave |

ETag хранится рядом с соответствующим generated DTO, а не в `Version`/`ExpectedRevision` поле бизнес-модели. Для Variant action применяется ETag **publication state**, а не revision/assignment/moderation/Product token. Product action использует свой token. Delivery effective нельзя включать в combined strong-ETag representation, версионируемую только Publishing state: Delivery меняется независимо. Separate read либо scoped metadata/representation **уточняется** в Q15.

Отменённый browser request не является отменой серверного commit/automatic operation. Ответ старого owner/operation не перезаписывает новый контекст. При refetch из-за конфликта сохраняют пользовательский target/reason по возможности, но не сохраняют stale token как действующий; несохранённые assignment/release формы соседних фич автоматически не save/discard.

### 6.2. Состав представления и формы

| Сведение | Источник | Использование |
| --- | --- | --- |
| Product / Variant / applied parent | F06 read | Heading/breadcrumb/context; имя не заменяет ID |
| Current/target ProductContentRelease | F09 exact + Product state | ID/Comment/created/availability; явный выбор current base |
| Manual / Automatic | Promotion request | Видимый выбор последствий; числовой enum/default из legacy не наследуется |
| Active / effective RevisionId, applied Sequence/readiness | Раздельные state reads | Две подписи, ссылки в F10; null различим; равенство IDs само по себе не доказывает readiness |
| Target revision | F10/F17 exact reads | ID/sources/Comment/lifecycle/Approved; semantic summary, без recomposition |
| Reason / action | Generated request, contract Q15 | Ввод по server constraints; не объявлять required/limits по старой форме |
| History record | F11 history read | Sequence, RevisionId либо null, время, reason, actor/correlation при наличии, ссылка/Comment revision |
| Operation item | Promotion read | Variant, stage/result/reason, known revision/review identity; independent progress |

History сортируется по server Sequence; одинаковое время не меняет порядок. Точный DTO actor/correlation/Comment, paging/filter и состав summary **уточняются** в Q15; UI не создаёт отсутствующие server facts.

## 7. Действия и клиентские потоки

| UI-ID / место | Условие и шаги | Серверная граница | Результат и coordination |
| --- | --- | --- | --- |
| UI01. Выбрать/promote base, SC01→SC03 | Exact допустимый Product release + own Product state token; показать current/target/mode; Automatic фиксирует один ClientOperationId/original input; Confirm | DF02 | Manual обновляет base; Automatic возвращает durable operation/captured membership, SC05; zero items уже completed без Variant changes |
| UI02. Очистить base, SC01→SC03 | Показать current→null и отсутствие implicit Variant Unpublish; отдельное явное подтверждение | ClearProductBase, HTTP shape Q15 | Product base null; F09 active-base Retire можно повторить отдельно по его правилам; Variant active/history прежние |
| UI03. Activate, SC02/F10→SC04 | Exact Approved Available revision same Variant; context/F15/permissions; exact state token; Confirm | DF04 | Confirmed active/history; new Delivery availability пока не заявляется; already active no-op без history |
| UI04. Rollback, SC02→SC04 | Выбрать любую допустимую historical exact revision; current/target/summary; Confirm | DF05 | Новая activation history запись, не редактирование прежней; same active no-op |
| UI05. Unpublish, SC02→SC04 | Current active→null; объяснить сохранение history/frozen semantics; Confirm | DF06 | Null/history; повтор null no-op; no Withdrawal/no fallback |
| UI06. Читать историю, SC02 | Own context, DF07; раскрыть exact revision/null transition | DF07/F10 reads | Read-only history; переход с captured identity |
| UI07. Наблюдать promotion, SC01/SC05 | Known OperationId; перечитать items; открыть причину в Variant/F10/F17 | DF08 | Обновление каждой строки; один успех не скрывает остальные; мониторинг Q15 |
| UI08. Обновить state после внешнего события, SC02 | F17 approval/automatic result/F10 Withdrawal/manual чужого пользователя/явный refresh | DF03/DF07/DF09 | Authoritative active/history/effective, без replay mutation |
| UI09. Явно отменить Automatic item, SC05 | Exact unfinished ItemId/own item precondition/permission; captured target | CancelAutomaticItem, route/wire Q15 | Cancelled item; publication pointer/history прежние; stale old ID не отменяет replacement head |
| UI10. Новый запуск исправленного subset, SC05→SC03 | Явно выбрать Blocked/Failed members после устранения причин, reread release/Product state; подтвердить new intent | DF02 Start с новым ClientOperationId/confirmed subset, wire Q15 | Новая operation/inputs/items; original items/history не переписаны; successful old results не replay |

Automatic pipeline исполняет backend. Start под guard фиксирует ProductReleaseId, Product publication state Version, applied Variant membership и exact latest-created local release по ReleaseNumber для каждого item. Archived/conflicted members отображаются Blocked. Latest local release Retired → Blocked без fallback на старый Available. Если release никогда не было, любая незавершённая работа в одной из четырёх patch groups блокирует base-only; при полном отсутствии работы base-only допустим. Explicit empty local release считается существующим source. Поздние local releases/drafts, новые/moved Variants не меняют inputs старой operation; current source/lifecycle/owner facts всё равно проверяются при исполнении.

Pending item ещё может не иметь результата. Один Publishing prepare commit создаёт новую F10 revision, её exact automatic F17 Pending iteration, binding и AwaitingModeration item; UI не выполняет цепочку Create→Submit и не усыновляет чужой Pending/hash-equivalent artifact. AwaitingModeration уже имеет exact RevisionId+ModerationId. Human decision и activation — отдельные durable commits. После matched Approval применяется exact result либо typed refusal; Rejected оставляет прежний active. Withdraw этой bound iteration отменяет её exact automatic item; новая manual iteration не прикрепляется к нему.

New automatic head по Variant заменяет предыдущий unfinished item, включая повтор той же base с новым намерением. Actual manual Activate/Rollback/Unpublish и active Withdrawal атомарно снимают прежний head; старый automatic result не перезаписывает их. Same-active/already-null no-op не меняет Version/Sequence/history/outbox и НЕ отменяет automatic item: для этого служит UI09. Cancel/Supersede item не Withdraw-ит human iteration; её Pending/Approved history сохраняется. Approved continuation требует exact bound Approved tuple/current head, unchanged captured Product state Version и Variant publication Sequence, current base и свежих guards. Duplicate/terminal decision не активирует ничего повторно. AlreadyCurrent завершает допустимый item без нового pointer/history/outbox. Item Activated сохраняется атомарно с state/history/outbox; operation summary выводится из exact item outcomes, а Delivery readiness читается отдельно.

Technical recovery использует те же captured inputs/item/result tuple, без reselect latest или нового Submit. Domain Blocked требует исправления и явного нового Start для подтверждённого subset; original item/history не переписываются. Точные routes/controls/attempt limits остаются Q15/Q20, legacy updates/retry не переносится. Unknown Start сохраняет original ClientOperationId/request/Product If-Match/actor; same ID/input после current auth возвращает original receipt до preconditions нового исполнения, bound ID/different input409. Receipt не запускает operation заново и не заявляет её current progress; нужны отдельные reads. Новый ID после lost ACK не генерируется автоматически. [Q15 execution](../../backend/questions/Q15-api-and-source-code.md#publishing-command-execution-и-clientoperationid), [Q20 workflow](../../backend/questions/Q20-persistence-and-migration.md#publishing-workflow-capture-и-продолжение).

## 8. Использование API и NSwag

Select/Clear/Activate/Unpublish/Cancel и promotion stages используют выбранный MVP writer protocol [Q20](../../backend/questions/Q20-persistence-and-migration.md): fresh lifecycle/catalog/F15/approval/basis facts защищены до каждого owner commit. Workspace/operation read не permit; own state/item preconditions сохраняются, foreign/domain409 не подменяется own412. Technical gate/read failure не открывает CatalogConflict, не выбирает fallback revision и не подтверждает activation. Принятые head/Product-Version/Variant-Sequence guards определяют late-approval/overlap applicability; они не делают Publishing commit временем Delivery effective. Unknown Start использует прежний execution intent; unknown lifecycle/Cancel recovery representation — Q15 без blind replay/new token.

Контрактные границы ниже наследуют целевые пути F11; это **не текущие** операции generated client. Final routes, request/response/status/headers, monitoring и retry остаются **Уточняется** в [Q15](../../backend/questions/Q15-api-and-source-code.md#f11-publication-state-и-automatic-promotion). Таблица не назначает ещё не принятые имена generated методов или DTO.

| DF-ID / потребитель | Целевая операция | Передаваемый контекст / metadata | Результат / владелец |
| --- | --- | --- | --- |
| DF01. Product read | `GET /api/v1/products/{productId}/publication-state` | Applied ProductId | Generated Product state + own ETag → ProductPublication |
| DF02. UI01/UI10 | `POST /api/v1/products/{productId}/publication-state/promote` | Exact ProductContentReleaseId + Manual/Automatic; own Product state If-Match; required GUID ClientOperationId в body Automatic Start; confirmed subset carrier Q15 | Original Start receipt/state/operation identity; subsequent progress отдельный read; response shape Q15 |
| DF03. Variant read | `GET /api/v1/products/{productId}/variants/{variantId}/publication-state` | Applied IDs | Generated Publishing state + own ETag → VariantPublication; Delivery separation Q15 |
| DF04. UI03 | `POST /api/v1/products/{productId}/variants/{variantId}/publication-state/activate` | Exact RevisionId/reason по contract; `If-Match` own Variant state | Confirmed state/history effect; no-op различим; shape Q15 |
| DF05. UI04 | `POST /api/v1/products/{productId}/variants/{variantId}/publication-state/rollback` | Exact historical RevisionId; own state `If-Match` | Confirmed transition; не previous fallback |
| DF06. UI05 | `POST /api/v1/products/{productId}/variants/{variantId}/publication-state/unpublish` | Own state `If-Match`; reason shape Q15 | Active=null/history либо no-op already null |
| DF07. UI06/UI08 | `GET /api/v1/products/{productId}/variants/{variantId}/activation-history` | Applied IDs; paging/filter Q15 | Generated history/server Sequence → VariantPublication |
| DF08. UI07 | `GET /api/v1/product-release-promotions/{operationId}` | Captured OperationId, не только ProductReleaseId | Generated operation/items → ProductPublication/SC05 |
| DF09. Effective read | **Уточняется**, Q15 | Variant context; separate read/metadata scope | Delivery effective evidence → separate state view |
| UI02. Clear Product base | **Уточняется**, Q15 | Clear intent + own Product state `If-Match` | Null base; без Variant transition |
| UI09. Cancel Automatic item | **Уточняется**, Q15; отдельная action, не Variant transition | Exact ItemId + own item precondition; token carrier Q15 | Confirmed item outcome, без publication history; не отменяет replacement item |

F09 release list/exact и F10 revision exact/history/F17 status используют public операции своих спецификаций. F11 не копирует их DTO и не добавляет revision withdrawal к publication-state endpoints.

NSwag конфигурация — [nswag.json](../../../../contentmanager.ui/src/ContentManager.ApiClient/nswag.json), клиент — [ContentManagerApiClients.g.cs](../../../../contentmanager.ui/src/ContentManager.ApiClient/OpenApi/Generated/ContentManagerApiClients.g.cs). После принятого backend/OpenAPI контракт перегенерируется; generated code вручную не правится. Headers доступны через согласованную shared client infrastructure per call; не через mutable global `LastETag`.

Mutation блокируется, пока own exact state/ETag не загружен. Initial no-state representation/token и первый mutation flow уточняются в Q15/Q20: нельзя придумывать token для отсутствующего root. После подтверждённого ответа сохраняют актуальные state/metadata из принятого контракта; перечитывают только требуемые представления. Потребность в GET после mutation и возвращаемый authoritative result фиксируются Q15; текущий безусловный `Load()` не становится требованием. No-op не добавляет client history строку. Readiness определяется подтверждёнными applied Sequence/status, не только равенством active/effective IDs (включая возврат к R1 и null/null).

[Q17 visibility](../../backend/questions/Q17-publication-availability.md#подтверждённая-видимость-и-история-delivery) использует stored VisibilityConfirmedAt: primary observation после уже committed exact полного Delivery transition, затем durable once-only confirmation. UI не показывает её как physical commit timestamp либо precommit time. Catch-up идёт в strict source Sequence order с durable gap recovery; предыдущий допустимый effective snapshot может обслуживаться во время lag. One transition per commit; known terminal-blocked intermediate имеет NotEffective outcome без availability-history row, technical missing artifact не successful consumed transition. Rollback старого artifact — новый transition/time. Повтор outcome возвращает сохранённую confirmation/time; unknown ACK выясняется чтением stored result, отсутствие результата требует fresh recovery confirmation, без backdating. Independent exact ban применим и к inactive/unmaterialized revision; late replay/materialization не снимает его. Read carrier/routes/refresh transport — [Q15](../../backend/questions/Q15-api-and-source-code.md#delivery-visibility-contracts), без нового автоматического polling.

## 9. Состояния, ошибки и доступ

| Ситуация | Что видит пользователь | Действие / сохранение контекста |
| --- | --- | --- |
| Technical gate/read failure | Техническая недоступность, не domain409/новый CatalogConflict | Context/token сохранены; safe read; unknown mutation выясняется по Q15 без blind retry/new token |
| Loading Product/Variant state | Загрузка соответствующей области; base/active неизвестны до ответа | Изменяющие действия ждут token; соседние формы не сбрасываются |
| Base/active/effective null | Явное отсутствие соответствующего выбора | Не смешивать null с read failure; history остаётся читаемой |
| Выбор / Confirm / pending | Captured current/target/action, результат ещё не подтверждён | Double click blocked per action; Cancel до отправки без mutation |
| Pending item | Подготовка Variant, exact revision ещё может отсутствовать | Нет выдуманных revision/review links; старые active/effective по server state |
| AwaitingModeration item | Revision + её exact Pending moderation + binding уже committed вместе | Exact links; old active/effective сохранены; Cancel exact item отдельно от Withdraw human iteration |
| Blocked/Rejected/Failed item | Stage и причина конкретного Variant | Domain исправление → new confirmed subset operation; technical retry сохраняет exact tuple; Rejected old active retained |
| Cancelled/Superseded item | Терминальный outcome прежнего intent; revision/iteration history остаётся | Нет resume после Restore/Resolve/new manual Submit; Approved artifact допускает отдельный manual workflow |
| Same-active/already-null no-op при auto head | Pointer не изменился, automatic item всё ещё действует | Explicit Cancel exact item по UI09; no-op не подменяет cancel и не создаёт history |
| AlreadyCurrent / zero-item operation | Completed item без нового transition либо completed operation без members | Original result/read progress различимы; не выдумывать activation/Delivery event |
| Publishing committed, Delivery ещё старая | Два exact ID и незавершённая propagation | Не писать «доступна клиентам» для новой active; не повторять Activate ради sync |
| Delivery read/propagation failure | Отдельная ошибка/неподтверждённая effective state | Не откатывать Publishing локально и не выдавать stale read за current |
| Effective committed / history confirmation Pending | Current snapshot сохранён, VisibilityConfirmedAt/history-ready ещё не подтверждены | Current serving только при иных разрешающих фактах; necessary Freeze history pending → Indeterminate, no earlier-history fallback |
| Source gap / materialization failure / NotEffective | Последовательность/причина показаны отдельно от active ID | Gap recover strict order; technical failure не consumed success; terminal-blocked intermediate no availability-history row, следующий outcome допустим |
| Exact terminal ban / ban facts unavailable | Known restriction отдельно от propagation либо неизвестная required authority | Ban запрещает target даже inactive/unmaterialized; unknown не permission; отсутствие history row не снимает restriction |
| Archived own/parent / stale automatic basis | Понятный reason/operation item blocker; own Variant lifecycle отдельно | Normal Activate/Rollback/Promotion disabled; read/history и разрешённый Unpublish; fresh context перед новым действием |
| `409 Conflict` | Предметный отказ: owner/availability/Approved/F15/coordination | Обновить нужный context; не повторять как transient/network failure |
| `412 Precondition Failed` | Exact publication state либо cancelled-item precondition изменился | Reread того же state/item, сохранить target/reason; старый Cancel не перенаправить replacement head; без automatic replay |
| `428 Precondition Required` | Mutation не содержала обязательный own resource precondition | Ошибка client flow; восстановить exact state/item metadata по контракту, не подменять чужим token |
| Network/timeout, неизвестен исход | Не подтверждён ни успех, ни отмена server commit | Выяснить exact state/operation по accepted contract; без слепого повторения POST |
| Нет permission / недоступен owner/target | Понятный отказ соответствующей области | Server authoritative; не переходить на другой Variant/первую revision молча |
| Withdrawal exact revision | Terminal restriction независимо от active | F10 control; state/history/effective refresh; no implicit fallback/Unpublish |

Управление cancel/уходом с unsaved reason/choice и сохранением operation selection после refresh **уточняется** в Q13/Q15. Context/action и ошибки доступны с клавиатуры; открытие/закрытие confirmation сохраняет осмысленный focus, pending/error/read result имеют понятные подписи по S05/S07. CSS/layout requirements отсутствуют.

## 10. Проверки и приёмка

Проверки реализации ещё не выполнены этой документационной работой. Ниже ожидаемое поведение; компонентные/API doubles не доказывают backend commit, F17 coordination или Delivery propagation. Применяется [S08](../standards/S08-testing.md).

| Блок / сценарий | Исходные условия и воздействие | Наблюдаемый результат | Граница проверки |
| --- | --- | --- | --- |
| Protected commit / technical failure | Foreign lifecycle/catalog/source changes между preview и commit; bounded gate wait/cancellation/read failure; lost ACK | Server revalidates protected facts; own412/domain409/technical failure различимы; ввод/basis/token сохранены, no false success/empty valid/blind retry | UI/transport doubles для representation; real PostgreSQL owner writers/barriers для guard, recovery contract Q15 отдельно |
| SC01/SC03 Manual | Выбрать exact base, Confirm | Own Product token/action; current base изменена, нет Variant activation | Composition + accepted API double; backend отдельно |
| SC01 clear | Current base; явный clear/Cancel | Cancel no mutation; Confirm base null, no Variant Unpublish | После согласования HTTP Q15 |
| SC02/SC04 Activate | Approved Available same-Variant target | Exact ID + own state ETag; после commit active обновлена, effective отдельно | Component/request contract; реальный transition отдельно |
| SC04 invalid targets | Never-approved/rejected-only/Retired/Withdrawn/wrong-owner/F15 | Target недопустим; server rejection понятен; active не подменяется | Eligibility view + server error composition |
| Same-active Activate/Rollback | Target уже active; повтор | Нет новой history/Sequence; UI не дописывает строку | Real backend invariant + UI authoritative response |
| Rollback | Несколько historical Approved Available revisions | Выбор произвольной допустимой, новая history, старые строки прежние | Component selection + real history |
| Unpublish/null повтор | Active exact → null, затем повтор | History сохранена, повтор no-op; frozen restriction не выдумана | UI + реальный Publishing/F12 контур |
| SC05 partial results | Один успех, другие Pending/Blocked/Failed/Rejected | Видны независимые строки; oldactive rejected сохранена; нет общего rollback/ложного общего успеха | Operation composition; реальный pipeline отдельно |
| First automatic/base-only | Local patch отсутствует; другой Variant draft без первого release | Первый разрешён base-only, второй Blocked без молчаливой публикации draft | Backend scenario + UI причины |
| F17 Approved→auto | Automatic item Pending→Approved | Backend Activation, затем separate Delivery evidence; browser не вызывает Activate сам | Реальный Publishing/Moderation/Delivery E2E |
| Active/effective discrepancy | Новый active, старый effective/Delivery error | Разные IDs/состояния; доступность только по Delivery | Component view + real propagation |
| History ordering | Одинаковые timestamps, разные Sequence/null rows | Порядок Sequence, null сохраняется; history read-only | View composition |
| `412`/`428`/`409`/timeout | Ошибка exact mutation | Различимые сообщения, target/reason context, no blind replay | HTTP double; server status contract отдельно |
| Owner/move/late response | Смена owner/parent или operation во время запроса | Ответ не перезаписывает чужой context; token не переносится | Client coordination |
| Withdrawal inactive/active | Withdraw frozen inactive; отдельно active exact | Terminal exact-ID restriction; active null/history, no fallback/no second Unpublish | Реальный F10/Publishing/Delivery/F12 контур |
| Late Approval / ABA | Product base A→B→A; Variant R1→R2→R1; matched old approval; same-base new operation | Captured Product Version/Variant Sequence и current head не позволяют old automatic overwrite; old Approved history/manual eligibility сохраняются | Real Publishing head/state/decision barriers + UI outcomes |
| No-op vs exact Cancel | Current target уже active либо pointer null при действующем auto item; old ItemId после replacement head | Manual no-op не cancels/no history/outbox; explicit Cancel только exact matched item/token, replacement untouched | UI action/transport + real item/state transactions |
| Captured membership/source | Latest local Retired; empty release exists; never-release+exclusion-only work; new/moved Variant/local release после Start; zero members | Blocked/no older fallback; empty release counts; no first base-only with any working group; old inputs unchanged; zero-item completed | Real Start/prepare capture + operation presentation |
| Prepare/decision/terminal retry | Crash при prepare/activation; duplicate decision; Withdrawn iteration/new manual Submit; Superseded/Cancelled | Atomic exact revision+Pending+binding; no browser Submit/adoption; same technical tuple/no replay activation; terminal no resume | Real Publishing prepare/outbox/inbox/recovery + UI |
| Intent and subset | Lost Start ACK; same ID after Product state drift; bound ID different subset; explicit repaired Blocked subset | Original receipt/no new blind operation; different input409; intentional new subset has new operation/inputs, old history intact | UI capture/recovery + execution-store tests |
| Item progress | Один item изменён; другие tokens прежние; mix Activated/AlreadyCurrent/Cancelled/Blocked; Delivery still pending | Per-item preconditions independent; summary from exact outcomes; Publishing completion не Delivery-ready | Public progress/metadata + real per-item writes |
| Conservative visibility | Delivery commit до cutoff, confirmation после либо ровно cutoff; effective ID уже совпал с active | Current committed отдельно от history-ready; поздняя/equal confirmation не входит в Freeze; matching IDs не readiness proof | UI metadata + real Delivery confirmation/history |
| Catch-up/outcome | Out-of-order/gap, required artifact missing, terminal-blocked intermediate, nullable transition | Previous allowed effective retained; gap pending/technical failure distinct; NotEffective consumes terminal outcome без availability history | Public progress + real sequential projector/outcome tests |
| Confirmation replay / rollback | Lost ACK after stored confirmation; crash без stored result; rollback на тот же старый artifact | Replay original confirmation/time; fresh recovery time только когда не stored/no backdate; rollback new transition/time | Real outcome/confirmation recovery + view |
| Ban versus lag | Inactive/unmaterialized withdrawal; late artifact/projection/replay; unavailable owner ban | Exact ban stays dominant; unknown denied/not-ready per contract; UI не маскирует ban как обычный lag | Real owner restriction/admission + metadata view |

Приёмка документа: все SC/UI/DF связаны с владельцем/результатом, представлены Product base и Variant transition отдельно, future blocks не названы готовыми классами, неизвестные contracts вынесены в Q. Проверка markdown/source trace не является тестом работающего приложения.

Приёмка F06/F15 guard: selectors показывают только eligible targets, exact history сохраняет ineligible ID с reason/actions. Archive блокирует normal Activate/Rollback/Automatic promotion; existing entitled Delivery/новые sessions и safety сохраняются. Continuation/late Approval после смены source/parent проверяет fresh basis и не активирует stale target; restore/Resolve не autoactivate.

## 11. Последовательность изменений и открытые вопросы

Сначала материализовать выбранные writer/captured-input/head/late-Approval protocols и согласовать точные state/item/operation/Delivery representations и recovery, затем OpenAPI/NSwag headers, public reads F09/F10/F17. После этого встроить workspace Start/transition/Cancel/new-subset flows и monitoring, выполнить intent/ABA/terminal/recovery проверки. Не переносить whole legacy workspace/release retry как готовую реализацию цели и не включать полный Publishing в Demo.

### 11.1. Текущая реализация и разрывы с целью

Source trace проверен по рабочему дереву `contentmanager.ui`, включая существующие staged изменения; приложение не изменялось и UI tests не запускались.

| Источник / точка | Подтверждённый текущий факт | Разрыв с целевым F11 |
| --- | --- | --- |
| [PublicationWorkspacePage.razor](../../../../contentmanager.ui/src/ContentManager.Pages/Components/Publishing/PublicationWorkspacePage.razor): routes, Load, вкладки | `/publishing/products/{OwnerId:guid}` / `/publishing/variants/{OwnerId:guid}`; один workspace совмещает assignment/policy/releases/revisions/activation. Показывает `_workspace.ActiveRevisionId`; history выводит Sequence | Нет отдельного Delivery effective/status/read; нет own publication-state ETag; contextual target workspaces §2–§4 |
| [Та же страница](../../../../contentmanager.ui/src/ContentManager.Pages/Components/Publishing/PublicationWorkspacePage.razor): Activate, Unpublish, Revoke | «Активировать / откатить» вызывает только `ActivateAsync`; reason обязателен локальным HasReason; target disabled лишь Busy/Dirty/IsRevoked/already-active. `ExpectedRevision = _workspace.Revision`; после mutation безусловный Load | Отдельный Rollback contract; exact Approved/Available/owner/F15; no-op semantics/own state token; F10 Withdrawal отделён. Legacy reason requirement не подтверждает target schema |
| [Та же страница](../../../../contentmanager.ui/src/ContentManager.Pages/Components/Publishing/PublicationWorkspacePage.razor): PublishRelease, LoadUpdates, RetryUpdates | UpdateMode включён в PublishReleaseRequest; результаты выбираются ProductReleaseId; Retry доступен при Status="Failed" и вызывает RetryAsync; таблица VariantId/Status/Error | F09 Create отдельно от F11 base selection/promotion; durable OperationId и moderation stages; target retry/monitoring Q15 |
| [Generated client](../../../../contentmanager.ui/src/ContentManager.ApiClient/OpenApi/Generated/ContentManagerApiClients.g.cs): IPublishingClient, workspace/action DTOs | Paths `/api/v1/publishing/products/{id}`, `variants/{id}/activate`, `variants/{id}/unpublish`, `product-releases/{id}/updates[/retry]`, `revisions/{id}/revoke`. Workspace Revision/ActiveRevisionId/Activations, revision IsRevoked; PromotionResultDto VariantId/RevisionId/Status/Error | Нет целевых publication-state/rollback/activation-history/operation read; нет Approved/Available/effective fields. Legacy route/body нельзя назвать действующим target contract |
| [nswag.json](../../../../contentmanager.ui/src/ContentManager.ApiClient/nswag.json), [ApiEnvelopeDelegatingHandler](../../../../contentmanager.ui/src/ContentManager.ApiClient/Infrastructure/Http/ApiEnvelopeDelegatingHandler.cs) | NSwag generates interfaces/classes/models с init properties; handler преобразует HTTP failures в ApiResponseException; generated Publishing methods возвращают DTO envelopes | Нужна принятая per-call state ETag/If-Match infrastructure; не копия DTO/ручная правка generated |
| [WorkspacePageBase](../../../../contentmanager.ui/src/ContentManager.Shareds/Components/Shared/Infrastructure/WorkspacePageBase.cs): RunAsync/GuardNavigation/Dispose | Общие Busy/Dirty/Error/Notice, discard confirmation, cancellation lifetime; `409` дополнен generic сообщением «Данные изменились» | State concurrency `412`/missing token `428`/domain `409`/unknown outcome различимы; late owner/operation responses scoped отдельно |
| [CatalogPage](../../../../contentmanager.ui/src/ContentManager.Pages/Components/Catalog/CatalogPage.razor) | Catalog rows/links ведут на текущие `/publishing/products/...` и `/publishing/variants/...` | Contextual routing/applied parent согласуется с F06/Q13; существующие links не утверждают target navigation |
| [UI tests Program.cs](../../../../contentmanager.ui/tests/ContentManager.Ui.Tests/Program.cs) | Проверяет PublicationDiffRow и nullable EffectivePolicy model shape | Эти проверки не подтверждают F11 commands/ETag/moderation/history/Delivery/operation E2E |

### 11.2. Открытые вопросы

| Вопрос | Какие детали блокирует |
| --- | --- |
| [Q15: publication state и automatic promotion](../../backend/questions/Q15-api-and-source-code.md#f11-publication-state-и-automatic-promotion) | Final routes/body/status/headers, clear base, authoritative mutation result, OperationId/item DTO, paging, own state metadata vs separate effective read, monitoring/retry/unknown outcome reconciliation |
| [Q20: workflow capture/continuation](../../backend/questions/Q20-persistence-and-migration.md#publishing-workflow-capture-и-продолжение) | Captured source/membership, exact prepare tuple/head/Product Version/Variant Sequence, terminal/no-op/cancel и retry policies приняты; persistence/read representation уточняется |
| [Q15: execution](../../backend/questions/Q15-api-and-source-code.md#publishing-command-execution-и-clientoperationid) | Required Start ClientOperationId/exact intent/original receipt приняты; wire recovery/Cancel/subset/item precondition carrier уточняется |
| [Q15: visibility contracts](../../backend/questions/Q15-api-and-source-code.md#delivery-visibility-contracts) | Separate active/committed effective/processed outcome/history confirmation/gap/ban read carrier; no matching-ID readiness shortcut |
| [Q20: catch-up/visibility](../../backend/questions/Q20-persistence-and-migration.md#delivery-catch-up-и-подтверждение-видимости) | Ordered durable outcome/once-only confirmation/replay/independent ban приняты; physical public representation materialization |
| [Q13: UI scope](../questions/Q13-ui-scope.md) | Product publication route, exact/operation navigation, несохранённый ввод/Cancel/refresh context; не обязательная новая глобальная страница |
| [Q17: availability](../../backend/questions/Q17-publication-availability.md) | Delivery effective/propagation evidence и терминальный Withdrawal включая historical/frozen |
| [Q18: history](../../backend/questions/Q18-activation-history.md) | Public history details в согласовании с Sequence/no-op/invariants, без переписывания записей |

Полные вопросы/решения остаются в Q. При закрытии вопроса обновляется соответствующий нормативный раздел F11; стандарты и M04 не заполняются копией экрана или project-specific HTTP контрактом.
