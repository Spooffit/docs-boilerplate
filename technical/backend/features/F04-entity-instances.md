# F04. Экземпляры сущностей и версии данных: бэкенд


[Бизнесовая спецификация](../../../business/features/F04-entity-instances.md) · [Модуль M01](../modules/M01-content-authoring.md) · [Фронтенд](../../frontend/features/F04-entity-instances.md) · [F02](F02-create-schema-version.md) · [F03](F03-publish-schema-version.md) · [F05](F05-media-assets.md) · [Q34](../../../business/questions/Q34-schema-field-semantics.md) · [Q36](../../../business/questions/Q36-entity-instance-lifecycle.md) · [Q15](../questions/Q15-api-and-source-code.md)


## 1. Область реализации


F04 создаёт логический EntityInstance и начальный Draft, читает списки и рабочую область, создаёт чистый Draft или копию по точной SchemaVersion, сохраняет значения и orphan, актуализирует основу по тому же target и публикует immutable версию данных. Переименование логического экземпляра выполняется отдельно от Save версии. Фича также выполняет допустимое физическое удаление. MVP добавляет Archive/Restore логического экземпляра и Retire/Restore Published версии; withdrawal прекращает прямую Demo-выдачу через F13, сохраняя историю.


Один экземпляр принадлежит одной EntitySchema, но может иметь версии по разным SchemaVersionId и несколько Draft по каждой. Бизнесовые номера образуют одну последовательность экземпляра; устаревание основы определяется отдельно по target SchemaVersion. Копирование сопоставляет только stable FieldId, без эвристик и автоматического merge. Загрузка media относится к F05, доставка и синхронизация — к F13, продуктовые назначения, releases и moderation — к Publishing. Demo не включает авторизацию, шину и полный Publishing; доступ MVP связан с Specialist F16/Q10, конкретное permission уточняется.


Документ разделяет реализованный серверный профиль Demo и последующий MVP. Конкретные типы текущего checkout определены в исходниках ниже; целевые обозначения в остальных таблицах не утверждают готовность MVP lifecycle, Publishing или Delivery.

| Источник | Реализованный Demo профиль | Граница следующего этапа |
| --- | --- | --- |
| [Instance controller](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Api/EntityInstances/EntityInstanceController.cs), [version controller](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Api/EntityInstances/EntityInstanceVersionController.cs) | Separate logical/version strong ETag, exact reads, full mutation results, copy/accept/delete-impact/withdraw/return; Save POST | Archive/Restore и version Retire/Restore остаются MVP |
| [InstanceAuthoringCommandHandlers](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application/EntityInstances/CommandHandlers/InstanceAuthoringCommandHandlers.cs) | Draft/Published exact target одной схемы, multiple Draft, stable-ID copy/orphans, own-token Save, global Publish sequence, separate logical Name | Нет вызова исключённого Demo Publishing; его historical authority относится к MVP |
| [EntityInstance](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Domain/EntityInstances/EntityInstance.cs), [EntityInstanceVersion](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Domain/EntityInstances/Entities/EntityInstanceVersion.cs) | Independent modern roots, own int Version, unnumbered Draft, target baseline, frozen Published; legacy factories сохранены для исторических данных | Legacy lifetime/one-Draft facade не является modern authoring policy; CreateNextDraftVersion требует loaded legacy history и отклоняет modern versions |
| [InstanceAuthoringContent](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Domain/EntityInstances/ValueObjects/InstanceAuthoringContent.cs), [InstanceDraftReconciler](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Domain/EntityInstances/ValueObjects/InstanceDraftReconciler.cs), [InstanceValueValidator](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Domain/EntityInstances/ValueObjects/InstanceValueValidator.cs) | Recursive Scalar/Object/List, exact scalar literals, stable ItemKey, saved provenance/orphan work, pure Draft/strict checks | Offset и Q35 advanced constraints не входят в Demo |
| [InstanceAuthoringQueryHandlers](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.EntityFramework.Persistence/EntityInstances/QueryHandlers/InstanceAuthoringQueryHandlers.cs), [instance mapping](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.EntityFramework/Configurations/EntityInstanceConfiguration.cs), [version mapping](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.EntityFramework/Configurations/EntityInstanceVersionConfiguration.cs) | Lightweight summaries, owned local snapshots и Application details projector, independent exact reads/basis, JSONB authoring/provenance/orphans, exact bytea canonical bytes, separate tokens, no one-Draft index | GET не пишет; foreign persistence не становится частью owner commit |
| [MediaCaptureContracts](../../../../contentmanager.api/src/BoundedContexts/MediaLibrary/ContentManager.MediaLibrary.Application.Abstractions/MediaCaptureContracts.cs), [MediaCapture](../../../../contentmanager.api/src/BoundedContexts/MediaLibrary/ContentManager.MediaLibrary.EntityFramework.Persistence/Storage/MediaCapture.cs) | Public batch capture/primary read/shared AssetId guard; frozen descriptor из complete verified Available metadata | Legacy [ContentReferenceGuard](../../../../contentmanager.api/src/App/ContentManager.Application/Publishing/ContentReferenceGuard.cs) сохраняется для прежнего Publishing; новый F04 его не вызывает |

### 1.1. Конкретный серверный профиль Demo

**Domain / canonical library.** Logical owner хранит immutable SchemaId/Key, mutable Name, latest global Published pointer/number и WithdrawnAt. Version хранит own Version, immutable owner/target, nullable PublishedVersionNumber и BasePublishedInstanceVersionId, ModernContent, SavedSchemaContent и OrphanValues. Named values адресуются FieldId; anonymous List items не получают fake FieldId. `InstanceScalarValue.ScalarJson` сохраняет JSON literal без преобразования в browser double: Integer Int64, Decimal, Naive DateTime с семью знаками, Date/TimeSpan, option keys, FlagEnum arrays и другие scalars сохраняют свои semantics. Полный runtime snapshot строят [InstanceRuntimeCanonicalMapper](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Domain/EntityInstances/ValueObjects/InstanceRuntimeCanonicalMapper.cs) и [InstanceContentCanonicalizer](../../../../contentmanager.api/src/BuildingBlocks/ContentManager.Canonicalization/InstanceContentCanonicalizer.cs) по `cm-instance-content/1`; canonical profile/owned byte buffer/digest сохраняются вместе. Авторские labels, orphan IDs и configured storage URLs в runtime не входят.

**Application.Abstractions / transport.** Concrete [requests](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application.Abstractions/EntityInstances/Models/InstanceAuthoringRequests.cs) и [DTO](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application.Abstractions/EntityInstances/Models/InstanceAuthoringDtos.cs) задают `Content.Fields[{NamedFieldId,Value}]`; Value содержит `Kind=Scalar|Object|List` и ровно соответствующий ScalarJson, Fields либо Items. Object item имеет ItemKey, scalar/anonymous List item — нет. Absent named field означает отсутствие значения. ScalarJson с literal `null` — отдельное явное значение, не omission/default; ни один из 15 Demo типов не допускает новый nullable input, поэтому такой new input отклоняется intrinsic validation. Сохранённый raw null в legacy/orphan work не coerces-ится в отсутствие/default и не подвергается скрытой conversion. Unknown request members и неоднозначные variants отклоняются.

CreateInstanceRequest содержит OperationId, SchemaId, TargetSchemaVersionId, Key, Name и Content. Create возвращает полный initial InstanceVersionDetailsDto с ETag этой версии; logical exact read отдельно предоставляет InstanceDetailsDto и logical ETag. Clean/copy request содержит OperationId/TargetSchemaVersionId; source copy защищён own If-Match. Operation receipt сохраняется с roots в owner commit и возвращает исходный result при том же normalized input; иной input с тем же ID — conflict.

SaveInstanceDraftRequest содержит полный Content, DiagnosticsBasisToken, DiscardOrphanValueIds и ComputedDiscardTokens. Сохранённые orphans имеют server Guid; вычисленные incompatibilities имеют basis-bound opaque token, не случайный persisted ID. AcceptInstanceBaseRequest содержит явно переданный nullable PublishedInstanceVersionId и DiagnosticsBasisToken; Accept не меняет payload/orphans. Exact version details возвращает TargetSchemaContent, saved values, current same-target baseline, IsStale, TextMaxLength, Issues и orphan projection. Для IsLegacyContent возвращаются readonly LegacyFields с исходными FieldId/ItemKey/raw ValueJson/Children и LegacySchemaFields; Content остаётся null, modern Save/Copy/Publish не выполняют скрытую conversion. Logical details/summary могут показывать SchemaName как caption. Own ETag относится к saved root; GET no-store и отдельный diagnostics basis исключают ложное подтверждение свежести изменившихся schema/media/base facts. Basis включает relevant saved version state, exact target content/Version, same-target baseline, withdrawal/lifecycle/media facts и Text maximum; unrelated logical instance/schema Rename и их generic owner Version не инвалидируют untouched Draft.

**Frozen asset read projection (docs 414b725).** Exact modern Published result теперь содержит `PublishedAssetReferences`: owned `PublishedInstanceAssetReferenceDto` с `AssetId`, `AssetKind`, `ContentType`, `SizeBytes`, `Sha256Hash`. [Projection](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application/EntityInstances/Mappers/PublishedInstanceAssetReferenceProjection.cs) проверяет сохранённые CanonicalBytes/Profile/Hash против SavedSchemaContent и рекурсивно читает только AssetReference definitions. Она не использует current MediaLibrary metadata или URL; одинаковые facts одного AssetId дедуплицируются, противоречивые либо повреждённый snapshot отклоняются. Draft/legacy не получают fabricated descriptors. Read DTO не меняет canonical `/1`, persisted authoring values или own Version. Live availability diagnostics и current F05 download остаются отдельными от этих frozen facts.

**Application details projection.** [InstanceDetailsProjector](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application/EntityInstances/Mappers/InstanceDetailsProjector.cs) владеет foreign media I/O через public IMediaAssetReader и собирает exact details с pure InstanceAuthoringMapper. Persistence загружает только materialized ContentModeling owner/version/schema/history snapshots и делегирует проектору; foreign MediaLibrary reads не находятся в query storage handler и не enlist-ятся в owner UoW.

**Application / Api.** [Handlers](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application/EntityInstances/CommandHandlers/InstanceAuthoringCommandHandlers.cs) выполняют authoring, validation/reconciliation, capture и owner mutations; запросы находятся в Persistence. Save использует `POST /api/v1/entity-instances/{id}/versions/{versionId}/save`, metadata rename — `PUT /api/v1/entity-instances/{id}/metadata`, Return — bodyless `POST /api/v1/entity-instances/{id}/return`. Publish/Withdraw также bodyless. Mutable success — полный committed DTO и new own ETag без hidden GET; Delete — Guid. Missing required If-Match 428, own mismatch 412, independent basis/lifecycle/dependency 409, malformed values 400. Lists используют SkipCount/MaxResultCount, default20/max200, stable Id tie-breaker; instance filters SchemaId/Search, version filters Status/TargetSchemaVersionId. Typed InstanceValueRejectedException передаёт Code/Path/Message в ValidationProblemDetails без замены всех value errors общим Content: named addresses /fields/{FieldId}, keyed Object item /items/@{escaped ItemKey}, anonymous scalar/List item /items/{index}. Эти paths совпадают с Issue/orphan location и позволяют UI раскрыть нужный nested control.

Key trim сохраняет регистр, format `[a-z][a-zA-Z0-9]*`, length1..50, uniqueness Ordinal внутри SchemaId. EF `RestoreExact` сохраняет также прежние lexical keys с underscore и не вызывает authoring Camelize. Restricted HTML Text санитизируется сервером; positive setting `ContentModeling:InstanceAuthoring:TextMaxLength`, default32768, выдаётся через `GET /api/v1/entity-instances/authoring-settings` и exact version details. Настройка регистрируется и проверяется при startup в [ContentManagerApplicationModule](../../../../contentmanager.api/src/App/ContentManager.Application/ContentManagerApplicationModule.cs).

**EntityFramework / Persistence.** [IEntityInstanceAuthoringRepository](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Domain/EntityInstances/Repositories/IEntityInstanceAuthoringRepository.cs) и [adapter](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.EntityFramework.Persistence/EntityInstances/Repositories/EntityInstanceAuthoringRepository.cs) используют только ContentModelingDbContext. Shared F02 schema transaction gate берётся до fresh tracked facts; Publish same-target baseline и global number проверяются под этим gate до одного owner commit. Publish и Return с retained Published history дополнительно отвергают Withdrawn/Retired exact target и требуют Published status; Return без retained Published выполняет только recovery собственного запрета; legacy next-draft facade не вычисляет sequence по unloaded history. Number allocation не отдельный commit. [F04 migration](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.EntityFramework/Migrations/20261008102838_F04InstanceAuthoring.cs) сохраняет old content/version_number/hash/published JSON/status, добавляет modern fields, снимает one-Draft index и ограничивает old ordinal uniqueness legacy rows. Existing Published/Archived ordinal переносится в derived PublishedVersionNumber/global pointer, Draft ordinal не резервирует новую sequence. Legacy bytes не получают новый canonical profile и не rehash-ятся; inconsistent retained sequence блокирует новый Publish с explicit conflict. Downgrade, теряющий modern rows/multiple Draft, отклоняется.

Content/orphans/schema provenance — JSONB converters/comparers; canonical runtime — bytea + exact decoded text/profile/hash. F02 schema-delete consent включает independent draft Version, modern/raw values, orphans и provenance, поэтому orphan-only edit тоже требует нового confirmation. [SchemaDraftCommandHandlers.ConfirmDelete](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application/EntitySchemas/CommandHandlers/SchemaDraftCommandHandlers.cs) под schema gate проверяет exact consent/cascade-set, собирает AssetIds всех зависимых modern Draft из saved values/orphan definitions и известных legacy refs из target schema, затем держит тот же public media guard до owner commit/disposal. Только после acquisition выполняется explicit removal; implicit DB cascade не вводится. Последний удалённый instance Draft оставляет logical owner. Saved schemas/definitions в provenance обеспечивают распознавание media usage даже после удаления поля.

**Минимальная media prerequisite.** Public IMediaAssetReader читает materialized primary facts в независимом scope через own MediaLibrary context; не enlist-ится в ContentModeling UoW. IMediaAssetGuard использует singleton registry и scoped owner facade; distinct AssetIds берутся в стабильном порядке, no-write participant держит lease до owner UoW disposal после commit/rollback. IMediaAssetCapture требует Available, совместимый VerifiedKind и все пять authoritative descriptor fields. Missing/unverified/incompatible metadata возвращает typed failure; сохранённая unavailable work остаётся authoring recovery с diagnostics. Это Demo one-process protocol, не MVP global SQL guard.

[MediaFileInspector](../../../../contentmanager.api/src/BoundedContexts/MediaLibrary/ContentManager.MediaLibrary.EntityFramework.Persistence/Storage/MediaFileInspector.cs) получает реальные upload bytes: PNG chunks/CRC, JPEG frame/scan markers, GIF structure, WAV fmt/data, MP3 frames, MP4 track handler+mdat. Client MIME/filename не назначают Kind; MP4 brand alone недостаточен. Unsupported/legacy files остаются с null VerifiedKind и не используются для F04 capture. Исторический Media list snapshot 08.10.2026 (прежний MediaAssetController.cs; текущий [AssetController](../../../../contentmanager.api/src/BoundedContexts/MediaLibrary/ContentManager.MediaLibrary.Api/AssetController.cs) использует `/api/v1/assets`) `GET /api/v1/media-assets` принимает search/kind/skipCount/maxResultCount и возвращает только Available verified metadata; MediaAssetDto дополнен nullable Kind/Status. Это не полный F05 policy/catalog/delete/YARP contour и не замена private MinIO provider; transcoding и фиктивные descriptors отсутствуют.

**Проверка реализованного Demo.** CM Unit — 564/564; полный CM Functional/OpenAPI — 84/84, после последних guards повторно F04 + legacy HTTP — 15/15; полный CM PostgreSQL Persistence — 48/48; App Integration F04 + Publishing/Archive/Delivery — 18/18; MediaLibrary F04 — 4/4; App/ContentModeling/BuildingBlocks Architecture — 44/44, 4/4 и 3/3. Реальные PostgreSQL tests подтверждают сохранение legacy rows и frozen representations, independent own tokens, concurrent Publish numbering/baselines, verified media capture/primary reads и удержание AssetId lease до owner commit, включая logical Delete legacy Draft. Configured NSwag generation прошла против API под Windows user; generated client вручную не изменялся. Полная UI solution build и F01–F04 console harness прошли. Browser подтверждает список, SC03 create с initial Int64.MaxValue, clean Draft, blank numeric guard, явный 0 Save, Published #2, Rename после Publish и readonly legacy values; финальный legacy Copy guard проверен renderer regression. API/UI остановлены, порты 7107/7108/9001 закрыты. Это проверка Demo authoring; полноценные F05/F13, MVP lifecycle и Unity runtime не заявляются проверенными.

