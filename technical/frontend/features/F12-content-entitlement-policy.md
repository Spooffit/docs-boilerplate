# F12. Политика права на контент: фронтенд

[Бизнес](../../../business/features/F12-content-entitlement-policy.md) · [Бэкенд](../../backend/features/F12-content-entitlement-policy.md) · [M04](../modules/M04-publishing.md) · [Q13](../questions/Q13-ui-scope.md)

Документ описывает целевой MVP UI Content Manager: .NET 10, Blazor Standalone, MudBlazor, FSD и NSwag. Будущие блоки ниже — логические обозначения, не подтверждённые классы. Факты текущих исходников и расхождения с целью приведены в §11. Вёрстка не задаётся.

## 1. Результат и границы

Специалист во вкладке «Политика» Product/Variant Workspace видит собственную настройку, источник наследования, текущий effective результат и неизменяемую историю редакций. Он явно сохраняет одно из двух значений MVP либо очищает настройку, возвращая наследование. Приоритет: Variant override → Product policy → system default `FreezeAtExpiration`. Отсутствие override не создаёт скрытой локальной копии.

Current effective policy относится к следующему PolicyAgreement, а не к уже подтверждённому коммерческому периоду. Publishing фиксирует immutable Agreement на CM commit до VRS confirmation того же planned period. Поздний source event/first use использует exact Agreement, а не сегодняшнюю policy или timestamp-as-of guess. Agreement не предоставляет grant/доступ; форма сотрудника и Unity не выдают Agreement и не подтверждают коммерческие права.

В разрешённой CM diagnostics показываются отдельно AgreementId/frozen policy/source/version/applied-parent provenance и current confirmed commercial facts: License/Variant/period basis, UpdatesUntil/unlimited/current-period marker/completeness. Эти данные поступают из [backend F12](../../backend/features/F12-content-entitlement-policy.md), без дублирования его моделей. Source planned/pending отображается как ожидание, а не CM commercial-edit действие. Точный diagnostic read/carrier — Q15, отдельный экран или license picker здесь не назначается.

Сохранение меняет только собственное mutable policy state этого scope. Оно не сохраняет assignment, не создаёт release/revision/activation, не проходит обычную модерацию и не меняет уже существующие entitlement bindings. Архивирование, удаление и редактирование исторической policy version не предлагаются. `ContinueWithLatest` отсутствует в MVP. F12 относится к MVP; Demo не получает скрытых зависимостей от лицензирования и полного Publishing.

