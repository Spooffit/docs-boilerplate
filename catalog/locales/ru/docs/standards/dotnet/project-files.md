---
title: "Файлы проектов .NET и соглашения о зависимостях"
description: "Инженерный стандарт для выбранной технологии"
kind: standard
status: active
---

# Файлы проектов .NET и соглашения о зависимостях

**Применяется к:** C#/.NET-репозиториям с проектами в стиле SDK.

## Назначение проекта

Держите файлы проектов минимальными и отражайте реальную целевую платформу, роль сборки и упаковки. Общие файлы сборки обязательны в соответствии с правилами ниже.

- Организуйте `PropertyGroup`, `ProjectReference` и `PackageReference` согласованно с `.editorconfig` и правилами репозитория.
- Объявляйте только нужные зависимости. Не копируйте ссылки на пакеты в каждый проект ради внешнего единообразия.
- Не указывайте версии во всех `PackageReference`, если репозиторий намеренно использует Central Package Management через `Directory.Packages.props`.
- `TargetFramework` или `TargetFrameworks` должны соответствовать потребностям развёртывания/выполнения. Не меняйте их ради одинакового вида файлов.
- `IsPackable` и `PackageId` — решения об упаковке. Не назначайте `PackageId` всем внутренним проектам по умолчанию.
- Сохраняйте зависимости кода в принятых архитектурных границах. Ссылка на приватную реализацию хранения другого модуля через `ProjectReference` требует явного обоснования.

## Явные зависимости и необязательные роли проектов

Каждый `ProjectReference`/`PackageReference` должен быть необходимым, действительным и без дубликатов. Если приняты отдельные модули или ограниченные контексты, для межмодульного взаимодействия ссылайтесь на **утверждённые публичные контракты/порты**, а не на чужой приватный `DbContext`, реализацию хранения или внутренности приложения. Общая библиотека подходит для действительно общих технических примитивов, а не для бизнес-логики без владельца.

Если выбран раздельный EventBus (например, отдельный профиль MassTransit/RabbitMQ), держите контракты независимыми от транспортного адаптера; реализация зависит от контрактов, а composition root выбирает обе части. **Не добавляйте иерархию EventBus в проект, который не выбрал такую архитектуру.**

Если тестовые проекты наследуют `Directory.Build.props` или специальные test props/targets, проверяйте порядок импортов и ожидаемые метаданные перед переопределениями. Не дублируйте автоматически унаследованные пакеты, целевые платформы или настройки раннера в каждом тестовом проекте.

## Обязательные общие файлы сборки и идентичность проектов

В каждом .NET-репозитории, где применяется этот профиль, **обязательны** корневые `Directory.Build.props`, `Directory.Packages.props` и `global.json`:

- `Directory.Build.props` **обязательно** задаёт `TargetFramework`, `ImplicitUsings` и `Nullable`. В обычных `.csproj` **запрещено** повторять эти свойства. Узкое исключение (другая целевая платформа, несовместимый SDK проекта, специальный test host) разрешено лишь с подтверждённым техническим обоснованием.
- `Directory.Packages.props` **обязательно** включает Central Package Management через `ManagePackageVersionsCentrally` и хранит версии пакетов. В `PackageReference` отдельных проектов обычно нет `Version`.
- `global.json` **обязательно** закрепляет выбор .NET SDK и явную политику `rollForward`. Версию подбирайте по реальному установленному SDK.
- В **каждом** `.csproj` **обязательно явно присутствуют** `PackageId`, `AssemblyName` и `RootNamespace`. Все три значения **одинаковые и точно равны имени самого .csproj без расширения**. Правило действует и для внутренних проектов, не упаковываемых в NuGet. Используйте допустимые для пространства имён имена, например `Example.Service`.
- `IsPackable`, `OutputType` и необходимые ссылки проектов остаются локальными решениями. Исключения для трёх полей идентичности стандарт не предусматривает.

## Проверяемые примеры XML и JSON

Корневой `Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

Корневой `Directory.Packages.props`:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.0" />
  </ItemGroup>
</Project>
```

Корневой `global.json` (подставьте подтверждённую версию SDK):

```json
{
  "sdk": {
    "version": "10.0.400",
    "rollForward": "latestFeature"
  }
}
```

`Example.Service.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <PackageId>Example.Service</PackageId>
    <AssemblyName>Example.Service</AssemblyName>
    <RootNamespace>Example.Service</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
  </ItemGroup>
</Project>
```

**Проверка:** сравните три свойства с именем каждого файла проекта. Убедитесь, что общие файлы существуют, свойства наследуются, центральные версии пакетов разрешаются, `dotnet --info` использует выбранный SDK и `dotnet build` проходит.

## Формат и инструменты

Выберите устойчивое XML-форматирование, подходящее команде. `dotnet format` умеет форматировать поддерживаемые проекты, но не обязательно равнозначен форматированию отдельного файла проекта. Не заявляйте об успешной работе форматтера, не запустив его и не просмотрев diff.

Перед коммитом проверьте синтаксис XML, пути ссылок на проекты и успешность `dotnet build` с настоящим SDK и источниками пакетов репозитория.

Источники: [MSBuild Directory.Build.props](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory?view=vs-2022), [NuGet Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management), [dotnet format](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-format).
