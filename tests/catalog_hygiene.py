#!/usr/bin/env python3
"""Check reusable catalog metadata, naming and absence of non-template fixture markers."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "catalog"
ALLOWED_SPECIAL = {"README.md", "AGENTS.md", "SKILL.md", "CONTRIBUTING.md"}
NON_TEMPLATE_MARKER = re.compile(r"\bUNREUSABLE_PROJECT_CONTENT\b")
DOC_FILENAME = re.compile(r"^[a-z0-9]+(?:-[a-z0-9]+)*\.md$")
PROTECTED_TREES = ("base", "docs", "modules", "skills")
errors = []
checked = 0

for folder in PROTECTED_TREES:
    for doc in sorted((CATALOG / folder).rglob("*.md")):
        text = doc.read_text(encoding="utf-8")
        relative = doc.relative_to(ROOT).as_posix()
        checked += 1

        if not text.startswith("---\n") or "\n---\n" not in text[4:]:
            errors.append(f"{relative}: missing frontmatter")
        if doc.name not in ALLOWED_SPECIAL and not DOC_FILENAME.fullmatch(doc.name):
            errors.append(f"{relative}: filename must use lowercase kebab-case")

        for match in NON_TEMPLATE_MARKER.finditer(text):
            line = text.count("\n", 0, match.start()) + 1
            errors.append(f"{relative}:{line}: non-template content marker '{match.group(0)}'")

viewer_files = sorted(path.name for path in (CATALOG / "sites").iterdir() if path.is_file())
if viewer_files != ["docsify.html"]:
    errors.append(f"Unexpected site assets: {viewer_files}. Only docsify.html is supported.")

for error in errors:
    print("ERROR", error)
print(f"Active catalog hygiene: {checked} Markdown files examined; {len(errors)} findings.")
raise SystemExit(1 if errors else 0)
