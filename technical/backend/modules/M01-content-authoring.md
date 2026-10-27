# M01. ContentModeling

## Назначение

ContentModeling владеет authoring-моделью динамического контента:

- логическими EntitySchema;
- immutable Published и mutable Draft EntitySchemaVersion;
- логическими EntityInstance;
- immutable Published и mutable Draft EntityInstanceVersion.

Publishing, Delivery, Registry и MediaLibrary используют ContentModeling только через публичные контракты/идентификаторы и не изменяют его persistence напрямую.

MVP DateTime definitions фиксируют Naive/Offset по [Q34](../../../business/questions/Q34-schema-field-semantics.md#datetime-mvp-temporal-semantics-и-совместимость): F01 owns immutable semantics, F02 enforces new FieldId on switch, F04 retains authored offset/100ns и publishes UTC runtime. Shared TemporalCodec/instant comparisons используют pure neutral input; new profiles совместно включают constraints/Offset, old /1 unchanged. Подробные models/layer algorithms — F01/F04, не отдельный temporal BC.

## Карта фич

| ID | Ответственность |
| --- | --- |
| F01 | создать EntitySchema и первый Draft SchemaVersion |
| F02 | создавать/редактировать/удалять Draft SchemaVersion |
| F03 | валидировать и публиковать SchemaVersion |
| F04 | создавать EntityInstance, Draft/Published InstanceVersion и управлять их lifecycle |

## Aggregate boundaries

### EntitySchema

Aggregate root логической схемы.

Отвечает за:
- current Name/Key/Description;
- global SchemaKey uniqueness через Application/persistence check;
- LatestPublishedVersionId;
- Archive/Restore logical schema;
- identity и owner-level optimistic concurrency.

Не содержит все version snapshots как загружаемую коллекцию aggregate.

### EntitySchemaVersion

Отдельный aggregate root.

Отвечает за:
- Draft/Published/Retired/Withdrawal state;
- immutable Published content;
- Draft Title/ChangeComment;
- BasePublishedVersionId;
- VersionNumber publish-time;
- SchemaVersionContent;
- hash/publish metadata;
- optimistic concurrency mutable lifecycle/Draft.

Несколько Draft одной EntitySchema независимы.

MVP typed value/collection constraints и List comparer входят в definitions SchemaVersion по [F01](../features/F01-create-entity-schema.md#21-aggregates-entities-и-value-objects)/[Q35](../../../business/questions/Q35-value-constraints.md). F02 сохраняет внутренне валидные definitions, F03 замораживает их с новым supported schema profile, F04 проверяет values при Save/Copy/Publish. Shared pure validators не требуют rules repository/другого BC; constraints-only violations остаются в Instance Draft с issues без orphan/coercion/autosort. Demo и old `/1` profiles сохраняют прежнюю границу.

### EntityInstance

Aggregate root logical content item.

Отвечает за:
- SchemaId;
- immutable EntityInstanceKey;
- mutable authoring Name;
- LatestPublishedVersionNumber;
- Archive/Restore/Withdrawal logical item;
- owner-level concurrency.

### EntityInstanceVersion

Отдельный aggregate root.

Отвечает за:
- exact SchemaVersionId;
- Draft/Published/Retired state;
- values/orphan authoring data;
- BasePublishedInstanceVersionId;
- VersionNumber publish-time;
- immutable Published payload/hash;
- optimistic concurrency Draft/lifecycle.

## Версионирование

Не смешивать:

- `Version` — internal `int` optimistic concurrency, HTTP DTO не содержит;
- `ETag`/`If-Match` — HTTP representation internal Version;
- `VersionNumber` — business integer Published Schema/Instance sequence;
- `BasePublishedVersionId` / `BasePublishedInstanceVersionId` — stale-base metadata.

Stale-base и stale ETag являются разными конфликтами.

## SchemaField model

SchemaField живёт внутри immutable/mutable SchemaVersionContent snapshot.

Stable `SchemaFieldId` сохраняется между версиями одного logical field.

Для существующего ID immutable:
- Key;
- FieldDataType;
- type-specific semantic identity.

Monotonic/mutable:
- Name mutable;
- HelpText mutable;
- Required только true → false;
- Deprecated только false → true;
- удаление из следующей version разрешено.

Новый key/type/смысл либо другая parent-chain identity → новый SchemaFieldId. Возврат ID после отсутствия допускается лишь с прежними Key/Type/meaning/context и monotonic flags всей истории.

Field key unique по всему дереву version.

Canonical traversal по SchemaFieldId; UI presentation order может отличаться.

## Instance model

EntityInstanceVersion target SchemaVersionId после create immutable.

Cross-SchemaVersion copy создаёт новый Draft:
- values match по SchemaFieldId;
- unmatched source values → orphan;
- новые target fields empty;
- fuzzy matching отсутствует.

Draft на Draft schema допустим. Save допускает missing required и retained incompatible/orphan work; newly entered type-invalid input отклонён. Accept меняет только baseline при valid identity/evolution/context и допускает неполную работу. Publish instance требует strict-valid data без orphan по eligible Published target schema.

Withdrawn read/Copy/Save разрешены, Publish/runtime блокируются; возврат в выдачу — explicit action с проверкой Published admissibility, Publish не снимает withdrawal. Последний Instance Draft можно удалить, оставив пустой parent.

## Demo authoring contracts

Clean Schema Draft существует локально до первого valid Save minimum tree. Instance Save — FULL desired authoring snapshot: absent optional clear, collapsed/unrendered values retained, orphan explicit discard only, no Demo PATCH. Explicit Scalar/Object/List authoring variants используют namedFieldId и anonymous items без fake IDs; runtime keyed JSON отдельный. Opaque client-on-add ItemKey только List<Object>, unique per List occurrence, preserved Copy/Reorder, не EntityId/FieldId.

Own ETag saved root отделён от diagnostics basis exact schema/content/baseline. Persisted orphan stable ID, computed incompatibility deterministic FieldId/ItemKey location token bound basis; Save validates explicit discard/basis. Accept/delete consent связан с shown context/exact impact; changed delete set требует нового review (schema cascade min5 s вне transaction).

Create/Copy имеют operation ID + durable original result owning BC, same ID/input replay, different input conflict. Different-target Instance Publish с valid relevant baselines сериализует короткую sequence allocation: оба Publish successful consecutive numbers. Text restricted sanitized HTML/p/br/lists/bold/italic, semantic-empty check и configurable max; Deprecated defined options valid с warning и скрыты из new picker.

MediaLibrary references всех writer paths координируются через общий application-level AssetId guard single-process Demo до owning UoW commit. Draft/Published/orphans сохраняют reference authority; Demo не поддерживает multi-replica; MVP coordination описана в Persistence ниже. Detailed algorithms/models/API и verification matrix принадлежат F01–F05/Q15/Q20/Q21, module не дублирует transport signatures.

## Persistence

MVP использует [утверждённый consistency guard](../questions/Q20-persistence-and-migration.md#mvp-протокол-consistency-guard): одна PostgreSQL database для всех CM replicas, один стабильный глобальный exclusive transaction-level advisory key. Owning BC начинает явную ReadCommitted transaction и standalone pg_advisory_xact_lock на её же physical connection до любых authoritative reads/row locks; затем fresh reload own roots и foreign facts через public ports в независимых primary read scopes/connections. Owner commit атомарен только для своих roots + outbox/audit intent/inbox при необходимости; foreign ambient UoW enlistment/shared SQL transaction/EF access/cache/replica reads запрещены. Host/DI обязан разделить owner UoW и foreign reads: current multi-context EfCoreUnitOfWorkParticipant этого автоматически не гарантирует.

Все invalidating writers участвуют, включая reference creation/removal/Copy/orphans, Publish/lifecycle/hard-delete, source acceptance/watermark/pending/apply/retries/setup/reconciliation и F15 open/supersede/repair/resolve. Own ETag сохраняет412, changed foreign basis даёт409; unchanged source Create не повышает source Version. External I/O/user wait/long streams вне gate; подготовка CPU вне gate требует basis recheck внутри. Wait bounded/configurable/cancellable, unavailable read не empty, connection loss abort без reconnect-continue, lost commit ACK → unknown outcome и inquiry/operation ID Q15. Domain не содержит SQL guard types; exact port/DI/mappings/error codes/numeric defaults остаются своими Q.


PostgreSQL schema ContentModeling, собственный DbContext/migrations.

Рекомендуемая target форма:
- EntitySchemas;
- EntitySchemaVersions с Content JSONB;
- EntityInstances;
- EntityInstanceVersions с Values/OrphanValues JSONB.

Stable field identity находится внутри version snapshots; отдельная таблица полей не нужна для текущих invariants.

Published profile+exact canonical UTF8 bytes+SHA25664lowerhex digest сохраняются атомарно и неизменяемо: schema `cm-schema-content/1`, instance `cm-instance-content/1`. Neutral contracts/common pure .NET library определены один раз в [F03](../features/F03-publish-schema-version.md#33-dto-и-parameter-models); owner adapters используют typed normalized inputs, no JSONB hashing/read-time reserialization/historical rehash. Canonical content bytes без BOM/insignificant spaces/final LF; byte-changing evolution требует нового profile.

Hard delete только pre-history objects по Q09. Archive/Retire/Withdrawal сохраняют historical references.

## Меж-BC ports

### Publishing

Batch resolve exact Published EntityInstanceVersion:
- EntityInstanceId;
- EntitySchemaId;
- SchemaVersionId;
- lifecycle;
- content hash;
- display metadata.

### Schema Registry

Batch read exact Published SchemaVersion contract source.

### MediaLibrary

Authoring ContentModeling хранит AssetId и использует public MediaLibrary port для validation/current UI metadata. Publish под Demo AssetId guard либо MVP global transaction advisory guard captures agreed immutable AssetReference descriptor в Published runtime values/hash; Delivery читает snapshot без re-enrichment. Configured CM/storage URL в snapshot отсутствует; SDK вычисляет его из configuration + AssetId.

### Delivery

MVP Delivery не читает ContentModeling live path после materialization Publishing revision. Demo direct sync использует отдельный read-port Published data по exact SchemaVersionId.

## HTTP

Exact mutable resources возвращают strong ETag.

State-changing mutation требует If-Match:
- 428 без precondition;
- 412 stale ETag.

Technical Version в NSwag DTO отсутствует.

## Фичи

[F01](../features/F01-create-entity-schema.md) · [F02](../features/F02-create-schema-version.md) · [F03](../features/F03-publish-schema-version.md) · [F04](../features/F04-entity-instances.md)

## Связи

[MediaLibrary M02](M02-media-library.md) · [Publishing M04](M04-publishing.md) · [Delivery M05](M05-delivery.md) · [Schema tooling M07](M07-schema-tooling.md)
