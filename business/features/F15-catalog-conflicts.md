# F15. Разрешение конфликтов каталога

## Цель

Не применять молча source Product/ProductVariant change, если новое состояние VR School несовместимо с локальным Content Manager state.

Conflict является управляемым pending state, а не permanent consumer failure и не правом CM переписать source catalog.

## Когда возникает conflict

Основной MVP-сценарий — ProductVariant move между Product, когда authoritative newest parent/base несовместим с конкретным mutable локальным состоянием либо незавершённой операцией CM. Проверка выявляет prospective patch collision, попытку будущего использования release неправильного owner, неоднозначный policy scope и другие конкретные affected dependencies.

Сам move допустим. Historical immutable releases/revisions и provenance сами по себе не являются blockers. Сохранённый override `A → B` при отсутствии A в prospective базе допустим по F08; независимый inherited B вместе с результатом `A → B` создаёт duplicate result, если B не подавлен другими допустимыми правилами. Проверяется фактический итоговый состав, а не совпадение IDs вне композиции.

## Conflict state

CM хранит:

- affected Product/ProductVariant;
- applied source state/version;
- reference/projection newest pending source snapshot/version из single mutable owner F06, без второй payload authority;
- reason/issues;
- authoritative affected closure и основание диагностики;
- opened/resolved metadata.

Если при Open conflict durably accepted более новый snapshot той же entity, pending target того же episode заменяется newest full candidate одного mutable owner. Пока fresh assessment не выполнен, прежние issues не являются актуальной диагностикой target. Специалист не обязан последовательно resolving промежуточные states.

Resolved conflict не удаляется из history, но исчезает из default unresolved queue.

Если newest snapshot требует ещё неизвестный parent, Open сохраняется вместе с durable waiting/newest target. Старый pending не применяется, duplicate conflict episode не создаётся. Parent wakeup/restart recovery durable по F06; Archived parent существует и не missing. Это processing state, отдельно от Active/Archived lifecycle и applied relation.

Durably accepted newer candidate уже запрещает применение прежнего pending target, даже если оценка newest impact или parent ещё не завершена. Техническая недоступность чтения/координации не считается предметным conflict и не означает отсутствие blockers.

CatalogIngestionFault F06 для trusted known entity/higher malformed или unsupported source — scoped technical unreadiness, не CatalogConflict и не reason снять publication/license/key. Fault отдельно запрещает stale apply/affected new writes; без known identity нельзя создать fake entity blocker. Исправление требует newer valid source либо explicit unchanged original-message replay после unsupported-profile fix, не редактирования payload под прежним EventId.

## Blocked scope

Пока conflict открыт, блокируется только authoritative affected closure: операции, чьи зависимости реально затронуты несовместимостью. Не вводится blanket blocker всех Product/Variant. Для этой closure блокируются:

- изменения Product/Variant content assignments;
- создание releases;
- создание новых PublicationRevision, обычная activation и rollback;
- automatic promotion;
- policy updates, если scope неоднозначен;
- новая submission на moderation, если revision нельзя достоверно валидировать.

Не блокируются:

- чтение catalog/current state;
- conflict resolution UI;
- отдельный corrective repair в пределах ниже;
- historical materialized Delivery;
- existing confirmed entitlement, включая новые sessions, если отдельно нет revoke/Withdrawal;
- safety actions с соответствующим permission/scope: Unpublish, explicit Withdrawal;
- unrelated операции за пределами affected closure.

Catalog conflict сам по себе не означает, что ранее активный клиентский состав запрещён.

## Resolution

Специалист с отдельным permission видит applied state, newest source state, affected dependencies и конкретные blockers. Разрешение не является admin-exclusive: роль сама по себе не заменяет permission и актуальный scope check.

Разрешение означает привести **локальное CM состояние** в совместимость с authoritative newest source snapshot либо инициировать исправление source данных в VR School.

CM не создаёт альтернативную parent relation вопреки VR School.

### Corrective repair в Publishing

Обычный Save assignment/patch остаётся заблокированным внутри affected closure, включая удаление/замену. Для устранения blocker предусмотрена отдельная Publishing corrective repair command, а не исключение Save, `ignoreConflict`, force или hidden privileged bypass. Repair относится только к exact newest pending target конкретного conflict.

Начальный scope — working Variant patch. Разрешены:

- удаление конфликтующего override или local addition;
- замена проблемного exact result на eligible result;
- inherited-only exclusions, убирающие blocker.

Repair должен устранять конкретный blocker без расширения контентного состава. Intrinsic patch invariants, new-use eligibility, authoritative scope и другие guards обязательны. Другие виды blockers требуют отдельно спроектированного corrective действия либо correction в VR School; команда не получает произвольный редактор Product assignment, policy или source relation.

Preview показывает три основания: текущую applied composition, prospective composition для newest parent и результат предлагаемого repaired patch. Обычный workspace использует applied parent до успешного Resolve; перспективный preview не делает pending relation рабочей.

Repair привязан к `ConflictId`, exact newest `SourceVersion`, captured repair basis и собственному patch `If-Match`. Basis включает применимые зависимости/контекст prospective проверки. Если source, patch или требуемое основание изменились, запрос отклоняется и нужен свежий preview. Пользователь не задаёт альтернативный ProductId. Permission и scope проверяются заново при исполнении; F18 фиксирует repair и его основание.

### Resolve в CatalogAccess

