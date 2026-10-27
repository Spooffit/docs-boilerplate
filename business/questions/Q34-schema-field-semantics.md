# Q34. Семантика типов полей и правила структуры схемы


## Вопрос и известные границы


Нужно определить семантику всех `FieldDataType`, общие правила `SchemaField`, минимальную допустимость Draft и строгую валидность Published. В Demo поддерживаются все 15 текущих типов: String, Text, Integer, Float, Decimal, Boolean, DateTime, Date, TimeSpan, Enum, FlagEnum, Color, Object, List, AssetReference.


Уже принято: ключи полей уникальны во всём дереве версии; новые field ID создаёт сервер; Draft схемы можно сохранить раньше полной публикационной готовности; Published неизменяем; Draft экземпляры разрешены по Draft-схеме, но не публикуются и не доставляются; клиент работает с точной опубликованной SchemaVersionId.


**Ответ:** Блоки 8 и 9 существенно закрыли семантику типов для Demo. Первый MVP набор typed constraints и comparer/order semantics согласован в [Q35](Q35-value-constraints.md); Demo не расширяется. Старые JSON Schema/C# `/1` profiles Q31 сохраняются; новые constraint definitions требуют отдельных profiles, exact bytes которых ещё уточняются. DateTimeOffset transition согласован в разделе «DateTime MVP: temporal semantics и совместимость»; whole new-profile bytes ещё материализуются Q19/Q31. Demo Text и historical field/container identity уточнены ниже.


## Затронутые фичи


[F01](../features/F01-create-entity-schema.md) · [F02](../features/F02-create-schema-version.md) · [F03](../features/F03-publish-schema-version.md) · [F04](../features/F04-entity-instances.md) · F05 для AssetReference · F21/F22 через технический Q31


## Блок 8. Поля схемы и FieldDataType


### 1. Что означает `IsRequired` и что считается отсутствующим значением?


**Ответ блока 9:** `IsRequired` означает, что значение **присутствует** и является допустимым значением своего типа. Оно больше не сравнивается с CLR/default value автоматически. `0`, `false`, нулевая длительность и прозрачный RGBA-цвет могут быть полноценными бизнесовыми значениями.


`DefaultValue` как отдельное schema-level свойство не вводится. Для Demo различаются «значение отсутствует» и «значение присутствует, но равно нулю/false/пустому набору». Типовая семантика:


| Тип | Отсутствие / недопустимое значение для required | Значение, которое **разрешено** и не считается отсутствием |
| --- | --- | --- |
| `String` | missing / `null` / `""` | любая непустая строка; базовый max 255 сохраняется |
| `Text` | missing / `null` / семантически пустой результат HTML-очистки | очищенная строка ограниченного HTML с полезным текстом в configured max |
| `Integer` | missing / `null` | `0` и другие допустимые integer |
| `Float` | missing / `null` / нечисловой JSON | `0` и другие finite значения |
| `Decimal` | missing / `null` | `0` и другие допустимые decimal |
| `Boolean` | missing / `null` | и `false`, и `true` |
| `DateTime` | missing / `null` / неверный формат | любое корректное значение выбранного Demo/MVP контракта |
| `Date` | missing / `null` / неверный формат | любая корректная календарная дата |
| `TimeSpan` | missing / `null` / неверный формат | в том числе нулевая и отрицательная длительность |
| `Enum` | missing / `null` / неизвестный key | любой объявленный key по правилам версии, включая deprecated |
| `FlagEnum` | missing / `null` | пустой набор keys допустим; новые FlagEnum constraints отложены за первый MVP набор Q35 |
| `Color` | missing / `null` / неверный RGBA | любой корректный `#RRGGBBAA`, включая `#00000000` |
| `Object` | missing / `null` | присутствующий object; required children валидируются рекурсивно |
| `List` | missing / `null` | пустой list допустим в Demo; MinItems относится к Q35 |
| `AssetReference` | missing / `null` / пустой либо невалидный `AssetId` | корректная ссылка на asset требуемого `AssetKind`; availability проверяется F04/F05 |


Для optional field отсутствие допустимо; новое присутствующее значение обязано соответствовать типу. Save instance Draft отдельно сохраняет ранее полученную несовместимость/orphan после schema changes по F04/Q36, но не принимает новый type-invalid ввод. Пустая String и семантически пустой Text после sanitization означают отсутствие полезного значения; если значения нет, свойство следует не задавать/передавать как отсутствие согласно wire-contract.


### 2. Является ли порядок полей частью схемы?


**Ответ:** Нет. Отдельное `SortOrder`/`Order` свойство не вводится. Технический порядок выдачи, сравнения и отображения по умолчанию стабилизируется сортировкой по `SchemaFieldId`.


Один и тот же детерминированный comparator должен использоваться там, где порядок влияет на canonical output, Registry/codegen и административное отображение. Конкретный клиент может визуализировать данные в собственном порядке.


