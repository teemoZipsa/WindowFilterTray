param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SkipPublish
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Get-ProjectVersion {
    param([string]$ProjectPath)

    [xml]$projectXml = Get-Content -LiteralPath $ProjectPath
    $version = $projectXml.Project.PropertyGroup |
        ForEach-Object { $_.Version } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Select-Object -First 1

    if ([string]::IsNullOrWhiteSpace($version)) {
        throw "WindowFilterTray.csproj must define a Version property."
    }

    return [string]$version
}

function Convert-ToMsiProductVersion {
    param([string]$Version)

    $match = [regex]::Match($Version, '^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?:\.(?<revision>\d+))?(?:[-+].*)?$')
    if (-not $match.Success) {
        throw "Version '$Version' must start with major.minor.patch."
    }

    if ($match.Groups["revision"].Success -or $Version.Contains("-") -or $Version.Contains("+")) {
        Write-Warning "MSI ProductVersion uses only major.minor.patch; '$Version' will be packaged as '$($match.Groups["major"].Value).$($match.Groups["minor"].Value).$($match.Groups["patch"].Value)'."
    }

    return "$($match.Groups["major"].Value).$($match.Groups["minor"].Value).$($match.Groups["patch"].Value)"
}

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = (Resolve-Path (Join-Path $ScriptRoot "..")).Path
$ProjectPath = Join-Path $RepoRoot "WindowFilterTray.csproj"
$InstallerProject = Join-Path $RepoRoot "installer\Installer.wixproj"
$PublishDir = Join-Path $RepoRoot "artifacts\publish\$Runtime"
$InstallerOutDir = Join-Path $RepoRoot "artifacts\installer\$Configuration"
$ReleaseDir = Join-Path $RepoRoot "artifacts\release"

$LocalDotnet = Join-Path $RepoRoot ".dotnet\dotnet.exe"
$Dotnet = if (Test-Path -LiteralPath $LocalDotnet) { $LocalDotnet } else { "dotnet" }

$Version = Get-ProjectVersion -ProjectPath $ProjectPath
$MsiProductVersion = Convert-ToMsiProductVersion -Version $Version

if (-not $SkipPublish) {
    & (Join-Path $ScriptRoot "Publish.ps1") -Configuration $Configuration -Runtime $Runtime -OutputDir $PublishDir
}

if (-not (Test-Path -LiteralPath $PublishDir)) {
    throw "Publish directory does not exist: $PublishDir"
}

if (-not (Test-Path -LiteralPath (Join-Path $PublishDir "불쑥창닫개.exe"))) {
    throw "Publish directory does not contain 불쑥창닫개.exe: $PublishDir"
}

if (Test-Path -LiteralPath $InstallerOutDir) {
    Remove-Item -LiteralPath $InstallerOutDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $ReleaseDir | Out-Null

$BuildArgs = @(
    "build",
    $InstallerProject,
    "-c", $Configuration,
    "-p:ProductVersion=$MsiProductVersion",
    "-p:PublishDir=$PublishDir"
)

& $Dotnet @BuildArgs
if ($LASTEXITCODE -ne 0) {
    throw "WiX MSI build failed with exit code $LASTEXITCODE."
}

$BuiltMsi = Get-ChildItem -LiteralPath $InstallerOutDir -Filter "*.msi" -File |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($null -eq $BuiltMsi) {
    throw "MSI was not created in $InstallerOutDir."
}

$MsiPath = Join-Path $ReleaseDir "WindowFilterTray-$Runtime-v$Version.msi"
if (Test-Path -LiteralPath $MsiPath) {
    Remove-Item -LiteralPath $MsiPath -Force
}

Copy-Item -LiteralPath $BuiltMsi.FullName -Destination $MsiPath
Write-Host "Packaged $MsiPath"
