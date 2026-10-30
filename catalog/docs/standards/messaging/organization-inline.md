---
title: "Application-owned messaging endpoints"
description: "Technology-scoped engineering standard"
kind: standard
status: active
---

# Application-owned messaging endpoints

**Only applies when the matching profile is explicitly selected.**

## Rules

Keep message handlers and consumer endpoints within the owning application or module. Share only explicitly stable integration DTOs with real consumers. The chosen transport and library are independent; do not force a separate solution-project hierarchy for small systems. Validate composition and actual broker topology.

## Verification

Record actual versions, settings and executed test evidence; the template does not decide them for the project.

## Contract and ownership boundaries

- Keep wire-contract types independent of private entity models even when handlers and transport registration live in the same application. Publish external contracts only when there are real external consumers.
- Assign ownership for endpoints, schema changes, subscriptions and handling of failed messages. Internal modular separation does not imply a new deployable service or a fixed solution layout.
- Verify that the application's composition root registers only selected broker/client combinations and does not unintentionally configure unused transports.
