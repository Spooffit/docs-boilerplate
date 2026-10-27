# F17. Модерация PublicationRevision: фронтенд

[Бизнесовая спецификация](../../../business/features/F17-content-review.md) · [Backend](../../backend/features/F17-content-review.md) · [Q28](../../../business/questions/Q28-content-review-scope.md) · [F10](F10-publication-revisions.md) · [F11](F11-publication-lifecycle.md) · [F16](F16-staff-access.md) · [M06](../modules/M06-staff-workflow.md)

Документ описывает целевой MVP UI Content Manager: Blazor Standalone, MudBlazor, FSD и NSwag. Будущие имена блоков являются проектными обозначениями, а не существующими классами. В Demo раздела модерации, ролей и обязательного quality gate нет. Факты текущей реализации и её разрывы указаны в §11.

## 1. Результат и границы

Специалист отправляет конкретную immutable `ProductVariantPublicationRevision`, наблюдает status/comments/history и при необходимости отзывает Pending iteration. Админ рассматривает exact materialized состав, сравнивает его с текущей Delivery effective revision и добавляет комментарий, одобряет либо возвращает с содержательной причиной. Все специалисты работают с общим контентом; раздел не ограничивается собственными отправками или `CreatedBy` текущего пользователя. Совмещённые permissions дают оба набора действий, включая self-approval; обязательный four-eyes rule не вводится ([Q10](../../../business/questions/Q10-permissions-and-isolation.md)).

F17 проверяет только exact RevisionId. В неё не входят approval SchemaVersion, EntityInstanceVersion, Product/Variant releases, ContentEntitlementPolicy, Unpublish или explicit Withdrawal. Approval не переносится на новую revision по hash/совпадению состава. Исправление требует новых source versions/releases и новой revision в F01–F10; исходный immutable target не изменяется, экран review не предлагает редактирование materialized artifact.

У одной revision одновременно не более одной Pending iteration. Переходы Pending → Approved/Rejected/Withdrawn terminal для iteration. WithdrawPending сохраняет history; ту же exact withdrawn revision можно отправить новой iteration. Возврат Rejected требует исправления источников и создания новых releases/revision, а не перевода старой записи обратно в Pending. Withdrawal revision — отдельный F10 lifecycle, не WithdrawPending F17.

Manual Approval разрешает отдельное Activate в F11 и само не меняет Publishing active или Delivery effective. Для Automatic backend после решения по exact ожидаемой revision durable продолжает конкретную F11 operation; UI наблюдает её результат отдельно. Approved не является сообщением «доступно клиентам». Прежний Publishing pointer сохраняется до подтверждённого transition, прежний Delivery effective — до подтверждённого применения Delivery. Items одного automatic batch решаются независимо; массовое approval/reject не добавляется.

## 2. Страницы, экраны и навигация

| Страница / экран | Точка входа и контекст | Состав и режим | Завершение / переход |
| --- | --- | --- | --- |
| SC01. Список «Модерация» | `/moderation`, общая MVP navigation для специалиста/админа | Context Product/Variant, exact revision, ChangeComment, submit actor/time, status, последний comment/reason и decision actor/time; server filters/paging, read-only rows | Выбор exact ModerationId → SC02; возврат в source workspace по context |
| SC02. Moderation Workspace | `/moderation/{moderationId}`, строка SC01, exact revision F10 или operation F11 | Core iteration, read-only exact revision/provenance, diff, live approval blockers, comments, revision iteration history, доступные действия | Подтверждённый результат обновляет свою iteration/history/list; переход в точный source или F11 |
| SC03. Отправка exact revision | Действие из exact revision F10; RevisionId и owner context уже определены | Exact identity/sources/ChangeComment, текущая допустимость Submit, Confirm/Cancel | Confirm создаёт новую Pending iteration, даёт переход SC02; Cancel без записи |
| SC04. Действие над iteration | SC02: AddComment, WithdrawPending, Approve или Reject; captured ModerationId/RevisionId/own ETag | Action/context; Text только для Comment, Reason для Reject, Confirm/Cancel, pending/error | Confirm выполняет одно действие; Cancel без записи; draft cancellation/retention не придумывается |

Routes `/moderation` и `/moderation/{moderationId}` сохранены как принятый target. Прямой вход/reload SC02 читает exact ModerationId; не выбирает latest revision/iteration при отсутствии или отказе. Review target берётся из authoritative iteration, а не из произвольного UI selection. История показывает все iterations той же revision, включая terminal; выбор historical строки открывает её exact workspace без mutation controls.

Для списка нужны status и Product/Variant context/search, server-side pagination. Точные поля поиска, actor filters, сортировка, размеры страниц, encoding фильтров и сохранение при возврате **уточняются** в [Q13](../questions/Q13-ui-scope.md)/[Q15](../../backend/questions/Q15-api-and-source-code.md). «Отправлено мной» не становится областью доступа и не применяется по умолчанию. Batch grouping, если будет выбран, не объединяет решения разных RevisionId. SC03/SC04 — локальные представления действия; отдельные routes для них не вводятся.

При смене ModerationId или staff identity page исключает поздние ответы предыдущего контекста. Переход к source открывает exact version/release, если это поддерживает его owner UI; не переключает Published/immutable состояние в редактор. Общие правила sign-in/recovery принадлежат F16, без отдельного механизма входа F17.

## 3. Карта FSD-блоков

### 3.1. Необходимые уровни

Pages владеют route и связывают чтения/действия. Widget нужен для совместного review результата core, immutable target и live comparison. Features выполняют отдельные команды. Entities предоставляют presentation/read boundary generated данных. Shared переиспользует transport metadata, controls и ошибки. Новые глобальные stores и слой screens не требуются; app подключает MVP navigation/routes в существующую оболочку F16.

### 3.2. Состав этой фичи

