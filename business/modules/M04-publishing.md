# M04. Publishing

## Назначение

Назначение начинается с выбора схемы и exact Published версии структуры: «Все экземпляры», «Все, кроме выбранных», «Только выбранные». F07/F08 хранят правила; F09 подтверждает и фиксирует exact результат, F10 композирует frozen releases. Own schema selection Variant заменяет inherited группу целиком. [Q39](../questions/Q39-schema-content-selection.md) связывает правила, дерево UI и открытые контракты.

Publishing превращает Published ContentModeling artifacts в управляемый клиентский состав:

```text
mutable assignments
→ immutable releases
→ materialized ProductVariantPublicationRevision
→ moderation
→ activation
→ Delivery effective
```

Publishing не владеет Product/ProductVariant и не читает чужие DbContext.

## Рабочее состояние и готовность публикации

[F07](../features/F07-product-content-assignment.md) и [F08](../features/F08-variant-content-assignment.md) сохраняют подготовку отдельно от готовности release. Unchanged exact reference может остаться после утраты доступности с ReleaseBlocker; новые позиции и замены требуют полной текущей eligibility. Intrinsic ошибки блокируют Save, а collision только с live Product base может сохраняться в структурно корректном Variant patch с диагностикой.

Для inactive/archived owner разрешено ограниченное сопровождение — удаление позиций и замена проблемных references допустимыми без расширения контентного состава. В Variant это может потребовать нового exclusion/override rule. Open blocking F15 conflict по-прежнему запрещает affected ordinary writes; отдельный corrective workflow определён F15. Source lifecycle/ownership/reactivation определяются F06/Q27, авторизация не становится зависимостью Demo.

[F09](../features/F09-content-releases.md) фиксирует immutable releases после отдельного review и повторной текущей проверки. VariantRelease остаётся reusable patch-only snapshot: ValidationContext явно выбирает exact immutable ProductRelease или сознательный EmptyBase. Live/current active/default empty не подставляются. Контекст сохраняется как provenance, не hard dependency; [F10](../features/F10-publication-revisions.md) полностью проверяет каждую actually selected комбинацию releases.

Явный empty Product release и fully empty Variant patch release разрешены. F10 требует хотя бы один exact source и непустой final composition. Намеренные same-content releases/revisions с новым ChangeComment создают новые identities: hash не dedup и не переносит approval. F09 review фиксирует previous latest-created ReleaseId того же owner по ReleaseNumber, включая Retired; новый release не подменяет уже подтверждённый baseline.

Current new-use instance/schema/media checks относятся к final effective closure: suppressed unavailable A не блокирует итоговый B, shared необходимая B dependency проверяется. Selected source release availability, ownership и trusted captured composition evidence проверяются независимо. Эта closure применяется также при F17 Approve, F11 Activate/Rollback и F13 ограничениях. Historical retention сохраняет более широкие provenance/references, без разрешения удаления suppressed artifacts.

SaveBlockers, ReleaseBlockers и Warnings различимы. UI отдельно показывает сохранение, диагностику и CanCreateRelease; server diagnostic basis учитывает зависимости отдельно от own assignment ETag. Read failure не становится пустым валидным preview. Save/release не переписывают историю и сами по себе не выполняют activation/Delivery.

## Aggregate roots

| Root | Ответственность |
| --- | --- |
| `ProductContentAssignment` | mutable common Product content |
| `ProductVariantContentAssignment` | mutable local patch Variant |
| `ProductContentRelease` | immutable Product assignment snapshot + lifecycle |
| `ProductVariantContentRelease` | immutable Variant patch snapshot + lifecycle |
| `ProductVariantPublicationRevision` | materialized immutable final composition + lifecycle |
| `ProductPublicationState` | current Product base release |
| `ProductVariantPublicationState` | current active Publishing revision + activation sequence |
| `ProductReleasePromotionOperation` | long-running automatic promotion batch |
| `ProductEntitlementPolicyState` | current Product policy pointer |
| `ProductVariantEntitlementPolicyState` | current Variant policy pointer |
| `PolicyAgreement` | immutable policy reservation exact planned license/Variant/source period при commit Agreement |

Immutable child/history:
- release items/patch;
- publication revision items/provenance;
- activation history;
- policy versions.

Automatic operation фиксирует inputs/membership и exact выбранные local releases; per-item progress имеет свой Version, summary вычисляется из items. Current VariantPromotionIntent с собственным Version определяет единственный current automatic item. Его поля/модели определяются один раз в [backend F11](../../technical/backend/features/F11-publication-lifecycle.md); module не вводит вторую копию.

## Фичи

| ID | Ответственность |
| --- | --- |
| F07 | Product assignment |
| F08 | Variant local patch and live preview |
| F09 | immutable releases |
| F10 | composition/materialized revision/lifecycle |
| F11 | current states, activation, rollback, automatic promotion |
| F12 | policy configuration + immutable PolicyAgreement в Publishing; Delivery owns confirmed binding/evaluation |

## Concurrency

Mutable roots use internal `int Version`.

HTTP does not expose Version in DTO:
- exact state GET → strong ETag;
- mutation → If-Match;
- missing → 428;
- stale → 412.

Immutable content snapshots do not need edit concurrency; their lifecycle row still has ETag where Retire/Restore/Withdrawal is allowed.

