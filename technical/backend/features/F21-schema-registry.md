# F21. Schema Registry: бэкенд


[Бизнесовая спецификация](../../../business/features/F21-schema-registry.md) · [M07](../modules/M07-schema-tooling.md) · [Codegen F22](F22-csharp-model-generation.md) · [UI-потребитель F22](../../frontend/features/F22-csharp-model-generation.md) · [F01](F01-create-entity-schema.md) · [F02](F02-create-schema-version.md) · [F03](F03-publish-schema-version.md) · [Q31](../questions/Q31-schema-code-generation.md).


## 1. Область реализации


F21 выдаёт официальный machine-readable JSON Schema контракт exact Published EntitySchemaVersion. Source of truth — immutable snapshot ContentModeling, включая опубликованный integer VersionNumber и замороженные PublishedSchemaName/PublishedSchemaKey. Registry не выводит структуру из экземпляров, не подставляет latest и не хранит второй редактируемый каталог.


Demo использует Registry в цепочке F22 C# generation без Identity, лицензий, шины/VR School. F22 вызывает внутреннюю query boundary в том же CM без HTTP loopback. Внешние developer/tooling consumers получают тот же формальный профиль. Registry не новый BC/микросервис и не runtime sync; отдельный UI F21 не требуется, selection/download принадлежат F22.


Exact Retired Published история читается для Registry/codegen по [F02](F02-create-schema-version.md). Draft не обычный стабильный target. Exact Published Withdrawn/Retired schema versions остаются доступны tooling exact-read; permission/error mapping MVP задаёт staff/tooling access/Q15. Сохранённый snapshot не даёт runtime entitlement.


Demo реализация использует следующие физические границы:


| Источник | Реализовано сейчас | Граница вывода |
| --- | --- | --- |
| Domain: [EntitySchemaVersion](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Domain/EntitySchemas/Entities/EntitySchemaVersion.cs) | F03 frozen integer number, name/key, canonical bytes/JSON/profile/hash и recursive content | Текущие labels owner не заменяют исторические; legacy rows без полного modern snapshot отклоняются |
| Application.Abstractions: [SchemaContractModels](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application.Abstractions/SchemaRegistry/SchemaContractModels.cs) | Metadata, source, owned formal document, queries и ports | Neutral canonical profile/digest; Content переиспользует полный SchemaContentSnapshotDto без Domain/EF graph |
| Application: [JsonSchemaContractExporter](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application/SchemaRegistry/JsonSchemaContractExporter.cs) | Pure all15 inline export, profile/shape/Unicode checks, byte/cancellation bounds | Только cm-json-schema/1; не C# generator или validator всех CM runtime extensions |
| Persistence: [SchemaContractSourceQueries](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.EntityFramework.Persistence/SchemaRegistry/SchemaContractSourceQueries.cs), SchemaContractQueryHandlers | Read-only repeatable-read preflight/materialization, frozen integrity, complete batch и timeout | Historical Published Retired/Withdrawn/Archived tooling read; export после закрытия transaction |
| Api: [SchemaRegistryController](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Api/SchemaRegistry/SchemaRegistryController.cs) | Exact raw GET, representation ETag, typed errors, binary OpenAPI | Нет public batch/latest/CRUD/If-Match/304; F22 и его UI остаются отдельной фичей |
| [Exact query handler](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.EntityFramework.Persistence/EntitySchemas/QueryHandlers/GetEntitySchemaVersionQueryHandler.cs), [version controller](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Api/EntitySchemas/Controllers/EntitySchemaVersionController.cs) | Authoring exact version read в Persistence и nested API | Это не schema-registry endpoint; authoring DTO не formal JSON Schema |


### 1.1. Зафиксированный formal profile Q31

F21 реализует JSON Schema **Draft 2020-12** profile `urn:vrschool:content-manager:json-schema-profile:1` (`cm-json-schema/1`). Exact `$id` = `urn:vrschool:content-manager:schema-version:{schemaVersionId:D}` с lowercase Guid. Документ self-contained; external cross-schema `$ref` в Demo отсутствует, internal `$defs` разрешён.

Profile version независим от package SemVer exporter-а. Изменение обещанных representation bytes/semantics требует нового profile, включая byte-changing bugfix; прежний `/1` сохраняет воспроизводимость поддерживаемого exact Published input. Это не пересчитывает исторический F03 ContentHash.

Exporter использует `x-cm-*` extensions для schema/version/field identities, FieldDataType, OrderMode, AssetKind и option/deprecated semantics. Optional property может отсутствовать, `null` не допускается текущими 15 типами, `required` задаёт обязательность, Object использует `additionalProperties: false`, `default` в Demo не экспортируется. Field traversal deterministic по SchemaFieldId, option keys ordinal; Registry bytes deterministic UTF-8 и не содержат timestamps/environment metadata.

Public exact endpoint фиксирован: `GET /api/v1/schema-registry/schema-versions/{schemaVersionId}` → raw `application/schema+json`. Latest fallback/key alias отсутствуют. Exact Published Retired/Withdrawn historical contract читается tooling-ом; Draft reject. Representation ETag вычисляется по deterministic JSON Schema bytes и не равен source F03 `ContentHash`.

Internal F22 использует `ISchemaContractSourceQueries`/exporter напрямую без HTTP loopback; batch обязан покрыть каждый requested ID или вернуть failure. Public batch endpoint Demo не нужен.

