# F06. Product и ProductVariant

## Цель

Предоставить Content Manager канонический каталог получателей динамического контента из VR School:

- `Product` — абстрактный продукт, например «Музей»;
- `ProductVariant` — конкретный проект/вариант, например «Музей собак».

CatalogAccess хранит локальную проекцию и не становится владельцем исходного каталога.

## Ownership

Product/ProductVariant создаются и редактируются только в VR School.

Content Manager:

- не читает VR School DB;
- не предоставляет локальный CRUD source catalog;
- получает state-carrying snapshots через broker;
- хранит stable source IDs;
- предоставляет read model Publishing/Delivery/UI.

Product/ProductVariant IDs immutable и никогда не переиспользуются.

## Integration model

Catalog V2 snapshot каждой source entity содержит полный актуальный state и positive int per-entity SourceVersion. Gaps допустимы: глобальный sequence, bootstrap и полная история промежуточных состояний не требуются. Source IDs immutable/не переиспользуются; Name не identity.

Product Name имеет 1–120 символов, Variant Name 1–200; optional Description не длиннее 500. VR School нормализует значения до source commit, CM сохраняет их без собственного переименования/повторной normalization. Duplicate names допустимы. Exact source/wire models определяются один раз в backend F06, а не в каждой dependent feature.

Две durable фазы обязательны: receiver atomic сохраняет full candidate + newest watermark + recoverable work/receipt, затем отдельный apply перечитывает newest current facts. Accepted v22 запрещает old v21 apply уже до processing v22. Broker ACK подтверждает receive commit, не applied relation/готовность Publishing. Один newest full candidate имеет одного mutable owner; pending/F15 не создают конкурирующие authorities этого payload.

Same EventId + unchanged fact, включая original OccurredAt, возвращает original durable outcome; changed fact quarantined без overwrite. New EventId + same SourceVersion сравнивает IDs/version/parent/name/description/lifecycle без нового EventId/republication clock: identical state — valid republish/no-op, contradictory state quarantined. Equal republish не переписывает source/history time. Older source version не откатывает watermark/applied state. Detected contradictions относятся к retained receipt/state; после cleanup нет обещания eternal обнаружения всех старых collisions.

Transport retry сохраняет исходные identity/fact; reconciliation создаёт обычную повторную публикацию current state, а pending resolver повторно оценивает сохранённый newest candidate даже при его version == watermark. Resolver не маскируется как новое source сообщение и никогда не applies superseded target. OccurredAt — audit/diagnostics, не ordering вместо SourceVersion.

Отдельного bootstrap каталога нет: новые Product/ProductVariant создаются в VR School, CM получает обычные events. MVP reconciliation — protected operational VR School command для selected IDs либо all current state, включая требуемых parents. Он сообщает scheduled/errors, не applied/readiness; absent requested ID не означает Delete. Current snapshots проходят тот же receiver/acceptance/apply protocol, без hidden CM source CRUD или history replay.

VR School producer использует transactional outbox; CM consumer — inbox/idempotency.

CM и Project Storage имеют независимые subscriptions. Catalog V2 не меняет License/SourcePeriod/PolicyAgreement или credential security-epoch алгоритмы.

## Parent relation и pending state

ProductVariant может прийти раньше Product. CM сохраняет pending/unresolved source state без фиктивного parent и без TTL auto-delete.

До первого apply весь AppliedState Variant отсутствует совместно: pending Name/ProductId не выдаются за partly applied working state. У существующего Variant старое whole AppliedState сохраняется до successful new apply.

Parent arrival создаёт durable wakeup work; crash/restart не теряет необходимый retry. При новом Variant snapshot candidate заменяется newest целиком. Archived parent существует и не считается missing: apply/новое использование отдельно повторяют source lifecycle/feature guards.

## Scoped ingestion unreadiness

Trusted known entity + higher SourceVersion с malformed/unsupported snapshot создают durable CatalogIngestionFault. Это scoped technical unreadiness: оно запрещает stale pending apply и affected new writes, но не превращается в F15 business conflict, Withdrawal/revoke или global catalog block. Если trustworthy entity/version не определить, сообщение quarantined/diagnosed без fake entity blocker.

