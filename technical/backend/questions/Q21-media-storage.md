# Q21. Хранилище медиа и YARP gateway


## Принятое решение


Demo использует **MinIO** как конкретный S3-compatible provider. Application работает через `IObjectStorage`; MinIO SDK, bucket, credentials и transport configuration остаются Infrastructure. Архитектура не вводит multi-provider manager и не делает MinIO частью публичного API: другой S3-compatible adapter может быть подключён позднее без изменения F05/F19 contracts.


Пути разделены:


- upload нового asset → CM HTTP API → MediaLibrary Application → `IObjectStorage` → private MinIO;
- catalog/metadata/lifecycle/delete → CM API/Application;
- immutable bytes → CM-owned `/storage/assets/{assetId}` → Host/YARP → private MinIO;
- клиент не получает MinIO endpoint, bucket, credentials или presigned URL как основной contract.


### Identity и object naming


`MediaAssetId` — server-generated `Guid` и единственная identity ресурса. Отдельный независимо генерируемый `ObjectKey` **не существует**. Final object name выводится из того же GUID; leaf object name равен canonical `AssetId` (`D` format). Допустимый infrastructure prefix (`assets/`) и bucket задаются конфигурацией, но не создают вторую identity.


Если physical storage name сохраняется в БД, он является производным storage metadata и обязан соответствовать `AssetId`; он не может расходиться с ним или быть случайным opaque key. `FileName` никогда не определяет storage path. Public DTO возвращает `AssetId` и asset metadata; отдельный `ObjectKey` наружу не выдаётся.


CM вычисляет immutable metadata при upload и хранит её в MediaLibrary DB. Та же серверно подтверждённая metadata дублируется в MinIO object metadata там, где это полезно для transport/diagnostics: AssetId, AssetKind, confirmed Content-Type/format, size и SHA-256. DB остаётся source of truth каталога и lifecycle; object metadata не становится authority для mutable status/access.


### Asset kind и format policy


Demo поддерживает ровно три `AssetKind`:


- `Image`;
- `Audio`;
- `Video`.


`AssetKind` выбирается специалистом при обычном upload и задаётся схемой для `AssetReference`. После `Available` kind immutable. В схеме остаётся один `FieldDataType.AssetReference`; его definition обязательно содержит ровно один `AssetKind`. `List<AssetReference>` наследует kind из recursive item definition. Отдельные FieldDataType `ImageReference`/`AudioReference`/`VideoReference` не вводятся.


Допустимые форматы задаёт **централизованная backend policy**, а не каждая схема. Demo policy минимум:


| AssetKind | Extensions | Основные MIME |
| --- | --- | --- |
| Image | `.jpg`, `.jpeg`, `.png` | `image/jpeg`, `image/png` |
| Audio | `.mp3`, `.ogg`, `.wav` | `audio/mpeg`, `audio/ogg`, `audio/wav`/эквивалентный подтверждённый WAV MIME |
| Video | `.mp4` | `video/mp4` |


Frontend не hardcode-ит эту таблицу. Он запрашивает policy у backend и использует её для `accept`, tooltip/help, picker filters и предварительной обратной связи. Backend остаётся authority и повторно валидирует upload. Per-kind maximum size также обязательная configuration policy и возвращается frontend; продуктовый числовой default не выдумывается. Отсутствующая обязательная configuration должна обнаруживаться при startup/config validation, а не превращаться в unlimited upload.


Для Demo video acceptance фиксирует MP4 container. H.264/AAC является рекомендуемым compatibility fixture для Unity/браузера, но generic file-signature detector не обязан доказывать codec compatibility; фактический Unity player profile проверяется F19/Q29/Q32.


### Определение типа файла


Backend не доверяет одному расширению либо client `Content-Type`. Проверка сопоставляет выбранный `AssetKind`, allowlist extension/MIME и фактическую file signature/container. Mismatch отвергается до `Available`.


Реализация file-signature detection находится за infrastructure/application port и не становится Domain dependency. Предпочтительный implementation adapter для Demo — `Mime-Detective`. На момент проверки 05.10.2026 актуальная NuGet-версия — `25.8.1`; пакет поддерживает signature-based определение по stream/bytes. Используется ограниченный definition set только на разрешённые форматы, а версия фиксируется обычным central package management проекта. Domain/Application contract от библиотеки не зависит. Если detector не может надёжно определить конкретный разрешённый формат, upload не считается автоматически безопасным только из-за extension. Основание политики: ASP.NET Core рекомендует server-side extension allowlist и file-signature validation.


