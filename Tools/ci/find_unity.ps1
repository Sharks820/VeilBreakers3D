param(
  [string]$ProjectVersion = "2022.3.62f3"
)

$ErrorActionPreference = "Stop"

if ($env:UNITY_EXE -and (Test-Path $env:UNITY_EXE)) {
  Write-Output $env:UNITY_EXE
  exit 0
}

$candidates = @(
  "C:\\Program Files\\Unity\\Hub\\Editor\\$ProjectVersion\\Editor\\Unity.exe",
  "C:\\Program Files\\Unity\\Hub\\Editor\\$ProjectVersion\\Editor\\Unity.exe",
  "C:\\Program Files\\Unity\\Hub\\Editor\\$ProjectVersion\\Editor\\Unity.exe"
)

foreach ($p in $candidates) {
  if (Test-Path $p) {
    Write-Output $p
    exit 0
  }
}

throw "Unity.exe not found. Set UNITY_EXE env var to the full path (e.g. C:\\Program Files\\Unity\\Hub\\Editor\\$ProjectVersion\\Editor\\Unity.exe)."