Это две отдельные фиксации в разных BC: сначала Publishing сохраняет допустимый repaired working patch; затем CatalogAccess выполняет Resolve. Repair не закрывает conflict и не применяет source relation. Если второй шаг не завершился, repair остаётся сохранённым, conflict — открытым.

Перед фиксацией Resolve сервер:

1. повторно проверяет permission и authoritative scope;
2. перечитывает exact newest source snapshot и его parent;
3. перечитывает affected dependencies, включая repaired patch, и проверяет их актуальную совместимость;
4. атомарно в CatalogAccess применяет authoritative source state, снимает blocker и сохраняет Resolved history.

F18 фиксирует Resolve, actor, применённую source version и проверенное основание; общий audit не заменяет предметную conflict history.

Если source изменился во время работы, stale resolution отклоняется. Если после repair для v21 пришёл v22, сохранённый repair не откатывается, v21 не применяется; Resolve заново оценивает v22. Missing newest parent оставляет durable waiting state, а не даёт применить v21 или считать отсутствие parent пустой базой.

MVP использует выбранный в [Q20](../../technical/backend/questions/Q20-persistence-and-migration.md) общий writer protocol: каждый шаг заново получает authoritative facts и защищает их от конкурентных affected writes/source changes до своей фиксации. Repair и Resolve остаются двумя отдельными commit своих BC; ни preview, ни две предварительные проверки не являются разрешением на последующую запись. Ограниченное ожидание, отмена и недоступность проверки обрабатываются как технический отказ, без ложного successful apply или нового предметного conflict.

Потерянное подтверждение записи означает неизвестный исход, а не доказанный rollback. Сохраняются отправленный target/basis и пользовательская correction; результат выясняется по согласованному recovery contract [Q15](../../technical/backend/questions/Q15-api-and-source-code.md), без слепого повтора с новым token. Подтверждённый repair не является подтверждением Resolve.

Already-started automatic promotion и late Approval повторно проверяют current affected scope и captured basis/current intent по F11/F17 перед новым commit. Resolve/auto-close не resurrect Cancelled/Superseded item и не дают старой операции безусловного права продолжить publication.

### Compatible source correction

Более новый compatible source snapshot не требует fake manual Resolve: System SourceChangedApply атомарно применяет newest full source и сохраняет Resolved history. Old AppliedState остаётся intact до commit, processing/diagnostics не обещают readiness до fresh assessment. Auto-close не Publish/Activate, не переписывает immutable history и не откатывает ранее committed Publishing repair. При remaining blockers тот же Open episode сохраняется; missing newest parent остаётся Open/waiting.

## Historical publication

Resolution не переписывает старые Releases/PublicationRevisions и не пересчитывает их composition.

Resolve не публикует/не активирует контент и не переписывает историю. Новая публикация после move создаётся обычным Publishing workflow со свежими guards, включая lifecycle F06.

Historical Delivery прекращается только отдельными основаниями: Withdrawal, grant/license/key revoke и другими entitlement rules.

## Demo / MVP

F15 обязателен для MVP. Demo external catalog integration не включает и потому не требует conflict workflow.

## Acceptance

- conflict snapshot не применяется молча;
- затронутые writes блокируются, unrelated scope работает;
- historical Delivery не останавливается только из-за conflict;
- newest source snapshot supersedes older pending target;
- same Open episode сохраняется; old issues помечены не current до assessment, newest missing parent сохраняет Open+waiting;
- stale resolution не закрывает conflict;
- accepted newest candidate запрещает stale apply до завершения его impact/parent processing;
- repair и Resolve защищают свои актуальные основания до каждой отдельной фиксации; technical failure не означает compatible state;
- historical immutable artifacts сами по себе не блокируют move; absent A в сохранённом A→B допустим, независимый prospective B выявляет фактическую коллизию;
- ordinary affected Save остаётся запрещён; отдельный repair не расширяет состав и повторяет intrinsic/new-use/scope guards;
- repair preview различает applied/prospective/repaired; stale SourceVersion/basis/patch If-Match отклоняется;
- repair и Resolve — две фиксации; repair сохраняется при v22, v21 не применяется и newest missing parent остаётся waiting;
- permission/scope проверяются заново, repair/Resolve доступны специалисту с permission и попадают в F18;
- Resolve не выполняет autopublish; затронутые promotion/late Approval требуют повторной проверки;
- compatible source correction System SourceChangedApply атомарно apply+Resolved, без publish/history rewrite/rollback repair;
- scoped ingestion fault не F15 business conflict/Withdrawal/global ban, unknown identity не fake blocker; historical rights отдельно F12/F13;
- successful resolution применяет authoritative source state и сохраняет history.

## Связи

[F06](F06-product-catalog.md) · [F08](F08-variant-content-assignment.md) · [F11](F11-publication-lifecycle.md) · [F17](F17-content-review.md) · [F18](F18-staff-activity-history.md) · [Q25](../questions/Q25-catalog-conflicts.md) · [Backend](../../technical/backend/features/F15-catalog-conflicts.md) · [Frontend](../../technical/frontend/features/F15-catalog-conflicts.md)

Source acceptance/identity/fault/reconciliation protocol: [Q23](../../technical/backend/questions/Q23-source-synchronization.md#принятый-протокол-каталога-v2). Detailed source models только backend F06; F15 не создаёт второй snapshot authority.
