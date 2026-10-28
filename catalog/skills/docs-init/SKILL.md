---
name: docs-init
description: Initialize Docs-as-Code projects using explicit profiles and evidence-first project intake.
---

# Initialize project documentation

1. Confirm the destination, name and factual summary. Inspect actual requirements, decisions, source code, schemas and available test/deployment evidence. Never invent project facts to fill a template.
2. Ask for an essential missing choice only when needed; mark unknown technology, architecture and product behavior as **Not yet verified**.
3. If the generator **source checkout** and .NET 10 SDK are available, run `dotnet run --project src/DocsBoilerplate.Cli -- list` from that checkout. Select only available applicable profiles, modules and skills. A generated documentation repository does not contain an installed CLI and does not require a NuGet package; profile selection does not prove which technology a product uses.
4. From the generator checkout, preview `dotnet run --project src/DocsBoilerplate.Cli -- init ... --dry-run` and refuse collisions or overwrites. Run the reviewed `init` command, then `dotnet run --project src/DocsBoilerplate.Cli -- validate --output <directory>` from the same checkout. Docsify adds one root `index.html`; `--site none` omits the viewer. If the checkout or SDK is unavailable, report that limitation instead of inventing a command in the generated project.
5. Read the generated `llms.txt`, `AGENTS.md`, `docs/index.md`, `docs/catalog.md` and `docs.config.json`. Verify installed files and dependencies match the confirmed selection.
6. **First-pass project intake:** fill `docs/project/overview.md` and `docs/architecture/overview.md` from evidence. Read an adopted specification and observed implementation separately; do not treat an `active` frontmatter status or selected profile as independent proof of business truth. Separate observed implementation, accepted target and open decisions. Keep both pages `draft` until reviewed; unavailable facts are not assumptions.
7. Create only feature, technical-feature, module, question, milestone or ADR documents actually required; select the smallest relevant template from `docs/templates/index.md`. Do not prepopulate empty document families.
8. Rerun the generator's `validate` command from its checkout after edits when available; inspect navigation, selected scope and file ownership. A valid document is not automatically an accepted design. Do not edit `docs.config.json` by hand.
9. Report files and profiles installed, real evidence inspected, remaining drafts/open choices and tests actually performed.

Use the generator's preview-first updater for later changes; do not duplicate its ownership or hashing logic in this skill.
