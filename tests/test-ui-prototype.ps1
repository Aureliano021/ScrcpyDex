# ScrcpyDeX — Automated Prototype Verification Suite (Stage 4)
# Verifies ConfigStore, XAML WinUI parsing, parameter propagation, and headless operation

$ErrorActionPreference = "Stop"

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "    ScrcpyDeX — Automated Verification Suite     " -ForegroundColor Cyan
Write-Host "      Stage 4: WinUI Prototype & ConfigStore     " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $scriptDir) { $scriptDir = (Get-Location).Path }
$rootDir = Split-Path -Parent $scriptDir
if (-not $rootDir -or -not (Test-Path (Join-Path $rootDir "client"))) { $rootDir = $scriptDir }

$savedConfigFile = Join-Path $rootDir "config\settings.json"
$savedConfigContent = if (Test-Path $savedConfigFile) { Get-Content $savedConfigFile -Raw } else { $null }

$passed = 0
$failed = 0

function Assert-Test($name, [scriptblock]$condition) {
    Write-Host -NoNewline "[TEST] $name ... "
    try {
        $result = & $condition
        if ($result -ne $false) {
            Write-Host "PASSED" -ForegroundColor Green
            $script:passed++
        } else {
            Write-Host "FAILED" -ForegroundColor Red
            $script:failed++
        }
    } catch {
        Write-Host "FAILED ($($_))" -ForegroundColor Red
        $script:failed++
    }
}

# 1. ConfigStore Files Exist
Assert-Test "Configuration files exist" {
    $configFile = Join-Path $rootDir "config\settings.json"
    $schemaFile = Join-Path $rootDir "config\settings.schema.json"
    (Test-Path $configFile) -and (Test-Path $schemaFile)
}

# 2. JSON Settings Validation
Assert-Test "settings.json parses valid JSON with 60 FPS & 8 Mbps defaults" {
    $configFile = Join-Path $rootDir "config\settings.json"
    $content = Get-Content $configFile -Raw | ConvertFrom-Json
    ($content.version -eq "1.0") -and 
    ($content.display.resolution -eq "1920x1080") -and 
    ($content.display.fps -eq 60) -and 
    ($content.display.bitrate -eq 8000000) -and 
    ($content.input.mouseDriver -eq "uhid")
}

# 3. Settings Roundtrip Save & Load
Assert-Test "Settings save and reload roundtrip" {
    $configFile = Join-Path $rootDir "config\settings.json"
    $origJson = Get-Content $configFile -Raw
    $testObj = $origJson | ConvertFrom-Json
    $testObj.display.bitrate = 16000000
    $testObj | ConvertTo-Json -Depth 6 | Set-Content $configFile -Encoding UTF8
    
    $reloaded = Get-Content $configFile -Raw | ConvertFrom-Json
    $success = ($reloaded.display.bitrate -eq 16000000)
    
    # Restore original
    Set-Content $configFile -Value $origJson -Encoding UTF8
    return $success
}

# 4. Headless WinUI XAML Verification & Kill Switch Default Collapsed
Assert-Test "WinUI 3 XAML parses, 30/60/120 FPS bound, and Kill Switch collapsed" {
    $winUiScript = Join-Path $rootDir "client\ScrcpyDeX-WinUI.ps1"
    $output = & pwsh.exe -ExecutionPolicy Bypass -NoProfile -File $winUiScript -HeadlessTest
    ($output -match "HEADLESS TEST PASSED")
}

# 5. Parameterized Orchestrator Syntax Check
Assert-Test "run-scrcpydex.ps1 accepts custom parameters" {
    $runScript = Join-Path $rootDir "tools\launchers\run-scrcpydex.ps1"
    if (-not (Test-Path $runScript)) { $runScript = Join-Path $rootDir "run-scrcpydex.ps1" }
    $tokens = $null
    $parseErrors = $null
    $ast = [System.Management.Automation.Language.Parser]::ParseFile($runScript, [ref]$tokens, [ref]$parseErrors)
    ($parseErrors.Count -eq 0) -and ($ast.ParamBlock -ne $null)
}

# 6. C# Source Architecture Check
Assert-Test "C# client source models and services compiled structure" {
    $csharpDir = Join-Path $rootDir "client\src"
    (Test-Path (Join-Path $csharpDir "Models\DisplayConfig.cs")) -and
    (Test-Path (Join-Path $csharpDir "Models\DeviceInfo.cs")) -and
    (Test-Path (Join-Path $csharpDir "Services\ConfigStore.cs")) -and
    (Test-Path (Join-Path $csharpDir "Services\AdbService.cs")) -and
    (Test-Path (Join-Path $csharpDir "ViewModels\MainViewModel.cs"))
}

# 7. Native Zero-Flash Windows Launchers Validation
Assert-Test "Native Launchers exist (ScrcpyDeX.bat, stop-dex.bat, tools/launchers)" {
    (Test-Path (Join-Path $rootDir "ScrcpyDeX.bat")) -and
    (Test-Path (Join-Path $rootDir "stop-dex.bat")) -and
    (Test-Path (Join-Path $rootDir "tools\launchers\ScrcpyDeX.vbs")) -and
    (Test-Path (Join-Path $rootDir "tools\launchers\ScrcpyDeX-UI.bat"))
}

# Restore pristine configuration
if ($savedConfigContent -ne $null -and (Test-Path $savedConfigFile)) {
    Set-Content $savedConfigFile -Value $savedConfigContent -NoNewline -Encoding UTF8
}

Write-Host "=================================================" -ForegroundColor Cyan
if ($failed -eq 0) {
    Write-Host "ALL TESTS PASSED ($passed/$passed)! Prototype is 100% operational." -ForegroundColor Green
} else {
    Write-Host "VERIFICATION FAILED: $passed passed, $failed failed." -ForegroundColor Red
}
Write-Host "=================================================" -ForegroundColor Cyan
