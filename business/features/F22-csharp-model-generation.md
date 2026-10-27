# F22. Генерация C#-моделей выбранных схем


[Модуль M07](../modules/M07-schema-tooling.md) · [Бэкенд](../../technical/backend/features/F22-csharp-model-generation.md) · [Интерфейс](../../technical/frontend/features/F22-csharp-model-generation.md) · [Registry F21](F21-schema-registry.md).


## Цель


Дать разработчику типизированные C#-модели именно тех схем и версий, которые использует его приложение, без ручного сопровождения копии каждого контракта. Основание — переданное [архитектурное дополнение](../../archive/entity-schema-codegen-architecture.md).


Генератор создаёт модели по JSON Schema, не по реальным экземплярам и не по эвристическому анализу полученного JSON. Он не создаёт интерфейс, отображение новых полей или игровую логику.


## Demo: обязательный сценарий


1. Пользователь открывает возможность генерации в UI CM.
2. Видит доступные схемы и выбирает конкретные нужные версии, в том числе несколько версий одной схемы по общему правилу выбора.
3. Запускает генерацию C#.
4. UI обращается к Code Generation API; сервер получает JSON Schema выбранных контрактов через F21.
5. Независимый переиспользуемый генератор формирует `.g.cs`, backend собирает ZIP и возвращает его пользователю.
6. Разработчик добавляет файлы в Unity-проект и использует модели с SDK F19. Сцена F20 остаётся отдельной демонстрацией runtime.


Выбор `dog@2`, `cat@3` и имена `dog@2.g.cs` из источника иллюстрируют версионность; C# type naming отдельно согласуется. Generated source маркируется как сгенерированный и не предназначен для ручного редактирования. Необязательный `metadata/generation.json` не становится условием Demo.


## Границы ответственности


| Часть | Обязанность |
| --- | --- |
| UI | Выбор схем/версий, запуск, состояние операции, скачивание результата |
| Code Generation API | Получить выбранные контракты, вызвать генератор, собрать ZIP и вернуть HTTP-результат |
| Schema Registry | Формальный JSON Schema-контракт |
| `Schema.CodeGeneration.Core` | JSON Schema → C# source files; переиспользуемая трансформация |
| SDK runtime | Получение данных и их использование как уже сгенерированных типов |


Генерация не переносится в браузер. API-wrapper не дублирует правила генерации в контроллере. Component names из источника задают логические обязанности, не обязательное число микросервисов.


## C# profile Demo

`Schema.CodeGeneration.Core` принимает formal JSON Schema profile F21, а не Domain/EF models. C# semantic profile v1: `urn:vrschool:content-manager:csharp-generation-profile:1` (`cm-csharp/1`). Генератор нормализует formal contract и строит C# собственным deterministic Roslyn pipeline; универсальный NSwag/NJsonSchema code generator не является source of truth.

Demo profile использует Newtonsoft.Json serialization, `#nullable enable`, ordinary `public sealed class` с get/set и parameterless construction; `record`, `init` и C# `required` не являются contract. Integer→`long`, Float→`double`, Decimal→`decimal`; Enum — generated enum с string wire keys; FlagEnum — `List<TEnum>` с set semantics; Color — SDK `RgbaColor`; AssetReference — SDK `AssetReference` с AssetId/AssetKind/server-confirmed immutable metadata, без bucket/ObjectKey/provider URL. Nested Object создаёт deterministic nested generated type, List→`List<T>`.

Exact version binding присутствует в generated root type через SDK attribute с SchemaId/SchemaVersionId/VersionNumber. Namespace/file/member names строятся из frozen PublishedSchemaKey и immutable field keys; collisions разрешаются stable suffix от IDs/keys. Несколько Published versions одной Schema должны компилироваться одновременно.

