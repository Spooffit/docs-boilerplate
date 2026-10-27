---
name: docs-init
description: Initialize a new Docs-as-Code repository by selecting verified profiles and running the shared docs-boilerplate CLI.
---

# Initialize project documentation

1. Confirm the project name, destination, project description and actual language/technology choices. Record unknown choices explicitly; never guess a framework or architecture.
2. Run `docs-boilerplate list` (or the repository-local `dotnet run --project src/DocsBoilerplate.Cli -- list`) to inspect implemented versus planned profiles.
3. Distinguish technology profiles, optional documentation modules and optional Agent Skills. Select only what the project uses or needs.
4. Run `init` with `--dry-run` first. Review file count, composition and output path; treat any existing-file conflict as a blocker, not a reason to force overwrite.
5. Run the same command without `--dry-run`, then `validate --output <directory>`.
6. Inspect `docs.config.json` and the generated documentation. Fill missing project information from confirmed sources, not assumptions.
7. Report selected options, output files, checks and decisions still required.

Generation logic belongs to the CLI and catalog. Do not manually reproduce its rules inside the agent workflow.