F09/F10 Create, F11 automatic Start и F17 Submit требуют ClientOperationId GUID в теле. После доступа confirmed receipt lookup с original bound input/If-Match/context/review/comment/actor выполняется до новых предусловий. Replay возвращает original result без повторного artifact/Submit/Restore/Activate; changed input409, failed до commit attempt не confirmed binding. Lost ACK выясняется по same ID. Exact execution contract принадлежит [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#publishing-command-execution-и-clientoperationid).

## Cross-BC ports

### CatalogAccess

Publishing queries:
- Product/Variant existence/current relation/lifecycle;
- F15 blockers.

Catalog internal events initialize empty Publishing state roots idempotently.

### ContentModeling

Resolve exact Published EntityInstanceVersion references and lifecycle availability for new use.

No cross-BC FK.

### Moderation / F17

Manual revision is submitted explicitly.

Automatic PREPARE вместе подтверждает CreateRevision, automatic Submit, exact revision/iteration binding и AwaitingModeration item. Human decision и activation отдельны; чужая Pending не присваивается по hash. Approval сохраняет exact continuation, которое rechecks current intent head, captured ProductStateVersion/Variant Sequence, fresh parent/lifecycle/F15 и final effective restrictions перед activation. Approval/activation не равны Delivery readiness.

Новый accepted automatic item supersedes прежний unfinished head даже при той же base. Реальные manual Activate/Rollback/Unpublish и active Withdrawal очищают head и меняют Sequence; no-op same-active/null не создаёт history/outbox и не отменяет intent. Explicit exact CancelAutomaticItem не отменяет replacement. Cancelled/Superseded не resumes через Restore/Resolve/events; Withdraw bound moderation iteration cancels exact item, новый manual Submit не reattaches его. Сохранённая Approved revision остаётся возможным manual target. [F11](../features/F11-publication-lifecycle.md) определяет поведение; [Q20](../../technical/backend/questions/Q20-persistence-and-migration.md#publishing-workflow-capture-и-продолжение) связывает backend capture/continuation.

F17 Approval дополнительно rechecks показанную Delivery effective comparison basis независимо от moderation ETag. Drift требует refresh/new consent409; unavailable не empty, а более позднее изменение после confirmed Approval не отзывает decision.

### Delivery

Publishing резервирует policy при commit PolicyAgreement, до VR School confirmation/first-use. Agreement сохраняет exact LicenseId/VariantId/SourcePeriodId, original planned parameters и applied parent/policy provenance. Default Freeze не требует fake PolicyVersion. Quote не выдаёт права и не требует CM license projection, но applied Variant/parent и policy context должны быть ready/однозначны.

VR School planned + RequestId → authenticated backend Agreement request вне source transaction → собственный same-plan conditional confirmation + AgreementId/outbox. Lost response replay сохраняет Agreement, failed source commit оставляет его unused без grant; в MVP unused Agreement не autoexpires. Confirmed AgreementId не заменяется. Renewal получает новый SourcePeriodId/Agreement, Correction сохраняет agreed policy. Конкретные правила и связь Delivery binding принадлежат [F12](../features/F12-content-entitlement-policy.md).

Publishing state transition emits immutable outbox event:

```text
VariantId
RevisionId?
Sequence
ActivatedAt
Reason
CorrelationId?
```

Delivery builds its own projection and reports effective state independently.

Broker event запускает ordered public-history catch-up с range completeness. Delivery сохраняет один real complete transition на own commit; processed sequence, effective history и visibility confirmation различаются. Conservative VisibilityConfirmedAt фиксируется после primary observation already committed exact transition, immutable/replayable и не обещает physical commit time. Commit-before/confirmation-after cutoff не входит в Freeze. Current/previous committed revision может обслуживаться при current rights/restrictions, но не заменяет отсутствующее historical confirmation.

Exact terminal Withdrawal ban существует независимо от active pointer или материализации, включая inactive/unmaterialized revision; replay не снимает его. Staff видит active/effective IDs, gap/materialization/pending confirmation/ban отдельно. [Q17](../../technical/backend/questions/Q17-publication-availability.md#подтверждённая-видимость-и-история-delivery) и [F13](../features/F13-content-delivery.md) определяют Delivery правило. Принятый [Q16](../../technical/backend/questions/Q16-activation-and-sessions.md#принятый-протокол-сессий-и-response-leases) сохраняет selected pin/absolute TTL, применяет CurrentPeriod/correction/revoke и bounded response cancellation без fallback/repin/resurrection; independent deadline действует при blocked renewal. Exact providers/wiring остаются engineering, не новый открытый выбор session semantics.

## Persistence

PostgreSQL schema `publishing`.

Separate DbContext from other BC.

No physical FK to CatalogAccess/ContentModeling/Delivery.

Transactions may span multiple Publishing aggregate roots when one Publishing invariant requires it (например Withdrawal active revision → revision lifecycle + publication state + history + outbox). Cross-BC transaction запрещён.

## Feature dependencies

```text
F07/F08 → F09 → F10 → F17 → F11 → Delivery
                    ↘
                     F12 policy config → Delivery entitlement
```

## Связи

[Catalog M03](M03-catalog.md) · [Delivery M05](M05-delivery.md) · [F17](../features/F17-content-review.md) · [Q07](../questions/Q07-exclusions-and-local-content.md) · [Q28](../questions/Q28-content-review-scope.md)
