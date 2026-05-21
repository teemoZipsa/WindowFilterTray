param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputDir = "",
    [switch]$SkipRestore
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = (Resolve-Path (Join-Path $ScriptRoot "..")).Path
$ArtifactsRoot = Join-Path $RepoRoot "artifacts"
$PublishRoot = Join-Path $ArtifactsRoot "publish"

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $PublishRoot $Runtime
}

$FullOutputDir = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDir)
$FullPublishRoot = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($PublishRoot)

if (-not $FullOutputDir.StartsWith($FullPublishRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "OutputDir must be inside artifacts\publish."
}

$LocalDotnet = Join-Path $RepoRoot ".dotnet\dotnet.exe"
$Dotnet = if (Test-Path -LiteralPath $LocalDotnet) { $LocalDotnet } else { "dotnet" }

New-Item -ItemType Directory -Force -Path $PublishRoot | Out-Null
if (Test-Path -LiteralPath $FullOutputDir) {
    Remove-Item -LiteralPath $FullOutputDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $FullOutputDir | Out-Null

$PublishArgs = @(
    "publish",
    (Join-Path $RepoRoot "WindowFilterTray.csproj"),
    "-c", $Configuration,
    "-r", $Runtime,
    "--self-contained", "true",
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=false",
    "-o", $FullOutputDir
)

if ($SkipRestore) {
    $PublishArgs += "--no-restore"
}

& $Dotnet @PublishArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Write-Host "Published to $FullOutputDir"
