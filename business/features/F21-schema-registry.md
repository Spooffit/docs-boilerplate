# F21. Schema Registry: внешний контракт схем

[Модуль M07](../modules/M07-schema-tooling.md) · [Серверная спецификация](../../technical/backend/features/F21-schema-registry.md) · [Генерация моделей F22](F22-csharp-model-generation.md).

## Цель и источник

Предоставить формальное machine-readable описание конкретной версии схемы независимо от внутренних классов CM. Решение принято в переданном [дополнении об архитектуре codegen](../../archive/entity-schema-codegen-architecture.md), разделы 1–8, 32–33 и 50.

Schema Registry API является источником внешнего контракта JSON Schema. Авторские схемы и их версии по-прежнему принадлежат ContentModeling; Registry не создаёт второй редактируемый каталог и не пересматривает внутренние типы полей.

## Пользователи и границы

Потребители — Code Generation API Demo и другие developer/build-time инструменты последующих этапов. Действие не требует получения реальных экземпляров для угадывания структуры и не является runtime-синхронизацией данных.

В Demo Registry участвует в реальной цепочке генерации C#. Вход, лицензирование, шина и VR School не вводятся как его зависимости. Политика доступа дальнейших этапов уточняется отдельно, а не подставляется из общей схемы SDK.

## Сценарий

1. Потребитель указывает конкретную схему и версию либо набор таких выборов.
2. Registry разрешает существующие идентификаторы и получает определения выбранных версий.
3. Выдаёт формальный JSON Schema-контракт для них.
4. Code generator либо иной инструмент использует этот контракт, не внутреннюю доменную модель CM.

Читаемая запись `dog@2` в источнике обозначает «ключ + конкретная опубликованная версия» и не утверждает финальный HTTP URL. По F01–F03 Published имеет положительный целочисленный номер внутри схемы; Draft опубликованного номера не имеет. Exact VersionId остаётся canonical identity Registry. Key-based aliases не требуются для canonical read; несколько версий одной схемы допустимы.

## Правила

Генерация опирается на опубликованное описание схем, а не на состав случайного экземпляра. Exact Retired Published история доступна для Registry/codegen по F02. Draft не выдаётся молча за стабильную Published версию. Exact Published Withdrawn/Retired history доступна tooling exact-read; обычная UI-selectability определяется F22 presentation policy, но не уничтожает formal contract.

Name/Key и hash исторического контракта берутся из замороженного Published snapshot F03. Переименование текущей логической схемы не переписывает snapshot; Registry использует ID-based `$id` и frozen PublishedSchemaKey. Export profile закреплён Q31.

Нужно сохранить смысл каждого определённого поля, структуры и версии в выбранном внешнем контракте. JSON Schema dialect/profile, null/required, special-type extensions и exact identity закреплены Q31. Отсутствие отображения сложного поля является неполной реализацией.

## Формальный профиль Demo

Registry экспортирует только exact Published `EntitySchemaVersion` в **JSON Schema Draft 2020-12**. CM semantics versioned profile: `urn:vrschool:content-manager:json-schema-profile:1` (`cm-json-schema/1`). `$id` не зависит от hostname/key и имеет форму `urn:vrschool:content-manager:schema-version:{SchemaVersionId:D}` с lowercase canonical Guid. Документ self-contained: cross-schema `$ref` graph в Demo отсутствует; internal `$defs` допустим.

`x-cm-*` metadata сохраняют stable identities и CM semantics: schema/version IDs, positive Published VersionNumber, SchemaFieldId, FieldDataType, List OrderMode, AssetKind и option/deprecated metadata. Optional означает property может отсутствовать; `null` не является значением текущих 15 типов; required задаётся стандартным `required`; `additionalProperties: false` используется для Object. Exact Retired/Withdrawn Published history остаётся доступна tooling exact-read; Draft не является stable Registry target.

Public exact contract: `GET /api/v1/schema-registry/schema-versions/{schemaVersionId}` → raw `application/schema+json`, без `ApiResponse<string>` и без fallback на latest. Registry bytes deterministic; ETag относится к exported JSON Schema bytes и не равен F03 `ContentHash`. Internal F22 получает те же formal contracts через Application boundary без HTTP loopback; public batch endpoint для Demo не нужен.

Formal mapping всех 15 типов, включая `long`/`double`/`decimal`, naive Demo DateTime, signed TimeSpan, string Enum, key-set FlagEnum, `#RRGGBBAA`, recursive Lists и typed `AssetReference(AssetKind)`, закреплён в [Q31](../../technical/backend/questions/Q31-schema-code-generation.md).

## Результат и этапы

