# M02. MediaLibrary: бэкенд


## Назначение


`MediaLibrary` — bounded context Content Manager для stable `AssetId`, типизированной metadata/lifecycle ресурса, каталога и object-storage adapter. Финальную границу BC используем уже в Demo, чтобы не переносить F05 после показа.


## Aggregate


```text
MediaAsset
  Id : Guid
  AssetKind : Image | Audio | Video
  FileName
  Format
  ContentType
  SizeBytes
  Sha256Hash
  Status
  UploadedAt
  ArchivedAt
  Version
```


Available bytes под одним `AssetId` immutable. `FileName` также immutable в Demo; будущие Title/Description, если понадобятся, являются отдельной mutable catalog metadata.


## Identity и object storage


CM создаёт один `MediaAssetId : Guid`. Этот же canonical GUID является именем объекта в final media namespace MinIO, например configured prefix + `{assetId:D}`. Отдельного независимого random `ObjectKey` не существует.


Bucket/prefix являются infrastructure configuration. Если persistence хранит physical location, object-name component выводится из AssetId и не является второй identity.


Demo provider — **MinIO**. Application зависит от S3-compatible `IObjectStorage`; MinIO SDK находится в Infrastructure. Multi-provider selector в Demo не нужен.


CM сохраняет authoritative metadata в PostgreSQL и пишет mirror metadata в object storage: AssetId, AssetKind, confirmed format/Content-Type, size и SHA-256. Object metadata не заменяет DB lifecycle/access authority.


## Upload policy


Backend владеет централизованной policy `AssetKind -> allowed extensions/MIME/max size`. Frontend получает её read-query/API и не хардкодит собственный список.


Минимальный Demo profile:


| AssetKind | Форматы |
| --- | --- |
| Image | `.jpg`, `.jpeg`, `.png` |
| Audio | `.mp3`, `.ogg`, `.wav` |
| Video | `.mp4` |


Max size обязателен в environment configuration per kind; отсутствие обязательной конфигурации не означает unlimited upload.


Upload сверяет selected kind, extension, declared MIME и фактическую signature/container. Предпочтительный infrastructure candidate для signature detection — Mime-Detective с ограниченным набором definitions; Domain/Application contract не зависит от конкретной библиотеки.


Upload — один streaming multipart request через CM API. Сервер не материализует весь файл в `byte[]`, вычисляет фактические size/SHA-256 и выполняет проверку до `Available`. Resumable/chunk upload не входит в Demo.


## Lifecycle


```text
PendingUpload -> Available
PendingUpload -> Failed
Available -> Archived -> Available   [MVP]
Available/Archived -> Deleting -> hard deleted
```


Failed/Pending не selectable; diagnostic catalog может их показывать. Manual cleanup разрешён, automatic retention GC отсутствует.


Hard delete возможен только после authoritative проверки отсутствия любых Draft/Published/history/orphan references. Cross-BC SQL/FK запрещены: usage facts получаются через public ports.


## Coordination и recovery

Demo работает одним CM process. Shared application-level AssetId guard используют все ContentModeling reference writers и MediaLibrary Delete до commit owning BC UoW; guard не создаёт cross-BC transaction. Authority — реальные Draft/Published/orphan references через public ports; full Publishing не mandatory Demo. Demo multi-replica не допускается. MVP использует выбранный PostgreSQL protocol ниже.

Delete под Demo AssetId guard либо MVP owner transaction/global advisory key fresh проверяет complete current usage через public primary ports и commits durable Deleting. Gate освобождается; все reference writers отвергают Deleting. Вне gate idempotent storage Delete retry (NotFound accepted) завершается, затем новая owner transaction/global advisory acquire → fresh Deleting/operation facts → metadata removal commit. Ошибка сохраняет Deleting, без fake Available/deleted success.

Storage upload success + failed final DB commit восстанавливается через exact attempt size/hash/type verification: complete object → durable Available, иначе cleanup только собственной attempt. Create/upload operation ID и durable original result owning MediaLibrary позволяют повторить same ID/input; different input conflict, no blind new upload. Конкретные wire/UoW/DI signatures остаются Q15/Q20/Q21.

