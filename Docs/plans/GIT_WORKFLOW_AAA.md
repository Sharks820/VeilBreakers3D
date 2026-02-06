# VeilBreakers Git Workflow (AAA-Oriented)

## Goals
- Keep `master` always releasable.
- Keep `develop` integration-ready.
- Minimize Unity merge pain with short-lived branches and narrow PR scope.
- Improve traceability of gameplay/system/design changes.

## Branch Model
- `master`: production branch.
- `develop`: next release integration branch.
- `feature/<domain>-<name>`: feature work.
- `fix/<domain>-<name>`: bugfix work.
- `release/<version>`: release hardening.
- `hotfix/<name>`: urgent production fix.

## Pull Request Rules
- Feature/fix PRs target `develop`.
- Hotfix PRs target `master` and are back-merged to `develop`.
- Prefer PRs under ~500 changed lines unless scene/content migration requires larger.
- Attach screenshots/clips for UI/VFX.

## Unity Collaboration Rules
- Keep Unity serialization in text mode.
- Commit `.meta` files with assets.
- Avoid parallel edits to same `.unity` scene where possible.
- If scene conflict happens, re-open in Unity and re-save intentionally.

## Local Setup
Run once from repo root:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\git\setup-local-git.ps1
```

Optional local branch prune:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\git\setup-local-git.ps1 -PruneMergedLocalBranches
```

## Branch Cleanup
Use:
- `Docs/plans/GIT_BRANCH_CLEANUP_RUNBOOK_2026-02-05.md`

