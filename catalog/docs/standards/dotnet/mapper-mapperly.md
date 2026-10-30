---
title: "Riok.Mapperly source-generated mapping"
description: "Optional technology-specific engineering standard"
kind: standard
status: active
---

# Riok.Mapperly source-generated mapping

**Scope:** Select this option explicitly in the wizard; a catalog entry alone does not prove an implementation exists.

## Guidance

Prefer explicit source-generated mapper declarations and use compiler diagnostics to find unmapped required members. Review user-provided values versus server-owned fields, nullability and nested references. A successful build does not prove that conversions preserve domain invariants or wire compatibility.

## Verification

Record actual versions, authoritative sources and executed tests. Do not import historical product-specific entities or workflows into reusable guidance.
