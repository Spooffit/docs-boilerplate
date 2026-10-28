#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."
project=src/DocsBoilerplate.Cli
temp="$(mktemp -d)"
trap 'rm -rf "$temp"' EXIT
run() { dotnet run --no-build --configuration Release --project "$project" -- "$@"; }

run doctor
run list

# A genuinely minimal repo must not contain dangling links to optional modules.
run init --name Minimal --description 'Minimal docs' --output "$temp/minimal" --profiles dotnet --non-interactive
run validate --output "$temp/minimal"
python3 - "$temp/minimal/docs.config.json" <<'PY'
import json,sys
with open(sys.argv[1],encoding="utf-8") as stream:
    config=json.load(stream)
assert len(config["GeneratedHashes"]) == len(config["GeneratedFiles"])
assert set(config["GeneratedHashes"]) == set(config["GeneratedFiles"])
assert all(len(value)==64 for value in config["GeneratedHashes"].values())
PY
# First-use project overviews must be draft and evidence-oriented.
[[ -f "$temp/minimal/docs/project/overview.md" ]]
[[ -f "$temp/minimal/docs/architecture/overview.md" ]]
grep -q '^status: draft$' "$temp/minimal/docs/project/overview.md"
grep -q '^status: draft$' "$temp/minimal/docs/architecture/overview.md"
grep -q '^## Evidence and confidence$' "$temp/minimal/docs/project/overview.md"
grep -q '^## Current state versus intended state$' "$temp/minimal/docs/project/overview.md"
grep -q '^## Current system as observed$' "$temp/minimal/docs/architecture/overview.md"
grep -q '^## Accepted target architecture$' "$temp/minimal/docs/architecture/overview.md"
[[ -f "$temp/minimal/docs/catalog.md" ]]
[[ -f "$temp/minimal/llms.txt" ]]
if grep -q '^## Project areas$' "$temp/minimal/llms.txt"; then
    echo "Optional project areas leaked into minimal documentation"; exit 1
fi
grep -q 'docs/architecture/overview.md' "$temp/minimal/llms.txt"
[[ -f "$temp/minimal/docs/standards/index.md" ]]
[[ -f "$temp/minimal/docs/standards/dotnet/index.md" ]]
grep -q "docs/standards/dotnet/index.md" "$temp/minimal/llms.txt"
grep -q "dotnet/testing.md" "$temp/minimal/docs/catalog.md"
[[ -f "$temp/minimal/docs/standards/dotnet/testing.md" ]]
grep -q "^## Test environment ownership and bounded parallelism$" "$temp/minimal/docs/standards/dotnet/testing.md"
grep -q "^## Shared testing infrastructure and safe migration$" "$temp/minimal/docs/standards/dotnet/testing.md"
[[ -f "$temp/minimal/docs/templates/dotnet-backend-feature.md" ]]
[[ -f "$temp/minimal/docs/templates/milestone.md" ]]
[[ -f "$temp/minimal/docs/templates/technical-feature.md" ]]
[[ -f "$temp/minimal/docs/templates/index.md" ]]
grep -q '^# Choose a documentation template$' "$temp/minimal/docs/templates/index.md"
grep -q 'No separate module, question, milestone or technical specification is mandatory' "$temp/minimal/docs/templates/index.md"
if grep -q '^Documents in this directory:$' "$temp/minimal/docs/templates/index.md"; then
    echo "Curated index was replaced by synthetic folder navigation"; exit 1
fi
grep -q 'templates/index.md' "$temp/minimal/docs/catalog.md"
grep -q 'docs/templates/index.md' "$temp/minimal/llms.txt"
[[ -f "$temp/minimal/docs/standards/stateful-workflows.md" ]]
[[ -f "$temp/minimal/docs/standards/specification-traceability.md" ]]
[[ ! -e "$temp/minimal/docs/testing/strategy.md" ]]
[[ ! -e "$temp/minimal/.agents" ]]
[[ ! -e "$temp/minimal/docs/templates/blazor-frontend-feature.md" ]]
[[ ! -e "$temp/minimal/docs/standards/ui" ]]
if grep -q "blazor/components.md" "$temp/minimal/docs/catalog.md"; then echo "Unselected standard leaked into catalog"; exit 1; fi
[[ -f "$temp/minimal/index.html" ]]
run init --name Headless --description "No site viewer" --output "$temp/headless" --profiles dotnet --site none --non-interactive
run validate --output "$temp/headless"
[[ ! -e "$temp/headless/index.html" ]]
[[ -f "$temp/headless/llms.txt" ]]

# Optional HTTP and persistence profiles must compose without implicit extra libraries.
run init --name Web --description 'HTTP and storage' --output "$temp/web" --profiles aspnet-mvc,fluentvalidation,postgresql,cqrs,ef-core --site docsify --non-interactive
run validate --output "$temp/web"
[[ -f "$temp/web/docs/standards/aspnet/http-api.md" ]]
[[ -f "$temp/web/docs/standards/aspnet/errors-and-validation.md" ]]
[[ -f "$temp/web/docs/standards/aspnet/mvc-controllers.md" ]]
[[ -f "$temp/web/docs/standards/dotnet/fluentvalidation.md" ]]
[[ -f "$temp/web/docs/standards/postgresql/persistence.md" ]]
[[ -f "$temp/web/docs/templates/postgresql-migration.md" ]]
[[ -f "$temp/web/docs/standards/architecture/cqrs.md" ]]
[[ -f "$temp/web/docs/standards/dotnet/project-files.md" ]]
[[ -f "$temp/web/docs/standards/dotnet/ef-core.md" ]]
[[ ! -e "$temp/web/docs/standards/blazor" ]]
[[ ! -e "$temp/web/docs/standards/dotnet/dapper.md" ]]
[[ ! -e "$temp/web/docs/standards/dotnet/testcontainers.md" ]]
[[ ! -e "$temp/web/docs/standards/dotnet/tunit.md" ]]
run init --name Web --description 'HTTP and storage' --output "$temp/web" --profiles aspnet-mvc,fluentvalidation,postgresql,cqrs,ef-core --site docsify --non-interactive

