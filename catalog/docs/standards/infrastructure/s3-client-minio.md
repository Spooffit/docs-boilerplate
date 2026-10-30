---
title: "MinIO .NET SDK S3 client"
description: "Draft optional client choice for S3-compatible object storage"
kind: standard
status: draft
---

# MinIO .NET SDK S3 client

**Placeholder, not a validated implementation example.** Applies to applications that explicitly choose the MinIO .NET client library to access an S3-compatible service.

Record the actual SDK version, configured endpoint, TLS, region expectations, credentials source, cancellation/timeouts, retries and multipart behavior. Validate interoperability against the chosen server (AWS S3 or an S3-compatible provider); an SDK choice does not select or deploy a MinIO server.

Use the supported public APIs of the installed MinIO client. Handle permission failures, incomplete uploads, retry uncertainty and object visibility according to the authoritative application contract. No credentials or bucket defaults belong in this template.

References: [MinIO .NET SDK](https://github.com/minio/minio-dotnet).
