#define MyAppName        "SigStream Agent"
#define MyAppExeName     "SigstreamTelemetryAgent.exe"

#ifndef BuildDir
  #define BuildDir "..\\dist\\SigStreamAgent\\win-x64"
#endif

#define BuildExe         AddBackslash(BuildDir) + MyAppExeName
#expr FileExists(BuildExe)

; Icon is one level up from the installer script: ..\Assets\Icons\SigStream.ico
#define IconPath         AddBackslash(SourcePath) + "..\\Assets\\Icons\\SigStream.ico"
#if FileExists(IconPath)
  ; ok
#else
  #error Icon not found: {#IconPath}
#endif

; Human-facing string (from ProductVersion)
#define MyAppVersion      GetStringFileInfo(BuildExe, "ProductVersion")
; Strict numeric a.b.c.d (from FileVersion)
#define MyAppFileVersion  GetVersionNumbersString(BuildExe)

[Setup]
AppId={{A1F9E2B2-1C3B-4C55-93F0-7B3C6A9D7A12}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
VersionInfoVersion={#MyAppFileVersion}

PrivilegesRequired=lowest
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
UsePreviousAppDir=yes

OutputDir=installer\Output
; === Standardized output filename (no version/timestamp) ===
OutputBaseFilename=SigStreamAgent-Setup
Compression=lzma2/ultra64
SolidCompression=yes

ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

WizardStyle=modern
SetupIconFile={#IconPath}
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; Flags: unchecked

[Files]
Source: "{#BuildDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs
Source: "{#IconPath}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\SigStream Agent"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\SigStream Agent";  Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; WorkingDir: "{app}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent; WorkingDir: "{app}"

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\SigStream"
