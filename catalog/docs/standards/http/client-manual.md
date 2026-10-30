---
title: "Manually authored REST API clients"
description: "Optional technology-specific engineering standard"
kind: standard
status: active
---

# Manually authored REST API clients

**Scope:** Select this option explicitly in the wizard; a catalog entry alone does not prove an implementation exists.

## Guidance

Use an owned, typed HttpClient wrapper with DI, handler ownership and explicit timeouts. Base DTOs, routes, headers and error handling on the published server contract. Do not retry uncertain write outcomes indiscriminately. Test actual serializer settings and error/status mapping against a real or contract-faithful server.

## Verification

Record actual versions, authoritative sources and executed tests. Do not import historical product-specific entities or workflows into reusable guidance.
