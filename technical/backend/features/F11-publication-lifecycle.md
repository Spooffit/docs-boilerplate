# F11. Publication state, activation и automatic promotion: бэкенд

[Бизнес](../../../business/features/F11-publication-lifecycle.md) · [M04](../modules/M04-publishing.md) · [Фронтенд](../../frontend/features/F11-publication-lifecycle.md) · [Q06](../../../business/questions/Q06-automatic-promotion.md) · [Q28](../../../business/questions/Q28-content-review-scope.md) · [Q18](../questions/Q18-activation-history.md) · [Q15](../questions/Q15-api-and-source-code.md#f11-publication-state-и-automatic-promotion) · [Q20](../questions/Q20-persistence-and-migration.md#f11-state-transitions-и-promotion-coordination) · [Q17](../questions/Q17-publication-availability.md#f11-publishing-active-и-delivery-effective).

## 1. Область реализации

F11 — MVP Publishing: выбрать текущую Product base из F09, вручную Activate/Rollback/Unpublish Variant, хранить append-only activation history и выполнить Automatic promotion независимо по Variant через F10 → F17 → activation → async Delivery. Demo не требует full Publishing, Identity, moderation или broker.

Product ActiveReleaseId — база подготовки/promotion, не клиентская revision. Variant ActiveRevisionId — подтверждённое Publishing намерение; Delivery effective revision меняется после полного consumer/projection commit. Пока новая проекция не готова, предыдущая effective revision остаётся доступной согласно F12/F13. HTTP success activation не доказывает client visibility.

F09 создаёт immutable Product release из saved assignment и Variant release из patch с explicit exact ProductRelease/EmptyBase validation context; F10 создаёт immutable materialized revision без Approval/activation. F17 владеет exact revision moderation; F11 разрешает activation только Approved и допустимой revision. Withdrawal lifecycle принадлежит F10: active Withdrawal атомарно очищает только соответствующий pointer/history/outbox внутри Publishing; exact-ID terminal distribution ban нужен и для inactive/frozen revision. Unpublish не Withdrawal, не отменяет frozen entitlement и не создаёт fallback. Retention не удаляет history.

Физические слои Publishing.Domain/Application.Abstractions/Application/Api/EntityFramework и целевой EntityFramework.Persistence. Сейчас [Publication states](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Domain/PublicationStates/ProductVariantPublicationState.cs) используют long Revision/LastManualRevision; [PublicationHistory](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Domain/Publications/PublicationHistory.cs) — простые activation/job records. [App ProcessPromotionAsync/Activate](../../../../contentmanager.api/src/App/ContentManager.Application/Publishing/PublishingService.Releases.cs) требуют существующую active revision, создают новую и сразу активируют без F17, напрямую пишут DeliveryDbContext. [Worker](../../../../contentmanager.api/src/App/ContentManager.Application/Publishing/PublicationPromotionWorker.cs) возобновляет Pending jobs; это не доказательство target operation/moderation/outbox. Типы ниже — целевая спецификация; расхождения и незакрытые протоколы Q15/Q20.

Own/parent catalog Archive запрещает Activate/Rollback и Automatic promotion по [F06](F06-product-catalog.md); Open conflict блокирует только authoritative affected closure [F15](F15-catalog-conflicts.md). Activation/promotion и продолжение после late approval повторно проверяют current affected scope/basis до commit; прежняя successful precheck/Approval не permit, head/ProductVersion/VariantSequence fences заданы §2.3. Explicit safety Unpublish разрешён с обычными permissions/preconditions. Archive/Conflict/Restore/Resolve не очищают active pointers и не запускают automatic activation/promotion; existing materialized Delivery/confirmed entitlement, включая new sessions, сохраняются с independent F12/F13 guards.

## 2. Domain

### 2.1. Aggregates, entities и value objects

| Тип | Роль / значения enum | Владелец / граница |
| --- | --- | --- |
| ProductPublicationState | Aggregate root | Один current base на stable Product; null допустим; не assignment Version |
| ProductVariantPublicationState | Aggregate root | Один current pointer на stable Variant; sequence != Version |
| ActivationRecord | Immutable history entity | Append-only Publishing; server sequence/time/context |
| ActivationReason | Enum: Manual, AutomaticPromotion, Rollback, Unpublish, Withdrawal | Server action определяет reason, не arbitrary client string |
| PromotionMode | Enum: Manual, Automatic | Выбор конкретного promotion, не постоянная настройка Product |
| ProductReleasePromotionOperation | Immutable operation root, Automatic only | Frozen inputs/membership/initiator/ClientOperationId; progress derived from independently mutable items |
| PromotionItem | Отдельный mutable concurrency owner | Own ItemId/Version, frozen inputs/basis и exact result tuple; independent item commits |
| VariantPromotionIntent | Внутренний mutable Publishing root | VariantId → current automatic ItemId; own Version; supersession без activation history |
| PublishingCommandExecution | Immutable confirmed receipt | Scoped ClientOperationId + actor + normalized original input → original confirmed result |
| PromotionItemStatus | Pending, AwaitingModeration, Activated, Blocked, Failed, Rejected, Cancelled, Superseded | Activated = Publishing commit, не Delivery effective; Cancelled/Superseded terminal для automatic continuation |
| PublicationTransitionParameters | Domain input | Не HTTP DTO; не client Approved flag |
| ActivationTransitionResult | Domain result | No-op не increment Version/sequence и не создаёт record/event |

**ProductPublicationState**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | Уточняется | Да | State identity (strong Guid ID) | Own root |
| ProductId | Уточняется | Да | Stable Product owner | Один current base на Product |
| ActiveReleaseId | `ProductContentReleaseId?` | Нет | Current Product base | null допустим; не клиентская revision |
| Version | `int` | Да | Own state concurrency | Internal mutable token; не assignment Version/DTO |

**ProductVariantPublicationState**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | Уточняется | Да | State identity (strong Guid ID) | Own root |
| ProductVariantId | Уточняется | Да | Stable Variant owner | Один current pointer на Variant |
| ActiveRevisionId | `ProductVariantPublicationRevisionId?` | Нет | Publishing intended pointer | Nullable; не Delivery effective |
| LastSequence | `long` | Да | Предметный порядок | Не Version; неотрицательный; initial/overflow Q20 |
| Version | `int` | Да | Own state concurrency | Internal mutable token; не DTO/sequence |

**ActivationRecord**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | `Guid` | Да | History identity | Server-generated append-only |
| ProductVariantId | Уточняется | Да | Stable Variant scope | History survives source move |
| Sequence | `long` | Да | Transition order | Positive; monotonic per Variant; unique Variant+Sequence |
| RevisionId | Уточняется (nullable) | Нет | Exact target or null | Null transition semantics; immutable |
| ActivatedAt | `DateTimeOffset` | Да | Transition timestamp | Trusted server time; timestamp не ordering |
| Reason | `ActivationReason` | Да | Server action | Manual/AutomaticPromotion/Rollback/Unpublish/Withdrawal |
| ActorId | `Guid?` | Нет | Trusted actor | Audit visibility profile Q15 |
| CorrelationId | `Guid?` | Нет | Trusted correlation | Correlation profile Q20 |

**ProductReleasePromotionOperation**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | `Guid` | Да | Server OperationId | Отдельный от ClientOperationId и ItemId |
| ProductId | `Guid` | Да | Captured applied Product | Immutable scope |
| ProductContentReleaseId | `ProductContentReleaseId` | Да | Exact selected base | Immutable; no latest substitution |
| CapturedProductStateVersion | `int` | Да | Resulting Product state generation внутри той же start transaction | Captured with own start writes/receipt до commit; no hidden post-commit read; ABA A→B→A |
| ClientOperationId | `Guid` | Да | Original client logical execution | Required/non-empty; shared receipt §2.1 |
| InitiatorId | `Guid` | Да | Trusted staff инициатор | Server execution context, не request actor |
| CreatedAt | `DateTimeOffset` | Да | Trusted start time | Immutable |
| ItemIds | `IReadOnlyList<Guid>` | Да | Frozen membership | Unique Variant; zero items allowed |
| RetryOf | `Guid?` | Нет | Trace к previous operation | Domain retry — новая operation с новым client ID/subset confirmation |

Operation inputs/membership immutable. Status/CompletedAt/counts — derived read summary из items, не собственный общий mutable progress token и не источник item If-Match.

**PromotionItem**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | `Guid` | Да | Server ItemId | Stable own identity |
| OperationId | `Guid` | Да | Immutable operation owner | Unique OperationId+ProductVariantId |
| ProductVariantId | `Guid` | Да | Captured applied member | Later/new/moved members не добавляются |
| ProductContentReleaseId | `ProductContentReleaseId` | Да | Frozen Product source | Из operation |
| ProductVariantContentReleaseId | `ProductVariantContentReleaseId?` | Нет | Frozen latest-created local release | Retired latest → Blocked; no older fallback; null только never-released |
| CapturedProductStateVersion | `int` | Да | Frozen Product state fence | Должен оставаться current при activation |
| CapturedVariantSequence | `long` | Да | Variant LastSequence на start capture | ABA fence R1→R2→R1 |
| Version | `int` | Да | Own item optimistic token | Progress одного item не увеличивает tokens других |
| Status | `PromotionItemStatus` | Да | Durable own stage/outcome | Cancelled/Superseded не autoresume |
| PublicationRevisionId | `ProductVariantPublicationRevisionId?` | Условно | Exact own prepared artifact | Required после successful prepare; no adoption/hash matching |
| ModerationId | `Guid?` | Условно | Exact own automatic Pending iteration | Bound atomically при prepare; different iteration не reattach |
| FailureCode | `string?` | Условно | Safe stage diagnostic | Public codes/limits Q15 |
| FailureMessage | `string?` | Условно | Safe diagnostic | Privacy/limits Q15 |

**VariantPromotionIntent**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductVariantId | `Guid` | Да | Stable Variant root identity | One intent per Variant |
| CurrentAutomaticItemId | `Guid?` | Нет | Current automatic head | New accepted operation supersedes older unfinished head, even same base |
| Version | `int` | Да | Own intent concurrency | Atomic with head/item transition; internal, no fake activation history |

Shared confirmed [CommandExecution model](../questions/Q20-persistence-and-migration.md#publishing-workflow-capture-и-продолжение) определяется только Q20; F09/F10/F11/F17 используют его scope, actor, normalized input и original receipt. Authorization precedes gate; lookup confirmed scoped ClientOperationId под gate предшествует новым execution preconditions. Same ID/input returns original receipt even source changed/artifact Retired; changed bound input →409. Failure before commit creates no confirmed success binding. Replay не Restore/Submit/Activate и не новый audit/history. Transport carrier/normalization/error exposure — [Q15](../questions/Q15-api-and-source-code.md#publishing-command-execution-и-clientoperationid).

**PublicationTransitionParameters**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Target eligibility | Уточняется | Да | Validated exact target facts | Не request DTO/client Approved flag |
| Reason | Уточняется | Да | Server transition kind | Allowed transition |
| Actor | Уточняется | Уточняется | Trusted actor | Execution context |
| Correlation | Уточняется | Уточняется | Trusted correlation | Execution context |
| Time | Уточняется | Да | Trusted transition time | Server clock |

**ActivationTransitionResult**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Changed | `bool` | Да | Actual transition indicator | No-op не increments Version/Sequence/history/event |
| New pointer | Уточняется (nullable) | Нет | Result current target | Nullable transition semantics |
| LastSequence | Уточняется | Да | Committed subject order | Не technical Version |
| New ActivationRecord | `ActivationRecord?` | Условно | New append record | Только при реальном transition |

Product/Variant/release/revision strong IDs переиспользуются из F06/F09/F10; не surrogate Keys/names/hash. Revision immutable content/lifecycle и F17 iteration models определены там, не дублируются. Frozen item capture/head/ABA/atomic prepare приняты §2.3; attempt/lease/wire summary enum, audit privacy и initial/overflow conventions уточняются Q15/Q20. Lifecycle state/history сохраняют stable VariantId после source move; historical Product context и совместимость с текущим applied parent требуют отдельной проверки.

### 2.2. Методы и переходы

| Тип | Метод | Параметры | Предусловия | Изменение / результат |
| --- | --- | --- | --- | --- |
| Product state | SelectRelease | Exact Available owned release либо null | Protected release/state/current context; Catalog guard применимого scope | Меняет только base; own Version при реальном изменении; clear не Unpublish Variant |
| Variant state | Activate | Exact revision + PublicationTransitionParameters | Same Variant; Available; exact Approved; no blocking F15/availability fact | Новый pointer, LastSequence+1, history и own Version; same active no-op |
| Variant state | Rollback | Same input, reason Rollback | Ранее созданная допустимая Approved revision того же Variant | Такой же append transition к старому ID, не rewind history |
| Variant state | Unpublish | Trusted context, reason Unpublish | Current state; moderation не требуется | Non-null→null/new sequence/history/Version; already null no-op |
| Variant state | ClearWithdrawnRevision | Exact withdrawn ID + trusted context | Только если current pointer равен этому ID; coordination F10 | Null/history reason Withdrawal; другой active ID сохраняется |
| Promotion operation | Create | Own IDs/frozen sources/membership/ProductVersion/VariantSequences/initiator/client ID | Automatic; accepted new logical execution | Immutable operation, own items/new heads; zero members complete |
| Operation item transitions | RecordPrepared / RecordBlocked / RecordFailed | Frozen input/own revision+iteration tuple либо typed issue | Own item Version/stage/current head | Atomic prepare AwaitingModeration or diagnostic; no old-result rewrite |
| Operation item transitions | RecordActivated / RecordRejected / Cancel / Supersede | Exact tuple/head/baseVersion/VariantSequence + trusted outcome | Own item Version/stage and current basis | Atomic Activated transition or terminal item outcome; no fake history |

Rollback не требует повторной moderation той же exact Approved revision. Approval не обходит Retired/terminal Withdrawn/актуальные distribution blockers. Retire source release сам по себе не пересобирает и не запрещает историческую materialized revision: activation валидирует revision eligibility, не требует все старые источники Available для нового F10 Create. Withdrawal dependencies и F15 conflict — отдельные guards.

### 2.3. Инварианты и constraints

1. Actual Variant transition атомарно меняет pointer/Version/LastSequence, добавляет history/outbox и clear/supersede current automatic intent. Same-active/already-null no-op сохраняет Version/history/outbox и head; active F10 Withdrawal меняет Sequence и отменяет head только при фактическом pointer transition.
2. Automatic start атомарно выбирает exact Product base, фиксирует resulting Product state Version, applied Variant membership, current Variant Sequence и latest **created** local ReleaseNumber каждого member. Later releases/members/moves не обновляют frozen inputs.
3. Latest-created local release включает Retired: Retired latest → Blocked без старого fallback. Never any local release + любая непустая из четырёх working groups → Blocked; never any + все группы пустые → Product-only. Explicit empty Variant release считается существующим release.
4. Каждый accepted Automatic start создаёт новую operation при новом ClientOperationId, даже same base, и устанавливает новый VariantPromotionIntent head для captured members, superseding older unfinished items. Zero-member operation завершается с zero items и собственным base-selection result.
5. Prepare атомарно создаёт F10 revision + F17 automatic Pending iteration, exact item tuple и AwaitingModeration в одном Publishing commit; отдельный required prepared event→Submit не нужен. Human decision и activation остаются последующими commits; no batch transaction across items.
6. Continuation требует exact bound RevisionId+ModerationId Approved, current intent ItemId, current captured Product Version и Variant Sequence, fresh applied parent/lifecycle/F15/final-effective dependency facts. Pointer equality не обходится ABA; stale basis не activates.
7. Technical retry сохраняет frozen inputs/item/own created tuple. Domain retry после repair создаёт новую operation с explicit subset/confirmation, new ClientOperationId и RetryOf; old result не переписывается. No adoption чужого Pending/hash/похожей revision.
8. CancelAutomaticItem имеет exact ItemId+own item precondition; cancelling old item не cancels replacement head и не создаёт history. Withdraw exact bound moderation cancels item; new manual iteration не reattaches. Rejected сохраняет старый pointer. Item Cancelled/Superseded не withdraws human moderation и не autoresume после Restore/Resolve/event; Approved artifact остаётся доступен manual Activate с fresh guards.
9. Publishing Activated и Delivery ready независимы; replay processed/terminal item читает durable result без повторной activation после later manual action.

[S21](../standards/S21-aggregate-constraints.md): state/history/operation/item/intent/command-execution constraints обеспечивают unique frozen membership, exact tuple, own item Version, head ownership и ABA fences. Codec/limits/claims/lease/retry numbers/privacy и wire status остаются Q15/Q20.

### 2.4. Domain events

| Fact / contract | Когда создаётся | Потребитель |
| --- | --- | --- |
| ProductVariantPublishingStateChanged | Каждый реальный transition, включая null Unpublish/active Withdrawal | Delivery projection через durable outbox |
| PublicationRevisionPreparedForAutomaticModeration (optional notification) | Own F10 revision+F17 iteration+item tuple already committed together | Observability/existing consumer compatibility only; never required second Submit |
| RevisionModerationApproved / RevisionModerationRejected | F17 committed decision | F11 matched operation item |

**ProductVariantPublishingStateChanged**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| VariantId | Уточняется | Да | Transition scope | Every real transition |
| RevisionId | Уточняется (nullable) | Нет | Exact target or null | Unpublish/active Withdrawal included |
| Sequence | Уточняется | Да | Per-Variant order | Monotonic; contract Q20 |
| ActivatedAt | Уточняется | Да | Transition time | Trusted; не effective time |
| Reason | Уточняется | Да | Server transition reason | F11 action |
| CorrelationId | Уточняется (nullable) | Нет | Trusted correlation | Profile Q20 |
| Event envelope | Уточняется | Уточняется | Durable envelope identity/version | Profile Q20; outbox same commit |

Automatic prepare не вводит обязательный cross-commit prepared-event dependency: F10 Create и F17 internal Submit используют один owner Publishing UoW. Если optional existing notification сохраняется, её correlation fields не удаляются; она не запускает повторный Submit. Codec/topology остаются Q20.

**PublicationRevisionPreparedForAutomaticModeration (optional notification)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| OperationId | Уточняется | Да | Original captured operation correlation | Exact committed own operation |
| VariantId | Уточняется | Да | Frozen member correlation | Not current membership re-selection |
| RevisionId | Уточняется | Да | Exact already-prepared immutable result | Already bound own Pending; not trigger to adopt/create another |
| Submission identity | Уточняется | Уточняется | Existing durable submission/correlation context | Exact committed item/ModerationId tuple carrier Q20; no second Submit |

**RevisionModerationApproved / RevisionModerationRejected**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| RevisionId | Уточняется | Да | Exact decision artifact | Owner F17; trusted committed decision |
| ModerationId | Уточняется | Да | Exact iteration | Matched operation item |
| Decision identity | Уточняется | Уточняется | Trusted decision identity | F17/Q20 |
| Decision order | Уточняется | Уточняется | Trusted decision ordering | F17/Q20 |
| Decision context | Уточняется | Уточняется | Trusted context | F17/Q20; raw payload Approved не authorization |

Transition и committed moderation outcomes сохраняются own Publishing outbox вместе с изменениями; транспортные версии/topology уточняются. Нельзя считать raw broker payload Approved авторизацией без trusted origin/idempotency/актуальной eligibility. F10 withdrawal emits independent exact-ID restriction fact даже без state transition. Audit F18 не заменяет предметную history. Product base audit не является Variant activation event.

## 3. Application.Abstractions

### 3.1. Commands

| Command | Result | Concurrency |
| --- | --- | --- |
| PromoteProductReleaseCommand | ProductPromotionResult | Exact Product state If-Match; target operation recovery Q20 |
| ClearProductBaseCommand | ProductPublicationStateDto | Own Product state; HTTP profile Q15 |
| ActivateVariantRevisionCommand | VariantPublicationStateDto | Own Variant state; server reason Manual |
| RollbackVariantRevisionCommand | VariantPublicationStateDto | Own Variant state; server reason Rollback |
| UnpublishVariantCommand | VariantPublicationStateDto | Own Variant state; reason Unpublish |
| ProcessAutomaticPromotionVariantCommand | PromotionItemOutcome | Own item Version/stage + head/ProductVersion/VariantSequence fences |
| CancelAutomaticItemCommand | Confirmed exact item outcome | Exact ItemId + own item expected Version; replacement head не отменяется |
| HandleModerationApprovedCommand / HandleModerationRejectedCommand | Processing acknowledgment | Durable inbox + matched item + current state/revision guards |

**PromoteProductReleaseCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | Уточняется | Да | Applied owner | Scoped context |
| ProductContentReleaseId | Уточняется | Да | Exact Product base | Owned Available source |
| Mode | `PromotionMode` | Да | Manual/Automatic intent | Не permanent Product setting |
| ExpectedStateVersion | `int` | Да | Original own Product precondition | If-Match header; confirmed receipt lookup before new-execution checks |
| ClientOperationId | `Guid` | Да | Logical StartPromotion execution | Required body GUID; canonical Q20 receipt before new-execution checks |
| RetryOf | `Guid?` | Нет | Previous operation trace | New domain retry; no mutation old operation |
| VariantSubset | `IReadOnlyList<Guid>?` | Условно | Explicit confirmed subset для нового retry | Must belong current applied scope; subset confirmation carrier Q15 |

**ClearProductBaseCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | Уточняется | Да | Exact owner | Clear не Unpublish Variant |
| ExpectedStateVersion | Уточняется | Да | Own Product precondition | Internal int Version per §5.2; If-Match |

**ActivateVariantRevisionCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | Уточняется | Да | Applied parent | Scoped route context |
| VariantId | Уточняется | Да | Stable target Variant | Scoped owner |
| RevisionId | Уточняется | Да | Exact approved artifact | Same Variant; Available; server reason |
| ExpectedStateVersion | Уточняется | Да | Own Variant precondition | Internal int Version per §5.2; If-Match |

**RollbackVariantRevisionCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | Уточняется | Да | Applied parent | Scoped route context |
| VariantId | Уточняется | Да | Stable target Variant | Scoped owner |
| RevisionId | Уточняется | Да | Exact approved artifact | Same Variant; Available; server reason |
| ExpectedStateVersion | Уточняется | Да | Own Variant precondition | Internal int Version per §5.2; If-Match |

**UnpublishVariantCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | Уточняется | Да | Applied parent | Scope profile Q15 |
| VariantId | Уточняется | Да | Stable target Variant | No moderation required |
| ExpectedStateVersion | Уточняется | Да | Own Variant precondition | Internal int Version per §5.2; If-Match |

**ProcessAutomaticPromotionVariantCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| OperationId | Уточняется | Да | Durable operation | Expected persisted stage/attempt |
| VariantId | Уточняется | Да | Captured member | Internal; no public If-Match |

**HandleModerationApprovedCommand / HandleModerationRejectedCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Decision identity | Уточняется | Уточняется | Trusted decision | Durable inbox |
| RevisionId | Уточняется | Да | Exact decided artifact | Matched item/current eligibility guards |
| ModerationId | Уточняется | Да | Exact iteration | Matched correlation |
| Correlation profile | Уточняется | Уточняется | Trusted execution correlation | F17/Q20 |

Trusted actor/time/correlation задаёт execution context; не дополнительные поля body. F10 Withdrawal вызывает coordinated state transition в своей command transaction, не второй public Unpublish с клиентским токеном. Domain retry — новый StartPromotion с explicit subset/confirmation и RetryOf; exact transport Q15. CancelAutomaticItem — отдельное logical действие с ExactItemId Guid и ExpectedItemVersion int; wire URI/status Q15.

**CancelAutomaticItemCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ItemId | `Guid` | Да | Exact automatic item | Does not mean current/latest item |
| ExpectedItemVersion | `int` | Да | Own item precondition | Mutation atomic with matching intent only; transport Q15 |

### 3.2. Queries

| Query | Result |
| --- | --- |
| GetProductPublicationStateQuery | Exact ProductPublicationStateDto + scoped own ETag metadata |
| GetVariantPublicationStateQuery | Exact VariantPublicationStateDto + scoped own ETag metadata |
| ListActivationHistoryQuery | Readonly ActivationRecordDto collection/page |
| GetPromotionOperationQuery | Exact PromotionOperationDto + own token only if chosen representation scope supports it |

**GetProductPublicationStateQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | Уточняется | Да | Exact state scope | Own ETag metadata |

**GetVariantPublicationStateQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | Уточняется | Да | Applied parent | Scoped read |
| VariantId | Уточняется | Да | Exact Variant state | Own ETag metadata |

**ListActivationHistoryQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | Уточняется | Да | Applied scope | History/read profile Q15 |
| VariantId | Уточняется | Да | Stable history owner | Immutable records |
| Paging | Уточняется | Уточняется | Pagination | Q15 |
| Order | Уточняется | Уточняется | History order | Sequence; profile Q15 |

**GetPromotionOperationQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| OperationId | Уточняется | Да | Exact operation | Durable progress |
| Caller access context | Уточняется | Уточняется | Operation visibility | Q15; own token scope if selected |

[S20](../../../catalog/docs/standards/architecture/cqrs.md#handler-responsibilities-and-dependency-direction): commands Application; queries EntityFramework.Persistence. Delivery visibility read composition — отдельный public port/read contract; включение в response и token scope Q15, не foreign join.

### 3.3. DTO и parameter models

| Модель | Назначение |
| --- | --- |
| PromoteProductReleaseRequest | Поля в таблице модели ниже; wire/audit profile остаётся у указанных Q-вопросов |
| RevisionActivationRequest | Поля в таблице модели ниже; wire/audit profile остаётся у указанных Q-вопросов |
| Unpublish input | Поля в таблице модели ниже; wire/audit profile остаётся у указанных Q-вопросов |
| ProductPublicationStateDto | Поля в таблице модели ниже; wire/audit profile остаётся у указанных Q-вопросов |
| VariantPublicationStateDto | Поля в таблице модели ниже; wire/audit profile остаётся у указанных Q-вопросов |
| ActivationRecordDto | Поля в таблице модели ниже; wire/audit profile остаётся у указанных Q-вопросов |
| ProductPromotionResult | Поля в таблице модели ниже; wire/audit profile остаётся у указанных Q-вопросов |
| PromotionItemOutcome / PromotionItemDto | Поля в таблице модели ниже; wire/audit profile остаётся у указанных Q-вопросов |
| PromotionOperationDto | Поля в таблице модели ниже; wire/audit profile остаётся у указанных Q-вопросов |
| DeliveryVisibilityRead | Поля в таблице модели ниже; wire/audit profile остаётся у указанных Q-вопросов |

**PromoteProductReleaseRequest**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductContentReleaseId | `Guid` | Да | Exact selected base | No Version/ExpectedRevision |
| Mode | `PromotionMode` | Да | Promotion intent | Manual/Automatic; Не Product setting |
| ClientOperationId | `Guid` | Да | Logical StartPromotion identity | Required non-empty body GUID; original confirmed receipt Q15 |
| RetryOf | `Guid?` | Нет | Previous operation trace | New operation only |
| VariantSubset | `IReadOnlyList<Guid>?` | Условно | Explicit retry members | Confirmation profile Q15; immutable capture после acceptance |

**RevisionActivationRequest**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| RevisionId | `Guid` | Да | Exact target revision | Reusable Activate/Rollback; server selects reason |

**Unpublish input**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Контекст Unpublish | Уточняется | Уточняется | Route/header scope and precondition | No required free-text Reason invented |
| Профиль body | Уточняется | Уточняется | Optional human comment/body audit input | Q15; body presence not chosen |

**ProductPublicationStateDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | `Guid` | Да | Stable state owner | Own Version absent JSON |
| ActiveReleaseId | `Guid?` | Нет | Current base | null supported |
| Summary | Уточняется | Уточняется | Read summary | Q15; independent token scope |

**VariantPublicationStateDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| VariantId | `Guid` | Да | Stable state owner | Own Version absent JSON |
| ActiveRevisionId | `Guid?` | Нет | Publishing pointer | Не Delivery effective |
| LastSequence | `long` | Да | Subject transition order | Не concurrency token |
| Exact summaries | Уточняется | Уточняется | Read summaries | Q15 |

**ActivationRecordDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | Уточняется | Уточняется | History identity | Wire/audit profile Q15 |
| VariantId | Уточняется | Уточняется | Stable history owner | Wire profile Q15 |
| Sequence | Уточняется | Уточняется | Transition order | Server sequence; profile Q15 |
| RevisionId | Уточняется (nullable) | Нет | Exact target or null | Null transitions retained |
| ActivatedAt | Уточняется | Уточняется | Trusted transition time | Wire/audit profile Q15 |
| Reason | Уточняется | Уточняется | Server action | Wire profile Q15 |
| ActorId | Уточняется (nullable) | Нет | Trusted actor | Audit visibility Q15 |
| CorrelationId | Уточняется (nullable) | Нет | Trusted correlation | Audit visibility Q15 |
| Профиль audit | Уточняется | Уточняется | Additional comment/audit representation | Q15 |

**ProductPromotionResult**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Committed Product state | Уточняется | Да | Confirmed local result | Own token metadata separate; result carrier Q15 |
| OperationId | `Guid?` | Условно | Automatic operation identity | null Manual |
| Result carrier | Уточняется | Уточняется | HTTP metadata/result carrier for commit/token representation | Q15 |

**PromotionItemOutcome / PromotionItemDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| VariantId | Уточняется | Уточняется | Captured member | Exact wire profile Q15/Q20 |
| Status | Уточняется | Уточняется | Durable progress | Activated = Publishing commit, не Delivery effective |
| VariantReleaseId | Уточняется (nullable) | Условно | Selected exact local input | Stage/result profile Q20 |
| RevisionId | Уточняется (nullable) | Условно | Exact prepared result | Q20 |
| ModerationId | Уточняется (nullable) | Условно | Exact moderation iteration | Q20 |
| FailureCode | Уточняется (nullable) | Условно | Diagnostic code | Strings/limits Q15/Q20 |
| FailureMessage | Уточняется (nullable) | Условно | Safe diagnostic | Sanitized; limits Q15/Q20 |

**PromotionOperationDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| OperationId | Уточняется | Уточняется | Durable operation identity | Wire profile Q15/Q20 |
| ProductId | Уточняется | Уточняется | Captured Product | Q15/Q20 |
| ProductReleaseId | Уточняется | Уточняется | Exact chosen release | Immutable source |
| CreatedAt | Уточняется | Уточняется | Creation time | Trusted; wire profile Q15/Q20 |
| CompletedAt | Уточняется (nullable) | Условно | Completion time | Terminal/summary profile Q20 |
| Items | Уточняется | Уточняется | Per-Variant progress | Independent outcomes; own Version absent |
| Overall status | Уточняется | Уточняется | Aggregate status/profile | Q20; enum не выбран |

DeliveryVisibilityRead reuses immutable nontracking [F13 CompleteDeliveryHistoryRead/read values](F13-content-delivery.md#33-dto-и-parameter-models) plus own Publishing active/state metadata; no duplicated Delivery model/Domain entity/internal evidence Version. Staff projection independently distinguishes Publishing active, current committed Delivery effective, processed source progress/gap/materialization failure, necessary/purpose-scoped pending time and exact ban. Matching IDs are not proof of readiness; complete current content may be ready while temporal confirmation pending. Exact exposure/codec [Q15](../questions/Q15-api-and-source-code.md#delivery-visibility-contracts).

Own technical Version отсутствует во всех DTO. Internal committed state/read result несёт Version для Api ETag отдельно от JSON. Nullable effective ID не объясняет pending/outage/unpublished само по себе: нельзя вывести synchronization success только по совпадению IDs.

### 3.4. Ports

| Port | Назначение | Направление |
| --- | --- | --- |
| IProductPublicationStateRepository / IVariantPublicationStateRepository | Load tracked root, conditional own Version save | Publishing storage |
| IPromotionOperationRepository / promotion item store | Immutable operation+membership, independently tracked item Version/stage/tuple | Publishing storage; не DTO repositories |
| VariantPromotionIntent store | Own Variant head Version; atomic replacement/cancel with exact item | Publishing-only state |
| Confirmed CommandExecution store | Scoped ID/input lookup and original receipt with business commit | Shared model Q20; own Publishing transaction |
| IActivationHistoryWriter | Append record атомарно с state | Local persistence |
| IPublicationEligibilityReader | Exact revision lifecycle/owner/materialized distribution eligibility + F17 approval evidence | Public F10/F17 contracts; protected current facts |
| ICatalogPublicationContextReader | Applied Product/Variant context, F15 blockers/maintenance guard | CatalogAccess public adapter, no source latest-parent substitution |
| IAutomaticPromotionSourceReader | Exact F09 releases, ever-published evidence, saved F08 local-work state | Publishing source projection/capture |
| IRevisionPreparationUseCase / F17 internal submission | Full F10 final closure validate/create + F17 automatic Pending + exact tuple | Один caller-owned Publishing UoW; no self HTTP/independent nested commit |
| IDeliveryPublicationVisibilityReader | Effective projection/readiness by Variant | Delivery public query contract |
| IPublishingOutbox / IIncomingDecisionInbox | Durable facts/consumer dedup in local commit | Local infrastructure |
| TimeProvider / IExecutionContext | Trusted timestamp/actor/correlation | Host/adapters |
| MVP consistency guard / owner UoW boundary | Один global transaction exclusive gate по Q20, owner-only atomic commit и independent primary public reads | Shared infrastructure adapter; owner connection, не foreign transaction |

Имена новых ports проектные, не доказанные current symbols. Approval/context/committed-CM ban facts защищает MVP gate §7.2; UoW owner partition обязателен. Carrier/binding/outbox codec остаются Q20/Q15; foreign public read не enlist-ится.

### 3.5. Specifications / read contracts

[S19](../../../catalog/docs/standards/dotnet/ardalis-specification.md): ProductStateByProductIdSpecification, VariantStateByVariantIdSpecification, ActivationHistoryByVariantSpecification, PromotionOperationByIdSpecification, PendingPromotionItemsSpecification. Atomic predicates: OwnedByProductCondition, ForVariantCondition, AtOperationStageCondition, AvailableReleaseCondition; Compose через builder extension в конкретной Ardalis specification, не spec.And.

History ordered by server Sequence с unique tie-free ключом, не ActivatedAt; direction/page/cursor/max Q15. Local input — highest created F09 ReleaseNumber at start capture, including Retired. Retired latest → Blocked без older fallback; reparent/later release проверяются для applicability, не заменяют capture. Projection возвращает исторические exact IDs даже если summaries source недоступны, no silent latest replacement. Read DTO не отслеживается; exact state token принадлежит тому же прочитанному root поколению.

## 4. Application

### 4.1. Handlers / use cases

Mutation handlers соблюдают порядок §7.2: shape/auth preparation → own ReadCommitted transaction → отдельный global gate statement → fresh tracked own roots + independent primary public facts → own token/basis/domain checks → own state/intents commit. Предварительные reads/CPU results не являются authority после ожидания gate.

PromoteProductReleaseHandler, ClearProductBaseHandler, ActivateVariantRevisionHandler, RollbackVariantRevisionHandler, UnpublishVariantHandler, ProcessAutomaticPromotionVariantHandler и F17 decision handlers находятся в Publishing.Application. Каждый получает ports из §3.4, работает с Publishing UoW, возвращает result из своего committed поколения; Query handlers находятся в Persistence. Единую TransitionVariantPublicationUseCase переиспользуют manual, rollback, approved automatic и coordinated F10 Withdrawal с различными guards/reasons.

StartPromotion после authorization/gate сначала ищет confirmed CommandExecution: same scoped ClientOperationId/input возвращает original receipt, changed input409. Только new execution проверяет own Product If-Match/Available/F15, применяет base selection и captures resulting Product Version, applied membership, each Variant Sequence/latest-created local release/local-work evidence. Operation/own items/new heads/superseded old unfinished items/safe audit/receipt фиксируются одним start commit; zero items completes zero-member operation. Manual base selection меняет только Product state.

Manual Activate/Rollback/Unpublish и active Withdrawal используют TransitionVariantPublicationUseCase: actual pointer transition atomically clear/supersede matching automatic head, state/history/outbox; no-op не изменяет head/token/history. CancelAutomaticItem handler работает по exact item own precondition и clears intent только если head ещё этот ItemId.

Automatic prepare читает только frozen inputs/item под новым gate, revalidates current applicability/final-effective closure, создаёт own F10 revision+F17 automatic Pending+exact tuple+AwaitingModeration одним commit. Existing tuple используется при technical retry; чужая Pending/similar hash не adopted. Human decision сохраняется отдельно; continuation повторно проверяет exact Approved iteration/current head/Product Version/Variant Sequence/fresh parent/F15/lifecycle/restrictions и atomically commits item Activated + pointer/history/outbox/inbox. Cancelled/Superseded/terminal result читает durable outcome без reactivation.

### 4.2. Validators

[S18](../../../catalog/docs/standards/dotnet/fluentvalidation.md): PromotionModeValidator, ProductIdValidator, VariantIdValidator, ReleaseIdValidator, PublicationRevisionIdValidator, OperationIdValidator, ExpectedStateVersionValidator и ClientOperationIdValidator переиспользуют constraints. PromoteProductReleaseValidator/ActivateVariantRevisionValidator/RollbackVariantRevisionValidator/UnpublishVariantValidator/ProcessPromotionVariantValidator compose смысловые компоненты. No repeated literal string limits/guessed comment requirement.

FV проверяет binding/ID/enum/request shape. Authorization, mutable Available/Approved/F15/owner facts, no-local-work classification, operation-stage/unique correlation и commit concurrency — Application policies/Domain/DB; async preliminary validator не race guard. Inbox проверяет trusted contract identity/version/correlation; payload не допускает arbitrary client actor/approved marker.

### 4.3. Mappers

Api: route/body/header → command + trusted context; own ETag parser → expected int Version. Domain: validated eligibility/source evidence → transition/materialization parameters, не DTO→tracked root. StateMapper возвращает committed pointer/sequence и отдельный token carrier; ActivationHistoryProjection/PromotionOperationProjection берут только нужные read columns. PromotionItemMapper sanitizes diagnostics без stack trace/source payload. Integration mapper формирует versioned facts из committed exact IDs/sequence/correlation, не из request body или позднего latest-read. DeliveryVisibilityMapper не переименовывает effective в active.

### 4.4. Services / algorithms

AutomaticPromotionCoordinator хранит frozen capture, head и independently mutable item progression; F10 composer остаётся единственным алгоритмом materialization. PublicationTransitionPolicy обновляет intent только при actual manual/Withdrawal pointer transition. Technical retry сохраняет exact frozen inputs/item/tuple; domain retry после repair создаёт новую operation/subset/confirmation/RetryOf. Numeric backoff/lease/limits Q20/Q15.

ApprovalContinuationPolicy требует exact RevisionId/ModerationId Approved + matching current automatic ItemId + unchanged captured Product state Version + Variant publication Sequence, затем fresh applied context/F15/lifecycle/final-effective dependency restrictions. ABA A→B→A/R1→R2→R1 invalidates captured tokens даже pointer тот же. New head supersedes old unfinished item; manual no-op не отменяет head. Withdraw iteration cancels exact bound item; новая manual iteration не reattach. Cancelled/Superseded не autoresume от Restore/Resolve/event, но retained Approved artifact можно вручную Activate под fresh guards.

ConfirmedCommandExecutionCoordinator использует единый Q20 receipt: auth → MVP gate → receipt/input lookup → только для нового execution current preconditions → business+receipt atomic commit. Lost ACK retry с тем же original input/ClientOperationId возвращает original receipt; новый ETag/input с уже bound ID409. Hash equality не dedup.

## 5. Api

### 5.1. Endpoints

Следующая карта сохранена из target F11; final OpenAPI/signatures/status/body/access уточняются [Q15](../questions/Q15-api-and-source-code.md#f11-publication-state-и-automatic-promotion). Это не маршруты существующего [PublishingController](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Api/PublishingController.cs), который обслуживает `/api/v1/publishing/...`, combined workspace/body ExpectedRevision и release-bound updates/retry.

| Method | Route / target contract | Request | Response | Access |
| --- | --- | --- | --- | --- |
| GET | /api/v1/products/{productId}/publication-state | ProductId | ApiResponse<ProductPublicationStateDto> + own state ETag | Scope/permission Q15/Q10 |
| POST | .../publication-state/promote | PromoteProductReleaseRequest + If-Match Product state | ApiResponse<ProductPromotionResult>, committed state token/result carrier Q15 | Publication operation permission Q15/Q10 |
| TBD | Product base Clear endpoint уточняется | ClearProductBase input + If-Match Product state | ApiResponse<ProductPublicationStateDto> | Scope/maintenance profile Q15 |
| GET | /api/v1/products/{productId}/variants/{variantId}/publication-state | ProductId, VariantId | ApiResponse<VariantPublicationStateDto> + own state ETag | Scoped read Q15/Q10 |
| POST | .../publication-state/activate | RevisionActivationRequest + If-Match Variant state | ApiResponse<VariantPublicationStateDto> + own ETag | Approved target + permission |
| POST | .../publication-state/rollback | RevisionActivationRequest + If-Match Variant state | ApiResponse<VariantPublicationStateDto> + own ETag | Historical exact Approved target + permission |
| POST | .../publication-state/unpublish | Route/header; optional body profile Q15 | ApiResponse<VariantPublicationStateDto> + own ETag | Scoped permission; no moderation |
| GET | .../variants/{variantId}/activation-history | Route + chosen paging/order | ApiResponse<ActivationRecordDto collection/page> | History permission Q15 |
| GET | /api/v1/product-release-promotions/{operationId} | OperationId | ApiResponse<PromotionOperationDto> | Product/Variant operation visibility Q15 |

Logical CancelAutomaticItem и new-operation domain retry с subset/confirmation приняты; их exact URI/body/status и Delivery readiness carrier остаются Q15. Не объявлять их существующими по аналогии с legacy updates/retry. Nested route ownership проверяется сервером; старый Product path после move не переносит POST незаметно на новый parent.

### 5.2. ETag / If-Match

[S16](../standards/S16-rest.md): exact mutable Product/Variant state GET и confirmed mutation возвращают strong numeric ETag собственного int Version. Mutations require If-Match; Api передаёт expected Version внутрь command, JSON Version/ExpectedRevision отсутствуют. Missing428, stale412, domain409; malformed/weak/wildcard/multiple header rules по общему parser/OpenAPI, exact profile Q15. List/history не источник per-state ETag. State version не revision lifecycle/hash/LastSequence/operation item version.

No-op не increment state; stale/no-op error priority и lazy initial singleton creation profile Q15/Q20. Successful response mapping использует commit result, не hidden обязательный GET после mutation. Product promotion result содержит operation identity, но Product ETag не доказывает неизменность operation progress. Если state response содержит independently mutable F17/Delivery summary, whole-response strong ETag нельзя строить только по state Version: separate read/token scoping profile Q15. Operation summary derived; exact mutable item own token separate, progress одного item не bumps другие. Wire summary/result metadata remains Q15.

### 5.3. OpenAPI и ошибки

Document explicit request/enums/nullable pointers/ETag/If-Match/ApiResponse, ValidationProblemDetails и ProblemDetails, 400/401/403/404/409/412/428 по применимости. Exact success/status и machine codes уточняются Q15, не выводятся из POST или legacy200. Activation acknowledgment означает Publishing committed transition; operation Activated означает то же, не consumer commit. NSwag обновляется после согласования/проверки backend contracts; per-call token/result metadata должны быть доступны клиенту без global header mutable state.

## 6. EntityFramework

### 6.1. DbContext ownership

PublishingDbContext владеет ProductPublicationStates, ProductVariantPublicationStates, ActivationHistory, PromotionOperations/Items, outbox/inbox и собственными F09/F10 roots. Catalog/F17/Delivery facts доступны через публичные contracts; ни foreign DbContext, ни tracked чужой aggregate не попадают в transaction. F17 physical ownership уточняется в F17/Q20; co-location в одном host не разрешает чужой DbContext.

### 6.2. Configurations

Имена таблиц ниже логические целевые; exact snake_case/layout/migration Q20. Current [State mapping](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.EntityFramework/Configurations/ProductVariantPublicationStateConfiguration.cs) stores shared long Revision/LastManualRevision; [History/job mapping](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.EntityFramework/Configurations/PublicationRevisionConfiguration.cs) has unique Variant+Sequence и release+Variant job, string Status token. Эти ограничения не заменяют целевую operation-stage модель.

| Entity | Table | Key/index | Concurrency | Mapping |
| --- | --- | --- | --- | --- |
| ProductPublicationState | Publishing.ProductPublicationStates | PK Id; unique ProductId | own Version int token | Nullable release ID; strong-ID conversion |
| ProductVariantPublicationState | Publishing.ProductVariantPublicationStates | PK Id; unique VariantId | own Version int token | Nullable revision ID, separate LastSequence long |
| ActivationRecord | Publishing.ProductVariantActivationHistory | PK Id; unique VariantId+Sequence; history Variant read index | Immutable insert-only | Trusted UTC timestamp, typed reason, nullable target/audit |
| PromotionOperation | Publishing.ProductReleasePromotionOperations | PK Id; source/time lookup profile Q20 | Immutable input root | Frozen Product/release/base Version/member IDs/initiator/client ID/RetryOf |
| PromotionItem | Publishing.ProductReleasePromotionItems | PK ItemId; unique OperationId+VariantId; exact tuple | own Version int token | Frozen baseVersion/VariantSequence/release IDs; own stage/outcome |
| VariantPromotionIntent | Publishing.VariantPromotionIntents | VariantId PK/unique | own Version int token | Nullable CurrentAutomaticItemId |
| CommandExecution | Own Publishing confirmed execution store; schema/codec Q20 | Unique logical scope+ClientOperationId | Immutable confirmed receipt | Q20 canonical input/actor/original result; atomic business commit |
| Outbox/inbox | Shared configured Publishing storage | Stable message/processing identity unique | Chosen durable adapter | Envelope version/order/correlation; payload profile Q20 |

### 6.3. Constraints / indexes

Unique state owner защищает concurrent first-create; unique Variant+Sequence защищает append order. Sequence положительный у history, root LastSequence не отрицательный; RevisionId null допустим только согласно null transition semantics. Unique item membership не равно request dedup по ProductReleaseId+VariantId: new ClientOperationId → new operation; bound ID/input receipt semantics Q20. Intra-Publishing references допустимы; cross-BC IDs не foreign FK. Stage/result consistency и immutable history safeguards DB/application выбранного profile, cascade delete истории запрещён. Limits/enums/overflow checks derived from named constraints, not current storage literals.

## 7. EntityFramework.Persistence

### 7.1. Repositories / query adapters

Host/DI задаёт owner-partitioned UoW participants: в transaction входят только DbContext/own outbox/inbox данного BC. Текущий EfCoreUnitOfWorkParticipant, способный объединять несколько DbContexts в общей transaction, не является target coordination adapter: participant selection разделяется по owner BC, foreign public read adapter создаёт независимый primary scope/connection без ambient enlistment. Guard adapter использует owner connection/transaction; mapping/repository другого BC в command handler не внедряется.

EfProductPublicationStateRepository, EfVariantPublicationStateRepository, EfPromotionOperationRepository, EfActivationHistoryWriter реализуют ports §3.4 через PublishingDbContext. GetProductPublicationStateQueryHandler/GetVariantPublicationStateQueryHandler/ListActivationHistoryQueryHandler/GetPromotionOperationQueryHandler и AutomaticPromotionSourceReader размещаются здесь, projections/read-only specifications §3.5. Нет repository на каждый history row/item/DTO.

Command repository возвращает tracked authoritative root; query читает exact representation и token из одной generation. Conditional save operation item не overwrites concurrent other-item progress. Stage получает новый gate и fresh operation/root read; own item Version сохраняет conditional update без bump других item tokens, не stale whole collection replacement. Worker claims/leases/recovery/query indexing и chosen atomic dedup Q20. DI регистрирует one accepted handler/query path, public context/eligibility/visibility adapters и durable outbox consumers в Host; current [App module](../../../../contentmanager.api/src/App/ContentManager.Application/ContentManagerApplicationModule.cs) registration не параллельный второй путь mutation.

### 7.2. Transactions

По [MVP consistency guard](../questions/Q20-persistence-and-migration.md#mvp-протокол-consistency-guard) каждый invalidating writer начинает **свою** BC transaction с явно заданным ReadCommitted и отдельным первым SQL statement получает один стабильный глобальный exclusive pg_advisory_xact_lock. Lock выполняется на том же открытом owner connection, на котором будут authoritative reads, row locks, запись и commit; общий ключ одинаков во всех репликах CM. До получения gate нельзя authoritative-read/check/row-lock затронутый basis. Для replayable Create/StartPromotion/Submit первым authoritative own read после gate идёт confirmed CommandExecution lookup; только новый execution загружает tracked business roots и проверяет expected Version. Остальные writes заново загружают own roots/expected token; чужие current facts читает только через public read ports в независимом primary connection/scope, без ambient UoW enlistment, foreign EF, shared SQL transaction, cache или read replica. Публичный read carrier остаётся предметом Q15; механизм сериализации уже выбран.

Все способные инвалидировать проверяемый basis writers используют тот же gate: F07 whole Save/setup, F08 patch Save/setup, ContentModeling/Media lifecycle/reference changes, CatalogAccess applied relation/F15 Resolve, F09 CreateRelease/Retire/Restore, F10 CreateRevision/lifecycle/Withdrawal, F11 Select/Activate/Rollback/Unpublish и promotion capture/per-item/stage/late Approval, F12 policy changes, F15 scoped repair, релевантные F17 decisions и Delivery-owned invalidation commits. Каждый изменяет только own BC state. Own state/token, required own outbox, safe audit intent и inbox marker фиксируются атомарно; commit/rollback освобождает transaction lock. Это общий write gate в одной PostgreSQL database между репликами, а не общая транзакция BC.

CPU-подготовка, immutable projection/canonical hashing и предварительные проверки могут быть снаружи gate, но после acquisition весь влияющий на commit basis заново подтверждается до применения подготовленного результата. Gate нельзя удерживать во время UI/человеческого Approval, VR School HTTP, S3/bytes, broker send/ACK, ожидания Delivery projection либо HTTP stream; эти действия и downstream commits имеют отдельные границы.

Acquisition wait ограничен выбранной конфигурацией и cancellation; timeout/cancel/lock failure — технический отказ, не domain Conflict. Failed/partial public port не превращается в empty compatible facts. Потеря owner connection прерывает операцию целиком; reconnect не продолжает прежний basis. Own expected Version mismatch остаётся 412; independent relevant basis drift — 409 с повторной проверкой/review, без автоматического retry с новым ETag. Потерянный commit ACK означает unknown outcome: inquiry по durable operation identity/выбранному Q15 recovery, а не утверждение rollback или слепой повтор.

Read queries, pickers, preview/review не получают этот global gate и не выдают commit permit. Own DTO и token читаются согласованно из одного owner поколения; composite basis/freshness других roots учитывается отдельно. Последующая mutation получает новый gate и перепроверяет basis.

F11 gate обязателен для DF02 promotion membership capture, DF03 Clear, DF05 Activate, DF06 Rollback, DF07 Unpublish, DF10 каждого item/preparation commit, DF11 atomic submission/binding как часть того же prepare commit и DF12 каждого committed decision/late Approval transition. Gate не удерживается между стадиями или в ожидании человека. Перед каждой стадией новый gate и свежие own state/item/revision/moderation плюс Catalog/F15/current restriction facts; previous capture не служит permit. Late Approval/head/ABA и frozen source selection уже определены §2.3; lease/codec carriers остаются Q20.

| Commit boundary | Atomic local writes | Что выполняется отдельно |
| --- | --- | --- |
| Product promotion | Actual base state/Version, immutable operation/frozen membership+inputs, own items/intent supersession, safe audit + confirmed receipt | Per-item prepare; zero-member operation complete; no Variant pointer writes |
| Manual/rollback/unpublish | Actual state pointer+Version+Sequence/history/outbox + head clear/supersession; no-op no writes/head cancellation | Outbox dispatch и Delivery consumer |
| Automatic prepare | New own F10 revision + F17 automatic Pending + exact item tuple/AwaitingModeration/safe audit in one commit | Human decision and later activation; no required PreparedEvent→Submit gap |
| Approved automatic activation | Inbox decision processing, protected state transition/history/outbox + item Activated | Delivery projection commit/read visibility |
| Rejected/blocked/failure | Correlated item outcome and inbox where applicable, own item Version/stage concurrency | Other items untouched; previous pointer preserved |
| F10 active Withdrawal | Revision lifecycle + matching active null transition/history/outbox + independent exact-ID restriction outbox | Delivery ban/effective projection; no clearing unrelated revision |

После first gate source capture/eligibility checks защищены до commit против Retire/Withdraw/parent/F15 changes; ordinary SELECT и new-row optimistic token не proof. State/revision mutual coordination исключает committed active Retired/Withdrawn и F09 active-base Retired. ReadCommitted + first exclusive transaction advisory gate и fresh owner/public primary reads заданы §7.2; late-decision head+captured Version/Sequence fences определены §2.3. Error classification после failed UoW disposal/clean tracking distinguishes stale client own state412 от domain/concurrent dependent fact409; не ловить всё как successful Failed outcome.

### 7.3. Migrations

Добавить state own int Version/LastSequence, immutable operations, own item Version/captured inputs/tuple, VariantPromotionIntent и confirmed CommandExecution receipt, audit/inbox/outbox. Shared old Revision assignment/policy/activation counter не переносить как три независимых tokens без выбранного transition plan. Старый ProductPromotion release+Variant uniqueness/IsRevoked model/implicit source-from-active не target compatibility constraint. CM не запущен production: dev-data reset/forward policy Q20; исторические target данные сохраняются после введения. Никакие migrations или reset в рамках аналитики не выполняются. Verification включает PostgreSQL upgrade/roundtrip/history immutability/constraint races; source current configuration не подтверждает target migration.

## 8. External adapters

### 8.1. RabbitMQ / MassTransit

Durable Publishing outbox dispatches state changes to Delivery и committed exact moderation outcomes to continuation; decision consumer принимает versioned F17 facts с inbox. [S22](../standards/S22-eventbus-topology.md) используется после проверки точного topology/contract package; конкретные exchange/queue/routing keys/retry counts уточняются Q20. Duplicate delivery допускается, duplicate domain effects запрещены; ACK после relevant local commit. Crash после send до marking dispatched требует consumer dedup, no exactly-once transport promise.

Delivery owns [F13](F13-content-delivery.md#21-aggregates-entities-и-value-objects) source-processing receipts, actual history and first saved primary observation evidence separately. One actual full graph/pointer/history+Pending evidence commit per source transition; known terminal blocked outcome only advances processed source/inbox, no fake history. Ordered source completeness/catch-up, null/replay rules do not require Publishing transaction to wait for consumer/time confirmation. Terminal exact-ID withdrawal restriction dominates delayed activation/rebuild; stale event не снимает ban. Payload completeness/checksum/canonical hash Q19/F13; producer не пишет DeliveryDbContext. F17 moderation и Delivery projection могут жить in-process, но ownership/atomic boundary не меняются.

### 8.2. Clock, execution context и фоновые задания

TimeProvider и доверенный identity/correlation context supplies server timestamps/audit. Worker просыпается по committed pending/recovery work с cancellation; расписание/lease/backoff/batch cap Q20, current 30sec/100 jobs не SLA. Technical retry повторяет тот же сохранённый stage/result; safe manual reprocess blocked subset после исправления не объявляется blind retry. Browser polling/readiness нужен для наблюдения, не часть commit acknowledgment. F11 не требует S3 uploads или внешнего activation-key HTTP; это F13/F16.

## 9. Data flows

Api → Application command → protected facts/Domain transition → Publishing persistence/history/outbox → committed response. Query → Persistence projection → scoped response. Background prepare → F10 durable result → F17 decision → F11 activation → Delivery consumer commit — отдельные стадии.

| Поток | Транспорт | Метод / канал | Endpoint / destination | Доступ | Вход | Application action | Выход | Успех | Ошибки |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| DF01. Product state | HTTP | GET | Product publication-state §5.1 | Scoped read Q15 | ProductId | GetProductPublicationStateQuery | Exact state + own ETag | Status Q15, exact generation | Scope/missing/read unavailable |
| DF02. Select/promote base | HTTP | POST | Product publication-state/promote | Scoped publication Q15 | Route, request ClientOperationId, original state If-Match | PromoteProductReleaseCommand | ProductPromotionResult | Publishing commit, status Q15 | Invalid source/scope,428/412/409 |
| DF03. Clear base | HTTP profile TBD | Method TBD | Contract Q15 | Maintenance scope Q15 | ProductId, state If-Match | ClearProductBaseCommand | Product state + ETag | Local commit/no-op profile | Scope/precondition/domain conflict |
| DF04. Variant state | HTTP | GET | Variant publication-state §5.1 | Scoped read Q15 | Parent/Variant IDs | GetVariantPublicationStateQuery | State + own ETag | Exact generation/status Q15 | Wrong parent/missing/read failure |
| DF05. Activate | HTTP | POST | Variant publication-state/activate | Publication scope | Target ID, own state If-Match | ActivateVariantRevisionCommand | State + ETag | Commit либо valid no-op | Unapproved/unavailable/blocker,428/412/409 |
| DF06. Rollback | HTTP | POST | Variant publication-state/rollback | Publication scope | Historical target, state If-Match | RollbackVariantRevisionCommand | State + ETag | New history либо same-active no-op | Same eligibility/precondition failures |
| DF07. Unpublish | HTTP | POST | Variant publication-state/unpublish | Scope Q15, no moderation | Route/header/body profile | UnpublishVariantCommand | State + ETag | Null transition либо no-op | Scope/precondition/guard failures |
| DF08. History | HTTP | GET | Variant activation-history | History scope Q15 | Route/page/order profile | ListActivationHistoryQuery | Ordered history collection/page | Stored history read | Scope/page/read failure |
| DF09. Operation progress | HTTP | GET | product-release-promotions/{operationId} | Operation visibility Q15 | OperationId | GetPromotionOperationQuery | Operation/items | Current durable progress | Missing/scope/read failure |
| DF10. Automatic prepare/recovery | Background job | Committed own pending item | Publishing Application | Trusted captured scope | Exact item/frozen inputs/current head | Atomic F10+F17 prepare | Own revision+iteration+tuple/AwaitingModeration | One Publishing item prepare commit | Blocked/transient/claim; no adoption |
| DF11. Automatic submission | Internal DF10 call | Same prepare UoW | F17 Application | Trusted captured initiator | Own new revision/exact ItemId | Internal Submit | Own Pending+ModerationId | Same DF10 commit, no separate ACK | Any failure rolls prepare back |
| DF12. Moderation decision | Broker + inbox | Approved/Rejected fact | F11 decision consumer | Trusted committed F17 decision | Exact iteration/revision/item identity | HandleModeration decision command | Activated либо Rejected item | Local commit + ACK | Duplicate/stale/withdrawn/blocked/race |
| DF13. Client visibility | Outbox trigger + source public history | State changed | F13 ordered projector/confirmation | Trusted source | Exact pointer/PublishingSequence/completeness | F13 processing + later confirmation | Actual state/history vs processed source/pending observation | Separate own outcomes and first saved evidence, no publisher wait | Gap/integrity/ban/technical prep/pending time |

### DF01. Прочитать Product state

1. Api binds stable ProductId, выполняет permission/scope checks и передаёт query.
2. Persistence получает applied Catalog context через public read и собственный exact state; initial missing state representation/ETag profile Q15/Q20, GET не создаёт неявный release/operation.
3. Projection возвращает nullable base и own Version carrier из одной generation; Api maps scoped ETag. Independently changing summaries требуют profile §5.2. History/assignments не мутируются.

### DF02. Выбрать base и запустить promotion

1. Authenticate/authorize current actor/scope; bind original input including required ClientOperationId, original If-Match, exact source/comment/review/confirmation.
2. Own ReadCommitted transaction + first global gate. Confirmed CommandExecution lookup до new-execution preconditions: identical normalized input/actor → original receipt; bound changed input409; no replay Restore/Submit/Activate.
3. New execution loads tracked Product state/current facts, checks expected Version, exact owned Available ProductRelease/F15. Apply base selection; capture resulting Product state Version.
4. Capture current applied Variant membership (или явно confirmed retry subset), each current Variant Sequence/latest-created local ReleaseNumber ID/local-work evidence. Retired latest/archived/conflicted members → own Blocked; no older fallback. Later members/moves/releases never replace frozen inputs.
5. Create immutable operation/own items, update each accepted automatic head; supersede older unfinished head without undoing history. Zero members complete zero items.
6. Atomic own commit state/operation/items/intent/safe audit + original confirmed receipt. Return original own result/OperationId; worker/Delivery later, lost ACK reconcile same original ClientOperationId/input.

### DF03. Очистить Product base

Внутренний write path после Api/FV/проверки caller: открыть owner ReadCommitted transaction, отдельным первым statement получить global pg_advisory_xact_lock на owner connection; только затем выполнять указанные ниже authoritative reads/row locks. Foreign facts — independent primary public ports без enlistment. Все guard/basis checks повторяются для этого commit; §7.2 задаёт failure/recovery и запрет внешнего ожидания под gate.

1. Api profile Q15 binds ProductId/If-Match/trusted context; Application validates access/current applied context.
2. Protected state expected Version; Domain SelectRelease(null). Согласование F09 Retire не позволяет stale Select снова установить уже Retired release.
3. Actual clear commits own state/audit; Variant states/history/Delivery untouched. Same-null Product no-op/token/audit profile Q15. Возврат подтверждённого состояния/token; нет auto Unpublish или release lifecycle mutation.

### DF04. Прочитать Variant state

1. Bind ProductId/VariantId, scoped read; query verifies actual applied parent, не silent redirect после move.
2. Persistence projects own pointer/LastSequence/Version из согласованного root generation; historical summaries не заменяют ID latest artifact.
3. Optional Delivery/F17 reads через public ports различают null/pending/unavailable; exact representation/token scope Q15/Q17. Api returns own-state metadata only по выбранному scoped contract; чтение не меняет pointer и не дожидается фоновой readiness как mutation prerequisite.

### DF05. Активировать exact revision

Внутренний write path после Api/FV/проверки caller: открыть owner ReadCommitted transaction, отдельным первым statement получить global pg_advisory_xact_lock на owner connection; только затем выполнять указанные ниже authoritative reads/row locks. Foreign facts — independent primary public ports без enlistment. Все guard/basis checks повторяются для этого commit; §7.2 задаёт failure/recovery и запрет внешнего ожидания под gate.

1. Bind own state If-Match и exact target; get trusted caller/time/context. Load protected current state, exact materialized revision and current eligibility, F17 Approved evidence, applied parent/F15 facts.
2. Check state precondition, target same Variant, Available/not Retired/Withdrawn, approved exact revision и отсутствие distribution blocker. Source release Retire alone не new-use revalidation старого materialized artifact.
3. Domain Activate reason Manual; same-active valid no-op preserves automatic head. Actual result increments LastSequence/Version, adds ActivationRecord and atomically clears/supersedes current automatic intent.
4. Commit pointer/history/outbox atomically with protected facts; stale own token412, eligibility409 по agreed error profile. No DeliveryDbContext write; rollback on failed commit includes history/outbox.
5. Return committed state/ETag. Outbox later projects Delivery; old effective remains until full ready commit. Side effects/retry не добавляют вторую history при already processed event.

### DF06. Rollback

Внутренний write path после Api/FV/проверки caller: открыть owner ReadCommitted transaction, отдельным первым statement получить global pg_advisory_xact_lock на owner connection; только затем выполнять указанные ниже authoritative reads/row locks. Foreign facts — independent primary public ports без enlistment. Все guard/basis checks повторяются для этого commit; §7.2 задаёт failure/recovery и запрет внешнего ожидания под gate.

1. Binding/access/own state precondition как DF05. Read chosen historical exact ID, materialized eligibility и Approved iteration evidence; no requirement “previous history row only”.
2. Domain Rollback reuses transition core with server reason Rollback; R1→R2→R3→R1 appends new Sequence, не редактирует прежние строки или revision Items/hash.
3. Commit state/history/outbox, map own result. Retired/Withdrawn/never Approved target запрещён; exact prior Approval не требует нового Submit. Same-active no-op preserves current head; actual Rollback clears/supersedes head. Delivery projection получает новый transition к старому artifact, а не отмену consumer history.

### DF07. Unpublish

Внутренний write path после Api/FV/проверки caller: открыть owner ReadCommitted transaction, отдельным первым statement получить global pg_advisory_xact_lock на owner connection; только затем выполнять указанные ниже authoritative reads/row locks. Foreign facts — independent primary public ports без enlistment. Все guard/basis checks повторяются для этого commit; §7.2 задаёт failure/recovery и запрет внешнего ожидания под gate.

1. Bind scope/own state If-Match; get protected state и применимый maintenance context Q15. F17 decision и новый revision не нужны.
2. Domain Unpublish: actual non-null→null appends reason Unpublish/Sequence; already-null no-op preserves head; actual non-null→null clears/supersedes current head. No revision lifecycle edits, no fallback.
3. Commit own state/history/outbox, return committed token. Delivery null applies asynchronously; historical frozen access остаётся F12/F13, Unpublish не exact-ID ban. F10 Withdrawal использует свой protected lifecycle+conditional state flow, не этот public command.

### DF08. Прочитать history

1. Validate scoped route/paging, verify applied current owner/access к stable Variant и историческому scope Q15.
2. Persistence reads immutable activation rows filtered by Variant, orders by Sequence chosen direction, applies agreed page/cursor. Actor/source summaries optional/read profile не скрывает history при retired source.
3. Return projected page/collection без per-state mutation ETag. Same timestamps/rollback/null targets сохраняют порядок; no recomposition/history delete/repair на read.

### DF09. Прочитать promotion progress

1. Authenticate/scope query by OperationId, проверяя captured Product/Variant visibility после move и доступность diagnostics Q15.
2. Persistence читает coherent operation/item snapshot выбранного concurrency profile, exact source/result/moderation IDs и durable status, не выводит completion по browser cached revisions.
3. Map safe diagnostics, link F17/F10. Activated отдельно от optional Delivery readiness. Missing/outage не synthetic success/empty batch. Operation token/overall terminal/completedAt projection Q20.

### DF10. Подготовить Automatic item и восстановить работу

1. Worker opens own transaction/first gate, reads fresh exact item/Version/head/tuple. Terminal result → read outcome only; Cancelled/Superseded never autoresume.
2. Use frozen Product/Variant release IDs, Product state Version/Variant Sequence. Revalidate current owner/lifecycle/F15 and final-effective closure; later release/member не enters. Retired latest captured input →Blocked, no older fallback.
3. Technical retry preserves exact inputs/item/tuple. No tuple → execute F10 materialization and F17 internal automatic Submit in the same caller-owned Publishing UoW; no reuse чужого Pending/hash/similar artifact.
4. Commit own revision + Pending moderation + RevisionId/ModerationId binding + AwaitingModeration + safe audit together. Failure rolls all back; confirmed tuple retried as own result.
5. Human decision later. Domain repair does not rewrite old item: new operation/subset/confirmation with RetryOf.

### DF11. Подать automatic revision на moderation

Этот DF — внутренняя часть **того же prepare commit DF10**, а не отдельный required consumer/PreparedEvent stage.

1. F17 internal use case получает trusted captured initiator и exact just-created RevisionId в caller Publishing UoW, checks current eligibility/unique Pending.
2. Create own new Pending iteration and bind exact ItemId/RevisionId/ModerationId; чужая existing Pending не adopted.
3. Save tuple/AwaitingModeration + revision+iteration atomically; no nested independent F17 commit, no awaiting broker correctness gap. Human Approve/Reject/Withdraw commits separately.

### DF12. Обработать committed decision

1. Trusted exact decision/outbox identity → new own transaction/first gate → fresh inbox/item/iteration/head/state. Duplicate/terminal processing returns durable result without new activation.
2. Reject → item Rejected/old pointer kept. Withdraw bound iteration → exact item Cancelled, matching head cleared; new manual iteration not reattached. Item cancellation/supersession itself does not withdraw human moderation.
3. Approved continuation requires exact bound RevisionId+ModerationId Approved, intent current ItemId, captured Product state Version still current and captured Variant LastSequence still current, fresh applied parent/lifecycle/F15/final-effective restrictions. ABA pointer return is rejected by Version/Sequence.
4. Allowed transition atomically commits item Activated + state pointer/Version/new Sequence/history/outbox/inbox + intent completion. Later duplicate cannot reactivate after manual transition.
5. Changed head/Cancelled/Superseded stays terminal; no hidden Restore/Resolve/event resume. Technical retry same frozen inputs; domain recovery new operation/subset/RetryOf.
6. CancelAutomaticItem logical action uses exact ItemId + own item precondition under first gate, sets Cancelled and clears head only if still this item; no fake activation history or cancellation replacement head.

### DF13. Применить Publishing transition в Delivery

1. Publishing outbox fact is trigger only. F13 primary public committed source history/completeness drives ordered catch-up;12→15 requires13/14. Known terminal banned intermediate is consumed NotEffective without history/stall, technical missing artifact not consumed.
2. F13 stages outside gate (known ban bypasses mandatory artifact prep), then new own ReadCommitted/global gate/fresh order/owner ban checks, committing one real full graph/pointer/history+Pending evidence+source receipt/processed sequence/inbox. Original ACK unknown reconciles exact stored outcome.
3. Separate F13 confirmation observes exact already committed history/full snapshot (or null) on primary even pointer advanced, serial needed pending first; saves first primary UTC VisibilityConfirmedAt. Timestamp is neither first nor confirmation commit time, replay unchanged/no backdate.
4. Staff read uses F13 immutable projections to show Publishing active vs actual committed Delivery effective, source gap/materialization/pending time/ban independently. Matching IDs do not prove ready; necessary temporal unknown DataNotReady, otherwise-allowed current complete content may serve.
5. Conservative strict cutoff uses saved observation; commit-before/confirm-after excluded. F12 agreement/current-period rules remain; opened-session credential/TTL/revoke and bounded response leases follow the accepted Q16 protocol. Details defined once F13/Q17, no producer→Delivery DbContext/transaction.

## 10. Ошибки и диагностика

| Ситуация | Поведение / HTTP или item outcome |
| --- | --- |
| Invalid IDs/enum/request/header shape | FV/Api validation, 400 profile S16/Q15 |
| Required own If-Match missing/stale | 428/412; current changed state/token после safe re-read, no hidden automatic retry mutation |
| Wrong applied parent/unknown target/access | 401/403/404 hiding/profile Q15/Q10; no redirect mutation |
| Unapproved/Retired/Withdrawn/wrong Variant/blocking F15 | Domain conflict409 по confirmed profile; exact issue code/path Q15 |
| First local draft/no first release or invalid strict composition | Automatic Blocked with safe diagnostic, prior active unchanged |
| F17 rejection | Item Rejected, no activation; no treating as transient error |
| Broker/guard service/DB transient outage | Recover durable stage/result; Failed/Pending retry presentation profile Q20, no fabricated Approval/effective success |
| Concurrent claim/decision/state/source lifecycle | Conditional commit/reconcile; own user-token stale412 отличается от internal item/eligibility race |
| Duplicate/replayed/late facts | Exact tuple/head/captured Product Version+Variant Sequence fences; terminal outcome read only, no reactivation |
| Unknown outcome after response loss | Same original ClientOperationId/input confirmed receipt lookup; changed bound input409; no new operation/Restore/Submit/Activate |

Log operation/item/revision/moderation IDs, transition Sequence, safe event/correlation identity, stage, outcome/latency/retry class. No activation keys, source content JSON, credentials, raw exception stack in public diagnostics. Metrics: Pending/AwaitingModeration age, blocked counts, technical retry/backlog, outbox latency, Publishing vs Delivery applied Sequence gap. Threshold/SLA/retention/code taxonomy Q15/Q20/Q17. F18 staff activity is linked audit, не замена activation/moderation history.

Global gate timeout/cancellation/failed port/owner connection loss — технический abort, не domain409 или empty valid facts. Unknown commit ACK требует inquiry/operation identity по Q15; reconnect continuation/blind retry с новым ETag не допускаются.

## 11. Тестирование

[S04](../standards/S04-testing.md)/[S14](../../../catalog/docs/standards/dotnet/testing.md#test-environment-ownership-and-bounded-parallelism): это план target verification, не отчёт о запущенных tests. Current [PublishingDeliveryIntegrationTests](../../../../contentmanager.api/tests/App/ContentManager.App.Tests.Integration/PublishingDeliveryIntegrationTests.cs) проверяют legacy isolated jobs/overlap/rollback, но direct Delivery contexts/no moderation/body Revision не target proof.

Unit SUT — state/operation roots, transition policy, FV/mappers, recovery/correlation policies; immutable own test inputs, doubles только external ports/clock. Integration SUT — Publishing real PostgreSQL/Testcontainers repositories/query adapters/UoW/outbox/inbox, реальные constraints/transactions; unique fixture scope/IDs, cleanup по project fixture, no in-memory EF substitution. Functional SUT — CM HTTP/JWT+PostgreSQL, real Api/handlers/mappers/headers; external dependencies controlled adapter doubles с явной verification boundary. E2E — реальный необходимый Publishing/F17/broker/Delivery contour и наблюдение projection/read access; несколько BC в одном API Functional не становятся E2E.

| ID | Уровень | Сценарий / DF | Ожидаемый результат |
| --- | --- | --- | --- |
| W01 | PostgreSQL Integration | Product A→B→A и Variant R1→R2→R1 после start capture | Old Approved continuation rejected by captured Version/Sequence despite same pointer |
| W02 | PostgreSQL Integration | Overlapping same-base operations/new head, manual actual transition, manual no-op, exact Cancel old item | Supersede unfinished head; actual manual cancels; no-op preserves; old Cancel does not cancel replacement |
| W03 | PostgreSQL fault integration | Crash in F10+F17 prepare, lost start/create/Submit ACK, same client ID/input vs changed input | One own tuple/atomic receipt; original confirmed result replay; changed bound input409/no new artifact |
| W04 | PostgreSQL Integration | Withdraw bound iteration then new manual iteration; duplicate Approved after terminal/manual; Resolve/Restore event | Exact item Cancelled; no reattach/reactivation/autoresume; saved Approved revision eligible for fresh manual use |
| W05 | PostgreSQL Integration | Empty Product/Variant sources, latest Retired local, never-release four-group work, later/moved member | Capture frozen; empty existing release respected; Retired/no-first-work Blocked; zero-member operation valid |
| T01 | Unit + Integration | DF02 Manual base select/Clear, source Retire race | Variant pointer/history unchanged; no selected Retired base commit |
| T02 | Unit + Integration | DF05 same-active, DF07 already-null; stale/no-op contract | No sequence/history/outbox/Version increment; chosen precondition priority |
| T03 | Integration | DF05 two competing transitions, timestamp equal, commit fail | One protected result; unique Sequence; atomic state/history/outbox, no partial writes |
| T04 | Functional | DF01–07 ETag/If-Match binding/headers/result | Own int token not DTO; missing428/stale412; response matches own commit; no shared workspace token |
| T05 | Unit + Functional | DF05/06 wrong Variant/never Approved/Retired/Withdrawn/F15; valid historical R1 | Forbidden blocked; Approved R1 rollback new sequence/reason, old artifact/history immutable |
| T06 | Unit + Integration | DF10 first no-release/no-work; local-work blocked; published patch+draft | Product-only first valid; draft blocks first; saved release reused with full F10 validation |
| T07 | Integration | DF10 crash before/after revision+Pending+item tuple commit, repeated same-item claim | All rows or none; exact own tuple retry; no orphan/moderation adoption |
| T08 | Integration + Functional | DF09–12 partial batch 9/10, Pending/Rejected/Approved | Independent progress; waiting not active; Rejected keeps old pointer; Approved exact item activates |
| T09 | Integration | DF11/12 duplicate/reverse iteration/late approval/manual/unpublish/new operation | Matched result only; successful result not reapplied after manual action; exact head/ProductVersion/VariantSequence fences enforced |
| T10 | Integration | Concurrent different items/own item Version/intent + first-state create | No operation-wide token bump/lost progress; immutable membership; independent safe item retry |
| T11 | Integration | Activate vs Retire/Withdraw, R1 Withdrawal vs concurrent R2 | No active inadmissible target; never clear unrelated R2; ban persisted for inactive ID too |
| T12 | E2E | DF13 outage/consumer crash/replay/reverse Sequence/incomplete composition | Old effective until full R2 commit; later null not reverted; exact withdrawal ban not lifted |
| T13 | Functional + E2E | Scope move/history/errors, combined effective token, unknown outcome recovery | No silent parent mutation; history retained; accurate active/effective distinction; safe reconciliation |
| T14 | Architecture | Commands/queries/DbContext/DI/current controller separation | Command Application/query Persistence; no foreign DB; one mutation path; NSwag target surface only after OpenAPI |

Для races deterministic barriers/gates управляют моментами capture/commit/decision, не arbitrary sleeps. PostgreSQL timestamp precision учитывается fixture conventions; Sequence assertions точные. Test data содержит Product/Variant hierarchy, approved/unapproved iterations, explicit exact F09 sources/F08 work, multiple history rows и delayed events. Cleanup удаляет собственные fixture данные/inbox/outbox/broker messages, не runtime business history. Любой port double test не доказывает real transport или cross-BC guarded facts.

| ID / DF | Уровень / SUT | Исходные данные / сценарий | Ожидаемый результат |
| --- | --- | --- | --- |
| G01 / guarded write flows | PostgreSQL Integration, separate owner connections/CM replicas | Pause writer after gate; concurrent relevant writer with same stable key | Second writer waits; first commits own BC only; second loads fresh committed facts, no forbidden stale commit |
| G02 / guarded write flows | PostgreSQL Integration + architecture/DI | Independent public primary reads while owner holds gate; foreign UoW enlistment/shared multi-DbContext participant | Reads see committed facts after acquisition; no foreign EF/transaction participant/cache/replica; own writes+outbox/audit intent/inbox atomic |
| G03 / guarded write flows | Fault integration | Bounded acquisition cancellation/timeout, failed read, owner connection loss, lost commit ACK | Technical abort/unknown outcome; no fake compatible empty facts or reconnect continuation; original own412 and independent basis409 preserved |

## 12. Порядок реализации

1. Реализовать approved frozen capture/head/ABA/atomic prepare и confirmed receipt; уточнить Q15 wire/read token/URI/status/privacy и Q20 lease/codec/DI/mapping, Q17 Delivery readiness.
2. Domain state/Sequence/history/operation + named constraints; Application.Abstractions commands/queries/DTO/ports; reuse F09/F10/F17 exact targets.
3. EntityFramework mappings/constraints/migrations и Persistence repositories/query projections/DI; real PostgreSQL atomicity/concurrency checks.
4. Application manual base/Activate/Rollback/Unpublish and F10 conditional Withdrawal coordination; Api scoped ETag/If-Match/OpenAPI; one accepted path replacing legacy combined mutation semantics.
5. Atomic F10+F17 prepare/exact tuple; independent item/head continuation/cancel/supersession and technical retry; real PG ABA/overlap/lost-ACK tests.
6. Delivery outbox consumer/full projection/null/terminal ban integration и observability; real necessary E2E contour.
7. После confirmed backend contract regenerate NSwag и UI screens/actions/progress; verify state metadata/unknown-result recovery без browser hidden mandatory post-mutation GET.

## 13. Открытые технические вопросы

- [Q15](../questions/Q15-api-and-source-code.md#f11-publication-state-и-automatic-promotion): final routes/body/status/permissions/comments/readiness representation, scoped state ETag/result carrier, initial singleton/no-op priority, paging, retry/recovery и NSwag.
- [Q20](../questions/Q20-persistence-and-migration.md#f11-state-transitions-и-promotion-coordination): approved capture/head/ABA/atomic prepare/receipt см. [workflow](../questions/Q20-persistence-and-migration.md#publishing-workflow-capture-и-продолжение); остаются lease/retry numbers/codec/DI/migrations.
- [Q17](../questions/Q17-publication-availability.md#f11-publishing-active-и-delivery-effective): full projection readiness/effective ordering/null/terminal restriction/in-flight availability и observed confirmations.
- [Q19](../questions/Q19-content-hashes.md): exact artifact canonical representation/integrity contract inherited F10/F13; F11 не выбирает иной hash.

Approved-only activation, async Delivery, per-Variant moderation, append-only history, no-op same-active/already-null и first base-only/no-first-local-draft rules уже приняты; их не возвращать в TBD.
