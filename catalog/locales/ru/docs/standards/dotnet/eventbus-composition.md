---
title: "Необязательная композиция EventBus из трёх проектов MassTransit/RabbitMQ"
description: "Проверяемая сборкой структура Contracts/RabbitMq/Default и маршрутизация topic в RabbitMQ"
kind: standard
status: active
---

# Необязательная композиция EventBus из трёх проектов MassTransit/RabbitMQ

**Применяется, когда:** явно выбран профиль `masstransit-eventbus-composition` наряду с общими [правилами MassTransit/RabbitMQ](masstransit-rabbitmq.md). Базовый `masstransit-rabbitmq` **не требует** такой организации.

## Физические проекты и папки решения

Используйте три библиотечных проекта в физическом каталоге `Integration/EventBus/` и соответствующей *папке решения*. Четвёртый проект `App` ниже — временное приложение для тестирования, **не четвёртая общая библиотека EventBus**, он не генерируется как приложение в репозитории документации.

```text
Integration/
  EventBus/
    Contracts/Contracts.csproj
    RabbitMq/RabbitMq.csproj
    Default/Default.csproj
App/ (только проверочный host, не часть общей библиотеки)
```

Contracts не должен знать о MassTransit, RabbitMQ, consumers и доменных сущностях приложения. RabbitMq ссылается на Contracts, настраивает publish topology и транспортные соглашения. Default ссылается на Contracts и RabbitMq, предоставляет регистрацию сервисов и оставляет владение consumer endpoints приложению-потребителю. Направление: `Contracts <- RabbitMq <- Default`. Не добавляйте эти проекты, пока вариант явно не принят.

## Минимальная справочная композиция

Следующие файлы внутри fenced code blocks образуют **универсальную техническую тестовую фикстуру**, а не приложение или требование продукта. `tests/optional_profile_contracts.py` восстанавливает ссылки проектов и C# в временной директории и компилирует/запускает пример против изолированного брокера в Linux CI. В примере закреплён `MassTransit.RabbitMQ 8.5.12` для воспроизводимости; применяющий проект должен выбрать и проверить поддерживаемую версию. Учётные данные `ci`/`ci-pass` принадлежат одноразовому CI, **не подходят для production**. Исполняемые исходники сохранены без перевода идентификаторов.

```xml file=Integration/EventBus/Contracts/Contracts.csproj
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

```csharp file=Integration/EventBus/Contracts/ExampleEvent.cs
namespace DocsExample.Integration.EventBus.Contracts;

public sealed record ExampleEvent(Guid EventId);
```

```xml file=Integration/EventBus/RabbitMq/RabbitMq.csproj
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../Contracts/Contracts.csproj" />
    <PackageReference Include="MassTransit.RabbitMQ" Version="8.5.12" />
  </ItemGroup>
</Project>
```

```csharp file=Integration/EventBus/RabbitMq/EventBusTopology.cs
using DocsExample.Integration.EventBus.Contracts;
using MassTransit;

namespace DocsExample.Integration.EventBus.RabbitMq;

public static class EventBusTopology
{
    public const string ExampleExchange = "examples.event";

    public static void Configure(IRabbitMqBusFactoryConfigurator bus)
    {
        bus.Message<ExampleEvent>(x => x.SetEntityName(ExampleExchange));
        bus.Publish<ExampleEvent>(x => x.ExchangeType = "topic");
        bus.Send<ExampleEvent>(x => x.UseRoutingKeyFormatter(_ => "examples.created"));
    }
}
```

```xml file=Integration/EventBus/Default/Default.csproj
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../Contracts/Contracts.csproj" />
    <ProjectReference Include="../RabbitMq/RabbitMq.csproj" />
  </ItemGroup>
</Project>
```

```csharp file=Integration/EventBus/Default/EventBusRegistration.cs
using DocsExample.Integration.EventBus.RabbitMq;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace DocsExample.Integration.EventBus.Default;

