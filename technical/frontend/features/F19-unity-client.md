# F19. Unity-библиотека клиента CM: клиентская спецификация


[Бизнесовая спецификация](../../../business/features/F19-unity-client.md) · [Backend Delivery F13](../../backend/features/F13-content-delivery.md) · [Клиентский контракт F13](F13-content-delivery.md) · [M05](../modules/M05-delivery.md) · [Отдельная сцена F20](F20-unity-demo-scene.md).


## 1. Результат и границы


Разработчик подключает самостоятельную библиотеку, задаёт полный реестр точных схем/версий и доступ к клиентскому состоянию, вызывает одну высокоуровневую синхронизацию и получает согласованный локальный набор, типизированное чтение, прогресс, возможность остановки и итог. SDK получает план CM, скачивает необходимые точные данные, применяет изменения и обновляет inventory. Приложение не повторяет вручную цепочку серверных вызовов и выбирает момент запуска: lifecycle, кнопка или таймер не являются обязательной политикой библиотеки.


Общий runtime SDK называется **`Sdk.Client.DotNet`**, целевой .NET target — `netstandard2.1`. Unity-платформа — **Unity 2022.3.62f3**, её адаптер — `Sdk.Client.Unity`; `Sdk.Client.Http` предоставляет обычный .NET transport. Прежнее имя `Sdk.Client.Core` не применяется к целевой runtime-библиотеке; независимый `Schema.CodeGeneration.Core` сохраняет имя. OpenAPI, JSON Schema и Schema API / [F21](../../backend/features/F21-schema-registry.md) остаются общими формальными контрактами, не заменяются API DotNet.


Архитектура допускает других .NET/C#-потребителей, например Godot C#, desktop или console; это не матрица подтверждённых платформ и не обязательная реализация их адаптеров в Demo. SDK для TypeScript, C++, GDScript, Rust либо Unreal может независимо потреблять те же формальные контракты; .NET SDK не становится фундаментом для всех языков.


Demo включает данные, изображения через отдельный raw asset API, progress/cancellation, default и настраиваемые retries, устойчивое resume и codegen-модели из F21/F22. Runtime Auth, лицензирование, шина, VR School и полное продуктовое Publishing не возвращаются в Demo. MVP protected delivery описана [F13](../../backend/features/F13-content-delivery.md); synchronizationId не подменяет SessionId или session credential. После Demo общий download/cache manager и выборочная launcher предзагрузка описаны §6.8; полный offline-first, binary patching и автоматическое скачивание всех assets не вводятся.


[F20](F20-unity-demo-scene.md) владеет отдельной демонстрационной сценой. [F23](F23-unity-addressables.md) владеет optional Addressables add-on, который не нужен для обычной картинки. [F21](../../backend/features/F21-schema-registry.md) и [F22](F22-csharp-model-generation.md) описывают получение схем и генерацию C# до runtime, без генерации поведения/UI. Серверной копии F19 нет: серверные модели, операции и алгоритмы принадлежат F13/F21/F22.


Основания: [SDK-архитектура](../../../archive/unity-client-sdk-architecture.md), [schema/codegen](../../../archive/entity-schema-codegen-architecture.md), [уточнение имени/контрактов](../../../archive/sdk-contracts-architecture-update.md), актуальные решения Q29/Q32/Q33. Действующая спецификация отделяет требования от проверенной реализации. SDK расположен в самостоятельных contentmanager.sdk.client и contentmanager.sdk.client.unity; серверные F13/F21/F22 остаются в contentmanager.api. Текущие проверки и незакрытые gates описаны в §10 и [плане реализации](../../backend/f19-demo-implementation-plan.md). F19 ещё не завершена: managed build и native PlayMode tests в Unity 6000.6.5f1 Editor проверены; согласованный Unity 2022.3.62f3 Windows x64/IL2CPP Player остаётся непроверенным.


## 2. Страницы, экраны и навигация


Не применяется как обязательный UI: F19 — библиотека с программными точками входа, без собственной страницы, web route, формы входа, диалога восстановления или обязательной навигации. Приложение принимает результаты/callbacks и выбирает их представление. Inspector-конфигурация Unity относится к платформенному адаптеру; она не заменяет программную конфигурацию и не вводит runtime-экран.


| Место использования | Вход и контекст | Используемое поведение | Завершение |
| --- | --- | --- | --- |
| Unity-приложение | Адрес CM, полный exact registry, state adapter, generated models | Публичный Unity facade и DotNet sync; typed чтение и отдельная загрузка изображения | Приложение использует committed данные и различимый результат |
| .NET/C#-приложение | Тот же контекст через DotNet и подходящий transport | Та же оркестрация без UnityEngine | Нет зависимости от Unity-сцены |
| Демонстрационная сцена F20 | Согласованный Demo-контекст | Переиспользует F19; основной состав сцены описан в F20 | Показывает реальные данные и изображение, не реализует второй SDK |


