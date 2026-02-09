param(
  [string]$CoverageRoot = "artifacts",
  [double]$MinLinePercent = 35.0
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $CoverageRoot)) {
  throw "Coverage root not found: $CoverageRoot"
}

$summaryFiles = Get-ChildItem -Path $CoverageRoot -Recurse -Filter "Summary.xml" -File -ErrorAction SilentlyContinue
if (-not $summaryFiles -or $summaryFiles.Count -eq 0) {
  throw "No Summary.xml found under '$CoverageRoot'. Ensure code coverage output is enabled."
}

function Get-LineCoveragePercent {
  param([string]$SummaryFilePath)

  [xml]$doc = Get-Content $SummaryFilePath -Raw

  $summaryNode = $doc.SelectSingleNode("//*[local-name()='Summary']")
  if ($summaryNode -and $summaryNode.Attributes["linecoverage"]) {
    return [double]$summaryNode.Attributes["linecoverage"].Value
  }

  $coverageNode = $doc.SelectSingleNode("//*[local-name()='coverage']")
  if ($coverageNode -and $coverageNode.Attributes["line-rate"]) {
    $lineRate = [double]$coverageNode.Attributes["line-rate"].Value
    if ($lineRate -le 1.0) { return $lineRate * 100.0 }
    return $lineRate
  }

  $raw = Get-Content $SummaryFilePath -Raw
  $lineCoverageRegex = [regex]::Match($raw, "linecoverage=""(?<v>[0-9]+(?:\.[0-9]+)?)""")
  if ($lineCoverageRegex.Success) {
    return [double]$lineCoverageRegex.Groups["v"].Value
  }

  $lineRateRegex = [regex]::Match($raw, "line-rate=""(?<v>[0-9]+(?:\.[0-9]+)?)""")
  if ($lineRateRegex.Success) {
    $lineRate = [double]$lineRateRegex.Groups["v"].Value
    if ($lineRate -le 1.0) { return $lineRate * 100.0 }
    return $lineRate
  }

  throw "Could not parse line coverage from '$SummaryFilePath'."
}

$bestCoverage = -1.0
$bestFile = ""

foreach ($file in $summaryFiles) {
  $coverage = Get-LineCoveragePercent -SummaryFilePath $file.FullName
  Write-Host ("Coverage candidate: {0} => {1:N2}%" -f $file.FullName, $coverage)
  if ($coverage -gt $bestCoverage) {
    $bestCoverage = $coverage
    $bestFile = $file.FullName
  }
}

if ($bestCoverage -lt 0) {
  throw "No valid coverage summary could be parsed."
}

Write-Host ("Selected coverage summary: {0}" -f $bestFile)
Write-Host ("Line coverage: {0:N2}% (threshold: {1:N2}%)" -f $bestCoverage, $MinLinePercent)

if ($bestCoverage -lt $MinLinePercent) {
  throw ("Coverage gate failed: {0:N2}% < {1:N2}%" -f $bestCoverage, $MinLinePercent)
}

Write-Host "Coverage gate passed."
