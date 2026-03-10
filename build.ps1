<#
.SYNOPSIS
    Gate Build Script（仅打包 CLI，Core 作为依赖随 CLI 发布）

.DESCRIPTION
    还原、构建并发布 Gate.CLI（多 RID）。不包含 Tests。支持自包含与框架依赖。
    输出到 publish/ 目录（按运行时分子目录，不生成压缩包）。结束时清理 obj/bin。

.PARAMETER fd
    框架依赖发布（需目标机安装 .NET 运行时）。省略则为自包含

.PARAMETER Runtimes
    要发布的运行时，逗号分隔。默认：win-x64,linux-x64,osx-x64

.EXAMPLE
    # 1）默认：自包含，多平台（win/linux/osx），输出到 publish/gate-*/ 目录
    .\build.ps1

    # 2）框架依赖发布（体积更小，需要目标机器已安装 .NET 运行时）
    .\build.ps1 -fd
    .\build.ps1 -Runtimes "win-x64" -fd

    # 3）只发布 Windows x64，自包含
    .\build.ps1 -Runtimes "win-x64"

    # 4）同时发布 Windows/Linux，自包含
    .\build.ps1 -Runtimes "win-x64,linux-x64"

    # 5）发布时在标题中展示自定义版本号（不影响程序集 Version，仅为脚本显示）
    .\build.ps1 -Version "1.2.3"
#>

param(
    [switch]$fd,
    [string]$Runtimes = "win-x64,linux-x64,osx-x64",
    [string]$Version = "1.0.0",
    [string]$UnityPkgPath = "D:\UniToolGUI\UnityPackage\Gate"
)
    [switch]$fd,
    [string]$Runtimes = "win-x64,linux-x64,osx-x64",
    [string]$Version = "1.0.0",
    [string]$UnityPkgPath = "D:\UniToolGUI\UnityPackage\Gate"
    [switch]$fd,
    [string]$Runtimes = "win-x64,linux-x64,osx-x64",
    [string]$Version = "1.0.0"
)

$SelfContained = -not $fd

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptDir

$SrcDir = Join-Path $ScriptDir "src"
$PublishDir = Join-Path $ScriptDir "publish"
$SlnPath = Join-Path $SrcDir "Gate.sln"
$CliProject = Join-Path $SrcDir "Gate.CLI\Gate.CLI.csproj"

$RuntimeList = $Runtimes -split "," | ForEach-Object { $_.Trim() }
$DeploySuffix = if ($SelfContained) { "" } else { "-fd" }

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Gate Build Script v$Version" -ForegroundColor Cyan
Write-Host "  Target: CLI only (Core as library)" -ForegroundColor Cyan
Write-Host "  Mode: $(if ($SelfContained) { 'Self-contained' } else { 'Framework-dependent' })" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "Error: dotnet CLI not found. Please install .NET SDK." -ForegroundColor Red
    exit 1
}

# 0. Clean publish directory for fresh output
if (Test-Path $PublishDir) {
    Write-Host "Cleaning existing publish directory: $PublishDir" -ForegroundColor Yellow
    Remove-Item $PublishDir -Recurse -Force -ErrorAction SilentlyContinue
}

# 1. Restore
Write-Host "[1/4] Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore $SlnPath -clp:ErrorsOnly
if ($LASTEXITCODE -ne 0) { exit 1 }

# 2. Build Core and CLI
Write-Host "[2/4] Building Core and CLI..." -ForegroundColor Yellow
dotnet build $SlnPath -c Release --no-restore -clp:ErrorsOnly
if ($LASTEXITCODE -ne 0) { exit 1 }

# 2.5 Copy Core DLL to Unity
Write-Host "[2.5/4] Copying Core DLL to Unity..." -ForegroundColor Yellow
$CoreProject = Join-Path $SrcDir "Gate.Core\Gate.Core.csproj"
$CoreBinPath = Join-Path (Split-Path $CoreProject) "bin\Release\netstandard2.0\Gate.Core.dll"
if (Test-Path $CoreBinPath) {
    if (-not (Test-Path $UnityPkgPath)) {
        New-Item -ItemType Directory -Force -Path $UnityPkgPath | Out-Null
    }
    Copy-Item $CoreBinPath -Destination (Join-Path $UnityPkgPath "Gate.Core.dll") -Force
    Write-Host "  Copied Gate.Core.dll to: $UnityPkgPath" -ForegroundColor Green
} else {
    Write-Host "  Warning: Gate.Core.dll not found at $CoreBinPath" -ForegroundColor Yellow
}


Write-Host "[2/4] Building..." -ForegroundColor Yellow
dotnet build $SlnPath -c Release --no-restore -clp:ErrorsOnly
if ($LASTEXITCODE -ne 0) { exit 1 }
# 3. Publish CLI per RID
# 3. Publish CLI per RID
Write-Host "[3/4] Publishing CLI..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null

foreach ($rid in $RuntimeList) {
    $outDir = Join-Path $PublishDir "gate-$rid$DeploySuffix"
    Write-Host "  Publish CLI -> $rid" -ForegroundColor Gray
    $args = @(
        "publish", $CliProject, "-r", $rid, "-c", "Release", "-clp:ErrorsOnly",
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

# 4. Clean build cache (obj, bin)
Write-Host "[4/4] Cleaning build cache..." -ForegroundColor Yellow
$objDirs = Get-ChildItem -Path $SrcDir -Directory -Recurse -Filter "obj" -ErrorAction SilentlyContinue
$binDirs = Get-ChildItem -Path $SrcDir -Directory -Recurse -Filter "bin" -ErrorAction SilentlyContinue
foreach ($d in $objDirs + $binDirs) {
    Remove-Item $d.FullName -Recurse -Force -ErrorAction SilentlyContinue
}
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
foreach ($rid in $RuntimeList) {
    if ([string]::IsNullOrWhiteSpace($rid)) { continue }
    Write-Host "  gate-$rid$DeploySuffix/"
}
