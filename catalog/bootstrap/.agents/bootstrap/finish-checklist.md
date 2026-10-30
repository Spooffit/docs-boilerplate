---
title: "Stage two completion checks"
description: "Temporary review and safe cleanup checklist for AI-assisted project intake"
kind: guide
status: draft
---

# Stage two completion checks

- [ ] The project's purpose, audience and boundary are recorded at the level actually known.
- [ ] Existing sources and neighboring systems are identified or explicitly unknown.
- [ ] Backend and frontend applicability reflects actual decisions, including skipped choices.
- [ ] Related services and integrations are confirmed or labeled planned/unverified.
- [ ] Infrastructure selections are not misrepresented as implemented deployments.
- [ ] Actual milestones are recorded if supplied; none are fabricated.
- [ ] Useful terminology was added if available; otherwise no empty glossary was fabricated.
- [ ] New files have frontmatter and correct relative links, folder indexes and catalog entries.
- [ ] Open questions and their decision owners (if known) are visible.
- [ ] The user reviewed the stage-two summary and explicitly approved completion.

After approval: run `docs-boilerplate bootstrap-complete --output <project-root>` for the dry-run cleanup plan, then add `--apply` to remove the four unchanged temporary intake files. Keep every permanent document and other selected Agent Skills. Run `docs-boilerplate validate --output <project-root>`. If cleanup or validation fails, report the exact issue; never delete modified or unrelated files merely to make a check green.
