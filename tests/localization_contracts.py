#!/usr/bin/env python3
"""Audit localized source overlays without altering canonical output paths.

Full translation coverage is required. Invalid locale files,
metadata changes and damaged project placeholders are test failures. The CLI
itself rejects missing translations when a localized project is requested.
"""
from __future__ import annotations

from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "catalog"
LOCALES = CATALOG / "locales"
AREAS = ("base", "bootstrap", "docs", "modules", "skills")
TOKEN = re.compile(r"\{\{[A-Z][A-Z0-9_]*\}\}")
errors: list[str] = []


def canonical_sources() -> dict[str, Path]:
    sources = {}
    for area in AREAS:
        for source in (CATALOG / area).rglob("*.md"):
            relative = source.relative_to(CATALOG).as_posix()
            sources[relative] = source
    return sources


def frontmatter(content: str) -> str:
    if not content.startswith("---\n"):
        return ""
    end = content.find("\n---\n", 4)
    return content[4:end] if end >= 0 else ""


def linked_document_paths(markdown: str) -> list[str]:
    """Compare local link destinations while allowing localized anchor slugs.

    This is a source-parity guard, not a substitute for generated Markdown
    navigation validation. Ignore code-fence examples and remote URLs.
    """
    prose = re.sub(r"^```[^\\n]*\\n[\\s\\S]*?^```", "", markdown, flags=re.M)
    targets = re.findall(r"\\]\\(([^)\\s]+)", prose)
    return sorted(path for target in targets
                  if (path := target.split("#", 1)[0]) and "://" not in path)


def check() -> None:
    sources = canonical_sources()
    ru = LOCALES / "ru"
    if not ru.is_dir():
        errors.append("Missing catalog/locales/ru")
        return
    translated = set()
    for localized in ru.rglob("*.md"):
        relative = localized.relative_to(ru).as_posix()
        translated.add(relative)
        if relative not in sources:
            errors.append(f"Unexpected translation without source: {relative}")
            continue
        english = sources[relative].read_text(encoding="utf-8")
        value = localized.read_text(encoding="utf-8")
        if not value.strip():
            errors.append(f"Empty translation: {relative}")
        if sorted(TOKEN.findall(english)) != sorted(TOKEN.findall(value)):
            errors.append(f"Placeholder mismatch: {relative}")
        if bool(frontmatter(english)) != bool(frontmatter(value)):
            errors.append(f"Frontmatter presence mismatch: {relative}")
        if frontmatter(english):
            en_meta = dict(re.findall(r"^([a-z][a-z0-9_-]*):\s*(.*)$", frontmatter(english), re.M))
            ru_meta = dict(re.findall(r"^([a-z][a-z0-9_-]*):\s*(.*)$", frontmatter(value), re.M))
            if en_meta.keys() != ru_meta.keys():
                errors.append(f"Frontmatter keys mismatch: {relative}")
            for key in ("kind", "status", "id", "name"):
                if key in en_meta and en_meta.get(key) != ru_meta.get(key):
                    errors.append(f"Non-localizable frontmatter changed: {relative} / {key}")
        if value == english:
            errors.append(f"Untranslated source copied verbatim: {relative}")
        if linked_document_paths(english) != linked_document_paths(value):
            errors.append(f"Relative Markdown link targets differ: {relative}")
        # Executable reference fixtures are language-independent contracts.
        # Translating explanation text must never change code that the source
        # tests extract, compile or compare byte-for-byte.
        if relative == "docs/standards/dotnet/eventbus-composition.md":
            pattern = r"^```(?:xml|csharp) file=[^\n]+\n[\s\S]*?^```"
            if re.findall(pattern, english, re.M) != re.findall(pattern, value, re.M):
                errors.append(f"Executable EventBus examples differ: {relative}")
        if relative == "docs/standards/http/etag-concurrency.md":
            pattern = r"^```http scenario=[^\n]+\n[\s\S]*?^```"
            if re.findall(pattern, english, re.M) != re.findall(pattern, value, re.M):
                errors.append(f"HTTP ETag scenarios differ: {relative}")
    for missing in sorted(sources.keys() - translated):
        errors.append(f"Missing Russian translation: {missing}")
    print(f"Russian translations: {len(translated & sources.keys())}/{len(sources)} Markdown source files")
    print(f"Pending translations: {len(sources.keys() - translated)}")


if __name__ == "__main__":
    check()
    for error in errors:
        print("ERROR", error)
    raise SystemExit(bool(errors))
