# F03. Публикация версии схемы

## Цель

Зафиксировать выбранный Schema Draft как immutable Published contract и назначить ему следующий business VersionNumber.

## Одиночная и массовая фиксация

UI называет действие «Зафиксировать версию», техническое состояние остаётся `Published`. Помимо одиночного действия в workspace, список схем поддерживает «Зафиксировать выбранные версии», а мастер «Подготовить выпуск» фиксирует явно подтверждённые schema dependencies перед экземплярами. Каждая exact Draft сохраняет обычные проверки F03; массовое действие не выбирает latest и не публикует продукт клиентам. Поэлементные результаты и failure boundaries определены в [F07](F07-product-content-assignment.md#массовая-фиксация-и-подготовка-выпуска), транспортные детали — [Q38](../questions/Q38-bulk-version-freeze-and-release-preparation.md).

## Предусловия

- version существует и находится в Draft;
- Draft не изменился после загруженного пользователем состояния;
- `BasePublishedVersionId` совпадает с current latest Published version;
- структура проходит strict structural/evolution validation, в MVP включая все typed constraints и comparer/path definitions по F01/Q35;
- logical Schema не Archived/Withdrawn и version не заблокирована lifecycle;
- F17 moderation не требуется.

## Публикация

При успешном Publish сервер атомарно:

1. повторно проверяет concurrency и stale baseline;
2. повторяет проверки всех definitions, в MVP включая recursive constraints и List comparers, и проверяет evolution rules по всей Published history каждой stable field/option identity, включая возвращённые ранее removed IDs;
3. назначает следующий `VersionNumber = 1,2,3...`;
4. вычисляет canonical content hash;
5. фиксирует PublishedAt и historical Schema Name/Key contract metadata;
6. переводит Draft в immutable Published;
7. обновляет logical Schema latest published pointer/sequence.

Специалист не задаёт VersionNumber, hash или PublishedAt. Demo Published semantic schema получает canonical profile cm-schema-content/1 и digest; immutable bytes/profile/hash сохраняются вместе. Authoring captions/comments/time не content identity, historical snapshot не rehash-ится после изменения правил/имени. Profile и digest сравниваются вместе; Registry/codegen representation — отдельные contracts [Q19](../../technical/backend/questions/Q19-content-hashes.md#принятый-canonical-profile-v1).

Первый новый MVP profile set совместно включает constraints Q35 и Offset DateTime Q34: требуются новые schema-content, instance-content, JSON Schema и C# profiles. Offset меняет runtime DateTime bytes, поэтому `cm-instance-content/1` недостаточен для Offset values. Published Offset values и Offset bounds schema definitions проецируются в canonical UTC с ровно семью fractional digits и `Z`; эквивалентные UTC instants дают одинаковый content/hash при остальном равном. Original offset остаётся в authoring snapshot F04 и не является runtime identity.

Старые `/1` bytes и historical Naive semantics сохраняются; historical rehash/re-export под новой семантикой отсутствует. Group/inventory/manifest profiles меняются только при собственном изменении layout/semantics и используют content-profile identifiers. Temporal semantics согласованы; exact whole new-profile codec, extension carriers и literal golden bytes остаются **Уточняется** в Q19/Q31. Готовый `/2` codec этим решением не назначается.

## Параллельный Publish

Если два Draft одной baseline публикуются конкурентно, только один может стать следующей version. Второй остаётся Draft и получает stale-base conflict. Он не становится следующей version автоматически.

## Evolution rules

Для одинакового `SchemaFieldId` сохраняются immutable Key/Type/type semantic identity и цепочка definition-контейнеров Object/List. Перемещение в другой контейнер требует нового ID; UI-перестановка и runtime List order identity не меняют.

DateTime `TemporalSemantics = Naive|Offset` входит в immutable semantic identity. MVP Publish отклоняет смену этой semantics под историческим ID; Offset bounds проверяет по UTC instant. Новый Offset field не меняет смысл historical Naive definitions.

Monotonic rules:

- Required можно только ослабить `true → false`;
- Deprecated только `false → true`;
- Deprecated field должен быть optional;
- removed field может отсутствовать в новой version и вернуться с прежним ID/contract/container chain; Required relaxation и Deprecated нельзя отменить относительно любого Published появления этого ID;
- новый field имеет новый SchemaFieldId.

Все type definitions обязаны быть publish-valid. Enum/FlagEnum options сохраняют immutable keys/смысл и monotonic Deprecated по Published history. Объявленные deprecated keys остаются допустимыми значениями при Save/Copy/Publish; UI предупреждает и скрывает их из обычного нового выбора, сохраняя уже выбранные исторические значения.

MVP constraints можно усиливать/ослаблять в пределах intrinsic типа без нового FieldId. Это не отменяет immutable Key/Type/container identity и monotonic Required/Deprecated. Все configured rules должны быть внутренне валидны; ошибочный definition блокирует весь Publish без частичного результата и автоматического исправления.

В интеграции после Demo Publish фиксирует `DeliveryMode` каждого AssetReference leaf definition вместе с точной SchemaVersion; обычные поля и рекурсивные ItemDefinition проходят одинаковую проверку применимости/literals. Режимы определены [F01](F01-create-entity-schema.md#режим-доставки-assetreference-после-demo). Смена mode требует новой публикации; допустимость сохранения прежнего FieldId ещё [Уточняется Q34](../questions/Q34-schema-field-semantics.md#deliverymode-и-идентичность-поля).

Delivery metadata нельзя молча добавить в canonical `cm-schema-content/1` или historical Registry `cm-json-schema/1`. Нужен согласованный versioned carrier/new canonical profile либо отдельный metadata digest; exact profile и golden bytes [Уточняется Q37](../../technical/frontend/questions/Q37-launcher-asset-delivery.md). Исторические Published bytes/hash/экспорт сохраняются; отсутствие нового mode имеет effective `OnDemand` без rehash. Runtime AssetReference descriptor этим решением не расширяется.

Canonical profile версионирует byte/semantic contract, а не весь бэкенд или любую validation policy. Новый schema snapshot не всегда требует нового profile; изменение нормативных bytes/значения representation требует. Обратная совместимость требует сохранения historical bytes/codecs и поддержки клиентом, не возникает автоматически от смены номера. Разграничение schema/profile/protocol и примеры — [Q19](../../technical/backend/questions/Q19-content-hashes.md#профиль-бизнесовые-правила-и-обратная-совместимость).

## Immutable result

После публикации SchemaVersion:

- не редактируется;
- business number не меняется и не переиспользуется;
- contract/hash не меняются;
- historical key/name snapshot не меняется при rename logical EntitySchema;
- доступна Schema Registry/codegen по exact identity;
- сама по себе не означает клиентскую Product публикацию.

## Lifecycle

Published version физически не удаляется.

MVP Retire запрещает новое использование, сохраняя historical exact read/codegen/Delivery dependencies. Withdrawal direct Demo delivery является отдельным запретом выдачи и нового Publish к снятому target, сохраняя historical read, Copy и Save recovery work с предупреждением. Успешный Publish сам по себе не снимает Withdrawal. Для logical instance возврат в выдачу — отдельное явное действие F04 с проверкой допустимых Published representations; F13 учитывает current availability в следующем новом plan.

## Acceptance

- Draft получает номер только после successful Publish;
- numbering integer/monotonic/unique per EntitySchema;
- concurrent publish имеет одного победителя;
- stale baseline отличим от concurrent edit;
- Published state immutable;
- Schema rename не меняет старый Published contract.
- MVP Publish повторяет проверку всех recursive constraints/comparer definitions; ошибка сохраняет Draft без partial result;
- old/new profile fixtures подтверждают сохранность historical `/1` bytes; новая schema semantics не подменяет старый profile.
- MVP fixtures проверяют Offset bounds/equivalent instants, immutable TemporalSemantics и canonical UTC projection; exact new-profile golden bytes проверяются после детализации codec Q19/Q31.

## Связи

[F02](F02-create-schema-version.md) · [F04](F04-entity-instances.md) · [F21](F21-schema-registry.md) · [Q08](../questions/Q08-keys-and-drafts.md) · [Q34](../questions/Q34-schema-field-semantics.md) · [Q35](../questions/Q35-value-constraints.md) · [Backend](../../technical/backend/features/F03-publish-schema-version.md) · [Frontend](../../technical/frontend/features/F03-publish-schema-version.md)
