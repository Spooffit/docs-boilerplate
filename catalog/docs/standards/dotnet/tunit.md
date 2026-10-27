---
title: "TUnit and Microsoft.Testing.Platform"
description: "Technology-scoped engineering standard"
kind: standard
status: active
---

# TUnit and Microsoft.Testing.Platform

**Applicability:** .NET projects that explicitly choose TUnit.

TUnit is a .NET test framework built on Microsoft.Testing.Platform (MTP). Framework APIs and test discovery behavior depend on the installed version. Do not assume command-line flags or extension APIs from a different framework.

## Runner configuration

For .NET 10 SDK and a deliberate MTP setup, set the following in `global.json`:

```json
{
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

Verify that TUnit and the selected MTP version support the target SDK. A runner choice must be applied consistently to local development, test filters and CI commands.

## Test lifecycle

- Use TUnit fixture and lifecycle mechanisms actually supported by the selected package version.
- Do not share mutable state across concurrently executing test cases without explicit isolation.
- Avoid cross-test dependencies and order-sensitive assertions.
- Keep resource cleanup deterministic, including cases that fail during setup.
- Publish MTP-compatible test results using supported extensions and command-line options.

## Important compatibility checks

1. Confirm package versions and target frameworks are supported together.
2. Run the exact `dotnet test` command from CI locally.
3. Verify filtering, reporting and coverage flags with the selected platform.
4. Verify the test process exits non-zero for a failing test.
5. Keep a small failing-test smoke case in the project toolchain tests when changing runner versions.

References: [MTP overview](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-intro), [MTP migration](https://learn.microsoft.com/en-us/dotnet/core/testing/migrating-vstest-microsoft-testing-platform).