В MVP Registry переносит согласованные [Q35](../questions/Q35-value-constraints.md) typed constraints конкретного immutable SchemaVersion: применимые value rules, collection constraints, recursive List Item rules и List comparer. Их модель определена один раз в [F01 backend](../../technical/backend/features/F01-create-entity-schema.md#21-aggregates-entities-и-value-objects); Registry не создаёт изменяемый каталог ограничений.

Первый новый MVP profile set совместно включает Q35 constraints и согласованный [Q34 Offset DateTime](../questions/Q34-schema-field-semantics.md#datetime-mvp-temporal-semantics-и-совместимость): новые schema-content, instance-content, JSON Schema и C# profiles. Constraints сами по себе не меняют runtime encoding; Offset runtime bytes меняются, поэтому instance `/1` недостаточен для Offset. Старые `/1` snapshots/bytes/hash/export остаются прежними, historical rehash/re-export под новым смыслом отсутствует. Product temporal choice закрыт; exact whole new-profile codec/identities/metadata/golden bytes ещё **Уточняется** Q19/Q31. Group/inventory/manifest profiles меняются только при собственном layout/semantics change.

Registry сохраняет exact immutable `DateTimeValueDefinition.TemporalSemantics`: historical Naive использует прежний CM format, new MVP Offset — `format: date-time` плюс CM strict precision/range rules. DateTime остаётся одним из 15 типов; смена Naive→Offset требует нового FieldId. Offset — instant с known whole-minute offset ±14:00; invariant wire имеет ровно семь fractional digits, mandatory offset, без `-00:00`/leap second и с local/UTC DateTimeOffset range checks. Authoring сохраняет введённый offset, runtime нового profile — canonical UTC Z без потери 100ns/hidden timezone conversion. Inclusive bounds/equality/UniqueItems/List comparer используют UTC instant; equivalent offset bounds имеют same semantic schema hash нового profile. Unknown temporal semantics/profile не заменяется Naive. Определения принадлежат F01, runtime projection — F04/Q19, не Registry.

Standard JSON Schema keywords используются только при эквивалентной typed semantics: numeric inclusive bounds, String Unicode code-point lengths и List counts. Text plaintext length, temporal bounds, comparer и особое full normalized equality выражаются CM extensions и проверяются реальным CM validator; generic JSON Schema validator не доказывает эти правила. List order — требование к authored array, exporter/Delivery/SDK его не сортируют. Regex, Float MultipleOf, Decimal Precision/Scale и расширенные comparers не входят в первый MVP.

Demo: JSON Schema выбранных версий для F22. MVP: тот же API пригоден для других tooling consumers без зависимости от UI. Дальнейший процесс с `content-schemas.json` использует этот же источник контрактов, а не новый Registry.

Registry не равен новому микросервису по умолчанию. «Отдельный API» означает публичную ответственность/контракт; размещение в существующем CM согласуется технической спецификацией.

Для Launcher integration после Demo formal schema contract должен сохранять `DeliveryMode` AssetReference leaf (включая recursive ItemDefinition) как policy metadata точной Published версии по [F01](F01-create-entity-schema.md#режим-доставки-assetreference-после-demo). Это не шестое поле runtime AssetReference, не generated instance property и не runtime выбор latest схемы. Historical отсутствие mode означает effective OnDemand без изменения Registry `/1` bytes. Exact versioned annotation, metadata integrity/profile ID и fixtures **Уточняется [Q37](../../technical/frontend/questions/Q37-launcher-asset-delivery.md)** вместе с Q19/Q31; ранее выбранный `cm-json-schema/1` не расширяется молча. Registry остаётся language-neutral tooling contract, а resolved asset manifest принадлежит Delivery.

## Приёмка

| Сценарий | Результат |
| --- | --- |
| Выбрана конкретная версия | Возвращён контракт именно этой версии, не latest |
| Нужны две версии схемы | Доступны обе с различимой идентичностью |
| Нет экземпляров выбранной схемы | Контракт можно получить из определения, а не вывести по данным |
| Внутренний класс CM изменён | Потребитель зависит от согласованного внешнего контракта, не Domain assembly |
| Контракт использует особый тип | Его экспорт/генерация проверены по принятому правилу, не заменены молча произвольным object |
| MVP recursive constraints/comparer | Полный exact snapshot losslessly экспортирован под новым profile; collection rules и Item rules не теряются |
| Unicode/exact Decimal/Text/temporal/List equality | Literal fixtures и actual CM validator доказывают правила; generic validator не принимается за proof extensions |
| Старый `/1` и новый profile | History bytes/hash прежние; новые правила не приписаны старому profile; array order и equal-key ties сохранены |
| Exact Naive/Offset contracts | Recursive source semantics сохранены; Naive CM format и Offset date-time + strict CM 7digits/range checks различены; unknown semantics rejects |
| Offset temporal fixtures | Literal rollover/±14:00/overflow/100ns/equivalent instants/bounds/unique/ties подтверждают strict codec/CM validator и сохранение old `/1`; generic date-time недостаточен |

Маршрут exact Registry read, raw media type и JSON Schema profile закреплены [Q31](../../technical/backend/questions/Q31-schema-code-generation.md). Exact Retired/Withdrawn Published read сохраняется для tooling. Это target requirements, не утверждение о реализованном endpoint.

## Общие контракты

В [дополнении от 30.09.2026](../../archive/sdk-contracts-architecture-update.md) эта точка выдачи названа **Schema API**: официальный источник машинно-читаемых контрактов выбранных схем и версий. Это роль существующей F21, не ещё одна фича с альтернативной копией определений.

Общая основа клиентской экосистемы — OpenAPI, JSON Schema и Schema API. JSON Schema не является .NET-форматом; C# tooling — текущий потребитель, а для других языков возможны независимые генераторы и SDK. Их реализация не добавляется в Demo этим уточнением.

## Field identity в Registry

Опубликованный schema contract должен сохранять stable `SchemaFieldId` между версиями для одного логического поля. Изменение key/type или immutable TemporalSemantics для того же ID запрещено; новый смысл получает новый ID.

`Name` и authoring `HelpText` не являются runtime content properties. Registry обязан выражать contract keys/types/required/deprecated и согласованные constraints, не используя отображаемое Name как идентичность поля.

## Formal type contract

Registry export использует string-key Enum, array-of-string-keys FlagEnum, единственный RGBA `#RRGGBBAA` и recursive List/array definitions, включая nested lists. `InheritanceStrategy` в target contract не экспортируется.

Deprecated field остаётся частью конкретной Published schema version и помечается deprecated metadata; Required при этом false. Name/HelpText остаются authoring metadata. Typed constraints/comparer первого MVP согласованы Q35; Demo baseline ими не расширяется.