Recovery — новый исправленный higher-version snapshot либо, для unsupported profile после исправления поддержки, explicit replay unchanged original message. Изменять payload под прежним EventId запрещено. Required unknown/fault не означает compatible/empty dependencies; current historical Delivery и права не отменяются только fault status.

Current/unresolved/conflict history сохраняются. Receipts/completed work retention configurable; после cleanup SourceVersion guard продолжает защищать current state. Это не forever event store. Принятый [Q23 catalog V2](../../technical/backend/questions/Q23-source-synchronization.md#принятый-протокол-каталога-v2) задаёт protocol, exact profiles/typed fields/retry-retention knobs остаются engineering без новых числовых defaults.

## ProductVariant move

MVP поддерживает смену parent Product существующего ProductVariant. Demo этот сценарий не включает.

Variant сохраняет identity и весь локальный CM state.

Если новый parent совместим с текущими CM dependencies, source relation применяется.

Если перенос создаёт конкретную несовместимость mutable assignments/overrides, незавершённой publishing операции или policy scope, открывается F15 CatalogConflict. Само наличие historical immutable releases/revisions не блокирует move. Source snapshot не теряется, а старое applied state остаётся рабочим до resolution.

Newer accepted target заменяет pending того же Open episode; пока fresh assessment не завершён, previous issues не являются current diagnostic. Missing newest parent сохраняет Open + waiting. Compatible source correction автоматически делает System SourceChangedApply и атомарно apply + Resolved history без autopublish/historical rewrite; manual Publishing repair и CatalogAccess Resolve сохраняют отдельные commits.

Сервер защищает проверенные зависимости от конкурентного изменения до фиксации source apply либо затронутой Publishing операции. Этот MVP-протокол выбран в [Q20](../../technical/backend/questions/Q20-persistence-and-migration.md); он сохраняет отдельные фиксации владельцев состояния. Недоступность проверки либо исчерпание ограниченного ожидания координации является техническим отказом, а не доказанной несовместимостью и не пустым набором dependencies.

Проверка относится к prospective состоянию newest parent. Сохранённый override `A → B` при отсутствии A в новой базе сам по себе остаётся допустимым по F08; если новая база независимо содержит B и итоговый состав содержит B дважды, это конкретная коллизия. Release старого Product не меняет владельца при move и не может стать future source нового Product только потому, что когда-то использовался Variant.

## Lifecycle

CM не имеет локальных Delete/Archive/Restore commands Product/ProductVariant.

Нормализованный source lifecycle содержит только `Active` и `Archived`. VR School является единственным владельцем lifecycle; изменение и восстановление приходят полным более новым source snapshot. Скрытие Archived объекта из обычного выбора является отображением этого состояния, а не отдельным `Hidden` lifecycle или локальным CM toggle. `Pending` и `CatalogConflict` относятся к обработке snapshot/relation, а не к lifecycle.

CM сохраняет identity и историю Archived объекта. Archive не выполняет Unpublish, Withdrawal, revoke grants/licenses/keys или отключение historical Delivery. Уже materialized revisions и ранее подтверждённые entitlement продолжают проверяться по F12/F13, в том числе для новой session.

Archived Product ограничивает новые операции своих Variants независимо от их собственного lifecycle: собственное `Active` Variant не перезаписывается в `Archived`. Новый выбор и обычная подготовка/публикация требуют собственного `Active`, доступного Active parent и отсутствия применимых blockers. Возвращение Product в Active не восстанавливает Variant, чей собственный lifecycle остаётся Archived.

Для Archived Product/Variant разрешены чтение и ограниченное сопровождение рабочего состава: сохранить неизменные exact references, удалить позиции или заменить проблемную reference допустимой. Inherited-only exclusion, убирающий позицию, считается удалением, override проблемной inherited позиции допустимым result — заменой. Число patch records не определяет расширение: новые позиции, independent additions и иное расширение запрещены, intrinsic/new-use/scope проверки сохраняются. Open F15 отдельно запрещает ordinary affected writes; corrective repair имеет собственные границы F15.

Archived запрещает новые releases/revisions, обычную activation, rollback и automatic promotion. Ограниченное maintenance не является публикацией. Safety actions Unpublish и explicit Withdrawal сохраняются при обычных permissions/preconditions и проверке scope.

Restore означает новый authoritative snapshot с `Active`, а не локальную CM-команду. Повторный выбор и новые операции разрешаются только если собственный lifecycle Active, parent доступен и остальные актуальные blockers отсутствуют. Restore не активирует revision автоматически, не отменяет Withdrawal и не выдаёт grants.

UI отдельно показывает applied state/relation, lifecycle newest source и processing state. Pending/Conflict не позволяет выдавать newest pending parent за уже применённую рабочую связь. Archived объект доступен в exact/history read, но исключён из обычного нового выбора.

## Grants

Default grant authority относится к конкретному ProductVariantId.

Product-level коммерческое право не распространяется на будущие variants неявно; такая модель должна быть отдельной явной policy VR School.

Один Variant общий для разных организаций/licenses, а не клонируется на покупателя.

## Demo / MVP

Demo не зависит от F06, VR School, broker и licenses.

MVP использует F06 как read source Catalog/Publishing/Delivery.

## Acceptance

- new source Product/Variant появляется без local create;
- duplicate/stale event не дублирует/не откатывает state;
- same EventId changed facts и new EventId same-version contradiction quarantined; identical valid republish no-op;
- newer full snapshot применим без missing intermediate versions;
- source-normalized V2 names/Description limits сохранены, duplicate names не identity; commercial/license/security protocols не меняются;
- Variant-before-Product не создаёт fake parent;
- initial whole AppliedState absent, existing AppliedState intact; parent wake durable через crash, Archived parent не missing;
- Variant move либо применяется, либо открывает F15 conflict;
- durably accepted newer snapshot запрещает apply старого pending, даже когда newest ещё ожидает обработки/parent;
- competing affected write не проходит между проверкой dependencies и фиксацией; technical failure не создаёт ложный compatible result или CatalogConflict;
- source lifecycle имеет Active/Archived; Hidden является отображением, Pending/Conflict — обработкой;
- Archived Product запрещает новые операции Variant, не меняя его собственный lifecycle;
- archive допускает ограниченное maintenance, но запрещает расширение, новые releases/revisions, activation/rollback/promotion;
- archive не выполняет Unpublish/Withdrawal/revoke, не удаляет history и не запрещает новую session по existing entitlement;
- restore приходит новым source snapshot, повторяет guards и не активирует/не unwithdraw/не выдаёт grants;
- UI различает applied/source lifecycle/processing;
- receive ACK не applied/readiness; one newest candidate owner, accepted v22 не позволяет v21 apply до processing v22;
- trusted known higher-version malformed/unsupported creates scoped ingestion fault, unknown identity не fake entity/global block;
- fault blocks stale/affected new use, не historical entitlement/Withdrawal/F15; newer correction либо explicit unchanged unsupported replay восстанавливают обработку;
- newer Open target invalidates old issues; missing parent остаётся Open/waiting, compatible source correction System apply+Resolved без publish;
- protected reconciliation selected/all incl parents сообщает scheduled/errors, missing requested ID не Delete;
- retention receipts/work configurable, current SourceVersion protection после cleanup сохраняется, no eternal old collision/event-store guarantee;
- один variant остаётся общей identity для нескольких licenses.

## Связи

[F15](F15-catalog-conflicts.md) · [F12](F12-content-entitlement-policy.md) · [F13](F13-content-delivery.md) · [Q11](../questions/Q11-catalog-ownership.md) · [Q27](../questions/Q27-catalog-archiving.md) · [Q23](../../technical/backend/questions/Q23-source-synchronization.md) · [Q25](../questions/Q25-catalog-conflicts.md) · [Backend](../../technical/backend/features/F06-product-catalog.md) · [Frontend](../../technical/frontend/features/F06-product-catalog.md)
