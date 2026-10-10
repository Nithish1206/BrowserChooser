<#
.SYNOPSIS
    Installs Browser Chooser for the current user and registers it as a browser.

.DESCRIPTION
    Publishes a Release build to a stable folder, registers it with Windows (HKCU, no admin rights needed) and
    opens Settings > Default apps, where you make Browser Chooser the default for HTTP and HTTPS.

    Windows stores the path of the registered executable, so always register an installed copy rather than a
    bin\Debug build that gets rebuilt or deleted.

.PARAMETER Uninstall
    Removes the registration and the installed files. Logs and per-user settings in
    %LOCALAPPDATA%\BrowserChooser are kept.

.PARAMETER InstallDirectory
    Where to install. Defaults to %LOCALAPPDATA%\Programs\BrowserChooser.

.EXAMPLE
    .\scripts\Install.ps1

.EXAMPLE
    .\scripts\Install.ps1 -Uninstall
#>
[CmdletBinding()]
param(
    [switch] $Uninstall,
    [string] $InstallDirectory = (Join-Path $env:LOCALAPPDATA 'Programs\BrowserChooser')
)

$ErrorActionPreference = 'Stop'
$exe = Join-Path $InstallDirectory 'BrowserChooser.exe'
$logs = Join-Path $env:LOCALAPPDATA 'BrowserChooser\Logs'
# Start-menu entry: opens the home screen (register, "Ask every time", logs).
$shortcut = Join-Path ([Environment]::GetFolderPath('Programs')) 'Browser Chooser.lnk'

function Invoke-BrowserChooser([string] $Argument) {
    # BrowserChooser.exe is a GUI app, so wait for it explicitly to get its exit code.
    $process = Start-Process -FilePath $exe -ArgumentList $Argument -PassThru
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) {
        throw "BrowserChooser.exe $Argument failed with exit code $($process.ExitCode). See the logs in $logs."
    }
}

if ($Uninstall) {
    if (Test-Path $exe) { Invoke-BrowserChooser '--unregister' }
    if (Test-Path $shortcut) { Remove-Item $shortcut -Force }
    if (Test-Path $InstallDirectory) { Remove-Item $InstallDirectory -Recurse -Force }
    Write-Host "Browser Chooser has been uninstalled. Logs and settings in $env:LOCALAPPDATA\BrowserChooser were kept."
    return
}

$runtime = if ($env:PROCESSOR_ARCHITECTURE -eq 'ARM64') { 'win-arm64' } else { 'win-x64' }
$project = Join-Path $PSScriptRoot '..\src\BrowserChooser\BrowserChooser.csproj'

Write-Host "Publishing Browser Chooser ($runtime) to $InstallDirectory ..."
# ReadyToRun pre-compiles the app: it starts on every link click, so start-up time matters.
dotnet publish $project --configuration Release --runtime $runtime --self-contained false `
    -p:PublishReadyToRun=true --output $InstallDirectory
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE." }

$shell = New-Object -ComObject WScript.Shell
$link = $shell.CreateShortcut($shortcut)
$link.TargetPath = $exe
$link.WorkingDirectory = $InstallDirectory
$link.Description = 'Choose which browser and profile opens each link'
$link.Save()

Invoke-BrowserChooser '--register'
Write-Host ''
Write-Host 'Installed and registered.'
Write-Host 'In the Settings window that opened, set Browser Chooser as the default for HTTP and HTTPS.'
