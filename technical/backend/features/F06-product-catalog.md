# F06. Каталог Product / ProductVariant: бэкенд

[Бизнесовая спецификация](../../../business/features/F06-product-catalog.md) · [Модуль M03](../modules/M03-catalog.md) · [Фронтенд](../../frontend/features/F06-product-catalog.md) · [Q23](../questions/Q23-source-synchronization.md#f06-проекция-pending-и-конкурентная-обработка) · [Q15](../questions/Q15-api-and-source-code.md#f06-детализация-read-контракта).

## 1. Область реализации

F06 относится к MVP. CatalogAccess принимает полный source snapshot Product/ProductVariant из VR School, сохраняет локальную проекцию и предоставляет каталог для интерфейса, Publishing и Delivery. Demo не зависит от этой фичи, шины или синхронизации VR School.

VR School владеет созданием, названиями, отношением Variant → Product и source lifecycle. CM сохраняет стабильные source IDs, не создаёт копии по организациям/лицензиям и не предоставляет source CRUD, rename, Delete, Archive или Restore. `Active`/`Archived` — согласованный нормализованный lifecycle; изменение приходит только полным более новым source snapshot. `Hidden` обозначает только presentation/filter в UI, а Pending/Conflict — отдельную ось обработки, не lifecycle. Каталог не читается из чужой БД и не поступает через Project Storage; у CM независимая подписка.

Входящие use cases: применение Product snapshot; применение Variant snapshot, включая отсутствие parent и перенос; повторное разрешение pending relation; read-only list/exact read; MVP reconciliation повторной публикацией current snapshots. Bootstrap, восстановление всех промежуточных ревизий и бесконечный архив событий в целевой модели отсутствуют. F15 владеет ручным разрешением конфликтов, F07–F11 — назначениями/публикацией, F12 — entitlement, F13 — Delivery. Каталожная принадлежность сама по себе не выдаёт grant всем текущим и будущим Variant.

**Граница исходника и цели.** Физические слои находятся в `src/BoundedContexts/CatalogAccess/ContentManager.CatalogAccess.*`; отдельный CatalogAccess.Application предстоит добавить. Сейчас обработка находится в [App Integration](../../../../contentmanager.api/src/App/ContentManager.Application/Integration/CatalogSnapshotProcessor.cs), read queries — в [CatalogQueryHandler](../../../../contentmanager.api/src/BoundedContexts/CatalogAccess/ContentManager.CatalogAccess.EntityFramework.Persistence/Catalog/CatalogQueryHandler.cs). Текущие SourceRevision: long, обязательный parent FK, запрет move и replay истории не доказывают реализацию описанной ниже цели. Подробная сверка и незакрытые механизмы — [Q23](../questions/Q23-source-synchronization.md#f06-проекция-pending-и-конкурентная-обработка). Новые имена типов ниже являются целевыми contracts, а не утверждением о наличии файлов.

## 2. Domain

### 2.1. Aggregates, entities и value objects

Domain не зависит от MassTransit contracts, EF, HTTP DTO или Publishing persistence. Ниже выбран single-payload ownership и двухстадийный receive/work протокол; physical mappings/codecs remain engineering Q20. Domain receives owned normalized values, never broker/EF contracts.

| Тип | Роль | Владелец / граница |
| --- | --- | --- |
| `ProductId`, `ProductVariantId` | Strong IDs | CatalogAccess; ссылки в других BC только по ID |
| `SourceCatalogLifecycle` | Selected normalized enum Active/Archived; V2 explicit Lifecycle, V1 compatibility adapter checked; no local Archive | Domain; input от source adapter |
| `ProductSnapshot` | Immutable parameter/value object полного состояния | Domain, нормализованный вход |
| `ProductVariantSnapshot` | Immutable parameter/value object полного состояния | Domain, нормализованный вход |
| `Product` | Projection aggregate root | CatalogAccess; source metadata изменяется только snapshot |
| `AppliedProductVariantState` | Immutable applied value | ProductVariant, последнее успешно применённое состояние |
| `ProductVariant` | Projection aggregate root | CatalogAccess; identity существует и до разрешения parent |
| Ожидающее состояние Variant | Typed full source state, не новый source object | CatalogAccess; одна актуальная pending target, не очередь всех версий |
| `CatalogConflict` | Root F15; точная модель/поля только в [F15](F15-catalog-conflicts.md) | CatalogAccess; открывается/обновляется вместе с F06 mutation |

**ProductId**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Value | `Guid` | Да | Stable source identity VR School | Non-empty, immutable, never reused; foreign BC ссылки только по ID |

**ProductVariantId**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Value | `Guid` | Да | Stable source Variant identity | Non-empty, immutable, never reused |

**ProductSnapshot**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | `ProductId` | Да | Source identity | Не CM-generated |
| Name | `string` | Да | Полное source name | Source-normalized Product Name1–120; CM validates/preserves, not unique identity |
| Description | `string?` | Нет | Full source Description | Max500; null/empty allowed, source normalizes/CM preserves |
| SourceLifecycle | `SourceCatalogLifecycle` | Да | Normalized source lifecycle | Active/Archived; V2 explicit lifecycle, V1 checked compatibility; не local Archive |
| SourceVersion | `int` | Да | Per-entity source freshness | Positive monotonic; wire long migration/range Q23; gaps допустимы |
| OccurredAt | `DateTimeOffset` | Да | Source audit time | Не порядок и не processed time |

**ProductVariantSnapshot**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductVariantId | `ProductVariantId` | Да | Source stable Variant identity | Immutable |
| ProductId | `ProductId` | Да | Source target parent | Может отсутствовать локально; no placeholder/FK requirement |
| Name | `string` | Да | Source Variant name | Source-normalized Variant Name1–200; CM validates/preserves, not Key/identity |
| Description | `string?` | Нет | Full source Description | Max500; null/empty allowed, source normalizes/CM preserves |
| SourceLifecycle | `SourceCatalogLifecycle` | Да | Normalized source lifecycle | Active/Archived; CLR/wire и IsDeleted mapping Q23 |
| SourceVersion | `int` | Да | Per-Variant source version | Positive monotonic; freshness по latest observed; Q23 wire migration |
| OccurredAt | `DateTimeOffset` | Да | Source audit | Не порядок |

**Product**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | `ProductId` | Да | Stable source identity | Source metadata только snapshot; ValueGeneratedNever |
| Name | `string` | Да | Applied source name | Domain constraints, non-unique; normalization Q23 |
| Description | `string?` | Нет | Applied full source Description | Max500; no local fallback |
| SourceLifecycle | `SourceCatalogLifecycle` | Да | Applied source lifecycle | Active/Archived; guard нового использования §2.3; identity/history не удаляются; V2 selected enum; wire projection Q15 |
| SourceVersion | `int` | Да | Applied source freshness | Positive monotonic; не local concurrency |
| SourceOccurredAt | `DateTimeOffset` | Да | Applied source audit | Не порядок |
| Version | `int` | Да | Internal local concurrency token | Не public DTO/ETag F06; guards consumer/resolver |

**AppliedProductVariantState**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | `ProductId` | Да | Последний applied parent | Заменяется вместе со всем applied group |
| Name | `string` | Да | Applied source name | Не pending label; Domain Name constraints |
| Description | `string?` | Нет | Applied Variant Description | Max500; same full applied group |
| SourceLifecycle | `SourceCatalogLifecycle` | Да | Applied lifecycle | Active/Archived; own Variant lifecycle не наследует parent Archive; CLR/wire Q23 |
| SourceVersion | `int` | Да | Applied version | Может отставать latest observed; до first apply отсутствует вместе с group |
| SourceOccurredAt | `DateTimeOffset` | Да | Applied source audit | Не ordering/processed time |

**ProductVariant**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | `ProductVariantId` | Да | Durable stable identity | Существует до разрешения parent; move не reset |
| AppliedState | `AppliedProductVariantState?` | Нет | Последнее полностью applied состояние | Null до first apply; никаких mixed old/new fields |
| LatestObservedSourceVersion | `int` | Да | Read value derived from CatalogSourceState.LatestAcceptedVersion | Source state is authoritative watermark; not a second independently mutable freshness owner |
| UnresolvedState | `UnresolvedProductVariantState?` | Условно | Single newest target only while no Open conflict owner | Missing parent; Open payload belongs F15.PendingState, handoff atomic |
| Version | `int` | Да | Internal local concurrency | Не public DTO; atomic pending/watermark/F15 |

**UnresolvedProductVariantState (own Variant payload while parent missing)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Snapshot | ProductVariantSnapshot | Да | One newest normalized full target | Transferred from receive-state, never independent duplicate F15 target |
| RequiredParentId | ProductId | Да | Exact missing source parent | Durable wakeup/recovery key |
| WaitingSince | DateTimeOffset | Да | CM waiting observation | No TTL disappearance/dead-letter solely missing parent |

Catalog receive/work state is technical CatalogAccess-owned state; fields below are defined only here and reused by F15. Receipt immutable evidence is not a second mutable target. Each entity has ONE authoritative latest accepted candidate payload; transfer between receive state → unresolved Variant → Open CatalogConflict.PendingState is atomic. Work references ID/version, never another independently editable payload.

**CatalogSourceState (own mutable receive-state)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| EntityKind | CatalogEntityKind | Да | Product or ProductVariant logical enum | Own composite identity |
| EntityId | Guid | Да | Stable source entity | No invented ID for malformed envelope |
| LatestAcceptedVersion | int? | Нет | Highest valid accepted positive source version | Durable stale guard after receipt/work cleanup |
| CurrentStateEvidence | ReadOnlyMemory<byte> | Условно | Versioned immutable normalized source-state equality evidence | ID/version/parent/name/description/lifecycle, not event clock; canonical codec Q20; no apply authority |
| CandidateOwner | CatalogCandidateOwner | Да | ReceiveState/UnresolvedVariant/OpenConflict/None logical enum | Single payload owner, atomic handoff |
| ReceiveCandidate | CatalogSnapshotCandidate? | Условно | Newest valid payload before classification | Only when owner ReceiveState; cleared on handoff/apply |
| OpenConflictId | Guid? | Условно | Exact F15 current payload owner | Reference only; no second target |
| Version | int | Да | Own mutable concurrency | Internal, no DTO |

**CatalogSnapshotCandidate (immutable owned normalized union)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Product | ProductSnapshot? | Условно | Normalized Product candidate | Exactly one according EntityKind |
| Variant | ProductVariantSnapshot? | Условно | Normalized Variant candidate | Exactly one; full source state |

**CatalogEventReceipt (own technical receipt)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| EventId | Guid | Да | Usable nonempty original admitted source EventId only | Unique receipt only for usable ID; malformed/missing ID uses own durable quarantine/fault, never fabricated receipt |
| ContractVersion | int | Да | Original versioned profile | No silent V1 rename |
| EntityKind | CatalogEntityKind? | Нет | Reliably admitted catalog kind | Unknown/malformed does not create entity block |
| EntityId | Guid? | Нет | Reliably admitted source ID | No fake ID |
| SourceVersion | int? | Нет | Reliably admitted positive source version | Nullable invalid header; raw V1 checked adapter |
| ImmutableEventEvidence | ReadOnlyMemory<byte> | Да | Preserved admitted immutable envelope identity and source event/rejected-wire evidence | Includes reliable identity/version/OccurredAt and original immutable payload evidence, even unsupported profile; no pretend valid normalization, retry headers excluded; private codec Q20/no raw log |
| Disposition | CatalogReceiptDisposition | Да | Accepted/StateRepeat/Stale/ScopedFault/Quarantined logical result | Ordinary same event returns original; explicit unchanged unsupported-profile replay checked before terminal replay may advance private disposition after repair, original evidence retained |
| IngestionFaultId | Guid? | Нет | Exact scoped fault if applicable | Distinct from F15 |
| AcceptedAt | DateTimeOffset? | Условно | CM durable acceptance observation | Not applied/remote clock/physical commit promise |
| Version | int | Да | Own receipt recovery transition token | Immutable event evidence never changes under EventId |

**CatalogWorkItem (own durable work reference)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | Guid | Да | Server work identity | Not new source EventId |
| EntityKind | CatalogEntityKind | Да | Target kind | Own scheduling scope |
| EntityId | Guid | Да | Exact target identity | No payload copy |
| CandidateVersion | int | Да | Referenced accepted version | Worker must reread latest; accepted22 defeats work21 |
| Status | CatalogWorkStatus | Да | Ready/Leased/WaitingParent/WaitingFault/Completed logical enum | Technical lease != source lifecycle/conflict status |
| RequiredParentId | Guid? | Условно | Missing parent durable wakeup key | No fake Product/TTL deletion |
| LeaseOwner | Guid? | Условно | Current worker attempt identity | Conditional reclaim/fencing profile Q20 |
| LeaseUntil | DateTimeOffset? | Условно | Bounded technical lease deadline | Expired work reclaim/recovery scan; numeric defaults engineering |
| CompletedAt | DateTimeOffset? | Условно | Terminal processing observation | Not ACK/admission/source-time proof |
| Version | int | Да | Own mutable work concurrency | Late obsolete lease cannot complete newer work |

**CatalogIngestionFault (own scoped readiness state)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | Guid | Да | Server fault identity | Not business conflict/Withdrawal |
| EventId | Guid? | Условно | Original usable nonempty source EventId if admitted | Missing/malformed ID never replaced with fake source ID; own FaultId separate |
| RejectedEventEvidence | ReadOnlyMemory<byte> | Да | Immutable private admitted envelope/rejected wire evidence | Supports fault without source EventId; private codec Q20, no public raw payload/logs |
| EntityKind | CatalogEntityKind | Да | Reliably admitted kind | Unknown entity/version no invented scope |
| EntityId | Guid | Да | Reliably admitted entity | Only affected source scope |
| SourceVersion | int | Да | Reliably admitted higher positive version | Separate barrier from valid accepted watermark |
| Kind | CatalogIngestionFaultKind | Да | InvalidSnapshot or UnsupportedContract logical enum | Transient/missingparent/F15 incompatibility distinct |
| DetectedAt | DateTimeOffset | Да | CM detection observation UTC | Not source order |
| ClearedAt | DateTimeOffset? | Нет | Confirmed fault recovery observation | Newer valid source fix, or unchanged original unsupported replay after profile repair |
| Version | int | Да | Own fault concurrency | Public guarded readiness consults active scope, no blanket global block |

CatalogSourceState valid watermark and CatalogIngestionFault barrier are distinct. A reliably admitted invalid/unsupported higher source state cannot silently leave old candidate applicable; affected new writes fail readiness. Unknown entity/version does not create a speculative entity block. Missing/malformed EventId with reliable entity+higher version still has scoped fault/private evidence, no fake EventId/receipt. Transient dependency/storage failure, missing parent and real F15 incompatibility remain separate branches.



`ProductVariant.SourceVersion` означает `AppliedState.SourceVersion`; до первого apply applied version отсутствует, а не равна fictitious source version. `LatestObservedSourceVersion` — watermark последнего принятого более свежего source snapshot, включая pending. Эти значения могут различаться: applied v18, pending v21. Input v20 не должен переписать pending v21 даже при applied v18. Product also has separate technical accepted candidate/watermark before worker apply; this is not a business missing-parent Product placeholder.

При первом Variant snapshot без parent `AppliedState` отсутствует. При move к отсутствующему parent либо конфликте уже применённое состояние сохраняется целиком. UI/Publishing не получают новый ProductId с прежним Name или наоборот. Pending payload при открытом F15 conflict принадлежит его `PendingState`; при одном лишь отсутствующем parent хранится как unresolved state CatalogAccess. Watermark, текущий payload и conflict обновляются атомарно; две независимо изменяемые копии pending target недопустимы. Single payload handoff is selected: receive-state → unresolved Variant → Open F15.PendingState, atomically with watermark/receipts/work; work holds refs/version only.

V2 Description string? max500 (null/empty allowed) is selected for normalized/applied state; no local description fallback. Product Name1–120, Variant Name1–200; source normalizes before source commit, CM validates and preserves. V1 long/IsDeleted conversion must be explicit checked range/continuity, not implicit rename; selected V2 source contracts §8.1 once, F15 reuses normalized values.

### 2.2. Методы и переходы

| Тип | Метод | Параметры | Предусловия | Изменение / результат |
| --- | --- | --- | --- | --- |
| `Product` | `CreateFromSnapshot` | `ProductSnapshot` | ID/fields/version валидны; identity отсутствует | Root с source ID и applied snapshot |
| `Product` | `ApplySnapshot` | `ProductSnapshot` | Тот же ID; incoming version больше applied | Полное замещение source metadata; lifecycle сохраняет identity |
| `ProductVariant` | `ObserveSnapshot` | `ProductVariantSnapshot` | Тот же ID; incoming больше latest observed | Watermark и newest candidate; старое AppliedState не меняется до успешного apply |
| `ProductVariant` | `KeepPendingParent` | Актуальный snapshot | Parent локально отсутствует; candidate ещё newest | Durable unresolved relation; AppliedState сохранён либо отсутствует |
| `ProductVariant` / F15 | `KeepConflictingTarget` | Актуальный snapshot, typed move impact | Parent найден; перенос несовместим с локальными зависимостями | Один open conflict, newest target; applied metadata прежние |
| `ProductVariant` | `ApplyObservedSnapshot` | Актуальный snapshot, результат проверки relation/impact | Candidate совпадает с latest observed; parent найден; необходимые guards подтверждены | Всё AppliedState заменяется; pending завершается согласованно с F15 |
| Lifecycle policy | Проверка нового использования | Applied lifecycle, наличие parent, pending/conflict, вид операции | Достоверные CatalogAccess facts | Archive/new-use/maintenance guards §2.3 и F15; не revoke/history mutation |

Equal source STATE/equalVersion new EventId and stale lower watermark are normal no-op; contradictory equalVersion or mutated sameEventId quarantine. Внутреннее повторное разрешение уже сохранённого pending snapshot является отдельным действием: source version равна watermark намеренно, но candidate должен оставаться актуальным до commit. Нельзя повторно пропускать такой candidate через проверку «только incoming > observed» и навсегда оставлять его pending.

### 2.3. Инварианты и constraints

1. IDs immutable и never reused; новый source ID создаёт отдельную projection, а не переименовывает старую.
2. Source version — положительный монотонный `int`; локальный `Version: int` защищает конкурентную запись и не заменяет source ordering. `OccurredAt` — аудит, не порядок.
3. Full snapshot v21 применяется поверх v18 без чтения v19/v20. Freshness Variant проверяется по latest observed, а не только applied.
4. Normal duplicate/stale не меняет applied/pending/conflict и не повторяет последствия. `EventId` отдельно дедуплицирует transport retry.
5. Parent missing — допустимое ожидание: нет placeholder Product, TTL удаления или DLQ за сам факт отсутствия parent.
6. Safe move сохраняет VariantId и локальное CM state; конфликт сохраняет прежнее applied состояние и newest target. Исторические releases/revisions не переписываются.
7. Active/Archived source lifecycle не удаляет projection. Product Archive ограничивает новые операции всех принадлежащих Variant, не изменяя их собственный lifecycle; Variant Archive ограничивает сам Variant. Restore Product допускает новое использование Variant лишь при own Active и отсутствии других blockers. Hidden не меняет guards; Pending/Conflict оцениваются отдельно. CM не архивирует/восстанавливает источник локальной командой. V2 carries explicit Active/Archived; V1 IsDeleted mapping belongs checked compatibility adapter, no silent contract reinterpretation.
8. Grants F12 остаются facts по конкретным Variant IDs; один Variant может использоваться разными организациями/лицензиями.

Archive сохраняет reads/историю и допускает ограниченное обслуживание mutable working state: retain unchanged, удаление и замену уже проблемного reference допустимым результатом без расширения состава. Добавление новых позиций, расширение состава, новый release/revision, Activate/Rollback и Automatic promotion запрещены. Если одновременно есть Open Conflict, ordinary Save остаётся запрещён: допустима только отдельная corrective repair операция [F15 DF07](F15-catalog-conflicts.md#df07-local-repair-boundary), в пределах её scope/basis и правил обслуживания. Архивирование не выдаёт обхода intrinsic validation, reference eligibility или permissions.

Archive не вызывает Unpublish, Withdrawal или revoke, не очищает active pointers и не меняет immutable release/revision. Уже materialized revision и подтверждённый entitlement остаются пригодными для Delivery, в том числе при создании новой session, при обычных независимых guards F12/F13. Explicit safety Unpublish/Withdrawal разрешены с обычными permissions/preconditions. Restore не активирует revision, не снимает Withdrawal, не возвращает grant и не закрывает Conflict автоматически.

[S21](../../../catalog/docs/standards/architecture/ddd.md#aggregate-owned-rule-definitions): источник ограничений — Domain constraints. Dated existing [ProductConstraints](../../../../contentmanager.api/src/BoundedContexts/CatalogAccess/ContentManager.CatalogAccess.Domain/Products/Constraints/ProductConstraints.cs) и [ProductVariantConstraints](../../../../contentmanager.api/src/BoundedContexts/CatalogAccess/ContentManager.CatalogAccess.Domain/ProductVariants/Constraints/ProductVariantConstraints.cs) currently define Name1–255 as source-code evidence, not selected target limits. Target Domain/FV/EF source-profile rules are Product1–120/Variant1–200/Description<=500; producer normalizes before commit and CM preserves; no name uniqueness. Name не является identity и не получает уникальность без source гарантии; нынешний unique `(ProductId, Name)` не выводится из требований.

### 2.4. Domain events

Обязательное внешнее событие CM для F06 не выбрано. VR School snapshots являются integration input, не domain events CM. Не создавать событие для каждого read/no-op или ради логирования. Повторное разрешение pending после Product commit требует надёжного внутреннего запуска; persisted CatalogWorkItem + atomic parent commit wakeup and recovery scan are mandatory; numeric lease/profile implementation Q20. Если применяется domain event, его обработка должна сохранять recoverable work, а не обещать доставку одним in-memory callback.

## 3. Application.Abstractions

### 3.1. Commands

| Command / use case | Result | Concurrency |
| --- | --- | --- |
| ReceiveProductSnapshotCommand / ReceiveProductVariantSnapshotCommand | Durable acceptance receipt, NOT apply | FIRST own gate/fresh EventId/source state/fault |
| ProcessCatalogWorkCommand | Applied/WaitingParent/OpenConflict/Superseded/technical outcome | NEW first gate/latestaccepted + fault/lease/root/dependencies |
| ResolvePendingProductVariants | Internal wakeup of durable work | Re-read newest; no external fresh event required |

**ReceiveProductSnapshotCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Event | ProductSnapshotV2 | Да | Trusted full versioned source input | Pure adapter validation then first own gate; model once §8.1 |

**ReceiveProductVariantSnapshotCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Event | ProductVariantSnapshotV2 | Да | Trusted full variant input | No ACK before acceptance commit |

**ProcessCatalogWorkCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| WorkItemId | Guid | Да | Exact own durable work | No second snapshot body |
| LeaseOwner | Guid | Да | Current worker attempt | Current claim token AND bounded deadline checked under gate; lease not business permit |

**ResolvePendingProductVariants**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | ProductId? | Условно | Parent committed wakeup scope | Or exact Variant; internal scheduling |
| ProductVariantId | ProductVariantId? | Условно | Exact waiting Variant | Reread newest payload/fault; no external event required |

Invalid admitted decoder result remains private, never fabricated valid V2 DTO. ApplyProductSnapshot/ApplyProductVariantSnapshot are internal worker stages, no alternative synchronous broker-consumer apply path.

### 3.2. Queries

| Query | Result |
| --- | --- |
| `ListProductsQuery` | `OffsetPagedResultDto<ProductListItemDto>` |
| `GetProductQuery` | `ProductDto` или not found |
| `ListProductVariantsQuery` | `OffsetPagedResultDto<ProductVariantListItemDto>` |
| `GetProductVariantQuery` | `ProductVariantDto` или not found |

**ListProductsQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| List parameters | Уточняется | Уточняется | Catalog list parameters §3.3 | Filters/default/sort profile Q15; не новый дублируемый model |

**GetProductQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | `Guid` | Да | Exact stable source ID | Not found только missing projection |

**ListProductVariantsQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| List parameters | Уточняется | Уточняется | Общие Catalog list parameters §3.3 | Q15 |
| ProductId | `Guid?` | Нет | Applied parent filter | Не pending relation/source mutation |

**GetProductVariantQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductVariantId | `Guid` | Да | Exact stable Variant ID | Existing no-applied state не fake404 |

Queries не запускают синхронизацию и не исправляют pending. Public inbound catalog read/operation-block ports предоставляют Publishing/Delivery необходимые facts без передачи EF entities. Query handlers находятся в EntityFramework.Persistence по S20.

### 3.3. DTO и parameter models

Ниже минимальное смысловое read contract, а не утверждённая generated signature; exact shape/property names and filter enums и permission profile — [Q15](../questions/Q15-api-and-source-code.md#f06-детализация-read-контракта).

| Model | Назначение |
| --- | --- |
| Product list/exact | Минимальное смысловое read contract; exact shape Q15 |
| Variant list/exact | Минимальное смысловое read contract; exact shape Q15 |
| Variant diagnostic state | Минимальное смысловое read contract; exact shape Q15 |
| Catalog list parameters | Минимальное смысловое read contract; exact shape Q15 |

**Product list/exact (ProductListItemDto / ProductDto; exact shape Q15)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | `Guid` | Да | Stable source identity | Local Version отсутствует |
| Name | `string?` | Условно | Applied Product source name only after worker apply | No fabricated label before apply;1–120 |
| Description | `string?` | Нет | Applied full Description | <=500 null/empty; no local fallback |
| HasAppliedState | `bool` | Да | Product worker-applied projection exists | Accepted durable source identity may exist before Product root |
| Processing | `CatalogProcessingRead` | Да | Current typed processing/fault/assessment diagnostics | Independent processing axis, no Version/permit |
| SourceLifecycle | `SourceCatalogLifecycle?` | Условно | Applied Product lifecycle | Null wholly unapplied; Active/Archived selected, wire codec Q15 |
| Допустимость нового использования и причина (имена уточняются) | Уточняется | Уточняется | Read-only eligibility | Q15; Archive guard §2.3, не grant всем Variant |
| Variant/conflict summary (имена уточняются) | Уточняется | Уточняется | Связанные summaries при потребности экрана | Только если утверждено Q15 |

**Variant list/exact (ProductVariantListItemDto / ProductVariantDto; exact shape Q15)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | `Guid` | Да | Durable stable Variant identity | Readable до first apply |
| ProductId | `Guid?` | Условно | Applied parent | Нет до first apply; pending parent отдельно |
| Name | `string?` | Условно | Applied source label | Not pending label; null no AppliedState;1–200 |
| Description | `string?` | Нет | Applied full Variant Description | <=500 null/empty, same whole applied group |
| Processing | `CatalogProcessingRead` | Да | Current source/target/fault/assessment diagnostics | Not lifecycle/permission |
| Lifecycle (имя уточняется) | `SourceCatalogLifecycle?` | Условно | Applied normalized lifecycle | Only AppliedState; Active/Archived, wire casing Q15 |
| Признак applied состояния (имя уточняется) | Уточняется | Уточняется | Отделяет no-applied от missing identity | Q15 |
| Parent/pending/conflict summary (имена уточняются) | Уточняется | Уточняется | Read-only relation/diagnostics | Shape Q15; search не смешивает applied и pending |
| Допустимость нового использования (имя уточняется) | Уточняется | Уточняется | Eligibility | Q15; own/parent lifecycle + pending/conflict §2.3; no-applied не picker candidate |

**CatalogProcessingRead (immutable nontracking public owner value)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Status | CatalogProcessingStatus | Да | Applied/Received/WaitingParent/OpenConflict/AwaitingAssessment/NotReadyFault logical states | Not new source lifecycle; wire codec Q15 |
| LatestAcceptedVersion | int? | Нет | Current durable valid accepted watermark | Positive; persists after receipt cleanup |
| AppliedSourceVersion | int? | Нет | Current wholly applied source version | Null before first worker apply |
| PendingSourceVersion | int? | Нет | Current accepted target awaiting apply | Not stale old target |
| RequiredParentId | Guid? | Условно | Exact waiting target parent | No fake parent |
| OpenConflictId | Guid? | Условно | Exact Open F15 episode | Root models only F15 |
| AssessedPendingSourceVersion | int? | Условно | Version bound to last F15 Issues | Old assessment may remain visible but not current proof |
| AssessmentCurrent | bool | Да | Assessment matches pending/latest and no blocking fault | Advisory readiness; writers fresh-read dependencies |
| Fault | CatalogFaultRead? | Нет | Active scoped ingestion unreadiness | Distinct transient/wait/F15, not Withdrawal/globalblock |

**CatalogFaultRead (immutable nontracking safe projection)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| FaultId | Guid | Да | Exact own fault identity | Not conflict/user mutable target |
| SourceVersion | int | Да | Reliably admitted offending higher source version | Separate validaccepted watermark |
| Kind | CatalogIngestionFaultKind | Да | InvalidSnapshot/UnsupportedContract | Known scope only |
| DetectedAt | DateTimeOffset | Да | CM detection observation UTC | Not source freshness |
| ClearedAt | DateTimeOffset? | Нет | Own recovery observation | Current read usually active fault |
| IsActive | bool | Да | Uncleared current blocking scope | No raw rejected payload/provider/Version |

**Catalog list parameters**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Search | `string?` | Нет | Server search | Exact semantics Q15; no mixed applied/pending name |
| Offset | `int` | Уточняется | Offset pagination | Values/defaults/границы Q15 |
| MaxResultCount | `int` | Уточняется | Page size | Values/defaults/границы Q15 |
| Sort key (имя уточняется) | Уточняется | Уточняется | Whitelist order | Q15; stable Id tie-breaker |
| Sort direction (имя уточняется) | Уточняется | Уточняется | Whitelist direction | Q15 |
| ProductId | `Guid?` | Нет | Только Variant applied-parent filter | Не pending target |
| Lifecycle/availability filters (имена уточняются) | Уточняется | Уточняется | Утверждённые read/picker filters | Q15; Active/Archived отдельно Hidden presentation и Pending/Conflict processing; wire/filter profile ещё не выбран |

Технический local `Version` не входит ни в один публичный DTO. Description/processing fields selected above; wholly unapplied Product/Variant distinguished from missing identity; new-use picker excludes unapplied. SourceVersion — отдельная source диагностика, не ETag; в обычной таблице его показ не обязателен. Exact Variant без applied состояния не маскируется под 404, если durable identity существует; read representation показывает ожидание, а picker новых назначений такой объект не предлагает. Список под Product фильтруется по applied parent; ожидающая source relation отображается отдельной диагностикой. Поиск никогда не сливает старые applied и новые pending поля в один якобы действующий объект.

### 3.4. Ports

| Port | Назначение | Направление |
| --- | --- | --- |
| `IProductRepository`, `IProductVariantRepository` | Tracked roots по stable ID, сохранение в local UoW; не repository на каждый snapshot field | Outbound в Persistence |
| F15 conflict repository/service contract | Open/update conflict с newest candidate в той же CatalogAccess UoW | Внутри BC; точные signatures F15/Q23 |
| `IProductVariantMoveImpactReader` | Читать typed consequences по VariantId, applied/target ProductId и версиям local dependencies | Outbound к публичным Publishing contracts; без чужого DbContext |
| Catalog read port | Applied catalog facts/eligibility для Publishing/Delivery | Inbound CatalogAccess, implementation Persistence |
| `ICatalogOperationBlockReader` | Block конкретной затронутой операции открытым F15 conflict | Inbound CatalogAccess; scope/guards F15/Q25 |
| Inbox / pending-work boundary | EventId tracking и durable trigger повторного разрешения parent | Adapter к существующей LeanSoft инфраструктуре; точная transactional enlistment Q23 |

Port возвращает immutable facts/impact, не Publishing root для сохранения. Чтение чужого BC не обеспечивает неизменность этих facts до CatalogAccess commit: протокол конкурирующих move и authoring описывается Q23/F15, не скрывается в mapper.

MVP coordination port принадлежит Application.Abstractions: задаёт границу guarded owner mutation, cancellation и outcome, без PostgreSQL/Npgsql/EF типов в Domain. Точные CLR-имена и signatures остаются Q15. Application открывает owning UoW и требует guard до authoritative reads; EntityFramework.Persistence выполняет standalone pg_advisory_xact_lock на той же physical connection и transaction владельца. Public foreign read adapters используют независимые primary read scopes/connections без ambient UoW enlistment. Host/DI отделяет owning UoW от этих scopes; существующий [EfCoreUnitOfWorkParticipant](../../../../contentmanager.api/src/BuildingBlocks/ContentManager.EntityFramework.UnitOfWork/EfCoreUnitOfWorkParticipant.cs), способный enlist несколько contexts в общую SQL transaction, не выполняет эту целевую границу автоматически. Domain получает уже проверенные предметные facts и не содержит SQL guard type.

### 3.5. Specifications / read contracts

[S19](../../../catalog/docs/standards/dotnet/ardalis-specification.md): атомарные builder conditions для ProductId, name search, lifecycle/eligibility и наличия unresolved state переиспользуются List/count/selector. `ProductsReadSpecification`, `ProductVariantsReadSpecification` и exact-ID specifications собирают условия в builder; не использовать выдуманный `spec.And` для объединения готовых specs.

List применяет фильтры на сервере, затем whitelist sort с обязательным stable Id tie-breaker, count и offset pagination; проецирует сразу read DTO без tracked aggregate graph. Фактические sort keys/default/limits и согласованность count/page — Q15. Query cancellation передаётся до EF. Обычное list чтение не открывает mutation UoW, не сохраняет projection и не вычитывает полный каталог для browser filtering. Deprecated lookup/all endpoints из текущего кода не объявляются обязательными для target UI.

## 4. Application

### 4.1. Handlers / use cases

`ApplyProductSnapshotHandler`, `ApplyProductVariantSnapshotHandler` и pending resolver размещаются в целевом `ContentManager.CatalogAccess.Application`. Consumer только принимает/mapping/dispatch и подтверждает надёжно сохранённую работу. Сейчас [CatalogSnapshotReceiver](../../../../contentmanager.api/src/App/ContentManager.Application/Integration/CatalogSnapshotReceiver.cs) и [CatalogSnapshotProcessor](../../../../contentmanager.api/src/App/ContentManager.Application/Integration/CatalogSnapshotProcessor.cs) находятся в App.Application: это исходная точка переноса, не второй независимый алгоритм.

Receiver own ReadCommitted/FIRST MVP gate fresh reads receipt/source state/fault. Atomic newest candidate/watermark + immutable event receipt + durable work; ACK after acceptance NOT apply. Payload stays receive-state until worker classification then atomic unresolved Variant/F15 handoff.

Worker bounded durable lease, NEW first-gate transaction reads latestaccepted plus scoped faults/payload owner/current parent/full independent primary public dependencies. Accepted22 prevents apply21 even before22 classification. Apply+pending/conflict/history+work completion+necessary F18 intent atomic own commit. Technical failure retry; missing parent waiting/no TTL/fakeProduct; incompatibility F15. Old Issues not fresh evidence for new target.

Parent apply commit durably schedules affected waiters; wait registration and parent commit share gate plus recovery scan reclaims expired leases. No lost wakeup concurrent Product/Variant processing/crash. Archived parent exists, lifecycle separate. Pending resolver uses stored newest target without new external event.

List/exact Persistence read only. Public guarded ports expose affected fault/readiness separately from Open conflict/lifecycle/absence; no new-use/old apply allowed on reliable higher invalid/unsupported source state.

### 4.2. Validators

[S18](../../../catalog/docs/standards/dotnet/fluentvalidation.md): композиция `SourceCatalogIdentityValidator`, `SourceVersionValidator`, `ProductSnapshotValidator`, `ProductVariantSnapshotValidator`, `CatalogListParametersValidator` и специфичных ID/filter validators. Названия целевые. Атомарные правила: non-empty source/event IDs, positive source version, Name через Domain constraints, корректный normalized lifecycle, допустимые list offset/sort/filter. Variant validator добавляет source parent ID. Envelope contract version и сериализация проверяются transport adapter до Domain mapping.

V2 rules Product Name1–120, Variant1–200, Description string?<=500 null/empty, positive int Version, nonempty IDs, UTC OccurredAt, Active/Archived. Source normalizes before commit; CM validates/preserves, no truncation/rename. V1 checked range/continuity separate, no implicit License/history/ProjectStorage migration.

Pure validators не читают БД/Publishing и не решают freshness, существование parent, collision EventId или move impact. Эти проверки выполняет handler в контексте текущих facts. FV/EF берут selected limits/lifecycle/UTC rules из одного source-profile constraints; смысл Active/Archived и guard обслуживания задан §2.3. Structural invalid input не создаёт partly mapped projection. Source name не нормализуется с потерей информации без выбранного contract profile Q23.

### 4.3. Mappers

| Mapping | Владелец | Правило |
| --- | --- | --- |
| Versioned VR School event → normalized snapshot + EventId | Integration adapter | Stable IDs без генерации; source version → SourceVersion, OccurredAt → source audit; неизвестный enum/schema — contract failure |
| HTTP route/query → Query | Api | Bind ID, parameters и trusted access context; не заменять source identity display name |
| Domain state → read DTO | Persistence projection | Applied и pending отдельно; eligibility/summary по public read contracts; без Version |
| Move facts → typed impact | Publishing port adapter | ID/scope/versions и blockers; не передавать EF entities в CatalogAccess |

Текущий source event имеет `EntityRevision: long`, `ContractVersion`, `IsDeleted`, `Description`. Rename/cast в `int SourceVersion` не является автоматически совместимым контрактом: migration is explicit checked range/continuity and IsDeleted compatibility, no implicit rename or License/history/ProjectStorage change; rollout Q23/Q20. HTTP lookup DTO и integration snapshot не взаимозаменяемы.

### 4.4. Services / algorithms

CatalogSnapshotFreshness uses authoritative CatalogSourceState.LatestAcceptedVersion plus separate active CatalogIngestionFault boundary. Same EventId compares preserved immutable event fact including ID/version/OccurredAt/payload excluding retry headers: changed quarantine/original receipt preserved, plus scoped higherfault if rejected incoming envelope reliably identifies entity+higherVersion; identical normally original receipt, but explicit unchanged UnsupportedContract replay is evaluated BEFORE terminal repeat under recorded evidence profile after decoder repair and fresh fault/latest checks, preserving original evidence. Different EventId/equal version compares source STATE (ID/version/parent/name/description/lifecycle), excludes republication EventId/OccurredAt: equal no-op, contradictory quarantine. Lower watermark stale no-op; after receipt cleanup no promise all old contradictions detectable.

ProductVariantMoveAssessment fresh facts classify newest target. Confirmed compatible SourceChangedApply full apply+Resolved atomic, no publish/history rewrite. New target updates same Open episode; original DetectedAt/applied evidence retained, old Issues not current new-target assessment. Missing parent retains Open/waiting. Fault/transient/missingparent/business conflict separate.

PendingParentResolver/RecoveryScan durable lease-bounded CatalogWorkItem processing, conditional reclaim/fencing/latest version, parent wakeup atomic with apply, scan recovers lost wakeups. Numeric lease/batch/retry defaults engineering Q20, no memory-only callback.

Reconciliation protected VRS operational command selects IDs/all CURRENT catalog; selected Variants include parents. New publication NEW EventId/same current Version; transport retry SAME EventId. Scheduling/report≠CM readiness; absence≠delete, no global generation/bootstrap/history sequence. Independent CM/ProjectStorage queues; CM replicas compete own queue. Exact operational route/permission/wire/topology Q15/Q23/S22.

## 5. Api

### 5.1. Endpoints

| Method | Route | Request | Response | Access |
| --- | --- | --- | --- | --- |
| GET | `/api/v1/products` | Catalog list query | `ApiResponse<OffsetPagedResultDto<ProductListItemDto>>`, 200 | MVP catalog read policy; exact permission Q15/Q10 |
| GET | `/api/v1/products/{productId}` | Route Guid | `ApiResponse<ProductDto>`, 200 | То же |
| GET | `/api/v1/product-variants` | List query, optional productId | `ApiResponse<OffsetPagedResultDto<ProductVariantListItemDto>>`, 200 | То же |
| GET | `/api/v1/product-variants/{productVariantId}` | Route Guid без обязательного parent | `ApiResponse<ProductVariantDto>`, 200 | То же |

Routes из согласованной F06; stable exact Variant ID остаётся пригодным после move. Read API возвращает сохранённую локальную projection и не обещает мгновенного соответствия VR School. Permissions/isolation остаются [Q10](../../../business/questions/Q10-permissions-and-isolation.md), exact transport binding — Q15. Нет source mutation API, CM bootstrap/reconcile POST или proxy записи в VR School.

Текущие [ProductController](../../../../contentmanager.api/src/BoundedContexts/CatalogAccess/ContentManager.CatalogAccess.Api/Product/Controllers/ProductController.cs) и [ProductVariantController](../../../../contentmanager.api/src/BoundedContexts/CatalogAccess/ContentManager.CatalogAccess.Api/ProductVariant/Controllers/ProductVariantController.cs) предоставляют list/lookup; exact GET предстоит реализовать. Наличие list DTO не означает наличия target exact resource.

### 5.2. ETag / If-Match

F06 HTTP surface read-only; обязательные ETag/If-Match и 428/412 для него не вводятся. Local Version — concurrency token consumer/resolver, SourceVersion — freshness. F15 mutable conflict API использует собственный ETag/If-Match по своей фиче. Нельзя переносить туда ETag Product/Variant list строки.

### 5.3. OpenAPI и ошибки

[S16](../standards/S16-rest.md): объявить response envelope, offset page, exact DTO, route/query types и стандартные error responses. Structural query/route validation — 400; absent exact projection — 404; MVP unauthenticated/forbidden — 401/403 по принятому auth profile. Pending/conflict — читаемое состояние, не 404/409 для обычного GET. Source lag не маскируется cached fake object. Machine codes, malformed ID routing и diagnostic DTO visibility уточняются Q15.

Не публиковать root Version, integration commands или обработку consumer как HTTP API. NSwag генерируется после проверки backend/OpenAPI, frontend использует реальные generated responses. Текущие generated signatures сверены во frontend F06; target pending/lifecycle DTO ещё не доказан source.

## 6. EntityFramework

### 6.1. DbContext ownership

`CatalogAccessDbContext`, schema `catalog_access`, владеет Product/ProductVariant/F15 conflict plus CatalogSourceState/CatalogEventReceipt/CatalogWorkItem/CatalogIngestionFault (§2). Shared integration inbox хранится инфраструктурой, но enlisted transaction должна атомарно фиксировать consumed/work state с CatalogAccess изменениями. Чужой DbContext не используется для чтения/сохранения Product, Variant или Publishing dependencies.

В исходнике [CatalogAccessDbContext](../../../../contentmanager.api/src/BoundedContexts/CatalogAccess/ContentManager.CatalogAccess.EntityFramework/CatalogAccessDbContext.cs) также содержит SourceSnapshots, IntegrationBaselines, License/GrantPeriod F12. Они не делают history/bootstrap обязательной моделью F06. Selected receipt/work/fault logical state integrated with chosen receiver infrastructure, no independent competing inbox authorities.

### 6.2. Configurations

| Entity | Table / representation | Key/index | Concurrency | Mapping |
| --- | --- | --- | --- | --- |
| Product | `catalog_access.products` | Source Id PK | `Version` EF concurrency token | Typed Guid ID; applied metadata, source_version int, source_occurred_at UTC; Active/Archived lifecycle; exact CLR/storage/wire profile Q23 |
| ProductVariant | `catalog_access.product_variants` | Source Id PK; applied product_id; pending parent index после выбора representation | `Version` EF concurrency token | Nullable applied group, latest_observed_source_version int; typed owned pending state либо related owned state Q23 |
| Pending snapshot | Owned CatalogAccess state; physical table/JSONB Q23 | Один newest candidate на Variant; требуемый parent доступен для selection | Защищён своим owning root.Version | Типизированная модель Domain; jsonb только storage, не произвольный CLR dictionary |
| CatalogConflict | F15 `catalog_conflicts` | Один Open conflict по kind/entity | Собственный `Version` | Pending target/applied evidence и blockers по F15; согласованно с Variant watermark |
| Consumed/work storage | Chosen LeanSoft adapter to own receipt/work states | Single receipt authority | Own enlistment profile Q20 | Two durable commits, not full source history |
| CatalogSourceState | Own technical receive state | EntityKind+EntityId unique | int Version | Latestaccepted + state evidence + single payload owner/reference |
| CatalogEventReceipt | Own receipt/inbox adapter | EventId unique | int Version for supported recovery | Preserved immutable event evidence/disposition, finite retention |
| CatalogWorkItem | Own durable work | Entity/version and parent/lease indexes | int Version/conditional claim | References only; bounded deadline/recovery scan/completed retention |
| CatalogIngestionFault | Own scoped unreadiness | Reliable entity/version/fault identity | int Version | Separate validwatermark barrier, no F15/Withdrawal |

До первого apply у Variant применённые поля отсутствуют совместно. CHECK/owned nullability не должны разрешать половину AppliedState. LatestObservedSourceVersion сохраняется и после завершения/замены pending, чтобы stale event не откатил root. Worker-applied snapshot must equal authoritative latest valid accepted version AND exact current payload owner, with no active covering fault (fault.SourceVersion >= candidate.SourceVersion); never generic <=watermark permission. LatestObservedSourceVersion is derived from CatalogSourceState, not independently persisted authority.

### 6.3. Constraints / indexes

Source identity PK unique, `ValueGeneratedNever`. Индекс Variant по applied ProductId нужен list; выбор pending parent index зависит от layout и resolver. Positive source versions, local Version и consistency applied/pending groups проверяются выбранными mappings/checks; exact names/overflow — Q23/[Q20](../questions/Q20-persistence-and-migration.md).

Target не имеет физического FK `product_variants.product_id → products.id`, который препятствовал бы допустимому unresolved parent. Не заменять его fake Product seed. Текущий [ProductVariantConfiguration](../../../../contentmanager.api/src/BoundedContexts/CatalogAccess/ContentManager.CatalogAccess.EntityFramework/Configurations/ProductVariantConfiguration.cs) содержит required parent FK и unique `(ProductId, Name)`: их пересматривают по source ownership, а не сохраняют как случайные бизнес-инварианты. [ProductConfiguration](../../../../contentmanager.api/src/BoundedContexts/CatalogAccess/ContentManager.CatalogAccess.EntityFramework/Configurations/ProductConfiguration.cs) сейчас хранит `_versions` в `product_variant_ids`. Parent-child list выводится из applied relation; необходимость совместимой коллекции/её удаления — Q23, нельзя поддерживать две расходящиеся authorities.

## 7. EntityFramework.Persistence

### 7.1. Repositories / query adapters

`ProductRepository`/`ProductVariantRepository` исполняют root reads tracked по stable ID и сохраняют через CatalogAccess UoW; Pending reader выбирает только newest targets по ожидаемому parent. Conflict repository относится к F15. DTO queries выполняют specifications и server projection AsNoTracking; count/ordering/page применяются в PostgreSQL. Текущий CatalogQueryHandler использует ILike, Id ordering, offset и clamp page size; это source evidence, не утверждение окончательных target defaults Q15.

Move-impact adapter обращается к публичному Publishing query port. `ICatalogOperationBlockReader` возвращает локальные blockers по F15, не загружает всю history или licensing в F06.

### 7.2. Transactions

Для MVP владелец этих commits — CatalogAccess. По [утверждённому MVP consistency guard](../questions/Q20-persistence-and-migration.md#mvp-протокол-consistency-guard) все CM replicas используют одну PostgreSQL database и один стабильный глобальный exclusive transaction-level advisory key. Каждый affected owner scope явно начинает ReadCommitted transaction, затем отдельным statement вызывает pg_advisory_xact_lock на **той же physical connection и transaction**, до любых authoritative domain reads и row locks. После успешного acquire заново загружаются own roots/tokens и current foreign facts через public ports из независимых primary read scopes/connections. Предварительные query/preview/cache/replica facts не разрешают commit; foreign read не enlist-ится в ambient owner UoW, не разделяет SQL transaction и не открывает foreign EF из caller.

Guard удерживается до owner commit/rollback: изменяются только roots своего BC и атомарные own outbox/audit intent/inbox markers, где они нужны. Все invalidating writers, включая creation/removal/reference Copy/orphan paths, Publish/lifecycle/hard delete, setup/reconciliation, source durable acceptance/watermark/pending/apply/retry и F15 open/supersede/repair/resolve, участвуют в этом же ключе. Own Version/If-Match сохраняется: stale own token →412, изменившаяся foreign/preview basis →409 по Q15. Create/Copy из неизменённого source не увеличивает source Version; два serially guarded допустимых создания могут оба завершиться успешно.

Внешний I/O (broker, VR School, MinIO), ожидание пользователя и долгие streams выполняются вне gate. CPU preparation допустима заранее, но exact basis повторно подтверждается внутри gate; иной basis требует отказа или новой подготовки вне gate. Bounded guard wait/lock timeout конфигурируются, поддерживают cancellation и являются availability/technical failure, не domain409; точные значения и transport mapping остаются Q15/Q20. Connection loss прерывает owner transaction: reconnect-and-continue запрещён. Потеря commit ACK даёт unknown outcome; выяснение exact state/исходного operation ID по Q15 предшествует повтору, blind command retry запрещён.

Receiver commit stores valid newest watermark/candidate/receipt+work atomically. Separate worker commit projection/pending/conflict/history+completed work+necessary F18 intent. При Product apply вместе с ним надёжно сохраняется необходимость resolve ожидающих Variant. UI может увидеть pending/conflict только после commit; брокерный ack не означает, что unresolved relation уже разрешена.

MVP selects TWO durable stages receiver acceptance + worker apply, no synchronous consumer-apply alternative; SDK registrations/mappings Q20. В MVP durable acceptance свежего candidate участвует в global guard сразу: собственный commit сохраняет проверяемую newest identity/version/full snapshot или соответствующий single authoritative handoff/watermark до подтверждения приёма. Если v22 accepted durable, но ещё ждёт impact/parent processing, resolver не имеет права применить старый pending v21. Неполученное/непринятое durably сообщение не считается watermark. Worker apply берёт новый owner transaction/global key и fresh перечитывает acceptance, watermark, candidate, parent и public Publishing facts. Обе фазы recoverable; первый успех не объявляется apply. Broker ack/publish выполняются после освобождения gate. Потеря ack допускает EventId/freshness repeat с fresh scope, ошибка до commit не оставляет partial projection.

Cross-BC transaction запрещена целевой архитектурой. Проверка Publishing dependencies и согласование writers используют принятый MVP global advisory protocol §7.2; существующий [InboxUnitOfWorkAdapter](../../../../contentmanager.api/src/App/ContentManager.EntityFramework/Integration/InboxUnitOfWorkAdapter.cs) описывает shared transaction всех BC и поэтому сам по себе не доказывает соответствие этой границе. Concurrency failure перечитывает весь input state в новом корректном scope.

### 7.3. Migrations

По [Q20](../questions/Q20-persistence-and-migration.md) CM ещё не имеет production data, ради которых требуется сохранять legacy schema. Миграции не выполняются в этой аналитике. Реализация должна согласованно заменить SourceRevision/concurrency mapping, required applied fields, parent FK/name uniqueness, pending/conflict storage и history/baseline зависимость для Product/Variant. Удаление общих source-history tables запрещено делать как побочный F06 cleanup, пока они используются License/F12: сначала согласовать границу F12/Q23.

Текущие definition seeds [ProductDefinition](../../../../contentmanager.api/src/BoundedContexts/CatalogAccess/ContentManager.CatalogAccess.EntityFramework/Products/Definitions/ProductDefinition.cs) и [ProductVariantDefinition](../../../../contentmanager.api/src/BoundedContexts/CatalogAccess/ContentManager.CatalogAccess.EntityFramework/ProductVariants/Definitions/ProductVariantDefinition.cs) могут служить test data; они не являются production source каталогом или способом bootstrap VR School. Rollback/forward migration design и shared integration changes — Q20/Q23.

## 8. External adapters

### 8.1. RabbitMQ / MassTransit

VR School source transaction сохраняет entity change + стандартную transactional outbox запись. Dispatcher отправляет versioned full snapshot через broker. Contracts содержат EventId, source entity ID, положительную per-entity version, OccurredAt и полное нужное CM состояние; transport metadata отдельно от Domain. CM получает собственную subscription, независимо от Project Storage.

[S22](../../../catalog/docs/standards/dotnet/masstransit-rabbitmq.md#optional-three-project-eventbus-composition): `Integration/EventBus/{SolutionName}.Integration.EventBus.Contracts`, `.RabbitMq`, `.Default`; Contracts не содержит Domain/EF/topology, RabbitMq централизует exchange/entity name, publish topology/correlation, Default композирует. Receive endpoints и consumers принадлежат consumer composition root. Queue/exchange migration, retry/redelivery/error policies и contract package version — Q23; literals из старого кода не выбираются заново этим документом.

Сейчас [CatalogConsumers](../../../../contentmanager.api/src/App/ContentManager.Application/Integration/CatalogConsumers.cs) вызывают receiver, а [CatalogSynchronizationServices](../../../../contentmanager.api/src/App/ContentManager.Application/Integration/CatalogSynchronizationServices.cs) регистрирует consumers/inbox, receive endpoints и request history client при включённой конфигурации. Это реальная инфраструктурная исходная точка. Её history request/bootstrap и contract ordering заменяются целевыми full-snapshot semantics, а не выдаются за reconciliation MVP.

Проверенные source [ProductSnapshotV1](../../../../VRSchool.Host/Integration/EventBus/VRSchool.Integration.EventBus.Contracts/Catalog/V1/ProductSnapshotV1.cs) и [ProductVariantSnapshotV1](../../../../VRSchool.Host/Integration/EventBus/VRSchool.Integration.EventBus.Contracts/Catalog/V1/ProductVariantSnapshotV1.cs) используют long EntityRevision и IsDeleted; [VRSchoolDbContext.Catalog](../../../../VRSchool.Host/src/VRSchool.EntityFrameworkCore/EntityFrameworkCore/VRSchoolDbContext.Catalog.cs) сохраняет snapshots history вместе с source changes и запрещает move. Требуемые producer outbox dispatch и допустимый move не доказаны этой записью history; точные расхождения перечислены Q23. История не подменяет delivery outbox.

**ProductSnapshotV2 (selected integration contract)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| EventId | Guid | Да | Source outbox identity | Transport retry same ID; reconciliation new ID |
| ContractVersion | int | Да | Supported V2 profile | No V1 silent rename |
| Id | Guid | Да | Stable Product source ID | Nonempty/source-owned/name not identity |
| Version | int | Да | Positive monotonic per-entity version | Gaps allowed, no global sequence |
| OccurredAt | DateTimeOffset | Да | Source event UTC audit | Not freshness; state equal-version comparer excludes |
| Lifecycle | SourceCatalogLifecycle | Да | Active or Archived | Explicit, not Hidden/Pending |
| Name | string | Да | Source-normalized name | 1–120 CM validates/preserves |
| Description | string? | Нет | Full source description | <=500 null/empty allowed, CM preserves |

**ProductVariantSnapshotV2 (selected integration contract)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| EventId | Guid | Да | Source outbox identity | Transport retry same ID; reconciliation new ID |
| ContractVersion | int | Да | Supported V2 profile | No V1 silent rename |
| Id | Guid | Да | Stable Variant source ID | Nonempty/source-owned/name not identity |
| ProductId | Guid | Да | Exact source parent | May be missing locally; no placeholder |
| Version | int | Да | Positive monotonic per-entity version | Gaps allowed, no global sequence |
| OccurredAt | DateTimeOffset | Да | Source event UTC audit | Not freshness; state equal-version comparer excludes |
| Lifecycle | SourceCatalogLifecycle | Да | Active or Archived | Explicit, not Hidden/Pending |
| Name | string | Да | Source-normalized name | 1–200 CM validates/preserves |
| Description | string? | Нет | Full source description | <=500 null/empty allowed, CM preserves |

Adapter V2 Id/Version/Lifecycle maps owned strong IDs/SourceVersion/SourceLifecycle (§2), no Domain→MassTransit. V1 long/IsDeleted rollout explicit checked range/lineage continuity, never clamp/reset/reinterpret. Catalog-only rollout does not imply License/history/ProjectStorage migration. Source fix newerVersion; repaired unsupported profile admits unchanged original replay, no changed payload under EventId.

### 8.2. Адаптер публичных Publishing contracts

Адаптер `IProductVariantMoveImpactReader` реализует согласованный public query boundary: читает dependencies/версии, возвращает typed impact; MVP caller уже держит owner global advisory key, adapter открывает независимый primary read scope и сам этот key повторно не acquires. Он не обращается к VR School DB, не хранит локальный source override и не выполняет Publishing mutations автоматически. Локальные изменения для разрешения конфликта принадлежат F15/затронутым фичам. Protected VRS reconciliation selected logically; exact HTTP/broker/operator trigger/permission/wire engineering, no guessed CM source CRUD.

## 9. Data flows

MVP порядок для DF01–DF02, durable source acceptance, pending resolver и CM apply в DF07: input/CPU preparation вне gate → явная owner ReadCommitted transaction → standalone exclusive advisory acquire → fresh own roots и foreign primary-port facts → проверки own token/basis/lifecycle/dependencies → Domain action → только own writes/atomic intents → commit и release. Это обязательный префикс каждого указанного mutation/retry stage; строки «load/guard» ниже означают загрузку **после** acquire. Read-only query/impact/preview остаются observations; при необходимости короткий source capture имеет отдельный guarded scope, но долгий stream и пользовательский review не удерживают guard. Demo использует свой согласованный контур без новых обязательных MVP dependencies.

### Транспортные потоки

Форма таблицы — [S17](../../../catalog/base/docs/standards/data-flows.md). Поля определены в разделах 2–3, не дублируются в алгоритмах. Broker «успех» означает durable acceptance/commit выбранной фазы, а не синхронизацию всех зависимостей.

| Поток | Транспорт | Метод / канал | Endpoint / destination | Доступ | Вход | Application action | Выход | Успех | Ошибки |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| DF01. Product apply | Durable worker after DF08 | Current work | Catalog worker | Internal | Work/current payload | ProcessCatalogWorkCommand | Projection+parent wakeup | Own apply separate ACK | Fault/stale lease/retry |
| DF02. Variant apply/move | Durable worker after DF08 | Current work | Catalog worker | Internal | Work/current target | ProcessCatalogWorkCommand | Applied/wait/Open/Resolved | Own atomic commit | Fault/transient/lease |
| DF03. Список Product | HTTP API | GET | `/api/v1/products` | MVP read policy Q15/Q10 | List query | ListProductsQuery | ApiResponse offset page | 200; пустая выборка допустима | 400, 401/403 по профилю, storage failure |
| DF04. Exact Product | HTTP API | GET | `/api/v1/products/{productId}` | То же | Route ID | GetProductQuery | ApiResponse ProductDto | 200 локальной projection | 400/routing, 404, 401/403; storage failure |
| DF05. Список Variant | HTTP API | GET | `/api/v1/product-variants` | То же | List query + optional productId | ListProductVariantsQuery | ApiResponse offset page | 200; applied relation filter | 400, 401/403; storage failure |
| DF06. Exact Variant | HTTP API | GET | `/api/v1/product-variants/{productVariantId}` | То же | Stable route ID | GetProductVariantQuery | Applied/pending representation | 200, включая сохранённое unresolved состояние | 400/routing, 404, 401/403; storage failure |
| DF07. Reconciliation MVP | Protected VRS command+outbox | Current selectedIDs/all with parents | Logical VRS boundary/route Q15 | Trusted operational profile | Scope | Schedule V2 republishes | Scheduling report/newEventIds | Not CM readiness/absence delete | Source/dispatch/apply failures separate |
| DF08. Receiver acceptance | Broker V2/admitted invalid envelope | Receive command/privatefault path | CM independent own queue | Trusted source | Full snapshot/evidence | FIRST-gate receive handlers | Receipt+newest candidate/work/fault | ACK acceptance NOT apply | Contradiction/invalid/transient distinct |
| DF09. Work/recovery | Durable scheduler/scan | Conditional bounded lease | Catalog worker | Internal owner | Work/entity/version/claim | ProcessCatalogWorkCommand | Apply/wait/Open/complete | NEW firstgate/latest/fault/token/deadline | Expired/superseded/technical |
| DF10. Scoped ingestionfault | Receiver/profile repair replay | Fault capture/recovery | Own scoped state | Reliable higher source identity/version | Rejected immutable evidence | Fault classifier/recovery | NotReady/coveredfault cleared | Not F15/Withdrawal/global block | Unknown entity/version no scope; invalidEventId valid entity still scoped; unchanged replay only |

### DF01. Применить Product и инициировать разрешение parent

1. Message first accepted DF08; worker claim/new own transaction/FIRST global gate fresh latestaccepted/fault/claim token+deadline/payload. Accepted22 prevents applying21.
2. Current full Product snapshot applies atomically, including Description/lifecycle/version; receive payload cleared, work completed. Scoped covering fault (version>=candidate) blocks ordinary apply; equal-version recovery only explicit unchanged UnsupportedContract branch.
3. SAME apply commit durable parent-waiter wakeup; waiter registration under same gate+recovery scan prevents lost wakeup. Archived parent exists, lifecycle separately guards new use.
4. Release before dispatch/next work. Catalogue projection not F18 noise; actual independent business Resolve result audited by F15/F18.

### DF02. Применить Variant, ожидать parent или открыть конфликт

1. DF08 acceptance separate from apply. Worker new FIRST gate rereads latestaccepted/scoped fault/current claim token+deadline/single payload owner/applied/Open/parent/full independent primary Publishing facts.
2. First AppliedState absent together; existing group preserved until safe full apply. No parent FK/placeholder. Accepted22 defeats21 even before classification.
3. Missing parent atomic receive→unresolved Variant or newest Open.PendingState handoff, durable WaitingParent; old Issues not fresh proof for new target, Open retained/no TTL/DLQ.
4. Parent exists including Archived; full complete current assessment incompatible→sameOpen updated, transient dependency failure retry not empty blockers. Compatible newest source full apply+Resolved(SourceChangedApply)+history/work completion+necessary F18 intent own atomic commit.
5. Preserve stable IDs/CM links/DetectedAt/applied episode evidence; no publish/history rewrite. Resolver reuses persisted target without external event; fault/lease/watermark rechecked before commit.

### DF03. Список Product

1. Api проверяет доступ, binds list parameters и вызывает ListProductsQuery; invalid query не заменяется молча default.
2. Persistence строит atomic filters/search, whitelist order + stable Id, count и page server-side. Не запрашивает VR School и не загружает все Variant/history для каждого Product.
3. Проецирует read DTO и только согласованные summary facts. Applied source state и eligibility основаны на local projection; source lag допустим, readiness из отсутствия событий не выводится.
4. Api возвращает 200 offset envelope. Пустая page — нормальный результат. Count/page под конкурентной source записью имеют согласованность выбранного read profile Q15, без обещания snapshot isolation по умолчанию. Отмена передаётся EF; записей/commit нет.

### DF04. Exact Product

1. Api binds source ProductId и trusted read access.
2. Persistence exact-ID specification читает локальную projection без tracking. Archived object не исчезает только потому, что исключён из новых pickers; Hidden presentation не изменяет lifecycle/eligibility.
3. Missing both source identity/fault state and applied Product→404. Known accepted source identity before worker projection→diagnostic200 with Name/lifecycle/applied group absent, Processing explicit; no fake Product/applied label. Pending Variant не превращает Product GET в синхронное resolve или ошибку.
4. No mutation/ETag prerequisite; typed source processing/fault fields §3.3, exact wire Q15.

### DF05. Список Variant

1. Api binds list query и optional ProductId. `productId` — applied relation filter, не команда перемещения и не утверждение newest source relation.
2. Persistence server-side выбирает Variant, применяет search/state/eligibility filters по выбранному read profile, sort/tie-breaker/count/page. Связь с pending source parent не подменяет applied ProductId.
3. DTO отделяет applied fields от pending/conflict summary. Новый unresolved Variant без applied parent не становится получателем новых назначений; необходимость его отдельного diagnostic listing — Q15.
4. Api возвращает 200, в том числе empty page. Изменение каталога между страницами возможно по выбранной pagination consistency; frontend не склеивает results разных filters. Нет записей или hidden resolve.

### DF06. Exact Variant и актуальный applied parent

1. Api получает только stable VariantId, не требует parent ID в API route.
2. Persistence читает Variant и необходимые local read facts/pending/F15 summary. No applied state — видимое ожидание; missing identity — 404.
3. Для safe move DTO содержит новый applied parent. При blocked move сохраняется прежний applied parent; newest target показан отдельно. Source Archived object сохраняется для exact read/истории; V2 explicit lifecycle/checked V1 adapter, no silent IsDeleted rename.
4. Api возвращает 200. UI canonical route строится по applied ProductId согласно frontend Q13; этот GET не меняет локальные задания или source relation. No required If-Match и автоматической записи после чтения.

### DF07. Сверить каталог повторной публикацией snapshots

1. Protected operational VRS command selects IDs/all CURRENT catalog; selected Variants include parents. Exact route/permission/wire engineering, no guessed CM source CRUD.
2. Outbox publishes NEW EventId/same current Version; transport retries SAME ID. No version bump for republication/global sequence/bootstrap/history replay.
3. CM ordinary DF08→worker flows, equal-state no-op/contradiction quarantine. Scheduling/error report≠CM readiness; absence≠delete, partial delivery not implicit archive. Independent CM/ProjectStorage queues.

### DF08. Durable receive acceptance

1. Adapter admits trusted envelope/identity/version; pure V2 decode/validation maps source values only when valid. Invalid/unsupported reliably admitted envelope reaches private receipt/fault path without requiring fabricated ProductSnapshot/V2 command.
2. Own ReadCommitted FIRST global gate before receipt/source/fault reads. Same EventId compares preserved original immutable event evidence under its RECORDED profile, including reliable identity/version/OccurredAt/immutable wire payload, retry headers excluded; changed evidence quarantined without changing original receipt. Before terminal return, if rejected incoming envelope reliably identifies an entity+higherVersion, atomically persist corresponding scoped ingestion fault/private rejected evidence too; quarantine cannot erase known newer unreadiness. Unknown entity/version creates no guessed scope. Before ordinary identical→original receipt, explicit UnsupportedContract recovery branch may decode unchanged original after profile repair and advance private receipt disposition/clear covered fault/store current candidate+work atomically after fresh source/fault checks. Original evidence never rewritten/new-schema renormalized for equality; ordinary replay returns original receipt.
3. Other EventId/equalVersion source STATE comparer (ID/version/parent/name/Description/lifecycle), excludes EventId/OccurredAt: equal no-op, contrary quarantine; lower watermark stale no-op.
4. Before ordinary valid-newer acceptance check active fault: candidate must be strictly newer than covered faultVersion; equal allowed ONLY exact unchanged UnsupportedContract replay branch, never ordinary new event/state fix. Fault still higher blocks. Valid eligible acceptance atomically stores newest source state/watermark/receipt+work. If existing Open owns payload, replace its PendingState in SAME acceptance commit and retain old version-bound assessment as stale; otherwise clear old unresolved slot/replace receive slot atomically. ONE mutable newest target at every stage, no independently valid old slot. New target assessment pending, not old-issues proof.
5. Reliable higher entity version invalid/unsupported→DF10 scoped barrier; unknownentity/version no invented entity block. Commit/release then ACK acceptance only; worker separate new transaction.
6. Original invalid-profile receipt preserves admitted envelope+rejected immutable wire evidence for exact unchanged replay, not fake valid normalization. Private codec Q20; no public raw payload/log.

### DF09. Work lease / parent recovery

1. Durable own scan conditional lease/reclaim/Version; numeric defaults engineering. Work entity/version refs no target copy.
2. NEW own first-gate worker rereads latestaccepted/scoped fault/payload/dependencies/current claim token AND bounded deadline before apply/complete; stale lease not business permit, obsolete owner fenced.
3. Parent apply commit durable wakeup, waiting registration samegate; recovery scan rechecks current parent if signal crashes. Missingparent WaitingParent/no TTL; technicalfailure reclaimable; accepted22 no apply21.
4. Receipt/completedwork finite configurable retention; durable current watermark/unresolved/conflict history remains stale guard, no allhistory detection promise after cleanup.

### DF10. Scoped ingestion fault / recovery

1. Trusted reliable identity+higher positive version with invalid/unsupported snapshot stores own receipt+CatalogIngestionFault unreadiness; validaccepted watermark separate.
2. Fresh worker and guarded Catalog/Publishing ports consult affected fault; no stale candidate/new-use commit. No fake F15/Withdrawal/blanket catalog block; unknown entity/version no fabricated scope.
3. Source fix NEWER version; unsupported profile repair may admit UNCHANGED original replay. EventId evidence never replaced. Recovery cannot bypass still-active covering fault with candidate<=faultVersion. Ordinary source-fix candidate must be strictly newer than relevant active fault; equal only explicit unchanged UnsupportedContract replay after profile repair.
4. Invalid/missing EventId with reliable entity+higherVersion persists own fault/quarantine evidence without source receipt/ID. Unknown entity/version creates no guessed block. Invalid source recovery requires strictly newer valid Version; only unchanged supported-after-repair original UnsupportedContract replay may recover equal fault version.
5. Transient dependency/DB, missingparent and genuine business conflict distinct; no absence delete/guess readiness from scheduling.

## 10. Ошибки и диагностика

MVP guarded failure различает confirmed business refusal и невозможность проверки: unavailable/incomplete public facts не означают empty/valid facts. Guard wait cancellation/timeout и connection loss — technical/availability outcome без domain Conflict; точные wire codes остаются Q15. После connection loss scope завершается rollback/disposal без продолжения на новой connection. Unknown commit outcome выясняется по exact state/operation ID; prepared tracker не подтверждает ни success, ни rollback. Own stale ETag сохраняет412, foreign basis change —409. Диагностика фиксирует wait/hold duration, owner/operation, acquire/commit/rollback/unknown phase без full payload и секретов.

| Категория | Реакция | Наблюдаемость / граница |
| --- | --- | --- |
| Duplicate EventId / stale/equal source version | Успешный no-op без повторных writes/side effects | Outcome и source IDs/versions; transport tracking отдельно |
| Missing parent | Durable pending, повторное разрешение после parent/нового source state | Required parent ID, applied/observed versions; age для оператора без TTL удаления |
| Несовместимый move | F15 newest target + scoped operation blocks | ConflictId, applied/target parent, blockers; GET не падает |
| Local optimistic race | Rollback, reread newest roots/pending/impact, повторная оценка | Не переименовывать source race в HTTP 412 F06 |
| Transient PostgreSQL/broker/public-port failure | Recoverable retry по выбранной политике | Commit stage/correlation, не фиктивный safe impact |
| Невалидный contract / reused EventId с другим содержимым | Contract diagnostics/error handling Q23 | Payload не применять частично; точные retry/DLQ/machine codes уточняются |
| Equal source version с противоречивым содержимым | Applied/pending не переписываются; диагностика source contract | Payload-consistency policy Q23; normal equal-version republish не отклонять только за новый EventId |
| HTTP read invalid/not found/access | 400/404/401/403 по Q15/S16 | ProblemDetails; no partial DTO/placeholder |

Диагностика различает received, durably queued, applied, pending-parent, conflict и no-op; last occurred source time не выдаётся за processed time. Метрики pending/conflict/consumer lag могут использовать operational time, но не заменяют per-entity ordering. Логи содержат EventId/entity ID/source versions/outcome и correlation, без full broker payload, credentials или activation key. License/key flow вне F06.

## 11. Тестирование

Это стратегия будущей реализации, а не отчёт о выполненных application tests. Current [CatalogPersistenceTests](../../../../contentmanager.api/tests/BoundedContexts/CatalogAccess/ContentManager.CatalogAccess.Tests.Integration/CatalogPersistenceTests.cs), [CatalogLookupQueryTests](../../../../contentmanager.api/tests/BoundedContexts/CatalogAccess/ContentManager.CatalogAccess.Tests.Integration/CatalogLookupQueryTests.cs) и [CatalogListTests](../../../../contentmanager.api/tests/BoundedContexts/CatalogAccess/ContentManager.CatalogAccess.Tests.Functional/CatalogListTests.cs) дают существующие fixtures, но не доказывают pending/move/full-snapshot target.

[S04](../standards/S04-testing.md), [S14](../../../catalog/docs/standards/dotnet/testing.md#test-environment-ownership-and-bounded-parallelism): Unit SUT — Domain/freshness/mappers/pure validators, допустимы doubles публичного impact port и scheduling boundary для проверки command orchestration. Persistence/consumer tests используют настоящий PostgreSQL через Testcontainers/существующий PostgresSuite, реальные configurations/UoW/inbox; in-memory provider не доказывает constraints или atomicity. Functional SUT — один CM host с HTTP/OpenAPI и настоящим PostgreSQL/JWT при MVP auth; fake VR School допустим, broker transport тогда не считается проверенным. Broker integration использует реальные RabbitMQ и PostgreSQL, реальные consumers/inbox; E2E — необходимый контур VR School producer → outbox → broker → CM → read API, со source DB, а не несколько BC в одном API.

Каждая проверка создаёт собственные source IDs/EventIds/data. Database isolation/cleanup по существующей fixture; broker queues/endpoints уникальны для run, дождаться observable commit ограниченным poll, не фиксированным sleep. Tests не меняют общий пользовательский каталог. Сценарии двух workers используют разные scopes/connections и управляемый barrier между read и commit.

| ID / DF | Уровень / SUT | Исходные данные / сценарий | Наблюдаемый результат |
| --- | --- | --- | --- |
| V201 / DF08–09 | Real PostgreSQL receiver+worker barriers | Receiver accepted22 before old worker21; two CM replicas and expired claim/deadline | No apply21, newest singleowner/work reference only, stale owner cannot complete |
| V202 / DF08 | Broker/PostgreSQL ACK fault | Crash before acceptance/after acceptance before ACK, repeated immutable EventId | Acceptance+receipt+work atomic, repeat original receipt or explicit unchanged unsupported recovery before terminal repeat; ACK not apply |
| V203 / DF08 | Unit + PostgreSQL equality | Same EventId changed time/body; newEventId sameVersion/time differs same sourceState, contrary state | Immutable event conflict vs normal equal-state no-op; no applied time/history rewrite |
| V204 / DF09 | PostgreSQL concurrent parent/Variant + crash | Missing first parent/old applied move, parent commit vs waiter registration/lost wakeup | No fake Product/TTL; whole applied absent/intact; durable wakeup+scan convergence |
| V205 / DF10 | Receiver/guarded source SQL | Reliable invalidhigher22 over valid21; missing/malformed or reused changed EventId but reliable entity22; unknownentity/version; unsupported22 repaired unchanged replay | Scoped NotReady blocks21/newuse, missingEventId no fake receipt/source ID, changed reusedEventId original receipt retained but higherfault stored, unknownentity no scope; strict newer fix/equal only unchanged supported-profile recovery; no F15/Withdrawal/globalblock |
| V206 / DF07,09 | Real source broker/current catalogue | SelectVariants includesparents/republication newEventId sameVersion, CM offline/partialdelivery/cleanup | Independent CM/ProjectStorage queues; schedule not readiness/absenceDelete; stale guard persists retention |
| V207 / V2 adapter | Unit/producer contract + PostgreSQL | V1 checked long overflow/continuity, explicit lifecycle, Name120/200 and Description500/null/empty | No clamp/rename/historyLicense migration; CM validates/preserves source normalization |
| V208 / read | Functional own projection | Accepted Product identity before apply; superseded F15 Issues/fault typed read | No fake404/Name/lifecycle; old assessment marked stale, Description/state typed no Version |
| T01 / DF01 | Unit + PostgreSQL Integration, Product apply | Новый stable ID; v18 → v21 при отсутствии v19/v20; более ранний OccurredAt у v21 | Применена v21 целиком, не запрошена история; local Version защищён |
| T02 / DF01–02 | Consumer Integration, inbox/freshness | Repeat EventId; equal/stale version с другим EventId; lost ack после commit | Нет повторных последствий или rollback свежего state |
| T03 / DF02 | PostgreSQL Integration, Variant pending | Variant v21 до parent; parent snapshot затем durable resolver | ID/pending сохранены без fake parent; после parent applied v21, no TTL/DLQ |
| T04 / DF01–02 | Integration, durable scheduling | Parent commit и crash до worker; parent/Variant приходят конкурентно | Work восстанавливается; pending не остаётся навсегда из-за consumed EventId |
| T05 / DF02 | Unit + Integration, move assessment | Safe move A → B; существующие CM links по VariantId | Parent меняется, ID/local references сохраняются; history immutable |
| T06 / DF02 | Integration, F15 boundary | Applied v18/A, incompatible v21/B, затем v20/C и v22/D | Один conflict, applied A/v18, newest target D/v22; v20 не затирает v21 |
| T07 / DF02 | Integration, pending resolver race | Pending v21/B; resolver read; более новый v22/C до resolver commit | v21 не применяется после v22; latest watermark согласован |
| T08 / DF02 | Integration, all local writes | Forced failure перед completed marker/commit | Нет partial projection/watermark/conflict/block state; retry восстанавливает outcome |
| T09 / DF02 | Integration, Publishing guard protocol | Impact safe; параллельный authoring writer/move/F15 Resolve | Запрещена несогласованная новая operation; принятый MVP global advisory protocol проверен реальными writers |
| T10 / DF01–02,06 | Integration + Functional, lifecycle | Более новый Active → Archived → Active snapshot; parent Archive при own Active/Archived Variant; Hidden/pending отдельно | Identity/read/history сохранены; lifecycle Variant не меняется от parent; restore только own Active + no blockers; no automatic activate/unwithdraw/grant/revoke |
| T11 / DF03–06 | Functional, CM HTTP/OpenAPI | Search/filter/page/order/exact pending/moved/Archived/absent IDs | Stable envelope, applied vs pending; correct empty/404/access; no root Version |
| T12 / DF03–06 | Functional/Architecture | Route/OpenAPI surface | Нет Product/Variant source CRUD, bootstrap/reconcile POST из предположения, required If-Match для GET |
| T13 / DF07 | Broker Integration + необходимый E2E | Republish current full snapshots, частичная доставка, CM offline/restart | Fresh state сходится; equal/stale безопасны; отсутствие в наборе не delete |
| T14 / DF01–02 | Architecture | Project references/handler placement/public ports | Commands Application, queries Persistence; no чужой DbContext; contracts/topology S22 |
| T15 / DF01–02 | Integration, producer/outbox/contract profile | Source commit rollback/dispatcher retry, реальный message version/lifecycle | Atomic source/outbox, no publish потерянного mutation; CLR/wire migration проверена |
| T16 / DF01–02,03–06 | Integration + Functional, archive maintenance и независимая Delivery | Archived Product/Variant; retain/remove/problematic eligible replace vs addition/new release/revision/activation/rollback/promotion; materialized revision + confirmed entitlement | Ограниченное обслуживание проходит обычные guards; расширение/new-use отклонены; Delivery/new session сохранены; safety Unpublish/Withdrawal доступны отдельно |
| T17 / DF02 | Unit + Integration, concrete impact | Move A → B; missing override target A, unrelated local result B; отдельно duplicate prospective B и inherited B плюс A → B | Missing target alone не blocker; подтверждённая effective collision blocker; immutable history alone не blocker, unrelated scope доступен |

MVP Integration matrix для DF01–DF02, durable source acceptance, pending resolver и CM apply в DF07 использует real PostgreSQL, отдельные owner connections/UoWs и barriers, включая разные CM replica processes/hosts там, где проверяется общность key. Doubles/in-memory locks не доказывают SQL coordination.

| MVP сценарий | SUT / воздействие | Наблюдаемый результат |
| --- | --- | --- |
| Guard до чтений | Реальный Persistence adapter; writer A держит gate, B начинает owner transaction до acquire; A меняет eligibility/basis и commit, B продолжает | B после acquire читает новый committed state при ReadCommitted; stale предварительные facts не применяются; lock statement использует owner connection/transaction |
| Меж-BC write/read partition | Public read adapter и Host/DI; owner держит guard, foreign primary read отдельным scope | Read не enlist-ится в owner transaction, не читает cache/replica; no foreign writes/EF access; atomic commit только owner + intents |
| Commit/rollback/release | Barriers для invalidating writer и affected operation; injected owner failure/cancellation | Второй writer ждёт release и revalidates; failed owner полностью rollback, нет partial roots/intents; unrelated next scope получает key |
| Wait и connection failure | Configured bounded wait/cancellation, разрыв owner physical connection | Technical/unavailable, не409/empty success; aborted transaction не продолжается через reconnect |
| Unknown commit ACK | Разрыв response/commit acknowledgement и inquiry exact state/original operation ID | Committed result сохраняется, rollback не предполагается; no blind duplicate mutation |
| Accepted v22 против pending v21 | Барьер после durable acceptance v22 до processing; resolver/retry/setup/reconciliation v21 пытается apply | Resolver после acquire видит newest accepted evidence, не applies v21; projection/pending/own inbox atomic; duplicate acceptance не откатывает watermark |

## 12. Порядок реализации

1. Materialize selected V2 contracts/limits/lifecycle and explicit checked V1 rollout; exact source packages/reconciliation wire/topology engineering Q23/S22.
2. Domain full applied/pending values + technical receive-state/receipt/work/fault ownership; immutable equality profile and single payload handoff.
3. Receiver acceptance FIRST own gate and worker separate NEW gate, conditional bounded claims/recovery scan; SDK owner-partitioned DI/EF mappings/indexes Q20.
4. Public guarded Catalog/Publishing facts consult latestaccepted and separate fault; F15 version-bound assessment/Open supersession/manual repair+Resolve two commits unchanged.
5. Preserve License/history boundary, no implicit ProjectStorage change; source outbox independent queue definitions, protected current reconciliation with parents.
6. Read applied/unapplied Product/Variant Description/processing diagnostics under existing routes; exact wire/errors/list defaults Q15.
7. Real PostgreSQL/broker acceptance ACK/crash/leases/concurrent waiters/newest/fault tests, then actual source-to-CM UI contour; docs checks not runtime proof.

## 13. Открытые технические вопросы

- [Q23 accepted catalogue V2](../questions/Q23-source-synchronization.md#принятый-протокол-каталога-v2): mechanisms/source fields/limits/single payload/two stages/fault/retention/reconciliation selected; exact V1 rollout compatibility and producer package/topology/admission registration engineering.
- [Q20 receive/work/fault](../questions/Q20-persistence-and-migration.md#catalog-v2-receive-work-и-fault-boundary): physical mappings/indexes/receipts+worker lease/scan/SDK owner enlistment/evidence codecs/configurable retention; no source-history/License cleanup by implication.
- [Q15 read contracts](../questions/Q15-api-and-source-code.md#f06-детализация-read-контракта): exact DTO casing/filters/sort/list limits/errors/permissions/NSwag; selected Description/typed processing fields §3.3.
- [F15](F15-catalog-conflicts.md): source target and assessment handoff selected; exact prospective repair/resolve wire unchanged, two own commits/no history rewrite.

No numeric worker lease/retry/retention defaults or guessed VRS reconciliation URI/permission chosen. Model source semantics/ACK-vs-apply/knownfault readiness no longer TBD.
