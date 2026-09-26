# Script de Compilação do ScrcpyDeX Server (Etapa 1)
# Utiliza o JDK do Android Studio e o compilador D8 do Android SDK 35

param(
    [string]$JdkPath = "C:\Program Files\Android\Android Studio\jbr",
    [string]$AndroidSdkPath = "C:\Users\aurel\AppData\Local\Android\Sdk",
    [string]$BuildToolsVersion = "35.0.0",
    [string]$PlatformVersion = "android-35"
)

$ErrorActionPreference = "Stop"

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "       Compilando ScrcpyDeX Server (JAR)         " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# 1. Validar JDK
$javacExe = Join-Path $JdkPath "bin\javac.exe"
if (-not (Test-Path $javacExe)) {
    throw "javac.exe nao encontrado em $javacExe"
}
$env:JAVA_HOME = $JdkPath
$env:PATH = "$JdkPath\bin;$env:PATH"

# 2. Validar Android SDK
$androidJar = Join-Path $AndroidSdkPath "platforms\$PlatformVersion\android.jar"
if (-not (Test-Path $androidJar)) {
    throw "android.jar nao encontrado em $androidJar"
}

$d8Bat = Join-Path $AndroidSdkPath "build-tools\$BuildToolsVersion\d8.bat"
if (-not (Test-Path $d8Bat)) {
    throw "d8.bat nao encontrado em $d8Bat"
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$srcDir = Join-Path $scriptDir "src"
$outClassesDir = Join-Path $scriptDir "build\classes"
$outJar = Join-Path $scriptDir "scrcpydex-server.jar"

# 3. Limpar build anterior
if (Test-Path $outClassesDir) {
    Remove-Item $outClassesDir -Recurse -Force
}
New-Item -ItemType Directory -Path $outClassesDir -Force | Out-Null

# 4. Coletar fontes Java
$javaFiles = Get-ChildItem -Path $srcDir -Recurse -Filter "*.java" | Select-Object -ExpandProperty FullName
Write-Host "[1/3] Compilando $($javaFiles.Count) fontes Java com javac (Java 17 / 21)..." -ForegroundColor Green

& $javacExe -encoding UTF-8 -cp $androidJar -d $outClassesDir $javaFiles
if ($LASTEXITCODE -ne 0) {
    throw "Falha na compilacao com javac (Exit code: $LASTEXITCODE)"
}

# 5. Converter bytecode .class para DEX no formato JAR via d8
Write-Host "[2/3] Gerando DEX otimizado com D8..." -ForegroundColor Green
$classFiles = Get-ChildItem -Path $outClassesDir -Recurse -Filter "*.class" | Select-Object -ExpandProperty FullName

if (Test-Path $outJar) {
    Remove-Item $outJar -Force
}

& $d8Bat --release --min-api 30 --output $outJar $classFiles
if ($LASTEXITCODE -ne 0) {
    throw "Falha no empacotamento com d8 (Exit code: $LASTEXITCODE)"
}

Write-Host "[3/3] Compilacao concluida com sucesso!" -ForegroundColor Green
$jarItem = Get-Item $outJar
Write-Host "JAR Gerado: $($jarItem.FullName) ($($jarItem.Length) bytes)" -ForegroundColor Cyan
