# Q20. База и миграция
## Вопрос и известные границы
Какой провайдер БД и mapping уже используются, есть ли ценные dev/staging-данные и требуется ли переходный период переименования?
**Ответ:** PostgreSQL, отдельные schema/DbContext/migrations каждого BC и отсутствие foreign physical FK/cross-BC transaction приняты ниже. MVP consistency guard выбран в следующем разделе. Ценных production/staging данных нет; до первого запуска разрешён destructive переход к target. Concrete physical mappings/adapters/settings остаются инженерной детализацией, а не повторным выбором provider/coordination/data-preservation policy.
## Затронутые фичи
[F01](../../../business/features/F01-create-entity-schema.md) · [F02](../../../business/features/F02-create-schema-version.md) · [F04](../../../business/features/F04-entity-instances.md) · [F06](../../../business/features/F06-product-catalog.md) · [F10](../../../business/features/F10-publication-revisions.md)
## Как применить ответ
Уточнить связанные спецификации в их собственных разделах. Не считать заглушку принятым решением.

## MVP: протокол consistency guard

### Принятый механизм и область действия

MVP использует одну PostgreSQL database и один общий exclusive **transaction-level advisory lock** для коротких mutations, проверяющих взаимозависимые факты. Все реплики/версии CM используют один постоянный lock key и тот же primary database. Техническая сериализация не вводит business blocker unrelated scope: допустимая операция ожидает очередь, затем проверяет свой scope. Цена общего guard — последовательность защищённых mutations; менять granularity можно только отдельным проверенным протоколом после измерений.

