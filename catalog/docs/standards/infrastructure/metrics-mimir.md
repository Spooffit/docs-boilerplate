---
title: "Grafana Mimir metrics backend"
description: "Optional technology-specific engineering standard"
kind: standard
status: active
---

# Grafana Mimir metrics backend

**Applies only when the profile is selected.** Not an approved architecture by itself.

## Guidance

Mimir is a horizontally scalable Prometheus-compatible metrics backend. Define ingestion, remote-write, object storage, retention, tenant isolation and query behavior only for the actual topology. Do not conflate Mimir with Prometheus scraping or Alloy collection.

## Verification

Verify installed versions and real contracts. Test integration and security boundaries separately.
