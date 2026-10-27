---
title: "MudBlazor component library conventions"
description: "Technology-scoped engineering standard"
kind: standard
status: active
---

# MudBlazor component library conventions

**Applies to:** Blazor applications that explicitly use MudBlazor. Selecting MudBlazor requires Blazor; it does not imply a particular hosting model.

## Component selection and state

- Prefer documented MudBlazor public components and APIs from the **version actually installed**. Avoid depending on internal CSS classes or undocumented internals.
- Keep data, editing, loading and committed state distinguishable. Do not confuse a component's local validation with a server-confirmed write.
- For forms, verify when validation runs (on change, on submit or explicitly) and how failures surface to users.
- For data grids, define server-side pagination, sorting and filtering contracts explicitly rather than assuming local operations scale to large datasets.
- Keep UI composition modular but avoid wrapping every native component in a trivial application wrapper.

## Theming and accessibility

- Use theme tokens consistently for spacing, color and typography. Check contrast, focus indication, labels, keyboard interaction and error messages.
- Preserve semantic user interactions when creating custom wrappers. Visual appearance alone is not accessibility proof.
- Apply provider/service registration per the installed version; document the actual app setup in project files rather than prescribing placeholder startup code in a universal standard.

## Testing

Component tests should assert observable behaviors and validation output. Browser tests are needed to confirm actual keyboard, overlay, focus and responsive interactions.

Reference: [MudBlazor documentation](https://mudblazor.com/).