| Блок | Слой / логический срез | Ответственность / использование | Основное описание |
| --- | --- | --- | --- |
| ModerationListPage | pages / moderation-list / ui, model | SC01, фильтры/paging/выбор и список | §3.3, §4 |
| ModerationPage | pages / moderation / ui, model | SC02, exact route/account context, результаты и navigation | §3.3, §4 |
| ModerationWorkspace | widgets / moderation-workspace / ui, model | Координация core/diff/live eligibility/history/forms SC02/SC04 | §3.3 |
| SubmitRevisionForModeration | features / submit-revision-for-moderation / model, api | SC03, Submit exact revision → новая iteration | UI01 / DF01 |
| AddModerationComment | features / add-moderation-comment / model, api | Pending admin Comment; Text без смены status | UI05 / DF05 |
| WithdrawPendingModeration | features / withdraw-pending-moderation / model, api | Pending specialist Withdraw; history сохраняется | UI06 / DF06 |
| ApproveModeration / RejectModeration | features / approve-moderation, reject-moderation / model, api | Независимые admin решения; Reject с Reason | UI07–UI08 / DF07–DF08 |
| ModerationSummary / ModerationHistory / reads | entities / moderation / ui, api | Core/status/actors/comments и история iterations; navigation events | §3.3, DF02–DF04 |
| ReviewDiff / ReviewEligibility / reads | entities / moderation-review / ui, api | Read-only server comparison/basis и live blockers | §3.3, DF10 |
| PublicationRevisionView / source views | Public F10/F09/F01–F04 | Exact immutable content/sources/SchemaVersion/InstanceVersion/provenance | [F10 §3](F10-publication-revisions.md#3-карта-fsd-блоков); локально только read-only review |
| PublicationStateView / PromotionResults | Public F11 | Separate active/effective/automatic continuation результат | [F11 §3](F11-publication-lifecycle.md#3-карта-fsd-блоков) |
| CurrentStaff / FormReauthentication | Public F16 | Current capabilities и same-user recovery ввода | [F16 §3](F16-staff-access.md#3-карта-fsd-блоков) |
| Headers/errors/controls/confirmation | shared / api, ui | Общий DTO+ETag transport, pending/errors/focus | [S05](../standards/S05-ui-and-forms.md), [S06](../standards/S06-api-client.md), [S07](../standards/S07-accessibility.md) |

### 3.3. Входы, выходы и владение

| Блок | Назначение и входы | Выходы / получатель | Собственное состояние | Данные, состав и жизненный цикл |
| --- | --- | --- | --- | --- |
| ModerationListPage | Route/account context, current read access, server-supported filters | ModerationId → navigation; DTO rows → views | Filter/page/read pending/error, generated list result | DF02, shared transport/F16; новый filter исключает late old list; list не владеет own ETag строки |
| ModerationPage | Exact route ModerationId и current staff | Exact context → widget; source/history/F11 navigation; confirmed result → refresh list | Route/context/read lifecycle | ModerationWorkspace, public F16; direct read или объяснимый отказ, не latest fallback |
| ModerationWorkspace | ModerationId, core read result+own ETag, exact revision context, current capabilities | Captured input → features; confirmed core → summary/history/page; refresh F11 по результату | Generated core DTO+own ETag; separate immutable revision, history, diff/basis/eligibility read results; selected action и pending/errors | DF03/DF04/DF10, public F10/F11/F16, summary/diff/history/actions; ошибки отдельных reads не затирают core; смена target очищает чужой context |
| SubmitRevisionForModeration | Exact F10 RevisionId, owner context, eligibility/permission; required ClientOperationId | Original Submit receipt/new iteration identity либо refusal → родитель F10/SC02 | One intent: GUID ID/exact generated original request/target/actor, pending/unknown/error | DF01/shared/F16; same ID/input replay; не выбирает другую revision, не требует старого iteration token |
| AddModerationComment | Exact Pending core, own ETag, permission; введённый Text | Confirmed core/comment result или ошибка → workspace | Generated request с actual Text, original ETag/context, pending/error | DF05/shared/F16 recovery; SC04; очищает отправленный Text только после подтверждения сохранения, при ошибке сохраняет ввод |
| WithdrawPendingModeration | Exact Pending core, own ETag, specialist capability | Withdrawn iteration → workspace/history | Captured context/token/action, pending/error | DF06/shared; SC04; не удаляет history и не вызывает F10 Withdrawal |
| ApproveModeration | Exact Pending core/own ETag, admin capability, live eligibility и shown Delivery-effective comparison basis | Approved либо own/basis/domain отказ → workspace; отдельный F11 refresh | Captured action/context/original own token/shown generated basis/consent, pending/unknown/error | DF07/DF10/shared; basis drift требует fresh read/new consent; не Activate из браузера |
| RejectModeration | Exact Pending core/own ETag, admin capability, actual Reason | Rejected/reason либо ошибки → workspace | Generated request с actual Reason, original ETag/context, pending/error | DF08/shared/F16 recovery; SC04; meaningful reason проверяется сервером, numeric limits не выбраны |
| Summary/history views и reads | Generated core/history DTO от workspace либо exact ID для read | Read data/metadata → owner; selected iteration → page | Read-only input и только раскрытие/выделение UI-state | DF02–DF04/shared; history не редактируется; chronological ordering по server contract |
| ReviewDiff/ReviewEligibility | Exact target и separate server comparison/basis/live result | Generated read result → workspace; basis/context navigation | Read-only DTO; own pending/error/stale presentation | DF10/shared/F10/F11; смена basis требует explicit refresh, не пересборки artifact в браузере |

Features не хранят вторую mutable копию business-модели core. Их локальный ввод использует generated request type напрямую; captured context/token — metadata операции. Workspace один раз принимает подтверждённый результат и обновляет нужные представления. Public source/revision/F11/F16 блоки описаны у владельца и не получают здесь дублирующих карточек.

## 4. Логическая компоновка

```text
SC01. ModerationListPage [pages; filters/page/account]
  Section «Модерация» / current capabilities [F16]
  Server-supported context/search/status filters
  ModerationSummary rows [entities; RevisionId/actors/status/reason]
    Open exact ModerationId → SC02
  Server paging / loading / empty / read failure [shared]

SC02. ModerationPage [pages; exact route context]
  ModerationWorkspace [widgets]
    ModerationSummary: exact iteration/status/actors/timestamps [core + own ETag]
    PublicationRevisionView [public F10; immutable exact target]
      Product/Variant/source release IDs and ChangeComments [F09]
      Materialized Schema/SchemaVersion/InstanceVersion/provenance
      Inherited/Variant additions/Override and exclusion summary
    ReviewDiff [entities; separate live comparison]
      Basis effective RevisionId or confirmed absence / sequence / read time
      Added / Changed version / Removed / loading / stale / failure
    ReviewEligibility [entities; separate live lifecycle/blockers]
      Persistent reason / Approve eligibility / reread
    ModerationSummary comments [read-only]
    ModerationHistory [all exact revision iterations] → exact SC02
    Valid role actions → SC04
    Separate F11 publication/automatic operation result and links

SC03. SubmitRevisionForModeration [feature; exact F10 context]
  Exact RevisionId/source identity/ChangeComment + eligibility
  Один ClientOperationId / original sent input / unknown outcome
  Confirm / Cancel / pending / domain failure

SC04. Captured iteration action [feature; action/context/own token]
  Exact ModerationId / RevisionId / current known status
  Comment: Text | Reject: mandatory meaningful Reason
  Approve: shown comparison basis + отдельный own ETag / fresh consent
  Basis drift409 → refresh comparison / новое согласие, не silent replay
  WithdrawPending: history remains / exact bound auto item cancelled
  Confirm / Cancel / pending / field/form/domain/concurrency errors
  Same-user recovery [public F16 when authentication expires]
```

SC02 постоянно показывает identity и immutable review target; diff/eligibility/history имеют собственные loading/errors. Workspace передаёт выбранную iteration и token действию, действие возвращает только подтверждённый result/error. History не заменяет core token. Подтверждённый comment/status refresh обновляет core/own ETag, history и соответствующую list строку либо list read; live eligibility/diff/F11 обновляются своими reads. Навигация и UI-вложенность дерева не разрешают зависимость feature от page/widget.

## 5. Зависимости и публичные границы

F17 потребляет серверные операции [Backend F17](../../backend/features/F17-content-review.md), exact immutable presentation [F10](F10-publication-revisions.md) и exact source details [F09](F09-content-releases.md). F10 владеет моделями revision/content/lifecycle и source selection; F17 не копирует их и не превращает diff в новую release/revision. Материализованный review target не пересобирается по latest источникам.

[F11](F11-publication-lifecycle.md) владеет manual activation, automatic operation и раздельными Publishing active/Delivery effective reads. Approve feature не оркестрирует F10 Create/F17 automatic Submit либо F11 Activate HTTP calls: prepare tuple и continuation являются серверными обязанностями; source/parent change, Archive либо Open F15 не обходятся поздним Approval. Принятые head/Product-Version/Variant-Sequence/terminal applicability rules задаются Q20/F11. [F15](F15-catalog-conflicts.md) предоставляет объяснимые catalog blockers; они блокируют применимые Submit/Approve, а не весь экран, Reject/WithdrawPending/comments. [F16](F16-staff-access.md) проверяет current staff и обеспечивает recovery; own moderation ETag не принадлежит CurrentStaff. [F18](F18-staff-activity-history.md) дополняет предметную history audit, но не заменяет её.

Допустимый поток зависимостей: page → widget/actions/entities → shared; features используют публичные reads/API boundaries entities и shared. Нет зависимости браузерного UI на server Domain/EF, page/widget из feature или соседние private stores. Соседний workspace получает navigation ID/confirmed refresh event, а не импортирует internal state F17.

## 6. Данные и владение состоянием

### 6.1. Источник истины и область жизни

| Данные | Источник истины | Владелец клиентского состояния | Изменение / сброс |
| --- | --- | --- | --- |
| Core iteration status/actors/comments | Backend F17 exact core read DF03 | ModerationWorkspace: generated DTO + отдельный own strong ETag | Подтверждённая mutation или exact reread; изменение target/account исключает чужие ответы |
| Immutable review target | F10 exact RevisionId/content/source/provenance read | Workspace получает public generated/read-only presentation F10 | Смена target; не Create/compose/edit и не latest reload |
| Live lifecycle/Withdrawal/catalog blockers | Separate current read DF10/F10/F15 | Workspace, отдельный read result/pending/error | Перечитывание, изменения eligibility; own moderation ETag их не version-ит |
| Diff и его basis | Server comparison с current Delivery effective | Workspace, отдельный generated result/basis и read time | Refresh; failed read не становится empty diff |
| Shown comparison consent basis | Generated DF10 basis, фактически показанный администратору | Approve feature хранит exact basis отдельно от own moderation ETag | Confirmed basis drift → independent409, refresh/new consent; basis не вычисляется по current pointer/hash в browser |
| Submit intent | Required GUID ClientOperationId + original generated input/exact RevisionId/actor | Submit feature до выяснения outcome | Same ID/input original receipt, changed payload/новое намерение — новый ID; lost ACK не blind new ID |
| Revision iteration history | DF04 для exact RevisionId | Workspace generated read result | Confirmed action или явный history refresh; list token не извлекается |
| List rows/filters/paging | DF02 + локальный UI selection | ModerationListPage | Изменение фильтра/страницы/account, безопасное перечитывание |
| Text/Reason | Actual local input в generated request | Соответствующая action feature | Confirmed save/explicit cancel по согласованному поведению; при ошибке и reauth ввод удержан |
| Captured mutation | Generated request + original own ETag/ModerationId/RevisionId/staff/action | Feature; recovery container F16 переносит snapshot | Подтверждение/выяснение исхода/отмена; fresh read не заменяет original baseline |
| Automatic continuation / active / effective | Public F11 reads | F11 owner; workspace получает read-only result | Separate refresh после Approved; не локальное выставление active/effective |

Core own ETag покрывает только собственные mutable сведения iteration: status, её actors/timestamps/decision/reason/comments в согласованной representation. Immutable revision data и live eligibility/diff basis не считаются versioned тем же token. Если backend wire response композиционный, границы representation/versioning должны быть явно выбраны в Q15; UI не распространяет один own ETag на весь composite. Равный token не доказывает отсутствие нового Withdrawal или CatalogConflict.

### 6.2. Состав представления и формы

| Сведение / поле | Источник | Использование | Правило |
| --- | --- | --- | --- |
| ModerationId / exact RevisionId / Product/Variant context | Core F17 и exact F10 owner read | Identity/navigation; target всех actions | Не подменять по hash/latest/source release |
| Status, SubmittedBy/At, WithdrawnBy/At, DecidedBy/At, reason/comments | Generated F17 core/history | Summary/list/history, actors/time | Terminal iteration read-only; self-approval наблюдаем |
| ChangeComment, exact source releases/versions/provenance/exclusions | F10/F09 owner reads | Read-only review и ссылки к источникам | Не редактировать artifact и не копировать содержимое в F17 aggregate/client model |
| Added / Changed version / Removed | Generated server diff DF10 | ReviewDiff с basis label | Stable EntityInstanceId сопоставляет обычную instance identity; Override показывает реальный OverrideResultId/version. Имя/key, позиция строки и inherited target не заменяют identity итогового override |
| Effective basis RevisionId либо подтверждённое отсутствие | Delivery read/comparison contract | Label рядом с diff, sequence/time там, где доступны | Comparison относится к current Delivery effective, не Publishing active/Product base; no effective и failed read различаются |
| Current eligibility и причины | Live DF10 | Persistent blocker, disabled Approve; refresh | Withdrawal/новый blocker проверяются сервером на Approve даже с matching own ETag; source release Retire сам по себе не инвалидирует materialized revision |
| Comment Text | Generated command request, ввод админа | SC04 | Pending только; содержательная серверная валидация по контракту, limits в Q15 |
| Reject Reason | Generated command request, ввод админа | SC04 | Обязательная содержательная причина; whitespace не считается осмысленным вводом |

Field-level JSON diff не является обязательным default MVP. Если current effective отсутствует подтверждённо, server comparison явно сообщает эту basis и additions относительно отсутствующего состава; UI не делает такой вывод из ошибки/timeout. Approve захватывает basis именно показанного comparison и отдельный own moderation ETag. Независимое изменение basis до защищённого decision commit даёт409: перечитать diff, показать новое основание и получить новое согласие; unchanged own token не разрешает replay прежнего согласия. Неизвестный/unavailable comparison не является confirmed empty basis. После committed Approved поздняя смена comparison сама по себе не отзывает decision; F11 activation имеет свои current guards. Формат basis/sequence/time и availability metadata остаётся Q15, browser не выполняет composition.

Generated модели используются напрямую для read и command input; отдельные ручные DTO/ViewModel/FormModel/EditorModel с теми же полями не вводятся. ETag/pending/selected action/errors/captured context — чистое UI/transport state, без технического Version поля в DTO. Browser storage, autosave, offline и глобальный store для формы не назначены.

## 7. Действия и клиентские потоки

| Действие | Начальное состояние и запуск | Исполнитель | Обработка / серверная операция | Результат в интерфейсе |
| --- | --- | --- | --- | --- |
| UI01. Submit | Exact revision F10, specialist permission, применимая eligibility; SC03 | SubmitRevisionForModeration | Создать один ClientOperationId → capture original exact RevisionId/actor/input → Confirm → DF01; без If-Match старой iteration | Original receipt/Pending iteration identity; history отдельно, переход SC02 |
| UI02. Найти отправку | SC01/filter/page | ModerationListPage | DF02 с server-supported фильтрами/paging | Rows либо отличимый empty/error; exact selection → SC02 |
| UI03. Открыть review | SC02/direct route/reload | ModerationPage / ModerationWorkspace | DF03 core/context, exact F10 target; separate DF10/live/diff | Exact core+own ETag и immutable content; отдельные errors не маскируются |
| UI04. История revision | SC02/history refresh | ModerationWorkspace / ModerationHistory | DF04 exact RevisionId | Все iterations/actors/comments/reasons; selection только navigation |
| UI05. Comment | Pending/admin; SC04 Text | AddModerationComment | Validate input → capture own ETag → DF05 | Confirmed comment без final status change, новый core/token/history; Text cleared только при confirmed save |
| UI06. WithdrawPending | Pending/specialist; SC04 | WithdrawPendingModeration | Capture own ETag → DF06 | Withdrawn history; exact bound auto item Cancelled; новая manual iteration не reattach-ится к прежнему item |
| UI07. Approve | Pending/admin; показанный confirmed comparison/live eligibility; SC04 | ApproveModeration | Capture own ETag/context/SHOWN generated Delivery basis и согласие → DF07; server rechecks protected basis/eligibility | Approved либо own412/independent basis409/current blocker; basis409 требует refresh/new consent; F11 result отдельный |
| UI08. Reject | Pending/admin; SC04 meaningful Reason | RejectModeration | Validate Reason → capture token/context → DF08 | Rejected terminal/reason/history; переход к source для новых releases/revision |
| UI09. Наблюдать automatic continuation | Exact Approved связана с ожидающей F11 operation | Workspace через public F11 | DF09 выполняет backend; UI перечитывает F11 operation/state | Независимый confirmed/blocked/failed результат конкретного Variant; active и effective отдельны |
| UI10. Обновить comparison/eligibility | SC02/open/explicit reread | Review reads / ModerationWorkspace | DF10 с exact target/current basis context | Новые basis/diff/blockers либо read error; core token не заменён этими reads |
| UI11. Восстановить после истечения доступа | 401 при local Text/Reason/action | Public F16 recovery + исходная feature | Snapshot actual request/input, original token/target/staff → sign-in → проверка same staff/access/context | Ввод восстановлен в исходном exact context; нет automatic mutation replay или замены original ETag |
| UI12. Отмена/переход | SC03/SC04 до отправки | Feature/page | Cancel без запроса; navigation по page | Нет сохранения; retention при явной отмене/reload/logout уточняется в F16/Q24 |

Во время одного action повторная отправка заблокирована. При смене target/action/accounts старый ответ не обновляет новый экран; отправленная mutation могла выполниться на сервере даже после ухода. Timeout/обрыв после отправки не доказывают отказ. Safe exact core/history reread помогает выяснить результат, но комментарий не повторяется автоматически: сравнение Text само по себе не является гарантией уникальности записи. Idempotency/correlation unknown-result contract остаётся в Q15.

Submit execution использует [Q15 ClientOperationId](../../backend/questions/Q15-api-and-source-code.md#publishing-command-execution-и-clientoperationid): при unknown сохраняются original ID/target/input/actor; same ID/input после current auth возвращает original receipt до eligibility checks нового исполнения, даже если iteration уже terminal или target Retired. Bound ID/different input409 отличается от one-Pending/domain409. Новый ID создаётся только для нового осознанного Submit intent, не автоматически после lost response. Receipt не выдаётся за current moderation state; subsequent actions требуют exact core/token. Comment/Approve/Reject/Withdraw recovery не получает выдуманный ClientOperationId contract автоматически.

Automatic revision и её Pending iteration/item binding уже созданы одним Publishing prepare commit F11; browser не вызывает Submit для готового automatic item и не связывает существующий manual Pending. Human decision и continuation activation — отдельные commits. Cancel/Supersede item не Withdraw-ит human iteration; Approval остаётся history/manual-valid при current eligibility, но terminal old item никогда не возобновляется. New manual Submit после Withdrawn не reattach-ится к cancelled automatic item. Rejected сохраняет старый active; duplicate decision не активирует result повторно. [Q20 workflow](../../backend/questions/Q20-persistence-and-migration.md#publishing-workflow-capture-и-продолжение).

412 сохраняет local Text/Reason и ORIGINAL own ETag, показывает свежую iteration отдельно и требует осмысленного следующего действия. Нельзя автоматически повторить old request с fresh token или перезаписать final decision. 409 live blocker либо одна Pending на revision — предметный конфликт; token сам по себе его не устраняет. Backend не переводит invalid Pending в Rejected автоматически: остаются Pending/history, persistent reason и disabled Approve. WithdrawPending/Reject/допустимый Comment доступны по своим permissions/state, даже когда approval заблокирован.

F16 recovery возвращает actual несохранённый ввод только тому же staff, с прежними ModerationId/RevisionId/action/token, а не latest context. Fresh post-login read может выявить terminal state, 404, изменённый доступ или новый blocker; ввод сохраняется как исходная работа без обещания допустимости записи. Другой аккаунт не видит/не применяет snapshot. Recovery не подменяет проверку доступа каждого protected request и не повторяет неизвестную mutation.

## 8. Использование API и NSwag

| Потребность / поток | Серверная фича и операция | Входные сведения | Используемый результат | Владелец вызова |
| --- | --- | --- | --- | --- |
| UI01 Submit | Backend F17 DF01 | Required GUID ClientOperationId в body; exact RevisionId из F10, actor через auth boundary | Original execution receipt/iteration identity либо typed refusal | Submit feature |
| UI02 Queue | F17 DF02 | Accepted status/context/search/page параметры | Generated list rows/paging metadata | List page/read boundary |
| UI03 Core detail/review context | F17 DF03 + F10 exact owner read | Exact ModerationId; authoritative target из результата | Generated core+own ETag; immutable target/context отдельно | Workspace/entity read + public F10 |
| UI04 Revision history | F17 DF04 | Exact RevisionId | Iterations/actors/comments/reasons | History read/workspace |
| UI05 Comment | F17 DF05 | ModerationId, generated Text request, own If-Match | Confirmed core/comment/new ETag либо ошибки | Comment feature |
| UI06 WithdrawPending | F17 DF06 | ModerationId, own If-Match | Withdrawn core/new ETag/history result | Withdraw feature |
| UI07 Approve | F17 DF07 | ModerationId, own If-Match + отдельный shown Delivery-effective comparison basis carrier Q15 | Approved core/new ETag либо own412/basis409/current guard refusal | Approve feature |
| UI08 Reject | F17 DF08 | ModerationId, generated Reason request, own If-Match | Rejected core/new ETag/reason либо validation | Reject feature |
| UI09 Automatic continuation | F17 DF09 → F11 public operation/state reads | Exact known operation/Variant/revision context | Отдельный transition/propagation result | Backend continuation; UI использует public F11 reads |
| UI10 Live review | F17 DF10 и владельцы F10/F11/F15 | Exact target/current Delivery comparison context | Generated diff+basis, lifecycle/blockers/eligibility, separate read errors | Review read/workspace |

DF — обязанности [Backend F17](../../backend/features/F17-content-review.md), не имена существующих NSwag методов. Прежние target routes `/api/v1/publication-revisions/{revisionId}/moderations`, `/api/v1/moderations` и subordinate detail/comments/withdraw/approve/reject остаются legacy target surface; окончательные wire routes, request/response DTO, HTTP status/error codes, permission identifiers и history/diff boundaries **уточняются** в [Q15](../../backend/questions/Q15-api-and-source-code.md). Эти примеры не доказывают наличие endpoints. Actor не выбирается пользователем в форме; exact способ auth/material определяется F16/Q24.

Подтверждённая общая генерация — [nswag.json](../../../../contentmanager.ui/src/ContentManager.ApiClient/nswag.json), результат — [ContentManagerApiClients.g.cs](../../../../contentmanager.ui/src/ContentManager.ApiClient/OpenApi/Generated/ContentManagerApiClients.g.cs). [ConfigureApi](../../../../contentmanager.ui/src/ContentManager.ApiClient/DependencyInjection/ApiClientWebAssemblyHostBuilderExtensions.cs) подключает named HttpClient и envelope handler, [AddContentManagerApiClients](../../../../contentmanager.ui/src/ContentManager.ApiClient/DependencyInjection/ApiClientServiceCollectionExtensions.cs) регистрирует generated clients по marker interface. [ApiResponseException](../../../../contentmanager.ui/src/ContentManager.ApiClient/Infrastructure/Responses/ApiResponseException.cs) предоставляет StatusCode/ValidationErrors; F17 требуется сохранить distinctions ошибок и per-call headers до своих владельцев. Наличие этой инфраструктуры не означает готовность moderation API.

Exact mutable core read возвращает own strong ETag. Workspace хранит его рядом с generated core DTO; action передаёт сохранённый token в If-Match. Submit новой iteration не использует ETag старой iteration. List/history/diff и F10 revision token не заменяют core own ETag. После подтверждённой записи UI получает новый token из согласованного response либо перечитывает exact core; до этого последующая mutation не отправляется с предположенным token. Поддержку headers текущей генерацией для F17 не объявляем: при разрыве исправляется общая генерация/transport и регенерируется NSwag, generated files вручную не меняются.

Matching own token не освобождает Approve от server reread/revalidation текущих Withdrawal/dependencies/blockers. Core mutation response не доказывает актуальность отдельно загруженного diff или delivery state. Client mapping ограничивается generic metadata/UI-state; business model и semantic diff не вычисляются вместо серверного владельца. Подробности контрактов/versioning/error priority не выбираются по порядку client checks.

Submit/decision и F11 continuation используют выбранный MVP writer protocol [Q20](../../backend/questions/Q20-persistence-and-migration.md): fresh lifecycle/catalog/F15/runtime output dependency facts и shown comparison basis защищены до owner commit. Preview не permit; отдельный captured basis служит проверяемым основанием согласия Approve, не заменяет own ETag. Evidence-only suppressed dependency не блокирует final effective instance/schema/media closure; shared output dependency проверяется. Technical failure не Rejected/foreign409/новый CatalogConflict. Lost decision ACK остаётся unknown по Q15 без fresh-token replay. Approved history и F11/Delivery results отдельны; точные head/Version/Sequence/terminal continuation rules приняты в Q20.

## 9. Состояния, ошибки и доступ

| Ситуация | Что видит пользователь | Доступные действия | Данные / восстановление |
| --- | --- | --- | --- |
| Technical gate/read failure | Координация/проверка недоступна, не domain409/новый CatalogConflict | Сохранить ввод/context; safe read | Не empty valid result; lost write acknowledgement остаётся unknown по Q15, без blind retry/new token |
| Demo | Нет обязательной moderation navigation/gate | Demo content flows | Не создаётся скрытая dependency на F16/F17 |
| List/core loading | Ожидание соответствующей области, exact identity где подтверждена | Безопасная навигация; mutation после core+token/current access | Late result привязан к исходному context |
| Пустой список/нет совпадений | Empty state отдельно от read error | Изменить фильтр/вернуться к revision | Отсутствие данных не считается отказом и наоборот |
| Diff/history/eligibility refresh failure | Ошибка именно этого read; прежний comparison с basis/time и признаком непроверенной актуальности | Safe reread; иные actions по core/state/permissions | Failure не empty diff/confirmed no-effective; Approve требует shown confirmed basis/current guards; later diff drift не отзывает уже committed Approval |
| Comparison basis changed / independent409 | Новое effective основание отличается от показанного при прежнем own token | Refresh comparison → показать различия → новое согласие | Original basis/token сохранены отдельно; no silent replacement/retry старого Approve |
| Pending action | Указаны action/target и ожидание | Дубль заблокирован; Cancel до отправки без записи | Нет локального final status/success |
| Confirmed Comment/decision/withdraw | Сохранённый server result и обновлённая core/history | Следующее допустимое действие | Token из confirmed response/exact read; terminal actions закрыты |
| Approved manual | «Одобрено», отдельная publication state | Specialist Activate через F11, если допустимо | Active/effective не выставляются локально |
| Approved automatic | «Одобрено», отдельный F11 operation результат/ожидание | Safe F11 reread, переход к Variant | Нет instant activation/Delivery success; blocked/failed continuation не отменяет history Approval |
| Terminal iteration | Approved/Rejected/Withdrawn с actors/reason/history | Read/navigation; новый Submit withdrawn revision через F10 flow | Нет terminal decision overwrite, edit/delete comments или chat-thread; такие расширения не выбраны |
| Invalid Pending | Persistent current blocker и disabled Approve | WithdrawPending/Reject/Comment по их state/permissions, исправление источника через owner | Pending не auto-Rejected; source Retire alone не выдаётся за invalidation |
| Field/form validation | Ошибка Text/Reason/операции по контракту | Исправить actual ввод, повторить осознанно после pending | Ввод не очищается, успех не показан |
| 401 / истёк доступ | Нужен sign-in/recovery F16 | Same-user восстановление и проверка context, без replay | Actual Text/Reason и original own token/target сохранены; fresh read не заменяет baseline |
| 403 / permission/account revoked | Отказ конкретной области/действия | Только актуально разрешённые действия | Нет relogin loop, local role не даёт право; snapshot не раскрывается другому staff |
| 404 exact iteration/target | Exact объект недоступен/отсутствует по контракту | Безопасный возврат/navigation | Не выбирать latest; не маскировать произвольно access refusal |
| 412 own core | Другой actor изменил iteration, current state прочитан отдельно | Сравнить, перечитать, выбрать допустимый следующий шаг | Actual Text/Reason/original ETag сохранены; нет retry с fresh token/last-write-wins |
| 428 missing token | Ошибка подготовки mutable action | Восстановить exact core flow | Не выключать concurrency, не брать list token |
| 409 foreign/live domain conflict | Одна Pending, terminal/current lifecycle либо live blocker по server code | Safe relevant reread/исправить причину/отмена | Не переименовывать domain conflict в own 412; matching token не устраняет blocker |
| Read network failure | Ошибка затронутой области | Повторить safe read | Не стирать безопасно доступный core только из-за отдельной refresh ошибки |
| Timeout/обрыв после mutation | Исход не определён | Выяснить результат через согласованный contract/read | Нет automatic resubmit/duplicate comment/ложного отказа; correlation/idempotency в Q15 |
| Другой staff / late old response | Только рабочая область нового context/account | Его разрешённые действия | Чужой input/result не показывается и не переносится на другую iteration |

Specialist Submit/WithdrawPending и admin Comment/Approve/Reject проверяются независимо. Admin-only не получает authoring действия только по admin role; specialist-only не получает решение. Combined staff может approve свою submission. Permissions проверяются сервером для каждого protected read/action по F16, включая актуальные роли и active account; UI hiding/disabled лишь объясняет доступ. Stale current-staff presentation не является доказательством права.

Comment доступен админу пока iteration Pending по выбранному backend правилу; терминальное редактирование/удаление comments и ответы специалиста не входят в MVP. Значимый Reject Reason обязателен; max length и остальные точные validation codes остаются Q15. F15 blocker не превращает весь workspace в недоступную область.

По [S05](../standards/S05-ui-and-forms.md)/[S07](../standards/S07-accessibility.md) поля имеют понятные подписи, обязательность/error не показываются только цветом. Основные действия/selection доступны с клавиатуры. При SC03/SC04 и возврате из sign-in фокус остаётся в исходном допустимом context; pending, reason и conflict понятны без знания transport. Layout, клавиши и browser/storage retention не выбираются здесь.

## 10. Проверки и приёмка

| Проверяемый блок / сценарий | Исходные условия и воздействие | Наблюдаемый результат | Граница проверки |
| --- | --- | --- | --- |
| Protected commit / technical failure | Foreign lifecycle/catalog/source changes между preview и commit; bounded gate wait/cancellation/read failure; lost ACK | Server revalidates protected facts; own412/domain409/technical failure различимы; ввод/basis/token сохранены, no false success/empty valid/blind retry | UI/transport doubles для representation; real PostgreSQL owner writers/barriers для guard, recovery contract Q15 отдельно |
| SC01/SC02 exact navigation | List row и direct route; две iterations одной revision; reload/404 | Открыта указанная iteration и её immutable target, latest fallback отсутствует | Page composition/API doubles; реальный read отдельно |
| Shared content и combined roles | Два специалиста, admin-only и combined staff; own submission | Specialists видят общий контент; действия независимы; combined self-approval не скрыт | UI composition + реальные server authz cases |
| Submit/WithdrawPending/history | Pending creation/race второй Submit/Withdraw; повтор withdrawn revision | Одновременно одна Pending; terminal история остаётся; повтор создаёт новую iteration/ID | Actions/API; server uniqueness через backend integration |
| Reject correction | Reason empty/whitespace/meaningful; Reject confirmed | Validation/Rejected reason; источник исправляется новой release/revision, immutable target не редактируется | Feature/component/API и совместный F10/F17 путь |
| Comment | Pending/admin, validation/confirmed save/lost response | Text удержан при ошибке; cleared после confirmed save; status не становится final, дубль не replay | Component/transport; реальная запись отдельно |
| Semantic diff | Target с обычной instance и override; current effective отличается от active/base | Added/Changed version/Removed по правильной basis; stable identity и real OverrideResultId/version различимы | Server diff integration + read-only client presentation |
| Basis/read failure | Confirmed no-effective; effective меняется до Approve; timeout reread | Separate generated shown basis+own ETag; drift409/new consent; unavailable не empty; committed Approval later basis drift не отзывается | Workspace/transport + protected real Delivery/Publishing writers |
| Submit intent | Lost ACK; same ID после terminal/target Retire; changed input; intentional new manual iteration после Withdrawn | Original receipt replay/no blind new ID; changed input409; новая iteration не auto-item binding | Transport + real execution/Pending constraints |
| Exact automatic tuple / terminal | Auto prepare; Cancel/Supersede; iteration Withdraw/new manual Submit; duplicate Approval | Browser Create→Submit отсутствует; exact tuple only; old item terminal/no reactivation; human history retained | F11/F17 real prepare/decision/activation + UI |
| Output restriction closure | Suppressed A Withdrawn/A-only asset unavailable, либо действующее ограничение schema/media shared с B | Evidence-only dependency не block Approve; runtime B closure invalid blocks, Pending/history сохранены; новый Media Withdrawal state не вводится | Real revision/moderation/activation/distribution guards |
| Own ETag | List row без token, exact core token A, Comment/status mutation | Exact own If-Match A; token из confirmed response/read; F10/live/history tokens не подставлены | Shared headers и feature interaction |
| Concurrent decisions | Два админа core A; первый final confirmed; второй 412/предметный terminal conflict | Один final result; второй reload/current decision; original input/token сохраняются, no overwrite | Реальный backend concurrency + UI errors |
| Foreign live state | Own token совпадает; после Submit новый Withdrawal/catalog blocker; отдельно source release Retire | Approve server validation отказывает при invalidity, Pending/history сохранены; Retire alone не объявлен invalid | Реальный backend checks + UI persistent blocker |
| Partial blockers | Invalid Pending, разные permissions | Approve disabled; WithdrawPending/Reject/Comment по своим правилам доступны | Component/API error behavior |
| Manual approval | Confirmed Approved, active/effective старые | UI не объявляет activation; отдельный F11 action/reads | F17/F11 composition/API |
| Automatic approval | Exact awaiting operation/revision; approved, continuation blocked/failed/delayed | Независимый per-Variant result; прежние pointers до confirmed transitions; нет browser Activate orchestration | Backend durable continuation + UI; E2E для полного пути |
| Same-user reauth | Actual Text/Reason отличается от server; 401; fresh core token B после входа | Same staff input/target/original A восстановлены; B только current read; нет mutation replay/blind replace | F16 recovery/client logic + composition/API doubles |
| Account/access/context changes | Staff A вводил, вошёл B; pending стал terminal, 404 или permission revoked | Нет чужого input/new-target overwrite; понятен отказ/конфликт, нет relogin loop | Recovery/page coordination + реальные authz cases |
| Unknown mutation outcome | Server мог сохранить Comment/decision, ответ потерян | Нет доказанного failure/success; safe выяснение без duplicate/replay | Transport/action coordination; гарантия retry после выбора Q15 |
| Late response | Смена ModerationId/filter/account во время read/action | Старый ответ не подменяет новый context и не раскрывает чужие данные | Изолированная async coordination |
| Scope и доступность | Открыть terminal target, keyboard actions/error/recovery | Нет generic approval иных сущностей/bulk decision/edit immutable/chat; понятны status/reason/focus | UI composition/ручная проверка без CSS acceptance |

Для future UI поведения применим [S08](../standards/S08-testing.md), без назначения test library этим документом. Изолированная логика/компоненты используют API/auth doubles; они не доказывают server role validation, uniqueness или durable automatic activation. E2E MVP требует реальных F16, F10/F17/F11 и Delivery для подтверждения цепочки exact approval → transition → effective. Отдельно проверяется source historical use и lifecycle invalidation, а не подмена реального backend клиентскими mocks.

Это критерии будущей реализации. При обновлении документа выполнены source inventory и проверки структуры/ссылок; application tests, browser flow, Identity и moderation endpoints не запускались. Наличие документа/NSwag проекта не является проверкой реализованного F17.

Приёмка automatic continuation: Approval history и publication result различимы; смена source/parent, Archive или Open F15 до continuation требует fresh scope/basis проверки без stale activation. Repair/Resolve не создают Submit/Approve/публикацию автоматически; целевой immutable revision/history не переписывается.

## 11. Последовательность изменений и открытые вопросы

Сначала согласовать backend owner core/composite boundaries, wire operations/errors/permissions и diff basis в Q15; server history/concurrency/automatic continuation persistence — в Q20. После OpenAPI подключить generated clients/headers без ручной правки generated code. Далее переиспользовать public exact revision/source/F11/F16 boundaries, собрать list/workspace/action features с одним владельцем core/token и отдельными live reads. Recovery actual Text/Reason/original token/context подключить одновременно с protected actions. Затем проверить states/own concurrency/foreign invalidation/unknown outcomes и реальные auth/durable continuation E2E. Массовый рефакторинг соседних фич и новые frameworks не требуются.

Текущее source inventory: в `contentmanager.ui/src` среди `.cs`/`.razor` вне bin/obj не найдены moderation names/routes; в [generated client](../../../../contentmanager.ui/src/ContentManager.ApiClient/OpenApi/Generated/ContentManagerApiClients.g.cs) нет подтверждённых F17 methods/models. [PublicationWorkspacePage](../../../../contentmanager.ui/src/ContentManager.Pages/Components/Publishing/PublicationWorkspacePage.razor) — существующая Publishing page, не Moderation Workspace. Подтверждены лишь общая NSwag/DI/error infrastructure из §8. Точные namespaces/classes будущих moderation blocks и server endpoints не восстанавливаются по целевым названиям; после выбора контракта нужна регенерация и отдельная проверка.

| Вопрос | Какие детали блокирует |
| --- | --- |
| [Q28. Content review scope](../../../business/questions/Q28-content-review-scope.md) | Основная MVP семантика принята: exact target, iterations, independent decisions, approval/activation, self-approval, invalidation. Appeal/re-submit unchanged Rejected/AlreadyApproved и новые terminal comments/chat/four-eyes/bulk решения требуют отдельного выбора, здесь не добавляются |
| [Q10. Permissions](../../../business/questions/Q10-permissions-and-isolation.md) | Shared content и независимые specialist/admin полномочия приняты; точные permission identifiers/API mapping согласуются в Q15/F16 |
| [Q15. API/source](../../backend/questions/Q15-api-and-source-code.md) | Exact routes/DTO/status/errors, core representation, history/list, shown comparison basis carrier/headers и action recovery уточняются; own412/basis409/new consent принято |
| [Q15. Submit execution](../../backend/questions/Q15-api-and-source-code.md#publishing-command-execution-и-clientoperationid) | Required ClientOperationId/exact intent/original receipt принято; transport/recovery representation уточняется |
| [Q20. Workflow](../../backend/questions/Q20-persistence-and-migration.md#publishing-workflow-capture-и-продолжение) | Atomic automatic prepare tuple, independent decision/activation, exact binding/terminal/head/ABA guards приняты; UI не выбирает physical tables/infra |
| [Q13. UI scope](../questions/Q13-ui-scope.md) | Общий moderation раздел и target routes приняты; уточняются точные search/filter/sort/page параметры, selected context/retention и возможное batch grouping без объединения решений |
| [Q24. Staff sign-in](../questions/Q24-admin-sign-in.md) | Auth protocol/provider, storage/TTL/logout/reload/cancel/tab recovery mechanics; обязательные same-user input/original token recovery и no replay уже определены в F16 |

Полные вопросы остаются у владельцев. Backend models/алгоритмы находятся в Backend F17; immutable target — в F10; continuation/publication state — в F11; общий модуль хранит связи без копирования карточек frontend blocks.
