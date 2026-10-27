# M05. Delivery

## Назначение

Delivery — runtime boundary Content Manager для клиентских приложений.

Модуль поддерживает два режима этапов проекта:

- Demo: resumable synchronization Published ContentModeling по exact SchemaVersion selection;
- MVP: entitlement-selected protected delivery exact ProductVariantPublicationRevision.

Они используют общие immutable IDs/content hashes, но не одну session model.

## Карта фич

| ID | Ответственность |
| --- | --- |
| [F13](../features/F13-content-delivery.md) | Demo sync plan/resume + MVP protected runtime delivery |
| [F14](../features/F14-client-compatibility.md) | Exact version boundary; расширенная совместимость отложена |
| [F12](../features/F12-content-entitlement-policy.md) | Publishing policy agreements/history и Delivery exact binding/current-period entitlement |
| F19 | .NET/Unity SDK consumer внешнего API |
| [F23](../../frontend/features/F23-unity-addressables.md) | Optional Unity integration; AssetId/Delivery contract сохраняются; server-границы ниже |

## Demo architecture

```text
Sdk.Client.DotNet
  -> full exact SchemaVersion selection + installed inventory
  -> immutable SynchronizationPlan
  -> bounded in-memory CM plan store (single Demo process, absolute TTL from creation 1h default)
  -> exact Published ContentModeling reads
  -> Start JSON metadata + one resumable NDJSON application/x-ndjson UTF-8/LF response
  -> durable record staging/checkpoint + atomic client commit per SchemaVersion group
  -> separate idempotent server group receipts (PlanId + SchemaVersionId + TargetGroupHash)
  -> next group only after receipt; endpoint independent from stream lease
  -> post-sync preflight
```

Demo не использует VR School, licenses/grants, Publishing, RabbitMQ и `DeliverySession`.

`SynchronizationPlan` — technical Application model/cache object, не Domain aggregate и не PostgreSQL business state.

Stream использует explicit envelope ProtocolVersion/PlanId/RecordSequence и variants GroupStarted/EntityUpsert/EntityRemove/GroupCompleted/OperationCompleted/OperationInvalidated. Opaque cursor связан с последним durably staged record sequence; identical replay дедуплицируется, changed payload fails. Server restart/expiry теряет plan, клиент создаёт новый от actual inventory, сохраняя installed группы. Stream не продлевает TTL; lease/generation fencing исключает late writer. Exact DTO/token codecs/counts/limits материализуются в [F13](../features/F13-content-delivery.md)/[Q33](../questions/Q33-sync-resume-transport.md), Unity/storage compatibility проверяется реально.

