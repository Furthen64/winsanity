[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$exe = Join-Path $PSScriptRoot "WUWatch\bin\$Configuration\net10.0-windows\win-x64\WUWatch.exe"

if (-not (Test-Path $exe)) {
    & (Join-Path $PSScriptRoot 'winbuild.ps1') -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

Start-Process -FilePath $exe