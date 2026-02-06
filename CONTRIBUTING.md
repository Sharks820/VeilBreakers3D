# Contributing to VeilBreakers3D

## Branch Strategy
- `master`: production-ready, shippable.
- `develop`: integration branch for upcoming release.
- `feature/<area>-<short-name>`: gameplay/UI/content features.
- `fix/<area>-<short-name>`: bug fixes.
- `hotfix/<short-name>`: urgent production fixes from `master`.
- `release/<version>`: release stabilization from `develop`.

## Daily Flow
1. Branch from `develop` for normal work.
2. Keep branches short-lived and focused (one domain per PR).
3. Rebase or merge from `develop` frequently to reduce Unity merge conflicts.
4. Open PR into `develop` (or `master` for hotfix).

## Commit Conventions
Use concise conventional-style commits:
- `feat: add combat stagger tuning panel`
- `fix: prevent null ref in capture banner`
- `perf: reduce allocations in title vfx update`
- `chore: adjust build settings and docs`

## Unity-Specific Rules
- Keep `Visible Meta Files` and `Force Text` serialization in Unity project settings.
- Commit `.meta` files with their assets.
- Avoid editing the same scene/prefab in parallel when possible.
- Prefer prefab/ScriptableObject changes over giant scene edits for collaboration.

## PR Expectations
- PR title follows commit style (`feat:`, `fix:`, etc.).
- Include test evidence for changed systems (EditMode/PlayMode/manual steps).
- Include screenshots or short clips for UI/VFX changes.
- Keep PR scope limited: no unrelated cleanup.

## Test/Validation Checklist
- `dotnet build .\\Assembly-CSharp.csproj -v minimal`
- Run relevant Unity EditMode/PlayMode tests.
- Verify changed scenes load and run without console errors.

## GitHub CI (Required)
- Workflow: `.github/workflows/unity-ci.yml`
- Required repo secrets for Unity activation:
  - `UNITY_LICENSE` (recommended) or
  - `UNITY_EMAIL`, `UNITY_PASSWORD`, and `UNITY_SERIAL`
- Recommended required status checks in branch protection:
  - `Unity CI / Build (Windows x64)`
  - `Unity CI / Test (EditMode)`
  - `Unity CI / Test (PlayMode Smoke)`