# Prior releases synthesized this same path. Verify a fingerprint-owned, unedited
# directory index upgrades in place to the curated template guide.
cp -a catalog "$temp/catalog-old-index"
rm "$temp/catalog-old-index/base/docs/templates/index.md"
run init --name PriorIndex --description 'Synthetic template navigation' --output "$temp/prior-index" --catalog "$temp/catalog-old-index" --profiles dotnet --non-interactive
run validate --output "$temp/prior-index" --catalog "$temp/catalog-old-index"
grep -q '^Documents in this directory:$' "$temp/prior-index/docs/templates/index.md"
run update --output "$temp/prior-index" --apply
run validate --output "$temp/prior-index"
grep -q '^# Choose a documentation template$' "$temp/prior-index/docs/templates/index.md"

# HTTP contract guidance can be selected by any client, without .NET or MVC.
run init --name HttpConsumer --description 'Contract-driven client' --output "$temp/http-consumer" --profiles http-contracts --modules api --non-interactive
run validate --output "$temp/http-consumer"
[[ -f "$temp/http-consumer/docs/standards/http/client-contracts.md" ]]
[[ -f "$temp/http-consumer/docs/standards/http/contract-verification.md" ]]
[[ -f "$temp/http-consumer/docs/architecture/api.md" ]]
grep -q '^## Project areas$' "$temp/http-consumer/llms.txt"
grep -q 'docs/architecture/api.md' "$temp/http-consumer/llms.txt"
if grep -q 'docs/testing/strategy.md' "$temp/http-consumer/llms.txt"; then
    echo "Unselected testing documentation leaked into llms.txt"; exit 1
fi
[[ ! -e "$temp/http-consumer/docs/standards/dotnet" ]]
[[ ! -e "$temp/http-consumer/docs/standards/aspnet" ]]
[[ ! -e "$temp/http-consumer/docs/standards/postgresql" ]]
run update --output "$temp/http-consumer" --apply
run validate --output "$temp/http-consumer"

# Base ASP.NET Core does not silently add MVC or FluentValidation.
run init --name Api --description 'Minimal HTTP API' --output "$temp/api" --profiles aspnet-core --non-interactive
run validate --output "$temp/api"
[[ -f "$temp/api/docs/standards/aspnet/http-api.md" ]]
[[ ! -e "$temp/api/docs/standards/aspnet/mvc-controllers.md" ]]
[[ ! -e "$temp/api/docs/standards/dotnet/fluentvalidation.md" ]]

# A database profile is usable without assuming a particular language or ORM.
run init --name Storage --description 'Database docs' --output "$temp/storage" --profiles postgresql --non-interactive
run validate --output "$temp/storage"
[[ -f "$temp/storage/docs/standards/postgresql/persistence.md" ]]
[[ -f "$temp/storage/docs/templates/postgresql-migration.md" ]]
[[ ! -e "$temp/storage/docs/standards/dotnet" ]]

[[ -f "$temp/web/index.html" ]]
grep -q 'basePath: "docs/"' "$temp/web/index.html"

# Testing add-ons are independent and resolved through their .NET prerequisite.
run init --name Addons --description 'Testing tools' --output "$temp/addons" --profiles tunit,testcontainers --non-interactive
run validate --output "$temp/addons"
[[ -f "$temp/addons/docs/standards/dotnet/testing.md" ]]
[[ -f "$temp/addons/docs/standards/dotnet/tunit.md" ]]
[[ -f "$temp/addons/docs/standards/dotnet/testcontainers.md" ]]
[[ ! -e "$temp/addons/docs/standards/blazor" ]]

args=(--name Example --description 'Test documentation' --output "$temp/project" --profiles dotnet,ef-core,blazor,mudblazor,modular-monolith --skills docs-review,docs-maintain,docs-init --modules testing,operations --site docsify --non-interactive)
run init "${args[@]}" --dry-run
[[ ! -d "$temp/project" ]] || { echo "Dry run unexpectedly wrote files"; exit 1; }

run init "${args[@]}"
run validate --output "$temp/project"
[[ -f "$temp/project/docs/standards/dotnet/ef-core.md" ]]
[[ -f "$temp/project/docs/standards/blazor/mudblazor.md" ]]
grep -q "blazor/mudblazor.md" "$temp/project/docs/catalog.md"
grep -q "testing/strategy.md" "$temp/project/docs/catalog.md"
[[ -f "$temp/project/docs/testing/strategy.md" ]]
grep -q 'docs/testing/strategy.md' "$temp/project/llms.txt"
grep -q 'docs/operations/observability.md' "$temp/project/llms.txt"
if grep -q 'docs/architecture/security.md' "$temp/project/llms.txt"; then
    echo "Unselected security module leaked into llms.txt"; exit 1
