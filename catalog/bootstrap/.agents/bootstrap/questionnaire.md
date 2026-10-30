---
title: "Conditional project intake questionnaire"
description: "Temporary staged questions and conditional follow-ups for an AI agent"
kind: guide
status: draft
---

# Conditional project intake questionnaire

**Ask in short rounds.** Do not demand answers that have no relevance yet. Each question supports "unknown" or "not applicable". Do not assume a response indicates authorization to implement software or deploy infrastructure.

## Round 1: Purpose and scope

- What problem should this project solve, and for whom?
- What is the intended outcome and how will you know it works?
- Is this a new product or an extension/replacement of an existing system?
- What is in scope now, and what should explicitly stay out of scope?

**If replacing an existing system:** ask which features, data or integrations must be retained and which are deliberately removed. **If experimental:** ask what needs to be learned, but do not invent a Demo/MVP/Production phase.

## Round 2: Existing context and neighboring projects

- Is there already a code repository, architecture document, design, issue tracker or running implementation to inspect?
- Which adjacent projects or shared components must be considered? Who owns the decisions?
- Are there existing constraints, policies or conventions we should cite rather than recreate?

**If repositories exist:** request accessible paths/links and verify the specific facts before marking them implemented. **If none exist:** record proposed architecture as a plan, not current behavior.

## Round 3: System boundaries, backend and frontend

- What interactions and data flows define the system? What data is owned by this project?
- If a backend was selected, what backend capabilities or responsibilities are actually needed?
- If a frontend was selected, who uses it and what main workflows matter?
- Does the project intentionally have no backend, no frontend or neither?

**If backend is absent:** do not invent an API, database or service. **If frontend is absent:** do not invent screens or browser workflows. **If a selected profile depends on .NET for a frontend-only project:** clarify whether that is merely shared runtime guidance or a planned backend.

## Round 4: Integrations and related services

- Which external APIs, message brokers, providers or internal services are required?
- For each integration, what is its purpose, owner and known contract?
- Is it already available, still planned or simply a possible option?
- Are there critical failure modes, availability requirements, data sensitivities or access constraints to document?

**If integration is real:** request existing contract/specification or implementation reference. **If hypothetical:** record an open question, not an invented endpoint, event topology or guaranteed SLA. Never request secrets or credentials for documentation.

## Round 5: Infrastructure (only for selected or known infrastructure)

- Which runtime/deployment environments exist or are planned?
- Is the selected logging/metrics/tracing tool already deployed? Where is the canonical observability configuration?
- Is deployment through Docker Compose, Kubernetes, or undecided? Which operational constraints matter?
- Are DevOps objects owned in the backend repository, a separate GitOps repository or another established location?
- If .NET Aspire was selected, is it an existing AppHost, a planned local development orchestrator, or a tentative idea?

**If infrastructure was skipped:** do not manufacture DevOps standards or deployment structure. **If selected:** keep placeholders in `docs/standards/infrastructure/` marked draft until real decisions and evidence exist.

## Round 6: Delivery, terminology and unknowns

- Are there real milestones, dates, checkpoints, contractual deadlines or rollout constraints?
- Which project-specific terms need definitions so developers and agents use the same vocabulary?
- What choices remain unresolved and who can approve them?

**If milestones exist:** record goals and verifiable acceptance for each; never impose a default stage taxonomy. **If terms are known:** populate the already-created glossary; otherwise leave its draft scaffold as-is. All documentation modules exist by default and do not imply that their subjects exist in the product. **If decisions are pending:** write open questions without fabricating answers.

## Writing and confirmation

Populate `docs/project/overview.md` and `docs/architecture/overview.md` from verified context. Only create additional project documents when information and ownership warrant them. Every new `docs/` document needs a stable unique `id` in frontmatter; preserve IDs on renames. Update navigation for new pages, summarize uncertainties, and ask whether the owner accepts the initial documentation as the stage-two baseline.
