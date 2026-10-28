# DocsBoilerplate.Cli

A .NET 10 command-line tool for generating Markdown-first documentation repositories with opt-in technology standards and an optional Docsify viewer.

The package contains the CLI and its built-in documentation catalog. It does not generate build-system dependencies or a static-site output tree in target projects.

## Commands

```sh
docs-boilerplate version
docs-boilerplate list
docs-boilerplate doctor
docs-boilerplate init --name Example --description "Documentation for Example" --output ./example-docs --profiles dotnet --non-interactive
docs-boilerplate validate --output ./example-docs
docs-boilerplate update --output ./example-docs
docs-boilerplate update --output ./example-docs --apply
```

`update` previews proposed changes by default and uses file fingerprints to protect local modifications. If an update was interrupted, inspect the project and use `docs-boilerplate recover --output ./example-docs`.

## Install from a locally built package

```sh
dotnet pack src/DocsBoilerplate.Cli -c Release -o ./artifacts
dotnet tool install DocsBoilerplate.Cli --tool-path ./tools --add-source ./artifacts --version 0.1.0
./tools/docs-boilerplate version
```

The repository's [generator guide](https://github.com/Spooffit/docs-boilerplate/blob/main/docs/generator.md) describes profile selection, schema compatibility, preview-first updates and recovery. Tool version and generated manifest schema version are independent. The examples above do not imply that the package is published to a public feed.
