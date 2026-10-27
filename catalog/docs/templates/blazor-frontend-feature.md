---
title: "Blazor frontend feature: [Capability]"
description: "Reusable writing template"
kind: template
status: template
---

# Blazor frontend feature: [Capability]

> **Use:** Describe the UI structure and behavior of one feature. This template is opt-in through the Blazor profile. It does not require MudBlazor, FSD, NSwag, a central state store or a particular hosting/rendering model.

## User outcome and navigation

[Who uses this interface? Which real page, route or component makes it available? What does the user accomplish?]

## Composition and responsibilities

[Describe pages and child components with a simple text tree only if the feature actually has them. Avoid prescribing Razor markup, pixels or CSS values as architecture.]

| Component/block | Responsibility | Inputs | Outputs |
| --- | --- | --- | --- |
| [Name] | [Purpose] | [Confirmed] | [Events / state changes] |

## State ownership and transitions

[Distinguish initial, editing, loading, validation, saving, success, failed and unknown network-result states.]

| State | Owner | Transition trigger | Visible behavior |
| --- | --- | --- | --- |
| [Name] | [Page / component / URL / service] | [Action] | [What user sees] |

## Interaction flows

1. [Describe verified user action.]
2. [Explain API calls or client calculations, with an actual contract reference.]
3. [Describe updates, concurrency conflicts, cancellation and retry controls.]
4. [Explain navigation and feedback following the authoritative operation result.]

## Data and permissions

[Link verified DTO/source contract. Distinguish local UI guards from server authorization, which remains authoritative. Include client privacy requirements if they apply.]

## Accessibility and responsive behavior

[Document keyboard/focus, labels, error announcements and UI constraints derived from real requirements. Avoid invented screen sizes.]

## Test scenarios and acceptance

[Record component tests for state transitions and browser tests for actual focus, JS integration and navigation. Do not assert bUnit or other tools were chosen until confirmed.]

## Open questions

[Explicitly record missing UI decisions and link to their owner. Do not fill gaps with assumed components.]
