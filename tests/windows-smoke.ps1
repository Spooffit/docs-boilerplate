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
}

try {
    Invoke-Docs doctor
    Invoke-Docs list

    $target = Join-Path $root 'generated'
    Invoke-Docs init --name WindowsSmoke --description 'Windows documentation check' --output $target --profiles dotnet,blazor,mudblazor --site docsify --non-interactive
    Invoke-Docs validate --output $target
    if (-not (Test-Path (Join-Path $target 'index.html'))) { throw 'Missing viewer' }
    if (-not (Test-Path (Join-Path $target 'llms.txt'))) { throw 'Missing agent index' }
    if (-not (Test-Path (Join-Path $target 'docs/standards/blazor/index.md'))) { throw 'Missing folder index' }

    Invoke-Docs update --output $target
    Invoke-Docs update --output $target --apply
    Invoke-Docs validate --output $target

    $userFile = Join-Path $target 'docs/project/overview.md'
    Add-Content -Path $userFile -Value 'Human-authored note'
    Invoke-Docs update --output $target --apply
    if (-not ((Get-Content $userFile -Raw) -match 'Human-authored note')) { throw 'User modification was lost' }

    $headless = Join-Path $root 'headless'
    Invoke-Docs init --name Headless --description 'Markdown only' --output $headless --profiles dotnet --site none --non-interactive
    Invoke-Docs validate --output $headless
    if (Test-Path (Join-Path $headless 'index.html')) { throw 'Headless generation included viewer' }

    Assert-Fail init --name Invalid --description 'Invalid' --output (Join-Path $root 'invalid') --profiles dotnet --site retired-site --non-interactive
    Assert-Fail init --name Unsafe --description 'Unsafe' --output (Join-Path $root 'unsafe') --profiles dotnet --unknown-flag --non-interactive
    Write-Host 'Windows smoke tests passed.'
}
finally {
    Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
}
