# F13. Доставка и возобновляемая синхронизация контента: бэкенд

[Бизнес](../../../business/features/F13-content-delivery.md) · [M05](../modules/M05-delivery.md) · [Фронтенд](../../frontend/features/F13-content-delivery.md) · [SDK F19](../../frontend/features/F19-unity-client.md) · [F12](F12-content-entitlement-policy.md) · [Q15](../questions/Q15-api-and-source-code.md#f13-runtime-contracts-и-source-evidence) · [Q16](../questions/Q16-activation-and-sessions.md#f13-session-credential-и-revocation) · [Q20](../questions/Q20-persistence-and-migration.md#f13-projection-session-и-plan-storage) · [Q30](../questions/Q30-content-synchronization.md) · [Q33](../questions/Q33-sync-resume-transport.md#f13-wire-protocol-и-execution-ownership) · [Q19](../questions/Q19-content-hashes.md).

## 1. Область реализации

**Demo:** SDK передаёт полный selection exact SchemaId+SchemaVersionId и actual managed inventory; CM выбирает latest Published внутри каждой exact структуры, строит immutable Add/Update/Remove plan, возвращает компактный Start JSON, выдаёт один resumable streaming HTTP response exact данных и принимает отдельные group receipts. Atomic client apply — целая schema/version group. SDK автоматически восстанавливает пригодную попытку, после успешного исполнения делает отдельный freshness/preflight check. Assets bytes — отдельный CM-owned YARP gateway F05, не sync data response. Demo не требует Identity/auth/license/VR School/broker/Publishing/DeliverySession.

**MVP:** separate named CmDeliverySession runtime scheme validates explicit full raw credential set for creation/recovery through authoritative VR School HTTP, использует trusted license/grant/period facts и F12 exact source-confirmed PolicyAgreement binding/current-period evaluator, выбирает одну whole Delivery-effective revision и создаёт pinned protected session. Manifest/items/assets requests проверяют session credential/status/expiry/revoke/withdrawal и membership в exact materialized snapshot. Publishing projector/terminal restriction facts обновляют Delivery-owned state/history, не live client composition. Demo synchronization ID — operation identity, не MVP credential.

F19 владеет SDK orchestration/staging/installed inventory/atomic apply; F20 — demo scene. F13 описывает серверный desired-set/plan/transport и MVP projection/session/read. SDK never supplies client-verified credential/license IDs as authority: first request knows only raw key set+Variant+durable CreationRequestId/provider context. Server derives normalized verified meaning. SDK persists request ID/nonsecret intent before call, not raw keys/key hashes by default; application credential provider owns recovery proof. Новая активация не repins старую session, late ordinary Unpublish не отменяет frozen access. Revoke/Withdrawal прекращают последующую серверную выдачу, не remote installed-data delete command.

Demo реализуется [SynchronizationController](../../../../contentmanager.api/src/BoundedContexts/Delivery/ContentManager.Delivery.Api/Synchronization/SynchronizationController.cs), public contracts в Delivery.Application.Abstractions/Synchronization, canonical writers в BuildingBlocks/ContentManager.Canonicalization и singleton [SynchronizationCoordinator](../../../../contentmanager.api/src/BoundedContexts/Delivery/ContentManager.Delivery.Application/Synchronization/SynchronizationCoordinator.cs). ContentModeling owning RuntimeSynchronizationReader читает primary PostgreSQL отдельными короткими READ ONLY REPEATABLE READ scopes. Планы сохраняют metadata/identities/hashes, payload читается по pinned exact version; SQL plan/session aggregate не добавляется. Выбранный настраиваемый [числовой профиль](../questions/Q33-sync-resume-transport.md#числовой-профиль-demo-f13) валидируется при startup.

Прежние [InternalDeliveryContracts](../../../../contentmanager.api/src/BoundedContexts/Delivery/ContentManager.Delivery.Application/InternalDeliveryContracts.cs) и [InternalDeliveryService](../../../../contentmanager.api/src/App/ContentManager.Application/Delivery/InternalDeliveryService.cs) сохраняются как отдельный trusted/protected путь. Они не реализуют целевой MVP session credential/TTL/revocation/key-validation протокол. Поля MVP ниже остаются нормативной целевой моделью; наличие Demo контроллера не подтверждает MVP.


### Demo: физические владельцы в исходниках

| Слой | Concrete implementation |
| --- | --- |
| Domain | Новых Demo aggregates/SQL sessions нет; exact representation values принадлежат source. Старые protected models не подменяют sync cache |
| Application.Abstractions | SynchronizationContracts/SynchronizationMessages: public DTO, ISynchronizationService, ISynchronizationSourceReader, ISynchronizationDataLease; lifecycle/lease/generation не credential |
| Application | SynchronizationPlanBuilder/PlanModels, SynchronizationCoordinator (private Attempt/Worker/Tombstone), CursorCodec, TransportOptions, validators/command handlers, ContentModelingSynchronizationSourceReader adapter |
| Api | SynchronizationController, scoped SynchronizationJsonContract и OpenAPI header transformer; control JSON отдельно от raw NDJSON writer с ограниченными сегментами |
| EntityFramework | Существующие ContentModeling Published canonical columns; новая Demo migration не требуется |
| EntityFramework.Persistence | ContentModeling RuntimeSynchronizationReader: coherent primary capture/exact bytes/availability; Delivery SynchronizationQueryHandlers: status/Freshness через public service port |
| BuildingBlocks / App | Pure netstandard2.1 SynchronizationCanonicalizer/input types; App связывает owning ports/singletons/startup options и исключает sync commands из общего SQL UoW |
### 1.1. Post-Demo: launcher entity sync и доставка медиа

Digital Space Launcher синхронизирует структурированные сущности с frozen AssetReference, затем запускает только выбранную предзагрузку. Unity-приложение читает закреплённый SDK snapshot и самостоятельно запрашивает отсутствующие файлы через CM-owned F05 gateway. Entity synchronization и media download — разные lifecycle/results: optional preload failure не откатывает entity snapshot; `RequiredBeforeLaunch` блокирует запуск только snapshot, содержащего эту реальную ссылку, до verified bytes. Режимы принадлежат exact schema leaf definitions F01, default `OnDemand`; policy не является новым entitlement и не смешивается с IsRequired.

Для этого предложен resolved `AssetDeliveryManifest` одной разрешённой exact ProductVariantPublicationRevision, после наследования/assignments/overrides/exclusions и whole-revision entitlement selection F12. Его формат/API/ID/hash/serialization/materialization owner остаются [Q37](../../frontend/questions/Q37-launcher-asset-delivery.md); это проектное предложение интеграционного этапа, не новый public wire. F13 Start/Data/Commit/Status v1, `cm-publication-manifest/1`, sync-plan/inventory/group hashes и frozen descriptor не расширяются. Самостоятельный Demo/F20 продолжает получать image отдельно, без launcher/manifest/preload dependencies.

## 2. Domain

### 2.1. Aggregates, entities и value objects

| Тип | Роль | Владелец / граница |
| --- | --- | --- |
| ProductVariantDeliveryState | MVP aggregate root | Projector-owned current effective state; no runtime CRUD |
| DeliveryTransitionProcessingOutcome | Immutable source-processing receipt | Exact Variant+PublishingSequence/meaningful input → original EffectiveCommitted or NotEffectiveTerminalRestriction outcome |
| DeliveryActivationRecord | Immutable actual effective transition history | One full committed transition per commit, including null; independent of processed source sequence |
| DeliveryVisibilityEvidence | Pending→Confirmed durable observation evidence | Primary re-observation of exact already committed history/full snapshot; time saved once |
| DeliveryTemporalDecisionValues | Owned immutable Domain value | Pure evaluator input, mapped from read carrier; no Domain→Abstractions dependency |
| DeliveryPublicationSnapshot | MVP aggregate root | Content immutable, distribution status mutable; no live recomposition |
| DeliveryPublicationItem | Immutable snapshot value | IDs+lookup metadata; authoring Name не identity |
| DeliverySchemaContractSnapshot | Immutable value if schema contracts included | Owned artifact membership; inclusion/reader representation Q15, no latest schema substitution |
| RevisionDistributionRestriction | Terminal MVP fact/root | Exists even before snapshot; can't be lost by late materialization |
| DeliverySession | MVP aggregate root | Exact pinned basis; no raw activation key, no implicit license switch |
| SessionCredentialMaterial | Ephemeral server result | 32 CSPRNG bytes/Base64Url opaque bearer; private SHA-256 verifier; temporary encrypted replay exception |
| PolicyAgreement / EntitlementPolicyBinding / decision | Agreement model owned Publishing, binding/decision owned Delivery; defined once [F12](F12-content-entitlement-policy.md#21-aggregates-entities-и-value-objects) | Exact confirmed license+Variant+source period; not current/as-of quote |
| SchemaRepresentationIdentity | Shared immutable sync value | Distinguishes multiple structures of one instance; Key not substitute |
| InstalledInventoryEntry | Application input value | Actual full managed inventory, untrusted declaration not proof filesystem |
| ExactSchemaSelection | Application input value | Full mandatory list; explicit empty vs absent |
| SynchronizationPlan | Immutable Application cached model, **not Domain aggregate** | Local CM cache; default retention1h configurable; physical cache/TTL profile Q33 |
| SchemaSynchronizationGroup | Immutable plan value | Atomic client result unit, including deselected-group removals |
| SynchronizationOperation | Immutable planned value | No latest-at-fetch; Remove scoped to managed identity |
| SynchronizationExecutionState | Mutable Application/cache state | Separate from immutable content plan; not licensed security session/SQL aggregate |
| SynchronizationExecutionOwner / SynchronizationControlReceipt / SynchronizationTransportOptions | Private Application/cache/Host models | Worker fence and monotonic lease, bounded original result replay and validated configuration; not Domain→Abstractions dependency |

**ProductVariantDeliveryState**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductVariantId | `Guid` | Да | Own Variant scope | Projector-owned, no runtime CRUD |
| EffectiveRevisionId | `Guid?` | Нет | Current fully committed snapshot pointer or null | Not Publishing active; matching ID does not prove time readiness |
| CurrentEffectiveHistoryId | `Guid?` | Нет | Exact current actual transition history row | Null only no actual transition yet; known-null transition has non-null row ID |
| LastProcessedPublishingSequence | `long` | Да | Last source transition with stored processing outcome | Advances for known terminal non-effective outcome; not effective-history/time order |
| Version | `int` | Да | Own conditional state token | Internal; no runtime DTO |

**DeliveryTransitionProcessingOutcome**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductVariantId | `Guid` | Да | Source transition scope | Unique with PublishingSequence |
| PublishingSequence | `long` | Да | Exact committed F11 transition order | No silently skipped gaps |
| MeaningfulSourcePayload | Уточняется [Q15](../questions/Q15-api-and-source-code.md#delivery-visibility-contracts) | Да | Canonical exact source identity/target/reason/time/correlation values | Same tuple different meaning → integrity conflict; not arbitrary JSON textual order/hash guess |
| Outcome | `DeliveryTransitionOutcomeKind` | Да | EffectiveCommitted or NotEffectiveTerminalRestriction | Technical missing artifact cannot be terminal non-effective |
| EffectiveHistoryId | `Guid?` | Условно | Own actual effective row ID | Required EffectiveCommitted; absent terminal blocked outcome |
| BlockingRevisionId | `Guid?` | Условно | Exact authoritative terminal ban reason | Required non-effective restriction outcome |
| CorrelationId | `Guid?` | Нет | Trusted source/processing trace | No secrets/raw payload diagnostic dump |

**DeliveryTransitionOutcomeKind** — selected logical values EffectiveCommitted, NotEffectiveTerminalRestriction; wire enum spelling/codec Q15.

**DeliveryActivationRecord**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | `Guid` | Да | Server-generated own transition identity | Immutable history, retained |
| ProductVariantId | `Guid` | Да | Own history scope | Exact Variant |
| PublishingSequence | `long` | Да | Origin committed source transition | Unique effective row per Variant+source sequence; subset of processed outcomes |
| EffectiveOrder | `long` | Да | Actual effective transition order | One transition per own commit; exact allocation/storage profile Q20 |
| RevisionId | `Guid?` | Нет | Exact fully materialized target or intentional null | Null is full history state, no older non-null fallback |
| PublishingActivatedAt | `DateTimeOffset` | Да | Producer audit time | Never cutoff checkpoint |
| Reason | Уточняется [Q15](../questions/Q15-api-and-source-code.md#delivery-visibility-contracts) | Да | Trusted original transition reason | F11 selected action; codec Q15 |
| CorrelationId | `Guid?` | Нет | Trusted source trace | Preserved on replay/rollback |

**DeliveryVisibilityEvidence**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| EffectiveHistoryId | `Guid` | Да | Exact owning committed history row | Unique one evidence record; own Delivery FK |
| ProductVariantId | `Guid` | Да | Confirmation/order scope | Serial oldest necessary pending first per Variant |
| PublishingSequence | `long` | Да | Exact source transition identity | Same committed history identity, no current-pointer equality prerequisite |
| State | `DeliveryVisibilityEvidenceState` | Да | Pending or Confirmed | Pending until durable confirmed time stored |
| Version | `int` | Да | Own mutable evidence conditional token | Internal; Pending→first confirmed winner, never DTO/user If-Match |
| VisibilityConfirmedAt | `DateTimeOffset?` | Условно | Primary PostgreSQL UTC observation time AFTER exact committed history/full snapshot was read | Required Confirmed; saved once winner, not either transaction's commit timestamp |
| ClockOrderValid | `bool` | Да | Detected temporal integrity state | Detected time/order failure affects temporal decision DataNotReady, no quiet max clamp/backdate |
| ObservationCorrelationId | `Guid?` | Нет | Safe trusted observation trace | Technical diagnostic context only |

**DeliveryVisibilityEvidenceState** — Pending, Confirmed; transport codec Q15. Clock failure is reported separately, never fabricated confirmation time.

**DeliveryTemporalHistoryPoint (owned immutable Domain value)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| EffectiveOrder | `long` | Да | Actual effective order | Not artifact age/processed progress |
| RevisionId | `Guid?` | Нет | Exact actual target/null | Null retained, no older non-null fallback |
| VisibilityConfirmedAt | `DateTimeOffset?` | Условно | Saved primary observation | Required confirmed; no guessed commit time |
| ConfirmationKnown | `bool` | Да | Durable confirmation known | Pending/missing false |
| TemporalOrderValid | `bool` | Да | Validated temporal integrity | Failure affects needed temporal decision |

**DeliveryTemporalDecisionValues (owned immutable Domain input)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| History | `IReadOnlyList<DeliveryTemporalHistoryPoint>` | Да | Owned flat snapshots for pure evaluator | No Abstractions/EF/tracked entities |
| SourceRangeComplete | `bool` | Да | Required decision/cutoff-prefix completeness | Not blanket live catch-up readiness; necessary missing prefix unknown |
| HasNecessaryPendingConfirmation | `bool` | Да | Required purpose/cutoff pending basis | Necessary unknown Indeterminate, irrelevant later pending not veto |
| TemporalOrderValid | `bool` | Да | Necessary temporal integrity | No quiet clamp |
| CurrentStateKnown | `bool` | Да | Actual current known versus unestablished | Known null/complete empty/unknown distinct |
| CurrentRevisionId | `Guid?` | Нет | Current committed target/null | No Publishing pointer substitute |
| CurrentEffectiveOrder | `long?` | Условно | Actual committed current order | Needed proven current; not artifact age |
| CurrentSnapshotComplete | `bool` | Да | Full current commit readiness | Otherwise allowed current may serve while time pending |

**DeliveryPublicationSnapshot**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| RevisionId | Уточняется | Да | Exact F10 artifact | Immutable; no live recomposition |
| ProductVariantId | Уточняется | Да | Snapshot owner | Immutable |
| Items | Уточняется | Да | Exact materialized content | Immutable complete snapshot; no partial effective |
| AssetIds | Уточняется | Уточняется | Exact asset membership | Immutable; profile Q15/Q19/Q20 |
| SchemaContracts | Уточняется | Уточняется | Captured schema contracts | Artifact inclusion/reader profile Q15/Q19 |
| Manifest representation | `CanonicalRepresentation` из [F03](F03-publish-schema-version.md#33-dto-и-parameter-models) | Да | Exact F10 runtime manifest bytes/profile/digest | `cm-publication-manifest/1`; copied unchanged, no second serializer |
| Manifest hash | `CanonicalDigest` | Да | Stored manifest digest | Matches exact triple; 64 lowercase hex; not auth/ETag |
| CreatedAt | Уточняется | Да | Snapshot creation time | Trusted; content immutable |
| WithdrawnAt | Уточняется (nullable) | Условно | Terminal availability ban | Mutable distribution status |
| Version | `int` | Да | Own lifecycle concurrency | Internal mutable token; no runtime DTO field |

**DeliveryPublicationItem**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| EntitySchemaId | Уточняется | Да | Logical schema identity | Exact snapshot membership |
| SchemaVersionId | Уточняется | Да | Exact schema identity | No latest schema substitution |
| EntityInstanceId | Уточняется | Да | Logical instance identity | Not authoring Name |
| EntityInstanceKey | Уточняется | Уточняется | Lookup metadata | Key не substitute identity |
| EntityInstanceVersionId | Уточняется | Да | Exact content version | Immutable pinned data |
| VersionNumber | Уточняется | Уточняется | Published business order | Не technical token |
| ContentJson | `ReadOnlyMemory<byte>` | Да | Exact values-only canonical UTF-8 | Immutable bytes declared ContentHashProfile: Demo/historical /1, Offset new MVP profile; no BOM/structural whitespace/final LF |
| ContentHash | `CanonicalDigest` | Да | Exact content digest | SHA-256 same bytes; no rehash/repair |
| ContentHashProfile | `CanonicalProfileId` | Да | Digest interpretation | Actual frozen supported profile: Demo/historical /1, Offset new MVP; compare with profile, no fallback |
| AssetIds | Уточняется | Уточняется | Exact membership | Immutable; no temporary URL identity |

**DeliverySchemaContractSnapshot**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| EntitySchemaId | Уточняется | Да | Logical schema | If included in artifact; owned membership |
| SchemaVersionId | Уточняется | Да | Exact structure | No latest substitution |
| VersionNumber | Уточняется | Уточняется | Business version | Artifact profile Q15/Q19 |
| Published contract representation | `ReadOnlyMemory<byte>` | Да | Exact semantic schema UTF-8 bytes | Actual frozen schema-content profile (/1 historical/new MVP); same bytes as Contract integrity; Registry JSON Schema/ETag separate |
| Contract integrity | `CanonicalRepresentation` из F03 | Да | Frozen semantic contract bytes/profile/digest | Actual frozen supported schema-content profile (/1 historical/new MVP); Registry representation/ETag independent |

**RevisionDistributionRestriction**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| RevisionId | `Guid` | Да | Exact terminal banned artifact | Own unique ID fact, independent of current pointer/materialization |
| ProductVariantId | `Guid` | Да | Exact owner scope | Cannot be lost by late materialization |
| WithdrawnAt | `DateTimeOffset` | Да | Trusted originating restriction audit time | Not Delivery visibility/cutoff timestamp |
| Reason | Уточняется [Q15](../questions/Q15-api-and-source-code.md#delivery-visibility-contracts) | Да | Trusted terminal reason | Logical restriction code; final wire Q15 |
| SourceFactIdentity | Уточняется [Q20](../questions/Q20-persistence-and-migration.md#delivery-catch-up-и-подтверждение-видимости) | Да | Trusted durable restriction dedup identity | Replay original meaning; contradictory identity integrity error |
| CorrelationId | `Guid?` | Нет | Trusted safe trace | No secret/payload exposure |

Ban is authoritative exact-revision fact even inactive/unmaterialized. Delayed activation/replay never lifts it; new admission reads owner public ban, protected reads enforce applicable restriction before cache/304/range. Unknown evidence is not Allowed; already in-flight behavior follows accepted independent response leases DF22.

**DeliverySession**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Id | `Guid` | Да | Server SessionId allocated outside gate | ID/content hash not authentication |
| CredentialId | `long` | Да | Selected VRS credential stable ID | Pins only selected credential, no staff rights/auto fallback |
| LicenseId | `long` | Да | Selected verified license | Deterministic tie min LicenseId then CredentialId |
| GrantPeriodId | `Guid` | Да | Own immutable F12 binding ID | Fixed selected period/agreement basis |
| SourcePeriodId | `Guid` | Да | Original selected source period | Renewal current-period mismatch needs new admission |
| VariantId | `Guid` | Да | Pinned scope | Never client-selected revision |
| RevisionId | `Guid` | Да | Exact pinned whole snapshot | R1 never silently replaced R2 |
| ManifestHash | `CanonicalDigest` | Да | Pinned integrity metadata | Profile `cm-publication-manifest/1`; not token/auth verifier |
| ManifestHashProfile | `CanonicalProfileId` | Да | Pinned digest interpretation | Copied exact F10 profile |
| AdmittedCredentialSecurityEpoch | `long` | Да | Selected credential security generation | Must match latest known CM epoch; changes need new admission |
| Verifier | `SessionTokenVerifier` | Да | Private irreversible verifier | 32-byte SHA-256; no bearer/raw key in session |
| AdmittedAt | `DateTimeOffset` | Да | Trusted final guarded admission UTC | Absolute lifetime origin |
| ExpiresAt | `DateTimeOffset` | Да | Stored absolute expiry | min admission+TTL, finite Deny cutoff, explicit credential deadline; never app ExpiresAt |
| InvalidatedAt | `DateTimeOffset?` | Условно | First confirmed terminal invalidation | Never cleared/restored; temporary data unknown not terminal revoke |
| InvalidationReason | Уточняется [Q15](../questions/Q15-api-and-source-code.md#delivery-session-credential-и-replay-contracts) | Условно | Typed invalidation/basis-change reason | No hash-based credential/organization scope |
| Version | `int` | Да | Own internal concurrency | No transport Version; state never restores old token |

**SessionTokenVerifier (private immutable value)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Hash | `ReadOnlyMemory<byte>` | Да | SHA-256 of decoded opaque token bytes | Exactly32 bytes; unique persistence; FixedTimeEquals, never API/log |

**SessionCredentialMaterial (ephemeral only)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| TokenBytes | `byte[]` | Да | 32 server CSPRNG bytes | Outside gate; ephemeral, never log/store plaintext |
| Bearer | `string` | Да | Base64Url opaque encoding | Authorization Bearer/HTTPS only, no URL; decoded length32 |
| Verifier | `SessionTokenVerifier` | Да | Private computed SHA-256 | SessionId/ContentHash not substitute |

**CredentialValidationEvidence (immutable Application attempt value)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| CredentialId | `long` | Да | Trusted VRS key identity | Client never supplies verified identity as authority |
| LicenseId | `long` | Да | Trusted linked license | Full requested key set validated |
| SecurityEpoch | `long` | Да | Source-owned security generation | Read validation/audit time does not increment epoch |
| SecurityMeaning | `CredentialSecurityRead` | Да | Immutable valid/revoke/deadline/security-binding facts | Public read-value fields §3.3; no Domain/EF entity |
| ValidatedCallerContext | Уточняется [Q23](../questions/Q23-source-synchronization.md#credential-validation-и-security-epoch) | Да | Source-verified original caller/possession context | Not body ActorId or organization-wide permission |
| RequiredLicenseSourceWatermark | Уточняется [Q23](../questions/Q23-source-synchronization.md#credential-validation-и-security-epoch) | Да | Source hint for needed CM projection completeness | Local needed source facts must be ≥ hint/ready; comparator profile Q23 |
| ValidationStartedMonotonic | `long` | Да | CM timestamp BEFORE VRS HTTP | Age includes HTTP/crypto/keyring/gate wait until final authorization |
| VrsValidatedAt | `DateTimeOffset?` | Нет | Source audit time only | Never CM proof freshness authority |

**CredentialSecurityState (private own Delivery normalized root)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| CredentialId | `long` | Да | Exact security root identity | One per source credential |
| LicenseId | `long` | Да | Security binding | Changed binding changes source security epoch |
| SecurityEpoch | `long` | Да | Latest known authoritative generation | HTTP/broker same order; older cannot overwrite newer |
| IsValid | `bool` | Да | Confirmed source security validity | Revoke/deadline checked independently; no API-audit increment |
| IsRevoked | `bool` | Да | Confirmed credential revoke | Selected credential scope only |
| ValidityDeadlineKnown | `bool` | Да | Explicit deadline/absence proof completeness | Unknown not no-deadline |
| ValidUntil | `DateTimeOffset?` | Условно | Explicit finite source credential deadline | Enforced without expire event; no app-license ExpiresAt |
| Version | `int` | Да | Own internal projection token | Never public read value; same epoch different stable meaning integrity fault |

**DeliverySessionCreationReceipt (immutable original outcome)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| CreationRequestId | `Guid` | Да | Stable SDK logical request persisted before call | Original known ID never becomes fresh create after cleanup |
| SessionId | `Guid` | Да | Original committed session | No new session/TTL extension on same meaning |
| OriginalCallerContext | Уточняется [Q15](../questions/Q15-api-and-source-code.md#delivery-session-credential-и-replay-contracts) | Да | Verified original caller context | Fresh authorization required; other actor no disclosure |
| OriginalMeaning | `SessionCreationMeaning` | Да | Normalized Variant+verified full identity set | No raw key/key hash/epoch/validation time |
| OriginalMetadata | `SessionCreationOriginalMetadata` | Да | Owned immutable original scope/pin/expiry | No Abstractions dependency/secret; mapped to readonly transport values |
| ConfirmedAt | `DateTimeOffset` | Да | Original admission result record time | Original outcome preserved; not response lease clock |

**SessionCreationMeaning (owned immutable value)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| VariantId | `Guid` | Да | Original requested scope | Normalized meaning includes no volatile authority checks |
| Credentials | `IReadOnlyList<VerifiedCredentialIdentity>` | Да | Full trusted verified identity set | Canonical semantic set, no raw key/key hash/security epochs/audit times |

**VerifiedCredentialIdentity (owned immutable value)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| CredentialId | `long` | Да | Source-verified credential identity | Not client asserted |
| LicenseId | `long` | Да | Source-verified license identity | No implicit organization set |

**SessionCreationOriginalMetadata (owned immutable value)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SessionId | `Guid` | Да | Original allocated session | Immutable receipt identity |
| VariantId | `Guid` | Да | Original scope | Never current candidate substitution |
| RevisionId | `Guid` | Да | Original pin | No repin |
| ManifestHash | `CanonicalDigest` | Да | Original integrity result | Not auth; same exact manifest bytes |
| ManifestHashProfile | `CanonicalProfileId` | Да | Original digest interpretation | `cm-publication-manifest/1` |
| AdmittedAt | `DateTimeOffset` | Да | Original admission record time | No replay extension |
| ExpiresAt | `DateTimeOffset` | Да | Original absolute expiry | Replayed unchanged |

**SessionReplayCapsule (private temporary ciphertext root)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| CreationRequestId | `Guid` | Да | Exact receipt link | Own Delivery FK |
| SessionId | `Guid` | Да | Original session purpose binding | Must match receipt/verifier/scope |
| ProtectedToken | `byte[]?` | Условно | ASP.NET DataProtection ciphertext of ONLY token | Explicit short encrypted exception; purpose SessionId+RequestId/version; no plaintext/API/log |
| ReplayUntil | `DateTimeOffset` | Да | Absolute replay bound | configured5min default, bounded by original session eligibility; no extension |
| CapsuleState | `SessionReplayCapsuleState` | Да | Available/Expired/Unrecoverable/Tombstone | Logical private enum; temporary outage not NoReplay; codec Q20 |
| Version | `int` | Да | Own internal cipher cleanup/generation token | Final guarded replay rechecks exact capsule state/version/deadline |

**SessionPinnedDecision (owned immutable Domain value)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Outcome | `SessionPinnedDecisionKind` | Да | Allowed/Denied/NeedsNewAdmission/DataNotReady | Domain no HTTP/foreign reader |
| RevisionId | `Guid` | Да | Original pin being evaluated | Never selects/replaces new revision |
| NextFinitePolicyBoundary | `DateTimeOffset?` | Условно | Next boundary including Freeze cutoff | Lease must not cross; separate from stored session TTL |
| Reason | Уточняется [Q15](../questions/Q15-api-and-source-code.md#delivery-session-credential-и-replay-contracts) | Да | Explained selected-basis result | Unknown temporary, not confirmed revoke |

**ResponseAuthorizationLease (ephemeral Application value/state)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ResponseId | `Guid` | Да | Exact HTTP response lifetime | New range/resume new checkpoint |
| SessionId | `Guid` | Да | Selected immutable basis | No new candidate/auto fallback |
| RevisionId | `Guid` | Да | Exact permitted pin | Membership checked |
| Generation | `long` | Да | Renewal/cancellation fencing | Late renewal never resurrects canceled response |
| LeaseUntilUtc | `DateTimeOffset` | Да | Short bound | ≤ min now+configured5s, session expiry, next finite policy boundary incl Freeze |
| MonotonicDeadline | `long` | Да | Independent local elapsed-time cancellation | Timer runs even renewal waits on DB/gate/HTTP resources |
| State | `ResponseAuthorizationLeaseState` | Да | Active/Cancelled/Completed | Ephemeral logical enum; terminal cancellation, no DB business aggregate |

**SessionInvalidationFact (immutable safe fact)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| FactId | `Guid` | Да | Durable own processing identity | Inbox/idempotency; no raw key/token |
| Scope | Уточняется [Q23](../questions/Q23-source-synchronization.md#credential-validation-и-security-epoch) | Да | Credential/license/Variant grant/exact revision scope | No nonselected/license organization widening |
| CredentialId | `long?` | Условно | Credential security scope | Required credential fact; latest epoch order |
| LicenseId | `long?` | Условно | License/grant scope | Exact selected basis |
| ProductVariantId | `Guid?` | Условно | Grant/revision scope | Per-Variant where applicable |
| RevisionId | `Guid?` | Условно | Exact terminal ban | No active-pointer prerequisite |
| SecurityEpoch | `long?` | Условно | Source credential generation | Source security change+outbox atomic; commercial correction not epoch change |
| SourceVersion | Уточняется [Q23](../questions/Q23-source-synchronization.md) | Условно | Other source ordering | Old facts cannot restore terminal session |

Session/token/security records are private. Public ports project immutable read values without Version/EF entities (§3.3); Application maps to owned Domain values. Raw keys only creation/recovery, bearer raw only response/ephemeral memory except explicitly encrypted short replay capsule.

**SchemaRepresentationIdentity**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SchemaId | `Guid` | Да | Logical schema | Immutable tuple identity; lowercase D wire |
| SchemaVersionId | `Guid` | Да | Exact structure | Multiple structures per instance allowed; lowercase D wire |
| EntityInstanceId | `Guid` | Да | Logical instance | Key not substitute; lowercase D wire |

**InstalledInventoryEntry**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| schemaId | `Guid` | Да | Logical schema | exact lowerD wire |
| schemaVersionId | `Guid` | Да | Exact schema version | belongs schema; no latest |
| entityInstanceId | `Guid` | Да | Logical instance representation | identity scoped to schema/version |
| entityInstanceVersionId | `Guid` | Да | Actually installed immutable version | input untrusted, no installation proof |
| instanceKey | `string` | Да | Frozen installed alias | exact actual lookup metadata, not desired/current alias |
| contentHash | `CanonicalDigest` | Да | Declared installed digest | wire64lowerhex, paired contentHashProfile |
| contentHashProfile | `CanonicalProfileId` | Да | Actual value interpretation | wire ordinal string, known supported; not group digest |

Inventory neutral parameter/read contract и serialized item принадлежат F13; representation identity получается из первых трёх fields (SchemaRepresentationIdentity выше), JSON не содержит вложенный representation object. DTO direct/generated mapping не Domain dependency. §3.3 Start использует этот logical shape без второго определения fields.


**ExactSchemaSelection**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SchemaId | `Guid` | Да | Selected logical schema | wire schemaId; exact pair; full mandatory list |
| SchemaVersionId | `Guid` | Да | Selected structure | wire schemaVersionId; exact Published owned version, не latest |

**SynchronizationPlan**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SynchronizationId | `Guid` | Да | SDK-generated operation identity | Accepted exception to server-generated IDs; not MVP credential |
| CreatedAt | `DateTimeOffset` | Да | Server reservation instant copied into ready plan | Absolute TTL origin, includes preparation; not client clock |
| ExpiresAt | `DateTimeOffset` | Да | Plan retention deadline | CreatedAt + configured TTL default1h; stream does not extend |
| Selection | `IReadOnlyList<ExactSchemaSelection>` | Да | Original full exact selection | non-null; immutable; explicit empty valid |
| InventoryFingerprint | `CanonicalDigest` | Да | Original actual inventory identity | `cm-installed-inventory/1`; own commits do not mutate original request |
| InventoryFingerprintProfile | `CanonicalProfileId` | Да | Inventory digest interpretation | `cm-installed-inventory/1` |
| CanonicalPlan | `CanonicalRepresentation` из F03 | Да | Immutable pinned targets/removals/original semantic request binding | `cm-sync-plan/1`; no random ID/time/progress/receipt inputs |
| OriginalRequestFingerprint | `CanonicalDigest` | Да | Original request-variant canonical digest | `cm-sync-plan/1`; no mutation after own commits |
| OriginalRequestProfile | `CanonicalProfileId` | Да | Original request variant interpretation | `cm-sync-plan/1`; literal kind=request |
| OriginalSemanticInput | `SynchronizationRequestCanonicalInput` | Да | Retained normalized initial request | Exact replay/collision comparison, not live recomputation |
| CanonicalPlanInput | `SynchronizationPlanCanonicalInput` | Да | Full pinned semantic plan input | Includes all target groups/membership incl unchanged; independent of changed-only execution Groups/Start summary; writer Q19 layout unchanged |
| PlanId | `Guid` | Да | Server-generated immutable plan binding | Separate from canonical plan hash/SDK synchronizationId/execution lease; fixed per retained plan |
| Groups | `IReadOnlyList<SchemaSynchronizationGroup>` | Да | Changed exact group targets only | unchanged omitted; immutable sorted schemaId/schemaVersionId; application cache, not SQL aggregate |

**SchemaSynchronizationGroup**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SchemaId | `Guid` | Да | Logical group identity | Atomic client result unit |
| SchemaVersionId | `Guid` | Да | Exact structure | Including deselected-group removals |
| TargetGroupHash | `CanonicalDigest` | Да | Full sorted target membership integrity | Exact schema context + instance identities/versions/Keys/ContentHashes; not values-only ContentHash |
| TargetGroupHashProfile | `CanonicalProfileId` | Да | Target group digest interpretation | `cm-target-group/1`; verify after all upserts/removals |
| BaseGroupHash | `CanonicalDigest` | Да | Original installed group's full membership | cm-target-group/1 same schema context, derived from original inventory |
| BaseGroupHashProfile | `CanonicalProfileId` | Да | Base digest interpretation | cm-target-group/1; not inventory whole fingerprint |
| TargetEntityCount | `long` | Да | Complete target membership count | ≥0 incl unchanged members |
| AddedCount | `long` | Да | Planned Add count | ≥0 |
| UpdatedCount | `long` | Да | Planned Update count | ≥0 |
| RemovedCount | `long` | Да | Planned Remove count | ≥0; operation totals checked no overflow |
| Operations | Уточняется | Да | Immutable planned diff | All Add/Update/Remove; no partial group apply |
| Internal failure evidence | Уточняется Q33 execution | Условно | Current invalid/incomplete source classification | Not public successful partial-plan errors; preheader error/postheader invalidation0; source/lifecycle guard remains separate from ownership fence |
| Availability status | Уточняется | Уточняется | Authoritative group evidence | Wire profile Q20/Q33 |

**SynchronizationOperation**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Kind | `SynchronizationChangeKind` (target enum) | Да | Immutable planned operation | Add/Update/Remove; wire Upsert add/update or EntityRemove; no unknown fallback |
| Representation identity | `SchemaRepresentationIdentity` | Да | Exact managed tuple | Remove scoped to managed identity |
| Target version | `Guid?` | Условно | Exact pinned EntityInstanceVersion | required Add/Update, absent Remove; no latest-at-fetch |
| Target hash | `CanonicalDigest?` | Условно | Exact content integrity | Required Add/Update; Remove has no pretend target payload |
| Target hash profile | `CanonicalProfileId?` | Условно | Exact digest interpretation | Required Add/Update; actual frozen supported instance profile (/1 legacy, new MVP Offset) |
| Stable lookup metadata | Уточняется | Уточняется | Lookup data | No Key identity substitution; profile Q33 |

**SynchronizationExecutionState — private Application/cache model**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SynchronizationId | `Guid` | Да | Reserved SDK identity | One coordinator per retained operation |
| PlanId | `Guid` | Да | Server identity allocated at reservation | Available through Status even before ready plan; never regenerated |
| CreatedAt / ExpiresAt | `DateTimeOffset` | Да | Reservation UTC and advertised absolute plan deadline | Default TTL1h from reservation; no extension |
| PlanDeadlineTimestamp | `long` | Да | TimeProvider monotonic reservation TTL deadline | UTC rollback cannot prolong retained execution; ExpiresAt is advertised UTC reservation+TTL |
| OriginalSemanticInput | `SynchronizationRequestCanonicalInput` | Да | Original normalized full request | Preserved preparing/stopped; own commits do not change |
| Plan | `SynchronizationPlan?` | Условно | Immutable prepared target | Absent until first preparation completes; no recapture once ready |
| PlanState | `SynchronizationPlanStateKind` | Да | preparing/ready/completed/invalidated/discarded | Completed after all server receipts; not local install proof |
| ExecutionState | `SynchronizationExecutionStateKind` | Да | idle/streaming/awaitingReceipt/stopped | idle means no Data stream; preparation worker is private |
| ExecutionGeneration | `long` | Да | Monotonic late-worker fence | Initial preparation1; increment on new execution/revocation; checked overflow, never wrap |
| Owner | `SynchronizationExecutionOwner?` | Условно | Current preparation/Data worker | Absent if no owner; expired owner can be replaced regardless of old Task |
| OfferedGroups | `IReadOnlySet<Guid>` | Да | Exact group boundaries offered under current fence | Registered before GroupCompleted send; retained plan fact, not installation |
| Receipts | `IReadOnlyList<SynchronizationGroupCommitResult>` | Да | Accepted exact group results | Plan facts; ordered by plan; identical replay original ACK |
| ControlReceipts | `IReadOnlyList<SynchronizationControlReceipt>` | Да | Bounded original control results | Action/plan/generation/meaning; evicted replay cannot bypass stale fence |

**SynchronizationExecutionOwner — private Application/cache model**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| WorkerId | `Guid` | Да | Private execution identity | Never public credential |
| Generation | `long` | Да | Captured current execution generation | Must match coordinator before each mutation/new record/renewal |
| Kind | `SynchronizationWorkerKind` | Да | Preparation or Data | No second live competing owner |
| LeaseDeadline | `long` | Да | TimeProvider monotonic timestamp deadline | Renew only current unexpired owner before absolute plan deadline |
| Cancellation | `CancellationTokenSource` | Да | Worker lifetime cancellation | Revocation/deadline signals outside guard; finally releases own resources only |

**SynchronizationControlReceipt — private Application/cache model**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Action | `SynchronizationControlAction` | Да | Resume/Stop/Discard discriminator | Part of idempotency key |
| PlanId | `Guid` | Да | Reserved immutable identity | Exact match |
| ExpectedExecutionGeneration | `long` | Да | Original caller fence | Positive integer, no silent fresh-generation retarget |
| Meaning | `SynchronizationControlRequest` / `ResumeSynchronizationRequest` | Да | Immutable normalized original action input | Includes Resume original fingerprint/profile; ordinal exact equality |
| Result | `SynchronizationStatusDto` | Да | Original immutable response snapshot | Identical replay returns this snapshot, never mutates newer state; not fresh Status |

**SynchronizationTransportOptions — private host configuration**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| LeaseDuration / RenewInterval | `TimeSpan` | Да | Worker ownership timing | Positive; renewInterval less than leaseDuration; monotonic scheduler |
| PreparationTimeout / ReceiptWaitTimeout | `TimeSpan` | Да | Finite preparation and next-receipt watchdogs | Timeout interrupts execution, does not fabricate invalid plan |
| WriteBackpressureTimeout | `TimeSpan` | Да | Finite stalled response-write budget | Independent cancellation; no lifetime extension |
| MaxActivePlans / MaxControlReceipts / MaxTombstones | `int` | Да | Bounded memory capacities | Positive; control receipt cap per retained operation, global active-plan/tombstone caps; no arbitrary active eviction; new Start unavailable at capacity |
| MaxRecordBytes / MaxBufferedBytes | `long` | Да | Bounded stream buffering | Positive; validated coherent limits, no whole-dataset buffer |
| TombstoneRetention | `TimeSpan` | Да | Known unavailable evidence retention | Positive; numeric sizing from representative fixtures/load tests |

Application owns these private state models, coordinator and response snapshots; they are not Domain aggregates or SQL entities. Their placement here distinguishes runtime state from immutable targets; Domain does not reference Application.Abstractions. All public model fields are defined once in §3.3.

Snapshots copy all exact content required for MVP runtime reads; schema contract delivery/manifest item shape and retention reference profile Q15/Q19/Q20. F10 immutable revision content/source/hash authority is preserved; a separately encoded Delivery manifest must have explicitly defined identity/mapping, not silently a different “same hash”. Delivery lifecycle/version не runtime DTO field.

Demo client state/staging belongs F19, cache receipts not authoritative installed inventory. Plan start ID is an explicit exception to server-generated creation IDs: SDK chooses accepted operation identity. Public timestamps/plan identity/hash/cursor/receipt поля и absolute1hTTL/noextension/one-process memory выбраны Q33. Lease/fencing/control transitions selected Q33; numeric transport sizing remains configuration; bounded tombstones distinguish known410 from unknown404/restart loss.

**SynchronizationUnavailableTombstone** — private bounded cache evidence, not persistent Domain aggregate/install record.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SynchronizationId | `Guid` | Да | Known unavailable operation | exact retained key, no guessed restart history |
| PlanId | `Guid` | Да | Reserved immutable identity | Present even if no ready plan formed; exact binding |
| Reason | `SynchronizationUnavailableReason` (target enum) | Да | Expired/Discarded evidence | Only known cause may yield410; machine registry common Q15 |
| RetainUntil | `DateTimeOffset` | Да | Tombstone own retention deadline | Configurable bounded retention/count, numeric sizing requires representative measurements; races fenced by coordinator; doesn't extend original plan |
| ControlReceipts | `IReadOnlyList<SynchronizationControlReceipt>` | Да | Bounded original control response snapshots | Matching retained replay can return original result after discard; no payload bytes/key, eventual eviction yields unavailable/stale rules |

After eviction/restart no retained evidence→404, not pretend410. Discard/expiry revokes current generation and cancels its worker; bounded tombstone/control receipts retain known outcome; Status/expiry errors use agreed public distinction. Tombstone holds no full content/private signing key, acknowledged receipt alone not client installed proof.

### 2.2. Методы и переходы

| Тип | Метод / функция | Параметры | Предусловия | Изменение / результат |
| --- | --- | --- | --- | --- |
| Delivery state | ApplyNextCommittedTransition | Exact next source + staged complete snapshot or null | First global gate/fresh source/order/ban checks | Pointer+actual history+Pending evidence+processed outcome/sequence+inbox atomic; one transition |
| Processing receipt | RecordTerminalNotEffective | Exact next source + authoritative terminal restriction | Known ban, not technical missing artifact | Outcome/processed sequence/inbox only; no effective row/pointer change |
| Visibility evidence | ConfirmCommittedObservation | Exact already committed history/full snapshot + primary UTC observation | New own transaction/gate; serial oldest pending, clock/order checks | First durable saved VisibilityConfirmedAt, never either commit time |
| Snapshot | Materialize | Exact F10 items + exact published payloads/schema/assets/hash profile | All dependencies resolved; no partial composition/terminal ban | Complete immutable stored graph |
| Restriction/snapshot | Withdraw | Exact ID+trusted terminal fact/time | Matched identity; replay-safe terminal status | Restriction persists; snapshot denied even if created later |
| Session | Create | Confirmed credential/license/period, exact allowed snapshot/hash, trusted clock/TTL/verifier | F12 eligibility, snapshot non-withdrawn, guarded admission | New pinned session/own initial Version; no client-chosen revision |
| Session | Invalidate | Trusted scoped reason/time/source identity | Confirmed selected security/period/rights refusal; necessary unknown excluded | First terminal InvalidatedAt/reason, no restore/repin/TTL change |
| Credential security | ApplySourceKnowledge | Trusted epoch/stable meaning | First own gate, ordered source facts | Newer state only; same contradictory epoch integrity fault; selected old sessions invalidated |
| Creation receipt/capsule | Confirm / recover | CreationRequestId/trusted original meaning/context/token ciphertext | Atomic admission or final guarded replay | Original result once; no fresh create from known ID/tombstone |
| Pinned evaluator | EvaluatePinnedRevision | Owned fresh selected input | F12 facts complete or unknown classified | Pure original-pin outcome/next boundary; no repin |
| Response lease | Renew / cancel | Same response/session/expected generation/fresh checkpoint | Independent deadline active, current rights | New ephemeral generation or terminal cancellation; no late resurrection |
| DesiredSetBuilder / DiffPlanner | Build | Complete selection/inventory + exact available Published projections | Per-group validity/completeness classified | Pure exact desired groups/operations/errors, no DB/clock/lease |
| Plan store workflow | Start / Resume | ID+original identity or stored context; ownership profile | One active execution same ID; no changed plan replacement | Create once/resume exact plan; recoverable ownership |
| Execution workflow | RecordGroupCommit / Stop / Discard | Exact plan/group/hash or control expected generation | Receipt next unconfirmed offered group, controls current fence/original replay | Idempotent plan receipt; Stop preserves; Discard unfinished attempt only |

No Domain method edits immutable snapshot Items or plan target after creation. Plain publication newer than plan не changes target. Explicit Demo withdrawal is safety invalidation: exact planned fetch rechecks authoritative distribution availability; affected group can't complete as success, fresh operation uses actual installed inventory. Unknown requested schema/group is error, not valid empty desired set.

### 2.3. Инварианты и constraints

1. Demo full selection mandatory/non-null; explicit empty deliberate removal of managed prior groups. Missing/malformed body/read failure can't become empty. Duplicate selections/representation identities invalid. Multiple SchemaVersion of one Schema/Instance coexist.
2. Desired-set first filters exact SchemaVersion/Published/distribution availability, then chooses highest Published VersionNumber per logical instance within that exact version. Draft/new unselected schema version not replacement. Authoritatively confirmed schema/version/instance withdrawal or allowed Demo hard delete excludes corresponding representations and creates scoped Remove in a **new** valid plan. Unknown ID/unreadable source/availability without confirmed withdrawal/deletion evidence is a group error; authoritative negative-state evidence/profile Q20/Q33 must distinguish them.
3. Group diff contains all Add/Update/Remove plus full target identity/hash/end evidence; client applies data/removals/inventory atomically. Принятый record/cursor и конец выдачи группы ещё не local commit; data/removals/inventory/durable receipt фиксируются вместе. Partial result only between whole groups, not partial MVP licensed revision.
4. Plan immutable; resume same ID/target/original fingerprint despite successful own commits changing actual inventory. Another body/selection cannot replace plan under same ID. SDK generates new ID only when safe continuation impossible, not every timeout/409.
5. Concurrent active same-ID Start rejected409; status/previous result and safe Resume not duplicate start. Atomic per-operation coordinator and monotonic lease/generation protocol Q33 selected; late executor not free to mutate new owner's state. Client rejects second high-level invocation same stable store scope across SDK instances before new ID F19; adapter cross-process proof Q29/Q32.
6. Demo bounded one-process memory + absolute created TTL default1h/no extension fixed; active plans not arbitrarily evicted; capacity rejects new Start, expiry revokes owner and leaves bounded tombstone. Cache miss/expiry doesn't delete installed data; new plan derives actual inventory, no unconditional staging carry-over.
7. Stop preserves continuation; Discard unfinished context only; started atomic client commit may finish and be reported before stopping remaining work. Lost receipt ACK doesn't undo local commit; server receipt isn't filesystem proof.
8. Post-success check includes no-change plan; same full selection+actual inventory against current available data → UpToDate/ChangesAvailable. Check failure → client Unknown, no installed rollback/no false UpToDate/no endless auto sync. Start JSON metadata, один sequential streaming HTTP data response, SDK local progress, separate assets; H3 preferred/H2 fallback same operation, no implied H1/transport compatibility proof.
9. MVP one actual effective transition per own commit; full projection/pointer/history/Pending evidence/processed receipt+sequence/inbox atomic. Processed source sequence is separate from actual history and time. Gap12→15 loads13/14 first via public committed history+completeness; bus event is only trigger. Known terminal banned intermediate gets durable NotEffective receipt, no history/pointer change/stall; technical missing artifact not consumed.
12. Conservative cutoff checkpoint is **stored VisibilityConfirmedAt after primary observation of exact already COMMITTED history/full snapshot**, not actual first or second commit timestamp, host UtcNow-before-commit or pg_track_commit_timestamp. Original full commit before cutoff but confirmation after deliberately excluded. Equality excluded; compare stored PostgreSQL UTC precision without second rounding.
13. Confirmation evidence Pending until first durable save; exact stored winner replay preserves outcome/time. Unknown original commit reconciles exact stored receipt/history before observation. Unknown confirmation ACK looks up saved time; absent save means fresh recovery observation/no backdate. Oldest necessary pending confirmed first per Variant; current pointer may already advanced. Clock/order failure marks affected temporal decision DataNotReady, no max clamp.
14. Current committed complete snapshot may serve if remaining rights/restrictions allow even time confirmation pending. Expired Freeze needs complete confirmed REQUIRED cutoff prefix; necessary pending/missing/time integrity unknown is Indeterminate, never skip to older known/non-null. Proven past prefix is not invalidated merely by irrelevant later source lag/pending that cannot backdate. Null row full state; rollback old artifact creates new transition/order/confirmation.
15. Same Variant+PublishingSequence same meaningful payload replays original receipt/time; different meaning integrity conflict. Exact revision ban independent of current/materialization; new admission authoritative owner public ban and protected reads restriction before cache/304/range, unknown not Allowed. In-flight enforcement remains Q16.
10. New session requires authoritative key validation and sufficient current source/grant/binding/effective facts. Invalid key vs valid key with missing projection distinct. One credential license only; multi-license explicit trusted set F12. Ordinary upstream outage after creation doesn't revoke pinned session before TTL.
11. Named CmDeliverySession scheme grants runtime-only scope, no staff/admin permissions; Demo stays anonymous. Bearer = Base64Url of32 CSPRNG bytes, Authorization/HTTPS only/no URL; private unique32-byte SHA-256 verifier and FixedTimeEquals. SessionId/content hash not authentication.
16. CreationRequestId durable before call; full proof set derives normalized Variant+verified credential/license IDs, no raw key/hash/epoch/time fingerprint. Same original meaning returns original session/result/TTL; changed meaning conflict, other actor no disclosure. Short encrypted DataProtection token-only replay capsule is explicit exception, original metadata/tombstone survives cleanup.
17. All crypto/keyring Protect/Unprotect outside global gate. Final own guarded check after them counts complete CM-monotonic age from BEFORE VRS HTTP, including network/crypto/keyring/gate wait. Expired proof revalidates outside gate; VrsValidatedAt audit only.
18. Defaults configurable: absolute session30min, capsule5min, proof max age30sec, response lease max5sec; not measured SLA. Expiry min admission+TTL/finite Deny cutoff/explicit credential deadline, never app ExpiresAt. No sliding/refresh/extension on replay.
19. Source security epoch changes only credential security binding/validity/revoke/restore, not reads/audit/general metadata/period correction. Same per-credential epoch via HTTP+broker; older no overwrite, same epoch conflicting stable meaning integrity failure. Latest CM epoch must match admitted selected epoch; forward even Valid needs new admission, no restore old token.
20. Existing session never repins R1→R2/unpublish/archive/retire; active-updates may still read R1. F12 EvaluatePinnedRevision rechecks selected current facts: expired Freeze pin must equal current eligible confirmed whole frozen candidate, else NeedsNewAdmission; renewal new current period makes old session unusable, not license Revoke. Genuine correction preserves agreement/pin/original TTL if still allowed; denied confirmed invalidation terminal, temporary data unknown not revoke, expansion never restores/extends.
21. Each response/cache304/range/resume gets short own ReadCommitted first-global-gate checkpoint/fresh own+public primary selected basis/epoch/current period/cutoff/pin/membership/ban; release before headers/proxy/bytes. No VRS HTTP on resource/progress checks, no foreign UoW/long stream transaction.
22. Ephemeral lease ≤min now+configured5s/session expiry/next finite policy boundary INCLUDING Freeze. Independent monotonic deadline cancels writes/upstream even renewal blocked; periodic fresh checkpoint can renew before deadline, late renewal cannot resurrect canceled response. Boundary may require NEW HTTP with same still-valid session.
23. Committed CM ban denies new checkpoint; active lease cancels when known or deadline. No zero-latency/physical-network5s SLA, already buffered bytes not recalled; unknown external VRS change before receipt not instant knowledge. After headers transport abort→SDK Unknown, not assumed Revoke/new-plan rebinding; installed data not deleted.

[S21](../standards/S21-aggregate-constraints.md): ProductVariantDeliveryStateConstraints, DeliveryPublicationSnapshotConstraints, RevisionDistributionRestrictionConstraints, DeliverySessionConstraints + shared exact-reference/hash/scope values. Application plan/selection/operation constraints named once; no invented TTL/session length/record cap/retry/lease numbers. Hash/order profile Q19, period/public guard Q12/Q20/Q23.

### 2.4. Domain events

| Fact / contract | Когда | Consumer |
| --- | --- | --- |
| ProductVariantPublishingStateChanged F11 | Committed Publishing transition | Delivery ordered projector/inbox |
| Exact revision withdrawal restriction F10 | Terminal withdrawal incl inactive/unmaterialized | Delivery restriction/snapshot/session guards |
| Confirmed credential/license/grant revoke | Authoritative projected invalidator | Session revocation/admission guard |

**ProductVariantPublishingStateChanged F11**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Exact pointer | Уточняется (nullable) | Нет | Committed target/null | Owner F11; ordered projector |
| Sequence | Уточняется | Да | Per-Variant transition order | Owner F11; contract identity/version profile Q20 |
| Trusted audit | Уточняется | Уточняется | Transition context | Owner F11 |
| Event envelope | Уточняется | Уточняется | Event version/identity for trust/dedup | Q20 |

**Exact revision withdrawal restriction F10**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Exact revision | Уточняется | Да | Terminal banned ID | Owner F10; inactive/unmaterialized included |
| Variant | Уточняется | Да | Artifact owner | Owner F10 |
| Terminal ban identity | Уточняется | Уточняется | Durable restriction | Contract Q20; no late lifting |

**Confirmed credential/license/grant revoke**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Non-secret basis | Уточняется | Уточняется | Relevant invalidation scope | Credential/license/grant distinguished |
| Version | Уточняется | Уточняется | Authoritative source version | Contract Q16/Q20 |
| Identity | Уточняется | Уточняется | Trusted invalidator identity | Contract Q16/Q20 |

Delivery readiness ACK/event if needed and session/audit integration profile Q20/Q17; no Domain event merely for read/logging. Demo streaming data/progress не requires broker/inbox infrastructure; local SDK progress не domain/integration event. Durable MVP inbox and own commit precede ACK; presence in-memory event not durable delivery proof.

## 3. Application.Abstractions

### 3.1. Commands

| Command / use case | Result | Concurrency |
| --- | --- | --- |
| StartSynchronizationCommand | Plan summary/result identity | Atomic same-ID reservation/ownership; not state If-Match |
| ResumeSynchronizationCommand | Exact previous plan/execution result | Conditional lease/owner/generation, not recompute |
| StopSynchronizationCommand / DiscardSynchronizationCommand | Confirmed state/outcome profile | Stop retain/Discard unfinished; late owner guard |
| CommitSynchronizationGroupCommand | Idempotent recorded acknowledgment | Matched plan/receipt/owner protocol Q33 |
| ApplyPublishingTransitionsCommand | Original source-processing receipt/progress | Separate processed sequence/history/time; one real transition per commit |
| ConfirmDeliveryVisibilityCommand | Stored pending/confirmed observation evidence | New own first gate; exact committed identity/serial order; saved time once |
| RecoverDeliveryVisibilityCommand | Reconciled stored outcome/time or fresh confirmation | Reconcile original commit first; never backdate |
| MarkPublicationWithdrawnCommand | Ban processing acknowledgment | Inbox/terminal restriction never cleared |
| CreateDeliverySessionCommand / RecoverDeliverySessionCreationCommand | Original/new session metadata+opaque bearer or typed NoReplay | Full proof/context/meaning then final guarded selected eligibility; no refresh |
| ApplyCredentialSecurityFactCommand | Stored latest normalized security knowledge | Same HTTP/broker epoch, forward only/conflicting same epoch integrity |
| AuthorizeDeliveryResponseCommand / RenewResponseLeaseCommand | Short checkpoint/ephemeral lease or semantic denial | Own first-gate selected pin/security/rights/membership; release before I/O |
| RevokeSessionsCommand | Processing acknowledgment | Own conditional status updates/inbox |

**StartSynchronizationCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SynchronizationId | `Guid` | Да | SDK operation ID | atomic same-ID reservation; not new ID on409 |
| Selection | `IReadOnlyList<ExactSchemaSelection>` | Да | Full exact selected structures | non-null; explicit empty supported; duplicates400 |
| InstalledInventory | `IReadOnlyList<InstalledInventoryEntry>` | Да | Actual full managed inventory | untrusted declaration; non-null; empty valid; duplicates400 |

**ResumeSynchronizationCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SynchronizationId | `Guid` | Да | Route operation | Retained exact context |
| Request | `ResumeSynchronizationRequest` | Да | Immutable identity/original request/caller fence | Result SynchronizationStatusDto; no Data lease reservation |

**StopSynchronizationCommand / DiscardSynchronizationCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SynchronizationId | `Guid` | Да | Route operation | Stop retains plan; Discard abandons unfinished attempt |
| Request | `SynchronizationControlRequest` | Да | Immutable plan/caller fence | Result SynchronizationStatusDto; original replay snapshot |

**CommitSynchronizationGroupCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SynchronizationId | `Guid` | Да | Route operation | cached matched immutable plan |
| PlanId | `Guid` | Да | Body immutable identity | exact match; mismatch409 |
| SchemaVersionId | `Guid` | Да | Route exact group | belongs retained plan; no source latest |
| TargetGroupHash | `CanonicalDigest` | Да | Complete target receipt evidence | paired profile; mismatch409 |
| TargetGroupHashProfile | `CanonicalProfileId` | Да | Target digest interpretation | cm-target-group/1; same retained profile |

Command handler Application возвращает SynchronizationGroupCommitResult; actor/lease/client progression не публичный body этой модели. Internal lifecycle/fencing policy derives from cached server context, receipts are accepted only for next unconfirmed offered group; replay identical ACK independently of stream lease.


**ApplyPublishingTransitionsCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductVariantId | `Guid` | Да | Projector/catch-up scope | Own processed sequence/history/evidence |
| TriggerSequence | `long` | Да | Trusted bus wakeup boundary | Event alone not source history authority; no skip gaps |
| Context | Уточняется [Q20](../questions/Q20-persistence-and-migration.md#delivery-catch-up-и-подтверждение-видимости) | Да | Trusted trigger/inbox context | Source payload revalidated from authoritative history |

**ConfirmDeliveryVisibilityCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductVariantId | `Guid` | Да | Confirmation serialization scope | Oldest necessary pending first |
| PublishingSequence | `long` | Да | Exact original transition | Reconcile already committed outcome/history, not current pointer equality |
| EffectiveHistoryId | `Guid` | Да | Exact own committed history identity | Null transition confirmed by exact null row; no new materialization |

**RecoverDeliveryVisibilityCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductVariantId | `Guid` | Да | Recovery scope | Own primary lookup |
| PublishingSequence | `long` | Да | Original source receipt identity | Original commit unknown →reconcile before attempting observation |
| EffectiveHistoryId | `Guid?` | Условно | Known durable actual transition ID | Loaded from stored outcome if original response lost; never invented |

**MarkPublicationWithdrawnCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Exact revision | Уточняется | Да | Terminal banned identity | Restriction never cleared |
| Variant | Уточняется | Да | Exact owner | Trusted contract |
| Terminal fact | Уточняется | Да | Authoritative withdrawal | Inbox/terminal guard Q20 |

**CreateDeliverySessionCommand / RecoverDeliverySessionCreationCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Request | `CreateDeliverySessionRequest` | Да | RequestId+Variant+full raw credential set | Same request on inconclusive recovery; no client verified-ID authority |
| Context | Уточняется [Q15](../questions/Q15-api-and-source-code.md#delivery-session-credential-и-replay-contracts) | Да | Trusted transport/cancellation/provider context | Server establishes caller meaning from complete validated proofs |

**ApplyCredentialSecurityFactCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Security | `CredentialSecurityRead` | Да | Trusted normalized security input | HTTP proof or source broker fact, not public client body |
| FactIdentity | Уточняется [Q23](../questions/Q23-source-synchronization.md#credential-validation-и-security-epoch) | Да | Durable source/HTTP processing trace | Same epoch ordering, no raw key |

**AuthorizeDeliveryResponseCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SessionId | `Guid` | Да | Runtime-authenticated exact session | Bearer verifier matched, not merely route ID |
| ResponseId | `Guid` | Да | Server HTTP lifetime identity | No SQL business lease aggregate |
| ResourceKind | Уточняется [Q15](../questions/Q15-api-and-source-code.md#delivery-session-credential-и-replay-contracts) | Да | Manifest/item/asset/progress context | Exact endpoint/membership semantics |
| ResourceId | `Guid?` | Условно | Requested exact pinned item/asset | Outsnapshot404; no arbitrary ID bypass |

**RenewResponseLeaseCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SessionId | `Guid` | Да | Original selected session | No new candidate |
| ResponseId | `Guid` | Да | Exact active HTTP response | Reuse original resource scope |
| ExpectedGeneration | `long` | Да | Ephemeral owner renewal fence | Canceled/expired response cannot resurrect |

**RevokeSessionsCommand**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Invalidator basis | Уточняется | Да | Trusted credential/license/grant | Scope-specific non-secret identity |
| Context | Уточняется | Уточняется | Authoritative invalidation context | Q16/Q20; own conditional status/inbox |

Trusted IDs/time/correlation/source facts supplied via execution context, raw key command ephemeral only. No runtime edit current Delivery state/own Version DTO. Binding use case F12 referenced not redefined; multi-license creation input is trusted contract Q16, not arbitrary client LicenseIds.

### 3.2. Queries

| Query | Result / handler placement |
| --- | --- |
| GetSynchronizationQuery | Plan/status profile; cache read adapter |
| GetSynchronizationDataQuery | Один ordered record stream pinned plan; cache + public ContentModeling read adapter |
| CheckSynchronizationFreshnessQuery | Current UpToDate/ChangesAvailable and changed group/error profile |
| GetPublishingTransitionsAfterQuery | Public Publishing ordered committed history + completeness |
| ReadCompleteDeliveryHistoryQuery | CompleteDeliveryHistoryRead; own Persistence |
| GetDeliveryTransitionOutcomeQuery | Exact stored processing/confirmation outcome; own primary Persistence |
| GetDeliveryManifestQuery / GetDeliveryItemQuery / GetDeliveryAssetReadAccessQuery | Owned snapshot/read projection; asset query only eligibility/access facts before YARP, no bytes |

**GetSynchronizationQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ID | `Guid` | Да | Exact cached operation | Observer; result SynchronizationStatusDto; no ownership/recompute |

**GetSynchronizationDataQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SynchronizationId | `Guid` | Да | Exact cached immutable plan | Не новый Start; lookup/read context Q33 |
| PlanId | `Guid` | Да | Required query immutable plan context | Match cached operation; wrong binding409 |
| Cursor | `string?` | Нет | Opaque after-record checkpoint | Q33/F13 binary codec; absent starts1; same replay dedup/conflict error; not install proof |

Query возвращает логическую incremental последовательность records, не весь materialized payload. Exact CLR sequence/result shape и wire envelope — Q33; network fragmentation не создаёт query per chunk.

**CheckSynchronizationFreshnessQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Request | `SynchronizationFreshnessRequest` | Да | Full selection/actual installed inventory | Result SynchronizationFreshnessResult; no new plan/session |

**GetPublishingTransitionsAfterQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductVariantId | `Guid` | Да | History scope | Primary public ordered committed Publishing reader |
| AfterPublishingSequence | `long` | Да | Durable processed source boundary | Completeness/high watermark required, gaps not silently skipped |

**ReadCompleteDeliveryHistoryQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductVariantId | `Guid` | Да | Exact current/history scope | Own coherent history/state/evidence read |
| Cutoff | `DateTimeOffset?` | Нет | Required temporal decision boundary, if Freeze | Strict stored checkpoint; no second rounding |
| RequiredDecisionScope | Уточняется [Q15](../questions/Q15-api-and-source-code.md#delivery-visibility-contracts) | Да | Current selection, Freeze cutoff prefix, or staff readiness purpose/range | Server-derived needed scope; carrier Q15; no blanket veto from every later gap/pending row |

**GetDeliveryTransitionOutcomeQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductVariantId | `Guid` | Да | Exact outcome scope | Own primary lookup, not cached tracker |
| PublishingSequence | `long` | Да | Exact original source transition | Stored outcome/history/evidence; unknown original commit reconciles before confirmation |

**GetDeliveryManifestQuery / GetDeliveryItemQuery / GetDeliveryAssetReadAccessQuery**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Session context | Уточняется | Да | Trusted exact protected basis | Authorization before 304/range/bytes |
| Requested exact resource ID | Уточняется | Условно | Item/asset identity | Where resource-specific; pinned membership, not arbitrary ID |

[S20](../standards/S20-cqrs.md): commands in Delivery.Application (Demo branch free of licensing dependencies). SQL query handlers Delivery.EntityFramework.Persistence; cache/public-content queries in owning access adapters/read handlers, not unnecessary EF persistence of Demo plan. A pure freshness diff doesn't insert new plan or session. Status read не execution owner acquisition; data stream acquisition/reconnect и fencing — Q33.

### 3.3. DTO и parameter models

| Model | Назначение |
| --- | --- |
| StartSynchronizationRequest | Поля в таблице модели ниже; незакрытый wire profile остаётся Уточняется |
| SynchronizationPlanDto | Поля в таблице модели ниже; незакрытый wire profile остаётся Уточняется |
| SynchronizationEntityUpsertRecord / SynchronizationEntityRemoveRecord | EntityUpsert / EntityRemove variants accepted; exact JSON/member carrier materialized by shared fixtures Q33 |
| SynchronizationGroupSummary | Компактные plan metadata; control framing ссылается на plan/group context |
| SynchronizationGroupCommitRequest | Поля в таблице модели ниже; незакрытый wire profile остаётся Уточняется |
| SynchronizationFreshnessRequest / Result | Full selection/actual inventory and typed outcome/checkedAt/changedGroups, model tables below |
| CreateDeliverySessionRequest | Поля в таблице модели ниже; незакрытый wire profile остаётся Уточняется |
| CreateDeliverySessionResponse | Поля в таблице модели ниже; незакрытый wire profile остаётся Уточняется |
| DeliveryManifestDto / DeliveryItemDto | Поля в таблице модели ниже; незакрытый wire profile остаётся Уточняется |

Public wire v1 определён [Q33](../questions/Q33-sync-resume-transport.md#public-wire-contract-v1). CLR target names ниже не обнаруженные source classes. Таблицы — единственный владелец DTO fields; camelCase names нормативны. Guid/hash/profile/Int64 имеют explicit converters без hidden numeric/string coercion. JSON control success ApiResponse<T>; errors common Q15; NDJSON raw records.

**StartSynchronizationRequest**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| selection | `IReadOnlyList<ExactSchemaSelection>` | Да | Полный выбранный exact set | array, non-null; empty valid; duplicate normalized pair invalid; selection fields §2.1 |
| installedInventory | `IReadOnlyList<InstalledInventoryEntry>` | Да | Actual complete managed inventory | array, non-null; empty valid; flattened wire fields §2.1; duplicate representation identity invalid |

**SynchronizationPlanDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| synchronizationId | `Guid` | Да | SDK operation identity | lowercase D wire; nonempty; exact identity |
| planId | `Guid` | Да | Server immutable plan identity | lowercase D wire; nonempty; exact identity |
| protocolVersion | `int` | Да | Protocol version | literal 1; не API/package/hash profile version |
| planHash | `CanonicalDigest` | Да | Immutable canonical plan integrity | wire string64lowerhex; paired profile, не authorization |
| planHashProfile | `CanonicalProfileId` | Да | cm-sync-plan/1 interpretation | wire ordinal string; actual supported profile; unknown rejects |
| expiresAt | `DateTimeOffset` | Да | Absolute deadline | UTC invariant 7digitsZ; created TTL default1h/no extension |
| groups | `IReadOnlyList<SynchronizationGroupSummary>` | Да | Changed groups only | array non-null; nochange []; canonical schemaId/schemaVersionId ordinal order; no successful partial-error plan |

**SynchronizationGroupSummary**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| schemaId | `Guid` | Да | Logical group schema | lowercase D wire; nonempty; exact identity |
| schemaVersionId | `Guid` | Да | Exact atomic target/removal scope | lowercase D wire; nonempty; exact identity |
| targetGroupHash | `CanonicalDigest` | Да | Complete post-operations group incl unchanged members | wire string64lowerhex; paired profile, не authorization |
| targetGroupHashProfile | `CanonicalProfileId` | Да | cm-target-group/1 | wire ordinal string; actual supported profile; unknown rejects |
| baseGroupHash | `CanonicalDigest` | Да | Original declared installed group | wire string64lowerhex; paired profile, не authorization |
| baseGroupHashProfile | `CanonicalProfileId` | Да | cm-target-group/1 same schema context | wire ordinal string; actual supported profile; unknown rejects |
| targetEntityCount | `long` | Да | Complete resulting membership count | JSON integer ≥0; no double/overflow; compare exact plan |
| addedCount | `long` | Да | Planned Add count | JSON integer ≥0; no double/overflow; compare exact plan |
| updatedCount | `long` | Да | Planned Update count | JSON integer ≥0; no double/overflow; compare exact plan |
| removedCount | `long` | Да | Planned Remove count | JSON integer ≥0; no double/overflow; compare exact plan |

**SynchronizationRecordEnvelope**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| protocolVersion | `int` | Да | Protocol version | literal 1; unknown error |
| synchronizationId | `Guid` | Да | Matched SDK operation | lowercase D wire; nonempty; exact identity |
| planId | `Guid` | Да | Matched immutable plan | lowercase D wire; nonempty; exact identity |
| recordSequence | `long` | Да | Planned position / terminal control marker | normal stable 1..N; operationInvalidated only0 outside planned data log |
| kind | `SynchronizationRecordKind` (target enum) | Да | Explicit discriminated variant | wire groupStarted/entityUpsert/entityRemove/groupCompleted/operationCompleted/operationInvalidated; unknown error |
| cursor | `string` | Условно | Server-issued candidate checkpoint | required normal records, absent invalidation; SDK opaque; durable only after record+cursor atomic stage |
| schemaId | `Guid` | Условно | Group schema | required group variants; operationCompleted absent; invalidation both schema IDs optional together |
| schemaVersionId | `Guid` | Условно | Exact group version | same context rule; equals Start group, no latest fallback |
| payload | `SynchronizationRecordPayload` (target known union) | Да | Typed variant payload | JSON object; no null/generic fallback; required fields variant tables below |

GroupStarted payload переиспользует summary fields hash/profile/counts из SynchronizationGroupSummary, schema IDs только envelope. Это projection same model без второй копии правил. Payload operationCompleted — явный empty object `{}`, не null/отсутствие; состояние завершения доказывается marker+receipts+correct HTTP end.

**SynchronizationEntityUpsertRecord**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| changeKind | `SynchronizationUpsertKind` (target enum) | Да | Planned Add/Update | wire add/update; agrees immutable plan |
| entityInstanceId | `Guid` | Да | Logical instance representation | lowercase D wire; nonempty; exact identity |
| entityInstanceVersionId | `Guid` | Да | Exact pinned Published version | lowercase D wire; nonempty; exact identity |
| instanceKey | `string` | Да | Frozen lookup alias | exact immutable target metadata, no live rename |
| contentHash | `CanonicalDigest` | Да | Digest exact restored canonical UTF8 | wire string64lowerhex; paired profile, не authorization |
| contentHashProfile | `CanonicalProfileId` | Да | Actual frozen instance interpretation | wire ordinal string; actual supported profile; unknown rejects |
| contentJson | `string` | Да | Exact canonical JSON as JSON string | unescape→strict UTF8 bytes→digest verify→typed parse; no nested object/base64/parse-reserialize; canonical bytes contain no framing LF |

**SynchronizationEntityRemoveRecord**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| entityInstanceId | `Guid` | Да | Managed representation removed within envelope group | lowercase D wire; nonempty; exact identity |

**SynchronizationGroupCompletedPayload**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| targetGroupHash | `CanonicalDigest` | Да | Complete target integrity | wire string64lowerhex; paired profile, не authorization |
| targetGroupHashProfile | `CanonicalProfileId` | Да | cm-target-group/1 | wire ordinal string; actual supported profile; unknown rejects |
| addedCount | `long` | Да | Actually emitted unique Add operations | JSON integer ≥0; no double/overflow; compare exact plan |
| updatedCount | `long` | Да | Actually emitted unique Update operations | JSON integer ≥0; no double/overflow; compare exact plan |
| removedCount | `long` | Да | Actually emitted unique Remove operations | JSON integer ≥0; no double/overflow; compare exact plan |

**SynchronizationInvalidatedPayload**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| reasonCode | `string` | Да | Stable safe machine reason | owner registry Q15/Q33 literal mapping engineering; not free stack trace; unknown reason still terminal known invalidation |
| message | `string` | Да | Safe human explanation | not machine contract; no raw content/secrets |

**SynchronizationGroupCommitRequest**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| planId | `Guid` | Да | Installed-result immutable plan | lowercase D wire; nonempty; exact identity |
| targetGroupHash | `CanonicalDigest` | Да | Claimed complete local result | wire string64lowerhex; paired profile, не authorization |
| targetGroupHashProfile | `CanonicalProfileId` | Да | Paired target profile | wire ordinal string; actual supported profile; unknown rejects |

**SynchronizationGroupCommitResult**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| planId | `Guid` | Да | Acknowledged plan | lowercase D wire; nonempty; exact identity |
| schemaVersionId | `Guid` | Да | Acknowledged exact route group | lowercase D wire; nonempty; exact identity |
| targetGroupHash | `CanonicalDigest` | Да | Original accepted target evidence | wire string64lowerhex; paired profile, не authorization |
| targetGroupHashProfile | `CanonicalProfileId` | Да | Original accepted interpretation | wire ordinal string; actual supported profile; unknown rejects |
| acknowledged | `bool` | Да | Server receipt accepted | literal true on success; not filesystem proof; identical replay same result |

**SynchronizationStatusDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| synchronizationId | `Guid` | Да | Exact observed operation | lowercase D wire; nonempty; exact identity |
| planId | `Guid` | Да | Reserved immutable identity | Allocated at reservation; available even preparing |
| executionGeneration | `long` | Да | Current execution fence | Positive JSON Int64; no double/overflow; control caller captures it |
| planState | `SynchronizationPlanStateKind` (target enum) | Да | Separate plan state | wire preparing/ready/completed/invalidated/discarded |
| executionState | `SynchronizationExecutionStateKind` (target enum) | Да | Separate execution state | wire idle/streaming/awaitingReceipt/stopped; preparing idle has private worker |
| plan | `SynchronizationPlanDto?` | Условно | Retained immutable plan metadata | may be absent preparing; ready materialized plan same exact Start identity/summary |
| receipts | `IReadOnlyList<SynchronizationGroupCommitResult>` | Да | Accepted group receipts | non-null []; exact immutable receipt evidence; completed means all server receipts, not installed proof |

Commit schemaVersionId берётся из route; duplicate body copy/progression/lease credential не добавляется. Receipt body excludes execution generation; controls use expectedExecutionGeneration and Data supplies X-CM-Sync-Execution-Generation. Start/Status/Commit share public wire; preparing/no-ready-plan нельзя выдавать как nochange success. Same-group receipts retained until operation retention; Completed data terminal remains replayable until plan expiry.

**SynchronizationCursorPayload**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| version | `byte` | Да | Token codec version | literal1; byte offset0 |
| synchronizationId | `Guid` | Да | Matched operation binding | 16 RFC UUID/network-order bytes offsets1..16; not mixed Guid.ToByteArray default |
| planId | `Guid` | Да | Matched immutable plan | 16 network-order bytes offsets17..32 |
| afterSequence | `long` | Да | Last checkpoint candidate | 8 signed Int64 big-endian offsets33..40; 1..N |
| mac | `ReadOnlyMemory<byte>` | Да | HMAC-SHA256 full tag | 32 bytes offsets41..72 over first41; not content hash/profile/auth token |

Binary token =73bytes; canonical Base64URL without padding=98chars. Server rejects invalid alphabet/length/noncanonical encoding/version/MAC/identity/sequence range. Secret random32bytes process-local Demo, singleton codec lifetime same cache; no SDK verification key. Cursor excludes execution generation so reconnect same immutable plan works; late writer checks captured generation against coordinator. Omitted query cursor starts at sequence1; no fabricated seq0 normal checkpoint. Token validity bounded by cached plan deadline. [Literal vectors](../sync-wire-v1-vectors.md) use public fixture-only key; production key is never documentation data.


**SynchronizationControlRequest**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| planId | `Guid` | Да | Reserved plan binding | Lowercase D nonempty; available while preparing |
| expectedExecutionGeneration | `long` | Да | Caller-observed execution fence | JSON integer greater than0; stale409; no automatic retarget |

**ResumeSynchronizationRequest**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Control | `SynchronizationControlRequest` | Да | Shared control fields | Flattened planId/expectedExecutionGeneration in JSON; no nested control member |
| originalRequestFingerprint | `CanonicalDigest` | Да | Original normalized request identity | 64lowerhex; compare retained original input, not changed actual inventory |
| originalRequestProfile | `CanonicalProfileId` | Да | Original request interpretation | cm-sync-plan/1 exact ordinal |

**SynchronizationFreshnessRequest**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| selection | `IReadOnlyList<ExactSchemaSelection>` | Да | Full exact selection | Non-null; explicit[] valid; same validation as Start |
| installedInventory | `IReadOnlyList<InstalledInventoryEntry>` | Да | Actual installed managed state | Non-null; no plan progress substitution; identity type defined above |

**SynchronizationFreshnessResult**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| outcome | `SynchronizationFreshnessOutcome` | Да | Current comparison outcome | Wire upToDate/changesAvailable; Unknown SDK-only on failed check |
| checkedAt | `DateTimeOffset` | Да | Server successful comparison observation UTC | Seven fractional digits Z; point-in-time evidence, no permanent guarantee |
| changedGroups | `IReadOnlyList<ExactSchemaSelection>` | Да | Changed exact schemaId/schemaVersionId pairs | Non-null; sorted pair ordinal canonical GUID; [] for upToDate; includes removal scope |

**CompleteDeliveryHistoryRead**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| ProductVariantId | `Guid` | Да | Exact history/evaluation scope | Coherent owner read |
| Entries | `IReadOnlyList<DeliveryEffectiveHistoryEntryRead>` | Да | Immutable nontracking actual-history read values incl null | No Domain/EF entity; empty only proven complete range |
| Visibility | `IReadOnlyList<DeliveryVisibilityEvidenceRead>` | Да | Immutable nontracking coherent evidence read values | No internal Version/method/EF tracking; not guessed timestamp |
| LastProcessedPublishingSequence | `long` | Да | Durable source processing progress | Not every processed source outcome is effective |
| PublishingHighWatermark | `long?` | Условно | Public authoritative committed source history boundary | Missing/unavailable completeness evidence not no-events |
| SourceRangeComplete | `bool` | Да | Completeness of REQUIRED decision/range, not whole live high watermark | Current may serve during later catch-up gap; proven past Freeze prefix remains valid if later unprocessed source cannot backdate into it |
| HasNecessaryPendingConfirmation | `bool` | Да | Unconfirmed evidence necessary for requested purpose/cutoff prefix | Later irrelevant pending is not blanket veto; necessary unknown cannot be skipped |
| TemporalOrderValid | `bool` | Да | Observed clock/order integrity | False → affected temporal decision DataNotReady |
| CurrentEffectiveHistoryId | `Guid?` | Нет | Exact committed current transition | Distinguishes never-effective from known-null history |
| CurrentCommittedSnapshotComplete | `bool` | Да | Full current pointer/snapshot readiness | Current committed snapshot may serve if otherwise permitted even time confirmation pending |

Completeness/pending flags are scoped to RequiredDecisionScope/Cutoff. A proven complete confirmed past prefix can remain usable when later unprocessed source cannot backdate; mere high-watermark lag/later pending does not make all decisions NotReady. Necessary missing history/confirmation is Indeterminate. Staff still sees overall gap/pending state independently.

Public CompleteDeliveryHistoryRead belongs Application.Abstractions; constituent immutable read values defined once §3.3. Persistence projects nontracking values, never exports Domain/EF records/evidence Version. Application maps to owned immutable Domain temporal values below; Domain does not depend on Application.Abstractions. Wire/physical order allocation Q15/Q20; semantics decided.

**DeliveryEffectiveHistoryEntryRead (immutable Application.Abstractions read value)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| EffectiveHistoryId | `Guid` | Да | Exact committed history identity | Read projection, no methods |
| ProductVariantId | `Guid` | Да | Exact read scope | Coherent carrier |
| PublishingSequence | `long` | Да | Source identity | Separate from processed progress/time |
| EffectiveOrder | `long` | Да | Actual effective order | Rollback/candidate order, not age |
| RevisionId | `Guid?` | Нет | Actual target/null | Null is full state |
| PublishingActivatedAt | `DateTimeOffset` | Да | Producer audit | Never cutoff checkpoint |
| Reason | Уточняется [Q15](../questions/Q15-api-and-source-code.md#delivery-visibility-contracts) | Да | Trusted read code/caption | No Domain type dependency; codec Q15 |
| CorrelationId | `Guid?` | Нет | Safe trace | Exposure Q15 |

**DeliveryVisibilityEvidenceRead (immutable Application.Abstractions read value)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| EffectiveHistoryId | `Guid` | Да | Matching own history identity | Read-only |
| ProductVariantId | `Guid` | Да | Scope | Coherent carrier |
| PublishingSequence | `long` | Да | Source identity | Not current-pointer match |
| ConfirmationKnown | `bool` | Да | First evidence durably saved | Pending/missing false, no internal Version |
| VisibilityConfirmedAt | `DateTimeOffset?` | Условно | First primary observation time | Required known; not commit/derived now |
| ClockOrderValid | `bool` | Да | Temporal integrity | False temporal DataNotReady |

Read carrier/types are defined here once, exact JSON/status/exposure Q15; no new route. Application maps to owned Domain values for pure evaluator.


**CreateDeliverySessionRequest**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| CreationRequestId | `Guid` | Да | SDK durable logical identity | Saved before first POST; same ID on inconclusive retry, no raw key/hash SDK default storage |
| ActivationKeys | `IReadOnlyList<string>` | Да | Explicit FULL raw credential set, creation/recovery only | Every key validated; server derives verified IDs, no URL/log/persistence |
| ProductVariantId | `Guid` | Да | Requested Variant | No client-selected revision/verified IDs/license expansion |

**CreateDeliverySessionResponse**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SessionId | `Guid` | Да | Created/original session identity | ID not authentication |
| Session credential | `string` | Да | Base64Url opaque bearer from32 CSPRNG bytes | Authorization/HTTPS only; raw token secret, encrypted replay exception only |
| VariantId | Уточняется | Уточняется | Pinned scope | Wire profile Q15/Q16 |
| RevisionId | Уточняется | Уточняется | Allowed whole snapshot | No implicit repin/fallback |
| ManifestHash | `CanonicalDigest` | Да | Pinned representation hash | Exact stored F10 manifest |
| ManifestHashProfile | `CanonicalProfileId` | Да | Pinned representation interpretation | `cm-publication-manifest/1` |
| ExpiresAt | `DateTimeOffset` | Да | Original absolute expiry | min admission+configured30min, finite Deny cutoff, explicit credential deadline; no sliding |
| CreationRequestId | `Guid` | Да | Original logical execution | Original result replay, no new session/TTL |

**CredentialSecurityRead (immutable nontracking Application.Abstractions value)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| CredentialId | `long` | Да | Trusted exact credential | Source stable ID alone does not prove target epoch API exists |
| LicenseId | `long` | Да | Security license binding | Authoritative |
| SecurityEpoch | `long` | Да | Same source generation via HTTP/broker | Forward security change new admission; older no overwrite |
| IsValid | `bool` | Да | Source security validity | No rights for staff/admin |
| IsRevoked | `bool` | Да | Credential scope revoke | Selected basis only |
| ValidityDeadlineKnown | `bool` | Да | Deadline/explicit absence completeness | Missing unknown |
| ValidUntil | `DateTimeOffset?` | Условно | Finite source credential deadline | Enforce without event, not application ExpiresAt |

**DeliverySessionMetadataRead (immutable nontracking read value)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| SessionId | `Guid` | Да | Original scope identity | No authentication by ID |
| VariantId | `Guid` | Да | Pinned scope | Not staff rights |
| RevisionId | `Guid` | Да | Exact pin | No repin |
| ManifestHash | `CanonicalDigest` | Да | Integrity metadata | Not verifier/auth |
| ManifestHashProfile | `CanonicalProfileId` | Да | Manifest digest interpretation | `cm-publication-manifest/1` |
| AdmittedAt | `DateTimeOffset` | Да | Original UTC record time | Read only |
| ExpiresAt | `DateTimeOffset` | Да | Original absolute deadline | No refresh/replay extension |

**ResponseCheckpointRead (immutable read value)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Session | `DeliverySessionMetadataRead` | Да | Authorized original selected pin | No Domain/EF entities/internal Version |
| Decision | `SessionPinnedDecisionKind` | Да | Allowed/Denied/NeedsNewAdmission/DataNotReady semantic result | Exact wire Q15; public mapper, no Domain instance |
| LeaseUntilUtc | `DateTimeOffset?` | Условно | Permitted response bound | Only Allowed; no persisted bearer/crypto verifier |
| NextFinitePolicyBoundary | `DateTimeOffset?` | Условно | Boundary incl Freeze cutoff | Response may need NEW HTTP, not token refresh |
| Reason | Уточняется [Q15](../questions/Q15-api-and-source-code.md#delivery-session-credential-и-replay-contracts) | Да | Safe semantic reason | No raw key/token/ciphertext/keyring details |

Public ports return immutable values, no internal Version/Domain entities. Private auth-store verifier projection is server-only, never transport DTO. Application maps reads to owned Domain inputs; Domain never references Abstractions.


**DeliveryManifestDto / DeliveryItemDto**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Pinned identity | Уточняется | Уточняется | Exact pinned snapshot/item references | Formal shape Q19/Q15; protected membership |
| Schema | Уточняется | Уточняется | Exact structure identity | Q19/Q15; no latest substitution |
| Version | Уточняется | Уточняется | Exact payload/business version | Q19/Q15; не internal int concurrency Version |
| Content | Уточняется | Уточняется | Exact pinned payload | Q19/Q15 |
| Hash | `CanonicalDigest` | Да | Representation integrity | Exact item bytes, not authorization |
| HashProfile | `CanonicalProfileId` | Да | Digest interpretation | Exact known item profile; wire carrier Q15 |
| AssetIds | Уточняется | Уточняется | Exact allowed asset membership | Q19/Q15; no temporary URL identity |

No internal int Version in JSON. Raw activation keys only create/recovery, no echo/log/default SDK persistence; bearer no URL/plaintext Session persistence, explicit short encrypted capsule exception. Own concurrency ETag not cache content validator. Client Unknown freshness is network/unconfirmed outcome, not successful UpToDate wire value. Target model names/shapes undecided portions Q15/Q33, not existing NSwag DTO claims.

**DeliverySessionOptions (server configurable values, not measured SLA)**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| AbsoluteSessionTtl | `TimeSpan` | Да | Default30 minutes | Absolute/no sliding/refresh; final stored min other finite deadlines |
| ReplayWindow | `TimeSpan` | Да | Default5 minutes | Short cipher exception, no original-ID rebinding after cleanup |
| MaximumValidationAge | `TimeSpan` | Да | Default30 seconds | CM monotonic before VRS HTTP through FINAL guarded check |
| MaximumResponseLease | `TimeSpan` | Да | Default5 seconds | Monotonic local authorization bound, not physical network SLA |

Exact configuration binding/provider/deployment values remain engineering Q20; selected defaults/mechanisms are not TBD.


**Canonical synchronization inputs** — профильные typed normalized модели pure shared .NET library. Общие `CanonicalProfileId`, `CanonicalDigest`, `CanonicalRepresentation` определены только в [F03](F03-publish-schema-version.md#33-dto-и-parameter-models). Таблицы задают fixed canonical member order; member names относятся к canonical bytes, transport DTO carrier уточняется Q15/Q33. Guid — lowercase D ordinal; набор groups/selection сортируется SchemaId,SchemaVersionId; members — InstanceId. Full target hash вычисляется после всех upserts/removals, включая unchanged members. Bare digest без profile не сравнивается.

**TargetGroupCanonicalInput** — `cm-target-group/1`.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| schemaId | `Guid` | Да | Logical managed schema | Exact context |
| schemaVersionId | `Guid` | Да | Exact managed structure | No latest substitution |
| members | `IReadOnlyList<TargetGroupMemberInput>` | Да | Full installed target membership | Sorted instanceId; empty valid, no failed-query empty |

**TargetGroupMemberInput** — используется также внутри actual inventory; порядок полей одинаковый.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| instanceId | `Guid` | Да | Real managed instance | Unique within exact schema group |
| instanceVersionId | `Guid` | Да | Exact immutable values | Not version number/latest pointer |
| instanceKey | `string` | Да | Frozen installed runtime alias | No normalization/live lookup |
| contentHash | `CanonicalDigest` | Да | Exact instance bytes integrity | 64 lowercase hex; interpreted with actual exact frozen content profile |
| contentHashProfile | `CanonicalProfileId` | Да | Digest interpretation | Required known exact profile |

**InstalledInventoryCanonicalInput** — `cm-installed-inventory/1`. Duplicate identity tuples invalid. Это фактический inventory из active committed local storage; selection, desired target, staging и неполученные receipts не подменяют его. Explicit empty inventory допустим.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| groups | `IReadOnlyList<TargetGroupCanonicalInput>` | Да | Actual installed groups | Sorted schemaId,schemaVersionId; exact versions/aliases/digest profiles |

**SynchronizationRequestCanonicalInput** — request variant `cm-sync-plan/1`.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| kind | `string` literal `request` | Да | Document role discriminator | Fixed first member; not caller-selected |
| selection | `IReadOnlyList<CanonicalSchemaSelectionInput>` | Да | Original full normalized exact selection | Sorted schemaId,schemaVersionId; empty distinct from absent |
| inventoryFingerprint | `CanonicalDigest` | Да | Original actual inventory digest | Exact initial installed bytes |
| inventoryProfile | `CanonicalProfileId` | Да | Inventory interpretation | `cm-installed-inventory/1` |

**CanonicalSchemaSelectionInput**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| schemaId | `Guid` | Да | Selected logical schema | Exact normalized pair |
| schemaVersionId | `Guid` | Да | Selected structure | Duplicate tuples invalid |

OriginalRequestFingerprint = SHA-256 exact request-variant bytes. Original normalized semantic input сохраняется рядом для same-ID replay/collision comparison; random operation ID/clock/session credentials не входят в эти bytes.

**SynchronizationPlanCanonicalInput** — plan variant того же `cm-sync-plan/1`; role literal устраняет неоднозначность двух форм документа.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| kind | `string` literal `plan` | Да | Document role discriminator | Fixed first member |
| originalRequestFingerprint | `CanonicalDigest` | Да | Exact original semantic request binding | Request variant above; unchanged after own commits |
| originalRequestProfile | `CanonicalProfileId` | Да | Request digest interpretation | `cm-sync-plan/1` |
| groups | `IReadOnlyList<TargetGroupCanonicalInput>` | Да | Exact pinned complete target groups | Sorted schemaId,schemaVersionId; includes empty/removal-only target |
| removals | `IReadOnlyList<CanonicalRemovalInput>` | Да | Exact pinned managed removals | Sorted schemaId,schemaVersionId,instanceId; no duplicate/foreign scope |

**CanonicalRemovalInput**

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| schemaId | `Guid` | Да | Managed removal schema | Scope belongs to plan |
| schemaVersionId | `Guid` | Да | Exact structure scope | No unrelated installed groups |
| instanceId | `Guid` | Да | Exact removed managed representation | Does not delete application data |

PlanId/SynchronizationId, CreatedAt/ExpiresAt, execution lease/generation/progress/cursors/receipts отсутствуют в plan bytes. Plan fingerprint не меняется после собственных client commits; новый actual inventory не переписывает original input. Empty/removal-only target имеет настоящий canonical full group digest, не null/ошибку/EOF. Session verifier, Registry ETag и media byte hash остаются независимыми representations/purposes, не используют эти profiles как credentials.

### 3.4. Ports

| Port | Responsibility | Direction |
| --- | --- | --- |
| IContentModelingDeliveryQueries | Exact available Published desired/read/schema/withdrawal evidence | Public owner queries, no foreign DbContext |
| ISynchronizationPlanStore / IExecutionOwnershipStore | Atomic reservation/Status snapshot/Acquire/Renew/Revoke/PublishPreparedPlan/RegisterOfferedBoundary/AcceptReceipt/control original replay/tombstone transitions | Local per-operation coordinator/cache adapter Q33; short guard, no I/O/wait inside; no Redis |
| IPublishingDeliveryQueries | Ordered committed transitions/completeness after processed sequence, immutable exact artifact | Primary public Publishing read; bus event not authority/recomposition |
| ICatalogEntitlementQueries / IPolicyAgreementReader | Explicit current confirmed period/current versioned facts + exact source-confirmed immutable agreement | F12 public models/independent primary readers; never current/as-of promise |
| IActivationKeyValidator | Full raw set→immutable verified IDs/security epoch/deadline/caller proof+license watermark | VRS HTTP only create/recovery; target Q23 extensions not proven by stableIdlong |
| ISessionTokenGenerator / ISessionVerifierLookup | 32-byte CSPRNG/Base64Url/SHA-256, private fixed32 verifier lookup/constant time compare | Crypto/private own auth store, no token/hash public DTO |
| ISessionReplayProtector | ASP.NET DataProtection Protect/Unprotect ONLY token, purposes SessionId+RequestId/version | OUTSIDE gate; shared persistent protected keyring |
| ICredentialSecurityReader / normalized state store | Immutable security values/latest epoch; apply trusted HTTP/broker same order | Own Delivery state, no raw key; public projections no Version |
| IMonotonicClock / IResponseLeaseScheduler | Before-HTTP proof age and independent lease deadline/cancellation | Local elapsed clock, not VrsValidatedAt/server wall-clock age |
| ICreationReceiptStore / IReplayCapsuleStore | Original immutable outcome + mutable short cipher/tombstone | Own Delivery commit; final version/deadline recheck, no foreign UoW |
| IDeliveryStateRepository / ISnapshotRepository / IRestrictionRepository / ISessionRepository | Own tracked roots/conditional writes | Delivery storage |
| IProtectedSessionAuthorizer / IResponseLeaseRegistry | Fresh guarded selected pinned decision+membership/ban checkpoint, ephemeral lease cancellation | Explicit Application use case; no VRS resource HTTP/long-held DB permit |
| IMediaDeliveryReader | Public AssetId metadata/read eligibility facts | MediaLibrary F05 port; bytes go Host YARP → private S3, not application OpenRead |
| TimeProvider / execution context / inbox | Session/Demo/audit trusted context | Existing Host; not visibility-clock authority |
| IPrimaryVisibilityObservationClock | PostgreSQL UTC clock after exact primary committed observation | Same new Delivery confirmation transaction/connection; no host UtcNow/precommit timestamp |
| IDeliveryProcessingOutcomeStore / IVisibilityEvidenceStore | Exact source meaning/outcome replay, Pending→first stored confirmation | Own Delivery persistence, unique history/Variant+Sequence |
| ICompleteDeliveryHistoryReader | Immutable Abstractions read values for actual history+evidence/source completeness | Own coherent read model §2.1, consumed F12/staff visibility |
| IAuthoritativeRevisionRestrictionReader | Exact owner public terminal ban, independent of projection/inbox lag | Primary public Publishing restriction read; unknown not Allowed |
| MVP consistency guard / owner UoW | First transaction advisory gate, Delivery-only participant partition и fresh primary public facts | Owner transaction connection, independent foreign read scopes; Demo separate |

### 3.5. Specifications / read contracts

[S19](../standards/S19-specifications.md): SessionByIdAndVerifierSpecification, SnapshotByRevisionIdSpecification, DeliveryHistoryBeforeCutoffSpecification, ActiveSessionsByInvalidatorBasisSpecification, RestrictionByRevisionIdSpecification. Atomic ForVariantCondition/AtSessionBasisCondition/EffectiveBeforeBoundaryCondition/AfterPublishingSequenceCondition; compose builder extensions in concrete Ardalis specs, no invented spec.And.

ContentModeling owns exact-schema Published/version-order query; Delivery receives immutable public values, not source tracked roots. Application record cursor bound plan/order/group/identity; no mutable latest paging or public GET-per-chunk contract. Manifest/item/hash membership checked prior to cache304/bytes, cursor/network position != durable checkpoint/install proof. Effective history strict cutoff keeps null, validates completeness/catch-up profile Q17/Q20.

## 4. Application

### 4.1. Handlers / use cases

PolicyAgreement stays F12 before-source-confirmation promise. F13 raw keys only creation/recovery; named CmDeliverySession runtime auth is separate from staff/admin auth and Demo remains anonymous. New F13 selection/admission uses explicit source CurrentPeriodId per verified license+Variant, exact confirmed AgreementId/original-plan binding and current versioned cutoff/unlimited/closure/revoke/completeness. Renewal excludes old binding as fallback; genuine correction preserves frozen policy and recomputes new selection cutoff. Pinned session eligibility now uses accepted separate F12 EvaluatePinnedRevision; no repin, original absolute expiry retained. Renewal/forward selected security epoch/confirmed denial require new admission; temporary unknown not terminal revoke.


Materialization/session/runtime restriction checks используют stored final effective instance/schema/media closure F10 вместе с exact revision ban и F12/session basis. Unavailable suppressed/excluded source A не блокирует B, если A не shared final dependency; source Available/owner/trusted evidence validation при Create и broader retained source/evidence graph не становятся runtime closure. Broad retention не удаляется. F11 item Activated означает Publishing commit; Delivery readiness/confirmed visibility остаются independent Publishing progress, без autoresume Cancelled/Superseded items.

MVP command write path следует §7.2: own ReadCommitted transaction и first global transaction advisory gate предшествуют authoritative own/public CM reads; preparation/external HTTP выполняются снаружи, relevant CM basis заново проверяется после gate. Demo и protected read/stream paths сохраняют собственные границы.

Delivery.Application: StartSynchronizationHandler/ResumeSynchronizationHandler/StopSynchronizationHandler/DiscardSynchronizationHandler/CommitSynchronizationGroupHandler — Demo workflow with local plan/ownership adapters and public ContentModeling queries. MVP ApplyPublishingTransitionsHandler/ConfirmDeliveryVisibilityHandler/RecoverDeliveryVisibilityHandler/MarkPublicationWithdrawnHandler/CreateDeliverySessionHandler/RevokeSessionsHandler — own Delivery roots/public F12/Publishing/Catalog/HTTP ports. Host composes Demo without license/Identity/broker dependencies. Query placement §3.2; no shared App service foreign DB traversal as target implementation.

Start atomically reserves same-ID context; existing fingerprint/result reconciles response loss. Pure diff captures immutable exact targets. Resume never recaptures ready target; if never ready it may complete first preparation from retained original input. Data query reads Published payload without re-enrichment, rechecks withdrawal and produces sequential NDJSON records with bounded buffers. Api waits group receipt before next group; receipt endpoint independent from stream lease. Absolute reservation TTL default1h/no extension, bounded in-memory store one Demo CM process loses plans on restart. Lease/generation fencing rejects late old writer. Concrete serializer/token/flush/compression/lifetimes materialized by shared fixtures Q33.

ApplyPublishingTransitionsHandler treats bus only as trigger, reads ordered committed Publishing history+completeness after LastProcessedPublishingSequence. Batch prepares immutable artifacts outside gate; each actual effective transition gets its own Delivery commit. Under first gate fresh exact source/order/authoritative ban checks precede full graph+pointer+actual history+Pending visibility evidence+processing receipt/sequence+inbox atomic writes. Known terminal banned intermediate consumes NotEffective outcome without history/pointer change/stall; technical missing artifact remains pending/failure, not consumed.

CreateDeliverySessionHandler receives durable CreationRequestId/Variant/full raw key set; validates every needed proof through VRS outside gate, CM monotonic start captured BEFORE HTTP. Server builds original meaning from trusted verified IDs/caller context only. No client verified IDs/organization license inference.

New create allocates SessionGuid+32-byte CSPRNG token/SHA verifier and DataProtection Protect(token-only, versioned purpose SessionId+RequestId) outside gate/keyring I/O. Final own ReadCommitted transaction/first global gate checks proof age including all cost/wait, latest normalized credential epoch (HTTP/broker order), license watermark readiness and F12 complete-set decision. Same whole candidate tie min LicenseId then CredentialId. Commit selected session/verifier+immutable original receipt+cipher atomically before HTTP; stale proof release/redo VRS outside gate.

Replay authenticates full original proof context fresh. Preliminary own receipt/cipher read is not authorization; Unprotect and verifier/purpose/scope check OUTSIDE gate. FINAL own guarded fresh check after crypto rechecks proof age, same original receipt/capsule version/state/deadline, selected session epoch/current PINNED eligibility. Return original secret/metadata only then, no new candidate/Restore/reissue/TTL extension. Temporary DB/keyring outage keeps same ID; proven expired/unrecoverable capsule typed NoReplay, new ID means fresh admission, no implicit closure of other sessions. Original ID ledger/outcome remains even cipher cleanup.

CmDeliverySessionAuthenticationHandler decodes32 bytes, hashes SHA-256 and compares fixed-length verifier in constant time; route/session scope must match. Runtime principal contains only CM delivery scope, no staff role/default-staff-scheme fallback. Basic bad/expired session401; required auth-store outage503, not mistaken invalid credential.

AuthorizeDeliveryResponseHandler and RenewDeliveryResponseLeaseHandler use short own first-gate checkpoint of fresh selected session/credential state/license current period/cutoff/F12 pinned decision/snapshot membership/owner public exact ban. No VRS HTTP; temporary source unknown503 and no session Revoke. Confirmed denied/basis change records terminal own invalidation where appropriate and never restores later. New R2/unpublish/archive/retire does not repin R1.

ResponseLeaseCoordinator owns ephemeral response generation/cancellation, periodic fresh renewal and INDEPENDENT monotonic deadline timer. It cancels downstream writes/YARP upstream when expired/known invalidation even renewal awaiting DB/gate; late renewal cannot reactivate. Gate released before headers/cache304/range/proxy/bytes. Finite boundary incl Freeze can stop this response and require NEW HTTP even same valid session. Cancellation after headers uses selected transport abort; SDK Unknown, no byte recall/physical-net SLA.

### 4.2. Validators

[S18](../standards/S18-fluent-validation.md): ExactSchemaSelectionValidator, RepresentationIdentityValidator, InstalledInventoryEntryValidator, CompleteSelectionPresenceValidator, SynchronizationIdValidator, GroupReceiptValidator, CursorContextValidator, ProductVariantIdValidator and SessionCreationInputValidator. SynchronizationControlValidator validates planId/nonempty GUID/positive Int64 expected generation; ResumeOriginalRequestValidator composes original digest/profile rules; host SynchronizationTransportOptionsValidator checks positive budgets/caps, coherent buffers and renewInterval less than leaseDuration. Compose Start/Resume/Stop/Discard/Commit/Freshness validators using semantic components/named constraints; JSON absence/null distinguished from explicit empty list. Duplicate tuple identities invalid; foreign exact SchemaVersion owner mismatch group validation, not empty result.

Model shape validation doesn't prove selection represents application's full local registry; SDK F19 owns it. Domain/source availability/completeness/lifecycle checks, cache atomic ownership, session token/status/ban/membership and commit guards remain authoritative Application/DB/adapters. Untrusted inventory hashes are comparison inputs, not permission/proof saved bytes. Session token/default TTL/proof age/lease durations selected §3; exact wire/security source/configuration/retention profiles Q15/Q20/Q23. Demo record/cursor/receipt limits/formats Q33, no source literals copied.

Public wire validators: mandatory non-null arrays, each exact Guid/profile/hash/key field and duplicate normalized tuple checks; no Distinct/coercion. Record parser rejects unsupported protocol/kind, missing/duplicate properties, noninteger/out-of-long-range counters, invalid UTF8/BOM/empty/tail/size. payload known union guided kind; group IDs required, normal sequence>0/cursor required vs invalidation0/no cursor. ContentJson is string; reconstructed UTF8 digest verified before typed serializer. Plan/group/base/full-target invariants independent from record-count checks; no default empty target on error.

### 4.3. Mappers

CreationMeaningMapper canonicalizes trusted verified identity set+Variant only; source caller context separate trusted authorization, raw key/hash/epochs/times excluded. SecurityMeaningMapper compares stable source security binding/valid/revoke/deadline (not validation audit/derived expired-at-read or commercial watermark). Private session/receipt/security projections map to owned Domain values; readonly public metadata/decisions never expose verifier/cipher/internal Version. Epoch/cutoff corrections never become new token/pin silently.


CompleteDeliveryHistoryMapper projects immutable nontracking Abstractions values to owned DeliveryTemporalDecisionValues/DeliveryTemporalHistoryPoint for pure F12 Domain evaluation. Private Domain history/evidence records including internal Version never leave repository/EF as read-port entities; Domain has no Application.Abstractions dependency. Staff exposure maps read carrier independently.


Api binds route/body/full-selection-presence/trusted session context and cancellation; key/session credentials excluded from normal logging. DesiredProjectionMapper copies ContentModeling exact IDs/version order/hash/schema/key values. DiffMapper maps immutable operations/groups, no guessed Key-match. SnapshotMaterializer copies F10 exact result and immutable content/schema payloads, preserves canonical profile/manifest identity Q19. CredentialMapper maps VRS trusted non-secret facts, not source grants/revision choice. Plan-summary/record/result mappers distinguish exact upsert/removal/group boundaries/invalidation/session denial/freshness; no hidden latest GET substitution. Domain own Version stays internal.

Plan mapper retains full CanonicalPlanInput groups incl unchanged for frozen cm-sync-plan/1 hash, while Start groups/stream execution include only changed groups. Inventory→base group hash uses same cm-target-group/1 context/member layout. Cursor/lease/PlanId/clocks excluded from semantic plan hash; runtime contentJson string is decoded exact captured canonical bytes, no reserialization, no Registry call/live enrichment. Status maps saved plan/state/receipts from same cache observation; preparing missing plan not groups:[] nochange. Control/Freshness use dedicated typed inputs in §3.3; Status includes reserved planId/current executionGeneration. Control replay maps original immutable response snapshot.

### 4.4. Services / algorithms

DesiredSetBuilder processes each requested exact schema group with authoritative availability; latest Published per Instance selected after exact structure filter. DiffPlanner compares full inventory identities: desired-only Add, changed exact version/hash Update, inventory-only in valid managed scope Remove. Deselected schema-version groups get scoped removals; requested unresolved/invalid group yields error/preserves client active group, never mass-delete from failed query. Full target hash/end/count/control profile Q19/Q33; same-version hash inconsistency/reuse rules specified there.

CanonicalSynchronizationBuilder maps full actual installed input → inventory triple, original normalized selection+inventory → request-variant fingerprint, captured complete targets/removals → plan-variant triple (§3.3). Все exact IDs/aliases/digest profiles типизированы; numeric semantics берутся из published schema metadata, SDK не угадывает long/decimal/float из untyped JSON number. Unknown profiles rejected before staging/apply; bytes/digest mismatch не исправляется rehash. Full target verified after upserts AND removals, including unchanged members. ID/time/receipts/own progress не изменяют original plan bytes. Inputs/profile registry/common library не зависят от Domain/EF/Asp; profile-specific adapters маппят owner facts в neutral input.

PlanExecutionCoordinator is one process-local instance per operation, with short atomic guard around Acquire/Renew/Revoke/PublishPreparedPlan/RegisterOfferedBoundary/AcceptReceipt/control-receipt transitions. Network/DB I/O/client receipt wait/cancellation signaling outside guard. Check generation, unexpired monotonic lease and absolute plan deadline before each new record/mutation/renewal/group advance; stale finally releases only its own resources. Expired worker Task can remain alive without blocking replacement; bytes already sent cannot be recalled. Receipt wait renews only current lease and has independent finite watchdog; timeout returns execution to recoverable interrupted state, not fabricated invalidation. Immutable target/original fingerprint/offered boundaries/accepted receipts survive ownership changes. PostSyncFreshnessService reuses desired/diff validation without creating plan/ID; ordinary newer publication yields changes after old successful plan, no recursive auto-sync loop.

SnapshotMaterializationService checks preliminary terminal status before mandatory graph prep; known ban bypasses prep and is revalidated under gate, preparation failure stays technical. Otherwise stages exact immutable graph outside gate. OrderedDeliveryProjector processes source history sequentially using exact receipts; one actual transition per commit, terminal bans consumed without effective rows. VisibilityConfirmationCoordinator performs serialized primary observation/UTC timestamp capture after original committed exact history read; TemporalHistoryCompletenessEvaluator distinguishes complete empty/known null/pending/unknown/order failure and never substitutes actual commit time. SessionCredentialService and ProtectedSessionAuthorizer implement accepted named CmDeliverySession bearer/auth/checkpoint/lease scheme; Entitlement evaluator/binding only F12, not duplicate copy. Asset read authorized here using public MediaLibrary eligibility facts, then Host YARP forwards bytes; storage SDK not runtime dependency.

MVP temporal delivery Q34 consumes already frozen Offset UTC7digitsZ bytes/new instance-content profile; historical Naive /1 retains seven-digit no-offset representation. Source read/materialization carries actual profile, never computes latest reinterpretation or reconstructs original offset. Authoring offset belongs F04, runtime response canonical UTC; no Delivery timezone/date coercion. Plan/upsert/hash/fingerprint context preserve content profile alongside digest; unsupported profile rejects before delivery preparation/client group apply, no type guessing. Group/inventory/manifest codecs remain /1 when own layout/semantics unchanged; same profile IDs/tuple positions already cover new content-profile values. Canonical temporal identity/codec F04/Q19, SDK binding F19. No new endpoint/capability body guessed from this decision.

Public-wire components: SynchronizationRecordFactory computes stable ordinal group/operation record layout; N=1+sum(2+added+updated+removed) checked long overflow. groupStarted payload summary hashes/counts, groupCompleted actual counts+target evidence, operationCompleted {} final. OperationInvalidated0/no cursor outside log; cannot replace previously issued seq payload. Replay equality compares parsed identity/all payload fields and restored content bytes, not incidental outer JSON whitespace/property order. No semantic serializer regeneration of content.

SynchronizationCursorCodec server signs41bytes with HMACSHA256 process-local32byte key, full32byte MAC, canonical Base64URL98 chars; UUID network-order conversion explicit, not Guid.ToByteArray mixed. Decoder checks syntax/length/version, then retained plan lookup/binding and MAC constant-time/seq range; missing plan after restart404 before old-key MAC check. Candidate token does not claim saved progress/installed facts. Crypto context singleton same one-process plan cache; pure DTO/record/hash code no private key. SDK does not receive key.

Api SynchronizationNdjsonWriter consumes incremental factory records, writes UTF8 compact line+LF with bounded record buffers/cancellation; arbitrary network chunks not boundaries. Existing JSON result mapper preserves ApiResponse<T> and common preheader errors, stream not wrapped. Application handlers/validators registered existing scan; stateless record/profile mappers, singleton cache/cursor key Host composition; owned query readers remain Persistence, no DbContext held through long wait. Demo NDJSON HTTP compression disabled; incremental writes honor separate finite backpressure/receipt/preparation budgets and current fence. Linked worker cancellation/deadline interrupts I/O outside guard; finally disposes own resources and conditionally releases only captured owner.

#### Разрешение asset requirements после Demo

Предлагаемый resolver имеет Application orchestration и pure recursive algorithm, без нового editable aggregate/MediaAsset policy. Для Application.Abstractions предлагается логическая read-модель `AssetDeliveryManifest`; точные DTO/ports/поля и их CLR-типы **Уточняются** Q37, source JSON не является declaration. Query handler независимого manifest-read должен находиться в Delivery.EntityFramework.Persistence; выбирать новый route или добавлять свойства в существующий manifest DTO до согласования запрещено.

1. После authoritative whole-revision resolution взять полный materialized authorized snapshot и exact Published SchemaVersion definitions через public owner boundary; не использовать live `latest`, имена продукта или чужую license composition. Binding исходных definition profiles сохраняется.
2. Рекурсивно сопоставить definition × фактические Published values, включая Object/List/List<List<AssetReference>>. Собрать только присутствующие valid refs, их frozen descriptor из пяти полей, declared mode exact leaf и контекст происхождения. Optional absence не creates required job. Legacy definition без policy имеет effective `OnDemand`, без historical bytes rewrite.
3. Внутри одного Variant/revision сгруппировать по проверенной immutable asset identity (`AssetId` вместе с ожидаемыми size/SHA), а не произвольному Guid. Конфликт immutable facts не маскировать выбором произвольного descriptor. Предлагаемая агрегация — строгий максимум `RequiredBeforeLaunch` > `Preload` > `OnDemand`, deterministic ordering и один download identity; конкретный integrity/order/DTO contract Q37.
4. Выдать повторяемую immutable опись, связанную с тем же pinned revision, что entity sync; либо целиком сохранить производную в owner Delivery projection при выбранной materialization модели. Нет foreign DbContext/FK/cross-BC transaction и mutable второй истины policy. Физические tables/cache/profile/commit placement Q37/Q20 не считаются выбранными.
5. Launcher сопоставляет опись с verified cache и планирует jobs через общий DotNet SDK; сервер не запускает клиентские downloads и не хранит их jobs в SynchronizationPlan. Running Player сохраняет старый pinned snapshot, новая policy/ревизия не hot-reloads его. Source exact build SchemaVersion requirements и ProjectStorage legacy→canonical Variant mapping Q37; по Name не связывать.

Manifest не разрешает выдачу bytes: каждый remote request повторяет DF16/DF21 authorization. Shared cache bytes/strictest mode не объединяют entitlement разных Variants; offline/revocation и old-snapshot fallback отдельно Q37. Unity re-admission после истечения absolute session, включая закрытый launcher, требует нового согласованного proof protocol; текущий bearer не refresh-ится автоматически.

## 5. Api

### 5.1. Endpoints

Start/data/group-commit route shapes ниже следуют [принятому транспортному решению](../../../archive/content-manager-sync-streaming-transport-decision.md); Demo control/Freshness routes также согласованы; MVP строки остаются **проектной картой**. [Q33](../questions/Q33-sync-resume-transport.md#f13-wire-protocol-и-execution-ownership) фиксирует Demo wire/control/Freshness; MVP exact URI/final DTO/error profiles требуют дальнейшей детализации; final signatures/status/access [Q15](../questions/Q15-api-and-source-code.md#f13-runtime-contracts-и-source-evidence). Это не current controller/OpenAPI. Demo no auth/security session; MVP named CmDeliverySession scheme/opaque bearer and creation recovery protocol accepted Q16. Exact runtime URI/security wire profile Q15. JSON response envelope/stream rules explicit after contract choice.

| Method | Route / destination | Request | Response | Access |
| --- | --- | --- | --- | --- |
| POST | /api/v1/content-synchronizations/{synchronizationId}/start | Full selection/inventory, SDK ID/context | Compact plan metadata JSON | Demo, no license/key |
| GET | /api/v1/content-synchronizations/{id} | ID/read context | Cached plan/status | Demo observer |
| POST | /api/v1/content-synchronizations/{id}/resume | ResumeSynchronizationRequest | ApiResponse<SynchronizationStatusDto> | Demo continuation |
| POST | /api/v1/content-synchronizations/{id}/stop and /api/v1/content-synchronizations/{id}/discard | SynchronizationControlRequest | ApiResponse<SynchronizationStatusDto> | Demo lifecycle |
| GET | /api/v1/content-synchronizations/{synchronizationId}/data | Optional application cursor | Один incremental record stream, MIME/framing Q33 | Demo pinned-plan read |
| POST | /api/v1/content-synchronizations/{synchronizationId}/groups/{schemaVersionId}/commit | TargetGroupHash/receipt context | Idempotent acknowledgment | Demo receipt |
| POST | /api/v1/content-synchronizations/freshness | SynchronizationFreshnessRequest | ApiResponse<SynchronizationFreshnessResult> | Demo check, no new plan |
| POST candidate | /api/v1/delivery-sessions | CreationRequestId+full ephemeral key set+VariantId | Session/credential/pinned IDs/hash/expiry | Authoritative VRS validation then F12 |
| GET candidate | .../delivery-sessions/{sessionId}/manifest | Session credential, conditional cache profile | Exact manifest/hash | Protected exact session |
| GET candidate | .../{sessionId}/items/{versionId} | Session credential, exact ID | Materialized exact content/hash | Protected item membership |
| GET/HEAD | CM-owned `/storage/assets/{assetId}` gateway Q21; protected session context Q16 | Session credential/context, AssetId; conditional/range profile | YARP binary/provider/gateway result | Protected pinned membership/access before cache/proxy |

Schemas/runtime model contract reader, Demo asset gateway reuse F05 and Registry F21; separate schema endpoint/no newroute TBD if manifest/registry not enough. Admin authoring version endpoints not substituted as runtime plan. H3 preference/H2 fallback same ID; concrete server/SDK/TLS/proxy support must be measured, not assumed from API signature. Data stream не JSON ApiResponse, SSE или materialized whole-response array. После отправки headers malformed/withdrawal/failure выдаются согласованным terminal record либо прекращением transfer по Q33; обычный error envelope нельзя дописывать в середину response.

### 5.2. ETag / If-Match

[S16](../standards/S16-rest.md): F13 has no public state CRUD, so internal own Version on state/snapshot/session not JSON/public mutation precondition automatically. Demo immutable plan + explicit execution ownership/receipt protocol, not If-Match Version as replacement for lease. Local internal optimistic guards protect projector/session invalidation.

Accepted canonical digests/profiles may supply integrity metadata; exact cache ETag representation/carrier remains Q21/Q15 and is independent. Authorization/membership/withdrawal runs **before** 304 or byte range; matching hash doesn't grant rights. Cache ETag/If-None-Match/If-Range distinct from mutation If-Match; no digest silently reused as own lifecycle Version. Plan identity/hash not resume cursor or proof installed. Header/status/carrier/CORS/redaction profile Q15/Q16/Q33.

### 5.3. OpenAPI и ошибки

Define separate Demo Start summary/full mandatory input arrays/optional application cursor/incremental record roles/group-end/invalidation/stop result and MVP creation/credential/denial/membership/binary contracts. Publish relevant 400/409 active same-ID and missing/expired-plan distinguishable result; Start/Status/Commit JSON200 and Data raw200 fixed Q33; malformed400/context409/unknown404/known unavailable410 via bounded tombstone agreed. Exact machine reason registry/error envelope common Q15, not new wire decision. MVP invalid credential/insufficient projection/grant/revoke/expired/session ban/resource-outside reasons remain distinct; preheader401 bad/expired session,403 confirmed rights denied,409 changed basis/new admission,503 proof/required-data unknown,404 outside composition selected; exact codes/DTO Q15, no blanket all403.

Streaming endpoint describes NDJSON application/x-ndjson UTF-8/LF, accepted envelope/variants, opaque cursor resume, cancellation/backpressure и OperationInvalidated/truncated outcomes after headers. camelCase/token/count/status profile fixed §3.3 and literal vectors; actual codec/adapter conformance required before integration. Asset binary endpoints separate conditional/range semantics. NSwag where applicable after backend verification; SDK protocol clients not Blazor client copies. Target runtime OpenAPI not proven by source scan; tests §11.

## 6. EntityFramework

### 6.1. DbContext ownership

DeliveryDbContext owns MVP state, immutable snapshots/items/schema/asset membership, effective history, terminal restrictions, sessions/verifiers, own credential security states, immutable creation receipts/replay capsules, F12 bindings and inbox. Cross-BC queries use public ports, no CatalogAccess/Publishing/ContentModeling/MediaLibrary DbContext in Delivery handlers. Current shared App service behavior explicitly legacy.

Demo content remains ContentModeling PostgreSQL; plan/execution storage local CM cache physical choice Q33. No forced PostgreSQL plan aggregate, distributed cache, license DB/broker dependency. Cache loss allowed recovery from actual client inventory; selecting local durable storage later not already required by content DB.

### 6.2. Configurations

Logical target tables below; physical names/layout/profile Q20. [Current mapping](../../../../contentmanager.api/src/BoundedContexts/Delivery/ContentManager.Delivery.EntityFramework/PublishedCompositionConfiguration.cs) stores PublishedComposition ManifestJson/text+Hash/IsRevoked and session IDs/basis/time, no target verifier/TTL/history/full items.

| Entity | Table | Key/index | Concurrency | Mapping |
| --- | --- | --- | --- | --- |
| Delivery state | Delivery.ProductVariantDeliveryStates | VariantId PK | own int Version | Nullable effective pointer/current history ID, separate LastProcessedPublishingSequence |
| Source processing outcome | Delivery-owned outcomes; physical table Q20 | Unique VariantId+PublishingSequence | Immutable original receipt | Meaningful input + EffectiveCommitted/NotEffectiveTerminalRestriction + nullable history/ban ID |
| Activation record | Delivery.DeliveryActivationHistory | HistoryId PK; unique Variant+PublishingSequence/effective order | Immutable insert | Exact source/order/target/null/audit; no fabricated timestamp |
| Visibility evidence | Delivery-owned evidence; physical table Q20 | EffectiveHistoryId unique/own FK; pending Variant/order index | own int Version; conditional Pending→first Confirmed winner, confirmed time immutable | Nullable timestamptz VisibilityConfirmedAt UTC, pending/confirmed/order integrity |
| Snapshot | Delivery.DeliveryPublicationSnapshots | RevisionId PK, Variant read index | own int Version lifecycle | Immutable exact manifest/representation/hash; ban status |
| Snapshot items/schema/assets | Delivery-owned child tables/values | Revision+instance uniqueness; asset/schema exact membership | Immutable content | Lossless canonical bytes/profile, not jsonb rerendering assumed hash bytes |
| Terminal restriction | Delivery.RevisionDistributionRestrictions | Exact RevisionId unique | Terminal/idempotent chosen token profile | Can preexist snapshot; trusted fact identity/time |
| Session | Delivery.DeliverySessions | Id PK; unique fixed32-byte verifier; selected credential/license/Variant/pin invalidation indexes | own int Version | Exact selected security epoch/binding/period/pin, absolute expiry/terminal invalidation |
| Credential security | Delivery-owned credential projection; names Q20 | CredentialId PK/long; source SecurityEpoch order | own int Version | Stable security fields only; no raw key/audit-time generation |
| Creation receipt | Delivery-owned original receipt store; names Q20 | CreationRequestId unique in caller scope/profile | Immutable insert-only | Original verified meaning/caller/session metadata retained, no raw key/key hash/epochs |
| Replay capsule | Delivery-owned ciphertext store; names Q20 | Receipt/session own FK; request unique | own int Version | DataProtection bytea ONLY token, absolute ReplayUntil/state; tombstone keeps original ID bound |
| Binding/inbox | F12/shared Delivery mapping | Unique LicenseId+VariantId+SourcePeriodId / event identity | Own insert-only binding/inbox | Exact AgreementId/frozen enum/source/version + own immutable factory-generated GrantPeriodId, no FK/frozen pointer |

### 6.3. Constraints / indexes

Unique processing outcome Variant+PublishingSequence and effective history/evidence identity/order, plus snapshot membership; exact SchemaVersion/Instance owner consistency/source ID mapping; null pointer legal transition, terminal ban cannot be cleared by materialize/replay. Session expiry/basis consistency and no raw-key storage; normalized grant scope uniqueness F12. Internal Delivery references/FK can Restrict historical deletes; no cross-BC FK. LastProcessedPublishingSequence/EffectiveOrder/VisibilityConfirmedAt are separate; processed progress doesn't certify actual history/time readiness or replace own Version; canonical storage/hash byte identity must be verified, not inferred from JSONB equality. Archive/new-use lifecycle vs historical permission F03/F04/F10 distinct.

## 7. EntityFramework.Persistence

### 7.1. Repositories / query adapters

MVP Host partition-ит UoW participants по owner Delivery: own state/snapshot/session/binding/inbox/outbox участвуют в одной Delivery transaction. Текущий EfCoreUnitOfWorkParticipant с общей multi-DbContext transaction требует owner partition; public Publishing/Catalog/ContentModeling/Media readers создают independent primary scopes/connections без ambient enlistment. Demo DI не получает полный Publishing/licensing guard как dependency.

EfDeliveryStateRepository, EfPublicationSnapshotRepository, EfRestrictionRepository, EfDeliverySessionRepository and EfPolicyBindingRepository F12 implement own local ports. SQL Query handlers manifest/item/session/effective history in Delivery.EntityFramework.Persistence; Demo plan/status/stream read handlers in cache/access adapters, exact content owner public query adapters. No repository per DTO/child or direct foreign context joins.

Host registers named CmDeliverySession AuthenticationHandler without changing staff default scheme/permissions; runtime-only endpoint policies and anonymous Demo path. Session/replay/checkpoint/renewal/security handlers, private verifier/receipt/capsule stores, full VRS validation client, normalized HTTP+broker epoch consumers, monotonic proof clock/independent response lease timer/registry and server options. Existing projector/confirmation/primary observation/read adapters preserved. Confirmation does not use host TimeProvider as visibility authority; owner-only participants remain. Current [App module](../../../../contentmanager.api/src/App/ContentManager.Application/ContentManagerApplicationModule.cs) registers internal service; it isn't concurrently retained as alternate public delivery bypass. Missing target Api/Abstractions/Persistence projects planned explicitly; Demo DI scope independent Q20.

### 7.2. Transactions

MVP write commits используют [consistency guard Q20](../questions/Q20-persistence-and-migration.md#mvp-протокол-consistency-guard): **own Delivery ReadCommitted transaction → отдельный первый pg_advisory_xact_lock statement на том же owner connection → fresh own tracked roots/restrictions/Sequence/inbox/binding + independent primary public reads → own state/intents/inbox commit**. Один стабильный global exclusive transaction key одинаков для всех CM replicas и BC writers в одной PostgreSQL database. До acquisition нет authoritative basis reads/row locks; после ожидания старые tracked/prepared факты не переиспользуются как разрешение commit. Foreign ports не enlist-ятся в ambient UoW, не возвращают EF roots, не читают cache/read replica и не выполняют shared SQL transaction.

DF11 each one-transition commit, DF18/DF19 separate post-original-commit confirmation/recovery, DF12 restriction, DF13 admission/binding и DF17 confirmed revoke получают новый gate на свой owner commit; Publish/Withdrawal/Catalog/policy writers используют тот же gate в своих BC commits. Gate защищает только **committed CM facts**: он не даёт мгновенного знания об изменении внешнего VR School и не заменяет Q16 credential/session TTL, revoke propagation или Q17 effective-time/in-flight guarantees. Сообщение — trigger для свежих public facts, не право восстановить запрещённую старую revision.

VRS key validation, payload/CPU preparation, S3/YARP bytes, broker send/ACK, ожидание projection и HTTP stream выполняются снаружи gate. После внешней validation/подготовки admission заново проверяет relevant committed Catalog/Publishing/Delivery facts под gate; внешняя validation остаётся bounded observation по Q16. Полный snapshot может готовиться снаружи transaction; до своего commit заново подтверждаются exact source identity/lifecycle, terminal bans, order и нужные current facts. Ни поток bytes, ни protected read не держит global gate до окончания передачи; непосредственное прерывание всех начатых streams не обещается.

Acquisition bounded configured wait+cancellation; timeout/cancel/port error — technical abort, не domain409/empty allowed basis. Owner connection loss прерывает операцию без reconnect continuation; lost commit ACK → unknown outcome/inquiry по выбранной operation/request identity Q15/Q16. Own expected Version и independent basis остаются разными условиями; 412 нельзя исправлять blind retry с новым token, relevant basis drift →409/revalidation там, где этот precondition выбран.

Demo остаётся одним CM process: AssetId guard F05 и immutable in-memory plan/ownership protocol Q33 сохраняются; global MVP Publishing guard/BC dependencies не добавляются к Demo. Queries/status/picker/freshness не выдают permit для будущего write commit. Protected manifest/item/asset/progress используют explicit short Application authorization checkpoint DF21 под новым own read transaction/global gate; это coherent current authorization, не stream-long write permit. Release до cache/headers/bytes.

| Boundary | Atomic local work | Отдельные границы |
| --- | --- | --- |
| Demo plan start/resume/state | Cache ID reservation/immutable plan binding/ownership/checkpoint according Q33 | Client atomic installed group, no server filesystem transaction |
| Actual projector transition | Full graph+pointer+actual history+Pending evidence+processing outcome/LastProcessedPublishingSequence+inbox | Exactly one actual transition per commit; staging outside gate |
| Known terminal non-effective source outcome | Original NotEffective receipt+processed sequence/inbox, own terminal ban if captured | No pointer/history/Pending visibility row; technical missing artifact not consumed |
| Confirm committed visibility | Own existing evidence Pending→first durable primary observation time | NEW own transaction after reconciled original commit; timestamp after reread, not either commit time |
| Revision restriction | Terminal ban+snapshot availability/session invalidation coordination/inbox | Matching F11 null transition ordering, no loss before snapshot |
| Session admission / creation replay | Session/verifier/pin+original receipt/encrypted capsule atomic; replay exact saved context authorization | HTTP/Protect/Unprotect before FINAL gate;30s monotonic age; original absolute TTL no extend |
| Confirmed revoke | Own relevant session revocation statuses/inbox | No reassign credential/license basis or client installed-delete |
| Protected read checkpoint | Fresh selected session/epoch/period/cutoff/pin/membership/ban decision + ephemeral lease | Release before headers/cache/304/Range/YARP; independent timer default5s, no persistent lease aggregate |

Catch-up order/state optimistic write/inbox checked together. Concurrent projectors don't skip history or expose incomplete revision. Retire source doesn't rewrite saved historical content; CM exact withdrawal/restriction/admission commits сериализует MVP gate §7.2; runtime read/in-flight propagation остаётся Q16/Q17. Publishing commit, outbox ACK, Delivery full transition commit, pending/confirmed primary observation evidence and protected refusal are distinct observations. Confirmation saved in another own transaction never claims its clock sample is that transaction's commit time. Session token/expiry alone no proof forever-valid grant/ban.

### 7.3. Migrations

Add accepted named-session/security epoch/verifier/original receipt/cipher/terminal-invalidation mappings and constraints with unchanged Q17 source processing/history/evidence; no persisted response lease/token plaintext and F12 normalized bindings; keep historical references. Legacy manifest-only/direct DB session transition and canonical byte/schema/media retention profile Q20/Q19/Q12. Dev CM not production: choose scoped dev-data transition without invented old promises/content from latest. Demo cache storage selection not arbitrary SQL migration. No migrations/data reset/tests executed by this analytical task.

## 8. External adapters

### 8.1. Local plan cache и streaming HTTP adapter

ISynchronizationPlanStore supports atomic ID reservation/result binding and separate immutable target/mutable execution state; exact store/TTL/lease/fencing/recovery Q33. Demo bounded one-process memory/absolute1hTTL/noextension fixed; no arbitrary active eviction; exhausted capacity rejects new Start, expiry revokes generation, bounded tombstone survives until retention/eviction/restart. GetSynchronizationDataQuery читает cached plan/context/cursor и через public ContentModeling reader выдаёт exact pinned records, без latest recompute.

Api-side incremental writer сериализует последовательные records одного конечного HTTP response с cancellation/backpressure и bounded buffering. Exact writer type/signature, MIME application/x-ndjson/UTF8/LF fixed; writer/resources/buffer limits and numeric budgets configured/validated; incremental flush tested through real proxy; Demo compression disabled. Independent TimeProvider lease/receipt watchdog cancels even stalled I/O. Не держать весь dataset в памяти; network fragment boundaries не являются record boundaries. Source query/resources освобождаются при normal completion/error/disconnect по выбранному adapter contract; query pipeline не держит общей foreign DB транзакции на всю передачу.

Reconnect открывает data request того же plan от durable application cursor; separate group receipt/status/lifecycle requests не требуют SSE channel и не меняют ID. Ошибка после headers не превращается в successful empty group либо второй JSON envelope; invalidation/result framing остаётся Q33. SDK вычисляет progress из plan/staging/validation/commit; Demo не требует event reader/broker/Redis.

### 8.2. RabbitMQ / MassTransit

MVP consumers Publishing transition/terminal withdrawal/confirmed credential/license/grant revoke, inbox/idempotency and source catch-up contracts. [S22](../standards/S22-eventbus-topology.md): topology/package/event identity/order/retry/DLQ details Q20/Q16; no literal guessed exchange names. Ordered history catch-up source contract sufficient beyond trigger event; baseline/history completeness and recovery Q17, no silently importing obsolete Catalog bootstrap design into Demo. Ban dominates delayed activation, outbox/inbox no exactly-once transport promise.

### 8.3. VR School HTTP и session credential

IActivationKeyValidator принимает полный raw-key set только на create/recovery. До HTTP запускается CM monotonic attempt clock; trusted response подтверждает CredentialId/LicenseId, caller context, stable security epoch/meaning, explicit validity deadline и RequiredLicenseSourceWatermark, не grants/revision. Каждый необходимый key проверяется; недоступный proof не пропускается. Stable ActivationKeyId long текущего VRS не доказывает наличие target epoch API: source adapter contract и normalization [Q23](../questions/Q23-source-synchronization.md#credential-validation-и-security-epoch) требуют реализации.

HTTP/broker применяют одинаковую per-credential ordering: newer stable security epoch может advance own Delivery knowledge под gate; older не перезаписывает newer, same epoch/different stable meaning — integrity fault. Validation/audit time, commercial correction и watermark не increment security epoch. Finite deadline действует без expire event. Новый proof не заменяет необходимую Catalog projection: required local watermark/completeness проверяется отдельно.

Timeout/cancellation/retry/circuit integration profile остаётся engineering Q15/Q23; недоступность validation запрещает create/replay disclosure, но обычный VRS outage не требует HTTP на resource/progress и не отменяет уже допустимый session до TTL/current restrictions. Never log keys/token/request secrets; selected opaque bearer/SHA256/DP protocol определён §2–§4, physical profiles §8.6.

### 8.4. MediaLibrary facts и Host YARP gateway

Public IMediaDeliveryReader supplies F05 metadata/read eligibility; Delivery проверяет exact pinned session/revision membership/current access, а не прокачивает bytes через IObjectStorage.OpenRead. Gate выполняется **до** output-cache lookup/304/range/proxy. Historical Archived может быть разрешён consumer context; new-use exclusion не равен физическому удалению. Pending/Failed/Deleting visibility и конкретный metadata gate — [Q21](../questions/Q21-media-storage.md#yarp-metadata-lookup-и-access-boundary).

Host YARP maps CM-owned `/storage/assets/{assetId}` deterministically into private MinIO namespace и использует server-side provider authentication/signing. Клиент не получает bucket/ObjectKey/endpoint/storage credentials, его CM credential не upstream authority. Read-only GET/HEAD; upload/delete application F05. Q21 фиксирует public SHA-256 content ETag, UploadedAt Last-Modified, configurable cache с Demo default 24h, conditional304 и Audio/Video Range206/416. Upstream S3 ETag не metadata Version и не public SHA validator. Concrete transforms/If-Range/error/lifetime checks и MVP session/in-flight revoke integration — Q21/Q16. Presigned URL не основной client contract.

Provider/gateway failures may be raw/non-JSON и отличаются от CM metadata ApiResponse. Forwarder streams/cancels/releases upstream without full-file buffering; incomplete transfer no false ready. Asset failure не undo committed Demo data group. Optional F23 catalog/bundles pipeline не назначается обязательной частью этого gateway.

### 8.5. Canonical representation и clock

Accepted [Q19 canonical profile v1](../questions/Q19-content-hashes.md#принятый-canonical-profile-v1) defines six IDs, exact UTF-8/no BOM/structural whitespace/final LF, SHA-256 64 lowercase hex, strict Unicode/presence/number/FieldId/List emission. F13 consumes `cm-instance-content/1`, `cm-schema-content/1`, exact F10 `cm-publication-manifest/1`, and builds `cm-target-group/1`, `cm-installed-inventory/1`, request/plan variants `cm-sync-plan/1` (§3.3). Pure common library/typed normalized inputs shared server/SDK, no EF/Domain/Asp dependencies; supported profile before group apply, exact metadata no numeric guessing. NDJSON LF frames records and is excluded from content hash. Temporal URLs/credentials/clocks/storage paths excluded; media byte hash/Registry ETag/session SHA verifier independent. A byte change for old input requires new profile; historical triples never rehashed. Hash doesn't authenticate/prove installation. TimeProvider handles session/audit; primary UTC observation after committed exact transition supplies VisibilityConfirmedAt, unchanged Q17. H3/H2/proxy/Unity compatibility still requires transport verification.

### 8.6. Crypto, DataProtection и response lease host adapter

Session generator uses server CSPRNG32 bytes, Base64Url wire encoding and private fixed32 SHA-256 verifier with CryptographicOperations.FixedTimeEquals. No reversible token in Session; raw key never persisted. Short token-only replay ciphertext is deliberate exception, purpose binds exact SessionId+CreationRequestId with versioned purpose and same application discriminator. Protect/Unprotect/keyring I/O outside gate; final authorization after crypto includes monotonic age/deadline/current selected eligibility.

MVP keyring shared/persistent across replicas, explicit encryption at rest and restricted access; physical storage/encryption/provider/rotation/retention configuration engineering Q20. Temporary keyring/network/DB failure cannot become NoReplay. Proven expired/unrecoverable capsule returns typed NoReplay; preserve original metadata/outcome/known-ID tombstone so cleanup never rebinds old confirmed request as new create. Capsule5min default, no secret logs/error details.

Runtime response adapter links RequestAborted+independent monotonic lease deadline+known selected invalidation to serialization/YARP upstream cancellation. Lease renewal is a separate fresh short checkpoint; timer does not await renewal/gate/DB, canceled generation never revived. Headers/cache304/range/proxy start only after checkpoint, every resume/new range fresh; private S3 uses server storage auth and CM Authorization/token never forwarded. After headers abort transport, SDK treats failure Unknown and never attaches old plan to new admission; installed content unaffected. Configured5s is authorization lease bound, not observed physical network revoke latency or byte recall.

## 9. Data flows

Demo Api → Application plan/ownership → public ContentModeling exact reads → pure diff/cache → summary; client fetch → staging/atomic group F19 → receipt → separate freshness. MVP Publishing fact → Delivery full materialize/local effective commit; new-session Api → VRS validation/F12 decision → own pinned session; protected read → own authorization/membership → snapshot JSON либо Host YARP media bytes after gate/cache ordering.

| Поток | Транспорт | Метод / канал | Endpoint / destination | Доступ | Вход | Application action | Выход | Успех | Ошибки |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| DF01. Demo Start | HTTP JSON | POST | /api/v1/content-synchronizations/{synchronizationId}/start | Demo anonymous | StartSynchronizationRequest mandatory arrays; route Guid | StartSynchronizationCommand | ApiResponse<SynchronizationPlanDto> | 200 after complete retained plan | 400 invalid/duplicates;409 active/context; unavailable common Q15, no partial result |
| DF02. Demo state | HTTP JSON | GET | /api/v1/content-synchronizations/{synchronizationId} | Demo observer | Route Guid | GetSynchronizationQuery | ApiResponse<SynchronizationStatusDto> | 200 coherent retained state | 404 unknown/restart;410 known expired/discarded tombstone; outage not Missing |
| DF03. Demo Resume | HTTP JSON | POST | /api/v1/content-synchronizations/{id}/resume | Demo anonymous | ResumeSynchronizationRequest | ResumeSynchronizationCommand | ApiResponse<SynchronizationStatusDto> | 200 original snapshot/eligible idle | 400 malformed;409 live/stale/context;404 unknown;410 tombstone |
| DF05. Data stream | Streaming HTTP | GET | /api/v1/content-synchronizations/{synchronizationId}/data?planId=...&cursor=... | Pinned plan context | route Guid+required query planId Guid+optional cursor string | GetSynchronizationDataQuery | Raw application/x-ndjson protocol1 | 200 stream; full SDK success requires terminal/commits/ACK/response end | 400 malformed cursor;409 context/active;404 unknown;410 tombstone; after headers invalidation0 or truncate |
| DF06. Group receipt | HTTP JSON | POST | /api/v1/content-synchronizations/{synchronizationId}/groups/{schemaVersionId}/commit | Matched plan | route IDs + SynchronizationGroupCommitRequest | CommitSynchronizationGroupCommand | ApiResponse<SynchronizationGroupCommitResult> | 200 original ACK/repeated identical ACK | 400 malformed;409 mismatched plan/group/evidence;404 unknown;410 tombstone; unoffered/future group409 |
| DF07. Stop | HTTP JSON | POST | /api/v1/content-synchronizations/{id}/stop | Demo anonymous | SynchronizationControlRequest | StopSynchronizationCommand | ApiResponse<SynchronizationStatusDto> | 200 original stopped snapshot | 400 malformed;409 stale/context;404 unknown;410 tombstone |
| DF08. Discard | HTTP JSON | POST | /api/v1/content-synchronizations/{id}/discard | Demo anonymous | SynchronizationControlRequest | DiscardSynchronizationCommand | ApiResponse<SynchronizationStatusDto> | 200 original discarded snapshot/replay | 400 malformed;409 stale/context;404 unknown;410 known unavailable without matching replay receipt |
| DF09. Freshness/preflight | HTTP JSON | POST | /api/v1/content-synchronizations/freshness | Demo anonymous | SynchronizationFreshnessRequest | CheckSynchronizationFreshnessQuery | ApiResponse<SynchronizationFreshnessResult> | 200 complete current comparison | 400 invalid; source unavailable common error, SDK Unknown |
| DF10. Demo asset | HTTP gateway F05 | GET/HEAD | `/storage/assets/{assetId}` Q21 | Demo no auth | AssetId/conditional-range profile | Host route/eligibility → YARP → private storage | Immutable bytes/content headers | Actual provider/gateway profile F05 | Missing/lifecycle/range/cache/gateway/storage failure |
| DF11. Project transition | Broker wakeup + public source history | Publishing trigger | Delivery ordered projector | Trusted trigger/current public source | Variant+PublishingSequence/completeness | ApplyPublishingTransitionsCommand | Original processing outcome/actual history+Pending evidence | One real transition per own commit or known terminal non-effective receipt | Gap/source/artifact/integrity/ban/commit conflict |
| DF12. Terminal withdrawal | Broker/public invalidator profile | Exact ban | Delivery restriction consumer | Trusted terminal fact | Exact revision/Variant/identity | MarkPublicationWithdrawnCommand | Persisted restriction | Own commit/ACK | Replay/order/unknown snapshot/outage |
| DF13. Create session | HTTP candidate + server HTTP | POST | delivery-sessions map §5 | Validate key then entitlement | CreationRequestId/full raw-key set/VariantId | CreateDeliverySessionCommand | Pinned session/credential | Local admission commit/status Q15 | Key/outage/data-not-ready/no-grant/ban/expiry |
| DF14. Manifest | Protected HTTP candidate | GET | session/manifest | Exact session credential | SessionId/conditional header | GetDeliveryManifestQuery | Exact manifest/hash | Authorized read/conditional result | Session invalid/revoked/expired/withdrawn |
| DF15. Item | Protected HTTP candidate | GET | session/items/{versionId} | Session + membership | Exact version ID | GetDeliveryItemQuery | Stored exact content/hash | Authorized immutable read | Outside pinned composition/unavailable/ban |
| DF16. Asset | Protected HTTP YARP gateway | GET/HEAD | `/storage/assets/{assetId}` Q21; protected context Q16 | Session + membership before cache/proxy | AssetId/credential/range/cache context | GetDeliveryAssetReadAccessQuery + applicable gate → YARP | Raw bytes/headers/gateway result | Authorized forward/cache profile | Outside membership/ban/lifecycle/storage/in-flight profile |
| DF17. Confirmed revoke | Broker/public invalidator profile | Credential/license/grant fact | Session invalidation consumer | Trusted relevant basis | Non-secret basis/version/ID | RevokeSessionsCommand | Revoked own sessions | Commit/ACK; later refusal | Replay/source lag/commit/race |
| DF18. Confirm committed visibility | Internal durable stage | Own command | Delivery confirmation handler | Trusted committed transition identity | Variant+PublishingSequence+HistoryId | ConfirmDeliveryVisibilityCommand | Original/new saved observation evidence | Pending→first confirmed own commit; not commit-time stamp | Original commit unknown,clock/order failure,ACK unknown |
| DF19. Recover outcome/time | Internal recovery | Own query/command | Delivery recovery handler | Exact original identity | Variant+PublishingSequence,optional known HistoryId | RecoverDeliveryVisibilityCommand | Stored outcome/time or fresh observation | Unique original outcome/first timestamp retained | Integrity conflict,missing history,pending/time-notready |
| DF20. Creation replay | HTTP logical create/recovery | Original request; exact URI Q15 | Original creation boundary | Fresh full proof/original caller context | CreationRequestId/full keys/Variant | RecoverDeliverySessionCreationCommand | Exact original token/metadata or typed NoReplay | Unprotect outside FINAL fresh gate; no TTL extend | Changed input/context, unavailable, aged proof, capsule/session ineligible |
| DF21. Response checkpoint | Internal Application before HTTP response | Short authorization command | Delivery protected manifest/item/progress/asset | Named exact session bearer | SessionId/ResponseId/resource identity | AuthorizeDeliveryResponseCommand | Pinned decision/ephemeral lease | Own ReadCommitted gate fresh authorization, release before headers | Preheader401/403/409/503/404 semantic classes Q15 |
| DF22. Lease renewal/cancel | Host timer + internal command | Periodic checkpoint / independent deadline | Active response writer/YARP | Same exact selected session/response | ResponseId/SessionId/expected generation | RenewResponseLeaseCommand + scheduler | New lease or terminal response cancellation | Independent timer aborts stalled renewal/upstream/writes | Gap/denial/deadline; late renewal never restores |
| DF23. Credential security facts | Trusted HTTP proof/broker | Per-credential epoch | Own Delivery security handler | Authenticated normalized source | Nonsecret credential/security meaning/epoch | ApplyCredentialSecurityFactCommand | Ordered own state/scoped terminal invalidation | Atomic state/inbox; older no overwrite | Same epoch contradictory meaning integrity fault, required projection unknown |


### DF01. Создать exact Demo plan

1. Api binds route Guid and required non-null selection/installedInventory arrays, maps StartSynchronizationCommand. Explicit empty arrays valid; duplicate normalized identities/invalid values reject400. Same-ID active reservation409; lost response reconciliation uses Status, not generating IDs repeatedly.
2. Application atomically reserves SynchronizationId/PlanId/original input/CreatedAt/ExpiresAt, generation1 and private preparation owner; Status can observe preparing/idle without PlanDTO. Capture coherent authoritative source/desired absence outside coordinator guard. No SQL/cache partial successful plan if selected input/source unavailable. Inventory declaration is comparison input, not authorization or install proof.
3. Public ContentModeling reader pins exact latest Published within each selected structure plus frozen metadata/hash/profile; unavailable proof isn't empty. Deselected/withdrawn proven desired absence produces Remove, not a failed-query mass cleanup.
4. Build immutable full normalized CanonicalPlanInput incl unchanged complete target memberships/removals and original request fingerprint. Pure diff produces changed execution Groups only; group base hash from original inventory and full target hash use cm-target-group/1; counts/order checked Int64/no overflow. Record layout1..N derived once, no full entity payload array in Start.
5. Reuse server Guid PlanId allocated at reservation, preserve SynchronizationId from SDK; publish complete cache result only under still-current preparation generation/lease before JSON200 ApiResponse<SynchronizationPlanDto>. No successful groups/errors mixture. Nochange groups:[] still retained real plan/Data OperationCompleted.
6. Absolute created TTL default1h/no stream extension, one-process bounded memory fixed. TTL starts reservation; active plans not arbitrarily capacity evicted, new Start unavailable at capacity. Expiry revokes generation/cancels worker, records bounded tombstone; delayed preparation cannot publish. Later ordinary publish does not change pinned targets.

### DF02. Прочитать статус/сохранённый результат

1. Bind route synchronizationId Guid; cache observer reads coherent immutable plan and execution/receipts without lease acquisition/recompute.
2. Existing retained state→200 ApiResponse<SynchronizationStatusDto>, preparing can lack ready plan. Known unavailable expired/discarded tombstone→410; unknown/evicted/restart missing→404. Infrastructure outage is failure, not fabricated Missing/Expired.
3. Map reserved planId/current executionGeneration even while preparing, planState/executionState separately and accepted receipts. completed means all server receipts, not installed proof/automatic SDK success. Observer doesn't mutate inventory or restart work. Late response applies only to captured ID/plan context.
4. Until retained plan expires, Data can replay OperationCompleted after lost terminal response; no new Start/target recompute under old ID.

### DF03. Продолжить

1. SDK acquires stable local store-scope single-run gate before new ID; captured local run generation fences callbacks/parser/control responses across SDK instances. Load durable original context and local receipts, then Status; no network error/409/new publication silently replaces ID.
2. POST /api/v1/content-synchronizations/{synchronizationId}/resume with ResumeSynchronizationRequest. Under short coordinator guard check exact plan/original fingerprint/profile, eligibility and caller generation. Retained identical control receipt returns original snapshot; stale changed intent409, live worker409.
3. For permitted stopped/idle attempt increment generation and enable idle; ready plan target unchanged. If never ready, retain original input and allow first preparation to finish/restart under new preparation fence. Resume does not open/reserve Data response; reply200 ApiResponse<SynchronizationStatusDto>.
4. Reconcile durable pending receipts without reapplying installed groups (after permitted Resume if stopped), then request Data checkpoint; Data acquires own generation/lease. Completed retained plan allows terminal recovery. SDK rejects obsolete local-run responses; replayed control snapshot is not latest Status.
5. New plan only after proven old missing/expired/discarded/invalidated or explicit new selection intent; build from actual installed inventory, do not blindly reuse staging. Unknown terminal response replays terminal suffix/deduplicates.

### DF05. Выдать resumable data stream

1. Bind route synchronizationId + required planId query + optional cursor. Validate syntax; resolve retained plan/tombstone/missing first; confirm PlanId then verify signed cursor MAC/identity/sequence. Initial no cursor starts1. Atomically acquire fresh execution generation/lease; competing live owner409, stopped requires Resume. Return captured generation in X-CM-Sync-Execution-Generation before body; check current fence before each new record/mutation/renewal, no target recapture.
2. Read pinned exact Published payloads/metadata via public owner queries; validate stored canonical triple/registered profile and current safety-sensitive availability. No latest/enrichment/serializer rehash; absent authority/error not empty target. Group execution order and operation layout fixed from plan.
3. For each changed group emit groupStarted summary, EntityUpsert(add/update) or EntityRemove by InstanceId. Immediately before groupCompleted, atomically register offered boundary under current fence; then send boundary counts/hash outside guard. New owner resumes same records; receipt for unoffered/future group409. Envelope group schema IDs explicit, normal sequences original1..N and candidate cursor. Upsert contentJson is JSON string decoded from strict original UTF8; server never reserializes entity object to construct hash.
4. Api writer frames compact UTF8/noBOM+LF, bounded one-record buffers, cancellation/backpressure. SDK parser accepts LF/CRLF and rejects empty/duplicate/malformed/oversize/invalidUTF8/truncated tail. Unescape contentJson→exact UTF8→profile/digest verification before typed decoding. No whole dataset materialization.
5. SDK records exact header/operation/payload and candidate cursor atomically in durable staging after validation. Resume mid-group uses preserved header/records; equal parsed identity/full payload/content replay dedup, differing same seq error. Cursor excludes lease generation; no partial tail checkpoint.
6. On groupCompleted verify plan/boundary/actual unique counts, original local base group when not already installed by own receipt, and full resulting target incl unchanged members after removals/upserts. Atomically data/inventory/index/local receipt commit; SDK posts DF06. Server waits receipt before next group, receipt endpoint independent of stream lease; finite receipt watchdog may interrupt recoverably; renewal only current unexpired lease before absolute deadline.
7. Final operationCompleted payload{} has normal sequence/cursor, including nochange single record. Full SDK success requires valid terminal+all local commits/server ACKs+correct response end; EOF alone, Status.completed alone, partial group or truncated response isn't success.
8. If invalid after headers emit operationInvalidated sequence0/no cursor, safe reason/message and optional matched schema scope, then close; if frame can't be sent correctly, truncate. This terminal is outside immutable log, never replaces previously issued seq. Keep committed groups, stop incomplete work and use new plan only after proven old plan unusable; retry/H3-H2 retains same ID/cursor when old plan valid.

### DF06. Зафиксировать group receipt

1. SDK posts route operation/schemaVersionId plus body planId/targetGroupHash/profile only after durable local atomic apply. No client actor/lease/generation/progression/installation certificate body added.
2. Application checks retained plan/group identity and full target evidence; malformed400/mismatch409, missing404/known unavailable tombstone410. First receipt must match next unconfirmed group whose GroupCompleted boundary was offered under current fence. Identical accepted replay returns original ACK; ownership replacement does not erase offered/receipt plan facts.
3. Cache atomically retains original SynchronizationGroupCommitResult keyed PlanId+SchemaVersionId+TargetGroupHash+profile: repeated identical returns same JSON200 ACK; other evidence409. Original request fingerprint/target/canonical bytes unchanged. Waiting data writer can advance only after accepted receipt under its valid fence; receipt POST isn't blocked by data-stream lease.
4. Lost ACK: Status receipts/durable local receipt lookup and idempotent POST repeat, no second local apply. Server ACK never proves filesystem; expired/missing server cache doesn't undo installed groups. Whole operation completed receipt state is distinct from terminal transfer/SDK success.

### DF07. Остановить с продолжением

1. SDK stops read/network work and preserves staged records/cursor/original request/installed data. Already begun short atomic local group commit finishes to known result; retain its durable receipt, ACK may defer until Resume.
2. POST /api/v1/content-synchronizations/{synchronizationId}/stop with SynchronizationControlRequest. Under coordinator guard check retained idempotent action receipt first, then matched plan and current expected generation; stale409 never retargets.
3. Increment/revoke generation, mark stopped, retain plan/input/offered groups/receipts, atomically save original Status result receipt. Signal revoked worker cancellation outside guard. Late worker/finally cannot release newer owner or mutate state.
4. Return200 ApiResponse<SynchronizationStatusDto>; repeated identical action returns original snapshot with no current-state change. Timeout/unknown ACK reconciles same ID/Status, no new attempt by inference.

### DF08. Отказаться от попытки

1. SDK first stops execution and resolves begun atomic local commit to known installed/receipt result. Unknown local commit blocks destructive cleanup; operation absence alone cannot prove commit failed.
2. POST /api/v1/content-synchronizations/{synchronizationId}/discard with SynchronizationControlRequest. Exact replay receipt returns original result; otherwise matched plan/current generation required or409.
3. Revoke/increment generation, mark discarded and save bounded tombstone/original control result atomically; cancel old worker outside guard. Ready payload resources can be released, retained replay evidence is bounded and contains no full payload/signing key.
4. Return200 ApiResponse<SynchronizationStatusDto> original snapshot. Subsequent lookup known discarded410, after eviction/restart404. SDK clears only unfinished attempt/staging, preserves installed data/inventory/committed receipts. New explicit attempt uses actual installed inventory.

### DF09. Проверить актуальность после успешного apply

1. SDK POST /api/v1/content-synchronizations/freshness after complete success including no-change, with SynchronizationFreshnessRequest full selection/actual installedInventory.
2. Persistence query adapter obtains coherent authoritative current exact desired data, validates complete selection/inventory, reuses pure diff comparison without plan/lease/session. No failed query treated as empty desired state.
3. Return200 ApiResponse<SynchronizationFreshnessResult>: outcome upToDate/changesAvailable, checkedAt UTC seven-digit Z, changedGroups sorted exact schemaId/schemaVersionId pairs. Comparison failure is normal API error, never successful partial comparison.
4. SDK failure maps to Unknown without undoing successful installation; ChangesAvailable does not automatically start endless synchronization. checkedAt reports comparison instant, not future guarantee.

### DF10. Demo asset bytes

F05/DF04: CM-owned media URL → Host read-only gate/deterministic mapping → YARP → private storage → raw binary/HEAD/range/conditional response. Demo не требует licensing/auth; state visibility/headers/proxy errors и cancellation/lifetime — Q21. Application upload/delete и metadata остаются F05; клиент не скачивает обязательный presigned S3 URL. Byte failure отдельно от successful sync group, installed content/AssetId unchanged.

### DF11. Применить Publishing transition

1. Trusted bus trigger only wakes catch-up. Primary public Publishing query returns ordered **committed** history plus completeness/high watermark after LastProcessedPublishingSequence; receipt of event15 while12 processed requires13/14 first. Missing history is NotReady, not no-events.
2. Outside gate obtain preliminary authoritative exact terminal status **before mandatory artifact prep**. Known banned bypasses artifact/staging, then fresh guarded ban recheck. Otherwise stage complete exact immutable graph and verify stored F10 manifest profile/digest/bytes against exact schema/instance triples. Copy SAME manifest bytes; no alternate manifest serialization/live enrichment. Unknown profile/corruption remains technical/unconsumed, never rehash/auto-repair. Recovery separately rechecks authoritative ban. Batch prep allowed; no external byte/network I/O/streams/broker wait under gate.
3. New own Delivery ReadCommitted transaction/first global advisory gate; fresh own state/receipt/inbox and primary public exact source/order/authoritative terminal restriction checks. Existing same Variant+PublishingSequence+meaning → original outcome/evidence; different meaning → integrity conflict.
4. Known terminal Withdrawn target → atomically save NotEffectiveTerminalRestriction receipt+LastProcessedPublishingSequence/inbox; no effective history/pointer/Pending visibility row. Technical missing/unreadable artifact is not terminal evidence: leave unconsumed, previous effective state may serve if other rights/restrictions allow.
5. Otherwise after fresh protected basis recheck commit **one** actual effective transition: full snapshot with exact canonical bytes/profile/digest and complete copied graph if needed + pointer/Version + immutable DeliveryActivationRecord (including explicit null) + Pending evidence + processing receipt/sequence+inbox together. Triple cannot tear; prepared bytes aren't permission to commit. No fake R1/R2/R3 visibility from one batch commit. Rollback old artifact is a new transition/order, preserving its original canonical triple.
6. ACK/progress after durable outcome; lost commit ACK reconcile exact primary stored receipt/history before confirmation/retry. Current committed full snapshot may already serve if otherwise permitted; temporal readiness remains Pending until DF18 evidence durably saved. Stored replay never adds history/materializes again or changes timestamp.

### DF12. Применить exact terminal ban

После проверки transport shape/trust этот owner write commit начинает ReadCommitted transaction и отдельным первым SQL statement получает global pg_advisory_xact_lock; затем fresh own tracked state и независимые primary public facts (§7.2). Следующая стадия/consumer commit получает новый gate; ACK/dispatch/stream снаружи.

1. Consumer validates exact revision/Variant/restriction identity even if snapshot inactive/frozen/not materialized; dedup trusted incoming fact.
2. После gate/fresh owner public authoritative exact ban basis persist terminal restriction even inactive/unmaterialized. Mark existing snapshot unavailable; no current-pointer requirement. New admission and protected reads enforce ban before cache/304/range; any unknown restriction evidence not Allowed. Existing in-flight/session enforcement follows DF21–23; independent deadline aborts blocked renewal.
3. Commit independent restriction/status/inbox; later materialization/activation/replay cannot lift ban. Existing history/confirmation remains historical evidence, never permission to bypass ban. Active F11 null transition processed DF11 separately; unrelated current revision not cleared.
4. ACK/observed protected refusal differ from producer Withdrawal commit; propagation/admission/in-flight guarantee Q16/Q17/Q20. No fallback/replacement/deletion of historical data/installed bytes.

### DF13. Создать protected session

1. SDK сохраняет CreationRequestId и nonsecret intent/provider context до первого POST; keys предоставляет application credential provider. Api принимает полный raw-key set+Variant+ID, не client-verified IDs. CM monotonic age начинается до каждого VRS HTTP attempt; every necessary key proof trusted/complete.
2. Server строит OriginalMeaning из Variant и нормализованного verified CredentialId/LicenseId set. Preliminary own receipt read направляет confirmed same-ID в DF20; other caller не получает original result, changed meaningful input конфликт. Raw key/hash/volatile epochs/audit time не входят в fingerprint.
3. Для нового ID server allocates SessionId, 32 CSPRNG bytes Base64Url credential/SHA256 verifier и Protect ONLY token bound SessionId+RequestId outside gate. Shared keyring I/O/crypto входит в age. Technical failure retry same ID.
4. Own Delivery ReadCommitted transaction → global gate first → fresh own receipt/security/session/binding + independent primary public Catalog/Publishing facts. Recheck attempt age (default30s), every credential epoch/deadline, required license watermark/completeness/current periods/Agreement/F12/final closure/exact ban. Expired evidence повторяет HTTP outside gate. Concurrent confirmed receipt redirects recovery; no duplicate session.
5. Any necessary unknown across explicit verified set gives whole Indeterminate. Choose most current allowed whole candidate; exact candidate tie min LicenseId, then min CredentialId. Pin selected basis only. ExpiresAt=min(AdmittedAt+configured30m, finite Deny cutoff, explicit credential deadline), never application ExpiresAt.
6. Atomic own commit session/verifier/pin/epoch/absolute expiry + immutable original receipt + short encrypted capsule(default5m) + required own binding/inbox/intents; no shared owner transaction. Release gate before HTTP; ACK unknown → same CreationRequestId DF20, not new session/TTL extension.

### DF14. Получить manifest

1. Named CmDeliverySession authentication resolves bearer/verifier to exact SessionId; FixedTimeEquals fixed-length SHA256, no staff fallback/SessionId/hash authorization.
2. DF21 checkpoint validates selected pinned basis/current rights before headers/cache/304. Read exact pinned manifest/hash; newer activation/Unpublish/Retire/archive do not repin.
3. Response lease applies while sending; blocked/failed renewal independently aborts. VRS resource HTTP отсутствует; progress repeats same pinned checks.

### DF15. Получить item

1. DF21 checkpoint checks exact EntityInstanceVersionId membership in pinned snapshot before payload/cache/304. Outside composition404; no guessed-ID live-schema bypass.
2. Read copied exact content/schema/key/hash; missing/corrupt dependency diagnosed integrity/materialization failure, not allowed empty. Public schema profile remains F21/Q15.
3. Response lease releases no long-lived gate; after-header cancellation transport abort means SDK Unknown, not assumed Revoke.

### DF16. Protected asset bytes

1. Every GET/HEAD/Range/resume starts fresh DF21 authorization for exact AssetId membership/pin/basis/ban, then applicable public MediaLibrary availability; no cache/304/Range bypass.
2. Release gate before cache headers/YARP/storage/bytes. Host server storage authentication only; CM bearer never forwarded to S3. Provider/cache/validators profile remains F05/Q21.
3. DF22 independent monotonic lease cancellation reaches YARP upstream and downstream writer. Already buffered/sent bytes cannot be recalled; default5s is authorization horizon, not physical network SLA. Late renewal cannot resurrect cancelled response.
4. After headers refuse by abort selected transport; SDK keeps installed data and reports Unknown. It cannot attach old plan to new session; asset failure does not rollback installed groups.

### DF17. Отозвать relevant sessions

1. Authenticated normalized invalidator identifies source version and exact credential/license/Variant grant/revision scope. HTTP security knowledge and broker share DF23 epoch ordering; unselected credential/license changes do not cancel selected session.
2. Own ReadCommitted transaction first global gate, fresh own sessions/security plus independent primary public facts. Persist first terminal InvalidatedAt/reason for confirmed revoke/exact ban, forward selected security epoch or selected period replacement/confirmed pinned refusal; immutable pin/receipt/expiry remain unchanged.
3. Temporary required-data gap is DataNotReady, not fabricated Revoke/terminal invalidation. Genuine correction uses F12 pinned evaluation: allowed continues original TTL, confirmed disallowed cannot be restored by later extension; renewal needs new admission.
4. Atomic own state/inbox then ACK outside gate; duplicate fact idempotent. Restore requires new admission, no resurrection/fallback. Known active response leases cancel; otherwise independent deadline bounds renewed authorization. External unknown fact before CM receipt is not instant knowledge.

### DF18. Подтвердить committed visibility

1. Reconcile original transition outcome on primary by Variant+PublishingSequence. Only EffectiveCommitted with exact complete history/snapshot (or exact null history) may confirm; no processing receipt/history or unknown original commit →pending/integrity/recovery, not fabricated stamp.
2. Open a **new own Delivery ReadCommitted transaction** and first global advisory gate after original commit. Fresh lookup exact history ID/source identity/full stored snapshot on primary; current pointer may already advanced and does not need equal this revision. Confirm oldest necessary pending first per Variant.
3. Already durably Confirmed → return original saved VisibilityConfirmedAt. Otherwise read primary PostgreSQL UTC observation clock **after** committed exact data re-read; validate consistency with earlier confirmation/effective order. Do not use host UtcNow-before-commit, original commit metadata, pg_track_commit_timestamp or clock of second commit.
4. Conditional own evidence Version writes first Confirmed timestamp and safe evidence once; persist precision as read, no second rounding/max clamp/backdate. Detected clock/order failure yields affected temporal DataNotReady, never invented successful time. Confirmation does not materialize or change pointer/history/ban.
5. The saved observation time is not this new transaction's commit time. Evidence remains Pending until its durable save; lost save ACK performs exact stored lookup. Commit-before-cutoff/confirmation-after-cutoff is intentionally excluded from Freeze; strict stored VisibilityConfirmedAt < UpdatesUntil, equality excluded.

### DF19. Восстановить outcome/time после crash или unknown ACK

1. New own primary scope/gate reconciles original processing receipt/meaning/history/full projection after uncertain first commit. Same tuple different meaningful source input integrity conflict; known terminal NotEffective returns original outcome without effective row/time.
2. If actual committed history/evidence exists, return original durable outcome and saved confirmed time unchanged. Exact old transition can be recovered even pointer later advanced/null.
3. If confirmation not saved, process oldest necessary pending under DF18 with **fresh** primary observation; no reconstruction from event time/precommit UtcNow/server logs/no backdate. If original commit not established, resume exact source processing DF11 under fresh guards; never confirm nonexistent/partial transition.
4. Concurrent consumers/recovery converge unique processing receipt/history and first evidence winner using own Version/constraints. Different source meaning integrity failure; missing required source range/current temporal evidence NotReady.
5. Temporal read scopes necessary completeness to decision purpose/cutoff prefix and distinguishes complete empty, known null, required pending, missing history and invalid clock order. Later unprocessed source that cannot backdate does not invalidate proven past prefix; no blanket live watermark readiness veto. Expired Freeze never skips unknown/pending to older/non-null state; current committed otherwise-allowed snapshot may serve independently.

### DF20. Recover confirmed creation / replay capsule

1. Same CreationRequestId+raw-key set obtains fresh trusted full proof; preliminary own receipt/context read confirms same trusted caller and OriginalMeaning. Confirmed ID never becomes new create after cleanup.
2. Read original capsule; Unprotect ONLY token and validate purpose/scope/SessionId/RequestId + fixed-length stored verifier outside gate. All HTTP/crypto/keyring latency belongs attempt age. Temporary database/keyring failure retries same ID; proven capsule expired/unrecoverable yields typed NoReplay/new ID fresh admission.
3. FINAL own ReadCommitted gate first, fresh receipt identity, capsule Version/state/ReplayUntil, proof age/epochs/deadlines/watermark, original session absolute expiry/terminal state and selected F12 pinned eligibility. Necessary unknown blocks disclosure; no new candidate, TTL extension or restore.
4. After allowed guarded check return exact original token/metadata. Unknown commit ACK queries stored receipt/result; no raw secret reconstruction from hash. Tombstone/original outcome survives physical cleanup profile Q20.

### DF21. Short response authorization checkpoint

1. Authenticate exact bearer and request route; open own Delivery ReadCommitted transaction/global gate first. Fresh own session/verifier/security and independent primary owner facts cover selected license/current period/agreement/cutoff, pinned membership/final closure/exact ban.
2. No VRS HTTP: enforce absolute expiry/finite credential deadline and selected epoch equality; forward epoch/current period replacement → NeedsNewAdmission. F12 EvaluatePinnedRevision evaluates original pin; required unknown → DataNotReady, no alternate-license fallback.
3. Pure decision: active updates may permit old R1; finite Deny cutoff refuses at equality; expired Freeze requires exact eligible confirmed frozen whole candidate equal pin or NeedsNewAdmission. Correction never extends stored TTL.
4. Map before headers:401 bad/expired session;403 confirmed rights denied;409 basis changed/new admission;503 proof/required-data unknown;404 outside pinned composition. Exact codes/DTO Q15. Successful checkpoint creates ephemeral lease <=min(now+configured5s, session expiry, next finite policy boundary including Freeze); release gate before response/cache/304/Range/proxy.

### DF22. Renew/cancel response authorization lease

1. Host installs independent monotonic deadline before emitting headers/bytes. Periodic renewal runs new DF21 short checkpoint for same response/session/pin with expected generation, never holds gate during stream.
2. Timer cancels downstream writes and upstream YARP even if renewal is stalled on gate/database/projection; unknown/denied check cancels too. Known exact ban/revoke may signal earlier cancellation. Late renewal generation cannot activate cancelled/completed response.
3. Finite policy boundary may require a new HTTP response even when same session remains valid; every resume/range repeats fresh checkpoint. Cancellation cannot recall buffers; no zero-latency or measured5s transport guarantee.

### DF23. Apply credential security knowledge

1. Trusted HTTP proof/broker normalized fact supplies CredentialId/LicenseId, security epoch/stable meaning and explicit deadline. Source security change+outbox atomic in VRS; raw keys absent. Source contract profile Q23 is implementation work, not inferred from stable long ID.
2. New own Delivery transaction first gate, fresh state: older ignored; same epoch/equal stable meaning idempotent; same epoch/different stable meaning integrity fault; newer stores own state and necessary selected-session terminal invalidations atomically/inbox.
3. Validation/audit time/general metadata/commercial correction do not advance security epoch. Newer Valid/restore/rotation still requires new admission for old captured epoch; unselected epoch irrelevant. Required license watermark/completeness stays independent and finite deadline enforced without event.

## 10. Ошибки и диагностика

| Situation | Result / recovery |
| --- | --- |
| Missing/duplicate/malformed full selection/inventory | Validation/group error; no “valid empty” removal plan |
| Requested schema invalid/unavailable | Error affected group, preserve installed group; no latest/schema substitution |
| Active competing same ID | 409 accepted; recover prior context, no new-ID bypass |
| Required temporal history/evidence pending or missing | Indeterminate/DataNotReady; cannot skip older/non-null; complete empty/known null distinct |
| Source same Variant+Sequence different meaningful payload | Integrity conflict, preserve original receipt/history/time; no silent overwrite |
| Detected primary clock/order failure | Affected temporal decision DataNotReady, no max clamp/rounded/backdated timestamp |
| Plan missing/expired vs timeout | Distinct impossibility vs unknown; new ID only conclusively impossible continuation |
| Invalid cursor/plan/hash/receipt/late owner | Typed profile Q33; no fake group complete or repeat install |
| Unknown canonical profile/required digest profile absent | Contract/integrity failure before group apply; no bare hash comparison/numeric guessing |
| Corrupt canonical bytes/digest mismatch/invalid normalized Unicode or number | Preserve original stored triple and installed group; no rehash/rounding/default fill/auto-repair; MVP transition remains unconsumed technical NotReady |
| Full target after removals/upserts differs from bound group digest/profile | No local atomic apply receipt; old active group retained, plan unchanged |
| Planned exact Demo withdrawal | Invalidated affected group/new plan from actual inventory; ordinary newer publish leaves old plan |
| Post-sync check fails | Client Unknown freshness, successful installed result retained |
| Key invalid/revoked vs upstream outage | Distinct credential denial/validation unavailable; no stale new-session fallback |
| Key valid, necessary projection/agreement/current-period/history/visibility evidence incomplete | Whole Indeterminate/DataNotReady, not InvalidKey/known none; complete current committed snapshot need not wait temporal evidence when otherwise allowed |
| Bad/expired session; confirmed rights refusal; selected basis changed; required unknown; outside snapshot | Preheader401 /403 /409 /503 /404 respectively; exact machine codes Q15; after headers abort/SDK Unknown |
| Resource outside pinned composition | Membership refusal, no payload/bytes/cache304 bypass |
| Materialization gap/partial integrity/source read outage | Previous effective preserved; don't backdate/update incomplete target |
| Asset/storage/range error | Separate byte failure; no Demo group rollback or source identity rewrite |

Session preheader semantics selected above; common machine reason registry Q15, Demo public statuses400/409/404/known410 and core DTOs fixed Q33; execution/control profile selected; numeric budgets require configuration/measurement and target DTOs require implementation. Log safe sync/session/revision/group/Sequence/correlation IDs, cache hit/miss/ownership/recovery, reason classes, materialization/transport protocol observed metrics; no key/session credential/storage secrets/payload JSON. Stream/download/access logs/redaction apply to headers/body/query too. Session defaults selected §3; physical configuration/keyring/replay retention/transport measurement Q15/Q20/Q23, Demo limits Q33 and targets Q22. No misleading “client applied” solely from data response/record cursor or server receipt.

## 11. Тестирование
Реальные проверки серверного Demo: полный App integration набор — 58/58, включая существующие F01–F05 и F13; полный Delivery unit набор — 125/125, включая deterministic TimeProvider/lease/control/cursor/canonical regressions; owning ContentModeling source — 7/7 на PostgreSQL; App architecture — 44/44. Socket fixture проверяет Kestrel HTTP/2/TLS с одним connection: LF records появляются до EOF, отдельный receipt проходит при открытом data response, затем приходят terminal и корректный EOF. Fixtures используют собственные disposable данные; эти результаты не подтверждают HTTP/3, deployed proxy, representative/load ceiling или F19/F20 durable client/Unity implementation. Наличие ACK не доказывает local installation. Проверочные hosts/listeners завершаются; codegen выполняется через отдельный временный Windows API.


Post-Demo resolver/manifest acceptance: pure Unit corpus exact schema×values и real owner PostgreSQL projection, full authorized inherited/local/override/exclusion composition, optional absence, nested recursive definitions, один AssetId в нескольких полях с разными modes, две версии одной схемы/разные Variants, descriptor inconsistency и historical default без rehash. Предлагаемая strictest aggregation проверяется после утверждения Q37. Functional tests сохраняют literal F13 wire v1/receipts/hashes, independent asset failures и fresh preheader/lease checks для GET/HEAD/Range/304/206. E2E требует реальные launcher+Unity Windows x64 IL2CPP `2022.3.62f3`, shared SQLite adapter, CM/PostgreSQL/YARP/MinIO, pinned old/new snapshots и controlled network/process faults; Editor smoke не подтверждает эту цепочку. Это future acceptance, не результат выполненных тестов.

[S04](../standards/S04-testing.md)/[S14](../../../catalog/docs/standards/dotnet/testing.md#test-environment-ownership-and-bounded-parallelism). Unit SUT: pure desired/diff/identity/validators/materializer/authorization policies with owned values/clock/adapter doubles. Integration: real owning PostgreSQL/Testcontainers snapshot/state/history/session/inbox/binding constraints, selected actual local cache adapter+deterministic ownership barriers; ContentModeling/Media public read contracts verified at owners. Functional: Demo/MVP HTTP Api/handlers/stream/error/credential contract with real SQL where needed; controlled VRS/storage/broker doubles label boundary. E2E: real necessary CM+SDK local store/Unity transport and source validation/broker/storage dependencies per scenario; multiple BC same API not automatically E2E.

| ID | Level | Scenario / DF | Expected result |
| --- | --- | --- | --- |
| G01 / DF11–13,17 | PostgreSQL Integration, two replica/owner scopes | Projector/admission/ban/revoke vs real Publishing/Catalog/policy writer under same global key | Fresh committed basis after gate; only own Delivery writes/intents/inbox; no stale allowed commit |
| G02 / DF13 | Integration + HTTP fault barriers | Slow VRS validator then CM revoke/policy/Withdrawal; lost owner connection/commit ACK; blocked gate cancellation | No VRS HTTP under lock; fresh committed CM checks; technical abort/unknown inquiry, not empty Allowed/reconnect continuation |
| G03 / Demo/read/stream | Architecture + Functional transport | Demo AssetId guard/plan and long running byte/record stream while MVP writer starts | Demo one-process path preserved; no full Publishing dependency; stream/read does not hold global write gate or promise instantaneous external revoke |
| P01 / DF13 | PostgreSQL Integration + F12 public readers | Freeze agreement→Deny change→late confirmation; missing agreement/projection/current period versus complete no grant | Exact Freeze binding/new admission; unknown DataNotReady not InvalidKey; complete no grant Denied |
| P02 / DF13 | Integration | New current renewal Deny/old Freeze; genuine same-period corrected cutoff; shared source period across Variants | New selection no old fallback; immutable per-Variant agreement/binding and current cutoff; renewal new admission; genuine correction evaluates original pin/cutoff/TTL |
| P03 / DF13 | Integration/Functional | Verified requested set with Allowed+necessary unknown; known Denied+Allowed; one credential organization context | Unknown set overall Indeterminate; known set selects whole Allowed; no implicit license expansion/item merge |
| V01 / DF11,18 | PostgreSQL Integration | Full transition commit before cutoff, primary confirmation/save after cutoff; equality boundary | Conservative stored confirmation excludes; no first/second commit time claim or second rounding |
| V02 / DF18–19 | PostgreSQL fault barriers | Crash after original commit before observation/save; lost confirmation ACK; two consumers | Pending until saved; stored timestamp winner replay or fresh recovery observation/no backdate |
| V03 / DF11 | PostgreSQL/public source Integration | Processed12 event15, required13/14 missing/available; one-commit batch attempt | Ordered catch-up/no silent gaps; missing temporal history NotReady; one actual history transition per commit |
| V04 / DF11,19 | PostgreSQL Integration | Known terminal banned intermediate vs technical missing artifact; same sequence same/different meaningful input | Ban consumed NotEffective/no history/no stall; technical missing not consumed; replay original versus integrity conflict |
| V05 / DF18,read | PostgreSQL Integration | Pointer advances before old exact/null confirmation; serial pending order; backwards primary clock | Confirm exact committed identity/full graph/null, not current pointer; order preserved/failure DataNotReady/no clamp |
| V06 / F12 | PostgreSQL Integration | Confirmed null before cutoff, late Unpublish, rollback old artifact, genuine cutoff correction same AgreementId | Strict last confirmed including null/no fallback; rollback new order/time; correction re-evaluates same agreed policy |
| V07 / DF12–16 | Functional/Integration | Inactive/unmaterialized exact ban + new admission/cache304/range/unknown owner restriction | Ban independent and no replay lift; restriction checks precede response; unknown not Allowed; independent response lease cancellation DF22 |
| S01 / DF13,20 | PostgreSQL + HTTP fault barriers, two replicas | Creation ACK loss/retry, same ID changed input/other caller, capsule expired/tombstone | Original session/token/TTL only; no disclosure or duplicate creation; typed conflict/NoReplay versus temporary failure |
| S02 / DF20 | Real shared DataProtection integration | Cross-replica replay/key rotation, protected payload tamper, keyring outage and slow Unprotect | Purpose/verifier/scope checked; temporary outage same ID; final age/deadline blocks late disclosure |
| S03 / DF13,23 | PostgreSQL/HTTP ordering barriers | Stale proof, gate wait, newer broker epoch versus HTTP, same epoch different meaning |30s starts before HTTP; older cannot overwrite; forward epoch invalidates selected basis; integrity fault for contradictory same epoch |
| S04 / DF21/F12 | PostgreSQL boundary tests | Absolute TTL/cutoff equality, renewal/correction expanded or reduced cutoff, expired Freeze pin mismatch | No sliding TTL/old-period fallback/repin; unknown temporary503, confirmed changed pin409 |
| S05 / DF17,23 | PostgreSQL integration | Credential/license/Variant grant/exact ban scopes; unselected license/epoch changes; restore | Only selected matching scopes deny; old terminal session never resurrected; new admission required |
| S06 / DF14–16,21 | Functional with real SQL | Token-route mismatch/full-proof missing, cache304/HEAD/Range/resume/outside membership | Preheader401/403/409/503/404 semantics; fresh checkpoint before any cache/header/bytes |
| S07 / DF22 | HTTP/PostgreSQL/YARP cancellation barriers | STALLED renewal waiting on gate/data; finite Freeze boundary; late renewal after timer | Independent monotonic deadline aborts upstream/downstream without renewal completion; no resurrection/buffer recall claim |
| S08 / DF16,22 | Real necessary storage/YARP integration | Range interruption/retry and client Unknown/session replacement | CM bearer absent from storage; every request reauthorized, old plan not rebound; installed data retained/no false app-run |
| T01 | Unit + Functional | DF01/09 exact multiple schema versions, Draft/latest order | Filter structure then highest Published; distinct instance representations coexist |
| T02 | Unit + Functional | Explicit empty vs missing, invalid schema/read error, duplicate inventory | Only explicit valid empty/deselection removals; failed group preserves data |
| T03 | Integration + Functional | Same ID racing Start/lost response/body mismatch/Resume | One immutable target/owner, active409; no replacement/latest recapture |
| T04 | Integration | Cache TTL boundary/eviction/restart/lease late writer | Chosen profile enforced; safe missing/resume/newID semantics, no installed deletion |
| T05 | Functional + E2E | DF05 incremental stream break/replay/truncated or oversized record/cursor/end | Full framed records only; durable staging before cursor, group end not commit, incomplete tail not success; complete empty distinct error; bounded memory |
| T06 | E2E | Group partial load, crash/verified staging/atomic data-removal-inventory | Whole group applied or old group retained; other complete groups independent |
| T07 | Integration + E2E | Receipt lost ACK/duplicate, stop during commit, discard/late response | Local committed result retained; idempotent receipts, unfinished only cleanup |
| T08 | Unit + E2E | New publish during plan vs explicit withdraw schema/version/instance | Plan target stable ordinary publish; withdrawn exact fetch invalidates; new plan scoped removals |
| T09 | Functional + E2E | Post-success incl no-change check changes/outage | UpToDate/ChangesAvailable current moment or client Unknown; no auto loop/rollback |
| T10 | E2E | H3 unavailable→H2; streaming proxy/Unity Player/storage restart | Same ID/plan/context, observed supported transport; no assumed compatibility |
| T11 | Integration | Projector reverse/gap/replay/crash/incomplete snapshot | Ordered full snapshot/state/history/inbox atomic; old effective until ready |
| T12 | Integration + E2E | Original full commit before cutoff, confirmation after; null/late Unpublish | Strict saved VisibilityConfirmedAt; delayed confirmation excluded, frozen null no fallback |
| T13 | Integration + E2E | Ban before snapshot/active/inactive/frozen/replayed transition | Terminal restriction survives materialization/replay; exact denied, unrelated state preserved |
| T14 | Integration | Historical source Retire/canonical bytes/schema/media retained | No live recompose/new-use failure; own exact stored graph/hash consistency |
| T15 | Functional | Session key invalid/valid-no-projection/outage/data sufficient | Distinct reasons; no stale new validation; protected admitted exact basis only |
| T16 | Functional + Integration | Token/route mismatch/expiry/revoke/Deny finite cutoff/304/range | Selected verifier/status/period guard before response; TTL/cache doesn't bypass rights |
| T17 | Integration + E2E | New activation/renewal/correction/upstream outage/exact ban | Pin never auto-reassigned; renewal new admission; correction original TTL/frozen pin check; ordinary VRS outage alone no resource HTTP |
| T18 | Functional + E2E | Other item/asset ID, YARP/cache/Range/HEAD/bytes failure/retry/secret logging | Membership/current access before cache/proxy, CM gateway bytes, metadata Version != storage validator; Demo data success not undone; no secrets in logs |
| T19 | Architecture | Commands/queries/foreign contexts/DI/Demo boundary | Owning adapters/read handlers, one runtime path, Demo no hidden security/broker deps |
| C01 / DF01,03,05,06 | Shared library .NET server/Unity SDK interoperability | Shared literal [canonical vectors](../canonical-v1-vectors.md), all15 value types/controls/Unicode/Int64 extremes/Decimal/Float64 RFC Appendix B/-0/null/absent/default/List and exact metadata | Both runtimes produce exact expected bytes/profile/digest without untyped number guessing; fixture expectations independent of production serializer |
| C02 / DF01,05,06 | Functional + SDK actual local group commit | Reordered sets, same instance across exact schemas, unchanged members, empty/removal-only targets, upsert+removal, omitted member/duplicate/truncated data/wrong profile | Stable full target digest; compute after removals/upserts; no partial apply/cursor-as-install proof; unknown profile fails before apply |
| C03 / DF01,03,06 | Cache/SDK integration | Random IDs/time/lease/progress/receipts changed; own successful groups mutate actual inventory; normalized original input differs | Original request/plan bytes/profile/digest retained; same-ID original result replay; changed semantic input conflicts; inventory hashes actual committed state only |
| C04 / DF11,14,15 | PostgreSQL fault + historical reads | F10 equal runtime/different provenance, copied exact bytes, corruption/unknown profile, new profile deployment/lifecycle change | Same runtime digest allowed across distinct artifacts; no alternate serializer/historical rehash; graph+triple atomic; technical artifact failure stalls without false effective history/time |

Fixtures: multiple exact structures of one instance, same/no-change/empty/error groups, progressive inventory, record/stream/receipt failures, source retirement/history/null/equal cutoff/withdrawal, distinct credentials/license/grants. Isolate test database/cache namespaces/IDs/broker work and SDK storage, cleanup own fixture resources; use barriers/fault injection not arbitrary sleeps for races. PostgreSQL microsecond boundary precision and actual negotiated network protocol asserted explicitly. Domain double tests not proof real proxy/Unity/auth/storage integration.

Current [Delivery PersistenceTests](../../../../contentmanager.api/tests/BoundedContexts/Delivery/ContentManager.Delivery.Tests.Integration/PersistenceTests.cs), [legacy SyncSession tests](../../../../contentmanager.api/tests/BoundedContexts/Delivery/ContentManager.Delivery.Tests.Unit/ClientProfileAndSyncSessionTests.cs), [App PublishingDeliveryIntegrationTests](../../../../contentmanager.api/tests/App/ContentManager.App.Tests.Integration/PublishingDeliveryIntegrationTests.cs) don't cover target Demo plan/streaming wire/auth TTL/effective projector; [EntitlementSelectionTests](../../../../contentmanager.api/tests/BoundedContexts/Delivery/ContentManager.Delivery.Tests.Unit/EntitlementSelectionTests.cs) current-null guard discrepancy F12. No app tests/build/browser/Unity/broker/network stand run during this analysis.

Temporal integration acceptance: real source F04 Offset authoring→frozen UTC runtime→F13 exact bytes/profile/hash→F19 generated DateTimeOffset; /1 Naive and new Offset exact versions concurrently; original offset absent from runtime hash; date rollover/100ns retained; unsupported profile fails before apply/inventory/receipt; nested Lists/equivalent instants group/hash/unique semantics. Existing envelope/group/inventory codecs checked for unchanged tuple layout; no implicit group profile increment/current-source rehash. Full new-profile golden corpus and actual SDK/Player transport execution engineering, не document-check success.

Execution acceptance: controllable TimeProvider and deterministic barriers exercise renewal versus deadline/revoke, expired owner replacement/late finally, offered boundary versus receipt, same/different control replay and evicted receipt stale409, preparing Stop/Resume, lost Start/control/receipt/terminal ACK, absolute reservation TTL and capacity, and stable local store scope. Unity/proxy tests cover header/read-idle/record assembly/write stall/cancellation, paused read-idle during local commit/receipt with other budgets still active, no heartbeat/compression and H3→H2 same plan/cursor. These are required tests, not results of editing documentation.

Public-wire acceptance [literal vectors](../sync-wire-v1-vectors.md): SUT pure record factory/strict parser/string-byte restoration/cursor codec and cache orchestration; actual API Functional with owner PostgreSQL, not doubles as DB proof. Check all6variants, empty/nochange/removal/Add, counts+base/fulltarget integrity, large long/decimal/Unicode/escaping exact bytes before typed parse, stable record layout and midgroup replay, duplicate-property/kind/version/tail/UTF8/size rejection. HMAC fixture key is public test data only; same UUID network byte order/server & SDK context, tampered/wrongbinding/zero/out-ofrange cursor errors. Status404/known tombstone410/outage, original receipt ACK/repeat409, invalidation0 no checkpoint or sequence replacement, OperationCompleted plus proper response end/receipts required. Actual process crash/ownership/transport/Unity conformance requires implementation; fixture verification is not application test result.

## 12. Порядок реализации

1. Implement accepted Q19 typed profiles/shared library + selected Q33 public DTO/framing/cursor/receipt/status using literal sync-wire-v1-vectors; close next execution lease/fencing/control/cancellation/retention races before independent SDK integration. Accepted canonical/Demo behavior не переоткрывать.
2. Demo Application.Abstractions/constraints/desired query public owner adapters/pure diff, local cache execution/read adapters/Host DI without licensing; Api/OpenAPI/stream/cancellation contract verification.
3. F19 durable state/atomic groups/recovery and F20 scene through accepted public SDK boundary; freshness/assets independent; necessary real SDK/CM/H3-H2/proxy tests.
4. Implement accepted F12 exact PolicyAgreement/current-period/completeness new admission; accepted Q16 session/replay/pinned enforcement/independent response leases, approved Q17 conservative observation/catch-up/ban, Q20 physical persistence/clock adapters and Q19 snapshot identity.
5. Domain/EF/Persistence full snapshots/state/history/terminal restrictions/sessions/bindings/inbox with PostgreSQL concurrency/immutability checks; public cross-BC ports/projector and source validators.
6. Session Api/auth middleware/protected manifest-item-asset reads/redaction; F05 Host YARP bytes/range/cache guarded; real necessary VRS/source/broker/storage contour checks.
7. Host one accepted mutation/read path, SDK wire regeneration/integration and observability; remove target dependency on legacy direct foreign contexts without unrelated cleanup.

## 13. Открытые технические вопросы

[Q37](../../frontend/questions/Q37-launcher-asset-delivery.md): утверждение resolved manifest derivation/strictest aggregation, точный API/read model/ID/hash/order/profiles/owner materialization, source build requirements/canonical mapping, App re-admission при закрытом launcher и offline/fallback policy. Modes/default/schema owner уже согласованы; extension после Demo не меняет existing v1.

- [Q15 visibility contracts](../questions/Q15-api-and-source-code.md#delivery-visibility-contracts): exact source outcome/complete history/staff progress codecs/status/projections; existing runtime contract  final runtime routes/signatures/status/DTO/header/binary/streaming OpenAPI and actual current source gaps.
- [Q33](../questions/Q33-sync-resume-transport.md#f13-wire-protocol-и-execution-ownership): NDJSON/envelope/variants/removals/cursor/replay/receipt pacing and identity, in-memory single-process store/absolute1h TTL/restart loss/lease fencing agreed. Public Start/Data/Commit/Status/casing/token/record models and literal vectors fixed; Resume/Stop/Discard/Freshness/lease-fencing/transitions selected; remaining public SDK/store signatures, configuration numbers and actual recovery/proxy/Unity tests; [Q30](../questions/Q30-content-synchronization.md) desired/diff protocol.
- [Q16 accepted sessions](../questions/Q16-activation-and-sessions.md#принятый-протокол-сессий-и-response-leases): mechanism/defaults selected; exact wire [Q15](../questions/Q15-api-and-source-code.md#delivery-session-credential-и-replay-contracts), source epoch/watermark codecs [Q23](../questions/Q23-source-synchronization.md#credential-validation-и-security-epoch), keyring/configuration/retention/mappings [Q20](../questions/Q20-persistence-and-migration.md#сессии-replay-и-response-leases) remain engineering.
- [Q20 visibility persistence](../questions/Q20-persistence-and-migration.md#delivery-catch-up-и-подтверждение-видимости) / [Q17 confirmed visibility](../questions/Q17-publication-availability.md#подтверждённая-видимость-и-история-delivery): MVP write gate выбран (§7.2); остаются source/evidence codecs/own mappings/DI/migrations; conservative observation/catch-up/history rules accepted, session runtime protocol accepted Q16, F12 agreement/current-period binding approved; source codecs/mapping [Q23](../questions/Q23-source-synchronization.md).
- [Q19 accepted canonical profiles](../questions/Q19-content-hashes.md#принятый-canonical-profile-v1): exact bytes/digest/member order/role variants приняты; physical mappings/common library/literal fixtures и реальная server/SDK interoperability требуют реализации. [Q21](../questions/Q21-media-storage.md): binary cache/range/resume; limits [Q22](../../../business/questions/Q22-quality-targets.md), retention [Q12](../../../business/questions/Q12-media-and-retention.md).

Demo full selection/group atomicity/automatic safe resume/default cache1h/post-success freshness/Start JSON+one data stream+separate receipts/assets/H3-H2 и MVP exact pinned entitlement/non-secret key basis/Withdrawal already accepted. Unknowns — mechanisms/formats/verification, не новые user approval gates.
