; ====== EDIT ME ======
#define MyAppName        "SigStream Agent"
#define MyAppExeName     "SigstreamTelemetryAgent.exe"      ; <-- confirm this matches your published EXE name
#define MyPublisher      "SigStreamCloud"
#define MyURL            "https://sigstreamcloud.com"
#define PublishDirRel    "..\dist\SigStreamAgent\win-x64"   ; output of dotnet publish in release.ps1
#define BuildExe         AddBackslash(SourcePath) + "{#PublishDirRel}\{#MyAppExeName}"
; =====================

; Resolve version from the published EXE, with a safe fallback
#ifexist "{#BuildExe}"
  #define MyAppVersion  GetVersionNumbersString("{#BuildExe}")
#else
  #pragma message "Build EXE not found at {#BuildExe}; using default version 1.0.0.0"
  #define MyAppVersion  "1.0.0.0"
#endif

[Setup]
; Keep AppId constant forever so upgrades work
AppId={{B8B2E0A1-89C0-4F8E-9D7E-6E3F7E5E1A72}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyPublisher}
AppPublisherURL={#MyURL}
AppSupportURL={#MyURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=.
OutputBaseFilename=SigStreamAgent-Setup
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
Compression=lzma
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\Resources\Icons\sigstream_app_transparent.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
DisableDirPage=no
DisableProgramGroupPage=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
Source: "{#PublishDirRel}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\logs"
