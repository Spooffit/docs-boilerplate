---
title: "Manual mapping / extension methods"
description: "Optional technology-specific engineering standard"
kind: standard
status: active
---

# Manual mapping / extension methods

**Scope:** Select this option explicitly in the wizard; a catalog entry alone does not prove an implementation exists.

## Guidance

Write clear C# constructors, projections or extension methods at explicit boundaries. Keep mapping focused and test nontrivial conversions, null handling, normalization and security-sensitive field exclusion. Prefer simple direct code over introducing a reflection framework without a demonstrated need.

## Verification

Record actual versions, authoritative sources and executed tests. Do not import historical product-specific entities or workflows into reusable guidance.