- https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-10.0
- https://www.nuget.org/packages/Mime-Detective/


### Upload и lifecycle


Demo использует один обычный streaming multipart upload через CM API; whole-file `byte[]` и обязательный resumable/chunk upload не вводятся. Сервер потоково считает actual size и SHA-256, выполняет format verification и пишет через `IObjectStorage`.


Upload создаёт новый `AssetId`. Bytes `Available` asset immutable: «перезалить/заменить файл» всегда означает создать **новый MediaAsset с новым AssetId**, после чего специалист явно выбирает его в нужном Draft. Старые Published/history references продолжают получать прежние bytes; automatic mass repoint запрещён.


Состояния: `PendingUpload`, `Available`, `Failed`, `Deleting`; MVP дополнительно `Archived`. Pending/Failed не selectable. Их можно увидеть через status filter для диагностики и удалить согласованным manual cleanup flow. Automatic retention/GC для незавершённых assets в Demo не вводится.


Physical delete разрешён только при доказанном отсутствии Draft/Published/history/orphan references. Delete-impact выполняется перед destructive operation; cascade очистки `AssetReference` запрещён. Недоступная reference authority не трактуется как `count = 0`.


### Reference coordination и partial-failure recovery

Для Demo выбран один CM process и shared application-level **AssetId guard**. Все ContentModeling reference writers (Create/Save/Copy/Publish/reconciliation/orphan retention и recovery) и Delete используют один singleton guard с stable order distinct AssetIds и удерживают его через commit своего owning BC UoW. Reference writer под guard читает Available/kind facts. Delete под guard читает authoritative реальные Draft/Published/orphan references; неполная authority fail closed. Full Publishing не mandatory Demo dependency. Cross-BC SQL/FK/transaction не вводятся. Multi-replica требует durable coordination и не является выбранным Demo вариантом.

После no-refs check Delete commits **durable Deleting**, затем освобождает guard. Все новые reference writers reject Deleting; storage delete выполняется idempotently (NotFound = выполненный stage) и может повторяться для той же durable операции. Metadata удаляется отдельным commit только после подтверждённого storage deletion. Storage/DB failure оставляет Deleting для retry, не возвращает Available и не объявляет deleted заранее. Resume не обходится со старым ETag как с актуальным token; exact operation/header/result binding ещё материализуется Q15.

Upload success возвращается только после durable Available metadata commit. Если storage write успешен, а DB final commit не прошёл, recovery проверяет exact object собственной operation/AssetId и actual size/hash/type. Подтверждённый complete object позволяет завершить Available новым коротким UoW; иначе cleanup затрагивает только собственную attempt location и фиксирует failure. Unknown outcome не запускает новый blind Create/upload и не позволяет удалить чужой object. Operation ID/durable original result принадлежит MediaLibrary, exact schema/result/carrier — Q15/Q20.

Concrete target guard/public eligibility port/UoW scopes/recovery state mappings/signatures и retries/trigger limits остаются инженерной детализацией. Эти неизвестные не переоткрывают выбранные policies и не являются доказательством существующей конкурентно корректной реализации.

### MediaLibrary в Demo


F05 в Demo является полноценной переиспользуемой MediaLibrary, а не одной кнопкой upload. `/assets` предоставляет server-side search/pagination и как минимум фильтры:


- `AssetKind`;
- extension/format;
- upload date;
- `Used` / `Unused`;
- `EntitySchema`;
- status для diagnostic views.


Фильтр по EntitySchema означает assets, реально referenced текущими Draft и Published authoring data этой схемы. Historical-only references показываются в usage/detail/delete-impact, но не обязаны загрязнять обычную выборку «используется схемой». MediaLibrary не делает cross-BC SQL join: usage/query composition идёт через public ContentModeling port; отдельная usage projection добавляется только при доказанной необходимости производительности.


Catalog/detail показывает минимум: preview/иконку, FileName, AssetKind, extension/format, confirmed MIME, size, UploadedAt, usage summary/status и actions View/Download/Delete. SHA-256 доступен в detail. Tags не входят в Demo.


