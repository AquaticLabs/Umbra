param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [string]$GameManagedDir = $env:UMBRA_GAME_MANAGED_DIR
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

if ([string]::IsNullOrWhiteSpace($GameManagedDir)) {
    $managedCandidates = @(
        "C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed",
        "C:\Program Files\Steam\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed",
        "D:\SteamLibrary\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed",
        "D:\Steam\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed",
        "E:\SteamLibrary\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed"
    )
    $GameManagedDir = $managedCandidates | Where-Object { Test-Path (Join-Path $_ "RoR2.dll") } | Select-Object -First 1
}

if ([string]::IsNullOrWhiteSpace($GameManagedDir) -or -not (Test-Path (Join-Path $GameManagedDir "RoR2.dll"))) {
    throw "Risk of Rain 2 assemblies were not found. Set UMBRA_GAME_MANAGED_DIR to the game's Risk of Rain 2_Data\Managed folder."
}

$msbuildCommand = Get-Command msbuild.exe -ErrorAction SilentlyContinue
$msbuildPath = if ($msbuildCommand) { $msbuildCommand.Source } else { $null }

if (-not $msbuildPath) {
    $vswhere = "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $installPath = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
        if ($installPath) {
            $candidate = Join-Path $installPath "MSBuild\Current\Bin\MSBuild.exe"
            if (Test-Path $candidate) { $msbuildPath = $candidate }
        }
    }
}

if (-not $msbuildPath) {
    throw "MSBuild was not found. Install Visual Studio 2022 or Visual Studio Build Tools with the MSBuild component."
}

Write-Host "Building Umbra $Configuration"
Write-Host "Game assemblies: $GameManagedDir"
& $msbuildPath (Join-Path $projectRoot "UmbraMenu.sln") /t:Build "/p:Configuration=$Configuration" "/p:GameManagedDir=$GameManagedDir" /m /v:minimal /nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$artifact = Join-Path $projectRoot "bin\$Configuration\UmbraMenu.dll"
Write-Host "Built: $artifact"
