# Q19. Хешируемое представление

## Вопрос и известные границы

Каковы формат, порядок, кодировка, нормализация и состав данных для хеша схемы, экземпляра и манифеста? Что проверяет клиент?

**Ответ:** Для Demo приняты semantic definition hash схемы, values-only hash экземпляра, отдельные versioned group/inventory/plan profiles, canonical ordering/encoding, naive DateTime format и capture AssetReference при Publish. Ниже зафиксированы границы. Все 24 решения приняты: identifiers/digest/escaping/numeric emission и F10 membership/provenance разделены в нормативном профиле ниже. Literal samples фиксируются отдельно; полная conformance matrix и реальная server/SDK/Unity совместимость проверяются при реализации.

## Профиль, бизнесовые правила и обратная совместимость

Canonical profile — версия конкретного контракта нормализации/байтов и их смысла, а не версия всего бэкенда или всех бизнесовых правил. Изменение сообщения validation, permission, UI-порядка таблицы или внутреннего алгоритма при сохранении нормативных bytes/semantics не требует нового profile. Изменение authored значений либо порядка элементов конкретного List меняет hash содержимого в том же profile. Новое field definition/constraint фиксируется новой SchemaVersion; необходимость нового profile определяется изменением самого formal/canonical контракта.

Если меняются нормативное кодирование, canonical порядок, состав hash input, presence/default rules или интерпретация runtime значения, требуется новый profile. Исправление, меняющее обещанные canonical bytes, нельзя скрыть под прежним ID. SchemaVersion, canonical profile, JSON Schema exporter profile, C# generator profile и sync protocol version — независимые оси версионирования, хотя совместимый набор проверяется вместе.

Обратная совместимость обеспечивается сохранением exact historical bytes/profile/hash и поддержкой старых readers/codecs/bindings. Старые Published snapshots не перевалидируются и не rehash-ятся текущими правилами. Новые строгие правила применяются к новым Draft/Publish согласно schema evolution F03/F04. Несовместимое изменение field semantics требует новой schema/field identity по правилам F01/F03 и явного перехода клиента; смена одного profile не делает старый SDK автоматически совместимым.

Перед включением нового profile сервер и SDK должны явно поддерживать его вместе с нужными historical profiles; неизвестный profile отклоняется объяснимо, без попытки читать его как current. Реальные old/new server+SDK golden/conformance fixtures обязательны. Аналитика сохраняет такие гарантии как требования и не объявляет новые MVP codecs или migration engine реализованными.

Русские UI-подписи: «Профиль расчёта хеша» (технически «Профиль канонизации») и отдельное «SHA-256 содержимого». Profile ID не показывается как hash; технические значения доступны в подробностях.

## DeliveryMode metadata после Demo

Дополнение [Q37](../../frontend/questions/Q37-launcher-asset-delivery.md) фиксирует schema-owned DeliveryMode и effective OnDemand для historical definitions без свойства. cm-schema-content/1/Published bytes/hashes не переписываются. Новый canonical профиль либо отдельный versioned metadata digest, encoding/presence/default и включение в MVP bundle constraints+Offset **уточняются**. Registry annotation не расширяет values-only frozen runtime AssetReference. Proposed AssetDeliveryManifest имеет отдельный integrity contract; profile/hash/order нельзя вывести из F13 group/inventory/plan hashes.

## Затронутые фичи

[F03](../../../business/features/F03-publish-schema-version.md) · [F04](../../../business/features/F04-entity-instances.md) · [F10](../../../business/features/F10-publication-revisions.md) · [F13](../../../business/features/F13-content-delivery.md)

## Как применить ответ

Применить принятые границы в F03/F04/F13/F19/F21/F22. Canonical profile реализуется по одним literal fixtures на сервере и клиенте; existing helper не становится oracle expected bytes.

## Current-state snapshot 01.10.2026

В текущем коде есть canonicalization/hash для опубликованного schema/instance content; `PublishedContentJson` сохраняется отдельно. `PublishedComposition` в Delivery проверяет, что stored manifest bytes соответствуют переданному manifest hash. Media upload вычисляет SHA-256 по реальным bytes.

