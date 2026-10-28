# F15. Разрешение конфликтов каталога: фронтенд

[Бизнес](../../../business/features/F15-catalog-conflicts.md) · [Бэкенд](../../backend/features/F15-catalog-conflicts.md) · [M03](../modules/M03-catalog.md) · [Q25](../../../business/questions/Q25-catalog-conflicts.md#f15-технические-границы-разрешения).

Документ описывает целевой MVP UI на подтверждённом [стеке](../README.md): .NET 10, Blazor Standalone, MudBlazor, FSD и NSwag. Имена будущих блоков — логические обозначения, не существующие классы или готовые generated API. Факты исходников отделены от цели в §11; вёрстка не задаётся.

## 1. Результат и границы

Специалист находит Open Product/ProductVariant conflict, сравнивает whole AppliedState (либо wholly absent до first apply) с одним newest candidate и видит current assessment отдельно от historical captured Issues. Durable acceptance v22 исключает21 before worker; ACK/acceptedVersion не applied/readiness. Конкретные mutable/in-flight affected blockers сообщает сервер. User corrective repair и отдельный Resolve сохраняются; также latest compatible source correction может дать System SourceChangedApply. Persistent restriction снимается только fresh confirmed server state, не чтением/toast/закрытием диалога.

MVP включает перенос ProductVariant между Product. Сам перенос допустим; конфликт возникает из-за несовместимых локальных последствий. ProductVariantId сохраняется. Пока перенос pending, каталог и canonical Variant workspace используют applied parent по [F06](F06-product-catalog.md). Newest pending parent показывается отдельно и не становится текущим Product только потому, что snapshot получен.

Конфликт ограничивает **новые записи только в затронутом scope**: зависимые assignments/overrides [F07](F07-product-content-assignment.md)/[F08](F08-variant-content-assignment.md), создание releases [F09](F09-content-releases.md), создание/activation revisions [F10](F10-publication-revisions.md), automatic promotion [F11](F11-publication-lifecycle.md), неоднозначные policy updates [F12](F12-content-entitlement-policy.md), новую moderation submission при невозможности достоверной валидации [F17](F17-content-review.md). Конкретные scope и причины сообщает backend, UI не выводит их из совпадения имён или локальных связей. Unrelated scope продолжает обычную работу.

Чтение каталога, conflict UI, существующее состояние и история доступны по правам. Historical materialized Delivery по ранее подтверждённому entitlement, включая новые sessions, продолжается, если отдельно нет Withdrawal/revoke/entitlement prohibition. Resolution не переписывает saved releases/revisions и не пересчитывает их composition. Новая публикация после move выполняется отдельным обычным Publishing workflow; Resolve её не создаёт и не активирует.

VR School остаётся владельцем каталога. В CM нет альтернативного parent/«оставить локального родителя»/Ignore/Force/source-write/sync/reconciliation button. Source correction выполняет уполномоченный VR School user; ordinary V2 receiver/worker переоценивает newest accepted state. Compatible correction атомарно full-apply+Resolved того же episode с System/SourceChangedApply history, без fake user Resolve/publish/autoresume/history rewrite. Source owner link/контекст Q13/Q25; protected operational VRS reconciliation не CM HTTP command/permission.

По [Q23 V2](../../backend/questions/Q23-source-synchronization.md#принятый-протокол-каталога-v2) newer accepted target обновляет **тот же Open episode**; previous Issues stale до assessment newest. Missing parent сохраняет Open+waiting, noTTL/fake Product; first-apply missing parent без episode не превращается автоматически в F15 conflict. Archived parent — отдельный lifecycle restriction. Trusted identifiable higher-version malformed/unsupported input создаёт scoped IngestionFault/unreadiness F06, не новый manual F15 conflict; old candidate не применяется, obsolete Issues не заменяют technical fault. Unknown identity не даёт guessed entity block. Fault alone не отзывает historical Delivery.

**Ordinary affected Save/release/publishing операции при Open заблокированы.** Отдельный corrective Publishing workflow backend DF07 исправляет Variant patch: удалить конфликтующий override/local addition, заменить проблемную result reference eligible reference либо добавить inherited-only exclusion, удаляющий blocker. Это эффект для состава: исправление не расширяет контент, independent additions запрещены, intrinsic/scope/new-use проверки обязательны. Это не общий local catalog edit, Ignore/Force или alternate parent. Historical immutable release/revision сами по себе не blocker; impact показывает конкретную mutable/in-flight affected closure. Override A → B при отсутствии A в prospective базе сохраняет B local и сам по себе допустим; независимая B новой базы + результат A → B создают duplicate blocker.

Repair preview отдельно показывает current applied, prospective newest parent и proposed repaired состав. Ordinary workspace не переключается от repair save: только full applied server result (manual Resolve либо compatible SourceChangedApply) меняет parent. Repair фиксирует ConflictId/newest SourceVersion/shown basis/own patch If-Match; source/patch change требует нового preview/consent. Exact carrier Q15, protected newest-source/patch/dependency facts до каждого commit — [Q20](../../backend/questions/Q20-persistence-and-migration.md).

Два шага не объединяются: repair сохраняет только patch; отдельный Resolve повторно проверяет fresh dependencies/newest source и применяет relation, закрывая conflict и сохраняя history. Repair success не означает move success. Новый source v22 между repair и Resolve сохраняет repair, отклоняет применение старого v21 и требует нового preview/consent; нет автоматического rollback repair. Missing newest parent остаётся durable waiting. Ни repair, ни Resolve не публикуют контент, не переписывают историю и не снимают отдельный archive/Withdrawal/revoke blocker.

F15 обязателен для MVP. Demo не включает внешний каталог, source synchronization и конфликты; conflict navigation/API/Identity/роли не становятся скрытыми зависимостями Demo. См. [этапы](../../../milestones/README.md).

## 2. Страницы, экраны и навигация

| Страница / экран | Точка входа и контекст | Состав и режим | Завершение / переход |
| --- | --- | --- | --- |
| SC01. Очередь конфликтов | Target `/conflicts`, sidebar «Каталог и публикация → Конфликты»; actionable item Home MVP | CatalogConflictsPage: server list/query, default Open, явный просмотр Resolved history; нет массового Resolve | ConflictId → SC02; возврат сохраняет query списка |
| SC02. Conflict Workspace | Target `/conflicts/{conflictId}`; SC01, banner F06–F12 или typed refusal зависимой операции | CatalogConflictPage: core applied/pending comparison, отдельно current impact, captured issue evidence, metadata/history и отдельная доступность repair/Resolve | SC03 либо SC05; переход к affected workspace по immutable ID; назад к списку/исходному контексту |
| SC03. Подтверждение Resolve | SC02; прочитан exact core conflict и own ETag, есть право/доступен DF05 | ResolveCatalogConflict: текущий authoritative pending target и последствия server recheck, без выбора ProductId | Cancel без HTTP; Confirm → DF05 → перечитывание деталей/списка/affected scope |
| SC04. Предупреждение в соседнем workspace | F06–F12 либо иной consumer DF06 blocker; Product/Variant и affected operation | CatalogConflictWarning: persistent причина/scope, applied/pending summary, ссылка на SC02 | Parent guard сохраняет context/dirty; ограничения перечитываются после возврата |
| SC05. Corrective Variant patch repair | SC02, Open и конкретный patch blocker, право repair и current repair basis | CorrectCatalogConflictPatch: три представления applied/prospective/proposed, только согласованные corrective действия | Cancel без mutation; repair save → подтверждённый patch, conflict остаётся Open; затем отдельный SC03 |

SC04 — область уже описанного workspace, не новый route. Основной состав Product/Variant страниц находится в F06–F12. SC03 подтверждает отдельный Resolve, SC05 выполняет corrective DF07; сохранение patch не закрывает conflict.

Маршруты страниц утверждены [Q13](../questions/Q13-ui-scope.md). Прямое открытие SC02 и refresh читают exact conflict по conflictId без предварительного списка. Resolved conflict доступен по прежнему ID в read-only history; отсутствие в Open queue не означает 404. Default списка — Open; Resolved/history выбирается явно, без смешивания unresolved count с общей историей.

Search/filter/page/sort хранятся в query string по Q13. Поиск с debounce и пагинация выполняются сервером; UI не загружает все конфликты для локальной фильтрации. Набор фильтров, query names, сортировки, лимиты, history presentation и search fields уточняются в [Q15](../../backend/questions/Q15-api-and-source-code.md#f15-conflict-api-и-read-contracts). Числовые defaults здесь не назначаются.

Breadcrumb/summary используют доступные Name и immutable IDs; диагностический ID видим при отсутствии label. Переход к Variant разрешает **applied** parent через F06, а не строит canonical route из pending parent. После confirmed applied move старый URL корректируется по VariantId. Старый mutating запрос не перенаправляется и автоматически не повторяется на новом Product.

Контекст возврата — исходный workspace/section, объект, query списка — передаёт владелец навигации. Дополнительные query parameters и восстановление контекста после полного refresh уточняются Q13/Q15. Возврат из F15 не удаляет dirty ввод соседней F07/F08 формы; следующий Save требует актуального applied context и серверной проверки.

## 3. Карта FSD-блоков

### 3.1. Границы уровней

Pages владеют route/query и загрузкой; widget связывает comparison, impact и action; features владеют отдельными confirmation/отправкой Resolve и corrective repair. Entities отображают generated read models; shared обслуживает транспорт, диалог, ошибки и focus. `app` подключает target routes/sidebar в MVP и доступный authenticated context по F16. Features для каждого GET, глобальный conflict store, новые frameworks и обязательный slice для каждого визуального фрагмента не нужны.

### Карта блоков и состояния

| Блок | Слой / логическое размещение | Ответственность | Использование | Основное описание |
| --- | --- | --- | --- | --- |
| CatalogConflictsPage | pages / catalog-conflicts / ui, model | Route/query, Open queue/явная history, выбор ID | Создать SC01 | §3.3 |
| CatalogConflictPage | pages / catalog-conflict / ui, model | Core exact data/ETag, отдельный impact read, return context, refresh | Создать SC02 | §3.3 |
| CatalogConflictDetails | widgets / catalog-conflict / ui | Comparison, captured evidence, отдельно live dependencies/blockers, history/action area | Создать SC02 | §3.3 |
| ResolveCatalogConflict | features / resolve-catalog-conflict / ui, model | Confirm/Cancel текущего target, DF05, результат без optimistic closure | Создать SC03 после confirmation/API contract | §3.3 |
| CatalogConflictView / conflict reads | entities / catalog-conflict / ui, api | Generated list/core/impact/block representations и reads без ручной копии DTO | Создать SC01/SC02/SC04 после OpenAPI | §3.3; §8 |
| CatalogConflictWarning | widgets / catalog-conflict-warning / ui | Persistent server restriction и navigation intent | Создать SC04, переиспользовать F06–F12 | §3.3 |
| CorrectCatalogConflictPatch | features / correct-catalog-conflict-patch / ui, model | Preview и save только допустимого corrective Variant patch; no move success | Создать SC05 после DF07 OpenAPI | §3.3; §7–§8 |
| CatalogRecordView | entities / product, product-variant / ui | Read-only имена/IDs/applied context | Переиспользовать summary/navigation | [F06](F06-product-catalog.md#3-карта-fsd-блоков) |
| API metadata/dialog/error/navigation primitives | shared / api, ui | Per-call headers, generic errors/confirmation/focus/guard | Переиспользовать; headers уточняются §8 | [S05](../../../catalog/docs/standards/ui/forms-and-accessibility.md#form-state-and-user-feedback), [S06](../standards/S06-api-client.md), [S07](../../../catalog/docs/standards/ui/forms-and-accessibility.md#access-and-semantics) |

### 3.3. Публичные обязанности блоков

| Блок | Назначение / входы | Выходы и получатель | Собственное состояние | Данные, зависимости и состав | Жизненный цикл |
| --- | --- | --- | --- | --- | --- |
| CatalogConflictsPage | Найти конфликт; route/query и разрешённая область | Selected conflictId + return context → shell/SC02 | Query, generated page, selection, loading/error | DF03 public reads, CatalogConflictView, shared controls | Вход/query change → read; поздний ответ другого query не применяется; уход прекращает ожидание |
| CatalogConflictPage | Exact conflictId; единый core context и отдельно live impact | Core data+ETag, impact/freshness и repair basis → Details/Resolve/Repair; navigation intent; refresh affected reads | Core DTO+own technical ETag; impact отдельно; current patch/read If-Match и repair basis отдельно; return context | DF04 core/impact; Details; public F06 navigation; shared API | ID change очищает старые data/token/action; поздний response другого ID не применяется; stale target требует нового согласия |
| CatalogConflictDetails | Core read, captured evidence, impact с собственной freshness, allowed Resolve/repair action/callback | Open confirmation/affected object navigation → Page | Expanded sections/selected dependency, без второй business model | CatalogConflictView comparison/impact/history; CatalogRecordView; Resolve UI/shared status | Core change обновляет comparison/history и инвалидирует старое confirmation; impact не выдаётся за сохранённую core evidence |
| ResolveCatalogConflict | ConflictId, показанный newest pending target, own ETag, availability | Cancel; confirmed/refusal/unknown result → Page | Dialog open, pending, sent context+ETag, error; обязательного comment нет | DF05 generated client, shared per-call headers/confirmation; Page перечитывает | Cancel до отправки без mutation; Confirm один раз; закрытие после отправки не доказывает server cancellation |
| CorrectCatalogConflictPatch | ConflictId/newest SourceVersion, current patch+own If-Match, показанный repair basis и allowed actions | Cancel; confirmed patch/refusal/unknown → Page, отдельно от Resolve | Dirty corrective draft, preview state, captured/sent basis и patch token, pending/error | DF07 public Publishing repair/preview; shared transport; generated models; Page обновляет patch/impact | Context/source/patch change инвалидирует preview/consent; late response не заменяет другой draft; success сохраняет patch, не parent/history |
| CatalogConflictView / conflict reads | Generated core/impact/block и F06 freshness diagnostics | Read result/navigation → composition owner | No duplicate server model; page owns loading/version/readiness states | DF03/DF04/DF06/F06 public reads, NSwag/shared; без Domain/EF | Whole applied/newest accepted/assessment/old Issues distinct; technical fault не conflict reason; label fallback IDs |
| CatalogConflictWarning | Server block summary: conflict ID, scope/operation, reason, доступный comparison summary | Open conflict intent → origin workspace | Только presentation state; не владеет resolution/authorization | Public conflict view/shared banner; input от agreed reads/refusal | Context change меняет summary; исчезает только после authoritative read; не загружает весь conflict list |

CatalogRecordView описан в F06; настройка F15 — подписи «Применено CM» и «Получено из VR School, ожидает применения». Generic shared confirmation принимает title/content/confirm/cancel/busy callbacks и возвращает решение; не знает ProductVariant move. Изменение общей инфраструктуры проверяет её фактических потребителей.

## 4. Логическая компоновка

```text
SC01. CatalogConflictsPage [pages; route/query]
  List query controls [shared; Open default, явная history]
  Conflict list [CatalogConflictView; generated server page]
    Identity / applied→pending summary / PendingSourceVersion
    Blocker summary / Open либо Resolved metadata / открыть conflict
  Loading / empty / read failure / retry [shared]

SC02. CatalogConflictPage [pages; core data+ETag, impact отдельно]
  Breadcrumb / назад / перечитать [shared; return context]
  CatalogConflictDetails [widgets]
    Identity / Open-Resolved / detected-resolved metadata [entity]
    Core comparison [entity]
      Применено CM: snapshot / applied parent / AppliedSourceVersion
      Ожидает применения: newest snapshot / source parent / PendingSourceVersion
    Captured issues [entity; диагностический снимок core]
    Current impact [entity; отдельно загружено, не own core ETag]
      Затронутые dependencies / blockers / affected new operations
      Переходы к applied Product/Variant и зависимым read views
      Historical Delivery/новые sessions по existing confirmed entitlement: без отдельного запрета
    Resolution area [Open]
      Причины недоступности / отдельный CorrectCatalogConflictPatch → SC05
      ResolveCatalogConflict [feature; открывает SC03]
    Resolved history [read-only, snapshots не редактируются]
  Core/impact read statuses / stale / forbidden / not-found [shared]

SC03. ResolveCatalogConflict [feature; confirmation]
  Conflict identity / показанный newest authoritative target
  Последствия: recheck и применение source state
  Historical releases/revisions не переписываются
  Confirm / Cancel / pending / refusal / unknown outcome [shared]

SC05. CorrectCatalogConflictPatch [feature; own patch draft/token/basis]
  ConflictId / newest SourceVersion / repair basis
  Current applied состав [read-only]
  Prospective newest-parent состав [preview]
  Proposed repaired состав [preview; только corrective действия]
  Remove override/local addition / eligible replacement / inherited exclusion
  Preview / Save repair / Cancel / stale / unknown outcome [shared]
  «Patch сохранён; перенос не применён» / перейти к отдельному Resolve

SC04. Product/Variant Workspace [pages владельца F06–F12]
  CatalogConflictWarning [widgets; persistent server restriction]
    Applied context / pending summary / reason / affected actions / ссылка
  Existing content/publishing/history/policy section [в своей F-фиче]
    Affected writes disabled с причиной; backend guard обязателен
    Чтение/история доступны по правам
```

SC02 показывает coherent applied/newest core и отдельно current impact/assessment freshness; accepted horizon/fault diagnostics не позволяют старым Issues выглядеть актуальными. SC03 отдельно сохраняет context, Resolved mode убирает repair/Resolve и показывает history/resolution kind. SC05 не подменяет workspace prospective составом; relation меняется только full applied server result — manual Resolve либо System SourceChangedApply.

| Связь частей | Передаваемые сведения | Координатор | Эффект |
| --- | --- | --- | --- |
| SC01 → SC02 | ConflictId + agreed return/query context | List page / shell | Exact read; возврат в тот же список |
| Page → Details/View | Core data+freshness, captured evidence; отдельно impact/freshness | Conflict page | Comparison/history согласованы; live status не скрыт |
| Details → Resolve | Intent + core target/own ETag/availability | Conflict page | SC03 подтверждает показанный target; новый core требует нового согласия |
| Resolve → Page → reads | Confirmed/refusal/unknown + sent context | Conflict page | DF04 reread; при успехе DF03/DF06/affected F06–F12, без optimistic patch |
| Repair → Page → reads | Confirmed own patch/token либо refusal/unknown, captured source/basis | Conflict page | Обновление patch/impact; Open остаётся, source recheck и Resolve требуют отдельного согласия |
| Warning → origin → SC02 | ConflictId, immutable affected ID, source section | Origin page / guard | Переход после guard; dirty набор не стирается |
| Возврат F15 → origin | Повод refresh, stable object identity | Origin workspace | Own context/restrictions read; canonical parent только после applied move; old write не повторяется |

## 5. Зависимости и публичные границы

Page вызывает public reads entity и передаёт generated data/metadata вниз. Details публикует callbacks открытия/навигации; Resolve возвращает результат родителю и не обращается к внутреннему state F07–F12 features. Warning публикует navigation intent, parent выполняет guard. UI-вложенность не требует ссылки дочернего блока на тип page.

Entities не зависят от server Domain/EF и не становятся browser aggregates. Shared/API переносит generated data/headers и обрабатывает транспорт; решение о совместимости move остаётся на сервере. Shared не зависит от pages/features. Исправление через внутренности соседней формы не вводится.

F06 владеет applied catalog reads/canonical navigation; F07/F08 — dirty набором; F09–F12 — own read/action states и tokens. F15 передаёт restriction/повод перечитать, а не новый ProductId для молчаливой подстановки. Token assignments/policy/Publishing workspace или SourceVersion не является own conflict ETag. Independently changing impact не получает token core root.

Чтение/Resolve доступны сотруднику по F16/Q10; repair доступен специалисту по отдельному праву и аудиту [F18](F18-staff-activity-history.md); конкретные permission identifiers уточняются. Новое исключительное право админа на конфликты по аналогии с moderation не назначается. Visible/disabled action объясняет UX, backend проверяет право и affected scope.

## 6. Данные и владение состоянием

### 6.1. Состояние

| Данные / состояние | Источник истины | Клиентский владелец / область | Обновление / сброс |
| --- | --- | --- | --- |
| Open queue / Resolved history page | Backend DF03 | CatalogConflictsPage, generated page | Вход/query/refresh/confirmed resolution |
| Core applied/newest candidate/source versions/status/captured issues/history | Backend DF04 core | CatalogConflictPage generated model | Entry/refresh/confirmed result; same episode target changes do not retain stale consent; ID change clears token/context |
| Newest accepted/current assessment/fault/readiness | Backend F06 public diagnostics + F15 core/impact linkage | Page read state рядом с generated models | New acceptance marks previous Issues stale until assessment; scoped fault/read failure не guessed compatible/empty |
| Own conflict ETag | HTTP core exact resource response | Technical API/UI state рядом с core DTO | Новый core response; нет token — нет Resolve |
| Current dependencies/blockers/labels/availability | DF04 logical impact query, DF06/F06 reads | Page, отдельная generated model/read freshness | Явное обновление и после DF05; не защищено тем же core ETag |
| Подтверждённый/отправленный target | Показанный core read до Confirm | Resolve feature, sent context+ETag/pending | Cancel очищает согласие; новый target требует нового confirmation |
| Route/query/return, expanded sections/selected dependency | UI/navigation | Page/widget только в своей области | Query в URL; дополнительные persistence details — Q13 |
| Block summary в F06–F12 | DF06/read restriction/typed refusal | Origin workspace; Warning получает input | Context read/refusal/return; не optimistic removal |
| Corrective patch draft/preview; own patch If-Match | DF07 Publishing patch/preview, текущее чтение | CorrectCatalogConflictPatch, единственный dirty repair draft | Preview/source/patch change требует refresh/нового согласия; confirmed save обновляет patch, не conflict status |
| Captured repair basis | ConflictId + newest SourceVersion + показанный basis; точный тип Q15/Q20 | Repair feature рядом с sent patch token | Cancel clears consent; stale source/patch не применяется; confirmed repair сохраняется при новом source |
| Dirty local input соседнего экрана | Feature до Save | Владельцы F07–F12 | Их guard/Cancel/confirmed Save; F15 не очищает ввод |

Глобальный store, browser storage, offline и autosave не вводятся. Pure UI-state не дублирует DTO. Загруженное, подтверждённое пользователем, отправленное и применённое сервером состояния различаются; отправка не является применением.

### 6.2. Состав представления и подтверждения

| Сведение | Источник | Использование | Правило |
| --- | --- | --- | --- |
| Conflict ID/kind/status, affected IDs/diagnostic labels | Generated DF03/core/impact | List/summary/navigation | IDs первичны; current Name может измениться независимо от core Version |
| Applied snapshot/parent, AppliedSourceVersion | DF04 core | «Применено CM» либо applied absent | Whole previous state/whole absent, no guessed partial fields/parent |
| Newest candidate/accepted version; PendingSourceVersion | F06 diagnostics/DF04 core | Accepted horizon отдельно от worker assessment/apply | Один newest target same Open episode, accepted22 excludes21 before worker; no stale target selection |
| Captured issue evidence/current assessment | Core capture + F06/F15 current assessment | Historical reasons/current evaluated source version | Prior Issues explicitly stale до newest assessment, не свежий repair/Resolve basis/complete impact |
| Current dependencies/blockers/affected operations | DF04 impact/DF06 | Impact/warning/availability | Server result, freshness видима; полнота/пагинация и representation — Q15 |
| Opened/resolved metadata/resolution kind | DF04 core/history | Manual result либо System SourceChangedApply | Compatible source full-apply+Resolved atomic, no fake user action/autoresume; prior history не rewritten |
| Current authoritative target в SC03 | Прочитанный core | Confirm context | Не editable ProductId; Resolve без выбора parent в body |
| Repair comparison | Applied/prospective newest parent/proposed repaired DF07 | SC05 | Three views; repair alone не меняет applied; subsequent source correction/manual Resolve result independent |
| Repair operation/basis | Разрешённые Variant patch corrections + ConflictId/newest SourceVersion/repair basis/own patch token | Preview/save SC05 | Без new content expansion/intrinsic invalid/new-use invalid; wire/carrier не выдумывается |
| Comment/reason, warning text/confirmation controls | Уточняется Q25/Q13/Q15 | Не добавлять обязательный field/checkbox из слова «ручное» | Если принято поле — server OpenAPI, затем generated UI |

NSwag-generated models используются напрямую. Не создавать ViewModel/FormModel/EditorModel с теми же бизнесовыми полями. ETag — metadata, не DTO Version/ExpectedVersion/SourceVersion. При недостаточном comparison/impact contract изменяется backend OpenAPI и выполняется генерация, UI не реконструирует зависимости по Publishing cache.

Core ETag относится только к CatalogConflict-owned snapshots/status/captured evidence. Live labels/impact могут измениться без root Version и не включаются в representation под тем же ETag. External exposure отдельного logical `GetCatalogConflictImpactQuery`/DTO уточняется Q15; это не выбранный endpoint/generated method. Совместимость всегда повторно проверяется сервером на Resolve, даже после успешно прочитанного impact.

## 7. Действия и клиентские потоки

| Действие | Начало / запуск | Исполнитель | Обработка / серверная связь | Результат |
| --- | --- | --- | --- | --- |
| UI01. Найти конфликт | SC01 entry/query/history | List page | DF03 server query, history явна | Open queue/Resolved history, distinct empty/failure |
| UI02. Открыть exact conflict | Row/banner/deep link/refresh | Conflict page | DF04 core по conflictId, own ETag; отдельный agreed impact read | Comparison, captured evidence и current impact с разной freshness |
| UI03. Перейти к зависимости / назад | SC02 identity/impact | Page + origin navigation | Read/navigation по stable ID через F06/owner, без mutation | Applied workspace, query/dirty по guard |
| UI04. Подготовить Resolve / отменить | Open, актуальный core/token/right/availability | Resolve feature | Открыть SC03/target/последствия; Cancel локальный | Server state не меняется; focus инициатору |
| UI05. Подтвердить Resolve | SC03, согласие на показанный target | Resolve feature | DF05: conflictId + own If-Match, bodyless команда; authoritative recheck | Confirmed/refusal/unknown → Page/reread |
| UI06. Новый target/stale/unknown | DF05 refusal, 412/timeout | Conflict page + feature | DF04 core/impact reread; сравнить context, не auto retry | New explicit confirmation либо remaining blockers/history |
| UI07. Ограничение / переход F15 | F06–F12 read или operation refusal | Origin + Warning | DF06/agreed read restriction, navigation guard | Persistent reason/link; affected writes blocked |
| UI08. Исправить local dependencies | Impact с patch blocker, Open, право repair | CorrectCatalogConflictPatch | SC05/DF07 preview и отдельный save own patch с captured ConflictId/newest SourceVersion/basis/If-Match | Только patch saved; Open/old applied relation остаются, отдельный Resolve |
| UI09. Refresh repair basis | Новый source/patch либо stale отказ DF07 | Page + Repair feature | Перечитать core/current patch/impact, повторить три preview, потребовать новое согласие | Dirty ввод сохранён для пересмотра; старый basis не повторяется автоматически |
| UI10. После repair перейти к Resolve | Confirmed patch save, conflict Open | Page + Resolve feature | Новый DF04/impact; SC03 с fresh target/own conflict token; DF05 recheck | Отдельное применение relation/history либо refusal/waiting; no auto apply/rollback |

UI05 фиксирует core context, показанный перед Confirm, и отправляет token per call. Pending блокирует повторный Confirm; backend concurrency обязательна. Новый source snapshot заменяет pending target, не предлагает выбор предыдущего. Если новый core прочитан до отправки, старое согласие недействительно; изменение после чтения/во время запроса приводит к server stale rejection. UI перечитывает comparison и требует нового явного согласия. Latest-known source учитывает durably accepted newer candidate, даже если его impact/parent processing ещё не завершён: отсутствие новой версии на старом экране не разрешает применить устаревший target. Durable acceptance/newest watermark участвуют в общем writer protocol; точные storage/transport representations — [Q23](../../backend/questions/Q23-source-synchronization.md#f15-newest-target-и-guards).

Resolve не обязан быть доступен при remaining blockers. Точная связь impact/disabled action/диагностического DF05 уточняется Q15/Q25; client scan не доказывает совместимость. Backend повторно проверяет newest source и dependencies/local compatibility, применяет authoritative state, снимает blocker и сохраняет history. Изменение live dependencies при том же core ETag не обходится precondition: server recheck остаётся решающим.

После confirmed Resolve Page перечитывает core/impact/list/affected context; System SourceChangedApply, обнаруженный fresh read, показывает свой отдельный resolution kind, не подтверждение user command. Repair success обновляет patch/own token/impact и означает только «Patch сохранён; перенос этим действием не применён». Сам repair не закрывает episode; independently compatible source correction может закрыть его отдельным full-apply commit. Resolved уходит из Open queue, оставаясь history/exact ID. Captured Issues могут оставаться у Resolved как historical evidence, не current blockers. Applied context перечитывается без old-parent resend/new publication/saved history rewrite. Unknown own mutation выясняется по Q15, Resolved status сам по себе не proof её исхода; confirmed command и failed refresh отдельно показаны, old token не становится пригодным.

Timeout/обрыв после отправки — неизвестный исход. Сохраняются conflictId/sent context, читается DF04: Resolved → history, Open → актуальный target/blockers. Сам Open ещё не доказывает завершение прежнего request; критерий completion/recovery и гарантии повторного DF05 — Q15. До выяснения нет слепого resend. Close/уход/cancellation ожидания не являются server cancellation. Ответ прежнего conflictId не меняет новый context и не показывает success для него. Для DF07 lost response также означает unknown patch outcome: сохранить correction/sent basis/token, прочитать current patch/core/impact по accepted recovery contract и не делать blind repair retry. Из подтверждённого patch state не выводится Resolve success.

## 8. Использование API и NSwag

Repair и Resolve используют выбранный MVP writer protocol [Q20](../../backend/questions/Q20-persistence-and-migration.md), каждый в своём отдельном owner commit. После gate сервер получает fresh newest source/parent/dependency facts и защищает их до commit; обычные preview/core/impact reads не являются permit и не удерживают координацию до Confirm. Accepted v22 запрещает apply v21 даже до оценки v22; repair-v21, уже подтверждённый Publishing, сохраняется. Technical bounded wait/cancellation/read failure не закрывает conflict, не выдаёт compatible impact и не открывает новый предметный conflict. UI сохраняет correction/sent basis/own token; lost commit acknowledgement остаётся unknown по Q15, без blind repair/Resolve retry или подстановки нового ETag.

| Потребность / UI-поток | Серверная фича / поток | Входы | Используемый результат | Владелец вызова |
| --- | --- | --- | --- | --- |
| UI01 list/history | [Backend F15](../../backend/features/F15-catalog-conflicts.md), DF03 | Agreed query, Open default/history явно | Generated page, summary/status/metadata | List page через public reads |
| UI02/UI06 core/recovery | F15 DF04 core | Immutable conflictId | Applied/pending/SourceVersions/status/captured evidence/history, own ETag | Conflict page |
| UI02/UI06 live impact | F15 DF04 logical impact query | ConflictId + agreed context | Current dependencies/blockers/labels/availability, без core ETag | Conflict page |
| UI05 Resolve | F15 DF05 | ConflictId, обязательный own strong numeric ETag в If-Match | Confirmed result/refusal/stale, agreed recovery metadata | Resolve feature |
| UI07 restrictions | F15 DF06 / agreed F06–F12 fields | Product/Variant + operation scope | Reason/conflict ID/allowed reads | Origin page, Warning получает data |
| UI08/UI09 corrective repair | F15 DF07, отдельная Publishing операция | Generated correction request: согласованные действия; ConflictId/newest SourceVersion/repair basis, own patch If-Match | Preview трёх составов либо confirmed patch/refusal/stale; wire/recovery Q15/Q20 | CorrectCatalogConflictPatch |
| UI10 отдельный Resolve | F15 DF04/DF05 после confirmed repair | Fresh conflict read/own ETag, fresh newest source/dependencies | Применённая relation + Resolved history либо remaining blocker/waiting | Page/Resolve feature |
| Source detect/supersede/assessment/fault/SourceChangedApply | F06 receiver/worker/resolver + F15 DF01/DF02 | Trusted source facts, accepted newest horizon/current assessment | Own public diagnostics/core/history; exact models backend F06/F15 once | Browser reads, no receiver/worker/reconciliation command |

DF-ID связывают ответственность с backend, не выбирают HTTP routes. F15 API в текущем generated UI client отсутствует. Candidate endpoint routes/controller tags/operation names, DTO/signatures/envelopes/statuses, list/history/impact и error codes — [Q15](../../backend/questions/Q15-api-and-source-code.md#f15-conflict-api-и-read-contracts). Не создавать фиктивный готовый conflict-client и не вызывать DF05 через Catalog bootstrap/legacy Publishing Save.

Подтверждённая infrastructure: [nswag.json](../../../../contentmanager.ui/src/ContentManager.ApiClient/nswag.json) генерирует `{controller}Client`, namespace `ContentManager.ApiClient.Generated`, interfaces/DTO в [ContentManagerApiClients.g.cs](../../../../contentmanager.ui/src/ContentManager.ApiClient/OpenApi/Generated/ContentManagerApiClients.g.cs). [AddContentManagerApiClients](../../../../contentmanager.ui/src/ContentManager.ApiClient/DependencyInjection/ApiClientServiceCollectionExtensions.cs) регистрирует implementations `IContentManagerApiClient` scoped; подключение — [ApiClientWebAssemblyHostBuilderExtensions.cs](../../../../contentmanager.ui/src/ContentManager.ApiClient/DependencyInjection/ApiClientWebAssemblyHostBuilderExtensions.cs). Это не доказательство F15 methods/header support.

Exact mutable core GET даёт **собственный strong numeric ETag**. Он хранится рядом с generated core DTO как metadata. SourceVersion/list row/assignment/policy/workspace token его не заменяют. Target DF05 — bodyless Resolve с per-call `If-Match`: не передаёт technical Version/ExpectedVersion, ProductId или local correction payload. Corrective payload относится только к отдельному DF07 и использует own patch If-Match, не core conflict token. Переданный repair basis защищает показанный prospective context; exact representation уточняется Q15; server guard выбран в Q20 и не назначается UI. После commit target response — подтверждённый core DTO и новый own ETag; точный wire carrier/OpenAPI, no-op/history representation — Q15. Если audit-поле будет отдельно принято, изменяется server contract, а не скрыто добавляется JSON в текущий flow. Браузер token не вычисляет; core ETag не маркирует independently changing live impact.

`412 Precondition Failed` — exact resource изменился: DF04 и новое согласие, не повтор прежней команды. `428 Precondition Required` — ошибка отсутствующего token в client flow, не право Force. `409 Conflict` при совпавшем own token означает предметный отказ: remaining blockers или independently changed authoritative basis. Machine codes/priority/structured blocker/recovery payload — Q15. Причины не угадываются по exception text.

Если NSwag infrastructure не возвращает headers/не принимает per-call If-Match, изменяются общие templates/configuration/transport и выполняется regeneration из OpenAPI. Generated classes не редактируются вручную; token не передаётся shared HttpClient default headers; копия DTO не создаётся. CORS exposure/error mapping и собственные headers проверяются в реальном browser/API контуре.

## 9. Состояния, ошибки и доступ

| Ситуация | Что видит пользователь | Доступные действия | Данные / восстановление |
| --- | --- | --- | --- |
| Technical gate/read failure | Координация/проверка недоступна, не domain409/новый CatalogConflict | Сохранить ввод/context; safe read | Не empty valid result; lost write acknowledgement остаётся unknown по Q15, без blind retry/new token |
| Initial list/core/impact loading | Загрузка соответствующей области | Навигация/Cancel ожидания; Resolve без core/token недоступен | Late response другого query/ID не применяется |
| Нет Open / нет совпадений | Отдельные «Открытых конфликтов нет»/«Нет совпадений» | Сброс agreed filters/history/navigation | Failure/403 не маскируется empty |
| Open с blockers | Applied/pending и SourceVersions, captured evidence, отдельно current impact/reasons | Reads/history/navigation; Resolve по server availability | Affected writes blocked; ordinary Save не safe repair |
| Resolved | Read-only result/metadata/history; captured reasons подписаны историческими | Affected workspace/назад/history | Нет повторного закрытия; queue update после reread; historical evidence не current blocker |
| Core/impact refresh failed | Ошибка конкретной области/freshness, прежние безопасные display data | Retry read, без mutation по непроверенному context | Capture не заменяет live impact; старое confirmation недействительно |
| Pending superseded/targetchanged | Новое comparison, «Основание изменилось» | Reread/пересмотр/new confirmation | Старый target не применяется; нет очереди промежуточных |
| Newest accepted not assessed | Version horizon отдельно, previous Issues stale/current assessment pending | Refresh; no repair/Resolve confirmation по obsolete issue basis | Accepted22 excludes21 before worker; ACK не applied/readiness |
| Open+new missing parent | Same episode/current newest + waiting; old AppliedState preserved/absent | Read/safe refresh; no fake parent/TTL | No new duplicate episode; Archived parent separate |
| IngestionFault | Scoped technical F06 unreadiness, existing conflict context отдельно | Safe diagnostics/read, no new manual Resolve task | No CatalogConflict created from malformed/unsupported alone, no empty/old-candidate apply/guessed entityblock |
| SourceChangedApply | Resolved by System/latest compatible source correction | History/affected canonical read | No «ваш Resolve выполнен» from background outcome; no publish/autoresume/repair rollback |
| 412 / 428 | Stale resource / ошибка обязательного precondition | Reread при 412, корректный client flow при 428 | Нет Force/чужого token; business отказ отдельно |
| Remaining dependency conflict | Concrete mutable/in-flight blockers/objects/scope | SC05 corrective patch либо отдельный source fix в VR School | Ordinary affected Save blocked; historical immutable alone не blocker |
| Repair pending/confirmed | Preview три состава; сохранение patch; после успеха явный patch-only result | Повтор Save disabled; после success отдельный Resolve с fresh reads | Open/old applied parent не меняются от repair save |
| Stale repair source/patch | Basis changed; введённое correction сохранено для пересмотра | Refresh/новый preview/consent | Ни старый target, ни старый patch token не повторяются |
| v22 после repair v21; newest parent missing | Новый comparison/ожидание parent | Refresh/коррекция под v22/Resolve после снятия blockers | Durable waiting; confirmed repair сохранён; no old apply/auto rollback |
| Resolve pending | Выполняется действие конкретного conflict | Повторный Confirm disabled | Sent context сохранён; close не отменяет сервер |
| Timeout после отправки | Неизвестный исход | Exact/recovery read, без слепого resend | IDs/context сохранены; banner не снимается локально |
| 403 / 404 | Отказ права отдельно от отсутствующего объекта | Разрешённая навигация | Resolved не 404; root/dependency error различаются |
| Неполный impact | Явный предел полученных current сведений | Retry/read; Resolve по agreed contract | Отсутствие dependencies в UI не proof compatibility |
| Cancel/возврат к dirty форме | Исходный context/ввод сохранён | Explicit следующий шаг по актуальному context | Cancel до отправки без server change; dirty не discarded |

Role areas/navigation учитывают F16/Q10; кнопка не permission. Warning не блокирует всю SPA/unrelated scope/historical Delivery. CatalogConflict и Archive не равны Unpublish/Withdrawal/revoke; safety actions сохраняются по правам. Archived запрещает expansion/new release/revision/ordinary activation/rollback/promotion даже после repair/Resolve; restore требует own Active, Active parent и отсутствия других blockers без autoactivation/unwithdraw/grant. Automatic continuation и поздний Approval перечитывают актуальный scope/basis/current intent по принятым F11/F17; Resolve/SourceChangedApply не возобновляют Cancelled/Superseded items. Отдельные запреты показываются владельцами своих фич, без объявления их следствием Resolve.

List selection/history/navigation/confirmation доступны с клавиатуры. Applied/pending/SourceVersions/captured-current различимы текстом, не только цветом; ошибки связаны с областью действия. SC03 получает focus внутри диалога, Cancel возвращает инициатору. После исчезновения resolved строки из Open list focus переходит к доступному элементу/результату. Stale/unknown/success понятны; refresh не отнимает focus без необходимости. Точные warning/confirmation controls и assistive environments — Q25/Q13; применять S05/S07 без назначения CSS/размеров.

## 10. Проверки и приёмка

Ниже критерии будущей реализации по [S08](../standards/S08-testing.md), не результаты запуска приложения. Isolated orchestration использует doubles public API с generated responses/headers; browser/API и source/Publishing/Delivery границы проверяются отдельно реальным контуром. Test library здесь не выбирается.

| Проверяемый блок / сценарий | Условия / воздействие | Наблюдаемый результат | Граница |
| --- | --- | --- | --- |
| Protected commit / technical failure | Foreign lifecycle/catalog/source changes между preview и commit; bounded gate wait/cancellation/read failure; lost ACK | Server revalidates protected facts; own412/domain409/technical failure различимы; ввод/basis/token сохранены, no false success/empty valid/blind retry | UI/transport doubles для representation; real PostgreSQL owner writers/barriers для guard, recovery contract Q15 отдельно |
| SC01/UI01 | Open+Resolved, default/history/query/page/browser back | Default Open, saved history/query, server pagination, empty/failure distinct | Page + public API double |
| SC02/UI02 | Deep link без list; разные applied/pending/versions | Core+own ETag, comparison/captured evidence и current impact раздельно; pending не canonical | Page/widget/entity |
| Core/impact scope | Live labels/dependencies меняются без core Version | Own ETag относится только к core; current freshness видна; Resolve выполняет recheck | Client representation + real API/header guards |
| SC04/UI07 | Affected writes и unrelated scope | Persistent reason/link; affected disabled; reads/history/unrelated доступны | Consumer UI doubles + real server guards |
| UI03 navigation | Pending/apply move, возврат к same dirty Variant | Applied parent, immutable ID/query/dirty; no old-parent resend | Navigation/page integration |
| UI04/UI05 | Current Open/token, Confirm/Cancel/double click | Cancel без mutation; одна команда own If-Match, no ProductId override/technical JSON token | Feature/dialog/generated transport |
| UI06 superseding/412 | Source before/during Resolve, durably accepted newer candidate ещё обрабатывается; late старого ID | Newest supersedes; stale не закрывает; reread/new confirmation; late response не меняет context | UI + real source/concurrency API |
| Remaining/428/403/404 | Blockers/token/right/object absent | No optimistic success/Force, structured scope, distinct access/not-found | UI doubles + real precondition/authorization |
| Unknown outcome | Response lost after commit либо request ещё выполняется | Unknown/recovery read, no blind resend; history/current Open без ложного completion | Transport/component + real recovery Q15 |
| Confirmed Resolve + refresh failure | Compatible newest target applied; captured reasons сохранены; reads success/fail | Queue/history/affected reread, canonical applied parent; historical evidence не current blocker; confirmed command и refresh failure distinct | Real CatalogAccess/Publishing + browser/API |
| Historical behavior | Materialized revision/Delivery/session during/after conflict | Saved composition не переписана; no auto Withdrawal; sessions/Delivery продолжаются без отдельного prohibition | Real Delivery/session contour, не UI badge |
| SC05/UI08 corrective repair | Remove conflicting override/local addition; eligible result replacement; inherited-only exclusion; invalid/new expansion | Три preview; только permitted patch correction, ordinary Save/Force/alternate parent отсутствуют; patch success не move | Feature/preview doubles + real DF07 Publishing guards |
| Repair concurrency/UI09 | Source v21→v22 до save; own patch изменён; v22 после confirmed repair до Resolve | Stale rejection/refresh/new consent; confirmed repair retained; no v21 apply/no auto rollback | Real source/patch/fence + UI error/context |
| Missing newest parent | Parent отсутствует после repair/new source | Durable waiting, Open сохранён, no fake parent/no TTL/UI success | UI + real F06/F15 pending handling |
| Same-episode supersede/assessment | Accepted22 while old Issues assessed21; new missing-parent target | Same Open ID, newest version/waiting distinct, old Issues stale, no21 Confirm/current compatibility | Read fixture/page + receiver/worker barriers |
| Compatible source correction | Open+retained repair, new compatible source target applied by System | Full apply+Resolved SourceChangedApply, history preserved, no fake user Resolve/publish/autoresume | Real F06/F15 + UI result/history |
| Technical input fault | Trusted scoped higher malformed/unsupported vs unknown identity | Separate unreadiness, no new F15 task/current Issues fiction; historical Delivery unaffected alone | Diagnostic fixtures + scoped source guards |
| Prospective composition | A→B при absent A; independently inherited B + A→B; historical immutable only | Первый сам по себе allowed, duplicate B concrete blocker; history alone не blocker | Real composition/affected closure + impact UI |
| Repair rights/audit | Специалист с/без repair права; VR School source editor | Backend permission/F18 audit; source correction только уполномоченным VRS user; no admin-exclusive inference | Real auth/audit + UI availability |
| Archive/continuation | Archived own/parent, Resolve; stale automatic continuation/late Approval | Archive publishing guards остаются; fresh scope/basis checked; safety/existing entitled Delivery доступны | Real Publishing/moderation/Delivery + UI representation |
| Accessibility/context | Keyboard open/cancel/resolve/history; row disappears/stale | Labels/errors/focus return/fallback, context сохранён | Component/browser; scope Q13 |
| MVP/Demo | Demo shell; MVP authenticated/source | Demo без F15/catalog/auth deps; MVP allowed reads/resolution | App composition + real access |

Приёмка описания: у SC01–SC05 понятен состав; UI01–UI10 имеют владельца и server obligation/явное открытое решение; core ETag/source versions/live impact/dirty input не смешаны. Ignore, альтернативный parent и обычный blocked Save не объявлены способом разрешения. Проверка Markdown/ссылок не является тестированием приложения.

## 11. Последовательность изменений и открытые вопросы

Согласованный corrective Variant patch workflow реализуется отдельно от Resolve. Сначала материализуется выбранный Q20 writer protocol и уточняются Q15 read/Resolve/repair/block wire contract, repair basis carrier, own headers/errors/recovery; confirmation details — Q13. Backend формирует DF03/DF04/DF05/DF06 и отдельный Publishing DF07; после OpenAPI выполняется NSwag regeneration/header infrastructure. Затем list/exact pages, comparison/impact/history, corrective preview/save, отдельный Resolve confirmation и warning подключаются к F06–F12 через public inputs/callbacks. Проверяются superseding/concurrency/unknown outcome, canonical applied navigation и real affected-write/Delivery границы. Source editing/synchronization браузером не выполняются.

### Факты текущих исходников

На момент редакции прочитаны следующие source files; это инвентаризация реализации, не target contract:

- [CatalogPage.razor](../../../../contentmanager.ui/src/ContentManager.Pages/Components/Catalog/CatalogPage.razor): `@page "/catalog/{Kind}"`, `ICatalogClient`, read-only catalog alert, SourceRevision, products/variants/licenses lists. Нет conflict queue/comparison/Resolve; displayed sync revision не own conflict ETag.
- [PublicationWorkspacePage.razor](../../../../contentmanager.ui/src/ContentManager.Pages/Components/Publishing/PublicationWorkspacePage.razor): `/publishing/products/{OwnerId:guid}` и `/publishing/variants/{OwnerId:guid}`, `IPublishingClient`, прежние Save/Release/CreateRevision/Activate/Revoke с workspace `ExpectedRevision` и `Load`. Это не accepted F15 repair и не own conflict header contract.
- [Generated client](../../../../contentmanager.ui/src/ContentManager.ApiClient/OpenApi/Generated/ContentManagerApiClients.g.cs): `ICatalogClient` содержит Synchronization/Products/Variants/Licenses/Bootstrap; conflict list/detail/Resolve API отсутствует. Поиск `.cs`/`.razor`/`.json` под текущим `src` без `bin/obj` не нашёл `CatalogConflict` или `/conflicts`.
- NSwag configuration/registration перечислены в §8. Их наличие не подтверждает future DTO/method names/per-call ETag support.

Аналитика F15 не меняет source/generated/tests/configuration. Current routes не являются compatibility contract target `/conflicts` по Q13.

| Вопрос | Что остаётся уточнить |
| --- | --- |
| [Q25: catalog conflict](../../../business/questions/Q25-catalog-conflicts.md#f15-технические-границы-разрешения) | Согласованы отдельный corrective Variant patch workflow, two-step repair/Resolve и source correction уполномоченным VR School user; UI confirmation details — Q13 |
| [Q23 V2 catalog](../../backend/questions/Q23-source-synchronization.md#принятый-протокол-каталога-v2) | Accepted-before-worker/one newest target/assessment freshness/fault/missing-parent/same-episode SourceChangedApply selected; exact diagnostics/read codecs/registrations engineering |
| [Q15: API/source code](../../backend/questions/Q15-api-and-source-code.md#f15-conflict-api-и-read-contracts) | Candidate F15 HTTP routes/tags/DTO/generated names; list/filter/page/history/impact completeness/exposure; core own ETag; body/status/no-op/recovery/error priorities; per-call If-Match/CORS |
| [Q20: persistence/coordination](../../backend/questions/Q20-persistence-and-migration.md) | Общий writer protocol и F11/F17 current-intent continuation выбраны; exact repair basis carrier/adapter/physical mapping остаются engineering |
| [Q13: UI scope](../questions/Q13-ui-scope.md) | List filters/history/return/dependency links/дополнительные confirmation controls; UI routes уже приняты; новый framework/store не выбирается |
| [Q10: права](../../../business/questions/Q10-permissions-and-isolation.md) | Concrete read/Resolve/safe repair permissions/audit без выдуманной индивидуальной изоляции или исключительного admin права |

Уже приняты: MVP move, applied/newest pending comparison, superseding older pending, affected новые writes blocked, historical Delivery/sessions без отдельного prohibition продолжаются, authoritative source parent, stale rejection и Resolved history. Согласованы corrective patch действия, три preview, own patch precondition/captured basis и отдельный Resolve с fresh recheck; current Publishing Save не является этим workflow. Historical immutable alone не blocker; Archive и Conflict не прекращают existing entitled Delivery, включая новые sessions.
