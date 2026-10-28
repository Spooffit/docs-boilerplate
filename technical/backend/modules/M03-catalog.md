# M03. CatalogAccess

## Назначение

CatalogAccess является anti-corruption layer между VR School и Content Manager.

BC хранит локальные projections канонического каталога и лицензионных фактов, принимает state-carrying snapshots через RabbitMQ/MassTransit, обеспечивает source freshness/idempotency и владеет конфликтами применения внешнего каталога.

VR School остаётся source of truth. CatalogAccess не предоставляет CRUD source entities и не читает VR School DB.

## Backend-фичи

| ID | Ответственность |
| --- | --- |
| [F06](../features/F06-product-catalog.md) | Product/ProductVariant projections, snapshot consumers, read API |
| [F15](../features/F15-catalog-conflicts.md) | Persisted move conflicts, operation blockers, manual resolution |
| [F12](../features/F12-content-entitlement-policy.md) | License/grant projections как вход entitlement; policy ownership остаётся Publishing |

## Domain model

Основные roots/state:

- `Product`;
- `ProductVariant`;
- `CatalogConflict`;
- License/grant projection roots по F12.

Source projections имеют:

- source identity;
- `SourceVersion`;
- нормализованный lifecycle Active/Archived; Hidden относится только к presentation, Pending/Conflict — к обработке;
- внутренний `Version` для local optimistic concurrency.

