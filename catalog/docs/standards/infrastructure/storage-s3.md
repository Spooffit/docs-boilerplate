---
title: "S3-compatible object storage"
description: "Draft infrastructure profile for selecting an S3-compatible storage API"
kind: standard
status: draft
---

# S3-compatible object storage

**Placeholder, not a production deployment standard.** Select this profile when the project's infrastructure uses the S3 API, including S3-compatible endpoints.

Document the actual service/provider, endpoint and TLS requirements, bucket ownership, bucket policies, credentials and secret rotation, lifecycle/retention policy, multipart behavior, monitoring, backups and data recovery. Keep a clear distinction between the **storage service** (for example a MinIO server or AWS S3) and the **application-side S3 library**.

Do not insert a hardcoded server address, access key, bucket name or deployment command into this reusable template. Confirm the chosen server's supported S3 features and version before implementing compatibility assumptions.

The general object-storage upload, access and cleanup standard is installed as a dependency; this profile does not imply a specific S3 client.