fi
[[ -f "$temp/project/docs/operations/observability.md" ]]
[[ -f "$temp/project/.agents/skills/docs-review/SKILL.md" ]]
[[ -f "$temp/project/.agents/skills/docs-init/SKILL.md" ]]
grep -q 'First-pass project intake' "$temp/project/.agents/skills/docs-init/SKILL.md"
grep -q 'docs/project/overview.md' "$temp/project/.agents/skills/docs-init/SKILL.md"
[[ ! -e "$temp/project/docs/standards/dotnet/dapper.md" ]]
[[ ! -e "$temp/project/docs/project/glossary.md" ]]
[[ ! -e "$temp/project/.agents/skills/docs-architecture" ]]
[[ ! -e "$temp/project/src" ]]
[[ ! -e "$temp/project/catalog" ]]
[[ -f "$temp/project/index.html" ]]

# Messaging and query specifications stay opt-in and do not imply a particular database.
run init --name Integration --description 'Message handling' --output "$temp/integration" --profiles masstransit-rabbitmq,ardalis-specification --non-interactive
run validate --output "$temp/integration"
[[ -f "$temp/integration/docs/standards/dotnet/masstransit-rabbitmq.md" ]]
[[ -f "$temp/integration/docs/standards/dotnet/ardalis-specification.md" ]]
[[ -f "$temp/integration/docs/standards/dotnet/conventions.md" ]]
[[ ! -e "$temp/integration/docs/standards/dotnet/ef-core.md" ]]
[[ ! -e "$temp/integration/docs/standards/postgresql" ]]

# Language-neutral reliable synchronization does not bring in an ORM, .NET, or broker.
run init --name Sync --description "Cross-system state replication" --output "$temp/sync" --profiles integration-reliability --modules integrations --non-interactive
run validate --output "$temp/sync"
[[ -f "$temp/sync/docs/standards/integrations/reliable-synchronization.md" ]]
[[ -f "$temp/sync/docs/standards/integrations/versioned-transfers.md" ]]
[[ -f "$temp/sync/docs/architecture/integrations.md" ]]
[[ -f "$temp/sync/docs/standards/integrations/index.md" ]]
[[ ! -e "$temp/sync/docs/standards/dotnet" ]]
[[ ! -e "$temp/sync/docs/standards/postgresql" ]]
grep -q 'integrations/versioned-transfers.md' "$temp/sync/docs/catalog.md"
run update --output "$temp/sync" --apply
run validate --output "$temp/sync"

# Storage guidance is independent of provider, language and specific HTTP stack.
run init --name Assets --description 'Binary storage' --output "$temp/assets" --profiles object-storage --non-interactive
run validate --output "$temp/assets"
[[ -f "$temp/assets/docs/standards/storage/object-storage.md" ]]
[[ -f "$temp/assets/docs/standards/storage/index.md" ]]
[[ ! -e "$temp/assets/docs/standards/dotnet" ]]
[[ ! -e "$temp/assets/docs/standards/postgresql" ]]
[[ ! -e "$temp/assets/docs/standards/integrations" ]]
run update --output "$temp/assets" --apply
run validate --output "$temp/assets"

# Aspire testing is opt-in, requires .NET and does not select TUnit, Testcontainers, or a broker.
run init --name AspireApp --description 'AppHost scenarios' --output "$temp/aspire" --profiles aspire-testing --non-interactive
run validate --output "$temp/aspire"
[[ -f "$temp/aspire/docs/standards/dotnet/testing.md" ]]
[[ -f "$temp/aspire/docs/standards/dotnet/aspire-testing.md" ]]
[[ ! -f "$temp/aspire/docs/standards/dotnet/tunit.md" ]]
[[ ! -f "$temp/aspire/docs/standards/dotnet/testcontainers.md" ]]
[[ ! -f "$temp/aspire/docs/standards/dotnet/masstransit-rabbitmq.md" ]]
run update --output "$temp/aspire" --apply
run validate --output "$temp/aspire"

# bUnit requires Blazor, but does not implicitly choose MudBlazor.
run init --name Components --description 'UI component tests' --output "$temp/components" --profiles bunit --non-interactive
run validate --output "$temp/components"
[[ -f "$temp/components/docs/standards/blazor/bunit.md" ]]
[[ -f "$temp/components/docs/standards/blazor/components.md" ]]
[[ -f "$temp/components/docs/standards/ui/forms-and-accessibility.md" ]]
[[ -f "$temp/components/docs/standards/blazor/index.md" ]]
[[ -f "$temp/components/docs/templates/blazor-frontend-feature.md" ]]
[[ -f "$temp/components/docs/standards/dotnet/project-files.md" ]]
[[ ! -e "$temp/components/docs/standards/blazor/mudblazor.md" ]]

run init --name All --description "Full stack" --output "$temp/full-stack" --profiles aspnet-mvc,ef-core,postgresql,fluentvalidation,cqrs,masstransit-rabbitmq,ardalis-specification,blazor,mudblazor,bunit --site docsify --non-interactive
run validate --output "$temp/full-stack"
[[ -f "$temp/full-stack/index.html" ]]

