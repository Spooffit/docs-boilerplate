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


# Approved owner decisions (ADR 0005): guard the existing reusable inventory.
# These are source-level checks; end-to-end profile and draft checks live in smoke.sh.
import json

GENERAL_TEMPLATES = {
    "feature.md", "technical-feature.md", "module.md", "question.md",
    "adr.md", "standard.md", "milestone.md",
}
OPTIONAL_TEMPLATES = {
    "dotnet-backend-feature.md", "blazor-frontend-feature.md",
    "postgresql-migration.md",
}
PLANNED_PROFILES = {"go", "angular", "react", "blazorise", "custom-ui"}

general_dir = CATALOG / "base/docs/templates"
actual_general = {p.name for p in general_dir.glob("*.md")} - {"index.md"}
if actual_general != GENERAL_TEMPLATES:
    errors.append(f"Approved general template set changed: {sorted(actual_general)}")
optional_dir = CATALOG / "docs/templates"
actual_optional = {p.name for p in optional_dir.glob("*.md")}
if actual_optional != OPTIONAL_TEMPLATES:
    errors.append(f"Approved optional template set changed: {sorted(actual_optional)}")

with (CATALOG / "profiles.json").open(encoding="utf-8") as stream:
    catalog_data = json.load(stream)
profiles = catalog_data["profiles"]
planned = {p["id"] for p in profiles if p["status"] == "planned"}
if planned != PLANNED_PROFILES:
    errors.append(f"Approved visible planned profiles changed: {sorted(planned)}")
for profile in profiles:
    if profile["id"] in PLANNED_PROFILES and profile["files"]:
        errors.append(f"Planned profile gained selectable content: {profile['id']}")
optional_sources = {"docs/templates/" + name for name in OPTIONAL_TEMPLATES}
owners = {path for profile in profiles if profile["status"] == "available" for path in profile["files"]}
if not optional_sources.issubset(owners):
    errors.append(f"Technology-specific templates lost opt-in profile ownership: {sorted(optional_sources - owners)}")

for relative in ("project/overview.md", "architecture/overview.md"):
    document = CATALOG / "base/docs" / relative
    content = document.read_text(encoding="utf-8") if document.is_file() else ""
    if not content.startswith("---\n") or "\nstatus: draft\n" not in content:
        errors.append(f"Approved initial draft missing or promoted: {document.relative_to(ROOT)}")

flow_doc = (CATALOG / "base/docs/standards/data-flows.md").read_text(encoding="utf-8")
milestone_doc = (general_dir / "milestone.md").read_text(encoding="utf-8")
overview_doc = (CATALOG / "base/docs/project/overview.md").read_text(encoding="utf-8")
if "Do not force globally numbered IDs" not in flow_doc:
    errors.append("Data-flow guidance lost optional flow-ID policy")
if "Do not turn every task" not in milestone_doc or "Do not invent Demo/MVP/Production stages" not in overview_doc:
    errors.append("Milestone guidance introduced assumed project stages")

for unwanted in ("demo", "demos", "example-project", "examples", "samples"):
    if (ROOT / unwanted).exists() or (CATALOG / unwanted).exists():
        errors.append(f"Unapproved demonstration project directory: {unwanted}")
# ADR 0006: three optional variants, no accidental default/profile coupling.
approved_optional = {
    "test-naming-convention": (["dotnet"], "docs/standards/dotnet/test-naming-convention.md"),
    "http-etag-concurrency": (["http-contracts"], "docs/standards/http/etag-concurrency.md"),
    "masstransit-eventbus-composition": (["masstransit-rabbitmq"], "docs/standards/dotnet/eventbus-composition.md"),
}
by_id = {p["id"]: p for p in profiles}
for profile_id, (dependencies, target) in approved_optional.items():
    entry = by_id.get(profile_id)
    if entry is None or entry["status"] != "available":
        errors.append(f"Missing approved opt-in profile {profile_id}")
    elif entry["requires"] != dependencies or entry["files"] != [target]:
        errors.append(f"Approved optional profile changed dependencies or content: {profile_id}")
for entry in profiles:
    if entry["id"] not in approved_optional and set(entry["requires"]) & approved_optional.keys():
        errors.append(f"Base profile unexpectedly requires optional convention: {entry['id']}")

# ADR 0007: preserve the last two explicitly selectable, independent conventions.
last_block_profiles = {
    "document-identifiers-and-statuses": ([], "docs/standards/documentation/identifiers-and-statuses.md"),
    "ui-direct-dto-forms": (["http-contracts", "ui-accessibility"], "docs/standards/ui/direct-dto-forms.md"),
}
for profile_id, (required, path) in last_block_profiles.items():
    selected = by_id.get(profile_id)
    if selected is None or selected["status"] != "available":
        errors.append(f"Missing final approved optional profile: {profile_id}")
    elif selected["requires"] != required or selected["files"] != [path]:
        errors.append(f"Final optional profile changed dependencies/content: {profile_id}")
for entry in profiles:
    if entry["id"] not in last_block_profiles and set(entry["requires"]) & last_block_profiles.keys():
        errors.append(f"Unselected final convention leaked into a base profile: {entry['id']}")

# Exclude the inherited application-specific client-library and code-generation
# content from *active* documentation. The .NET SDK used to build the docs CLI
# is a separate compiler tool and is intentionally not excluded.
legacy_client_patterns = [
    re.compile(p, re.IGNORECASE) for p in (
        r"\bcode[- ]generat(?:ion|or|ors|ed|ing)\b",
        r"\bcodegen\b",
        r"\bsource[- ]generat(?:ion|or|ors|ed|ing)\b",
        r"\bschema registr(?:y|ies)\b",
        r"\bgenerated (?:api )?(?:client|dto|model)s?\b",
        r"\b(?:cross-platform|client|platform|custom) SDK\b",
        r"\bSDK (?:core|architecture|adapter|package|development)\b",
        r"\bSDKs\b",
        r"\bplatform adapters?\b",
    )
]
for document in (CATALOG / "base").rglob("*.md"):
    contents = document.read_text(encoding="utf-8")
    for pattern in legacy_client_patterns:
        if pattern.search(contents):
            errors.append(f"Prior-project client/code-generation policy in {document.relative_to(ROOT)}: {pattern.pattern}")
for document in (CATALOG / "docs").rglob("*.md"):
    contents = document.read_text(encoding="utf-8")
    for pattern in legacy_client_patterns:
        if pattern.search(contents):
            errors.append(f"Prior-project client/code-generation policy in {document.relative_to(ROOT)}: {pattern.pattern}")

authoring = (CATALOG / "base/docs/standards/documentation.md").read_text(encoding="utf-8")
for phrase in ("Code examples and verification evidence", "label it illustrative or partial", "build/test command"):
    if phrase not in authoring:
        errors.append(f"Code-example verification policy missing: {phrase}")
for error in errors:
    print("ERROR", error)
print(f"Active catalog hygiene: {checked} Markdown files examined; {len(errors)} findings.")
raise SystemExit(1 if errors else 0)
