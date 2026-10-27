---
title: "API design"
description: "Project-specific document scaffold"
kind: project
status: draft
---

# API design

## Consumers and responsibilities

Name actual consumers, transport choices and ownership boundaries.

## Contracts

Link the authoritative OpenAPI, Protocol Buffers, AsyncAPI or other published contract files. Do not maintain a second copy of the schema in Markdown.

## Consumers and generated SDKs

When the project has consumer SDKs, record the exact contract source and generator configuration, supported client versions, necessary response headers and fallback/error behavior. The standalone `http-contracts` profile offers an optional language-neutral review and test checklist; it does not choose NSwag, a frontend framework or an API server implementation.

## Compatibility and versions

Document the project's actual versioning, deprecation and change-management policy.

## Authentication, authorization and errors

Describe adopted mechanisms and failure semantics with references to the source contracts.

## Validation

List contract checks and any consumer-driven tests that are actually executed.
