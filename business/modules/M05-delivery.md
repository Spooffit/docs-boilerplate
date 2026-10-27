# M05. Delivery: бэкенд

## Назначение

Delivery — runtime boundary Content Manager.

Он:
- получает immutable publication transitions из Publishing;
- материализует собственную runtime projection;
- сохраняет conservative VisibilityConfirmedAt после primary observation committed transition;
- решает entitlement;
- создаёт pinned sessions;
- строит возобновляемые synchronization plans;
- выдаёт entity payload и runtime assets.

Delivery не читает Publishing/ContentModeling/MediaLibrary tables напрямую.

## Demo

Demo использует F13 без лицензий/Publishing:

```text
client selections + installed inventory
→ ContentModeling direct target
→ synchronization plan
→ one resumable streaming data response
→ client atomic schema/version commits
```

Withdrawal ContentModeling формирует Remove.

Start фиксирует полный выбор и actual installed inventory целиком либо отклоняется; failed authoritative read не превращается в массовое удаление. План содержит только изменённые exact schema/version группы, включая deselected/removal groups с пустым target. No-change возвращает `groups:[]` и terminal marker без receipts. Успех требует проверенного конца response, terminal marker и всех local commits/server ACKs; Status completed не доказывает установку клиента. Owner wire/models — [F13](../../technical/backend/features/F13-content-delivery.md#33-dto-и-parameter-models), согласованные public границы — [Q33](../../technical/backend/questions/Q33-sync-resume-transport.md#public-wire-contract-v1).

Assets получаются отдельно.

Immutable plan/records/hash/cursor отделены от execution generation/lease. Один process-local coordinator защищает короткие transitions/receipts; HTTP/DB I/O и client wait идут вне guard. Data atomically получает собственное исполнение; expired owner заменяется, stale worker/control не изменяет новое. Server receipts независимы от Data lease, сохраняются идемпотентно; first receipt допустим лишь для следующей offered group. Execution lease не является MVP authorization lease.

SDK восстанавливает durable context → Status → pending receipt repeats без local reapply → Data checkpoint. Stop сохраняет checkpoint/staging/installed, started atomic commit может завершиться. Discard сначала разрешает неизвестный local commit outcome, затем очищает unfinished attempt, сохраняя installed/inventory/committed receipts. Stable store scope допускает одну sync между экземплярами SDK; cross-process adapter proof остаётся Q29/Q32.

Preparing reservation получает PlanId и сохраняет original input; Stop/Resume могут завершить первую подготовку, но не пересчитать ready target. Absolute TTL от reservation default1h без продления; active capacity не вытесняется произвольно, новый Start при заполнении недоступен. Known expired/discarded tombstone даёт 410, unknown/restart/evicted tombstone — 404. Отдельная Freshness по полному selection/actual inventory не создаёт plan/lease и не запускает sync автоматически; ошибка проверки оставляет Unknown и installed сохраняется.

## MVP

```text
activation key
→ VR School validation HTTP
→ complete confirmed License/Variant facts + explicit CurrentPeriod
→ exact PolicyAgreement-backed EntitlementPolicyBinding
→ Delivery effective history
→ allowed Revision
→ pinned DeliverySession
→ synchronization plan
```

## Launcher, snapshots и selective preload после Demo

ProjectStorage распространяет builds, CM — entities/media, VR School — каталог/лицензионные facts. Digital Space Launcher вызывает F13 через DotNet и публикует согласованный entity snapshot в общем SQLite. Запущенная Unity app читает pinned snapshot и сама обращается за cache miss к protected CM `/storage/assets/{assetId}`. Исторический Demo direct-sync сценарий F19/F20 остаётся самостоятельным.

Mode объявлен в exact schema AssetReference leaf: OnDemand default, Preload без launch block, RequiredBeforeLaunch до verified bytes присутствующих ссылок. Runtime descriptor остаётся пяти полей; отсутствующая optional ссылка не блокирует запуск. Entity sync readiness не равна media readiness, optional media failure не откатывает entities; gate относится только к конкретному snapshot. Общий SDK coordinator и persistent cache — [F19](../features/F19-unity-client.md#launcher-и-общий-media-cache-после-demo); optional Addressables — adapter над ними.

Рекомендован resolved immutable AssetDeliveryManifest из exact permitted revision после Publishing composition, с strictest-mode aggregation внутри одного scope. Он не entitlement и не editable policy owner. Exact API/profile/IDs, build↔SchemaVersion mapping, process locking и безопасный повторный admission app после session expiry **Уточняется [Q37](../../technical/frontend/questions/Q37-launcher-asset-delivery.md)**. F13 wire v1 и source Published hashes не переписываются. Offline/revocation/old snapshot fallback и точная milestone декомпозиция ещё не утверждены.

## Основные модели

| Модель | Ответственность |
| --- | --- |
| `DeliveryPublicationRevision` | immutable runtime projection revision |
| `DeliveryVariantState` | current committed effective revision; LastProcessedPublishingSequence отдельно от confirmed availability history |
| `DeliveryEffectiveRecord` | append-only complete transition с separately confirmed immutable VisibilityConfirmedAt; pending time не historical availability |
| `DeliverySession` | exact selected credential/license/Variant/period/Agreement/whole revision pin, absolute TTL; runtime bearer отдельно от staff Identity |
| `EntitlementPolicyBinding` | Delivery-owned immutable policy basis exact LicenseId + VariantId + SourcePeriodId; own GrantPeriodId и Agreement/source provenance |
| `SynchronizationPlan` | short-lived cache model, not DB aggregate |

## Publishing projection

Publishing outbox transition содержит VariantId, RevisionId?, Sequence, ActivatedAt, Reason.

Delivery:
1. использует bus event как trigger и читает ordered public Publishing history с range completeness: после12 event15 требует13/14;
2. stages complete payload вне короткого coordination gate;
3. own commit атомарно сохраняет один real complete transition: projection/pointer/history/processed sequence/inbox; R1/R2/R3 одним commit не получают fake visibility;
4. после primary observation exact already committed transition один раз сохраняет UTC VisibilityConfirmedAt со stored precision; replay возвращает original outcome/time, changed payload same Variant/Sequence — integrity conflict;
5. если confirmation не stored, crash/ACK recovery сначала выясняет durable result, затем при необходимости фиксирует fresh observation без backdating; time-order failure даёт DataNotReady без clamp.

Terminal Withdrawn intermediate advances processed outcome NotEffective без availability row и позволяет продолжить next transition. Technical artifact error даёт NotReady/stall, не skip. LastProcessedPublishingSequence не является visibility history/time.

Current Publishing pointer and Delivery effective pointer intentionally may differ temporarily.

Previous committed effective revision может обслуживаться при current rights/restrictions во время catch-up. Current committed state без confirmation не синтезирует historical Freeze. Freeze выбирает последнее confirmed state строго VisibilityConfirmedAt < UpdatesUntil, включая null; commit before/confirmation after либо equality исключены, older non-null fallback отсутствует. Empty complete history, confirmed null, pending confirmation и incomplete history различаются; required Unknown → IndeterminateDataNotReady.

Rollback старого artifact получает новый transition/time; whole candidates сравниваются по effective order, не artifact age. Identical candidate выбирает minimum LicenseId, затем credentialId; selected provenance фиксируется в session без unselected revoke/fallback. Правила [Q17](../../technical/backend/questions/Q17-publication-availability.md#подтверждённая-видимость-и-история-delivery)/[Q20](../../technical/backend/questions/Q20-persistence-and-migration.md#delivery-catch-up-и-подтверждение-видимости) не меняют immutable PolicyAgreement: Correction меняет cutoff, не saved visibility time/policy.

## Agreement, admission и периоды

Publishing PolicyAgreement резервирует policy при своём commit, а не при Delivery first-use. Delivery получает confirmed source period с неизменяемым AgreementId и создаёт exact unique LicenseId + VariantId + SourcePeriodId binding. Current commercial StartsAt/UpdatesUntil/IsUnlimitedLicense/closure/revoke versioned отдельно от frozen policy; default Freeze сохраняется без fake published PolicyVersion.

Explicit CurrentPeriod source выбирает период LicenseId + VariantId для новой выдачи. Renewal создаёт новый source period/Agreement и после вступления в силу исключает fallback на old same-license period, включая expired новый Deny. Typed Correction с reason/history сохраняет agreed policy и пересчитывает Freeze cutoff; купленная длительность/unlimited upgrade — Renewal.

Complete authoritative no-grant и confirmed revoke — Denied с разными reasons; missing required facts/Agreement — IndeterminateDataNotReady. Contradictory replacement AgreementId rejected/quarantined сохраняет binding. Полный проверенный caller multi-license set с любой required Unknown даёт весь selection Indeterminate; при известных KnownDenied + Allowed выбирается latest Allowed whole revision. Explicit single license допустима без implicit organization expansion.

Agreement quote не выдаёт grant; unused Agreement не autoexpires в MVP. Renewal/CurrentPeriod change требует new admission и делает old-period session непригодной без fallback; genuine Correction сохраняет Agreement/pin и rechecks current facts с original TTL. Unknown temporarily blocks, known ineligible invalidates; extension/restoration не resurrect token. Exact правила [F12](../features/F12-content-entitlement-policy.md).

## Runtime session, replay и leases

CmDeliverySession отделён от staff/admin Identity и anonymous Demo. Raw key используется только Create/recovery через application provider, без default SDK persist. SDK сохраняет CreationRequestId до call; original normalized Variant + verified credential/license IDs не включает raw-key/hash или volatile epoch/time. Full explicit caller set проверяется; session pin только selected basis.

Session bearer 32 CSPRNG bytes Base64url передаётся по HTTPS Authorization. Stored Session имеет только SHA-256 verifier; временное replay exception — encrypted capsule ASP.NET Data Protection с persistent shared encrypted-at-rest keyring replicas. Own Session/receipt atomic commit предшествует HTTP response, token protection вне gate. Same-ID replay после fresh auth/pinned checks возвращает original token/result без TTL extension/нового candidate; other actor не получает receipt. Outage retry same ID; known expired/irrecoverable replay → new ID/admission, не закрывая другие sessions.

Server configurable defaults: absolute TTL 30 минут, capsule 5 минут, proof age 30 секунд, response lease 5 секунд; это не SLA. TTL ограничен finite Deny UpdatesUntil/credential deadline, не app ExpiresAt. CM monotonic proof age начинается до VRS HTTP и включает crypto/final gate wait; source ValidatedAt audit-only. Source security epoch/state/deadline + same-mutation outbox согласуют HTTP и broker facts; stale epoch не overwrites newer, CM license projection ниже source watermark hint →DataNotReady.

Existing session requests не вызывают VRS HTTP; upstream outage сам по себе не прекращает допустимый pin до TTL. Active updates сохраняют R1 после R2/ordinary Unpublish/Archive/Retire. Current Deny cutoff enforced после Correction; expired Freeze требует pin=current eligible confirmed frozen candidate Q17, иначе NeedsNewAdmission без repin; required Unknown temporarily blocks.

Короткий owner Delivery checkpoint по consistency guard fresh читает primary/public Session/selected epoch/license period/cutoff/pin/membership/bans. Release gate до headers/cache/304/Range/proxy/bytes. Response lease ограничен Session expiry и next finite boundary включая Freeze; independent monotonic deadline stops blocked renewal и late callback не revive canceled response. Known committed CM selected revoke denies checkpoint/cancels response по факту либо deadline. Нельзя обещать zero latency/physical 5-second SLA или отозвать buffered bytes; source asynchronous lag учитывается. Range/resume fresh auth, CM bearer не передаётся S3; after-headers abort SDK Unknown. Новая session/new plan без silent rebinding, installed data сохраняются.

Protocol/details: [Q16](../../technical/backend/questions/Q16-activation-and-sessions.md#принятый-протокол-сессий-и-response-leases), [Q20](../../technical/backend/questions/Q20-persistence-and-migration.md#сессии-replay-и-response-leases), [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#delivery-session-credential-и-replay-contracts), [Q23](../../technical/backend/questions/Q23-source-synchronization.md#credential-validation-и-security-epoch).

## Cross-BC dependencies

### Publishing
Public read-port for immutable revisions/history и exact PolicyAgreement evidence. Events for transitions/withdrawal. Policy не подбирается заново при первом использовании confirmed period.

### ContentModeling
Batch load immutable Published payload by EntityInstanceVersionId.

### CatalogAccess
Confirmed source license/Variant periods, explicit CurrentPeriod, completeness и versioned commercial/revoke facts; binding belongs Delivery, source commercial authority remains VR School.

### MediaLibrary
Open Asset stream/metadata by AssetId.

### VR School
Activation-key validation HTTP только для runtime Create/recovery; existing session requests используют current CM facts без VRS HTTP. Raw key never brokered/persisted by default SDK.
Отдельный authenticated backend VR School → CM planned Agreement workflow F12 не выдаёт runtime entitlement и не требует CM license projection. Подтверждение CM-content period зависит от Agreement; app license/Project Storage независимы.

## Persistence

PostgreSQL schema `delivery`, own DbContext/migrations.

No cross-BC FK.

Tables:
- runtime revision projection/items/assets;
- variant effective state/history;
- sessions;
- entitlement policy bindings.

Short-lived synchronization plans use cache port, not relational history by default.

## Transport

Plan: compact Start JSON metadata. Entity payload: one ordinary resumable streaming HTTP response, sequential exact SchemaVersion groups; UTF-8 NDJSON/LF. Start/Data/Commit/Status и Resume/Stop/Discard/Freshness semantics согласованы в [Q33](../../technical/backend/questions/Q33-sync-resume-transport.md#execution-control-и-transport-profile); owner DTO/public models — backend F13. Numeric stage budgets/renew/buffer/capacity/tombstone settings startup-validated, sizing после representative checks. Read-idle исключает ожидаемую local validation/commit/receipt паузу при сохранении stage/watchdog limits. Demo stream compression disabled, heartbeat отсутствует. Common reason literals/API mapping, SDK signatures и реальные execution/adapter/transport tests ещё требуются.

Progress: SDK local metadata/durable-record/validation/commit progress; separate group receipts after atomic local apply. No SSE control/progress stream.

HTTP/3 preferred where verified, HTTP/2 fallback.

Assets: separate runtime route, authorized against pinned session/revision.

New admission и checkpoints protected reads используют authoritative owner terminal ban/restrictions до cache/304/Range. Ban exact revision действует независимо от pointer/materialization, включая inactive/unmaterialized; late replay/materialization не lifts его. Unknown temporarily blocks; selected revoke/ban и bounded lease deadline останавливают response по принятому протоколу Q16.

Staff diagnostics различают active/effective IDs, processed gap, materialization, pending confirmation и ban; matching IDs не означает full readiness. Exact fields относятся к [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#delivery-visibility-contracts).

## Feature map

| ID | Ответственность |
| --- | --- |
| F12 | policy configuration/Agreement reservation in Publishing + confirmed binding/CurrentPeriod evaluation in Delivery |
| F13 | projection, session, sync and runtime asset authorization |
| F19 | DotNet/Unity client implementation |
| F20 | Demo scene |
| F23 | optional Addressables adapter |

## Связи

[Publishing M04](M04-publishing.md) · [Media M02](M02-media-library.md) · [Registry M07](M07-schema-tooling.md) · [Q16](../../technical/backend/questions/Q16-activation-and-sessions.md) · [Q30](../../technical/backend/questions/Q30-content-synchronization.md) · [Q33](../../technical/backend/questions/Q33-sync-resume-transport.md)
