---
title: "Specification traceability and decision propagation"
description: "Preserve stage boundaries and canonical decisions across feature, question, module and technical documents"
kind: standard
status: active
---

# Specification traceability and decision propagation

**Applies to:** documentation with multiple linked requirements, architecture/technical specifications, decisions and delivery stages. This describes how to document a project; it does not prescribe product behavior.

## Canonical ownership

- Distinguish product outcome and acceptance criteria (feature), cross-feature organization (module), technical implementation and transport contract (technical specification), architectural choice (ADR), and unresolved/resolved decisions (question record).
- A documentation module is not automatically a deployable service, namespace or DDD bounded context. State its purpose and ownership explicitly.
- Choose one authoritative location for each rule. Other documents may summarize it with a relative link, but must not silently maintain a competing copy of its values.
- Preserve contract authority: machine-readable OpenAPI/JSON Schema and real external message schemas take precedence for their respective wire contracts only when the project actually selected them.

## Delivery scope, when staged

When the project defines delivery stages, record which requirements and verification evidence apply to each stage. Deferred work is not included by implication. A proposed milestone or test plan is not evidence of shipped functionality. Do not invent Demo/MVP phases or require milestones for small projects.

## Decision lifecycle and question records

- Where a project uses a question record, read its current content and decision status instead of assuming it remains unresolved.
- Mark what is confirmed, which details remain open, who has actual decision authority and how a resolution affects the canonical specification.
- After a decision is approved, update each affected normative feature/architecture/milestone, not merely the Q-record. Track incomplete propagation explicitly.
- Keep historical decisions identifiable, including when a later decision supersedes them. An old accepted statement and a later proposal are not equivalent authorities.
- Never invent owner names, acceptance dates or implementations to make the documentation look finished.

## Cross-document consistency review

When a rule changes, verify (as applicable) its business observable outcome, technical data flow, request/response contract, UI feedback, security boundaries, milestones, tests and linked decisions.

Ask whether the documentation accidentally equates any of these different events:

- accepting an HTTP request and finishing an asynchronous operation;
- showing an available action in the UI and authorizing it on the server;
- validating an input and committing its effect;
- verifying source-code compatibility and checking deployed behavior.

These are **examples of distinctions to evaluate**, not universal requirements that every project has all these stages.

## Source evidence and claim authority

Before calling a specification "implemented" or "verified", identify **which repository, version/ref and actual source/test** supports the claim. A previous code revision or proposed API table does not establish the behavior of a currently deployed endpoint.

If a document links to source code outside the repository, resolve and inspect the exact checkout before treating it as a technical fact. A relative link to a missing file is an **unresolved evidence dependency**; do not silently replace it with an invented endpoint or approval.

When changes span HTTP contracts, storage and asynchronous workflows, distinguish **accepted target design**, **observed source**, **verified deployed behavior** and **tests that actually ran**. Keep unknowns marked "Not verified"; an accepted architectural decision does not imply corresponding implementation exists.

## Verification

Review links, stage labels, actual accepted states and feature/technical acceptance traceability. The CLI validates names, frontmatter and some local links, but cannot infer product correctness, decision approval or whether an implementation exists.
