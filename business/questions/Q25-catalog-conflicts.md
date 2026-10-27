# Q25. Конфликты изменений внешнего каталога


## Принятое направление


VR School остаётся владельцем каталога. CM принимает state-carrying snapshots и обязан поддерживать перенос существующего ProductVariant между Product в MVP. Перенос **не считается запрещённым сам по себе**.


ProductVariant сохраняет identity/локальные CM-данные. Если newest parent/base несовместим с mutable/in-flight dependencies, создаётся видимый F15 conflict. Manual repair/Resolve устраняют локальные последствия; последующий compatible source correction может автоматически apply + close тот же Open episode без fake manual action или publication.


Demo перенос ProductVariant между Product не включает.


## Матрица блокировок


Пока для Product/ProductVariant открыт unresolved catalog conflict, в затронутом scope блокируются:


- изменение content assignments/overrides, зависящее от конфликтной parent relation;
- создание новых releases;
- создание и activation новых publication revisions;
- automatic promotion;
- изменение ContentEntitlementPolicy, если её Product/Variant scope стал неоднозначен.

Блокировка применяется к authoritative affected closure: Variant и только действительно зависимым операциям соседних Product/Variant. Несвязанный scope продолжает работу. Historical immutable artifacts сами по себе не являются move blocker; основание — конкретная несовместимость mutable/in-flight состояния: prospective patch collision, будущий publishing source не того owner либо неоднозначный policy scope. Отсутствующий в новой базе override target A не конфликт: retained A→B даёт B как local result. Независимая inherited B вместе с A→B создаёт duplicate и blocker.


Не блокируются:


- чтение каталога и существующего состояния;
- просмотр/разрешение конфликта;
- historical Delivery уже materialized/active revisions;
- уже начатые sessions, если отдельно не возник revoke/withdrawal.


Catalog conflict не является основанием автоматически отзывать ранее активную revision. Для запрета historical delivery используются отдельные причины: Withdrawal, grant revoke, license/key revoke.


## Поведение source snapshots


Source Catalog V2 snapshot full с positive int per-entity Version. Receiver durable принимает full newest candidate/watermark/work отдельно от apply: accepted v22 уже исключает v21 apply до processing/impact/parent assessment v22. ACK receive не applied. One newest candidate/single mutable owner; same Open episode pending заменяется без дублирующей payload authority.


Current/unresolved state и conflict history сохраняются; receipts/completed work имеют configurable retention без forever eventstore/all-old collision detection. Conflict assessment выполняется относительно:


- последнего applied CM state;
- newest известного source snapshot.


Устаревший pending snapshot после более нового не применяется.

Пока fresh assessment нового target не завершён, old issues не current diagnostic. Missing newest parent сохраняет Open+waiting; parent wake/crash recovery durable, Archived parent не missing. Initial whole Variant AppliedState absent, existing old AppliedState intact до successful apply.

