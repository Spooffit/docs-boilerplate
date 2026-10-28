# F10. Materialized PublicationRevision: бэкенд

[Бизнес](../../../business/features/F10-publication-revisions.md) · [M04](../modules/M04-publishing.md) · [Фронтенд](../../frontend/features/F10-publication-revisions.md) · [Q07](../../../business/questions/Q07-exclusions-and-local-content.md) · [Q15](../questions/Q15-api-and-source-code.md#f10-revision-contracts-и-preview) · [Q19](../questions/Q19-content-hashes.md#f10-materialized-representation-и-hash) · [Q20](../questions/Q20-persistence-and-migration.md#f10-composition-capture-и-withdrawal) · [Q17](../questions/Q17-publication-availability.md#f10-withdrawal-и-historical-distribution).

## 1. Область реализации

F10 — MVP Publishing: специалист явно выбирает exact ProductContentRelease, ProductVariantContentRelease либо оба, проверяет composition preview, задаёт ChangeComment и создаёт immutable итоговую ProductVariantPublicationRevision. Это moderation/activation target F17/F11 и исторический source для Delivery F13. Demo не зависит от full Publishing, Identity, moderation или broker.

Revision хранит materialized exact Items, source IDs, provenance/metadata и ContentHash. Исторический read не выполняет merge заново, не использует assignments/current base/latest. Поле content immutable, lifecycle Available/Retired/Withdrawn mutable. F10 включает exact/list reads, Retire/Restore и terminal Withdrawal; active Withdrawal атомарно согласует Publishing current state/history/outbox F11.

Create не approve/activate/promotion и не выдаёт content клиентам автоматически. Releases/assignments не создаются неявно. F08 working preview и F09 release semantic review — другие use cases. Freeze/entitlement/клиентское удаление локальных данных принадлежат F12/F13/F19; server Withdrawal не означает дистанционное стирание installed data.

Физические Publishing.Domain/Application.Abstractions/Application/Api/EntityFramework и целевой EntityFramework.Persistence. Сейчас [ProductVariantPublicationRevision](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Domain/Publications/ProductVariantPublicationRevision.cs) делает Key-based dictionary overwrite, хранит ItemsJson/ManifestJson/ManifestHash и IsRevoked. [App PublishingService](../../../../contentmanager.api/src/App/ContentManager.Application/Publishing/PublishingService.Releases.cs) использует общий publication-state Revision, foreign ContentModeling/Delivery contexts. Ни это, ни legacy тесты не доказывают target stable-ID composition/lifecycle/outbox. Новые типы ниже — целевые; current/target факты Q15/Q19/Q20.

Own/parent catalog Archive запрещает new revision; Open conflict блокирует Create только authoritative affected closure [F15](F15-catalog-conflicts.md). Immutable historical revision сохраняется и сама по себе не move blocker. Уже materialized revision/confirmed entitlement остаются доступны Delivery, включая new sessions, при независимых F12/F13 guards. Explicit safety Withdrawal проходит обычные permissions/preconditions; Archive/Conflict/Restore/Resolve не выполняют Withdrawal/unwithdraw/activation автоматически.

## 2. Domain

### 2.1. Aggregates, entities и value objects

| Тип | Роль / значения enum | Владелец / граница |
| --- | --- | --- |
| ProductVariantPublicationRevisionId | Strong ID | Publishing artifact identity, не hash или sequence |
| RevisionSourceSelection | Immutable parameter | Exact F09 sources, null = отсутствует, не latest |
| PublicationRevisionItem | Immutable materialized value | Реальный result ID B; exact content/schema IDs |
| PublicationRevisionItemSourceKind | Enum: Product, VariantSchemaSelection, VariantAddition, VariantOverride | Provenance, не runtime identity; exact wire Q39 |
| CompositionMetadata | Immutable audit value | Не substitute Items; не текущий mutable patch |
| PublicationRevisionLifecycle | Enum: Available, Retired, Withdrawn | Permission/ban отдельно content identity |
| ProductVariantPublicationRevision | Aggregate root | Content/sources/comment/creation/context fixed; lifecycle/audit mutable |
| PublicationCompositionInput | Immutable values | Временно; не tracked чужие roots |
| PublicationCompositionResult | Pure result | Invalid candidates не валидная revision; no DB/ID/clock |
| RevisionMaterializationParameters | Domain create input | Не request DTO/preview DTO |

**ProductVariantPublicationRevisionId**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Value | `Guid` | Да | Artifact identity Publishing | Non-empty; server-generated при Create; не hash/sequence |

**RevisionSourceSelection**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductContentReleaseId | `ProductContentReleaseId?` | Условно | Exact Product source F09 | Хотя бы один source; null = отсутствие, не latest |
| ProductVariantContentReleaseId | `ProductVariantContentReleaseId?` | Условно | Exact Variant source F09 | Хотя бы один source; null = отсутствие, не latest |

**PublicationRevisionItem**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| EntityInstanceId | Уточняется | Да | Реальный result ID B | Stable identity, не target A |
| EntityInstanceVersionId | Уточняется | Да | Exact content version | Immutable materialized reference |
| EntitySchemaId | Уточняется | Да | Logical schema identity | Same-schema constraint |
| SchemaVersionId | `EntitySchemaVersionId` | Да | Exact schema version | Immutable materialized reference |
| SourceKind | `PublicationRevisionItemSourceKind` | Да | Provenance | Не runtime identity |
| OverrideTargetEntityInstanceId | Уточняется (nullable) | Условно | Override target provenance | Обязателен для VariantOverride; остальные kinds не используют как implicit override |

**CompositionMetadata**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Captured schema exclusion facts | Уточняется | Уточняется | Captured audit facts | Immutable; exact shape Q15; не substitute Items |
| Captured schema selection/replacement facts | Уточняется (F09 frozen schema snapshots) | Да | Источник выбора и logical SchemaId masks | Immutable; no live selection evaluation; provenance вне manifest hash |
| Captured instance exclusion facts | Уточняется | Уточняется | Captured audit facts | Immutable; exact shape Q15; не текущий mutable patch |
| Provenance | Уточняется | Уточняется | Captured override/source provenance | Immutable; exact shape Q15 |

**ProductVariantPublicationRevision**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | `ProductVariantPublicationRevisionId` | Да | Revision identity | Server-generated; content identity не hash |
| ProductVariantId | Уточняется | Да | Stable owner | Fixed |
| ProductId | Уточняется | Да | Captured context создания | Fixed; source move не меняет; representation Q15/Q20 |
| ProductContentReleaseId | `ProductContentReleaseId?` | Условно | Exact Product source | Fixed; ≥1 source |
| ProductVariantContentReleaseId | `ProductVariantContentReleaseId?` | Условно | Exact Variant source | Fixed; ≥1 source |
| ChangeComment | `string?` | Нет | Creation comment | Fixed; constraints Q15 |
| Items | Уточняется | Да | Materialized exact items | Readonly collection; Immutable; nonempty; unique result IDs |
| CompositionMetadata | `CompositionMetadata` | Да | Captured audit/provenance | Immutable; shape Q15 |
| ContentHash | `CanonicalDigest` | Да | SHA-256 exact runtime manifest bytes | 64 lowercase hex без prefix; не ETag/artifact ID |
| Manifest | `CanonicalRepresentation` из [F03](F03-publish-schema-version.md#33-dto-и-parameter-models) | Да | Immutable bytes/profile/digest triple | Profile `cm-publication-manifest/1`; ContentHash совпадает с digest; atomic owner commit |
| CreatedAt | `DateTimeOffset` | Да | Creation time | Fixed; trusted server clock |
| CreatedBy | `Guid?` | Нет | Creation actor | Fixed; trusted context; details Q15/Q20 |
| Lifecycle | `PublicationRevisionLifecycle` | Да | Availability status | Mutable; initial Available; Withdrawn terminal |
| RetiredAt | Уточняется (nullable) | Условно | Retirement time | Lifecycle audit; profile Q15/Q20 |
| RetiredBy | Уточняется (nullable) | Условно | Retirement actor | Lifecycle audit; trusted context; profile Q15/Q20 |
| WithdrawnAt | Уточняется (nullable) | Условно | Withdrawal time | Lifecycle audit; profile Q15/Q20 |
| WithdrawnBy | Уточняется (nullable) | Условно | Withdrawal actor | Lifecycle audit; trusted context; profile Q15/Q20 |
| WithdrawalReason | `string?` | Условно | Terminal withdrawal reason | Meaningful reason при Withdraw; constraints Q15 |
| Version | `int` | Да | Own optimistic concurrency | Internal mutable lifecycle token; не DTO/hash/sequence |

**PublicationCompositionInput**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Product refs | Уточняется (nullable) | Нет | Exact captured F09 Product refs; Selected Product input | Immutable temporary values; не чужие tracked roots |
| Variant patch | Уточняется (nullable) | Нет | F09 frozen schema replacements/materialization и четыре legacy groups | Immutable temporary values; ≥1 source; no live expansion |
| Resolved context | Уточняется | Уточняется | Current applied context | Authoritative profile Q15/Q20 |
| New-use evidence | Уточняется | Уточняется | Eligibility evidence | Authoritative profile Q15/Q20 |

**PublicationCompositionResult**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| IsValid | Уточняется | Да | Validity of pure composition | Invalid candidates не валидная revision |
| Результирующий состав | Уточняется | Уточняется | Composition result: effective candidates / Items | No DB/ID/clock; valid vs invalid shape Q15 |
| Excluded trace | Уточняется | Уточняется | Exclusion diagnostics | Shape Q15 |
| Provenance trace | Уточняется | Уточняется | Source/override diagnostics | Shape Q15 |
| Issues | Уточняется | Уточняется | Typed issues; Composition diagnostics | Shape Q15 |

**RevisionMaterializationParameters**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Items | Уточняется | Да | Valid copied materialized Items | Domain input; не request/preview DTO |
| Metadata | Уточняется | Уточняется | Copied composition metadata | Validated immutable input |
| Selection | `RevisionSourceSelection` | Да | Selected exact sources | ≥1 source |
| Context | Уточняется | Уточняется | Captured context | Authoritative context profile Q15/Q20 |
| Canonical result | `CanonicalRepresentation` из F03 | Да | Validated runtime manifest bytes/profile/digest | `cm-publication-manifest/1`; exact immutable Items binding |
| Comment | Уточняется | Уточняется | Creation comment | Trusted/validated input; constraints Q15 |
| Time | Уточняется | Уточняется | Creation time | Trusted clock |
| Actor | Уточняется | Уточняется | Creation actor | Trusted context |

Exact reference values/Variant target schema evidence reuse [F07](F07-product-content-assignment.md#21-aggregates-entities-и-value-objects)/[F08](F08-variant-content-assignment.md#21-aggregates-entities-и-value-objects)/[F09](F09-content-releases.md#21-aggregates-entities-и-value-objects), не копии Domain чужого BC. Captured ProductId описывает context создания, не меняется от source move; ownership/new activation compatibility остаются F06/F11/F15 guards. Public historical context/actor type/details Q15/Q20.

VariantOverride item requires target provenance; other kinds не используют nullable target как implicit override. Same-instance A/v2→A/v5 разрешён, A→B same logical Schema разрешён при разных SchemaVersions. B не маскируется под A. Runtime manifest содержит реальный B и exact materialized membership; SourceKind/target A/exclusion trace сохраняются отдельно. Пользовательский SortOrder исключён из revision/manifest input; canonical InstanceId ordering задаётся сервером. Legacy JSONB representation не принимается автоматически. Client не задаёт runtime identity/sourceKind/target schema/hash.

### 2.2. Методы и переходы

| Тип | Метод | Параметры | Предусловия | Изменение / результат |
| --- | --- | --- | --- | --- |
| PublicationCompositionService | Compose | Captured typed Product/Variant inputs + authoritative evidence | Context/source/patch facts classification | Pure valid result или diagnostics; no ID/time/storage |
| ProductVariantPublicationRevision | Create | ID + RevisionMaterializationParameters | ≥1 explicit source; valid nonempty unique Items; protected new-use facts | New Available immutable composition; own initial Version convention |
| Revision root | Retire | Trusted now/actor | Available; не current active F11; coordinated guard | Retired/audit only; Items/hash/history unchanged |
| Revision root | Restore | Trusted lifecycle context/audit profile | Retired; не Withdrawn | Available; no automatic moderation/activation |
| Revision root | Withdraw | Reason: string; trusted now/actor | Non-Withdrawn supported profile; meaningful reason | Terminal Withdrawn/metadata; application coordinates state/outbox |
| F11 Variant publication state | Transition to null for Withdrawal | Server current state/Sequence, reason Withdrawal, trusted context | ActiveRevisionId == withdrawn ID, protected multi-root capture | Pointer null + new activation record; not fallback |

Retired→Withdrawn допустимый terminal запрет существующего historical artifact. Повтор Retire/Restore/Withdraw, no-op/token/audit priority Q15/Q20; не переносить current Revoke unconditional no-op как target contract. Lifecycle не меняет Items/source/comment/hash. Restore не делает withdrawn dependencies допустимыми; F11/F17 проверки activation остаются отдельными.

### 2.3. Инварианты и constraints

1. Source selection: Product-only, Variant-only и combined допустимы; none недопустимо. Empty final result не создаёт revision ([Q05](../../../business/questions/Q05-initial-publication.md)); explicit empty Product/Variant releases allowed F09.
2. Sources доступны для нового использования и имеют правильный Product/Variant owner текущего applied context. Optional F09 PreparedAgainst provenance не hard pin и не подставляет обязательную базу.
3. Merge authority EntityInstanceId/EntitySchemaId, no Key matching/last write wins/dedup. Intrinsic F08 target/result uniqueness, same-schema и inherited-only exclusions сохраняются.
4. Missing target в selected ProductRelease либо Variant-only не удаляет intent: exact local result остаётся; captured authoritative TargetSchemaId подтверждает same-schema. Реально missing source evidence отличается от absent-in-base (Q07/Q15).
5. New-use eligibility проверяется только по final effective instance/schema/media closure. Suppressed/excluded A unavailable не блокирует effective B, если A не final/shared dependency B. Source release Available/owner и trusted captured target/schema/exclusion evidence проверяются отдельно; retained source/evidence closure шире runtime closure и не удаляется.
6. Preview/Create одна бизнес-валидация; Create заново разрешает facts и compose, не доверяет client Items/hash/IsValid или старому preview. No required release/workspace If-Match не означает отсутствие lifecycle/parent/reference race guard.
7. Historical materialized content неизменен после base/source/content algorithm/lifecycle изменений. Read не reruns new-use validation как gate к stored history; Withdrawal distribution ban отдельный F13 check.
8. Current active revision нельзя Retire до замены/Unpublish. Withdraw current → null/history/outbox атомарно в Publishing, no automatic fallback; inactive Withdrawal тоже запрещает historical/frozen выдачу этого ID. Runtime dependency restrictions follow final-effective closure, не suppressed source/evidence graph.

[S21](../standards/S21-aggregate-constraints.md): ProductVariantPublicationRevisionConstraints переиспользует общие exact-reference/patch value constraints и фиксирует source selection, Items/source-kind consistency, ChangeComment/WithdrawalReason constraints после выбора Q15. No guessed string limits/order/number. Hash constraints/profile Q19 отдельно от own concurrency Version.

### 2.4. Domain events

| Fact / contract | Когда фиксируется | Consumer / граница |
| --- | --- | --- |
| ProductVariantPublishingStateChanged F11 | Active Withdrawal реально меняет pointer | Delivery; durable Publishing outbox в том же commit |
| Revision withdrawal availability fact, exact contract TBD | Любой новый terminal Withdrawal, включая inactive/frozen ID | Delivery distribution guards; не заменить одним null-pointer event |

**ProductVariantPublishingStateChanged F11**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| VariantId | Уточняется | Да | Variant transition identity | Contract owner F11 |
| RevisionId | Уточняется (nullable) | Нет | Cleared current pointer | null при active Withdrawal |
| Sequence | Уточняется | Да | Transition order | Monotonic per Variant; owner F11 |
| ActivatedAt | Уточняется | Да | Transition time | Trusted profile F11 |
| Reason | Уточняется | Да | Transition reason | Withdrawal |
| Correlation profile | Уточняется | Уточняется | Correlation metadata | Contract F11 |

**Revision withdrawal availability fact, exact contract TBD**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Exact revision | Уточняется | Да | Forbidden artifact identity | Contract TBD Q20/Q17; inactive/frozen тоже |
| Owner | Уточняется | Уточняется | Revision owner context | Contract Q20/Q17 |
| Ban identity | Уточняется | Уточняется | Terminal restriction identity | Contract Q20/Q17 |
| Order | Уточняется | Уточняется | Restriction ordering | Contract Q20/Q17 |
| Audit profile | Уточняется | Уточняется | Trusted restriction audit | Contract Q20/Q17 |

Создание F10 само по себе не emits approved/activated/client-visible event. F17 manual submission/automatic promotion orchestration принадлежат F17/F11/Q28. Если нужны internal materialized-available/retention facts, consumers/protocol уточняются Q20; не event ради логирования. Domain event in memory не proof durable delivery. Restrictions/result propagation отдельно Publishing commit, broker/consumer commit и Delivery effective visibility.

## 3. Application.Abstractions

### 3.1. Commands

| Command | Result | Concurrency |
| --- | --- | --- |
| CreatePublicationRevisionCommand | Created revision DTO + own Version metadata | New root; no workspace/source If-Match; source/guard gate §7.2; carrier Q15 |
| RetirePublicationRevisionCommand / RestorePublicationRevisionCommand | Committed revision DTO + own token | Revision If-Match; F11 active guard separately |
| WithdrawPublicationRevisionCommand | Committed revision DTO + own token; propagation/current-state result profile Q15 | Own revision If-Match; protected current state internally Q20 |

**CreatePublicationRevisionCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| RouteProductId | `Guid` | Да | Applied route context | Проверяемый parent, не source override |
| VariantId | `Guid` | Да | Variant context | Applied ownership/F15 guards |
| ProductContentReleaseId | `Guid?` | Условно | Exact Product source | ≥1 source; no source If-Match/latest fallback |
| ProductVariantContentReleaseId | `Guid?` | Условно | Exact Variant source | ≥1 source; no source If-Match/latest fallback |
| ChangeComment | `string?` | Нет | Creation comment | Constraints Q15 |
| ClientOperationId | `Guid` | Да | Logical public CreateRevision execution | Required body/non-empty; original input/actor receipt before new-execution preconditions Q15 |

**RetirePublicationRevisionCommand / RestorePublicationRevisionCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| RevisionId | `Guid` | Да | Exact lifecycle root | Retire/Restore |
| ExpectedVersion | `int` | Да | Own revision precondition | Api parses If-Match; не body; active guard отдельно |

**WithdrawPublicationRevisionCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| RevisionId | `Guid` | Да | Exact lifecycle root | Terminal Withdrawal |
| ExpectedVersion | `int` | Да | Own revision precondition | Api parses If-Match; protected current state отдельно Q20 |
| Reason | `string` | Да | Withdrawal reason | Meaningful; constraints Q15 |

Actor/time trusted ports, not caller fields. ExpectedVersion Api parses header, not body. Product/Variant route fields проверяемый applied context, не source override. Command handlers Publishing.Application. New ClientOperationId intentional Create creates a new artifact ID even identical sources/content/comment. Same original scoped ID/input returns [confirmed receipt](../questions/Q15-api-and-source-code.md#publishing-command-execution-и-clientoperationid) from canonical [CommandExecution](../questions/Q20-persistence-and-migration.md#publishing-workflow-capture-и-продолжение), not hash dedup.

### 3.2. Queries

| Query | Result |
| --- | --- |
| PreviewPublicationRevisionQuery | Preview DTO; pure diagnostics/result, no persisted root |
| GetPublicationRevisionQuery | Stored materialized DTO + own token |
| ListPublicationRevisionsQuery | Summaries/list shape Q15, no row token |
| Public materialized revision read | Stored immutable payload/hash/provenance + lifecycle availability facts |

**PreviewPublicationRevisionQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| RouteProductId | Уточняется | Да | Applied Product context, как в Create command | Exact context/profile Q15/Q20 |
| VariantId | Уточняется | Да | Applied Variant context, как в Create command | Exact context/profile Q15/Q20 |
| ProductContentReleaseId | Уточняется (nullable) | Условно | Exact Product source, как в Preview request | ≥1 source; no permanent root/token |
| ProductVariantContentReleaseId | Уточняется (nullable) | Условно | Exact Variant source, как в Preview request | ≥1 source; no permanent root/token |

**GetPublicationRevisionQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| RevisionId | Уточняется | Да | Exact stored revision | Historical materialized read; own token |

**ListPublicationRevisionsQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| RouteProductId | Уточняется | Да | Applied Product context | List profile Q15 |
| VariantId | Уточняется | Да | Stable Variant context | List profile Q15 |
| List profile | Уточняется | Уточняется | Chosen filters/order/paging | Q15; no row token |

**Public materialized revision read**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Exact revision ID | Уточняется | Да | Stored artifact identity | Не foreign DB query |
| Selected caller context | Уточняется | Уточняется | Availability/read context | Administrative vs distribution guard Q15 |

Queries в Publishing.EntityFramework.Persistence. External preview uses accepted POST despite no mutation. F09 picker reads reused; public F17/Delivery read не означает foreign DB query. Administrative history и server distribution используют разные availability guards; permissions/read shape Q15, no fake withdrawn404 hiding evidence.

### 3.3. DTO и parameter models

| Model | Назначение / владелец |
| --- | --- |
| PreviewPublicationRevisionRequest | Поля в таблице модели ниже; точный профиль сохраняет указанные Q-вопросы |
| CreatePublicationRevisionRequest | Поля в таблице модели ниже; точный профиль сохраняет указанные Q-вопросы |
| PublicationRevisionPreviewDto | Поля в таблице модели ниже; точный профиль сохраняет указанные Q-вопросы |
| PublicationRevisionItemDto | Поля в таблице модели ниже; точный профиль сохраняет указанные Q-вопросы |
| ProductVariantPublicationRevisionDto | Поля в таблице модели ниже; точный профиль сохраняет указанные Q-вопросы |
| Revision summary/list parameters | Поля в таблице модели ниже; точный профиль сохраняет указанные Q-вопросы |
| Retire/Restore action request | Поля в таблице модели ниже; точный профиль сохраняет указанные Q-вопросы |
| WithdrawPublicationRevisionRequest | Поля в таблице модели ниже; точный профиль сохраняет указанные Q-вопросы |
| Composition inputs / materialization parameters | Переиспользуются PublicationCompositionInput и RevisionMaterializationParameters §2.1: strong typed captured source values/context/facts, validated result/canonical bytes, trusted creation metadata |

**PreviewPublicationRevisionRequest**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductContentReleaseId | `Guid?` | Условно | Exact Product source | ≥1 source; no Items/Version/active fallback |
| ProductVariantContentReleaseId | `Guid?` | Условно | Exact Variant source | ≥1 source; no Items/Version/active fallback |

**CreatePublicationRevisionRequest**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductContentReleaseId | `Guid?` | Условно | Exact Product source | ≥1 source; no root ID/hash/provenance/source token |
| ProductVariantContentReleaseId | `Guid?` | Условно | Exact Variant source | ≥1 source; no root ID/hash/provenance/source token |
| ChangeComment | `string?` | Нет | Creation comment | Constraints Q15 |
| ClientOperationId | `Guid` | Да | Logical public CreateRevision execution | Required body/non-empty; original input/actor receipt before new-execution preconditions Q15 |

**PublicationRevisionPreviewDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| IsValid | `bool` | Да | Composition validity | Не lifecycle token/creation authority |
| Состав preview | Уточняется | Уточняется | Items/candidates: valid result or invalid candidates | Shape Q15; no permanent revision ID/ETag |
| Excluded summary | Уточняется | Уточняется | Exclusion diagnostics | Q15 |
| Issues | Уточняется | Уточняется | Typed diagnostics | Invalid preview/status profile Q15 |
| Selected source summaries | Уточняется | Уточняется | Explicit input sources | Q15/Q20 |
| Captured input context | Уточняется | Уточняется | Input capture evidence | Q15/Q20 |

**PublicationRevisionItemDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| EntityInstanceId | `Guid` | Да | Exact real result ID | §2.1; B не A |
| EntityInstanceVersionId | `Guid` | Да | Exact content version | §2.1 |
| EntitySchemaId | `Guid` | Да | Logical schema | §2.1 |
| SchemaVersionId | `Guid` | Да | Exact schema version | §2.1 |
| SourceKind | `PublicationRevisionItemSourceKind` | Да | Provenance | String enum; включает VariantSchemaSelection; вне hash |
| OverrideTargetEntityInstanceId | Уточняется (nullable) | Условно | Override target | Required for VariantOverride |
| Display enrichment | Уточняется | Уточняется | Display metadata | Q15; не content identity |
| Order enrichment | Canonical InstanceId order §3.3 | Да | Runtime manifest ordering | Lowercase Guid D ordinal; no legacy SortOrder reinterpretation |
| Assets enrichment | Уточняется | Уточняется | Asset representation | Q15/Q19 |
| Hash enrichment | `CanonicalDigest` + `CanonicalProfileId` | Да | Exact instance/schema digest interpretation | Stored immutable triples; never live recompute |

**ProductVariantPublicationRevisionDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | `Guid` | Да | Stored revision identity | Не hash |
| ProductVariantId | Уточняется | Да | Stable owner | Historical context |
| ProductId | Уточняется | Уточняется | Captured Product context | Representation profile Q15/Q20 |
| ProductContentReleaseId | Уточняется (nullable) | Условно | Exact Product source | ≥1 source; stored |
| ProductVariantContentReleaseId | Уточняется (nullable) | Условно | Exact Variant source | ≥1 source; stored |
| ChangeComment | Уточняется (nullable) | Нет | Creation comment | Stored immutable |
| Items | Уточняется | Да | Stored materialized content | No recomposition |
| CompositionMetadata | Уточняется | Уточняется | Stored provenance/audit | Shape Q15 |
| ContentHash | `CanonicalDigest` | Да | Stored runtime manifest digest | Не ETag/artifact ID |
| ContentHashProfile | `CanonicalProfileId` | Да | Digest interpretation | `cm-publication-manifest/1` |
| Lifecycle metadata | Уточняется | Уточняется | Availability status | Own Version only headers/internal carrier |
| Creation metadata | Уточняется | Уточняется | Creation audit | Q15; stored |
| Retire metadata | Уточняется | Условно | Retirement audit | Lifecycle profile Q15 |
| Withdrawal metadata | Уточняется | Условно | Terminal ban audit | Lifecycle profile Q15 |

**Revision summary/list parameters**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| IDs | Уточняется | Уточняется | Summary identities | Q15; no per-row token |
| Created | Уточняется | Уточняется | Creation summary | Q15 |
| Comment | Уточняется | Уточняется | Comment summary | Q15 |
| Lifecycle | Уточняется | Уточняется | Lifecycle summary | Q15 |
| Hash | `CanonicalDigest` | Да | Stored manifest summary | Paired canonical profile |
| HashProfile | `CanonicalProfileId` | Да | Summary digest interpretation | `cm-publication-manifest/1` |
| Source summary | Уточняется | Уточняется | Selected sources | Q15 |
| Filters | Уточняется | Уточняется | List filters | Q15 |
| Order | Уточняется | Уточняется | List ordering | Q15 |
| Paging | Уточняется | Уточняется | List pagination | Q15 |
| Include-history profile | Уточняется | Уточняется | Historical inclusion | Q15 |

**Retire/Restore action request**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Профиль action request | Уточняется | Уточняется | Body/audit: absence/empty model/audit choice | Q15; no content/comment/technical Version |

**WithdrawPublicationRevisionRequest**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Reason | `string` | Да | Meaningful withdrawal reason | Exact constraints Q15 |

Explicit null source ≠ implicit ActiveRelease/latest. Transport omission/null encoding/error addressing Q15. Version only internal/header metadata; ContentHash not ETag and preview validity not lifecycle token. Preview does not return persisted/new saved revision identity as if created. Invalid preview candidate items/diagnostic shape and HTTP invalid-vs-error mapping Q15. Read older revisions uses stored values, not mutable display metadata as content identity.

**PublicationManifestInput** — typed normalized input shared canonical library, не Domain aggregate/HTTP DTO. Общие `CanonicalProfileId`, `CanonicalDigest`, `CanonicalRepresentation` определены один раз в [F03](F03-publish-schema-version.md#33-dto-и-parameter-models); здесь только профильные поля. Canonical root имеет fixed member order `items`, item — порядок строк таблицы ниже. Items сортируются ordinal по lowercase Guid D `entityInstanceId`; duplicate result identities запрещены. Полный nonempty membership отражает результат после overrides/exclusions, а не источники до composition.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| items | `IReadOnlyList<PublicationManifestItemInput>` | Да | Complete runtime membership | Nonempty; canonical InstanceId order; no omitted effective item |

**PublicationManifestItemInput**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| entitySchemaId | `Guid` | Да | Logical schema | Exact real result schema; lowercase D |
| schemaVersionId | `Guid` | Да | Immutable schema content binding | Exact version; no latest |
| schemaKey | `string` | Да | Frozen runtime schema alias | Capture at schema Publish; no live lookup/normalization |
| schemaContentHash | `CanonicalDigest` | Да | Frozen semantic schema digest | Binds exact schema bytes |
| schemaContentHashProfile | `CanonicalProfileId` | Да | Schema digest interpretation | Actual frozen supported source profile: /1 historical/new MVP constraints+Offset; no reinterpretation |
| entityInstanceId | `Guid` | Да | Real effective instance B | No substitution of override target A |
| entityInstanceVersionId | `Guid` | Да | Immutable values binding | Exact version |
| instanceKey | `string` | Да | Frozen runtime instance alias | Capture at instance Publish; not identity |
| contentHash | `CanonicalDigest` | Да | Exact values-only instance digest | Full values remain separate immutable instance bytes |
| contentHashProfile | `CanonicalProfileId` | Да | Instance digest interpretation | Actual frozen supported instance profile: /1 historical/new Offset MVP |

Canonical runtime manifest не дублирует полные instance values: exact version + digest/profile связывают их со stored immutable bytes. Frozen AssetReference входит в instance content profile F04; asset byte hash отдельный, URLs/storage paths/live enrichment отсутствуют. SourceKind, target A, excluded facts, release IDs, actor/comment/time, RevisionId, lifecycle и own Version отсутствуют в runtime bytes и остаются provenance/audit владельца. Одинаковый runtime manifest может иметь одинаковый digest при разных artifact IDs/provenance; это не deduplication и не adoption другой revision.

### 3.4. Ports

| Port | Назначение | Направление |
| --- | --- | --- |
| Revision repository / Publishing UoW | Insert immutable graph; exact tracked lifecycle root; atomic F11 state/history/outbox changes | Outbound Publishing.Persistence |
| F09 protected release read/use | Exact immutable sources + current lifecycle; protect valid new-use capture during Create | Intra-Publishing roots под gate §7.2 |
| ICatalogPublishingQueries / scope coordination | Applied Variant/Product ownership/eligibility/F15 blocker | Public CatalogAccess, no foreign DbContext |
| IContentModelingPublishingQueries / reference-use | Batch exact dependency/schema/target/media facts and retention guards | Public ContentModeling F04/F05, no root token writes |
| Composition service | Pure stable-ID merge and diagnostics | Domain/Application captured input only |
| Canonical representation/hash service | Typed PublicationManifestInput → CanonicalRepresentation; verify exact schema/instance bytes+profiles before binding | Pure shared .NET library; no Domain/EF/Asp dependencies |
| Clock/current actor | Trusted time/actor | Reuse host ports |
| F11 state/history / withdrawal propagation | Guard Retire vs Activate; active Withdrawal→null; durable restriction fact | Publishing-owned roots/outbox; Delivery via public contracts |
| Public revision read | Immutable materialized input + availability metadata for F17/F11/Delivery | Inbound Publishing public contract |
| MVP consistency guard / owner UoW boundary | Один global transaction exclusive gate по Q20, owner-only atomic commit и independent primary public reads | Shared infrastructure adapter; owner connection, не foreign transaction |

No direct ContentModeling/Delivery repository or HTTP client automatically added. Media bytes retained elsewhere; F10 doesn't upload/copy assets. Canonical profile contract reused by F13/F19, not generic SDK F19 document treated as already selected byte specification.

### 3.5. Specifications / read contracts

[S19](../standards/S19-specifications.md): target RevisionByIdSpecification, RevisionsByVariantSpecification; atomic ID/owner/lifecycle/created conditions, chosen filters/order/paging Q15. F09 source exact specs reused, tracking only guarded writes. Public projection captures complete stored materialized set/lifecycle/token; list summaries don't load full Items. No guessed spec.And API/new repository per Item.

Historical exact read doesn't load current releases and recompute or fail because source Retired. Display missing metadata/Withdrawn admin permissions handled explicitly Q15. Materialized public reader uses stored canonical representation Q19 and own availability facts, not current serializer over JSONB/source query. Preview coherent selected source lifecycle/context/metadata capture Q20; no latest state disguised as previously validated input.

MVP temporal Q34: manifest/source projections копируют actual frozen schema/instance content profile identifiers и digests, не помечают Offset как /1. Offset runtime bytes уже UTC и immutable у F04, F10 не переводит timezone/rehash values. cm-publication-manifest/1 сохраняется при прежнем собственном layout/semantics: эти profile fields уже часть membership tuple; unknown source profile rejected, new codecs materialize Q19/Q31. Подробные definitions/temporal algorithms — F01/F04, Publishing не создаёт temporal model/validator.

### 3.6. Schema selection provenance и source-format gate

**PublicationRevisionSchemaGroupDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SchemaId | Guid | Да | Logical group | Derived from stored final Items/source metadata |
| SchemaVersionIds | IReadOnlyList<Guid> | Да | Exact targets финальных items | Unique/sorted; возможны разные targets legacy local additions |
| ItemInstanceIds | IReadOnlyList<Guid> | Да | Final membership IDs | Join к Items по stable ID; не повторный live resolve |
| HasVariantSchemaReplacement | bool | Да | Captured logical schema mask | False для legacy metadata без нового intent |
| Sources | IReadOnlyList<RevisionSchemaSelectionSourceDto> | Да | Frozen intent/provenance | Не участвует в runtime hash |

**RevisionSchemaSelectionSourceDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ReleaseId | Guid | Да | Exact source artifact | Исторический ID |
| Kind | PublishingContentOwnerKind | Да | Product либо Variant | F09 closed enum |
| Format | ReleaseSelectionFormat | Да | Source format на materialization | LegacyExact либо SchemaRulesV1 |
| Snapshot | SchemaSelectionSnapshotDto? | Условно | Captured schema intent/result | Null для legacy; не трактовать как AllExcept |

PublicationRevisionPreviewDto и ProductVariantPublicationRevisionDto получают SchemaGroups: IReadOnlyList<PublicationRevisionSchemaGroupDto>. Existing Items остаются authoritative final payload, SchemaGroups содержит IDs/provenance для tree UI, а не независимую editable копию контента. New source picker использует F09 SelectionFormat; LegacyExact показывается с причиной «Для новой ревизии подготовьте новый релиз» и не допускается к новому Create. Backend exact guard возвращает release-review.legacy-source даже при переданном known legacy ID.

Новая materialization сохраняет frozen schema selection provenance в CompositionMetadata и internal SchemaSelectionMetadataVersion=1. EF migration добавляет publication_revisions.schema_selection_metadata_version smallint NOT NULL DEFAULT 0 CHECK IN(0,1); legacy rows остаются 0 без rewriting ItemsJson/ManifestJson/hash. New exact query строит группы только из stored Items/CompositionMetadata, не выполняет source/live lookups. Format0 группируется по stored schema/version/item IDs, Source Snapshot=null и явно legacy provenance. Это позволяет читать существующую revision даже при временной недоступности source adapters.

Source-format gate относится к новой Preview/Create и Automatic preparation, не отзывает существующую materialized revision. Её current activation/history/frozen delivery/previously-approved rollback следуют F11/F13 и обычным lifecycle guards. Реплика с неподдерживаемым metadata codec не читает его как legacy/current: объяснимый integrity refusal, без пересборки runtime bytes. Hash profile и SDK wire не меняются от добавления backend tree provenance.

## 4. Application

### 4.1. Handlers / use cases

Mutation handlers соблюдают порядок §7.2: shape/auth preparation → own ReadCommitted transaction → отдельный global gate statement → fresh tracked own roots + independent primary public facts → own token/basis/domain checks → own state/intents commit. Предварительные reads/CPU results не являются authority после ожидания gate.

Preview query handler resides Persistence, shares validation/composition application services with Create command handler in Application. Both resolve applied route ownership/F15/lifecycle, load explicitly selected F09 sources, validate source owners/Available and intrinsic patch, compose stable-ID result and batch-check required exact dependency scope. Malformed inputs/missing sources/invalid composition response profile Q15; no writes/IDs/state.Touch/setup/moderation. Current Preview returning newly constructed revision DTO isn't target.

Public Create after auth/first gate first compares confirmed ClientOperationId/input/actor receipt: identical original execution returns original result even source changed/artifact Retired; changed bound input409. Only new execution repeats protected current source/context/final-effective closure validation. Pure compose→nonempty valid unique Items→canonicalize/hash chosen Q19→Domain.Create with trusted time/actor/comment→local insert materialized graph/metadata/required broader retention facts+safe audit+original confirmed receipt→commit→map saved DTO/own ETag. No active state/source Version mutation, client preview reuse or hidden post-commit GetWorkspace.

Retire/Restore use exact revision own token. Retire coordinates F11 current pointer/activation guards; Restore only Retired and doesn't reset moderation/lifecycle of dependencies. Withdraw uses same own token, marks terminal metadata and reads protected F11 current state: only if this ID active perform null transition/new Sequence/history and atomically clear/supersede automatic head per F11; inactive Withdrawal does not fake transition/cancel replacement head. Always persist durable exact-revision restriction fact for distribution. All affected Publishing roots/history/outbox same local commit; Delivery separate consumer transaction. No direct foreign write/fallback.

### 4.2. Validators

[S18](../standards/S18-fluent-validation.md): SourceReleaseSelectionValidator checks ≥1 ID and non-empty passed GUID; public Create also composes ClientOperationIdValidator; context/RevisionId validators; Create validator composes ChangeCommentValidator; Withdraw validator composes WithdrawalReasonValidator. Retire/Restore/list models use selected pure constraints. ProductVariantPublicationRevisionConstraints/shared value constraints single source S21; no guessed string length/default.

Pure FV doesn't query active pointer, retired sources/content availability or Latest, and doesn't normalize/dedup inputs. Application/Domain validate owner, captured target/schema, cross-group uniqueness, result availability/finalnonempty and F15. Header expected Version separate. Diagnostic labels/collision paths don't require client-generated IDs.

### 4.3. Mappers

| Mapping | Owner | Rule |
| --- | --- | --- |
| Route/request → preview/create | Api | Preserve explicit null/exact IDs; no automatic current base; trusted actor/time separately |
| F09 snapshots → composition input | Application/read adapter | Copy typed refs/schema selections и четыре legacy-группы/captured schema; owner/lifecycle facts captured; no Key reconstruction |
| Composition result → preview DTO | Read mapper | Candidates/issues distinguish valid result; source/target provenance; no permanent root ID |
| Valid result → revision parameters/root | Application | Real result IDs, frozen metadata/canonical output; no new source content/field merge |
| Lifecycle own If-Match → command | Api | Own root internal expected int, no workspace state token |
| Committed/read root → DTO/token/public payload | Result/Persistence mapper | Stored Items/hash/provenance; own commit token; no later writer substituted |

### 4.4. Services / algorithms

PublicationCompositionService uses immutable source values, sharing stable-ID core with F08 but F10 has selected immutable sources/strict new-use/nonempty/materialization profile. Algorithm independent of override array order:

1. Verify at least one exact source, source owner/Available/current applied context; classify foreign/lifecycle/F15 failures. Intrinsic duplicate targets/results/reference schema errors never auto-correct.
2. Product-only/combined: index base by EntityInstanceId; duplicate input error, not dictionary overwrite. Variant-only starts empty inherited set; no fabricated base release.
3. Apply schema exclusions to inherited candidates by EntitySchemaId, then instance exclusions by EntityInstanceId. Keep trace of effects/redundancy; no local result removal or normalization of stored rules.
4. Resolve all override target/captured Schema evidence and same-schema result. Suppress **all inherited target IDs first** before inserting any local result. Missing target is allowed; no cascade where a result B becomes inherited target for another rule.
5. Insert override exact results with VariantOverride/target provenance and real B ID, even when target absent/excluded. Same-instance A→A/v5 suppresses inherited A then inserts exact local A/v5.
6. Insert additions with VariantAddition provenance. Check uniqueness against surviving inherited and all local results. Independent inherited B + A→B, addition B + A→B, A→B/C→B or duplicate target produce explicit issues; no LWW/Distinct/hash-based merge.
7. Require final nonempty. Batch-check final effective instance/schema/media closure with current new-use/Withdrawal guards, including shared dependencies of surviving items. Don't treat unavailable final content as valid because source release Available; suppressed unavailable A не blocker effective B; shared media/schema остаётся blocker, если входит в final B closure. Retained source/evidence closure separate and broader.
8. Valid final list/provenance/metadata goes to selected deterministic order/canonical pipeline Q19. Hash same bytes that are stored/exported; invalid preview never accepted as persisted canonical composition. No Domain DB/hash from current mutable source/store physical JSONB representation.

Canonical emission follows [accepted Q19 profile](../questions/Q19-content-hashes.md#принятый-canonical-profile-v1): SHA-256 exact UTF-8 bytes, 64 lowercase hex без prefix, no BOM/whitespace/final LF. Typed values reuse F03/F04 rules: strict presence/null/default, FieldId lowercase D ordinal, List order preserved, exact Int64/Decimal, finite Float64 numeric RFC 8785 rules including -0→0, explicit escaping without NFC/trim/case conversion. Это не whole-JCS serialization. Canonicalizer принимает normalized typed input и сохраняет bytes/profile/digest вместе; duplicate properties, malformed Unicode, trailing garbage/invalid values rejected before materialization. Root ID/time/comment/lifecycle/own Version не входят runtime hash. Profile changes for any byte change of an old input; historical bytes/profile/digest never rehashed. Same digest doesn't replace artifact IDs/approval/authentication.

### Композиция frozen schema selections

Domain composition получает exact Product refs и Variant schema replacement-mask/materialized refs из F09 snapshots. Удаляет из inherited layer все позиции logical SchemaId из mask независимо от их SchemaVersion, применяет оставшиеся inherited exclusions/override suppression, добавляет local schema results и legacy local results, затем проверяет final uniqueness/eligibility. Selector вместе с legacy rules той же схемы недопустим по F08, а не исправляется порядком перечисления. Captured selection intent нужен для объяснения; exact F09 refs являются authority результата.

Application.Abstractions включает эти frozen inputs и source-kind в parameters/read provenance. Application handlers запрашивают F09 public snapshots, current new-use facts только для final effective closure и обычный review/Create basis. Они не вызывают resolver AllExcept по текущему ContentModeling: новый экземпляр не должен попасть в revision из старого release. EntityFramework сохраняет final Items/CompositionMetadata и canonical manifest прежней роли; Persistence exact reads не зависят от live assignments. Host/API связывают те же source use cases; tree read shape/NSwag и provenance encoding определён §3.6 и [техническим Q39](../questions/Q39-schema-content-selection.md).

Schema rules, excluded IDs и source policy не добавляются в runtime/hash input только ради tree UI. Равный final exact runtime payload остаётся равным payload независимо от AllExcept/SelectedOnly provenance; если фактически меняются нормативные bytes, применяются versioned profile правила Q19. Новая schema-only runtime distribution или расширенная F14 compatibility не вводятся. Existing nonempty final guard сохраняется.

Приёмка: source Product содержит 20, Variant snapshot SelectedOnly содержит 2 → final ровно 2; тот же VariantRelease с другой допустимой ProductRelease всё равно заменяет logical schema целиком; новое Published instance после F09 не расширяет F10; удалённые live rules не меняют исторический snapshot; одинаковые exact results разных policies дают одинаковый runtime manifest. Unit SUT — pure composition; integration/API SUT — сохранение и повторное чтение immutable revision с captured source evidence. SDK и Unity не получают AllExcept как исполняемое клиентское правило.

## 5. Api

### 5.1. Endpoints

Accepted routes из прежней F10. Целевые ProductVariantPublicationRevisionController для context preview/create/list и PublicationRevisionController для exact/lifecycle: bind/request/header→IQueryBus/ICommandBus→общий ToActionResult S16. Имена целевые, не current files; controllers не compose/write foreign contexts.

| Method | Route | Request | Response | Access |
| --- | --- | --- | --- | --- |
| POST | `/api/v1/products/{productId}/variants/{variantId}/publication-revisions/preview` | Preview request §3.3 | Preview DTO; success/invalid-result profile Q15 | MVP Specialist/context validation |
| POST | `/api/v1/products/{productId}/variants/{variantId}/publication-revisions` | Create request §3.3 | Created materialized DTO + own ETag; status/Location Q15 | New-use/F15 scope guards |
| GET | Тот же collection route | Selected list parameters Q15 | Revision summaries/list shape Q15, 200 | History/read profile |
| GET | `/api/v1/publication-revisions/{revisionId}` | Exact ID | Stored DTO + own ETag, 200 | Admin/history/access profile Q15 |
| POST | `/api/v1/publication-revisions/{id}/retire` | Own If-Match; action body profile Q15 | Lifecycle DTO + own ETag; status Q15 | Active guard F11 |
| POST | `/api/v1/publication-revisions/{id}/restore` | Own If-Match; action body profile Q15 | Same result, only from Retired | Lifecycle permission profile |
| POST | `/api/v1/publication-revisions/{id}/withdraw` | Own If-Match + Reason | Withdrawn DTO/own ETag + propagation status profile Q15 | Distribution-ban action; access Q10/Q15 |

Route ProductId checked against **applied** parent F06; pending move doesn't become context, old-parent POST isn't automatically redirected/retried. Exact identity/history survives move/source inactivity, read/profile Q15; no global revision CRUD/delete/content/comment edit. Pick sources only F09 reads; preview doesn't create releases/setup. Withdrawal doesn't expose browser Delivery mutation API.

### 5.2. ETag / If-Match

Own lifecycle root internal int Version→strong numeric ETag S16, never DTO/body. Exact revision GET and successful Create/lifecycle return own token. Retire/Restore/Withdraw require revision If-Match: missing428, own stale412, domain409. List/ContentHash/Variant publication-state/source release tags not revision token.

Preview/Create no required workspace/source If-Match: source content immutable, lifecycle/parent/dependency facts still change and must be protected/revalidated Q20. Preview IsValid is neither creation consent nor source lifecycle fence. Own tag doesn't prove current ActiveRevisionId/F17 approval/Delivery visibility. Withdrawal multi-root guard internal; no mandatory second state header invented.

Conditional commit failure classified after rollback/disposal against correct revision root; external guard/state/context race isn't automatically412. No-op/repeated Withdraw grammar/error priority Q15/Q20. Source/hash don't dedup new-ID Create. Lost Create ACK → same original ClientOperationId/input receipt inquiry/replay; no blind newtag/oldparent execution. Confirmed result maps committed root, no hidden GetWorkspace/exact result GET.

### 5.3. OpenAPI и ошибки

Separate preview/create/exact/list/lifecycle models and ETag/If-Match metadata. Exact/list200; preview invalid typed result vs 400/404/409 profile Q15, Create/lifecycle success profile Q15. Structural validation400, missing404/profile readiness, domain source/ownership/composition/F15/active/terminal conflict409, own412/428, real permissions401/403. Machine issue/path/participating IDs/severity/CORS/header carrier/NSwag Q15.

Withdrawn admin-history exact DTO doesn't imply distribution allowed; F13 denies server delivery. Reason plain text with selected constraints, no generic IsRevoked meaning hiding differences Retired/Withdrawn. Current `/publishing/variants/{id}/preview|revisions` and `/publishing/revisions/{id}/revoke` aren't accepted context/own-lifecycle contracts automatically. Backend/OpenAPI before NSwag/UI, no hand-edit generated models.

## 6. EntityFramework

### 6.1. DbContext ownership

PublishingDbContext schema publishing owns revision roots/materialized Items/metadata/canonical representation/lifecycle. F09 source roots and F11 state/history/outbox same BC, different concurrency owners. Delivery has separate projection, no direct foreign DbContext writes target. Public reference facts/retention coordinated across BCs Q20, not cross-BC FK/shared transaction claim.

### 6.2. Configurations

| Entity | Representation | Keys / indexes | Concurrency / immutable state |
| --- | --- | --- | --- |
| ProductVariantPublicationRevision | publishing.publication_revisions target root | RevisionId PK; VariantId/CreatedAt query indexes; captured Product context; source IDs nullable ≥1 | Own Version int lifecycle token; fixed sources/comment/creation/hash |
| PublicationRevisionItem | Normalized owned item rows | RevisionId + EntityInstanceId unique; version-ID diagnostics index if selected | Immutable exact IDs/SourceKind/target provenance; no per-item tag |
| CompositionMetadata | Immutable diagnostic/provenance value; JSONB допустим | Owner root; no independent merge authority | No updates on lifecycle; exact shape/storage Q15/Q20 |
| Canonical representation/profile | Immutable byte storage + profile ID + 64-hex digest | Owner graph atomic triple; `cm-publication-manifest/1` | Digest over exact preserved bytes; no JSONB rerender or lifecycle rehash |
| F11 state/history/outbox | Existing logical Publishing-owned roots/append records | Variant/Sequence unique, delivery fact identity profile | Protected current pointer/own state Version; local atomic withdrawal |

Physical child/table/column/profile names Q20. Current [PublicationRevisionConfiguration](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.EntityFramework/Configurations/PublicationRevisionConfiguration.cs) maps ItemsJson jsonb/ManifestJson text/ManifestHash and ignores computed Items, with Variant/CreatedAt index. It doesn't provide normalized target Items, own Version/Available-Retired-Withdrawn/comment/metadata mapping. Current ManifestHash length and RevocationReason2000 aren't accepted constraints.

### 6.3. Constraints / indexes

Nonempty valid source selection/final Items, stable result uniqueness, valid source kind/required target for override, required exact IDs; no source/content foreign BC FK. Product/Variant source ownership Domain/handler guard, not cross-BC SQL join. Local Publishing source retention links nullable, no source hard pin from provenance.

Lifecycle-only root update never updates/deletes immutable child/content/comment/source/hash/creation rows. Immutability safeguard selected Q20 and verified via real EF/SQL behavior, not assumed from private setters. Withdrawn terminal; deletion not public or automatic migration shortcut. Sequence applies F11 transitions, not RevisionNumber or unique ContentHash dedup. No arbitrary source Key-based index as uniqueness authority.

## 7. EntityFramework.Persistence

### 7.1. Repositories / query adapters

Host/DI задаёт owner-partitioned UoW participants: в transaction входят только DbContext/own outbox/inbox данного BC. Текущий EfCoreUnitOfWorkParticipant, способный объединять несколько DbContexts в общей transaction, не является target coordination adapter: participant selection разделяется по owner BC, foreign public read adapter создаёт независимый primary scope/connection без ambient enlistment. Guard adapter использует owner connection/transaction; mapping/repository другого BC в command handler не внедряется.

One revision root repository inserts full materialized graph and loads tracked lifecycle root. Query handlers/specifications/projectors live Publishing.Persistence S20. List summaries profile Q15, exact/public read consistent stored content+lifecycle+own token, Preview adapter captures selected sources/public facts and invokes shared validation/composer. No repository per Item/metadata or history recomposition on read.

DI reuses public F09/Catalog/ContentModeling contracts and selected canonical/outbox/state ports, one accepted command/query path. Current [ContentManagerApplicationModule](../../../../contentmanager.api/src/App/ContentManager.Application/ContentManagerApplicationModule.cs) registers App PublishingService/IPublishingService; target revision handlers not simultaneously another bool-switch algorithm. Public materialized reader serves F17/Delivery without sharing DbContexts.

### 7.2. Transactions

По [MVP consistency guard](../questions/Q20-persistence-and-migration.md#mvp-протокол-consistency-guard) каждый invalidating writer начинает **свою** BC transaction с явно заданным ReadCommitted и отдельным первым SQL statement получает один стабильный глобальный exclusive pg_advisory_xact_lock. Lock выполняется на том же открытом owner connection, на котором будут authoritative reads, row locks, запись и commit; общий ключ одинаков во всех репликах CM. До получения gate нельзя authoritative-read/check/row-lock затронутый basis. Для replayable Create/StartPromotion/Submit первым authoritative own read после gate идёт confirmed CommandExecution lookup; только новый execution загружает tracked business roots и проверяет expected Version. Остальные writes заново загружают own roots/expected token; чужие current facts читает только через public read ports в независимом primary connection/scope, без ambient UoW enlistment, foreign EF, shared SQL transaction, cache или read replica. Публичный read carrier остаётся предметом Q15; механизм сериализации уже выбран.

Все способные инвалидировать проверяемый basis writers используют тот же gate: F07 whole Save/setup, F08 patch Save/setup, ContentModeling/Media lifecycle/reference changes, CatalogAccess applied relation/F15 Resolve, F09 CreateRelease/Retire/Restore, F10 CreateRevision/lifecycle/Withdrawal, F11 Select/Activate/Rollback/Unpublish и promotion capture/per-item/stage/late Approval, F12 policy changes, F15 scoped repair, релевантные F17 decisions и Delivery-owned invalidation commits. Каждый изменяет только own BC state. Own state/token, required own outbox, safe audit intent и inbox marker фиксируются атомарно; commit/rollback освобождает transaction lock. Это общий write gate в одной PostgreSQL database между репликами, а не общая транзакция BC.

CPU-подготовка, immutable projection/canonical hashing и предварительные проверки могут быть снаружи gate, но после acquisition весь влияющий на commit basis заново подтверждается до применения подготовленного результата. Gate нельзя удерживать во время UI/человеческого Approval, VR School HTTP, S3/bytes, broker send/ACK, ожидания Delivery projection либо HTTP stream; эти действия и downstream commits имеют отдельные границы.

Acquisition wait ограничен выбранной конфигурацией и cancellation; timeout/cancel/lock failure — технический отказ, не domain Conflict. Failed/partial public port не превращается в empty compatible facts. Потеря owner connection прерывает операцию целиком; reconnect не продолжает прежний basis. Own expected Version mismatch остаётся 412; independent relevant basis drift — 409 с повторной проверкой/review, без автоматического retry с новым ETag. Потерянный commit ACK означает unknown outcome: inquiry по durable operation identity/выбранному Q15 recovery, а не утверждение rollback или слепой повтор.

Read queries, pickers, preview/review не получают этот global gate и не выдают commit permit. Own DTO и token читаются согласованно из одного owner поколения; composite basis/freshness других roots учитывается отдельно. Последующая mutation получает новый gate и перепроверяет basis.

F10 DF02 Create и DF05/DF06/DF07 Retire/Restore/Withdrawal получают gate до authoritative source/current-state reads. Create повторно подтверждает применимый source/dependency scope и выбранный canonical input; final-effective instance/schema/media closure выбран §2.3; broader source/evidence retention remains. Retire/Activate и Withdrawal/current pointer сериализуются: active R1 Withdrawal очищает только R1; уже выбранный R2 не очищается. Lifecycle/state/Sequence/history/exact restriction intent и own outbox фиксируются одним Publishing commit.

Create inserts root+all materialized Items+metadata+canonical bytes/hash/broader retention facts+original confirmed receipt atomically; internal F11 prepare adds own F17 Pending+exact tuple/AwaitingModeration in the same caller Publishing UoW. Source content fixed but Available metadata/catalog relation/F15/content lifecycle protected через MVP gate §7.2, not stale read permission. Don't bump F09 source/assignment/F11 state Versions merely for new revision. New client ID means new artifact even identical content/comment, source Version not bumped. Same bound client ID/original input receipt replay before current preconditions; changed bound input409.

Retire vs F11 Activate/Rollback serialized guard: can't retire current pointer, can't activate concurrently committed Retired/Withdrawn revision. Restore own metadata only, not new approval/activation. Withdrawal revision row, affected F11 pointer/Version/Sequence/history and durable restriction/outbox commit together. If current pointer changed to unrelated R2, withdrawing R1 doesn't clear R2 or emit fake null transition. MVP transaction gate §7.2 сериализует capture и lifecycle/state writes; no client state ETag substituted for internal guard.

Inactive Withdrawal needs durable exact-ID restriction distribution even without pointer change/history record. Active Withdrawal additionally emits F11 null transition; null event alone doesn't convey arbitrary frozen historical revision ban. Publishing API success means local commit, not broker ACK/Delivery effective commit or immediate termination of all started sessions. Delivery propagation/guard/read in-flight semantics Q17/Q20/F13, no synchronous foreign transaction.

After pre-commit failure original rows/Items/current state/history/outbox intact. Lost response/cancellation after commit unknown outcome; same original client ID/input receipt inquiry/replay, no claim rollback/new execution blind retry. Own lifecycle result maps that commit, not later GetWorkspace. Existing revisions stay stored after source Retire or profile change; restrictions separate from immutable content.

### 7.3. Migrations

Add target lifecycle/audit/comment/own Version/normalized Items/metadata/canonical-profile storage with scoped Publishing migration; preserve materialized history. Current ItemsJson/manifest can be actual captured source evidence but no explicit A→B target may be inferred from Keys. Legacy IsRevoked vs terminal Withdrawal/audit policy selected Q20, not a guessed lossless migration.

CM dev transition/no production legacy obligations Q20 doesn't authorize automatic historical deletion; selected recreate/migrate policy explicit. Shared F09/F11 mappings not collateral refactor. PostgreSQL roundtrip/lifecycle-only updates/nonempty/source-null constraints/retention/outbox rollback/races checked in implementation. Analysis executes no migrations.

## 8. External adapters

### 8.1. CatalogAccess / ContentModeling

Public applied relation/eligibility/F15 read and protected mutation-use facts; batch exact schema/instance/media dependencies. No foreign tracked tokens incremented to emulate reservation. Captured override target schema from trusted F09 intent, not client or guessed current Key. Final effective invalid dependency always blocks new materialization; suppressed source excluded from new-use/runtime closure, broader trusted evidence retained. Historical target/display missing doesn't trigger new merge/root rewrite.

### 8.2. Canonical and public materialized read

Canonical representation/hash boundary Q19 produces preserved bytes+digest/profile once. F13/F19 consumer uses selected same contract; general JSON serialization/current CanonicalContent helper not enough proof. Public exact revision read serves immutable Items/provenance/representation plus lifecycle availability; admin history and distribution reasons differentiated. Media bytes reside F05; no blob copy into revision.

### 8.3. Publishing → Delivery withdrawal propagation

Recoverable Publishing outbox stores restriction fact on every new Withdrawal and F11 transition fact if active. Dispatcher/transport/inbox/public materialized reader/consumer contracts Q20/Q17/F11/F13; no invented queue/message name or claim current implementation. Consumer atomically updates its own distribution ban/projection; duplicate/out-of-order/retry handling chosen protocol. Domain event alone/broker send isn't completed Delivery ban. Client installed data deletion absent.

## 9. Data flows

### Транспортные потоки

[S17](../standards/S17-data-flows.md). DF08 separates durable local intent, delivery and consumer commit; DF09 reuses F09 reads, no new source API.

| Поток | Транспорт | Метод / канал | Endpoint / destination | Доступ | Вход | Application action | Выход | Успех | Ошибки |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| DF01. Preview composition | HTTP API | POST | `/api/v1/products/{productId}/variants/{variantId}/publication-revisions/preview` | MVP read/validation/context | Exact nullable source IDs, ≥1 | PreviewPublicationRevisionQuery | Preview items/issues/source context |No writes; success/invalid envelope Q15 |400,404,domain/invalid profile409/access/port failures |
| DF02. Create revision | HTTP API | POST | `/api/v1/products/{productId}/variants/{variantId}/publication-revisions` | New-use/F15/owner guards | Exact sources+comment; no client result/hash | CreatePublicationRevisionCommand | Stored DTO+own revision ETag |Status Q15; materialized commit, no activation |400,404,409,context/access/storage/guard failures |
| DF03. List history | HTTP API | GET | Same collection route | History/applied context profile | IDs + selected list parameters Q15 | ListPublicationRevisionsQuery | Summaries/list shape Q15 |200, no per-row token |400,context/missing/access/storage failures |
| DF04. Exact revision | HTTP API | GET | `/api/v1/publication-revisions/{revisionId}` | Admin/history profile | RevisionId | GetPublicationRevisionQuery | Stored content/lifecycle+own ETag |200; no merge or source latest |404/access/read failure; distribution guards separate |
| DF05. Retire | HTTP API | POST | `/api/v1/publication-revisions/{id}/retire` | Lifecycle/current-active guard | RevisionId, own If-Match; body Q15 | RetirePublicationRevisionCommand | Lifecycle DTO+own ETag |Metadata-only commit/status Q15 |400,404,409 active/domain,412,428,access/storage |
| DF06. Restore | HTTP API | POST | `/api/v1/publication-revisions/{id}/restore` | Lifecycle permission | ID, own If-Match; body Q15 | RestorePublicationRevisionCommand | Available DTO+own ETag |Only Retired→Available; no activation |400,404,409 terminal/state,412,428,access/storage |
| DF07. Withdraw | HTTP API | POST | `/api/v1/publication-revisions/{id}/withdraw` | Distribution-ban authority Q15 | ID, Reason, own If-Match | WithdrawPublicationRevisionCommand | Withdrawn DTO+own ETag/result profile |Local revision/state/history/outbox commit |400,404,409,412,428,guard/transaction/access failures |
| DF08. Propagate ban/null state | Publishing outbox → selected async transport → Delivery | F11 state fact + withdrawal fact Q20 | Delivery public consumer/destination Q20/Q17 | Internal trusted contract | Exact withdrawn ID/ban metadata; optional null pointer+Sequence | Dispatcher + Delivery consumer |Distribution restriction / effective null readiness |Separate dispatcher ACK and Delivery local commit |Publish/consume/storage failures, duplicate/out-of-order/recovery profile |
| DF09. Select source releases | HTTP API reuse F09 | GET | F09 `/products/{productId}/content-releases`, `/products/{productId}/variants/{variantId}/content-releases` + exact release routes, prefix `/api/v1` | F09 read/new-use picker context | IDs/profile/select exact Available source | F09 list/exact queries |Source summaries/immutable patch/lifecycle facts |200; explicit source ID or explicit none |F09 errors; current availability rechecked DF01/02 |

### DF01. Рассчитать preview

1. Api/FV binds applied Product/Variant route and exact nullable IDs; ≥1, no automatic ActiveRelease/latest/default empty source, no user result/hash.
2. Resolve applied context/ownership/F15 facts and exact F09 snapshots/current lifecycle. Source missing/foreign/Retired distinct from valid source with empty/invalid composed result; response profile Q15.
3. Capture typed input; pure stable-ID algorithm §4.4 plus required batch dependency checks profile. Use trusted stored target schema, excluded-only-inherited/no-cascade/final uniqueness/nonempty.
4. Map IsValid/items or diagnostic candidates/issues/excluded/source metadata. No persisted ID/clock/root/version/state.Touch/approval/outbox. No If-Match required as write fence; input context/freshness Q20.
5. Preview marks selected inputs only, not authority later Create. Invalid result envelope/status and partial diagnostic support Q15.

### DF02. Материализовать revision

Public auth/first gate → confirmed ClientOperationId receipt lookup до перечисленных new-execution source checks. Internal F11 prepare вместо повторного public Create использует frozen item/own tuple и caller Publishing UoW, атомарный с F17 Submit.

Внутренний write path после Api/FV/проверки caller: открыть owner ReadCommitted transaction, отдельным первым statement получить global pg_advisory_xact_lock на owner connection; только затем выполнять указанные ниже authoritative reads/row locks. Foreign facts — independent primary public ports без enlistment. Все guard/basis checks повторяются для этого commit; §7.2 задаёт failure/recovery и запрет внешнего ожидания под gate.

| Шаг | Исполнитель | Вход / чтение | Преобразование и проверка | Запись / выход |
| --- | --- | --- | --- | --- |
| 1 | Api/FV | Context, exact sources/comment | Pure selection/IDs/comment/access; no preview DTO authority | Create command |
| 2 | Public context/protected F09 reader | Applied parent/F15/source owners/lifecycle and snapshots | Gate до authoritative capture §7.2; no latest/provenance hard pin | Captured authoritative input |
| 3 | Shared composer/ContentModeling ports | Exact input/facts | Full same validation as DF01, re-run now; stable IDs/collisions/dependencies/nonempty | Valid Items/provenance/metadata или full отказ |
| 4 | Canonical service + Domain | Complete effective Items, frozen aliases, exact immutable schema/instance triples; separate provenance/comment/actor | Verify input triples, typed manifest fixed order §3.3 → exact bytes/profile/SHA-256; outside-gate preparation rechecks all basis inside §7.2 | New Available graph; real B identity; bytes/profile/digest atomic with Items |
| 5 | Publishing UoW | Root/Items/metadata/representation/retention | Atomic insert with current source/guard validity protected | Commit; no source/state/approval/Delivery mutation |
| 6 | Result mapper/Api | Own committed root/token | Return DTO+own ETag, status profile Q15 | No hidden workspace GET; lost response unknown outcome |

### DF03. Получить список revisions

Bind applied context/list parameters; verify owner/read/history profile. Apply RevisionsByVariantSpecification and selected filters/order/paging/lifecycle inclusion/tie-break/count Q15. Project summaries, no full content preload or per-row ETag. Return200; empty confirmed list differs from failure. Source move/read context policy Q15/F06; history not rewritten or silently dropped by new-use picker filtering.

### DF04. Прочитать stored revision

Read exact ID/access; project immutable stored Items/source IDs/metadata/hash/canonical profile plus own lifecycle/audit/token coherently. Source Retire/assignment change/algorithm update doesn't recompute or invalidate stored snapshot read. Administrative Withdrawn history response permitted profile Q15; distribution reader/F13 evaluates ban separately. Return200 DTO+own ETag, no F09 reload as composition authority/write.

### DF05. Retire

Внутренний write path после Api/FV/проверки caller: открыть owner ReadCommitted transaction, отдельным первым statement получить global pg_advisory_xact_lock на owner connection; только затем выполнять указанные ниже authoritative reads/row locks. Foreign facts — independent primary public ports без enlistment. Все guard/basis checks повторяются для этого commit; §7.2 задаёт failure/recovery и запрет внешнего ожидания под gate.

Load tracked exact revision/own Version; parse/check own If-Match. Coordinate F11 active pointer/Activate/Rollback: current ID cannot retire, no implicit Unpublish or fallback. Apply Retire, conditional own metadata/Version commit, return own DTO/ETag. Immutable Items/hash/source/history/Delivery untouched; active-guard race distinct from own stale412. Selected repeated state behavior Q15/Q20.

### DF06. Restore

Внутренний write path после Api/FV/проверки caller: открыть owner ReadCommitted transaction, отдельным первым statement получить global pg_advisory_xact_lock на owner connection; только затем выполнять указанные ниже authoritative reads/row locks. Foreign facts — independent primary public ports без enlistment. Все guard/basis checks повторяются для этого commit; §7.2 задаёт failure/recovery и запрет внешнего ожидания под gate.

Load exact root/own token/access; only Retired, Withdrawn terminal cannot restore. Apply chosen lifecycle/audit metadata convention and own conditional commit. Return Available DTO/ETag, no content replacement/approval reset/activation/outbox-null. Dependency/F17/F11 new-use checks remain; available artifact isn't automatic activation consent. No-op/recovery Q15/Q20.

### DF07. Withdrawal

Внутренний write path после Api/FV/проверки caller: открыть owner ReadCommitted transaction, отдельным первым statement получить global pg_advisory_xact_lock на owner connection; только затем выполнять указанные ниже authoritative reads/row locks. Foreign facts — independent primary public ports без enlistment. Все guard/basis checks повторяются для этого commit; §7.2 задаёт failure/recovery и запрет внешнего ожидания под gate.

1. Bind exact ID/Reason/own revision If-Match; pure validation/access, load root/current lifecycle. Own stale412/missing428; already Withdrawn/repeat profile Q15/Q20.
2. Capture protected F11 current pointer/state/sequence/automatic intent; actual active Withdrawal clears/supersedes head atomically, inactive keeps unrelated head. Fresh gate guards concurrent activation/lifecycle. Source own tag alone doesn't prove active pointer freshness.
3. Mark terminal Withdrawn/audit/reason, preserving content/hash. If active ID still this revision, transition to null, advance state Version/Sequence and append immutable history Reason=Withdrawal. If different current R2, leave R2 pointer/history unchanged; no fallback/null regardless of captured old state.
4. Persist durable exact-ID withdrawal availability fact **always**; persist F11 null transition outbox when state actually changed. Revision/state/history/outbox one Publishing commit; failure leaves all unchanged.
5. Return committed revision DTO/own token/result profile, not Delivery success. No direct DeliveryDbContext, GetWorkspace result read or distribution bytes deletion. DF08 completes separate propagation; frozen/inactive bans must not be lost.

### DF08. Доставить restriction и optional null transition

1. Dispatcher reads committed Publishing outbox. Exact withdrawal fact identifies forbidden revision; active-null fact uses F11 per-Variant Sequence. Exact payload/routing/order/dedup/ack/recovery profile Q20/Q17; no imaginary queue.
2. Publish/delivery retry without re-withdrawing root or incrementing pointer/history. Send/transport ACK not consumer commit. Consumer validates trusted contract, deduplicates and coordinates out-of-order/restriction facts with later activations.
3. Delivery stores terminal exact-ID ban in its own transaction; frozen/history lookup/session/chunk/download guards selected F13/Q17. Optional active-null effective transition follows F11 Sequence/readiness. Clearing current pointer alone doesn't prohibit old frozen ID.
4. Availability ban remains even if other revision later becomes effective; no automatic replacement/unwithdraw. Public materialized payload read doesn't recompute history. In-flight/stale projection rejection guarantees Q17/Q20, not immediate assumed synchronous API visibility.
5. Ack after consumer commit; failed processing recoverable per selected protocol. Publishing HTTP commit/result and client distribution state remain distinct observations.

### DF09. Выбрать exact releases

Reuse [F09 DF03–06](F09-content-releases.md#9-data-flows): explicit source list/exact read with owner/current Available profile. Product source optional independently Variant source; ≥1 and exact version IDs already inside immutable release. Explicit none never replaced with active/Latest by UI or API. Optional Variant PreparedAgainst link metadata only. F09 list isn't source ETag/precondition; lifecycle may change after picker/preview, DF02 repeats protected checks. No assignment picker/implicit Save/CreateRelease in F10.

## 10. Ошибки и диагностика

| Состояние / отказ | Реакция | Scope |
| --- | --- | --- |
| No sources/invalid Guid/comment/reason |400 validation profile |No implicit base/auto-normalization/content mutation |
| Missing source/revision/owner |404/context-readiness profile Q15 |Не fake empty/latest selection |
| Wrong source owner, Retired release, forbidden dependency/F15 |409 либо invalid preview profile Q15 |Create blocked, historical stored reads remain distinct |
| Collision/cross-schema/intrinsic invalid/empty final |Typed preview issues; Create refusal409/profile Q15 |No dedup/LWW/valid-looking partial result |
| Target absent in selected base |Valid local result/provenance or separate source-evidence issue |Absent-in-base alone no error; missing source facts Q07/Q09/Q15 |
| Active revision Retire |409 guard |First switch/Unpublish F11; no implicit clear |
| Restore Withdrawn/terminal action |409 state conflict |Withdrawal isn't moderation iteration cancellation |
| Missing/stale own lifecycle tag |428/412 |Revision root, not current pointer/source/ContentHash |
| Withdrawn distribution request |Explainable F13 refusal, no replacement |Frozen/history bytes denied; admin history retained |
| Unsupported canonical profile/corrupt exact bytes/digest mismatch |Technical integrity failure; no rehash/auto-repair or guessed comparison |No materialization/false valid preview; source artifacts unchanged |
| Invalid normalized canonical input |Typed validation failure before result |Absent/null/default distinct; no silent default fill/numeric rounding |
| Port/storage/guard failure before commit |Rollback local affected roots/history/outbox |No partial create/withdraw/null; foreign facts not fabricated |
| Loss of response after commit |Unknown outcome/reconciliation profile |No automatic duplicate Create/retry or false rollback |
| Publishing committed, Delivery pending |Captured propagation/readiness result Q15/Q17 |No immediate effective/null/ban completion claim |

Log revision/Variant/applied Product IDs, selected source IDs, correlation/guard category, participating target/result IDs, lifecycle action/commit stage, Sequence/outbox identity where selected. No content JSON/credentials/full comment/reason required. Event «revision created» not approved/active/delivered. Terminal ban and effective-pointer changes different diagnostic facts. Codes/severity/item addressing/visibility Q15.

Global gate timeout/cancellation/failed port/owner connection loss — технический abort, не domain409 или empty valid facts. Unknown commit ACK требует inquiry/operation identity по Q15; reconnect continuation/blind retry с новым ETag не допускаются.

## 11. Тестирование

[S04](../standards/S04-testing.md)/[S14](../../../catalog/docs/standards/dotnet/testing.md#test-environment-ownership-and-bounded-parallelism): будущая стратегия, тесты приложения этой аналитикой не запускались. Existing [PublicationRevisionTests](../../../../contentmanager.api/tests/BoundedContexts/Publishing/ContentManager.Publishing.Tests.Unit/PublicationRevisionTests.cs) проверяет frozen release overlay/current Key-based rules and source existence; [PublicationInvariantTests](../../../../contentmanager.api/tests/BoundedContexts/Publishing/ContentManager.Publishing.Tests.Unit/PublicationInvariantTests.cs) — legacy exclusions/hash values. [PersistenceTests](../../../../contentmanager.api/tests/BoundedContexts/Publishing/ContentManager.Publishing.Tests.Integration/PersistenceTests.cs) real PostgresSuite/DatabaseTest verifies old revisions/manifest roundtrip, не target normalized Items/lifecycle/If-Match/withdrawal-outbox protocol.

Unit SUT: composer/constraints/lifecycle/mappers/FV/accepted canonical pipeline; controlled public facts/clock doubles допустимы для orchestration. Integration SUT: real PostgreSQL/Testcontainers, root/normalized Items/metadata/canonical roundtrip/conditional writes/selected public guards/source lifecycle and F11 races/outbox atomicity. Functional: один CM host+real PostgreSQL/JWT/MVP permissions и HTTP/OpenAPI/headers/routes; несколько BC одного API не E2E. E2E DF08: real Publishing/selected transport/Delivery stores/public reader/F13 guard, не mock chain as proof.

Собственные source/owner/content/revision IDs; no shared mutable seed; isolation/cleanup existing fixture. Races separate contexts/scopes/connections/barriers, не sleeps. Composer literal expected real-ID/provenance/issue results, not same production helper oracle. Shared [literal canonical vectors](../canonical-v1-vectors.md) содержат exact bytes/profile/digest; expected не вычисляется production serializer. Real .NET server/Unity SDK interoperability и storage roundtrip проверяются отдельно: JSONB equality/helper comparison не доказывают совместимость. Verify affected rows/state/history/outbox and unchanged sources. Это стратегия, не результат запуска.

| ID / DF | Уровень / SUT | Сценарий / данные | Ожидаемый результат |
| --- | --- | --- | --- |
| W01 / DF01–02 | PostgreSQL Integration | Suppressed A retired/withdrawn or missing current new-use evidence; effective B valid; shared media of B unavailable | A alone does not block B; shared final-effective dependency blocks; broader source/evidence retained |
| W02 / DF02 | PostgreSQL/Functional fault | New-ID identical sources/content/comment; same original ID after lost ACK/source Retire; changed bound input | Distinct new artifact IDs; original receipt replay/no Restore/Submit/Activate; changed input409 |
| W03 / F11,DF07 | PostgreSQL Integration | Crash during atomic revision+Pending+item tuple; active vs inactive Withdrawal with automatic head | All prepare rows or none; actual active Withdrawal cancels head with Sequence; inactive does not fake history |
| T01 / DF01–02 | Unit/Functional | Product-only, Variant-only, combined; no sources; exclusion-only empty final |Explicit sources honored; invalid none/empty rejected; no fake release/latest |
| T02 / DF01–02 | Unit composer | Same-instance A/v2→A/v5, same-schema A→B with other SchemaVersion |Inherited target suppressed, actual B/local exact version/provenance; cross-schema rejected |
| T03 / DF01–02 | Unit composer | Schema+instance rules with local addition/override; redundant exclusions |Inherited only removed; local survives, independent rules unchanged |
| T04 / DF01–02 | Unit + PostgreSQL | Target absent from base/Variant-only; source target missing; A later reappears in another selected release |Captured intent/result preserved; distinct evidence scope Q09; newly composed result valid per selected input |
| T05 / DF01–02 | Unit composer | Independent B+A→B; addition B+A→B; A→B/C→B; duplicate target/source |Explicit collisions, no LWW/Distinct/partial valid composition |
| T06 / DF01–02 | Unit composer | A→B and B→C in reordered arrays |All inherited targets suppressed before locals; local B not cascaded away; deterministic result/profile |
| T07 / DF01→02 | Functional+real guards | Release Retire/owner move/F15/dependency Withdrawal after valid preview |Create repeats validation/protected capture; saved preview not authority; no guessed source If-Match |
| T08 / DF02,04 | PostgreSQL/history | Create then assignment/source Retire/base/hash algorithm changes |Stored Items/source/hash/provenance unchanged; historical read not recomposed |
| T09 / DF02,04 | Shared canonical library/server interop + PostgreSQL storage | Literal fixtures all 15 types: controls/Unicode/malformed Unicode, Int64 extremes, exact Decimal, Float64 RFC Appendix B/-0, missing/null/default, nested Lists/FieldIds; reordered input membership |Exact precomputed bytes/profile/64-hex digest; no rounding/default fill/order loss; persisted triple byte-for-byte unchanged |
| C01 / DF02,04 | Composer/canonical/storage | Same runtime with different releases/SourceKind/target A/excluded facts/actor/comment/RevisionId; effective B; schema or instance exact version/alias/hash change |Provenance-only changes leave manifest hash equal with distinct artifacts; runtime changes change bound manifest; full values separately immutable |
| C02 / DF02,04 | Architecture + fault integration | Unknown profile, corrupted stored bytes, prepared basis changes while waiting gate, lifecycle change/new profile deployment |Fail without fallback/rehash; fresh basis recheck; atomic bytes/profile/digest+Items; no historical update; pure common library no Domain/EF/Asp refs |
| T10 / DF03–04,09 | Functional/query | Selected history/filter/owners/exact IDs/Withdrawn admin read/explicit source none |Correct shape after Q15; list not own token; no active fallback/source history hiding |
| T11 / DF05–06 | PostgreSQL+Functional | Missing/stale/valid revision tag, Retire→Restore, Withdrawn restore |428/412/domain409; own metadata only, no approval/activation/content edits |
| T12 / DF05 | PostgreSQL concurrent roots | Retire current revision; Retire vs Activate/Rollback/Unpublish |Protected active/lifecycle guard; no active Retired target/implicit clear |
| T13 / DF07 | PostgreSQL/UoW | Active R1 Withdrawal; active pointer concurrently R2; inactive R0 frozen |Active only correct ID→null/history Sequence; unrelated R2 preserved; exact R0 ban persisted |
| T14 / DF07 | PostgreSQL fault | Failure writing revision/state/history/outbox; cancellation/response loss |Atomic rollback or committed own DTO/token; no partial ban/null/false rollback/resultGET |
| T15 / DF08 | Real contour E2E | Dispatcher/consumer crash, duplicate/reorder, inactive/frozen ban, delayed projection |Selected ack/recovery/Sequence/ban persists; all forbidden exact-ID distribution refused, no fallback |
| T16 / DF02,07 | Functional + fault integration | Repeated unchanged-source Create; lost response; repeated Withdraw/no-op profile | Create allowed without source token bump; new-ID artifact vs same-ID original confirmed receipt; codec/recovery Q15; hash/token not artifact identity |
| T17 / all | Architecture/OpenAPI | DI/handler/models/routes/public dependencies |Commands Application/queries Persistence, no foreign DbContexts/createActivation/VersionDTO/PUT/delete/currentRevision precondition |

| ID / DF | Уровень / SUT | Исходные данные / сценарий | Ожидаемый результат |
| --- | --- | --- | --- |
| G01 / guarded write flows | PostgreSQL Integration, separate owner connections/CM replicas | Pause writer after gate; concurrent relevant writer with same stable key | Second writer waits; first commits own BC only; second loads fresh committed facts, no forbidden stale commit |
| G02 / guarded write flows | PostgreSQL Integration + architecture/DI | Independent public primary reads while owner holds gate; foreign UoW enlistment/shared multi-DbContext participant | Reads see committed facts after acquisition; no foreign EF/transaction participant/cache/replica; own writes+outbox/audit intent/inbox atomic |
| G03 / guarded write flows | Fault integration | Bounded acquisition cancellation/timeout, failed read, owner connection loss, lost commit ACK | Technical abort/unknown outcome; no fake compatible empty facts or reconnect continuation; original own412 and independent basis409 preserved |

## 12. Порядок реализации

1. Материализовать accepted Q19 shared typed profiles и literal fixtures, затем Q15 input/preview/diagnostics/result/list/lifecycle/headers/recovery; MVP gate §7.2 + Q20 immutable storage/DI/outbox carriers/recovery; Q17 propagation guard и Q09 final-effective closure/broader retained evidence mapping. Closed source/nonempty/terminal/canonical правила не переоткрывать.
2. Domain typed Items/metadata/lifecycle/constraints, pure stable-ID composer with shared F08 core and strict F10 profile; literal invariants/tests.
3. Application.Abstractions requests/queries/results/public ports; Application Create/lifecycle/orchestration; shared full Preview/Create validation, no App bool-switch duplication.
4. EF/Persistence normalized immutable content/own lifecycle token/preserved canonical data/query adapters/scoped migrations/atomic state-history-outbox/source-use guards. Real PostgreSQL validation.
5. Accepted API routes/source semantics/own lifecycle If-Match/OpenAPI; NSwag/shared transport/frontend source picker/preview/create/history/lifecycle after contracts.
6. DF08 selected real async delivery ban/null protocol and recovery checks; F17/F11 readiness distinct from Create. Documentation/source existence не proof приложения.

## 13. Открытые технические вопросы

[Технический Q39](../questions/Q39-schema-content-selection.md) закрывает schema-selection provenance/tree projection и legacy transition. Новая composition использует только frozen F09 refs/mask и SchemaRulesV1 sources.

- [Q15 — revision contracts и preview](../questions/Q15-api-and-source-code.md#f10-revision-contracts-и-preview): DTO/null encoding/invalid envelope/comment/reason/permissions/list/headers/parent/result/readiness/recovery.
- [Q19 — принятый canonical profile v1](../questions/Q19-content-hashes.md#принятый-canonical-profile-v1): bytes/digest/order/profiles и runtime-vs-provenance приняты; implementation/physical mapping/общие literal fixtures ещё материализуются. Старый helper не доказательство совместимости SDK.
- [Q20 — composition capture и withdrawal](../questions/Q20-persistence-and-migration.md#f10-composition-capture-и-withdrawal): gate выбран (§7.2); immutable storage/DI, outbox/consumer contract/recovery, durable exact-ID ban/null delivery propagation.
- [Q17 — Withdrawal и historical distribution](../questions/Q17-publication-availability.md#f10-withdrawal-и-historical-distribution): inactive/frozen restriction delivery/readiness/in-flight availability guarantees; async target already selected.
- [Q09 — зависимости исключённого слоя](../../../business/questions/Q09-editing-and-deletion.md#f10-зависимости-исключённого-слоя): final-effective new-use/runtime closure и broader retention приняты; physical retained evidence/storage mapping остаётся Q20.
- [Q05 — повторное создание revision](../../../business/questions/Q05-initial-publication.md#f10-повторное-создание-revision): intentional duplicate artifacts/empty releases accepted; ≥1 source/finalnonempty mandatory; exact receipt wire/errors Q15.

F17/Q28 governs moderation of exact immutable revision; cancellation of moderation iteration doesn't unwithdraw terminal revision. F11 governs active state; F13 enforces distribution ban. History stored, never recomposed.
