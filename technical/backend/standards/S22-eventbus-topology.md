# S22. MassTransit / RabbitMQ EventBus topology

## Назначение

Стандарт задаёт структуру EventBus-интеграции, направление зависимостей и правила RabbitMQ topology для MassTransit. Бизнес-смысл сообщений, их payload и сценарии определяются соответствующими F/Q-документами.

## Структура проектов

EventBus размещается в `Integration/EventBus` и состоит из трёх проектов:

```text
Integration/EventBus/
├── {SolutionName}.Integration.EventBus.Contracts/
├── {SolutionName}.Integration.EventBus.RabbitMq/
└── {SolutionName}.Integration.EventBus.Default/
```

Та же иерархия должна быть отражена в solution folders.

Направление зависимостей:

```text
Contracts
   ▲
   │
RabbitMq
   ▲
   │
Default
```

Правила:

- `Contracts` не зависит от `RabbitMq` и `Default`;
- `RabbitMq` зависит только от `Contracts` и transport-пакетов;
- `Default` композирует `Contracts` + `RabbitMq`;
- граф зависимостей ацикличен;
- consumer/application projects не дублируют topology.

## Contracts

`Contracts` содержит только сериализуемые integration contracts:

- integration events;
- commands/requests/responses, если они действительно передаются через EventBus;
- contract DTO, enum и простые contract value models.

Запрещены:

- RabbitMQ exchange/queue/binding configuration;
- consumers;
- application/domain services;
- persistence;
- business logic;
- transport-specific RabbitMQ types.

Допустим `MassTransit.Abstractions`, если он нужен самим контрактам. `MassTransit.RabbitMQ` и `RabbitMQ.Client` в `Contracts` не подключаются.

Domain entities bounded context не публикуются как integration DTO напрямую.

## RabbitMq

`RabbitMq` владеет transport-specific topology:

- стабильными exchange names;
- `SetEntityName`;
- send/publish topology;
- correlation conventions;
- exchange type;
- durability/auto-delete;
- общими RabbitMQ conventions интеграции.

Бизнес-логика и message contracts здесь не дублируются.

## Default

`Default` является стандартной composition dependency для consumer/producer приложения и ссылается на `Contracts` и `RabbitMq`.

Если дополнительный composition code не нужен, проект может состоять только из project references. `Default` не содержит альтернативную topology или копии contract/constants.

## Exchange names

Exchange names задаются централизованно в `RabbitMq`, а не повторяются строковыми литералами.

Рекомендуемый формат:

```text
msg.{integration}.{purpose}
```

Требования:

- lowercase;
- сегменты разделены `.`;
- имя стабильно после появления внешних потребителей;
- CLR rename сам по себе не меняет broker name;
- изменение существующего broker name считается integration-contract migration.

Пример констант:

```csharp
public static class CatalogMessageExchanges
{
    public const string ProductChanged = "msg.catalog.product-changed";
    public const string ProductVariantChanged = "msg.catalog.product-variant-changed";
}
```

## Topology registrar

Topology регистрируется централизованно классом вида:

```text
{ContextName}TopologyRegistrar
```

Registrar находится в `RabbitMq` и принимает `IRabbitMqBusFactoryConfigurator`.

Для каждого сообщения с явной topology должны быть явно определены:

- entity name;
- correlation mapping, если contract содержит correlation identifier;
- publish exchange type;
- `Durable`;
- `AutoDelete`.

Пример формы:

```csharp
cfg.Message<ProductChanged>(x =>
    x.SetEntityName(CatalogMessageExchanges.ProductChanged));

cfg.Publish<ProductChanged>(x =>
{
    x.ExchangeType = ExchangeType.Fanout;
    x.Durable = true;
    x.AutoDelete = false;
});
```

Если contract имеет correlation property, она маппится в MassTransit `CorrelationId` централизованно. Registrar не генерирует новый correlation ID вместо значения контракта.

Другой exchange type допускается только как явное решение интеграции.

## Receive endpoints и подписчики

Exchange topology не означает одну общую очередь для всех приложений.

Независимые приложения/сервисы должны получать сообщения через независимые receive endpoints/queues, если competing-consumer semantics не является осознанным требованием.

Consumer endpoint configuration принадлежит consuming application/composition root; central `RabbitMq` project задаёт общую message/exchange topology и не содержит consumers.

Имена queues, retry/redelivery, DLQ/error queues и routing rules задаются отдельным topology/application standard либо конкретной интеграцией. Их нельзя выдумывать по аналогии с чужим сервисом.

## Надёжность и повторы

Для операции `DB change + publish event` должен использоваться согласованный repository outbox-механизм. Если в solution уже есть стандартный outbox, новый параллельный механизм не создаётся.

Consumer должен безопасно переживать at-least-once delivery. Inbox/deduplication применяется там, где повтор сообщения способен дать повторный эффект.

EventBus не считается exactly-once транспортом. Успешная запись outbox, доставка broker, consumer receive и consumer commit являются разными этапами.

Порядок/staleness определяется contract semantics конкретной интеграции, а не временем доставки RabbitMQ.

## Изменение contract/topology

Следующие изменения считаются integration-contract changes и не выполняются как случайный refactoring:

- rename/remove message type или serialized property;
- изменение типа serialized property;
- rename exchange;
- изменение exchange type;
- изменение correlation semantics;
- изменение durability/auto-delete;
- перенос contract identity, влияющий на MassTransit serialization/topology.

Versioning integration contracts обязателен; конкретная package/versioning convention определяется отдельным стандартом проекта.

## Проверка изменения

Перед завершением проверить:

- projects находятся в `Integration/EventBus`;
- dependency direction соблюдён;
- contracts не зависят от RabbitMQ implementation;
- exchange names определены централизованно;
- `SetEntityName` и publish topology явные;
- correlation mapping согласован с contract;
- consumer не дублирует central topology;
- независимые приложения не используют одну queue случайно;
- outbox/inbox и repeat semantics определены там, где нужны;
- существующие broker names не изменены неявно;
- `.csproj` соответствуют S23.
