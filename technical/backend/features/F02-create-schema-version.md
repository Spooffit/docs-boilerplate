# F02. Создание и редактирование версии схемы: бэкенд

[Бизнесовая спецификация](../../../business/features/F02-create-schema-version.md) · [Модуль M01](../modules/M01-content-authoring.md) · [Фронтенд](../../frontend/features/F02-create-schema-version.md) · [F01](F01-create-entity-schema.md) · [F03](F03-publish-schema-version.md) · [Q09](../../../business/questions/Q09-editing-and-deletion.md) · [Q15](../questions/Q15-api-and-source-code.md) · [Q20](../questions/Q20-persistence-and-migration.md)

## 1. Область реализации

F02 даёт workspace EntitySchema: list/exact read versions; создание clean Draft и копии; Save; актуализация Published baseline; preview и физическое удаление Draft с допустимыми Draft dependencies. MVP добавляет Retire/Restore Published SchemaVersion и Archive/Restore логической EntitySchema. Contextual hard-delete логической схемы допустим только без Published history и после отдельного удаления logical instance owners по Q09/F04.

Publish/evolution policy — F03, экземпляры — F04, Withdrawal — F13. F02 не публикует автоматически, не переписывает instance values и не редактирует Published content. Demo без auth/moderation/шины; MVP Specialist по F16/Q10. Retire/Archive/Restore не входят в Demo F02.

Исходная точка — уже выполненный F01, а не прежний Editor: EntitySchema и EntitySchemaVersion являются независимыми roots с собственными int Version, поддерживаются multiple ненумерованные Draft и recursive definitions всех 15 типов. F02 переиспользует эти модели и F01 mapping.

| Источник, сверка checkout 07.10.2026 | Назначение |
| --- | --- |
| [Domain roots](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Domain/EntitySchemas/EntitySchema.cs), [version root](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Domain/EntitySchemas/Entities/EntitySchemaVersion.cs), [SchemaDraftApplier](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Domain/EntitySchemas/ValueObjects/SchemaDraftApplier.cs) | F01 independent roots; F02 clean/copy/Save/Accept, whole-tree operations и historical identity/monotonic guards |
| [SchemaDraftRequests](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application.Abstractions/EntitySchemas/Models/SchemaDraftRequests.cs), [SchemaVersionDetailsDto](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application.Abstractions/EntitySchemas/Models/SchemaVersionDetailsDto.cs) | Материализованные request/read contracts §3; публичного technical Version нет |
| [SchemaDraftCommandHandlers](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application/EntitySchemas/CommandHandlers/SchemaDraftCommandHandlers.cs), [SchemaDraftMapper](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application/EntitySchemas/Mappers/SchemaDraftMapper.cs) | Раздельные version/logical metadata use cases, durable create/copy replay и resulting DTO |
| [ISchemaDraftRepository](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Domain/EntitySchemas/Repositories/ISchemaDraftRepository.cs), [SchemaDraftRepository](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.EntityFramework.Persistence/EntitySchemas/Repositories/SchemaDraftRepository.cs), [SchemaWriteGate](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.EntityFramework.Persistence/EntitySchemas/Repositories/SchemaWriteGate.cs) | Owner transaction, exact deletion facts/consent, per-schema Demo gate и shared operation receipt gate |
| [EditorRepository](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.EntityFramework.Persistence/Editor/EditorRepository.cs) | Старые schema/instance writers участвуют в том же schema gate перед authoritative load; legacy DTO/handlers не определяют новый F02 wire |

Demo F02 реализован и проверен в согласованном контуре: Unit 427/427, PostgreSQL Integration 36/36, Functional HTTP/OpenAPI 50/50, UI build/runner/HtmlRenderer и browser Copy→Save→Reopen, clean valid Create, реальный412 с сохранением формы и delete-preview→Cancel. Границы и оставшиеся проверки раскрыты в §11; это не готовность всего Demo, F03/F04/MVP или production deployment.

## 2. Domain

### 2.1. Aggregates, entities и value objects

