# Blazor component and UI state guidance

**Applies to:** projects explicitly using Blazor (interactive server, WebAssembly, hybrid or static SSR as actually selected).

## State and component lifecycle

- Know the distinction among initialization, parameter changes, rendering and disposal. `OnInitializedAsync` is not a universal place to reload data that changes with route or component parameters; consider `OnParametersSetAsync` when appropriate.
- Keep the component in a renderable state before awaiting incomplete tasks.
- Cancel long-running work when a component is disposed or superseded. Prevent stale responses from overwriting newer UI state.
- Dispose subscriptions, timers and cancellation resources. Avoid triggering updates on disposed components.
- Choose where durable state belongs (URL, scoped service, browser storage or server) from actual feature needs; do not store secrets in browser-visible state.

## Interactions and API boundaries

- Distinguish an editable form's local state, submitted request, accepted server operation and persisted result. Do not show success before the authoritative server confirms it.
- Generated API clients may help keep DTOs aligned with OpenAPI, but a client-side form is never a proof of server authorization.
- Handle transient failures and unknown outcomes of writes distinctly. Do not blindly retry non-idempotent requests.
- Use validation messages accessible to assistive technologies and appropriate keyboard behavior.

## Rendering and performance

Blazor has different hosting and rendering models; do not assume every app uses the same prerendering, JS interop or authorization behavior. In server prerendering, initialization may execute more than once. Avoid unexpected duplicate external effects.

Preserve stable component keys when lists reorder and measured UI state otherwise moves incorrectly. Profile rendering and network behavior rather than prematurely introducing state frameworks or components.

## Verification

Component tests can verify UI behavior in isolation (bUnit is an optional profile); browser tests verify actual DOM, focus, network and JavaScript behavior. Test loading, empty, error, cancellation and authorization-visible states.

Reference: [Blazor component lifecycle](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/lifecycle?view=aspnetcore-10.0).
