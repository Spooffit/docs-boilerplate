# M07. Schema Registry и Code Generation

## Назначение

M07 — developer/tooling boundary над immutable Published SchemaVersion ContentModeling.

Он не является отдельным bounded context и не владеет persistence.

Цепочка:

```text
ContentModeling Published SchemaVersion
→ F21 Schema Registry / JSON Schema
→ Schema.CodeGeneration.Core
→ F22 HTTP ZIP / MVP CLI
```

## F21 Schema Registry

Registry:
- читает exact Published schema через public ContentModeling port;
- экспортирует один agreed JSON Schema profile;
- не хранит второй редактируемый catalog;
- не выводит schema из EntityInstance data;
- не подставляет latest;
- не зависит от C# generator.

Source/document models определены один раз в [F21](../features/F21-schema-registry.md#3-applicationabstractions): exact IDs, integer Published number, frozen name/key/source hash и формальный document. Source adapter и query handlers находятся в owner ContentModeling.EntityFramework.Persistence; exporter — pure Application service. Module не создаёт второй DTO или редактируемый Registry catalog.

Registry profile закрыт: JSON Schema Draft 2020-12 + `cm-json-schema/1`, ID-based `$id`, `x-cm-*` semantics и deterministic raw representation.

## F22 Code Generation

`Schema.CodeGeneration.Core` — pure reusable library.

Demo:
```text
Admin UI exact versions
→ server Core
→ ZIP *.g.cs
```

MVP:
```text
cm-schema restore + content-manager.json
→ Registry → locked original documents
→ cm-schema generate (offline) → same Core → owned project sources
→ cm-schema check (readonly) → compile
```

HTTP wrapper и CLI не создают собственные mapping rules.

## Dependencies

### ContentModeling
Public read-port exact immutable Published schema contracts.

### Delivery
Нет runtime dependency. F21/F22 contracts должны быть совместимы с F13 payload, что проверяется end-to-end tests.

### SDK
Generated source используется Sdk.Client.DotNet/Unity consumer, но generator Core не зависит от SDK implementation.

## Persistence

Собственных tables/DbContext/migrations нет.

Generated ZIP/history сервер не хранит.

Optional cache допускается только как техническая оптимизация keyed exact schema/profile, не source of truth.

## API

F21: `GET /api/v1/schema-registry/schema-versions/{schemaVersionId}` → raw `application/schema+json`; F22 internal batch без HTTP loopback, public batch Demo не нужен. Exact error codes remain Q15.

F22: Demo `/code-generation` UI вызывает `POST /api/v1/code-generation/csharp` с exact IDs и получает deterministic complete binary ZIP `content-manager-csharp-models.zip`. Technical GenerateCSharpModelsCommand handler — Application, source reads — F21 Persistence, Core/ZIP/CLI adapters не содержат второго mapper-а. [F22](../features/F22-csharp-model-generation.md) владеет моделями, stream lifecycle и алгоритмами.

F22 binary ZIP response не использует JSON envelope.

## Profile

F21/F22 сохраняют source integrity pair `cm-schema-content/1` + SHA25664lowerhex без пересчёта, используя neutral [F03 contracts](../features/F03-publish-schema-version.md#33-dto-и-parameter-models). Source semantic hash, raw Registry representation ETag и generated-file/ZIP bytes имеют разные scopes; annotation changes могут менять Registry ETag при прежнем semanticHash. Canonical content no-final-LF не изменяет отдельно выбранный `cm-csharp/1` source LF+final newline или `cm-json-schema/1`.

Q31 Demo profiles и MVP CLI configuration/commands/pins/ownership/recovery согласованы. CLI package ContentManager.Schema.CodeGeneration.Cli, command cm-schema; модели/потоки единственный owner [F22](../features/F22-csharp-model-generation.md#82-mvp-cli). Открыты exact codecs/errors/exit/journal fixtures, actual tool/filesystem/feed checks Q32, developer auth Q24 и новые MVP profiles Q19/Q31; numeric guardrails sizing отдельно.

DateTime exact naive format и Publish-captured AssetReference values закреплены Q19/Q31. Literal formal/source fixtures, exact sanitization/escaping/collision suffix/ZIP settings и shared SDK CLR API материализуются engineering contract до emitter/integration; Unity installation/Player compatibility доказывается реальными проверками. Schema semantic ContentHash не Registry representation ETag и не ZIP/source digest.

## Delivery policy после Demo

F01–F03 фиксируют mode exact recursive AssetReference definition, а F21 экспортирует его как versioned formal annotation для tooling. Режим не входит в JSON instance value и frozen five-field descriptor, не становится глобальным generated AssetReference CLR property. F22 остаётся formal consumer, launcher requirements получает из F13 resolved manifest одной revision, не Registry latest. Concrete extension/profile/integrity codec [Q37](../../frontend/questions/Q37-launcher-asset-delivery.md)/Q31 пока **Уточняется**; historical `cm-schema-content/1`, `cm-json-schema/1`, generated/source hashes не переписываются. New-profile golden documents и F21→F22 compilation/round-trip acceptance предшествуют implementation, это не новый Demo prerequisite.

## Feature map

[F21](../features/F21-schema-registry.md) · [F22](../features/F22-csharp-model-generation.md)

## Связи

[ContentModeling M01](M01-content-authoring.md) · [Delivery M05](M05-delivery.md)