## Policy promise before runtime
[F12](../features/F12-content-entitlement-policy.md#21-aggregates-entities-и-value-objects) defines agreement/models once: VRS durable exact planned period → CM own Publishing PolicyAgreement commit/current quote → VRS own conditional original-plan+AgreementId confirmation/outbox → Catalog own confirmed tuple/current facts → Delivery own immutable binding. Source calls CM outside its transaction; no shared source/Publishing/Catalog/Delivery UoW/FK.

Agreement fixes exact LicenseId+VariantId+SourcePeriodId/original plan/parent/policy provenance before source confirmation. Delivery binding unique tuple copies exact agreement policy; own GrantPeriodId is immutable binding identity from server factory, not another mutable commercial period. Freeze quote before later Deny still Freeze when confirmed later; missing agreement cannot use current/default/as-of guess. System Freeze carries null policy version; unused/no-expiry agreement alone grants nothing.

## MVP architecture

```text
Publishing state transition
  -> MassTransit event
  -> Delivery projector / sequence catch-up
  -> immutable DeliveryPublicationSnapshot
  -> ProductVariantDeliveryState + effective history

Activation key
  -> Delivery API
  -> VR School HTTP key validation
  -> CatalogAccess License/grant facts
  -> explicit source current period + exact source-confirmed F12 agreement binding
  -> F12 completeness/whole entitlement selector
  -> pinned DeliverySession
  -> protected manifest / items / assets
```

## Domain ownership

Delivery owns:

- `ProductVariantDeliveryState`;
- immutable source-processing outcomes separate from actual append-only DeliveryActivationRecord;
- pending→first confirmed visibility evidence with internal own Version; immutable public read values/owned Domain evaluator values defined F13;
- immutable `DeliveryPublicationSnapshot` and materialized items/assets;
- `DeliverySession`;
- `EntitlementPolicyBinding` from F12 Delivery side.

Raw activation keys are never persisted. Session stores only fixed32-byte SHA256 verifier; an explicit short encrypted replay capsule stores ONLY purpose-bound token using ASP.NET DataProtection, separately from session, with retained original receipt/tombstone.

## Effective state

Publishing `ActiveRevisionId` is not runtime availability.

Bus only triggers public ordered committed Publishing history/completeness catch-up; gap12→15 processes13/14 first. LastProcessedPublishingSequence differs from actual effective history/time. Known terminal banned intermediate consumes NotEffective/no history/no stall; technical missing artifact remains retryable. Stage outside gate, recheck source/order/owner ban under own gate, one actual transition per own commit.

Full own snapshot+pointer/actual history+Pending evidence/source processing receipt/sequence/inbox commits atomically; batch artifact preparation allowed, fake multiple effective states in one commit forbidden. Previous committed state may serve if remaining rights/restrictions allow. Null actual state is retained; rollback old artifact is a new transition/order/time.

Freeze cutoff is **stored VisibilityConfirmedAt after primary observation of exact already committed history/full snapshot**, not actual commit time or time of evidence-save commit. Primary PostgreSQL UTC/strict stored comparison, equality excluded/no second rounding. Business rule is conservative: original commit before cutoff but confirmation after is excluded; no host precommit clock/pg_track_commit_timestamp dependency.

Confirmation runs in a NEW own transaction after original commit reconciliation, re-reading exact committed old/history/null/full graph by identity on primary, without requiring current pointer equality. Oldest necessary pending first; save first UTC observation once, ACK unknown lookup winner or fresh recovery if not saved, no backdate/max clamp. Detected clock/order failure affects necessary temporal DataNotReady. Pending time does not alone block otherwise-allowed current committed snapshot; expired Freeze requires proven complete relevant cutoff prefix, cannot skip necessary unknown to older/non-null. Irrelevant later source/pending lag that cannot backdate is not blanket veto.

[F13 models/handlers/read values](../features/F13-content-delivery.md#21-aggregates-entities-и-value-objects) define receipt/history/evidence/ban/completeness once; [Q17](../questions/Q17-publication-availability.md#подтверждённая-видимость-и-история-delivery) records conservative checkpoint. Exact terminal ban remains independent, primary owner checked for new admission/protected reads before cache304/range; unknown not Allowed; accepted response checkpoints/independent response leases below govern in-flight cancellation.

## Session model

[F13](../features/F13-content-delivery.md#21-aggregates-entities-и-value-objects) defines full session/security/receipt/capsule/pinned decision/lease models once. Named CmDeliverySession bearer scheme has no staff/admin rights; Demo remains anonymous. Server32-byte CSPRNG Base64Url opaque token, fixed32 SHA256 verifier/FixedTimeEquals, HTTPS Authorization only. Session pins selected verified CredentialId/LicenseId/SourcePeriodId/GrantPeriodId/Variant/revision/hash/security epoch and absolute expiry.

Creation validates explicit full raw-key set; required unknown fails whole admission. Most current whole candidate, exact tie min LicenseId then CredentialId; no alternate-license fallback for existing pin. SDK durably saves CreationRequestId/nonsecret intent before POST; server derives normalized meaning from trusted verified IDs, not client authority/rawkey/hash/epochs/audit times. Same confirmed context replays original session/token/TTL.

Protect/Unprotect and VRS HTTP outside gate; CM monotonic proof age starts before HTTP and includes keyring/crypto/gatewait(default30s). Final own Delivery gate checks fresh authority/receipt/capsule version/state/deadlines; Session+verifier+original receipt+encrypted capsule atomic. Replay disclosure requires fresh original context/current pinned eligibility; no new candidate/restore/extend. Shared persistent DP keyring/application discriminator/versioned purpose with encryption at rest/restricted access; physical profile Q20. Temporary outage same ID; conclusive expired/irrecoverable capsule typed NoReplay/new ID, original knownID/outcome survives cleanup.

Configured defaults: absolute TTL30m, replay5m, proof age30s, response lease5s. Expiry min admitted+TTL, finite Deny cutoff, explicit credential deadline, never application ExpiresAt. Source HTTP/broker same per-credential security epoch ordering; forward selected epoch requires new admission even Valid, old terminal state never resurrects; audit/general metadata/commercial correction do not increment epoch. Local necessary license projection meets trusted watermark/completeness.

Separate pure [F12 EvaluatePinnedRevision](../features/F12-content-entitlement-policy.md#44-services--algorithms): active updates may keep R1 despite R2/Unpublish/Retire/archive; renewal new current period requires new admission. Genuine correction preserves Agreement/pin, reevaluates current cutoff; expanded cutoff never extends TTL/restores invalidated session. Expired Freeze requires original pin equal eligible confirmed whole candidate; mismatch needs new admission, needed unknown DataNotReady. No VRS HTTP on resource/progress; ordinary VRS outage alone does not revoke remaining rights.

## Response authorization

Each manifest/item/progress/asset/range/resume starts short own Delivery ReadCommitted transaction with global gate first, fresh own session/security and independent primary public selected-license/current-period/cutoff/pin/membership/exact-ban facts. Release before headers/cache304/Range/YARP/bytes. This Application authorization checkpoint is not a stream-long write permit and does not enlist foreign UoW.

Ephemeral response lease <=min(now+configured5s, absolute session expiry, next finite policy boundary INCLUDING Freeze). Periodic new checkpoints plus independent monotonic timer cancel downstream/upstream when renewal stalls/fails, known invalidation arrives or deadline expires; late renewal cannot resurrect cancelled response. Boundary may need new HTTP even same session remains valid. No zero-latency/physical5s SLA or buffered-byte recall.

Preheader401 bad/expired session,403 confirmed rights denied,409 changed basis/new admission,503 proof/required-data unknown,404 outside composition; exact wire Q15. After headers transport abort yields SDK Unknown, not assumed revoke. Server storage auth only/no CM token forwarded; installed data retained and old plan never rebound to new session.

## Persistence

PostgreSQL schema `delivery`, own DbContext/migrations.

Persistent MVP groups:

- delivery publication snapshots/items/assets;
- actual effective state/history, separate source-processing receipts/progress, pending/confirmed observation evidence;
- entitlement policy bindings with own immutable GrantPeriodId and unique License+Variant+SourcePeriod/exact agreement provenance;
- protected sessions/fixed verifier/selected security epochs;
- immutable creation receipts, short encrypted replay capsules/retained knownID outcomes, own credential security projection;
- inbox/consumer state required by internal events.

Demo plan store is local/configurable; physical cache adapter/expiry profile Q33, no PostgreSQL plan aggregate by default.

## Меж-BC зависимости

### Publishing

- state/withdrawal/policy events;
- ordered activation history catch-up;
- immutable exact PublicationRevision read-port.

### ContentModeling

- exact immutable Published payload for Demo desired set;
- exact immutable payload during MVP snapshot materialization.

### CatalogAccess

- verified License+Variant+SourcePeriod facts, immutable source-confirmed AgreementId/original plan;
- explicit current period and versioned StartsAt/UpdatesUntil/IsUnlimitedLicense/closure/revoke/classification/completeness;
- revoke/source readiness events.

### MediaLibrary

- public media metadata/eligibility facts after Delivery verifies AssetId belongs to pinned snapshot; Host YARP forwards bytes after gate/cache ordering.

No foreign DbContext/repository/table or cross-BC FK.

## Runtime invariants

- Publishing active and Delivery effective are different states;
- partial snapshot never becomes effective;
- historical materialized snapshot is never recomputed using newer composition logic;
- one entitlement decision selects one whole revision from explicit current period/exact agreement;
- complete authoritative no grant Denied, missing projection/agreement/period Indeterminate, confirmed revoke distinct; never InvalidKey masking;
- any necessary unknown in verified requested multi-license set gives whole Indeterminate; known Denied+Allowed can choose whole Allowed, no item merge/organization expansion;
- raw activation keys appear only during create and creation recovery; no default SDK secret persistence;
- upstream validation outage is not credential revocation;
- Freeze uses required complete actual history and first stored primary confirmation strictly before cutoff, including null;
- matching active/effective IDs not readiness proof: staff shows source gap/materialization/pending time/ban; necessary unknown Indeterminate;
- Withdrawal overrides Freeze/session access;
- assets are independent downloads and do not roll back successful data sync.

## Launcher и media delivery после Demo

Digital Space Launcher вызывает F13 только для structured exact entity sync и затем планирует выборочный preload; Unity Player читает pinned local snapshot и самостоятельно получает cache misses через защищённый CM F05 gateway. Entity readiness, required-media readiness и download progress различаются; optional preload failure не откатывает snapshot, RequiredBeforeLaunch блокирует именно выбранный snapshot до verified files. Общий download/cache coordinator — DotNet SDK, persistent adapter — SQLite metadata/jobs плюс filesystem bytes; никаких крупных BLOB и media jobs в CM SynchronizationPlan.

Policy owner — versioned recursive AssetReference leaf definition F01, default OnDemand; MediaAsset/Variant не получают editable override. Предлагаемый F13 resolver работает на одной whole authorized materialized revision после assignments/overrides/exclusions и exact schema×instance pairing; optional absence не creates blocker, repeated asset uses агрегируются по strictest mode внутри этого scope после согласования Q37. Derived manifest/API/integrity/owner materialization и build→Variant/exact SchemaVersion mapping остаются [Q37](../../frontend/questions/Q37-launcher-asset-delivery.md), не новое поле wire v1 или `cm-publication-manifest/1`.

Shared verified bytes не означают общий grant: fresh session/revision membership/entitlement проверяется для каждого remote HEAD/GET/Range до cache/304/206 и headers, независимые response leases прекращают долгие transfers. App re-admission при закрытом launcher, offline/revocation/old-snapshot fallback, cross-process SDK locks/native provider и actual Windows x64 Unity `2022.3.62f3` IL2CPP compatibility остаются Q37. Demo direct sync/image path работает независимо от этого integration этапа.

## Transport

Demo:
- HTTP JSON for Start metadata/status/commit/preflight;
- one finite streaming HTTP response for sequential group/entity/removal/completion records, optional durable application cursor;
- SDK local progress from metadata/checkpointed records/validation/commit;
- HTTP/3 preferred where actually supported;
- HTTP/2 mandatory fallback with identical application semantics;
- assets separate CM-owned YARP gateway F05 (read-only GET/HEAD, provider/cache profile Q21).

MVP:
- session create/recovery by explicit full key set + durable CreationRequestId;
- opaque bearer token for subsequent requests;
- manifest/item routes scoped to exact pinned session; media gateway checks pinned session/revision access before cache/304/range/proxy.

## Связи

[F13](../features/F13-content-delivery.md) · [F12](../features/F12-content-entitlement-policy.md) · [Publishing M04](M04-publishing.md) · [Media M02](M02-media-library.md) · [Q16](../questions/Q16-activation-and-sessions.md) · [Q17](../questions/Q17-publication-availability.md) · [Q30](../questions/Q30-content-synchronization.md) · [Q33](../questions/Q33-sync-resume-transport.md)

## F23: граница optional Addressables

[Бизнес F23](../../../business/features/F23-unity-addressables.md) · [Клиентская F23](../../frontend/features/F23-unity-addressables.md). У add-on нет отдельной CM server-фичи, aggregate или protocol. Таблица поясняет применимость физических слоёв целевой реализации, не подтверждает готовность их API; server models/алгоритмы определены один раз в [F05](../features/F05-media-assets.md) и [F13](../features/F13-content-delivery.md).

| Физический слой | Ответственность CM для потребителя F23 |
| --- | --- |
| Domain | ContentModeling/MediaLibrary/Delivery сохраняют свои identities и инварианты. AssetReference хранит универсальный AssetId; Unity key/catalog/handle не доменные поля CM. Новый F23 aggregate, internal Version и domain event не нужны |
| Application.Abstractions | Переиспользуются exact content/AssetId/file contracts F13/F05. Нет LoadAddressableCommand, mapping DTO или нового client catalog query на сервере |
| Application | Целевые F13/F05 use cases исполняют свои проверки и orchestration. Resolve/Load/Preload/Release принадлежат add-on; успешный sync данных заканчивается независимо от загрузки Unity content |
| Api | Целевой Demo data/asset API и MVP protected delivery; новый Addressables endpoint не вводится. Unity key не выдаётся вместо AssetId. CM entitlement/membership проверяется только в своих F12/F13/F05 границах |
| EntityFramework | BC mappings/DbContexts из F05/F13; нет таблицы manifest/catalog/bundle/handle, foreign Unity FK, сохранённого Addressables reference count или общей CM/Unity revision |
| EntityFramework.Persistence | Exact reads и local BC commits из F05/F13; нет транзакции sync commit + загрузка/переключение Unity resources и нет rollback installed данных при сбое add-on |
| External adapters / Host DI | CM storage/HTTP adapters остаются у F05/F13. Unity Addressables package, mapping provider и catalog lifecycle регистрируются в Unity host/add-on. CM Host не зависит от Unity runtime и не строит bundles |

Runtime ветви F23 выбираются явно: exact entity → typed SDK reader; raw AssetId → общий SDK verified cache/CM-owned F05 gateway → platform conversion либо file lease; Unity-built key → согласованный native catalog/bundles → managed resource handle → Release. Обычный raw F19 Stream/Unity helper остаётся самостоятельным. Native AssetBundle provider не обязан сначала скачивать raw bytes через F05; add-on не выбирает raw fallback по ошибке.

После Demo F23 дополнительно использует custom entity provider над typed SQLite reader и raw asset provider над общим SDK download/cache manager. Выбор kind/expected type явный: images/audio преобразуются Unity adapter, большие raw MP4 дают lease проверенного local file для VideoPlayer.url, не VideoClip из произвольного MP4 и не byte[] всего файла. Это не заменяет native AssetBundle/catalog ветвь для prefab/scene и не включает optional add-on в Demo. Точные provider/key/result API и migration legacy Mapping Manifest остаются Q37/Q29; CM не вводит второй Addressables endpoint/переносит runtime handles в БД.

Content pipeline готовит catalog/bundles/dependency metadata через Unity build. Наличие prefab-файла в CM/S3, совпадение bucket или наличие AssetId не делает его готовым Unity runtime asset. Источник/доставка mappings и content updates принадлежат [Q29](../../frontend/questions/Q29-unity-client-package.md#f23-mapping-snapshot-и-ownership-операций), версии/поставка package — [Q32](../../frontend/questions/Q32-sdk-package-distribution.md#f23-матрица-поставки-add-on). Их shared S3/CDN размещение не назначено как новая инфраструктура CM.

Для проверки границы нужны architecture checks отсутствия Unity/Addressables dependency у CM/DotNet и реальный клиентский сценарий из F23: metadata/AssetId → explicit Load → use → release. CM HTTP/DB тесты отдельно подтверждают F05/F13; doubles не доказывают lifetime/cancellation Unity handles. Этап add-on не утверждён; Demo F19/F20 продолжает работать без него.
