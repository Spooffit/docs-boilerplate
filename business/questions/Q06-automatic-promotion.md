# Q06. Автоматическое обновление вариантов

## Вопрос и известные границы

Автоматический режим только создаёт новые ревизии или также активирует их? Что делать с вариантом без первой публикации?

Уже определено: режим выбирается для конкретного promotion Product release, отдельно от создания snapshot F09; автоматическое распространение задумано для вариантов этого продукта. Старые ревизии не меняются.

## Ответ блока 13

Режим Manual/Automatic выбирается для конкретного Product release/promotion при выборе базы/запуске F11, не постоянным переключателем Product и не side effect F09 Create. `ProductPublicationState.ActiveReleaseId` определяет current общую базу Publishing; его переключение само по себе не меняет клиентский состав Variant.

- Manual оставляет активные revisions вариантов прежними.
- Automatic выполняет compose → полную F10 validation → create revision → automatic submission F17 → ожидание moderation → после Approval activation exact revision → async Delivery. [Q28](Q28-content-review-scope.md) задаёт quality gate; создание само по себе не активирует revision.
- Каждый Variant проходит подготовку, moderation и activation независимо. Activation state + history фиксируются атомарно в его Publishing transition; create → human decision → activation — отдельные устойчивые стадии. Глобальной транзакции на весь batch нет; ошибка одного не откатывает успешные другие. Operation сохраняет результаты по каждому Variant, включая awaiting moderation, rejected, blocked/failed и причину.
- Вариант без прежней публикации может получить первую revision и activation автоматически, если итог валиден и непуст.
- При наличии Published VariantRelease используется последний Published local release; незавершённые local Draft changes в состав не входят.
- Если Published VariantRelease ещё никогда не было, но есть незавершённая local работа, automatic promotion блокируется для этого Variant. Нельзя незаметно выпустить base-only вместо подготавливаемого patch.
- Если local работы нет и VariantRelease никогда не было, допустим base-only. Пустой фиктивный VariantRelease не требуется.
- Reusable VariantRelease проверяется заново против конкретного нового ProductRelease. При конфликте active revision остаётся прежней.
- Transient failures допускают безопасный идемпотентный retry; domain conflicts не повторяются бесконечно. После исправления можно повторить failed/blocked subset либо весь набор с пропуском уже успешно обработанного результата.

**Бизнес-вопрос закрыт.** Inputs/приоритет/continuation уточнены ниже; public wire/mappings/numeric retry settings материализуются Q15/Q20/Q13. Повтор завершённого результата не должен заново активировать его или отменять последующее ручное действие.

## F11: captured inputs и приоритет действий

StartPromotion сохраняет immutable exact ProductReleaseId, captured ProductPublicationState.Version, applied Variant membership и exact local release каждого item. Archived/conflicted members видны как Blocked; pending/unapplied source parent не выдаётся за applied membership. Новые/перемещённые позднее Variants и новые local releases не добавляются в старую operation. Нулевой membership завершает operation с zero items; выбор Product base имеет свой результат.

Local source — latest-created VariantRelease по ReleaseNumber. Retired latest блокирует item, не разрешает fallback на более старый Available. Never-any-release + незавершённая работа хотя бы в одной из четырёх working групп блокирует base-only; при отсутствии работы base-only допустим. Явный fully empty VariantRelease считается существующим release.

Технический retry сохраняет item/frozen inputs и committed RevisionId/ModerationId. После исправления domain blocker запускается новая подтверждённая operation для выбранного subset; previous outcome сохраняется с retry trace, не переписывается. Продолжение не подхватывает latest source автоматически.

Внутренний VariantPromotionIntent хранит current automatic item. Новый принятый automatic intent для Variant supersedes прежний unfinished item, даже при той же base. Actual manual Activate/Rollback/Unpublish и другие реальные pointer transitions инвалидируют старое продолжение; уже выполненные activation/history не отменяются.

Manual no-op same-active/already-null сохраняет no-Version/no-history/no-outbox правило Q18 и сам не отменяет automatic работу. Для остановки есть отдельный Cancel automatic item по exact ItemId с own item precondition: отмена не создаёт fake publication history и не отменяет заменивший item.

Late Approval продолжает только exact bound RevisionId/ModerationId Approved при current item head, captured Product state Version и Variant publication Sequence, current applied parent/lifecycle/F15/restrictions. Product base A→B→A и Variant R1→R2→R1 не возобновляют старый intent. Cancelled/Superseded не возобновляются Restore/Resolve/events; retained Approved artifact может быть выбран отдельным manual activation под current guards.

## F11/F17: устойчивые стадии

Подготовка item — один Publishing commit: CreateRevision + automatic Submit + exact tuple binding + AwaitingModeration. Human decision и последующая activation отдельны. Retry не присоединяет чужую Pending iteration по hash/sources/resemblance.

Approval фиксирует exact decision и durable continuation. Activation атомарно коммитит item Activated, pointer, history/Sequence и outbox; replay terminal item не изменяет pointer после последующих manual действий. Withdrawn iteration cancels только её exact bound item; новая manual iteration не присоединяется к старому item. Rejected сохраняет прежний active pointer. Supersede/Cancel automatic item не Withdraw-ит human moderation автоматически.

Operation inputs/membership immutable; item — самостоятельный mutable state owner с own Version; progress одного item не меняет tokens остальных. Summary derived из items, Publishing Activated и Delivery readiness — отдельные результаты. Scope/access/result/retry/profile — [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#publishing-command-execution-и-clientoperationid), storage/stages — [Q20](../../technical/backend/questions/Q20-persistence-and-migration.md#publishing-workflow-capture-и-продолжение).

## Затронутые фичи

[F11](../features/F11-publication-lifecycle.md)

## Как применить ответ

Ответ внесён в F09–F11 и M04. F11 раскрывает серверный алгоритм и пользовательские состояния, а не оставляет это решение только здесь.
