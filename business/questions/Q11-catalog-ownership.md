# Q11. Каталог и legacy


## Исходный вопрос


Где создаются Product/ProductVariant и как вводится новый каталог вместо Project/Application?


## Ответ


Product и ProductVariant создаются и редактируются **только в VR School**. Content Manager получает события из шины; самостоятельный CM CRUD или интерфейс CM с записью через VR School API не требуется.


Новая модель вводится как отдельная параллельная фича VR School для CM. Project Storage пока использует старые Project/Application. Работники постепенно переносят сведения вручную; обязательное автоматическое сопоставление и немедленная замена старого контура не планируются.


CM показывает received processing/newest source отдельно от AppliedState: receive ACK не доказывает applied/readiness. Отдельное подключение проекта к CM не вводится. Source IDs immutable/не переиспользуются; duplicate names не identity. Разные организации/licenses используют один Variant без персональных переопределений.

Catalog V2: Product Name 1–120, Variant Name 1–200, optional Description ≤500 символов; VR School нормализует до commit, CM значения сохраняет. Full snapshots с positive int per-entity SourceVersion допускают gaps без global sequence/bootstrap/all-history replay. Source commercial License/period/security-epoch алгоритмы не меняются, CM/Project Storage subscriptions независимы.

Receiver durably atomic принимает full candidate/watermark/work до separate apply. One newest candidate/single mutable owner; accepted v22 исключает old v21 apply до processing v22. Same EventId unchanged fact включая original OccurredAt replay original; changed fact quarantine. New EventId/same version сравнивает IDs/version/parent/name/description/lifecycle без republish EventId/clock: identical state no-op без history rewrite, contradictory state quarantine. Stale no-op/receipt cleanup не дают eternal old collision detection.

Missing parent даёт durable pending без TTL/fake Product; whole initial Variant AppliedState absent, prior whole AppliedState intact. Parent wake/crash recovery durable, Archived parent не missing. Trusted known higher malformed/unsupported source создаёт scoped CatalogIngestionFault, не F15/Withdrawal/global ban; unidentified input не fake entity blocker.


Переименование ProductVersion в ProductVariant внутри CM не является миграцией legacy Project/Application. При таком переименовании существующая идентичность CM сохраняется.


## Что уточняется отдельно


Принятый protocol передачи/fault/reconciliation: [Q23 catalog V2](../../technical/backend/questions/Q23-source-synchronization.md#принятый-протокол-каталога-v2). Exact typed/wire/physical mappings и retention/retry settings engineering; detailed source models только backend F06. F15 manual repair/Resolve и compatible source correction auto-close: [Q25](Q25-catalog-conflicts.md). Lifecycle owner: [Q27](Q27-catalog-archiving.md).


F06/external catalog обязательны только для MVP; Demo не зависит от VR School/broker/licenses или проверки этой модели.


## Применение ответа


[F06](../features/F06-product-catalog.md), [F15](../features/F15-catalog-conflicts.md), [M03](../modules/M03-catalog.md), [назначение продукта](../product.md) и [этапы](../../milestones/README.md). Не продолжать задавать закрытый вопрос о месте создания каталога.


## Source identity, lifecycle и recovery


- `ProductId` и `ProductVariantId` неизменяемы и никогда не переиспользуются.
- CM сохраняет current/unresolved/conflict history и source identities. Нормализованный lifecycle только Active/Archived, без local Delete/Archive/Restore/Hidden/soft-delete source authority.
- Перенос ProductVariant между Product поддерживается в MVP, но не входит в Demo.
- При переносе локальные items/assignments Variant сохраняются. Если новая родительская база делает их несовместимыми или создаёт конфликт зависимостей, изменение проходит через F15 и требует решения специалиста.
- Лицензионный grant по умолчанию authority-ориентирован на конкретный `ProductVariantId`. Product-level commercial grant не должен неявно распространяться на будущие variants; такая семантика допустима только как отдельная явная policy VR School.
- CM остаётся read-only consumer каталога; source of truth — VR School.

Protected operational reconciliation в VR School selected IDs/all current state включая parents повторно публикует snapshots и сообщает scheduled/errors, не CM applied/readiness. Missing requested ID не Delete. Current/unfinished/history сохраняются, receipts/completed work retention configurable при продолжающемся SourceVersion guard, не forever event store.

Fault recovery использует newer corrected version либо unchanged original message explicit replay после unsupported-profile fix. Payload под EventId не меняется. Historical materialized Delivery/current confirmed rights не отзываются fault/Archive/Conflict сами по себе; F12/F13 restrictions независимы.
