# M01. Моделирование контента: интерфейс
## Назначение
Основной UX модуля — versioned workspace, а не цепочка CRUD-таблиц.
Пользователь должен одинаково понимать работу со Schema и EntityInstance:
- найти logical object;
- выбрать Draft/Published version;
- редактировать Draft;
- увидеть validation/stale/concurrency state;
- Publish либо выполнить lifecycle action.
## Навигация Demo
```text
Home
Контент
  Схемы
  Экземпляры
  Ресурсы
  Генерация моделей
```
Основные routes:
```text
/schemas
/schemas/new
/schemas/{schemaId}
/schemas/{schemaId}/versions/{versionId}
/schemas/{schemaId}/instances


/instances
/instances/new
/instances/{instanceId}
/instances/{instanceId}/versions/{versionId}
```
Route identity использует immutable ID.
## Schema Workspace
Композиция desktop:
```text
Header / breadcrumbs / actions
┌───────────────┬──────────────────────────────────┐
│ Version rail  │ selected version                 │
│ Draft...      │ tree + field inspector           │
│ Published...  │ validation / read-only metadata  │
└───────────────┴──────────────────────────────────┘
```
Version rail:
- Draft выше Published;
- Draft newest-first;
- Published VersionNumber descending;
- Draft показывает Title и authoring context.
Published version read-only.
## Schema editor
Основной pattern:
- tree fields;
- inspector selected field;
- add root/child actions;
- type-specific controls;
- inline validation.
Existing field Key/Type не редактируются.
В MVP DateTime inspector показывает fixed `TemporalSemantics`: historical Naive и new Offset definitions по F01. Смена semantics требует нового FieldId F02; mutable selector на существующем field отсутствует.
Deprecated fields остаются видимыми и сортируются после non-deprecated siblings только на UI.
В MVP inspector задаёт typed constraints конкретной definition и recursive Item constraints/List comparer по [F01](../features/F01-create-entity-schema.md)/[Q35](../questions/Q35-value-constraints.md). Invalid definitions отклоняют Save; опубликованный snapshot не редактируется. Demo сохраняет прежнюю глубину без конструктора дополнительных constraints.
## Instance Workspace
Повторяет Schema Workspace:
- logical identity/header;
- versions rail;
- selected version editor/read-only view.
Каждая version показывает SchemaVersion context.
Dynamic form:
- scalar controls;
- Object sections;
- recursive List editor;
- Enum single select;
- FlagEnum multi-select;
- Color picker + `#RRGGBBAA`;
- Asset picker.
Orphan values показываются отдельной секцией и не скрываются.
В MVP type-valid нарушения дополнительных constraints сохраняются с адресуемыми issues, блокирующими Publish; constraint-only change не превращает скопированное совместимое value в orphan. Явное «Сортировать» стабильно меняет локальный editable Draft по comparer F04, затем обычный Save с исходным If-Match; Published read-only, missing/invalid key делает sort недоступным.

Для MVP Offset DateTime форма раздельно редактирует date/time и offset, начинает новый value с видимого `+00:00` и показывает derived UTC instant. При смене offset date/time сохраняются, instant меняется; UI объясняет это действие. Original offset и 100ns fractional seconds сохраняются при Save/read, открытии формы и изменении других fields; fractional seconds имеют явный control. Browser timezone binding, региональные зоны и автоматический DST отсутствуют. Published runtime view показывает canonical UTC, authoring view — original offset. Naive → Offset Copy оставляет старое value orphan и новое field незаполненным: специалист вручную переносит значение и задаёт offset по F04.
## Save / Publish / stale-base
Demo использует explicit Save, без autosave.
Dirty navigation защищена guard.
Publish:
- не делает скрытый Save;
- использует current ETag;
- различает validation, `412` concurrency и stale-base domain conflict.
Stale Draft можно продолжать редактировать; UI предлагает создать новый Draft либо «Актуализировать основу» без automatic merge.
## NSwag и ETag
NSwag-generated DTO используются напрямую.
Технический `Version` в DTO отсутствует.
Exact mutable GET возвращает strong `ETag`, который page/feature хранит рядом с DTO как transport state.
Mutation отправляет `If-Match`.
После `412` UI не повторяет mutation автоматически: перечитывает current representation и показывает конфликт.
## Archive MVP
Обычные списки показывают только active objects.
Отдельный navigation block `Архив` содержит Schema/Instance/Asset.
Direct link archived object остаётся рабочим и открывает read-only workspace с Restore action, если lifecycle разрешает Restore.
## FSD
```text
Pages
  schema-workspace
  instance-workspace


Widgets
  version-rail
  schema-editor
  instance-editor
  contextual-instances


Features
  create-draft
  save-draft
  publish-version
  delete-draft
  accept-current-base
  archive / restore / retire


Entities
  schema
  schema-version
  instance
  instance-version
```
Shared содержит только generic UI/API infrastructure.
## Карта фич
- [F01](../features/F01-create-entity-schema.md)
- [F02](../features/F02-create-schema-version.md)
- [F03](../features/F03-publish-schema-version.md)
- [F04](../features/F04-entity-instances.md)
## Связи
[Q13](../../technical/frontend/questions/Q13-ui-scope.md) · [S06 API client](../../technical/frontend/standards/S06-api-client.md) · [M02 Media](M02-media-library.md) · [M07 Tooling](M07-schema-tooling.md).
