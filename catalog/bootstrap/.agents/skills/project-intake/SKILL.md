---
name: project-intake
description: Guide a user through evidence-based two-stage project documentation initialization
---

# Project intake (temporary skill)

Use only while `bootstrap.md` exists. Read it first, then use `.agents/bootstrap/questionnaire.md` for adaptive interview questions and `.agents/bootstrap/finish-checklist.md` for completion.

Ask about problem, objectives, related repositories, dependent services and contracts, existing architecture, infrastructure, milestones and useful glossary terms. Follow up conditionally, distinguish unknown from verified and never fabricate business logic, dates, owners or technology implementations.

Edit the **permanent** project and architecture overview documents, add only needed pages and repair navigation. Request approval of the resulting baseline. Then trigger the explicit `bootstrap-complete --apply` cleanup, which checks file bytes before deleting temporary intake materials. Do not delete other skills or project documents. If the installed CLI is unavailable, do not pretend the cleanup ran.
