<#
.SYNOPSIS
    Build and package the portable Anhei4Map application with bundled WebView2 Fixed Runtime.

.DESCRIPTION
    Creates a self-contained win-x64 publish and bundles the WebView2 Fixed Version Runtime
    so target machines require no pre-installed .NET or WebView2.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$runtimeSource = Join-Path $repoRoot ".runtime-cache\webview2-fixed-x64"
$outputDir = Join-Path $repoRoot "artifacts\Anhei4Map-win-x64-portable"
$zipPath = Join-Path $repoRoot "artifacts\Anhei4Map-win-x64-portable.zip"
$projectPath = Join-Path $repoRoot "src\Anhei4Map.App\Anhei4Map.App.csproj"

# ---------- Validate Runtime ----------
$runtimeExe = Join-Path $runtimeSource "msedgewebview2.exe"
if (-not (Test-Path -LiteralPath $runtimeExe)) {
    throw @"
WebView2 Fixed Runtime not found at:
  $runtimeExe

Expected Runtime source directory:
  $runtimeSource

Place a local Fixed Version Runtime (containing msedgewebview2.exe) at that path before publishing.
This script does not download or install WebView2.
"@
}

Write-Host "[1/5] Runtime validated: $runtimeExe" -ForegroundColor Green

# ---------- Clean previous outputs ----------
Write-Host "[2/5] Cleaning previous publish outputs..." -ForegroundColor Cyan

if (Test-Path -LiteralPath $outputDir) {
    Remove-Item -LiteralPath $outputDir -Recurse -Force
}

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

# ---------- Publish ----------
Write-Host "[3/5] Publishing win-x64 self-contained..." -ForegroundColor Cyan

$artifactsDir = Join-Path $repoRoot "artifacts"
if (-not (Test-Path -LiteralPath $artifactsDir)) {
    New-Item -ItemType Directory -Path $artifactsDir | Out-Null
}

dotnet publish `
    $projectPath `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o $outputDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$appExe = Join-Path $outputDir "Anhei4Map.App.exe"
if (-not (Test-Path -LiteralPath $appExe)) {
    throw "Publish output missing: $appExe"
}

# ---------- Copy Fixed Runtime ----------
Write-Host "[4/5] Copying WebView2 Fixed Runtime..." -ForegroundColor Cyan

$targetRuntimeDir = Join-Path $outputDir "WebView2Runtime"
Copy-Item -LiteralPath $runtimeSource -Destination $targetRuntimeDir -Recurse

$targetExe = Join-Path $targetRuntimeDir "msedgewebview2.exe"
if (-not (Test-Path -LiteralPath $targetExe)) {
    throw "Runtime copy failed: msedgewebview2.exe not found at $targetExe"
}

if (-not (Test-Path -LiteralPath $appExe)) {
    throw "Portable package incomplete: $appExe"
}

# ---------- Create ZIP ----------
Write-Host "[5/5] Creating ZIP..." -ForegroundColor Cyan

Compress-Archive -Path (Join-Path $outputDir "*") -DestinationPath $zipPath -CompressionLevel Optimal

$zip = Get-Item -LiteralPath $zipPath
if ($zip.Length -le 0) {
    throw "ZIP creation failed or produced empty archive: $zipPath"
}

Write-Host ""
Write-Host "=== PUBLISH COMPLETE ===" -ForegroundColor Green
Write-Host "Portable Directory: $outputDir" -ForegroundColor Green
Write-Host "Portable ZIP:       $zipPath" -ForegroundColor Green
Write-Host "ZIP Size:           $([math]::Round($zip.Length / 1MB, 1)) MB" -ForegroundColor Green
Write-Host "Fixed Runtime:      $targetRuntimeDir" -ForegroundColor Green
