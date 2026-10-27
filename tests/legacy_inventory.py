#!/usr/bin/env python3
"""Audit manifest invariants without changing or deleting historical source content."""
from __future__ import annotations

import csv
from pathlib import Path
from collections import Counter

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "docs/audit/legacy-inventory.csv"
TYPES = {
    "project-source",
    "candidate-technology-standard",
    "candidate-template",
    "root-config-or-guidance",
}
STATES = {"pending", "screened-structure", "reviewed-targeted", "reviewed-complete", "rejected"}
ROOT_SOURCE_FILES = {"features.md", "modules.md", "questions.md", "standards.md"}


def main():
    with SOURCE.open(encoding="utf-8", newline="") as stream:
        rows = list(csv.DictReader(stream))
    assert rows, "Legacy manifest must not be empty"
    paths = set()
    counts = Counter()
    for row in rows:
        path = row["path"]
        assert path not in paths, f"Duplicate inventory path: {path}"
        assert path and not path.startswith("/") and ".." not in Path(path).parts, f"Unsafe inventory path: {path}"
        assert row["classification"] in TYPES, f"Unknown classification for {path}"
        assert row["content_review"] in STATES, f"Unknown review status for {path}"
        assert (ROOT / path).is_file(), f"Historical source missing without reviewed removal: {path}"
        paths.add(path)
        counts[(row["classification"], row["content_review"])] += 1

    tracked_source = {
        item.relative_to(ROOT).as_posix()
        for directory in ("business", "technical", "milestones")
        for item in (ROOT / directory).rglob("*")
        if item.is_file()
    }
    tracked_source.update(x for x in ROOT_SOURCE_FILES if (ROOT / x).exists())
    assert tracked_source.issubset(paths), (
        f"Uninventoried legacy source files: {sorted(tracked_source - paths)}"
    )

    print(f"Legacy inventory: {len(paths)} entries")
    for (category, state), amount in sorted(counts.items()):
        print(f"  {category} / {state}: {amount}")
    print("Inventory structure and source-file preservation checks passed.")


if __name__ == "__main__":
    main()