# Exercise every available (not planned) profile/module/agent skill together.
# Audit the *rendered output*, not only catalog Markdown sources.
all_profiles="$(python3 -c 'import json; print(",".join(p["id"] for p in json.load(open("catalog/profiles.json", encoding="utf-8"))["profiles"] if p["status"] == "available"))')"
all_modules="$(python3 -c 'import json; print(",".join(p["id"] for p in json.load(open("catalog/modules.json", encoding="utf-8"))["modules"]))')"
all_skills="$(python3 -c 'import json; print(",".join(p["id"] for p in json.load(open("catalog/skills.json", encoding="utf-8"))["skills"]))')"
run init --name PortableCanary --description 'Generated artifact scope audit' --output "$temp/all-profiles" --profiles "$all_profiles" --modules "$all_modules" --skills "$all_skills" --site docsify --non-interactive
run validate --output "$temp/all-profiles"
grep -q 'docs/project/requirements.md' "$temp/all-profiles/llms.txt"
grep -q 'docs/architecture/security.md' "$temp/all-profiles/llms.txt"
grep -q 'docs/operations/runbook.md' "$temp/all-profiles/llms.txt"
# Optional audit-history review prompts must render only with the selected modules.
grep -q '^## Durable activity history (only when required)$' "$temp/all-profiles/docs/operations/observability.md"
grep -q 'source owner' "$temp/all-profiles/docs/operations/observability.md"
grep -q 'visibility before counts, pagination and filter suggestions' "$temp/all-profiles/docs/architecture/security.md"
grep -q '^## Conditional history and audit effects$' "$temp/all-profiles/docs/standards/data-flows.md"
grep -q 'cross-feature read concern' "$temp/all-profiles/docs/templates/module.md"
grep -q 'UI notification or an attempted action' "$temp/all-profiles/docs/templates/feature.md"
grep -q 'generator source checkout' "$temp/all-profiles/.agents/skills/docs-init/SKILL.md"
grep -q 'generated repository does not contain the CLI' "$temp/all-profiles/.agents/skills/docs-review/SKILL.md"
[[ ! -e "$temp/minimal/docs/operations/observability.md" ]]
[[ ! -e "$temp/minimal/docs/architecture/security.md" ]]
python3 tests/generated_output_hygiene.py "$temp/minimal" "$temp/headless" "$temp/full-stack" "$temp/all-profiles"

# Prove the new artifact guard fails specifically for source contamination and extra assets.
cp -a "$temp/headless" "$temp/project-leak"
printf '\nUNREUSABLE_PROJECT_CONTENT\n' >> "$temp/project-leak/docs/project/overview.md"
if python3 tests/generated_output_hygiene.py "$temp/project-leak" > "$temp/project-leak.log" 2>&1; then
    echo "Historical product content escaped generated-output audit"; exit 1
fi
grep -q "non-template content marker" "$temp/project-leak.log"
cp -a "$temp/headless" "$temp/asset-leak"
mkdir -p "$temp/asset-leak/assets"
printf 'unexpected browser bundle\n' > "$temp/asset-leak/assets/app.js"
if python3 tests/generated_output_hygiene.py "$temp/asset-leak" > "$temp/asset-leak.log" 2>&1; then
    echo "Unexpected frontend assets escaped generated-output audit"; exit 1
fi
grep -q "unexpected generated asset" "$temp/asset-leak.log"
grep -q "unexpected file" "$temp/asset-leak.log"

# Only docsify/none are supported; retired site engines cannot run or be updated.
if run init --name InvalidViewer --description Test --output "$temp/invalid-viewer" --profiles dotnet --site legacy-engine --non-interactive; then
  echo "Unsupported site engine accepted by init"; exit 1
fi
cp -a "$temp/minimal" "$temp/invalid-viewer-config"
python3 - "$temp/invalid-viewer-config/docs.config.json" <<'PY'
import json,sys
path=sys.argv[1]
with open(path, encoding="utf-8") as f: data=json.load(f)
data["Site"]="legacy-engine"
with open(path,"w",encoding="utf-8") as f: json.dump(data,f)
PY
if run validate --output "$temp/invalid-viewer-config"; then
  echo "Unsupported site engine accepted by validate"; exit 1
fi
if run update --output "$temp/invalid-viewer-config" --apply; then
  echo "Unsupported site engine accepted by update"; exit 1
fi

# An identical repeated initialization must not alter authored extra files.
echo 'custom' > "$temp/project/my-notes.txt"
run init "${args[@]}"

run init "${args[@]}"

# Reject unknown flags, missing values and invalid boolean-flag values.
if run init --name Flag --description Test --output "$temp/flag" --profiles dotnet --foo bar --non-interactive; then
    echo "Unknown CLI option was accepted"; exit 1
fi
if run init --name Flag --description Test --output --profiles dotnet --non-interactive; then
    echo "Missing option value was accepted"; exit 1
fi
if run init --name Flag --description Test --output "$temp/flag" --profiles dotnet --dry-run false --non-interactive; then
    echo "Boolean flag value was accepted"; exit 1
fi

# Reject symlinked output roots and symlinks nested inside output trees.
mkdir -p "$temp/external"
printf 'sentinel\n' > "$temp/external/keep.txt"
ln -s "$temp/external" "$temp/output-link"
if run init --name Linked --description Test --output "$temp/output-link" --profiles dotnet --non-interactive; then
    echo "Symlink output root was accepted"; exit 1
fi
mkdir -p "$temp/poison"
ln -s "$temp/external" "$temp/poison/docs"
if run init --name Poison --description Test --output "$temp/poison" --profiles dotnet --non-interactive; then
    echo "Nested symlink in target directory was accepted"; exit 1
fi
ln -s "$temp/external" "$temp/minimal/docs/external-link"
if run validate --output "$temp/minimal"; then
    echo "Validation followed or accepted an output symlink"; exit 1
fi
rm "$temp/minimal/docs/external-link"
[[ "$(cat "$temp/external/keep.txt")" == "sentinel" ]]

