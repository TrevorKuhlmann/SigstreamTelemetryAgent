Param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Csproj = "SigstreamTelemetryAgent.csproj",
    [string]$PublishDir = "dist\SigStreamAgent\win-x64",
    [string]$InnoScript = "installer\SigStreamAgent.iss",
    [string]$OutputInstaller = "installer\SigStreamAgent-Setup.exe",
    [string]$DownloadsDir = "downloads",
    [string]$UpdateXml = "downloads\update.xml",
    [string]$IsccPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
)

$ErrorActionPreference = "Stop"

# 1) Clean publish folder
if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null

# 2) Publish self-contained single file
dotnet publish $Csproj -c $Configuration -r $Runtime -o $PublishDir `
  -p:SelfContained=true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:PublishTrimmed=false `
  -p:PublishReadyToRun=true `
  -p:DebugType=None -p:DebugSymbols=false

# 3) Build installer with Inno Setup
if (-not (Test-Path $IsccPath)) {
    Write-Warning "Inno Setup compiler not found at: $IsccPath"
    Write-Warning "Install Inno Setup 6 and adjust -IsccPath if needed."
} else {
    & "$IsccPath" $InnoScript | Write-Host
}

# 4) Compute SHA-256 of installer
$installer = $OutputInstaller
if (-not (Test-Path $installer)) {
    # fallback: Inno outputs into installer\Output\ by default using OutputBaseFilename
    $defaultOut = Join-Path (Split-Path $InnoScript) "Output\SigStreamAgent-Setup.exe"
    if (Test-Path $defaultOut) { $installer = $defaultOut }
}

if (-not (Test-Path $installer)) {
    throw "Installer not found. Looked for $OutputInstaller and $defaultOut"
}

$hash = (Get-FileHash $installer -Algorithm SHA256).Hash
Write-Host "SHA256: $hash"

# 5) Ensure downloads dir exists
New-Item -ItemType Directory -Force -Path $DownloadsDir | Out-Null

# 6) Update update.xml with new checksum (and optionally version if you pass it)
$xmlPath = $UpdateXml
if (-not (Test-Path $xmlPath)) {
@"
<?xml version="1.0" encoding="UTF-8"?>
<item>
  <version>1.0.0.0</version>
  <url>https://sigstreamcloud.com/downloads/SigStreamAgent-Setup.exe</url>
  <changelog>https://sigstreamcloud.com/downloads/changelog.html</changelog>
  <mandatory>false</mandatory>
  <checksum>sha256:$hash</checksum>
</item>
"@ | Out-File -FilePath $xmlPath -Encoding UTF8
} else {
    (Get-Content $xmlPath -Raw) `
      -replace 'sha256:[A-Fa-f0-9]{64}', "sha256:$hash" `
      | Set-Content $xmlPath -Encoding UTF8
}

Write-Host "`nDone!"
Write-Host "Installer: $installer"
Write-Host "update.xml updated at: $xmlPath"
