---
title: "ADR 0004: Markdown-first Docsify viewer"
description: "Documented architectural decision"
kind: adr
status: active
---

# ADR 0004: Markdown-first Docsify viewer

- Status: Accepted
- Date: 2026-10-27

## Context

The repository is designed primarily for AI agents reading Markdown source. A static-site generator creates redundant artifacts, build tooling and potential navigation drift.

## Decision

Use Docsify as the only built-in browser-side Markdown renderer. Fresh repositories include a single root `index.html` by default; `--site none` disables it. Markdown remains in `docs/`, with `docs/index.md` as the home page and `index.md` files for directory overviews. The viewer loads Docsify from CDN and uses `basePath: "docs/"` and `relativePath: true`. Do not require a Docsify CLI, npm package, build tree, underscore navigation files or a separate content repository.

## Tradeoffs

- A local HTTP server is needed for reliable browser viewing; opening `file://...` by double-clicking usually fails because browsers block JavaScript fetching local Markdown.
- The viewer uses CDN-delivered assets; offline browser viewing would require separately hosted assets.
- Client-rendered pages differ from pre-rendered HTML in indexability and JavaScript requirements.
- `llms.txt` and original Markdown remain readable independently of Docsify.

## Previously generated repositories

The only supported site selections are `docsify` and `none`. Other saved site identifiers are deliberately rejected. For an old project, generate a new Docsify repository and reconcile documentation manually in Git; the updater never silently deletes or rewrites content it cannot safely identify.

References: https://docsify.js.org/ , https://github.com/docsifyjs/docsify/blob/develop/docs/configuration.md , https://developer.mozilla.org/en-US/docs/Web/HTTP/Guides/CORS/Errors/CORSRequestNotHttp
