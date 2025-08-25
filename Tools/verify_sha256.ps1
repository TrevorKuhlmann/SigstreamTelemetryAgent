Param(
    [Parameter(Mandatory=$true)][string]$FilePath,
    [Parameter(Mandatory=$true)][string]$ExpectedSha256
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $FilePath)) {
    throw "File not found: $FilePath"
}

$hash = (Get-FileHash $FilePath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($hash -eq $ExpectedSha256.ToLowerInvariant()) {
    Write-Host "OK: SHA-256 matches." -ForegroundColor Green
    Exit 0
} else {
    Write-Host "FAIL: Expected $ExpectedSha256 but got $hash" -ForegroundColor Red
    Exit 1
}
