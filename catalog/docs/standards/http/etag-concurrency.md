---
title: "Opt-in HTTP strong ETag and If-Match concurrency contract"
description: "Portable conditional-write wire examples, status semantics and atomicity verification"
kind: standard
status: active
---

# Opt-in HTTP strong ETag and If-Match concurrency contract

**Applies when:** the `http-etag-concurrency` profile is selected for HTTP resources with an explicitly adopted **strong ETag / If-Match lost-update contract**. The base [HTTP client contract standard](client-contracts.md) does not require this mechanism. HTTP paths and tag values below are intentionally generic example values, not project routes or domain requirements.

## Wire contract and strong comparisons

1. The origin server returns an `ETag` for the selected resource representation (including any negotiated representation differences). Preserve the quoted opaque value exactly. Never construct a client-side tag from an editable field or treat a weak `W/"..."` tag as strong.
2. When an update requires optimistic concurrency, the client sends `If-Match` with the last retrieved strong validator for that representation. A server compares it **strongly** against the current selected representation **before applying** the state change, within a concurrency-safe commit or equivalent conditional update.
3. On success, return the appropriate success status and the new validator if one is available for the resulting representation. On a stale validator the usual response is `412 Precondition Failed`; **no update occurs**. `If-Match: *` only asserts an existing current representation, not a particular revision.
4. If this API explicitly requires a precondition but it is missing, it can reject with `428 Precondition Required` (RFC 6585). `409 Conflict` is for an independent application-level conflict and is not a substitute for failed `If-Match`. Validate malformed headers per the adopted API contract; do not silently drop them.
5. Precondition evaluation follows HTTP semantics, including whether the method would otherwise succeed. Check authorization and target existence according to the adopted API; do not let conditional headers disclose a protected resource. `If-None-Match` has different semantics and is not an alias for `If-Match`.

## Reference request/response scenarios

Each pair is an **illustrative** HTTP/1.1 exchange for one generic resource `/resources/42`. The sequence starts with current tag `"rev-7"`, then one successful update changes it to `"rev-8"`. The tests check the scenario tags, expected statuses and ETag transitions; they are **not** a live HTTP server test.

```http scenario=read
GET /resources/42 HTTP/1.1
Host: api.example.test

HTTP/1.1 200 OK
Content-Type: application/json
ETag: "rev-7"

{"value":"before"}
```

```http scenario=success
PUT /resources/42 HTTP/1.1
Host: api.example.test
Content-Type: application/json
If-Match: "rev-7"

{"value":"after"}

HTTP/1.1 204 No Content
ETag: "rev-8"
```

```http scenario=stale
PUT /resources/42 HTTP/1.1
Host: api.example.test
If-Match: "rev-7"
Content-Type: application/json

{"value":"outdated"}

HTTP/1.1 412 Precondition Failed
```

```http scenario=weak
PUT /resources/42 HTTP/1.1
Host: api.example.test
If-Match: W/"rev-8"
Content-Type: application/json

{"value":"cannot-match-strongly"}

HTTP/1.1 412 Precondition Failed
```

```http scenario=missing
PUT /resources/42 HTTP/1.1
Host: api.example.test
Content-Type: application/json

{"value":"without-guard"}

HTTP/1.1 428 Precondition Required
```

```http scenario=conflict
PUT /resources/42 HTTP/1.1
Host: api.example.test
If-Match: "rev-8"
Content-Type: application/json

{"value":"violates-an-independent-constraint"}

HTTP/1.1 409 Conflict
```

## Implementation and verification

- **Atomicity:** two concurrent writes carrying the same current ETag cannot both overwrite each other. Compare-and-write under an appropriate transaction, version condition, or equivalent storage guarantee; checking the ETag in an earlier request handler step alone is insufficient.
- **Regression cases:** read validator, successful conditional update with new validator, stale rejection leaving data unchanged, weak-tag rejection, required-but-missing precondition, authorized/nonexistent resource behavior, and unrelated `409` with a matching tag.
- **Retry behavior:** a timeout after a conditional write is an uncertain outcome. A retry with the old tag may receive `412` because the first write committed. Re-read/reconcile or use an approved idempotency mechanism; never silently refresh and repeat the overwrite.
- **Coverage boundary:** `tests/optional_profile_contracts.py` validates the example exchanges against a deterministic reference model. Every consuming project must still test its own actual API, storage transaction and security behavior.

References: [RFC 9110, section 13.1.1 If-Match](https://www.rfc-editor.org/rfc/rfc9110#section-13.1.1), [RFC 9110, section 8.8.3.2 strong comparison](https://www.rfc-editor.org/rfc/rfc9110#section-8.8.3.2), [RFC 6585, section 3 (428)](https://www.rfc-editor.org/rfc/rfc6585#section-3).
