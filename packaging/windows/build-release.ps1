<#
.SYNOPSIS
    ScrcpyDeX — Windows Release Packaging Script
.DESCRIPTION
    Compiles ScrcpyDex.App into a self-contained single-file win-x64 executable,
    assembles runtime distribution files, generates the release zip, and computes SHA-256 sums.
#>

[CmdletBinding()]
param(
    [string]$Version = "2.0.0",
    [string]$Configuration = "Release",
    [string]$OutputDir = ""
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $scriptDir) { $scriptDir = (Get-Location).Path }
$repoRoot = Split-Path -Parent (Split-Path -Parent $scriptDir)
if (-not (Test-Path (Join-Path $repoRoot "src\ScrcpyDex.App\ScrcpyDex.App.csproj"))) {
    $repoRoot = (Get-Location).Path
}

if (-not $OutputDir) {
    $OutputDir = Join-Path $repoRoot "publish"
}

if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "   ScrcpyDeX — Windows Release Packaging Builder " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "Version:       $Version"
Write-Host "Configuration: $Configuration"
Write-Host "Repository:    $repoRoot"
Write-Host "Output:        $OutputDir"
Write-Host ""

$stageDir = Join-Path ([System.IO.Path]::GetTempPath()) ("scrcpydex-win-stage-" + [System.Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $stageDir -Force | Out-Null

try {
    $appProject = Join-Path $repoRoot "src\ScrcpyDex.App\ScrcpyDex.App.csproj"
    if (-not (Test-Path $appProject)) {
        throw "ScrcpyDex.App project not found at $appProject"
    }

    Write-Host "[1/4] Publishing ScrcpyDex.App (win-x64 single file)..." -ForegroundColor Yellow
    & dotnet publish $appProject `
        -c $Configuration `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -p:Version=$Version `
        -p:AssemblyVersion="${Version}.0" `
        -p:FileVersion="${Version}.0" `
        -o $stageDir
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE"
    }

    function Copy-IfNotSame($src, $dst) {
        if (-not $src -or -not (Test-Path $src)) { return }
        $srcFull = (Resolve-Path $src).Path
        $parent = Split-Path -Parent $dst
        if ($parent -and -not (Test-Path $parent)) {
            New-Item -ItemType Directory -Path $parent -Force | Out-Null
        }
        if (Test-Path $dst) {
            $dstFull = (Resolve-Path $dst).Path
            if ($srcFull -eq $dstFull) { return }
        }
        Copy-Item -Path $srcFull -Destination $dst -Force
    }

    Write-Host "[2/4] Staging release companion files..." -ForegroundColor Yellow
    
    # Server JAR
    $jarPath = Join-Path $repoRoot "server\scrcpydex-server.jar"
    if (-not (Test-Path $jarPath)) { $jarPath = Join-Path $OutputDir "scrcpydex-server.jar" }
    Copy-IfNotSame $jarPath (Join-Path $stageDir "scrcpydex-server.jar")
    Copy-IfNotSame $jarPath (Join-Path $OutputDir "scrcpydex-server.jar")

    # Stop script
    $stopBat = Join-Path $repoRoot "stop-dex.bat"
    if (-not (Test-Path $stopBat)) { $stopBat = Join-Path $repoRoot "scripts\windows\stop-dex.bat" }
    if (Test-Path $stopBat) {
        Copy-IfNotSame $stopBat (Join-Path $stageDir "stop-dex.bat")
        Copy-IfNotSame $stopBat (Join-Path $OutputDir "stop-dex.bat")
    }

    # Documentation & Licenses
    $readme = Join-Path $repoRoot "README.md"
    Copy-IfNotSame $readme (Join-Path $stageDir "README.md")
    Copy-IfNotSame $readme (Join-Path $OutputDir "README.md")

    $disclaimer = Join-Path $repoRoot "docs\DISCLAIMER.md"
    if (-not (Test-Path $disclaimer)) { $disclaimer = Join-Path $OutputDir "DISCLAIMER.md" }
    Copy-IfNotSame $disclaimer (Join-Path $stageDir "DISCLAIMER.md")
    Copy-IfNotSame $disclaimer (Join-Path $OutputDir "DISCLAIMER.md")

    $license = Join-Path $OutputDir "LICENSE"
    if (-not (Test-Path $license)) { $license = Join-Path $repoRoot "LICENSE" }
    Copy-IfNotSame $license (Join-Path $stageDir "LICENSE")
    Copy-IfNotSame $license (Join-Path $OutputDir "LICENSE")

    $notices = Join-Path $repoRoot "docs\THIRD_PARTY_NOTICES.md"
    if (-not (Test-Path $notices)) { $notices = Join-Path $OutputDir "THIRD_PARTY_NOTICES.md" }
    Copy-IfNotSame $notices (Join-Path $stageDir "THIRD_PARTY_NOTICES.md")
    Copy-IfNotSame $notices (Join-Path $OutputDir "THIRD_PARTY_NOTICES.md")

    # Config Templates
    $configDir = Join-Path $repoRoot "config"
    Copy-IfNotSame (Join-Path $configDir "settings.json") (Join-Path $stageDir "config\settings.json")
    Copy-IfNotSame (Join-Path $configDir "settings.json") (Join-Path $OutputDir "config\settings.json")
    Copy-IfNotSame (Join-Path $configDir "settings.schema.json") (Join-Path $stageDir "config\settings.schema.json")
    Copy-IfNotSame (Join-Path $configDir "settings.schema.json") (Join-Path $OutputDir "config\settings.schema.json")

    # Copy published exe and pdb to output directory
    Copy-IfNotSame (Join-Path $stageDir "ScrcpyDex.App.exe") (Join-Path $OutputDir "ScrcpyDex.App.exe")
    Copy-IfNotSame (Join-Path $stageDir "ScrcpyDex.App.pdb") (Join-Path $OutputDir "ScrcpyDex.App.pdb")

    Write-Host "[3/4] Creating ZIP distribution package..." -ForegroundColor Yellow
    $zipName = "ScrcpyDeX-v${Version}-win-x64.zip"
    $zipPath = Join-Path $OutputDir $zipName
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Compress-Archive -Path (Join-Path $stageDir "*") -DestinationPath $zipPath -CompressionLevel Optimal

    Write-Host "[4/4] Calculating SHA-256 checksums..." -ForegroundColor Yellow
    $hashFiles = @(
        $zipName,
        "ScrcpyDex.App.exe",
        "scrcpydex-server.jar",
        "stop-dex.bat",
        "README.md",
        "DISCLAIMER.md",
        "LICENSE",
        "THIRD_PARTY_NOTICES.md",
        "ScrcpyDex.App.pdb",
        "config/settings.json",
        "config/settings.schema.json"
    )

    $hashLines = [System.Collections.Generic.List[string]]::new()
    foreach ($item in $hashFiles) {
        $localPath = Join-Path $OutputDir $item
        if (Test-Path $localPath) {
            $hash = (Get-FileHash -Path $localPath -Algorithm SHA256).Hash.ToLowerInvariant()
            $hashLines.Add("$hash  $item")
        }
    }

    $shaFile = Join-Path $OutputDir "SHA256SUMS.txt"
    Set-Content -Path $shaFile -Value $hashLines -Encoding utf8

    Write-Host "=================================================" -ForegroundColor Cyan
    Write-Host "Package created: $zipPath" -ForegroundColor Green
    Write-Host "Checksums saved: $shaFile" -ForegroundColor Green
    Write-Host "=================================================" -ForegroundColor Cyan
}
finally {
    if (Test-Path $stageDir) {
        Remove-Item -Path $stageDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
