# F03. Публикация версии схемы: бэкенд

[Бизнесовая спецификация](../../../business/features/F03-publish-schema-version.md) · [Модуль M01](../modules/M01-content-authoring.md) · [Фронтенд](../../frontend/features/F03-publish-schema-version.md) · [F01](F01-create-entity-schema.md) · [F02](F02-create-schema-version.md) · [Q08](../../../business/questions/Q08-keys-and-drafts.md) · [Q19](../questions/Q19-content-hashes.md) · [Q15](../questions/Q15-api-and-source-code.md) · [Q20](../questions/Q20-persistence-and-migration.md)

## 1. Область реализации

F03 публикует exact сохранённый EntitySchemaVersion Draft: проверяет его собственный ETag, current Published baseline, структуру/evolution; назначает следующий integer business number; фиксирует immutable representation/hash/PublishedAt и schema Name/Key snapshot; обновляет published pointer логической схемы. Два roots одного ContentModeling BC фиксируются атомарно.

UI/Registry/codegen/authoring экземпляров используют Published result. Это не Product/Variant публикация и не Activate/Delivery transition. Save, copy, accept-base, Retire/Archive — F02; instances — F04; Registry/codegen — F21/F22; Withdrawal — F13. Schema Publish без F17 moderation. Demo anonymous, без auth/шины; MVP Specialist F16/Q10, конкретный permission не изобретается.

Demo реализует приведённый ниже контракт. MVP расширения constraints/comparer/Offset и межконтекстного consistency guard остаются целевыми требованиями соответствующих разделов; они не становятся зависимостями Demo.

| Слой / источник | Реализация Demo | Граница |
| --- | --- | --- |
| [Version controller](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Api/EntitySchemas/Controllers/EntitySchemaVersionController.cs), [Publish command](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application.Abstractions/EntitySchemas/Commands/PublishSchemaVersionCommand.cs) | Bodyless POST, own Draft If-Match → internal int; full `SchemaVersionDetailsDto`/ETag | Legacy body не принимается; requestBody отсутствует в OpenAPI |
| [Publish handler](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application/EntitySchemas/CommandHandlers/PublishSchemaVersionCommandHandler.cs), [strict policy](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Domain/EntitySchemas/ValueObjects/SchemaPublishParameters.cs) | Fresh reads под F02 owner gate; own token/state/base; вся identity history; TimeProvider; одна UoW | Без auth/moderation/foreign DbContext/broker/hidden GET |
| [EntitySchema](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Domain/EntitySchemas/EntitySchema.cs), [EntitySchemaVersion](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Domain/EntitySchemas/Entities/EntitySchemaVersion.cs) | Independent `Publish(parameters)`/`RegisterPublishedVersion(version)`; integer sequence, frozen name/key/canonical; каждый root increment один раз | Legacy major/minor facade остаётся для исторических внутренних use cases; modern Archive отклоняется |
| [Canonicalization library](../../../../contentmanager.api/src/BuildingBlocks/ContentManager.Canonicalization/ContentManager.Canonicalization.csproj), [owner mapper](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Domain/EntitySchemas/ValueObjects/SchemaContentCanonicalMapper.cs) | Pure netstandard2.1, typed schema input, fixed `cm-schema-content/1`, immutable bytes/profile/lowercase digest, strict integrity restore | Другие пять content profiles и Registry/codegen codecs не реализуются публикацией схемы |
| [Version EF configuration](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.EntityFramework/Configurations/EntitySchemaVersionConfiguration.cs), [Draft repository](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.EntityFramework.Persistence/EntitySchemas/Repositories/SchemaDraftRepository.cs) | JSONB authoring snapshot отдельно от canonical bytea/text/profile/hash; unique number/checks; two-root commit | Legacy profile/bytes остаются NULL; старые bytes не маркируются /1 |

## 2. Domain

### 2.1. Aggregates, entities и value objects