Из `AssetReference(Image|Audio|Video)` specialist может либо выбрать существующий совместимый `Available` asset, либо загрузить новый. Новый upload сразу становится обычным reusable asset каталога. Picker автоматически ограничен kind из schema definition; выбор несовместимого kind не откладывается до Save.


### Public DTO / value representation


Клиенту не нужно работать с «голым Guid» как единственной информацией об asset. MediaLibrary read models возвращают descriptor/value object с `AssetId` и серверно подтверждённой metadata: kind, FileName, format/extension, ContentType, SizeBytes, SHA-256, status, UploadedAt и CM-owned ContentUrl по применимому query.


Identity authoring reference при записи остаётся `AssetId`; клиентская metadata не trusted input. При Publish F04 через public F05 boundary под shared AssetId guard захватывает agreed immutable metadata в Published runtime AssetReference descriptor; descriptor входит в canonical values/hash по Q19. Delivery читает frozen snapshot без повторного enrichment; CM/storage URL не сериализуется, SDK вычисляет его из CM configuration + AssetId. Current descriptor/ContentUrl в MediaLibrary/authoring UI DTO — transient query result и не изменяет Published payload/hash. Точное wire представление descriptor для JSON Schema/codegen материализуется Q31/F21/F22.


### YARP, metadata lookup и access boundary


`/storage/assets/{assetId}` детерминированно отображается на final MinIO object name от того же GUID. Поэтому **DB lookup `AssetId → ObjectKey` для маршрутизации не нужен**. Host/YARP может вычислить upstream path из route value и configuration.


Demo gateway до cache/304/range/proxy обязательно читает authoritative eligibility через public MediaLibrary port. PendingUpload/Failed/Deleting не readable; отсутствие auth не отменяет lifecycle gate. Это eligibility lookup, а не AssetId→ObjectKey routing lookup: path по-прежнему выводится из route GUID. MVP добавляет current session/revision/entitlement checks; MinIO metadata не заменяет CM access authority.


Public gateway read-only: `GET`/`HEAD`; PUT/PATCH/DELETE/list через `/storage` запрещены. Upstream private access использует server credentials/signing; CM client Authorization не пересылается как MinIO authority. Response/error не раскрывают endpoint/bucket/credentials.


### HTTP caching, validators и Range


Bytes immutable под одним AssetId, поэтому binary contract использует long-lived HTTP/client caching:


- content ETag строится из server SHA-256 (не metadata `int Version` и не S3 multipart ETag);
- `Last-Modified` соответствует подтверждённому upload time;
- `Cache-Control`/`immutable` policy configurable; Demo default — 24h;
- conditional GET/HEAD с `If-None-Match`/`304` поддерживается;
- `Range`/`206`/`416` обязателен для реального Audio/Video path и проверяется с MinIO+YARP.


Server-side YARP/ASP.NET OutputCache не является обязательным хранилищем media bodies. Он optional/bounded и может включаться для подходящих небольших responses после измерений. File больше cache body limit всё равно должен нормально stream-иться. В MVP authorization выполняется до любых protected cache shortcuts.