Same EventId unchanged fact включая OccurredAt replay original; changed facts quarantined. New EventId equal-version сравнивает source IDs/version/parent/name/description/lifecycle без republish clock: identical no-op/no history rewrite, contradictory quarantine. Transport retry, normal reconciliation republish и pending resolver сохранённого candidate имеют разные identity rules по [Q23](../../technical/backend/questions/Q23-source-synchronization.md#принятый-протокол-каталога-v2).

## Technical ingestion fault и source correction

Trusted known entity/higher-version malformed или unsupported snapshot durable создаёт CatalogIngestionFault: scoped technical unreadiness блокирует stale apply/affected new writes, не F15/Withdrawal/revoke/global blocker. Cannot identify trusted entity/version — no fake entity block. Newer valid correction либо unsupported-profile fix + unchanged original message explicit replay восстанавливают обработку; менять payload под EventId нельзя.

Compatible source correction вызывает System SourceChangedApply: newest whole source + Resolved history atomic в CatalogAccess, без autoPublish/historical rewrite/rollback committed repair. При blockers тот же Open остаётся, при missing parent waiting. Historical materialized Delivery и confirmed rights остаются под независимыми F12/F13 guards, catalog faults не отзывают ключи.

Protected operational VRS reconciliation selected IDs/all current state включая parents сообщает scheduled/errors, не applied/readiness; absent requested ID не Delete. CM не получает source CRUD/force resolution. Catalog V2 не меняет License/period/security epoch algorithms.


## Разрешение конфликта


CM не получает право произвольно переписать каталог вопреки VR School. Ручное разрешение означает согласование локальных последствий CM с newest source state: специалист может исправить local assignments/overrides/publishing setup либо инициировать исправление исходного каталога в VR School.


После разрешения CM повторно валидирует затронутый scope против newest Version и только затем снимает блокировку.


Corrective repair и Resolve доступны уполномоченному специалисту, не только администратору. На каждой операции проверяются актуальные roles/scope/preconditions; F18 фиксирует самостоятельные подтверждённые outcomes. Точные permission names и wire остаются Q10/Q15. Source correction выполняет уполномоченный пользователь VR School, CM не создаёт альтернативный ProductId relation.


## Current-state gap


Current code, в котором ProductVariant.ProductId фактически immutable и move бросает исключение, не является target. Target MVP должен поддерживать move и переводить несовместимые локальные последствия в управляемый F15 conflict вместо hard failure transport consumer.


## Связанные документы


[F06](../features/F06-product-catalog.md) · [F15](../features/F15-catalog-conflicts.md) · [M03](../modules/M03-catalog.md) · [Q23](../../technical/backend/questions/Q23-source-synchronization.md) · [этапы](../../milestones/README.md).

## F15: технические границы разрешения

Связано с [Backend F15](../../technical/backend/features/F15-catalog-conflicts.md) и [Frontend F15](../../technical/frontend/features/F15-catalog-conflicts.md). Принятые authority/newest/blocked-scope/historical Delivery правила выше сохраняются. Отставание исходника не делает их новыми открытыми бизнес-вопросами.

**Принято:**

- Repair — отдельная corrective command Publishing для exact newest pending source. Ordinary Save остаётся заблокированным, `ignoreConflict`, force и скрытого privileged bypass нет.
- Начальный scope — working Variant patch: удалить конфликтный override/local addition, заменить проблемный result eligible exact version, изменить inherited-only exclusions для устранения конкретного blocker. Проверяются intrinsic validity, new-use eligibility и affected scope, без постороннего расширения. Release/revision/activation/history не редактируются. Иной blocker требует отдельно спроектированного corrective action либо source correction в VR School.
- Preview показывает current applied composition, prospective newest-parent composition и proposed repaired composition. Обычный workspace остаётся на applied relation до Resolve.
- Repair связан с `ConflictId`, newest `SourceVersion`, captured repair basis и own patch `If-Match`. Изменение source или patch до commit отвергает старую попытку. Parent берётся из authoritative pending, не из альтернативного `ProductId` request.
- Два отдельных commit: Publishing сохраняет repair; CatalogAccess Resolve заново читает newest source/dependencies и при совместимости атомарно применяет source и Resolved history. Успешный repair не равен успешному move; общей cross-BC SQL transaction нет.
- При v22 после repair-v21 patch сохраняется, v21 не применяется и автоматического rollback repair нет. Нужны refreshed preview/revalidation newest. Missing newest parent означает durable waiting, без fake parent, применения stale target или потери pending.
- Resolve/compatible System SourceChangedApply не публикуют автоматически. Active/history прежние; новый workspace требует самостоятельного publishing workflow. Auto promotion/late Approval recheck scope/basis/current intent по принятым F11/F17, cancelled/superseded не auto-resumes.

**Инженерная детализация:** exact CLR/method/URI/DTO/basis/error profiles Q15; mappings/retention knobs по принятому [Q23 catalog V2](../../technical/backend/questions/Q23-source-synchronization.md#принятый-протокол-каталога-v2). Detailed source models только backend F06. Общий MVP writer guard выбран [Q20](../../technical/backend/questions/Q20-persistence-and-migration.md#mvp-протокол-consistency-guard): fresh owning checks; repair и Resolve separate commits. Technical wait/fault не business CatalogConflict, unknown commit не rollback. Immutable history сама по себе не расширяет affected closure.

Resolution по-прежнему применяет authoritative newest source state только после совместимости. Source correction выполняется в VR School и поступает обычным новым snapshot; CM source-write command не вводится.
