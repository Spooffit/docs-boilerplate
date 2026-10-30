---
title: "xUnit.net testing for .NET"
description: "Optional xUnit.net test framework conventions"
kind: standard
status: active
---

# xUnit.net testing for .NET

**Applies when:** the project explicitly selects xUnit.net for .NET tests. This is an alternative to TUnit; select only one framework for base testing guidance.

- Use `[Fact]` for a test without inputs and `[Theory]` with data attributes for parameterized cases.
- Keep assertions deterministic and focused on observable outcomes. Avoid shared mutable fixtures that let one test affect another.
- Use async test methods returning `Task` for asynchronous behavior; avoid blocking on `.Result` or `.Wait()`.
- Make test fixtures and lifecycle explicit when using constructor setup, `IAsyncLifetime` or collection fixtures. Do not assume execution ordering.
- Separate unit tests from integration tests requiring real resources. Testcontainers and Aspire testing remain independently selectable supporting tools.
- Run tests with the actual SDK, test runner and xUnit package versions installed in the project. Do not assume a particular xUnit major version or runner integration without evidence.

References: [xUnit.net](https://xunit.net/), [.NET test](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test).