### 3. Что является идентичностью одного логического поля между версиями схемы?


**Ответ:** Один стабильный `SchemaFieldId` сохраняется между версиями схемы, пока поле остаётся тем же логическим полем. Создание следующей версии на основе предыдущей переносит field identity вместе с допустимым снимком его определения.


Новый field всегда получает новый `SchemaFieldId`. Ранее удалённое поле можно вернуть с прежним ID, если совпадают Key/Type/смысл и цепочка definition-контейнеров Object/List. Required relaxation и Deprecated, включая options, проверяются монотонно по всей Published history этого ID; отсутствие в latest не обнуляет историю. Новое поле с похожим, но другим смыслом получает новый ID.


Persistence должен поддерживать присутствие одной stable field identity в нескольких schema-version snapshots. Точный физический mapping (отдельная логическая сущность + version definition, composite membership либо иной вариант) остаётся Q20; нельзя просто генерировать новый field ID при каждом копировании версии.


### 4. Что разрешено менять у существующего поля в новом Draft?


**Ответ:** Семантическая identity поля защищена. У field с существующим `SchemaFieldId` **нельзя** менять `Key`, `FieldDataType` и type-specific семантические параметры. Для другого key/type/смысла или другой цепочки definition-контейнеров Object/List старое поле удаляется из новой версии, а новое создаётся с новым ID. Визуальная перестановка и runtime List order не меняют identity.


Разрешены следующие изменения определения того же поля:


- `Name` — можно менять; это authoring metadata для специалиста, в runtime-контент клиенту не отправляется;
- `IsRequired`: только `true → false`; `false → true` запрещено для той же field identity;
- `Deprecated`: только `false → true`; возврат `true → false` запрещён;
- поле можно удалить из новой версии схемы;
- можно добавить новое поле с новым ID.
- в MVP можно усиливать/ослаблять constraints в пределах intrinsic типа без нового FieldId по Q35; это не отменяет immutable Key/Type/container identity и monotonic Required/Deprecated.


`Deprecated` добавляется в target-модель `SchemaField`. При переключении `IsRequired true → false` либо `Deprecated false → true` UI до сохранения показывает модальное предупреждение, что после публикации этой schema version обратный переход для той же field identity будет запрещён. Рядом с соответствующими настройками нужны поясняющие tooltip.


Также вводится optional authoring-подсказка поля (`HelpText` как рабочее имя модели): специалист может задать комментарий/подсказку, который показывается tooltip-ом при заполнении EntityInstance. Это authoring metadata, не runtime payload Delivery. HelpText можно свободно менять между версиями, как подтверждено в пункте 25.


### 5. Чем отличаются `String` и `Text`?


**Ответ:** Оба имеют строковое значение, но разную бизнесовую/UI-семантику.


- `String`: короткое значение, максимум **255 символов**, обычный однострочный input.
- `Text`: длинный текст для описаний в rich text editor. Хранится строка ограниченного HTML: абзацы, переносы строки, списки, жирное и курсивное выделение. Backend очищает HTML до сохранения и возвращает очищенный результат UI. Семантически пустой результат, включая разметку без текста, считается отсутствием значения.


Максимум Text явно задаётся backend configuration и передаётся UI. Числовой cap выбирается после проверки representative fixture; произвольный новый предел здесь не устанавливается. Validation выполняется по очищенному значению. Формат ограниченного HTML выбран; точная sanitizer/config реализация и проверка fixture относятся к техническому контракту.


### 6. Какие ограничения нужны числовым и другим типам?


**Ответ блока 9:** для **Demo** пользовательские дополнительные constraints не требуются. Число должно быть корректным значением своего базового типа; `0` является обычным допустимым значением. `Float` не принимает `NaN`/`Infinity` в JSON-контракте.


Первый MVP набор настроек согласован в [Q35](Q35-value-constraints.md): typed numeric bounds, exact Integer/Decimal MultipleOf, String/Text length, temporal bounds и List constraints/comparers. Regex, Float MultipleOf и Decimal Precision/Scale отложены за этот набор. Дополнительные constraints не являются обязательным объёмом Demo.


`Decimal` сохраняет точную десятичную семантику в runtime/codegen; configurable Precision/Scale отложены, rounding отсутствует. Q35 не вводит их молча в Demo.


### 7. Какая семантика у `DateTime`, `Date` и `TimeSpan`?


**Ответ:**


- `Date` — календарная дата без времени.
- `DateTime`: в **Demo** используется упрощённое представление без поддержки timezone/offset; точный wire-format должен совпадать на backend/UI/codegen. В **MVP** целевая семантика — эквивалент .NET `DateTimeOffset`: дата+время с offset, однозначно представляющие момент времени.
- `TimeSpan` — продолжительность с семантикой .NET `TimeSpan`; отрицательные значения разрешены.


