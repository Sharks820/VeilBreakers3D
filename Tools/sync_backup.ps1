param(
    [string]$Source = "C:\Users\Conner\OneDrive\Documents\VeilBreakers3DCurrent",
    [string]$Destination = "C:\Users\Conner\VeilBreakers3DCurrent_BACKUP"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $Source)) {
    Write-Error "Source not found: $Source"
    exit 1
}

if (-not (Test-Path $Destination)) {
    New-Item -ItemType Directory -Path $Destination | Out-Null
}

$excludeDirs = @(
    "Library",
    "Temp",
    "Logs",
    "TestResults",
    "obj",
    "TempCompileCheck",
    "UserSettings"
)

Write-Host "Syncing backup..."
Write-Host "  Source:      $Source"
Write-Host "  Destination: $Destination"
Write-Host "  Excluding:   $($excludeDirs -join ', ')"

& robocopy $Source $Destination /MIR /XJ /R:2 /W:5 /FFT /COPY:DAT /DCOPY:DA /XD $excludeDirs | Out-Host

$rc = $LASTEXITCODE
if ($rc -gt 7) {
    Write-Error "Robocopy failed with exit code $rc"
    exit $rc
}

Write-Host "Backup sync complete. Robocopy exit code: $rc"
exit 0
