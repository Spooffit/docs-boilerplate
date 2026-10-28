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

Link the actual authoritative API contract, such as OpenAPI or another adopted wire schema, when it exists. Do not invent a machine-readable contract or maintain a competing Markdown copy.

## Client compatibility (when applicable)

For real consumers, document required compatibility, headers and error behavior. The separate `http-contracts` profile is optional and does not follow automatically from selecting this documentation module.

## Compatibility and versions

Document the project's actual versioning, deprecation and change-management policy.

## Authentication, authorization and errors

Describe adopted mechanisms and failure semantics with references to the source contracts.

## Validation

List contract checks and any consumer-driven tests that are actually executed.
