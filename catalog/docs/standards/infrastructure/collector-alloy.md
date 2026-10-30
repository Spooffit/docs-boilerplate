---
title: "Grafana Alloy telemetry collection"
description: "Optional technology-specific engineering standard"
kind: standard
status: active
---

# Grafana Alloy telemetry collection

**Applies only when the profile is selected.** Not an approved architecture by itself.

## Guidance

Alloy receives, processes and exports telemetry using OpenTelemetry Collector pipelines. Specify ingestion, routes, credentials, resource budgets and backpressure. It is not a metrics database; validate signal routing in the real deployment.

## Verification

Verify installed versions and real contracts. Test integration and security boundaries separately.
