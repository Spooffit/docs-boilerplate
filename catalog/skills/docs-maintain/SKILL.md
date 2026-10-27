---
name: docs-maintain
description: Update existing project documentation after a requirement or implementation change, maintaining canonical sources and relative links.
---

# Maintain project documentation

1. Read `README.md`, `docs.config.json`, the affected feature or architectural document, and applicable standards.
2. Determine whether the request changes a confirmed requirement, a decision or an implementation fact.
3. Edit the authoritative document first. Link to it from related pages; avoid copying full requirements or rules.
4. Preserve stable identifiers and relative paths. If a document moves, repair incoming references.
5. Keep unknown information explicit and traceable rather than filling it with assumptions.
6. Run documentation validation, inspect the diff, and report what remains unverified.

Do not turn advisory examples into mandatory standards.
