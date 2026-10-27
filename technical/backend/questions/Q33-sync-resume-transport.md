# Q33. Возобновляемый streaming HTTP и проверка транспорта

[Решения Q30](Q30-content-synchronization.md) · [Backend F13](../features/F13-content-delivery.md) · [SDK F19](../../frontend/features/F19-unity-client.md) · [Принятое транспортное решение](../../../archive/content-manager-sync-streaming-transport-decision.md).

## Действующее решение по транспорту

По переданному пользователем решению прежняя схема SSE state/progress + отдельные GET порций заменена: **Start JSON → один обычный конечный streaming HTTP response данных → отдельные group commit requests**. Отдельный SSE progress/control channel, второй SSE stream и SSE bulk payload не являются целевым протоколом. Исторические ответы блока 6 о SSE отменены в транспортной части; остальные гарантии сохраняются.

Start передаёт полный Selection и фактический InstalledInventory. CM фиксирует immutable plan с exact Published EntityInstanceVersion IDs, Add/Update/Remove и группами exact SchemaVersionId; возвращает компактные plan metadata, без основного entity payload. Не обязательно отдавать все pinned IDs клиенту, если они нужны только серверу. Логический состав summary: SynchronizationId, expiry/plan identity, groups с target hash и counts; camelCase members/types/requiredness приняты ниже в Public wire contract v1 и таблицах F13; whole codecs/adapters проверяются fixtures.

HTTP-поверхность из решения: POST /api/v1/content-synchronizations/{synchronizationId}/start; GET /api/v1/content-synchronizations/{synchronizationId}/data с optional application cursor; POST /api/v1/content-synchronizations/{synchronizationId}/groups/{schemaVersionId}/commit. Start/Data/Commit/Status exposure и основные statuses согласованы Public wire contract v1; Resume/Stop/Discard/Freshness/generation/lease/transport semantics согласованы ниже; concrete adapters/numeric settings/runtime checks остаются implementation. Application cursor не HTTP byte range, SSE event ID или новый synchronizationId.

