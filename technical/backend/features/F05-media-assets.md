# F05. Медиаресурсы: бэкенд

[Бизнесовая спецификация](../../../business/features/F05-media-assets.md) · [M02 MediaLibrary](../modules/M02-media-library.md) · [Фронтенд](../../frontend/features/F05-media-assets.md) · [F04](F04-entity-instances.md) · [F13](F13-content-delivery.md) · [Q21](../questions/Q21-media-storage.md) · [Q12](../../../business/questions/Q12-media-and-retention.md)

## Реализация Demo в исходниках

[Wire contract Q15](../questions/Q15-api-and-source-code.md#выбранный-demo-wire-contract) и [план](../f05-demo-implementation-plan.md) фиксируют принятые значения и границы.

| Физический слой | Реализованное поведение |
| --- | --- |
| Domain | MediaAsset immutable bytes, nullable incomplete facts, own Version; PendingUpload → Available/Failed, Deleting; durable MediaAssetOperation receipts без FK на удаляемый asset |
| Application.Abstractions | Metadata/list/policy/impact DTO, primitive owning read-projection/storage/usage ports; нет ObjectKey/Version в HTTP DTO |
| Application | AssetHandlers координирует upload/delete через короткие UoW stages и внешний storage I/O; fingerprint OperationId, original result replay, SHA-256/spool, central policy, current own ETag для Deleting recovery |
| Api | AssetController `/api/v1/assets`, own ETag, mandatory If-Match, File/Kind/OperationId multipart, dynamic configured request caps; scoped OpenAPI transformer сохраняет binary FileParameter в NSwag |
| EntityFramework.Persistence | AssetQueryHandlers фильтрует complete current usage до count/page; tracked command repository, private MinIO adapter, verifier PNG/JPEG/MP3/WAV/OGG/MP4, method-correct SigV4 signing и primary binary eligibility |
| EntityFramework | Additive F05MediaLifecycle migration, own Version concurrency и Available completeness CHECK; исторические locations сохраняются, destructive Down запрещён при новых lifecycle/receipt facts |
| App composition | Owning ContentModeling/Publishing/Delivery public readers и singleton AssetId guard; legacy writers без immutable AssetKind reject; YARP GET/HEAD `/storage/assets/{id}`, lifecycle gate до 304/range, SHA ETag, If-Range/206/416, private 24h immutable cache с `Vary: Origin` для native media и CORS HEAD, attachment `?download=true` |

Новые bytes всегда получают новый AssetId. Body gateway не входит в NSwag JSON API. Публичные legacy `/media-assets` маршруты удалены с разрешения владельца; прежний внутренний protected Delivery query сохранён как отдельная совместимость за пределами Demo.

Usage извлекается из recursive saved Draft, orphan work и проверенного frozen Published/history. Неполная authority блокирует delete и usage-dependent filters, а catalog показывает nullable count. Retained opaque legacy Publishing/Delivery данные дают conservative blocker; scan по retained owner data не является проверенной projection для большой библиотеки.

Archive/Restore, полноценный protected Delivery/Unity, производительность больших browser uploads и codec/IL2CPP playback не подтверждаются этим backend slice. Container verification не является полной codec decoding.

## Исторический источник до реализации Demo

## 1. Область реализации

F05 принимает файл через HTTP CM, сохраняет реальные bytes в private MinIO через S3-compatible `IObjectStorage` и предоставляет reusable `MediaAsset`. Demo включает полноценный catalog/list/search/filter/page, exact metadata, policy endpoint, preview/download, выдачу bytes через CM-owned YARP gateway, usage/delete-impact, допустимое физическое удаление и picker composition. MVP добавляет Archive/Restore и protected access. Upload не сохраняет EntityInstance Draft и не публикует контент. `AssetReference` сохраняет stable AssetId, а schema definition обязательно задаёт `AssetKind` (`Image`, `Audio`, `Video` в Demo).

Post-Demo режимы `OnDemand`/`Preload`/`RequiredBeforeLaunch` принадлежат recursive schema definition F01, не MediaAsset: один файл допускает разные declared modes в разных местах использования. F05 выдаёт те же immutable bytes для launcher preload и самостоятельного on-demand запроса Unity через общий SDK cache. Успешный entity sync не зависит от успешного file download; расширенный persistent cache/launcher не являются Demo dependency.

Demo должен пройти реальный путь upload → AssetId → сохранённый/Published instance → download прежних bytes → отображение Unity. Авторизация, шина, лицензирование и полный Publishing в Demo не требуются. В MVP MediaLibrary владеет отдельным BC, metadata и storage; Delivery проверяет membership session/revision до выдачи bytes. Знание AssetId не заменяет эту проверку. F05 не определяет entitlement и не реализует Unity rendering.

Документ описывает целевую реализацию. Новые типы ниже — проектные обозначения. Подтверждённая исходная точка checkout 04.10.2026:

| Источник | Реализовано сейчас | Целевое изменение |
| --- | --- | --- |
| MediaAssetController (исторический исходник) | /api/v1/media-assets: multipart POST, exact GET, GET /download; IFormFile, текущий предел 1 GiB | Принятый /assets API, library/impact/lifecycle, read-only YARP gateway (prefix Q21), own metadata ETag; предел не становится требованием |
| [MediaAssetContracts](../../../../contentmanager.api/src/BoundedContexts/MediaLibrary/ContentManager.MediaLibrary.Application.Abstractions/MediaAssetContracts.cs) | DTO содержит ObjectKey/Length/Hash; download DTO Url/ExpiresAt | Public metadata/status без storage location; отдельные query/mutation/binary contracts |
| MediaAssetHandler (исторический исходник) | Command и queries в Persistence с прямым MinIO SDK; временный файл, фактический размер и SHA-256; object write, затем Add уже Available | Command orchestration в Application, storage port; durable этапы Pending/Available/Failed и recovery |
| [MediaAsset](../../../../contentmanager.api/src/BoundedContexts/MediaLibrary/ContentManager.MediaLibrary.Domain/MediaAssets/MediaAsset.cs), [status](../../../../contentmanager.api/src/BoundedContexts/MediaLibrary/ContentManager.MediaLibrary.Domain/MediaAssets/Enums/MediaAssetStatus.cs) | PendingUpload/Available/Failed/Archived; RegisteredAt/UploadedAt; MarkUploaded event, Archive; нет target Restore/Deleting/own Version | Полный lifecycle/guards/metadata token, immutable bytes |
| [Mapping](../../../../contentmanager.api/src/BoundedContexts/MediaLibrary/ContentManager.MediaLibrary.EntityFramework/Configurations/MediaAssetConfiguration.cs), [DbContext](../../../../contentmanager.api/src/BoundedContexts/MediaLibrary/ContentManager.MediaLibrary.EntityFramework/MediaLibraryDbContext.cs) | Своя schema media_library, owned location, required size, status/hash indexes | Nullable incomplete facts, concurrency/consistency/delete orchestration mapping |
| [Persistence module](../../../../contentmanager.api/src/BoundedContexts/MediaLibrary/ContentManager.MediaLibrary.EntityFramework.Persistence/MediaLibraryPersistenceModule.cs), [App module](../../../../contentmanager.api/src/App/ContentManager.Application/ContentManagerApplicationModule.cs) | Singleton MinIO client/options, CQRS scanning Persistence, общий UoW | Раздельная регистрация Application/query/storage и коротких durable этапов |
| MediaUploadTests (исторический исходник) | PostgreSQL+MinIO тест upload/hash/private bucket/presigned download/new IDs/size mismatch | Исходный тест не доказывает target binary API, recovery, ETag и reference/delete races; в этой аналитике не запускался |

Исторический upload до Demo не фиксировал Pending в БД до storage и не сохраняет Failed при отказе. Если storage write прошёл, а последующий DB commit упал, объект может остаться без metadata. Это различие раскрыто в Q21 и не скрывается наличием Domain status.

## 2. Domain

### 2.1. Aggregates, entities и value objects

| Тип | Роль | Владелец / граница |
| --- | --- | --- |
| MediaAsset | Aggregate Root | MediaLibrary: metadata/lifecycle/location; size/hash/time подтверждаются после upload |
| MediaAssetId | Typed identity | Stable AssetId, не filename/URL/content hash |
| AssetFileName | Value Object | Display/download metadata; не путь storage; нормализация/ограничения Q21/Q15 |
| AssetKind | Enum/value contract | Demo: Image, Audio, Video; immutable после Available; shared semantic contract без зависимости на MediaLibrary implementation |
| AssetFormat | Value Object | Серверно подтверждённый logical format/extension из central policy |
| AssetContentType | Value Object | Подтверждённая MIME metadata; client type не authority |
| ObjectStorageLocation | Value Object | Internal bucket + object name, где object name детерминирован `MediaAssetId`; не отдельная identity |
| MediaAssetStatus | Enum: PendingUpload, Available, Failed, Archived, Deleting | Lifecycle, не HTTP delivery entitlement |
| AssetUsageFacts | Внутренний результат public boundaries | Не отдельный aggregate и не копия контента; unavailable authority не означает count0 |

**MediaAsset**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | `MediaAssetId` | Да | Stable identity; генерируется сервером | Новый upload создаёт новый Id; не filename/URL/hash; не меняется при Archive/Restore |
| FileName | `AssetFileName` | Да | Display/download metadata | Не storage path; immutable после Available в Demo |
| Kind | `AssetKind` | Да | Семантический вид ресурса | Demo: Image/Audio/Video; выбирается до upload, immutable после Available |
| Format | `AssetFormat?` | Условно | Подтверждённый format/extension | Null до verification; Available/Archived required; должен быть разрешён policy Kind |
| ContentType | `AssetContentType` | Да | MIME metadata; после MarkAvailable подтверждённая | Client MIME не authority; must match policy/signature |
| SizeBytes | `long?` | Условно | Фактический размер после upload; до подтверждения null | Required для Available/Archived; declared length не authority; zero-byte/limits Q21/Q12 |
| Sha256Hash | `ContentHash?` | Условно | Серверный SHA-256 реальных bytes | Required для Available/Archived; не S3 ETag и не instance JSON hash; не dedup |
| Status | `MediaAssetStatus` | Да | Серверный lifecycle | PendingUpload, Available, Failed, Archived, Deleting |
| StorageLocation | `ObjectStorageLocation` | Да | Server-owned object location | Internal; не выдаётся в DTO; credentials отдельно |
| RegisteredAt | `DateTimeOffset` | Да | Серверное время регистрации попытки | Не UploadedAt |
| UploadedAt | `DateTimeOffset?` | Условно | Подтверждённое серверное время доступности | Required для Available/Archived; null до подтверждения; Archive/Restore не меняет |
| ArchivedAt | `DateTimeOffset?` | Условно | Серверное время Archive | Archived lifecycle; Restore сбрасывает |
| Version | `int` | Да | Внутренний own concurrency token | ETag metadata; отсутствует в DTO; lifecycle guards; recovery Q21 |

**MediaAssetId**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Value | `Guid` | Да | Server-generated stable AssetId | Новый upload — новая identity; не filename/URL/content hash |

**AssetFileName**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Value | `string` | Да | Display/download filename | Не storage path; нормализация/ограничения Q21/Q15 |

**AssetKind**

Demo values: `Image`, `Audio`, `Video`. Vocabulary используется ContentModeling definition и MediaLibrary compatibility checks; physical assembly placement не создаёт cross-BC implementation dependency.

**AssetFormat**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Value | `string` | Да | Canonical confirmed extension/format | Входит в central policy соответствующего AssetKind |

**AssetContentType**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Value | `string` | Да | MIME metadata | Trust client/bytes verification Q21; не proof безопасного preview |

**ObjectStorageLocation**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Bucket | `string` | Да | Internal server-owned bucket | Не public DTO; provider credentials отдельно |
| ObjectName | `string` | Да | Canonical Guid string, derived from AssetId | Не отдельная identity; должен соответствовать `AssetId:D`; optional configured prefix применяется infrastructure-side |

**AssetUsageFacts**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Draft reference count (имя уточняется) | Уточняется | Уточняется | Draft usage от public authority | Тип/единица count Q15/Q21; unavailable не count0 |
| Published reference count (имя уточняется) | Уточняется | Уточняется | Published usage | Полнота/единица Q15/Q21 |
| Historical reference count (имя уточняется) | Уточняется | Уточняется | Retained history usage | Archived/Retired owner не отсутствие зависимости; Q15/Q21 |
| Blockers (имя уточняется) | Уточняется | Уточняется | Блокирующие dependencies | Shape Q15; не content copy |
| Authoritative/complete outcome (имя уточняется) | Уточняется | Уточняется | Достоверность и полнота ответа authority | Q15/Q21; incomplete запрещает destructive действие |

SizeBytes до подтверждения не подменяется declared length. RegisteredAt обозначает регистрацию попытки; UploadedAt — подтверждённую доступность. AssetDto не раскрывает Bucket/provider credentials/own Version и подробности recovery. Public Id уже является той же object identity; отдельный ObjectKey DTO отсутствует. Нужные дополнительные durable operation facts для recovery/retention протокола определяются в Q21, точный public operation-ID carrier материализуется Q15, не выводится из существующего DTO.

ContentType в Pending — только принятая на регистрации metadata по policy Q21, а не доказательство проверки bytes. MarkAvailable фиксирует окончательный confirmedType из verification; absent/client-mismatched type/fallback правила уточняются Q21. Header скачивания использует подтверждённую metadata; Available/Archived не меняют её по новой догадке из расширения файла.

### 2.2. Методы и переходы

| Тип | Метод | Параметры | Предусловия | Изменение / результат |
| --- | --- | --- | --- | --- |
| MediaAsset | RegisterPending | server AssetId, FileName, AssetKind, declared MIME, derived location, RegisteredAt | Kind существует в central policy; server-owned object name derived from Id | PendingUpload; Format/confirmed facts null; own Version |
| MediaAsset | MarkAvailable | actualSize, sha256, confirmedFormat, confirmedType, UploadedAt | PendingUpload; signature/policy verification и storage write завершены; confirmed format compatible with Kind | Available, immutable kind/format/type/size/hash/bytes; новый Version |
| MediaAsset | MarkFailed | outcome policy Q21 | Незавершённая upload попытка, отказ не скрывает successful Available | Failed/Version; не удаляет object автоматически |
| MediaAsset | Archive | server time | Available, MVP, own precondition | Archived/ArchivedAt/Version; storage bytes прежние |
| MediaAsset | Restore | — | Archived, own precondition | Available, ArchivedAt reset, новый Version; bytes/hash/UploadedAt прежние |
| MediaAsset | BeginDelete | подтверждённая eligibility/guard | Допустимый status, отсутствие всех refs под согласованным протоколом | Deleting/Version, новые refs/обычное чтение запрещены |
| Repository / delete orchestration | Remove metadata | AssetId, current operation/Version | Deleting, object delete подтверждён или точный object отсутствует | Физическое удаление строки; это не Domain Archive |

Принятый основной delete переход Available/Archived → Deleting → физическое удаление. Удаление Failed/Pending, восстановление зависших попыток и повторные/no-op команды — Q21. Нет ReplaceBytes/UpdateContent для Available и нет endpoint, заменяющего object под прежним AssetId.

### 2.3. Инварианты и constraints

Постоянные ограничения FileName/Kind/Format/ContentType/location/hash задаёт MediaAssetConstraints в Domain по [S21](../standards/S21-aggregate-constraints.md). FluentValidation и EF используют те же определения. Provider/request limits относятся к согласованной upload policy Q21 и эксплуатационным целям [Q22](../../../business/questions/Q22-quality-targets.md). Current EF512/255 и MaxBytes1GiB не назначаются как target.

- Успешный upload возвращает только Available с реальными size/hash/time и сохранённым object. Bytes и AssetId не меняются при Archive/Restore. Одинаковый файл или filename не запускает автоматический dedup: новый upload создаёт новую identity.
- Filename, declared MIME/length и client hash — недоверенные сведения. Backend проверяет выбранный AssetKind, central allowlist extension/MIME и фактическую file signature/container, вычисляет actual size/SHA-256. Frontend hints не authority. Per-kind max bytes обязательны в configuration и выдаются policy query.
- Отдельный независимо генерируемый ObjectKey запрещён. Final object name равен canonical `AssetId:D`; optional prefix/bucket — configuration. Если object name хранится в DB, он invariant-derived и не может расходиться с Id. DTO/content JSON не содержат bucket/provider location. SHA-256 здесь считается по bytes файла, а не по canonical instance JSON F04/Q19.
- Draft reference блокирует delete наряду с Published/history. Нет cascade очистки AssetReference. Retired/Archived/Withdrawn owner и frozen historical Delivery не означают отсутствие dependencies.
- Archive MVP блокирует все новые references, а не только новый Published; обычный picker исключает Archived. Historical чтение bytes сохраняется. Различие нового reference и сохранения старого Draft/reference требует Q12/Q21 policy и согласованного F04 writer.
- Deleting не становится Available при повторе. Reference precheck/own asset Version сами по себе не исключают concurrent создание ссылки в другом BC. В отсутствие подтверждённой полной authority destructive действие не выполняется.
- Automatic GC отсутствует в MVP. Recovery известной неоконченной операции не является разрешением сканировать и удалять любые «неиспользуемые» objects.

### 2.4. Domain events

Current MediaAssetAvailableDomainEvent создаётся при MarkUploaded, payload MediaAssetId; consumer не подтверждён этой цепочкой. Target local event нужен только при конкретном потребителе; OccurredAt должен соответствовать подтверждённому lifecycle fact, rollback не публикует success. Не вводятся брокер/outbox и Archive/Restore events только ради логов. Решение о потребителе/dispatch после durable final commit — Q15/Q21.

**MediaAssetAvailableDomainEvent (current payload)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| MediaAssetId | Уточняется | Да | Payload current MarkUploaded event | Consumer не подтверждён; target contract/dispatch Q15/Q21 |

## 3. Application.Abstractions

### 3.1. Commands

| Command | Result | Concurrency |
| --- | --- | --- |
| UploadAssetCommand | AssetDto + internal ETag carrier после Available commit | Новый AssetId; без If-Match |
| ArchiveAssetCommand | AssetDto + metadata | Own asset Version |
| RestoreAssetCommand | AssetDto + metadata | Own asset Version |
| DeleteAssetCommand | Guid при завершённом удалении | Own Version перед BeginDelete + отдельная resume policy |
| Internal upload/delete steps | Внутренние durable facts | Короткие отдельные local UoW, не новые HTTP routes |

**UploadAssetCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Content | `Stream` | Да | Поток HTTP upload | Request lifetime; не сериализуется в DTO/broker/background |
| FileName | `string` | Да | Filename transport metadata | Недоверенный ввод; не storage key |
| AssetKind | Уточняется — [Q15](../questions/Q15-api-and-source-code.md) | Да | Выбранный semantic kind | Demo Image/Audio/Video; backend policy authority; logical type/representation: shared enum/value; final CLR type is implementation detail |
| DeclaredContentType | `string?` | Нет | Client MIME | Verification/fallback Q21; не final authority |
| DeclaredLength | `long?` | Нет | Client size | Проверяется actual size; не confirmed SizeBytes |
| Cancellation (имя уточняется) | Уточняется | Уточняется | От HTTP lifetime | Contract Q21; не wire field |

**ArchiveAssetCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| AssetId | `Guid` | Да | Exact asset из route | Own identity |
| ExpectedVersion | `int` | Да | Внутренний token из If-Match | Не body/DTO; own Version |

**RestoreAssetCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| AssetId | `Guid` | Да | Exact asset из route | Own identity |
| ExpectedVersion | `int` | Да | Внутренний token из If-Match | Не body/DTO; own Version |

**DeleteAssetCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| AssetId | `Guid` | Да | Exact asset из route | Own identity |
| ExpectedVersion | `int` | Да | Внутренний token перед BeginDelete | Recovery/resume binding Q21; не bypass старого If-Match |

**Internal upload/delete steps**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Server AssetId (имя уточняется) | Уточняется | Да | RegisterPending, ConfirmUpload/FailUpload, BeginDelete/CompleteDelete | Внутренние durable stages; не отдельные HTTP routes/обязательный command bus |
| Facts (имя уточняется) | Уточняется | Уточняется | Server-confirmed stage facts | Состав/recovery Q21 |
| Observed Version (имя уточняется) | Уточняется | Уточняется | Internal conditional stage token | Короткие отдельные local UoW; Q21 |

Названия внутренних шагов не предписывают отдельный command bus на каждый метод. Stream принадлежит HTTP request lifetime и не сериализуется в DTO/брокер/фон. Client ExpectedVersion/Version/Revision отсутствуют в wire; Api преобразует If-Match во внутренний int.

### 3.2. Queries

| Query | Result |
| --- | --- |
| GetAssetPolicyQuery | AssetUploadPolicyDto |
| ListAssetsQuery | OffsetPagedResultDto<AssetListItemDto> |
| GetAssetQuery | AssetDto + own metadata ETag |
| GetAssetDeleteImpactQuery | AssetDeleteImpactDto; полнота/authority отдельно |

**GetAssetPolicyQuery**

No input. Возвращает active backend policy для frontend: kinds, allowed extensions/MIME и configured maximum bytes per kind. Frontend не имеет hardcoded authoritative copy.

**ListAssetsQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Search FileName | `string?` | Нет | Server-side filename search | Normalized search contract Q15 |
| Kind | `AssetKind?` | Нет | Kind filter / picker constraint | Demo Image/Audio/Video |
| Format | `string?` | Нет | Canonical extension/format | Must belong to current policy |
| UploadedFrom/To | `DateTimeOffset?` | Нет | Upload date range | Server time semantics |
| Usage | Уточняется — [Q15](../questions/Q15-api-and-source-code.md) | Нет | Current authoring usage | Via public ContentModeling composition; no foreign SQL join; logical type/representation: Used/Unused?; final CLR type is implementation detail |
| EntitySchemaId | `Guid?` | Нет | Assets used by current Draft/Published data of schema | Historical-only usage stays detail/impact |
| Status/context | Уточняется — [Q15](../questions/Q15-api-and-source-code.md) | Нет | Normal/diagnostic/archive/picker | Picker requires Available + requested Kind; logical type/representation: optional; final CLR type is implementation detail |
| Page/sort | Уточняется — [Q15](../questions/Q15-api-and-source-code.md) | Да | Server paging/sort | Q15 limits/defaults; logical type/representation: standard query; final CLR type is implementation detail |

**GetAssetQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| AssetId | `Guid` | Да | Exact metadata identity | Own metadata ETag отдельно |

**GetAssetDeleteImpactQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| AssetId | `Guid` | Да | Exact impact identity | Authority/полнота отдельно от own ETag |

Читающие handlers располагаются в EntityFramework.Persistence по [S20](../../../catalog/docs/standards/architecture/cqrs.md#handler-responsibilities-and-dependency-direction). Binary gateway/runtime access использует публичные media eligibility facts при проверке session/revision; это access gate, не enrichment Published runtime values. Immutable AssetReference descriptor захватывается F04 при Publish и читается Delivery из snapshot. Binary read выполняет Host YARP после applicable gate; separate OpenAssetContentQuery/MVC byte pump не вводится. Anonymous Demo gateway не означает anonymous protected MVP access.

### 3.3. DTO и parameter models

| Модель | Назначение |
| --- | --- |
| Upload HTTP request | Transport input |
| AssetUploadPolicyDto | Backend-authoritative kinds/formats/limits for UI |
| AssetDto | Exact public metadata |
| AssetListItemDto | Lightweight list projection |
| AssetDeleteImpactDto | Informational usage impact |
| UploadVerifiedFacts | Verified server upload facts |
| Mutation result metadata | Post-commit internal carrier |

**Upload HTTP request**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Multipart file | Уточняется — [Q15](../questions/Q15-api-and-source-code.md) | Да | Transport file/filename/type/length metadata | Single-file Demo upload; no whole byte[]; logical type/representation: stream; final CLR type is implementation detail |
| AssetKind | Уточняется — [Q15](../questions/Q15-api-and-source-code.md) | Да | User-selected expected kind | Must exist in backend policy; exact transport binding Q15; logical type/representation: enum/value; final CLR type is implementation detail |

**AssetDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | `Guid` | Да | Stable AssetId | Нет storage location/credentials/own Version/recovery internals |
| FileName | `string` | Да | Download/display metadata | Не path; immutable after Available |
| Kind | `AssetKind` | Да | Confirmed semantic kind | Demo Image/Audio/Video |
| Format | `string?` | Условно | Confirmed canonical format | Required Available/Archived |
| ContentType | `string` | Да | Stored MIME metadata | Pending не verification; Available confirmed |
| SizeBytes | `long?` | Условно | Confirmed bytes count | Nullable incomplete facts; Available/Archived confirmed |
| Sha256Hash | `string?` | Условно | Confirmed SHA-256 | Nullable до confirmation; не S3 ETag |
| Status | Уточняется | Да | Stored lifecycle | Domain MediaAssetStatus; exact wire type Q15 |
| RegisteredAt | `DateTimeOffset` | Да | Registered attempt time | Server fact |
| UploadedAt | `DateTimeOffset?` | Условно | Confirmed upload time | Nullable incomplete state |
| ArchivedAt | `DateTimeOffset?` | Условно | Archive time | Restore resets; lifecycle fact |
| ContentUrl | `string?` | Условно | Transient CM-owned binary URL query/UI DTO | For readable Available/historical context; never provider URL; не serialized runtime AssetReference/hash |

**AssetListItemDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | `Guid` | Да | Stable AssetId | Нет content/location/per-item ETag |
| FileName | `string` | Да | Summary filename | immutable after Available |
| Kind | `AssetKind` | Да | Summary kind | server-confirmed |
| Format | `string?` | Условно | Summary format | confirmed when available |
| ContentType | `string` | Да | Summary MIME metadata | Stored metadata |
| SizeBytes | `long?` | Условно | Confirmed size | Incomplete facts nullable |
| Status | Уточняется | Да | Lifecycle summary | Exact wire type Q15 |
| UploadedAt | `DateTimeOffset?` | Условно | Confirmed time | Null до подтверждения |

**AssetDeleteImpactDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| CanDelete | `bool` | Да | Informational eligibility | Preview не authority/guard до delete |
| DraftReferenceCount | Уточняется | Уточняется | Draft dependencies | Count type/unit Q15; unavailable не count0 |
| PublishedReferenceCount | Уточняется | Уточняется | Published dependencies | Count type/unit Q15 |
| HistoricalReferenceCount | Уточняется | Уточняется | Historical dependencies | Count type/unit Q15 |
| Blockers | Уточняется (массив) | Уточняется | Blocking reasons/dependencies | Shape Q15; не полный content JSON |

**UploadVerifiedFacts**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Actual size (имя уточняется) | Уточняется | Да | Вычисленный фактический размер | Проверка Q21, не declared length |
| Hash (имя уточняется) | Уточняется | Да | Вычисленный hash bytes | Server authority; Q21 |
| ContentType | Уточняется | Да | Принятый confirmed MIME | Verification Q21 |
| Object write confirmation (имя уточняется) | Уточняется | Да | Подтверждённое завершение storage write | Q21; не prepared Available |

**Mutation result metadata**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Result (имя уточняется) | Уточняется — [Q15](../questions/Q15-api-and-source-code.md#f05-детализация-целевого-контракта) | Да | Результат конкретного committed use case | Logical Upload/Archive/Restore AssetDto либо completed deletion ID; exact carrier/generic type Q15; не hidden GET |
| Version | `int` | Да | Internal own committed token | Api ETag mapping; не DTO/public body |

Binary gateway не application Stream result/NSwag JSON DTO; resource headers/error/access profile — Q21/Q15. Status Pending/Failed/Deleting может быть показан exact metadata для recovery; допустимость library/picker/read filters определяется Q15. Storage/error internals не выводятся. Read DTO не включает computed delete impact: impact отдельным query, чтобы чужие refs не меняли собственную metadata representation при прежнем own ETag.

### 3.4. Ports

| Port | Ответственность | Направление |
| --- | --- | --- |
| IMediaAssetRepository | Exact tracking Add/Load/Remove, own conditional updates | Application → local Persistence |
| IObjectStorage | PutAsync(location, readable stream, known size/type, ct), DeleteAsync(location, ct), выбранная verify/existence/recovery операция; не hot-path client read | Application/maintenance → storage adapter; provider types не выходят наружу |
| IUploadContentVerifier | Ограниченный потоковый size/hash/type-check по утверждённой policy, результат UploadVerifiedFacts | Local Application algorithm; никакого trust client digest |
| IAssetReferenceUsageReader | Полные facts ContentModeling и применимых Publishing/Delivery history authorities | Public BC contracts; без foreign DbContext/FK/SQL |
| Target IAssetReferenceGuard | Общий application-level guard keyed AssetId: reference creation и Delete координируются до owning UoW commit | Demo singleton в одном CM process; public protocol Q21, не foreign context. Exact signature уточняется |
| IMediaAssetReader | Get/GetMany metadata, EnsureUsableForNewContent, historical/read eligibility facts | Inbound media public boundary для F04/Publishing/Delivery |
| TimeProvider / local UoW | Registered/upload/archive time; отдельные durable metadata stages | Application → infrastructure |

В IMediaAssetReader usable означает существующий Available. Archived читается historical consumer по policy, но не возвращается как usable new reference. Public read port не выдаёт credentials/location; eligible facts используются gate, bytes проксирует Host YARP. Demo gateway всегда читает MediaLibrary public eligibility port до cache/proxy; это lifecycle gate, не AssetId→ObjectKey routing lookup. Pending/Failed/Deleting не readable. Exact public signature — Q21. Demo не вызывает отсутствующий full Publishing ради подтверждения count0.

MVP coordination port принадлежит Application.Abstractions: задаёт границу guarded owner mutation, cancellation и outcome, без PostgreSQL/Npgsql/EF типов в Domain. Точные CLR-имена и signatures остаются Q15. Application открывает owning UoW и требует guard до authoritative reads; EntityFramework.Persistence выполняет standalone pg_advisory_xact_lock на той же physical connection и transaction владельца. Public foreign read adapters используют независимые primary read scopes/connections без ambient UoW enlistment. Host/DI отделяет owning UoW от этих scopes; существующий [EfCoreUnitOfWorkParticipant](../../../../contentmanager.api/src/BuildingBlocks/ContentManager.EntityFramework.UnitOfWork/EfCoreUnitOfWorkParticipant.cs), способный enlist несколько contexts в общую SQL transaction, не выполняет эту целевую границу автоматически. Domain получает уже проверенные предметные facts и не содержит SQL guard type.

### 3.5. Specifications / read contracts

MediaAssetsListSpecification, MediaAssetByIdSpecification и UsableMediaAssetsByIdsSpecification используют [S19](../../../catalog/docs/standards/dotnet/ardalis-specification.md): атомарные conditions identity/status/name search и отдельные ordering/projection/page. Count использует те же filters без paging. Нет spec.And и client-side загрузки всей library для поиска. List summaries без streams; exact metadata без body bytes. Default sort/tie-breaker/page/search comparison Q15. Archived исключён из normal picker; Failed/Pending/Deleting не selectable.

## 4. Application

### 4.1. Handlers / use cases

UploadAssetCommandHandler, ArchiveAssetCommandHandler, RestoreAssetCommandHandler и DeleteAssetCommandHandler находятся в целевом ContentManager.MediaLibrary.Application. Такой assembly сейчас отсутствует: source MediaAssetHandler совмещает command/query в Persistence. Перенос command orchestration нужен для физического разделения [S20](../../../catalog/docs/standards/architecture/cqrs.md#handler-responsibilities-and-dependency-direction); он не означает переноса query handlers в Application.

Каждый use case раскрыт в §9. Upload/Delete координируют несколько durable stages и storage IO; один ambient transactional command scope на весь request не подходит для фиксации Pending/Deleting до object write/delete. Concrete pipeline opt-out/отдельные scopes должны быть согласованы и проверены Q21. Контроллер не коммитит БД и не управляет этим orchestration.

### 4.2. Validators

UploadAssetInputValidator композирует FileNameRules/ContentTypeRules из MediaAssetConstraints и согласованную upload policy; Archive/Restore/Delete validators проверяют ID/precondition structure, без чтения чужих БД. По [S18](../../../catalog/docs/standards/dotnet/fluentvalidation.md) metadata validation выполняется до writes, валидаторы не потребляют upload stream и не меняют DTO. Фактический размер/hash/type verification выполняет отдельный streaming algorithm; reference authority/eligibility — handler/public protocol. Limits применяются согласованно на HTTP/proxy/storage, а не отдельными несовпадающими literals.

### 4.3. Mappers

Api multipart → application stream input; application normalized metadata/facts → Domain VOs; Domain/read projection → AssetDto/list; own Version → отдельный header carrier. Исходный filename очищается по утверждённой portable policy; storage key не вычисляется из него. Upload stream не превращается в JSON base64/byte[]. Metadata Api mapper не пишет downloadable bytes: raw binary response/forwarder ownership принадлежит Host YARP (§8/Q21); application read-result wrapper не вводится.

### 4.4. Services / algorithms

Для материалов 5–10 ГБ после Demo нужен отдельно проверенный inbound upload protocol: streamed/resumable multipart object upload, durable parts/completion/abort/recovery и согласованные HTTP/proxy/storage/temp-space limits. Это инженерная ветвь [Q37](../../frontend/questions/Q37-launcher-asset-delivery.md), не утверждённый новый endpoint, размер лимита или готовность текущего single-file Demo request. HTTP multipart form и multipart object-store upload — разные механизмы. Поддержка Range на download не доказывает возможность загрузить такой материал в CM; Available по-прежнему требует server-confirmed size/SHA/type и durable commit.

Upload orchestration ограничивает RAM, считает фактические bytes/SHA-256 и при необходимости использует контролируемый временный spool. Выбор прямого streaming либо spool для известного storage length/проверки файла — Q21; current 64KiB buffer/temp-file путь не нормативный default. Сбой после object write не компенсируется вслепую: timeout может иметь неизвестный результат, а удаление должно относиться к точному server-owned location этой попытки.

Reference usage должен учитывать recursive F04 AssetReference, Lists/Objects и сохранённую orphan authoring work. Нельзя искать произвольные Guid-строки как AssetIds либо сканировать только flat ListItemType. Provenance/адресация orphan связаны с F04/Q15; неполная extraction не считается доказательством безопасного удаления.

DI: Application handlers/validators/verifier; Persistence repository/query/metadata adapters; EntityFramework configs/context; infrastructure storage adapter/client/options; Api metadata header/result mapper; App/Host YARP route/cluster/transforms + applicable access gate + optional output cache composition. Current MediaLibraryPersistenceModule регистрирует MinIO из MediaStorageOptions, App сканирует Persistence и вызывает общий UoW behavior. Целевой внешний adapter извлекается из handler; physical assembly/directory и pipeline registration — Q21, Domain/Application не ссылаются на SDK. Новый broker/service не требуется.

## 5. Api

### 5.1. Endpoints

Таблица [S17](../standards/S17-data-flows.md), HTTP adapter по [S16](../standards/S16-rest.md). `A=/api/v1/assets`, IDs Guid. D = anonymous Demo; M = MVP authoring access F16/Q10, без выдуманного permission. MVP runtime bytes выдаются после проверки Delivery session/revision; точный access contract Q15/F13.

| Поток | Транспорт | Метод / канал | Endpoint / destination | Доступ | Вход | Application action | Выход | Успех | Ошибки |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| DF00. Media policy | HTTP | GET | A/policy | D/M | — | GetAssetPolicyQuery | ApiResponse<AssetUploadPolicyDto> |200 |500 invalid server configuration |
| DF01. Library/picker | HTTP | GET | A | D/M | Search/filter/page/sort | ListAssetsQuery | ApiResponse<OffsetPagedResultDto<AssetListItemDto>> |200 |400; access Q15 |
| DF02. Upload | HTTP | POST | A | D/M | Multipart file/transport metadata | UploadAssetCommand | ApiResponse<AssetDto> + metadata ETag |200, только Available |400/413 profile Q15; storage5xx |
| DF03. Exact metadata | HTTP | GET | A/{assetId:guid} | D/M | AssetId | GetAssetQuery | ApiResponse<AssetDto> + ETag |200 |404 |
| DF04. Binary gateway | HTTP proxy | GET/HEAD | `/storage/assets/{assetId:guid}` | D; M applicable gate before cache/proxy | AssetId; conditional/range | Access/eligibility facts if required → Host YARP → private storage; no OpenRead byte query | Raw binary/storage/gateway result + content headers | Full/HEAD/206/304 subject to provider/profile checks | Access401/403; unavailable404/lifecycle profile; range416; gateway/storage5xx |
| DF05. Delete impact | HTTP | GET | A/{assetId:guid}/delete-impact | D/M | AssetId | GetAssetDeleteImpactQuery | ApiResponse<AssetDeleteImpactDto> |200 |404; incomplete authority Q15 |
| DF06. Delete | HTTP | DELETE | A/{assetId:guid} | D/M | Own If-Match; resume Q21 | DeleteAssetCommand | Deleted ID/result profile Q15 |Уточняется Q15; только после object+metadata удаления |404/409 asset.in-use/412/428; storage5xx |
| DF07. Archive | HTTP | POST | A/{assetId:guid}/archive | M | Own If-Match | ArchiveAssetCommand | ApiResponse<AssetDto> + ETag |Mutation status Q15 |404/409/412/428 |
| DF08. Restore | HTTP | POST | A/{assetId:guid}/restore | M | Own If-Match | RestoreAssetCommand | ApiResponse<AssetDto> + ETag |Mutation status Q15 |404/409/412/428 |

Внутренние recovery stages не создают отдельные публичные endpoints/worker schedule. Multipart transport не означает buffering всего файла в RAM; ASP.NET binding и пределы разных proxy layers проверяются отдельно. Current /media-assets/{id}/download presigned DTO не target read contract. YARP route `/storage/assets/{assetId}` read-only; POST/PUT/PATCH/DELETE и arbitrary bucket traversal/listing не разрешены, upload/delete остаются CM application API. Guid-derived object name и public HTTP semantics закреплены Q21; concrete route matcher и upstream signing/header-query transforms проверяются при реализации.

### 5.2. ETag / If-Match

Exact metadata GET и успешный upload/Archive/Restore возвращают strong own-resource ETag из internal Version. DTO/list не содержит Version/Revision/ExpectedVersion, per-row tag не источник precondition. Archive/Restore/Delete require own If-Match; missing428, mismatch412, domain conflict409. Upload нового asset не требует If-Match.

YARP binary content — другая representation: immutable bytes/hash не меняются от Archive/Restore metadata. Storage ETag/Last-Modified/conditional/range/cache не подменяются metadata Version и не автоматически SHA-256. Applicable access проверяется до cache hit/304/range; actual provider support и final policy — Q21. Внешняя stale reference диагностика не включается в AssetDto как computed impact при прежнем own ETag.

BeginDelete фиксирует Deleting и новый Version до storage IO. Retry прежнего If-Match поэтому нельзя автоматически считать разрешённым: recovery должна связать запрос с конкретной операцией либо получить current exact Deleting metadata/new tag по выбранному контракту. Policy Q21/Q15: create/upload operation ID принят, конкретные resume/header signatures уточняются; no silent bypass412 и no новый route по догадке.

### 5.3. OpenAPI и ошибки

Документируются multipart part/binding/metadata, доступ, envelopes, headers, nullable незавершённые facts, status и binary content response. Binary bytes не оборачиваются в ApiResponse<byte[]> и не обрабатываются как JSON envelope; общий HTTP handler должен сохранить binary response и необходимые headers. NSwag относится к metadata/mutation API; gateway binary/error/header contract и client stream ownership проверяются отдельно до UI; generated source не правится вручную.

Принятые codes asset.not-available/asset.in-use, own412/428 и not-found semantics применяются по назначению. Concrete upload validation codes, 400 vs413, storage failures, impact incompleteness/access, повторный Delete/no-op и final success status — [Q15 F05](../questions/Q15-api-and-source-code.md#f05-детализация-целевого-контракта). Current asset.unavailable404 для download-link query не переносится автоматически на все lifecycle conflicts.

Общий Demo transport profile [Q15](../questions/Q15-api-and-source-code.md#согласованный-demo-профиль-authoring-api): issues имеют stable machine code, message и address; адреса named values используют FieldId и ItemKey, а не индекс элемента List<Object>. List queries используют единые paging/sort conventions со stable identity tie-breaker; конкретные names/limits/defaults остаются инженерной детализацией. Binary success остаётся binary, ошибки используют общий error profile. NSwag generation и recursive round-trip обязательны до интеграции UI; схема всех 15 типов и nested Lists проверяется буквально, без сокращённого набора variants.

ETag exact authoring read относится только к сохранённому mutable root. Derived diagnostics имеют отдельную identity basis, включающую exact schema/content/baseline context: изменение diagnostics не имитирует изменение own ETag. Accept и delete consent связываются с показанным context/impact; одного root ETag для этих external facts недостаточно.

## 6. EntityFramework

### 6.1. DbContext ownership

MediaLibraryDbContext/schema media_library содержит DbSet<MediaAsset> и собственные configs/migrations. ContentModeling/Publishing/Delivery dependencies — stable IDs/public contracts, без foreign DbContext/EF navigation/FK в чужую schema. S3 object не является участником EF transaction.

### 6.2. Configurations

| Entity / value | Table / mapping | Key/index | Concurrency |
| --- | --- | --- | --- |
| MediaAsset | media_assets: id UUID, file_name/content_type, nullable size_bytes/sha256_hash, status, registered_at, uploaded_at/archived_at nullable, version int | PK; status/uploaded_at indexes для library, hash lookup не dedup authority | own version int concurrency token |
| ObjectStorageLocation | Owned bucket/object_key в metadata строке | Internal location uniqueness strategy Q21/Q20; не filename uniqueness | Вместе с root |
| Дополнительные recovery/retention facts | Если выбранный протокол требует durable identity/guards | Состав/index/cleanup Q21/Q20 | Не объявлять существующей таблицей из названия port |

EF преобразует VOs и times; постоянные lengths берутся из Domain constraints. Available/Archived обязаны иметь фактические size/hash/upload time; pending facts nullable. Как Deleting сохраняет прежние подтверждённые facts и recovery context — Q21. Location remains internal, не public URL column. No FK между asset и JSON reference чужого BC.

### 6.3. Constraints / indexes

Current mapping required size и literal lengths заменяется согласованной моделью incomplete metadata/own Version. Status consistency/check names/hash format/filename comparison — Q20/Q21/Q15. Hash index не unique; два uploads одинаковых bytes получают разные AssetIds. Published asset bytes недоступны для Update; EF conditional updates lifecycle не изменяют size/hash/location/upload time произвольно.

## 7. EntityFramework.Persistence

### 7.1. Repositories / query adapters

MediaAssetRepository загружает exact tracking root и применяет Add/Remove/conditional writes, без собственного скрытого commit. List/Get handlers — projections/specifications §3.5; GetImpact собирает public facts. Applicable gateway eligibility/access gate переиспользует metadata facts; ordinary byte read не query handler и не IObjectStorage.OpenRead pump. No query handlers в Application; no command handler с MinIO SDK в Persistence.

Metadata query не проверяет object existence сетевым HEAD при каждом read. Availability materialized state не доказывает отсутствие storage outage; gateway/storage read failure диагностируется отдельно, не переписывает автоматически Available в Failed.

### 7.2. Transactions

Для MVP владелец этих commits — MediaLibrary. По [утверждённому MVP consistency guard](../questions/Q20-persistence-and-migration.md#mvp-протокол-consistency-guard) все CM replicas используют одну PostgreSQL database и один стабильный глобальный exclusive transaction-level advisory key. Каждый affected owner scope явно начинает ReadCommitted transaction, затем отдельным statement вызывает pg_advisory_xact_lock на **той же physical connection и transaction**, до любых authoritative domain reads и row locks. После успешного acquire заново загружаются own roots/tokens и current foreign facts через public ports из независимых primary read scopes/connections. Предварительные query/preview/cache/replica facts не разрешают commit; foreign read не enlist-ится в ambient owner UoW, не разделяет SQL transaction и не открывает foreign EF из caller.

Guard удерживается до owner commit/rollback: изменяются только roots своего BC и атомарные own outbox/audit intent/inbox markers, где они нужны. Все invalidating writers, включая creation/removal/reference Copy/orphan paths, Publish/lifecycle/hard delete, setup/reconciliation, source durable acceptance/watermark/pending/apply/retry и F15 open/supersede/repair/resolve, участвуют в этом же ключе. Own Version/If-Match сохраняется: stale own token →412, изменившаяся foreign/preview basis →409 по Q15. Create/Copy из неизменённого source не увеличивает source Version; два serially guarded допустимых создания могут оба завершиться успешно.

Внешний I/O (broker, VR School, MinIO), ожидание пользователя и долгие streams выполняются вне gate. CPU preparation допустима заранее, но exact basis повторно подтверждается внутри gate; иной basis требует отказа или новой подготовки вне gate. Bounded guard wait/lock timeout конфигурируются, поддерживают cancellation и являются availability/technical failure, не domain409; точные значения и transport mapping остаются Q15/Q20. Connection loss прерывает owner transaction: reconnect-and-continue запрещён. Потеря commit ACK даёт unknown outcome; выяснение exact state/исходного operation ID по Q15 предшествует повтору, blind command retry запрещён.

Durable local boundaries:

1. Pending registration — короткий commit metadata до storage write.
2. Available или подтверждённый Failed — новый короткий commit после результата storage/verification.
3. Archive/Restore — own conditional metadata transition.
4. Eligibility/BeginDelete — согласованный reference protocol и короткий commit Deleting.
5. После подтверждённого storage delete — отдельный commit удаления metadata.

Не держать DB transaction открытой на весь upload/download. SaveChanges в ambient UoW не доказывает durable commit. Current [UnitOfWorkBehavior](../../../../contentmanager.api/src/BuildingBlocks/ContentManager.Cqrs.UnitOfWork/Behaviors/UnitOfWorkBehavior.cs) оборачивает команду, CompleteAsync выполняется после handler; он не обеспечивает эти независимые stages автоматически. Concrete orchestration/owner scopes/pipeline исключение фиксируется Q21, без несуществующего Begin(requiresNew) API по догадке.

Failure final commit классифицируется после rollback/disposal с fresh context: нельзя отвечать Available/deleted по prepared tracker. При DB отказе после storage write сохраняется Pending/unknown outcome и известный AssetId/location; при DB отказе после storage delete остаётся Deleting, чтобы не выдать обычный Available с удалёнными bytes. Не обещать rollback storage с rollback DB.

Reference race требует координации всех writer paths: F04 Save/Copy/Publish/orphan retention, применимые Publishing/Revision dependencies, deletion/Archive. Asset Version проверяет только asset row; отсутствие refs до commit недостаточно. Demo protocol выбран Q21: один CM process, singleton application-level AssetId guard для всех reference writers и Delete; удерживается через owning BC UoW commit. Acquire sorted distinct AssetIds предотвращает inverse-order deadlock; exact API/DI материализуются при реализации. Multi-replica не входит в Demo deployment; MVP использует выбранный global PostgreSQL advisory key §7.2. No distributed transaction/foreign contexts/synthetic count0.

Create/clean-create/copy/upload используют клиентский operation ID и durable result у owning BC по [Q15](../questions/Q15-api-and-source-code.md#согласованный-demo-профиль-authoring-api). Одинаковые operation ID и input возвращают исходный committed result с теми же identities; иной input для того же ID — conflict. Запись результата и создание локальных roots фиксируются атомарно; для upload учитываются durable stages F05. Потерянный response восстанавливается повтором той же операции, а не новым Create. Точный carrier/header, input comparison representation и result model — Q15; это целевой контракт, не факт существующей реализации.

Reference authority Demo — реальные ContentModeling Draft, Published и retained orphan references, включая recursive Object/List values. Все пути создания/сохранения этих references (Create, Save, Copy, schema reconciliation, Publish и recovery) берут общий AssetId guard и проверяют Available/kind до commit owning ContentModeling UoW. Delete под тем же guard повторно читает authoritative references, commits durable Deleting, после чего новый writer отклоняется по статусу. Full Publishing не обязательная зависимость Demo; при его подключении его реальные retained references также становятся authority, отсутствие BC не подменяется вымышленным count0.

### 7.3. Migrations

Нужны own Version, nullable incomplete facts, Archive/Deleting state/timestamps и выбранные recovery fields/indexes. Для незапущенного CM допустим согласованный destructive reshape Q20; backward-compatible production migration не вводится. Аналитика не выполняет migrations/cleanup существующего storage. Проверки clean PostgreSQL schema и реального provider входят в §11.

## 8. External adapters

### 8.1. S3 / object storage

В Demo конкретный provider — **MinIO**. Application сохраняет S3-compatible `IObjectStorage`; MinIO SDK/version/config находятся в Infrastructure. Multi-provider selector не вводится. SDK/client/options принадлежат infrastructure MediaLibrary; Application работает с IObjectStorage. Physical размещение adapter описывается Q21, не создаётся новый package ради имени слоя.

Put пишет объект с object name, derived from `MediaAssetId:D`; optional configured prefix не является identity.

Upload verifier использует central policy + signature detector за собственным port. Предпочтительный Demo adapter — `Mime-Detective 25.8.1` (актуальная NuGet-версия на 05.10.2026) с definition set, ограниченным разрешёнными форматами. Domain/Application не зависят от package API; библиотека может быть заменена без изменения contract. Поток/known length должны соответствовать выбранному SDK; spool cleanup, multipart abort, temp capacity, ограниченная память, проверка успешного завершения и actual size/hash — Q21. Digest S3 ETag не приравнивается к SHA-256 файла. В bucket не включается публичный доступ ради отсутствия Demo auth. Создание bucket на каждом upload, как сейчас, не является выбранной provisioning policy.

Host YARP получает immutable bytes напрямую от private storage после applicable gate; IObjectStorage остаётся для upload/delete/verification/recovery. Presigned/direct storage URL не основной client contract; future optimization только отдельным решением. Private MinIO access использует server-side credentials. Response headers/range/conditional/cache profile задан Q21 и проверяется integration tests.

Delete точного object считает подтверждённое отсутствие этого key допустимым повторным результатом. Permission denied, timeout и connectivity failure не превращаются в NotFound success. При unknown storage outcome сначала выясняется состояние точной операции; no blind overwrite/re-upload/delete другого object. SDK retries не должны скрывать lifecycle конкуренцию или безгранично повторять чтение request stream.

### 8.2. Public BC reference и Delivery boundary

ContentModeling сообщает Draft/Published/orphan dependencies по public contract, включая recursive values. Publishing/Delivery сообщают applicable retained Revision/frozen dependencies. Counts/уникальность и полнота — Q15/Q21; одинаковая ссылка в нескольких списках не определяет единицу count сама по себе. Archived owner history не исключается из usage автоматически.

MVP consumer authorization выполняет Delivery/CM gateway gate по применимому контексту до output cache/proxy, используя media eligibility facts; byte pump через Application не нужен. Exact archived bytes остаются доступны историческому consumer; запрет new reference не превращается в физическое удаление. Политика existing Draft/new references уточняется Q12, transport доступа Q15. Demo не получает скрытую зависимость на полный Publishing/licensing.

### 8.3. Host YARP gateway и response ownership

Host регистрирует YARP через AddReverseProxy/route-cluster configuration и MapReverseProxy. Public route фиксирован `/storage/assets/{assetId}` и разрешает GET/HEAD. Upstream object name вычисляется напрямую из того же GUID + configured prefix; DB lookup `AssetId → ObjectKey` для маршрутизации не нужен. MediaLibrary Domain/Application не зависят от YARP/MinIO SDK.

Перед protected proxy/cache/304/range выполняются current authentication/access/session/revision/lifecycle проверки F13/F16. Demo не требует auth. В Demo routing не делает metadata lookup только ради storage key; final namespace используется для readable assets. В MVP current authorization/session/revision/lifecycle eligibility gate выполняется до proxy/cache и может читать CM projection/state. Это access check, не key-resolution. Client credentials не storage credentials; upstream private access/signing (SigV4 если требует provider), safe header/query forwarding и internal Host semantics выбираются infrastructure-side, не anonymous bucket по догадке.

YARP/forwarder владеет upstream response и server transfer; streaming/cancellation/disconnect/timeouts/cleanup проверяются на реальном provider. Content-Type хранится при upload, Content-Length передаётся если provider его возвращает. Range/HEAD/conditional/cache validators и error bodies проверяются фактически. Нельзя буферизовать весь файл в byte[] либо дописать JSON ProblemDetails после начала binary response.

OutputCache optional и bounded; media body cache не является обязательным слоем. Binary response использует SHA-256 based content ETag, Last-Modified=UploadedAt и configurable Cache-Control; Demo default 24h + immutable. Файл выше maximum cached body продолжает скачиваться через proxy. Default200/GET/HEAD не подтверждает206/authenticated caching; custom protected cache требует access **до lookup** и safe isolation/invalidation. Exact policy/headers/ordering/health/metrics — Q21; browser/Unity success и server proxy completion — разные наблюдения.

## 9. Data flows

MVP порядок для durable stages DF02/DF06, DF07–DF08 и recovery: input/CPU preparation вне gate → явная owner ReadCommitted transaction → standalone exclusive advisory acquire → fresh own roots и foreign primary-port facts → проверки own token/basis/lifecycle/dependencies → Domain action → только own writes/atomic intents → commit и release. Это обязательный префикс каждого указанного mutation/retry stage; строки «load/guard» ниже означают загрузку **после** acquire. Read-only query/impact/preview остаются observations; при необходимости короткий source capture имеет отдельный guarded scope, но долгий stream и пользовательский review не удерживают guard. Demo использует свой согласованный контур без новых обязательных MVP dependencies.

Каждый поток связан с §5. Queries не создают lifecycle transition. Post-commit DTO/ETag не строится из неподтверждённых изменений. Промежуточный stage не равен окончательному пользовательскому success.

### DF00. Media policy

Validated server configuration → immutable policy projection kinds/formats/MIME/max-bytes → frontend/API DTO. Missing/invalid per-kind configuration fails startup or explicit policy read; frontend никогда не становится authority.

### DF01. Library / picker

Validate query/policy → MediaAssetsListSpecification with search + kind/format/date/status and composed usage/schema filters → count по тем же filters → stable sort/page → lightweight projection → envelope. Archived исключён из normal picker, selectable только Available. Empty/error отличаются; Pending/Failed/Deleting recovery visibility и normal library filters Q15. Нет HEAD/S3 reads каждого списка и нет загрузки bytes.

### DF02. Upload

1. Api связывает multipart stream, selected AssetKind, filename/MIME/length metadata и cancellation. Policy query/config валидирует kind/max-size; filename не storage key.
2. Application генерирует AssetId/location/RegisteredAt; MVP начинает короткую owner ReadCommitted transaction, берёт global advisory key до fresh operation/eligibility facts, создаёт PendingUpload и фиксирует metadata/operation result. Demo использует отдельный короткий UoW. Если этот commit не прошёл, object write не запускается.
3. Потоковый verifier считает actual bytes/size/SHA-256 и проверяет extension + declared MIME + actual signature/container against selected AssetKind policy; whole file не хранится в RAM. При local validation failure последующее состояние/cleanup policy Q21; usable result не возвращается.
4. IObjectStorage записывает точный новый object, подтверждает выбранные facts. DB transaction не открыта на время передачи. Partial/multipart object и unknown timeout относятся к этой попытке.
5. Новый guarded UoW (MVP: fresh owner transaction → standalone global advisory acquire → fresh Pending/operation facts) повторно подтверждает attempt basis, вызывает MarkAvailable(actual facts, server time), conditional commit. Конкурентный recovery/Delete/lifecycle не допускает ложного Available после удаления; protocol Q21.
6. Только durable Available → AssetDto/new own ETag200. F04 form/server content не меняется.
7. При отказе storage/verifier attempt не становится Available; MarkFailed допускается только с корректной классификацией и собственной durable записью. Если failed-state commit невозможен, остаётся Pending/unknown, не fake Failed success. Object write success + DB failure recovery проверяет exact object/attempt, actual size/hash/type: verified object завершает Available новым durable commit либо cleanup удаляет только объект собственной попытки и фиксирует Failed. Несовпадение/unknown не выдаёт success; новый blind upload не запускается.

Потеря HTTP response после final commit означает неизвестный исход для клиента, а не rollback. Same operation ID/input возвращает durable original attempt/result; exact AssetId partial-failure/result carrier — Q15/Q21; endpoint registration не добавляется автоматически.

### DF03. Exact metadata

Exact AssetId specification → no-tracking projection stored facts → AssetDto + own metadata ETag либо404. Bucket/provider endpoint/internal operation secrets не возвращаются; отдельного public ObjectKey нет. Чтение Pending/Failed/Deleting не продолжает upload/delete само; metadata200 не означает доступные binary bytes.

### DF04. Прочитать bytes через CM gateway

1. CM Host принимает GET/HEAD конкретного AssetId в разрешённом media namespace. Проверяет route/method/ID и допускаемый header/query profile; upload/delete и произвольный internal object path не проходят.
2. Demo перед cache/proxy читает authoritative MediaLibrary eligibility через public port: только readable Available допускается, Pending/Failed/Deleting блокируются. Route GUID напрямую определяет upstream object; eligibility lookup не является routing lookup. MVP добавляет current consumer rights/session/revision gate. Client/object metadata не является permission authority.
3. После access применяется HTTP validator/cache path: SHA-256 content ETag, UploadedAt Last-Modified, Cache-Control. `If-None-Match`/304 и Range не обходят MVP gate. Mapping GUID → configured prefix/object infrastructure-side без metadata DB lookup. Client Authorization не upstream storage authority.
4. На cache miss YARP forwarder обращается к private S3 с выбранной server-to-server authentication/signing, передаёт raw response/content headers/bytes потоково. HEAD/Range/206/416/conditional/storage failures сохраняют согласованный profile; нет OpenAssetContentQuery/IObjectStorage.OpenRead/MVC pump.
   Для resume `If-Range` проверяет сильный CM content validator: quoted HTTP ETag из server SHA-256, не S3 multipart ETag/metadata Version. При несовпадении validator возвращается full representation по HTTP semantics, не прежний fragment; `206` содержит согласованные `Content-Range`, total length и фактический `Content-Length`, `416` — корректную информацию о длине. Forwarder не перекодирует/сжимает bytes representation и использует bounded buffers/64-bit byte counters. Каждый новый HEAD/GET/Range/resume заново проходит eligibility и MVP exact session/membership checkpoint до headers/cache/304/206. Продолжительный MVP response контролируется независимой deadline/renewal lease F13; expiry/denial/unknown abort-ят upstream/downstream, SQL gate не держится на transfer. Наличие manifest или успешного прежнего HEAD не заменяет эти проверки.
5. Forwarder управляет transfer/cancellation/resource lifetime; после headers error/truncation не JSON envelope в binary body. Proxy metrics безопасно фиксируют status/bytes/duration/cache/range/destination error. Поздний read failure не меняет committed instance content/AssetId или sync result F13.
6. Клиенты используют CM-owned URL convention и AssetId, не endpoint/bucket/ObjectKey. Реальные frontend/Unity/provider tests подтверждают equality и headers, а не один static route config.

### DF05. Delete impact

Exact asset → public ContentModeling facts (все Draft/Published/orphans) + применимые historical authorities → полный usage outcome/counts/blockers → informational CanDelete. Unavailable/incomplete authority не count0. Preview не держит guard/transaction до modal; state/ETag query DF03 отдельно. Не допускается delete только потому, что текущий UI не показывает reference.

### DF06. Physical delete

1. MVP: явная owner ReadCommitted transaction → standalone global advisory acquire → fresh exact root/own ExpectedVersion; missing404, stale412, status eligibility policy. Demo acquire выбранного AssetId guard предшествует load. Preview не authority.
2. Demo продолжает под уже acquired AssetId application guard; MVP уже держит global advisory key шага1 и читает полный current ContentModeling/применимый historical usage через public ports в независимых primary read scopes. Все reference creation/removal/Copy/orphan/Publish/lifecycle/delete writers используют тот же profile через owning UoW commit. Любая Draft/Published/historical dependency →409 asset.in-use с blockers, no cascade.
3. Под защитой protocol повторно подтвердить eligibility и BeginDelete; commit Deleting/новый Version. Не выполнять storage delete до успешного durable stage. Durable Deleting запрещает новые refs после освобождения Demo process guard либо MVP transaction advisory key и во время storage/delete recovery stages; все writers проверяют статус под guard.
4. После commit Deleting transaction/gate полностью освобождены. Idempotent Delete exact storage object; подтверждённый NotFound считается выполненным storage stage, остальные ошибки сохраняют durable Deleting и retry той же операции. Failure не возвращает окончательный success.
5. В новом UoW (MVP: explicit ReadCommitted transaction → standalone global advisory acquire) fresh перечитать exact Deleting/operation facts, conditional Remove metadata → commit → deleted ID/выбранный HTTP result. Другой asset/новая попытка не затрагиваются.
6. Сбой после шага4 оставляет Deleting до resume; resume идентифицирует эту операцию и соблюдает preconditions Q21/Q15. Повтор старого If-Match не bypass412. Missing metadata после lost response не доказывает, кто удалил asset; idempotent external outcome не обещается без контракта.

Recovery повторяет конкретную durable Pending/Deleting операцию, не является GC. Deleting retry сохраняет operation/AssetId, блокирует refs, idempotent storage delete завершается до metadata removal. Exact trigger/limits/endpoint/header signatures уточняются Q21; автоматическое удаление произвольных unused objects не вводится.

### DF07–DF08. MVP Archive / Restore

MVP owner ReadCommitted transaction/advisory acquire → fresh exact root/ExpectedVersion → current lifecycle/eligibility facts new-reference writers → Archive(server time) или Restore → conditional metadata commit → полный AssetDto/new ETag. Storage не переписывается и не удаляется; UploadedAt/size/hash/location прежние. Archive hidden normal library/picker, запрещены новые refs; существующие historical reads продолжаются. Restore возвращает выбор/new use, не Publish/Activate/content rewrite. Same-state/repeated command outcomes Q15/Q21; Archive не требует отсутствия historical refs.

## 10. Ошибки и диагностика

MVP guarded failure различает confirmed business refusal и невозможность проверки: unavailable/incomplete public facts не означают empty/valid facts. Guard wait cancellation/timeout и connection loss — technical/availability outcome без domain Conflict; точные wire codes остаются Q15. После connection loss scope завершается rollback/disposal без продолжения на новой connection. Unknown commit outcome выясняется по exact state/operation ID; prepared tracker не подтверждает ни success, ни rollback. Own stale ETag сохраняет412, foreign basis change —409. Диагностика фиксирует wait/hold duration, owner/operation, acquire/commit/rollback/unknown phase без full payload и секретов.

| Условие | Исход / обязательная гарантия |
| --- | --- |
| Malformed multipart/invalid metadata/actual size/type verification |400/413 profile Q15; нет usable fake asset/partial Available |
| Unknown AssetId |404, без lookup другого filename/key |
| Pending/Failed/Deleting/new use Archived |asset.not-available/lifecycle profile; точная category Q15 |
| Any content/history dependency |409 asset.in-use, dependency count/blocker, refs не очищаются |
| Missing/stale If-Match |428/412, не общий409 |
| Storage timeout/write/open/delete или DB commit failure |5xx/final profile Q15; durable stage сохраняется для recovery, no distributed rollback claim |
| Unavailable reference authority |Destructive operation fail closed, не пустой impact/успешный delete |
| Final commit success/response lost |Unknown клиентский outcome; no blind upload/delete retry/false Failed badge |
| Gateway read abort после headers |Прерванный binary response, forwarder/upstream cleanup/диагностика; no JSON in bytes/no false success |

Логи: correlation/DF/AssetId, operation stage, prior/current status, bytes count, digest/outcome, duration и storage error category. Не логировать содержимое файлов, credentials, authorization/session tokens и полные presigned URLs. Метрики upload/gateway download bytes/duration/failures, Pending/Failed/Deleting count/возраст по доступным timestamps; числовые alert/retry defaults Q22. Логи prepared Available не считаются success до commit.

## 11. Тестирование

Дополнительная post-Demo приёмка большого файла: реальный CM Host + PostgreSQL + YARP + MinIO, контролируемые disconnect/expiry/revoke/206/416 faults. Проверить SHA-based strong ETag/If-Range match и mismatch, точность ranges/headers/полных bytes, bounded RAM и Int64 counters при 5–10 ГБ, fresh authorization даже warm cache/HEAD/304/206, independent lease interruption и resume только после нового admission. Large upload отдельно доказывает inbound completion/abort/recovery и Available size/hash; download tests не являются upload proof. Profile/limits и actual fixtures остаются Q37, это future acceptance.

Стратегия по [S04](../standards/S04-testing.md): Unit — Domain/constraints/verifier/mapping/orchestration с controlled clock и явными storage/reference doubles для отказов. Integration — real PostgreSQL Testcontainers и реальный выбранный S3-compatible provider (current MinIO fixture как исходная точка). Functional — actual HTTP Api/WebApplicationFactory + real metadata/storage, Demo anonymous. Architecture — физические слои/CQRS/public BC boundaries. Несколько BC в одном API сами по себе не превращают Functional в E2E.

Собственные IDs/database/object prefix per test, отдельные bounded fixtures/cleanup; сохраняемые credentials test-local и не выводятся. Recovery/races — независимые contexts/process attempts/barriers, без Sleep. Test object удаляется только по своему exact prefix/location, failed stage не уничтожает чужие bytes. Fault doubles доказывают обработку ошибки, но не реальные storage guarantees/stream lifecycle. Current MediaUploadTests проверяет старый MinIO/presigned contract и не заменяет новые проверки; здесь тесты приложения не запускались.

| ID / DF | Уровень / SUT | Воздействие | Наблюдаемый результат |
| --- | --- | --- | --- |
| T01 / DF02 | Unit + Integration verifier/storage | Known bytes, declared size mismatch, arbitrary read chunks | Actual size/SHA-256 exact; limits policy, memory bounded; no fake Available |
| T02 / DF02 | Integration real PostgreSQL+storage | Failure before Pending commit/before-after object write/final commit | Верные durable stages, no orphan success; recovery contract выбран Q21 |
| T03 / DF00,02,04 | Integration + Functional HTTP | Image/Audio/Video policy, valid signatures, spoofed ext/MIME, replacement upload | Policy一致; mismatch rejected; new bytes→new AssetId; old ID byte-identical |
| T04 / DF01,03 | Integration composition + Functional | Search/page/kind/format/date/used/schema/status/notfound | Correct server filter/page; schema usage through public port; no foreign SQL join; descriptor metadata |
| T05 / DF04 | Functional real CM Host/YARP/MinIO | GET/HEAD, ETag/304, Audio/Video Range206/416, large streaming/disconnect/missing object | Byte equality/headers/lifetime, no application OpenRead pump/full byte[]/storage topology leak; file above cache body limit still downloads |
| T06 / DF04 | Functional gateway / real access-policy integration | Historical Archived/Pending/Deleting, authorized/denied/wrong-session, warm cache/304/Range, revoke/withdraw | Current access before cache/proxy, historical bytes retained; Range206/416/HEAD/conditional actual profile, metadata ETag not content validator |
| T07 / DF05,06 | Integration public adapters | Recursive Draft/Published/List/Object/orphan/frozen dependencies | Any retained ref blocks delete; full authority, unavailable не count0 |
| T08 / DF06,F04 | Real chosen coordination protocol | Save/Copy/Publish new ref vs asset Delete/Archive | Нет dangling reference/удаления retained bytes; mocks/precheck alone недостаточны |
| T09 / DF06 | Integration faults + real storage | BeginDelete commit failure/object NotFound/timeout/final DB failure | No premature object delete; exact retry cleanup, durable Deleting, stale old ETag не bypass |
| T10 / DF07,08 | Unit + Functional + Integration | Archive/Restore/ETag race/existing history | Correct own token/lifecycle, bytes/hash unchanged, picker/new refs eligibility, no automatic publishing |
| T11 / all writes | Functional actual HTTP/OpenAPI | Missing/stale If-Match/list tag/upload no tag/NSwag |428/412 vs409; no Version/ObjectKey/credentials wire; generated metadata headers correct; gateway non-JSON/errors handled separately |
| T12 / DF02,06 | Functional + UI composition | Lost response/cancel/doubleclick/context switch | Durable result не false rollback; chosen recovery без blind write retry |
| T13 / all | Architecture/DI | Handler placement/provider types/public boundary | Commands Application, queries Persistence; no foreign contexts/Domain SDK/fullPublishing/auth/broker Demo dependency |
| T14 / end-to-end | Реальный CM+storage+SDK/Unity contour | Upload → picker/F04 Save/reopen → Publish → sync data → bytes/render; новый файл | AssetId survives/reusedbytesold, real rendering; поздний file failure не отменяет sync data |

T14 не подменяется тестом отдельного handler или browser mock. Тесты range/cache/retry/limits пишутся после принятия профиля Q21/Q22, ожидаемое поведение не выбирается молча.

Дополнительная Demo приёмка: warm cache/304/Range не обходят public eligibility lookup; Pending/Failed/Deleting unreadable. Storage success + failed metadata commit восстанавливается через exact size/hash verification и Available commit либо cleanup собственного attempt. Concurrent Create/Save/Copy/Publish/orphan-retention против Delete участвуют в одном application-level AssetId guard через owner UoW commit; no-refs → durable Deleting → retryable idempotent storage deletion → metadata removal. Multi-replica запуск без durable coordination не удовлетворяет выбранной Demo гарантии. Это target tests, а не утверждение о выполненной проверке приложения.

MVP Integration matrix для durable stages DF02/DF06, DF07–DF08 и recovery использует real PostgreSQL, отдельные owner connections/UoWs и barriers, включая разные CM replica processes/hosts там, где проверяется общность key. Doubles/in-memory locks не доказывают SQL coordination.

| MVP сценарий | SUT / воздействие | Наблюдаемый результат |
| --- | --- | --- |
| Guard до чтений | Реальный Persistence adapter; writer A держит gate, B начинает owner transaction до acquire; A меняет eligibility/basis и commit, B продолжает | B после acquire читает новый committed state при ReadCommitted; stale предварительные facts не применяются; lock statement использует owner connection/transaction |
| Меж-BC write/read partition | Public read adapter и Host/DI; owner держит guard, foreign primary read отдельным scope | Read не enlist-ится в owner transaction, не читает cache/replica; no foreign writes/EF access; atomic commit только owner + intents |
| Commit/rollback/release | Barriers для invalidating writer и affected operation; injected owner failure/cancellation | Второй writer ждёт release и revalidates; failed owner полностью rollback, нет partial roots/intents; unrelated next scope получает key |
| Wait и connection failure | Configured bounded wait/cancellation, разрыв owner physical connection | Technical/unavailable, не409/empty success; aborted transaction не продолжается через reconnect |
| Unknown commit ACK | Разрыв response/commit acknowledgement и inquiry exact state/original operation ID | Committed result сохраняется, rollback не предполагается; no blind duplicate mutation |
| Delete против references | Барьер: first scope собирается BeginDelete, второй Create/Save/Copy/orphan/Publish writer | Writer-first commit создаёт retained ref и блокирует delete; Delete-first Deleting commit закрывает new refs; storage I/O после release; final removal reacquires key |

## 12. Порядок реализации

1. Уточнить необходимые для первого потока wire/limits/read ownership/recovery stages Q15/Q21, reference policy Q12 и implementation выбранного single-process AssetId guard Q21.
2. Domain root/constraints/lifecycle и Unit checks; EF metadata/own Version/consistency/migration.
3. Application commands/validators/verifier и storage port/real adapter; DI и короткие независимые durable UoW.
4. Upload/metadata/list API и Host YARP GET/HEAD gateway, deterministic mapping/private access/signing/safe transforms/optional cache; real bytes/Range/conditional/denial/lifetime checks, NSwag metadata contract.
5. Public reference readers/coordination и delete impact/delete/recovery; реальные гонки F04 writers.
6. Frontend library/detail/picker/upload и реальная Demo chain; MVP Archive/Restore/Delivery eligibility в своей области.

## 13. Открытые технические вопросы

[Q37](../../frontend/questions/Q37-launcher-asset-delivery.md): большой inbound multipart/resumable upload, допустимые размеры/ресурсные guardrails и actual long-transfer испытания. Существующие Q21 SHA-based content ETag, CM-owned route и access-before-cache правила не переоткрываются.

| Вопрос | Требуемая детализация |
| --- | --- |
| [Q21: F05](../questions/Q21-media-storage.md#upload-и-lifecycle) | MinIO/IObjectStorage, public route, Guid-derived identity, kind policy, SHA validators/cache/Range и preview уже приняты. Остались multipart/spool/verification implementation, short UoW, wire/API materialization recovery/Deleting retry, implementation общего Demo AssetId guard/retention, YARP visibility/private signing/header-query transforms и actual provider/lifetime checks |
| [Q15: F05](../questions/Q15-api-and-source-code.md#f05-детализация-целевого-контракта) | DTO/part/header/filters/counts/blockers/error mappings, mutation success/recovery correlation, authoring vs runtime access, NSwag |
| [Q12](../../../business/questions/Q12-media-and-retention.md) | Archived existing Draft/new reference policy, read audience/retention границы; no implicit dependency loss |
| [Q22](../../../business/questions/Q22-quality-targets.md) | Размеры/нагрузка/temp storage/concurrency/delays/recovery targets без выдуманных числовых defaults |
| [Q20](../questions/Q20-persistence-and-migration.md), [F04](F04-entity-instances.md) | MVP guard принят; concrete mapping/DI и recursive orphan provenance/extraction; безопасный delete не выводится из flat current mapper |

Stable AssetId, immutable Available bytes, real upload/download, запрет удаления любых referenced assets, MVP Archive с историческим чтением и отсутствие automatic GC уже приняты. Technical TBD не отменяют эти гарантии и не означают готовность кода.