Логическая per-entity source version маппится в `SourceVersion`; V2 fields selected; V1 EntityRevision/IsDeleted transition explicit checked adapter/rollout [Q23](../questions/Q23-source-synchronization.md#принятый-протокол-каталога-v2). Не смешивать source ordering с HTTP ETag/internal Version. CM не предоставляет local source Archive/Restore: lifecycle меняется только полным более новым snapshot VR School. Product Archive ограничивает новые операции Variant без изменения их own lifecycle; Restore допускает новый use только own Active и без иных blockers.

## Integration

[F06](../features/F06-product-catalog.md) defines ProductSnapshotV2/ProductVariantSnapshotV2, normalized source values, CatalogSourceState/CatalogEventReceipt/CatalogWorkItem/CatalogIngestionFault and safe processing reads once. Source V2 EventIdGuid/ContractVersionint/IdGuid/Versionpositiveint/UTC OccurredAt/LifecycleActiveArchived/Name/Description?<=500; VariantProductIdGuid. ProductName1–120/Variant1–200 normalized before source commit, CM validates/preserves/nonunique identity. V1 long/IsDeleted rollout explicit checked range/continuity, no silent rename or License/history/ProjectStorage migration.

VRSchool source state+outbox → independent CM queue → receiver FIRST own MVP gate acceptance(candidate/watermark+receipt+durable work)→ACK acceptance NOT apply → worker NEW firstgate/currentaccepted/fault/fulldependencies/claim+boundeddeadline → own apply+pending/F15/history/workcompletion+necessary F18 intent.

One newest mutable payload owner receive-state→unresolved Variant→Open.PendingState, atomic handoff; work refs/version only, immutable receipt evidence not second target. Accepted22 prevents apply21 before22 processed. New target sameOpen, old Issues bound oldSourceVersion stale until reassessed; missingparent retainswaiting/Open/noTTL/fakeProduct, wholly firstAppliedState absent/otherwiseintact. Parent commit durable wakeup+scan/expiredlease reclaim prevents lost wakeup. Compatible currentSourceChangedApply fullapply+Resolved atomic, no publish/historyrewrite.

Same EventId compares admitted immutable identity/version/time/payload, excluding retry headers: changed evidence quarantined, ordinary identical replay returns original receipt. Explicit unchanged UnsupportedContract recovery after profile repair is checked before terminal replay; original evidence remains immutable. New EventId/equal Version compares source IDs/version/parent/name/Description/lifecycle, excluding event clock: equal no-op, contrary quarantine. Lower-than-watermark input is stale; configurable receipt/completed-work cleanup retains current/unresolved state and conflict history.

Reliable higher invalid/unsupported source snapshot creates scoped CatalogIngestionFault unreadiness separate validacceptedwatermark; fresh Catalog/Publishing gates consult it. No F15/Withdrawal/globalblock/guessedentity; missingEventId with reliable entity+higherVersion still scopedfault/private evidence without fake source ID/receipt. Newer sourcefix or unchanged unsupported replay after profile repair, never mutatepayload underEventId/bypass activecoveringfault(candidate<=faultVersion); ordinaryfix strictlynewer, equalfault only exactunchanged unsupportedreplay. Transient/missingparent/businessconflict distinct.

Protected VRS operational reconciliation selectedIDs/allCURRENT, includesparents for selectedVariants, newEventId samecurrentVersion. Scheduling/report not CMready, absence notDelete; no contiguoushistory/globalsequence/bootstrap. CM replicas competeownqueue; ProjectStorage independentqueue. Exact route/permission/wire/packages/topologynames/numericlease/retention engineering [Q23](../questions/Q23-source-synchronization.md#принятый-протокол-каталога-v2)/[Q20](../questions/Q20-persistence-and-migration.md#catalog-v2-receive-work-и-fault-boundary).

Raw activation key not broker; Q16/F13 independent HTTP validation.

## Меж-BC зависимости

Publishing/Delivery читают CatalogAccess только через public Application query-ports.

CatalogAccess при Variant move получает impact зависимостей через port/adapters поверх публичных contracts Publishing; прямой доступ к Publishing persistence запрещён.

CatalogConflict блокирует конкретные операции authoritative affected closure, не весь каталог. Move blockers относятся к реально несовместимому mutable/in-flight состоянию; immutable artifacts сами по себе blockers не создают. Missing override target A сохраняет local B; independent prospective B плюс A → B образуют concrete effective duplicate.

Publishing владеет отдельной corrective repair command Variant working patch: удалить conflicting rule/local addition, eligible replace уже проблемного result, inherited exclusion для устранения blocker; intrinsic/new-use/scope/no-expansion guards обязательны. Ordinary Save остаётся blocked, ignoreConflict/alternate ProductId запрещены. Applied/prospective/newest-parent/proposed-repair preview отделён от обычного applied workspace. Other blockers снимаются explicit owning action либо VR School correction.

Repair binds ConflictId + newest SourceVersion + captured repair basis и собственный patch If-Match; concurrent source/patch change отвергается. Publishing repair commit и CatalogAccess Resolve commit отдельны: fresh source/dependencies, full apply + Resolved history атомарны только внутри CatalogAccess. Committed repair v21 сохраняется при v22, старое основание rejected; missing parent — durable waiting. Foreign DbContext/shared transaction запрещены; каждый из двух commits отдельно acquires принятый global transaction advisory key до fresh reads и releases после commit/rollback, две prechecks недостаточны. Resolve не публикует; activation/promotion/late approval recheck current affected scope/basis.

Archive допускает read/retain unchanged/remove/eligible problematic replacement как ограниченное обслуживание без expansion; запрещает new release/revision/Activate/Rollback/promotion. При Conflict применим только отдельный repair scope. Archive/Conflict/Restore не выполняют automatic Unpublish/Withdrawal/revoke/activation/unwithdraw/grant. Delivery по существующим materialized revisions и confirmed entitlement, включая new sessions, сохраняется при независимых guards F12/F13; explicit safety Unpublish/Withdrawal доступны с обычными permissions/preconditions.

## Persistence

MVP использует [утверждённый consistency guard](../questions/Q20-persistence-and-migration.md#mvp-протокол-consistency-guard): одна PostgreSQL database для всех CM replicas, один стабильный глобальный exclusive transaction-level advisory key. Owning BC начинает явную ReadCommitted transaction и standalone pg_advisory_xact_lock на её же physical connection до любых authoritative reads/row locks; затем fresh reload own roots и foreign facts через public ports в независимых primary read scopes/connections. Owner commit атомарен только для своих roots + outbox/audit intent/inbox при необходимости; foreign ambient UoW enlistment/shared SQL transaction/EF access/cache/replica reads запрещены. Host/DI обязан разделить owner UoW и foreign reads: current multi-context EfCoreUnitOfWorkParticipant этого автоматически не гарантирует.

Все invalidating writers участвуют, включая reference creation/removal/Copy/orphans, Publish/lifecycle/hard-delete, source acceptance/watermark/pending/apply/retries/setup/reconciliation и F15 open/supersede/repair/resolve. Own ETag сохраняет412, changed foreign basis даёт409; unchanged source Create не повышает source Version. External I/O/user wait/long streams вне gate; подготовка CPU вне gate требует basis recheck внутри. Wait bounded/configurable/cancellable, unavailable read не empty, connection loss abort без reconnect-continue, lost commit ACK → unknown outcome и inquiry/operation ID Q15. Domain не содержит SQL guard types; exact port/DI/mappings/error codes/numeric defaults остаются своими Q.

Durable acceptance source v22, даже до impact/parent processing, участвует в gate и фиксирует newest evidence; pending resolver fresh rechecks accepted version/watermark и не применяет v21. Single target ownership/two durable stages/receipt-work-fault semantics selected; concrete physical adapters/mappings Q20. Broker/VR School I/O выполняются после release; operational setup/reconciliation используют те же guarded CM mutation stages.

PostgreSQL schema `catalog_access`, собственный DbContext и migrations.

Ожидаемые tables:

- `products`;
- `product_variants`;
- license/grant projections F12;
- `catalog_conflicts`;
- own CatalogSourceState/event receipts/durable work/ingestion faults, integrated with chosen MassTransit/LeanSoft receiver;
- pending/version-bound assessment and F15 history; no independent second target.

Target pending layout допускает unresolved ProductVariant без существующего Product и не использует обязательный физический FK для pending parent. Физический mapping applied/pending, indexes и migrations остаётся Q23/Q20; это не утверждение, что текущий required FK уже снят.

## API

Read-only source catalog:

- Products;
- ProductVariants;
- Licenses/grants diagnostics по их feature.

Mutable API CatalogAccess относится только к локальному conflict Resolve workflow F15 и использует exact own ETag/If-Match. Scoped repair принадлежит Publishing; preview read-only. Authorized specialist permissions по F16/Q10 и separate committed repair/Resolve audit F18; admin-exclusive доступ не вводится. Exact repair/preview routes/CLR contracts — Уточняется (Q15).

## Связи

[Q11 — ownership](../../../business/questions/Q11-catalog-ownership.md) · [Q23 — source synchronization](../questions/Q23-source-synchronization.md) · [Q25 — conflict semantics](../../../business/questions/Q25-catalog-conflicts.md) · [S22 — EventBus topology](../../../catalog/docs/standards/dotnet/masstransit-rabbitmq.md#optional-three-project-eventbus-composition).
