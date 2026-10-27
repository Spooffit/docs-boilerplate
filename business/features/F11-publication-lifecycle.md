# F11. Публикация, activation, rollback и promotion
## Цель
Управлять тем, какой Product release является текущей общей базой и какая PublicationRevision ProductVariant выбрана Publishing для клиентской публикации.
Publishing active state и фактическая Delivery effective state являются разными состояниями.
## Product publication state
Product может иметь current ProductContentRelease как общую базу для подготовки/automatic promotion.
Смена Product current base сама по себе не меняет клиентскую effective revision Variant.
## Variant publication state
Для каждого ProductVariant Publishing хранит ActiveRevisionId либо null и последовательную activation history.
ActiveRevisionId означает выбор Publishing, но не гарантирует готовность Delivery.
## Activation
Manual activation разрешена только для exact PublicationRevision, которая Approved F17, не Retired, не Withdrawn, не блокируется актуальным CatalogConflict и относится к нужному Variant.
Текущие ограничения instance/schema/media проверяются только по final effective closure F10; suppressed provenance не становится runtime blocker, а общие необходимые dependencies остаются в проверке.
Обычная activation требует собственного Active Variant и доступного Active parent F06. Archived запрещает activation/rollback и automatic promotion. Conflict guard относится только к authoritative affected closure F15; lifecycle/source/permission/scope и dependencies проверяются заново при фиксации.
Activation создаёт новую append-only history запись и increment Sequence.
Повторная activation уже активной revision — no-op без изменения state Version/Sequence, новой history/outbox и отмены automatic intent. Такой no-op не подменяет отдельную явную отмену automatic item.
## Delivery propagation
После committed activation Publishing передаёт transition Delivery.
Delivery обрабатывает полную упорядоченную Publishing history: broker event служит trigger, а range completeness проверяется через public read-port. Stage полной revision выполняется до короткой фиксации. Каждый собственный commit фиксирует один реальный полный transition вместе с pointer/history/processed sequence/inbox; нельзя объявить R1/R2/R3 последовательно доступными одним commit.

Для historical visibility MVP выбирает VisibilityConfirmedAt: сохранённое один раз UTC-время primary observation exact уже committed полного Delivery transition. Это консервативная бизнесовая граница, а не восстановленный physical commit timestamp. Нельзя брать время до commit, backdate после crash либо менять ранее подтверждённое время при replay. Current committed revision при достаточных правах/ограничениях может обслуживаться до historical time confirmation; отсутствующее required время не создаёт старую Freeze visibility.

