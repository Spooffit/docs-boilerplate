#!/usr/bin/env python3
"""Reject source-tree leakage and broken links in the active boilerplate.

This test covers the maintainer docs, not the generated project's complete
Markdown syntax. Catalog-specific frontmatter and name checks live in
catalog_hygiene.py; generated file scope lives in generated_output_hygiene.py.
"""
from __future__ import annotations

from pathlib import Path
import json
import posixpath
import re
from urllib.parse import unquote

ROOT = Path(__file__).resolve().parents[1]
FORBIDDEN_ROOTS = ("business", "technical", "milestones", "shared")
FORBIDDEN_FILES = ("features.md", "modules.md", "questions.md", "standards.md")
FORBIDDEN_DIRECTORIES = ("docs/audit", "docs/legacy")
FORBIDDEN_TESTS = ("legacy_inventory.py", "legacy_link_graph.py", "source_evidence_links.py")
FORBIDDEN_GENERATOR_FORMAT_FILES = (
    "schemas", "tests/schema_contracts.py", "src/DocsBoilerplate.Cli/SchemaCompatibility.cs",
    "src/DocsBoilerplate.Cli/PACKAGE-README.md",
)
NON_TEMPLATE_MARKER = re.compile(r"\bUNREUSABLE_PROJECT_CONTENT\b")
INLINE_LINK = re.compile(r"\]\(\s*<?([^\s)<>]+)>?(?:\s+[^)]*)?\)")
REFERENCE_LINK = re.compile(r"^\s{0,3}\[[^\]]+\]:\s*<?([^\s<>]+)>?", re.M)
FENCE = re.compile(r"^\s{0,3}(`{3,}|~{3,})")
SCHEME = re.compile(r"^[a-z][a-z0-9+.-]*:", re.I)


def links(document: str):
    fenced = None
    for line in document.splitlines():
        marker = FENCE.match(line)
        if marker:
            style = marker.group(1)[0]
            if fenced is None:
                fenced = style
            elif fenced == style:
                fenced = None
            continue
        if fenced is not None:
            continue
        yield from (x.group(1) for x in INLINE_LINK.finditer(line))
        yield from (x.group(1) for x in REFERENCE_LINK.finditer(line))


def target_path(source: str, target: str):
    cleaned = unquote(target).split("#", 1)[0].split("?", 1)[0]
    if not cleaned or cleaned.startswith("/") or SCHEME.match(cleaned):
        return None
    path = posixpath.normpath(posixpath.join(posixpath.dirname(source), cleaned))
    if path.startswith("../") or path == "..":
        return None
    return path


def main():
    errors = []
    for d in FORBIDDEN_ROOTS + FORBIDDEN_DIRECTORIES:
        if (ROOT / d).exists():
            errors.append(f"Obsolete project directory returned: {d}")
    for f in FORBIDDEN_FILES:
        if (ROOT / f).exists():
            errors.append(f"Obsolete project index returned: {f}")
    for f in FORBIDDEN_TESTS:
        if (ROOT / "tests" / f).exists():
            errors.append(f"Obsolete migration-only test returned: tests/{f}")

    # The template generator intentionally has no schema registry, format
    # versioning or standalone CLI distribution/release machinery.
    for path in FORBIDDEN_GENERATOR_FORMAT_FILES:
        if (ROOT / path).exists():
            errors.append(f"Generator-only format or distribution artifact returned: {path}")
    for name in ("profiles", "modules", "skills"):
        catalog_path = ROOT / "catalog" / f"{name}.json"
        data = json.loads(catalog_path.read_text(encoding="utf-8"))
        if "schemaVersion" in data:
            errors.append(f"Catalog format version returned: {catalog_path.relative_to(ROOT)}")
    for code in (ROOT / "src" / "DocsBoilerplate.Cli").rglob("*.cs"):
        if "SchemaVersion" in code.read_text(encoding="utf-8"):
            errors.append(f"Generator manifest versioning returned: {code.relative_to(ROOT)}")
    project = (ROOT / "src" / "DocsBoilerplate.Cli" / "DocsBoilerplate.Cli.csproj").read_text(encoding="utf-8")
    for token in ("<PackAsTool>true</PackAsTool>", "<ToolCommandName>docs-boilerplate</ToolCommandName>",
                  "<PackageId>DocsBoilerplate.Cli</PackageId>", 'CopyToPublishDirectory="PreserveNewest"'):
        if token not in project:
            errors.append(f"Installable CLI packaging missing: {token}")
    workflow = (ROOT / ".github" / "workflows" / "docs-generator.yml").read_text(encoding="utf-8")
    if "dotnet nuget push" in workflow or "dotnet tool update -g" in workflow:
        errors.append("Unapproved public package publication job")

    docs = set((ROOT / "docs").rglob("*.md"))
    docs.update((ROOT / "catalog").rglob("*.md"))
    docs.update(ROOT / f for f in ("README.md", "AGENTS.md", "CONTRIBUTING.md"))
    docs.add(ROOT / "llms.txt")

    if not (ROOT / "README.md").read_text(encoding="utf-8").startswith("# "):
        errors.append("Repository README.md must start with its Markdown H1, not GitHub-rendered YAML metadata.")

    checked = 0
    for p in sorted(docs):
        if not p.is_file():
            errors.append(f"Missing expected maintainer document: {p}")
            continue
        relative = p.relative_to(ROOT).as_posix()
        content = p.read_text(encoding="utf-8")
        checked += 1
        if NON_TEMPLATE_MARKER.search(content):
            errors.append(f"Non-template content marker in {relative}")
        for link in links(content):
            dest = target_path(relative, link)
            if dest is None:
                continue
            if dest.split("/", 1)[0] in FORBIDDEN_ROOTS or dest in FORBIDDEN_FILES or dest.startswith(FORBIDDEN_DIRECTORIES):
                errors.append(f"Obsolete project link: {relative} -> {link}")
            # Maintainer docs must always resolve. Catalog content may contain
            # paths relative to generated project output and is checked after rendering.
            if relative.startswith("docs/") or relative in ("README.md", "AGENTS.md", "CONTRIBUTING.md"):
                if not (ROOT / dest).exists():
                    errors.append(f"Broken maintainer Markdown link: {relative} -> {link}")

    for error in errors:
        print("ERROR:", error)
    print(f"Repository cleanliness: {checked} source documents checked; {len(errors)} findings.")
    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
