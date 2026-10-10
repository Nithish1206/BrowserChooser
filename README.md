# Browser Chooser

A small Windows 11 app that asks **which browser and profile** should open each link you click. It shows a
picker styled like the Windows "Select an app to open this file" dialog, with one row per browser profile
("Microsoft Edge · Work", "Google Chrome · Personal", …).

Select a row and press **Open** (or double-click it, or press Enter). **Close**, Esc or clicking anywhere
outside the picker dismisses it without opening anything; switching away with Alt+Tab leaves it open. Your previous choice is listed first under **Last used** and preselected, so
opening a link the same way as last time is one click or one Enter.

Browsers and profiles are found again on every click, so newly installed browsers and new profiles appear
without any configuration.

Browser Chooser handles links opened from **other apps**: Outlook, Teams, Slack, VS Code, terminals, PDF
readers. Links clicked inside a browser stay in that browser, because browsers open their own links without
asking Windows.

## Requirements

- Windows 11, or Windows 10 version 1809 or later (square corners there)
- To build: .NET 10 SDK, and [Inno Setup 6](https://jrsoftware.org/isinfo.php) for the installer
  (`winget install JRSoftware.InnoSetup`)
- To run: nothing. The installer bundles the .NET runtime.

## Build the installer (to give to other PCs)

```powershell
.\scripts\Build-Installer.ps1                    # artifacts\installer\BrowserChooserSetup-1.0.0-x64.exe
.\scripts\Build-Installer.ps1 -Runtime win-arm64 # for ARM PCs
```

The version comes from `<Version>` in `Directory.Build.props`. Running the setup:

1. **Welcome → Install.** It installs for the current user only, into `%LOCALAPPDATA%\Programs\BrowserChooser`,
   and needs no admin rights.
2. It adds a Start-menu entry and registers Browser Chooser as a browser (`BrowserChooser.exe --register --quiet`).
3. **Finish**, with "Make Browser Chooser my default browser" checked. That opens Settings on Browser Chooser's
   page, where you click **Set default**. Windows doesn't let apps or installers set the default browser
   themselves, so this one click is the user's.

Browser Chooser then appears in **Settings › Apps › Installed apps**. Uninstalling there removes the
registration too; settings and logs in `%LOCALAPPDATA%\BrowserChooser` are kept. Running a newer setup upgrades
in place.

- **Silent install:** `BrowserChooserSetup-1.0.0-x64.exe /VERYSILENT /SUPPRESSMSGBOXES`. This registers the app
  but doesn't open Settings.
- **SmartScreen warning:** the setup isn't code-signed, so Windows shows "Windows protected your PC" on other
  PCs. Choose **More info › Run anyway**. Signing it with a code-signing certificate removes the warning.

## Install from source (development)

```powershell
.\scripts\Install.ps1
```

This publishes a framework-dependent Release build to the same folder, adds the Start-menu entry, registers it
and opens Default apps settings. It needs the .NET 10 Desktop Runtime. Uninstall with
`.\scripts\Install.ps1 -Uninstall`. If scripts are blocked, run
`powershell -ExecutionPolicy Bypass -File .\scripts\Install.ps1`.

Register an installed copy rather than a `bin\Debug` build: Windows stores the path of the registered exe.

## Build, test and run from source

```powershell
dotnet build
dotnet test
.\src\BrowserChooser\bin\Debug\net10.0-windows\BrowserChooser.exe "https://example.com"
```

| Command line | What it does |
| --- | --- |
| `BrowserChooser.exe "https://…"` | Shows the picker. This is what Windows runs for a clicked link. |
| `BrowserChooser.exe` | Home screen: register, unregister, Default apps settings, log folder. |
| `BrowserChooser.exe --register` | Registers as a browser and opens Default apps settings. Add `--quiet` to skip opening Settings. |
| `BrowserChooser.exe --unregister` | Removes the registration. |

Exit codes: `0` success, `1` cancelled, `2` not an http(s) link, `3` no browsers found, `4` launch failed,
`5` registration failed, `99` unexpected error.

## How it works

1. Windows starts `BrowserChooser.exe "<link>"` because Browser Chooser is the default for http/https.
2. **Discovery** lists browsers registered under `SOFTWARE\Clients\StartMenuInternet` (HKCU and both
   HKLM views), the same list Default apps uses, plus any `CustomBrowsers` from settings. It removes
   duplicates, excluded browsers and Browser Chooser itself.
3. **Profiles**: Chromium browsers (Edge, Chrome, Brave, Vivaldi…) from `User Data\Local State`,
   in the order of the browser's own profile menu; Firefox from `%APPDATA%\Mozilla\Firefox\profiles.ini`.
   Rows use each profile's own icon (`Edge Profile.ico` and similar) when the browser keeps one.
4. **Launch** starts the browser executable directly (`--profile-directory="Profile 1"` for Chromium,
   `-P <name>` for Firefox). It never hands the link back to the shell, which would only start Browser
   Chooser again.

## Project structure

```
BrowserChooser.slnx
Directory.Build.props          shared settings: net10.0-windows, nullable, warnings as errors
Directory.Packages.props       central NuGet versions
installer/BrowserChooser.iss   Inno Setup script for the setup.exe (+ wizard images)
scripts/Build-Installer.ps1    self-contained publish + Inno Setup → artifacts/installer/*.exe
scripts/Install.ps1            development install from source; -Uninstall
scripts/New-AppIcon.ps1        draws the app icon (Assets/BrowserChooser.ico) and the wizard images
src/
  BrowserChooser.Core/         everything except UI; no WinForms dependency
    Abstractions/              interfaces the app depends on (IBrowserCatalog, IBrowserPicker, …)
    Commands/                  use cases: command-line parsing, OpenLinkHandler, RegistrationHandler
    Configuration/             options bound from appsettings.json
    Discovery/                 registry and settings sources, family detection, BrowserCatalog
    Profiles/                  Chromium Local State and Firefox profiles.ini readers
    Launching/                 command-line arguments per browser family, process start
    Preferences/               the "Last used" choice, saved as JSON
    Registration/              HKCU browser registration
    Exceptions/                BrowserChooserException and its subtypes
  BrowserChooser/              WinForms app
    Program.cs                 entry point
    Assets/BrowserChooser.ico  app icon, 16-256 px (exe icon and window icon)
    Hosting/                   composition root (AppHost), command dispatch
    Logging/                   Serilog file logging
    ExceptionHandling/         global unhandled-exception handler
    UI/Chooser/                the picker window, list, buttons, theme, icons
    UI/                        home screen and error/notice dialogs (TaskDialog)
tests/BrowserChooser.Core.Tests/   xUnit tests for Core
```

Core defines the interfaces and the app implements the UI-facing ones (`IBrowserPicker`,
`IUserNotifier`), so the open-link flow, including retries and "Last used", is unit-tested without
any windows. Dependencies are wired in `Hosting/AppHost.cs` with `Microsoft.Extensions.DependencyInjection`.
The .NET Generic Host is deliberately not used: the app starts on every click, so start-up time matters.

## Logging

Logs go to `%LOCALAPPDATA%\BrowserChooser\Logs\browserchooser-YYYYMMDD.log`: one file per day, 14 days
kept, shared safely when several links open at once. Only the scheme and host of a link are logged
(`https://github.com`). Paths and query strings often contain tokens, so they are never written.

## Error handling

- **Expected failures** are typed `BrowserChooserException`s (`BrowserLaunchException`,
  `ProfileReadException`, `AppRegistrationException`), handled where a sensible fallback exists:
  - A browser that fails to start offers **Choose another browser**, so the link isn't lost.
  - Unreadable profile data still lists the browser, opening its last-used profile.
  - A broken registry entry is skipped with a warning.
  - A broken or missing settings or preferences file falls back to defaults with a warning.
  - An unwritable log folder disables file logging but never blocks a link.
- **Unexpected exceptions** (bugs) reach `GlobalExceptionHandler` from the UI thread, other threads or
  tasks. They are logged as critical and shown once as an error dialog with a link to the logs.
- Only specific exception types are caught below the top level.

## Settings

`appsettings.json` ships next to the exe. To keep your changes across reinstalls, put overrides in
`%LOCALAPPDATA%\BrowserChooser\appsettings.json`:

```jsonc
{
  "Logging": { "MinimumLevel": "Debug" },
  "Appearance": { "Theme": "Light" },                   // System (follows Windows), Light or Dark
  "BrowserChooser": {
    "ExcludedBrowsers": [ "iexplore.exe", "Opera" ],   // display name, exe name or registry key
    "CustomBrowsers": [
      { "Name": "Chromium (portable)", "ExecutablePath": "D:\\Tools\\Chromium\\chrome.exe", "Family": "Chromium" }
    ]
  }
}
```

The last-used choice is kept in `%LOCALAPPDATA%\BrowserChooser\preferences.json`; delete the file to reset it.

## Limitations

- **The picker is Browser Chooser's own window, styled after the Windows one.** The system "Select an app"
  dialog can only list apps, not browser profiles.
- **Firefox profiles were not tested on a real install.** Firefox wasn't installed on the development machine,
  so that path is covered by unit tests only.
- **Settings may open on the general page.** After registering, Settings is asked to open directly on
  Browser Chooser's Default apps page. Windows builds that don't support that open the general Default
  apps page instead; find Browser Chooser there.
- **The home screen and error dialogs are always light.** They are standard Windows task dialogs, which
  don't support dark mode; only the browser picker follows the theme.
- **Some links bypass Browser Chooser.** Windows still opens `microsoft-edge:` links (Widgets, some Start
  search results) directly in Edge.
- **A browser installer can take over links.** If a new browser asks to become your default and you say yes,
  re-select Browser Chooser in Default apps.
