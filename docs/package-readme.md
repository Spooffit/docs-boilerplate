---
title: "Docs Boilerplate .NET tool"
description: "README embedded in the installable NuGet package"
kind: guide
status: active
---

# Docs Boilerplate CLI

A Markdown-first documentation repository initializer built with .NET 10 and Spectre.Console. It creates optional backend, frontend and infrastructure documentation profiles, plus temporary AI-agent intake instructions.

## Install and update

```powershell
dotnet tool install --global DocsBoilerplate.Cli
docs-boilerplate init
dotnet tool update --global DocsBoilerplate.Cli
```

## Project lifecycle

1. Run `docs-boilerplate init` and choose the project name, output directory, optional technologies, infrastructure and documentation site.
2. Open the generated project in your AI coding-agent workspace and follow `bootstrap.md` for an evidence-based project questionnaire.
3. After the owner approves the initial project docs, preview `docs-boilerplate bootstrap-complete --output <project-directory>`, then add `--apply` to remove only unchanged temporary intake files.
4. Run `docs-boilerplate validate --output <project-directory>` to check structure, references and selected profiles.

The tool ships Markdown templates and technical guidance; it does not create an application or deploy infrastructure. Infrastructure profiles initially contain draft placeholders.

For maintenance and sources, see the repository: https://github.com/Spooffit/docs-boilerplate
