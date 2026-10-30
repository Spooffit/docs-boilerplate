---
title: "NSwag REST API clients"
description: "Optional technology-specific engineering standard"
kind: standard
status: active
---

# NSwag REST API clients

**Applies only when the profile is selected.** Not an approved architecture by itself.

## Guidance

Generate typed clients from an authoritative versioned OpenAPI contract with pinned tooling. Keep generated output separate from handwritten code; verify nullable fields, errors, HttpClient lifetime and actual server compatibility.

## Verification

Verify installed versions and real contracts. Test integration and security boundaries separately.
