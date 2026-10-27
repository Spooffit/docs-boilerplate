# F04. Экземпляры и версии данных


## Цель


Создавать логические EntityInstance и независимо версионировать их данные по точным SchemaVersion, сохраняя Published history и позволяя заранее готовить Draft под Draft-схему.


## Logical EntityInstance


Содержит:


- stable `EntityInstanceId`;
- stable immutable `EntityInstanceKey`, unique внутри EntitySchema;
- mutable authoring `Name`;
- ссылку на logical EntitySchema.


Demo: Key вводится явно. MVP UI может предложить редактируемый key из Name до создания, но после create key immutable.


## Version model


EntityInstance может иметь несколько Draft одновременно, в том числе по разным SchemaVersionId.


Draft:


- имеет technical ID;
- business number отсутствует;
- target `SchemaVersionId` immutable после создания;
- может ссылаться на Draft или Published SchemaVersion;
- может содержать incomplete/orphan authoring data.


Published:


- получает следующий integer `VersionNumber = 1,2,3...`;
- sequence одна на весь EntityInstance независимо от SchemaVersionId;
- immutable после Publish.


## Создание Draft


Новый Draft может быть:


- clean;
- скопирован из Draft или Published instance version того же EntityInstance.


При copy можно выбрать другую SchemaVersion той же EntitySchema.


Перенос выполняется только по stable `SchemaFieldId`:


- совпавшие совместимые fields получают source value;
- в MVP type-compatible value переносится и при нарушении target constraint: возвращается адресуемый issue, но одно нарушение constraint не делает value orphan;
- source values отсутствующих target fields сохраняются как orphan;
- новые target fields остаются незаполненными;
- matching по Name/Key/позиции и fuzzy migration запрещены.


Orphan можно удалить/исправить вручную; до устранения Publish запрещён.

В MVP переход DateTime Naive → Offset требует нового FieldId F01/F02. Copy не угадывает offset по server/user/browser timezone: удалённое Naive value остаётся orphan, новое Offset field — незаполненным. Специалист вручную переносит date/time и явно задаёт offset; matching по Key/Name и migration engine не вводятся.


## Значения и validation


Save Draft разрешает missing required, ранее сохранённые значения, ставшие несовместимыми после изменения target schema, и orphan authoring data. Эти данные сохраняются и возвращаются с адресуемой диагностикой для ручного исправления. Новое присутствующее type-invalid значение отклоняет Save с указанием поля/пути; промежуточный нечисловой ввод остаётся локальным состоянием UI. Text очищается до сохранения по F01; семантическая пустота и максимум проверяются по очищенному результату.

В MVP type-valid значение, нарушающее дополнительные constraints F01/Q35, сохраняется с адресуемой диагностикой, блокирующей Publish. Правило одинаково для новых и уже сохранённых values; отличие от нового intrinsic type-invalid ввода сохраняется. Отсутствующее optional значение пропускает constraints значения; IsRequired проверяется отдельно. Проверка не делает trim/coercion/default filling/clamp/rounding/dedup/sort. Text sanitization выполняется до validator и Save, а не как скрытое исправление constraint.


На Publish:


- target SchemaVersion должна быть Published и допустима;
- orphan отсутствуют;
- required = value present + type-valid;
- `0`, `false`, zero TimeSpan, empty FlagEnum/List, `#00000000` допустимы;
- required String не принимает пустую строку; required Text не принимает семантически пустое значение после HTML-очистки;
- Enum = option key; FlagEnum = set of keys; объявленные deprecated keys валидны для Save/Copy/Publish. UI предупреждает, скрывает их из обычного нового выбора и сохраняет выбранные исторические значения;
- AssetReference = one AssetId, compatible with the AssetKind fixed by schema definition;
- при Publish server захватывает immutable confirmed media descriptor в runtime values/canonical profile+digest (`cm-instance-content/1` для Demo/historical encoding, новый MVP profile для Offset по F03); exact bytes/hash/profile сохраняются вместе. Value identity не включает instance ID/Key/version/orphans/ItemKey; absent/null/default не взаимозаменяемы. Delivery читает immutable bytes без live enrichment/rehash. Asset binary hash отдельный; Authoring write identity AssetId, CM URL не Published content;
- Object/List валидируются рекурсивно.
- в MVP все configured value/collection constraints, uniqueness и stored order проходят строгую проверку.