Поток последовательно выдаёт группы, без interleaving. Выбран **NDJSON**, `application/x-ndjson`, UTF-8: один compact JSON record на строку, сервер пишет LF delimiter, JSON string newlines экранируются. LF завершает полный record; неполный хвост не checkpoint. Parser допускает стандартный CRLF delimiter без изменения canonical content bytes; malformed/empty records не превращаются в успешные данные. [NDJSON specification](https://github.com/ndjson/ndjson-spec).

Envelope содержит ProtocolVersion, PlanId, RecordSequence и explicit variant. Приняты variants `GroupStarted`, `EntityUpsert`, `EntityRemove`, `GroupCompleted`, `OperationCompleted`, `OperationInvalidated`; group variants содержат SchemaVersionId. Remove передаётся отдельным EntityRemove record в той же группе, не большим массивом Start metadata. У каждого variant свой required payload; unknown variant — protocol error. CamelCase kind/payload/cursor и wire types выбраны Public wire contract v1; model tables/literal examples у F13. recordSequence=0 зарезервирован terminal operationInvalidated, normal planned sequence1..N.

## SDK implementation evidence и выбранный client profile

Public wire/profile semantics ниже сохраняются. Portable SDK реализован в contentmanager.sdk.client; Unity adapter source находится в contentmanager.sdk.client.unity. Explicit generated exact bindings и ordered canonical verification используют common ContentManager.Canonicalization 0.0.0-f19.local.2; /1 profiles/F22 source не меняются. Inventory wire содержит schema/version/instance IDs, key, hash/profile; internal VersionNumber не добавляется.

Настраиваемые SDK defaults закреплены [F19 §5.5](../../frontend/features/F19-unity-client.md#55-composition-root-и-lifecycle): control/error JSON и record 8 MiB, queue 4 MiB/block 64 KiB, page 256; StartResume 90 s, unary/header/idle 30 s, record 60 s, storage/validation 120 s, receipt 30 s; three safe transient retries 0.5–5 s+jitter. Raw AssetTransferTimeout 120 s и image DecodeTimeout 30 s самостоятельны. Server Delivery:Synchronization defaults ниже остаются отдельным профилем.

Generic protocol/HTTP/assets/store suites и SDK E2E через actual CM/PostgreSQL/MinIO выполнены: F22 ZIP compile, multiversion typed durable reads, no-change freshness, update/remove и gateway SHA. Unity queue/budget/syntax checks не являются Unity managed compile/Editor/Player IL2CPP proof. HTTP/1.1 явно разрешён выбранному Demo; native incremental/cleanup/platform gates и GitLab CI/publish/private install остаются открытыми. Точный итоговый evidence — [план реализации](../f19-demo-implementation-plan.md).

## Public wire contract v1

**Ответ:** Согласованы все 24 пункта. Это specification для Start/Data/Commit/Status, не доказательство реализации. Execution/control/Freshness semantics согласованы в отдельном разделе ниже; numeric settings/concrete adapters и real transport conformance ещё материализуются. Owner models/field tables/literal fixtures — [F13](../features/F13-content-delivery.md#33-dto-и-parameter-models).

1. **Endpoints.** POST /api/v1/content-synchronizations/{synchronizationId}/start; GET /api/v1/content-synchronizations/{synchronizationId}; GET /api/v1/content-synchronizations/{synchronizationId}/data?planId=...&cursor=...; POST /api/v1/content-synchronizations/{synchronizationId}/groups/{schemaVersionId}/commit. JSON success existing ApiResponse<T>, Data raw NDJSON.
2. **Start request.** Required non-null selection и installedInventory arrays. Missing/null→400; empty supported. Empty selection удаляет ранее installed managed representations.
3. **Selection.** Item schemaId/schemaVersionId exact; duplicate normalized pairs→400; order не влияет на plan; version ownership checked server.
4. **Inventory.** schemaId/schemaVersionId/entityInstanceId/entityInstanceVersionId/instanceKey/contentHash/contentHashProfile required. Duplicate representation identities invalid; no Distinct/repair/install proof.
5. **Wire types.** camelCase properties; GUID output lowercase D; digest64lowerhex; profile ordinal string; record sequence/count JSON integer/CLR long without double conversion. Unknown protocolVersion/kind rejects; missing/duplicate required property invalid.
6. **Start atomicity.** All-or-error capture: invalid selection/unavailable authority не successful partial plan. Proven desired absence допускает Remove; failed read не mass removal.
7. **Plan identity.** Server-generated Guid planId отдельно от SDK synchronizationId и canonical planHash/profile; immutable exact plan binding, не credential.
8. **Plan result.** synchronizationId/planId/protocolVersion=1/planHash/planHashProfile/expiresAt/groups; no primary entity payload/full target ID array.
9. **Groups.** Only changed groups; ordinal canonical schemaId then schemaVersionId. Deselected/removal group retained with empty target.
10. **Summary.** schema IDs, targetGroupHash/profile, baseGroupHash/profile, targetEntityCount/addedCount/updatedCount/removedCount. Base = original installed group; target = complete post-operations membership incl unchanged members; both existing cm-target-group/1.
11. **No-change.** Successful groups:[]; Data OperationCompleted only, zero receipts; SDK then separate freshness.
12. **Envelope.** protocolVersion/synchronizationId/planId/recordSequence/kind/cursor/payload; group records schemaId/schemaVersionId required. OperationCompleted no schema IDs. Fixed kinds groupStarted/entityUpsert/entityRemove/groupCompleted/operationCompleted/operationInvalidated. Cursor only normal checkpointable records.
13. **Order.** Normal sequences stable 1..N per immutable plan. GroupStarted → operations by entityInstanceId → GroupCompleted, one operation per representation, no interleave.
14. **Upsert.** payload changeKind:add/update, entityInstanceId/entityInstanceVersionId/instanceKey/contentHash/contentHashProfile/contentJson. contentJson is JSON **string** carrying exact original canonical JSON. Unescape→strict UTF8 reconstruct→digest verify→typed parse; not nested object reserialization/base64; extra escaping tradeoff accepted.
15. **Remove.** payload entityInstanceId only; group context envelope; no target version/hash/fake content. Delete only matching managed group representation.
16. **Boundaries.** GroupStarted repeats immutable summary without duplicating envelope schema context. GroupCompleted target hash/profile and actual Add/Update/Remove counts. SDK matches plan, counts and full resulting membership/base; count alone not enough.
17. **Framing.** UTF8/noBOM compact JSON plus LF; parser LF/CRLF. Empty/malformed/duplicate property/truncated tail→protocol error. Size/buffers configurable representative fixtures; no invented numbers.
18. **Checkpoint.** Server record cursor is candidate; SDK persists validated full record+cursor atomically in durable staging. Parsed/network-only bytes don't advance it.
19. **Cursor codec.** Base64URL/no padding of versioned binary: version + synchronizationId + planId + afterSequence + HMAC-SHA256. UUID network byte order, sequence big-endian; Demo random process-local key, restart loses plans. SDK opaque, server verifies; not authorization or install proof. Concrete lengths/golden token fixture закрепляются у F13.
20. **Replay.** Resume after cursor with original sequences. Same identity/full parsed payload+restored content bytes dedup, other payload same seq error. Mid-group resume needs stored header/metadata/staging, не renamed fresh GroupStarted.
21. **Invalidation.** operationInvalidated reserved recordSequence=0/no cursor: terminal control outside immutable planned sequence. payload stable reasonCode/safe message, optional schema context for scope. No checkpoint/data-sequence replacement; unfinished work stops, committed groups retained.
22. **Receipt.** Body planId/targetGroupHash/targetGroupHashProfile, schemaVersionId route. Result same identity+acknowledged:true. Same receipt sameACK, other evidence409. LostACK Status/repeat, no local reapply.
23. **Status.** Plan metadata/accepted group receipts and separate planState preparing/ready/completed/invalidated/discarded; executionState idle/streaming/awaitingReceipt/stopped. Preparing may have no ready plan. Completed means all server receipts, not installed proof; retained plan can replay OperationCompleted after lost response.
24. **Errors / acceptance.** 400 malformed input/cursor,409 active ownership or plan/receipt mismatch,404 unknownID,410 only known expired/discarded via bounded tombstone until eviction. Restart missing→404; outage≠Missing. Before headers normal error profile; afterward invalidation or truncated stream. Full success valid OperationCompleted+all local commits/ACKs+correct response end. Fixtures all6variants/nochange/removal/replay/conflicts/partialUTF8/lostACK/unknownprofile/invalidation/restart; execution/lease races selected below; actual runtime/transport verification requires implementation.

### Что остаётся после public wire

Execution/control/Freshness/generation/timeout-stage/compression semantics согласованы в разделе Execution, control и transport profile; numeric budgets/buffers/limits, adapter lifecycle и actual races/transport checks остаются materialization. Literal machine reason/error registry mapping и concrete adapter integration остаются owner Q15/F13; public fields/sequence/cursor/record shape не выбираются заново. Generic SDK store/HTTP evidence описано выше; actual Unity Player conformance требует стенда Q29/Q32. Demo HTTP/1.1 разрешён, future H2/H3 требует отдельного proof.

## Execution, control и transport profile

**Ответ:** Пользователь согласовал все 24 пункта. Public wire v1 сохраняется; добавляются control-generation metadata и отдельные control/Freshness contracts. Это target model, не доказательство API/cache/SDK/Player реализации. Owner models/layers/flows — [F13](../features/F13-content-delivery.md), client behavior — [F19](../../frontend/features/F19-unity-client.md).

1. **Plan / execution.** Immutable target/records/hash/cursor отдельно от mutable worker/generation/lease/wait state. New execution не меняет semantic plan.
2. **Coordinator.** Один process-local coordinator на операцию с короткой atomic state guard: acquire/renew/revoke/transitions/receipts; HTTP/DB I/O и client wait вне guard.
3. **Generation.** Monotonic long, increments new execution/revocation, overflow prohibited. Status executionGeneration и Data header X-CM-Sync-Execution-Generation; no generation in NDJSON/cursor.
4. **Acquire.** Каждый Data request получает own generation/lease atomically. Valid competing lease→409. Expired owner revoke/replace allowed; old Task existence не perpetual lock.
5. **Fencing.** Before new record/state write/renew/group advance current generation checked; stale worker cannot mutate newer state. finally releases only own resources. Sent bytes не recall.
6. **Monotonic lease.** Renewal only current unexpired owner before operation deadline; never extends plan absolute TTL. Server TimeProvider elapsed time/timers; execution lease отдельный от MVP authorization lease.
7. **Receipt wait.** Current worker can renew while waiting, but finite receipt deadline bounds silence. Timeout interrupts execution/recovery, not proves invalid plan.
8. **Receipt facts.** Plan receipts independent of worker lease, existing body unchanged. First receipt only next unconfirmed offered group; Offered fixed atomically under current fence before boundary send. Identical prior receipt sameACK; endpoint independent from Data lease.
9. **Routes.** POST /api/v1/content-synchronizations/{id}/resume,/stop,/discard; POST /api/v1/content-synchronizations/freshness. Control JSON ApiResponse<T>.
10. **Control input.** planId + expectedExecutionGeneration required. Resume also originalRequestFingerprint/originalRequestProfile from durable context. Stale command409 doesn't affect new execution; SDK no fresh-generation retarget.
11. **Lost control ACK.** Bounded receipts keyed action/planId/expected generation/meaningful input; identical repeat original result without mutation. After receipt eviction stale fence still rejects. Original result snapshot not fresh Status.
12. **Preparing.** PlanId allocated atomic reservation, Status exposes it before ready plan metadata. Stop retains original input/cancels preparing worker; Resume can finish first preparation when ready plan never existed. Doesn't recapture ready immutable target.
13. **Resume.** Checks identity/original request/eligibility; enables stopped/idle, competing live worker409. Doesn't open/reserve stream; Data acquires. Completed retained plan permits terminal recovery.
14. **Stop.** SDK closes reading/transport but retains staged records/cursor/installed groups. Server revoke generation→stopped preserving valid plan. Started short local atomic commit may finish; receipt saved, ACK deferred until Resume allowed.
15. **Discard.** Stop execution and resolve local atomic outcome first; remove unfinished attempt/staging only, keep installed/inventory/committed receipts. Unknown local commit postpones destructive cleanup. Server bounded discarded tombstone prevents resurrection.
16. **Expiry / capacity.** Absolute deadline from reservation default1h/no extension for preparing/streaming/stopped. Active plans not arbitrary capacity-evicted; new Start unavailable if no capacity. Deadline still ends active work; after tombstone eviction/restart missing404.
17. **Local concurrency.** One high-level sync stable store scope across SDK instances, second call rejects before newID. Local run generation scopes callbacks/parser/control outcomes. Actual cross-process ready-store lock mechanism tested Q29/Q32.
18. **Recovery order.** Durable context/receipts→Status→pending server receipt repeats(no local reapply)→Data checkpoint. Own commits don't change original fingerprint. Timeout/409/unknown outcome not newID proof.
19. **New plan.** NewID only proven old unavailable/expiry/discard/invalidation or new explicit selection intent. Actual inventory authoritative; staging no blind reuse. Durable terminal with unknown HTTP end may replay terminal suffix/dedup.
20. **Freshness.** Required full selection/actual installedInventory. Result outcome upToDate/changesAvailable, checkedAt (UTC seven fractional digits Z), changedGroups sorted exact pairs. No plan/lease creation. Authority failure error→SDK Unknown; doesn't undo successful sync or auto-trigger next.
21. **Timeout stages.** Separate unary/header/read-idle/record-assembly/server-write-backpressure/preparation/receipt-wait budgets. ResponseHeadersRead HttpClient timeout only until headers; stream body separately bounded.
22. **Expected pause.** Read-idle suspended during local validation/atomic commit/receipt POST, respective stage budgets+server watchdog remain. No heartbeat/empty lines. First Demo stream HTTP compression disabled; enable only after actual incremental checks.
23. **Configuration / retry.** Durations/renew interval/buffers/capacity/tombstone retention from startup-validated config; numeric sizing after representative/load tests. renewInterval<leaseDuration fixed. Bounded transient retries/backoff/jitter only; no corruption/validation/confirmed-invalidation/fresh-ID fallback.
24. **Acceptance.** Controlled-clock/barriers: expired takeover, late writer/finally/Stop/Discard, offered receipt vs Stop/expiry, crash/local commit/lostACK/preparing resume/tombstone eviction/capacity/clock change. Generic SDK/real CM checks имеют отдельное evidence выше; actual Unity Player должен подтвердить incremental headers/generation/stalls/cancel/buffering/reconnect с прежним plan/cursor на разрешённом HTTP/1.1. Future H2/H3 требует собственной проверки. Docs/literal fixtures не являются actual execution proof.

### После согласования execution

Модель исполнения и control/Freshness semantics согласованы. Public SDK/store contract выбран Q29/F19; остаются actual adapter/provider/platform/lifecycle conformance Q32, literal machine reason registry/Q15 mapping, numeric configuration/spike results и реальная conformance matrix. Ни authoritative authorization/session lease, ни SDK cross-process store implementation не объявлены доказанными этим решением. [TimeProvider](https://learn.microsoft.com/en-us/dotnet/standard/datetime/timeprovider-overview), [ResponseHeadersRead](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpcompletionoption?view=net-10.0).

## Неизменившиеся принятые гарантии

| Область | Принятое поведение |
| --- | --- |
| Logical identity | synchronizationId генерирует DotNet для одной операции; прежний ID сохраняется при reconnect/resume на разрешённом HTTP/1.1 или отдельно подтверждённых иных версиях |
| Plan | Immutable exact target/original fingerprint; новая обычная публикация его не меняет, latest не выбирается заново во время stream |
| Atomicity | Data + removals + installed inventory + durable local receipt одной exact schema/version group фиксируются вместе; partial success только между группами |
| Durable recovery | Контекст/валидированные staged records/checkpoints/receipts переживают закрытие процесса; SDK даёт interface + ready Demo adapter, SQLite первый кандидат со spike Q29/Q32 |
| Default resume | SDK автоматически продолжает пригодный старый plan; при доказанной невозможности создаёт новый ID/plan от фактического inventory |
| Client concurrency | Второй прикладной вызов одного локального набора отклоняется до генерации нового ID; queue/join не default |
| Server concurrency | Конкурирующий активный Start того же ID получает 409; Status/Resume/receipt reconciliation не второй Start |
| Cache | Один Demo CM process, bounded in-memory plan store; абсолютный TTL от создания, default 1 час, configurable, без автоматического продления stream. Restart CM теряет plan: SDK начинает новый от actual inventory; persistent server resume-after-restart потребовал бы другой adapter |
| Stop / Discard | Stop прекращает network reading, сохраняет пригодный checkpointed staging и installed groups; Discard очищает только unfinished context. Начатый короткий atomic commit допустимо закончить |
| Assets | Отдельный Assets API; bytes всех assets не входят в stream и не условие успеха data sync |
| Freshness | После полного успеха, включая no-change, отдельный preflight; UpToDate/ChangesAvailable/Unknown — логические исходы, не выбранный enum. Следующую sync после успеха выбирает приложение |

## Records, application cursor и durable checkpoint

Полный LF-delimited record разбирается и проходит identity/integrity checks, затем устойчиво сохраняется в staging. Только после этого продвигается durable checkpoint/cursor. Network fragment/callback/bytes received либо parsed-but-not-saved record не подтверждают продвижение. Maximum record size и bounded buffers configurable; числовые пределы выбираются representative fixtures/load tests.

Cursor opaque, связан с immutable plan и sequence последнего полностью durably staged record; не byte offset и не local group commit. Resume продолжает после этого cursor. Одинаковый replay sequence/identity/payload безопасно дедуплицируется; та же sequence с другим payload — protocol error. Незавершённая группа остаётся старой в installed store. Новые publications не заменяют pinned versions; unknown/malformed/unreadable record — ошибка, не пустая successful group. Base64URL/HMAC versioned codec binding принят ниже; candidate cursor входит в обычный record, durable record/cursor атомарно сохраняет SDK.

## GroupCompleted, local commit и server receipt

GroupCompleted означает конец выдачи группы, а не подтверждённую установку. SDK проверяет полноту/TargetGroupHash либо согласованную integrity representation, исходный local basis при необходимости; атомарно применяет data/removals/inventory и сохраняет durable receipt. После этого отдельно отправляет server commit request с target hash и выбранным plan/group/receipt context.

Server receipt нужен для plan/recovery/diagnostics; его identity — PlanId + SchemaVersionId + TargetGroupHash. Идентичный повтор подтверждает прежний результат, другой hash для того же plan/group — conflict. Receipt не доказывает filesystem клиента. При lost ACK SDK читает durable local receipt, сверяет status и повторяет server receipt; local apply не повторяется. Actual InstalledInventory — источник следующего нового plan; свои commits не меняют original request fingerprint.

После GroupCompleted сервер **ждёт отдельный commit request** перед следующей группой. Поток остаётся открыт во время local validation/atomic commit/POST receipt. Receipt endpoint не блокируется lease активного data stream. Ожидание отменяемое, timeout configurable; потеря stream/timeout оставляет recovery, а не rollback committed данных. Stream success/EOF не заменяет local commits/receipts; terminal invalidation/truncated transfer не full success.

## Resume, expiry и invalidation

SDK проверяет прежний ID/plan identity, selection, совместимость actual inventory с выполненными собственными commits и пригодность staged checkpoint. При доступном valid plan продолжает data request от application cursor. Timeout/status outage сами по себе не доказательство expired или invalid; нельзя обходить active409 новым ID.

При доказанном отсутствии/expiry/invalidation SDK сохраняет committed группы и строит новый plan по actual inventory с новым ID. Staging reuse только после exact identity/content проверки. Absolute TTL не продлевается stream; expiry прекращает выдачу неполной группы и не откатывает уже local committed данные. Restart CM уничтожает in-memory plan, не client installed store. Active data writer использует lease/generation token; old-generation writer отвергается по current fence; receipts проверяют retained plan/next unconfirmed offered group и accepted replay независимо от worker lease. Точный fencing API/storage lifecycle — инженерная материализация выбранных гарантий.

Перед выдачей exact content повторно проверяется safety-sensitive доступность. Withdrawal блокирует старый plan: affected incomplete group не commit-ится, previous committed groups сохраняются, SDK строит новый от actual inventory. После headers server отправляет OperationInvalidated record либо обрывает transfer, если record невозможно корректно отправить; оба исхода исключают full success. Обычный error envelope не вставляется в середину NDJSON; точные reason codes/status mapping — contract materialization.

Freshness — отдельный read-only preflight по полному Selection + actual InstalledInventory: успешные UpToDate/ChangesAvailable, failure на клиенте Unknown. Не создаёт plan и не запускает endless sync. Retry разрешён для transport outage и согласованных transient failures; validation/hash/lifecycle failures не повторяются автоматически. Status/control/Freshness DTO/exposure закреплены в F13; numeric defaults/backoff/timeout, reason registry и actual adapters материализуются до интеграции.

## Progress и HTTP profile Demo

Прогресс вычисляет DotNet по plan metadata, полностью проверенным/durably staged records, validation и local commit/receipts. Network byte count не durable progress; неизвестный total byte-size не даёт честного byte percentage. Stage names/units/callback threads — Q29, числовые counts/profile — F13/Q15/Q19. Отдельный SSE observer и event cursor не нужны.

Для Unity 2022.3.62f3 Windows x64/IL2CPP Demo явно разрешён HTTP/1.1. H2/H3 не являются обязательной приёмкой этого Demo и не объявляются supported без отдельной матрицы. Incremental headers/body, bounded memory, generation, cancellation/quiescent cleanup и reconnect сохраняют прикладные ID/plan/durable checkpoint независимо от разрешённой версии HTTP. UnityWebRequest не раскрывает negotiated version, SDK её не фабрикует. TLS failure/malformed contract/domain denial не обходятся protocol fallback.

## F13: wire protocol и execution ownership

[F13](../features/F13-content-delivery.md) — единственный server contract. DF05 теперь выдаёт resumable data stream; отдельного DF04 SSE нет. ID остальных flows сохранены для ссылок. Модели record roles описаны у F13; public framing/camelCase kinds/payloads/cursor1/normal1..N/invalidation0/status/receipts выбраны в Public wire contract v1 и model tables F13; execution/control profile выше согласован; public SDK/store signatures/physical settings и actual conformance implementation. Примеры из нового источника не назначают готовые classes/defaults.

| Область | Оставшаяся детализация |
| --- | --- |
| Wire framing | NDJSON application/x-ndjson UTF8/LF/camelCase known payloads required fields fixed; literal sync-wire-v1-vectors проверены, actual incremental SDK/Unity/proxy adapter conformance ещё нужен |
| Records | Шесть explicit variants приняты; Remove отдельный record; exact boundaries/count/hash payload и reason codes конкретизируются fixtures |
| Cursor | Binary cursor1=byte version+network UUIDs+Int64BE+HMAC32:73bytes/98chars Base64URL; literal positive/negative vectors checked; actual codec/binding integration ещё implementation |
| Streaming | Server waits receipt before next group; configurable wait timeout/record size/buffers, flush/compression/cancel cleanup проверяются на реальном пути |
| Plan store | Bounded in-memory один Demo process, absolute reservation TTL default1h/no extension/restart loss приняты; eviction/fencing implementation проверяется |
| Ownership | Atomic reservation/lease/generation fencing; old writer rejected, receipt endpoint independent от stream lease; concrete API/lifetimes — implementation |
| Receipts | PlanId+SchemaVersionId+TargetGroupHash, identical replay success/different hash conflict; lostACK/status/expiry scenarios должны быть проверены |
| SDK storage | SDK interface + ready adapter; SQLite first candidate Q29/Q32, stage/checkpoint и installed+inventory+receipt transactions; single-writer recovery tests |
| Freshness | Read-only full selection/actual inventory, UpToDate/ChangesAvailable, client Unknown; POST freshness/result outcome/checkedAt/changedGroups selected; literal reason registry/OpenAPI/adapters require implementation |
| Retry / infrastructure | Transient-only retries согласованы; numeric defaults, proxy/LB limits и exact Unity target/Player/transport compatibility требуют configuration и spike |

## F19: проверка SDK и server boundary

[Q19](Q19-content-hashes.md#принятый-canonical-profile-v1) фиксирует group/inventory/request/plan profiles и SHA-256 exact canonical bytes/64 lowercase hex. Required profile metadata проверяется SDK до применения группы; unknown/missing profile не downgrade/retry по guessed serializer. LF NDJSON и внешняя упаковка не входят в nested content digest. Exact typed schema/value metadata supplies FieldId ordering/numeric kinds без нового Registry runtime read. Full target membership verified after upserts/removals before installed+inventory+receipt atomic commit; initial request fingerprint/profile не меняются own commits. [Literal samples](../canonical-v1-vectors.md) не проверяют HTTP framing/token/Unity transport; remaining Q33 literal envelopes/cursor codecs/configuration сохранены.

[F19](../../frontend/features/F19-unity-client.md) выполняет stream parsing/staging/checkpoint/atomic apply/receipt/recovery и local progress. Серверные models/handlers не дублируются в SDK. Record receipt, cursor, stream completion, local installed result и server ACK — отдельные факты. Устойчивое состояние клиента нельзя заменить SSE replay или in-memory buffers.

Обязательные будущие проверки: arbitrary network-fragment boundaries/UTF-8 split, partial/malformed oversized record; restart после durable stage и до checkpoint; crash до/после group commit; lost Start/receipt response; duplicate records/receipts; Stop до/во время commit; Discard без удаления installed; TTL/withdrawal during stream; reconnect на разрешённом HTTP/1.1 с прежним plan; no-change и freshness outage; bounded-memory large dataset; реальный Unity/CM/proxy route. Проверки требуют выбранного framing и реального storage/transport; doubles не доказательство этой совместимости. Результаты выполненных generic/CM checks указаны выше; остальные native Unity/Player и реальные fault/load случаи остаются gates, без общего claim всей матрицы.

## Literal wire fixtures и граница проверки

[Sync wire v1 vectors](../sync-wire-v1-vectors.md) содержат exact canonical digests, add/removal/nochange NDJSON, terminal invalidation0 и signed cursor bytes/tokens на public fixture-only key. F13 остаётся единственным owner models/fields; fixtures не второй editable contract. Source /1 canonical codecs не меняются: contentJson unescape restores original bytes, extra wrapper escaping/LF outside digest. Full canonical plan groups retain unchanged targets, Start/execution summaries only changed groups; base hash derived original installed subset, same group profile.

Cursor has fixed binary version1/UUIDnetwork16+16/Int64BE8/MAC32, signing first41bytes, total73/Base64URL98nopad. ProtocolVersion1 отдельный от package/hash/codec version. Independent fixture calculations/checks не доказывают existing API/parser/cache/SDK/SQLite/Player implementation. DTO adapters/error reason registry and actual conformance materialize before integration; lease/fencing/control/Freshness/stage budgets selected above; numeric sizing and actual conformance remain implementation.

## Техническая проверка первичных источников, 30.09.2026

Ниже результаты внешней проверки, отдельно от ответов пользователя. Практический стенд Unity/CM не запускался.

| Источник | Что установлено | Чего это не доказывает |
| --- | --- | --- |
| [WHATWG: Server-Sent Events](https://html.spec.whatwg.org/multipage/server-sent-events.html) | SSE использует UTF-8 text/event-stream; есть поля id/retry и механизм Last-Event-ID для восстановления соединения EventSource | Серверный durable replay, сохранение клиентской порции и атомарность прикладного commit не обеспечиваются самим форматом |
| [RFC 9114: HTTP/3](https://www.rfc-editor.org/rfc/rfc9114.html) | HTTP/3 работает поверх QUIC; протокол поддерживает независимые потоки и не превращает завершение соединения в доказательство исхода прикладного запроса | Нет автоматического восстановления долгой бизнес-операции после утраты соединения/состояния; выигрыш скорости конкретной нагрузки нужно измерять |
| [Unity 2022.3 UnityWebRequest](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Networking.UnityWebRequest.html) | API предоставляет asynchronous request/send/abort/response/header operations | Документация API не доказывает HTTP/2 или HTTP/3 capability выбранного 2022.3.62f3 Player; Demo разрешает HTTP/1.1 |
| [Unity 2022.3 DownloadHandlerScript](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Networking.DownloadHandlerScript.html) | API передаёт принятые data в callback пользовательского handler | Callback сам не доказывает headers-before-isDone, bounded native memory, parser, cleanup или H2/H3 support |
| [Kestrel HTTP/3, .NET 10](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/http3?view=aspnetcore-10.0) | Для HTTP/3 важны поддерживаемая ОС/QUIC-библиотека и конфигурация сервера; Microsoft рекомендует сохранять другие HTTP-версии | netstandard2.1 SDK и включение HTTP/3 в Kestrel не доказывают его поддержку в Unity и на всём пути через proxy |
| [RFC 9110: 409 Conflict](https://www.rfc-editor.org/rfc/rfc9110.html#name-409-conflict) | 409 обозначает конфликт запроса с текущим состоянием ресурса | Это не стандартное правило «любой повтор одного ID обязан быть ошибкой»; семантику Start/Resume/Status нужно определить в CM |

HTTP/3 и QUIC не два независимых обязательных канала. SSE является форматом потока HTTP-ответа; сам по себе SSE не требует HTTP/3. В одной серверной событийной ленте не появляются автоматически независимые прикладные потоки для каждой схемы. Нельзя обещать ускорение только из-за названий протоколов.

## Числовой профиль Demo F13

По ответу владельца выбран стартовый настраиваемый профиль одного процесса. Host configuration `Delivery:Synchronization` обязательна; отсутствующие, неположительные, переполняющие timer или несовместимые параметры отклоняют startup.

| Настройка | Начальное значение |
| --- | --- |
| PlanRetention | 1 час, абсолютный от reservation |
| MaxActivePlans | 32, без вытеснения активных планов |
| MaxControlReceipts | 256 на retained operation |
| MaxTombstones / TombstoneRetention | 128 / 1 час |
| LeaseDuration / RenewInterval | 30 с / 10 с |
| PreparationTimeout | 60 с |
| ReceiptWaitTimeout | 5 мин |
| WriteBackpressureTimeout | 30 с |
| MaxRecordBytes / MaxBufferedBytes | 8 MiB / 16 MiB |

RenewInterval меньше LeaseDuration. MaxRecordBytes учитывает UTF-8 JSON framing и LF; MaxBufferedBytes не меньше двух record buffers. Writer использует ограниченные сегменты без geometric allocation growth и whole-response buffering. Это пределы transport buffers, не обещание общего heap ceiling либо измеренной вместимости PostgreSQL/source metadata. Monotonic absolute/stage deadlines проверяются независимыми timers; поток и ожидание receipt не продлевают TTL. Значения остаются доступными разработчику для настройки и требуют representative/load проверки для другого окружения.

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

## Проверка Unity transport с реальным CM

11.10.2026 реальный Windows-user CM host с PostgreSQL17.6/MinIO проверен через UnityWebRequest в Unity6000.6.5f1 Editor. Подтверждены Add двух exact версий, Update одной группы, Remove/deselection/Withdraw, no-change UpToDate и committed reads после повторного открытия native SQLite. Gateway HEAD и полная verified image download также прошли. Два real-CM tests входят в прошедший полный PlayMode suite 5/5; [F19](../../frontend/features/F19-unity-client.md#проверка-unity-sdk-с-реальным-cm) фиксирует scope и cleanup. Lost Start/ACK, restart незавершённой попытки, Stop/Discard, Range failures и timeout/queue/cancellation matrix не проверялись этим новым Unity прогоном. Wire variants, profiles, retries и server deadlines не менялись. Backend/Unity/собственные контейнеры и запущенный Docker Desktop выключены.
