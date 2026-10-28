---
title: "Opt-in direct HTTP DTO models for UI forms"
description: "Bind UI edits directly to selected API request DTOs without a mandatory ViewModel layer"
kind: standard
status: active
---

# Opt-in direct HTTP DTO models for UI forms

**Applies when:** the `ui-direct-dto-forms` profile has been explicitly selected for a UI that consumes an HTTP contract and has suitable API request DTO types. The DTO types may be supplied by the project’s existing client tooling. **This profile specifies form-state use, not how client code or model types are produced.** It requires the basic [HTTP contract](../http/client-contracts.md) and [accessible form guidance](forms-and-accessibility.md), but it does not require Blazor, an additional client-library architecture, a state-management framework or a new application layer.

## Direct editing is the selected default

- For an editable form, prefer the **API write/request DTO itself** as the form input model when its mutability, nullability and validation semantics are appropriate. Do not create a parallel ViewModel and duplicate property mapping by default.
- Distinguish a read/response DTO from an editable request DTO. Server-managed identity, timestamps, permissions, computed properties and fields absent from the update contract must not become user-editable merely because they appear in a response.
- Initialize an independent form value from actual, authorized source data. Avoid binding a mutable form directly to a shared cache object where unsaved edits would become externally visible.
- Use the adopted contract’s requiredness and null/omitted behavior. HTML input normalization and local validation improve feedback but do not replace server-side validation or authorization. Do not fabricate defaults to bypass required fields.
- On submit, send only the contractually permitted request and retain user changes until the server confirms success. Handle loading, field errors, transient failures, concurrency/precondition rejection and uncertain outcomes explicitly. Prevent stale responses from overwriting newer edits.
- Treat ETag and other transport metadata as metadata, not DTO properties, unless the public contract explicitly represents them as fields. The optional ETag profile applies independently.

## When direct DTO binding is not safe

Some API types are immutable, read-only, polymorphic or combine display-only and write fields. When a selected DTO cannot represent safe user edits without violating the public contract, **do not mutate or bypass its invariants**: use a deliberate form adapter or project-specific ViewModel for that form, or revise the contract through an approved API change. This is an exception justified by actual requirements, not an automatic second model layer for every form.

## Verification

Check an actual form’s empty/loading/error/success states, input normalization, nullability, cancellation, stale responses, validation feedback, accessibility, authorized request payload and concurrency conflicts. Demonstrate that unsaved edits do not modify shared authoritative state and that UI success is displayed only after the real API confirms it. This is a documentation convention, **not** a generated UI, compiler, client package or application scaffold.
