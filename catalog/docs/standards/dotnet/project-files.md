---
title: ".NET project files and dependency conventions"
description: "Technology-scoped engineering standard"
kind: standard
status: active
---

# .NET project files and dependency conventions

**Applies to:** C#/.NET repositories using SDK-style project files.

## Project intent

Keep each project file minimal and express its real target framework, output and packaging role. Shared `Directory.Build.props`/`Directory.Build.targets` are useful when a setting is common, but are not mandatory for every solution.

- Organize `PropertyGroup`, `ProjectReference` and `PackageReference` elements consistently with `.editorconfig` and repository conventions.
- Declare only dependencies required by the project. Avoid copying package references into every project to make them appear uniform.
- Do not hardcode a version in each `PackageReference` if the repository deliberately uses Central Package Management through `Directory.Packages.props`.
- `TargetFramework` or `TargetFrameworks` must reflect supported deployment/runtime needs. Do not change them merely to standardize file aesthetics.
- `IsPackable` and `PackageId` are packaging decisions. Avoid assigning `PackageId` to all internal projects by default.
- Keep source dependencies aligned with the selected architectural boundaries; a `ProjectReference` to another module's private persistence implementation needs explicit justification.

## Explicit dependencies and optional project roles

Keep each `ProjectReference`/`PackageReference` necessary, non-duplicated and valid. If the solution has adopted separate modules or bounded contexts, reference the **approved public contracts/ports** for cross-boundary collaboration instead of another module's private `DbContext`, persistence implementation or unpublished application internals. A shared library is appropriate for genuinely reusable technical primitives, not for unowned business logic.

If the project uses a split EventBus integration (e.g. an independently selected MassTransit/RabbitMQ profile), keep contracts independent of the transport adapter; let the implementation depend on contracts and a composition root select both. **Do not add an EventBus project hierarchy to projects that have not selected this architecture.**

If test projects inherit shared `Directory.Build.props` or dedicated test props/targets, verify import order and the expected metadata before overriding settings. Do not blindly duplicate inherited package references, runtime targets or test-platform declarations in every test project.

## Reviewable XML example

This example illustrates **format only**, not a mandatory package ID, namespace, packaging rule, project count or layer.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="../Contracts/Contracts.csproj" />
  </ItemGroup>
</Project>
```

If the repository uses two-space XML indentation and one element per line, preserve that convention consistently; otherwise follow the team's actual XML style. Use relative paths that resolve on the target platforms; validate the resulting MSBuild graph and restore.

## Format and tooling

Choose a stable XML formatting convention appropriate to the team. `dotnet format` can format supported projects, but it is not automatically equivalent to formatting an isolated project file. Do not claim a formatter passed without executing it and inspecting the diff.

Before committing, validate XML well-formedness, project-reference paths and that `dotnet build` succeeds with the repository's actual SDK and package sources.

References: [MSBuild Directory.Build.props](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory?view=vs-2022), [NuGet Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management), [dotnet format](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-format).