Любое нарушение блокирует весь Publish без partial result и автоматического изменения values. cm-instance-content/1 сохраняется при прежнем runtime encoding historical Naive values; MVP Offset требует нового instance-content profile в совместном наборе constraints+Offset F03. Явная перестановка List меняет value/hash обычным образом, без нового profile только из-за constraints.

## DateTime values MVP

New Offset value обозначает UTC instant с введённым local date/time и offset. Authoring принимает только `yyyy-MM-dd'T'HH:mm:ss.fffffffzzz` либо UTC `yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'`: invariant culture, ровно семь fractional digits, обязательный offset. Без offset, leap second и `-00:00` невалидны. Offset — whole minutes в диапазоне −14:00…+14:00; local value и corresponding UTC instant должны находиться в диапазоне DateTimeOffset. Новое intrinsic type-invalid value отклоняет Save по общим правилам выше. Historical Naive `/1` contract сохраняется без auto-conversion.

Save/read сохраняет original offset и 100ns precision: введённое `12:00 +03:00` возвращается как `12:00 +03:00`, без server/browser timezone conversion или округления. Dynamic authoring value остаётся в JSONB snapshot с offset и семью fractional digits, а не в отдельной SQL timestamp-колонке каждого поля. Original offset доступен в authoring view и после Publish.

Publish Offset value проецируется в canonical UTC `yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'`; `12:00 +03:00` и `09:00 Z` дают одинаковые runtime bytes/hash при остальном равном. Это явная runtime projection нового profile F03/Q19, а не coercion validator. Exact Published canonical bytes/profile/digest сохраняются атомарно и далее читаются без rehash. Published runtime view показывает canonical UTC.

Equality, inclusive Minimum/Maximum и comparator Offset values используют UTC instant; разные offsets одного instant равны, offset не secondary key. Naive не сравнивается с Offset через скрытую conversion. Historical Naive read/generated C# остаётся `DateTime` с Kind=Unspecified, Offset использует `DateTimeOffset`; exact versions обеих semantics могут одновременно существовать в одном generated client project F22.

Редактор показывает отдельные date/time и offset controls; initial new Offset value имеет видимый `+00:00`, который специалист может изменить. Derived UTC instant показан отдельно. Смена offset сохраняет введённые date/time и меняет instant, что UI объясняет. Региональные timezone IDs, расписания/DST и browser timezone binding не используются. Fractional seconds имеют явное редактирование; открытие формы или изменение других fields не обнуляет их. Date/TimeSpan и audit/lifecycle/system timestamps сохраняют прежний смысл.

## List: равенство и порядок MVP

MinItems/MaxItems ограничивают размер отдельно от Required. Empty List допустим без запрещающего MinItems. List Item definitions рекурсивно проверяют применимые constraints по F01.

UniqueItems сравнивает полное type-aware normalized runtime value по Q19: Object fields — по FieldId, absent отличается от present default, FlagEnum — set keys, nested Lists — с учётом порядка. ItemKey/orphan/authoring metadata в равенство не входят: одинаковые Object с разными ItemKey — duplicates. Hash только индексирует кандидатов; совпадение подтверждается полными normalized values/bytes. Invalid/incomplete items дают issues, а не успешное доказательство uniqueness; partial Draft сохраняется, Publish требует валидного результата.

Offset DateTime values разных offsets с одинаковым UTC instant считаются duplicates в UniqueItems. Ascending/Descending сравнивают их как equal keys; stable sort сохраняет authored tie order без offset tie-break. Это применяется и к DateTime leaf в ObjectFieldPath comparer.

