# F09. Релизы назначенного контента: бэкенд

[Бизнес](../../../business/features/F09-content-releases.md) · [M04](../modules/M04-publishing.md) · [Фронтенд](../../frontend/features/F09-content-releases.md) · [Q05](../../../business/questions/Q05-initial-publication.md#f09-пустой-snapshot-и-повторное-создание) · [Q07](../../../business/questions/Q07-exclusions-and-local-content.md#f09-live-collision-и-release-validation) · [Q15](../questions/Q15-api-and-source-code.md#f09-release-contracts-и-semantic-review) · [Q20](../questions/Q20-persistence-and-migration.md#f09-snapshot-creation-и-lifecycle-races).

## 1. Область реализации

F09 фиксирует сохранённый ProductContentAssignment F07 либо ProductVariantContentAssignment F08 в immutable content release. Пользователь просматривает semantic summary относительно предыдущего release, вводит ChangeComment, явно создаёт snapshot, читает list/exact release и меняет только его lifecycle Available ↔ Retired. Это MVP; Demo не зависит от full Publishing, авторизации, broker или VR School.

ProductContentRelease хранит exact выбранные Published references. ProductVariantContentRelease — четыре группы local patch, включая explicit target A → exact result B и captured target schema; он не копирует inherited Product items и не хранит final effective composition. Create Variant требует явно выбранный ReleaseValidationContext: exact immutable ProductContentRelease либо explicit EmptyBase. Semantic review и Create используют один и тот же saved patch и этот же контекст; live Product assignment/current ActiveReleaseId не являются fallback. F10 заново валидирует выбранные immutable releases и материализует revision. Historical release/revision не пересчитываются из изменившихся assignments или текущей базы.

F09 не сохраняет пользовательскую unsaved форму, не создаёт release Draft workflow, не активирует Product base/Variant revision, не создаёт promotion, moderation или Delivery. Mode выбирается операцией F11, не хранится в Product/release Create request. Retire/Restore не Unpublish/Withdrawal; immutable artifacts физически не удаляются.

Физические слои Publishing.Domain, Application.Abstractions, Application, Api, EntityFramework и целевой EntityFramework.Persistence. Сейчас [PublishingService.Releases](../../../../contentmanager.api/src/App/ContentManager.Application/Publishing/PublishingService.Releases.cs) создаёт Draft → Published, использует общую publication Revision/ReleaseNumber, активирует Product release и может запускать promotion. Source/target разрыв зафиксирован Q15/Q20. Новые типы и contracts ниже — целевая спецификация, а не утверждение о существующих endpoint/handler.

Own/parent catalog Archive запрещает новый release; ограниченное рабочее обслуживание не разрешает snapshot creation. Open conflict блокирует Create только authoritative affected closure [F15](F15-catalog-conflicts.md); immutable historical release сам по себе не move blocker и не переписывается repair/Resolve. Source Restore не выбирает release/activation автоматически; обычные independent lifecycle guards сохраняются.

## 2. Domain

### 2.1. Aggregates, entities и value objects

| Тип | Роль | Владелец / граница |
| --- | --- | --- |
| ProductContentReleaseId / ProductVariantContentReleaseId | Strong stable IDs | Publishing identity; не ReleaseNumber |
| PublishedInstanceVersionReference | Immutable exact reference | [F07](F07-product-content-assignment.md#21-aggregates-entities-и-value-objects); no latest, Keys не identity |
| VariantContentAddition / VariantContentOverride | Immutable snapshot values | [F08](F08-variant-content-assignment.md#21-aggregates-entities-и-value-objects); independent deep snapshot |
| SchemaExclusion / InstanceExclusion | Immutable snapshot rules | F08 inherited-only intent, не effective removed item copy |
| ReleaseLifecycle | Lifecycle value: Available, Retired | New-use permission; не контентный Draft/Published editor |
| ReleaseSelectionFormat | Enum: LegacyExact=0, SchemaRulesV1=1 | Storage marker; JSON names as strings; new sources только SchemaRulesV1 |
| ProductContentRelease | Aggregate root | Content/owner/comment/created metadata immutable; lifecycle metadata mutable |
| ProductVariantContentRelease | Aggregate root | Immutable patch-only; lifecycle mutable; provenance не dependency |
| ReleaseSnapshotParameters | Domain create input | Не HTTP body, не tracked assignment navigation |
| AssignmentReleaseReview | Transient application read result | Нет DbSet, не snapshot/history root |
| ReleaseValidationContext | Logical discriminated union | Явный immutable ProductRelease или EmptyBase; CLR/machine DTO Q15 |
| CompositeReleaseReviewBasis | Logical derived evidence | Saved source + exact context + current guard/review basis; own ETag отдельно |
| SchemaSelectionSnapshot | Frozen rule intent + resolved membership identity | F07 rule, exact F09 Items authoritative; no live query после Create |

**SchemaSelectionSnapshot (целевая модель):**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Selection | `SchemaContentSelection` из F07 | Да | Deep snapshot exact target/mode/includes/excludes | Immutable; не navigation к assignment; Variant означает replacement logical SchemaId |
| ResolvedInstanceIds | `IReadOnlyCollection<EntityInstanceId>` | Да | Membership результата review | Каждый ID однозначно соответствует сохранённой exact reference; empty допустим |

Maximum Published выбирается для AllExcept до snapshot, не при чтении этого типа. Source input и результат сохраняются для объяснения «почему включён», но authoritative runtime refs находятся в Items/SchemaSelectionItems, а не вычисляются из rule повторно.

**ProductContentReleaseId**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Value | `Guid` | Да | Server-generated immutable release identity | Non-empty; не ReleaseNumber |

**ProductVariantContentReleaseId**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Value | `Guid` | Да | Server-generated immutable patch release identity | Non-empty; не sequence/display key |

**ProductContentRelease**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | `ProductContentReleaseId` | Да | Independent immutable release identity | Generated Create; физически не удаляется |
| ProductId | `ProductId` | Да | Fixed source owner | Stable ID; history не rewrite после catalog changes |
| SchemaSelections | `IReadOnlyCollection<SchemaSelectionSnapshot>` | Да | Frozen intent и grouping evidence | Unique SchemaId; resolved membership полностью покрывает Items |
| SelectionFormat | `ReleaseSelectionFormat` | Да | Legacy либо schema-rules snapshot | Новые factories — SchemaRulesV1, legacy restore history-only |
| Items | Уточняется (readonly exact references) | Да | Immutable copied PublishedInstanceVersionReference set F07 | Unique EntityInstanceId; independent snapshot; empty explicitly-created set allowed |
| ChangeComment | `string?` | Нет | Immutable plain text snapshot comment | No Title/edit after Create; limits/normalization Q15; no default invented |
| CreatedAt | `DateTimeOffset` | Да | Trusted server creation time | Immutable |
| CreatedBy | `Guid?` | Уточняется | Trusted MVP actor identity | Not client field; requiredness/identity Q15/Q10; immutable |
| Lifecycle | `ReleaseLifecycle` | Да | Mutable lifecycle | Available при Create; Available ↔ Retired; не Draft/Published |
| RetiredAt | `DateTimeOffset?` | Условно | Lifecycle retirement audit | Restore/no-op/history profile Q15/Q20 |
| RetiredBy | `Guid?` | Уточняется | Trusted retirement actor | Not client; requiredness/audit/Restore profile Q15/Q10 |
| Version | `int` | Да | Own internal lifecycle concurrency | No DTO; own ETag; source assignment Version не меняется от Create |
| ReleaseNumber | Уточняется [Q15](../questions/Q15-api-and-source-code.md) | Да | Created ordering внутри same owner/type | Server-generated monotonic sequence; includes Retired for baseline/latest-created selection |

**ProductVariantContentRelease**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | `ProductVariantContentReleaseId` | Да | Independent immutable patch release identity | Server generated |
| ProductVariantId | `ProductVariantId` | Да | Fixed Variant owner | Нет mutable ProductId; move не rewrite history |
| SchemaSelections | `IReadOnlyCollection<SchemaSelectionSnapshot>` | Да | Own schema replacements | Frozen logical SchemaId masks; не inherited exclusions |
| SelectionFormat | `ReleaseSelectionFormat` | Да | Stored patch format | LegacyExact не новый F10 source |
| SchemaSelectionItems | `IReadOnlyCollection<PublishedInstanceVersionReference>` | Да | Exact materialized local results | Не повторяются в Additions/Overrides; same target; full coverage snapshot IDs |
| Additions | Уточняется (readonly VariantContentAddition collection) | Да | Immutable local addition snapshot F08 | Independent deep copy; unique result across additions/overrides |
| Overrides | Уточняется (readonly VariantContentOverride collection) | Да | Stored explicit target/schema/exact result intents F08 | Unique targets/results; same logical Schema; target absence не remove |
| SchemaExclusions | Уточняется (readonly SchemaExclusion collection) | Да | Stored inherited-only schema rules | Unique stable IDs; coexists local rules |
| InstanceExclusions | Уточняется (readonly InstanceExclusion collection) | Да | Stored inherited-only instance rules | Unique stable IDs; не effective removed item copy |
| PreparedAgainstProductContentReleaseId | ProductContentReleaseId? | Условно | Historical chosen validation-base provenance | Exact выбранный ProductRelease в этом режиме; null только EmptyBase; explicit mode evidence обязательно; не hard dependency/F10 source |
| ValidationContextMode | `ReleaseValidationContextMode` | Да | ProductRelease либо EmptyBase | Closed enum; historical immutable evidence |
| ChangeComment | `string?` | Нет | Immutable plain text snapshot comment | No Title/edit after Create; limits/normalization Q15; no default invented |
| CreatedAt | `DateTimeOffset` | Да | Trusted server creation time | Immutable |
| CreatedBy | `Guid?` | Уточняется | Trusted MVP actor identity | Not client field; requiredness/identity Q15/Q10; immutable |
| Lifecycle | `ReleaseLifecycle` | Да | Mutable lifecycle | Available при Create; Available ↔ Retired; не Draft/Published |
| RetiredAt | `DateTimeOffset?` | Условно | Lifecycle retirement audit | Restore/no-op/history profile Q15/Q20 |
| RetiredBy | `Guid?` | Уточняется | Trusted retirement actor | Not client; requiredness/audit/Restore profile Q15/Q10 |
| Version | `int` | Да | Own internal lifecycle concurrency | No DTO; own ETag; source assignment Version не меняется от Create |
| ReleaseNumber | Уточняется [Q15](../questions/Q15-api-and-source-code.md) | Да | Created ordering внутри same owner/type | Server-generated monotonic sequence; includes Retired for baseline/latest-created selection |

**ReleaseSnapshotParameters**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Owner (имя уточняется) | Уточняется | Да | Strong Product/Variant owner ID | Not HTTP body; exact model variants Q15/Q20 |
| Copied references/patch values (имена уточняются) | Уточняется | Да | Immutable independent captured source set/schema selections и четыре legacy-группы | F07/F08 owned models; empty Product set/entirely empty Variant patch allowed |
| Comment (имя уточняется) | `string?` | Нет | Validated plain text comment | ChangeComment constraints Q15 |
| Trusted time (имя уточняется) | Уточняется | Да | Server creation context | Не client field; physical carrier Q15 |
| Trusted actor (имя уточняется) | Уточняется | Уточняется | MVP actor context | Q15/Q10 requiredness/identity |
| Variant validation provenance (имя уточняется) | Уточняется [Q15](../questions/Q15-api-and-source-code.md) | Условно | Chosen context mode и chosen release ID либо explicit EmptyBase | Только Variant Create; immutable audit evidence; physical storage Q20, не hard FK/dependency |

**AssignmentReleaseReview**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Source kind/owner (имена уточняются) | Уточняется | Да | Same-kind/same-owner saved source identity | Read-only, no DbSet/history root |
| Captured saved assignment (имя уточняется) | Уточняется | Да | Full source generation | Coherent set/own token; Q20 |
| Selected previous release identity/summary (имена уточняются) | Уточняется | Условно | Baseline semantic review | Highest created same-owner ReleaseNumber incl Retired; exact captured ID nullable first; wire Q15 |
| Semantic changes (имя уточняется) | Уточняется | Да | Stable-ID content/intent/rule diff | Pure comparator §4.4; не JSON field diff/final composition |
| Input/review freshness (имена уточняются) | Уточняется [Q15](../questions/Q15-api-and-source-code.md) | Да | CompositeReleaseReviewBasis | Source/selected context/baseline/current facts; own source ETag не весь basis |

Owner IDs fixed. Current CatalogAccess applied parent Variant выбирается снаружи для guard/context; он не переписывает release после move. Историческое отображение parent/labels и evidence на момент Create — Q15/Q20, без invented mutable ProductId в Variant release. PreparedAgainstProductContentReleaseId отражает exact выбранный и проверенный ProductRelease, а не текущий ActiveReleaseId. Для explicit EmptyBase ID null, но explicit mode evidence отличает этот выбор от missing/read failure. Это immutable historical provenance; точное название/physical layout Q15/Q20. Provenance не hard FK/retention dependency, не pin reusable patch к базе и не обязательный F10 source.

Read display names/Keys могут быть enrichment, но не merge authority. Требуемые historical display/hash/actor profile уточняются Q15; ручной SortOrder исключён из release, canonical ordering не задаётся пользователем; legacy ManifestHash не добавляется в минимальную модель автоматически, ReleaseNumber сохраняет собственный смысл sequence. CreatedBy/RetiredBy из trusted MVP actor, не client field; requiredness/identity integration Q15/Q10. Snapshot values не разделяют mutable collections с assignment.

### 2.2. Методы и переходы

| Тип | Метод | Параметры | Предусловия | Изменение / результат |
| --- | --- | --- | --- | --- |
| ProductContentRelease | Create | ID + ReleaseSnapshotParameters(Product) | Captured source valid; external new-use/context guards; empty source allowed | New Available release; exact Items/comment/created metadata |
| ProductVariantContentRelease | Create | ID + ReleaseSnapshotParameters(Variant) | Valid saved patch; explicit ReleaseValidationContext; current new-use всех own refs; full context composition/review guards | New Available patch-only snapshot + historical chosen-context evidence |
| Оба release roots | Retire | now: DateTimeOffset, actorId: Guid? | Available; protected Product active-base / Variant atomic-use guard | Lifecycle Retired, audit metadata; content unchanged |
| Оба release roots | Restore | Trusted lifecycle context, now/actor если audit profile | Retired; permitted mutation profile | Available; immutable content не меняется; retired metadata/history policy Q15 |
| Semantic review service | Compare | Captured saved assignment + selected previous same-owner release | Correct source kinds/identities | Pure summary; no commit/assignment edit |

Повтор Retire/Restore в уже достигнутом состоянии и изменение metadata/no-op Version — Q15/Q20. Не объявлять такую операцию автоматически idempotent. Create не вызывает assignment.Replace/Touch и не увеличивает его Version. Release lifecycle защищает собственный mutable token; статические child values не имеют отдельных tokens/edit methods. Restore возвращает permission new use, но не делает недопустимые content dependencies допустимыми и не активирует release автоматически.

### 2.3. Инварианты и constraints

1. Immutable snapshot копирует exact references/intent из одного protected saved source. Client не отправляет новый Items/patch, source owner или result metadata в Create body.
2. Product results unique по EntityInstanceId. Variant targets unique и result IDs unique across additions/overrides; same logical Schema, не same SchemaVersion; exclusions inherited-only/coexistence сохранены как F08. No dedup/LWW/Key-match reconstruction.
3. Target отсутствует только в live base → stored override/captured schema/result не уничтожаются. Missing ContentModeling target и new-use dependency errors отличаются; exact profile Q07/Q15.
4. Новый Product release и новый Variant release требуют полной текущей new-use проверки **всех собственных references**, включая unchanged unavailable, разрешённые при Save F07/F08. Retention Save не release exception. [Q09](../../../business/questions/Q09-editing-and-deletion.md#entityinstanceversion) прямо запрещает новый Release с Retired EntityInstanceVersion до replacement/removal dependency. Existing artifact остаётся читаемым. Published status alone не заменяет owner/schema/media/Withdrawal guards.
5. Available release может участвовать в F10 лишь после новой полной validation; Retired не используется для новой revision. Historical revisions остаются materialized/readable независимо от Retire. Restore не bypass dependency checks F10.
6. Current active Product base нельзя Retire до select другой базы/clear F11. VariantRelease не имеет отдельного active pointer; нельзя нарушить выполняющуюся защищённую atomic operation.
7. Optional provenance не hard-pins VariantRelease к одному ProductRelease и не подменяет явно выбранный F10 source. Reuse с другим release того же Product проверяется заново.
8. Collision выбранного ReleaseValidationContext блокирует Variant release. Explicit empty ProductContentRelease и entirely empty schema selections и legacy patch VariantContentRelease allowed; EmptyBase — явный transient context, не synthetic release/read failure. F10 всё равно требует ≥1 exact source и final nonempty. Intentional Create с новым ClientOperationId всегда создаёт новый artifact ID даже identical content/comment; hash не dedup.

[S21](../../../catalog/docs/standards/architecture/ddd.md#aggregate-owned-rule-definitions): целевые ProductContentReleaseConstraints и ProductVariantContentReleaseConstraints переиспользуют общие reference/ChangeComment definitions и F08 patch constraints, не копируя их literals; comment limits выбираются Q15. Comment plain text без отдельного Title и редактирования после Create. Не задавать guessed length/normalization/default comment или release sequence. Lifecycle/check constraints не independent копия бизнесовых правил.

### 2.4. Domain events

F09 не требует event только ради логирования и не запускает promotion/activation на snapshot commit. Current roots emit ProductContentReleasePublishedDomainEvent / ProductVariantContentReleasePublishedDomainEvent; это legacy факт, не выбранный target broker/outbox workflow. Если F11 требует отдельного committed artifact-created fact, contract/consumer/durable protocol выбирается там/Q20, не молчаливый callback после Create. Retire/Restore не отправляют effective-null Delivery transition: active pointers/Withdrawal относятся F11.

## 3. Application.Abstractions

### 3.1. Commands

| Command | Result | Concurrency |
| --- | --- | --- |
| CreateProductContentReleaseCommand | Created ProductContentReleaseDto + own release Version metadata | Source F07 assignment If-Match; release ещё нет |
| CreateProductVariantContentReleaseCommand | Created ProductVariantContentReleaseDto + own release Version metadata | Source F08 patch If-Match; applied-parent/F15 guard separately |
| RetireProductContentReleaseCommand / RestoreProductContentReleaseCommand | Result ProductContentReleaseDto + committed own token | Release If-Match; active-base guard Retire |
| RetireProductVariantContentReleaseCommand / RestoreProductVariantContentReleaseCommand | Result ProductVariantContentReleaseDto + committed own token | Release If-Match; atomic-use guard |

**CreateProductContentReleaseCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | `Guid` | Да | Saved F07 source owner из route | Source identity не body |
| ExpectedAssignmentVersion | `int` | Да | Internal source If-Match precondition | Source защищён через commit, Create не bump source; не DTO/body |
| ChangeComment | string? | Нет | Snapshot plain text | Limits/profile Q15; no Items/UpdateMode |
| ReviewToken | `string` | Да | Opaque captured review receipt token | Base64url 32 bytes; actor/owner/context/lifetime; own If-Match отдельно |
| ClientOperationId | `Guid` | Да | Original logical CreateRelease execution | Required/non-empty body GUID; scoped confirmed receipt lookup до new-execution preconditions; Q15 |

**CreateProductVariantContentReleaseCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| RouteProductId | `Guid` | Да | Applied parent route context | Guard отдельно source token; pending не current |
| VariantId | `Guid` | Да | Saved F08 patch owner | Fixed stable ID |
| ExpectedAssignmentVersion | `int` | Да | Internal source patch If-Match | Not DTO/body; protect source capture through commit Q20 |
| ChangeComment | string? | Нет | Snapshot plain text | Q15 limits; no client provenance/patch |
| ValidationContext | `ReleaseValidationContextRequest` | Да | Captured exact ProductRelease/EmptyBase | Совпадает с review; без implicit source |
| ReviewToken | `string` | Да | Opaque captured review receipt token | Base64url 32 bytes; actor/owner/context/lifetime; own If-Match отдельно |
| ClientOperationId | `Guid` | Да | Original logical CreateRelease execution | Required/non-empty body GUID; scoped confirmed receipt lookup до new-execution preconditions; Q15 |

**RetireProductContentReleaseCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ReleaseId | `Guid` | Да | Exact own release identity | Product active-base/Variant atomic-use guard отдельно |
| ExpectedVersion | `int` | Да | Internal release own If-Match | Не source assignment token/DTO/body; no-op Q15/Q20 |

**RestoreProductContentReleaseCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ReleaseId | `Guid` | Да | Exact own release identity | Product active-base/Variant atomic-use guard отдельно |
| ExpectedVersion | `int` | Да | Internal release own If-Match | Не source assignment token/DTO/body; no-op Q15/Q20 |

**RetireProductVariantContentReleaseCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ReleaseId | `Guid` | Да | Exact own release identity | Product active-base/Variant atomic-use guard отдельно |
| ExpectedVersion | `int` | Да | Internal release own If-Match | Не source assignment token/DTO/body; no-op Q15/Q20 |

**RestoreProductVariantContentReleaseCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ReleaseId | `Guid` | Да | Exact own release identity | Product active-base/Variant atomic-use guard отдельно |
| ExpectedVersion | `int` | Да | Internal release own If-Match | Не source assignment token/DTO/body; no-op Q15/Q20 |

Actor/time trusted context/ports, не caller-created request fields. ExpectedAssignmentVersion/ExpectedVersion internal, Api parses header; no Revision body. Create/Retire/Restore handlers Publishing.Application, не App bool-owner switch. Source token после успешного Create остаётся прежним; new release token возвращается отдельно как metadata его result. ClientOperationId required в create body; [confirmed execution](../questions/Q15-api-and-source-code.md#publishing-command-execution-и-clientoperationid) и единый [CommandExecution model](../questions/Q20-persistence-and-migration.md#publishing-workflow-capture-и-продолжение) обеспечивают original receipt replay. Actor только trusted context.

### 3.2. Queries

| Query | Result |
| --- | --- |
| ListProductContentReleasesQuery | Release list summaries, shape Q15; no per-row ETag |
| ListProductVariantContentReleasesQuery | Patch release list summaries; shape/history/context profile Q15 |
| GetProductContentReleaseQuery | Exact Product DTO + own Version HTTP metadata |
| GetProductVariantContentReleaseQuery | Exact Variant DTO + own Version HTTP metadata |
| GetAssignmentReleaseReview | Saved source semantic review, no writes; external method/URL not selected |

**ListProductContentReleasesQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | Уточняется | Да | Route-fixed owner | Exact carrier Q15 |
| List parameters | Уточняется | Уточняется | Release list parameters §3.3 | Q15; no per-row ETag |

**ListProductVariantContentReleasesQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| RouteProductId | Уточняется | Да | Applied route context | Context/history policy Q15 |
| VariantId | Уточняется | Да | Stable owner | History после move не rewrite |
| List parameters | Уточняется | Уточняется | Shared Release list parameters §3.3 | Q15 |

**GetProductContentReleaseQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ReleaseId | Уточняется | Да | Exact release identity | Route Guid; exact carrier Q15; own Version metadata отдельно |

**GetProductVariantContentReleaseQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ReleaseId | Уточняется | Да | Exact patch release identity | No current-base merge; exact carrier Q15 |

**GetAssignmentReleaseReview**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Source kind/owner/route context (имена уточняются) | Уточняется | Да | Saved source review context | Same-kind/same-owner baseline; exact carrier/transport Q15 |
| Selected previous-release profile (имена уточняются) | Уточняется | Условно | Baseline read selection | Latest-created ReleaseNumber incl Retired at review capture; carrier Q15 |
| ValidationContext | `ReleaseValidationContextRequest` | Да | Captured exact ProductRelease/EmptyBase | Совпадает с review; без implicit source |
| Freshness profile (имена уточняются) | Уточняется [Q15](../questions/Q15-api-and-source-code.md) | Да | CompositeReleaseReviewBasis | Подтверждение basis обязательно; token/body/header carrier не выбран |

Query handlers в Publishing.EntityFramework.Persistence. Review — required user-visible read need, не новый утверждённый preview POST. Existing F07/F08 exact reads и F09 exact/list могут дать входы, но transport/envelope/authoritative diagnostics/confirmation profile Q15. F10 composition preview другой use case.

### 3.3. DTO и parameter models

| Model | Назначение |
| --- | --- |
| CreateProductContentReleaseRequest | Product release request; exact profile Q15 |
| CreateProductVariantContentReleaseRequest | Variant release request с explicit context/review basis; exact wire profile Q15 |
| ProductContentReleaseDto | Собственный release contract; exact shape Q15 |
| ProductVariantContentReleaseDto | Собственный release contract; exact shape Q15 |
| Release list summary | Собственный release contract; exact shape Q15 |
| Release list parameters | Собственный release contract; exact shape Q15 |
| Lifecycle action request | Собственный release contract; exact shape Q15 |
| AssignmentReleaseReviewDto | Собственный release contract; exact shape Q15 |
| Create snapshot parameter objects | Переиспользует ReleaseSnapshotParameters §2.1; не новая копия Domain create input |

**CreateProductContentReleaseRequest**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ChangeComment | string? | Нет | Plain text comment | No artifact ID/Items/patch/UpdateMode/source Version/provenance; limits Q15 |
| ReviewToken | `string` | Да | Opaque captured review receipt token | Base64url 32 bytes; actor/owner/context/lifetime; own If-Match отдельно |
| ClientOperationId | `Guid` | Да | Original logical CreateRelease execution | Required/non-empty body GUID; scoped confirmed receipt lookup до new-execution preconditions; Q15 |

**ReleaseValidationContext (логическая union-модель; CLR/discriminator/machine DTO — Q15)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Mode (логическое имя) | Уточняется [Q15](../questions/Q15-api-and-source-code.md) | Да | Явный discriminator ProductRelease или EmptyBase | Ровно одна ветвь; не default/null selection; CLR enum/string carrier не выбран |
| ProductContentReleaseId (логическое имя) | Уточняется [Q15](../questions/Q15-api-and-source-code.md) | Условно | Exact immutable выбранная база | Обязателен и non-empty только ProductRelease; отсутствует при EmptyBase; correct owner/current guards |

EmptyBase задаёт пустой inherited input именно для этого candidate. Это не synthetic persisted release, не fallback при отсутствии assignment/current base, не способ признать port failure успешным read. Предложенное логическое имя не утверждает готовый CLR type/wire DTO.

**CompositeReleaseReviewBasis (логическая модель; physical/public carrier — Q15/Q20)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SourceGeneration (логическое имя) | Уточняется [Q15](../questions/Q15-api-and-source-code.md) | Да | Exact saved source generation/full set evidence | Own assignment If-Match защищает этот ресурс; Version не публикуется в DTO |
| OwnerContext (логическое имя) | Уточняется [Q15](../questions/Q15-api-and-source-code.md) | Да | Stable owner и applied parent context | Parent/guard change не скрывается unchanged own ETag |
| ValidationContext | `ReleaseValidationContextRequest` | Да | Captured exact validation context | Variant required; same branch/ID as review |
| PreviousReleaseBasis (логическое имя) | Уточняется [Q15](../questions/Q15-api-and-source-code.md) | Условно | Selected previous same-owner baseline | Exact captured latest-created same-owner ID incl Retired; null first release; later creates do not replace baseline |
| CurrentEligibilityBasis (логическое имя) | Уточняется [Q15](../questions/Q15-api-and-source-code.md) | Да | Evidence необходимых current content/catalog/context guards | Create revalidates; read outage ≠ valid empty facts; MVP gate §7.2; read evidence carrier Q15 |

**CreateProductVariantContentReleaseRequest**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ChangeComment | `string?` | Нет | Plain text snapshot comment | Общий comment constraint; no patch/items/source Version/provenance authority |
| ValidationContext | `ReleaseValidationContextRequest` | Да | Captured exact ProductRelease/EmptyBase | Совпадает с review; без implicit source |
| ReviewToken | `string` | Да | Opaque captured review receipt token | Base64url 32 bytes; actor/owner/context/lifetime; own If-Match отдельно |
| ClientOperationId | `Guid` | Да | Original logical CreateRelease execution | Required/non-empty body GUID; confirmed receipt lookup before new-execution checks |

**ProductContentReleaseDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | `Guid` | Да | Exact immutable release identity | No technical Version/Revision; own ETag header отдельно |
| ProductId | `Guid` | Да | Fixed owner | Stable ID |
| SelectionFormat | `ReleaseSelectionFormat` | Да | LegacyExact либо SchemaRulesV1 | Closed string enum; storage 0/1; new Create только SchemaRulesV1 |
| SchemaSelections | `IReadOnlyList<SchemaSelectionSnapshotDto>` | Да | Frozen schema rules и exact results | LegacyExact → empty; группировка legacy Items не fake selector |
| Items | IReadOnlyList (exact element CLR уточняется) | Да | Exact reference read F07 | Immutable content authority; history preserved; display profile Q15 |
| ChangeComment | `string?` | Нет | Immutable snapshot plain text | Limits Q15; no edit route/Title |
| CreatedAt | `DateTimeOffset` | Да | Trusted snapshot creation time | Not technical Version |
| CreatedBy | `Guid?` | Уточняется | Trusted actor audit | Requiredness/profile Q15/Q10 |
| Lifecycle | Уточняется | Да | Available/Retired | Exact wire enum Q15; не Draft/Published |
| RetiredAt | `DateTimeOffset?` | Условно | Retirement audit | Restore/no-op profile Q15/Q20 |
| RetiredBy | `Guid?` | Уточняется | Trusted retirement actor | Q15/Q10 identity/requiredness |

**ProductVariantContentReleaseDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | `Guid` | Да | Exact patch release identity | No technical Version/Revision |
| ProductVariantId | `Guid` | Да | Fixed owner | Applied context не rewrite release |
| SelectionFormat | `ReleaseSelectionFormat` | Да | LegacyExact либо SchemaRulesV1 | Old history сохраняется; new F10 source требует SchemaRulesV1 |
| SchemaSelections | `IReadOnlyList<SchemaSelectionSnapshotDto>` | Да | Frozen whole-schema replacements/results | Не inherited exclusion list; LegacyExact empty |
| Additions | Уточняется | Да | Read local additions F08 | Exact reference read; no inherited fill |
| Overrides | Уточняется | Да | Read explicit target/captured schema/result F08 | Stored intent сохраняется; no current-base merge |
| SchemaExclusions | Уточняется | Да | Immutable read schema rules F08 | Inherited-only; shape Q15 |
| InstanceExclusions | Уточняется | Да | Immutable read instance rules F08 | Stable IDs; shape Q15 |
| PreparedAgainstProductContentReleaseId | Guid? | Условно | Historical chosen validation-base ID | Exact chosen ID при ProductRelease; null при explicit EmptyBase; не F10 source/hard dependency |
| ValidationContextMode | `ReleaseValidationContextMode` | Да | ProductRelease либо EmptyBase | Closed enum; historical immutable evidence |
| ChangeComment | `string?` | Нет | Immutable snapshot plain text | Limits Q15; no edit route/Title |
| CreatedAt | `DateTimeOffset` | Да | Trusted snapshot creation time | Not technical Version |
| CreatedBy | `Guid?` | Уточняется | Trusted actor audit | Requiredness/profile Q15/Q10 |
| Lifecycle | Уточняется | Да | Available/Retired | Exact wire enum Q15; не Draft/Published |
| RetiredAt | `DateTimeOffset?` | Условно | Retirement audit | Restore/no-op profile Q15/Q20 |
| RetiredBy | `Guid?` | Уточняется | Trusted retirement actor | Q15/Q10 identity/requiredness |

**Release list summary**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Release ID (имя уточняется) | Уточняется | Да | Exact artifact identity | Field/type Q15; no usable row token |
| Owner (имя уточняется) | Уточняется | Да | Stable source owner | Exact summary wire Q15 |
| Created summary (имя уточняется) | Уточняется | Уточняется | Snapshot creation summary | Q15; не full child preload |
| Comment summary (имя уточняется) | Уточняется | Уточняется | Immutable comment summary | Q15 |
| Lifecycle summary (имя уточняется) | Уточняется | Уточняется | Available/Retired summary | Exact shape Q15 |
| Counts (имена уточняются) | Уточняется | Уточняется | Selected summary counts | Units/shape Q15 |
| Provenance summary (имя уточняется) | Уточняется | Уточняется | Optional captured-base read summary | Q15; не mandatory dependency |
| Display metadata (имена уточняются) | Уточняется | Уточняется | Selected labels | Q15; не added ReleaseNumber/hash/order автоматически |

**Release list parameters**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Owner context (имя уточняется) | Уточняется | Да | Fixed route owner | Не body override; exact contract Q15 |
| Filters (имена уточняются) | Уточняется | Уточняется | Lifecycle/history read selection | Include-Retired/defaults Q15 |
| Order (имена уточняются) | Уточняется | Уточняется | Selected ordering | Q15 stable tie-breaker; не implicit Max ReleaseNumber |
| Paging (имена уточняются) | Уточняется | Уточняется | Server list page | Defaults/limits/total consistency Q15; no full content preload |

**Lifecycle action request**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Body/audit request (имена уточняются) | Уточняется | Уточняется | Body absence/empty model либо выбранный audit profile | Q15; no mutable content/comment/Version; carrier не утверждён |

**AssignmentReleaseReviewDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Saved source summary (имя уточняется) | Уточняется | Да | Saved source semantic input | Не unsaved form/release source override |
| Previous release ID/context (имена уточняются) | Уточняется | Условно | Baseline если есть | Latest-created ReleaseNumber incl Retired; null first, exact capture; wire Q15 |
| Added items (имя уточняется) | Уточняется | Уточняется | After-only Product exact references | Stable EntityInstanceId; shape Q15 |
| Changed version items (имя уточняется) | Уточняется | Уточняется | Same logical ID, changed exact version | Не Key rename; shape Q15 |
| Removed items (имя уточняется) | Уточняется | Уточняется | Before-only references | Не content delete |
| Variant intent/rule changes (имена уточняются) | Уточняется | Уточняется | Added/replaced/removed explicit intents и independent exclusions | Не final effective field JSON diff; shape Q15 |
| Warnings/input freshness (имена уточняются) | Уточняется | Уточняется | Baseline/source/context drift | Q15/Q20 confirmation; later creates do not replace captured previous ID; source/context/dependency drift проверяется отдельно |

Exact field names/nullability/read enrichment beyond these meanings Q15. Lifecycle enum Available/Retired defined; ReleaseNumber — server ordering; no Title/user-edited ChangeComment route. Technical Version не DTO; CreatedAt/Version не one field. List не предоставляет usable row token. Comment хранится snapshot metadata, ссылка внешней задачи остаётся plain text; JSON field-level diff не обязателен.

F09 использует логические категории [AssignmentValidationDiagnostics F07](F07-product-content-assignment.md#33-dto-и-parameter-models): SaveBlockers отражают intrinsic/target-context ошибки рабочего candidate, ReleaseBlockers — current new-use/context/collision/review guard причины отказа release, Warnings — nonblocking persisted absent target/redundant rules. Каждая issue адресует item/rule/field и participating IDs; machine DTO/codes Q15. Derived basis фиксируется отдельно от own assignment ETag и обязательно включает explicit Variant validation context. Нельзя выдавать live-only collision за отказ для другой выбранной базы или скрыть collision выбранной базы behind successful Save. Read outage не создаёт empty valid context или успешный review.

### 3.4. Ports

| Port | Назначение | Направление |
| --- | --- | --- |
| Product/Variant release repositories + Publishing UoW | Insert new root/children; tracked lifecycle root with own token; no content child update API | Outbound → Publishing.Persistence |
| Protected assignment snapshot read | Capture source set/schema selections и четыре legacy-группы+Version и сохранить source precondition на commit | Within Publishing reuse F07/F08; persistence strategy Q20 |
| ICatalogPublishingQueries / move guard | Source eligibility, stable identity/applied parent/F15 affected scope | Outbound public CatalogAccess, no foreign DbContext |
| IContentModelingPublishingQueries / reference-use guard | Batch exact new-use facts/dependency retention, target/schema evidence validation | Outbound public ContentModeling F04/F05, reuse F07/F08 |
| Selected validation-context reader/guard | Exact immutable выбранный ProductRelease by ID либо explicit EmptyBase; no fallback | Within Publishing public snapshot read/use contract; query adapter Persistence, orchestration Application |
| Composite review basis guard | Revalidate exact saved patch/context/necessary current facts against semantic review | Application + MVP gate §7.2; public review evidence carrier Q15 |
| Product current-state/atomic release-use coordination | Product Retire vs F11 Select/Clear; release Retire vs F10/F09 protected use | Within Publishing roots под gate §7.2; no Variant provenance lookup |
| Clock / current actor | Trusted UTC time/MVP actor | Reuse host ports, no client actor trust |
| Semantic review comparator | Pure stable-ID diff inputs | Application/domain pure, no DbContext/HTTP |
| MVP consistency guard / owner UoW boundary | Один global transaction exclusive gate по Q20, owner-only atomic commit и independent primary public reads | Shared infrastructure adapter; owner connection, не foreign transaction |

No storage/hash/blob/broker adapter new for F09 minimal snapshot references. Exact hash/order/historical payload profile Q15/Q20 only if justified; physical dependency retention provides old snapshots' referenced data, not automatic latest dereference. Public impact/retention readers возвращают факты release references F04/F05/F15, не edits foreign aggregate tokens.

### 3.5. Specifications / read contracts

[S19](../../../catalog/docs/standards/dotnet/ardalis-specification.md): root-by-release-ID, releases-by-ProductId/VariantId и выбранные lifecycle/created ordering filters — atomic reusable conditions. Целевые ProductContentReleaseByIdSpecification / ProductVariantContentReleaseByIdSpecification и ProductContentReleasesByOwnerSpecification / ProductVariantContentReleasesByOwnerSpecification собирают эти условия; names не существующие исходники. Tracking только lifecycle command. Owner fixed, filters reuse constraints, no repeated expression copies/imaginary spec.And API. Previous baseline selects highest created same-kind/same-owner ReleaseNumber including Retired; exact selected PreviousReleaseId nullable first release is captured in review. Later creation alone не replaces/invalidate captured baseline; no active pointer/browser-page inference.

Exact read captures content+lifecycle+own Version consistently. List summary projections don't require every child payload; total/page consistency/filter include-Retired/order tie-breaker Q15. Historical exact Retired read разрешён; new-use picker filter не history filter. Review captures entire saved source и baseline identity; source own token alone не доказывает current applied parent/selected context/relevant eligibility basis; previous exact captured artifact immutable и не заменяется later-created release.

### 3.6. Контракт проверки schema selection

Модели ниже принадлежат Publishing Application.Abstractions/technical Persistence; это принятый target Q39. Product release-review не имеет body, использует route + required assignment If-Match. Variant body — VariantReleaseReviewRequest. DTO enums сериализуются camelCase properties/string enum values по F07; Domain не принимает HTTP types. Public CreateProductContentReleaseRequest фиксирует ClientOperationId: Guid, ReviewToken: string и ChangeComment: string?; Variant дополнительно передаёт ValidationContext: ReleaseValidationContextRequest, совпадающий с review. Их поля раскрыты в существующих таблицах Create ниже. Snapshot content клиент не передаёт.

**ReleaseValidationContextRequest**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Mode | ReleaseValidationContextMode | Да | ProductRelease либо EmptyBase | Closed string enum; не null default |
| ProductReleaseId | Guid? | Условно | Exact Product source | Required только ProductRelease; null для EmptyBase |

**VariantReleaseReviewRequest**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ValidationContext | ReleaseValidationContextRequest | Да | Явная база review | Applied Product match; no implicit active/latest |

**ReleaseSemanticDiffDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| AddedInstanceIds | IReadOnlyList<Guid> | Да | Новые exact membership identities | Относительно captured previous source release |
| RemovedInstanceIds | IReadOnlyList<Guid> | Да | Убранные identities | Не physical deletion |
| ChangedVersionInstanceIds | IReadOnlyList<Guid> | Да | Тот же instance с другой exact version | Не сравнивать Name |
| ChangedSchemaIds | IReadOnlyList<Guid> | Да | Изменившиеся schema policy/scope | Отдельно от runtime content diff |

**SchemaSelectionReleaseReviewDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| CanCreate | bool | Да | Пройдены актуальные guards | False не создаёт receipt |
| ReviewToken | string? | Условно | Opaque receipt token | Required только CanCreate; 32 random bytes base64url без padding, 43 chars |
| ReviewedAt | DateTimeOffset | Да | Server UTC момента review | Trusted clock; label относится к snapshot |
| ExpiresAt | DateTimeOffset? | Условно | Конец lifetime receipt | Required только при ReviewToken |
| SourceAssignmentETag | string | Да | Own source token | Canonical strong int ETag; не весь evidence |
| PreviousReleaseId | Guid? | Условно | Captured latest-created baseline | Null только первый release; later-created не подменяет |
| ValidationContext | ReleaseValidationContextRequest? | Условно | Captured exact context | Variant required; Product null |
| Preview | SchemaSelectionPreviewDto | Да | Frozen review tree/diagnostics | BasisHash = receipt EvidenceHash при CanCreate |
| Diff | ReleaseSemanticDiffDto | Да | Semantic summary | Полный captured source set, не page |

**ReleaseReviewTreeQueryRequest**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ReviewToken | string | Да | Receipt proof of captured context | Owner/actor/access/lifetime проверяются |
| Query | SchemaSelectionTreePageQuery | Да | Page/filter по captured rows | ExpectedBasisHash = receipt EvidenceHash; live resolver не вызывается |

**ReleaseReviewReceipt**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | Guid | Да | Internal technical row identity | Server-generated; не release ID |
| TokenHash | byte[] | Да | SHA-256 raw token bytes | 32 bytes; UNIQUE; raw token не сохраняется/не логируется |
| OwnerKind | PublishingContentOwnerKind | Да | Product либо Variant | Stored text CHECK |
| OwnerId | Guid | Да | Exact assignment owner | No cross-BC FK |
| ActorId | Guid | Да | Trusted staff identity | Не из request; current access заново при Create |
| AssignmentVersion | int | Да | Captured own generation | Совпадает с source If-Match |
| ValidationContextJson | string? | Условно | Captured neutral context JSON | PostgreSQL jsonb; Product null, Variant non-null |
| PreviousReleaseId | Guid? | Условно | Historical diff baseline | Нет автоматической замены после review |
| RuleSnapshotJson | string | Да | Нормализованный full candidate | jsonb; schema selections + scoped legacy groups |
| ResolutionSnapshotJson | string | Да | Frozen rows/refs/counts/diff | jsonb; no authoring value JSON или asset bytes |
| EvidenceJson | string | Да | Exact canonical evidence text | text, UTF-8 bytes определены ниже; не jsonb-normalized bytes |
| EvidenceHash | string | Да | SHA-256 EvidenceJson bytes | 64 lowercase hex; current codec profile |
| CodecVersion | int | Да | Receipt codec | 1; unsupported version rejected, не читается как current |
| CreatedAt | DateTimeOffset | Да | Trusted server UTC | Immutable; TimeProvider |
| ExpiresAt | DateTimeOffset | Да | CreatedAt + ReviewLifetime | Immutable; >CreatedAt |

**SelectionGuardFact**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Kind | SelectionGuardFactKind | Да | CatalogProduct/CatalogVariant/ProductRelease/VariantRelease/EntitySchema/EntitySchemaVersion/EntityInstance/EntityInstanceVersion/MediaAsset | Closed internal enum |
| Id | Guid | Да | Authoritative identity | Не label; same kind+ID unique |
| Version | int | Да | Current owner/version generation | Relevant protected entity; не published VersionNumber |

**SelectionWinnerEvidence**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| EntityInstanceId | Guid | Да | Scope owner | Sorted canonical ID |
| EntityInstanceVersionId | Guid | Да | Exact winner/pin | AllExcept max Published; SelectedOnly pin |
| VersionNumber | int | Да | Published business number | Positive; max внутри exact target |
| IsExcluded | bool | Да | Policy exclusion | Не new-use dependency |
| ContentProfile | string | Да | Frozen content profile | Не current configured profile |
| ContentHash | string | Да | Frozen canonical digest | Не Registry ETag |

**SelectionScopeEvidence**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SchemaId | Guid | Да | Logical scope | Sorted ID |
| SchemaVersionId | Guid | Да | Fixed structure | Exact target |
| Mode | SchemaInstanceSelectionMode | Да | AllExcept/SelectedOnly | Не Inherit sentinel |
| Winners | IReadOnlyList<SelectionWinnerEvidence> | Да | Наблюдённые Published winners либо exact pins | AllExcept включает excluded Published winners для полного basis; SelectedOnly только pins |

**CanonicalSelectionEvidence**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Profile | string | Да | Internal format ID | cm-publishing-selection-basis/1 |
| Kind | SelectionEvidenceKind | Да | Preview либо Release | Domain separator; не ContentHash profile |
| OwnerKind | PublishingContentOwnerKind | Да | Product либо Variant | Explicit |
| OwnerId | Guid | Да | Scoped owner | Route-bound |
| AssignmentVersion | int | Да | Own source generation | Preview отражает current saved generation, даже для dirty candidate |
| CandidateJson | string | Да | Нормализованные logical rules | В canonical document записывается как JSON object candidate, не escaped string |
| ValidationContext | ReleaseValidationContextRequest? | Условно | Release context | Product null; Preview Variant live context отдельно InheritedBasis |
| PreviousReleaseId | Guid? | Условно | Captured baseline | Release-only; не повторять latest query при Create |
| InheritedBasisJson | string? | Условно | Working base либо exact release evidence | JSON object inheritedBasis; null без базы |
| Scopes | IReadOnlyList<SelectionScopeEvidence> | Да | Полная resolution evidence | Не только children page |
| GuardFacts | IReadOnlyList<SelectionGuardFact> | Да | Protected revisions | Sorted Kind ordinal + Guid D; только relevant facts |

Receipt — immutable технический persistence record, не Domain aggregate/moderation item и не новая business Version. Read-only preview queries её не создают. Review commands: ReviewProductContentReleaseCommand(ProductId: Guid, ExpectedAssignmentVersion: int); ReviewVariantContentReleaseCommand(ProductId: Guid, VariantId: Guid, ExpectedAssignmentVersion: int, ValidationContext: ReleaseValidationContextRequest). Actor получает handler из trusted current-user context, не из DTO. ReleaseReviewTreeQuery получает route owner, ReviewToken и Query; его handler читает только receipt snapshot с current access/lifetime guards.

**ReviewProductContentReleaseCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | Guid | Да | Route owner | Current access |
| ExpectedAssignmentVersion | int | Да | Assignment If-Match | Не release lifecycle token |

**ReviewVariantContentReleaseCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | Guid | Да | Applied parent | Authoritative context |
| VariantId | Guid | Да | Route owner | Current access |
| ExpectedAssignmentVersion | int | Да | Patch If-Match | Whole saved rule set |
| ValidationContext | ReleaseValidationContextRequest | Да | Exact ProductRelease/EmptyBase | Не implicit latest |

**ReleaseReviewTreeQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| OwnerKind | PublishingContentOwnerKind | Да | Product/Variant | Из endpoint, не свободный body choice |
| OwnerId | Guid | Да | Exact route owner | Applied parent/access отдельно проверены |
| ReviewToken | string | Да | Opaque captured review token | F09 lifetime/binding guards |
| Query | SchemaSelectionTreePageQuery | Да | Captured tree page | Не live resolver |

**SchemaSelectionSnapshotDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Selection | SchemaContentSelectionDto | Да | Frozen exact rule | F07 fields; immutable snapshot |
| Items | IReadOnlyList<PublishedSelectionReferenceDto> | Да | Exact resolved local refs | F07 immutable ref fields; no recompute; empty allowed |

## 4. Application

### 4.1. Handlers / use cases

Mutation handlers соблюдают порядок §7.2: shape/auth preparation → own ReadCommitted transaction → отдельный global gate statement → fresh tracked own roots + independent primary public facts → own token/basis/domain checks → own state/intents commit. Предварительные reads/CPU results не являются authority после ожидания gate.

Create Product после authorization/first gate lookup confirmed receipt; same scoped ID/input →original result, changed bound input409. Только новый execution loads one protected F07 assignment. Verifies source expected Version, composite review basis, Catalog/lifecycle/F15 facts, intrinsic reference identities and batch current new-use всех own refs, включая retained unavailable; obtains trusted comment/time/actor. Copies typed exact set, creates Available release, inserts root/children/required reference retention facts + original confirmed receipt/safe audit in local Publishing transaction. Assignment/content foreign roots/active state не меняются. Maps committed release DTO + own token without post-commit GetWorkspace/exact GET.

Create Variant после auth/gate выполняет тот же confirmed receipt lookup до нового source/context checks, затем loads protected saved F08 patch/own Version/trusted target evidence, explicit ReleaseValidationContext и composite review basis. ProductRelease branch loads exact immutable selected release by ID/specification and verifies owner/context/lifecycle guards; EmptyBase branch builds a transient explicit empty inherited input. No live assignment/current active lookup/fallback. Revalidates own intrinsic patch, current eligibility всех own results, необходимые source/result facts и chosen context basis. Same pure original-layer composer F08 checks selected composition; collision blocks Create. Patch/context/basis drift after review requires review again. Creates independent patch-only release plus immutable chosen-context provenance и original confirmed receipt в том же commit; F10 later validates its actually selected releases anew.

Retire/Restore load tracked exact release, check own expected token. Product Retire coordinates current active base F11; Variant Retire coordinates active F10 operation. Applies lifecycle method and own conditional Version change, commits only permitted metadata. Result mapped from own committed state, no hidden workspace read. Restore doesn't replace dependencies or schedule Create/Select/promotion.

Queries reside in Persistence and project exact/list/read review. Read errors distinguish source missing/setup pending, Retired history and context mismatch. Scope guard affects mutation, not silent history deletion. Candidate forms belong F07/F08; release review uses saved source. Required confirmation transport/review drift/idempotency/lifecycle no-op policies remain Q15/Q20/Q05.

### 4.2. Validators

[S18](../../../catalog/docs/standards/dotnet/fluentvalidation.md): CreateProductContentReleaseRequestValidator composes ClientOperationIdValidator(non-empty Guid), ChangeCommentValidator; CreateProductVariantContentReleaseRequestValidator also composes ReleaseValidationContextValidator (required exclusive union branch/non-empty selected ID) и ReviewBasisValidator (required evidence carrier); command composes ProductId/VariantId/ReleaseId validators and internal expected Version/header profile. List parameters compose pagination/sort/selected lifecycle conditions. Constraints единый источник по S21, а не независимые per-endpoint limits.

Pure FV checks syntax/non-empty IDs/comment shape only; doesn't read databases, count latest releases or normalize source content. Application/Domain checks current references, cross-group stable-ID uniqueness, same-schema, owner/F15/active-base rules. Retire/Restore cannot validate successfully only by enum in list DTO. No client Title/UpdateMode/Version/provenance fields accepted as mutation authority.

### 4.3. Mappers

| Mapping | Owner | Rule |
| --- | --- | --- |
| Context/body/source If-Match → Create command | Api | Comment + agreed logical review basis; Variant explicit validation-context selection; own token names source assignment, wire carrier Q15 |
| Captured source → snapshot parameters | Application | Copy refs/fourgroups; target schema from saved authoritative intent; no tracked navigation sharing |
| Explicit validated ReleaseValidationContext → historical provenance | Application | Store chosen release ID or null+explicit EmptyBase mode; no ActiveReleaseId lookup/hard F10 selection |
| New release / committed lifecycle → DTO/header carrier | Application result mapper | Own release metadata; no later root reload substitutes another writer's result |
| Exact/list projection | Persistence | Consistent own state; list token not authoritative; real source/result IDs |
| Saved source + baseline → semantic review | Pure comparator/read mapper | Stable identity, compare intent separately from effective result |

Current [ReleaseSnapshotMapper](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Application/ReleaseSnapshotMapper.cs) maps legacy Items/Exclusions/IsPublished; it requires a target four-group/lifecycle contract, not a guessed target from coinciding Keys.

### 4.4. Services / algorithms

Snapshot capture validates one saved source generation: root token and all child refs/rules must come from the same state. Check If-Match early and protect the source through release insert commit using chosen Q20 strategy; a read followed by unrelated insert isn't enough to detect intervening F07/F08 Save. Source isn't Touch-ed to emulate serialization. Child edits to empty/source no-op conventions reuse F07/F08. Two Create requests can both observe same unchanged source Version: source precondition doesn't implement request deduplication or one-Create-only semantics.

Deep snapshot copies exact reference values/target evidence/exclusions into new owner children. No full content deep merge, latest choice, normalization, inherited-items fill or release snapshot recomputation after assignment changes. ContentModeling Published versions and dependency data remain retained; lifecycle checking decides new use, not immutable bytes deletion.

Semantic review compares saved source to selected previous same-kind/same-owner release:

1. Capture source identity/Version/full set and baseline ID/content; verify context/kind. Select highest created same-owner ReleaseNumber incl Retired; nullable exact PreviousReleaseId for first, no fabricated predecessor.
2. Product: index by EntityInstanceId. Before-only → Removed, after-only → Added, same logical ID and changed exact version → Changed version. Names/Keys rename doesn't create content replacement. Duplicate IDs diagnosed, not Distinct.
3. Variant: compare additions by result ID; overrides by explicit target ID with result exact ID/version; compare schema/instance exclusions as separate stable-ID sets. Removing/replacing override/rule is intent change, even if current effective preview happens to coincide. A→B versus A→C isn't the same logical B version change; concrete labels/grouping Q15.
4. Return semantic details/counts/reasons, not field JSON diff/hash as sole explanation. No final Variant composition presented as immutable patch diff.
5. Review includes exact explicit Variant context, its composition/collisions and CompositeReleaseReviewBasis; Product includes full source/new-use checks. Create repeats validation of the same source/context and necessary current facts. Changed patch requires review again; changed chosen context/relevant basis also requires review refresh/confirmation. Own If-Match protects source only; later-created release alone does not replace captured previous ID/invalidate review. Confirmation carrier remains Q15, source/context/dependency drift still requires new review.

### Разрешение schema selection и captured release basis

Application.Abstractions расширяет release review input/result и snapshot parameters моделями schema selections F07 и resolution evidence. `SchemaSelectionResolutionBasis` — logical opaque evidence, codec §3.6: binds assignment own token, exact schema scopes, правила, весь учитываемый Published universe, winner IDs/numbers, eligibility basis и resolved exact set. Изменение source schema/instance вне saved assignment тоже инвалидирует review. Счётчик загруженной UI-страницы не evidence.

Application review handler batch-resolves AllExcept через ContentModeling public boundary, применяет stable exclusions до проверки effective closure, сохраняет exact SelectedOnly pins, выполняет F08 validation context и показывает semantic tree/diff. Для AllExcept maximum Published внутри target выбирается до eligibility: Retired/unavailable winner — blocker, не fallback на старую version. Draft-only не входит; неподтверждённая read failure не считается пустым universe. Не происходит implicit Publish схем/экземпляров.

Create command handler после начала owner transaction получает общий exclusive transaction-level MVP gate Q20 до authoritative reads и row locks. Затем выполняет fresh primary schema-scope resolution через public ContentModeling adapter, сравнивает captured rule/publication basis, повторяет guards и передаёт Domain frozen snapshots/exact refs. Все ContentModeling Publish/Retire/Withdraw/Archive writers используют тот же общий gate: новый Published owner/winner не появляется между resolution и owner commit. Foreign read adapter не захватывает второй lock и не enlist-ится в Publishing transaction. Known-ID locks не заменяют global gate. Новая lease/независимый scoped-lock protocol не вводится. Изменённый относительно подтверждения состав требует нового review, после commit новая публикация не переписывает release. Codec/storage basis определены §3.6 и §6; [технический Q39](../questions/Q39-schema-content-selection.md).

Domain factories принимают SchemaSelectionSnapshot collections и exact materialized refs, проверяют coverage, same-schema target, unique membership и immutable copies; clocks/repositories/API в Domain не вводятся. EntityFramework сохраняет frozen rules/membership и exact refs в Publishing storage без tracked relation на mutable ContentModeling graph. Persistence queries для exact release читают только snapshot. Ни read, ни F10 не делают повторный AllExcept resolution. Exact DDL и legacy migration Q39; существующие JSON/hash/history не переписываются.

Host связывает тот же F07 resolution port и owner-coordination в F09 handlers; API review/Create contracts и NSwag обновляются совместно. Diagnostics адресуют SchemaId/SchemaVersionId и конкретный instance, различают «исключён правилом», «нет Published по target», «недопустимый winner» и «basis изменился»; новые public codes/HTTP carriers не выдумываются. Unit tests проверяют immutable coverage и пустые selections; PostgreSQL concurrency tests обязательно воспроизводят Published insert нового owner между review/Create и между resolution/commit, смену winner, unavailable winner без fallback, точное подтверждение и независимость старого release. Existing replay/ClientOperationId semantics сохраняются: confirmed receipt возвращает первоначальный exact release.

### Review receipt, canonical evidence и Create

`ReviewProductContentReleaseCommandHandler` / `ReviewVariantContentReleaseCommandHandler` находятся в Application: проверяют access/own source If-Match, получают текущий saved rule set и explicit Variant context, выбирают previous latest-created baseline один раз, запрашивают protected fresh resolution через ContentModeling adapter и строят tree/diff/diagnostics. Для согласованного capture они используют короткую own transaction + общий Q20 gate; foreign reads не enlist-ятся. Если CanCreate=false, возвращается review DTO с null token/expiry, receipt не пишется. Иначе handler создаёт случайный token через RandomNumberGenerator, сохраняет immutable receipt в этой технической transaction и возвращает 200. Root Version/release numbers не меняются. Receipt repository не Domain aggregate repository.

`IReleaseReviewReceipts` определяет Insert, ReadByTokenHash и DeleteExpiredBatch; реализация в EntityFramework.Persistence. `ISchemaSelectionResolver` Application port возвращает neutral scopes/winners/guard facts и bounded resolution rows, а не foreign Domain objects. `SchemaSelectionEvidenceWriter` — deterministic serializer в Publishing Application, `SchemaSelectionEvidenceComparer` сравнивает профиль/hash и при mismatch формирует stale reasons. `GetReleaseReviewTreeQueryHandler` в Persistence читает stored ResolutionSnapshotJson по token hash с actor/owner/access/lifetime checks. Он не вызывает live AllExcept; captured review rows/counts остаются прежними до нового review, даже если Create уже потребует обновления.

Canonical evidence: UTF-8 без BOM, без insignificant whitespace/финального newline; JSON strings по Q19 canonical string rules, GUID lowercase D, integer invariant без leading zeros, enum как указанные строки, optional object/ID записываются явным null. Fixed root member order соответствует CanonicalSelectionEvidence: profile, kind, ownerKind, ownerId, assignmentVersion, candidate, validationContext, previousReleaseId, inheritedBasis, scopes, guardFacts. CandidateJson/InheritedBasisJson встраиваются как JSON objects. SHA-256 этих bytes →64 lowercase hex EvidenceHash. Profile/Kind участвуют в bytes; это не hash SDK content.

Candidate object всегда содержит schemaSelections, additionEntityInstanceVersionIds, overrides, schemaExclusionIds, instanceExclusionIds в этом порядке; у Product последние четыре arrays empty. Schema selectors сортируются по SchemaId, поля schemaId/schemaVersionId/mode/includedVersionIds/excludedInstanceIds; IDs arrays — ordinal canonical GUID; overrides — по target ID, fixed fields targetEntityInstanceId, entityInstanceVersionId существующего F08 neutral contract. Scopes сортируются SchemaId; winner rows — EntityInstanceId; guardFacts — Kind ordinal, затем Id. Duplicates — ошибка до serialization, не silently Distinct. Источник labels/messages/page/search не входит в Release evidence.

Kind Preview включает relevant logical row revisions всего tree universe и inherited live basis для согласованного paging. Kind Release включает exact scopes, AllExcept Published winners (включая явно excluded Published identities как observation, без их media/new-use closure), SelectedOnly pins и guard facts final required dependencies. Draft-only rows не становятся content dependencies. InheritedBasis: null без базы; для authoring Variant object kind=WorkingAssignment/id=ProductId/version=Product assignment Version/hash=его resolution hash; для F09 object kind=ProductRelease/id=exact ReleaseId/version=current own release Version/hash=immutable content digest либо kind=EmptyBase с null id/version/hash. Names/current UI order не влияют на runtime hash; изменение relevant entity own Version может консервативно потребовать нового review.

Create sequence: current access → optional confirmed ClientOperationId fast-path только если запись уже найдена → owner transaction ReadCommitted → общий Q20 gate до authoritative reads/row locks → повторный confirmed execution receipt lookup/replay/conflict → own source If-Match → decode/read review token → owner/actor/context/lifetime → fresh source/context/required facts → resolution/evidence comparison с captured previous ReleaseId → current eligibility/format guards → expiry check непосредственно перед persistence → Domain factory + release/execution receipt одним commit. Если fast-path ничего не нашёл, нельзя вернуть expired/stale/412 до gate и повторного lookup: original request ещё мог выполнять commit. После ожидания общего gate read видит его завершённый исход. Нельзя держать pre-lock facts как authority. Gate acquired до authoritative source check внутри transaction; ранний transport check не заменяет повтор под gate. Lifetime/unsupported codec/stale evidence failure не consume ClientOperationId, не резервирует готовый release и не меняет assignment.

Replay confirmed execution выполняется раньше review expiry/deletion и current new-use guards по Q15; changed bound input конфликтует. Новый review после unknown Create outcome не даёт права отправить новый ClientOperationId вслепую: сначала выясняется исход прежней операции. Review POST можно повторить при lost response: business artifact не создаётся, лишняя техническая receipt удалится cleanup. Все tokens/receipt bodies Cache-Control:no-store, raw token и evidence payload не попадают в logs; безопасно логировать receipt Id, owner IDs, profile и digest.

Host binds PublishingSelectionOptions, ValidateOnStart и trusted actor/TimeProvider; registers resolver/evidence writer/receipt repository/cleanup в owning module. Не добавляет shared cache, HMAC keys, persisted generation job или bus event. New Domain events для preview/review не вводятся; successful release сохраняет прежний F09 event contract.

## 5. Api

### 5.1. Endpoints

Accepted route surface из прежней F09; presence current controller не утверждается. Source assignment Api adapters F07/F08 dispatch create commands; целевые ProductContentReleaseController и ProductVariantContentReleaseController владеют list/exact/lifecycle routes. Controllers bind route/body/header и вызывают ICommandBus/IQueryBus, общий ToActionResult по S16; handlers и правила не помещаются в action. Имена новых controllers — целевые. Response envelope ApiResponse; review 200, Create 201 с Location exact resource и own release ETag; errors F07 §5.3.

| Method | Route | Request | Response | Access |
| --- | --- | --- | --- | --- |
| POST | `/api/v1/products/{productId}/content-assignment/release` | CreateProductContentReleaseRequest; source assignment If-Match | Created Product release DTO + own release ETag; 201/Location exact release | MVP Specialist/context/F15/new-use profile Q10/Q15 |
| POST | `/api/v1/products/{productId}/content-assignment/release-review` | Нет body; source assignment If-Match | 200 ApiResponse<SchemaSelectionReleaseReviewDto>, no-store | Те же owner/new-use guards; receipt только CanCreate |
| POST | `/api/v1/products/{productId}/content-assignment/release-review/schema-instances/query` | ReleaseReviewTreeQueryRequest | 200 ApiResponse<SchemaSelectionTreePageDto> из receipt | Same actor/owner/current access/lifetime |
| POST | `/api/v1/products/{productId}/variants/{variantId}/content-assignment/release-review` | VariantReleaseReviewRequest; source patch If-Match | 200 ApiResponse<SchemaSelectionReleaseReviewDto>, no-store | Applied parent + explicit context |
| POST | `/api/v1/products/{productId}/variants/{variantId}/content-assignment/release-review/schema-instances/query` | ReleaseReviewTreeQueryRequest | 200 ApiResponse<SchemaSelectionTreePageDto> из receipt | Same actor/owner/context/access/lifetime |
| POST | `/api/v1/products/{productId}/variants/{variantId}/content-assignment/release` | CreateProductVariantContentReleaseRequest logical comment/context/review basis; source Variant patch If-Match | Created Variant release DTO + own release ETag; 201/Location exact release | Applied parent guard |
| GET | `/api/v1/products/{productId}/content-releases` | List profile Q15 | Product list summaries; shape Q15 | Read profile Q15 |
| GET | `/api/v1/products/{productId}/variants/{variantId}/content-releases` | List profile Q15 | Variant list summaries; shape Q15 | Applied context/history policy Q15 |
| GET | `/api/v1/product-content-releases/{releaseId}` | Guid release ID | Exact Product DTO + own release ETag, 200 | Historical/read profile |
| GET | `/api/v1/product-variant-content-releases/{releaseId}` | Guid release ID | Exact Variant DTO + own release ETag, 200 | Historical/read profile |
| POST | `/api/v1/product-content-releases/{id}/retire` / `.../{id}/restore` | Release If-Match; body profile Q15 | Saved lifecycle DTO + own release ETag; status Q15 | Release action profile; Product active-base guard |
| POST | `/api/v1/product-variant-content-releases/{id}/retire` / `.../{id}/restore` | Release If-Match; body profile Q15 | Saved lifecycle DTO + own release ETag; status Q15 | Release action/atomic-use guard |

DF09 использует POST release-review и typed contracts §3.6; это техническая receipt без создания business release. No release content/comment PUT, delete, client source override or F11 mode field. Явный ReleaseValidationContext является выбором validation input, не caller-supplied stored provenance или snapshot payload. Context ProductId on Variant Create/list must match applied CatalogAccess relation; mismatch/status/canonical guidance reuse Q15 F08. Exact release identity survives move/inactive source; historical navigation policy Q15, no rewrite into new owner to satisfy route.

### 5.2. ETag / If-Match

[S16](../standards/S16-rest.md): strong numeric tags derived from internal int Version; no technical Version/ExpectedVersion/Revision in DTO. Create requires **source assignment** If-Match, missing428/stale412; compares exact F07/F08 saved source, not list/publication/base/provenance tag. Create doesn't change source Version; success returns newly created release own ETag for subsequent lifecycle, not a new source token. F08 derived preview representation/freshness limitations remain Q15/Q20.

Exact release GET and successful Retire/Restore return own release token; lifecycle POST requires that token, missing428/stale412. Domain/source/active-base conflict409. List not row token source. Release content immutable but exact representation includes mutable lifecycle/audit, hence own token. Parent/selected-context/reference guards и composite review basis independent of own tag match; unchanged source token не доказывает review freshness.

Root race classification after rollback/disposal uses fresh correct owner read; no blanket412 for Catalog/F15/operational failure. Atomic source check+snapshot without bump Q20. Create recovery uses required body ClientOperationId: after auth/first gate confirmed receipt lookup before current source preconditions; same original input returns original artifact result even now Retired, changed input409. New ID creates new artifact; no hash dedup/Restore/re-Create on replay. Lifecycle no-op/header grammar/error precedence profile Q15/Q20. Confirmed result does not require post-mutation GET.

### 5.3. OpenAPI и ошибки

Separate Product/Variant read DTOs, create body and lifecycle action contracts. Declare ETag/If-Match and result/header carrier; exact read200, create/lifecycle success status selected Q15. 400 ValidationProblemDetails, 404 missing exact root/source/reference (readiness distinction Q20), 409 intrinsic/new-use/F15/active-base conflict, 412/428 ProblemDetails, 401/403 by real access profile. Composition/review diagnostics not ordinary GET fakeempty. Permission/machine code/item addressing/Location/CORS/NSwag header support Q15.

Current `/api/v1/publishing/products|variants/{id}/releases` + PublishReleaseRequest(ExpectedRevision, UpdateMode)/PublishReleaseResultDto is a different workflow. Target controllers/OpenAPI regeneration replace accepted operations coherently; no hand-edit generated clients or simultaneous alternative authoring route silently retained. Retire method in Domain alone doesn't prove public lifecycle operation.

## 6. EntityFramework

### Schema selection storage и переход формата

Release tables получают `selection_snapshot_format smallint NOT NULL DEFAULT 0 CHECK IN (0,1)`: 0 = LegacyExact, 1 = SchemaRulesV1. Новые Domain factories создают только 1; row existence/schema rules не используются как эвристический discriminator. Old rows/payloads/hash остаются byte-for-byte прежними. Format 0 читается history/lifecycle endpoints и существующей materialized revision; новый F10 Create его не принимает (release-review.legacy-source), UI предлагает создать новый release. Это не Retire старой revision и не запрет прежнего historical/frozen delivery/rollback.

Новые owned tables `publishing.product_content_release_schema_selections` и `product_variant_content_release_schema_selections` содержат release owner FK, schema_id uuid, schema_version_id uuid, mode text, rule_json jsonb. PK(owner_fk,schema_id), CHECK mode, non-empty IDs; rule_json содержит immutable SchemaContentSelection той же schema/target/mode, включая exact SelectedOnly includes либо AllExcept exclusions. SQL не владеет canonical runtime hash через jsonb. EF row mapping `SchemaSelectionSnapshotConfiguration` использует typed ID converters, string mode и explicit JSON converter/snapshot comparer для immutable rule; rule_json scalar header consistency проверяется Domain, parser и integrity integration tests.

Таблицы `product_content_release_schema_members` / `product_variant_content_release_schema_members` содержат owner_fk uuid, schema_id uuid, entity_instance_id uuid; PK(owner_fk,schema_id,entity_instance_id), FK(owner_fk,schema_id) к frozen selector ON DELETE CASCADE. Product members ссылаются на authoritative exact Product Items, Variant — на новую owned `product_variant_content_release_schema_items`: owner_fk, schema_id, schema_version_id, entity_instance_id, entity_instance_version_id, content_profile text, content_hash char(64). PK(owner_fk,entity_instance_id), UNIQUE(owner_fk,entity_instance_version_id); checks non-empty IDs/nonempty profile/lowercase digest. Поля schema_items — тот же exact reference/hash payload, который использует F10; не второй JSON value store. F09 factory проверяет полное mutual coverage members↔items и same target. Schema replacement-mask выводится из frozen selector SchemaId, не отдельная mutable таблица. Existing legacy Additions/Overrides/Exclusions не становятся schema_items автоматически.

Indexes: frozen selectors(schema_version_id,owner_fk), frozen items(entity_instance_version_id,owner_fk), schema_members(entity_instance_id,owner_fk). Все FK только Publishing, source instances/schemas/media подтверждаются public adapters. Canonical refs/profile/hash materialization проходит существующий immutable-source boundary; schema selection intent не добавляется в runtime manifest.

`publishing.release_review_receipts` хранит поля ReleaseReviewReceipt: id uuid PK, token_hash bytea UNIQUE CHECK octet_length=32, owner_kind text CHECK Product/Variant, owner_id uuid, actor_id uuid, assignment_version integer, validation_context_json jsonb nullable, previous_release_id uuid nullable, rule_snapshot_json jsonb, resolution_snapshot_json jsonb, evidence_json text, evidence_hash char(64) CHECK lowercase hex, codec_version integer CHECK=1, created_at/expires_at timestamptz. CHECK expires_at>created_at, required UUID non-empty; INDEX(expires_at,id), INDEX(owner_kind,owner_id,created_at). No FK к execution receipts или mutable assignment: cleanup review не удаляет business history и replay. `ReleaseReviewReceiptConfiguration`/repository находятся в Publishing EF/Persistence; raw ReviewToken хранится только в ответе клиенту.

`ReleaseReviewCleanupService` в Host-owned background execution удаляет expired rows bounded batches по options, используя TimeProvider и cancellation. TTL не общий срок хранения release/ClientOperationId. Token не consume-once: новый ClientOperationId может намеренно создать ещё один release при неизменном basis до expiry. Token никогда не авторизует чужого actor/owner; current permissions проверяются при каждом использовании.

**Migration preflight и порядок upgrade**

1. Maintenance window/backup и dry-run читают working assignment roots и необходимые authoritative identity/target mappings. Preflight формирует JSON report с owner kind/id, source version, proposed schema selections и machine reasons; не содержит instance values/секретов. Ничего не пишет.
2. Product exact refs одной SchemaId и одного target → SelectedOnly с теми же IDs. Если одна logical schema имеет mixed targets либо нарушена identity/uniqueness, upgrade останавливается до явного исправления working data. Latest/одну «похожую» версию не выбирает.
3. Variant legacy local item остаётся local addition, exclusion — inherited exclusion. Override target не выводится из Key. Ambiguous legacy overlay, чужой instance под тем же Key, incompatible captured targets или одновременно конкурирующие intents останавливают preflight. Оператор исправляет working intent до upgrade; отчёт не является командой очистить историю.
4. После успешного preflight additive migration создаёт tables/constraints/format marker, копирует только однозначное working состояние с preserved pins и own generation. New schema selections Variant по умолчанию empty; legacy addition не превращается в whole-schema replacement. Old mutable flat tables удаляются лишь после equality/row-count/constraint validation внутри той же transaction; любое несоответствие rollback.
5. Release/revision payload/hash и active/materialized historical pointers не rewrite-ятся. Legacy release marker =0; новые source pickers разрешают для нового F10 только format1. Upgrade не создаёт release/revision и не активирует контент.
6. Exact migrations реализуются последовательными named EF migrations (add schema selection storage, migrate working rules, add frozen schema snapshots/review receipts); их generated timestamp/ID задаётся фактическим scaffolding, не выдуман в аналитике. Down не делает lossy обратную конверсию, требуется backup либо отдельно reviewed forward migration.

Preflight/migration повторяют checks на актуальных данных до commit, не доверяют старому dry-run как lease. Все writes остановлены maintenance gate; обычный runtime общий Q20 guard не заменяет deployment coordination. Fixtures обязаны покрыть mixed targets/ambiguous Variant и подтвердить refusal без partial data loss, exact safe conversion, legacy JSON/hash equality и сохранение существующей revision.

### 6.1. DbContext ownership

PublishingDbContext schema publishing owns two release roots/lifecycle metadata and immutable children. Assignment source and ProductPublicationState are other Publishing roots with independent Versions. Cross-BC references stable IDs through public contracts, no FK/transaction to CatalogAccess/ContentModeling DbContexts. Review transient, no review history/event store created by this feature.

### 6.2. Configurations

| Entity | Table / logical representation | Identity / indexes | Concurrency | Mapping |
| --- | --- | --- | --- | --- |
| ProductContentRelease | publishing.product_content_releases | ReleaseId PK; owner/lifecycle/created list indexes selected Q20 | Own Version int for lifecycle | Fixed ProductId/comment/created; lifecycle/retire metadata |
| Product release items | publishing.product_content_release_items | Owner+EntityInstanceId unique | Immutable after insert | Exact reference IDs F07, no foreign Domain navigation |
| ProductVariantContentRelease | publishing.product_variant_content_releases | ReleaseId PK; VariantId/list indexes | Own Version int for lifecycle | Fixed owner/comment/created/chosen context mode + nullable chosen ID; no hard FK/parent copy |
| Variant release additions | Owned additions representation | Owner+result EntityInstanceId unique | Immutable after insert | Exact result references |
| Variant release overrides | Owned overrides representation | Owner+target ID unique; result uniqueness | Immutable after insert | Target ID/captured SchemaId + exact result IDs |
| Variant release schema/instance exclusions | Two owned rule groups | Owner+SchemaId / owner+InstanceId unique | Immutable after insert | Stable-ID inherited intent, no effective base copy |

Concrete new child table/column/layout names Q20. Current [Product mapping](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.EntityFramework/Configurations/ProductContentReleaseConfiguration.cs) / [Variant mapping](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.EntityFramework/Configurations/ProductVariantContentReleaseConfiguration.cs) have ReleaseNumber unique per owner, Status string, ManifestHash/PublishedAt and legacy Items/Exclusions; no own Version/comment/provenance/four-group target. [PublishingOwnedMappings](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.EntityFramework/PublishingOwnedMappings.cs) shared with adjacent features, requiring scoped configuration changes.

### 6.3. Constraints / indexes

Required strong IDs, stable-ID root/child uniqueness, internal owner FKs, valid lifecycle, comment selected constraint. Cross-group Variant result uniqueness and same-schema enforced from complete snapshot; physical consistency approach Q20, not two independent indexes falsely proving cross-table uniqueness. Historical chosen-base ID nullable only for explicit EmptyBase; required mode evidence immutable. Layout/check profile Q20 must enforce exclusive branches without hard FK/dependency to provenance base; selected context lifecycle is validated at Create, not retained as reusable patch dependency.

Content columns/children and created/comment/owner/provenance not changed by lifecycle handlers. Domain/API immutability isn't proof arbitrary EF tracked graph cannot mutate: selected persistence safeguard and real checks Q20. No arbitrary delete/overwrite/cascade old release on assignment move/lifecycle; historical dependencies retained. Server owner/type ReleaseNumber orders every created artifact, including Retired; physical counter/mapping Q20. Intentional new-ID duplicate creates never hash-dedup.

## 7. EntityFramework.Persistence

### 7.1. Repositories / query adapters

Host/DI задаёт owner-partitioned UoW participants: в transaction входят только DbContext/own outbox/inbox данного BC. Текущий EfCoreUnitOfWorkParticipant, способный объединять несколько DbContexts в общей transaction, не является target coordination adapter: participant selection разделяется по owner BC, foreign public read adapter создаёт независимый primary scope/connection без ambient enlistment. Guard adapter использует owner connection/transaction; mapping/repository другого BC в command handler не внедряется.

Repositories by Product/Variant release root, not each child. Create inserts new graph only; lifecycle repository loads exact root, updates permitted metadata/Version without replacing owned content. Exact/list query adapters project generated read contracts; full snapshot for F10 public release reader includes exact four-group intent/lifecycle, not current assignment enrichment as content authority.

Protected source reader/review adapters reuse F07/F08 persistence specifications and coherent whole-set capture; no default absent assignment constructed on release creation. Query handler placement Publishing.Persistence per S20, commands Application. Current [DI module](../../../../contentmanager.api/src/App/ContentManager.Application/ContentManagerApplicationModule.cs) binds IPublishingService to App PublishingService; new adapters must become one accepted path, not parallel algorithms or new bool-based legacy bridge as target architecture.

### 7.2. Transactions

По [MVP consistency guard](../questions/Q20-persistence-and-migration.md#mvp-протокол-consistency-guard) каждый invalidating writer начинает **свою** BC transaction с явно заданным ReadCommitted и отдельным первым SQL statement получает один стабильный глобальный exclusive pg_advisory_xact_lock. Lock выполняется на том же открытом owner connection, на котором будут authoritative reads, row locks, запись и commit; общий ключ одинаков во всех репликах CM. До получения gate нельзя authoritative-read/check/row-lock затронутый basis. Для replayable Create/StartPromotion/Submit первым authoritative own read после gate идёт confirmed CommandExecution lookup; только новый execution загружает tracked business roots и проверяет expected Version. Остальные writes заново загружают own roots/expected token; чужие current facts читает только через public read ports в независимом primary connection/scope, без ambient UoW enlistment, foreign EF, shared SQL transaction, cache или read replica. Публичный read carrier остаётся предметом Q15; механизм сериализации уже выбран.

Все способные инвалидировать проверяемый basis writers используют тот же gate: F07 whole Save/setup, F08 patch Save/setup, ContentModeling/Media lifecycle/reference changes, CatalogAccess applied relation/F15 Resolve, F09 CreateRelease/Retire/Restore, F10 CreateRevision/lifecycle/Withdrawal, F11 Select/Activate/Rollback/Unpublish и promotion capture/per-item/stage/late Approval, F12 policy changes, F15 scoped repair, релевантные F17 decisions и Delivery-owned invalidation commits. Каждый изменяет только own BC state. Own state/token, required own outbox, safe audit intent и inbox marker фиксируются атомарно; commit/rollback освобождает transaction lock. Это общий write gate в одной PostgreSQL database между репликами, а не общая транзакция BC.

CPU-подготовка, immutable projection/canonical hashing и предварительные проверки могут быть снаружи gate, но после acquisition весь влияющий на commit basis заново подтверждается до применения подготовленного результата. Gate нельзя удерживать во время UI/человеческого Approval, VR School HTTP, S3/bytes, broker send/ACK, ожидания Delivery projection либо HTTP stream; эти действия и downstream commits имеют отдельные границы.

Acquisition wait ограничен выбранной конфигурацией и cancellation; timeout/cancel/lock failure — технический отказ, не domain Conflict. Failed/partial public port не превращается в empty compatible facts. Потеря owner connection прерывает операцию целиком; reconnect не продолжает прежний basis. Own expected Version mismatch остаётся 412; independent relevant basis drift — 409 с повторной проверкой/review, без автоматического retry с новым ETag. Потерянный commit ACK означает unknown outcome: inquiry по durable operation identity/выбранному Q15 recovery, а не утверждение rollback или слепой повтор.

Read queries, pickers, preview/review не получают этот global gate и не выдают commit permit. Own DTO и token читаются согласованно из одного owner поколения; composite basis/freshness других roots учитывается отдельно. Последующая mutation получает новый gate и перепроверяет basis.

F09 DF01/DF02 Create и DF07/DF08 Retire/Restore получают gate до чтения source assignment, release lifecycle и F11 pointer. Gate защищает source capture без увеличения source assignment Version: повтор Create по неизменённому source допустим, не искусственный stale412. Independent CompositeReleaseReviewBasis перепроверяется отдельно; stale own source token →412, relevant review/context drift →409. Explicit immutable ProductRelease/EmptyBase context и reusable patch-only provenance сохраняются; no-op/duplicate request carriers остаются Q15.

Create atomically commits new root + all children + chosen retention/dependency facts + original confirmed receipt/safe audit. Source assignment protected against concurrent whole-set edit through commit per Q20, but no source Version increment merely for Create. Own release state/token initialized convention Q20. Failure before commit leaves no partial release or F11 state update. Cancellation/response loss after commit → unknown outcome, not rollback claim.

Local Product Retire vs F11 Select/Clear serializes active-pointer check and lifecycle change: checking pointer once then retiring isn't enough. F10 create vs Retire coordinates correct release availability and dependency acquisition; old materialized revisions don't need mutable release read for historical delivery. Catalog/move/F15/content lifecycle/media writers сериализуются MVP gate §7.2, facts — independent primary public ports; no new direct foreign DbContext transaction or token writes.

New ClientOperationId intentional repeated Create создаёт новый ID при valid current source/context/dependency basis; source Version не меняется и не даёт artificial412. Confirmed original ID/input lookup gives original receipt first. Review previous exact captured ID immutable: later release creation alone не replaces baseline/invalidate consent; owner/context/dependency drift проверяется отдельно. PUT F07/F08 result remains own commit DTO, not F09 hidden save.

### 7.3. Migrations

Analysis doesn't execute migration. Add lifecycle/audit/comment/own Version and explicit schema selections и legacy patch snapshot storage; transition legacy Draft/Published/Retired/Number/hash to chosen target profile Q20. CM has no production legacy obligations per Q20, but dev migration/recreation policy still explicit. Existing Items lack override target: never infer A→B from Keys.

Shared assignment/release mappings F07/F08/F10 are not rewritten as collateral cleanup. Migration scope/DI/repository snapshots retain immutable history and optional provenance. Round-trip and child immutability/atomic-source/lifecycle race tests real PostgreSQL; enum/index/own Version initial/overflow/no-op profile Q20.

## 8. External adapters

### 8.1. CatalogAccess public facts

Source Product/Variant stable identity, source eligibility/applied parent, scoped F15 blockers. Pending source parent doesn't become Create context; safe move keeps old artifacts, F15 resolution owns incompatible transition. History read need different from new-use guard. Parent mismatch no automatic POST redirect/resubmit. Missing assignment/base/setup readiness separate from absent catalog owner.

### 8.2. ContentModeling / MediaLibrary public facts

Batch exact references/new-use/lifecycle/schema/asset availability and retention guards reuse F07/F08 ports. Snapshot retains historical IDs; new Release from now-Retired instance version blocked. Target absent live base uses trusted captured schema; actually missing source evidence profile Q07/Q15. Adapters don't copy new latest payload or modify foreign logical root Revision. Dependency-retention protocol must account release and materialized history, not only working assignment refs.

### 8.3. Host / Publishing coordination

Trusted clock/actor and DI; exact chosen ProductRelease reader/guard и explicit EmptyBase factory; Product active-release Retire guard inside Publishing, not Variant Create context lookup. No mandatory RabbitMQ/outbox/S3/HTTP external integration added for snapshot creation. F11/F10 consume release public contracts под MVP gate §7.2. Current in-memory Published domain event isn't proof durable delivery or reason to add broker to Demo.

## 9. Data flows

### Транспортные потоки

[S17](../standards/S17-data-flows.md). Two lifecycle rows each name both accepted actions. DF09 использует выбранный POST release-review, Application command и technical receipt; Preview/Tree queries остаются read-only.

| Поток | Транспорт | Метод / канал | Endpoint / destination | Доступ | Вход | Application action | Выход | Успех | Ошибки |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| DF01. Create Product release | HTTP API | POST | `/api/v1/products/{productId}/content-assignment/release` | MVP context/new-use/F15 profile | ProductId, comment, source If-Match | CreateProductContentReleaseCommand | Created DTO + own release ETag | Status/Location Q15; source token unchanged |400,404,409,412,428,access/dependency failures |
| DF02. Create Variant release | HTTP API | POST | `/api/v1/products/{productId}/variants/{variantId}/content-assignment/release` | Applied-parent/Variant guards | Context IDs, comment, explicit ReleaseValidationContext, composite review basis, own patch If-Match | CreateProductVariantContentReleaseCommand | Patch release DTO + own release ETag | Status/Location Q15; no final composition |Same + context mismatch/selected composition collision/review drift; exact carrier Q15 |
| DF03. List Product releases | HTTP API | GET | `/api/v1/products/{productId}/content-releases` | Read/history profile | Owner, paging/filter/order Q15 | ListProductContentReleasesQuery | Release list summaries; shape Q15 |200 list, no row token |400,missing/access/dependency profile |
| DF04. List Variant releases | HTTP API | GET | `/api/v1/products/{productId}/variants/{variantId}/content-releases` | Applied context/history profile | IDs, same list parameters | ListProductVariantContentReleasesQuery | Patch list summaries; shape Q15 |200, source/patch kind visible |Same + parent mismatch profile Q15 |
| DF05. Exact Product release | HTTP API | GET | `/api/v1/product-content-releases/{releaseId}` | Historical read | ReleaseId | GetProductContentReleaseQuery | Exact DTO + release ETag |200, Retired content readable |404,access/storage failures |
| DF06. Exact Variant release | HTTP API | GET | `/api/v1/product-variant-content-releases/{releaseId}` | Historical read | ReleaseId | GetProductVariantContentReleaseQuery | Schema selections и четыре legacy-группы/provenance/lifecycle + ETag |200, no current-base merge |404,access/storage failures |
| DF07. Product Retire/Restore | HTTP API | POST | `/api/v1/product-content-releases/{id}/retire` или `/restore` | Lifecycle access; active-base guard | ReleaseId, own If-Match; body Q15 | Retire/RestoreProductContentReleaseCommand | Lifecycle result DTO + own ETag |Status Q15, metadata commit only |400,404,409 active-base/domain,412,428,guard/storage/access |
| DF08. Variant Retire/Restore | HTTP API | POST | `/api/v1/product-variant-content-releases/{id}/retire` или `/restore` | Lifecycle/atomic-use guard | ReleaseId, own If-Match; body Q15 | Retire/RestoreProductVariantContentReleaseCommand | Lifecycle result DTO + own ETag |Status Q15, old revisions unchanged |400,404,409,412,428,guard/storage/access |
| DF09. Semantic review | HTTP API | POST | Owning content-assignment/release-review §5.1 | Current access/source If-Match; Variant explicit context | Route/source/context | ReviewProductContentReleaseCommand / ReviewVariantContentReleaseCommand | SchemaSelectionReleaseReviewDto + optional technical receipt token |

### DF01. Зафиксировать Product assignment

После authorization/first gate сначала confirmed CommandExecution lookup по ClientOperationId/input/actor; original receipt replay предшествует перечисленным new-execution source checks. Только новый execution создаёт release+receipt одним commit.

Внутренний write path после Api/FV/проверки caller: открыть owner ReadCommitted transaction, отдельным первым statement получить global pg_advisory_xact_lock на owner connection; только затем выполнять указанные ниже authoritative reads/row locks. Foreign facts — independent primary public ports без enlistment. Все guard/basis checks повторяются для этого commit; §7.2 задаёт failure/recovery и запрет внешнего ожидания под gate.

| Шаг | Исполнитель | Вход / чтение | Преобразование и проверка | Запись / выход |
| --- | --- | --- | --- | --- |
| 1 | Api/FV | ProductId, ChangeComment, If-Match | Syntax/access; source int expected | Command, no items body |
| 2 | Protected Publishing reader | F07 root/Version/all exact Items | Missing vs readiness; compare/protect source token through commit Q20 | Consistent snapshot input |
| 3 | Public Catalog/Content ports | Source eligibility/F15, exact version/dependency facts | Batch new-use; retired version refusal; public race coordination | Trusted facts, no foreign writes |
| 4 | Domain/mapper | Captured refs/comment/trusted actor/time | Intrinsic unique IDs; explicit empty artifact allowed; new client ID means new artifact; unchanged source token не создаёт automatic second412 | New Available root with independent children |
| 5 | Publishing UoW | New root/children/retention facts | Atomic insert + protected source expectation | Commit; source Version/active state/history unchanged |
| 6 | Result mapper/Api | Own created root/token | Return own DTO/ETag, 201/Location exact release | No post-commit workspace GET or promotion |

### DF02. Зафиксировать Variant patch

После authorization/first gate такой же receipt lookup до текущих source/context/If-Match checks; identical original request возвращает original result без Restore/Submit/Activate. Новый ClientOperationId — отдельный intentional artifact.

Внутренний write path после Api/FV/проверки caller: открыть owner ReadCommitted transaction, отдельным первым statement получить global pg_advisory_xact_lock на owner connection; только затем выполнять указанные ниже authoritative reads/row locks. Foreign facts — independent primary public ports без enlistment. Все guard/basis checks повторяются для этого commit; §7.2 задаёт failure/recovery и запрет внешнего ожидания под gate.

1. Api parses comment, explicit ReleaseValidationContext selection, composite review basis и own F08 If-Match. Selected ProductRelease ID names validation input; it is not trusted snapshot/provenance. Never accepts effective composition/target schema/body Version.
2. Load one complete saved schema selections и legacy patch/Version; protect source condition Q20. Applied Catalog parent mustmatch route; source pending parent doesn't permit Create.
3. Resolve explicit context: exact immutable ProductRelease of correct applied Product with current required guards, or explicit EmptyBase transient input. Missing/wrong-owner/retired/failed read refuses Create; no fallback to live/current-active/empty. Revalidate every own exact result for current new use, necessary source/result facts, trusted target schema and independent exclusions; retained unavailable result blocks release.
4. Validate saved patch intrinsic same-schema/target/result uniqueness and trusted target context. Pure compose against original inherited selected input: exclusions, simultaneous target suppression, each result once, no cascade/array order. Persisted absent-target intent remains. Collision in this selected context is ReleaseBlocker and refuses whole Create; current live collision alone is not substituted for selected-context validation.
5. Check exact patch/context/composite review basis against confirmed semantic review and current protected facts through commit. Changed patch/context/relevant basis requires review again; public refusal/confirmation carrier Q15. Store chosen ProductRelease ID or null+explicit EmptyBase mode as historical provenance only; explicit empty entire patch allowed; F10 final nonempty отдельно.
6. Create immutable patch root, insert all schema selections и четыре legacy-группы + own initial token/metadata in one local commit; no base items/hash composition/history/source mutation. Return committed own release DTO/ETag. Future F10 request selects/releases/validates anew.

### DF03. Получить Product releases

1. Bind ProductId/list parameters; read identity/access/history facts. Inactive source doesn't imply historical release deletion.
2. Apply owner specification and selected lifecycle/filter/order/paging. Exact list defaults/include-Retired/search/tie-break/count profile Q15; don't unconditionally copy current Published-only workspace filtering.
3. Project summaries without loading every child's payload; return200 list. Confirmed empty result differs from read failure. No per-row ETag, write, active pointer inference or GET-create.

### DF04. Получить Variant releases

1. Resolve stable VariantId and applied parent context. Pending move retains applied context; mismatch handling Q15; no owner fallback.
2. Owner VariantId fixed; apply same selected release list profile. Historical applied-product display/provenance after move Q15/Q20, not rewrite old content/owner.
3. Return patch summaries/metadata; no live base merge or full workspace preload. Exact chosen release DF06 supplies lifecycle token.

### DF05. Прочитать Product release

1. Exact read by ReleaseId/access; no automatic latest/base pointer substitute.
2. Consistently project immutable Items/comment/creation plus mutable lifecycle/audit/own token. Retired remains readable; current content availability can be read diagnostic profile, not replacement.
3. Return200 DTO+strong own ETag. No source assignment/base refresh or release mutation; history content authority immutable stored snapshot.

### DF06. Прочитать Variant release

1. Exact ID selects artifact independent of current workspace route; owner/access/history context profile Q15.
2. Project four groups including target/captured schema/exact result, stored exclusions and optional provenance, lifecycle/own Version. Target disappeared/current parent changed doesn't alter stored intent.
3. Return200 DTO+own ETag. Current Product base/effective list belongs F08/F10 read needs, not this exact immutable patch response.

### DF07. Retire/Restore Product release

Внутренний write path после Api/FV/проверки caller: открыть owner ReadCommitted transaction, отдельным первым statement получить global pg_advisory_xact_lock на owner connection; только затем выполнять указанные ниже authoritative reads/row locks. Foreign facts — independent primary public ports без enlistment. Все guard/basis checks повторяются для этого commit; §7.2 задаёт failure/recovery и запрет внешнего ожидания под gate.

1. Bind release ID/own If-Match/action request profile; load exact tracked root. Missing428, own stale412 without mutation.
2. Retire coordinates F11 current Product state: if ActiveReleaseId equals release, reject409. Serialize guard with concurrent Select/Clear; no implicit clear/fallback. Restore applies selected eligibility/no-op/audit profile without selecting release.
3. Domain transition modifies lifecycle/retire metadata only; conditional own Version + local commit. Failure leaves root/children/active pointers unchanged.
4. Return committed DTO/token without hidden result GET. Old revisions/Delivery unchanged; Restore doesn't bypass F10 new-use checks. Response loss handled as unknown outcome.

### DF08. Retire/Restore Variant release

Внутренний write path после Api/FV/проверки caller: открыть owner ReadCommitted transaction, отдельным первым statement получить global pg_advisory_xact_lock на owner connection; только затем выполнять указанные ниже authoritative reads/row locks. Foreign facts — independent primary public ports без enlistment. Все guard/basis checks повторяются для этого commit; §7.2 задаёт failure/recovery и запрет внешнего ожидания под gate.

1. Exact tracked root/own expected token, real permissions and lifecycle profile. VariantPublicationState token not used; no separate VariantRelease active pointer invented.
2. Coordinate Retire with F10 protected snapshot/new-use operation; no breaking already-running atomic use. Old materialized revisions survive; historical reads continue.
3. Apply metadata transition/own token and commit only lifecycle; four child groups unchanged. Restore enables future new-use candidate, not create/activate revision.
4. Return own result/ETag. Repeat/no-op race semantics Q15/Q20, not implicit unconditional retry.

### DF09. Показать semantic review

1. User reviews saved source; unsaved form isn't release source. Explicit F07/F08 Save first if desired, using returned DTO/source ETag. Review doesn't silently save.
2. Capture saved source/full set/own token/applied context и выбранный previous same-owner release; Variant additionally requires explicit exact ProductRelease or EmptyBase context used by subsequent Create. Highest-created same-owner ReleaseNumber incl Retired; nullable exact captured previous ID first; wire Q15; no fake empty persisted predecessor or fallback base.
3. Pure comparator §4.4 produces Product exact-version changes / Variant intent+rule changes. Validate current eligibility всех own refs; Variant composer evaluates same selected context and necessary source/result facts. Show SaveBlockers/ReleaseBlockers/Warnings with item/rule addresses and CompositeReleaseReviewBasis; no mandatory JSON field diff.
4. Return required read summary/context via chosen Q15 transport, no new endpoint invented. Previous baseline/input stamps/candidate read details are not source Version body.
5. DF01/02 validate the same saved set/patch and exact selected context again. Changed patch must be reviewed again; context/relevant basis drift requires refreshed confirmation. Own source If-Match alone does not prove review freshness; exact public carrier Q15. No automatic Create after ambiguous result.

## 10. Ошибки и диагностика

| Отказ / состояние | Response / результат | Значение |
| --- | --- | --- |
| Invalid comment/ID/list/header syntax |400 validation/header profile Q15 | No guessed comment limit or partial snapshot |
| Missing source assignment/owner/release/reference |404 либо readiness/context profile Q15/Q20 | Не synthetic empty assignment/release |
| Source assignment stale |412 | Snapshot не создаётся; preserve comment/edit и explicit reload |
| Release own token stale |412 | Lifecycle не меняется; list token не substitute |
| Missing required source/release If-Match |428 | Distinct resource metadata |
| Forbidden new-use/retired dependency/intrinsic invalid patch/F15 |409 domain conflict | Snapshot отказ; existing history readable |
| Collision in selected ReleaseValidationContext | ReleaseBlocker, Create refused; exact error carrier Q15 | Same patch/context reviewed and revalidated; no dedup/auto F15; F10 validates its own sources |
| Active Product base Retire / protected atomic use |409 либо selected guard conflict profile | No implicit Clear/Unpublish/fallback |
| Applied parent mismatch |Explicit context error profile Q15 | No automatic old-parent POST retry |
| Retired historical release |200 exact/history | Недоступность новой revision не missing artifact |
| Empty/new-ID repeated Create | Explicit empty Product/entire Variant allowed; new IDs even same content/comment | Bound same ID/input original receipt replay; changed input409; no source bump/hash dedup |
| Storage/port/cancellation before commit |Rollback/no partial release, operational error | After commit cancellation/lost response → unknown outcome |

Log owner/release/source kind, correlation, expected/current token category, lifecycle action, guard/commit stage, relevant dependency IDs, captured review/provenance context. No content JSON/credentials/full comment payload required for diagnostics. SnapshotCreated metric не означает activated/delivered/promotion success; Retired не Withdrawn. Machine codes/severity/availability reasons/error priority Q15. Exact error text doesn't expose foreign private data.

Global gate timeout/cancellation/failed port/owner connection loss — технический abort, не domain409 или empty valid facts. Unknown commit ACK требует inquiry/operation identity по Q15; reconnect continuation/blind retry с новым ETag не допускаются.

## 11. Тестирование

Стратегия [S04](../standards/S04-testing.md)/[S14](../../../catalog/docs/standards/dotnet/testing.md#test-environment-ownership-and-bounded-parallelism). Тесты приложения этой аналитикой не запускались. Existing [ReleaseTests](../../../../contentmanager.api/tests/BoundedContexts/Publishing/ContentManager.Publishing.Tests.Unit/ReleaseTests.cs) проверяют legacy Draft/Publish/Retire и independent assignment snapshot; [ReleaseSnapshotMapperTests](../../../../contentmanager.api/tests/BoundedContexts/Publishing/ContentManager.Publishing.Tests.Unit/ReleaseSnapshotMapperTests.cs) — Items/Exclusions/IsPublished. [PersistenceTests](../../../../contentmanager.api/tests/BoundedContexts/Publishing/ContentManager.Publishing.Tests.Integration/PersistenceTests.cs) использует реальный PostgresSuite/DatabaseTest и проверяет сохранённые snapshots. Это не proof target explicit overrides/Restore/If-Match/guards/creation semantics.

Unit SUT: immutable root/transition/constraint/comparator/mappers/FV; controlled public facts/clock doubles разрешены для orchestration. Integration SUT: real PostgreSQL/Testcontainers, source protected capture, schema selections и legacy patch storage/immutability/lifecycle concurrency/guard adapters, не in-memory EF. Functional SUT: один CM host+real PostgreSQL/JWT и выбранные MVP permissions, routes/OpenAPI/headers/errors. Cross-BC внутри CM Functional/Integration, не E2E. E2E отдельно real necessary applications если проверяется Publishing→Delivery F11, не required new infrastructure F09.

Собственные owner/content/schema/release IDs на test; clean isolation/cleanup, no shared mutable seed. Конкурентные операции отдельными contexts/scopes/connections и barriers, не sleeps. Expected snapshot/diff literal independent values, не тот же production mapper как oracle. Verify source token/set и all historical state после успеха/отказа. Tests unresolved policies добавляются после выбора, не замораживают догадку.

| ID / DF | Уровень / SUT | Сценарий / исходные данные | Ожидаемый результат |
| --- | --- | --- | --- |
| W01 / DF01–02 | PostgreSQL Integration | Explicit empty Product set/all-empty Variant groups; same content/comment with different ClientOperationId | Empty release allowed; distinct artifact IDs; source Version unchanged |
| W02 / DF01–02 | PostgreSQL fault/Functional | Lost ACK then same original request/source changed/release Retired; changed comment/context/If-Match under same ID | Original receipt after access check; changed bound input409; no new artifact/Restore/action replay |
| W03 / DF09,01–02 | PostgreSQL Integration | Review prev latest-created Retired; later another Create while saved source/context/dependencies unchanged | Exact captured nullable previous ID retained; later create alone does not invalidate confirmation |
| T01 / DF01 | Unit + PostgreSQL | Source A/v2+B/v4, create, edit/clear assignment | Exact saved refs/comment remain; source Version unchanged by Create |
| T02 / DF02 | Unit + PostgreSQL | Schema selections и четыре legacy-группы, A→B captured schema, coexist exclusions, target disappears/returns | Snapshot patch-only, independent children/intent, no inherited fill/latest |
| T03 / DF01–02 | Functional + source race | Valid/missing/stale source tag; concurrent F07/F08 whole-set Save between read/insert |428/412; one coherent protected source; no snapshot mismatched generation |
| T04 / DF01–02 | Unit + real guards | Retired/Withdrawn version/owner/schema/media guard; retained ref in readable assignment | New release blocked as selected current new-use; old releases/revisions untouched |
| T05 / DF02 | Integration guards | Same-schema different SchemaVersions; cross-schema/duplicate target/results/rules | Valid exact patch copied; invalid intrinsic full refusal/no dedup |
| T06 / DF02 | Integration | Chosen exact immutable ProductRelease vs explicit EmptyBase; no active base; missing/wrong-owner/retired/context read outage | Chosen context only; mode evidence/provenance honest; missing/failed read refuses, never fallback live/current-active/empty; old artifacts preserved |
| T07 / DF03–06 | Functional/query | Mixed lifecycle releases/paging; exact Retired; source inactive/moved | Selected list profile/owner context; history exact unchanged; no list row token/fallback |
| T08 / DF07 | PostgreSQL concurrent roots | Retire active Product base; Retire vs F11 Select/Clear | Active Retire refused; first global transaction gate serializes Retire vs Select; re-read cannot select committed Retired base |
| T09 / DF08 | PostgreSQL guard | Variant Retire vs F10 Create; existing historical revision | Selected protected-use outcome; existing materialized content/Delivery intact |
| T10 / DF07–08 | Functional + PostgreSQL | Own valid/stale/missing tag; Retire→Restore; repeat/no-op profile | Required412/428, lifecycle+own token only, content/comment/provenance immutable |
| T11 / DF01–02,07–08 | Persistence/result | Child insert failure/constraint/cancellation, later unrelated writer after commit | Atomic rollback or committed own DTO/token; no hidden GetWorkspace/current-writer result |
| T12 / DF09 | Unit comparator | Added/Changed exact version/Removed; renamed Keys; A→B vs A→C; exclusion added/removed | Stable-ID semantic intent summary, meaningful rules; no field JSON diff required |
| T13 / DF09→01–02 | Functional + UI | Changed patch; changed selected context; changed relevant context/eligibility basis with unchanged source token | Review again/refresh required; exact patch/context revalidation; source If-Match alone insufficient; no trusted browser snapshot |
| T14 / DF01–02 | Functional + PostgreSQL fault | Explicit empty patch/Product; new-ID identical creates; lost successful response | Empty allowed; distinct new artifacts; same-ID original receipt replay; unchanged token not execution identity |
| T15 / DF07–08 | Persistence safeguard | Attempt tracked content child/comment/provenance update during lifecycle | Selected immutability safeguard rejects/prevents; content columns don't change |
| T16 / DF02,F10 | Integration composition | Same VariantRelease with another valid ProductRelease; retired provenance/source | No hard dependency; F10 full validation on chosen sources; release snapshot untouched |
| T18 / DF02,09 | Unit + Integration | Live base collides, selected immutable base valid; selected base collides despite live valid; explicit EmptyBase | Validation uses same selected context as review; first case allowed subject other guards, selected collision blocked; no live substitution |
| T19 / DF01–02 | Functional + PostgreSQL | Retained unavailable own refs saved successfully F07/F08; Create with same own token | Both Product/Variant release refused under current new-use; source unchanged and no partial release |
| T20 / DF02,F10 | Persistence + composition | Retire historical provenance, then reuse patch release with another valid base; cycle/permuted intents | Provenance not hard FK/dependency; F10 full selected validation; original-layer/no cascade/results once preserved |
| T17 / all | Architecture/OpenAPI | DI handler/project references, public route surface/body fields | Commands Application, queries Persistence, public ports; no contentPUT/delete/Draft/UI Version/UpdateMode/activation-on-create |

| ID / DF | Уровень / SUT | Исходные данные / сценарий | Ожидаемый результат |
| --- | --- | --- | --- |
| G01 / guarded write flows | PostgreSQL Integration, separate owner connections/CM replicas | Pause writer after gate; concurrent relevant writer with same stable key | Second writer waits; first commits own BC only; second loads fresh committed facts, no forbidden stale commit |
| G02 / guarded write flows | PostgreSQL Integration + architecture/DI | Independent public primary reads while owner holds gate; foreign UoW enlistment/shared multi-DbContext participant | Reads see committed facts after acquisition; no foreign EF/transaction participant/cache/replica; own writes+outbox/audit intent/inbox atomic |
| G03 / guarded write flows | Fault integration | Bounded acquisition cancellation/timeout, failed read, owner connection loss, lost commit ACK | Technical abort/unknown outcome; no fake compatible empty facts or reconnect continuation; original own412 and independent basis409 preserved |

## 12. Порядок реализации

1. Применить agreed explicit ReleaseValidationContext, current new-use всех own refs и composite review basis; реализовать MVP gate §7.2; реализовать accepted empty/duplicate/latest-created captured baseline; уточнить Q15 CLR/union carrier/review/status/header/diagnostics и Q20 storage/binding/recovery.
2. Domain roots/immutable schema selections и legacy patch values/Available↔Retired transitions и shared constraints; pure semantic comparator с target/result/rule identity.
3. Application.Abstractions create/list/exact/lifecycle/review models/public protected-use contracts; Application command orchestration без legacy activation/promotion.
4. Scoped EF/Persistence mapping/own lifecycle Version/source capture/query handlers/migrations и real PostgreSQL verification. Don't infer targets from old Keys.
5. Api accepted source action/lifecycle routes, precise If-Match resource/result carrier/OpenAPI. Затем NSwag/shared transport/front F09; no manual generated edits.
6. Проверить full matrix после выбора unknowns и границы F10/F11/F15. Current legacy tests/Markdown checks не являются реализацией целевой F09.

## 13. Открытые технические вопросы

[Технический Q39](../questions/Q39-schema-content-selection.md) закрывает captured review DTO/codec/receipt lifetime, frozen storage и migration. Concurrency использует общий Q20 gate; implementation/tests ещё не выполнены.

- [Q05 — пустой snapshot и повторное создание](../../../business/questions/Q05-initial-publication.md#f09-пустой-snapshot-и-повторное-создание): explicit empty artifacts, new-ID intentional duplicates и latest-created incl Retired exact nullable baseline accepted; F10 final nonempty остаётся обязательным; CLR/wire/mapping Q15/Q20.
- [Q07 — live collision и release validation](../../../business/questions/Q07-exclusions-and-local-content.md#f09-live-collision-и-release-validation): explicit immutable/empty context и reusable patch semantics согласованы; carriers Q15 и persistence profiles Q20; gate выбран (§7.2), F10 final-effective new-use/runtime closure и broader retention согласованы Q09/F10; carrier/storage остаются Q15/Q20.
- [Q15 — release contracts и semantic review](../questions/Q15-api-and-source-code.md#f09-release-contracts-и-semantic-review): exact DTO/list/filter/order/comment/actor/diagnostics/lifecycle action/status/result headers; ReleaseValidationContext CLR/discriminator/machine DTO и composite review confirmation carrier/recovery; маршруты прежней F09 сохраняются.
- [Q20 — snapshot creation и lifecycle races](../questions/Q20-persistence-and-migration.md#f09-snapshot-creation-и-lifecycle-races): gate/capture/receipt приняты; остаются storage/immutability/migration/DI/codec, см. [workflow Q20](../questions/Q20-persistence-and-migration.md#publishing-workflow-capture-и-продолжение).

Release fixes one saved assignment; F10 fixes final selected composition; F11 governs current state. Scope boundaries не превращают неизвестные профили в уже существующие contracts.
