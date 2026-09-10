[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

dotnet build (Join-Path $PSScriptRoot 'WUWatch.slnx') -c $Configuration

exit $LASTEXITCODE