# F01. Создание схемы сущности


## Цель


Специалист создаёт новый логический тип динамического контента и его первый Draft, не привязывая схему к Product/ProductVariant и не публикуя её клиентам.


## Участники и этапы


- Demo: без входа и ролей.
- MVP: действие доступно сотруднику с правами Specialist по F16/Q10.


## Основной сценарий


1. Специалист задаёт `Name`, глобально уникальный `Key`, optional Description и дерево полей.
2. Для первого Draft задаёт короткий `Title` и optional `ChangeComment`.
3. До первого Save форма остаётся локальной: пустая или структурно невалидная схема не создаёт серверный Draft. Первый Save валидного дерева создаёт `EntitySchemaId`, `EntitySchemaVersionId` и `SchemaFieldId` всех новых полей.
4. Форма открывает точный созданный Draft для readonly просмотра. Дальнейшее редактирование относится к F02 и не включается в готовность F01.
5. Клиентская публикация/Delivery не меняются.


## Идентичность и ключи


- `EntitySchemaId` — стабильная identity логической схемы.
- `EntitySchema.Key` глобально уникален в CM.
- Key логической схемы можно переименовать, если новый key свободен; Published SchemaVersion не переписываются задним числом.
- В Demo schema/field keys вводятся явно в `camelCase`: `^[a-z][a-zA-Z0-9]*$`, длина 1–50 символов. Регистр сохраняется; сравнение case-sensitive (`fieldKey` и `fieldkey` — разные ключи). Автоматического `Camelize` нет.
- В MVP UI может предлагать `kebab-case` по названию, но пользователь подтверждает/исправляет значение.
- `SchemaFieldId` новых полей создаёт только backend. Create contract **не содержит ID** и не использует nullable-ID/`IsNew` гибрид.
- `SchemaField.Key` существующего `SchemaFieldId` immutable.


## Метаданные и нормализация

| Поле | Обязательность | Ограничение после удаления внешних пробельных символов |
| --- | --- | --- |
| Name схемы, Title первого Draft, FieldName (Name поля), optionName (Name варианта Enum/FlagEnum) | Обязательное | 1–255 символов |
| Description, ChangeComment, HelpText | Необязательное | Не более 1024 символов; пустое значение становится null |

Перед проверкой и сохранением у метаданных удаляются только внешние пробельные символы. Внутренние пробелы и регистр сохраняются. Обязательное значение, состоящее только из пробельных символов, недопустимо. Для необязательных полей отсутствие, пустая строка и строка из пробельных символов означают отсутствие значения (`null`); служебный текст не подставляется. Это правила authoring metadata, а не нормализация runtime values полей.


## Поля схемы


Demo поддерживает весь набор:


`String`, `Text`, `Integer`, `Float`, `Decimal`, `Boolean`, `DateTime`, `Date`, `TimeSpan`, `Enum`, `FlagEnum`, `Color`, `Object`, `List`, `AssetReference`.


Общие правила:


- keys полей уникальны во всём дереве одной SchemaVersion, включая разные ветки Object/List, с case-sensitive сравнением;
- `Name` и `HelpText` — authoring metadata;
- существующий field сохраняет stable `SchemaFieldId` между версиями, включая разрешённый возврат ранее удалённого поля с прежними Key/Type/смыслом и цепочкой definition-контейнеров;
- для существующего field immutable `Key`, `FieldDataType`, type-specific semantic identity и цепочка definition-контейнеров Object/List; перенос в другой контейнер требует нового ID;
- разрешено менять Name/HelpText;
- `IsRequired` только `true → false`;
- `Deprecated` только `false → true`;
- Deprecated field обязан быть optional;
- другой key/type/смысл создаётся как новый field с новым ID;
- отдельный `SortOrder` schema fields не хранится; canonical порядок не зависит от UI-сортировки. Визуальная перестановка полей и перестановка элементов runtime List не меняют field identity. Monotonic rules проверяются по всей Published history этого ID, а не только по latest version.


## Семантика типов Demo


- `String`: однострочное значение, max 255.
- `Text`: строка с ограниченным HTML: абзацы, переносы строки, списки, жирное и курсивное выделение. Backend очищает HTML до сохранения; UI получает и показывает очищенный результат. Семантически пустой текст после очистки считается отсутствием значения. Максимум явно задаётся backend configuration и передаётся UI; числовой предел выбирается по representative fixture и здесь не назначается.
- `DateTime`: Demo без timezone/offset (`Naive`); historical `/1` сохраняет этот смысл. MVP Offset semantics определены ниже.
- `Date`: календарная дата.
- `TimeSpan`: duration, включая zero/negative.
- `Enum`: один stable option key.
- `FlagEnum`: набор stable option keys, не numeric bitmask.
- Enum/Flag option: immutable Key, mutable Name, monotonic Deprecated.
- `Color`: `#RRGGBBAA`.
- `Object`: минимум один child; вложенные Object/List разрешены.
- `List`: recursive item definition, включая `List<List<T>>`; `OrderMode = Default|Ascending|Descending`; comparator Asc/Desc относится к MVP/Q35.
- `AssetReference`: definition обязательно содержит один `AssetKind` (`Image`, `Audio`, `Video` в Demo); runtime reference имеет stable `AssetId`; несколько ресурсов выражаются `List<AssetReference>` с kind в recursive item definition. Допустимые extensions/MIME не задаются схемой и приходят из централизованной F05 media policy.
- `InheritanceStrategy` и `ColorFormat` отсутствуют в target.


