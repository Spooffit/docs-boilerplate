# F01. Создание схемы сущности: бэкенд


[Бизнесовая спецификация](../../../business/features/F01-create-entity-schema.md) · [Модуль M01](../modules/M01-content-authoring.md) · [Фронтенд](../../frontend/features/F01-create-entity-schema.md) · [Q08](../../../business/questions/Q08-keys-and-drafts.md) · [Q34](../../../business/questions/Q34-schema-field-semantics.md) · [Q15](../questions/Q15-api-and-source-code.md) · [Q20](../questions/Q20-persistence-and-migration.md)


## 1. Область реализации


Фича создаёт логическую `EntitySchema` и первый `EntitySchemaVersion` в статусе `Draft` одной атомарной операцией внутри ContentModeling. Для входа и просмотра результата включены список схем, exact-чтение логической схемы и exact Draft с recursive snapshot. Workspace F01 показывает результат readonly. Дальнейшее редактирование относится к [F02](F02-create-schema-version.md), публикация — к [F03](F03-publish-schema-version.md); их legacy mutations не считаются готовым target-контрактом.


Вход содержит обязательный client-generated OperationId:Guid, Name, Key, необязательное Description, InitialDraft.Title/ChangeComment и дерево новых полей. Сервер создаёт SchemaId, SchemaVersionId и каждый SchemaFieldId. Draft не имеет опубликованного номера, baseline и hash. Создание не меняет клиентскую выдачу, не создаёт экземпляры, не назначает контент продукту и не обращается к другим BC.


Demo работает без входа, ролей, модерации, лицензирования и шины. MVP использует доступ Specialist по [F16](F16-staff-access.md)/[Q10](../../../business/questions/Q10-permissions-and-isolation.md); конкретный permission здесь не изобретается.


Документ описывает контракт Demo и отдельные target-расширения MVP. Следующие исходники подтверждают наличие F01 в текущем checkout; наличие кода не заменяет результат сборки, PostgreSQL/HTTP/OpenAPI или browser-проверок. Планы MVP и F02/F03 не объявляются реализованными по этой таблице.


| Слой | Подтверждённый исходник | Наблюдаемое поведение F01 |
| --- | --- | --- |
| Domain | [EntitySchema](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Domain/EntitySchemas/EntitySchema.cs), [EntitySchemaVersion](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Domain/EntitySchemas/Entities/EntitySchemaVersion.cs), [receipt](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Domain/EntitySchemas/SchemaCreationReceipt.cs) | Metadata schema и independent initial Draft, собственные Version; durable original result receipt |
| Application.Abstractions | [request](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application.Abstractions/EntitySchemas/Models/CreateEntitySchemaRequest.cs), [details/list DTO](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application.Abstractions/EntitySchemas/Models/EntitySchemaDetailsDto.cs) | OperationId + InitialDraft + recursive definitions; public DTO без numeric token; internal VersionedResource<T> |
| Application | [CreateSchemaCommandHandler](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application/EntitySchemas/CommandHandlers/CreateSchemaCommandHandler.cs), [validator](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application/EntitySchemas/Validators/CreateSchemaCommandValidator.cs), [mapper](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application/EntitySchemas/Mappers/SchemaCreationMapper.cs) | Recursive validation/Trim, original normalized-input replay до key precheck; atomic schema/Draft/receipt |
| Api | [EntitySchemaController](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Api/EntitySchemas/Controllers/EntitySchemaController.cs), [version exact GET](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Api/EntitySchemas/Controllers/EntitySchemaVersionController.cs) | POST/root GET: Details DTO + own ETag; collection GET без lookup alias; exact Draft GET: snapshot + own ETag |
| EntityFramework | [DbContext](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.EntityFramework/ContentModelingDbContext.cs), [schema mapping](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.EntityFramework/Configurations/EntitySchemaConfiguration.cs), [version mapping](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.EntityFramework/Configurations/EntitySchemaVersionConfiguration.cs), [receipt mapping](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.EntityFramework/Configurations/SchemaCreationReceiptConfiguration.cs) | Separate roots/nullable published number, JSONB content, content_modeling.schema_creation_receipts; наличие migration не означает её применение в production |
| EntityFramework.Persistence | [query handlers](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.EntityFramework.Persistence/EntitySchemas/QueryHandlers/F01SchemaQueryHandlers.cs), [creation repository](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.EntityFramework.Persistence/EntitySchemas/Repositories/SchemaCreationRepository.cs) | Bounded summaries20 + total, list page20/max200/allowlisted sort; PostgreSQL transaction locks operation→key и specific duplicate-key mapping |

Защитный [EditorCommandHandler.RequireLegacyDefinition](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application/Editor/EditorCommandHandler.cs) отклоняет legacy Update/Publish нового authoritative recursive snapshot с400 до изменения content. Stored legacy schemas сохраняют прежнюю возможность записи. Это временная граница F01 до definition-aware F02/F03, а не новый Publish implementation.


## 2. Domain


### 2.1. Aggregates, entities и value objects


Все модели принадлежат ContentModeling. Между roots хранятся stable IDs; root схемы не загружает полные snapshots всех версий.


Обязательность в таблице относится к полю соответствующей модели. Для серверного состояния и result она не означает требование передать поле клиентом. `Условно` раскрывает lifecycle или variant; неизвестный wire omission/default остаётся `Уточняется`.


| Тип | Роль | Владелец / граница |
| --- | --- | --- |
| `EntitySchema` | Aggregate Root | Логическая identity, metadata, published pointer; Demo создаёт Active |
| `EntitySchemaVersion` | Aggregate Root | Snapshot одной версии; Draft mutable, Published content immutable; lifecycle F02/F03 |
| `EntitySchemaId`, `EntitySchemaVersionId`, `SchemaFieldId` | Типизированные identities | Схема, версия, логическое поле; серверные identity factories, существующие root factories используют Guid v7 |
| `EntitySchemaName`, `EntitySchemaKey`, `EntitySchemaDescription`, `SchemaFieldKey`, `SchemaFieldName` | Value Objects | Валидность смыслового значения; не владеют HTTP/БД |
| `SchemaVersionContent` | Value Object | Полное дерево snapshot, immutable-by-replacement; коллекции защищены от изменения |
| `SchemaField` | Snapshot node, не EF row | Stable identity поля между версиями; definition принадлежит snapshot |
| `SchemaValueDefinition` и варианты | Рекурсивные Value Objects | Определение значения; anonymous List.Item без отдельного FieldId |
| `SchemaOption` | Value Object | Identity — Key внутри Enum/FlagEnum; отдельного Guid нет |


**`EntitySchema`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Id` | `EntitySchemaId` | Да | Identity схемы, создаёт сервер | Stable ID |
| `Name` | `EntitySchemaName` | Да | Текущее имя логической схемы | VO; ограничения §2.3 |
| `Key` | `EntitySchemaKey` | Да | Ключ схемы | Глобально уникален, case-sensitive; Demo profile §2.3 |
| `Description` | `EntitySchemaDescription?` | Нет | Необязательное описание | После внешнего Trim ≤1024; blank → null; §2.3 |
| `Lifecycle` | `EntitySchemaLifecycle` | Да | Состояние логической схемы | Active/Archived; Demo create Active |
| `LatestPublishedVersionId` | `EntitySchemaVersionId?` | Условно | Текущий Published pointer | Null до первой публикации; тот же owner; меняется вместе с номером |
| `LatestPublishedVersionNumber` | `int?` | Условно | Номер текущей Published | Null до первой публикации; положительная последовательность F03 |
| `Version` | `int` | Да | Внутренний concurrency token root | Серверный; не DTO; initial/increment по persistence convention |
| `CreationTime` | `DateTimeOffset` | Да | Серверный audit создания | Базовый AuditedAggregateRoot |
| `ModificationTime` | `DateTimeOffset?` | Условно | Серверный audit изменения | Базовый AuditedAggregateRoot; отсутствует до изменения |


**`EntitySchemaVersion`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Id` | `EntitySchemaVersionId` | Да | Identity версии, создаёт сервер | При Publish сохраняется |
| `SchemaId` | `EntitySchemaId` | Да | Логический owner | Stable reference между roots |
| `Status` | `PublicationStatus` | Да | Состояние версии | Draft/Published; Demo create Draft |
| `VersionNumber` | `int?` | Условно | Бизнесовый номер | Draft null; Published positive/unique per schema; immutable |
| `Title` | `string` | Да | Заголовок Draft/version | После внешнего Trim 1–255; §2.3 |
| `ChangeComment` | `string?` | Нет | Комментарий изменения | После внешнего Trim ≤1024; blank → null; §2.3 |
| `BasePublishedVersionId` | `EntitySchemaVersionId?` | Условно | Сохранённая Published основа Draft | Первый Draft null; F02 copy/accept; F03 stale check |
| `Content` | `SchemaVersionContent` | Да | Полное дерево snapshot | Непустое; Draft replacement; Published immutable |
| `CreatedAt` | `DateTimeOffset` | Да | Серверное время создания версии | Не меняется при Publish |
| `PublishedSchemaName` | `string?` | Условно | Frozen имя схемы | Draft null; Published заполнено/immutable |
| `PublishedSchemaKey` | `string?` | Условно | Frozen ключ схемы | Draft null; Published заполнено/immutable |
| `CanonicalContentJson` | `string?` | Условно | Frozen canonical representation | Draft null; Published заполнено/immutable; bytes Q19 |
| `ContentHash` | `ContentHash?` | Условно | Hash canonical bytes | Draft null; Published заполнено/immutable; VO/hash profile Q19 |
| `PublishedAt` | `DateTimeOffset?` | Условно | Серверное время Publish | Draft null; Published заполнено/immutable |
| `RetiredAt` | `DateTimeOffset?` | Условно | Серверная lifecycle metadata | F02 Retire/Restore; не меняет snapshot |
| `WithdrawnAt` | `DateTimeOffset?` | Условно | Серверная lifecycle metadata | Withdrawal F13; не меняет snapshot |
| `Version` | `int` | Да | Внутренний concurrency token версии | Серверный; не DTO; собственный owner |


**`EntitySchemaId`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Value` | `Guid` | Да | Типизированная identity | Server identity factory; stable ID |


**`EntitySchemaVersionId`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Value` | `Guid` | Да | Типизированная identity | Server identity factory; stable ID |


**`SchemaFieldId`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Value` | `Guid` | Да | Типизированная identity | Server identity factory; stable ID |


**`EntitySchemaName`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Value` | `string` | Да | Смысловое значение VO | После внешнего Trim 1–255; §2.3 |


**`EntitySchemaKey`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Value` | `string` | Да | Смысловое значение VO | Global uniqueness, case-sensitive; Demo `^[a-z][a-zA-Z0-9]*$`, 1–50 после Trim; casing сохраняется |


**`EntitySchemaDescription`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Value` | `string` | Да | Смысловое значение VO | После внешнего Trim 1–1024; отсутствие представляет null VO; §2.3 |


**`SchemaFieldKey`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Value` | `string` | Да | Смысловое значение VO | Demo `^[a-z][a-zA-Z0-9]*$`, 1–50 после Trim; case-sensitive whole-tree uniqueness; casing сохраняется; immutable identity F02/F03 |


**`SchemaFieldName`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Value` | `string` | Да | Смысловое значение VO | После внешнего Trim 1–255; mutable label; §2.3 |