OrderMode `Ascending`/`Descending` проверяет stored authored array order по одному comparer F01; ни canonicalizer, ни Delivery/SDK не переставляют элементы. Нарушение порядка сохраняется как issue при Save и блокирует Publish. Equal comparer keys разрешены; implicit Guid/ItemKey tie-break отсутствует. UniqueItems проверяет полный value, а не только comparer key.

Явное действие «Сортировать» стабильно переставляет только локальный editable Draft, сохраняя взаимный порядок equal keys. Затем выполняется обычный Save с исходным ExpectedVersion/If-Match; отдельного server mutation/route нет. Published read-only. Missing/invalid ObjectFieldPath key в partial Draft даёт адресуемый issue, делает сортировку недоступной и блокирует Publish; null/default substitution отсутствует.


## Stale-base


Draft хранит baseline latest Published EntityInstanceVersion **для той же target SchemaVersionId**.


Публикация version по другой SchemaVersion не делает Draft stale.


Если более новая Published version появилась по той же target SchemaVersion:


- Draft можно Save;
- Publish блокируется;
- automatic merge отсутствует;
- specialist создаёт новый Draft либо явно «Актуализирует основу»: backend проверяет exact admissible target, exact current target baseline, подтверждённую пользователем basis и concurrency без изменения payload. Missing required/orphan и сохранённая несовместимость допускаются; результат возвращает diagnostics. Это не strict Publish validation и не разрешение Publish к Withdrawn target.


## Конкурентная публикация

Одновременный Publish Draft по разным SchemaVersionId кратко сериализует выделение общей sequence. Если relevant target baseline каждого Draft актуальна и остальные условия допустимы, обе операции завершаются успешно с уникальными последовательными номерами. Изменение общего счётчика само по себе не является stale-base. Реальный конфликт target baseline возвращается без автоматического retry/merge.

## Published selection в Demo


Для exact requested SchemaVersionId Delivery сначала фильтрует Published instance versions этой структуры, затем выбирает latest VersionNumber каждого EntityInstance.


Новая Published version по другой SchemaVersion не вытесняет representation запрошенной структуры.


## Lifecycle


Draft version можно hard-delete, включая последний Draft. У logical EntityInstance сохраняются identity, Key, Name и пустой список versions; parent не удаляется автоматически.


Logical EntityInstance можно hard-delete только пока нет Published/history dependencies; тогда Draft versions удаляются вместе. Backend сравнивает current exact dependency set с подтверждённым impact; при смене состава, даже с прежним count, отклоняет delete и требует нового подтверждения. 5-second задержка обязательна для schema Draft cascade F02 и не назначается автоматически instance delete.


После Published history MVP использует Archive:


- hidden from normal authoring/pickers;
- no new Draft/Publish/new assignment;
- historical Release/Revision/Delivery unaffected.


Restore возвращает authoring availability без автоматической публикации.


Published EntityInstanceVersion не hard-delete; MVP Retire/Restore запрещает/возвращает возможность нового использования, сохраняя history.


Demo withdrawal schema, exact schema version либо logical EntityInstance приводит к Remove соответствующих representations на следующей direct sync и не удаляет Published history. Historical read, Copy и Save recovery work разрешены с предупреждением для снятого source/target; Publish к снятому target и runtime выдача запрещены. Снять logical EntityInstance с выдачи можно только при наличии retained Published content: Draft-only/пустой экземпляр отклоняется без изменения состояния. UI показывает «Нет опубликованных версий», а не «Доступен» только из отсутствия Withdrawal.

Отдельное явное действие «Вернуть в выдачу» для owner с Published history проверяет допустимые Published representations и действующие запреты schema/schema-version/instance и media. Для ранее возникшего Withdrawn owner **без** retained Published content допускается recovery: снять только собственный запрет withdrawal с own concurrency check; Draft, schema/media facts, номера и хеши не изменяются, выдача не появляется. Это возвращает возможность продолжить authoring/Publish, а не публикует Draft и не снимает независимые запреты. Новый Publish не очищает Withdrawal автоматически.