Полный состав roots и recursive definitions определён в [F01, раздел 2](F01-create-entity-schema.md#2-domain). EntitySchema владеет identity/Name/Key/Description/Lifecycle/current Published pointer и внутренним int Version; EntitySchemaVersion — своим snapshot/metadata и собственным int Version. SchemaField — snapshot node, не EF row. Anonymous List.Item не имеет FieldId.

Обязательность в таблице относится к полю соответствующей модели. Для серверного состояния и result она не означает требование передать поле клиентом. `Условно` раскрывает lifecycle или variant; неизвестный wire omission/default остаётся `Уточняется`.

| Модель F02 | Роль / lifecycle | Владение |
| --- | --- | --- |
| Draft EntitySchemaVersion | Состав [F01 §2](F01-create-entity-schema.md#2-domain); Draft ненумерован, published state отсутствует | Один mutable root; content/metadata вместе |
| Published EntitySchemaVersion | Состав [F01 §2](F01-create-entity-schema.md#2-domain); Published facts immutable, lifecycle mutable | Content/number/hash/published identity immutable |
| SchemaDraftChangeSet | Domain parameter object | Не хранимое состояние/universal DTO |
| SchemaDraftCreate | Create новой ветви | Root либо existing Object, в том числе через ItemDepth |
| SchemaDraftUpdate | Existing field update | Identity обязательна; Key/Type/semantic shape не обновляются |
| SchemaFieldId в Deletes | Subtree deletion | Выбранный Draft snapshot |

**`SchemaDraftChangeSet`**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Creates` | `IReadOnlyList<SchemaDraftCreate>` | Да | Новые ветви | Whole operation-set validation; Внутренняя required коллекция; public [] допустим |
| `Updates` | `IReadOnlyList<SchemaDraftUpdate>` | Да | Existing updates | Repeated/conflicting IDs запрещены |
| `Deletes` | `IReadOnlyList<SchemaFieldId>` | Да | Subtree removals | Delete parent/descendant conflict проверяется до применения |
| `Restores` | `IReadOnlyList<SchemaDraftRestore>` | Да | Historical subtree restore | Exact source/FieldId, original context и monotonic history |

**`SchemaDraftCreate`**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `ParentFieldId` | `SchemaFieldId?` | Нет | Parent новой ветви | Null=root; existing parent |
| `ItemDepth` | `int` | Да | Anonymous List.Item traversal depth | ≥0; target Object; root0 |
| `Field` | `SchemaField` | Да | Новая ветвь | Server IDs; unsaved parent subtree одной ветвью |

**`SchemaDraftUpdate`**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `FieldId` | `SchemaFieldId` | Да | Existing identity | Saved own field; без скрытого перемещения |
| `Name` | `SchemaFieldName` | Да | Изменяемая подпись | VO constraints F01 |
| `IsRequired` | `bool` | Да | Required value policy | Только true→false; Deprecated optional |
| `Deprecated` | `bool` | Да | Отказ от поля | Только false→true |
| `HelpText` | `string?` | Нет | Подсказка | Limits/normalization [Q15](../questions/Q15-api-and-source-code.md) |
| `Definition` | `SchemaDefinitionChanges?` | Нет | Option/List/anonymous Item settings | Null=unchanged, identity/context immutable; MVP typed constraints отдельно |

**Удаление subtree по SchemaFieldId**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `FieldId` | `SchemaFieldId` | Да | Existing subtree identity | Whole-set validation; resulting minimum одно поле |

Type-specific update использует определения `SchemaOption` и `ListValueDefinition` из [F01 §2](F01-create-entity-schema.md#2-domain). Numeric options, InheritanceStrategy, ColorFormat, SortOrder отсутствуют. Все 15 типов/Object/List<List<T>> — F01/Q34. Новый subtree под unsaved parent передаётся одной create-веткой; временный UI ID не становится server identity. Demo адресует anonymous List.Item через ParentFieldId+ItemDepth и recursive Definition.Item (§3.3), без fake ItemId.

MVP constraints/comparer находятся в definitions F01. Их изменение не меняет immutable type/meaning/container identity: bounds можно усиливать/ослаблять в пределах intrinsic типа. Новый snapshot валидирует settings целиком, включая comparer paths; изменение required/deprecated по-прежнему monotonic. Copy сохраняет source constraints/comparer exact, не берёт current defaults и не меняет source. Instance values автоматически не переписываются, новый target constraint alone не превращает совместимое value в orphan F04.

DateTime temporal evolution Q34: DateTimeValueDefinition.TemporalSemantics immutable. Existing Naive не меняется на Offset через metadata/settings update; новый смысл оформляется новым server FieldId, new MVP factory выбирает Offset. Copy exact source retains Naive/Offset declaration и bound offsets/precision; current source profile не reinterpretируется. Manual instance recovery/migration — F04, F02 не назначает timezone.

**Другие domain parameters/facts** — [SchemaDraftChangeSet.cs](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Domain/EntitySchemas/ValueObjects/SchemaDraftChangeSet.cs) и [SchemaDeletionImpact.cs](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Domain/EntitySchemas/SchemaDeletionImpact.cs):

| Модель | Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- | --- |
| SchemaDefinitionChanges | Options | IReadOnlyList<SchemaOption>? | Нет | Desired options | Null=unchanged; Published keys retained |
| SchemaDefinitionChanges | OrderMode | ListOrderMode? | Нет | List setting | Known enum; null=unchanged |
| SchemaDefinitionChanges | Item | SchemaDefinitionChanges? | Нет | Recursive anonymous definition update | List-only; shape immutable |
| SchemaDraftRestore | SourceVersionId | EntitySchemaVersionId | Да | Trusted historical snapshot | Same schema |
| SchemaDraftRestore | FieldId | SchemaFieldId | Да | Original subtree identity | Same parent chain/meaning; historical monotonic flags |
| SchemaHistorySnapshot | Id | EntitySchemaVersionId | Да | Historical version identity | Server-authoritative |
| SchemaHistorySnapshot | IsPublished | bool | Да | Snapshot ever published | Includes published metadata/history states |
| SchemaHistorySnapshot | Content | SchemaVersionContent | Да | Historical definition tree | Used for admission/evolution |
| SchemaDeletionImpact | CanDelete | bool | Да | Full current eligibility | Read fact |
| SchemaDeletionImpact | SchemaDraftCount | int | Да | Scoped schema Draft count | Full set |
| SchemaDeletionImpact | DependentInstanceDraftCount | int | Да | Scoped dependent Draft count | Full set |
| SchemaDeletionImpact | Blockers | IReadOnlyList<SchemaDeletionBlocker> | Да | Server blockers | Empty when allowed |
| SchemaDeletionImpact | ConsentToken | string | Да | Opaque basis digest | Rechecked under gate |
| SchemaDeletionImpact | Dependencies | IReadOnlyList<SchemaDraftDependency> | Да | Exact dependent Draft composition | [] default |
| SchemaDeletionImpact | SchemaDraftIds | IReadOnlyList<Guid> | Да | Scoped schema Draft IDs | [] default |
| SchemaDeletionBlocker | Code | string | Да | Blocker category | Server-defined |
| SchemaDeletionBlocker | Message | string | Да | Human message | No payload/secrets |
| SchemaDraftDependency | InstanceId | Guid | Да | Preserved owner | Server-read |
| SchemaDraftDependency | InstanceVersionId | Guid | Да | Dependent version | Draft-only composition |
| SchemaDraftDependency | SchemaVersionId | Guid | Да | Referenced version | Exact scope |
| SchemaDraftDependency | InstanceName | string | Да | Owner label | Server-read |
### 2.2. Методы и переходы

| Тип | Метод | Параметры | Предусловия | Изменение / результат |
| --- | --- | --- | --- | --- |
| EntitySchemaVersion | CreateCleanDraft | new Id, SchemaId, Title/comment?, content, current Published ID?, server CreatedAt | Active parent; metadata/content по clean profile Q09 | Новый Draft, baseline=current Published, number/published state null |
| EntitySchemaVersion | CreateCopy | new Id, exact source snapshot, Title/comment?, server CreatedAt | Source того же owner; lifecycle policy Q09; согласованный snapshot | Content с теми же FieldId; source Draft baseline наследуется, Published baseline=source.Id; source не меняется |
| EntitySchemaVersion | UpdateDraft | string title, string? changeComment, SchemaVersionContent resultingContent | Draft; applier уже проверил operations/history/resulting tree | Atomic metadata/content replacement, один increment Version |
| EntitySchemaVersion | AcceptCurrentBase | EntitySchemaVersionId? baseline | Draft; handler проверил current identity/availability и historical admissibility | Только baseline/Version, без merge; Content/Title/comment/CreatedAt прежние |
| EntitySchemaVersion (MVP target) | Retire | server now | Published и допустимый lifecycle | RetiredAt; immutable snapshot/number/hash прежние |
| EntitySchemaVersion (MVP target) | RestoreFromRetired | — | Retired Published, не Withdrawn | Снять RetiredAt; без нового number/Publish |
| EntitySchema (MVP target) | Archive / Restore | audit time при необходимости | Допустимый lifecycle Q09 | Только parent lifecycle/Version, не массовое изменение child lifecycle |
| EntitySchema | UpdateMetadata | EntitySchemaName name, EntitySchemaDescription? description | Active schema/If-Match проверены handler | Atomic Name/Description, один own Version increment |
| EntitySchema | SetLegacyLatestVersionAfterDeletion | deletedId, remainingId? | Explicit allowed deletion plan | Только deprecated compatibility pointer; own Version не меняется |

Repeated lifecycle actions/no-op Save — общий mutation profile Q15; Create/Copy имеют явно принятый operation-ID/durable-result replay; остальные произвольные POST не объявляются идемпотентными.

### 2.3. Инварианты и constraints

Единый источник постоянных ограничений — EntitySchemaVersionConstraints и field/option constraints рядом с VO по [S21](../../../catalog/docs/standards/architecture/ddd.md#aggregate-owned-rule-definitions). Limits/normalization — Q15, без новых literals в FV/EF.

- Save только Draft и Active schema; несколько Draft допустимы; business number до Publish отсутствует.
- Existing FieldId/Key/Type/semantic List.Item identity и identity всей parent chain неизменны. Возврат ранее удалённого FieldId допустим только с теми же Key/Type/meaning/context и monotonic историческими flags; перенос к другому parent создаёт новый server FieldId. Name/HelpText изменяемы; Required только true→false, Deprecated только false→true. Deprecated+Required запрещено. Inverse transition запрещён уже при Save, независимо от baseline.
- Create генерирует server IDs; IDs/keys уникальны whole-tree. Object/Enum/FlagEnum непусты, List.Item рекурсивно валиден; неизвестные variants/OrderMode запрещены.
- Один ID не участвует в конфликтующих операциях. Foreign ID/parent недопустимы. Delete parent и изменение descendants проверяются до применения, без зависимости от порядка массива. Existing field не перемещается скрытой сменой parent.
- Option Key immutable, Name mutable, Deprecated monotonic. Draft-only option key разрешено удалять; key, встречавшийся в любом Published snapshot, сохраняется и при необходимости deprecates. Проверка действует уже при Save и recursive Restore; minimum option set остаётся непустым.
- Save защищает persisted identity/structure. Дополнительные cross-Published evolution blockers F03 могут быть read-side PublishIssues, но не отменяют Save guards.
- IsStale = Draft.BasePublishedVersionId != schema.LatestPublishedVersionId. Stale можно Save; Publish блокируется F03. Accept проверяет current Published, merge отсутствует.
- Published history/content не hard-delete. Retired доступна exact historical Registry/codegen, запрещена для new instance authoring/publish; existing Draft instances сохраняются. Withdrawn source можно читать и Copy; полученный Draft можно Save, но Withdrawn context блокирует Publish/runtime до явного возврата в выдачу.
- Archive parent закрывает authoring/new instances/new publication, сохраняет children/history/Delivery. Restore не Activate/Publish и не снимает собственный Archived child.
- Draft cascade включает только допустимые Draft instance versions. Logical hard-delete только без Published schema history; Draft-version cascade не удаляет logical instance owners, включая ставших пустыми. Logical-instance Delete — отдельное явное действие с собственной eligibility/consent; Last Draft deletion не auto Delete parent.

Минимум сохранённого дерева — одно поле. Clean Draft сначала локальный несохранённый editor; server Create выполняется только при первом valid Save полного минимального дерева. Пустой persisted Schema Draft не создаётся; до Create у него нет server version ID/ETag.

### 2.4. Domain events

Отдельные события F02 не требуются. Результат — local commit; логирование не создаёт domain event/outbox/broker. Publication/Delivery effects принадлежат соседним фичам.

Reintroduced known FieldId проходит отдельную target domain identity-admission проверку всей истории: same Key/Type/meaning/parent chain и monotonic flags. Новая ветвь получает server ID. Исторический ID не признаётся foreign лишь по отсутствию в current snapshot; unknown ID или изменённый context отклоняется. Demo Restore задаётся SchemaDraftRestoreRequest(SourceVersionId, FieldId) внутри existing Save; unknown IDs запрещены, новый endpoint не вводится.

## 3. Application.Abstractions

### 3.1. Commands

Фактические declarations — [SchemaDraftCommands.cs](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application.Abstractions/EntitySchemas/Commands/SchemaDraftCommands.cs). ExpectedVersion:int — internal parameter из If-Match, не body/query property. VersionedResource<T> переносит resource и own token внутри CQRS; Api возвращает только Resource и strong numeric ETag после outer commit.

| Command | Result | Own precondition |
| --- | --- | --- |
| CreateCleanSchemaDraftCommand | VersionedResource<SchemaVersionDetailsDto> | Новый Draft без If-Match |
| CopySchemaDraftCommand | VersionedResource<SchemaVersionDetailsDto> | Source Draft или Published If-Match |
| SaveSchemaDraftCommand | VersionedResource<SchemaVersionDetailsDto> | Exact Draft If-Match |
| AcceptSchemaDraftBaseCommand | VersionedResource<SchemaVersionDetailsDto> | Exact Draft If-Match + показанная Published identity |
| DeleteSchemaDraftCommand | Guid | Exact Draft If-Match + consent |
| DeleteEntitySchemaCommand | Guid | Logical schema If-Match + consent |
| UpdateSchemaMetadataCommand | VersionedResource<EntitySchemaDetailsDto> | Logical schema If-Match |

Для каждого command отдельные fields:

| Command | Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- | --- |
| CreateCleanSchemaDraftCommand | SchemaId | Guid | Да | Route owner | Existing Active schema |
| CreateCleanSchemaDraftCommand | Request | CreateCleanSchemaDraftRequest | Да | Первый valid Save local clean editor | §3.3 |
| CopySchemaDraftCommand | SchemaId | Guid | Да | Route owner | Active schema |
| CopySchemaDraftCommand | SourceVersionId | Guid | Да | Exact source route | Same owner |
| CopySchemaDraftCommand | ExpectedVersion | int | Да | Source header token | >0; включая Published |
| CopySchemaDraftCommand | Request | CopySchemaDraftRequest | Да | Copy metadata/operation ID | §3.3 |
| SaveSchemaDraftCommand | SchemaId | Guid | Да | Route owner | Active schema |
| SaveSchemaDraftCommand | VersionId | Guid | Да | Exact target | Same owner, Draft |
| SaveSchemaDraftCommand | ExpectedVersion | int | Да | Own Draft header token | >0 |
| SaveSchemaDraftCommand | Request | UpdateSchemaDraftRequest | Да | Atomic change set | §3.3 |
| AcceptSchemaDraftBaseCommand | SchemaId | Guid | Да | Route owner | Active schema |
| AcceptSchemaDraftBaseCommand | VersionId | Guid | Да | Exact Draft | Same owner |
| AcceptSchemaDraftBaseCommand | ExpectedVersion | int | Да | Own Draft header token | >0 |
| AcceptSchemaDraftBaseCommand | Request | AcceptSchemaDraftBaseRequest | Да | Reviewed basis | §3.3 |
| DeleteSchemaDraftCommand | SchemaId | Guid | Да | Route owner | Active schema |
| DeleteSchemaDraftCommand | VersionId | Guid | Да | Exact Draft | Same owner |
| DeleteSchemaDraftCommand | ExpectedVersion | int | Да | Own Draft header token | >0 |
| DeleteSchemaDraftCommand | Request | SchemaDeleteRequest | Да | Explicit consent | §3.3 |
| DeleteEntitySchemaCommand | SchemaId | Guid | Да | Logical owner | Active, never-published, no instance owners |
| DeleteEntitySchemaCommand | ExpectedVersion | int | Да | Logical header token | >0 |
| DeleteEntitySchemaCommand | Request | SchemaDeleteRequest | Да | Explicit consent | §3.3 |
| UpdateSchemaMetadataCommand | SchemaId | Guid | Да | Logical owner | Active schema |
| UpdateSchemaMetadataCommand | ExpectedVersion | int | Да | Logical header token | >0 |
| UpdateSchemaMetadataCommand | Request | UpdateSchemaMetadataRequest | Да | Name/Description replacement | §3.3 |

RetireSchemaVersionCommand, RestoreSchemaVersionCommand, ArchiveEntitySchemaCommand и RestoreEntitySchemaCommand остаются проектными MVP use cases; эти declarations/endpoints не считаются частью materialized Demo F02.

### 3.2. Queries

Declarations — [SchemaDraftQueries.cs](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application.Abstractions/EntitySchemas/Queries/SchemaDraftQueries.cs).

| Query | Result |
| --- | --- |
| ListSchemaVersionsQuery | OffsetPagedResultDto<EntitySchemaVersionSummaryDto> |
| GetSchemaVersionDetailsQuery | VersionedResource<SchemaVersionDetailsDto> |
| GetSchemaDeleteImpactQuery | SchemaDeleteImpactDto |

| Query | Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- | --- |
| ListSchemaVersionsQuery | SchemaId | Guid | Да | Owner | Missing owner →404 |
| ListSchemaVersionsQuery | Request | SchemaVersionListRequest | Да | Server paging/filter/sort | §3.3 |
| GetSchemaVersionDetailsQuery | SchemaId | Guid | Да | Owner | No fallback |
| GetSchemaVersionDetailsQuery | VersionId | Guid | Да | Exact version | Same owner; missing/foreign →404 |
| GetSchemaDeleteImpactQuery | SchemaId | Guid | Да | Owner | Missing →404 |
| GetSchemaDeleteImpactQuery | VersionId | Guid? | Нет | Exact version либо logical scope | Null = entire logical schema; impact unpaged |

По [S20](../../../catalog/docs/standards/architecture/cqrs.md#handler-responsibilities-and-dependency-direction) query handlers находятся в EntityFramework.Persistence. Logical details читаются F01 GetSchemaDetailsQuery. Summary list не заменяет exact read и не содержит per-row technical ETag.

### 3.3. DTO и parameter models

Request declarations — [SchemaDraftRequests.cs](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application.Abstractions/EntitySchemas/Models/SchemaDraftRequests.cs). Они запрещают unmapped JSON members. Все обязательные operation collections должны присутствовать; пустые arrays допустимы. Field create definitions и CreateSchemaOptionDto принадлежат [F01](F01-create-entity-schema.md#33-dto-и-parameter-models).

| Модель | Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- | --- |
| CreateCleanSchemaDraftRequest | OperationId | Guid | Да | Client-generated create identity | Nonempty; durable replay |
| CreateCleanSchemaDraftRequest | Title | string | Да | Version title | Trim, 1–255 UTF-16 code units |
| CreateCleanSchemaDraftRequest | ChangeComment | string? | Нет | Version comment | Optional blank→null, ≤1024 |
| CreateCleanSchemaDraftRequest | Fields | List<CreateSchemaFieldRequest> | Да | Initial recursive tree без IDs | Minimum одно поле; server IDs |
| CopySchemaDraftRequest | OperationId | Guid | Да | Client-generated copy identity | Nonempty; replay |
| CopySchemaDraftRequest | Title | string | Да | New Draft title | Trim, 1–255 |
| CopySchemaDraftRequest | ChangeComment | string? | Нет | New comment | Optional blank→null, ≤1024 |
| UpdateSchemaDraftRequest | Title | string | Да | Resulting version title | Не logical Name |
| UpdateSchemaDraftRequest | ChangeComment | string? | Нет | Resulting comment | Не logical Description |
| UpdateSchemaDraftRequest | Creates | List<SchemaDraftCreateRequest> | Да | New branches | [] допустим |
| UpdateSchemaDraftRequest | Updates | List<SchemaDraftUpdateRequest> | Да | Existing field changes | [] допустим |
| UpdateSchemaDraftRequest | Deletes | List<Guid> | Да | Existing subtree IDs | [] допустим; whole-set conflict checks |
| UpdateSchemaDraftRequest | Restores | List<SchemaDraftRestoreRequest> | Да | Historical subtree recovery | [] допустим; trusted source history |
| SchemaDraftCreateRequest | ParentFieldId | Guid? | Нет | Existing named parent | Null=root |
| SchemaDraftCreateRequest | ItemDepth | int | Нет | Число anonymous List.Item переходов от parent | Default0; ≥0; root только0; resulting target Object |
| SchemaDraftCreateRequest | Field | CreateSchemaFieldRequest | Да | Entire new subtree | No client FieldId; F01 recursive union |
| SchemaDraftUpdateRequest | FieldId | Guid | Да | Persisted named identity | Saved own field |
| SchemaDraftUpdateRequest | Name | string | Да | Mutable label | F01 text constraints |
| SchemaDraftUpdateRequest | IsRequired | bool | Да | Desired flag | True→false only |
| SchemaDraftUpdateRequest | Deprecated | bool | Да | Desired flag | False→true only; deprecated optional |
| SchemaDraftUpdateRequest | HelpText | string? | Нет | Desired help | Optional blank→null, ≤1024 |
| SchemaDraftUpdateRequest | Definition | SchemaDefinitionChangesDto? | Нет | Editable settings | Null=unchanged; immutable shape/context |
| SchemaDefinitionChangesDto | Options | List<CreateSchemaOptionDto>? | Нет | Desired option set | Null=unchanged; [] invalid; keys unique |
| SchemaDefinitionChangesDto | OrderMode | ListOrderMode? | Нет | List setting | Null=unchanged; known enum |
| SchemaDefinitionChangesDto | Item | SchemaDefinitionChangesDto? | Нет | Recursive anonymous item settings | No fake ItemId; only List |
| SchemaDraftRestoreRequest | SourceVersionId | Guid | Да | Historical exact snapshot | Same schema, submitted FieldId exists there |
| SchemaDraftRestoreRequest | FieldId | Guid | Да | Restored subtree root | Same identity/parent chain and monotonic flags |
| AcceptSchemaDraftBaseRequest | PublishedVersionId | Guid? | Да | Reviewed current Published identity | Null explicitly acknowledges no Published baseline; no technical Version |
| SchemaDeleteRequest | ConsentToken | string | Да | Opaque server impact digest | Exact comparison under schema gate |
| SchemaDeleteRequest | Cascade | bool | Да | Explicit dependent Draft deletion consent | True required when dependent Draft count>0 |
| UpdateSchemaMetadataRequest | Name | string | Да | Logical schema name | F01 constraints; Key unchanged |
| UpdateSchemaMetadataRequest | Description | string? | Нет | Logical description replacement | F01 optional normalization |

Options: removal разрешён только для key, который никогда не встречался в Published history. Любой Published option key сохраняется; вместо удаления используется Deprecated=true. Key immutable, Name mutable, Deprecated monotonic. Правило применяется рекурсивно внутри List.Item и на Save/Restore, а не только Publish. Текущий Draft-only key можно удалить при сохранении непустого valid option set.

Read declarations — [SchemaVersionDetailsDto.cs](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application.Abstractions/EntitySchemas/Models/SchemaVersionDetailsDto.cs).

| Модель | Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- | --- |
| SchemaVersionDetailsDto | Id | Guid | Да | Version identity | Read-only |
| SchemaVersionDetailsDto | SchemaId | Guid | Да | Owner | Read-only |
| SchemaVersionDetailsDto | Status | PublicationStatus | Да | Publication state | Read-only enum |
| SchemaVersionDetailsDto | VersionNumber | int? | Условно | Business number | Draft null; Published immutable |
| SchemaVersionDetailsDto | Title | string | Да | Version metadata | Read-only result |
| SchemaVersionDetailsDto | ChangeComment | string? | Нет | Version comment | Nullable |
| SchemaVersionDetailsDto | BasePublishedVersionId | Guid? | Нет | Accepted baseline | Identity, не technical token |
| SchemaVersionDetailsDto | CurrentPublishedVersionId | Guid? | Нет | Current owner Published | Derived read basis |
| SchemaVersionDetailsDto | IsStale | bool | Да | Draft baseline differs | Derived |
| SchemaVersionDetailsDto | IsRetired | bool | Да | Retired timestamp exists | MVP lifecycle fact |
| SchemaVersionDetailsDto | IsWithdrawn | bool | Да | Withdrawn timestamp exists | Read fact |
| SchemaVersionDetailsDto | IsLegacyDefinition | bool | Да | Requires explicit legacy conversion | Does not authorize lossy update |
| SchemaVersionDetailsDto | Content | SchemaContentSnapshotDto? | Условно | Full recursive tree | Modern snapshot; F01 definition owner |
| SchemaVersionDetailsDto | LegacyFields | IReadOnlyList<SchemaFieldDto>? | Условно | Historical legacy snapshot | Read-only compatibility; null for modern definitions; not editable conversion |
| SchemaVersionDetailsDto | CreatedAt | DateTimeOffset | Да | Server creation time | Preserved on Save/Accept |
| SchemaVersionDetailsDto | PublishedAt | DateTimeOffset? | Условно | Publication time | Draft null |
| SchemaVersionDetailsDto | RetiredAt | DateTimeOffset? | Нет | MVP retirement fact | No Demo mutation |
| SchemaVersionDetailsDto | WithdrawnAt | DateTimeOffset? | Нет | Distribution fact | F13 |
| SchemaVersionDetailsDto | PublishedSchemaName | string? | Условно | Captured logical name | Immutable Published snapshot |
| SchemaVersionDetailsDto | PublishedSchemaKey | string? | Условно | Captured logical key | Immutable Published snapshot |
| SchemaVersionDetailsDto | ContentHash | string? | Условно | Published hash | Draft null |
| SchemaVersionDetailsDto | PublishIssues | IReadOnlyList<SchemaIssueDto> | Да | Read diagnostics | [] default; own ETag not diagnostics token |
| SchemaVersionDetailsDto | BaseChanges | IReadOnlyList<SchemaBaseChangeDto> | Да | Baseline→current semantic diff | [] default; no merge payload |
| SchemaIssueDto | Code | string | Да | Stable machine code | Read-only |
| SchemaIssueDto | Message | string | Да | Human message | Read-only |
| SchemaIssueDto | Address | string | Да | Error/summary address | Read-only |
| SchemaBaseChangeDto | FieldId | Guid | Да | Stable changed field | Read-only |
| SchemaBaseChangeDto | Kind | string | Да | Added/Removed/Changed | Read-only |
| SchemaBaseChangeDto | BeforeName | string? | Нет | Previous label | Null for added |
| SchemaBaseChangeDto | AfterName | string? | Нет | Current label | Null for removed |
| SchemaDeleteImpactDto | CanDelete | bool | Да | Current eligibility | Observation, not permission |
| SchemaDeleteImpactDto | SchemaDraftCount | int | Да | Full scoped Draft count | No paging |
| SchemaDeleteImpactDto | DependentInstanceDraftCount | int | Да | Full scoped dependency Draft count | No paging |
| SchemaDeleteImpactDto | Blockers | IReadOnlyList<SchemaIssueDto> | Да | Current blockers | Exact server facts |
| SchemaDeleteImpactDto | ConsentToken | string | Да | Opaque digest | Not authentication credential |
| SchemaDeleteImpactDto | DependentDrafts | IReadOnlyList<SchemaDependencySummaryDto> | Да | Full composition | [] default; no client-selected removal IDs |
| SchemaDeleteImpactDto | SchemaDraftIds | IReadOnlyList<Guid> | Да | Exact scoped schema Draft IDs | [] default |
| SchemaDependencySummaryDto | InstanceId | Guid | Да | Preserved logical owner | Read-only |
| SchemaDependencySummaryDto | InstanceVersionId | Guid | Да | Dependent Draft version | Read-only |
| SchemaDependencySummaryDto | SchemaVersionId | Guid | Да | Referenced schema version | Read-only |
| SchemaDependencySummaryDto | InstanceName | string | Да | Logical owner label | Read-only |
| SchemaVersionListRequest | Status | PublicationStatus? | Нет | Publication filter | Known enum |
| SchemaVersionListRequest | SkipCount | int | Нет | Offset | Default0; ≥0 |
| SchemaVersionListRequest | MaxResultCount | int | Нет | Page size | Default20; 1–200 |
| SchemaVersionListRequest | Sorting | string? | Нет | Allow-listed sort | CreatedAt ASC/DESC, VersionNumber ASC/DESC; case-insensitive |

DTO не содержит technical Version/Revision. Публичный default Published pointer принадлежит logical EntitySchemaDetailsDto F01. Nullable LatestVersionId — deprecated internal physical compatibility pointer; после удаления последнего Draft null и не становится новым публичным member.

MVP typed constraints/comparer read models принадлежат F01/Q35: typed nullable bounds, recursive Item и ordered server FieldIds для comparer path должны сохраняться в полном read snapshot без coercion. DateTime TemporalSemantics immutable по Q34; new-field comparer reference transport остаётся отдельной MVP детализацией Q15. Это не расширяет Demo request settings.

### 3.4. Ports

Demo port принадлежит Domain: [ISchemaDraftRepository](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Domain/EntitySchemas/Repositories/ISchemaDraftRepository.cs). IQueryable, DbContext, SQL и transaction types наружу не выходят. Каждый async метод получает CancellationToken.

| Метод | Вход | Result / ответственность |
| --- | --- | --- |
| LockOperationAsync | Guid operationId | Task; F01-shared transaction operation lock |
| LockSchemaAsync | Guid schemaId | Task; owner schema transaction lock, refresh unchanged tracked roots |
| GetSchemaAsync | Guid id | Task<EntitySchema?>; exact tracked root, без Include(Versions) |
| GetVersionAsync | Guid schemaId, Guid versionId | Task<EntitySchemaVersion?>; tracked exact owner/version |
| GetHistoryAsync | Guid schemaId | Task<IReadOnlyList<SchemaHistorySnapshot>>; authoritative source IDs/published facts/content |
| GetReceiptAsync | Guid operationId | Task<SchemaCreationReceipt?>; original durable result |
| Add | EntitySchemaVersion version, SchemaCreationReceipt receipt | Tracking обеих rows; no commit |
| FlushAsync | CancellationToken ct | Task; SaveChanges внутри owner transaction, no commit; stale EF write → shared concurrency exception |
| GetDeleteImpactAsync | Guid schemaId, Guid? versionId | Task<SchemaDeletionImpact>; exact observations/consent facts |
| RemoveConfirmedAsync | EntitySchema schema, EntitySchemaVersion? version | Task; current server plan under held gate; explicit removal, no commit |

Domain facts/parameters определены в §2.1, public mapping — §3.3. Client IDs не разрешают deletion.

IUnitOfWorkManager/command pipeline владеет local commit/rollback. TimeProvider задаёт server CreatedAt. F02 не добавляет внешний BC/reference service, field repository или production dependency. Future global MVP coordination/public foreign-read ports остаются Q20; Demo per-schema port не объявляется их реализацией.

### 3.5. Specifications / read contracts

По [S19](../../../catalog/docs/standards/dotnet/ardalis-specification.md) отбор owner/exact ID/status должен оставаться серверным. Не вводятся fictional specification classes/And API. Count использует те же filters без paging; summary не содержит полное Content всех versions.

Default rail: Draft first, Draft CreatedAt descending; Published VersionNumber descending; stable Id tie-breaker. Explicit allow-listed sort применяется стабильно с identity tie-breaker. Default selection по owner DefaultVersionId F01 — newest Draft, иначе latest Published, empty history — empty workspace. Exact отсутствующий/foreign version и missing list owner →404, без fallback. Major/minor filters в новом F02 wire отсутствуют.

Current Published/stale/issues — read observations; own ETag относится только к persisted version root. Accept дополнительно подтверждает показанную Published identity; Delete подтверждает impact digest. Counts/composition не зависят от UI page.

## 4. Application

### 4.1. Handlers / use cases

Материализованный SchemaDraftCommandHandlers в Application обрабатывает Demo DF03–DF06/DF08/DF14/DF15; lifecycle DF09–DF12 остаётся MVP. Подробные algorithms §9: validated input → roots/facts → Domain action → tracking → один local commit → result metadata → Api. Неуспех любой проверки/conditional write откатывает scope. Add/Remove не коммитят сами.

Save version не переименовывает schema. Name/Description принадлежат UpdateSchemaMetadataCommand/PUT S с logical ETag (§3/§5). Handler FlushAsync сохраняет owner context внутри transaction, затем in-process GetSchemaDetailsQuery возвращает complete DTO; это не дополнительный HTTP GET и не ранний commit. Content/Title/comment/Version сохраняются вместе; response содержит confirmed resulting IDs, без скрытого HTTP GET.

### 4.2. Validators

По [S18](../../../catalog/docs/standards/dotnet/fluentvalidation.md) [SchemaDraftCommandValidators](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application/EntitySchemas/Validators/SchemaDraftCommandValidators.cs) содержат отдельный validator каждого Demo command/query; shared DraftValidation и правила CreateSchemaCommandValidator переиспользуют F01 metadata/recursive definitions. Atomic Title/ChangeComment/FieldName/HelpText/OptionKey/Name/known OrderMode используют Domain constraints. Validator не меняет DTO, не генерирует IDs/не скрыто нормализует Key.

Create: recursive shape/null/collection checks, затем whole-tree uniqueness. Update: required IDs/repeated/conflicting operations/variant shape; после загрузки domain applier сравнивает saved identity/monotonic state. Foreign ID/parent/deleted ancestor/resulting minimum проверяются до replacement. Async facts читает handler/port; FV не заменяет transaction guard. Published evolution issues отдельно от Save errors.

MVP Save повторно вызывает DefinitionConstraintsPolicy/typed DTO validators F01 на полном resulting tree. Некорректные bounds/type/comparer path отклоняют Save, не возвращаются как допустимый Schema Draft. Field удалён/сделан optional на required comparer path — invalid settings, нужно явное изменение comparer в той же atomic operation-set. UI preview не заменяет server traversal; instance completeness здесь не проверяется.

MVP temporal validation сравнивает TemporalSemantics с whole historical identity того же FieldId. Attempt Naive↔Offset на существующем ID отклоняет whole Save/Publish; create branch получает новый ID. Bounds обновляются в selected semantics без DateTime/DateTimeOffset implicit conversion. New branch temporal codec/guards F01/F04; known semantic wire union Q15 материализуется отдельно.

### 4.3. Mappers

После Demo Copy переносит exact delivery policy recursive `AssetReference` definitions из source; Save валидирует resulting tree по F01 и atomically сохраняет metadata режима вместе с остальным Draft. Published policy readonly: изменение требует новой Draft/Published SchemaVersion. Create новых branches сохраняет server-generated IDs; существующие branches используют свои IDs. Менять только DeliveryMode при том же FieldId предлагается как совместимое изменение, но до решения [Q37](../../frontend/questions/Q37-launcher-asset-delivery.md)/Q34 это не согласованный exception к semantic identity. Ни Mode, ни Type не угадываются из AssetId/расширения файла; отсутствующий update setting не получает неоговорённое reset-to-default.

Request→domain parameters генерирует IDs create nodes ровно раз через recursive mapper F01; existing IDs проверяет. Options/anonymous Item без Guid. Явный variant mapping без fallback неизвестного enum; metadata не меняют existing Key/Type.

Source→copy сохраняет content identities, выбирает baseline, создаёт owner/time/Title/comment; не переносит number/hash/Published lifecycle в Draft. Domain→exact DTO возвращает full resulting tree/IDs; read→summary без full tree. Version передаётся internal metadata и становится header, не DTO.

MVP mapping: constraints без numeric coercion/rounding; comparer reference resolution после assignment IDs create branches, затем полная path/type/required validation resulting snapshot. Invalid late reference rollback всего Save. Domain→read сохраняет typed settings/FieldPath; source copy переносит их без reinterpretation/defaults.

### 4.4. Services / algorithms

SchemaDraftApplier — pure domain algorithm: индекс original tree → whole operation-set validation → resulting tree в памяти → полная validation → replacement. Ошибка последней операции не оставляет частичных изменений. Semantic diff и identity/evolution validator используются для Accept; strict F03 validator — для PublishIssues/Publish, без strict instance-value completeness gate на Accept.

Delete planner authoritative только внутри mutation transaction. Guards §7 должны участвовать также в F03/F04. DI по слоям: commands/validators/mappers Application; queries/repositories/guard Persistence; context/configs EntityFramework; controllers/header mapping Api; App composition. [ContentModelingEntityFrameworkModule](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.EntityFramework/ContentModelingEntityFrameworkModule.cs) регистрирует owner context; composition регистрирует ISchemaDraftRepository→SchemaDraftRepository и existing UoW pipeline. SQL gate реализован Persistence и не отдельным external service; HTTP/DI integration подтверждён Functional suite; standalone Integration TestModule отдельно регистрирует query port до AppModule composition. Universal Editor/external adapter не вводится.

MVP SchemaDraftApplier различает identity definition и editable value-rule settings; bounds/collection/comparer changes не создают IDs. Full resulting definitions проходят guards F01 перед replacement. DI переиспользует stateless DefinitionConstraintsPolicy и atomic FV rules F01; отдельного repository/mutable rules engine нет.

## 5. Api

### 5.1. Endpoints

Таблица по [S17](../standards/S17-data-flows.md). `V=/api/v1/entity-schemas/{schemaId:guid}/versions`, `S=/api/v1/entity-schemas/{schemaId:guid}`. Demo anonymous, MVP Specialist F16/Q10. Таблица задаёт materialized Demo transport; проверка controller/HTTP/OpenAPI подтверждена scoped Functional suite (§11). Retire/Restore и logical Archive/Restore отдельно остаются MVP, Publish — F03.

| Поток | Транспорт | Метод / канал | Endpoint / destination | Доступ | Вход | Application action | Выход | Успех | Ошибки |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| DF01. Versions | HTTP | GET | V | Demo | SchemaVersionListRequest | ListSchemaVersionsQuery | ApiResponse<OffsetPagedResultDto<EntitySchemaVersionSummaryDto>> | 200 | 400/404 |
| DF02. Exact | HTTP | GET | V/{versionId} | Demo | Owner/version IDs | GetSchemaVersionDetailsQuery | ApiResponse<SchemaVersionDetailsDto> + ETag | 200 | 404 |
| DF03. Clean | HTTP | POST | V | Demo | CreateCleanSchemaDraftRequest | CreateCleanSchemaDraftCommand | ApiResponse<SchemaVersionDetailsDto> + new ETag | 200 | 400/404/409 |
| DF04. Copy | HTTP | POST | V/{sourceVersionId}/copies | Demo | CopySchemaDraftRequest, source If-Match | CopySchemaDraftCommand | ApiResponse<SchemaVersionDetailsDto> + new Draft ETag | 200 | 400/404/409/412/428 |
| DF05. Save | HTTP | PUT | V/{versionId} | Demo | UpdateSchemaDraftRequest, own If-Match | SaveSchemaDraftCommand | ApiResponse<SchemaVersionDetailsDto> + ETag | 200 | 400/404/409/412/428 |
| DF06. Accept base | HTTP | POST | V/{versionId}/accept-current-base | Demo | AcceptSchemaDraftBaseRequest, own If-Match | AcceptSchemaDraftBaseCommand | ApiResponse<SchemaVersionDetailsDto> + ETag | 200 | 400/404/409/412/428 |
| DF07. Draft impact | HTTP | GET | V/{versionId}/delete-impact | Demo | Exact IDs | GetSchemaDeleteImpactQuery(versionId) | ApiResponse<SchemaDeleteImpactDto> | 200 | 404 |
| DF08. Delete Draft | HTTP | DELETE | V/{versionId} | Demo | SchemaDeleteRequest body, own If-Match | DeleteSchemaDraftCommand | ApiResponse<Guid> | 200 | 400/404/409/412/428 |
| DF13. Logical impact | HTTP | GET | S/delete-impact | Demo | SchemaId | GetSchemaDeleteImpactQuery(null) | ApiResponse<SchemaDeleteImpactDto> | 200 | 404 |
| DF14. Delete schema | HTTP | DELETE | S | Demo | SchemaDeleteRequest body, logical If-Match | DeleteEntitySchemaCommand | ApiResponse<Guid> | 200 | 400/404/409/412/428 |
| DF15. Logical metadata | HTTP | PUT | S | Demo | UpdateSchemaMetadataRequest, logical If-Match | UpdateSchemaMetadataCommand | ApiResponse<EntitySchemaDetailsDto> + schema ETag | 200 | 400/404/409/412/428 |

DELETE consent находится в body: ConsentToken + Cascade. В query нет versions/count/ExpectedVersion/ExpectedRevision; client IDs из preview не выбирают удаляемые записи. Name/Description PUT не меняет Schema Key, Draft metadata/content или Draft token. Save Draft не переименовывает logical schema.

### 5.2. ETag / If-Match

Exact version GET/mutation возвращает strong numeric ETag собственного int Version. Logical actions используют ETag EntitySchema. Lists не источник per-row token. Parser передаёт ExpectedVersion во внутренний command; публичные DTO/query не содержат technical Version/Revision.

Required header missing →428; malformed/weak/list/wildcard header →400; own token mismatch/conditional-write race →412; basis/lifecycle/dependency conflict →409. Copy требует exact source If-Match для Draft **и Published**: immutable content не означает immutable lifecycle. Clean create не требует token нового root.

Для повторного clean/copy с тем же OperationId сначала проверяется durable receipt/input, затем при отсутствии receipt — current owner/source/precondition. Replay исходного success возвращает первоначальный DTO/ETag даже после последующего изменения или удаления resource; он не запускает новое создание. Copy input включает original source ExpectedVersion: повтор сохраняет тот же request и header. Missing/invalid обязательный header всё равно отклоняется на Api boundary.

### 5.3. OpenAPI и ошибки

OpenAPI отражает точные отдельные §3 requests, required operation collections/IDs, recursive variants, nullable business number и headers 400/404/409/412/428. SchemaVersionDetailsDto содержит resulting server FieldIds, baseline/diagnostics и metadata; technical token только header. Successful write reply — после outer commit, без скрытого последующего HTTP GET.

NSwag regenerated client использует set-accessors; recursive generated request runner, HtmlRenderer и actual browser flows проверяют UI/header boundary в указанном §11 объёме. Generated requests редактируются без второй domain/form copy. LegacyFields остаётся nullable read-only compatibility для исторических read/старого F04 editor; modern instance definition editor входит в следующий F04. Общий error profile — [Q15](../questions/Q15-api-and-source-code.md#согласованный-demo-профиль-authoring-api).

Derived diagnostics/current Published facts не меняют own ETag. Accept передаёт показанную PublishedVersionId, Delete — server ConsentToken; stale external basis проверяется отдельно от own token внутри owner transaction.

## 6. EntityFramework

### 6.1. DbContext ownership

ContentModelingDbContext, schema content_modeling; root DbSets F01/F04, version snapshot JSONB. Нет field table/repository, чужого context/cross-BC FK. Загружаются нужные roots, не все snapshots schema.

### 6.2. Configurations

| Entity | Table / mapping | Key/index | Concurrency |
| --- | --- | --- | --- |
| EntitySchema | F01 mapping: logical metadata/lifecycle/current Published pointer | PK/global unique Key | Собственный Version:int |
| EntitySchemaVersion | F01 mapping: SchemaId/status/nullable number/metadata/content JSONB/baseline/Published snapshot/hash/timestamps | PK; owner/status/time lookup; unique Published number внутри owner | Собственный Version:int conditional write |
| EntityInstanceVersion | F04 mapping; stable SchemaVersionId target | FK SchemaVersionId Restrict, dependency lookup index | Собственный F04 owner, не parent schema Revision |

### 6.3. Constraints / indexes

F01 уже удалил one-Draft filtered index для schema versions. Draft number/published fields null; Published сохраняет number/history. Baseline self-FK Restrict; same-owner/Published semantics проверяются domain/application, один FK их не доказывает. JSONB round-trip сохраняет FieldIds/keys/nested definitions, чтение не генерирует identity.

DB не каскадирует произвольные instance records при delete schema/version. Application формирует allowed plan, FK — финальная защита от dangling references. Constraints из Domain; точные names/indexes/checks Q20.

MVP JSONB definition converter и comparer настроек переиспользуются F01: typed nullable bounds, recursive Item и immutable comparer path входят в полный snapshot; изменение settings обнаруживается replacement. Old Published state/profile/bytes immutable. Save не перехеширует Published и не вызывает downstream instance migration.

## 7. EntityFramework.Persistence

### 7.1. Repositories / query adapters

Root repositories загружают exact schema/version; version lookup проверяет owner. Add/Remove ставят изменения в tracking текущего UoW, без commit. List/exact/impact query handlers используют §3.5 projections/specifications. Exact DTO/ETag из одной version row; issues/base отражают прочитанные facts, не гарантию будущего Publish.

Impact adapter считает все Draft dependencies/blockers, не UI страницу/boolean HasInstances. Mutation adapter rereads IDs/statuses/versions/retained-history facts и удаляет допустимые records явно. SchemaDraftRepository не коммитит. Legacy EditorRepository.GetSchema/GetSchemaVersion/GetInstance принимает schema gate до fresh load; GetInstance покрывает также legacy archive/delete dependency writers, которые не вызывают PublishedSchema. Queries gate не держат.

### 7.2. Transactions

**Demo F02:** ISchemaDraftRepository требует уже открытую physical ContentModeling owner transaction. LockOperationAsync использует transaction-level advisory namespace `0x464F3101`, тот же operation key hash, что F01 creation receipts; LockSchemaAsync — namespace `0x464F3201` и SHA256-derived int32 key exact SchemaId. Clean/Copy берут operation gate, проверяют receipt, затем schema gate. Остальные mutations берут schema gate до authoritative reads. Все calls используют ту же connection/transaction DbContext; outer UoW удерживает lock до commit/rollback. Это per-schema Demo gate, а не будущий глобальный меж-BC MVP gate.

LockSchemaAsync обновляет ранее tracked **unchanged** schema/version roots после acquire, чтобы cached pre-gate entity не обходила current token check; intentional in-flight edits не затираются. New GetSchemaAsync не Include(Versions), exact GetVersionAsync проверяет owner. Legacy schema/instance writers используют тот же gate до authoritative loads; обычные queries/impact preview gate не держат.

Preview выдаёт полный exact состав: schema Draft IDs, dependent Draft version/owner/schemaVersion IDs и owner labels. ConsentToken — SHA256 digest детерминированного server basis: scope IDs, schema lifecycle/own revision/current pointers, own version identities/revisions/status/history, dependency identities/status/schemaVersionId, instance owner revision и dependency content. Он не auth credential и не список client-authorized IDs. Даже прежние counts не позволяют подтвердить изменившийся состав или edited dependency.

DELETE под gate перечитывает current impact, сравнивает opaque token и требует Cascade=true при dependent Drafts. При mismatch —409 schema-delete.impact-changed, новый preview и новое явное подтверждение с 5-second UI delay. RemoveConfirmedAsync строит current server plan, не принимает client IDs и явно удаляет только eligible dependent Instance Draft versions и выбранные schema Drafts. Logical instance owners сохраняются, включая пустые. Logical schema Delete блокируется любым instance owner и любой Published history; F04 logical-instance Delete — отдельное действие. Last Draft Delete сохраняет logical schema; deprecated internal latest pointer меняется на remaining version либо null без schema Version bump.

Server не держит transaction во время UI countdown. Cancellation до commit rollback-ит owner writes. Add/Remove не коммитят; FlushAsync только SaveChanges в существующей transaction и нормализует stale EF write в shared concurrency exception. Source Copy не увеличивает source Version. Receipt сохраняется атомарно с новым Draft и позволяет восстановить original outcome после потери response.

Для MVP владелец этих commits — ContentModeling. По [утверждённому MVP consistency guard](../questions/Q20-persistence-and-migration.md#mvp-протокол-consistency-guard) все CM replicas используют одну PostgreSQL database и один стабильный глобальный exclusive transaction-level advisory key. Каждый affected owner scope явно начинает ReadCommitted transaction, затем отдельным statement вызывает pg_advisory_xact_lock на **той же physical connection и transaction**, до любых authoritative domain reads и row locks. После успешного acquire заново загружаются own roots/tokens и current foreign facts через public ports из независимых primary read scopes/connections. Предварительные query/preview/cache/replica facts не разрешают commit; foreign read не enlist-ится в ambient owner UoW, не разделяет SQL transaction и не открывает foreign EF из caller.

Guard удерживается до owner commit/rollback: изменяются только roots своего BC и атомарные own outbox/audit intent/inbox markers, где они нужны. Все invalidating writers, включая creation/removal/reference Copy/orphan paths, Publish/lifecycle/hard delete, setup/reconciliation, source durable acceptance/watermark/pending/apply/retry и F15 open/supersede/repair/resolve, участвуют в этом же ключе. Own Version/If-Match сохраняется: stale own token →412, изменившаяся foreign/preview basis →409 по Q15. Create/Copy из неизменённого source не увеличивает source Version; два serially guarded допустимых создания могут оба завершиться успешно.

Внешний I/O (broker, VR School, MinIO), ожидание пользователя и долгие streams выполняются вне gate. CPU preparation допустима заранее, но exact basis повторно подтверждается внутри gate; иной basis требует отказа или новой подготовки вне gate. Bounded guard wait/lock timeout конфигурируются, поддерживают cancellation и являются availability/technical failure, не domain409; точные значения и transport mapping остаются Q15/Q20. Connection loss прерывает owner transaction: reconnect-and-continue запрещён. Потеря commit ACK даёт unknown outcome; выяснение exact state/исходного operation ID по Q15 предшествует повтору, blind command retry запрещён.

Один command — один local commit UoW. Draft Save conditional write по собственной Version; два независимых Draft не конфликтуют только из-за общей schema Revision. Content/Title/comment/Version вместе. Любой failure откатывает весь scope.

Одного root ETag недостаточно для неизменяемости external facts. В той же transaction нужен общий protocol:

- Logical Archive/Delete против new Draft/Save/new instance authoring/Publish F03/F04.
- Copy Draft против source Save/Delete, copy lifecycle против Retire/Withdrawal.
- Accept против другого Publish/current pointer change.
- Draft Delete против новой instance reference, изменения dependency Draft и её Publish.
- Retire против нового использования version для authoring/publish.

Для MVP применяется принятый глобальный exclusive advisory guard §7.2/Q20: acquire предшествует загрузке parent/source/dependencies и любым row locks. Все F03/F04 invalidating writers участвуют; FK Restrict/обычный precheck не заменяют coordination. Read-only source не получает искусственный Version increment. Если два Copy проходят на unchanged source, оба создают свои roots; gate сериализует commits без ложного source412.

Delete после guard rereads status/history/current dependencies/observed versions. Changed/retained blocker откатывает plan. Schema Draft-version Delete удаляет только явно подтверждённые dependent Instance Draft versions и сохраняет logical instance owners, даже если они пусты; pointers не dangling. Отдельный explicit logical-instance Delete разрешён только по F04 eligibility/consent. Schema с Published history никогда не hard-delete; last Draft deletion не auto parent deletion.

Preview и UI min 5 s schema cascade не держат DB transaction/locks. Delete consent связывается с exact показанным набором identities/versions/impact; changed set отклоняет Delete и требует нового preview/confirmation, не автоматическое расширение подтверждения. После commit external side effects отсутствуют. Cancellation до commit=rollback; после commit response может потеряться при durable result. Повтор create/copy использует исходный operation ID/input и durable original result; новый blind Create не разрешён.

Demo clean/copy durable result реализован через OperationId/input namespace и SchemaCreationReceipt в той же owner transaction (§3/§7.2). Same ID/input возвращает original resource/token; different input409. Upload durable stages принадлежат F05 и этим receipt не реализуются. Future MVP global consistency не отменяет original-result recovery.

### 7.3. Migrations

F01 уже материализовал independent roots/Version/Draft numbering/recursive JSON definitions и receipt table. [20261007140653_F02EmptySchemaDraftHistory](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.EntityFramework/Migrations/20261007140653_F02EmptySchemaDraftHistory.cs) меняет только deprecated internal latest_version_id на nullable для empty logical history; snapshot/designer согласованы. Up не удаляет существующие rows. Down явно отклоняется, если существуют empty-history owners, и не выдумывает zero/dangling pointer. PostgreSQL Integration проверяет migration/round-trip/deletion; production DB migration/deployment здесь не подтверждаются.

## 8. External adapters

S3, RabbitMQ, HTTP/VRSchool не применяются. TimeProvider — общий clock F01; diff/evolution validator — local pure service. Delivery/Registry не вызываются side effect Save/Retire/Archive. MVP cross-BC retention/integration определяется соответствующими фичами отдельно.

## 9. Data flows

MVP порядок для DF03–DF06, DF08–DF12 и DF14: input/CPU preparation вне gate → явная owner ReadCommitted transaction → standalone exclusive advisory acquire → fresh own roots и foreign primary-port facts → проверки own token/basis/lifecycle/dependencies → Domain action → только own writes/atomic intents → commit и release. Это обязательный префикс каждого указанного mutation/retry stage; строки «load/guard» ниже означают загрузку **после** acquire. Read-only query/impact/preview остаются observations; при необходимости короткий source capture имеет отдельный guarded scope, но долгий stream и пользовательский review не удерживают guard. Demo использует свой согласованный контур без новых обязательных MVP dependencies.

Внутренние algorithms связаны с §5 DF; DTO не переопределяются. Api binding/header parsing/validation до command; successful result только после commit.

### DF01–DF02. List / exact read

DF01: validate list input → проверить owner (missing404) → server status filter → count без paging и stable sort/page summaries (default20/max200) → envelope. Content не читается всей историей. DF02: exact owner+version →404 если не совпали → Content/metadata/Version и current parent pointer → stale/lifecycle/issues → exact DTO/ETag. Без записи/version bump; retired history readable.

### DF03. Clean create

Frontend формирует локальный clean editor без server Draft. Первый valid Save с Title и minimum schema tree → operation ID/input validation → Active schema/current Published facts под guard → server version/field IDs/time → CreateCleanDraft(current baseline, null number) → атомарно Add root + durable operation result → commit → exact new DTO/ETag. Retry с тем же operation ID/input возвращает первоначальный result; нет пустого persisted Schema Draft.

### DF04. Copy

Demo owner transaction/per-schema gate (§7.2) → fresh exact source того же owner + parent availability → source If-Match для Draft и Published; receipt replay до current source checks, затем snapshot/lifecycle Q09 → copy content с теми же FieldIds → новый version ID/time/Title/comment → baseline Draft наследуется, Published=source.Id → Add/commit → new DTO/ETag. Source прежний. Copy старой Published может сразу быть stale. Number/hash не создаются.

### DF05. Save

Demo owner transaction/per-schema gate (§7.2) → fresh exact Draft/parent availability → compare ExpectedVersion → map create branches/server IDs, existing IDs unchanged → whole operation-set validation против original tree → load historical identity facts для submitted/reintroduced IDs → pure applier/resulting snapshot → domain minimum/shape/unique IDs+keys/monotonic validation → UpdateDraft content+metadata → conditional write/commit → resulting DTO/ETag. Invalid поздняя операция не сохраняет ранние; baseline не принимается автоматически. Instance values не переписываются; orphan values/revalidation принадлежат F04.

MVP DF04 сохраняет exact source settings; DF05 после построения resulting tree валидирует constraints/required comparer paths и new references, затем одним UpdateDraft/commit сохраняет дерево. Failure не меняет ранние операции. Если actual schema Draft поменял constraints, F04 revalidation показывает current issues отдельно от собственного instance Version.

### DF06. Accept current base

Exact Draft/ExpectedVersion + consent basis показанной current Published identity → guarded reread parent/current baseline/schema context → проверить ownership, identity/evolution/structural admissibility и точное совпадение показанной базы → AcceptCurrentBase только baseline/Version → commit → DTO/ETag. Missing required instance values и retained orphans не являются strict Publish gate для Accept; payload/Title/comment не меняются. Сменившийся context или invalid evolution отклоняет Accept и требует повторного review; merge отсутствует.

### DF07–DF08. Draft impact / delete

DF07: exact Draft → total dependent Draft count/retained blockers → informational DTO. DF08: exact If-Match/consent → transaction/guard → Draft compare Version → recompute current plan/history → eligibility + exact consent set/basis freshness → remove только явно подтверждённых допустимых Draft instance versions и schema Draft; logical instance owners сохраняются, включая пустые → один commit → deleted ID. Нет Published cascade. Unexpected FK/concurrency failure rollback whole plan; client IDs/count не authority.

### DF09–DF10. Retire / Restore Retired

MVP owner transaction/advisory acquire → fresh exact Published/ExpectedVersion → lifecycle checks + current new-usage facts → Retire(server now)/RestoreFromRetired → conditional lifecycle/Version write/commit → DTO/ETag. Content/number/hash/PublishedAt/history прежние. Existing Draft instances не удаляются/не публикуются; Withdrawn не Restore этой операцией.

### DF11–DF12. Archive / Restore logical schema

MVP owner transaction/advisory acquire → fresh logical root/ExpectedVersion → lifecycle checks → согласовать authoring с F03/F04 writers → parent Archive/Restore only → commit → logical DTO/ETag. Child lifecycle/current pointer/frozen snapshots не массово меняются. Restore не Activate/Publish; exact archived workspace readable.

### DF13–DF14. Logical impact / delete

DF13: exact owner → count всех schema Draft/dependent instance Draft и Published/retained history → impact. DF14: logical If-Match/consent → transaction/guard → reread root/history/current dependencies → запрет при любой Published schema history → отсутствие любых logical instance owners (иначе blocker; требуется explicit F04 Delete) → allowed Draft-only schema plan → explicit FK-safe removals → schema Remove/один commit → deleted SchemaId. Preview не recheck; changed exact set отклоняется и требует нового preview/confirmation. Нет partial cascade.

### DF15. Logical metadata

Logical If-Match → schema gate → fresh Active schema/own token → нормализовать Name/Description → UpdateMetadata, Key неизменен → FlushAsync в owner transaction → in-process GetSchemaDetailsQuery → outer commit → complete EntitySchemaDetailsDto + own schema ETag. Draft roots/content/tokens не меняются; hidden HTTP GET отсутствует.

## 10. Ошибки и диагностика

MVP guarded failure различает confirmed business refusal и невозможность проверки: unavailable/incomplete public facts не означают empty/valid facts. Guard wait cancellation/timeout и connection loss — technical/availability outcome без domain Conflict; точные wire codes остаются Q15. После connection loss scope завершается rollback/disposal без продолжения на новой connection. Unknown commit outcome выясняется по exact state/operation ID; prepared tracker не подтверждает ни success, ни rollback. Own stale ETag сохраняет412, foreign basis change —409. Диагностика фиксирует wait/hold duration, owner/operation, acquire/commit/rollback/unknown phase без full payload и секретов.

| Условие | Ответ / outcome | Сохранение |
| --- | --- | --- |
| Invalid body/recursive shape/conflicting operations | 400, field/operation paths | Ничего |
| Missing/foreign owner/version | 404 | Без fallback |
| Lifecycle/evolution/dependency/history conflict | 409, schema-version.invalid-state / entity_schema.unavailable / schema-delete.blocked | Whole rollback |
| Stale If-Match/conditional-write race | 412 | Чужие изменения прежние, текущий command rollback |
| Required If-Match missing | 428 | Mutation не запускается |
| Changed exact delete-impact/basis | 409 schema-delete.impact-changed либо schema-draft.review-basis-changed | Delete отклонён, нужен новый preview/confirmation |
| Commit succeeded/response lost | Unknown outcome клиента | Server result durable; recovery Q15 |

Логи: correlation ID, DF/operation, SchemaId/VersionId, outcome/conflict category, dependency count/duration; без full content/секретов/stack trace клиенту. Lifecycle conflict не transient retry. Current editor.invalid_operation/concurrent_change не считаются target machine codes.

## 11. Тестирование

Post-Demo delivery extension acceptance: Unit/real PostgreSQL/HTTP проверяют Copy exact mode, nested List item update, invalid late operation whole rollback, Published read-only и unchanged historical profiles. Same-FieldId mode change проверяется только после решения Q37/Q34. Это future checks, не часть уже проверенного Demo.

Подтверждённые проверки checkout 07.10.2026:

- ContentModeling Unit **427/427**; coverage: **91.98% lines / 87.72% branches**.
- Real PostgreSQL ContentModeling Integration **36/36**, включая 13 F02 cases в [F02DraftPersistenceTests](../../../../contentmanager.api/tests/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Tests.Integration/F02DraftPersistenceTests.cs), после регистрации query port в standalone TestModule. Проверены shared F01 operation gate, legacy/schema/instance contention, freshness, full exact consent, last Draft, preserved empty owners и transaction-only flush/concurrency. Persistence TDD наблюдал initial RED, stale-tracking RED и Flush RED, затем GREEN.
- Functional HTTP/OpenAPI **50/50** в [F02SchemaDraftTests](../../../../contentmanager.api/tests/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Tests.Functional/F02SchemaDraftTests.cs) и related ContentModeling HTTP suite: real concurrent Save даёт один 200 / один 412; Copy replay после source change возвращает original result по original header; Accept basis drift 409/no merge; logical metadata оставляет Published captured identity immutable; nested create/operation-set сохраняется atomically. Historical Restore HTTP также подтверждён: exact historical source FieldId восстанавливается в том же Save вместе с Draft metadata, source snapshot остаётся прежним. Scoped App Publishing/Delivery **9/9** прошли как regression boundary; это не реализация F03/MVP.
- UI build и runner F01/F02/legacy + real HtmlRenderer прошли. В настоящем browser проверены Copy→Save→Reopen с preserved source FieldIds, реальный412 сохраняет local form до explicit Discard/Reload, empty clean Save blocked local и valid first Save создаёт exact server Draft.
- Browser delete-проверка открывала full exact preview, отдельное unchecked consent и disabled permanent-delete action, затем Cancel. Permanent GUI delete не выполнялся; actual atomic removal/consent/FK/owner preservation подтверждены PostgreSQL suites. Это различные verification boundaries.

F02 Demo implemented в указанном scope, но не объявляет готовым весь Demo. Historical Restore представлен wire operation/Domain admission; отдельной Restore GUI action нет, history Copy сохраняет identities. Publish остаётся следующим F03; modern instance definition editor — F04; Retire/Archive/Restore logical lifecycle — MVP. Локальные приложения для browser checks остановлены; listening ports проверены свободными, temporary QA browser tab закрыт. Ни tests, ни browser checks не подтверждают production deployment или migration production database. Diff check выполнен.

Стратегия дальнейших проверок по [S04](../standards/S04-testing.md). Unit SUT=domain/applier/validator/mappers; controlled TimeProvider, port doubles только handler orchestration. Integration SUT=real EF repositories/queries/UoW/guard с PostgreSQL Testcontainers. Functional SUT=API WebApplicationFactory + real PostgreSQL, Demo без JWT. Architecture=references/handler placement. Несколько BC одного API не E2E.

Каждый test создаёт свои IDs/data; без shared mutable seed. Изоляция/cleanup fixture, все writes await. Race tests используют отдельные contexts/connections и barriers для точного порядка; текущие F02 gate-contention cases дополнительно проверяют pending task коротким bounded ожиданием. Такой check не заменяет более широкие multi-host MVP barrier tests. Cleanup не удаляет чужие records. Timestamp assertions учитывают PostgreSQL precision. Wire tests используют real OpenAPI/generated serialization, не упрощённый double.

| ID / DF | Уровень / SUT | Данные / воздействие | Результат |
| --- | --- | --- | --- |
| T01 / DF03–04 | Unit + Integration factory/repo | Active owner, clean profile, Draft/Published source | New root/time, null number, correct baseline, copy IDs preserved/source прежний; returned ID history/context проверены |
| T02 / DF04 | Integration + Functional | Concurrent source Save/copy, foreign owner | Нет torn snapshot; stale412/foreign404; lifecycle после Q09 |
| T03 / DF05 | Unit applier | Все types/Object/List<List>/new subtree | New IDs only create, recursive shape/whole-tree uniqueness |
| T04 / DF05 | Unit + Functional | Key/Type rename, inverse transitions, foreign/repeated/conflicting IDs | Save rejected; valid Name/HelpText allowed; no partial state |
| T05 / DF05 | Unit + Integration | Invalid последняя operation, zero roots, duplicate key | Whole rollback, metadata прежние |
| T06 / DF05 | Integration + Functional | Два writers same Draft и different Draft | Same-root412; independent roots без conflict через schema Revision |
| T07 / DF01–03 | Integration queries/mapping | Multiple Draft/Published/JSONB/page/filter | Нет one-Draft ban, null Draft number, stable sort/count, no tree in summary |
| T08 / DF02,05–06 | Unit + Integration | Sibling Publish/stale/valid-invalid Accept/concurrent base change | Stale Save allowed, accept baseline only; invalid no writes; согласованный current |
| T09 / DF07–08 | Integration planner/UoW + Functional | N Draft dependencies/blocker/failure halfway | Exact confirmed N, Draft-version-only atomic cascade, empty logical instance owners preserved, history unchanged, whole rollback |
| T10 / DF08,14 | Integration concurrency | New reference/dependency change/F04 Publish during delete | Changed exact set требует reconfirmation; нет phantom/Published deletion |
| T11 / DF09–12 | Integration + Functional | Lifecycle против concurrent authoring/publish | Own lifecycle/Version; immutable bytes/hash/history; closed targets protected |
| T12 / DF13–14 | Integration + Functional | Never-published/all Draft против Published history, last Draft | Only allowed logical delete; no retained removal; no auto parent delete |
| T13 / writes | Functional actual API/OpenAPI | Missing/stale/current If-Match, response | 428/412/409 distinction, own new ETag, no DTO/query Version/Revision, resulting IDs |
| T14 / DTOs | Functional/codegen | Все 15 variants/nested Lists/null number/new subtree/options/errors | Lossless generated models, required IDs boundary; wire profile Q15 |
| T15 / all | Architecture | Project references/handlers | Domain без EF/HTTP, commands Application, queries Persistence, no foreign context/event dependency |

Legacy Editor persistence test теперь следует transactional owner protocol; независимый raw tracked root отдельно доказывает EF stale concurrency. Legacy DTO сами по себе не подтверждают target HTTP contract. UI 5 s/explicit consent/dirty/412 preservation проверяет frontend F02; atomic effects — real backend DB. Согласованные local-first/Withdrawn/exact-set правила входят в target tests; не выбранные Retired/repeated-action варианты не подменяются assumptions.

MVP Integration matrix для DF03–DF06, DF08–DF12 и DF14 использует real PostgreSQL, отдельные owner connections/UoWs и barriers, включая разные CM replica processes/hosts там, где проверяется общность key. Doubles/in-memory locks не доказывают SQL coordination.

| MVP сценарий | SUT / воздействие | Наблюдаемый результат |
| --- | --- | --- |
| Guard до чтений | Реальный Persistence adapter; writer A держит gate, B начинает owner transaction до acquire; A меняет eligibility/basis и commit, B продолжает | B после acquire читает новый committed state при ReadCommitted; stale предварительные facts не применяются; lock statement использует owner connection/transaction |
| Меж-BC write/read partition | Public read adapter и Host/DI; owner держит guard, foreign primary read отдельным scope | Read не enlist-ится в owner transaction, не читает cache/replica; no foreign writes/EF access; atomic commit только owner + intents |
| Commit/rollback/release | Barriers для invalidating writer и affected operation; injected owner failure/cancellation | Второй writer ждёт release и revalidates; failed owner полностью rollback, нет partial roots/intents; unrelated next scope получает key |
| Wait и connection failure | Configured bounded wait/cancellation, разрыв owner physical connection | Technical/unavailable, не409/empty success; aborted transaction не продолжается через reconnect |
| Unknown commit ACK | Разрыв response/commit acknowledgement и inquiry exact state/original operation ID | Committed result сохраняется, rollback не предполагается; no blind duplicate mutation |
| Own token и foreign basis | Конкурирующий own writer против изменения только foreign basis |412 отдельно от409; оба допустимых Create из unchanged source успешны, source Version прежний |

MVP tests: bounds tightening/loosening с тем же FieldId, invalid settings late operation rollback, copy exact constraints, remove/optional comparator leaf требует исправления того же operation-set, new-field reference resolution, typed JSONB/NSwag round-trip, unchanged old Published profile. Unit applier/policy; Integration real PostgreSQL snapshot/concurrency; Functional existing Save endpoint and OpenAPI fixtures. Compare definition changes отдельно от value-sort; silent coercion/rehash/new ID запрещены.

Temporal acceptance: existing semantic change rollback всего operation-set; delete+new Offset branch new ID; source Copy retains exact semantics/bound offsets/100ns; known same-instant bound comparison; immutable Published /1 read/canonical bytes прежние. Domain applier/historical identity SUT, real PostgreSQL snapshot/atomicity, Api exact generated read fixtures. Audit DateTimeOffset metadata не subject dynamic field migration.

## 12. Порядок реализации

Post-Demo delivery extension: уточнить update/presence и same-FieldId semantics Q37, затем applier/validators/read mappers и JSONB converter F01, отдельный OpenAPI/NSwag/editor inspector. Новые contracts не считаются частью уже проверенного Demo.

1. Использовать принятые Q09 profiles и materialized Demo request/consent/metadata/gate contracts (§2–§7); future MVP contracts не вводить в Demo.
2. Domain F01 roots + F02 applier/monotonic/baseline/lifecycle с Unit checks.
3. EF mappings/migration/Version owners/dependency constraints; real PostgreSQL mapping/race checks.
4. Separate DTO/commands/queries/ports; Application validators/mappers/handlers; Persistence queries/repositories/planner/guard совместно F03/F04.
5. Api ETag/errors/OpenAPI; Functional HTTP tests и NSwag round-trip.
6. Frontend workspace/editor/dialogs; shared F01/F02 editor и real Save/reopen/copy/stale/delete checks.

## 13. Открытые технические вопросы

[Q37](../../frontend/questions/Q37-launcher-asset-delivery.md) владеет post-Demo DeliveryMode carrier/update presence, исторической совместимостью и предложением сохранять FieldId при изменении только режима. Frozen Published bytes/hashes не перезаписываются.

[Q35](../../../business/questions/Q35-value-constraints.md) согласовал semantics первого MVP набора. Остались Q15 new-field comparator references/settings clear/presence/recursive operation wire и exact new-profile layouts Q19/Q31; Offset semantics согласованы Q34; whole new temporal/profile codecs материализуются Q15/Q19/Q31. Это инженерные незавершённые contracts MVP, не дополнительные Demo dependencies.


| Вопрос | Что определить |
| --- | --- |
| [Q09: уточнения F02](../../../business/questions/Q09-editing-and-deletion.md#уточнения-f02-создание-и-удаление-draft) | Clean local-first/minimum, Withdrawn copy/read/Save и exact-set reconfirmation приняты; только не выбранные Retired/repeated-action детали |
| [Q15: детализация F02](../questions/Q15-api-and-source-code.md#f02-детализация-целевого-контракта) | Demo request/Item/option/paging/consent/replay/metadata contracts материализованы; завершить HTTP/OpenAPI/NSwag/UI scope уже проверен; historical Restore HTTP exact identity/source preservation подтверждён. MVP lifecycle/repeated-action/typed constraints остаются отдельно |
| [Q20](../questions/Q20-persistence-and-migration.md) | Demo per-schema gate/port/nullable pointer migration материализованы; global MVP protocol/foreign facts/retention остаются отдельной реализацией |

Неизвестное не отменяет multiple Draft/stable identity/immutable Published/ETag и не вводит auth/шину в Demo.