Поддержка timezone/offset не должна незаметно попасть в Demo как обязательная функция. Переход к DateTimeOffset в MVP согласован в разделе «DateTime MVP: temporal semantics и совместимость» и требует новых profiles совместно с Q35; whole codec/fixtures материализуются Q19/Q31. Текущие Q19/Q31 `/1` фиксируют naive DateTime.


### 8. Как моделируются `Enum` и `FlagEnum`?


**Ответ блока 9:** обе модели options унифицируются.


`EnumOption`/`FlagEnumOption` в target имеют:


- immutable `Key` — стабильная identity option внутри стабильного field contract;
- mutable `Name` — authoring label для специалиста;
- monotonic `Deprecated` — только `false → true`.


Отдельный GUID `OptionId` не нужен: immutable option `Key` достаточно. Numeric `Value` и `BitValue` удаляются из target contract.


Эволюция option monotonic: существующий key нельзя переименовать или переиспользовать с другим смыслом; Name можно менять; новые options можно добавлять; option, от которого уходят, помечается Deprecated вместо повторного использования/переписывания старого key. Старые Published snapshots не меняются. Объявленные deprecated Enum/FlagEnum keys валидны для Save/Copy/Publish. UI предупреждает и скрывает их из обычного нового выбора; выбранные исторические значения остаются видимыми и сохраняются.


Runtime `Enum` хранит один string key. Runtime `FlagEnum` хранит JSON-массив/набор string keys, **не** bitmask. Порядок FlagEnum keys не имеет бизнес-смысла; canonical output сортирует их детерминированно по key.


### 9. Какую семантику имеет `Color`?


**Ответ блока 9:** target использует **один RGBA-контракт**. `ColorFormat` как выбираемое свойство SchemaField удаляется из target-модели.


Canonical wire representation для Demo/MVP — строка `#RRGGBBAA`: 8 hex-цифр после `#`, каналы red/green/blue/alpha. UI использует color picker и преобразует значение в этот единый contract.


`#00000000` является корректным прозрачным цветом, а не «отсутствием значения». Для required Color отсутствием считается только missing/null либо невалидный RGBA payload.


### 10. Как работает `Object`?


**Ответ:** `Object` не может быть пустым даже в сохранённом Draft: у него должен быть как минимум один child. Перед публикацией это правило повторно проверяется.


`Object` может содержать вложенный `Object` и `List`. Вложенные поля остаются частью одного дерева `SchemaVersionContent` и подчиняются глобальной уникальности field key по всему дереву.


### 11. Как работает `List`?


**Ответ блока 9:** List поддерживает рекурсивную item-definition. Разрешены scalar/value types, `Enum`, `FlagEnum`, `Color`, `AssetReference`, `Object` и **`List`**, поэтому `List<List<T>>` является допустимым target-сценарием.


Это означает, что одного плоского `ListItemType` недостаточно для target-модели. Нужна рекурсивная концепция item definition: item имеет Type и применимые настройки; `List<Object>` содержит child fields, а `List<List<T>>` содержит следующую item-definition. Точное имя C# type/DTO не фиксируется этим решением.


`List` имеет отдельную настройку порядка, рабочее имя `OrderMode`:


- `Default`;
- `Ascending`;
- `Descending`.


Для Demo достаточно хранить/показывать этот выбор и гарантировать корректный round-trip. `Default` сохраняет авторский порядок массива. В MVP Ascending/Descending проверяет stored authored order по supported comparer Q35, без autosort canonicalizer/Delivery/SDK; явное «Сортировать» стабильно меняет локальный Draft перед обычным Save.


Для `List<Object>` в Demo не вводится новый GUID item ID. Стабильная item identity при необходимости задаётся существующим `ItemKey`; deterministic traversal/diff по keyed items использует `ItemKey`. Поля внутри object-item по-прежнему упорядочиваются по `SchemaFieldId`. Для scalar list отдельная item identity не требуется.


`MinItems`, `MaxItems`, `UniqueItems` входят в согласованный первый MVP набор [Q35](Q35-value-constraints.md), но не расширяют Demo. UniqueItems использует полный normalized runtime value, а не ItemKey или comparer key.


### 12. Что именно означает `AssetReference`?


**Ответ:** `AssetReference` — ссылка на MediaLibrary asset с **обязательным `AssetKind` в definition**. В Demo допустимы `Image`, `Audio`, `Video`. Отдельные `FieldDataType.ImageReference`/`AudioReference`/`VideoReference` не вводятся: остаётся один `FieldDataType.AssetReference`, а kind является его type-specific setting.


Одно поле содержит одну ссылку; несколько ресурсов моделируются `List<AssetReference>`, причём recursive item definition также содержит свой обязательный kind. Field definition выбирает kind, но **не** собственный список extensions: поддерживаемые форматы задаёт централизованная backend policy F05/Q21.


