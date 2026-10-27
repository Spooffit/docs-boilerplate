---
title: "Generator code map and verification"
description: "Maintainer guide to the CLI code boundaries, tests and safe change workflow"
kind: guide
status: active
---

# Generator code map and verification

**Audience:** contributors and AI coding agents maintaining this template repository, not users of generated project documentation.

## Where changes belong

| Area | Canonical file | Responsibility |
| --- | --- | --- |
| Entrypoint | `src/DocsBoilerplate.Cli/Program.cs` | Parse the command name, load a catalog, dispatch `init`, `update`, `validate`, `doctor` and `list` |
| CLI UX | `src/DocsBoilerplate.Cli/DocsCli.Commands.cs` | Prompt for profiles, validate command options, create a new project |
| Catalog | `src/DocsBoilerplate.Cli/DocsCli.Catalog.cs` | Resolve dependencies, reject unknown profiles, check catalog integrity and model the manifests |
| Rendering | `src/DocsBoilerplate.Cli/DocsCli.Rendering.cs` | Compose selected source files, indexes, `llms.txt`, and the generated ownership manifest |
| Updating | `src/DocsBoilerplate.Cli/DocsCli.Updates.cs` | Preview-first, fingerprint-checked updates and best-effort rollback |
| Validation | `src/DocsBoilerplate.Cli/DocsCli.Validation.cs` | Check generated manifest ownership, filenames, frontmatter, local links and folder indexes |
| Markdown metadata | `src/DocsBoilerplate.Cli/DocumentMetadata.cs` | Narrow frontmatter policy and kebab-case naming |
| File system | `src/DocsBoilerplate.Cli/SafeFiles.cs` | Reject traversal, symlink/reparse-point paths and unsafe file-system entries |
| Data contracts | `schemas/*.schema.json` | JSON Schema definitions for input registries and generated `docs.config.json` |
| Content | `catalog/` | Reusable standards, templates, optional modules, Agent Skills and the Docsify viewer |

The CLI is still one executable and uses a **partial static class** split by responsibility. This is a maintainability step, not a new domain layering scheme or separate shared library. Keep project-specific feature content out of these files.

## Testing layers

- **Focused .NET contract checks:** `tests/DocsBoilerplate.ContractTests` runs isolated assertions for catalog dependency resolution, Markdown metadata, naming, path containment, document navigation and hashing. It has no external testing-package dependencies.
- **End-to-end CLI verification:** `tests/smoke.sh` exercises generated profiles, updates, conflicts, invalid manifests, file-system safety and Docsify HTTP delivery on Linux.
- **Windows CLI verification:** `tests/windows-smoke.ps1` checks an independent real Windows execution path.
- **JSON Schema contracts:** `tests/schema_contracts.py` validates catalog files and representative generated config; CI installs `jsonschema` for these checks only.
- **Migration inventory:** `tests/legacy_inventory.py` refuses disappearance of source material without a recorded process.

A passing focused check does not prove browser interactivity, all Markdown link variants or crash-atomic multi-file updates. Keep these limitations explicit.

## Editing workflow

1. Choose the owning area from the table rather than adding behavior back into `Program.cs`.
2. Change only the necessary catalog manifest and canonical Markdown source.
3. Add a focused regression assertion for pure logic and an end-to-end case when file output or command behavior changes.
4. Run `dotnet build src/DocsBoilerplate.Cli -c Release`, `dotnet run --project tests/DocsBoilerplate.ContractTests -c Release` and `bash tests/smoke.sh` in a suitable environment.
5. Check Linux and Windows GitHub Actions before marking the change verified.
6. Never delete historical project content solely because an inventory entry calls it project-specific. Follow the [historical triage](../audit/project-source-triage.md) and record its disposition.

## Long-term refactor boundary

If features grow further, the renderer, catalog and updater may become separate injected services and a testing-framework project may replace the no-dependency harness. Do not introduce framework layers just for architecture aesthetics: measure whether the split makes independent testing and safe changes substantially easier.
