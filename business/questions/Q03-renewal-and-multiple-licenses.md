# Q03. Продление и несколько лицензий

## Вопрос

Что доступно после продления, чем Renewal отличается от Correction и как выбирать права нескольких лицензий?

## Renewal и CurrentPeriod

Renewal создаёт новый SourcePeriodId + PolicyAgreement. Source явно задаёт CurrentPeriod для LicenseId + VariantId. После вступления renewal в силу старый период той же лицензии не fallback для новой выдачи, включая expired новый Deny period.

После перерыва доступна допустимая current whole revision нового периода; публикации во время перерыва не исключаются по item. Freeze пересчитывается по новой UpdatesUntil и последнему confirmed Delivery state со strict VisibilityConfirmedAt < cutoff по Q17, включая null без older non-null fallback. Это не physical commit timestamp и не сохранение первоначального frozen artifact навсегда; required missing confirmation/history означает IndeterminateDataNotReady.

## Correction

Legitimate Correction того же confirmed period явно типизирована, имеет reason/history и сохраняет Agreement/agreed policy. Она исправляет versioned current StartsAt/UpdatesUntil/IsUnlimitedLicense/closure/revoke и пересчитывает Freeze cutoff.

Additional purchased duration и unlimited upgrade — Renewal с новым периодом. Тип операции не выводится из разницы дат/дней. Ordinary period end не revoke; license/credential/Variant grant scopes различимы.

## Несколько лицензий

Оценивается полный явно предъявленный и доверенно проверенный caller credential/license set. Для каждой license используется own CurrentPeriod/binding, затем выбирается наиболее актуальная Allowed whole revision. Items разных лицензий не merge-ятся. При identical candidate выбирается минимальный LicenseId, затем credentialId; session фиксирует только selected basis. Unselected revoke не отменяет её, а selected denial не выполняет fallback.

Любая required Unknown license/period/Agreement basis делает весь selection IndeterminateDataNotReady, даже при KnownAllowed другой лицензии. Если required facts всех лицензий известны, KnownDenied + Allowed позволяют выбрать Allowed revision. Complete no-grant означает Denied; missing required facts — не Denied.

Explicit single-license choice разрешён и оценивается отдельно. Один activation/license key не присоединяет лицензии организации; implicit organization expansion отсутствует.

## Граница текущего решения

Renewal/смена CurrentPeriod делает old-period session непригодной и требует нового admission без old Freeze fallback; это не grant revoke. Genuine Correction сохраняет Agreement/pin: Allowed продолжает original absolute TTL, known ineligible invalidates, Unknown temporarily blocks без revoke. Extension не продлевает TTL и не resurrect invalidated/expired session.

При active updates pin R1 сохраняется после effective R2/ordinary Unpublish/source Archive/Retire. Current Deny cutoff enforced после correction; expired Freeze требует exact match current eligible confirmed frozen whole candidate Q17, иначе NeedsNewAdmission без repin, missing history →DataNotReady. Selected credential/license/Variant grant/Revision Withdrawal scopes различаются; restoration не оживляет старый token.

Response leases ограничены original Session expiry и следующей finite policy boundary включая Freeze. Checkpoints и interruption приняты в [Q16](../../technical/backend/questions/Q16-activation-and-sessions.md#принятый-протокол-сессий-и-response-leases); новая session требует нового plan без silent binding старого.

Exact source/completeness contracts: [Q23](../../technical/backend/questions/Q23-source-synchronization.md#f12-planned-agreement-confirmed-workflow), [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#f12-policyagreement-integration-boundary).

## Применение

[F12](../features/F12-content-entitlement-policy.md) · [F13](../features/F13-content-delivery.md) · [Q01](Q01-license-rights.md) · [Q02](Q02-policy-changes.md).