Client raw/image boundary now selected [Q29](../../frontend/questions/Q29-unity-client-package.md#raw-assets-и-unity-image-helper-согласованный-контракт)/[F19](../../frontend/features/F19-unity-client.md#74-отдельная-загрузка-assets-и-unity-lifecycle): identity file bytes, accepted-header/open vs full transfer verification, explicit single Range/strong If-Range/200 rejection before append, owned stream/destination/file/texture. Demo SDK has no additional body/resource cache or persistent asset-resume; server Cache-Control/conditional304/Range policy remains above. Provider transforms/identity bytes/ETag/Content-Range/error/cancel and MVP access before cache require real gateway conformance; no new endpoint/descriptor fields/storage identity.

### Preview


CM UI использует безопасные preview по kind: Image preview, Audio player, Video player для browser-supported format. Unsupported renderer не делает asset недопустимым: detail/download остаются доступны. Произвольный upload не исполняется как HTML/script приложения.


## Большие файлы и общий SDK cache после Demo

[Q37](../../frontend/questions/Q37-launcher-asset-delivery.md) описывает Launcher selective preload и app on-demand с общим persistent SDK cache после самостоятельного Demo. F13 entities отдельно от media bytes, SQLite хранит metadata/jobs, крупные verified файлы — filesystem. Demo raw API без persistent media-resume остаётся прежним.

Для расширенного download нужны Range/strong If-Range, bounded stream, точный Content-Range/total size, durable checkpoints и full size/SHA-256 verification. 200 после Range не append, 416 не implicit Ready. Выбранный CM SHA-based strong ETag сохраняется; S3 multipart ETag не заменяет digest. F12/F13 защита выполняется до headers/cache/304/206 и при response checkpoints.

Inbound streamed/resumable multipart upload 5–10 ГБ, max size/limits/recovery **уточняются** Q37/F05. Наличие gateway Range download не доказывает upload readiness. DeliveryMode принадлежит schema definition, не MediaAsset.


## Application/API additions

F05 добавляет read operation upload-policy/media-policy. Она возвращает поддерживаемые `AssetKind`, allowed extensions/MIME и configured max size per kind. Точный route name следует REST conventions F05/Q15; frontend не имеет отдельной копии policy.


Library query принимает server-side search/page/sort и согласованные filters kind/format/date/used/schema/status. Usage filter/composition не создаёт physical FK между MediaLibrary и ContentModeling.


## Protected runtime response leases

MVP read gateway использует [Q16 Session/response lease protocol](Q16-activation-and-sessions.md#принятый-протокол-сессий-и-response-leases), [F13](../features/F13-content-delivery.md) — owning credential/pin/lease authorization, F05/Host — actual YARP byte path. Short own guarded checkpoint до headers/output-cache/304/Range проверяет Session, selected epoch/license/period/cutoff/pinned eligibility, membership и current exact bans/asset eligibility. Gate освобождается до forwarding; SessionId/AssetId/hash не permission.

Lease defaultmax5sec boundedSessionexpiry и next finite policy boundary (Freeze тоже). Independent local monotonic deadline cancels response/upstream even renewal awaits guard/source; periodic fresh checks renew only before expiry, late callback не resurrects cancelled transfer. Every Range/resume new checkpoint; no mandatory Redis/new SSE/control channel. Host strips client CM Bearer/cookies/secret authorization before private storage, signs using server storage credentials.

Known committed CM restriction denies new checkpoints; active transfer cancels on known/deadline. Five seconds — permission horizon, not hard physical network SLA or byte recall; buffers/already-sent bytes и source fact transport lag отдельно. Normal external VRS validation outage doesn't call/revoke every active media read; required CM fact/storage availability unknown still failclosed. Before headers typed error; after headers selected binary/forwarder abort, not JSON appended to file. SDK treats truncation Unknown until an observed reason/complete result; installed content not deleted.

**Открытые инженерные детали:** final protected route/context/Host transformer/cancellation linkage/provider fallback/error mapping, keyring/runtime scheme DI, measured lease expiry→YARP→upstream stop latency and cache/Range behavior. Real CM/YARP/MinIO tests cover deadline while renewal blocked, revocation on another replica, stale callbacks, 304/range auth, no Bearer forwarded/private Location leak and incomplete downstream file not accepted. Lease/TTL/pin semantics already selected; physical cancellation is not proven by documentation.

## Обязательные проверки Demo


| Boundary | Проверка |
| --- | --- |
| Upload policy | Backend policy и frontend hints/accept совпадают; manual spoof client не обходит server validation |
| File validation | Correct extension+signature accepted; extension/MIME/signature mismatch rejected; candidate detector не подменяет policy |
| PostgreSQL + MinIO | AssetId/object name invariant, metadata DB/object mirror, actual size/SHA-256, partial failure no fake Available |
| Catalog | Search/page/kind/format/date/used/schema/status filters, reuse existing asset, picker only compatible kind |
| Replacement | New bytes → new AssetId; old Published reference returns byte-identical old content |
| YARP | GET/HEAD computes path from AssetId, public eligibility lookup precedes cache/304/Range/proxy; Pending/Failed/Deleting unreadable; no topology leak |
| HTTP media | Conditional304, Range206/416, large streaming/cancel, Audio/Video path |
| Delete | Shared AssetId guard through owner commit on all writers; refs block deletion; durable Deleting blocks new refs; idempotent storage retry then metadata removal |
| Unity/browser | Real CM URL returns same bytes; Image/Audio/Video representative fixtures verified on target path |
| MVP gate | Authorization/eligibility precedes protected cache/proxy; knowledge of AssetId is not permission |


Числовые per-kind upload limits, operational timeouts/quotas и optional server body-cache capacity являются environment configuration, а не скрытыми бизнес-defaults. Это не блокирует Demo architecture; конфигурация обязательна и валидируется при запуске.


## Source evidence и незавершённая реализация


Scoped API src .cs/.csproj вне bin/obj/Migrations 05.10.2026 содержит прежний MinIO PresignedGetObjectAsync в MediaAssetHandler; YARP/AddReverseProxy/MapReverseProxy/OutputCachePolicy не найдены. Это ограниченный source scan, не отсутствие gateway во всех иных repositories и не выполненный API/storage/network test. Target реализация остаётся F05/F13/M02, unknown profiles выше. Исторические source snapshots ниже отделены от действующего решения.


### Актуальная исходная реализация F05


Checkout 04.10.2026 уточняет старый snapshot: MediaAssetHandler создаёт Pending объект в памяти, читает request во временный FileStream с DeleteOnClose, вычисляет фактический размер/SHA-256 lowercase hex, сверяет declared length, пишет MinIO object, вызывает MarkUploaded и только затем Add в DbContext. Durable Pending/Failed до storage IO отсутствуют; DB commit выполняет общий UnitOfWorkBehavior после handler. Storage object после успешного Put и failed DB commit может остаться без metadata. Domain status сам по себе не доказывает recovery.


Current download выдаёт MinIO presigned URL, public DTO содержит ObjectKey; library/delete/lifecycle HTTP routes и own asset ETag отсутствуют. Действующий target F05 использует CM-owned YARP gateway; прежний Application /content read не сохраняется как основной byte path. Current MinIO provider, временный файл/buffer, bucket creation на upload, TTL и 1GiB — исходные факты, не утверждённые target defaults. Source links в F05; existing PostgreSQL/MinIO test не запускался при этой аналитике.




## Current-state snapshot 01.10.2026


Текущий MediaLibrary handler использует MinIO client и S3-compatible bucket; default bucket в options — `content-manager-assets`. Upload проходит через CM, использует временный файл, потоково вычисляет SHA-256, сверяет фактический размер, загружает object с key на основе AssetId и только после успеха отмечает metadata Available.


Current download endpoint возвращает **presigned MinIO GET URL**, а не proxy stream bytes через CM. TTL default 600 секунд и current clamp 60..3600. Current backend/frontend ограничивают upload 1 GiB. `MediaAssetDto` также содержит `ObjectKey`.


Все перечисленные числа и способ download являются **current implementation**, не автоматически принятыми requirements. Действующее решение выбирает CM-owned YARP gateway для обычного read и исключает storage-specific `ObjectKey` из публичной модели; прежний presigned DTO — source gap, не target. Ограничение 1 GiB не считается согласованным продуктовым лимитом.

### Конкретизация Demo F05

Обязательные caps: `MediaUpload:ImageMaxBytes=20971520`, `AudioMaxBytes=104857600`, `VideoMaxBytes=1073741824`. Отсутствующие/неположительные значения и overflow request envelope блокируют запуск. HTTP/form limit вычисляется из configured максимума плюс 1 MiB; backend spool/inspection/hash используют ограниченные buffers. Existing byte/container verifier дополнен OGG; новая production detector dependency не потребовалась.

Согласованный recovery: multipart `file/kind/operationId`; DELETE JSON `OperationId/ConsentToken` и own If-Match. Durable receipt сохраняет исходный normalized input/fingerprint и подтверждённый result; operation ID не переиспользуется с иным input. Deleting resume требует original operation/consent и текущий own ETag. Реальные PostgreSQL/MinIO тесты отдельно проверяют lost storage response и failed final EF commit после внешнего I/O.

Binary gateway отдельно от metadata OpenAPI: base GET/HEAD inline; `?download=true` задаёт attachment filename только на CM response и не уходит в upstream. Private max-age 86400 + immutable, SHA-256 ETag, Range/If-Range, no-store failures и current lifecycle gate. Browser file playback/large-upload memory и Unity не следуют из backend conformance; .NET browser request streaming требует отдельной HTTPS/HTTP2 deployment проверки.
Binary cacheable ответы (включая conditional 304) содержат `Vary: Origin` независимо от наличия Origin: cached native media GET без CORS headers не должен подменять последующий browser fetch HEAD из UI. Проверено реальным Chrome после перехода из picker обратно в detail и отдельной PostgreSQL/MinIO gateway regression.