Stable identity ссылки — `AssetId`. Клиентские read models и runtime SDK возвращают `AssetReference` как enriched descriptor/value object: `AssetId` плюс серверно подтверждённая metadata ресурса и CM-owned content URL. Persisted authoring identity остаётся AssetId; metadata формируется сервером и не является trusted write input, не создаёт вторую identity и не разрешает клиенту менять resource facts. Прямой S3 path/URL не является доменной ссылкой; bytes получаются через CM-owned F05/SDK boundary.


При strict Publish F04 проверяет существование/доступность asset и соответствие `AssetKind` definition. Picker на frontend заранее ограничивает кандидатов тем же kind, но server validation остаётся обязательной.


### 13. Нужен ли `InheritanceStrategy` как пользовательская возможность?


**Ответ блока 9:** нет. `InheritanceStrategy` удаляется из target `SchemaField`, API/DTO, Registry и административного UI.


Current `Replace/MergeObject/AppendCollection/MergeCollectionByKey` рассматриваются как наследие прежней product/product-version overlay-модели. Композиция Product/ProductVariant выполняется Publishing/materialized revisions; ContentModeling описывает форму и версии данных. Если в будущем появится отдельный бизнес-сценарий наследования instance content, он проектируется новой фичей, а не возвращается автоматически из current enum.


### 14. Какая type-specific неполнота допустима при сохранении Draft?


**Ответ блока 9:** definition поля должен быть структурно собран уже при `Save Draft`; Draft мягче Published прежде всего по данным экземпляров и cross-version/publish checks, а не позволяет сохранять бессмысленный half-configured field.


Минимальная матрица Save Draft:


- любой field: `Name`, `Key`, `Type`;
- `Object`: минимум один child;
- `List`: полностью заданная рекурсивная item-definition;
- `Enum`/`FlagEnum`: минимум одна option с корректным unique key;
- `Color`: дополнительных format-settings больше нет; Type уже однозначно означает RGBA;
- type-specific настройки, которые присутствуют, должны быть внутренне валидны уже при сохранении;
- пользовательские advanced constraints из Q35 не требуются в Demo.


Publish F03 повторяет structural validation и дополнительно проверяет whole-tree key uniqueness, stable field/option identity, monotonic Required/Deprecated/option Deprecated и остальные cross-version invariants.




## Сквозные следствия блока 8


1. `SchemaFieldId` — стабильная identity между версиями; key/type не являются identity и для существующего ID неизменяемы.
2. Published version остаётся snapshot: разрешённые изменения field definition происходят только в новом Draft и не переписывают старую версию.
3. При удалении field старые Published schema/instance остаются читаемыми; новый field с новым смыслом получает новый ID.
4. `Name` и authoring-help не являются runtime content properties. Delivery ориентируется на опубликованный schema contract, keys и IDs по формальному контракту F21/Q31.
5. Сортировка по `SchemaFieldId` является deterministic default; отдельный order index не нужен.
6. `IsRequired` проверяет присутствие и type-validity, а не запрещает CLR/default автоматически: `0`, `false`, zero TimeSpan, empty FlagEnum/List и `#00000000` могут быть валидными значениями по типу.
7. UI схемы должен содержать подготовленные системные tooltips по типам/настройкам и показывать user `HelpText` в редакторе экземпляров.
8. Enum/FlagEnum options используют immutable key + mutable Name + monotonic Deprecated; numeric values/bitmask удаляются из target.
9. Color имеет один canonical RGBA `#RRGGBBAA`; `ColorFormat` удаляется.
10. List item-definition рекурсивна и допускает List<List<T>>; OrderMode = Default/Ascending/Descending, полный comparator относится к Q35/MVP.
11. `InheritanceStrategy` удаляется из target ContentModeling.


## Применение ответов


Разнести правила в F01 (модель/constraints/валидация), F02 (stable field identity и разрешённые изменения), F03 (publish validation), F04 (required/default values и instance UI), F05 (AssetReference), frontend M01 и backend M01. Формальное отражение в JSON Schema/C# обновить через Q31/F21/F22. Блок 9 закрывает прежние заглушки Enum/FlagEnum/Color/List/InheritanceStrategy/Draft-validity для Demo. MVP constraints и comparer/order semantics согласованы в Q35; текущий wire/codegen `/1` Q31 сохраняется, exact new-profile bytes ещё уточняются.


## Current-state implementation evidence 01.10.2026


Источник: [current-state code snapshot](../../archive/current-state-code-snapshot-2026-10-01.md). Следующее **не является ответом на оставшиеся target-заглушки**, а только фиксирует существующий backend/frontend.


