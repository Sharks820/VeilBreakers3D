param(
  [switch]$SkipClaudePlugins
)

$ErrorActionPreference = "Stop"

function Test-JsonFile {
  param([string]$Path)
  if (-not (Test-Path $Path)) {
    Write-Host "[MISSING] $Path" -ForegroundColor Yellow
    return $false
  }
  try {
    Get-Content $Path -Raw | ConvertFrom-Json | Out-Null
    Write-Host "[OK] JSON parse: $Path" -ForegroundColor Green
    return $true
  } catch {
    Write-Host "[FAIL] JSON parse: $Path :: $($_.Exception.Message)" -ForegroundColor Red
    return $false
  }
}

$coreOk = Test-JsonFile ".mcp.json"
$fullOk = Test-JsonFile ".mcp.full.json"
$gemCoreOk = Test-JsonFile "Tools/mcp/gemini.settings.example.json"
$gemFullOk = Test-JsonFile "Tools/mcp/gemini.settings.full.example.json"

if ($coreOk) {
  $core = Get-Content ".mcp.json" -Raw | ConvertFrom-Json
  $coreServers = @($core.mcpServers.PSObject.Properties.Name)
  Write-Host ("Core MCP count: {0} -> {1}" -f $coreServers.Count, ($coreServers -join ", "))
}

if ($fullOk) {
  $full = Get-Content ".mcp.full.json" -Raw | ConvertFrom-Json
  $fullServers = @($full.mcpServers.PSObject.Properties.Name)
  Write-Host ("Full MCP count: {0} -> {1}" -f $fullServers.Count, ($fullServers -join ", "))
}

if (-not $SkipClaudePlugins) {
  if (Get-Command claude -ErrorAction SilentlyContinue) {
    $plugins = claude plugin list --json | ConvertFrom-Json
    $dupIds = @($plugins | Group-Object id | Where-Object { $_.Count -gt 1 })
    $dupEnabled = @()
    foreach ($g in $dupIds) {
      $enabledCount = ($g.Group | Where-Object { $_.enabled }).Count
      if ($enabledCount -gt 1) {
        $dupEnabled += $g.Name
      }
    }

    $episodicPresent = [bool]($plugins | Where-Object { $_.id -like "episodic-memory*" })

    Write-Host ("Duplicate plugin ids: {0}" -f $dupIds.Count)
    Write-Host ("Duplicate-enabled plugins: {0}" -f $dupEnabled.Count)
    if ($dupEnabled.Count -gt 0) {
      Write-Host ("Duplicate-enabled list: {0}" -f ($dupEnabled -join ", ")) -ForegroundColor Yellow
    }
    Write-Host ("Episodic-memory plugin present: {0}" -f $episodicPresent)
  } else {
    Write-Host "Claude CLI not found; skipping plugin checks." -ForegroundColor Yellow
  }
}

Write-Host "Reasoning stack check complete."