## 2. Domain


### 2.1. Aggregates, entities и value objects


Все модели ниже принадлежат ContentModeling. Определения полей и типов переиспользуются из F01/F02, но значения экземпляра не копируют metadata схемы. Современные roots отделены; прежняя child-version facade сохранена только для совместимости исторических данных. Concrete Demo profile указан в §1.1; MVP extensions ниже остаются отдельной областью.


Обязательность в таблице относится к полю соответствующей модели. Для серверного состояния и result она не означает требование передать поле клиентом. `Условно` раскрывает lifecycle или variant; неизвестный wire omission/default остаётся `Уточняется`.


| Тип | Роль | Владелец / граница |
| --- | --- | --- |
| EntityInstance | Aggregate Root | Logical identity/current label/global sequence/withdrawal; SchemaId/Key immutable |
| EntityInstanceVersion | Aggregate Root | Exact Draft mutable; target immutable; Published snapshot immutable; lifecycle mutable |
| EntityInstanceId / EntityInstanceVersionId | Typed identities | Серверные factories; Publish ID не меняет |
| EntityInstanceKey / EntityInstanceName | Value Objects | Key unique per schema; Name authoring label; limits Q15/Q36 |
| EntityInstanceContent | Value Object | Full snapshot immutable-by-replacement; no inheritance overlay |
| InstanceFieldValue | Named value node | Field identity в object occurrence; no schema metadata copy |
| InstanceValue | Recursive value | Logical union; exact variant Q15; anonymous item без fakeFieldId |
| InstanceListItem | Snapshot item | Scalar без Guid; keyed Object item; authored order retained |
| OrphanValue | Authoring-only record | Stable server ID for discard; не runtime payload |
| InstanceDraftReconciliation | Domain/application parameter result | Pure in-memory conversion до replacement; не persistence owner |


**`EntityInstance`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Id` | `EntityInstanceId` | Да | Server logical identity | Stable при Publish |
| `SchemaId` | `EntitySchemaId` | Да | Logical schema owner | Immutable |
| `Key` | `EntityInstanceKey` | Да | Logical key | Unique per SchemaId; immutable |
| `Name` | `EntityInstanceName` | Да | Current authoring label | Mutable; не runtime field |
| `Lifecycle` | Уточняется | Да | Logical lifecycle | Active/Archived; create Active; CLR type [Q15](../questions/Q15-api-and-source-code.md) |
| `LatestPublishedVersionId` | `EntityInstanceVersionId?` | Условно | Global Published pointer | Null до первой публикации; не same-target baseline |
| `LatestPublishedVersionNumber` | `int?` | Условно | Global Published sequence | Null до первой публикации; unique positive sequence |
| `WithdrawnAt` | `DateTimeOffset?` | Условно | Server withdrawal time | Create null; logical scope всех targets; возврат [Q36](../../../business/questions/Q36-entity-instance-lifecycle.md) |
| `Version` | `int` | Да | Internal logical token | Серверный; не DTO; own concurrency |
| `Audit creation metadata (имя уточняется)` | Уточняется | Уточняется | Server audit создания | Convention/name/type [Q15](../questions/Q15-api-and-source-code.md) |
| `Audit modification metadata (имя уточняется)` | Уточняется | Уточняется | Server audit изменения | Convention/name/type [Q15](../questions/Q15-api-and-source-code.md) |


**`EntityInstanceVersion`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Id` | `EntityInstanceVersionId` | Да | Server version identity | Publish ID не меняет |
| `EntityInstanceId` | `EntityInstanceId` | Да | Logical owner | Immutable reference |
| `SchemaVersionId` | `EntitySchemaVersionId` | Да | Exact target schema version | Same logical schema; immutable после create |
| `Status` | Уточняется | Да | Version lifecycle | Draft/Published; CLR type [Q15](../questions/Q15-api-and-source-code.md) |
| `VersionNumber` | `int?` | Условно | Global business number | Draft null; Publish positive/unique per owner; immutable |
| `BasePublishedInstanceVersionId` | `EntityInstanceVersionId?` | Условно | Saved same-target baseline | Null если group history нет; не global pointer |
| `Content` | `EntityInstanceContent` | Да | Full saved authoring values | Draft replacement; Published immutable; missing/default не смешиваются |
| `OrphanValues` | `IReadOnlyList<OrphanValue>` | Да | Saved incompatible work | Draft preserved; strict Publish empty; explicit discard only |
| `CreatedAt` | `DateTimeOffset` | Да | Server create time | Immutable при Publish |
| `CanonicalProfileId` | `CanonicalProfileId?` | Условно | Frozen instance content profile | Draft null; Published actual frozen supported instance profile (/1 historical, Offset new MVP); neutral type F03 |
| `CanonicalBytes` | `ReadOnlyMemory<byte>?` | Условно | Exact frozen runtime UTF8 bytes | Draft null; Published noBOM/no insignificantspace/no finalLF, no JSONB/reserialization |
| `CanonicalContentJson` | `string?` | Условно | Optional strict UTF8 decoding of CanonicalBytes | Same exact bytes/profile/hash; not independently generated payload; no configured URL |
| `ContentHash` | `ContentHash?` | Условно | CanonicalDigest.Value wrapped by own Domain VO | Draft null; Published SHA256 exactbytes64lowerhex/no prefix; compare stored profile too |
| `PublishedAt` | `DateTimeOffset?` | Условно | Server Publish time | Draft null; Published immutable |
| `RetiredAt` | `DateTimeOffset?` | Условно | Server Retire time | Published lifecycle only; snapshot прежний |
| `Version` | `int` | Да | Internal version token | Серверный; не DTO; own owner |


**`EntityInstanceId`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Value` | `Guid` | Да | Typed stable identity | Server factories; Publish unchanged |


**`EntityInstanceVersionId`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Value` | `Guid` | Да | Typed stable identity | Server factories; Publish unchanged |


**`EntityInstanceKey`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Value` | `string` | Да | Смысловое значение VO | Unique per SchemaId; immutable; limits/format Q15/Q36 |


**`EntityInstanceName`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Value` | `string` | Да | Смысловое значение VO | Mutable authoring label; не runtime content field; limits Q15/Q36 |


**`EntityInstanceContent`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Fields` | `IReadOnlyList<InstanceFieldValue>` | Да | Full authoring snapshot | Immutable-by-replacement; empty/unset clean Draft allowed; strict Required policy §2.3 |


**`InstanceFieldValue`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `SchemaFieldId` | `SchemaFieldId` | Да | Named field identity | Unique в одном object occurrence; повтор в разных list items допустим |
| `Value` | `InstanceValue` | Да | Recursive value envelope | Presence/default/null различаются; Draft/strict profiles Q36/Q15 |


**`InstanceValue`** — ровно соответствующий logical variant; exact union/presence Q15


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Scalar value (имя уточняется)` | Уточняется | Условно | Scalar JSON variant | Exact property/CLR/wire/presence [Q15](../questions/Q15-api-and-source-code.md); semantic rules §2.3 |
| `Object.Fields` | `IReadOnlyList<InstanceFieldValue>` | Условно | Object variant fields | Logical variant address, не выбранное wire-имя; recursive occurrence rules |
| `List.Items` | `IReadOnlyList<InstanceListItem>` | Условно | List variant items | Logical variant address; nested Lists allowed; authored order |


**`NaiveDateTimeInstanceValue`** — целевой typed scalar variant; actual union/discriminator carrier Q15.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Value` | `DateTime` | Да | Historical/Demo Naive value | Kind=Unspecified; exact /1 seven-digit syntax без offset; определяется exact definition, не host timezone |

**`OffsetDateTimeInstanceValue`** — целевой typed scalar variant для Offset definition; новый semantic contract Q34.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Value` | `DateTimeOffset` | Да | Known instant и authored offset | Offset ±14:00 whole minutes, valid local/UTC range; authoring preserves offset/100ns; runtime UTC projection отдельно |

DateTime scalar payload выбирается по immutable DateTimeValueDefinition.TemporalSemantics F01, не client override/value-shaped guessing. Оба variants остаются scalar внутри existing InstanceValue recursion; FieldDataType набор не расширяется. Domain wrappers не новые wire envelopes/таблицы. Logical transport scalar DateTime — string выбранного exact syntax, generated CLR mapping соответствует semantics.

**`InstanceListItem`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Value` | `InstanceValue` | Да | Recursive anonymous item | Без fakeFieldId |
| `ItemKey` | `string?` | Условно | Opaque identity occurrence List<Object>, создаётся клиентом при add | Уникален в одной occurrence конкретного List; сервер валидирует. Copy/Reorder сохраняют key; не EntityId/FieldId. Scalar List key не имеет |


**`OrphanValue`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Id` | `Guid` | Да | Server saved record identity | Stable explicit discard ID; новый owner copy получает новый ID |
| `SchemaFieldId` | `SchemaFieldId` | Да | Исходное named field identity | Provenance/context [Q15](../questions/Q15-api-and-source-code.md) |
| `FieldKeySnapshot` | `string?` | Нет | Снимок исходного key | Не fallback mapping identity |
| `Path` | Уточняется | Уточняется | Authoring value location | Nested location/provenance type [Q15](../questions/Q15-api-and-source-code.md) |
| `ValueJson` | `string` | Да | Сохранённое raw значение | Lossless preservation; no silent coercion; не runtime property |


**`InstanceDraftReconciliation`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Resulting Content (имя уточняется)` | Уточняется | Уточняется | Полный resulting snapshot | EntityInstanceContent owner выше; exact result property/type [Q15](../questions/Q15-api-and-source-code.md) |
| `OrphanValues` | Уточняется | Уточняется | Preserved/new orphans | OrphanValue owner выше; exact result collection [Q15](../questions/Q15-api-and-source-code.md); explicit discard only |
| `ValidationIssues` | Уточняется | Уточняется | Draft/strict issue result | Exact collection/issue/path [Q15](../questions/Q15-api-and-source-code.md)/[Q36](../../../business/questions/Q36-entity-instance-lifecycle.md) |


Presence отличается от present default/null; mapper не заполняет отсутствующие поля CLR defaults. Содержание root Content/OrphanValues корректно как JSON/структура envelope, но Draft может иметь incomplete/type-incompatible work по Draft profile Q36/Q15. Нет universal Inherit/Remove overlay operations из current InstanceField. Runtime Published payload формируется из validated content по Q19/F13, а не выдаётся как authoring tree с orphan IDs/Name.


При read после изменения Draft schema новые incompatibilities обнаруживаются в projection исходного stored Content, не изменением БД в GET. Stable stored orphan IDs не пересоздаются; вновь вычисленная incompatibility получает deterministic location token по FieldId/ItemKey, связанный с exact schema/content basis. Save проверяет basis и explicit discard этого token; это не случайный persisted orphan ID. Отсутствие новых ID не даёт права потерять raw work.


Ручное исправление означает явный ввод корректного значения в допустимое target поле и отдельное удаление старой orphan-записи. Автоматическое восстановление orphan в вновь появившееся поле не согласовано; reconciliation не предполагает такое согласие.


### 2.2. Методы и переходы


| Тип | Метод | Параметры | Предусловия | Изменение / результат |
| --- | --- | --- | --- | --- |
| EntityInstance | Create | server Id, SchemaId, Key/Name | Valid metadata, Active schema, key availability проверена Application/DB | Active, no published pointer/number/withdrawal, internal Version |
| EntityInstance | Rename | EntityInstanceName | Allowed logical lifecycle | Name/audit/Version, без изменения Key/versions/runtime content |
| EntityInstanceVersion | CreateDraft | new Id, owner, exact target, Content/Orphans, same-target base?, server time | Target belongs schema, authoring eligibility | Draft number/hash/published fields null; immutable target |
| EntityInstanceVersion | SaveDraft | InstanceDraftReconciliation | Draft, own precondition; current target schema facts согласованы | Content/orphans/own Version атомарно, baseline/target прежние |
| EntityInstanceVersion | AcceptCurrentBase | latest same-target Published ID?, validated policy result | Draft, validation Q36/Q15 | Только baseline/Version; Content/Orphans неизменны, no merge |
| EntityInstanceVersion | Publish | InstancePublishParameters with CanonicalRepresentation | Published eligible target schema; strict values valid; no orphan; same-target baseline current | Immutable Published facts/Status/own Version, target/Id/content/CreatedAt/base прежние |
| EntityInstance | RegisterPublishedVersion | versionId, next global number, verified ownership | Next=(LatestPublishedVersionNumber??0)+1; instance Active | Global pointer/number/own Version, не same-target pointer |
| EntityInstance | Archive / Restore | audit time при необходимости | MVP lifecycle eligibility | Logical lifecycle/Version; no mass child rewrite, no automatic Publish/Activate |
| EntityInstanceVersion | Retire / RestoreRetired | server time при Retire | Published; allowed lifecycle | RetiredAt/own Version, snapshot/hash/number прежние |
| EntityInstance | WithdrawFromDelivery | server now | Applicable direct Delivery scope; retained Published content обязателен, Draft-only/empty rejected | Logical WithdrawnAt/Version; history/global numbering прежние |
| EntityInstance | ReturnToDelivery (target name) | ReturnToDeliveryAdmissibility | Explicit user action, Withdrawn owner; при Published history — current schema/asset eligibility; без Published — recovery собственного запрета | Clears withdrawal/own Version; не Publish, не меняет snapshots/number/hash |
| Deletion policy | CheckDeleteEligibility | Current versions/local+historical refs | Version только Draft; logical no Published/history | Allowed explicit removal set/blockers; repository removal, no implicit history cascade |


Copy — отдельное создание Draft через mapping stable IDs, не изменение source/target сохранённой версии. ReturnToDelivery — target ContentModeling owner mutation F04: own If-Match, при Published history eligibility checks текущих Published/schema/asset representations; без Published recovery без создания runtime representation; clearing withdrawal и own Version одним commit. Exact command/request/route materialization Q15. F13 только потребляет обновлённую availability в следующем новом plan; не владеет командой снятия EntityInstance withdrawal. Repeated/no-op outcomes не выводятся из Archive/Restore, Q15.


### 2.3. Инварианты и constraints


Единый source постоянных ограничений — EntityInstanceConstraints/EntityInstanceVersionConstraints и value-specific shared constraints F01/Q34 по [S21](../../../catalog/docs/standards/architecture/ddd.md#aggregate-owned-rule-definitions). Unique/lifecycle/baseline — state policies. Числовые payload limits/Key regex не придумываются.


- Instance Key unique внутри SchemaId, immutable после create; Name mutable label. Target version same schema, immutable после Draft create. Для смены target создаётся другой Draft, не PUT нового SchemaVersionId.
- Multiple Draft разрешены на instance и на exact target. Draft может ссылаться на Draft schema; strict Publish только по Published/not-Retired/not-Withdrawn eligible schema и Active logical owners.
- Draft baseline = latest Published instance version с тем же owner и target SchemaVersionId. Null допустим при отсутствии этой группы. Publish другой target не stale; latest global pointer не заменяет group baseline. Retired history не удаляется для искусственного принятия более ранней базы.
- Number positive integer1,2,3... per logical instance global; assigned only committed Publish. No gap от failed Publish/deleted Draft, no wrap/duplicate/reuse. Same-target concurrent winner делает loser stale409; different-target Publish при актуальных relevant baselines использует короткую serialized sequence allocation: оба успешны с разными последовательными номерами, без ложного stale.
- Copy match только SchemaFieldId + compatible semantic definition/context. Name/Key/позиция не fallback. Unmatched/incompatible source и уже saved orphans сохраняются; новые target fields absent. ItemKey различает повторения List<Object> и сохраняется при Copy/Reorder. ItemKey opaque, client-generated-on-add, сервер проверяет uniqueness per List occurrence; не EntityId/FieldId. Scalar lists не получают keys; index не является stable diagnostic identity.
- Changing mutable target Draft schema не мигрирует dependent content в фоне. Read/Save/Publish revalidate current target; no data loss/auto deletion incompatible values. Удаление orphan только явно: persisted server ID либо deterministic computed location token с exact basis. Обычное отсутствие поля в payload не является consent удалить orphan.
- В object occurrence один named field ID не дублируется; тот же ID в разных list items допустим. Value uniqueness не равна schema whole-tree field-definition uniqueness.
- Strict Published validation: required = presence+valid type, absent/null forbidden. String/Text nonempty, String max255 Q34; optional absent разрешено, присутствующее значение тоже type-valid. Numeric0, Boolean false, zero/negative TimeSpan, empty List/FlagEnum, transparent Color допустимы.
- Integer/Decimal сохраняют числовую семантику, Float finite; widths/precision/wire Q15/Q31. Demo DateTime без offset, Date календарная, TimeSpan duration. Text — restricted sanitized HTML с paragraphs/p, br, lists и bold/italic; script/event/style и неподдерживаемый markup не допускаются. Пустота определяется по семантическому тексту после sanitization; максимум настраивается, фиксированное число не назначено. Не вводятся Q35 advanced constraints как Demo blocker.
- Enum:string option Key; FlagEnum:set string keys без bitmask, deterministic canonical order Q34; unknown key invalid. Deprecated defined option остаётся valid, выдаёт warning и скрыта из new-choice picker; unknown option invalid. Color exact #RRGGBBAA; Object/List recursively type-valid, nested Lists allowed, Object children required evaluated inside present object, List default authored order retained. Advanced comparer не применяется молча.
- AssetReference имеет stable AssetId и должен соответствовать `AssetKind`, зафиксированному definition schema field/list item. Strict Publish под Demo AssetId guard либо MVP global transaction advisory guard batch-проверяет availability/kind и захватывает согласованную immutable metadata через F05 public port в runtime AssetReference descriptor. Descriptor входит в Published values/ContentHash, без configured CM/storage URL; subsequent Delivery читает frozen snapshot без enrichment. Storage URLs/MinIO metadata не являются authority. Orphans и unresolved current type issues блокируют Publish, не попадают в runtime payload.
- Published data/history immutable/no hard-delete. Logical hard-delete только без Published/history refs; Draft removal не удаляет parent автоматически. Archive/Retire blocks new use, historical Release/Revision retained. Withdrawal logical scope F13 не уничтожает records; новая публикация/Archive Restore не должны молча сбрасывать withdrawal.


MVP [Q35](../../../business/questions/Q35-value-constraints.md): type-valid extra-rule violations сохраняются в Draft как адресуемые blocking-Publish issues. Это не меняет rejection новых intrinsic type-invalid values. Полные recursive constraints definitions принадлежат [F01](F01-create-entity-schema.md#21-aggregates-entities-и-value-objects); F04 не хранит второй mutable набор rules. Copy сохраняет совместимые values при нарушении bounds/length/count/order/uniqueness и возвращает issues, не делает их orphan только из-за constraints. Runtime lists сохраняют authored order, equality исключает ItemKey/orphans/metadata. Instance Publish проверяет current exact target definitions без coercion/default/rounding/dedup/sort.

MVP temporal [Q34](../../../business/questions/Q34-schema-field-semantics.md#datetime-mvp-temporal-semantics-и-совместимость): authoring Offset сохраняет введённое local date/time+offset/100ns; Published runtime UTC seven-digit Z. Offset equality/min/max/order/unique по instant, не offset. Temporal bounds same instant с разными offsets имеют same semantic identity. Naive→Offset new FieldId; Copy old unmatched value orphan/new field unfilled, manual user offset. Server/browser timezone и DST/zone database не используются. Date/TimeSpan/audit timestamps остаются прежними.

### 2.4. Domain events


Current EntityInstance создаёт Created/VersionPublished domain events. Target не вводит новый broker/outbox: факт commit обеспечивает server read visibility. Current event consumer/effective Delivery propagation не предполагаются из имени события; реальные public boundary/retention contracts относятся к F13/F05/Publishing. Нужность local consumers и timestamps/rollback semantics — Q15, no events только ради логов.


**`EntityInstanceCreatedDomainEvent`** — [current event](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Domain/EntityInstances/DomainEvents/EntityInstanceCreatedDomainEvent.cs); необходимость target/local consumer Q15


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `EntityInstanceId` | `EntityInstanceId` | Да | Identity созданного logical owner | Current payload; target consumer Q15 |
| `SchemaId` | `EntitySchemaId` | Да | Logical schema identity | Current payload |
| `OccurredAt` | `DateTimeOffset` | Да | Серверное время domain fact | Current default DateTimeOffset.UtcNow; не client input; target rollback Q15 |


**`EntityInstanceVersionPublishedDomainEvent`** — [current event](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Domain/EntityInstances/DomainEvents/EntityInstanceVersionPublishedDomainEvent.cs); необходимость target/local consumer Q15


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `EntityInstanceId` | `EntityInstanceId` | Да | Logical owner identity | Current payload; target consumer Q15 |
| `EntityInstanceVersionId` | `EntityInstanceVersionId` | Да | Published version identity | Current payload; no broker inference |
| `OccurredAt` | `DateTimeOffset` | Да | Серверное время transition | Current default DateTimeOffset.UtcNow; не client input; target rollback Q15 |


## 3. Application.Abstractions


### 3.1. Commands


Все version commands содержат InstanceId:Guid и соответствующий VersionId/SourceVersionId:Guid; logical commands InstanceId. Api If-Match → внутренний ExpectedVersion:int, отсутствует в wire DTO.


| Command | Result | Concurrency owner |
| --- | --- | --- |
| CreateEntityInstanceCommand | EntityInstanceDto с exact InitialDraft summary + logical metadata | Новые roots; no If-Match; parent/target guard |
| CreateCleanEntityInstanceDraftCommand | EntityInstanceVersionDto + metadata | Новый Draft; target/base guard |
| CreateEntityInstanceDraftCopyCommand | EntityInstanceVersionDto + metadata | Source Draft ETag; Published lifecycle Q15 |
| UpdateEntityInstanceDraftCommand | EntityInstanceVersionDto + metadata | Exact Draft; current schema separately |
| AcceptCurrentEntityInstanceDraftBaseCommand | EntityInstanceVersionDto + metadata | Draft + same-target baseline |
| PublishEntityInstanceVersionCommand | EntityInstanceVersionDto + metadata | Draft + logical sequence/schema eligibility |
| DeleteEntityInstanceDraftCommand | Guid | Exact Draft + dependency guards |
| DeleteEntityInstanceCommand | Guid | Logical root + history/refs |
| ArchiveEntityInstanceCommand | EntityInstanceDto + metadata | Logical lifecycle |
| RestoreEntityInstanceCommand | EntityInstanceDto + metadata | Logical lifecycle |
| RetireEntityInstanceVersionCommand | EntityInstanceVersionDto + metadata | Published lifecycle |
| RestoreEntityInstanceVersionCommand | EntityInstanceVersionDto + metadata | Published lifecycle |
| WithdrawEntityInstanceCommand | EntityInstanceDto + metadata | Logical root |
| RenameEntityInstanceCommand | EntityInstanceDto + metadata | Logical root |
| ReturnEntityInstanceToDeliveryCommand (target proposed name) | EntityInstanceDto + logical metadata | Logical root own ExpectedVersion; current Published/schema/media admissibility guards |


**`CreateEntityInstanceCommand`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Request` | `CreateEntityInstanceRequest` | Да | Create input | Server IDs/number не принимаются |


