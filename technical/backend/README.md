# Бэкенд

## Стек и граница раздела

Направление серверной части: .NET 10, библиотеки LeanSoft, DDD/Clean Architecture, Entity Framework Core и RabbitMQ через MassTransit. Внешний контур включает VR School UI (Angular), VR School Host (.NET 7, ABP / ASP.NET Zero) и Project Storage (.NET 7). Host передаёт существующие события о лицензиях и проектах в Project Storage, который предоставляет проекты Unity-лаунчеру. Состав, разделение с динамическим контентом и риск неподдерживаемого .NET 7 раскрыты в [архитектуре](architecture.md). Точные пакеты, mapping и конфигурации требуют актуального solution; это не утверждение о выполненном развёртывании.

## Структура

| Место | Назначение |
| --- | --- |
| [Архитектура стороны](architecture.md) | Общие границы, ownership и взаимодействия BC. |
| [Целевая структура solution](target-solution-structure.md) | Конкретные проекты, зависимости, composition root, PostgreSQL schemas и правила cross-BC references. |
| [План реализации](implementation-plan.md) | Последовательность рефакторинга и реализации Demo → MVP. |
| `features/` | Подробная реализация: модели, commands/queries, потоки данных, слои, API, хранение, ошибки и тестовая стратегия. |
| `modules/` | Общий результат связанного набора функций и карта их спецификаций. |
| `questions/` | Отдельные вопросы этой стороны; полный текст не дублируется в модуле. |
| `standards/` | Нумерованные общие стандарты разработки без специфики проекта. |
| `adr/` | Зафиксированный выбор и его последствия. |

## Как читать серверную фичу

Backend feature является нормативной implementation-spec текущего target, а не журналом решений. Она описывается по физическим слоям solution: `Domain` → `Application.Abstractions` → `Application` → `Api` → `EntityFramework` → `EntityFramework.Persistence` → внешние adapters (`Infrastructure`/`Integration`/S3/HTTP при наличии), затем data flows, ошибки, тесты и порядок реализации. Для каждого слоя указываются конкретные модели, поля, методы, handlers, ports, mappings и ограничения. История выбора и заменённые варианты остаются в Q/ADR, а не дописываются в feature секциями «решение блока N».

HTTP-профиль: [S16](standards/S16-rest.md). Оформление потоков: [S17](standards/S17-data-flows.md). Классификация проверок: [S04](standards/S04-testing.md). Валидация: [S18](standards/S18-fluent-validation.md). Фильтрация: [S19](standards/S19-specifications.md). Размещение обработчиков: [S20](standards/S20-cqrs.md). Форма нового документа: [шаблон backend-фичи](../../templates/backend-feature.md).

Незаполненные адреса, DTO и параметры не считаются выданным контрактом. Подробное описание известных алгоритмов не отменяет Q-документы и не подтверждает готовность кода.

## Навигация

[Фичи](../../features.md) · [Модули](../../modules.md) · [Целевая структура solution](target-solution-structure.md) · [План реализации](implementation-plan.md) · [Вопросы](../../questions.md) · [Стандарты](../../standards.md).

Для работы агента действуют [инструкции стороны](AGENTS.md) и общие правила репозитория. Спецификация этой стороны не переписывает бизнесовое правило, а объясняет собственную обязанность при его выполнении.
