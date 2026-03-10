<#
.SYNOPSIS
    ProxyTool Build Script (packaging only)

.DESCRIPTION
    Restore, build, and publish CLI/API. No tests. Supports self-contained and framework-dependent.
    Outputs to publish/ directory (no archives). Cleans build cache at end.

.PARAMETER fd
    Framework-dependent deployment (requires .NET runtime). Omit for self-contained

.PARAMETER Runtimes
    Runtimes to publish, comma-separated. Default: win-x64,linux-x64,osx-x64

.PARAMETER Projects
    Projects to publish: cli,api,all. Default: all

.EXAMPLE
    .\build.ps1
    .\build.ps1 -fd
    .\build.ps1 -Runtimes "win-x64" -Projects "cli"
#>

param(
    [switch]$fd,
    [string]$Runtimes = "win-x64,linux-x64,osx-x64",
    [string]$Projects = "all",
    [string]$Version = "1.0.0"
)
# Self-contained by default; use -fd for framework-dependent
$SelfContained = -not $fd

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptDir

$SrcDir = Join-Path $ScriptDir "src"
$PublishDir = Join-Path $ScriptDir "publish"
$SlnPath = Join-Path $SrcDir "ProxyTool.sln"
$CliProject = Join-Path $SrcDir "ProxyTool.CLI\ProxyTool.CLI.csproj"
$ApiProject = Join-Path $SrcDir "ProxyTool.API\ProxyTool.API.csproj"

$RuntimeList = $Runtimes -split "," | ForEach-Object { $_.Trim() }
$ProjectList = if ($Projects -eq "all") { @("cli", "api") } else { $Projects -split "," | ForEach-Object { $_.Trim() } }

$DeploySuffix = if ($SelfContained) { "" } else { "-fd" }

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ProxyTool Build Script v$Version" -ForegroundColor Cyan
Write-Host "  Mode: $(if ($SelfContained) { 'Self-contained' } else { 'Framework-dependent' })" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "Error: dotnet CLI not found. Please install .NET SDK." -ForegroundColor Red
    exit 1
}

# 1. Restore
Write-Host "[1/4] Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore $SlnPath
if ($LASTEXITCODE -ne 0) { exit 1 }

# 2. Build
Write-Host "[2/4] Building..." -ForegroundColor Yellow
dotnet build $SlnPath -c Release --no-restore
if ($LASTEXITCODE -ne 0) { exit 1 }

# 3. Publish (direct output, no archive)
Write-Host "[3/4] Publishing..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null

foreach ($rid in $RuntimeList) {
    if ($ProjectList -contains "cli") {
        $outDir = Join-Path $PublishDir "proxy-tool-$rid$DeploySuffix"
        Write-Host "  Publish CLI -> $rid" -ForegroundColor Gray
        $args = @(
            "publish", $CliProject, "-r", $rid, "-c", "Release",
            "--self-contained", $(if ($SelfContained) { "true" } else { "false" }),
            "-o", $outDir,
            "-p:PublishSingleFile=true",
            "-p:DebugType=None", "-p:DebugSymbols=false"
        )
        if ($SelfContained) {
            $args += "-p:IncludeNativeLibrariesForSelfExtract=true"
            $args += "-p:EnableCompressionInSingleFile=true"
        }
        & dotnet $args
        if ($LASTEXITCODE -ne 0) { exit 1 }
        Write-Host "    -> $outDir" -ForegroundColor Green
    }

    if ($ProjectList -contains "api") {
        $outDir = Join-Path $PublishDir "proxy-tool-api-$rid$DeploySuffix"
        Write-Host "  Publish API -> $rid" -ForegroundColor Gray
        & dotnet publish $ApiProject -r $rid -c Release `
            --self-contained $(if ($SelfContained) { "true" } else { "false" }) `
            -o $outDir `
            -p:DebugType=None -p:DebugSymbols=false
        if ($LASTEXITCODE -ne 0) { exit 1 }
        Write-Host "    -> $outDir" -ForegroundColor Green
    }
}

# 4. Clean build cache (obj, bin)
Write-Host "[4/4] Cleaning build cache..." -ForegroundColor Yellow
$objDirs = Get-ChildItem -Path $SrcDir -Directory -Recurse -Filter "obj" -ErrorAction SilentlyContinue
$binDirs = Get-ChildItem -Path $SrcDir -Directory -Recurse -Filter "bin" -ErrorAction SilentlyContinue
foreach ($d in $objDirs + $binDirs) {
    Remove-Item $d.FullName -Recurse -Force -ErrorAction SilentlyContinue
}
# Also clean root obj/bin if any
@("obj", "bin") | ForEach-Object {
    $p = Join-Path $ScriptDir $_
    if (Test-Path $p) { Remove-Item $p -Recurse -Force -ErrorAction SilentlyContinue }
}
Write-Host "  Cache cleaned" -ForegroundColor Green

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Build completed!" -ForegroundColor Green
Write-Host "  Output: $PublishDir" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Get-ChildItem $PublishDir -Directory | Where-Object { $_.Name -like "proxy-tool*" } | ForEach-Object { Write-Host "  $($_.Name)/" }
