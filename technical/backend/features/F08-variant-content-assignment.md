# F08. Локальный контент ProductVariant: бэкенд

[Бизнесовая спецификация](../../../business/features/F08-variant-content-assignment.md) · [Модуль M04](../modules/M04-publishing.md) · [Фронтенд](../../frontend/features/F08-variant-content-assignment.md) · [Q07](../../../business/questions/Q07-exclusions-and-local-content.md#f08-сохранение-patch-и-проверка-live-базы) · [Q15](../questions/Q15-api-and-source-code.md#f08-patch-preview-и-parent-route) · [Q20](../questions/Q20-persistence-and-migration.md#f08-patch-concurrency-и-preview-consistency).

## 1. Область реализации

F08 — MVP Publishing. Специалист редактирует local patch Variant: собственные schema selections, additions/explicit overrides и inherited exclusions. Schema selection заменяет весь inherited состав logical SchemaId, остальные схемы наследуются. Live preview строится из F07 и patch, не является историческим snapshot. Demo не зависит от полного Publishing.

Publishing владеет assignment по stable ProductVariantId. Source applied parent ProductId/lifecycle принадлежит CatalogAccess F06. F08 не копирует Product assignment в Variant, не изменяет source parent, не создаёт ContentSlot, release/revision/activation или Delivery. F09 фиксирует immutable local patch release, F10 строит immutable materialized revision из выбранных releases; live preview F08 не заменяет их.

Физические слои `ContentManager.Publishing.Domain`, `.Application.Abstractions`, `.Application`, `.Api`, `.EntityFramework`; целевой `.EntityFramework.Persistence` размещает query/repository adapters по S20. Сейчас [ProductVariantContentAssignment](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Domain/Assignments/ProductVariantContentAssignment.cs) хранит общую коллекцию Items/Exclusions, explicit override target отсутствует; [PublishingService](../../../../contentmanager.api/src/App/ContentManager.Application/Publishing/PublishingService.cs) работает в App с foreign DbContexts и общей Variant publication Revision. Имена новых типов ниже — целевые contracts. Source/target сверка находится в Q15/Q20; наличие current release preview не доказывает рабочий live preview.

Own/parent Archive допускает только ограниченное обслуживание по [F06 §2.3](F06-product-catalog.md#23-инварианты-и-constraints), без добавления новых позиций/expansion; replacement остаётся new use и требует current eligibility. Open conflict оставляет ordinary Save blocked; отдельная Publishing corrective [F15 DF07](F15-catalog-conflicts.md#df07-local-repair-boundary) разрешает только remove conflicting rule/local addition, eligible problematic replacement и inherited exclusion, с intrinsic/new-use/scope/no-expansion guards. Она binds ConflictId/newest SourceVersion/captured repair basis + patch If-Match и коммитит только patch, затем требуется отдельный CatalogAccess Resolve. Standard workspace остаётся applied; repair preview отдельно applied/prospective newest-parent/proposed repair. Missing trusted target A сохраняет local B, independent prospective B плюс A → B остаётся concrete duplicate blocker.

## 2. Domain

### 2.1. Aggregates, entities и value objects

| Тип | Роль | Владелец / граница |
| --- | --- | --- |
| ProductVariantId; reference IDs | Strong IDs; reference IDs переиспользуют F07, поля не дублируются | Publishing references без зависимости на чужой Domain root |
| SchemaContentSelection | Exact schema target + AllExcept/SelectedOnly | Поля определены в F07; наличие в Variant patch означает whole-schema replacement |
| PublishedInstanceVersionReference | Immutable exact reference | Переиспользует [F07 модель](F07-product-content-assignment.md#21-aggregates-entities-и-value-objects); authoritative ContentModeling facts |
| VariantContentAddition | Owned value | ProductVariantContentAssignment |
| VariantContentOverride | Owned intent value | Root; target/result различны по роли, даже при том же logical ID |
| SchemaExclusion | Owned rule value | Только inherited слой |
| InstanceExclusion | Owned rule value | Только inherited слой |
| ProductVariantContentAssignment | Mutable aggregate root | Publishing; один patch на Variant |
| WorkingCompositionInput | Immutable application/domain input | На время одного расчёта; не исторический root |
| WorkingCompositionResult | Pure result | Preview, не DbSet и не PublicationRevision |

**ProductVariantId**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Value | `Guid` | Да | Stable Variant reference identity | Non-empty; source CatalogAccess, move не меняет owner |

**VariantContentAddition**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Result | `PublishedInstanceVersionReference` | Да | Exact local result | Reference fields F07; no nullable target; unique result EntityInstanceId across local groups |

**VariantContentOverride**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| TargetEntityInstanceId | `EntityInstanceId` | Да | Inherited target intent identity | Unique target within patch; не результат B identity |
| TargetEntitySchemaId | `EntitySchemaId` | Да | Captured authoritative logical target schema | Backend подтверждает; хранится при target absent base; same logical Schema с Result |
| Result | `PublishedInstanceVersionReference` | Да | Exact local result | F07 reference; unique result across additions/overrides; no latest; same-instance override допустим |

**SchemaExclusion**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SchemaId | `EntitySchemaId` | Да | Stable rule schema identity | Unique in collection; inherited-only; не удаляет instance rule/local results |

**InstanceExclusion**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| EntityInstanceId | `EntityInstanceId` | Да | Stable inherited instance rule | Unique in collection; inherited-only; coexistence с schema/local rules |

**ProductVariantContentAssignment**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductVariantId | `ProductVariantId` | Да | Root semantic identity | Один patch на Variant; нет mutable stored ProductId |
| SchemaSelections | `IReadOnlyCollection<SchemaContentSelection>` | Да | Own schema replacements | Unique logical SchemaId; не копия Product; full target fields F07 |
| Additions | Уточняется (readonly collection VariantContentAddition) | Да | Local additions | Non-null, empty допустим; intrinsic result uniqueness |
| Overrides | Уточняется (readonly collection VariantContentOverride) | Да | Explicit target/result intents | Non-null, empty допустим; target/result cross-group uniqueness/same-schema |
| SchemaExclusions | Уточняется (readonly collection SchemaExclusion) | Да | Inherited-only schema rules | Non-null, empty допустим; stable-ID uniqueness; redundant rules сохраняются |
| InstanceExclusions | Уточняется (readonly collection InstanceExclusion) | Да | Inherited-only instance rules | Non-null, empty допустим; coexistence |
| Version | `int` | Да | Own internal patch token | No DTO; whole-patch conditional write; base update не bump |
| UpdatedAt | `DateTimeOffset?` | Условно | Optional last-save audit | Server clock; profile Q15/Q20; no-op convention не выбрана |

**WorkingCompositionInput**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Stable VariantId (имя уточняется) | Уточняется | Да | Composition owner context | Не historical root; exact carrier Q15/Q20 |
| Applied ProductId (имя уточняется) | Уточняется | Да | Current applied base owner | Pending source parent не base; Q20 captured consistency |
| Captured Product assignment references (имя уточняется) | Уточняется | Да | F07 coherent whole-set input | Reference model F07; no tracked mutable sharing |
| Local patch (имя уточняется) | Уточняется | Да | Captured schema selections и legacy patch | Не full inherited copy; exact carrier Q15/Q20 |
| Input freshness (имена уточняются) | Уточняется | Уточняется | Captured input context | Отдельно от content/root ETag; Q15/Q20 |

**WorkingCompositionResult**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Effective exact references (имя уточняется) | Уточняется | Уточняется | Surviving inherited + local results | Stable ID/same-schema/no dedup; invalid composition не fake valid set |
| Provenance (имя уточняется) | Уточняется | Уточняется | Inherited/addition/override origin, target отдельно result | Exact shape Q15 |
| Excluded inherited entries (имя уточняется) | Уточняется | Уточняется | Derived excluded view | Не persisted patch content copy |
| Rule diagnostics (имя уточняется) | Уточняется | Уточняется | Missing target/redundancy warnings | Intent остаётся; Q07/Q15 |
| Collision issues/validity (имена уточняются) | Уточняется | Уточняется | Typed participating IDs и composition validity | Intrinsic conflict отдельно live base collision; Q07/Q15 |

Root не хранит свой изменяемый ProductId: current base выбирается по applied CatalogAccess relation. Move не создаёт новый assignment, не очищает patch и не заменяет source fields локальным override. `TargetEntitySchemaId` подтверждается при создании intent и сохраняется, чтобы same-schema правило переживало отсутствие target в live base. Transport имя target/result fields Q15; client не задаёт target schema произвольно.

Item/result identity — stable EntityInstanceId, exact chosen version pinned. Name/Key не merge identity. Addition не имеет nullable target; override не кодируется совпадением Keys. Same-instance version override A/v2 → A/v5 допустим при same Schema, как и A → B той же Schema. Result в effective composition имеет собственный B ID; target A остаётся provenance, не маскирует B для клиента.

Exclusion — самостоятельный сохраняемый intent. Schema/instance exclusion и local addition/override могут сосуществовать; schema exclusion не уничтожает instance rule автоматически. Excluded rows/labels derived, не новая copy content в patch. Hash, SortOrder, AssetIds/JSON и audit fields из legacy не входят автоматически в минимальный patch: профиль metadata/order/limits Q15.

### 2.2. Методы и переходы

| Тип | Метод | Параметры | Предусловия | Изменение / результат |
| --- | --- | --- | --- | --- |
| ProductVariantContentAssignment | Create | ProductVariantId | Authoritative Variant identity существует; root ещё нет | Empty patch; own initial Version convention |
| VariantContentOverride | CreateFromResolved | Target ID/schema facts, Result reference | Same logical Schema; NEW target в current original inherited layer либо unchanged persisted trusted target evidence при сохранении intent | Immutable explicit target/result, без ContentSlot |
| ProductVariantContentAssignment | ReplacePatch | Четыре validated collections, audit time при profile | Valid intrinsic patch, external new-use/retained/route/block guards | Whole patch replace, без изменения base/history/source parent |
| WorkingCompositionService | Compose | Captured base references + validated patch | Deterministic stable-ID inputs | Pure result/diagnostics, без write/Version increment |

Add/Override/Exclude/Remove в UI — подготовка candidate целого Save. Сервер не требует отдельных per-rule mutation routes. Удаление override не создаёт новую inherited copy: следующая композиция возвращает inherited target лишь при отсутствии exclusion. Missing target — состояние preview, не метод удаления intent. UpdatedAt/Version меняются только по successful mutation/no-op convention Q20.

### 2.3. Инварианты и constraints

1. Один root на Variant. Target override unique; result IDs unique среди всех additions и overrides. Две versions одного result или один B для A и C недопустимы, не LWW.
2. Target/result одной logical EntitySchema; разные SchemaVersion допустимы как exact whole-version замена. Cross-schema replacement — exclusion + addition.
3. Schema/instance exclusions unique в своей коллекции и применяются только к inherited items. Избыточность совместных rules не меняет их смысл и может быть non-blocking warning.
4. Новый override может адресовать только current inherited position исходного Product слоя; target из local result или произвольного known ID недопустим. Ранее сохранённый trusted intent при отсутствии target в Product assignment сохраняет schema evidence, result остаётся local. Target вернулся → прежний intent снова подавляет inherited target. Latest result не подставляется.
5. Обычный Product base update меняет preview, но не переписывает patch/own Version и сам по себе не открывает F15 conflict.
6. Collision только с текущей live Product базой, например independent inherited B + override A → B, является ReleaseBlocker этого derived basis и не блокирует Save intrinsically valid patch. Intrinsic duplicate targets/results, local+override duplicate, cross-schema, invalid IDs и неподтверждённый target context являются SaveBlockers. Нельзя silently dedup/LWW. F09 валидирует выбранный immutable ReleaseValidationContext; F10 — свои выбранные sources.
7. Новая позиция/result либо замена exact result проверяется как F07/F04 current new-use. Unchanged saved exact results после Retired/Withdrawn/Archived/unavailable сохраняются при Save с ReleaseBlockers. Display reorder не new use. Для inactive/archived owner разрешён maintenance по смысловому эффекту: Remove проблемной существующей reference и eligible Replace существующей позиции. Inherited-only exclusion может удалить существующую проблемную inherited позицию; новый override может заменить такую current inherited позицию допустимым result. Добавление rule row само по себе не expansion, но unrelated новые rules, additions и расширение content positions запрещены. Все intrinsic/new-use/current-basis guards сохраняются. OPEN F15 блокирует ordinary maintenance Save; repair bypass Q25 не вводится.
8. F15 scoped blocker запрещает affected mutations. Pending source move не меняет applied parent/current base; historical release/revision/Delivery не отзывается F08.

[S21](../standards/S21-aggregate-constraints.md): `ProductVariantContentAssignmentConstraints` — целевой контейнер ID/collections/rule constraints; reuse exact reference constraints F07. Domain/FV/EF согласуют uniqueness/required fields, не объявляют независимые limits. MaxCount/order/metadata profiles остаются Q15. Нельзя сохранять unique по string Keys как substitute ID invariant.

### 2.4. Domain events

Save F08 не публикует release/activation/Delivery events и не запускает automatic promotion. Preview pure, без domain event для логирования. Internal setup после source Variant требует выбранного recoverable trigger Q20; Domain event сам по себе не доказывает durable delivery. Root setup не создаёт automatically policy/publication state соседних фич. F06 move/F15 resolution остаются их workflow.

## 3. Application.Abstractions

### 3.1. Commands

| Command / use case | Result | Concurrency |
| --- | --- | --- |
| UpdateProductVariantContentAssignmentCommand | Saved patch DTO + committed own Version metadata; preview с явно определённой input consistency Q20 | Own assignment If-Match; applied-parent/lifecycle/F15 guard отдельно |
| EnsureProductVariantContentAssignment | Existing/created root, internal outcome | Idempotency stable VariantId + unique key, не user If-Match |

**UpdateProductVariantContentAssignmentCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductVariantId | `Guid` | Да | Stable patch owner | Route-bound identity |
| RouteProductId | `Guid` | Да | Проверяемый applied-parent context | Не stored root parent/body source override |
| ExpectedVersion | `int` | Да | Own internal token из If-Match | Не DTO/body; base/parent guards отдельно |
| Normalized patch request (имя carrier уточняется) | Уточняется | Да | Schema selections и четыре legacy-группы §3.3 | Whole patch; normalized exact fields ниже; carrier Q15 |

**EnsureProductVariantContentAssignment**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductVariantId | `Guid` | Да | Stable internal setup owner | Idempotency unique key; existing patch не reset после move |
| Trigger metadata (имена уточняются) | Уточняется | Уточняется | Recoverable setup input | Q20; не HTTP/create source command |

RouteProductId — проверяемый context, не stored root parent и не request source override. ExpectedVersion из header, не DTO/body. Commands в Publishing.Application; carrier Version внутренний.

### 3.2. Queries

| Query / logical use case | Result |
| --- | --- |
| GetProductVariantContentAssignmentQuery | Saved patch/read context + computed live preview + own ETag metadata согласно Q15 representation profile |
| EvaluateVariantWorkingPatch | Candidate preview/diagnostics, no writes; внешний transport не выбран |
| ContentModeling public resolution | Authoritative stable identities/schema/availability/display/guard facts |

**GetProductVariantContentAssignmentQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| VariantId | Уточняется | Да | Stable assignment identity | Exact carrier naming/type Q15; route Guid context |
| RouteProductId | Уточняется | Да | Applied-parent route context | Q15 mismatch; pending relation не base |

**EvaluateVariantWorkingPatch**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| VariantId | Уточняется | Да | Candidate owner | Exact carrier/transport Q15 |
| Parent context (имя уточняется) | Уточняется | Да | Current applied parent | Q15/Q20 |
| Candidate patch (имя уточняется) | Уточняется | Да | Schema selections и четыре legacy-группы §3.3 без записи | Request не persisted root; Q15 |
| Input freshness (имена уточняются) | Уточняется | Уточняется | Candidate/base/context capture | Q15/Q20; external transport не выбран |

**ContentModeling public resolution input**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Result version IDs (имя уточняется) | Уточняется | Да | Batch exact selected results | Reference contract F07; не latest |
| Target instance IDs (имя уточняется) | Уточняется | Условно | Target/schema authority для overrides | Existing trusted evidence vs missing source Q15/Q07 |
| Exclusion IDs/context (имена уточняются) | Уточняется | Условно | Rule scope/display authority | Exact carrier Q15; inherited-only intent |

Evaluate — необходимая граница для authoritative preview несохранённых изменений, не утверждённый HTTP POST. Без выбранного контракта UI показывает candidate intent и прежний saved preview с подписью; браузер не объявляет локальный merge authoritative. Base/input tokens не подменяют own assignment ETag. Picker reads reuse F04.

### 3.3. DTO и parameter models

Минимальный смысловой composition/read contract ниже; exact generated names/nullability/diagnostic shapes/input stamps — [Q15](../questions/Q15-api-and-source-code.md#f08-patch-preview-и-parent-route).

| Model | Назначение |
| --- | --- |
| UpdateProductVariantContentAssignmentRequest | Собственный F08 contract; exact read shape Q15 |
| VariantOverrideRequest | Собственный F08 contract; exact read shape Q15 |
| ProductVariantContentAssignmentDto | Собственный F08 contract; exact read shape Q15 |
| Addition read item | Result reference/read metadata F07; ID результата не target; поля переиспользованной модели не дублируются |
| Override read item | Собственный F08 contract; exact read shape Q15 |
| Schema/Instance exclusion read item | Собственный F08 contract; exact read shape Q15 |
| WorkingCompositionPreviewDto | Собственный F08 contract; exact read shape Q15 |

**UpdateProductVariantContentAssignmentRequest**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SchemaSelections | `IReadOnlyList<SchemaContentSelectionInput>` | Да | Own whole-schema replacements | Input model F07; empty допустим; conflict с legacy rules той же схемы rejected |
| AdditionEntityInstanceVersionIds | `IReadOnlyList<Guid>` | Да | Exact local result versions | Non-null; empty допустим; non-empty/unique IDs |
| Overrides | `IReadOnlyList<VariantOverrideRequest>` | Да | Explicit target/exact result intents | Non-null; empty допустим; unique target/result rules |
| SchemaExclusionIds | `IReadOnlyList<Guid>` | Да | Stable schema rules | Non-null; empty допустим; unique IDs; inherited-only |
| InstanceExclusionIds | `IReadOnlyList<Guid>` | Да | Stable instance rules | Non-null; empty допустим; unique IDs; inherited-only |

**VariantOverrideRequest**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| TargetEntityInstanceId | `Guid` | Да | Explicit inherited target | Non-empty; schema backend authority, no ContentSlotId |
| EntityInstanceVersionId | `Guid` | Да | Exact result version | Non-empty; backend resolves result identity/schema, same logical Schema |

**ProductVariantContentAssignmentDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductVariantId | `Guid` | Да | Stable saved patch owner | No internal Version/Revision |
| SchemaSelections | `IReadOnlyList<SchemaContentSelectionDto>` | Да | Own schema replacements | F07 §3.6; unique logical SchemaId; отдельно от inherited exclusions |
| ProductId | `Guid` | Условно | Applied parent при resolved context | No-applied/readiness shape Q15/Q20; pending не подмена |
| Additions | Уточняется | Да | Saved addition read collection | Shape Q15; authoritative refs/read metadata, no inherited copy |
| Overrides | Уточняется | Да | Saved target/schema/result read collection | Captured target intent сохраняется при absent base; Q15 |
| SchemaExclusions | Уточняется | Да | Saved schema rule collection | Stable IDs; Q15 read metadata |
| InstanceExclusions | Уточняется | Да | Saved instance rule collection | Stable IDs; Q15 derived effects |
| Live preview/summary/blockers (имена уточняются) | Уточняется | Уточняется | Derived current composition | Shape/ETag representation Q15; root token не доказывает unchanged preview |

**Override read item**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Target ID (имя уточняется) | Уточняется | Да | Stored explicit target | Domain TargetEntityInstanceId authority; wire Q15 |
| Captured target SchemaId (имя уточняется) | Уточняется | Да | Stored logical target schema | Не client field; target absence не loss intent |
| Result reference (имя уточняется) | Уточняется | Да | Exact F07 reference/read metadata | Не target identity; wire Q15 |
| Target display/found-in-base summary (имена уточняются) | Уточняется | Уточняется | Derived diagnostic read metadata | Отдельно от stored intent; fallback/shape Q15 |

**Schema/Instance exclusion read item (exact model names Q15)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Stable rule ID (имя уточняется) | Уточняется | Да | SchemaId либо EntityInstanceId своего вида rule | Domain model §2.1; wire shape Q15 |
| Display metadata (имена уточняются) | Уточняется | Уточняется | Rule labels | Q15 missing/fallback; stable ID сохраняется |
| Derived effect (имя уточняется) | Уточняется | Уточняется | Current inherited effect | Не persisted authority |
| Redundancy/missing-in-base warning (имена уточняются) | Уточняется | Уточняется | Non-blocking diagnostic при потребности UI | Q15; rule не удаляется автоматически |

**WorkingCompositionPreviewDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Effective references/provenance (имена уточняются) | Уточняется | Уточняется | Inherited/local addition/override result | WorkingCompositionResult §2.1; exact wire Q15 |
| Excluded inherited entries (имя уточняется) | Уточняется | Уточняется | Derived exclusion view | Не materialized historical content |
| Typed issues/collision paths/participating IDs (имена уточняются) | Уточняется | Уточняется | Validity/diagnostics | Q15/Q07; no dedup/LWW |
| Captured base/parent freshness context (имена уточняются) | Уточняется | Уточняется | Inputs конкретного расчёта | Q20; root token не input stamp |

All-empty four arrays означает empty patch; не unknown/null PATCH. Отсутствие collection/duplicate exact IDs — validation profile Q15. Из override request backend восстанавливает target SchemaId и result identity/schema; из version ID — exact instance/schema version IDs. Client не передаёт labels, content, hash, result EntityInstanceId/SchemaId, ContentSlotId, Version/Revision, F09 base release ID или materialized effective list.

Internal `Version` не входит public DTO. Preview input context отличается от technical root token: GET/preview может измениться из-за F07 base, не меняя patch ETag. Scope strong ETag и включение derived preview в representation требуют явного Q15/Q20 решения; нельзя объявлять own token доказательством неизменности всего multi-input preview. Existing missing/retired labels показываются с stable ID/issue по profile, не исчезают из массивов.

Diagnostics переиспользуют логическую [AssignmentValidationDiagnostics F07](F07-product-content-assignment.md#33-dto-и-parameter-models). SaveBlockers — intrinsic/target-context/new-use/maintenance/OPEN F15 ошибки; ReleaseBlockers — retained unavailable dependencies и collision только с конкретной live базой; live-collision issue не global CanCreateRelease и не переносится в F09 как безусловный запрет для другого ProductRelease/EmptyBase; Warnings — target absent для persisted intent, redundant/no-effect exclusions. Каждая issue адресует группу/target/result/field и участвующие IDs; exact machine DTO Q15. InputBasis содержит captured patch/base generations и applied parent/current facts отдельно от own patch ETag. Read outage не превращается в explicit empty valid base или зелёный preview.

### 3.4. Ports

| Port | Назначение | Направление |
| --- | --- | --- |
| ProductVariant assignment repository | Tracked root с четырьмя child groups; Add internal setup; local UoW | Outbound → Publishing.Persistence |
| IContentModelingPublishingQueries | Batch Published results, target logical identities/schema и exclusion scope facts; existing ref enrichment | Outbound public ContentModeling, reuse F07 boundary |
| ICatalogPublishingQueries | Stable Variant identity/applied parent/source eligibility/F15 blocks | Outbound public CatalogAccess |
| Product assignment read boundary | Current ProductContentAssignment references + captured own version/context | Внутри Publishing, reuse F07, no foreign SQL |
| Working composition service | Pure compose captured immutable inputs | Domain/Application, без storage/transport |
| Reference-use/move coordination | Защитить new-use, parent route/patch Save против lifecycle/move/F15 races | MVP gate §7.2 + public facts F04/F06/F15; carrier Q15; не foreign DbContext |
| Move impact / dependency read | F08 patch evidence/compatibility для F06/F15; references для retention checks | Inbound Publishing public query contract |
| MVP consistency guard / owner UoW boundary | Один global transaction exclusive gate по Q20, owner-only atomic commit и independent primary public reads | Shared infrastructure adapter; owner connection, не foreign transaction |

Move-impact port не сохраняет source parent, не меняет patch автоматически и не трактует обычный F07 base update как catalog move. Signature/guard fact carrier уточняются Q15; write ordering задан gate §7.2. Server clock/audit reuse F07; отдельных adapters для bytes/hash/S3 в F08 нет.

### 3.5. Specifications / read contracts

[S19](../standards/S19-specifications.md): Variant assignment by stable VariantId specification и Product assignment by applied ProductId; atomic ID conditions reusable, tracking only mutation. Query adapter читает schema selections и четыре legacy-группы и own token согласованно, а не N+1/reconstructed full base copy. Result/target/exclusion facts разрешаются batch только по relevant IDs. Count/pagination для picker принадлежат F04; local patch subset не пересохраняется как весь набор при client filter.

Captured base/patch inputs передаются pure composer; читатель не загружает releases/policy/history для saved working preview. Read consistency разных roots/current parent и refresh detection — Q20. Optional presentation grouping по Schema/labels не влияет на stable matching или content order; default ordering/detail limits Q15. No invented `spec.And` и no shared mutable preview store.

### 3.6. Schema tree и conversion contracts

Новые generated request/result types фиксируются ниже; shared models определены в F07 §3.6. Enum string values/error codes/options — те же, отдельный Variant codec не вводится. POST preview/schema-instances/query/conversion-preview используют full owning route и таблицу F07 §5.3. Read DTO SchemaSelections имеет IReadOnlyList<SchemaContentSelectionDto>; public inherited rule collections остаются отдельными.

**VariantSchemaTreeQueryRequest**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Candidate | UpdateProductVariantContentAssignmentRequest | Да | Whole dirty/saved patch | SchemaSelections + legacy groups |
| Query | SchemaSelectionTreePageQuery | Да | Общий F07 tree query | Basis включает live Product assignment/resolution |

**VariantSchemaConversionRequest**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Candidate | UpdateProductVariantContentAssignmentRequest | Да | Whole исходный patch | Не mutate |
| SchemaId | Guid | Да | Меняемая logical schema | Matching stable ID |
| Target | SchemaSelectionConversionTarget | Да | Inherit/AllExcept/SelectedOnly | F07 target; Own suppresses all inherited versions |
| ExpectedBasisHash | string | Да | Current full preview evidence | Mismatch →409 |

**VariantSchemaConversionDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Candidate | UpdateProductVariantContentAssignmentRequest | Да | Предлагаемый generated patch | Competing rule removals отражены в Diff |
| Preview | SchemaSelectionPreviewDto | Да | Новый preview | Same models F07 |
| Diff | SchemaSelectionConversionDiffDto | Да | Полный change review | F07; не page-local diff |

**GetVariantSchemaSelectionPreviewQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | Guid | Да | Applied parent route | CatalogAccess check |
| VariantId | Guid | Да | Exact owner | Не body override |
| Candidate | UpdateProductVariantContentAssignmentRequest | Да | Whole patch | Read-only |

**GetVariantSchemaInstancesQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | Guid | Да | Applied parent | Current context |
| VariantId | Guid | Да | Exact owner | Current access |
| Request | VariantSchemaTreeQueryRequest | Да | Candidate/scope/page | F07 shared paging |

**PreviewVariantSchemaConversionQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | Guid | Да | Applied parent | Current context |
| VariantId | Guid | Да | Exact owner | Current access |
| Request | VariantSchemaConversionRequest | Да | Full conversion input | No mutation |

Persistence query handlers сначала batch-resolve source Product rules и current patch под coherent read basis. Schema replacement удаляет logical группу до legacy composition. Conversion service в Application — pure transformation над authoritative resolved input; не пишет DbContext и не вызывает Save. Inherit→Own materializes полный соответствующий target набор, несовместимые старые targets/removals видны в Diff; AllExcept→SelectedOnly pins весь resolved набор, включая проблемные refs с diagnostics, а не silently filtered eligible subset. CanApply=false при Save/Error. SelectedOnly→AllExcept использует явный empty exclusions (либо explicit переданный список), не автоматически вычисленный complement. Own→Inherit удаляет selector; ранее удалённые правила не восстанавливает из скрытой памяти.

EF mapping использует F07 shared selector rows/constraints с owner_fk=product_variant_content_assignment_id. Inherited exclusions/additions/overrides остаются существующими отдельными owned collections и не участвуют в новых AllExcept exclude tables. Whole-patch validator отклоняет same-Schema конкуренцию, mapper не нормализует её. Migration предписана F09: legacy local items не превращаются в replacements, неоднозначные intents останавливают preflight. Host регистрирует общие resolver/evidence/options и Variant handlers; schemas/parents читаются только через public boundaries.

## 4. Application

### 4.1. Handlers / use cases

Mutation handlers соблюдают порядок §7.2: shape/auth preparation → own ReadCommitted transaction → отдельный global gate statement → fresh tracked own roots + independent primary public facts → own token/basis/domain checks → own state/intents commit. Предварительные reads/CPU results не являются authority после ожидания gate.

Update handler и internal setup находятся в Publishing.Application; saved-patch/working-preview query handlers — в Publishing.EntityFramework.Persistence. Api dispatches route/body/header, не пишет patch и не знает правил композиции. Current App PublishingService не остаётся параллельным вторым алгоритмом.

Update читает root/current patch/own Version, current applied Variant relation, parent и F15/eligibility facts. Проверяет route parent, own precondition, batch-resolves exact results и новые/изменённые target/rule IDs, классифицирует retained/removed intents, подтверждает same-schema/intrinsic uniqueness и delta new-use/maintenance. Новый target подтверждается только исходным inherited слоем; для retained absent target используется evidence own root. Collision только с live base не SaveBlocker. Затем ReplacePatch и local atomic commit возвращают saved patch + resulting own token; derived preview относится к captured inputs по Q20. No GetWorkspace после commit, source mutation или release creation.

Query читает saved patch и live Product assignment текущего applied parent, разрешает read metadata и pure-composes preview/diagnostics. Missing target, redundancy и ordinary base collision не бросают GET как «assignment отсутствует». Preview не увеличивает Version/UpdatedAt. Internal setup идемпотентен по VariantId; unresolved relation/readiness и recoverable trigger Q20. Applied move сохраняет root/patch; pending move F15 оставляет рабочую базу прежней.

Candidate preview use case принимает schema selections и четыре legacy-группы без записи, проверяет структурный shape, resolves needed facts и compose. Exact error/envelope/transport/per-candidate freshness ещё Q15; выбранный dirty preview не заменяется F10 release preview с ActiveReleaseId.

### 4.2. Validators

[S18](../standards/S18-fluent-validation.md): UpdateVariantPatchRequestValidator композирует stable-ID validators, AdditionResultIdsValidator, VariantOverrideRequestValidator, SchemaExclusionIdsValidator и InstanceExclusionIdsValidator. Lists required, empty допустим; each ID non-empty; duplicates в своей rule/request group structural issue. Limits reuse Domain constraints только после выбора Q15. Internal command проверяет expected token/context отдельно.

Pure FV не читает БД, не выбирает latest и не удаляет redundant rules. Same-schema, result cross-group duplicates по authoritative EntityInstanceId, new-use facts и F15 scope проверяет handler/Domain. VariantPatchIntrinsicValidator выявляет duplicate targets/results, cross-group local+override duplicates и cross-schema; VariantOverrideTargetContextValidator подтверждает NEW target в original inherited layer и не доверяет UI schema. AssignmentChangeClassifier F07 классифицирует exact retention/replacement; VariantMaintenanceGuard сравнивает смысловой content effect с исходным captured basis: разрешает removal проблемной inherited reference через exclusion и eligible replacement через override current inherited target; запрещает unrelated rule/content expansion inactive/archived owner, не считает rule count единственным критерием. Different version IDs одного result instance не считаются разными позициями. Exclusion + local same-ID/schema/override не отвергается валидатором как overlap.

### 4.3. Mappers

| Mapping | Владелец | Правило |
| --- | --- | --- |
| HTTP route/body/If-Match → command | Api | Route ProductId только context; own expected int из tag; четыре явные groups |
| Exact result version facts → reference | Application | Real result ID и exact schema target, no client-supplied identity/hash |
| Target facts → override intent | Application | Capture authoritative logical target SchemaId; unchanged saved evidence trusted from root, не от UI |
| Patch + captured base → preview | Pure service + read mapper | Stable identity; provenance/diagnostics; отсутствие target не remove intent |
| Committed patch/read facts → DTO + header carrier | Result/query mapper | Own token отдельно; derived input context explicit; no later workspace read с чужими mutations |

Нельзя реконструировать explicit A→B из coincident Keys старого Item — там нет target authority. Release/canonical hash mapping F09/F10 отдельны; generated request не становится Domain entity и не получает сохранённый full inherited состав.

### 4.4. Services / algorithms

WorkingCompositionService работает с captured typed values, не с tracked roots. Алгоритм не зависит от порядка массива overrides и не меняет исходные collections:

1. Взять current Product assignment refs, индексировать по EntityInstanceId; нарушение уникальности исходного base возвращает issue, не Distinct.
2. Удалить из inherited candidates refs matching SchemaExclusions по EntitySchemaId, сохранив excluded provenance.
3. Затем применить InstanceExclusions по EntityInstanceId к оставшемуся inherited слою. Redundant/no-current-effect rules сохранить в patch и read diagnostics.
4. Для всех overrides одновременно сопоставить target/schema evidence с **исходным inherited** слоем до exclusions/overrides. Для NEW intent target обязан присутствовать в этом исходном слое; для persisted trusted intent отсутствие допустимо. Определить target IDs и удалить только surviving inherited refs этих IDs; local results ещё не внесены.
5. Добавить каждый exact override result ровно один раз как VariantOverride с target provenance. B сохраняет собственный ID, в том числе когда target отсутствует или уже excluded. Это не cascading rewrite: result одного intent не становится inherited target другого.
6. Добавить exact additions как VariantAddition. Для каждого result проверить uniqueness относительно surviving inherited и всех local results; collision содержит participating IDs/rules, не last write wins.
7. Вернуть effective refs либо diagnostic candidates/ambiguity без ложного «валидного итогового состава», excluded summary и warnings/issues. Deterministic presentation/order profile Q15; никакого materialized revision/hash/commit.

Valid same-instance override A→A/v5 suppresses inherited A перед вставкой exact local A/v5 и не считается duplicate случайно. Independent surviving B + A→B collision недопустим. Если B excluded из inherited layer, local B не запрещён самим exclusion. Несколько local rules на один result остаются intrinsic conflict независимо от base. Связанные intents A→B и B→C проверяются против original inherited A/B одновременно: local B не становится target следующего override. Cycles A→B/B→A не имеют отдельного запрета, если targets/results unique и выполнены same-schema/current eligibility/прочие guards. Перестановка массива не меняет результат; issue carrier уточняется Q15.

Base update F07 пересчитывает result при новом чтении/candidate evaluation; patch не переписывается. Composer core может переиспользоваться F10, но input source/lifecycle/empty/order/canonical profiles различаются: F08 работает с mutable working inputs, F10 — immutable releases и strict create validation. Полученный preview не authority последующего Save/release/revision. Content/new-use/reference-use и parent writes сериализует MVP gate §7.2; обычное preview read commit permit не даёт.

### Schema selections по физическим слоям

Domain reuse SchemaContentSelection F07; own SchemaSelections хранится отдельно от inherited SchemaExclusions/InstanceExclusions. Candidate не содержит selector и legacy addition/override/exclusion той же схемы одновременно: это intrinsic ambiguity, а не last-write-wins. Existing patches не преобразуются в replacements молча. При mode switch UI явно показывает удаляемые правила, server whole-patch Save проверяет полученный candidate. Удаление selector возвращает наследование с явно оставшимися правилами.

Application.Abstractions добавляет SchemaSelections в patch command/read contracts; exact carriers/wire — [Q39](../../../business/questions/Q39-schema-content-selection.md). Application batch-validates target/pins/exclusions через F07 public port. Pure composition получает уже resolved local schema selections: из original inherited layer удаляет все позиции SchemaId replacement-mask независимо от версии структуры, применяет остальные inherited exclusions/override suppression и затем добавляет frozen/resolved local results. Existing A→B semantics сохраняются вне заменяемых схем; final EntityInstanceId уникален.

EntityFramework сохраняет owned rules/includes/excludes отдельно от inherited exclusion tables; Persistence query adapters используют public F07 resolution и latest live Product basis только для authoring preview. F09 release review использует explicit immutable ProductRelease/EmptyBase. Host связывает общие F07 resolver contracts без cross-BC DbContext. API/NSwag передают schema-mode/scope и раздельные exclusion kinds; codec/paging определены §3.6 и F07 §5.3. Архив/F15 ограничения продолжают действовать, режим не даёт обхода new-use/no-expansion.

Приёмка: inherited 20 + own SelectedOnly 2 → ровно 2; own AllExcept exclusions не затрагивают другие схемы; удаление selector возвращает inherited layer; смешанные rules одной schema rejected; frozen VariantRelease не расширяется при новых публикациях. Unit проверяет pure composition, integration — whole-patch persistence/basis и отсутствие silent legacy conversion, HTTP — field-addressed conflicts. Нет новой шины/event только ради preview.

## 5. Api

### 5.1. Endpoints

| Method | Route | Request | Response | Access |
| --- | --- | --- | --- | --- |
| GET | `/api/v1/products/{productId}/variants/{variantId}/content-assignment` | Guid context IDs | ApiResponse<ProductVariantContentAssignmentDto> + own strong ETag, 200 | MVP Specialist read profile Q15/Q10 |
| PUT | Тот же exact route | Patch request §3.3, required If-Match | 200 saved patch DTO + resulting own ETag | MVP Specialist write profile, scoped lifecycle/F15 guards |

Route из прежней F08; ProductId сверяется с **applied** parent CatalogAccess, не newest pending source parent. Exact Variant F06 по stable ID помогает обнаружить canonical context после move. Parent mismatch response/machine code/read redirection profile — Q15; Api не переадресует PUT автоматически на новый Product. No source CRUD/per-rule endpoints, F09 publish-on-save или выбранный POST preview. DF06 candidate-preview transport пока не утверждён.

### 5.2. ETag / If-Match

Own internal int Version → strong numeric assignment ETag по [S16](../standards/S16-rest.md). Technical Version/Revision не сериализуется. PUT requires If-Match assignment: missing428, stale own token412; domain/reference/F15 blocker409. Product/base/policy/publication-state tokens не являются patch token. Successful state-changing Save atomically advances own Version и возвращает результат без hidden result GET.

Parent/base меняются независимо: прежний patch token не доказывает неизменность applied relation или preview. Не вводить mandatory base If-Match/composite token без решения Q15/Q20. Representation boundary для patch ETag и changing derived preview явно согласуется там: одинаковый own token нельзя выдавать за сильный cache validator всего multi-input body без принятого профиля. Root ETag остаётся concurrency contract, не «preview всё ещё безопасен для публикации».

Own race классифицируется после rollback/disposal свежим root read; guard/parent change — другой отказ, не автоматически412. Header grammar/error priority/no-op profile Q15/Q20. 412 preserves edits, explicit reload/reconcile; lost response после commit — unknown outcome, не automatic retry с новым token или новым parent.

### 5.3. OpenAPI и ошибки

Declare request/read models, ETag/If-Match, 200 ApiResponse, 400 ValidationProblemDetails, 404 missing identity/root/reference, 409 intrinsic/availability/F15 conflicts, 412/428 ProblemDetails и 401/403 по MVP access. Parent mismatch/setup pending/preview diagnostic codes и exact error shape Q15/Q20. GET с base-dependent composition issue возвращает читаемый patch/diagnostics, не fake empty; intrinsic bad request Save не partial success.

Read route/headers не выбираются по current `/publishing/variants/...`. Generated NSwag модели/методы/typed header access после backend/OpenAPI; CORS/header result support Q15. Отдельный external preview flow появляется только после выбора контракта DF06, не копированием F10 route/body.

## 6. EntityFramework

### 6.1. DbContext ownership

PublishingDbContext, schema `publishing`, владеет local patch и четырьмя owned groups. F07 base читается внутри Publishing; CatalogAccess/ContentModeling facts через public ports. Cross-BC FK/DbContext/repository dependencies отсутствуют в target. Preview transient: не DbSet и не сохранённый final composition.

Текущий [PublishingDbContext](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.EntityFramework/PublishingDbContext.cs) содержит assignment/release/state groups; это не общий token. F08 не меняет Product assignment, release или Variant active publication state.

### 6.2. Configurations

| Entity | Table / logical representation | Key/index | Concurrency | Mapping |
| --- | --- | --- | --- | --- |
| ProductVariantContentAssignment | publishing.product_variant_content_assignments | VariantId semantic key; one root per Variant | Own Version int EF concurrency token | Stable Guid; optional UpdatedAt; no duplicated source ProductId |
| VariantContentAddition | Owned additions child table | Owner + Result.EntityInstanceId unique | Protected by root.Version | Four exact reference IDs F07 |
| VariantContentOverride | Owned overrides child table | Owner + TargetEntityInstanceId unique; owner+result ID unique | Protected by root.Version | Captured target SchemaId and exact result refs |
| SchemaExclusion | Owned schema exclusions table | Owner + SchemaId unique | Protected by root.Version | Required stable Schema ID, no required source Name/Key |
| InstanceExclusion | Owned instance exclusions table | Owner + EntityInstanceId unique | Protected by root.Version | Required logical ID; display/effective summary not stored as rule authority |

Concrete table/column/check names/layout — Q20. Current [ProductVariantContentAssignmentConfiguration](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.EntityFramework/Configurations/ProductVariantContentAssignmentConfiguration.cs) stores surrogate root Id/unique VariantId, two children Items/Exclusions без own Version. [PublishingOwnedMappings](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.EntityFramework/PublishingOwnedMappings.cs) stores row_id/Keys/hash/SortOrder и Key-based uniqueness. Этот mapping не описывает four-group target и не сохраняет explicit target schema intent.

### 6.3. Constraints / indexes

Stable-ID uniqueness каждой owned group, required reference IDs, owner FK только внутри Publishing. Same-schema invariant хранится/проверяется Domain/authoritative facts; no cross-BC FK к targets/results. Cross-table result uniqueness additions+overrides и root Version guard при конкурирующих writers должны быть обеспечены whole-root atomic replacement, а не уникальным индексом каждой таблицы по отдельности. MVP gate + own conditional Version обеспечивает whole-root serialization; consolidated membership/physical constraints mapping остаются Q20.

Одновременные schema/instance exclusion и local rule разрешены: не constraint «только один из видов правила». Target schema evidence сохраняется при исчезновении target из base. No cascade source deletion на patch. Child remove/add-to-empty защищён own conditional root update; EF delete/insert order/unique conflicts проверяются PostgreSQL. Root physical surrogate допустим при single semantic identity, не новый пользовательский ID или новый assignment после move.

## 7. EntityFramework.Persistence

### 7.1. Repositories / query adapters

Host/DI задаёт owner-partitioned UoW participants: в transaction входят только DbContext/own outbox/inbox данного BC. Текущий EfCoreUnitOfWorkParticipant, способный объединять несколько DbContexts в общей transaction, не является target coordination adapter: participant selection разделяется по owner BC, foreign public read adapter создаёт независимый primary scope/connection без ambient enlistment. Guard adapter использует owner connection/transaction; mapping/repository другого BC в command handler не внедряется.

Repository загружает root с четырьмя child groups tracked для whole-patch Save. Read query adapter captured saved patch/base/token и pure-composes, enriches IDs батчами. Repositories не создаются на каждый rule; filters in UI не определяют набор для записи. Public dependency/impact adapter выдаёт stable patch facts F15/F04 через accepted contracts.

Новая Publishing.Persistence размещает query handlers/specifications/repositories, Application — update/setup orchestration. DI связывает один accepted путь вместо current App service и нового handler одновременно. Registration/migration references scoped Q20; current [ContentManagerApplicationModule](../../../../contentmanager.api/src/App/ContentManager.Application/ContentManagerApplicationModule.cs) регистрирует IPublishingService как App PublishingService, не target query boundary.

### 7.2. Transactions

По [MVP consistency guard](../questions/Q20-persistence-and-migration.md#mvp-протокол-consistency-guard) каждый invalidating writer начинает **свою** BC transaction с явно заданным ReadCommitted и отдельным первым SQL statement получает один стабильный глобальный exclusive pg_advisory_xact_lock. Lock выполняется на том же открытом owner connection, на котором будут authoritative reads, row locks, запись и commit; общий ключ одинаков во всех репликах CM. До получения gate нельзя authoritative-read/check/row-lock затронутый basis. Затем тот же свежий owner scope на удерживающем gate connection загружает собственные tracked roots и проверяет expected Version; чужие current facts читает только через public read ports в независимом primary connection/scope, без ambient UoW enlistment, foreign EF, shared SQL transaction, cache или read replica. Публичный read carrier остаётся предметом Q15; механизм сериализации уже выбран.

Все способные инвалидировать проверяемый basis writers используют тот же gate: F07 whole Save/setup, F08 patch Save/setup, ContentModeling/Media lifecycle/reference changes, CatalogAccess applied relation/F15 Resolve, F09 CreateRelease/Retire/Restore, F10 CreateRevision/lifecycle/Withdrawal, F11 Select/Activate/Rollback/Unpublish и promotion capture/per-item/stage/late Approval, F12 policy changes, F15 scoped repair, релевантные F17 decisions и Delivery-owned invalidation commits. Каждый изменяет только own BC state. Own state/token, required own outbox, safe audit intent и inbox marker фиксируются атомарно; commit/rollback освобождает transaction lock. Это общий write gate в одной PostgreSQL database между репликами, а не общая транзакция BC.

CPU-подготовка, immutable projection/canonical hashing и предварительные проверки могут быть снаружи gate, но после acquisition весь влияющий на commit basis заново подтверждается до применения подготовленного результата. Gate нельзя удерживать во время UI/человеческого Approval, VR School HTTP, S3/bytes, broker send/ACK, ожидания Delivery projection либо HTTP stream; эти действия и downstream commits имеют отдельные границы.

Acquisition wait ограничен выбранной конфигурацией и cancellation; timeout/cancel/lock failure — технический отказ, не domain Conflict. Failed/partial public port не превращается в empty compatible facts. Потеря owner connection прерывает операцию целиком; reconnect не продолжает прежний basis. Own expected Version mismatch остаётся 412; independent relevant basis drift — 409 с повторной проверкой/review, без автоматического retry с новым ETag. Потерянный commit ACK означает unknown outcome: inquiry по durable operation identity/выбранному Q15 recovery, а не утверждение rollback или слепой повтор.

Read queries, pickers, preview/review не получают этот global gate и не выдают commit permit. Own DTO и token читаются согласованно из одного owner поколения; composite basis/freshness других roots учитывается отдельно. Последующая mutation получает новый gate и перепроверяет basis.

F08 gate покрывает DF02 whole patch Save и DF03 setup. После gate заново читаются own patch, current F07 original inherited layer, applied Catalog parent/F15 и authoritative new-use facts. Обычный F07 base Save не увеличивает F08 Version; live-only collision по-прежнему не SaveBlocker. Scoped F15 corrective patch repair и CatalogAccess Resolve — два независимых owner commits с новым gate для каждого: source v22 после repair-v21 сохраняет committed repair и запрещает Resolve со старым basis.

Local Publishing transaction атомарно фиксирует root Version/audit и все four-group changes. Intrinsic/reference/parent/F15 failure → original patch/token intact. No partial replacement/rule normalization. Cross-BC writes не объявляются частью этой transaction; MVP gate §7.2 защищает reference use и applied-parent/eligibility против concurrent lifecycle/move/Resolve.

F07 base и F08 patch — разные roots. Read preview captures оба и parent context в явно выбранном consistency profile: не смешивать refs двух base saves или выдавать latest base при старом parent без stamps/guards. Точный query isolation/result snapshot/retry profile Q20. Ordinary base update не own stale412 и не patch mutation. Изменение live base не блокирует intrinsically valid Save только из-за collision; NEW target context и external guards проверяются по protected captured basis Q20.

Successful PUT result patch/token относится к его commit, preview — к указанным captured inputs. Поздний GetWorkspace не заменяет result и не приписывает чужой Save этой операции. Lost response after commit оставляет unknown outcome; fresh exact read/explicit comparison, no blind retry. F09 snapshots patch под own source If-Match, F10 validates chosen immutable releases заново.

### 7.3. Migrations

CM без production legacy obligations по Q20; анализ migrations не исполняет. Реализация добавляет own root Version, explicit target/schema fields и four-group storage, меняет Key-based constraints, отделяет VariantPublicationState.Revision. Current Items не содержит target intent: его нельзя автоматически угадать по Keys; нужна выбранная policy пересоздания dev data/transition Q20.

Shared PublishingOwnedMappings обслуживает F07/F09: не менять все immutable release rows как collateral F08 cleanup. Реализовать scoped configurations/migrations/DI и реальную round-trip/atomicity проверку. Initialization/create race one VariantId не создаёт duplicate roots. Catalog move не переносит таблицу в другой owner Product и не reset-ит root.

## 8. External adapters

### 8.1. ContentModeling public contracts

Reuse F07 batch exact reference boundary; дополнительно batch logical target/schema и exclusions scope facts. Для нового override target schema подтверждается сервером; retained missing-from-base intent использует saved authoritative target evidence, не invented placeholder. Missing-from-base и missing-from-ContentModeling различны. Persisted unchanged intent имеет trusted stored evidence и сохраняется с diagnostics; NEW или изменённый target без authoritative original-layer/schema evidence получает SaveBlocker, а не invented placeholder; exact diagnostic carrier Q15. Assets/lifecycle new-use checks авторитетны F04/F05; F08 не uploads/downloads bytes или генерирует новую schema/content version.

### 8.2. CatalogAccess public contracts

Variant stable ID → current applied parent/source eligibility/F15 blockers. Pending target relation не becomes current base. Move-impact adapter Publishing даёт scope/dependency/patch compatibility F06/F15, не source writer. Safe move сохраняет patch; incompatible move F15 keeps old applied context/newest source pending. Full resolution workflow F15 отдельно; обычная смена F07 working base не автоматически catalog conflict.

### 8.3. Internal setup и Host

Idempotent empty Variant assignment setup отдельно от CatalogAccess commit; trigger/storage/delivery Q20. Не предполагать обязательный broker event/queue или fan-out создания policy/publication states. Initial Variant без resolved parent не становится editable working base с fictitious Product; readiness/read-only diagnostic profile Q20/F06. Повтор source snapshots/setup не reset local decisions. Clock/transport infrastructure reuse существующих модулей.

## 9. Data flows

### Транспортные потоки

По [S17](../standards/S17-data-flows.md). DF06 фиксирует незакрытую потребность preview candidate, а не существующий или выбранный endpoint. Public query fields/error profiles F04 переиспользуются, не дублируются как новый API F08.

| Поток | Транспорт | Метод / канал | Endpoint / destination | Доступ | Вход | Application action | Выход | Успех | Ошибки |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| DF01. Saved patch и live preview | HTTP API | GET | `/api/v1/products/{productId}/variants/{variantId}/content-assignment` | MVP read Q15/Q10 | Applied parent context + VariantId | GetProductVariantContentAssignmentQuery | Saved patch, preview/diagnostics, own ETag в profile Q15 | 200 readable working state | Missing/root readiness/parent mismatch Q15/Q20; access/dependency failures |
| DF02. Сохранить whole patch | HTTP API | PUT | Тот же route | MVP write; lifecycle/F15 scope guards | Context IDs, schema selections и четыре legacy-группы request, If-Match | UpdateProductVariantContentAssignmentCommand | Saved DTO + committed own ETag; captured preview profile | 200 после local Publishing commit |400,404,409,412,428; parent mismatch profile;401/403; dependency/storage failure |
| DF03. Internal setup | Internal mechanism Q20 | EnsureProductVariantContentAssignment | Publishing.Application; durable destination TBD | Authoritative Variant identity fact | Stable VariantId, selected trigger metadata | EnsureProductVariantContentAssignment | Existing/empty created root | One root после commit; repeated setup no-op | Readiness/missing facts/race/storage recovery |
| DF04. Найти local result/target context | HTTP API reuse F04 | GET | `/api/v1/entity-instances` | MVP ContentModeling read | Search/schema/availability/page context | ListEntityInstancesQuery F04 DF01 | ApiResponse offset summaries |200, explicit logical choice | Validation/access/storage по F04; no guessed candidate filter |
| DF05. Выбрать exact result version | HTTP API reuse F04 | GET | `/api/v1/entity-instances/{instanceId}/versions` | То же | Logical instance, target/schema/status/page context | ListEntityInstanceVersionsQuery F04 DF04 | Exact version summaries |200, пользователь выбирает ID | Missing/filter/access по F04; selection revalidated Save |
| DF06. Preview несохранённого candidate | Транспорт уточняется Q15 | Метод/канал уточняется; не выбран POST | Destination/URL Q15, без фиктивного route | MVP read/evaluation profile TBD | Variant/applied-parent context + candidate schema selections и четыре legacy-группы | EvaluateVariantWorkingPatch | Pure composition diagnostics/input freshness profile | Расчёт без записи; transport status TBD | Candidate syntax/reference/parent/freshness profile Q15/Q20 |

### DF01. Прочитать patch и current base

| Шаг | Исполнитель | Вход / чтение | Преобразование и проверка | Запись / выход |
| --- | --- | --- | --- | --- |
| 1 | Api | ProductId/VariantId, read context | Binding/access, не setup-on-read | Query |
| 2 | CatalogAccess public adapter | Stable Variant/applied relation/block state | Проверить parent context; pending source relation не использовать как current | Captured applied ProductId/availability/diagnostics |
| 3 | Publishing query adapter | Saved patch four groups/own token, current F07 base по applied ProductId | Consistent snapshots по Q20; absent root/base readiness не превращать synthetic empty set | Typed captured composition inputs |
| 4 | ContentModeling read adapter | Relevant result/target/rule IDs | Batch metadata/availability; missing live-base target отдельно source missing | Display facts; existing intents сохраняются |
| 5 | Pure composer | Captured base+patch | §4.4; excluded inherited, overrides/additions и collision issues | Preview/summary/diagnostics без persist |
| 6 | Mapper/Api | Saved patch, preview input context, own Version | DTO и strong own ETag в согласованном scope Q15; no technical Version body |200; no commit/UpdatedAt/Version change |

Base-dependent collision и target missing не становятся GET failure, hiding rules или 404. Read-only при F15/source restrictions сохраняет patch и applied parent context. Когда F07 base меняется, новый read меняет preview без patch edit; cached/dirty UI требует explicit freshness handling, не inference по одному unchanged ETag.

<a id="df02-сохранить-four-group-patch"></a>

### DF02. Сохранить schema selections и legacy patch

Внутренний write path после Api/FV/проверки caller: открыть owner ReadCommitted transaction, отдельным первым statement получить global pg_advisory_xact_lock на owner connection; только затем выполнять указанные ниже authoritative reads/row locks. Foreign facts — independent primary public ports без enlistment. Все guard/basis checks повторяются для этого commit; §7.2 задаёт failure/recovery и запрет внешнего ожидания под gate.

| Шаг | Исполнитель | Вход / чтение | Преобразование и проверка | Запись / выход |
| --- | --- | --- | --- | --- |
| 1 | Api/FV | Context IDs/body/header | Required If-Match, pure shape/IDs/group duplicates | Command, no writes |
| 2 | Publishing repository | Root/current four groups/Version | Own expected token; missing owner/readiness profile; stale→412 | Current retained intent/evidence для comparison |
| 3 | CatalogAccess public port | Applied relation/eligibility/F15 blocks | RouteProductId mustmatch applied; scoped write allowed; guard не использует source pending parent | Captured context/coordination facts |
| 4 | ContentModeling public port | Requested results; new/changed targets/exclusions IDs | Classify unchanged exact vs new/replaced results; current eligibility только new use; retain unavailable stored IDs. NEW target только original inherited layer; persisted absent target reuses trusted stored SchemaId | Authoritative result/target/rule facts |
| 5 | Application/Domain | Candidate/current IDs | Same-schema; unique targets/results across addition/override; reject untrusted target context; inactive/archived owner semantic removal/eligible replacement only, including scoped inherited exclusion/override; no unrelated expansion; preserve exclusions coexistence | Valid intrinsic patch или полный отказ |
| 6 | Working composer/guards | Captured current base и candidate | Live collision → ReleaseBlockers, intrinsically valid Save allowed; separate SaveBlockers/Warnings with addresses+basis; gate получен до root read; recheck parent/content/F15 basis §7.2 | Candidate+preview tied to inputs, no external root writes |
| 7 | Domain/Persistence | Four validated groups + own expected Version | ReplacePatch, conditional root write и all children/audit в local transaction | Commit patch+own Version; no base/history/release mutation |
| 8 | Result mapper/Api | Saved patch/token и captured facts | DTO/header данного Save; preview freshness profile explicit, no GetWorkspace result read |200 либо unknown outcome при потере ответа после commit |

Own-token match не гарантирует прежний parent/base: safe catalog move может случиться без patch edit, обычный F07 base update тоже не own stale. Parent mismatch/guard race не вызывает automatic resubmit на новый URL. After rollback fresh classification отделяет own412, structural/lifecycle409 и operational failure. Ни один частичный subset не сохраняется после error.

Снятие exclusion или удаление override меняет только patch; inherited target возвращается только через следующую композицию и при отсутствии других exclusions. Existing releases/revisions/history/Delivery не меняются. Repeated source/Published result updates не auto-upgrade. Save подтверждает сохранение рабочего patch и не утверждает release-readiness. Удаление target из live base не стирает persisted intent; collision только с live базой не блокирует Save. NEW intent на отсутствующий target либо неподтверждённое schema evidence блокирует Save.

### DF03. Обеспечить один assignment на Variant

Внутренний write path после Api/FV/проверки caller: открыть owner ReadCommitted transaction, отдельным первым statement получить global pg_advisory_xact_lock на owner connection; только затем выполнять указанные ниже authoritative reads/row locks. Foreign facts — independent primary public ports без enlistment. Все guard/basis checks повторяются для этого commit; §7.2 задаёт failure/recovery и запрет внешнего ожидания под gate.

1. Source Variant fact/выбранный recoverable trigger Q20 инициирует internal setup. CatalogAccess commit отдельно от Publishing commit; initial missing parent не создаёт fake Product/base.
2. Handler читает root по stable VariantId. Existing root, включая filled patch после move/new source snapshot → no reset/Version bump.
3. Absent → Create(empty patch), Add и local commit; unique owner key защищает concurrent creation. Classified unique race → fresh existing root read, не обработка любой ошибки как duplicate.
4. Trigger/work complete только после соответствующего commit; delivery/recovery protocol Q20. Не fan-out setup чужих state/policy roots и не side effect GET.
5. Editing/read preview readiness при unresolved parent/absent F07 base/patch задаётся Q20/F06. Identity не удаляется и не становится новым assignment при parent change.

### DF04. Выбрать logical result/context

1. UI reuse F07 picker/public F04 list, задаёт server search/schema/page/new-use context. Add не ограничен схемой target; Override result ограничен той же logical Schema; replacement выбранной result version фиксирует её logical instance.
2. F04 возвращает filtered summaries, query sorting/page по своей фиче. Для inherited target основной контекст — конкретный base row/current intent, а не произвольный Key match. NEW override target вне current original inherited base запрещён; сохранённый absent-target intent остаётся отдельным trusted intent.
3. Выбор logical instance не отправляет patch mutation и не назначает latest version. Cancel сохраняет локальную форму; target/result IDs передаются picker отдельно.
4. Далее DF05; target display/exact instance при необходимости reuse F04 exact reads, без new target CRUD или all-content preload.

### DF05. Выбрать exact Published result

1. По selected logical ID клиент получает F04 Published version list с необходимым schema target/availability profile. Same logical Schema, а не same SchemaVersion, определяет допустимость override.
2. Пользователь явно выбирает exact version. Confirm сохраняет candidate local result; target intent и unrelated exclusions не исчезают. Cross-schema вариант оформляется другой локальной комбинацией exclusion+addition, не принудительным override.
3. Client не присылает result InstanceId/SchemaId metadata; DF02 разрешает exact ID вновь. Наличие row Published не отменяет current archived/retired/withdrawn/media guard.
4. No automatic upgrade из-за новой Published version; read preview/candidate refresh не заменяют явный выбор. Full version inspection/navigation принадлежит F04, не F08 content editor.

### DF06. Оценить candidate без Save

Контракт external evaluation не выбран. Требуемый logical algorithm:

1. Получить candidate schema selections и четыре legacy-группы и context, проверить shape/access по принятому Q15 profile; отсутствие выбранного transport не разрешает выдумать POST route.
2. Capture current applied parent, F07 base, saved patch/target evidence и needed public facts. Dirty request не обязан соответствовать current persisted patch, но own/local/input freshness различимы Q20.
3. Resolve results/targets/rules и применить pure composition §4.4. Вернуть validity, collisions/excluded/provenance, target missing и redundancy warnings. Допустимость partial diagnostics vs malformed input error — Q15.
4. No writes/Version/UpdatedAt/setup/history. Поздний ответ прежнего candidate/base/context не заменяет UI текущего state; stamp/cancellation profile Q15/Q20.
5. Subsequent Save/release/revision повторно проверяют authoritative inputs. Пока external flow не определён/реализован, local intent editing не объявляется confirmed server preview, а F10 release preview не заменяет live working-base result.

## 10. Ошибки и диагностика

| Состояние / отказ | Реакция | Scope / смысл |
| --- | --- | --- |
| Invalid patch shape/empty required ID/duplicate raw rule |400 ValidationProblemDetails | No partial replacement; exact item/path Q15 |
| Intrinsic duplicate targets/results, cross-schema override, forbidden new reference |409 domain/availability conflict | Same-schema и cross-group uniqueness серверные, не picker-only |
| Missing source identity/root/new reference |404 либо readiness/missing-parent profile Q15/Q20 | Existing absent-from-base target не эта ошибка |
| Parent route mismatch after applied move |Explicit rejection/read canonical context profile Q15 | Old own ETag не право auto-write в new Product |
| OPEN F15 / source lifecycle | OPEN F15 →409 ordinary writes; inactive/archived owner limited maintenance | Remove/eligible Replace без Add/expansion; no Q25 repair bypass; current applied context сохранён |
| Ordinary live-base collision | Save разрешён при остальных guards; ReleaseBlockers данного basis | F09 заново проверяет explicit immutable/empty context; F10 выбранные sources; no dedup/LWW/autoF15 |
| Missing target/redundant exclusion |Readable diagnostic/warning | Intent/result сохраняются, не invalid только по этому основанию |
| Missing/stale own precondition |428/412 | Own patch token, не parent/base/publication token |
| Transient port/DB/guard failure |Rollback до commit; operational error | No false safe facts; no user overwrite retry |
| Lost response после commit |Unknown outcome | Exact read/compare intent, no blind resubmit |

Логи: VariantId, route/applied ProductId, correlation, rule target/result IDs, category/participating IDs/captured input context и commit stage. Без content JSON, credentials, raw activation keys/full payload. Machine codes/issue severity, metadata visibility и preview freshness diagnostics Q15. Event/metric «patch saved» не означает клиентскую публикацию или подтверждённую совместимость любых будущих bases.

Global gate timeout/cancellation/failed port/owner connection loss — технический abort, не domain409 или empty valid facts. Unknown commit ACK требует inquiry/operation identity по Q15; reconnect continuation/blind retry с новым ETag не допускаются.

## 11. Тестирование

Стратегия по [S04](../standards/S04-testing.md)/[S14](../../../catalog/docs/standards/dotnet/testing.md#test-environment-ownership-and-bounded-parallelism); тесты приложения этой аналитикой не запускались. Existing [AssignmentTests](../../../../contentmanager.api/tests/BoundedContexts/Publishing/ContentManager.Publishing.Tests.Unit/AssignmentTests.cs) описывают Key-based replacement и удаление локальных items/exclusions; [PublicationRevisionTests](../../../../contentmanager.api/tests/BoundedContexts/Publishing/ContentManager.Publishing.Tests.Unit/PublicationRevisionTests.cs) проверяют immutable releases/current overlay, а не explicit A→B/live working-base target. [PersistenceTests](../../../../contentmanager.api/tests/BoundedContexts/Publishing/ContentManager.Publishing.Tests.Integration/PersistenceTests.cs) даёт real PostgresSuite fixture. Их наличие не доказывает target patch/preview guards.

Unit SUT: patch Domain invariants, pure composer, mappers/classifier/validators; controlled public facts/clock doubles допустимы для orchestration. Integration SUT: real PostgreSQL/Testcontainers, mappings/root child persistence/UoW/concurrency/selected public guard adapters; in-memory provider не замена. Functional SUT: один CM host+real PostgreSQL/JWT/MVP permissions, HTTP/OpenAPI/headers/routes. Real Catalog/ContentModeling writers нужны для lifecycle/move guards: mocks лишь local error handling. Не несколько BC одного host как E2E; source broker/setup контур проверяется отдельно после выбора trigger. Candidate transport tests добавляются после Q15, не тест несуществующего POST.

Tests создают собственные Variant/Product/content IDs, saved target/schema evidence, base/patch data; fixture isolation/cleanup без shared mutable seed. Races отдельные contexts/connections/scopes и barriers, не sleeps. Expected composition literal stable-ID results/issues, не тот же production helper как oracle. Проверять все schema selections и четыре legacy-группы/root token и unchanged historical artifacts после failure.

| ID / DF | Уровень / SUT | Сценарий / исходные данные | Ожидаемый результат |
| --- | --- | --- | --- |
| T01 / DF01–02 | Unit + Integration Domain | Empty patch; addition A/v2; replacement A/v5; subsequent Publish A/v6 | Whole-set persisted, exact v5 remains, no Latest; own token only |
| T02 / DF01,06 | Unit composer | Base A, same-schema A→B и A→A/v5 | A suppressed only inherited; B actual identity/provenance либо exact A/v5 |
| T03 / DF02 | Unit + Functional | Cross-schema result; different SchemaVersions same logical Schema | Cross-schema rejected; same logical Schema allowed subject availability |
| T04 / DF02 | Unit + Integration | Duplicate target, duplicate result, addition B+override A→B, A→B/C→B | No LWW/partial patch; stable-ID intrinsic conflict |
| T05 / DF01,06 | Unit composer | Independent surviving B+override A→B; two input orders | Collision diagnostics, no valid dedup result; order-independent |
| T06 / DF01–02,06 | Unit + Integration | Schema/instance exclusion + same-schema/local B/override; redundant rules | Only inherited excluded, local survives; rules co-exist persisted |
| T07 / DF01–02,06 | Integration + Unit | Target A base disappears/returns, meanwhile newer result version exists | Intent/target SchemaId/exact B preserved; return suppresses A automatically |
| T08 / DF01,06 | Integration query | F07 base update, own patch unchanged; candidate/context response arrives late | New preview with unchanged patch; no false own stale/no patch rewrite, freshness profile Q20 |
| T09 / DF02 | PostgreSQL + Functional | Two Saves same token, add/remove all groups to empty, constraint failure | One winner/other412; atomic all-schema selections и legacy patchs/root token, no partial set |
| T10 / DF02 | Integration guards | Content lifecycle/media availability, real parent move/F15 Open/Resolve during validation | MVP gate before reads; refreshed basis отказ отделён от own412; no cross-BC SQL write |
| T11 / DF01–02 | Functional HTTP/OpenAPI | Missing/stale/valid own tag, parent mismatch/inactive/applied-vs-pending |428/412/200+own header; parent code Q15; no technicalVersion/hidden resultGET |
| T12 / DF02 | Functional result carrier | Commit then concurrent Save/base update; lost response | Saved patch/token принадлежит first commit; preview captured context honest; unknown outcome no retry |
| T13 / DF03 | Integration setup | Duplicate/concurrent setup; unresolved parent; source move after filled patch | One root, no reset/fake Product; readiness/recovery Q20 |
| T14 / DF02,F09–10 | Integration Publishing | Existing release/revision/Delivery; patch Save/base update | History unchanged; current preview not historical source; F09 snapshot own protected patch |
| T15 / DF04–05 | Functional F04 + UI interaction | Exact Add/Override result, fixed-owner replacement, archived/retired candidates | Server pages/same-schema filter/context; no automatic version/default metadata |
| T16 / DF01–02,06 | Unit/Functional | Retained unavailable exact results; NEW absent/local target; persisted absent target; live-only collision | Unchanged retention и live-only collision Save allowed with ReleaseBlockers; invalid new use/untrusted target → SaveBlockers; persisted A→B survives target absence |
| T18 / DF01–02,06 | Unit composer + PostgreSQL | A→B/B→C, A→B/B→A; reordered override arrays; exclusions applied first | Targets against original inherited layer, each result once, no cascade/order dependence; cycles allowed when all guards/uniqueness hold |
| T19 / DF02 | Functional + guards | Inactive/archived owner Remove/eligible Replace; exclusion проблемного inherited A; override проблемного current inherited A→eligible B; unrelated new rule/Add; OPEN F15 | Semantic removal/replacement allowed despite new rule row; expansion/unrelated rules refused; OPEN F15 refuses ordinary Save |
| T17 / all | Architecture + source/API boundary | DI/handler/spec/route/project references | Commands Application, queries Persistence, public ports; no sourceCRUD/per-rule/publish-on-save/imaginary preview API |

| ID / DF | Уровень / SUT | Исходные данные / сценарий | Ожидаемый результат |
| --- | --- | --- | --- |
| G01 / guarded write flows | PostgreSQL Integration, separate owner connections/CM replicas | Pause writer after gate; concurrent relevant writer with same stable key | Second writer waits; first commits own BC only; second loads fresh committed facts, no forbidden stale commit |
| G02 / guarded write flows | PostgreSQL Integration + architecture/DI | Independent public primary reads while owner holds gate; foreign UoW enlistment/shared multi-DbContext participant | Reads see committed facts after acquisition; no foreign EF/transaction participant/cache/replica; own writes+outbox/audit intent/inbox atomic |
| G03 / guarded write flows | Fault integration | Bounded acquisition cancellation/timeout, failed read, owner connection loss, lost commit ACK | Technical abort/unknown outcome; no fake compatible empty facts or reconnect continuation; original own412 and independent basis409 preserved |

## 12. Порядок реализации

1. Применить agreed retention, live-collision Save, limited maintenance, original-layer target semantics; уточнить candidate transport/representation/parent mismatch Q15 и MVP gate §7.2; уточнить initialization trigger/read consistency и carrier Q20/Q15.
2. В Domain оформить schema selections и legacy patch, captured target SchemaId, stable-ID cross-group constraints/whole ReplacePatch и pure composer; unit verify target absent/return/exclusion coexistence/order independence.
3. В Application.Abstractions описать request/query/result/public ports; Application реализует batch resolve/classify/guards/update/setup и candidate evaluation после выбора scope.
4. EntityFramework/Persistence: own root Version, schema selections и legacy patch mapping/unique/atomic replacement/query snapshots, scoped migrations и guard/real PostgreSQL checks. Не угадывать missing override intent из legacy Keys.
5. Подключить public Catalog/ContentModeling/reference-use boundaries, internal setup и compatibility read для F15; проверить real concurrent writers. F07 base и historical release/revision не изменяются F08.
6. Реализовать accepted exact GET/PUT/OpenAPI/headers и selected candidate preview transport только после Q15; затем NSwag/frontend scenes/context/dirty recovery.
7. Выполнить матрицу реальных boundary checks. Code existence, Markdown links и legacy overlay tests не являются proof target implementation.

## 13. Открытые технические вопросы

[Технический Q39](../questions/Q39-schema-content-selection.md) закрывает own-selector/inherited-rule carriers, conversion и migration. Runtime/API/storage должны быть реализованы по этим контрактам; существующий flat patch их не подтверждает.

- [Q07 — сохранение patch и проверка live-базы](../../../business/questions/Q07-exclusions-and-local-content.md#f08-сохранение-patch-и-проверка-live-базы): semantics Save/live collision, retained results, original inherited targets и no cascade согласованы; остаются carriers Q15 и read/setup profiles Q20; MVP gate выбран (§7.2), no dedup сохраняется.
- [Q15 — patch, preview и parent route](../questions/Q15-api-and-source-code.md#f08-patch-preview-и-parent-route): exact DTO/diagnostics/candidate evaluation transport, limits/order, own ETag representation scope, mismatch/status/header/NSwag/access/result/freshness profile.
- [Q20 — concurrency и preview consistency](../questions/Q20-persistence-and-migration.md#f08-patch-concurrency-и-preview-consistency): own root mapping/atomic four-group writes, consistency current parent/F07base/patch, public fact carriers, F15/F09 binding profiles; MVP gate уже выбран (§7.2), recoverable internal setup и shared mapping migration.

F08 остаётся рабочим patch, а historical result хранится F09/F10. Неизвестные механизмы не добавляют full Publishing или broker в Demo.
