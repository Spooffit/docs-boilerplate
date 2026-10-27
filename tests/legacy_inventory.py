#!/usr/bin/env python3
"""Audit manifest invariants without changing or deleting historical source content."""
from __future__ import annotations

import csv
from pathlib import Path
from collections import Counter
from urllib.parse import unquote
import posixpath
import re

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "docs/audit/legacy-inventory.csv"
TYPES = {
    "project-source",
    "candidate-technology-standard",
    "candidate-template",
    "root-config-or-guidance",
}
STATES = {"pending", "screened-structure", "reviewed-targeted", "reviewed-complete", "rejected", "removed-cleanup"}
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
        if row["content_review"] == "removed-cleanup":
            assert not (ROOT / path).exists(), f"Marked removed-cleanup but still exists: {path}"
        else:
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

    removed = {
        row["path"] for row in rows if row["content_review"] == "removed-cleanup"
    }
    report = (ROOT / "docs/audit/legacy-removals.md").read_text(encoding="utf-8")
    for path in removed:
        assert f"`{path}`" in report, f"Missing recorded removal rationale for {path}"

    # Guard old and new documentation against inline links to removed paths.
    # Reference-style links and dynamic references still need manual review.
    inline = re.compile(r"\\]\\(<?([^\\s)<>]+)>?(?:\\s+[^)]*)?\\)")
    for doc in ROOT.rglob("*.md"):
        if ".git" in doc.parts:
            continue
        relative = doc.relative_to(ROOT).as_posix()
        body = doc.read_text(encoding="utf-8-sig")
        for match in inline.finditer(body):
            target = unquote(match.group(1)).split("#", 1)[0].split("?", 1)[0]
            if not target or "://" in target or target.startswith(("#", "/")):
                continue
            normalized = posixpath.normpath(posixpath.join(posixpath.dirname(relative), target))
            assert normalized not in removed, (
                f"Markdown link to removed historical file: {relative} -> {normalized}"
            )

    print(f"Legacy inventory: {len(paths)} entries")
    for (category, state), amount in sorted(counts.items()):
        print(f"  {category} / {state}: {amount}")
    print("Inventory structure and source-file preservation checks passed.")


if __name__ == "__main__":
    main()
