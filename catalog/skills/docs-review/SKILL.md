---
name: docs-review
description: Review agent-first documentation for evidence, applicability, navigation, metadata, and safe updates.
---

# Review documentation

1. Read `llms.txt`, `AGENTS.md`, `docs/index.md` and `docs.config.json` to establish project context and selected profiles.
2. Inspect only relevant documents and their authoritative references. Separate accepted decisions, requirements, implementation evidence, examples and unresolved questions. An `active` metadata status alone does not prove a requirement is approved or functionality exists.
3. Check YAML frontmatter, lowercase kebab-case filenames, directory `index.md` files and relative links. Preserve `README.md`, `AGENTS.md`, `SKILL.md` and `CONTRIBUTING.md` when those well-known names are applicable.
4. Compare each normative rule with its declared technology scope. A selected optional library does not imply unrelated architectures or dependencies.
5. Look for duplicated rules, stale links, broken anchors and contradictions. `docs/catalog.md` and `llms.txt` are navigation aids, not replacements for canonical sources.
6. Use the source-checkout validator only if a separate boilerplate checkout and .NET 10 SDK are available: `dotnet run --project src/DocsBoilerplate.Cli -- validate --output <directory>` **from that checkout**. Otherwise inspect references and metadata manually and explicitly report missing automated checks. Never assume the generated repository contains the CLI or that validation proves factual correctness.
7. Provide file-specific findings, impact and safe next steps. Do not rewrite approved project decisions without supporting evidence.

Never infer that a passing link/metadata check proves technical correctness or security.
