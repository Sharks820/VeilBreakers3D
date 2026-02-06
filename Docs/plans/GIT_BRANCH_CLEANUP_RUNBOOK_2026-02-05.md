# Git Branch Cleanup Runbook (2026-02-05)

## Snapshot from audit
- `master` -> `origin/master` (`ahead 0`, `behind 0`)
- `develop` has no upstream configured
- `feature/monster-system` -> `origin/feature/monster-system` (`ahead 1`, `behind 0`)
- `feature/world-terrain` -> `origin/feature/world-terrain` (`ahead 1`, `behind 0`)
- `backup/pre-unity6` -> `origin/backup/pre-unity6` (`ahead 0`, `behind 3`)
- Other local feature branches are fully synced (`ahead 0`, `behind 0`)
- Remote branches merged into `origin/master`: `origin/backup/pre-unity6`, `origin/develop`, all `origin/feature/*` branches listed in this repo

## 1) Safety preflight
```bash
git fetch --prune
git checkout master
git pull --ff-only
```

## 2) Normalize `develop` tracking
```bash
git branch --set-upstream-to=origin/develop develop
```

## 3) Resolve local ahead branches before cleanup
Choose one option for each:

### Option A: Keep and publish local work
```bash
git push origin feature/monster-system
git push origin feature/world-terrain
```

### Option B: Discard local-only commits (destructive)
```bash
git checkout feature/monster-system
git reset --hard origin/feature/monster-system
git checkout feature/world-terrain
git reset --hard origin/feature/world-terrain
git checkout master
```

## 4) Delete merged local branches (safe list)
Run after step 3 is settled:
```bash
git branch -d feature/ai-behavior feature/capture-system feature/champion-system feature/combat-system feature/dialogue-vera feature/ui-system
```

Optional, if no longer needed:
```bash
git branch -d backup/pre-unity6
```

## 5) Delete merged remote branches (GitHub cleanup)
Run only for branches your team no longer wants to keep:
```bash
git push origin --delete feature/ai-behavior feature/capture-system feature/champion-system feature/combat-system feature/dialogue-vera feature/ui-system
```

Optional:
```bash
git push origin --delete backup/pre-unity6
```

## 6) Final verification
```bash
git branch -a -vv
git for-each-ref --format="%(refname:short) %(upstream:short) %(upstream:track)" refs/heads
git branch -r --merged origin/master
```

