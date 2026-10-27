# Q38. Массовая фиксация версий и подготовка выпуска

## Принятый результат

Пользователь согласовал массовую фиксацию SchemaVersion/EntityInstanceVersion и мастер «Подготовить выпуск» в «Каталог и публикации». Одиночный Publish остаётся доступен. В UI первая операция называется «Зафиксировать версию», чтобы отличаться от продуктового выпуска. Технические статусы Published, HTTP имена и неизменяемость сохраняются.

Нормативный сценарий: [F03](../features/F03-publish-schema-version.md), [F04](../features/F04-entity-instances.md), [F07 business](../features/F07-product-content-assignment.md#массовая-фиксация-и-подготовка-выпуска), [backend F07](../../technical/backend/features/F07-product-content-assignment.md#массовая-фиксация-и-подготовка-выпуска), [frontend F07](../../technical/frontend/features/F07-product-content-assignment.md#мастер-подготовить-выпуск). Это целевое поведение; описание не подтверждает наличие API, job storage или готового UI.

## Согласованные границы

1. Выбираются exact Draft IDs с own optimistic tokens и exact Published references; implicit latest запрещён.
2. Preflight показывает schema dependencies, ошибки по каждому выбранному элементу и будущий состав. Одна группа фиксации не выбирает автоматически между конфликтующими Draft одного baseline/target.
3. Подтверждение фиксирует проверенные входы. Изменившийся Draft, schema dependency, baseline или lifecycle требует нового preflight/подтверждения; Save не выполняет implicit Publish.
4. Сначала фиксируются необходимые схемы F03, затем экземпляры F04; все обычные guards сохраняются. Следующий продуктовый stage открывается только после успешного результата всего подтверждённого набора.
5. Затем exact результаты используются в F07/F08 assignment, F09 release и F10 revision. Moderation F17, activation F11 и Delivery-effective остаются отдельными шагами. Полное Publishing не добавляется в самостоятельный Demo.
6. Cross-BC all-or-nothing transaction не вводится. Успешно созданные immutable версии сохраняются при последующем отказе; partial results видимы. Частичная подготовка не активирует клиентский состав.
7. Неизвестный исход после потери связи сначала выясняется по exact state/результатам, затем пользователь подтверждает продолжение. Автоматический retry mutation без гарантий запрещён.
8. Ручное поле SortOrder удаляется из назначения/релиза/ревизии и UI. Стабильная canonical serialization, ReleaseNumber, activation Sequence и authored List order — разные механизмы.
9. Выбор назначения schema-first по [Q39](Q39-schema-content-selection.md): exact Published структура и AllExcept/SelectedOnly. Это не команда «опубликовать все Draft схемы». Draft для фиксации выбираются явно; после этой стадии release review разрешает AllExcept в конкретный Published состав и подтверждает его publication basis. Новое изменение universe после review требует повторной проверки, а не подстановки новых данных при Create без согласия.

## Открытая инженерная детализация

| Вопрос | Что уже определено | Что уточняется до реализации массового API |
| --- | --- | --- |
| Транспорт | Отдельные preflight/explicit execute, exact IDs/tokens, per-item result | URI/method/body, generated DTO CLR types, problem codes, result envelope; не выводятся из одиночных routes |
| Долгое выполнение | Stage gates, partial results, безопасное выяснение неизвестного исхода | Sync/async execution, job/operation identity, durable checkpoint, retention, cancel/continuation/idempotency protocol |
| Проверенный basis | Зафиксировать точный выбор и dependencies; повторить guards | Opaque preflight token, срок/состав binding, серверное хранение либо signed representation |
| Границы партии | Все явно выбранные входы и известные dependencies | Configurable limits, representative sizing; не назначать числа по догадке |
| Product/Variant wizard | Существующие exact assignment/release/revision правила без обхода moderation | Финальные DTO/команды orchestration и текущий состав создаваемых Product/Variant artifacts; пользователь явно подтверждает source releases |

Открытые механизмы не отменяют принятый бизнесовый результат и не позволяют объявлять workflow реализованным. Проверки существующих единичных команд не доказывают recovery/continuation массового выполнения.
