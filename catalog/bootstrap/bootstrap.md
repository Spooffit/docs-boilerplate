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
5. Populate the existing project and architecture overviews with grounded, concise answers, preserving `draft` until facts and authority justify promotion. Documentation modules and persistent agent skills are already installed by default. Fill the glossary scaffold only if verified terminology exists; otherwise leave its placeholder untouched. Do not invent features or implementations to fill blank documents.
6. Preserve the stable frontmatter `id` when editing or moving documents, and assign a unique ID to each new page under `docs/`. Update immediate directory indexes and `docs/catalog.md` when adding documents. Follow the installed standards that actually apply to the project; infrastructure placeholders are explicitly not accepted operational instructions.
7. Summarize what was learned, what remains unknown, the documents changed, and ask for **explicit owner confirmation** that stage two is complete. Do not claim completion on the basis of a successful structural validation.
8. With approval, preview removal using `docs-boilerplate bootstrap-complete --output <project-root>` and then run `docs-boilerplate bootstrap-complete --output <project-root> --apply`. Only four known intake files are removed, after verifying they have not been edited. If the tool is unavailable, retain the files and report the limitation; do not claim cleanup.
9. Run `docs-boilerplate validate --output <project-root>` after stage-two edits when available, otherwise verify navigation/metadata manually. The generator has already validated initial output on creation, but subsequent edits need another check. All default permanent skills, project document scaffolds, templates and selected technology standards **must remain**.

The interview and branch logic are in [questionnaire](.agents/bootstrap/questionnaire.md). Use [closure criteria](.agents/bootstrap/finish-checklist.md) before requesting approval.