- Current `Enum` option: Key + Name + numeric `int Value`; frontend требует unique keys и unique values.
- Current `FlagEnum`: Key + Name + power-of-two `long BitValue`; runtime instance хранит numeric bitmask. Это расходится с уже принятым target `FlagEnum = набор keys`.
- Current `ColorFormat`: `Rgb`, `Rgba`, `Argb`, `Hex`; instance value для RGB-like форматов — byte array, Hex — `#RRGGBB/#RRGGBBAA`.
- Current `List<List<T>>` запрещён; `List<Object>` поддержан и имеет child fields. Это current restriction, а не выбранный target для Q34.
- Current `InheritanceStrategy`: Replace/MergeObject/AppendCollection/MergeCollectionByKey и показывается в admin UI. Это current legacy/feature evidence; блок 9 после snapshot принял решение удалить InheritanceStrategy из target.
- Current field shape не имеет `Deprecated`, `HelpText`, numeric constraints target-модели.
- Current editor позволяет менять Key, Required и type-specific options под тем же ID; Type existing field read-only. Это расходится с target immutable Key/type-specific semantics и monotonic Required/Deprecated.
- Current backend/frontend duplicate field-key validation выполняется на sibling level, а не глобально по дереву; target whole-tree uniqueness требует изменения.


Таким образом, snapshot помогает понять цену рефакторинга и текущие UI controls. Блок 9 ниже закрывает target Enum/FlagEnum, Color, List, InheritanceStrategy и Draft matrix для Demo; расширяемые constraints/comparator вынесены в Q35.


## Блок 9. Enum, FlagEnum, Color, List, InheritanceStrategy и Draft-validity


Ниже вопросы следующего уточнения. Current-state из блока выше используется только как пример существующего поведения; он не закрывает ответы автоматически.


### 15. Как выглядит target-модель `Enum` option?


Current implementation хранит `Key + Name + int Value`, но числовой `Value` нужен только если у него есть отдельная бизнес-семантика. Нужно выбрать target:


- `Key + Name`, runtime value = стабильный string key;
- либо `Key + Name + numeric Value`, где numeric Value также является частью внешнего контракта.


Отдельно нужно определить, является ли `Name` только authoring label и отправляется ли он runtime-клиенту.


**Ответ:** `Key + Name`. Numeric `Value` удаляется. Runtime `Enum` value — immutable string key. `Name` является authoring label и не требуется runtime payload как identity.


### 16. Как развивается `Enum` между schema versions?


Для одного стабильного field ID нужно определить допустимые изменения options. Возможная monotonic-модель: существующий option key нельзя переименовать или переиспользовать с другим смыслом; `Name` можно менять; новые options можно добавлять; удаление выполняется через `Deprecated` option либо полное исключение только с новым field ID.


Нужно решить, требуется ли отдельная стабильная `EnumOptionId`, или immutable option `Key` достаточно для identity внутри одного immutable field contract.


**Ответ:** Используется monotonic-модель без отдельного `EnumOptionId`: immutable option `Key` является identity внутри immutable field. `Name` можно менять, options можно добавлять, а отказ от option выражается `Deprecated false → true`. Key нельзя переименовать/переиспользовать с новым смыслом.


### 17. Как выглядит target-модель `FlagEnum`?


Уже принято: runtime value — набор keys, не bitmask. Нужно определить definition options: совпадает ли target с Enum (`Key + Name`, без `BitValue`) и сериализуется ли instance value как JSON array строковых keys.


Также нужны правила эволюции options между версиями: add/rename/remove/deprecate.


**Ответ:** `FlagEnum` использует ту же option-модель `Key + Name + Deprecated`, что и Enum. Numeric `BitValue` отсутствует. Runtime value — набор/JSON array string keys; bitmask не используется.


### 18. Какой target-контракт у `Color`?


Current implementation имеет четыре ColorFormat и несколько wire-shape. Это создаёт разные формы одного типа. Нужно решить, оставляем ли selectable format либо вводим один канонический runtime contract.


Рабочее предложение для упрощения: один RGBA-контракт с alpha, одна JSON-форма и color picker в UI; формат отображения пользователю не становится свойством schema field. Если нужен HEX/RGB/ARGB как именно разные бизнесовые форматы, это нужно подтвердить отдельно.


Также нужно определить default value для required-validation Color.


**Ответ:** Один canonical RGBA. `ColorFormat` удаляется. Wire value — `#RRGGBBAA`; alpha поддерживается. `#00000000` — валидный цвет, не отсутствие значения.


### 19. Какие item types разрешает `List`?


Нужно определить target matrix. Кандидат: List поддерживает все scalar/value types, `Enum`, `FlagEnum`, `Color`, `AssetReference` и `Object`, но не поддерживает `List` как item type, то есть `List<List<T>>` запрещён.


`List<Object>` описывает children непосредственно в definition этого list field, а не ссылается на другую EntitySchema, если отдельно не будет принято иное правило.


