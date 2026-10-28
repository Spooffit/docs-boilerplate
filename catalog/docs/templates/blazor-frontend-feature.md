---
title: "Blazor frontend feature: [Capability]"
description: "Reusable writing template"
kind: template
status: template
---

# Blazor frontend feature: [Capability]

> **Use:** Optional Blazor-focused alternative to the general technical-feature template when UI behavior warrants its own document. Do not require MudBlazor, FSD, NSwag, a state store or a particular rendering model. Omit headings and tables that do not fit the actual feature.

## User outcome and navigation

[Who uses this interface? Which real page, route or component makes it available? What does the user accomplish?]

## Composition and responsibilities

[Describe pages and child components with a simple text tree only if the feature actually has them. Avoid prescribing Razor markup, pixels or CSS values as architecture.]

| Component/block | Responsibility | Inputs | Outputs |
| --- | --- | --- | --- |
| [Name] | [Purpose] | [Confirmed] | [Events / state changes] |

## State and feedback (when applicable)

[Document only observed or agreed UI states and their owners. Do not invent edit/save/network states for a static view.]

| State | Owner | Transition trigger | Visible behavior |
| --- | --- | --- | --- |
| [Name] | [Page / component / URL / service] | [Action] | [What user sees] |

## Interaction flow (when applicable)

[Describe real user actions, data dependencies, navigation and visible feedback. Include API calls, cancellation and retry only if the feature actually uses them; do not require a four-stage workflow.]

## Data and permissions

[Link verified DTO/source contract. Distinguish local UI guards from server authorization, which remains authoritative. Include client privacy requirements if they apply.]

## Accessibility and responsive behavior

[Document keyboard/focus, labels, error announcements and UI constraints derived from real requirements. Avoid invented screen sizes.]

## Test scenarios and acceptance

[Record the real verification needed for this UI and the evidence if executed. Component and browser tests may prove different things; do not assume a specific library or require a browser test for every static component.]

## Open questions

[Explicitly record missing UI decisions and link to their owner. Do not fill gaps with assumed components.]
