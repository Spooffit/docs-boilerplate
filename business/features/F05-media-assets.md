# F05. MediaLibrary и ссылки на ресурсы


## Цель


Хранить бинарные ресурсы отдельно от Unity-сборок и content JSON, предоставляя переиспользуемые типизированные assets со stable `AssetId`, которые можно использовать в Draft/Published content и historical delivery.


## Demo scope


Demo обязан показать реальную цепочку:


`upload через CM → reusable MediaAsset → AssetReference(kind) → Published instance → download bytes через CM → отображение в Unity`.


В Demo MediaLibrary уже включает каталог ресурсов, upload, preview/download, server search/pagination/filtering, reuse существующего asset, usage/delete-impact и допустимый hard delete. Это не временная кнопка upload.


## Asset identity


- CM генерирует `MediaAssetId : Guid`.
- `MediaAssetId` является единственной identity ресурса и одновременно определяет имя объекта в final S3 namespace; отдельный независимо генерируемый `ObjectKey` не вводится.
- infrastructure prefix/bucket не являются business identity и не выдаются клиенту.
- `FileName` — display metadata, а не storage key.
- новый файл или «перезагрузка» всегда создаёт новый `AssetId`; bytes существующего Available asset неизменяемы.
- один asset можно переиспользовать в нескольких EntityInstance.


MediaLibrary read model возвращает asset descriptor/value object: `AssetId` плюс server-confirmed kind, FileName, format/extension, ContentType, SizeBytes, SHA-256, status, UploadedAt и CM-owned content URL там, где он нужен. Клиент не обязан работать с голым Guid как с единственной информацией о ресурсе. Stable identity для reference при этом остаётся AssetId.


## AssetKind


Demo поддерживает:


- `Image`;
- `Audio`;
- `Video`.


`AssetKind` immutable после успешного upload.


В schema остаётся один `FieldDataType.AssetReference`. Его definition обязательно фиксирует один `AssetKind`; `List<AssetReference>` использует kind recursive item definition. Поэтому специалист при заполнении экземпляра видит только совместимые assets.


## Format policy


Список допустимых форматов принадлежит централизованной backend policy, а не каждой схеме и не hardcoded frontend.


Demo minimum:


- Image: `.jpg`, `.jpeg`, `.png`;
- Audio: `.mp3`, `.ogg`, `.wav`;
- Video: `.mp4`.


Frontend запрашивает policy у backend и показывает разрешённые форматы/лимиты в tooltip/help и file picker. Backend всегда повторно валидирует kind/format/signature. Per-kind size limits обязательны как environment configuration; отсутствие configured limit не означает unlimited upload.


## Upload lifecycle


Ресурс различает `PendingUpload`, `Available`, `Failed`, `Deleting`; MVP добавляет `Archived`.


Успешный upload означает, что CM реально принял bytes, проверил их тип, вычислил actual size и SHA-256, сохранил object и durable metadata. Ошибка object storage/validation не должна возвращать ложный Available asset.


Upload и Save EntityInstance Draft — разные действия. Новый asset после upload появляется в общей MediaLibrary и может быть повторно выбран где угодно; upload сам по себе не сохраняет форму экземпляра.


## Каталог ресурсов Demo


`/assets` поддерживает server-side search/pagination и фильтры как минимум:


- AssetKind;
- format/extension;
- upload date;
- Used / Unused;
- EntitySchema;
- status для diagnostic views.


Фильтр по EntitySchema означает assets, используемые текущими Draft/Published authoring data выбранной схемы. Historical-only usage остаётся видимым в detail/delete-impact.


Detail показывает preview/player по поддерживаемому kind, metadata, usage и download. Unsupported browser preview не означает, что asset нельзя хранить или скачать.


## Выбор в EntityInstance


Для поля `AssetReference(Image|Audio|Video)` специалист может:


1. выбрать существующий совместимый Available asset из каталога;
2. загрузить новый файл допустимого формата, который после успеха становится обычным reusable asset каталога.


Picker не показывает несовместимые kinds. Server при Save/Publish не доверяет picker и повторно проверяет reference compatibility/availability.


## Историческая неизменяемость


Published content, Release/Revision и frozen entitlement продолжают получать прежние bytes по прежнему AssetId. Новый upload не меняет старые references и не запускает mass repoint.


## Delete / Archive


Demo hard-delete разрешён только при доказанном отсутствии любых Draft/Published/history/orphan references. Перед destructive operation показывается usage/delete-impact; cascade удаления AssetReference запрещён.


MVP Archive:


- скрывает asset из обычной library/picker;
- запрещает новые references;
- сохраняет historical bytes.


Restore возвращает возможность нового использования. Automatic GC отсутствует; физическая очистка не запускается только из-за `Unused`.


## Delivery и cache boundary


Upload/metadata/lifecycle/delete проходят через CM API/Application. Обычное чтение bytes: `/storage/assets/{assetId}` → CM Host/YARP → private MinIO. Клиенты не получают provider endpoint/bucket/credentials/presigned URL как основной contract.


Маршрутизация не требует lookup `AssetId → ObjectKey`, потому что final object name детерминирован тем же GUID. Это не отменяет будущий MVP authorization/entitlement gate перед protected cache/proxy.


Bytes immutable, поэтому используются HTTP validators/client caching; Audio/Video path поддерживает Range. Server body OutputCache optional и не является обязательным хранилищем media bytes.


После самостоятельного Demo Launcher и Unity-приложение используют единый SDK-managed file cache; приложение само обращается к защищённому CM `/storage/assets/{assetId}` при cache miss. Launcher предварительно загружает выбранные ссылки по schema leaf `DeliveryMode` из [F01](F01-create-entity-schema.md#режим-доставки-assetreference-после-demo), а не весь каталог. `MediaAsset` не получает глобального DeliveryMode: один immutable asset может использоваться в разных полях с разными режимами.

Большие файлы 5–10 ГБ требуют bounded-memory streaming, Range/If-Range resume и full size/SHA-256 verification до Ready; расширенный persistent cache относится к [F19](F19-unity-client.md#launcher-и-общий-media-cache-после-demo), не Demo upload. Серверная возможность загрузить такой файл **в CM**, multipart/inbound resume, sizing/limits и фактическая proxy conformance отдельно [Уточняется Q37](../../technical/frontend/questions/Q37-launcher-asset-delivery.md); успешный download Range не доказывает готовый upload. CM остаётся authority MIME/extensions/size и проверяет protected GET/HEAD/Range до headers/cache/304; S3/MinIO закрыт за CM/YARP.

## Acceptance


- Image/Audio/Video upload проходит server policy и signature validation;
- frontend получает supported kinds/formats/limits динамически;
- каталог ищет/фильтрует/paginate-ит и позволяет reuse;
- field kind ограничивает picker и server validation;
- старый Published reference после replacement upload получает прежние bytes;
- referenced asset нельзя physical delete;
- `/storage/assets/{id}` возвращает реальные bytes через CM, включая GET/HEAD и media Range path;
- storage-specific topology не становится публичной identity.


## Связи


[F01](F01-create-entity-schema.md) · [F04](F04-entity-instances.md) · [F13](F13-content-delivery.md) · [Q12](../questions/Q12-media-and-retention.md) · [Q21](../../technical/backend/questions/Q21-media-storage.md) · [Backend](../../technical/backend/features/F05-media-assets.md) · [Frontend](../../technical/frontend/features/F05-media-assets.md)
