# Q37. Launcher, режимы доставки ассетов и общий файловый кэш

## Источник и границы ответа

Пользователь передал [архитектуру доставки](../../../archive/content-manager-launcher-asset-delivery-architecture.md) 08.10.2026 и поручил перенести её в docs. [Карта переноса](../../../archive/launcher-asset-delivery-source-map.md) связывает разделы источника с действующими спецификациями. Источник различает принятые бизнесовые решения, инженерные предложения и открытые вопросы; примеры JSON/C#/URI, таблицы SQLite и план этапов не являются утверждёнными wire/DDL или доказательством реализации.

Q37 отвечает за межпроцессную интеграцию Launcher/Unity и расширенный media cache. Уже принятые серверные правила остаются у владельцев [Q16](../../backend/questions/Q16-activation-and-sessions.md), [Q19](../../backend/questions/Q19-content-hashes.md), [Q21](../../backend/questions/Q21-media-storage.md), [Q33](../../backend/questions/Q33-sync-resume-transport.md); семантическая идентичность поля — [Q34](../../../business/questions/Q34-schema-field-semantics.md), поставка/Player proof — [Q29](Q29-unity-client-package.md)/[Q32](Q32-sdk-package-distribution.md).

## Принятые правила

| Область | Правило |
| --- | --- |
| Первый target | Unity **2022.3.62f3**, Windows x64, IL2CPP для приёмки. Другие платформы не входят в первую реализацию; совместимость выбранного target ещё проверяется. |
| Разделение систем | ProjectStorage распространяет сборки; CM — структурированный контент и медиа; VR School владеет каталогом/лицензионными фактами. Миграционное соответствие legacy Project/Application и Product/ProductVariant не выводится из Name. |
| Entity sync | Launcher через DotNet SDK синхронизирует сущности и frozen AssetReference, публикует согласованный SQLite snapshot. Бинарные bytes не добавляются в F13 stream/plan/receipts. |
| Получение файла | Unity app самостоятельно обращается через SDK к CM-owned `/storage/assets/{assetId}` при cache miss. Private S3/MinIO остаётся за CM/YARP. |
| Общий механизм | Launcher preload и app on-demand используют один SDK download/cache механизм и общую локальную структуру. DotNet владеет координацией; adapters выполняют platform I/O. SQLite хранит данные/метаданные; крупные binary files — файловая система. |
| Владелец режима | DeliveryMode принадлежит exact versioned definition типа AssetReference: обычному leaf-полю, вложенному leaf или recursive List ItemDefinition. Object/List-контейнеры и другие scalar types режима не имеют. |
| Default | OnDemand: без автоматического предварительного скачивания; ручной запрос через SDK допустим. |
| Предзагрузка | Preload: Launcher скачивает заранее/фоном; незавершённость или ошибка не блокирует запуск и не откатывает entity snapshot. |
| Обязательные файлы | RequiredBeforeLaunch: запуск данного snapshot запрещён, пока все реально присутствующие обязательные ссылки не имеют verified bytes. Пустое optional поле не создаёт job/blocker. |
| Разные свойства | IsRequired проверяет наличие значения экземпляра; RequiredBeforeLaunch — готовность файла уже указанного AssetReference. Режим не является глобальным свойством MediaAsset или entitlement. |
| Authoring | Draft inspector показывает режим рядом с AssetKind, default OnDemand; Published только для чтения. Изменение требует новой версии. Create полей не принимает клиентский SchemaFieldId. |
| История | Frozen runtime AssetReference сохраняет пять полей: AssetId, AssetKind, ContentType, SizeBytes, SHA-256. Режим не добавляется в value; старые Published bytes/hashes не переписываются. Historical definitions без свойства имеют effective OnDemand. |
| Большие файлы | Нужны Range/If-Range resume, bounded memory, 64-bit счётчики, полная проверка размера/SHA-256, отдельные Downloading/Verifying/Ready и восстановление после обрыва. Partial file не является Ready. |
| Optional Addressables | Add-on может предоставлять custom entity/asset providers поверх SDK. Для Unity-built prefab/scene остаётся совместимый native AssetBundle/catalog pipeline. Add-on не становится базовой зависимостью. |
| Demo | Самостоятельные F19/F20 direct sync и raw image flow сохраняются. Launcher, protected admission, preload/RequiredBeforeLaunch и расширенный persistent cache относятся к последующему интеграционному этапу. |