**`SchemaVersionContent`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Fields` | `IReadOnlyList<SchemaField>` | Да | Полное recursive дерево | Минимум одно поле; whole-tree unique IDs/keys; protected collection |


**`SchemaField`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Id` | `SchemaFieldId` | Да | Stable identity named поля | Server-generated; unique whole-tree; сохраняется между версиями |
| `Key` | `SchemaFieldKey` | Да | Semantic key поля | Unique whole-tree; immutable F02/F03 |
| `Name` | `SchemaFieldName` | Да | Изменяемая подпись | VO constraints §2.3 |
| `IsRequired` | `bool` | Да | Обязательность instance value | Deprecated запрещает true; default не назначен; F02 monotonic |
| `Deprecated` | `bool` | Да | Отказ от поля | При true IsRequired=false; default не назначен; F02 monotonic |
| `HelpText` | `string?` | Нет | Подсказка поля | После внешнего Trim ≤1024; blank → null; §2.3 |
| `Definition` | `SchemaValueDefinition` | Да | Definition конкретного snapshot | Ровно один known variant; recursive shape |
| `Type` | `FieldDataType` | Да | Вычисляемый тип из Definition | Не независимое изменяемое состояние; known 15 types |


**`SchemaValueDefinition`** — общая часть вариантов; anonymous List.Item без FieldId


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Type` | `FieldDataType` | Да | Дискриминатор recursive definition | Known type; ровно одно соответствующее type-specific состояние |
| `ValueConstraints` | `SchemaValueConstraints?` | Нет | MVP ограничения этого definition | Только применимый typed variant; null без дополнительных rules; вне Demo |


**`SchemaOption`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Key` | `string` | Да | Identity option | Unique внутри конкретного Enum/FlagEnum; no numeric Value/BitValue |
| `Name` | `string` | Да | Подпись option | После внешнего Trim 1–255; §2.3; mutable F02/F03 |
| `Deprecated` | `bool` | Да | Отказ от option | Default не назначен; evolution F02/F03 |


| Тип definition | Структура |
| --- | --- |
| `ScalarValueDefinition` | String, Text, Integer, Float, Decimal, Boolean, Date, TimeSpan, Color; shared typed ValueConstraints |
| `DateTimeValueDefinition` | DateTime + immutable TemporalSemantics; Demo/historical Naive, new MVP Offset |
| `AssetReferenceValueDefinition` | AssetReference + обязательный `AssetKind` |
| `EnumValueDefinition` | Enum |
| `FlagEnumValueDefinition` | FlagEnum; без numeric bitmask |
| `ObjectValueDefinition` | Object; каждый child получает FieldId |
| `ListValueDefinition` | List, включая List<List<T>> и List<Object> |


`ScalarValueDefinition` использует общую часть; Demo без settings, MVP с применимыми typed ValueConstraints. `AssetReference` не scalar-without-settings: его semantic definition содержит kind.


**`DateTimeValueDefinition`** — целевой model, Type/ValueConstraints inherited из SchemaValueDefinition; не обнаруженный source class.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `TemporalSemantics` | `DateTimeTemporalSemantics` (целевой enum) | Да | Naive либо Offset | Immutable semantic identity: Demo/historical Naive, new MVP fields Offset; не mutable UI selector. Naive→Offset требует нового FieldId |

Logical transport DateTimeValueDefinitionDto переиспользует эти fields в Application.Abstractions; concrete union carrier Q15. Исторический /1 input без discriminator означает Naive **только в explicitly supported /1 reader**, не global fallback unknown semantics. New known input имеет explicit definition semantics; всего 15 FieldDataType сохраняются. Offset value codec/Domain instance values владелец F04.

**`AssetReferenceValueDefinition`** — общий Type определён в SchemaValueDefinition


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `AssetKind` | `AssetKind` | Да | Допустимый вид asset | Domain enum: Image/Audio/Video; extensions/MIME не хранятся в schema и приходят из F05 policy |
| `DeliveryMode` | `AssetDeliveryMode` (целевой domain enum) | Да | Effective режим доставки для этого leaf definition; default `OnDemand` | После Demo: `OnDemand`, `Preload`, `RequiredBeforeLaunch`; Published immutable; numeric values/transport placement/profile [Q37](../../frontend/questions/Q37-launcher-asset-delivery.md) |


`AssetKind` является стабильным value contract между ContentModeling и MediaLibrary; physical placement не должно создавать зависимость ContentModeling.Domain → MediaLibrary implementation.

Режим принадлежит definition узла `AssetReference`, включая anonymous `List.Item` и leaf внутри nested Object/List, а не контейнеру, MediaAsset или instance value. `OnDemand` не запускает автоматическую предзагрузку; `Preload` допускает предварительную/фоновую загрузку без блокировки запуска; `RequiredBeforeLaunch` требует verified bytes присутствующей ссылки до запуска соответствующего snapshot. `IsRequired` независимо определяет валидность присутствия instance value. Variant-level override не вводится. Это post-Demo extension; текущие create/read DTO и bytes профилей `/1` этим описанием не расширяются.


**`EnumValueDefinition`** — общий Type определён в SchemaValueDefinition


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Options` | `IReadOnlyList<SchemaOption>` | Да | Options definition | Минимум одна; unique option keys |


**`FlagEnumValueDefinition`** — общий Type определён в SchemaValueDefinition


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Options` | `IReadOnlyList<SchemaOption>` | Да | Options definition | Минимум одна; unique option keys |


**`ObjectValueDefinition`** — общий Type определён в SchemaValueDefinition


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Fields` | `IReadOnlyList<SchemaField>` | Да | Named child fields | Минимум один child; whole-tree unique field IDs/keys |


**`ListValueDefinition`** — общий Type определён в SchemaValueDefinition


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Item` | `SchemaValueDefinition` | Да | Anonymous item definition | Recursive; nested List/Object допустимы; no FieldId |
| `OrderMode` | `ListOrderMode` (целевой enum) | Да | Порядок элементов | Demo round-trip; MVP Default/Ascending/Descending validation; wire/default Q15 |
| `Comparer` | `ListComparerDefinition?` | Условно | MVP ключ сравнения | Default: null; Asc/Desc: supported comparer Q35; collection constraints в ValueConstraints, item constraints в Item |


`InheritanceStrategy`, `ColorFormat`, numeric option Value/BitValue и SchemaField.SortOrder в target отсутствуют. Color имеет единый runtime-формат `#RRGGBBAA`. String runtime value ограничен 255; это не длина Name/Key поля. DateTime в Demo Naive/Unspecified без offset; new MVP DateTime Offset по Q34, Date — календарная дата, TimeSpan допускает zero/negative; настройки instance values относятся к F04. List.OrderMode сохраняется в Demo, расширенный comparator относится к [Q35](../../../business/questions/Q35-value-constraints.md).


Целевые модели MVP ниже описывают Q35, а не найденные исходники. Definitions immutable-by-replacement и принадлежат SchemaVersion snapshot; статические EntitySchemaVersionConstraints не подменяют пользовательские ValueConstraints. Поля ниже — единственное логическое описание соответствующих DTO §3.3.

**`SchemaValueConstraints`**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Kind` | `SchemaConstraintKind` (целевой enum) | Да | Discriminator typed variant | StringLength/TextLength/Integer/Float/Decimal/Date/DateTime/TimeSpan/List; соответствует Type definition; unknown rejected |

**`StringValueConstraints`**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `MinLength` | `int?` | Нет | Минимальная длина | Unicode code points; intrinsic min 1 не ослабляется |
| `MaxLength` | `int?` | Нет | Максимальная длина | Unicode code points; intrinsic max 255 не ослабляется; min ≤ max |

**`TextValueConstraints`**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `MinTextLength` | `int?` | Нет | Минимум полезного текста | Non-negative; decoded sanitized text nodes; semantic-empty отдельно |
| `MaxTextLength` | `int?` | Нет | Максимум полезного текста | Non-negative; min ≤ max; не max HTML bytes |

**`NumericValueConstraints<T>`**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Minimum` | `T?` | Нет | Inclusive нижняя граница | T = long/double/decimal; Float finite |
| `Maximum` | `T?` | Нет | Inclusive верхняя граница | Minimum ≤ Maximum; intrinsic representation |
| `MultipleOf` | `T?` | Условно | Положительный точный шаг | Только Integer/Decimal; Float rejected; без epsilon/rounding |

**`TemporalValueConstraints<T>`**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Minimum` | `T?` | Нет | Inclusive нижняя граница | Date: DateOnly; DateTime: DateTime для Naive либо DateTimeOffset для Offset согласно exact definition; TimeSpan: TimeSpan |
| `Maximum` | `T?` | Нет | Inclusive верхняя граница | Minimum ≤ Maximum; Offset bounds требуют offset и сравниваются по UTC instant, Naive без conversion; dynamic/cross-field rules нет |

**`ListValueConstraints`**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `MinItems` | `int?` | Нет | Минимальный count | Non-negative; required не означает непустой |
| `MaxItems` | `int?` | Нет | Максимальный count | Non-negative; min ≤ max |
| `UniqueItems` | `bool` | Да | Проверять full runtime equality | false без uniqueness rule; exact wire default Q15; ItemKey исключён |

**`ListComparerDefinition`**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Kind` | `ListComparerKind` (целевой enum) | Да | ItemValue/ObjectFieldPath | Known variant; не expression |
| `FieldPath` | `IReadOnlyList<SchemaFieldId>?` | Условно | Ordered immutable field identity path | ObjectFieldPath nonempty, Object-only transitions, весь path/leaf required; ItemValue без path |

Generic notation задаёт known typed variants, не свободный T/type name из HTTP. Boolean/Enum/FlagEnum/Color/Object/AssetReference дополнительных ValueConstraints этого набора не имеют. ListOrderMode/ListComparerKind — целевые enums, wire Q15. Comparator String/Enum — UTF-16 ordinal semantics; String length — code points, разные операции.

**`SchemaCreationReceipt`** — immutable durable result успешного Create, владелец ContentModeling:

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `OperationId` | `Guid` | Да | Identity client attempt/PK | Non-empty, original attempt; не SchemaId |
| `InputJson` | `string` | Да | Parsed normalized input в receipt profile | Metadata/keys Trim, optional blank null, recursive definitions; exact ordinal serialized comparison |
| `ResultJson` | `string` | Да | Original EntitySchemaDetailsDto | Сохранённый initial result, не новый current projection |
| `LogicalVersion` | `int` | Да | Original logical ETag basis | ≥1, internal, не public DTO property |
| `CreatedAt` | `DateTimeOffset` | Да | Server receipt time | Один commit с schema/Draft; совпадает с initial creation basis |

### 2.2. Методы и переходы


Сигнатуры ниже определяют ответственность целевых фабрик; существующий API фабрик требует перестройки, а не механического переименования.


| Тип | Метод | Параметры | Предусловия | Изменение / результат |
| --- | --- | --- | --- | --- |
| `EntitySchema` | `CreateMetadata(...)` | `id: EntitySchemaId`, `key: EntitySchemaKey`, `name: EntitySchemaName`, `description: EntitySchemaDescription?`, `initialDraftId: EntitySchemaVersionId` | Valid IDs/VO; глобальная uniqueness проверяется Application + DB | Active schema, published pointer/number = null, initial internal Version; Draft отдельный root |
| `EntitySchemaVersion` | `CreateInitialDraft(...)` | `id: EntitySchemaVersionId`, `schemaId: EntitySchemaId`, `title: string`, `changeComment: string?`, `content: SchemaVersionContent`, `createdAt: DateTimeOffset` | Valid metadata и дерево | Draft, VersionNumber/base/published metadata = null; CreatedAt — серверное время; initial internal Version |
| `SchemaVersionContent` | `Create(fields)` | Полное дерево новых `SchemaField` | Непустое дерево, уникальные IDs и keys во всём snapshot, valid definitions | Immutable-by-replacement content либо domain failure |
| `SchemaField` | `Create(...)` | Новый server ID, key/name, required/deprecated/help text, definition | Valid VO/definition; Deprecated запрещает Required | Snapshot нового логического поля |
| Definitions / `SchemaOption` | Type-specific `Create(...)` | Значения из 2.1 | Нет чужих type-настроек, неизвестных enum values; коллекции структурно полны | Валидный рекурсивный definition / option |


