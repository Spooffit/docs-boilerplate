#!/usr/bin/env python3
"""Check reusable catalog metadata, naming and absence of non-template fixture markers."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "catalog"
ALLOWED_SPECIAL = {"README.md", "AGENTS.md", "SKILL.md", "CONTRIBUTING.md"}
NON_TEMPLATE_MARKER = re.compile(r"\bUNREUSABLE_PROJECT_CONTENT\b")
DOC_FILENAME = re.compile(r"^[a-z0-9]+(?:-[a-z0-9]+)*\.md$")
PROTECTED_TREES = ("base", "docs", "modules", "skills", "bootstrap")
errors = []
checked = 0

for folder in PROTECTED_TREES:
    for doc in sorted((CATALOG / folder).rglob("*.md")):
        text = doc.read_text(encoding="utf-8")
        relative = doc.relative_to(ROOT).as_posix()
        checked += 1

        if relative == "catalog/base/README.md":
            if not text.startswith("# "):
                errors.append(f"{relative}: root README must start with a heading, not frontmatter")
        elif not text.startswith("---\n") or "\n---\n" not in text[4:]:
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
INFRA_PROFILES = {
    "infra-logging-loki", "infra-metrics-prometheus", "infra-tracing-tempo",
    "infra-tracing-jaeger", "infra-deploy-compose", "infra-deploy-kubernetes",
    "infra-devops-backend-repo", "infra-devops-gitops-repo", "infra-orchestration-aspire",
    "infra-storage-s3", "infra-s3-client-minio", "infra-s3-client-aws", "object-storage",
    "infra-collector-alloy", "infra-metrics-mimir",
}

INFRA_PLACEHOLDER_PROFILES = INFRA_PROFILES - {
    "infra-storage-s3", "infra-s3-client-minio", "infra-s3-client-aws", "object-storage",
    "infra-collector-alloy", "infra-metrics-mimir",
}

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
infra = {p["id"] for p in profiles if p["category"].startswith("infra-")}
if infra != INFRA_PROFILES:
    errors.append(f"Required temporary infrastructure choices changed: {sorted(infra)}")
for p in profiles:
    if p["id"] in INFRA_PLACEHOLDER_PROFILES:
        for path in p["files"]:
            source = CATALOG / path
            if not source.is_file() or "Placeholder only" not in source.read_text(encoding="utf-8"):
                errors.append(f"Infrastructure placeholder is missing/not labeled: {path}")
if (CATALOG / "docs/standards/aspnet/mvc-controllers.md").exists() or any(p["id"] == "aspnet-mvc" for p in profiles):
    errors.append("Removed MVC-only profile or standard reappeared")
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
# Independent optional conventions remain separate from baseline guidance.
approved_optional = {
    "test-naming-convention": (["dotnet"], "docs/standards/dotnet/test-naming-convention.md"),
    "http-etag-concurrency": (["http-contracts"], "docs/standards/http/etag-concurrency.md"),
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

# Document IDs, DDD, reliable integration, accessibility and UI DTO binding are baseline.
if "document-identifiers-and-statuses" in by_id or "integration-reliability" in by_id:
    errors.append("Old optional IDs/integration profiles returned")
if not (CATALOG / "base/docs/standards/document-identifiers.md").is_file():
    errors.append("Missing baseline document identifier standard")
for item in ("reliable-synchronization.md", "versioned-transfers.md"):
    if not (CATALOG / "base/docs/standards/integrations" / item).is_file():
        errors.append(f"Missing baseline integration standard: {item}")
dto = by_id.get("ui-direct-dto-forms")
if not dto or dto["requires"] != ["http-contracts", "ui-accessibility"]:
    errors.append("Baseline UI DTO forms profile missing or dependencies changed")
xunit = by_id.get("xunit")
if not xunit or xunit["status"] != "available" or xunit["category"] != "test-framework":
    errors.append("xUnit test framework missing")
for id in ("tunit", "xunit"):
    if by_id.get(id, {}).get("category") != "test-framework":
        errors.append(f"Test framework must be single-select: {id}")
if by_id.get("bunit", {}).get("category") != "blazor-testing":
    errors.append("bUnit must appear in Blazor component testing choices")
if by_id.get("infra-storage-s3", {}).get("category") != "infra-storage":
    errors.append("S3 infrastructure storage choice is missing")
# S3 is infrastructure; client selection is exclusive, while general binary
# safety is pulled in transitively and is never a separate wizard question.
s3_profile_contracts = {
    "object-storage": ("infra-storage-policy", [], "docs/standards/storage/object-storage.md"),
    "infra-storage-s3": ("infra-storage", ["object-storage"], "docs/standards/infrastructure/storage-s3.md"),
    "infra-s3-client-minio": ("infra-s3-client", ["infra-storage-s3", "dotnet"], "docs/standards/infrastructure/s3-client-minio.md"),
    "infra-s3-client-aws": ("infra-s3-client", ["infra-storage-s3", "dotnet"], "docs/standards/infrastructure/s3-client-aws.md"),
}
for profile_id, (category, dependencies, path) in s3_profile_contracts.items():
    entry = by_id.get(profile_id)
    if not entry or entry["status"] != "available":
        errors.append(f"Missing available S3 profile: {profile_id}")
    elif (entry["category"], entry["requires"], entry["files"]) != (category, dependencies, [path]):
        errors.append(f"S3 profile grouping, dependencies or source changed: {profile_id}")

# Current wizard architecture and removed legacy profiles are contractual.
retired_messaging = {
    "masstransit-rabbitmq", "masstransit-kafka", "masstransit-eventbus-composition",
}
for profile_id in retired_messaging:
    if profile_id in by_id:
        errors.append(f"Retired messaging profile is still selectable: {profile_id}")
for name in ("masstransit-rabbitmq.md", "masstransit-kafka.md", "eventbus-composition.md"):
    if (CATALOG / "docs/standards/dotnet" / name).exists():
        errors.append(f"Retired messaging document remains: {name}")
required_profiles = {
    "messaging-rabbitmq": "messaging-transport",
    "messaging-kafka": "messaging-transport",
    "masstransit": "messaging-client",
    "wolverine-rabbitmq": "messaging-client",
    "wolverine-kafka": "messaging-client",
    "messaging-manual-rabbitmq": "messaging-client",
    "messaging-manual-kafka": "messaging-client",
    "messaging-organization-inline": "messaging-organization",
    "messaging-organization-separate": "messaging-organization",
    "modular-monolith": "architecture",
    "microservices": "architecture",
    "architecture-hybrid": "architecture",
    "postgres-pooler-pgbouncer": "postgres-pooler",
    "postgres-pooler-odyssey": "postgres-pooler",
    "specification-manual": "query-pattern",
    "ardalis-specification": "query-pattern",
    "cqrs-manual": "cqrs-implementation",
    "cqrs-wolverine": "cqrs-implementation",
    "cqrs-mediatr": "cqrs-implementation",
    "cqrs-mediator": "cqrs-implementation",
    "cqrs-requestum": "cqrs-implementation",
    "mapper-automapper": "mapper",
    "mapper-mapster": "mapper",
    "mapper-mapperly": "mapper",
    "mapper-manual": "mapper",
    "rest-client-manual": "rest-client",
    "rest-client-nswag": "rest-client",
    "openapi-swagger-ui": "openapi-ui",
    "openapi-scalar": "openapi-ui",
    "infra-collector-alloy": "infra-collector",
    "infra-metrics-mimir": "infra-metrics-backend",
}
for profile_id, category in required_profiles.items():
    profile = by_id.get(profile_id)
    if not profile or profile["status"] != "available" or profile["category"] != category:
        errors.append(f"Missing or miscategorized wizard choice: {profile_id}")
    elif len(profile["files"]) != 1:
        errors.append(f"Wizard choice must own one documentation file: {profile_id}")
# Validate the actual illustrative XML/JSON, not only mentions of their tags.
# The user-facing standard requires three shared root files and explicit,
# filename-matching assembly metadata in each individual project.
from xml.etree import ElementTree

for locale_root in (CATALOG, CATALOG / "locales/ru"):
    project_doc = locale_root / "docs/standards/dotnet/project-files.md"
    if not project_doc.is_file():
        errors.append(f"Mandatory .NET project file standard missing: {project_doc.relative_to(ROOT)}")
        continue
    standard = project_doc.read_text(encoding="utf-8")
    for token in ("Directory.Build.props", "Directory.Packages.props", "global.json",
                  "TargetFramework", "ImplicitUsings", "Nullable",
                  "PackageId", "AssemblyName", "RootNamespace"):
        if token not in standard:
            errors.append(f"Missing mandatory .NET project configuration rule: {token}")
    xml_blocks = re.findall(r"```xml\n(.*?)\n```", standard, re.S)
    json_blocks = re.findall(r"```json\n(.*?)\n```", standard, re.S)
    if len(xml_blocks) != 3 or len(json_blocks) != 1:
        errors.append(f"Expected 3 XML and 1 JSON examples: {project_doc.relative_to(ROOT)}")
        continue
    try:
        props, versions, project = map(ElementTree.fromstring, xml_blocks)
        sdk = json.loads(json_blocks[0])["sdk"]
        shared = {element.tag: (element.text or "").strip()
                  for element in props.findall("./PropertyGroup/*")}
        identity = {element.tag: (element.text or "").strip()
                    for element in project.findall("./PropertyGroup/*")}
        if not {"TargetFramework", "ImplicitUsings", "Nullable"} <= shared.keys():
            errors.append(f"Missing shared build properties: {project_doc.relative_to(ROOT)}")
        if versions.findtext("./PropertyGroup/ManagePackageVersionsCentrally") != "true":
            errors.append(f"Central Package Management not enabled: {project_doc.relative_to(ROOT)}")
        if not sdk.get("version") or not sdk.get("rollForward"):
            errors.append(f"SDK version/rollForward missing: {project_doc.relative_to(ROOT)}")
        if any(key in identity for key in ("TargetFramework", "ImplicitUsings", "Nullable")):
            errors.append(f"Inherited build property repeated in csproj: {project_doc.relative_to(ROOT)}")
        if {identity.get(key) for key in ("PackageId", "AssemblyName", "RootNamespace")} != {"Example.Service"}:
            errors.append(f"Example project identity differs from Example.Service.csproj: {project_doc.relative_to(ROOT)}")
        if any(ref.get("Version") for ref in project.findall(".//PackageReference")):
            errors.append(f"Project reference bypasses central package versions: {project_doc.relative_to(ROOT)}")
    except (ElementTree.ParseError, ValueError, KeyError, TypeError) as exc:
        errors.append(f"Invalid .NET project-file XML/JSON example: {project_doc.relative_to(ROOT)}: {exc}")

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
        r"\bplatform adapters?\b",
    )
]
for document in (CATALOG / "base").rglob("*.md"):
    contents = document.read_text(encoding="utf-8")
    for pattern in legacy_client_patterns:
        if pattern.search(contents):
            errors.append(f"Prior-project client/code-generation policy in {document.relative_to(ROOT)}: {pattern.pattern}")
# Compiler source generation is an intrinsic feature of selected Mapperly and
# martinothamar/Mediator integrations, not the former project's custom
# source-model pipeline. Preserve the legacy block for every other document
# and every other forbidden topic.
source_generator_standards = {
    "docs/standards/dotnet/mapper-mapperly.md",
    "docs/standards/dotnet/cqrs-mediator.md",
}
for document in (CATALOG / "docs").rglob("*.md"):
    contents = document.read_text(encoding="utf-8")
    relative = document.relative_to(CATALOG).as_posix()
    for pattern in legacy_client_patterns:
        if pattern.pattern == r"\bsource[- ]generat(?:ion|or|ors|ed|ing)\b" and relative in source_generator_standards:
            continue
        if pattern.search(contents):
            errors.append(f"Prior-project client/code-generation policy in {document.relative_to(ROOT)}: {pattern.pattern}")

authoring = (CATALOG / "base/docs/standards/documentation.md").read_text(encoding="utf-8")
for phrase in ("Code examples and verification evidence", "label it illustrative or partial", "build/test command"):
    if phrase not in authoring:
        errors.append(f"Code-example verification policy missing: {phrase}")
# The selected technology catalog must have localized sources with consistent
# metadata. The CLI validates this at runtime; also fail early during catalog
# hygiene checks, without requiring a .NET SDK.
PROFILE_KEYS = ("id", "name", "status", "category", "requires", "files")
profile_ids = [profile["id"] for profile in profiles]
if len(profile_ids) != len(set(profile_ids)):
    errors.append("Duplicate profile IDs in catalog/profiles.json")
all_profile_ids = set(profile_ids)
for profile in profiles:
    missing_dependencies = set(profile.get("requires", [])) - all_profile_ids
    if missing_dependencies:
        errors.append(f"{profile['id']}: missing dependencies {sorted(missing_dependencies)}")
    if profile["status"] != "available":
        continue
    for source_path in profile["files"]:
        source = CATALOG / source_path
        translated = CATALOG / "locales/ru" / source_path
        for file_path in (source, translated):
            if not file_path.is_file():
                errors.append(f"{profile['id']}: missing source: {file_path.relative_to(ROOT)}")
                continue
            body = file_path.read_text(encoding="utf-8")
            if not body.startswith("---\n") or "\n---\n" not in body[4:]:
                errors.append(f"{profile['id']}: invalid frontmatter: {file_path.relative_to(ROOT)}")
            if not re.search(r"(?m)^# \S", body):
                errors.append(f"{profile['id']}: missing H1: {file_path.relative_to(ROOT)}")

# Regression protection for the engineering content enriched in both locales.
# These markers are intentionally structural, not a forced translation of wording.
AUDITED_STANDARDS = {
    "docs/standards/dotnet/specification-manual.md": ("## Expression composition", "## Композиция выражений"),
    "docs/standards/postgresql/pgbouncer.md": ("## Pooling contract", "## Контракт пулера"),
    "docs/standards/postgresql/odyssey.md": ("## Pooling and routing contract", "## Контракт маршрутизации и пула"),
    "docs/standards/dotnet/dapper.md": ("## Query boundaries and consistency", "## Границы запросов и согласованность"),
    "docs/standards/architecture/hybrid.md": ("## Ownership and deployment boundaries", "## Владение данными и границы развёртывания"),
    "docs/standards/architecture/microservices.md": ("## Service and data boundaries", "## Границы сервисов и владение данными"),
    "docs/standards/dotnet/ef-core.md": ("## Query execution and context boundaries", "## Выполнение запросов и границы контекста"),
    "docs/standards/dotnet/masstransit.md": ("## Transport-specific composition and guarantees", "## Компоновка транспорта и гарантии"),
    "docs/standards/messaging/rabbitmq.md": ("## Delivery and topology checks", "## Доставка и проверка топологии"),
    "docs/standards/messaging/kafka.md": ("## Consumption verification", "## Проверки потребления и контрактов"),
    "docs/standards/dotnet/wolverine-rabbitmq.md": ("## Reliability contract", "## Контракт надёжности"),
    "docs/standards/dotnet/wolverine-kafka.md": ("## Reliability contract", "## Контракт надёжности"),
    "docs/standards/dotnet/manual-rabbitmq.md": ("## Client boundary and failure tests", "## Граница клиента и проверки отказов"),
    "docs/standards/dotnet/manual-kafka.md": ("## Client boundary and failure tests", "## Граница клиента и проверки отказов"),
    "docs/standards/messaging/organization-inline.md": ("## Contract and ownership boundaries", "## Границы контрактов и ответственности"),
    "docs/standards/messaging/organization-separate.md": ("## Dependency and compatibility boundaries", "## Зависимости и совместимость"),
}
for relative, markers in AUDITED_STANDARDS.items():
    for root, heading in zip((CATALOG, CATALOG / "locales/ru"), markers):
        file_path = root / relative
        if file_path.is_file() and heading not in file_path.read_text(encoding="utf-8"):
            errors.append(f"Audited standard lost content {heading!r}: {file_path.relative_to(ROOT)}")

for error in errors:
    print("ERROR", error)
print(f"Active catalog hygiene: {checked} Markdown files examined; {len(errors)} findings.")
raise SystemExit(1 if errors else 0)
