# F12. ContentEntitlementPolicy и entitlement decision: бэкенд

[Бизнес](../../../business/features/F12-content-entitlement-policy.md) · [Publishing M04](../modules/M04-publishing.md) · [Delivery M05](../modules/M05-delivery.md) · [Фронтенд](../../frontend/features/F12-content-entitlement-policy.md) · [Q01](../../../business/questions/Q01-license-rights.md) · [Q02](../../../business/questions/Q02-policy-changes.md) · [Q03](../../../business/questions/Q03-renewal-and-multiple-licenses.md) · [Q04](../../../business/questions/Q04-withdrawn-publications.md) · [Q15](../questions/Q15-api-and-source-code.md#f12-policy-contracts-и-diagnostics) · [Q20](../questions/Q20-persistence-and-migration.md#f12-policy-binding-и-effective-history) · [Q17](../questions/Q17-publication-availability.md#f12-freeze-cutoff-и-effective-time).

## 1. Область реализации

F12 — MVP: versioned local policy и immutable PolicyAgreement в Publishing, source confirmation через точный agreement и immutable Delivery binding/whole-revision decision по confirmed **current** period/effective history. Policy values: FreezeAtExpiration, DenyAfterExpiration; system default FreezeAtExpiration. Precedence Variant → Product → system. ContinueWithLatest, policy Archive и moderation policy не входят в цель. Demo не требует policy/Identity/licenses/VR School/broker.

Обещание policy фиксируется commit immutable Publishing PolicyAgreement **до** VR School confirmation периода. Agreement Freeze at t1 → current Deny at t2 → source confirms exact plan at t3 сохраняет Freeze. Source commit/processing/first use/period date не выбирают policy. Renewal создаёт новый planned period+agreement current policy; existing agreement/binding не переписываются later parent/policy change. Frozen access нельзя отменить просто сменой Freeze на Deny. GrantPeriod binding принадлежит Delivery, source license/grant/renewal facts — CatalogAccess public projection. Credential validation/session/content routes принадлежат F13, raw activation key не policy input и не persisted secret. CM не разрешает скачивание/запуск приложения вместо launcher/Project Storage.

Freeze выбирает последнее actual Delivery history состояние со **stored VisibilityConfirmedAt < UpdatesUntil**, включая null. Это conservative observation checkpoint после primary re-read уже committed полного transition, не actual commit time: commit-before-cutoff/confirmation-after-cutoff намеренно исключён. Necessary pending/missing temporal evidence →Indeterminate, не skip older known row. Late Unpublish и source Retire не отменяют frozen revision; exact Withdrawal запрещает её без fallback. Unlimited должен быть явным подтверждённым фактом, неизвестная дата не unlimited. Multiple licenses принимаются только из доверенно подтверждённого набора; каждая оценивается отдельно, выбирается одна целая наиболее актуальная разрешённая revision, без item merge.

Физические Publishing.Domain/Application.Abstractions/Application/Api/EntityFramework и целевой EntityFramework.Persistence; Delivery.Domain/Application/EntityFramework и необходимые целевые Abstractions/Persistence размещения по S20. Сейчас [ContentPolicyVersion](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Domain/Publications/PublicationHistory.cs) хранит append versions, [SavePolicyAsync](../../../../contentmanager.api/src/App/ContentManager.Application/Publishing/PublishingService.cs) использует общий workspace Revision. [Policy resolver](../../../../contentmanager.api/src/App/ContentManager.Application/Publishing/EntitlementPolicyResolver.cs) выбирает version as-of timestamp; [CatalogSnapshotProcessor](../../../../contentmanager.api/src/App/ContentManager.Application/Integration/CatalogSnapshotProcessor.cs) записывает policy в CatalogAccess LicenseGrantPeriod при source processing. [InternalDeliveryService](../../../../contentmanager.api/src/App/ContentManager.Application/Delivery/InternalDeliveryService.cs) читает foreign Publishing activation history/current pointer. Это не target Delivery binding/effective history/ETag. Типы ниже проектные; source/target расхождения Q15/Q20/Q23.

Own/parent catalog Archive и Open conflict не отменяют confirmed entitlement и доступ к существующим materialized revisions, включая new session; independent policy/Withdrawal/revoke guards сохраняются [F06](F06-product-catalog.md)/[F15](F15-catalog-conflicts.md). Новая policy operation блокируется только при реально несовместимом affected scope/basis; scoped Variant patch repair не меняет policy. Restore/Resolve не выдаёт grant и не изменяет bound policy существующего периода автоматически.

## 2. Domain

### 2.1. Aggregates, entities и value objects

| Тип | Роль / значения enum | Владелец / граница |
| --- | --- | --- |
| ContentEntitlementPolicy | Enum: FreezeAtExpiration, DenyAfterExpiration | Shared semantic contract, не legacy arbitrary string |
| PolicyScopeType / PolicyScope | Value | Stable Catalog IDs; Variant ProductId не изменяемый policy owner |
| ProductEntitlementPolicyState | Aggregate root | Current local Product setting, отдельно F07/F11 states |
| ProductVariantEntitlementPolicyState | Aggregate root | Current local override, отдельно F08/F11 |
| ContentEntitlementPolicyVersion | Immutable history entity/root | Publishing history; null Policy = clear local value, не третья policy |
| PolicySource | Enum: Variant, Product, System | Где найдена применимая non-null policy |
| EffectivePolicyResolution | Read-only value | Product/Variant exact version или system default без фиктивной version |
| PolicyAgreement | Immutable Publishing root | Exact planned license/Variant/source period + caller request + captured applied parent/policy; nontransferable promise checkpoint |
| EntitlementPolicyBinding | Immutable Delivery root | Unique LicenseId+VariantId+SourcePeriodId; copies exact confirmed AgreementId/terms |
| EntitlementPeriodFacts | Read-only public values | Authoritative CatalogAccess/source contract, no foreign tracked entity; exact wire profile Q23 |
| DeliveryTemporalDecisionValues | Own immutable Delivery Domain temporal input | Defined once [F13](F13-content-delivery.md#21-aggregates-entities-и-value-objects); mapped from public immutable read values |
| EntitlementEvaluationInput | Domain parameter | No raw key/request DTO/current policy fallback |
| ContentEntitlementDecision | Domain result | Whole revision; reason profile не автоматически HTTP status |

**PolicyScopeType / PolicyScope**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Тип scope | `PolicyScopeType` | Да | Product либо ProductVariant | Stable Catalog owner |
| ScopeId | `Guid` | Да | Stable owner identity | Non-empty; Variant ProductId не изменяемый policy owner |

**ProductEntitlementPolicyState**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | Уточняется | Да | Stable Product policy owner | Separate current local setting |
| CurrentPolicyVersionId | `Guid?` | Нет | Current local version | Initial empty/pointer/latest-clear representation Q20 |
| LastSequence | `long` | Да | Scoped history order | Не Version; initial/overflow Q20 |
| Version | `int` | Да | Own policy concurrency | Internal mutable token; не DTO/F07/F08/F11 state |

**ProductVariantEntitlementPolicyState**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductVariantId | Уточняется | Да | Stable Variant policy owner | Separate local override; no copied parent value |
| CurrentPolicyVersionId | `Guid?` | Нет | Current local version | Initial empty/pointer/latest-clear representation Q20 |
| LastSequence | `long` | Да | Scoped history order | Не Version; initial/overflow Q20 |
| Version | `int` | Да | Own policy concurrency | Internal mutable token; не DTO/F07/F08/F11 state |

**ContentEntitlementPolicyVersion**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | `Guid` | Да | Immutable version identity | Server-generated; no Update/Archive/Delete |
| Scope | `PolicyScope` | Да | Stable owner | Same scope current pointer |
| Sequence | `long` | Да | Scoped history order | Positive; unique scope+Sequence |
| Policy | `ContentEntitlementPolicy?` | Нет | Local value or clear marker | null = clear, не third policy |
| EffectiveFrom | `DateTimeOffset` | Да | Policy-history/current state metadata | Trusted; never reconstruct period promise as-of this date |
| CreatedAt | `DateTimeOffset` | Да | Creation time | Trusted server clock |
| CreatedBy | `Guid?` | Нет | Creation actor | Trusted context |

**EffectivePolicyResolution**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Policy | `ContentEntitlementPolicy` | Да | Resolved non-null policy | Variant → Product → system FreezeAtExpiration |
| Source | `PolicySource` | Да | Resolution origin | Variant/Product/System |
| PolicyVersionId | `Guid?` | Условно | Exact selected version | System не получает фиктивную version |
| Applicability provenance | Уточняется | Уточняется | Owner/context applicability evidence | Q20 |

**PolicyAgreement**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | `Guid` | Да | CM-generated AgreementId | Immutable Publishing identity; not entitlement/grant |
| RequestId | `Guid` | Да | Durable original VRS planned request | Non-empty; retry same caller/input gives original agreement |
| SourceCaller | Уточняется [Q15](../questions/Q15-api-and-source-code.md#f12-policyagreement-integration-boundary) | Да | Trusted authenticated VRS backend identity | Not client ActorId/raw activation key; auth adapter profile remains Q15 |
| LicenseId | `long` | Да | Exact planned license scope | CM license projection not required to quote; no grant issued |
| ProductVariantId | `Guid` | Да | Exact Variant scope | Nontransferable with license/period; authoritative applied relation |
| SourcePeriodId | `Guid` | Да | Exact durable planned period reservation | New changed plan/renewal requires new ID |
| OriginalPlannedParameters | Уточняется [Q23](../questions/Q23-source-synchronization.md#f12-planned-agreement-confirmed-workflow) | Да | Original caller plan, retained unchanged | Canonical meaningful caller input; no fresh derived policy in replay comparison |
| OriginalExpectedSourceContext | Уточняется [Q15](../questions/Q15-api-and-source-code.md#f12-policyagreement-integration-boundary) | Да | Caller expected Variant/parent context | New quote checks exact current applied match; wire carrier Q15 |
| AppliedProductId | `Guid` | Да | Authoritative parent captured under guard | Immutable provenance; pending/ambiguous relation cannot be guessed |
| Policy | `ContentEntitlementPolicy` | Да | Frozen agreed non-null enum | Current Variant→Product→system at CM agreement commit |
| PolicySource | `PolicySource` | Да | Frozen resolution provenance | Variant/Product/System |
| PolicyVersionId | `Guid?` | Условно | Exact chosen local Publishing policy version | System Freeze has null, no fake version |
| CreatedAt | `DateTimeOffset` | Да | Trusted CM agreement record time | Promise checkpoint is successful own commit, not date-as-of resolver |
| OriginalNormalizedRequest | Уточняется [Q15](../questions/Q15-api-and-source-code.md#f12-policyagreement-integration-boundary) | Да | Durable replay comparison of original caller input | Caller+RequestId+license/Variant/period/plan/expected context; excludes later current policy/parent |

No Agreement ExpiresAt/age cleanup in MVP. Unused exact-plan agreement remains durable but gives no entitlement; changed plan before confirmation requires **new SourcePeriodId and new agreement**, not transferring this promise.

**EntitlementPolicyBinding**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| GrantPeriodId | `Guid` | Да | Own immutable Delivery binding identity | Trusted server factory generates once on new binding; race/replay returns stored winner; not mutable commercial period |
| LicenseId | `long` | Да | Exact confirmed license | Unique tuple with Variant+SourcePeriodId |
| ProductVariantId | `Guid` | Да | Exact confirmed Variant | Agreement scope must match |
| SourcePeriodId | `Guid` | Да | Original confirmed source period | May be license-wide shared across Variants; agreement per Variant |
| PolicyAgreementId | `Guid` | Да | Exact source-confirmed Publishing agreement | Immutable external ID, no foreign FK |
| Policy | `ContentEntitlementPolicy` | Да | Frozen agreement policy copy | Never re-resolve current/as-of policy |
| PolicySource | `PolicySource` | Да | Agreement provenance | Immutable |
| PolicyVersionId | `Guid?` | Условно | Exact agreement version provenance | System null; no fictive version |
| BoundAt | `DateTimeOffset` | Да | Own Delivery binding time | Not policy promise checkpoint |

Binding does not store permanent FrozenRevisionId/cutoff/current period facts. Same confirmed period genuine correction changes versioned source facts and new evaluation cutoff; agreement policy/history stays immutable.

**EntitlementPeriodFacts**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| LicenseId | `long` | Да | Verified commercial identity | Not organization expansion |
| ProductVariantId | `Guid` | Да | Confirmed grant scope | Per-license Variant scope |
| SourcePeriodId | `Guid` | Да | Exact source confirmed period | License-wide sharing allowed; tuple includes Variant |
| CurrentPeriodId | `Guid` | Да | Explicit source current period for license+Variant | Exact source period identity; no old Freeze fallback after new period current |
| PolicyAgreementId | `Guid` | Да | Source-confirmed agreement pointer | Immutable for confirmed period; substitution rejected/quarantined |
| OriginalConfirmedPlan | Уточняется [Q23](../questions/Q23-source-synchronization.md#f12-planned-agreement-confirmed-workflow) | Да | Original plan confirmed with agreement | Retained independently of current correction values |
| StartsAt | `DateTimeOffset` | Да | Current confirmed applicability start | Versioned source facts, not policy as-of checkpoint |
| UpdatesUntil | `DateTimeOffset?` | Условно | Current finite updates cutoff | Required finite evidence; missing not unlimited; not application ExpiresAt |
| IsUnlimitedLicense | `bool` | Да | Explicit source updates-unlimited fact | Not IsUnlimitedUsers |
| Closure | Уточняется [Q23](../questions/Q23-source-synchronization.md) | Да | Current completion/closure evidence | Expiration/completion not revoke; exact source semantics Q23 |
| GrantRevoke | Уточняется [Q23](../questions/Q23-source-synchronization.md) | Да | Current Variant grant restriction | Does not revoke other Variant grants |
| LicenseRevoke | Уточняется [Q23](../questions/Q23-source-synchronization.md) | Да | Current whole license restriction | Scope all relevant license grants |
| SourceVersion | Уточняется [Q23](../questions/Q23-source-synchronization.md) | Да | Authoritative current fact ordering | Versioned correction/revoke/current-period evidence; carrier Q23 |
| ChangeClassification | Уточняется [Q23](../questions/Q23-source-synchronization.md#f12-planned-agreement-confirmed-workflow) | Да | Explicit source Renewal/Correction/other fact kind | Same-period correction requires reason/history; purchased time/unlimited upgrade is renewal |
| CorrectionReason | `string?` | Условно | Authoritative correction explanation/history reference | Required genuine correction; privacy/codec Q23 |
| Completeness | Уточняется [Q23](../questions/Q23-source-synchronization.md) | Да | Complete authoritative grant/period evidence | Complete no grant Denied; missing projection/agreement/period Indeterminate |

Delivery history/time/completeness models определены только в [F13](F13-content-delivery.md#21-aggregates-entities-и-value-objects): actual committed history separate from processed Publishing sequence; pending/first saved primary confirmation, known null/complete empty/missing range/order validity are explicit. F12 Application consumes immutable nontracking Abstractions read values and maps to owned DeliveryTemporalDecisionValues for pure Domain evaluation. It never passes Domain/EF entities across read port or creates its own EffectiveAt; Domain has no Abstractions dependency.

**EntitlementEvaluationInput**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Requested Variant | Уточняется | Да | Trusted scope | No raw key/request DTO |
| License-period facts | `EntitlementPeriodFacts` | Да | Confirmed commercial evidence | No current policy fallback |
| Binding | `EntitlementPolicyBinding` | Да | Captured period policy | Immutable |
| Evaluation time | Уточняется | Да | Trusted now | Pure evaluator receives values |
| TemporalDecisionValues | `DeliveryTemporalDecisionValues` | Да | Own Domain flat temporal/current values mapped by Application from F13 read carrier | No Domain→Abstractions/EF dependency; necessary purpose/cutoff prefix completeness, actual null and stored time |
| Revision restrictions | `IReadOnlyList<RevisionDistributionRestriction>` | Да | Known authoritative exact bans, model F13 | Necessary restriction read unknown→Indeterminate before mapping, not valid empty; no fallback |

**ContentEntitlementDecision**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Semantic outcome | Уточняется | Да | Allowed/Denied/Indeterminate | Machine reason/status profile Q15 |
| RevisionId | Уточняется (nullable) | Условно | Selected whole revision | No item merge/fallback; only allowed candidate |
| Selected license | Уточняется | Уточняется | Commercial basis | Trusted confirmed set |
| Selected period | Уточняется | Уточняется | Period basis | Captured binding |
| Binding | Уточняется | Уточняется | Policy applicability basis | Immutable period basis |
| Policy source | Уточняется | Уточняется | Resolution provenance | Q15/Q20 |
| Policy version | Уточняется | Уточняется | Exact applicable version | System no fabricated version |
| Unlimited basis | Уточняется | Уточняется | Explicit unlimited evidence | Not unknown date/users flag |
| Cutoff basis | Уточняется | Уточняется | Finite expiration evidence | Strict boundary; Q17 |
| Machine reason | Уточняется | Уточняется | Explainable decision | Q15; не automatically HTTP status |

History version Policy=null сохраняет факт clear. Product clear resolves system; Variant clear resolves current applied Product, then system. State pointer/latest-clear record representation и initial empty state profile Q20; нельзя терять audit clear или хранить скрытую копию inherited value. Binding хранит выбранную policy, поэтому её последующая current resolution не изменяет promise. PolicyVersionId — external Publishing ID без cross-BC FK; system binding не получает invented policy root.

LicenseId long подтверждён current integration, final normalized period contract Q23. Target GrantPeriodId — own immutable Delivery binding identity from trusted server factory, not Catalog private row ID and not mutable commercial period. Catalog public confirmation contract carries only License+Variant+SourcePeriod tuple; legacy source row Id stays private implementation evidence. SourcePeriodId может относиться ко всей лицензии: binding unique key = (LicenseId, ProductVariantId, SourcePeriodId); agreement captured per Variant. Own immutable Delivery GrantPeriodId сохраняется отдельно от Catalog private row ID. Current [LicenseGrantPeriod](../../../../contentmanager.api/src/BoundedContexts/CatalogAccess/ContentManager.CatalogAccess.Domain/Licenses/LicenseGrantPeriod.cs) имеет own Guid Id + LicenseId/VariantId/SourcePeriodId и mutable cutoff/closure; это source projection, не target Delivery policy binding.

**PinnedRevisionEvaluationInput (owned immutable Domain value)**

Owned Delivery Domain value; Application maps fresh public immutable read carriers into these values. Domain never references Application.Abstractions DTO/EF roots. Session security epoch/verifier/absolute expiry belong F13 and are checked before policy evaluation.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| LicenseId | long | Да | Selected verified license | No alternate-license fallback |
| VariantId | Guid | Да | Original pinned Variant | Exact binding scope |
| SourcePeriodId | Guid | Да | Original admitted period | Must equal authoritative current period |
| GrantPeriodId | Guid | Да | Original own immutable binding identity | Agreement retained; no mutable commercial aggregate |
| RevisionId | Guid | Да | Original pinned whole revision | Never changed by evaluator |
| Facts | EntitlementEvaluationInput | Да | Fresh selected current-period facts/binding/closure/temporal values | Missing necessary evidence remains unknown |
| EvaluatedAt | DateTimeOffset | Да | Trusted checkpoint time | Current cutoff equality enforced, no application ExpiresAt substitute |

Pure EvaluatePinnedRevision returns SessionPinnedDecision defined once [F13](F13-content-delivery.md#21-aggregates-entities-и-value-objects), carrying outcome/original pin/next finite policy boundary/reason. It does not mutate session, repin, issue credentials or perform I/O.

### 2.2. Методы и переходы

| Тип | Метод / функция | Параметры | Предусловия | Изменение / результат |
| --- | --- | --- | --- | --- |
| Product/Variant policy state | SetPolicy | Valid policy, new version ID, trusted time/actor | Own state expected Version, allowed scope/context | New immutable version; monotonic Sequence; current local policy transition |
| Product/Variant policy state | ClearPolicy | New clear-version ID, trusted time/actor | Same guards; no third enum value | Null local setting/version audit; inheritance/default, не erase history |
| Policy version | Create | Exact scope/Sequence/nullable policy/audit | Defined enum, positive order, consistent owner | Immutable version; no Update/Archive/Delete |
| PolicyResolutionService | ResolveCurrent | Captured applied parent + local current versions | Coherent scope/version evidence | Variant non-null → Product non-null → system Freeze |
| PolicyAgreement | Capture | Original exact planned request + captured current policy/applied parent | Authenticated VRS backend; first guard; original replay lookup before new quote checks | Immutable promise record; no entitlement |
| PolicyAgreement reader | ReadExact | AgreementId + exact confirmed original scope/plan | Exact source-confirmed pointer, no transferred coupon | Frozen original terms; no current/as-of resolver |
| EntitlementPolicyBinding | CreateFromAgreement | Confirmed tuple+exact agreement terms; identity from trusted server factory | Agreement ID/scope/original plan match; complete confirmed evidence | Immutable own insert; renewal new tuple/agreement |
| EntitlementDecisionService | Evaluate | EntitlementEvaluationInput | Facts classified; pure function with trusted values | Current, frozen whole revision либо explained Denied/Indeterminate |
| EntitlementDecisionService | EvaluatePinnedRevision | PinnedRevisionEvaluationInput | Fresh selected basis, F13 expiry/security checks | Allowed/Denied/NeedsNewAdmission/DataNotReady; original pin unchanged |
| ConfirmedLicenseDecisionSelector | SelectWholeRevision | Independent allowed/denied decisions same Variant | All participating licenses confirmed | One allowed whole candidate + basis; ordering/tie Q17/Q20 |

Own state Version изменяется при policy action согласно выбранному repeat/no-op profile Q15/Q20; Sequence для history серверный, не Version. Client не задаёт version ID/Sequence/audit/time/PolicySource. Null mutation не отменяет старые bindings и не снимает Variant publication. Existing binding всегда имеет приоритет над новым policy resolution для того же подтверждённого периода.

### 2.3. Инварианты и constraints

1. Current precedence Variant → authoritative applied Product → system Freeze. Current UI resolution is not period promise; immutable Publishing PolicyAgreement commit before source confirmation freezes policy/parent/provenance for exact license+Variant+planned SourcePeriodId/input.
2. Agreement at t1 Freeze/current policy t2 Deny/source exact plan confirms t3 → Freeze. Later parent/policy change does not rewrite agreement/binding. Request replay compares original normalized caller input after auth and before fresh Catalog/new quote checks; never compare stored agreement to newly derived policy.
3. MVP no automatic expiry/age cleanup. Changed unconfirmed plan needs new durable planned period ID + new agreement; unused agreement has no entitlement. VRS own conditional confirmation fixes exact plan/AgreementId+outbox; no shared transaction.
4. Confirmed PolicyAgreementId immutable. Contradictory newer source snapshot/transfer scope rejected or quarantined preserving old binding/history. Missing agreement/confirmed period/projection = Indeterminate/DataNotReady, not InvalidKey/current default/as-of guess.
5. Binding key exact LicenseId+VariantId+SourcePeriodId; own immutable Delivery binding GrantPeriodId allocated once, not a public source row ID. Source period may be shared across Variants, agreement may not. Copy exact AgreementId/frozen enum/source/version; System Freeze no fake version/FK.
6. Renewal always new period+new agreement using current quote; explicit source CurrentPeriodId per license+Variant controls new selection/admission. Once new period is current, no expired-new-Deny→old-Freeze fallback. Retain old binding/history; do not repin already-open sessions automatically.
7. Genuine correction requires explicit authoritative classification/reason/history for same confirmed period, preserves agreement policy and updates versioned current StartsAt/UpdatesUntil/unlimited/closure/revoke facts. Purchased more time/unlimited upgrade = renewal, not correction grandfathering.
8. Updates expiration/completion is not revoke. Freeze remains if remaining rights/restrictions permit. Grant revoke Variant scoped, license revoke license scoped, credential revoke credential scoped. UpdatesUntil != application ExpiresAt; missing date != explicit IsUnlimitedLicense; IsUnlimitedUsers not updates proof.
9. Finite now < UpdatesUntil current; equality expired. Expired Freeze requires coherent complete F13 history with necessary evidence confirmed and temporal order valid; select strict stored VisibilityConfirmedAt < current corrected cutoff, including null, by actual effective order. Commit-before/confirmation-after cutoff deliberately excluded; equality excluded/no second rounding/no fallback/backdate/permanent frozen pointer. Expired Deny refuses; exact ban overrides permission.
10. Complete authoritative no grant = Denied; missing projection/agreement/current period/history = Indeterminate, confirmed revoke = separate Denied reason. Source/query outage not complete empty evidence.
11. Multi-license evaluates each verified requested basis independently. Any necessary unknown yields whole Indeterminate; never skip unknown to claim global maximum. Known Denied others + Allowed may choose most current whole Allowed by authoritative effective order, no item merge/implicit organization license expansion.
12. Existing pinned-session behavior uses separate EvaluatePinnedRevision and F13 accepted Q16 absolute TTL/epoch/current-period/cutoff/response leases. New-selection rules never auto-repin original session.

[S21](../../../catalog/docs/standards/architecture/ddd.md#aggregate-owned-rule-definitions): own policy/history/agreement/binding constraints enforce exact nontransferable scope, immutable source-confirmed agreement, unique tuple, enum/provenance and caller replay. Physical indexes/codecs/auth scheme/retry numbers/current source mapping and observation storage/codec remain engineering Q15/Q20/Q23; conservative time semantics approved Q17.

### 2.4. Domain events

| Fact / contract | Когда фиксируется | Потребитель |
| --- | --- | --- |
| EntitlementPolicyChanged | Committed local policy action requiring new history | Diagnostics/read cache invalidation; no existing binding rewrite |
| Delivery effective state history / terminal restriction facts F11/F10 | Consumer подтверждает full projection либо exact withdrawal ban | F12 evaluation through Delivery-owned reader |
| Trusted license/grant period changes F06/F23 | Source projection applied | Period eligibility/binding orchestration, не policy CRUD |

**EntitlementPolicyChanged**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ScopeType | Уточняется | Да | Policy owner kind | Committed local action |
| ScopeId | Уточняется | Да | Stable owner | No binding rewrite |
| PolicyVersionId | Уточняется | Да | Exact committed history version | Durable outbox |
| Sequence | Уточняется | Да | Scoped order | Contract Q20 |
| ChangedAt | Уточняется | Да | Trusted action time | Contract Q20 |
| Event envelope | Уточняется | Уточняется | Trusted event envelope identity/version | Q20 |

**Delivery effective state history / terminal restriction facts F11/F10**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Exact Variant | Уточняется | Да | Effective/restriction scope | Owner F11/F10; reader F12 |
| Exact revision | Уточняется | Уточняется | Target/restriction identity | Profile Q17; null transition distinguished |
| Order | Уточняется | Уточняется | Applied/ban ordering | Profile Q17 |
| Effective evidence | Уточняется | Уточняется | Full-projection visibility | Profile Q17 |

**Trusted license/grant period changes F06/F23**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Commercial period facts | Уточняется | Уточняется | Confirmed period | Owner source integration Q23 |
| Revoke facts | Уточняется | Уточняется | Confirmed commercial invalidation | License/grant scope Q23 |
| Unlimited facts | Уточняется | Уточняется | Explicit confirmed unlimited | Q23; no source protocol duplication |

F12 не вводит broker event каждого entitlement read либо generic event ради logging. PolicyChanged не retroactive “update all grant bindings” command. Producer outbox durable после local commit; contract/topology/profile Q20, source integration F06/Q23 не дублируется.

## 3. Application.Abstractions

### 3.1. Commands

| Command | Result | Concurrency |
| --- | --- | --- |
| SetProductContentPolicyCommand | Local policy committed state result + token carrier | Required own policy state If-Match |
| SetVariantContentPolicyCommand | Same, Variant local state | Own Variant policy state, not parent policy token |
| CreatePolicyAgreementCommand | Immutable PolicyAgreementRead | Authenticated VRS RequestId/input replay; first MVP gate before new quote facts |
| EnsureEntitlementPolicyBindingCommand | Existing/new EntitlementPolicyBindingRead | Exact confirmed License+Variant+SourcePeriod+Agreement pointer; own unique insert |
| EvaluateContentEntitlementUseCase | ContentEntitlementDecisionRead | Read/evaluation plus ensure binding if needed; F13 admission guards |

**SetProductContentPolicyCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | `Guid` | Да | Applied owner | Own policy scope |
| Policy | `ContentEntitlementPolicy?` | Нет | Local setting or explicit clear | Two values only; null valid |
| ExpectedStateVersion | `int` | Да | Own policy precondition | If-Match header, not body |

**SetVariantContentPolicyCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | Уточняется | Да | Applied parent | Not parent policy token |
| VariantId | Уточняется | Да | Stable own scope | F15/ownership guards |
| Policy | `ContentEntitlementPolicy?` | Нет | Local override or clear | Two values only; null valid |
| ExpectedStateVersion | `int` | Да | Own Variant policy precondition | Header, не body |

**CreatePolicyAgreementCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Request | `CreatePolicyAgreementRequest` | Да | Exact original planned request | Model §3.3; trusted caller supplied separately by auth context |

**EnsureEntitlementPolicyBindingCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| LicenseId | `long` | Да | Exact confirmed license | Internal confirmed input, not a grant request |
| VariantId | `Guid` | Да | Confirmed Variant scope | Unique tuple |
| SourcePeriodId | `Guid` | Да | Exact confirmed source period | Unique tuple; never latest/as-of lookup |
| PolicyAgreementId | `Guid` | Да | Immutable source-confirmed pointer | Exact scoped original plan match; public Publishing read |
| ConfirmedFacts | `EntitlementPeriodFacts` | Да | Versioned current source and original confirmation evidence | Model §2.1; missing agreement/period Indeterminate |

**EvaluateContentEntitlementUseCase**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Confirmed license set | Уточняется | Да | Trusted participating rights | No organization license expansion |
| Requested Variant | Уточняется | Да | Evaluation scope | Whole revision decision |
| Evaluation context | Уточняется | Да | Trusted context | No raw key; F13 admission guards |

Publishing command handlers Publishing.Application; binding/evaluation orchestration Delivery.Application; own immutable binding GrantPeriodId only from trusted server factory, never Catalog/caller input. Trusted actor/time/correlation приходят execution context, не arbitrary body. Evaluate pure Domain portion не command handler с hidden foreign mutation; optional binding insert — отдельный явный stage.

### 3.2. Queries

| Query | Result |
| --- | --- |
| GetProductPolicyQuery | Own local state DTO + state ETag carrier |
| GetVariantPolicyQuery | Own local state DTO + own state ETag carrier |
| ListPolicyHistoryQuery | Immutable policy version collection/page |
| ResolveCurrentPolicyQuery | EffectivePolicyResolutionDto |
| GetPolicyAgreementQuery | Exact immutable agreement terms/provenance for validated scope |
| GetEntitlementPolicyBindingQuery | Immutable binding read |
| ReadDeliveryEffectiveHistoryQuery | F13 CompleteDeliveryHistoryRead via ReadCompleteDeliveryHistoryQuery |

**GetProductPolicyQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | Уточняется | Да | Own scope | Local state/token |

**GetVariantPolicyQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductId | Уточняется | Да | Applied parent | Coherent context |
| VariantId | Уточняется | Да | Own Variant scope | Local state/token |

**ListPolicyHistoryQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ScopeType | Уточняется | Да | Policy owner kind | Immutable scoped history |
| ScopeId | Уточняется | Да | Stable owner identity | Not raw slash-separated choice |
| Paging | Уточняется | Уточняется | Pagination | Q15 |
| Order | Уточняется | Уточняется | Scoped Sequence ordering | Q15 |

**ResolveCurrentPolicyQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductVariantId | Уточняется | Да | Applied Variant scope | Current display, not old period policy |
| Captured parent context | Уточняется | Уточняется | Applied parent evidence | Q20; no latest-parent fallback |

**GetPolicyAgreementQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| AgreementId | `Guid` | Да | Exact source-confirmed agreement | Publishing-owned immutable public read; no current resolution |
| LicenseId | `long` | Да | Expected confirmation scope | Must match agreement |
| ProductVariantId | `Guid` | Да | Expected Variant scope | Must match agreement |
| SourcePeriodId | `Guid` | Да | Expected source period | Must match agreement, no coupon transfer |

**GetEntitlementPolicyBindingQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| LicenseId | `long` | Да | Exact binding license scope | Confirmed verified identity |
| ProductVariantId | `Guid` | Да | Exact binding Variant scope | No cross-Variant agreement transfer |
| SourcePeriodId | `Guid` | Да | Exact confirmed source period | Unique tuple, not globally unique period-only key |

ReadDeliveryEffectiveHistoryQuery reuses the exact [F13 ReadCompleteDeliveryHistoryQuery](F13-content-delivery.md#32-queries) parameter/read contract: ProductVariantId Guid, optional Cutoff DateTimeOffset and required server decision-purpose/range scope, immutable CompleteDeliveryHistoryRead values (no internal Version/entities). Application maps to owned Domain temporal values; no direct Domain→Abstractions reference. No separate F12 temporal DTO/time fields; own Delivery read/projection handler remains EntityFramework.Persistence.

[S20](../../../catalog/docs/standards/architecture/cqrs.md#handler-responsibilities-and-dependency-direction): all queries в владельце EntityFramework.Persistence, не Application command implementation. Current resolution UI отличается от policy для existing binding. Entitlement diagnostics read/public endpoint ещё не выбран Q15; method existence не proof REST route.

### 3.3. DTO и parameter models

| Модель | Назначение |
| --- | --- |
| SetContentPolicyRequest | Поля в таблице модели ниже; применимость и wire profile сохраняют указанные Q-вопросы |
| ContentEntitlementPolicyStateDto | Поля в таблице модели ниже; применимость и wire profile сохраняют указанные Q-вопросы |
| EffectivePolicyResolutionDto | Поля в таблице модели ниже; применимость и wire profile сохраняют указанные Q-вопросы |
| ContentEntitlementPolicyVersionDto | Поля в таблице модели ниже; применимость и wire profile сохраняют указанные Q-вопросы |
| EntitlementPolicyBindingRead | Поля в таблице модели ниже; применимость и wire profile сохраняют указанные Q-вопросы |
| ContentEntitlementDecisionRead | Поля в таблице модели ниже; применимость и wire profile сохраняют указанные Q-вопросы |
| CreatePolicyAgreementRequest / PolicyAgreementRead | Integration request/read defined below; no staff/Unity credential fields |

**SetContentPolicyRequest**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Policy | `ContentEntitlementPolicy?` | Нет | Explicit null clears local setting | No Version/ExpectedRevision/server IDs; omission encoding Q15 |

**ContentEntitlementPolicyStateDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ScopeType | Уточняется | Уточняется | Owner kind | Wire profile Q15 |
| ScopeId | Уточняется | Уточняется | Stable owner | Wire profile Q15 |
| ConfiguredPolicy | Уточняется (nullable) | Нет | Own local setting | Not copied effective policy |
| CurrentPolicyVersionId | Уточняется (nullable) | Нет | Current history pointer | Initial/pointer-clear profile Q20 |
| LastSequence | Уточняется | Уточняется | Scoped history order | No internal Version in DTO |
| Local history summary | Уточняется | Уточняется | Read summary | Q15 |

**EffectivePolicyResolutionDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Policy | Уточняется | Уточняется | Resolved policy | Precedence Variant/Product/System; wire profile Q15 |
| Source | Уточняется | Уточняется | Resolution origin | Inherited Product/system distinguished |
| PolicyVersionId | Уточняется (nullable) | Условно | Applicable version | System no fictive version |
| Applied parent evidence | Уточняется | Уточняется | Context provenance | Q15/Q20 |
| Read evidence | Уточняется | Уточняется | Coherent resolution proof | Q15/Q20 |

**ContentEntitlementPolicyVersionDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | Уточняется | Уточняется | Immutable version identity | Wire/audit profile Q15 |
| ScopeType | Уточняется | Уточняется | Owner kind | Exact stable scope |
| ScopeId | Уточняется | Уточняется | Owner identity | Exact stable scope |
| Sequence | Уточняется | Уточняется | History order | Server sequence |
| Policy | Уточняется (nullable) | Нет | Local policy/clear marker | Null clear retained |
| EffectiveFrom | `DateTimeOffset` | Да | Trusted policy-history metadata | Never determines period promise as-of date |
| CreatedAt | Уточняется | Уточняется | Creation time | Trusted audit |
| CreatedBy | Уточняется (nullable) | Нет | Creation actor | Visibility Q15 |

**EntitlementPolicyBindingRead**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| GrantPeriodId | `Guid` | Да | Own immutable Delivery binding ID | Server-generated once; stored winner reused on race/replay; no new mutable commercial-period owner |
| LicenseId | `long` | Да | Confirmed license | Unique tuple |
| ProductVariantId | `Guid` | Да | Exact Variant scope | Unique tuple |
| SourcePeriodId | `Guid` | Да | Original confirmed source period | May be shared across Variants |
| PolicyAgreementId | `Guid` | Да | Exact source-confirmed Publishing agreement | Immutable external ID; no FK/current resolver |
| Policy | `ContentEntitlementPolicy` | Да | Frozen agreement enum | No replacement on correction/first use |
| PolicySource | `PolicySource` | Да | Frozen agreement provenance | Variant/Product/System |
| PolicyVersionId | `Guid?` | Условно | Frozen Publishing policy version | System null; no fake policy version |
| BoundAt | `DateTimeOffset` | Да | Own Delivery binding commit record time | Not policy promise checkpoint |

**ContentEntitlementDecisionRead**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Semantic status | Уточняется | Уточняется | Allowed/Denied/Indeterminate | Exact representation Q15 |
| Reason | Уточняется | Уточняется | Explainable decision | Machine strings/public disclosure Q15 |
| RevisionId | Уточняется (nullable) | Условно | Selected whole revision | No item merge/fallback |
| License basis | Уточняется | Уточняется | Trusted chosen license | Confirmed set only |
| Period basis | Уточняется | Уточняется | Chosen period | Immutable binding |
| Policy source | Уточняется | Уточняется | Applicable origin | Profile Q15/Q17 |
| Policy version | Уточняется | Уточняется | Captured version | System no invented root |
| Cutoff basis | Уточняется | Уточняется | Finite boundary evidence | Strict effective-before cutoff |
| Unlimited basis | Уточняется | Уточняется | Explicit confirmed unlimited | Not unknown date/users flag |
| Completeness provenance | Уточняется | Уточняется | Sufficient history/facts | Q15/Q17 |
| Projection provenance | Уточняется | Уточняется | Actual Delivery effective evidence | Q15/Q17 |

**CreatePolicyAgreementRequest**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| RequestId | `Guid` | Да | Durable VRS planned request identity | Non-empty; exact same original input on recovery |
| LicenseId | `long` | Да | Exact planned license | Existing CM projection not required; not grant issuance |
| ProductVariantId | `Guid` | Да | Planned content scope | Exact authoritative applied relation required for new quote |
| SourcePeriodId | `Guid` | Да | Durable planned source period | Changed plan/renewal needs new ID |
| OriginalPlannedParameters | Уточняется [Q23](../questions/Q23-source-synchronization.md#f12-planned-agreement-confirmed-workflow) | Да | Original exact planned parameters | Canonical caller input; no secret/raw key |
| ExpectedSourceContext | Уточняется [Q15](../questions/Q15-api-and-source-code.md#f12-policyagreement-integration-boundary) | Да | Caller expected Variant/applied parent context | New quote mismatch/pending/ambiguous F15 → NotReady/conflict; carrier Q15 |

**PolicyAgreementRead**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| AgreementId | `Guid` | Да | CM agreement identity | Source fixes exactly this ID at conditional confirmation |
| RequestId | `Guid` | Да | Original VRS request | Confirmed replay identity |
| LicenseId | `long` | Да | Exact license scope | Immutable/nontransferable |
| ProductVariantId | `Guid` | Да | Exact Variant scope | Immutable |
| SourcePeriodId | `Guid` | Да | Exact planned source period | Immutable |
| OriginalPlannedParameters | Уточняется [Q23](../questions/Q23-source-synchronization.md#f12-planned-agreement-confirmed-workflow) | Да | Original captured request plan | Source confirms exact original plan; not corrected current values |
| AppliedProductId | `Guid` | Да | Captured authoritative parent | Historical provenance, no later overwrite |
| Policy | `ContentEntitlementPolicy` | Да | Frozen enum | No current/as-of substitution |
| PolicySource | `PolicySource` | Да | Frozen source | System allowed |
| PolicyVersionId | `Guid?` | Условно | Frozen selected Publishing policy version | System Freeze null; no fake ID |
| CreatedAt | `DateTimeOffset` | Да | Trusted record time | Successful agreement commit is promise checkpoint |

Request/read are target integration models, not existing DTO evidence. Exact JSON/URI/auth scheme/response status/normalized codec остаются Q15; domain PolicyAgreement fields are defined once §2.1.

No technical Version in DTO. Committed state/read mapper carries own Version separately for Api ETag. Own-config + inherited/effective/history display may be composed, но whole composite strong ETag нельзя вычислять только из Variant own Version при independent parent/context change; response shape/token scope Q15. Public diagnostics authorization/minimal disclosure and exact machine strings Q15; raw activation keys/credentials не model fields.

### 3.4. Ports

| Port | Назначение | Направление |
| --- | --- | --- |
| IProductEntitlementPolicyStateRepository / IVariantEntitlementPolicyStateRepository | Tracked own scope state, conditional save | Publishing persistence |
| IPolicyVersionStore | Insert immutable history, scoped exact projections | Publishing persistence; no generic Update/Delete |
| ICatalogPolicyContextReader | Applied parent/existence/F15 guard | CatalogAccess public contract |
| IPublishingPolicyReader / IPolicyAgreementReader | Current UI resolution separate from exact agreement immutable read | Public Publishing → Delivery, independent primary connection |
| IPolicyAgreementRepository / request replay store | Immutable own agreement + unique trusted caller/RequestId/input lookup | Publishing-only persistence; no foreign contexts |
| IConfirmedEntitlementPeriodReader | License/grant/revoke/explicit unlimited/period/completeness | CatalogAccess public adapter → Delivery |
| IEntitlementPolicyBindingRepository | Existing read, unique create-if-absent | Delivery persistence |
| ICompleteDeliveryHistoryReader / IRevisionDistributionRestrictionReader | Exact F13 CompleteDeliveryHistoryRead + authoritative owner public ban | Own coherent Delivery reads + independent primary restriction port; unknown not Allowed |
| IPolicyOutbox | Durable policy fact when required | Publishing local commit |
| TimeProvider / ITrustedExecutionContext | Server clock/actor/correlation/confirmed credential identity | Host/F13 infrastructure |
| MVP consistency guard / owner UoW boundary | Один global transaction exclusive gate по Q20, owner-only atomic commit и independent primary public reads | Shared infrastructure adapter; owner connection, не foreign transaction |

CatalogFacts не изменяются Publishing/Delivery evaluator. Ownership/period/availability writers сериализует MVP gate §7.2; agreement/caller replay and exact confirmation protocol approved; wire/storage mapping remains Q15/Q20/Q23. Новые project names не доказанные existing interfaces; F13 credential/session public contracts reuse, no direct VR School key call from policy handler.

### 3.5. Specifications / read contracts

[S19](../../../catalog/docs/standards/dotnet/ardalis-specification.md): ProductPolicyStateByIdSpecification, VariantPolicyStateByIdSpecification, PolicyHistoryByScopeSpecification, BindingByPeriodScopeSpecification, PolicyAgreementByIdSpecification, PolicyAgreementByCallerRequestSpecification, CurrentPeriodByLicenseVariantSpecification, EffectiveStateBeforeCutoffSpecification. Atomic predicates ForPolicyScopeCondition, ForLicenseVariantPeriodCondition, EffectiveBeforeBoundaryCondition; builder extensions compose внутри concrete Ardalis specification, не invented spec.And API.

History policy ordered server Sequence; Delivery history selection использует saved VisibilityConfirmedAt+actual effective order, не revision CreatedAt, GUID или Publishing Sequence alone. Strict stored confirmation cutoff retains null rows; necessary prefix pending/completeness/order failure stays Indeterminate, not filtered away to older known rows. Irrelevant later catch-up/pending cannot blanket-veto proven prior Freeze prefix if it cannot backdate; current committed otherwise-allowed state remains usable. New agreement captures coherent current applied parent/local/Product policy under gate; existing agreement exact read never re-resolves by period/date/first use. History pagination/tie/clock profile Q15/Q17; nontracking projections и completeness не сводятся к presence одного latest row.

## 4. Application

### 4.1. Handlers / use cases

F12 entitlement/runtime decision uses F10/F13 **final effective instance/schema/media closure** и exact revision restrictions, не весь retained Product/Variant source/evidence graph. Suppressed unavailable A не блокирует valid effective B; shared dependency, которая остаётся в B closure, учитывается. Broader retained closure поддерживает audit/evidence/history и не удаляется. Publishing Activated != Delivery effective/readiness; ни policy evaluator, ни Restore/Resolve не resurrect Cancelled/Superseded automatic items F11.

Mutation handlers соблюдают порядок §7.2: shape/auth preparation → own ReadCommitted transaction → отдельный global gate statement → fresh tracked own roots + independent primary public facts → own token/basis/domain checks → own state/intents commit. Предварительные reads/CPU results не являются authority после ожидания gate.

Publishing.Application: SetProductContentPolicyHandler и SetVariantContentPolicyHandler load own state/applied context, check required expected Version/enum/F15 operation guard, create immutable version и domain transition, commit state/history/outbox/audit, map own committed result/token. Policy action не touches F07/F08 assignments/F11 publication state и не требует F17. Null clear поддерживается для Product и Variant; repeated same-policy/clear action semantics Q15/Q20.

Publishing.Application CreatePolicyAgreementHandler receives authenticated VRS backend context, opens own transaction/first MVP gate and **looks up original caller+RequestId/input first**. Same original input replays original agreement despite fresh policy/parent changes; changed bound input conflict, no new quote. New request reads authoritative Catalog applied Variant/parent/F15 via independent primary public port, validates expected context, then resolves own current Variant→Product→System policy and atomically commits immutable exact agreement/replay facts/required safe intent. No CM license projection/grant prerequisite and no VRS HTTP callback under gate.

Delivery.Application EnsureEntitlementPolicyBindingHandler reads fresh confirmed Catalog period/original plan/immutable AgreementId, then exact public Publishing agreement. Scope+original plan must match. Trusted server factory allocates own binding GrantPeriodId only on new insert; existing/race winner identity reused. Existing immutable binding wins only after exact identity/confirmation checks; contradictory source substitution rejected/quarantined at source owner, old binding/history preserved. New unique (LicenseId,VariantId,SourcePeriodId) binding copies exact agreement policy/provenance; no current/as-of selection at first use. Own Delivery commit remains separate from Publishing/Catalog owners.

EvaluateContentEntitlementUseCase receives verified requested license set, checks source completeness and explicit **current period** per license+Variant, ensures exact current agreement binding and evaluates versioned current cutoff/unlimited/closure/revoke/effective history/final closure. Complete no grant Denied; missing projection/agreement/period Indeterminate. Renewal new current period excludes old binding as fallback. Correction recalculates evaluation cutoff from current facts without changing policy or storing perpetual frozen pointer.

Multi-license orchestrator evaluates every necessary requested verified basis. Any unknown → overall Indeterminate; otherwise choose most current Allowed whole revision, even if other known bases Denied. Do not infer other licenses from organization/one credential or merge items. Existing pinned sessions use separate pure EvaluatePinnedRevision below and accepted [Q16](../questions/Q16-activation-and-sessions.md#принятый-протокол-сессий-и-response-leases); new-admission selector never repins a session.

Query handlers remain owner Persistence. Current policy UI query grants no agreement/binding/commit permit; history preserves exact old scope/clear markers. Detailed agreement/source/binding pipeline DF11–DF13; existing DF07–DF09 cover binding/decision.

### 4.2. Validators

[S18](../../../catalog/docs/standards/dotnet/fluentvalidation.md): ProductIdValidator, VariantIdValidator, PolicyScopeValidator, OptionalContentPolicyValidator, ExpectedStateVersionValidator, ConfirmedPeriodIdentityValidator. SetProductContentPolicyValidator/SetVariantContentPolicyValidator compose semantic rules: null разрешён; только два enum values. AgreementRequestValidator composes non-empty RequestId/SourcePeriodId/VariantId, LicenseId and original plan/expected-context shape validators; BindingParametersValidator composes exact tuple/AgreementId/original confirmation constraints, без arbitrary max dates или guessed source unlimited flag.

FV — shape/primitive constraints. Scope permissions/applied parent/F15, protected mutable source facts/current Version, availability/completeness и unique binding race — Application/Domain/DB. “Async validator resolved current policy” не proof new quote remained valid through commit; authoritative capture happens after guard, confirmed replay lookup comes first. Server trusted clock/actor source не editable validated body.

### 4.3. Mappers

Api maps route/body/null/If-Match → command; expected int из общего strong numeric ETag parser. PolicyVersionMapper copies domain version/history values; PolicyStateMapper creates DTO+separate committed token. ResolutionMapper preserves inherited/default source, no hidden local value. AgreementRequestNormalizer compares meaningful original caller input only; AgreementMapper freezes own current resolution/applied parent; BindingMapper copies exact public agreement ID/enum/source/version and License+Variant+SourcePeriod; trusted server binding factory allocates own GrantPeriodId once, stored winner reused. SourcePeriodAdapter maps explicit unlimited and unknown boundary distinctly; не IsUnlimitedUsers→unlimited-updates. DecisionMapper сохраняет allowed whole revision/license/period/reason, redacts secrets; public diagnostic/status mapping Q15/F13.

### 4.4. Services / algorithms

PolicyResolutionService resolves current own Variant→applied Product→System only for UI/new agreement under guard. PolicyAgreementCaptureService performs authenticated original-request replay lookup before fresh derived quote checks and freezes exact current terms at Publishing commit. AgreementConfirmationValidator compares source original confirmed plan/immutable AgreementId/scope with stored agreement; later current correction values are classified separately.

EntitlementDecisionService receives exact binding/current source facts/clock/effective history/final closure. Branches: sufficient verified current-period evidence → scoped revoke/no-grant classification → exact agreement binding → unlimited or finite-active current Delivery state → expired agreed Deny or Freeze using complete confirmed observation history strictly before current cutoff → exact ban/final closure → whole result. Expiration/completion not revoke; null history no earlier fallback. Current policy or as-of date never substitutes missing agreement.

CurrentPeriodSelector uses authoritative explicit current SourcePeriodId per license+Variant. Renewal replaces eligibility candidate for new selection, keeps old binding/history; expired current Deny cannot fall back old Freeze. Genuine versioned correction/reason/history changes current cutoff and recomputes Freeze result, never changes frozen agreement policy. Pinned runtime uses EvaluatePinnedRevision: current-period renewal needs new admission; genuine correction keeps agreement/pin and reevaluates current facts without extending original absolute TTL.

EvaluatePinnedRevision is a separate pure algorithm for **one selected** admitted license/credential/period/pin, not the multi-license new-admission selector. F13 first checks credential security epoch/deadline, absolute TTL and terminal invalidation. Necessary current facts/history/restriction unknown → DataNotReady; do not manufacture revoke or select another license.

Authoritative current period differs from admitted SourcePeriodId → NeedsNewAdmission (renewal, not license revoke). Same-period genuine correction preserves original Agreement/GrantPeriodId and evaluates current cutoff/unlimited/closure/revoke/terminal ban/final effective closure against original pin. While updates active, original R1 may remain Allowed despite newer R2/Unpublish/Retire/archive; no current pointer substitution. Finite Deny at/after corrected UpdatesUntil → Denied even when stored session expiry is later. Expired Freeze recomputes strict Q17 confirmed whole candidate at corrected cutoff: necessary unknown → DataNotReady; known null/no eligible grant → Denied; eligible candidate different from original pin → NeedsNewAdmission; exact same pin → Allowed. NextFinitePolicyBoundary includes Freeze cutoff as well as Deny.

No mutation/repin/TTL extension occurs in evaluator. Allowed correction continues original session expiry; confirmed disallowed outcome may be terminally invalidated by F13, later expansion/restore never resurrects it. Temporary unknown remains retryable. New admission evaluates fresh complete requested set; restoration/renewal need new session.

ConfirmedLicenseDecisionSelector evaluates complete verified requested set. Any necessary unknown → whole Indeterminate, no maximum claim after skipping unknown. Known Denied + Allowed → select most current **whole Allowed** by authoritative effective order; same exact candidate tie min LicenseId then min CredentialId (F13); exact carrier remains Q15, no hash/ID/created sorting/item merge.

## 5. Api

### 5.1. Endpoints

Наследуемая target карта F12 ниже; final response composition/status/signatures/permissions/history/diagnostics profile [Q15](../questions/Q15-api-and-source-code.md#f12-policy-contracts-и-diagnostics). Current [PublishingController](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Api/PublishingController.cs) exposes workspace GET + `/api/v1/publishing/products/{id}/policy` и `variants/{id}/policy` PUT с body ExpectedRevision; это другой contract.

| Method | Route / target contract | Request | Response | Access |
| --- | --- | --- | --- | --- |
| GET | /api/v1/products/{productId}/content-policy | ProductId | ApiResponse local policy read + own ETag; inherited/default carrier Q15 | Scoped read Q10/Q15 |
| PUT | Same | SetContentPolicyRequest + own policy If-Match | Committed local policy state/token; effective display scope Q15 | Policy edit permission уточняется |
| GET | /api/v1/products/{productId}/variants/{variantId}/content-policy | Applied parent/Variant IDs | Local Variant state+own ETag; inherited/effective separate scope Q15 | Scoped read |
| PUT | Same | Nullable policy + own Variant policy If-Match | Committed local state/token | Scope/F15/edit permission Q15 |
| GET, separate history profile | Product .../content-policy/history либо nested Variant .../content-policy/history | Scoped IDs, chosen paging/order | Policy version collection/page | Exact history routes/signatures/access Q15 |
| Integration command; transport Q15 | CreatePolicyAgreement boundary, exact URI not selected | CreatePolicyAgreementRequest, trusted backend auth | Original/new immutable PolicyAgreementRead | Authenticated VRS backend only; not staff/Unity/raw key |

History target раньше описывался условно “если summary не помещается”; paged history policy/response не определяется произвольным UI размером, final profile Q15. ResolveCurrent/GetPolicyAgreement/Binding/Evaluate — public application contracts, не автоматически REST. New CreatePolicyAgreement is authenticated VRS-backend integration boundary only; final URI/method/envelope/security adapter Q15. F13 owns session/delivery endpoints; new diagnostic endpoint не придумывается.

### 5.2. ETag / If-Match

[S16](../standards/S16-rest.md): exact own policy GET/confirmed PUT returns own int Version strong numeric ETag, DTO не содержит Version/ExpectedRevision. Required If-Match targets тот же local policy state; missing428/stale412/domain409. List/version/history response не source mutable state token; F11 publication ETag и assignment token не policy preconditions.

Variant inherited/effective policy может измениться от Product update/move при неизменном local Variant Version. Same risk Product current DTO with independently changing external context. Whole composite strong ETag cannot be only local Version; choose separate representation/read or explicit scoped metadata Q15. Own-token header не proof freshness of other aggregate. Initial missing state/read-default ETag, concurrent first-create, repeat/no-op/invalid-header priority Q15/Q20. Result mapper uses own committed generation; hidden second GET не требование success.

### 5.3. OpenAPI и ошибки

Explicit nullable clear semantics, two enum values, scope IDs, ETag/If-Match, ApiResponse/read carrier, ValidationProblemDetails/ProblemDetails, status400/401/403/404/409/412/428 по confirmed profile. Actual success code/documented status and machine strings Q15, не automatic PUT204/POST201. Internal entitlement Denied vs Indeterminate domain reasons map to F13 response independently; no blanket “Expired” for missing data or “Revoked” for null publication. NSwag only after accepted backend/OpenAPI tests, per-call headers/metadata, no manual generated patches.

## 6. EntityFramework

### 6.1. DbContext ownership

PublishingDbContext owns policy states/history/immutable PolicyAgreements and own request replay facts/outbox. CatalogAccess owns confirmed source period/original plan/immutable AgreementId/current versioned facts. DeliveryDbContext owns immutable bindings/effective state/history/restrictions. CatalogAccessDbContext owns source license/grant projections; F12 uses public readers, не чужой context внутри query/command. Current policy на CatalogAccess LicenseGrantPeriod — legacy coupling, не reason move binding ownership туда. Existing shared host/UoW не cross-BC transaction license.

### 6.2. Configurations

Target logical tables/layout; physical names/key profile/migration Q20. Current [PublicationRevisionConfiguration](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.EntityFramework/Configurations/PublicationRevisionConfiguration.cs) maps content_policy_versions unique OwnerId+IsVariant+Sequence, string policy MaxLength32. Current [DeliveryDbContext](../../../../contentmanager.api/src/BoundedContexts/Delivery/ContentManager.Delivery.EntityFramework/DeliveryDbContext.cs) lacks separate target binding/effective-history DbSets.

| Entity | Table | Key/index | Concurrency | Mapping |
| --- | --- | --- | --- | --- |
| Product policy state | Publishing.ProductEntitlementPolicyStates | ProductId PK/unique owner | own int Version token | Current version ID nullable; LastSequence bigint |
| Variant policy state | Publishing.ProductVariantEntitlementPolicyStates | VariantId PK/unique owner | own int Version token | Stable scope, no stored copied Product policy |
| Policy version | Publishing.ContentEntitlementPolicyVersions | Id PK; unique ScopeType+ScopeId+Sequence; history index | Immutable insert-only | Nullable policy clear, UTC effective/create/audit values |
| Policy agreement | Publishing-owned PolicyAgreements; physical schema/name Q20 | PK AgreementId; unique trusted caller+RequestId; exact license+Variant+planned period lookup | Immutable insert-only | Original caller input/plan/expected context, captured parent/frozen enum/source/version; no expiry |
| Policy binding | Delivery.EntitlementPolicyBindings | Unique LicenseId+VariantId+SourcePeriodId | Immutable insert-only | Own GrantPeriodId+exact AgreementId/frozen enum/source/version/BoundAt; no permanent frozen revision |
| History/visibility/restrictions | Models/mappings owned F13 | Exact processing/history/evidence/order identity + independent ban | F13 own conditional evidence Version, immutable history/outcomes | Processed sequence separate from actual history/first saved observation; explicit null/pending |

### 6.3. Constraints / indexes

Positive history Sequence, unique scope/order, pointer same scope, defined two policy values or clear marker, binding policy non-null, provenance consistent (System no fabricated version), normalized period/license/Variant identity. Version FK only within Publishing. Binding.PolicyVersionId external ID no cross-BC FK/cascade. Unique normalized binding key protects concurrent first insert across credentials for same right; never raw SourcePeriodId-only. Publishing agreement unique trusted caller+RequestId/original input; scope/nontransferability immutable, no expiry index/cleanup.

History versions/agreements/bindings no normal Update/Delete/Archive, no policy-edit cascade to existing right. Source revoke/cutoff corrections are independently projected commercial facts, не mutable policy binding. Effective history must include null transitions and completeness/order evidence; index-only latest non-null lookup breaks Freeze. Retention F16/Q12 keeps historical distribution dependencies; no fixed retention interval introduced.

## 7. EntityFramework.Persistence

### 7.1. Repositories / query adapters

Host/DI задаёт owner-partitioned UoW participants: в transaction входят только DbContext/own outbox/inbox данного BC. Текущий EfCoreUnitOfWorkParticipant, способный объединять несколько DbContexts в общей transaction, не является target coordination adapter: participant selection разделяется по owner BC, foreign public read adapter создаёт независимый primary scope/connection без ambient enlistment. Guard adapter использует owner connection/transaction; mapping/repository другого BC в command handler не внедряется.

Publishing.Persistence implements EfProductPolicyStateRepository, EfVariantPolicyStateRepository, EfPolicyVersionStore, EfPolicyAgreementRepository/request replay store and own query handlers §3.2; Delivery.Persistence implements EfEntitlementPolicyBindingRepository, binding/effective-history query handlers. Domain/application public projections imported through adapters, not SQL join across contexts. Policy scope projection no repository per DTO/history row.

Policy immutable version insertion/update current state uses MVP gate §7.2 and own conditional Version. ResolveCurrent captures current applied parent/local/Product pointers+versions coherently; Query coherence/carrier profile Q20/Q15; обычный read не write guard. PolicyAgreement reader loads exact immutable terms; no ResolveApplicablePeriod/as-of reconstruction from current or historical policy snapshots. Binding unique conflict fresh scope returns exact tuple/agreement winner after confirmation checks, never replaces old policy. Agreement request lookup compares only original caller input before Catalog/new quote checks.

DI: register one accepted command/query implementation path in Publishing/Delivery modules, public independent primary Catalog/Publishing agreement readers, authenticated VRS integration context, TimeProvider/request normalization, own outbox/decision orchestration F13. Integration auth adapter not staff/Unity session credential path. Current [App module](../../../../contentmanager.api/src/App/ContentManager.Application/ContentManagerApplicationModule.cs) registers shared policy resolver/service; new target не второй concurrent mutation path к same policy/history.

### 7.2. Transactions

For CreatePolicyAgreement authoritative first own read after gate is original caller/RequestId replay lookup. Fresh Catalog/current policy/expected-context checks run only for new quote. Publishing agreement, Catalog normalized confirmed facts and Delivery binding remain three owners/UoWs; GrantPeriodId is own immutable Delivery binding ID, generated once and returned as stored winner; Catalog private row ID not input, no second mutable commercial-period owner/FK.

По [MVP consistency guard](../questions/Q20-persistence-and-migration.md#mvp-протокол-consistency-guard) каждый invalidating writer начинает **свою** BC transaction с явно заданным ReadCommitted и отдельным первым SQL statement получает один стабильный глобальный exclusive pg_advisory_xact_lock. Lock выполняется на том же открытом owner connection, на котором будут authoritative reads, row locks, запись и commit; общий ключ одинаков во всех репликах CM. До получения gate нельзя authoritative-read/check/row-lock затронутый basis. Затем тот же свежий owner scope на удерживающем gate connection загружает собственные tracked roots и проверяет expected Version; чужие current facts читает только через public read ports в независимом primary connection/scope, без ambient UoW enlistment, foreign EF, shared SQL transaction, cache или read replica. Публичный read carrier остаётся предметом Q15; механизм сериализации уже выбран.

Все способные инвалидировать проверяемый basis writers используют тот же gate: F07 whole Save/setup, F08 patch Save/setup, ContentModeling/Media lifecycle/reference changes, CatalogAccess applied relation/F15 Resolve, F09 CreateRelease/Retire/Restore, F10 CreateRevision/lifecycle/Withdrawal, F11 Select/Activate/Rollback/Unpublish и promotion capture/per-item/stage/late Approval, F12 policy changes, F15 scoped repair, релевантные F17 decisions и Delivery-owned invalidation commits. Каждый изменяет только own BC state. Own state/token, required own outbox, safe audit intent и inbox marker фиксируются атомарно; commit/rollback освобождает transaction lock. Это общий write gate в одной PostgreSQL database между репликами, а не общая транзакция BC.

CPU-подготовка, immutable projection/canonical hashing и предварительные проверки могут быть снаружи gate, но после acquisition весь влияющий на commit basis заново подтверждается до применения подготовленного результата. Gate нельзя удерживать во время UI/человеческого Approval, VR School HTTP, S3/bytes, broker send/ACK, ожидания Delivery projection либо HTTP stream; эти действия и downstream commits имеют отдельные границы.

Acquisition wait ограничен выбранной конфигурацией и cancellation; timeout/cancel/lock failure — технический отказ, не domain Conflict. Failed/partial public port не превращается в empty compatible facts. Потеря owner connection прерывает операцию целиком; reconnect не продолжает прежний basis. Own expected Version mismatch остаётся 412; independent relevant basis drift — 409 с повторной проверкой/review, без автоматического retry с новым ETag. Потерянный commit ACK означает unknown outcome: inquiry по durable operation identity/выбранному Q15 recovery, а не утверждение rollback или слепой повтор.

Read queries, pickers, preview/review не получают этот global gate и не выдают commit permit. Own DTO и token читаются согласованно из одного owner поколения; composite basis/freshness других roots учитывается отдельно. Последующая mutation получает новый gate и перепроверяет basis.

Publishing DF02/DF04 policy set/clear выполняются под gate в own Publishing transaction. Delivery DF07 create-if-absent binding и F13 admission получают новый gate в own Delivery transaction; policy/period/parent facts читаются public primary ports без foreign enlistment. Unique normalized binding key сохраняет immutable winner; Agreement own Publishing commit is selected promise checkpoint; source exact-plan confirmation and binding key/current-period/strict complete multi-license policy approved. Wire/mapping/observation read adapters remain Q15/Q20/Q23; conservative checkpoint semantics approved Q17. Pure DF06/DF08/DF09 read/evaluation не получают commit permit.

| Boundary | Local atomic writes / protected reads | Отдельная ответственность |
| --- | --- | --- |
| Set/Clear policy | Own state+Version/Sequence, immutable version, required outbox/audit; scoped context/preconditions | Delivery bindings unchanged; external dispatch after commit |
| Agreement quote | Own immutable agreement + caller/RequestId normalized replay facts/required safe intent | Capture current applied parent/own policy after first gate; no VRS transaction/network under gate |
| Source confirmation/projection | Own VRS conditional exact plan+AgreementId+outbox; later own CatalogAccess current confirmed facts | VRS calls CM outside its transaction; Catalog reads exact Publishing agreement independently; no shared transaction |
| Binding create-if-absent | Own Delivery immutable unique tuple/binding terms | Exact confirmed original plan/AgreementId via primary public reads, no current/as-of policy lookup |
| Pure entitlement evaluation | Coherent current source/completeness/binding/effective history/ban evidence | No policy mutation/history rewrite; F13 session issue needs its guarded admission |
| PolicyChanged consumer | Consumer inbox/cache/read invalidation if used | No binding rewrite or new policy assignment |

Concurrent policy action/move/source-period arrival/renewal/binding commits используют MVP gate §7.2; agreement promise checkpoint and original-plan confirmation selected; source codecs/DDL/recovery mapping remain Q20/Q23. Repeat requests/unknown insert outcome reconcile by normalized key; unique violation не automatically error409 to runtime caller. Source valid read до commit не гарантия no revoke/withdraw before delivery; chosen admission/check/propagation profile Q16/Q17/F13. Normal request read не выполняет cross-BC UoW/write foreign tokens.

### 7.3. Migrations

Add own Publishing immutable agreement/original request replay storage plus existing policy states/history. Own Catalog projection retains exact original confirmation/agreement/current-period/classification/sourceVersion, no public private-row ID. Own Delivery binding unique License+Variant+SourcePeriod with own factory-generated GrantPeriodId/AgreementId/provenance, no frozen-pointer cache. Current long shared workspace Revision нельзя copy into independent tokens без explicit dev transition profile. Legacy CatalogAccess policy string/version coupling separation, legacy periods without agreement treated insufficient data, not reconstructed from current/date-as-of policy; agreement pipeline/engineering migration Q20.

CM не production: выбранный dev-data policy не сохраняет legacy schema только ради несуществующих production consumers, но не делает unknown historical promises true по today's policy. Source contract IsUnlimitedUsers/IsUnlimitedLicense и ExpiresAt/UpdatesUntil migration Q23, no silently renamed unlimited flag. В этой аналитике migrations/reset/tests не выполняются; plan verification real PostgreSQL roundtrip/immutable updates/unique races.

## 8. External adapters

### 8.1. Public CatalogAccess / Publishing / Delivery contracts

Publishing exposes exact PolicyAgreement and current UI policy separately. CatalogAccess public facts retain source-confirmed immutable AgreementId/original plan, source period tuple and versioned current StartsAt/UpdatesUntil/IsUnlimitedLicense/closure/revoke/current-period/completeness/classification. Delivery copies exact confirmed terms into own binding and exports explained decision to F13. No foreign tracked entities/FK/ambient enlistment.

Creating agreement needs authoritative Catalog Variant/applied parent/F15 facts but no preexisting CM license/grant projection. New expected-context mismatch/pending relation/ambiguous affected F15 gives NotReady/conflict, never random parent/default. Existing exact agreement/binding preserves terms after later parent/policy change. Agreement/original plan mismatch or changed confirmed AgreementId in newer source snapshot is rejected/quarantined by owning source processor, preserving previous binding/history. Affected current basis is incomplete/NotReady, so retaining history does not permit new admission on stale complete-looking facts; missing evidence Indeterminate, not InvalidKey.

F13 owns activation-key HTTP validation, F12 integration caller is authenticated VRS backend, not raw key/Unity/staff issuer. Application license/Project Storage workflow remains independently valid; content-period confirmation waits for agreement explicitly in source UI.

### 8.2. RabbitMQ / MassTransit

PolicyChanged fact durable in Publishing outbox; optional consumer invalidates read/cache/diagnostic projection по scope/Sequence/inbox profile, no existing-period rebinding. [S22](../../../catalog/docs/standards/dotnet/masstransit-rabbitmq.md#optional-three-project-eventbus-composition): exact contract package/topology/queue/dispatch/retry/cache completeness rules Q20. Versioned license/period/revoke facts are F06 integration; effective-state/terminal-ban facts F11/F10. Transport at-least-once/replay требуют stable identity/order, no exactly-once promise.

### 8.3. Clock и source normalization

Trusted server TimeProvider handles now and audit; UTC DateTimeOffset boundaries. VisibilityConfirmedAt supplied only by F13 primary-observation evidence, saved once/serial order. Conservative cutoff business change accepted [Q17](../questions/Q17-publication-availability.md#подтверждённая-видимость-и-история-delivery); time/order failure DataNotReady, no backdating/max clamp/commit-time dependency. Explicit source unlimited is independent from unlimited-users allowance; confirmed update date unknown maps Indeterminate. No synthetic DateTime.MaxValue/zero-date fallback.

### 8.4. VR School planned → agreement → confirmed workflow

Это **логическая external adapter спецификация**, не изменение реальных VRS исходников и не выбор service URI/auth/provider.

**SourcePlannedContentPeriod (logical VRS-owned durable record)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| LicenseId | `long` | Да | Planned source license | VRS owns application/commercial workflow independently |
| ProductVariantId | `Guid` | Да | Planned content scope | Agreement per Variant |
| SourcePeriodId | `Guid` | Да | Durable planned period identity | Changed plan/renewal new ID |
| RequestId | `Guid` | Да | Durable original CM quote request | Same exact input on lost-response retry |
| OriginalPlannedParameters | Уточняется [Q23](../questions/Q23-source-synchronization.md#f12-planned-agreement-confirmed-workflow) | Да | Persisted exact planned input | Not edited under same reservation/quote |
| ExpectedSourceContext | Уточняется [Q15](../questions/Q15-api-and-source-code.md#f12-policyagreement-integration-boundary) | Да | Expected applied Variant/parent | Retained original caller input |
| PolicyAgreementId | `Guid?` | Условно | Confirmed exact CM reply | Mandatory before source confirmation, immutable afterwards |
| State | Уточняется [Q23](../questions/Q23-source-synchronization.md#f12-planned-agreement-confirmed-workflow) | Да | Planned/waiting versus confirmed semantics | Visible source waiting; no falsely confirmed content period |

VRS first commits durable planned period+RequestId/original input. After transaction disposal it calls CM CreatePolicyAgreement; CM reply loss retries same RequestId/input and receives original agreement even current policy changed. It then opens a **new own VRS transaction**, conditionally verifies exact untouched plan/reservation, fixes AgreementId + source confirmation + durable outbox together. Changed plan before confirmation needs new period/RequestId/agreement; old unused agreement remains without entitlement. CM failure leaves source content period visibly unconfirmed; independent application license/Project Storage steps are not rolled back by invented cross-service transaction.

Later source facts carry original AgreementId/plan and explicit current period. Genuine corrections have authoritative classification/reason/history and current versioned values; purchase more time/unlimited upgrade creates a renewal. Source contradictory newer AgreementId cannot silently rewrite old binding. Retries/canonical codecs/auth/URI/recovery numeric defaults remain Q15/Q20/Q23.

## 9. Data flows

Policy HTTP → Api → Publishing.Application → own state/history/outbox → committed result. VRS durable planned period → CM own Publishing agreement commit → VRS own conditional exact-plan confirmation/outbox → Catalog own projection → Delivery exact agreement binding → explicit current-period/effective-history decision → F13 new admission. Broker invalidation не rebind.

| Поток | Транспорт | Метод / канал | Endpoint / destination | Доступ | Вход | Application action | Выход | Успех | Ошибки |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| DF01. Product policy read | HTTP | GET | Product content-policy §5 | Scoped read Q15 | ProductId | GetProductPolicyQuery | Local state/own ETag + resolution carrier Q15 | Exact generation/status Q15 | Missing/scope/read incomplete |
| DF02. Set/Clear Product | HTTP | PUT | Product content-policy | Policy edit Q15 | Nullable policy + own If-Match | SetProductContentPolicyCommand | Committed local result/ETag | Publishing commit/status Q15 | Invalid/428/412/409/scope |
| DF03. Variant policy read | HTTP | GET | Nested Variant content-policy | Scoped read Q15 | Applied parent/Variant | GetVariantPolicyQuery | Local state/token + inherited profile | Exact generation/status Q15 | Parent/scope/read failure |
| DF04. Set/Clear Variant | HTTP | PUT | Same Variant content-policy | Scope/edit Q15 | Route/null policy/own If-Match | SetVariantContentPolicyCommand | Own local result/token | Local commit/status Q15 | Invalid/428/412/409/F15 |
| DF05. Policy history | HTTP target profile | GET | Scoped content-policy/history profile Q15 | History access Q15 | Scope/page/order | ListPolicyHistoryQuery | Immutable version collection/page | Stored history read | Scope/page/read errors |
| DF06. Current resolution | In-process public query | Query | Publishing public policy reader | Trusted scoped caller | Variant/applied context | ResolveCurrentPolicyQuery | EffectivePolicyResolutionDto | Coherent current result | Missing parent/version/port outage |
| DF07. Period binding | In-process application | Ensure binding | Delivery own use case | Confirmed current period/license/Variant | Exact tuple/original plan/AgreementId | EnsureEntitlementPolicyBindingCommand | Exact immutable binding | Own Delivery commit/winner read | Missing agreement/period, scope substitution, race/outage |
| DF08. Entitlement decision | In-process public use case | Evaluate | F13 → Delivery | Confirmed credential license set | Trusted facts/binding/effective history/ban | EvaluateContentEntitlementUseCase | Whole decision/provenance | Allowed/Denied/Indeterminate decision | Fact outage/history incomplete; no secret output |
| DF09. Confirmed multi-license selection | Internal Application/Domain | Select whole | Delivery verified requested-set selector | Every requested basis verified | Independent current-period decisions | ConfirmedLicenseDecisionSelector | Whole Allowed/Denied/Indeterminate | Any necessary unknown ⇒ overall Indeterminate | Unknown never skipped; no implicit organization set |
| DF10. Policy fact invalidation | Outbox then broker | EntitlementPolicyChanged | Chosen read/cache/diagnostic consumer | Trusted versioned contract | Scope/version/Sequence/event identity | Consumer invalidation use case | Read/cache freshness outcome | Consumer commit/ACK profile Q20 | Duplicate/stale/outage, never binding rewrite |
| DF11. Capture PolicyAgreement | Authenticated integration | Command; wire Q15 | Publishing own agreement boundary | VRS backend only | Original RequestId/license/Variant/period/plan/expected context | CreatePolicyAgreementCommand | Original/new immutable agreement | Own CM promise commit before source confirmation | Input replay conflict, expected-context/F15 NotReady, technical failure |
| DF12. Conditional source confirmation | External logical VRS workflow | Own conditional command/outbox | VRS planned period | Trusted source workflow | Durable untouched exact plan+returned AgreementId | Verify plan and confirm | Confirmed source period+outbox | Separate VRS commit, CM HTTP outside transaction | CM response unknown/retry same original request; changed plan new period |
| DF13. Project confirmed period facts | Source outbox/snapshot | Existing versioned source integration | CatalogAccess own projection | Trusted authoritative source | Original confirmed plan/immutable AgreementId/current period/classification | Validate exact public agreement and normalize facts | Current source facts/completeness, no public private-row ID | Own Catalog commit, later Delivery binding | Contradictory agreement reject/quarantine; missing data not empty |
| DF14. Pinned policy evaluation | Internal Application/Domain | Pure evaluation | F13 guarded checkpoint → owned Delivery values | Selected original session basis | PinnedRevisionEvaluationInput | EvaluatePinnedRevision | SessionPinnedDecision/original pin/next finite boundary | Allowed/Denied/NeedsNewAdmission/DataNotReady; no mutation | Necessary unknown temporary; no repin/alternate license/TTL extension |


### DF01. Прочитать Product policy

1. Bind ProductId; verify scoped access/applied existence through public Catalog reader. Query own Publishing state/local version, initial absent representation/token Q15/Q20.
2. Persistence projects local configured value/pointer/order/token from one generation; default/effective resolution в display carrier выбранного scope, not fake configured Freeze.
3. Api maps exact local ETag и response profile Q15. Read no hidden Set/Create version/binding. Independently changing summaries не whole-body tagged own Version без отдельного контракта.

### DF02. Установить либо очистить Product policy

Внутренний write path после Api/FV/проверки caller: открыть owner ReadCommitted transaction, отдельным первым statement получить global pg_advisory_xact_lock на owner connection; только затем выполнять указанные ниже authoritative reads/row locks. Foreign facts — independent primary public ports без enlistment. Все guard/basis checks повторяются для этого commit; §7.2 задаёт failure/recovery и запрет внешнего ожидания под gate.

1. Api parses required own policy If-Match → expected int, validates enum-or-null/ID; gets trusted actor/time/correlation and edit access.
2. Handler loads protected Product policy state and applied Catalog/F15 context; stale own precondition412, invalid domain/context409 according error profile. No F07/F11 Touch.
3. Domain SetPolicy/ClearPolicy creates new immutable scoped version and server next Sequence/current transition; repeat/no-op profile Q20. Null clear selects default via resolution, не third enum.
4. Local commit state/Version+history+required outbox/audit atomically. Existing bindings and license/grant source rows untouched.
5. Map own committed DTO/token, dispatch after commit. Policy change affects new agreements/renewal quotes; failed response reconciliation/read Q15, no claim existing frozen access revoked.

### DF03. Прочитать Variant policy

1. Bind ProductId/VariantId; validate against current applied parent and read permissions. Pending target source parent не route authority; old route after move не automatic POST relocation.
2. Persistence reads own local override/version/token and separately coherent Product inherited/default current resolution through owned queries/public context.
3. Response distinguishes configured-null from effective Freeze/Deny and includes source evidence per Q15. Parent update may change inherited display without Variant Version bump; read/representation/token scope is explicit. No copied Product value stored as Variant override.

### DF04. Установить либо очистить Variant override

Внутренний write path после Api/FV/проверки caller: открыть owner ReadCommitted transaction, отдельным первым statement получить global pg_advisory_xact_lock на owner connection; только затем выполнять указанные ниже authoritative reads/row locks. Foreign facts — independent primary public ports без enlistment. Все guard/basis checks повторяются для этого commit; §7.2 задаёт failure/recovery и запрет внешнего ожидания под gate.

1. Bind applied IDs/nullable enum/own Variant policy If-Match; trusted access/actor/time. Handler protected own state/context/F15, expected Version.
2. Set creates explicit local policy; clear creates immutable null history and local absence, retaining old policy history. Server resolves current Product then system only for display/new agreement quote, not existing entitlement.
3. Commit own state/history/outbox/audit only; Product state/assignments/publication pointers/grant bindings unchanged. Concurrent Product/move commit facts проверены после gate §7.2; result read scope Q15, no implicit parent token requirement.
4. Return own confirmed generation/token and independently scoped inherited result profile. Domain blocker/stale scope/error preserves configured inputs; no blind retry mutation on new parent.

### DF05. Прочитать immutable history

1. Validate scope/route/paging/access, project own version rows filtered by exact stable owner.
2. Order by server Sequence chosen direction; retain null clear rows, exact source version/time/actor, no filtering to currently effective values.
3. Return page/collection per Q15; no per-state ETag from row/list, no delete/archive/rewrite or future current inheritance calculation replacing stored Policy.

### DF06. Разрешить current effective policy

1. Public query receives trusted Variant context; Catalog reader supplies actual applied parent and consistency evidence.
2. Persistence captures local Variant current version/pointer; if non-null policy return Variant source. Otherwise reads current Product version; if non-null return Product source; else system Freeze with no invented version ID.
3. Map coherent resolution/provenance with protected read profile Q20. No binding insertion, history mutation or retroactive period selection. Inherited read failure не автоматически system fallback: absence и недоступность evidence различаются.

### DF07. Сохранить применимую policy периода

1. Own Delivery ReadCommitted transaction/first global gate; fresh Catalog public confirmed License+Variant+SourcePeriod/current-period/immutable AgreementId/original plan facts via independent primary scope.
2. Missing projection/current period/agreement → Indeterminate/DataNotReady; complete authoritative no grant → Denied. Exact public Publishing AgreementId read must match license/Variant/source period/original confirmation; no current/as-of lookup, later parent/policy changes do not rewrite agreement.
3. Lookup own unique tuple. Existing binding returns exact immutable terms only if same agreement/provenance; changed confirmed pointer/plan is source contradiction, never overwrite. Source owner rejects/quarantines contradiction preserving prior binding/history.
4. Genuinely new binding uses trusted server factory for own immutable GrantPeriodId, copies exact AgreementId+frozen policy/source/version; insert atomically in own Delivery transaction. Unique race uses fresh scope/winner comparison, not current policy recomputation; unknown commit inquiry by exact tuple.
5. This binding gives no grant by itself. New admission uses explicit current source period and current versioned facts; renewal has new tuple/agreement and does not nominate old Freeze fallback. Existing pinned sessions use separate DF14/F13 checkpoint; no repin/TTL extension.

### DF08. Вычислить entitlement для одной подтверждённой лицензии

1. F13 supplies verified license/Variant. Read complete authoritative grant/period evidence: complete no grant Denied; source/projection/current period/agreement missing Indeterminate/DataNotReady; grant/license/credential confirmed revoke separate scoped Denied, not InvalidKey.
2. Select source explicit **CurrentPeriodId** for license+Variant and exact binding DF07. Old bindings stay historical; expired current Deny cannot fall back previous Freeze. If current period not yet applicable, no guessed old-period candidate.
3. **Before current/expired Deny/Freeze branches**, validate every necessary completeness/period/agreement/time fact: finite missing UpdatesUntil without explicit IsUnlimitedLicense →Indeterminate, not guessed expiration/unlimited. Versioned current StartsAt/UpdatesUntil/IsUnlimitedLicense/closure facts define present applicability. Genuine classified correction/reason/history preserves agreed policy and recomputes cutoff; purchased time/unlimited upgrade requires renewal. Expiration/completion is not revoke; UpdatesUntil != application expiry.
4. Explicit unlimited or finite now < UpdatesUntil → current Delivery-effective whole state. Expired agreed Deny →refusal. Expired agreed Freeze →last actual history state with stored VisibilityConfirmedAt < current corrected cutoff, including null. Necessary pending/missing/time-order evidence is Indeterminate, not earlier-row fallback; no binding frozen-pointer cache/backdate/rounding.
5. After necessary facts passed step3, complete selected null has no allowed revision/no older non-null fallback. Late ordinary Unpublish/source Retire does not recompose frozen history; exact Withdrawal/final-effective hard dependency restrictions deny without replacement.
6. Return explained whole Allowed/Denied/Indeterminate with exact agreement/binding/current sourceVersion/period/cutoff/revoke/effective provenance. This governs **new selection/admission**; opened sessions use separate DF14/F13 pinned evaluation, never automatic repin; necessary unknown temporary, confirmed refusal scoped.

### DF09. Выбрать whole revision по нескольким подтверждённым правам

1. Verified requested set proves each participating license/credential authorization; one credential/organization never implies all licenses. Evaluate each necessary license+Variant **current** period independently via DF08.
2. Any necessary unknown (projection/agreement/period/history/completeness) yields whole Indeterminate, even another license Allowed; no skip-unknown/global-maximum claim.
3. With all necessary bases known, known Denied others do not cancel Allowed basis. Choose most current whole Allowed revision by authoritative effective order/tie profile Q17; no item/asset union/hash/ID/created-order guess. No Allowed → explained Denied.
4. Return one exact license/current period/agreement/binding basis for new F13 admission; old binding history preserved. Opened-session basis/timing not switched automatically by this selection.

### DF10. Обработать PolicyChanged

1. Publishing mutation writes versioned event in local durable outbox alongside state/history. Dispatcher sends committed fact; unknown send acknowledgment can replay.
2. Chosen consumer verifies scope/version/Sequence/event identity, applies inbox/dedup/out-of-order checks and invalidates only relevant read/cache/diagnostic projection according profile Q20.
3. Consumer commits own freshness/inbox result then ACK; failure remains recoverable. No bindings rewritten, no F11 ActiveRevisionId transition, no credential/license permission derived from event alone. Cache stale/error cannot replace missing authoritative applicability facts with assumed current policy.

### DF11. Capture immutable PolicyAgreement до source confirmation

1. Authenticated VRS backend only; validate original RequestId/license/Variant/SourcePeriodId/plan/expected context, no raw activation key/staff/Unity issuer.
2. Own Publishing ReadCommitted transaction; first global advisory gate. Lookup stored trusted caller+RequestId **before new Catalog quote checks**. Same normalized original input replays original agreement; fresh derived policy/parent is not replay comparison. Changed bound input conflict.
3. New quote resolves authoritative applied Variant/parent/F15 via independent primary Catalog read. Expected mismatch/pending relation/ambiguous affected context NotReady/conflict; never random parent. No CM license projection/grant needed.
4. Read own current Variant/Product policy and capture precedence/system Freeze with null version. Save immutable exact plan scope/applied parent/policy/provenance/request replay and required own safe intent atomically.
5. CM commit is promise checkpoint. HTTP loss →same RequestId/input replay original terms even policy/parent changed. No auto expiry; unused agreement grants nothing.

### DF12. Source conditional exact-plan confirmation

1. VRS own commit persists planned period+RequestId/original plan/expected context durably; source UI keeps content period waiting for agreement.
2. Dispose VRS transaction, call CM DF11 with auth/cancellation; no source SQL transaction around network request. Lost reply retry same original input/RequestId.
3. After reply, new own VRS transaction conditionally checks exact unchanged reservation/plan, stores exact AgreementId+confirmed period+outbox together. No shared source/CM commit.
4. Changed plan before confirmation requires new SourcePeriodId/request/agreement; old unused agreement remains without entitlement. Application license/Project Storage workflow remains independently committed; content confirmation failure visible, no fake confirmed right.

### DF13. Catalog projection/renewal/correction/integrity

1. Existing source integration validates trusted sourceVersion/completeness and explicit current period per license+Variant. Read exact immutable Publishing agreement via independent primary public port under own Catalog gate; validate original scope/plan/confirmed pointer.
2. Confirmed AgreementId immutable. Contradictory newer pointer/substituted scope/plan rejected/quarantined; prior binding/history not overwritten. Affected current basis becomes incomplete/NotReady; retaining old history does not permit new admissions on an apparently complete old projection. Missing agreement/period is DataNotReady, not successful empty/InvalidKey.
3. Normalize confirmed tuple License+Variant+SourcePeriod; license-wide source period sharing allowed but each Variant requires its agreement. Catalog private row ID stays internal; Delivery allocates own binding identity later.
4. Renewal always new period/agreement; when new period becomes current old period no longer candidate fallback. Genuine same-period correction has authoritative classification/reason/history; retains agreed policy and updates versioned StartsAt/UpdatesUntil/unlimited/closure/revoke facts. Purchase time/unlimited upgrade is renewal.
5. Own Catalog projection/outbox/inbox commit remains separate from Publishing agreement and Delivery binding; later DF07 copies exact promise. Session consequences follow separate DF14 and F13 accepted Q16.

### DF14. Evaluate original pinned revision

1. F13 guarded response checkpoint maps fresh selected public facts to owned PinnedRevisionEvaluationInput after session security/absolute expiry checks; no VRS HTTP/policy I/O inside pure evaluator.
2. Validate selected current-period identity/completeness/agreement and original pin restrictions. Renewal → NeedsNewAdmission; necessary unknown → DataNotReady.
3. Active updates allow still-eligible original pin. Corrected finite Deny equality denies. Expired Freeze recomputes confirmed whole candidate with complete necessary cutoff history and compares exact pin, never repins/falls back another license.
4. Return original pin/outcome/next finite policy boundary to F13 lease/terminal-state orchestration; original TTL and Agreement remain unchanged.

## 10. Ошибки и диагностика

| Ситуация | Семантика / результат |
| --- | --- |
| Missing/stale own policy If-Match | 428/412; read current own state/token, no implicit action replay |
| Invalid enum/null binding/IDs/header | Validation400 profile S16/Q15; null policy clear valid |
| Wrong parent/access/F15 scope | 401/403/404/409 profile Q10/Q15; no silent mutation on new Product |
| Missing projection/agreement/current period/finite cutoff/completeness | Indeterminate/DataNotReady for new selection, not InvalidKey/revoke/current/default/as-of promise |
| Complete authoritative no grant / confirmed revoke / not-yet-applicable current period | Explained distinct Denied reasons; Variant grant vs license vs credential scope, expiration not revoke |
| Deny after cutoff | Expired permission denial, not data loss/deletion |
| Complete history yields null before cutoff | No frozen composition, no earlier non-null fallback |
| Effective history incomplete/delayed artifact | Indeterminate or readiness result Q17; don't backdate Publishing activation |
| Frozen exact Withdrawal | Denied same exact revision, no replacement; audit/history retained |
| Source agreement substitution / changed quote input / concurrent binding | Reject/quarantine contradiction preserving old terms; request/tuple replay inquiry; no current policy overwrite |
| Any necessary requested-license evidence unknown | Whole multi-license Indeterminate; no skip-unknown/global maximum claim |
| Public port/broker/DB unavailable | Technical outage distinguished from complete no grant; existing binding not proof current source/ban readiness |

Public machine strings/status mappings, diagnostic disclosure/access Q15/F13. Log safe license/Variant/period/policy-version/revision IDs, SourceVersion evidence, own transition Sequence, effective projection provenance/cutoff and reason class; no raw activation key/token/connection secrets/source content JSON. Explainability includes policy source/version and selected commercial basis, not only “Denied”. Metrics: binding conflicts/recovery, insufficient-data decisions, history readiness lag, policy-outbox backlog; threshold/retention/SLA Q17/Q20, no unsolicited numeric defaults.

Global gate timeout/cancellation/failed port/owner connection loss — технический abort, не domain409 или empty valid facts. Unknown commit ACK требует inquiry/operation identity по Q15; reconnect continuation/blind retry с новым ETag не допускаются.

## 11. Тестирование

[S04](../standards/S04-testing.md)/[S14](../../../catalog/docs/standards/dotnet/testing.md#test-environment-ownership-and-bounded-parallelism). Unit SUT — state/history/binding/domain evaluator/resolver/validators/mappers; pure owned inputs and deterministic clock, port doubles only for external evidence. Integration SUT — owning Publishing/Delivery PostgreSQL/Testcontainers repositories/query/UoW/unique keys/outbox; real SQL concurrency/immutability, no in-memory provider. Functional — CM HTTP/JWT/PostgreSQL local policy Api/handlers/ETag, controlled external adapters with boundary stated. E2E — necessary real source-period/Publishing/Delivery/credential/broker contour F13/F06 where needed, observed effective readiness and terminal ban; multiple BC in one API alone still Functional.

| ID | Уровень | Сценарий / DF | Ожидаемый результат |
| --- | --- | --- | --- |
| P01 / DF11–13,07 | PostgreSQL Integration/fault | Freeze quote→Deny edit→delayed exact source confirmation; lost CM reply same RequestId/input | Original immutable Freeze agreement and binding; replay before fresh quote checks, no fresh derived-input comparison |
| P02 / DF11–12 | Integration external workflow fixture | Source/CM failures, changed plan before confirmation, retry older original quote | Source waiting visible; network outside source transaction; changed plan new period/agreement, unused old quote no entitlement |
| P03 / DF13,07 | PostgreSQL Integration | Substitute AgreementId/scope/original plan in newer confirmed source snapshot | Reject/quarantine contradictory source; prior binding/provenance/history preserved |
| P04 / DF08 | PostgreSQL Integration | Renewal new current Deny after old Freeze; genuine correction shifts same-period cutoff; purchased time/unlimited upgrade | No old Freeze fallback; correction preserves policy/recomputes cutoff; purchase/upgrade requires new period/agreement |
| P05 / DF07–09 | Integration + Functional | Complete authoritative no grant vs projection/agreement/current-period missing; cutoff unknown/unlimited/users/application expiry | Distinct Denied/Indeterminate/revoke reasons; not InvalidKey; explicit updates-unlimited only |
| P06 / DF11/auth | Functional integration boundary | Authenticated VRS backend vs Unity/staff/raw-key caller; absent CM license projection but valid applied context | Only backend quote; valid exact quote needs no license projection/grant; no raw-key/staff issuer flow |
| T01 | Unit + Integration | DF02/04 Set/Clear and precedence DF06 | Two values only; null inherits/default, no copied override; immutable clear history |
| T02 | Functional | DF01–04 own ETag/If-Match/initial/repeat profile | No Version body, missing428/stale412, result matches own commit; no workspace token |
| T03 | Integration | Policy simultaneous actions/commit failure/history sequence | Unique monotonic scope order; state/history/outbox atomic, no partial row |
| T04 | Functional + Integration | Parent policy change/move with unchanged Variant local state | Effective display refreshed/scoped token, own override/bindings unchanged, no wrong-parent mutation |
| T05 | Integration | Agreement Freeze t1, current Deny t2, exact source confirms t3, first use later | Exact agreement Freeze retained; no date-as-of/current-policy lookup |
| T06 | Integration | Concurrent exact tuple binding + crash/unknown outcome; same source period shared by Variants | One own binding per License+Variant+SourcePeriod; independent per-Variant agreements/winner terms |
| T07 | Unit + Integration | DF08 finite now before/equal/after cutoff, explicit unlimited, unknown | Strict boundary; unlimited explicit; missing cutoff Indeterminate; users flag doesn't extend updates |
| T08 | PostgreSQL Integration/E2E | Full original Delivery commit before cutoff, primary confirmation/save after; equality | Conservative saved VisibilityConfirmedAt excludes, no first/second commit time claim or second rounding |
| T09 | PostgreSQL Integration | Last confirmed before cutoff null; late Unpublish; pending/missing required history; rollback | Null no fallback; late confirmed Unpublish preserves prior Freeze; necessary unknown Indeterminate; rollback new history/order/time |
| T10 | Unit + E2E | Withdraw active/inactive/frozen plus delayed activation replay | Exact frozen denied/no replacement; terminal ban survives replay; no client installed-data delete command |
| T11 | Unit + Integration | Renewal after gap, new cutoff, reinstall frozen | New binding/current whole composition, next freeze new boundary; no extra download deadline |
| T12 | Unit + Functional | DF09 verified requested set: known Denied+Allowed versus necessary unknown+Allowed | First chooses whole Allowed; second overall Indeterminate, no implicit licenses/unknown skipping/global maximum claim |
| T13 | Integration | PolicyChanged duplicate/out-of-order/cache outage | Only read invalidation; existing bindings immutable; outage ≠ default/null evidence |
| T14 | Architecture | Layer placement/DI/foreign DbContext/generated contracts | Commands Application/queries Persistence, public cross-BC ports, one mutation path |

Fixtures contain multiple Products/Variants/periods/credentials, clear markers, same-time different-order effective rows incl null, policy changed between agreement commit/source confirmation/use, revoked/unknown/unlimited distinctions. Unique fixture IDs/test database isolation+cleanup project conventions, barriers/gates for races not arbitrary sleeps; microsecond PostgreSQL precision handled explicitly at boundary tests. Own fixture cleanup не runtime history deletion. Domain/Functional doubles don't prove transport/source contract or real session authorization.

Current [EntitlementSelectionTests](../../../../contentmanager.api/tests/BoundedContexts/Delivery/ContentManager.Delivery.Tests.Unit/EntitlementSelectionTests.cs) expects today's null→PublicationWithdrawn before Freeze lookup; [CatalogSynchronizationTests](../../../../contentmanager.api/tests/App/ContentManager.App.Tests.Integration/CatalogSynchronizationTests.cs) tests policy retention on legacy source period; [PublishingDeliveryIntegrationTests](../../../../contentmanager.api/tests/App/ContentManager.App.Tests.Integration/PublishingDeliveryIntegrationTests.cs) covers direct-context legacy sessions. These names/code do not prove target binding/effective-time/no-late-null gate. В рамках аналитики tests/build не запускались и код не менялся.

| ID / DF | Уровень / SUT | Исходные данные / сценарий | Ожидаемый результат |
| --- | --- | --- | --- |
| G01 / guarded write flows | PostgreSQL Integration, separate owner connections/CM replicas | Pause writer after gate; concurrent relevant writer with same stable key | Second writer waits; first commits own BC only; second loads fresh committed facts, no forbidden stale commit |
| G02 / guarded write flows | PostgreSQL Integration + architecture/DI | Independent public primary reads while owner holds gate; foreign UoW enlistment/shared multi-DbContext participant | Reads see committed facts after acquisition; no foreign EF/transaction participant/cache/replica; own writes+outbox/audit intent/inbox atomic |
| G03 / guarded write flows | Fault integration | Bounded acquisition cancellation/timeout, failed read, owner connection loss, lost commit ACK | Technical abort/unknown outcome; no fake compatible empty facts or reconnect continuation; original own412 and independent basis409 preserved |

Pinned evaluator tests additionally cover R1 pinned while R2 active, renewal period mismatch, corrected Deny equality with longer stored TTL, corrected Freeze candidate equal/different/null/necessary unknown, expansion without TTL extension/terminal restoration. Real PostgreSQL F13 checkpoint tests verify scoped outcomes; pure doubles alone do not prove lease cancellation.

## 12. Порядок реализации

1. Implement approved immutable PolicyAgreement checkpoint/exact plan scope/replay ordering; finalize Q15 integration URI/auth/codec/errors and Q23 original planned/current source contract fields, Q20 physical mapping.
2. Publishing Domain policy/history/agreement constraints and Application.Abstractions Request/Read/query/ports; Application authorized quote handler/normalizer/current capture after first gate.
3. Own Publishing EF immutable agreement/request replay unique indexes/DI; separate Catalog public primary context adapter, no foreign UoW. Real PostgreSQL lost-ACK/policy-race tests.
4. External VRS durable planned→CM outside transaction→own conditional confirmation/outbox contract. Source Catalog validates original agreement/scope/current period/correction/completeness and preserves contradictory-history evidence.
5. Delivery exact tuple immutable binding + agreement reader; new-selection current-period/correction/completeness/strict multi-license evaluator/effective history/final closure. Implement separate pinned evaluator DF14 and F13 accepted session/replay/lease protocol.
6. Existing policy ETag/null clear/OpenAPI/history and source/Delivery public ports verified; no current/as-of promise reconstruction. Source integration/replay/tuple/failure tests plus necessary real contour labelled distinctly.
7. Confirm actual integration OpenAPI/codecs before generated clients; no staff/Unity agreement issuer UI or invented endpoint/security provider.



## 13. Открытые технические вопросы

- [Q15 — agreement integration boundary](../questions/Q15-api-and-source-code.md#f12-policyagreement-integration-boundary): exact URI/method/JSON/envelope/status/auth adapter/request normalization/privacy and original planned parameter carrier; no staff/Unity/raw-key issuer.
- [Q20 — agreement and period confirmation](../questions/Q20-persistence-and-migration.md#f12-policyagreement-и-подтверждение-периода): physical own tables/indexes/DI/immutable safeguards/request replay encoding/migrations/recovery; checkpoint/unique tuple/no-expiry are approved.
- [Q23 — planned→agreement→confirmed workflow](../questions/Q23-source-synchronization.md#f12-planned-agreement-confirmed-workflow): exact source planned/current-period/original plan/classification/reason/history/completeness/version contract and quarantine/retry adapters, not current/as-of policy selection.
- [Q17 confirmed visibility](../questions/Q17-publication-availability.md#подтверждённая-видимость-и-история-delivery): approved conservative primary observation checkpoint/completeness/null/order/cutoff semantics; physical reader/codec [Q20](../questions/Q20-persistence-and-migration.md#delivery-catch-up-и-подтверждение-видимости).
- [Q16 accepted session protocol](../questions/Q16-activation-and-sessions.md#принятый-протокол-сессий-и-response-leases): pinned evaluator mechanics selected DF14/F13; exact wire/physical security source/keyring/retention profiles remain engineering Q15/Q20/Q23.
- [Q15 — policy staff API](../questions/Q15-api-and-source-code.md#f12-policy-contracts-и-diagnostics): own/inherited read token scope, exact policy/history transport/permissions/no-op profile.

Agreement before source confirmation, nontransferable original scope, exact binding/current period/renewal-versus-correction/completeness/strict multi-license semantics approved; do not list them as undecided implementation alternatives.
