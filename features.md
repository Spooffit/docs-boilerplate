# Каталог фич

F-ID обозначает одну функцию во всех разделах. Таблица связывает её описания, но не задаёт порядок реализации и не показывает статусы задач.

## Карта документов

| ID | Бизнес | Бэкенд | Фронтенд |
| --- | --- | --- | --- |
| F01 | [Создание схемы сущности](business/features/F01-create-entity-schema.md) | [Серверная спецификация](technical/backend/features/F01-create-entity-schema.md) | [Поведение интерфейса](technical/frontend/features/F01-create-entity-schema.md) |
| F02 | [Создание версии схемы](business/features/F02-create-schema-version.md) | [Серверная спецификация](technical/backend/features/F02-create-schema-version.md) | [Поведение интерфейса](technical/frontend/features/F02-create-schema-version.md) |
| F03 | [Публикация версии схемы](business/features/F03-publish-schema-version.md) | [Серверная спецификация](technical/backend/features/F03-publish-schema-version.md) | [Поведение интерфейса](technical/frontend/features/F03-publish-schema-version.md) |
| F04 | [Экземпляры и версии данных](business/features/F04-entity-instances.md) | [Серверная спецификация](technical/backend/features/F04-entity-instances.md) | [Поведение интерфейса](technical/frontend/features/F04-entity-instances.md) |
| F05 | [Медиа и ссылки на ресурсы](business/features/F05-media-assets.md) | [Серверная спецификация](technical/backend/features/F05-media-assets.md) | [Поведение интерфейса](technical/frontend/features/F05-media-assets.md) |
| F06 | [Продукты и конкретные варианты](business/features/F06-product-catalog.md) | [Серверная спецификация](technical/backend/features/F06-product-catalog.md) | [Поведение интерфейса](technical/frontend/features/F06-product-catalog.md) |
| F07 | [Общий контент продукта](business/features/F07-product-content-assignment.md) | [Серверная спецификация](technical/backend/features/F07-product-content-assignment.md) | [Поведение интерфейса](technical/frontend/features/F07-product-content-assignment.md) |
| F08 | [Отличия конкретного варианта](business/features/F08-variant-content-assignment.md) | [Серверная спецификация](technical/backend/features/F08-variant-content-assignment.md) | [Поведение интерфейса](technical/frontend/features/F08-variant-content-assignment.md) |
| F09 | [Релизы назначенного контента](business/features/F09-content-releases.md) | [Серверная спецификация](technical/backend/features/F09-content-releases.md) | [Поведение интерфейса](technical/frontend/features/F09-content-releases.md) |
| F10 | [Итоговая ревизия публикации](business/features/F10-publication-revisions.md) | [Серверная спецификация](technical/backend/features/F10-publication-revisions.md) | [Поведение интерфейса](technical/frontend/features/F10-publication-revisions.md) |
| F11 | [Активация, история, откат и обновление вариантов](business/features/F11-publication-lifecycle.md) | [Серверная спецификация](technical/backend/features/F11-publication-lifecycle.md) | [Поведение интерфейса](technical/frontend/features/F11-publication-lifecycle.md) |
| F12 | [Политика права на контент](business/features/F12-content-entitlement-policy.md) | [Серверная спецификация](technical/backend/features/F12-content-entitlement-policy.md) | [Поведение интерфейса](technical/frontend/features/F12-content-entitlement-policy.md) |
| F13 | [Сессия, манифест и загрузка](business/features/F13-content-delivery.md) | [Серверная спецификация](technical/backend/features/F13-content-delivery.md) | [Поведение интерфейса](technical/frontend/features/F13-content-delivery.md) |
| F14 | [Совместимость клиента: расширение отложено](business/features/F14-client-compatibility.md) | [Принятый минимум по слоям](technical/backend/features/F14-client-compatibility.md) | [Контекст версий и клиентские экраны](technical/frontend/features/F14-client-compatibility.md) |
| F15 | [Ручное разрешение конфликтов каталога](business/features/F15-catalog-conflicts.md) | [Серверная спецификация](technical/backend/features/F15-catalog-conflicts.md) | [Поведение интерфейса](technical/frontend/features/F15-catalog-conflicts.md) |
| F16 | [Вход и доступ сотрудников](business/features/F16-staff-access.md) | [Серверная спецификация](technical/backend/features/F16-staff-access.md) | [Поведение интерфейса](technical/frontend/features/F16-staff-access.md) |
| F17 | [Проверка контента администратором](business/features/F17-content-review.md) | [Серверная спецификация](technical/backend/features/F17-content-review.md) | [Поведение интерфейса](technical/frontend/features/F17-content-review.md) |
| F18 | [История действий сотрудников](business/features/F18-staff-activity-history.md) | [Серверная спецификация](technical/backend/features/F18-staff-activity-history.md) | [Поведение интерфейса](technical/frontend/features/F18-staff-activity-history.md) |
| F19 | [Unity-библиотека клиента CM](business/features/F19-unity-client.md) | Общий HTTP-контракт в [F13](technical/backend/features/F13-content-delivery.md); отдельная серверная копия не требуется | [Клиентская спецификация](technical/frontend/features/F19-unity-client.md) |
| F20 | [Демонстрационная сцена Unity](business/features/F20-unity-demo-scene.md) | Использует F13 через F19; отдельной серверной реализации нет | [Клиентская спецификация](technical/frontend/features/F20-unity-demo-scene.md) |
| F21 | [Schema Registry: внешний контракт схем](business/features/F21-schema-registry.md) | [Серверная спецификация](technical/backend/features/F21-schema-registry.md) | Используется генерацией F22; отдельный UI не требуется |
| F22 | [Генерация C#-моделей выбранных схем](business/features/F22-csharp-model-generation.md) | [Серверная спецификация](technical/backend/features/F22-csharp-model-generation.md) | [Выбор и скачивание моделей](technical/frontend/features/F22-csharp-model-generation.md) |
| F23 | [Необязательная интеграция Unity Addressables](business/features/F23-unity-addressables.md) | [Применимость server-слоёв](technical/backend/modules/M05-delivery.md#f23-граница-optional-addressables); AssetId/HTTP-контракт сохраняется | [Клиентская спецификация](technical/frontend/features/F23-unity-addressables.md) |
