---
title: "Documentation template architecture"
description: "Maintainer documentation"
kind: guide
status: active
---

# Documentation template architecture

## Products

**Template repository** contains the CLI, profile registry, reusable content, agent workflows and quality checks. **Generated repository** contains `README.md`, `AGENTS.md`, `llms.txt`, `docs/` (with indexes and frontmatter), `.agents/skills/` when selected, an optional root Docsify `index.html`, and the machine-readable `docs.config.json`.

## Components

```text
CLI/TUI or non-interactive flags
          |
          v
Configuration model + profile resolver
          |
          v
Generation plan -> conflict detection -> file writes
          |
          v
Generated Markdown + profile files + docs.config.json
          |
          v
Read-only validate
```

The CLI reads the catalog relative to its installation location or from an explicit `--catalog` path. Neither the agent nor the TUI implements a separate renderer.

## Categories

- **Standard**: enforceable, applicable engineering rules.
- **Guideline**: advisory and explicitly non-normative choices.
- **Reference**: verified explanation of external tools and contracts.
- **Template**: fill-in structure for a durable project document.
- **Profile**: selections, compatibility constraints and contributions.
- **ADR**: project decision and its consequences.
- **Skill**: reproducible agent procedure, pointing at normative documents rather than duplicating them.

Profiles carry `available` status. Planned profiles are discoverable but cannot be generated. Architecture principles and deployment topology can be chosen independently.

## Safety and lifecycle

No writes occur during preview or validation. Initialization refuses nonempty output folders unless they contain an identical generated plan; an identical re-run must not modify documents. The existing `update` command enforces fingerprint-based ownership and requires `--apply` to change files.

## Update lifecycle

Newly generated repositories track original template SHA-256 fingerprints. The `update` command renders the saved selection against a newer catalog, previews safe changes, and applies updates only with an explicit `--apply` flag. Concurrent author and template edits cause a conflict and leave files untouched. Retired files are preserved. Repositories without fingerprints need manual migration. See [ADR 0003](../decisions/0003-fingerprint-update.md).
