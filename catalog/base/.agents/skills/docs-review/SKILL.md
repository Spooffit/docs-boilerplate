---
name: docs-review
description: Review project documentation for link integrity, contradictory rules, missing evidence and architecture decisions.
---

# Documentation review workflow

1. Read `README.md`, `docs.config.json`, and only the affected documents and relevant selected standards.
2. Separate confirmed requirements and decisions from open questions; never invent missing information.
3. Check each link and the relationship between standards, ADRs, project behavior and code.
4. Report conflicts with locations, likely impact and a concrete suggested change.
5. Run the repository's available documentation validator. Do not claim execution if it was unavailable.
6. Keep review findings short, actionable and traceable to the actual sources.