## Массовая фиксация

Список экземпляров поддерживает «Зафиксировать выбранные версии» по exact Draft IDs. Preflight и выполнение сохраняют те же strict validation, stale-base, concurrency и lifecycle guards, что одиночный Publish; необходимые Draft схемы фиксируются первыми. Подтверждение и частичные результаты определяются [F07](F07-product-content-assignment.md#массовая-фиксация-и-подготовка-выпуска) и [Q38](../questions/Q38-bulk-version-freeze-and-release-preparation.md). Runtime по-прежнему исключает Draft; наличие Published версии само по себе не активирует продуктовую ревизию.


## Assets


Stable identity AssetReference — AssetId; schema definition дополнительно фиксирует требуемый AssetKind. Runtime projection возвращает frozen descriptor: `AssetId`, `AssetKind`, `ContentType`, `SizeBytes`, SHA-256 файловых bytes. URL строится клиентом из CM configuration + AssetId и не является frozen runtime member. Persisted authoring identity остаётся AssetId; server не доверяет metadata, пришедшей обратно от клиента. Save/Publish проверяют соответствие kind и availability через F05. Upload/download bytes принадлежит F05. Ошибка последующей загрузки bytes не меняет published reference.

В интеграции после Demo способ доставки определяется AssetReference leaf definition **точной** SchemaVersion по [F01](F01-create-entity-schema.md#режим-доставки-assetreference-после-demo); instance editor выбирает asset, а не собственный mode. `DeliveryMode` не становится шестым runtime member и не меняет форму значения. Resolver учитывает только реально присутствующие ссылки разрешённого snapshot; отсутствующее optional значение не создаёт требование предварительной загрузки. Binary readiness и entity publication/sync различны по [F13](F13-content-delivery.md#launcher-и-selective-preload-после-demo).


## Acceptance


- stable key unique per schema;
- multiple Drafts;
- cross-SchemaVersion copy по SchemaFieldId;
- orphan и сохранённая несовместимость не теряются и блокируют Publish; новое present type-invalid значение отклоняет Save адресуемо;
- target SchemaVersion Draft позволяет только Draft instance;
- Published numbering одна sequence на logical instance;
- stale определяется только внутри target SchemaVersion; конкурентный Publish разных targets с актуальными bases успешен с разными sequence numbers;
- historical representations exact schema versions сохраняются.
- MVP type-valid constraint violations сохраняются адресуемо и блокируют Publish; новый type-invalid ввод отклоняет Save;
- Copy сохраняет type-compatible value и target-constraint issue без искусственного orphan;
- fixtures покрывают Object duplicates с разными ItemKey, nested order, equal comparer keys, stable local sort и Save/Publish/Copy без автоматических исправлений.
- Offset fixtures покрывают date rollover, ±14:00/local и UTC range overflow, strict parsing и 100ns precision; Save/read и UI сохраняют original offset/fractional seconds;
- equivalent instants дают одинаковые runtime bytes/hash, inclusive bounds, UniqueItems duplicates и equal comparer keys со stable ties;
- Naive → Offset Copy сохраняет orphan/unfilled границу и требует ручного offset; historical/new generated versions совместно читаются без timezone conversion.


## Связи


[F01](F01-create-entity-schema.md) · [F02](F02-create-schema-version.md) · [F03](F03-publish-schema-version.md) · [F05](F05-media-assets.md) · [Q34](../questions/Q34-schema-field-semantics.md) · [Q35](../questions/Q35-value-constraints.md) · [Q36](../questions/Q36-entity-instance-lifecycle.md) · [Backend](../../technical/backend/features/F04-entity-instances.md) · [Frontend](../../technical/frontend/features/F04-entity-instances.md)
