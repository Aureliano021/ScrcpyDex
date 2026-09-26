# ScrcpyDeX Server Build Script (Stage 1)
# Uses Android Studio JDK and D8 compiler from Android SDK 35

param(
    [string]$JdkPath = "C:\Program Files\Android\Android Studio\jbr",
    [string]$AndroidSdkPath = "C:\Users\aurel\AppData\Local\Android\Sdk",
    [string]$BuildToolsVersion = "35.0.0",
    [string]$PlatformVersion = "android-35"
)

$ErrorActionPreference = "Stop"

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "        Building ScrcpyDeX Server (JAR)          " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# 1. Validate JDK
$javacExe = Join-Path $JdkPath "bin\javac.exe"
if (-not (Test-Path $javacExe)) {
    throw "javac.exe not found at $javacExe"
}
$env:JAVA_HOME = $JdkPath
$env:PATH = "$JdkPath\bin;$env:PATH"

# 2. Validate Android SDK
$androidJar = Join-Path $AndroidSdkPath "platforms\$PlatformVersion\android.jar"
if (-not (Test-Path $androidJar)) {
    throw "android.jar not found at $androidJar"
}

$d8Bat = Join-Path $AndroidSdkPath "build-tools\$BuildToolsVersion\d8.bat"
if (-not (Test-Path $d8Bat)) {
    throw "d8.bat not found at $d8Bat"
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$srcDir = Join-Path $scriptDir "src"
$outClassesDir = Join-Path $scriptDir "build\classes"
$outJar = Join-Path $scriptDir "scrcpydex-server.jar"

# 3. Clean previous build
if (Test-Path $outClassesDir) {
    Remove-Item $outClassesDir -Recurse -Force
}
New-Item -ItemType Directory -Path $outClassesDir -Force | Out-Null

# 4. Collect Java source files
$javaFiles = Get-ChildItem -Path $srcDir -Recurse -Filter "*.java" | Select-Object -ExpandProperty FullName
Write-Host "[1/3] Compiling $($javaFiles.Count) Java source files with javac (Java 17 / 21)..." -ForegroundColor Green

& $javacExe -encoding UTF-8 -cp $androidJar -d $outClassesDir $javaFiles
if ($LASTEXITCODE -ne 0) {
    throw "javac compilation failed (Exit code: $LASTEXITCODE)"
}

# 5. Convert .class bytecode to DEX inside JAR archive via d8
Write-Host "[2/3] Generating optimized DEX with D8..." -ForegroundColor Green
$classFiles = Get-ChildItem -Path $outClassesDir -Recurse -Filter "*.class" | Select-Object -ExpandProperty FullName

if (Test-Path $outJar) {
    Remove-Item $outJar -Force
}

& $d8Bat --release --min-api 30 --output $outJar $classFiles
if ($LASTEXITCODE -ne 0) {
    throw "d8 packaging failed (Exit code: $LASTEXITCODE)"
}

Write-Host "[3/3] Compilation completed successfully!" -ForegroundColor Green
$jarItem = Get-Item $outJar
Write-Host "Generated JAR: $($jarItem.FullName) ($($jarItem.Length) bytes)" -ForegroundColor Cyan