# Catalog paths must not contain parent traversal or point at symlinks.
cp -a catalog "$temp/catalog-invalid"
python3 - "$temp/catalog-invalid/profiles.json" <<'PY'
import json,sys
path=sys.argv[1]
with open(path,encoding="utf-8") as f: data=json.load(f)
data["profiles"][0]["files"][0] = "../outside.md"
with open(path,"w",encoding="utf-8") as f: json.dump(data,f)
PY
if run doctor --catalog "$temp/catalog-invalid"; then
    echo "Unsafe catalog path was accepted"; exit 1
fi

# A profile may not claim the authored base index: actual source ownership is unique.
cp -a catalog "$temp/catalog-index-collision"
python3 - "$temp/catalog-index-collision/profiles.json" <<'PY'
import json,sys
p=sys.argv[1]
with open(p,encoding="utf-8") as f: data=json.load(f)
next(item for item in data["profiles"] if item["id"]=="dotnet")["files"].append("docs/templates/index.md")
with open(p,"w",encoding="utf-8") as f: json.dump(data,f)
PY
if run doctor --catalog "$temp/catalog-index-collision"; then
    echo "Authored index ownership collision accepted"; exit 1
fi

# Reject manifest ambiguity and broken dependencies before any output is created.
cp -a catalog "$temp/catalog-collision"
python3 - "$temp/catalog-collision/profiles.json" <<'PY'
import json,sys
p=sys.argv[1]
with open(p,encoding="utf-8") as f: x=json.load(f)
x["profiles"][1]["files"].append(x["profiles"][0]["files"][0])
with open(p,"w",encoding="utf-8") as f: json.dump(x,f)
PY
if run doctor --catalog "$temp/catalog-collision"; then
    echo "Duplicate profile output path accepted"; exit 1
fi
cp -a catalog "$temp/catalog-unknown"
python3 - "$temp/catalog-unknown/profiles.json" <<'PY'
import json,sys
p=sys.argv[1]
with open(p,encoding="utf-8") as f: x=json.load(f)
x["profiles"][0]["requires"] = ["ghost-profile"]
with open(p,"w",encoding="utf-8") as f: json.dump(x,f)
PY
if run doctor --catalog "$temp/catalog-unknown"; then
    echo "Unknown profile dependency accepted"; exit 1
fi
cp -a catalog "$temp/catalog-cycle"
python3 - "$temp/catalog-cycle/profiles.json" <<'PY'
import json,sys
p=sys.argv[1]
with open(p,encoding="utf-8") as f: x=json.load(f)
x["profiles"][0]["requires"] = ["tunit"]
with open(p,"w",encoding="utf-8") as f: json.dump(x,f)
PY
if run doctor --catalog "$temp/catalog-cycle"; then
    echo "Circular profile dependency accepted"; exit 1
fi


# Broken fragments are rejected even if the Markdown file itself exists.
cp -a "$temp/minimal" "$temp/valid-fragment"
printf '\n[Existing heading](docs/standards/dotnet/testing.md#test-environment-ownership-and-bounded-parallelism)\n' >> "$temp/valid-fragment/README.md"
run validate --output "$temp/valid-fragment"
cp -a "$temp/minimal" "$temp/missing-fragment"
printf '\n[Missing heading](docs/standards/dotnet/testing.md#not-a-real-section)\n' >> "$temp/missing-fragment/README.md"
if run validate --output "$temp/missing-fragment"; then
    echo "Missing target Markdown heading was accepted"; exit 1
fi
cp -a "$temp/minimal" "$temp/missing-samepage-fragment"
printf '\n[Missing local heading](#not-a-real-heading)\n' >> "$temp/missing-samepage-fragment/docs/index.md"
if run validate --output "$temp/missing-samepage-fragment"; then
    echo "Missing same-page heading was accepted"; exit 1
fi

# Frontmatter, kebab-case and folder indexes are enforced on authored documents too.
cp -a "$temp/minimal" "$temp/invalid-frontmatter"
printf '# Missing metadata\n' > "$temp/invalid-frontmatter/docs/project/overview.md"
if run validate --output "$temp/invalid-frontmatter"; then
    echo "Missing YAML frontmatter was accepted"; exit 1
fi

cp -a "$temp/minimal" "$temp/invalid-name"
cp "$temp/invalid-name/docs/project/overview.md" "$temp/invalid-name/docs/project/Bad_Name.md"
if run validate --output "$temp/invalid-name"; then
    echo "Non-kebab-case Markdown filename was accepted"; exit 1
fi

cp -a "$temp/minimal" "$temp/invalid-index"
rm "$temp/invalid-index/docs/standards/index.md"
if run validate --output "$temp/invalid-index"; then
    echo "Missing directory index.md was accepted"; exit 1
fi

cp -a "$temp/minimal" "$temp/invalid-llms"
sed -i 's|docs/standards/dotnet/index.md|docs/standards/non-existent.md|' "$temp/invalid-llms/llms.txt"
if run validate --output "$temp/invalid-llms"; then
    echo "Broken llms.txt reference was accepted"; exit 1
fi

# llms.txt must check fragments, not merely the presence of the target file.
cp -a "$temp/minimal" "$temp/valid-llms-fragment"
printf '\n- [Existing heading](docs/standards/dotnet/testing.md#test-environment-ownership-and-bounded-parallelism)\n' >> "$temp/valid-llms-fragment/llms.txt"
run validate --output "$temp/valid-llms-fragment"
cp -a "$temp/minimal" "$temp/invalid-llms-fragment"
printf '\n- [Bad heading](docs/standards/dotnet/testing.md#no-such-heading)\n' >> "$temp/invalid-llms-fragment/llms.txt"
if run validate --output "$temp/invalid-llms-fragment"; then
    echo "Broken llms.txt heading fragment was accepted"; exit 1
