# F22. Генерация C#-моделей: бэкенд


[Бизнесовая спецификация](../../../business/features/F22-csharp-model-generation.md) · [Frontend](../../frontend/features/F22-csharp-model-generation.md) · [Registry F21](F21-schema-registry.md) · [M07](../modules/M07-schema-tooling.md) · [SDK F19](../../frontend/features/F19-unity-client.md) · [Q31](../questions/Q31-schema-code-generation.md).


## 1. Область реализации


Demo: пользователь выбирает exact Published SchemaVersion IDs в UI, server wrapper получает формальные контракты F21, reusable Schema.CodeGeneration.Core создаёт C# source, wrapper собирает ZIP с `.g.cs` и возвращает binary download. Браузер не выполняет генерацию/ZIP. Набор включает нужные зависимые типы по agreed profile, не весь случайный каталог; несколько версий одной схемы должны использоваться совместно.


MVP: local .NET tool ContentManager.Schema.CodeGeneration.Cli / cm-schema обязателен поверх того же Registry/Core. content-manager.json + content-manager.lock.json закрепляют exact versions/profiles и formal bytes; restore, offline generate, readonly check и safe owned output/recovery определены ниже. UI→ZIP сохраняется; автоматические Editor/source-generator/build integrations не добавлены.


Генерация не изменяет схемы/instance/inventory, не анализирует экземпляры для угадывания структуры и не генерирует runtime UI/game behavior. Нет persistent jobs/history/generated package publish/registry dependency в Demo. F19 runtime использует уже generated типы; F20 отдельная сцена, её successful UI не доказывает CLR/type/serializer compatibility всей матрицы.


Source evidence 09.10.2026: F21 реализован. Neutral Core находится в `contentmanager.api/src/BuildingBlocks/ContentManager.Schema.CodeGeneration.Core`, tooling `net10.0`, Roslyn `4.14.0`/Newtonsoft.Json `13.0.3`; production references на Domain/EF/SDK/ASP.NET/Unity отсутствуют. Server `GenerateCSharpModelsCommandHandler`, `CSharpGenerationOptions`, `DeterministicZipWriter`, request/archive DTO и `CodeGenerationController`/OpenAPI transformer находятся в existing ContentModeling слоях. Platform-neutral SDK model/serialization foundation находится в `contentmanager.sdk.client.dotnet/src/Sdk.Client.DotNet`, `netstandard2.1`. Реальные Core ABI/naming/source bytes и ZIP layout описаны ниже; наличие исходников этих компонентов не доказывает полный UI/HTTP/PostgreSQL/SDK/Unity chain или package publication. MVP CLI/package/install остаются target scope, не результат этого Demo среза.


### 1.1. Зафиксированный generation profile Q31

F22 использует semantic profile `urn:vrschool:content-manager:csharp-generation-profile:1` (`cm-csharp/1`). NuGet/package version generator implementation не является profile version. Incompatible output semantics/guaranteed bytes требуют нового profile. Demo UI profile не выбирает; server uses configured current profile.

Byte-changing bugfix также вводит новый profile; прежний `/1` сохраняет воспроизводимость поддерживаемого exact input. Package SemVer может меняться без изменения semantic profile. Это правило применяется отдельно к JSON Schema profile F21 и C# profile F22.

`Schema.CodeGeneration.Core` принимает formal JSON Schema F21, строит normalized generation model и генерирует source собственным Roslyn `SyntaxFactory` pipeline. Domain/EF reflection и universal NSwag/NJsonSchema schema-to-C# generator не являются source of truth. Parsing/validation JSON Schema допускает специализированную library adapter, но CM mapping/naming/diagnostics остаются Core.

C# v1: Newtonsoft.Json; `#nullable enable`; public sealed classes, get/set, parameterless construction; no record/init/C# required. Integer=`long`, Float=`double`, Decimal=`decimal`; Date/DateTime/TimeSpan explicit converters; Enum real C# enum with string wire keys; FlagEnum `List<TEnum>` with unique/set semantics; Color SDK `RgbaColor`; AssetReference SDK `AssetReference`; Object separate generated type; List=`List<T>`. Deprecated field/option gets `[Obsolete]`.

DateTime converter использует yyyy-MM-dd'T'HH:mm:ss.fffffff, всегда 7 fractional digits, Kind=Unspecified без Z/offset/implicit timezone conversion. AssetReference descriptor's exact five fields/order defined once [F04](F04-entity-instances.md#33-dto-и-parameter-models), immutable captured при F04 Publish и включён в runtime values/hash; Delivery не enrich-ит его заново. Shared CLR helpers и literal syntax/naming/ZIP settings материализуются golden fixtures, не отдельными mappings UI/SDK.