## Режим доставки AssetReference после Demo

В интеграции с Digital Space Launcher специалист задаёт `DeliveryMode` в определении значения типа `AssetReference`: `OnDemand` (default), `Preload` либо `RequiredBeforeLaunch`. Owner — leaf `SchemaField` или recursive `ItemDefinition`, включая `List<AssetReference>` и вложенные Object/List; контейнеры и другие типы режима не имеют. Неизвестный mode и передача настройки к другому типу отклоняются при Save. При создании отсутствие настройки означает effective `OnDemand`; exact DTO/default encoding ещё [Уточняется Q37](../../technical/frontend/questions/Q37-launcher-asset-delivery.md).

`OnDemand` не запускает автоматическое предварительное скачивание; приложение обращается за файлом при потребности. `Preload` запускает предварительную загрузку без блокировки старта. `RequiredBeforeLaunch` требует verified bytes присутствующих ссылок до запуска соответствующего snapshot. Это свойство не заменяет `IsRequired`: пустое optional AssetReference не создаёт download job или launch blocker.

Published definition фиксирует режим неизменно; для другой политики нужна новая Draft/Published SchemaVersion. Create fields по-прежнему не содержат клиентского `SchemaFieldId`. Настройка не относится к MediaAsset, ProductVariant override или frozen пяти полям runtime AssetReference. Historical версии без metadata трактуются как `OnDemand` без изменения bytes/hash. Profile/versioned carrier и compatibility смены mode при сохранении FieldId открыты в Q37/Q34. Самостоятельное Demo не получает обязательную предзагрузку.

## Required и отсутствие значения


`IsRequired=true` означает наличие type-valid value, а не `value != CLR default`.


Допустимыми значениями являются `0`, `false`, zero TimeSpan, empty FlagEnum/List и `#00000000`. Для required String пустая строка не считается допустимым значением; для Text отсутствие полезного текста после HTML-очистки, включая разметку без текста, считается пустым значением.


Дополнительные constraints относятся к MVP по [Q35](../questions/Q35-value-constraints.md) и не расширяют Demo. Required коллекции само по себе не требует непустого массива; это отдельное MinItems для List.

## Настраиваемые ограничения MVP

Typed constraints принадлежат конкретному `SchemaValueDefinition` внутри snapshot SchemaVersion. List имеет собственные collection constraints и отдельную recursive Item definition с применимыми правилами. Published definitions immutable; внешнего изменяемого каталога правил нет.

| Тип | Первый набор constraints MVP |
| --- | --- |
| String | MinLength/MaxLength в Unicode code points в пределах intrinsic max 255, уточнённого в тех же единицах. Supplementary emoji = 1, combining mark считается отдельно; normalization отсутствует |
| Text | MinTextLength/MaxTextLength по текстовым узлам sanitized HTML после entity decoding, в Unicode code points. Узлы конкатенируются в document order без layout-пробелов/newlines: `<p>a</p><p>b</p>` = 2, `<br>` не добавляет символ; реальные текстовые пробелы учитываются |
| Integer / Float / Decimal | Inclusive Minimum/Maximum: long / finite double / decimal. UI preset «неотрицательное» задаёт Minimum=0, отдельного bool нет |
| Integer / Decimal | Positive MultipleOf с точной кратностью; без Decimal→double и overflow промежуточной арифметики |
| Date / DateTime / TimeSpan | Inclusive min/max; Offset DateTime bounds требуют offset и сравниваются по UTC instant, historical Naive — по прежней naive семантике. Без dynamic today/now, relative и cross-field expressions |
| List | Non-negative MinItems/MaxItems, min ≤ max; UniqueItems по полному runtime value и OrderMode по F04 |

Для Text sanitization выполняется до проверки и сохранения по F04, вне constraint validator. Intrinsic semantic-empty check, операционный предел сохранённого HTML и ограничения длины полезного текста — отдельные проверки.

OrderMode `Default` требует отсутствующего comparer. `Ascending`/`Descending` требуют одного supported comparer:

- `ItemValue`: Integer/Float/Decimal/Boolean/String/Enum/Date/DateTime/TimeSpan;
- `ObjectFieldPath` для List<Object>: один scalar key по непустому ordered path SchemaFieldId через Object, без переходов через List. Весь путь и scalar leaf required; leaf — один из типов ItemValue.

