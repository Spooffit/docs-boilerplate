# Q17. Момент доступности

## Вопрос и выбранные границы

Как отделить Publishing activation, committed Delivery effective state и доказательство времени для Freeze? Как восстановить ordered историю без пропущенных/null/фиктивных состояний?

**Ответ:** MVP использует async Publishing → Delivery projection и консервативный профиль **подтверждённой видимости** ниже. Конкретные CLR/wire/DDL/DI/clock-failure adapter детали ещё материализуются; бизнесовый checkpoint, порядок, полнота, replay и terminal-ban семантика выбраны. Session credential/TTL/pinned renewal-correction/revoke и independently expiring response leases приняты в [Q16](Q16-activation-and-sessions.md#принятый-протокол-сессий-и-response-leases); physical deployment/wire/measured lag ещё engineering.

[F11](../features/F11-publication-lifecycle.md) · [F12](../features/F12-content-entitlement-policy.md) · [F13](../features/F13-content-delivery.md) · [Q15 contracts](Q15-api-and-source-code.md#delivery-visibility-contracts) · [Q20 persistence](Q20-persistence-and-migration.md#delivery-catch-up-и-подтверждение-видимости).

## Подтверждённая видимость и история Delivery

### Checkpoint и осознанное уточнение правила

Полная projection, effective pointer, effective-history row, LastProcessedPublishingSequence и processed inbox outcome коммитятся атомарно в **собственной Delivery transaction**. Partial snapshot не становится effective. До этого commit прежнее committed состояние сохраняется, если права и hard restrictions позволяют. Publishing ActivatedAt остаётся source/audit временем.

Для Freeze применяется **VisibilityConfirmedAt**: UTC-время primary PostgreSQL, полученное при наблюдении уже committed exact transition/full snapshot, затем сохранённое один раз в durable evidence. Оно **не называется точным временем первого commit или commit подтверждения**. Наблюдение производится после первого commit через fresh primary read; pending evidence до сохранения не выдаётся за confirmed результат.

Это заменяет прежнюю буквальную формулировку «Freeze по actual Delivery commit time». Ревизия, committed до UpdatesUntil, но подтверждённая после, **не входит в Freeze**. Даже очень короткая задержка около границы имеет такой результат. Более раннее Publishing время, timestamp до commit и guessed recovered date не подставляются. Текущий полностью committed snapshot может обслуживаться при достаточном текущем праве; temporal-history readiness для expired Freeze — отдельный факт.

Правило строгое: **VisibilityConfirmedAt < UpdatesUntil**. Равенство исключено; сравнение использует сохранённую точность без округления до секунд. UpdatesUntil остаётся отдельной коммерческой датой, не application ExpiresAt. Agreement/policy binding неизменен; genuine correction пересчитывает выбор по current confirmed cutoff, не переписывает history timestamp.

### Почему не timestamp перед commit

UtcNow/TimeProvider и SQL clock_timestamp до commit не доказывают уже состоявшуюся видимость. PostgreSQL clock_timestamp возвращает момент вызова, transaction_timestamp — начало transaction: [официальная документация времени](https://www.postgresql.org/docs/current/functions-datetime.html#FUNCTIONS-DATETIME-CURRENT).

Альтернативой обсуждался PostgreSQL commit-record timestamp с post-commit переносом в бизнесовую history; track_commit_timestamp требует настройки и его системные данные не бессрочны: [commit information](https://www.postgresql.org/docs/current/functions-info.html#FUNCTIONS-INFO-COMMIT-TABLE). Commit-record timestamp также не тождественен наблюдению клиентской видимости: [PostgreSQL commit processing](https://github.com/postgres/postgres/blob/REL_17_STABLE/src/backend/access/transam/xact.c). Для MVP выбран primary committed observation, без обязательного track_commit_timestamp/CDC или timestamp-as-of реконструкции.

### Confirmation и recovery

1. После подтверждённого original commit worker открывает fresh own Delivery scope/transaction, получает [общий guard](Q20-persistence-and-migration.md#mvp-протокол-consistency-guard) до authoritative reads и наблюдает exact committed history record/full snapshot на primary. При неизвестном исходе original commit сначала выясняет durable transition outcome; потеря ACK не доказательство rollback.
2. Проверяется identity, actual effective order и предыдущая подтверждённая history basis. Confirmation обрабатывается в порядке effective history Variant: более позднее подтверждение не обходит необходимый earlier pending record. Exact historical record подтверждается и после продвижения current pointer; совпадение с сегодняшним pointer не prerequisite. Null record имеет собственное подтверждение, без content artifact.
3. При отсутствии saved confirmation фиксируется primary UTC observation time и durable evidence в own Delivery transaction. Timestamp означает наблюдение **первого уже committed состояния**, а не будущий commit evidence transaction. После commit результат объявляется сохранённым.
4. Lost evidence commit ACK → read сохранённого outcome по exact transition identity. Existing winner timestamp неизменен. Если evidence не было сохранено, recovery делает **новое** наблюдение с новым временем, без backdating. Никакого обновления confirmed timestamps повтором сообщения.
5. При обнаруженном нарушении порядка времени соответствующий temporal result остаётся DataNotReady; нельзя молча max/clamp timestamp либо угадать время. Exact detection/health/recovery adapter и precision/profile codecs — Q15/Q20; иной clock authority не вводится.

### Catch-up и реальные состояния

LastProcessedPublishingSequence отличен от списка действительно ставших effective transitions и их confirmation evidence. Broker event — trigger: публичный Publishing history port возвращает ordered committed range и completeness/watermark. Event arrival alone не доказывает полноту.

При applied/processed 12 и событии 15 восстанавливаются 13 и 14. Пропуски не discarded; incomplete history не выдаётся за отсутствие transitions. Подготовка immutable artifact возможна вне gate; после acquire fresh own/public source order, integrity и exact terminal bans revalidated перед own commit.

**Один реально применённый effective transition — один commit в MVP.** Batch preparation допустима; R1→R2→R3 внутри одного commit не порождает три availability records. Rollback старого RevisionId — новый transition, новый effective order и confirmation. Artifact CreatedAt/Guid/hash не порядок актуальности. No-op Publishing не создаёт новых transitions.

Duplicate processed transition возвращает original durable outcome/evidence. Иной значимый payload для того же Variant+Publishing Sequence — integrity conflict, не нормальный duplicate. Canonical payload comparison/event envelope/details — Q15.

Known permanently terminal Withdrawn intermediate revision получает durable processed outcome **NotEffective**, без effective availability row; затем можно обработать следующие Sequence. Technical failure/missing artifact/unknown ban не дают права так пропустить transition. Source processing/outcome/inbox сохраняются согласованно без fake availability и бесконечного stall на уже доказанном terminal ban.

Предварительная authoritative exact-ban classification выполняется до обязательной artifact preparation: known terminal target не требует успешно читать уже запрещённый artifact. Под gate ban/source order перечитываются перед NotEffective commit. Если подготовка технически не удалась, это не terminal outcome; recovery может заново проверить ban, а при отсутствии доказанного запрета сохраняет failure/retry, не пропускает transition.

### Null, completeness и whole selection

Null — полноценный effective-history state. Expired Freeze выбирает последнее qualifying **confirmed** состояние по actual effective order с VisibilityConfirmedAt < corrected cutoff, включая null. Chosen null = нет разрешённого состава; искать более ранний non-null запрещено. Ordinary Unpublish после cutoff не отменяет прежний qualifying frozen состав. Retire source не вызывает live recomposition historical snapshot.

Read result различает complete empty, confirmed null, pending necessary confirmation, incomplete/corrupt/unavailable history. **Необходимый unknown — Indeterminate/DataNotReady**, не подтверждённый no-publication факт и не разрешение выбрать старый удобный состав. Confirmation readiness и catch-up lag различимы; known previous effective snapshot не обещает полноту любого Freeze расчёта.

Completeness относится к required range/basis конкретного решения. Live catch-up lag или later pending row не вводят blanket veto для иначе разрешённого current committed snapshot либо уже доказанно complete historical prefix. Unprocessed source transition не может позже backdate availability до cutoff. Если необходимая prefix/time basis неизвестна, это по-прежнему Indeterminate; граница required range объясняется read contract, не произвольным skip неизвестных записей.

Multi-license сохраняет [F12 strict verified-set правило](../features/F12-content-entitlement-policy.md): each applicable CurrentPeriod/Agreement independently, any necessary unknown blocks entire selection. При complete facts выбирается whole allowed candidate по authoritative effective order одного Variant. Один и тот же candidate имеет stable selected license provenance; exact deterministic tie codec ещё Q15. Нет item union или сравнения artifact age.

## F10: Withdrawal и historical distribution

[Exact F10 Withdrawal](../features/F10-publication-revisions.md) terminal независимо от current pointer/наличия snapshot. Delivery хранит самостоятельный exact RevisionId restriction для inactive/frozen/unmaterialized revision. Replay/newer activation/late materialization не снимают ban. У unrelated active revision pointer не очищается; active Publishing Withdrawal и его null transition остаются собственным F10/F11 workflow.

New admission проверяет authoritative ban через public owner boundary; protected reads проверяют current restrictions до output cache/304/Range/bytes. Unknown restriction status не становится Allowed. Локальная проекция ban/inbox полезна для durable enforcement, но lagged absence не заменяет authoritative new-admission check. Frozen banned exact candidate Denied, без replacement/older fallback/remote installed-data deletion.

Проверка permission и byte stream разделены: короткий guarded response checkpoint выдаёт independently expiring lease по Q16, gate не держится на download. Renewal wait не продлевает deadline; known ban/deadline cancels writes/upstream, buffers/уже отправленные bytes не отзываются. Physical propagation/cancellation wiring и measured latency ещё Q21/Q22; configured5sec lease не обещает мгновенный запрет всех начатых выдач.

## F11: Publishing active и Delivery effective

Publishing pointer/history/outbox commit, Delivery full effective commit и сохранённое confirmation evidence — **три разных наблюдаемых результата**. Staff diagnostics показывают Publishing active, Delivery committed effective, processed order и temporal readiness; gap, materialization failure, pending confirmation и terminal ban имеют отдельные причины. Совпадающие nullable RevisionIds не доказывают full readiness, dispatcher ACK не равен consumer commit или confirmation.

[Frontend F11](../../frontend/features/F11-publication-lifecycle.md) и [F13](../../frontend/features/F13-content-delivery.md) используют authoritative read models. Новые UI routes/forms/polling interval этим решением не выбираются. Independent summary representation/tag/access/status profile — Q15/Q13.

## F12: Freeze cutoff и effective time

Policy checkpoint остаётся [immutable PolicyAgreement commit](Q20-persistence-and-migration.md#f12-policyagreement-и-подтверждение-периода), не VisibilityConfirmedAt/source timestamp. New CurrentPeriod invalidates old-period session basis, no old same-license Freeze fallback; genuine correction сохраняет Agreement/pin, но future reads reevaluate current cutoff. Expired Freeze требует matching currently eligible confirmed frozen whole candidate, иначе new admission без repin; necessary unknown временно DataNotReady без revoke. Timestamp history не исправляется вместе с cutoff, TTL не продлевается; [Q16 pinned protocol](Q16-activation-and-sessions.md#принятый-протокол-сессий-и-response-leases) принят.

Current [EntitlementSelector](../../../../contentmanager.api/src/BoundedContexts/Delivery/ContentManager.Delivery.Domain/Publications/EntitlementSelector.cs) и [InternalDeliveryService](../../../../contentmanager.api/src/App/ContentManager.Application/Delivery/InternalDeliveryService.cs) используют старую Publishing/current-pointer basis; [current tests](../../../../contentmanager.api/tests/BoundedContexts/Delivery/ContentManager.Delivery.Tests.Unit/EntitlementSelectionTests.cs) содержат latest-null refusal. Это legacy implementation evidence, не выбранный confirmed-visibility профиль и не доказательство readiness.

## Приёмка и открытая инженерная детализация

Нормативные five models/fields/layers/handlers/ports/DF определяются **один раз в backend F13**; F11/F12 ссылаются на них. Обязательны реальные PostgreSQL fault/barrier tests: original commit straddles cutoff, commit-before/confirmation-after, equality, pending history, crash между stages, lost ACK stored/unsaved, two consumers, gap/replay/payload conflict, null, rollback, terminal intermediate, missing artifact/unknown ban, clock order failure и genuine cutoff correction без изменения Agreement/history.

Уточняются final contract signatures/CLR/wire/enum literals, event envelope/canonical comparison/tie rule, indexes/DI/inbox and confirmation adapter, stage recovery/clock health/diagnostic exposure, retry/limits/retention/lag settings и application verification. Семантические checkpoints, separate commits, no fake history, completeness, exact ban и conservative cutoff уже закрыты. Demo остаётся без Publishing/license/broker/session зависимости.