Root generated type carries exact `global::ContentManager.Sdk.Client.DotNet.ContentSchemaAttribute(string schemaId,string schemaVersionId,int versionNumber)` с lowercase canonical GUID `D`. Namespace/file/type/property naming uses frozen PublishedSchemaKey and immutable field keys; Json property attributes preserve exact wire key. Literal normalization/collision policy и shared CLR references frozen в [Q31 §§5–6](../questions/Q31-schema-code-generation.md#56-identifier-normalization-и-collisions): uppercase первого символа с сохранением остальных, invalid runs→underscore; root C# keywords/Windows device stems получают underscore prefix; member/type collisions используют full uppercase FieldId `N`, option collisions — full uppercase SHA-256 UTF-8 exact key. Unresolved type/casefold filepath collision fails whole set. Historical regeneration uses frozen Published metadata, not current logical rename.

Demo endpoint fixed: `POST /api/v1/code-generation/csharp`, body exact SchemaVersionId selection, success `application/zip`, filename `content-manager-csharp-models.zip`. Empty/duplicate selection invalid. Generation is all-or-error; fatal source/output error returns no successful partial ZIP.

cm-csharp/1 generated source bytes retain UTF-8 no BOM, LF, final newline (canonical content profiles' noFinalLF does NOT change this selected generation profile), invariant culture, no timestamps/machine/user metadata. ZIP entries ordinal, fixed entry timestamp/metadata and stable compression policy. Same formal input + profile => byte-identical `.g.cs` and ZIP. Core returns exact SchemaVersionId→root type/file coverage and wrapper verifies all requested IDs. Artifact stateless, not persisted. Numeric size/time guardrails configurable and validated separately.

### 1.2. MVP: typed constraint metadata и temporal semantics

[Q35](../../../business/questions/Q35-value-constraints.md) согласовал typed constraints и List comparer первого MVP. Core переносит полный formal contract metadata, включая recursive Item rules, используя definitions единственного владельца [F01 §2.1](F01-create-entity-schema.md#21-aggregates-entities-и-value-objects). Новые definition/formal semantics требуют новых `cm-schema-content`/`cm-json-schema` profiles; новый `cm-csharp` profile обязателен, если generated metadata/source/ZIP bytes меняются. Старые `/1` profiles остаются воспроизводимыми; history не rehash-ится и не regenerates под новым смыслом.

Constraints сами по себе не меняют прежние CLR types или runtime encoding. Generated setters/constructors/converters не вводят скрытые constraint validation/coercion/trim/default/clamp/round/dedup/sort. Runtime validation выполняют согласованные CM validators, не факт генерации DTO. [Q34](../../../business/questions/Q34-schema-field-semantics.md#datetime-mvp-temporal-semantics-и-совместимость) отдельно согласовал immutable DateTime TemporalSemantics: historical Naive → `DateTime` Kind=Unspecified, new Offset → `DateTimeOffset`. Первый новый MVP profile set совместно включает constraints и Offset, с новыми schema-content, instance-content, JSON Schema и C# profiles; instance `/1` недостаточен для изменённых Offset runtime bytes. Exact whole codec/metadata carriers/identities/layouts/literal bytes ещё **Уточняется** Q19/Q31; product temporal choice закрыт. Старые `/1` bytes/semantics сохраняются, exact old/new модели должны компилироваться совместно.

## 2. Domain


### 2.1. Aggregates, entities и value objects


DDD aggregate/entity F22 не создаётся. Published source/root/number/name/key/hash принадлежат ContentModeling/F21. Source files/ZIP — производные ephemeral artifacts, не domain history или новые версии схемы. Core models описаны в Application.Abstractions по логическим владельцам, не являются EF entities.


| Граница | Собственный результат | Зависимости |
| --- | --- | --- |
| Schema.CodeGeneration.Core | Formal JSON Schema/profile → generated files/bindings/diagnostics | Platform-independent generator/serializer profile; no CM Domain/EF/ASP.NET/UI/Unity/GitLab/S3 |
| Server Application wrapper | Exact F21 documents → Core → validated complete ZIP | Registry query boundary, Core, archive utility |
| API adapter | Binary ZIP или отдельная structured ошибка | HTTP/access/binding, не type mapping |
| MVP CLI | Exact project declaration → Registry → same Core → local generated source | Registry HTTP adapter/filesystem; no administrative UI dependency |


### 2.2. Методы и переходы


| Тип | Метод / действие | Вход | Предусловия | Результат |
| --- | --- | --- | --- | --- |
| CSharpSchemaCodeGenerator | Generate | Matched CodeGenerationInput set + explicit CodeGenerationLimits/fixed profile | Полные supported exact contracts | CodeGenerationResult; source unchanged |
| Application handler | GenerateCSharpModels | Exact selection | F21 availability/profile и full match | Prepared archive либо ошибка, no persistent mutation |
| DeterministicZipWriter | Prepare/write ZIP | Validated complete generated files | Safe unique paths, no fatal generation diagnostics | Completed archive resource, not partial success |
| CLI adapter | Restore / Generate / Check | Config/lock/exact formal inputs + same Core | Complete coverage, pins и canonical owned scope | Verified lock, safe local source outcome либо readonly differences; no second generator |


### 2.3. Инварианты и constraints


- VersionId/SchemaId/source snapshot identity и положительный Published integer number F21 сохраняются. Parent current Name/Key и latest не заменяют выбранный контракт. Retired/Withdrawn exact Published history доступна tooling generation; runtime entitlement этим не определяется.
- Member meaning берётся из stable contract Key/type/FieldId, не authoring Name/HelpText. Исторические версии не переписываются при camelCase→kebab-case profile evolution.
- Deprecated field остаётся property и **обязательно получает `[Obsolete]`** по `cm-csharp/1`. Удалённый из exact schema field не генерируется. Deprecated enum option также получает `[Obsolete]` в `cm-csharp/1`.
- Все 15 wire meanings, required/presence, recursive List/Object/anonymous items, string-key Enum/FlagEnum, RGBA и `AssetReference` semantics должны сохраниться; AssetReference включает required schema AssetKind и общий runtime descriptor contract F05/Q34. Нет numeric bitmask/ColorFormat/InheritanceStrategy или generic object fallback для unsupported construct.
- Requested set сопоставляется с результатом точно, нельзя successful ZIP без одного выбранного контракта. Нет случайных дополнительных source schemas, кроме agreed dependency closure.
- Source ContentHash не hash generated source/ZIP. Одинаковый normalized formal input + `cm-csharp/1` даёт byte-identical `.g.cs` и ZIP по deterministic encoding/order/fixed ZIP metadata.
- MVP constraint/comparer metadata относится к immutable SchemaVersion snapshot, не новым mutable Core/Domain rules. List order передаётся verbatim; String code-point length и UTF-16 ordinal comparer — разные операции; equal keys не требуют ItemKey/Guid tie-break. SDK/Delivery/canonicalizer не выполняют sort.
- DateTime остаётся одним из 15 FieldDataType; immutable TemporalSemantics принадлежит [F01](F01-create-entity-schema.md#21-aggregates-entities-и-value-objects). Naive→Offset требует нового FieldId; generator не переинтерпретирует historical definition. Unsupported semantics/profile — error, не Naive/DateTime/object fallback или host-timezone conversion.


### 2.4. Domain events


Новых domain/integration events/outbox F22 не требуется. Generated files/ZIP не сохраняют Domain state, не запускают Publish/Delivery/reconciliation и не создают F18 event лишь от invocation. Persistent job/progress subscription/history потребовали бы отдельного решения, не вводятся для binary request.


## 3. Application.Abstractions


Core-owned models отделены от server request/archive models. Demo public source ABI находится в neutral BuildingBlocks Core; minimal server wrapper — existing ContentModeling Application.Abstractions/Application/Api рядом с F21, без нового BC. Physical Core project/tooling `net10.0` выбран; standalone package publication/install и MVP distribution остаются Q32. Ниже Demo модели actual; отдельно помеченные MVP/local-tool models остаются целевыми aliases.


### 3.1. Commands


Техническое создание артефакта — GenerateCSharpModelsCommand с handler в Application по [S20](../standards/S20-cqrs.md). No business persistence mutation не является основанием поместить query handler в Application. Source reads остаются query F21 в Persistence.


| Command | Назначение | Result | Concurrency |
| --- | --- | --- | --- |
| GenerateCSharpModelsCommand | Получить exact source set, invoke Core, prepare ZIP | GeneratedArchiveResult | Нет mutable owner/expected Version/If-Match |


**GenerateCSharpModelsCommand**


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SchemaVersionIds | `IReadOnlyList<Guid>` | Да | Exact selected targets | Non-empty, unique exact IDs; configured guardrails; no latest |


### 3.2. Queries


Новые data queries F22 не нужны. Используется GetSchemaContractsQuery и public source/read boundary [F21 §3](F21-schema-registry.md#3-applicationabstractions); UI selection читает обычные schema/version lists. GenerateCSharpModelsQuery из старого technical outline не добавляется параллельно command как второй handler/API.


### 3.3. DTO и parameter models


**GenerateCSharpModelsRequest** — actual Demo DTO в `ContentModeling.Application.Abstractions/CodeGeneration/CodeGenerationContracts.cs`, camelCase JSON `schemaVersionIds`; mandatory `IReadOnlyList<Guid>`, unknown body members запрещены `JsonUnmappedMemberHandling.Disallow`.


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SchemaVersionIds | `IReadOnlyList<Guid>` | Да | Полный выбор exact версий | Non-empty, every Guid non-empty, duplicates invalid; configured limits; no client JSON Schema/name/actor/options/Version |


Demo request не имеет public Options property. `CSharpGenerationOptions` — internal configured `cm-csharp/1` profile, не пользовательский editor: Newtonsoft.Json, deterministic naming/serializer/type mappings и exact-version binding заданы `cm-csharp/1`. HTTP wrapper не принимает arbitrary switches/код с клиента.


**Demo Core public ABI** — namespace `ContentManager.Schema.CodeGeneration.Core`; actual definitions находятся в `CodeGenerationModels.cs`. Server не передаёт Domain/EF graph или SDK instances.

| Модель / операция | Поля / типы | Контракт |
| --- | --- | --- |
| CodeGenerationInput | SchemaVersionId:Guid, JsonSchema:ReadOnlyMemory<byte> | Exact requested version + complete F21 UTF-8 document. Core проверяет total input budget, затем создаёт owned snapshot. Source profile/hash принадлежат F21 metadata и не пересчитываются Core |
| CodeGenerationLimits | MaxSelectedSchemas:int, MaxInputBytes:long, MaxFiles:int, MaxFileBytes:long, MaxSourceBytes:long, MaxDepth:int | Все значения explicit positive; count/input/depth проверяются до emission, actual UTF-8 source bytes ограничены при записи |
| CSharpSchemaCodeGenerator.Generate | IReadOnlyList<CodeGenerationInput>, CodeGenerationLimits, CancellationToken → CodeGenerationResult | Fixed Profile=`cm-csharp/1`; whole requested set, pure values, no HTTP/FS/DB/SDK dependency |
| GeneratedSourceFile | RelativePath:string, Length:int, GetBytes():byte[] | Safe unique `.g.cs`; constructor копирует input bytes, GetBytes возвращает copy. UTF-8 no BOM, LF/final LF, no environment metadata |
| GeneratedSchemaBinding | SchemaVersionId:Guid, FullyQualifiedTypeName:string, RelativePath:string | Exactly one root/file per requested exact version; binding references actual source file |
| CodeGenerationDiagnostic | Code:string, Message:string, SchemaVersionId:Guid?, FieldPath:string?, Severity:CodeGenerationDiagnosticSeverity | Safe exact target/location; enum Info/Warning/Error, stable machine code; malformed JSON не echo-ит source. Anonymous item не получает fake FieldId |
| CodeGenerationResult | Files:IReadOnlyList<GeneratedSourceFile>, Bindings:IReadOnlyList<GeneratedSchemaBinding>, Diagnostics:IReadOnlyList<CodeGenerationDiagnostic>, IsSuccess:bool | Error означает пустые Files/Bindings; successful result covers complete set. IsSuccess означает отсутствие Error |

Normalized Demo parser models internal: FormalContract/FormalField/FormalShape/FormalOption. Они сохраняют IDs/keys, required/deprecated, intrinsic type/option/kind/recursive Item semantics; unknown metadata/ref/type/profile отклоняются. Source annotations не пересчитывают schema ContentHash. Root/object field order — ordinal lowercase GUID `D`; option order — ordinal exact wire key. Formal input не строится по authoring DTO reflection или экземплярам.

MVP typed constraints/comparer/TemporalSemantics остаются отдельным новым-profile contract F01/Q31: current Demo ABI не означает готовый `/2` codec и не вводит Offset semantics в historical `/1`.

**Server GeneratedArchiveResult** — actual owned download bytes в `ContentModeling.Application.Abstractions/CodeGeneration/CodeGenerationContracts.cs`, не JSON response DTO. Constructor копирует completed ZIP bytes; Length:int, GetBytes():byte[] возвращает copy, DownloadFileName constant=`content-manager-csharp-models.zip`. Handler полностью готовит artifact; Controller передаёт copy в FileContentResult. MIME/headers принадлежат Api, Stream ownership aliases прежнего target не являются actual Demo ABI.

**Operational options** — `CSharpGenerationOptions`, section `ContentModeling:CSharpGeneration`; Profile=`cm-csharp/1`, MaxSelectedSchemas=64, MaxInputBytes=33554432, MaxFiles=64, MaxFileBytes=4194304, MaxSourceBytes=33554432, MaxArchiveBytes=41943040, OperationTimeout=`00:01:00`. CoreLimits задаёт MaxDepth=64. Startup ValidateOnStart отвергает missing/unsupported/nonpositive/overflow и inconsistent settings. Эти environment thresholds не являются generation profile bytes; F21 source/formal caps и deadline независимы.

### 3.3.1. MVP CLI: локальные модели проекта

Единственный владелец моделей CLI — F22. Это целевые logical aliases, не обнаруженные source classes/HTTP DTO/EF entities. Они размещаются в CLI host/application и filesystem/Registry adapters, не вводят зависимость pure Core на CLI. Документы versioned; literal JSON casing, internal journal/catalog filenames и diagnostic/exit-code registry закрепляются fixtures при реализации, без изменения согласованных полей/поведения. Package version фиксирует стандартный .NET tool manifest, не поле semantic profile.

**CliProjectConfiguration** — `content-manager.json`.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| FormatVersion | `int` | Да | Версия формата project declaration | Только supported version; unknown fields/options reject |
| CmBaseAddress | `string` | Да | Адрес CM Registry | Валидный base URI по F21 HTTP; без credentials |
| OutputDirectory | `string` | Да | Папка generated sources | Относительные пути считаются от configuration file; canonical owned scope проверяет writer |
| Schemas | `IReadOnlyList<CliSchemaDependency>` | Да | Полный exact selected set | Non-empty, unique non-empty SchemaVersionId; no latest/Key/version-number lookup |

**CliSchemaDependency**.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SchemaVersionId | `Guid` | Да | Exact Published input | Non-empty; несколько versions одной схемы допустимы |
| CSharpProfileId | `string` | Да | Явный generation profile этого exact input | Known compatible profile; package version его не заменяет; no fallback |

**CliProjectLock** — `content-manager.lock.json`.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| FormatVersion | `int` | Да | Lock format version | Supported version, не версия схемы или tool package |
| Schemas | `IReadOnlyList<LockedSchemaContract>` | Да | Exact inputs/profiles/raw bytes | Ровно declaration set, без missing/extra/duplicates |
| GeneratedFiles | `IReadOnlyList<LockedGeneratedSource>` | Да | Expected generated file set/hashes | Full Core coverage/path uniqueness; deterministic ordering; не признак installed output |

**LockedSchemaContract**.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SchemaVersionId | `Guid` | Да | Exact Registry identity | Совпадает с declaration и formal document |
| FormalProfileId | `string` | Да | Actual formal profile F21 | Known supported; unknown/incompatible rejects |
| CSharpProfileId | `string` | Да | Pinned generation profile | Совпадает с declared exact dependency |
| FormalSha256 | `string` | Да | Digest exact downloaded formal bytes | SHA-256, 64 lowercase hex; не semantic ContentHash/HTTP ETag |
| DocumentRelativePath | `string` | Да | Локальный formal input | `content-manager.schemas/<sha256>.json` относительно config; original bytes, без reserialization |

**LockedGeneratedSource** — expected input-derived результат; тот же record используется в committed ownership catalog, без второй модели hash/path.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SchemaVersionId | `Guid` | Да | Exact root coverage | Binding/path из Core; association проверена до writes |
| RelativePath | `string` | Да | Generated filepath относительно output root | Safe unique `.g.cs`; existing GeneratedRelativePathValidator |
| SourceSha256 | `string` | Да | Expected exact source bytes digest | SHA-256, 64 lowercase hex; encoding/newlines по profile |

**CliOutputOwnership** — durable каталог фактически установленных CLI files, отдельно от expected lock.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| FormatVersion | `int` | Да | Версия ownership format | Supported, не proof ownership по comment |
| Files | `IReadOnlyList<LockedGeneratedSource>` | Да | Фактически committed owned file set и hashes | Только `.g.cs` CLI; foreign/manual/Unity `.meta` не включаются автоматически; публикация после известных file outcomes |

**CliOutputJournal** — сведения о незавершённой локальной записи, не persisted CM job.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| PreviousOwnership | `CliOutputOwnership?` | Условно | Каталог до записи | Null только при первой установке в new owned scope |
| CandidateOwnership | `CliOutputOwnership` | Да | Проверенный полный новый набор | Не становится current лишь от подготовки |
| Changes | `IReadOnlyList<CliFileChange>` | Да | Planned file mutations со staging/backups | Только owned paths; full plan зафиксирован до рабочих writes |
| Phase | Logical journal phase | Да | Preparation/apply/commit/recovery состояние | Exact encoding/commit marker fixtures до writer; неизвестный outcome не success |

**CliFileChange**.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| RelativePath | `string` | Да | Изменяемый owned filepath | Safe under canonical output scope; не `.meta`/foreign |
| PreviousSha256 | `string?` | Условно | Expected previous bytes | Null для create; unchanged ownership/hash перед replace/remove; другой current hash conflict |
| NextSha256 | `string?` | Условно | Verified new bytes | Null для remove; хотя бы один old/new hash present |
| BackupRelativePath | `string?` | Условно | Own preserved previous bytes для recovery | Для replace/remove; только принадлежащий CLI staging/backup |
| StagedRelativePath | `string?` | Условно | Own prepared new bytes | Для create/replace; проверен против NextSha256 до установки |

**CliCommandResult** — local outcome, не server ApiResponse.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Success | `bool` | Да | Фактический результат restore/generate/check | True только complete command; check differences = false |
| Diagnostics | `IReadOnlyList<CodeGenerationDiagnostic>` | Да | Existing Core diagnostics + safe CLI projection | Error blocks; Warning/Info nonfatal; literal CLI codes отдельно |
| AffectedPaths | `IReadOnlyList<string>` | Да | Реально изменённые либо отличающиеся пути | Relative safe paths; при check различия, без writes |
| RecoveryRequired | `bool` | Да | Осталась unresolved local operation | Не допускает false success/слепое продолжение; check сообщает без восстановления |

### 3.4. Ports


| Port / boundary | Назначение | Направление |
| --- | --- | --- |
| F21 GetSchemaContractsQuery boundary | Resolve полный exact input set с availability/profile metadata | Server wrapper → Registry; no HTTP loopback |
| CSharpSchemaCodeGenerator | Generate(inputs, limits, cancellation) → CodeGenerationResult | Server/CLI → pure reusable Core |
| DeterministicZipWriter | Validate/prepare ZIP(files, cancellation) | Server wrapper → archive utility; Core не знает ASP.NET/ZIP |
| CLI Registry adapter | Exact ID → original F21 bytes/profile | Existing Registry HTTP/access; no remote generation/no persistent credentials |
| CLI workspace reader | Config/lock/formal bytes/ownership/journal/current source | CLI host filesystem reads/validation; check readonly |
| CLI project-output writer | Complete validated candidate + previous ownership → known result | Canonical scope lock/staging/backups/journal/recovery; no foreign/.meta deletion |


Физические assemblies server wrapper используют existing ContentModeling Application.Abstractions/Application/Api и F21 query registration. Independent Core не импортирует эти серверные types/EF: adapter преобразует DTO к Core-owned input. Concrete C# method signature follows Core API design; semantic options are fixed `cm-csharp/1`, cancellation is supported by caller/runtime.


### 3.5. Specifications / read contracts


F22 не дублирует source specifications/readers. Exact Published/Retired/Withdrawn/owner/ID matching выполняются public F21 boundary. F22 проверяет полноту возвращённого набора и supported profile/dependency closure; новая «get latest schema» query/spec не нужна. UI options/list policy согласуется F21/F22 frontend; selection caption не доказывает текущую backend availability.


## 4. Application


### 4.1. Handlers / use cases


GenerateCSharpModelsCommandHandler — ContentModeling.Application/CodeGeneration, source queries F21 — Persistence. Handler валидирует selection, вызывает Registry, проверяет exact match, адаптирует formal input Core, анализирует diagnostics/bindings, валидирует files и готовит archive. Ни Controller, ни archive writer, ни CLI не реализуют второй type/naming mapper.


| Компонент | Собственная ответственность | Boundary |
| --- | --- | --- |
| Command handler | Full selection → complete prepared archive / explicit error | F21/Core/archive ports, no Domain SaveChanges |
| CodeGenerationInputMapper | F21 document/metadata → Core-owned input | Exact IDs/number/frozen key/source hash сохранены; not authoring DTO reflection |
| Core generator | Parse/validate agreed schema profile, resolve references/types/naming, emit sources/bindings/diagnostics | Pure values/config, no network/FS/CM/Unity dependency |
| Output validator / archive utility | Coverage, path/symbol collision, completed ZIP | Не исправляет поломанный source код случайным rename/omission |


### 4.2. Validators


Actual FluentValidation [S18](../standards/S18-fluent-validation.md) `GenerateCSharpModelsCommandValidator` проверяет request/array nonnull/nonempty, count, nonempty unique IDs. Core parser проверяет exact F21 document identity/profile/definitions; handler проверяет final requested root/file coverage. Отдельный SchemaContractSetMatchValidator class для этих checks не вводится. Непрошедший target/unsupported construct не исключается ради successful ответа.


Actual output checks находятся в Core naming plan, handler coverage checks и `DeterministicZipWriter.SafeName`/preflight. Они проверяют bindings/files completeness, `.g.cs` paths и cross-target uniqueness, rejects absolute/drive/UNC/rooted paths/traversal/unsafe separators/casefold collisions; допустимые Unicode identifier categories сохраняются, filename length считается по UTF-8 bytes. Отдельные GeneratedOutputValidator/GeneratedRelativePathValidator classes не являются actual Demo components. Code/naming identifiers не создаются из untrusted options/Name/кодовых snippets; reserved/type-member conflicts решаются profile. Count/bytes/deadline задаются выбранной конфигурацией, Core recursive depth64.


### 4.3. Mappers

F21 сохраняет neutral [F03 CanonicalProfileId/CanonicalDigest](F03-publish-schema-version.md#33-dto-и-parameter-models) source pair cm-schema-content/1 +64lowerhexSHA256 unchanged в metadata. Handler передаёт Core только exact SchemaVersionId + formal cm-json-schema/1 bytes; number/key/schema ID извлекаются strict parser-ом из F21 document. Source hash не является Core input field и не пересчитывается, не становится generated source/ZIP checksum или cache identity для annotation-sensitive output. Domain tree/rawJSON-number type guessing не используются.

В MVP mapper сохраняет фактический frozen source profile/digest и full typed metadata/TemporalSemantics, проверяя совместимую supported пару formal/generation profiles для каждого exact input. Старый `/1` не получает новые annotations; byte-changing metadata/emission требует нового generation profile. Decimal bounds/step не проходят через double; Offset bounds требуют offset, сравниваются по UTC instant и представлены в semantic schema hash нового profile в UTC по F03/Q19. Naive не конвертируется в UTC. Text plaintext/temporal/comparer/full normalized equality CM extensions не заменяются похожими generic keywords без доказанной эквивалентности.


Request exact IDs → technical command; request не содержит JSON Schema/Name/options/actor или ZIP filename. F21 returned metadata/documents → Core input без потери exactness и с frozen source key. Core outputs → archive files после проверки полной coverage и diagnostics. Diagnostics → safe API error profile, не JSON в успешном binary ZIP. SourceHash не пересчитывается и не используется как generated artifact checksum.


### 4.4. Core algorithms / type profile


Generator читает F21 formal keys/types/required/deprecated/IDs/ref metadata, а не Domain classes или schema labels. Exact mapping/serializer/naming are fixed by `cm-csharp/1` и обязаны соответствовать F13 payload, F19 consumption и Unity 2022.3.62f3 matrix.


| Contract meaning | C# target / открытая форма | Требуемая гарантия |
| --- | --- | --- |
| String | `string`; optional/presence syntax profile | Intrinsic nonempty/max255 сохраняется semantics, не min/max по случайному payload |
| Text | `string`; formatting/annotation profile | Не второй rich-text runtime type без требования, nonempty смысл |
| Integer | `long` | JSON integer; 0 valid |
| Float | `double` | JSON number; finite only |
| Decimal | `decimal` | JSON number + CM type metadata; no binary64 collapse |
| Boolean | `bool` / nullable by optionality | false valid; optional absence distinct |
| DateTime | Exact Naive: `DateTime` Kind=Unspecified; exact Offset: `DateTimeOffset`; explicit converters | 7 fractional digits/100ns; historical Naive без offset/UTC conversion, Offset runtime canonical UTC Z под новым profile; optional → nullable по presence rule |
| Date | platform-neutral SDK/date value used by profile | wire `yyyy-MM-dd`, no timezone |
| TimeSpan | `TimeSpan` + explicit converter | invariant `c`, signed/zero valid |
| Enum | generated C# enum | wire exact string key via Newtonsoft converter/attributes; deprecated member `[Obsolete]` |
| FlagEnum | `List<TGeneratedEnum>` | set semantics/unique keys; canonical sort by wire key; no `[Flags]` |
| Color | SDK `RgbaColor` | canonical uppercase `#RRGGBBAA`; Unity conversion outside generated model |
| Object | generated `public sealed class` | deterministic path/key naming; no shape-based dedup |
| List | `List<TItem>` recursively | Nested List/List<Object>, anonymous items without fake member/ID |
| AssetReference | SDK `AssetReference` | AssetId + AssetKind + confirmed immutable metadata; no bucket/ObjectKey/provider URL; content URL not serialized identity |


Profile `cm-csharp/1` задаёт namespace/type/member/file names, collision rules, reserved identifiers, Newtonsoft.Json attributes/converters и binding generated root type к exact schema version. Name/HelpText могут стать selected comments/annotations, но не identity. Runtime business fields с Key `name`/`helpText` не исключаются по metadata blacklist. C# member alias сохраняет wire Key через agreed serialization, не меняет контракт на PascalCase сам по себе.

Root schemas после разрешения полного requested set обходятся по ordinal lowercase canonical Guid `D` SchemaVersionId; ZIP entries сортируются ordinal по filepath. Nested Object name строится от пути immutable field keys (`AddressCoordinates`, `ItemsItem`), а коллизия разрешается stable suffix от SchemaFieldId definition position. Одинаковый shape в разных позициях не дедуплицируется.


Deprecated property **остаётся с `[Obsolete]`**, не «если удобно профилю». Deprecated option генерируется с `[Obsolete]`, как и deprecated field property. Required не означает C# `required` keyword, `[Required]` или blanket CLR defaults: missing/null/type-validity и supported language/serializer behavior должны быть согласованы. Нет `new DateOnly`/record struct/init/nullable attributes только из .NET 10 server target.

Temporal converter использует shared pure codec с explicit invariant parse/format. Offset authoring forms — `yyyy-MM-dd'T'HH:mm:ss.fffffffzzz` либо UTC `yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'`; runtime Publish/serialization нового instance profile — UTC Z с ровно 7 fractional digits, без потери 100ns. Mandatory known whole-minute offset ±14:00, local и UTC DateTimeOffset range, rejection missing offset/`-00:00`/leap second — type/codec conformance, не hidden constraint coercion. Authoring snapshot сохраняет исходный offset по F04; generated runtime contract не восстанавливает его из UTC payload. Naive codec сохраняет Unspecified и прежние bytes. Ни server, ни browser, ни generated converter не выбирают timezone. Offset equality/bounds/List unique/comparer используют UTC instant; same instant не получает secondary offset key, ties сохраняют authored order. Эти проверки принадлежат CM validators, generated CLR equality не заменяет их.

Optional Naive/Offset properties используют `DateTime?`/`DateTimeOffset?` по существующему presence rule; null CLR допускает отсутствие property, не wire null. Exact historical Naive и новые Offset версии сохраняют разные version bindings и могут генерироваться/компилироваться в одном client project без преобразования старого смысла. Совместимость profiles проверяется для каждого input; разрешённый mixed set не является смешением semantics одной версии.


Input graph/reference closure валиден до emission. Неизвестные keyword/type/custom annotation/ref или неразрешённая после deterministic suffix naming collision — диагностика/error, не arbitrary object/skip field/silent suffix latest. Генерация не требует EntityInstance rows. Same normalized exact inputs/profile дают byte-identical output: UTF-8 no BOM, LF/final newline, deterministic file/member ordering, fixed ZIP entry timestamps/metadata and stable compression policy.


## 5. Api


### 5.1. Endpoints


Code Generation screen route `/code-generation` выбран Q13. Backend `CodeGenerationController.CSharp` реализует фиксированный `POST /api/v1/code-generation/csharp`; `CodeGenerationOpenApiTransformer` задаёт binary string schema под `application/zip`.


| Method / surface | Route / destination | Request | Response | Access |
| --- | --- | --- | --- | --- |
| POST generation | `/api/v1/code-generation/csharp` | `GenerateCSharpModelsRequest` exact IDs | Binary `application/zip`, `Content-Disposition: attachment; filename="content-manager-csharp-models.zip"` | Demo anonymous; MVP staff/developer tooling access |
| Core library API | Server/CLI → CSharpSchemaCodeGenerator | Formal inputs + selected configuration | Source files/bindings/diagnostics | No HTTP/users/DB; caller handles availability |
| MVP CLI | Local tool cm-schema restore/generate/check | Exact config/lock/profile/local inputs | Verified lock/source либо readonly diagnostics/process outcome | Registry staff/developer auth Q24; offline generation без credentials |


Binary success не оборачивается в ApiResponse<byte[]> и не JSON/base64 artifact DTO. `CodeGenerationController.CSharp` выставляет `Cache-Control: no-store`, возвращает `File(byte[], application/zip, content-manager-csharp-models.zip)` только после полного handler/archive result. Error до successful headers — common structured S16 mapping, actual observable codes/statuses описаны [Q15 F22](../questions/Q15-api-and-source-code.md#f22-binary-generation-api-и-source-evidence). Request не запускает persistent job или returns task/artifact URL. Metadata/generation.json не mandatory ZIP content.

Успешный Demo ZIP содержит generated source files; Info/Warning остаются в Core result и серверной диагностике. Success diagnostic headers или preview API не являются обязательным контрактом Demo. Empty и duplicate selection возвращают 400; any missing/non-Published/invalid/unsupported target, unresolved collision либо fatal generator error блокирует весь artifact. Wrapper проверяет ровно одну root binding на каждый requested ID, без extra/missing/duplicate targets.


### 5.2. ETag / If-Match


Нет existing mutable owner, expected Version/If-Match/428/412 flow. Published VersionNumber/sourceHash — metadata, не concurrency token generation. HTTP caching/conditional generation/idempotency keys/durable resume не добавляются только из POST или одинакового selection. Повтор non-persistent generation не меняет source; current access/profile может отличаться, поэтому no permanent cached success guarantee.


### 5.3. OpenAPI и ошибки


Успех описывает binary ZIP и download headers, errors — distinct JSON/ProblemDetails/ValidationProblemDetails согласно final API mapper. NSwag generation/common transport должны реально сохранять bytes/filename и различать JSON failure, а не существующий URL-based media descriptor. Current common web envelope handler не доказывает готовый FileApi/ZIP client — frontend source evidence F22.


Full input/profile/generation/output validation и archive completion предшествуют успешному HTTP result. Если connection оборвалась после headers/в передаче prepared archive, нельзя вставить JSON error в ZIP или утверждать browser save; consumer различает incomplete transfer. Header encoding/transport error codes follow Q15; configurable size/time guardrails and stream ownership are verified before claim корректного API.


## 6. EntityFramework


### 6.1. DbContext ownership


F22 не имеет DbContext/DbSet/domain persistence. Source definitions читаются только F21 owner boundary. No ZIP/source/history/job rows, foreign DB/EntityInstance/Delivery/Media joins или snapshot republish.


### 6.2. Configurations


Нет GeneratedSource/GenerationJob EF configuration. Core models не EF entities и не SQL JSONB payload. Source source/publications mapping описаны F01–F03/F21; output staging ресурсы не называются persistent business state.


### 6.3. Constraints / indexes


Новых SQL indexes/FKs/concurrency columns нет. Input exact identity/readability и output uniqueness — application/Core/archive validation. User history/rate quotas/cache/job tables потребовали бы отдельного требования, не добавляются в Demo/MVP автоматически.


## 7. EntityFramework.Persistence


### 7.1. Repositories / query adapters


У F22 нет собственного repository/persistence query handler. Generate command handler в Application, F21 query handlers уже размещены в owner Persistence по S20. Wrapper не делает query к ContentModelingDbContext вместо F21 и не создаёт GenerateCSharpModelsQuery-in-Application.


### 7.2. Transactions / artifact resources


Нет DB/UoW commit/outbox. Последовательность: validated inputs → complete Core result → validated file set → prepared completed ZIP → binary response. No successful partial archive. Buffer/storage strategy memory/temp file/stream abstraction, bounds/resource cleanup Q20; не hidden durable job/cache/history.


Handler владеет временными resources до передачи Api. На generation/archive/cancel failure закрывает и освобождает свой ресурс. Api owns ready stream до завершения/abort передачи и cleanup; framework поведение проверяется Functional test, не предполагается по названию FileResult. Temporary cleanup только своих output resources; no arbitrary recursive path operations.


CLI local writes — separate boundary от Registry/Core. Согласованы staging/ownership/journal/recovery и exclusive CLI writer scope (§8.2/DF03). Full-set atomic visibility compiler/Unity Editor и общая HTTP+filesystem transaction не обещаются. Prepare failure сохраняет old lock/output; write failure требует фактического recovery outcome, чужие/manual/.meta файлы не очищаются.


### 7.3. Migrations


No F22 DB migrations. Изменения source model относятся к F01–F03/F21. Generation availability current implementation не решается legacy metadata/hash backfill или обходом unsupported source. CLI packaging/version matrix — Q32/MVP tooling, not schema migration/generated artifact history.


## 8. External adapters


### 8.1. Core / ZIP / HTTP


Q32/F19 SDK packaging selects assemblies/UPM-NuGet IDs and DotNet binary delivery; package identifiers are not generated namespace/profile IDs. Full shared CLR namespace frozen: `ContentManager.Sdk.Client.DotNet`; actual `ContentSchemaAttribute`, `ContentFieldAttribute`, `LocalDate`, `RgbaColor`, `AssetKind`, `AssetReference`, `ContentModelConverter` входят в SDK foundation. Emitted references fully qualified; SDK project не становится Core dependency. Consumer preservation/AOT handled by SDK/sample build recipe, never rewriting historical .g.cs to fix stripping; profile reproducibility and ZIP-only Demo unchanged. Actual package publication/Unity release proof отдельны, без package-registry side effect генерации.

Schema.CodeGeneration.Core — reusable tooling `net10.0` library в API BuildingBlocks; profile semantics closed by Q31, standalone package/distribution remains Q32. Server invokes actual `CSharpSchemaCodeGenerator.Generate` API, не копирует type mapping. Core output — owned UTF-8 files/bindings/diagnostics; no network/filesystem/ASP.NET/DbContext/Unity dependency или Stream result. Нет package registry push/read/cache side effect генерации Demo.


Actual `DeterministicZipWriter.Write` собирает classic stored ZIP в bounded memory после complete input/path/count/byte preflight. Ordinal UTF-8 entry names; no path separators/traversal/root/drive/UNC, Windows reserved stems или duplicate/casefold collisions; portable filename byte length ограничен 255. Header literals: method0, UTF-8 flag0x0800, needed-version10, central creator20/DOS, timestamp1980-01-01 00:00:00, attributes/disks0, CRC-32/size/offset explicit; extras/comments/ZIP64/data descriptors отсутствуют. Cancellation проверяется на preflight/CRC/write/final boundary. Artifact возвращается owned bytes; required metadata.json не вводится. Эти bytes не зависят от platform ZIP defaults и не являются source version/time.


API binary adapter owns prepared stream/headers. Browser binary support должен быть сгенерирован/реализован в common NSwag transport после verified OpenAPI, не manual edit generated client. MediaDownload URL DTO и envelope JSON не raw ZIP result.


### 8.2. MVP CLI

Поставка — local .NET tool package `ContentManager.Schema.CodeGeneration.Cli`, команда `cm-schema`. Версия фиксируется в `.config/dotnet-tools.json`; инструмент не входит в runtime SDK/Unity build. Actual tool TFM/runtime/feed/package locks и install matrix — Q32, не выбранная автоматически Unity platform.

`content-manager.json` задаёт format version, CM address, output directory и non-empty unique exact dependencies с явным C# profile для каждого input. Относительные пути считаются от config file; `--config` задаёт файл, иначе поиск только в текущей папке, без parent discovery. `content-schemas.json` не второй источник/alias. Unknown format/options/empty/duplicate IDs отклоняются.

| Команда | Input и boundary | Результат |
| --- | --- | --- |
| `cm-schema restore [--config …]` | Existing F21 HTTP Registry + declaration + optional existing lock | Полный verified formal input set и lock; первый запуск создаёт, обычный сохраняет pin |
| `cm-schema restore --update-lock [--config …]` | Явное новое declaration/profile intent | Полная новая lock candidate после generation/coverage; no latest/implicit upgrade |
| `cm-schema generate [--config …]` | Только verified local lock/documents + same Core | Offline generated source installation по ownership/journal contract |
| `cm-schema check [--config …]` | Те же local inputs/Core + current owned files/catalog | Readonly byte/set/coverage comparison; success либо failure без writes/recovery |

Selection/profile drift требует explicit `restore --update-lock`. Tool package SemVer отдельно от profile; новый byte-changing generator bugfix не маскируется обновлением lock под прежним обещанным profile. Formal profile определяется actual F21 document и проверяется с lock/Core; CLI не придумывает новый Registry negotiation endpoint. Unsupported pairing/unknown semantics fails whole set, no downgrade.

Lock и `content-manager.schemas/<sha256>.json` сохраняют исходные formal bytes, exact identities/profiles и ожидаемые files/hashes. Downloaded SHA вычисляется независимо от ETag; semantic source hash не checksum formal/source file. Перед writes все inputs parsed/validated, Core generated весь набор и coverage/path/collision проверены. Ошибка подготовки сохраняет existing lock/generated output. Config/lock/formal inputs рекомендуется хранить в Git; generated `.g.cs` в первом workflow также хранятся в Git.

Ownership catalog фиксирует действительно установленные пути/hashes, comment `auto-generated` не authority. Update/remove только owned unchanged файлы; новая target path занята foreign file или прежние bytes изменены вручную — conflict без `--force`. Удаление dependency из declaration удаляет только её прежние owned sources после successful generation. Unrelated files и Unity `.meta` CLI не удаляет; update существующего `.g.cs` сохраняет его `.meta`.

Writer готовит staging/backups и journal до рабочих mutations, затем устанавливает полный plan и подтверждённый ownership. На failure выполняет recovery; следующий modifying call сначала разрешает unfinished operation. Неизвестный/изменённый сторонним процессом файл не перезаписывается ради rollback: explicit conflict/recovery required. Exclusive canonical project/output scope lock исключает конкурентные CLI writers; HTTP/FS не общая transaction. Atomic visibility всего набора для compiler/Unity Editor не обещается: generation заканчивается до build/import. Exact journal markers/rename/durability/lock primitives закрепляются real filesystem fixtures; process-crash recovery не объявляется доказанным по схеме.

CLI host владеет parsing/options/diagnostics/cancellation и локальным workspace. Application use cases Restore/Generate/Check вызывают public Core; Registry HTTP adapter читает existing F21; filesystem adapter владеет bytes/hashes/paths/locks/staging/journal/catalog. Server ContentModeling Application/Api/EF, Domain и SDK inventory не импортируются CLI как persistence API. Нет второго naming/type generator, server ZIP endpoint не заменяет local Core.

Registry access — staff/developer tooling; credentials через отдельный authorization adapter, без сохранения в config/lock/arguments и без runtime activation keys/DeliverySession. Механизм CLI login/token/provider остаётся [Q24](../../frontend/questions/Q24-admin-sign-in.md#developer-tooling-доступ-cli); browser cookie сам по себе его не определяет. New MVP profile bytes/identities остаются Q19/Q31. Никаких CLI login API/PAT/JWT scheme, Unity Editor plugin/MSBuild generator или automatic build hook этим блоком не выбрано.

Exact CLI error/exit integers и JSON/journal codecs — engineering fixtures Q31. Success/failure/check drift/recovery required различимы, incomplete write не success. CI: tool restore → `cm-schema check` → compilation; намеренное regeneration/lock update выполняет разработчик отдельным изменением. Actual tool/filesystem/CI/compiler/Unity tests здесь не запускались.

### 8.3. Host / DI


ContentModeling.Application/CodeGeneration содержит actual handler/command validator/options/ZIP writer; Api — CodeGenerationController/OpenAPI transformer; server contracts — Application.Abstractions. F21 source/query Persistence reused. `src/App/ContentManager.Application/ContentManagerApplicationModule.cs` binds options с ValidateOnStart, registers Core/ZIP singletons и добавляет GenerateCSharpModelsCommand в SelfManagedCommands: read-only generation не открывает caller SQL UoW и не сохраняет artifact. `src/App/ContentManager.Api/ContentManagerApiModule.cs` registers binary transformer. Core создаёт per-call parser/AST/result, без shared mutable generation state или CM assembly dependency.


Handler discovery следует existing AddLeanSoftCqrs Application/Persistence registration [app module](../../../../contentmanager.api/src/App/ContentManager.Application/ContentManagerApplicationModule.cs). Configured `cm-json-schema/1` + `cm-csharp/1` are identical for HTTP/CLI; no silent library defaults. Core lifecycle должен быть safe по chosen implementation, shared mutable AST/source/result между concurrent calls не передаётся. Local UI duplicate block не server dedup/lease/job protocol.

MVP DI/adapter явно выбирает поддерживаемую source/formal/generation compatibility для каждого input, включая разрешённый exact old Naive/new Offset набор. Старые `/1` meanings не подменяются новыми. Domain immutable constraints/TemporalSemantics/guards остаются F01; Application.Abstractions переносит neutral metadata; Application wrapper валидирует complete set и profiles; Core переносит metadata и выбирает explicit temporal codec без host-timezone conversion. EntityFramework/Persistence только читают полный сохранённый owner snapshot через F21; Api не добавляет setter-validator, profile picker или sort endpoint. CM-aware validation adapter нужен для extension conformance, generic Draft 2020-12 validator не проверяет CM правила.


## 9. Data flows


По [S17](../standards/S17-data-flows.md) ephemeral server artifact и local CLI writes — разные boundaries.


| Поток | Транспорт | Метод / канал | Endpoint / destination | Доступ | Вход | Application action | Выход | Успех | Ошибки |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| DF01. Demo generation ZIP | HTTP | POST | `/api/v1/code-generation/csharp` | Demo anonymous; MVP tooling access | Exact unique schema-version selection | GenerateCSharpModelsCommand | Binary complete ZIP | Prepared full artifact; not browser save proof | Input/source/profile/naming/output/archive/cancel/transfer failure |
| DF02. Reusable Core generation | Library API | Generate | Schema.CodeGeneration.Core | Caller validates source access | Full formal inputs + agreed profile | Core parse/resolve/map/emit | Files/bindings/diagnostics | Complete matched source semantics | Unsupported/ref/identity/naming/type/profile/cancel errors |
| DF03. MVP CLI workflow | CLI + Registry HTTP + local FS | cm-schema restore / generate / check | Registry → locked raw documents → same Core → owned output | Tooling authorization; local scope ownership | Configuration, lock, explicit profile set | Resolve/verify/Core → lock or staged source publication or readonly comparison | Locked documents/source files/differences + diagnostics | Full command completion; check exact match | Profile/pin/identity/hash/collision/manual/foreign/lock/journal/recovery/IO failure |


### DF01. Demo generation ZIP


1. Api binds exact IDs, validates explicit non-empty collection/each Guid and agreed duplicates/limits, selected access. No browser JSON Schema/source code/options/filename/hash as trusted input. No If-Match.
2. Handler calls F21 batch in-process. Check full matching requested targets, exact immutable metadata/profile/availability and dependency closure. Not silently dropped target/current parent Name/latest fallback. Unavailable/unsupported member не превращается в complete selection.
3. Adapt formal documents to Core inputs; invoke same Core algorithm DF02. Fatal diagnostic/coverage/symbol/file collision yields error, not partial artifact. Request input order/naming/default profile must not accidentally change source semantics.
4. Validate output relative paths/bindings and prepare completed ZIP in selected bounded resource strategy. Before successful HTTP response all generation/archive failures can map to structured error; cleanup prepared/intermediate resources.
5. Api returns binary ZIP/MIME/attachment filename and owns stream lifecycle. No schema/DB/history/outbox/job mutation. Disconnect during transfer →incomplete client outcome, not JSON in ZIP and not confirmed browser save. Consumer may safely retry non-persistent generation with original selection under current policy, no remote artifact resume protocol.


### DF02. Reusable Core generation


1. Validate explicit supported formal/generation profiles and exact input identity; Demo uses `cm-json-schema/1` + `cm-csharp/1`, MVP uses compatible new profiles for constraints/Offset. Compatibility проверяется для каждого exact member; supported mixed Naive/Offset version set разрешён. Unsupported pairing или unknown TemporalSemantics fails before emission; no downgrade/omitted extensions. Resolve only supplied self-contained exact contracts; no network/filesystem/EntityInstance inference inside pure Core.
2. Build symbol/type/namespace/file plan preserving stable keys/field IDs, all 15 meanings, required/deprecated and multiple schema versions; MVP transfers recursive typed constraints/comparer/TemporalSemantics metadata losslessly. Naive maps to DateTime Unspecified, Offset to DateTimeOffset, optional according existing nullable presence rule. Reserved names/collisions/escaping follow selected deterministic profile; no random/time suffix or Name-driven schema rename.
3. Emit complete `.g.cs` code with generated marker, preserved wire serialization meaning and `[Obsolete]` for deprecated properties. Exact old/new types compile together; explicit codecs сохраняют 7digits/100ns, Naive без offset и Offset runtime UTC Z без host timezone. Source schema/hash not edited; option annotation/CLR collection/temporal/decimal profiles selected, not guessed.
4. Return files + coverage bindings + diagnostics. Errors may carry prepared partial internal work but never successful full artifact. Same input/profile has same supported semantics for server and CLI. Cancellation/configured guardrails do not create persistent job.


### DF03. MVP CLI workflow

#### Restore / explicit update-lock

1. CLI parses config/current-directory or explicit --config, validates format/options/non-empty unique IDs/profiles, resolves paths relative to file. Validate project/output scope; before modifying calls acquire exclusive writer ownership and resolve unfinished journal. Refuse foreign/manual recovery conflict, no new writes over unknown state.
2. Ordinary restore reads existing lock and compares exact dependency/profile declaration; drift requires --update-lock. First/explicit update prepares a new candidate. Registry adapter obtains existing F21 exact documents with staff/developer access; no server generation ZIP, latest lookup, Domain/EF or credential persistence.
3. Independently hash original bytes, compare declared identity/actual formal profile and existing lock where applicable. Validate parse/profile compatibility, invoke DF02 same Core on complete set. Fatal diagnostics, unsafe paths, collision, coverage or locked expected-source mismatch stop before new lock/output publication.
4. Store verified content-addressed formal inputs as original bytes under own schemas directory; verify existing same-hash file, not blindly overwrite corruption. Build complete lock with profiles/input SHA/expected source file SHA. Publish new lock only after its entire input set and Core output plan are ready; failed preparation preserves old lock/source. Exact replace/durability markers validated by filesystem fixtures; unused own preparation files are not a success.
5. Report real outcome. Ordinary restore does not recapture new selection/profile, update-lock is explicit pin change. Neither generates working output nor updates runtime selection/inventory.

#### Generate / output publication and recovery

1. Validate config/lock exact match; read original local documents and recompute locked hashes/identities/profiles. No HTTP. Invoke same Core; verify byte hashes/file set/full bindings against lock before output mutation.
2. Under canonical project/output exclusive scope read actual ownership and target files. Existing owned hash must match; new filepath must be free. Plan replace/create and remove dependencies excluded from current lock. Never adopt foreign files by extension/comment, never modify Unity .meta.
3. Prepare staged source bytes/backups and full journal before replacing/removing working files. Use only validated owned paths; report actual applied files. Full validated candidate alone is not committed output.
4. Publish actual ownership after known file outcomes. Failure/cancellation after writes invokes recovery; unfinished state survives crash and is resolved before the next modifying operation. Current files/ownership compared with journal previous/candidate, not arbitrary overwrites. Third-party edits or unknown filesystem outcome return conflict/recovery required. No false success or claim of whole-set atomic compiler visibility.
5. Release writer/resources after known outcome; own staging cleanup only. Generated bytes remain deterministic even if temporary resource names vary. Build/Unity import follows completed generation, not concurrently as a promised safe reader.

#### Check / readonly verification

1. Read config/lock/original inputs, validate exact pins and hashes, regenerate with same Core offline.
2. Compare expected bytes/set/coverage to current output plus actual ownership. Missing/changed/extra owned sources, foreign target collision, corrupt metadata or unfinished journal fail; unrelated files/.meta remain outside cleanup.
3. Return diagnostics and differing paths. No writes, lock update, staging cleanup or recovery. Success only matching complete output, failure never reported as up-to-date. CI check precedes actual compilation; check does not itself prove compilation.



## 10. Ошибки и диагностика


| Ситуация | Требуемый результат |
| --- | --- |
| Empty/null/invalid selection | Validation failure, no generated fake empty complete ZIP |
| Duplicate IDs | 400 validation failure; no silent `Distinct()` |
| Notfound/Draft/nonPublished/unavailable source | F21 failure reflected, no latest/reconstruct from instances |
| Retired/Withdrawn exact Published | Accepted historical source read per F21/F02; UI listing policy separate |
| Unsupported schema/ref/profile/type | Explicit error, no omitted field/object fallback |
| Unknown/missing TemporalSemantics/incompatible exact profile | Error before emission; no Naive fallback, DateTimeOffset approximation или host timezone |
| Naming/symbol/output path collision | Error under agreed namespace/type/path policy; no silent lost target/random suffix |
| Missing root binding/file/dependency | Incomplete result, not complete archive |
| Deprecated field | Still generated and `[Obsolete]`; omission not success |
| Warning versus fatal diagnostic | Error blocks archive; Warning/Info do not; successful ZIP has no mandatory diagnostics manifest |
| ZIP generation/write/cancel before response | Structured error and own resource cleanup, no partial successful file |
| Transfer interrupted after success headers | Binary outcome incomplete; no body JSON mixing/browser-save guarantee |
| CLI unsafe/foreign path/manual edit | Conflict before destructive mutation, no force/foreign adoption/.meta cleanup |
| CLI config/lock/hash/profile drift | Restore/generate/check failure; explicit update-lock для selection/profile changes, no silent recapture |
| CLI unfinished journal/write/unknown outcome | Recovery before next modifying call; check fails readonly, no false full success |
| CLI check missing/changed/extra owned output | Failure/differences, no writes; matching set success |
| CLI Registry auth/provider failure | Explicit failure/unknown, not empty set or runtime entitlement; no credential dump |


Core diagnostics содержат Code/Message/SchemaVersionId?/FieldPath?/Severity, без fake anonymous FieldId. Invalid JSON сохраняет affected exact target ID и не echo-ит raw input. Full schema/source/token/absolute server path не logs/HTTP diagnostics by default. Application projection использует existing ValidationProblemDetails/400 `validation.failed`; Core code остаётся internal diagnostic, а timeout/incomplete-output/F21 source errors имеют distinct ProblemDetails mapping [Q15](../questions/Q15-api-and-source-code.md#f22-binary-generation-api-и-source-evidence). No F22 automatic notification/audit/package publish or new schema state.


## 11. Тестирование

Source conformance provenance tests map F03 profile/digest through F21 formal contract to Core without recomputing/mutating source history. Generated serializers/models cover exact Int64/Decimal/finite IEEE64 semantics/presence/all15 types; content integrity uses common typed canonical library at SDK boundary rather than generated .ToString/general JSON defaults. Unknown content profile rejected before runtime group apply (F13/F19), no source rehash. Generated cm-csharp/1 and cm-json-schema/1 fixed byte policies remain separate; actual runtime tests not executed here.


По [S04](../standards/S04-testing.md)/[S14](../../../catalog/docs/standards/dotnet/testing.md#test-environment-ownership-and-bounded-parallelism) Core pure mapping, server artifact preparation, HTTP binary lifecycle и CLI writes тестируются отдельно.


| ID / DF | SUT / уровень | Собственные данные / воздействие | Наблюдаемый результат |
| --- | --- | --- | --- |
| T01 / DF02 | Unit Core profile | Literal approved F21 contracts всех 15 types | Full fields/type/wire meaning preserved, no arbitrary object/default loss |
| T02 / recursive | Unit + contract | Object/List<Object>/List<List>, anonymous items, required/default/empty collection cases | Correct recursive types/serialization/presence; no fake item field ID or required≠default guard |
| T03 / enum/color/deprecated | Unit + compile/serializer contract | String keys, FlagEnum collection, RGBA, deprecated field/options | No numeric bitmask; property retained with `[Obsolete]`, serializer wire meaning/profile option annotations correct |
| T04 / numeric/temporal | Contract generated model + real serializer | Decimal precision, finite float, naive DateTime, Date, zero/negative TimeSpan | No binary64/UTC/unsupported target type loss; approved range/format exactly |
| T05 / identity/naming | Unit + compilation | Two versions one schema, renamed current parent, reserved/escaped keys/type/file collisions | Same exact source labels/keys, distinguishable types; invalid profile collision fails deterministically |
| T06 / DF01 set | Application with F21/Core doubles, then real F21 integration | Missing/extra/mismatched/duplicate target, unsupported member, error diagnostics/coverage | No successful partial ZIP/drop/latest; inputs mapped exactly, open selection policy chosen explicitly |
| T07 / ZIP | Utility Integration | Prepared files, unsafe traversal/root/drive/UNC path, duplicates/target case policy, injected stream failure/cancel | Only safe complete expected entries, correct UTF-8 text/profile; no bad partial output success, resources cleanup |
| T08 / API | Functional real CM API + PostgreSQL/F21 + generator | Selected endpoint/body, download headers/content/valid ZIP entries and JSON errors | Binary success without JSON envelope; errors separate; no Version/If-Match/job/schema mutations |
| T09 / stream lifecycle | Functional fault barriers | Before headers/after prepared archive/during response transfer, abort | Producer/Api resources disposed at own boundary; truncated ZIP not valid client success |
| T10 / source/golden | Contract + compilation | Independent literal source/profile vectors, repeated generator server/CLI invocation | Byte-identical `.g.cs`/ZIP for same input/profile, sourceHash unchanged; not tests computing expected with same helper |
| T11 / architecture/DI | Architecture/registration | Command/query placement, Core dependencies and adapters | Handler Application, F21 reads Persistence, no Core CM/ASP.NET/Unity/UI/DB/network leak, no dual generator |
| T12 / DF03 restore | CLI Integration with real owned temp project/controlled Registry; real F21 separately | First/ordinary/update-lock, config location, unknown formats/options, empty/duplicates/drift, corrupt cached bytes/ETag/profile mismatch | Full verified pins/original bytes; preparation failure preserves old lock/output; no latest/HTTP in generator |
| T13 / real chain | E2E separately | Real Published CM sources/no instances → UI selected multi-version ZIP → compile in agreed Unity 2022.3.62f3 → F13 payload via F19 | Models match selected exact contracts, compile/deserialise supported profile; browser-only proof insufficient |
| T14 / MVP metadata | Literal formal→generated golden + compile/serializer + actual CM validator | Unicode code points, exact Decimal/bounds, Text plaintext/temporal extensions, recursive List constraints, full Object duplicates разных ItemKey, comparator ties/path | Metadata preserved under new profile; constraints сами по себе не меняют CLR types; real CM validation aligned F04, generic validator alone not extensions proof; no hidden setter validation/coercion/reorder |
| T15 / profile evolution | Contract + F21 real snapshot integration | Old `/1` and new source/formal/generation profiles, Save/Publish/Copy definitions F01–F04, repeated generation | Old bytes/history unchanged; new metadata/output uses new C# profile; actual source provenance preserved; unsupported pairing/partial metadata reject |
| T16 / Offset/mixed versions | Literal formal→source + compile + real serializer/shared temporal codec; actual CM validator | Exact old Naive/new Offset versions together; nullable presence; Object/List; 7digits/100ns, date rollover, ±14:00/range overflow, equivalent instants/bounds/unique/ties, unknown semantics | DateTime Unspecified и DateTimeOffset coexist; old bytes unchanged, Offset runtime UTC Z; no precision loss/host timezone; unsupported metadata fails whole set; type-invalid forms reject; generated equality не заменяет CM UTC-instant rules |
| T17 / DF03 offline/check | Real packaged local tool + own fixtures/temp project | Network unavailable, twoexact/all15, same server/Core inputs, source/lock/manual drift | Offline byte-identical output/check; check no writes; tool version pinned separately |
| T18 / DF03 writer/recovery | Real filesystem/process Integration, independent subprocess + crash/fault barriers | Concurrent writers, crash before/after writes/catalog publication, IO failure, third-party change during recovery, owned dependency removal/foreign files | No foreign delete/false success; actual recovery/lock release and manual conflict; unfinished check readonly failure |
| T19 / CLI Unity consumer | Actual CLI output + pinned Unity project/compiler | Regenerate/remove dependencies, existing .meta, two exact versions/15 types | .meta preserved on update; no CLI deletion of unrelated metadata; actual generated compile; no concurrent import atomicity claim |


Core Unit uses approved literal JSON Schema/config, not current editor DTO or guessed future profile. Independent expected source/wire fixtures verify mapping; compiler/serializer/profile fixed by Q31; Unity/package/TFM matrix finalized with Q32 before full distribution acceptance. No implied `required`/DateOnly/record support from server .NET target.

MVP fixture chain использует реальный CM validator и owner Save/Publish/Copy semantics: type-invalid Save reject, type-valid constraint issues retained in Draft, strict Publish block, type-compatible Copy with target issue not orphan. Generated model setters/serializer проверяются на сохранение values и authored array/ties без исправлений. Equality включает FieldId/presence/FlagEnum sets/nested List order и подтверждение full value после hash match; generator не заменяет эту проверку CLR equality.


F21 source Integration uses existing PostgreSQL/Testcontainers fixture and own schema/version roots (not in-memory substitute). File tests own temp dirs/streams/resources, cleanup only resolved test-owned roots. HTTP/app tests can use doubles for individual failures, но these do not prove actual Registry/Core/network/binary contract. Several modules одного CM API не Unity E2E; real chain includes actual required browser/SDK/Unity steps. Profile/overwrite/availability decisions не выбираются лишь чтобы test passed.


Scoped verification 09.10.2026: Core Unit suite прошёл 41/41 вместе с compiled architecture boundary. Independent literal Boolean/all15 source fixtures не сняты с production emitter; проверены C#8 syntax, recursive depth/input/source bounds, all3 asset kinds, option/field/root/Unicode/device collisions, multiple exact versions, cancellation и owned bytes. Actual emitted consumer с реальным SDK прошёл 71 assertions; SDK codec также проверяет intrinsic String nonempty/max255 и Text semantic nonempty без coercion. Scoped API suite прошёл 78 checks, architecture — 44; final F22 HTTP9 и ZIP/options26 повторно прошли после ZIP whitelist refinement. PostgreSQL cases включают binary headers/ZIP, exact multiversion historical rename/Retired/Withdrawn, whole-set rejection, budgets и own deadline/caller cancellation. Реальный browser Code Generation flow проверен отдельно; UI screenshot/source evidence принадлежат frontend F22. CLI/MVP profiles/install, full F19/F20 runtime и Unity IL2CPP acceptance не объявлены выполненными по этим checks.


## 12. Порядок реализации


1. Implement fixed `cm-json-schema/1` + `cm-csharp/1`, Newtonsoft.Json/Roslyn mappings, deterministic naming/output and fixed Demo HTTP endpoint.
2. F21 agreed complete documents/metadata/readability; Core-owned input/output/binding/diagnostic configuration contracts.
3. Core parse/resolve/plan/map/emit с all-type, collision, required/deprecated/Obsolete и serializer/compiler tests.
4. Application technical command/validators/mappers и complete prepared archive utility; source queries reused F21.
5. Api/OpenAPI/common binary transport/headers/errors/cancellation/stream lifecycle → NSwag → Demo UI selection/download. No options editor/job persistence.
6. Real source/HTTP/ZIP/compile/F13/F19/Unity acceptance; MVP tool restore/generate/check with locked inputs/ownership/recovery по §8.2/DF03. Actual install/TFM/feed/filesystem tests Q32, credentials Q24; не Demo dependency.
7. MVP: сначала materialize combined constraints/Offset profile set и literal metadata/source/ZIP/temporal codec fixtures, затем lossless parse/emission constraints/comparer/TemporalSemantics и actual CM validator conformance; old `/1` acceptance и mixed exact-version compile сохраняются.


## 13. Открытые технические вопросы

Q31 Demo generation profile закрыт. Осталось:

- [Q15](../questions/Q15-api-and-source-code.md#f22-binary-generation-api-и-source-evidence) фиксирует actual Demo body/binary/error/OpenAPI mapping; common browser transport/download/CORS и Unity acceptance проверяются отдельно, не по одному source inventory;
- Demo generation guardrail values выбраны: section `ContentModeling:CSharpGeneration`, profile `cm-csharp/1`, 64 selected schemas, 32 MiB formal input, 64 files, 4 MiB/file, 32 MiB whole source, 40 MiB ZIP, 60 seconds; Core recursive depth64. Positive/overflow/startup consistency checks обязательны; performance/load ceiling этим не доказан. F21 source/formal caps и deadline остаются отдельными;
- CLI config/commands/package/ownership/recovery выбраны Q31; exact document/journal/error/exit codecs и actual filesystem/tool/Unity tests закрепляются до реализации; developer authorization отдельно Q24, runtime F19 inventory ими не заменяется;
- Q32 владеет actual tool package/Core dependency/TFM/feed/install и SDK Unity UPM compatibility; выбранный CLI package не переопределяет SDK base release train.
- [Q35](../../../business/questions/Q35-value-constraints.md)/[Q34](../../../business/questions/Q34-schema-field-semantics.md#datetime-mvp-temporal-semantics-и-совместимость)/[Q31](../questions/Q31-schema-code-generation.md): constraints/comparer и Offset semantics согласованы; exact whole codec, новые metadata carriers/identities/bytes schema-/instance-content/formal/C# profiles — **Уточняется** до golden fixtures. Product temporal choice закрыт, naive `/1` не меняется.

Demo emitter engineering contract материализован в [Q31 §§5–7](../questions/Q31-schema-code-generation.md#5-c-generation-profile-f22), actual Core/SDK ABI и independent golden fixtures. Source normalization — Roslyn four-space LF с fixed header, nullable directive и `default!`; final LF сохраняется. ZIP timestamps/compression/header bytes fixed выше. Неразрешённая collision первых восьми GUID digits namespace/file, type names или casefold paths блокирует весь request; успешный partial ZIP запрещён. Это реализация выбранного profile, не повторный выбор architecture/API/serializer; будущий byte-changing fix получает новый profile.

Demo route, serializer, CLR mapping, namespace/naming, Roslyn ownership, profile identity, deterministic source/ZIP, all-or-error, AssetReference/RgbaColor bindings и exact coverage больше не `Уточняется`; new MVP metadata/profile byte layouts остаются открытыми.
