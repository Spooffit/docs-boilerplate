# ADR 0002: Opt-in Zensical documentation site

- Status: Superseded by [ADR 0004](0004-docsify-viewer.md)
- Date: 2026-10-27

## Context

Documentation must remain readable as Markdown and the website must not become a second content source. The generated repository already contains a canonical `docs/` directory.

## Alternatives

- **Astro Starlight**: excellent reading UX, navigation, search and editing links. Requires a Node/Astro project and often places sources under `src/content/docs`.
- **Docusaurus**: mature functionality and versioning but adds more JavaScript project structure than needed for a new project's internal documentation.
- **Zensical**: Markdown-native, straightforward `docs/` source directory, Mermaid support, local preview, static build and Git-host editing integration.

Official references: https://starlight.astro.build/guides/sidebar/ ; https://docusaurus.io/docs/versioning ; https://zensical.org/docs/setup/basics/ ; https://zensical.org/docs/authoring/diagrams/

## Decision

Offer `--site zensical` as an explicit opt-in setting. Generate `zensical.toml` in the project's root and put all generated Markdown standards under `docs/standards/`, alongside other documentation. The site consumes original Markdown without copying it. Avoid requiring Python for users who don't select a site. The first implementation pins Zensical in CI to a verified available release, **0.0.73**, while noting that the project was marked alpha in October 2026.

## Consequences

- A Python environment is needed to build or preview the optional site.
- Selecting no site adds no static-site dependencies or configuration.
- No site deployment is included. Publishing requires explicit user action.
- Review availability of major stable releases and alternatives during future maintenance.
