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

## Format and tooling

Choose a stable XML formatting convention appropriate to the team. `dotnet format` can format supported projects, but it is not automatically equivalent to formatting an isolated project file. Do not claim a formatter passed without executing it and inspecting the diff.

Before committing, validate XML well-formedness, project-reference paths and that `dotnet build` succeeds with the repository's actual SDK and package sources.

References: [MSBuild Directory.Build.props](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory?view=vs-2022), [NuGet Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management), [dotnet format](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-format).
