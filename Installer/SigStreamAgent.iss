#define MyAppName        "SigStream Agent"
#define MyAppExeName     "SigstreamTelemetryAgent.exe"

#ifndef BuildDir
  #define BuildDir "..\dist\SigStreamAgent\win-x64"
#endif

#define BuildExe AddBackslash(BuildDir) + MyAppExeName
#expr FileExists(BuildExe)
#define MyAppVersion GetStringFileInfo(BuildExe, "ProductVersion")

[Setup]
AppId={{A1F9E2B2-1C3B-4C55-93F0-7B3C6A9D7A12}}   ; <-- fixed braces
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=SigStream
AppPublisherURL=https://sigstreamcloud.com
AppSupportURL=https://sigstreamcloud.com
AppUpdatesURL=https://sigstreamcloud.com

PrivilegesRequired=lowest
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
UsePreviousAppDir=yes

; Put output under installer\Output
OutputDir=installer\Output
OutputBaseFilename=SigStreamAgent-Setup
Compression=lzma2/ultra64
SolidCompression=yes

ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

WizardStyle=modern
; Optional: make the installer EXE show your app icon
SetupIconFile=SigStream.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

#ifexist "{#SourcePath}\wizard.bmp"
WizardImageFile={#SourcePath}\wizard.bmp
#endif
#ifexist "{#SourcePath}\wizard-small.bmp"
WizardSmallImageFile={#SourcePath}\wizard-small.bmp
#endif

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; Flags: unchecked

[Files]
; Copy the entire publish folder contents
Source: "{#BuildDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion
; Also include the ICO used by the app (if you reference it at runtime or for SetupIconFile)
Source: "SigStream.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Start Menu
Name: "{autoprograms}\SigStream Agent"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
; Desktop (only if task checked)
Name: "{autodesktop}\SigStream Agent"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; WorkingDir: "{app}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent; WorkingDir: "{app}"

[UninstallDelete]
; Remove app data your agent created (adjust if your folder is different)
Type: filesandordirs; Name: "{userappdata}\SigStream"
