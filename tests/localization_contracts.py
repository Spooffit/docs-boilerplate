#!/usr/bin/env python3
"""Verify that each English catalog Markdown file has a Russian counterpart."""
from pathlib import Path
import json
import re
import sys

root = Path(__file__).resolve().parents[1] / "catalog"
ru = root / "locales" / "ru"
sections = ("base", "bootstrap", "docs", "modules", "skills")
source = {
    file.relative_to(root).as_posix(): file
    for section in sections
    for file in (root / section).rglob("*.md")
}
locales = {file.relative_to(ru).as_posix(): file for file in ru.rglob("*.md")}
errors = []
for name in sorted(source.keys() - locales.keys()):
    errors.append("Missing Russian file: " + name)
for name in sorted(locales.keys() - source.keys()):
    errors.append("Unexpected Russian file: " + name)

for name in sorted(source.keys() & locales.keys()):
    en = source[name].read_text(encoding="utf-8")
    translated = locales[name].read_text(encoding="utf-8")
    if len(re.findall("[А-Яа-яЁё]", translated)) < 20:
        errors.append("No substantial Russian text in " + name)
    for marker in ("{{PROJECT_NAME}}", "{{PROJECT_DESCRIPTION}}"):
        if marker in en and marker not in translated:
            errors.append("Missing placeholder in " + name + ": " + marker)
    if name != "base/README.md":
        def frontmatter(text):
            return dict(re.findall(r"(?m)^([a-z-]+):\s*(.+)$", text.split("\n---\n", 1)[0]))
        original = frontmatter(en)
        target = frontmatter(translated)
        for key in ("kind", "status", "name"):
            if original.get(key) != target.get(key):
                errors.append("Changed " + key + " in " + name)
    fence = chr(96) * 3
    def code_blocks(text):
        return re.findall("^" + fence + "[^\n]*\n.*?^" + fence + "[ \t]*$", text, re.M | re.S)
    for block in code_blocks(en):
        if block not in code_blocks(translated):
            errors.append("Changed code example in " + name)

profiles = json.loads((root / "profiles.json").read_text(encoding="utf-8"))["profiles"]
names = json.loads((ru / "profiles.json").read_text(encoding="utf-8"))
if set(names) != {p["id"] for p in profiles}:
    errors.append("Russian profile name list differs from catalog")
for error in errors:
    print("ERROR:", error)
print(f"Localization: {len(source)} source files, {len(locales)} Russian translations; {len(errors)} problems.")
sys.exit(bool(errors))
