#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."
project=src/DocsBoilerplate.Cli
temp="$(mktemp -d)"
trap 'rm -rf "$temp"' EXIT
run() { dotnet run --no-build --configuration Release --project "$project" -- "$@"; }
run doctor
run list
run init --name Example --description 'Test documentation' --output "$temp/project" --profiles dotnet,ef-core,blazor,mudblazor,modular-monolith --non-interactive --dry-run
[[ ! -d "$temp/project" ]] || { echo "Dry run unexpectedly wrote files"; exit 1; }
run init --name Example --description 'Test documentation' --output "$temp/project" --profiles dotnet,ef-core,blazor,mudblazor,modular-monolith --non-interactive
run validate --output "$temp/project"
[[ -f "$temp/project/standards/dotnet/ef-core.md" ]]
[[ -f "$temp/project/standards/blazor/mudblazor.md" ]]
[[ ! -e "$temp/project/standards/dotnet/dapper.md" ]]
[[ ! -e "$temp/project/src" ]]
[[ ! -e "$temp/project/catalog" ]]
run init --name Example --description 'Test documentation' --output "$temp/project" --profiles dotnet,ef-core,blazor,mudblazor,modular-monolith --non-interactive
if run init --name Example --description 'Test documentation' --output "$temp/project" --profiles go --non-interactive; then
  echo "Planned profile was accepted"; exit 1
fi
echo 'user-authored change' >> "$temp/project/docs/project/overview.md"
if run init --name Example --description 'Test documentation' --output "$temp/project" --profiles dotnet,ef-core,blazor,mudblazor,modular-monolith --non-interactive; then
  echo "User edits were overwritten"; exit 1
fi
echo '[broken](missing.md)' >> "$temp/project/docs/project/overview.md"
if run validate --output "$temp/project"; then
  echo "Broken link was accepted"; exit 1
fi
echo "Smoke tests passed."
