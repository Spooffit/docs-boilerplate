# F02. Создание и редактирование версии схемы

## Цель

Позволить независимо готовить несколько новых Draft-структур одной EntitySchema, не изменяя Published history.

## Участники и этапы

- Demo: без входа/ролей.
- MVP: Specialist по F16/Q10.
- F17 moderation к SchemaVersion не применяется.

## Создание Draft

Новый Draft можно создать:

- с чистого листа;
- на основе любой Draft или Published version той же EntitySchema.

Clean Draft сначала существует только как локальная форма. Первый Save с минимально валидной структурой F01 создаёт серверный Draft; пустая форма resource не создаёт. Copy валидной существующей версии создаёт серверный Draft сразу. Источник с Withdrawal остаётся доступным для historical read и Copy; UI показывает предупреждение. Это подготовка recovery work и не разрешение Publish/Delivery снятого target.

Сохранённый Draft имеет собственный `EntitySchemaVersionId`, но **не имеет business VersionNumber** до F03. Каждый Draft имеет:

- `Title` — короткое имя параллельной работы;
- optional `ChangeComment` — что и зачем изменяется; внешний номер задачи допустим обычным текстом;
- `BasePublishedVersionId` — Published baseline, определяемый сервером.

При copy другого Draft его Published baseline наследуется. Это не Git branch и не живая ссылка на source version.

## Редактирование fields

Для существующего field обязательна identity `SchemaFieldId`.

Разрешено:

- менять `Name`;
- менять `HelpText`;
- в MVP усиливать/ослаблять typed constraints в пределах intrinsic типа по F01/Q35 без нового FieldId;
- `IsRequired true → false`;
- `Deprecated false → true`;
- удалять field из Draft;
- добавлять новые fields без ID.

Запрещено для существующего field:

- менять Key;
- менять Type/type semantic identity;
- менять `DateTime.TemporalSemantics` Naive/Offset;
- `IsRequired false → true`;
- `Deprecated true → false`.

Изменение key/type/смысла либо цепочки definition-контейнеров Object/List оформляется удалением старого field и добавлением нового с новым ID. Визуальная перестановка полей и порядок элементов runtime List identity не меняют. Ранее удалённый `SchemaFieldId` можно вернуть только с прежними Key/Type/смыслом и контейнерами; Required/Deprecated и options проверяются монотонно по всей Published history этого ID.

В MVP это включает immutable `TemporalSemantics` DateTime по F01: copy historical Naive definition сохраняет Naive и прежний ID; добавляемое Offset field получает новый ID. Naive → Offset не переинтерпретирует Published history и не переносит instance values автоматически. При cross-version Copy F04 старое отсутствующее field value остаётся orphan, новое Offset field — незаполненным; специалист вручную переносит значение и задаёт offset.

### DeliveryMode в интеграции после Demo

В Draft inspector для AssetReference leaf, в том числе recursive List ItemDefinition, показывается `DeliveryMode = OnDemand|Preload|RequiredBeforeLaunch` с default `OnDemand` по [F01](F01-create-entity-schema.md#режим-доставки-assetreference-после-demo). Published показывает значение readonly; режим нельзя менять PATCH существующей Published версии. Изменение политики требует новой версии схемы, а не редактирования MediaAsset или значения экземпляра. Возможность сохранить прежний SchemaFieldId при такой смене **Уточняется [Q34](../questions/Q34-schema-field-semantics.md#deliverymode-и-идентичность-поля)**; это ещё не разрешённое evolution rule. Serialization/profile и интеграция после Demo — [Q37](../../technical/frontend/questions/Q37-launcher-asset-delivery.md).

## Параллельные Draft и stale-base

Несколько Draft одной схемы разрешены.

Draft считается stale, когда его `BasePublishedVersionId` больше не совпадает с current latest Published version, например другой Draft уже опубликован.

Stale Draft:

- можно смотреть;
- можно редактировать и Save;
- нельзя Publish до разрешения baseline.

Automatic merge/rebase отсутствует.

Специалист либо:

1. создаёт новый Draft от current Published и переносит изменения вручную; либо
2. выполняет «Актуализировать основу»: содержимое не меняется, backend проверяет exact допустимую current Published basis, подтверждённую пользователем basis, concurrency и evolution rules и при успехе обновляет baseline. Смена basis после подтверждения даёт конфликт, а не скрытый выбор более новой версии.

## Save и concurrency

Редактирование Draft использует optimistic concurrency. Technical concurrency token не является business version.

Lost update/last-write-wins запрещены; при конкурентном изменении пользователь должен перечитать актуальный Draft и решить конфликт осознанно. Save существующей recovery work при Withdrawal разрешён с предупреждением; Publish снятой schema/target version и runtime delivery остаются запрещены.

В MVP Save проверяет полный definition snapshot, включая recursive Item constraints и List comparer/path по F01. Противоречивое или неприменимое правило отклоняет весь Save; Draft не служит хранилищем invalid definitions. Изменение constraints не обходит immutable Key/Type/container identity и monotonic Required/Deprecated. Published history не меняется. При Copy instance type-compatible value переносится по stable FieldId; нарушение target constraint сохраняется как issue, само по себе не создавая orphan (F04).

## Delete Draft

Draft SchemaVersion можно удалить физически.

Если от него зависят Draft EntityInstanceVersion:

- UI показывает точное число зависимостей;
- destructive confirmation блокируется минимум 5 секунд;
- backend повторно вычисляет exact dependency set и сравнивает его с подтверждённым набором; совпадения count недостаточно. При изменении состава удаление отклоняется, UI показывает новый impact и требует повторного подтверждения с новой 5-second задержкой;
- допустимый cascade удаляет только Draft dependencies атомарно.

Published history этим не затрагивается.

## Published lifecycle MVP

Published SchemaVersion нельзя hard-delete.

MVP поддерживает Retire/Restore:

- Retired остаётся доступной exact historical Registry/codegen;
- запрещена для нового content authoring/publish;
- historical publications продолжают работать.

Draft instances, уже созданные по retired SchemaVersion, сохраняются, но не публикуются до перехода на допустимую target version.

## Archive logical Schema MVP

Archive логической EntitySchema:

- скрывает её из обычной authoring работы;
- блокирует новые Draft/instances/new publishing;
- не каскадно меняет lifecycle field каждого Instance;
- не ломает historical Delivery.

Restore возвращает authoring availability без автоматического Publish/Activate.

## Acceptance

- несколько Draft существуют одновременно;
- Draft не получает VersionNumber до Publish;
- copy не изменяет source;
- stale Draft Save разрешён, Publish блокирован;
- «Актуализировать основу» не делает merge и не подменяет подтверждённую current basis;
- concurrent edit не теряет чужие изменения;
- Draft delete не уничтожает Published history.
- MVP constraints можно усиливать/ослаблять для прежнего FieldId без изменения Published history; invalid definitions не сохраняются;
- constraint-only change не превращает type-compatible copied instance value в orphan.
- DateTime Naive/Offset нельзя переключить под прежним FieldId; copy historical definition сохраняет её semantics, переход с новым ID сохраняет orphan/unfilled границу F04.

## Связи

[F01](F01-create-entity-schema.md) · [F03](F03-publish-schema-version.md) · [F04](F04-entity-instances.md) · [Q08](../questions/Q08-keys-and-drafts.md) · [Q09](../questions/Q09-editing-and-deletion.md) · [Q34](../questions/Q34-schema-field-semantics.md) · [Q35](../questions/Q35-value-constraints.md) · [Backend](../../technical/backend/features/F02-create-schema-version.md) · [Frontend](../../technical/frontend/features/F02-create-schema-version.md)