MVP использует [утверждённый consistency guard](../questions/Q20-persistence-and-migration.md#mvp-протокол-consistency-guard): одна PostgreSQL database для всех CM replicas, один стабильный глобальный exclusive transaction-level advisory key. Owning BC начинает явную ReadCommitted transaction и standalone pg_advisory_xact_lock на её же physical connection до любых authoritative reads/row locks; затем fresh reload own roots и foreign facts через public ports в независимых primary read scopes/connections. Owner commit атомарен только для своих roots + outbox/audit intent/inbox при необходимости; foreign ambient UoW enlistment/shared SQL transaction/EF access/cache/replica reads запрещены. Host/DI обязан разделить owner UoW и foreign reads: current multi-context EfCoreUnitOfWorkParticipant этого автоматически не гарантирует.

Все invalidating writers участвуют, включая reference creation/removal/Copy/orphans, Publish/lifecycle/hard-delete, source acceptance/watermark/pending/apply/retries/setup/reconciliation и F15 open/supersede/repair/resolve. Own ETag сохраняет412, changed foreign basis даёт409; unchanged source Create не повышает source Version. External I/O/user wait/long streams вне gate; подготовка CPU вне gate требует basis recheck внутри. Wait bounded/configurable/cancellable, unavailable read не empty, connection loss abort без reconnect-continue, lost commit ACK → unknown outcome и inquiry/operation ID Q15. Domain не содержит SQL guard types; exact port/DI/mappings/error codes/numeric defaults остаются своими Q.

## Каталог и query composition


Demo list/query поддерживает server-side search/pagination и filters:


- AssetKind;
- format/extension;
- upload date;
- Used/Unused;
- EntitySchema;
- status для diagnostics.


Фильтр EntitySchema использует public ContentModeling query/port и учитывает current Draft + Published authoring references. Historical-only references остаются usage/detail/delete-impact. Отдельную read projection вводим только при доказанной необходимости производительности.


## AssetReference boundary


ContentModeling хранит stable AssetId, а определение поля `AssetReference` содержит обязательный AssetKind. Перед сохранением/публикацией F04 batch-проверяет через MediaLibrary port существование, `Available` и exact kind. Клиентская metadata не является validation authority.

После Demo `DeliveryMode` (`OnDemand` default, `Preload`, `RequiredBeforeLaunch`) хранится в exact recursive schema leaf definition F01, включая List.Item. MediaAsset не получает глобальную delivery policy: разные ссылки на один immutable файл могут иметь разные режимы. Frozen runtime descriptor F04 сохраняет пять полей. [F13](../features/F13-content-delivery.md#разрешение-asset-requirements-после-demo) описывает предлагаемый per-revision resolver/manifest; [Q37](../../frontend/questions/Q37-launcher-asset-delivery.md) — незакрытые integrity/API/mapping contracts.


## HTTP binary boundary


Canonical route:


```text
GET|HEAD /storage/assets/{assetId}
```


YARP вычисляет object path напрямую из AssetId; AssetId→ObjectKey routing lookup не нужен. Demo public MediaLibrary eligibility lookup обязателен до cache/304/Range/proxy: Pending/Failed/Deleting unreadable. MVP добавляет authorization/session/revision gate; lifecycle authority остаётся CM DB.


Binary contract:


- `ETag` строится из server SHA-256, не из EF Version и не из provider multipart ETag;
- `Last-Modified` соответствует `UploadedAt`;
- `Cache-Control` configurable, Demo default 24h + immutable semantics;
- conditional `If-None-Match`/304;
- Range/206/416 обязательно проверяется для Audio/Video;
- YARP/ASP.NET OutputCache optional/bounded, не фундамент delivery.

Launcher preload и Unity on-demand используют один CM gateway и общий SDK verified file cache; они не создают новый download protocol внутри entity sync. Range/resume сохраняет raw immutable representation, SHA-based strong CM ETag/If-Range и проверяемый Content-Range/length, без подмены S3 multipart ETag. Каждый новый request проходит fresh access до headers/cache/304/206; длительный MVP transfer живёт под independent response lease F13. SQL gate освобождается до bytes. Large-file download и upload — разные подсистемы: resumable/multipart inbound для 5–10 ГБ, completion/abort/recovery и limits остаются Q37; single-file streamed Demo upload не объявляется такой реализацией.


## Фича и вопросы


[F05](../features/F05-media-assets.md) · [Q09](../../../business/questions/Q09-editing-and-deletion.md) · [Q12](../../../business/questions/Q12-media-and-retention.md) · [Q21](../questions/Q21-media-storage.md) · [Q22](../../../business/questions/Q22-quality-targets.md).
