#!/usr/bin/env python3
"""Conservative incoming-link graph and deletion gate for legacy Markdown.

Supports ordinary inline Markdown links and reference definitions, not every
CommonMark, HTML or Docsify link variant. Does not verify external evidence.
"""
from __future__ import annotations

from collections import defaultdict
import csv
from pathlib import Path
import posixpath
import re
from urllib.parse import unquote

ROOT = Path(__file__).resolve().parents[1]
INLINE = re.compile(r"\]\(\s*<?([^\s)<>]+)>?(?:\s+[^)]*)?\)")
REFERENCE = re.compile(r"^\s{0,3}\[[^\]]+\]:\s*<?([^\s<>]+)>?", re.M)
FENCE = re.compile(r"^\s{0,3}(\x60{3,}|~{3,})")
REMOTE = re.compile(r"^[a-z][a-z0-9+.-]*:", re.I)


def link_targets(source: str):
    """Return destinations outside fenced code blocks."""
    fenced = None
    for line in source.splitlines():
        marker = FENCE.match(line)
        if marker:
            kind = marker.group(1)[0]
            if fenced is None:
                fenced = kind
            elif fenced == kind:
                fenced = None
            continue
        if fenced is not None:
            continue
        yield from (match.group(1) for match in INLINE.finditer(line))
        yield from (match.group(1) for match in REFERENCE.finditer(line))


def resolve(document: str, target: str) -> str | None:
    decoded = unquote(target).split("#", 1)[0].split("?", 1)[0]
    if not decoded or decoded.startswith("/") or REMOTE.match(decoded):
        return None
    path = posixpath.normpath(posixpath.join(posixpath.dirname(document), decoded))
    if path == ".." or path.startswith("../"):
        return None
    return path


def test_parser():
    source = """
[normal](../standards/S01.md#intro)
[encoded](../standards/My%20File.md)
[external](https://example.com/not-local.md)
[details][ref]

[ref]: ../standards/S02.md "Reference"
~~~
[code](../standards/not-a-link.md)
~~~
"""
    targets = {
        resolve("technical/backend/features/F01.md", link)
        for link in link_targets(source)
    }
    assert targets == {
        "technical/backend/standards/S01.md",
        "technical/backend/standards/My File.md",
        "technical/backend/standards/S02.md",
        None,
    }
    assert resolve("docs/audit/index.md", "../../technical/backend/standards/S01.md") == (
        "technical/backend/standards/S01.md")
    assert resolve("docs/audit/index.md", "../../../../etc/passwd") is None
    # The deletion guard must reject a currently linked removed document.
    try:
        assert_no_removed_links({"old.md": {"docs/index.md"}}, ["old.md"])
    except AssertionError:
        pass
    else:
        raise AssertionError("Removed-file references were incorrectly accepted")
    assert_no_removed_links({"safe.md": {"docs/index.md"}}, ["old.md"])


def assert_no_removed_links(inbound, removed):
    broken = [(path, sorted(inbound[path])) for path in removed if inbound.get(path)]
    if broken:
        raise AssertionError("Current Markdown still links to removed history:\n"
                             + "\n".join(f"{path} <- {refs}" for path, refs in broken))


def analyze():
    with (ROOT / "docs/audit/legacy-inventory.csv").open(encoding="utf-8", newline="") as handle:
        rows = list(csv.DictReader(handle))
    inbound = defaultdict(set)
    scanned = 0
    for document in ROOT.rglob("*.md"):
        if ".git" in document.parts:
            continue
        scanned += 1
        source = document.relative_to(ROOT).as_posix()
        text = document.read_text(encoding="utf-8-sig")
        for link in link_targets(text):
            target = resolve(source, link)
            if target is not None:
                inbound[target].add(source)

    removed = [row["path"] for row in rows if row["content_review"] == "removed-cleanup"]
    assert_no_removed_links(inbound, removed)

    standards = sorted(row["path"] for row in rows
        if row["classification"] == "candidate-technology-standard"
        and row["content_review"] != "removed-cleanup")
    zero = []
    print(f"Scanned {scanned} Markdown files; {len(standards)} historical standards.")
    for path in standards:
        sources = sorted(inbound[path] - {path})
        print(f"{len(sources):3} incoming documents | {path} | example: {', '.join(sources[:2])}")
        if not sources:
            zero.append(path)
    print(f"Zero-inbound standards (requires manual review): {len(zero)}")
    for path in zero:
        print("  ZERO-INBOUND", path)
    print(f"Removed files without recognized incoming links: {len(removed)}")
    print("Limit: HTML/dynamic/bare-text references and fenced examples are not counted.")
    return {"zero": zero, "removed": removed}


if __name__ == "__main__":
    test_parser()
    analyze()