public static class EventBusRegistration
{
    public static IServiceCollection AddEventBus(
        this IServiceCollection services,
        Uri broker,
        Action<IBusRegistrationConfigurator> registerConsumers,
        Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator> mapEndpoints)
    {
        services.AddMassTransit(x =>
        {
            registerConsumers(x);
            x.UsingRabbitMq((context, bus) =>
            {
                bus.Host(broker, h =>
                {
                    h.Username("ci");
                    h.Password("ci-pass");
                });
                EventBusTopology.Configure(bus);
                mapEndpoints(context, bus);
            });
        });
        return services;
    }
}
```

В реальном проекте получайте учётные данные из безопасной конфигурации развёртывания, не копируйте константы тестового host. Endpoint callback передаётся намеренно: общий пакет EventBus не должен диктовать имена очередей и классы consumers.

## Привязка под контролем потребителя и наблюдаемая доставка

**Только тестовый host** использует принадлежащую приложению очередь `examples-observer` и явно связывает `examples.*` с именованным обменником `topic`. Установка `ConfigureConsumeTopology = false` отключает автоматический binding по типу сообщения, который иначе мог бы скрыть неверный routing key. Publisher не должен знать список consumers и имена их очередей.

```xml file=App/App.csproj
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../Integration/EventBus/Default/Default.csproj" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.0" />
  </ItemGroup>
</Project>
```

```csharp file=App/Program.cs
using DocsExample.Integration.EventBus.Contracts;
using DocsExample.Integration.EventBus.Default;
using DocsExample.Integration.EventBus.RabbitMq;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddEventBus(
    new Uri("rabbitmq://localhost:5673"),
    register => register.AddConsumer<ExampleConsumer>(),
    (context, bus) =>
    {
        bus.ReceiveEndpoint("examples-observer", endpoint =>
        {
            endpoint.ConfigureConsumeTopology = false;
            endpoint.Bind(EventBusTopology.ExampleExchange, binding =>
            {
                binding.ExchangeType = "topic";
                binding.RoutingKey = "examples.*";
            });
            endpoint.ConfigureConsumer<ExampleConsumer>(context);
        });
    });

await using var provider = services.BuildServiceProvider();
var bus = provider.GetRequiredService<IBusControl>();
await bus.StartAsync();
try
{
    var eventId = Guid.NewGuid();
    await bus.Publish(new ExampleEvent(eventId));
    var delivered = await ExampleConsumer.Received.Task.WaitAsync(TimeSpan.FromSeconds(30));
    if (delivered != eventId)
        throw new InvalidOperationException("Wrong event was delivered.");
    Console.WriteLine("PASS: topic binding delivered a published event.");
}
finally
{
    await bus.StopAsync();
}

public sealed class ExampleConsumer : IConsumer<ExampleEvent>
{
    public static readonly TaskCompletionSource<Guid> Received =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task Consume(ConsumeContext<ExampleEvent> context)
    {
        Received.TrySetResult(context.Message.EventId);
        return Task.CompletedTask;
    }
}
```

## Принятие и границы тестирования

- Размещайте проекты `.csproj` в `Integration/EventBus/` и в соответствующей папке решения, соблюдая выбранную политику именования. Точный namespace и префикс пакетов не обязательны.
- Согласуйте имя producer exchange, его тип publish, formatter routing key, ключ binding у consumer и шаблон. Стандартный fanout не создаёт topic-фильтрацию только из-за наличия routing key.
- Держите сериализацию контрактных DTO отдельно от внутренних доменных сущностей; consumers принадлежат сборкам приложения. Композиционный пакет EventBus `Default` не должен знать количество и имена зависимых сервисов.
- Для других сообщений явно выбирайте именование, маршрутизацию и совместимость. `examples.event` и `examples.created` — не обязательные production-соглашения.
- На живом RabbitMQ (CI использует одноразовый Docker-контейнер) проверяйте достижение подписанной очереди через нужный exchange/binding при публикации. По контракту расширяйте тесты на несовпадающие шаблоны, отклонённые сообщения, повторы, дублирующиеся эффекты и топологию развёртывания. Включённый host проверяет **одну успешную topic-маршрутизацию**, а не production-надёжность и все политики брокера.

Источники: [транспорт RabbitMQ в MassTransit](https://masstransit.io/documentation/configuration/transports/rabbitmq), [топология MassTransit](https://masstransit.io/documentation/configuration/topology).
