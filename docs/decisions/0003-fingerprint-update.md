# ADR 0003: Preview-first, fingerprint-checked updates

- Status: Accepted
- Date: 2026-10-27

## Context

A generated documentation repository becomes owned and edited by its project team. Re-running initialization cannot tell a deliberate user edit from a stale boilerplate file. Silently overwriting is unacceptable.

## Decision

During initialization, store a SHA-256 digest of each generated file's UTF-8 content under `GeneratedHashes` in `docs.config.json`. The configuration itself is excluded from the hash map to avoid a self-referential digest. The hash map and selected profiles, skills, modules and site define the last known template baseline.

The `update` command:
1. Renders the **same selection** against the current catalog.
2. Compares the current filesystem bytes to the old baseline and the new template bytes.
3. Classifies each owned path as unchanged, protected user edit, safe replacement or a concurrent-edit conflict.
4. Rejects any collision with an unmanaged new path.
5. Preserves obsolete files in place while dropping their generator ownership.
6. Reports an explicit preview and requires `--apply` to write.
7. Applies approved changes via same-directory temporary files and records the new baseline in `docs.config.json` last.

The update stops before writing if **any** preflight conflict exists. Runtime errors trigger a best-effort rollback. No automatic deletion and no `--force` flag.

## Backward compatibility

Projects initialized by earlier catalog versions did not record baseline hashes. They remain valid documentation, but the updater **must not infer ownership**. Create a fresh target with the current generator and manually reconcile historical content.

## Consequences and limitations

- Local edits to a file are preserved when the source template for that file is unchanged.
- If both the author and generator change a file, the author must resolve the conflict manually.
- Reapplying an already applied update is idempotent.
- Generator selection changes are not supported by this update command; profile migration is a separate future feature.
- Safety assumes one writer; crash-consistent multi-file transactions and hostile filesystem race prevention require stronger infrastructure.
- Projects should use Git history for review and recoverability.