Методы Rename/RegisterPublishedVersion/Archive/Restore и изменения Draft описываются в F02/F03; F01 их не вызывает. Опубликованные поля и canonical content не получают Update.


### 2.3. Инварианты и constraints


Единый источник постоянных ограничений — Domain constraints по [S21](../standards/S21-aggregate-constraints.md). Schema metadata принадлежат `EntitySchemaConstraints`; version metadata и subtree — целевому `EntitySchemaVersionConstraints`. Переиспользуемые field/option constraints остаются рядом с соответствующими VO и подключаются владельцем, без копирования literals в Application/EF.


| Инвариант | Защита |
| --- | --- |
| Schema Key глобально уникален с case-sensitive сравнением | Application precheck + DB unique index одной семантики; внутри объекта нельзя доказать отсутствие другой схемы |
| Demo schema/field keys вводятся явно в camelCase: `^[a-z][a-zA-Z0-9]*$`, длина 1–50 | Domain/validator одного профиля; VO удаляет внешние whitespace до проверки и хранения, сохраняет регистр без Camelize/case folding. Внешний Trim согласован; переход к MVP kebab-case suggestion — Q15 |
| Минимальный Draft имеет Name, Key, Title и хотя бы одно поле | Request validator + domain factories; Description/ChangeComment/HelpText необязательны |
| Уникальность field IDs и case-sensitive keys во всём дереве | Один recursive traversal SchemaVersionContent с StringComparer.Ordinal по нормализованным keys, включая Object внутри List и вложенные Lists; siblings-only проверка недостаточна |
| Все 15 типов и только их применимые definitions | Known type + recursive shape validator; Object/Enum/FlagEnum непустые; List содержит Item |
| Deprecated field optional | Domain guard + request validation; существующая identity/evolution проверяется при дальнейшей работе F02/F03 |
| Первый Draft ненумерован и не Published | CreateInitialDraft; initial baseline, timestamps публикации, hash и frozen schema metadata отсутствуют |
| Draft creation не меняет published pointer | EntitySchema.Create возвращает оба published-поля null; F01 не вызывает publication methods |


Required означает наличие type-valid instance value, а не отличие от CLR default; instance values в F01 не передаются. Cross-version evolution, stale-base и совместимость с Published history здесь не проверяются: у новой схемы истории нет.


Demo schema/field key profile принят: после Trim длина 1–50, ASCII camelCase regex выше, сравнение StringComparison.Ordinal/StringComparer.Ordinal. `fieldKey` и `fieldkey` различаются; исходный регистр проходит через mapper, persistence и response без изменения.

| Authoring metadata | Constraints после внешнего Trim | Хранимое значение |
| --- | --- | --- |
| Schema Name, Draft Title, FieldName (field Name), optionName (option Name) | Required, 1–255 символов | Непустая строка; blank недопустим |
| Description, ChangeComment, HelpText | Optional, ≤1024 символов | Отсутствие/empty/whitespace → null; иначе trimmed строка |