fi

# Docsify is checked through actual static HTTP delivery, not a fake static HTML build.
python3 - "$temp/minimal" <<'PY'
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from threading import Thread
from urllib.request import urlopen
import sys

handler = partial(SimpleHTTPRequestHandler, directory=sys.argv[1])
httpd = ThreadingHTTPServer(("127.0.0.1", 0), handler)
thread = Thread(target=httpd.serve_forever, daemon=True)
thread.start()
try:
    root = f"http://127.0.0.1:{httpd.server_port}"
    shell = urlopen(root + "/index.html").read().decode()
    assert 'basePath: "docs/"' in shell
    assert 'homepage: "index.md"' in shell
    assert "docsify@" in shell
    assert "# Minimal" in urlopen(root + "/docs/index.md").read().decode()
    assert "# Minimal" in urlopen(root + "/llms.txt").read().decode()
finally:
    httpd.shutdown()
    thread.join(timeout=5)
    httpd.server_close()
PY


# A manifest missing owned files must be rejected.
cp "$temp/project/docs.config.json" "$temp/original.json"
python3 - "$temp/project/docs.config.json" <<'PY'
import json, sys
p = sys.argv[1]
with open(p, encoding="utf-8") as f:
    data = json.load(f)
data["GeneratedFiles"] = []
with open(p, "w", encoding="utf-8") as f:
    json.dump(data, f)
PY
if run validate --output "$temp/project"; then
    echo "Invalid output manifest was accepted"; exit 1
fi
mv "$temp/original.json" "$temp/project/docs.config.json"

if run init --name Example --description 'Test documentation' --output "$temp/invalid-profile" --profiles go --non-interactive; then
    echo "Planned profile was accepted"; exit 1
fi
if run init --name Example --description 'Test documentation' --output "$temp/invalid-skill" --profiles dotnet --skills unknown --non-interactive; then
    echo "Unknown agent skill was accepted"; exit 1
fi
if run init --name Example --description 'Test documentation' --output "$temp/invalid-module" --profiles dotnet --modules unknown --non-interactive; then
    echo "Unknown documentation module was accepted"; exit 1
fi

echo 'user-authored change' >> "$temp/project/docs/project/overview.md"
if run init "${args[@]}"; then
    echo "User edits were overwritten"; exit 1
fi
echo '[broken](missing.md)' >> "$temp/project/docs/project/overview.md"
if run validate --output "$temp/project"; then
    echo "Broken link was accepted"; exit 1
fi
echo '[escape](../../../../escape.md)' >> "$temp/project/docs/project/overview.md"
if run validate --output "$temp/project"; then
    echo "Escaping link was accepted"; exit 1
fi

# Updates default to preview. The manifest contains expected template bytes, not user modifications.
run update --output "$temp/minimal"
run update --output "$temp/minimal" --apply
echo 'authored architecture note' >> "$temp/minimal/docs/architecture/overview.md"
cp -a catalog "$temp/catalog-update"
printf '\n## Newly provided guidance\n\nChecked update path.\n' >> "$temp/catalog-update/base/docs/project/overview.md"
cp "$temp/minimal/docs/project/overview.md" "$temp/before-project.md"
run update --output "$temp/minimal" --catalog "$temp/catalog-update"
cmp "$temp/before-project.md" "$temp/minimal/docs/project/overview.md"
run update --output "$temp/minimal" --catalog "$temp/catalog-update" --apply
grep -q 'Newly provided guidance' "$temp/minimal/docs/project/overview.md"
grep -q 'authored architecture note' "$temp/minimal/docs/architecture/overview.md"
run validate --output "$temp/minimal" --catalog "$temp/catalog-update"
run update --output "$temp/minimal" --catalog "$temp/catalog-update" --apply

# New owned paths may be added, including a regenerated navigation document.
cp -a "$temp/catalog-update" "$temp/catalog-addition"
cat > "$temp/catalog-addition/docs/standards/dotnet/new-guidance.md" <<'DOC'
# New guidance

The newly installed document is part of the selected profile.
DOC
python3 - "$temp/catalog-addition/profiles.json" <<'PY'
import json,sys
p=sys.argv[1]
with open(p,encoding="utf-8") as f: data=json.load(f)
next(p for p in data["profiles"] if p["id"]=="dotnet")["files"].append("docs/standards/dotnet/new-guidance.md")
with open(p,"w",encoding="utf-8") as f: json.dump(data,f)
PY
run update --output "$temp/minimal" --catalog "$temp/catalog-addition" --apply
run validate --output "$temp/minimal" --catalog "$temp/catalog-addition"
[[ -f "$temp/minimal/docs/standards/dotnet/new-guidance.md" ]]
grep -q 'dotnet/new-guidance.md' "$temp/minimal/docs/catalog.md"
grep -q 'authored architecture note' "$temp/minimal/docs/architecture/overview.md"

# A retired template is left in place, but no longer tracked by the new manifest.
cp -a "$temp/catalog-addition" "$temp/catalog-retire"
python3 - "$temp/catalog-retire/profiles.json" <<'PY'
import json,sys
p=sys.argv[1]
with open(p,encoding="utf-8") as f: data=json.load(f)
next(p for p in data["profiles"] if p["id"]=="dotnet")["files"].remove("docs/standards/dotnet/new-guidance.md")
with open(p,"w",encoding="utf-8") as f: json.dump(data,f)
PY
run update --output "$temp/minimal" --catalog "$temp/catalog-retire" --apply
run validate --output "$temp/minimal" --catalog "$temp/catalog-retire"
python3 - "$temp/minimal/docs.config.json" <<'PY'
import json,sys
with open(sys.argv[1], encoding="utf-8") as stream:
    data = json.load(stream)
