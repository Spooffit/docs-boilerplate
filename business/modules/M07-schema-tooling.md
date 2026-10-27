# M07. Контракты схем и генерация моделей

## Назначение

M07 — developer/tooling-контур поверх immutable Published schema contracts ContentModeling.

Он не является отдельным bounded context и не хранит второй каталог схем.

Функции:

- F21 экспортирует exact Published EntitySchemaVersion в формальный JSON Schema contract;
- F22 генерирует C# source из exact contract set;
- Demo предоставляет UI → backend generation → ZIP `.g.cs`;
- MVP обязательно добавляет CLI, использующий тот же Registry contract и тот же generator core.

Runtime Delivery/installed inventory/Addressables mapping не являются частью M07.

## Проекты

Целевая структура:

```text
ContentManager.SchemaRegistry.Application.Abstractions
ContentManager.SchemaRegistry.Application
ContentManager.SchemaRegistry.Api

ContentManager.Schema.CodeGeneration.Core
ContentManager.Schema.CodeGeneration.Cli        // MVP
```

Отдельные Domain, EntityFramework и Persistence проекты для Registry/codegen не нужны: собственных aggregate roots и таблиц нет.

## F21 Schema Registry

Source of truth — ContentModeling.

Registry читает exact immutable Published schema через public ContentModeling port:

```text
ISchemaContractSourceQueries
  GetPublishedSchemaVersion(id)
  GetPublishedSchemaVersions(ids)
```

Registry не получает ContentModeling DbContext/repository и не импортирует его Domain assembly как внешний contract.

`IJsonSchemaContractExporter` является pure application service и переводит Published contract source в согласованный JSON Schema profile.

Exact contract identity сохраняет:

- EntitySchemaId;
- EntitySchemaVersionId;
- Published VersionNumber;
- historical SchemaKey/SchemaName snapshot;
- ContentHash;
- stable SchemaFieldId;
- immutable field Key/Type semantics;
- Required/Deprecated;
- Enum/Flag keys;
- recursive Object/List;
- AssetReference contract;
- согласованные constraints/profile annotations.

Published Retired version остаётся доступной по exact historical ID для tooling. Draft не выдаётся как stable contract.

Formal profile закрыт Q31: JSON Schema Draft 2020-12, `cm-json-schema/1`, ID-based `$id`, self-contained document, `x-cm-*` extensions, deterministic representation и exact raw Schema API.

Для интеграции Launcher после Demo policy `DeliveryMode` сохраняется на AssetReference leaf exact schema (включая ItemDefinition) и должна иметь versioned formal metadata carrier. Registry остаётся language-neutral; metadata не становится member generated runtime AssetReference и не заменяет Delivery manifest. Exact annotation/canonical profile и fixtures **Уточняется [Q37](../../technical/frontend/questions/Q37-launcher-asset-delivery.md)** вместе с Q19/Q31. Historical `/1` bytes/profile/hash и пять runtime полей сохраняются; absent mode трактуется как OnDemand без re-export/rehash.

## F22 Code Generation

`ContentManager.Schema.CodeGeneration.Core` — stateless pure library:

```text
ICSharpSchemaCodeGenerator
  Generate(inputs, options)
```

Input — exact F21 contracts. Output — deterministic `GeneratedSourceFile[]` + diagnostics.

Core не зависит от:

- ASP.NET Core;
- EF Core;
- ContentManager server Domain projects;
- NSwag;
- Unity runtime assemblies;
- S3/RabbitMQ/VR School;
- Web UI.

Одинаковый contract/options должен давать детерминированный source content.

## Demo

Flow:

```text
Admin UI exact SchemaVersion selection
→ POST /api/v1/code-generation/csharp
→ F21 batch contract query
→ Schema.CodeGeneration.Core
→ ZIP *.g.cs
→ browser download
```

Demo не содержит CLI/build configuration/Unity Editor integration.

ZIP — binary `application/zip`, не `ApiResponse<byte[]>`.

Если хотя бы один exact contract unavailable/unsupported, генерация всего selection завершается ошибкой; partial ZIP с молча пропущенной схемой запрещён.

## MVP CLI

CLI **обязателен в MVP** и является отдельным adapter над тем же contract/core:

```text
project dependency config
→ Schema Registry API/client
→ Schema.CodeGeneration.Core
→ write/update *.g.cs in consumer project
```

CLI не вызывает административный Web UI и не реализует второй generator algorithm.

Точный MVP manifest/config filename, CLI commands, distribution/package и build/CI integration остаются отдельным tooling/Q32 follow-up; Demo profile от этого не зависит.

Unity Editor plugin/source-generator/CI wrapper могут появляться как дополнительные adapters, но не считаются автоматически отдельными обязательными продуктами MVP.

CLI выбран как local .NET tool ContentManager.Schema.CodeGeneration.Cli / cm-schema. content-manager.json + lock/raw formal inputs, restore/offline generate/readonly check и owned output recovery описаны в [F22](../features/F22-csharp-model-generation.md#mvp-cli-workflow); не runtime inventory или build/editor integration. Actual tool install Q32, console authorization Q24 и новые MVP profiles остаются отдельными.

## API

Registry exact contract:

```text
GET /api/v1/schema-registry/schema-versions/{schemaVersionId}
```

F22 использует internal Application batch без HTTP loopback. Public batch Registry endpoint в Demo не вводится.

Code generation Demo:

```text
POST /api/v1/code-generation/csharp
```

No ETag/If-Match: operations читают immutable Published contracts и не меняют persistent resource.

## Persistence

M07 не имеет собственных таблиц/DbContext/migrations.

Generated files и generation history не сохраняются в серверной БД без будущего отдельного требования.

## Связи

ContentModeling M01 — source contracts.

Delivery M05/F19 — runtime consumer generated models, но M07 не читает runtime data.

F20 Demo scene подтверждает compile/deserialization результата.

[Фича F21](../features/F21-schema-registry.md) · [Фича F22](../features/F22-csharp-model-generation.md) · [Q31](../../technical/backend/questions/Q31-schema-code-generation.md) · [Q32](../../technical/frontend/questions/Q32-sdk-package-distribution.md).
