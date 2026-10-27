# M05. Права и доставка

## Demo: пакет и сцена Unity

[F19](../features/F19-unity-client.md) — самостоятельная Unity-библиотека поверх HTTP; отдельная [F20](../features/F20-unity-demo-scene.md) — тестовая сцена CM-команды для команды Unity. Сцена использует публичный интерфейс библиотеки; обратной зависимости нет. [F13](../features/F13-content-delivery.md) описывает передачу интересующих структур и текущего состояния, получение/применение серверной дельты. Selections/inventory хранит клиент; F19 оркестрирует полный sync и включает DotNet/Unity raw helpers. SDK предоставляет store interface и готовый Demo adapter; SQLite first candidate проходит native/AOT/platform spike, host может заменить. Disposable image resource имеет explicit owner и SDK cleanup cancelled/late output; exact signatures уточняются в [Q29](../questions/Q29-unity-client-package.md).

Сцена F20 — каталог экспонатов со списком по exact версиям, подробностями/image и раздельными data/freshness/resource results. uGUI/TextMeshPro/RawImage, Inspector configuration и explicit sync/Stop/Discard/image retry согласованы [Q26](../../../business/questions/Q26-demo-mvp-scope.md#f20-экран-и-границы-показа). SDK от UI сцены не зависит. Нет авторизации/ролей/F12/VR-интеграции в Demo; FSD/Blazor не навязываются Unity. [Этапы](../../../milestones/README.md), [Q30](../../backend/questions/Q30-content-synchronization.md).

SDK component/port implementation подробно в [F19 §5.3–5.5](../features/F19-unity-client.md#53-интерфейсы-dotnet-и-конкретные-реализаторы): DotNet interfaces + portable coordinator; HttpClientTransport, UnityWebRequestTransport и SqliteClientStateStore в adapter projects. Unity callbacks/owned queues/main-thread lifecycle, actual SQLite transactions и optional F23 runtime adapter не дублируют sync. Это target design; real Player/native/protocol tests ещё требуются.

## Полная модель MVP: назначение и целостный результат

Клиентский модуль получения контента относится к приложению-получателю, не к административному редактору. Общий результат: рабочий локальный состав переключается только после получения согласованного набора. Реальные клиентские приложения и их интерфейсы ещё уточняются.

Новая admission использует confirmed SourceCurrentPeriod лицензии и exact immutable PolicyAgreement, committed Publishing до VRS confirmation. Agreement фиксирует policy/provenance для planned License+Variant+SourcePeriod, не grant/entitlement и не editable клиентскую лицензию. VRS backend сохраняет stable plan/RequestId, получает quote вне своей transaction и подтверждает тот же plan с AgreementId/outbox; unknown response повторяет ту же identity. Unity/SDK и staff CM UI не вызывают issuance/confirmation. Pending CM content grant виден как waiting, даже если application license уже доступна через независимый Project Storage path.

Runtime/diagnostics различают frozen Agreement policy и fresh versioned commercial facts/cutoff/unlimited/current-period/completeness. Renewal либо новый plan получают новый period/Agreement; confirmed Agreement не transfers/replaces и не expiry-ится по возрасту в MVP. Reasoned source correction сохраняет policy и пересчитывает Freeze; duration purchase/unlimited upgrade — renewal. Dates не находятся в immutable policy binding и не вводятся клиентской формой. Models/storage принадлежат [backend F12](../../backend/features/F12-content-entitlement-policy.md), representation — [Q15 boundary](../../backend/questions/Q15-api-and-source-code.md#f12-policyagreement-integration-boundary).

Valid credential плюс missing required Agreement/period facts → Indeterminate/DataNotReady, не InvalidKey/default adoption. Complete authoritative no-grant и revoke → distinct Denied. После renewal под Deny нет old same-license Freeze fallback. Full verified multi-license set строгий: any needed Unknown whole Indeterminate; determinate set one latest whole Allowed. Same-candidate tie min LicenseId, then credentialId server-side. Session pins только selected basis, без organization expansion/item merge/auto-fallback; unselected revoke её не cancels. [Q20 Agreement](../../backend/questions/Q20-persistence-and-migration.md#f12-policyagreement-и-подтверждение-периода), [Q23 workflow](../../backend/questions/Q23-source-synchronization.md#f12-planned-agreement-confirmed-workflow).

Runtime `CmDeliverySession` отделён от staff authentication, Demo anonymous. App credential provider даёт raw keys только для admission/creation replay; SDK не defaults к key/hash persistence и не resend-ит raw key на ordinary reads. Durable CreationRequestId before POST восстанавливает same verified logical intent/original session-token-result без TTL extension/reselection. Temporary DB/keyring unknown сохраняет same ID, proved ReplayEnded допускает new ID/admission; остальные sessions не закрываются. Token opaque HTTPS header-only, no URL/log/S3 forwarding. Backend verifier-only store имеет явное short encrypted replay capsule exception с shared persistent encrypted Data Protection keyring.

Selected server defaults: absolute session30 min, replay5 min, source-proof-age30 s, response lease5 s configurable. TTL ограничен finite Deny cutoff/explicit credential deadline, не application expiry; no sliding/refresh. Proof age CM monotonic включает весь VRS HTTP+crypto+gate interval, source timestamp audit-only; credential epoch/license watermark independent. Active-period R1 pin может продолжаться при R2/Unpublish/Archive/Retire; expired Freeze требует equal eligible frozen pin, required unknown temporarily blocks. Renewal/known ineligible correction требует нового admission, expansion/restoration не extends TTL/resurrects.

Short primary guarded checkpoint проверяет current selected facts/ban перед cache304/Range/headers, после него byte I/O вне gate. Periodic checks + independent monotonic deadline abort even stalled renewal/late callback, bounded next policy boundary/session expiry; source propagation и buffered-byte delivery не становятся physical5sec SLA. Every Range/resume fresh authorization. Postheaders abort даёт SDK Unknown до typed evidence, не предполагаемый revoke/JSON inside bytes. New session имеет новый session-bound plan, old cursor не rebind; installed data retained. Runtime meanings401 bad/expired session,403 rights denial,409 basis needs new admission,503 needed data/upstream unknown,404 outside snapshot; exact carrier [Q15](../../backend/questions/Q15-api-and-source-code.md#delivery-session-credential-и-replay-contracts). [Q16 protocol](../../backend/questions/Q16-activation-and-sessions.md#принятый-протокол-сессий-и-response-leases), [Q20](../../backend/questions/Q20-persistence-and-migration.md#сессии-replay-и-response-leases).

Delivery availability history использует stored conservative VisibilityConfirmedAt: primary observation после exact полного committed transition, persisted once. Commit до cutoff с confirmation после/equal cutoff в Freeze не входит. Necessary confirmation/history pending — Indeterminate, не no-grant/old-nonnull fallback; complete last null означает no frozen revision. Current otherwise-allowed committed snapshot/previous effective может обслуживаться во время ordered catch-up. Source processing, effective outcome и history confirmation — разные server facts, matching IDs не readiness proof.

One transition per commit и strict source order/gap recovery сохраняют outcomes; terminal-blocked intermediate — NotEffective без availability row, missing artifact technical failure — не consumed success. Replay original stored time/outcome; absent confirmation recovery fresh/no backdate. Rollback новый transition/time. Whole candidate effective order не artifact age; same-candidate tie min LicenseId then credentialId, stable provenance retained. PolicyAgreement не меняется от corrected cutoff.

Independent exact ban действует для inactive/unmaterialized target и не снимается late materialization/replay. Admission/checkpoint owner authority и restriction до cache/304/Range остаются обязательны; required unknown не bytes permission. Transfer timing следует selected Q16 deadline/periodic-check protocol, без отзыва уже отправленных buffers. Models/owner protocols backend F13, SDK/UI показывают typed result. [Q17 visibility](../../backend/questions/Q17-publication-availability.md#подтверждённая-видимость-и-история-delivery), [Q20 catch-up](../../backend/questions/Q20-persistence-and-migration.md#delivery-catch-up-и-подтверждение-видимости).

## Карта фич

| ID | Спецификация этого раздела |
| --- | --- |
| F12 | [Политика права на контент](../features/F12-content-entitlement-policy.md) |
| F13 | [Сессия, манифест и загрузка](../features/F13-content-delivery.md) |
| F14 | [Точный version context в существующих экранах; расширение отложено](../features/F14-client-compatibility.md) |
| F19 | [Unity-библиотека клиента CM](../features/F19-unity-client.md) |
| F20 | [Демонстрационная сцена Unity](../features/F20-unity-demo-scene.md) |
| F23 | [Необязательная интеграция Unity Addressables](../features/F23-unity-addressables.md) |

## Связи

[Карта модулей по разделам](../../../modules.md). [Полная карта документов фич](../../../features.md).

## Вопросы модуля

[Q13](../questions/Q13-ui-scope.md) · [Q24](../questions/Q24-admin-sign-in.md)

## SDK и developer tooling

F19: netstandard2.1 DotNet with separate Http and Store.Sqlite adapters -> DotNet; Unity 2022.3.62f3 source bridge -> DotNet. Public full-sync/typed-store/raw-image contract F19; [Q32](../questions/Q32-sdk-package-distribution.md#согласованная-поставка-и-release-policy-sdk) selects separate SDK repo/packaged F20/DotNet DLL+NuGet-UPM/first Windows x64 IL2CPP/aligned immutable train, actual environment proof still needed. [F23](../features/F23-unity-addressables.md) optional separate release cycle, no base SDK dependency.

[M07](M07-schema-tooling.md) и F21/F22 отвечают за JSON Schema → C# до runtime, включая обязательный Demo UI→ZIP. Generated source не равен Unity-компоненту отображения, NSwag API-клиенту или inventory. Эти задачи не объединяются в одну модель.

## Совместная работа Demo

F13 рассчитывает immutable план по полному реестру и actual installed inventory, отдаёт компактный Start JSON metadata без основного payload/обязательного массива всех IDs и один конечный resumable streaming HTTP data response. F19 в DotNet читает последовательные exact SchemaVersion groups, сохраняет полные validated records в durable staging, затем checkpoint и после group validation/hash атомарно применяет data/removals/inventory/local receipt. Server group commit сообщается отдельно и идемпотентно восстанавливается при потерянном ACK. Допустим partial успех между целыми группами; исключённые из registry managed representations удаляются. Assets API/F05 самостоятельный, не блокирует завершение sync данных.

По [Q19 v1](../../backend/questions/Q19-content-hashes.md#принятый-canonical-profile-v1) source schema/instance hashes, runtime publication manifest, whole target group, actual inventory и original sync plan имеют разные фиксированные profiles. Integrity identity profile+SHA-256 digest (64 lowercase hex без prefix), immutable bytes/profile/digest together, no historical rehash. SDK strict typed verification не угадывает Long/Decimal/Float из untyped JSON и не принимает unknown profile до group apply. Whole upsert+removal target verified before atomic installed commit; own inventory commit не меняет original plan fingerprint. Provenance/comment/artifact ID не manifest bytes/dedup/Approval; binary Asset hash separate. Common pure .NET kernel/neutral models once backend F03; Registry/codegen profiles unchanged, no runtime Registry fetch/new UI picker.

[F13 и Q30](../../backend/questions/Q30-content-synchronization.md) закрепляют semantics, [Q33](../../backend/questions/Q33-sync-resume-transport.md) — NDJSON application/x-ndjson UTF-8 LF, explicit envelope variants/sequence, opaque cursor, durable stage+checkpoint/replay dedup, separate receipt gate/key и terminal invalidation. CM bounded one-host process cache имеет absolute default TTL 1 час без продления, restart теряет plan, lease generation fenced. Freshness read-only отдельно, H3→H2 сохраняет context. Numeric limits/Unity transport matrix ещё требуют spike; технический Demo не добавляет licensed sessions/продуктовые revisions/шину.

## Восстановление и проверка актуальности

F19 через DotNet устойчиво сохраняет ID/plan, validated staged records/checkpoints и local receipts. Следующий вызов автоматически продолжает прежний ID при совместимых selection/installed/staging и доступном plan; timeout не доказывает невозможность resume. При доказанной невозможности/expiry либо safety invalidation старого target создаётся новый plan от actual inventory, committed группы сохраняются, latest не подставляется в старый план. Второй вызов одного набора отклоняется. Stop сохраняет пригодный plan/checkpointed staging, Discard очищает лишь unfinished; начатый atomic commit можно закончить. Stream completion не installed success. После полного успеха SDK отдельно проверяет freshness; следующий запуск выбирает приложение.

[Q33](../../backend/questions/Q33-sync-resume-transport.md) содержит принятый protocol и engineering details. SDK stage+checkpoint атомарны; installed/index/inventory/local receipt активируются следующей transaction. Ready Demo adapter обязателен, SQLite первый кандидат до native/AOT/platform spike; host adapter заменяем. Assets API самостоятельно, main-thread dispatcher/disposable image ownership принадлежат F19, F20 только потребляет их.

## Launcher, shared media cache и readiness после Demo

[F13](../features/F13-content-delivery.md#41-launcher-readiness-и-pinned-player-после-demo) описывает launcher-facing readiness: entities synced отдельно от media; RequiredBeforeLaunch блокирует только candidate snapshot до verified files, optional Preload failure не откатывает snapshot и не блокирует запуск. Player получает nonsecret StoreScope/SnapshotId, остаётся на pinned snapshot и сам обращается к защищённому CM gateway при cache miss. Builds распространяет ProjectStorage; build-to-Variant/exact SchemaVersion mapping не выводится из Name/latest.

[F01/F02](../features/F01-create-entity-schema.md#63-режим-доставки-assetreference-после-demo) владеют DeliveryMode inspector у AssetReference leaf/ItemDefinition, default OnDemand; Published readonly. MediaLibrary не задаёт глобальную policy, Variant override не введён. Предложенный manifest scoped к одной permitted revision и final effective content; предлагаемая strongest-mode aggregation одного файла требует подтверждения Q37 и не объединяет права разных Variants.

[F19](../features/F19-unity-client.md#68-общий-файловый-cache-и-launcherapp-интеграция-после-demo) — единственный owner download/cache state machine и ports: SQLite index/jobs, отдельные filesystem bytes, Range/checkpoint/full SHA/atomic publish/crash recovery, cross-process fencing и file leases. Optional F23 только оборачивает manager и typed reader. 5–10 ГБ не SQLite BLOB/byte[]; VideoPlayer использует verified local file. [Q37](../questions/Q37-launcher-asset-delivery.md) owns оставшиеся manifest/profile/session re-admission/offline/locking/quota/large-upload решения, [Q32](../questions/Q32-sdk-package-distribution.md) — exact native/package/IL2CPP proof. Всё это последующая интеграция, без изменения самостоятельного Demo.

## F23: независимый платформенный content flow

[Optional add-on F23](../features/F23-unity-addressables.md) принимает AssetId из уже полученных данных F19 и отдельно выполняет Resolve → Load/explicit Preload → managed handle → Release. Пакет не вводит CM admin page, дополнительный sync/inventory owner или обязательную часть F20. Управление конкретным игровым экраном принадлежит приложению; add-on предоставляет ресурсы и lifecycle API.

Mapping Manifest, Unity catalog/bundles и operation handles находятся в платформенном/add-on слое. schemaVersion формата и contentVersion mappings не являются schema/version selections F13 или package version SDK. Успешный data commit не гарантирует готовность Unity asset, а сбой Load не откатывает installed данные. CM-слои сохраняют [свои границы](../../backend/modules/M05-delivery.md#f23-граница-optional-addressables).

В F23 определены компоненты, модели, ownership и отдельные клиентские потоки. Точный mapping/catalog snapshot, preload mode, prefab instance lifetime, cancellation и remote update policy — [Q29](../questions/Q29-unity-client-package.md#f23-mapping-snapshot-и-ownership-операций); пакетная/runtime matrix — [Q32](../questions/Q32-sdk-package-distribution.md#f23-матрица-поставки-add-on). Эти вопросы не добавляют remote content или Addressables в обязательный Demo.
