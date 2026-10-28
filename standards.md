# Каталог стандартов


Стандарты содержат только общие соглашения разработки, независимые от проекта. Проектная специфика запрещена также в примерах, примечаниях и ссылках. Конкретные сценарии, применение соглашения и проектные проверки находятся в фичах, модулях, вопросах и ADR, а не внутри стандартов.


Область применения может быть технологической: .NET, ASP.NET Core, PostgreSQL, RabbitMQ. Обязательное правило, рекомендация и иллюстративный пример различаются. Номер не является приоритетом.


## Карта документов


| ID | Бэкенд | Фронтенд |
| --- | --- | --- |
| S01 | [.NET conventions, migrated reusable profile](catalog/docs/standards/dotnet/conventions.md) | — |
| S02 | [Хранение и целостность](catalog/base/docs/standards/stateful-workflows.md#durable-snapshots-and-retained-history) | — |
| S03 | [Валидация и ошибки](catalog/base/docs/standards/data-flows.md#input-trust-and-failure-semantics) | — |
| S04 | [Общая модель тестирования](technical/backend/standards/S04-testing.md) | — |
| S05 | — | [Интерфейсы и формы](technical/frontend/standards/S05-ui-and-forms.md) |
| S06 | — | [Работа с API](technical/frontend/standards/S06-api-client.md) |
| S07 | — | [Доступность интерфейса](technical/frontend/standards/S07-accessibility.md) |
| S08 | — | [Тестирование клиента: Blazor/bUnit, FSD и браузерные проверки](technical/frontend/standards/S08-testing.md) |
| S09 | [Архитектурные тесты: базовый .NET-стандарт](catalog/docs/standards/dotnet/testing.md) | — |
| S10 | [Unit tests](catalog/docs/standards/dotnet/testing.md#unit-tests) | — |
| S11 | [Integration tests](catalog/docs/standards/dotnet/testing.md#integration-tests) | — |
| S12 | [Functional tests](catalog/docs/standards/dotnet/testing.md#functional-tests) | — |
| S13 | [E2E: .NET-стандарт и опциональный Aspire](catalog/docs/standards/dotnet/testing.md) | — |
| S14 | [Окружения, изоляция и параллельность](catalog/docs/standards/dotnet/testing.md#test-environment-ownership-and-bounded-parallelism) | — |
| S15 | [Test engineering и проверка изменений](catalog/docs/standards/dotnet/testing.md#shared-testing-infrastructure-and-safe-migration) | — |
| S16 | [REST: реализация HTTP API](technical/backend/standards/S16-rest.md) | — |
| S17 | [Потоки данных и транспортные взаимодействия](technical/backend/standards/S17-data-flows.md) | — |
| S18 | [FluentValidation и композиция правил](catalog/docs/standards/dotnet/fluentvalidation.md) | — |
| S19 | [Фильтрация: Ardalis.Specification](catalog/docs/standards/dotnet/ardalis-specification.md) | — |
| S20 | [CQRS: размещение обработчиков](catalog/docs/standards/architecture/cqrs.md#handler-responsibilities-and-dependency-direction) | — |
| S21 | [Ограничения агрегатов: единый источник в Domain](technical/backend/standards/S21-aggregate-constraints.md) | — |
| S22 | [MassTransit / RabbitMQ EventBus topology](technical/backend/standards/S22-eventbus-topology.md) | — |
| S23 | [.NET project files, migrated reusable profile](catalog/docs/standards/dotnet/project-files.md) | — |


[Фичи](features.md) · [Модули](modules.md) · [Вопросы](questions.md) · [Правила ведения](CONTRIBUTING.md).
