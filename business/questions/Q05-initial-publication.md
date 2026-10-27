# Q05. Первая публикация

## Вопрос и известные границы

Разрешены ли ревизии только с общей базой, только с локальным релизом или вообще с пустым итогом?

Уже определено: Нельзя неявно подставлять текущую базу вместо конкретного исходного релиза.

## Ответ блока 13

Разрешены base-only, variant-only и base + variant revisions. `ProductReleaseId` и `VariantReleaseId` независимо optional; хотя бы один конкретный Published source обязателен. Отсутствующий source не заменяется `latest`, фиктивным пустым release или текущим assignment.

После composition итоговый content обязан быть непустым. Ни отсутствие обоих sources, ни пустой materialized результат не создают revision. Снятие варианта с публикации выполняется F11 через `ActiveRevisionId = null` с новой записью истории, не через пустую revision.

**Бизнес-вопрос закрыт.** Это не отдельное решение о допустимости пустого ProductRelease или patch-only VariantRelease как самостоятельного артефакта F09; такая деталь, если понадобится, уточняется отдельно. Точные DTO/null encoding и persistence constraints остаются Q15/Q20.

## Затронутые фичи

[F09](../features/F09-content-releases.md) · [F10](../features/F10-publication-revisions.md)

## Как применить ответ

Ответ внесён в F09/F10 всех затронутых сторон; снятие публикации описано в F11. Правило касается целевого MVP, полный Publishing не возвращается в Demo.

## F09: пустой snapshot и повторное создание

Связано с [business F09](../features/F09-content-releases.md), [backend F09](../../technical/backend/features/F09-content-releases.md), [frontend F09](../../technical/frontend/features/F09-content-releases.md), [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#f09-release-contracts-и-semantic-review), [Q20](../../technical/backend/questions/Q20-persistence-and-migration.md#f09-snapshot-creation-и-lifecycle-races).

Закрытое правило F10 выше сохраняется: конкретный source обязателен, итог revision непустой, отсутствие source не заменяется latest/фиктивным release/current assignment. F09 фиксирует source artifact и не активирует его; это другая граница.

**Ответ:**

- Разрешены явно созданный ProductContentRelease с empty Items и ProductVariantContentRelease с полностью empty patch. Exclusions-only patch тоже допустим; intrinsic/context/reference checks сохраняются. Фиктивный release автоматически не создаётся. F10 отдельно требует хотя бы один exact source и nonempty final composition.
- Намеренный Create из unchanged assignment, включая тот же content и тот же/другой ChangeComment, создаёт новый artifact ID. ContentHash/source ETag не являются request identity и не запрещают два корректных Create.
- По умолчанию predecessor semantic review — latest-created same-owner release по ReleaseNumber, включая Retired. Exact predecessor ID фиксируется с review/Create provenance; first release имеет отсутствующий predecessor, не fake baseline.
- Позднее создание другого release не подменяет уже показанный predecessor и не требует нового review само по себе. Изменение assignment, выбранного validation context либо необходимых dependency facts требует fresh review. Create сохраняет исходный exact reviewed predecessor.
- ClientOperationId обозначает один пользовательский intent. Confirmed same ID/input возвращает исходный execution receipt; новый намеренный Create получает новый ID. Changed bound input конфликтует. Auth/confirmed-execution lookup выполняются до new-execution source/lifecycle checks, поэтому поздний source edit/Retire не превращает успешный replay в новый отказ. Receipt не Restore/Activate и не current-resource lifecycle view.

**Инженерная детализация:** normalized input/result/replay/access/error carrier и публичный review basis в [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#publishing-command-execution-и-clientoperationid), owner transaction/unique execution storage в [Q20](../../technical/backend/questions/Q20-persistence-and-migration.md#publishing-workflow-capture-и-продолжение). Empty/repeat/predecessor policy закрыта.

## F10: повторное создание revision

Связано с [business F10](../features/F10-publication-revisions.md), [backend F10](../../technical/backend/features/F10-publication-revisions.md), [frontend F10](../../technical/frontend/features/F10-publication-revisions.md), [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#f10-revision-contracts-и-preview), [Q20](../../technical/backend/questions/Q20-persistence-and-migration.md#f10-composition-capture-и-withdrawal).

Закрытые ≥1 explicit source и nonempty final composition сохраняются. Новая revision имеет свою artifact identity и является самостоятельным exact moderation target; ContentHash не заменяет RevisionId/approval.

**Ответ:** намеренный CreateRevision из той же exact комбинации releases/результата, с тем же либо другим ChangeComment, разрешён и создаёт новый RevisionId/самостоятельный moderation target. Approval по hash не переносится. Обязательный ClientOperationId отличает этот intent от retry неизвестного результата: confirmed same input replay возвращает original receipt, не создаёт вторую revision и не выполняет Submit/Activate. Body shape/result wire и normalized input persistence материализуются Q15/Q20; source/workspace If-Match не добавляется из факта idempotency.