**Ответ:** `List` допускает все применимые item types, включая `Object` и **`List`**, поэтому `List<List<T>>` поддерживается. Target item-definition рекурсивна; `List<Object>` содержит child fields внутри item-definition, а не отдельную EntitySchema.


### 20. Является ли порядок элементов `List` частью значения?


Порядок SchemaField не является бизнесовой частью схемы, но для list-value ситуация другая. Нужно решить: `List` — ordered collection, где `[A,B]` отличается от `[B,A]`, либо unordered set-like collection.


Current UI позволяет менять порядок items, но это только evidence. Если order значим, он должен сохраняться в instance snapshot, hash и Delivery payload.


**Ответ:** У List появляется schema-level `OrderMode`: `Default`, `Ascending`, `Descending`. `Default` сохраняет authored array order. Для Demo достаточно выбора/сохранения режима. В MVP supported comparer, strict order validation и stable local «Сортировать» согласованы Q35; equal keys разрешены без ItemKey tie-break, automatic reorder отсутствует.


### 21. Нужна ли item identity внутри `List<Object>`?


Для редактирования, diff и будущего merge можно дать каждому element стабильный `ItemKey`/ID. Для простой snapshot-модели это необязательно: вся list version является immutable содержимым конкретной EntityInstanceVersion.


Нужно определить, является ли item identity бизнесовым контрактом клиента либо только внутренним editor detail. Current instance model содержит `ItemKey`, но это не делает его target автоматически.


**Ответ:** Для `List<Object>` не вводим отдельный GUID ItemId в Demo. Используется стабильный `ItemKey` для deterministic keyed traversal/diff; это не runtime equality и не перестановка authored массива. Поля внутри item-object упорядочиваются по `SchemaFieldId`. Scalar list отдельной item identity не требует. MVP Q35 исключает ItemKey из UniqueItems и comparer/tie-break.


### 22. Нужны ли constraints коллекции?


Нужны ли на schema field `MinItems`, `MaxItems`, `UniqueItems`, либо в Demo/MVP достаточно типа элемента и required/default semantics? Не назначать случайные лимиты без бизнес-потребности.


**Ответ:** `MinItems`, `MaxItems`, `UniqueItems` согласованы для первого MVP набора в [Q35](Q35-value-constraints.md), но **не входят в Demo**. Полные equality/order и Save/Publish правила принадлежат Q35/F04; расширения требуют отдельного решения.


### 23. Что делать с `InheritanceStrategy` в target?


Current enum появился для merge product-level и product-version content. После выделения Publishing и materialized revisions нужно подтвердить, остаётся ли это свойство частью ContentModeling.


Рабочее предложение: удалить `InheritanceStrategy` из target SchemaField и не показывать его в Demo UI; композиция product/variant content выполняется Publishing, а EntitySchema описывает только форму данных. Если остаётся реальный отдельный бизнес-сценарий наследования instance content, его нужно сформулировать до сохранения enum.


**Ответ:** `InheritanceStrategy` удаляется из target SchemaField/API/UI/Registry. Current enum остаётся current-state evidence старой overlay-модели; product/variant composition принадлежит Publishing.


### 24. Какая минимальная валидность field нужна для `Save Draft`?


Нужно закрыть матрицу Q34. Рабочее предложение:


- любой field требует `Name`, `Key`, `Type`;
- `Object` требует минимум одного child уже для Save Draft, это ранее принято;
- `List` требует выбранный item type уже для Save Draft;
- `Enum`/`FlagEnum` требуют минимум один option уже для Save Draft;
- `Color` требует все настройки только если в target вообще остаются настройки ColorFormat;
- numeric constraints, если заданы, должны быть внутренне непротиворечивы уже при Save Draft;
- Publish повторно выполняет все проверки и добавляет полный strict profile.


Нужно подтвердить либо ослабить эту матрицу.


**Ответ:** Рабочая матрица подтверждена: каждый field требует Name/Key/Type; Object — минимум одного child; List — полную рекурсивную item-definition; Enum/FlagEnum — минимум одну корректную option; Color не имеет дополнительных format settings; заданные настройки обязаны быть внутренне валидны уже при Save Draft. Publish выполняет более строгие cross-version проверки.


### 25. Можно ли менять `HelpText` между версиями того же field?


`HelpText` не входит в runtime payload и служит только специалисту. Рабочее предложение: разрешить менять его свободно в новом Draft того же `SchemaFieldId`, как `Name`, потому что это authoring metadata и не меняет клиентский контракт.


**Ответ:** Да. `HelpText` можно свободно менять в новом Draft того же `SchemaFieldId`, как `Name`. Это authoring metadata и не меняет runtime client contract.


### 26. Как `Deprecated` влияет на instance editor и client contract?


Нужно определить поведение deprecated field:


