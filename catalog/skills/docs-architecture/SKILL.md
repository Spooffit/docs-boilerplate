---
name: docs-architecture
description: Record evidence-based architecture context and decisions without inventing constraints or approvals.
---

# Document architecture

1. Read `llms.txt`, the existing architecture overview, selected standards, real requirements and applicable ADRs.
2. Separate observed architecture from future options. Confirm the decision authority and unresolved drivers.
3. Use `docs/templates/adr.md` only when a consequential tradeoff needs a separate ADR; otherwise update the existing authoritative architecture page. Start an unaccepted ADR at `status: draft` and never claim approval.
4. Document specific alternatives, constraints, failure modes, migration cost, measurable evidence and consequences. Do not impose a particular framework, directory structure or technology because it appeared in a generic template.
5. Keep YAML frontmatter, kebab-case filenames and links consistent. For each new page under `docs/`, add a relative link to its immediate directory's `index.md` **and** `docs/catalog.md`; repair inbound links when moving or renaming a page. Never generate an empty document just to populate an index.
6. After a decision is actually approved, update authoritative architecture references and record the accepted state with its real owners and evidence. Avoid silently editing historical ADR decisions.
7. Run validation and report open questions and any checks unavailable in the current project.
