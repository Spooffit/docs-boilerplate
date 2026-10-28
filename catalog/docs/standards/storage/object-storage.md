---
title: "Object storage, uploads and binary delivery"
description: "Optional storage guidance for file safety, consistency and authorized access"
kind: standard
status: active
---

# Object storage, uploads and binary delivery

**Applies when:** a project uploads, stores or serves binary objects. It does not require S3, MinIO, an application metadata database, presigned URLs, asset pipelines or a special domain model.

## Ownership and access

Identify the logical resource owner, metadata authority, bytes storage, permitted consumers and access enforcement boundary. A public identifier, display filename and storage key need not be identical. Never expose credentials, private paths or bucket configuration merely because an internal provider uses them.

Where temporary signed URLs are selected, document authorization, permitted operations, lifetime and revocation limitations. Authorization must be enforced before revealing protected metadata or content, including cache and partial-response paths.

## Safe upload

- Enforce the project's approved size, format, quota and concurrency limits on trusted boundaries.
- Treat filename, extension and claimed `Content-Type` as untrusted. Validate supported formats as appropriate and consider malware scanning for the actual threat model.
- Prevent path traversal, unsafe execution and unintended overwrite. Prefer streaming or bounded buffering for large uploads.
- Where checksums exist, specify the exact representation and algorithm. An object-store `ETag` is not universally a file's MD5 or SHA-256.

## Consistency and cleanup

If binary storage and metadata persist independently, document the actual failure boundaries and recovery. An object write and relational database write generally do not share one transaction. Make an object visible as complete only after the checks promised by the selected application contract.

Account for incomplete multipart operations, orphaned objects, retries and deletion only where those situations are possible. If deletions must respect references or retention, obtain reliable evidence under the applicable concurrency rules; a failed reference lookup does not prove an object is unused. Do not invent publish, approval or archive states.

## Delivery and cache, when applicable

Define the authorized delivery mechanism, caching scope, validators, range support and privacy effects. If HTTP ranges are offered, verify `If-Range`, `206`, `416` and fallback `200` for the same byte representation.

A consumer-side cache is an optional design decision, not an implied part of object storage. When adopted, separate incomplete from verified content, specify ownership/eviction and consider authorization and representation changes. Runtime-specific textures, audio objects and other decoded resources belong to the selecting client/platform's design, not this standard.

## Verification

Cover chosen threats and failure boundaries: unsafe names, incorrect type or size, interrupted uploads, orphan cleanup, permission denial, corrupt bytes, referenced-object deletion and cache/range semantics. Verify against the selected storage provider and access path before claiming compatibility.

References: [ASP.NET Core secure file upload guidance](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads), [AWS multipart upload semantics](https://docs.aws.amazon.com/AmazonS3/latest/userguide/mpuoverview.html), [RFC 9110 Range Requests](https://www.rfc-editor.org/rfc/rfc9110.html#section-14).