**`CreateCleanEntityInstanceDraftCommand`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `InstanceId` | `Guid` | Да | Logical owner | Route identity |
| `Request` | `CreateEntityInstanceDraftRequest` | Да | Clean input | Empty/unset Draft values допустимы |


**`CreateEntityInstanceDraftCopyCommand`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `InstanceId` | `Guid` | Да | Logical owner | Route identity |
| `SourceVersionId` | `Guid` | Да | Exact source | Same owner |
| `CopyRequest` | `CreateEntityInstanceDraftCopyRequest` | Да | Target copy input | Same logical schema |
| `Expected source Version (имя уточняется)` | Уточняется | Условно | Internal source token | Draft required If-Match; exact name/type/Published profile [Q15](../questions/Q15-api-and-source-code.md) |


**`UpdateEntityInstanceDraftCommand`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `InstanceId` | `Guid` | Да | Logical owner | Route identity |
| `VersionId` | `Guid` | Да | Exact Draft | Same owner |
| `UpdateRequest` | `UpdateEntityInstanceDraftRequest` | Да | Desired values/discard | Preserve raw saved work; Q15 |
| `ExpectedVersion` | `int` | Да | Internal own token | Api If-Match; не DTO |


**`AcceptCurrentEntityInstanceDraftBaseCommand`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `InstanceId` | `Guid` | Да | Logical owner | Route identity |
| `VersionId` | `Guid` | Да | Exact Draft | Same owner |
| `ExpectedVersion` | `int` | Да | Internal own token | Api If-Match |
| `Consent/base identity (имя уточняется)` | Уточняется | Уточняется | Shown baseline consent | Exact freshness contract [Q15](../questions/Q15-api-and-source-code.md) |


**`PublishEntityInstanceVersionCommand`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `InstanceId` | `Guid` | Да | Logical owner | Route identity |
| `VersionId` | `Guid` | Да | Saved exact Draft | Same owner |
| `ExpectedVersion` | `int` | Да | Internal own token | Api If-Match; no body Save |


**`DeleteEntityInstanceDraftCommand`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `InstanceId` | `Guid` | Да | Logical owner | Route identity |
| `VersionId` | `Guid` | Да | Exact Draft | Same owner |
| `ExpectedVersion` | `int` | Да | Internal own token | Api If-Match |


**`DeleteEntityInstanceCommand`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `InstanceId` | `Guid` | Да | Logical owner | Route identity |
| `ExpectedVersion` | `int` | Да | Internal logical token | Api If-Match |
| `Consent (имя уточняется)` | Уточняется | Уточняется | Delete consent | [Q15](../questions/Q15-api-and-source-code.md)/[Q36](../../../business/questions/Q36-entity-instance-lifecycle.md); preview не authority |


**`ArchiveEntityInstanceCommand`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `InstanceId` | `Guid` | Да | Logical owner | Route identity |
| `ExpectedVersion` | `int` | Да | Internal logical token | Api If-Match |


**`RestoreEntityInstanceCommand`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `InstanceId` | `Guid` | Да | Logical owner | Route identity |
| `ExpectedVersion` | `int` | Да | Internal logical token | Api If-Match |


**`RetireEntityInstanceVersionCommand`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `InstanceId` | `Guid` | Да | Logical owner | Route identity |
| `VersionId` | `Guid` | Да | Exact Published | Same owner |
| `ExpectedVersion` | `int` | Да | Internal own token | Api If-Match |


**`RestoreEntityInstanceVersionCommand`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `InstanceId` | `Guid` | Да | Logical owner | Route identity |
| `VersionId` | `Guid` | Да | Exact Published | Same owner |
| `ExpectedVersion` | `int` | Да | Internal own token | Api If-Match |


**`WithdrawEntityInstanceCommand`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `InstanceId` | `Guid` | Да | Logical owner | Route identity |
| `ExpectedVersion` | `int` | Да | Internal logical token | Api If-Match; logical scope |


**`ReturnEntityInstanceToDeliveryCommand` (target proposed name, не существующий исходник):**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `InstanceId` | `Guid` | Да | Logical ContentModeling owner | Exact identity; URI материализуется Q15 |
| `ExpectedVersion` | `int` | Да | Internal own optimistic token | Api принимает logical If-Match; не wire DTO |

Command не принимает replacement values, hash, number или client-supplied media eligibility. Result переиспользует EntityInstanceDto с own post-commit ETag; отдельная копия DTO не создаётся. Exact transport request/body/carrier и repeated/no-op mapping — Q15.

**`ReturnToDeliveryAdmissibility` (target domain parameter model; имя предложено):**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `PublishedVersionIds` | `IReadOnlyList<EntityInstanceVersionId>` | Да | Exact retained Published representations; empty только для восстановления Draft-only/пустого owner | Same logical instance; current candidate/eligibility rules F13; empty не создаёт runtime content |
| `IsAdmissible` | `bool` | Да | Итог current owner/schema/target/media проверки handler/policy | Только true разрешает снятие withdrawal; не клиентский input, не persisted content |

**`RenameEntityInstanceCommand`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `InstanceId` | `Guid` | Да | Logical owner | Route identity |
| `ExpectedVersion` | `int` | Да | Internal logical token | Api If-Match |
| `Name` | Уточняется | Да | Новая logical подпись | Request Name:string; command CLR type [Q15](../questions/Q15-api-and-source-code.md); Key immutable |


Metadata carrier post-commit Q15; result не объявляет success в handler до UoW CompleteAsync.


### 3.2. Queries


| Query | Result |
| --- | --- |
| ListEntityInstancesQuery | OffsetPagedResultDto<EntityInstanceListItemDto> |
| GetEntityInstanceQuery | EntityInstanceDto + internal ETag metadata |
| ListEntityInstanceVersionsQuery | OffsetPagedResultDto<EntityInstanceVersionSummaryDto> |
| GetEntityInstanceVersionQuery | EntityInstanceVersionDto + internal ETag metadata |
| GetEntityInstanceDeleteImpactQuery | EntityInstanceDeleteImpactDto |


**`ListEntityInstancesQuery`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Search (имя уточняется)` | Уточняется | Уточняется | Name/Key search | Exact wire/type [Q15](../questions/Q15-api-and-source-code.md) |
| `SchemaId` | Уточняется | Нет | Optional schema filter | CLR/wire type [Q15](../questions/Q15-api-and-source-code.md) |
| `Lifecycle/status/paging/sort (имена уточняются)` | Уточняется | Уточняется | Server selection/page | Exact properties/defaults/types [Q15](../questions/Q15-api-and-source-code.md) |


**`GetEntityInstanceQuery`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `InstanceId` | `Guid` | Да | Exact logical owner | No fallback |


**`ListEntityInstanceVersionsQuery`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `InstanceId` | `Guid` | Да | Logical owner | Route identity |
| `TargetSchemaVersionId` | Уточняется | Нет | Optional target filter | Exact CLR/wire [Q15](../questions/Q15-api-and-source-code.md) |
| `Status/paging/sort (имена уточняются)` | Уточняется | Уточняется | Server selection/page | Names/types/defaults [Q15](../questions/Q15-api-and-source-code.md) |


**`GetEntityInstanceVersionQuery`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `InstanceId` | `Guid` | Да | Logical owner | Route identity |
| `VersionId` | `Guid` | Да | Exact version | Same owner; no fallback |


**`GetEntityInstanceDeleteImpactQuery`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `InstanceId` | `Guid` | Да | Logical owner | Full dependencies/history |


Target schema exact read переиспользует F02. Новый version-delete preview API не согласован; requirement blockers если нужны возвращаются command/Q15. Query handlers [S20](../../../catalog/docs/standards/architecture/cqrs.md#handler-responsibilities-and-dependency-direction) в EntityFramework.Persistence, no query handlers в Application.


### 3.3. DTO и parameter models


**`CreateEntityInstanceRequest`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Name` | `string` | Да | Authoring label | Shared InstanceName constraints; Q15/Q36 |
| `Key` | `string` | Да | Logical key | Unique per SchemaId; immutable after create |
| `SchemaId` | `Guid` | Да | Logical schema | Active eligible owner |
| `SchemaVersionId` | `Guid` | Да | Exact target | Same schema; Draft/Published authoring profile |
| `Values` | `EntityInstanceValuesDto` | Да | Initial authoring values | Draft permits missing required; no server IDs/orphans/number |


**`CreateEntityInstanceDraftRequest`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `TargetSchemaVersionId` | `Guid` | Да | Exact target | Same logical schema; clean content empty/unset Draft |


**`CreateEntityInstanceDraftCopyRequest`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `TargetSchemaVersionId` | `Guid` | Да | Copy target | Source route; same logical owner/schema; mapping FieldId only |


**`UpdateEntityInstanceDraftRequest`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Values` | `EntityInstanceValuesDto` | Да | Desired authoring values | FULL desired authoring snapshot; absent optional clears; collapsed/unrendered retained; orphan explicit discard only; no PATCH; target/Name not included |
| `DiscardOrphanValueIds` | `IReadOnlyList<Guid>` | Уточняется | Explicit saved orphan discard | Own server IDs; collection omission/default [Q15](../questions/Q15-api-and-source-code.md); no implicit deletion |
| `Computed discard tokens (имя уточняется)` | Уточняется | Условно | Explicit discard вычисленных incompatibilities | Deterministic FieldId/ItemKey location token, только с exact basis; carrier/type Q15 |
| `Diagnostics basis (имя уточняется)` | Уточняется | Да | Exact observed schema/content/context identity | Logical DiagnosticsBasis ниже; stale basis не применяется молча; carrier/type Q15 |


**`RenameEntityInstanceRequest`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Name` | `string` | Да | Новая authoring подпись | Key immutable; method/route Q15 |


