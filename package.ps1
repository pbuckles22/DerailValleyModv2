param (
    [switch]$NoArchive,
    [string]$OutputDirectory = $PSScriptRoot,
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    # Set when invoked from csproj PostBuild (already compiled into build/).
    [switch]$SkipBuild
)

Set-Location "$PSScriptRoot"

# Rebuild into build/ unless PostBuild already did (avoids Release ↔ package.ps1 recursion).
# Tests alone can leave build/ stale (Core is linked into YardMasterSuite.dll).
if (-not $SkipBuild) {
    & dotnet build "YardMasterSuite/YardMasterSuite.csproj" -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed (exit $LASTEXITCODE); not packaging stale build/YardMasterSuite.dll"
    }
}

$FilesToInclude = @("info.json", "build/YardMasterSuite.dll")
foreach ($required in $FilesToInclude) {
    if (-not (Test-Path $required)) {
        throw "Missing $required; build before packaging."
    }
}

$modInfo = Get-Content -Raw -Path "info.json" | ConvertFrom-Json
$modId = $modInfo.Id
$modVersion = $modInfo.Version

$outFull = [System.IO.Path]::GetFullPath($OutputDirectory)
$repoFull = [System.IO.Path]::GetFullPath($PSScriptRoot)
if ($NoArchive -and ($outFull.TrimEnd('\') -eq $repoFull.TrimEnd('\'))) {
    throw "-NoArchive to the repo root would overwrite the YardMasterSuite project folder. Pass -OutputDirectory to the game Mods folder."
}

$DistDir = Join-Path $OutputDirectory "dist"
if ($NoArchive) {
    $ZipWorkDir = $OutputDirectory
} else {
    $ZipWorkDir = Join-Path $DistDir "tmp"
}
$ZipOutDir = Join-Path $ZipWorkDir $modId

New-Item "$ZipOutDir" -ItemType Directory -Force | Out-Null
Get-ChildItem -Path $ZipOutDir -Filter "*.cache" -ErrorAction SilentlyContinue | Remove-Item -Force
# Remove stale sibling Core from older deploys (logic is now inside YardMasterSuite.dll).
Remove-Item -Force -ErrorAction SilentlyContinue (Join-Path $ZipOutDir "YardMasterSuite.Core.dll")
Copy-Item -Force -Path $FilesToInclude -Destination "$ZipOutDir"

$IconDir = "YardMasterSuite/Icons"
if (Test-Path $IconDir) {
    $iconsOut = Join-Path $ZipOutDir "Icons"
    New-Item "$iconsOut" -ItemType Directory -Force | Out-Null
    Copy-Item -Force -Path "$IconDir/*.png" -Destination $iconsOut
}

if (!$NoArchive) {
    $FILE_NAME = Join-Path $DistDir "${modId}_v$modVersion.zip"
    New-Item "$DistDir" -ItemType Directory -Force | Out-Null
    if (Test-Path $FILE_NAME) { Remove-Item $FILE_NAME -Force }
    Compress-Archive -CompressionLevel Fastest -Path "$ZipOutDir/*" -DestinationPath "$FILE_NAME"
    Remove-Item -Recurse -Force (Join-Path $DistDir "tmp")
    Write-Host "Packaged: $FILE_NAME"
} else {
    Write-Host "Copied to: $ZipOutDir"
}
