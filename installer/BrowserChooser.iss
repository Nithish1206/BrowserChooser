; Inno Setup script for Browser Chooser. Build it with scripts\Build-Installer.ps1, which publishes the app and
; passes the defines below; compiling this file on its own needs them supplied with /D.
;
; The installer is per-user: no admin rights, files in %LOCALAPPDATA%\Programs\BrowserChooser, registry entries in
; HKCU. Browser registration is done by the app itself (BrowserChooser.exe --register), so the installer and the
; app's home screen always write exactly the same entries.

#ifndef AppVersion
  #error AppVersion is not defined. Build with scripts\Build-Installer.ps1.
#endif

#define AppName "Browser Chooser"
#define AppExe "BrowserChooser.exe"
#define DefaultAppsUri "ms-settings:defaultapps?registeredAppUser=BrowserChooser"

[Setup]
; Never change AppId: Windows uses it to recognise upgrades and to find the uninstaller.
AppId={{7C1E4B2A-5D93-4F6E-9A1B-3E8D2C6F0A47}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppName}
VersionInfoVersion={#AppVersion}
VersionInfoDescription={#AppName} Setup
PrivilegesRequired=lowest
DefaultDirName={autopf}\BrowserChooser
; A short wizard: Welcome (with the Install button), progress, Finish.
DisableWelcomePage=no
DisableDirPage=yes
DisableProgramGroupPage=yes
DisableReadyPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=BrowserChooserSetup-{#AppVersion}-{#Arch}
SetupIconFile={#SourceRoot}\src\BrowserChooser\Assets\BrowserChooser.ico
; Generated with the app icon by scripts\New-AppIcon.ps1.
WizardImageFile={#SourceRoot}\installer\WizardImage.bmp
WizardSmallImageFile={#SourceRoot}\installer\WizardSmallImage.bmp
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
WizardStyle=modern
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesAllowed={#ArchAllowed}
ArchitecturesInstallIn64BitMode={#ArchAllowed}
; Windows 10 1809 or later, the oldest version .NET 10 supports.
MinVersion=10.0.17763
; If a picker is open during an upgrade, offer to close it instead of failing on locked files.
CloseApplications=yes
RestartApplications=no
SetupLogging=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[InstallDelete]
; The folder only ever holds program files (settings and logs live in %LOCALAPPDATA%\BrowserChooser), so clear
; it first: an upgrade then never mixes files from two versions.
Type: filesandordirs; Name: "{app}\*"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"; Comment: "Choose which browser and profile opens each link"

[Run]
; Windows doesn't let apps or installers make themselves the default browser; only the user can, in Settings.
; So the last page asks, and opens Browser Chooser's own Default apps page, where "Set default" is one click.
Filename: "{#DefaultAppsUri}"; Description: "Make {#AppName} my default browser"; Flags: postinstall shellexec nowait skipifsilent

[UninstallRun]
Filename: "{app}\{#AppExe}"; Parameters: "--unregister"; Flags: waituntilterminated; RunOnceId: "UnregisterBrowser"

[Messages]
WelcomeLabel2=This will install [name/ver] on your computer and register it as a browser, so it can ask which browser and profile should open each link you click.%n%nNo administrator rights are needed.
; Shown under the Welcome text; that page's button installs (see CurPageChanged).
ClickNext=Click Install to continue, or Cancel to exit Setup.
FinishedLabel=Setup has installed [name] and registered it as a browser.%n%nWindows only lets you choose the default browser yourself. Leave the box below checked and click Finish: Settings opens on [name]'s page, where you click Set default.

[Code]
// Every page between Welcome and installing is turned off, so the Welcome page's button starts the install.
procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpWelcome then
    WizardForm.NextButton.Caption := SetupMessage(msgButtonInstall);
end;

// Registers the installed copy as a browser. Runs after the files are copied, so the registry points at them.
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep <> ssPostInstall then
    Exit;

  // Shown normally, not hidden: if registration fails the app explains why in its own dialog.
  if not Exec(ExpandConstant('{app}\{#AppExe}'), '--register --quiet', '', SW_SHOWNORMAL, ewWaitUntilTerminated, ResultCode)
     or (ResultCode <> 0) then
  begin
    // (A line must not start with "#": the preprocessor would read it as a directive.)
    SuppressibleMsgBox(
      '{#AppName} was installed, but it could not register itself as a browser (code ' + IntToStr(ResultCode) + ').' + #13#10#13#10 +
      'Open {#AppName} from the Start menu and choose "Register as a browser".',
      mbError, MB_OK, IDOK);
  end;
end;
