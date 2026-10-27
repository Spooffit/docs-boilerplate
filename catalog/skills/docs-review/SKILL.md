---
name: docs-review
description: Review agent-first documentation for evidence, applicability, navigation, metadata, and safe updates.
---

# Review documentation

1. Read `llms.txt`, `AGENTS.md`, `docs/index.md` and `docs.config.json` to establish project context and selected profiles.
2. Inspect only relevant documents and their authoritative references. Separate accepted decisions, requirements, implementation evidence, examples and unresolved questions.
3. Check YAML frontmatter, lowercase kebab-case filenames, directory `index.md` files and relative links. Preserve `README.md`, `AGENTS.md`, `SKILL.md` and `CONTRIBUTING.md` when those well-known names are applicable.
4. Compare each normative rule with its declared technology scope. A selected optional library does not imply unrelated architectures or dependencies.
5. Look for duplicated rules, stale links, broken anchors and contradictions. `docs/catalog.md` and `llms.txt` are navigation aids, not replacements for canonical sources.
6. Run `docs-boilerplate validate --output <directory>` where available. Report manual checks separately from automated checks and identify what the validator cannot establish.
7. Provide file-specific findings, impact and safe next steps. Do not rewrite approved project decisions without supporting evidence.

Never infer that a passing link/metadata check proves technical correctness or security.
