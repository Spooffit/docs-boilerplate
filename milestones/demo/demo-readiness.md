# Demo: готовность документации к реализации

[Состав Demo](demo.md) · [Приёмка Demo](demo-acceptance.md) · [Каталог milestones](../README.md) · [План реализации](../../technical/backend/implementation-plan.md).

Проверка от 05.10.2026 охватывает все 11 фич Demo: F01–F05, F13, минимальную F14, F19–F22. Пользователь согласовал Q31 23–55 и последующие предложения 1–42. Решения перенесены в owner Q и нормативные F бизнеса/backend/frontend. Это оценка требований, не подтверждение работающих API/UI/SDK или пройденных runtime tests.

## Вывод

**Можно приступать к реализации Demo. Перечисленные бизнесовые развилки и основные архитектурные варианты согласованы.** [Q33 public wire v1](../../technical/backend/questions/Q33-sync-resume-transport.md#public-wire-contract-v1) дополнительно согласовал все 24 пункта Start/Data/Commit/Status: endpoints, required fields/types, envelope/payload/sequence/cursor и ошибки. Owner models/field tables/literal fixtures — [backend F13](../../technical/backend/features/F13-content-delivery.md#33-dto-и-parameter-models). Это specification, не работающий protocol/codec и не отчёт о пройденных fixtures. До независимой server/client integration нужны complete literal JSON/byte/hash fixtures, OpenAPI, общие codecs/adapters и реализация согласованного F19 public SDK/store API.

[Q33 execution/control/transport profile](../../technical/backend/questions/Q33-sync-resume-transport.md#execution-control-и-transport-profile) закрывает generation/lease/controls/recovery/TTL/Freshness. [Q29 public SDK/store](../../technical/frontend/questions/Q29-unity-client-package.md#public-sdk-и-локальный-store-согласованный-контракт) дополнительно закрывает 24 пункта public API/result/progress/typed read/store operations/scope/format и SQLite candidate. Owner models/signatures F19; literal codes/numeric settings/provider/native/OS-lock/Player и actual concurrency/crash/HTTP3→HTTP2 conformance остаются engineering результатами, не пройденными тестами.

SDK recovery: durable context/committed receipts → Status → pending receipt repeats без local reapply → Data checkpoint. Stale command не retarget fresh generation, timeout/409/Unknown не разрешают blind new ID. Stop сохраняет staging/cursor/installed и допускает окончание начатого atomic commit. Discard сначала разрешает Unknown local commit outcome, затем удаляет лишь unfinished attempt, сохраняя installed/inventory/committed receipts. One sync ограничена stable store scope между SDK instances; actual cross-process ready-store lock требует Q29/Q32 adapter proof. Preparing PlanId виден в Status; original input сохраняется, Resume завершает только первую незавершённую preparation, не recapture ready target.

Freshness — отдельный POST с полными selection/actual installedInventory, outcome `upToDate`/`changesAvailable`, `checkedAt` UTC и sorted exact `changedGroups`, без plan/lease. Failed comparison оставляет SDK Unknown, не rollback и не autosync. Раздельные unary/header/read-idle/record-assembly/server-write-backpressure/preparation/receipt-wait budgets и ожидаемые local validation/commit/receipt паузы согласованы. Первый Demo stream без compression/heartbeat. Durations/renew/buffers/capacity/tombstones startup-validated, renewInterval меньше leaseDuration; numeric sizing после representative/load checks, значения не взяты из source examples.

Первый Player выбран: Unity 2022.3.62f3, Windows x64/IL2CPP. Stripping/build settings, Text/performance limits, package/native versions и SQLite совместимость фиксируются/проверяются representative fixtures/spikes. SQLite — выбранный adapter примера, его native/AOT support пока не доказан.

Demo CM — один процесс: bounded in-memory plan store, absolute TTL от reservation default1h/no extension для preparing/streaming/stopped; active plans не capacity-evicted произвольно, новый Start при заполнении недоступен. Known expired/discarded bounded tombstone даёт 410, unknown/restart/evicted tombstone — 404. Restart CM теряет plan, SDK создаёт новый от actual inventory после подтверждения недоступности прежнего; outage Status не доказывает отсутствие. AssetId reference/delete guard тоже работает в одном процессе. Multi-replica требует другого durable coordination/store контракта и не объявляется поддержанным.

## Согласованные пункты 1–42

| Пункты | Принятые границы | Владелец |
| --- | --- | --- |
| 1–2 | Restricted sanitized HTML Text: paragraphs/br/lists/bold/italic, semantic empty после очистки, server configurable max/UI hints, число после fixture | [Q34](../../business/questions/Q34-schema-field-semantics.md), F01/F04/F21 |
| 3–4 | Clean Schema Draft локален до first valid Save; Copy resource сразу. Changed delete impact reject/reconfirm exact set, schema cascade заново ждёт 5 секунд | [Q09](../../business/questions/Q09-editing-and-deletion.md), F01/F02/F04 |
| 5 | Withdrawn context historical read/Copy/Save recovery allowed; Publish по withdrawn target/runtime blocked | Q09/[Q36](../../business/questions/Q36-entity-instance-lifecycle.md), F02/F04 |
| 6–7 | Historical FieldId return same Key/Type/meaning, monotonic all-history; definition-container chain identity, move new ID | Q34/F02/F03/F04 |
| 8–9 | Save missing required/preserved incompatible/orphans allowed, NEW type-invalid rejected; Accept exact current basis/no payload change/missing-orphans allowed | Q36/F04 |
| 10–13 | Last Instance Draft delete leaves parent; explicit return to delivery, Publish not clearing withdrawal; declared deprecated options valid/warn; different-target Publish short serialized sequence allocation | Q36/F04/F13 |
| 14–15 | Explicit authoring Scalar/Object/List, named FieldId/anonymous item; Instance Save full desired snapshot, optional absence clear, orphan explicit discard, no Demo PATCH | [Q15](../../technical/backend/questions/Q15-api-and-source-code.md), F01–F04 |
| 16–18 | Client opaque ItemKey per List<Object> occurrence/copy/reorder; persisted orphan stable ID/computed token exact basis, paths FieldId/ItemKey; own ETag distinct diagnostics basis | Q15/F04/frontend |
| 19–20 | Owner-BC operation ID/durable create replay, same input original result/mismatch conflict; common stable code/message/address issues, list conventions, NSwag and binary ZIP success/error | Q15/[Q20](../../technical/backend/questions/Q20-persistence-and-migration.md), F01–F05/F22 |
| 21–23 | Schema hash semantic definition without labels/publication metadata; instance hash values; versioned canonical UTF-8 no BOM/no insignificant whitespace, stable FieldId/GUID/Flag order, no hidden normalization/no double long-decimal conversion | [Q19](../../technical/backend/questions/Q19-content-hashes.md), F03/F04/F13/F21 |
| 24–25 | DateTime yyyy-MM-dd'T'HH:mm:ss.fffffff Unspecified/7digits/noZ-offset; AssetReference captured Publish/runtime values/hash, no live Delivery enrichment/serialized URL | Q19/[Q31](../../technical/backend/questions/Q31-schema-code-generation.md), F04/F13/F19/F21/F22 |
| 26 | Separate group/inventory/plan profiles: full sorted target membership, actual installed fingerprint, original request/exact targets; removals in plan, target validated after apply | Q19/F13/F19 |
| 27–30 | NDJSON application/x-ndjson UTF-8/LF; ProtocolVersion/PlanId/RecordSequence envelope/6 variants; EntityRemove records; opaque last-durable-sequence cursor, identical replay dedup/conflicting error | [Q33](../../technical/backend/questions/Q33-sync-resume-transport.md), F13/F19 |
| 31–34 | Wait receipt before next group; plan/group/hash receipt sameACK/different conflict; single-process bounded in-memory absolute1hTTL/restart loss/lease fencing; readonly freshness/transient retry/invalidation-or-truncation no success | Q33/[Q30](../../technical/backend/questions/Q30-content-synchronization.md), F13/F19 |
| 35–37 | Single-process AssetId guard all reference/delete writers through owner commit; eligibility port before YARP, no routing lookup; Pending/Failed hidden/upload recovery; durable Deleting/idempotent storage retry, Draft/Published/orphan reference authority | [Q21](../../technical/backend/questions/Q21-media-storage.md)/Q20, F04/F05/F13 |
| 38–39 | SDK interface + ready Demo store, SQLite candidate/spike, stage/checkpoint and installed/inventory/receipt atomicity; oneSync/Stop/Discard/typed binding/stage-count progress/main-thread/disposable resource ownership | [Q29](../../technical/frontend/questions/Q29-unity-client-package.md)/[Q32](../../technical/frontend/questions/Q32-sdk-package-distribution.md), F19/F20 |
| 40 | Pin Unity 2022.3.62f3 patch/platform/Player/backend before spike; all15/twoexact/nested/image + sync/recovery/withdrawal/freshness show | [Q26](../../business/questions/Q26-demo-mvp-scope.md), F20/Q32 |
| 41–42 | Literal naming/escaping/suffix/ZIP/shared-type fixtures before emitter; unresolved GUID8 collision all-error; unified real PostgreSQL/media/validator/compiler/recovery/Player acceptance | Q31/F22/[матрица приёмки](demo-acceptance.md) |

Clean-local-first относится к Schema Draft, не запрещает persisted empty/missing-required Instance Draft. Retired/Archive MVP не подменяют Demo Withdrawn recovery. CLI, advanced constraints/List comparator, expanded F14, Addressables/F23, Identity/licenses/broker/VR School/админ-проверка/полный Publishing не условия Demo.

## Q31: принятые пункты 23–55

Сохраняются exact Registry GET/raw Draft2020-12 cm-json-schema/1 и C# POST IDs-only → cm-csharp/1 Roslyn/Newtonsoft → complete deterministic content-manager-csharp-models.zip. Empty/duplicates400, all-or-error/coverage, frozen SchemaKey/multiple versions, canonical Guid root order, nested key-path naming/identity suffix/no shape dedup; UTF-8 no BOM/LF/final newline, fixed ZIP metadata/compression/ordinal entries, stateless artifacts/cancel/guardrails.

Registry ETag отдельно от F03 ContentHash; semantic profile отдельно от package SemVer, byte-changing bugfix новый profile. UI profile/options не выбирает, ZIP не требует diagnostics manifest; Info/Warning остаются Core/server diagnostics. Literal source/ZIP/JSON Schema fixtures, real validator/compile/round-trip/F19/F20 use обязательны, CLI reusable Core MVP. Owners: [Q31](../../technical/backend/questions/Q31-schema-code-generation.md), [F21](../../technical/backend/features/F21-schema-registry.md), [F22](../../technical/backend/features/F22-csharp-model-generation.md), [M07](../../technical/backend/modules/M07-schema-tooling.md).

[Q29 raw/image contract](../../technical/frontend/questions/Q29-unity-client-package.md#raw-assets-и-unity-image-helper-согласованный-контракт) закрывает ещё 24 пункта: owned stream/Completion, full expected size/SHA, explicit Range/If-Range, caller destination и own temp no-overwrite file commit, verified JPEG/PNG bounded decode/Unity handle ownership. Models/signatures/flows F19, consumer F20; no extra SDK cache/persistent asset resume. Numeric image/transport/error caps/literal registry и real CM/YARP/MinIO/Player lifecycle/leak/memory/cancellation conformance остаются required engineering results, не доказаны документом.

[Q32 SDK release policy](../../technical/frontend/questions/Q32-sdk-package-distribution.md#согласованная-поставка-и-release-policy-sdk) закрывает 24 пункта поставки: separate SDK repo/DotNet DLL+NuGet-UPM/Unity source+asmdef/Store.Sqlite artifact, selected IDs/registry scope/project-level path, aligned immutable base train and same-build tarball fallback, packaged F20 project. First Player Windows x64/IL2CPP; Unity 2022.3.62f3/stripping/native/dependency and actual GitLab/registry/CI permissions must pin/verify before support claim. AOT preservation without historical .g.cs edits, no duplicate DotNet/Newtonsoft, real clean install/Player before complete release. Repo creation/publication/Player not performed here.

[Q26 F20](../../business/questions/Q26-demo-mvp-scope.md#f20-экран-и-границы-показа) закрывает все 18 пунктов сцены: каталог demo_exhibit/all15/twoexact/nested, uGUI/TextMeshPro/RawImage, Inspector+explicit generated bindings, explicit sync без auto-start, Stop/Discard/image retry, SQLite persistent path и offline committed read. Owner fixture — business F20, screen/state/flows — frontend F20. Приёмка real packaged Windows IL2CPP + CM/PostgreSQL/YARP/S3, fault checks controlled harness; implementation/runtime не подтверждены записью решения.

## Что ещё конкретизируется при реализации

| Инженерный результат | Владелец | Когда нужен |
| --- | --- | --- |
| Exact DTO/discriminator/member casing, clear/orphan/basis/operation-ID/header transport, list defaults, issue codes/paths и OpenAPI/NSwag literals | Q15/F01–F05/F22 | До editor/API integration |
| Chosen six canonical profiles/SHA-256/64 lowercase hex/string/numeric rules; complete runtime/group/inventory/request/plan fixture corpus and real shared-library conformance | Q19/F03/F04/F13 | До Published/SDK interop; current helper не oracle |
| Public wire v1 + execution/control/Freshness semantics выбраны; complete literal fixtures/common reason registry/API mapping/OpenAPI, controlled-clock/barrier/race checks и real codec/transport conformance ещё требуются. Numeric stage/lease/buffer/capacity settings startup-validated после representative sizing | Q33/Q15; owner models F13 | До server/SDK protocol integration |
| Selected F19 public/store and raw/texture contracts: bindings/handles/results/scope/SQLite conformance plus stream Completion/destination/temp-file/Range/verified JPEG-PNG/main-thread ownership. Literal codes/numeric caps/native-provider/platform checks remain | Q29/Q32/F19 | До consumer/sample integration |
| Identifier sanitizer/suffix/escaping/ZIP settings и shared helper API, source/ZIP/compiler fixtures | Q31/F22 | До emitter/Unity compile |
| Numeric Text/upload/retry/performance caps, Unity 2022.3.62f3/stripping/package/native lock for selected Windows x64/IL2CPP and actual GitLab context | Q22/Q26/Q29/Q32/Q34 | До startup-validated deployment/load/Player acceptance |

Эти результаты закрепляются единым reviewable contract/fixtures, не случайным serializer default или вторым несовместимым UI/SDK mapper. Numeric limits не назначаются из source-примеров.

## Порядок реализации

1. Согласованные domain invariants/public boundaries F01–F05; параллельно concrete authoring/OpenAPI и расширение [literal canonical samples](../../technical/backend/canonical-v1-vectors.md) по выбранному Q19 profile.
2. SQLite/Unity package/Player/transport feasibility на pinned окружении, результат в Q29/Q32/Q33; кандидат не называется проверенным раньше spike.
3. F21/F22 по formal/source fixtures/shared SDK contract; all-type compile/round-trip.
4. F13/F19 по одному NDJSON/recovery contract; реальные process-crash/lostACK/expiry/withdrawal checks.
5. F20 и [приёмка Demo](demo-acceptance.md). Documentation/static search/fixture doubles не подтверждают весь работающий контур.
