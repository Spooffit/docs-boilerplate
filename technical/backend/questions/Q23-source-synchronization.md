# Q23. Синхронизация источника
## Подтверждённое направление
Product/ProductVariant создаются и редактируются только в VR School. Это новые сущности, которые после деплоя будут постепенно и вручную заводиться сотрудниками, вероятно с переносом сведений из legacy Application/Project. Отдельного bootstrap существующего каталога в Content Manager **не требуется**: CM получает обычные события новых сущностей с момента их создания.
Content Manager не читает таблицы VR School напрямую и не получает каталог через Project Storage. Project Storage сохраняет собственный legacy-поток. CM и Project Storage являются независимыми потребителями необходимых событий.
Каталог, License и grants передаются через шину как state-carrying snapshots, а не как полевые delta-команды.
## Событийная модель
Для каждой source entity в VR School используется внутренний монотонный `int Version`.
Snapshot-event содержит как минимум:
- `EventId` — уникальная identity transport-события;
- `EntityId` — identity Product/ProductVariant/License/другой source entity;
- `Version` — версия этой source entity;
- `OccurredAt` — время source-события для аудита/диагностики;
- полный актуальный snapshot полей, нужных CM.
CM различает last applied SourceVersion и durable latest accepted/observed watermark для entity. Актуальный [протокол V2](#принятый-протокол-каталога-v2) определяет две стадии и source-data faults; первоначальное правило ниже применяется с учётом newest принятого candidate, а не только applied state.
Правило применения:
- incoming Version > latest accepted watermark → атомарно принять candidate для последующей обработки;
- incoming Version == latest accepted watermark → equal-state republish no-op; противоречивый normalized state quarantine без overwrite;
- incoming Version < latest accepted watermark → stale/no-op.
Глобальная sequence для всей VR School не нужна. Freshness определяется per-entity Version. `OccurredAt` не используется вместо Version.
Так как сообщение несёт полный snapshot, пропуск промежуточной версии не является проблемой: если CM имеет v18 и получает v21, v21 можно применить напрямую без v19/v20.
## Надёжность доставки
VR School должен публиковать integration events согласованно с изменением собственной БД через transactional outbox. Для реализации планируется переиспользовать outbox-механизм LeanSoft boilerplate; точный API/registration остаётся технической деталью реализации.
CM использует inbox/consumed-message tracking по `EventId`, чтобы transport retry не создавал повторных side effects. Inbox и `Version` решают разные задачи:
- EventId/inbox — повтор конкретного сообщения;
- Entity Version — stale/newer state.
CM не становится event store VR School. Хранятся current projection, last applied Version, необходимые source metadata, inbox с ограниченным retention, unresolved dependencies/conflicts и operational diagnostics. Бесконечный архив всех integration messages не требуется.
## Reconciliation MVP
Отдельного bootstrap/snapshot-generation protocol, GenerationId, Started/Completed marker, entity count или readiness-generation **не вводится**.
Для MVP нужен reconciliation-механизм, но он должен переиспользовать те же обычные full-snapshot events. Если требуется сверка/восстановление, VR School повторно публикует актуальные snapshots нужных entities через шину; CM применяет только более свежие Version и безопасно принимает duplicate/stale.
Выбрана защищённая операционная команда на стороне VR School: selected IDs либо весь current каталог; selected Variant включают parent snapshots. Scheduling/error report не означает CM apply/readiness. Exact command wire/access/registration ещё инженерная детализация; HTTP export/import и прямой доступ CM к БД VR School не вводятся.
Отсутствие entity среди каких-либо повторно опубликованных snapshots не трактуется как delete/archive. Lifecycle должен приходить явно внутри snapshot.
## Зависимости между source entities
ProductVariant snapshot может прийти раньше Product. CM сохраняет Variant как pending/unresolved и связывает его после появления parent Product. Временное отсутствие parent:
- не создаёт фиктивный Product;
- не является poison-message;
- не имеет TTL с автоматическим удалением.
Pending state остаётся до появления parent, более нового snapshot с другой relation или ручного разбора source inconsistency.
## ProductVariant move
Перенос существующего ProductVariant между Product поддерживается в MVP и не входит в Demo.
Если новый ProductId совместим с локальным CM state, relation применяется. Если перенос конфликтует с local items/overrides/assignments/publishing/policy dependencies, newest source snapshot сохраняется как ожидающее состояние F15, а специалист разрешает последствия вручную.
## Subscription topology
CM и Project Storage получают события через независимые queues; реплики CM конкурируют в собственной CM queue. Применяется [S22](../standards/S22-eventbus-topology.md) Contracts/RabbitMq/Default; stable exchange/queue names, package/ACL и конкретные retry/error registrations материализуются явно. CLR rename не переименовывает существующую topology молча.
## Versioned integration contracts
ProductSnapshotV2/ProductVariantSnapshotV2 — отдельный выбранный контракт с int Version и Active/Archived lifecycle, не несовместимая перезапись V1 long EntityRevision/IsDeleted. DTO определены один раз в [F06](../features/F06-product-catalog.md). Package SemVer/registry conventions и coexistence/cutover wiring ещё инженерная детализация; source IDs/version continuity и независимый Project Storage flow сохраняются.
## Activation key boundary
Raw activation key не публикуется в broker и не хранится в CM projection. Проверка предъявленного key выполняется отдельным HTTP validation API VR School по Q16.
License/grant facts продолжают приходить full-snapshot events через broker.
## Current-state snapshot 01.10.2026
В CatalogAccess уже есть projection entities Product/ProductVariant/License с source revision/timestamp/deleted metadata и read-only list/lookup API. Это подтверждает направление локальных projections.
В предоставленном snapshot не найден законченный MassTransit/RabbitMQ consumer для этих projections, поэтому producer/consumer contracts, topology, retry/inbox/outbox wiring и reconciliation ещё требуют реализации.
## Применение
[F06](../features/F06-product-catalog.md) · [F15](../features/F15-catalog-conflicts.md) · [F12](../features/F12-content-entitlement-policy.md) · [F13](../features/F13-content-delivery.md) · [Q11](../../../business/questions/Q11-catalog-ownership.md).
## Уточнение блока 23: SourceVersion vs Version
На integration boundary VR School по-прежнему публикует своё поле `Version` как монотонную версию source entity.
Внутри CM projection это значение хранится/называется **`SourceVersion`**, чтобы не смешивать его с общим внутренним optimistic-concurrency token CM `Version`.
Правило freshness:
- incoming event.Version > latest accepted/observed SourceVersion → snapshot кандидат на durable receive/work;
- equal → equal-state republish no-op либо contradictory-state quarantine; less → stale no-op.
Если два consumer execution конкурентно применяют snapshots одной projection entity, локальная запись дополнительно защищается внутренним CM `Version`; при concurrency conflict consumer перечитывает projection и заново применяет freshness rule по SourceVersion.
Таким образом:
- SourceVersion отвечает «насколько свеж источник»;
- Version отвечает «не изменил ли кто-то локальное состояние между read и commit».

## F06: проекция, pending и конкурентная обработка

Связано с [backend F06](../features/F06-product-catalog.md), [frontend F06](../../frontend/features/F06-product-catalog.md), [F15](../features/F15-catalog-conflicts.md), [Q25](../../../business/questions/Q25-catalog-conflicts.md), [Q27](../../../business/questions/Q27-catalog-archiving.md#f06-источник-lifecycle-и-допустимость-нового-использования), [Q15](Q15-api-and-source-code.md#f06-детализация-read-контракта). Ниже проверенная исходная точка и открытые implementation вопросы; она не заменяет согласованный target full-snapshot/MVP/no-bootstrap.

### Подтверждённая исходная точка F06 на 04.10.2026

Snapshot от 01.10 выше не отражает новые файлы текущего checkout: consumers/receiver/processor и DI уже существуют. Runtime deployment, broker delivery и SDK atomicity этой статической сверкой не проверялись.

- [Product](../../../../contentmanager.api/src/BoundedContexts/CatalogAccess/ContentManager.CatalogAccess.Domain/Products/Product.cs) хранит Name/Description, SourceRevision: long, SourceUpdatedAt и IsDeleted; ApplySnapshot отсекает equal/stale. Есть owned коллекция Variant IDs. [ProductVariant](../../../../contentmanager.api/src/BoundedContexts/CatalogAccess/ContentManager.CatalogAccess.Domain/ProductVariants/ProductVariant.cs) имеет immutable ProductId и сначала отклоняет move, затем проверяет revision. Target допускает move, observed pending и отдельный int Version.
- [ProductVariantConfiguration](../../../../contentmanager.api/src/BoundedContexts/CatalogAccess/ContentManager.CatalogAccess.EntityFramework/Configurations/ProductVariantConfiguration.cs) содержит required Product FK, unique ProductId/Name и SourceRevision concurrency token. [ProductConfiguration](../../../../contentmanager.api/src/BoundedContexts/CatalogAccess/ContentManager.CatalogAccess.EntityFramework/Configurations/ProductConfiguration.cs) хранит `_versions` в отдельной owned table. Это ещё не target nullable applied/pending layout.
- [CatalogSnapshotReceiver](../../../../contentmanager.api/src/App/ContentManager.Application/Integration/CatalogSnapshotReceiver.cs) проверяет envelope, блокирует entity через PostgreSQL advisory lock, сохраняет JSON snapshot journal + LeanSoft inbox work. Дубликат определяется по EventId либо entity/revision; иная event identity на той же revision отклоняется, canonical whole payload сравнивается отдельно. Broker durable receive и projection apply являются разными фазами.
- [CatalogSnapshotProcessor](../../../../contentmanager.api/src/App/ContentManager.Application/Integration/CatalogSnapshotProcessor.cs) применяет revision+1 по очереди, запрашивает GetCatalogHistoryV1 при пробеле, отклоняет incomplete history. Для Variant требует parent через EnsureExists/history; missing dependency — exception, move — Domain exception. CatalogBootstrapMessage сканирует history по sequence. Эти механизмы противоречат прямому apply v21 поверх v18, normal pending и отсутствию bootstrap целевого каталога.
- [CatalogSourceSnapshotConfiguration](../../../../contentmanager.api/src/BoundedContexts/CatalogAccess/ContentManager.CatalogAccess.EntityFramework/Configurations/CatalogSourceSnapshotConfiguration.cs) хранит journal с unique entity/revision и baseline. Это permanent history текущей реализации, а не необходимая целевая inbox retention. Общий processor также обслуживает License/F12: removal нельзя выполнить как изолированный Product cleanup без проверки остальных владельцев.
- [CatalogSynchronizationServices](../../../../contentmanager.api/src/App/ContentManager.Application/Integration/CatalogSynchronizationServices.cs) регистрирует реальных Product/Variant consumers, SDK inbox, RabbitMQ receive endpoints и request history client при CatalogSynchronization:Enabled. Наличие регистрации не доказывает включённую конфигурацию или доставки в окружении. [InboxUnitOfWorkAdapter](../../../../contentmanager.api/src/App/ContentManager.EntityFramework/Integration/InboxUnitOfWorkAdapter.cs) использует IntegrationDbContext и shared CM scope; комментарий/реализация ориентированы на shared transaction всех BC. Для target local CatalogAccess boundary требуется проверить SDK protocol и enlistment, а не обещать atomicity по имени адаптера.
- Versioned source [ProductSnapshotV1](../../../../VRSchool.Host/Integration/EventBus/VRSchool.Integration.EventBus.Contracts/Catalog/V1/ProductSnapshotV1.cs) и [ProductVariantSnapshotV1](../../../../VRSchool.Host/Integration/EventBus/VRSchool.Integration.EventBus.Contracts/Catalog/V1/ProductVariantSnapshotV1.cs) уже существуют: ContractVersion=1, EventId, EntityRevision: long, OccurredAt, IsDeleted, Id, Name/Description, у Variant ProductId. EntityName задаётся attribute. Target int Version → SourceVersion и normalized lifecycle требуют contract migration/profile, не unchecked cast.
- [VRSchoolDbContext.Catalog](../../../../VRSchool.Host/src/VRSchool.EntityFrameworkCore/EntityFrameworkCore/VRSchoolDbContext.Catalog.cs) увеличивает CatalogRevision: long, сохраняет full snapshots history с source changes и запрещает изменение parent. [GetCatalogHistoryV1Consumer](../../../../VRSchool.Host/src/VRSchool.Application/EventConsumers/GetCatalogHistoryV1Consumer.cs) возвращает history, а global request материализует bootstrap snapshots для revision=0. Это не согласованный current-state republish trigger. В этих проверенных файлах не доказан стандартный outbox dispatcher, публикующий snapshots; durable history row сама по себе не доказывает broker delivery.

### Непротиворечивый applied / observed контракт

Последняя применённая source version остаётся `SourceVersion`. При pending необходима также долговечная граница свежести принятого source state: `LatestObservedSourceVersion` либо эквивалент на единственном authoritative pending owner. В F06 используется это логическое имя, physical layout ещё выбирается.

Пример: applied v18/A, newest pending v21/B. Внешний v20/C — stale относительно наблюдаемого v21 и не меняет target. Resolver сохранённого v21 имеет право применить его после появления parent без нового EventId, хотя v21 == watermark; он revalidates newest target перед commit. Новый v22 не должен быть затёрт resolver v21. До первого apply Variant identity/pending существуют, а applied parent/name/version отсутствуют совместно.

### Выбранные механизмы и оставшаяся детализация

| Область | Принято | Оставшаяся engineering деталь |
| --- | --- | --- |
| Wire profile | ProductSnapshotV2/ProductVariantSnapshotV2, positive int Version/explicit Lifecycle, source Name limits/optional Description, checked V1 continuity | Literal enum/JSON codecs, package SemVer/source cutover/transport registration; no unchecked cast |
| Pending ownership | One newest candidate/active payload owner receive-state→unresolved Variant→Open conflict, whole nullable AppliedState | Own schema columns/JSONB, atomic handoff/reference mappings, parent/owner indexes |
| Receive/work | Separate receiver acceptance and worker apply, own commit before ACK, accepted newer prevents old apply | LeanSoft inbox/work participant enlistment/DDL/Host registration and unknown-ACK lookup carrier |
| Wakeup/recovery | Durable parent signal plus scan, bounded claims/reclaim/token fencing, pending no TTL | SDK hooks/backpressure/batch/lease/backoff settings after measurements |
| Equality | SameEventId immutable event vs equalVersion source state, newEventId republish valid, contradiction quarantine, stale no-op | Versioned canonical codecs/receipt evidence/quarantine disposition and retention settings |
| Faults | Trusted reliable newer entity invalid input → scoped unreadiness/fault boundary, no fake F15/global block; explicit supported recovery | Fault mappings/trust envelope admission/safe diagnostic codes and operator recovery registration |
| F15/source change | Same Open episode/new target assessment separate, missing parent retained, compatible SourceChangedApply | Concrete public impact/evidence query adapters and assessment wire basis |
| Reconciliation/topology | Protected VRS selectedIDs/all-current command incl selected Variant parents; scheduled≠applied, independent subscriber queues | Command wire/auth/response destination, exact names/package/ACL/retry/error settings; no CM bootstrap/UI POST |
| Migration/testing | Source IDs/version continuity preserved; no automatic License/GrantPeriod history removal, no Project Storage cutover assumptions | Physical migration and actual source/SQL/broker/SDK/browser acceptance; documentation is not runtime evidence |

**Ответ:** блок 1–24 согласован полностью. Ниже закреплён протокол; exact codecs/DDL/SDK/transport registrations и реальные проверки остаются engineering, не повторным выбором mechanisms.

## Принятый протокол каталога V2

Объём — MVP Product/ProductVariant F06/F15. License/grants/periods/security epoch имеют собственные F12/Q16 contracts; каталоговый full-snapshot algorithm не переносится на них автоматически. Demo не получает Identity/broker/catalog dependencies. [F06](../features/F06-product-catalog.md) определяет все модели и реализацию по слоям один раз, [F15](../features/F15-catalog-conflicts.md) — конфликт и repair/Resolve; [Q20](Q20-persistence-and-migration.md#catalog-v2-receive-work-и-fault-boundary) задаёт durable boundaries, [Q15](Q15-api-and-source-code.md#f06-детализация-read-контракта) — read carriers.

### Контракт и migration

ProductSnapshotV2 и ProductVariantSnapshotV2 имеют Guid EventId/Id, int ContractVersion/positive monotonic Version, UTC OccurredAt, явный Lifecycle Active/Archived, Name и optional Description; Variant также Guid ProductId. Version gap разрешён; source timestamp не определяет freshness. Source нормализует Name до своего commit, CM проверяет и сохраняет source result без собственного trim/rename. Product Name 1–120, Variant Name 1–200, Description nullable/empty либо до 500; Name не identity/CM unique key. Поля и requiredness только F06, не второй DTO в Q.

V1 long EntityRevision → V2 int Version требует checked positive-range mapping/continuity без wrap/reset; IsDeleted mapping не заменяет выбранный новый Lifecycle wire. Schema/message/transport version меняются явно. Проверка фактических source versions и package/broker cutover обязательна; Catalog migration не удаляет License/GrantPeriod history, не меняет Project Storage subscription неявно. Source transactional outbox существующего механизма атомарен с source mutation; no parallel bespoke outbox.

### Receive, active candidate и work

Receiver и worker — разные durable стадии. Receiver допускает trusted producer/schema/envelope, нормализует безопасный полный snapshot, начинает own CatalogAccess ReadCommitted transaction и первым получает существующий MVP guard. В одном commit сохраняет newest accepted candidate/watermark, normalized event receipt и durable work. ACK только после фиксации принятия или durable classified repeat/failure outcome; это не projection-applied result. Unknown commit ACK reconciles exact event/source receipt, no fabricated rollback/new event.

На entity один newest candidate. Полный active payload имеет единственного mutable owner: receive-state до классификации → unresolved Variant state при missing parent → CatalogConflict.PendingState при Open. Handoff атомарен в CatalogAccess; work содержит identity/version/owner reference, не вторую изменяемую target copy. Captured immutable receipt evidence с ограниченным retention не является второй live authority. Initial pending identity существует с целиком отсутствующим AppliedState; прежний AppliedState при move сохраняется целиком.

Worker получает тот же guard заново до reads, перечитывает newest accepted evidence/fault/parent и fresh public primary dependencies. Accepted v22 не позволяет применить v21 до обработки v22. Superseded work завершается без старого apply. Own apply/pending/conflict/history/work completion и необходимый F18 intent атомарны; no foreign DbContext transaction. Технический отказ не означает compatibility, не создаёт успешный apply и сохраняет work для retry.

Bounded worker claim/lease и durable recovery scan возвращают work после crash. Lease — защита технического владения work, не разрешение source apply: stale worker после потери claim и нового candidate не коммитит старый result. Parent commit сохраняет durable wakeup; конкурентное появление parent/Variant и потеря внешнего сигнала компенсируются scan/recheck. Missing parent не TTL/poison/fake Product; Archived parent найден и отдельно участвует в lifecycle guards. Точные lease/backoff/batch numbers конфигурационные после измерений, не новый нормативный default.

Newer target сохраняет тот же Open conflict episode. Старые Issues относятся к прежнему assessed basis и не выдаются за fresh оценку нового target; pending assessment и waiting parent видимы отдельно. Source correction confirmed compatible даёт whole apply + Resolved(SourceChangedApply) в одном own commit с trusted System attribution/F18; repair/manual Resolve сохраняют отдельные own commits. Нет auto-publish/resume/rollback repaired patch или rewriting immutable historical artifacts.

### Identities, equality и faults

| Случай | Принятый результат |
| --- | --- |
| Transport retry, тот же EventId/normalized immutable event fact | Original receive outcome, no new work/semantic F18 action |
| Тот же EventId, изменённый stable факт | Durable contract quarantine, original unchanged; no fabricated accepted/applied result |
| Новый EventId, equal source Version/equal normalized source state | Valid republish no-op; event identity не делает state свежее |
| Equal source Version/contradictory normalized source state | Contract quarantine/no overwrite; source correction new Version |
| Lower-than-watermark input | Stale no-op; no promise inspect every ancient payload after retention |
| Internal pending resolver | Recheck saved newest candidate без нового внешнего EventId; equal-to-watermark intentional |
| Reconciliation republish | Новый EventId/current full source snapshot с прежней source Version, тот же обычный receive pipeline |

Event equality включает immutable normalized admitted envelope/fact, включая OccurredAt. Source-state equality сравнивает IDs/Version/ProductId/Name/Description/Lifecycle, исключая EventId и event OccurredAt повторной публикации; equal republish не переписывает applied state/history. Canonical precision/null/ID/time/serializer profile exact codec engineering; raw JSON member order/transport headers не source truth. Producer reconciliation не изображает новый source mutation: исходное version state сохраняется, republication metadata отдельно от source change facts. Fingerprint не равен CM content hash, record receipt не perpetual event store.

CatalogIngestionFault — durable scoped техническая source-data unreadiness. Если trusted admitted envelope достоверно сообщает entity и более свежую Version, но required snapshot invalid/unsupported, сохраняется fault boundary: старый candidate нельзя применить и affected new-use/writes запрещены до trusted recovery. Invalid data не становится accepted valid snapshot; fault boundary хранится отдельно от accepted watermark. Не F15 move conflict/Archive/Withdrawal/license revoke и не blanket global catalog block. Если entity/version невозможно достоверно определить, сообщение quarantine/diagnostics без придуманного entity blocker. Malformed/missing EventId при достоверной entity/version не требует fake source EventId: fault/quarantine имеет собственную technical identity и private immutable rejected evidence, original EventId optional только если usable; event receipt uniqueness относится к admitted nonempty EventId.

Source correction — новый valid source Version, строго превосходящий relevant active fault boundary. Обычный valid receive сравнивает и accepted watermark, и active fault; covering fault (Version≥candidate) запрещает старый/equal apply. При неизвестном профиле после добавления поддержки допускается explicit authorized replay исходного неизменённого сообщения: после проверки preserved original event evidence, но до terminal original-receipt return, выполняется now-supported normalization/fresh latest/fault validation. Только этот recovery path может атомарно снять covered fault, advance private receipt disposition и сохранить same-version candidate/work; original evidence/EventId не переписываются. Higher active fault по-прежнему запрещает recovery старого сообщения. Противоречивый equal-version state не лечится молчаливой заменой. Transient dependency/storage failure, missing parent, business incompatible move и invalid contract имеют разные outcomes/recovery. Историческая Delivery не останавливается только по catalog ingestion fault; независимые hard restrictions F12/F13 остаются.

### Reconciliation, retention и acceptance

Защищённая операционная команда принадлежит VR School: selected source IDs либо весь current каталог, parent snapshots для selected Variant, current snapshots через тот же source outbox/broker flow. Report показывает scheduled/source errors, не CM applied или global completeness/readiness. Отсутствие ID среди опубликованных не Delete/Archive; source lifecycle явно в snapshot. Exact request/message names/authorization/package registrations ещё engineering; CM UI sync/bootstrap button, public guessed POST, HTTP export/import, direct source SQL и generation Started/Completed не вводятся.

CM/Project Storage независимые подписчики, CM replicas competing consumers собственной queue; source entities не сериализуются как Domain roots. Stable exchange/queue/package migrations по S22, no silent EntityName rename. Receipt/completed work retention configurable с безопасным cleanup; current source state/accepted watermark, unresolved candidates/fault boundary/conflict history сохраняются. SourceVersion защищает от stale apply после удаления receipts, но changed old EventId после retention не обещается обнаружить вечно.

Реальные PostgreSQL/broker/source acceptance tests: source mutation+outbox atomicity, receive pre/postcommit crash/ACK loss, work reclaim/stale worker fencing, concurrent v21/v22, newest-before-processing guard, single-owner handoff, parent/Variant lost wakeup, supersede/compatible SourceChangedApply/missing parent, same-ID collision/equal-version republish/stale cleanup, malformed newer fault/unsupported profile replay, reconciliation scheduled≠applied/absence≠delete, independent subscriptions and no License side effects. Модели CatalogSourceState/CatalogEventReceipt/CatalogWorkItem/CatalogIngestionFault и слой adapters описаны F06. Docs validation не доказывает эти runtime guarantees.

## F12: license period и unlimited mapping

Связано с [F12](../features/F12-content-entitlement-policy.md), [Q20 Agreement](Q20-persistence-and-migration.md#f12-policyagreement-и-подтверждение-периода), [Q01](../../../business/questions/Q01-license-rights.md), [Q03](../../../business/questions/Q03-renewal-and-multiple-licenses.md), [Q16](Q16-activation-and-sessions.md).

Business IsUnlimitedLicense map-ится в explicit unlimited updates fact; отсутствие UpdatesUntil не unlimited, IsUnlimitedUsers и application ExpiresAt не substitutes. Current [License](../../../../contentmanager.api/src/BoundedContexts/CatalogAccess/ContentManager.CatalogAccess.Domain/Licenses/License.cs), [LicenseGrantPeriod](../../../../contentmanager.api/src/BoundedContexts/CatalogAccess/ContentManager.CatalogAccess.Domain/Licenses/LicenseGrantPeriod.cs) и [processor](../../../../contentmanager.api/src/App/ContentManager.Application/Integration/CatalogSnapshotProcessor.cs) содержат legacy issuedAt/policy/expiration mapping. Их наличие не доказывает выбранный normalized target contract.

## F12: planned Agreement confirmed workflow

**Принято:** VR School сначала durable сохраняет planned CM-content period и stable RequestId. Затем authenticated backend вызывает CM вне VRS transaction. CM Publishing фиксирует immutable PolicyAgreement для exact LicenseId/VariantId/SourcePeriodId и исходного plan/applied context. VRS own conditional transaction подтверждает тот же неизменённый plan, AgreementId и outbox. Нет distributed/shared SQL transaction, внешнего вызова под source transaction или CM gate.

Если CM response потерян, retry с тем же RequestId/input возвращает исходный Agreement даже после смены policy/parent; сравнение input предшествует new-quote validation. Если source commit не состоялся/его outcome неизвестен, planned record и own confirmed outcome позволяют восстановиться без нового случайного RequestId. Source не подтверждает CM-content period до получения Agreement. Новая зависимость согласования с CM явно принята; application-license и Project Storage остаются независимыми. Pending Agreement видим в source, а CM не становится коммерческим редактором.

CM License projection не prerequisite согласования: Agreement не выдаёт право. Source scope проверяется через authenticated integration identity; authoritative applied Variant/parent читается под existing guard. Pending/mismatch/ambiguous F15 дают NotReady/context conflict. Raw activation key, staff/Unity quote API не используются. Agreement без source confirmation не создаёт grant и в MVP не истекает автоматически.

Confirmed snapshot содержит immutable AgreementId, exact source period scope, source version, current confirmed StartsAt/UpdatesUntil, explicit IsUnlimitedLicense и distinct closure/revoke facts. Explicit CurrentPeriodId задаётся per license/Variant; full-snapshot completeness позволяет отличить no grant от missing data. Один license-wide SourcePeriodId допускает несколько Variants, но Agreement/binding отдельно на каждый. Versioned payload и exact normalized planned parameters/source context/replay equality ещё материализуются [Q15](Q15-api-and-source-code.md#f12-policyagreement-integration-boundary).

После confirmation AgreementId не заменяется. Contradictory newer source payload rejected/quarantined, прежняя binding сохраняется; missing Agreement/facts не лечатся today policy/as-of/default. До confirmation изменение plan означает новый SourcePeriodId+Agreement. Renewal означает новый период/Agreement; extra purchased duration/unlimited upgrade не маскируются под correction. Legit correction source явно маркирует operation kind/reason/history и обновляет commercial facts того же периода, сохраняя Agreement. CM не угадывает kind по delta dates.

New entitlement selection учитывает applicable CurrentPeriodId: после вступившего renewal нет старого same-license Freeze fallback. Correction меняет будущую Freeze cutoff оценку; pinned-session переоценка/cancellation выбраны Q16; confirmation-history профиль Q17 не меняется. Complete authoritative no-grant — Denied, necessary unknown — Indeterminate/DataNotReady, revoke — отдельный Denied. Strict caller verified multi-license set требует completeness каждого необходимого участника; один unknown делает весь выбор Indeterminate. Один credential подтверждает свою license, без implicit organization-wide expansion.

**Открытая инженерная детализация:** versioned source wire/schema/migration current long revision, authenticated quote transport и exact route/status, planned record/conditional commit/producer outbox adapter, retry/recovery/completeness/public readers и quarantine diagnostics. Приёмка требует failure между каждым durable stage, lost response, changed plan, wrong/substituted Agreement, shared period across Variants, typed renewal/correction и delayed/incomplete source. Current full-snapshot/catalog protocol F06 сохраняется; historical bootstrap и новые commercial CM mutation APIs не вводятся.

Сохранение старого binding/history при contradiction не подтверждает актуальность commercial facts. Quarantined newer source basis не публикуется как complete current evidence: если необходимая текущая basis неизвестна, новое admission получает Indeterminate/DataNotReady. Physical quarantine/recovery profile ещё открыт.

## Credential validation и security epoch

Runtime validation profile задаёт [Q16](Q16-activation-and-sessions.md#принятый-протокол-сессий-и-response-leases), wire owner — [Q15](Q15-api-and-source-code.md#delivery-session-credential-и-replay-contracts), CM storage/guard — [Q20](Q20-persistence-and-migration.md#сессии-replay-и-response-leases). Новая Session и receipt recovery принимают ephemeral raw key(s); CM backend выполняет authoritative VRS validation вне SQL transaction, клиент не вызывает VR School сам. Валидация source read-only для CM content identity, не commercial mutation/выбор RevisionId или передача grant authority. Raw keys не публикуются в broker/CM persistence/logs.

Ответ для каждого входа содержит trusted stable credential ID, bound LicenseId, CM-purpose validity, **CredentialSecurityEpoch**, explicit validity deadline когда finite и required license-source watermark. ValidatedAt — source audit; CM 30sec attempt freshness меряется monotonic до вызова и через HTTP/crypto/gate до final authorization. Source clock не делает задержанный response свежим. Full requested set must have complete verified coverage: missing response/failed needed validation не partial-success admission. Stable verified identities создаёт server normalizer, не Unity input.

CredentialSecurityEpoch — отдельный per-credential security counter, не License SourceVersion/normal metadata revision. Relevant key binding/validity/revoke/restore mutation + epoch + versioned non-secret outbox fact сохраняются atomic **в VRS**. Read validation/label/audit и CM commercial-period correction сами не increments epoch. Explicit finite validity deadline enforced by expiry, without waiting for delayed expire event. Credential IDs/state events не содержат raw key/hash и не расширяют права всей организации.

Fresh authenticated HTTP validation и broker facts используют **один** credential security order. CM Delivery владеет own credential-security knowledge/invalidation; lower epoch не overwrites newer, same epoch inconsistent stable state gives integrity fault. Higher trusted response может advance local knowledge под own CM gate; delayed older revoke после newer restoration не отзывает новую epoch session. Existing sessions bind admitted selected epoch: forward epoch не resurrects old token even new source Valid, new admission required. Unselected credentials не basis existing session.

Source даёт license watermark **hint**, не grants. CatalogAccess required projection того же license-source authority должна быть >= hint и applied/complete. Newest observed pending/quarantine/normalization gap не становится ready old projection. Missing needed facts дают DataNotReady, не InvalidKey/default grant. Source License epoch/version и credential epoch нельзя сравнивать между собой или подставлять Product/Variant SourceVersion. Exact native CLR/version migration/payload/canonical comparison/time normalization ещё materialize profile.

CM Session/receipt/lease mutations и Catalog source apply не shared transaction с VRS/другим BC. Credential HTTP happens outside CM gate; known CM security/source writers и accepted-newest watermark участвуют в общем guard. VRS actual change до получения CM corresponding fact не обещает мгновенный revoke; producer outbox/consumer lag измеряются отдельно. Normal VRS validation outage не invalidates existing session by itself; fresh CM lease facts/restrictions remain necessary.

Current [ActivationKey](../../../../VRSchool.Host/src/VRSchool.Core/Licenses/ActivationKey.cs) has stable long ID, raw Guid key, nullable LicenseId and IsActivated; [ActivationKeyDto](../../../../VRSchool.Host/src/VRSchool.Application.Shared/Licenses/Dtos/ActivationKeyDto.cs) includes secret. Эти leaf types не доказывают credential epoch/outbox/CM-purpose deadline protocol. Legacy DTO не переиспользуется как target non-secret proof, nullable/unmapped identity/date не guessed valid/unlimited. Source implementation remains required work, not changes already applied by this analysis.

**Открытая инженерная детализация:** exact authenticated source API URI/service scopes/transport/profile/package DTO/envelope, concrete key validity/deadline normalization, security epoch persistence/outbox adapter/conditional reads, source native version→CM watermark mapping, duplicate/canonical payload/failure/retry/coverage fixtures и observability. Acceptance включает HTTP→gate delayed proof, credential mutation during validation, newer HTTP vs older broker/restore/no resurrection, license projection gaps, no key echo/persistence/logs и real VRS/source integration. Mechanisms/freshness/defaults/authority scopes уже выбраны.

## F15: newest target и guards

Связано с [F15](../features/F15-catalog-conflicts.md), [Q25](../../../business/questions/Q25-catalog-conflicts.md#f15-технические-границы-разрешения) и [Q20](Q20-persistence-and-migration.md#f15-local-atomicity-и-writer-coordination).

Receiver атомарно фиксирует authoritative pending target/latest observed watermark/receipt/work; если Open уже существует, его PendingState заменяется в том же acceptance commit. Первый Open может быть detected отдельным worker commit после acceptance: atomic single-owner handoff/conflict/history/work outcome, NEW guard/fresh newest checks обязательны. При Open Variant не содержит второй independently mutable full snapshot. Applied v18/pending v21 не допускают замены на input v20. Resolver сохранённого newest candidate == watermark отличается от нового equal/stale transport input; incoming v22 запрещает apply v21.

**Принято:** lifecycle нормализуется в `Active/Archived` по Q27 и приходит только полным newer VR School snapshot. Parent Archive ограничивает Variant new-use без переписывания собственного lifecycle; Restore не снимает другие blockers, Withdrawal или старую inactive Variant. Applied/newest lifecycle и processing показаны отдельно. V2 explicit Lifecycle заменяет V1 IsDeleted только через checked версионированный cutover; codecs/actual adapter wiring ещё engineering.

Repair Publishing и CatalogAccess Resolve — два commit по Q25. Если v22 принят между repair-v21 и Resolve, patch сохраняется, v21 не применяется, нужен refreshed newest preview/revalidation. Missing parent у newest target сохраняет pending/watermark и видимую waiting причину, не даёт fake parent, stale apply или автоматический rollback repair. Immutable history сама по себе move blocker не образует; scope основан на concrete mutable/in-flight dependencies.

**Принято:** durable CM acceptance полного newer snapshot под [MVP gate](Q20-persistence-and-migration.md#mvp-протокол-consistency-guard) сохраняет authoritative newest candidate/watermark в own CatalogAccess commit. Уже принятый v22 запрещает Resolve/apply v21 даже при ещё неготовом impact/parent processing v22. Broker receipt/ACK и durable acceptance/applied outcome различаются. Receiver, worker, pending resolver, reconciliation, Open/supersede и Resolve участвуют в общем transaction-level key; own Read Committed transaction connection защищает fresh source/impact checks до commit. Public dependency reads independent primary scope и не enlisted foreign DbContext. Repair и Resolve получают gate раздельно.

**Принято:** receive/worker split, atomic single-owner handoff, same-episode supersede/current-assessment status, compatible SourceChangedApply, event/source-state equality, scoped ingestion fault и durable wakeup/recovery определены [протоколом V2](#принятый-протокол-каталога-v2). Exact inbox SDK enlistment/mapping/DDL/claim codec/transport disposition ещё engineering. Unavailable impact не разрешает считать старый target newest/compatible; business incompatible move — committed pending conflict, не permanent retry hard exception.

Public impact/block query сам по себе не закрывает гонку Publishing writer → CatalogAccess apply. Все affected assignment/release/revision/promotion/policy/moderation writers и source/resolver используют [принятый MVP protocol Q20](Q20-persistence-and-migration.md#mvp-протокол-consistency-guard): acquire до authoritative reads/row locks, fresh primary public facts, own commit. Read-only failure не считается compatible/unblocked; acquisition timeout/cancel технические, не business CatalogConflict. Exact evidence/DTO/adapters ещё материализуются, два prechecks не заменяют gate.

Текущая проверенная точка на 05.10.2026: ProductVariant.ApplySnapshot запрещает move до своей проверки SourceRevision; CatalogSnapshotProcessor вызывает этот метод, CatalogConflict/ICatalogOperationBlockReader в src не найдены. Ссылки и подробные границы проверки находятся в backend F15; это source evidence, не target отказ от move.
