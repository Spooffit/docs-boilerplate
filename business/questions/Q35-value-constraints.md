# Q35. Настраиваемые ограничения значений и сравнение List

## Вопрос и ответ

Какие дополнительные ограничения входят в первый MVP, как проверяются Draft/Published instances и что означает порядок List?

**Ответ:** Согласованы все 24 пункта ниже. Это целевая спецификация MVP, не подтверждение реализации. Demo сохраняет базовые правила Q34 без конструктора дополнительных constraints.

## Владение, Save, Publish и Copy

1. **Этап.** Дополнительные constraints относятся к MVP. Current controls не расширяют Demo.
2. **Владелец.** Typed constraints входят в SchemaValueDefinition конкретного SchemaVersion snapshot. List имеет собственные collection constraints и отдельный recursive Item definition с применимыми constraints. Published definitions immutable; внешнего изменяемого каталога правил нет.
3. **Без исправлений.** Validator не выполняет trim/coercion/default filling/clamp/rounding/dedup/sort. Отсутствующее optional значение пропускает constraints значения; IsRequired проверяется отдельно. Intrinsic type rules сохраняются.
4. **Schema Draft.** Save отклоняет min > max, отрицательные lengths/counts, MultipleOf ≤ 0, несовместимый с типом constraint и invalid comparer/path. Publish повторяет проверки всех definitions и evolution rules.
5. **Instance Draft.** Новое present type-invalid значение отклоняет Save по F04. Type-valid значение, нарушающее дополнительный constraint, сохраняется с адресуемой диагностикой, блокирующей Publish; одинаково для новых и сохранённых values. Неполный нечисловой ввод остаётся локально в UI.
6. **Instance Publish.** Строгая проверка presence/types/nested values/constraints/uniqueness/order. Любое нарушение блокирует весь Publish без partial result и исправления values.
7. **Evolution / Copy.** В новой SchemaVersion constraints можно усиливать/ослаблять в пределах intrinsic типа без нового FieldId. Key/Type/container identity и monotonic Required/Deprecated остаются F02/F03. Copy переносит type-compatible value по stable FieldId; нарушение target constraint даёт issue, но само по себе не делает value orphan. Published history не меняется.

## String, Text, числа и время

8. **String.** MinLength/MaxLength измеряются в Unicode code points, не UTF-16 units/графемах. Supplementary emoji = 1; combining mark отдельный; normalization отсутствует. Intrinsic max 255 уточняется в тех же единицах; configured rules только сужают intrinsic область.
9. **Text.** MinTextLength/MaxTextLength измеряют текстовые узлы sanitized HTML после entity decoding в Unicode code points. Конкатенация в document order без layout-пробелов/newlines: `<p>a</p><p>b</p>` имеет длину 2; `<br>` не добавляет символ. Реальные текстовые пробелы учитываются; intrinsic semantic-empty check отдельный. Операционный предел сохранённого HTML и полезного текста — разные проверки. Sanitization выполняется до проверки/сохранения по F04, не внутри constraint validator.
10. **Regex.** Пользовательские pattern/regex отложены за первый набор MVP; не выбирается неявный .NET/JavaScript dialect/execution policy.
11. **Numeric ranges.** Inclusive Minimum/Maximum: Integer — long, Float — finite double, Decimal — decimal. Non-negative UI preset = Minimum=0, отдельного bool нет.
12. **MultipleOf.** Integer/Decimal positive step и точная кратность; без Decimal→double и overflow промежуточной арифметики. Float MultipleOf и Decimal Precision/Scale отложены; rounding отсутствует.
13. **Temporal bounds.** Inclusive min/max Date/DateTime/TimeSpan в действующем profile значения. Dynamic today/now/relative/cross-field expressions отсутствуют. Q34 согласовал immutable Naive/Offset semantics, exact Offset wire, authored offset и UTC runtime/bounds/equality. Offset требует новых profiles совместно с constraints; `/1` остаётся Naive. Whole codec/fixtures Q19/Q31 ещё материализуются, temporal product choice закрыт.

## List: размер, равенство и порядок