## Предлагаемая инженерная схема

Это направление реализации из источника; точные API, модели, codecs и platform primitives **уточняются**, а не объявляются frozen контрактами.

### Разрешение требований к файлам

Предлагается immutable AssetDeliveryManifest для одной exact разрешённой ProductVariantPublicationRevision. После Product/Variant composition, overrides/exclusions и materialization resolver рекурсивно сопоставляет exact SchemaVersion definitions с exact Published InstanceVersion values. Попадают только присутствующие валидные ссылки, frozen size/digest и режим из той же exact schema version.

Внутри Variant/revision предлагается группировать verified asset identity (AssetId + expected size/digest) и брать наиболее строгий режим: RequiredBeforeLaunch > Preload > OnDemand. Один физический файл может использоваться повторно, но режимы и права разных scopes не сливаются. Manifest связывается с тем же pinned revision/snapshot, что entity sync; latest Registry не подменяет зафиксированную definition.

Manifest — производная опись, а не право. URI/DTO/format/hash/order/carrier не выбраны; пример нельзя вставлять в F13 public wire v1. Новая schema metadata/annotation требует согласованного versioned profile либо отдельного metadata digest; silent изменение cm-schema-content/1, cm-json-schema/1, cm-csharp/1 запрещено. Совмещение с уже запланированным MVP bundle Q34/Q35 — открытый вопрос.

### SDK, состояние и конкуренция

Предложены DotNet ports IAssetDownloadManager, IAssetCacheIndex, IAssetBinaryStore, IRuntimeDeliverySessionProvider. Имена и пример сигнатур — проектные предложения. Coordinator выполняет single-flight, Range protocol, integrity/recovery, progress и typed failure; Http/Unity transports, SQLite и filesystem adapters реализуют I/O. Addressables не дублирует алгоритмы.

Предлагается SDK-managed local root вне заменяемых ProjectStorage builds, scopes Installation/Variant/Snapshot, один Launcher writer entity snapshots и согласованные cache writes обоих процессов. SQLite WAL/short transactions, межпроцессные ownership leases с fencing generation, durable job checkpoints и file pins требуют проверки native provider/Windows IL2CPP. HTTP/file I/O не удерживает SQL transaction.

Job/download index хранится отдельно от F13 SynchronizationId/PlanId. Файлы разделены на partial и verified objects. File publish и metadata commit не образуют одну транзакцию: proposed порядок — full verify → atomic filesystem publish → SQLite Ready commit, с crash reconciliation. Orphan ready file требует проверки до использования.

Повторный requester присоединяется к той же job. Отмена одного ожидания не удаляет работу других consumers/required policy. File lease удерживает bytes от eviction на время использования; abnormal termination требует безопасного обнаружения stale pins. Primitives, таблицы/индексы и public result/error contracts остаются открытыми.

### Resume и integrity

Resume использует durable confirmed offset и strong HTTP validator; новый GET проходит текущее право. Проверяются 206, точный Content-Range/total size и длина. 200 после Range не дописывается к partial; 416 не означает автоматический успех. Confirmed bytes обновляются после durable file write, не по callback. После получения всех bytes проверяется SHA-256 всего файла.

В Q21 **уже выбран** CM strong ETag на основе server SHA-256. Замечание источника об отличии digest и ETag не отменяет этого: HTTP validator имеет свой формат, S3 multipart ETag не заменяет digest. If-Range, upstream validator translation и реальное CM/YARP/MinIO conformance подлежат проверке.

