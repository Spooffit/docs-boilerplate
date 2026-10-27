# ADR 0001: Composable, declarative documentation profiles

- Status: Accepted
- Date: 2026-10-27

## Context

The source repository mixes project-specific specifications with standards that are useful in more than one software project. A generated repository should contain only selected, maintainable documents. The CLI must not require AI and agent skills must not fork generation rules.

## Decision

Use a small .NET 10 CLI and a data-driven catalog of profiles, templates, standards and skill sources. The CLI resolves profile dependencies and conflicts, creates an explicit generation plan, and validates the result. A profile declares which files it contributes. Supported profile IDs compose without assuming DDD, CQRS, microservices or an ORM.

The generated repository stores a small `docs.config.json` with selected profiles and generated-file ownership information. The generator will reject collisions and existing differing target files rather than silently overwriting them. Catalog files are only used during initialization and are not copied wholesale.

Use plain Markdown and relative links as the document transport format. No custom template language beyond explicit, escaped placeholders for a few initialization fields. Keep technology-specific rules out of the default core.

## Consequences

- New technology support mostly changes manifests and content.
- The generator is not a full plugin runtime.
- Generator upgrades and applying new technology profiles require an explicit migration strategy.
- A documentation site remains optional; source Markdown stays usable without the site.
- Complexity is traded for predictability rather than maximum feature count.
