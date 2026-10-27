# F12. Политика права на контент
## Цель
Определить, какую ProductVariant revision можно выдавать клиенту с учётом лицензии/grant, периода контентных обновлений, policy и historical Delivery state.
Policy настраивается и резервируется в immutable PolicyAgreement в Publishing, а binding подтверждённого периода и entitlement decision принадлежат Delivery.
Source Archive/restore и CatalogConflict F06/F15 не являются revoke, Unpublish или Withdrawal. Они не отменяют already confirmed entitlement и не запрещают новую session с ранее разрешённой materialized revision; обычные F12/F13 license/grant/credential/revision guards сохраняются. Restore/Resolve не создают новый entitlement и не выдают grants автоматически.
## Период обновлений
Право на новые content updates имеет отдельную границу `UpdatesUntil`.
Она не обязана совпадать со сроком использования приложения.
VR School остаётся source of truth коммерческих фактов. Существующий `IsUnlimitedLicense` маппится в CM как явная бессрочность обновлений; отсутствие даты само по себе не означает unlimited.
StartsAt, UpdatesUntil, явная бессрочность, closure и revoke являются versioned коммерческими фактами вне immutable policy binding. IsUnlimitedUsers не определяет бессрочность контентных обновлений. Если UpdatesUntil неизвестен и IsUnlimitedLicense не подтверждён, новая выдача получает IndeterminateDataNotReady, а не подтверждённый Denied или revoke.
## Policy values MVP
Поддерживаются:
- FreezeAtExpiration;
- DenyAfterExpiration.
System default при отсутствии explicit Product/Variant policy — FreezeAtExpiration.
## Precedence
Effective policy:
1. Variant override;
2. Product policy;
3. system default.
Если Variant override отсутствует, он наследует Product/system rule, а скрытая локальная копия не создаётся.
При move неоднозначный mutable/future policy scope является конкретным F15 blocker; само наличие immutable policy history и ранее подтверждённого периода move не блокирует. Обычные policy updates в authoritative affected closure запрещены до разрешения; начальный Variant patch repair не включает редактирование policy.
## Non-retroactivity
Policy резервируется в момент commit PolicyAgreement, а не при VR School confirmation либо первом обращении клиента. Agreement связывает exact LicenseId + VariantId + SourcePeriodId, исходные planned parameters и applied parent/policy provenance. Оно неизменно и не переносится на иной период, Variant или лицензию.

Freeze Agreement в t1, смена current policy на Deny в t2 и подтверждение того же source plan в t3 сохраняют Freeze. Изменение policy не переписывает существующие Agreements или bindings; новый/продлённый период получает собственное Agreement. При system default фиксируется FreezeAtExpiration и его provenance без фиктивной опубликованной PolicyVersion.

В MVP Agreement не имеет automatic expiry. Неиспользованное Agreement само по себе не создаёт grant и не разрешает выдачу. Изменённый неподтверждённый plan требует нового SourcePeriodId и нового Agreement, а не перепривязки прежнего.

## Планирование и подтверждение периода

1. VR School устойчиво сохраняет planned period и RequestId.
2. Authenticated backend VR School вне своей transaction обращается в CM за PolicyAgreement для exact plan. Наличие CM license projection для этой quote не требуется: quote не выдаёт право. CM проверяет готовый applied Variant/parent и однозначную policy basis; mismatch, pending relation либо ambiguous affected F15 context дают NotReady/context conflict.
3. После ответа VR School собственным условным действием подтверждает только тот же неизменённый plan, сохраняет AgreementId вместе с подтверждением и outbox фактом.
4. Подтверждённый факт доставляется CM, где Delivery фиксирует собственный binding и применяет права при достаточных authoritative facts.

CM-content period нельзя подтвердить без Agreement. Зависимость CM принята для этого workflow; право скачивания/запуска приложения и Project Storage независимы. Lost response повторяет тот же RequestId/plan и получает исходное Agreement. Failed source confirmation не создаёт grant: сохранённое Agreement остаётся неиспользованным, а confirmed snapshot не имитируется.