assert "docs/standards/dotnet/new-guidance.md" in data["RetiredFiles"]
assert "docs/standards/dotnet/new-guidance.md" not in data["GeneratedFiles"]
PY
[[ -f "$temp/minimal/docs/standards/dotnet/new-guidance.md" ]]
if grep -q 'dotnet/new-guidance.md' "$temp/minimal/docs/catalog.md"; then
    echo "Retired profile file remains in generated navigation"; exit 1
fi

# Competing edits of the same owned file must block the entire update.
cp -a "$temp/minimal" "$temp/conflict-project"
printf '\nUser-owned change\n' >> "$temp/conflict-project/docs/project/overview.md"
cp -a "$temp/catalog-update" "$temp/catalog-conflict"
printf '\n## Upstream changed again\n' >> "$temp/catalog-conflict/base/docs/project/overview.md"
cp "$temp/conflict-project/docs.config.json" "$temp/conflict-config.json"
if run update --output "$temp/conflict-project" --catalog "$temp/catalog-conflict" --apply; then
    echo "Competing user/template edits were overwritten"; exit 1
fi
cmp "$temp/conflict-config.json" "$temp/conflict-project/docs.config.json"
grep -q 'User-owned change' "$temp/conflict-project/docs/project/overview.md"
if grep -q 'Upstream changed again' "$temp/conflict-project/docs/project/overview.md"; then
    echo "Conflicting upstream change was applied"; exit 1
fi

# An unmanaged file can never be silently adopted just because the new template wants its path.
cp -a "$temp/minimal" "$temp/unmanaged-project"
cp -a "$temp/catalog-retire" "$temp/catalog-collision-update"
cat > "$temp/catalog-collision-update/docs/standards/dotnet/user-note.md" <<'DOC'
# The proposed new standard
DOC
python3 - "$temp/catalog-collision-update/profiles.json" <<'PY'
import json,sys
p=sys.argv[1]
with open(p,encoding="utf-8") as f: data=json.load(f)
next(p for p in data["profiles"] if p["id"]=="dotnet")["files"].append("docs/standards/dotnet/user-note.md")
with open(p,"w",encoding="utf-8") as f: json.dump(data,f)
PY
echo 'my personal note' > "$temp/unmanaged-project/docs/standards/dotnet/user-note.md"
if run update --output "$temp/unmanaged-project" --catalog "$temp/catalog-collision-update" --apply; then
    echo "Unmanaged file was adopted without explicit consent"; exit 1
fi
grep -q 'my personal note' "$temp/unmanaged-project/docs/standards/dotnet/user-note.md"

# Case-only file collisions must fail before any write, even on Linux.
cp -a "$temp/minimal" "$temp/case-conflict"
printf '%s\n' 'Owner document' > "$temp/case-conflict/docs/standards/dotnet/USER-NOTE.md"
cp "$temp/case-conflict/docs.config.json" "$temp/case-original-config.json"
if run update --output "$temp/case-conflict" --catalog "$temp/catalog-collision-update" --apply > "$temp/case-result.log" 2>&1; then
    echo "Case-only colliding path was adopted"; exit 1
fi
grep -q 'Case-only path collision' "$temp/case-result.log"
grep -q 'USER-NOTE.md' "$temp/case-result.log"
cmp "$temp/case-original-config.json" "$temp/case-conflict/docs.config.json"
grep -q 'Owner document' "$temp/case-conflict/docs/standards/dotnet/USER-NOTE.md"
[[ ! -f "$temp/case-conflict/docs/standards/dotnet/user-note.md" ]]

# A user file cannot silently become a directory required by an added profile.
cp -a "$temp/minimal" "$temp/blocked-parent"
cp -a "$temp/catalog-collision-update" "$temp/catalog-blocked-parent"
mkdir -p "$temp/catalog-blocked-parent/docs/standards/nested"
printf '# Future guidance\n' > "$temp/catalog-blocked-parent/docs/standards/nested/guide.md"
python3 - "$temp/catalog-blocked-parent/profiles.json" <<'PY'
import json,sys
p=sys.argv[1]
with open(p,encoding="utf-8") as fh: data=json.load(fh)
next(x for x in data["profiles"] if x["id"]=="dotnet")["files"].append("docs/standards/nested/guide.md")
with open(p,"w",encoding="utf-8") as fh: json.dump(data,fh)
PY
printf 'Owner file, not a directory\n' > "$temp/blocked-parent/docs/standards/nested"
cp "$temp/blocked-parent/docs.config.json" "$temp/blocked-parent-before.json"
if run update --output "$temp/blocked-parent" --catalog "$temp/catalog-blocked-parent" --apply > "$temp/blocked-parent.log" 2>&1; then
    echo "Existing file became a generator directory"; exit 1
