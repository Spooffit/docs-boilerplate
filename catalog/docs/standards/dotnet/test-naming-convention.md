---
title: "Opt-in .NET test method naming convention"
description: "Adopt MethodUnderTest_Scenario_ExpectedBehavior without prescribing a test framework"
kind: standard
status: active
---

# Opt-in .NET test method naming convention

**Applies when:** the `test-naming-convention` profile is explicitly selected. The base [.NET testing standard](testing.md) remains framework-neutral; selecting `dotnet`, TUnit or bUnit does not implicitly select this naming rule.

## Shape

Use `MethodUnderTest_Scenario_ExpectedBehavior` for test method names, with three meaningful segments separated by underscores:

- **MethodUnderTest:** the public method, endpoint operation, rule or behavior exercised. For a multi-component workflow choose the stable observable operation, not a private helper or arbitrary `Execute`.
- **Scenario:** the specific input, state or trigger that distinguishes this case.
- **ExpectedBehavior:** the externally observable outcome, including a relevant error or side effect.

Example names (illustrative signatures, independent of a runner):

```csharp
public void Normalize_WhitespaceOnly_ReturnsEmptyString() { /* assertions */ }
public void Calculate_NegativeQuantity_RejectsInput() { /* assertions */ }
public async Task Submit_DuplicateRequest_DoesNotRepeatSideEffect() { /* assertions */ }
```

Use the spelling, casing and async signature supported by the chosen framework. Keep scenario descriptions concise but distinct. Do not encode setup implementation, incidental data values or class names solely to satisfy the pattern. Parameterized tests can share a method name if the framework reports each case clearly; ensure each failure still identifies its input.

## Adoption and scope

This is an explicitly selected **naming convention**, not a requirement to adopt TUnit, xUnit, NUnit, a test folder layout, layered architecture or a particular mocking library. Apply to new and touched tests according to the project adoption policy; do not bulk-rename tests merely to conform without review. A project may use another established naming scheme unless this profile was selected.

**Verify:** naming checks may inspect the adopted suite for three meaningful parts; do not confuse name conformity with assertion quality or verified coverage.
