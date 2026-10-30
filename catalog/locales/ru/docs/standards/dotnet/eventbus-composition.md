---
title: "Опциональная трёхпроектная композиция EventBus на RabbitMQ"
description: "Стандарт .NET: Опциональная трёхпроектная композиция EventBus на RabbitMQ"
kind: standard
status: active
---

# Опциональная трёхпроектная композиция EventBus на RabbitMQ

**Опциональный профиль masstransit-eventbus-composition.** Здесь представлен технический пример организации контрактов и адаптеров, а не обязательная архитектура приложения. Пример не устанавливает бизнес-события, топологию конкретного продукта или правила жизненного цикла сущностей.

## Физические проекты и логические каталоги

Вариант композиции разделяет Contracts (независимые сетевые контракты), RabbitMq (адаптер транспорта и топология) и Default (регистрация/связывание зависимостей). Папка Integration/EventBus может быть только логическим каталогом решения; не создавайте лишние проекты без явного выбора. Хост приложения владеет фактическим запуском и конфигурацией.

## Минимальный воспроизводимый пример

Следующие C# и csproj фрагменты взяты из проверяемой технической композиции. Их пути задают **примерную структуру**, а не обязательные имена проекта. Контракты не должны зависеть от транспорта; RabbitMq зависит от Contracts, Default — от Contracts и RabbitMq.

## Binding и наблюдаемая доставка

Управление подпиской и binding принадлежит принимающей стороне. При topic exchange убедитесь, что реальная routing key согласована с правилами подписки и выбранным типом exchange. При ручной конфигурации отключение ConfigureConsumeTopology требует явного binding.

При публикации недостаточно проверить отсутствие исключения: фактическая доставка и обработка должны подтверждаться живым тестом брокера, корректной корреляцией и наблюдаемыми результатами.

## Границы принятия в проект

Выбор примера **не означает**, что в существующем проекте уже есть RabbitMQ, EventBus или именно три сборки. Уточните версии, требования к конфигурации, владельцев контрактов, согласованность и безопасность до внедрения. Воспроизводимую компиляцию и маршрутизацию проверяйте на настоящем RabbitMQ; модульные тесты адаптера недостаточны.

## Исходные технические фрагменты (без изменения исполняемого кода)

```text
Integration/
  EventBus/
    Contracts/Contracts.csproj
    RabbitMq/RabbitMq.csproj
    Default/Default.csproj
App/ (verification host only, not part of the shared library)
```

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