Закрытие или аварийное завершение приложения не отменяет сохранённый unfinished context. Следующий вызов SDK проверяет его и автоматически продолжает пригодную попытку. Это не обязательный вызов при каждом старте приложения. Local run generation отсекает поздние callbacks, parsing results и control ACK прежнего запуска; они не заменяют новый набор/контекст. Явный новый selection intent допускает новый plan после согласования прежней работы; exact API смены registry/lifecycle остаётся [Q29](../questions/Q29-unity-client-package.md), принятое recovery/control поведение — [Q33](../../backend/questions/Q33-sync-resume-transport.md#execution-control-и-transport-profile).


## 3. Карта FSD-блоков


### 3.1. Применимость уровней


FSD, Blazor Standalone, MudBlazor и NSwag как web UI stack относятся к административному Content Manager, согласно [обзору frontend](../README.md). Для F19 они не назначают Unity-проекты, каталоги `pages/features/entities/shared`, Razor-компоненты или глобальный web store. Раздел сохраняет назначение шаблона — карту ответственности; ниже показаны принятые SDK-слои и их логические блоки. Каждая строка не требует отдельной физической сборки.


### 3.2. Состав именно этой фичи


| Блок | Слой / граница | Ответственность | Использование | Основное описание |
| --- | --- | --- | --- | --- |
| Runtime API и сериализация | `Sdk.Client.DotNet` | Entity/Assets API, typed exact metadata, canonical profile/digest verification, errors/cancellation/transport | Целевой общий runtime; pure common .NET library reused, не новый frontend framework | Разделы 5, 6, 8; neutral models once backend F03 |
| Sync-координатор | DotNet orchestration | ID, Start/Status/Resume, plan, download, staging, atomic apply, прогресс, остановка/отказ, итог и preflight | Один публичный вызов полного процесса | Разделы 6–9 |
| Клиентский state adapter и typed store | Порт DotNet ↔ клиентское хранилище | Registry, inventory, committed data/index, durable recovery и atomic group | SDK предоставляет интерфейс и готовый Demo adapter; SQLite first candidate после Unity/native/AOT/platform spike | Разделы 5–7; Q29/Q32 |
| `IHttpTransport` | Технический порт DotNet | Передать HTTP-запрос, получить ответ/поток, распространить cancellation | Заменяемый transport, имя из источника | Разделы 5, 8 |
| Обычный HTTP-адаптер | `Sdk.Client.Http` | Реализовать transport через .NET HttpClient | Для обычного .NET-потребителя | Раздел 5 |
| Unity facade и адаптеры | Sdk.Client.Unity | UnityWebRequest transport/stream bridge, Inspector/logger/dispatcher и raw image helper | Implements DotNet ports, delegates common sync; no mandatory HttpClient adapter | §5.3–§5.5 |
| Generated models | Код потребителя, созданный F21/F22 | Типизированная структура exact schema-version | Получены до runtime, без редактирования `.g.cs` вручную | Раздел 6; F21/F22 |
| Addressables add-on | `Sdk.Client.Unity.Addressables` | Mapping, Load/Preload/Release и платформенный кэш | Optional; здесь только граница | F23 |


### 3.3. Обязанности блоков


| Блок | Назначение и входы | Выходы | Собственное состояние | Зависимости / состав | Жизненный цикл и потребители |
| --- | --- | --- | --- | --- | --- |
| Runtime API | Exact identity/model metadata, AssetId/config/cancellation; проверить supported profile+digest и строгий typed canonical input | Verified bytes/typed data/integrity metadata либо error координатору | Parser state, без latest selection/numeric guessing/profile selection | F13 carriers + pure common .NET library/neutral inputs once backend F03; no Domain/EF/ASP dependencies, Q29/Q31 | Verification before staging/group apply; unknown profile/corruption не automatic reserialize+rehash |
| Sync-координатор | Принять полный selection отдельно от bindings; получить actual inventory через store | SyncResult/SyncProgress, automatic Resume, handle-based Stop/Discard | Stable scope gate до ID; durable workflow §6.4/§7, configured retry/timeouts |
| State adapter / typed store | Принять полные проверенные records/checkpoints и complete exact группу | Durable staging, atomic commit данных/removals/inventory/index/local receipt; typed чтение committed набора | Клиентский registry, installed, staging и receipts; network position не durable state | Заменяемое хранилище Q29; stable scope gate/cross-process adapter proof Q29/Q32 | Переживает закрытие процесса; Stop сохраняет unfinished; Discard ждёт определённого atomic outcome и сохраняет installed/inventory/committed receipts |
| Transport и HTTP-адаптеры | Уже сформированный request и cancellation | Immutable head/incremental body/terminal outcome | One HTTP attempt/bounded response resources; NDJSON в DotNet | IHttpTransport DotNet → HttpClientTransport или UnityWebRequestTransport | Owned response §5.3–§5.4; no full sync buffer |
| Unity facade / helpers | Получить конфигурацию приложения, SDK-операцию, AssetId и lifecycle/cancellation | Результат/progress приложению, поддерживаемый Unity-ресурс либо его самостоятельная ошибка | Только платформенное состояние/lifetime; нет второго diff/resume | DotNet, Unity transport/config/logger/main-thread integration | Init/config и уничтожение потребителя координируются с отменой/cleanup; точные callbacks и ownership Q29 |
| Generated models | Принять payload своего exact контракта | Typed данные для store/приложения | DTO-значения, не MonoBehaviour/ScriptableObject | Формальный F21/F22 profile и согласованный serializer | Generated source пересоздаётся tooling; смена schema-version не изменяет тип молча |


Модели/signatures §5.1/§6.2 задают SDK contract. В contentmanager.sdk.client уже реализованы ContentManagerClient, SyncCoordinator, SynchronizationProtocolClient, NdjsonRecordReader, GeneratedBindingRegistry и RuntimeIntegrityValidator; adapters остаются отдельными физическими проектами. Область проверенной реализации ограничена §10; целевая спецификация не объявляет Unity/поставку готовыми.


### 3.4. Физические проекты и внутренняя декомпозиция

| Проект | Target components | Где реализация / зависимости |
| --- | --- | --- |
| Sdk.Client.DotNet | IContentManagerClient/IHttpTransport/clock/logger и common models; ContentManagerClient, SynchronizationCoordinator, SyncProtocolClient, NdjsonRecordReader, RuntimeIntegrityVerifier, GeneratedModelCodec, TypedContentReader, AssetDownloadService, RetryPolicy | Portable algorithms и interfaces вместе; netstandard2.1/common serializer/canonical helpers; no Unity/native/CM Domain/EF |
| Sdk.Client.DotNet net10.0 | HttpClientTransport/response/body owner | .NET implementation; excluded from portable target |
| Sdk.Client.Store | IClientStateStore, immutable metadata and typed errors | Independent storage abstractions |
| Sdk.Client.Store.Sqlite | SqliteClientStateStore, StoreScopeLock, StageRecordWriter, GroupActivationWriter, CommitOutcomeReader, CommittedContentQueries, StoreFormatMigrator | Implements Store port; private SQL/provider/native/OS lock; no HTTP/Unity business logic |
| Sdk.Client.Unity | UnityContentManagerClient, UnityWebRequestTransport, UnityStreamingDownloadHandler, UnityResponseBodyStream, UnityMainThreadDispatcher, UnitySdkLogger, UnityTextureLoader/handle | Implements neutral ports + Unity-only helpers; own source/asmdef, no duplicate sync |
| Sdk.Client.Unity.Addressables | Mapping provider, Addressables runtime adapter, operation/handle owners | Optional F23, own mapping/catalog/runtime dependency; no reverse base dependency |

Карта показывает обязанности компонентов. Текущая DotNet реализация разделена на Client/Bindings/Runtime/Assets; adapters не копируют state machine/hash/retry/wire mapping. GetPageAsync читает committed SQL snapshot через store и десериализует через DotNet codec; adapter не генерирует DTO и не ищет Schema Registry. Detailed interfaces/adapters §5.3–§5.5, local tables §6.7.


## 4. Логическая компоновка


UI-дерево F19 не требуется. Следующее дерево показывает композицию обязанностей SDK, а не визуальную вложенность или выдуманную структуру Unity solution:


```text
Приложение-потребитель (в том числе F20)
  Контекст: CM address + полный exact registry + доступ к client state
  Публичный Unity facade [для Unity] / DotNet API [для .NET]
    Sync-координатор DotNet
      Entity / plan / status / resume / data / preflight API
        Заменяемый HTTP transport
      State adapter
        Committed data + typed index + actual inventory
        Durable operation context + проверенный staging + checkpoints
    Typed чтение committed данных
    Отдельный Assets API → raw stream → Unity image helper
  Представление результата / игровая логика приложения
Optional Addressables: отдельный add-on F23 над Unity/DotNet
Developer tooling: F21/F22 → generated source до runtime
```


| Связь частей | Передаваемые сведения | Координатор | Наблюдаемый результат |
| --- | --- | --- | --- |
| Приложение → SDK | Полный exact registry и конфигурация; adapter даёт фактический inventory | Приложение выбирает момент, DotNet ведёт execution | Одна новая либо восстановленная операция |
| SDK → state adapter | Полные проверенные records, complete group, removals и inventory | DotNet | Durable staging/checkpoint; только validation и atomic commit с local receipt обновляют рабочий набор |
| State adapter → typed read → приложение | Подтверждённые данные/index точного контракта | Приложение/F20 обновляет свой показ | Несколько версий не схлопываются; неполная группа остаётся прежней |
| SDK → приложение | Progress, исход установки, freshness либо unknown | DotNet, показ у приложения | Прогресс скачивания не выдаётся за готовность установленного набора |
| Приложение → Assets API/helper | Отдельный AssetId и lifecycle | Приложение | Изображение/ошибка ресурса отдельно от successful sync данных |
| Preflight → приложение | Актуально / есть изменения / проверить не удалось | DotNet | Следующий sync после успеха выбирает приложение |


Приложение может продолжать читать прежний рабочий набор, пока SDK готовит staging. Успешные целые группы становятся доступны независимо от ещё не завершённых групп; нельзя показать половину новой группы. Unity facade передаёт результат, а не назначает окно или навигацию. Основное UI-описание примера остаётся в F20.


## 5. Зависимости и публичные границы


Направление: приложение → Unity facade при использовании Unity → DotNet API/координатор → transport и state adapter. Unity transport реализует технический порт DotNet; DotNet не вызывает Unity напрямую. Addressables зависит от Unity и .NET-контрактов, обратной зависимости нет. Generated models используют согласованный контракт сериализации без зависимости от административных Domain/EF.


Sdk.Client.DotNet не зависит от UnityEngine/Godot/Addressables/S3/MinIO/server Domain/EF или native SQLite. DotNet -> Store; Store.Sqlite -> Store. HttpClientTransport входит только в net10.0 target DotNet; Unity реализует IHttpTransport portable target. Base Unity no Addressables/schema-driven UI dependency; desired/diff authority CM, no client-side server diff.


| Публичная возможность | Вход | Результат / граница | Точность |
| --- | --- | --- | --- |
| Высокоуровневая sync | Full selection, CancellationToken, optional progress; inventory from store | Task<SyncResult>, automatic valid Resume/full workflow | SynchronizeAsync §5.1; no ID/cursor/generation input |
| Stop и Discard | Opaque SynchronizationAttemptHandle | ControlResult, expected failure/stale outcome distinct | StopAsync/DiscardAsync §5.1; no stale intent retarget |
| Typed store/read | Explicit registered exact T, key/instanceId/page | LookupResult<CommittedEntity<T>> or committed page | GetByKeyAsync/GetByIdAsync/GetPageAsync §5.1; no latest/edited persistence |
| Entity API | Exact identity из плана и model context | Typed payload, без неявного latest | Примеры `IEntitiesClient`/`GetAsync` из источника не заменяют F13 wire contract |
| Assets API | AssetReadRequest, optional expected frozen descriptor/range, caller destination/target | AssetOpenResult/owned Stream handle, AssetTransferResult/HEAD metadata | DotNet API §5.2 and models §6.5; Opened/transfer/Verified separate |
| Transport | HttpTransportRequest + CancellationToken | IHttpTransportResponse/owned Stream/head/Completion | Interface DotNet, HttpClientTransport в Http или UnityWebRequestTransport в Unity; §5.3–§5.4/§6.6 |
| Unity raw helper | Frozen Image descriptor, cancellation/progress | UnityTextureResult/owned UnityTextureHandle | LoadTextureAsync §5.2: verified JPEG/PNG, fixed display profile, main-thread handoff/Destroy |
| Конфигурация и диагностика | Адрес, timeout/retry/logging и state boundary | Обычная программная config; Unity Inspector/logger adapter | Точные поля/default numbers и callbacks Q29, без секретов |


Внешний API блока не равен HTTP API. Приложение принимает callback/result без обратной ссылки SDK на классы сцены. Изменение этих границ затрагивает F20 и других .NET/Unity-потребителей. Публичное имя DotNet не назначает физическое имя сборки, namespace, NuGet/UPM ID либо Git-репозиторий. NSwag admin client и runtime SDK не объединяются для повторного использования серверных классов.


### 5.1. Public DotNet API

Signatures below are SDK API, not new HTTP routes. Configuration contains source/partition, explicit binding registry, transport and store; the first public operation freezes bindings. Supported root T is the unchanged F22 generated exact binding, no assembly scanning or runtime Registry fetch. Register<T>() conflict rejects configuration before network/install.

```csharp
void Register<T>();

Task<SyncResult> SynchronizeAsync(
    IReadOnlyList<ExactSchemaSelection> selection,
    CancellationToken cancellationToken,
    IProgress<SyncProgress>? progress = null);

Task<SynchronizationState> GetSynchronizationStateAsync(
    CancellationToken cancellationToken);

Task<ControlResult> StopAsync(
    SynchronizationAttemptHandle attempt,
    CancellationToken cancellationToken);

Task<ControlResult> DiscardAsync(
    SynchronizationAttemptHandle attempt,
    CancellationToken cancellationToken);

Task<LookupResult<CommittedEntity<T>>> GetByKeyAsync<T>(
    string key, CancellationToken cancellationToken);

Task<LookupResult<CommittedEntity<T>>> GetByIdAsync<T>(
    Guid entityInstanceId, CancellationToken cancellationToken);

Task<LookupResult<CommittedPage<T>>> GetPageAsync<T>(
    int pageSize, string? continuation,
    CancellationToken cancellationToken);
```

SynchronizationState expected storage/status failure is represented by its SdkError (§6.2); no read error masquerades as no unfinished attempt. Public methods require valid arguments/own live client; expected operational errors are typed results, misuse/unexpected defects exceptions. State read does not silently Resume/acquire Data. Stop/Discard capture immutable handle intent, current Status cannot retarget its server fence; an unbound handle can stop its local reader/Start request but cannot fill its old server fence from arbitrary later Status or claim server ACK. Reconcile early reservation and obtain a fresh explicitly observed handle before remote control; pending/unknown remote outcome remains typed, ready target never recaptured. Main SynchronizeAsync cancellation means Stop/Stopped even if token already cancelled (no new ID/network in that case); control-call cancellation leaves outcome unknown until reconciliation, never evidence of applied Discard.

Selection belongs one managed scope and is persisted in original attempt input. Register<T>() only declares supported deserialization. Every Demo selected exact version needs supported binding; deselected historical group can still be removed by exact identity. Inventory comes from active committed rows, not caller DTO. Task-based facade does not imply nonblocking SQLite provider; storage scheduling belongs adapter, Unity callbacks dispatcher.

Typed lookup maps exact generated ContentSchema binding to stored group and decodes verified bytes into detached T; no latest, hidden runtime Registry fetch or edited persistence. Returned wrappers/counters/receipts immutable. Each page uses one short committed read snapshot; continuation binds group hash/profile and scope. Changed snapshot -> LookupOutcome.SnapshotChanged, caller starts paging again. Empty group has real hash; missing representation -> NotFound, corrupt binding/storage/parse -> Failed. MaxPageSize256 и group/scope-bound token codec реализованы; SQL indexes остаются private.

### 5.2. Raw assets и Unity image API

Target DotNet API uses neutral RuntimeAssetReferenceDescriptor mapped from the five-field owner F04 contract; no server Domain/EF assembly and no second descriptor shape. Assets use configured CM read context captured for operation; no caller-provided storage URL/arbitrary redirect. Unity types appear only in Unity facade.

```csharp
Task<AssetOpenResult> OpenReadAsync(
    AssetReadRequest request, CancellationToken cancellationToken);

Task<AssetTransferResult> DownloadAsync(
    AssetReadRequest request, Stream destination,
    CancellationToken cancellationToken,
    IProgress<AssetProgress>? progress = null);

Task<AssetTransferResult> DownloadToFileAsync(
    AssetReadRequest request, string targetPath,
    CancellationToken cancellationToken,
    IProgress<AssetProgress>? progress = null);

Task<AssetMetadataResult> GetReadMetadataAsync(
    Guid assetId, CancellationToken cancellationToken);

// Sdk.Client.Unity only
Task<UnityTextureResult> LoadTextureAsync(
    RuntimeAssetReferenceDescriptor image,
    CancellationToken cancellationToken,
    IProgress<AssetProgress>? progress = null);
```

OpenReadAsync reports Opened only for accepted headers/body; AssetReadHandle.Completion reports terminal transfer result. Stream.ReadAsync keeps normal Stream exceptions; owned wrapper records expected failure in typed Completion. Download/image helpers map expected transport/IO/integrity/decode/cancel to result; misuse/unexpected defects follow §5.1. AssetReadHandle.Dispose and UnityTextureHandle.Dispose idempotent. No asset request changes sync/store/frozen descriptor.

DownloadAsync leaves caller destination open/writes current position; no silent Seek/Truncate/retry after first write. DownloadToFileAsync requires full request (no Range), creates exclusively owned temp in target directory, validates whole transfer, then no-overwrite move. Existing target/error/cancel/integrity failure never overwrites it; cleanup only owned temp, never caller partial destination. Atomic file move capability and actual cancellation/lifecycle require pinned-platform checks, no durable asset-resume protocol inferred.

GetReadMetadataAsync unconditional HEAD; headers may be absent. It does not read catalog or mutate expected descriptor. Standalone Demo raw API: no SDK body cache/conditional If-None-Match by default; unexpected304 is no downloadable body, not fabricated cached success. Explicit Range requires matching prior strong ETag in If-Range, one forward range, no multipart/suffix/automatic resume. Each request rechecks current CM gateway access; no cache or old response as MVP authorization substitute.

### 5.3. Интерфейсы DotNet и конкретные реализаторы

`Sdk.Client.DotNet` определяет платформенно-независимые interfaces, request/result models и реализует общую protocol/orchestration/integrity/recovery логику. Порт означает заменяемую техническую зависимость; это не перенос всей sync-логики в Unity/Http и не требование «DotNet содержит только interfaces». Ниже обязанности текущих neutral contracts и adapters двух SDK repositories. Public CLR exact bindings совместимы с unchanged F22 generated source; выпуск требует Q32 gates.

| Интерфейс / контракт | Где объявлен | Реализатор | Ответственность и предел |
| --- | --- | --- | --- |
| `IContentManagerClient` | DotNet | `ContentManagerClient` DotNet; `UnityContentManagerClient` делегирует ему | Public методы §5.1/§5.2; один coordinator, без второго Unity workflow |
| `IHttpTransport` | DotNet | `HttpClientTransport` в Http; `UnityWebRequestTransport` в Unity | Один HTTP attempt, immutable headers + incremental bytes; no NDJSON business parsing/retry/new synchronizationId |
| `IHttpTransportResponse` | DotNet | `HttpClientTransportResponse` / `UnityWebRequestTransportResponse` | Head/body/completion + own HTTP lifetime; не raw HttpResponseMessage/UnityWebRequest в DotNet |
| `IClientStateStore` | DotNet | `SqliteClientStateStore` в Store.Sqlite; custom consumer adapter | Operations §6.4, durable stage/checkpoint/activation/receipts и committed reads; не arbitrary CRUD/SQL connection |
| `ISdkClock` | DotNet | Portable `MonotonicSdkClock` DotNet; fake clock tests | GetTimestamp / GetElapsed / DelayAsync; monotonic deadlines, не Unity Time/frame count/DateTime.Now |
| `ISdkLogger` | DotNet | `UnitySdkLogger` Unity / consumer .NET logger adapter; no-op default | Log(SdkDiagnostic) по §6.6; logging не prerequisite commit/ACK |
| `IUnityMainThreadDispatcher` | Unity | `UnityMainThreadDispatcher` | Unity-only scheduling/cleanup/progress; не DotNet public dependency |

Common request/response/error/value types остаются в DotNet; `UnityWebRequest`, `Texture2D`, `AsyncOperationHandle`, native SQLite connection и HttpClient-owned response остаются в своих adapters. Unity-specific и Addressables-specific interfaces объявляются в соответствующем optional package, когда их типы не platform-neutral.

Технический transport contract:

```csharp
public interface IHttpTransport
{
    Task<IHttpTransportResponse> SendAsync(
        HttpTransportRequest request,
        CancellationToken cancellationToken);
}

public interface IHttpTransportResponse : IDisposable
{
    HttpTransportResponseHead Head { get; }
    Stream Body { get; }
    Task<TransportCompletion> Completion { get; }
}
```

SendAsync возвращает head/body после получения нужных response headers, до окончания body. HTTP4xx/5xx — HTTP response, который классифицирует DotNet; DNS/TLS/connect/timeout до headers — transport failure. После headers read error/cancel проявляется в Stream и Completion, не fake EOF. Body read-only/non-seekable, один reader; DotNet использует ReadAsync. Completion не доказывает NDJSON group commit, asset hash verification или decoded texture. Dispose idempotent, закрывает own response/handler/body и прекращает ненужный transfer; внешние shared dependencies этим контрактом не уничтожаются.

### 5.4. Реализация транспортов

#### Sdk.Client.Http: HttpClientTransport

1. Создать HttpRequestMessage из проверенного neutral request, скопировать URI/method/headers и bounded body. Request смысл/plan/cursor/generation/context формирует DotNet protocol client; transport их не переопределяет.
2. Выполнить `HttpClient.SendAsync(..., HttpCompletionOption.ResponseHeadersRead, cancellationToken)`. Client/handler переиспользуется; новый HttpClient на каждый record/operation не создаётся. Auto redirects/decompression не должны нарушать selected origin/identity-byte policy.
3. Скопировать status/response и content headers в immutable head. Получить response content stream и вернуть response owner, сохранив HttpRequestMessage/HttpResponseMessage до завершения чтения. Скопированный head не зависит от lifetime native objects.
4. Body wrapper считает действительно прочитанные bytes, завершает Completion на EOF/error/cancel/early dispose; ошибки после headers не становятся successful empty body. Body timeout/read-idle контролирует DotNet, не один общий HttpClient.Timeout. Это выбранный streaming режим [.NET](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpcompletionoption).
5. Dispose own request/response/stream/cancellation registrations once; injected HttpClient остаётся у caller. Если factory создала client/handler сама, owning composition освобождает их после shutdown SDK.
6. HTTP/1.1 явно разрешён для выбранного Demo. Версия HTTP не меняет ID/plan/checkpoint/atomicity; HTTP/2 или HTTP/3 возможны только при подтверждённой поддержке выбранного adapter/окружения. Negotiated protocol фиксируется по actual response там, где API его раскрывает; UnityWebRequest не выдаёт эту metadata, SDK её не угадывает. Transport не делает непрозрачный retry state-changing request: retry/reconciliation у DotNet.

#### Sdk.Client.Unity: UnityWebRequestTransport

Unity package реализует тот же IHttpTransport через `UnityEngine.Networking.UnityWebRequest`, без обязательного Sdk.Client.Http/HttpClient внутри Unity facade.

| Часть Unity adapter | Что реализует | Собственное состояние / lifetime |
| --- | --- | --- |
| UnityWebRequestTransport | Создать native request/UploadHandlerRaw/DownloadHandlerScript, start на dispatcher | One send attempt; bounded config; не sync state |
| UnityStreamingDownloadHandler | ReceiveData/length/completion callbacks → byte queue | Preallocated receive buffer, own bounded queued chunks |
| UnityResponseBodyStream | Queue → asynchronous forward reads | One consumer, partial-chunk offset, EOF/error/cancel terminal signal |
| UnityWebRequestTransportResponse | Immutable head + Body + Completion | Native request/handlers/registration owner до Dispose |
| UnityRequestOperation | Race-safe send/header/terminal/cleanup transitions | Current operation generation; completion/cleanup once |
| UnityMainThreadDispatcher | Native start/Abort/Dispose и callbacks | Живёт до cleanup owned requests/resources |

1. Main-thread dispatcher создаёт и запускает request. JSON POST использует explicit Content-Type + UploadHandlerRaw, а не WWWForm с иным encoding. SetRequestHeader выполняется до SendWebRequest; URI только configured CM operation context, redirect policy explicit, никакого permissive CertificateHandler.
2. Streaming response использует DownloadHandlerScript с preallocated buffer. ReceiveData получает chunks на main thread, буфер переиспользуется; SDK копирует только dataLength bytes в собственный queue block, не сохраняет ссылку на callback array. Callback не парсит JSON/не hash-ит/не выполняет SQL и не ждёт Task.Result/Wait. Эти свойства описаны [Unity ReceiveData](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Networking.DownloadHandler.ReceiveData.html).
3. Background consumer читает bounded queue через Stream.ReadAsync; возвращает использованные blocks в pool. Chunk не равен NDJSON record: LF/UTF-8/token может пересечь несколько callbacks, один chunk содержит несколько records. Decoder/record-size limit у DotNet. Unary JSON/error body также ограничен, unknown Content-Length не разрешает unbounded buffer. DownloadHandlerBuffer/downloadHandler.text для полного sync body не применяется.
4. ReceiveData не даёт awaitable backpressure: false прекращает request. Queue overflow фиксирует local typed transport failure и abort, не блокирует Unity frame/не увеличивает память без границы. DotNet не сохраняет partial record/cursor и восстанавливает transfer только по Q33 durable checkpoint/pinned attempt. Managed queue bound не является обещанием bound всех native Unity buffers.
5. Adapter наблюдает responseCode/GetResponseHeaders и выдаёт immutable head как только required headers доступны. X-CM-Sync-Execution-Generation нужен до конца body; SendAsync не может ждать isDone при серверном ожидании group receipt. Пустой/частичный header snapshot не публикуется как достоверный. На pinned Player отдельно доказать early headers; [GetResponseHeaders](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Networking.UnityWebRequest.GetResponseHeaders.html) сам не доказательство момента доступности.
6. Callback CompleteContent и UnityWebRequestAsyncOperation.completed — разные сигналы. Transport Completed только при известном завершённом HTTP body outcome и полном чтении queue; известный HTTP4xx/5xx с доставленным body также physical completion. Network truncation/DataProcessingError/abort не fake EOF. Own overflow/cancel reason сохраняется и не заменяется общей Unity error string. HTTP ProtocolError с известным status передаётся как HTTP response; DotNet читает bounded error и применяет common classifier.
7. Cancellation registration устанавливает terminal intent и dispatches Abort; race до создания/после headers/после success проверяет operation identity. Early Dispose запрещает дальнейшее чтение, будит consumer и планирует native cleanup. Abort не доказывает server Stop/Discard или local rollback; их выполняет coordinator по своим fences.
8. Dispose owns download/upload handlers по выбранным dispose*HandlerOnDispose flags; double cleanup исключается. Request и Unity objects освобождаются на dispatcher; injected shared dispatcher не уничтожается response. Shutdown composition сначала отменяет/drains requests и cleanup, затем завершает runner. Если consumer ушёл, поздний callback не обращается к disposed/new operation.
9. Low-level queue events/TaskCompletionSource используют asynchronous continuations; engine callback не выполняет inline heavy DotNet continuation. Синхронный blocking body read на main thread запрещён; facade/consumer используют async reads, core work и SQLite scheduling вынесены из frame.
10. Native Unity API не объявляется аналогом HttpClient VersionPolicy. Для Demo разрешён HTTP/1.1; headers, stalls/overflow/Abort/Dispose и Player memory всё ещё требуют Q32/Q33. Whole-response buffering и замена выбранного UnityWebRequest на HttpClient не являются разрешённой подменой streaming adapter. Выбранные configurable limits перечислены в §5.5; future H2/H3 support требует собственной проверки.

### 5.5. Composition root и lifecycle

.NET consumer собирает ContentManagerClient + HttpClientTransport + выбранный IClientStateStore + clock/logger/bindings. Unity composition создаёт UnityWebRequestTransport + Unity dispatcher/logger, подключает SqliteClientStateStore и передаёт их одному DotNet ContentManagerClient. UnityContentManagerClient делегирует sync/control/read/raw методы и добавляет Unity-only image helper; resource types не просачиваются в DotNet.

| Шаг | Поток / владелец | Результат |
| --- | --- | --- |
| Проверить options/source-partition/binding registry | Composition | Immutable configuration, no assembly scan/latest; numeric caps validated |
| Open store/format/native checks | Adapter worker | Ready scope либо explicit initialization failure, no memory fallback |
| Создать protocol/services/coordinator | DotNet | One shared transport/store/clock/logger instance per composed client scope |
| Запустить public operation | Unity facade → worker/DotNet | Async flow без SQL/hash/large parse на main thread |
| Показать progress/result/image | Unity dispatcher | Callback текущего presentation context; native decode/Destroy только здесь |
| Shutdown | Owning root | Stop/cancel, resolve begun commit, drain own requests/resources; потом dispose own adapters/runner |

DI container не обязателен: explicit constructor/factory composition достаточно. DotNet не service-locates Http/Unity/SQLite и не создаёт HTTP connection из store. Injected external dependencies остаются caller-owned; factory-created dependencies явно owned composition. Runtime GameObject/dispatcher pump — инфраструктура Unity client lifetime, не scene list/second inventory. Она должна пережить cleanup своих operations, не исчезнуть раньше callbacks. Task/ConfigureAwait(false) сами не гарантируют off-main-thread работу; Unity work runner и SQLite adapter явно планируют blocking/CPU work на поддержанном worker.


### Выбранные implementation defaults

Это настраиваемый и startup-validated клиентский профиль; он не изменяет серверные F13 settings и не доказывает sizing для всех нагрузок.

| Настройка | Выбранное значение |
| --- | --- |
| Control/error JSON и один NDJSON record | 8 MiB каждый |
| Unity bounded queue / callback block | 4 MiB / 64 KiB |
| MaxPageSize | 256 |
| StartResumeTimeout | 90 s |
| UnaryTimeout / HeaderTimeout / ReadIdleTimeout | 30 s каждый |
| RecordTimeout | 60 s |
| StorageTimeout, включая validation/atomic commit | 120 s |
| ReceiptTimeout | 30 s |
| AssetTransferTimeout | 120 s; finite whole-transfer budget, отдельный от read-idle |
| Transient retries | Максимум 3, exponential 0.5–5 s с jitter; только safe boundary |
| Image encoded cap / MaxSide / MaxPixels | 20 MiB / 8192 / 16777216 |
| Image DecodeTimeout | 30 s; observation budget, не физическое прерывание native decoder |

Validation/hash/refusal ошибки не auto-retry. Expected local pauses приостанавливают только sync read-idle; stage/server deadlines сохраняются. Image cap не является пределом обычного raw stream. Все значения доступны через validated SdkRuntimeOptions/UnityImageOptions; decode timeout закрывает success handoff и освобождает late output после окончания synchronous native decode.

## 6. Данные и владение состоянием


### 6.1. Состояние


| Данные / состояние | Источник истины | Клиентский владелец и область жизни | Обновление / сброс |
| --- | --- | --- | --- |
| Полный exact registry selections | Явный выбор приложения/разработчика | Клиентская сторона данного managed набора | Получается целиком перед sync; допускает несколько версий одной Schema |
| Installed данные и inventory | Подтверждённый atomic local commit | Клиентский state adapter | Вместе с данными, removals и read index группы, не после download |
| Исходный контекст/fingerprint и immutable plan identity | Исходный вызов и план CM | Durable context одной synchronization | Собственные commits не подменяют исходный запрос; новый plan требует нового ID |
| ID, проверенные records, application checkpoints и local receipts/server ACK outcome | DotNet и фактически сохранённое состояние | Устойчивый клиентский adapter | Сохраняется после process exit; cursor отдельно от receipt; Discard очищает unfinished, не installed |
| Execution/gate, cancellation и progress | Текущая SDK-операция | DotNet для stable store scope между SDK instances, local run generation | Второй вызов отклонён до нового ID; late callbacks/parser/control outcomes fenced, actual cross-process adapter proof Q29/Q32 |
| Freshness | Отдельная проверка CM после полного успеха | SDK result, затем состояние приложения | Актуально/изменения на момент сравнения; сбой = unknown |
| Raw file / Unity resource / optional cache | Самостоятельная asset-загрузка | Владелец stream и Unity/platform lifecycle | Не участвует в commit data group; release/cleanup Q29/F23 |


Приложение задаёт полный selection в SynchronizeAsync; SDK сохраняет original selection в durable attempt, actual inventory выводит из committed store. Binding registry регистрируется отдельно и не выбирает группы. SDK предоставляет IClientStateStore и готовый replaceable Demo adapter; SQLite — первый готовый кандидат после native/AOT/platform spike. Stage+checkpoint и activation+index+inventory+receipt — отдельные атомарные операции (§6.4), in-memory не обеспечивает restart recovery.


CM plan хранится в bounded process memory одного host с настраиваемым absolute default TTL 1 час от reservation (CreatedAt), без продления requests/retry/lease. Restart теряет plan; SDK создаёт новый от actual inventory. Lease generation fencing защищает execution ownership. Это не TTL installed/staging SDK и не обещание persistence plan постоянной БД контента.


### 6.2. Логические клиентские модели


§6.2 определяет поля SDK public/result/recovery models; §5.1 — signatures, §6.4 — store operations. HTTP DTO принадлежат backend F13. Выбранные physical packages/provider/native versions и numeric defaults описаны в §5.5/§11.1; literal behavior fixtures и generic results не заменяют ещё не пройденный Unity/platform/release conformance.

По [Q19 canonical profile v1](../../backend/questions/Q19-content-hashes.md#принятый-canonical-profile-v1) integrity identity — **profile + SHA-256 digest**, не bare hex. SHA-256 вычисляется по exact canonical UTF-8 bytes, digest —64 lowercase hex без prefix. Immutable bytes/digest/profile сохраняются вместе; SDK/store используют exposed integrity metadata, не выбирают profile UI/API параметром. Изменение правила, меняющего bytes old input, требует нового profile; historical bytes не rehash-ятся. Neutral typed inputs/common pure .NET library определены один раз [backend F03](../../backend/features/F03-publish-schema-version.md); frontend не копирует DTO.

Фиксированные Demo/historical profiles: cm-schema-content/1, cm-instance-content/1, cm-publication-manifest/1, cm-target-group/1, cm-installed-inventory/1, cm-sync-plan/1. Schema — semantic contract без authoring labels; instance — values/frozen media descriptor без instance identity/Key/version/orphans/ItemKey. Manifest — whole runtime membership/exact schema-instance IDs+versions/frozen aliases/profile+digests; provenance/override target/exclusions/release IDs/actor/comment/time/RevisionId не content bytes. Group — full target после upserts **и removals**, exact schema context/members sorted InstanceId с exact version/Key/profile+digest. Inventory — actual installed representations. cm-sync-plan/1 имеет fixed kind=request (original selection+initial inventory profile/fingerprint) и kind=plan (original request fingerprint/profile+pinned groups/removals), без random PlanId/time/progress/receipts. Roles не user selection/новый седьмой profile. Full input shapes/exclusions/order backend F03/F10/F13/Q19; Registry cm-json-schema/1 и C# cm-csharp/1 отдельны и сохраняются для old exact versions. Новый MVP set совместно включает Q35 constraints и Q34 Offset: новые schema-content, instance-content, JSON Schema и C# profiles. Root group/inventory/manifest profiles не требуют смены, если собственные layout/semantics/codec прежние и existing content-profile identifiers выражают новое вложенное содержимое. Exact whole new-profile codecs/extension carriers/literal golden bytes — engineering Q19/Q31, название `/2` не означает готовой реализации.

Canonical UTF-8 без BOM/formatting spaces/final newline; NDJSON LF — framing, не content hash. Strict parsing rejects duplicate members, malformed Unicode/trailing garbage и unknown profile до group apply. Strings не trim/case/NFC-normalize; literal UTF-8 с selected JSON quote/backslash/control escaping, без HTML/slash escaping. Numeric kind приходит из typed exact metadata: Long exact/no double/no exponent; Decimal exact/no rounding/trailing-zero stripping/no exponent; finite IEEE64 Float только RFC8785 numeric section/Appendix B (-0→0), не весь JCS. Absent/null/default distinct; type-invalid null/missing required rejected без default-fill. Fixed member order/lower-D FieldId/Ordinal keys задаёт profile, Lists сохраняют order; Demo Asc/Desc не выбирается вместо Q35. No untyped JSON number guessing/new runtime Registry fetch ради verification.


DateTime parsing выбирается **по exact supported content profile и generated binding**, без угадывания по строке/типу latest. По [Q34 DateTime MVP](../../../business/questions/Q34-schema-field-semantics.md#datetime-mvp-temporal-semantics-и-совместимость) old Naive `/1` десериализуется в DateTime Kind=Unspecified без Z/offset; новый Offset — в DateTimeOffset из canonical UTC `yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'` с ровно семью fractional digits (100ns). В generated client project могут одновременно использоваться old/new exact versions и типы; original offset принадлежит authoring snapshot и SDK runtime его не восстанавливает. Explicit pure parse/format codec принадлежит [backend F04](../../backend/features/F04-entity-instances.md), model — [backend F01 §2.1](../../backend/features/F01-create-entity-schema.md#21-aggregates-entities-и-value-objects); общий neutral CLR contract не импортирует Domain/EF в SDK. No host local timezone, implicit DateTime conversion, JavaScript Date или floating-point loss of ticks. Offset equality/hash/ordering используют UTC instant без offset secondary key: equivalent instants дают те же runtime bytes/hash; SDK не переставляет List. Unknown temporal/content profile, invalid precision/range или mismatched exact binding дают validation/protocol failure **до group apply**, сохраняя old installed group/index/inventory. Layers прежние: DotNet owns typed parsing/integrity/staging/apply, Http — transport, Unity — callbacks/lifecycle; SDK не выполняет authoring migration или generation на sync.

Client models below belong Sdk.Client.DotNet; they are target public/adapter contracts, not proof of existing source. GUID identities use Guid, key/profile/digest representations use string with F13/Q19 validation; ExactSchemaSelection means neutral SDK value mapped to/from owner F13 pair contract, not imported server assembly; no server Domain/EF dependency and no duplicate wire DTO definitions. Container snapshots are immutable; generated T may have setters but is detached.

**StoreScopeMetadata — durable adapter identity**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SourceIdentity | `string` | Да | Explicit stable configured CM source binding | Application/configuration-owned, no invented server identity endpoint; changed configured binding refuses existing store, no silent normalization/rebinding |
| ApplicationPartition | `string` | Да | Application-owned managed dataset partition | Explicit stable nonempty identity; one scope per store |
| StoreFormatVersion | `int` | Да | Local persistence format version | Separate SDK/protocol/profile; supported atomic migrations only |

**SynchronizationAttemptHandle — opaque public SDK value**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ScopeBinding | Private `string` | Да | Owning source/partition/store context | Caller cannot construct/edit; no secret or server auth |
| SynchronizationId | Private `Guid` | Да | Captured durable attempt | Created after scope gate; no caller chosen sync ID |
| ClientIncarnationId | Private `Guid` | Да | Owning SDK incarnation | Opaque, restart does not reuse old callback context |
| LocalRunGeneration | Private `long` | Да | Captured local execution fence | Monotonic checked, old handle not retargeted; state read obtains current handle |
| ServerControlBinding | Private `(Guid PlanId, long ExecutionGeneration)?` | Условно | Exact Q33 control precondition | No generation selection exposed to application; unbound/unknown cannot fabricate control ACK or silently retarget |

**SynchronizationState — public observed SDK state**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Attempt | `SynchronizationAttemptHandle?` | Условно | Current observed unfinished/active attempt | Absent if none; after restart capture fresh handle, no recovered old callbacks |
| HasUnfinishedAttempt | `bool?` | Условно | Observed durable continuation | true/false only successful local observation; null with Error on unreadable state, not inferred from remote failure |
| IsRunning | `bool` | Да | Current local high-level execution | Does not imply server preparation/Data worker live |
| PlanAvailable | `bool` | Да | Ready immutable plan observed | Preparing may have identity without ready metadata |
| Error | `SdkError?` | Условно | Expected state lookup failure | Error does not prove no attempt/Missing; no silent Resume or new ID |
| CommittedGroups | `IReadOnlyList<LocalGroupCommitReceipt>` | Да | Durably committed groups of observed attempt | Non-null; empty allowed; no staging counted as committed |

**SyncResult — public high-level call result**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Outcome | `SyncOutcome` | Да | Completed/Stopped/Failed/AlreadyRunning | Full terminal/all commit ACK/correct HTTP end for Completed; no false success from EOF |
| CommittedGroups | `IReadOnlyList<LocalGroupCommitReceipt>` | Да | Actual reconciled committed results of attempt | Non-null []; other group failure does not roll back |
| UnfinishedAttempt | `SynchronizationAttemptHandle?` | Условно | Retained continuation | Present when recoverable unfinished context remains; second rejected call creates none |
| Error | `SdkError?` | Условно | Expected operational failure | Required Failed; cancellation returns Stopped and known committed outcome |
| Freshness | `SyncFreshness` | Да | Independent freshness result | Failed check after full sync = Completed + Unknown; otherwise NotChecked |

**SyncFreshness — public SDK comparison result**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Outcome | `SyncFreshnessOutcome` | Да | NotChecked/UpToDate/ChangesAvailable/Unknown | Maps F13 outcome; NotChecked/Unknown SDK-only, not HTTP success |
| CheckedAt | `DateTimeOffset?` | Условно | Successful server comparison UTC | Present UpToDate/ChangesAvailable; not fabricated after failure |
| ChangedGroups | `IReadOnlyList<ExactSchemaSelection>` | Да | Exact changed pairs from successful F13 comparison | Non-null []; no failed comparison treated as no changes |
| Error | `SdkError?` | Условно | Comparison failure information | Unknown preserves successful sync result |

**SyncProgress — immutable notification snapshot**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Stage | SDK stage discriminator | Да | Preparation/recovery/download/validation/commit/receipt/freshness stage | Semantic stage, not byte percentage; exact literal stage codes engineering mapping |
| Attempt | `SynchronizationAttemptHandle?` | Условно | Captured attempt control context | May be absent before attempt reservation; stale callbacks fenced |
| Group | `ExactSchemaSelection?` | Условно | Current exact group | No implicit latest or merging versions |
| ProcessedCount / TotalCount | `long` / `long?` | Да / Условно | Stage counts | Nonnegative; checked Int64; total only known |
| ProcessedBytes / TotalBytes | `long?` | Условно | Observed transfer counters | Optional known sizes, network bytes not durable installed progress |
| Percent | `double?` | Условно | Derived display percentage | Only known nonzero total; no false global percentage across stages |

**SdkError — safe expected failure**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Category | SDK error category | Да | Transport/protocol/integrity/storage/configuration/control classification | Literal stable codes and HTTP reason mapping owned Q15/Q29; expected failures returned, no fake NotFound |
| Code | `string` | Да | Stable SDK diagnostic code | Safe machine value; text not contract; registry materializes before integration |
| Message | `string` | Да | Safe human detail | No credentials/secret URL/raw canonical payload |
| Group | `ExactSchemaSelection?` | Условно | Affected exact group | Absent when failure scope whole attempt |

**CommittedEntity<T> — detached typed local result**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Value | `T` | Да | Separate deserialized generated exact root | Caller mutation has no persisted effect; no editable tracked entity |
| SchemaId / SchemaVersionId | `Guid` | Да | Exact committed structure | Must match registered root T binding |
| EntityInstanceId / EntityInstanceVersionId | `Guid` | Да | Exact installed identity/version | No latest lookup; multiple structures coexist |
| Key | `string` | Да | Stable per-schema alias | Ordinal exact key semantics; lookup group from T |
| ContentHash / ContentHashProfile | `string` | Да | Stored canonical content integrity | 64lowerhex digest + supported ordinal profile; no reserialize/rebuild |

**LookupResult<TValue> — local read result**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Outcome | `LookupOutcome` | Да | Found/NotFound/Failed/SnapshotChanged | NotFound for missing committed record; SnapshotChanged for invalidated page continuation |
| Value | `TValue?` | Условно | Detached committed value/page | Required Found, absent on failure/notfound |
| Error | `SdkError?` | Условно | Storage/binding/parse/page conflict evidence | Failure never successful empty record/page |

**CommittedPage<T> — one committed group page**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Items | `IReadOnlyList<CommittedEntity<T>>` | Да | Detached typed page records | Non-null; empty page may be valid successful result |
| GroupHash / GroupHashProfile | `string` | Да | Observed exact group snapshot binding | cm-target-group/1, independent content hashes |
| Continuation | `string?` | Условно | Opaque local page continuation | Bound scope/exact group/hash/profile and traversal position; changed group reports SnapshotChanged; encoding/order/size guardrails engineering |

**ControlResult — public Stop/Discard outcome**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Acknowledged | `bool?` | Условно | Observed requested control outcome | true only known ACK; unknown network outcome null; no installation proof |
| State | `SynchronizationState?` | Условно | Observed state bound to action | Original replay snapshot not fresh Status/newer run; do not overwrite current context |
| Error | `SdkError?` | Условно | Stale handle/remote/storage/unknown outcome | No fresh-generation retarget; unknown local commit defers Discard cleanup |

**LocalGroupCommitReceipt — durable local atomic fact**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SynchronizationId / PlanId | `Guid` | Да | Original attempt and immutable plan | Together with group and target evidence idempotency identity |
| SchemaId / SchemaVersionId | `Guid` | Да | Exact committed group | Includes deselected removal-only group |
| TargetGroupHash / TargetGroupHashProfile | `string` | Да | Complete installed group target | Compare actual active membership even empty; 64lowerhex + supported profile |
| ServerAcknowledged | `bool` | Да | Recorded server receipt outcome | Atomic local commit initially false; separate durable ACK update; local receipt proves local transaction, server receipt not filesystem proof |

**DurableSynchronizationAttempt — private adapter recovery context**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SynchronizationId | `Guid` | Да | Persisted SDK operation ID | Allocated under scope gate and saved before Start |
| OriginalSelection | `IReadOnlyList<ExactSchemaSelection>` | Да | Original full selection snapshot | Distinct binding registry; explicit[] valid |
| OriginalRequestFingerprint / OriginalRequestProfile | `string` | Да | Frozen request variant from initial inventory | cm-sync-plan/1; own commits never change |
| PlanId / PlanMetadata | Retained exact F13 plan context | Условно | Reserved identity/ready immutable metadata | PlanId can exist before ready plan; no second editable HTTP DTO owner |
| Checkpoint / StagedRecords | Opaque cursor + normalized verified staged record storage | Условно | Validated records and continuation | Record+candidate cursor atomic; content exact bytes; mid-group header retained |
| Receipts | `IReadOnlyList<LocalGroupCommitReceipt>` | Да | Local committed outcomes/pending server ACK | Reconcile before Data; preserve after Discard; staging not actual inventory |



Stable identity AssetId GUID. Runtime AssetReference использует selected five-field frozen descriptor, определённый один раз [backend F04](../../backend/features/F04-entity-instances.md): только agreed identity/kind/content type/size/binary SHA-256. Publish captures confirmed MediaLibrary facts в immutable instance bytes/hash/profile, Delivery не live-enrich-ит, SDK не rebuild-ит из current catalog. FileName/format/dimensions/duration/URL не runtime descriptor; UI/catalog metadata F05 остаётся отдельной transient read model. URL строится из configuration+AssetId и не values hash. Exact CLR/helper signatures Q29/Q31/Q32; AssetId не storage/Addressables key, topology/credentials не public models.


Минимальная atomic единица — полный target exact SchemaVersionId в schema context. [Public wire contract v1](../../backend/questions/Q33-sync-resume-transport.md#public-wire-contract-v1) задаёт camelCase, protocolVersion=1 и normal recordSequence 1..N immutable plan. SDK получает только изменённые группы, в canonical schemaId/schemaVersionId порядке, без interleave; deselected группа имеет пустой target. Start возвращает compact metadata JSON без main payload/полного массива IDs; groups:[] означает no-change, Data содержит только operationCompleted, group receipts не нужны. Exact envelope, payloads и модели определены один раз [backend F13](../../backend/features/F13-content-delivery.md#33-dto-и-parameter-models); SDK не создаёт второй DTO-contract.


entityUpsert.payload.contentJson — JSON **string**, содержащая исходный canonical JSON. SDK снимает JSON escaping, строго восстанавливает UTF-8 bytes без BOM и изменения текста, проверяет supported profile и SHA-256 этих exact bytes, затем выполняет typed parsing по exact generated binding. Нельзя parse→reserialize, canonicalize заново, преобразовать Long/Decimal через double либо считать внешнее escaping/LF частью content digest. Малформированный Unicode, duplicate members и unknown profile отклоняются до apply.

Полный LF-delimited record проходит envelope/identity/payload/integrity validation. Cursor из каждого normal record — только checkpoint candidate: SDK атомарно сохраняет validated full record и cursor в durable staging; parsed-but-not-saved record не продвигает checkpoint. Cursor opaque для SDK, server проверяет HMAC и привязку к synchronizationId/planId/afterSequence; он не даёт authorization либо доказательство установки. Resume продолжает original sequences после cursor. Replay дедуплицируется по identity и полному parsed payload, включая восстановленные content bytes; другой payload с той же sequence — protocol error. Для mid-group resume сохраняются group header/metadata/staging, новый groupStarted не придумывается.

После groupCompleted SDK сверяет immutable plan и groupStarted, actual Add/Update/Remove counts, baseGroupHash/profile с исходной local group и **полный** resulting membership после upserts/removals, включая неизменённых членов, exact schema context/member versions/Keys/profile+digests и targetGroupHash/profile. Counts сами не подтверждают target. Только затем atomic data/removals/index/inventory/local receipt commit. Missing removal/member, unknown profile/corruption сохраняют old group; legitimate empty target отличается от failure. Inventory обновляется только успешным commit, partial outcome между целыми группами допустим.


После local commit SDK отправляет body planId/targetGroupHash/targetGroupHashProfile в commit route группы schemaVersionId. Same receipt возвращает тот же ACK acknowledged:true, другой evidence даёт 409; server ждёт receipt перед next group. Lost ACK сверяется с durable local receipt/Status и повторяет receipt без local reapply. Status разделяет planState и executionState и возвращает accepted receipts: completed подтверждает все server receipts, а не installed store. Полный успех требует valid operationCompleted, всех local commits/server ACKs и корректного окончания HTTP response. operationInvalidated — terminal recordSequence=0 без cursor вне normal planned sequence; он не заменяет checkpoint, останавливает unfinished работу и сохраняет committed группы. Invalidation/truncation исключают full success. Next sync uses actual inventory; own commit не переписывает original request/plan fingerprint.


Отказ от пары в registry удаляет принадлежащие ей локальные managed representations, не серверный контент и не независимые данные приложения. Один `EntityInstanceId` может иметь несколько representations для разных schema-version. Typed lookup по stable `EntityInstanceKey` работает в generated/model schema context и обязан различать эти representations. Связь T с exact версией задаёт root `ContentSchema(SchemaId, SchemaVersionId, VersionNumber)` по Q31; lookup signatures и реализация чтения binding остаются Q29. `EntityInstance.Name` — authoring label, не обязательное runtime поле. Дополнительный generic dictionary-read как обязательный публичный режим не принят.


### 6.3. Generated models и конфигурация


Demo codegen: UI CM → API F22 → Registry F21 → reusable generator → ZIP `.g.cs` → исходники в Unity-проекте. Runtime десериализует уже доступные модели, не выводит их из экземпляров и не запускает генератор на каждой sync. Generated source не редактируется вручную как основной способ расширения. Правила 15 типов, required/deprecated, stable field identity, recursive lists, naming и serialization принадлежат F21/F22/Q31; здесь не создаётся второй профиль. Новое поле не создаёт его автоматическое отображение или игровую логику.


Реестр Demo явно задаёт разработчик. Последнее рабочее имя future декларации — `content-manager.json`, ранее в источнике `content-schemas.json`; exact формат/имя и связь tooling с runtime registry остаются Q31. Это один developer/build-time источник выбора, не два обязательных файла, не installed inventory, sync plan F13 или Addressables mapping manifest. Автоматическое применение декларации не является условием Demo; CLI/Editor/build workflows описываются F22 отдельно.


**Логическая конфигурация SDK.** Программный config и Unity adapter передают один согласованный профиль; exact public property names/types пока не выбраны.


| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Адрес CM | Уточняется — [Q29](../questions/Q29-unity-client-package.md) | Да | Адрес runtime сервиса от приложения | Не адрес package registry; source-name BaseUrl не фиксирует signature |
| Timeout policy | Уточняется — [Q29](../questions/Q29-unity-client-package.md) | Уточняется | Отдельные unary, response-header, read-idle, record-assembly, storage/validation и receipt budgets | Число 30 из источника не принято; expected local pause приостанавливает только read-idle, соответствующий stage deadline остаётся; timeout не доказывает expiry |
| Retry policy | Уточняется — [Q29](../questions/Q29-unity-client-package.md) | Да | Ограниченные transient retries с backoff/jitter, default и override потребителя | Safe boundary обязателен; числовые defaults и exact policy fields уточняются Q29/Q32; 409/timeout не разрешают fresh-ID fallback |
| Logging configuration | Уточняется — [Q29](../questions/Q29-unity-client-package.md) | Уточняется | Независимая SDK abstraction и Unity adapter | Не tokens/secret URL/full payload; серверный logging framework не навязывается |
| Client state access | `IClientStateStore` operation port §6.4 | Да | Source/partition-bound durable recovery/active inventory/stage/activation/read | Full selection public input; no arbitrary CRUD/HTTP DTO/SQL connection export, Inspector не единственная config boundary |


Environment/optional flags из источника не становятся обязательными полями Demo. Secrets и registry tokens не помещаются в ScriptableObject или исходники. Package versions, timeout defaults и source-пример `record struct` не объявляют выбранные значения/API языка для Unity 2022.3.62f3.


### 6.4. Store port и SQLite adapter

The replaceable IClientStateStore logical port owns data access; DotNet owns protocol validation/orchestration and full-target checks. Normalized stage/verified activation input mapped from F13 records is private SDK data, not HTTP DTO/Domain/SQL connection in adapter API.

| Operation | Input / result | Atomicity / constraints |
| --- | --- | --- |
| ReadScopeMetadata / OpenScope | Configured source identity + application partition -> StoreScopeMetadata | Validate existing binding/format before operations; mismatch refuses, no auto-delete/rebind |
| TryAcquireSynchronizationScope | Canonical store scope -> owned disposable gate or AlreadyRunning | Before new ID/Start; shared SDK instances/processes, no queue/join; release only own settled writer lifetime |
| ReadRecoveryContext / ReadInstalledInventory | Scope -> durable attempt/receipts and actual active membership | Coherent local read; inventory derived active representations, never staging/parallel authoritative copy |
| CreateAttempt | New SDK ID + normalized full selection/original fingerprint/profile | Save original input before network; one unfinished active context per scope |
| StageRecordAndCheckpoint | Exact normalized validated record + candidate cursor | One transaction, duplicate same sequence/meaning no effect; different meaning integrity failure; content exact bytes |
| ReadStagedGroup | Attempt + exact group -> complete ordered stage/header/boundary evidence | Preserve mid-group resume; no incomplete group visibility in active rows |
| ActivateGroup | Attempt/plan/group + expected base hash/profile + validated complete diff/target | Short transaction recheck basis; atomically data/removals/index/inventory/local receipt; identical existing result original receipt |
| ReadCommitOutcome | Receipt identity -> durable receipt + actual active group | Resolve unknown commit; contradiction integrity failure, never blind apply/cleanup |
| MarkServerReceiptAcknowledged | Exact local receipt + verified F13 ACK | Separate short durable update; no second apply; server ACK not local filesystem proof |
| RemoveUnfinishedAttempt | Exact scope/attempt after stop and known local commit outcome | Remove only unfinished context/staging; installed rows/inventory/committed receipts retained |
| ReadByKey / ReadById / ReadPage | Exact registered model group + query/page context -> committed bytes/metadata | Short coherent snapshot, detached typed mapper; page checks expected group hash/profile |

SQLite adapter uses one local database per managed scope; no long SQL transaction through network/receipt wait. Private mapping подробно §6.7: StoreMetadata, SynchronizationAttempts, AttemptSelections, StagedRecords, ActiveGroups, ActiveRepresentations, LocalGroupCommitReceipts. Stage sequence unique within attempt; representation exact schemaId/schemaVersionId/entityInstanceId; Key index group-scoped, receipt unique original attempt/plan/group/target. Hash/profile/canonical BLOB bytes stored together. Active group metadata and membership/index update atomically, including confirmed empty groups; actual inventory derived committed state, не staging/вторая caller-controlled копия. No raw assets/secret credentials in DB.

Activation first looks for identical local receipt and reconciles corresponding active group before returning old result; otherwise rechecks complete actual base group under write transaction and applies whole validated target. Failure leaves old active group. Unknown commit closes/settles own writer resources and reconciles durable receipt/active group before reapply or Discard. An absent receipt is not sufficient while old writer outcome remains unknown. Reconciliation applies current owning attempt context; historical receipt retained after legitimate later installs must not reactivate old data or be confused with a pending current commit. Stop may finish started atomic commit with pending server ACK; no network work in transaction.

High-level scope gate is distinct from SQLite transaction locks: in-process shared gate plus process-held OS lock associated with canonical store path (actual API/platform proof Q32), held during whole sync/recovery including network. Failure to acquire returns AlreadyRunning before new ID. Canonical path resolution must prevent aliases bypassing same-store gate; filesystem/provider/platform lock behavior validated. Crash releases OS ownership; lease timeout does not permit another writer while process still owns lock. Unsupported locking/native runtime means adapter not supported on that target, not weakened concurrency guarantee.

Provider/native library and journal/durability settings selected by pinned platform spike; filesystem/native crash tests must show required guarantees. Async methods may execute synchronously in Microsoft.Data.Sqlite: blocking storage/typed validation scheduled outside Unity main thread with bounded work, main-thread dispatcher handles callbacks separately. [Provider async limitations](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async). No assumption Task.Run/API name proves all Player/backend behavior.

StoreFormatVersion separate package SemVer/protocol/content profile. Atomic supported migration under exclusive scope; newer unsupported/corrupt format refuses and preserves files, no automatic reset. Current adapter materializes private SQLite schema/SQL and supported format1, selected native/WAL/FULL/worker/canonical-path process lock. Real file/reopen/process tests prove tested Windows x64 scope only; arbitrary migrations and actual Unity native/AOT/Player support remain separate gates.

### 6.5. Asset и texture models

Models defined once here; frozen descriptor fields remain [F04](../../backend/features/F04-entity-instances.md#33-dto-и-parameter-models). These are target SDK types, not HTTP DTOs/existing implementation. Counts/ranges checked Int64; raw bytes SHA-256 independent instance ContentHash/canonical profiles/S3 ETag.

**AssetReadRequest — captured neutral SDK input**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| AssetId | `Guid` | Да | CM-owned media identity | Nonempty lowercaseD route, no ObjectKey/source URL |
| ExpectedDescriptor | `RuntimeAssetReferenceDescriptor?` | Нет | Optional frozen five-field Published expectation | Same AssetId, valid kind/MIME/SizeBytes/Sha256Hash; no current catalog enrichment |
| Range | `AssetByteRange?` | Нет | Explicit one byte range | Absent full read; no multipart; DownloadToFile/Image require full |
| IfRangeETag | `string?` | Условно | Prior strong CM response validator | Required Range; valid strong ETag preserved opaque, no W/weak/hash substitution; no unsolicited conditional headers |

**AssetByteRange — SDK single forward range**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Start | `long` | Да | First requested byte offset | Nonnegative; no suffix range |
| Length | `long?` | Нет | Requested count/open-ended remainder | Positive if specified; checked end computation/no overflow; one range only |

**AssetContentRange — parsed response range**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Start / EndInclusive | `long` | Да | Actual returned segment bounds | Nonnegative ordered; match requested legal interval |
| TotalSizeBytes | `long?` | Нет | Representation total length if known | Unknown * not fabricated; compare expected full SizeBytes when present; segment length checked End-Start+1 |

**AssetReadMetadata — immutable transport observation**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| AssetId | `Guid` | Да | Captured requested CM identity | From request, not new server header/asset descriptor |
| ContentType | `string?` | Нет | Response MIME when supplied | Valid media type compatible expected ContentType; no FileName guess |
| ContentLength | `long?` | Нет | Response body length when known | Nonnegative Int64; Range length is segment, not full asset size |
| ETag | `string?` | Нет | CM content validator when supplied | Strong value for If-Range; not hash-auth credential or metadata Version |
| LastModified | `DateTimeOffset?` | Нет | Observed confirmed upload-time header | Optional HTTP precision, not frozen runtime member |
| AcceptRanges | `string?` | Нет | Advertised support | Header presence not actual Range conformance proof |
| ContentRange | `AssetContentRange?` | Условно | Validated206 segment context | Required accepted206, absent full200/HEAD |
| StatusCode | `int` | Да | Observed HTTP status | No raw provider/private headers exposed |

**AssetOpenResult — headers/open boundary**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Outcome | `AssetOpenOutcome` | Да | Opened/Cancelled/Failed/RangeNotHonored | Opened not full download; no error body as Stream success |
| Handle | `AssetReadHandle?` | Условно | Owned accepted stream | Present only Opened; SDK cleans rejected response |
| Error | `SdkError?` | Условно | Expected typed failure | RangeNotHonored before any destination writes; safe bounded diagnostics |

**AssetReadHandle — IDisposable raw owner**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Stream | `Stream` | Да | Read-only counting/verifying owned stream | One serialized consumer; no seek or exposing underlying response bypass |
| Metadata | `AssetReadMetadata` | Да | Accepted response headers | Frozen observation; no catalog enrichment |
| Completion | `Task<AssetTransferResult>` | Да | Single terminal transfer outcome | Driven by caller reads/EOF/error/dispose, not background full buffering; expected failure result, never opened=completed |
| IsDisposed | `bool` | Да | Logical owner lifetime | Dispose once closes/cancels stream/response; premature dispose Cancelled/incomplete, never Verified |

**AssetTransferResult — terminal bytes/file outcome**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Outcome | `AssetTransferOutcome` | Да | Completed/Cancelled/Failed/RangeNotHonored | Completed accepted transfer scope incl range, not full asset unless full request |
| Verification | `AssetVerificationState` | Да | NotRequested/Pending/Verified/PartialRange/Failed | Verified only full expected size/SHA; partial range not full hash proof; interrupted incomplete Pending if no integrity conclusion |
| TransferredBytes | `long` | Да | Actually read body bytes | Nonnegative; correct HTTP end and declared length checks, no overflow |
| WrittenBytes | `long?` | Условно | Actually written destination bytes | Download/file workflows only; failed/cancelled destination may contain partial bytes |
| ActualSha256 | `string?` | Условно | Computed complete physical byte digest | 64lowerhex if full hash computed; not partial digest presented as whole |
| Metadata | `AssetReadMetadata?` | Условно | Observed response | Absent if no headers accepted |
| CommittedFilePath | `string?` | Условно | Final file successfully moved | Only successful full DownloadToFile; temp path not committed result |
| Error | `SdkError?` | Условно | Safe failure classification | Transport/access/notfound/range/integrity/storage/decode/cancel distinct; no secrets/full untrusted body |

**AssetMetadataResult — HEAD outcome**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Outcome | `AssetMetadataOutcome` | Да | Completed/Cancelled/Failed | Completed HEAD not actual body verification |
| Metadata | `AssetReadMetadata?` | Условно | Observed optional headers | Required Completed; missing length remains null |
| Error | `SdkError?` | Условно | Typed HEAD failure | No synthetic metadata/false file success |

**AssetProgress — immutable transfer/decode notification**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| AssetId | `Guid` | Да | Captured operation resource | F20 binds own presentation context as well |
| Stage | `AssetStage` (target enum) | Да | Headers/transfer/verification/decode/handoff | Headers/Transfer/Verification/Decode/Handoff SDK stages, not server wire; callbacks not completion/installation proof |
| TransferredBytes / TotalBytes | `long` / `long?` | Да / Условно | Observed byte counts | Nonnegative; total only known/validated, range segment context |
| Percent | `double?` | Условно | Derived display value | Only known nonzero total; no fake decode/GPU percentage |

**UnityTextureResult — Unity-only resource handoff**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Outcome | `UnityTextureOutcome` | Да | Ready/Cancelled/Failed | Ready only verified full descriptor+supported JPEG/PNG+successful decode+handoff |
| Handle | `UnityTextureHandle?` | Условно | Application-owned ready resource | Standalone Demo Ready only; no SDK shared cache/ref-counted dedup; future file cache §6.8 is separate |
| Transfer | `AssetTransferResult?` | Условно | Independent verified raw outcome | Raw Completed not automatically decoded Ready; does not change SyncResult |
| Error | `SdkError?` | Условно | Typed format/limits/integrity/decode/lifecycle failure | Cancelled before handoff cleans owned texture; unexpected defects/misuse exceptions per §5.1 |

**UnityTextureHandle — Unity-only IDisposable owner**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Texture | `Texture2D` | Да | Owned display texture | Access after Dispose ObjectDisposedException; consumer uses Dispose, not independent ownership via raw Destroy |
| IsDisposed | `bool` | Да | Atomic logical release state | Idempotent; main-thread Destroy scheduled once; not instant GPU-memory guarantee |

**AssetReadOptions / ImageReadOptions — client configuration**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| MaxCopyBufferBytes | `int` | Да | Bounded raw copy buffer | Positive; raw stream no full-file array; measured sizing |
| MaxErrorBodyBytes | `long` | Да | Bounded non-JSON diagnostics read | Positive, sanitized plain safe details; no sensitive headers/provider URL/body execution |
| HeaderTimeout / BodyReadIdleTimeout / TransferTimeout | `TimeSpan` | Да | Separate finite transport budgets | Startup/config validation; expected decode uses own stage budget, no physical interruption claim |
| MaxImageEncodedBytes | `long` | Да | Allowed complete image buffer | Positive/within addressable buffer range; validate actual bytes too |
| MaxImageWidth / MaxImageHeight | `int` | Да | Image header dimension caps | Positive; checked before large texture decode |
| MaxImageDecodedPixels | `long` | Да | Checked width*height cap | Positive Int64/no overflow; header parser bounded; actual decoded dimensions validated |
| DecodeTimeout | `TimeSpan` | Да | Decode/handoff observation budget | Positive; cancellation cannot interrupt already running synchronous native decoder; late output cleanup |
| DisplayProfile | Fixed Demo profile | Да | sRGB, no mipmaps, markNonReadable | Not runtime field/choice inferred from file metadata; CPU copy not retained after decode |

### 6.6. Платформенно-независимые transport и adapter models

Модели объявлены в DotNet один раз. Adapter не возвращает HttpResponseMessage/UnityWebRequest/SQLite connection. SDK request — внутренний technical value, не копия server command/Domain aggregate; protocol DTO/record shape принадлежит backend F13.

**HttpTransportRequest**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Method | `string` | Да | Метод уже выбранной F13 операции | GET/POST/HEAD по endpoint profile; transport не меняет семантику |
| Uri | `Uri` | Да | Полный URI из captured CM context | Configured origin/relative route, no arbitrary storage URL; redirects по policy |
| Headers | `IReadOnlyDictionary<string, string>` | Да | Headers этого запроса | Snapshot до send; credentials ephemeral/no logging; no transport-generated alternate context |
| Body | `byte[]?` | Условно | Уже encoded JSON bytes для POST | SDK-owned bounded immutable-by-contract bytes; no WWWForm/second serializer; transport не меняет caller bytes |
| Purpose | Logical `Unary / DataStream / AssetRead` | Да | Выбор transport resource/budget profile | Не server enum/new HTTP field; numeric caps validated |
| HeaderTimeout | `TimeSpan` | Да | Finite header-phase budget | Linked cancellation/deadline; not body/commit timeout |

**HttpTransportResponseHead**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| StatusCode | `int` | Да | Actual HTTP status | Known before exposing head; not inferred from generic Unity success |
| Headers | `IReadOnlyDictionary<string, IReadOnlyList<string>>` | Да | Snapshot response + content headers | Case-insensitive names, no extra fabricated values; critical combined/ambiguous headers reject by protocol validator |
| ContentLength | `long?` | Нет | Declared body length если корректно задана | Unknown remains null; checked parse/range; no preallocation of unlimited body |
| NegotiatedHttpVersion | `Version?` | Нет | Actual observed protocol когда platform API даёт его | Http uses response.Version; Unity no invented version from adapter name/config; actual negotiation proof Q32/Q33 |

**TransportCompletion**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Outcome | Logical `Completed / Canceled / Failed / DisposedBeforeEnd` | Да | Terminal body outcome | Один terminal result; exact literals/API freeze Q29 before release |
| BytesRead | `long` | Да | Body bytes действительно переданы reader | Non-negative/checked count; не canonical hash/group commit/downloadProgress |
| Error | `SdkError?` (§6.2) | Условно | Safe normalized failure | Expected failure/cancel/early disposal не fake EOF; no raw headers/keys/SQL/provider topology |

**StoredRepresentation** — neutral private port result SQLite/custom store → DotNet mapper, не второй public CommittedEntity<T>.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SchemaId | `Guid` | Да | Logical schema owner | Matching configured source/group |
| SchemaVersionId | `Guid` | Да | Exact structure version | Matching registered model/context, no latest |
| EntityInstanceId | `Guid` | Да | Logical instance owner | Composite representation identity; not list index |
| EntityInstanceVersionId | `Guid` | Да | Exact immutable instance version | Version identity preserved from F13 |
| Key | `string` | Да | Stable EntityInstanceKey | Ordinal exact equality, group-scoped uniqueness |
| VersionNumber | `int` | Да | Published instance number | Positive business number, not store format/version |
| ContentProfileId | `string` | Да | Stored canonical content profile | Known supported profile, SQLite does not select a newer profile |
| ContentHash | `string` | Да | Exact content digest under its profile | Format Q19/F13; not recomputed under a different serializer |
| CanonicalContentBytes | `byte[]` | Да | Exact persisted bytes, detached read buffer | BLOB→owned returned bytes; no pooled buffer invalidation, no parse/reserialize in store |

**SdkDiagnostic** — logger port value.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Severity | Logical diagnostic level | Да | Safe diagnostic importance | Exact public registry Q29; logging не меняет workflow result |
| Stage | `string` | Да | Logical operation stage | Не произвольный HTTP payload/error HTML |
| Message | `string` | Да | Human-readable sanitized text | No raw activation key/session token/body/storage URL |
| SynchronizationId | `Guid?` | Нет | Current sync correlation | Actual attempt ID, no invented actor/source |
| SchemaVersionId | `Guid?` | Нет | Exact group correlation | Only when applicable to diagnostic |
| AssetId | `Guid?` | Нет | Resource correlation | Only when applicable; no storage URL/key |
| Error | `SdkError?` (§6.2) | Нет | Already sanitized known error | Native exception/private diagnostics не public contract |

### 6.7. Конкретная реализация Store.Sqlite

SqliteClientStateStore implements IClientStateStore; file/provider/migration/native imports принадлежат Sdk.Client.Store.Sqlite. Один DB file на source/application scope. Portable DotNet определяет правила/проверенные inputs, SQLite adapter только выполняет own SQL и atomic operations. Не подключаются CM EF/DbContext или Newtonsoft-derived T mappings.

| Private table / component | Key / indexes | Содержимое и mapping | Read/write owner |
| --- | --- | --- | --- |
| StoreMetadata | Single scope metadata, own format version | Source/partition/canonical scope и поддержанный store format | OpenScope/StoreFormatMigrator; mismatch refuses |
| SynchronizationAttempts | Unique unfinished attempt per scope, exact synchronizationId | Original input/profile/fingerprint, server plan/control/recovery context | Create/read/reconcile/stop/discard SDK operations |
| AttemptSelections | Attempt + exact SchemaId/SchemaVersionId | Полный original selection отдельно от registered CLR bindings | CreateAttempt, durable recovery |
| StagedRecords | Unique attempt + record sequence; index attempt/group/sequence | Exact validated normalized record/content bytes и resume evidence | StageRecordWriter, ordered ReadStagedGroup |
| ActiveGroups | Exact SchemaId/SchemaVersionId | Committed group header/hash/profile/count, включая доказанный empty group | GroupActivationWriter; actual inventory alongside active membership |
| ActiveRepresentations | Group + EntityInstanceId; unique group + Key | StoredRepresentation §6.6, exact canonical BLOB/profile/hash/IDs/number | Activation/CommittedContentQueries; staging не участвует |
| LocalGroupCommitReceipts | Unique original attempt/plan/exact group/target evidence | Known atomic activation result и separate server-ACK state | Activation, CommitOutcomeReader, acknowledgement update |
| StoreScopeLock | Canonical DB/scope OS ownership | Stable cross-client/cross-process active workflow ownership | SDK run/recovery; native/process proof required |

ActiveGroups — metadata того же installed snapshot, не второй independent inventory или requested selection. Inventory строится из committed groups/actual representations; empty group допускается только с committed empty snapshot evidence. Header и membership согласованы в одной activation transaction, повреждённое/противоречивое состояние — failure.

GUID columns сохраняются losslessly, выбранный storage codec фиксируется store-format fixtures; keys сравниваются ordinal/case-sensitive без NOCASE/culture normalization. Content хранится BLOB, dynamic Integer/Decimal/DateTime не раскладываются через SQLite affinity/double в отдельные value columns. Prepared SQL параметризован; consumer values не SQL fragments. Порт возвращает detached bytes; GeneratedModelCodec DotNet проверяет profile/integrity и создаёт T, adapter не reflection-deserializes arbitrary CLR model.

StageRecordAndCheckpoint открывает короткую transaction: проверить owning attempt/sequence, вставить полный validated record и продвинуть durable cursor/checkpoint вместе; commit before progress/next checkpoint. Partial network fragment не record/DB write. Same replay original meaning no-op, conflicting sequence failure.

ActivateGroup получает complete validated candidate из DotNet, под короткой write transaction повторно читает actual base/current owner и existing receipt. Применяет Add/Update/Remove, ActiveGroups/ActiveRepresentations/Key index и local receipt одним commit. После commit координатор отправляет server receipt отдельным HTTP request; DB transaction через сеть не удерживается. Unknown commit → close/settle own writer и ReadCommitOutcome, no blind second apply/discard. Historical receipt не resurrects old group после legitimate later install.

Native connection/command/transaction не используется конкурентно несколькими threads. Adapter scheduling обеспечивает bounded worker execution и short coherent read snapshots; synchronous SQLite API не блокирует Unity frame под видом Task. WAL/journal/synchronous/busy settings/provider versions — pinned native/platform result Q32, не случайные defaults. Cross-process high-level ownership не подменяется SQL transaction lock. Stop не ждёт за whole-sync gate, нужен отдельный короткий coordinator/control path.

Schema/format migration отдельна от content/protocol profiles: initialize validates metadata, supported upgrade атомарен, newer unknown format refuses, no auto-delete/recreate. Native library load failure — initialization failure, no in-memory fallback. Gate release/connection disposal/own checkpoint resources выполняются при known settled outcome; tests используют собственные DB files/processes и не production/user data.


### 6.8. Общий файловый cache и launcher/app интеграция после Demo

В самостоятельном Demo остаются direct F13 sync и независимые raw/texture операции §5.2/§6.5/§7.4 без обязательного persistent media cache. Последующая интеграция добавляет общий DotNet download/cache coordinator: launcher — writer структурированных snapshots; приложение читает pinned `StoreScope + SnapshotId` и само запрашивает отсутствующие медиа через CM. Бинарные bytes не включаются в F13 records/receipts, job не привязывается к F13 PlanId/TTL. Работающий Player остаётся на старом snapshot при подготовке нового.

| Порт / компонент | Владелец | Вход / результат | Состояние и зависимости |
| --- | --- | --- | --- |
| IAssetDownloadManager / общий coordinator | DotNet | Scoped asset request → existing/new job handle, progress, typed result, verified file lease | Единый state machine, single-flight, Range/integrity/recovery и scheduler; Http/Unity реализуют transport |
| IAssetCacheIndex | Interface DotNet, реализация Store.Sqlite | Scoped identity/checkpoint/publication → durable metadata | Jobs/index/refs/ownership generation, короткие WAL-транзакции; не binary BLOB |
| IAssetBinaryStore | Interface DotNet, Windows filesystem adapter | Owned partial/file → verified immutable publication | Bounded file I/O, flush/checkpoint evidence, atomic publish и recovery; путь вне заменяемой папки build |
| IRuntimeDeliverySessionProvider | Interface DotNet, application/integration implementation | Exact permitted scope → действующая session либо typed refusal | Transport не знает происхождение credential; новый admission после expiry, protocol — Q37 |
| Unity platform conversion | Unity | Verified lease → Texture2D/Audio helper либо local-file abstraction | Main-thread create/decode/destroy; video читает файл, add-on не дублирует manager |

Порты, logical models и алгоритмическая декомпозиция ниже — предложенная инженерная реализация принятого общего механизма; имена интерфейсов — target aliases, не frozen компилируемые signatures. Exact manifest/API/cache codec, Windows paths/ACL, provider и cross-process locking protocol выбираются [Q37](../questions/Q37-launcher-asset-delivery.md) вместе с [Q32](../questions/Q32-sdk-package-distribution.md); netstandard2.1 не доказывает native/IL2CPP capabilities.

**Логический состав SDK-моделей; public CLR/codec/requiredness уточняются Q37.** Таблица задаёт обязанности полей, не утверждённый DDL или wire DTO.

| Поле / модель | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| AssetIdentity | AssetId + expected size/hash descriptor | Для job | Identity ожидаемых immutable bytes | Не только mutable path; mode/priority не меняют identity |
| StoreScope / SnapshotId | Scoped identities | Для потребителя | Installation/Variant и pinned snapshot | Проверка разрешённости отдельно от physical cache hit |
| ResolvedAssetRequirement | Immutable scoped asset requirement | Для present reference | Expected descriptor + effective mode | Предлагаемая производная exact Published definitions × final effective content; strongest-mode rule — Q37 |
| AssetDownloadJob.State | State enum | Для job | Planned/Downloading/Paused/AuthRequired/Verifying/Ready/Failed/Blocked | Имена иллюстративны; Ready только после полного size/SHA |
| AssetDownloadJob.ConfirmedBytes | long / 64-bit | Для resumable job | Durable offset | Не network callback count; не больше доказанно сохранённых bytes |
| AssetDownloadJob.Validator | Strong HTTP ETag и representation evidence | Для безопасного resume | If-Range binding | Нельзя выводить валидатор из предположения ETag = SHA |
| AssetDownloadJob.Generation | Fencing generation | Для shared worker ownership | Stale worker guard | Cross-process lease + generation проверяются перед публикацией |
| VerifiedAssetFileLease / CachedAssetFile | Owned local verified file abstraction | Для file consumer | Safe path/descriptor + release operation | Не byte[]/VideoClip; lease удерживается до завершения playback, не секретный gateway URL |
| Download result/progress | Typed outcome и 64-bit counters | Для high-level operation | Transfer отдельно от verification/Ready; Denied/DataNotReady/Transient/IntegrityFailure/DiskFull | Exact codes/fields Q37, cancellation одного waiter не удаляет нужную другим job |

Физически SQLite хранит entities/snapshots, asset requirements, index/jobs/checkpoints/ownership; 5–10 ГБ bytes — отдельные immutable filesystem objects и owned `.part`. Не создаются копии файла в SQLite BLOB, папке build и Addressables raw cache. WAL допускает одновременное чтение, но writer один; HTTP не удерживает SQL-транзакцию. Межпроцессный worker lock/lease с generation fencing и consumer file pins защищают от duplicate GET, stale publish и eviction во время чтения. Physical reuse не объединяет entitlement разных Variants. Crash/stale pin expiry и точная lock реализация — Q37.

Coordinator выполняет: scoped permission/expected descriptor → verified hit/lease либо join одной job → disk preflight → metadata/strong validator → Range от durable offset → bounded stream-to-file → durable checkpoint → full-file size/SHA → atomic filesystem publish → metadata commit. `206` требует проверки Content-Range/count/total; `200` на Range не append, а корректный restart; `416` не автоматически успех. Downloaded и Verifying не Ready. SQL/file publish не одна транзакция: crash между rename и commit разрешает reconciler, найденный файл проверяется до Ready. Memory не растёт с размером файла, counters long. Configurable chunks/parallelism/bandwidth/quota/GC/retention и shortage behavior выбираются по representative fixtures, не по source-числам.

`OnDemand` не создаёт автоматическую preload job; `Preload` запускается заранее/фоном без launch block; `RequiredBeforeLaunch` блокирует только выбранный snapshot до всех реально присутствующих required refs Ready. Предложено агрегировать один AssetId с несколькими modes внутри одной revision по strongest mode и одной job; правило агрегации и codec/binding подтверждаются Q37. Отсутствующая optional ссылка не создаёт job. Ошибка optional media не откатывает EntitiesReady. Новые remote HEAD/GET/Range проходят CM entitlement; expiry session требует нового admission, unknown/denied не продолжает bytes. Наличие файла не определяет offline/revocation policy.

UnityWebRequest adapter сохраняется; file-oriented DownloadHandlerFile/stream bridge должен доказать Range/append/durability/bounded native-memory поведение, а не просто наличие класса. Texture2D/Audio создаются только из checked bytes и имеют memory lifetime, отдельный от persistent file lease. Для большого MP4 consumer получает verified local file и передаёт путь VideoPlayer.url; не File.ReadAllBytes/Task<byte[]>/LoadAssetAsync<VideoClip>. Optional Addressables §F23 оборачивает те же manager/read ports.

Обязательная будущая приёмка: два реальных packaged Windows x64 IL2CPP процесса Unity 2022.3.62f3 на общем SQLite/filesystem; concurrent preload/on-demand с одной job, pinned old/new snapshots, 10 ГБ/bounded memory/long, restart/partial checkpoint, 200/206/416 mismatch, size/SHA corruption, rename/commit crash, disk full, stale fenced worker, playing-video lease/GC, expiry/readmission/denial. Используются actual provider + CM/PostgreSQL/YARP/MinIO и fault fixtures; Editor/doubles не доказывают эту совместимость. Эти проверки ещё не запускались.

## 7. Действия и клиентские потоки


### 7.1. Полный высокоуровневый вызов


| Шаг | Исполнитель | Действие | Гарантия / результат |
| --- | --- | --- | --- |
| 1 | Приложение / DotNet | SynchronizeAsync full selection; захватить stable source/partition/store gate до ID; inventory прочитать из active rows | Binding registry отдельный, все Demo selected versions supported; второй call AlreadyRunning; local run fencing |
| 2 | DotNet / state adapter | Найти durable unfinished context и сверить registry/store, план и сохранённые части | Пригодная попытка продолжается автоматически с прежним ID; запуск не зависит от обязательного окна |
| 3 | DotNet / F13 | Durable context/receipts → Status → при необходимости разрешить Resume → pending server receipt reconciliation без local reapply; новый Start только при доказанном основании | Capture planId/server executionGeneration; Resume передаёт original fingerprint/profile и не резервирует stream; preparing ещё без ready metadata допускает первое завершение подготовки |
| 4 | DotNet / transport F13 DF05 | Следующим GET открыть data response с planId/durable cursor и захватить X-CM-Sync-Execution-Generation | Data сам получает generation/lease; live competitor → 409; последовательные pinned группы, без SSE/control channel; прежний local run не принимает поздний ответ |
| 5 | DotNet / state adapter | Инкрементально разобрать envelope/full record; contentJson unescape→exact UTF-8/hash verification→typed parse; атомарно сохранить validated record+candidate cursor | Network fragments/parsed-unsaved record/неполный хвост не checkpoint; download не меняет installed; весь response не буферизуется |
| 6 | DotNet / state adapter | groupCompleted: сверить plan/groupStarted/counts, baseGroupHash/profile и full target after upserts+removals, включая unchanged members | Counts не заменяют membership; missing record/removal/profile/hash mismatch блокируют commit; no default-fill/numeric guess/rehash; empty target differs error |
| 7 | State adapter | Атомарно применить data/removals/index/inventory и сохранить durable local receipt | Нет половины новой группы; сбой не создаёт несогласованный installed/receipt |
| 8 | DotNet / F13 DF06 | Отдельно сообщить server group commit, выяснить потерянный ACK по local receipt/status и безопасно повторить; продолжить stream/следующие группы | Идемпотентный server repeat без двойного local apply; stream completion не заменяет receipts; честный partial outcome между группами |
| 9 | DotNet / F13 DF09 | После полного успеха, включая no-change, выполнить preflight actual installed/full selection | Установлено и свежесть различаются; старый plan не меняется |
| 10 | SDK / приложение | Передать результат и найденные изменения | Новая sync после успеха только по выбору приложения, с новым ID/plan |


Один SynchronizeAsync получает **полный selection** managed набора, независимо от registered bindings. SDK управляет IDs/Resume/plan/stream/apply/receipts/freshness. SyncResult сообщает Completed/Stopped/Failed/AlreadyRunning и actual committed groups; freshness independent, Completed+Unknown сохраняет успех. Expected failures типизированы; CancellationToken = Stop с resolved begun atomic commit, explicit Discard отдельно.


### 7.2. Resume, новый запуск, Stop и Discard


При следующем вызове, включая после закрытия процесса, DotNet сверяет selections, фактический installed, durable staging/checkpoint и доступность/совместимость immutable plan. Пригодный context продолжается по прежнему ID с application cursor последнего durable record. Network fragments не восстанавливаются как records. Reconnect на разрешённом Demo HTTP/1.1 либо переход между отдельно подтверждёнными HTTP versions сохраняет прежний ID/plan/durable checkpoint. Start, Status и Resume различимы; active duplicate того же ID даёт **409**, что не означает запрет любого повторного обращения к ID.


CM plan живёт в bounded process memory одного host; absolute TTL 1 час от atomic reservation по умолчанию не продлевается preparing/streaming/stopped execution, restart теряет plans. Новый ID/plan допустим при доказанном отсутствии/expiry/discard/invalidation старого либо explicit new selection intent; actual inventory авторитетен, committed группы сохранены. Reuse staged data разрешён только после exact identity/hash проверки нового plan. Timeout/разрыв/active409/unknown outcome не доказывают невозможность resume и не обходятся новым ID. Capacity failure нового Start не превращается в произвольное вытеснение active plan.


Локальная защита действует на стабильный scope хранилища между экземплярами SDK, а не только на facade instance. Второй sync отклоняется до генерации ID/Start без default очереди/join. Local run generation ограждает callbacks, parser/staging results и control outcomes прежнего запуска; начатый atomic commit выясняется по durable факту. SDK поставляет store interface и готовый Demo adapter; host может заменить adapter. SQLite первый кандидат после Unity native/AOT/Player/platform spike. Stage+checkpoint атомарны; activation+inventory+local receipt — отдельная atomic transaction. IClientStateStore operations/source-partition contract выбран §6.4; physical implementation и доказанный cross-process lock/recovery выбранного adapter остаются Q32.


SDK захватывает server executionGeneration из Status и Data header `X-CM-Sync-Execution-Generation`; generation не добавляется в NDJSON/cursor. Resume/Stop/Discard адресуют planId и **захваченную** expectedExecutionGeneration; Resume дополнительно передаёт originalRequestFingerprint/profile из durable context. Stale control получает 409: SDK не перечитывает fresh generation ради автоматического перенаправления старого намерения на новый worker. При lost control ACK повтор того же action/planId/expected generation/meaningful input возвращает original result snapshot; SDK согласует его с отдельным Status, не считает snapshot свежим состоянием и не повторяет local apply.

PlanId существует уже после reservation: Status preparing раскрывает его без готовых metadata. Stop сохраняет original input и прекращает preparing worker; Resume разрешает завершить **первую** подготовку, если ready plan ещё не существовал, но не пересчитывает ready immutable target. Resume проверяет identity/original request/eligibility и разрешает stopped/idle исполнение, live competitor даёт 409. Resume не открывает и не резервирует Data lease; следующий GET получает собственную generation/lease. Retained completed plan допускает terminal recovery: durable terminal при неизвестном HTTP end можно повторить/dedup terminal suffix.

Stop закрывает reading/transport и сохраняет context/checkpointed staging/installed группы; server revoke переводит execution в stopped. Начатый короткий local atomic commit может завершиться целиком: receipt сохраняется, POST откладывается до разрешённого Resume. Discard сначала останавливает execution и выясняет actual atomic outcome; unknown commit откладывает очистку. Затем удаляется только unfinished attempt/staging, сохраняются installed данные/inventory и committed receipts. Process crash recovery читает durable context/receipts → Status → pending server receipt repeats без local reapply → Data checkpoint; для stopped execution сначала требуется разрешённый Resume перед отложенным POST. Own commits не переписывают original fingerprint. Выход потребителя сам не означает Discard; discarded server tombstone исключает восстановление старой попытки в пределах retention.


Withdrawal/safety invalidation запрещает продолжать выдачу affected exact target старого plan. SDK не commit-ит неполную affected группу, сохраняет ранее committed группы, признаёт старый plan непригодным для продолжения affected group и создаёт новый от actual inventory по recovery policy. Замена pinned content на latest внутри старого ID запрещена.


Stop/Discard validate opaque handle against shared in-process scope coordinator and captured run, then signal the owning execution under short state guard; do not reacquire/wait the whole-run gate before sending Stop to its owner. Network and commit-outcome waits outside guard. Discard cleanup occurs under exclusive scope access only after owner writer/atomic outcome settled; ownership handed off explicitly, no window for competing new sync. Another process-held scope lock is typed busy/unknown ownership, never bypassed for cleanup; cross-process control IPC is not required by this adapter contract. State reads may observe committed data without acquiring a high-level sync gate, but unreadable recovery state returns Error/unknown, not no attempt.

### 7.3. Post-success актуальность


Preflight сравнивает actual installed с актуальным CM результатом **того же полного выбора**. Она может обнаружить Add/Update/Remove, а не только более новый VersionId; изменение не выбранной приложением schema-version не требует переключать модель. Client result различает успех установки и актуальность: актуально на момент сравнения / доступны изменения / проверить не удалось.


Freshness — отдельный POST `/api/v1/content-synchronizations/freshness` с полным selection и actual installedInventory, без plan/lease. SDK использует outcome `upToDate`/`changesAvailable`, UTC `checkedAt` и canonical sorted exact pairs `changedGroups` по единственной модели backend F13. Authority/transport failure даёт client Unknown, не «нет обновлений», не отменяет completed sync и не запускает следующий автоматически. Bounded transient retry с backoff/jitter применим только в safe boundary; validation/hash/lifecycle/confirmed-invalidation failures не auto-retry. После успеха новый sync выбирает приложение; новый plan при доказанно невозможном resume — отдельный recovery путь.


### 7.4. Отдельная загрузка assets и Unity lifecycle

**DotNet / Http / gateway.** AssetReadRequest captures configured CM source/read context and optional expected descriptor; no later catalog enrichment or changed session binding. Resolve canonical /storage/assets/{assetId}, GET (optional explicit Range/If-Range) or unconditional HEAD. Transport requests Accept-Encoding: identity and disables automatic redirects:3xx/default304 without local body cannot be returned as file. Demo Available-only gateway eligibility and MVP current session/revision/entitlement precede cache/headers/range/proxy; archived historical bytes do not grant access by retention alone; server storage auth remains private, CM bearer never forwarded as storage authority.

Before exposing Stream/writing destination, classify status and header contract. Full response200, requested range206 with valid single Content-Range/request matching/prior validator context. 200 for expected206 -> RangeNotHonored before any caller writes,416 typed range failure, other denied/notfound/provider non-JSON failures bounded/sanitized. No error body decoded as media. Reject Content-Encoding other than identity; validate available Content-Type against expected confirmed media type (not filename), full/range Content-Length against correct scope. Expected descriptor GUID/kind/MIME/size/SHA valid, no hash/auth substitution.

**Owned raw stream.** AssetReadHandle opens after accepted headers, owns HTTP response and read-only serialized-consumer counting/hash wrapper. Caller drives reads; no background whole-body buffering. Usage: consume through EOF, then await Completion, finally Dispose; await Completion without driving reads does not start download. Metadata/Completion remain observable after Dispose; disposed Stream cannot be read. Separate header/body read-idle/transfer cancellation budgets active; linked token/Dispose cancels remaining reads. Each read counts actual bytes, expected full SHA incremental. Correct HTTP end and declared response length required; full expected SizeBytes/Sha256Hash -> Verified; ID-only NotRequested; accepted partial range -> PartialRange, never full file hash. Overlong/truncated/mismatched bytes failure, not silently truncate/rehash/repair.

Bytes may be consumed before complete integrity verdict; caller cannot treat Opened/partial read as Verified. Completion resolves once after EOF/error/earlyDispose; framework ReadAsync may throw normal stream exception while Completion gives typed expected failure. Early disposal Cancelled/incomplete with actual counters, no false Verified. SDK closes rejected/failed open resources; caller owns successful handle until idempotent Dispose, which releases only that operation. Actual upstream abort and memory bounds require tests, not inferred from cancellation callback.

**Destination / file.** DownloadAsync maps read/write/protocol/integrity/cancel to AssetTransferResult and actual WrittenBytes, leaves caller stream open/current position, no hidden Seek/Truncate/retry after first write. Partial/unverified destination is application responsibility and not ready media. Before-write transient retry allowed; after-write arbitrary stream not automatically retried or appended. Output storage failure separate transport failure, previously committed data sync unchanged.

DownloadToFileAsync full request only; create own unique temp exclusively in final target directory. Stream/verify/close and finish file writes before no-overwrite move; validate target/platform move semantics, no prior exists-check as clobber protection. Default target existing -> typed file failure, old file untouched. Cancellation before move leaves no committed target and removes only own temp. Begun short final move resolves actual known outcome; successful move reported committed, no false canceled rollback/delete. Owned-temp retry starts clean before target commit, no persistent asset resume. Unknown filesystem outcome requires observation/reconciliation, never deleting arbitrary target or claiming Completed.

**Unity helper.** LoadTextureAsync requires frozen Image descriptor. Reuse full DotNet transfer/size/SHA check and limits; only JPEG/PNG for Demo. Bound encoded byte[] by configured cap and actual bytes, inspect bounded JPEG/PNG headers and match actual format to expected MIME, positive dimensions and checked width*height caps before large texture allocation. No backend upload-policy change or dimensions added to descriptor. Raw Audio/Video remain available, playback helpers/Addressables separate.

Schedule Unity creation/ImageConversion.LoadImage on main thread after bytes verified; display profile fixed sRGB/no mipmaps/markNonReadable. Invalid header/unsupported image/limit/malformed body/decoder false -> typed failure and SDK cleanup. Validate actual decoded size too. Network/hash/header inspection off main thread where supported; bounded byte[] specific image convenience, raw API never whole-file buffer. [LoadImage API](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/ImageConversion.LoadImage.html).

Cancellation before queued decode prevents it; synchronous native decode already running not physically interrupted by token/deadline. After it returns check captured operation/lifecycle before atomic success handoff. Cancelled/failed/obsolete output before handoff SDK schedules Destroy once and releases buffers. Ready result transfers UnityTextureHandle to application; after completed handoff consumer disposes result if own presentation context became obsolete, never silently reuse shared resource. Independent operation per request, no Demo SDK cache/dedup.

UnityTextureHandle.Dispose atomically invalidates access and schedules main-thread Destroy once; repeated Dispose no effect, Texture after Dispose ObjectDisposedException. No direct raw-object ownership transfer/independent Destroy by consumer and no instantaneous GPU-memory guarantee. Main-thread dispatcher handles callbacks/create/decode/destroy; ordinary DotNet API promises no Unity thread. Actual scene teardown/late success/cancel race tests required. F20 captures exact displayed representation/instance version/AssetId and presentation generation, never uses list index as resource identity.

**Layers / DI.** Target AssetReadRequestValidator/AssetResponseHeaderValidator validate input/status/headers; AssetVerifiedStream counts/checks physical bytes and owns terminal Completion; AssetDownloadService/OwnedTemporaryFileWriter manage destination/whole-file commit; UnityImageHeaderInspector/UnityTextureLoader own bounded JPEG-PNG validation/dispatcher decode. Names describe target types, not existing code. DotNet owns request/results/verification/transfer/counters/retry; Http owns response/Stream/header/redirect/body lifetime adapter; Unity owns image header/decode bridge/dispatcher/Texture handle. Host/provider eligibility/signing/range/cache F05/Q21; frontend changes do not create a new endpoint or ObjectKey model. Client composition captures source/read credential context per operation and validates positive transport/image/error/buffer budgets. SDK owned-temp storage port/test fixture owns only its temp artifacts; provider/native/platform settings numeric values frozen after Q32 measurements. No SQLite asset-body persistence.



### 7.5. Внутренняя последовательность и владельцы implementation

| Компонент DotNet | Вход / метод | Что делает | Кому передаёт результат |
| --- | --- | --- | --- |
| ContentManagerClient | Public interface §5.1/§5.2 | Argument/lifecycle checks, frozen bindings/options, делегирование операции | Coordinator/typed reader/raw service |
| SynchronizationCoordinator | Synchronize/Stop/Discard/recovery | Scope gate до ID, durable intent, Status/Resume/control fences, whole-group/receipt/freshness state machine | Protocol/store и immutable public result/progress |
| SyncProtocolClient | Start/Status/Data/Commit/Resume/Stop/Discard/Freshness по F13 | Формирует exact HTTP request, проверяет status/headers/JSON contracts; no server-side diff/latest | Transport и validated protocol records |
| NdjsonRecordReader | Async response Body | Incremental UTF-8/LF, byte-size/sequence/variant/cursor checks, complete records только | Integrity/validation и store StageRecordAndCheckpoint |
| RuntimeIntegrityVerifier | Original content/profile/hash/group membership | Common canonical profile checks, exact bytes/digest/full target coverage; no generic JSON rehash | Validated activation candidate |
| GeneratedModelCodec / TypedContentReader | Exact registered T + committed StoredRepresentation | Metadata/binding/strict codec, detached T, NotFound/Failed/SnapshotChanged distinction | Public committed read wrappers |
| AssetDownloadService / verified stream | Raw HTTP body/destination + frozen descriptor | Size/SHA/range/status checks, Completion, own temp file commit по §7.4 | Caller-owned stream/file или UnityTextureLoader |
| RetryPolicy / clock | Safe failure + original operation intent | Bounded only-allowed retry/delay, finite stage budgets, unknown-result reconciliation | Тот же coordinator/request meaning, no arbitrary new ID |

UnityWebRequestTransport/HttpClientTransport не парсят synchronization records, не применяют группы и не принимают policy «создать новый ID». Store.Sqlite не делает HTTP/ACK и не определяет desired set. Unity facade не повторяет coordinator, а dispatcher не хранит inventory. Logger/progress callback failures не разрешают потерять already committed receipt или сообщить ложный rollback.

Parser/checksum/group validation/store work продолжаются вне Unity main thread; только native UWR callbacks, UI progress handoff и texture decode/destroy проходят dispatcher. Во время ожидаемого local stage network idle budget приостановлен по Q33, но stage/receipt deadlines конечны; queue overflow — transfer failure, а не игнорирование budgets. Send/response/read/commit/ACK имеют разные completion boundaries.


## 8. Использование API и NSwag


NSwag является согласованным средством web UI CM; F19 не назначает им автоматически runtime Unity SDK. OpenAPI остаётся формальным HTTP-контрактом. Generated admin client не заменяет Delivery API, а `.g.cs` из F22 — модели динамических схем, не копия OpenAPI-клиента. Generated code не редактируется вручную. Выбор генерации технического SDK-клиента и mapping транспортных ошибок остаётся Q29/Q15.


Серверные logical DTO, validation и точные desired-set алгоритмы полностью описаны backend F13. Таблица связывает клиентское действие с его server flow без назначения новых routes/methods/status codes:


| Действие SDK | Поток backend F13 | Что использует клиент | Как применяет результат |
| --- | --- | --- | --- |
| Start нового sync | DF01 | Новый ID, полный exact selection, actual installed inventory | Сохраняет original context/immutable plan identity |
| Выяснить состояние | DF02 | Прежний ID и context операции | Различает active/completed/impossible/unknown по согласованному wire; не создаёт второй Start |
| Продолжить | DF03 | Прежний ID, planId, captured expectedExecutionGeneration и original fingerprint/profile | Resume пригодного context; Data lease захватывается только следующим GET |
| Получить exact data | DF05 | Один finite streaming HTTP response, optional durable application cursor | Incremental full records → validation/durable staging/checkpoint; последовательные группы, pinned upserts и removals по Q33 |
| Сообщить group receipt | DF06 | Target identity/hash и durable receipt реального atomic local commit | Отдельный идемпотентный server commit; потерянный ACK сверяется по Status и повторяется без local double apply |
| Остановить | DF07 | planId и captured expectedExecutionGeneration | Закрывает transport, сохраняет продолжение; начатый commit может сохранить receipt с отложенным POST |
| Окончательно отказаться | DF08 | planId и captured expectedExecutionGeneration; определённый local atomic outcome | Очищает unfinished, сохраняет installed/inventory/committed receipts; unknown commit откладывает cleanup |
| Проверить свежесть | DF09 | Тот же полный selection и actual installed | Separate freshness после full success; unknown при ошибке |
| Получить asset | DF10, F05 DF04 CM-owned YARP gateway | AssetId и отдельный file/lifecycle context; MVP CM credential | Raw stream/destination/headers/helper результат либо HTTP failure, включая non-JSON, отдельно от sync данных |


Public wire v1 использует POST /api/v1/content-synchronizations/{synchronizationId}/start, GET /api/v1/content-synchronizations/{synchronizationId} (Status), GET /api/v1/content-synchronizations/{synchronizationId}/data?planId=...&cursor=... и POST /api/v1/content-synchronizations/{synchronizationId}/groups/{schemaVersionId}/commit. Success control responses используют существующий общий `ApiResponse<T>`; Data — raw application/x-ndjson UTF-8 без BOM, compact JSON + LF, parser принимает LF/CRLF. CamelCase protocolVersion=1, kind/payload/cursor и recordSequence, шесть variants и required fields задаёт F13/Q33. Unknown version/kind, missing/duplicate required property, empty/malformed record и truncated tail — protocol error. Application cursor не HTTP byte range или SSE event ID; серверный HMAC SDK не декодирует. Server ждёт receipt после groupCompleted. Transport parser учитывает произвольные network/UTF-8 boundaries и cancellation с bounded buffer; весь response не буферизуется.

HTTP400 означает malformed input/cursor, 409 — active ownership либо plan/receipt mismatch, 404 — неизвестный ID, включая потерю plan при restart. 410 возможен только для известного expired/discarded plan в bounded tombstone до eviction; timeout/outage не Missing/Expired, исход остаётся Unknown. До headers применим обычный error profile; после headers — operationInvalidated либо обрыв, без JSON error envelope внутри NDJSON. Retained completed plan позволяет replay operationCompleted после потери terminal response.


Для Unity 2022.3.62f3 Windows x64/IL2CPP Demo явно разрешён HTTP/1.1. HTTP/2/HTTP/3 не являются условием готовности этого Demo и не объявляются поддержанными без отдельного transport proof. Reconnect либо смена реально поддержанного HTTP protocol сохраняют ID/plan/checkpoint; смена протокола сама не создаёт новый plan и не повторяет commit. Истёкший plan требует нового ID от actual inventory. TLS/сертификатная ошибка, contract error и предметный отказ не обходятся fallback.


Execution/control profile Q33 добавляет POST `/{synchronizationId}/resume`, `/stop`, `/discard` под тем же base path и отдельный POST `/api/v1/content-synchronizations/freshness`; модели/поля принадлежат backend F13. Header generation читается до data parsing; отсутствие/невалидный required generation — contract failure, не guessed generation. Control receipt repeat возвращает original result, Status читается отдельно. SDK не создаёт второй lease coordinator или DTO-contract.

Отдельно ограничиваются unary request, response-header ожидание, read-idle, assembly полного record, storage/validation/atomic commit и receipt POST. Для HttpClient `ResponseHeadersRead` общий timeout заканчивается на headers; body ограничивает SDK. Read-idle приостановлен во время ожидаемых local validation/commit/receipt POST, но соответствующий stage deadline и server receipt watchdog остаются конечными. Отсутствие records в этой паузе не повод для преждевременного idle failure. Нет heartbeat/empty NDJSON lines. В первом Demo HTTP compression потока выключена; включение требует реальной incremental проверки. Server preparation/write-backpressure/lease/receipt-wait deadlines не подменяются client timers.

Unity transport — UnityWebRequestTransport через UnityWebRequest/DownloadHandlerScript (§5.4), с bounded copied callback queue и worker stream. Neutral queue/observation-budget tests и C# 8 syntax checks пройдены; это не Unity managed compile. Headers-before-body-end, native memory/overflow/Abort/quiescent Dispose, SQLite/AOT и texture decode/handoff/Destroy на fixed Unity 2022.3.62f3 Windows x64/IL2CPP требуют actual Editor/Player gate Q29/Q32/Q33. HTTP/1.1 разрешён; H2/H3 capabilities не заявлены.


## 9. Состояния, ошибки и доступ


Прогресс и cancellation обязательны. Публичные async-операции предусматривают CancellationToken со сквозной передачей в transport/storage/helper. DotNet рассчитывает local progress из plan metadata, доступного total count/groups, durable checkpointed records, validation и local commit: planning → downloading → validating group → committing group → checking freshness → completion — логические стадии, не final enum. Network bytes не durable progress или installed readiness. При известном count допустимо сообщать подтверждённые records/total; при неизвестном byte-size нельзя изображать фиктивный byte percentage. Progress stages/counts и main-thread dispatcher приняты; exact public types/units остаются Q29.


Retry имеет default и override потребителя: bounded transient retries с backoff/jitter только в safe boundary; validation/hash/lifecycle/confirmed-invalidation failures не auto-retry. Retry transport/Resume/control receipt/server receipt/new Start и local apply — разные действия. Local receipt не разрешает двойной apply; stale control не перенаправляется на fresh generation; 409/timeout не разрешают новый ID. Числа attempts/delays/budgets и final public policy fields остаются Q29/Q32.


| Состояние / отказ | Что получает приложение | Сохраняемое состояние / следующий шаг |
| --- | --- | --- |
| Plan/data выполняются | Локальный progress metadata/durable records/validation/commit; прежние committed данные доступны | Durable проверенные records сохраняются отдельно от installed; network fragments не checkpoint |
| Complete group committed, другая незавершена | Частичный итог, точные результаты групп | Новая неполная группа не видна, пригодное resume сохраняется |
| Полный apply/no-change успешен | Установленный набор и separate preflight result | Следующий sync после успеха выбирает приложение |
| Stop / cancellation | Различимый остановленный итог, фактические committed группы | Сохранить пригодный context; начатый commit можно закончить |
| Discard | Попытка забыта только после выяснения actual atomic outcome | Удалить unfinished служебное состояние; installed/inventory/committed receipts сохранить; unknown commit отложит cleanup |
| Временный network failure/timeout | Отдельная transport/unknown outcome ошибка по retry policy | Не false empty inventory; Status/Resume прежде нового ID |
| Plan missing/expired или context непригоден | Различимая невозможность прежнего resume | Автоматический новый ID/plan от actual installed, successful группы сохраняются |
| Второй local call / server active duplicate | Local «уже выполняется» для stable store scope между instances / отдельный HTTP409 | Не очередь/join/newID для обхода active owner; stale control не retarget-ится на fresh generation |
| Contract/schema/profile/serialization error | Malformed Unicode/duplicate member/trailing garbage, unknown profile, wrong exact metadata/group profile+digest — integrity failure, не transient network | No latest/default-fill/number guessing/automatic rehash; old group/last durable checkpoint retained, error before apply |
| Local apply/storage error | Ошибка группы и фактический durable итог | Data/removals/index/inventory/local receipt согласованы; checkpoint не продвигается перед durable staging; crash reconciliation Q29/Q33 |
| Stream completed, local installation незавершена | Не полный успех только по wire marker/EOF | Проверить все group validations/local receipts и server confirmation по контракту; preflight после полного installed успеха |
| Withdrawal / invalidation | Affected group/operation invalidation | Не commit-ить incomplete affected group; old plan не продолжать для неё, не latest substitute; previously committed сохранить и новый plan от actual inventory |
| Preflight не удался | Данные установлены, freshness unknown | Не rollback и не «up-to-date», выбор повтора у приложения |
| Asset/helper failure | Отдельный отказ ресурса: denied/unavailable/invalid Range/gateway failure либо decoding | Не переименовывает successful data sync в failure; non-JSON error не ready image; cleanup Q29 |
| Unity-потребитель уничтожен/контекст изменён | Lifecycle/cancellation по согласованному контракту | Поздние callbacks не применяются к новому context; ownership Q29 |


SDK не назначает обязательный UI для этих состояний. Доступность кнопки у F20 не заменяет server validation. EntityInstanceKey, hashes и synchronizationId не являются правом доступа. Private registry auth — developer/build access, не runtime-вход в Demo. Protected session/errors принадлежат F13/Q16, без второй лицензирующей подсистемы Unity.

MVP application credential provider даёт raw key material только для нового admission/creation replay; SDK не сохраняет activation key/hash по умолчанию и не добавляет его к ordinary content/asset requests. Durable nonsecret CreationRequestId до POST сохраняет тот же intent при unknown creation outcome; replay возвращает original session/token/pin без TTL extension, temporary failure не new ID. Proven replay end/new admission получает новый session-bound plan: old synchronization context/cursor не перепривязывается silently, installed данные остаются. Runtime bearer header-only, no logs/query/S3 forwarding. Подробный выбранный recovery/auth/lease contract — [F13](F13-content-delivery.md)/[Q16](../../backend/questions/Q16-activation-and-sessions.md#принятый-протокол-сессий-и-response-leases); exact provider/storage/public signatures Q29. Demo не получает эти зависимости.


Диагностика использует независимую SDK abstraction и Unity-адаптер, безопасные ID/стадию/описание ошибки. Source-категории HTTP/AUTH/ASSET/ADDRESSABLES/CACHE не требуют внедрить все механизмы в Demo. Tokens, secret URLs и полные payload не записываются в журнал. Функциональная доступность SDK — через программные операции без обязательного визуального окна; доступность и показ сцены отдельно F20.


## 10. Проверки и приёмка


Проверки связываются с [S06](../standards/S06-api-client.md) и [S08](../standards/S08-testing.md). First required Player Unity 2022.3.62f3 Windows x64/IL2CPP selected Q32; Editor patch 2022.3.62f3 fixed; stripping/settings/dependency/native locks pinned before spike, actual values not yet provided. Fixtures all15/twoexact/nested/Image/generated models; initial/update/restart/ACK/StopDiscard/withdrawal/freshness. Editor/mocks/docs not Player/AOT/HTTP/storage/resource proof; unsupported targets not silently supported.


| Сценарий | Ожидаемый результат |
| --- | --- |
| SDK без F20/Addressables | DotNet не зависит от Unity/storage SDK, основной Unity работает с данными/raw image самостоятельно |
| Один high-level sync | SDK выполняет plan/data/staging/apply/inventory/preflight; сцена не реализует свою API-цепочку |
| Полный registry, две версии Schema и deselection | Обе representations сохраняются отдельно; удаляются только representations исключённой managed пары |
| Typed lookup по EntityInstanceKey | Работает без GUID в прикладном вызове, учитывает exact model context; technical IDs остаются в inventory |
| Exact generated model | F21/F22 source компилируется/десериализует поддержанный F13 payload; Draft/latest не заменяет выбранную структуру |
| MVP temporal multiversion/profile | Old Naive DateTime Unspecified и new Offset DateTimeOffset в одном exact-model project; canonical UTC 7digits Z/100ns сохранены, no local timezone conversion; unknown profile/binding mismatch rejects до group apply |
| MVP temporal golden fixtures | Equivalent instants same runtime bytes/hash, offset date rollover/±14:00/range overflow/precision/bounds/uniqueness/stable ties и unchanged old `/1`; SDK consumes supported runtime, не выполняет migration/reorder. Codec fixtures и real Unity Player checks должны быть выполнены отдельно |
| Missing selection/inventory versus explicit empty | Отсутствие не превращается в массовое Remove; валидный полный empty выбор обрабатывается по F13 |
| Неполная группа при успехе другой | Неполная остаётся прежней; весь complete group/data/removals/index/inventory атомарны |
| Group boundary/target hash/removals | Missing record/removal и hash mismatch блокируют commit; успешная validation активирует весь target с durable local receipt; приложение видит прежний набор до atomic commit |
| Canonical server/Unity fixtures | Hand-specified expected bytes/profile/digest проверяются обеими реализациями, не production helper как oracle; Unicode/control/no normalization, Long>2^53, exact Decimal/trailing-zero stripping, Float RFC vectors/-0/nonfinite reject, absent/null/default/duplicates/trailing garbage |
| Profile/group/inventory integrity | Unknown profile rejects before apply; whole upsert+removal/exact membership/profile differences detected; staging/selection не installed, own commit не меняет original plan fingerprint; provenance/comment/new artifact identity не runtime digest/dedup/Approval |
| Fragmented/malformed/oversized record, replay и legitimate empty target | Неполный хвост не checkpoint; invalid не превращается в empty Remove; replay durable record не имеет двойного local эффекта |
| Public wire v1 | Все шесть camelCase variants/protocolVersion=1; contentJson escaping и UTF-8 splits/hash-before-typed-parse; unknown version/kind/profile/duplicate member rejected. Normal immutable sequences/replay full payload+bytes, sequence=0/no cursor invalidation вне plan, wrong base/count/membership не commit-ятся |
| Process exit после durable record / crash в commit | Durable context/records/cursor пригодны после запуска; network хвост отброшен; фактический commit/local receipt согласованы без половины группы |
| Потерянный Start response / commit ACK | Status/Resume/reconciliation, прежний ID/result, без слепого нового Start/двойного apply |
| Resume cache hit/miss/expiry | Пригодный old plan с прежним ID; 404 unknown/restart loss, 410 known tombstone до eviction, outage Unknown; доказанно unavailable → новый ID от actual installed, success groups сохранены |
| Status/terminal/lost ACK | planState/executionState/accepted receipts различимы; completed не installed proof; lost ACK не повторяет local apply. Full success только valid operationCompleted+all commits/ACKs+correct HTTP end; retained completed plan replay terminal; no-change zero receipts |
| Stop до/во время commit и Discard | Stop закрывает transport и сохраняет checkpoint; начатый commit может сохранить receipt/POST later. Discard ждёт определённого atomic outcome, unknown откладывает cleanup; installed/inventory/committed receipts сохранены |
| Второй call stable store scope / same-ID active409 | Два SDK instances одного scope: local отказ до второго ID/Start; разные scopes не блокируются случайно. Server conflict не обходится newID; реальный cross-process adapter lock/recovery проверяется отдельно |
| Local run / captured server generation | Старые callbacks/parser/control ACK после нового run не меняют его state; Status generation/Data header captured. Stale Stop/Discard/Resume409 не retarget-ится на fresh generation; late result cleanup принадлежит прежнему owner |
| Control lost ACK / preparing / completed recovery | Identical command возвращает original result snapshot, отдельный Status выясняет current state; preparing planId без ready metadata и Resume первой подготовки; ready target не rewritten, Resume не reserves Data lease; completed terminal suffix dedup |
| Timeout stages / expected pause | Unary/header/body-read-idle/record-assembly/storage-validation-commit/receipt budgets раздельны; во время validation/commit/receipt read-idle suspended, stage/server deadlines конечны. No heartbeat/empty lines и Demo compression-off |
| CM изменился во время old plan | Обычный publish не меняет pinned plan; withdrawal invalidates affected target без commit/подмены latest; previously committed сохранены; после full success preflight сообщает changes |
| No-change / freshness failure | POST полного selection/actual inventory после успеха; outcome/checkedAt UTC/sorted changedGroups корректны. Authority/transport failure даёт Unknown, не отменяет completed sync/не запускает новый |
| Приложение не выбирает следующий sync | После успешного apply нет самопроизвольного повторного плана |
| Progress/cancellation/default retry/override | Реальные стадия/исход/сохранность соответствуют действию; configurable policy без выдуманной гарантии retry mutation |
| Metadata с множеством AssetId / большой файл | Файлы не грузятся все автоматически; основной API поддерживает stream/destination |
| CM gateway boundary | Unity/.NET получает bytes по CM-owned URL; public models не содержат bucket/ObjectKey/endpoint/credentials; gateway upload/delete и произвольный object path недоступны |
| Gateway HTTP/cache/error | Actual GET/HEAD/Content-Type/Content-Length, supported Range/206/416 и conditional cases корректны; non-JSON failure не success; файл больше cache body limit скачивается |
| MVP gateway authorization | Authorization/entitlement/membership/lifecycle проверяются до cache hit/304/Range/proxy; client CM credentials не S3 credentials |
| MVP provider/creation boundary | Raw keys только через app provider admission/replay, no SDK default key/hash persistence/logs; same CreationRequestId for unknown; proven new session gets new plan/no silent old cursor rebind, installed data retained — полные tests F13 |
| Image load/release/failure | Работает без Addressables, lifetime проверен; failure не откатывает sync данных |
| HTTP protocol / reconnect | Разрешённый Demo HTTP/1.1 сохраняет ID/plan/checkpoint/atomicity; future H2/H3 проверяются отдельно |
| Unity 2022.3.62f3 | Target patch/platform/scripting backend/serializer/generated models/lifecycle/main-thread проверены по выбранной матрице |
| Release graph/immutability | Selected base train NuGet-UPM archives and common DotNet DLL hash, separate SQLite/native, configured third-party locks | One DLL identity/shared baseline build; no duplicate Newtonsoft, no backward native dependency; same ID/version different bytes conflict, no overwrite; add-on explicit supported base set | Packaged artifact inspections and actual consumer import/compile, not name match only |
| CI partial publication/staging | Pack once, staged clean consumer/Windows x64 IL2CPP acceptance, one registry artifact fails publish/retry/existing different bytes | Prepublication same-byte tests; retry missing identical artifacts, full exact registry fetch/install before complete; no rebuild/fake release-ready from one successful publish | Actual test-owned feed/registry/project CI permissions and clean install; not source/docs checker |
| Consumer AOT/preservation | Generated exact roots/nested types/converters/binding attributes and supported stripping, native SQLite | Actual Player verifies typed reads/store/main-thread resource lifecycle; historical .g.cs unchanged, explicit consumer rules; unsupported target initialization refuses | Locked Unity 2022.3.62f3 patch Windows x64/IL2CPP and real native/approved HTTP/1.1/proxy route |
| Установка пакетов | Воспроизводимая чистая установка/обновление/зависимости и discovery/private access по Q32, без двойной DotNet |
| Сквозной показ F20 | Реальный CM → HTTP → публичный SDK → separate scene; данные и изображение не подменены fixture-показом |


### 10.1. Дизайн ключевых проверок

Additional adapter acceptance:

| SUT | Воздействие / реальные зависимости | Требуемый результат |
| --- | --- | --- |
| Same DotNet client + два transport adapters | Same literal F13 responses/chunk splits/error headers через .NET HTTP и actual Unity Player UWR | Одинаковые parsed records/checkpoints/results, нет второго protocol implementation |
| UnityStreamingDownloadHandler/body | UTF-8/LF split, multiple records/chunk, reused callback array, headers before isDone, server waiting receipt | Copied own data/ordered incremental read; complete record only, no waiting full response deadlock |
| Unity queue/backpressure | Slow store/receipt, unknown length, bounded queue overflow | Main thread не blocked; explicit overflow/Abort, memory caps observed, durable cursor сохранён, safe original-context recovery |
| Response cancellation/disposal | Before dispatch/headers, mid-read, native completion race, early Dispose, stale callback, shutdown runner | One terminal outcome/cleanup, no fake EOF, no double handler Dispose, no raw response leak |
| Http response ownership | Real streamed/truncated response + shared injected HttpClient | Stream lifetime сохраняет response, headers-only completion, caller client не disposed, body budget enforced |
| SQLite/typed read | Real owned files/native Player, empty group/multiple versions/large Decimal/Int64/corrupt BLOB | Correct detached typed values; empty committed group/inventory consistent; no affinity/double/serializer drift |
| Composition/AOT | Clean packaged Unity 2022.3.62f3 Windows x64/IL2CPP, official Newtonsoft, no Addressables | UWR base raw path работает через interfaces; no duplicate DotNet/serializer/native import, generated roots preserved |




| Сценарий | Исходные условия / воздействие | Наблюдаемый результат | Граница проверки |
| --- | --- | --- | --- |
| Оркестрация и preflight | Полный registry/actual inventory, сценарные plan/data/status ответы; full success/no-change, partial failure и failed preflight | Один high-level вызов ведёт всю цепочку; точные группы/исходы, separate freshness; после success нет автоматического нового Start | DotNet coordinator; transport и state port doubles для последовательности/отказов, не доказательство persistence или сети |
| Durable atomic apply/recovery | Реальный выбранный persistent adapter; завершение процесса после durable staging, до/во время/после atomic commit; потерянный ACK; следующий SDK-вызов | Сохраняются только проверенные части; одна группа/index/inventory согласованы; committed result восстановлен без двойного эффекта; Stop/Discard не стирают success groups | Integration SDK + настоящий adapter; doubles HTTP допустимы, замена persistent store памятью не доказывает recovery |
| Store scope / run fencing / control | Два SDK instances одного stable store; controlled barriers перед ID и поздними callback/parser/control ACK; server generation меняется до старого Stop/Discard/Resume; lost ACK | Второй call rejected before ID; old run не меняет новый state; captured stale control409 без retarget; identical replay original result + Status, no local reapply | Coordinator/contract с controlled-clock/barriers; настоящий adapter доказывает cross-process lock/crash-owner recovery отдельно Q29/Q32 |
| Preparing / terminal recovery | Start response потерян до ready metadata; Stop preparing → Resume original input; completed retained plan и потерянный HTTP end | Status показывает planId без готовых metadata; Resume завершает первую подготовку без target rewrite/lease reservation; Data next GET acquire; terminal suffix replay/dedup | SDK contract + реальный CM coordinator/plan store, не только успешный mock Start |
| Stage budgets / expected pause | Headers задержаны; stream stalled/partial record; validation/atomic commit/receipt POST удержаны barriers; server receipt wait/deadline истекает | Header/body budgets различны; read-idle suspended только expected pause, stage/server watchdog истекают; error не expiry/newID. No heartbeat, compression-off; bound transient backoff/jitter | Controlled timers/parser/storage orchestration плюс реальный CM/proxy/Unity incremental path для buffering/cancel/headers |
| Incremental data stream | HTTP peer отправляет несколько records в fragment, один record по множеству fragments, replay после cursor, malformed/oversized/truncated record, cancellation; завершение stream до local receipt | Не буферизуется весь response; checkpoint только после полного validated durable record; invalid не empty Remove, replay не double apply; completion marker не installed success | Contract/transport integration реального parser/request boundary; принятые NDJSON MIME/framing и configurable numeric limits Q33; без обязательного SSE/chunk endpoint |
| Typed key lookup/multiversion | Generated exact models F21/F22; committed representations одного instance в двух schema-version, stable EntityInstanceKey, Add/Update/Remove | Lookup без GUID возвращает правильный exact model context; версии не схлопываются, index меняется только с atomic commit, removed representation недоступна | Typed store и согласованный serializer/codegen integration; выбранные fixtures, без Domain/EF зависимости клиента |
| Stream и raw image ownership | Файловый response/stream, destination и Unity image helper; complete/fail/cancel/смена контекста/уничтожение потребителя по выбранному ownership contract | Ready объект только после успешной загрузки; HTTP/stream/Unity ресурсы освобождаются у согласованного владельца; image failure не откатывает данные | DotNet stream boundary с controllable transport + реальный Unity helper/lifecycle; не подтверждение Addressables handle F23 |
| SDK API/result/cancellation | Full selection separately explicit bindings, cancellation before/inside commit, failed freshness, expected HTTP/storage errors, competing call | Completed+Unknown separate; Stopped only known atomic outcome, Failed preserves receipt/context on required storage uncertainty, AlreadyRunning before ID; handles never retarget | Actual DotNet contract with barriers and real store, no scene workflow copy |
| Typed detached read/page | Generated root registrations/multiversion, caller modifies returned model, page continuation across update, corrupt payload/binding | No assembly scan/latest; conflict rejects config; stored bytes/inventory/hash unaffected; NotFound/Failed/SnapshotChanged distinct | Actual binding mapper/serializer/read adapter; no production serializer as fixture oracle |
| Store conformance/scope/format | Same-source/partition/canonical-path aliases/two SDK instances/processes, crash/reopen, unknown commit, unsupported/migration failure | Whole-run OS gate before ID separate SQL transactions; atomic stage/activation/receipts; unsupported target no weakened promise; no cleanup/auto-delete on uncertainty | Shared suite for adapters, SQLite real files/process termination/native settings on pinned Q32 platform |
| Storage scheduling | Blocking provider call/parse plus Unity callback tracing | Storage does not block Unity main thread; main-thread dispatcher callbacks; async names not proof | Actual native provider + Unity Player/dispatcher, not Task mock |
| Raw open/transfer/destination | Accepted headers then EOF/truncation/overlong/hash-size mismatch/early Dispose/non-JSON denied/redirect/encoding; caller stream starts nonzero | Opened separate Completion/Verified; expected failures typed while Stream exceptions normal; actual written count/leave open/no seek/no after-write retry | Actual HTTP/stream adapter, literal own byte fixtures; large CM/YARP/MinIO tests prove bounded path |
| Range and file commit | Prior strong ETag,206 Content-Range/count/total mismatch,200 instead206,416, temp/cancel/target exists/move outcome | PartialRange no full SHA, RangeNotHonored before write; full-file helper own temp+verified no-overwrite move, cleans only own temp | Real gateway/provider plus actual local file/platform move and fault barriers |
| Texture limits/handoff | Valid JPEG/PNG malformed header/dimensions/encoded cap/false decoder; cancel queued/running decode and obsolete scene after handoff | Verify full descriptor before Ready, fixed sRGB/no mipmaps/non-readable; synchronous decode no physical abort; atomic owner transfer and exactly once scheduled Destroy | Actual Unity Player/main-thread dispatcher/native decoder; resource leak/doubleDispose/afterDispose access tests, doubles not physical GPU proof |
| Media gateway integration | Реальный CM YARP/private provider и Unity/.NET transport: GET/HEAD, большие bytes, cancel, non-JSON errors, Range/conditional/cache cases при выбранной поддержке | Правильные bytes/status/headers, bounded streaming, observable downstream/upstream cancellation; cache body limit не download limit; MVP права/lifecycle до cache/proxy | Actual gateway/provider/target client; Q21 infrastructure и Q29 SDK contract; не только doubles или header-forwarding проверка |
| Unity transport / reconnect | Unity 2022.3.62f3 Windows x64/IL2CPP и реальный CM/proxy; разрешён HTTP/1.1 | Headers-before-body-end, bounded queue, generation, reconnect, overflow/cancel/quiescent cleanup; прежний ID/plan/checkpoint сохраняется | Actual Editor/Player/native matrix обязательна; syntax/neutral tests не являются Player/IL2CPP/H2/H3 proof |
| Сквозная F20 | Реальный CM, Published exact schema/data и asset; generated models и публичный SDK в отдельной сцене; публикация изменения и повторный выбранный вызов | Реальные data/image, Add/Update/Remove, multiversion и отдельные sync/freshness/resource outcomes видны; сцена не обходит SDK | E2E CM → HTTP → SDK → Unity scene; постоянное серверное хранение/resources, без mock цепочки вместо deployable units |
| Package distribution | CI публикация, чистый Unity 2022.3.62f3 проект, exact install/discovery/private access/dependencies/upgrade-downgrade/lock конкретной GitLab установки | Воспроизводимое разрешение нужного набора, без двойной DotNet; известные ограничения отражены; успешный publish отдельно от install | Q32 integration spike реального GitLab/UPM; обычная .NET поставка проверяется по выбранному packaging, без утверждения готового NuGet ID |


Unit-проверки применимы к сериализации, mapping ошибок, построению запросов, состояниям координатора и typed lookup с doubles. Integration должна проверять HTTP/streams, реальные границы persistent state adapter и recovery/crash, cancellation и errors. Unity-проверки относятся к transport/config/lifecycle/main-thread и выбранным платформам; наличие `netstandard2.1` не заменяет их. E2E через реальный CM и F20 проверяется отдельно. Addressables-проверки принадлежат F23; runtime Auth-тесты не добавляются в Demo.


Текущая generic реализация проверена локальными canonical/generated-binding, protocol/coordinator, raw HTTP/assets и реальными SQLite file/process tests. Отдельный SDK E2E прошёл через actual CM/PostgreSQL/MinIO: F22 ZIP скомпилирован без изменения generated source, multiple exact versions прочитаны typed из durable store, no-change freshness, update/remove и gateway size/SHA подтверждены. Unity source проверен C# 8 syntax checks и neutral bounded queue/decode-observation tests; managed compile с Unity assemblies, Editor и Player/IL2CPP не запускались, поэтому F19 не Done. GitLab CI, публикация и private registry clean install не выполнялись. Точные финальные наборы/числа и артефакты фиксируются в [плане реализации](../../backend/f19-demo-implementation-plan.md); эти результаты не доказывают F20/F23/launcher cache.


## 11. Последовательность изменений и открытые вопросы


### 11.1. Поставка и зависимости пакетов

SDK использует два существующих репозитория: contentmanager.sdk.client владеет ContentManager.Sdk.Client.DotNet, ContentManager.Sdk.Client.Store, ContentManager.Sdk.Client.Store.Sqlite и generic tests; contentmanager.sdk.client.unity владеет Unity source/asmdef, UPM staging и отдельным packaged acceptance harness. CM backend и Schema.CodeGeneration.Core независимы. F20 scene/F23/Auth/launcher cache не входят в текущую реализацию. Actual GitLab endpoints/project access/runners ещё не проверены; repository names не подменяют registry configuration.

| Единица / assembly | NuGet ID | UPM ID | Прямая зависимость / ответственность |
| --- | --- | --- | --- |
| Sdk.Client.DotNet | ContentManager.Sdk.Client.DotNet | com.leansoft.contentmanager.dotnet | netstandard2.1;net10.0 runtime/orchestration/transport; depends on Store; HttpClient only net10 |
| Sdk.Client.Store | ContentManager.Sdk.Client.Store | No new UPM ID assigned in generic slice | netstandard2.1 IClientStateStore, immutable models/errors; no upper client/native dependencies |
| Sdk.Client.Store.Sqlite | ContentManager.Sdk.Client.Store.Sqlite | com.leansoft.contentmanager.store.sqlite | Store; replaceable ready store adapter owns managed/native provider/imports; actual supported target/refusal §6.4 |
| Sdk.Client.Unity | — | com.leansoft.contentmanager.unity | DotNet; source facade/platform transport bridge/dispatcher/image helper; application explicitly composes SQLite adapter |
| Sdk.Client.Unity.Addressables | — | com.leansoft.contentmanager.unity.addressables | Optional Unity/DotNet plus selected Addressables dependency; separate supported-version cycle, no base reverse dependency |

DotNet precompiled netstandard2.1 DLL/PDB/XML documentation shipped UPM; NuGet uses same produced DLL bytes/hash. Wrappers have separate package/archive hashes; same artifact version cannot be overwritten. Unity facade/platform bridge source+asmdef, compatible consumer reference graph explicit. SQLite artifact owns provider/native components and per-platform/architecture PluginImporter settings; unsupported initialization fails, no in-memory substitution. Third-party neutral canonical/helper dependencies remain owner Q19, no Domain/EF/Unity/S3 added into DotNet.

One Newtonsoft.Json assembly per process. Generic SDK фиксирует NuGet Newtonsoft.Json 13.0.3; Unity package dependency — official com.unity.nuget.newtonsoft-json 3.2.2, без второй Newtonsoft DLL в SDK. ContentManager.Canonicalization подключён как отдельный локальный immutable NuGet 0.0.0-f19.local.2; historical canonical /1 и F22 generated bytes сохранены. SQLite adapter фиксирует SQLitePCLRaw.core/provider.e_sqlite3 3.0.5 и lib.e_sqlite3 3.53.3. Generic Windows x64 checks используют native 3.53.3, WAL/FULL, off-thread native calls и process-held OS lock на canonical store path; это не Unity native/AOT proof. Local NuGet/UPM 0.0.0-f19.local.3 — staging artifacts, не опубликованный release.

Runtime/Editor/Tests/Samples separated, references explicit; stable .meta/asmdef GUIDs versioned. Consumer avoids simultaneous DotNet imports from UPM/NuGet/Assets. UPM layout package.json/Runtime/needed Editor/Tests/Samples~/Documentation~/README/CHANGELOG/LICENSE; Unity package version/dependencies exact. NET SDK/runtime restore graph pinned. SDK/package SemVer independent store format/protocol/content/generator profiles; published profile-compatible generated .g.cs not rewritten to solve packaging.

**First target.** Required Demo Unity 2022.3.62f3 Windows x64/IL2CPP Player; HTTP/1.1 явно разрешён. Editor patch выбран; stripping/build settings и native importer/build locks остаются открытыми до actual Player gate. Generic netstandard2.1 и Http net10 builds не доказывают Unity managed compile/native/AOT. Android ARM64/VR/Linux/macOS/WebGL не заявлены поддержанными; optional Addressables не расширяет base matrix. H2/H3 support требует отдельной подтверждённой матрицы.

**AOT integration.** SDK/library reflection/binding/converter roots and sample consumer generated roots/nested generic collections preserved by documented build rules/link.xml in supported consumer integration. SDK core no UnityEngine dependency; Unity Editor/build bridge/templates own Unity preservation. SDK/example includes necessary recipe, explicit registered types; historical .g.cs unchanged, no reflection assembly scanning. Verify real IL2CPP and configured stripping level, not mere presence of link.xml. Exact generated shared CLR namespaces/types freeze owner Q31/F22 before source golden; this release policy doesn't introduce byte-changing old-profile regeneration.

**Distribution/access.** Project-level GitLab NuGet/npm registries предназначены для двух owning SDK repositories; actual host/project endpoints/access/runners ещё не проверены. Unity scoped registry prefix com.leansoft.contentmanager only, not npm @scope. CI job token publish when permissions verified, developers/consumers read-only package access; credentials user/CI config, not packages/source/demo/ScriptableObject. Actual Unity registry authentication/private dependencies/discovery and clean install prove viability. Registry auth not CM runtime Identity/license dependency. If required registry capability fails, first fallback immutable release tarballs from identical validated build; no automatic proxy/new registry infrastructure.

**Release train.** DotNet/Http/Unity/SQLite aligned SDK SemVer in one release set; optional Addressables independent version with exact supported base set. Specific first version chosen at release, not source-example1.0.0. CI tag -> validate -> tests -> pack once -> clean packaged consumer install/Player acceptance -> publish same accepted artifacts. Prepublication tests resolve staged exact artifacts/tarballs/local feed (test-owned temporary metadata only); no rebuilding or patching package bytes between verification and publish. Registry-specific end-to-end fetch/clean installation checked on published artifacts too before release set considered complete.

**Автоматический UPM CI.** Для Unity-репозитория выбран workflow существующего generic SDK: push в staging запускает release-version с rc, push в main — stable с агрегированием prereleases. Release job ждёт build/tests/security; созданный тег запускает отдельный pipeline с package/publish. В branch pipeline эти две стадии не отображаются. Версия тега записывается только во временные копии обоих UPM manifests и exact dependency Addressables на базовый UPM; зафиксированные generic NuGet DLL/lock и исходные manifests не переписываются. Текущая автоматическая поставка выпускает base и add-on вместе; независимая упаковка add-on остаётся отдельным ручным инструментом. Это не делает add-on runtime-зависимостью base. Фактические CI permissions, создание тега, registry upload и clean Unity install проверяются отдельно; локальный pack/dry-run их не доказывает. Подробности выбора и результатов — [Q32](../questions/Q32-sdk-package-distribution.md).

Корневая `.versionize` задаёт `tagOnly: true`: Versionize берёт SDK release version из Git tags, не сканирует вложенный Unity test project и не меняет версии csproj/ProjectSettings. Для staging без version tags первая версия задаётся `1.0.0-rc.0`; далее работает обычный Versionize prerelease calculation. Main сохраняет стандартный stable workflow компонента. Это исключает как ошибку поиска ProjectSettings.asset, так и начальный stable fallback Versionize при запросе rc.

Publication isn't transaction across registries/packages. Track exact artifact hashes/versions and resumable publication evidence; partial publication not ready release. Retry publishes only missing same-byte artifacts after validating existing ID/version hash; existing different bytes conflict, never overwrite. Complete set only all required intended artifacts verified available/installable; no success inferred from one npm publish. Fix new version, old packages retained per release policy. CI stores target/dependency/build/provenance/verification facts safely, no credential material. Physical pipeline/provider setting implementation and real CI permissions aren't claimed here.

F20 остаётся отдельной consumer scene вне текущего slice. Unity repository содержит packaged acceptance harness, который не объявляется сценой F20; consumer использует staging/released packages, не direct SDK source folders. Small Samples~ snippets optional; full scene no runtime dependency. Generated models downloaded from F22 ZIP into consumer project before runtime and compile against selected SDK set; no automatic NuGet/UPM publication of generated models required. Sample locks Editor patch/manifest/packages-lock/native/backend/stripping/settings and demonstrates actual CM+gateway+store+typed binding without hidden source copy.

**Release tooling records — target CI values, not CM runtime DTOs.**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SdkVersion | `string` (SemVer) | Да | Aligned base release version | DotNet/Http/Unity/SQLite same; optional add-on own version |
| SourceCommitId | `string` | Да | Exact source build provenance | Real verified commit, no rebuilding after acceptance under same version |
| Artifacts | Immutable artifact descriptor list | Да | Kind/ID/version/archive hash and shared DLL hash | nupkg/tgz wrappers distinct; same DotNet DLL/hash in both; no secrets/private token URLs |
| Dependencies | Exact locked package/native/provider/runtime set | Да | Reproducible restored consumer graph | No latest/ranges as release fixture; Newtonsoft one assembly per process |
| PlayerTarget | Exact Unity patch/platform/architecture/backend/stripping/build-settings record | Да | Tested intended matrix | First Windows x64/IL2CPP; patch/build values pinned before test, separate unsupported target |
| VerificationEvidence | Immutable safe build/install/Player/registry observations | Да | Actual successful checks for same artifacts | Docs/static mocks not Player/native/registry proof |
| PublicationEvidence | Per artifact ID/version/hash verified publication result | Да | Safe recovery of partial publish | Only identical existing artifact replay; complete set after all required checked |

[Q32](../questions/Q32-sdk-package-distribution.md#согласованная-поставка-и-release-policy-sdk) owns decisions/remaining actual deployment values; [Unity scoped registries](https://docs.unity3d.com/2022.3/Documentation/Manual/upm-scoped.html), [Unity Newtonsoft](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.2/manual/index.html), [link.xml](https://docs.unity3d.com/2022.3/Documentation/Manual/ManagedCodeStripping.html), [GitLab registry](https://docs.gitlab.com/user/packages/npm_registry/) supply mechanism references. No actual repository/publication/Unity install run by this specification.



### 11.2. Порядок реализации


1. Реализовать согласованные F13/Q33 wire и F19 public/store models/signatures/operations без пересмотра границ; выбрать numeric guardrails/physical provider и pinned environment, выполнить conformance.
2. Реализовать DotNet contracts/API/serializer и заменяемые transport/state ports без Unity/Domain/EF зависимостей.
3. Реализовать persistent adapter, typed store/lookup и atomic group recovery; проверить crash/ACK/Stop/Discard и local gate.
4. Реализовать high-level sync, progress/cancellation/retry, automatic resume/new plan и post-success freshness.
5. Реализовать Unity 2022.3.62f3 facade с UnityWebRequestTransport/DownloadHandlerScript, bounded async bridge, dispatcher/logger и raw/texture helpers §5.3–§5.5/§6.5/§7.4; pinned Player early-header/overflow/cancel/cleanup tests обязательны.
6. Проверить Q33 реальный finite streaming HTTP/framing/cancellation/reconnect и HTTP3→HTTP2 path; Q32 — минимальную package installation/distribution.
7. Подключить generated models F21/F22 и отдельно реализовать сцену F20 через публичную библиотеку; optional F23 не блокирует raw сценарий.
8. После самостоятельного Demo и выбора Q37 manifest/profile/session/cache contracts добавить общий §6.8 manager/adapters и launcher/app интеграцию; real двухпроцессная/большая-file/fault acceptance отдельно от первого Demo.


Документация подключения, программный пример, публичные операции, ошибки/retry/cancellation, state и lifetime входят в результат библиотеки. Registry/generator остаются самостоятельными соседними фичами и не дублируются runtime SDK.


### 11.3. Открытая детализация


| Владелец вопроса | Что уточняется | Что уже принято |
| --- | --- | --- |
| [Q29](../questions/Q29-unity-client-package.md) | Literal error/stage codes/page encoding, numeric budgets/retry, actual Demo raw media/texture conformance; SQLite/provider/platform conformance | Public sync/control/read/result models и store operations согласованы §5.1/§6.2/§6.4, detached values, group-bound page continuation, OS scope lock, format migration/conformance |
| [Q37](../questions/Q37-launcher-asset-delivery.md) | Exact manifest/profile/cache API and models, bootstrap re-admission, lock protocol, limits/quota/retention/offline and 5–10 ГБ upload | После Demo общий DotNet manager, SQLite metadata + filesystem bytes, field-level default OnDemand/Preload/RequiredBeforeLaunch и pinned snapshots; предложенные scoped single-flight/integrity/leases models/ports §6.8 |
| [Q21 media](../../backend/questions/Q21-media-storage.md) / [Q29 media resource](../questions/Q29-unity-client-package.md#raw-assets-и-unity-image-helper-согласованный-контракт) | Numeric limits/literal codes/private provider/transforms and actual CM/YARP/MinIO/Player conformance | Demo raw/HEAD/file/Range and texture signatures/models/lifetime/no SDK cache agreed §5.2/§6.5/§7.4; frozen descriptor unchanged |
| [Q30](../../backend/questions/Q30-content-synchronization.md) / [Q15](../../backend/questions/Q15-api-and-source-code.md) | Machine reason/error registry mapping, concrete adapter integration и compatibility с исходниками | CM рассчитывает exact desired/diff; Start/Data/Commit/Status public wire v1 принадлежит backend F13 |
| [Q33](../../backend/questions/Q33-sync-resume-transport.md#execution-control-и-transport-profile) / [Q32](../questions/Q32-sdk-package-distribution.md) | Numeric timeout/record/buffer/capacity/retention configuration, concrete adapters и реальная Unity/CM/proxy conformance matrix | Public wire v1 и execution/control/Freshness: generation header/Status, stale-command fencing/original ACK, preparing Resume, Data lease acquire, stop/discard recovery, finite stage deadlines/expected pause и Demo compression-off согласованы; durable atomic state и absolute reservation TTL сохраняются |
| [Q31](../../backend/questions/Q31-schema-code-generation.md) / [Q34 DateTime MVP](../../../business/questions/Q34-schema-field-semantics.md#datetime-mvp-temporal-semantics-и-совместимость) | Whole new-profile codec/golden bytes/shared CLR freeze и numeric guardrails; CLI workflow выбран F22, exact codecs отдельно | Old /1 immutable; Offset DateTimeOffset/UTC7digits/instant equality и codegen до runtime согласованы; no runtime Registry fetch/UI generation |
| [Q32](../questions/Q32-sdk-package-distribution.md) | Actual GitLab/repo/host/project context, Unity 2022.3.62f3 stripping/dependency/native/transport locks, CLR binding freeze with F22 and real CI/registry/Player results | Two SDK repositories, generic DotNet/Http/Store.Sqlite and Unity source/UPM; IDs/DLL-source/aligned train/immutable release policy selected; first Windows x64/IL2CPP, HTTP/1.1 allowed |


Неизвестное остаётся «Уточняется» у владельца вопроса, не заполняется source-примерами routes, методов, числовых defaults или permission rules. Вопросы не пересматривают принятые обязанности и не требуют повторно выбирать Unity 2022.3.62f3 Windows x64/IL2CPP, имя DotNet либо самостоятельность F19/F20.

## Актуальный generic этап — 10.10.2026

По уточнению владельца рабочий generic репозиторий — **contentmanager.sdk.client**; contentmanager.sdk.client.dotnet является старым источником, его не удаляют. В новом репозитории ровно три production csproj/NuGet: ContentManager.Sdk.Client.DotNet, ContentManager.Sdk.Client.Store и ContentManager.Sdk.Client.Store.Sqlite. Store владеет IClientStateStore/immutable metadata/typed errors в namespace ContentManager.Sdk.Client.Store и не зависит от DotNet или SQLite; DotNet и SQLite зависят от Store. F22 value types/attributes/converter остаются в ContentManager.Sdk.Client.DotNet / assembly ContentManager.Sdk.Client.DotNet. DotNet имеет netstandard2.1 и net10.0; HttpClientTransport включён только в net10.0, отдельный Http пакет не выпускается. Store/Sqlite — netstandard2.1.

Порядок: закончить generic source + tests + NuGet → передать владельцу для CI/CD/publication → владелец устанавливает exact NuGet в Unity SDK → только после этого продолжать Unity. Межрепозиторные ProjectReference/linked source из Unity в generic запрещены; Unity использует PackageReference. Уже созданный Unity source остаётся предварительным, не принят и сейчас не изменяется; предыдущие UPM artifacts не подтверждают новый трёхпакетный graph. Наличие/отсутствие Editor не блокирует текущий generic этап.

Владелец перенёс canonicalization source в ContentManager.Sdk.Client.DotNet. Код находится в namespace ContentManager.Sdk.Client.DotNet.Canonicalization; отдельного NuGet ContentManager.Canonicalization и обязательного owning feed больше нет. Алгоритм/profiles/canonical bytes сохранены. Поставка остаётся из трёх SDK пакетов; actual pipeline/private install/publication выполняет владелец. Межрепозиторных source references нет.

### Тесты и CI generic SDK — 10.10.2026

Тесты переведены на xUnit v3 с отдельными Fact/Theory и категориями Unit/Integration; запуск через dotnet test, без рукописных Program.cs runners. Итоговый Release build обоих DotNet targets и Store/Sqlite: 0 warnings/errors; dotnet test: 200 passed, 1 skipped (CM_F19_TEST_API не задан). После переноса canonicalization source restore не требует отдельного Canonicalization пакета; private feed не проверялся. Отдельный package-only xUnit consumer: 1/1 passed после упаковки трёх Release SDK, restore из локального feed с пустым cache; в assets ровно три SDK package dependencies, без ProjectReference. Временные feed/cache/build outputs удалены. CrashHost — отдельный non-test/non-packable helper для реального аварийного завершения процессов SQLite. Windows x64 native сценарии явно пропускаются на неподдерживаемой платформе; CM integration явно пропускается без CM_F19_TEST_API.

CI использует имеющиеся GitLab dotnet components1.15.1: staging → rc tag/package, main → stable tag/package; pack targets — ровно три production csproj. Build/test предшествуют release и pack; deploy получает artifacts своего pack. Actual GitLab pipeline не запускался. Скрипты ps1 и временные отчёты хешей не входят в поставку. Unity ждёт установки NuGet владельцем; для будущих UI проверок выбран bUnit, без JS tests.

## Unity после установки generic NuGet — 10.10.2026

Владелец установил ContentManager.Sdk.Client.DotNet/Store/Store.Sqlite 1.0.0 в contentmanager.sdk.client.unity. Handoff выполнен; прежнее ожидание выше относится к предыдущему этапу. Unity использует exact PackageReference трёх пакетов, без межрепозиторного ProjectReference/linked source и отдельного Canonicalization пакета. Addressables относится к отдельной optional F23; существующий scaffold не входит в реализацию F19.

Production source находится обычными файлами рядом с csproj в src/ContentManager.Sdk.Client.Unity: UnityContentManagerClient, UnityWebRequestTransport, UnityMainThreadDispatcher, BoundedDownloadBody, DecodeObservationBudget, UnityTexture. Manifest/asmdef/linker/plugin metas находятся в Packaging. Build создаёт .upm/com.leansoft.contentmanager.unity из этих исходников и установленных NuGet DLL. .upm/ игнорируется Git; tracked files в нём отсутствуют. Никакие production Compile items не ссылаются на generated UPM output. Guard отклоняет сборку без runtime source и реальных Unity assemblies.

UPM содержит три portable SDK DLL, SQLitePCLRaw core/provider 3.0.5 netstandard2.0 и native e_sqlite3 из lib 3.53.3. Stable metas сохраняются; native importer включает Windows Editor x64/Standalone Win64. Только официальный com.unity.nuget.newtonsoft-json 3.2.2 поставляет Newtonsoft, дополнительной DLL нет. Generated F22 fixture и production serializer не менялись.

Default Release build solution без переопределения UnityManagedPath прошёл с 0 warnings/errors. Directory.Build.props выбирает явно заданный Editor/managed path либо установленный Hub Editor. 30 neutral/package и 3 cancellation xUnit tests прошли. Regression pre-header cancellation возвращает public raw OpenRead/HEAD Cancelled, вместо Failed.

В tests/UnityProject создан настоящий Unity test-проект, который импортирует локальный generated .upm и фиксирует dependency lock. Фактически запущен Unity 6000.6.5f1 Editor в batchmode/PlayMode: **3 passed, 0 failed, 0 skipped**. Проверены native SQLite создание файла и exact missing-key lookup с регистрацией неизменённой F22 all15 модели; реальные UnityWebRequest headers/первый fragment до завершения body; verified PNG → non-readable Texture2D без mipmaps, сохранение application-owned texture после client shutdown и Destroy после освобождения handle. Это Editor/native/GPU execution, не Player/AOT/stripping acceptance. Cache/log/test outputs игнорируются, Editor завершился после тестов.

Обязательная проверка согласованного **Unity 2022.3.62f3 Windows x64 IL2CPP Player** остаётся открытой: соответствующие Editor/Windows IL2CPP module/C++ tools не доступны в проверенной установке. Unity 6 Editor не доказывает совместимость с лаунчером на 2022.3. Нужны реальные Player import/build/run, AOT/stripping, queue overflow, stream/Range failures, SQLite recovery и CM/PostgreSQL/MinIO sync через Unity transport. Consumer сохраняет собственные F22 generated roots/enums/properties и closed generic collections; package link.xml сохраняет SDK assemblies. **F19 целиком не завершена** до обязательной Player-проверки.

В этом Unity этапе backend/контейнеры/Player не запускались; использовался тестовый loopback HTTP. CI/push/publication/private-registry install/коммиты не выполнялись. Scripts ps1 и отчёты package hashes не входят в репозиторий.

## UPM-миграция F19 — 11.10.2026

Уточнение владельца имеет приоритет над MigrationKit: F19 остаётся без Addressables, но отдельная F23 включена в Demo и будет реализована позднее. Основной UPM ID сохраняется **com.leansoft.contentmanager.unity**. Будущий add-on зависит от основного пакета, основной asmdef не зависит от Addressables и один поставляет общие SDK DLL. Generic SDK packages не пересобирались, public contracts/runtime source/generated serialization не изменялись.

Актуальная структура: **src/com.leansoft.contentmanager.unity/Runtime** содержит обычные Unity исходники/asmdef/link.xml с сохранёнными .meta GUID; package.json находится в корне этого UPM. Внутри распространяемого пакета нет csproj/slnx. Build/DependencyRestore восстанавливает три опубликованных NuGet 1.0.0 для netstandard2.1 с committed packages.lock.json; build/PackageAssembler читает project.assets.json/packageFolders, сверяет полный graph ID/version/NuGet hash metadata с lock, проверяет CLR identities, собирает .artifacts/upm и кандидат .tgz. DLL hash reports/dependencies.json/SHA256SUMS/ps1 не создаются. Stable plugin importer templates находятся в build/plugin-metadata; временные DLL/cache/results/.artifacts и generated embedded package исключены Git, source/meta/ProjectSettings/locks остаются учитываемыми файлами.

Корневая slnx теперь содержит только restore/build/test infrastructure. Существующие xUnit tests сохранены; UnityAdapter.Runtime — test-only managed compile bridge реальных Editor modules, не production UPM/DLL. Tests/UnityProject импортирует **generated embedded** пакет, его manifest сохраняет существующий ID, packages-lock.json подтверждает source:embedded. User-owned NuGet.Config не менялся; CI использует отдельный build/NuGet.CI.Config с фактическим alldirect.ru feed и process/env authentication. Прямой private install/feed availability не проверялся; restore выполнен offline из уже установленного cache.

Фактические результаты: Release build 0 warnings/errors, **52 xUnit passed** (49 neutral/package/assembler, 3 adapter); 19 assembler checks включают missing native/managed, incompatible TFM/RID, conflicts, GUID/metadata, credential/build file rejection, stale graph/CLR name, archive reproducibility/content и embedded replacement. Два defect regressions ревью наблюдались RED→GREEN. Unity **6000.6.5f1 Editor: 3 EditMode + 3 PlayMode passed**, реальные native SQLite/HTTP early headers/PNG Texture2D lifecycle/Destroy сохранены. Editor процессы завершены.

Минимальный CI подготовлен: locked restore → assembled UPM → Linux .NET tests + Windows host/Unity EditMode/PlayMode → .tgz для stable/rc tag, совпадающего с package version. Zero-test failure/Editor version/exit/XML/process cleanup проверяются. Runner unity-windows требует PowerShell7/.NET10/licensed Editor из ProjectVersion, UNITY_EDITOR_PATH и Generic GitLab CI_JOB_TOKEN allowlist. Pipeline/push/publication/private registry install не запускались; publish job отсутствует до отдельной проверки registry/UPM handoff. .tgz не объявлен Unity-signed.

Реальный CM/PostgreSQL/MinIO sync через Unity transport проверен в Editor, как описано ниже. Обязательный **2022.3.62f3 Windows x64 IL2CPP Player** остаётся непроверенным; Unity6 Editor не заменяет этот gate. **F19 не завершена целиком.** В самой UPM-миграции backend/контейнеры/Player/коммиты не запускались.

## Проверка Unity SDK с реальным CM

Фактически выполнено 11.10.2026 в **Unity 6000.6.5f1 Editor / Windows x64**: два NUnit PlayMode integration tests с реальным CM прошли; полный PlayMode suite — **5 passed, 0 failed, 0 skipped**. После восстановления согласованного публичного namespace **ContentManager.Sdk.Client.Unity** solution Release build прошёл с 0 warnings/errors, все **52 xUnit tests** прошли. Имя вспомогательного Development csproj не меняет публичный namespace SDK. Backend Release build: 0 errors и 55 существующих warnings; его бизнесовый код не изменялся.

CM запущен отдельным процессом под Windows-пользователем **WIN-FMUDK19NC9N\Spooffit**. PostgreSQL 17.6 и MinIO RELEASE.2025-09-07T16-13-09Z использовались в собственных временных контейнерах с loopback-портами. Через реальный API подготовлены две Published exact версии схемы с required Boolean enabled и optional Image image, загружен валидный PNG 1×1; две модели получены через F22. SDK использовал установленный NuGet 1.0.0 graph и собранный embedded UPM. HttpClient в тесте выполняет только authoring; синхронизация, HEAD/download и image helper проходят через реальный UnityWebRequest и native SQLite.

Подтверждены Add для двух exact версий, типизированное чтение каждой, no-change с UpToDate, сохранение чтения после dispose/reopen физического SQLite файла, Update одной версии без изменения второй, Remove при deselection и после серверного Withdraw, отсутствие незавершённой попытки после завершения. Reopen подтверждает сохранение committed state; он не доказывает recovery оборванной попытки.

Descriptor получен из синхронизированной модели: AssetId/size/SHA совпадают с upload. Реальные gateway HEAD и download завершаются успешно, полная загрузка имеет Verified, caller stream остаётся открыт. Image helper создаёт non-readable Texture2D 1×1 без mipmaps; неверный SHA даёт Integrity/Failed без handle. Application-owned texture переживает shutdown клиента и уничтожается после DisposeAsync handle и следующего кадра.

Тесты находятся в contentmanager.sdk.client.unity/tests/UnityProject/Assets/Tests/RealContentManagerIntegrationTests.cs; порядок подготовки fixture/F22 source и запуска описан в README Unity-репозитория. Без CM_F19_UNITY_FIXTURE эти два теста явно skip. GeneratedRealCm/*.cs и его .meta, assembled UPM, XML/logs и fixture находятся только в игнорируемых каталогах. Тесты отзывают свои entities при успешном выполнении; при сбое обязательный outer cleanup удаляет весь disposable backend state.

После проверок CM и Unity остановлены, оба временных контейнера удалены вместе с томами, запущенный для проверки Docker Desktop выключен; временные F22 исходники удалены. Generic пакеты не пересобирались, package/profile/serialization contracts не менялись. Коммиты, push, CI pipeline, публикация и private install не выполнялись.

Открыты целевой **2022.3.62f3 Windows x64 IL2CPP Player**, AOT/stripping, interrupted-attempt recovery через Unity и оставшаяся failure/cancellation/Range/bounds матрица. Этот прогон не проверяет все 15 типов на реальном Unity CM пути. Addressables остаётся отдельной Demo-фичей F23.