Demo endpoint: `POST /api/v1/code-generation/csharp` с exact `SchemaVersionId[]` → `application/zip`, filename `content-manager-csharp-models.zip`. Empty/duplicate selection invalid; generation all-or-error; partial ZIP запрещён. Output deterministic: UTF-8 без BOM, LF, final newline, stable file ordering, fixed ZIP entry timestamps/metadata. Одинаковый formal input + `cm-csharp/1` даёт byte-identical `.g.cs` и ZIP. Generation stateless, artifacts не сохраняются.

## Последующие этапы


**MVP:** UI→ZIP остаётся доступным, но перестаёт быть единственным способом. Обязателен CLI поверх того же Schema Registry + `Schema.CodeGeneration.Core`, чтобы генерация выполнялась без ручного посещения UI. MVP должен иметь полноценный reusable codegen workflow для точного набора schema versions; конкретные дополнительные adapters (Unity Editor plugin, source generator, build integration, CI wrapper) включаются только после отдельного определения, а не автоматически все сразу.

MVP переносит согласованные [Q35](../questions/Q35-value-constraints.md) typed constraints/List comparer из exact Registry profile в generated contract metadata, включая collection rules и recursive Item definitions. Единственный владелец моделей — [F01 backend](../../technical/backend/features/F01-create-entity-schema.md#21-aggregates-entities-и-value-objects). Constraints не меняют CLR types, не добавляют hidden setter/constructor/converter validation/coercion/default/clamp/round/dedup/sort. CM validator проверяет правила отдельно; generated model не становится доказательством валидности значения.

Первый новый MVP profile set совместно включает constraints и согласованный [Q34 Offset DateTime](../questions/Q34-schema-field-semantics.md#datetime-mvp-temporal-semantics-и-совместимость): новые schema-content, instance-content, JSON Schema и C# profiles. Constraints сами по себе не меняют runtime encoding; Offset bytes меняются, поэтому instance `/1` недостаточен. Старый `/1` сохраняет прежние bytes/semantics без historical rehash/re-generation под новым смыслом. Product temporal choice закрыт; exact whole new-profile codec/identities/metadata/layouts/golden bytes ещё **Уточняется** Q19/Q31. Canonicalizer/Delivery/SDK сохраняют authored List order, equal keys не получают скрытый tie-break.

Generator сохраняет exact immutable DateTime TemporalSemantics, включая Object/List Item: historical Naive → `DateTime` Kind=Unspecified, new Offset → `DateTimeOffset`; optional → nullable по existing presence rule. DateTime остаётся одним из 15 типов, смена semantics требует нового FieldId. Explicit codecs сохраняют ровно семь fractional digits/100ns и Offset runtime UTC Z, без server/browser timezone conversion; authoring original offset остаётся по F04. Offset bounds/equality/UniqueItems/comparer используют UTC instant, same instant разных offsets равен без secondary offset key. Unknown semantics/profile отвергается, не получает Naive fallback. Exact old/new версии могут одновременно генерироваться и компилироваться в одном client project; выбранный per-exact profile не становится UI picker.


MVP project declaration/CLI workflow выбран ниже: content-manager.json, local tool cm-schema, lock/raw input bytes и safe owned output. Модели/algorithms — backend F22, actual packaging — Q32; это не runtime manifest loader.


В Demo не требуются CLI, source generator, Unity Editor plugin, автоматизация сборки, потребление content-manager.json, incremental generation и package registry generated моделей. Developer-tooling workflow MVP не отменяет runtime sync F13/F19.


## Модели и совместимость


Появление generated свойства не гарантирует реализацию его вывода или обработки в Unity. Полноценная модель функциональной совместимости F14 остаётся отложенной. Автоматизация написания DTO и автоматическое поведение приложения — разные задачи.


Декларация content-manager.json фиксирует exact schema dependencies и generation profiles. Она не entitlement/licensing и не installed InstanceVersion inventory; runtime состояние принадлежит F19.


## MVP CLI workflow

CLI поставляется отдельным local .NET tool `ContentManager.Schema.CodeGeneration.Cli`, команда `cm-schema`; версия закреплена в standard `.config/dotnet-tools.json`. Он использует тот же Registry/Core, не входит в runtime SDK/Unity device build. Это согласованный target, package/source/install ещё не созданы этой аналитикой.

Конфигурация `content-manager.json` содержит format version, CM address, output directory и non-empty unique exact SchemaVersionId dependencies с explicit C# profile каждого. `content-schemas.json` не alias. Paths от config file; --config либо файл только в текущей папке. Unknown options/format, duplicates/empty — ошибка; no latest/current Key lookup.

| Действие | Результат |
| --- | --- |
| `cm-schema restore` | Полный проверенный formal input set и lock; первый создаёт, обычный соблюдает существующий |
| `cm-schema restore --update-lock` | Явное обновление dependencies/profiles по configuration, без latest |
| `cm-schema generate` | Offline generation из locked local documents через тот же Core |
| `cm-schema check` | Readonly exact bytes/set/coverage comparison с фактическим owned output |

`content-manager.lock.json` фиксирует IDs/formal и C# profiles, SHA-256 original formal bytes и expected filenames/source hashes. Documents хранятся как original bytes в `content-manager.schemas/<sha256>.json`, без reserialization. ETag не checksum authority. Configuration/lock/formal inputs рекомендуется хранить в Git, generated `.g.cs` первого workflow тоже в Git. Developer обновляет явно; CI check → compile. Tool package SemVer не generation profile.

Full resolution/parse/profile/generation/collision/coverage до publication. Ошибка подготовки сохраняет old lock/generated source. Unknown/incompatible profiles отвергаются целиком; old profile не получает silently new byte semantics.

CLI владеет своим actual output catalog, а не всеми файлами папки. Manual edit owned source либо foreign new target path — conflict без --force. Dependency removal удаляет только unchanged owned sources; foreign files/Unity `.meta` не удаляются, existing source update сохраняет `.meta`. Staging/journal/backups и exclusive project/output lock обеспечивают recoverable запись; следующий modifying call сначала разрешает unfinished state. Third-party changes during recovery требуют explicit conflict, не перезаписи. Atomic visibility всего набора для параллельного compiler/Unity Editor не обещается: generate завершается до build/import. Check ничего не чинит/не пишет.

Доступ — developer/staff Registry, credentials не config/lock/command arguments, не activation keys/DeliverySession. Exact CLI authorization отдельно [Q24](../../technical/frontend/questions/Q24-admin-sign-in.md#developer-tooling-доступ-cli); новые MVP profile identities/bytes Q19/Q31. Модели и технические потоки определены один раз в [backend F22](../../technical/backend/features/F22-csharp-model-generation.md#331-mvp-cli-локальные-модели-проекта), package/platform/install fixtures — Q32.


## Приёмка


| Сценарий | Результат |
| --- | --- |
| Выбран конкретный набор | ZIP содержит модели выбранных контрактов и необходимые зависимые типы по правилам генератора, не весь случайный каталог |
| Выбраны две версии схемы | Их типы используют deterministic exact-version namespace/file naming `cm-csharp/1` и могут существовать совместно |
| Схема без экземпляров | Модель генерируется из Registry |
| Нажата генерация | UI не преобразует JSON Schema и не собирает ZIP самостоятельно |
| Файлы добавлены в Unity 2022.3.62f3 | Модели компилируются и принимают соответствующий payload через F19 |
| Повторная генерация | Source можно пересоздать, ручные правки `.g.cs` не требуются |
| MVP CLI restore/generate/check | Explicit exact pins/lock/original bytes, offline same Core, readonly check различий; no silent latest/network build |
| CLI manual/foreign files | Conflict без force; remove только unchanged owned sources, Unity .meta сохраняется |
| CLI crash/concurrency | Реальные process/filesystem проверки staging/journal/exclusive lock/recovery, no false full success |
| Новый consumer tooling | Переиспользует Registry и generator, не пишет другой алгоритм |
| MVP constraints/comparer | Full typed metadata и recursive definitions сохранены; constraints не меняют CLR mappings, setters/serializer не исправляют values и не сортируют array |
| Старый `/1` и новый output | Byte-changing metadata использует новый C# profile, старые literal `.g.cs`/ZIP воспроизводимы |
| CM extension semantics | Actual CM validator + Unicode/exact Decimal/bounds/Object duplicates разных ItemKey/ties/recursive/Save-Publish-Copy fixtures; generic validator alone не proof |
| Mixed exact Naive/Offset versions | DateTime Unspecified и DateTimeOffset/optional nullable компилируются вместе; explicit real serializer сохраняет 7digits/100ns и runtime UTC Z, old bytes unchanged |
| Offset conformance | Rollover/±14:00/local и UTC overflow/missing offset/-00:00/leap second/equivalent instants/bounds/unique/ties/Copy orphan fixtures; unknown semantics rejects whole set без hidden timezone |


Маршрут Demo UI `/code-generation` выбран [Q13](../../technical/frontend/questions/Q13-ui-scope.md). Типовые mappings, serializer, namespace/naming, exact API URI/response и deterministic artifact profile закреплены [Q31](../../technical/backend/questions/Q31-schema-code-generation.md). Числовые guardrail limits остаются конфигурационными, а final machine error names относятся к Q15. Новое требование о codegen явно расширяет Demo и заменяет прежнее исключение генерации моделей; остальные исключения Demo не отменяются.


## Языконезависимый источник, конкретный target C#


По [дополнению SDK от 30.09.2026](../../archive/sdk-contracts-architecture-update.md) общая цепочка контрактов — Schema API → JSON Schema → Code Generation. F22 остаётся генерацией C# для текущего сценария. Другие targets, например TypeScript и C++, являются возможным развитием, не обязательным объёмом Demo.


Переименование `Sdk.Client.Core` в `Sdk.Client.DotNet` относится к runtime SDK и не меняет имя `Schema.CodeGeneration.Core`. Общее основание клиентов — контракты, а не сгенерированные C#-классы или бинарная библиотека.


## Generated model identity


C# generation использует contract key/type и exact immutable TemporalSemantics, а не authoring `Name`. Stable `SchemaFieldId` нужен для сравнения поколений и diagnostics, но новый key/type/temporal смысл означает новый field identity и новый generated member semantics.


Required/deprecated должны быть отражены согласованным generator/profile; deprecated field в C# помечается `[Obsolete]`. Typed constraints и List comparer первого MVP согласованы Q35 и отражаются в новом metadata profile; serializer/type mappings закреплены Q31. Regex/Float MultipleOf/Decimal Precision/Scale и расширенные comparers не входят в первый MVP.


## Generated semantics


Generated C# использует Enum option keys как значения контракта, FlagEnum как коллекцию keys/типизированных enum values с корректной string serialization, RGBA как единый contract и recursive collections для nested List. Numeric bitmask и ColorFormat не генерируются.


Deprecated field остаётся generated property и обязательно получает `[Obsolete]`; удаление property происходит только когда field отсутствует в выбранной schema version.


## Demo UI и MVP CLI


Граница этапов закреплена:


- **Demo:** экран Code Generation содержит только выбор точных SchemaVersion, итоговый selection, кнопку генерации и скачивание ZIP с `.g.cs` файлами;
- **MVP:** local .NET tool cm-schema с content-manager.json, restore/offline generate/readonly check, pin/raw lock и safe owned filesystem workflow; UI→ZIP сохраняется;
- UI Demo не превращается в редактор generator options/CI pipeline;
- CLI не создаёт второй алгоритм генерации: он вызывает тот же reusable generator;
- Unity Editor/source-generator/build/CI integrations не считаются автоматически обязательными только из фразы «полноценная кодогенерация»; их конкретный MVP scope определяется отдельно.
