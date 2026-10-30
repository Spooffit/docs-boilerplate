---
title: "Swagger UI for ASP.NET Core"
description: "Optional technology-specific engineering standard"
kind: standard
status: active
---

# Swagger UI for ASP.NET Core

**Applies only when the profile is selected.** Not an approved architecture by itself.

## Guidance

Swagger UI is an interface for OpenAPI documents, not a specification generator. Configure Microsoft.AspNetCore.OpenApi or Swashbuckle independently. Limit exposure by environment or authorization; verify the OpenAPI route and security schemes.

## Verification

Verify installed versions and real contracts. Test integration and security boundaries separately.