Это подтверждает наличие текущих hash-механизмов, но **не превращает их автоматически в нормативный контракт Q19**. По-прежнему нужно зафиксировать canonical bytes/encoding/order/profile, нормализацию строк/чисел и golden vectors, особенно потому что target schema semantics и field ordering меняются.

## F03: профиль схемы и эталонные векторы

Связано с [backend F03](../features/F03-publish-schema-version.md), [Registry F21](../features/F21-schema-registry.md), [Q15 F03](Q15-api-and-source-code.md#f03-детализация-целевого-контракта). Number1,2,3/immutable Published/own ETag и граница semantic hash приняты; concrete byte/digest fixtures подлежат contract freeze.

Current local source 04.10.2026: EditorCommandHandler сериализует version.ToDto().Fields через JsonSerializer.SerializeToElement и CanonicalContent.Normalize; helper сортирует object properties ordinal, сохраняет array order, пишет UTF-8 JSON и вычисляет SHA-256 uppercase hex. Это current implementation, не согласованный target. Target definition отличается от current DTO; сортировка properties не обеспечивает stable SchemaFieldId order полей внутри arrays.

| Вопрос | Требуемая граница |
| --- | --- |
| Hash input | `/1` semantic definition: FieldIds, Keys, Types, Required/Deprecated, options и recursive definitions, включая AssetKind/OrderMode. MVP typed constraints/comparer Q35 требуют нового schema-content profile. Name/HelpText/option labels, logical schema labels, Draft Title/ChangeComment, publication number/time исключены; ContentHash себя не включает |
| Разделение representations | F03 semantic definition ContentHash и F21 exact JSON Schema representation ETag независимы. Изменение annotations может изменить Registry bytes/ETag без изменения semantic hash; exact frozen snapshot/labels всё равно immutable |
| Ordering | Named definitions по SchemaFieldId; Guid lowercase D, comparator ordinal по canonical representation. Options по immutable Key ordinal. Metadata property emission фиксируется versioned writer-ом; произвольный input/dictionary order не меняет bytes |
| Normalization | UTF-8 без BOM, без insignificant whitespace; скрытого trim/case/Unicode normalization нет. Profile явно задаёт recursive variants/presence и escaping. Schema definition не содержит instance values |
| Bytes/digest/storage | Hash по тем же canonical bytes, которые losslessly сохраняются и восстанавливаются из Published snapshot; не physical JSONB/re-serialization. SHA-256 exact bytes → 64 lowercase hex, cm-schema-content/1; escaping/numeric rules выбраны ниже, current helper не oracle |
| Profile evolution | Versioned CM canonical profile обязателен; identifiers выбраны ниже; actual storage/wire carriers materialize вместе с profile-aware models. Изменение writer не переписывает historical Published bytes/hash. Registry/codegen profiles версионируются отдельно |
| Golden vectors | Literal input → expected bytes → expected digest для scalar/Object/Enum/Flag/List<List>/List<Object>, reordered input, non-ASCII/escaping/null и metadata changes. Expected нельзя вычислять тем же production helper во время test |

**Ответ:** Hash input/exclusions/order/encoding/profile evolution согласованы. Exact digest/serializer bytes и interop подтверждаются literal golden vectors; до их реализации byte compatibility не объявляется проверенной.

## F04: профиль значений экземпляра

Связано с [backend F04](../features/F04-entity-instances.md), [Q15 F04](Q15-api-and-source-code.md#f04-детализация-целевого-контракта), [F13](../features/F13-content-delivery.md), [Q31](Q31-schema-code-generation.md). Publish экземпляра сохраняет immutable representation и hash по одним и тем же bytes; целевой профиль требуется определить отдельно от authoring tree и формального определения схемы.

Current source 04.10.2026: EditorCommandHandler сериализует mapped Fields DTO, CanonicalContent сортирует object properties ordinal, сохраняет array order, пишет UTF-8 JSON и SHA-256 hex. InstanceContentMapper упорядочивает named fields; legacy values/options/list shapes отличаются от целевого recursive contract. Это исходная реализация, а не согласованная canonical representation F04.

| Вопрос | Требуемая граница |
| --- | --- |
| Runtime representation | Validated FieldId authoring tree преобразуется в recursive runtime properties по exact contract Keys. Orphans/authoring labels/technical Version/diagnostics не runtime values. Authoring DTO и runtime JSON разные representations; служебный ItemKey не становится fake business property |
| Hash input/envelope | Instance ContentHash покрывает только canonical runtime values, включая captured immutable AssetReference descriptor. InstanceId, SchemaVersionId, Key, VersionNumber передаются отдельно и входят в group integrity; времена/authoring metadata/сам hash исключены |
| Presence/order | Absent/null/default различаются; missing required не заполняется default. Named fields по stable FieldId с ordinal canonical lowercase Guid D comparator; Default List сохраняет authored order, FlagEnum keys ordinal. Runtime ItemKey treatment явно задаёт mapper, не array sorting helper |
| Scalars | Integer long/Float finite double/Decimal decimal без прохода Integer/Decimal через double; Date yyyy-MM-dd; signed TimeSpan invariant c; DateTime yyyy-MM-dd'T'HH:mm:ss.fffffff Kind=Unspecified без Z/offset, всегда 7 fractional digits; Enum/Flag keys, uppercase Color #RRGGBBAA. Text — согласованный sanitized restricted HTML Q34. Exact numeric emission/escaping приняты ниже и закрепляются fixtures; hidden value normalization нет |
| Bytes/storage/hash | UTF-8 без BOM/insignificant whitespace; canonical text/bytes сохраняются losslessly без повторного JSONB serialization. SHA-256 exact bytes → 64 lowercase hex, cm-instance-content/1; escaping/numeric rules ниже; helper не oracle |
| Profile compatibility | cm-instance-content/1 выбран; exact placement/codec adapters — engineering freeze. Новый canonicalizer не переписывает historical Published bytes/hash и не меняет существующую Delivery representation |
| Golden vectors | Literal input → approved bytes → approved digest: all15 types, Object/List<List>/List<Object>, zero/false/negative duration/transparent color/empty List/Flag, absence, option order, reordered fields, authored list reorder, non-ASCII/escaping/Decimal precision/date/Text. Векторы отражают также принятый envelope и несколько target schema одного instance. Expected не вычисляется production helper во время test |

**Ответ:** Values-only hash, representation boundaries, scalar/order/encoding и immutable capture согласованы. Literal bytes/digest vectors и server/SDK serializer implementation ещё должны быть подготовлены и проверены.

## Demo F13/F19: integrity и общая runtime representation

Связано с [F13](../features/F13-content-delivery.md), [F19](../../frontend/features/F19-unity-client.md), [Q30](Q30-content-synchronization.md), [Q33](Q33-sync-resume-transport.md) и принятыми semantic mappings [Q31](Q31-schema-code-generation.md). Согласованные streaming/group-commit guarantees требуют единого byte/hash контракта; имя semantic profile само по себе его не задаёт.

| Оставшаяся граница | Что требуется закрепить |
| --- | --- |
| Inventory/group/plan | Отдельные versioned profiles. TargetGroupHash покрывает полный отсортированный target membership: exact instance identities/version IDs/Keys/ContentHashes в exact SchemaId/SchemaVersionId context. Inventory fingerprint описывает actual installed state. Plan identity связывает original request fingerprint с exact target groups/removals. Target group проверяется после atomic removals/upserts; порядок tuple members/bytes/digest закрепляется literal contract |
| Naive DateTime | Canonical JSON string yyyy-MM-dd'T'HH:mm:ss.fffffff, всегда 7 fractional digits, без Z/offset; Kind=Unspecified. UI может показывать меньше знаков, но сохраняет precision. Пример 2026-10-05T14:30:00.0000000; converter/strict validation/round-trip без implicit UTC/local conversion |
| AssetReference descriptor capture | Authoring write — AssetId. При Publish сервер захватывает согласованные immutable MediaLibrary metadata в Published runtime snapshot; descriptor входит в values/hash. Delivery читает snapshot без повторного enrichment. URL SDK строит из CM configuration + AssetId; configured CM/storage address не сериализуется |
| Conformance vectors | Literal published values + selections/inventory → exact payload/group membership → expected bytes/hash/checks; reordered set, multiple exact versions, removals, missing/duplicate/truncated content, descriptor/temporal edge cases. Server и SDK проверяют один fixture contract, expected не строится production canonicalizer |

Это детализация Demo interoperability, не требование F10/лицензий/полного Publishing. Profile identifiers/digest/escaping/numeric emission и integrity inputs приняты ниже; full literal envelopes/shared adapters и conformance остаются materialization перед server/SDK integration. Принятые Date/TimeSpan/numeric/Enum/Color semantics и отделение Registry ETag от F03 hash не переоткрываются. JCS нельзя применять без адаптации: его IEEE-754 numeric restriction не сохраняет наш long/decimal contract; [RFC 8785](https://www.rfc-editor.org/rfc/rfc8785).

## F10: materialized representation и hash

Связано с [backend F10](../features/F10-publication-revisions.md), [frontend F10](../../frontend/features/F10-publication-revisions.md), [F13](../features/F13-content-delivery.md), [SDK F19](../../frontend/features/F19-unity-client.md), [Q15](Q15-api-and-source-code.md#f10-revision-contracts-и-preview), [Q20](Q20-persistence-and-migration.md#f10-composition-capture-и-withdrawal). Deterministic materialization/preserved historical result и cm-publication-manifest/1 выбраны; full materialized membership/frozen aliases/hash profiles входят в runtime manifest, provenance хранится отдельно.

Current source 04.10.2026: [ProductVariantPublicationRevision.Create](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Domain/Publications/ProductVariantPublicationRevision.cs) сортирует effective items по SchemaKey/SortOrder/InstanceKey и строит два JSON: Items с Source, manifest без Source, но с IDs/Keys/versions/contentHash/order/assets. [CanonicalContent](../../../../contentmanager.api/src/BoundedContexts/Shared/ContentManager.Domain.Shared/ValueObjects/CanonicalContent.cs) сортирует object properties ordinal, сохраняет array order, пишет UTF-8 и SHA-256. [ContentHash](../../../../contentmanager.api/src/BoundedContexts/Shared/ContentManager.Domain.Shared/ValueObjects/ContentHash.cs) приводит Value к lower hex. [Mapping](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.EntityFramework/Configurations/PublicationRevisionConfiguration.cs) сохраняет ItemsJson JSONB/ManifestJson text; [PublishedComposition.Create](../../../../contentmanager.api/src/BoundedContexts/Delivery/ContentManager.Delivery.Domain/Publications/PublishedComposition.cs) проверяет hash сохранённого manifest. Это current profile, не target stable-ID/new schema/provenance contract и не interop verification SDK.

| Область | Принято / оставшаяся materialization |
| --- | --- |
| Representation | cm-publication-manifest/1 full materialized identities/versions/frozen aliases/profile-aware hashes; full values retained in immutable instance representations; fixed input/member order F10 |
| Content vs provenance | Runtime result B; release IDs/SourceKind/override target A/exclusions/actor/comment/time/lifecycle/RevisionId excluded hash, retained immutable provenance; hash not artifact/approval/entitlement |
| Ordering | F10 fixed service members and canonical InstanceId order; semantic array order preserved; no mutable Key/EF order used to sort |
| Bytes/digest | UTF-8 no BOM/whitespace/final newline; chosen string/numeric rules; SHA-256 exact bytes64lowerhex, not JSONB reserialization |
| Profile evolution | Persist/expose cm-publication-manifest/1 with exact original bytes/hash, byte-changing new profile, no historical rehash |
| Golden vectors | Linked literal definition/value→manifest/profile/hash samples; full reorder/provenance/removal/asset/source-to-SDK conformance still actual implementation tests |

**Ответ:** cm-publication-manifest/1 выбран: immutable exact membership/frozen runtime aliases/profile-aware hashes, без full values/provenance/artifact IDs. Модели/фиксированный member order принадлежат F10; canonical правила ниже. F19 потребляет F13 contract. Реальная byte compatibility не объявляется доказанной по документации.

## Принятый canonical profile v1

Все пункты 1–24 согласованы. Это contract specification, не отчёт о готовом writer/Unity conformance. Модели общих CanonicalProfileId/CanonicalDigest/CanonicalRepresentation определены один раз в [F03](../features/F03-publish-schema-version.md), typed schema/value projections — F03/F04, manifest — F10, group/inventory/request/plan — F13. SDK использует те же neutral library contracts через adapters; source Domain и EF не становятся SDK dependencies.

| ProfileId | Вход и назначение |
| --- | --- |
| cm-schema-content/1 | Semantic definition с immutable field IDs/keys/types/required/deprecated/options/recursive definitions; labels/HelpText/logical labels/publication metadata исключены |
| cm-instance-content/1 | Canonical runtime values и frozen captured AssetReference descriptor; root IDs/key/number/labels/orphans/ItemKey не values |
| cm-publication-manifest/1 | Полный materialized membership exact schema/instance identities/versions/frozen aliases/profile-aware content hashes; full values остаются в instance representations |
| cm-target-group/1 | Полный target membership в exact SchemaId/SchemaVersionId, sorted InstanceId; instance ID/version/key/hash/profile |
| cm-installed-inventory/1 | Фактические installed identities/exact versions/hash profiles, не desired selection или staging |
| cm-sync-plan/1 | Discriminated original-request fingerprint и pinned plan fingerprint, fixed request/plan document variants F13 |

Hash = SHA-256 от exact canonical UTF-8 bytes; Value ровно 64 lowercase hex, без sha256: prefix. ProfileId — обязательная association рядом с bytes/digest, не вставляется как скрытый salt/prefix. Сравнение digest допускается только в соответствующем profile/typed integrity role. Профиль сохраняется с immutable original representation и передаётся в necessary integrity metadata; unknown/missing profile не guessed current profile. Registry cm-json-schema/1 representation ETag, generator cm-csharp/1, media byte SHA-256, own concurrency ETag и credential verifier имеют свои отдельные обязанности.

### Byte rules

- UTF-8 без BOM, indentation/insignificant whitespace и final newline. Record LF/HTTP compression/transport envelope не входят в content digest.
- Strict parse: duplicate properties, bad Unicode/lone surrogate, trailing garbage/invalid JSON отвергаются. Нет last-property-wins, silent replacement character или автоматического ремонта.
- String content сохраняется без trim/case/NFC. Quote/backslash используют JSON escape; U+0008/U+0009/U+000A/U+000C/U+000D — short b/t/n/f/r escapes, остальные U+0000–001F — lowercase u00xx escape. Другие Unicode scalars пишутся прямо в UTF-8. Slash/HTML characters не получают дополнительный escape. JSON delimiters/escape backslash — literal ASCII; это formatting, не изменение logical string.
- Integer — exact Int64 decimal digits, no exponent/leading plus/extra leading zero, never through double.
- Decimal — exact base-10 без exponent/лишних fraction zeros/лишней decimal point: 1.2300→1.23,1.000→1. No rounding; scale не отдельный business value. Numeric zero каноничен 0.
- Float — finite IEEE-754 binary64 по numeric serialization [RFC 8785 §3.2.2.3/Appendix B](https://www.rfc-editor.org/rfc/rfc8785#section-3.2.2.3), включая -0→0, normative exponent/shortest-round-trip rules. Полный JCS не применяется: он не заменяет exact long/decimal или FieldId ordering. Ordinary ToString/default serializer не oracle; implementation must pass vectors.
- Absent/null/zero/false/empty collection различаются. Type-invalid null rejected до writer, required absence не default-filled. Optional absent не подменяется null.
- Named field order — canonical lowercase Guid D SchemaFieldId ordinal из exact contract metadata, включая nested occurrences. Enum definition options/FlagEnum set values — immutable Key ordinal; flags duplicates не quietly Distinct. Fixed service/descriptor/manifest/tuple member order задаёт owning profile, dictionary/EF/input order его не меняет.
- List order сохраняется как authored/runtime array; nested List не получает fake FieldId. Canonicalizer не выполняет Asc/Desc comparator. Согласованный Q35 comparer валидирует authored order; его definition требует нового schema-content/formal profile, но не меняет runtime encoding `/1`. Old Published bytes не пересортировываются.
- Date/naive DateTime/signed TimeSpan/Color/Text базовые semantics Q34/Q31 прежние: Date yyyy-MM-dd, DateTime yyyy-MM-dd'T'HH:mm:ss.fffffff Unspecified/no offset, TimeSpan invariant c, Color uppercase #RRGGBBAA, Text уже sanitized до canonical writer. Canonicalizer не выполняет новый HTML sanitize.

### MVP: constraints, equality и profile evolution

[Q35](../../../business/questions/Q35-value-constraints.md) согласовал все 24 пункта первого MVP. Typed constraints принадлежат immutable SchemaValueDefinition snapshot и определены один раз в [F01 §2.1](../features/F01-create-entity-schema.md#21-aggregates-entities-и-value-objects). Semantic schema input нового profile включает все применимые typed rules, collection constraints, recursive Item rules и ListComparerDefinition с ordered SchemaFieldId path. Усиление/ослабление constraints может менять semantic definition/hash без нового FieldId; Key/Type/container identity и Required/Deprecated guards остаются F02/F03.

Это требует нового `cm-schema-content` и нового formal `cm-json-schema` profile; generated metadata/output changes требуют нового `cm-csharp`. Frozen `/1` definitions/bytes/hash/export остаются прежними: historical rehash/re-export под новой semantics запрещён. Exact identifiers/new canonical byte layouts и extension metadata ещё **Уточняется** в Q31 до literal golden fixtures; принятие Q35 не задаёт готовый `/2` codec. DateTimeOffset semantics согласованы Q34: immutable Naive/Offset definition, original authored offset, UTC runtime/semantic bounds/equality. DateTime /1 остаётся Naive; whole new-profile codecs/golden layouts ещё engineering, не открытый product выбор.

`cm-instance-content/1` сохраняется при неизменном runtime encoding: constraints/comparer — definition metadata, не values/hash envelope. Validator/canonicalizer/Delivery/SDK не trim/coerce/default/clamp/round/dedup/sort. Явный stable UI Draft reorder меняет array/value/hash обычным Save/If-Match; Published history не меняется. Equal comparer keys допустимы без Guid/ItemKey tie-break.

RuntimeValueEquality использует full type-aware normalized values Q19: Object fields по SchemaFieldId, absent отличается от present default, FlagEnum — set immutable keys, nested Lists — ordered arrays. ItemKey/orphans/authoring metadata исключены. Hash только индексирует кандидатов; совпадение требует подтверждения полными normalized values/bytes. Invalid/incomplete items дают addressable issues и не successful uniqueness proof; Draft issue допустим, strict Publish блокируется. Comparer проверяет typed scalar/required ObjectFieldPath key по Q35 отдельно от full-value equality; String/Enum key comparison UTF-16 ordinal не подменяет String length в Unicode code points.

Shared fixtures должны связать actual CM ValueConstraintValidator/RuntimeValueEquality/ListOrderValidator со schema/value canonical adapters: Unicode supplementary/combining, exact Decimal step без double/overflow, inclusive bounds, Text decoded document-order plaintext, temporal bounds, recursive rules, Object duplicates разных ItemKey, absent/default, FlagEnum sets, nested order, ties/path errors и Save/Publish/Copy outcomes. Old/new source/formal/generation vectors проверяют unchanged `/1` bytes и отсутствие hidden sorting; expected literal bytes/digests не строятся production helper. Generic JSON Schema validation не доказывает CM extension semantics. Regex, Float MultipleOf, Decimal Precision/Scale и расширенные comparers не входят в первый MVP.

### MVP temporal canonical semantics

[Q34](../../../business/questions/Q34-schema-field-semantics.md#datetime-mvp-temporal-semantics-и-совместимость) согласовал все 18 пунктов. Shared TemporalCodec/instance scalar models описаны один раз в [F04](../features/F04-entity-instances.md#44-services--algorithms), definitions DateTimeValueDefinition — [F01](../features/F01-create-entity-schema.md#21-aggregates-entities-и-value-objects). Новый schema semantic input включает immutable TemporalSemantics; Offset bounds canonical UTC yyyy-MM-dd'T'HH:mm:ss.fffffff'Z', equivalent instant bounds same semantic bytes/hash. Original authoring numeric offsets/100ns immutable snapshot отдельно; runtime identity их не включает.

Offset instance runtime projection также UTC7digitsZ и requires **новый instance-content profile**: Q35-only definition extensions могли оставить runtime /1, но Offset меняет runtime value bytes. Первый new MVP schema/instance/formal/C# set совместно включает constraints и Offset. /1 exact naive DateTime encoding/old hashes/source exports/generation воспроизводимы; новые semantics не расширяют /1 или rehash history. Exact new profile identifiers/full member layouts/extension metadata/golden corpus остаются Q31/Q19 materialization; здесь закреплён temporal scalar encoding, не готовый whole /2 codec.

DateTimeOffset equality/order/UniqueItems/constraints по UTC instant; same instant с разными offsets → same canonical scalar bytes. Naive не promote по timezone/defaultoffset: новый FieldId и manual migration F04. Offset input only seven digits/Z/explicit ±HH:mm, reject missing/leap/-00:00/out-of-local-or-UTC-range; offsets whole minutes ±14:00. Pure formatters use explicit invariant output, не serializer default/local clock. JSONB authoring retains offset/100ns; runtime bytes separate frozen triple, не SQL timestamp precision oracle. Temporal codec не мигрирует audit/lifecycle fields.

cm-target-group/1, cm-installed-inventory/1 и cm-publication-manifest/1 не меняются только из-за новых content profile identifiers: они уже включают content hash/profile, layout остаётся прежним при прежней собственной semantics. Actual selected profile support по exact schema binding проверяется до value decoding/hash/group apply; unknown rejected, не downgrade/infer по строке. Hash bytes canonical UTC — не authorization.

Literal fixtures нужны на date rollover, ±14:00 и local/UTC min/max overflow, 100ns exact fraction, equivalent instants/offset bounds/unique/stable ties, old Naive/new Offset together, Copy/orphan and stored-authoring vs runtime precision. Expected temporal strings из независимых fixtures, full profile bytes/hashes не выдумываются из одного scalar sample. Actual .NET/SDK/Unity shared-library tests ещё выполняются при реализации.

### Membership, provenance и fingerprint

F10 canonical manifest связывает все materialized exact items с immutable schema/instance representations; сам не копирует full instance values. Member fields/order только в [F10](../features/F10-publication-revisions.md). Release IDs, SourceKind, override target A, exclusion/provenance, actor/comment/creation/lifecycle/RevisionId хранятся отдельно и исключены из runtime hash. Effective result identity B остаётся runtime identity. Same payload может иметь same digest при иных sources/artifact IDs, но это не artifact dedup/Approval/entitlement.

Target group canonical membership includes exact schema context and every target InstanceId/InstanceVersionId/InstanceKey/content hash/profile; sorted by canonical InstanceId. SDK first validates every incoming immutable instance representation, then verifies complete target group AFTER all removals/upserts, before atomic installed/inventory/receipt commit. GroupCompleted/network EOF/previous partial set не proof complete installed target.

Inventory fingerprint только actual committed installed store, включая exact versions/content profiles. Staging и desired selection не installed state. Canonical plan фиксирует original semantic request fingerprint + pinned target groups/removals; excludes random PlanId/CreatedAt/ExpiresAt/progress/receipts. Own completed commits не изменяют original request. cm-sync-plan/1 имеет fixed kind=request и kind=plan variants: request digest вычисляется этим же profile из initial normalized selection/inventory fingerprint+profile, plan binds original request digest/profile и pinned targets/removals. Seventh profile не вводится; role discriminator предотвращает смешение shapes. Preserved normalized original input remains replay comparison evidence, hash not authorization/secret.

Asset byte SHA-256 покрывает реальные immutable file bytes; instance hash покрывает captured descriptor в values. Configured URL/storage address/credentials/FileName authoring labels или live enrichment не превращаются в content integrity. Пользователь выбрал frozen descriptor из пяти обязательных полей: AssetId/AssetKind/ContentType/SizeBytes/Sha256Hash; canonical order assetId,assetKind,contentType,sizeBytes,sha256Hash. Типы/constraints один раз F04, neutral library adapter не импортирует MediaLibrary Domain. FileName/Format/dimensions/duration/URL исключены; media catalog UI может показывать их своим read DTO. Submitted metadata не authority, public F05 capture и guards прежние.

### Public sync wire v1 representation

[Q33 public wire](Q33-sync-resume-transport.md#public-wire-contract-v1) and [literal vectors](../sync-wire-v1-vectors.md) fixed contentJson as JSON string carrying original canonical JSON. One outer unescape→strict UTF8 reconstruct→digest verification→typed parse, no nested-value parse/reserialize or double. Extra envelope escaping/LF doesn't enter content hash. Candidate cursor server HMAC only protects plan/sequence token; not seventh canonical content profile/credential/install proof. HMAC binary byte order and literal tokens belong F13; signing key never part of SDK/digest schema.

baseGroupHash/profile reuse cm-target-group/1 over full original inventory subset in exact schema context. targetGroupHash covers resulting membership incl unchanged members. Immutable canonical plan retains all pinned target groups while Start/execution group arrays expose only changed groups; no-change groups:[] can still have nonempty canonical target. Existing canonical field layouts/profile IDs unchanged, controls/receipts/cursors/clock random IDs excluded. Signed cursor can't replace local-base/full-target check and atomic receipt/inventory.

### Library, persistence и conformance

One pure .NET canonicalization library implements explicit typed normalized inputs/profiles for backend and SDK, no Domain/EF/ASP.NET references. Application adapters map exact source contracts; SDK uses exact generated metadata/already provided contract, not untyped JToken number guesses and not a new runtime Registry fetch. Physical package/name/runtime numeric writer choice still must satisfy pinned SDK/Unity profile; no new external dependency unless justified.

Publish/materialization сохраняют original bytes/profile/hash атомарно в owning BC. Preparation outside guard допускается только с fresh basis validation внутри existing owner guard; source/instance/media basis change не разрешает commit старых prepared bytes. Stored bytes recovered losslessly, not JSONB normalization. Corrupted/mismatched representation fails; no silent reserialization/re-hash/history repair. Unknown profile error BEFORE group apply, no downgrade/guessing. Byte-changing bugfix/new implementation profile increments; old readers/writers retain original profiles and fixtures.

[Literal conformance samples](../canonical-v1-vectors.md) фиксируют reviewable exact bytes/digest независимо от production writer. Полная required matrix: all15 types, Int64/Decimal/Float extremes and negativezero, Unicode/control/escaping/absence, recursive objects/lists/flags, captured assets, reordered sets/IDs, all membership/removals/initial inventory, F10 same content/different provenance, malformed/truncated/unknown profile/corrupted bytes. Expected literals не генерируются production writer внутри assertion; literal SHA calculations не являются server/SDK/Unity tests. Реальная library/server/Unity conformance и exact normalized input/envelope fixtures materialize before integration; chosen algorithms/profile IDs/exclusions no longer TBD.