Это согласованные target-ограничения [Q15](../questions/Q15-api-and-source-code.md#принятый-f01-metadata-contract), а не доказательство готовой реализации. Нормализация удаляет только внешние whitespace, сохраняет внутренние пробелы и регистр и предшествует length/required checks и сохранению. VO, FV и mapper применяют одну семантику; optional null не заменяется служебным текстом. Политика не распространяется на runtime values и MVP typed constraints. Demo Text operational cap — Q34/Q22 после fixture; advanced per-field constraints — Q35/MVP. Длина metadata инженерно измеряется в UTF-16 code units по .NET String.Length/FluentValidation MaximumLength, как в существующих VO; это не измерение runtime String constraints в Unicode code points. Recursive wire и read-profile материализуются инженерно в Q15, без повторного выбора числовых лимитов.


MVP DefinitionConstraintsPolicy проверяет применимость union, typed bounds/positive step, recursive constraints и comparer. ObjectFieldPath разрешается в Item definition по IDs; required chain, supported scalar leaf. Default с comparer и Asc/Desc без supported comparer invalid. Смена constraints/comparer в новой version сама по себе не меняет FieldId; historical key/type/container/monotonic guards — F02/F03. Intrinsic String length — code points. Правила [Q35](../../../business/questions/Q35-value-constraints.md), без новых type-specific extras.

DateTimeValueDefinition factory фиксирует TemporalSemantics; existing field не меняет её через settings update. Offset temporal constraints содержат DateTimeOffset values с known offset ±14:00 whole minutes и valid local/UTC range; immutable semantic bounds сравниваются по instant. Definition mapper не определяет semantics по строке значения/host timezone; historical /1 reader явный. Q34 temporal semantics закрыты, whole new-profile codec Q19/Q31 материализуется отдельно.

### 2.4. Domain events


F01 не требует integration event, outbox или внешнего consumer: создание authoring Draft не меняет Delivery. Текущий `EntitySchemaCreatedDomainEvent` существует, но в просмотренной F01-цепочке не найден необходимый потребитель. При переходе к target событие сохраняется только при конкретной внутренней обязанности; его наличие не является основанием включать RabbitMQ в Demo. Domain event не используется вместо transaction и не создаётся только ради лога.


**`EntitySchemaCreatedDomainEvent`** — наблюдаемый [current event](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Domain/EntitySchemas/DomainEvents/EntitySchemaCreatedDomainEvent.cs); не обязательное target событие


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `SchemaId` | `EntitySchemaId` | Да | Identity созданной схемы | Current payload; target necessity/consumer [Q15](../questions/Q15-api-and-source-code.md) |
| `OccurredAt` | `DateTimeOffset` | Да | Серверное время domain fact | Current default DateTimeOffset.UtcNow; не client input; target consumer/rollback [Q15](../questions/Q15-api-and-source-code.md) |


## 3. Application.Abstractions


### 3.1. Commands


| Command | Result | Concurrency |
| --- | --- | --- |
| `CreateSchemaCommand` | `Result<VersionedResource<EntitySchemaDetailsDto>>` | Новые roots; expected Version не принимается; OperationId и unique-key race разрешает storage |


**`CreateSchemaCommand`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Request` | `CreateEntitySchemaRequest` | Да | Input создания схемы и Draft | Без server IDs и expected Version |


Public DTO не содержит технического Version/Revision/ExpectedVersion. Внутренний `VersionedResource<T>(Resource, Version:int)` передаёт metadata через CQRS; Api публикует только Resource и strong numeric ETag header. Carrier не становится частью response JSON.


### 3.2. Queries


| Query | Result |
| --- | --- |
| `ListSchemasQuery` | `Result<OffsetPagedResultDto<EntitySchemaListItemDto>>` |
| `GetSchemaDetailsQuery` | `Result<VersionedResource<EntitySchemaDetailsDto>>` |


**`ListSchemasQuery`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Request` | `EntitySchemaListRequest` | Да | Search по Name/Key, offset page и sort | Таблица request ниже; defaults/allow-list инженерно выбраны |


**`GetSchemaDetailsQuery`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Id` | `Guid` | Да | Route identity схемы | Exact read; без fallback |


List/read handlers находятся в EntityFramework.Persistence по [S20](../standards/S20-cqrs.md). Exact version GET используется для readonly initial Draft в F01; дальнейшее редактирование F02 отдельно. Старые GetAllEntitySchemasQuery/EntitySchemaLookupRequestDto не являются F01 collection contract; major/minor-фильтров в нём нет.


### 3.3. DTO и parameter models


Эти определения — единственное описание внешних моделей в F01; frontend ссылается на них и использует generated NSwag types. JSON использует camelCase properties и string enum names; definition.type выбирает один known variant с только применимым payload. Unknown members на create/request nodes запрещены. OpenAPI → NSwag recursive round-trip проверяется отдельно, наличие CLR DTO не доказывает успешную генерацию.


**`CreateEntitySchemaRequest`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `OperationId` | `Guid` | Да | Client-generated identity попытки Create | Non-empty; replay с тем же normalized input; не SchemaId и не auth token |
| `Name` | `string` | Да | Имя логической схемы | После внешнего Trim 1–255; §2.3 |
| `Key` | `string` | Да | Ключ схемы | Global unique, case-sensitive; Demo key profile §2.3 |
| `Description` | `string?` | Нет | Описание схемы | После внешнего Trim ≤1024; blank → null; §2.3 |
| `InitialDraft` | `CreateSchemaDraftRequest` | Да | Первый Draft | Без опубликованного номера/base/hash |


**`CreateSchemaDraftRequest`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Title` | `string` | Да | Заголовок первого Draft | После внешнего Trim 1–255; §2.3 |
| `ChangeComment` | `string?` | Нет | Комментарий | После внешнего Trim ≤1024; blank → null; §2.3 |
| `Fields` | `IReadOnlyList<CreateSchemaFieldRequest>` | Да | Новое recursive дерево | Минимум одно поле; unique keys whole-tree; без IDs |


**`CreateSchemaFieldRequest`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Key` | `string` | Да | Ключ нового поля | Case-sensitive whole-tree unique; Demo key profile §2.3 |
| `Name` | `string` | Да | Подпись поля | FieldName constraints §2.3 |
| `IsRequired` | `bool` | Да | Required instance value | JSON member required, в том числе false; Deprecated запрещает true |
| `Deprecated` | `bool` | Да | Отказ от поля | JSON member required, в том числе false; при true optional |
| `HelpText` | `string?` | Нет | Подсказка | После внешнего Trim ≤1024; blank → null; §2.3 |
| `Definition` | `CreateSchemaValueDefinitionDto` | Да | Recursive definition | Known variant; applicable settings only |


**`CreateSchemaValueDefinitionDto`** — Demo discriminator Type + applicable payload; MVP constraints/comparer ниже — target-расширения, не часть текущего Demo request


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Type` | `FieldDataType` | Да | Discriminator | Known 15 types, string enum name в JSON |
| `TemporalSemantics` | `DateTimeTemporalSemantics` | Условно | DateTime semantic definition | Только DateTime; Demo Naive/new MVP Offset; immutable after creation, concrete discriminator wire Q15; unknown rejected |
| `ValueConstraints` | `SchemaValueConstraintsDto?` | Нет | MVP typed constraints | Логические поля/variants §2.1; не free JSON rules bag |
| `Comparer` | `CreateListComparerDefinitionDto?` | Условно | MVP List comparer | Только List Asc/Desc; ItemValue/ObjectFieldPath; new-field reference wire Q15; client IDs новых fields запрещены |
| `Options` | `IReadOnlyList<CreateSchemaOptionDto>` | Условно | Payload Enum/FlagEnum | Только Enum/FlagEnum; непусто/unique keys |
| `Fields` | `IReadOnlyList<CreateSchemaFieldRequest>` | Условно | Payload Object | Только Object; непусто; IDs серверные |
| `Item` | `CreateSchemaValueDefinitionDto` | Условно | Payload List | Только List; обязателен; recursive/no FieldId |
| `OrderMode` | `ListOrderMode?` | Условно | Порядок List | Только List, обязательный Default/Ascending/Descending; string enum name |
| `AssetKind` | `AssetKind?` | Условно | Kind AssetReference | Только AssetReference, обязательный Image/Audio/Video; string enum name; extension allowlist здесь запрещён |


**`CreateSchemaOptionDto`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Key` | `string` | Да | Identity option | Unique внутри Enum/FlagEnum; no numeric value |
| `Name` | `string` | Да | Подпись | После внешнего Trim 1–255; §2.3 |
| `Deprecated` | `bool` | Да | Отказ от option | JSON member required, в том числе false |


**`EntitySchemaDetailsDto`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Id` | `Guid` | Да | Identity схемы | Серверная |
| `Name` | `string` | Да | Текущее имя | Logical metadata |
| `Key` | `string` | Да | Ключ схемы | Global uniqueness |
| `Description` | `string?` | Нет | Описание | Nullable VO projection |
| `Lifecycle` | `EntitySchemaLifecycle` | Да | Состояние схемы | Active/Archived, string enum name |
| `LatestPublishedVersionId` | `Guid?` | Условно | Current Published pointer | Null до публикации; create-result null |
| `LatestPublishedVersionNumber` | `int?` | Условно | Current Published number | Null до публикации; create-result null |
| `Versions` | `IReadOnlyList<EntitySchemaVersionSummaryDto>` | Да | Bounded summaries без full trees | Create ровно первый Draft; root GET максимум20, CreatedAt DESC + Id DESC |
| `VersionsTotalCount` | `int` | Да | Общее число versions этой схемы | Create1; root GET total независимо от bound20; не concurrency token |


**`EntitySchemaVersionSummaryDto`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Id` | `Guid` | Да | Identity версии | Не version ETag |
| `Status` | `PublicationStatus` | Да | Состояние версии | Draft/Published, string enum name |
| `VersionNumber` | `int?` | Условно | Business number | Draft null; Published заполнен |
| `Title` | `string` | Да | Заголовок | Version metadata |
| `ChangeComment` | `string?` | Нет | Комментарий | Version metadata |
| `BasePublishedVersionId` | `Guid?` | Условно | Сохранённая основа | Первый Draft null; F02 copy/accept |
| `IsStale` | `bool` | Да | Derived stale-base flag | Сравнение baseline/current pointer; create false |
| `IsRetired` | `bool` | Да | Derived retired flag | По timestamp; create false |
| `IsWithdrawn` | `bool` | Да | Derived withdrawn flag | По timestamp; create false |
| `CreatedAt` | `DateTimeOffset` | Да | Server creation time | Сохранённое время |
| `PublishedAt` | `DateTimeOffset?` | Условно | Server Publish time | Draft null; Published immutable |


**`EntitySchemaListItemDto`**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Id` | `Guid` | Да | Identity схемы | Без per-row ETag |
| `Name` | `string` | Да | Текущее имя | Projection без full Content |
| `Key` | `string` | Да | Ключ схемы | Projection без full Content |
| `DraftCount` | `int` | Да | Число Draft | Server projection |
| `LatestPublishedVersionId` | `Guid?` | Условно | Current Published pointer | Null если Published нет |
| `LatestPublishedVersionNumber` | `int?` | Условно | Current Published number | Null если Published нет |
| `LatestPublishedVersionIsWithdrawn` | `bool?` | Условно | Withdrawal именно latest Published | Null если Published нет; derived, не stored logical flag |
| `ModificationTime` | `DateTimeOffset?` | Условно | Server audit modification | По audit convention |


ListItem — projection минимальных колонок из [Q13](../../frontend/questions/Q13-ui-scope.md). Nullable withdrawal flag означает отсутствие Published version при null, иначе withdrawal именно latest Published version; это не отдельный stored флаг logical schema. List не содержит trees, summaries каждой версии и concurrency tokens. Область ordinary/archived lists F02 отдельно.

**`EntitySchemaListRequest`** — query-bound properties, одинаковые для API и generated client:

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Search` | `string?` | Нет | Search по Name OR Key | До255 UTF-16 code units; outer Trim при чтении, blank = no search |
| `SkipCount` | `int` | Нет | Offset | Default0, ≥0 |
| `MaxResultCount` | `int` | Нет | Размер страницы | Default20, 1–200 |
| `Sorting` | `string?` | Нет | Allowlisted sort | Default Name ASC; Name ASC/DESC или ModificationTime ASC/DESC, case-insensitive parse; Id ASC stable tie-breaker |


SchemaValueConstraintsDto — known typed transport union в Application.Abstractions без Domain dependency. StringValueConstraintsDto/TextValueConstraintsDto/NumericValueConstraintsDto<T>/TemporalValueConstraintsDto<T>/ListValueConstraintsDto имеют логические поля §2.1 без второго определения правил. Immutable Domain objects создаёт mapper, generated NSwag DTO редактирует UI. Offset bound syntax/range/UTC semantic comparison приняты Q34; concrete transport union/discriminator остаются Q15.

**`CreateListComparerDefinitionDto`**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| `Kind` | `ListComparerKind` | Да | ItemValue/ObjectFieldPath | Applicable List variant; wire Q15 |
| `FieldPath` | Уточняется — request field references Q15 | Условно | Ссылка на новый scalar field внутри Item | ObjectFieldPath nonempty; create не принимает IDs новых fields; exact reference/request-location wire Q15; mapper разрешает после server ID assignment |

Create/add fields не получают client-generated Guid ради comparer. Mapping сначала назначает IDs всему дереву, затем разрешает references и повторяет guards. До определения new-field reference wire и OpenAPI/NSwag fixture этот MVP transport остаётся открытым; не обходится скрытым сохранением invalid comparer. Read/known-existing path — ListComparerDefinitionDto F02.

Create definitions:


| Type | Вариант |
| --- | --- |
| String/Text/Integer/Float/Decimal/Boolean/Date/TimeSpan/Color | Scalar общая часть; MVP применимый typed ValueConstraints; runtime value/legacy settings отсутствуют |
| DateTime | DateTimeValueDefinitionDto: TemporalSemantics + typed bounds; Demo/historical Naive, new MVP Offset; logical fields §2.1 |
| AssetReference | AssetKind variant CreateSchemaValueDefinitionDto |
| Enum/FlagEnum | Options variant CreateSchemaValueDefinitionDto |
| Object | Fields variant CreateSchemaValueDefinitionDto |
| List | Item/OrderMode + MVP collection constraints/Comparer; Item constraints recursive |


CreateSchemaFieldRequest, вложенные definitions и root request **не содержат server IDs**, nullable Id, IsNew, client-generated identity, опубликованный номер и hash. Option identity — Key, не numeric Value/BitValue. Новые ID возвращаются при exact-чтении версии в F02; create-result содержит Id logical schema и Id initial Draft summary для немедленного перехода.


В create-result Versions содержит ровно первый Draft и VersionsTotalCount=1: Status Draft, VersionNumber/BasePublishedVersionId/PublishedAt = null; IsStale/IsRetired/IsWithdrawn = false. Оба published-поля schema равны null. GetSchemaDetailsQuery возвращает metadata и максимум20 summaries с total без full JSON content; полная история F02 отдельно, не скрывается в загрузке aggregate.


Domain parameter objects — VO и definitions раздела 2, а не копия transport DTO с теми же полями. Update-child DTO с обязательным Id принадлежит F02 и не объединяется с create DTO.


### 3.4. Ports


| Port | Назначение / минимальные операции | Направление |
| --- | --- | --- |
| `IEntitySchemaRepository` (target вместо current IEntitySchemasRepository) | `ExistsByKeyAsync(EntitySchemaKey, CancellationToken): Task<bool>`; `AddAsync(EntitySchema, CancellationToken): Task<Result>`; другие операции root описывает F02 | Application → command storage; контракт Domain repositories |
| `IEntitySchemaVersionRepository` (target) | `AddAsync(EntitySchemaVersion, CancellationToken): Task<Result>`; загрузка/mutations версий относятся к F02 | Application → command storage; root-level repository, не repository каждого field |
| `IUnitOfWorkManager` / command UoW boundary | Открыть scope, rollback failed result, сохранить/commit оба roots перед передачей result в Api | CQRS pipeline → local transaction infrastructure |
| `TimeProvider` | Получить единый server CreatedAt; текущая Host composition уже регистрирует TimeProvider.System | Application → clock; параллельный IClock не требуется |
| `IDbContextProvider<ContentModelingDbContext>` | Доступ repositories/query adapters к context данного BC | Только EntityFramework/EntityFramework.Persistence → EF infrastructure; не Domain/Application |


Repository signatures — target-контракт; текущий plural repository умеет Get/Add и Add только ставит граф на tracking, не делает commit. IQueryable не возвращается за границы data access. Нет ports в CatalogAccess/Publishing/MediaLibrary: тип AssetReference не проверяет существование конкретного Asset.


MVP coordination port принадлежит Application.Abstractions: задаёт границу guarded owner mutation, cancellation и outcome, без PostgreSQL/Npgsql/EF типов в Domain. Точные CLR-имена и signatures остаются Q15. Application открывает owning UoW и требует guard до authoritative reads; EntityFramework.Persistence выполняет standalone pg_advisory_xact_lock на той же physical connection и transaction владельца. Public foreign read adapters используют независимые primary read scopes/connections без ambient UoW enlistment. Host/DI отделяет owning UoW от этих scopes; существующий [EfCoreUnitOfWorkParticipant](../../../../contentmanager.api/src/BuildingBlocks/ContentManager.EntityFramework.UnitOfWork/EfCoreUnitOfWorkParticipant.cs), способный enlist несколько contexts в общую SQL transaction, не выполняет эту целевую границу автоматически. Domain получает уже проверенные предметные facts и не содержит SQL guard type.

### 3.5. Specifications / read contracts


По [S19](../standards/S19-specifications.md) условия оформляются через Ardalis.Specification. Целевые EntitySchemaByKeySpecification, EntitySchemaByIdSpecification, EntitySchemaListSpecification используют атомарные builder conditions по key/id и Search по Name OR Key. Эти имена — проектные, current handler пока использует WhereILikeSearch и локальные Where.


Key existence и DB uniqueness используют одну case-sensitive семантику сравнения с сохранением регистра. Search по Name OR Key использует case-insensitive ILIKE; это не определяет key uniqueness. Count использует те же фильтры без paging. Name ASC default либо allowlisted Name/ModificationTime ASC/DESC завершается Id ASC tie-breaker; SkipCount≥0, MaxResultCount default20/max200 до материализации. Неизвестный sort возвращает400, не становится произвольным SQL-полем.


Projection list считает DraftCount и выбирает latest Published metadata без десериализации Content всех versions. Exact query проецирует summaries и читает Version того же root отдельно для ETag. IsStale сравнивает baseline Draft с текущим published pointer по F02/F03; при новой схеме false. IsRetired/IsWithdrawn выводятся из timestamps, а не конкурирующих stored boolean copies.


## 4. Application


### 4.1. Handlers / use cases


CreateSchemaCommandHandler располагается в Application. Он выполняет DF01; List/Get обработчики размещаются в EntityFramework.Persistence и раскрыты в DF02/DF03.


1. Получить validated Request, нормализовать metadata/keys рекурсивно и сериализовать meaningful input тем же receipt JSON profile. В owner transaction взять PostgreSQL transaction-level locks сначала по OperationId, затем по schema Key. Read committed receipt: same normalized input возвращает original DTO + original logical Version, different input →409 `entity_schema.operation_input_conflict`. Replay выполняется до нового duplicate-key precheck; process restart не меняет receipt. Cancellation передавать во все async operations.
2. Создать metadata VO по согласованной normalization policy; проверить ExistsByKeyAsync для глобального SchemaKey. При duplicate вернуть предметный conflict до Add.
3. Создать EntitySchemaId и EntitySchemaVersionId через domain identity factories, один CreatedAt через TimeProvider с microsecond precision PostgreSQL; original response/receipt/exact read используют одну сохранённую точность.
4. Рекурсивно преобразовать новое дерево: каждому field, включая Object children внутри Lists, назначить новый SchemaFieldId. Anonymous list definitions и options не получают лишних identities.
5. Создать valid SchemaVersionContent, затем EntitySchema.CreateMetadata и EntitySchemaVersion.CreateInitialDraft с null baseline/number. Не вызывать Publish, hash builder или чужие repositories.
6. Add обоих roots и SchemaCreationReceipt в один context/UoW. Repository flush SaveChanges остаётся внутри transaction и классифицирует `ix_entity_schemas_key` violation как `entity_schema.duplicate_key`; внешний CompleteAsync выполняет commit. Failure помечает scope rollback-only; промежуточного успешного результата не существует.
7. Сформировать EntitySchemaDetailsDto с initial Draft summary из созданных объектов. Prepared result выходит в HTTP только после successful CompleteAsync внешнего command UoW; отдельный GET после POST не нужен для получения identity.
8. Receipt хранит OperationId, normalized InputJson, original Details ResultJson, original LogicalVersion и CreatedAt. Replay возвращает именно первоначальный DTO/ETag даже после дальнейшего изменения resource; current state читается отдельно exact GET. Потеря response не отменяет создание и не разрешает новый Create с другим ID.


Предварительная uniqueness-проверка улучшает диагностику, но не защищает от параллельного POST; окончательный арбитр — unique index на commit. Repository.Add не является моментом фиксации. Текущий [UnitOfWorkBehavior](../../../../contentmanager.api/src/BuildingBlocks/ContentManager.Cqrs.UnitOfWork/Behaviors/UnitOfWorkBehavior.cs) вызывает CompleteAsync только для успешной команды и владельца scope; target сохраняет этот порядок с одним BC.


### 4.2. Validators


Текущий [CreateSchemaCommandValidator](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Application/EntitySchemas/Validators/CreateSchemaCommandValidator.cs) через FluentValidation проверяет OperationId и recursive Request/Fields/Definition/Options, включая whole-tree keys, shape и metadata. По [S18](../standards/S18-fluent-validation.md) целевое переиспользование выделяет атомарные смысловые rules и композицию; отдельные классы Request/Draft/Field validators не объявляются уже существующими.


Атомарные компоненты F01: проверки SchemaName, SchemaKey, SchemaDescription; DraftTitle/ChangeComment; FieldKey/Name/HelpText; OptionKey/Name; known FieldDataType/OrderMode; согласованность Required/Deprecated. Параметры берутся из Domain constraints, а не разных literals в create/update. Option validator переиспользуется Enum/FlagEnum.

Required/length validators проверяют нормализованное значение по §2.3, не изменяя DTO. Domain factories/mapper передают в сохранённое состояние trimmed metadata и null для optional blank; trim выполняется до проверки длины. Проверки охватывают все named fields и Enum/FlagEnum options в recursive Object/List definitions. Empty/whitespace required metadata и over-limit значения отклоняют весь Create до Add; blank optional не является ошибкой.


Root и обязательный вложенный объект проверяются отдельно от SetValidator. Collection nonempty не заменяет проверку элементов; null element запрещён. Type-specific validator отклоняет settings другого варианта, неизвестный type и неполные Object/List/Enum definitions. Whole-tree key uniqueness проверяет отдельное общее правило после recursive shape validation, чтобы неверный null subtree не приводил к исключению.


Ошибки имеют общий stable code/message/address. F01 new fields без server FieldId адресуются request location Request.InitialDraft.Fields[index].Definition..., включая nested Fields/Item/Options. Existing named field/ItemKey addressing относится к будущим F02/F04, не создаёт fake ID в Create. Validator не меняет DTO, не генерирует ID, не пишет в БД. Async key precheck выполняется handler/port; unique race независимо контролирует DB. Domain guards повторно защищают инварианты при обходе HTTP.


В MVP CreateSchemaValueDefinitionDtoValidator композирует SchemaValueConstraintsDtoValidator с typed StringLength/TextLength/NumericBounds/PositiveMultiple/TemporalBounds/ListCount rules и ListComparerDefinitionValidator. После shape validation DefinitionConstraintsPolicy проверяет path/required/type consistency. Invalid/отложенные variants reject, не silent ignore. Error address по Q15, новые wire codes не назначаются. Instance values здесь не валидируются.

TemporalDefinitionValidator проверяет known semantics/profile и type-compatible bounds; Offset values exact seven-digit wire, Z/explicit known offset, без leap/-00:00/missing offset. Domain guards защищают typed range/semantics, lexical checks выполняет shared TemporalCodec F04, не locale parser. Новый field default factory new MVP Offset известен UI до отправки; validator не silent-convert existing Naive. Неприменимый/unknown semantic variant отклоняет whole Create/Save.

Post-Demo recursive validation/mapping delivery policy: отсутствующий режим при создании поддерживаемого нового definition получает effective `OnDemand`; известный явный режим сохраняется. Неизвестный enum и delivery setting у любого другого FieldDataType отклоняют whole Create/Save, без silent ignore/default. Server factories создают FieldId, UI его не присылает. Application.Abstractions должен отдельно согласовать create/update/read carrier, nullable/presence и serialization в [Q37](../../frontend/questions/Q37-launcher-asset-delivery.md); conceptual `assetDefinition` из источника не является утверждённым wire. Domain effective mode всегда определён.

### 4.3. Mappers


CreateSchemaFieldRequest → SchemaField заменяет текущий SchemaFieldDomainMapper под новый definition contract. Mapper назначает server IDs один раз, явно выбирает известный variant, рекурсивно создаёт дочерние nodes, переносит metadata без скрытой смены Key и не использует scalar fallback для неизвестного enum.


Domain → EntitySchemaDetailsDto отображает logical metadata и summary первого Draft; Type/IDs полного дерева получает mapper exact version F02. EF projection → read DTO выполняется в Persistence, не требует Domain reconstruction всего aggregate. Internal Version передаётся в metadata response path и никогда не попадает в public DTO.


MVP mapper переносит typed bounds без округления/trim/coercion; List constraints отдельно от Item, paths после server ID assignment. Read/copy сохраняет exact definitions, current defaults не подставляются. Domain не зависит от DTO/FV, reusable validation чистая.

### 4.4. Services / algorithms и регистрация


Отдельные hashing/composition/integration services F01 не нужны. Recursive mapping и validation остаются небольшими чистыми компонентами. Для immutable content converter использует общий serializer profile; Draft serialization не объявляется canonical published serialization F03.


| Сборка / точка композиции | Регистрация и ответственность |
| --- | --- |
| Domain | Factories/VO/constraints без зависимости на FluentValidation, EF или HTTP |
| Application.Abstractions | Commands, queries, DTO; не регистрирует реализации портов |
| Application | Command handler и validators; target Create handler попадает в assembly scan, validators включают internal types |
| EntityFramework | ContentModelingDbContext, converters/configurations; только свой context и migrations |
| EntityFramework.Persistence | Command repositories, query handlers, projections/specifications; target repositories scoped и используют тот же context/UoW |
| Api | EntitySchemaController, binding, cancellation, CQRS buses, общий result/error mapper, ETag headers и OpenAPI |
| Host / App composition | Подключить Api application part, Application и Persistence handler assemblies, validators, scoped repositories, local UoW и TimeProvider.System; Demo не вводит auth/шину как зависимости |


Текущие точки подключения: [ContentManagerApplicationModule](../../../../contentmanager.api/src/App/ContentManager.Application/ContentManagerApplicationModule.cs) (AddValidatorsFromAssemblies, AddLeanSoftCqrs, logging/UoW/result-validation behaviors, TimeProvider); [ContentModelingEntityFrameworkModule](../../../../contentmanager.api/src/BoundedContexts/ContentModeling/ContentManager.ContentModeling.EntityFramework/ContentModelingEntityFrameworkModule.cs) (PostgreSQL context и repository); [ContentManagerApiModule](../../../../contentmanager.api/src/App/ContentManager.Api/ContentManagerApiModule.cs) (controllers application part).


SchemaCreationRepository физически в EntityFramework.Persistence; Host/App composition регистрирует scoped IEntitySchemaCreationRepository. Carrier VersionedResource<T> проходит CQRS result pipeline, controller публикует Resource/ETag; DbContext из controller не читается. Legacy repositories не являются отдельным доказательством F02/F03 target readiness.


MVP composition подключает DefinitionConstraintsPolicy/shared pure constraint primitives. FV validators в существующем assembly scan; reusable components stateless. Domain без HTTP/EF/FV, Abstractions без registration implementations. F04 использует definitions для values; web UI не ссылается на Domain/EF.

Domain/FV/DTO mapping DateTimeValueDefinition переносит exact TemporalSemantics и typed bounds. Persistence хранит authored bound offsets/100ns в JSONB snapshot; semantic hashing нового profile нормализует Offset bounds UTC, equivalent bound instants одинаковы; canonical profile /1 не принимает Offset. Shared stateless TemporalCodec F04 применяется parsing/mapping и conformance, без зависимостей на browser/host timezone/DbContext. Audit/lifecycle timestamps не migrated dynamic fields.

## 5. Api


### 5.1. Endpoints и транспортные потоки


Target routes сохраняют прежнюю целевую спецификацию F01. Таблица соответствует [S17](../standards/S17-data-flows.md); controller — transport adapter по [S16](../standards/S16-rest.md).


| Поток | Транспорт | Метод / канал | Endpoint / destination | Доступ | Вход | Application action | Выход | Успех | Ошибки |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| DF01. Создать схему и initial Draft | HTTP | POST | `/api/v1/entity-schemas` | Demo без auth; MVP Specialist/F16 | Body CreateEntitySchemaRequest с OperationId; без If-Match | CreateSchemaCommand | ApiResponse<EntitySchemaDetailsDto> + own logical ETag/no-store | 200 после commit либо original replay | 400 invalid request/definition; 409 duplicate key либо operation input conflict; 500 unexpected failure |
| DF02. Список схем | HTTP | GET | `/api/v1/entity-schemas` | Тот же этапный доступ | Query search/page/sort по 3.2/3.5 | ListSchemasQuery | ApiResponse<OffsetPagedResultDto<EntitySchemaListItemDto>> | 200, в том числе пустой список | 400 invalid query; 500 unexpected failure |
| DF03. Exact logical schema | HTTP | GET | `/api/v1/entity-schemas/{schemaId:guid}` | Тот же этапный доступ | Route schemaId: Guid | GetSchemaDetailsQuery | ApiResponse<EntitySchemaDetailsDto> + own ETag, no-store | 200 | 400 invalid bound input; 404 schema отсутствует; 500 unexpected failure |
| DF04. Exact initial Draft readonly | HTTP | GET | `/api/v1/entity-schemas/{schemaId:guid}/versions/{schemaVersionId:guid}` | Тот же этапный доступ | Exact schema/version IDs | GetEntitySchemaVersionQuery | ApiResponse<EntitySchemaVersionDto>, recursive Content + own Draft ETag, no-store | 200 | 400 invalid input; 404 missing/wrong-owner version; 500 unexpected failure |


POST возвращает 200 ApiResponse<EntitySchemaDetailsDto> и logical schema ETag после owning commit либо confirmed replay. Collection GET использует выбранные query properties §3.3; обязательный `/lookup` alias не сохраняется. F01 exact Draft read даёт recursive Content snapshot/server IDs для readonly workspace; legacy F02/F03 mutations остаются следующими фичами.


### 5.2. ETag / If-Match


POST не требует If-Match: объект ещё не существует. Successful create и exact root GET возвращают strong numeric ETag logical schema, например `"1"`, и Cache-Control:no-store. Exact Draft GET возвращает собственный strong numeric ETag Draft и no-store. Replay использует original logical Version из receipt; этот header не заявляет текущее состояние изменённого resource. Token logical schema не используется для Draft content. Числовой technical token не входит ни в Details, ни в exact version DTO.


List не выдаёт per-row Version/ETag. Дальнейшая mutation existing resource требует If-Match, API преобразует его в expected int Version внутри command: missing → 428, stale → 412, business conflict → 409. Такие mutation не входят в F01; их проверка относится к F02/F03. Применимый F01 acceptance проверяет source ETag, скрытый Version и отсутствие требования If-Match при POST.


### 5.3. OpenAPI и ошибки


Controller использует текущие versioning/group constants и ToActionResult profile. Request/result models, обязательность и nullable fields, recursive definition variants, 200/400/404/409/500 responses и ETag response header описываются OpenAPI. Creation не принимает field IDs и не возвращает Revision/Version в DTO. Generated clients не редактируются вручную.


400 request validation возвращает ValidationProblemDetails с request field paths; create paths используют Request.InitialDraft.Fields[index].Definition... без fake FieldId. Duplicate schema key →409 `entity_schema.duplicate_key`, field Key; same OperationId/different normalized input →409 `entity_schema.operation_input_conflict`, field OperationId. Exact missing schema →404 `entity_schema.not_found`. Остальные ошибки используют общий ProblemDetails/traceId profile. ProducesResponseType соответствует реальному response, а не подтверждает выполнение HTTP tests.


Общий Demo transport profile [Q15](../questions/Q15-api-and-source-code.md#согласованный-demo-профиль-authoring-api) задаёт error structure; F01 Create использует request paths без fake IDs, тогда как saved named-value addressing FieldId/ItemKey относится к другим owner flows. F01 paging/sort names/defaults/limits выбраны §3.3. NSwag generation и recursive round-trip обязательны до интеграции UI; все15 types и nested Lists проверяются буквально, без сокращённого набора variants. Binary success других фич остаётся binary.

ETag exact authoring read относится только к сохранённому mutable root. Derived diagnostics имеют отдельную identity basis, включающую exact schema/content/baseline context: изменение diagnostics не имитирует изменение own ETag. Accept и delete consent связываются с показанным context/impact; одного root ETag для этих external facts недостаточно.

## 6. EntityFramework


### 6.1. DbContext ownership


ContentModelingDbContext владеет DbSet<EntitySchema>, DbSet<EntitySchemaVersion> и DbSet<SchemaCreationReceipt> в PostgreSQL schema `content_modeling`. Не подключает tables или navigations других BC. Content JSONB содержит field IDs в каждом snapshot: одинаковый stable SchemaFieldId может существовать в нескольких versions без global field PK conflict.


### 6.2. Configurations


| Entity / configuration | Table | Key/index | Concurrency | Mapping |
| --- | --- | --- | --- | --- |
| EntitySchema / EntitySchemaConfiguration | `content_modeling.entity_schemas` | PK id; global UNIQUE(key) | `version integer NOT NULL`, concurrency token | Guid IDs ValueGeneratedNever; string VO conversions; nullable published pointer/number; metadata columns по Domain constraints |
| EntitySchemaVersion / EntitySchemaVersionConfiguration | `content_modeling.entity_schema_versions` | PK id; FK schema_id; indexes из 6.3 | Собственный `version integer NOT NULL`, concurrency token | Separate root; Content через JSON converter + value comparer; nullable published/baseline/lifecycle metadata |
| SchemaCreationReceipt / SchemaCreationReceiptConfiguration | `content_modeling.schema_creation_receipts` | PK operation_id, Guid ValueGeneratedNever | Immutable receipt; no public edit token | InputJson/ResultJson text preserve serialized normalized input/original DTO; LogicalVersion integer and CreatedAt; один commit с обоими roots |


Target columns:


```text
entity_schemas
  id uuid NOT NULL PK
  name text/varchar NOT NULL
  key text/varchar NOT NULL
  description text/varchar NULL
  lifecycle text NOT NULL
  latest_published_version_id uuid NULL
  latest_published_version_number integer NULL
  version integer NOT NULL
  creation_time timestamptz NOT NULL (audit convention)
  modification_time timestamptz NULL (audit convention)


entity_schema_versions
  id uuid NOT NULL PK
  schema_id uuid NOT NULL
  status text NOT NULL
  version_number integer NULL
  title text/varchar NOT NULL
  change_comment text/varchar NULL
  base_published_version_id uuid NULL
  content jsonb NOT NULL
  created_at timestamptz NOT NULL
  published_schema_name text/varchar NULL
  published_schema_key text/varchar NULL
  canonical_content_json text NULL
  content_hash text/varchar NULL
  published_at timestamptz NULL
  retired_at timestamptz NULL
  withdrawn_at timestamptz NULL
  version integer NOT NULL
```


Schema key хранит нормализованное значение длиной 1–50 с сохранённым регистром; text/varchar mappings применяют metadata constraints §2.3: Name/Title 255, optional Description/ChangeComment 1024 и null для отсутствия. Recursive field/option metadata в JSONB сохраняет те же trimmed строки и optional null; JSON storage не отменяет Domain/FV limits. JSON converter serializes весь target definition union и восстанавливает IDs/metadata без генерации новых ID при чтении. Value comparer и immutable-by-replacement assignment обеспечивают обнаружение замены Draft content. JSONB serialization не заменяет canonical bytes для F03.


MVP content converter сохраняет known constraints variants, typed nullable bounds и comparer FieldId paths в полном JSONB snapshot. Отдельные tables/repositories rules не нужны; converter не валидирует/сортирует и не переинтерпретирует old Published с current defaults. Decimal/temporal round-trip точный; canonical bytes/profile F03 независимы от JSONB.

### 6.3. Constraints / indexes


- UNIQUE(entity_schemas.key) с case-sensitive сравнением; precheck и index различают `fieldKey`/`fieldkey`, не выполняют case folding.
- FK entity_schema_versions.schema_id → entity_schemas.id, только внутри ContentModeling. Удаление root регулирует F02; случайный cascade не заменяет delete-impact policy.
- Nullable baseline FK → entity_schema_versions.id. Same-schema принадлежность baseline проверяется Domain/Application F02/F03; простой single-column FK её не доказывает.
- UNIQUE(schema_id, version_number) WHERE version_number IS NOT NULL; Draft number = null. Nullable latest Published number/pointer относятся к одной publication state.
- INDEX(schema_id, status), INDEX(schema_id, created_at); current ux_schema_one_draft удаляется. Несколько Draft не получают фиктивные номера.
- State coherence Draft/null number и Published/positive number защищается Domain и необходимыми SQL CHECK. Cross-row проверка latest pointer принадлежит F03, не скрывается в обычном FK.


Вложенные keys/IDs JSONB проверяются доменом, а не отдельным relational unique index fields. Version token обновляется при успешной state-changing mutation владельца по Q20; механизм атомарного increment проверяется вместе с F02 и не оставляется в caller как произвольный setter.


### 6.4. Загрузка aggregate и published pointer


EntitySchemaVersion читается/сохраняется отдельно. EntitySchema не требует Include всего Content версий для metadata или published pointer. Nullable latest_published_version_id не может стать циклическим обязательным FK, мешающим initial create: в F01 он null. Его owner-consistency и update проверяются в F03 вместе с Published snapshot и nullable number.


## 7. EntityFramework.Persistence


### 7.1. Repositories / query adapters


Целевые EntitySchemaRepository и EntitySchemaVersionRepository реализуют ports 3.4 в EntityFramework.Persistence. Add ставит новый root на tracking; оба используют один scoped ContentModelingDbContext. ExistsByKey применяет specification; никакого SaveChanges/commit отдельно на каждый root.


ListSchemasQueryHandler выполняет SQL selection/count/projection/order/page и mapping ListItem; GetSchemaDetailsQueryHandler проецирует logical schema, ≤20 summaries/VersionsTotalCount и internal concurrency metadata. Tracking для чтений не нужен. Ни list, ни exact logical read не материализуют полный Content всех versions.


### 7.2. Transactions

Для MVP владелец этих commits — ContentModeling. По [утверждённому MVP consistency guard](../questions/Q20-persistence-and-migration.md#mvp-протокол-consistency-guard) все CM replicas используют одну PostgreSQL database и один стабильный глобальный exclusive transaction-level advisory key. Каждый affected owner scope явно начинает ReadCommitted transaction, затем отдельным statement вызывает pg_advisory_xact_lock на **той же physical connection и transaction**, до любых authoritative domain reads и row locks. После успешного acquire заново загружаются own roots/tokens и current foreign facts через public ports из независимых primary read scopes/connections. Предварительные query/preview/cache/replica facts не разрешают commit; foreign read не enlist-ится в ambient owner UoW, не разделяет SQL transaction и не открывает foreign EF из caller.

Guard удерживается до owner commit/rollback: изменяются только roots своего BC и атомарные own outbox/audit intent/inbox markers, где они нужны. Все invalidating writers, включая creation/removal/reference Copy/orphan paths, Publish/lifecycle/hard delete, setup/reconciliation, source durable acceptance/watermark/pending/apply/retry и F15 open/supersede/repair/resolve, участвуют в этом же ключе. Own Version/If-Match сохраняется: stale own token →412, изменившаяся foreign/preview basis →409 по Q15. Create/Copy из неизменённого source не увеличивает source Version; два serially guarded допустимых создания могут оба завершиться успешно.

Внешний I/O (broker, VR School, MinIO), ожидание пользователя и долгие streams выполняются вне gate. CPU preparation допустима заранее, но exact basis повторно подтверждается внутри gate; иной basis требует отказа или новой подготовки вне gate. Bounded guard wait/lock timeout конфигурируются, поддерживают cancellation и являются availability/technical failure, не domain409; точные значения и transport mapping остаются Q15/Q20. Connection loss прерывает owner transaction: reconnect-and-continue запрещён. Потеря commit ACK даёт unknown outcome; выяснение exact state/исходного operation ID по Q15 предшествует повтору, blind command retry запрещён.


DF01 фиксирует schema и initial Draft в одной PostgreSQL transaction ContentModeling. При отказе validation, Domain, второго Add или SaveChanges/commit не сохраняется ни одного объекта. Это один BC, одна физическая transaction, без S3/брокера/HTTP side effects.


Current [EfCoreUnitOfWorkParticipant](../../../../contentmanager.api/src/BuildingBlocks/ContentManager.EntityFramework.UnitOfWork/EfCoreUnitOfWorkParticipant.cs) сохраняет contexts и commit-ит transaction; технически он умеет разделять transaction между contexts. Для F01 enlist-ится только ContentModelingDbContext и own transactional intent participants: cross-BC atomicity запрещена Q20. MVP guard adapter использует эту же connection/transaction; Host не даёт чужим contexts enlist-иться в owner UoW.


При concurrent duplicate key только один commit успешен, второй откатывается целиком и возвращает 409 schema-key conflict. Current participant преобразует все SQLSTATE 23505 в общий concurrency conflict. Target должен различать именно constraint schema key и stale Version: unique-key failure не становится 412 и не возвращает вводящий в заблуждение совет перечитать изменённый объект.


Отмена до commit не приводит к success; ошибка commit требует rollback scope. После подтверждённого commit client cancellation/сбой response не отменяет запись. Body OperationId и SchemaCreationReceipt сохраняют normalized input/original Details DTO/LogicalVersion; повтор возвращает original DTO+ETag, включая restart. SchemaKey сам по себе не является identity попытки.


Для F01 Create клиентский OperationId и durable receipt описаны §2.1/§4.1; normalized same input возвращает original result, иной input409. Root inserts и receipt фиксируются атомарно. Общая политика clean-create/copy/upload [Q15](../questions/Q15-api-and-source-code.md#согласованный-demo-профиль-authoring-api) не утверждает реализацию других фич по наличию F01 receipt; upload durable stages отдельно F05.

### 7.3. Migrations


Проект не запущен; Q20 разрешает destructive перестройку без сохранения legacy production compatibility. Миграции здесь описаны как необходимые изменения, а не уже выполненные команды:


1. Отделить root version state от загружаемого graph EntitySchema.Versions; сохранить schema_id relation.
2. Заменить Revision на внутренний int Version logical schema и добавить Version mutable version owner.
3. Заменить required version_major/version_minor nullable version_number; удалить ux_schema_one_draft.
4. Добавить Title/ChangeComment/baseline/created and lifecycle metadata, latest Published pointer/number.
5. Обновить JSON serialization для recursive definitions, Deprecated/HelpText и string-key options; удалить InheritanceStrategy/ColorFormat/numeric options.
6. Уточнить global key comparison/index, number unique/check constraints, serializer migration и обновить read projections.


History/schema пересобирается только по принятой repository practice. Миграция не запускается при написании аналитики и не выдаётся за проверенную работоспособность target mapping.


## 8. External adapters


S3/MinIO, RabbitMQ/MassTransit и HTTP integrations не применяются. AssetReference — definition с обязательным AssetKind; конкретный AssetId/value и соответствие kind проверяются в F04 через F05 boundary. F01 не синхронизирует VR School и не получает Product/Variant/License facts.


Используется только clock adapter TimeProvider для server timestamp. Публикационный hashing/canonicalization — F03/Q19, не побочный эффект создания Draft.


## 9. Data flows

MVP порядок для DF01: input/CPU preparation вне gate → явная owner ReadCommitted transaction → standalone exclusive advisory acquire → fresh own roots и foreign primary-port facts → проверки own token/basis/lifecycle/dependencies → Domain action → только own writes/atomic intents → commit и release. Это обязательный префикс каждого указанного mutation/retry stage; строки «load/guard» ниже означают загрузку **после** acquire. Read-only query/impact/preview остаются observations; при необходимости короткий source capture имеет отдельный guarded scope, но долгий stream и пользовательский review не удерживают guard. Demo использует свой согласованный контур без новых обязательных MVP dependencies.


Поля DTO определены в 3.3; внешние contracts — в 5.1. Ниже конкретная внутренняя обработка по S17.


### DF01. Создание схемы и initial Draft


| Шаг | Исполнитель | Вход / чтение | Преобразование и проверка | Запись / выход |
| --- | --- | --- | --- | --- |
| 1 | Api / validation pipeline | Request body и cancellation | Binding, root/recursive shape/known types, paths ошибок | CreateSchemaCommand либо 400, без записей |
| 2 | Application + creation repository | Demo owner transaction, locks OperationId → Key, normalized input и committed receipt; MVP global guard дополнительно по Q20 | Same-ID/input original DTO+ETag replay; different input conflict до нового duplicate-key precheck; для новой операции global key existence | `operation_input_conflict`/`duplicate_key` →409 либо продолжение |
| 3 | Application mapper / Domain | Request tree, identity factories, TimeProvider | Generate root/field IDs, recursive definitions, whole-tree guards | Два valid новых roots; initial published/baseline/number отсутствуют |
| 4 | Repositories | Schema и version | Add в один context; FK IDs согласованы | Pending root inserts + durable operation result; это ещё не externally visible success |
| 5 | Command UoW / PostgreSQL | Pending inserts | SaveChanges, unique constraint, commit единой transaction | Оба roots + original operation result доступны вместе либо полный rollback/409/error |
| 6 | Api result adapter | Prepared DTO и metadata созданного logical root после commit | ApiResponse и logical schema ETag | 200, schema ID и initial Draft ID; без publication side effects |


Клиент переходом открывает readonly exact Draft и получает его Content/server IDs/own ETag. Потерянный response после шага 5 означает unknown outcome; тот же OperationId/normalized input возвращает original result без создания новых roots.


### DF02. Список схем


1. Api binding и query validator проверяют параметры Search/page/sort; DTO фильтров не превращается в command.
2. Persistence получает свой context, собирает specification. Search ограничивает Name OR Key; ordinary list scope и filters используют Q13/F02 без скрытого обращения к другим BC.
3. SQL count считает выбранные schemas без paging. Projection включает Id/Name/Key, DraftCount, latest Published number/id/withdrawal и ModificationTime, без загрузки Content JSONB.
4. Sort с deterministic Id tie-breaker и offset/page выполняются на сервере до материализации. Invalid query возвращает 400; отсутствие записей — 200 с пустыми Items и TotalCount.
5. Возвращается paged result. Count и page при конкурентных изменениях не обещают snapshot isolation: в текущем read-profile это отдельные запросы, и total может измениться к следующему чтению. Новая transaction ради обычного списка не вводится.


### DF03. Exact logical schema


1. Api передаёт route schemaId и cancellation в query bus.
2. Persistence отбирает logical schema по Id через specification, проецирует metadata и читает internal Version того же root, не загружая mutable graph для редактирования.
3. Если schema отсутствует, возвращается entity_schema.not_found/404. VersionsTotalCount считает все versions owner; Versions выбирает максимум20 по CreatedAt DESC + Id DESC без full content. Summary flags рассчитываются по owner pointer/timestamps; bounded summaries не обещают всю историю.
4. Api возвращает ApiResponse<EntitySchemaDetailsDto> и ETag logical root из read metadata. Технический token не входит в DTO; version-summary Id не является version ETag.


Read-model и token должны относиться к одной прочитанной logical schema. При дополнительных SQL для summaries согласованность большого списка versions определяется read-контрактом Q15/F02; один aggregate token не объявляется глобальной ревизией всех независимых child roots.


### DF04. Exact initial Draft readonly

1. Api передаёт exact schemaId/versionId в GetEntitySchemaVersionQuery; query handler проверяет принадлежность версии указанной схеме, без latest fallback.
2. Persistence читает saved version Content и own Version. SchemaCreationMapper.Snapshot возвращает recursive named fields с server Id, typed definitions и anonymous List.Item. Для нового F01 Draft snapshot обязателен; legacy historical content может не иметь этого representation и не подменяется пустым деревом.
3. Api возвращает EntitySchemaVersionDto с Title/ChangeComment, nullable published number, Content и own ETag/no-store. Numeric technical Version не сериализуется. Workspace читает snapshot readonly; Save/Publish F02/F03 этим потоком не реализуются.

## 10. Ошибки и диагностика

MVP guarded failure различает confirmed business refusal и невозможность проверки: unavailable/incomplete public facts не означают empty/valid facts. Guard wait cancellation/timeout и connection loss — technical/availability outcome без domain Conflict; точные wire codes остаются Q15. После connection loss scope завершается rollback/disposal без продолжения на новой connection. Unknown commit outcome выясняется по exact state/operation ID; prepared tracker не подтверждает ни success, ни rollback. Own stale ETag сохраняет412, foreign basis change —409. Диагностика фиксирует wait/hold duration, owner/operation, acquire/commit/rollback/unknown phase без full payload и секретов.


| Причина | Наблюдаемый результат | Записи / восстановление |
| --- | --- | --- |
| Неполная metadata/definition, неизвестный type, повторный field key | 400 + validation field paths либо согласованный domain error | Нет schema/initial Draft; UI сохраняет ввод |
| Schema key занят до Add или concurrent commit | 409 entity_schema.duplicate_key, поле Key | Второй create не оставляет partial graph |
| OperationId уже имеет другой normalized input | 409 entity_schema.operation_input_conflict, поле OperationId | Original receipt сохранён; новых roots нет; не silent reuse |
| Exact logical schema не найдена | 404 | Чтение без mutation |
| Unexpected storage/serialization failure | 500 + traceId; без SQL/секретов в ответе | Scope rollback при failure до commit; лог фиксирует стадию |
| Отмена/обрыв ответа | Общий cancellation profile; успех не обещается при потерянном response | После commit созданное состояние сохраняется; blind POST retry не безопасная гарантия |


Duplicate schema key и HTTP optimistic conflict — разные ошибки. F01 codes/path semantics выше выбраны; общий concurrency.conflict не заменяет entity_schema.duplicate_key или entity_schema.operation_input_conflict. Future F02/F03 mutation codes не выводятся из этого Create contract.


Диагностика пишет traceId/UoW correlation, operation, SchemaId/SchemaVersionId после назначения, field count, duration, stage и machine result. Полное дерево, HelpText/ChangeComment, пользовательский текст и credentials не логируются. Ожидаемый validation/conflict не маскируется unexpected exception. Success в журнале операции относится к successful commit, не к одному Add.


## 11. Тестирование

Post-Demo delivery policy acceptance: default/explicit modes, nested Lists/Objects, reject setting у String/unknown enum, real PostgreSQL save/read и generated-client round-trip. Старые `/1` golden bytes неизменны, исторический effective default не перезаписывает Published. Это future checks после согласования Q37 carriers/profile, не результат существующего Demo suite.


Это стратегия target-проверок, не отчёт о выполненных тестах. Применяются [S04](../standards/S04-testing.md), [Architecture testing (migrated)](../../../catalog/docs/standards/dotnet/testing.md), [S10](../../../catalog/docs/standards/dotnet/testing.md#unit-tests), [S11](../../../catalog/docs/standards/dotnet/testing.md#integration-tests), [S12](../../../catalog/docs/standards/dotnet/testing.md#functional-tests), [S14](../../../catalog/docs/standards/dotnet/testing.md#test-environment-ownership-and-bounded-parallelism).


| Уровень | SUT / реальные зависимости / doubles | Собственные данные, изоляция и cleanup |
| --- | --- | --- |
| Unit | Domain factories, recursive mapper, validators, create handler; допустимы repository test doubles и controlled TimeProvider; без EF in-memory «вместо PostgreSQL» | Каждый тест создаёт request/tree/IDs; double отражает port contract, не доказывает transaction/index |
| Integration | Repositories, specifications, DbContext mappings, serializer и local UoW с реальным PostgreSQL Testcontainers | Disposable database на test; migrations/seed собственного теста; независимые scopes для races; cleanup данных/ресурсов fixture |
| Functional | Один CM API через WebApplicationFactory, настоящие handlers/validators/PostgreSQL | Собственная БД и HTTP client, Demo без fake JWT; cleanup fixture. HTTP outcomes и headers, storage state при необходимости проверяет atomicity |
| Architecture | Зависимости сборок, размещение handlers/repositories и границы BC | Без runtime infrastructure; учитываются реальные project references |


| ID | Поток / уровень | Сценарий / исходные данные | Ожидаемый результат |
| --- | --- | --- | --- |
| F01-U01 | DF01 / Unit | Каждый из 15 типов, Enum/Flag options, Object/List<Object>/List<List<T>> | Полная valid definition, server field IDs, metadata; list item не получает лишний FieldId |
| F01-U02 | DF01 / Unit | Key длиной 1/50/51, leading digit/uppercase, underscore/hyphen/Unicode; внешние whitespace; root/child duplicate key в разных ветках и `fieldKey`/`fieldkey`; null/unknown definitions, empty Object/Enum, Required+Deprecated | VO/FV согласованы: Trim без Camelize, valid casing сохранён, 1/50 допустимы, invalid format/51, точный duplicate и invalid definitions отклонены с правильным path; разные casing допустимы; нет repository Add при ошибке |
| F01-U03 | DF01 / Unit | Valid request и controlled time | Schema Active, первый Draft, null number/base/hash/published state, правильный CreatedAt; option identity string Key |
| F01-U05 | DF01 / Unit | Name/Title/FieldName/optionName: blank и 1/255/256 после Trim; Description/ChangeComment/HelpText: null/empty/whitespace и 1024/1025; recursive options/fields | Required blank/256 и optional1025 отклонены до Add; 1/255/1024 допустимы; внешние whitespace удалены, внутренние пробелы/регистр сохранены, optional blank → null |
| F01-U04 | DF01 / Unit | Key precheck duplicate; failure schema/version port; cancellation | Conflict/failed result и исходный cancellation не теряются; нет ложного success; rollback отдельно проверяется Integration |
| F01-I01 | DF01 / Integration | Valid schema + Draft; injected failure после первого pending insert, до commit | Обе строки после success; ни одной после rollback; Add сам не commit |
| F01-I02 | DF01 / Integration | Два scopes с одинаковым SchemaKey и барьером перед commit; отдельно schema keys, отличающиеся регистром | Точный duplicate: один success/один conflict, одна schema и её initial Draft; разные casing сохраняются отдельно, precheck и DB index согласованы |
| F01-I03 | DF01/DF03 / Integration | Все definitions, metadata и stable IDs JSONB round-trip | ID/options/recursive item не теряются; одинаковый FieldId хранится в snapshots разных versions |
| F01-I05 | DF01/DF03 / Integration | Trimmed metadata на границах и optional blank в schema/Draft/recursive fields | PostgreSQL/read DTO сохраняют нормализованные строки и null, без пустых строк и служебных подстановок |
| F01-I04 | DF02 / Integration | Разные Names/Keys, несколько Draft, опубликованные/withdrawn versions | SQL filters/count/projection/order/page корректны; нет one-Draft constraint и legacy major/minor зависимости |
| F01-FN01 | DF01 / Functional | POST без field IDs/If-Match, затем exact schema/version GET | 200, response содержит SchemaId/initial Draft Id; logical ETag получен; Draft ETag берётся из F02; tree IDs созданы сервером |
| F01-FN02 | DF01 / Functional | Invalid recursive request; duplicate schema Key, включая commit race | 400 с paths / 409 duplicate; нет partial persistence state |
| F01-FN03 | DF02/DF03 / Functional | Empty/search/page list; existing/missing exact schema | 200 paged result/404; exact GET с ETag; DTO/OpenAPI без Revision/Version; list без per-row token |
| F01-FN04 | DF01 / Functional + OpenAPI | Schema request/result shape и generated client deserialization | Create DTO без Id/IsNew/VersionNumber; recursive union и nullable fields отражены корректно; headers доступны client transport |
| F01-FN05 | DF01 / Functional | Metadata boundaries/whitespace через POST, включая nested field и option Name | 400 с metadata paths при invalid input и без partial state; valid result/exact read возвращает trimmed metadata и optional null |
| F01-FN06 | DF01 / Functional + PostgreSQL | Same OperationId/normalized input, loss/restart, later resource change; different input и two concurrent scopes | Original DTO+original ETag replay после restart; roots/receipt только один раз; different input409 operation_input_conflict; original receipt не current-state projection |
| F01-FN07 | DF02/DF03 / Functional | ≥21 version summaries; list default20/max200/sort ties/invalid query | Details Versions≤20 + correct VersionsTotalCount; list без trees/tokens, stable Id tie-breaker, invalid limit/sort400 |
| F01-FN08 | DF04 / Functional | Exact initial Draft, wrong schema/version, recursive List<Object>/List<List<T>> | Own Draft ETag/no-store; server field IDs и Content сохранены; numeric token отсутствует; wrong-owner404, no latest fallback |
| F01-A01 | Все / Architecture | Assembly dependencies и handler/port registration | Commands в Application, queries/repositories в data access, Domain без HTTP/EF/FV; нет чужих contexts/шины |


Применимая проверка concurrency F01 — unique creation race и правильный источник ETag. Обязательные 412/428 и atomic Version increment existing Draft проверяет F02; их нельзя «покрыть» только happy-path POST. Историческая неизменяемость Publication и evolution field identity проверяются F03, F01 не публикует history ради своего теста.


Несколько BC внутри одного API не превращают Functional в E2E. Для F01 нет необходимой межприложенческой цепочки; отдельный distributed E2E только ради инфраструктуры не требуется. UI → CM browser scenario может проверяться при реализации frontend, но не заменяет PostgreSQL/HTTP tests и не был выполнен при подготовке документа.


MVP Integration matrix для DF01 использует real PostgreSQL, отдельные owner connections/UoWs и barriers, включая разные CM replica processes/hosts там, где проверяется общность key. Doubles/in-memory locks не доказывают SQL coordination.

| MVP сценарий | SUT / воздействие | Наблюдаемый результат |
| --- | --- | --- |
| Guard до чтений | Реальный Persistence adapter; writer A держит gate, B начинает owner transaction до acquire; A меняет eligibility/basis и commit, B продолжает | B после acquire читает новый committed state при ReadCommitted; stale предварительные facts не применяются; lock statement использует owner connection/transaction |
| Меж-BC write/read partition | Public read adapter и Host/DI; owner держит guard, foreign primary read отдельным scope | Read не enlist-ится в owner transaction, не читает cache/replica; no foreign writes/EF access; atomic commit только owner + intents |
| Commit/rollback/release | Barriers для invalidating writer и affected operation; injected owner failure/cancellation | Второй writer ждёт release и revalidates; failed owner полностью rollback, нет partial roots/intents; unrelated next scope получает key |
| Wait и connection failure | Configured bounded wait/cancellation, разрыв owner physical connection | Technical/unavailable, не409/empty success; aborted transaction не продолжается через reconnect |
| Unknown commit ACK | Разрыв response/commit acknowledgement и inquiry exact state/original operation ID | Committed result сохраняется, rollback не предполагается; no blind duplicate mutation |
| Duplicate Create | Два independent owner scopes, same schema Key либо same operation ID/input | Один logical schema по unique Key; durable original result replay для same operation; global serialization не создаёт дубликат |

MVP acceptance: contradictory/inapplicable definitions reject без roots; exact numeric/temporal/Unicode round-trip; emoji/combining length; recursive item constraints; Default/comparer consistency; optional/missing/cross-List/unsupported path reject; new comparator references после server IDs; constraints evolution без нового FieldId; old immutable snapshots. SUT: pure Domain/DTO validators, PostgreSQL snapshot atomic round-trip, Api/OpenAPI/NSwag typed recursive fixtures. Golden profiles F03/F21/F22 отдельно; незавершённый wire/profile codec не объявляется проверенным.

Temporal MVP fixtures: known Naive/Offset definition/typed bounds recursive round-trip; new MVP field Offset, existing semantic change rejected/new ID required; equivalent Offset bounds same UTC meaning; ±14:00/local+UTC range; missing offset/leap/-00:00/precision rejection; authoring JSONB preserves offset/100ns. Source /1 fixture unchanged, new profile combines Q35 constraints/Offset; full new-profile bytes отдельно Q19/Q31. SUT Domain/DTO validators+pure codec, PostgreSQL snapshot round-trip/OpenAPI, не report executed application tests.

## 12. Порядок реализации

После Demo: материализовать delivery policy Domain/FV → versioned JSONB converter/EF migration → create/read API/OpenAPI → editor inspector. Converter сохраняет режим по всему recursive definition, historical Published читается с effective `OnDemand` без записи default обратно и без rehash. Exact mapping/codec/new canonical либо metadata integrity profile и semantic identity при смене режима остаются [Q37](../../frontend/questions/Q37-launcher-asset-delivery.md). Наличие этого плана не доказывает реализацию.


1. Проверить реализованный Demo wire/metadata/read-profile §3/§5 и Q15 по реальному API/OpenAPI/NSwag; не вводить постоянную duplicate frontend form model и не переоткрывать выбранные contracts.
2. Перестроить Domain roots/constraints/definitions и server factory semantics; начать с Unit tests initial Draft, recursive types и whole-tree invariants.
3. Обновить EF mapping/serializer/migrations; проверить реальный PostgreSQL round-trip, multiple Draft storage, constraints и rollback.
4. Обновить Application.Abstractions: отдельный create contract/result, list/exact read models, внутренний carrier ETag metadata и ports.
5. Реализовать Application create handler/recursive mapper/validators и scoped repositories; сохранить один command UoW/BC; MVP guard adapter использует owner connection/transaction до reads, Host разделяет owner/read scopes. Проверить key race и constraint-specific error mapping.
6. Реализовать specifications/projections/query handlers в EntityFramework.Persistence, source ETag из того же root read; проверить search/order/count/paging.
7. Обновить Api routes/contracts/errors/headers, Host DI, OpenAPI; выполнить Functional/Architecture проверки перед handoff UI.
8. Перегенерировать NSwag, подключить frontend F01 к новым contracts и target routes; проверить UI acceptance, не редактируя generated models вручную.


Эта последовательность — карта реализации и проверок, не отчёт об их полном успешном выполнении. Source presence §1 отдельно от verification evidence; migration production и browser flow требуют своей проверки. Следующие фичи разбираются отдельно: F02 → F03; F01 readonly workspace не включает их legacy mutation contracts.


## 13. Открытые технические вопросы

Delivery policy post-Demo: [Q37](../../frontend/questions/Q37-launcher-asset-delivery.md) — concrete CLR/EF/DTO carriers, presence/encoding, новые integrity/formal profiles и изменение режима при сохранении FieldId; modes/default/owner уже выбраны.


- [Q15: F01 contract details](../questions/Q15-api-and-source-code.md#f01-детализация-целевого-контракта): Demo request/recursive definitions, paging/summaries, internal VersionedResource/own ETag, OperationId/receipt и F01 conflict codes выбраны. Проверки OpenAPI/NSwag/PostgreSQL/browser и future MVP key profile остаются отдельно; это не новые пользовательские решения для запуска F01.
- [Q34](../../../business/questions/Q34-schema-field-semantics.md)/[Q22](../../../business/questions/Q22-quality-targets.md): numeric operational Text cap после fixture, restricted HTML/sanitization/semantic-empty уже приняты. [Q35](../../../business/questions/Q35-value-constraints.md) согласовал первый MVP набор и не блокирует Demo. Open: new-field comparator reference transport Q15, exact new-profile layouts Q19/Q31, Offset semantics закрыты Q34; whole new temporal/profile codec и OpenAPI/NSwag materialization отдельно Q15/Q19/Q31.


Global key uniqueness, server-generated field IDs, nullable Draft number, несколько Draft, отдельные mutable owners и HTTP ETag/If-Match уже приняты; они не возвращаются в список нерешённых решений.

## 14. Проверенное поведение Demo F01

F01 реализован и проверен в локальном контуре CM API/PostgreSQL/UI. Выполненные проверки относятся к F01 и указанным regressions; они не подтверждают готовность остальных Demo-фич или deployment/production migration.

| Проверка | Выполненный результат и граница |
| --- | --- |
| ContentModeling Unit | 405/405; coverage91.49% lines/87.45% branches. Domain/validation/mapping, recursive definitions и metadata; не доказательство HTTP/БД |
| ContentModeling Integration | 23/23 с PostgreSQL; independent roots/migrations/modern JSON и compatibility. [F01AggregatePersistenceTests](../../../../contentmanager.api/tests/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Tests.Integration/F01AggregatePersistenceTests.cs) |
| Scoped ContentModeling Functional/OpenAPI | 34/34; metadata6 limits, unknown client IDs, own ETags/CORS, duplicate/replay/concurrency и protection legacy Update/Publish. [F01SchemaCreationTests](../../../../contentmanager.api/tests/BoundedContexts/ContentModeling/ContentManager.ContentModeling.Tests.Functional/F01SchemaCreationTests.cs) |
| UI build/test runner | Build и runner прошли; explicit exact ID вне bounded20 summaries сохраняется до ownership check exact GET, positive Int32 ETag parse. [F01Tests](../../../../contentmanager.ui/tests/ContentManager.Ui.Tests/F01Tests.cs) |
| Real browser → CM API | Создана новая схема с required List<AssetReference Audio> и HelpText; открыт именно первый ненумерованный Draft в readonly workspace с сохранённым recursive snapshot |
| Real backend restart | Повтор прежнего OperationId/input после остановки/старта backend вернул exact original response body и ETag `"1"`; process-memory replay не используется |

Новые snapshots защищены от потери metadata/definitions через legacy writes; legacy stored schema mutation compatibility проверена отдельно. Definition-aware Save/Publish и остальные F02/F03 requirements остаются следующими фичами. Повтор этих suites нужен после последующих изменений затронутого contract, а не как требование повторно согласовать инженерные решения F01.