Root/field/definition модели определены один раз в [F01 §2](F01-create-entity-schema.md#2-domain), lifecycle/change-set в [F02 §2](F02-create-schema-version.md#2-domain). F03 не создаёт новый tree DTO или field identity.

Обязательность в таблице относится к полю соответствующей модели. Для серверного состояния и result она не означает требование передать поле клиентом. `Условно` раскрывает lifecycle или variant; неизвестный wire omission/default остаётся `Уточняется`.

| Тип | Роль / lifecycle | Владение |
| --- | --- | --- |
| EntitySchema | Aggregate Root; полный состав [F01 §2](F01-create-entity-schema.md#2-domain) | Current identity/metadata/pointer; atomically changed Publish |
| EntitySchemaVersion | Aggregate Root; полный состав [F01 §2](F01-create-entity-schema.md#2-domain) | Exact Draft; Published content/number/hash/identity immutable; lifecycle F02/F13 |
| SchemaVersionContent / SchemaField / definitions | Recursive snapshot [F01 §2](F01-create-entity-schema.md#2-domain) | Stable identities; не EF rows; anonymous Item без Guid |
| SchemaPublishParameters | Domain parameter object | Серверные вычисления; не HTTP request |
| SchemaPublishValidationIssue | Pure validation result | Transport code/path Q15; без HTTP/EF |

**`SchemaPublishParameters`**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `VersionNumber` | `int` | Да | Следующий business number | Positive checked sequence per schema; only successful commit |
| `PublishedSchemaName` | `EntitySchemaName` | Да | Frozen parent name | Согласованный server snapshot |
| `PublishedSchemaKey` | `EntitySchemaKey` | Да | Frozen parent key | Согласованный server snapshot |
| `Canonical` | `CanonicalRepresentation` | Да | Server-computed profile/bytes/digest triple | Supported exact schema profile (/1 historical, new MVP constraints/Offset); immutable same-byte factory; not HTTP authority |
| `PublishedAt` | `DateTimeOffset` | Да | Server Publish time | TimeProvider; immutable |

**`SchemaPublishValidationIssue`**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Logical category (имя уточняется)` | Уточняется | Уточняется | Категория strict validation | Result shape [Q15](../questions/Q15-api-and-source-code.md) |
| `FieldId` | `SchemaFieldId?` | Нет | Named field issue location | Nullable для другого location context |
| `Definition/option location (имя уточняется)` | Уточняется | Уточняется | Recursive issue location | Exact shape/path [Q15](../questions/Q15-api-and-source-code.md) |
| `Message (имя уточняется)` | Уточняется | Уточняется | Описание проблемы | Exact result/transport shape [Q15](../questions/Q15-api-and-source-code.md) |

CanonicalRepresentation profile/exact bytes/digest persisted atomically with Published state; optional CanonicalContentJson is strict UTF8 decoding of SAME bytes/noBOM/noFinalLF, not new serialization; physical mapping Q20. Immutable published contract не пересобирается при read через новый serializer. Generated JSON Schema/C# — производные F21/F22, не замена исходному snapshot/hash.

### 2.2. Методы и переходы

| Тип | Метод | Параметры | Предусловия | Изменение / результат |
| --- | --- | --- | --- | --- |
| StrictSchemaPublishValidator (domain policy) | Validate | candidate:SchemaVersionContent, currentPublished:SchemaVersionContent?, исторические identity facts при необходимости Q15 | Loaded snapshots того же owner; validators не мутируют input | Complete structural/evolution issues либо success |
| EntitySchema | GetNextPublishedNumber | — | Согласованный current pointer/number; next вычисляется без overflow | (LatestPublishedVersionNumber ?? 0)+1; read-only расчёт, номер не резервируется |
| EntitySchemaVersion | Publish | SchemaPublishParameters, validated content/policy result | Status Draft; root not lifecycle-blocked; positive number; valid canonical/hash/time/metadata; strict policy passed | Status Published, number/name/key/canonical/hash/PublishedAt; own Version increment; Id/SchemaId/Content/Title/comment/CreatedAt/base сохраняются |
| EntitySchema | RegisterPublishedVersion | VersionId:EntitySchemaVersionId, number:int, принадлежность version | Active; same schema; number ровно next; baseline совпадает; concurrent guard | LatestPublishedVersionId/Number и own Version; не latest-created pointer |

Два transitions не делают commit сами и не вызывают HTTP/другой BC. Failed second transition откатывает весь command. Version increment делается одним принятым root/persistence механизмом, без двойного increment domain+EF. Последующие Save/повторный Publish Published resource запрещены; lifecycle timestamp actions остаются F02/F13.

### 2.3. Инварианты и constraints

[Q08](../../../business/questions/Q08-keys-and-drafts.md), [Q34](../../../business/questions/Q34-schema-field-semantics.md), [S21](../standards/S21-aggregate-constraints.md). Статические metadata/field/option ограничения берутся из владельцев F01/F02; state/evolution/ownership — Domain policy, не новые literals FV/EF.

- ExpectedVersion выбранного Draft совпадает, Status=Draft, logical owner доступен для authoring. Null base == null current допустимо первой публикации.
- BasePublishedVersionId == current LatestPublishedVersionId; source старой Published или copied stale Draft не получает автоматический rebase.
- Number только при successful Publish: положительный integer1,2,3..., unique per SchemaId; из parent committed sequence, не MAX по ограниченной странице, не PostgreSQL sequence/резервирование вне transaction. Failed Publish/Draft deletion не расходуют номер; опубликованный номер не меняется/не переиспользуется. Overflow не допускает wrap/partial commit, error profile Q15.
- Полное resulting дерево структурно валидно: минимум root field, whole-tree unique IDs/keys, valid Object/Enum/FlagEnum minimum, recursive List.Item включая nested List, known types/settings. Все 15 типов F01, numeric options/InheritanceStrategy/ColorFormat/SortOrder отсутствуют.
- Для одинакового FieldId Key/Type/type semantic identity и вся parent-chain identity неизменны, Name/HelpText mutable. Required false→true запрещён, Deprecated true→false запрещён, Deprecated должен быть optional. Удалённый field может отсутствовать; новый смысл получает новый server FieldId. UI read-only controls не заменяют policy.
- Current Published Enum/FlagEnum option keys нельзя удалить/rename/reuse; отказ от option выражается Deprecated false→true. Name mutable, новые keys допустимы; option Key identity без Guid/numeric Value/BitValue.
- Validator сравнивает весь recursive semantic definition, не плоский ListItemType. Возврат FieldId после отсутствия разрешён только с теми же Key/Type/meaning/parent chain и monotonic flags во всей истории. Перенос в другой parent — новый server FieldId. Historical identity index/read проверяет не только latest Published; exact mapping/query и anonymous Item address encoding уточняются Q15/Q20.
- Q35 typed constraints/comparer обязательны для согласованного MVP набора: повторить DefinitionConstraintsPolicy F01 на всём recursive snapshot, path/type/required consistency. Bounds можно усилить/ослабить без нового FieldId; key/type/container/monotonic guards независимы. В Demo эти extra rules не требуются. F03 не проверяет instance completeness и не грузит все instances/assets.
- Published snapshot/name/key/hash/time immutable. Parent rename не переписывает historical metadata. Title/comment сохраняются как metadata версии и не входят в semantic definition hash. Hash исключает Name, HelpText, option labels, Title, ChangeComment, publication numbers/times по Q19; annotations/full Published snapshot остаются immutable отдельно.
- Retired/Withdrawn current history не удаляется и не вызывает откат numbering/pointer к более раннему номеру. Withdrawn read/Copy/Save разрешены, Publish/runtime блокируются до явного возврата; Publish сам не очищает withdrawal. Допустимость lifecycle-blocked baseline для нового Publish отдельно Q15, не выводится из exact historical read.

Strict validation повторяется на server при Publish независимо от ранее прочитанных PublishIssues. Эти read issues являются advisory. При параллельном Publish одной baseline только один победитель; другой остаётся Draft со stale-base conflict.

Post-Demo Publish фиксирует DeliveryMode exact recursive `AssetReference` leaf definition как immutable metadata новой SchemaVersion. Повторно проверяет known mode/applicability/default и согласованные identity guards F01/F02; MediaLibrary bytes для этого не скачивает. Новая policy не расширяет frozen runtime descriptor F04. `cm-schema-content/1` и `cm-json-schema/1` не получают новое свойство молча: нужен новый versioned canonical profile либо отдельный versioned integrity profile metadata. Конкретный выбор/ID/layout/golden fixtures и same-FieldId compatibility остаются [Q37](../../frontend/questions/Q37-launcher-asset-delivery.md). Исторические Published без setting имеют effective `OnDemand`, исходные bytes/profile/hash сохраняются. Таблица CanonicalAssetDefinition ниже описывает `/1`, не новый delivery codec.

### 2.4. Domain events

Current [EntitySchemaVersionPublishedDomainEvent](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Domain/EntitySchemas/DomainEvents/EntitySchemaVersionPublishedDomainEvent.cs) создаётся EntitySchema.PublishVersion с payload, определённым ниже. При поиске source потребитель этого события не найден; оно не доказывает Registry update/broker delivery.

Target F03 не требует нового domain/integration event для наблюдаемого результата: Published rows/pointer доступны после commit. Если существующее local событие сохраняется для реального потребителя, факт/время соответствуют этому transition и не отправляются при rollback; необходимость/consumer contract Q15. Outbox/RabbitMQ/refresh notification не добавляются только ради логирования.

**`EntitySchemaVersionPublishedDomainEvent`** — current event; необходимость target/local consumer Q15

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `SchemaId` | `EntitySchemaId` | Да | Logical schema identity | Current payload; target consumer не найден |
| `SchemaVersionId` | `EntitySchemaVersionId` | Да | Published version identity | Current payload; no broker inference |
| `OccurredAt` | `DateTimeOffset` | Да | Серверное время transition | Current default DateTimeOffset.UtcNow; не client input; target rollback [Q15](../questions/Q15-api-and-source-code.md) |

## 3. Application.Abstractions

### 3.1. Commands

| Command | Result | Concurrency |
| --- | --- | --- |
| PublishSchemaVersionCommand | SchemaVersionDetailsDto F02 + internal response metadata | Own Draft If-Match; observed parent Version internal, не второй client token |

**`PublishSchemaVersionCommand`**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `SchemaId` | `Guid` | Да | Logical owner | Route ID |
| `VersionId` | `Guid` | Да | Exact saved Draft | Same owner |
| `ExpectedVersion` | `int` | Да | Internal Draft precondition | Api If-Match; не wire DTO |

Команда не содержит tree/Title/номер/hash/time/base от клиента. Это Publish сохранённого exact state, а не Save+Publish. Transport Version отсутствует. Result prepared в handler, но не выходит Api как success до UoW commit.

### 3.2. Queries

Новых query/preview endpoints нет. F02 List/Get показывают saved Draft/base/current Published/PublishIssues и возвращают exact owner ETag. Handler читает roots/snapshot через command ports; это не дополнительный HTTP GET. Registry F21 отдельно читает exact Published результат. Нет API «зарезервировать следующий номер».

Массовое действие «Зафиксировать выбранные версии» переиспользует F03 как exact single-owner use case, сохраняя strict guards/If-Match и immutable result. Coordinator не публикует произвольный latest и не объединяет BC transaction; target модели/границы описаны в [F07](F07-product-content-assignment.md#массовая-фиксация-и-подготовка-выпуска), transport [Q38](../../../business/questions/Q38-bulk-version-freeze-and-release-preparation.md). Это не утверждение о готовом batch endpoint.

### 3.3. DTO и parameter models

Neutral common .NET canonicalization contract types are defined once here; F04/F10/F13/F21/F22 reuse this technical contract, not a dependency on F03 feature runtime or CM Domain. Library imports no Domain/EF/AspNetCore and receives explicit profile + typed normalized inputs mapped at owning boundaries; no untyped JSON-number guessing. Domain-owned ContentHash VO may wrap CanonicalDigest.Value without reselecting algorithm/profile.

**CanonicalProfileId (neutral immutable value)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Value | string | Да | Registry-known exact profile identifier | Six selected /1 identifiers Q19 plus explicitly implemented new MVP schema/instance profiles (names/golden codec Q19/Q31); exact ordinal comparison, unknown rejected/no implicit latest |

**CanonicalDigest (neutral immutable value)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Value | string | Да | SHA256(exact canonical UTF8 bytes),64 lowercase hex | No prefix; fixed algorithm, not cookie/verifier/storage ETag/authority |

**CanonicalRepresentation (neutral immutable result)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProfileId | CanonicalProfileId | Да | Exact byte-contract registry profile | Compared with digest, never hash-only cross-profile equivalence |
| Bytes | ReadOnlyMemory<byte> | Да | Exact owned canonical UTF8 representation | No BOM/insignificant spaces/final LF; factory owns immutable copy/no mutable alias |
| Digest | CanonicalDigest | Да | Digest computed/validated by same-byte factory | Mismatch rejects; no later serialization/re-encoding before hashing |

Factory computes or validates digest against the same immutable byte buffer/profile. Canonical schema publish uses cm-schema-content/1. Future byte-changing profile including bugfix new ID; historical profile+bytes+digest preserved, no rehash/rewrite. Existing legacy bytes cannot be stamped as /1 without proven exact profile conformance; unknown/corrupt historical representation fails explicit compatibility/integrity handling Q20, never silently normalized. cm-json-schema/1 and cm-csharp/1 are separate tooling output profiles, not these content profiles.

SchemaVersionDetailsDto определяется F02; published number/name/key/hash/time/profile становятся заполнены, Status=Published, own ETag — header. Draft identity не меняется. EntitySchemaVersionSummaryDto/EntitySchemaDetailsDto для rail/pointer refresh — F01/F02; не включаются как несогласованный второй result body.

HTTP request body отсутствует: publish request DTO не создаётся. Route IDs и If-Match — полный input. SchemaPublishParameters — внутренний parameter object §2; CanonicalSchemaContent — application result canonicalizer, определён ниже по Q19. Bytes вычисляются до hash и не произвольно перекодируются после него. Exact wire Issue/metadata carrier — Q15; public DTO без Version/ExpectedVersion/Revision.

**`CanonicalSchemaContent`**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Canonical` | `CanonicalRepresentation` | Да | Canonical schema result | Actual supported schema profile (/1 historical/new MVP); neutral immutable triple same factory bytes/hash |

Neutral typed schema canonical input uses fixed PRIVATE cm-schema-content/1 syntax, not public authoring DTO/Registry emission. Root service member fields; each named field member order fieldId,key,type,required,deprecated,definition. Field arrays at every depth sorted by lowerD FieldId ordinal. Known type tokens retain all15 approved meanings, no future Q35 rule injection.

**CanonicalSchemaInput (neutral immutable normalized input)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Fields | IReadOnlyList<CanonicalSchemaField> | Да | Complete semantic named fields | Emits root {fields}; no owner/version/label/time/hash metadata |

**CanonicalSchemaField**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| FieldId | Guid | Да | Stable named field identity | Emits fieldId lowercaseD; sorts ordinal D |
| Key | string | Да | Exact immutable runtime contract Key | Verbatim unicode/case, no trim |
| Type | CanonicalSchemaType | Да | Known15-type neutral enum | Fixed v1 type token; no Domain enum dependency |
| Required | bool | Да | Semantic presence constraint | No CLR-default rule |
| Deprecated | bool | Да | Semantic deprecation flag | Profile meaning, not label |
| Definition | CanonicalSchemaDefinition | Да | Closed typed definition variant | Selected member layout below; no arbitrary dictionary |

**CanonicalObjectDefinition : CanonicalSchemaDefinition**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Fields | IReadOnlyList<CanonicalSchemaField> | Да | Recursive object named fields | Emits {fields}, sorted FieldId |

**CanonicalOptionsDefinition : CanonicalSchemaDefinition**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Options | IReadOnlyList<CanonicalSchemaOption> | Да | Enum/Flag semantic definitions | Emits {options}, optionKey ordinal |

**CanonicalSchemaOption**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Key | string | Да | Immutable option identity | Emits key first, ordinal sorting; no Guid/numeric bitmask |
| Deprecated | bool | Да | Exact semantic flag | Emits deprecated second; labels excluded |

**CanonicalAssetDefinition : CanonicalSchemaDefinition**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| AssetKind | string | Да | Required registered AssetKind | Emits {assetKind}; Image/Audio/Video Demo vocabulary, not media metadata |

**CanonicalListDefinition : CanonicalSchemaDefinition**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Item | CanonicalAnonymousItemDefinition | Да | Recursive anonymous item definition | Emits item first; supports scalar/Object/List |
| OrderMode | CanonicalListOrderMode | Да | Neutral Default/Ascending/Descending enum | Emits orderMode second; stores meaning, runtime array never sorted by writer |

**CanonicalAnonymousItemDefinition**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Type | CanonicalSchemaType | Да | Exact anonymous item type | Emits type first; no FieldId/Key/Required/Deprecated |
| Definition | CanonicalSchemaDefinition | Да | Exact recursive definition | Emits definition second |

CanonicalScalarDefinition has no fields and emits {} for scalar types. Object emits {fields}; Enum/FlagEnum {options:[{key,deprecated}]}; AssetReference {assetKind}; List {item:{type,definition},orderMode}. Service member order fixed above, not alphabetic. CanonicalSchemaType maps String/Text/Integer/Float/Decimal/Boolean/Date/DateTime/TimeSpan/Enum/FlagEnum/Color/Object/List/AssetReference; no public type-wire rename. Neutral CanonicalListOrderMode maps exactly stored Default/Ascending/Descending. This normalizes already validated semantic values, no default insertion/new intrinsic constraints/comparer.

**SchemaPublishedCanonicalRead (immutable public integrity value)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProfileId | CanonicalProfileId | Да | Frozen schema source content profile | Actual frozen supported schema-content profile: /1 historical/new constraints+Offset MVP; not Registry profile |
| ContentHash | CanonicalDigest | Да | Frozen exact semantic digest |64lowerhex/no prefix, compare profile too |
| CanonicalBytes | ReadOnlyMemory<byte> | Да | Exact stored semantic bytes | No read-time regeneration/normalization |

**SchemaPublishedCanonicalState (own persisted immutable value)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Canonical | CanonicalRepresentation | Да | Frozen publication triple | Published required/Draft absent; own root mapping, no standalone mutable entity |

### 3.4. Ports

| Port | Операции / ответственность | Направление |
| --- | --- | --- |
| IEntitySchemaRepository F01/F02 | Get exact schema/current pointer/metadata/observed Version | Application → root command storage |
| IEntitySchemaVersionRepository F02 | Get exact owner+Draft; Get Published by pointer ID; tracking mutations | Application → local command storage |
| Local mutation guard F02 | Согласовать Publish с parent Archive/Delete/rename, baseline/Retire/Withdrawal, Draft Save/Delete и dependency writers | Application → ContentModeling Persistence; принятый MVP protocol Q20, signatures Q15 |
| Historical field identity reader при необходимости | Stable ID semantics вне latest Published для strict evolution | Local ContentModeling read facts; необходимость/shape Q15, не Registry HTTP |
| ISchemaContentCanonicalizer | Canonicalize(validated snapshot/contract inputs Q19):CanonicalSchemaContent | Application → deterministic local adapter |
| IContentHasher / factory | SHA256(same canonical Bytes):CanonicalDigest | Pure common .NET profile factory; Domain wrapper mapping |
| TimeProvider | Единый server PublishedAt | Application → clock, общий F01 |
| IUnitOfWorkManager / commit conflict boundary | Atomic commit двух roots; failed commit rollback; fresh classification scope | CQRS composition → local Persistence |

Repositories не возвращают IQueryable и не коммитят по одному root. Domain не знает JSON serializer/EF/HTTP. Canonical/hash contract ports не означают network dependency. Чужие DbContexts/Publishing/Delivery ports отсутствуют.

MVP coordination port принадлежит Application.Abstractions: задаёт границу guarded owner mutation, cancellation и outcome, без PostgreSQL/Npgsql/EF типов в Domain. Точные CLR-имена и signatures остаются Q15. Application открывает owning UoW и требует guard до authoritative reads; EntityFramework.Persistence выполняет standalone pg_advisory_xact_lock на той же physical connection и transaction владельца. Public foreign read adapters используют независимые primary read scopes/connections без ambient UoW enlistment. Host/DI отделяет owning UoW от этих scopes; существующий [EfCoreUnitOfWorkParticipant](../../../../contentmanager.api/src/BuildingBlocks/ContentManager.EntityFramework.UnitOfWork/EfCoreUnitOfWorkParticipant.cs), способный enlist несколько contexts в общую SQL transaction, не выполняет эту целевую границу автоматически. Domain получает уже проверенные предметные facts и не содержит SQL guard type.

### 3.5. Specifications / read contracts

По [S19](../standards/S19-specifications.md) SchemaVersionByOwnerAndIdSpecification, SchemaByIdSpecification и PublishedSchemaVersionByOwnerAndIdSpecification переиспользуют atomic OwnerId/Id/Status builder conditions из F02. Current Published загружается exact pointer ID, не latest-created/наибольший номер из page. Tracking policy command repositories отдельно от no-tracking F02 projections; filter не скрывает missing/foreign resource fallback.

Если для immutable historical identity нужен history projection, он не ограничивается UI paging и не загружает instance values. Concrete projection/profile Q15. Новые queries по [S20](../standards/S20-cqrs.md), если появятся после решения, размещаются в EntityFramework.Persistence, не Application.

## 4. Application

### 4.1. Handlers / use cases

PublishSchemaVersionCommandHandler в Application выполняет DF01 §9. Его dependency graph: repositories/guard, pure strict validator, canonicalizer/hasher, TimeProvider. Он не использует общий Editor handler других use cases/чужой reference guard для Demo.

Load/ExpectedVersion/state/base checks → complete strict validation → number/canonical/hash/time → version.Publish + schema.RegisterPublishedVersion → prepared DTO/metadata → внешний local UoW commit → success. Время/hash/номер не сообщаются клиенту как confirmed до фиксации. Исторические rows и другие Draft не мутируются ради обновления IsStale; stale derived от pointer.

### 4.2. Validators

PublishSchemaVersionCommandValidator по [S18](../standards/S18-fluent-validation.md) проверяет IDs/internal input profile, но не ожидает body tree. Общие structural field/definition/metadata validators F01/F02 переиспользуют Domain constraints без DTO mutation/default insertion. StrictSchemaPublishValidator — pure policy loaded content, не FluentValidation HTTP binding.

Сначала shape/null/known-variant/коллекции, затем tree identity/uniqueness и cross-version evolution. Индексы FieldId и option Key позволяют сопоставлять смысл, array position/Name не identity. Ищутся все релевантные issues с адресуемым path, без исключений null subtree. Async DB/lifecycle/concurrency facts читает handler/guard; validator не пишет в БД, не обращается ко всем instance/asset records. Known PublishIssues UI не заменяют повторную validation.

В MVP StrictSchemaPublishValidator повторяет typed settings guards F01 и comparer resolution на exact saved tree, включая nested Item definitions. Invalid settings не публикуются даже после ранее успешного Schema Save. Старый historical identity validator не трактует bounds/comparer как immutable type identity; changing Required/Deprecated всё ещё monotonic. Definition validation не мигрирует существующие instances.

Temporal StrictSchemaPublishValidator проверяет DateTimeValueDefinition.TemporalSemantics/typed bound domain, historical semantic identity F01/F02. Naive→Offset с тем же FieldId invalid; новые Offset поля + valid bound offsets по Q34. DateTimeOffset system audit metadata не field semantics. Same instant bound offsets normalize в semantic input, не произвольный current timezone.

### 4.3. Mappers

Api route+If-Match → typed IDs/internal ExpectedVersion. Loaded Domain snapshots → pure validation/contract input; canonicalizer не сериализует случайную transport projection version.ToDto().Fields как нормативный контракт. SchemaPublishParameters задаются только серверными facts; field IDs не генерируются на Publish.

Post-transition Domain→SchemaVersionDetailsDto использует F02 mapper и returned immutable result, не новый latest query. Internal resulting Version передаётся отдельно response metadata. Issue mapping Domain location→FV/ProblemDetails/OpenAPI paths Q15; не flatten все categories в generic409 concurrency.

MVP mapper включает frozen typed constraints/comparer в neutral semantic schema input для нового profile, а не в неизменяемый cm-schema-content/1 layout. Profile support проверяется заранее; при отсутствии реализованного codec с golden fixtures Publish нового contract не выдаёт под /1. Схемы без новых rules и их historical bytes сохраняют прежнюю семантику /1; old Published не rehash. Definitions owner F01; neutral canonical model расширяется только с новым profile, без Domain/EF dependency.

### 4.4. Services / algorithms

Canonical v1 [Q19](../questions/Q19-content-hashes.md#принятый-canonical-profile-v1): exact UTF8/noBOM/no insignificant space/no finalLF; NDJSON LF framing outside hash. Duplicate properties/badUnicode/trailinggarbage invalid. Strings preserved/no trim/case/Unicode normalization; quote/backslash escaped, short control b/t/n/f/r, others lowercase u00xx, other Unicode directUTF8/no HTML/slash escaping.

Typed Int64 exact decimaldigits/no exponent/plus/leadingzero/double. Decimal exact noexponent/rounding/scale semantics, trim fractionalzeroes (1.2300→1.23,1.000→1). Float finiteIEEE64 emission [RFC8785 §3.2.2.3/AppendixB](https://www.rfc-editor.org/rfc/rfc8785.html#section-3.2.2.3), -0→0/NaNInfinity reject. Not whole JCS: exact integer/decimal and CM field ordering differ; ToString/general serializer defaults not profile proof.

Presence absent/null/default distinct; invalidnull/missingrequired reject, no defaults. FieldIDs lowercaseD ordinal; Enum definitions/Flag values immutableKey ordinal. Each profile fixed service-member order/layout Q19/literalvectors, not generic alphabetical DTO serialization. Runtime Lists preserve storedarrayorder incl stored Asc/Desc choice, no implicit sorting/Q35 value-order validation/historical rewrite. All15 type/Date/naiveDateTime/TimeSpan/Color semantics preserved.



Canonicalizer maps owner semantic definition to explicit neutral CanonicalSchemaInput above and emits fixed private cm-schema-content/1 syntax, затем deterministic representation/bytes; exact Registry JSON Schema representation F21 имеет отдельный ETag. Field ordering default по stable FieldId Q34; current CanonicalContent сортирует только object properties и сохраняет arrays, поэтому сам по себе не устраняет зависимость от порядка field collection. Semantic definition hash excludes Name/HelpText/option labels/Title/ChangeComment/publication numbers/times. Canonical ordering/temporal/SHA256lowerhex/escaping/typed numbers/noFinalLF selected; implementations checked independent literal vectors. Hash вычисляется ровно по bytes возвращённой representation, не по повторной serialization DTO.

Current [ContentManagerApplicationModule](../../../../contentmanager.api/src/App/ContentManager.Application/ContentManagerApplicationModule.cs) вручную связывает IEditorRepository с EditorRepository. Target commands/validator/mapping регистрируются Application; repositories/guard/query projections — Persistence; context/mappings — [EntityFramework module](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.EntityFramework/ContentModelingEntityFrameworkModule.cs); headers/controller — Api; App/Host связывают выбранные local canonical/hash implementations. Новый production package/BC/service не выбирается только ради описания портов.

MVP schema hash учитывает declared semantic bounds/collection rules/comparer identities и ordered FieldPath. UI preset non-negative сохраняется как Minimum=0, не дополнительный флаг. Exact layout/absence representation нового profile остаётся Q19/Q31 до golden fixtures; нельзя сериализовать Domain DTO произвольно и считать codec согласованным. cm-instance-content/1 не меняется из-за definition-only расширения, когда runtime values encoding прежний. Canonicalizer не сортирует authored values.

Новый MVP schema canonical mapper включает immutable Naive/Offset semantic discriminator и Offset constraints bounds UTC7digitsZ. Authoring bound offsets сохраняются в content snapshot, equivalent bound instant representations same semantic bytes/hash. Первые новые profiles совместно охватывают Q35+Offset; instance runtime изменяется и требует нового instance-content profile. Old /1 schema definition writer/layout остаётся прежним (Naive), Offset source не downgraded в него. Shared TemporalCodec owner F04; exact whole new-profile member layout/identifiers golden Q19/Q31 инженерно открыты.

## 5. Api

### 5.1. Endpoints

Транспортный поток по [S17](../standards/S17-data-flows.md), HTTP по [S16](../standards/S16-rest.md). Existing nested route сохраняется; body/response/concurrency меняются целевым контрактом.

| Поток | Транспорт | Метод / канал | Endpoint / destination | Доступ | Вход | Application action | Выход | Успех | Ошибки |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| DF01. Publish exact Schema Draft | HTTP | POST | /api/v1/entity-schemas/{schemaId:guid}/versions/{versionId:guid}/publish | Demo anonymous; MVP Specialist F16/Q10 | Route IDs, required If-Match; body отсутствует | PublishSchemaVersionCommand | ApiResponse<EntitySchemaVersionDto> + strong ETag | 200 после commit | 404/412/428; stale-base409, invalid state409; strict issue mapping400/409 Q15 |

Read preconditions/resulting history — F02 DF01/DF02; никаких implicit Save/query/reserve-number/preview endpoints.

### 5.2. ETag / If-Match

Api разбирает strong exact Draft ETag в ExpectedVersion:int. Token логической схемы/другого Draft/list не допустим. Required header missing428, stale/mismatched412, domain stale-base409. Malformed/wildcard/multiple-header parsing profile — общий Q15, не private publish convention.

Successful response возвращает ETag того же version resource после Published transition; root Id не меняется. Parent internal Version увеличивается также, но второй ETag не помещается в version response как token для logical actions. Для них frontend получает own exact schema ETag через F01. Публикуемый DTO/query не содержит Version/Revision; клиент не передаёт назначаемые number/hash/time/base.

### 5.3. OpenAPI и ошибки

Action не принимает RevisionRequest/generated publish DTO. OpenAPI фиксирует отсутствие requestBody, required If-Match, ETag response и full SchemaVersionDetailsDto с Published state/nullable Draft profile F02. ProducesResponseType описывает200/400/404/409/412/428/500. Непустой unexpected body даёт400 `schema-publish.unexpected-body`; caller-assigned publication fields не являются альтернативным contract.

Согласованные machine codes прежнего F03: schema-draft.base-outdated и schema-version.invalid-state. Exact shape/diagnostics и structural400 vs evolution409 profile уточняется Q15; generic editor.concurrent_change не заменяет оба случая. NSwag регенерируется после backend contract verification, generated code вручную не меняется; headers доступны через общую transport infrastructure. Return full confirmed result устраняет hidden GET после publish для получения номера/identity.

Общий Demo transport profile [Q15](../questions/Q15-api-and-source-code.md#согласованный-demo-профиль-authoring-api): issues имеют stable machine code, message и address; адреса named values используют FieldId и ItemKey, а не индекс элемента List<Object>. List queries используют единые paging/sort conventions со stable identity tie-breaker; конкретные names/limits/defaults остаются инженерной детализацией. Binary success остаётся binary, ошибки используют общий error profile. NSwag generation и recursive round-trip обязательны до интеграции UI; схема всех 15 типов и nested Lists проверяется буквально, без сокращённого набора variants.

ETag exact authoring read относится только к сохранённому mutable root. Derived diagnostics имеют отдельную identity basis, включающую exact schema/content/baseline context: изменение diagnostics не имитирует изменение own ETag. Accept и delete consent связываются с показанным context/impact; одного root ETag для этих external facts недостаточно.

## 6. EntityFramework

### 6.1. DbContext ownership

Одна ContentModelingDbContext transaction/schema content_modeling. Два tracked roots из F01/F02; current Published baseline — exact read, historical snapshots не обновляются. No EF navigation/cross-BC FK в Registry/Publishing/Delivery.

### 6.2. Configurations

| Entity | Table | Key/index | Concurrency | Mapping |
| --- | --- | --- | --- | --- |
| EntitySchema | content_modeling.entity_schemas | PK, global Key unique F01 | Version:int conditional write | LatestPublishedVersionId:Guid?, LatestPublishedVersionNumber:int?; metadata/lifecycle F01 |
| EntitySchemaVersion | content_modeling.entity_schema_versions | PK, owner FK, unique(schema_id, version_number) при number not null | Own Version:int conditional write | Draft/Published state, Content JSONB, profile_id+exact canonical_bytes+64lowerhex digest/time/name/key; optional text strict same-byte UTF8 decode |

JSONB не является hashed representation: его physical formatting/ordering не используется для вычисления contract hash. Canonical text/bytes profile Q19 сохраняется отдельно. Storage comparer замечает replacement Content; Published lifecycle writes не пересобирают snapshot.

Local Demo storage (08.10.2026): `EntitySchemaVersionConfiguration` maps `ContentHashProfile` to nullable `content_hash_profile:text` and the private `_canonicalContentBytes` buffer to nullable `canonical_content_bytes:bytea` with field access. Migration `20261008085953_F03CanonicalPublication` adds these columns without reserializing or assigning a profile to legacy rows. The existing `published_content_json:text` is the same-byte UTF-8 convenience representation. The new-profile CHECK requires the profile/bytes pair and complete publication fields, including exact UTF-8 text/byte equality; a separate CHECK protects positive integer publication numbers. The existing partial unique `ux_schema_published_version_number` remains the numbering defence. Null profile/bytes identify retained legacy storage and do not claim `/1` conformance. Owner mutation reuses the F02 schema gate and existing local UoW; this does not implement the separate MVP cross-BC coordinator described below.

### 6.3. Constraints / indexes

Published numbers unique per SchemaId, positive; Draft VersionNumber null. Published metadata/canonical/hash/time non-null, Draft published fields null; target CHECK/state rules согласуются Q20 без arbitrary literals. Owner pointer FK внутри BC; same-owner/Published-pointer semantics дополнительно application/domain, простая FK недостаточна. Number/pointer initial null согласованы, root fields обновляются вместе.

Unique index — последняя защита параллельной allocation, не замена baseline/root Version guards. PostgreSQL23505 классифицируется по конкретному constraint/context; нельзя все unique violations объявлять stale ETag412 или baseline409. Parent/own concurrency tokens сохраняют atomic increments и запрещают last-write-wins.

MVP constraints/comparer остаются частью immutable schema Content snapshot F01/F02; новый canonical profile+exact bytes+digest сохраняются атомарно тем же mechanism. Не вводить mutable global rule rows, rehash jobs или переинтерпретацию old profile. Lifecycle updates immutable definitions/hash не меняют.

## 7. EntityFramework.Persistence

### 7.1. Repositories / query adapters

IEntitySchemaRepository/IEntitySchemaVersionRepository F02 используют один context/UoW; exact version ownership проверяется. Latest Published content читается по pointer ID в согласованном mutation scope. No query всех version snapshots/instance values для next number. Add/Update не вызывают отдельный SaveChanges/commit.

Fresh failure-classification reader после rollback использует новый context/UoW и no-tracking snapshot current Draft/parent; старый EF tracker нельзя принять за latest facts. Concrete port/result mapping Q15. Read projections F02 для UI отдельно от command storage.

### 7.2. Transactions

Для MVP владелец этих commits — ContentModeling. По [утверждённому MVP consistency guard](../questions/Q20-persistence-and-migration.md#mvp-протокол-consistency-guard) все CM replicas используют одну PostgreSQL database и один стабильный глобальный exclusive transaction-level advisory key. Каждый affected owner scope явно начинает ReadCommitted transaction, затем отдельным statement вызывает pg_advisory_xact_lock на **той же physical connection и transaction**, до любых authoritative domain reads и row locks. После успешного acquire заново загружаются own roots/tokens и current foreign facts через public ports из независимых primary read scopes/connections. Предварительные query/preview/cache/replica facts не разрешают commit; foreign read не enlist-ится в ambient owner UoW, не разделяет SQL transaction и не открывает foreign EF из caller.

Guard удерживается до owner commit/rollback: изменяются только roots своего BC и атомарные own outbox/audit intent/inbox markers, где они нужны. Все invalidating writers, включая creation/removal/reference Copy/orphan paths, Publish/lifecycle/hard delete, setup/reconciliation, source durable acceptance/watermark/pending/apply/retry и F15 open/supersede/repair/resolve, участвуют в этом же ключе. Own Version/If-Match сохраняется: stale own token →412, изменившаяся foreign/preview basis →409 по Q15. Create/Copy из неизменённого source не увеличивает source Version; два serially guarded допустимых создания могут оба завершиться успешно.

Внешний I/O (broker, VR School, MinIO), ожидание пользователя и долгие streams выполняются вне gate. CPU preparation допустима заранее, но exact basis повторно подтверждается внутри gate; иной basis требует отказа или новой подготовки вне gate. Bounded guard wait/lock timeout конфигурируются, поддерживают cancellation и являются availability/technical failure, не domain409; точные значения и transport mapping остаются Q15/Q20. Connection loss прерывает owner transaction: reconnect-and-continue запрещён. Потеря commit ACK даёт unknown outcome; выяснение exact state/исходного operation ID по Q15 предшествует повтору, blind command retry запрещён.

Атомарно фиксируются: same version Status/number/ProfileId+exactBytes+Digest/time/name/key/own Version; schema published pointer/number/own Version. Ошибка любой conditional write/FK/unique/commit откатывает всё; memory prepared result не success. Next number не резервируется вне transaction, поэтому failure не создаёт gap.

Parent observed Version защищает pointer/sequence и metadata snapshot от concurrent Publish/rename/Archive; own Version — content от Save/Delete/повторного Publish. Local mutation guard F02 должен согласовать также current Published lifecycle, schema deletion и dependency cascades. MVP protocol/isolation/acquire order приняты в Q20/§7.2; общий advisory key берётся до authoritative root/baseline reads и любых local row locks. Optimistic Version и short transaction boundary сохраняются; HTTP/диалоги не держат locks.

В MVP параллельные different Draft одной baseline сериализуются advisory key до fresh checks. Первый commit меняет pointer/sequence; второй после acquire видит новый pointer и отклоняется stale-base409, без нового номерного retry. Parent conditional write/unique number остаются storage defence и не отменяют own Version проверку. На parent conflict нельзя просто отправить412: клиент передавал Draft ETag, не parent token.

[UnitOfWorkBehavior](../../../../contentmanager.api/src/BuildingBlocks/ContentManager.Cqrs.UnitOfWork/Behaviors/UnitOfWorkBehavior.cs) вызывает CompleteAsync после возврата handler. Поэтому catch только внутри Publish handler не перехватывает commit race. Нужна classification boundary после failed CompleteAsync/disposal rollback и до Api error mapping; область App/CQRS composition с local ContentModeling reader, конкретная интеграция Q15. Не добавлять foreign BC transaction и universal publish retry middleware.

Classification выполняется read-only в fresh scope:

1. Own Draft Version изменился относительно ExpectedVersion →412; эта категория отделена от pointer race. Missing exact owner/resource →404.
2. Own Version прежний, но current pointer != original Draft baseline →409 schema-draft.base-outdated; loser всё ещё Draft без number/hash/PublishedAt.
3. Pointer прежний, parent metadata/lifecycle изменились → предметный state/concurrency conflict, profile/code Q15; новый snapshot не принимается автоматически.
4. Constraint/storage failure без доказанной причины не маскируется baseline или ETag conflict; диагностируется соответствующая persistence ошибка Q15.

Если several facts изменились, приоритет own-token conflict следует исходному F03; failure reader обязан сохранить доказательство initial expected/baseline. Internal read после rollback классифицирует результат, не запускает повторную публикацию и не гарантирует frozen future state. Если selected resource уже Published вследствие другого запроса, старый ETag даёт412, актуальный ETag с повторным Publish — invalid-state409. Lost successful response не равен rollback.

### 7.3. Migrations

Совместно F01/F02: integer nullable version number вместо major/minor, own version concurrency owner, published pointer/sequence, immutable metadata/text/hash/time, unique/check/FK constraints; remove one-Draft. Новая physical model/clean DB проверка Q20. Production compatibility/legacy migration не проектируется без запроса: CM ещё не запущен, destructive rebuild разрешён Q20. Аналитика не выполняет migrations/DB writes.

Local Demo compatibility: `20261008094758_F03ScopeLegacySchemaNumbers` scopes the raw `ux_schema_version_number(schema_id, version_major, version_minor)` index from `EditorLifecycle` to `content_hash_profile IS NULL`. Modern Published integer numbering remains protected by `ux_schema_published_version_number`; an unrelated legacy Draft `1.0` therefore cannot block modern publication number `1`. The migration does not update, relabel or renumber rows and leaves the previously applied canonical-storage migration intact. Its Down checks for mixed semver collisions before restoring the unfiltered index and refuses the downgrade rather than modifying valid history. Real PostgreSQL tests verify modern/legacy coexistence, continued duplicate legacy protection, safe Down/Up and refusal that preserves rows, index and migration history.

## 8. External adapters

Pure common .NET canonicalization library + typed neutral models §3.3, Domain/EF/AspNetCore independent, registered server/SDK consistently by Host. CPU normalization/byte emission/hash preparation outside gate from preliminary non-authoritative input; final own guard reloads exact draft/current baseline/owner and prepared semantic basis before commit. Drift discards preparation, preserves own412/independent409; no serializer work under long-lived stream/foreign UoW.

Только deterministic local canonical/hash implementations и TimeProvider. Canonicalizer/hash ports принадлежат Application.Abstractions; placement конкретной реализации среди existing local infrastructure projects — Q19/Q15. Domain хранит result VO, не выбирает serializer/algorithm. Нет S3/broker/VRSchool/Delivery dependency, codegen ZIP не строится при Publish. Six v1 profiles/digest/escaping/numbers selected; common .NET library/owner mappers/storage verify literal vectors before runtime compatibility claim.

## 9. Data flows

MVP порядок для DF01: input/CPU preparation вне gate → явная owner ReadCommitted transaction → standalone exclusive advisory acquire → fresh own roots и foreign primary-port facts → проверки own token/basis/lifecycle/dependencies → Domain action → только own writes/atomic intents → commit и release. Это обязательный префикс каждого указанного mutation/retry stage; строки «load/guard» ниже означают загрузку **после** acquire. Read-only query/impact/preview остаются observations; при необходимости короткий source capture имеет отдельный guarded scope, но долгий stream и пользовательский review не удерживают guard. Demo использует свой согласованный контур без новых обязательных MVP dependencies.

### DF01. Publish exact Draft

| Шаг | Исполнитель | Чтение / преобразование | Проверка | Запись / выход |
| --- | --- | --- | --- | --- |
| 1 | Api | Bind SchemaId/VersionId, parse If-Match → internal command, pass cancellation | Required/valid exact token; no body-assigned publication fields | Command либо428/parser error profile Q15 |
| 2 | Application/Persistence | MVP: explicit ReadCommitted owner transaction; standalone pg_advisory_xact_lock на owner connection; затем fresh exact schema+selected version | Ownership/existence; expected own Version; Draft; parent authoring availability | Loaded roots/observed parent Version;404/412/409 при отказе |
| 3 | Application | Read current Published pointer, compare candidate baseline; load exact baseline content если есть | Null==null первая publication; stale mismatch409; baseline ownership/status/lifecycle profile Q15 | Immutable evolution input без fallback/latest-created lookup |
| 4 | Domain policy | Recursive structure/evolution validation against exact current; identity facts при нужном Q15 profile | All §2 invariants; complete issue locations | Issues → failure/rollback; valid snapshot неизменён |
| 5 | Application/adapters | Fresh-check outside-gate prepared semantic input/profile/bytes/digest; allocate next integer, get PublishedAt/frozen parent Name/Key | Checked number arithmetic; complete deterministic result Q19 | SchemaPublishParameters; ничего durable ещё нет |
| 6 | Domain | version.Publish(parameters); schema.RegisterPublishedVersion(version.Id, number) | Preconditions still same agreed snapshot; no arbitrary next-number allocation | Prepared two-root changes/own increments, same version identity |
| 7 | Persistence/UoW | Conditional root writes + unique/FK guards, один commit | Own/parent Version, current facts guard; any failure rollback | Durable Published+pointer together; loser fresh classification §7.2 |
| 8 | Application/Api | F02 mapper prepared Published DTO + internal resulting Version, release only after commit | Никакого silent retry/rebase/hidden GET |200 DTO/new version ETag; no external side effects |

Cancellation до commit откатывает scope; после commit потерянный/отменённый response оставляет durable result. UI выясняет outcome exact read version ID/owner, не auto повторяет POST. Если Version стал Published, подтверждение показывается из прочитанного state; как отличить собственный attempt от чужого и нужен ли idempotency contract — Q15.

Остальные Draft не обновляются массово: новый pointer делает их baseline stale на следующем read. F02 Accept использует ту же strict policy, но не hash/Publish/allocate number. Historical Registry F21 использует frozen exact snapshot, не текущие logical Name/Key или новый serializer.

MVP DF01 дополнительно повторяет DefinitionConstraintsPolicy по loaded resulting snapshot перед выделением номера/canonical preparation. Unsupported profile либо invalid constraints/comparer — failure до transition/commit, без consumed number/partial pointer. Новые strict issues проходят existing machine-code/address contract Q15; неизвестные exact codes не угадываются.

## 10. Ошибки и диагностика

MVP guarded failure различает confirmed business refusal и невозможность проверки: unavailable/incomplete public facts не означают empty/valid facts. Guard wait cancellation/timeout и connection loss — technical/availability outcome без domain Conflict; точные wire codes остаются Q15. После connection loss scope завершается rollback/disposal без продолжения на новой connection. Unknown commit outcome выясняется по exact state/operation ID; prepared tracker не подтверждает ни success, ни rollback. Own stale ETag сохраняет412, foreign basis change —409. Диагностика фиксирует wait/hold duration, owner/operation, acquire/commit/rollback/unknown phase без full payload и секретов.

| Условие | Transport / recovery | Данные |
| --- | --- | --- |
| Missing/foreign IDs |404, без fallback | Без changes |
| Own ETag missing/stale |428/412, explicit Reload, no retry | Текущая команда не перезаписывает saved state |
| Baseline != latest |409 schema-draft.base-outdated, UI F02 stale resolution | Draft прежний, number не назначен |
| Not Draft / invalid lifecycle |409 schema-version.invalid-state либо specific lifecycle profile Q15 | Published/history unchanged |
| Strict issues | Structural400/evolution409 distinction и paths Q15 | All-or-nothing; no number/hash/time |
| Parent facts/constraint race | Rollback, fresh classification §7.2 | Не маскируется generic412; no new-number retry |
| Canonical/hash implementation failure | Internal failure mapping Q15, no false validation/conflict | Whole rollback; no partial published metadata |
| Successful commit / response loss | Unknown outcome, exact read/manual recovery | Durable result/history сохраняются |

Логируются correlation/operation/UoW ID, SchemaId/VersionId, expected/internal conflict category, original base/current pointer, assigned number/canonical profile identifier, duration/outcome. Полный content/secret/stack trace клиенту не выдаются. Success number логируется как committed, не prepared. Counters publish success/stale/ETag/validation/commit failure допустимы без domain event лишь для telemetry.

## 11. Тестирование

Post-Demo delivery policy: Unit/real PostgreSQL/Functional acceptance включает nested definitions/default/explicit modes, невозможность Published mutation и отсутствие подмены mode одной exact версии mode новой. Literal old/new fixtures раздельны; старые `/1` bytes/hash неизменны. Future checks после Q37 не входят в отчёт прошедших Demo тестов.

Canonical conformance [literal vectors](../canonical-v1-vectors.md): literal input/expected bytes/digest independent of production helper, all15 types + Int64 extrema/Decimal scale/Float RFC AppendixB, Unicode/controls/invalidduplicate/trailinggarbage, absent/null/default, FieldID-vs-Key order, Enum/Flag sets/nested List order/excluded metadata/profile mismatch/corrupt stored bytes. Owner PostgreSQL atomic triple/pointer rollback and SDK unknownprofile-beforeapply/no-autorepair required; fixtures not executed proof.

Target стратегия по [S04](../standards/S04-testing.md), не результат запуска. Unit SUT — Domain transitions/strict policy/canonical mapping/handler orchestration; controlled TimeProvider и ports doubles допустимы без имитации реальной DB race. Integration SUT — actual EF/UoW/constraints/guard, PostgreSQL Testcontainers. Functional — WebApplicationFactory API с real PostgreSQL, Demo anonymous без JWT. Architecture — references/physical placement. Один API с BC не E2E; Registry/UI реальный контур проверяется своими фичами.

Local storage verification (08.10.2026): `F03PublicationMappingTests` reproduced the missing byte mapping before implementation and passes with field access and a matching EF snapshot. Seven `F03PublicationPersistenceTests` pass on isolated real PostgreSQL: migration retains null legacy profiles, independent roots atomically persist the exact profile/bytes/hash and frozen metadata, a failure after both roots flush rolls back all publication fields without consuming a number, SQL constraints reject duplicate/zero numbers and incomplete or mismatched new-profile storage, and the legacy index supports modern coexistence, retained legacy uniqueness and safe migration reversal. The legacy collision regression reproduced PostgreSQL `23505` before the second migration. The complete ContentModeling Integration suite passed **44/44**, including these eight tests and the existing F01/F02/legacy persistence scenarios. This storage evidence does not establish the full target HTTP/UI/Registry/MVP matrix below.

Тестовые owner/version IDs и data собственные, separate DB/fixture cleanup без чужих records. Concurrent tests отдельными contexts/connections/barriers, no Sleep. Обе branches доводятся до commit/rollback, classification после CompleteAsync проверяется через реальный pipeline. Timestamps с PostgreSQL precision. Canonical golden fixtures approved Q19 должны быть literal input/expected bytes/hash, а не значения, вычисленные тем же production serializer в expected.

| ID / поток | Уровень / SUT | Данные / воздействие | Наблюдаемый результат |
| --- | --- | --- | --- |
| T01 / DF01 | Unit + Integration transitions | First Publish null base/pointer; subsequent valid base | Numbers1,2... only commit, own and parent tokens advance once, no Id/field ID change |
| T02 / DF01 | Unit + Functional strict policy | All15 types/Object/List<List>/unique whole-tree keys, invalid empty/variant | Valid full tree accepted; invalid addressed issues, no writes |
| T03 / DF01 | Unit policy + Functional | Same FieldId changed Key/Type/Item shape/inverse Required/Deprecated; option removal/rename | Strict reject; labels/add options/removal field allowed; identity-history corner Q15 |
| T04 / DF01 | Integration + Functional | Two different Draft same baseline, barrier before commit | Exactly one Published/next number; loser Draft409 stale, no auto retry/new number |
| T05 / DF01 | Integration + Functional | Save/Delete/same Draft Publish racing Publish | Own token protection412/missing404; no torn tree/metadata or double Publish |
| T06 / DF01 | Integration guard/commit boundary | Parent rename/Archive, baseline lifecycle change against Publish | Согласованный metadata snapshot/lifecycle eligibility; failed conflict not generic412; profile Q15 |
| T07 / DF01 | Integration UoW | Second root write/constraint/hash failure after prepared first change | Whole rollback, no partial number/hash/time/pointer; next valid publish does not skip number |
| T08 / DF01 | Integration constraints | Duplicate Published number, inconsistent Draft/Published fields/FK | Real DB constraints protect state; constraint-specific mapping, no broad23505→412 |
| T09 / DF01 | Functional actual API/OpenAPI | Missing/stale/current ETag, no body, correct response |428/412 vs409, bodyless contract, result full Published DTO/new own ETag; no public technical token |
| T10 / DF01 | Integration + Functional | Read old Published; rename parent/new Publish/Retire | Frozen content/hash/number/time/name/key unchanged; latest pointer separate; no history delete |
| T11 / DF01 | Unit adapter + Integration storage | Literal Q19 vectors, reordered semantic fields/options, changed Name/HelpText/option labels/Title/comment/publication metadata | Semantic hash unchanged by excluded authoring metadata; exact saved canonical bytes/hash reproducible; Registry representation ETag independently reflects its representation |
| T12 / DF01 | Functional composition/Architecture | Demo no auth/moderation/broker/foreign context, layer references | Publish works in required minimal contour; command Application/query Persistence/Domain no EF/HTTP |
| T13 / DF01 | Functional + UI/backend contour | Commit then lost response, explicit exact recovery | Durable exact Published discoverable; no automatic duplicate publication/false rollback |

Current source tests проверяют прежний major/minor/Revision contract; их наличие не доказывает target guards. После Q19/Q15 ответы становятся literal expected profiles, не выбираются тестами молча. UI feedback/paths/dirty handling проверяет frontend F03; atomic DB effects — backend Integration/Functional.

MVP Integration matrix для DF01 использует real PostgreSQL, отдельные owner connections/UoWs и barriers, включая разные CM replica processes/hosts там, где проверяется общность key. Doubles/in-memory locks не доказывают SQL coordination.

| MVP сценарий | SUT / воздействие | Наблюдаемый результат |
| --- | --- | --- |
| Guard до чтений | Реальный Persistence adapter; writer A держит gate, B начинает owner transaction до acquire; A меняет eligibility/basis и commit, B продолжает | B после acquire читает новый committed state при ReadCommitted; stale предварительные facts не применяются; lock statement использует owner connection/transaction |
| Меж-BC write/read partition | Public read adapter и Host/DI; owner держит guard, foreign primary read отдельным scope | Read не enlist-ится в owner transaction, не читает cache/replica; no foreign writes/EF access; atomic commit только owner + intents |
| Commit/rollback/release | Barriers для invalidating writer и affected operation; injected owner failure/cancellation | Второй writer ждёт release и revalidates; failed owner полностью rollback, нет partial roots/intents; unrelated next scope получает key |
| Wait и connection failure | Configured bounded wait/cancellation, разрыв owner physical connection | Technical/unavailable, не409/empty success; aborted transaction не продолжается через reconnect |
| Unknown commit ACK | Разрыв response/commit acknowledgement и inquiry exact state/original operation ID | Committed result сохраняется, rollback не предполагается; no blind duplicate mutation |
| Publish basis и own token | Барьер: different Draft same baseline; own Save либо parent Publish/lifecycle invalidation | Fresh post-acquire baseline даёт одному Published, другому409; own stale token412; atomic number/pointer/history |

MVP matrix: constraint tightening/loosening остаётся same FieldId, missing/optional/unsupported comparator definition blocks Publish, comparer path in nested Object, strict repeat checks, atomic snapshot/profile/bytes/digest failure. Golden old /1 fixtures неизменны; new-profile rules bytes покрываются literal fixtures отдельно до release. SUT pure policy/canonical mapper, PostgreSQL transaction and Api Functional; не проверять все Instance values из schema Publish.

Temporal Publish fixtures: immutable semantic identity; Offset bound min/max instant inclusivity/equivalent offset bounds same new-profile hash; opposite offsets date rollover; authoring bound snapshot exact but canonical UTC; old /1 vectors unchanged/Offset excluded. New-profile schema runtime corpus включает constraints и TemporalSemantics одновременно, не скрытая byte-changing /1 bugfix.

## 12. Порядок реализации

Для delivery policy после Demo согласовать Q37 integrity/profile и identity до canonical mapper/EF/API; сохранить полный authored mode в Published snapshot и подтвердить literal old/new fixtures отдельно.

1. Implement accepted Q19 schema canonical v1 profile/common typed library/literal vectors, Q15 strict errors/history semantics/commit boundary, Q20 physical storage. Другие content profiles реализуются их владельцами F04/F10/F13; MVP guard — в своём контуре.
2. Reuse F01/F02 roots/constraints; strict policy и Publish/Register methods с Unit invariants.
3. DTO/command/ports; canonical/hash implementations, deterministic fixtures, mapper/validators/handler.
4. EF mappings/unique checks/two-root UoW/guard/fresh failure classification; реальные PostgreSQL race/rollback tests.
5. Api bodyless contract/own ETag/errors/OpenAPI; HTTP Functional verification; NSwag regeneration.
6. Frontend action/diagnostics/Published result в F02 workspace; общая identity/evolution policy для F02 Accept; strict Publish completeness не переносится на Accept.

## 13. Открытые технические вопросы

[Q37](../../frontend/questions/Q37-launcher-asset-delivery.md): delivery metadata integrity/profile, serialization и same-FieldId evolution. Immutable schema policy согласована; отсутствие выбранного codec не разрешает использовать старый `/1` для новых bytes.

[Q35](../../../business/questions/Q35-value-constraints.md) semantics первого MVP набора согласованы. Exact neutral new-profile model/bytes/fixtures остаются Q19/Q31; Offset semantics согласованы Q34, runtime/library codec implementation и whole new-profile fixtures отдельно Q19/Q31. Ни один из них не изменяет frozen /1 или расширяет Demo.


| Вопрос | Что остаётся |
| --- | --- |
| [Q19: профиль F03](../questions/Q19-content-hashes.md#f03-профиль-схемы-и-эталонные-векторы) | Demo /1 writer/storage/literal verification реализованы; exact новые constraints/Offset profiles и SDK/Unity integration остаются своим этапам |
| [Q15: детализация F03](../questions/Q15-api-and-source-code.md#f03-детализация-целевого-контракта) | Demo issues/paths/header/parser/body/outcome материализованы; расширенная lifecycle baseline eligibility и foreign-fact post-rollback classification — MVP |
| [Q20](../questions/Q20-persistence-and-migration.md) | Shared MVP guard принят; constraints/index names, owner UoW/read-scope partition implementation, physical migration |
| [Q34: историческая identity](../../../business/questions/Q34-schema-field-semantics.md#f03-возвращение-исторического-поля-и-положение-в-дереве) | Same identity/context/parent-chain и monotonic history проверяются Demo; новые MVP constraints/temporal fixtures — Q19/Q31 |

Multiple Draft, integer publication numbering, no silent retry/rebase, immutable history и own ETag уже приняты; они не становятся открытыми заново из-за неполного current source.
