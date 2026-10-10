<#
.SYNOPSIS
    Builds BrowserChooserSetup-<version>-<arch>.exe, an installer to give to other PCs.

.DESCRIPTION
    1. Publishes a self-contained Release build: the .NET runtime is bundled, so the PCs it is installed on need
       nothing else.
    2. Packs it with Inno Setup using installer\BrowserChooser.iss.

    The installer is written to artifacts\installer. The version comes from <Version> in Directory.Build.props.

    Requires Inno Setup 6: winget install JRSoftware.InnoSetup

.PARAMETER Runtime
    win-x64 (default) for Intel/AMD PCs, or win-arm64 for ARM PCs.

.EXAMPLE
    .\scripts\Build-Installer.ps1
#>
[CmdletBinding()]
param(
    [ValidateSet('win-x64', 'win-arm64')] [string] $Runtime = 'win-x64'
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

[xml] $buildProps = Get-Content (Join-Path $root 'Directory.Build.props')
$version = @($buildProps.Project.PropertyGroup | ForEach-Object { $_.Version } | Where-Object { $_ })[0]
if (-not $version) { throw 'No <Version> found in Directory.Build.props.' }

$candidates = @()
$command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
if ($command) { $candidates += $command.Source }
$candidates += @(
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
    (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
    (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe'))
$iscc = $candidates | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $iscc) { throw 'Inno Setup 6 was not found. Install it with: winget install JRSoftware.InnoSetup' }

$arch = $Runtime.Substring('win-'.Length)
$archAllowed = if ($arch -eq 'arm64') { 'arm64' } else { 'x64compatible' }
$publishDir = Join-Path $root "artifacts\publish\$Runtime"
$outputDir = Join-Path $root 'artifacts\installer'

Write-Host "Publishing Browser Chooser $version ($Runtime, self-contained) ..."
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
dotnet publish (Join-Path $root 'src\BrowserChooser\BrowserChooser.csproj') `
    --configuration Release --runtime $Runtime --self-contained true `
    -p:PublishReadyToRun=true --output $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE." }

Write-Host 'Building the installer ...'
& $iscc /Qp `
    "/DAppVersion=$version" `
    "/DPublishDir=$publishDir" `
    "/DOutputDir=$outputDir" `
    "/DArch=$arch" `
    "/DArchAllowed=$archAllowed" `
    "/DSourceRoot=$root" `
    (Join-Path $root 'installer\BrowserChooser.iss')
if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed with exit code $LASTEXITCODE." }

$installer = Get-Item (Join-Path $outputDir "BrowserChooserSetup-$version-$arch.exe")
Write-Host ''
Write-Host "Installer: $($installer.FullName) ($([Math]::Round($installer.Length / 1MB, 1)) MB)"
