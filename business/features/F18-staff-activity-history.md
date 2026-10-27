# F18. История действий сотрудников

## Цель и этап

MVP сохраняет immutable Activity History, чтобы сотрудники видели committed действия с контентом/настройками, автора, target и результат. Это persisted журнал, а не временное сообщение UI. В anonymous Demo фича не обязательна.

## Граница

Activity History не является event store, undo, security log или историей выдаваемого состава. Предметные F11 activation/visibility и F17 moderation history сохраняются у владельцев; F18 дополняет их exact references, не заменяет и не пересчитывает.

Auditable scope включает Create, actual Save, Publish, согласованные lifecycle transitions и Delete своего owner; Publishing release/revision creation, promotion Start/PREPARE/item outcomes, Activate/Rollback/Unpublish/Withdrawal, policy changes/Clear и CancelAutomaticItem; F17 Submit/Comment/Approve/Reject/Withdraw; отдельные F15 repair и Resolve commits.

Staff provision/role assignment/deactivate/reactivate, setup/reset initiation/completion, bootstrap и emergency recovery записываются только в Admin-visible staff-access classification. Meaningful automated Publishing/catalog conflict lifecycle также входит в scope. Не включаются GET/search/preview/download/codegen, Login, failed commands/security counters, transport/inbox/retry noise, обычная projection/Delivery materialization.

## Сценарий

Сотрудник открывает журнал, вручную refresh-ит, фильтрует доступные записи и открывает detail. Видны action, actor kind/captured staff label при наличии, UTC time, typed exact target IDs и confirmed result/details. Realtime, polling, notifications и export не входят в MVP. Переход на mutable Draft открывает текущий объект той же exact identity, не обещает старое содержимое.

По актуальному [Q13](../../technical/frontend/questions/Q13-ui-scope.md) MVP показывает самостоятельный Activity History screen `/admin/activity`. Дополнительный обязательный audit внутри каждой карточки не вводится. Предметные истории F11/F17 остаются у владельцев. Показать лишь системный timestamp без автора/действия недостаточно.

## Правила

Одна запись относится к одному independent committed business result. Whole-tree Save — одна запись; atomic automatic PREPARE tuple — одна запись с revision/iteration/item refs. Human Approval и subsequent activation — разные результаты. Outcome=Committed означает durable собственный результат; typed details могут отражать partial/Failed business workflow item, не заявляют, что все targets activated. Failed command без commit не становится таким outcome.

Actor kind — Staff/System/Integration/Operator. Staff actor захватывается из trusted CM identity при исполнении, не из текущего viewer. Automatic executor отличается от trusted persisted Staff initiator операции; внешнему integration/operator не назначается fake CM staff. SourceOccurredAt и AuditRecordedAt — UTC observations, не physical commit/causal clock. Safe actor/target labels optional as-of; rename/deactivate/delete не переписывают старую запись.

Publishing replay confirmed ClientOperationId возвращает исходное выполнение, не создаёт второе успешное действие в истории. No-op same-active Activate/null Unpublish не выдумывают activation history; explicit CancelAutomaticItem относится к exact item и не выдаётся за Unpublish. Item Activated отражает подтверждённый выбор Publishing отдельно от Delivery effective/readiness; moderation Approval остаётся отдельным human decision.

В F15 corrective patch repair и Resolve отражаются отдельно: repair с Variant/Conflict/проверенным source basis, Resolve с committed newest apply. Repair success не означает успешный move, а отказ subsequent Resolve не отменяет уже committed repair. Actual no-op не создаёт новой записи; intentional new artifact даже с equal content является новым результатом.

Metadata whitelist ограничен IDs, old/new state, role/policy values, counts и operation/correlation references с safe optional labels. Не копируются полный payload, content values/before-after tree, storage URLs, ETags, пароли/codes/keys/session credentials и прочие secrets. Полный F17 reason остаётся authoritative у exact moderation/revision owner; denied/gone/unavailable ref показывается явно, не как пустое замечание или complete short summary.

## Visibility и чтение

Common content journal доступен Specialist и Administrator по audit.activity.read. Staff-access classification доступна только Administrator по audit.staff-access.read; fixed role mapping F16, без personal SubmittedBy/ownership filter. Audit read не предоставляет target-read permission: переход/ref имеет свои current access rules. Unknown visibility profile не считается безопасным общим record.