Type profile: String minLength1/maxLength255; Text minLength1; Integer→integer, Float/Decimal→number с CM type metadata; Boolean; Date=`format: date`; Demo DateTime uses CM naive format (не RFC3339 `date-time`); signed TimeSpan invariant `c`; Enum string keys; FlagEnum unique array of keys; Color uppercase `#RRGGBBAA`; recursive Object/List; AssetReference с exact `x-cm-asset-kind` (`Image|Audio|Video` Demo) и exact five-field runtime descriptor contract [F04](F04-entity-instances.md#33-dto-и-parameter-models)/Q19; no FileName/Format/dimensions/duration/URL fields.

### 1.2. MVP: typed constraints, temporal semantics и совместимость profiles

[Q35](../../../business/questions/Q35-value-constraints.md) согласовал первый MVP набор typed constraints и List comparer. Demo сохраняет baseline Q34 и `cm-json-schema/1`. MVP экспортирует полный immutable definition snapshot, включая constraints коллекции и recursive Item definition; изменения definitions требуют нового `cm-schema-content` и нового `cm-json-schema` profile. Старые `/1` bytes и historical hashes не меняются и не переэкспортируются под новым смыслом. Exact identity/byte layout нового profile и имена metadata extensions — **Уточняется** в Q19/Q31 до утверждения literal golden fixtures; этот документ не определяет готовый `/2` codec.

Constraints сами по себе не изменяют CLR/value type или runtime encoding, не исправляют values и не сортируют List. Первый новый MVP profile set совместно включает Q35 constraints и согласованный [Q34 Offset DateTime](../../../business/questions/Q34-schema-field-semantics.md#datetime-mvp-temporal-semantics-и-совместимость): новые schema-content, instance-content, JSON Schema и C# profiles. Для Offset runtime bytes меняются, поэтому instance `/1` недостаточен. Historical `/1` с Naive сохраняется; group/inventory/manifest profiles меняются только при собственном layout/semantics change по Q19. Product semantic choice закрыт, exact whole codec/identities/metadata/golden bytes нового набора ещё **Уточняется** Q19/Q31.

### 1.3. Post-Demo: аннотация доставки медиа

DeliveryMode принадлежит exact recursive AssetReference definition [F01](F01-create-entity-schema.md#21-aggregates-entities-и-value-objects), default `OnDemand`; modes `Preload`/`RequiredBeforeLaunch` immutable в Published. Registry должен losslessly передавать policy как versioned formal annotation tooling, включая List.Item и nested definitions, а не как property JSON instance или sixth frozen descriptor field. Concrete extension name/encoding/formal profile ID и integrity policy **Уточняются** [Q37](../../frontend/questions/Q37-launcher-asset-delivery.md)/Q31. Нельзя дописать её в frozen `cm-json-schema/1` или silently omit из нового source профиля: historical `/1` документ и representation ETag сохраняются по своему profile; old source effective default вычисляется без historical rehash.

F22/Core остаётся consumer formal contract; generated AssetReference CLR value не получает ошибочный глобальный DeliveryMode. Launcher получает разрешённую per-asset policy отдельно из resolved F13 manifest exact revision. Registry не вычисляет requirements из instances, не выдаёт entitlement и не заменяет manifest. Для нового supported profile mapper/compatibility validator обязаны явно знать metadata; unknown variants/profiles fail, не latest fallback.

## 2. Domain


### 2.1. Aggregates, entities и value objects


F21 не вводит Domain aggregate/entity или собственный mutable Version. EntitySchema/EntitySchemaVersion/SchemaField/SchemaValueDefinition и constraints определены один раз в [F01 §2](F01-create-entity-schema.md#2-domain); Publish/immutable snapshot — [F03](F03-publish-schema-version.md). Registry имеет read-contract models в Application.Abstractions, а не второй владелец схемы.


| Владелец | Состояние | Использование F21 |
| --- | --- | --- |
| EntitySchemaVersion ContentModeling | Published structure, number/name/key/hash immutable; lifecycle mutable | Exact target snapshot и current external availability |
| EntitySchema ContentModeling | Logical identity/current labels/key/lifecycle | Schema ownership и availability scope; current labels не переписывают Published export |
| SchemaField/recursive definitions | Stable named field ID/key/type в каждой exact версии | Полный lossless source для exporter, anonymous List item без fake FieldId |


### 2.2. Методы и переходы


Registry не вызывает Create/Update/Publish/Retire/Withdraw и не увеличивает Version. Source reader проектирует сохранённый exact snapshot; exporter выполняет pure преобразование read model → agreed JSON Schema. Domain factories/evolution checks остаются F01–F03; read не запускает миграцию/согласование или повторный Publish.


### 2.3. Инварианты и constraints


- VersionId и SchemaId — stable identity; опубликованный VersionNumber positive integer, Draft number отсутствует. Canonical Registry addressing ID-based; key aliases не требуются.
- Frozen name/key/hash берутся из Published version, не из текущего parent. Изменение logical Name/Key не изменяет exact historical export.
- Named SchemaFieldId и Key/FieldDataType сохраняют identity/meaning между версиями. New key/type получает новый ID. Технический default traversal — по SchemaFieldId [Q34](../../../business/questions/Q34-schema-field-semantics.md); Registry GUID encoding: lowercase canonical `D`; source ContentHash comparison remains Q19.
- Все 15 type meanings и recursive structures сохраняются. InheritanceStrategy/ColorFormat/numeric option Value/BitValue не включаются в target. Option identity — string Key, не Guid/index.
- Required означает presence/type-validity, не сравнение с CLR default. Deprecated field остаётся частью exact контракта с Required=false. Option deprecation metadata сохраняется; допустимость deprecated option в runtime validation/profile не решается удалением source key.
- Source ContentHash — metadata публикации, не hash экспортированного JSON Schema. F21 не пересчитывает и не записывает его. Source/export profiles и hashes разделены [Q19](../questions/Q19-content-hashes.md#f03-профиль-схемы-и-эталонные-векторы).
- Typed constraints и `ListComparerDefinition` принадлежат exact SchemaVersion и определены один раз в [F01 §2.1](F01-create-entity-schema.md#21-aggregates-entities-и-value-objects). Registry не вводит mutable каталог, свою модель правил или независимые evolution guards. OrderMode проверяет authored array order; equality/order semantics не становятся canonical sorting.
- `DateTimeValueDefinition.TemporalSemantics` immutable: Naive/Offset остаются одним `FieldDataType.DateTime` среди 15 типов. New MVP fields используют Offset; смена смысла требует нового FieldId. Historical Naive definitions и их CM format сохраняются, неизвестная temporal semantics не получает fallback. Models/guards принадлежат F01/F02, Registry лишь переносит exact definition.


### 2.4. Domain events


Новые domain/integration events F21 не требуются: после Publish source доступен через owner read boundary. GET/batch read/export не создаёт transition, outbox или F18 action только ради логирования. Существующий publication event не доказывает обновление Registry cache. F22 codegen output и SDK events не являются событиями F21.


## 3. Application.Abstractions


Public read contracts размещены в существующей ContentModeling.Application.Abstractions/SchemaRegistry без Domain/EF graph. `SchemaContractMetadata` содержит exact IDs/positive number/frozen labels и neutral CanonicalProfileId/CanonicalDigest. `PublishedSchemaContractSource` объединяет Metadata и Content: полный существующий SchemaContentSnapshotDto. Логические named-field/definition/options models ниже реализуются соответствующими SchemaFieldSnapshotDto/SchemaValueDefinitionSnapshotDto/CreateSchemaOptionDto, а не второй копией tree DTO. `SchemaContractDocument` владеет formal byte buffer (GetBytes возвращает копию), Metadata, Profile, Length и representation ETag. F22/Core получает formal bytes, не Domain nodes.


### 3.1. Commands


Команд изменения F21 нет. Public batch POST в Demo не вводится; internal batch принадлежит Application boundary F22. Экспорт не сохраняет schema/ZIP/историю генераций. Не вводить SaveSchemaContractCommand или fake Version/If-Match.


### 3.2. Queries


| Query | Назначение | Result |
| --- | --- | --- |
| GetSchemaContractQuery | Получить/export exact Published target | SchemaContractDocument |
| GetSchemaContractsQuery | Получить полный exact набор для F22/tooling | IReadOnlyList<SchemaContractDocument>; all-or-error, каждый requested ID обязан быть покрыт |


**GetSchemaContractQuery**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SchemaVersionId | `Guid` | Да | Exact target identity | Не Guid.Empty; не latest, Name/Key или Published number вместо ID |


**GetSchemaContractsQuery**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SchemaVersionIds | `IReadOnlyList<Guid>` | Да | Полный набор exact targets | Non-null/non-empty; каждый Guid non-empty; duplicates invalid; request order не влияет на canonical output; numeric guardrails configurable |


Public batch Registry endpoint в Demo не вводится. `GetSchemaContractsQuery` — внутренняя in-process boundary F22/tooling; отдельный HTTP batch body отсутствует в Demo.


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SchemaVersionIds | `IReadOnlyList<Guid>` | Да | Internal batch exact selection | Non-empty unique IDs; configured guardrails; no actor/Version/source fields |


### 3.3. DTO и parameter models


**PublishedSchemaContractSource** — immutable public projection ContentModeling. Parent mutable labels сюда не подставляются.


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| EntitySchemaId | `Guid` | Да | Owner logical identity | Принадлежит exact version; не snapshot Key вместо ID |
| EntitySchemaVersionId | `Guid` | Да | Exact опубликованная версия | Не другой/latest target |
| VersionNumber | `int` | Да | Frozen published business number | Positive per-schema number F03; major/minor текущего кода не target |
| SchemaKey | `string` | Да | PublishedSchemaKey snapshot | Immutable; current rename не меняет значение; Registry canonical addressing остаётся ID-based |
| SchemaName | `string` | Да | PublishedSchemaName snapshot | Frozen authoring label, не runtime content property |
| ContentProfileId | `CanonicalProfileId` | Да | Frozen source integrity profile | Actual frozen source profile: `/1` Demo или новый MVP profile Q19; neutral types defined once F03, не formal profile |
| ContentHash | `CanonicalDigest` | Да | Frozen schema source digest | SHA25664lowerhex/no prefix; not exported document ETag |
| Fields | `IReadOnlyList<PublishedSchemaFieldContractSource>` | Да | Полное дерево exact definition | Snapshot content, включая optional/deprecated fields, не instance sample |


**PublishedSchemaFieldContractSource** — только named field; anonymous List.Item определяется отдельно.


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SchemaFieldId | `Guid` | Да | Stable field identity | Не генерируется Registry; stable default traversal Q34 |
| Key | `string` | Да | Exact contract key | Сохраняется verbatim; Name/normalization не заменяют Key |
| Name | `string` | Да | Frozen name definition | Authoring metadata → JSON Schema `title`; не runtime property identity |
| HelpText | `string?` | Нет | Frozen authoring help | Authoring metadata → JSON Schema `description`; не runtime property |
| Required | `bool` | Да | Presence/type-validity правило | Не CLR-default check; deprecated →false |
| Deprecated | `bool` | Да | Deprecated metadata этой версии | Не удаляет field/type из документа |
| Definition | `SchemaContractValueDefinition` | Да | Один recursive value variant | Полный type semantics без Domain object graph |


**SchemaContractValueDefinition** — logical discriminated read model, common часть вариантов. Demo CLR representation — SchemaValueDefinitionSnapshotDto с FieldDataType discriminator; formal profile зафиксирован Q31, новая независимая type system не создаётся.

Для MVP read model переносит typed constraints и exact DateTime TemporalSemantics из единственных [F01 definitions](F01-create-entity-schema.md#21-aggregates-entities-и-value-objects) без независимой копии полей. List переносит собственные collection constraints, `ListComparerDefinition` и отдельную recursive Item definition с её constraints и temporal semantics. Projection не теряет nested rules и path SchemaFieldId. Exact transport carrier новых formal metadata — **Уточняется** [Q31](../questions/Q31-schema-code-generation.md); отсутствие выбранного byte layout не отменяет принятый логический состав.


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Type | Уточняется — [Q31](../questions/Q31-schema-code-generation.md) | Да | Один из 15 известных type meanings | Unknown/unsupported не `object` fallback; exact JSON Schema mapping задаёт `cm-json-schema/1`; logical type/representation: `FieldDataType` / formal profile discriminator; final CLR type is implementation detail |
| TemporalSemantics | Logical enum `Naive` / `Offset` | Да для DateTime variant | Frozen semantic discriminator DateTime definition | Единственная модель F01; immutable, recursive projection сохраняет значение; для других variants не применяется; CLR/extension carrier Q31, unknown rejected |
| ValueConstraints | `SchemaValueConstraintsDto?` | Нет | MVP exact typed definition rules | Переиспользуемый neutral Application.Abstractions contract F01 §3.3, логические fields F01 §2.1; только applicable known union; no Domain dependency |


**SchemaOptionsContractDefinition** — общая форма Enum/FlagEnum; Type определён выше.


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Options | `IReadOnlyList<SchemaContractOptionSource>` | Да | Exact declared option keys/metadata | Не numeric Value/BitValue; unique keys; deprecation metadata не теряется |


**SchemaContractOptionSource**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Key | `string` | Да | Identity option | Exact unique key внутри definition; не Guid или порядковый индекс |
| Name | `string` | Да | Snapshot caption | Authoring annotation; не runtime enum value и не C# member identity |
| Deprecated | `bool` | Да | Snapshot option deprecation | Source key preserved; exported deprecated metadata, F22 enum member gets `[Obsolete]` |


**SchemaObjectContractDefinition** — Type определён в common definition.


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Fields | `IReadOnlyList<PublishedSchemaFieldContractSource>` | Да | Nested named fields | Required/deprecated применяются к соответствующему object occurrence |


**SchemaListContractDefinition** — item может быть scalar/Object/List, Type common.


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Item | `SchemaContractValueDefinition` | Да | Anonymous recursive item | Не получает FieldId/Key/Name; List<List<T>> допустим |
| OrderMode | Уточняется — [Q31](../questions/Q31-schema-code-generation.md) | Да | Порядок List; logical Default/Ascending/Descending values fixed | Exported x-cm-order-mode `/1`; MVP comparer semantics Q35: проверка authored order, Default без comparer; concrete CLR enum type is implementation detail |
| Comparer | `ListComparerDefinitionDto?` | Условно | MVP frozen comparison key | Neutral contract F02 §3.3; Default null, Asc/Desc known supported variant; ordered server FieldIds, не new-field request references |


**SchemaContractSummaryDto** — internal/read metadata projection; public exact Registry endpoint returns raw schema document, не этот DTO envelope.


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| EntitySchemaId | `Guid` | Да | Owner identity | Из source, не lookup по текущему key |
| EntitySchemaVersionId | `Guid` | Да | Exact target | Однозначно сопоставляется запросу |
| VersionNumber | `int` | Да | Published integer number | Не technical Version или Draft number |
| SchemaKey | `string` | Да | Frozen key | Source snapshot, не current parent |
| SchemaName | `string` | Да | Frozen label | Не генерация member identity из display Name |
| ContentProfileId | `CanonicalProfileId` | Да | Frozen source integrity profile | Actual frozen source profile: `/1` Demo или новый MVP profile Q19; neutral types defined once F03, не formal profile |
| ContentHash | `CanonicalDigest` | Да | Source hash metadata | Не checksum JsonSchema bytes |


**SchemaContractDocument** — logical internal query result. Public exact endpoint maps its formal document to raw `application/schema+json`.


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| EntitySchemaId | `Guid` | Да | Owner identity | Exact source metadata; public `$id` identifies SchemaVersion, owner preserved in `x-cm-schema-id` |
| EntitySchemaVersionId | `Guid` | Да | Exact target | Не меняется на latest при export |
| VersionNumber | `int` | Да | Frozen published number | Positive/integer, не major/minor DTO |
| SchemaKey | `string` | Да | Frozen source key | Старый Published contract после rename не переименовывается |
| SchemaName | `string` | Да | Frozen source caption | Exported as annotation/title where applicable; not runtime identity |
| ContentProfileId | `CanonicalProfileId` | Да | Frozen source integrity profile | Actual frozen source profile: `/1` Demo или новый MVP profile Q19; neutral types defined once F03, не formal profile |
| ContentHash | `CanonicalDigest` | Да | Source publication metadata | Не hash формального schema export |
| JsonSchema (GetBytes) | owned UTF-8 bytes, byte[] copy | Да | Полный `Draft 2020-12` document supported formal profile | Public exact read отдаёт raw `application/schema+json`; internal F22 получает formal document in-process; logical type/representation: deterministic UTF-8 JSON Schema document; final CLR type is implementation detail |


### 3.4. Ports


| Port | Назначение / вход | Результат / направление |
| --- | --- | --- |
| ISchemaContractSourceQueries | GetPublishedSchemaVersion(exact Guid, cancellation) / GetPublishedSchemaVersions(exact IDs, cancellation) | Exact Published source projection; Retired/Withdrawn historical tooling read; complete set or explicit failure |
| IJsonSchemaContractExporter | Export(PublishedSchemaContractSource, explicit supported profile (`cm-json-schema/1` Demo; новый MVP Q31)) | Deterministic Draft 2020-12 document либо явная profile/source error; pure dependency, не HTTP/EF |


Порт Source читает только ContentModeling. Нельзя импортировать его Domain entity/DbContext в reusable CodeGeneration.Core; F22/Core потребляет JSON Schema через свой agreed boundary. Названия методов описывают ответственность; concrete async/result wrapper follows repository/CQRS conventions without changing Q31 semantics. Нет media/storage/license/session ports: AssetReference — type definition, не загрузка Asset.


### 3.5. Specifications / read contracts


Owner reader выражает атомарные exact version-ID, owner-membership и Published conditions в существующем EF/SQL read adapter. Отдельные классы PublishedSchemaContractByVersionIdSpecification/PublishedSchemaContractsByVersionIdsSpecification не вводятся: текущий стек использует прямые owner predicates и не содержит Ardalis.Specification dependency. Batch проверяет coverage и состояние явно, без partial-as-full результата. Exact Retired/Withdrawn Published history остаётся доступной developer/tooling read; runtime entitlement этим не определяется.


Source projection NoTracking, полного дерева/замороженных metadata, без latest pointer fallback. Batch должен различить absent/unavailable member и legitimate набор; raw filtered query result не считается полным ответом без проверки всех запрошенных IDs. Internal batch all-or-error: каждый requested ID обязан иметь exact result; empty/duplicate selection для F22 invalid. Порядок public Registry не задаёт UI; F22 selection использует ordinary schema/version reads.


## 4. Application


### 4.1. Handlers / use cases


Query handlers размещаются в ContentModeling.EntityFramework.Persistence/SchemaRegistry по [S20](../../../catalog/docs/standards/architecture/cqrs.md#handler-responsibilities-and-dependency-direction), Source query adapter — там же в SchemaContracts. Application реализует pure JsonSchemaContractExporter и reusable validators/mappers; query handler в Application не добавляется. Конкретные процедуры для single/internal/batch приведены в DF01–DF04.


| Тип | Ответственность | Вызовы / boundary |
| --- | --- | --- |
| GetSchemaContractQueryHandler | Input/access → exact source → export → result | Source port + exporter; query, без Domain transition/SaveChanges |
| GetSchemaContractsQueryHandler | Full requested set → materialized exact sources → export каждого → проверенный набор | Без N HTTP loopback, без silently omitted/partial-as-full result |
| SchemaContractSourceQueries | Owner EF read и projection target snapshot | Единственный доступ к ContentModeling DbContext для F21 source, не новый repository export documents |
| JsonSchemaContractExporter | Lossless formal mapping и deterministic traversal | Только read-contract values/profile; не model selection или C# generator |


F22 inside-process использует те же query/port semantics. Возвращённый source — materialized immutable values, не lazy IQueryable/EF entities, поэтому exporter не держит DB transaction во время обработки. Demo read checkpoint/batch consistency реализованы одной read-only repeatable-read transaction SourceQueries; отсутствие mutable writes не означает автоматический frozen external access forever.


### 4.2. Validators


FluentValidation composition по [S18](../../../catalog/docs/standards/dotnet/fluentvalidation.md): SchemaContractVersionIdValidator проверяет конкретную exact identity, SchemaContractSelectionValidator — наличие коллекции/валидность каждого ID и agreed duplicate/empty/size policy. Built-in NotNull/ID checks не оборачиваются в классы ради шаблона; названия компонентов означают смысловое reuse. Source constraints/type shape проверяются по F01/Q34 и ExportProfileCompatibilityValidator, а не независимым универсальным validator строки.


Publication status/ownership/lifecycle берутся reader-ом из актуальных owner facts, не доверяются body и не решаются только FluentValidation. Source без обязательного frozen metadata/hash/positive published number, с неизвестным definition или потерянными descendants не превращается в successful document. Отсутствующая profile policy не заменяется default latest/normalization/пересчётом из instance.


### 4.3. Mappers

Source content integrity types reuse [F03 neutral CanonicalProfileId/CanonicalDigest](F03-publish-schema-version.md#33-dto-и-parameter-models): frozen cm-schema-content/1 +64lowerhex digest forwarded unchanged. Domain VO adapter unwraps into neutral read values; Registry neither hashes source anew nor imports Domain canonicalizer. Exact metadata HTTP carrier Q15, no unapproved keyword/header mutation of fixed cm-json-schema/1 document.

MVP mapper сохраняет фактический frozen source profile/digest и полный typed definition snapshot, включая immutable TemporalSemantics и temporal bounds, выбирая совместимый поддерживаемый formal profile для каждой exact версии. Нельзя пометить новые constraints/Offset как `/1`, отбросить их или rehash старую history. Offset bounds имеют обязательный offset и сравниваются по UTC instant; semantic schema hash нового profile представляет их в UTC по F03/Q19, exporter не пересчитывает этот hash. Naive bounds не переводятся в UTC. Read DTO/profile boundary остаётся Application.Abstractions; Domain guards/validation Save/Publish/Copy остаются F01–F04.


1. EntitySchemaVersion target → PublishedSchemaContractSource: scalar identities и frozen published name/key/number/hash + complete immutable field tree. Parent используется для ownership/availability, но не для подмены historical labels.
2. Source recursive definitions → agreed JSON Schema: named Key/ID, object context required set, enum keys/metadata, anonymous list items, type constraints. SourceHash не вычисляется этим mapping.
3. Source metadata + exported value → logical document/summary result. Controller возвращает raw `application/schema+json`; не сериализует Domain entities или обычный editor DTO в качестве formal schema.


Name/HelpText экспортируются только как annotations (`title`/`description`); `$id`, `x-cm-*`, `$defs` и остальные formal semantics задаёт `cm-json-schema/1`. Authoring labels не добавляются как runtime properties. При этом настоящий named field с contract Key `name`/`helpText` сохраняется как любое объявленное поле; исключение authoring metadata не является blacklist таких business keys. F22 `[Obsolete]`/C# attributes принадлежат generator profile; F21 передаёт согласованный deprecated meaning, а не C# код.


### 4.4. Services / algorithms


Exporter выбирает ровно известный type variant и переводит его без silent `object`/empty/true-schema fallback. Точные keywords/format/extensions определены `cm-json-schema/1` и должны совпадать с F13 wire semantics.


| FieldDataType | Согласованная семантика | Что обязан сохранить exporter | Открытая форма |
| --- | --- | --- | --- |
| String | Строка, intrinsic max 255, empty string недопустима | JSON `string`, `minLength:1`, `maxLength:255` | Normalization rules remain Q15 |
| Text | Restricted sanitized HTML, semantic empty invalid | JSON string/minLength1 + CM Text semantics; numeric max from server config after fixture | Paragraphs/br/lists/bold/italic; sanitize before Save, exporter preserves frozen text |
| Integer | Integer value | JSON `integer`; generated C# `long` | Inclusive bounds/positive exact MultipleOf Q35 входят в MVP new profile |
| Float | Finite number semantics | JSON `number`; generated C# `double`; no NaN/Infinity | Inclusive bounds Q35 входят в MVP new profile; Float MultipleOf отложен |
| Decimal | Decimal semantics | JSON `number` + `x-cm-field-data-type=Decimal`; generated C# `decimal` | No binary64 collapse |
| Boolean | Boolean | JSON `boolean`; false valid present value | Optional absence distinct from false |
| DateTime | Exact immutable TemporalSemantics | Naive: прежний JSON string yyyy-MM-dd'T'HH:mm:ss.fffffff + CM annotation, CLR Kind=Unspecified; Offset: JSON string `format: date-time` + CM strict precision/range rules, CLR DateTimeOffset | 7 fractional digits/100ns; Offset runtime UTC Z по новому profile; no host timezone conversion/unknown fallback |
| Date | Календарная дата | JSON string `format: date`, wire `yyyy-MM-dd` | No time/offset |
| TimeSpan | Длительность, включая zero/negative | JSON string, invariant .NET `c` | Sign preserved |
| Enum | Один string option Key | JSON `enum` of exact keys; option deprecated metadata preserved | F22 generates enum/string converter; deprecated member `[Obsolete]` |
| FlagEnum | Array/set string option Keys; empty set допустим | JSON array of enum keys, `uniqueItems:true`; canonical key order ordinal | No bitmask |
| Color | Единственный RGBA string `#RRGGBBAA` | Uppercase canonical hex + exact pattern; transparent valid | No ColorFormat |
| Object | Nested object named fields | object properties + `required`; `additionalProperties:false`; null not allowed | Self-contained exact contract |
| List | Recursive array/item, включая List<List<T>>/List<Object> | JSON array + recursive item; `x-cm-order-mode` `/1` | MVP new profile сохраняет counts/full-value uniqueness/typed comparer Q35 без перестановки массива |
| AssetReference | Stable AssetId + required schema AssetKind | `x-cm-asset-kind`; runtime descriptor semantics align F05/F13 | No storage topology/Unity object |


Named fields помещаются под exact keys в соответствующий object context. Required set не заполняется по `value != default` и не заставляет list/flags быть non-empty. Optional absence and present type-valid value differ; `null` is not accepted by current 15-type profile, `default` is not emitted in Demo. Deprecated field сохраняется с Required=false и metadata; deprecated option keys remain formal values; F04/F13 apply exact published contract, ключи не исчезают из source contract молча.


Object/List traversal рекурсивен. Стабильный default field order по SchemaFieldId выбран Q34; Registry GUID uses lowercase D; option/flag canonical key ordering is ordinal. Source ContentHash comparison remains Q19. Name/Key case не нормализуются новым exporter-ом. Profile Q31 определяет `$id`/`$ref`/definitions/annotation names без коллизий, сохраняя exact version и stable field identities.

Configured Text cap относится к authoring Save/Publish policy, не today's input exact exporter. Historical Registry bytes не меняются при настройке cap; exporter использует frozen snapshot/profile и не санитизирует опубликованный текст заново. Semantic-empty HTML validation остаётся ContentModeling rule, не эквивалент одному JSON Schema minLength.


Один source + один agreed profile дают воспроизводимую semantics. Demo Registry document bytes/profile identity/evolution fixed by Q31; source F03 ContentHash golden profile remains Q19. Смена serializer/dialect/DateTime profile не разрешает менять старый source/ContentHash или silently ломать F13 payload/generated models. MVP constraints/comparer не расширяют Demo; regex/Float MultipleOf/Decimal Precision/Scale не входят в первый MVP.

**MVP mapping:** стандартные JSON Schema keywords применяются только при эквивалентной typed semantics. Inclusive numeric bounds → `minimum`/`maximum`; String Unicode code-point lengths → `minLength`/`maxLength`; List counts → `minItems`/`maxItems`. Integer/Decimal `multipleOf` допустим только с exact арифметикой без Decimal→double/overflow; generic numeric implementation не доказывает это соответствие. Text plaintext length после sanitization/entity decoding, temporal bounds и List comparer требуют CM extensions и CM-aware validator. `uniqueItems` допустим только при доказанном совпадении с full type-aware normalized equality Q19/Q35; Object FieldId/presence, FlagEnum set keys и nested List order могут потребовать CM extension. ItemKey/orphan/authoring metadata исключены из equality. Hash только индексирует кандидатов, equality подтверждается полными normalized values/bytes; invalid/incomplete item не даёт uniqueness proof.

Exporter сохраняет comparer как contract metadata: ItemValue supported scalar или ObjectFieldPath по ordered SchemaFieldId через required Object nodes к required supported scalar leaf. String/Enum key используют UTF-16 ordinal comparison, числовые/temporal — typed, false < true; это не Unicode length metric. Ascending/Descending допускают equal keys без ItemKey tie-break, Default не имеет comparer. Ни exporter, ни canonicalizer/Delivery/SDK не переставляют authored array. Явный stable sort остаётся локальным Draft action F04 с обычным Save/If-Match. Exact extension names/encoding и новые golden bytes остаются Q31.

Offset formal contract задаёт mandatory known offset, invariant `yyyy-MM-dd'T'HH:mm:ss.fffffffzzz` либо UTC `yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'`, ровно семь fractional digits. CM checks отклоняют missing offset, `-00:00`, leap second, offset вне ±14:00/не whole minutes и local/UTC DateTimeOffset range overflow. Generic `format: date-time` не доказывает более строгие CM правила. F04 сохраняет authoring offset, Publish нового instance profile выводит UTC Z без потери 100ns; Registry описывает этот exact runtime contract, не преобразует authoring values. Bounds/equality/List comparer для Offset используют UTC instant, same instant разных offsets equal/duplicate и не получают secondary offset key; Naive/Offset не сравниваются скрытой conversion. Pure temporal codec с explicit parse/format применяется через agreed profile, без server/browser timezone.


## 5. Api


### 5.1. Endpoints


Registry API реализован как exact raw JSON Schema GET; ordinary authoring version GET остаётся отдельным authoring read.


| Method / surface | Route / destination | Request | Response | Access |
| --- | --- | --- | --- | --- |
| GET exact contract | `/api/v1/schema-registry/schema-versions/{schemaVersionId}` | Exact Published version ID | Raw `application/schema+json` + representation ETag | Demo anonymous; MVP staff/developer tooling access, не runtime entitlement |
| Internal query single/batch | F22 → in-process query boundary | Exact ID(s) | Complete formal document(s) | Same tooling readability; no HTTP loopback |



SchemaContractSummaryDto не требует дополнительного GET list/latest/key lookup. F22 UI показывает exact source versions через existing authoring read contracts и обращается к generation API F22; отдельный raw JSON inspector/registry page не вводится.


### 5.2. ETag / If-Match

Annotation-only changes across exact published inputs may preserve semantic cm-schema-content/1 hash while changing JSON Schema representation bytes/ETag. Compare content profiles too; semantic hash never validates raw Registry representation. Registry profile cm-json-schema/1 remains independent; common content noFinalLF rule not imposed on a separately fixed tooling byte profile.


F21 не изменяет owner: expected Version/If-Match/428/412 concurrency flow отсутствуют, technical Version не входит в DTO. VersionNumber — business metadata, не concurrency token. Exact raw Registry response возвращает representation ETag, вычисленный из deterministic JSON Schema bytes `cm-json-schema/1`; он не равен source `ContentHash` и не строится из mutable root Version.


### 5.3. OpenAPI и ошибки


Controller отдаёт raw JSON Schema `application/schema+json`; successful body не `ApiResponse<string>`. ProblemDetails/ValidationProblemDetails и конкретные machine codes/status для invalid/notfound/nonPublished/source/profile failures оформляются по Q15; Q31 больше не выбирает media/envelope/route. Public batch в Demo отсутствует.


Не добавлять одновременно расходящиеся schemas/envelopes/custom keyword sets. JSON Schema formal contract и OpenAPI response описываются отдельно, согласованно. NSwag NJsonSchema toolchain сама не реализует Registry. Response body не считается complete contract после network truncation; повтор read не создаёт запись/новую schema version, но availability может измениться к следующему запросу.


## 6. EntityFramework


### 6.1. DbContext ownership


F21 не владеет DbContext/DbSet/schema/table. ContentModelingDbContext и version/source mapping — owner F01–F03. Source adapter живёт в owner Persistence и читает только own data; Registry query handler использует port, не чужие DbContexts/репозитории. Published source может быть получен без единого EntityInstance и без Catalog/Publishing/Delivery/MediaLibrary joins.


### 6.2. Configurations


Отдельной SchemaContractDocumentConfiguration/Registry table нет. Требуемые target immutable published fields/recursive JSON/value converters находятся у ContentModeling. [Current version configuration](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.EntityFramework/Configurations/EntitySchemaVersionConfiguration.cs) хранит legacy major/minor/content JSONB/published text; это не реализация target snapshot/export.


### 6.3. Constraints / indexes


Новых F21 indexes/FKs/concurrency columns нет. Source owner поддерживает uniqueness/identity/positive published number и immutable content по F01–F03. Exact/batch source lookup использует его read constraints; performance indexes и bounds принадлежат owner/Q20, не Registry cache DB по умолчанию.


## 7. EntityFramework.Persistence


### 7.1. Repositories / query adapters


Целевое размещение без новых проектов: ContentModeling.EntityFramework.Persistence/SchemaContracts — SchemaContractSourceQueries и source specifications/projections; /SchemaRegistry — GetSchemaContractQueryHandler/GetSchemaContractsQueryHandler. Exporter — ContentModeling.Application/SchemaRegistry, ports/models — Application.Abstractions, controller — ContentModeling.Api. Это не новое BC/persistence authority M07.


Source materializes exact committed version + required owner availability context, проверяет Published/frozen metadata и проецирует complete read graph, включая recursive TemporalSemantics/typed bounds без потери 100ns или обращения к host timezone. GetSchemaContractQueryHandler вызывает Source port и pure exporter. Source adapter не вызывает editor mapper, если тот теряет target semantics. Historical Naive определяется exact definition/profile, не отсутствием offset с последующим угадыванием. Missing frozen fields/unsupported legacy construct не исправляются current Name/Key, inference из instances или recomputation hash; recovery/owner migration Q20.


### 7.2. Transactions


Нет source writes/SaveChanges/outbox/export history/ZIP persistence. Demo SourceQueries открывает отдельный scope и short read-only repeatable-read transaction: сначала exact ID/status и SQL source-size preflight, затем NoTracking materialization, ownership/frozen integrity и complete neutral projections. Суммируются UTF-8 definition JSON и обе frozen canonical representations до загрузки graph; превышение согласованных input bounds отклоняет whole set. Transaction закрывается перед pure export. Query handlers применяют общий operation timeout и отдельно формальный per-document/batch byte budget; caller cancellation не становится timeout error. Withdrawn/Retired/Archived owner не запрещает exact Published tooling read.


Optional cache не реализуется по умолчанию. Если будет выбран, key включает exact version и agreed exporter profile/dependencies; source ContentHash один недостаточен. Cache не подставляет latest и не обходится current availability/read checks. TTL/eviction/store and lifecycle invalidation Q20; cache miss — read/export, не удаление Published source.


### 7.3. Migrations


F21 не создаёт migration собственных таблиц. Нужные целевые frozen name/key/number/hash/recursive definitions — изменения owner F01–F03 по Q20. Current published DTO/JSON не объявляется совместимым legacy Registry без доказанной projection/profile. Миграционный контекст и source recovery принадлежат owner/Q20; F21 не вводит historical label backfill угадыванием из current parent.


## 8. External adapters


### 8.1. HTTP / F22 / Schema API


Registry HTTP adapter размещается в ContentModeling.Api; SchemaRegistryController — целевой utility controller, не новый entity CRUD. F22 server wrapper вызывает queries напрямую внутри процесса и передаёт формальный документ reusable Core; не N HTTP self-calls, не импорт Domain tree. Внешние generators могут потреблять тот же Schema API/JSON Schema, их реализация не входит в F21.


JSON Schema parser/validator/AST library остаётся implementation adapter и не определяет formal semantics; наличие NJsonSchema в NSwag не выбирает exporter-а. Formal semantics задаёт `cm-json-schema/1`. `$ref` resolver не обращается к непредоставленному latest/чужой DB и не скачивает произвольные ссылки из пользовательского payload. Closure/inline/exact referenced contracts и naming policy — часть formal profile.


### 8.2. Host / DI


ContentModeling.Application регистрирует pure exporter/profile/validators. EntityFramework.Persistence регистрирует scoped owner source adapter/specifications и Registry query handlers; не сохраняет DbContext в shared exporter. ContentModeling.Api подключается через existing app API module. [ContentManagerApplicationModule](../../../../contentmanager.api/src/App/ContentManager.Application/ContentManagerApplicationModule.cs) уже включает Application/Persistence handler assemblies в AddLeanSoftCqrs; новые handlers должны быть доступны тому же dispatcher по actual registration.


Configured `cm-json-schema/1` одинаков для HTTP и internal F22. Missing/unsupported profile configuration fails fast и не заменяется library defaults. Lifetimes/export output mutability определяются implementation, не скрытым shared JsonNode между запросами. Нет S3/RabbitMQ/VR School/SDK/Unity зависимости и отдельного Registry bus/cache service/seed credentials.

Host регистрирует singleton pure exporter (readonly document budget) и scoped SourceQueries; CQRS сканирует существующие Application validators/Persistence handlers. Required `ContentModeling:SchemaRegistry` settings: Profile=`cm-json-schema/1`, MaxBatchSchemas=64, MaxDocumentBytes=8MiB, MaxBatchDocumentBytes=32MiB, MaxSourceDocumentBytes=16MiB, MaxBatchSourceBytes=64MiB, OperationTimeout=30s. ValidateOnStart отклоняет отсутствующие/невалидные settings. Полный byte layout и раздельные input/output limits закреплены [Q31 §3](../questions/Q31-schema-code-generation.md#3-deterministic-registry-representation); значения конфигурации не входят в formal bytes.

MVP DI регистрирует поддерживаемые old/new profiles с явной source→formal compatibility, CM-aware validation adapter и shared pure temporal codec. Domain definitions не получают exporter/DbContext; Application validators/exporter принимают neutral definitions. EntityFramework/Persistence сохраняют и читают полный owner snapshot атомарно по F01–F03, включая recursive constraints/comparer/TemporalSemantics, не live catalogue и не SQL timestamp каждого dynamic value. Api выдаёт полный selected profile без собственного validator бизнес-правил; HTTP/internal consumers используют одну compatibility policy. Generic Draft 2020-12 validator проверяет стандартную часть, но не заменяет CM validator для extensions. Unknown semantics/profile fails, host timezone не выбирает meaning.


## 9. Data flows


По [S17](../standards/S17-data-flows.md): раздельно external API и internal F22, собственных записей/commit нет.


| Поток | Транспорт | Метод / канал | Endpoint / destination | Доступ | Вход | Application action | Выход | Успех | Ошибки |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| DF01. Exact public contract | HTTP | GET | `/api/v1/schema-registry/schema-versions/{schemaVersionId}` | Demo anonymous; MVP tooling access | Exact VersionId | GetSchemaContractQuery | Raw `application/schema+json` + ETag | Exact deterministic contract, no latest | Invalid/notfound/nonPublished/source/profile/Q15 access error |
| DF02. Internal exact contract | In-process query | Query dispatcher | F22/tooling → Registry boundary | Same tooling readability | Exact VersionId | GetSchemaContractQuery | Same formal document | Same semantics as public read | Same source/profile failures, no HTTP loopback |
| DF03. Internal batch | In-process query | Batch query | F22 → Registry boundary | Same tooling readability | Non-empty unique exact IDs | GetSchemaContractsQuery | Complete document set | All requested targets covered; request order irrelevant | Missing/unsupported member, duplicate/empty, configured limits, export failure |


### DF01. Exact public contract


1. Api binds exact requested ID, applies selected access/error profile and query validation. No client-provided source fields/hash/published status and no If-Match. Parse/missing input failure is distinct from absent target.
2. GetSchemaContractQueryHandler в Persistence вызывает owner Source port. Reader selects exact version and owner, materializes Published content/frozen number/name/key/hash, validates completeness/availability. No current-name/major-minor/latest fallback, no instance/media/license join. Retired exact allowed per F02; Retired/Withdrawn exact Published tooling read.
3. Pure exporter checks agreed source/formal compatibility and builds complete recursive contract. Named IDs/keys/required/deprecated/options/list items map to correct contexts; MVP typed constraints/comparer/TemporalSemantics и Item rules переносятся losslessly в новый profile. Exact Naive получает прежний CM format, Offset — `date-time` и strict CM checks. Old `/1` сохраняет прежние bytes; unknown construct/temporal semantics/failed dependency не становится generic object/omitted rule. Export не валидирует instance и не сортирует List.
4. Map exact source metadata + document. Return selected raw/envelope representation after successful preparation; error/malformed output is not marked full success. No SaveChanges, new number/hash/event/ZIP.
5. Cancellation/transport truncation is not complete contract for consumer. Repeated read can recompute same exact content/profile without writes; current external availability still follows policy.


### DF02. Internal exact contract


1. F22/server tooling passes exact selection to query dispatcher/Registry boundary with cancellation, not to Domain serializer and not to HTTP self-route.
2. Same GetSchemaContractQueryHandler/source reader/exporter as DF01. Internal consumer does not bypass Draft/Withdrawn/type-profile checks.
3. Return immutable logical document, exact metadata and agreed formal content. Core F22 gets its supported document/profile interface, no DbContext or arbitrary authoring DTO. F21 creates neither generated C# nor ZIP/history.


### DF03. Internal batch


1. Validate explicit collection and each exact ID. Require non-empty unique exact IDs; duplicates invalid; numeric limits are configured guardrails; request order does not define export semantics.
2. Owner Source port loads/materializes the requested source set and required current availability facts. Match by exact version ID and check every requested target; filtered rows do not prove all targets available. Multiple versions of one schema are legitimate.
3. Export each source under an explicitly compatible supported profile; preserve matching IDs, recursive definitions/constraints/comparer/TemporalSemantics and actual frozen source metadata. В одном наборе допустимы exact historical Naive и new Offset версии с совместимыми profiles; historical `/1` не маркируется новым profile. Batch completeness не разрешает silently drop constraints/temporal meaning из unsupported member; no latest substitution or repeated HTTP per item.
4. Return only explicitly agreed complete-success/error result; missing/unsupported member cannot be quietly omitted while reporting the full selection succeeded. Internal batch is all-or-error for F22: any missing/unsupported requested member fails the set.
5. No global write transaction/SaveChanges/cache history. Cancellation/export failure does not change source. Order/dedup presentation, batch capture consistency и bounds не выбираются случайным EF order или library defaults.


## 10. Ошибки и диагностика


| Ситуация | Требуемое поведение | Owner/profile |
| --- | --- | --- |
| Missing/invalid exact ID/input collection | Различимая validation/binding ошибка | Api/validators Q15 |
| Exact version отсутствует / belongs не тому owner | Не latest/чужая схема по похожему Key | Source reader/error mapping |
| Draft/nonPublished target | Не выдаётся stable Published contract | F01–F03 / fixed Q31 profile |
| Exact Retired Published | Historical Registry/codegen read, source content unchanged | F02 |
| Withdrawn schema/version | External readability по отдельной policy, not hash-as-permission | Q20/Q15 |
| Frozen source metadata/hash отсутствуют | Invalid/unready target source, not current parent inference | Owner implementation/Q20 |
| Unknown type/profile/ref/unsupported construct | Явная ошибка, не omitted field/`object` fallback | Exporter `cm-json-schema/1` |
| Unknown/missing DateTime temporal semantics или incompatible profile | Явная source/profile ошибка; не Naive fallback, не host-timezone interpretation | F01/Q34 + Q19/Q31 |
| Batch member failed | Не false full set/частичный successful ZIP | F22 all-or-error result |
| Source/schema renamed or current pointer changed | Exact IDs и frozen metadata сохранены | Source snapshot, not live labels |
| Cache/profile/access mismatch | Не старый документ под новым profile/сейчас запрещённым read | Optional cache Q20 |
| Cancellation/network/export failure | Read не меняет source; incomplete response not usable contract | Consumer/Api/exporter |


Diagnostics: requested/exact schema version ID, export profile/stage и field/type path для отказа. Не raw entire schema/instance/secret payload, credentials или C# artifacts. Полный JSON Schema лог не обязателен для анализа. No read action F18/outbox/email/export notifications by default. Exact machine codes/status для ошибок остаются Q15; UI F22 различает selection/source/profile/generation failures через своего владельца.


## 11. Тестирование

Post-Demo literal fixtures нового formal profile отдельно проверяют annotation/default/explicit modes на nested AssetReference definitions и отсутствие mode в instance value contract. Source profiles/digests forwards unchanged; historical `/1` JSON bytes/ETag сохраняются. F21→F22 compilation/serializer fixtures подтверждают неизменный five-field AssetReference value; F13 manifest resolver получает exact definition, не latest Registry. Конкретные extension/bytes ждут Q37/Q31; это future strategy, не выполненная проверка.

Additional canonical contract fixtures: F03 literal source bytes/digest/profile propagated without mutation; reordered semantic fields/options and Name/HelpText/label changes preserve semanticHash as appropriate while actual exported annotations alter representation ETag. Unknown source profile/corrupt immutable source triple rejected, no historical rehash/guess; source metadata neutral read types preserved in F22. Tests are required strategy, not executed app proof.


Применяются [S04](../standards/S04-testing.md) и [S14](../../../catalog/docs/standards/dotnet/testing.md#test-environment-ownership-and-bounded-parallelism). Методы exporter-а и source projection, API и cross-app contract compatibility проверяются отдельно.


| ID / DF | SUT / уровень | Собственные данные / воздействие | Наблюдаемый результат |
| --- | --- | --- | --- |
| T01 / DF01–03 | Unit pure exporter | Table fixtures всех 15 types, scalar limits/intrinsic validity и approved profile | Каждый смысл lossless, no arbitrary object/default or omitted type |
| T02 / recursive | Unit + contract | Object/List<Object>/List<List<T>>, named child IDs и anonymous items, required/deprecated | Полное дерево, real IDs только named nodes, correct object required contexts; deprecated field остаётся optional |
| T03 / enum/flags/color | Unit + contract | Exact keys/names/deprecated option metadata, empty flags, fixed RGBA включая прозрачный | Wire semantics string/set-of-keys/RGBA, no numeric enum/bitmask/ColorFormat; profile validation aligned F04/F13 |
| T04 / temporal/numeric | Contract + F13 compatibility | Decimal precision, Demo naive DateTime, Date, signed/zero TimeSpan, 0/false/empty list | Approved formats preserve semantics; no hidden UTC/offset/binary64/non-empty constraints |
| T05 / identity/profile | Unit golden vectors | Same immutable source, reordered input, renamed current parent, changed explicit exporter profile | Stable exact IDs/keys/frozen labels; default FieldId order using agreed comparer; sourceHash unchanged; literal vectors independent from production encoder |
| T06 / DF01–03 | PostgreSQL Integration source adapter | Own schema/version roots, Published frozen metadata, no EntityInstance rows; another owner and newer Published | Exact snapshot/metadata получены, no latest/instance inference; read не меняет roots/Version/pointer |
| T07 / lifecycle | PostgreSQL Integration + Functional | Draft, Retired Published, confirmed Withdrawal/root scope and selected policy | Draft not stable; Retired exact доступен; Withdrawn соответствует chosen profile, no cache bypass |
| T08 / DF03 | Query Integration + Functional | Multiple versions same schema, missing member, duplicate/empty/cancel cases | Complete ID matching; duplicate/empty reject; no partial-as-full |
| T09 / source errors | Unit + Integration | Missing frozen name/key/hash, incomplete/unsupported legacy definition, failed public reader | No current parent/hash recompute/instance fallback, explicit source/profile error |
| T10 / HTTP | Functional real API + PostgreSQL | Selected route/media/envelope/schema response/errors/OpenAPI; batch only if exposed | Actual published exact content matches profile; technical Version/If-Match отсутствуют; GET/batch no mutations |
| T11 / boundaries | Architecture/DI | Query/reader/Api assemblies and pure exporter dependencies | Queries in Persistence, no Registry DB/foreign contexts/C# generator/Unity/S3/broker dependency; handlers/ports resolved |
| T12 / F22/F13/SDK | Contract Integration; E2E separately | Registry source → Core generated models → real F13 payload → consuming .NET/Unity example | Selected supported profile компилируется/десериализуется без drift; real application chain required for E2E claim |
| T13 / MVP constraints | Contract: actual exporter + real CM validator | Supplementary emoji/combining marks, exact Decimal MultipleOf/extremes, inclusive numeric/temporal bounds, Text entities/document order, List counts and recursive Item constraints | Standard keywords эквивалентны typed rules; CM extensions реально проверены; generic validator alone не proof; no trim/round/coercion |
| T14 / MVP equality/order | Contract + owner PostgreSQL projection | Object duplicates разных ItemKey, absent/default, FlagEnum set order, nested Lists, equal comparer keys, required ObjectFieldPath and incomplete keys | Full equality/hash-collision confirmation и path semantics сохранены; ties allowed, original authored array неизменён, invalid/incomplete values не дают uniqueness proof |
| T15 / old/new profiles | Literal golden + Integration | Constraints strengthened/weakened, Save/Publish/Copy fixtures F01–F04, old `/1` and new source/formal input | New constraints change definition hash under new profile; all recursive rules round-trip; historical bytes/hash/ETag unchanged; unsupported pairing rejects without downgrade |
| T16 / Offset contract | Literal exporter/CM temporal codec conformance + real owner PostgreSQL projection | Exact Naive/Offset definitions в Object/List, 7digits/100ns, date rollover, ±14:00 и local/UTC overflow, missing offset/-00:00/leap second, equivalent instants/bounds/unique/ties | Naive CM format и Offset `date-time` + strict CM checks сохранены; equivalent bounds имеют same new semantic hash, source pair unchanged; unknown semantics reject; F04 authoring offset и canonical UTC различены без host timezone |


Unit mocks source/profile failures/metadata are appropriate for pure export, но не доказывают PostgreSQL projection/lifecycle/canonical source. Owner Integration uses existing PostgreSQL/Testcontainers fixture, собственные schema/version rows и scoped cleanup; no in-memory provider замена. Source fixtures соответствуют target F01–F03, не current legacy DTO для «зеленого» теста.


Functional проверяет реальный вход одного CM API с own database/actual selected access context, не означает Unity E2E. Independent JSON Schema Draft 2020-12 validator/library adapter и approved F13 payload fixture используются по profile; C# compilation/generator tests находятся в F22, runtime typed deserialization в F19/F13. Golden expected bytes/schema/digest не вычисляются тем же production helper в тесте. Открытые profile/policy не выбираются через implicit expected result.

MVP acceptance включает реальный CM constraint/equality/order validator над shared literal fixtures, а не mocks extensions. Save type-invalid → reject; type-valid constraint violation → addressable Draft issue; strict Publish blocked; Copy type-compatible values по FieldId сохраняет target issues без превращения violation в orphan. Эти owner flows проверяются F01–F04 и используются здесь для source/export conformance.


F21 exporter/options suite — 38/38, с независимо написанными Boolean и all15 recursive literal bytes/digests. Public API + real PostgreSQL suite F21 — 9/9; полный App integration — 69/69; App architecture — 44/44. Проверены raw body/OpenAPI/ETag, frozen labels после rename, multiple exact versions/complete batch, историческое tooling чтение, corrupt hash/JSONB (missing metadata/null member), SQL input-size preflight, timeout и caller cancellation. Независимый `jsonschema==4.25.1` Draft202012Validator проверяет actual PostgreSQL→HTTP document и 57 instance cases через `tests/tools/F21SchemaValidation/validate.py`; selected temporal format checkers явно подключены. Эта проверка не заменяет CM sanitization/Decimal/Float semantic validation и не доказывает F22 codegen/SDK/Unity. F21 не добавляет migration.

Клиент UI сгенерирован существующей `generate-api-client` конфигурацией через live OpenAPI временного backend под Windows пользователем. `ISchemaRegistryClient.SchemaVersionsAsync` возвращает FileResponse; successful application/schema+json bypass сохраняет raw stream/bytes, header и lifetime. UI build и F01–F05/F13/F21 console runner проверяют новый transport без отдельной страницы. Временные приложения и disposable инфраструктура после проверок остановлены.


## 12. Порядок реализации


1. Реализовать зафиксированный Q31 formal profile `cm-json-schema/1` и проверить alignment с F13/Q19; exact/Retired/Withdrawn tooling-read и stable snapshot semantics уже приняты.
2. Реализовать owner F01–F03 target snapshot/number/recursive definitions и public source read models/port, без legacy current-name inference.
3. Owner Persistence source projection/specifications и query handlers; public source fields не содержат EF graph.
4. Pure Application exporter/validators/mappers, agreed deterministic comparison/refs/metadata. Не импортировать generator/Core в Registry.
5. Реализовать exact raw Schema API GET и Host/OpenAPI registration; internal batch F22 остаётся in-process, public batch в Demo не вводится.
6. Pure contract tests, real owner PostgreSQL/read/Functional checks и согласованная F21→F22→F13 round-trip matrix. Optional cache only после Q20, без DB/history по умолчанию.
7. MVP: materialize combined Q35/Q34 new schema-/instance-content/formal/C# profile golden bytes и extension metadata до exporter implementation; lossless constraints/comparer/TemporalSemantics projection, shared temporal codec/actual CM validator и old/new profile conformance без historical rehash.


## 13. Открытые технические вопросы

[Q37](../../frontend/questions/Q37-launcher-asset-delivery.md): конкретная delivery annotation, versioned source/formal integrity profiles и default encoding. F21–F22 не принимают illustrative `assetDefinition`/enum numeric values как согласованный контракт.

Q31 Demo formal profile закрыт. Для реализации остаются соседние детали и materialization новых MVP profiles:

- [Q15](../questions/Q15-api-and-source-code.md): concrete ProblemDetails machine codes, OpenAPI description/authorization metadata и общая error mapping infrastructure;
- [Q19 accepted canonical v1](../questions/Q19-content-hashes.md#принятый-canonical-profile-v1): selected source cm-schema-content/1 profile/hash separate Registry ETag; library/bytes verification engineering, not open mechanism;
- [Q20](../questions/Q20-persistence-and-migration.md): final query/storage details source snapshot; F21 не создаёт отдельный DbContext/таблицу Registry.
- [Q35](../../../business/questions/Q35-value-constraints.md)/[Q34](../../../business/questions/Q34-schema-field-semantics.md#datetime-mvp-temporal-semantics-и-совместимость)/[Q19](../questions/Q19-content-hashes.md)/[Q31](../questions/Q31-schema-code-generation.md): constraints и Offset semantics первого MVP согласованы; exact whole codec/bytes/identities и extension metadata нового schema-/instance-content/formal/C# набора остаются **Уточняется** до golden fixtures. Product temporal choice закрыт, naive `/1` не меняется.

Demo dialect, `$id`, type mapping, AssetKind, raw endpoint/media type, deterministic export, exact historical read и internal batch semantics больше не `Уточняется`; это не закрывает exact byte layouts новых MVP profiles.
