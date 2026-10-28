---
title: "Optional document identifiers, approvals and statuses"
description: "Project-adopted document IDs, decision approval evidence and lifecycle transitions"
kind: standard
status: active
---

# Optional document identifiers, approvals and statuses

**Applies when:** the `document-identifiers-and-statuses` profile has been explicitly selected. The baseline [documentation authoring convention](../documentation.md) still uses descriptive `kebab-case` filenames and minimal frontmatter; choosing this profile does not rename existing documents, require new document types, or add mandatory metadata fields.

## Identifier policy

- Decide **per project** which artifact kinds need stable identifiers, if any. A descriptive relative Markdown path is sufficient for ordinary documents. Do not import F/M/Q numbering, a project-specific prefix or a globally incrementing sequence into unrelated projects.
- When an identifier is justified (for external references, audit or durable cross-document traceability), choose a project-owned namespace, uniqueness scope, allocation owner, collision-resolution and rename/retirement policy. Record the adopted policy in an existing project document or ADR before assigning IDs.
- Prefer stable references and relative links. Do not retroactively generate identifiers for every template, module, milestone or paragraph. A filename can remain descriptive even if a document contains a separately managed identifier.
- Do not invent an identifier, timestamp, review owner or approver to fill a template. If the ID policy is undecided, leave the document descriptive and record the unresolved decision.

## Different kinds of status

The ordinary YAML frontmatter `status` remains **document metadata**, with existing permitted values `active`, `draft`, `template` and `superseded`. It does not certify business acceptance, runtime deployment or successful testing. A draft document can describe an accepted fact; an active document can reference an unimplemented proposal.

When the project genuinely needs an **approval workflow**, track the decision separately from document metadata in prose or an adopted project-local schema. For example, a question/ADR may record `Decision status: Pending / Accepted / Rejected / Deferred` along with the actual authority, dated evidence and affected documents. These decision labels are a **suggested local vocabulary**, not new required generator frontmatter enum values. Do not silently promote a question to accepted because a file was edited or a CI build passed.

## Approval and change protocol

1. Record what is proposed, why, relevant constraints and the source of authority.
2. Identify the real decision-maker if known. Leave authority and dates unknown otherwise.
3. On an actual decision, record the result, rationale, source/evidence and affected references. A suggestion or historical example is not approval.
4. Update the canonical document and its inbound links/navigation. Mark replaced guidance as superseded only when the replacement is real and linked.
5. Where a project uses a formal review gate, enforce it in **that project** with the adopted process. The docs generator does not create an approval system, transition engine, ID registry or workflow automation.

## Verification

Review identifier uniqueness only within the project-defined scope; check that references resolve and decision authority is evidenced before stating that a decision was accepted. Generator `validate` verifies document structure and supported metadata but does **not** verify organizational approvals or enforce project-specific ID schemes.
