---
title: "HTTP API contract verification and compatibility"
description: "Review authoritative wire contracts, generated clients, versions and evidence without inventing endpoints"
kind: standard
status: active
---

# HTTP API contract verification and compatibility

**Applies when:** a project publishes or consumes HTTP endpoints whose behavior must remain compatible with deployed clients. This is a documentation and verification standard, not a required API framework.

## Source of truth and scope

Identify the **authoritative** API contract and the actual deployed behavior. Link the published OpenAPI specification or equivalent machine-readable description where one exists. An example controller signature, historical documentation, generated SDK and internal DTO are evidence of different things.

| Concern | Authoritative source to check |
| --- | --- |
| Resource address and HTTP method | Adopted public contract and deployed routing |
| Input requiredness, nullability and response shape | Published schema plus actual binding/serialization behavior |
| Conditional request headers and status mapping | Selected HTTP contract and actual endpoint tests |
| Security and permission scope | Server policy and authorized endpoint behavior |
| Code-generated client | Versioned contract inputs, generator configuration and resulting build artifact |
| Historical compatibility | Versioned schemas, deployment policy, supported consumers and observed tests |

Do not fill unknown entries by copying names, error envelopes, route prefixes, response codes or custom bus types from another project. Mark unverified implementation claims explicitly as **Not verified**.

## Contract-change review

For every externally visible change, review:

- Method, URL, query defaults, route constraints and headers.
- Required/optional fields, missing versus `null`, enum values and unknown fields.
- Success/error status, error schema, pagination, sorting and conditional concurrency semantics.
- Serialization details, date/time and numeric representation, content type and streaming.
- Authorization scope, exposure of previously internal operations and redaction of sensitive data.
- Client compatibility, version coexistence, rollout order, deprecation and rollback plan.

A modification that seems source-compatible in one language may be a wire-breaking change for another consumer. A successful OpenAPI generation step cannot establish runtime compatibility by itself.

## Generated clients and meaningful models

Generated SDK code should be reproducible from its pinned contract and generator settings. Verify compilation in the **target SDK environment** and required runtime behaviors, including access to headers and problem responses. Keep presentation state and transport metadata deliberately owned by the client; do not force every UI or domain type to equal a generated transport DTO.

When a generated client needs a missing API capability, change and review the **actual server contract** or explicitly implement an adapter. Do not maintain an unnoticed second, divergent copy of the public contract.

## Generated model and source-code conformance (when applicable)

Where a team generates typed models or source code from a schema registry, verify that generation consumes an **immutable, identified schema contract**, not guessed properties from example instance JSON or arbitrary runtime reflection. Separate the source schema's identity and interpretation from the generated-language mapping and any independently versioned binary/serialization profile. A rename of an authoring label must not accidentally redefine historical field identity when the contract distinguishes them.

Record the selected JSON Schema dialect or equivalent contract language, required versus omitted properties, `null`, unknown fields, numeric precision, date/time zone and offset semantics, enum fallback, nested collections and any extension annotations needed to preserve meaning. Do not assume JSON Schema `number` establishes a specific decimal representation or that a timestamp without offset represents UTC. Source-language types and generated-language types must be tested against the actual wire codec, not only against similar-looking declarations.

For generated source, test identifier normalization, reserved keywords and collisions, deterministic output where the product requires it, and duplicate or incompatible input definitions. When producing a source bundle, distinguish deterministic **source content** from optional deterministic archive/container bytes. Neither is implied merely by a successful compiler invocation. Test generated code in a real consuming project against the chosen serializer and target runtime; source generation alone does not prove it can deserialize, round-trip or load the adopted schema.

If a consumer uses multiple runtimes (for example a desktop build and a restricted game-engine runtime), verify each promised environment rather than substituting a host-only .NET unit test for native dependencies, ahead-of-time compilation, platform threading or packaging compatibility. Editor tests and player/runtime tests answer different questions. Only require such gates for platforms the project has actually selected.

## Per-operation evidence comparison

For complex contracts or partially implemented API migrations, document the same operation across distinct authorities without copying all fields:

| Operation | Accepted target source | Existing source/contract version | Generated client status | Runtime/test evidence | Unresolved mismatch |
| --- | --- | --- | --- | --- | --- |
| [Real operation] | [Approved requirement or Not established] | [Commit and actual schema or Not verified] | [Generated from pinned version or Not verified] | [Executed contract test or Not verified] | [Exact difference] |

A controller file found in a historical archive is not proof that its replacement route exists. A proposed public contract does not become current simply because a design review accepted it. The client generator and the deployed service can lag the source schema independently.

If original source links point outside the current repository or to missing files, record them as **unverified evidence dependencies**, not as a passing implementation check. Do not use a broken citation to justify new endpoints, DTOs or error mappings.

## Versioned rollout and verification matrix

| Scenario | Intended behavior | Test boundary | Evidence |
| --- | --- | --- | --- |
| Old client with current API | [Confirmed compatibility requirement] | [Real HTTP integration] | Not yet verified |
| Current client with older API | [Only if rollout requires it] | [Deployed-version integration] | Not yet verified |
| Invalid/unauthorized request | [Actual status and format] | [End-to-end route/policy] | Not yet verified |
| Lost write acknowledgement | [Replay/inquiry contract, if any] | [Fault-injection test] | Not yet verified |
| Stale conditional update | [Actual selected concurrency contract] | [Concurrent mutation test] | Not yet verified |
| Unknown response/field value | [Selected reader behavior] | [Serializer/client test] | Not yet verified |

Record the specific API version, generated client version and test execution evidence. A proposed matrix with all rows unverified is a plan, not passing conformance.

References: [RFC 9110](https://www.rfc-editor.org/rfc/rfc9110), [OpenAPI Specification](https://spec.openapis.org/oas/latest.html).