fi
grep -q 'File blocks required directory' "$temp/blocked-parent.log"
cmp "$temp/blocked-parent-before.json" "$temp/blocked-parent/docs.config.json"
grep -q 'Owner file, not a directory' "$temp/blocked-parent/docs/standards/nested"
# A stale transaction directory must block validation and updates, never erase user data.
cp -a "$temp/minimal" "$temp/interrupted-staging"
mkdir "$temp/interrupted-staging/.docs-boilerplate-update"
printf '%s\n' 'preserve unknown staging data' > "$temp/interrupted-staging/.docs-boilerplate-update/unknown-note.txt"
cp "$temp/interrupted-staging/docs.config.json" "$temp/interrupted-staging-config.json"
if run init --name Minimal --description 'Minimal docs' --output "$temp/interrupted-staging" --profiles dotnet --non-interactive > "$temp/interrupt-init.log" 2>&1; then
    echo "Init ignored interrupted recovery journal"; exit 1
fi
grep -q 'docs-boilerplate recover' "$temp/interrupt-init.log"
if run update --output "$temp/interrupted-staging" --apply > "$temp/interrupt-update.log" 2>&1; then
    echo "Update ignored interrupted recovery journal"; exit 1
fi
grep -q 'docs-boilerplate recover' "$temp/interrupt-update.log"
if run validate --output "$temp/interrupted-staging" > "$temp/interrupt-validate.log" 2>&1; then
    echo "Validate ignored unfinished update"; exit 1
fi
grep -q 'unfinished update' "$temp/interrupt-validate.log"
if run recover --output "$temp/interrupted-staging" > "$temp/interrupt-recover.log" 2>&1; then
    echo "Recovery accepted an incomplete staging directory as a committed plan"; exit 1
fi
grep -q 'staging was interrupted' "$temp/interrupt-recover.log"
cmp "$temp/interrupted-staging-config.json" "$temp/interrupted-staging/docs.config.json"
grep -q 'preserve unknown staging data' "$temp/interrupted-staging/.docs-boilerplate-update/unknown-note.txt"
if run recover --output "$temp/minimal" > "$temp/recover-clean.log" 2>&1; then
    echo "Recovery succeeded without a journal"; exit 1
fi

# Corrupt v2 fingerprints must fail validation even if all files still exist.
cp -a "$temp/minimal" "$temp/invalid-hashes"
python3 - "$temp/invalid-hashes/docs.config.json" <<'PY'
import json,sys
p=sys.argv[1]
with open(p,encoding="utf-8") as f: data=json.load(f)
data["GeneratedHashes"].pop(next(iter(data["GeneratedHashes"])))
with open(p,"w",encoding="utf-8") as f: json.dump(data,f)
PY
if run validate --output "$temp/invalid-hashes" --catalog "$temp/catalog-retire"; then
    echo "Malformed update fingerprint manifest was accepted"; exit 1
fi


# Old repositories cannot safely adopt historical ownership without the original file hashes.
cp -a "$temp/minimal" "$temp/legacy-project"
python3 - "$temp/legacy-project/docs.config.json" <<'PY'
import json,sys
p=sys.argv[1]
with open(p,encoding="utf-8") as f: data=json.load(f)
del data["GeneratedHashes"]
with open(p,"w",encoding="utf-8") as f: json.dump(data,f)
PY
if run update --output "$temp/legacy-project" --catalog "$temp/catalog-retire" --apply; then
    echo "Legacy unfingerprinted repository was overwritten"; exit 1
fi

# Document navigation must remain complete after author-controlled additions and moves.
cp -a "$temp/minimal" "$temp/nav-authored"
cat > "$temp/nav-authored/docs/project/release-notes.md" <<'MD'
---
title: "Release notes"
description: "Example author-owned document"
kind: project
status: draft
---

# Release notes

This document was authored after initialization.
MD
if run validate --output "$temp/nav-authored"; then
    echo "Unlisted author page was accepted without navigation"; exit 1
fi
printf '%s\n' '- [Release notes](release-notes.md)' >> "$temp/nav-authored/docs/project/index.md"
if run validate --output "$temp/nav-authored"; then
    echo "Missing catalog entry was not caught"; exit 1
fi
printf '%s\n' '- [Release notes](project/release-notes.md)' >> "$temp/nav-authored/docs/catalog.md"
run validate --output "$temp/nav-authored"
if grep -q "release-notes.md" "$temp/nav-authored/llms.txt"; then
    echo "llms.txt was unexpectedly expanded into a full file inventory"; exit 1
fi
mv "$temp/nav-authored/docs/project/release-notes.md" "$temp/nav-authored/docs/project/decision-log.md"
if run validate --output "$temp/nav-authored"; then
    echo "Moved page left broken/stale navigation unnoticed"; exit 1
fi
python3 - "$temp/nav-authored/docs/project/index.md" "$temp/nav-authored/docs/catalog.md" <<'PY'
from pathlib import Path
import sys
for name in sys.argv[1:]:
    path = Path(name)
    content = path.read_text(encoding='utf-8')
    assert content.count('release-notes.md') == 1
    path.write_text(content.replace('release-notes.md', 'decision-log.md'), encoding='utf-8')
PY
run validate --output "$temp/nav-authored"

# An llms.txt entry point must keep its link to the complete catalog.
cp -a "$temp/minimal" "$temp/llms-without-catalog"
python3 - "$temp/llms-without-catalog/llms.txt" <<'PY'
from pathlib import Path
import sys
p = Path(sys.argv[1])
text = p.read_text(encoding="utf-8")
lines = [line for line in text.splitlines(keepends=True) if "](docs/catalog.md)" not in line]
assert len(lines) == len(text.splitlines(keepends=True)) - 1
p.write_text("".join(lines), encoding="utf-8")
PY
if run validate --output "$temp/llms-without-catalog"; then
    echo "Missing llms.txt catalog entrypoint was accepted"; exit 1
fi

echo "Smoke tests passed."
