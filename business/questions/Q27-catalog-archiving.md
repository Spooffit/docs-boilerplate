# Q27. Скрытие и архивирование каталога

## Принято

Нормализованный lifecycle Product/ProductVariant в CM имеет два состояния: `Active` и `Archived`. Скрытие из выбора — следствие Archive, отдельного `Hidden` нет. `Pending` и `CatalogConflict` описывают обработку source state, а не lifecycle.

Каталожные Product/ProductVariant создаются и редактируются только в VR School. Поэтому интерфейс CM не получает произвольный редактор исходного каталога под названием «архивирование».

## Ограничения и восстановление

VR School — единственный источник lifecycle: Archive/Restore приходят полным snapshot с более новым `SourceVersion`. CM не предоставляет локальные Archive/Restore/toggle. Mapping фактических source-полей в этот нормализованный контракт остаётся в [Q23](../../technical/backend/questions/Q23-source-synchronization.md#f06-проекция-pending-и-конкурентная-обработка); существующий `IsDeleted` сам по себе не доказывает выбранную семантику.

Archived Product ограничивает новые операции его Variant, но не меняет собственный lifecycle Variant и ничего каскадно не удаляет. После Restore родителя снова eligible только Variant с собственным `Active` и без иных blockers.

Для working F07/F08 ограниченное maintenance определено [Q07](Q07-exclusions-and-local-content.md#f07-допустимость-рабочих-references): чтение, сохранение неизменных exact references, удаление проблемных references и их замена eligible result без расширения. Семантическое удаление через inherited-only exclusion или замена проблемной inherited позиции через override также maintenance; число строк patch не определяет расширение. Open F15 по-прежнему блокирует ordinary writes; отдельный corrective repair описан в [Q25](Q25-catalog-conflicts.md#f15-технические-границы-разрешения).

До Restore запрещены новые releases/revisions, обычная activation/rollback и automatic promotion. Archive не делает Unpublish/Withdrawal и не отзывает grants/licenses: materialized revisions и подтверждённое существующее entitlement остаются доступны F12/F13, включая новую session по действующему праву. Остановка выдачи требует самостоятельного Withdrawal/revoke; Unpublish сохраняет свою согласованную семантику. Safety actions Unpublish/Withdrawal доступны с обычными правами и preconditions. Restore не активирует revision, не снимает Withdrawal и не выдаёт entitlement автоматически.

UI отдельно показывает applied lifecycle, newest source lifecycle и processing/pending/conflict. Обычный selection содержит eligible объекты, exact workspace сохраняет объект, причины ограничений и допустимые действия; pending не изображается успешно применённым состоянием.

## Применение

[F06](../features/F06-product-catalog.md) · [F15](../features/F15-catalog-conflicts.md) · [M03](../modules/M03-catalog.md) · [Q12](Q12-media-and-retention.md) · [Q04](Q04-withdrawn-publications.md).

Lifecycle и ограничения выбраны; source mapping и механизмы координации остаются технической детализацией Q23/Q20. Локальная команда архивирования и автоматический отзыв grants не вводятся.

## F06: источник lifecycle и допустимость нового использования

Связано с [бизнес F06](../features/F06-product-catalog.md), [backend F06](../../technical/backend/features/F06-product-catalog.md), [frontend F06](../../technical/frontend/features/F06-product-catalog.md), [Q23](../../technical/backend/questions/Q23-source-synchronization.md#f06-проекция-pending-и-конкурентная-обработка), [Q25](Q25-catalog-conflicts.md). Source ownership не пересматривается: CM не предоставляет Delete/Archive/Restore, rename или изменение ProductVariant parent через UI/API.

Для полной technical модели требуется материализовать:

- Mapping VR School → `Active/Archived`, versioned wire profile и миграцию current `IsDeleted`; identity сохраняется.
- Derived eligibility из собственного/родительского lifecycle и scoped F15 blockers без второй локальной lifecycle authority.
- Fresh проверку этих фактов на affected commits с [выбранным MVP writer guard Q20](../../technical/backend/questions/Q20-persistence-and-migration.md#mvp-протокол-consistency-guard); physical adapters и writer coverage требуют проверки, простые prechecks не заменяют protocol.
- Read DTO для applied/newest lifecycle и processing, допустимых maintenance/safety actions; точный CLR/wire contract в Q15.

**Ответ:** бизнесовая lifecycle/eligibility policy и общий MVP writer coordination protocol закрыты. Открыты source mapping, wire/storage/adapters и numeric settings; эти детали не переоткрывают Archive/Restore или historical Delivery semantics.
