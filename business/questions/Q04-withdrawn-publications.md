# Q04. Отзыв исторической публикации


## Вопрос и известные границы


Что делать с замороженным составом, если его позже сняли или запретили: сохранить, запретить или заменить специально разрешённым?


Уже определено: отзыв лицензии не равен обычному окончанию срока. История не стирается.


## Freeze, null и подтверждённая видимость


Freeze фиксирует последнее confirmed Delivery state с VisibilityConfirmedAt строго до UpdatesUntil, включая null. Unpublish commit до cutoff, но observation/confirmation после либо ровно на нём, не считается подтверждённой исторической доступностью null до cutoff. Система не ищет автоматически older non-null revision при последнем подтверждённом null.

VisibilityConfirmedAt — persisted один раз UTC-время primary observation exact уже committed full transition, не physical commit timestamp. Pending necessary time/history или incomplete ordered range дают IndeterminateDataNotReady. Current committed state может обслуживаться при достаточных правах/restrictions, но не доказывает historical Freeze. Recovery без stored time фиксирует fresh confirmation, не backdates; saved time replay сохраняет. [Q17](../../technical/backend/questions/Q17-publication-availability.md#подтверждённая-видимость-и-история-delivery) задаёт консервативную MVP-границу.


Обычный `Unpublish`, выполненный уже после окончания периода обновлений, не отменяет ранее frozen historical access. Он управляет текущей публикацией, а не запрещает историческое распространение.


`Retire` release или контентной зависимости запрещает её использование в новых публикациях, но не разрушает уже materialized historical revisions и необходимые им данные.


Для запрета дальнейшей серверной выдачи исторического состава используется отдельный явный `Withdrawal`. Withdrawal конкретной revision блокирует дальнейшую выдачу этой revision всем, включая Freeze. История и причина запрета сохраняются.

Ban terminal и независим от current pointer/materialized snapshot: действует также для inactive/unmaterialized exact revision. Late materialization/replay не снимают его. Terminal Withdrawn intermediate во время ordered catch-up даёт processed NotEffective без availability row; technical artifact failure вместо этого stalls и не разрешает skip.


При Withdrawal frozen revision система не подбирает автоматически предыдущую или иную замену. Для MVP возвращается объяснимый отказ. Специально разрешённая replacement revision может быть отдельной будущей функцией.


Уже локально установленные данные клиента автоматически не удаляются только из-за нового отказа. SDK обрабатывает отказ новых requests и сообщает причину. Checkpoint selected session/basis и authoritative bans выполняется до headers/cache/304/Range; unknown temporarily blocks, не объявляется Revoked.

Known committed CM selected revoke/terminal revision ban запрещает новый checkpoint и отменяет active response при обнаружении либо по independently enforced bounded lease deadline. Lease configurable default 5 секунд ограничен Session expiry и next finite policy boundary включая Freeze; delayed renewal не удерживает bytes бесконечно, late success не resurrect canceled response. Already buffered bytes не отзываются, instantaneous/physical 5-second SLA не обещается; VRS→CM facts имеют asynchronous lag.

After-headers abort для SDK означает Unknown outcome, не доказательство Revoked. Range/resume заново авторизуется; восстановление source не resurrect invalidated token. Новая session требует нового admission/plan без silent old-plan binding. Протокол принят в [Q16](../../technical/backend/questions/Q16-activation-and-sessions.md#принятый-протокол-сессий-и-response-leases); блокировка использования/удаление уже installed data не вводятся.


## Затронутые фичи


[F11](../features/F11-publication-lifecycle.md) · [F12](../features/F12-content-entitlement-policy.md) · [F13](../features/F13-content-delivery.md)


## Как применить ответ


Unpublish, Retire, Withdrawal и revoke должны оставаться разными фактами во всех связанных спецификациях. Historical lookup не выполняет неявную замену withdrawn revision другой revision.


## Различие операций


`Unpublish`, `Retire`, `Withdrawal` и отзыв license/grant имеют разные последствия и не должны сводиться к одному статусу.
