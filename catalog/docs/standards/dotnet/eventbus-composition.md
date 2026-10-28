---
title: "Opt-in MassTransit RabbitMQ three-project EventBus composition"
description: "Compile-tested Contracts/RabbitMq/Default layout and RabbitMQ topic routing contract"
kind: standard
status: active
---

# Opt-in MassTransit RabbitMQ three-project EventBus composition

**Applies when:** the `masstransit-eventbus-composition` profile is explicitly selected, in addition to the general [MassTransit/RabbitMQ guidance](masstransit-rabbitmq.md). The normal `masstransit-rabbitmq` profile does **not** require this organization.

## Physical projects and solution folders

Use these three library projects inside a physical `Integration/EventBus/` directory and corresponding *solution folder*. The fourth `App` project shown in the verification sample is a disposable consumer harness, **not a fourth shared EventBus library** and never generated into a documentation repository.

```text
Integration/
  EventBus/
    Contracts/Contracts.csproj
    RabbitMq/RabbitMq.csproj
    Default/Default.csproj
App/ (verification host only, not part of the shared library)
```

Contracts must not know MassTransit, RabbitMQ, consumers or application domain entities. RabbitMq references Contracts, configures publish topology and transport-specific conventions. Default references Contracts and RabbitMq, exposes service registration and delegates consumer endpoint ownership to the consuming application. Direction: `Contracts <- RabbitMq <- Default`. Do not add these projects unless the profile has actually been adopted.

## Verified minimal reference composition

The fenced files below form a **generic technical fixture**, not an application or product requirement. `tests/optional_profile_contracts.py` reconstructs the project references and C# sources in a temporary directory and compiles/runs them against an isolated broker in Linux CI. The sample pins MassTransit.RabbitMQ 8.5.12 for reproducibility; adopting projects must explicitly choose and verify a supported version. The fixture credentials (`ci`/`ci-pass`) are disposable CI-only credentials, never production defaults.

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

In a real project, resolve credentials from secure deployment configuration rather than copying the harness literals. Accepting the endpoint callback is intentional: the shared EventBus package must not dictate queue names or consumer classes.

## Consumer-owned binding and observable delivery

This **test harness only** uses an application-owned `examples-observer` queue and explicitly binds `examples.*` to the named `topic` exchange. Setting `ConfigureConsumeTopology = false` avoids adding an automatic per-message-type binding whose behavior could mask a wrong routing key. The publisher need not know the consumer list or queue name.

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

## Adoption and test boundaries

- Store `.csproj` projects under `Integration/EventBus/` and add them under the matching solution folder, using the selected project/solution naming policy. Do not require an exact namespace or package prefix.
- Match the producer entity name, publish exchange type, routing-key formatter, consumer binding key and pattern. A default fanout exchange does not implement topic filtering merely because a routing key is present.
- Keep contract DTO serialization independent of internal domain entities; consumers live in application-owned assemblies. An EventBus `Default` composition package must not know the number or names of downstream services.
- For another event contract, make naming, routing and compatibility decisions explicitly. Never assume `examples.event` or `examples.created` are mandatory production conventions.
- Verify with a live RabbitMQ instance (CI uses a disposable Docker container) that a publishing application can reach a subscribing queue through the intended exchange/binding. Expand tests for disjoint patterns, rejected messages, retries, duplicate effects and deploy topology as the real contract requires. The included harness checks one successful topic-route, not production reliability or all broker security policies.

References: [MassTransit RabbitMQ transport](https://masstransit.io/documentation/configuration/transports/rabbitmq), [MassTransit topology](https://masstransit.io/documentation/configuration/topology).
