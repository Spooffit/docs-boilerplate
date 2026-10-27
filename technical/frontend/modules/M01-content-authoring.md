# M01. Моделирование контента
## Назначение
Модуль объединяет работу со структурой динамического контента и данными экземпляров.
Специалист:
- создаёт логическую EntitySchema;
- готовит одну или несколько Draft SchemaVersion;
- публикует immutable SchemaVersion;
- создаёт логические EntityInstance;
- готовит и публикует их версии по конкретным SchemaVersion.
Authoring content существует независимо от Product/ProductVariant. Попадание Published content в клиентскую поставку начинается только в Publishing.
## Основные бизнесовые объекты
### EntitySchema
Логический тип контента со stable identity, mutable Name/Description и immutable Key и историей immutable Published versions.
Schema может иметь несколько параллельных Draft.
### EntitySchemaVersion
Snapshot структуры Schema.
Draft имеет ID, business VersionNumber не имеет, может редактироваться и может быть stale относительно более новой Published base.
Published получает монотонный integer VersionNumber, immutable и используется Registry/codegen и EntityInstance versions.
### EntityInstance
Логический объект данных внутри EntitySchema.
Имеет stable immutable `EntityInstanceKey` и mutable authoring Name.
### EntityInstanceVersion
Snapshot данных конкретного EntityInstance по конкретной SchemaVersion.
Один EntityInstance может иметь versions по нескольким SchemaVersion.
## Schema evolution
Stable `SchemaFieldId` сохраняет identity поля между versions.
Для существующего field immutable:
- Key;
- FieldDataType;
- type-specific contract semantics.
Разрешены согласованные monotonic изменения:
- Name;
- HelpText;
- Required true → false;
- Deprecated false → true;
- удаление поля из новой version.
Новый contract key/type/sense оформляется новым field ID.
Полная семантика типов — Q34. Advanced constraints — Q35/MVP.
## Draft и stale-base
Несколько Draft одной Schema/Instance разрешены.
Если другой Draft опубликован раньше, старый Draft не удаляется и остаётся editable, но Publish блокируется до осознанного разрешения stale-base.
Clean SchemaDraft локален до первого valid Save, Copy создаётся сразу на сервере. Save хранит full snapshot и разрешает incomplete required/preserved incompatibility/orphans; новые type-invalid values отвергаются. Accept меняет только approved basis; own ETag и diagnostic basis разделены. Changed exact delete impact требует отказа/нового consent и повторных 5 секунд SchemaCascade. Withdrawal допускает read/Copy/Save, блокирует Publish/runtime до explicit ReturnToDelivery с Published проверкой. Automatic merge отсутствует.
## Published numbering
SchemaVersion и EntityInstanceVersion получают business number только при успешном Publish.
Нумерация — `1, 2, 3...`.
Draft number не имеет.
## Assets
Instance content хранит `AssetId`, а не storage path/URL.
Published descriptor фиксируется при Publish, ContentHash не содержит URL; bytes читаются отдельно. Asset lifecycle принадлежит M02 MediaLibrary.
## Lifecycle
Demo:
- Draft можно удалять;
- Published history не удаляется;
- withdrawal поддерживается в согласованных scopes.
MVP:
- logical Schema/Instance получают Archive/Restore;
- Published versions могут Retire/Restore для запрета нового использования без разрушения истории.
## Карта фич
| ID | Результат |
| --- | --- |
| [F01](../features/F01-create-entity-schema.md) | новая Schema + initial Draft |
| [F02](../features/F02-create-schema-version.md) | create/copy/edit/delete/retire SchemaVersion |
| [F03](../features/F03-publish-schema-version.md) | immutable Published SchemaVersion |
| [F04](../features/F04-entity-instances.md) | EntityInstance и versioned data |
## Связи
[Publishing M04](M04-publishing.md) использует Published content. [Media M02](M02-media-library.md) владеет Asset lifecycle. [M07](M07-schema-tooling.md) экспортирует Published schema contracts.
Решения и альтернативы: [Q08](../../../business/questions/Q08-keys-and-drafts.md) · [Q09](../../../business/questions/Q09-editing-and-deletion.md) · [Q34](../../../business/questions/Q34-schema-field-semantics.md) · [Q35](../../../business/questions/Q35-value-constraints.md) · [Q36](../../../business/questions/Q36-entity-instance-lifecycle.md).
