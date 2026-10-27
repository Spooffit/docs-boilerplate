# Q16. Ключи и сессии

## Вопрос и принятые границы

Как CM создаёт protected session, восстанавливает потерянный ответ, ограничивает её срок и прекращает передачу при изменении права?

**Ответ:** приняты stateful Delivery sessions, отдельная runtime Bearer scheme, verifier-only Session с короткой encrypted replay exception, absolute TTL, source credential security epoch, pure pinned-eligibility check и independently expiring response leases. Семантика и defaults закрыты ниже. Final wire/signatures/codec/access/error literals/physical registrations/keyring/provider/retention ещё инженерная детализация, не новый выбор механизмов.

[F12](../features/F12-content-entitlement-policy.md) · [F13](../features/F13-content-delivery.md) · [Q15 contracts](Q15-api-and-source-code.md#delivery-session-credential-и-replay-contracts) · [Q20 persistence](Q20-persistence-and-migration.md#сессии-replay-и-response-leases) · [Q23 source](Q23-source-synchronization.md#credential-validation-и-security-epoch) · [Q17 visibility](Q17-publication-availability.md#подтверждённая-видимость-и-история-delivery).

## Принятый протокол сессий и response leases

### Launcher/app re-admission после истечения session

App on-demand из [Q37](../../frontend/questions/Q37-launcher-asset-delivery.md) использует тот же authoritative runtime access. Absolute TTL/response leases/новый admission после истечения сохраняются. Источник credential/proof для многочасового app и повторного Range при закрытом Launcher **уточняется**. Launcher-mediated proof/IPC и app credential provider — альтернативы, не принятые протоколы. StoreScope/SnapshotId — non-secret bootstrap, не право на bytes. Нет staff-token fallback, expired bearer refresh или license keys в build/URL.

### Runtime identity и credential

CmDeliverySession — отдельная runtime authentication scheme, не staff/admin identity. Demo anonymous chain не получает лицензирование/Identity. Raw activation key передаётся CM только при new session и creation-result recovery; CM server вызывает authoritative VR School validation, Unity не вызывает второй backend и не выбирает LicenseId из своего ввода. Source validation подтверждает credential→license identity/validity, не RevisionId/grants. CM entitlement использует own broker projection/public owner facts.

При explicit multi-license input **весь** credential set доверенно проверяется, necessary unknown блокирует admission. Одна key не расширяется до organization licenses. Каждая license оценивается самостоятельно; одинаковый whole candidate выбирает smallest LicenseId, затем smallest credential ID. Session pins **selected** credential/license/period/Variant/revision/hash, без item merge/implicit future fallback. Unselected credential/license revoke не отменяет selected session.

Server credential = 32 bytes CSPRNG, Base64Url opaque string, Authorization: Bearer через HTTPS, no URI/query/cookie token. Session хранит SHA-256 verifier с unique lookup и fixed-length verification; SessionId/hash content сами не authority. Profile exact canonical encoding/parse/hash representation ещё Q15; decoded secret и verifier не wire/audit fields. [Bearer header](https://www.rfc-editor.org/rfc/rfc6750.html#section-2.1), [RandomNumberGenerator](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.randomnumbergenerator.getbytes), [FixedTimeEquals](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.cryptographicoperations.fixedtimeequals).

### Creation idempotency и encrypted replay

SDK durably сохраняет CreationRequestId **до HTTP**. После fresh authoritative validation original receipt lookup/comparison выполняется до new-session selection. Normalized meaningful input = requested Variant + verified credential/license identities и остальные действительно выбранные caller parameters. No raw activation key/hash, newly derived epoch/source version/validation time/current candidate in stored fingerprint. Other actor/scope не получает original result; changed meaningful input конфликтует. Existing result не новый admission, не extension/restore/reselection.

Hash-only verifier необратим и сам не возвращает потерянный random token. Поэтому **явная короткая exception**: encrypted credential capsule находится в owning creation receipt. Allocate SessionId/token и Protect только credential с purpose bound к exact SessionId/CreationRequestId **outside gate**, включая keyring I/O. После fresh guarded entitlement check собственный Delivery commit атомарно сохраняет Session/verifier и original receipt/result/capsule **до HTTP success**.

Replay требует fresh validation исходного credential context и current **pinned-session** eligibility, а не текущий new-admission selector. После Unprotect decoded token проверяется против stored verifier и exact receipt/session scope. Original pinned result/ExpiresAt/token не меняются. Known unusable session не resurrects, receipt committed outcome не переписывается как будто original creation не было.

Replay order: fresh source proofs → preliminary own receipt/context read → Unprotect/verifier match **outside gate** → final guarded current identity/epoch/pin/proof-age/replay-deadline check → response. Final checkpoint снова проверяет persisted receipt/capsule state; keyring/decrypt latency входит в attempt age. До final allow секрет клиенту не выдаётся; stale source proof требует redo outside transaction.

Применяется ASP.NET Core Data Protection: shared persistent keyring across replicas, одинаковые application discriminator/versioned purpose, restricted access и explicit at-rest key encryption. Custom key persistence сама не включает encryption: [configuration](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview), [purpose isolation](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/purpose-strings). Exact physical provider/certificate/secret registration materializes deployment, без ephemeral per-replica fallback.

Temporary DB/keyring/read outage — unknown/retry **same CreationRequestId**, не доказательство отсутствующей Session или законченного replay. Proven replay deadline/irrecoverable capsule loss — typed NoReplay, новый RequestId и новое admission. Новая операция не закрывает другие license sessions и не вводит one-session-per-license rule. Minimal receipt metadata сохраняет original outcome; cleanup не делает старый confirmed ID новой операцией. Precise retained-window/purge implementation — Q20; plaintext key/token/cipher/hash не diagnostics.

### Defaults, proof age и source epoch

| Server setting | MVP default | Constraints |
| --- | --- | --- |
| Absolute session TTL | 30 минут | Positive, no sliding/request extension; effective lease может быть короче |
| Credential replay window | 5 минут | Positive, capped Session expiry; capsule purge не стирает original outcome |
| Validation attempt max age | 30 секунд | CM monotonic elapsed от **перед VRS HTTP** до final guarded check, including HTTP/crypto/keyring/gate wait |
| Response lease maximum | 5 секунд | Positive, bounded session expiry и next finite policy boundary; not physical network SLA |

Все значения configurable server-side, startup validated; client не задаёт TTL/lease/replay duration. Это утверждённые стартовые defaults, не результаты load test. Guard/keyring/source timeout и retry/limits измеряются отдельно; backend не превращает stalled wait в grace extension.

VRS ValidatedAt — audit, не cross-system clock freshness. Expired attempt release/rollback guard scope, затем revalidate вне SQL transaction. Max age не отсчитывается от получения задержанного response. Exact monotonic timer/deadline adapter — F13/Q20.

CredentialSecurityEpoch distinct от license/source general Version. Он изменяется только при relevant credential security binding/validity/revoke/restore changes, не на каждом validation read или metadata edit. Source mutation+versioned non-secret fact/outbox atomic. Source event и authenticated validation response используют один per-credential epoch; lower epoch не перезаписывает newer CM knowledge, same epoch incompatible stable meaning — integrity failure. A fresh higher trusted response может advance own CM credential knowledge under gate; source audit timestamps не stable meaning.

Session immutable captured **selected credential epoch** должен совпадать с latest authoritative known CM epoch; forward security epoch требует new admission даже при новом Valid состоянии. Restore не оживляет revoked/invalidated old sessions. Commercial period correction сама не credential security epoch change. Finite credential deadline применяется непосредственно, не ждёт expire event.

Validation response даёт required license-source watermark без выдачи grants. Необходимая CM license/period projection >= hint, applied/ready/complete; otherwise DataNotReady, not InvalidKey/default grant. Source key actual revoke до получения CM соответствующего fact не означает мгновенное знание — propagation lag отдельно. Raw key в broker/CM DB отсутствует.

### Absolute expiry и pinned eligibility

SessionExpiresAt = min(AdmittedAt + configured TTL, current finite Deny UpdatesUntil at admission, explicit credential validity deadline). Application ExpiresAt не подставляется. No sliding expiration/refresh endpoint; expired Session требует new admission с app credential provider. Expanded later cutoff не продлевает original TTL.

Каждый protected request/checkpoint перечитывает current selected credential/security state, license/Variant grant, CurrentPeriod, immutable Agreement/binding, corrected cutoff/unlimited, exact pin/membership/restrictions. Snapshot/content не пересобирается по live authoring. No VRS validation HTTP на resource/progress read: ordinary VRS outage alone не отзывает existing session до TTL, остальные CM facts/restrictions должны быть достаточны.

Pure F12 **EvaluatePinnedRevision** отличается от new-session selector:

- During valid updates period otherwise-allowed pin R1 сохраняется при новой R2/ordinary Unpublish; source Archive/Retire сами не revoke.
- Current finite Deny expiry запрещает выдачу независимо от old stored TTL after correction.
- Expired Freeze pin должен совпасть с currently eligible **confirmed frozen whole candidate** Q17. Different candidate = unusable pin/new admission, не repin/older fallback. Necessary missing confirmation/history = temporary DataNotReady.
- Source CurrentPeriod renewal меняет basis: old period session unusable/new admission, no old same-license Freeze fallback. Это не license revoke.
- Genuine correction сохраняет Agreement/pin, reevaluates current confirmed facts. Allowed сохраняет original TTL; known ineligible invalidates/new admission; unknown временно блокирует без revoked mark. Extension/restore не resurrects terminal-invalidated session.

Credential revoke, license revoke, Variant grant revoke и exact revision Withdrawal имеют отдельные scopes. Selected-basis refusal не переключает к unselected earlier proof; new full admission может выбрать другое право. Bulk session status updates — enforcement/diagnostics effect, не причина ждать consumer вместо fresh authoritative facts.

### Response checkpoint, lease и cancellation

Authorization checkpoint — explicit Application use case в **короткой собственной Delivery ReadCommitted transaction**, first shared MVP advisory guard до authoritative reads. Fresh own Session/security epoch и independent primary public license/period/ban/membership facts; нет foreign DbContext/UoW. Это согласованная read authorization, не grant на всю SQL/HTTP жизнь и не mutation authoring. Gate освобождается **до headers/cache/304/Range/YARP/bytes**.

Ephemeral response lease bound min(checkpoint + configured5sec, SessionExpiresAt, next finite policy boundary, включая наступление Freeze). Periodic renewal obtains fresh checkpoint; deadline действует **независимо от renewal**. Final authoritative clock sample и conservative elapsed mapping к local monotonic timer учитывают spent time до выдачи lease; уже истёкший lease не используется.

If renewal waits on gate/source/DB or fails, old deadline отменяет writes/upstream независимо от pending task. Late callback не resurrects cancelled response; lease generation/deadline fencing остаётся local transport ownership, не business Session int Version. На policy boundary возможен новый HTTP request даже при ещё допустимой same Session. No guard held on download/stream/receipt wait.

Known committed CM invalidator denies new checkpoints; active response отменяется при known restriction/deadline. Local after-commit cancellation может ускорять свою replica, но across replicas correctness даёт fresh primary checkpoints+independent expiry, без обязательного Redis/new notification channel. Five seconds — configured server permission horizon **не hard физическая delivery latency**: scheduling/transport buffers/уже отправленные bytes не отзываются. Producer-to-CM source lag и test-observed cancellation измеряются отдельно.

Range/resume получает fresh checkpoint; pin/membership/current restrictions проверяются перед protected cache/304. Client token не forwarding credential S3: Host strips client authorization, uses server storage auth. Before headers — typed refusal; afterwards selected protocol invalidation/abort, no JSON ProblemDetails inside binary. SDK interrupted transfer outcome **Unknown** until observed typed refusal/result, no guessed revoke.

New Session не привязывается к old plan silently: context/attempt fenced, новая operation basis должна быть явно установлена. Raw key supplies application provider, SDK default persistence не вводится. Previously installed data и committed results автоматически не удаляются; client local enforcement отдельна.

## F13: session credential и revocation

Этот anchor сохраняется для существующих ссылок. Accepted protocol расположен выше; models/fields/commands/ports/layers/DF определяются один раз в owning backend F13 и pure pinned decision F12. Public read carriers immutable/nontracking, no internal Version/Domain entities/secret exposure.

Pre-header semantic mapping: 401 invalid/expired session credential, 403 confirmed right denied, 409 changed basis/new admission, 503 validation unavailable/necessary data unknown, 404 resource outside authorized snapshot. Same credential resolve/route mismatch does not authenticate guessed IDs; exact machine enums/body/headers/access priority ещё Q15. Raw activation validation errors не маскируют missing license projection. Creation replies/capsules не cached publicly.

## Current implementation evidence

Current [InternalDeliveryContracts](../../../../contentmanager.api/src/BoundedContexts/Delivery/ContentManager.Delivery.Application/InternalDeliveryContracts.cs) принимает already-trusted ResolvedLicenseContext, а [DeliverySession](../../../../contentmanager.api/src/BoundedContexts/Delivery/ContentManager.Delivery.Domain/Publications/PublishedComposition.cs) не доказывает внешний verifier/TTL/lease protocol. Source [ActivationKey](../../../../VRSchool.Host/src/VRSchool.Core/Licenses/ActivationKey.cs) имеет long stable Id и raw Guid key; [legacy DTO](../../../../VRSchool.Host/src/VRSchool.Application.Shared/Licenses/Dtos/ActivationKeyDto.cs) также содержит secret. Это current leaf evidence, не выбранный non-secret epoch/validation response и не причина копировать legacy DTO в новый contract.

## Приёмка и оставшаяся детализация

Обязательны real PostgreSQL/HTTP fault barriers: lost creation ACK, same-ID replay/different input/unauthorized receipt; two-replica shared-ring decrypt/rotation/outage; decoded token/verifier/scope mismatch; stale HTTP+crypto+gate-wait proof; newer/lower credential epoch races and restore; license watermark gaps; TTL/Deny/Freeze equality and pin mismatch; current R1 versus new R2/Unpublish; renewal/correction unknown vs known-ineligible; scoped revoke/unselected license; cached304/Range/membership; blocked renewal with independent expiry/late callback; real CM→YARP→storage abort and SDK Unknown/no installed deletion.

Не запускались source changes/application tests/builds/deployments. Open engineering: final source/runtime wire and DTOs, keyring/crypto/timer/Host adapters and physical migrations/DI, retention/limits/retry/clock-health/propagation metrics and measured network guarantees. Session mechanisms/defaults/pinned semantics/replay exception/lease expiry выбраны; future tooling/staff/SDK storage questions не становятся hidden Demo dependencies.
