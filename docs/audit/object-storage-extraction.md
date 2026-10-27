---
title: "Object storage and media lifecycle extraction"
description: "Targeted review of the historical object storage gateway and file lifecycle specification"
kind: audit
status: active
---

# Object storage and media lifecycle extraction

- Date: 2026-10-28
- Source: `technical/backend/questions/Q21-media-storage.md`, compared with the introductory implementation/evidence sections of `technical/backend/features/F05-media-assets.md`.
- Method: reviewed the source's identity/object-naming, format validation, upload/lifecycle, reference coordination, recovery, proxy/access, caching and Range sections. The entire ~80 KB F05 technical implementation and all historical code links were **not** reverified. This is a **targeted semantic extraction**, not a full implementation audit.
- Inventory: Q21 promoted to `reviewed-targeted`; F05 remains `screened-structure`. Neither historical file is authorized for deletion.

## What carries over

| Original source decision | Generalizable documentation requirement |
| --- | --- |
| MinIO behind a private storage adapter and CM/YARP public gateway | Separate logical asset identity, public API, authoritative metadata, physical object store and authorized bytes delivery; adapter/provider choice remains project-specific |
| Exact `AssetId` object-name rule | Describe identity/key mapping, but do not require a GUID-derived key |
| Central extension/MIME/signature/size policy | Define and enforce actual server-side validation; file signature checks have limits |
| `PendingUpload/Available/Failed/Deleting` and immutable file bytes | Document selected lifecycle, metadata/object dual-write recovery, and historical retention |
| Deletion guard across Draft/Published/orphan references | Discover and revalidate exact authoritative dependencies; incomplete dependency information is not zero references |
| Cache validators and partial media delivery | Document authorized access and correct HTTP conditional/Range semantics where supported |

## What remains exclusively historical

The source's `MinIO` provider, `IObjectStorage` adapter names, `YARP` routes, Unity formats, `AssetKind` enum, `Guid`-derived keys, exact permission flows, MIME matrix, multipart request names, confirmation timing and upload-size choices are **specific to the prior product**. None is required of a new project.

## Reusable outcome

The optional `object-storage` profile emits [object storage, uploads and binary delivery](../../catalog/docs/standards/storage/object-storage.md), independent of any database, programming language, HTTP framework, storage provider or other profile.

Cross-checks: [Microsoft upload security guidance](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads), [Amazon S3 multipart docs](https://docs.aws.amazon.com/AmazonS3/latest/userguide/mpuoverview.html) and [HTTP Range semantics](https://www.rfc-editor.org/rfc/rfc9110.html#section-14).

## Remaining cleanup gate

The backend F05 source includes extensive code references and current/historical implementation snapshots. They require independent verification and an owner decision on any preserved examples before deletion.