Во время catch-up previous committed effective revision может обслуживаться при действующих правах и restrictions. LastProcessedPublishingSequence, current effective state и confirmed history/time различаются; совпадение ActiveRevisionId с EffectiveRevisionId не доказывает range/materialization/confirmation readiness. [Q17](../../technical/backend/questions/Q17-publication-availability.md#подтверждённая-видимость-и-история-delivery) фиксирует правило.
## Rollback
Rollback выбирает любую допустимую historical revision того же Variant.
Rollback не переписывает старую history, создаёт новую activation запись, разрешён только на ранее Approved exact revision и запрещён на Retired/Withdrawn revision.
Пример:
```text
R1 → R2 → R3 → R1
```
## Unpublish
Unpublish устанавливает Publishing ActiveRevisionId = null и создаёт history entry.
Если state уже null, Unpublish — no-op без state Version/Sequence/history/outbox и отмены automatic intent.
Historical revisions сохраняются.
Unpublish не равен Withdrawal и сам по себе не запрещает ранее frozen historical access.
Archive/Conflict не выполняют Unpublish автоматически. Явные Unpublish и Withdrawal остаются safety actions при соответствующем permission/scope даже для Archived либо conflicted объекта; их собственные последствия сохраняются.
## Withdrawal active revision
Withdrawal active revision приводит к Publishing null-state без automatic fallback.
Фактический переход при manual Activate/Rollback/Unpublish либо Withdrawal active revision меняет Sequence и очищает current automatic intent этого Variant; позднее продолжение прежней операции не может перезаписать выбор специалиста. No-op не создаёт такого перехода.
Delivery после propagation также становится effective null.
Exact terminal Withdrawal независимо запрещает эту revision для новой выдачи даже до обработки pointer/null transition, при inactive/unmaterialized revision и позднем replay. Ban не снимается материализацией или старым событием. Принятый [Q16](../../technical/backend/questions/Q16-activation-and-sessions.md#принятый-протокол-сессий-и-response-leases) запрещает новый selected-session checkpoint и отменяет active response при known CM ban либо independent bounded lease deadline; late callback/restoration не resurrect invalidated token/response. Actual buffered bytes/asynchronous source lag не превращаются в zero-latency SLA; engineering wiring уточняется отдельно.
## Automatic promotion
Automatic promotion запускается от exact ProductRelease и обрабатывает Variants независимо. Start фиксирует ProductReleaseId, ProductStateVersion, applied Variant membership, captured Variant Sequence и exact latest-created local VariantReleaseId каждого item. Inputs и membership операции неизменяемы: более поздние Variant/releases в неё не добавляются. Archived либо conflicted member фиксируется как Blocked, а не исчезает из состава. Ноль Variants означает completed operation с нулём items; действие над собственной Product base выполняется по своим предусловиям.

Latest-created local release определяется по ReleaseNumber, включая Retired. Если последний release Retired, item Blocked: fallback на более старый отсутствует. Если release нет, но хотя бы одна из четырёх working patch groups (additions, overrides, schema exclusions, instance exclusions) непуста, item Blocked. Иначе допустим base-only. Явно созданный empty local release считается существующим release, а не отсутствием patch artifact.

Для каждого допустимого frozen item:
1. Проверить сохранённые exact inputs, lifecycle, parent/current affected scope и final effective closure; не подставлять latest заново.
2. Одним подтверждённым действием создать PublicationRevision, выполнить automatic Submit, связать exact revision/iteration с item и перевести его в AwaitingModeration. Нельзя принять чужую Pending iteration по hash, совпавшим source IDs или content.
3. Human moderation decision выполняется отдельно. Approval сохраняется для exact revision и создаёт надёжное продолжение exact bound item.
4. Перед продолжением проверить exact RevisionId и ModerationApproved, current automatic head, captured ProductStateVersion и Variant Sequence, fresh parent/lifecycle/F15 и ограничения effective closure.
5. Подтверждённая activation вместе фиксирует item Activated, Publishing pointer, history/Sequence и уведомление Delivery. Повтор продолжения возвращает terminal outcome и не перезаписывает более поздний manual state.

Результаты независимы: AwaitingModeration / Activated / Blocked / Failed / Cancelled / Superseded. Failure одного Variant не откатывает остальные. Activated означает Publishing result, а Delivery readiness/effective transition наблюдаются отдельно.

Technical retry сохраняет frozen item inputs и exact bound tuple. Исправление domain blocker требует новой операции для явно подтверждённого subset; прежние inputs/history не переписываются. Принятый порядок фиксации и продолжения связан с [Q20](../../technical/backend/questions/Q20-persistence-and-migration.md#publishing-workflow-capture-и-продолжение); wire/provider и числовые retry limits остаются техническими вопросами.

## Current automatic intent и отмена

У каждого Variant отдельный internal VariantPromotionIntent с CurrentAutomaticItemId и собственным Version. Новая принятая automatic operation для Variant заменяет прежний unfinished head, даже при том же ProductRelease; прежний item становится Superseded. Это не изменяет его сохранённые inputs и не Withdraw-ит human review.

Поздний Approval не продолжает Cancelled/Superseded item. Возврат Product A → B → A либо manual revision R1 → R2 → R1 не восстанавливает прежнее основание: captured ProductStateVersion/Variant Sequence должны совпадать, одного равенства IDs недостаточно. Restore/Resolve/event не возобновляют старый intent. Сохранённую Approved revision можно позднее Activate вручную при обычных current checks.

CancelAutomaticItem — отдельное явное действие для exact item с fresh own preconditions. Оно не создаёт фиктивную activation history и не отменяет уже заменивший его новый head. No-op Activate/Unpublish не считаются Cancel. При Withdrawal exact bound moderation iteration её automatic item отменяется; новая manual iteration той же revision не присоединяется к старой операции. Rejected оставляет прежний Publishing pointer. Cancel/Supersede item не выполняет автоматически human Withdraw.

## Повтор Start

Start automatic promotion требует ClientOperationId — GUID в теле команды. После проверки доступа lookup confirmed execution с исходными inputs/If-Match/context/review/comment/actor предшествует новым предусловиям. Same ID/input возвращает initial confirmed receipt и frozen operation, даже после смены Product base, Retire или завершения item; replay не начинает новый batch и не реактивирует результат. Changed bound input →409. Failure до commit не создаёт confirmed binding; unknown lost-ACK outcome выясняется по исходному ID. [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#publishing-command-execution-и-clientoperationid) определяет точную форму receipts.
## Первый publish Variant
Automatic mode может создать первую base-only revision Variant, если local patch отсутствует.
Если любая working group непуста, но VariantRelease ещё нет, automatic promotion этого Variant блокируется, а не публикует base-only состав молча. При выбранном empty source итоговая revision всё равно должна быть непустой F10.
## History
Activation history append-only и упорядочена server Sequence.
Запись содержит revision/null, time, reason и actor/correlation context при наличии.
Captured inputs/membership операции сохраняются; progress хранится per-item с own Version. Изменение одного item не меняет concurrency tokens остальных. Operation summary вычисляется из items, не служит общей mutation Version всей операции.
## Retention
Publication state/history не удаляются.
Retire release/revision не уничтожает historical records.
Archive, restore и Resolve не переписывают activation history, не активируют revision, не отменяют Withdrawal и не выдают grants автоматически. Existing materialized Delivery и confirmed entitlement, включая новые sessions, проверяются отдельно по F12/F13.
## Acceptance
- Publishing active != Delivery effective;
- manual activation требует Approved revision;
- rollback создаёт новую history запись;
- Unpublish не удаляет history;
- automatic promotion независим по Variant;
- pending local draft может блокировать auto promotion;
- active withdrawal не выбирает старую revision автоматически.
- Start фиксирует membership и exact latest local releases; latest Retired не заменяется старым, новый Variant/release не попадает в существующий batch;
- explicit empty release существует, fully empty final composition запрещена; zero-member batch completes без fake items;
- PREPARE атомарен по созданию revision/Submit/exact binding/AwaitingModeration; чужая Pending не присваивается по hash;
- late Approval проверяет current head и версии/Sequence: A→B→A и R1→R2→R1 не проходят на старом основании;
- same-active Activate и null Unpublish no-op не отменяют automatic item; explicit exact Cancel не создаёт history и не отменяет replacement;
- lost-ACK Start replay возвращает receipt, changed bound input409; duplicate continuation не перезаписывает поздний manual transition;
- Cancelled/Superseded не возобновляются Restore/Resolve/event, Approved revision остаётся доступной для отдельной manual activation;
- per-item progress не меняет tokens остальных и Activated не объявляется Delivery-ready.
- commit до UpdatesUntil и visibility confirmation после него не дают Freeze-доступа; equality cutoff также исключена;
- catch-up не пропускает sequence gaps и не создаёт несколько фиктивных visibility transitions одним commit;
- terminal Withdrawal запрещает exact revision независимо от состояния pointer/материализации; diagnostics различают activation, processing, visibility и ban.
## Связи
[F09](F09-content-releases.md) · [F10](F10-publication-revisions.md) · [F12](F12-content-entitlement-policy.md) · [F17](F17-content-review.md) · [Q05](../questions/Q05-initial-publication.md) · [Q06](../questions/Q06-automatic-promotion.md).
