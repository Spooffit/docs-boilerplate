#!/usr/bin/env python3
"""Lightweight release policy guard without additional Python dependencies."""
from pathlib import Path
import re
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
project = ET.parse(root / "src/DocsBoilerplate.Cli/DocsBoilerplate.Cli.csproj").getroot()
props = project.find("PropertyGroup")
errors = []

def prop(name):
    node = props.find(name)
    return node.text.strip() if node is not None and node.text else None

for name, expected in [
    ("PackAsTool", "true"), ("ToolCommandName", "docs-boilerplate"),
    ("PackageId", "DocsBoilerplate.Cli"), ("PackageReadmeFile", "README.md")
]:
    if prop(name) != expected:
        errors.append(f"Unexpected .NET tool package field {name}: {prop(name)!r}")
if not re.fullmatch(r"\d+\.\d+\.\d+", prop("Version") or ""):
    errors.append("Default tool version must be a valid numeric SemVer prefix")
if not (root / "packaging/README.md").is_file():
    errors.append("NuGet package README is missing")

workflow = (root / ".github/workflows/release.yml").read_text(encoding="utf-8")
for must in (
    "tags:", "'v*'", "git merge-base --is-ancestor HEAD origin/main",
    "dotnet pack", '-p:Version="$PACKAGE_VERSION"', "dotnet tool install",
    "bash tests/smoke.sh", "NuGet/login@v1", "id-token: write",
    "vars.NUGET_USER", "dotnet nuget push", "api.nuget.org/v3/index.json",
    "gh release create", "--verify-tag", "needs: verify",
    "contents: write", "cancel-in-progress: false"
):
    if must not in workflow:
        errors.append(f"Release workflow missing safety or publication check: {must}")
if re.search(r"(?m)^\s{2}branches:\s*\[main\]", workflow):
    errors.append("Release workflow cannot publish on main branch pushes")
if "workflow_dispatch:" in workflow:
    errors.append("Release workflow must not allow manual publication without a version tag")

ci = (root / ".github/workflows/docs-generator.yml").read_text(encoding="utf-8")
if "dotnet nuget push" in ci or "NuGet/login@v1" in ci:
    errors.append("Regular CI must not publish packages")
if "python tests/release_contracts.py" not in ci:
    errors.append("Regular CI does not validate the release workflow")

for error in errors:
    print("ERROR:", error)
print(f"Release contract checks: {len(errors)} findings.")
raise SystemExit(1 if errors else 0)