UI объясняет, что изменение own policy относится к будущим Agreements. Уже reserved/confirmed exact Agreement сохраняет применимую policy, включая delayed source confirmation/first use. Выбранный [Q20 Agreement protocol](../../backend/questions/Q20-persistence-and-migration.md#f12-policyagreement-и-подтверждение-периода) не реконструирует promise из сегодняшней policy; unknown остаётся только exact representation, а не checkpoint.

Runtime entitlement decision выполняет сервер [F13](../../backend/features/F13-content-delivery.md); CM UI настраивает policy и объясняет её смысл. Выдача/продление/revoke коммерческих прав не являются действиями этой вкладки. Диагностика конкретного решения возможна только через подтверждённый контракт; endpoint и отдельный diagnostic screen здесь не изобретаются.

Catalog Archive/Conflict не являются revoke и не перепривязывают existing entitlement periods; уже materialized Delivery по ранее подтверждённому entitlement, включая новые sessions, продолжает собственные F12/F13 проверки. Exact policy/history read сохраняет объект с catalog reason/actions. Open F15 блокирует только affected ambiguous policy writes; отдельный corrective Variant patch repair не редактирует policy и не снимает её blockers. Restore не выдаёт grants и не меняет historical bindings. Конкретная eligibility нового policy изменения задаётся backend scope, UI не выводит blanket запрет из одного Archived badge.

## 2. Страницы, экраны и навигация

| Страница / экран | Точка входа и контекст | Состав и режим | Завершение / переход |
| --- | --- | --- | --- |
| SC01. Product Workspace, «Политика» | `/products/{productId}/policy`; каталог F06, вкладка либо deep link | Read-only Product context; собственная policy или отсутствие; system default; effective result/source; редактор и компактная история; blocker F15 | Save остаётся на вкладке; переходы согласованы с общим workspace |
| SC02. Variant Workspace, «Политика» | `/products/{productId}/variants/{variantId}/policy`; выбранный Variant F06 | Read-only Product/Variant context; inherited Product/system policy; собственный optional override; effective result/source; редактор и история | Save/clear остаются на вкладке; доступен переход к policy родительского Product |
| SC03. Подтверждение очистки | Действие SC01 «Использовать системную политику» либо SC02 «Сбросить переопределение» | Scope, очищаемая собственная настройка, текущее основание наследования и ожидаемый effective результат | Confirm устанавливает локальное `Policy = null`; явный Save отправляет изменение; Cancel сохраняет ввод |

Маршруты приняты для целевого workspace в Q13; текущие `@page` отличаются (§11). Нет глобального policy CRUD. Основной каталожный контекст и другие вкладки описаны [F06](F06-product-catalog.md). Прямой вход и refresh читают exact scope, его own policy state и необходимые inherited сведения. Неизвестный/недоступный объект не подменяется первым доступным; Variant должен принадлежать Product из route. Отсутствующая настройка не равна ошибке чтения.

При смене объекта, вкладки или уходе применяется guard Save / Discard / Stay. Save выполняет только policy mutation; Discard сбрасывает только локальный policy ввод; Stay сохраняет контекст. Browser refresh/закрытие используют доступное предупреждение, восстановление после reload не обещается. Нет autosave/offline/browser storage. Ответ от предыдущего scope не заменяет данные нового.

## 3. Карта FSD-блоков

### 3.1. Необходимые уровни

Page передаёт exact Product/Variant context и координирует навигацию. Один переиспользуемый widget объединяет own baseline, editor, inherited/effective summary и history для обоих scopes. Save выделен как пользовательское действие; выбор значения и подготовка очистки остаются локальными действиями редактора. Entity предоставляет generated policy данные и операции чтения. Shared обслуживает транспорт, controls, сообщения и guard. Отдельный глобальный store не требуется.

### 3.2. Состав

| Блок | Слой / логическое размещение | Ответственность | Использование | Основное описание |
| --- | --- | --- | --- | --- |
| Product/VariantWorkspace: PolicySection | pages / workspace / ui, model | Exact context, вкладка, переходы и dirty guard | Изменить SC01/SC02 | §3.3; каталог F06 |
| PolicyWorkspace | widgets / policy-workspace / ui, model | Координировать редактор, server baseline, источник/effective и историю | Создать общий блок SC01/SC02 | §3.3 |
| PolicyEditor | Локальная часть PolicyWorkspace / ui | Выбор двух values, подготовка clear, кнопки Save/Cancel и persistent non-retro info | Создать внутри widget, без отдельного среза для каждого поля | §3.3 |
| SaveContentPolicy | features / save-content-policy / api, model | Отправить own policy с exact If-Match и вернуть подтверждённый результат | Создать; оба scopes | §3.3 |
| ContentPolicy read/summary/history | entities / content-policy / api, ui | Generated read model, представления own/source/effective и immutable history | Создать по контракту backend, без ручной копии DTO | §3.3 |
| Catalog context/blocker | Публичные блоки F06/F15 | Read-only идентичность и подтверждённые ограничения операции | Переиспользовать | [F06](F06-product-catalog.md), [F15](F15-catalog-conflicts.md) |
| Controls/transport/guard | shared / ui, api, lib | Общие controls, metadata headers, ошибки и navigation guard | Переиспользовать/изменить инфраструктуру | [S05](../../../catalog/docs/standards/ui/forms-and-accessibility.md#form-state-and-user-feedback), [S06](../standards/S06-api-client.md) |

`app` подключает принятые routes/вкладку и существующие API services; отдельная policy business orchestration там не появляется. Физические namespaces и окончательный состав slices уточняются по реализации; существующий inline tab не доказывает наличие перечисленных будущих блоков.

### 3.3. Ответственность и границы блоков

| Блок | Входы и зависимости | Выходы | Собственное состояние и жизнь |
| --- | --- | --- | --- |
| PolicySection | ProductId, optional VariantId из route; F06 context; F15 ограничения; общий workspace navigation | Exact scope в widget; guard при переходе; обновление summary workspace после подтверждённой policy mutation | Только текущий scope/navigation; при смене scope не переносит draft/ETag прежнего объекта |
| PolicyWorkspace | Scope из page; ContentPolicy read API; SaveContentPolicy; read-only source/default и история | Dirty/saved/error в page; новое read presentation в summary/history | Единственный владелец saved generated model, локального generated request, ETag рядом с model, pending/errors и clear confirmation; состояние ограничено scope |
| PolicyEditor | Generated editable request, saved own setting, inherited/source/effective read presentation, allowed actions, pending/errors | Изменение локального generated request; clear-confirmation, Save/Cancel intents владельцу | Временное открытие SC03; не хранит вторую бизнесовую модель; close/Cancel не изменяют baseline; Save callback не вызывает assignment/publication |
| SaveContentPolicy | Scope, generated request с own `Policy?`, exact own-state ETag; NSwag transport | Подтверждённый policy response + новый ETag либо distinct failure/unknown outcome | Только pending операции; не владеет read baseline/history; завершение принимает widget; повторное нажатие не создаёт вторую mutation |
| ContentPolicy read/summary/history | Exact scope и backend generated contract; разрешённый diagnostic context при его наличии | Own/current policy/history; отдельно frozen Agreement provenance и current period/completeness read-only в widget | Generated server data принадлежат widget; presentation не выдаёт Agreement/grant, не пересчитывает Freeze и не редактирует историю/даты |

Назначение PolicyWorkspace — общий результат настройки и объяснения, а не вычисление runtime прав. Generated DTO/request используются напрямую; совместимые технические state и ETag находятся рядом с ними. Shared primitives получают данные и callbacks, не выбирают source, scope или policy.

## 4. Логическая компоновка

```text
SC01. ProductWorkspace: PolicySection [pages, ProductId]
  Catalog context и F15 blocker [переиспользуются]
  PolicyWorkspace [widgets, own Product state]
    ContentPolicy summary [entities: own / system default / effective / source]
    PolicyEditor [локальная часть widget]
      Выбор policy / подготовка clear / Save / Cancel [shared controls]
      Постоянное пояснение отсутствия обратной силы
      SC03. Подтверждение очистки [условно]
    ContentPolicy history [entities, read-only]
    Разрешённая diagnostics: frozen Agreement provenance / current period facts / completeness
    Состояния загрузки/ошибки [shared]

SC02. VariantWorkspace: PolicySection [pages, ProductId + VariantId]
  Catalog context и F15 blocker [переиспользуются]
  PolicyWorkspace [тот же widgets, own Variant state]
    ContentPolicy summary [own override / inherited Product-system / effective / source]
    PolicyEditor [тот же, clear возвращает наследование]
      SC03. Подтверждение очистки [условно]
    ContentPolicy history [own scope, read-only]
    Разрешённая diagnostics: AgreementId/frozen policy отдельно от current cutoff/unlimited
    Состояния загрузки/ошибки [shared]
```

| Связь | Что передаётся | Координатор | Наблюдаемый результат |
| --- | --- | --- | --- |
| Page → widget | Exact scope и доступные действия | PolicySection | Читается правильный объект; clear/Save относятся к нему |
| Read API → summary/editor/history | Saved own setting, source/effective и history | PolicyWorkspace | Editor не подставляет inherited значение как собственное; части различают saved/draft |
| Editor → Save feature → widget | Own generated request + ETag; подтверждённый результат | PolicyWorkspace | Обновляются own baseline/ETag и policy представления; assignment/publication остаются независимыми |
| Parent policy refresh → Variant summary | Новое inherited/effective read presentation | PolicyWorkspace | Меняется объяснение наследования, не локальный override/draft и не own-state ETag |
| F15 → page/widget | Причина запрета и ссылка на конфликт | PolicySection | Persistent banner, disabled mutation; доступное чтение сохраняется |

Summary, non-retro info и history видны в той же вкладке. SC03 открывается отдельно и возвращает intent в editor. UI-вложенность не разрешает features напрямую вызывать другие features; их результаты координирует widget.

## 5. Зависимости и публичные границы

Page использует публичный PolicyWorkspace с exact scope и callbacks dirty/saved. Widget использует публичные ContentPolicy read/view возможности и SaveContentPolicy action; action принимает request/ETag, возвращает подтверждённые данные или различимый отказ. Shared не зависит от Publishing slices. Клиентские entities не являются Domain/EF-моделью; зависимости UI на серверные Domain/EF не вводятся.

Read-only catalog identity берётся из F06; authoritative blockers — F15. Assignment [F07](F07-product-content-assignment.md)/[F08](F08-variant-content-assignment.md), release/revision [F09](F09-content-releases.md)/[F10](F10-publication-revisions.md), lifecycle [F11](F11-publication-lifecycle.md) имеют свои state и concurrency scopes. Policy Save не пользуется их ETag, не сбрасывает их dirty ввод и не требует publication ради применения настройки новым периодам. Общий [F18](F18-staff-activity-history.md) не заменяет предметную policy history.

Runtime правила, period binding, historical lookup и decision reason принадлежат backend F12/F13. Чтение effective policy для настройки не доказывает разрешение выдачи конкретному клиенту. Наличие кнопки/disabled state не заменяет серверную авторизацию.

## 6. Данные и владение состоянием

### 6.1. Единственный владелец состояния

| Данные / состояние | Источник истины | Клиентский владелец | Обновление / сброс |
| --- | --- | --- | --- |
| Saved own `Policy?` | Exact backend policy state данного Product/Variant | PolicyWorkspace, generated model | GET/подтверждённый PUT; смена scope очищает |
| Strong ETag own state | HTTP заголовок exact resource, отдельный серверный `int Version` | Технический state рядом с generated model | GET/PUT success; не вычисляется UI и не переносится из workspace Revision |
| Editable `Policy?` | Локальный generated request до Save | PolicyWorkspace; editor получает model/callback | Выбор, clear; подтверждённый Save/Cancel; guard при уходе |
| Agreement/period diagnostics | Backend F12 authorized generated result; Publishing Agreement и CatalogAccess confirmed-period facts имеют разные owners | PolicyWorkspace хранит read result/context/error отдельно от own editor/ETag | Refresh только diagnostics; planned pending/unknown не превращаются в confirmed grant, current policy не replaces frozen Agreement |
| Inherited/default/current effective/source | Backend policy read presentation | Read-only часть widget | Независимое перечитывание по серверному контракту; не затирает own draft/ETag |
| Исторические редакции | Immutable backend history | Widget read-only | Read/history refresh после подтверждённой mutation; failure не превращается в пустую историю |
| Dirty/pending/errors/SC03 | Presentation state | Widget; page получает dirty | Ввод/операция/завершение; reset по scope |
| Policy применимого entitlement period | Backend binding/history, Q20 | Не редактируется этой формой | Current policy change никогда не переписывает existing binding |

Own-state `int Version` — технический серверный guard, policy history identity/order — предметная неизменяемая редакция. UI не подменяет один другим, не отправляет `Version`, `ExpectedRevision` или history sequence в body. Изменение inherited/effective результата само по себе не увеличивает local own-state Version. Если GET возвращает own/inherited/effective/history в одном composite body, область валидности его own-state ETag и независимого обновления read presentation требует явного контракта [Q15](../../backend/questions/Q15-api-and-source-code.md#f12-policy-contracts-и-diagnostics). UI не считает такой ETag автоматически валидатором всего composite representation.

### 6.2. Представление и форма

| Сведение | Источник | Использование | Правило |
| --- | --- | --- | --- |
| Scope и названия Product/Variant | Route + F06 read context | Заголовок, clear-confirmation, связь с родителем | Exact ID; source поля read-only |
| Собственная policy | Backend own state / generated request | Редактируемое значение либо явно «Не задана» | Только `FreezeAtExpiration`, `DenyAfterExpiration`, null |
| System default | Backend read contract | Product summary и fallback Variant | `FreezeAtExpiration`; не записывается в own настройку при чтении |
| Inherited policy и источник | Backend read contract | Variant показывает Product либо system | Отсутствие override — «Наследуется от Product» с указанием system fallback при отсутствии Product setting |
| Current effective policy/source/version | Backend read contract | Объяснение policy следующего Agreement | Не изображает frozen policy существующего права/раньше созданного Agreement |
| Agreement provenance / confirmed period facts | Разрешённая diagnostics backend F12 | Read-only AgreementId/frozen policy/source/version/parent и отдельно current period/cutoff/unlimited/completeness | Binding unique License+Variant+SourcePeriod и own GrantPeriod — server model; System Freeze без fake policy version; UI не изменяет identity/dates |
| Policy history | Backend history | Scope, редакция/порядок, значение либо очистка, время, actor/source по контракту | Неизменяемая; точные поля/paging/order — Q15/Q20 |

Форма не содержит коммерческих дат, license/key/token, выбранного RevisionId или клиентского computed entitlement. Не задаёт autosave, новую DTO/ViewModel/FormModel или глобальное хранилище. Точная editability generated classes и envelope/header transport согласуются в Q15, затем выполняется регенерация NSwag.

### 6.3. Объяснение бизнесовых последствий

Вкладка объясняет два режима без обещания новых коммерческих прав. `DenyAfterExpiration` запрещает новую серверную выдачу после окончания updates period. `FreezeAtExpiration` использует полную confirmed Delivery availability history строго при stored `VisibilityConfirmedAt < UpdatesUntil`; equality исключена. VisibilityConfirmedAt — durable once-only primary observation после committed exact полного transition, а не его physical commit timestamp/precommit/Publishing activation time. Если Delivery commit был до cutoff, а confirmation после, этот transition в Freeze не входит. Necessary pending confirmation/history даёт Indeterminate, не «нет grant» и не выбор старой ненулевой revision. Complete history с последним eligible null означает no frozen revision, без previous-nonnull fallback. [Q17 visibility](../../backend/questions/Q17-publication-availability.md#подтверждённая-видимость-и-история-delivery).

Current committed snapshot может обслуживать текущий разрешённый период до readiness history confirmation; diagnostics не называют его Freeze-history-ready. Rollback старой revision создаёт новый transition/time. Replay возвращает stored confirmation/outcome; отсутствие stored result восстанавливается fresh time без backdate. Correction cutoff пересчитывает выбор по прежней confirmation history/agreed policy. Whole allowed candidate order effective transition, не artifact age; same-candidate tie min LicenseId then credentialId, stable provenance retained. Exact wire metadata — Q15.

Поздний обычный Unpublish и Retire source artifacts не отменяют frozen historical access. Терминальный Withdrawal запрещает exact RevisionId, включая Freeze, без автоматической замены. Повторное скачивание frozen revision не получает дополнительного deadline при сохранении допустимых license/grant/credential и отсутствии Withdrawal. Новый отказ не означает автоматическое удаление уже установленного контента ([Q04](../../../business/questions/Q04-withdrawn-publications.md)).

Бессрочность подтверждается явно через нормализованный source факт `IsUnlimitedLicense`; отсутствие UpdatesUntil не означает unlimited. Неизвестная граница при неподтверждённой бессрочности блокирует новую выдачу как insufficient data; UI не описывает её как подтверждённый revoke ([Q01](../../../business/questions/Q01-license-rights.md)).

IsUnlimitedLicense и IsUnlimitedUsers различны; UpdatesUntil не подменяется application license expiry. Dates/unlimited — отдельные current versioned commercial facts, не editable поля immutable Agreement/binding. Разрешённая source correction с explicit reason/history сохраняет policy и пересчитывает Freeze по corrected cutoff; purchased-duration extension/unlimited upgrade является renewal с новым period/Agreement. Ordinary period completion не revoke Freeze.

Продление создаёт новый planned period/Agreement и затем source-confirmed current period. Новая admission использует authoritative SourceCurrentPeriod: после renewal под Deny нет старого same-license Freeze fallback. Полный verified license set требует ready всех needed bases: Unknown Indeterminate, determinate Denied+Allowed выбирает одну most-current whole Allowed без item merge; same-candidate tie min LicenseId, затем credentialId server-side. Один key не расширяет права до организации. Session pins только selected basis; unselected revoke не cancels, selected revoke не triggers fallback. Эти пояснения не добавляют операций policy формы.

Approved [Q16 session protocol](../../backend/questions/Q16-activation-and-sessions.md#принятый-протокол-сессий-и-response-leases) сохраняет R1 pin during active updates несмотря на R2/ordinary Unpublish/Archive/Retire при действующих rights/restrictions. At expired Freeze/corrected cutoff known eligible frozen candidate должен совпасть с pin; mismatch → NeedsNewAdmission, required pending history → temporary DataNotReady. Renewal new CurrentPeriod требует нового admission для old-period session. Correction не меняет agreed policy/pin, но re-evaluates facts; known ineligible invalidates, unknown не revoke. Cutoff expansion/restoration не extends TTL/не resurrect-ит session.

Absolute server TTL default30 min ограничен finite Deny cutoff и explicit credential deadline, не application expiry; replay default5 min и proof/lease default30 s/5 s configurable. Runtime bearer/replay/lease — server/app-provider boundary, не policy editor inputs. Known selected key/license/grant/revision restrictions блокируют checkpoint; leased transfer заканчивается по independent deadline/known invalidation, без гарантии отзыва уже buffered bytes. Source propagation отдельна, 5 s не physical-network SLA. Policy diagnostics показывают permitted nonsecret session/selected basis/status/expiry, никогда bearer/hash/capsule.

VRS хранит durable planned identity/RequestId, вызывает CM через authenticated backend boundary вне VRS transaction и подтверждает тот же plan с exact AgreementId/outbox. Lost quote response повторяется с тем же request/input; нет staff/Unity call для «исправления». Agreement в MVP не истекает по возрасту; новый plan до confirmation и renewal получают новые period/Agreement. После confirmation Agreement нельзя заменить/перенести. CM может быть prerequisite нового CM content grant, но application license/Project Storage path независим; pending content grant не называется уже доступным. [Q23 workflow](../../backend/questions/Q23-source-synchronization.md#f12-planned-agreement-confirmed-workflow).

## 7. Действия и клиентские потоки

| Действие | Начало / исполнитель | Обработка | Результат |
| --- | --- | --- | --- |
| UI01. Открыть/обновить вкладку | SC01/SC02; page → widget | Exact read own state/ETag, source/effective, history; contract §8 | Saved baseline и read views правильного scope; null own отображается как наследование |
| UI02. Изменить значение | PolicyEditor | Изменить generated request, отметить dirty; HTTP отсутствует | Saved/effective summary не выдаёт ввод за применённую настройку |
| UI03. Подготовить очистку | SC01/SC02 → SC03 | Показать scope, текущее наследуемое основание/результат; Confirm задаёт request Policy=null | Dirty draft; hidden copy не создаётся; Cancel сохраняет предшествующий ввод |
| UI04. Сохранить | PolicyEditor → SaveContentPolicy | Проверить доступ/known own ETag; PUT only own Policy с If-Match; дождаться серверного результата | Обновить own baseline/ETag из success, затем source/effective/history по контракту; сообщение только о policy Save |
| UI05. Отменить ввод | PolicyEditor | Локально восстановить saved own setting; при необходимости выяснить неизвестный сетевой исход | Draft/dirty сброшены только после известного baseline; нет обратной server mutation |
| UI06. Перечитать после конфликта | 412; widget | Сохранить локальный ввод отдельно от нового baseline; read exact policy + views; показать расхождение | Пользователь заново решает, что сохранить; blind resubmit/auto-merge отсутствуют |
| UI07. Перейти к Product policy/конфликту | SC02 source link либо F15 banner | Guard Save/Discard/Stay; затем navigation | Контекст понятен; возврат не выдаёт локальное снятие blocker за серверное |
| UI08. Читать периодное основание | Разрешённый diagnostic context, без новой issuance формы | ContentPolicy read/summary | Прочитать generated frozen Agreement + current commercial facts/completeness по accepted Q15 read | Read-only proven/pending/not-ready/denied reasons отдельно; own policy draft/token сохранены |

После UI04 успешный PUT является подтверждением own mutation; frontend не делает скрытый GET exact own state вместо использования согласованного success result. History/read presentation при необходимости обновляются отдельным чтением по Q15; это не повтор Save и не изменение own ETag. Если inherited policy поменялась между SC03 и Save, показывается актуальный подтверждённый effective результат; preview очистки не является гарантией неизменности родителя.

Pending policy mutation блокирует повторный Save/clear этого scope. Смена scope/уход не разрешают позднему ответу менять новую страницу; отмена клиентского ожидания не доказывает отмену серверной записи. При неизвестном сетевом исходе сначала выясняется current state/history, затем пользователь решает о повторе в пределах гарантий Q15. Несохранённые соседние assignment/publication состояния не отправляются вместе с policy.

Save/clear policy используют selected MVP guard: fresh owner/scope/catalog/F15 facts защищены до Publishing commit; own412 отдельно от foreign409/technical failure. Guard не заменяет approved VisibilityConfirmedAt/history и session/response-lease protocols. Exact runtime wire/crypto/store/clock adapters — Q15/Q20/Q23 engineering, не новые policy controls.

## 8. Использование API и NSwag

| Потребность | Серверная операция | Вход | Результат / владелец |
| --- | --- | --- | --- |
| UI01, own Product policy | Целевой `GET /api/v1/products/{productId}/content-policy`, backend F12 | Exact ProductId | Generated own read model + strong ETag; ContentPolicy read API → widget |
| UI01, own Variant policy | Целевой `GET /api/v1/products/{productId}/variants/{variantId}/content-policy`, backend F12 | Exact parent + Variant IDs | Own nullable setting/ETag и согласованное inherited/effective presentation |
| UI04 Product Save/clear | Целевой `PUT /api/v1/products/{productId}/content-policy` | Generated body с `Policy?`; own If-Match | Saved own state + новый ETag; SaveContentPolicy → widget |
| UI04 Variant Save/clear | Целевой `PUT /api/v1/products/{productId}/variants/{variantId}/content-policy` | Generated body с own `Policy?`; own If-Match | Saved override/null + новый own ETag; SaveContentPolicy → widget |
| UI01/UI04 history | Чтение immutable policy history backend F12 | Exact scope; параметры — Q15 | Compact history в той же вкладке; точный endpoint/DTO/paging — Уточняется |
| Возможное объяснение entitlement | Backend F13, diagnostics contract — Уточняется | Только допустимое доверенное основание, без secrets в UI/URL | Policy source/version, unlimited/cutoff basis, selected RevisionId, license/grant basis, machine reason; отдельный endpoint/экран не утверждён |

REST пути — целевой контракт, не доказательство существующих методов. Финальные сигнатуры, envelope/read DTO/own-vs-composite scope, history, failure codes и diagnostics — [Q15](../../backend/questions/Q15-api-and-source-code.md#f12-policy-contracts-и-diagnostics). GET не создаёт policy state/историю. Выбор initialization/missing semantics принадлежит backend Q20; UI не выполняет скрытый setup.

Используется существующая база `ContentManager.ApiClient.Generated`, NSwag config и DI (§11). Generated models применяются напрямую; новые имена client methods/types не угадываются и generated файл вручную не правится. У текущего `IPublishingClient` другая route/body семантика; подключение target требует backend OpenAPI и регенерации.

ETag сохраняется как exact строка из HTTP рядом с generated model. PUT передаёт её без изменения в `If-Match`; UI не вычисляет numeric Version и не помещает token в DTO. Success предоставляет saved data и новый strong ETag; если заголовок недоступен/пропущен, дальнейшая запись блокируется до выяснения exact состояния. Общая генерация/transport должна предоставлять headers и per-request If-Match; hooks/local headers словарь generated method сами по себе не являются caller contract. Shared default HttpClient headers не используются как общий изменяемый token разных scopes. Применяется [S06](../standards/S06-api-client.md).

## 9. Состояния, ошибки и доступ

| Ситуация | Представление / действия | Данные и восстановление |
| --- | --- | --- |
| Technical gate/read failure | Техническая недоступность, не domain409/новый CatalogConflict | Context/token сохранены; safe read; unknown mutation выясняется по Q15 без blind retry/new token |
| Первичная загрузка | Ожидание policy области; Save/clear недоступны без baseline/ETag | Ошибка read не заменяется system default или пустой history |
| Own setting отсутствует | Явное «Используется системная политика» для Product / наследование для Variant | Подтверждённый null остаётся null; UI не сохраняет effective значение как собственное |
| Dirty / pending / success | Ввод отделён от saved, при отправке виден pending, success относится только к подтверждённому Save | Нет optimistic объявления применения к existing periods; после success новый baseline/ETag |
| Изменилось inherited/effective | Source/effective summary обновляется, поясняет текущее основание | Own draft и token не меняются из-за parent change; composite semantics — Q15 |
| 412 Precondition Failed | «Политика изменена другим пользователем», перечитать/сравнить | Локальный ввод сохраняется по возможности; нет blind retry и auto-merge |
| 428 Precondition Required | Ошибка клиентского потока обязательного If-Match | Не предлагать обычный повтор без восстановления exact ETag |
| 409 Conflict / F15 blocker | Предметная причина и persistent ссылка на конфликт; disabled mutation | Не называть любой409 конкурентной записью; чтение остаётся доступным в разрешённой области |
| Validation / unsupported value | Ошибка поля/формы по серверному ответу; доступны исправление/Cancel | Неизвестное enum значение нельзя молча заменить default и отправить |
| Отказ доступа / объект отсутствует / неверный parent | Понятный отказ в соответствующем scope | Не заменять объект и не маскировать отказ отсутствием policy |
| Сетевой сбой чтения | Ошибка и retry этой области | Прежний read может оставаться явно неактуальным; история не становится «нет изменений» |
| Неизвестный исход PUT | Сообщение о неопределённом исходе, выяснение сохранённого состояния | Нет безусловного повторения; draft не выдаётся за confirmed state |
| Success без пригодного ETag | Видно подтверждённое действие и невозможность следующей записи до восстановления | Contract defect не исправляется вычислением Version или использованием assignment token |
| История недоступна | Локальная ошибка history, отдельное повторное чтение | Успешная own mutation не объявляется отклонённой; полнота history не обещается |
| Agreement frozen / current policy changed | Старое Agreement основание отдельно от текущей policy для будущего quote | Не обещать retroactive replacement при Save или delayed first use; immutable provenance read-only |
| Planned content period / Agreement not ready | Видимая waiting/completeness причина, content grant не confirmed | Safe diagnostic reread; source recovery принадлежит VRS backend; нет CM issuance/edit/requote кнопки |
| Missing/contradictory required period evidence | Indeterminate/DataNotReady отдельно от complete no-grant/revoke Denied | Не InvalidKey для valid credential, не system-default adoption; confirmed Agreement/binding не overwritten |
| Current renewal/cutoff correction | Source period/corrected facts отдельно от frozen policy; known session mismatch виден | New admission for changed period/ineligible frozen pin; Unknown временно blocks, не revoke; no TTL extension/restore/date-edit controls |
| Runtime selected basis / lease refusal | Разрешённые nonsecret session status/basis/expiry и known-denied vs readiness reason | No unselected-key cancellation/auto-license fallback; expiry/new admission не repin old plan; staff editor не выдаёт credential |
| Necessary visibility/history Pending | Indeterminate readiness/completeness, отдельно от complete no-grant/null | Не выбрать earlier nonnull/другую лицензию из неполного verified set; safe read без новой quote/policy mutation |
| Complete confirmed null / late Unpublish | No frozen revision либо сохранённое до cutoff historical право | Confirmed null не error; no previous-nonnull fallback; late ordinary Unpublish не отменяет earlier frozen state |
| Exact ban / required authority unknown | Known terminal refusal отдельно от unknown restriction facts | Inactive/unmaterialized ban доминирует Freeze и replay; unknown не разрешение; agreed policy/history не переписаны |

Доступ к просмотру/редактированию определяется серверной авторизацией и подтверждёнными permissions; точные имена и sign-in flow здесь не назначаются. Кнопки имеют понятные подписи; основное изменение/Save/Cancel доступно с клавиатуры, ошибки не передаются только цветом. SC03 получает фокус при открытии и возвращает его к запускающему действию при закрытии. Dirty guard не скрывает введённое значение за unrelated reload. Применяются [S05](../../../catalog/docs/standards/ui/forms-and-accessibility.md#form-state-and-user-feedback) и [S07](../../../catalog/docs/standards/ui/forms-and-accessibility.md#access-and-semantics).

## 10. Проверки и приёмка

| Блок / сценарий | Условия и воздействие | Наблюдаемый результат | Граница |
| --- | --- | --- | --- |
| Protected commit / technical failure | Foreign lifecycle/catalog/source changes между preview и commit; bounded gate wait/cancellation/read failure; lost ACK | Server revalidates protected facts; own412/domain409/technical failure различимы; ввод/basis/token сохранены, no false success/empty valid/blind retry | UI/transport doubles для representation; real PostgreSQL owner writers/barriers для guard, recovery contract Q15 отдельно |
| Product summary/editor | Own null, system Freeze; выбрать Deny, Cancel; clear explicit value | Собственное/inherited различимы; Cancel без PUT; clear отправляет null, не Freeze copy | Component с подменённым generated API |
| Variant precedence | Parent explicit/null, override explicit/null; change parent при открытой вкладке | Variant → Product → system; own draft/token не заменяются inherited value | Widget orchestration; реальная precedence отдельно backend |
| UI03 очистка | Confirm/Cancel в обоих scopes; parent change во время подтверждения | Context/result понятны; Confirm только готовит draft; Save явный; актуальный success показывается после ответа | Component/composition |
| UI04 transport | Exact GET ETag → PUT; null и оба MVP values | If-Match равен сохранённому заголовку; body only own Policy, без ExpectedRevision/Version/sequence; новый ETag сохранён | Transport с HTTP test double; real HTTP отдельно |
| Concurrency independence | Assignment/publication изменены; parent policy изменена; другой пользователь изменил own policy | Независимые guards; только own stale mutation даёт соответствующий412; composite Q15 не выбирается тестом молча | Реальный API и UI transport |
| Ошибки | 412/428/409/validation/403/missing/network/нет ETag | Разные сообщения/next steps; нет ложного успеха, blind retry, скрытого default или auto-merge | Widget/transport |
| History/source | Несколько редакций, clear, history read failure | Read-only compact history, различимый clear/source и отдельная ошибка; F18 не подставлен вместо неё | Component и реальный history contract |
| Non-retro Agreement | Quote Freeze committed → current policy Deny → VRS confirm/event/first use later | Frozen Agreement policy/provenance retained; current policy отдельно; no first-use/as-of rebinding | UI diagnostics + real Agreement/source/Delivery integration |
| Planned boundary/recovery | CM outage/lost quote ACK; source confirmation failure; application license exists | Pending content visible/no fake grant; source retries same request identity; staff/Unity issuance absent, app flow independent | CM read presentation + real VRS/CM workflow отдельно |
| Scope/immutable identity | License-wide PeriodId для разных Variants; changed AgreementId после confirm; system default | Exact triple/own GrantPeriod/exact Agreement; contradiction not overwritten; System Freeze no invented published version | Generated diagnostic representation + backend constraints |
| Renewal/correction | Renewal Deny при старом Freeze; reasoned cutoff correction; duration/unlimited upgrade; ordinary completion | Current period only/no old fallback; correction keeps policy/recalculates Freeze; upgrade new Agreement; closure не revoke | UI explanations + actual source/entitlement evaluation |
| Complete verified set | One valid key; full verified set Allowed+Denied; Allowed+neededUnknown; valid key incomplete facts | Single-license explicit; known whole Allowed selected only for determinate set; Unknown Indeterminate не InvalidKey/revoke | Typed UI/SDK outcomes + real resolver |
| Confirmation cutoff | Commit до cutoff/confirmation после; equality; necessary pending confirmation | StoredConfirmedAt strict before cutoff; late/equal excluded; pending Indeterminate не no-grant/old-nonnull fallback | Diagnostics + real primary confirmation/history |
| Null / rollback / replay | Complete null before cutoff; late Unpublish; old artifact rollback; lost confirmation ACK | Null no frozen fallback; late Unpublish сохраняет historical Freeze; rollback new time; replay stored time/fresh recovery only absent | Real ordered availability history + UI basis |
| Commercial correction with history | Corrected UpdatesUntil; frozen Agreement unchanged; history readiness/ban outage | Freeze recalc same agreed policy/confirmed history; needed Unknown keeps Indeterminate; no mutable/fake timestamp | Source correction + real evaluator/diagnostics |
| Active pin / Freeze boundary | R1 active-period pin, R2 becomes effective; expired Freeze candidate R2/null/required pending | Active R1 retained if valid; known frozen mismatch NeedsNewAdmission, pending DataNotReady; no repin/current candidate substitution | Real runtime policy/checkpoint + diagnostic presentation |
| Session renewal/correction/revoke | New CurrentPeriod, shrinking/expanding cutoff, selected/unselected revoke, restoration | New admission for known old basis/ineligible; Unknown not revoke; no TTL extension/resurrection/fallback; only selected scope | Actual session/binding/authority + typed outcomes |
| Runtime explainability boundary | Target Freeze/cutoff/null/Withdrawal/renewal/multi-license пояснения | Тексты соответствуют F12/F13; нет клиентского entitlement calculation, secrets или выдуманной diagnostics operation | UI; runtime correctness — реальные backend tests |
| Context/lifetime/guard | Deep link/refresh, switch scope, late response, dirty уход, double Save, keyboard SC03 | Exact scope; Save/Discard/Stay; одна pending mutation; поздний ответ не подменяет новый объект; focus восстановлен | Component/browser |
| Feature boundary | Policy Save при dirty assignment и existing publication/history | Сохраняется policy; соседний ввод/история/publication не переписаны; нет moderation/archive/delete policy | Реальный API/E2E |

MVP E2E после реализации: Product clear → system Freeze → Product Deny → Variant inheritance → Variant Freeze override → clear override → reopen → подтвердить saved own/source/effective/history и optimistic concurrency. Проверка existing period binding, строгого Delivery cutoff, позднего Unpublish/Retire, терминального Withdrawal, renewal/revoke и trusted multi-license проводится реальным backend контуром F12/F13; текст формы не является такой проверкой. Будущие тесты не выбирают открытый Q15/Q20 contract молча. Конкретная библиотека/runner не назначается этим документом. Применяется [S08](../standards/S08-testing.md).

Приёмка catalog context: Archive/Conflict сохраняют policy/history и existing entitlement binding, не запускают revoke/grant. Affected ambiguous Save получает server blocker с F15 переходом; unrelated scope доступен. Repair/Resolve не сохраняют policy скрыто.

## 11. Последовательность изменений и открытые вопросы

Сначала backend F12/F13 закрывает own state/read/history и historical binding/effective contracts, затем OpenAPI/NSwag и общую header infrastructure. Переиспользуются catalog context F06, blocker F15 и workspace guard. После этого собираются PolicyWorkspace/editor/Save action и read-only history/source views без объединения с assignment/publication Save. Проверяются local orchestration/HTTP metadata, затем реальный policy/history/concurrency путь. Документационная аналитика не меняет UI/API/generated код.

| Вопрос | Блокируемая деталь |
| --- | --- |
| [Q15: policy contracts и diagnostics](../../backend/questions/Q15-api-and-source-code.md#f12-policy-contracts-и-diagnostics) | Финальные methods/DTO/envelope, own-vs-composite ETag scope, independently mutable inherited/effective data, history endpoint/paging/ordering, failure/recovery и наличие публичной diagnostics операции |
| [Q20: PolicyAgreement/period](../../backend/questions/Q20-persistence-and-migration.md#f12-policyagreement-и-подтверждение-периода) | Immutable Agreement checkpoint/triple binding/current commercial facts/strict-set semantics приняты; concrete read/storage representation материализуется |
| [Q23: planned workflow](../../backend/questions/Q23-source-synchronization.md#f12-planned-agreement-confirmed-workflow) | Durable planned/quote/same-plan confirmation и renewal/correction ownership приняты; VR School issuance-screen детали не назначаются CM UI |
| [Q15: Agreement boundary](../../backend/questions/Q15-api-and-source-code.md#f12-policyagreement-integration-boundary) | Authenticated VRS backend-only issuance; exact integration/diagnostic routes/DTO/status/error/recovery carrier уточняется, staff/Unity issuance отсутствует |
| [Q17: confirmed visibility/history](../../backend/questions/Q17-publication-availability.md#подтверждённая-видимость-и-история-delivery) | Conservative once-only VisibilityConfirmedAt, strict cutoff/null/history completeness приняты; read representation/tie metadata материализуется |
| [Q20: Delivery confirmation](../../backend/questions/Q20-persistence-and-migration.md#delivery-catch-up-и-подтверждение-видимости) | Strict ordered outcomes/recovery/terminal ban/confirmation; policy Agreement checkpoint остаётся отдельным |
| [Q15: visibility carrier](../../backend/questions/Q15-api-and-source-code.md#delivery-visibility-contracts) | Exact readiness/history/confirmation/indeterminate diagnostics without new routes/automatic refresh by guess |
| [Q16: session/response leases](../../backend/questions/Q16-activation-and-sessions.md#принятый-протокол-сессий-и-response-leases) | Runtime absolute TTL/defaults/pin/correction/renewal/scoped revoke/deadline semantics selected; no staff issuance inputs |
| [Q20: session replay/storage](../../backend/questions/Q20-persistence-and-migration.md#сессии-replay-и-response-leases) | Own verifier/receipt and short encrypted capsule exception/primary authorization lease; exact storage adapters |
| [Q13](../questions/Q13-ui-scope.md) | Окончательная декомпозиция/presentation/testing; policy routes, source/effective, non-retro и компактная history приняты |
| [Q01](../../../business/questions/Q01-license-rights.md), [Q02](../../../business/questions/Q02-policy-changes.md), [Q03](../../../business/questions/Q03-renewal-and-multiple-licenses.md), [Q04](../../../business/questions/Q04-withdrawn-publications.md) | Подтверждённые бизнесовые правила применяются в UI; незакрытые source/runtime contracts остаются backend вопросами |

Подтверждённые исходники UI на 04.10.2026:

| Источник | Текущая реализация | Отличие от цели |
| --- | --- | --- |
| [PublicationWorkspacePage.razor](../../../../contentmanager.ui/src/ContentManager.Pages/Components/Publishing/PublicationWorkspacePage.razor), routes/Policy tab | `/publishing/products/{OwnerId}` и `/publishing/variants/{OwnerId}`; inline tab в общей странице; ровно два MVP values | Целевые workspace policy routes и самостоятельная policy composition; отдельного policy widget в проверенных исходниках нет |
| PublicationWorkspacePage Load/SaveChanges | Variant nullable; Product читает `_workspace.Policy ?? _workspace.EffectivePolicy`, clear доступен только Variant, null Product отклонён | Product own null должен оставаться различимым и очищаться к system default; hidden effective copy недопустима |
| PublicationWorkspacePage SaveChanges | Общая кнопка и Busy/Dirty; assignment затем policy; request `ExpectedRevision = _workspace.Revision`; после Save `await Load()` | Policy own guard/Save и DTO+ETag result; без скрытого exact GET и сброса соседнего ввода |
| PublicationWorkspacePage Policy tab | Показывает EffectivePolicy и текст «фиксируется при выдаче или продлении» | Нужны явный source/default/own и compact policy history; текст не доказывает историческую applicability/binding для delayed first use |
| [Generated client](../../../../contentmanager.ui/src/ContentManager.ApiClient/OpenApi/Generated/ContentManagerApiClients.g.cs), IPublishingClient/PolicyAsync/Policy2Async/SavePolicyRequest | PUT `api/v1/publishing/products/{id}/policy` и `api/v1/publishing/variants/{id}/policy`; `ExpectedRevision: long`, nullable Policy; result workspace envelope, per-call If-Match отсутствует | Target read/write/history и header contract требуют server OpenAPI/regeneration; enum уже содержит только два MVP values |
| Generated PublicationWorkspaceDto | Own Policy и EffectivePolicy вместе с shared Revision/assignment/releases/activations; отдельной policy version history в просмотренном DTO нет | Exact own state/ETag, source и immutable domain history; current workspace Revision не новый policy guard |
| [nswag.json](../../../../contentmanager.ui/src/ContentManager.ApiClient/nswag.json) | `ContentManager.ApiClient.Generated`, custom templates, DTO `init` accessors | Использовать существующую генерацию; direct generated editable request по согласованному контракту; не manual model copy |
| [Client DI](../../../../contentmanager.ui/src/ContentManager.ApiClient/DependencyInjection/ApiClientServiceCollectionExtensions.cs), [ConfigureApi](../../../../contentmanager.ui/src/ContentManager.ApiClient/DependencyInjection/ApiClientWebAssemblyHostBuilderExtensions.cs), [ApiEnvelopeDelegatingHandler.cs](../../../../contentmanager.ui/src/ContentManager.ApiClient/Infrastructure/Http/ApiEnvelopeDelegatingHandler.cs) | Scoped generated clients, HTTP factory/envelope/errors | База переиспользуется; наличие инфраструктуры не подтверждает caller ETag/If-Match для policy |
| [WorkspacePageBase.cs](../../../../contentmanager.ui/src/ContentManager.Shareds/Components/Shared/Infrastructure/WorkspacePageBase.cs) | Busy/Dirty/lifetime, discard guard; любой409 поясняется как изменение данных | Policy scope ownership, Save/Discard/Stay и distinct412/409/unknown outcome |

Current UI не подтверждает target runtime F13. В проверенном серверном пути текущая null publication проверяется до Freeze historical lookup, что расходится с сохранением frozen доступа после позднего обычного Unpublish; детали и серверные source evidence приведены в [backend F12](../../backend/features/F12-content-entitlement-policy.md). Признак `IsUnlimitedUsers` нельзя использовать как доказательство явной бессрочности обновлений: это не `IsUnlimitedLicense`/нормализованный updates unlimited. UI не выводит отсутствие cutoff как unlimited и не объявляет server runtime исправленным наличием пояснения.

В этой аналитике проверены структура документа и source evidence, а не запуск UI, backend tests, NSwag generation или browser flow. Целевые блоки и операции не объявляются реализованными.
