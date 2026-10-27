# bUnit component testing

**Applies to:** Blazor projects that deliberately use bUnit. This profile selects a test library, not a particular rendering mode or production UI component framework.

## What a component test proves

bUnit renders Razor components within a test context, supplies parameters and services, dispatches UI interactions and inspects markup or component-visible behavior. It is not a real browser, backend or complete end-to-end journey.

- Prefer assertions about visible state, disabled controls, messages, navigation and callback behavior rather than private component fields.
- Exercise loading, validation, errors, cancellation, empty states and interactive transitions that belong to the component.
- Use controlled test doubles at HTTP/client or dependency boundaries; do not claim a fake response verifies a live API contract.
- Keep a fresh or intentionally isolated test context for test cases with mutable services.
- Dispose rendered components and fixtures to prevent callbacks or timers leaking across tests.

## Runner integration

Verify bUnit compatibility with the actual test framework and runner. Its documented integrations include xUnit, NUnit and MSTest; **do not assume an unverified TUnit integration is supported merely because TUnit is an MTP-based runner**. When combining frameworks, confirm the selected package versions and run a compilation/example test.

## Complementary checks

Use browser-level automation for real keyboard navigation, focus behavior, CSS/layout, JavaScript interop, client routing and genuine network integration. Component tests and E2E serve different scopes.

References: [bUnit writing tests](https://bunit.dev/docs/getting-started/writing-tests.html), [Blazor lifecycle](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/lifecycle?view=aspnetcore-10.0).
