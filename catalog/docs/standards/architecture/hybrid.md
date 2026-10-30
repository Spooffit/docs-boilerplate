---
title: "Hybrid monolith and microservices"
description: "Technology-scoped engineering standard"
kind: standard
status: active
---

# Hybrid monolith and microservices

**Only applies when the matching profile is explicitly selected.**

## Rules

Use a deployable monolith alongside independently deployable services only when specific scaling, organizational or integration requirements justify the hybrid. Explicitly define the monolith boundary, service responsibilities, data ownership, synchronous and asynchronous contracts, observability and deployment coordination. Avoid treating an internal module as a service until it has independent deployment and operational ownership. Test partial failure, retries and data consistency.

## Verification

Record actual versions, settings and executed test evidence; the template does not decide them for the project.