Disk-full, integrity mismatch, interruption, stale ownership, auth expiry и denial имеют отдельные результаты; нет false Ready. Для 5–10 ГБ используется verified local file/lease, а не byte[]. Видеопотребитель получает file abstraction и локальный VideoPlayer path; raw MP4 не объявляется VideoClip. HTTP/adaptive streaming и CDN остаются отдельной будущей ветвью.

### Admission и запуск

Предлагаются отдельные EntitiesReady, RequiredMediaReady, PreloadInProgress и LaunchAllowed. Работающий Player привязан к своему snapshot; подготовка следующего не смешивает данные и не требует hot reload.

Каждый remote GET/HEAD/Range следует F12/F13 session/membership/checkpoint rules до cache/304/206. Истечение absolute session TTL требует **нового admission**, не refresh старого bearer. Безопасный bootstrap/re-admission для app, особенно при закрытом Launcher, не выбран. Launcher-mediated proof либо контролируемый app credential provider — предложенные альтернативы, ни одна не принята автоматически. Кэш не устанавливает offline entitlement, fallback на старую ревизию или revocation policy.

## Открытые вопросы

| ID источника | Что уточняется | Связанный владелец |
| --- | --- | --- |
| TBD-01 | Domain/EF/DTO placement, enum/presence codec, новый canonical/metadata profile и Registry annotation; совместное versioning с constraints/Offset | Q15/Q19/Q31, F01–F03/F21 |
| TBD-02 | Изменение только DeliveryMode в новой версии с сохранением SchemaFieldId | Q34; предложенное compatibility rule, не закрытое решение |
| TBD-03 | Manifest placement/API/DTO/format ID/hash/order, authorization/binding и exact repeatable read | F13/Q15/Q19 |
| TBD-04 | Возможный future per-Variant override: сейчас исключён; добавление требует отдельного решения | Business F01/F13 |
| TBD-05 | Новый admission после TTL при закрытом Launcher; actor/proof/IPC/secret ownership | F12/F13/Q16 |
| TBD-06 | Offline/revocation/local cached bytes, старый snapshot при blocked новом | Business F12/F13, Q01/Q12 |
| TBD-07 | Queue priority/fairness/preemption, ranges, concurrency/bandwidth, quota/disk reserve/retention/GC и числовые guardrails | Q22/F19 |
| TBD-08 | Cross-process locks/leases/fencing/pins, SQLite native provider/DDL и Windows IL2CPP proof | Q29/Q32/F19 |
| TBD-09 | Lifetime Preload при закрытии Launcher, manual Pause/Resume и переходах сети | F19, launcher integration |
| TBD-10 | Streamed/resumable multipart inbound upload, max server size и recovery | F05/Q21; download Range не доказывает upload readiness |
| TBD-11 | SDK names/signatures/errors, resource keys/provider mapping, IL2CPP DTO preservation и Addressables package version | F19/F23/Q29/Q32/Q31 |
| TBD-12 | ProjectStorage legacy → canonical Variant mapping и exact build SchemaVersion requirements | Q11/Q23/F14 |

Распределение полного hardening по MVP/Production также уточняется: §14.5 источника — рекомендуемая декомпозиция, а не утверждение всего объёма milestones. Default/owner/modes уже приняты и не переоткрываются.

## Приёмка и проверка реализации

Будущая приёмка по F-документам: schema default/applicability/nested definitions/immutable history; exact composition/modes/optional refs/scope isolation; two-process single-flight/pinned snapshots; 10 ГБ bounded memory/long counters; Range 200/206/416; crash между file publish и metadata commit; integrity/disk-full; required launch gate и optional failure; session expiry/denial/checkpoints; actual Unity 2022.3.62f3 Windows x64 IL2CPP typed DTO/Texture2D/video/file lease; inbound upload recovery.

Проверки используют реальные CM/PostgreSQL/YARP/MinIO и packaged Launcher/app/native SQLite с управляемыми faults. Editor smoke и проверка документации не доказывают runtime compatibility. Числовые лимиты/package versions выбираются после representative fixtures/измерений. Логи/метрики различают sync/download/verification/launch readiness; bearer/raw keys/private URLs не записываются. Metric names и runbook commands из источника остаются предложениями.
