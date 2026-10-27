# Q01. Границы лицензии

## Вопрос

Кто разрешает скачивание/запуск приложения и как это связано с периодом обновлений динамического контента?

## Граница ответственности

Организация вводит номер лицензии в лаунчер. Project Storage обслуживает старый Project/Application и файлы приложений; лаунчер разрешает скачивание и запуск. CM не заменяет эти проверки.

VR School — source of truth коммерческих фактов. UpdatesUntil — отдельная граница контентных обновлений, не application expiration. Период действует при time < UpdatesUntil; Freeze использует conservative VisibilityConfirmedAt после primary observation already committed exact Delivery transition по Q17, не Publishing или physical Delivery commit timestamp. Confirmation после либо ровно на cutoff исключена. IsUnlimitedLicense явно определяет бессрочность; отсутствие даты не unlimited, IsUnlimitedUsers не определяет обновления.

StartsAt/UpdatesUntil/unlimited/closure/revoke — versioned current facts вне immutable policy binding. Ordinary period end не grant revoke. License, credential и Variant grant scopes различаются.

## Подтверждение CM-content period

VR School durable сохраняет planned period + RequestId, затем authenticated backend вне своей transaction получает PolicyAgreement CM. Собственный conditional commit подтверждает тот же plan + AgreementId + outbox. CM-content period не подтверждается без Agreement; новая зависимость CM принята, app license и Project Storage независимы.

Quote не даёт grant и не требует CM license projection. Applied Variant/parent должны быть ready, policy context однозначен; mismatch/pending/ambiguous affected F15 дают NotReady/context conflict. Unused Agreement не даёт права и не autoexpires; изменённый unconfirmed plan требует нового SourcePeriodId + Agreement.

Delivery владеет GrantPeriodId и exact unique LicenseId + VariantId + SourcePeriodId binding с Agreement/policy/source/version. Complete authoritative no-grant = Denied; missing required Agreement/period/source facts = IndeterminateDataNotReady. Confirmed revoke имеет отдельный Denied reason.

Frozen whole revision можно повторно скачать без отдельного download deadline при допустимых license/grant/credential и не Withdrawn revision. Missing Agreement не заменяется system default.

## Оставшаяся детализация

Exact source/wire/completeness/authenticated boundary: [Q23](../../technical/backend/questions/Q23-source-synchronization.md#f12-planned-agreement-confirmed-workflow), [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#f12-policyagreement-integration-boundary), [Q20](../../technical/backend/questions/Q20-persistence-and-migration.md#f12-policyagreement-и-подтверждение-периода).

## Runtime session и действующее право

Принят отдельный CmDeliverySession, без staff/admin Identity; Demo anonymous. Create/recovery проверяет весь явно предъявленный credential set в VR School, без organization expansion; existing requests используют CM facts без нового VRS HTTP. Session фиксирует selected license/credential/Variant/period/Agreement/revision, при identical candidate minimum LicenseId, затем credentialId. Unselected revoke не отменяет pin; отказ selected basis не вызывает fallback.

Absolute configurable TTL default 30 минут ограничен finite Deny UpdatesUntil и explicit credential deadline, не application ExpiresAt; no sliding/refresh. Renewal/CurrentPeriod смена требует нового admission; Correction rechecks current facts при original Agreement/pin/TTL. Unknown temporarily blocks, known ineligible invalidates, restoration/extension не resurrect old token. Freeze после cutoff требует совпадения pin с current confirmed frozen candidate Q17.

Raw keys только Create/recovery; SDK durable CreationRequestId обеспечивает original committed replay без TTL extension, при fresh auth/current pinned eligibility. Response checkpoints/leases/defaults и safe interruption приняты в [Q16](../../technical/backend/questions/Q16-activation-and-sessions.md#принятый-протокол-сессий-и-response-leases)/[F13](../features/F13-content-delivery.md); wire/providers остаются [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#delivery-session-credential-и-replay-contracts).

## Применение

[F12](../features/F12-content-entitlement-policy.md) · [F13](../features/F13-content-delivery.md) · [назначение продукта](../product.md) · [policy Q02](Q02-policy-changes.md) · [renewal/multiple licenses Q03](Q03-renewal-and-multiple-licenses.md) · [Withdrawal Q04](Q04-withdrawn-publications.md).
