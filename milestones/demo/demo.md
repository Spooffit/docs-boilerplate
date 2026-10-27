# Demo


[Каталог milestones](../README.md) · [Следующий milestone: MVP](../mvp/mvp.md).

[Проверка готовности к реализации](demo-readiness.md) содержит согласованные решения 1–42 и оставшуюся инженерную детализацию. [Матрица приёмки](demo-acceptance.md) задаёт обязательные fixtures/окружение/результаты; её наличие не означает пройденные tests.


## Цель


Показать минимальную работающую цепочку самого CM, проверить требования и гипотезы вместе с заказчиком. Схемы, экземпляры/версии, реальные media bytes, HTTP-синхронизация, Unity SDK/сцена и UI-генерация C# должны демонстрировать заявленный результат. Заглушки внешнего окружения допустимы; они не заменяют эту цепочку и не доказывают готовность интеграций.


## Входящие фичи


| Фича | Модуль | Объём Demo |
| --- | --- | --- |
| [F01. Создание схемы](../../business/features/F01-create-entity-schema.md) | M01 | Все 15 согласованных типов FieldDataType; структура и поля, стабильные identities, редактирование Draft |
| [F02. Версии схемы](../../business/features/F02-create-schema-version.md) | M01 | Создание/копирование/редактирование Draft, несколько версий, допустимое удаление черновиков; orphan authoring data сохраняются до исправления |
| [F03. Публикация схемы](../../business/features/F03-publish-schema-version.md) | M01 | Обязательный Draft → Published; immutable структура и точный version context, без F17 |
| [F04. Экземпляры и версии](../../business/features/F04-entity-instances.md) | M01 | Stable immutable per-schema key, mutable authoring Name; несколько Draft без номера, общий published sequence экземпляра; заполнение/сохранение/публикация всех применимых типов, разрешённый physical delete и withdrawal |
| [F05. MediaLibrary](../../business/features/F05-media-assets.md) | M02 | MinIO via S3-compatible IObjectStorage; AssetKind Image/Audio/Video; backend format/size policy → dynamic frontend hints; reusable catalog with search/page/kind/format/date/Used/Schema/status filters; compatible picker/upload; guarded delete; `/storage/assets/{id}` YARP bytes, HTTP validators/Range; real image shown in Unity |
| [F13. Доставка и sync](../../business/features/F13-content-delivery.md) | M05 | Полный exact schema/version registry и actual inventory; immutable plan, Add/Update/Remove, atomic apply целой группы, durable resume, Stop/Discard и отдельная freshness |
| [F14. Минимальная совместимость](../../business/features/F14-client-compatibility.md) | Сквозная | Exact поддерживаемая структура и версионирование; несколько версий не схлопываются, расширенная совместимость/преобразование на лету не входят |
| [F19. Unity SDK](../../business/features/F19-unity-client.md) | M05 | Самостоятельная DotNet/Http/Unity библиотека для Unity 2022.3.62f3; typed data, один high-level sync, client selections/inventory/storage, progress/cancellation/configurable retries, отдельный raw image/file API |
| [F20. Сцена Unity](../../business/features/F20-unity-demo-scene.md) | M05 | Небольшая отдельная сцена команды CM для команды Unity; публичный SDK F19, реальные версии/изображение, запуск sync и отображение результата; runtime SDK не зависит от сцены |
| [F21. Schema Registry](../../business/features/F21-schema-registry.md) | M07 | Exact Published → raw JSON Schema Draft 2020-12 `cm-json-schema/1`; ID-based `$id`, deterministic representation/ETag, public exact GET, internal batch F22 |
| [F22. Генерация C#](../../business/features/F22-csharp-model-generation.md) | M07 | Exact selection → `POST /api/v1/code-generation/csharp` → Roslyn `cm-csharp/1`/Newtonsoft.Json → deterministic `content-manager-csharp-models.zip`; all-or-error, multiple versions compile together |
| [F23. Unity Addressables](../../business/features/F23-unity-addressables.md) | M05 | Отдельный UPM add-on поверх F19; включён в Demo решением владельца 11.10.2026, реализуется отдельной фичей; основной SDK не зависит от Addressables |

## Технические границы проверки


Sync использует compact Start JSON → один NDJSON application/x-ndjson UTF-8/LF response → отдельные group commits. Envelope ProtocolVersion/PlanId/RecordSequence, explicit GroupStarted/EntityUpsert/EntityRemove/GroupCompleted/OperationCompleted/OperationInvalidated. SDK durably stages полный record до opaque cursor; identical replay dedup/conflicting sequence error. Atomic data/removals/inventory/receipt; server ждёт receipt plan/group/hash перед следующей группой, endpoint independent от stream lease. Lost ACK не повторяет local apply. SSE/chunk GET API нет.


DotNet генерирует synchronizationId и автоматически продолжает пригодную попытку после client process restart. При доказанной невозможности resume новый plan от actual inventory/newID сохраняет committed группы; timeout не expiry proof. CM один process, bounded in-memory plan store, absolute CreatedAt TTL default1h configurable/no stream extension; CM restart теряет plan. Lease/generation fencing rejects late writer. SDK даёт store interface + ready Demo adapter, SQLite первый кандидат требует native/AOT/platform spike. Stop retains resume/Discard unfinished-only/started atomic commit may finish.


После полного успеха, включая no-change, отдельный preflight сообщает актуальность/изменения; ошибка проверки означает Unknown, не rollback данных. Следующую sync после успеха выбирает приложение. HTTP/3 предпочтителен, HTTP/2 fallback сохраняет тот же ID/plan/checkpoint; фактическая Unity/proxy/framing совместимость требует проверки.


Media read и sync stream разделены. Application owns upload/catalog/metadata/delete; gateway public MediaLibrary eligibility lookup до YARP/private MinIO по `/storage/assets/{assetId}`, path computed Guid/no routing ObjectKey lookup. AssetId — единственная identity. SHA-256 ETag/UploadedAt LastModified/conditional304/configurable cache default24h, Audio/Video Range206/416. One-process AssetId guard охватывает all reference/delete writers до owner commit; Pending/Failed hidden, durable Deleting blocks newrefs/retries storageDelete. Publish captures immutable AssetReference descriptor в runtime values/hash, no live Delivery enrichment/serialized URL. Media failure не отменяет sync commit; real provider/cancel checks обязательны. Demo noauth/private bucket; multi-replica coordination не доказана.


Registry/codegen contract Demo фиксирован Q31: `GET /api/v1/schema-registry/schema-versions/{schemaVersionId}` отдаёт raw `application/schema+json`; C# generation использует `cm-csharp/1`, Newtonsoft.Json и deterministic Roslyn output. Source UTF-8 no BOM/LF, ZIP deterministic, filename `content-manager-csharp-models.zip`, empty/duplicate selection invalid, partial success запрещён. Generated models используют platform-neutral SDK types `RgbaColor`/`AssetReference` и exact SchemaVersion binding; artifacts server-side не persist-ятся.


## Что не входит


F06/F15 и живой каталог VR School, шина и синхронизация внешних систем; полный продуктовый Publishing F07–F11; лицензии/права F12 и protected sessions; сотрудники/вход/Identity/роли F16, административная проверка F17 и персональный журнал F18.


Soft-delete/Archive/Restore не вводятся вместо упрощённого Demo lifecycle. Допустимый delete физический; для Draft schema с dependent Draft instance data используется явный cascade flow с dependency count и согласованным 5-second confirmation. Withdrawal schema/schema-version/instance приводит к scoped Remove следующего нового valid plan; существование Published history не разрешает её уничтожение.


CLI F22 относится к MVP. Generator options editor/Unity Editor tools/source generators/build/CI/автоматическая публикация generated packages не обязательны для Demo. Отдельная F23 включена в Demo; remote catalogs/bundles, дополнительные SDK других языков, offline-first и расширенная runtime compatibility этим решением не добавляются.


## Демонстрационная цепочка


1. Открыть CM без входа, создать/изменить схему и опубликовать структуру.
2. Загрузить изображение; создать экземпляры/Draft по Published структуре, заполнить применимые типы и опубликовать данные.
3. В Code Generation UI выбрать версии, скачать ZIP .g.cs и добавить модели в Unity-проект.
4. Через F19 в отдельной F20 получить одну/несколько exact структур, typed данные и изображение по CM gateway.
5. Опубликовать новый Draft, показать Add/Update/Remove, независимость разных exact schema/version групп и сохранение старой целой группы при interruption.
6. Показать durable resume после закрытия, lost ACK/Stop/Discard, отдельную freshness и HTTP/2 fallback на реально поддерживаемом transport.
7. Показать допустимое удаление черновика и withdrawal, без подмены этого уничтожением history.


CM хранит content в постоянной БД; рестарт не очищает его. Selections/inventory и durable SDK recovery находятся у клиента. Когда запускать sync и загрузку images, выбирает приложение; кнопка в сцене не обязательный режим всех клиентов.


## Приёмка и переход


Проверяются реальные сохранение/чтение exact версий, byte equality, generation ZIP/Unity compilation, sync/recovery и client rendering. Наличие кода, enum или настройка protocol не доказательство результата. Заглушки не подтверждают работу VR School/licenses/broker. SDK distribution через self-hosted GitLab/UPM требует spike Q32, generated .g.cs package publication не нужна.


После показа/обратной связи уточняются [MVP](../mvp/mvp.md) requirements. Product decisions/NDJSON/cursor semantics/receipt pacing/server cache/SDK store direction закрыты. Literal DTO/token/hash/source fixtures, numeric configuration, SDK public signatures и pinned Unity 2022.3.62f3 patch/Player/platform/backend materialize до интеграции/приёмки; SQLite candidate/platform/package compatibility требует spike. Q31 semantic profiles не выбираются заново.


[Q26](../../business/questions/Q26-demo-mvp-scope.md) · [Q13 UI](../../technical/frontend/questions/Q13-ui-scope.md) · [Q21 storage/gateway](../../technical/backend/questions/Q21-media-storage.md) · [Q29 SDK](../../technical/frontend/questions/Q29-unity-client-package.md) · [Q31 codegen](../../technical/backend/questions/Q31-schema-code-generation.md) · [Q32 packages](../../technical/frontend/questions/Q32-sdk-package-distribution.md) · [Q33 sync transport](../../technical/backend/questions/Q33-sync-resume-transport.md).