**`EntityInstanceValuesDto`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Fields` | `IReadOnlyList<InstanceFieldValueDto>` | Да | Recursive named values | FieldId/value variants §2; exact scalar/Object/List/ItemKey/presence Q15; no schema metadata |


`InstanceFieldValueDto` реализует логический `NamedAuthoringValue` ниже: namedFieldId плюс explicit recursive value variant. Exact CLR declaration/discriminator и wire casing кроме согласованного namedFieldId материализуются в [Q15](../questions/Q15-api-and-source-code.md); Domain model не объявляется существующим transport-контрактом.


Authoring wire имеет явные recursive **Scalar / Object / List** variants; это отдельный формат от runtime keyed JSON. Ниже имена variant families и `namedFieldId`/`ItemKey` являются согласованной семантикой; exact discriminator name, CLR declarations и JSON required/null encoding материализуются Q15, не утверждаются как существующий DTO.

**Logical `NamedAuthoringValue` (target wire model):**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `namedFieldId` | `Guid` | Да | Stable identity named schema field | Unique внутри object occurrence; не key/name/index |
| `Value` | Уточняется | Да | Recursive Scalar/Object/List variant | Ровно один variant; exact CLR/discriminator Q15 |

**Logical `Scalar` variant:**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Scalar payload (wire name уточняется)` | Уточняется | Да | Type-specific scalar value | Scalar encoding всех применимых типов по Q15; missing не CLR default |

**Logical `Object` variant:**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Named values (wire name уточняется)` | Уточняется | Да | Collection NamedAuthoringValue | Recursive; namedFieldId принадлежит exact definition/context |

**Logical `List` variant:**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Anonymous items (wire name уточняется)` | Уточняется | Да | Ordered recursive Scalar/Object/List values | Нет fakeFieldId; nested List поддерживается |

**Logical `AnonymousAuthoringItem` (target wire model):**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Value (wire name уточняется)` | Уточняется | Да | Recursive Scalar/Object/List value | Anonymous item не получает namedFieldId/fakeFieldId; exact CLR Q15 |
| `ItemKey (carrier уточняется)` | `string?` | Условно | Opaque occurrence key только для items List<Object> | Client add, server uniqueness validation, preserve Copy/Reorder; scalar List без key |

Runtime serializer использует schema Keys только после strict Publish validation; authoring namedFieldId/ItemKey/orphan/discard/diagnostics metadata не превращаются в runtime properties. Точный byte/hash profile принадлежит Q19, не этой таблице.

**Logical `DiagnosticsBasis` (target read/consent model):**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Basis identity (wire name уточняется)` | Уточняется | Да | Identity exact schema/content/baseline observation | Отдельна от own ETag; exact encoding Q15 |
| `Schema basis (wire name уточняется)` | Уточняется | Да | Exact schema version + observed saved contract | Mutable Draft schema не определяется одним Id |
| `Content basis (wire name уточняется)` | Уточняется | Да | Exact saved instance root/content observation | Валидация computed discard только против этой basis |

Persisted `OrphanValueDto.Id` стабилен; computed incompatibility не получает случайный persisted ID в GET, а выдаёт deterministic location token из stable FieldId/ItemKey address и exact basis. Save передаёт basis и explicit list computed tokens помимо DiscardOrphanValueIds. Common Issue code/message/address определяется Q15; индекс List<Object> не identity.

**`EntityInstanceDto`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Id` | `Guid` | Да | Logical identity | Server-generated |
| `SchemaId` | `Guid` | Да | Logical schema | Immutable owner |
| `Key` | `string` | Да | Logical key | Immutable |
| `Name` | `string` | Да | Current authoring label | Mutable; no runtime field |
| `Lifecycle` | Уточняется | Да | Logical state | Active/Archived; exact CLR/wire [Q15](../questions/Q15-api-and-source-code.md) |
| `Withdrawn` | `bool` | Да | Logical withdrawal projection | Derived from WithdrawnAt; not new stored flag |
| `WithdrawnAt` | `DateTimeOffset?` | Условно | Server withdrawal time | Create null; F13 logical scope |
| `LatestPublishedVersionId` | `Guid?` | Условно | Global Published pointer | Null until first publication; not same-target baseline |
| `LatestPublishedVersionNumber` | `int?` | Условно | Global Published number | Null until first publication |
| `Versions` | `IReadOnlyList<EntityInstanceVersionSummaryDto>` | Да | Version summaries | Create selects exact initial Draft; history profile [Q15](../questions/Q15-api-and-source-code.md) |
| `Audit metadata (имена уточняются)` | Уточняется | Уточняется | Server read metadata | Exact properties/types [Q15](../questions/Q15-api-and-source-code.md) |


**`EntityInstanceVersionSummaryDto`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Id` | `Guid` | Да | Version identity | No per-row ETag |
| `SchemaVersionId` | `Guid` | Да | Exact target | Immutable |
| `Status` | Уточняется | Да | Version lifecycle | Draft/Published; exact type [Q15](../questions/Q15-api-and-source-code.md) |
| `VersionNumber` | `int?` | Условно | Global business number | Draft null; Published immutable |
| `BasePublishedInstanceVersionId` | `Guid?` | Условно | Saved same-target base | Null without same-target history |
| `IsStale` | `bool` | Да | Derived stale flag | Same-target baseline comparison |
| `IsRetired` | `bool` | Да | Derived lifecycle flag | Timestamp projection |
| `CreatedAt` | `DateTimeOffset` | Да | Server create time | Immutable on Publish |
| `PublishedAt` | `DateTimeOffset?` | Условно | Server Publish time | Draft null; Published immutable |


**`EntityInstanceListItemDto`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Id` | `Guid` | Да | Logical identity | No full snapshots |
| `SchemaId` | `Guid` | Да | Logical schema | List projection |
| `Key` | `string` | Да | Logical key | Immutable |
| `Name` | `string` | Да | Current label | List projection |
| `Lifecycle` | Уточняется | Да | Logical state | CLR/wire [Q15](../questions/Q15-api-and-source-code.md) |
| `Withdrawn` | `bool` | Да | Logical withdrawal projection | Derived |
| `DraftCount` | `int` | Да | Полное число Draft | Server projection |
| `LatestPublishedVersionNumber` | `int?` | Условно | Global current number | Null without Published |
| `Modified metadata (имя уточняется)` | Уточняется | Уточняется | Server modification metadata | Exact property/type [Q15](../questions/Q15-api-and-source-code.md) |


**`EntityInstanceVersionDto`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Id` | `Guid` | Да | Version identity | Stable on Publish |
| `EntityInstanceId` | `Guid` | Да | Logical owner | Exact ownership |
| `SchemaVersionId` | `Guid` | Да | Exact target | Immutable |
| `Status` | Уточняется | Да | Version state | Draft/Published; exact type [Q15](../questions/Q15-api-and-source-code.md) |
| `VersionNumber` | `int?` | Условно | Global business number | Draft null; Published immutable |
| `BasePublishedInstanceVersionId` | `Guid?` | Условно | Saved same-target baseline | Null without group history |
| `CurrentPublishedInstanceVersionId` | `Guid?` | Условно | Current same-target Published | Not global pointer; derived freshness independent own token |
| `IsStale` | `bool` | Да | Derived stale flag | Same-target comparison |
| `IsRetired` | `bool` | Да | Derived lifecycle flag | Timestamp projection |
| `Content` | `EntityInstanceValuesDto` | Да | Full saved work | Raw unknown preserved; no GET mutation |
| `OrphanValues` | Уточняется | Уточняется | Saved orphan projection | Elements OrphanValueDto; collection/derived candidate addressing [Q15](../questions/Q15-api-and-source-code.md) |
| `ValidationIssues` | Уточняется | Уточняется | Current derived diagnostics | Issue/path/collection shape [Q15](../questions/Q15-api-and-source-code.md); freshness not own token |
| `CreatedAt` | `DateTimeOffset` | Да | Server creation time | Immutable on Publish |
| `PublishedAt` | `DateTimeOffset?` | Условно | Server Publish time | Draft null; Published immutable |
| `RetiredAt` | `DateTimeOffset?` | Условно | Server lifecycle time | Published Retire/Restore |
| `CanonicalProfileId` | `CanonicalProfileId?` | Условно | Frozen runtime integrity profile | Actual frozen supported instance profile, /1 historical/new Offset MVP; HTTP adapter Value→string, wire casing Q15 |
| `ContentHash` | `CanonicalDigest?` | Условно | Hash frozen runtime bytes |64lowerhex/no prefix; profile comparison required; no DTO-number guessing |


