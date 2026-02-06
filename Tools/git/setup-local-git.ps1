[CmdletBinding()]
param(
    [switch]$PruneMergedLocalBranches
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Test-Path ".git")) {
    throw "Run this script from the repository root."
}

Write-Host "Configuring local git defaults..."
git config --local core.hooksPath .githooks
git config --local fetch.prune true
git config --local pull.ff only
git config --local rerere.enabled true
git config --local rebase.autoStash true
git config --local merge.renames true
git config --local core.longpaths true
git config --local push.autoSetupRemote true

Write-Host "Fetching/pruning remotes..."
git fetch --prune

git show-ref --verify --quiet refs/heads/develop
$hasLocalDevelop = $LASTEXITCODE -eq 0
git show-ref --verify --quiet refs/remotes/origin/develop
$hasOriginDevelop = $LASTEXITCODE -eq 0
if ($hasLocalDevelop -and $hasOriginDevelop) {
    $upstream = (git for-each-ref --format="%(upstream:short)" refs/heads/develop).Trim()
    if (-not $upstream) {
        Write-Host "Setting upstream for develop -> origin/develop"
        git branch --set-upstream-to=origin/develop develop | Out-Null
    }
}

if ($PruneMergedLocalBranches) {
    Write-Host "Pruning merged local branches (excluding master/develop/current)..."
    $current = (git branch --show-current).Trim()
    $protected = @("master", "develop", $current)
    $merged = git branch --merged master | ForEach-Object { $_.Trim().TrimStart("*").Trim() } | Where-Object { $_ -and -not ($protected -contains $_) }
    foreach ($branch in $merged) {
        git branch -d $branch | Out-Host
    }
}

Write-Host "`nDone. Current branch overview:"
git branch -a -vv
