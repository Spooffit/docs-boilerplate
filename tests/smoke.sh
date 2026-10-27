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
[[ -f "$temp/minimal/docs/standards/dotnet/testing.md" ]]
[[ ! -e "$temp/minimal/docs/testing/strategy.md" ]]
[[ ! -e "$temp/minimal/.agents" ]]
[[ ! -f "$temp/minimal/zensical.toml" ]]

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
[[ -f "$temp/project/docs/testing/strategy.md" ]]
[[ -f "$temp/project/docs/operations/observability.md" ]]
[[ -f "$temp/project/.agents/skills/docs-review/SKILL.md" ]]
[[ ! -e "$temp/project/docs/standards/dotnet/dapper.md" ]]
[[ ! -e "$temp/project/docs/project/glossary.md" ]]
[[ ! -e "$temp/project/.agents/skills/docs-architecture" ]]
[[ ! -e "$temp/project/src" ]]
[[ ! -e "$temp/project/catalog" ]]
[[ -f "$temp/project/zensical.toml" ]]

# An identical repeated initialization must not alter authored extra files.
echo 'custom' > "$temp/project/my-notes.txt"
run init "${args[@]}"

if command -v zensical >/dev/null; then
    ( cd "$temp/project" && zensical build --strict )
    [[ -f "$temp/project/site/index.html" ]]
    run init "${args[@]}"
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