- остаётся ли оно в JSON Schema/codegen и runtime payload;
- показывается ли оно при создании нового instance по умолчанию;
- можно ли вводить новое значение в deprecated field;
- может ли `Deprecated=true` coexist with `IsRequired=true`, либо при deprecation required должен одновременно стать false;
- когда поле следует физически исключать из следующей schema version.


Рабочее направление: deprecated сохраняется в контракте для совместимости, но перестаёт быть required и UI предупреждает специалиста не использовать его для новых данных.


**Ответ:** Deprecated field остаётся частью Published schema, Registry и runtime contract до явного удаления в будущей schema version. При `Deprecated false → true` поле обязано стать `IsRequired=false`; сочетание Deprecated+Required невалидно. Instance editor визуально помечает/деакцентирует поле, но значение остаётся доступным для ввода при необходимости. C# codegen обязан помечать generated property атрибутом `[Obsolete]`. Deprecated является migration signal, а не автоматическим удалением данных.


### 27. Что считается default для `Enum`, `FlagEnum` и `Color` после выбора их target-модели?


Прежнее общее правило «default запрещён» пересмотрено: Required означает presence/type-validity. Нужно было окончательно определить absence/value semantics для Enum/FlagEnum/Color после выбора runtime shapes.


**Ответ:** Отказываемся от общего правила «default value запрещён». Required означает присутствие допустимого значения. Enum: отсутствующий/неизвестный key невалиден, объявленный key валиден. FlagEnum: присутствующий пустой набор keys валиден. Color: корректный RGBA, включая `#00000000`, валиден. По остальным типам используется таблица из пункта 1. Настраиваемые специальные ограничения относятся к Q35/MVP.


## F03: возвращение исторического поля и положение в дереве


