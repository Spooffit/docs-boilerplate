# Q29. Unity-библиотека клиента CM

## Выбранная Demo реализация и границы доказательств

Два существующих checkout: contentmanager.sdk.client (portable DotNet, ordinary Http, replaceable Store.Sqlite) и contentmanager.sdk.client.unity (Unity source/asmdef, local UPM и packaged acceptance harness). Public API/bindings/store/transport реализованы; exact F22 ZIP/source сохраняется, Register<T>() замораживается первым public operation, runtime Registry fetch/latest guessing нет. Inventory содержит exact IDs/key/content hash/profile без internal VersionNumber.

Выбранные configurable client limits/defaults и locks находятся в [F19 §5.5](../features/F19-unity-client.md#55-composition-root-и-lifecycle): 8 MiB JSON/record, 4 MiB queue/64 KiB block, page 256; finite AssetTransferTimeout 120 s и DecodeTimeout 30 s. Dependency/native set и две repository boundaries описаны [F19 §11.1](../features/F19-unity-client.md#111-поставка-и-зависимости-пакетов). HTTP/1.1 явно разрешён Demo Unity 2022.3.62f3 Windows x64/IL2CPP; H2/H3 не являются доказанными capabilities.

Локальные generic tests, actual SQLite native/files/process lock checks и отдельный CM/PostgreSQL/MinIO SDK E2E выполнены; source syntax/neutral queue/budget tests Unity не заменяют managed compile, Editor/Player/IL2CPP/native decode/Destroy. Эти обязательные Unity gates не пройдены, F19 ещё не завершена. GitLab CI/publish/private clean install не выполнялись; actual endpoints/access/runners/stripping/build locks остаются открытыми. Точные результаты — [план реализации](../../backend/f19-demo-implementation-plan.md).

## Launcher asset delivery: принятая граница

Актуальная первая платформа — Unity 2022.3.62f3 / Windows x64 / IL2CPP. После самостоятельного Demo launcher пишет согласованные entity snapshots, Player читает pinned snapshot и сам выполняет CM media cache misses. DotNet owns единый download/cache coordinator; SQLite adapter сохраняет jobs/index, filesystem — большие verified files. Optional Addressables custom locator/entity/raw-asset providers только оборачивают F19, native bundles сохраняют свой pipeline. Нормативный implementation owner — [F19 §6.8](../features/F19-unity-client.md#68-общий-файловый-cache-и-launcherapp-интеграция-после-demo), add-on — [F23 §5.2](../features/F23-unity-addressables.md#52-cm-entity-и-asset-providers-после-demo).

DeliveryMode выбран на schema AssetReference leaf/ItemDefinition: OnDemand default, Preload optional, RequiredBeforeLaunch verified present refs до запуска соответствующего snapshot. Это не global MediaAsset/Variant policy и не расширение frozen пяти полей runtime descriptor. Demo raw no-cache/no-dedup правила ниже относятся к самостоятельному raw API; после Demo common manager выполняет persistent resume/single-flight/file leases. Exact manifest/profile/bootstrap re-admission/locking/quotas/5–10 ГБ upload вопросы переданы единому owner [Q37](Q37-launcher-asset-delivery.md), не подменяются source signatures/keys. Actual package/provider/runtime compatibility — Q32, не проверена этим текстом.

## MVP credential provider и session recovery

MVP [Q16 protocol](../../backend/questions/Q16-activation-and-sessions.md#принятый-протокол-сессий-и-response-leases) требует app-owned credential provider для new Session/creation recovery; SDK не сохраняет raw key/hash по умолчанию и не вызывает VRS сам. Before POST durable CreationRequestId + Variant/nonsecret selected provider context; trusted credential/license IDs normalizes CM after source validation. Server replay returns same original Session/pin/token/ExpiresAt while eligible; temporary unknown retry same ID, proven replay ended/new admission new ID. No new token/cipher viewer/log output, no new staff form.

Technical SessionTTL30min absolute, replay5min, proofage30sec and responselease5sec — server defaults, not SDK mutable duration values or physical network SLA. Session carries selected basis only. SourceCurrentPeriod change/current cutoff or expiredFreeze mismatch can require new admission; no silent old plan rebind/selected-license fallback/installed deletion. A complete new context/plan must be established, compatible partial data reused only through existing exact integrity/membership checks. Required unknown temporarily blocked; truncated postheaders transfer SDKUnknown until observed typed result, not forced revoke.

Existing provider/context/credential storage ownership, exact callback/public operation signatures and platform secret store remain SDK engineering alongside Q15 wire. No default plaintext SQLite/raw-key persistence added. Tests cover provider unavailable/restart intent/replaywindow/keyring outage vs proven ended, returned credential scope, typed auth failure/transport Unknown and new-session context fencing. Demo automatic resume keeps its existing anonymous behavior.


## Реализация SDK по interfaces и platform adapters

Пользователь задал конкретное направление: neutral interfaces в Sdk.Client.DotNet, реализации внешних dependencies в Http/Store.Sqlite/Unity; Unity HTTP — UnityWebRequest. DotNet также владеет общей sync/protocol/hash/codec/recovery реализацией, чтобы adapters не копировали её. Owning specifications: [F19 interfaces/components](../features/F19-unity-client.md#53-интерфейсы-dotnet-и-конкретные-реализаторы), [models/SQLite](../features/F19-unity-client.md#66-платформенно-независимые-transport-и-adapter-models), [optional F23 adapter](../features/F23-unity-addressables.md#51-конкретные-interfacesadapters-optional-пакета), consumer F20.

IContentManagerClient/IHttpTransport/IHttpTransportResponse/IClientStateStore/clock/logger — neutral DotNet contracts. ContentManagerClient/SyncProtocolClient/Coordinator/NDJSON reader/integrity/typed codec остаются portable DotNet. HttpClientTransport implements transport через HttpClient ResponseHeadersRead; UnityWebRequestTransport через DownloadHandlerScript/owned bounded queue/async body stream/main-thread Abort+Dispose. SqliteClientStateStore implements store через private tables/short transactions/native worker/OS scope gate, не через shared CM EF.

Один response owner, headers-before-body-end, one-reader byte stream/terminal completion, no full sync buffer, nonblocking frame и complete record checkpoint описаны нормативно F19. Reused callback array копируется; queue overflow — explicit transfer failure, no fake pause/unbounded growth. Retry/original intent/Status recovery у DotNet, no transport-level new ID. Factory/consumer dependencies lifetime явный, Unity facade только delegates; numeric buffers/timeouts/native deps и public namespace/code literals закрепляются fixtures до реализации.

Unity-specific dispatcher/texture и Addressables mapping/runtime interfaces остаются в Unity/add-on assemblies. F23 конкретный AddressablesRuntimeAdapter/operation ownership описан, но stage/public API/manifest/concurrent waiter/Preload/remote switch/prefab policies продолжают требовать отдельного Q29 решения.

Actual Unity 2022.3.62f3 Windows x64/IL2CPP early headers/GetResponseHeaders, bounded managed/native memory, overflow/Abort/quiescent cleanup и SQLite/AOT tests остаются обязательным gate Q32/Q33. HTTP/1.1 явно разрешён для Demo; whole-response buffering или подмена выбранного UnityWebRequest на HttpClient не разрешены. Реализация/test evidence указаны выше; actual Unity Player compatibility не подтверждена.


## Принятое уточнение имени и основания SDK


[Документ от 30.09.2026](../../../archive/sdk-contracts-architecture-update.md) явно переименовывает `Sdk.Client.Core` в `Sdk.Client.DotNet`. Это закрытое решение, не новый вопрос. Все принятые ответы про оркестрацию, состояние, assets, progress/cancellation, retries, resume и preflight сохраняются под новым именем.


SDK является общей реализацией для .NET/C#-клиентов, а универсальными контрактами остаются OpenAPI, JSON Schema и Schema API. Godot C# может иметь адаптер над DotNet; для GDScript и других не-.NET-языков возможны самостоятельные SDK. Их создание не добавляется в текущий scope. Открыты прежние конкретные API/storage/packaging детали, а не само переименование.


## Принятые ответы блока 4


Источник: ответы пользователя и документы [SDK](../../../archive/unity-client-sdk-architecture.md), [Schema Registry / Code Generation](../../../archive/entity-schema-codegen-architecture.md). F19 — самостоятельная библиотека, F20 — отдельная сцена. Они не объединяются обратно.


| № | Вопрос | Ответ |
| --- | --- | --- |
| 1 | Объём одного вызова | Обязателен сценарий «вызвал sync → SDK получил план, скачал необходимое, обновил локальный набор → приложение использует результат». Нужна многослойная расширяемая архитектура и простой привычный Unity experience |
| 2 | Вид данных | Типизированные C# модели генерируются по выбранным JSON Schema через Registry/codegen, не эвристически из экземпляров. Runtime использует уже созданные модели; генерация UI/поведения не подразумевается |
| 3 | Кто хранит selections/inventory | Клиент хранит полный selections/actual inventory. SDK поставляет store interface и готовый Demo adapter, host может заменить; SQLite first candidate до native/AOT/platform spike. Atomic stage+checkpoint и activate+inventory+receipt обязательны; exact storage/API details engineering freeze |
| 4 | Ресурсы | Formal AssetReference descriptor фиксируется при Publish в immutable snapshot/hash; Delivery/SDK не обогащают его live metadata. URL не serialized; transient catalog/UI metadata F05 и производный CM URL допустимы. Binary API отдельно. Disposable image: SDK owns до передачи, consumer после; SDK cleans cancelled/late-unused output через main-thread dispatcher. Exact signatures/reuse/cache ещё engineering details; Addressables отдельно |
| 5 | Управление | Прогресс и отмена обязательны. Для конкурентного активного дубля synchronizationId в блоке 5 выбран 409; в блоке 6 выбран отказ второму прикладному вызову одного набора до создания нового ID. |
| 6 | Повторы | В SDK нужны default и настройка потребителем. Bounded transient retries/backoff/jitter только в safe boundary приняты; выбранные attempts/delays/budgets зафиксированы F19 §5.5; actual Unity/release conformance остаётся открытым. Validation/hash/lifecycle/confirmed invalidation не auto-retry; timeout/409 не fresh-ID fallback |
| 7 | Среда | Целевая линия **Unity 2022.3.62f3**. Первый target Windows x64/IL2CPP выбран; provider/settings и real conformance ещё Q32. DotNet target netstandard2.1 |
| 8 | Поставка | Unity packages через self-hosted GitLab NPM Package Registry, с обязательной проверкой фактической UPM-совместимости. DotNet/Http/Unity/optional Addressables разделены; подробности установки вынесены Q32 |


## Что изменилось относительно прежнего текста


Формулировки «не предполагаем генерацию классов» относились к прежнему вопросу, не к новому решению. Пользователь принёс отдельную архитектуру codegen; F21/F22 включены в аналитику, UI → ZIP `.g.cs` включено в Demo. Генерация поведения приложения и сложная runtime-совместимость всё ещё не приняты.


Auth в общей архитектуре SDK — дальнейшая возможность, не скрытое условие Demo. Private registry auth относится к получению пакета разработчиком, не к входу runtime-клиента в CM. В Demo по-прежнему нет лицензирования, шины и VR School.


Момент синхронизации выбирает приложение. Постоянная БД сервера не зависит от перезапуска процесса. Эти вопросы закрыты и повторно не задаются.


## Оставшиеся вопросы по библиотеке


| Тема | Незакрытая часть | Что уже не пересматриваем |
| --- | --- | --- |
| Клиентское состояние | Actual Unity native/AOT/Player proof, unsupported-format migration и release conformance Q32 | Scope/source/partition, operations/table ownership/atomic stage+activation/unknown commit/store format selected F19 |
| Прогресс | Literal stage codes/dispatch mapping | Immutable SyncProgress stage/handle/group/count/optional bytes; percent only known; Unity main-thread callbacks |
| Cancellation | Atomic-outcome/early control reconciliation and platform lifetime tests | Main cancellation Stop/Stopped, separate handle-based controls; unknown commit delays cleanup, installed/receipts retained |
| Retry defaults | Sizing иных workloads и actual Player observations; текущие defaults выбраны F19 §5.5 | Default с override, bounded transient safe retry; validation/hash/lifecycle/confirmed invalidation без auto-retry; timeout/409 не new-ID fallback |
| Конкурентность | OS lock/canonical path/native actual process tests Q32 | Stable store scope before ID; process-held lock separate SQL, stale handles/local run fenced, no silent retarget |
| Raw resource ownership | Actual transport/Player/lifecycle proof, numeric caps/literal codes | Raw/HEAD/file/Range/open Completion and texture handles/signatures/no cache selected F19; before-handoff SDK owner, after-Ready consumer owner |
| Публичная модель | Literal errors/stages/page codec, actual API/serializer/store conformance | Register<T>/SynchronizeAsync/state/control/read signatures and models once F19; exact binding Q31 preserved |
| Unity 2022.3.62f3 | Actual stripping/native/provider/dependency lock and Player proof Q32 | First required Windows x64/IL2CPP selected; Editor success not Player/AOT proof, other targets separate verified rows |
| Streaming transport spike | Actual Unity generation headers/incremental callbacks/stalls/cancellation/proxy/native memory matrix; client limits выбраны | NDJSON framing/variants/checkpoint/receipt gate и execution/control profile приняты Q33; expected pause приостанавливает только read-idle, stage/server deadlines остаются; Demo compression-off/no heartbeat; HTTP/1.1 allowed, reconnect сохраняет context |
| Optional Addressables | Этап первой реализации, package version, mapping lifecycle, handle/instance lifetime | Add-on независим, не обязательная dependency Demo |


**Ответ:** SDK public/store contract selected выше/F19; открыты engineering results/actual raw resource conformance/Addressables и actual platform/transport/distribution checks, без повторного выбора approved signatures.


## Применение


Требования приняты в [F19](../features/F19-unity-client.md), [F13](../features/F13-content-delivery.md), [F23](../features/F23-unity-addressables.md) и картах M05. Общий sync wire-контракт — [Q30](../../backend/questions/Q30-content-synchronization.md). Codegen/profile JSON Schema — [Q31](../../backend/questions/Q31-schema-code-generation.md), packages/spike — [Q32](Q32-sdk-package-distribution.md). [Этапы](../../../milestones/README.md) отделяют Demo от дальнейшего tooling.


## Ответы блока 5, влияющие на библиотеку


Полный клиентский реестр синхронизируется целиком; per-call частичной области нет. При отказе от версии схемы удалить её локальные представления. Реестр сейчас явно поддерживает разработчик; будущий файл назван `content-manager.json`, ранее в источнике `content-schemas.json`; точный общий формат/имя уточняются, автоматизация после Demo.


Выбран раздельный обмен: план → точные данные. Минимальная атомарная единица применения — весь набор одной точной schema/version. Допускается результат, в котором некоторые группы применены, другие ещё нет; внутри одной группы частичное новое рабочее состояние запрещено.


Sync заканчивается на данных с `AssetReference` identities/descriptors; тяжёлые bytes не загружаются автоматически. Изображение/аудио/видео получается отдельным API приложения и имеет отдельный результат. Runtime DotNet не обращается напрямую к S3. Addressables не становится обязательной зависимостью обычного image loader.


synchronizationId создаётся и управляется **Sdk.Client.DotNet**, не обычным прикладным вызовом. Приняты 409 конкурентного активного дубля и необходимость продолжения по прежнему ID/checkpoint с возможностью нового запуска при несовпадении. Архитектура состояния/transport подробно вынесена [Q33](../../backend/questions/Q33-sync-resume-transport.md); [Q30](../../backend/questions/Q30-content-synchronization.md) сохраняет ответы. Дублировать незакрытые решения вторым самостоятельным протоколом в SDK нельзя.


Прогресс/retry/отмена остаются обязательными; дефолтные числа попыток не выбраны. Просьба DotNet предложить продолжение не вводит обязательный UI в платформенно-независимый слой. Восстановление после закрытия процесса обязательно: нужны durable checkpoints и сохранённые полные validated records. Default — автоматическое продолжение прежнего ID при совместимости selections/installed/staging и доступном plan либо новый plan от actual inventory при доказанной невозможности. Timeout сам по себе не доказывает невозможность resume.


Start/Data/Commit/Status закреплены [Public wire v1](../../backend/questions/Q33-sync-resume-transport.md#public-wire-contract-v1): control JSON success в существующем `ApiResponse<T>`, Data raw application/x-ndjson UTF-8/LF с camelCase protocolVersion=1, kind/payload/cursor и recordSequence. Start metadata содержит changed groups; no-change groups:[] даёт только operationCompleted и zero receipts. contentJson — JSON string: unescape → exact UTF-8 → profile/hash verification → typed parse, без parse→reserialize. Normal sequences 1..N неизменны; cursor candidate+validated full record сохраняются атомарно, SDK cursor opaque/server HMAC. Replay сравнивает identity/full parsed payload/restored content bytes. operationInvalidated terminal sequence=0/no cursor вне planned sequence. Commit body planId/targetGroupHash/targetGroupHashProfile, schemaVersionId route; same ACK и lostACK/Status repeat без local reapply. Status разделяет planState/executionState и accepted receipts; completed не installed proof. 404 unknown/restart loss отличается от 410 known bounded tombstone, outage остаётся Unknown. Полный успех требует valid terminal/all local commits+ACKs/correct HTTP end. Модели/payloads определены backend F13, не повторяются в SDK. [Execution/control/Freshness profile](../../backend/questions/Q33-sync-resume-transport.md#execution-control-и-transport-profile) согласован; signatures/adapters/client defaults материализованы. Generic concurrency/HTTP/store evidence отделено от actual Unity Player gate Q32; HTTP/1.1 разрешён, reconnect сохраняет context.


## Новые принятые границы: блок 6


CM хранит plan в bounded process memory одного host. TTL default 1 час absolute от atomic reservation без продления preparing/streaming/stopped, настраиваемый backend. Active plans не вытесняются произвольно ради capacity нового Start. Restart теряет plans; SDK строит новый от actual inventory, committed данные не стираются. Mutable execution/lease/generation отдельно от immutable target/hash/records/cursor; Data получает own lease/generation, expired owner может быть заменён, late producer fenced. Это не TTL локальных installed данных.


Stop закрывает reading/transport, сохраняет plan/checkpointed staging/installed группы; server revoke переводит execution в stopped. Начатый короткий atomic commit может завершиться и сохранить receipt; его POST отложен до разрешённого Resume. Discard сначала останавливает execution и выясняет atomic outcome: Unknown postpones cleanup. Затем удаляет только unfinished context/staging, сохраняет installed/inventory/committed receipts; bounded server discarded tombstone препятствует resurrection. Withdrawal/safety invalidation не разрешает commit incomplete affected group либо latest substitution; previously committed сохраняются, новый plan от actual inventory.


После полного успеха, включая no-change, SDK делает POST freshness с полным selection/actual installedInventory. Сервер возвращает outcome upToDate/changesAvailable, checkedAt UTC и sorted exact changedGroups; plan/lease не создаётся. Authority/transport failure даёт SDK Unknown, не отменяет completed sync и не запускает auto-sync. Приложение выбирает следующий plan. Установлено и актуально — отдельные состояния; SyncResult/SyncFreshness/SyncProgress и handle/read/store contract выбран F19, literal stage/error registry и actual callback/platform conformance materialize. Технический контракт один: backend F13/DF09 и Q33, без второй копии DTO в SDK.


## Public SDK и локальный store: согласованный контракт

**Ответ:** Пользователь согласовал все 24 пункта. Это target public/store contract; наличие опубликованного SDK/native adapter/Player conformance не доказано. Единственный owner SDK моделей, signatures и workflows — [F19](../features/F19-unity-client.md); HTTP models принадлежат F13, literal transport Q33 неизменен.

1. **High-level API.** SynchronizeAsync получает full exact selection, CancellationToken и optional progress; inventory SDK читает из store. Caller не передаёт ID/cursor/generation/готовый inventory.
2. **Automatic Resume.** Пригодная durable попытка восстанавливается следующим SynchronizeAsync по Q33. Отдельный обязательный public ResumeAsync отсутствует.
3. **Selection / bindings.** Binding registry определяет supported exact models, selection — desired managed groups. Register не добавляет группу в selection. Demo требует supported binding каждой selected version.
4. **Explicit registration.** Register<T>() читает ContentSchema metadata exact root type; no assembly scanning. Two CLR types bound same exact schema version — configuration conflict.
5. **SyncResult.** Outcome Completed/Stopped/Failed/AlreadyRunning, actual committed groups, unfinished attempt, typed error. Independent freshness NotChecked/UpToDate/ChangesAvailable/Unknown; success plus failed freshness = Completed+Unknown.
6. **Expected failures.** HTTP/protocol/integrity/storage failure — typed result, preserving committed groups. Exceptions for API misuse/unexpected defects; no false successful empty result.
7. **Cancellation.** Cancellation основного вызова означает Stop/recoverable state. Begun atomic commit разрешается до known outcome; Stopped carries actual committed groups. Discard explicit separate action.
8. **Attempt handle.** Opaque SynchronizationAttemptHandle in state/progress/result; StopAsync(handle)/DiscardAsync(handle). Scope/execution binding prevents old intent retarget; after restart fresh handle through state read.
9. **Progress.** Immutable stage/attempt/exact-group/count/bytes snapshots; optional total/percent only known. Unity facade main-thread dispatcher, DotNet does not promise Unity thread.
10. **Committed typed read.** GetByKeyAsync<T>/GetByIdAsync<T>/GetPageAsync<T>; exact type binding determines group, no latest. NotFound distinct from storage/parse failure.
11. **Read wrapper.** CommittedEntity<T> carries Value, exact schema/instance/version IDs, Key, ContentHash/profile; metadata outside generated content.
12. **Detached values.** Returned Value separate deserialized instance; caller mutation does not change store bytes/inventory/hash. No Demo synchronized-content edit API.
13. **Read consistency.** Each lookup/page observes committed group snapshot. Page continuation bound group hash/profile; group change reports changed snapshot/restart, no whole-selection atomicity.
14. **Store scope.** One managed store bound explicit CM source and application partition saved at creation. Opening same store with different binding refuses; source datasets cannot merge.
15. **Operation port.** Store port exposes recovery/inventory read, attempt create, atomic record+cursor stage, staged-group read, atomic activation, receipt reconciliation, unfinished cleanup. No arbitrary CRUD/HTTP DTO/SQL connection export.
16. **Stored bytes.** Exact canonical payload bytes/profile/digest/identities retained together. Stage verified record+cursor; generated object reserialization never persisted content authority.
17. **Atomic activation.** Short transaction rechecks base group, applies upserts/removals and index/inventory/local receipt atomically. Identical committed result original receipt, conflicting basis no mutation.
18. **Unknown commit.** Reconcile durable receipt identity sync/plan/exact group/target hash+profile and installed group; contradiction storage integrity failure, no blind apply/cleanup.
19. **Ready adapter.** SQLite first ready candidate after platform spike, replaceable via port. In-memory test adapter not restart recovery. Provider/native packages Q32.
20. **SQLite structure.** Store metadata, attempt context/selection, staged records, active representations, local receipts; lookup index derived active rows, no independent unsynchronized authoritative inventory. Assets/resources excluded.
21. **High-level lock.** Whole sync scope lock independent short SQL transactions. Process-held OS lock for canonical store path, crash releases. Unsupported guarantee means platform adapter not supported; transaction lock not whole-run gate.
22. **Threads.** Blocking storage operations off Unity main thread; Async API alone not nonblocking guarantee. Main-thread callbacks separate dispatcher.
23. **Store format.** Version separate SDK SemVer/protocol/content profiles; supported migration atomic, unsupported newer format refuses. Open/migration failure never auto-delete database.
24. **Conformance.** Shared adapter suite: stage/cursor, activation, receipt replay/unknown commit, SDK-instance/process concurrency, crash/reopen, stale handle, Discard installed preservation. SQLite real files/process termination; doubles no crash proof.

Physical package/provider/native set, exact binding/page codec и client defaults уже выбраны F19; actual Unity patch-platform-Player и release conformance остаются Q32. Raw assets/image contract согласован ниже; optional Addressables остаётся отдельным вопросом. [Microsoft.Data.Sqlite async limitations](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async), [SQLite atomic commit](https://www.sqlite.org/atomiccommit.html) объясняют motivation; документация платформы не является proof нашего adapter.

## F19: публичный SDK, typed store и устойчивое состояние


[Клиентская F19](../features/F19-unity-client.md) раскрывает принятую оркестрацию по слоям SDK. Отдельного server aggregate/DbContext/DTO F19 нет: wire models и handlers принадлежат [backend F13](../../backend/features/F13-content-delivery.md), Schema Registry/codegen — F21/F22. Unity-конфигурация и callback не создают CM admin screen; F20 является отдельным потребителем публичного API.


**Принято:** public SDK/store contract выше; signatures/models принадлежат F19 §5.1/§6.2/§6.4. Register<T>() explicit binding, independent selection, detached reads, Task<SyncResult> и handle controls. Literal diagnostic/stage codes/native/provider/runtime conformance materialize. Historical/Demo generated `cm-csharp/1` profile, Newtonsoft.Json и exact root binding `ContentSchema(SchemaId, SchemaVersionId, VersionNumber)` закрыты Q31 и не выбираются повторно здесь. [Q34 DateTime MVP](../../../business/questions/Q34-schema-field-semantics.md#datetime-mvp-temporal-semantics-и-совместимость) закрывает Offset semantics: old Naive exact versions остаются DateTime Kind=Unspecified, new Offset — DateTimeOffset/UTC 7digits Z, old/new generated bindings могут жить в одном project. SDK parsing выбирается по exact supported profile+binding без implicit host timezone; unknown profile отклоняется до group apply. Новый combined Q35/Offset profile set требует отдельного whole codec/carriers/golden bytes Q19/Q31; whole new-profile codecs/literal temporal fixtures и фактическая AOT/Player binding implementation требуют проверки. Названия интерфейсов в F19 задают ответственность, а не наличие опубликованной сборки. netstandard2.1 — целевой профиль из архитектуры, совместимость зависимостей/Player/AOT ещё нужно проверить по Q32.


EntityInstanceKey — stable per-schema alias. GetByKeyAsync<T>/GetByIdAsync<T>/GetPageAsync<T> use explicitly registered root T exact binding; no latest/assembly scanning/Name identity. Same version conflicting CLR types reject config. CommittedEntity<T> detached; caller edits do not change store. Page context bound group hash/profile; changed snapshot explicit read outcome. Models once F19, no duplicated field tables.


Storage adapter устойчиво хранит request fingerprint, ID/plan, full validated records и candidate cursor/checkpoint атомарно. После groupCompleted сверяет plan/groupStarted/counts, baseGroupHash/profile исходной local group и full target membership/hash, включая unchanged members; затем атомарно активирует data/removals/index/inventory/local receipt. Counts не заменяют membership; incomplete группа остаётся прежней. Receipt server identity planId/schemaVersionId/targetGroupHash/profile: same idempotent, mismatch conflict. Потерянный ACK выясняется по durable receipt/Status без повторного apply; server gate разрешает next group только после receipt. Invalidation/truncation после headers не дают full success.


IClientStateStore operation contract/source-partition scope/format and SQLite table responsibilities agreed F19 §6.4. Whole-run process-held OS lock distinct short SQL transactions, crash releases; unsupported guarantee means unsupported adapter. Replaceable ready SQLite candidate requires native/AOT/platform/provider/lock/durability spike Q32; in-memory cannot prove recovery. Actual schema/SQL/numeric settings materialize, no arbitrary CRUD public port.

SDK захватывает server executionGeneration из Status и Data header X-CM-Sync-Execution-Generation, не из NDJSON/cursor. Control Resume/Stop/Discard использует planId/captured expectedExecutionGeneration, Resume также originalRequestFingerprint/profile из durable context. Stale control409 не retarget-ится на свежую generation. Lost control ACK повторяет original action/planId/expected generation/meaningful input и возвращает original result snapshot; отдельный Status выясняет current state. Original result не fresh Status и не повод local reapply.

PlanId выделен при reservation, Status preparing доступен без ready metadata. Resume разрешает закончить первую подготовку по original input, если ready plan ещё не существовал; ready immutable target не recapture-ится. Resume проверяет identity/eligibility, разрешает stopped/idle, competing live worker409; stream/lease не резервирует. Следующий Data GET получает собственную generation/lease. Durable recovery order: context/receipts → Status → pending server receipt repeats без local reapply → Data checkpoint; stopped execution требует разрешённый Resume до отложенного POST. Own commits не меняют original fingerprint. Retained completed plan допускает terminal suffix replay/dedup при unknown HTTP end. NewID только proven unavailable/expiry/discard/invalidation либо explicit new selection intent, не timeout/409/unknown outcome; actual inventory авторитетен, staging reuse требует exact integrity checks.

Раздельны unary/header/read-idle/record-assembly/storage-validation-commit/receipt budgets; HttpClient ResponseHeadersRead timeout заканчивается на headers, body ограничивается отдельно. Во время ожидаемых local validation/atomic commit/receipt POST read-idle приостановлен, соответствующий stage deadline и server watchdog остаются. Heartbeat/empty NDJSON lines отсутствуют; первая Demo HTTP compression выключена, enable только после actual incremental checks. Bounded transient retries/backoff/jitter применимы в safe boundary; corruption/validation/lifecycle/confirmed invalidation без auto retry/fresh-ID fallback. Exact settings/signatures/numeric defaults и Unity transport matrix требуют Q29/Q32 spike; server config/lease/preparation/write-backpressure deadlines — backend F13/Q33.


Результаты data apply, freshness и image загрузки различны; их public SDK types не выводятся из server enum без mapping. Main-thread dispatcher доставляет Unity callbacks и lifecycle cleanup с local run guard. Image helper возвращает disposable owned resource: SDK до передачи, consumer после; cancelled/late-unused output SDK освобождает. Demo raw signatures/no SDK cache/disposable handles выбраны в разделе Raw assets; numeric defaults/adapters/conformance ещё материализуются.


Current source evidence: SDK implementation находится в contentmanager.sdk.client и contentmanager.sdk.client.unity; API содержит server F13, не SDK runtime. Generic tests и actual CM/PostgreSQL/MinIO SDK E2E отделены от ещё не выполненных Unity managed compile/Editor/Player и GitLab package-install gates. Точные results представлены в [плане реализации](../../backend/f19-demo-implementation-plan.md).


## Raw assets и Unity image helper: согласованный контракт

**Ответ:** Согласованы все 24 пункта. Это target SDK/resource contract; working gateway/Player/helper/physical cancellation не доказаны. Models/signatures/layers/flows — [F19](../features/F19-unity-client.md), scene consumer [F20](../features/F20-unity-demo-scene.md), gateway [Q21](../../backend/questions/Q21-media-storage.md). Frozen five-field descriptor остаётся у F04; no new media identity/profile/URL property.

1. **Raw API.** DotNet OpenReadAsync/DownloadAsync(destination)/DownloadToFileAsync; Unity helper uses same port, no own HTTP/download layer.
2. **Request.** AssetReadRequest has AssetId, optional expected frozen five-field descriptor, optional single range context. Descriptor owner F04 unchanged.
3. **Expected integrity.** Expected AssetId matches request; full bytes actual size/SHA-256 verified, available Content-Type/Length compared. Mismatch typed integrity failure.
4. **ID-only read.** Allowed raw AssetId request without expected descriptor; result explicitly reports no Published-snapshot verification.
5. **HEAD.** Optional GetReadMetadataAsync: Content-Type/nullable Content-Length/ETag/Last-Modified/range facts. Transport metadata, no catalog read/frozen descriptor enrichment.
6. **Open handle.** Owned AssetReadHandle holds response/readable Stream/metadata/Completion. Opened means accepted headers, not complete download.
7. **Transfer completion.** Correct HTTP end plus required size/hash checks before full success. Early Dispose incomplete, not Verified.
8. **Stream failures.** Stream keeps standard ReadAsync exception behavior; Completion records typed transfer failure. Download/image methods map expected errors to SDK result.
9. **Ownership.** Idempotent handle Dispose closes stream/response and cancels remaining read; SDK cleans failed open. Actual upstream cancellation separately proven.
10. **Original bytes.** Accept-Encoding identity; unexpected Content-Encoding rejected. Raw files streaming/bounded buffers, no mandatory whole byte array.
11. **Caller destination.** DownloadAsync writes current position/leaves destination open; no hidden Seek/Truncate. Failure reports actual written bytes, partial output possible.
12. **File commit.** Owned same-directory temp file, complete validation before move; existing target not overwritten by default. On failure only own temp removed.
13. **Retry.** No automatic retry appending arbitrary stream after first write. Before write or clean owned-temp file workflow only; no duplicate output.
14. **Range.** Explicit one byte range; no multipart Demo SDK. Continuation If-Range uses prior strong response ETag, not instance hash/S3 multipart validator.
15. **206/200.** Validate Content-Range/actual segment length; 200 instead expected206 -> RangeNotHonored before destination write, no append full body to partial.
16. **Partial integrity.** Partial bytes not full SHA proof; full verification after complete assembly. No automatic persistent assets resume, no data cursor reuse.
17. **Texture API.** LoadTextureAsync frozen Image AssetReference/cancellation/progress -> typed outcome plus owned UnityTextureHandle. DotNet no UnityEngine types.
18. **Formats.** Demo JPEG/PNG only; kind/full size/hash/format verified before successful resource transfer. Unsupported format typed, decoder capabilities do not extend policy.
19. **Decode.** ImageConversion.LoadImage after byte verification; bounded complete image byte[] allowed, raw API remains streaming.
20. **Image limits.** Config max encoded bytes/width/height/decoded pixels; inspect JPEG/PNG headers before large texture allocation. Demo sRGB/no mipmaps/no retained CPU-readable copy; numbers from representative fixtures.
21. **Unity cancellation.** Create/decode/Destroy main thread dispatcher; cancel before decode, synchronous decode not physically interrupted by CancellationToken. Cancellation winning before handoff SDK cleans created texture.
22. **Texture owner.** Successful handoff -> application owner. Idempotent Dispose invalidates handle and schedules main-thread Destroy; Texture after Dispose misuse; no instant GPU memory promise.
23. **Cache/errors.** Demo no extra SDK resource cache/shared dedup; independent resource/owner per operation. Gateway cache separate. Typed transport/access/notfound/range/integrity/decode/cancel; bounded non-JSON error, redirects rejected by default.
24. **Acceptance.** Real CM/YARP/MinIO/Unity Player: streaming/truncated/hash-size/earlyDispose/partialdestination/200vs206/416/cancel before-after decode/late scene success/doubleDispose, lifecycle/leak verification. Docs/doubles not runtime proof.

Literal diagnostic/stage codes, transport/provider adapters, numeric buffers/timeouts/image limits, exact pinned Player/platform dependencies and real conformance remain engineering Q15/Q32. Audio/Video playback helpers, persistent asset resume/resource cache after Demo now F19 §6.8/Q37; optional Addressables F23. [Unity LoadImage](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/ImageConversion.LoadImage.html) specifies byte[] decode boundary; documentation is not our adapter test.

## F19: CM-owned media gateway и resource contract


По [принятому решению S3/YARP](../../../archive/content-manager-s3-yarp-media-delivery-decision.md) upload выполняется CM API/Application/IObjectStorage → S3-compatible storage; metadata/lifecycle также остаются CM API. Обычный read для browser/Unity/.NET: CM-owned URL → CM host / read-only YARP → private S3-compatible storage. Public models/SDK не получают bucket/ObjectKey/provider endpoint/storage credentials и обязательный прямой/presigned S3 URL. Client CM credentials не являются S3 credentials. Server-side layout детерминирован от AssetId, без authoritative FileName или random opaque key, который невозможно отобразить static route. Это целевой contract, не утверждение о реализованном gateway.


Q21 теперь фиксирует Demo media boundary: MinIO за `IObjectStorage`, один AssetId Guid без независимого ObjectKey identity, canonical `/storage/assets/{assetId}`, SHA-256 content ETag, configurable Cache-Control с Demo default 24h, conditional304 и Range206/416 для Audio/Video. Открыты только low-level recovery/idempotency/provider-operational details и MVP access integration; они не меняют public media semantics. Cache body limit не download limit и не связан с default 1 час хранения sync plan Q33.


**Уже определено для клиента:**


- GET/HEAD gateway не принимает upload/delete; SDK не вводит второй storage protocol.
- Content-Type и Content-Length доступны, когда их возвращает provider; FileName extension не единственный источник MIME.
- Public gateway ETag — SHA-256 binary content validator по Q21, не metadata ETag/internal Version. Upstream provider ETag не считается SHA-256 автоматически и не подменяет согласованный public validator.
- HTTP 401/403, 404, 416 и 5xx остаются различимыми failures; raw gateway/provider error body может быть non-JSON. Нет ApiResponse<byte[]> либо successful file/image из body ошибки.
- MVP CM authorization/entitlement, session/revision membership и lifecycle/consumer context выполняются до proxy/cache/304/Range shortcuts. Archive сохраняет historical bytes, но eligibility определяет CM context. Demo остаётся без auth.
- Asset transport/result и stream/raw resource ownership принадлежат F19. Отмена downstream request и прекращение upstream передачи проверяются отдельно; EOF binary body не sync checkpoint/group receipt/local atomic data commit и не decoded ready image.


**Согласованный клиентский контракт и оставшаяся проверка:**


| Область | Нужное SDK решение / проверка |
| --- | --- |
| CM URL binding | Configured CM source/context + canonical route, captured per operation; no private topology/redirect by default; models/signatures F19 |
| Response/header boundary | GetReadMetadataAsync HEAD; OpenReadAsync owned handle/Completion; Download destination/File contracts selected F19; raw bounded, optional headers/no false total |
| Error mapping | Typed resource outcomes and SdkError categories chosen F19; literal registry mapping/actual safe bounded non-JSON diagnostics remain implementation |
| Range/conditional use | Single explicit range/strong prior If-Range, validate206/count/Content-Range,200->RangeNotHonored before writes,416 typed; no multipart/persistent resume/body cache; actual conformance Q32 |
| Cancellation/lifetime | Idempotent raw handle Dispose/Completion; texture main-thread atomic handoff/dispose/destroy/no CPU copy selected F19; physical cancel/leak/lifecycle proof real transport/Player |
| Gateway integration | Реальный CM/private provider и Unity/.NET transport: GET/HEAD/headers, streaming большого файла вне output cache, cancellation, non-JSON failures, actual Range/conditional/cache cases; MVP запрет до cache/proxy |


SDK retry/resource cache и transport signatures не выбираются из примера server OutputCache. Gateway cache не заменяет local resource ownership или устойчивое data-sync состояние. Дополнительный presigned read требует отдельного будущего архитектурного решения. Public models с неизвестными types/fields не создаются ради заполнения описания.


Затронуты [F05](../features/F05-media-assets.md), [F13](../features/F13-content-delivery.md), [F19](../features/F19-unity-client.md), [F20](../features/F20-unity-demo-scene.md) и [M02](../modules/M02-media-library.md). Runtime/gateway/provider/Unity проверки в этой документационной задаче не выполнялись.


## F20: presentation lifecycle и raw resource ownership


[Сцена F20](../features/F20-unity-demo-scene.md) использует публичную F19 без второго sync/HTTP/storage/retry слоя. Тема/controls принадлежат Q26; SDK signatures, callbacks и ресурсные гарантии остаются здесь. Общие server wire DTO принадлежат F13, generated data models — F21/F22.


Приняты F19 §5.2/§6.5/§7.4 LoadTextureAsync/UnityTextureResult/owned handle, full expected SHA/size+JPEG/PNG limits before ready decode, Demo no SDK cache/dedup. Main-thread create/decode/destroy и atomic handoff: SDK cleans before transfer, consumer disposes after Ready even if scene context obsolete. Double Dispose idempotent/after-dispose access misuse; physical GPU/network cancellation not claimed. Scene uses SDK contract, own presentation guard not second resource policy.


Изображение привязывается к текущему presentation context, exact model representation, EntityInstanceVersionId показанного committed снимка и AssetId. Index списка/один InstanceId не гарантируют актуальность callback после обновления или смены структуры. Сцена не считает закрытие/отмену доказательством rollback: начатый atomic SDK commit может завершиться с durable receipt/отложенным POST; следующий показ читает actual committed store. Stop сохраняет resume; Discard сначала выясняет atomic outcome, unknown откладывает cleanup, затем очищает лишь unfinished, сохраняет installed/inventory/committed receipts. Выход сам не является новым выбором Discard; SDK local run generation дополняет presentation guard сцены.


Презентационные tests с SDK double проверяют delegation и late-response guards, а не persistence/transport/resource release. Для claims о lifecycle требуются реальные Unity helper/ресурс в согласованной ownership matrix; crash/ACK и durable resume проверяются у F19. Runtime/Player/network проверки в этой аналитике не запускались.


## F23: mapping snapshot и ownership операций

Этот раздел уточняет прежде всего native Unity-built mapping/catalog/AssetBundle ветку. Новые CM custom entity/raw-file providers описаны [F23 §5.2](../features/F23-unity-addressables.md#52-cm-entity-и-asset-providers-после-demo); их scoped key/type API, общий download/cache manager и file leases имеют owner [Q37](Q37-launcher-asset-delivery.md). Общий raw-file single-flight уже выбран, его нельзя считать повторно незакрытым вопросом из native Concurrent loads ниже.


[F23](../features/F23-unity-addressables.md) раскрывает optional add-on над Unity 2022.3.62f3. F23 включена в Demo, выбран первый локальный объём; raw путь F19/F20 остаётся самостоятельным. Общий AssetId, HTTP и установленный inventory принадлежат F05/F13/F19. Серверный DbContext, mapping CRUD, Unity key в DTO, сборка bundles на CM и автоматический preload metadata не вводятся. Размещение по server-слоям отражено в [M05 backend](../../backend/modules/M05-delivery.md#f23-граница-optional-addressables).


Источник задаёт Load/explicit Preload/Release, mapping provider и управляемый handle. Имена IAddressableMappingProvider, AddressableAssetInfo, IAssetHandle и AddressablesClient обозначают ответственность. Пример public C# API и JSON manifest не утверждает окончательные signatures, nullable/requiredness, limits или конкретные значения версий. schemaVersion обозначает формат manifest; contentVersion обозначает набор mappings. Они не SchemaVersionId, не revision F10 и не версия SDK package.


**Уточняется:**


| Область | Нужное техническое решение / проверка |
| --- | --- |
| Первый выпуск | Этап add-on, local-only или также remote provider; обязательные asset kinds, Load/Preload и состав samples; F20 не получает обязательный Addressables UI автоматически |
| Public API | Exact AssetId C# type из F19, ограничения generic T, signatures/return/error types Load/Preload/Resolve/Release, sync/async provider, cancellation/progress contract |
| Manifest contract | Exact CLR/wire types schemaVersion/contentVersion/assets/generatedAt, обязательность, AssetId key encoding, duplicates, пустые/invalid keys, unknown members, unsupported format, limits/validation и место formal schema |
| Mapping info | Exact тип/requiredness Key и optional Version; смысл Version относительно contentVersion/catalog, возможные дополнительные сведения о target type/dependencies. Поле из примера не становится обязательным |
| Active snapshot | Когда операция фиксирует mapping/content context; ownership immutable view и изменение provider во время Resolve/Load/Preload; несколько AssetId не должны незаметно получать случайную смесь наборов |
| Catalog binding | Каким способом mapping contentVersion связывается с нужным catalog/bundles; кто подтверждает готовность, какая проверка предшествует использованию, ответ при mismatch. Само наличие URL/key не подтверждает доступность |
| Remote update | Источник/доставка manifest, catalog build/release identifiers, staged validation, точка переключения active context, сохранение предыдущего рабочего набора при неготовом кандидате, offline/rollback policy |
| Existing handles | Что происходит с уже выданными handles при обновлении provider/catalog; old/new resource coexistence, ownership и release. Свежий mapping не подменяет ресурс ранее выданного handle без явного контракта |
| Local provider | Место упаковки manifest в build/content, startup validation, поддержанные форматы и начальный failure result; наличие manifest offline не означает наличие всех bundles offline |
| Load ownership | Кто владеет operation до completion и ready resource после передачи; Dispose/Release идемпотентность/ошибка, Value после release, failed-operation cleanup, late success после ухода потребителя |
| Prefab instances | LoadAsset + application Instantiate либо отдельный supported instance API; кто удерживает dependencies до конца жизни instance, какие release/destroy методы допустимы и когда. Не выбирать по короткому using-примеру |
| Concurrent loads | Независимые/reference-counted claims или разделяемая operation, dedup scope по AssetId/type/content context, влияние отмены одного waiter на остальных и release последнего владельца |
| Explicit Preload | Warm downloaded cache или удержание loaded assets, expected types, duplicates/empty input, result по элементам, partial success/failure, lifecycle preload owner и передача/повторное использование claims через Load |
| Cancellation | Что отменяется до запуска и после старта; кто наблюдает/очищает продолженную operation, когда выдаётся результат и есть ли физическая остановка. CancellationToken обязательный async input, но не доказательство прекращения Addressables download |
| Cache boundaries | Native memory/bundle/mapping cache Q29; shared raw-file manager F19 §6.8/Q37. Нет второй raw cache внутри add-on; GC/file lease/quota Q37 |
| Thread / lifecycle | Required main-thread calls и dispatcher, delivery callbacks, shutdown/scene teardown/domain reload, app lifetime vs scene lifetime, release/Dispose с разных потоков |
| Failures / retry | Публичные stage/error codes и safe context, missing mapping/key/catalog/type mismatch/failure/cancel distinctions, числовые retry/timeout defaults и настройка, безопасный повтор без duplicate ownership |
| Access / integrity | Источник и допустимые transport/provider настройки для remote content, integrity/authentication при необходимости, expiration и редактирование sensitive URLs в diagnostics. Доступ к CM AssetId сам по себе не утверждает доступ к внешнему content pipeline |


До выбора политики обновления наличие remote manifest не доказывает корректный общий переход mapping/catalog/bundles. Новый набор, успешный CM data sync и готовность Unity resource — разные результаты; F13 sync commit не становится распределённой транзакцией с Addressables. Политика update не выводится из ETag/If-Match CM authoring; техническое именование contentVersion не выбирает SemVer или порядок сравнения.


Source evidence 05.10.2026: targeted API src .cs/.csproj вне bin/obj/Migrations и UI src .cs/.csproj/.razor/.json вне bin/obj не содержат target mapping provider, AddressableAssetInfo, IAssetHandle, AddressablesClient/add-on dependency. Проверка ограничена этими checkout/scope и не доказывает отсутствие пакета в иных репозиториях. Наличие документа не означает опубликованный UPM package, проверенный Release/cancellation или построенный catalog. Runtime/Player/Unity/content/registry проверки не запускались; матрица и поставка остаются [Q32](Q32-sdk-package-distribution.md#f23-матрица-поставки-add-on).

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

Обязательный **2022.3.62f3 Windows x64 IL2CPP Player** и реальный CM/PostgreSQL/MinIO sync через Unity transport ещё не проверены. Unity6 Editor не заменяет эти gates; F19 не объявлена завершённой. Backend/контейнеры/Player/коммиты не запускались в этой миграции.

## Реальный CM через Unity Editor

11.10.2026 подтверждён composition основного пакета: UnityWebRequest → portable coordinator → native SQLite и отдельная загрузка assets/image helper с реальным CM/PostgreSQL/MinIO. Два новых integration tests и полный PlayMode suite прошли (2/2 и 5/5 без skip). Две exact модели получены через F22; публичный namespace восстановлен на ContentManager.Sdk.Client.Unity по ответу владельца. Generic NuGet 1.0.0 и serialization/profile contracts сохранены. Сценарии, cleanup и пределы доказательства описаны в [F19](../features/F19-unity-client.md#проверка-unity-sdk-с-реальным-cm). Target Player/IL2CPP и interrupted-attempt recovery остаются открыты; Editor reflection не заменяет AOT acceptance.

## F23: выбранный локальный Demo

Ответ владельца 11.10.2026: локальный immutable mapping AssetId → Unity key, Load<T>, explicit Preload и управляемые handles. Remote catalog update и Launcher/shared-cache providers отложены. Public local API/ownership описаны в [F23 §5.1](../features/F23-unity-addressables.md#51-конкретные-interfacesadapters-optional-пакета), модели — §6; прежние открытые строки о native local Load/ownership теперь имеют этот ответ. Immutable snapshot копируется при initialization; независимые claims, Cancelled waiter с продолжающим cleanup owner, ready handle переживает client shutdown; preload сохраняет owned successful items при Partial. Prefab instance API отсутствует, load handle удерживается до Destroy instances. JSON manifest/schema/update/remote binding остаются незакрытыми future вопросами. Runtime evidence и Player boundary — [F23 §10.2](../features/F23-unity-addressables.md#102-фактическая-проверка-локального-demo).
