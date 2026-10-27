# Q02. Изменение политики

## Вопрос

Когда policy становится обязательным основанием периода и влияет ли её смена на ранее обещанные права?

## Принятое правило

Precedence: Variant override → Product policy → system default FreezeAtExpiration. Immutable Publishing PolicyAgreement резервирует policy при собственном commit, до VR School confirmation/first-use. Agreement связывает exact LicenseId + VariantId + SourcePeriodId, original planned parameters и applied parent/policy provenance. Default Freeze фиксируется без fake published PolicyVersion.

Freeze Agreement t1 → Deny policy t2 → same plan confirmation t3 сохраняет Freeze. Lost response same RequestId/plan возвращает исходное Agreement. Failed source commit не создаёт grant; unused Agreement не даёт прав и не autoexpires в MVP.

Изменение unconfirmed plan требует нового SourcePeriodId + Agreement. После confirmation AgreementId не заменяется: contradictory snapshot rejected/quarantined с сохранением binding; missing Agreement означает incomplete data. Renewal имеет новое Agreement, typed Correction того же периода сохраняет agreed policy.

Publishing владеет Agreement. Delivery владеет GrantPeriodId и unique LicenseId + VariantId + SourcePeriodId binding с exact Agreement/frozen policy/source/version provenance. Versioned current commercial facts существуют отдельно, legitimate Correction не меняет policy и пересчитывает Freeze cutoff.

## Оставшаяся детализация

Exact integration boundary: [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#f12-policyagreement-integration-boundary). Planned/confirmed workflow: [Q23](../../technical/backend/questions/Q23-source-synchronization.md#f12-planned-agreement-confirmed-workflow). Storage/coordination: [Q20](../../technical/backend/questions/Q20-persistence-and-migration.md#f12-policyagreement-и-подтверждение-периода). Эти детали не возвращают момент policy reservation к confirmation/first-use.

## Применение

[F12](../features/F12-content-entitlement-policy.md) · [F13](../features/F13-content-delivery.md) · [Q01](Q01-license-rights.md) · [Q03](Q03-renewal-and-multiple-licenses.md).