Связано с [F03](../features/F03-publish-schema-version.md), [backend F03](../../technical/backend/features/F03-publish-schema-version.md), [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#f03-детализация-целевого-контракта). Stable FieldId/immutable Key/Type и разрешённое отсутствие removed field остаются принятыми.


| Вопрос | Известная граница | Что ещё выбрать |
| --- | --- | --- |
| Возвращение field ID, отсутствующего в latest Published | Copy старой Published сохраняет IDs, Accept current не merge; история immutable | **Ответ:** разрешено вернуть прежний FieldId с теми же Key/Type/смыслом и container chain. Required relaxation и Deprecated/options проверяются по всей Published history ID; нельзя отменить ни одно ранее опубликованное monotonic изменение. Новый смысл требует нового ID. |
| Container/parent semantics existing ID между Published versions | List.Item type semantic identity immutable | **Ответ:** цепочка definition-контейнеров Object/List входит в semantic identity; перенос поля в иной контейнер требует нового FieldId. Визуальная перестановка полей и перестановка элементов runtime List не меняют identity; mutable authoring Name/HelpText сами по себе не являются новым container/contract. |


Технический identity-history lookup относится к Q15. Отсутствие ID в текущей базе само по себе не доказывает, что это новое поле; решением не вводятся merge/alias/автоматический перенос instance values.

## DeliveryMode и идентичность поля

В [F01](../features/F01-create-entity-schema.md#режим-доставки-assetreference-после-demo) принято, что AssetReference leaf definition (включая recursive ItemDefinition) имеет DeliveryMode OnDemand/Preload/RequiredBeforeLaunch; Published mode immutable и смена требует новой версии схемы. Режим не меняет форму instance AssetReference и не добавляется в его frozen пять полей.

**Уточняется:** разрешено ли в новом Draft менять только DeliveryMode при сохранении прежнего SchemaFieldId и той же Key/Type/AssetKind/container semantic identity? Приложенный источник предлагает такой compatibility rule, но окончательного решения по смысловой идентичности поля нет. Предложение не является разрешённым F02/F03 evolution rule. Serialization/profile и остальные Launcher integration детали — [Q37](../../technical/frontend/questions/Q37-launcher-asset-delivery.md).

## DateTime MVP: temporal semantics и совместимость

**Ответ:** Пользователь согласовал все 18 пунктов. Это target specification, не отчёт реализации. Владелец definitions и моделей — [F01 backend](../../technical/backend/features/F01-create-entity-schema.md#21-aggregates-entities-и-value-objects); typed values/codec/Publish projection — [F04 backend](../../technical/backend/features/F04-entity-instances.md).

1. **Смысл.** Offset DateTime обозначает конкретный момент времени: дата/время плюс известный UTC offset. Domain/generated C# value type — DateTimeOffset.
2. **Offset без timezone.** Региональные IANA/Windows zone IDs, расписания и автоматические DST-переходы в первый MVP не входят. +03:00 не выбирает регион.
3. **Диапазон.** Offset от −14:00 до +14:00, whole minutes. Local date и corresponding UTC instant должны быть в диапазоне DateTimeOffset; overflow до/после conversion invalid.
4. **Wire.** Offset authoring contract принимает только `yyyy-MM-dd'T'HH:mm:ss.fffffffzzz` либо UTC `yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'`: ровно семь fractional digits, invariant culture, обязательный offset. Без offset, leap second и -00:00 rejected. Naive contract /1 остаётся прежним.
5. **Definition.** DateTimeValueDefinition имеет immutable TemporalSemantics=Naive/Offset. FieldDataType.DateTime остаётся одним из 15 типов. New MVP fields используют Offset, historical definitions — Naive. Это fixed semantic definition, не mutable UI selector существующего поля.
6. **Identity.** Смена Naive→Offset требует нового FieldId; историческое поле не переинтерпретируется. Обычная смена constraints сама по себе не меняет identity Q35.
7. **Authoring.** Save/read сохраняет введённый offset: 12:00 +03:00 возвращается как 12:00 +03:00, без server/browser timezone conversion.
8. **Runtime / canonical.** Publish Offset value сериализует UTC `yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'`. 12:00 +03:00 и 09:00 Z дают same runtime bytes/hash при остальном равном. Original offset остаётся в authoring snapshot, не runtime identity. Это declared runtime projection нового profile, не coercion validator.
9. **Persistence.** Dynamic authoring value хранится в existing JSONB snapshot с offset/7digits; exact Published canonical bytes отдельно атомарно profile+digest по Q19. Не отдельная SQL timestamp-колонка каждого dynamic value; loss of 100ns запрещён.
10. **Copy / migration.** Naive не auto-convert по server/user timezone. Cross-version Copy только по stable FieldId: removed old value — orphan, новое Offset field — unfilled. В первом MVP specialist вручную переносит value и задаёт offset; fuzzy/name/key mapping/migration engine не вводится.
11. **History / C#.** Historical Naive exact read/generation остаются DateTime Kind=Unspecified; Offset — DateTimeOffset. Несколько exact versions/semantics могут жить одновременно в одном generated client project.
12. **Equality.** Offset values равны и сравниваются по UTC instant. Different offsets same instant equal; offset не secondary key.
13. **Constraints.** Inclusive Minimum/Maximum требуют offset и сравниваются по UTC instant. Semantic schema hash нового profile пишет Offset bounds в UTC; equivalent instant bound offsets не меняют смысл/hash. Naive bounds используют прежнюю naive семантику.
14. **List.** UniqueItems считает same instant разных offsets duplicate; Asc/Desc comparator считает keys equal, stable sort сохраняет authored tie order. Naive и Offset не сравниваются через скрытую conversion.
15. **Editor.** Date/time и offset отдельные controls; initial new Offset value имеет visible +00:00, specialist может изменить. Показать derived UTC instant. Смена offset сохраняет введённые date/time и меняет instant; UI объясняет. Browser timezone binding отсутствует.
16. **Precision / read-only.** Открытие формы/редактирование других fields не обнуляет fractional seconds; есть явный fractional edit. Published runtime view показывает canonical UTC, original offset доступен в authoring view.
17. **Profiles.** Первый новый MVP profile set совместно включает Q35 constraints и Offset: новые schema-content, instance-content, JSON Schema и C# profiles. Runtime DateTime bytes меняются, поэтому instance /1 недостаточен для Offset. Old /1 сохраняются. Group/inventory/manifest profiles меняются только при собственном layout/semantics change, с existing content-profile identifiers. Registry Offset = format:date-time + CM precision/range checks. Exact whole new-profile codec/extension carriers/literal golden bytes ещё **Уточняется** Q19/Q31; названия /2 здесь не выдаются за реализованный codec.
18. **Layers / acceptance.** F01/F02 own immutable definitions; F04 parsing/authoring/Publish UTC projection; F03/Q19 canonical; F21/F22 formal/generated; F19 supported-profile read/integrity. Shared pure temporal codec с explicit parse/format и без host timezone. Fixtures: offset date rollover, ±14:00/range overflow, 100ns precision, equivalent instants, bounds, unique/ties, Copy/orphan и old/new generated versions together. Code/tests/Player checks не объявляются выполненными по документу.

### Граница решения

Date и TimeSpan сохраняют текущий смысл. Audit/lifecycle/system timestamps не относятся к миграции dynamic DateTime fields. Protocol/new-profile codec materialization, OpenAPI/NSwag temporal union и SDK signatures остаются engineering Q15/Q19/Q31/Q29; semantic timezone/offset выбор здесь закрыт.

Primary references: [DateTimeOffset constructors](https://learn.microsoft.com/en-us/dotnet/api/system.datetimeoffset.-ctor?view=net-10.0), [Equals](https://learn.microsoft.com/en-us/dotnet/api/system.datetimeoffset.equals?view=net-10.0), [RFC3339 unknown offset](https://www.rfc-editor.org/rfc/rfc3339#section-4.3).
