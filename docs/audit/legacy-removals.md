---
title: "Initial historical placeholder cleanup"
description: "Reviewed removal of seven empty or placeholder-only legacy files"
kind: audit
status: active
---

# Initial historical cleanup: placeholders

- Date: 2026-10-27
- Scope: exactly **seven** files, all inspected directly before removal.
- Outcome: remove them from current `main` while preserving normal Git history. Remaining substantive historical documents are untouched.

## Removal decisions

| File | Actual content and reason for removal |
| --- | --- |
| `.ai/mcp/mcp.json` | Empty JSON file (zero bytes), provides no MCP configuration |
| `business/adr/README.md` | Short request to add an ADR index in the old project, not an ADR or standard |
| `business/archive/README.md` | Short request for an old-project archive index, not authoritative content |
| `technical/backend/adr/README.md` | Placeholder text only, no actual decision |
| `technical/backend/archive/README.md` | Placeholder text only, no actual archived source |
| `technical/frontend/adr/README.md` | Placeholder text only, no actual decision |
| `technical/frontend/archive/README.md` | Placeholder text only, no actual archived source |

The six README placeholders were the **only files** in their corresponding old `adr/` or `archive/` directories. The empty MCP file was likewise the only file in `.ai/mcp/`.

These files are replaced for new projects by intentional generator behavior: real `docs/index.md` navigation, `docs/templates/adr.md`, actual ADR records where chosen, and selected Agent Skills. No artificial empty folder is generated.

## Link and safety checks

The audit looked for explicit repository references to these removed filenames, and CI now checks that surviving Markdown does not link to a removed path. This covers conventional inline Markdown links; it is not a full general-purpose Markdown parser and does not imply all historical anchors resolve.

The legacy inventory retains the seven paths as `removed-cleanup` so subsequent audits cannot silently confuse historical removal with an original file that never existed.

This review does **not** authorize removal of any substantive feature, module, technical standard, question, product glossary or milestone. Later removals must receive individual disposition and evidence before editing this allowlist.

## Follow-up removal: C# formatter rule

- Removed path: `technical/backend/standards/S24-code-formatting.md`.
- Source inspection: the entire file was a 339-byte instruction to run `dotnet format --include` after changing C# files. Its title incorrectly used `S23` even though the filename was `S24`, colliding with the separate project-file standard.
- Inbound-reference check: the full Markdown graph reported **zero** incoming documents to the S24 path. The old root `standards.md` indexed S01–S23, not S24. The link checker does not prove absence of every plain-text or HTML mention.
- Migrated value: the formatting-and-verification rule now lives in `catalog/docs/standards/dotnet/conventions.md`, included only with the `.NET` profile. The new version respects actual repository formatter settings and requires reporting when formatting was not executed.
- Reason: no unique product contract or example needs preserving in the current documentation tree. The original text remains accessible through normal Git history.
- Deletion gate: no current recognized Markdown link may reference this path; `tests/legacy_link_graph.py` enforces it in CI.
