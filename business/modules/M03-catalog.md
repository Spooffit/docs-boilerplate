# M03. Каталог продуктов

## Назначение и целостный результат

Каталог задаёт идентичность получателей контента: Product и конкретный ProductVariant. VR School создаёт/редактирует записи, CM получает события и показывает обработанные сведения. Один вариант общий для разных организаций и лицензий; только вариант задаёт локальные отличия состава.

Новый каталог вводится параллельно со старыми Project/Application и постепенно заполняется работниками вручную. Project Storage пока работает со старой моделью. Автоматическое сопоставление legacy не требуется.

## Карта фич

| ID | Роль |
| --- | --- |
| [F06](../features/F06-product-catalog.md) | Source identity, Active/Archived lifecycle, выбор получателя и ограниченное сопровождение |
| [F15](../features/F15-catalog-conflicts.md) | Видимое ручное разрешение конфликтных изменений связей и ограничение затронутых операций |

## Совместная работа

Catalog V2 full snapshots имеют immutable source IDs и positive int per-entity SourceVersion с допустимыми gaps. Source-normalized Product Name 1–120, Variant Name 1–200, optional Description ≤500 сохраняются CM; duplicate names не identity. CM/Project Storage subscriptions независимы, licensing/period/security epoch algorithms не меняются. Detailed source models only backend F06.

Receiver durable acceptance full candidate/watermark/work предшествует separate apply: accepted v22 блокирует v21 до processing, ACK не applied/readiness. One newest candidate/single mutable owner; initial whole AppliedState отсутствует, existing AppliedState intact. Missing parent pending без TTL/fake Product, durable wake/crash recovery; Archived parent exists, не missing.

Бесконфликтный apply F06 обновляет сведения; несовместимость mutable/in-flight move dependencies переходит в F15. Historical artifacts сами по себе не blockers. Same Open episode получает newest target; old issues не current до assessment, newest missing parent сохраняет Open+waiting. Compatible source correction System SourceChangedApply атомарно apply+Resolved без publish/history rewrite. UI различает applied/newest lifecycle/processing/readiness.

Lifecycle Active/Archived задаётся только VR School полным более новым snapshot. Скрытие Archived из нового выбора — его отображение. Архив Product ограничивает новые операции Variants, сохраняя их собственный lifecycle. Разрешено ограниченное сопровождение без расширения состава; новые releases/revisions, обычная activation/rollback/promotion запрещены. Восстановление повторяет актуальные guards и ничего не публикует автоматически.

F15 блокирует только authoritative affected closure. Отдельный Publishing repair исправляет working Variant patch под newest source basis, затем CatalogAccess Resolve заново проверяет зависимости и атомарно применяет authoritative source state с history. Ordinary Save не обходит conflict. Выбранный MVP-протокол [Q20](../../technical/backend/questions/Q20-persistence-and-migration.md) защищает проверенные facts до каждого отдельного commit владельца. Durably accepted newer source запрещает apply прежнего pending, даже пока newest ещё обрабатывается; committed repair сохраняется. Technical failure не становится предметным conflict или пустым impact, lost commit confirmation требует выяснения неизвестного исхода.

Publishing использует applied identity для назначений; Delivery отдельно проверяет лицензионные права и выбирает публикацию. Archive/Conflict не выполняют Unpublish/Withdrawal/revoke и не запрещают выдачу materialized revisions по existing confirmed entitlement, включая новые sessions. Эти ограничения и safety actions имеют отдельные основания и permissions.

Known entity/higher malformed или unsupported snapshot создаёт durable scoped CatalogIngestionFault, блокирующий stale apply/affected new writes: не F15/global block/Withdrawal, unknown identity не fake blocker. Recovery newer valid version либо unsupported-profile fix + explicit unchanged original replay; same EventId payload не меняется. Historical rights/ключи не отзываются fault status автоматически.

Same EventId replay включает original OccurredAt, changed facts quarantined. New EventId/equal version сравнивает IDs/parent/name/description/lifecycle без republish clock, identical no-op/no history rewrite. Pending resolver сохранённого equal-watermark candidate отдельно от transport retry/reconciliation republish.

Protected operational VRS reconciliation selected IDs/all current incl parents сообщает scheduled/errors, не CM applied/readiness; missing ID не delete. Current/unresolved/conflict history сохраняются, receipts/completed work configurable retention со SourceVersion guard after cleanup. Нет bootstrap/global sequence/all-history replay/forever collision detection.

Модуль не редактирует исходный каталог в CM, не формирует персональный контент организации и не управляет запуском приложений. Неизменяемые ревизии остаются в Publishing; текущая связь не переписывает их задним числом.

## Этапы и вопросы

Demo не зависит от внешнего каталога VR School, broker и лицензий. Каталог и ручное разрешение конфликтов требуются для MVP; рабочее распределение находится в [milestones/README.md](../../milestones/README.md).

[Q10](../questions/Q10-permissions-and-isolation.md) · [Q11](../questions/Q11-catalog-ownership.md) · [Q25](../questions/Q25-catalog-conflicts.md) · [Q27](../questions/Q27-catalog-archiving.md).

Принятый [Q23 catalog V2 protocol](../../technical/backend/questions/Q23-source-synchronization.md#принятый-протокол-каталога-v2); exact typed profiles/physical handoff/operational route и permission/retry-retention settings — engineering, не новые business TBD/default numbers.

[Карта модулей](../../modules.md) · [Фичи](../../features.md).
