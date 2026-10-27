# F15. Разрешение конфликтов каталога: бэкенд

[Бизнесовая спецификация](../../../business/features/F15-catalog-conflicts.md) · [M03](../modules/M03-catalog.md) · [Фронтенд](../../frontend/features/F15-catalog-conflicts.md) · [F06](F06-product-catalog.md) · [Q25](../../../business/questions/Q25-catalog-conflicts.md#f15-технические-границы-разрешения).

## 1. Область реализации

F15 относится к MVP. CatalogAccess сохраняет управляемый конфликт между последним applied CM state и newest известным source snapshot VR School, если последствия нельзя безопасно применить к локальному CM состоянию. Минимальный сценарий — ProductVariantMove. Сам перенос допустим, stable Variant identity и локальные данные сохраняются. Источник остаётся VR School; CM не предлагает альтернативный ProductId, ignore/force или source CRUD.

Фича включает открытие/обновление одного конфликта, замену pending target более новым snapshot, безопасное автоматическое закрытие при исправлении источника, очередь Open/history, exact read, ручную повторную проверку и применение authoritative target, отдельный corrective repair Variant working patch в Publishing и read-only operation guard для зависимых фич. Новые затронутые writes блокируются; unrelated scope продолжает работать. Delivery по уже materialized revision и подтверждённому entitlement, включая создание новой session, не запрещается конфликтом: отдельные основания Withdrawal/revoke/entitlement принадлежат F10–F13. Demo не требует F15, внешнего каталога, шины или этого workflow.

Имена новых типов ниже — целевые обозначения, не наличие исходников. Проверенная исходная точка на 05.10.2026:

| Источник | Реализовано сейчас | Необходимое отличие цели |
| --- | --- | --- |
| [ProductVariant.cs](../../../../contentmanager.api/src/BoundedContexts/CatalogAccess/ContentManager.CatalogAccess.Domain/ProductVariants/ProductVariant.cs) | ProductId getter-only; ApplySnapshot сначала бросает exception при другом parent, затем проверяет revision | F06 допускает move; несовместимые последствия становятся durable Open conflict, AppliedState сохраняется |
| [CatalogConsumers.cs](../../../../contentmanager.api/src/App/ContentManager.Application/Integration/CatalogConsumers.cs), [CatalogSnapshotReceiver.cs](../../../../contentmanager.api/src/App/ContentManager.Application/Integration/CatalogSnapshotReceiver.cs), [CatalogSnapshotProcessor.cs](../../../../contentmanager.api/src/App/ContentManager.Application/Integration/CatalogSnapshotProcessor.cs) | Consumer → receiver; processor вызывает ApplySnapshot после EnsureExists parent; применяется history/revision flow | Normalized full-snapshot/newest watermark F06, local transaction pending/conflict/inbox; dependency absence и business blockers не permanent consumer failure |
| [CatalogAccessDbContext.cs](../../../../contentmanager.api/src/BoundedContexts/CatalogAccess/ContentManager.CatalogAccess.EntityFramework/CatalogAccessDbContext.cs), [ProductVariantConfiguration.cs](../../../../contentmanager.api/src/BoundedContexts/CatalogAccess/ContentManager.CatalogAccess.EntityFramework/Configurations/ProductVariantConfiguration.cs) | Нет DbSet конфликта; required Product FK, SourceRevision concurrency, unique ProductId/Name | Own local int Version отдельно SourceVersion, conflict storage и unresolved parent layout F06/Q23 |
| Поиск CatalogConflict / ICatalogOperationBlockReader в src без bin/obj/Migrations | Типы и внешний conflict API не найдены; CatalogAccess.Application directory отсутствует | Application use cases, Persistence reads, Api/DI и inter-BC guards предстоит реализовать |

Полная целевая реализация не сводится к снятию exception: нужно сохранить ordering, blocker scope, историческую неизменяемость и конкурирующие записи. Граница ремонта согласована ниже; точные CLR/wire/routes, pending mappings и materialization принятого MVP coordination protocol остаются Q15/Q23/Q20. Specialist permissions и committed repair/resolve audit принадлежат F16/Q10 и [F18](F18-staff-activity-history.md); admin-exclusive доступ не вводится.

## 2. Domain

### 2.1. Aggregates, entities и value objects

CatalogAccess владеет конфликтом. ProductVariant и normalized ProductVariantSnapshot/AppliedProductVariantState переиспользуются из [F06](F06-product-catalog.md#21-aggregates-entities-и-value-objects), их поля повторно не определяются. SourceVersion — selected normalized int; V2 contracts/receiver/work/fault models defined once F06. V1 long checked range/continuity rollout separate, no contract rename. Local Version не является SourceVersion.

| Модель | Роль | Состояние / lifecycle | Владелец / граница |
| --- | --- | --- | --- |
| CatalogConflict | Aggregate root | Open → Resolved; один newest pending target; resolved history сохраняется | CatalogAccess; local int Version |
| CatalogConflictId | Strong identity | Immutable server ID нового conflict episode | CatalogAccess; не VariantId или EventId |
| CatalogConflictKind | Enum | MVP значение ProductVariantMove; другие категории не назначены | F15/Q25 |
| CatalogConflictStatus | Enum | Open, Resolved | Никакого Delete/Ignore состояния |
| CatalogConflictResolutionKind | Предметный outcome | ManualApply либо SourceChangedApply как logical outcomes; точные enum/wire значения Q15 | Сервер задаёт, клиент не выбирает source relation |
| CatalogConflictIssue | Immutable captured diagnostic value | Последняя подтверждённая оценка при открытии/обновлении | Не independently mutable Publishing entity |
| ProductVariantMoveImpact | Application/read result, поля §3.3 | Живые факты и blockers выбранного target | Adapter к публичному Publishing boundary; не Domain root |

**CatalogConflictId**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Value | `Guid` | Да | Server-created conflict identity | Stable в одном episode; resolved ID не переиспользуется новым Open |

**CatalogConflict**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | `CatalogConflictId` | Да | Identity episode | Серверный, immutable |
| Kind | `CatalogConflictKind` | Да | Категория конфликта | MVP ProductVariantMove; не произвольная строка клиента |
| EntityId | `ProductVariantId` | Да | Affected source Variant | Stable source identity F06; расширение на Product conflicts Q25 |
| AppliedState | `AppliedProductVariantState` | Да | Captured applied basis при открытии move conflict | Immutable diagnostic basis episode; не смесь old/new; уже существующий applied Variant |
| AppliedSourceVersion | `int` | Да | Версия captured applied basis | Производное AppliedState.SourceVersion; не независимо mutable копия |
| PendingState | `ProductVariantSnapshot` | Да | Единственная full normalized authoritative target | Заменяется целиком только более новой source version; хранит source parent/name/lifecycle |
| PendingSourceVersion | `int` | Да | Версия newest target | Производное PendingState.SourceVersion; согласовано с watermark F06 |
| Status | `CatalogConflictStatus` | Да | Open либо Resolved | Default unresolved queue = Open; Resolved terminal для episode |
| Issues | `IReadOnlyList<CatalogConflictIssue>` | Да | Captured причины/зависимости | Captured assessment bound AssessedPendingSourceVersion; superseding target does not make old Issues current; not foreign-fact permit |
| AssessedPendingSourceVersion | `int?` | Условно | SourceVersion bound to captured Issues/ImpactObservedAt | New pending target preserves old diagnostics as stale until fresh assessment |
| AssessmentCurrent | `bool` | Да | Derived match of assessed target and current latest accepted target | No independent freshness authority; active fault/foreign gaps deny current proof |
| ImpactObservedAt | `DateTimeOffset` | Да | Server time last confirmed assessment | Version-bound, not source ordering/permit |
| DetectedAt | `DateTimeOffset` | Да | Server time открытия | Сохраняется при supersede |
| ResolvedAt | `DateTimeOffset?` | Условно | Server time успешного apply/close | Null пока Open; заполнено после successful commit |
| ResolutionKind | `CatalogConflictResolutionKind?` | Условно | Server outcome закрытия | Null Open; конкретные значения/serialization Q15 |
| ResolvedBy | Guid? | Условно | Trusted CM staff identity for ManualApply | Required manual resolved outcome; null Open/SourceChangedApply System, no fake CM staff; public actor presentation Q15 |
| Version | `int` | Да | Own optimistic concurrency | Internal; изменяется при committed pending/status/owned issues mutation; не DTO |

Missing parent до первого apply остаётся F06 pending relation, не автоматически ProductVariantMove conflict. Receive→unresolved→Open single-payload handoff is selected F06: receiver acceptance with Open atomically replaces its newest PendingState, receipt/watermark/work and stale assessment marker; work contains refs/version only. Missing parent keeps same Open/waiting, never stale compatible proof. При конфликте PendingState является единственным mutable owner payload; Variant хранит watermark/связь, но не второй независимо изменяемый target. Captured AppliedState в history не становится live pointer. После resolve последний pending snapshot сохраняется для объяснения outcome, а Variant получает полное applied состояние.

**CatalogConflictIssue**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Category | Уточняется | Да | Смысл причины несовместимости | Категории/коды Q25/Q15; без invented string codes |
| AffectedObject | Уточняется | Условно | Typed ID/context зависимого CM объекта | Kind/ID representation Q25/Q15; не EF graph |
| Operation | Уточняется | Условно | Какая операция зависит от конфликта | Семантическая матрица §2.3; wire enum Q25 |
| Explanation | `string` | Да | Пригодная для UI диагностическая причина | Localized/text profile/limits Q15; не secrets/full source payload |
| Evidence | Уточняется | Условно | Основа captured проверки | Version/guard profile Q23/Q20; не permission обхода |

### 2.2. Методы и переходы

| Тип | Метод | Параметры | Предусловия | Изменение / результат |
| --- | --- | --- | --- | --- |
| CatalogConflict | Open | id, AppliedProductVariantState, ProductVariantSnapshot, captured issues/time | Existing applied Variant, тот же source ID, impact confirmed incompatible | Open episode; fixed applied basis, newest pending, own token |
| CatalogConflict | ReplacePending | snapshot, issues/time | Open, тот же EntityId, incoming SourceVersion newer than existing pending and equals authoritative accepted target at commit | Whole newest PendingState replacement; prior Issues/assessedVersion stale until assessment, DetectedAt/applied basis retained |
| CatalogConflict | ResolveApplied | outcome, trusted actor/time | Open, проверенный newest target и совместимость | Resolved metadata/history; сам метод не меняет чужой BC |
| ProductVariant | ObserveSnapshot / ApplyObservedSnapshot | Full snapshot F06 | Same ID; authoritative newest candidate, parent/impact checks | Watermark/pending либо полная applied группа; сохраняет identity/local CM links |

Applied state + conflict closure фиксируются одной CatalogAccess transaction. Domain метод не выполняет HTTP/foreign EF queries, не доказывает внешнюю совместимость без facts Application и не принимает произвольный ProductId из UI. Ранее Resolved episode не переоткрывается; новый последующий конфликт создаёт новый Id.

### 2.3. Инварианты и constraints

1. Максимум один Open conflict для Kind + EntityId. Equal/stale snapshot не заменяет pending и не создаёт второй episode. SourceVersion сравнивается с latest observed, а не только applied.
2. Пример applied v18/A, pending v21/B: вход v20/C не меняет target. Resolve v21 после получения v22 не применяет v21. Совместимость проверяется относительно newest известного CatalogAccess state, не обещает синхронный запрос «самой новой» remote DB.
3. Pending target типизирован и полон. Невозможность прочитать parent/impact не равна «нет blockers». Недоступный dependency port является технической ошибкой/ожиданием, не решением безопасного move.
4. Новый snapshot, исправляющий source relation/последствия, может быть применён и закрыть Open автоматически после тех же guards. Возврат к applied parent не объявляется всегда безопасным без повторной проверки lifecycle/local facts.
5. Repair/Resolution не переписывают старые Releases/PublicationRevisions, не пересчитывают историческую composition и не меняют pinned Delivery session. Resolve не публикует, не Activate/Unwithdraw и не выдаёт grant; эти действия остаются отдельными защищёнными операциями. Новая публикация после move проходит обычный F09–F13 workflow.
6. По [S21](../standards/S21-aggregate-constraints.md) root constraints принадлежат целевому CatalogConflictConstraints: допустимые статусы/transition, single Open и согласованность snapshot identities/version/metadata. Source limits F06 переиспользуются; числовые лимиты explanation/history/paging не выбираются здесь.

Матрица operation guards:

| Операция | При Open conflict | Граница |
| --- | --- | --- |
| Assignment/override mutation, release creation, new revision creation/activation, automatic promotion | Блокировать конкретный affected scope | F07–F11; unrelated операции работают |
| Policy change | Блокировать при неоднозначном affected Product/Variant scope | F12; не blanket запрет всех policies |
| New moderation submission | Блокировать, если revision нельзя достоверно валидировать | F17; activation/Automatic promotion/late approval повторно проверяют affected scope и basis; future precedence рассматривается отдельно Q25 |
| Read catalog/current/history, conflict read/resolve | Разрешено при обычных правах | Блокировка не отнимает просмотр/разрешение |
| Delivery по materialized revision и confirmed entitlement, включая new session | Conflict сам по себе не блокирует | Независимые Withdrawal/revoke/entitlement guards F12/F13 сохраняются |
| Variant working patch corrective repair | Отдельная Publishing command с exact conflict/newest source/captured basis и собственным If-Match | DF07; ordinary blocked Save и ignoreConflict запрещены |
| Explicit safety Unpublish / Withdrawal | Разрешены с обычными permissions/preconditions | Archive/Conflict не запрещают безопасное отключение; другие actions отдельны |

Scope вычисляется сервером из Variant и applied/pending parent/dependencies; authoritative affected closure включает только действительно затронутые mutable/in-flight consumers общих Product assignments/base/policy. Блокировка всего каталога или всех Variant обоих Product по умолчанию запрещена. Точный CLR contract closure и защита от новых конкурентных зависимостей — Q15/Q23/Q20.

Move blocker — конкретная несовместимость mutable/in-flight CM состояния с newest target: effective duplicate/collision prospective composition, incompatible working rule/reference, pending activation/promotion/moderation или policy operation с invalidated scope/basis. Historical immutable releases/revisions сами по себе blockers не создают. Отсутствие override target A в prospective Product base не blocker: trusted A → B сохраняет local result B по F08. Если prospective base независимо содержит B, тот же A → B создаёт duplicate effective B и является blocker; сравнение по logical result identity, не по разным version IDs.

Первый corrective scope ограничен сохранённым Variant working patch: удалить conflicting rule/local addition; заменить уже проблемный result eligible reference; добавить/изменить inherited exclusion, чтобы убрать blocker. Любая коррекция проходит intrinsic validation, eligibility нового reference, scope и no-expansion guard; общее добавление/расширение состава не разрешено. Other blockers требуют отдельного явно разрешённого действия в owning feature либо source correction VR School. Archived own/parent разрешает только обслуживание из F06, и не расширяет repair permission.

Диагностика/preview сравнивает три отдельные основы: обычную applied composition, prospective composition newest pending parent и proposed repair composition. Standard workspace использует applied parent до успешного Resolve. Prospective preview не изменяет ProductId, не выбирает другой parent и не считается publication или успешным resolve.

### 2.4. Domain events

Нового обязательного Domain/integration event ради логирования F15 не вводит. Detection/resolve являются local committed facts; внешние сообщения нужны только при определённом потребителе и отдельном согласованном контракте. F06 использует входящий source snapshot/inbox, F11–F13 свои Publishing события. Conflict не генерирует Withdrawal или revoke.

## 3. Application.Abstractions

### 3.1. Commands

| Command | Назначение | Result | Concurrency |
| --- | --- | --- | --- |
| OpenOrUpdateCatalogConflictCommand | Внутреннее применение normalized newest source candidate F06 | Local apply/pending/conflict outcome F06 | Root/watermark/unique Open guards; не HTTP If-Match |
| ResolveCatalogConflictCommand | Manual revalidate и apply authoritative target | Confirmed CatalogConflictDto + own metadata | ExpectedVersion из exact conflict If-Match; Variant own token и newest guard внутри |

**OpenOrUpdateCatalogConflictCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Pending | `ProductVariantSnapshot` | Да | Normalized full source target F06 | Не raw external contract, latest observed guard; stored payload single owner |
| Applied | `AppliedProductVariantState` | Да | Loaded applied basis | Серверный, не client input; совпадает с tracked Variant |
| Impact | `ProductVariantMoveImpact` | Да | Confirmed typed оценка последствий | Loaded through public port; evidence protocol Q23/Q20 |

Command is internal worker-stage action after durable F06 receiver acceptance, not public endpoint/shared receiver-worker transaction. NEW first-gate worker rereads latestaccepted/fault/claim-deadline/dependencies then atomic payload handoff/conflict/apply/history/work completion. Existing Open target supersession belongs receiver acceptance commit; immutable receipts not cloned Domain roots. Side effects after own commit.

**ResolveCatalogConflictCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ConflictId | `CatalogConflictId` | Да | Exact episode из route | Не parent selection |
| ExpectedVersion | `int` | Да | Own precondition | Api связывает с If-Match; не DTO/source version |

Actor/correlation/time приходят через trusted execution context и clock, не из request. Corrective repair — отдельная Publishing command, не mutation CatalogAccess или произвольный payload в Resolve. Следующие имена — предложенные логические contracts; точные CLR/wire names и shapes остаются [Q15](../questions/Q15-api-and-source-code.md#f15-conflict-api-и-read-contracts).

**RepairVariantWorkingPatchCommand** — предложенное логическое имя, owning BC Publishing.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductVariantId | Уточняется | Да | Exact Publishing working patch scope | Stable Variant; typed reference CLR Q15, не новый source ID |
| ConflictId | Уточняется | Да | Exact Open episode для corrective действия | Публичный CatalogAccess ID contract Q15; не owner Domain dependency |
| PendingSourceVersion | `int` | Да | Newest source basis, выбранный для коррекции | Совпадает с authoritative pending/watermark; не If-Match |
| CapturedRepairBasis | Уточняется | Да | Basis saved patch, pending parent и prospective dependencies | Состав/CLR/wire/fencing Q15/Q23/Q20; concurrent source/patch change отклоняет |
| Correction | Уточняется | Да | Proposed Variant patch correction | Только §2.3 операции, intrinsic/reference/scope/no-expansion guards; shape Q15 |
| ExpectedPatchVersion | `int` | Да | Own patch optimistic precondition | Api maps required If-Match; internal, отсутствует в DTO |

**VariantWorkingPatchRepairRequest** — предложенное логическое имя input, не готовый endpoint DTO.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ConflictId | Уточняется | Да | Exact episode | Wire Q15; server validates Variant membership |
| PendingSourceVersion | `int` | Да | Captured newest target basis | Не допускает автоматического repair другого newest target |
| CapturedRepairBasis | Уточняется | Да | Подтверждаемая captured основа proposal | Wire/evidence Q15/Q23/Q20; не бессрочный permit |
| Correction | Уточняется | Да | Scoped candidate repair | Единственное логическое определение §2.3; nested shapes Q15, не обычный Save body с ignoreConflict |

Required If-Match для repair относится к exact mutable patch F08. Conflict/source/repair basis проверяются отдельно. Result — confirmed saved patch, чья модель принадлежит F08; literal wrapper и repair-specific metadata остаются Q15, без выдуманного нового DTO inheritance.

### 3.2. Queries

| Query | Назначение | Result |
| --- | --- | --- |
| ListCatalogConflictsQuery | Open queue либо явно выбранная history/status | Offset list CatalogConflictListItemDto; полный paging carrier Q15 |
| GetCatalogConflictQuery | Core owned exact состояние | CatalogConflictDto + internal own Version carrier для ETag |
| GetCatalogConflictImpactQuery | Live dependency оценка для detail/diagnostics | ProductVariantMoveImpact; отдельный read без root ETag, exposure Q15 |
| GetCatalogOperationBlockQuery | Read-only guard зависимой операции | CatalogOperationBlockDecision |
| GetCatalogRepairPreviewQuery (предложенное имя) | Сравнить applied/prospective/proposed-repair composition | Read-only логическая модель ниже; CLR/wire Q15 |

**ListCatalogConflictsQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Parameters | Уточняется | Уточняется | Filter/page/sort request | Logical parameters §3.3; names/default/limits Q15 |

**GetCatalogConflictQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ConflictId | `CatalogConflictId` | Да | Exact own-state read episode | Resolved history допускается читать; no mutation on GET |

**GetCatalogConflictImpactQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ConflictId | `CatalogConflictId` | Да | Exact basis для live dependency оценки | Separate result; не own-root ETag; no mutation on GET |

**GetCatalogOperationBlockQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | `ProductId?` | Условно | Контекст вызывающей операции | Требуемый scope определяется операцией; не wildcard для всех объектов |
| ProductVariantId | `ProductVariantId?` | Условно | Stable affected Variant | Matched scope/parent evidence Q25 |
| OperationKind | Уточняется | Да | Смысл planned operation | Known матрица §2.3; точный enum/wire Q25 |

**GetCatalogRepairPreviewQuery** — предложенное имя; read orchestration и точный owner/adapters Q15/Q20.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ConflictId | `CatalogConflictId` | Да | Exact basis read | No writes/resolve on preview |
| PendingSourceVersion | `int` | Да | Target, для которого готовится proposal | Server rejects stale proposal basis |
| Correction | Уточняется | Да | Предложенная scoped patch correction | Shape Q15; те же §2.3 ограничения, не permission bypass |

### 3.3. DTO и parameter models

Модели ниже содержат логический минимальный состав; неизвестные nested wire shapes остаются Q15/Q25. DTO не имеет Version/ExpectedVersion. Для public Guid IDs mapping с typed Domain ID явный; названия JSON properties/final response carrier уточняются Q15.

**CatalogConflictListParameters** — логическое обозначение, точное request имя не принято.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Status | Уточняется | Нет | Open/default или явный Resolved/history выбор | Default Open; exact filter wire Q15 |
| EntityId | `Guid?` | Нет | Ограничить Variant scope | Server-side filter |
| Kind | Уточняется | Нет | Категория conflict | MVP ProductVariantMove; wire enum Q15 |
| Search | `string?` | Нет | Поиск diagnostic context | Search fields/case/limits Q15; имя не identity |
| Page | Уточняется | Нет | Offset pagination | Concrete fields/default/range Q15 |
| Sort | Уточняется | Нет | Whitelist ordering | Deterministic stable Id tie-breaker; whitelist/default Q15 |

**CatalogConflictListItemDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | `Guid` | Да | Conflict identity | Stable episode |
| Kind | Уточняется | Да | Conflict category | Wire enum/profile Q15 |
| EntityId | `Guid` | Да | Affected Variant | CatalogAccess source ID |
| DiagnosticLabel | `string?` | Нет | Подпись для очереди | Snapshot/current label origin Q15; not identity; list не per-row ETag |
| AppliedProductId | `Guid` | Да | Captured old parent | Snapshot basis |
| PendingProductId | `Guid` | Да | Newest source target parent | Не выбирается в CM |
| PendingSourceVersion | `int` | Да | Freshness pending | Не concurrency token |
| Status | Уточняется | Да | Open/Resolved | Default query Open |
| DetectedAt | `DateTimeOffset` | Да | Server detection time | Сохраняется при supersede |
| BlockerSummary | Уточняется | Уточняется | Captured summary/count | Count/category shape Q15/Q25; не current authorizing evidence |

**CatalogConflictDto** — core representation собственного ресурса. List DTO не наследуется автоматически: current label/overlay не переносятся в body, маркируемый own ETag.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | `Guid` | Да | Conflict episode | Own resource |
| Kind | Уточняется | Да | Category | Known semantic ProductVariantMove, wire Q15 |
| EntityId | `Guid` | Да | Source Variant identity | Immutable |
| AppliedState | Уточняется | Да | Captured applied facts F06 mapped to public snapshot DTO | Exact fields reuse F06; DTO shape Q15, no foreign tracked entity |
| PendingState | Уточняется | Да | Full newest source target | Snapshot read only; typed Domain not directly public EF graph |
| AppliedSourceVersion | `int` | Да | Captured source basis | Equal AppliedState source version |
| PendingSourceVersion | `int` | Да | Current target basis | Equal PendingState source version |
| Status | Уточняется | Да | Open/Resolved | Own committed state |
| Issues | Уточняется | Да | Captured issue diagnostics | CatalogConflictIssue semantic map; nested wire Q15/Q25 |
| AssessedPendingSourceVersion | `int?` | Условно | SourceVersion of captured Issues | Old assessment retained but not current new-target proof |
| AssessmentCurrent | `bool` | Да | Typed current-target assessment status | Independent active faults/latestaccepted checked, no commit permit |
| ImpactObservedAt | `DateTimeOffset` | Да | Captured diagnostic as-of | Version-bound, not live freshness |
| DetectedAt | `DateTimeOffset` | Да | Open time | Immutable |
| ResolvedAt | `DateTimeOffset?` | Условно | Confirmed closing time | Null Open |
| ResolutionKind | Уточняется (nullable) | Условно | Outcome закрытия | Null Open; server-selected enum profile Q15 |
| ResolvedBy | Уточняется (nullable) | Условно | Trusted actor representation | Public visibility/type Q15; auto/manual distinction |

Links в UI строятся по IDs; отдельная DTO коллекция arbitrary navigation URL не проектируется. Live impact/labels/operation eligibility имеют собственный read lifecycle и не считаются частью strong own-root ETag core body.

**ProductVariantMoveImpact**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductVariantId | `ProductVariantId` | Да | Scope оценки | Same entity pending/applied |
| AppliedProductId | `ProductId` | Да | Old relation | Captured applied basis |
| PendingProductId | `ProductId` | Да | Target relation | Authoritative PendingState |
| PendingSourceVersion | `int` | Да | Target оценки | Must still match watermark at apply |
| Issues | `IReadOnlyList<CatalogConflictIssue>` | Да | Подтверждённые blockers | Empty только при complete confirmed evaluation; unavailable не empty |
| AffectedDependencies | Уточняется | Да | Typed dependency identities/context | Publishing public projection; closure/evidence Q25/Q23 |
| ObservedAt | `DateTimeOffset` | Да | Server observation time | Не fence и не предотвращение race |
| Evidence | Уточняется | Да | Guard/versions для coordinated apply | Формат evidence уточняется Q23/Q15; MVP guard protocol принят Q20, query-before-commit alone недостаточен |

**CatalogOperationBlockDecision**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| IsBlocked | `bool` | Да | Подтверждённая blocked/unblocked оценка операции | Техническая недоступность не маскируется false |
| ConflictIds | `IReadOnlyList<CatalogConflictId>` | Да | Matching Open episodes | Empty только при complete unblocked read |
| Reason | Уточняется | Условно | Typed объяснение отказа | Codes/nested wire Q15/Q25 |
| Evidence | Уточняется | Условно | Вход shared writer coordination | Guard validity/Q23/Q20; не клиентский bypass token |

**CatalogRepairPreview** — предложенная логическая read model; точные CLR/wire contracts Q15, definitions composition переиспользуются из F08.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ConflictId | Уточняется | Да | Episode comparison | Stable identity wire Q15 |
| PendingSourceVersion | `int` | Да | Newest source target comparison | Bound к authoritative pending |
| AppliedComposition | Уточняется | Да | Ordinary applied base + saved patch | F08 composition; не prospective substitute |
| ProspectiveComposition | Уточняется | Да | Newest pending parent base + saved patch | Read only, parent берётся из pending |
| ProposedRepairComposition | Уточняется | Да | Та же prospective base + proposed correction | Не publication/resolve; same scope/basis |
| CapturedRepairBasis | Уточняется | Да | Основа последующей repair command | Source/patch/dependency evidence Q15/Q23/Q20 |
| Diagnostics | Уточняется | Да | Concrete blockers до/после correction | Complete evaluation; unavailable не empty blockers; wire Q15 |

### 3.4. Ports

| Port | Назначение | Направление |
| --- | --- | --- |
| ICatalogConflictRepository | Tracked exact root/GetOpen(kind, entityId)/Add | Outbound Persistence своего BC |
| IProductVariantRepository | Tracked F06 root и own Version/watermark | CatalogAccess local UoW |
| IProductVariantMoveImpactReader | Evaluate(VariantId, applied state, pending snapshot) → immutable impact | Adapter к public Publishing query boundary; без foreign contexts |
| ICatalogOperationBlockReader | IsBlocked(ProductId?, VariantId?, OperationKind) → decision | Inbound CatalogAccess для F07–F12/F17 |
| Publishing corrective command boundary (предложенное логическое имя) | Scoped repair в Publishing через публичный contract; separate commit | Inbound Publishing, без foreign DbContext; exact signatures Q15/Q20 |
| Writer coordination boundary | MVP global transaction advisory key; fresh impact/repair basis и competing writes до apply | Protocol принят Q20; exact port/DI signatures Q15/Q23, без нового lock service |
| TimeProvider / trusted execution context | Clock и actor/correlation | Existing Host infrastructure; no client time/actor |

Read contracts объявляются в Application.Abstractions, реализации queries — EntityFramework.Persistence по [S20](../standards/S20-cqrs.md). Чужие mutable Domain/EF entities через port не проходят. Интеграционный adapter и Host DI связывают public BC contracts, не разрешают CatalogAccess напрямую изменять Publishing.

MVP coordination port принадлежит Application.Abstractions: задаёт границу guarded owner mutation, cancellation и outcome, без PostgreSQL/Npgsql/EF типов в Domain. Точные CLR-имена и signatures остаются Q15. Application открывает owning UoW и требует guard до authoritative reads; EntityFramework.Persistence выполняет standalone pg_advisory_xact_lock на той же physical connection и transaction владельца. Public foreign read adapters используют независимые primary read scopes/connections без ambient UoW enlistment. Host/DI отделяет owning UoW от этих scopes; существующий [EfCoreUnitOfWorkParticipant](../../../../contentmanager.api/src/BuildingBlocks/ContentManager.EntityFramework.UnitOfWork/EfCoreUnitOfWorkParticipant.cs), способный enlist несколько contexts в общую SQL transaction, не выполняет эту целевую границу автоматически. Domain получает уже проверенные предметные facts и не содержит SQL guard type.

### 3.5. Specifications / read contracts

По [S19](../standards/S19-specifications.md) OpenByEntity/ExactId/status-kind-entity filters являются атомарными builder predicates; queue/history/list-count используют одну композицию conditions. Не выдумывать spec.And и repository для каждой issue. Projection выбирает DTO fields без tracked graph; server-side paging/sort с stable Id tie-breaker. Current dependency impact отдельный read, не save диагностики при GET. Для detail root state и own Version считываются согласованно; response metadata carrier/profile Q15.

## 4. Application

### 4.1. Handlers / use cases

| Use case / физический слой | Загрузка и проверки | Записи / результат |
| --- | --- | --- |
| F06 detection, CatalogAccess.Application | Tracked Variant, source watermark, pending owner, current parent facts, complete move impact | Open/update conflict или safe apply в той же local UoW; inbox/observed outcome F06 |
| Supersede/auto-resolution, CatalogAccess.Application | Latest source candidate, существующий Open, parent readiness и текущие dependencies | ReplacePending либо полное apply + ResolveApplied; stale no-op |
| ResolveCatalogConflictHandler, CatalogAccess.Application | Exact Open и own token, tracked Variant/watermark, newest PendingState, parent/lifecycle/new-use facts, impact/writer guards | Полное apply Variant + Resolved history одной CatalogAccess transaction; response после commit |
| List/Get/Block query handlers, EntityFramework.Persistence | Own projections/specifications и read ports | Read-only DTO/decision; никакого изменения Version/закрытия при GET |
| Live impact/repair preview query adapter, composition boundary | Own exact basis через CatalogAccess public query + public Publishing projection; applied/prospective/proposed-repair | Nontracked structured result/error; no writes/foreign UoW |
| RepairVariantWorkingPatchHandler (предложенное имя), Publishing.Application | Own exact patch/token, Open conflict/newest source/captured repair basis через public ports; intrinsic/new-use/scope/no-expansion validation | Коммитит только Publishing working patch; actor/audit F18; conflict остаётся Open |

Command handler не помещается в EF.Persistence, query handler не переносится в Application лишь ради одинакового namespace. F06 consumer нормализует транспорт и вызывает use case, но не хранит бизнес-логику разрешения в RabbitMQ consumer. Конкретное registration новое CatalogAccess.Application/handlers/ports задаёт Host; current App Integration остаётся source gap, не целевой owner.

### 4.2. Validators

По [S18](../standards/S18-fluent-validation.md) переиспользовать атомарные правила ConflictId, source identity/full snapshot F06, source version/profile и semantic operation scope. List validators композируют принятую paging/sort whitelist после Q15; не выбирать лимиты по аналогии. Resolve валидирует только exact ID и внутренний expected int Version, не принимает новый ProductId, permission flag, arbitrary actor или payload локального «решения». Repair validator отдельно композирует intrinsic F08 constraints, eligibility только новых/заменённых references, допустимость corrective delta и exact conflict/newest/captured basis. Ordinary Save validator не получает ignoreConflict flag.

Mutable dependency facts, newest watermark, parent readiness и lifecycle не являются достаточной FluentValidation async проверкой до transaction. Handler revalidates их с принятым guard protocol перед apply; различает подтверждённые blockers и port unavailability. Нормализация/версия source контракта принадлежит F06/Q23, не дублируется внутри F15.

### 4.3. Mappers

External ProductVariantSnapshotV1 → normalized ProductVariantSnapshot выполняет F06 adapter. Domain snapshots → public read snapshot DTO сохраняют source IDs, версии и полное состояние; old Name/parent не соединяются с pending version. Core conflict → DTO и own Version carrier разделены. Issue mapping сохраняет category/affected identity/as-of, но не переносит Publishing EF graph или credentials. Resolve response мапится из confirmed saved state, не optimistically из введённого клиентом ProductId.

### 4.4. Services / algorithms

MoveImpactEvaluator читает через public boundaries local assignments/Variant patches, relevant Product base и mutable/in-flight revision/promotion/moderation/policy operations. Concrete blockers/repair scope заданы §2.3. Immutable release/revision сохраняет captured context и сам по себе не blocker. Evaluator строит authoritative affected closure и prospective composition; missing A vs duplicate B проверяются по F08 stable identity semantics, не по факту смены parent. Target меняется только из VR School snapshot.

Local CatalogAccess concurrency решает latest pending/open uniqueness и races source/resolve. Оно **не** гарантирует неизменность Publishing между read impact и CatalogAccess commit. Все affected writers участвуют в принятом MVP global transaction advisory guard Q20/§7.2. Ключ берётся до authoritative facts/row locks; foreign public reads имеют independent primary scope, а commit остаётся owner-local. Защита также охватывает parent/source lifecycle facts F06: mere tracking прочитанного Product без проверяемого guard не гарантирует его неизменность. Не считать second query, timestamp, два независимых ETag, краткую транзакцию только CatalogAccess или read-only ICatalogOperationBlockReader доказательством закрытой меж-BC гонки. Cross-BC transaction/foreign DbContext не допускаются.

## 5. Api

### 5.1. Endpoints

Это целевая поверхность F15; в текущем src не подтверждена. Literal routes и точные DTO для live impact/repair/preview — Уточняется (Q15). Corrective use case согласован; неизвестный transport не отменяет его scope.

| Method | Route / boundary | Request | Response | Access |
| --- | --- | --- | --- | --- |
| GET | /api/v1/catalog-conflicts | Queue filters; default Open | ApiResponse list CatalogConflictListItemDto; paging profile Q15 | Specialist F16/Q10; exact permission Q15 |
| GET | /api/v1/catalog-conflicts/{conflictId} | Exact Guid | ApiResponse CatalogConflictDto + own ETag | Read/history permitted by F16/Q10 |
| POST | /api/v1/catalog-conflicts/{conflictId}/resolve | No body; required If-Match | 200 ApiResponse CatalogConflictDto + new own ETag после commit | Resolve authorized specialist; concrete permission Q15 |
| Read boundary TBD | Exposure GetCatalogConflictImpactQuery / GetCatalogRepairPreviewQuery | ConflictId + newest basis/proposal для preview | Live impact либо applied/prospective/proposed-repair, без own-root ETag | Q15; exact exposure/DTO Уточняется |
| Mutation boundary Уточняется | Publishing corrective repair, route/method Q15 | Variant patch scope + request §3.1; required own If-Match | Confirmed saved patch F08 после Publishing commit; no resolve/publication | Authorized specialist по F16/Q10; exact permission Q15 |
| Same-process query | ICatalogOperationBlockReader | Scope + operation | Confirmed decision/error | Internal public BC contract; no HTTP self-call |

Resolve body не выбирает parent, не передаёт local content changes и не включает Version. Source repair в VR School не является HTTP action CM. Уточнение body/audit/wire carrier/security/error code profile — Q15; scope local repair принят §2.3/DF07; exact API/wire profile остаётся Q15.

### 5.2. ETag / If-Match

Применяется [S16](../standards/S16-rest.md): strong numeric ETag из own internal int Version exact CatalogConflict, без Version в DTO; required If-Match → missing428, stale412. SourceVersion/watermark/source business order не заменяют local token. Source supersede/auto-close увеличивает own token и делает прежнее клиентское основание stale.

List не содержит per-row ETag. Core exact body состоит из собственного captured state и metadata. Live impact/current labels/eligibility внешних owners не помечаются этим token: отдельный read без own ETag или согласованный composite protocol Q15. Current dependency facts не становятся верными для apply только из-за совпадения If-Match.

Own mismatch классифицируется 412; при unchanged own token, но предметно изменившейся внешней основе/оставшихся blockers — 409 с актуальной диагностикой по Q15. Уникальный Open/own Variant storage races не сводятся все к client412: после rollback fresh scope классифицирует own precondition, source advancement и internal competing consumer, не возвращает подготовленный unsaved DTO.

### 5.3. OpenAPI и ошибки

Api adapter bind Guid/If-Match/cancellation → command/query bus → общий result mapping. Document required header, no body, exact 200 result/headers, 400 invalid input, 404 missing, 428/412/409, access401/403 MVP; dependency-unavailable status/machine codes Q15. Exception middleware не превращает ожидаемый incompatible move в permanent500. GET history допустим и после Resolved; no reopen/force action в OpenAPI. NSwag регенерируется только после подтверждения server contract, вручную generated methods не создаются.

## 6. EntityFramework

### 6.1. DbContext ownership

CatalogAccessDbContext/schema catalog_access владеет CatalogConflict, ProductVariant applied/observed/pending и их local transaction. Publishing остаётся отдельным DbContext. FK к Publishing, joins по foreign schema из F15 Persistence и shared BC transaction запрещены. F06 target допускает missing parent без fake Product; изменение current required parent FK принадлежит F06/Q23, не скрытая специальная FK bypass команда F15.

### 6.2. Configurations

Целевая CatalogConflictConfiguration: table catalog_conflicts, PK id Guid/typed converter, kind/entity_id/status, typed captured applied_state и pending_state в JSONB, owned captured issues/diagnostic as-of и resolved metadata, version:int concurrency. JSONB — representation, не stringly typed Domain. SourceVersion columns, если используются для indexes/projections, совпадают со значениями typed snapshots и не обновляются независимо. Mapping enum values, constraints names, actor representation, owned diagnostics columns/jsonb и pending handoff — Q20/Q23/Q15.

ProductVariantConfiguration хранит own Version отдельно source ordering. Pending conflict payload не сериализуется второй раз в другой independently mutable column Variant. Resolved record сохраняет applied/pending snapshots и outcome; закрытие не очищает объяснение history. DomainEvents не сериализуются как поля агрегата.

### 6.3. Constraints / indexes

- Partial unique (kind, entity_id) для Open гарантирует один episode при competing delivery/detection. Predicate учитывает выбранное enum storage, не фиксирует guessed SQL status literal.
- Queue/history index status + DetectedAt + stable Id; lookup EntityId/Kind. Выбранные физические names Q20.
- Required snapshots/identity/version equality/status-metadata rules защищены Domain и соответствующим mapping/check там, где SQL выражает условие. int Version concurrency token отдельный от source fields.
- ProductVariant и CatalogConflict — отдельные roots; их local changes/inbox enlistment координируются одним CatalogAccess UoW. Unique race не «лечится» created duplicate или удалением history.

## 7. EntityFramework.Persistence

### 7.1. Repositories / query adapters

CatalogConflictRepository реализует tracked GetAsync/ GetOpenAsync(kind, entityId)/Add, scope и specifications своего BC. ProductVariantRepository F06 загружает latest observed и own token. ConflictQueryHandler проецирует list/core DTO и internal Version; OperationBlockQueryHandler читает matching Open scope без foreign entities. Dependency data получает IProductVariantMoveImpactReader adapter, не прямой repository на каждый Publishing child.

Resolved history queryable по status/entity/kind; default Open применяет сервер. Read failure не возвращает пустую queue или IsBlocked=false. Root projection/read metadata должны относиться к одной версии; query не перезаписывает Issues/ResolvedAt и не запускает apply.

### 7.2. Transactions

Для MVP владелец этих commits — CatalogAccess; corrective repair принадлежит Publishing. По [утверждённому MVP consistency guard](../questions/Q20-persistence-and-migration.md#mvp-протокол-consistency-guard) все CM replicas используют одну PostgreSQL database и один стабильный глобальный exclusive transaction-level advisory key. Каждый affected owner scope явно начинает ReadCommitted transaction, затем отдельным statement вызывает pg_advisory_xact_lock на **той же physical connection и transaction**, до любых authoritative domain reads и row locks. После успешного acquire заново загружаются own roots/tokens и current foreign facts через public ports из независимых primary read scopes/connections. Предварительные query/preview/cache/replica facts не разрешают commit; foreign read не enlist-ится в ambient owner UoW, не разделяет SQL transaction и не открывает foreign EF из caller.

Guard удерживается до owner commit/rollback: изменяются только roots своего BC и атомарные own outbox/audit intent/inbox markers, где они нужны. Все invalidating writers, включая creation/removal/reference Copy/orphan paths, Publish/lifecycle/hard delete, setup/reconciliation, source durable acceptance/watermark/pending/apply/retry и F15 open/supersede/repair/resolve, участвуют в этом же ключе. Own Version/If-Match сохраняется: stale own token →412, изменившаяся foreign/preview basis →409 по Q15. Create/Copy из неизменённого source не увеличивает source Version; два serially guarded допустимых создания могут оба завершиться успешно.

Внешний I/O (broker, VR School, MinIO), ожидание пользователя и долгие streams выполняются вне gate. CPU preparation допустима заранее, но exact basis повторно подтверждается внутри gate; иной basis требует отказа или новой подготовки вне gate. Bounded guard wait/lock timeout конфигурируются, поддерживают cancellation и являются availability/technical failure, не domain409; точные значения и transport mapping остаются Q15/Q20. Connection loss прерывает owner transaction: reconnect-and-continue запрещён. Потеря commit ACK даёт unknown outcome; выяснение exact state/исходного operation ID по Q15 предшествует повтору, blind command retry запрещён.

F06 receiver acceptance earlier durable commit atomically stores receipt/latestaccepted+candidate/work and supersedes existing Open target with stale-assessment marker. Receiver ACK acceptance NOT apply. First Open detection may occur later in worker NEW first-gate transaction after rereading latestaccepted/fault/current claim+deadline/full dependencies: atomic single payload handoff/conflict/apply/history/work completion+own intents. Manual/auto apply whole Variant+Resolved cleanup/history atomic in its own stage; no receiver+worker shared transaction and no requirement receiver await business assessment.

Применение authoritative snapshot само по себе не требует Active lifecycle: Archived target сохраняется, а new-use/maintenance guards продолжают действовать у последующих операций. No transaction с Publishing/VR School. Первый commit repair меняет только Publishing working patch; второй отдельный Resolve commit в CatalogAccess после fresh source/dependencies атомарно applies full newest target и Resolved history. Conflict остаётся Open между commits. Неудачный Resolve не откатывает committed repair. Если после repair v21 пришёл v22, patch correction сохраняется, старое основание отклоняется и требуется новая оценка v22. Unavailable parent сохраняет durable waiting, без placeholder или применения старого target. Техническая availability ошибка/невалидное guard evidence → не commit successful resolution. Порядок acquire до fresh reads/validation, owner commit/rollback и release принят Q20/§7.2; implementation/DI/wire materialization остаются Q15/Q23. Repair и Resolve используют **две отдельные** transactions/acquires с освобождением gate между commits и UI review.

### 7.3. Migrations

После materialization pending layout и mappings при принятой coordination добавить table/index/concurrency, привести F06 source/local versions и nullable applied layout. Принятая Q20 возможность перестройки незапущенного CM не является командой применить destructive migration сейчас. В этом проходе меняются только документы; миграции и БД не запускаются. SQL fixtures тестируют unique Open, JSONB round trip и atomic outcome, не InMemory substitute.

## 8. External adapters

### 8.1. RabbitMQ / MassTransit

Вход — existing state-carrying Product/ProductVariant snapshots F06. Topology/Contracts/RabbitMq/Default adapters по [S22](../standards/S22-eventbus-topology.md); wire long/version/lifecycle mapping и current transport names Q23 не переименовываются по догадке. Business incompatible move заканчивается durable pending/conflict accepted outcome, не endless retry/DLQ. Technical port/storage failure остаётся техническим retry/operational failure по политике F06/Q23; Receiver ACK means durable acceptance receipt/candidate+work/fault only; worker apply/conflict complete separate NEW first-gate commit. Missing parent remains recoverable waiting, not receiver poison. Новый conflict-specific exchange/event не вводится без потребителя.

### 8.2. Publishing public-query adapter / Host

Adapter move impact ссылается на публичные immutable Application.Abstractions projections Publishing, проверяет полноту/evidence и выдаёт типизированный результат или failure. Operation guard предоставляет CatalogAccess read boundary всем affected F07–F12/F17 writers; MVP coordinator реализует принятый Q20 advisory protocol; exact port/DI implementation Q15/Q23, no foreign EF/Domain mutation. Host регистрирует CatalogAccess.Application handlers, Persistence repos/queries, public BC adapters и существующие clock/context/transport infrastructure. F15 не использует S3/ProjectStorage HTTP и не вызывает source mutation VR School от имени специалиста.

## 9. Data flows

MVP порядок для DF01–DF02, DF05, caller commit DF06 и отдельный repair commit DF07: input/CPU preparation вне gate → явная owner ReadCommitted transaction → standalone exclusive advisory acquire → fresh own roots и foreign primary-port facts → проверки own token/basis/lifecycle/dependencies → Domain action → только own writes/atomic intents → commit и release. Это обязательный префикс каждого указанного mutation/retry stage; строки «load/guard» ниже означают загрузку **после** acquire. Read-only query/impact/preview остаются observations; при необходимости короткий source capture имеет отдельный guarded scope, но долгий stream и пользовательский review не удерживают guard. Demo использует свой согласованный контур без новых обязательных MVP dependencies.

По [S17](../standards/S17-data-flows.md) внешний поток и внутренний алгоритм описаны отдельно. DF07 задаёт согласованное корректирующее действие, чей точный transport/CLR остаётся Q15/Q23, а MVP guard protocol принят Q20; описание не доказывает готовый endpoint.

| Поток | Транспорт | Метод / канал | Endpoint / destination | Доступ | Вход | Application action | Выход | Успех | Ошибки |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| DF01. Detect move conflict | RabbitMQ → internal F06 | Source snapshot consumer | F06 CatalogAccess apply | MVP trusted source transport | Full Variant snapshot/EventId | F06 evaluate + OpenOrUpdateCatalogConflict | Applied либо durable pending/Open | Local commit + accepted message outcome | Malformed/version mismatch, parent pending, technical failure |
| DF02. Supersede / safe auto-close | RabbitMQ / durable F06 resolver | Newer snapshot / current candidate | CatalogAccess apply | MVP internal | Newest full source + existing Open | ReplacePending / ResolveApplied | New target или applied/Resolved | Atomic newest state/history | Stale no-op, blockers, missing parent, guard unavailable |
| DF03. Queue/history | HTTP | GET | /api/v1/catalog-conflicts | F16/Q10 specialist | Filters/page/sort | ListCatalogConflictsQuery | List DTO | 200 confirmed result | Invalid filters,401/403,technical read error |
| DF04. Exact conflict / impact | HTTP + read adapter | GET / additional read TBD | /api/v1/catalog-conflicts/{id}; impact exposure Q15 | F16/Q10 | ConflictId | GetCatalogConflictQuery / ImpactQuery | Core DTO+ETag; separate live impact | 200 own state; read-only | 404,401/403,dependency unavailable |
| DF05. Manual resolve | HTTP | POST | /api/v1/catalog-conflicts/{id}/resolve | Authorized specialist F16/Q10 | ID + If-Match, no body | ResolveCatalogConflictCommand | Confirmed DTO+newETag | 200 after local apply/close commit | 428,412,409 blockers/changed basis,404,403,technical failure |
| DF06. Guard affected operation | Same-process public query | Operation block evaluation | ICatalogOperationBlockReader | Trusted BC caller; business access у caller | Product/Variant + operation | GetCatalogOperationBlockQuery | Decision + diagnostic/evidence | Confirmed scope decision | Unavailable cannot equal unblocked; approved coordination Q20, exact port Q23 |
| DF07. Local repair boundary | Publishing command; transport Уточняется | Scoped corrective action; method Q15 | Exact route Q15 | Authorized specialist F16/Q10; audit F18 | ConflictId + newest SourceVersion + captured repair basis + correction + patch If-Match | RepairVariantWorkingPatchCommand (предложенное имя) | Confirmed saved patch; separate DF05 required | Publishing commit only | Stale source/patch/basis, invalid/expanding delta, access, remaining blockers; no Save bypass |
| DF08. Repair preview | Read boundary Уточняется | Query; exposure Q15 | Exact route Q15 | Authorized specialist F16/Q10 | ConflictId + newest target + proposed correction | GetCatalogRepairPreviewQuery (предложенное имя) | Applied/prospective/proposed-repair comparison + captured basis/diagnostics | Confirmed read only | Stale source/invalid proposal/technical dependency failure |

### DF01. Detect move conflict

1. F06 receiver durably accepts under FIRST own gate before ACK. Later worker NEW first-gate transaction rereads latestaccepted/single payload owner/scoped coveringfault/current claim token+deadline/Variant/Open; accepted22 rejects work21 before22 classified. First conflict detection occurs here, not synchronous acceptance.
2. При missing parent сохраняется F06 durable pending без placeholder. Для existing applied Variant и нового parent Application читает complete impact через public adapter; source перенос сам по себе не blocker.
3. Compatible facts + coordinated guards позволяют apply whole snapshot без F15. Confirmed incompatibility вызывает Open либо ReplacePending existing Open; AppliedState Variant не меняется, snapshots/diagnostics типизированы.
4. Worker Persistence commits own roots/single pending owner/conflict/history/workcompletion+necessary intent atomically; earlier receiver receipt/watermark remains durable independently. Unique Open/concurrency race обрабатывает fresh state и latest guard; не создаётся второй episode. Technical failure не подтверждает бизнесовый outcome.
5. После commit source message считается обработанным согласно F06/inbox contract. Historical Publishing/Delivery не изменяются; existing confirmed entitlement/new session сохраняются с независимыми guards; affected новые writers используют DF06. Гарантия их гонок требует участия всех writers в принятом MVP global advisory key Q20, не факта наличия row.

### DF02. Supersede / safe auto-close

1. Receiver existingOpen supersession and worker assessment each own new first-gate scope; fresh latestaccepted/fault/current target. v20 does not replace21; equality/fault profile F06 handles immutable repeat/state contradiction, no fresh evidence from old Issues.
2. Загружаются Open/root и новый full candidate; весь PendingState/версия/диагностическая основа заменяются вместе, DetectedAt/original applied basis сохраняются. Специалист работает с newest, не обязан resolving промежуточные snapshots.
3. Re-evaluate parent/lifecycle/local dependencies и coordinated evidence. Если confirmed compatible — ApplyObservedSnapshot + ResolveApplied(SourceChangedApply) в одной local transaction. Предыдущий клиентский ETag устаревает.
4. При remaining confirmed blockers Open сохраняется с newest target. При dependency failure нельзя объявить target compatible. Missing parent нового target при уже Open не разрешает применить старый pending; единый storage/handoff и отображение waiting vs conflict уточняются Q23/Q25.
5. Resolver существующего candidate может применять сохранённую версию == watermark после устранения препятствия; это не новый stale source input. Перед commit ещё раз защищается candidate identity/version, включая durably accepted newer candidate, ещё ожидающий impact/parent processing (receipt/processing boundary Q23). Pending v21 не применяется поверх accepted v22.

### DF03. Queue/history

1. Api binds allowed filters/cancellation, Application.Abstractions query передаётся Persistence.
2. Specification server-side применяет default Open либо явный status/history, entity/kind/search и whitelist sort; stable Id tie-breaker, count/page profile Q15.
3. Projection возвращает summary snapshots/diagnostic label, без tracked graph/Version/per-row ETag. Empty confirmed queue отличается от read failure/denied.
4. Никаких records/status/diagnostics writes, apply или broker side effects при чтении. Resolved rows остаются queryable.

### DF04. Exact conflict / impact

1. Core query читает exact own root projection и internal Version согласованно; missing →404. Snapshot DTO содержит captured applied/newest pending, as-of issues и resolution metadata, а не live Publishing graph.
2. Api формирует own strong ETag из internal int Version; техническое поле не попадает в DTO. History после Resolved читается, но не переоткрывается.
3. Если клиенту нужна свежая оценка, отдельный logical impact read получает current own basis и public Publishing facts; выдаёт typed result/failure отдельно от root ETag. Repair comparison использует DF08. Exposure/wire profile Q15.
4. Ни core GET, ни live impact query не сохраняют refreshed Issues, не increment Version и не закрывают conflict. Прочитанные facts не обещаются неизменными до последующего POST.

### DF05. Manual resolve

1. Api требует valid If-Match, bind ConflictId/cancellation и trusted context. Клиент не присылает ProductId или local corrected payload.
2. Handler начинает явную CatalogAccess ReadCommitted transaction, standalone acquires global advisory key на owner connection до любых facts/row locks, затем fresh загружает tracked conflict; missing404, own stale412, current non-Open предметный отказ по Q15. Загружает tracked Variant/watermark и authoritative pending owner, убеждается в same ID/newest candidate.
3. После отдельного committed repair либо явного устранения других blockers читает заново newest source, parent readiness/source lifecycle и complete current move impact. Remaining blockers →409 с актуальной диагностикой; technical unavailable не409 «пустой impact» и не successful apply. Ordinary blocked writes не выполняются внутри resolve.
4. Уже удерживаемый global transaction advisory key защищает fact basis/newest target от source acceptance/open/supersede/authoring writers. Fresh complete Publishing facts шаг3 читаются через independent primary read scopes без foreign enlistment. Accepted durable v22, даже ещё не processed, делает apply v21 недопустимым; changed basis →409, unknown/unavailable не превращается в valid impact.
5. Domain ApplyObservedSnapshot переносит full target по F06, ResolveApplied фиксирует actor/time/outcome. Persistence коммитит обе own roots и pending cleanup одной CatalogAccess transaction; history immutable, unrelated/historical state не переписывается.
6. Только после подтверждения UoW возвращает confirmed core DTO/new ETag, без скрытого GET latest для построения иного результата. На race/rollback fresh state классифицирует own412 vs changed external basis409/technical failure. Нет automatic retry на новый target от имени пользователя.
7. При timeout клиент перечитывает exact/history и выясняет outcome; повтор со старым ETag не применяется как новая команда с обновлённым основанием. Семантика repeated already-Resolved ответа/diagnostic machine code Q15.

### DF06. Guard affected operation

1. Calling F07–F12/F17 handler проверяет access/input, начинает **свой** explicit ReadCommitted owner transaction и standalone acquires global advisory key до own roots/row locks. Затем fresh проверяет own ETag и формирует actual planned operation scope, включая current applied context.
2. Public CatalogAccess query в независимом primary read scope без owner ambient enlistment читает matching Open conflicts, newest accepted source и agreed affected closure; возвращает confirmed decision/IDs либо failure. UI disable не заменяет этот server guard; no HTTP self-call.
3. Blocked → caller domain refusal с conflict reference; unrelated scope продолжает работу. Read/history/Delivery не превращаются в запрещённые operations только из-за Open.
4. Unblocked result является observation: caller удерживает свой global advisory key до своего owner commit/rollback, атомарно пишет только собственные roots и audit/outbox intents. Обычный once-read guard без fence не закрывает race с новым conflict. Withdrawal/revoke processing и historical safety не блокируются blanket CatalogConflict flag.

### DF07. Local repair boundary

1. Authorized specialist получает current Open conflict, newest full pending и три preview основы DF08. Source correction выполняется отдельно в VR School и поступает DF02; parent нельзя выбрать в CM.
2. Publishing.Api binds exact working patch + required If-Match → internal expected int patch Version. Command содержит ConflictId, PendingSourceVersion, CapturedRepairBasis и proposed correction; exact route/CLR/wire Уточняется (Q15). F16/Q10 checks specialist permission; trusted actor/correlation для F18.
3. Publishing.Application handler начинает свою explicit ReadCommitted transaction и standalone acquires global advisory key до reads/row locks; fresh загружает tracked saved patch, проверяет own token и через CatalogAccess public ports exact Open/Variant/newest target. Parent берётся исключительно из pending. Проверяет captured prospective dependency basis через public boundaries; concurrent source/patch/basis change отвергает proposal, без automatic retry на новый target.
4. Classifies delta только как удаление conflicting rule/local addition, eligible replacement уже проблемного result или inherited exclusion для удаления blocker. Intrinsic F08 validation, eligibility новых references, no-expansion/Archive guards и affected scope обязательны. Нельзя ordinary Save с ignoreConflict, произвольные content/policy edits, releases/revisions/activation или rewrite history. Other blockers остаются для отдельного explicit action/source correction.
5. Удерживает global advisory key своей Publishing transaction от acquire шага3 до commit/rollback, сохраняя basis; foreign facts прочитаны через independent primary ports. Publishing.Domain применяет correction, own Persistence коммитит только working patch + owning audit fact F18; foreign DbContext/SQL transaction не используются. Confirmed result после commit; CatalogConflict остаётся Open, publication не создаётся.
6. Специалист отдельно вызывает DF05. Resolve заново читает newest source и dependencies; при compatible state CatalogAccess атомарно применяет full snapshot и закрывает history. Если v22 пришёл после repair v21, committed correction сохраняется, old basis отвергается, нужен fresh preview/recheck. Если parent отсутствует, durable waiting сохраняется; success Resolve не объявляется.

### DF08. Repair preview

1. Read adapter читает exact Open/newest pending и saved Publishing patch через публичные query ports. Normal workspace остаётся на applied parent.
2. Получает applied base и prospective base только source pending parent; отдельно применяет proposed correction pure composer F08. Сравнивает applied, prospective и proposed-repair, concrete blockers до/после, scope и captured repair basis.
3. Missing A target оставляет local B; independent prospective B плюс A → B диагностирует duplicate B. Immutable artifacts не пересчитываются и не включаются в blockers лишь из-за наличия.
4. Возвращает read-only comparison/diagnostics либо failure; не сохраняет patch/Issues, не закрывает conflict и не увеличивает own Version. Unavailable basis не становится empty blockers. Exact CLR/wire/exposure Q15; прочитанный basis не является permit без command guard до commit.

F06 reliably scoped CatalogIngestionFault is independent unreadiness: Resolve/repair/new-use authoritative ports consult it plus latestaccepted before own commit, even if older valid pending/Issues exist. No F15 episode/Withdrawal/global catalogue block for ingestion fault; invalididentity no fabricated scope. Fresh parent/dependencies can auto SourceChangedApply+Resolved only for current target and no active higher fault. Old Issues bound to old version remain history, never new-target proof.

## 10. Ошибки и диагностика

MVP guarded failure различает confirmed business refusal и невозможность проверки: unavailable/incomplete public facts не означают empty/valid facts. Guard wait cancellation/timeout и connection loss — technical/availability outcome без domain Conflict; точные wire codes остаются Q15. После connection loss scope завершается rollback/disposal без продолжения на новой connection. Unknown commit outcome выясняется по exact state/operation ID; prepared tracker не подтверждает ни success, ни rollback. Own stale ETag сохраняет412, foreign basis change —409. Диагностика фиксирует wait/hold duration, owner/operation, acquire/commit/rollback/unknown phase без full payload и секретов.

| Ситуация | Результат / сохранность данных |
| --- | --- |
| Confirmed incompatible source move | Durable Open/newest pending, applied state прежний; сообщение accepted после local commit |
| Equal/stale valid source snapshot | Не заменяет newest target; не создаёт очередной conflict; inbox semantics F06 |
| Invalid/mismatched source envelope/identity | Input/transport rejection F06; не нормальный catalog conflict |
| Missing parent | Durable F06 waiting, без fake Product и применения старого pending; existing Open handoff Q23/Q25 |
| Source/dependency port/storage unavailable | Technical failure/ожидание; не safe move, empty blockers или false unblocked |
| Exact conflict отсутствует | 404; другой episode не подставляется |
| Missing required If-Match / own stale | 428 / 412; resolve не коммитит |
| Current own token, но target/impact несовместим либо independent basis изменился | 409 по предметному контракту; newest state/история не теряются |
| Already Resolved | Нет повторного apply/reopen; exact response profile Q15; UI выясняет исход через read |
| Concurrent Open unique/local Version race | Fresh scope/reload и classification; максимум один Open, no unsaved response |
| Repair stale source/patch/captured basis | Proposal отвергнут без корректировки нового target; committed прежний repair не откатывается из-за v22; own412 vs independent409 profile Q15 |
| Repair расширяет scope/состав либо invalid/new-use-ineligible reference | Domain отказ; ordinary Save остаётся blocked; no partial patch/publication |
| Denied MVP access | 401/403 по F16; не empty queue и не локальный bypass |

Machine codes, structured field/global issues, localized diagnostics, dependency-unavailable HTTP mapping и trace retention — Q15/Q25. Логировать correlation/EventId/conflict/Variant/applied+pending parent IDs и source versions, operation category/observed basis, outcome. Не писать activation key, session credential, license payload или полный произвольный контент. Current unchecked exception move — gap, не целевой error contract.

## 11. Тестирование

По [S04](../standards/S04-testing.md), [S14](../standards/S14-test-environments.md) и category standards проверка отделяет pure invariant, реальное persistence, HTTP и inter-BC coordination. SUT и данные принадлежат сценарию, не общему заранее seeded внешнему проекту.

| ID / поток | Уровень / SUT | Свои данные / воздействие | Ожидаемый результат |
| --- | --- | --- | --- |
| T01 / DF01 | Domain + Application Unit | Applied v18/A, incompatible v21/B; complete impact stub | Stable Variant/local links; один Open/full pending; captured basis прежний |
| T02 / DF01–02 | Unit | v21 pending, затем equal/stale v20/C и newer v22/D | v20/equal не затирают; v22 заменяет целиком; DetectedAt прежний |
| T03 / DF02 | Unit + Integration | Новый source исправляет relation; current facts подтверждены compatible | Atomic whole apply + Resolved(SourceChangedApply); no historical rewrite |
| T04 / DF01–02 | Integration, CatalogAccess + PostgreSQL | Competing detection/open, own Versions/unique index, typed snapshots/JSONB | Максимум один Open; no split watermark/payload; retry rechecks newest |
| T05 / DF05 | HTTP Functional | Exact saved Open, ETag, request без header/stale header/current header | 428/412 либо confirmed200/new ETag по состоянию; Version отсутствует в DTO |
| T06 / DF05 | Functional/Integration | v21 UI basis; v22 arrival либо auto-close before/during resolve | v21 не применяется; own412/independent409 по basis, history/newest retained |
| T07 / DF05 | Integration, real CatalogAccess UoW | Fault before/at commit после prepared apply/close | ProductVariant/conflict/cleanup либо все durable, либо rollback; no success prepared DTO |
| T08 / DF03–04 | Functional + Persistence | Open+Resolved history, captured issues; foreign impact changes | Default Open; explicit history; core own ETag корректен, live impact не маркируется тем же token; GET no writes |
| T09 / DF06 | Functional, real affected handlers | Matching conflict и unrelated Variant/Product operations | Только agreed affected writes blocked; read/resolve/historical Delivery разрешены |
| T10 / DF05–06 | Integration, real Publishing/CatalogAccess coordination | Барьер: impact прочитан, competing assignment/policy/new conflict до commit | Небезопасный apply/write не коммитит; принятый global advisory protocol Q20, no mock fence proof |
| T11 / DF01–06 | Unit + Integration/Functional | Parent/impact unavailable, частичная projection, malformed source | Не safe move и не false unblocked; technical recovery/inbox rules distinct |
| T12 / DF05–06 | Functional | Historical materialized release/revision/session при Open | Ни repin, ни revoke/withdrawal от conflict; отдельные invalidators продолжают действовать |
| T13 / DF07–08 | Functional + Integration, Publishing repair и CatalogAccess resolve | Remove rule/local addition, eligible problematic replace, inherited exclusion; ordinary Save/ignoreConflict/parent rewrite/expansion | Допустимые corrections фиксируются только Publishing; separate Resolve applies+history; forbidden действия отклонены; audit/permission F18/F16 |
| T14 / layers | Architecture | Assemblies CatalogAccess/Application/Persistence/adapters/Publishing | No foreign DbContext/Domain mutation/cross-BC UoW; commands Application, queries data access |
| T15 / DF01,07–08 | Unit + Integration, prospective composer | Missing target A при local B; separately prospective inherited B и A → B | Первый случай допустим; второй concrete duplicate blocker; historical immutable alone не blocker |
| T16 / DF07,05 | Integration, two own BC commits | Repair v21 committed, then v22 arrives либо parent отсутствует до Resolve | Correction сохраняется; old basis rejected; fresh evaluation/newest only, durable parent waiting, no foreign transaction |
| T17 / DF06–08 | Functional + Integration, authoritative closure и permission/audit | Shared Product dependencies + unrelated scope, specialist permission without admin role, archive+conflict maintenance | Только authoritative affected operations blocked; scoped repair/no expansion; F18 committed audit; admin-only restriction отсутствует |
| V201 / DF01–02,05–06 | PostgreSQL receiver/worker barriers | Accepted22 before worker21, higher ingestionfault, same Open new target before assessment | No stale21 Resolve/apply; old Issues not current, affected fault NotReady independent F15 |
| V202 / DF02,05 | PostgreSQL own commit | Missing parent for newer Open target then parent arrives/compatible current correction | SameOpen preserved/waiting, autoSourceChangedApply+Resolved full apply atomic; no publish/history rewrite |
| T18 / DF06 | Integration, activation/promotion/late approval | Operation started on prior basis, Open/source/lifecycle changes before continuation | Rechecked scope/basis denies unsafe continuation; future precedence choice не подменяется successful activation |

Unit использует own typed IDs/full snapshots и deterministic clock; mocks допустимы для complete/incomplete impact/error, но не подтверждают PostgreSQL constraints или inter-BC safety. Integration использует PostgreSQL/Testcontainers, реальные mappings/own contexts/UoW и isolated data с cleanup fixture; concurrency tests синхронизируют реальные competing commits. Broker delivery/idempotency ACK test в F06 использует реальный RabbitMQ/Testcontainers, если проверяется именно transport outcome, а не прямой handler call.

Functional поднимает API + real PostgreSQL; access при необходимости actual staff-cookie fixture F16. External impact/source ports doubles обозначаются явно. Несколько BC того же API не становятся E2E. Сквозной VRS source move → CM conflict → UI/manual accepted workflow → unchanged historical SDK delivery требует реальных необходимых приложений и контракта F06/F13, не цепочки mocks. Repair scope и lifecycle guards проверяются по согласованным §2.3/DF07. Неизвестные exact wire/port/mapping детали не выбираются в expected result молча; безопасность принятого MVP guard подтверждается real PostgreSQL с реальными invalidating writers.

В данном проходе выполнены source reads и документальные проверки. API/VR School/broker/UI/SDK не запускались; dotnet test и применение миграций не выполнялись.

MVP Integration matrix для DF01–DF02, DF05, caller commit DF06 и отдельный repair commit DF07 использует real PostgreSQL, отдельные owner connections/UoWs и barriers, включая разные CM replica processes/hosts там, где проверяется общность key. Doubles/in-memory locks не доказывают SQL coordination.

| MVP сценарий | SUT / воздействие | Наблюдаемый результат |
| --- | --- | --- |
| Guard до чтений | Реальный Persistence adapter; writer A держит gate, B начинает owner transaction до acquire; A меняет eligibility/basis и commit, B продолжает | B после acquire читает новый committed state при ReadCommitted; stale предварительные facts не применяются; lock statement использует owner connection/transaction |
| Меж-BC write/read partition | Public read adapter и Host/DI; owner держит guard, foreign primary read отдельным scope | Read не enlist-ится в owner transaction, не читает cache/replica; no foreign writes/EF access; atomic commit только owner + intents |
| Commit/rollback/release | Barriers для invalidating writer и affected operation; injected owner failure/cancellation | Второй writer ждёт release и revalidates; failed owner полностью rollback, нет partial roots/intents; unrelated next scope получает key |
| Wait и connection failure | Configured bounded wait/cancellation, разрыв owner physical connection | Technical/unavailable, не409/empty success; aborted transaction не продолжается через reconnect |
| Unknown commit ACK | Разрыв response/commit acknowledgement и inquiry exact state/original operation ID | Committed result сохраняется, rollback не предполагается; no blind duplicate mutation |
| Repair v21 → accepted v22 → Resolve | Реальные Publishing/CatalogAccess writers, barriers между двумя commits, включая v22 acceptance до processing | Repair committed и сохраняется; старый Resolve basis rejected409, fresh newest обязателен; каждая transaction отдельно acquires/releases key, Open/history atomic |

## 12. Порядок реализации

1. Materialize Q15 exact repair/preview contracts, selected Q23 single pending ownership/handoff и Q20 owner/read-scope DI при принятом global advisory protocol и scope/impact/repair правилах §2.3. Проверить реальные competing writers до заявления о race-safe рабочем resolution.
2. Domain: CatalogConflict/typed issues/constraints/transitions, reuse F06 snapshots; сохранить stable identity и terminal history.
3. Application.Abstractions: commands/queries/ports/core vs live read contracts; закрепить external DTO/status/ETag profile Q15 без technical Version.
4. EntityFramework/Persistence: single authoritative pending layout, unique Open/own tokens/local atomic roots + inbox, typed projection/specifications. Изменить F06 source guard, а не добавить retry существующего hard exception.
5. Application/adapters/Host: detection/supersede/resolver/manual resolve и participation всех affected writers; no foreign EF/cross-BC transaction. Подключить правильную transport classification F06.
6. Api: queue/history/core read/resolve, отдельные Publishing repair/preview contracts, access/ETag/errors/OpenAPI/F18 audit; затем generated web client и frontend screens, live impact exposure по Q15.
7. Проверить functional persistence/concurrency/rollback и agreed repair пути. Отдельно проверить historical Delivery и real application E2E границу, когда необходимый контур доступен.

## 13. Открытые технические вопросы

| Вопрос | Что блокирует |
| --- | --- |
| [Q25](../../../business/questions/Q25-catalog-conflicts.md#f15-технические-границы-разрешения) | Concrete blockers/closure/scoped repair и source correction приняты; continuation rechecks current scope/basis и F11 head/ProductVersion/VariantSequence, no resurrection Cancelled/Superseded; exact carriers engineering Q15/Q20 |
| [Q23](../questions/Q23-source-synchronization.md#f15-newest-target-и-guards) | Selected F06 single pending owner/handoff/receiver+worker/fault/assessment semantics; only physical adapter/DTO codecs engineering, invalidating writers mandatory |
| [Q20](../questions/Q20-persistence-and-migration.md#f15-local-atomicity-и-writer-coordination) | MVP global advisory protocol принят; physical mapping/index/concurrency, own inbox enlistment и owner/read-scope DI materialization без общей меж-BC SQL transaction |
| [Q15](../questions/Q15-api-and-source-code.md#f15-conflict-api-и-read-contracts) | Core DTO/ETag vs live impact/three-basis preview, exact repair request/result/CLR/routes, public nested shapes/actors, list filters/paging, errors/repeated outcome/security profile |
| [Q10](../../../business/questions/Q10-permissions-and-isolation.md) | Доступ в существующей модели специалистов; не выдуманный admin-only resolution |

Принятые source authority/newest/history/blocked operations правила реализуются у владельцев и не становятся неопределёнными только потому, что код отстаёт. Открыты конкретные механизмы, не сам запрет source-of-truth обхода.