После подтверждения периода AgreementId не заменяется. Противоречивый source snapshot отклоняется/изолируется с сохранением существующей binding; missing Agreement означает incomplete data. [Q23](../../technical/backend/questions/Q23-source-synchronization.md#f12-planned-agreement-confirmed-workflow) описывает integration workflow, [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#f12-policyagreement-integration-boundary) — его exact boundary.

## Delivery binding и completeness

Delivery владеет своим GrantPeriodId и immutable binding для unique LicenseId + VariantId + SourcePeriodId. Binding содержит exact AgreementId, frozen policy и source/version provenance. SourcePeriodId одного source plan для разных Variants не объединяет их grants или Agreements.

При complete authoritative facts подтверждённое отсутствие grant означает Denied. Отсутствующий required snapshot, Agreement, period или policy/source basis означает IndeterminateDataNotReady: неизвестность не подменяется отсутствием права или default новой policy. Подтверждённый revoke даёт отдельный Denied reason. Agreement и license projection по отдельности не разрешают выдачу. Storage/coordination детали принадлежат [Q20](../../technical/backend/questions/Q20-persistence-and-migration.md#f12-policyagreement-и-подтверждение-периода).
## Freeze
После окончания UpdatesUntil клиент сохраняет право на whole revision последнего confirmed Delivery state до cutoff, включая explicit null. Для MVP используется conservative VisibilityConfirmedAt: persisted один раз время primary observation exact уже committed полного transition, а не purported physical commit moment.
Граница строгая:
```text
VisibilityConfirmedAt < UpdatesUntil
```
Если последнее confirmed state строго до cutoff было null после Unpublish, frozen revision отсутствует. Система не ищет автоматически предыдущую ненулевую revision.
Новый нормативный случай: complete Delivery commit до UpdatesUntil, но confirmation после cutoff либо ровно на нём, не входит в Freeze. Pending necessary confirmation time/history или incomplete ordered range означает IndeterminateDataNotReady, а не доказательство отсутствия либо старой доступности. Current committed state может обслуживаться, если его обычное право разрешает, но из него не синтезируется historical Freeze.

Confirmed время immutable; при lost ACK читается сохранённый outcome/time. Если confirmation не была durably сохранена, recovery наблюдает exact committed transition заново и фиксирует fresh time без backdating. UTC primary clock/stored precision и порядок времени проверяются; обнаруженный time-order failure даёт DataNotReady без silent clamp. [Q17](../../technical/backend/questions/Q17-publication-availability.md#подтверждённая-видимость-и-история-delivery) задаёт эту границу, [Q20](../../technical/backend/questions/Q20-persistence-and-migration.md#delivery-catch-up-и-подтверждение-видимости) — надёжное применение.
Поздний обычный Unpublish не отменяет уже frozen historical revision.
Retire source artifacts не отменяет frozen historical access.
Explicit Withdrawal revision блокирует дальнейшую серверную выдачу, включая Freeze, без automatic replacement.
Exact terminal ban проверяется по authoritative owner независимо от active pointer/materialized snapshot, включая inactive/unmaterialized revision; unknown ban/restrictions не разрешают admission. Late materialization/replay не снимают запрет.
## Повторная установка
Frozen revision можно повторно скачать после окончания updates period без отдельного download deadline, пока license/grant не revoked, credential допустим и revision не Withdrawn.
## Продление
Renewal создаёт новый SourcePeriodId и новое PolicyAgreement. Для каждой LicenseId + VariantId source явно задаёт CurrentPeriod. Когда renewal вступил в силу, прежний период этой же лицензии больше не fallback для новой выдачи, включая случай истёкшего нового Deny-периода. Продление после перерыва снова разрешает допустимую current whole revision нового периода; публикации во время перерыва не вырезаются по item-level. Freeze затем рассчитывается по новой UpdatesUntil.

Легитимная Correction того же confirmed period явно помечается типом, reason и history и сохраняет agreed policy. Коррекция versioned StartsAt/UpdatesUntil/бессрочности/closure/revoke изменяет соответствующие current facts; Freeze cutoff пересчитывается. Дополнительная купленная длительность либо unlimited upgrade — Renewal, а не Correction. Семантика операции не выводится из разницы дат или количества дней.

Смена explicit CurrentPeriod при commercial Renewal делает session старого периода непригодной и требует нового admission без old same-license Freeze fallback. Это смена основания, не grant revoke. Genuine Correction сохраняет Agreement и pin, но current commercial facts повторно оцениваются: допустимый pin продолжает работу до original абсолютного TTL; known ineligible требует invalidate/new admission; Unknown временно блокирует, не объявляется revoke. Extension не продлевает TTL и не resurrect-ит expired/invalidated session.

## Pinned runtime session

Session фиксирует selected LicenseId + credentialId + VariantId + SourcePeriodId/Agreement + whole RevisionId. При одинаковом permitted whole candidate выбирается минимальный LicenseId, затем credentialId. После pin невыбранные credentials/licenses не являются основанием session: их revoke не отменяет selected session. Selected denial не переключает session на другую license или revision автоматически.

Абсолютный server-configurable TTL по умолчанию 30 минут, ограничен finite Deny UpdatesUntil и explicit credential deadline. Application ExpiresAt не подставляется в этот минимум. Нет sliding/refresh; expired session требует нового admission.

При active updates pinned R1 остаётся R1 после effective R2, ordinary Unpublish, source Archive либо Retire; право не пересобирается в latest автоматически. Current Deny cutoff проверяется после Correction, даже если исходный SessionExpiresAt оказался позднее новой границы. При expired Freeze pin должен совпадать с current eligible confirmed frozen whole candidate Q17; иначе NeedsNewAdmission, без repin. Required missing history/time даёт DataNotReady, а не mismatch/revoke.

Selected credential/license/Variant grant revoke или exact revision Withdrawal запрещают только свой scope. Source restoration не восстанавливает старый invalidated token. Runtime response проверяется короткими bounded leases F13; facts Unknown временно блокируют session, а уже подтверждённая invalidation не отменяется extension/restoration. Правила [Q16](../../technical/backend/questions/Q16-activation-and-sessions.md#принятый-протокол-сессий-и-response-leases) приняты, exact wire/storage остаются технической детализацией.

Для интеграции Launcher/app delivery mode и resolved asset manifest не создают нового entitlement. OnDemand/Preload/RequiredBeforeLaunch влияют только на локальную готовность, а каждый protected GET/HEAD/Range получает проверку точного Variant/revision/media scope по F13. SQLite snapshot, GUID, cache hit либо полученный manifest не доказывают право на серверные bytes. Новый admission работающего приложения после абсолютного session TTL, когда Launcher закрыт, **Уточняется [Q37](../../technical/frontend/questions/Q37-launcher-asset-delivery.md)**; refresh/sliding либо raw activation key в билде этим решением не вводятся. Offline/revocation и запуск прежнего snapshot при неготовности нового не согласованы.

## Несколько лицензий
Если клиент доверенно подтвердил несколько лицензий на один Variant, entitlement вычисляется для каждой отдельно и выбирается наиболее актуальная разрешённая **целая revision**.
Рассматривается полный явно предъявленный и проверенный caller license set. Любая необходимая Unknown license/period/agreement basis делает весь selection IndeterminateDataNotReady, даже если другая license уже KnownAllowed. Если факты всех лицензий известны, KnownDenied + Allowed разрешают выбрать наиболее актуальную Allowed revision. Explicit single-license выбор допустим и оценивается отдельно; неявного расширения до лицензий организации нет.
Контент разных лицензий по item не объединяется. Whole candidates сравниваются по effective transition order, а не возрасту/номеру исходного artifact: Rollback старой revision имеет новое transition/time. При identical candidate stable selected provenance определяется минимальным LicenseId, затем credentialId; session pin сохраняет только выбранное основание.
Если предъявлен только один activation key, автоматически использовать другие лицензии той же организации нельзя.
## Revoke
Отзыв одного Variant grant запрещает только этот Variant.
Полный revoke лицензии запрещает все её grants.
Credential revoke — отдельный scope проверки предъявленного credential; он не смешивается с license revoke или Variant grant revoke. Обычный конец content period не означает grant revoke; UpdatesUntil не является сроком использования приложения.
Новый отказ в выдаче не является автоматической командой удалить уже локально установленные данные клиента.
## Explainability
Entitlement decision должен быть объясним специалисту/диагностике:
- policy source/version;
- exact PolicyAgreementId, LicenseId/VariantId/SourcePeriodId и CurrentPeriod;
- Agreement planned basis отдельно от current corrected commercial facts;
- unlimited/UpdatesUntil basis;
- selected RevisionId;
- Publishing active/Delivery effective IDs, sequence gap, materialization и pending visibility confirmation отдельно;
- exact terminal ban и authoritative restriction readiness; совпадение pointers не означает полную готовность;
- license/grant basis;
- machine reason denial/indeterminate state.
Secrets/key/token в diagnostics не выводятся.
## Policy history
Policy versions сохраняются исторически и не удаляются после смены current policy.
Archive policy не нужен.
## Acceptance
- Variant override precedence;
- system Freeze default;
- unknown period fail closed;
- existing Agreement/binding сохраняет frozen agreed policy basis, включая default без fake PolicyVersion;
- strict cutoff;
- strict VisibilityConfirmedAt cutoff: committed-before/confirmed-after и equality исключены; missing necessary time/history → IndeterminateDataNotReady;
- confirmed visibility time не изменяется replay/correction; Correction пересчитывает только cutoff при неизменном PolicyAgreement;
- Unpublish/Retire/Withdrawal различаются;
- renewal пересчитывает Freeze;
- multi-license не merge-ит items;
- one key не подтягивает другие licenses;
- decision имеет объяснимую причину.

| Семейство проверки | Сценарий | Результат |
| --- | --- | --- |
| Policy change/delay | Freeze Agreement t1 → Deny policy t2 → same planned confirmation t3 | Frozen Freeze basis сохраняется, first-use не переоценивает policy |
| Lost response | Agreement commit успешен, ответ потерян | Same RequestId/plan возвращает исходное Agreement, не новый quote |
| Failed source commit | Agreement создано, VR School confirmation не прошёл | Нет confirmed grant; unused Agreement не даёт доступа и не autoexpires |
| Changed plan | Unconfirmed параметры изменились | Новый SourcePeriodId + Agreement; прежнее не перепривязывается |
| Agreement substitution | Confirmed snapshot заменяет AgreementId либо не содержит его | Substitution rejected/quarantined без потери binding; missing → IndeterminateDataNotReady |
| Renewal/Correction | Additional purchase/unlimited upgrade либо explicitly typed correction | Renewal — new CurrentPeriod/agreement без old same-license fallback; Correction сохраняет policy и пересчитывает cutoff |
| Completeness/Variant scope | Complete no-grant vs missing facts; shared source period для двух Variants | Denied отличается от Indeterminate; exact Variant bindings независимы |
| Unknown multi-license | Allowed + Unknown или KnownDenied + Allowed | Первый весь selection Indeterminate; второй выбирает Allowed whole revision; explicit single не расширяется |

| Pinned-session проверка | Воздействие | Результат |
| --- | --- | --- |
| Active updates | R1 session, затем R2/ordinary Unpublish/Archive/Retire | R1 сохраняется при допустимом selected основании до original TTL |
| Deny Correction | Новый cutoff раньше исходного SessionExpiresAt | Current cutoff enforced, equality не разрешает выдачу |
| Expired Freeze | Current confirmed frozen candidate отличается от pin | NeedsNewAdmission без repin; required missing history → DataNotReady |
| Renewal | Explicit CurrentPeriod сменился | Session old period требует new admission без fallback, это не revoke |
| Genuine Correction | Current facts изменены при прежнем Agreement | Allowed pin continues original TTL; known ineligible invalidates; Unknown temporarily blocks |
| Scoped revoke | Unselected key/license vs selected credential/license/grant/revision | Первый session не отменяет; selected scope denied без иной basis fallback |
| Extension/restoration | TTL истёк либо session invalidated, затем срок увеличен/право восстановлено | Старый token не resurrect/refresh; новая session/new admission |
## Связи
[F11](F11-publication-lifecycle.md) · [F13](F13-content-delivery.md) · [Q01](../questions/Q01-license-rights.md) · [Q02](../questions/Q02-policy-changes.md) · [Q03](../questions/Q03-renewal-and-multiple-licenses.md) · [Q04](../questions/Q04-withdrawn-publications.md).
