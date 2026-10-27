# Q18. Хранение истории

## Вопрос и известные границы

Журнал активаций или новая неизменяемая запись при повторном включении? Как упорядочивать равновременные изменения?

## Ответ блока 13

Activation history — append-only записи, фиксируемые атомарно с изменением `ProductVariantPublicationState.ActiveRevisionId`. Повтор activation уже активной revision — идемпотентный no-op без новой history record.

Rollback разрешён на любую ранее созданную revision того же Variant, всё ещё допустимую по Q04. Он добавляет новую activation record со старым immutable RevisionId: R1 → R2 → R3 → R1, не изменяя прежние записи.

Unpublish использует тот же механизм: pointer становится null и добавляется history record с `RevisionId = null`. Старые revisions сохраняются. Повтор при уже null pointer не создаёт нового изменения состояния.

Manual no-op не supersedes automatic intent и не меняет Version/history/outbox. Остановка такого intent — самостоятельный Cancel exact automatic item, не fake null transition. Actual manual transitions и active Withdrawal инвалидируют старое продолжение; captured Variant Sequence и ProductPublicationState.Version предотвращают ABA. Item Activated + actual pointer/history/outbox фиксируются одним Publishing commit; повтор terminal outcome не активирует revision заново после manual transition. [Workflow Q20](Q20-persistence-and-migration.md#publishing-workflow-capture-и-продолжение).

Минимальная запись MVP: nullable RevisionId; серверный monotonic Sequence внутри одного ProductVariant; ActivatedAt; Reason (Manual, AutomaticPromotion, Rollback, Unpublish); actor, когда появится Identity; correlation/batch ID automatic operation. Комментарий сотрудника необязателен. Временная метка отвечает за «когда», Sequence — за порядок при одинаковых timestamps; клиент Sequence не назначает.

**Бизнес-семантика закрыта.** Физическое хранение, concurrency contract, алгоритм Sequence и привязка effective readiness при async Delivery уточняются Q20/Q15/Q17. Требование append-only не объявляет конкретную технологию event sourcing.

## Затронутые фичи

[F11](../../../business/features/F11-publication-lifecycle.md)

## Как применить ответ

Ответ внесён в F11 и связанную историческую выдачу F12/F13. При async видимости Publishing history сама по себе ещё не доказывает фактическую доступность — Q17.

## Current-state snapshot 01.10.2026

Publishing уже содержит persistable `PublicationActivation` с sequence, nullable RevisionId, ActivatedAt и reason, а PublicationState имеет `ActiveRevisionId`. Повторная активация/rollback представлена новой записью истории, а unpublish может быть выражен activation с отсутствующей revision.

Это подтверждает текущую реализацию append-history направления, но exact правила конкурентного sequence, одинакового времени и обязательного audit context остаются целевой детализацией. Наличие текущей таблицы не запрещает её корректировку.
