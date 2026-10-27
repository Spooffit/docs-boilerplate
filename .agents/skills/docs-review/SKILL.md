---
name: docs-review
description: Review Docs-as-Code changes for correctness, ownership, links, duplication and compliance with the selected technology standards.
---

# Review a documentation change

1. Read the affected project's `README.md` and `docs.config.json` (if generated), or this template repository's `docs/architecture/overview.md`.
2. Identify the authoritative documents for each changed rule. Distinguish normative standards, ADRs, project decisions, templates and examples.
3. Check that claims are grounded in supplied requirements or inspected source, not plausible guesses.
4. Review internal relative links and uniqueness of normative rules; inspect adjacent references when a document moved.
5. Verify that selected technology profiles match the documents. Never invent compatibility for a planned profile.
6. Run the supported validator. In the template repository, build and run `tests/smoke.sh` if the CLI was affected.
7. Report the files reviewed, proven issues, changes proposed, successful checks and checks that could not be run.

Do not copy a standard into this skill; load the relevant standard when needed.
