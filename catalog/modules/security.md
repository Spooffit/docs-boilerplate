---
title: "Security architecture"
description: "Project-specific document scaffold"
kind: project
status: draft
---

# Security architecture

> **Use:** Complete this optional document only from approved requirements, a verified threat model and actual implementation evidence. Delete inapplicable sections. The examples below are review prompts, not a prescribed identity provider, role system, session lifetime, cookie configuration or framework.

## Scope, assets and trust boundaries

Identify protected information and operations, human/service/browser actors, relevant external identities and data owners. Distinguish identity used to sign in from authorization to act on a particular resource. State which systems own credentials, permissions, business records and audit evidence; a documentation module does not automatically own a database or bounded context.

Identify the deployment and trust boundaries of browsers, APIs, reverse proxies, service calls and administrative tools, but only where they exist. Record the accepted security scope for each delivery stage. A deliberately unauthenticated prototype does not prove that a later authenticated product is safe.

## Identity, credentials and sessions (when applicable)

Document the selected authentication method, credential lifecycle, secure provisioning and recovery paths, session creation, expiration, logout and revocation guarantees. Specify the security effect of account disablement, password reset and permission changes, including whether already-authorized in-flight operations may complete. Distinguish per-request evidence from stale UI or cached role claims.

Record which provider owns password hashing, credential reset, replay prevention and account state. Never duplicate credential stores or assume a default password, open registration, multi-factor policy or session duration without an approved requirement. State who can recover administration access and how concurrent access changes preserve any selected administrative-availability invariant.

## Authorization and resource ownership

Record enforcement **at the server or trusted operation boundary**, not merely in client navigation. Authentication, permission for an operation, ownership/scope of the requested resource and its current business-state invariants are separate checks. UI-visible capabilities and hidden/disabled buttons are advisory and may become stale. A trusted background actor is not an invented privileged human account.

| Operation / protected result | Actor and scope | Authoritative policy or resource owner | Denied/changed-state outcome | Verification evidence |
| --- | --- | --- | --- | --- |
| [Actual operation] | [Confirmed identity and resource scope] | [Real checker/owner] | [Specified result] | [Actual test or Not verified] |

Define roles, permissions and inheritance only if chosen by the project. Include negative cases: unauthorized direct API calls, different identities, stale grants, changed resource ownership and concurrent revocation. Prefer least privilege, deny by default, and verify authorization on each protected request.

## Browser, API and cross-origin protections (when applicable)

For cookie-authenticated browser requests, document actual origin/site boundaries, cookie scope and security attributes, credentialed requests and the selected CSRF defenses for state-changing operations. **CORS is not authorization or a substitute for CSRF protection**; `SameSite` by itself is not a universal CSRF guarantee, including between same-site subdomains. Verify the real browser/API deployment and relevant framework/version behavior rather than guessing from a local same-origin test.

For non-browser clients, record separately how their credentials are provisioned, stored, scoped and revoked; do not reuse a browser login flow merely because both call the same HTTP API.

## Unsaved work, identity switches and uncertain outcomes (when applicable)

Specify what happens to in-progress user input if a session expires, permissions change or a different account signs in. If recovery is supported, define its lifetime, actor isolation, original resource identity and concurrency baseline, what is retained, and what is deliberately not persisted. A fresh server read must not silently turn old input into a write against a newer version.

Do not automatically replay an uncertain state-changing request after reauthentication or a transport timeout; use an authorized outcome inquiry or a verified idempotency contract. Exclude secrets, cookies, credential codes and authentication headers from form recovery snapshots and diagnostics.

## Data protection, audit and operational evidence

State key/secret ownership, persistence, rotation and multi-replica compatibility where applicable. Keep security diagnostics separate from product activity history where their audience, retention or confidentiality differs. Define what may be logged, who can read it, how failures are distinguished, and whether audit delivery is synchronous or eventually consistent.

Where a durable activity or security audit is required, establish the authoritative source and classification for each fact, rather than inferring the result from an HTTP status, a button click or an application log. Record trusted actor identity versus automated executor/initiator, typed resource/version identity and safe historical labels. For asynchronous delivery, capture the intent durably with the actual committed source change; specify replay-safe receiving and lag/error handling. The chosen policy determines which failed attempts, security denials and completed business effects are recorded; these may belong to **different** histories.

Protect audit records and their metadata through a documented allowlist and reader policy. Enforce visibility before counts, pagination and filter suggestions; an audit-reader permission does not bypass the current target owner's authorization. Keep unrelated credentials, payloads, access tokens, private URLs and full before/after content out of a convenient activity summary. Define retention, privacy exceptions and removal of personal data from real approved obligations, not a default infinite-history promise.

| Threat / requirement | Selected mitigation and owner | Implementation version/reference | Negative or failure test | Status |
| --- | --- | --- | --- | --- |
| [Confirmed risk] | [Approved control] | [Observed source or Not verified] | [Actual scenario] | [Planned / Implemented / Verified] |

Do not treat a written mitigation, generated client, test plan, or unverified link as execution evidence. Record missing dependencies and owner decisions explicitly.

## References

- [OWASP Authorization Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Authorization_Cheat_Sheet.html)
- [OWASP CSRF Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Cross-Site_Request_Forgery_Prevention_Cheat_Sheet.html)
- [ASP.NET Core antiforgery guidance](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0) (only for projects selecting that stack/version)