**`OrphanValueDto`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Id` | `Guid` | Да | Saved server record ID | Own explicit discard; GET candidates not random persistent IDs |
| `SchemaFieldId` | `Guid` | Да | Source field identity | No Name/Key mapping fallback |
| `FieldKeySnapshot` | `string?` | Нет | Historical key snapshot | Optional context |
| `Path` | Уточняется | Уточняется | Authoring location | String/typed location [Q15](../questions/Q15-api-and-source-code.md); no decided wire shape |
| `ValueJson` | `string` | Да | Raw preserved value | Lossless; server record, not client-created OrphanValues |


**`EntityInstanceDeleteImpactDto`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `CanDelete` | `bool` | Да | Informational eligibility | Mutation rechecks history/dependencies |
| `DraftVersionCount` | `int` | Да | Полное число Draft | No UI paging |
| `Blockers` | Уточняется | Уточняется | History/dependency blockers | Exact element/collection shape [Q15](../questions/Q15-api-and-source-code.md); client count not authority |


Returned create result/operation ID позволяет после Create выбрать именно созданную version, не arbitrary latest; own exact version ETag получает F04 read. DTO не содержит Version/ExpectedVersion/Revision. ValidationIssue/path/type ambiguity/generated recursive profile — Q15. GET derived incompatibilities имеют deterministic location tokens + exact basis, persisted orphans сохраняют server IDs; wire encoding до Save Q15. Absence/null и полного desired Values snapshot versus preservation saved raw unknown work должны быть явны в contract.


**RuntimeAssetReferenceDescriptor (immutable captured public value)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| AssetId | Guid | Да | Stable source media identity | Canonical assetId lowercaseD; same as authoring reference, no URL identity |
| AssetKind | AssetKind | Да | Authoritative immutable kind | Canonical assetKind registered token; exact schema RequiredAssetKind compatibility |
| ContentType | string | Да | Authoritative confirmed immutable MIME metadata | Canonical contentType preserves confirmed string, not client guess |
| SizeBytes | long | Да | Actual physical file byte count | Canonical sizeBytes exact Int64; confirmed F05 count, no declared-length substitution |
| Sha256Hash | string | Да | SHA256 actual filebytes64lowerhex | Canonical sha256Hash; not instance CanonicalDigest/content profile/S3 ETag |

Published present AssetReference requires ALL five fields captured from server-confirmed F05 Available metadata/current kind+publish eligibility under guards. Fixed cm-instance-content/1 descriptor member order assetId,assetKind,contentType,sizeBytes,sha256Hash. Boundary mapper maps Guid/registered-kind-string/contentType/Int64/file-digest-string into neutral typed asset node; common library imports no MediaLibrary Domain or storage types. Pure metadata read only, no filebytes download during Publish.

Descriptor immutable with Published runtime bytes/profile/digest; Delivery/SDK never enrich it live. FileName/Format/dimensions/duration/URLs and other authoring metadata excluded. SDK derives CM URL separately from configuration+AssetId; file digest verifies filebytes separately from instance canonical SHA256. Changes in live UI metadata do not rewrite historical descriptor/hash; unavailable/incomplete authority never substitutes null/default/zero facts.

`DeliveryMode` не является шестым полем descriptor и не редактируется при выборе asset в instance editor. Его owner — exact versioned `AssetReferenceValueDefinition` [F01](F01-create-entity-schema.md#21-aggregates-entities-и-value-objects). Publish instance захватывает прежние пять facts и не инициирует preload. После Demo Delivery сопоставляет только реально присутствующие опубликованные ссылки с definitions exact target SchemaVersion для manifest; optional отсутствующая ссылка не создаёт job/launch blocker. `IsRequired` и `RequiredBeforeLaunch` независимы. Новая policy другой версии схемы не меняет существующие frozen instance bytes/hash или pinned snapshot; совместимость metadata/profile остаётся [Q37](../../frontend/questions/Q37-launcher-asset-delivery.md).

**InstancePublishParameters (server-owned domain parameter)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| VersionNumber | int | Да | Checked next logical instance business number | Server only, no failed allocation gap |
| Canonical | CanonicalRepresentation | Да | Frozen profile+bytes+digest triple | Exact target-bound supported instance profile; /1 historical/new Offset MVP; neutral type defined once F03 |
| PublishedAt | DateTimeOffset | Да | Server Publish time | Not canonical hash input |

**CanonicalInstanceContent (immutable Application result)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Canonical | CanonicalRepresentation | Да | Same-buffer canonical factory result | Typed validated runtime values/captured metadata; no later reserialization |

### 3.4. Ports


| Port | Минимальная ответственность | Направление |
| --- | --- | --- |
| IEntityInstanceRepository | Get exact logical root, ExistsBySchemaAndKey, Add/Remove, observed sequence/lifecycle | Application → Domain repository → local EF |
| IEntityInstanceVersionRepository | Exact owner+version, Add/Remove, latest Published same owner/target | Application → command storage |
| Schema facts reader + local mutation guard F02 | Exact current target content/status/Version+parent availability, source/cascade guards | Application → local ContentModeling storage; no F02 HTTP |
| IEntityInstanceDraftMapper / reconciliation policy | Source schema/content/target schema, existing orphans → compatible content+preserved orphans/issues | Local pure algorithm §4; no Name/Key/fuzzy fallback |
| ISchemaInstanceValueValidator | Draft/strict/Accept profiles current target/current baseline | Domain pure policy; no external IO |
| Asset availability/reference boundary F05 | Batch distinct `(AssetId, RequiredAssetKind)`, availability/kind compatibility и safe retention/deletion coordination | Application → public media boundary, adapter placement Q21/Q15 |
| Historical reference boundary MVP | Impact/retained reference authority; fail closed if unavailable | Application → public Publishing contract; absent excluded subsystem не вызывается в Demo |
| IInstanceContentCanonicalizer / factory | Explicit typed validated values+contract bindings+captured descriptors → CanonicalInstanceContent | Pure common library explicit /1/new-MVP instance profile compatibility; boundary mapper, no Domain/EF/Asp imports |
| Target IRestrictedTextSanitizer | Restricted HTML p/br/lists/bold/italic, server sanitization до semantic-empty/configured HTML max и MVP decoded text length | Local Application port/adapter; exact signature/library Q15; plaintext length semantics Q35 |
| TimeProvider / IUnitOfWorkManager | CreatedAt/PublishedAt/lifecycle; один ContentModeling commit/rollback | Application/CQRS → infrastructure |


Ports/signatures — target; IQueryable/foreign DbContext не выходят в Application/Domain. S3 bytes handling не часть command; no cross-BC transaction guarantees от public adapter. Reference precheck alone не предотвращает concurrent delete/new reference; protocol Q15/§7.


MVP coordination port принадлежит Application.Abstractions: задаёт границу guarded owner mutation, cancellation и outcome, без PostgreSQL/Npgsql/EF типов в Domain. Точные CLR-имена и signatures остаются Q15. Application открывает owning UoW и требует guard до authoritative reads; EntityFramework.Persistence выполняет standalone pg_advisory_xact_lock на той же physical connection и transaction владельца. Public foreign read adapters используют независимые primary read scopes/connections без ambient UoW enlistment. Host/DI отделяет owning UoW от этих scopes; существующий [EfCoreUnitOfWorkParticipant](../../../../contentmanager.api/src/BuildingBlocks/ContentManager.EntityFramework.UnitOfWork/EfCoreUnitOfWorkParticipant.cs), способный enlist несколько contexts в общую SQL transaction, не выполняет эту целевую границу автоматически. Domain получает уже проверенные предметные facts и не содержит SQL guard type.

### 3.5. Specifications / read contracts


По [S19](../../../catalog/docs/standards/dotnet/ardalis-specification.md) EntityInstancesListSpecification, InstanceVersionByOwnerAndIdSpecification, InstanceVersionsByOwnerSpecification, LatestPublishedInstanceVersionForSchemaSpecification и InstanceDeleteImpactSpecification reuse atomic builder conditions owner/schema/target/status/lifecycle. Нет выдуманного spec.And; tracking/projection/order/paging отдельно.


Latest same-target query: OwnerId AND SchemaVersionId AND Status Published, max VersionNumber, exact Id; не global LatestPublishedVersionId и не last created. Baseline учитывает Published history без искусственного fallback, eligibility — отдельная policy. List count те же filters без paging; summaries без Content; normal list excludes Archived, exact archived readonly доступен. Rail Draft CreatedAt desc, Published number desc, stable Id tie-breaker. Default newest Draft else latest Published, target filter/query names/page/default-offpage Q15. Delivery selection contract F13 сначала target filter, затем latest number per owner; F04 не пишет выдачу как side effect Publish.


## 4. Application


### 4.1. Handlers / use cases


Отдельные command handlers DF02/DF06–DF11/DF13–DF20 располагаются в Application; query handlers DF01/DF03–DF05/DF12 — в Persistence. Алгоритмы раскрыты в §9. Один validated input → roots/facts/ports → Domain actions → tracking → local commit → DTO/internal metadata. Add/Remove не самостоятельный commit. Rename logical Name не Save version; Draft Save stale разрешён.


`ReturnEntityInstanceToDeliveryCommandHandler` — proposed target Application handler DF20. До authoritative reads открывает owner scope: MVP явная ReadCommitted transaction/global advisory acquire, Demo local mutation/AssetId guard. Затем fresh загружает exact logical owner и retained Published versions с frozen payloads; использует local schema/target facts reader и public F05 eligibility port, в MVP из независимого primary read scope. Проверяет own ExpectedVersion, Withdrawn/lifecycle. При отсутствии retained Published content выполняет recovery только собственного запрета без schema/media read; иначе current admissibility проверяется полностью. Domain ReturnToDelivery получает validated parameter model; один owner conditional commit снимает WithdrawnAt и увеличивает Version. Mapper возвращает prepared logical DTO/own metadata только после CompleteAsync. Handler/validator регистрируются в Application, local reader/guard/repository в Persistence, public F05 adapter/shared guard в Host composition; новый F13 handler/aggregate не вводится.

### 4.2. Validators


По [S18](../../../catalog/docs/standards/dotnet/fluentvalidation.md) composition: CreateEntityInstanceRequestValidator (InstanceName/Key rules+Schema IDs+Values); UpdateInstanceValuesValidator (structure/syntax/presence envelopes+unique occurrence/discard IDs); recursive InstanceValueValidator guided current definitions. Limits берутся Domain constraints; configured Text max и exact wire/key details Q15/Q34/Q36, restricted HTML/sanitization уже выбраны.


Draft допускает missing required; incompatible/orphan authoring values сохраняются согласно Draft profile, не получают silent defaults/coercion. Malformed JSON/unknown union/foreign IDs/conflicting discard не оставляют partial state. Save допускает missing required и сохранённую incompatibility/orphans; newly entered type-invalid value отклоняется. Retained saved work не коэрцируется в новый тип и не удаляется. Strict Publish проверяет full values/orphans/AssetIds; required defaults valid, unknown option invalid; defined Deprecated valid с warning. Async availability/DB facts handler, FV не заменяет guard. Validators не пишут/не меняют DTO.


ReturnEntityInstanceToDeliveryCommandValidator (target name) проверяет InstanceId/internal token; current owner/Published/schema/media admissibility проверяет handler/domain policy после загрузки, не клиентские facts. Validator не пишет и не заменяет guards.

MVP validation состоит из intrinsic type checks и ValueConstraintValidator по exact typed definitions F01. Save/Copy/read возвращают constraint issues без потери values; new type-invalid input rejected; strict Publish requires all valid. Text length после sanitization: concatenate decoded text nodes document order, code points, без browser innerText/CSS/layout-newlines; semantic-empty/operational HTML cap отдельно. Numeric bounds typed inclusive; MultipleOf Integer/Decimal positive and exact, Decimal coefficient/scale арифметика не конвертируется в double и не overflow. Temporal bounds guided exact Naive/Offset definition Q34; Offset inclusive instant bounds, Naive без hidden conversion. Missing optional skip value rules, missing required Draft issue/Publish failure.

TemporalCodec.ParseOffset сначала проверяет fixed lexical syntax (7 digits, uppercase T, explicit ±HH:mm или Z), rejects missing offset/leap second/-00:00/extra digits, затем typed DateTimeOffset и whole-minute ±14/local+UTC range. Z branch явно задаёт zero offset; ±HH:mm branch использует parsed whole-minute offset и explicit Unspecified local date/time, не parser machine-timezone default. ParseNaive имеет отдельный explicit /1 branch, результат DateTimeKind.Unspecified; не DateTimeOffset(DateTime) с implicit machine timezone. Invalid newly submitted temporal scalar отклоняет Save; valid instant вне bounds сохраняется с constraint issue. Exact existing error code/address mapping Q15, новые literals не назначаются.

### 4.3. Mappers


Generated Values → domain snapshot preserves named FieldId, explicit presence/type/decimal/ItemKey/order; anonymous items без fakeFieldId. Current Inherit/Remove/numeric option legacy не переносится. Source-copy mapper maps identities/current saved snapshots, reconciles preserved data с target и генерирует orphan IDs сервером при mutation; source не меняется.


Domain→read DTO full stored content/persisted orphans + non-mutating current issues; raw unknown values не выбрасываются projection. Read projection→summary/list не десериализует весь graph. Runtime canonical projection включает captured immutable AssetReference descriptors и исключает authoring Orphans/Name/configured URLs; hash values-only по Q19. Delivery читает сохранённый Published snapshot без live media enrichment; exact wire Q19/Q30/F13. Internal Version отдельным carrier→header. Post-create result own logical root/initial Draft summary; Save/Copy/Publish full resulting exact DTO/own ETag.


MVP reconciliation совместимого значения в target остаётся lossless при constraint-only incompatibility: value сохраняется на прежнем FieldId и получает issue, не orphan. Definition type/shape/identity mismatch использует existing orphan policy. Mapper не приводит число/строку к допустимому range и не сортирует по target mode. Diagnostics содержат exact target/basis context; GET revalidation не меняет saved Content/Version. New Present type-invalid всё ещё Save failure.

Temporal mapper guided exact definition/binding: authoring DTO string → typed Naive/Offset scalar, reverse authoring formatter сохраняет numeric offset/100ns; serialization input не auto-convert в local DateTime. Publish runtime projection Offset→UTC/Z, Naive→unchanged /1. Definition/instance profile compatibility проверяется до writer; Offset нельзя записать/пометить cm-instance-content/1. New runtime normalized input несёт declared temporal semantics, exact target metadata owner F01/F03; ordinary Copy не конвертирует Naive, не делает matching нового FieldId по Key/Name. Original authoring snapshot сохраняется отдельно от immutable runtime bytes.

### 4.4. Services / algorithms

Целевые reusable MVP components — pure stateless algorithms, не новые persisted модели/сервисы. Typed definitions owner F01, neutral normalized values используют contract bindings Q19; Domain aggregate/DbContext/F05 HTTP в input отсутствуют. Application строит exact value/definition context и переводит existing issue result в Q15 diagnostics.

| Компонент | Input / метод | Result / ответственность |
| --- | --- | --- |
| `ValueConstraintValidator` | `Validate(definition, normalized value, location)` | Применимые scalar/range/text/collection checks; адресуемые issues, без mutation/I/O |
| `PlainTextLength` | `Count(decoded sanitized text nodes)` | Code-point count, entity decoding один раз при HTML parse; actual whitespace учитывается, synthetic separators нет |
| `RuntimeValueEquality` | `Equals(definition, left, right)` | Full typed normalized runtime values; Object FieldId order, FlagEnum set keys, List authored order, absence != default; authoring identity excluded |
| `ListUniqueItemsValidator` | `Validate(item definition, items, locations)` | Hash index candidate → exact normalized value/byte equality confirmation; duplicates issues; incomplete invalid value не считается proven unique |
| `ListOrderValidator` | `Validate(order mode, comparer, item definition, items)` | Adjacent typed keys nondecreasing/nonincreasing; equal allowed; Default без order constraint; missing/invalid key issue |
| `StableListComparer` | `Compare(keys)` / `Sort(items, comparer, direction)` | Pure explicit helper для UI contract/conformance; stable equal-key order, no Guid/ItemKey tie-break; backend Save/Publish Sort не вызывает |

Scalar key types Integer/Float/Decimal/Boolean/String/Enum/Date/DateTime/TimeSpan; String/Enum Key ordinal UTF-16 comparison, false < true, numeric/temporal typed. ObjectFieldPath — required chain из immutable FieldIds только через Objects, required supported scalar leaf. Unsupported direct Text/Color/FlagEnum/AssetReference/Object/List comparator и optional/cross-List/expression/composite paths rejected при Schema Save F01/F02. Partial Draft missing key сохраняет issue; frontend Sort unavailable. UniqueItems проверяет full value, не сортировочный key: разные values с equal key допустимы при uniqueness.

Целевой shared pure `TemporalCodec` принадлежит reusable нейтральной библиотеке Q19, adapter Application без EF/HTTP/timezone/TimeProvider. Методы: ParseNaive(string)→typed validation result DateTime(Unspecified), FormatNaive(DateTime)→exact /1 seven-digit no-offset string, ParseOffset(string)→typed validation result DateTimeOffset; FormatAuthoringOffset(DateTimeOffset)→7-digit offset form (zero +00:00), FormatRuntimeOffset(DateTimeOffset)→UTC7digitsZ; CompareOffset/EqualsOffset→UTC instant; FormatOffsetBound→UTC semantic schema input. Parsing Z сохраняет known zero offset, исходный lexical выбор Z/+00:00 не отдельная business identity. DTO union/envelopes не второй format codec. Explicit converters/formatters обязательны, default serializer/date parser не oracle. Common fixture suite используется backend/UI/SDK adapters; library target compatibility ещё Q29/Q32.

Canonical preparation uses same pure common .NET library as F03, explicit profile+typed normalized values/contract bindings/captured descriptors. FieldId ordering never becomes runtime FieldId properties; no ItemKey/authoring metadata in content digest. Sanitization/Domain normalization approved for Text happen BEFORE canonical input; canonical writer itself never trims/case-folds/Unicode-normalizes content.


Reconciliation сначала индексирует source/target named field definitions по FieldId и compatible value contexts; затем whole snapshot трансформация без mutation оригинала. Object children recursively; List values сохраняют author order/ItemKey; compatible whole list copied, shape mismatch сохраняет affected raw subtree/orphans по Q15. Ни index/Name/Key не восстанавливают field identity. Existing orphans не дублируются на повторном Save/revalidation, explicit discard проверяется по owning record, failed operation не применяет ранние deletions. Исторический/schema context orphan locations Q15 необходим для lossless nested recovery.


Current Draft schema может измениться между form read и Save. Reconcile saved raw snapshot вместе с submitted Values, не только видимые controls; omitted unknown field не означает consent discard. Server current schema snapshot и client-visible context freshness согласуются guard/contract Q15. GET не выполняет Save; stable IDs/content не изменяются за read side.


Publish/canonical/commit-conflict boundary reuse F03 pattern; relevant same-target baseline отдельно от global sequence. App [ContentManagerApplicationModule](../../../../contentmanager.api/src/App/ContentManager.Application/ContentManagerApplicationModule.cs) сейчас registers EditorRepository/reference guard и CQRS/UoW/TimeProvider. Target handlers/validators/mappers Application, repositories/query/guard Persistence, configs EntityFramework, controllers/header/error mapper Api; Host composition подключает реальные F05/public adapters этапа, без обязательного foreign Publishing в Demo.


Text input обрабатывает целевой IRestrictedTextSanitizer до проверки semantic-empty/configured maximum: неподдерживаемые tags/attributes и active content не сохраняются. Empty определяется по оставшемуся смысловому тексту, а не длине HTML markup. Concrete sanitizer adapter/allowed attributes и operational HTML maximum материализуются Q15/Q22, без назначения фиксированного числа. MVP MinTextLength/MaxTextLength semantics выбраны Q35; parser/helper conformance fixtures подтверждают portable text-node count.

DI Application подключает ValueConstraintValidator/PlainTextLength/RuntimeValueEquality/ListUniqueItemsValidator/ListOrderValidator как stateless pure components; FV wrapper uses existing scan, no new BC/rules repository. Web UI независим от Domain/EF: локальная explicit stable Sort реализация либо portable helper follows same contract/fixtures, authoritative validation server. No server Sort endpoint. Definition-only Q35 extensions не меняют runtime /1 encoding; Offset меняет его и требует нового instance profile совместно с MVP schema/formal/C# profiles. Old Published /1 bytes/hash не пересчитываются.

## 5. Api


### 5.1. Endpoints


Транспортная таблица по [S17](../standards/S17-data-flows.md)/[S16](../standards/S16-rest.md). `I=/api/v1/entity-instances`, `R=I/{instanceId:guid}`, `V=R/versions`; version/source IDs Guid route constraints. Доступ D=Demo anonymous, M=MVP Specialist F16/Q10. Существующие target routes прежнего F04 сохранены; Rename route не выбирается по аналогии. Models §3, algorithms §9.


| Поток | Транспорт | Метод / канал | Endpoint / destination | Доступ | Вход | Application action | Выход | Успех | Ошибки |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| DF01. Instance list | HTTP | GET | I | D/M | Search/schema/filter/page/sort | ListEntityInstancesQuery | ApiResponse<OffsetPagedResultDto<EntityInstanceListItemDto>> |200 |400 |
| DF02. Create instance | HTTP | POST | I | D/M | CreateInstanceRequest | CreateInstanceCommand | ApiResponse<InstanceVersionDetailsDto> + initial version ETag |200 Demo create profile |400/404/409 |
| DF03. Logical exact | HTTP | GET | R | D/M | InstanceId | GetEntityInstanceQuery | ApiResponse<EntityInstanceDto> + ETag |200 |404 |
| DF04. Version list | HTTP | GET | V | D/M | Owner/target/status/page/sort | ListEntityInstanceVersionsQuery | ApiResponse<OffsetPagedResultDto<EntityInstanceVersionSummaryDto>> |200 |400; missing owner Q15 |
| DF05. Version exact | HTTP | GET | V/{versionId} | D/M | Owner/version IDs | GetEntityInstanceVersionQuery | ApiResponse<EntityInstanceVersionDto> + ETag |200 |404 |
| DF06. Clean Draft | HTTP | POST | V | D/M | CreateEntityInstanceDraftRequest | CreateCleanEntityInstanceDraftCommand | ApiResponse<EntityInstanceVersionDto> + ETag |200 write profile |400/404/409 |
| DF07. Copy Draft | HTTP | POST | V/{sourceVersionId}/copies | D/M | Copy request, source Draft If-Match | CreateEntityInstanceDraftCopyCommand | ApiResponse<EntityInstanceVersionDto> + ETag |200 |400/404/409/412/428 |
| DF08. Save Draft | HTTP | POST | V/{versionId}/save | D/M | Values/discard IDs, If-Match | UpdateEntityInstanceDraftCommand | ApiResponse<EntityInstanceVersionDto> + ETag |200 |400/404/409/412/428 |
| DF09. Accept base | HTTP | POST | V/{versionId}/accept-current-base | D/M | Own If-Match, base consent profile Q15 | AcceptCurrentEntityInstanceDraftBaseCommand | ApiResponse<EntityInstanceVersionDto> + ETag |200 |400/404/409/412/428 |
| DF10. Publish | HTTP | POST | V/{versionId}/publish | D/M | Own If-Match, body absent | PublishEntityInstanceVersionCommand | ApiResponse<EntityInstanceVersionDto> + ETag |200 |400/404/409/412/428 |
| DF11. Draft delete | HTTP | DELETE | V/{versionId} | D/M | Own If-Match | DeleteEntityInstanceDraftCommand | ApiResponse<Guid> |200 |404/409/412/428 |
| DF12. Logical impact | HTTP | GET | R/delete-impact | D/M | InstanceId | GetEntityInstanceDeleteImpactQuery | ApiResponse<EntityInstanceDeleteImpactDto> |200 |404; external unavailable Q15 |
| DF13. Logical delete | HTTP | DELETE | R | D/M | Logical If-Match, consent Q15 | DeleteEntityInstanceCommand | ApiResponse<Guid> |200 |404/409/412/428 |
| DF14. Archive | HTTP | POST | R/archive | M | Logical If-Match | ArchiveEntityInstanceCommand | ApiResponse<EntityInstanceDto> + ETag |200 |404/409/412/428 |
| DF15. Restore | HTTP | POST | R/restore | M | Logical If-Match | RestoreEntityInstanceCommand | ApiResponse<EntityInstanceDto> + ETag |200 |404/409/412/428 |
| DF16. Retire Published | HTTP | POST | V/{versionId}/retire | M | Own version If-Match | RetireEntityInstanceVersionCommand | ApiResponse<EntityInstanceVersionDto> + ETag |200 |404/409/412/428 |
| DF17. Restore Retired | HTTP | POST | V/{versionId}/restore | M | Own version If-Match | RestoreEntityInstanceVersionCommand | ApiResponse<EntityInstanceVersionDto> + ETag |200 |404/409/412/428 |
| DF18. Withdraw logical | HTTP | POST | R/withdraw | D; M applicability F13 | Logical If-Match | WithdrawEntityInstanceCommand | ApiResponse<EntityInstanceDto> + ETag |200 |404/409/412/428 |
| DF19. Rename logical | HTTP | PUT | R/metadata | D/M | UpdateInstanceMetadataRequest / logical If-Match | UpdateInstanceMetadataCommand | ApiResponse<InstanceDetailsDto> + ETag |200 |400/404/409/412/428 |
| DF20. Return logical instance to delivery | HTTP | POST | R/return | D; M applicability F13 | InstanceId + logical If-Match; body absent | ReturnInstanceCommand | ApiResponse<InstanceDetailsDto> + own logical ETag |200 committed Demo result |404/409/412/428; technical authority failure not success |


### 5.2. ETag / If-Match


Exact logical GET/changes → logical strong ETag; exact version GET/changes → version strong ETag. Save/Publish/Accept/Delete version и Retire/Restore own version token. Archive/Restore/Delete/Withdraw/Rename/ReturnToDelivery logical own root token. Copy Draft source exact token; Published source lifecycle precondition Q15. Clean/create нового resource без If-Match, parent/target checks server guarded.


428 missing required token;412 own stale;409 domain baseline/lifecycle/dependency. Same-target stale отличается от different-target sequence allocation; при valid relevant baseline counter-only race коротко сериализуется и оба Publish успешны. Target schema может измениться при прежнем instance ETag; дополнительная freshness/consent граница Q15, no hidden transport internal Version. List summaries не источник ETag. Successful mutation полный confirmed DTO + new own ETag после commit, без hidden GET для returned IDs/value tree; Demo Create возвращает initial Draft DTO/own version ETag, logical ETag доступен exact logical read DF03.


Own Version защищает запись сохранённой версии. Вычисляемые IsStale, current baseline и ValidationIssues зависят также от другой публикации и mutable schema: они могут измениться без записи в instance version. В Q15 требуется определить границу saved representation, вычисляемой диагностики и HTTP-validator. Не считать неизменившийся own token подтверждением свежести всего такого DTO и не обновлять Version записью внутри GET.


Для Demo принят POST Save §1.1/Q15: server reconciliation/sanitization возвращает полный transformed result и own validator после commit. Full desired snapshot и explicit discard semantics сохраняются; ограничение validator для преобразованного PUT не применяется.


### 5.3. OpenAPI и ошибки


Отдельные create/update/read contracts, recursive Values/presence/ItemKey/orphan IDs, nullable Draft number. No Version/Revision/ExpectedVersion wire, no legacy lifetime target/one-Draft/major-minor. Publish не body Save; target не меняется PUT.


OpenAPI: headers/statuses/body/result/null profiles; concrete ProblemDetails/ValidationProblemDetails/ValidationIssue paths Q15. Existing target codes instance-draft.base-outdated, schema-version.not-published, schema-version.retired, dependency.blocked сохраняются по назначению; остальные не угадываются. NSwag generation/round-trip all types и headers перед UI; generated вручную не правится, second manual form tree не подменяет contract.


Общий Demo transport profile [Q15](../questions/Q15-api-and-source-code.md#согласованный-demo-профиль-authoring-api): issues имеют stable machine code, message и address; адреса named values используют FieldId и ItemKey, а не индекс элемента List<Object>. List queries используют единые paging/sort conventions со stable identity tie-breaker; конкретные names/limits/defaults остаются инженерной детализацией. Binary success остаётся binary, ошибки используют общий error profile. NSwag generation и recursive round-trip обязательны до интеграции UI; схема всех 15 типов и nested Lists проверяется буквально, без сокращённого набора variants.

ETag exact authoring read относится только к сохранённому mutable root. Derived diagnostics имеют отдельную identity basis, включающую exact schema/content/baseline context: изменение diagnostics не имитирует изменение own ETag. Accept и delete consent связываются с показанным context/impact; одного root ETag для этих external facts недостаточно.

MVP Api Save success сохраняет и возвращает constraint-violating type-valid Draft с ValidationIssues; new malformed/type-invalid input — existing error contract. Strict Publish failure адресует bounds/count/uniqueness/order без ZIP/content/side effects. Новые exact diagnostic codes и settings wire Q15; public API не принимает server-normalized hash либо sort command. Original own If-Match обязателен ordinary saved Draft Save, initial Create использует существующий no-own-token contract.

## 6. EntityFramework


### 6.1. DbContext ownership


ContentModelingDbContext/schema content_modeling: logical roots + independent versions, references typed IDs. Content/orphans JSONB; no EF row каждого named value/orphan. Чужие media/Publishing DbContexts не входят local transaction; F13 читает public contract своего этапа, не FK в foreign schema.


### 6.2. Configurations


| Entity | Table / mapping | Keys/indexes | Concurrency |
| --- | --- | --- | --- |
| EntityInstance | entity_instances: id UUID, schema_id, key/name, lifecycle, latest_published_version_id/number nullable, withdrawn_at nullable, audit profile | PK, unique(schema_id,key), index(schema_id,lifecycle) | version int token |
| EntityInstanceVersion | entity_instance_versions: id/owner/schema_version UUID, status, nullable version_number/base ID, content/orphan_values JSONB, created_at, canonical_profile_id/exact canonical_bytes/64lowerhex digest/published_at/retired_at nullable; optional strict same-byte canonical text | PK; unique(owner,number) number not null; index(owner,target,status,number); index(schema_version_id,status) | own version int token |


FK local: instance.SchemaId→schema; version.OwnerId→instance; TargetSchemaVersionId→schema version; baseline→instance version; pointers→version. Restrict/NoAction, explicit allowed removal order; same-owner/target/status semantics domain/application, простой FK их не доказывает. JSONB comparer/converter сохраняет presence/decimal/raw values/ItemKey/orphan stable IDs, не выполняет validation/ID generation. Physical JSONB bytes не hash representation.


### 6.3. Constraints / indexes


Удалить ux_instance_one_draft и required Draft business number. Published number positive/unique global per owner; Draft published fields null, Published complete snapshot/time/hash and no orphan by invariant. Exact CHECK/index names/serializer profile Q20/Q15. Key comparison/limits shared Domain rules; current InstanceFieldConstraints.Key для logical key mapping не считается окончательным target constraint. Schema lifetime restriction отсутствует.


Сохранение global sequence вместе с Publish защищено logical Version, content отдельно own Version. Published profile+exact bytes+digest saved together/no partial triple; optional text exact UTF8 decoding. Lifecycle writes never recompute it; read corruption/unknown profile explicit failure, no auto-repair. DB cascades не уничтожают history; FK финальный guard, не доказательство всей delete eligibility.


MVP persistence сохраняет authored order и type-valid constraint violations в Draft content. ValidationIssues derived from exact target schema/basis, отдельные rule/result rows не создаются; constraint-only violations не populate orphan_values. Publish requires zero blocking issues и атомарный immutable runtime/profile/bytes/digest; constraints-only schema extensions не меняют stored runtime encoding. Converter/comparer не сортирует и не выполняет current validator при historical read.

Temporal authoring converter JSONB сохраняет date/time+offset/7digits, не SQL timestamp-per-field/DateTime→local/JS milliseconds. Exact Published runtime bytes UTC для Offset лежат отдельно; original offset доступен exact authoring read, runtime read canonical UTC. Read projection не восстанавливает offset из текущего timezone; snapshot retains it. Hash generation нового profile использует UTC runtime values, не original lexical offset. DI регистрирует pure TemporalCodec/explicit format adapter в существующей composition, без NodaTime/timezone infrastructure.

## 7. EntityFramework.Persistence


### 7.1. Repositories / query adapters


EntityInstanceRepository/EntityInstanceVersionRepository exact loads и tracking, query adapters/projections/specifications §3.5 отдельно. Command storage не грузит весь граф/все Content для определения next. Exact read returns full saved work+persisted orphans и computed validation против current target; no read-side mutation. Current [EditorQueryHandler](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.EntityFramework.Persistence/Editor/EditorQueryHandler.cs) служит evidence, target projection не повторяет full graph list.


Latest same-target lookup фильтрует owner/target/Published перед sort(max number). No global pointer substitution. Count/impact полный, не выбранная rail page. Schema target lookup всегда принадлежность logical schema; чужие version IDs404/ownership errors consistent profile Q15. Fresh classification после failed commit новый no-tracking context, не old prepared tracker.


### 7.2. Transactions

Для MVP владелец этих commits — ContentModeling. По [утверждённому MVP consistency guard](../questions/Q20-persistence-and-migration.md#mvp-протокол-consistency-guard) все CM replicas используют одну PostgreSQL database и один стабильный глобальный exclusive transaction-level advisory key. Каждый affected owner scope явно начинает ReadCommitted transaction, затем отдельным statement вызывает pg_advisory_xact_lock на **той же physical connection и transaction**, до любых authoritative domain reads и row locks. После успешного acquire заново загружаются own roots/tokens и current foreign facts через public ports из независимых primary read scopes/connections. Предварительные query/preview/cache/replica facts не разрешают commit; foreign read не enlist-ится в ambient owner UoW, не разделяет SQL transaction и не открывает foreign EF из caller.

Guard удерживается до owner commit/rollback: изменяются только roots своего BC и атомарные own outbox/audit intent/inbox markers, где они нужны. Все invalidating writers, включая creation/removal/reference Copy/orphan paths, Publish/lifecycle/hard delete, setup/reconciliation, source durable acceptance/watermark/pending/apply/retry и F15 open/supersede/repair/resolve, участвуют в этом же ключе. Own Version/If-Match сохраняется: stale own token →412, изменившаяся foreign/preview basis →409 по Q15. Create/Copy из неизменённого source не увеличивает source Version; два serially guarded допустимых создания могут оба завершиться успешно.

Внешний I/O (broker, VR School, MinIO), ожидание пользователя и долгие streams выполняются вне gate. CPU preparation допустима заранее, но exact basis повторно подтверждается внутри gate; иной basis требует отказа или новой подготовки вне gate. Bounded guard wait/lock timeout конфигурируются, поддерживают cancellation и являются availability/technical failure, не domain409; точные значения и transport mapping остаются Q15/Q20. Connection loss прерывает owner transaction: reconnect-and-continue запрещён. Потеря commit ACK даёт unknown outcome; выяснение exact state/исходного operation ID по Q15 предшествует повтору, blind command retry запрещён.


Atomic local units: create instance+initial Draft; Save content+orphan/discard+own Version; Copy new Draft snapshot/orphans; Accept baseline/Version; Publish version+global sequence/pointer+own/root Version; logical allowed delete+Draft versions; single owner lifecycle/Rename; DF20 ReturnToDelivery clears only logical withdrawal/own Version. Repositories Add/Remove не коммитят. Любой отказ откатывает всю операцию: частичное удаление orphan и сохранение номера, hash или времени публикации недопустимы.


Local guard всех writers F02/F03/F04 согласует:


- Create/Copy/Save/Publish instance против schema Archive/Delete/Retire/Withdrawal, exact target Draft schema Save и schema-Draft cascade deletion; Withdrawn authoring read/Copy/Save не блокируется withdrawal gate, Publish блокируется.
- Source Draft copy против Save/Delete, current target definition snapshot против переопределённой UI context и reconciliation.
- Logical instance Archive/Delete против new Draft/Save/Publish, version delete против Save/Publish.
- Same-target baseline change против Accept/Publish; global sequence change любого target против Publish.
- DF20 ReturnToDelivery против concurrent owner Withdraw/Archive/Delete/Publish, schema/target lifecycle и asset deletion: Demo local admissibility/AssetId guards либо MVP global transaction advisory key до owner commit; snapshots не переписываются.


MVP isolation/acquire order приняты в Q20/§7.2: явная ReadCommitted owner transaction и standalone global exclusive advisory acquire до всех own/schema/media facts и row locks. Exact port/DI signatures остаются Q15; local conditional writes сохраняются. Demo AssetId guard остаётся собственным согласованным single-process protocol и не держится через UI. Не увеличивать schema Version искусственно при каждом read/new reference ради legacy sharedRevision; нужный guard должен явно обеспечивать eligibility/reference safety.


Same-target concurrent Publish: one success, other rollback/current baseline mismatch409; own Draft save race412. Different-target Publish при unchanged relevant baseline коротко сериализует allocation/commit global sequence; оба commits успешны с разными следующими номерами, без counter-only user conflict. Serialization повторно проверяет relevant baseline/own token/schema context и не принимает другой same-target base или новый Draft content молча.


Commit выполняется [UnitOfWorkBehavior](../../../../contentmanager.api/src/BuildingBlocks/ContentManager.Cqrs.UnitOfWork/Behaviors/UnitOfWorkBehavior.cs) после handler, поэтому failure classification boundary после CompleteAsync rollback/disposal, fresh scope как F03. Приоритет: missing404, own token412, same-target baseline409, lifecycle/dependencies Q15; counter-only serialization не user conflict; global pointer change не доказательство stale. Constraint23505 классифицируется конкретно: duplicate logical key409 отдельно от duplicate sequence/other failures, без blanket concurrency mapping.


Impact preview не удерживает transaction. Delete command rechecks all local records/published/history; logical hard-delete только no Published/history, explicit Draft set, last Draft delete не auto parent removal. F02 schema Draft-version cascade удаляет только явно подтверждённые dependent Draft versions, сохраняет ставшие пустыми logical instances; отдельный logical-instance Delete требует своего explicit consent. External ref/media check и local commit не объявляются атомарными вместе: отсутствие refs/availability snapshot недостаточно против concurrent reference/removal; safe coordination/retention protocol §8/Q15, fail closed при невозможности получить authoritative facts. Demo не зависит от отсутствующего full Publishing, но использует реальные локальные invariants/F05.


Create/clean-create/copy/upload используют клиентский operation ID и durable result у owning BC по [Q15](../questions/Q15-api-and-source-code.md#согласованный-demo-профиль-authoring-api). Одинаковые operation ID и input возвращают исходный committed result с теми же identities; иной input для того же ID — conflict. Запись результата и создание локальных roots фиксируются атомарно; для upload учитываются durable stages F05. Потерянный response восстанавливается повтором той же операции, а не новым Create. Точный carrier/header, input comparison representation и result model — Q15; это целевой контракт, не факт существующей реализации.

### 7.3. Migrations


Target rebuild own roots/tokens/nullable numbers/current global pointer, baseline/orphans/lifecycle/withdrawal и indexes/FKs/JSON profile; remove lifetime target/one-Draft assumptions. Clean PostgreSQL migration/round-trip/constraint/race tests. Q20 разрешает destructive refactor для ещё незапущенного CM, production migration не добавляется молча. Аналитика не выполняет DB migrations.


## 8. External adapters


### 8.1. F05 media boundary


Strict validation обходит schema/values рекурсивно (AssetReference/List/nested Object/List<List>) и gathers distinct active references как `(AssetId, RequiredAssetKind)`, затем вызывает public F05 availability/compatibility contract. Bytes не скачиваются; kind проверяется по authoritative MediaLibrary metadata, а не по filename/MIME из client value. Неразрешённый/незавершённый asset не даёт Publish success; отказ bytes download позднее не изменяет persisted reference identity. Authoring write содержит AssetId; submitted descriptor metadata не является authority. При Publish под Demo AssetId guard либо MVP global advisory guard через F05 public boundary захватывается согласованная immutable metadata в runtime descriptor; он сохраняется в Published values и входит в hash. Delivery читает этот snapshot без повторного enrichment. CM/storage URL не сериализуется в runtime values: SDK строит URL из CM configuration + AssetId. Transient authoring UI DTO может содержать current descriptor/ContentUrl из F05 query, но это отдельная read projection, не runtime payload/hash. Orphan/source definition provenance для обнаружения Draft asset refs и безопасного delete F05 — Q15/Q21, без guessing Guid-строк как assets.


Existing App ContentReferenceGuard читает foreign DbContexts, target adapter не копирует этот coupling в F04. Demo сохраняет MediaLibrary BC boundary и использует shared application-level AssetId guard для всех reference writer paths и Delete через owning UoW commit (Q21); Demo multi-replica не допускается; MVP использует выбранный global PostgreSQL transaction advisory guard §7.2 через owner commit. Availability call выполняется после acquire через независимый primary read scope, не cross-BC transaction.


### 8.2. MVP historical reference boundary


Delete-impact/eligibility через public Publishing read/reference port, не direct SQL joins. Missing/unavailable authority → fail closed destructive action, error profile Q15. Новый reference writer должен участвовать согласованном eligibility/retention protocol; обычный precheck не защита от гонки. Historical Published invariant уже блокирует logical delete локально. F04 не пишет Release/Revision/assignment/moderation state.


### 8.3. Canonical/hash и Delivery

Reuse [F03 neutral canonical contract/library](F03-publish-schema-version.md#33-dto-и-parameter-models), cm-instance-content/1 and [Q19 v1](../questions/Q19-content-hashes.md#принятый-canonical-profile-v1). Explicit typed normalized runtime inputs, never generic JSON-number interpretation: exact Int64/Decimal, finite Float RFC8785 §3.2.2.3/AppendixB, preserved Unicode/escape rules, UTF8noBOM/no whitespace/no finalLF. NDJSON LF framing not content hash. Unknown profile/mismatched digest/corrupt savedbytes fail BEFORE group apply; no automatic historical rehash/repair.

Object members use captured schema FieldId lowercaseD ordinal for ordering while emitted runtime property names remain approved schema Keys; no FieldIds emitted into values-only hash. Enum/Flag immutable keys ordinal, runtime List order preserved incl already stored Asc/Desc, no implicit canonical sort. Presence absent/null/default distinct; required/type-invalid null never silently default. Values-only excludes InstanceId/SchemaVersionId/Key/versionnumber/labels/orphans/ItemKey/own Version/hash itself; envelope/group integrity separately carries needed identities/profiles. Canonical input includes exactly RuntimeAssetReferenceDescriptor five fields/order above, no FileName/Format/dimensions/duration/URL/current enrichment; fixed byte layout/literal asset vector Q19.

Profile/bytes/digest saved as one immutable triple with successful owner Publish; byte-changing newprofile/no historical rehash. CPU canonical/hash preparation outside gate may use preliminary non-authoritative snapshots; final DemoAssetId or MVP first globalgate fresh rechecks exact draft/target/baseline/descriptor authority and prepared basis, stale own412 vs independent409 remain. Different basis discards prepared result/reprepares outside gate; no port failure becomes compatible empty.


Local canonicalizer/hasher/TimeProvider, profile [Q19](../questions/Q19-content-hashes.md#f04-профиль-значений-экземпляра). F13 наблюдает committed exact Published snapshots и logical Withdrawn через public boundary; withdrawal приводит Remove в следующем новом direct-sync plan, no broker/network request side effect этой команды. Handling уже зафиксированных plan/frozen history/F13 session отдельно Q30/Q33/F13. Archive/Retire не physical GC.


## 9. Data flows

MVP порядок для DF02, DF06–DF11, DF13–DF20: input/CPU preparation вне gate → явная owner ReadCommitted transaction → standalone exclusive advisory acquire → fresh own roots и foreign primary-port facts → проверки own token/basis/lifecycle/dependencies → Domain action → только own writes/atomic intents → commit и release. Это обязательный префикс каждого указанного mutation/retry stage; строки «load/guard» ниже означают загрузку **после** acquire. Read-only query/impact/preview остаются observations; при необходимости короткий source capture имеет отдельный guarded scope, но долгий stream и пользовательский review не удерживают guard. Demo использует свой согласованный контур без новых обязательных MVP dependencies.


Ниже внутренние algorithms связаны с DF §5. Api binding/header parsing и FV input validation до command; root writes/side effects не выходят за local boundary §7. Successful DTO/ETag только после commit. Query no mutations; cancellation до commit rollback, после commit lost response не rollback.


### DF01–DF05. Чтение / создание workspace


DF01: validate query → atomic list specs search Name/Key/schema/lifecycle → count без paging + stable sort/page list projection → envelope. Без загрузки snapshots всех versions. DF03: exact logical root/no404 fallback → metadata/current global pointer/summaries по выбранному Q15 profile → logical DTO/own ETag. Logical archived/parent-archived context остаётся readable, authoring availability отдельно.


DF04: owner exists/profile missing owner Q15 → same query filters/count/stable Draft-first/Published-number sort → summaries. DF05: exact owner+version → stored Content/Orphans/Version + current target schema facts + latest same-target Published → pure revalidation/derived issues/orphan candidates → exact DTO/own ETag. GET ничего не Save/Discard/generate persistedIDs, no default substitution. Derived diagnostics freshness не равна own token change.


DF02: guard/load Active schema/exact permissible target of this schema → Name/Key validation + ExistsBySchemaAndKey → server InstanceId/VersionId/CreatedAt → Draft-profile Values reconciliation/preserve incompatible work → new logical root/initial Draft, base=null для нового owner → Add обеих roots → one commit → EntityInstanceDto/logical ETag с exact initial Draft summary. Unique race конкретного index→duplicate-key conflict; no orphan/history partial write. Получение initial exact version ETag — DF05, не угадывается из parent header.


### DF06–DF07. Clean / copy Draft


DF06: Active logical owner+schema/current target eligibility under guard → latest Published same owner/target (в том числе null) → server VersionId/time → empty/unset instance Content/no orphan → CreateDraft/commit → exact DTO/new own ETag. Empty instance data Draft допускается, это не исключение min schema fields F01/F02.


DF07: exact source same owner; source Draft If-Match/source lifecycle profile; согласованные source/target schema snapshots → target того же logical schema/eligible → source content+already persisted orphan → mapping FieldId/compatible recursive definition, no Name/Key/position migration → preserve unmatched/incompatible raw values/new orphanIDs → new owner-version ID/time, immutable target, baseline latest по TARGET независимо от source/global pointer → Add/commit → exact new Draft DTO/ETag. Orphan-копии получают новые server IDs для нового version owner, сохраняя значения и контекст исходных записей; source unchanged, newtargetfields absent. Unknown nested/provenance mapping не заменяется coercion; Q15.


MVP Clean/Copy дополнительно валидирует type-compatible values против target constraints: сохраняет их и issues, не orphan/clamp/default. Повторная validation не создаёт orphan из range/length/count/order. Source snapshot/ItemKeys/order прежние, new Draft source tokens/owner guard existing.

Temporal Copy: immutable semantics change требует нового FieldId по F01/F02. Source Naive removed field сохраняется orphan; new Offset field пустое. Specialist вручную заполняет value/offset, обычный Save/Publish validation; no offset guessing/automatic migration route. Same compatible semantics copied by identity preserves authored offset/precision/source.

### DF08. Save Draft

Load exact Draft/own ExpectedVersion + parent authoring availability/current target schema под guard → validate submitted diagnostics basis → принять FULL desired snapshot authoring tree → reconcile saved raw work/orphans → validate explicit persisted-ID/computed-token discards → validate syntax/variants/ItemKey и newly entered values → SaveDraft Content+Orphans+own Version → conditional commit → full resulting DTO/ETag. Absent optional named field означает clear normal value. Collapsed/unrendered nodes клиент удерживает в полном request; partial rendered tree не является complete snapshot. Persisted/derived orphan удаляется только explicit discard. Missing required и retained incompatible work разрешены; newly entered type-invalid input отклонён. No Demo PATCH; target/Key/Name/baseline не меняются. Stale basis требует refresh/review, не применения discard к другой schema/content.

MVP Save после intrinsic/reconciliation подготовки выполняет ValueConstraintValidator + uniqueness/order checks на exact current definitions. New type-invalid input → rollback; type-valid additional violations → commit Draft values + resulting issues/own new ETag. Массив сохраняется ровно в явно submitted порядке. UI Sort только локально; Save заново валидирует submitted order и не исправляет его. Current target changed bounds revalidation повторяется внутри существующего owner guard, исходный own token не заменяется автоматически.

### DF09. Accept same-target base

Exact Draft ExpectedVersion + shown baseline/context consent basis → latest Published same owner/target и exact schema/content facts под guard → проверить совпадение basis, ownership/structure/evolution → AcceptCurrentBase только BasePublishedInstanceVersionId/Version → commit → DTO/ETag. Missing required и retained incompatible/orphan work разрешены; Accept не strict Publish. Content/Orphans/target прежние, нет auto merge/Publish/hash/number. Changed shown context отклоняет действие и требует нового review; global latest другой target не заменяет same-target baseline.

### DF10. Publish instance version

0. CPU normalization/canonical bytes/hash preparation outside gate using preliminary non-authoritative draft/target/descriptor projections; hold no stream/storage/foreign UoW. Prepared result is never commit authority until fresh checks below.

1. MVP: начать owner ReadCommitted transaction, standalone global advisory acquire; затем fresh load exact Draft+logical instance+schema owner/exact target. Demo использует local guarded UoW. Compare own ExpectedVersion до mutation; Draft/active instance/schema, published eligible target schema.
2. Read latest Published instance version именно same target; compare saved baseline, mismatch409 instance-draft.base-outdated. Global pointer/number отдельно read для sequence.
3. Revalidate full saved payload against exact Published schema, derive any current orphan/type issue; require persisted/derived orphans empty, all required/type rules §2. Не применять reconciliation, меняющую Content, молча в Publish; сначала explicit Save если нужна коррекция.
4. Collect distinct `(AssetId, RequiredAssetKind)` recursive values; Demo acquire shared application-level AssetId guard, MVP уже держит global advisory guard шага1. Через F05 public boundary получить authoritative availability/kind и exact five-field RuntimeAssetReferenceDescriptor metadata; MVP читает primary в независимом read scope без foreign UoW enlistment. Error/wrong kind/unconfirmed authority блокируют Publish. Захватить immutable five-field RuntimeAssetReferenceDescriptor values; guard удерживается через owning UoW commit. No bytes downloads и no serialized CM/storage URL.
5. Fresh guarded check of outside-gate prepared typed runtime/captured-descriptor basis and explicitly supported target-bound instance ProfileId+Bytes+Digest (Demo/historical /1, Offset новый MVP profile); basis drift discards preparation/rechecks outside gate, no stale permission. Checked next global number/server PublishedAt→InstancePublishParameters; version.Publish + instance.RegisterPublishedVersion. Same immutable triple committed, no reserialize/hash via JSONB. No Name/orphans/runtime URLs in entity payload.
6. Atomic own commit both roots including immutable profile/exactbytes/digest and conditional own+parent Versions/guards/unique sequence. Same-target loser rollback/classify stale409, own writer race412; different-target allocation/commit коротко сериализуется по logical instance sequence, оба relevant-baseline-valid Publish успешны. Не скрывать эту категорию под stale-base и не auto принять другой baseline.
7. Full exact Published DTO/new version ETag; own version Id/target сохраняются, no external Publishing/Activate side effect. Logical pointer refresh отдельный read. Other target Draft не stale от этой publication, global number вырос.


MVP Publish дополнительно применяет strict full constraints/count/uniqueness/order к loaded exact values; final runtime equality проверяет complete normalized representation с frozen media descriptors шага4 до canonical preparation/transition. Missing/incomplete/invalid key, duplicates, descending/ascending violation блокируют whole Publish. No sorting/dedup/rounding; equal comparer keys допустимы. Повторная validation и captured immutable runtime basis находятся в existing preparation/owner-guard protocol, не создают дополнительные writes/media download.

Temporal DF08 сохраняет parsed authored offset/100ns и resulting DTO после обычного owner guarded Save; valid extra-bound issues не теряют value. Temporal DF10 перед canonical transition строит UTC7digitsZ runtime projection для Offset и применяет supported new instance profile; Naive old input old profile branch unchanged. Equivalent Offset instants produce same runtime bytes/hash; no original offset in runtime identity. Authoring Content snapshot остаётся immutable со своим offset, runtime bytes/hash отдельны; сохранение обеих частей атомарно в existing owning transaction. List equality/order используют instant, stable equal ties; no server autosort.

### DF11–DF13. Delete / impact


DF11: MVP explicit owner ReadCommitted transaction/global advisory acquire, Demo local guard против Save/Publish/schema cascade → fresh exact Draft/ExpectedVersion → no Published/retained refs → remove exact Draft explicit → commit → deleted ID. Удаление последнего Instance Draft разрешено и оставляет пустой logical parent; no automatic removal parent/перенумерация. Failed remove rollback. Published version никогда не hard-delete.


DF12: logical owner → total current Draft count/Published history/local refs + applicable authoritative historical boundary MVP → informational CanDelete/Blockers. No transaction через confirmation. DF13: logical own If-Match/consent → guarded exact owner/history/current dependencies reread → forbid any Published/history → explicit allowed Draft version removals FK-safe → logical Remove/one commit → deleted ID. Changed exact impact set/basis отклоняет Delete и требует нового preview/confirmation; preview/count не authority. Instance removal не удаляет schema/assets/external refs, no implicit historical cascade.


### DF14–DF17. MVP Archive/Restore и Retire/Restore


DF14/DF15: load logical owner/ExpectedVersion → lifecycle/parent effective availability/guard → Archive/Restore logical only → conditional commit → logical DTO/ETag. Archive скрывает new authoring/assignments; child own lifecycle/history не массово меняются. Restore не Publish/Activate, childRetired/parentArchived/Withdrawn остаются независимыми conditions.


DF16/DF17: exact Published version/ExpectedVersion → lifecycle/new usage coordination → Retire(server now)/RestoreRetired → timestamp/own Version conditional commit → DTO/ETag. Snapshot/hash/number/time/history неизменны. New assignments/releases use eligibility public boundary, retained old revisions сохранены. Different-target baseline/sequence не пересчитываются с исключением history по retire.


### DF18–DF19. Withdraw / Rename logical


DF18: exact logical owner/ExpectedVersion → applicability/current withdrawal и retained Published content checks (Draft-only/empty rejected409 без mutation) → WithdrawFromDelivery(server now) → local commit → logical DTO/ETag. Все direct representations данного owner на всех schema targets exclude desired state в F13; следующий новый plan Remove; existing Published rows/history и уже frozen plan не переписываются. Already fixed plan/frozen historical handling F13, не обещание network delivery прямо в этом response. Возврат — отдельная explicit операция: при Published history проверяет admissibility уже Published representations; без retained Published выполняет recovery только собственного запрета. Publish не очищает withdrawal; Withdrawn read/Copy/Save разрешены, Publish/runtime блокируются. ReturnToDelivery mutation выполняет ContentModeling F04, F13 наблюдает availability для следующего нового plan. Exact route/request и repeated outcome Q15.


DF19: exact logical root/ExpectedVersion/availability → Name VO validation → Rename → own audit/Version commit → logical DTO/ETag. Key/target/content/hash всех versions прежние; runtime отображаемое имя моделируется SchemaField, не logical Name. Method/route не выбирается из текущего version Save, Q15.


### DF20. Return logical instance to delivery

1. Api связывает exact InstanceId и required logical If-Match во внутренний ExpectedVersion. Request не передаёт values/snapshot/hash/number; exact URI/method/body signatures материализуются Q15. Missing token428; malformed parser mapping Q15.
2. Application открывает owning ContentModeling UoW: MVP начинает ReadCommitted transaction и standalone global advisory acquire до facts, Demo берёт короткий local mutation guard; затем загружает fresh exact logical instance: missing404, own stale412; проверяет Withdrawn и current logical lifecycle. Already-returned/repeated/no-op outcome остаётся exact transition/error materialization Q15, не silent bypass token.
3. Local facts reader определяет retained Published representations этого owner. Если их нет, recovery снимает только own withdrawal после проверки own token/lifecycle: перейти к шагу5 без schema/media eligibility reads; Draft не валидируется и runtime content не создаётся. Иначе загружает schema owner/exact targets и current lifecycle. Проверяет applicability/ownership и admissibility возвращаемого candidate set по согласованным F13 selection rules, без замены blocked latest произвольной старой version. Inadmissible/missing required current facts отклоняют return; Draft values и strict Draft completeness здесь не участвуют.
4. Из frozen Published values/captured descriptors получить distinct AssetIds/required kinds. В Demo stable order взять общий AssetId guard; в MVP удерживается global transaction advisory key шага2, public MediaLibrary port в независимом primary read scope batch-проверяет current existence/availability/kind. Недоступная authority fail closed, no bytes download; current metadata не заменяет captured descriptor. Соответствующий Demo local/schema/AssetId guard либо MVP global transaction advisory key удерживается до owning commit; повторные fact checks исключают гонку lifecycle/delete.
5. Сформировать ReturnToDeliveryAdmissibility и вызвать EntityInstance.ReturnToDelivery. Меняются только owner WithdrawnAt и own Version; versions/content/captured descriptors/hash/PublishedAt/business numbers/global pointer прежние. Conditional owner write и CompleteAsync фиксируют один local commit; любой отказ оставляет withdrawal и все snapshots прежними. No broker/full Publishing/network sync side effect в Demo.
6. Post-commit Api возвращает confirmed EntityInstanceDto + новый logical ETag без hidden GET. F13 потребляет committed availability при создании следующего НОВОГО plan; существующий frozen plan/retained history не переписываются. Lost response восстанавливается exact logical read, не повторной публикацией.

## 10. Ошибки и диагностика

MVP guarded failure различает confirmed business refusal и невозможность проверки: unavailable/incomplete public facts не означают empty/valid facts. Guard wait cancellation/timeout и connection loss — technical/availability outcome без domain Conflict; точные wire codes остаются Q15. После connection loss scope завершается rollback/disposal без продолжения на новой connection. Unknown commit outcome выясняется по exact state/operation ID; prepared tracker не подтверждает ни success, ни rollback. Own stale ETag сохраняет412, foreign basis change —409. Диагностика фиксирует wait/hold duration, owner/operation, acquire/commit/rollback/unknown phase без full payload и секретов.


| Условие | Наблюдаемый исход | Данные / восстановление |
| --- | --- | --- |
| Invalid syntax/variant/duplicate occurrence/discard request |400 paths/issue shape Q15 | No partial Content/orphan deletion |
| Missing/foreign owner/version |404/ownership profile Q15 | Без fallback на latest/dругой target |
| Duplicate logical Key |409 concrete constraint/machine code Q15 | Весь create rollback |
| Own If-Match missing/stale |428/412 | No overwriting unseen edits; explicit reload |
| Relevant baseline outdated |409 instance-draft.base-outdated | Draft прежний; no assigned number |
| Target Draft/not Published/Retired |409 schema-version.not-published/schema-version.retired; withdrawn profile Q15 | Draft retained; no Publish |
| Strict values/orphans/unavailable assets |400/409 final category Q15 | No number/hash/published time; saved work preserved |
| Local Published/history/dependency blocker |409 dependency.blocked / exact profile | No partial delete/history cascade |
| Different-target counter allocation |Коротко serialized sequence; оба relevant-baseline-valid Publish успешны, не stale-base | No false user counter conflict, consecutive committed numbers |
| ReturnToDelivery inadmissible current owner/schema/target/media |Common issue/code/status materialization Q15 | Owner остаётся Withdrawn, snapshots/hash/number прежние |
| External authority/storage failure |Q15, not synthetic validation success/empty refs | Fail closed required destructive/Publish checks |
| Commit success/response lost |Unknown client outcome | Durable result, exact read/manual recovery, no blind retry |


Correlation/DF/UoW, InstanceId/VersionId/target, baseline/current same-target/global sequence category, orphan count/asset count, assigned number только success, duration/outcome. Без full dynamic values/rich-text/media URLs/секретов. Read-side issues не successful mutation. Current editor.concurrent_change/invalid_operation не единственный target error profile.


## 11. Тестирование

Canonical literal [v1 fixtures](../canonical-v1-vectors.md): all15 types, exact Int64 extrema/Decimal trailing scale/Float subnormal+RFC8785 edges/-0/NaNInfinity rejection, strings/unicodecontrols/duplicates/invalidUnicode/trailinggarbage, absence/null/default, FieldId-vs-Key/Enum+Flag ordering, nested List and stored Asc/Desc, Asset captured fields/no URLs/ItemKey/orphan exclusion. Expected bytes/digest literal, never productionhelper. PostgreSQL owner atomic profile+bytes+digest guard/rollback; SDK unknownprofile/corrupt bytes refused before local group commit/no autorepair. No runtime test run claimed.


Стратегия проверок целевой реализации по [S04](../standards/S04-testing.md). Unit SUT Domain/validator/reconciliation/mappers/canonical adapter, controlled clock/ports doubles для orchestration. Integration SUT реальные EF/UoW/guard/queries с PostgreSQL Testcontainers; external media/history doubles только явно для local-boundary behavior, не proof всего retention protocol. Functional API WebApplicationFactory + real PostgreSQL, Demo без JWT. Architecture physical references/handler placement. Real media/content/delivery контур проверяется отдельной Integration/E2E при необходимости, mocks не E2E.


Собственные owner/schema/instance/version IDs, per-test isolation/cleanup fixture; no shared mutable seed. Races separate contexts/connections/barriers, no Sleep; commit/failure classification реального UoW. Timestamps PostgreSQL precision. Schema change/orphan tests проверяют raw unchanged saved data и GET no writes/Version bump, а не лишь наличие validation warning. Golden bytes/digest approved Q19 буквально fixtures, не production helper as expected.


| ID / DF | Уровень / SUT | Данные / воздействие | Ожидаемый результат |
| --- | --- | --- | --- |
| T01 / DF02 | Unit + Integration + Functional | Create по Draft/Published eligible schema, duplicate key same/other schema | Atomic instance+initial Draft; null number/base, IDs server; same-schema unique409, different schemas allowed |
| T02 / DF01,03–05 | Integration query | Multiple Draft/targets/Published/pages/archived/exact foreign | Stable filtered counts/rail, no full graph list, own correct ETags, exact no fallback |
| T03 / DF06–07 | Unit mapper + Integration | Source Draft/Published, same/cross-target, matched/unmatched/new fields | Mapping FieldId only, target immutable; missing values empty, existing/new orphan data preserved, source unchanged |
| T04 / DF05,08 | Integration + Functional | Mutable schema removes/changes field before read/Save; stale UI omits hidden data | GET no writes/random IDs; lossless raw projection, Save preservation/discovery/discard per Q15 |
| T05 / DF08 | Unit + Integration | Duplicate occurrences/foreign orphan IDs/repeated Save/last invalid discard | No accidental orphan duplication/loss, own-record check, whole rollback, explicit discard only |
| T06 / DF08,10 | Unit value policy + Functional | Missing/null vs0/false/zero-negative durations/empty Flag/List/transparent Color | Defaults valid; Draft missing allowed; strict Required enforced; newly entered present type-invalid rejected; retained incompatible work preserved |
| T07 / DF08,10 | Unit + Integration | All15 types/nested Object/List<List>/ItemKey/Decimal/Text/Date | Recursive presence/semantic types, no numeric option/bitmask/offset Demo coercion; unknown wire profiles not guessed |
| T08 / DF08–10 | Integration + Functional | A published number1, B number2; A Draft baseA1; next A publish3 | Global1,2,3; B publish not stale A; A newer publish makes A sibling stale, same-target queries correct |
| T09 / DF09 | Unit + Integration | Stale valid/invalid Accept, different-target latest, changed shown base | Only relevant baseline/Version changes, no merge/payload/orphan change; shown exact basis checked; missing required/retained orphan allowed |
| T10 / DF10 | Integration + Functional races | Same-target sibling Publish/own Save/Delete, rollback second-root failure | One winner/stale409, own token412, no partial number/hash/time/pointer; no failed-attempt gap |
| T11 / DF10 | Integration races | Different-target concurrent Publish; schema lifecycle/cascade vs content | Never false stale from global pointer; оба valid Publish committed consecutive numbers; closed/deleted target guarded |
| T12 / DF10,F05 | Integration adapter + real contour | Recursive AssetRefs, unavailable/batch refs, concurrent asset removal, immutable metadata capture | F05 public facts и captured descriptor under Demo AssetId guard либо MVP global advisory key through owner commit; descriptor values/hash frozen, no runtime enrichment/serialized URL; mocks alone not full proof |
| T13 / DF11–13 | Integration + Functional | Draft-only logical owner vs Published/history refs/external unavailable/change set | Allowed explicit atomic delete only; no schema/assets/history cascade; preview not authority; last Instance Draft удаляется, empty parent retained |
| T14 / DF14–19 | Unit + Functional + Integration | Rename/archive/restore/retire/withdraw, parent archived, retained history | Correct owner ETag/state; immutable snapshots; Restore no Publish/unwithdraw; next direct sync Remove F13 |
| T15 / all writes | Functional actual HTTP/OpenAPI | Required/missing/stale If-Match, own vs other owner, full returned DTO |428/412 vs409, no Version/Revision wire, new own headers, create exact initial identity, no hidden write-result GET |
| T16 / DF08,10 | Integration storage/canonical | JSONB presence/decimal/raw orphanIDs/nested order; approved byte vectors | Lossless round-trip; same hashed saved values/immutable asset descriptors; authoring metadata/configured URLs excluded; changed live media read не re-enriches Published runtime snapshot |
| T17 / all | Architecture/Functional composition | Physical layers/public boundary/Demo minimal contour | Commands Application, queries Persistence, no Domain EF/HTTP/foreign context/fullPublishing/auth/broker dependency |
| T18 / writes | Functional + UI/API | Lost response/doubleclick/context switch | Durable exact recovery without blind retry/false rollback, UI keeps correct context |
| T19 / DF18,DF20 | Unit + Integration + Functional | Draft-only/empty Withdraw rejected без mutation; legacy Withdrawn без Published recovery; Withdrawn owner с valid/inadmissible Published/schema/media, missing/stale own tag, authority failure | Only WithdrawnAt/own Version change after admissible commit; recovery без Published не требует schema/media и не публикует Draft; logical DTO/new ETag,428/412/404, rejection leaves withdrawal; no snapshot/hash/number/descriptor changes |
| T20 / DF20,F05,F13 | Integration real guards + Functional contour | Return vs concurrent Withdraw/schema lifecycle/asset Delete; new plan vs already frozen plan | Eligibility/AssetId guards through owner commit; next NEW plan consumes committed return, old frozen plan unchanged; no broker Demo dependency |


Существующие тесты и валидатор не доказывают реализацию принятых правил multiple Draft, orphan и конкуренции. Frontend F04 проверяет editor/state/accessibility; targeted pipeline/schema/media/Delivery real contour доказывает только запущенные boundary scenarios. Не выбирать Q36 unknown policy в expected tests молча.


Дополнительная target acceptance matrix: FULL Save clears absent optional, сохраняет collapsed/unrendered values и explicit-only orphan discard; retained invalid work/missing required Save/Accept allowed, new type-invalid rejected; ItemKey add/Copy/Reorder/duplicate tests на nested List<Object>; computed location tokens deterministic и stale basis discard rejected; Withdrawn read/Copy/Save allowed; explicit return checks Published eligibility либо recovery собственного запрета без Published; новый Draft-only Withdraw rejected; Publish blocked/no reset; last Instance Draft removal leaves empty parent; defined Deprecated options valid with warning; concurrent different-target valid publishes оба committed consecutive numbers. Functional OpenAPI→NSwag round-trip охватывает буквально все 15 типов и nested Lists; PostgreSQL race checks используют barriers, не Sleep. Demo media reference writers проверяются под общим AssetId guard до owning UoW commit (F05).

MVP Integration matrix для DF02, DF06–DF11, DF13–DF20 использует real PostgreSQL, отдельные owner connections/UoWs и barriers, включая разные CM replica processes/hosts там, где проверяется общность key. Doubles/in-memory locks не доказывают SQL coordination.

| MVP сценарий | SUT / воздействие | Наблюдаемый результат |
| --- | --- | --- |
| Guard до чтений | Реальный Persistence adapter; writer A держит gate, B начинает owner transaction до acquire; A меняет eligibility/basis и commit, B продолжает | B после acquire читает новый committed state при ReadCommitted; stale предварительные facts не применяются; lock statement использует owner connection/transaction |
| Меж-BC write/read partition | Public read adapter и Host/DI; owner держит guard, foreign primary read отдельным scope | Read не enlist-ится в owner transaction, не читает cache/replica; no foreign writes/EF access; atomic commit только owner + intents |
| Commit/rollback/release | Barriers для invalidating writer и affected operation; injected owner failure/cancellation | Второй writer ждёт release и revalidates; failed owner полностью rollback, нет partial roots/intents; unrelated next scope получает key |
| Wait и connection failure | Configured bounded wait/cancellation, разрыв owner physical connection | Technical/unavailable, не409/empty success; aborted transaction не продолжается через reconnect |
| Unknown commit ACK | Разрыв response/commit acknowledgement и inquiry exact state/original operation ID | Committed result сохраняется, rollback не предполагается; no blind duplicate mutation |
| Own token и foreign basis | Конкурирующий own writer против изменения только foreign basis |412 отдельно от409; оба допустимых Create из unchanged source успешны, source Version прежний |
| Reference/orphan safety | Реальные ContentModeling/MediaLibrary writers с barriers: Save/Copy/Publish/discard/Return против BeginDelete/Archive | Current complete usage и eligibility согласованы до commits; Deleting отвергнут новым reference; discard удаляет только own explicit work, snapshots/hash не меняются lifecycle |

MVP fixtures: type-valid range/length/count/order/duplicate issue Save success + Publish failure; new intrinsic type-invalid rejection; Copy retains constraint-invalid values без orphans; Unicode supplementary/combining, decoded entities/nested HTML/br actual whitespace; Integer extremes/Decimal scale exact MultipleOf incl intermediate overflow edge, Float bounds/no Float MultipleOf; temporal negative/boundary; optional absence/default; Object duplicates разных ItemKey; FlagEnum set/nested List order; ascending/descending/equal keys/required path/missing key; explicit stable UI Sort followed ordinary stale If-Match failure; old runtime /1 bytes unchanged. SUT pure components with literal expected results, PostgreSQL Draft/Publish snapshot/concurrency, existing Api Functional round-trip and frontend helper conformance. Production expected data не строится тем же equality/comparer в assertions. Тестовая стратегия не является отчётом выполненных application tests.

Temporal matrix: Offset save/reopen exact numeric offset и 100ns incl untouched other-field edits; runtime UTC rollover; equivalent instants bytes/hash/equality/unique/ties/bounds; ±14/end-of-date-range/invalidUTC; missing offset/leap/-00/6-or-8 digits reject; Naive Unspecified never host-converted; manual Copy old orphan/newblank; no SQL precision loss; old /1 preserved and Offset rejected by /1 writer. Unit pure codecs with literal expected strings, PostgreSQL snapshot+runtime atomic persistence, Api generated round-trip; full new profile golden codecs/framework/Unity runtime проверяются отдельно до release.

## 12. Порядок реализации


1. Уточнить необходимые для выбранной операции профили Q36; recursive/presence/orphan/freshness/error contracts Q15; implement accepted cm-instance-content/1/common typed library/literal conformance; physical storage/DI Q20 при принятом MVP guard.
2. Реализовать Domain roots, constraints, значения, orphan, reconciliation и строгую validation; проверить сохранение данных и presence-инварианты в Unit.
3. Реализовать EF/UoW, query projections, specifications, guards и migration; проверить PostgreSQL round-trip, уникальность, multiple Draft, основу и гонки.
4. Реализовать commands, DTO, ports, validators, mappers и handlers; согласовать F05 и адаптер исторических ссылок через публичные границы BC.
5. Реализовать Api с собственными ETag, ошибками и OpenAPI; выполнить Functional HTTP проверки и recursive NSwag generation/round-trip.
6. Реализовать frontend: списки, создание, рабочую область, динамическую форму, orphan и действия. Проверить реальный путь сохранённого Draft → Publish → Delivery по точному target, а также связи с cascade F02 и media picker F05.


## 13. Открытые технические вопросы

[Q37](../../frontend/questions/Q37-launcher-asset-delivery.md) определяет post-Demo resolved manifest/profile и compatibility metadata; frozen five-field descriptor и `/1` instance codec остаются прежними. Проверки resolver для nested Object/List, отсутствующих optional refs и двух exact SchemaVersions находятся у [F13](F13-content-delivery.md), не новый download handler F04.


| Вопрос | Что остаётся определить |
| --- | --- |
| [Q36: детализация F04](../../../business/questions/Q36-entity-instance-lifecycle.md#f04-оставшиеся-продуктовые-границы) | Согласованные Demo outcomes отражены в §2/§9; невыбранные repeated-action и MVP lifecycle детали |
| [Q15: детализация F04](../questions/Q15-api-and-source-code.md#f04-детализация-целевого-контракта) | Concrete Scalar/Object/List union encoding/CLR, ItemKey/presence carriers, raw-work provenance and computed token encoding, exact basis/consent carriers, paging/metadata/rename/return/errors/commit classification/public guard signatures |
| [Q19 v1 instance profile](../questions/Q19-content-hashes.md#принятый-canonical-profile-v1) | Selected cm-instance-content/1 byte/digest rules; neutral-library/storage implementation and independent literal conformance verification; descriptor five-field layout selected here |
| [Q20](../questions/Q20-persistence-and-migration.md) / [Q21](../questions/Q21-media-storage.md) | MVP guard принят; owner/read-scope materialization, FK/check/index migration и recursive orphan provenance для safe retention |
| [Q34](../../../business/questions/Q34-schema-field-semantics.md) / [Q35](../../../business/questions/Q35-value-constraints.md) | Первый MVP constraints/comparer набор согласован; operational HTML cap/library и typed wire materialization Q15/Q22, Offset temporal semantics согласованы Q34; whole new schema/instance/formal/C# profile codecs/golden bytes остаются engineering Q19/Q31; frozen instance /1 не меняется |


Stable Key и target, multiple Draft, копирование только по FieldId, сохранение orphan без потерь, основа по тому же target, общая нумерация и immutable Published уже приняты. Отличия исходной реализации эти решения не отменяют.
