---
title: "Stable document identifiers"
description: "Default stable identity rules for all project documentation"
kind: standard
status: active
---

# Stable document identifiers

Every Markdown page under `docs/` has a unique stable `id` in YAML frontmatter. The generator assigns deterministic IDs to all initially installed pages. Keep the existing ID when a document is renamed or moved; file paths and document identifiers serve different purposes.

For a manually created page, choose a new unique ID consistent with the existing document IDs. A human-readable title and kebab-case file path are still required. Do not derive a business feature ID, timestamp, approval decision or imaginary global sequence from the document's ID.

The CLI validates presence, syntax and uniqueness of IDs across `docs/`. It does not verify organizational acceptance. Ordinary `status` metadata means document drafting or maintenance state, **not** approval by a product owner or a business workflow. No separate approval-status system is included.
