# Docs Boilerplate CLI

A documentation-first `.NET 10` global tool using Spectre.Console. Create Markdown documentation repositories with optional backend, frontend and infrastructure selections and a temporary AI-agent project intake questionnaire.

## Install or update

```powershell
dotnet tool install --global docs-boilerplate
docs-boilerplate init
dotnet tool update --global docs-boilerplate
```

## Two-stage project initialization

First, run `docs-boilerplate init` to choose technologies and generate reusable standards/templates. Then open the generated project with your AI agent and follow `bootstrap.md` to interview the owner, document the actual project, and receive approval.

Once stage two is approved:

```powershell
docs-boilerplate bootstrap-complete --output <project-directory>
docs-boilerplate bootstrap-complete --output <project-directory> --apply
docs-boilerplate validate --output <project-directory>
```

The temporary intake pack is removed, while project documentation and independently selected standards remain. The tool does not create application code or deploy infrastructure. Initial infrastructure technology pages are labeled as draft placeholders.

Source repository: https://github.com/Spooffit/docs-boilerplate