`pg_advisory_xact_lock` выполняется на **том же физическом connection и внутри той же local transaction**, которая коммитит owner mutation. Отдельный lock connection, session-level lock, process mutex как MVP-гарантия и автоматически истекающая lease не используются. Lock освобождается при завершении transaction; потеря connection запрещает продолжение операции на другом connection. Основание: [PostgreSQL advisory locks](https://www.postgresql.org/docs/current/explicit-locking.html#ADVISORY-LOCKS).

Это не cross-BC SQL transaction. Команда меняет только свои roots, retention/dependency facts и обязательные durable intents в owner boundary. Foreign queries выполняются через публичные ports владельца в independent read scope/connection, без enlistment в caller UoW. Current `EfCoreUnitOfWorkParticipant`/`PostgresConnectionRegistry` умеют объединять несколько contexts; target registration обязана отделить owner command UoW от foreign query scopes. Иначе объявленный public port скрыто возвращает запрещённую общую transaction.

Demo остаётся one-process с application-level AssetId guard; MVP coordinator не добавляет ему Publishing/broker/Identity/multi-replica. Q16 выбирает отдельный credential/TTL/response lease, Q17 confirmed observation time; общий guard покрывает short authorization checkpoint, но не держится на stream и не доказывает немедленный физический отказ всех начатых выдач.

### Порядок mutation и ответственностей

1. Api/transport проверяет shape/access и binds исходный own `If-Match` или internal command precondition. Предварительное чтение не становится разрешением commit.
2. Owner Application/UoW создаёт fresh command scope и explicit **Read Committed** transaction своего BC.
3. Owner PostgreSQL adapter выполняет standalone `SELECT pg_advisory_xact_lock(...)` до authoritative reads и row locks. Не объединять acquisition с domain SELECT. Не переносить pre-lock tracked entities/cache/basis в authoritative validation.
4. Handler перечитывает own roots и foreign committed facts через independent primary public read ports. Нет lagging replica, stale cache, старого Repeatable Read snapshot или foreign write. READ COMMITTED даёт последующим statements fresh committed snapshot после ожидания lock: [PostgreSQL isolation](https://www.postgresql.org/docs/current/transaction-iso.html#XACT-READ-COMMITTED).
5. Проверяются own expected Version, lifecycle/new-use/retained semantics, scope/blockers, source watermark и captured review/repair basis. Missing/unavailable dependency не превращается в пустой compatible result.
6. Domain выполняет собственное изменение; owner Persistence атомарно сохраняет roots/children, Version, нужные history/retention facts, outbox/safe audit intent и enlisted own inbox outcome.
7. Commit/rollback завершает local transaction и освобождает lock. Api возвращает result собственного подтверждённого commit; последующий workspace read не заменяет этот результат.

Порядок locks: общий guard первым, затем необходимые owner row locks в стабильном порядке. Нельзя сначала взять row lock, потом ждать global guard. Foreign query не получает второй exclusive guard на другом connection и не вызывает command. Вложенные действия внутри одного BC используют тот же owner transaction/scope; изменения другого BC выполняются отдельным durable workflow после commit. Guard не освобождается через savepoint; acquire выполняется до recoverable savepoints, rollback перед acquisition не позволяет продолжить с предполагаемой защитой.

### Технические boundaries и модели

Ниже целевые технические обозначения для implementation, не утверждение наличия типов в current src. Guard не создаёт Domain entity, business event или публичный HTTP DTO.

| Boundary / слой | Контракт и ответственность |
| --- | --- |
| Owner Application + UoW boundary | Begin local mutation transaction → acquire → fresh checks → own commit/rollback; handler никогда не выбирает foreign DbContext |
| Shared technical Application.Abstractions | Логический `ICmConsistencyGate.AcquireExclusiveAsync(CancellationToken)`; подтверждённый acquire действует до конца текущей owner transaction; no source/catalog business logic |
| Owner PostgreSQL/Persistence adapter | Использует current owner transaction connection, проверяет наличие transaction/принадлежность scope, применяет единый key и bounded acquisition; никакого standalone lock connection |
| Independent public read adapter | Отдельный owner read scope/primary connection и fresh materialized read models; no caller ambient enlistment, command, stale tracker или cache |
| Host/DI | Owner-isolated UoW, единый lock profile/options across replicas, fail startup для неверного timeout/key deployment; scope/reentrancy защита и writer coverage |
| Api/transport | Cancellation/error mapping, own ETag отдельно independent basis; unknown commit outcome не optimistic success/rollback |

**CmConsistencyGateKey** — immutable technical value, не persisted business record.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| Namespace | `int` | Да | Namespace двухчастного PostgreSQL advisory key | Постоянная project-wide constant; одинаковая на всех replicas, не randomized hash/process ID |
| Resource | `int` | Да | Единственный global MVP resource в namespace | Постоянная constant; не ProductId/AssetId и не client input; literals закрепляются единым infrastructure contract |

**CmConsistencyGuardOptions** — server technical configuration, не request model.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| AcquisitionTimeout | `TimeSpan` | Да | Верхняя граница ожидания acquisition | Positive, startup validated; numeric default/upper bound Q22 по измерениям, cancellation учитывается |

Exact signatures/result/DI project placement, key literals, command timeout/cancellation adapter и scope assertions материализуются при реализации в этих boundaries. Это инженерные контракты выбранного механизма, не новый выбор distributed lock или foreign transaction. Own `Version`/business `Sequence`/`SourceVersion` в lock key или gate options не помещаются.

### Участие writers

| Владелец / фичи | Обязательные защищённые изменения |
| --- | --- |
| ContentModeling F01–F04 | Authoring mutations/lifecycle/delete, Copy/Publish, создание/удаление references и orphan retention, baseline/global number/history changes, способные менять проверяемые условия |
| MediaLibrary F05 | Metadata/lifecycle transitions, final upload availability, usage→Deleting commit и final deletion/recovery metadata |
| CatalogAccess F06/F15/F12 source facts | Durable snapshot acceptance/newest watermark/pending, apply/retry/reconciliation, source lifecycle/move, Open/supersede/Resolve и readiness facts; confirmed period/AgreementId/current period/completeness/correction/revoke факты |
| Publishing F07–F10 | Setup owner creation, assignment/patch Save, corrective patch repair, release/revision Create и lifecycle/retention/Withdrawal |
| Publishing F11/F12/F17 | Source/membership capture promotion, отдельные item stages, state/policy changes, immutable PolicyAgreement creation/request replay binding, activation/rollback/unpublish, decision/automatic continuation с current basis |
| Delivery F12/F13 | Binding/admission/invalidation commits, processing outcomes/effective-state switches/visibility confirmation-recovery и exact restrictions, способные изменить проверяемый policy/history/restriction/Approve comparison basis |
| Background paths | Internal setup, imported/source facts, retry/reconciliation и consumers, меняющие перечисленные conditions; transport entry point не даёт обхода |

Матрица writer→checked fact→owner commit фиксируется в feature DF/handlers/test strategy. Guard не защищает от writer, не участвующего в протоколе. VR School producer и storage не коммитят CM state: guard определяет порядок committed CM facts, а не обещает знание внешних изменений до их получения.

F06 durable acceptance более нового полного snapshot под guard сохраняет authoritative newest candidate/watermark. После такого commit v21 resolver обязан отказаться при v22, даже если impact/parent processing v22 ещё не закончено. Inbox receive ACK, durable acceptance и applied outcome различаются; physical receive/worker split ещё Q23, но newest boundary не откладывается до удобного старому resolver apply.

Repair и Resolve — отдельные owner transactions, каждая с новым acquire/fresh validation. Publishing repair не держит gate до ручного Resolve. Committed repair сохраняется при новом source; CatalogAccess не применяет stale basis и не делает automatic rollback чужого patch.

### Длительные действия, outbox и reads

Upload/download, VR School/Identity calls, S3 I/O, broker publish, user confirmation и ожидание projection выполняются вне gate. CPU preparation допустима до gate, но её exact inputs/basis повторно проверяются под ним. Auto promotion не держит один gate на batch/moderation wait: каждый отдельный durable stage захватывает его заново.

Asset delete: fresh complete usage под guard → own commit `Deleting` → release → idempotent exact storage delete вне transaction → новый acquire/fresh check → final own metadata commit. Все reference writers отвергают Deleting; storage failure сохраняет durable retry state, не освобождает объект для нового использования.

Outbox/safe audit intent сохраняется с owning mutation; dispatch и Audit/Delivery consumer commit независимы. Ошибка позднего dispatch/audit/projection не отменяет уже завершённое business действие. Guard не делает внешние effects exactly-once и не подменяет Delivery-effective time.

Обычные GET/pickers/previews не получают общий exclusive gate. Они показывают observations, а не commit permit. Coherent own DTO+ETag требует своего read/materialization profile; independently changing preview/labels/dependencies не объявляются защищёнными own ETag. Команда повторяет fresh checks under gate.

### Ошибки, recovery и проверка

Own stale `If-Match` остаётся 412, missing required precondition — 428; independent domain/basis mismatch — 409 согласно owner contract. Global queue не отменяет эти проверки и не меняет Version источника лишь из-за read/CreateRelease. Две последующие Save со старым tag не оба успешно изменяют root; разрешённые repeated Create с unchanged source могут оба succeed.

Acquisition timeout/cancellation или foreign read failure не являются business CatalogConflict и не разрешают commit. Exact HTTP/code/retry-after profile Q15; bounded numeric caps Q22. Соединение нельзя прозрачно заменить внутри операции после acquisition. Server-side session/transaction cleanup определяет освобождение при crash; отсутствие ответа не доказывает немедленный rollback.

Lost commit acknowledgment — unknown outcome: использовать согласованный operation identity/result inquiry/readback владельца. Blind retry, подмена old If-Match новым и объявление результата rolled back запрещены. Technical retry допустим только с fresh context, acquire и повторением исходных preconditions после определённого safe failure; политика неопределённого commit отдельно Q15.

Внутренняя setup mutation также участвует; semantic owner unique constraint защищает duplicate creation. GET не создаёт root и не возвращает fabricated empty ETag при задержке setup. Source apply и setup остаются двумя BC commits с явной readiness boundary.

Обязательные real PostgreSQL barrier/fault tests: Save ↔ Archive/Delete/Open; Resolve ↔ Save/newest acceptance; repair ↔ source supersede; CreateRelease ↔ Save/Retire; activation ↔ Withdrawal/decision; reference creation ↔ Deleting; initial owner duplicate/setup; rollback/cancellation/connection loss/lost ACK. Использовать реальные writer paths и independent contexts/connections. Проверить отсутствие foreign enlistment/shared transaction, порядок gate→row lock и повторную fresh validation после ожидания.

Metrics: acquisition wait, held transaction duration, queue timeout/cancel/failure, operation kind и writer path; no credentials/full payload. Числовые limits/alerts определяются representative load tests. Общий gate принят для MVP, возможность Production throughput/physical BC separation требует отдельной оценки.

## Publishing workflow: capture и продолжение

### Артефакты, closure и review

F09 explicitly-created empty ProductContentRelease и fully empty Variant patch release разрешены. F10 отдельно требует ≥1 exact source и nonempty final composition. Новый намеренный CreateRelease/CreateRevision получает новый artifact ID даже при одинаковом content/comment; content hash не request dedup/Approval identity.

F10 new-use facts относятся к final effective instance/schema versions и реально используемой media closure. Полностью suppressed A не блокирует output B своим lifecycle; shared schema/media в B проверяются. Source releases Available/owner и intrinsic trusted capture evidence проверяются отдельно. F17/F11/F13 используют saved effective closure и соответствующие current target restrictions без recomposition; historical retention source/evidence closure шире. Нового Media Withdrawal state это не вводит.

Semantic predecessor — latest-created same-owner release по ReleaseNumber, включая Retired, либо absent first baseline. Exact predecessor ID фиксируется с review/Create provenance. Позднее созданный другой release не заменяет captured predecessor; изменённый assignment/validation context/necessary dependency basis требует fresh review.

### CommandExecution и ClientOperationId

Обязательный client-generated `ClientOperationId:Guid` вводится для F09 Product/Variant CreateRelease, F10 CreateRevision, F11 StartPromotion и F17 Submit. Server PromotionOperationId/PromotionItemId — другие identities. Идентификатор принадлежит одному intent; клиент сохраняет его вместе с исходным payload/If-Match/context/review/comment до выяснения результата. Intent с изменённым input получает новый ID.

`CommandExecution` — immutable Publishing-owned technical record, не moderation/activation event store. Поля определены здесь один раз; feature ссылается на этот storage contract.

| Поле | Тип | Обязательное | Описание | Constraints |
| --- | --- | --- | --- | --- |
| CommandKind | `PublishingCommandKind` | Да | Scope подтверждённого command | ProductReleaseCreate, VariantReleaseCreate, PublicationRevisionCreate, PromotionStart, ModerationSubmit; technical enum, wire names Q15 |
| ClientOperationId | `Guid` | Да | Client intent identity | Non-empty; unique CommandKind + ClientOperationId внутри Publishing |
| ActorId | `Guid` | Да | Trusted initiating CM staff | Captured server-side; не body actor; actor входит в normalized comparison, viewer authorization проверяется заново |
| NormalizedInput | `string` | Да | Immutable deterministic command-input representation | Meaningful fields, original parsed source If-Match, exact owner/context/review/previous IDs и comment; no raw secrets/headers; codec/literal fixtures Q15 |
| OriginalResult | Уточняется | Да | Immutable original confirmed execution receipt | Artifact/Operation/Moderation identity + подтверждённый original result; не current lifecycle view, не fresh resource token; CLR/result carrier Q15 |
| ConfirmedAt | `DateTimeOffset` | Да | Source commit outcome time | Trusted server metadata; не Delivery-effective time, не input identity |

После current authentication/authorization handler под owner gate сначала lookup подтверждённого execution. Same scope/ID/normalized input/actor возвращает original receipt **до** new-execution source If-Match/lifecycle checks. Later source edit/Retire не меняет уже совершённое действие. Bound same ID/different meaningful input/actor — conflict; результат другого actor не раскрывается обходом access check.

Если execution отсутствует, handler выполняет current domain/source/context checks и атомарно коммитит artifact/start/submission и execution/result. Rejected до commit request не сохраняется как successful bound execution; transient failure не становится permanently cached refusal. Concurrent same-ID requests под global gate либо читают committed original result, либо первая failed transaction не создаёт результата. Normalized input comparison не использует ContentHash как замену command identity.

Replay — inquiry подтверждённого действия, не новое Create/Restore/Submit/Activate/audit event. Current lifecycle/details читаются отдельно обычным exact reader при необходимости. Lost ACK сохраняет исходный ClientOperationId/input; новое поколение source If-Match нельзя подставить в retry незаметно. SQL unique constraint, normalized codec/result serialization/retention и exact response metadata материализуются без изменения этих гарантий.

Normalized input — исходные parsed caller поля + trusted actor, без fresh recapture latest release/current membership до lookup. Captured derived sources/basis сохраняются в original operation/result. Staff/action/scope authorization не включает new-use lifecycle checks до confirmed replay lookup. Public Submit требует ClientOperationId; внутренний autoSubmit same-BC preparation использует exact ItemId/stage/tuple, не общий Start client key для всех items и не random client ID retry. Trusted initiator пришёл из accepted Start, не из fake body actor.

### Operation start и источники

Под MVP gate StartPromotion подтверждает exact Product release/base action и сохраняет immutable operation inputs, trusted initiator и applied Variant membership. Captured ProductPublicationState.Version берётся **после** собственного изменения base, если оно было. Actual base transition меняет own Version; no-op не симулирует change. Captured Variant publication Sequence фиксируется для каждого item; no-op pointer action её не меняет.

В membership входят applied Variants выбранного Product; Archived/conflicted members имеют видимый Blocked result, newest unapplied parent не подставляется. Новые/перемещённые позже Variants не добавляются. Zero-member operation завершена с zero items; base selection имеет самостоятельный outcome. Source capture/facts/intent reservations и durable operation/items принадлежат одному Publishing start commit, не общей транзакции всего последующего batch.

Local source — latest-created VariantRelease по ReleaseNumber. Retired latest даёт Blocked, без older Available fallback. Never-any-release и nonempty хотя бы одна из четырёх working groups дают Blocked; never-any-release + no work допускает BaseOnly. Explicitly-created fully empty release — существующий exact source. Later Draft/local release не подхватывается старым item. Captured release retiring **до** revision preparation блокирует new use; Retire source **после** materialization сам по себе не отменяет saved revision.

### State owners и приоритет

Полные поля `PromotionOperation`, mutable `PromotionItem` и внутреннего `VariantPromotionIntent` определены в [F11](../features/F11-publication-lifecycle.md). Operation inputs/membership/initiator immutable; item own int Version защищает именно этот item, progress не Touch-ит все sibling items. Summary derived из items/coherent read, не глобальный mutable aggregate token для каждой стадии.

Новый accepted automatic intent per Variant заменяет current head и supersedes older unfinished item, в том числе same-base restart. Уже Activated result/history не undo. Cancelled/Superseded/другие terminal outcomes не автоматически возобновляются source Restore/F15 Resolve/replayed decision. Нельзя восстановить previous automatic head после завершения нового intent.

Actual manual Activate/Rollback/Unpublish и active Withdrawal atomically меняют pointer/history/Sequence и инвалидируют old automatic continuation. Manual no-op same-active/already-null сохраняет no-Version/no-history/no-outbox и **не** отменяет automatic intent. Separate CancelAutomaticItem действует по exact ItemId и own item precondition: current head очищается только если всё ещё указывает на этот item; publication pointer/history не симулируются. Cancel не отменяет заменивший item по одному VariantId.

Перед continuation под новым acquire проверяются exact bound RevisionId/ModerationId Approved, unfinished item/current automatic head, captured Product state Version и Variant Sequence, current base identity/applied parent/lifecycle/F15/effective restrictions. Product A→B→A и Variant R1→R2→R1 дают changed generation и не оживляют old continuation. Failed freshness/domain condition фиксирует объяснимый non-success item outcome; no stale activation/fallback.

### Устойчивые стадии и retry

Preparation — **один Publishing commit**: F10 CreateRevision + F17 automatic Submit + exact item tuple binding + AwaitingModeration + необходимые own intents. Старое обязательное PreparedEvent→отдельный Submit больше не correctness boundary. Domain/application use cases переиспользуются внутри owning BC, не через HTTP loopback или новую shared BC transaction. Подготовка может вычисляться заранее, но final owner gate перечитывает item/inputs/eligibility до commit; competing workers не коммитят второй target/iteration для уже prepared item.

Human decision — отдельный guarded commit F17. Own moderation If-Match и показанный current Delivery-effective comparison basis независимы. Compare basis drift требует refresh/new consent/independent409; unavailable не confirmed no-effective. Все writers, меняющие actual effective basis, участвуют в общем gate. Committed Approval позднее не отменяется только из-за смены comparison baseline; activation проверяет собственные current intent/basis facts.

Decision/outbox continuation относится к exact ItemId/RevisionId/ModerationId. Retry использует committed tuple; чужую Pending iteration/hash/похожую revision не adopts. Withdrawn iteration cancels только bound item; новый manual Submit withdrawn revision создаёт новую iteration без старого automatic binding. Rejected сохраняет прежний pointer. Supersede/Cancel item не Withdraw-ит human moderation: допустимый Approved target может использоваться будущим explicit manual activation.

Activation commit atomically записывает item Activated, pointer, append-only history/Sequence и outbox. Decision/inbox replay сначала читает terminal item outcome, не активирует снова после manual action. Нельзя по текущему pointer восстановить якобы потерянный item success в обход exact committed correlation. Crash до commit откатывает всю стадию, после commit recovery читает durable outcome.

Safe technical retry продолжает тот же item с frozen sources/tuple и current stage/version; no latest adoption/new target. Retry budget/backoff/claim/lease numeric profile остаётся configurable materialization. Domain retry после исправления — **новая** operation/confirmed subset с new ClientOperationId и trace предыдущего результата; old outcome/history сохраняются. Production/Delivery readiness не равно Item Activated, собственная async effective boundary Q17/F13 сохраняется.

### Приёмка и оставшаяся детализация

Real PostgreSQL barrier/crash/inquiry tests: Product A→B→A, Variant R1→R2→R1, new head before/after preparation/Approval, manual no-op vs exact Cancel, duplicate decision после manual action, prepare/decision/activation commit crashes, lost ACK и original ClientOperationId replay after source edit/Retire, different input/actor, Withdraw→new manual iteration, empty release/nonempty-or-empty F10 result, suppressed-only vs shared dependencies. Проверять реальные writer/consumer paths и separate owner contexts/gate.

Closed policies: empty/duplicate artifacts, predecessor, effective closure, frozen operation inputs, latest-Retired block, latest intent/manual/no-op/cancel/ABA precedence, preparation atomicity, exact continuation и independent review consent. Open engineering: exact public basis/input/result CLR/wire/status metadata, mappings/indexes/DI/inbox transport/normalized fixtures и numeric retry settings. Q16/Q17 period/session/effective-clock протоколы не закрываются этим workflow.
## Уточнение блока 8: stable SchemaFieldId
Одна логическая field identity (`SchemaFieldId`) должна присутствовать в нескольких schema-version snapshots без регенерации ID. Текущий persistence mapping необходимо проверить: primary key/owned-entity модель не должна запрещать один стабильный field ID в нескольких версиях.
Физический вариант пока не выбран: отдельная логическая сущность поля + versioned definition, composite membership `(SchemaVersionId, SchemaFieldId)` либо иной mapping. Требование к смыслу фиксировано, SQL/EF-схема остаётся задачей Q20. Старые Published snapshots должны оставаться неизменяемыми.
## Current-state snapshot 01.10.2026
Подробности: [current-state code snapshot](../../../archive/current-state-code-snapshot-2026-10-01.md).
Все предоставленные BC EF factories/configurations ориентированы на PostgreSQL. В ContentModeling `EntitySchemaVersion.Content` и `EntityInstanceVersion.Content` сохраняются в `jsonb` через value converters; published canonical JSON хранится отдельным text-полем. `Revision` aggregate используется как concurrency token.
Current DB constraint `ux_schema_one_draft` разрешает только один schema Draft; `ux_instance_one_draft` — только один instance Draft. `EntitySchemaVersion.VersionNumber` major/minor required уже при Draft. Эти constraints **расходятся с target schema-version model** и являются migration/refactor work, а не бизнес-требованием.
Поскольку SchemaField snapshots сериализуются внутрь JSONB, current storage не имеет отдельной таблицы fields. Он способен хранить одинаковое значение SchemaFieldId в разных JSON snapshots, но target stable identity/monotonic evolution всё равно требует явной проверки application/domain semantics. Наличие JSONB не закрывает выбор будущего mapping.
Snapshot сам по себе не определял ценность данных. Последующее решение ниже закрывает этот вопрос: ценных production/staging данных нет, destructive migration до первого запуска допустима.
## Уточнение блока 10: EntityInstance persistence target
Target снимает current `ux_instance_one_draft`: один EntityInstance может хранить несколько Draft versions. `EntityInstanceVersion.VersionNumber` в бизнес-смысле отсутствует до Publish; persistence не должен заставлять API выдавать фиктивный номер Draft. Published number назначается конкурентно-безопасно одной последовательностью на EntityInstance.
`EntityInstanceKey` immutable и unique внутри EntitySchema, поэтому storage должен обеспечить логическую уникальность `(SchemaId, Key)` (конкретный index/mapping зависит от итоговой физической модели). `Name` не участвует в identity.
Для explicit cascade delete Draft schema требуется транзакционно согласованное удаление dependent Draft instance versions; простой FK cascade без domain/application проверки не считается достаточной реализацией UX-правила.
Demo delete физический и не требует soft-delete columns. MVP Archive/Restore вводятся отдельно и должны сохранять исторические refs; миграцию нельзя предвосхищать скрытым `IsDeleted` только ради удобства.
## Решения блока 17: границы persistence между BC
Target сохраняет уже используемую модель PostgreSQL с отдельной schema для каждого bounded context. Каждый BC имеет собственный `DbContext` и собственные migrations/history; чужие таблицы не подключаются в mapping другого BC.
Между schemas разных BC не создаются physical foreign keys. Cross-BC reference хранится как stable ID и проверяется через публичный contract/port владельца данных.
Код одного BC не инжектит `DbContext`, repository, EF entity или persistence implementation другого BC. Синхронное межмодульное чтение допустимо только через публичные Application.Abstractions/query ports.
Техническая возможность выполнить одну transaction через несколько BC в текущем решении **не является разрешённой архитектурной моделью**. Новые сценарии не должны строить cross-BC atomic transaction; это правило нужно закрепить architecture tests. Atomic invariant заканчивается внутри одного BC, межмодульные последствия передаются через event/outbox либо явный read-port.
Одна PostgreSQL instance/database и один application DB principal допустимы для MVP; изоляция обеспечивается schemas + code/architecture boundaries. Отдельные DB users нужны только при будущей физической декомпозиции, а не заранее.
Проект ещё не запущен и ценных production/staging данных, которые требуется сохранить, нет. До первого запуска разрешены destructive migrations, удаление лишних tables/BC и полная перестройка persistence model, если это упрощает переход к target. Исторические migrations можно пересобрать согласно принятой repository practice; не нужно сохранять legacy storage только ради compatibility с неиспользуемыми данными.
### MediaLibrary
Решение уточнено блоком 18.
Текущую слабую реализацию `MediaLibrary` не требуется сохранять ради совместимости, но **целевое состояние уже Demo включает отдельный `MediaLibrary` bounded context**.
Этапное решение:
- Demo не обязан зависеть от окончательной BC-декомпозиции media; F05 может временно быть реализована рядом с ContentModeling, если это ускоряет проверяемую цепочку upload → AssetReference → Delivery;
- уже в Demo media ownership принадлежит `MediaLibrary`;
- `MediaLibrary` владеет Asset metadata/lifecycle и public media application boundary;
- object-storage adapter относится к Infrastructure этого BC; только действительно универсальные storage/stream primitives могут жить в Shared;
- Delivery не читает MediaLibrary persistence напрямую: historical publication использует immutable AssetId/dependency references и публичный media/storage contract.
Таким образом, удаляется не целевая идея MediaLibrary, а требование сохранять текущую реализацию без переработки.
## Решение блока 23: общий optimistic concurrency contract
Content Manager использует единый optimistic-concurrency подход для всех mutable aggregate/state, где возможны конкурентные записи.
### Внутренний Version
Mutable owner хранит отдельный внутренний `int Version`.
- начальное значение задаётся persistence/domain convention и не является business number;
- каждая successful state-changing mutation увеличивает Version атомарно вместе с изменением;
- EF mapping помечает Version как concurrency token;
- update/delete/publish/archive/restore/decision/activation и другие конкурентные mutation используют expected Version владельца;
- mismatch не перезаписывает данные и возвращает concurrency conflict;
- last-write-wins не используется.
Технический механизм — optimistic concurrency; pessimistic row locks не являются общим бизнесовым способом редактирования. Локальные lock/transaction детали допустимы только внутри реализации атомарной операции и не заменяют expected-version contract.
### Уровень token
Version принадлежит владельцу согласованного mutable invariant/aggregate, а не каждому вложенному JSON field. Изменение child collection увеличивает Version aggregate owner.
Immutable Published/Release/Revision snapshots после создания не требуют дальнейшего edit concurrency; конкурентно защищается операция их создания и mutable state/pointer вокруг них.
### Разные типы versioning
Не смешивать:
- `Version` — внутренний optimistic-concurrency token CM;
- `SourceVersion` — source entity version VR School в CatalogAccess/licensing projection;
- `VersionNumber` — опубликованный business number Schema/Instance version;
- `Sequence` — порядок activation history.
Для source projections могут существовать одновременно `SourceVersion` и внутренний `Version`: SourceVersion решает freshness source snapshot, Version защищает конкурентное применение/изменение локальной projection state.
### Current-state migration
Текущие ContentModeling `Revision/ExpectedRevision` выполняют похожую задачу, но target унифицирует внутреннее поле на `Version`, а HTTP contract — на `ETag`/`If-Match`. Так как production-данных нет, допускается destructive rename/refactor без compatibility с текущим API/storage.
### Draft baselines
Schema Draft хранит `BasePublishedVersionId`; Instance Draft — `BasePublishedInstanceVersionId`. Это business stale-base metadata и не заменяет optimistic Version.
Publish обязан пройти обе независимые проверки:
1. expected mutable Version совпадает;
2. published baseline остаётся актуальной.


## Решения блока 24: retention и lifecycle storage


Physical storage обязан сохранять все records/blobs, необходимые Published Release, PublicationRevision и frozen entitlement.


Hard-delete допускается только для согласованных pre-history объектов: Draft SchemaVersion/InstanceVersion, logical Schema/Instance без Published history и unreferenced Demo Asset.


Archive/Retire/Withdrawal реализуются lifecycle metadata/state, а не физическим delete historical rows.


Parent Archive не должен массово обновлять child lifecycle columns: effective unavailability вычисляется через owner relation/read model.


Asset bytes, referenced historical publication, не удаляются из object storage. MVP automatic garbage collection отсутствует.


Проект не запущен, поэтому текущие delete/archive columns/constraints можно перестроить destructive под эту lifecycle matrix.


## Demo authoring: принятые persistence и coordination guarantees

[Q19 canonical v1](Q19-content-hashes.md#принятый-canonical-profile-v1) requires immutable original bytes/profile/digest as one own publish/materialization commit. F03/F04/F10 owns stored profile metadata; Delivery/SDK preserves it, not JSONB reserialization or default current-profile hash. Pure shared canonicalization library takes typed normalized inputs; CPU preparation outside guard retains exact basis and must fresh-check owner/media/source evidence before own commit. Corruption/unknown profile fails without autorepair/rehash; byte-changing version adds profile, preserves old historical triples. Group target verifies complete membership/removals before client atomic installed+inventory+receipt commit. Literal samples are independent byte/hash documentation checks, not SQL/SDK/Unity proof; physical storage/codec/DI still engineering.

F01/F02/F04 create/clean-create/copy записывают operation ID/input identity и durable original result в owner BC вместе с созданными roots. Same ID/input возвращает original result, different input конфликтует; unique operation scope защищает concurrent retry. Exact tables/types/input comparison/index names и UoW integration остаются инженерной материализацией Q15/Q20. Upload хранит ту же attempt identity между короткими durable stages MediaLibrary по Q21.

Saved schema/content/root ETag и diagnostics basis — разные identities. Persisted orphan ID stable; computed incompatibility token deterministic по stable FieldId/ItemKey location и exact schema/content basis. Save атомарно проверяет basis/discard и фиксирует full desired tree + retained orphans + own Version. GET не записывает incompatibility. ItemKey client-generated при add List<Object>, unique per List occurrence, сохраняется Copy/Reorder; scalar List keys не получает.

Historical field identity включает parent chain; reintroduced FieldId требует прежних Key/Type/meaning/context и monotonic исторических flags. Move к другому parent — новый ID. Для проверки нельзя читать только latest Published; exact historical identity index/projection/JSONB mapping выбираются при реализации без новой field entity ради одного индекса.

Different-target Instance Publish с valid relevant baselines коротко сериализует global sequence allocation и commit owning instance. Оба Publish успешны consecutive numbers; counter-only race не выдаётся за stale base. Same-target loser остаётся stale409; own Draft race412. Concrete PostgreSQL isolation/guard API/order определяется технически; transaction не удерживается через UI.

Delete consent bound exact observed impact identities/versions/basis. Changed exact set отклоняет delete и требует нового preview/confirmation; count не authority. Schema cascade confirmation min5 s проходит вне transaction. Последний Instance Draft можно удалить, оставив пустой logical parent.

Media Demo deployment — один CM process. Все paths создания/сохранения references и MediaLibrary Delete используют **один shared application-level AssetId guard** от чтения eligibility/authority до commit owning BC UoW. Distinct keys берутся в stable order. Local DB transactions не становятся cross-BC transaction; full Publishing не mandatory Demo. Durable Deleting, committed после no-refs check под guard, блокирует новых writers после освобождения guard и сохраняется при storage retries. Multi-replica deployment требует durable coordination; этот вариант не выбран для Demo. Exact singleton/port DI/UoW release hooks и PostgreSQL/MinIO race tests ещё материализуются.

Ни одна из этих guarantees не объявляет текущие source mappings/handlers готовыми; migration/schema rebuild и application tests в этой редакции не выполнялись.

## F07: initialization и конкурентные guards


Связано с [backend F07](../features/F07-product-content-assignment.md), [frontend F07](../../frontend/features/F07-product-content-assignment.md), [Q15](Q15-api-and-source-code.md#f07-read-write-и-reference-контракт), [Q07](../../../business/questions/Q07-exclusions-and-local-content.md#f07-допустимость-рабочих-references), [F04](../features/F04-entity-instances.md), [F06](../features/F06-product-catalog.md), [F15](../features/F15-catalog-conflicts.md), [F09](../features/F09-content-releases.md).


### Исходник и целевой concurrency owner


Current [ProductContentAssignment](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Domain/Assignments/ProductContentAssignment.cs) имеет server-generated surrogate Id, ProductId, коллекцию и Assign/Remove по Keys, без собственного Version. [ProductContentAssignmentConfiguration](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.EntityFramework/Configurations/ProductContentAssignmentConfiguration.cs) unique ProductId, child mappings — [PublishingOwnedMappings](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.EntityFramework/PublishingOwnedMappings.cs) с Key-based uniqueness/row_id. Current [ProductPublicationState](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Domain/PublicationStates/ProductPublicationState.cs)/[его mapping](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.EntityFramework/Configurations/ProductPublicationStateConfiguration.cs) владеет Revision: long. Save assignment изменяет этот общий token через App PublishingService.


Target: семантически один ProductContentAssignment по ProductId, собственный int Version, whole-set stable EntityInstanceId membership. Active-release/policy changes не являются assignment edits. SourceVersion каталога и content business VersionNumber — другие сущности. Surrogate physical ID допустим только при сохранении одного semantic root/resource; legacy schema не диктует новый HTTP resource.


Current [PublishingService](../../../../contentmanager.api/src/App/ContentManager.Application/Publishing/PublishingService.cs) создаёт absent assignment при Save, а workspace GET для absent assignment возвращает empty items и fallback state Revision=0. Это не подтверждает durable internal setup и корректный empty owner ETag. `ResolveItem` напрямую меняет ContentModeling revisions, используя общий UoW: target public-port/local Publishing boundary требует отдельной coordination design. [PublishingArchiveConcurrencyTests](../../../../contentmanager.api/tests/App/ContentManager.App.Tests.Integration/PublishingArchiveConcurrencyTests.cs) использует real PostgreSQL/barrier для current archive race; его наличие не доказывает иной выбранный target protocol.


### Что требуется уточнить


| Область | Требуемый invariant | Открытая реализация |
| --- | --- | --- |
| Internal setup | Пустой assignment для existing Product, повтор не сбрасывает filled set; no creation-on-GET | Durable event/hook/work mechanism после CatalogAccess commit, registration и retry/ack/recovery; никакого несуществующего обязательного Product event из старого текста; first apply vs newer source snapshots |
| Setup readiness | Read token соответствует real persisted owner; no fake empty DTO/ETag | Response missing assignment при present Product/задержке setup, machine code/retry representation; отсутствие source Product отдельный404; root creation race unique ProductId classified fresh read |
| Physical model | Own Version guards все child edits, one version per stable EntityInstanceId | ProductId PK vs retained physical surrogate, unique membership/check/index names, collection delete/insert tracking order, initial Version/overflow/UpdatedAt; no-op PUT policy |
| Cross-BC guards | New-position/version replacement requires current eligibility; unchanged exact unavailable may remain Save; limited inactive maintenance, no F15 exemption | MVP gate выше фиксирует owner/order/recovery; exact retained/new-use/effect evidence adapter; no foreign AdvanceRevision/shared SQL transaction. Read failure not empty valid preview |
| Concurrent writer surface | F07 Save, F04 Archive/Retire/Withdrawal/delete, F06 move/lifecycle, F15 Open/Resolve согласованы | Где сохраняются dependency facts, как снять only working ref без потери historical F09/F10 dependency, race double add/remove/Resolve/source update; effect media availability через F04/F05 profile |
| Error classification | Own expected mismatch412, lifecycle/composition/catalog409, transient dependency не false safe | Fresh post-disposal reader и failed conditional root classification, precedence when both changed, boundary retry без user overwrite; whole-set rollback до commit |
| F09 snapshot | Release captures ровно один protected assignment set/version; Save не меняет history | Local Publishing assignment read/source If-Match/CreateRelease interleaving, guard при read before Save, own token result policy без false stale unrelated activation |
| Query/result | Set/token coherent, PUT returns committed saved work separately release readiness/three diagnostic classes and derived basis | Own read isolation/materialization, external evidence capture/addresses/code/nullability without hidden GetWorkspace; unknown commit recovery Q15 |
| Migration / DI | CM без production legacy obligations; один accepted command/query path | Scoped F07 changes без переписывания общего F08/F09 mappings/immutable rows; Publishing.Persistence project/query placement, existing module references/service registration migration и реальные schema tests |


**Ответ:** Уточняется. Эти вопросы нужны для проверяемой реализации собственных ETag и публичных границ. Они не добавляют шину, initialization или полный Publishing в предпосылки Demo; целевая F07 остаётся MVP.


## F08: patch concurrency и preview consistency


Связано с [backend F08](../features/F08-variant-content-assignment.md), [frontend F08](../../frontend/features/F08-variant-content-assignment.md), [Q15](Q15-api-and-source-code.md#f08-patch-preview-и-parent-route), [Q07](../../../business/questions/Q07-exclusions-and-local-content.md#f08-сохранение-patch-и-проверка-live-базы), [F07](../features/F07-product-content-assignment.md), [F06](../features/F06-product-catalog.md), [F15](../features/F15-catalog-conflicts.md), [F09](../features/F09-content-releases.md).


### Current-state и обязательные границы


Текущий [Variant assignment mapping](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.EntityFramework/Configurations/ProductVariantContentAssignmentConfiguration.cs) содержит surrogate assignment ID и unique VariantId, две owned groups. [PublishingOwnedMappings](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.EntityFramework/PublishingOwnedMappings.cs) использует row_id/Keys/hash/order и Key-based indexes, без four-group explicit target. [ProductVariantPublicationState](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Domain/PublicationStates/ProductVariantPublicationState.cs) владеет long Revision/LastManualRevision; Save App PublishingService touches этот state. Это не own int Version target patch.


Target: один patch по stable VariantId, собственный int Version, four groups atomic mutation. Current applied parent — внешнее CatalogAccess fact, не duplicated mutable ProductId в root. F07 base отдельный mutable root. При safe move local decisions сохраняются; несовместимый move сохраняет старый applied context и newest pending source target F15. Ordinary F07 base change пересчитывает preview, не открывает автоматически F15 и не повышает own patch Version.


### Требуемая техническая проработка


| Область | Обязательное свойство | Что уточняется |
| --- | --- | --- |
| Root/membership | Four groups сохраняются целиком, own Version guards any child edit включая empty patch | Physical tables/owned layout, surrogate vs semantic Variant key, target SchemaId column, unique targets/results/rules, cross-table result uniqueness, delete/insert order/constraints, initial Version/overflow/UpdatedAt/no-op |
| Composition core | Original inherited layer targets/suppression, append results once/order-independent; A→B/B→C notcascade, A→B/B→A allowed if unique/otherguards | Shared pure core signatures/distinct inputs/profile; intrinsic duplicate errors vs base collision diagnostics, no LWW/automatic cycle rejection |
| Multi-input read | Patch set/token/base refs/applied parent captured coherently | Query isolation/version revalidation/captured input context, read while F07 Save/F06 move/F15 resolution; no mixed base saves/current parent; strongest freshness guarantee и diagnostics |
| Own ETag vs preview | Patch unchanged при base change; own token не доказательство неизменного derived body | Representation/header scope вместе Q15; private captured versions vs public opaque freshness/stamp/transport metadata; no exposed technical root Version ради preview |
| Whole Save | Own stale412 distinct externalguards; validpatch/live-base-only collision allowedSave with ReleaseBlocker; intrinsic errors block; NEWtarget current inherited only | MVP guard/fresh facts выбран; captured-base evidence/admission/consent/error priority и retained-evidence carrier; no mandatory base If-Match invented. Inactive maintenance by content effect/removal-eligible replacement incl rules, not rawrulecount; F15block remains |
| Cross-BC protocol | Parent/F15/new-use facts не stale permission для записи; no foreign DbContext transaction | MVP gate/owner-only UoW/independent public reads выбран; exact reference/catalog evidence adapters, writers F04/F05/F06/F15/F07/F08, post-disposal own race classification и dependency rollback |
| Setup/readiness | One empty root per stable Variant, retries не reset filled patch | Durable trigger/ack/recovery, initial Variant missing parent и missing F07 base, read/write mapping пока roots не ready; GET не synthesize own ETag и не creates source Product |
| Move/F09 dependencies | Patch survives move, F09 snapshot owns exact protected patch, history immutable | Atomic capture source patch/If-Match при F09, impact compatibility evidence/public port для F15; source newer target vs patch edits/Resolve; no rewrite old releases/revisions |
| Migration/DI | Scope F08 не collateral rewrite общего F07/F09 storage | Four groups вместо legacy Items/Exclusions, policy dev transition без guess override target по Keys; Publishing.Persistence/query adapters и единый command path, real PostgreSQL migration/roundtrip |


**Ответ:** Уточняется. Ни explicit override intent, ни whole-root concurrency нельзя заменить автоматическим Key-match/общей publication-state Revision. Приоритет требований остаётся source-traceable target MVP, без зависимости Demo от full Publishing.


## F09: snapshot creation и lifecycle races


Связано с [backend F09](../features/F09-content-releases.md), [frontend F09](../../frontend/features/F09-content-releases.md), [Q15](Q15-api-and-source-code.md#f09-release-contracts-и-semantic-review), [Q05](../../../business/questions/Q05-initial-publication.md#f09-пустой-snapshot-и-повторное-создание), [Q07](../../../business/questions/Q07-exclusions-and-local-content.md#f09-live-collision-и-release-validation), [F07](../features/F07-product-content-assignment.md), [F08](../features/F08-variant-content-assignment.md), [F10](../features/F10-publication-revisions.md), [F11](../features/F11-publication-lifecycle.md).


Current [Product release mapping](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.EntityFramework/Configurations/ProductContentReleaseConfiguration.cs) / [Variant mapping](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.EntityFramework/Configurations/ProductVariantContentReleaseConfiguration.cs) stores unique owner+ReleaseNumber, Status/hash/PublishedAt; shared [PublishingOwnedMappings](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.EntityFramework/PublishingOwnedMappings.cs) stores legacy Items/Exclusions/Keys. No own lifecycle Version/comment/audit/provenance/four-group override evidence. Current App create touches publication Revision, writes active base/foreign reference roots; target protected source snapshot не должен наследовать эти side effects.


| Область | Требуемое свойство | Открытая реализация |
| --- | --- | --- |
| Source capture | One saved source generation, exact Version+all children; required source If-Match protected through insert commit | Read Committed + global MVP gate + fresh independent reads приняты; exact adapter binding, concurrent whole-set Save/empty child removal/no-op; no source Version bump simply for Create и no optimistic check on unrelated new row as substitute |
| Repeated Create | Intentional identical-content/comment artifact разрешён; mandatory ClientOperationId replay original receipt before new preconditions | Exact normalized/result codec, unique scope/storage/inquiry/retention; no hash dedup/source bump; policy закрыта |
| Snapshot/immutability | Independent deep Product refs/Variant four groups, fixed owner/comment/creation/provenance, lifecycle-only edits | Owned layout/cross-group uniqueness/required constraints, no accidental shared tracked navigation or updates via lifecycle repository; chosen persistence safeguard/real SQL verification, no automatic historical delete |
| Lifecycle concurrency | Own release Version protects metadata; exact DTO/token belongs own commit | Initial/overflow/no-op/repeat/audit Restore conventions; stale classification fresh correct root after disposal; no later read substitutes concurrent lifecycle result |
| Active Product Retire | Can't commit Retired while F11 current pointer selects it | Global MVP gate защищает Retire/Select/Clear; deterministic owner row mapping/lock keys; raw current pointer read isn't race protection and implicit Clear forbidden |
| Variant Retire/F10 | New revision uses Available protected source; Retire doesn't break running atomic use/history | Multi-release source capture/retention/use protocol and races with F10 create, policy after committed revision; no active VariantRelease pointer invented |
| Cross-BC references | New-use/schema/media/catalog/F15 facts stable enough at Create; historical dependencies retained | Общий MVP gate для всех lifecycle/move/Resolve/reference writers; exact public fact adapters, no foreign DbContext token writes or claimed shared SQL transaction |
| Provenance/review/context | VariantCreate explicit exact immutable ProductRelease or EmptyBase; selectedcontext collision blocksrelease; source token != review/parent freshness | Capture same reviewed patch/context/trusted parent/lifecycle through commit; precise review-basis protocol Q15, no currentactive/live/fallbackempty. Contextstoredprovenance notharddependency, F10otherbase fullyvalidated; history move/display rules |
| Mapping/migration/DI | Scoped release storage and one command/query path; preserve adjacent F07/F08/F10 histories | Publishing.Persistence adapters/projects, transition legacy Draft/Published/Number/hash, dev-data migration policy no target inference by Keys; real PostgreSQL roundtrip/atomicity/metadata-only update checks |


**Ответ:** Уточняется. Immutable child content не означает отсутствие lifecycle concurrency; protected source Create не означает изменение source assignment. Local Publishing commit не доказывает cross-BC race safety или idempotent request.


## F10: composition capture и withdrawal


Связано с [backend F10](../features/F10-publication-revisions.md), [frontend F10](../../frontend/features/F10-publication-revisions.md), [Q15](Q15-api-and-source-code.md#f10-revision-contracts-и-preview), [Q19](Q19-content-hashes.md#f10-materialized-representation-и-hash), [Q17](Q17-publication-availability.md#f10-withdrawal-и-historical-distribution), [Q09](../../../business/questions/Q09-editing-and-deletion.md#f10-зависимости-исключённого-слоя), [F09](../features/F09-content-releases.md), [F11](../features/F11-publication-lifecycle.md), [F13](../features/F13-content-delivery.md).


Current [PublicationRevisionConfiguration](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.EntityFramework/Configurations/PublicationRevisionConfiguration.cs) stores ItemsJson JSONB/ManifestJson text/ManifestHash and ignores computed Items; no own lifecycle Version/normalized target Items/comment/Available-Retired-Withdrawn. [App RevokeAsync](../../../../contentmanager.api/src/App/ContentManager.Application/Publishing/PublishingService.Releases.cs) touches shared state Revision and directly mutates Delivery composition before GetWorkspace. Target requires Publishing-owned atomic lifecycle/current-state/history/outbox commit and separate Delivery consumer; current source code doesn't prove it.


| Область | Обязательное свойство | Открытая реализация |
| --- | --- | --- |
| Selected sources/current facts | Exact F09 immutable content with mutable Available permission; no input source/workspace If-Match | Multi-release protected use/capture against Retire, applied parent/F15/move/Resolve/reference lifecycle/media guards, common Preview/Create evidence model; no stale read or foreign tracked root token writes |
| Composer/materialization | Pure stable-ID composer, final unique/nonempty; effective runtime closure отдельно source evidence/retention | Shared core signatures/closure representation/canonical profile Q19 and child mapping; suppressed dependency policy закрыта Q09 |
| Revision root/children | Immutable sources/content/comment/created/context/hash; own lifecycle int Version | Normalized Items/metadata JSONB/lossless canonical bytes/profile mapping/indexes/constraints, immutable persistence safeguard, historical read missing source metadata handling; no guessed Key→target transition |
| Create transaction/result | Complete graph/retention facts atomic, no source/assignment/F11 token mutation | Capture/commit linearization/new-use guards; own new root Version initial/overflow/result mapper; identical Create/request dedup/recovery Q05/Q15, not unique hash by default |
| Retire/Restore vs F11 | Active can't retire, can't activate committed Retired/Withdrawn; Restore no approval/activation | MVP gate → deterministic owner locks выбран; concrete conditional writes/fresh classification, internal state race vs own412; repeated/no-op/audit conventions |
| Active Withdrawal | Revision terminal + correct current pointer→null + monotonic Sequence/history/outbox local atomic | MVP owner-only multi-root transaction под gate выбрана; concurrent R1→R2 doesn't clear unrelated R2, no required client state header; block/new-use/lifecycle maintenance access scope Q15 |
| Inactive/frozen Withdrawal | Exact-ID distribution ban independent of pointer transition | Dedicated restriction fact/read guard contract/payload/identity/order and storage/outbox for every new Withdrawal; null event alone insufficient |
| Delivery propagation | Durable ban + optional F11 Sequence-null, consumers commit separately; terminal not undone | Transport/routes/topic names/inbox/dedup/ack/retry/crash recovery, ordering restriction vs projection/read/activation, terminal ban dominates old payload replay; Q17 effective/in-flight/session/chunk/refusal guarantee |
| Moderation/read contracts | F17/Delivery receive saved exact materialized data + current availability, no foreign DB | Manual create/submission boundary Q28/F17; restriction invalidates activation/approval use without changing immutable iteration history; admin exact vs distribution service boundaries |
| Migration/DI | One accepted command/query path, scoped preserved history | Legacy ItemsJson/hash/Revoke transition and dev-data policy without inferred explicit override target; Publishing.Persistence projects/public adapters/outbox DI, real PostgreSQL and real Delivery contour checks |


**Ответ:** Уточняется. Revision own ETag protects its lifecycle, not current pointer/source permissions/Delivery readiness. Outbox insert/dispatch ACK/consumer commit/server refusal разные подтверждения. Принятая async модель не объявляется existing implementation или immediate ban in all in-flight responses.


## F11: state transitions и promotion coordination


Связано с [backend F11](../features/F11-publication-lifecycle.md), [frontend F11](../../frontend/features/F11-publication-lifecycle.md), [Q15](Q15-api-and-source-code.md#f11-publication-state-и-automatic-promotion), [Q17](Q17-publication-availability.md#f11-publishing-active-и-delivery-effective), [F09](../features/F09-content-releases.md), [F10](../features/F10-publication-revisions.md), [F17](../../../business/features/F17-content-review.md), [Q28](../../../business/questions/Q28-content-review-scope.md).


Current [states](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Domain/PublicationStates/ProductVariantPublicationState.cs) use Revision and LastManualRevision, [ProductPromotion](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Domain/Publications/PublicationHistory.cs) uses Pending/Activated/Conflict/Failed/Superseded, [ProcessPromotionAsync](../../../../contentmanager.api/src/App/ContentManager.Application/Publishing/PublishingService.Releases.cs) requires prior active revision, uses its VariantReleaseId, bypasses moderation and writes Delivery directly. [Worker](../../../../contentmanager.api/src/App/ContentManager.Application/Publishing/PublicationPromotionWorker.cs) polls Pending 30sec/100. These are current source facts, not accepted target concurrency/dedup/SLA.


| Область | Обязательное свойство | Открытая реализация |
| --- | --- | --- |
| State roots/history | Own int Version separate LastSequence, actual transition+append+outbox atomic, no-op no effects | Initial root creation/unique race/tag, Version/Sequence initialization/overflow, named constraints/immutable safeguards, no-op intent/precondition priority |
| Lifecycle/current guards | Can't Select retired Product release or Activate retired/withdrawn revision; F10 clears only matching withdrawn ID | Read Committed/MVP gate/fresh protected reads vs Retire/Withdraw выбраны; exact mappings/adapters, concurrent R1→R2 classification, approval/context/ban protected public facts |
| Base/membership | Start captures exact base/state Version/applied membership; archived/conflicted members Blocked; zero members completed | Mapping/capture adapters, initial owner readiness, request/result metadata; no retroactive membership |
| Local source capture | Latest-created local ReleaseNumber; Retired latest Blocked/no fallback; never-release plus any working group Blocked else BaseOnly; frozen exact IDs | Full four-group work projection/source mode mapping; no latest adoption on technical retry |
| Operation/items | Immutable operation inputs/membership; separate item own Version; internal per-Variant intent head; coherent derived summary | Conditional stage/item mapping, summary read isolation, stage indexes/attempt/backoff/lease numeric settings; no shared progress token |
| F10 stage/result | CreateRevision + autoSubmit + exact tuple + AwaitingModeration one Publishing preparation commit | Same-BC UoW wiring/unique item-result safeguards/outbox/CommandExecution mapping; no orphan successful half-stage or hash dedup |
| F17 linkage | Exact own iteration only/no adoption; Withdraw cancels bound item, new manual iteration no reattach; Cancel/Supersede no automatic moderation Withdraw | Concrete typed envelope/inbox/actor/correlation and stage persistence adapters; linkage policy закрыта |
| Late decision/overlap | Current head + Product state Version + Variant Sequence + exact Approved tuple/current guards; real manual transitions supersede, no-op preserves; exact Cancel | Head/generation/conditional mapping, Cancel route/own item token and diagnosis; ABA/precedence policy закрыта |
| Approved commit | Durable exact decision/continuation then atomic item Activated + pointer/history/Sequence/outbox; duplicate terminal outcome never reactivates | Inbox/outbox enlistment/envelope/conditional stage/result recovery; no inference of success from current pointer alone |
| Delivery ordering | Full projection before effective, monotonic Variant Sequence, terminal exact-ID ban dominates replay | Versioned contract package/topology/payload vs public artifact reader, inbox/outbox, null sequencing, ban ordering/availability, readiness acknowledgment/read protocol Q17 |
| Migration/DI | Publishing ownership/query Persistence/one command path, preserve target history | Legacy shared counter/job uniqueness/IsRevoked transition and dev-data policy, projects/adapters/configuration registrations, real PostgreSQL constraint/crash tests |


**Ответ:** inputs/membership/latest-source/intent/ABA/prepare/continuation/replay policies закрыты [workflow](#publishing-workflow-capture-и-продолжение). Открыты physical mapping/public evidence/inbox/result carrier и numeric worker settings. Human wait/activation отдельны; own preparation atomicity не объединяет human decision и activation. Current LastManualRevision не заменяет chosen head + captured Product Version/Variant Sequence.


## F12: policy binding и effective history

Связано с [backend F12](../features/F12-content-entitlement-policy.md), [F13](../features/F13-content-delivery.md), [Q15 integration boundary](Q15-api-and-source-code.md#f12-policyagreement-integration-boundary), [Q23 workflow](Q23-source-synchronization.md#f12-planned-agreement-confirmed-workflow), [Q17 effective history](Q17-publication-availability.md#f12-freeze-cutoff-и-effective-time). PolicyAgreement/binding и confirmed-visibility/catch-up semantics приняты; exact adapters/mappings/codecs ещё materialize, existing session guarantees остаются отдельным Q16.

Current [ContentPolicyVersion](../../../../contentmanager.api/src/BoundedContexts/Publishing/ContentManager.Publishing.Domain/Publications/PublicationHistory.cs) хранит configuration history. [EntitlementPolicyResolver](../../../../contentmanager.api/src/App/ContentManager.Application/Publishing/EntitlementPolicyResolver.cs) разрешает as-of caller timestamp, а [CatalogSnapshotProcessor](../../../../contentmanager.api/src/App/ContentManager.Application/Integration/CatalogSnapshotProcessor.cs) записывает policy в CatalogAccess period. Это наблюдаемый legacy механизм; target не угадывает promise по этому времени и не захватывает current policy на first use.

## F12: PolicyAgreement и подтверждение периода

### Принятое обещание и scope

Publishing владеет immutable `PolicyAgreement`. Обещание фиксируется **commit Agreement в CM**, до подтверждения коммерческого CM-content периода в VR School. Freeze Agreement в t1, current policy Deny в t2 и confirmation исходного plan в t3 сохраняют Freeze. Это резервирование политики, а не доказательство её актуальности на момент source confirmation.

Agreement связан с exact LicenseId, ProductVariantId, SourcePeriodId, исходными planned parameters и captured applied parent/policy source/version. Системный default сохраняется как Freeze с явным provenance без фиктивной PublishedPolicyVersionId. После commit Agreement смена current policy/parent не перепривязывает promise. Новое согласование требует ready authoritative applied context под [MVP guard](#mvp-протокол-consistency-guard); pending/mismatch/ambiguous F15 не допускают arbitrary parent.

MVP не вводит автоматическое expiry/age cleanup Agreement. Неиспользованный Agreement не даёт прав. Изменение plan до confirmation требует нового SourcePeriodId и Agreement. Renewal также создаёт новый период и Agreement; перенос старого Agreement на другую license/Variant/renewal запрещён.

### Владельцы и атомарность

| Boundary | Собственные durable facts и commit |
| --- | --- |
| Publishing | Agreement, stable RequestId replay и normalized original input, frozen policy/provenance; own guarded transaction; CM License projection не prerequisite quote |
| VR School | Durable planned period → external authenticated CM call вне source transaction → own conditional confirmation того же plan с AgreementId и outbox |
| CatalogAccess | Versioned authoritative confirmed period facts, immutable confirmed AgreementId, explicit CurrentPeriodId per license/Variant и completeness; newer contradictory Agreement rejected/quarantined без replacement |
| Delivery | Immutable binding уникальна по `(LicenseId, ProductVariantId, SourcePeriodId)`, собственный GrantPeriodId binding, exact AgreementId и frozen policy/provenance; нет чужих EF writes/FK/общей transaction |

Lost quote response повторяет RequestId и исходный input. После auth поиск confirmed request и comparison исходного input выполняются **до** fresh new-quote context/policy checks; возвращается исходный Agreement. Иной input той же request identity конфликтует, а не перезаписывает promise. Failed source commit не делает quote entitlement; source recovery проверяет прежний plan/confirmation outcome. Delivery first insert проверяет confirmed exact scope и Agreement через owner public ports; unique winner/read recovery не выбирает current policy заново.

### Mutable коммерческие facts и выбор

GrantPeriodId binding создаёт Delivery один раз; race/replay возвращает сохранённый winner ID. Это immutable identity binding, не второй mutable коммерческий period aggregate. ID legacy CatalogAccess projection не переносится в это поле; authoritative source tuple остаётся LicenseId/VariantId/SourcePeriodId.

Confirmed StartsAt, UpdatesUntil, explicit unlimited, closure/revoke и source revision отделены от immutable policy binding. Genuine correction одного confirmed периода явно классифицируется source с reason/history, сохраняет Agreement и пересчитывает Freeze по corrected cutoff; fixed FrozenRevision в binding не кэшируется. Покупка дополнительной длительности/unlimited upgrade является Renewal, а не correction. CM не выводит operation kind из разницы дат.

New admission использует explicit applicable CurrentPeriodId. После вступившего renewal старый период той же license не fallback: expired новый Deny не открывает прежний Freeze. Это правило выбора нового права; renew/TTL/revoke и independently expiring leases уже pinned session приняты в [Q16](Q16-activation-and-sessions.md#f13-session-credential-и-revocation).

Complete authoritative no-grant даёт Denied; missing Agreement/period/projection/completeness — Indeterminate/DataNotReady, confirmed revoke — отдельный Denied reason. Ordinary update period end не revoke. UpdatesUntil не application ExpiresAt; unlimited только explicit IsUnlimitedLicense, не absent date/IsUnlimitedUsers. Для полного caller verified multi-license set любой необходимый unknown блокирует весь выбор как Indeterminate. Known Denied вместе с Allowed допускают newest allowed whole revision; explicit single-license запрос допустим, implicit organization expansion запрещён.

### Проверка и оставшаяся инженерная детализация

Обязательны реальные PostgreSQL race/replay/fault tests Agreement и binding, source planned/conditional-confirm/outbox integration и сценарии Freeze→Deny→delayed confirmation, lost response, failed source commit, changed plan, Agreement substitution, renewal/correction, completeness, shared SourcePeriod across Variants и unknown multi-license member. Нормативные модели/поля/DF/layers определены в F12; Q не заменяет их.

Открыты exact versioned integration wire/auth/URI, normalized plan representation и comparison fixtures, named indexes/mappings/DI/inbox recovery, policy-state initial tag/no-op/clear semantics и physical adapters/wire для выбранных effective-history/admission/session протоколов Q16/Q17. Выбор checkpoint, owners, immutable promise, unique binding scope, renewal/correction и strict completeness уже закрыт. Ценных production/staging данных нет: legacy reset до первого запуска допустим; неизвестные promises не реконструируются из today policy.

## F13: projection session и plan storage


Связано с [backend F13](../features/F13-content-delivery.md), [frontend F13](../../frontend/features/F13-content-delivery.md), [Q15](Q15-api-and-source-code.md#f13-runtime-contracts-и-source-evidence), [Q33](Q33-sync-resume-transport.md#f13-wire-protocol-и-execution-ownership), [Q16](Q16-activation-and-sessions.md#f13-session-credential-и-revocation), [Q17](Q17-publication-availability.md), [Q19](Q19-content-hashes.md), [F12](../features/F12-content-entitlement-policy.md).


Current [mapping](../../../../contentmanager.api/src/BoundedContexts/Delivery/ContentManager.Delivery.EntityFramework/PublishedCompositionConfiguration.cs) persists manifest-only composition and pinned session with no TTL/credential/status/full-item/effective-history model. [App runtime service](../../../../contentmanager.api/src/App/ContentManager.Application/Delivery/InternalDeliveryService.cs) traverses foreign contexts; current Publishing activation writes Delivery directly. Target is own Delivery atomic projection and public readers, Demo local cache separate from MVP security state.


| Area | Required property | Open mechanism |
| --- | --- | --- |
| Demo ownership | One immutable plan per SDK ID, one active execution; status/read not start | Atomic reservation before capture, original request fingerprint/result binding, owner/lease/fencing/stop/discard/late receipt/cache-expiry consistency; Q33 |
| Demo storage/DI | Local CM cache default1h configurable; persistent source content no guarantee plan restart survival | Memory vs local durable form/eviction/TTL origin/sliding/inflight limits; Demo adapters/Host don't require license/security/broker or hidden Redis |
| Exact source capture | Valid full desired set/diff, group errors distinct from empty, exact planned payload pinned | Public ContentModeling coherent capture/version/order/withdrawal guard, exact schema/key/hash/control/data adapter and retention/new-use vs historical read permissions |
| Snapshot integrity | Complete copied F10 result/content/schema/assets/canonical representation before effective | Storage normalized graph/lossless bytes/manifest-hash mapping Q19, source lifecycle/ban guard against late materialization, atomic pointer/history/inbox commit |
| Ordered catch-up | Strict source range recovery, separate processed Sequence/effective history/confirmation; one effective transition per commit | [Selected protocol](#delivery-catch-up-и-подтверждение-видимости); exact public range carrier, canonical payload comparison, indexes/DI/clock-health adapter remain engineering |
| Banned historical transition | Known terminal intermediate consumed NotEffective without fake availability, then ordered continuation; technical failure cannot skip | Outcome/inbox layout and diagnostics adapter materialize F13; terminal semantic choice closed, no automatic older fallback |
| Restriction before snapshot | Inactive/frozen/unmaterialized exact ID ban retained forever per lifecycle | Independent restriction identity/store, snapshot status/session invalidation atomicity, incoming order and session/read fences; no null-pointer-only ban |
| Session admission | Selected Q16 scheme/verifier, immutable selected epoch/period/pin, atomic creation receipt + short encrypted capsule | [Selected replay/session protocol](#сессии-replay-и-response-leases); exact indexes/codecs/keyring/DI/retention fixtures remain engineering |
| Protected requests | Short guarded coherent checkpoint before cache304/range, independent-expiry response lease; current pinned eligibility | Q16 defaults and cancellation selected; final timer/Host/YARP/profile wiring, measured delivery/source lag Q21/Q22 |
| Revoke/inbox | Relevant credential/license/normalized grant scope, no repin/basis merge | Conditional/bulk session status update + inbox, source-version freshness/admission fence, no direct foreign aggregate tokens |
| Migration/registrations | One accepted own command/query/auth path, historical target content retained | Api/Abstractions/Persistence projects, legacy manifest-only/SyncSession/session transition/dev-data policy, real PostgreSQL crash/race/byte integrity tests; no parallel legacy bypass |


**Ответ:** Уточняется. Full materialization doesn't imply whole cross-BC SQL transaction. Outbox send/consumer commit/effective read/refusal/client receipt distinct. Demo plan progress/lease is not domain int Version/security session, and process-memory/fixed expiry old text doesn't override Q33.


## Delivery catch-up и подтверждение видимости

Нормативные модели/поля/layers/DF — [backend F13](../features/F13-content-delivery.md), business checkpoint — [Q17](Q17-publication-availability.md#подтверждённая-видимость-и-история-delivery), integration carriers — [Q15](Q15-api-and-source-code.md#delivery-visibility-contracts). Физические названия ниже уточняются в owning F13; этот Q не создаёт вторую модель.

### Раздельные owner commits

| Стадия | Собственный durable результат | Проверки / граница |
| --- | --- | --- |
| Prepare | Preliminary authoritative exact terminal check, then required immutable artifact staging outside gate; не effective result | Known terminal target bypasses artifact loading, final recheck under gate; technical failure/unknown ban cannot become terminal outcome; no foreign EF/live recomposition/S3 bytes under gate |
| Apply one effective transition | Full snapshot when non-null + pointer + effective-history pending evidence + LastProcessedPublishingSequence + processed outcome/inbox | Own Delivery ReadCommitted + global guard first; fresh order/source/terminal restriction; один actual transition на commit |
| Consume known terminal intermediate | NotEffective processing outcome + processed Sequence/inbox, без effective-history row | Только доказанный permanent exact ban; technical missing/unknown не успешное skip |
| Confirm committed visibility | Existing exact history/snapshot observation time on primary → saved confirmation evidence | **Новая** own Delivery transaction после original commit, новый guard/fresh read; timestamp observation, не second commit time; pending until save confirmed |
| Recover unknown result | Existing committed apply/confirmation winner либо fresh necessary stage | Lookup exact identity before re-execution; no timestamp overwrite/backdate or fake rollback after lost ACK |

Broker message trigger не processed-range authority. Public owner history возвращает complete ordered range; gap 12→15 сначала восстанавливает 13/14. Outcomes unique per Variant+Publishing Sequence; matching duplicate original result, different meaningful payload integrity conflict. Mutable owner int Version не заменяет source Sequence/effective order. Multiple consumers сериализуются guard и conditional own state/inbox/unique constraints; stage preparation допускается параллельно, repeated stage under gate revalidates facts.

Full apply commit сохраняет temporal evidence pending. Primary confirmation наблюдает **exact уже committed historical record/full snapshot**, даже если current pointer продвинулся; null подтверждается собственной записью без artifact. Подтверждения Variant сохраняют порядок effective history и не обходят necessary earlier pending. Trusted primary UTC observation присваивается один раз; если evidence commit ACK потерян, saved winner read возвращает тот же timestamp. Если evidence не сохранилось, fresh recovery observation даёт новое время. No automatic max/clamp при обнаруженном time-order violation; affected temporal calculation DataNotReady. Exact clock-health detection/failure/recovery и codec остаются инженерными деталями.

Freeze сравнивает saved VisibilityConfirmedAt < current confirmed UpdatesUntil без округления; late confirmation после cutoff исключает даже original commit-before-cutoff revision. Это выбранный conservative eligibility profile, не exact physical commit reconstruction. Нужная pending/incomplete history basis — Indeterminate, не silent old candidate; current committed snapshot может обслуживаться при иных достаточных rights/restrictions. Distinct complete empty/known null/pending/missing/corrupt results и stable same-candidate license provenance materialize F13/Q15. Genuine commercial correction меняет cutoff, не Agreement/history time.

Independent exact ban не зависит от наличия snapshot/current pointer; stale materialization/activation/replay не очищают его. New admission authoritative public owner ban read под выбранным admission guard; protected reads restriction before cache/304/Range/bytes, unknown fail closed. Long transfer не держит gate; independent lease deadline/renewal failure cancellation приняты Q16, physical YARP/storage wiring и measured latency остаются Q21/Q22. Retention/producer outbox/local inbox/confirmed history не объединяются в cross-BC transaction.

Acceptance: real PostgreSQL barriers around original/confirmation commits, cutoff straddle/equality/late confirmation, crash/unknown ACK saved versus missing outcome, concurrent replicas/consumers, gap/range completeness/duplicate payload, rollback/null/terminal intermediate, source outage/unknown restrictions/time order и corrected cutoff. Static documentation checks не доказывают эти application guarantees.

**Открытая инженерная детализация:** exact immutable record/outcome/evidence codecs и named keys/indexes; owner registrations/public adapters/stage claim/inbox recovery/clock health; package envelope/tie/access/status/read-carrier; timeout/limits/retention/lag settings. Sequential catch-up, owner commits, five model responsibilities, conservative timestamp, replay/null/terminal semantics уже выбраны. Demo dependencies не меняются.

## Сессии replay и response leases

Accepted [Q16](Q16-activation-and-sessions.md#принятый-протокол-сессий-и-response-leases), models once in [F13](../features/F13-content-delivery.md), pure pinned decision [F12](../features/F12-content-entitlement-policy.md), wire [Q15](Q15-api-and-source-code.md#delivery-session-credential-и-replay-contracts), source epoch/watermark [Q23](Q23-source-synchronization.md#credential-validation-и-security-epoch). Credential states/Session/verifier/receipt/capsule ownership Delivery; license/period projection CatalogAccess; policy Agreement/history Publishing. No cross-BC FK/transaction.

| Stage | Own durable / ephemeral result | Guard and recovery boundary |
| --- | --- | --- |
| Validate full source set / prepare crypto | Authenticated fresh proofs + server identity/token/verifier/Protected credential only, outside gate | CM monotonic age starts BEFORE HTTP, all latency included; keyring I/O outside SQL; no raw key/hash persistent |
| Create Session | Session selected immutable basis/epoch/pin/absolute expiry + original request/result/receipt/cipher atomic | Own ReadCommitted first global guard, fresh own/public primary source/ban/epoch/watermark, original replay lookup before new selection; no HTTP/keyring I/O under gate |
| Replay stored receipt | Original result + ephemeral Unprotect when current pinned Session allowed | Same normalized input/verified context, no current candidate/TTL extension; decoded verifier/scope match; temporary unknown same ID, proven NoReplay new admission |
| Credential security fact | Monotonic own per-credential security knowledge/inbox and scoped invalidation | Source HTTP/broker same epoch, no stale overwrite; higher Valid does not restore old admitted epoch/terminal sessions |
| Request authorization checkpoint | Ephemeral response lease with exact permission/resource/deadline basis | Explicit short own Delivery ReadCommitted + first common guard, fresh own Session + independent primary public facts; release BEFORE headers/cache304/Range/bytes |
| Transfer/renew/abort | Local lease generation/monotonic deadline/cancellation, no long SQL UoW | Independent deadline remains active while renewal waits; late completion cannot resurrect expired/cancelled response; no distributed cache assumption |

Prepared protected capsule is purpose-bound SessionId/CreationRequestId, safe metadata composed after authoritative selection; first success only after atomic owning commit. Session verifier-only storage and encrypted receipt exception explicitly separate. Replay capsule5min <= absolute session expiry; SessionTTL30min bounded finiteDeny/explicitcredentialdeadline, not app expiry; validation attempt30sec and response lease5sec server configurable/startup validated. Minimal original receipt outcome survives cipher cleanup; metadata retention/named indexes/tombstone profile remains engineering, no silent old-ID new create.

Confirmed input compares Variant + trusted stable credential/license IDs/selected caller meaning, no raw key/hash or newly derived epoch/time/current release. Replay requires fresh source context and **current pinned eligibility**, not new selection. Keyring/read outage not NoReplay. Shared persistent DP keys/discriminator/versioned purpose/explicit at-rest encryption/restricted access/key rotation across replicas: physical provider/secret/Host setup must be verified; no per-replica ephemeral fallback. Unprotect/crypto key I/O runs outside gate; checkpoint freshness prevents late disclosure becoming transferable access; decoded secret must match exact stored verifier/context.

Для replay preliminary receipt/context read и Unprotect выполняются до FINAL guarded identity/epoch/pin/attempt-age/deadline validation. Receipt/version/capsule existence/deadline reread under gate prevents stale cleanup/expiry success. Thirty-second monotonic budget includes decrypt/keyring latency; unprotected secret remains ephemeral until final allow, no raw response on old preliminary permission.

Short authorization transaction using gate is **read permission checkpoint**, not authoring commit permit. All invalidating writers/credential known-newest/source apply/period/current-policy-history hard restrictions use same key. Reconcile latest selected epoch/license-source watermark before admission; current unknown required facts fail closed. Scoped status bulk consumer does not postpone refusal until all rows marked: fresh authority/pinned evaluator checks facts independently. Restoration/new credential epoch cannot resurrect old Session; active updates don't repin old R1 to R2, commercial renewal invalidates old period context, correction/Freeze/Deny boundaries follow Q16.

Final trusted primary checkpoint time converted conservatively into local monotonic deadline, accounting for elapsed capture/return time. Lease expires <=Sessionexpiry/next finite boundary (Freeze too), independently of stalled renewal. Coordinator cancels writes/upstream on expiry/failure and fences late generation; after expiry new HTTP needed even if same Session still valid. Local known-revoke cancellation can optimize, replica correctness uses fresh primary rechecks + deadline. No physical5sec/zero-byte-recall promise; source-to-CM delivery lag and buffers measured separately. Long bytes/proxy/broker/client waits outside guard; cache304/range/resume require fresh authorization.

Accept real PostgreSQL two-scope barriers for unknown creation commit, normalized replay/race, capsule corruption/keyring rotation/outage, stale source HTTP/crypto/gate evidence, security epoch/order/watermark/inbox, corrected cutoff/renewal/Freeze-pin, scope/unselected revoke, cache/range and blocked lease renewal/late callback. Real CM/YARP/MinIO cancellation/SDK context tests needed; document verification is not those checks.

**Open engineering:** exact private/public input/value codecs/indexes/local repositories/timer/crypto/source/keyring/Host registrations, receipt/cipher/expired session retention and purge, limits/retries/clock health/metrics/SDK carriers and deployment. Protocol semantics/defaults/lifecycles, independent deadline and original replay fixed; concrete engineering missing values stay Q15/Q20/Q23, not fabricated API/code.

## Catalog V2: receive work и fault boundary

[Принятый Q23](Q23-source-synchronization.md#принятый-протокол-каталога-v2) определяет только Product/ProductVariant MVP. Все модели/поле-типы и physical-layer responsibilities один раз в [F06](../features/F06-product-catalog.md); [F15](../features/F15-catalog-conflicts.md) владеет конфликтом. License/GrantPeriod/credential epoch не мигрируются каталоговым алгоритмом.

| Boundary | Own CatalogAccess durable state / recovery |
| --- | --- |
| Receiver commit | Trusted normalized V2 candidate + latest accepted watermark + immutable event receipt + durable work together; ACK after receive commit, not after projection apply |
| Worker commit | Re-read current candidate/fault/parent/public dependency facts after own first MVP gate; whole apply/pending/conflict/history/work outcome and required F18 intent together |
| Payload handoff | One active mutable owner receive-state → unresolved Variant → Open CatalogConflict.PendingState; same-BC atomic transfer/reference update, no independently writable copies |
| Parent wakeup | Product commit + durable resolver trigger; scan recovers lost wakeup/crash/concurrent first Variant arrival |
| Worker ownership | Bounded claim/lease, reclaim/recovery scan; token/candidate-version fence before commit, stale worker cannot complete newer work |
| Ingestion fault | Trusted reliable entity+higher Version malformed/unsupported fact creates separate scoped unreadiness boundary; no valid accepted candidate or fake F15/Withdrawal |
| Compatible source correction | Exact newest supported candidate + fresh complete guards → full apply and same-episode Resolved(SourceChangedApply), own System/F18 capture; no auto-publish |

Receiver/worker/pending resolver/fault-clearance/reconciliation acceptance и F15 supersede/Resolve начинают own ReadCommitted transaction, получают общий MVP advisory key до reads/row locks и проверяют authoritative state заново. Long broker/I/O/retry waits вне gate; foreign primary public reads в independent scope без foreign UoW. Work lease не заменяет transaction-level consistency guard и не даёт разрешения на commit после потери claim/замены candidate. ACK loss/readback использует прежний EventId/work identity; не новый source mutation.

Accepted v22 запрещает v21 apply, пока v22 ещё ждёт assessment/parent. Known newer ingestion fault также запрещает stale candidate/apply и affected new writes через CatalogAccess public readiness/eligibility guards; fault Version хранится отдельно от valid latest accepted watermark. Unavailable/unknown scope не confirmed compatible. До первого Variant apply AppliedState целиком отсутствует, после успешного apply при move прежний snapshot сохраняется целиком. Missing parent без TTL, Archived parent found/lifecycle separately; same Open conflict сохраняется при новом missing-parent target. Issues имеют captured assessment basis; после supersede прежняя оценка не current evidence.

Duplicate identity scopes разные: same EventId сравнивает preserved normalized immutable event fact включая OccurredAt; new EventId/equal Version сравнивает source state (IDs/relation/Name/Description/Lifecycle/Version), не republication event clock/transport identity. Equal republish подтверждённый no-op, changed same-ID или contradictory same-version state quarantine/original unchanged. Lower watermark stale input no-op. Event receipt evidence full-fidelity/profile-versioned, не JSONB/timestamptz reserialization oracle или CM ContentHash. После configurable receipt cleanup вечное обнаружение каждого reused ancient EventId не обещается; durable accepted/applied watermark и fault boundary не очищаются вместе с receipt.

Fault recovery: ordinary candidate Version strictly greater than relevant active fault, no covering active fault (Version≥candidate) before apply. Explicit authorized unchanged UnsupportedContract replay branches after original immutable event-evidence equality but BEFORE terminal receipt return: now-supported valid normalization/current latest/fault checks, then atomic covered-fault clearance/private receipt-disposition advancement/same-version candidate+work. Immutable original evidence/EventId never rewritten; higher active fault blocks older recovery. Quarantine/retry/error dispositions не превращаются в applied success. Нельзя исправить payload под исходным EventId или считать repeat ACK достаточным для снятия fault. Historical Delivery не требует current catalog processing readiness и продолжает независимые F12/F13 checks.

CatalogSourceState/CatalogEventReceipt/CatalogWorkItem/CatalogIngestionFault и snapshot mappings/indexes/unique constraints/claim token/work scheduling/receipt cleanup materialize in own catalog_access schema; F15 partial unique Open/status-history сохраняются. Concrete tables/index names/JSONB-vs-columns/canonical codecs/SDK inbox enlistment/transport+host DI и lease/backoff/batch/retention values engineering, выбранный dual-stage/single-owner/fault/reconciliation protocol не TBD. No permanent all-source event journal/history fetch/bootstrap or Product history dependency; migration retains License/GrantPeriod state separately. Positive checked V1 long→V2 int range and never-reset source version continuity verified before cutover, no unchecked cast.

Real PostgreSQL/source/broker tests require crash-before/after both commits, unknown ACK, claim-loss/worker fencing, accepted22-vs-processing21, parent/Variant wakeup recovery, handoff rollback, fault-range blocks/current recovery, same-ID/equal-version canonical comparison after retries, retention-stale guard, source compatible close/current assessment, independent queues/replicas and reconciliation scheduled≠CM-applied/absence≠delete. Documentation checks don't execute DB migrations or prove these guarantees.

## F15: local atomicity и writer coordination


[F15](../features/F15-catalog-conflicts.md) сохраняет CatalogConflict и ProductVariant одной owning CatalogAccess worker/Resolve transaction; никакой общей SQL transaction/foreign DbContext с Publishing. Receiver раньше атомарно сохраняет candidate/watermark/receipt/work, с заменой PendingState если Open уже существует. Первый Open может быть detected позже worker: atomic payload handoff/conflict/history/work result под NEW gate/fresh accepted+fault checks. Это две durable stages, не требование receiver+worker общего commit; resolve коммитит whole applied group/cleanup/Resolved history либо откатывает всё.


**Уточняется:** physical mapping типизированных snapshots/issues/assessment basis в JSONB/owned columns, canonical equality codecs, enum/index names, partial unique Open, actor wire и SDK inbox enlistment. Dual receive/worker, single-owner handoff, scoped fault, same-episode supersede и compatible SourceChangedApply выбраны [выше](#catalog-v2-receive-work-и-fault-boundary). Current source long SourceRevision concurrency не заменяет target own int Version.


Принятый [MVP consistency guard](#mvp-протокол-consistency-guard) исключает competing affected Publishing write между impact read и apply commit, новый conflict между block-read и Publishing commit и изменение parent/source lifecycle фактов F06 при полном writer participation. Read-only tracked Product не является защитой: acquire на owner transaction connection, fresh independent public primary reads и owner commit обязательны. Timestamp, повторный SELECT либо две независимые ETag-проверки не заменяют protocol. Barrier tests используют реальные contexts/writers/PostgreSQL и проверяют отсутствие foreign enlistment. Exact port evidence/mappings/inbox implementation материализуются отдельно.


Local corrective workflow выбран в Q25: Publishing атомарно сохраняет допустимый working Variant patch repair под own patch If-Match и captured ConflictId/newest SourceVersion/repair basis. Source или patch change до commit отвергает старую попытку. Затем отдельный CatalogAccess Resolve перечитывает newest source и dependencies, коммитит apply + Resolved history либо ничего не применяет. Repair success не подтверждает move; cross-BC SQL transaction/foreign DbContext не используются. Протокол guards должен обеспечить проверку source basis до repair commit, а не ограничиться двумя prechecks.

New v22 после repair-v21 сохраняет committed repair, запрещает stale v21 apply и требует новой проверки/preview; автоматического rollback patch нет. Missing newest parent сохраняет durable pending и waiting reason. Immutable historical artifacts не blocker сами по себе и не переписываются. Affected closure выводится из конкретного mutable/in-flight dependency, а не всего Product каталога. Auto promotion/late Approval повторно проверяют этот scope/basis/current head/ProductVersion/VariantSequence перед affected commit по принятому F11/F17; Resolve/SourceChangedApply не resurrect Cancelled/Superseded и не переоткрывает precedence как неизвестное правило.

Source lifecycle нормализуется Active/Archived (Q27), parent archive влияет на derived eligibility без mutation собственного Variant lifecycle. Archive/Restore writers участвуют в общем MVP gate; historical Delivery/entitlement не invalidated автоматически. Guard выбран; exact mappings/port carriers/audit reliability остаются инженерной детализацией. В рамках аналитики migrations/DB не менялись.


## F16: Identity store и concurrency


[Backend F16](../features/F16-staff-access.md) принадлежит отдельному IdentityAccess BC. [Q24](../../frontend/questions/Q24-admin-sign-in.md#принятый-протокол-identityaccess-и-восстановления-формы) выбирает ASP.NET Core Identity, infrastructure ContentManagerUser/Role<Guid>, cookie+own staff session и RBAC/RolePermission без duplicate Domain.StaffUser/password/stamp stores. Legacy технический контур не актуальный target; SDK/runtime implementation пока не проверен.


Own provider-user access state включает stable CM identity, known assigned roles, IsActive и internal int Version. Core staff ETag покрывает own fields; provider-owned independent labels/security state требуют отдельного read/precondition profile Q15. Role membership writers должны менять own Version согласованно: provider RoleManager call сам по себе не доказывает guard root row.


Per-request authorization получает **fresh coherent committed active/roles tuple**, а не старые role claims/долгоживущий cache/старую business transaction snapshot. Q24 выбрал provider-user/role/grant/session ownership и coherent request snapshot; concrete read projection/transaction/enlistment, metadata/normalization/codecs остаются инженерной детализацией. Separate user/role calls при competing admin update не объявляются одной версией.


**Уточняется:** exact provider-user extension fields/codecs/tables/context/migrations/role IDs, atomic own user+memberships+Version/provider/session/setup metadata implementation, partial/unknown recovery/idempotency identity, duplicate login normalization и safe F18 fact reliability. Same IdentityAccess DbContext/own transaction принимает provider access changes; half-provisioning не даёт доступ. Framework UserManager call сам не доказательство owning transaction/inbox/Version integration.


Business BC используют trusted ICurrentUser/PermissionSnapshot из Authorization.Abstractions без Identity DbContext/foreign FK; прежний ICurrentStaffContext — logical responsibility, не второй independently mutable principal. Commit-time cross-BC revoke fence не вводится: already checked in-flight может завершиться, новый coherent access read после commit использует новое состояние. Server system-role grants и manual bootstrap/recovery выбраны Q24; no default password/startup user creation. Demo от них не зависит.


## IdentityAccess persistence и authorization snapshots

[Q24 accepted protocol](../../frontend/questions/Q24-admin-sign-in.md#принятый-протокол-identityaccess-и-восстановления-формы) и [Q15 wire](Q15-api-and-source-code.md#identityaccess-contracts-и-capabilities), models/layers/DF once [F16](../features/F16-staff-access.md), source [IdentityAccess architecture](../../../archive/identity-access-architecture.md). Own IdentityAccessDbContext/identity schema, Guid provider users/roles; Domain не копирует credentials/roles/stamps. Physical source tree placement адаптируется к target EF(DbContext/configurations/migrations)/EF.Persistence(repositories/query handlers).

| Boundary | Atomic own work / observation | Отдельные условия |
| --- | --- | --- |
| Provision/bootstrap | Provider account+own access/membership+durable original identity/outcome/setup state | Protected original lookup/no duplicate creation; no startup auto-user/default secret |
| Role/lifecycle/access mutation | Current own expected Version+target state+last-admin scope, provider memberships/access/Version+safe activity intent | Own transaction + first MVP guard; no cross-BC enlistment, no current role in stale cookie authority |
| Setup/reset completion | Exact purpose/user/security generation/deadline, one-use consume+provider password/stamp/account readiness+old-session invalidation | Provider SDK enlisted own DbContext/transaction; no half-ready account or reused challenge |
| Sign-in | Verified provider credential/current active/readiness + new private StaffSession/original absolute expiry | Protected cookie emitted after known durable session result; provider/network/DB failure not bad password |
| Logout/revoke | Own current-session terminal status; selected-account old sessions/stamp lifecycle on deactivation/reset | Current Session logout leaves other devices; Reactivate new admission, no resurrect |
| Permission snapshot | One coherent fresh committed session/account/roles/registered RolePermission read generation per HTTP request | Single primary projection/read scope; reuse in memory within request only, not 50x resource DB checks |
| Capabilities | Owner resource projection + immutable permission input/pure owner state checks | No tracking Identity roots in other BC, no duplicate Domain invariants/private principal |
| Form recovery | Local memory actual input/File/original context/base, no Identity persistence | No autosave/command queue; fresh exact read distinct from original baseline |

IdentityAccess writes use their **own** ReadCommitted transaction and existing MVP global transaction advisory guard first before authoritative last-admin/current-target/row-lock reads. All administrative writers affecting IsActive/Administrator/readiness/memberships participate, including bootstrap/setup transitions; count/target read outside gate not commit permit. Cannot commit zero IsActive+Administrator+CredentialReady, self-deactivate allowed if another remains. Default grants installation/uniqueness and replay of manual bootstrap are explicit deployment operations, not silent restoration of admin at every startup.

Per-request access check is not a long-lived cross-BC permission lease/SQL transaction: fresh coherent committed Identity primary snapshot then public immutable ICurrentUser/PermissionSnapshot. Separate EF user/role calls/split-query generations cannot claim coherency; concrete one-query/coherent read mapping must be verified. Stale claims/cache/read replica not authority; accepted already-checked in-flight may complete. Credentials/security stamps stay provider-private, no second access-role authority.

Private staff Session keeps fixed CreatedAt/ExpiresAt(default8h), terminal revocation; no sliding/refresh/RememberMe. Current-session logout, deactivation/reset invalidation and Reactivate/new-login cannot clear original revoked status. Setup action(default20min) purpose/user/generation/consumed outcome protected by conditional owner commit. Issuance/delivery/reissue/provider token storage/receipt/safe inquiry/cleanup profile needs concrete implementation; do not store raw password/setup token in normal DTO/audit. Temporary DB/keyring outage not conclusive original absence or permission denial.

**Provider verification boundary:** preparation вне gate строго read/CPU-only и не вызывает Identity auto-save. SDK verification может иметь побочные rehash/failed-counter/lockout updates; provider adapter должен отделять immutable password evidence от этих writes. Final own guarded transaction сверяет captured credential generation/stamp с текущими значениями; stale positive proof не создаёт Session после reset. Rehash, failed-attempt counters, lockout/reset counters и security-state writes проходят own protected conditional transaction, без скрытого UserManager/SignInManager commit вне gate. Exact SDK call adaptation/порядок hash migration и stamp policy проверяются реальным provider store, не предполагаются по названию CheckPassword. Это не собственный hashing/stamp store: криптографию и provider поля по-прежнему обслуживает Identity.

Пример side effect проверен в [официальном UserManager v10.0.0](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Identity/Extensions.Core/src/UserManager.cs): CheckPasswordCoreAsync при SuccessRehashNeeded обновляет hash и user. Это reference evidence, не выбор package version либо утверждение, что provider уже установлен в CM.

IdentityAccess normalized safe F18 activity intent saves in same source local commit as successful access change. Audit append/inbox separate; no source+Audit transaction. Failed/rolled-back/no-op operation not recorded as success; login/security-event taxonomy belongs later Q10/F18, not silently added business audit.

Browser independent origins on HTTPS same-site subdomains require exactUI-origin credentialed CORS, host-only API cookie/SameSiteLax, antiforgery API response/header flow (UI cannot read HttpOnly API cookie), identity-token rotation and persistent restricted encrypted-at-rest shared DP keyring. Staff/Delivery purposes separate. Physical provider/path/config/header/DEV topology remains Q15/deployment; no shared-domain cookie/proxy dependency.

Identity-switch safety combines local tab notice and server expected-login-context precondition on mutation, without client actor authority. No exception to current permission check or business own ETag. Personal AllowedActions can change without own Version: physical core concurrency vs whole-representation validator split Q15; shared cache/stale304 cannot bypass access.

Open engineering: exact schema/index/migrations/provider store enlistment/counter update/secret blocklist/code delivery/bootstrap CLI/receipt retention, permission registry codecs/public policy names, coherent projection/query and context precondition, keyring/CORS/CSRF options/rate limit and actual browser tests. Accepted defaults/mechanisms/BC independent hosting are fixed, no new architecture gate. Real PostgreSQL/barrier/provider/HTTP/browser tests must cover last-admin races, half-commit/ACK/setup reuse/expired generation, actual cookie/logout/reactivate/current rights, permission list snapshots, keyring restart/replicas and form File/original ETag recovery; docs checks are not those tests.

## F17: decision atomicity и automatic binding


[F17](../features/F17-content-review.md) owns moderation в PublishingDbContext с F10/F11, без нового Moderation BC. Pending unique per exact RevisionId, immutable comments/terminal metadata/own int Version, same-BC target FK и retained history обязательны; enum/index/mapping/actor/system identity/comment ordering/outbox envelope/profile уточняются.


Own moderation ETag не защищает mutable F10 revision lifecycle. Read-only EF tracking/FV/precheck не гарантируют Submit/Approve validity до commit. Submit/Approve, revision Retire/Withdrawal/final decision участвуют в [MVP gate](#mvp-протокол-consistency-guard) внутри owner Publishing transaction; current F15/distribution facts читаются independent primary public ports Q23/F15. Foreign DbContext не enlisted. После rollback own mismatch412 отличается от foreign/lifecycle409; context/diff read не commit permit.


Approved decision и durable exact continuation фиксируются атомарно; Rejected сохраняет old pointer, Withdrawn iteration cancels exact bound item. Envelope EventId/version/correlation/producer/sequence и outbox/inbox registrations/dedup материализуются по S22. Decision message не activation/Delivery event, lag не стирает committed Approval. Own moderation ETag и current shown comparison basis независимы; basis drift409 требует fresh consent, subsequent drift после committed Approval не отменяет decision.


Подготовка automatic item атомарно создаёт F10 revision/F17 own iteration/exact binding/AwaitingModeration внутри Publishing. Existing-Pending/hash adoption запрещён; mandatory ClientOperationId replay и same-item stage retry используют committed original identities. Cancel/Supersede item не Withdraw-ит human iteration, а её новый manual Submit не reattaches old item. Перед continuation F11 проверяет exact Approved tuple/current head/Product state Version/Variant Sequence и fresh eligible facts. Policies закрыты workflow выше; actor/envelope/mapping/result codec и numeric retry/lease/limits материализуются.


Real PostgreSQL barrier tests проверяют lifecycle/finality/outbox/binding; mock port/query-before-commit не proof. Exact previous Approved reuse для rollback сохраняется с F11 current guards; дополнительные reconsideration cases Q28 не выбираются молча.


## F18: source intent и audit append


[F18](../features/F18-staff-activity-history.md) requires two durable boundaries: source mutation + normalized safe activity intent one source local commit; audit immutable append + inbox completion one separate audit local commit. Dispatch после source commit at-least-once, audit ACK после audit commit. Common ambient transaction source+Audit запрещена; current generic Catalog inbox не доказательство suitable F18 enlistment.


Pure after-commit callback/memory event без stored intent оставляет crash gap. Source intent failure before commit отличается от audit outage after committed action: latter не rollback бизнес-success, backlog retained/recovered. IdentityAccess Core Identity user/roles/setup/session + safe source intent сохраняются owning transaction по принятому Q24; exact provider SDK enlistment нельзя предположить из названия метода.


**Уточняется:** source outbox/participant storage per owner, audit DbContext/schema/tables/inbox/index/SourceEventId uniqueness/canonical fact comparison, actor/target/typed metadata mappings, contract envelope/producer trust/topic/transport/consumer registrations, retry/quarantine/DLQ/redaction/operational recovery and lag thresholds. Replayed same EventId same normalized facts append once; changed facts не overwrite retained row, profile canonical/hash choices не guessed JSON equality/digest length.


Actor/target safe snapshots captured source-side, audit not foreign-enriched; no Identity/target FK deletion cascade. No global timestamp causal ordering/activation Sequence from audit. MVP retains all rows without auto purge/user delete; archive/export/exceptional anonymization require later policy. Real source/audit PostgreSQL + broker fault/dedup tests нужны, mock after-commit event не proof durability.


## F18: принятый capture append и cursor read

Approved block1–24: [Q10 scope](../../../business/questions/Q10-permissions-and-isolation.md#f18-history-scope-и-visibility), [F18 owner models](../features/F18-staff-activity-history.md), [Q15 activity contract](Q15-api-and-source-code.md#f18-принятый-activity-contract). Audit остаётся technical read contour/schema, не новый source-of-truth BC. SourceActivityIntent/versioned StaffActivityEvent/StaffActivityRecord/typed metadata/ActionProfileDefinition/cursor/read values определены once in F18; physical structures implement selected boundaries.

| Stage | Durable result / failure boundary |
| --- | --- |
| Source normalization/capture | Actual audited business result + trusted actor/initiator/exact target/safe profile, stable EventId, source UTC time; before owner commit |
| Source local commit | Business change + durable activity intent atomic; failed/no-op/replay not new fake fact; conditional original receipts remain owner source |
| Dispatcher | Read committed stored intent, at-least-once unchanged identity/fact; outside source transaction/gate, no source response waiting for Audit |
| Audit consume | Trusted schema/profile/classification + unique SourceEventId/normalized-fact comparison; record+own inbox completion atomic |
| Same-ID recovery | Same fact returns original append; changed stable fact quarantine/no overwrite; audit ACK only after own durable result |
| List/options/exact read | Current-reader PermissionSnapshot, classification before filters/keyset/options/projection; no foreign provider/target DB lookup |
| Owner target/remark read | Separate public owner authorization, unavailable target/reason doesn't mutate retained audit row |

Source owner invalidating mutations use existing MVP gate first within their own ReadCommitted transaction, then safe intent joins only that owner's participants. Atomic preparation tuple yields one semantic fact with references; separate human Approval/Activation/repair/Resolve commits each have their own fact. Source facts derive performed results, not DomainEvent dump/button/request payload. Significant automatic Publishing/catalog-conflict transitions may be audited with System/Integration actors; ordinary projection/Delivery materialization/inbox/retry/security counters excluded.

Audit append uses **own** short transaction/unique record+inbox and doesn't obtain global source mutation guard merely to append an immutable row. Concurrent same EventId commits converge one winner via unique constraints/new-scope readback after conflict. Different normalized same-ID fact quarantined, no last-writer-wins mutation. Audit failure after source commit preserves source success/outbox backlog. Source lost ACK reconciles original operation identity, not new EventId/duplicate business action. Source and Audit don't share ambient UoW/FK.

Duplicate meaning includes source/profile, actual actor/initiator/action/OccurredAt/target/Outcome=Committed/typed safe metadata; excludes delivery retry envelope, AuditId/RecordedAt and consumer processing details. Preserve sufficient lossless accepted normalized source-fact representation for comparison: reserialized timestamptz/JSONB projection need not reproduce source ticks/bytes. Exact normalized profile/time/Guid/sets/serializer representation materializes before producer-consumer fixtures; never silently treat a rounding-induced mismatch as new record or compare arbitrary property-order JSON. Metadata payload is safe schema, not raw business content dump.

Captured actor/target captions are immutable optional source snapshots. Renamed/deactivated/deleted user or target cannot rewrite actor/current context, cascade retained rows or force later foreign enrichment. Staff/System/Integration/Operator semantics distinct; trusted durable initiator of automated operation isn't executor. Operator initialization/account recovery uses approved secured source boundary, no fake CM staff. Common vs StaffAccess classification comes from registered ActionProfile and is checked against producer/action/schema, not arbitrary caller visibility field.

Fresh request PermissionSnapshot gives audit.activity.read to Specialist/Administrator and audit.staff-access.read only Administrator. Predicate for allowed record class precedes **keyset/options/count**; no post-page redaction/leaking staff authors through filter dictionaries. No target permission inferred from audit read. Options authors come from visible history, not IdentityAccess admin directory; old captions survive directory changes.

SQL list ordering preserves full source OccurredAt DESC, Id DESC. Private persisted **SortOccurredAtUtcTicks:long / bigint** is derived from exact normalized source UTC ticks (100ns); it is the comparison/range/cursor key, not a third business time or causal sequence. A microsecond timestamptz projection cannot replace that key: two distinct source times below SQL timestamp precision still order by true time before UUID tie. From/To UTC `[From,To)` converts to the same tick basis, no default cutoff; action/actor/type+ID/correlation/operation predicates consistent with cursor. Default50/max200; read one extra eligible row for HasMore/NextCursor, no required total count/offset page contract. Opaque cursor binds reader/query/last stored tick+UUID key; parse/version/query mismatch rejects invalid input, dependency outage not invalid token. User/context change resets query; each page reauthorizes current access. No promised fixed snapshot across pages: late/out-of-order append remains possible, manual refresh resets first page. Canonical SQL UUID ordering, exact index/cursor codec/integrity options Q15, not guessed CLR Guid.CompareTo/EF insertion order.

Retention MVP: no automatic purge, public delete/update/undo/export/realtime/notification. Later archive/anonymization policies require explicit requirements/migrations; immutable ordinary history does not prescribe infinite retention for every future milestone. Secret suppression is schema allowlist at source and consumer; generic password-name blacklist insufficient. Login/failure/provider-security events belong security diagnostics, not business history by default.

Real tests: source state+intent rollback/crash/ACK, dispatcher/Audit outage and stable replay, competing append/inbox/conflicting duplicate/time precision, tuple-vs-independent outcomes and true no-op, correct operator/integration/initiator capture, classification/options filtering before pagination, fresh changed reader roles/context/cursor boundaries/late append, missing/denied F17 reason/target, immutable as-of captions/no FK cascade/no secrets. Exact DDL/index/partitions/outbox SDK/inbox broker topic/retry/quarantine/DLQ/observability thresholds and literal serialization/cursor/wire remain engineering; chosen protocols and limits are not TBD. Documentation verification not these runtime checks.

## F21: source projection и optional cache


[F21](../features/F21-schema-registry.md) не создаёт собственных tables/DbContext/BC/migrations/ZIP history. Source adapter живёт в owner ContentModeling.EF.Persistence, queries Registry — в том же data-access layer, pure exporter — Application, ports/read values — Application.Abstractions и controller — Api. Это reuse existing physical assemblies, не отдельная Registry persistence authority.


Target source берётся из exact Published root: frozen name/key/number/hash и complete immutable field definition. Current child major/minor version DTO и published Fields JSON не обеспечивают всю target metadata/type semantics. Нельзя восстановить historical labels current parent rename-значениями или silently пересчитать source hash. Owner changes/migration/recovery относятся к F01–F03, не незапрошенному F21 backfill.


**Уточняется:** read checkpoint/availability consistency, materialized batch capture/error semantics, source projection/result failure representation и precise lifecycle scope Withdrawn. Retired exact history доступна по F02. В Registry read нет write transaction/SaveChanges/outbox или distributed commit; exporter не держит DB context после материализации.


Optional cache не обязательна. Если выбрана: exact version + exporter/profile/dependency identity в ключе, source hash один недостаточен; current access/lifecycle не обходятся cached bytes/304. Store/TTL/eviction/invalidation/profile evolution и read-consistency выбираются отдельно. Нет fake cached latest или чужого ContentModeling DbContext в reusable generator/Core.


## F22: ephemeral artifact и local output


[F22](../features/F22-csharp-model-generation.md) не хранит Domain/EF rows, generated history/job/cache/source archives или outbox. Server wrapper uses F21 → same Core → validated complete files/bindings → prepared ZIP → binary stream; no DB transaction/SaveChanges/schema republish. Previous F21 owner snapshots/migrations остаются у ContentModeling.


**Уточняется:** archive strategy memory/temp resources/stream abstraction, bounds/seekability/length, init/ownership transfer handler→Api, cancellation/abort cleanup. Preparation failures before HTTP success не return partial artifact; incomplete network after headers не becomes JSON ZIP or confirmed browser save. Cleanup только собственных verified resource paths; no persistent artifact job по умолчанию.


CLI Registry/Core и local project writes — независимые boundaries. Output ownership/overwrite/staging/backup/stale-file policy/atomic multi-file replacement и partial-write recovery Q31/Q32. Нельзя приравнять HTTP+filesystem к общему commit, удалять чужие/manual files или скрывать фактический partial outcome. Exact generated layout/profile/package выбран до claims о reproducible output.
