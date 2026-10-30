---
title: "Stage two: project discovery with an AI agent"
description: "Temporary project intake instructions, removed after owner-approved onboarding"
kind: guide
status: draft
---

# Stage two: project discovery

**This file is temporary.** It exists only after CLI initialization. The first stage selected technologies and installed reusable writing standards; it did **not** invent project goals, business rules, integration contracts or milestones.

## What the agent must do

1. Read `AGENTS.md`, `docs.config.json`, `docs/project/overview.md` and `docs/architecture/overview.md`, then follow the temporary [questionnaire](.agents/bootstrap/questionnaire.md). If the temporary `project-intake` skill is available, apply it.
2. Introduce the project-discovery interview. Ask related questions in small, coherent rounds, not as one huge form. Always allow "unknown", "not decided" and "not applicable"; follow-up questions must depend on actual answers.
3. Distinguish **confirmed existing behavior**, **approved intended behavior**, **ideas awaiting approval**, and **unknown facts**. Technology choices in the initial config are only selected documentation profiles, not evidence of implementation.
4. Identify product purpose and intended users, success criteria, existing repositories, associated projects, external services, environments and integration owners. Ask about delivery milestones only if the user actually has stages or checkpoints. Do not invent Demo/MVP/Production.
5. Populate the existing project and architecture overviews with grounded, concise answers, preserving `draft` until facts and authority justify promotion. Create or populate a glossary only when useful terms exist; do not create empty feature/specification directories.
6. Update immediate directory indexes and `docs/catalog.md` when adding documents. Follow the installed standards that actually apply to the project; infrastructure placeholders are explicitly not accepted operational instructions.
7. Summarize what was learned, what remains unknown, the documents changed, and ask for **explicit owner confirmation** that stage two is complete. Do not claim completion on the basis of a successful structural validation.
8. With approval, preview removal using `docs-boilerplate bootstrap-complete --output <project-root>` and then run `docs-boilerplate bootstrap-complete --output <project-root> --apply`. Only four known intake files are removed, after verifying they have not been edited. If the tool is unavailable, retain the files and report the limitation; do not claim cleanup.
9. Run `docs-boilerplate validate --output <project-root>` when available, otherwise verify navigation/metadata manually. The permanent project documentation, templates, technology standards and any independently selected skills **must remain**.

The interview and branch logic are in [questionnaire](.agents/bootstrap/questionnaire.md). Use [closure criteria](.agents/bootstrap/finish-checklist.md) before requesting approval.
