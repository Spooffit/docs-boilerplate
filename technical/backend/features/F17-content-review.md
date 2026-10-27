# F17. Модерация PublicationRevision: бэкенд

[Бизнесовая спецификация](../../../business/features/F17-content-review.md) · [M04](../modules/M04-publishing.md) · [M06](../modules/M06-staff-workflow.md) · [Frontend](../../frontend/features/F17-content-review.md) · [Q28](../../../business/questions/Q28-content-review-scope.md#f17-technical-boundaries).

## 1. Область реализации

F17 — обязательный quality gate MVP для одной exact immutable ProductVariantPublicationRevision F10. Не вводится обязательная модерация SchemaVersion, EntityInstanceVersion, Product/Variant releases, Policy, Unpublish или explicit Withdrawal. Demo не включает эту фичу/Identity/роли.

Специалист Submit exact revision, читает status/comments/history и Withdraw Pending. Исправление rejected content создаёт новые releases/revision; withdrawn exact revision можно submit новой iteration. Админ читает точный materialized состав/provenance/source comments, semantic diff относительно текущей Delivery effective revision, добавляет comment, Approve либо Reject/Return с meaningful reason. Один раздел UI и общая область контента; SubmittedBy не personal-access filter. Совмещение ролей/self-approval допустимы при обеих permissions Q10/F16.

Manual Approval только разрешает отдельную F11 activation; automatic matched decision продолжает durable F11 operation конкретного Variant/revision. Approval не означает Published active или Delivery effective и не переносится по hash. Variants batch принимают независимые решения. Historical Approved exact revision допускает F11 rollback без новой moderation только при текущей lifecycle/eligibility; Withdrawal не удаляет Approval history, но запрещает распространение.

Новые типы — целевые обозначения. Проверенный checkout от 05.10.2026:

| Источник | Current implementation | Target gap |
| --- | --- | --- |
| [PublishingDbContext](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.EntityFramework/PublishingDbContext.cs) | Assignments/releases/revisions/activations/promotions/policy; нет moderation iterations/comments | Own roots/constraints/history/outbox/read models F17 |
| [Revision](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Domain/Publications/ProductVariantPublicationRevision.cs) | Exact artifact и IsRevoked metadata; Approval model не найден | Approval хранится отдельными iterations, не client flag на revision |
| [PublishingService.Releases](../../../../contentmanager.api/src/App/ContentManager.Application/Publishing/PublishingService.Releases.cs), [Variant state](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Domain/PublicationStates/ProductVariantPublicationState.cs) | Found activation path проверяет IsRevoked, пишет active/history и current Delivery context; Approved gate отсутствует | Protected F11 exact Approved eligibility + async Delivery; current flow не доказательство F17 |
| [PublishingController](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Api/PublishingController.cs), scoped src/test search без bin/obj/Migrations | Moderation types/routes/approval reader/tests не найдены; отдельный Publishing.EntityFramework.Persistence directory не найден | Api/Application/queries/persistence/topology и tests предстоит реализовать |

Catalog Archive/Conflict не являются основанием изменять immutable revision или её прежнюю Approval history. Новая зависимая submission и continuation activation/promotion/late approval проходят current affected scope/basis guard [F15](F15-catalog-conflicts.md); exact automatic head/ProductVersion/VariantSequence precedence принята F11; остальная effectivity Q17. Approval не обходит Archive/new-use guards [F06](F06-product-catalog.md) и не запускает publication через Resolve; explicit safety Unpublish/Withdrawal остаются отдельными обычными operations F11/F10.

## 2. Domain

### 2.1. Aggregates, entities и value objects

Publishing BC владеет moderation вместе с F10 artifact и F11 publication state. Content revision не копируется в iteration и не recomposed из текущих assignments. Actor IDs — safe CM staff identities F16, без Identity EF navigation.

| Модель | Роль | Состояние / lifecycle | Владелец / граница |
| --- | --- | --- | --- |
| ModerationIteration | Aggregate root | Pending → Withdrawn / Approved / Rejected, terminal decision | Publishing; own internal int Version |
| ModerationIterationId | Strong ID | Stable episode identity | Не RevisionId/operation ID |
| ModerationComment | Immutable child entity | Append while Pending; no edit/delete/chat thread | Iteration, не repository на каждый child |
| ModerationCommentId | Strong child ID | Server-created comment identity | Не текстовый dedup key |
| ModerationStatus | Enum | Pending, Withdrawn, Approved, Rejected | Rejected/Returned одна смысловая ветвь, не дополнительные состояния |
| Approval eligibility / review diff | Immutable Application/read facts, §3.3 | Не owned Domain revision content/state | Public ports/read models; mutable external facts требуют guards |

**ModerationIterationId / ModerationCommentId** — разные typed identities, одна общая shape Value.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Value | `Guid` | Да | Stable server ID соответствующего ресурса | New ID на iteration/comment, server factory profile Q20 |

**ModerationIteration**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | `ModerationIterationId` | Да | Identity review episode | Immutable |
| PublicationRevisionId | `ProductVariantPublicationRevisionId` | Да | Exact target F10 | Immutable; same Publishing BC; no hash transfer |
| Status | `ModerationStatus` | Да | Current iteration state | Initial Pending; final states terminal |
| SubmittedByUserId | `Guid` | Да | Trusted staff submit actor | F16 context manual; recorded F11 initiator automatic, actor/system profile Q20; no arbitrary input/fake actor |
| SubmittedAt | `DateTimeOffset` | Да | Server submission time | Immutable; clock, не ordering foreign state |
| WithdrawnByUserId | `Guid?` | Условно | Staff Withdraw actor | Заполнено Withdrawn, не auto revision Withdrawal |
| WithdrawnAt | `DateTimeOffset?` | Условно | Pending withdrawal time | Null других outcomes |
| DecidedByUserId | `Guid?` | Условно | Current authorized Admin final actor | Approved/Rejected; self-approval permitted при полномочиях |
| DecidedAt | `DateTimeOffset?` | Условно | Final decision time | Immutable после final commit |
| DecisionReason | `string?` | Условно | Rejection/return explanation | Required meaningful при Rejected; optional approval reason не назначен Q15/Q28 |
| Comments | `IReadOnlyList<ModerationComment>` | Да | Own immutable comments | Empty допустим; order/profile Q15; mutations меняют own Version |
| Version | `int` | Да | Own optimistic concurrency | Internal; не DTO/approval identity/SourceVersion |

Automatic actor attribution использует trusted F11 operation/prepared fact, не UI ActorId и не fake authenticated system user. Представление инициатора/system action и случая без recorded staff инициатора уточняется Q20/Q15; default Guid.Empty не закрывает вопрос.

**ModerationComment**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | `ModerationCommentId` | Да | Server child identity | Immutable, no client create ID |
| Text | `string` | Да | Admin review note | Meaningful text; normalization/limits Q15/Q28, no silent rewrite |
| CreatedByUserId | `Guid` | Да | Current authorized staff actor | Admin action, from F16 context |
| CreatedAt | `DateTimeOffset` | Да | Server append time | Immutable; stable tie-breaker ID ordering profile Q15 |

### 2.2. Методы и переходы

| Тип | Метод | Параметры | Предусловия | Изменение / результат |
| --- | --- | --- | --- | --- |
| ModerationIteration | Submit | id, exact revisionId, trusted actor/time | Protected available target/current submit eligibility, no existing Pending | New Pending; immutable target; server fields |
| ModerationIteration | AddComment | new commentId, text, actor/time | Pending, Admin permission Application, valid note | Append immutable child; status остаётся Pending; own token |
| ModerationIteration | Withdraw | actor/time | Pending, Specialist permission | Withdrawn; history retained; не F10 terminal Withdrawal |
| ModerationIteration | Approve | actor/time, validated eligibility facts | Pending, Admin, exact current lifecycle/distribution/F15 guards | Approved, immutable final metadata; durable outcome |
| ModerationIteration | Reject | reason, actor/time | Pending, Admin, meaningful reason | Rejected/Returned; target content не изменён |

Domain не обращается к Identity/CatalogAccess/Delivery DB или HTTP. Application checks/commit protocol обеспечивают достоверность переданных фактов. Final-state methods не reopen/delete/редактируют decision; fresh command на terminal iteration получает agreed domain error, не last-write-wins. Exact repeated command/outcome semantics Q15.

### 2.3. Инварианты и constraints

1. Maximum one Pending на exact RevisionId; many retained historical iterations. Single pending защищают Domain/use case и partial unique index, не только precheck.
2. Initial submit и новая iteration после Withdrawn входят в согласованный workflow. Rejected immutable не редактируется: correction создаёт новые F09/F10 artifacts. Не вводить appeal/re-submit неизменённой Rejected или повторную проверку уже Approved по аналогии; эти дополнительные cases/outcomes уточняются Q28/Q15.
3. Exact identity Approval — RevisionId; одинаковый hash иной revision не Approved. Reader Approved evidence не равен текущему разрешению activation: F11 повторно проверяет lifecycle/withdrawal/context/F15.
4. Pending Withdraw only; terminal history/comments/final actors/time сохраняются. Комментарии MVP Admin while Pending; edit/delete и terminal discussion не вводятся без отдельного решения Q28.
5. Reject требует meaningful reason; one final decision wins. Comment тоже меняет own Version, поэтому stale decision после чужого comment →412 по token, не свободный overwrite.
6. Submit требует current Available target. Approve требует текущей new-activation eligibility F10/F11: Retired/Withdrawn target, blocking F15 либо distribution-invalid dependency не разрешают Approved command. Retire **source release** сам по себе не разрушает уже materialized revision и не blanket blocker.
7. Новые blockers после Submit не auto Reject/Withdraw/delete iteration. Pending остаётся читаемым с причиной недопустимости Approve; разрешённые Comment/Withdraw/Reject не запрещаются только по F15 flag как новые activation actions.
8. Shared specialists, independent combined roles, self-approval при обеих permissions; нет CreatedBy/SubmittedBy ownership restriction, обязательного four-eyes или admin-only Submit.

[S21](../standards/S21-aggregate-constraints.md): ModerationIterationConstraints — transitions/terminal metadata/one Pending, ModerationCommentConstraints — meaning/content constraints; defined text limits/normalization Q15/Q28, не guessed numbers. Candidate validity защищается до commit: own iteration Version не покрывает mutable F10 lifecycle/foreign distribution/F15 facts. Read tracking/FluentValidation недостаточны; MVP gate §7.2; carrier/profile Q20/Q23.

### 2.4. Domain events

Committed final outcomes формируют normalized safe moderation fact. Ни Approved event, ни Comment означают activation/Delivery effective. Обязательный durable Approved outcome нужен F11 automatic continuation; Rejected outcome обеспечивает independent item progress. Withdrawn bound iteration cancels exact item/matching head atomically; exact envelope/topic profile Q20, не подмена terminal revision Withdrawal.

Logical event models описаны §3.3; как existing identifiers сохраняются PublicationRevisionModerationApproved/Rejected. Их package/version/topology names и F11 aliases ещё согласуются Q20, наличие исходников не утверждается. F18 receives safe activity separately, domain moderation history остаётся authority.

## 3. Application.Abstractions

### 3.1. Commands

| Command | Назначение | Result | Concurrency |
| --- | --- | --- | --- |
| SubmitPublicationRevisionForModerationCommand | New iteration exact immutable target | ModerationDto + own metadata | No existing iteration If-Match; target/new-use/unique guards protected |
| AddModerationCommentCommand | Append Admin note | Confirmed ModerationDto + ETag | Own expected int Version |
| WithdrawModerationCommand | Withdraw Pending | Confirmed ModerationDto + ETag | Own expected int Version |
| ApproveModerationCommand | Approve exact Pending target | Confirmed ModerationDto + ETag | Own expected Version + protected current eligibility |
| RejectModerationCommand | Return/reject exact target | Confirmed ModerationDto + ETag | Own expected Version + meaningful Reason |

**SubmitPublicationRevisionForModerationCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| RevisionId | `ProductVariantPublicationRevisionId` | Да | Exact artifact из route/trusted F11 fact | No client hash/actor/Approved flag; immutable target |
| ClientOperationId | `Guid` | Условно | Original public Submit execution | Required public body; internal automatic Submit uses frozen own item/tuple in caller UoW |

F11 automatic submission receives trusted captured initiator/item in the same atomic prepare Publishing UoW: own revision+Pending+exact ItemId/RevisionId/ModerationId+AwaitingModeration commit together. No required PreparedEvent→separate Submit stage/adoption чужой Pending. Internal automatic Submit dedup-ится exact PromotionItemId/own stage/committed tuple, не Start ClientOperationId across members и не новым random client ID на retry. InitiatorId из accepted start, без HTTP loopback/fake staff. Submit creation не использует чужой own iteration ETag; current revision lifecycle защищается внутри use case.

**AddModerationCommentCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ModerationId | `ModerationIterationId` | Да | Exact Pending iteration | Не другой revision moderation |
| Text | `string` | Да | User-entered note | Known semantic validator; limits Q15/Q28 |
| ExpectedVersion | `int` | Да | Own token | From required If-Match, не DTO |

**WithdrawModerationCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ModerationId | `ModerationIterationId` | Да | Exact Pending | Specialist current context; no personal isolation |
| ExpectedVersion | `int` | Да | Own precondition | Header mapped Api |

**ApproveModerationCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ModerationId | `ModerationIterationId` | Да | Exact candidate | Current Admin, target from stored iteration |
| ExpectedVersion | `int` | Да | Own precondition | Independent from live blockers/shown comparison basis |
| ShownComparisonBasis | Уточняется [Q15](../questions/Q15-api-and-source-code.md) | Да | Exact Delivery effective comparison basis shown Admin | Independent from own ETag; ReviewDiff identity/ordering/confirmed-none evidence §3.3; current drift409/new consent |

**RejectModerationCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ModerationId | `ModerationIterationId` | Да | Exact candidate | Current Admin |
| Reason | `string` | Да | Meaningful return explanation | Normalization/limits Q15/Q28 |
| ExpectedVersion | `int` | Да | Own precondition | Not SourceVersion/Revision lifecycle ETag |

Actor/time не поля client commands: ICurrentStaffContext/TimeProvider authoritative. Automatic background actor semantics F16/F11 отдельны от HTTP permission context.

### 3.2. Queries

| Query / read contract | Назначение | Result |
| --- | --- | --- |
| ListModerationsQuery | Shared queue/status/context/search | Paged summary; exact params Q15 |
| GetModerationQuery | Own core iteration | ModerationDto + internal Version metadata |
| GetRevisionModerationHistoryQuery | Retained all iterations exact revision | History summary/read profile Q15 |
| GetModerationReviewContextQuery | Exact F10 composition/source context + live eligibility/diff | ReviewContextDto logical shape Q15 |
| IRevisionApprovalReader | Has confirmed Approved exact RevisionId | Approved evidence / not approved / read failure |

**ListModerationsQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Parameters | Уточняется | Уточняется | Server list request | Status/context/search/optional actor/paging/sort profile Q15; actor filter не access isolation |

**GetModerationQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ModerationId | `ModerationIterationId` | Да | Exact read | Own core, no foreign live fields under ETag |

**GetRevisionModerationHistoryQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| RevisionId | `ProductVariantPublicationRevisionId` | Да | History owner | Does not filter away Withdrawn/Rejected |
| Parameters | Уточняется | Нет | Ordering/pagination if chosen | Q15, no invented history truncation |

**GetModerationReviewContextQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ModerationId | `ModerationIterationId` | Да | Exact target lookup | RevisionId taken from stored core, not client substitution |

### 3.3. DTO и parameter models

Own core, exact F10 read and independent live context split. DTO Version/ExpectedVersion absent. Public names/carriers/actor-label exposure/enums/nested shapes Q15; identity IDs mапятся явно, not ORM graph.

**ModerationDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | `Guid` | Да | Iteration identity | Own exact resource |
| RevisionId | `Guid` | Да | Immutable target | Same stored exact ID across decisions/history |
| Status | Уточняется | Да | Pending/Withdrawn/Approved/Rejected | Wire enum/profile Q15 |
| SubmittedByUserId | `Guid` | Да | Trusted recorded staff initiator | Actor/system attribution profile Q20/Q15, no fake actor |
| SubmittedAt | `DateTimeOffset` | Да | Submission timestamp | Own immutable metadata |
| WithdrawnByUserId | `Guid?` | Условно | Withdraw actor | Only Withdrawn |
| WithdrawnAt | `DateTimeOffset?` | Условно | Withdraw time | History retained |
| DecidedByUserId | `Guid?` | Условно | Final Admin identity | No SubmittedBy != DecidedBy invariant |
| DecidedAt | `DateTimeOffset?` | Условно | Final decision time | Approved/Rejected |
| DecisionReason | `string?` | Условно | Required rejected explanation | Not generic current provider/security field |
| Comments | `IReadOnlyList<ModerationCommentDto>` | Да | Own recorded comments | Empty confirmed list valid; paging alternative Q15, exact core/token consistency |

**ModerationCommentDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | `Guid` | Да | Immutable server identity | No comment create body ID |
| Text | `string` | Да | Recorded note | No edit/delete endpoint |
| CreatedByUserId | `Guid` | Да | Recorded actor ID | Safe label lookup separate read contract Q15 |
| CreatedAt | `DateTimeOffset` | Да | Recorded time | Server; not unique command dedup key |

**ModerationListItemDto / history summary** — logical shared projection; exact carrier/profile Q15, no per-row ETag.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | `Guid` | Да | Iteration context | Stable route identity |
| RevisionId | `Guid` | Да | Exact artifact | No hash-based Approval |
| ProductVariantId | `Guid` | Да | Captured revision owner | Current source parent/label separately marked if needed |
| Status | Уточняется | Да | Recorded state | Wire enum Q15 |
| SubmittedByUserId | `Guid` | Да | Submission actor | Convenience filter not ownership boundary |
| SubmittedAt | `DateTimeOffset` | Да | Submitted time | Stable ID tie-breaker for ordering |
| LatestCommentSummary | Уточняется | Нет | Safe preview if chosen | Shape/limits Q15; full comments via core/profile |
| DecisionSummary | Уточняется | Условно | Final actor/time/reason metadata | Own metadata; exact shortened projection Q15 |
| DiagnosticLabels | Уточняется | Нет | Human Product/Variant/actor captions | Not identity; live labels no own ETag promise |

**AddModerationCommentRequest**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Text | `string` | Да | Pending admin note | No ActorId/Version/server CommentId |

**RejectModerationRequest**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Reason | `string` | Да | Meaningful return explanation | No hidden content correction/actor/Version |

Public Submit requires body ClientOperationId; Approve requires shown Delivery comparison basis. Exact envelope/casing/codec/audit fields remain Q15. Withdraw carrier separately Q15. Не объявлять весь worker batch одной DTO решения админа.

**SubmitModerationRequest**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ClientOperationId | `Guid` | Да | Logical original Submit execution | Non-empty; no actor/body Approved; canonical receipt Q20 |

**ApproveModerationRequest**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ShownComparisonBasis | Уточняется [Q15](../questions/Q15-api-and-source-code.md) | Да | Confirmation of exact shown Delivery basis | Refers ReviewDiff identity/order/confirmed-none; drift409 independent own If-Match, refresh+new consent |

**ModerationListParameters** — logical request shape, wire names/defaults Q15.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Status | Уточняется | Нет | Known moderation status filter | Default/profile Q15, no hidden personal queue isolation |
| RevisionId | `Guid?` | Нет | Exact target filter | Server-side, не hash match |
| ProductVariantId | `Guid?` | Нет | Captured owner filter | Current/captured context details Q15 |
| Context | Уточняется | Нет | Additional Product/context filter if selected | Q15; no guessed separate tenant partition |
| Search | `string?` | Нет | Safe caption/context search | Fields/case/limits Q15 |
| SubmittedByUserId | `Guid?` | Нет | Optional convenience actor filter | Shared access not reduced to creator |
| Page | Уточняется | Нет | Server paging | Field names/ranges/defaults Q15 |
| Sort | Уточняется | Нет | Whitelist ordering | Stable Id tie-breaker, defaults Q15 |

**ReviewContextDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| RevisionId | `Guid` | Да | Reviewed exact target | Must match ModerationDto.RevisionId |
| RevisionRead | Уточняется | Да | F10 materialized items/exact sources/comment/provenance/exclusions | Owner [F10](F10-publication-revisions.md#33-dto-и-parameter-models), no recomposition from current assignments |
| Eligibility | Уточняется | Да | Current lifecycle/withdrawal/F15/new-activation facts | Known blocked vs unavailable separate; shown basis validated separately at Approve commit |
| Diff | Уточняется | Условно | Semantic comparison result if confirmed basis | Mandatory review capability, read failures explicit, no fake empty |
| ObservedAt | `DateTimeOffset` | Да | Server context observation | Не own iteration Version или proof для Approve |

**ReviewDiff**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| TargetRevisionId | `Guid` | Да | Exact F10 candidate | Immutable target set |
| BasisRevisionId | `Guid?` | Условно | Current Delivery effective revision Variant | Null только confirmed no effective; missing projection/read failure ≠ no publication |
| BasisSequence | `long?` | Условно | Captured Delivery applied ordering | Meaning per F13; required authoritative compared ordering/confirmed-none evidence; carrier Q15 |
| BasisObservedAt | `DateTimeOffset` | Да | Captured read time | As-of caption; time alone not concurrency token; shown exact basis rechecked for Approve |
| Entries | `IReadOnlyList<ReviewDiffEntry>` | Да | Added/ChangedVersion/Removed | Empty only confirmed complete comparison; no mandatory field JSON diff |

**ReviewDiffEntry**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| EntityInstanceId | `Guid` | Да | Real logical result identity | Не Key/Name/override target A вместо result B |
| Kind | Уточняется | Да | Added / ChangedVersion / Removed | Wire enum Q15; no guessed extra diff states |
| Before | Уточняется (nullable) | Условно | Exact old item reference pair | EntityInstanceVersionId+SchemaVersionId, owner F10/F13 read fields |
| After | Уточняется (nullable) | Условно | Exact target reference pair | Added has no Before, Removed has no After, Changed has both |

**PublicationRevisionModerationApproved (logical outcome payload)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ModerationId | `Guid` | Да | Exact final iteration | F11 must match binding/current fact |
| RevisionId | `Guid` | Да | Exact immutable approved artifact | No hash/Variant latest substitution |
| ApprovedByUserId | `Guid` | Да | Recorded trusted final actor | Not recipient authorization claim |
| ApprovedAt | `DateTimeOffset` | Да | Committed final time | Not Delivery effective time |
| Correlation | Уточняется | Условно | Trusted operation/item binding | Envelope/type/idempotency profile Q20; no arbitrary client OperationId override |

**PublicationRevisionModerationRejected (logical outcome payload)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ModerationId | `Guid` | Да | Exact rejected iteration | Independent Variant result |
| RevisionId | `Guid` | Да | Exact candidate | Previous active revision unchanged |
| RejectedByUserId | `Guid` | Да | Recorded Admin | Not actor input event from browser |
| RejectedAt | `DateTimeOffset` | Да | Final time | No activation/Delivery transition |
| Correlation | Уточняется | Условно | Trusted operation/item linkage | Q20; consumer rereads reason/history, raw reason not required transport |

EventId/contract version/producer/sequence/inbox properties относятся к agreed envelope Q20/S22; не создаются по guessed transport defaults. Withdraw bound iteration atomically cancels exact automatic item/matching head without publication transition; durable outcome recovery codec Q20. New manual iteration does not reattach. Full F18 activity event owner F18, not duplicate content snapshot.

### 3.4. Ports

| Port | Назначение | Направление |
| --- | --- | --- |
| IModerationIterationRepository | Exact tracked root+comments, current Pending by revision, Add | Publishing Persistence |
| F10 exact revision read / guarded eligibility boundary | Materialized target/current lifecycle/same-BC protected facts | Publishing local boundary; root reused |
| IRevisionApprovalReader | Confirmed Approved exact target evidence for F11 | Public Publishing read contract; implementation data-access |
| CatalogAccess operation-block / dependency restriction read | Current F15/new-use/distribution facts | Public BC contracts, no foreign EF contexts |
| Delivery effective review reader | Confirmed current Variant effective basis/exact materialized items | Delivery public query adapter, not license-entitlement or live SQL |
| ICurrentStaffContext / TimeProvider | Verified actor/role and server time | F16 Host/Application context; no Identity tables |
| Outbox / automatic binding boundary F11 | Durable decision+matching operation continuation | Same Publishing state/integration, exact atomic binding Q20 |
| MVP consistency guard / owner UoW boundary | Один global transaction exclusive gate по Q20, owner-only atomic commit и independent primary public reads | Shared infrastructure adapter; owner connection, не foreign transaction |

### 3.5. Specifications / read contracts

[S19](../standards/S19-specifications.md): atomic exact-ID/RevisionId/Status/context/search/actor predicates reused queue/history/Pending/Approved reads, no spec.And. Shared scope; SubmittedBy filter only optional convenience. Whitelist sort/stable Id/page defaults Q15, no invented paging limits.

[S20](../standards/S20-cqrs.md): command handlers Application; query handlers EntityFramework.Persistence либо adapter data boundary. Separate Publishing.EntityFramework.Persistence предстоит добавить. Own core projection и Version coherent; list no per-row token. Approval reader false требует confirmed отсутствие Approved, unavailable/incomplete not false. Live review joins используют public projections, не foreign DbContext/navigation.

## 4. Application

### 4.1. Handlers / use cases

Public Submit: auth → first MVP gate → canonical [CommandExecution](../questions/Q20-persistence-and-migration.md#publishing-workflow-capture-и-продолжение) lookup before new-execution preconditions → own new iteration+receipt commit. Replay original confirmed result; no re-Submit/Restore/Activate. [Q15](../questions/Q15-api-and-source-code.md#publishing-command-execution-и-clientoperationid) owns receipt transport. Approve independently validates shown Delivery effective comparison basis under gate; final-effective closure shared F10/F11/F13.

Mutation handlers соблюдают порядок §7.2: shape/auth preparation → own ReadCommitted transaction → отдельный global gate statement → fresh tracked own roots + independent primary public facts → own token/basis/domain checks → own state/intents commit. Предварительные reads/CPU results не являются authority после ожидания gate.

| Handler / physical layer | Чтения и protected facts | Запись / result |
| --- | --- | --- |
| Submit handler, Publishing.Application | F16 Specialist либо trusted F11 prepared context; exact F10 target, current Available/new-use/F15 guard, Pending/history | New Pending + durable automatic binding if applicable, own result after local commit |
| Comment/Withdraw/Reject handlers, Publishing.Application | F16 current action permission, own root/comments/token, Pending | Append/comment либо terminal outcome/history; safe F18 fact; no target content edit |
| Approve handler, Publishing.Application | Current Admin, own token/Pending, exact revision lifecycle/withdrawal/dependency/F15 facts with guards | Approved + durable decision outbox same Publishing commit; no manual activation |
| List/core/history/Approval queries, EntityFramework.Persistence | Specifications/local projection/current exact Approved evidence | Read-only coherent DTO/evidence, no save on GET |
| Review context/diff query adapter, data-access composition | F10 exact materialized target, source comments/provenance; Delivery effective public basis and current restrictions | As-of read result/typed unavailable; not own moderation ETag or transaction across BC |

Actor проверяется F16 на каждом staff HTTP entrypoint; его current role context immutable на request, already checked in-flight permission behavior F16 сохраняется. ActorId не trusted body field. Automated F11 submission использует agreed durable initiator/system context, не fake authenticated StaffUser.

### 4.2. Validators

[S18](../standards/S18-fluent-validation.md): переиспользовать атомарные ModerationId/RevisionId/Text/DecisionReason validators и agreed list parameters; known text constraints берутся из named Domain definitions. Reason meaningful, не whitespace; max lengths/normalization/format Q15/Q28. Comment и reason отдельные semantic rules, не generic «строка».

Pending state/unique iteration/current revision availability/F15/withdrawal факты проверяются handler и commit guards, не считаются защищёнными async FluentValidation. Сам Submit no If-Match не отменяет DB unique/current-lifecycle guard. Failed external read != no blockers/Approved=false.

### 4.3. Mappers

Request route ID → typed root ID; If-Match → internal expected int Version; trusted F16 context → persisted actor Guid. Core → ModerationDto и generic own metadata carrier отдельно. Actor labels/live revision availability/diff не join в core ETag body. Exact F10 materialized read reused, no copy of current editable fields; safe source captions via public ports, no Identity/CatalogAccess EF join. Public enum/envelope/type profiles Q15/Q20.

### 4.4. Services / algorithms

ApprovalEligibilityEvaluator проверяет exact artifact current new-activation eligibility, matching Variant/context, terminal distribution restrictions и F15 scope. Source release Retire alone не invalidates immutable composition. Eligibility/current restrictions use F10 final-effective instance/schema/media closure; unavailable suppressed A does not block effective B unless shared final dependency. Он не использует browser Approved flag, current Product base как replacement или общий IsApproved по hash.

SemanticDiffBuilder принимает две complete immutable item projections: target exact RevisionId и captured current Delivery effective basis того же Variant. Key = real result EntityInstanceId F10. Added — только target, Removed — только basis, ChangedVersion — тот же ID, но отличается EntityInstanceVersionId и/или SchemaVersionId. Hash equality не подавляет exact version change, Name/Key не identity. Override A→B сравнивает result B, а не подменяет его target A. Same IDs/version pair — не ChangedVersion только из-за иной provenance; дополнительные metadata/order differences Q15/Q28, no invented fourth default diff state.

Confirmed no-effective basis означает empty set и все target entries Added; unavailable/incomplete/missing projection не такое подтверждение. Capture revision/sequence/as-of basis, не Publishing intended pointer/license-specific Frozen selection. Approve получает shown comparison basis отдельно от own iteration If-Match. После gate current public Delivery basis сверяется с показанным exact ID/ordering/confirmed-none evidence; drift →409, refresh comparison и новое consent. Unavailable/incomplete != empty basis. Later comparison change alone does not revoke committed Approval; F11 activation checks its own applicability basis.

Own iteration Version защищает comments/status. F10 lifecycle mutable в другом root того же Publishing BC и F15/dependency restrictions другого BC требуют отдельной защиты до commit. Tracking read-only revision, повторный SELECT, own moderation ETag и async FV не гарантируют invariants. MVP gate §7.2 сериализует lifecycle/decision и all-writer cross-BC basis: no foreign DbContext/shared transaction. F11 activation после Approval ещё раз проверяет approved/available/applicable facts.

## 5. Api

### 5.1. Endpoints

Это logical target surface прежней F17; source implementation не найдена. Final wire/body/status/access names/versions Q15, без фиктивного generated method.

| Method | Route / boundary | Request | Response | Access |
| --- | --- | --- | --- | --- |
| POST target | /api/v1/publication-revisions/{revisionId}/moderations | Exact target + required ClientOperationId; audit profile Q15 | New Pending core DTO/own ETag | Specialist, no iteration If-Match |
| GET target | /api/v1/moderations | Allowed filters/page/sort | Paged shared summary | Specialist/Admin read policy Q10/Q15 |
| GET target | /api/v1/moderations/{moderationId} | Exact ID | Core DTO+own ETag | Shared current staff read |
| GET target | /api/v1/publication-revisions/{revisionId}/moderation-history | Exact artifact ID/history params | Retained iteration history | Shared current staff read |
| POST target | /api/v1/moderations/{id}/comments | Text + required If-Match | Confirmed core DTO/new ETag | Admin, Pending |
| POST target | /api/v1/moderations/{id}/withdraw | Required If-Match, action body profile Q15 | Confirmed Withdrawn core/new ETag | Specialist, Pending |
| POST target | /api/v1/moderations/{id}/approve | Required If-Match + shown comparison basis, stored exact target | Confirmed Approved core/new ETag | Admin, Pending + current eligibility |
| POST target | /api/v1/moderations/{id}/reject | Reason + required If-Match | Confirmed Rejected core/new ETag | Admin, Pending |
| Review read exposure TBD | GetModerationReviewContextQuery | IterationId, stored RevisionId | F10 exact read + live eligibility/diff | Q15; not new guessed route/composite ETag |

Submit success status/Location/profile Q15 не подставляется автоматически201. Mutation success body/core metadata profile Q15, no automatic204/no hidden fresh GET replacing prepared outcome. Read history не требует повторной moderation, actor/public label profile не открывает StaffManagement API специалисту.

### 5.2. ETag / If-Match

[S16](../standards/S16-rest.md): exact owned core Staff-independent ModerationIteration state получает strong numeric ETag from own internal int Version. Required If-Match для Comment/Withdraw/Approve/Reject; missing428, own stale412, unchanged own token при current lifecycle/F15/domain mismatch409. Technical Version не DTO; list no per-row ETag.

Submit создаёт root и не принимает pending token; target eligibility/unique race не превращается в client412. External F10 lifecycle/current Delivery basis/blockers/actor captions изменяются без iteration Version: их не выдавать как часть composite under same ETag. Header exposure/metadata carrier Q15.

Terminal status/own race классифицируются после rollback/fresh state: original stale token →412; fresh terminal action →domain409/profile Q15. Нельзя просто retry с новым ETag после чужого решения. Expired staff reauth удерживает original moderation token/Text/Reason; fresh GET не заменяет baseline автоматически.

### 5.3. OpenAPI и ошибки

Api adapters bind/cancellation/F16 context/headers → CQRS → shared result mapping. Declare body fields Text/Reason, required Submit ClientOperationId and Approve ShownComparisonBasis carrier, own ETag/If-Match,400 input,401/403 access,404 exact missing,428/412/409 guards; unavailable review/provider/dependency mapping Q15. Strict error code/field path enums/status for repeat/duplicate/invalidated source не изобретаются.

No edit/delete comment/decision, universal approval routes, batch final decision or UI actor input. Approval representation не обещает activation or Delivery available; отдельные F11 read states. Generated NSwag after actual server OpenAPI verification; ручная model/client подмена не является готовым контрактом.

## 6. EntityFramework

### 6.1. DbContext ownership

PublishingDbContext/schema publishing owns iterations/comments/target F10 and F11 binding/outbox. Identity/CatalogAccess/Delivery данные приходят через contracts/ports, без cross-BC FK или transactions. Отдельного Moderation BC/DbContext только ради названия F17 нет.

### 6.2. Configurations

Target ModerationIterationConfiguration → moderation_iterations: own Guid PK, publication_revision_id Guid FK внутри Publishing, status enum mapping Q20, submitted/withdrawn/decided actor IDs и server timestamps, decision_reason text, own version:int concurrency. Safe actor IDs без Identity table FK. Child moderation_comments: server Guid PK, moderation_id FK, immutable text/actor/time, aggregate append changes root Version.

Typed snapshots content не копируются в moderation row; revision retained exact root FK. Root/comments projection coherent. Provider-specific actor/system metadata и ordering/comments pagination profile Q15/Q20. Own domain events не ORM property blob. Approved outcome outbox stores normalized IDs/outcome metadata, not revision JSON/admin credential.

### 6.3. Constraints / indexes

Partial unique Pending(publication_revision_id) разрешает many historical terminal rows и maximum one Pending. Predicate использует selected enum storage, не guessed integer/string literal. Required conditional final/withdraw metadata согласованы с statuses, Reject reason meaningful; SQL can enforce согласованное structural часть, Domain semantic rules authoritative.

Indexes queue status/submittedAt/Id and history revisionId/submittedAt/Id, exact Approved evidence lookup; concrete names/filter/storage Q20. Comments root FK/id retained; no cascade business history deletion через F10 revision API. Approval/iteration/RevisionId identity не content-hash uniqueness.

## 7. EntityFramework.Persistence

### 7.1. Repositories / query adapters

Host/DI задаёт owner-partitioned UoW participants: в transaction входят только DbContext/own outbox/inbox данного BC. Текущий EfCoreUnitOfWorkParticipant, способный объединять несколько DbContexts в общей transaction, не является target coordination adapter: participant selection разделяется по owner BC, foreign public read adapter создаёт независимый primary scope/connection без ambient enlistment. Guard adapter использует owner connection/transaction; mapping/repository другого BC в command handler не внедряется.

ModerationIterationRepository tracks own root+comments для mutation, GetPending/ExactId/Add and shared specifications. EntityFramework.Persistence target query handlers project core/list/history/approved evidence; current project отсутствует и должен быть создан по слоям F17, не query code inside Application.

Approved existence query ограничивает exact revision ID/status, read failure не false approval/no-history. Activation safety требует current F10/F15 guards отдельно. Review adapter public F10/Delivery/Catalog/constraint projections, no ORM foreign entities. Queries не refresh-owned comments/status/diagnostics через writes.

### 7.2. Transactions

По [MVP consistency guard](../questions/Q20-persistence-and-migration.md#mvp-протокол-consistency-guard) каждый invalidating writer начинает **свою** BC transaction с явно заданным ReadCommitted и отдельным первым SQL statement получает один стабильный глобальный exclusive pg_advisory_xact_lock. Lock выполняется на том же открытом owner connection, на котором будут authoritative reads, row locks, запись и commit; общий ключ одинаков во всех репликах CM. До получения gate нельзя authoritative-read/check/row-lock затронутый basis. Для replayable Create/StartPromotion/Submit первым authoritative own read после gate идёт confirmed CommandExecution lookup; только новый execution загружает tracked business roots и проверяет expected Version. Остальные writes заново загружают own roots/expected token; чужие current facts читает только через public read ports в независимом primary connection/scope, без ambient UoW enlistment, foreign EF, shared SQL transaction, cache или read replica. Публичный read carrier остаётся предметом Q15; механизм сериализации уже выбран.

Все способные инвалидировать проверяемый basis writers используют тот же gate: F07 whole Save/setup, F08 patch Save/setup, ContentModeling/Media lifecycle/reference changes, CatalogAccess applied relation/F15 Resolve, F09 CreateRelease/Retire/Restore, F10 CreateRevision/lifecycle/Withdrawal, F11 Select/Activate/Rollback/Unpublish и promotion capture/per-item/stage/late Approval, F12 policy changes, F15 scoped repair, релевантные F17 decisions и Delivery-owned invalidation commits. Каждый изменяет только own BC state. Own state/token, required own outbox, safe audit intent и inbox marker фиксируются атомарно; commit/rollback освобождает transaction lock. Это общий write gate в одной PostgreSQL database между репликами, а не общая транзакция BC.

CPU-подготовка, immutable projection/canonical hashing и предварительные проверки могут быть снаружи gate, но после acquisition весь влияющий на commit basis заново подтверждается до применения подготовленного результата. Gate нельзя удерживать во время UI/человеческого Approval, VR School HTTP, S3/bytes, broker send/ACK, ожидания Delivery projection либо HTTP stream; эти действия и downstream commits имеют отдельные границы.

Acquisition wait ограничен выбранной конфигурацией и cancellation; timeout/cancel/lock failure — технический отказ, не domain Conflict. Failed/partial public port не превращается в empty compatible facts. Потеря owner connection прерывает операцию целиком; reconnect не продолжает прежний basis. Own expected Version mismatch остаётся 412; independent relevant basis drift — 409 с повторной проверкой/review, без автоматического retry с новым ETag. Потерянный commit ACK означает unknown outcome: inquiry по durable operation identity/выбранному Q15 recovery, а не утверждение rollback или слепой повтор.

Read queries, pickers, preview/review не получают этот global gate и не выдают commit permit. Own DTO и token читаются согласованно из одного owner поколения; composite basis/freshness других roots учитывается отдельно. Последующая mutation получает новый gate и перепроверяет basis.

F17 DF01 Submit, DF05 Comment, DF06 Withdraw pending, DF07 Approve и DF08 Reject получают gate до tracked Pending/current revision checks; own Version и foreign/current eligibility разделены. DF09 automatic continuation после исходного decision commit получает свой новый gate по F11. Exact binding/captured actor/head/ProductVersion/VariantSequence continuation approved F11; message codec Q20. Shown Delivery basis is required independent Approve precondition; own ETag not substitute.

Public Submit после auth/first gate confirmed receipt lookup before current/new-execution preconditions; new iteration + safe audit + original CommandExecution receipt commit together. Internal automatic Submit atomic with own F10 revision/item exact tuple/AwaitingModeration in F11 prepare, no independent commit/durable-intent gap. Unique Pending race fresh reads текущий episode; no duplicate or blind adoption arbitrary existing iteration by other operation.

Approve commit атомарно сохраняет final decision/own Version и durable outbox outcome. Reject/Withdraw history/outcome/F18 facts сохраняются по same chosen local reliability profile. Comment append/root token update один local commit. Withdraw bound Pending atomically marks exact item Cancelled/clears matching head; new manual iteration not reattach, old pointer/history retained. No success/event publish before UoW completion. First transaction advisory gate §7.2 обязателен для F10 lifecycle и F17 decision; own token не заменяет fresh eligibility.

Cross-BC F15/committed CM distribution facts читаются independent primary public ports после gate §7.2, не общей SQL transaction. Failed read/guard/commit rollback не публикует approved fact. Outbox lag не rollback committed Approval; F11 idempotently resumes continuation. Lost HTTP result inquiry/recovery Q15, не automatic duplicate Comment/Submit.

### 7.3. Migrations

После final enum/actor/envelope/guards/profile choices добавить own tables/FK/partial unique/index/concurrency/outbox linkage и query module. No mandatory separate content snapshot table, four-eyes check или staff ownership constraint. Existing no-production-data reshape policy Q20 применима, но migrations/DB здесь не запускаются.

## 8. External adapters

### 8.1. RabbitMQ / MassTransit и F11 continuation

[S22](../standards/S22-eventbus-topology.md): normalized committed moderation outcomes через agreed Publishing outbox/broker/consumer либо выбранный same-BC durable handler mechanism; concrete Contract/RabbitMq/default registrations/names/envelope Q20. Approved payload exact iteration/revision/actor/time, Rejected progress exact same target; no Delivery activation message from Approval.

F11 consumer loads durable operation/item/bound ModerationId, rechecks terminal exact outcome/current target eligibility/current stage/applicability. Matching Approved activates only exact prepared revision via F11 protected transition; Rejected leaves old active revision и records per-item result. Duplicates/order/late approvals после manual changes/new operation require current head/captured Product Version/Variant Sequence F11, no latest-wins/active pointer reset simply from event.

Withdrawn iteration и new resubmission same revision имеют разные ModerationIds. Withdraw cancels exact item; new manual iteration never reattaches. Transport/outcome codec Q20; old iteration event не approves новую. No permanent AwaitingModeration loop/fake new approved revision as implementation shortcut.

### 8.2. Review/public adapters и Host

DeliveryEffectiveReviewReader returns complete Variant current effective exact basis/items/sequence or confirmed none vs unavailable. Это не raw license key/entitlement Frozen selection и не Publishing pointer. Adapter к F15/dependency facts returns complete guards/error; safe staff captions через agreed read boundary, no staff admin rights from review UI.

Host DI registers Publishing.Application handlers, new Persistence queries/repos, own UoW/outbox/inbox, current staff/clock, public BC adapters and F11 atomic prepare/internal submission/decision continuation. No new S3/conversion/auth provider solely F17; exact item/content/asset read responsibility F10/F05/F13. Demo registration не зависит от F17/Identity/broker moderation.

## 9. Data flows

[S17](../standards/S17-data-flows.md): transport obligations и internal algorithms отдельно, all mutations preserve exact target. DF10 read context не root concurrency authority.

| Поток | Транспорт | Метод / канал | Endpoint / destination | Доступ | Вход | Application action | Выход | Успех | Ошибки |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| DF01. Submit | HTTP / same F11 prepare UoW | POST / internal call | /publication-revisions/{id}/moderations; internal caller | Specialist/captured trusted initiator | Exact revision, public ClientOperationId; no existing iteration token | SubmitPublicationRevisionForModeration | New Pending core+ETag | Committed unique Pending | Invalid/unavailable/blocked target,duplicate pending/race,actor binding Q20 |
| DF02. Queue | HTTP target | GET | /api/v1/moderations | Shared Specialist/Admin read | Selected params | ListModerationsQuery | Paged summary |200 confirmed |401/403,validation/read error |
| DF03. Core | HTTP target | GET | /api/v1/moderations/{id} | Shared staff read | ModerationId | GetModerationQuery | Owned core+ETag |200 same resource version |404,401/403,read failure |
| DF04. Revision history | HTTP target | GET | /publication-revisions/{id}/moderation-history | Shared staff read | Exact revision ID | GetRevisionModerationHistory | Retained all outcomes |200 confirmed history |404/validation/read unavailable |
| DF05. Comment | HTTP target | POST | /moderations/{id}/comments | Admin Pending | Text+If-Match | AddModerationCommentCommand | Core+newETag | Append after commit |428/412,terminal409,invalid text,unknown result |
| DF06. Withdraw pending | HTTP target | POST | /moderations/{id}/withdraw | Specialist Pending | If-Match | WithdrawModerationCommand | Withdrawn core+ETag | Commit,history retained |428/412,terminal409,unknown result |
| DF07. Approve | HTTP target | POST | /moderations/{id}/approve | Admin Pending,current valid target | If-Match + shown Delivery comparison basis | ApproveModerationCommand | Approved core+ETag,outbox | Committed exact Approval |428/412,invalid target/F15/dependency409,read/guard outage |
| DF08. Reject/Return | HTTP target | POST | /moderations/{id}/reject | Admin Pending | Reason+If-Match | RejectModerationCommand | Rejected core+ETag,outcome | Meaningful reason, no target edit |428/412,invalid reason,terminal409 |
| DF09. Automatic continuation | Durable F11/outbox/inbox | Approved/Rejected exact outcome | Publishing F11 stage handler | Trusted internal producer/context | Exact IterationId/RevisionId/binding | F11 matched revalidation | Per-item activation/reject/blocked progress | Idempotent correlated transition | Duplicate/late/stale binding/current lifecycle/guard failure Q20 |
| DF10. Review context/diff | Public read adapters / exposure TBD | GetModerationReviewContextQuery | F10 + Delivery effective + restriction ports | Authorized shared review read | Exact stored target/Variant | Materialized reads/SemanticDiffBuilder | As-of complete compare/context | Confirmed full basis/none | Unready/missing/read failure ≠ empty, no own composite ETag |

### DF01. Submit

Внутренний write path после Api/FV/проверки caller: открыть owner ReadCommitted transaction, отдельным первым statement получить global pg_advisory_xact_lock на owner connection; только затем выполнять указанные ниже authoritative reads/row locks. Foreign facts — independent primary public ports без enlistment. Все guard/basis checks повторяются для этого commit; §7.2 задаёт failure/recovery и запрет внешнего ожидания под gate.

1. HTTP F16 current Specialist/authorized scope then required ClientOperationId; first MVP gate then confirmed scoped receipt lookup before new-execution eligibility/Pending check. Same original input/actor →original iteration receipt even terminal/source changed; changed bound input409. Internal F11 Submit uses captured trusted initiator in caller prepare UoW. Bind exact revision; no client ActorId/hash/Approved flag/other-operation body.
2. Load current F10 exact root/owner/lifecycle, approved/history/Pending facts и complete new-submit eligibility/F15/dependencies. Available target не заменяется active/latest; known resubmit Withdrawn creates new ID.
3. Domain Submit creates Pending. Gate получен до target/Pending read; fresh lifecycle/current target facts и unique Pending защищают commit (§7.2). A precheck без DB constraint не serialized creation.
4. Public new execution commits root+safe activity+original confirmed receipt; internal automatic commits root with own F10 revision/item tuple/AwaitingModeration in same UoW. Unique race →fresh confirmed state/typed existing-Pending refusal, no unsaved resource response. Original receipt replay is specified; no adoption. Codec/error/privacy profile Q15/Q20.
5. Return confirmed core/own metadata. Existing terminal history remains; rejected correction not edit of same immutable target, unsupported reconsideration cases Q28.

### DF02. Queue

1. Bind allowed filters/cancellation after staff shared read policy. Actor filter optional convenience, не authorization by SubmittedBy.
2. Data-access specifications apply server status/context/search/page/whitelist sort/stable Id tie-breaker, project summaries/safe labels.
3. Return confirmed page; read failure ≠ empty queue. No per-row ETag, root status updates, decision or GET audit success.

### DF03. Core

1. Exact query reads own status/target/actors/times/comments and own Version coherently. Missing404; terminal rows remain readable.
2. Map DTO without technical Version, form strong numeric header. Current external lifecycle/diff/labels not part of same token-protected body.
3. No writes/comments/status promotion from read, no auto-completion/rejection. Review context separate DF10, staff display lookup profile Q15.

### DF04. Revision history

1. Validate exact RevisionId/shared access, immutable target/read availability; history query own records including Withdrawn/Approved/Rejected.
2. Apply agreed ordering/paging, return recorded actors/time/comments/reason or summaries with explicit detail read. No max history truncation chosen by default.
3. No rewriting past outcome from current lifecycle/Withdrawal. F18 journal optional linked companion, not domain history replacement. Approval reader exact existence remains separate current eligibility F11.

### DF05. Comment

Внутренний write path после Api/FV/проверки caller: открыть owner ReadCommitted transaction, отдельным первым statement получить global pg_advisory_xact_lock на owner connection; только затем выполнять указанные ниже authoritative reads/row locks. Foreign facts — independent primary public ports без enlistment. Все guard/basis checks повторяются для этого commit; §7.2 задаёт failure/recovery и запрет внешнего ожидания под gate.

1. Require current Admin, valid Text, exact own If-Match; load tracked Pending+comments. Own stale412/current terminal409 distinct.
2. Domain AddComment creates new immutable server child ID/actor/time, append/root concurrency. No reply threads, participant ownership or final decision change.
3. Commit child/root Version and safe F18 fact per chosen reliability. Concurrent final decision/comments cannot overwrite each other.
4. Return confirmed core/new token. Network unknown → inquiry/recovery profile Q15; matching Text/actor/time alone not reliable request dedup, no blind resend with fresh ETag. Expired login preserves original Text/token F16.

### DF06. Withdraw pending

Внутренний write path после Api/FV/проверки caller: открыть owner ReadCommitted transaction, отдельным первым statement получить global pg_advisory_xact_lock на owner connection; только затем выполнять указанные ниже authoritative reads/row locks. Foreign facts — independent primary public ports без enlistment. Все guard/basis checks повторяются для этого commit; §7.2 задаёт failure/recovery и запрет внешнего ожидания под gate.

1. Current Specialist/shared scope, own precondition, load Pending; no SubmittedBy==CurrentUser guard invented.
2. Domain Withdraw stores actor/time/Withdrawn terminal, preserves target/comments/history. This is not F10 revision Withdrawal and does not revoke existing client content.
3. Commit own state/outcome/activity, atomically cancel exact bound item/matching head; no reattach through later manual iteration. No pending row deletion or automatic new iteration.
4. New submit same withdrawn exact artifact is independent DF01 and current eligibility; new ModerationId, old decision messages do not match it. Return confirmed DTO/ETag; own race or unknown outcome no UI fake success.

### DF07. Approve

Внутренний write path после Api/FV/проверки caller: открыть owner ReadCommitted transaction, отдельным первым statement получить global pg_advisory_xact_lock на owner connection; только затем выполнять указанные ниже authoritative reads/row locks. Foreign facts — independent primary public ports без enlistment. Все guard/basis checks повторяются для этого commit; §7.2 задаёт failure/recovery и запрет внешнего ожидания под gate.

1. Current Admin/own If-Match, load Pending target ID; check own stale412/current terminal409. Self-approval разрешён при обеих current permissions специалиста и админа по Q10/F17; нет обязательного actor inequality/four-eyes guard.
2. Reload exact revision/current new-activation lifecycle, F15/distribution-invalid dependencies/other authoritative restriction facts. Source release Retire alone not blocker. Confirmed invalidity →domain409; unavailable/partial facts →technical failure, not no blockers.
3. Под уже полученным first gate удержать revision lifecycle/own finality и fresh public CM basis до own commit (§7.2); carrier Q20/Q23. Read tracking/old own token does not certify independent facts. Shown Delivery comparison basis is mandatory independent precondition: re-read after gate; drift409→refresh/new consent; unavailable technical failure, not empty. Later basis drift does not undo committed Approval.
4. Domain Approve records exact actor/time/final status. Publishing UoW commits Approved + durable Approved outbox together; failure rolls both back, no externally sent fact before commit.
5. Return confirmed owned core/new ETag. Manual stops here. Automatic DF09 later revalidates matched exact result; UI can't call Activate as a substitute after automatic Approved.

### DF08. Reject/Return

Внутренний write path после Api/FV/проверки caller: открыть owner ReadCommitted transaction, отдельным первым statement получить global pg_advisory_xact_lock на owner connection; только затем выполнять указанные ниже authoritative reads/row locks. Foreign facts — independent primary public ports без enlistment. Все guard/basis checks повторяются для этого commit; §7.2 задаёт failure/recovery и запрет внешнего ожидания под gate.

1. Current Admin, meaningful Reason, exact Pending/token; validation400 vs own412/current terminal409 profile Q15.
2. Domain Reject stores trusted actor/time/reason, target immutable/unchanged. No automatic source correction/delete/release/revision regeneration.
3. Commit terminal history, safe outcome/outbox/activity and own token per chosen profile. Independent F11 item Reject keeps previous active pointer; no Activation/Delivery event.
4. Return confirmed core/new ETag. Specialist fixes source authoring/assignments then creates new release/revision; same hash new artifact still requires its own moderation. Restore Reason after expired session preserves original base/no replay.

### DF09. Automatic continuation

Внутренний write path после Api/FV/проверки caller: открыть owner ReadCommitted transaction, отдельным первым statement получить global pg_advisory_xact_lock на owner connection; только затем выполнять указанные ниже authoritative reads/row locks. Foreign facts — independent primary public ports без enlistment. Все guard/basis checks повторяются для этого commit; §7.2 задаёт failure/recovery и запрет внешнего ожидания под gate.

1. F11 prepare already committed own F10 revision+F17 Pending+exact item tuple/AwaitingModeration atomically. Public Submit cannot adopt/rebind operation metadata; no separate prepared-event Submit correctness dependency.
2. Consume committed Approved/Rejected exact fact idempotently; load actual iteration status and matched durable item/stage. Same revision with new iteration не equals old binding; stale/duplicate/unmatched decision cannot activate latest artifact.
3. Rejected keeps old pointer; bound Withdraw cancels exact item. Approved requires exact tuple/head/current captured Product Version/current Variant Sequence plus fresh parent/lifecycle/F15/final-effective restrictions. ABA pointer return not enough; terminal/superseded item not activated.
4. Protected F11 transition stores own state/history/outbox independently from original Approval commit. Unavailable/blocked stage visible/recoverable; no rollback Approved history or implicit re-moderation. Delivery later materializes F13, not approval shortcut.
5. Cancelled/Superseded never autoresume after Restore/Resolve/event. New manual iteration never reattaches; retained Approved artifact may manual Activate with fresh guards. Technical retries same own tuple; no adoption/hash matching.

### DF10. Review context/diff

1. Resolve exact revision from stored iteration, read F10 materialized set/source exact releases/ChangeComments/provenance/exclusions. No current mutable assignment recomposition, no schema/instance latest replacement.
2. Read current Delivery effective Variant basis through public adapter: exact revision/item snapshot/sequence/as-of либо confirmed no-effective. Missing/unready projection and read error не empty base; no license/Frozen/Publishing pointer substitution.
3. For complete inputs compute keyed semantic Added/ChangedVersion/Removed as §4.4. Exact pair changes shown despite equal bytes/hash; real Override result identity used. Nonchanging/provenance-only defaults do not fake field JSON comparison.
4. Read current approval eligibility separately; full blockers distinct from unavailable. Read context with own IDs/as-of/failure state, not Moderation core ETag or permit to Approve.
5. UI can refresh changed basis; final command reloads required eligibility, не automatically copies new context/ETag onto old local reason/decision intent. Rendering не mutates root or sends outbox.

## 10. Ошибки и диагностика

| Ситуация | Результат / сохранность |
| --- | --- |
| One Pending already exists / concurrent Submit | Typed existing-pending/domain race profile Q15; maximum one row, no duplicate root/fake saved result |
| Exact moderation/revision отсутствует |404 соответствующего ресурса; не подставлять latest/другой iteration |
| Invalid Text/Reason | Validation400/profile Q15; Reject no meaningful reason не finalizes |
| Required If-Match missing / own stale |428 / 412; no overwrite/comment lost or second final decision |
| Current terminal iteration | Domain409/profile Q15; immutable history remains |
| F10 target not new-use eligible / confirmed F15 or withdrawal dependency | Approve/Submit domain refusal; Pending not auto Rejected/Withdrawn |
| Source release Retired alone | Not automatic invalidation stored materialized target; historical exact read allowed по owner |
| Constraint/guard/read/transport unavailable | Technical failure/retry/as-of read error; no Approved=false/unblocked/empty diff fallback |
| Shown Delivery basis changed at Approve | Independent409; refresh comparison and new consent; current own ETag does not replace basis |
| Approval result/outbox known, F11/Delivery still pending | Approved history true; activation/effective separate, no rollback Approval from lag |
| Lost mutation response | Unknown outcome inquiry; no blind repeated comment/new Submit/final decision |
| F16 access/session denied |401/403 distinction; local Text/Reason+original own token recovery same actor, no forced foreign input |

Machine codes/messages/priority no-op/duplicate vs own precondition errors, dependency/actor/event profiles Q15/Q20/Q28. Logs: safe correlation/operation/item/moderation/revision/Variant IDs, own token category, lifecycle/blocker class, final outcome/time/outbox stage. No credentials/license keys, whole revision JSON/comment/reason payload dumping. F18 safe audit not substitute terminal history; alert thresholds/retention Q20/F18, not chosen numbers.

Global gate timeout/cancellation/failed port/owner connection loss — технический abort, не domain409 или empty valid facts. Unknown commit ACK требует inquiry/operation identity по Q15; reconnect continuation/blind retry с новым ETag не допускаются.

## 11. Тестирование

Стратегия [S04](../standards/S04-testing.md)/[S14](../standards/S14-test-environments.md) проверяет own finality, protected target facts и real outbox/correlation независимо от read UI. Current no-moderation checkout не доказывает эти сценарии.

| ID / поток | Уровень / SUT | Собственные данные / воздействие | Ожидаемый результат |
| --- | --- | --- | --- |
| W01 / DF01 | PostgreSQL/Functional fault | Lost Submit ACK, identical scoped ClientOperationId/input vs changed ID/input/source/terminal iteration | Original confirmed receipt replay after auth; changed bound input409; new ID follows new execution guards |
| W02 / DF07,10 | PostgreSQL/Functional | Admin saw effective R1/sequence; Delivery advances or becomes unavailable before Approve | Drift409 independent own ETag, fresh comparison/new consent; unavailable not empty; later drift not revoke committed Approval |
| W03 / DF09,F11 | PostgreSQL Integration | Atomic prepare crash; Withdraw bound iteration then manual iteration; duplicate Approved after manual/no-op/Cancel/new head | Exact own tuple or none; bound item cancelled, no reattach; head/baseVersion/VariantSeq ABA rules enforce no stale activation |
| W04 / DF01,07 | PostgreSQL Integration | Suppressed A unavailable vs final B with valid/shared-invalid dependency | Only final-effective closure restrictions block; source/evidence retained, not deleted |
| T01 / DF01 | Domain/Application Unit | Exact available artifact, valid actor, prior Withdrawn; many old terminal rows | New stable Pending, max one; same target withdrawn new ID, history retained |
| T02 / DF01 | PostgreSQL Integration | Concurrent new submits same revision/Pending partial unique | One Pending, loser typed confirmed race; no duplicate/adoption foreign operation |
| T03 / DF05–08 | Domain + Functional | Pending comments/Text/Reason; no reason/whitespace; terminal operations | Comment append immutable, meaningful Reject, terminal guards, no revision edits |
| T04 / DF07–08 | HTTP Functional | Two admins same ETag decide/withdraw/comment | First valid committed outcome; stale412, no LWW/final overwrite; missing428 |
| T05 / roles | Functional real F16 context | Specialist/Admin/combined, self SubmittedBy==DecidedBy | Shared read/Submit/Withdraw; Admin decision/comment; self-approval only permitted roles; no creator isolation |
| T06 / DF07 | Integration real guarded Publishing writers | Approve check then F10 Withdrawal/Retire target before commit; simultaneous finality | Protected eligibility serialization enforced; no approved command on invalid target; history/outbox coherent |
| T07 / DF01/07 | Integration public BC guard contour | F15/distribution invalidation or read failure after Submit/while Approve | Confirmed blocker vs technical refusal distinct; Pending preserved, source Retire alone not blocker |
| T08 / DF07–09 | PostgreSQL+outbox Integration | Fault before/at commit, crash after committed decision before delivery | Approved + durable fact together or neither; eventual idempotent F11 recovery; no early event |
| T09 / DF09 | Real F11/Publishing integration | Manual vs automatic exact matched decision; batch Approved/Rejected independent | Manual no auto activation; automatic exact matched revision, previous pointer retained for Rejected |
| T10 / DF09 | Integration correlation/order | Duplicate/unmatched/old iteration, late Approval after manual/operation/state change, withdrawn resubmit | No newer iteration/Latest activation from old fact; exact F11 head+ABA/tuple/recovery rules, no forever awaiting shortcut |
| T11 / Approval gate | Functional F11/F17 | R1 Approved; R2 same hash/new ID; only Rejected/Withdrawn; Approved then Withdrawal | Exact only; no hash transfer; F11 current safety recheck; history not rewritten by ban |
| T12 / DF10 | Unit SemanticDiffBuilder | Added/removed/result A→B override, same stable ID new schema/data version, equal bytes new version | Three semantic kinds by real result ID/exact pairs; no Key/Name match/field JSON dependency |
| T13 / DF10 | Integration/Functional public Delivery read | Confirmed effective/null vs missing projection/outage; Publishing pointer differs | Correct Delivery basis/as-of; missing/unavailable not empty/all Added; no license Frozen basis |
| T14 / DF02–04 | Functional/query Integration | Queue/context/actor filter/all outcomes, actor captions/provider changes | Shared scope, retained history, no per-row/core composite ETag, GET no writes |
| T15 / HTTP/browser | UI + real owner API | Expired Comment/Reject form, same/different actor, intervening comment/final decision, lost response | Preserve Text/Reason/original own ETag; no replay/overwrite/duplicate note, current state explicit |
| T16 / architecture/stages | Architecture + Functional Demo/MVP | Assemblies/DI/ports/routes, Demo host | No foreign DbContext/Identity FK; new query layer, true approval gate only MVP, no universal moderation surface |

Unit doubles для facts/outage/clock допустимы, но mock guard не доказывает cross-BC race safety. Integration использует PostgreSQL/Testcontainers с собственными F10 artifacts, iterations/comments/op-items/outbox и deterministic barriers; cleanup своих fixture rows, не deletion runtime history. Real broker/outbox/inbox test использует RabbitMQ/Testcontainers when checking transport/correlation, direct handler call не считается transport verification.

Functional поднимает API+real own PostgreSQL и actual selected F16 authentication, Role composition может иметь узкую test principal boundary, но это не proof login MVP. Port doubles явно ограничивают утверждение: valid mock F15/Delivery факты не real guard/projector checks. Same API multiple BC remains Functional, не E2E. Real UI→CM→automatic F11→Delivery review/activation E2E требует necessary real apps/dependencies; schemas/instances/codegen не получают новый mandatory moderation workflow из тестовой setup.

Open Q28 re-submit/appeal, Q20 codec/lease/mappings и Q15 wire/error/privacy не выбираются через молчаливый expected result. Здесь выполнены source/documentation checks; app tests, API/broker/DB/browser/provider/migrations не запускались.

| ID / DF | Уровень / SUT | Исходные данные / сценарий | Ожидаемый результат |
| --- | --- | --- | --- |
| G01 / guarded write flows | PostgreSQL Integration, separate owner connections/CM replicas | Pause writer after gate; concurrent relevant writer with same stable key | Second writer waits; first commits own BC only; second loads fresh committed facts, no forbidden stale commit |
| G02 / guarded write flows | PostgreSQL Integration + architecture/DI | Independent public primary reads while owner holds gate; foreign UoW enlistment/shared multi-DbContext participant | Reads see committed facts after acquisition; no foreign EF/transaction participant/cache/replica; own writes+outbox/audit intent/inbox atomic |
| G03 / guarded write flows | Fault integration | Bounded acquisition cancellation/timeout, failed read, owner connection loss, lost commit ACK | Technical abort/unknown outcome; no fake compatible empty facts or reconnect continuation; original own412 and independent basis409 preserved |

## 12. Порядок реализации

1. Реализовать MVP gate/confirmed receipt/atomic automatic binding/head+ABA/shown comparison consent; уточнить Q20 codec/DI и Q23 fact carriers; Q15 core/context/DTO/error/recovery profiles, Q28 extra submit/comment/diff decisions.
2. Domain roots/immutable comments/status/actor/time/constraints, reuse exact F10 target, no content clone or four-eyes ownership rule.
3. Abstractions commands/queries/approval/public review ports; new Publishing.EntityFramework.Persistence repo/projection/specifications; migrations/FK/partial unique/token/outbox chosen Q20.
4. Application Submit/Comment/Withdraw/Approve/Reject protected use cases и Host/current actor binding. F11 integrates atomic prepare/internal Submit/exact head+ABA approved continuation, not legacy direct Activate shortcut.
5. Review adapters read exact materialized/source/provenance and current Delivery basis with complete/error distinctions; Api core own token отделить от composite exposure.
6. Verified OpenAPI/NSwag → frontend shared queue/workspace/comments/reason/iteration history и same-user recovery. Проверить real PostgreSQL finality/races/outbox и F11 independent stage outcomes.
7. Отдельно проверять end-to-end Delivery effective и approval!=availability; Demo remains without F16/F17/broker dependence.

## 13. Открытые технические вопросы

| Вопрос | Что блокирует |
| --- | --- |
| [Q28](../../../business/questions/Q28-content-review-scope.md#f17-technical-boundaries) | Extra unchanged-Rejected/Approved submit/reconsideration cases, comment terminal behavior beyond Pending, source restriction eligibility/diff metadata semantics; core review target already accepted |
| [Q15](../questions/Q15-api-and-source-code.md#f17-moderation-api-и-review-reads) | Routes/wire enums/actors/labels/comments/history/list params, core vs live exposure/error/status/headers, confirmed ClientOperationId receipt/ShownComparisonBasis carrier, normalization/errors/privacy |
| [Q20](../questions/Q20-persistence-and-migration.md#f17-decision-atomicity-и-automatic-binding) | Lifecycle/finality gate выбран (§7.2); own schema/status/index/actor privacy/outbox codec; exact atomic binding/head/ABA/receipt already approved F11/Q20 |
| [Q23](../questions/Q23-source-synchronization.md) / [F15](F15-catalog-conflicts.md#44-services--algorithms) | Public distribution/catalog fact carriers/source mappings; MVP gate coordination выбрана (§7.2), no shared transaction |
| [F16/Q24](../../frontend/questions/Q24-admin-sign-in.md#f16-техническая-модель-доступа-и-восстановления) | Actual selected auth/provider and same-user Text/Reason recovery/storage; F16 current access checkpoint reused |

Не возвращать Approved-only activation, manual/automatic distinction, exact identity/no hash transfer, meaningful Reject, Pending Withdraw, shared specialists/self-approval и retained history в список невыбранных требований.
