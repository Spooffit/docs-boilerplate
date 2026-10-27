#!/usr/bin/env python3
"""Track historical Q15/Q20 source links without pretending absent code is verified.

This guard deliberately checks *only* the two large historical decision logs.
The known missing paths are legacy evidence debt, not links in generated docs.
"""
from __future__ import annotations

import posixpath
import re
from pathlib import Path
from urllib.parse import unquote

ROOT = Path(__file__).resolve().parents[1]
SOURCES = (
    "technical/backend/questions/Q15-api-and-source-code.md",
    "technical/backend/questions/Q20-persistence-and-migration.md",
)
KNOWN_MISSING = {
    "archive/current-state-code-snapshot-2026-10-01.md",
    "archive/identity-access-architecture.md",
    "technical/backend/f03-demo-implementation-plan.md",
    "technical/backend/f05-demo-implementation-plan.md",
    "technical/backend/sync-wire-v1-vectors.md",
}
LINK = re.compile(r"\]\(([^)\s]+)(?:\s+[^)]*)?\)")


def classify(source: str, target: str, exists) -> tuple[str, str]:
    raw = unquote(target).split("#", 1)[0].split("?", 1)[0]
    if not raw or raw.startswith("#") or re.match(r"^[a-z][a-z0-9+.-]*:", raw, re.I):
        return "not-local", target
    if raw.startswith("/"):
        return "root-absolute", raw
    normalized = posixpath.normpath(posixpath.join(posixpath.dirname(source), raw))
    if normalized.startswith(("../contentmanager.api/", "../contentmanager.ui/")):
        return "unmounted-sibling-repo", normalized
    if normalized.startswith("../"):
        return "unknown-outside-repo", normalized
    return ("available-local" if exists(normalized) else "missing-local"), normalized


def selftest() -> None:
    source = SOURCES[0]
    def present(path: str) -> bool:
        return path == "technical/backend/features/F09-content-releases.md"
    assert classify(source, "../features/F09-content-releases.md#api", present)[0] == "available-local"
    assert classify(source, "../../../archive/missing.md", present) == (
        "missing-local", "archive/missing.md")
    assert classify(source, "../../../../contentmanager.api/src/Old.cs", present)[0] == "unmounted-sibling-repo"
    assert classify(source, "https://example.org/a", present)[0] == "not-local"


def run() -> None:
    selftest()
    missing = set()
    unexpected = []
    total = {}
    for source in SOURCES:
        text = (ROOT / source).read_text(encoding="utf-8")
        counts = {}
        for match in LINK.finditer(text):
            category, path = classify(source, match.group(1), lambda p: (ROOT / p).is_file())
            counts[category] = counts.get(category, 0) + 1
            if category == "missing-local":
                missing.add(path)
                if path not in KNOWN_MISSING:
                    line = text.count("\n", 0, match.start()) + 1
                    unexpected.append(f"{source}:{line} -> {path}")
            elif category in {"unknown-outside-repo", "root-absolute"}:
                unexpected.append(f"{source} -> unsupported {category}: {path}")
        total[source] = counts

    assert KNOWN_MISSING.issuperset(missing), "Unreviewed missing historical evidence detected"
    for source, counts in total.items():
        print(source, counts)
    print("Known unavailable evidence references:", ", ".join(sorted(missing)))
    if unexpected:
        raise AssertionError("Unexpected historical references:\n" + "\n".join(unexpected))
    print("Q15/Q20 evidence-link baseline verified. Missing sources remain unverified.")


if __name__ == "__main__":
    run()