Server visibility применяется до paging/count/filter options. Filters: actor kind + staff ID, action, target type/ID, UTC [From,To), correlation/operation. Default temporal cut отсутствует. Authors выбираются из visible audit history, не из Admin user directory.

Keyset order OccurredAt DESC, Id DESC; default page 50, maximum 200. Cursor bound filter + reader context, каждый request fresh авторизуется. NextCursor/HasMore не требуют total count; concurrent append не обещает stable paging snapshot. Read-only API включает GET /api/v1/activity, exact GET /api/v1/activity/{activityId} и filter-options endpoint; публичных mutations нет. Exact wire/types/codes определяет backend F18/Q15.

## Надёжное сохранение и retention

Source audit intent фиксируется атомарно с own committed result. Dispatcher at-least-once сохраняет stable EventId/source-fact identity; собственный Audit row Id приёмник выделяет позже и независимо. Audit append + inbox коммитятся в собственном scope без cross-BC transaction. Duplicate same source facts не создаёт второй row; changed facts same EventId/source identity quarantined без overwrite старой записи.

Append eventual: source success не ждёт Audit availability, UI не создаёт локальный final row по нажатию кнопки. Пустая query не доказывает отсутствие committed действия при delivery lag. Source intent нельзя заменить best-effort logging после commit.

MVP сохраняет все audit records без automatic purge, user deletion/FK cascade или переписывания captions. Export/archive/exceptional anonymization требуют поздней explicit policy; это scope MVP, не legal promise вечного хранения.

## Критерии приёмки

| Сценарий | Ожидаемый результат |
| --- | --- |
| Сотрудник выполнил отслеживаемое действие | История связывает его с автором, временем, целью и результатом |
| Админ вернул контент | Видно замечание и то, к чему оно относится |
| История показана после перезагрузки | Она не является только временным UI-сообщением |
| Действие не было подтверждено сервером | Не отображается как заведомо успешно выполненное |
| Repair сохранён, Resolve ещё не выполнен или отвергнут | История показывает repair, не утверждает успешный move |
| Whole-tree Save/atomic PREPARE и subsequent Approval/activation | Один record на own committed result; distinct решения отдельно с exact refs |
| No-op/replayed ClientOperationId vs intentional equal-content artifact | Первые не создают новый row, новый artifact создаёт |
| Meaningful automatic item/catalog conflict outcome | Executor kind и persisted staff initiator различимы; Failed item details не failed command |
| Staff-access record и common content record | Spec/Admin видят common по audit.activity.read; staff-access только Admin по audit.staff-access.read |
| Unknown profile/denied target reference | Нет safe-by-default visibility или выданного target-read права |
| User/target renamed/deactivated/deleted | Immutable optional as-of labels/IDs/history прежние, no cascade |
| Metadata/detail/return reason | Только whitelist, full F17 reason через exact owner ref; unavailable не empty remark |
| Filters/author list/page/cursor | Visible history до paging, no admin-directory dependency; default 50 / max 200, fresh auth/context-bound cursor без snapshot promise |
| Source commit и Audit outage/repeated dispatch | Source durable intent сохраняется; eventual append/inbox idempotent, changed duplicate quarantined |
| Empty refresh при append lag | Не доказательство отсутствующего действия; UI не fake local final row/realtime poll |
| Retention | No MVP auto-purge/user cascade; exceptional privacy/export policy не выдумывается |
| Нет отдельных пользователей в Demo | Отсутствие этой фичи не блокирует демонстрацию |

## Уточнения и связи

Scope/visibility/retention приняты в [Q10](../questions/Q10-permissions-and-isolation.md#f18-history-scope-и-visibility). Exact CLR/wire/machine codes/cursor protection и technical implementation относятся к [Q15](../../technical/backend/questions/Q15-api-and-source-code.md#f18-принятый-activity-contract) и [Q20](../../technical/backend/questions/Q20-persistence-and-migration.md#f18-принятый-capture-append-и-cursor-read). Полные models только backend F18; это спецификация, не заявление о готовом коде/пройденных application tests.

[F16](F16-staff-access.md), [F17](F17-content-review.md), [M06](../modules/M06-staff-workflow.md). [Бэкенд](../../technical/backend/features/F18-staff-activity-history.md) · [Фронтенд](../../technical/frontend/features/F18-staff-activity-history.md).
