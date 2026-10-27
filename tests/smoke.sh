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
[[ -f "$temp/minimal/docs/catalog.md" ]]
grep -q "dotnet/testing.md" "$temp/minimal/docs/catalog.md"
[[ -f "$temp/minimal/docs/standards/dotnet/testing.md" ]]
[[ -f "$temp/minimal/docs/templates/dotnet-backend-feature.md" ]]
[[ ! -e "$temp/minimal/docs/testing/strategy.md" ]]
[[ ! -e "$temp/minimal/.agents" ]]
[[ ! -e "$temp/minimal/docs/templates/blazor-frontend-feature.md" ]]
if grep -q "blazor/components.md" "$temp/minimal/docs/catalog.md"; then echo "Unselected standard leaked into catalog"; exit 1; fi
[[ ! -f "$temp/minimal/zensical.toml" ]]

# Optional HTTP and persistence profiles must compose without implicit extra libraries.
run init --name Web --description 'HTTP and storage' --output "$temp/web" --profiles aspnet-mvc,fluentvalidation,postgresql,cqrs,ef-core --site zensical --non-interactive
run validate --output "$temp/web"
[[ -f "$temp/web/docs/standards/aspnet/http-api.md" ]]
[[ -f "$temp/web/docs/standards/aspnet/errors-and-validation.md" ]]
[[ -f "$temp/web/docs/standards/aspnet/mvc-controllers.md" ]]
[[ -f "$temp/web/docs/standards/dotnet/fluentvalidation.md" ]]
[[ -f "$temp/web/docs/standards/postgresql/persistence.md" ]]
[[ -f "$temp/web/docs/standards/architecture/cqrs.md" ]]
[[ -f "$temp/web/docs/standards/dotnet/project-files.md" ]]
[[ -f "$temp/web/docs/standards/dotnet/ef-core.md" ]]
[[ ! -e "$temp/web/docs/standards/blazor" ]]
[[ ! -e "$temp/web/docs/standards/dotnet/dapper.md" ]]
[[ ! -e "$temp/web/docs/standards/dotnet/testcontainers.md" ]]
[[ ! -e "$temp/web/docs/standards/dotnet/tunit.md" ]]
run init --name Web --description 'HTTP and storage' --output "$temp/web" --profiles aspnet-mvc,fluentvalidation,postgresql,cqrs,ef-core --site zensical --non-interactive

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
[[ ! -e "$temp/storage/docs/standards/dotnet" ]]

if command -v zensical >/dev/null; then
    ( cd "$temp/web" && zensical build --strict )
    [[ -f "$temp/web/site/index.html" ]]
fi

# Testing add-ons are independent and resolved through their .NET prerequisite.
run init --name Addons --description 'Testing tools' --output "$temp/addons" --profiles tunit,testcontainers --non-interactive
run validate --output "$temp/addons"
[[ -f "$temp/addons/docs/standards/dotnet/testing.md" ]]
[[ -f "$temp/addons/docs/standards/dotnet/tunit.md" ]]
[[ -f "$temp/addons/docs/standards/dotnet/testcontainers.md" ]]
[[ ! -e "$temp/addons/docs/standards/blazor" ]]

args=(--name Example --description 'Test documentation' --output "$temp/project" --profiles dotnet,ef-core,blazor,mudblazor,modular-monolith --skills docs-review,docs-maintain --modules testing,operations --site zensical --non-interactive)
run init "${args[@]}" --dry-run
[[ ! -d "$temp/project" ]] || { echo "Dry run unexpectedly wrote files"; exit 1; }

run init "${args[@]}"
run validate --output "$temp/project"
[[ -f "$temp/project/docs/standards/dotnet/ef-core.md" ]]
[[ -f "$temp/project/docs/standards/blazor/mudblazor.md" ]]
grep -q "blazor/mudblazor.md" "$temp/project/docs/catalog.md"
grep -q "testing/strategy.md" "$temp/project/docs/catalog.md"
[[ -f "$temp/project/docs/testing/strategy.md" ]]
[[ -f "$temp/project/docs/operations/observability.md" ]]
[[ -f "$temp/project/.agents/skills/docs-review/SKILL.md" ]]
[[ ! -e "$temp/project/docs/standards/dotnet/dapper.md" ]]
[[ ! -e "$temp/project/docs/project/glossary.md" ]]
[[ ! -e "$temp/project/.agents/skills/docs-architecture" ]]
[[ ! -e "$temp/project/src" ]]
[[ ! -e "$temp/project/catalog" ]]
[[ -f "$temp/project/zensical.toml" ]]

# Messaging and query specifications stay opt-in and do not imply a particular database.
run init --name Integration --description 'Message handling' --output "$temp/integration" --profiles masstransit-rabbitmq,ardalis-specification --non-interactive
run validate --output "$temp/integration"
[[ -f "$temp/integration/docs/standards/dotnet/masstransit-rabbitmq.md" ]]
[[ -f "$temp/integration/docs/standards/dotnet/ardalis-specification.md" ]]
[[ -f "$temp/integration/docs/standards/dotnet/conventions.md" ]]
[[ ! -e "$temp/integration/docs/standards/dotnet/ef-core.md" ]]
[[ ! -e "$temp/integration/docs/standards/postgresql" ]]

# bUnit requires Blazor, but does not implicitly choose MudBlazor.
run init --name Components --description 'UI component tests' --output "$temp/components" --profiles bunit --non-interactive
run validate --output "$temp/components"
[[ -f "$temp/components/docs/standards/blazor/bunit.md" ]]
[[ -f "$temp/components/docs/standards/blazor/components.md" ]]
[[ -f "$temp/components/docs/templates/blazor-frontend-feature.md" ]]
[[ -f "$temp/components/docs/standards/dotnet/project-files.md" ]]
[[ ! -e "$temp/components/docs/standards/blazor/mudblazor.md" ]]

if command -v zensical >/dev/null; then
    run init --name All --description 'Full stack' --output "$temp/full-stack" --profiles aspnet-mvc,ef-core,postgresql,fluentvalidation,cqrs,masstransit-rabbitmq,ardalis-specification,blazor,mudblazor,bunit --site zensical --non-interactive
    run validate --output "$temp/full-stack"
    ( cd "$temp/full-stack" && zensical build --strict )
    [[ -f "$temp/full-stack/site/index.html" ]]
fi

# An identical repeated initialization must not alter authored extra files.
echo 'custom' > "$temp/project/my-notes.txt"
run init "${args[@]}"

if command -v zensical >/dev/null; then
    ( cd "$temp/project" && zensical build --strict )
    [[ -f "$temp/project/site/index.html" ]]
    run init "${args[@]}"
fi

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
echo "Smoke tests passed."
