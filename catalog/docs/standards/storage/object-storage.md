---
title: "Object storage, uploads and binary delivery"
description: "Optional technology-neutral guidance for secure binary handling and metadata/byte consistency"
kind: standard
status: active
---

# Object storage, uploads and binary delivery

**Applies when:** a project accepts uploaded files or exposes managed binary objects through a storage service. This does **not** require S3, MinIO, a reverse proxy, presigned URLs, a specific client framework or a MediaAsset domain model.

## Ownership and public boundaries

Specify who owns the logical file/asset identity, authoritative metadata, binary content, access policy, and physical storage location. A user-visible filename, logical asset ID and object-storage key may differ; define their mapping rather than assuming a particular `Guid` naming convention.

Public contracts should expose only stable identifiers and metadata intended for consumers. Bucket names, credentials and internal object paths should not become public API by accident. If the project deliberately issues temporary signed URLs, document their audience, permissions, expiry and revocation limits.

## Validating an upload

- Choose allowed content types and size ceilings from actual product constraints. Apply limits on the server and configure infrastructure accordingly; client picker filters are only usability hints.
- Never trust an uploaded filename, extension or claimed `Content-Type` alone. Validate against the adopted policy, inspect format signatures/container structure where feasible and use malware scanning where appropriate. Signature identification is **not** proof the file is safe or decodable by every client.
- Use application-assigned safe physical names, avoiding traversal and overwrites. Do not place untrusted files in an executable web root.
- Prefer streaming/bounded buffering for potentially large uploads; document quotas for concurrent uploads, temporary storage and incomplete multipart sessions.
- If checksums are used, state the algorithm and whether it covers original bytes, encrypted bytes or a transformed representation. An object-store `ETag` is not automatically SHA-256 or MD5 of the logical file.

## Metadata, storage and partial failure

An object store and application metadata database usually do **not** share one transaction. Document the actual stages and recovery path for:

1. Initial request/intent persisted, if any.
2. Streaming upload, size/content verification and checksum computation.
3. Completed object storage and independently persisted authoritative metadata.
4. Transition to user-visible/available state only after required checks.
5. Crash/timeout between any two stages: retry behavior, recovery, cleanup of incomplete uploads and orphaned bytes.

Document which process owns cleanup and how failed/in-progress objects become visible to operators. Do not claim `Available` merely because object upload bytes were received.

## Immutable references and lifecycle

Decide whether replacing bytes mutates the same logical identity or creates a new one. If published historical content must remain reproducible, define retention and versioning at the correct boundary.

Before deletion, determine which drafts, published versions, external references, caches and retained historical records still require the object. Do not equate "metadata query returned no references" with proof of no use if some authoritative dependency source is unavailable. Previewed impact can change before deletion; revalidate the precise dependency set when the workflow requires it.

Archive, retire, withdrawal, physical removal and restoration are separate optional semantics. Do not introduce all of them just because a previous project had them.

## Access, caching and partial reads

For authenticated delivery, decide where authorization is enforced **before** revealing metadata, `304 Not Modified`, `206 Partial Content` or bytes. The mere possession of an object ID or checksum is not an access grant. Specify private/public cache scope, expiry, validators and authorization-sensitive `Vary` behavior where applicable.

If HTTP Range is supported, verify `If-Range`, `206`, `416` and fallback `200` behavior for the **same representation**. Do not assume byte ranges and an application's resumable-record cursor are interchangeable.

## Verification

Test mismatched MIME/signatures, executable payloads, filename/path attacks, oversized streams, incomplete uploads, crashes after object write but before metadata commit, duplicate requests, forbidden downloads, deletion while referenced, range/cache validators and historical reads. Use the actual object provider and access gateway for integration claims.

References: [ASP.NET Core secure file upload guidance](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads), [AWS multipart object upload and checksum behavior](https://docs.aws.amazon.com/AmazonS3/latest/userguide/mpuoverview.html), [RFC 9110 Range Requests](https://www.rfc-editor.org/rfc/rfc9110.html#section-14).
