param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SkipPublish
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = (Resolve-Path (Join-Path $ScriptRoot "..")).Path
$ProjectPath = Join-Path $RepoRoot "WindowFilterTray.csproj"
$PublishDir = Join-Path $RepoRoot "artifacts\publish\$Runtime"
$ReleaseDir = Join-Path $RepoRoot "artifacts\release"

[xml]$ProjectXml = Get-Content -LiteralPath $ProjectPath
$VersionNode = $ProjectXml.Project.PropertyGroup | ForEach-Object { $_.Version } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($VersionNode)) {
    throw "WindowFilterTray.csproj must define a Version property."
}

if (-not $SkipPublish) {
    & (Join-Path $ScriptRoot "Publish.ps1") -Configuration $Configuration -Runtime $Runtime -OutputDir $PublishDir
}

if (-not (Test-Path -LiteralPath $PublishDir)) {
    throw "Publish directory does not exist: $PublishDir"
}

$PublishedItems = Get-ChildItem -LiteralPath $PublishDir -Force
if (-not $PublishedItems) {
    throw "Publish directory is empty: $PublishDir"
}

New-Item -ItemType Directory -Force -Path $ReleaseDir | Out-Null
$ZipPath = Join-Path $ReleaseDir "WindowFilterTray-$Runtime-v$VersionNode.zip"
if (Test-Path -LiteralPath $ZipPath) {
    Remove-Item -LiteralPath $ZipPath -Force
}

$PublishedItems | Compress-Archive -DestinationPath $ZipPath -CompressionLevel Optimal

Write-Host "Packaged $ZipPath"
