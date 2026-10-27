---
name: docs-architecture
description: Record evidence-based architecture context and decisions without inventing constraints or approvals.
---

# Document architecture

1. Read `llms.txt`, the existing architecture overview, selected standards, real requirements and applicable ADRs.
2. Separate observed architecture from future options. Confirm the decision authority and unresolved drivers.
3. Use `docs/templates/adr.md` when a consequential tradeoff merits an ADR; start with `status: draft` for unaccepted content and do not mark a proposal accepted.
4. Document specific alternatives, constraints, failure modes, migration cost, measurable evidence and consequences. Do not impose a particular framework, directory structure or technology because it appeared in a generic template.
5. Keep the document's YAML frontmatter and links consistent, use kebab-case filenames, and add new pages to their nearest `index.md`.
6. After a decision is actually approved, update authoritative architecture references and record the accepted state with its real owners and evidence. Avoid silently editing historical ADR decisions.
7. Run validation and report open questions and any checks unavailable in the current project.
