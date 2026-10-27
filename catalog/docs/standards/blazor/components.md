# Blazor component guidance

**Applies to:** projects using Blazor.

- Keep component state and async lifecycle transitions explicit.
- Avoid mutating state after disposal; handle cancellation for long-running operations.
- Separate API contracts from UI state where needed.
- Expose accessible labels, keyboard operation and meaningful status messages.
- Use component tests and browser tests for their respective boundaries; bUnit is a possible component-testing tool.
- Do not assume a particular rendering mode or hosting model.