Numeric/temporal comparison типизированный; false < true; String и immutable Enum Key сравниваются ordinal по UTF-16 (.NET semantics), без culture/case folding/natural sort/Name. Direct Text/Color/FlagEnum/AssetReference/Object/List сравнение, optional/expression/composite keys и ItemKey comparer не входят в первый набор.

Validator не выполняет trim/coercion/default filling/clamp/rounding/dedup/sort. Отсутствующее optional значение пропускает constraints значения; IsRequired проверяется отдельно, intrinsic type rules сохраняются. Regex, Float MultipleOf, Decimal Precision/Scale и новые Enum/FlagEnum/Color/AssetReference constraints отложены за первый MVP набор.

## DateTime MVP

`FieldDataType.DateTime` остаётся одним из 15 типов. Его definition фиксирует immutable `TemporalSemantics = Naive|Offset`: исторические definitions остаются Naive, новые MVP DateTime fields используют Offset. Это семантика поля, а не редактируемый переключатель существующего поля. Смена Naive → Offset требует нового `SchemaFieldId` по F02; изменение обычных constraints нового ID не требует.

Offset DateTime обозначает конкретный момент: local date/time и известный UTC offset. Offset задаётся в целых минутах от −14:00 до +14:00; local date/time и соответствующий UTC instant должны укладываться в диапазон DateTimeOffset. Региональные timezone IDs, расписания и автоматические DST-переходы в первый MVP не входят. Date и TimeSpan сохраняют свой смысл; audit/lifecycle timestamps этим переходом не изменяются.

Minimum/Maximum Offset DateTime требуют offset; min ≤ max и inclusive value bounds проверяются по UTC instant. Разные offsets одного instant означают одинаковую границу; semantic schema hash нового profile записывает её в canonical UTC. Naive и Offset не смешиваются через скрытую conversion. Правила authoring precision/offset, runtime projection и List comparison описаны в F04; полное решение — [Q34](../questions/Q34-schema-field-semantics.md#datetime-mvp-temporal-semantics-и-совместимость).


## Минимально сохраняемый Draft


Для создания первого Draft нужны:


- Name и Key логической схемы;
- Title первого Draft;
- хотя бы одно структурно валидное поле;
- полные type definitions: Object child, List item definition, Enum/Flag options.

В MVP Save отклоняет противоречивые constraints (min > max, отрицательные lengths/counts, MultipleOf ≤ 0), несовместимость constraint с типом и invalid comparer/path. Ошибка не создаёт частичный Schema Draft. Эти правила распространяются и на recursive Item definitions.


Сохранение Draft и Publish имеют разные validation profiles; cross-version evolution rules проверяет F03.


## Result


Созданы:


- logical EntitySchema;
- первый ненумерованный Draft EntitySchemaVersion;
- серверные identities всех новых fields.


## Acceptance


- frontend создаёт поля без ID;
- два разных поля не могут иметь одинаковый key в одном дереве;
- invalid type definition не создаёт частичный результат;
- обязательные metadata длиной 1/255 после Trim допустимы, пустые/пробельные и 256 символов отклоняются; optional metadata длиной 1024 допустимы, 1025 отклоняются, blank сохраняется как null;
- все 15 типов проходят schema round-trip;
- результат остаётся Draft и не влияет на Delivery.
- lost response восстанавливается тем же OperationId и тем же нормализованным вводом: возвращается исходный результат создания; другой ввод с тем же OperationId отклоняется, новый Draft не появляется;
- readonly workspace открывает точный созданный Draft с server IDs, без перехода на другую/latest версию и без legacy Save/Publish действий F02/F03;
- MVP typed constraints и recursive Item definitions проходят round-trip; invalid constraint/comparer/path отклоняет Save атомарно;
- Unicode/String/Text length, exact Decimal и inclusive bounds имеют пограничные fixtures; advanced constraints не становятся зависимостью Demo.
- MVP DateTime definitions сохраняют immutable Naive/Offset и historical semantics; offset bounds покрывают equivalent instants, ±14:00 и local/UTC range overflow.
- После Demo AssetReference leaf/recursive ItemDefinition round-trip сохраняет выбранный mode; отсутствие при создании даёт OnDemand, неизвестный mode/настройка другого типа отклоняется; пустое optional значение не становится обязательным файлом. Published/history bytes остаются прежними; новый profile/carrier проверяется после решения Q37.


## Связи


[F02](F02-create-schema-version.md) · [F03](F03-publish-schema-version.md) · [F04](F04-entity-instances.md) · [Q08](../questions/Q08-keys-and-drafts.md) · [Q34](../questions/Q34-schema-field-semantics.md) · [Q35](../questions/Q35-value-constraints.md) · [Backend](../../technical/backend/features/F01-create-entity-schema.md) · [Frontend](../../technical/frontend/features/F01-create-entity-schema.md)