14. **Размер.** MinItems/MaxItems non-negative integers, min ≤ max. Empty List допустим без запрещающего MinItems. Required коллекции не означает непустой массив.
15. **UniqueItems.** Full type-aware normalized runtime value equality по Q19. Object fields сравниваются по FieldId, absent отличается от present default; FlagEnum — set keys, nested Lists — с учётом порядка. ItemKey/orphan/authoring metadata исключены. Равные Object с разными ItemKey — duplicates.
16. **Equality implementation.** Hash индексирует кандидатов, совпадение подтверждается full normalized values/bytes. Invalid/incomplete items дают issues; успешная uniqueness proof для непроверенных values запрещена. Partial Draft сохраняется, Publish требует валидный результат.
17. **OrderMode.** Ascending/Descending — требование к stored authored array order, не автоматическая сортировка. Нарушение Save допускается как issue, Publish запрещается. Canonicalizer/Delivery/SDK элементы не переставляют.
18. **UI action.** Явное «Сортировать» стабильно переставляет локальный editable Draft, затем обычный Save с исходным ExpectedVersion/If-Match. Published read-only. Отдельный server mutation/route не нужен.
19. **ItemValue comparer.** Integer/Float/Decimal/Boolean/String/Enum/Date/DateTime/TimeSpan. Numeric/temporal comparison типизированный; false < true; String/immutable Enum Key — ordinal (UTF-16 ordinal .NET semantics), без culture/case folding/natural sort/Name.
20. **ObjectFieldPath comparer.** Для List<Object> один scalar key по непустому ordered path SchemaFieldId; переходы через Object, не List. Весь путь и scalar leaf required. Leaf только из supported типов пункта 19. Name/ItemKey/expression/composite key отсутствуют.
21. **Граница comparer.** Direct Text/Color/FlagEnum/AssetReference/Object/List сравнение отложено. Default требует отсутствующего comparer; Ascending/Descending — supported comparer. Invalid definition/path отклоняет Schema Save. Missing/invalid key в partial Instance Draft даёт issue, sort unavailable и Publish blocked; null/default substitution нет.
22. **Equal keys.** Разрешены без implicit Guid/ItemKey tie-break; stable sort сохраняет взаимный порядок. UniqueItems проверяет full value, не только comparer key.

## Модели, слои, экспорт и проверка

23. **Реализация.** Typed value constraints и ListComparerDefinition(ItemValue/ObjectFieldPath) описаны один раз в [F01 backend](../../technical/backend/features/F01-create-entity-schema.md#21-aggregates-entities-и-value-objects). Domain владеет immutable definitions/guards; Application.Abstractions — transport definitions; Application применяет validators при Create/Save/Copy/Publish; EntityFramework/Persistence сохраняют полный snapshot атомарно; Api передаёт typed DTO и существующие path diagnostics. Reusable ValueConstraintValidator/RuntimeValueEquality/ListOrderValidator/StableListComparer не получают DbContext/Domain aggregates в input. Instance behavior — F04, frontend использует generated DTO без копий business models.
24. **Profiles / acceptance.** Новые definitions требуют нового cm-schema-content и cm-json-schema profiles; старые `/1` bytes не меняются, historical rehash/re-export под новой семантикой отсутствует. Новый C# profile требуется при изменении generated bytes/metadata. cm-instance-content/1 сохраняется для definition-only extensions с прежним runtime encoding; согласованный Q34 Offset меняет encoding и требует нового instance profile. Явный reorder меняет value/hash обычным образом. Standard JSON Schema keywords только при эквивалентной семантике; Text length/temporal bounds/comparer/особое semantic equality — CM extensions + CM validator. Generic validator не доказывает extensions. Constraints-only C# metadata не меняет CLR types/не вводит hidden setter validation/coercion; Q34 temporal semantics отдельно требует DateTime(Unspecified) для Naive и DateTimeOffset для Offset. Fixtures: Unicode/exact Decimal/bounds/Object duplicates разных ItemKey/ties/recursive definitions/Save-Publish-Copy/old-new profiles. Exact byte layouts новых profiles отдельно **Уточняются** до golden fixtures; Q35 не выдаёт готовый `/2` codec.

## Отложенные расширения и связанные вопросы

Regex, Float MultipleOf, Decimal Precision/Scale, expression/composite/optional-key comparers и новые Enum/FlagEnum/Color/AssetReference constraints не входят в первый MVP набор. Расширение требует отдельного решения. Temporal offset semantics согласованы [Q34](Q34-schema-field-semantics.md#datetime-mvp-temporal-semantics-и-совместимость); exact whole new-profile bytes/materialization остаются открытыми Q19/Q31. Базовый набор Q35 согласован.

[Q34](Q34-schema-field-semantics.md) · [Q19](../../technical/backend/questions/Q19-content-hashes.md) · [Q31](../../technical/backend/questions/Q31-schema-code-generation.md). Standard keywords: [JSON Schema Validation 2020-12](https://json-schema.org/draft/2020-12/json-schema-validation).

## Применение

[F01](../features/F01-create-entity-schema.md) · [F02](../features/F02-create-schema-version.md) · [F03](../features/F03-publish-schema-version.md) · [F04](../features/F04-entity-instances.md) · [F21](../features/F21-schema-registry.md) · [F22](../features/F22-csharp-model-generation.md). Schema editor — frontend F01/F02; Instance editor/sort — frontend F04. Не добавлять constraints в Demo ради current controls.
