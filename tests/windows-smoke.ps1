$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

Set-Location (Join-Path $PSScriptRoot '..')
$root = Join-Path $env:RUNNER_TEMP ('docs-boilerplate-windows-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $root | Out-Null
$project = 'src/DocsBoilerplate.Cli'
function Invoke-Docs {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Options)
    & dotnet run --no-build --configuration Release --project $project -- @Options
    if ($LASTEXITCODE -ne 0) { throw "CLI failed with exit code $LASTEXITCODE. Arguments: $($Options -join ' ')" }
}
function Assert-Fail {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Options)
    & dotnet run --no-build --configuration Release --project $project -- @Options
    if ($LASTEXITCODE -eq 0) { throw "Command unexpectedly succeeded: $($Options -join ' ')" }
    # PowerShell GitHub Actions treats any remaining nonzero native exit as job failure.
    $global:LASTEXITCODE = 0
}

try {
    Invoke-Docs doctor
    Invoke-Docs list

    $target = Join-Path $root 'generated'
    Invoke-Docs init --name WindowsSmoke --description 'Windows documentation check' --output $target --profiles "dotnet,blazor,mudblazor" --site docsify --non-interactive
    Invoke-Docs validate --output $target
    if (-not (Test-Path (Join-Path $target 'bootstrap.md'))) { throw 'Missing temporary stage-two introduction' }
    if (-not (Test-Path (Join-Path $target '.agents/skills/project-intake/SKILL.md'))) { throw 'Missing temporary stage-two skill' }
    if ((Get-Content (Join-Path $target 'README.md') -TotalCount 1) -ne '# WindowsSmoke') {
        throw 'Root README begins with YAML metadata or a missing project heading'
    }
    if (-not (Test-Path (Join-Path $target 'index.html'))) { throw 'Missing viewer' }
    if (-not (Test-Path (Join-Path $target 'llms.txt'))) { throw 'Missing agent index' }
    if (-not (Test-Path (Join-Path $target 'docs/standards/blazor/index.md'))) { throw 'Missing folder index' }
    $agentInstructions = Get-Content (Join-Path $target 'AGENTS.md') -Raw
    if ($agentInstructions -notmatch 'dotnet run --project src/DocsBoilerplate.Cli -- recover') { throw 'Recovery instruction does not use generator source checkout' }
    if ($agentInstructions -match '(?<![\w./-])docs-boilerplate\s+(?:init|update|validate|recover|list|doctor)\b') { throw 'Agent instructions require an unavailable installed CLI' }
    if ($agentInstructions -notmatch 'status: active') { throw 'Agent instructions do not distinguish metadata from evidence' }

    Invoke-Docs bootstrap-complete --output $target
    if (-not (Test-Path (Join-Path $target 'bootstrap.md'))) { throw 'Cleanup preview unexpectedly removed intake file' }
    Invoke-Docs bootstrap-complete --output $target --apply
    Invoke-Docs validate --output $target
    if (Test-Path (Join-Path $target 'bootstrap.md')) { throw 'Stage-two cleanup did not remove intake file' }
    if (Test-Path (Join-Path $target '.agents/skills/project-intake/SKILL.md')) { throw 'Stage-two cleanup did not remove temporary skill' }

    Invoke-Docs update --output $target
    Invoke-Docs update --output $target --apply
    Invoke-Docs validate --output $target

    $userFile = Join-Path $target 'docs/project/overview.md'
    Add-Content -Path $userFile -Value 'Human-authored note'
    Invoke-Docs update --output $target --apply
    if (-not ((Get-Content $userFile -Raw) -match 'Human-authored note')) { throw 'User modification was lost' }

    # Authored nested documentation must be linked by its parent, not just the catalog.
    $guides = Join-Path $target 'docs/project/guides'
    New-Item -ItemType Directory -Path $guides | Out-Null
    Set-Content -Encoding utf8 -LiteralPath (Join-Path $guides 'index.md') -Value @'
---
id: win-guides-index
title: "Guides"
description: "Windows navigation test directory"
kind: index
status: draft
---

# Guides

- [Guide](guide.md)
'@
    Set-Content -Encoding utf8 -LiteralPath (Join-Path $guides 'guide.md') -Value @'
---
id: win-guides-guide
title: "Guide"
description: "Windows navigation test page"
kind: project
status: draft
---

# Guide
'@
    Add-Content -LiteralPath (Join-Path $target 'docs/catalog.md') -Value '- [Guides](project/guides/index.md)'
    Add-Content -LiteralPath (Join-Path $target 'docs/catalog.md') -Value '- [Guide](project/guides/guide.md)'
    Assert-Fail validate --output $target
    Add-Content -LiteralPath (Join-Path $target 'docs/project/index.md') -Value '- [Guides](guides/index.md)'
    Invoke-Docs validate --output $target
    Invoke-Docs update --output $target --apply
    Invoke-Docs validate --output $target
    if (-not ((Get-Content (Join-Path $target 'docs/project/index.md') -Raw) -match 'guides/index.md')) {
        throw 'Upstream update erased authored parent navigation'
    }

    $variants = Join-Path $root 'optional-conventions'
    Invoke-Docs init --name OptionalConventions --description 'Optional standards' --output $variants --profiles 'test-naming-convention,http-etag-concurrency,masstransit-eventbus-composition' --site none --non-interactive
    Invoke-Docs validate --output $variants
    foreach ($file in @('docs/standards/dotnet/test-naming-convention.md', 'docs/standards/http/etag-concurrency.md', 'docs/standards/dotnet/eventbus-composition.md')) {
        if (-not (Test-Path (Join-Path $variants $file))) { throw "Missing opt-in profile content: $file" }
    }
    if (Test-Path (Join-Path $variants 'docs/standards/dotnet/ef-core.md')) { throw 'Unselected data access profile leaked' }

    $final = Join-Path $root 'final-optional'
    Invoke-Docs init --name FinalConventions --description 'Selected documentation and UI rules' --output $final --profiles 'ui-direct-dto-forms' --site none --non-interactive
    Invoke-Docs validate --output $final
    foreach ($file in @('docs/standards/document-identifiers.md', 'docs/standards/integrations/reliable-synchronization.md', 'docs/standards/ui/direct-dto-forms.md', 'docs/standards/http/client-contracts.md')) {
        if (-not (Test-Path (Join-Path $final $file))) { throw "Missing final opt-in profile: $file" }
    }
    if (Test-Path (Join-Path $final 'docs/standards/blazor')) { throw 'Unselected Blazor profile leaked' }

    $plain = Join-Path $root 'plain-docs'
    Invoke-Docs init --name PlainDocs --description 'No selected stack' --output $plain --profiles none --site none --non-interactive
    Invoke-Docs validate --output $plain
    if (Test-Path (Join-Path $plain 'docs/standards/dotnet')) { throw 'No-stack mode installed .NET guidance' }
    $plainManifest = Get-Content (Join-Path $plain 'docs.config.json') -Raw | ConvertFrom-Json
    if (@($plainManifest.Profiles).Count -ne 0) { throw 'No-stack mode selected profiles' }
    if (@($plainManifest.Modules).Count -ne 7 -or @($plainManifest.Skills).Count -ne 5) { throw 'Default documentation modules or skills missing' }
    if (-not (Test-Path (Join-Path $plain 'docs/standards/integrations/reliable-synchronization.md'))) { throw 'Baseline integration missing' }

    $headless = Join-Path $root 'headless'
    Invoke-Docs init --name Headless --description 'Markdown only' --output $headless --profiles dotnet --site none --non-interactive
    Invoke-Docs validate --output $headless
    if (Test-Path (Join-Path $headless 'index.html')) { throw 'Headless generation included viewer' }

    $xunit = Join-Path $root 'xunit'
    Invoke-Docs init --name XUnit --description 'xUnit on Windows' --output $xunit --profiles 'xunit,testcontainers' --non-interactive
    if (-not (Test-Path (Join-Path $xunit 'docs/standards/dotnet/xunit.md'))) { throw 'Missing xUnit standard' }
    Assert-Fail init --name BothFrameworks --description 'Invalid selection' --output (Join-Path $root 'both-tests') --profiles 'tunit,xunit' --non-interactive
    $s3 = Join-Path $root 'minio'
    Invoke-Docs init --name S3 --description 'MinIO SDK' --output $s3 --profiles 'infra-s3-client-minio' --non-interactive
    if (-not (Test-Path (Join-Path $s3 'docs/standards/infrastructure/s3-client-minio.md'))) { throw 'Missing MinIO SDK profile' }

    Assert-Fail init --name Invalid --description 'Invalid' --output (Join-Path $root 'invalid') --profiles dotnet --site retired-site --non-interactive
    Assert-Fail init --name Unsafe --description 'Unsafe' --output (Join-Path $root 'unsafe') --profiles dotnet --unknown-flag --non-interactive
    Write-Host 'Windows smoke tests passed.'
}
finally {
    Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
}
