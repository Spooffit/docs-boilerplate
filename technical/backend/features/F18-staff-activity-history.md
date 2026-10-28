# F18. История действий сотрудников: бэкенд

[Бизнесовая спецификация](../../../business/features/F18-staff-activity-history.md) · [Frontend](../../frontend/features/F18-staff-activity-history.md) · [M06](../modules/M06-staff-workflow.md) · [Q10](../../../business/questions/Q10-permissions-and-isolation.md#f18-history-scope-и-visibility).

## 1. Область реализации

F18 предоставляет MVP persisted append-only read model подтверждённых действий с автором, временем, целью и результатом. История сохраняется после reload и не является toast/button press/server log. Общий Activity History screen выбран актуальным Q13; отдельный обязательный audit в каждой карточке не вводится.

Own histories F11 activation, F12 policy и F17 moderation остаются authority своих предметных фактов. F18 не event store бизнесовых BC, не entitlement/availability timeline, не модерация и не механизм rollback/replay. Замечание возврата хранится в F17 и доступно по exact reference; safe audit summary не должен превращать его отсутствие/отказ чтения в «причины нет».

Source owner атомарно фиксирует safe durable activity intent вместе с подтверждаемым изменением своего состояния. Dispatch происходит после commit, consumer независимо строит read record. Audit DB/broker lag не откатывает уже committed source command. Неперсистентный after-commit callback как единственный источник не обеспечивает историю при crash между commit и отправкой.

Только MVP: Demo без Identity не зависит от персонального журнала и не создаёт fake employee для audit. Full before/after каждого поля, raw content/payload logs, экспорт, failed login/command journal, notifications/realtime не входят; all records retained MVP without automatic purge/cascades, later archive/export/anonymization explicit.

Accepted [Q15 activity contract](../questions/Q15-api-and-source-code.md#f18-принятый-activity-contract) and [Q20 capture/append/cursor](../questions/Q20-persistence-and-migration.md#f18-принятый-capture-append-и-cursor-read) apply below.

Source evidence checkout 05.10.2026:

| Источник | Реализовано сейчас | Граница вывода |
| --- | --- | --- |
| Scoped src search .cs/.csproj без bin/obj/Migrations | StaffActivityRecord/StaffActivityEvent/activity API и audit module не найдены | Новые модели/queries/consumer/producers/DI предстоит реализовать |
| [AuditedAggregateRoot](../../../../contentmanager.api/src/BoundedContexts/Shared/ContentManager.Domain.Shared/Entities/AuditedAggregateRoot.cs) | Shared audited entity base существует | Entity metadata не append-only персональный журнал/авторизация сотрудников |
| [CatalogSynchronizationServices](../../../../contentmanager.api/src/App/ContentManager.Application/Integration/CatalogSynchronizationServices.cs), [IntegrationDbContext](../../../../contentmanager.api/src/App/ContentManager.EntityFramework/Integration/IntegrationDbContext.cs), [Inbox adapter](../../../../contentmanager.api/src/App/ContentManager.EntityFramework/Integration/InboxUnitOfWorkAdapter.cs) | Есть LeanSoft inbox infrastructure Catalog synchronization/ambient transaction adapter | Не доказательство F18 event/outbox producer atomicity и не разрешение общей transaction всех BC |

## 2. Domain

### 2.1. Aggregates, entities и value objects

Audit — immutable technical persistence/read contour M06, не новый business BC/event store. Source owner captures durable intent; independent audit append never imports Identity roots/foreign Domain entities or enriches history by current foreign joins. Target names below selected logical types; exact physical codecs Q15/Q20.

| Модель | Роль | Состояние / lifecycle | Владелец / граница |
| --- | --- | --- | --- |
| SourceActivityIntent | Immutable source fact payload | Captured with source business result, stable EventId | Each source owner own outbox/UoW |
| StaffActivityFact | Preserved normalized safe semantic fact | Immutable source values | Shared technical contract, no provider/business roots |
| StaffActivityEvent | Versioned transport payload | Same EventId/fact across at-least-once dispatch | Source adapter §3 |
| StaffActivityRecord | Immutable append-only record | Create once, no Version/update/delete | Audit own persistence |
| ActivityActorReference / initiation | Executor and optional prior staff initiator | As-of safe refs, no impersonation | Shared immutable values |
| ActivityTargetReference | Exact typed reference/context | No current/latest caption substitution | Shared immutable value |
| ActivityMetadata variants | Typed whitelist | Safe IDs/states/roles/policy/counts only | Shared technical contract |
| ActionProfileDefinition | Fixed trusted registry | Action classification/metadata/visibility | Server registration, no producer arbitrary audience |

**SourceActivityIntent (source owner immutable payload)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| EventId | Guid | Да | Generated once for independent committed business result | Stable across retry/replay dispatcher; not CorrelationId |
| ContractVersion | int | Да | Captured event/schema profile | Supported version registry; numeric literals Q20 |
| Fact | StaffActivityFact | Да | Normalized original safe source fact | Atomic with source mutation; no memory-only after-commit gap |
| CapturedAt | DateTimeOffset | Да | Source intent technical capture time UTC | Not physical commit time; excluded semantic duplicate comparison |

**StaffActivityFact (immutable normalized source fact)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SourceModule | string | Да | Trusted source owner code | Registered producer+action profile; no browser free label |
| OccurredAt | DateTimeOffset | Да | Source action time UTC | No physical commit/global causal promise; preserve original precision for equality |
| Actor | ActivityActorReference | Да | Actual executor kind/identity | Trusted source context |
| Initiation | ActivityInitiationReference? | Нет | Optional persisted initiating staff context | Async System remains executor; never impersonates initiator |
| ActionCode | string | Да | Registered business action | Fixed action profile determines visibility/metadata |
| Target | ActivityTargetReference | Да | Exact affected object/version/context | Mutable draft ref identifies current object, not historical content snapshot |
| Outcome | ActivityOutcome | Да | Committed enum value | Source commit succeeded; partial workflow result represented separately |
| BusinessResult | ActivityBusinessResult | Да | Typed committed domain/workflow result | Blocked/partial not fake all Activated |
| CorrelationId | Guid? | Нет | Safe cross-action correlation | Not dedup key |
| OperationId | Guid? | Нет | Exact workflow linkage | Selected operation ref, not retry identity replacement |
| Metadata | ActivityMetadata? | Нет | Approved typed safe action data | No raw data/content/before-after/secret/ETag/URL |

**StaffActivityRecord (own immutable technical record)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | Guid | Да | Audit-generated immutable row ID | Canonical PostgreSQL uuid ordering for keyset; no Version |
| SourceEventId | Guid | Да | Accepted event identity | Unique, one original record |
| NormalizationProfileVersion | int | Да | Preserved normalized comparison profile | Versioned field semantics; exact codec Q20 |
| SourceFact | StaffActivityFact | Да | Preserved full-fidelity original normalized fact | Duplicate equality uses full preserved normalized source fact, not lossy timestamptz reconstruction |
| SortOccurredAtUtcTicks | long | Да | Exact normalized source OccurredAt UTC100ns ticks for SQL order | Private bigint derived value, not another clock; preserves full source precision |
| RecordedAt | DateTimeOffset | Да | Audit append observation time UTC | Never source commit/causal order; reflects lag |

**ActivityActorReference**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Kind | ActivityActorKind | Да | Staff/System/Integration/Operator enum | Actual trusted executor |
| StaffId | Guid? | Условно | CM staff ID only for Kind=Staff | Required Staff; null other kinds; no Identity FK |
| DisplayNameSnapshot | string? | Нет | Optional safe as-of executor caption | Rename/deactivate/delete never rewrites row; no current enrichment |

**ActivityInitiationReference**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| InitiatorStaffId | Guid | Да | Original accepted workflow staff initiator | Trusted persisted start context, not current job actor |
| DisplayNameSnapshot | string? | Нет | Optional original safe caption | No directory dependency/enrichment |

**ActivityBusinessResult (typed registered value)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Code | string | Да | Committed business-result discriminator | Fixed per-action registered result; exact wire literals Q15; never generic exception/HTTP status |

**ActivityTargetReference**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| TargetType | string | Да | Registered resource kind | Fixed ActionProfile target kinds |
| TargetId | string | Да | Canonical exact ID by target kind | Guid/accepted owner ID conversion profile Q15; no URL |
| ExactVersionId | Guid? | Условно | Exact version entity ID where applicable | Not own concurrency Version/ETag or implicit latest |
| Context | IReadOnlyList<ActivityRelatedReference> | Да | Exact captured owner/parent/cause refs | Bounded allowlist per action; empty complete context explicit |
| DisplayNameSnapshot | string? | Нет | Optional safe as-of target caption | No rename/delete rewrite; mutable draft no snapshot promise |

**ActivityRelatedReference**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| TargetType | string | Да | Known related type | Registered per action |
| TargetId | string | Да | Exact related identity | No live parent/name guessing |
| ExactVersionId | Guid? | Условно | Exact subordinate version if required | No latest fallback |

ActivityMetadata is a closed typed union of immutable variants below. Discriminator/serializer contract Q15, not arbitrary dictionary/JObject/full source entity. Every variant may contain only its declared fields; captions are safe optional as-of labels, never raw content values.

**LifecycleActivityMetadata : ActivityMetadata**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| RelatedReferences | IReadOnlyList<ActivityRelatedReference> | Да | Approved exact source/parent refs | Not graphs/raw payload |
| PreviousState | string? | Нет | Known old lifecycle state | Fixed per-action enum mapping, no arbitrary producer text |
| NewState | string? | Нет | Known committed state | Registered state value, not desired unsaved state |

**RoleActivityMetadata : ActivityMetadata**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| PreviousRoles | IReadOnlySet<string> | Да | Old fixed membership names | Specialist/Administrator only |
| NewRoles | IReadOnlySet<string> | Да | Committed fixed membership names | No provider stamps/direct grants/private principal |

**PolicyActivityMetadata : ActivityMetadata**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| PreviousPolicy | ActivityPolicyValue? | Нет | Old local policy value | Freeze/Deny logical enum; null clear/inheritance |
| NewPolicy | ActivityPolicyValue? | Нет | Committed local policy value | No as-of entitlement inference |
| PolicyVersionId | Guid | Да | Exact owner immutable policy version | No policy body/own ETag |
| RelatedReferences | IReadOnlyList<ActivityRelatedReference> | Да | Captured policy owner/applied context | Exact refs |

**PublicationWorkflowActivityMetadata : ActivityMetadata**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| PromotionItemId | Guid? | Нет | Exact item where item result | OperationId in parent fact |
| ProductReleaseId | Guid? | Нет | Exact captured base release | No hash-dedup/current latest |
| VariantReleaseId | Guid? | Нет | Exact captured local release | Selected frozen tuple |
| RevisionId | Guid? | Нет | Exact created/activated revision | Intentional new same-content artifact distinct |
| ModerationId | Guid? | Нет | Exact bound moderation iteration | Atomic prepare tuple correlation retained |
| PreviousActiveRevisionId | Guid? | Нет | Previous actual publication pointer | Null real state |
| NewActiveRevisionId | Guid? | Нет | New actual pointer | No fake pointer transition for no-op/Cancel |
| Counts | ActivityWorkflowCounts? | Нет | Committed aggregate workflow summary if relevant | No item rewrite/all activated claim |

**ActivityWorkflowCounts**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Total | int | Да | Committed operation membership count | Nonnegative; zero operation allowed |
| Activated | int | Да | Committed activated item outcomes | Not Delivery ready |
| Blocked | int | Да | Committed blocked outcomes | No fake source command rollback |
| Cancelled | int | Да | Committed cancelled items | No auto-resume |
| Superseded | int | Да | Committed superseded items | Not undone historical activations |

**ModerationActivityMetadata : ActivityMetadata**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ModerationId | Guid | Да | Exact iteration | No attach to new manual iteration |
| RevisionId | Guid | Да | Exact moderated artifact | Full reason stays F17 |
| CommentId | Guid? | Нет | Exact comment identity if relevant | No comment/reason body copy |
| ReasonReference | ModerationReasonReference? | Условно | Authoritative explanation pointer | Required where committed result has reason; unavailable≠empty |

**ModerationReasonReference**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ModerationId | Guid | Да | Exact owner iteration | Own authorized read F17 |
| RevisionId | Guid | Да | Exact associated revision | No latest substitution |
| CommentId | Guid? | Условно | Exact comment/reason record where applicable | No protected reason text in audit |

**ConflictRepairActivityMetadata : ActivityMetadata**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ConflictId | Guid | Да | Exact F15 conflict | No repair payload values |
| SourceVersion | int | Да | Normalized committed source version from F15 owner | Repair and Resolve independent captures; legacy source EntityRevision long is not this contract |
| RelatedReferences | IReadOnlyList<ActivityRelatedReference> | Да | Exact patch/owner/application refs | No full JSON patch/data |

**StaffAccessActivityMetadata : ActivityMetadata**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SetupActionId | Guid? | Нет | Exact setup/reset issuance/completion action | No code/hash/stamp |
| PreviousState | string? | Нет | Safe registered access/readiness state | No provider security dump |
| NewState | string? | Нет | Committed safe state | Staff-access rows Admin-only profile |

**ActionProfileDefinition (server-fixed immutable registry)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SourceModule | string | Да | Admitted producer owner | Trusted registration |
| ActionCode | string | Да | Unique owner action code | No arbitrary producer classification |
| Visibility | ActivityVisibility | Да | GeneralStaff or StaffAccessAdministrative enum | Derived fixed registry, not supplied event audience |
| TargetTypes | IReadOnlySet<string> | Да | Allowed exact target kinds | Capture validator |
| MetadataTypes | IReadOnlySet<string> | Да | Allowed typed variant names | No raw dictionary |
| BusinessResults | IReadOnlySet<string> | Да | Known committed result values | Partial/Blocked/Prepared distinguish activation |
| DisplayName | string | Да | Safe catalog label | UI filter options, exact localization Q15 |

### 2.2. Методы и переходы

| Тип / boundary | Метод | Параметры | Предусловия | Изменение / результат |
| --- | --- | --- | --- | --- |
| Source builder | CaptureCommittedIntent | Actual independent changed business result/trusted context | Registered audited action, safe typed fact | Stable EventId+immutable payload in SAME source commit |
| Dispatcher | SendStoredIntent | Durable captured payload | Source committed | At-least-once same EventId; no memory-only gap |
| Audit record | Create / AppendOnce | Validated event/original profile/new own ID/time | Source admitted, normalization consistent | Immutable row+inbox commit or original duplicate |
| Duplicate validator | ComparePreservedFact | Same SourceEventId and preserved normalized fact/profile | Independent audit transaction | Same→original ACK; conflicting→quarantine no overwrite |
| Read scope builder | ApplyVisibility | Current F16 snapshot/fixed profiles | audit.activity.read; staff rows need extra permission | Visibility before filters/page/options |
| Cursor reader | ReadNext | Viewer+normalized filters+SQL key | Fresh auth each request; valid bound cursor | SQL DESC keyset, no frozen global snapshot |

### 2.3. Инварианты и constraints

1. One record per independent committed business result, Outcome=Committed. Typed BusinessResult represents blocked/partial/terminal workflow results, never fake all Activated.
2. Genuine no-op/same ClientOperationId confirmed replay adds no fact. Intentional F09/F10 same-content new artifact is new result/new record. Whole Save tree one, automatic prepare revision+moderation+item tuple one, human approval/activation separate.
3. Staff executor Guid only Staff; System/Integration/Operator explicit. Persisted initiating staff optional for async work, not executor impersonation. Operator/bootstrap no fake CM staff user.
4. Source action time UTC and audit append UTC different, neither physical commit/global cause order. Preserve as-of optional actor/target captions without rename/deactivate/delete cascade; no Identity/target FK.
5. Metadata typed allowlist IDs/old-new states/roles/policy/counts/operationrefs; no raw values/full before-after/storage URL/ETag/credential/hash/cookie/code. F17 full reason exact owner read; missing/error≠empty.
6. audit.activity.read granted Specialist+Administrator. Staff-access rows require audit.staff-access.read Administrator only. Fixed source ActionProfile determines class, producer cannot self-label general. Fresh ICurrentUser/PermissionSnapshot once per request; no interrequest permission cache.
7. Server visibility before page/count/options; audit visibility never grants target navigation. Actor options come from visible historical captions/IDs, not admin user directory.
8. Filters §3 selected, UTC [From,To), no default time cutoff; SQL normalized source UTC100ns ticks DESC (exact OccurredAt order), Id DESC using PostgreSQL canonical uuid ordering. Default50/max200, reader+normalized-filter-bound opaque cursor, fresh auth per call; no frozen feed snapshot/global drained claim.
9. Retain all MVP records without automatic purge/user deletion/cascades. Future archive/export/anonymization explicit decision; not legal forever retention promise.
10. Duplicate same EventId comparison uses preserved original normalized actor/action/target/outcome/metadata/source-time/context profile, not lossy timestamptz-reserialized row, envelope/retry/row ID/RecordedAt. Conflicting same ID quarantined; no old truth overwrite.
11. Login failures/security counters/transport retries/inbox/GET/search/preview/download/codegen/plain projection/Delivery materialization excluded. F16 provider guard writes stay provider owner; no audit login/security-counter noise.
12. General /admin/activity read-only eventually updated; manual refresh, no local optimistic row or waiting audit append before source success. F11/F17 owner histories remain authority.

### 2.4. Domain events

Source safe activity event defined once §3.3, distinct from raw DomainEvents/owner history. No event solely for trace logging, audit append does not cascade new business action/notification. All audited source actions use same captured committed-result granularity and stable EventId.

## 3. Application.Abstractions

### 3.1. Commands

Public Create/Update/Delete/Undo absent. Internal AppendStaffActivityCommand handled by technical Audit.Application, not arbitrary browser API. Source owner builders receive actual business result; no additional source lifecycle command invented.

**AppendStaffActivityCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Event | StaffActivityEvent | Да | Trusted versioned transport fact | Validated producer/profile; original stable EventId |

### 3.2. Queries

| Query | Назначение | Result |
| --- | --- | --- |
| ListStaffActivityQuery | Authorized server filters/keyset | StaffActivityPageRead |
| GetStaffActivityQuery | Exact retained visible row | StaffActivityRead |
| GetStaffActivityFilterOptionsQuery | Current visible historic authors/registered actions | StaffActivityFilterOptionsRead |

**ListStaffActivityQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Parameters | StaffActivityListParameters | Да | Selected filters/cursor/limit | Fresh current reader context not body actor |

**GetStaffActivityQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ActivityId | Guid | Да | Exact audit record | Not source target ID |

GetStaffActivityFilterOptionsQuery no mandatory input: current snapshot defines visibility; exact optional narrowing if later needed remains Q15, no admin directory access dependency.

### 3.3. DTO и parameter models

Selected typed immutable read values below do not expose private Version/EF roots/inbox/full producer envelope; exact DTO member casing and polymorphic wire codecs engineering Q15. Shared safe nested fact/reference/metadata models §2 reused, no second copy.

**StaffActivityEvent (versioned payload)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| EventId | Guid | Да | Original durable source fact identity | Preserved every delivery |
| ContractVersion | int | Да | Supported immutable contract profile | Numeric version/transport registration Q20 |
| Fact | StaffActivityFact | Да | Full-fidelity normalized safe source fact | Compared after admitted versioned normalization; no arbitrary raw payload |

**StaffActivityRead (immutable public value)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | Guid | Да | Audit record identity | Read-only |
| OccurredAt | DateTimeOffset | Да | Original action time UTC | No global causal/physical commit promise |
| RecordedAt | DateTimeOffset | Да | Own audit append time UTC | Visible eventual lag, not source success gate |
| Actor | ActivityActorReference | Да | Captured executor/caption | No Identity entity/directory lookup |
| Initiation | ActivityInitiationReference? | Нет | Optional captured workflow initiator | Executor remains actual kind |
| ActionCode | string | Да | Registered performed action | Safe catalog caption mapping |
| Target | ActivityTargetReference | Да | Exact source object/version/context | Current mutable draft read only, no old content promise |
| Outcome | ActivityOutcome | Да | Committed | No fake failed source action |
| BusinessResult | ActivityBusinessResult | Да | Typed committed result | Partial/block/prepare not all activated |
| CorrelationId | Guid? | Нет | Safe selected correlation filter value | No secret |
| OperationId | Guid? | Нет | Exact operation linkage | No hidden automatic resume |
| Metadata | ActivityMetadata? | Нет | Typed allowed details | Full F17 reason outside audit |

**StaffActivityListParameters**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Filters | StaffActivityFilters | Да | Normalized semantic filter set | No default time cutoff |
| PageSize | int? | Нет | Default50, maximum200 | Positive<=200; exact query member wire Q15 |
| Cursor | string? | Нет | Opaque next-page token | Reader+normalized filter bound, not credential; codec/version/size/signing literals Q15 |

**StaffActivityFilters**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ActorStaffId | Guid? | Нет | Historic executor staff filter | No forced personal scope |
| ActorKind | ActivityActorKind? | Нет | Staff/System/Integration/Operator filter | Only visible rows |
| ActionCode | string? | Нет | Registered action filter | Authorized action profile |
| TargetType | string? | Нет | Registered target kind | Canonical taxonomy wire Q15 |
| TargetId | string? | Условно | Exact canonical target | Needs corresponding TargetType |
| From | DateTimeOffset? | Нет | UTC inclusive lower source-time bound | [From,To), none by default |
| To | DateTimeOffset? | Нет | UTC exclusive upper source-time bound | From<To when both set |
| CorrelationId | Guid? | Нет | Safe exact correlation | No uniqueness claim |
| OperationId | Guid? | Нет | Safe exact workflow operation | Selected captured linkage |

**StaffActivityCursorRead (private immutable decoded value)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ReaderStaffId | Guid | Да | Current reader binding | Different reader cannot reuse as global token |
| Filters | StaffActivityFilters | Да | Original normalized semantic filter values | Equivalent normalized set required; no fresh grants embedded |
| LastUtcTicks | long | Да | Actual stored bigint source UTC100ns key | Exact keyset boundary; no lossy timestamptz rounding |
| LastId | Guid | Да | Last actual row key | PostgreSQL uuid ordering, no .NET/string replacement |
| CodecVersion | int | Да | Supported cursor profile | Exact token codec/signing literals/size Q15; not frozen global snapshot |

**StaffActivityPageRead**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Items | IReadOnlyList<StaffActivityRead> | Да | Authorized ordered page | No forbidden rows/count leakage |
| NextCursor | string? | Нет | Opaque continuation if more | Bound to reader/filters/current lastSQLkey |
| HasMore | bool | Да | More authorized rows observed for this query | No mandatory total count/global drained guarantee |

**StaffActivityActorOptionRead**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ActorKind | ActivityActorKind | Да | Visible historical executor kind | No currently active-user filter |
| ActorStaffId | Guid? | Условно | Staff ID for historical Staff option | No Identity lookup/FK |
| DisplayNameSnapshot | string? | Нет | Safe caption from visible historic row | No private directory leak; exact multiple-as-of-caption aggregation Q15 |

**StaffActivityActionOptionRead**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ActionCode | string | Да | Authorized registered profile code | Hidden staff-access profile absent for Specialist |
| DisplayName | string | Да | Safe registered action label | Server-fixed catalog |

**StaffActivityFilterOptionsRead**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Actors | IReadOnlyList<StaffActivityActorOptionRead> | Да | Options from visible retained history | No all-user directory |
| Actions | IReadOnlyList<StaffActivityActionOptionRead> | Да | Authorized fixed action profiles | No arbitrary event labels or hidden admin class |
| TargetTypes | IReadOnlySet<string> | Да | Authorized profile target kinds | No unrelated staff access taxonomy leakage |

### 3.4. Ports

| Port | Назначение | Направление |
| --- | --- | --- |
| ISourceActivityIntentWriter / source builder | Own state+safe intent atomic capture | Each owner UoW; not shared Audit transaction |
| ISourceActivityDispatcher | At-least-once committed intent delivery | After source commit, same EventId |
| IStaffActivityAppendStore / audit inbox | Append/original lookup/preserved fact equality | Own Audit Persistence transaction |
| IActionProfileRegistry / normalization service | Fixed owner action/target/metadata/result/visibility | Server registration/immutable definitions |
| ICurrentUser / PermissionSnapshot / IPermissionChecker | Current reader once/request | F16 public authorization abstraction; no Identity roots |
| IActivityCursorCodec | Encode/decode reader/filter-bound SQL key | Immutable value, not authorization source |
| Owner target/F17 reason read ports | Independently authorized exact reference | Source public read boundary; no foreign ORM |
| TimeProvider | Source capture/audit append UTC | Clock semantics separate, no physical commit stamp |

### 3.5. Specifications / read contracts

[S19](../../../catalog/docs/standards/dotnet/ardalis-specification.md): compose fixed authorized ActionProfile visibility predicate then actor/action/target/UTC range/correlation/operation filters, then keyset boundary. No Count/page/options before visibility. [S20](../../../catalog/docs/standards/architecture/cqrs.md#handler-responsibilities-and-dependency-direction) List/Get/FilterOptions handlers in Audit.EntityFramework.Persistence; SQL stable descending SortOccurredAtUtcTicks bigint+Id UUID, server limit+one extra authorized row yields HasMore. No offset/Page/Sort choice or mandatory totals. Actor options visible historic IDs/captions, not admin account listing. Cursor normalized filters/current reader checked before executing; fresh permission each call, never frozen session authorization.

## 4. Application

### 4.1. Handlers / use cases

Source capture builder maps actual independent committed business result to registered typed safe fact using trusted source actor/context/target. EventId generated before source commit and intent included own UoW; failed source transaction rolls back both. One action tree Save one intent; intentional new release/revision identical contents distinct artifacts/results. ClientOperationId replay and real no-op no new capture.

AppendStaffActivityHandler validates producer/contract/profile/typed metadata/actor/result, preserves normalized source-fact fidelity, opens own Audit transaction and checks SourceEventId/inbox. Original equal fact returns original result/ACK; changed fact quarantine preserves original, no overwrite. New own record ID/RecordedAt plus original source fact/index projections and inbox marker atomic, ACK after commit. Unique race rereads original in fresh own scope and compares source evidence before ACK.

List/Get/FilterOptions readers receive fresh F16 snapshot; audit.activity.read required, staff-access action profiles need audit.staff-access.read. Fixed registry class is authority, not event-provided label. Source target navigation always separate permission/resource validation. Exact hidden-row/not-found privacy codes Q15, but no forbidden row/options/page/count leak.

### 4.2. Validators

[S18](../../../catalog/docs/standards/dotnet/fluentvalidation.md): ActivityActorValidator (Staff ID only Staff), TrustedActionProfileValidator, ExactTargetReferenceValidator, TypedMetadataVariantValidator, CommittedResultValidator, UtcIntervalValidator, CursorBindingValidator, PageSizeValidator50/200. Builder whitelist positive inclusion, not regex-blacklist redaction after full entity serialization. No generic source payload allowed. Schema/codec/text limits Q15/Q20; semantic scope/routes/defaults selected here.

### 4.3. Mappers

SourceResultActivityMapper uses actual source IDs/as-of optional safe labels/context and one committed-result granularity. Async System executor keeps persisted InitiatorStaffId, not Staff substitution. Fixed action/profile mapper builds typed safe metadata; no raw content/cookie/code/storageURL/ETag/full before-after. RecordReadMapper projects preserved fact/captions, not current foreign labels. Private SortOccurredAtUtcTicks is derived from normalized source UTC100ns ticks, never a separate clock or replacement for preserved fact evidence; source UTC range bounds use matching bigint ticks. Cursor mapper uses exact stored SQL keys/canonical UUID comparator; renderer uses registered action labels/owner refs, not arbitrary URL.

### 4.4. Source coverage / algorithms

| Source family | Accepted capture unit | Visibility / exclusions |
| --- | --- | --- |
| Authoring/media owners | Create, actual Save, Publish, approved lifecycle/Delete already defined by owner | Whole saved tree one; no invented lifecycle command/raw data |
| Publishing F09/F10/F11 | Release/revision create, promotion start, atomic prepare tuple, item committed outcomes, Activate/Rollback/Unpublish/Withdrawal/policy Set/Clear/Cancel | Actual manual pointer transition only; true no-op/replay no record |
| F17 | Submit/Comment/Approve/Reject/Withdraw | Exact iteration/revision/reason refs; no full comment/reason body |
| F15 | Patch repair and Resolve | Two own commits/intents; old basis rejected Resolve no success fact |
| IdentityAccess F16 | Create/roles/deactivate/reactivate/setup-reset issue+complete/bootstrap/emergency | StaffAccessAdministrative fixed profile; no login failures/security counters |
| Async/integration/operator execution | Same registered independent committed business result | Actual System/Integration/Operator executor; optional original staff initiation |
| Excluded runtime/infrastructure | GET/search/preview/download/codegen/loginfail/transport/inbox/retry/plain projection/Delivery materialization | No extra audit noise/action API |

Promotion start immutable capture, automatic PREPARE revision+moderation+bound item tuple single record with exact refs; human decision and activation later separate commits/facts. Item own committed outcome can Block/Cancel/Supersede independently; terminal results never relabeled all Activated. Activity does not change heads, cancellation/withdrawal semantics or source last-admin guard.

PreservedFactComparer compares registered normalized Actor/Initiation/Action/Target/Outcome/BusinessResult/Metadata/source-time/context/correlation/operation/source owner; excludes transport envelope/delivery retry/audit row ID/RecordedAt. Store original normalized fact/profile without losing source datetime precision. Comparison never rebuilds timestamp from relational timestamptz projection: two source facts differing below PostgreSQL microsecond retain exact bigint source-time order and conflict correctly if bound to the same EventId. Exact representation/canonical comparison codec Q20, no unselected hash algorithm/digest or all-envelope equality.

## 5. Api

### 5.1. Endpoints

Chosen read routes below; exact DTO member casing/cursor/error wire engineering [Q15](../questions/Q15-api-and-source-code.md#f18-принятый-activity-contract). General UI /admin/activity does not impose Administrator-only general history.

| Method | Route / destination | Request | Response | Access |
| --- | --- | --- | --- | --- |
| GET | /api/v1/activity | Selected filters/PageSize/Cursor | StaffActivityPageRead | audit.activity.read, staff rows additional audit.staff-access.read |
| GET | /api/v1/activity/{activityId} | Exact Guid; route constraint preserves filter-options literal | StaffActivityRead | Same fixed authorized action classification |
| GET | /api/v1/activity/filter-options | Current reader | StaffActivityFilterOptionsRead | Authorized profiles/visible historic actors only |
| Public owner read | Exact target/F17 iteration/reason API | Typed refs | Authoritative view or denied/gone/unavailable | Independent owner authorization/resource guards |

No public Create/Update/Delete/Undo; no arbitrary actor/time post or event-store replay. Source command returns own confirmed success without waiting for Audit row/UI EventualLag; no audit ID forced into every mutation DTO. Manual refresh first page may show delayed append; no realtime/local optimistic row required.

### 5.2. ETag / If-Match

Immutable records have no own Version/ExpectedVersion/If-Match. Audit cursor is continuation key not credentials/concurrency/effective-order proof. Target owner ETag/resource checks independent, no history timestamp/reference as mutation permit. User-dependent visibility freshly checked every cursor/details/options request; no shared representation validator/cached permissions bypass.

### 5.3. OpenAPI и ошибки

Document chosen routes/Guid literal disambiguation, UTC interval/default50/max200, exact source UTC100ns SQL order/cursor/HasMore/NextCursor without mandatory total. Fresh F16401/403/503 policy, unknown/hidden exact-row privacy and invalid-cursor machine codes Q15. Target gone/denied/error distinct from missing audit record; unavailable reason never empty. Exact current generated ActivityClient not assumed: actual API/OpenAPI tests before NSwag/frontend integration.

## 6. EntityFramework

### 6.1. DbContext ownership

AuditDbContext owns immutable records/inbox/conflict quarantine technical persistence. Separate technical Audit schema/EF mappings/migrations; no new business BC or shared source+Audit SQL transaction. SourceActivityIntent maps in each source own outbox/UoW. No Identity/Publishing/content/media FK/navigation/import of actor roots.

### 6.2. Configurations

| Entity / value | Mapping | Key/index | Concurrency | Responsibility |
| --- | --- | --- | --- | --- |
| SourceActivityIntent | Own source durable intent/outbox profile | Stable EventId | Existing owner/outbox coordination | Payload immutable, delivery state infrastructure outside fact |
| StaffActivityRecord | Own audit records | Guid PK/unique SourceEventId | Immutable; no Version | Preserved normalized source fact/profile + typed safe columns/metadata/captions + RecordedAt |
| SourceFact / metadata | Full-fidelity preserved versioned fact encoding + typed safe variant storage | Registered schema/profile | Immutable | Original timestamp precision retained separate from SQL projection, codec Q20 |
| SortOccurredAtUtcTicks / Id | bigint normalized source UTC100ns ticks and UUID | Descending keyset | Read-only | Exact SQL source-time order/range/cursor; no third clock or timestamptz rounding |
| Audit inbox | Own technical receiver state | Stable source event identity | Own transaction profile | Marker atomic with accepted record; no source inbox enlistment |
| Conflict quarantine | Technical rejected conflicting source identity evidence | Original SourceEventId/profile | No record rewrite | Operational recovery/private safe evidence profile Q20 |

RecordedAt is own append time; SortOccurredAtUtcTicks is only private exact source-time index/cursor value, not another observed timestamp. Staff/reference captions not enriched/resaved; no raw generic Domain entity JSON dump. Exact physical table/index names/canonical codec/inbox schema Q20, not reopened accepted behavior.

### 6.3. Constraints / indexes

Unique SourceEventId protects concurrent append independently of inbox precheck. ActorKind+nullable StaffId consistency, immutable event profile/action/target/typed result. Indexed exact source SortOccurredAtUtcTicks bigint+Id UUID, visibility/action and actor/target/time/correlation/operation access patterns; concrete composite index layout Q20. No correlation uniqueness/Identity existence FK/user delete cascade. No MVP automatic purge/TTL/export migration.

## 7. EntityFramework.Persistence

### 7.1. Repositories / query adapters

Append store/original-source-ID lookup/preserved fact comparer+inbox share Audit own transaction; no normal Update/Delete API/repository. Query handlers List/Get/FilterOptions in data layer; compose visibility first across query/page/any optional count/options, then filter/keyset/limit. Read carriers immutable nontracking values, no internal Version/provider entities.

Concurrent unique race rolls back failed own append then reads durable winner in fresh audit scope, compares full preserved fact/profile before ACK. Unsupported/conflicting fact quarantined no overwrite. Source/auth/current target contexts never cross-enrich record; target details separate public owner reads.

### 7.2. Transactions

Invalidating source writers use existing [MVP consistency guard](../questions/Q20-persistence-and-migration.md#mvp-протокол-consistency-guard): own ReadCommitted transaction, standalone first pg_advisory_xact_lock on SAME owner connection, then fresh own roots/rowlocks and independent primary public foreign facts without ambient enlistment. Normalized safe SourceActivityIntent+stable EventId/outbox commit atomically with independent business result before gate releases. AuditDbContext is never another source participant; existing multi-DbContext participant must remain owner-partitioned.

After source commit release gate before dispatcher/broker ACK/Audit append/inquiry/UI waiting. Audit append is technical append-only, not invalidating business writer: own record+inbox atomic/unique source ID, **no global source gate** merely to write history. Read cursor/history also no source write permit. At-least-once retry preserves EventId/source fact; no after-commit-only memory callback.

Source technical guard/port/connection failure aborts no committed fact; lost source ACK unknown inquiry chosen original operation identity, no blind rerun/new EventId. Downstream failure cannot retroactively fail committed source result. Audit crash before commit no row+completed marker; after commit before ACK duplicate original same receipt. Conflict quarantine technical durable recovery never overwrites old record.

F15 patch repair and Resolve two own commits/new gates/two independent successful-result facts: repair-v21 retained if source v22 arrives; stale Resolve rejected no successful apply audit. Publishing automatic PREPARE exact revision+moderation+bound item tuple one own commit/intent; human approval and actual activation separate. Promotion item committed Block/Cancel/Supersede recorded as own typed result, never resumes/activates by audit. IdentityAccess provider/users/membership/Version/setup/session atomic guarded source commit and last-ready-admin checks remain F16; failed login/rehash/security-counter noise excluded.

### 7.3. Migrations

Create own audit records/full-fidelity source evidence/profile/keyset indexes/inbox/quarantine; each source intent participant belongs own schema/transaction. Preserve no foreign FK/cascade and all MVP records. Physical migration/serialization/transport registration [Q20](../questions/Q20-persistence-and-migration.md#f18-принятый-capture-append-и-cursor-read); no application migration executed during docs task.

## 8. External adapters

### 8.1. Durable transport / Host

[S22](../standards/S22-eventbus-topology.md): source dispatcher sends immutable versioned StaffActivityEvent after source commit at-least-once with stable EventId. Queue/envelope/type registration/retry/quarantine operational profiles Q20; source event semantic fact independent of retry envelope. Broker or selected durable same-process transport must preserve source atomic capture/recovery/unique append, not memory callback.

Host registers fixed ActionProfile registry/typed source result builders/source-own intent participants, dispatcher/consumer Append handler/audit append store/inbox, query readers/cursor codec/clock and F16 permissions audit.activity.read(common Specialist+Administrator), audit.staff-access.read(Administrator only). No role/direct grant editor, Demo fake actor, Audit bootstrap password dependency, realtime notifications/purge/export.

### 8.2. Target/reason references / current reader

Staff actor capture from trusted F16 ICurrentUser; System/Integration/Operator from admitted source execution, optional persisted prior staff initiation. Audit never resolves current Identity entity or VRS user as fake staff.

Current reader F16 PermissionSnapshot freshly checked once/request, reused for registry-classified visibility/options/keyset. Target/F17 reason navigation uses independent owning public contracts and current access. Safe record surviving deleted/denied target no grant/fallback/current caption rewrite. Cursor is opaque viewer/filter-bound SQL continuation, not bearer/auth or globally frozen history snapshot; exact codec/version/size/signing literals Q15.

## 9. Data flows

Source owner command → actual committed result + own durable intent → after-commit dispatcher → Audit Append+inbox → read-only /admin/activity. F16 current reader snapshot → fixed visibility → semantic filters → SQL keyset → safe page/options; target/reason navigation independent.

| Поток | Транспорт | Метод / канал | Endpoint / destination | Доступ | Вход | Application action | Выход | Успех | Ошибки |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| DF01. Source capture/dispatch | Own source commit then durable dispatch | Registered independent business result | Each source owner intent/outbox | Trusted actor/executor + source guards | Actual changed result/refs/context | CaptureCommittedIntent / dispatcher | Versioned stable event | Source state+intent atomic; source success before audit | Source rollback/unknown ACK; backlog preserves captured ID |
| DF02. Audit consume | Internal durable transport | Versioned event | Own Audit handler/store/inbox | Trusted admitted source | StaffActivityEvent | AppendStaffActivityCommand | Original/new immutable record | Record+inbox atomic then ACK | Unsupported/unsafe fact; duplicate race; conflict DF07 |
| DF03. List history | Protected HTTP | GET | /api/v1/activity | audit.activity.read + registry staff visibility | Filters/cursor/PageSize | ListStaffActivityQuery | StaffActivityPageRead | SQL keyset50 default/max200/HasMore |401/403/503/invalid cursor/filter |
| DF04. Exact record | Protected HTTP | GET | /api/v1/activity/{activityId} | Same fixed visible profile | Guid | GetStaffActivityQuery | StaffActivityRead | Immutable visible original row | Hidden/missing exact profile Q15, technical outage |
| DF05. Target / reason | UI + own public read | Exact navigation | Source target/F17 iteration/reason | Independent owner permission/resource rules | Captured exact refs | Owner query/read | Actual current or historical owner view | Audit does not grant or rewrite | Gone/denied/unavailable != empty reason |
| DF06. Filter options | Protected HTTP | GET | /api/v1/activity/filter-options | Same F16 fresh snapshot/visibility | Current reader | GetStaffActivityFilterOptionsQuery | Visible historic authors/authorized action profiles | No staff-directory/hidden admin rows leak |401/403/503, distinct error vs empty |
| DF07. Conflicting duplicate | Internal technical recovery | Same ID normalized equality | Audit quarantine/original lookup | Trusted receiver/operator recovery | Original profile+incoming safe fact | PreservedFactComparer | Original unchanged + quarantined conflict | No overwrite/double append/false ACK of changed truth | Operational codec/source repair profile Q20 |

### DF01. Source capture/dispatch

1. Owner validates actual command/source conditions through its own Application/Domain/provider path. Registered action scopes §4.4; no GET/retry/logincounter/projection capture. Genuine no-op/confirmed ClientOperationId replay no new intent.
2. After actual source result is known but before own commit, builder captures trusted Staff/System/Integration/Operator executor; async optional persisted staff initiation, exact target/context and safe typed result/metadata/as-of captions. One whole Save or atomic PREPARE tuple one fact; deliberate new artifact even same content new fact.
3. Stable EventId generated once; source state/history+SourceActivityIntent/outbox committed atomically in source owner. Invalidating source commit uses its own first MVP gate; no Audit participant. Source rollback no successful activity.
4. Source HTTP confirms own durable result without waiting row/transport. Dispatcher reads committed intent and sends same EventId/fact at-least-once outside source gate; crash/failed send retains durable backlog, no after-commit memory gap.
5. F15 repair/Resolve separate captures/new gates; source22 after repaired21 preserves first fact/rejects stale second. Publishing operation immutable capture/prepare/item outcomes and IdentityAccess last-admin/provider guards unchanged; audit never adds fake resume or lifecycle action.

### DF02. Audit consume

1. Receiver admits source/contract profile and validates normalized actor/action/target/outcome/typed metadata against fixed registry. Outcome Committed with registered BusinessResult; unsupported/unsafe rejected technical record, not fabricated success.
2. Normalize while preserving original source-fact precision; retain versioned evidence independent of relational timestamp projections/envelope retry fields. Begin own Audit transaction, query SourceEventId/inbox.
3. Equal preserved original fact returns original append outcome. Different normalized fact routes DF07, no overwrite. New record owns fresh Id/RecordedAt, copied preserved source fact/safe index projections; record+completed inbox marker atomic.
4. ACK after committed original/new outcome. Crash before commit retries; after commit before ACK same source ID returns same record. Unique race rollback/fresh scope winner compare; no duplicate row/timestamp rewrite.
5. Audit append does not take global source gate or mutate source/foreign roots. Broker/DB outage affects backlog, never retroactively fails source committed command.

### DF03. List history

1. F16 authenticates current reader/ONE fresh snapshot. Require audit.activity.read; fixed registry staff-access class requires audit.staff-access.read. Ignore producer audience guesses/client capability.
2. Bind registered actor kind/staff/action/target+ID/UTC [From,To)/correlation/operation filters, no default time cutoff. Default PageSize50, max200; normalize semantic filters, validate cursor reader+filter binding.
3. Own SQL visibility predicate before filters/page/any optional count; apply descending source SortOccurredAtUtcTicks bigint+Id UUID keyset, converting UTC [From,To) bounds to the same100ns ticks. Cursor uses exact stored LastUtcTicks/UUID ordering, not host string sort/timestamptz projection.
4. Query at most requested+one visible row, map safe immutable items, HasMore/NextCursor; no mandatory total count. Fresh auth every page; changing permissions may remove rows, no cursor-granted old visibility.
5. Late append can enter earlier ordering positions; continuation not frozen snapshot/global history completion. User manual refresh resets first page; no optimistic UI row or source-success wait.

### DF04. Exact record

1. Exact Guid route constraint distinguishes literal filter-options; F16 current read permission and fixed ActionProfile visibility applied in query before projection.
2. Stored record not rewritten by actor/target rename/deactivate/delete; return immutable safe original references/captions/action/result/time. No Identity/source live joins, Version/inbox/envelope/private equality evidence.
3. Missing/hidden privacy profile Q15; target gone != row absent. Audit permission does not authorize source reason/content read; exact reference DF05.

### DF05. Target / reason

1. Source TargetType/TargetId/ExactVersionId/context drives known owner navigation, never arbitrary URL/Name/hash/latest guessing. Mutable draft identity navigates current object; audit is not historical content snapshot.
2. Owner reads apply current independent authorization/resource guards. F17 full Comment/Reject reason remains exact moderation/revision/comment owner record; audit stores only reference.
3. Missing/denied/unavailable explanation reported distinctly, never «reason absent». Deleted/denied target retains audit fact, no cascade/history rewrite. Own F11/F17 authoritative histories/Sequence/ETag remain authority.
4. Navigation GET does not create an activity or grant rollback/replay/approval/runtime rights.

### DF06. Filter options

1. Same current request snapshot+fixed authorized profiles as list/detail, before options lookup. Specialist sees general profiles; staff-access administrative profiles only with extra permission.
2. Query visible retained history for executor kind/StaffId/as-of safe captions; renamed/deactivated historic author remains available without admin employee-directory API.
3. Actions/target kinds derive authorized fixed registry, not untrusted event label. No hidden admin row/action/author leakage through count/options.
4. Empty confirmed options vs query outage distinct; exact multiple-caption aggregation/DTO wire Q15 does not replace current visibility policy.

### DF07. Conflicting duplicate

1. Same SourceEventId load original preserved normalized fact/profile from own Audit scope. Compare semantic source fact, not envelope/retry/RecordedAt/Id or lossy timestamptz row reserialization.
2. Same fact original receipt; changed actor/action/target/result/metadata/source facts quarantined with safe technical correlation, original row remains immutable. No automatic new ID/overwrite/forged success.
3. Technical quarantine/recovery persistence and transport disposition Q20; audit-safe diagnostics exclude full secret/content payload. Operator source repair/re-delivery cannot silently alter old accepted fact.
4. Concurrent duplicate winner and replay preserve original record/time; versioned normalizer supports explicit profiles, unknown incompatible profile technical recovery rather than empty compatible fact.

## 10. Ошибки и диагностика

| Ситуация | Принятый результат |
| --- | --- |
| Source rollback / failed command | No committed activity; typed committed workflow Block/partial is different real result |
| Genuine no-op / confirmed command replay | No new fact; deliberate new artifact same content distinct fact |
| Source intent capture/storage failure before commit | Source own transaction abort, no falsely committed source result |
| Audit/broker outage after source commit | Source success preserved; durable same-ID backlog/retry |
| Duplicate same ID/same normalized source fact | Original one record/outcome/time, no rewrite |
| Same ID/different normalized fact | Conflict quarantine, original unchanged; no lossy timestamp equality |
| Unsafe/unregistered producer/action/metadata/actor | Reject technical fact profile, no sensitive payload log |
| Missing audit.activity.read / hidden staff-access rows | Fresh F16 denial/privacy Q15; no page/options/count leak |
| Invalid viewer/filter-bound cursor/UTC interval/limit | Typed request/cursor error Q15; token not authentication |
| Confirmed empty vs technical unavailable | Distinct success-empty/error; no global feed completeness claim |
| Missing/denied/unavailable target or F17 reason | Original refs/record retained, unavailable≠empty |
| Actor/target rename/deactivate/delete | As-of caption/identity unchanged, no FK cascade |
| Expired session/role change between cursor calls | Fresh request F16 authorization, no cached permissions token |

Safe record/event/source/correlation/operation/action/stage/retry categories only; no full content/reason/provider secret/hash/cookie/code/ETag/storageURL. Operational source backlog/audit lag/duplicate-conflict/quarantine observable; numeric retry/threshold/transport disposition Q20 engineering. No new realtime/export/purge/user notification.

## 11. Тестирование

[S04](../standards/S04-testing.md)/[S14](../../../catalog/docs/standards/dotnet/testing.md#test-environment-ownership-and-bounded-parallelism): source capture and audit append separate durable boundaries. Real owning PostgreSQL/Testcontainers and deterministic barriers for constraints/races; real broker when delivery/ACK guarantees claimed; F16 actual current auth for Functional visibility. No mocks/InMemory proof SQL atomicity.

| ID / поток | Уровень / SUT | Собственные данные / воздействие | Ожидаемый результат |
| --- | --- | --- | --- |
| G01 / DF01,F15 | PostgreSQL separate owner scopes | Repair21 commit+intent; source22 then old Resolve/new gate | First repair/history retained; rejected old Resolve no success fact/shared Audit commit |
| G02 / DF01–02 | PostgreSQL/transport fault barriers | Source gate timeout/connection loss/ACK unknown, audit/broker blocked | Source abort/unknown correctly classified; committed intent survives, gate released before audit wait |
| T01 / DF01 | Unit builder | Trusted actor4 kinds/initiator/targets/safe variants | No fake Staff/operator impersonation/raw data/secrets |
| T02 / DF01 | Real source UoW/outbox | Crash/failure before source commit/after commit before dispatch | State+intent both or neither; no memory-gap loss |
| T03 / DF01–02 | Real source/transport/Audit | Downstream outage then recovery/repeated delivery | Source success unaffected, stable EventId eventual single record |
| T04 / DF02 | PostgreSQL parallel consumers | Same EventId concurrent append/unique race | One immutable record/inbox, original receipt |
| T05 / DF02 | PostgreSQL fault barriers | Audit precommit crash/postcommit preACK loss | Row+marker atomic, original time retained |
| T06 / DF07 | Unit/PostgreSQL | Changed actor/result/metadata/time same ID; unsafe/unknown profile | Quarantine/no overwrite/secret logs; duplicate compares original normalized source fact |
| T07 / DF03–06 | API/PostgreSQL | Actor/action/target/UTC/correlation/operation filters, Spec/Admin data | Server visibility before paging/count/options, interval [From,To) |
| T08 / history | PostgreSQL/Functional | Rename/deactivate/delete actor/target | Captured history/caption persists, no Identity FK/live enrichment |
| T09 / DF05 | Functional real F17 | Exact reason/revision read denied/gone/unavailable | Correct protected owner explanation or explicit failure, never summary/full reason/empty guess |
| T10 / authority | Owner integration | F11 activation/policy and F17 iteration history | Audit does not authorize/replace subject history/Delivery effective time |
| T11 / UI/API | Browser necessary source→Audit contour | Reload/manual refresh/late append during keyset pagination | Persisted read only; no optimistic row/frozen global snapshot/source wait |
| T12 / source coverage | Source handler integration matrix | Whole Save, intentional duplicate new artifact, genuine no-op/ClientOperationId replay | One committed-result granularity; new artifact new fact/no replay-noop fact |
| T13 / layers/stages | Architecture/Functional | Actor root references/own UoWs/API/Demo | No foreign Identity/domain root/FK/source+Audit shared txn; public append/update/delete absent |
| T14 / DF03 | Real PostgreSQL keyset | Equal timestamp/different UUID, limit50/200, cursor filters/viewer mismatch | Canonical DB descending order/HasMore/NextCursor no total; bound cursor rejects mismatch |
| T15 / DF02,07 | Precision regression PostgreSQL | Original source times differ below microsecond and UUID tie-break order would reverse them; envelope/retry differs only | Exact bigint UTC100ns order/range/cursor retained; real source-fact difference conflicts, envelope-only repeat equals |
| T16 / DF03–06 | Actual F16 API | Role/access change between cursor requests, hidden admin author/action/options | Fresh snapshot each request; no old cursor grants/admin leaks/directory dependency |
| T17 / Publishing | PostgreSQL real source commits | Promotion start/atomic prepare tuple/human approve/activate/itemBlocked/Cancel/Supersede | Exact refs; one prepare fact, separate human/activation/item outcomes, no fake allActivated/auto-resume |
| T18 / F16 | Provider/own PostgreSQL intent | Account/roles/setup issue+complete/bootstrap/emergency vs loginfails/counters | Admin-only fixed profiles; provider guarded atomic state intact, security noise excluded |
| T19 / retention | Persistence/schema tests | Source/user deletion and long-lived rows | All MVP retained/no purge/cascade; no export/legal-forever claim |
| T20 / routes | Functional OpenAPI | Activity/filter-options/exact Guid routes, forged producer label/public write | No route ambiguity, fixed visibility, read routes selected/no public Create/Update/Delete/Undo |

Fixtures isolate source roots/outbox and Audit record/inbox/quarantine/database namespaces; clean only fixture records. Unit clocks/transport doubles do not prove real SQL/broker/browser behavior. Browser E2E required for manual refresh/source success while delayed audit/owner reason navigation; API-only not UI proof. Multiple BC within same API not automatically E2E.

Docs-only checks do not run application/provider/broker/database/browser or migration. Exact wire/codec/transport profiles Q15/Q20 remain engineering; accepted routes/granularity/visibility/defaults/retention not reopened by tests.

## 12. Порядок реализации

1. Materialize approved fixed action scope/profiles/typed safe metadata/actor+initiator contracts, routes and visibility; finalize exact wire/canonical comparison/cursor profiles Q15/Q20.
2. Source-own atomic intent builders from actual independent result and stable ID; preserve existing source MVP gate/provider/Publishing tuple rules. No Audit cross-BC transaction.
3. Immutable Audit EF/inbox/unique full-fidelity source evidence+SQL indexes; Append/conflicting duplicate quarantine/ACK recovery with real PostgreSQL.
4. Dispatcher/transport registration/Host fixed permissions+ActionProfiles and F16 fresh snapshot readers/cursor handlers; no cache/foreign enrichment.
5. Read-only OpenAPI/NSwag/frontend /admin/activity manual refresh/filter/detail/independent exact reason navigation; no local optimistic row/source-success wait.
6. Barrier source commit/delivery/precision/UUID/page/access/route tests and real necessary browser contour. No realtime/export/purge/notifications hidden scope.

## 13. Открытые технические вопросы

| Вопрос | Оставшаяся engineering граница |
| --- | --- |
| [Q10](../../../business/questions/Q10-permissions-and-isolation.md#f18-history-scope-и-visibility) | Approved scope/granularity/visibility/retain-all MVP fixed; future archive/export/anonymization separate explicit policy |
| [Q15](../questions/Q15-api-and-source-code.md#f18-принятый-activity-contract) | Exact DTO member wire/polymorphic metadata/error privacy/cursor codec version/size/signing literals/canonical target conversion/options multiple-caption aggregation |
| [Q20](../questions/Q20-persistence-and-migration.md#f18-принятый-capture-append-и-cursor-read) | Physical migrations/source intent+provider participant registration/broker envelope/transport retries/quarantine/preserved source precision codec |
| [F16](F16-staff-access.md) | Current ICurrentUser/PermissionSnapshot mechanism selected; actual provider/middleware integration and permission registry validation |
| [F17](F17-content-review.md) / [F11](F11-publication-lifecycle.md) | Owner-authoritative exact reference/reason/navigation/subject histories; activity not availability/approval basis |

Immutable committed journal, capture/append boundaries, selected read routes/cursor50–200/visibility/actor semantics/no-op granularity/retain-all MVP are accepted mechanisms, not TBD. No chronological decision append or new approval flow.
