---
title: "bUnit component testing"
description: "Technology-scoped engineering standard"
kind: standard
status: active
---

# bUnit component testing

**Applies to:** Blazor projects that deliberately use bUnit. This profile selects a test library, not a particular rendering mode or production UI component framework.

## What a component test proves

bUnit renders Razor components within a test context, supplies parameters and services, dispatches UI interactions and inspects markup or component-visible behavior. It is not a real browser, backend or complete end-to-end journey.

- Prefer assertions about visible state, disabled controls, messages, navigation and callback behavior rather than private component fields.
- Exercise loading, validation, errors, cancellation, empty states and interactive transitions that belong to the component.
- Use controlled test doubles at HTTP/client or dependency boundaries; do not claim a fake response verifies a live API contract.
- Keep a fresh or intentionally isolated test context for test cases with mutable services.
- Dispose rendered components and fixtures to prevent callbacks or timers leaking across tests.

## Stable isolated component tests

- Arrange inputs and required dependency doubles in a fresh, independently disposed bUnit test context. Supply parameters, cascading values and only the needed registered services; do not share mutable renderer/DI state across parallel cases.
- Drive normal component events such as click, change and submit, or an actual public callback, instead of directly calling private event-handler methods. Assert meaningful text, labels, disabled state, parameters, emitted requests and callbacks; avoid coupling to internal fields, render counts or framework-generated CSS classes.
- Use semantic `MarkupMatches` on the relevant fragment when HTML comparison is useful. Snapshots of large third-party component trees are not a substitute for behavior checks. Stub a child only for intentionally isolated parent behavior; a stubbed child does not prove real child composition.
- Configure bUnit JS interop expectations deliberately and prefer strict verification of required calls. A fake `IJSRuntime` can establish that .NET invoked JavaScript but cannot prove actual browser DOM, layout or browser JavaScript execution.
- For asynchronous renders use `WaitForAssertion` or `WaitForState` and set bounded timeouts appropriate to the test. Never place click/submit/mutations inside a retried assertion. Use controlled task completion to test loading, error, cancellation, repeated actions and stale responses rather than arbitrary sleeps.
- A fake API adapter or controlled `HttpMessageHandler` can test component-side requests; the adapter itself needs real transport/serialization boundary tests, and a genuine server/UI combination needs separate integration or end-to-end verification.

References: [bUnit asynchronous assertions](https://bunit.dev/docs/verification/async-assertion.html), [bUnit JS interop](https://bunit.dev/docs/test-doubles/emulating-ijsruntime.html).

## Architecture-aware test placement

If the project **selects Feature-Sliced Design (FSD)**, group tests with the owner of the relevant UI behavior: Shared primitives, Entities display/state, Features interactions, Widgets composition, Pages routing and App providers/bootstrap. Use the *actual* public dependencies and slice boundaries in architecture tests; don't automatically introduce a test for every layer or force FSD onto every Blazor project. Keep test-only builders in test projects, not production Shared.

Component tests with fake API clients remain component tests even when several real child components are rendered. A test of a client's HTTP adapter has a different SUT; a cross-application journey with real services can be E2E. Verify only the boundary that the test genuinely executes. Naming policy is chosen by the project; behavior-focused naming is a safe neutral default, not a mandatory `MethodUnderTest_Scenario_ExpectedBehavior` pattern.

Reference: [FSD layers](https://feature-sliced.design/docs/reference/layers) and [FSD public API](https://feature-sliced.design/docs/reference/public-api).

## Browser and separately hosted UI verification

Use a real browser suite (Playwright for .NET is one option) for behavior bUnit cannot execute: focus, keyboard, JS-heavy controls, deep-link/reload, downloads, static asset bootstrap, network credentials, real CORS/cookies/CSRF and independently hosted UI/API origin boundaries. A browser using a mocked API is *not* evidence that the live backend contract works.

Favor stable role/name/label or test-ID locators, automatic actionability waits and retryable *pure* assertions over DOM-index selectors and sleeps. Give parallel tests isolated browser contexts and data. Capture safe diagnostics on failures without leaking credentials. The project chooses actual browsers, deployment topology, runner and test budgets.

References: [Playwright .NET locators](https://playwright.dev/dotnet/docs/locators), [auto-wait](https://playwright.dev/dotnet/docs/actionability), [web assertions](https://playwright.dev/dotnet/docs/test-assertions).

## Runner integration

Verify bUnit compatibility with the actual test framework and runner. Its documented integrations include xUnit, NUnit and MSTest; **do not assume an unverified TUnit integration is supported merely because TUnit is an MTP-based runner**. When combining frameworks, confirm the selected package versions and run a compilation/example test.

## Complementary checks

Use browser-level automation for real keyboard navigation, focus behavior, CSS/layout, JavaScript interop, client routing and genuine network integration. Component tests and E2E serve different scopes.

References: [bUnit writing tests](https://bunit.dev/docs/getting-started/writing-tests.html), [Blazor lifecycle](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/lifecycle?view=aspnetcore-10.0).
