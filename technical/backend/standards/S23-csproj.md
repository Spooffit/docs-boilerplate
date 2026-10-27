# S23. Структура `.csproj`

## Назначение

Стандарт задаёт минимальные правила структуры и форматирования `.csproj`. При изменении файла сохранять назначение проекта и не добавлять зависимости или build-настройки, не требуемые задачей.

## XML

- отступ — 2 пробела, без tab;
- один XML element на строку;
- self-closing tag оформляется как ` />`;
- относительные пути используют `/`;
- пустые `ItemGroup` не создаются;
- логические группы разделяются не более чем одной пустой строкой;
- если формат файла нарушен, нормализуется весь `.csproj`, а не случайный фрагмент.

## Обычный проект

Рекомендуемый порядок:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <PackageId>{ProjectName}</PackageId>
    <AssemblyName>{ProjectName}</AssemblyName>
    <RootNamespace>{ProjectName}</RootNamespace>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="../Some.Project/Some.Project.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Some.Package" />
  </ItemGroup>
</Project>
```

Правила:

- `PackageId`, `AssemblyName`, `RootNamespace` обычно совпадают с canonical project name;
- `IsPackable=false` используется для внутренних проектов, которые не публикуются как package;
- packable contract/tooling projects сохраняют осознанную package configuration и не переводятся в non-packable по шаблону;
- `TargetFramework`, nullable/analyzers и другие общие свойства не дублируются, если задаются repository `.props/.targets`;
- NuGet version не указывается в project file при central package management.

## References

`ProjectReference` и `PackageReference` размещаются в отдельных `ItemGroup`.

Добавляется только реально необходимая зависимость:

- не копировать references из соседнего проекта "для симметрии";
- не добавлять duplicate reference;
- учитывать transitive/repository-level dependencies;
- перед сохранением проверить относительный путь к `.csproj`.

## Границы bounded contexts

`ProjectReference` не должен обходить границы BC.

Запрещено использовать ссылку на чужой `EntityFramework`, persistence implementation или внутренний Domain/Application project только ради прямого доступа к данным.

Между BC используются утверждённые public contracts/abstractions/ports. Если синхронное межмодульное чтение разрешено архитектурой, dependency направляется на соответствующий public abstractions project, а не на чужой `DbContext`.

Общий технический код допускается в Shared только при реальном переиспользовании; Shared не используется как место для бизнес-логики без владельца.

## EventBus projects

Проекты `Integration/EventBus/*` дополнительно соблюдают S22.

- `Contracts` не ссылается на `RabbitMq`;
- `RabbitMq` ссылается на `Contracts`;
- `Default` композирует оба проекта;
- package/packability contract projects определяется принятой integration-contract distribution scheme.

## Test projects

Тестовый `.csproj` использует repository-level test configuration (`.props/.targets`) и объявляет требуемые metadata до import, если такой порядок установлен тестовой инфраструктурой.

Точные имена metadata, допустимые значения и classification тестов не дублируются здесь: они определяются test standards и `TestProject.props`.

Не повторять свойства и package configuration, уже задаваемые общими test props, если override не является осознанным.

## Изменение существующего проекта

При правке `.csproj`:

1. определить роль проекта и его BC;
2. сохранить существующую repository configuration из `.props/.targets`;
3. проверить допустимость каждой новой зависимости по architecture boundaries;
4. разделить project/package references;
5. не менять packability, framework или build behavior без требования;
6. сделать минимальное изменение;
7. после правки проверить валидность XML и relative paths.

## Проверка

Перед завершением убедиться, что:

- XML валиден и единообразно отформатирован;
- нет пустых групп и duplicate references;
- package versions не продублированы при central management;
- repository props/targets не обходятся;
- cross-BC dependency не открывает чужой persistence;
- test metadata/import имеют правильный порядок;
- EventBus project соблюдает S22;
- изменение не добавило лишних dependencies/settings.
