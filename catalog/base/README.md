# {{PROJECT_NAME}}


Start with [the documentation home](docs/index.md) and [generated document catalog](docs/catalog.md), [project overview](docs/project/overview.md), [architecture](docs/architecture/overview.md), and the [engineering standards](docs/standards/index.md).

Use [llms.txt](llms.txt) for an AI-readable overview and [AGENTS.md](AGENTS.md) for agent instructions. Every documentation directory has an `index.md`; see [the authoring standard](docs/standards/documentation.md).

This is a generated documentation repository. Modify project documents in Git; do not treat sample templates as factual decisions.

## Complete initialization with an AI agent

If `bootstrap.md` is present, the initial technology selection is done but project discovery is **not**. Ask your AI coding agent to follow the temporary intake instructions and question guide. They will ask about goals, related systems, integration constraints, actual milestones and terminology, then populate the permanent project/architecture documentation with your approval.

Only after you approve the stage-two summary, remove the temporary intake files with `docs-boilerplate bootstrap-complete --output <repository-root> --apply`. The normal templates, standards, agent instructions and selected persistent skills remain. You may inspect the deletion plan first without `--apply`.
