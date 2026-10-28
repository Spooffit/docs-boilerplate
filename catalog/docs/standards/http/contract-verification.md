---
title: "HTTP API contract verification and compatibility"
description: "Verify published wire contracts against clients and deployed behavior"
kind: standard
status: active
---

# HTTP API contract verification and compatibility

**Applies when:** an HTTP API has published consumers or compatibility requirements.

## Contract authority

Identify the approved public contract, its version or reference, and the deployed behavior being tested. OpenAPI or another machine-readable description may be authoritative where adopted. A sample controller or historical document is not equivalent evidence of deployed behavior.

Review the selected endpoint's method, URL, request/response types, requiredness, nullability, error body, status codes, headers, security scope and serialization. Mark unverified behavior explicitly; do not create endpoints or DTOs to fill documentation gaps.

## Compatibility review

For changes affecting consumers, consider route constraints, query defaults, newly required fields, unknown enum values, missing versus `null`, numeric precision, dates and offsets, collections, content types, pagination, authentication, conditional requests and error mapping. Include only concerns actually exposed by the public contract.

A language-level compatible change may still break a wire consumer. Distinguish property names from display labels and field meanings where the schema defines them. Do not impose a specific domain field-identity or historical-version model.

Validate the actual consumer runtime, especially response headers, errors, type/nullability handling and serializer behavior. UI representation choices are covered separately by explicitly selected form-state conventions, not by the public API contract itself.

## Evidence and rollout

For each changed operation, record the accepted target contract, observed implementation, observed consumer compatibility if known, executed test evidence and any mismatch. A historical source revision is not proof of a deployed route. A proposed test matrix with no successful runs is a plan, not verification.

Assess old/new consumer compatibility and rollout sequence only for deployments that must coexist. Document applicable deprecation and rollback expectations; do not invent an API versioning scheme.

## Verification

Exercise the actual transport and selected client/runtime. Test required versus omitted values, unauthorized access, error formats, status/header consistency and backward compatibility according to confirmed support commitments. A successful build alone cannot prove wire compatibility.

References: [RFC 9110](https://www.rfc-editor.org/rfc/rfc9110), [OpenAPI Specification](https://spec.openapis.org/oas/latest.html).
