---
title: ".NET project files and dependency conventions"
description: "Technology-scoped engineering standard"
kind: standard
status: active
---

# .NET project files and dependency conventions

**Applies to:** C#/.NET repositories using SDK-style project files.

## Project intent

Keep each project file minimal and express its real target framework, output and packaging role. Shared build files are mandatory under the conventions below.

- Organize `PropertyGroup`, `ProjectReference` and `PackageReference` elements consistently with `.editorconfig` and repository conventions.
- Declare only dependencies required by the project. Avoid copying package references into every project to make them appear uniform.
- Do not hardcode a version in each `PackageReference` if the repository deliberately uses Central Package Management through `Directory.Packages.props`.
- `TargetFramework` or `TargetFrameworks` must reflect supported deployment/runtime needs. Do not change them merely to standardize file aesthetics.
- Keep source dependencies aligned with the selected architectural boundaries; a `ProjectReference` to another module's private persistence implementation needs explicit justification.

## Explicit dependencies and optional project roles

Keep each `ProjectReference`/`PackageReference` necessary, non-duplicated and valid. If the solution has adopted separate modules or bounded contexts, reference the **approved public contracts/ports** for cross-boundary collaboration instead of another module's private `DbContext`, persistence implementation or unpublished application internals. A shared library is appropriate for genuinely reusable technical primitives, not for unowned business logic.

If the project uses a split EventBus integration (e.g. an independently selected MassTransit/RabbitMQ profile), keep contracts independent of the transport adapter; let the implementation depend on contracts and a composition root select both. **Do not add an EventBus project hierarchy to projects that have not selected this architecture.**

If test projects inherit shared `Directory.Build.props` or dedicated test props/targets, verify import order and the expected metadata before overriding settings. Do not blindly duplicate inherited package references, runtime targets or test-platform declarations in every test project.

## Mandatory shared build files and consistent project identity

All .NET repositories governed by this profile **MUST contain** root-level `Directory.Build.props`, `Directory.Packages.props` and `global.json`. They define one authoritative build baseline:

- `Directory.Build.props` **MUST** declare `TargetFramework`, `ImplicitUsings` and `Nullable`. Ordinary `.csproj` files **MUST NOT** repeat these properties. A documented, narrowly scoped exception (for example a genuinely different target framework, incompatible project SDK or test host) may override them.
- `Directory.Packages.props` **MUST** enable Central Package Management with `ManagePackageVersionsCentrally` and own centrally managed package versions. Individual `PackageReference` entries normally omit `Version`.
- `global.json` **MUST** specify the supported SDK selection and an explicit `rollForward` policy. Choose and maintain the real SDK version used by the project; the sample is illustrative.
- Every `.csproj` **MUST** explicitly set `PackageId`, `AssemblyName` and `RootNamespace` to the same exact value, equal to the `.csproj` filename **without its extension**. This requirement applies even if a project is not packed. Name projects using valid namespace segments (for example `Example.Service`).
- `IsPackable`, `OutputType`, `ProjectReference` and project-specific settings remain intentional per-project choices. Exceptions to the shared properties must state a verified technical reason; exceptions to the three identity properties are not part of this template's policy.

## Reviewable XML and JSON examples

Root `Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

Root `Directory.Packages.props`:

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

Root `global.json` (adjust SDK to the verified installed toolchain):

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

**Verification:** compare the three project metadata values to every project filename. Verify the required shared files exist, target properties are inherited, central package versions resolve, `dotnet --info` resolves the selected SDK and `dotnet build` passes.

## Format and tooling

Choose a stable XML formatting convention appropriate to the team. `dotnet format` can format supported projects, but it is not automatically equivalent to formatting an isolated project file. Do not claim a formatter passed without executing it and inspecting the diff.

Before committing, validate XML well-formedness, project-reference paths and that `dotnet build` succeeds with the repository's actual SDK and package sources.

References: [MSBuild Directory.Build.props](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory?view=vs-2022), [NuGet Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management), [dotnet format](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-format).
