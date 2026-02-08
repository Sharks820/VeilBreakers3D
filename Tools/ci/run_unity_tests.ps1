param(
  [ValidateSet("editmode","playmode")]
  [string]$Platform,
  [string]$Category = "",
  [string]$UnityExe = "",
  [string]$ProjectPath = ""
)

$ErrorActionPreference = "Stop"

if (-not $ProjectPath) {
  $ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..\\..")).Path
}

$projectVersion = (Get-Content (Join-Path $ProjectPath "ProjectSettings\\ProjectVersion.txt") | Select-String "m_EditorVersion:" | ForEach-Object { $_.ToString().Split(":")[1].Trim() })

if (-not $UnityExe) {
  $UnityExe = & (Join-Path $PSScriptRoot "find_unity.ps1") -ProjectVersion $projectVersion
}

$resultsDir = Join-Path $ProjectPath "TestResults"
New-Item -ItemType Directory -Force $resultsDir | Out-Null

$logsDir = Join-Path $ProjectPath "Logs"
New-Item -ItemType Directory -Force $logsDir | Out-Null

$resultFile = Join-Path $resultsDir ("{0}_{1}.xml" -f $Platform, (Get-Date -Format "yyyyMMdd_HHmmss"))
$logFile = Join-Path $logsDir ("unity_tests_{0}_{1}.log" -f $Platform, (Get-Date -Format "yyyyMMdd_HHmmss"))

$args = @(
  "-batchmode",
  "-nographics",
  "-projectPath", $ProjectPath,
  "-runTests",
  "-testPlatform", $Platform,
  "-testResults", $resultFile,
  "-logFile", $logFile
)

if ($Category) {
  $args += @("-testCategory", $Category)
}

Write-Host ("Running Unity tests: {0} (Category='{1}')" -f $Platform, $Category)
Write-Host ("Unity: {0}" -f $UnityExe)
Write-Host ("Results: {0}" -f $resultFile)
Write-Host ("Log: {0}" -f $logFile)

& $UnityExe @args
$exitCode = $LASTEXITCODE

if ($exitCode -ne 0) {
  throw "Unity Test Runner failed (exit $exitCode). See log: $logFile"
}

# Unity can return/exit before results are fully flushed to disk.
$sw = [Diagnostics.Stopwatch]::StartNew()
while (($sw.Elapsed.TotalSeconds -lt 90) -and (-not (Test-Path $logFile))) { Start-Sleep -Milliseconds 200 }
while (($sw.Elapsed.TotalSeconds -lt 120) -and (-not (Test-Path $resultFile))) { Start-Sleep -Milliseconds 200 }

if (-not (Test-Path $logFile)) {
  throw "Unity did not create the expected log file: $logFile"
}
if (-not (Test-Path $resultFile)) {
  throw "Unity did not create the expected test results file: $resultFile"
}

$logText = Get-Content $logFile -Raw

# Fatal project lock: batch tests require Unity editor to be closed.
if ($logText -match "another Unity instance is running|Multiple Unity instances cannot open the same project") {
  throw "Unity reports the project is already open. Close Unity for this project before running batchmode tests. Log: $logFile"
}

# Hard gate only on high-signal failures (avoid false positives from licensing noise).
if ($logText -match "Scripts have compiler errors|Compilation failed|Crash!!!|Fatal Error!") {
  throw "Unity log indicates a fatal failure (compile/crash). Log: $logFile"
}

# Truth source for pass/fail: the NUnit XML results.
[xml]$xml = Get-Content $resultFile
$run = $xml.'test-run'
if ($null -eq $run) {
  throw "Unrecognized test results format (missing <test-run>). File: $resultFile"
}

$failed = [int]$run.failed
$errors = [int]$run.errors
if ($failed -gt 0 -or $errors -gt 0) {
  throw "Unity tests reported failures (failed=$failed, errors=$errors). Results: $resultFile Log: $logFile"
}

Write-Host "Unity tests passed."
