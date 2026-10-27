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
