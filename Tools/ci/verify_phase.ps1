param(
  [ValidateSet("PreProd","VerticalSlice","Alpha","Beta","RC")]
  [string]$Phase = "PreProd",
  [string]$UnityExe = ""
)

$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..\\..")).Path
Set-Location $root

Write-Host ("Phase verify: {0}" -f $Phase)

# 1) Fast compile gate (catches missing files/includes quickly)
Write-Host "dotnet build (sanity compile gate)"
dotnet build .\\VeilBreakers3DCurrent.sln -v minimal
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }

# 2) Unity EditMode integrity (always)
& (Join-Path $PSScriptRoot "run_unity_tests.ps1") -Platform editmode -Category "Phase.PreProd" -UnityExe $UnityExe

# 3) Phase-specific PlayMode
switch ($Phase) {
  "PreProd" {
    & (Join-Path $PSScriptRoot "run_unity_tests.ps1") -Platform playmode -Category "Phase.PreProd" -UnityExe $UnityExe
  }
  "VerticalSlice" {
    & (Join-Path $PSScriptRoot "run_unity_tests.ps1") -Platform playmode -Category "Phase.PreProd" -UnityExe $UnityExe
    & (Join-Path $PSScriptRoot "run_unity_tests.ps1") -Platform playmode -Category "Phase.VerticalSlice" -UnityExe $UnityExe
  }
  "Alpha" {
    & (Join-Path $PSScriptRoot "run_unity_tests.ps1") -Platform playmode -Category "Phase.PreProd" -UnityExe $UnityExe
    & (Join-Path $PSScriptRoot "run_unity_tests.ps1") -Platform playmode -Category "Phase.VerticalSlice" -UnityExe $UnityExe
    & (Join-Path $PSScriptRoot "run_unity_tests.ps1") -Platform playmode -Category "Phase.Alpha" -UnityExe $UnityExe
  }
  "Beta" {
    & (Join-Path $PSScriptRoot "run_unity_tests.ps1") -Platform playmode -Category "Phase.PreProd" -UnityExe $UnityExe
    & (Join-Path $PSScriptRoot "run_unity_tests.ps1") -Platform playmode -Category "Phase.VerticalSlice" -UnityExe $UnityExe
    & (Join-Path $PSScriptRoot "run_unity_tests.ps1") -Platform playmode -Category "Phase.Alpha" -UnityExe $UnityExe
    & (Join-Path $PSScriptRoot "run_unity_tests.ps1") -Platform playmode -Category "Phase.Beta" -UnityExe $UnityExe
  }
  "RC" {
    & (Join-Path $PSScriptRoot "run_unity_tests.ps1") -Platform playmode -Category "Phase.PreProd" -UnityExe $UnityExe
    & (Join-Path $PSScriptRoot "run_unity_tests.ps1") -Platform playmode -Category "Phase.VerticalSlice" -UnityExe $UnityExe
    & (Join-Path $PSScriptRoot "run_unity_tests.ps1") -Platform playmode -Category "Phase.Alpha" -UnityExe $UnityExe
    & (Join-Path $PSScriptRoot "run_unity_tests.ps1") -Platform playmode -Category "Phase.Beta" -UnityExe $UnityExe
    & (Join-Path $PSScriptRoot "run_unity_tests.ps1") -Platform playmode -Category "Phase.RC" -UnityExe $UnityExe
  }
}

Write-Host "Phase verification complete."

