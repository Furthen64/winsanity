[CmdletBinding()]
param(
    [switch]$SkipInstallTool
)

$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$publishDir = Join-Path $root 'installer\publish'
$outputDir = Join-Path $root 'installer\output'

$isccPath = $null

$candidate = 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe'
if (Test-Path $candidate) {
    $isccPath = $candidate
}
else {
    $iscc = Get-Command iscc.exe -ErrorAction SilentlyContinue
    if ($iscc) {
        $isccPath = $iscc.Source
    }
}

if (-not $isccPath -and -not $SkipInstallTool) {
    Write-Host 'Inno Setup 6 not found - installing via chocolatey...'
    choco install innosetup -y
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to install Inno Setup via chocolatey.'
    }
    $isccPath = 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe'
}

if (-not $isccPath -or -not (Test-Path $isccPath)) {
    throw 'Inno Setup compiler (ISCC.exe) not found. Install Inno Setup 6 or rerun without -SkipInstallTool.'
}

if (Test-Path $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

Write-Host 'Publishing self-contained single-file build...'
dotnet publish (Join-Path $root 'WUWatch\WUWatch.csproj') `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    throw 'dotnet publish failed.'
}

if (Test-Path $outputDir) {
    Remove-Item -LiteralPath $outputDir -Recurse -Force
}

Write-Host "Compiling installer with $isccPath ..."
& $isccPath (Join-Path $root 'installer\wuinno.iss')
if ($LASTEXITCODE -ne 0) {
    throw 'Inno Setup compilation failed.'
}

Write-Host ''
$built = Get-ChildItem -LiteralPath $outputDir -Filter '*.exe' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($built) {
    Write-Host "Installer created: $($built.FullName)"
}
else {
    Write-Host 'No installer output found.'
}