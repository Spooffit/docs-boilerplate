# M04. Publishing
## Назначение

F07/F08 владеют schema selection rules с fixed Published target и scoped inclusions/exclusions. F09 выполняет captured resolution и immutable snapshot под общим MVP gate Q20; F10 использует frozen refs/replacement-mask без live expansion. Модели и алгоритмы находятся в feature-документах; открытые carriers/migration — [Q39](../../../business/questions/Q39-schema-content-selection.md).
Publishing превращает mutable authoring assignments в immutable publication artifacts и управляет выбором текущей публикации.
Он отвечает за:
- общий Product assignment;
- local patch ProductVariant;
- immutable releases;
- materialized PublicationRevision;
- Product/Variant publication state и activation history;
- automatic promotion;
- versioned ContentEntitlementPolicy.
Publishing **не** отвечает за фактическую клиентскую доступность. Это состояние фиксирует Delivery после собственной materialization.
## Карта фич
| ID | Ответственность |
| --- | --- |
| F07 | mutable ProductContentAssignment |
| F08 | mutable ProductVariantContentAssignment |
| F09 | immutable Product/Variant releases |
| F10 | immutable materialized ProductVariantPublicationRevision |
| F11 | Product base, activation, rollback, unpublish, automatic promotion |
| F12 | Policy states/history + immutable exact planned-period PolicyAgreement; Delivery consumes confirmed promise |
CatalogAccess F06/F15 является внешней read dependency, а не частью Publishing.
## Aggregate boundaries
### Mutable authoring
`ProductContentAssignment`
- identity ProductId;
- exact Published content references;
- internal Version/ETag.
`ProductVariantContentAssignment`
- identity ProductVariantId;
- additions;
- overrides;
- schema/instance exclusions;
- internal Version/ETag.
Assignments не являются историей и не влияют на клиентов напрямую. Save сохраняет unchanged exact references, ставшие unavailable, с ReleaseBlockers; new position/replacement требует current eligibility. Inactive/archived owner допускает только semantic removal/eligible replacement без расширения; OPEN F15 блокирует ordinary maintenance writes. Saved working composition не обязательно release-ready.
### Immutable snapshots
`ProductContentRelease` и `ProductVariantContentRelease` фиксируют assignments.
Content release immutable; mutable только lifecycle Available/Retired с отдельным internal Version. Explicit empty Product set и entirely empty Variant patch allowed; intentional new-ID duplicate Create produces new artifact ID even identical content/comment. Semantic previous — latest-created same-owner ReleaseNumber incl Retired, exact nullable captured ID; later Create alone не replaces baseline. F09 Create повторно проверяет current new-use всех own references. Variant Create/review требует один и тот же explicit ReleaseValidationContext: exact immutable ProductRelease либо explicit EmptyBase; own patch If-Match и composite review basis защищают разные условия. Live assignment/current ActiveReleaseId/fallback empty не становятся validation context. Chosen context хранится как historical provenance, не hard dependency; Variant release остаётся reusable patch-only и F10 заново валидирует фактически выбранные sources.
### Materialized revision
`ProductVariantPublicationRevision` хранит exact source releases и полный materialized final content.
Historical read никогда не rerun-ит merge. F10 требует ≥1 exact source и final nonempty; source Available/owner/trusted target/exclusion evidence отдельно. New-use и F17/F11/F13 runtime restrictions проверяют final effective instance/schema/media closure: suppressed unavailable A не blocks valid B, shared final dependency B blocks. Retained source/evidence closure шире и сохраняется.
Lifecycle:
- Available;
- Retired;
- Withdrawn.
Withdrawal terminal для server distribution.
### Publication state
`ProductPublicationState`
- current Product base release;
- internal Version.
`ProductVariantPublicationState`
- current Publishing ActiveRevisionId?;
- LastSequence;
- internal Version.
`ActivationRecord` append-only и упорядочен Sequence внутри Variant.
### Policy
Product/Variant own mutable policy states/immutable history remain separate. Publishing additionally owns immutable [PolicyAgreement F12](../features/F12-content-entitlement-policy.md#21-aggregates-entities-и-value-objects), captured current policy/applied parent under MVP guard **before** VRS confirmation. Agreement commit is promise checkpoint; source confirmation/processing/first use/date-as-of cannot replace it. Exact license+Variant+planned SourcePeriodId/original parameters/nontransferable scope retained, System Freeze has no fictive version.
## Composition
ProductVariant composition:
1. ProductRelease base;
2. schema exclusions inherited layer;
3. instance exclusions inherited layer;
4. одновременно подавить override targets исходного inherited слоя;
5. каждый exact override result один раз, затем local additions;
6. collision validation;
7. immutable PublicationRevision.
Authority merge identity — EntityInstanceId.
Deep JSON merge отсутствует. Results одного override не становятся targets другого; порядок массива не меняет результат. Cycles отдельно не запрещены при unique targets/results и выполненных остальных guards. New authoring override target только current inherited position; persisted absent-target intent сохраняется. Live-only collision даёт ReleaseBlockers конкретного preview, но intrinsically valid patch Save допускается; selected F09/F10 context collision блокирует соответствующий Create.
## Moderation boundary
F17 проверяет exact immutable ProductVariantPublicationRevision.
Manual:
```text
Revision → Moderation → Activate
```
Automatic:
```text
Product promotion
→ per-Variant Revision + own Pending Moderation + exact item tuple (one Publishing commit)
→ human Approval
→ automatic Activate
```
Approval не хранится как mutable content field PublicationRevision. F17 moderation живёт внутри Publishing BC как отдельный aggregate/process; F11 читает approval через публичный application contract F17, без отдельного StaffWorkflow BC.
## Automatic workflow ownership
F11 immutable operation captures exact Product release/resulting Product state Version, applied member set и each local latest-created release/Variant Sequence once. Retired latest blocks без fallback; never-release nonempty working groups blocks, empty existing local release counts as release; zero-member operation valid. Later members/moves/releases не заменяют inputs.

Each PromotionItem имеет собственные Version/stage/outcome/exact RevisionId+ModerationId; VariantPromotionIntent хранит internal current ItemId/own Version. New accepted operation supersedes older unfinished heads even same base; actual manual/active Withdrawal transition clears head atomically, manual no-op preserves it. Exact CancelAutomaticItem не отменяет replacement head и не пишет fake history. Technical retry same inputs/tuple; domain repair retry — новая operation/subset/confirmation/RetryOf.

Prepare commits own F10 revision+F17 Pending+exact tuple/AwaitingModeration together; no required PreparedEvent→separate Submit correctness dependency. Continuation requires exact bound Approved iteration, current head and unchanged captured Product Version/Variant Sequence (ABA fences), fresh context/F15/lifecycle/final closure. Withdraw iteration cancels exact bound item; new manual iteration doesn't reattach. Cancelled/Superseded не autoresume after Restore/Resolve/event; retained Approved artifact remains manually usable with fresh guards. Activated != Delivery ready.

Public CreateRelease/CreateRevision/StartPromotion/Submit require body ClientOperationId. Auth → MVP gate → canonical [CommandExecution receipt lookup](../questions/Q20-persistence-and-migration.md#publishing-workflow-capture-и-продолжение) → only new execution preconditions → own business+original receipt commit. Same bound ID/input replays original result even changed source/Retired artifact; changed input409; no Restore/Submit/Activate replay. Wire/error/privacy profile [Q15](../questions/Q15-api-and-source-code.md#publishing-command-execution-и-clientoperationid).

Admin Approve confirms shown Delivery effective comparison basis independently of own iteration ETag; current drift409 means refresh/new consent, unavailable != empty. Later diff basis change alone does not revoke committed Approval; automatic activation checks own head/ABA/current eligibility basis.

## Delivery boundary
F11 после committed activation/unpublish/withdrawal публикует internal event:
```text
ProductVariantPublishingStateChanged
  ProductVariantId
  RevisionId?
  Sequence
  ActivatedAt
  Reason
  CorrelationId?
```
Delivery stages artifact outside gate, commits one actual complete transition/history+Pending evidence/processing receipt in its own UoW, then separately observes exact committed transition on primary and saves first VisibilityConfirmedAt. This primary observation is not either transaction commit time; strict Freeze cutoff uses it. Known terminal banned intermediate has processed outcome but no effective row; technical missing artifact not skipped. Models/recovery defined once [F13](../features/F13-content-delivery.md#21-aggregates-entities-и-value-objects).
Publishing active, committed Delivery effective, processed source progress and pending/confirmed time evidence differ independently. Staff sees gap/materialization/time/ban; matching IDs not readiness proof. Current otherwise-allowed committed content may serve while needed temporal history readiness is separate.
## Policy agreement/source boundary
Authenticated VRS backend first durably reserves planned period/RequestId, calls CM outside its own transaction, then conditionally confirms exact original plan+AgreementId/outbox in another own commit. Lost CM reply replays original caller/input agreement before new quote checks; no current derived policy comparison. Changed unconfirmed plan or renewal needs new period/agreement; MVP no expiry/age cleanup, unused agreement gives no entitlement. No staff/Unity/raw-key issuer or prerequisite CM license projection/grant.

Catalog owns confirmed immutable AgreementId/original plan plus versioned current source facts/explicit current period; Delivery separately owns immutable tuple binding+factory-generated GrantPeriodId copied from exact agreement terms. Confirmed agreement substitution quarantines affected current basis NotReady while old history survives. Current-period new admission/strict multi-license completeness and correction-versus-renewal are [F12](../features/F12-content-entitlement-policy.md); opened-session timing and response leases follow accepted Q16, no shared UoW/FK.

## Cross-BC dependencies
### CatalogAccess
- Product/ProductVariant existence/current relation;
- source lifecycle;
- F15 blockers.
### ContentModeling
- resolve exact Published EntityInstanceVersion при editing assignments;
- validate availability for new use.
Immutable F10 composition работает по release snapshots и не читает live ContentModeling для merge.
### Delivery
- public read-port может загружать immutable revision snapshot;
- transitions идут через outbox/internal event.
Прямые DbContext/repository другого BC запрещены.
## Persistence
PostgreSQL schema `publishing`, отдельный DbContext/migrations.
Основные groups:
- assignments + child rows;
- releases + immutable child rows;
- publication revisions + materialized items;
- Product/Variant publication states;
- activation history;
- promotion operations/items;
- policy states/versions, immutable agreements and original caller/RequestId replay facts;
- outbox.
Cross-BC FK отсутствуют.
Одна Publishing transaction может изменять несколько aggregate roots **внутри Publishing**, когда use case требует атомарности, например active revision Withdrawal + state/history/outbox.
## Consistency guard MVP
По [Q20](../questions/Q20-persistence-and-migration.md#mvp-протокол-consistency-guard) одна PostgreSQL database обслуживает все CM replicas. Каждый invalidating writer получает один стабильный global exclusive pg_advisory_xact_lock отдельным первым statement **в своей owner BC ReadCommitted transaction на её же connection**, до authoritative reads/row locks. После acquisition собственные roots заново читаются tracked, foreign current facts — public read ports через independent primary scope/connection без ambient enlistment, foreign EF, cache/read replica или shared SQL transaction.

Guard объединяет F07/F08 Save и setup, F09 Create/lifecycle, F10 Create/lifecycle/Withdrawal, F11 Select/Activate/Rollback/Unpublish и каждый promotion capture/item/stage/late Approval commit, F12 policy changes/agreement creation, relevant F17 decisions и invalidating ContentModeling/Media/CatalogAccess/Delivery writers. Scoped F15 patch repair и CatalogAccess Resolve — два commits, каждый с новым gate: source v22 после repair-v21 сохраняет repair и запрещает старый Resolve basis. Участие обоих writers обязательно; один предварительный read guard не закрывает race.

Transaction изменяет только own BC state/token, required own outbox/safe audit intent/inbox. Commit/rollback освобождает gate; downstream Delivery/Audit commits получают свои owner boundaries. Target DI partition-ит нынешние multi-DbContext EfCoreUnitOfWorkParticipant по owner, foreign public reads используют независимые primary connections. CPU preparation разрешена снаружи при recheck relevant basis под gate; UI, VRS HTTP, S3/bytes, broker/ACK, ожидание moderation/projection и streams снаружи.

Own ETag/Version по-прежнему даёт stale412; independent relevant context/review basis drift даёт409, не retry с новым token. CreateRelease/CreateRevision не увеличивают source Version только ради capture; repeated Create по unchanged источникам допускается. Configured bounded lock wait+cancellation и failed public port/owner connection — technical abort, не domain Conflict/empty compatible facts; reconnect не продолжает старый basis. Lost commit ACK — unknown outcome с inquiry по выбранной operation identity/recovery Q15.

Query/picker/preview/review не получают write gate/commit permit; own DTO/token согласованы отдельно от composite freshness. Eligibility основана на committed CM facts, не мгновенном внешнем VR School knowledge. Gate не закрывает Q16 session credential/TTL/revoke/in-flight и Q17 confirmed primary observation/catch-up/history, head/ABA late Approval precedence и final-effective closure приняты workflow F11/F10; Q17 conservative observation semantics approved; runtime session/in-flight profile принят Q16; physical visibility codecs/mapping и проверка фактической отмены передачи остаются Q15/Q20/Q21. Demo single-process AssetId guard и минимальная цепочка остаются самостоятельными, полный Publishing guard не становится скрытой Demo dependency.
## Optimistic concurrency
Mutable roots используют internal `int Version`.
HTTP DTO Version не содержит. Exact mutable GET возвращает strong ETag; mutation использует If-Match.
Immutable source content create operations не получают fake concurrency token:
- create revision из immutable releases не требует If-Match;
- create release использует If-Match **source assignment**, поэтому route является action над assignment.
## API principles
- source catalog fields read-only;
- assignment mutation — exact resource + ETag;
- release/revision lifecycle — exact resource + ETag;
- activation/policy — state resource + ETag;
- Preview никогда не является authority для subsequent Create;
- DTO не принимает IDs/metadata, которые backend может authoritative resolve из exact reference.
## Feature map
[F07](../features/F07-product-content-assignment.md) · [F08](../features/F08-variant-content-assignment.md) · [F09](../features/F09-content-releases.md) · [F10](../features/F10-publication-revisions.md) · [F11](../features/F11-publication-lifecycle.md) · [F12](../features/F12-content-entitlement-policy.md)
## Связи
[CatalogAccess M03](M03-catalog.md) · [Delivery M05](M05-delivery.md) · [M06 staff workflow](M06-staff-workflow.md) · [Q07](../../../business/questions/Q07-exclusions-and-local-content.md) · [Q28](../../../business/questions/Q28-content-review-scope.md)
