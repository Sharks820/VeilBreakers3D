---
phase: 06-character-select-aaa-rebuild
plan: 02
subsystem: ui
tags: [shader, dissolve, urp, material, render, unity]

# Dependency graph
requires:
  - phase: 06-character-select-aaa-rebuild
    provides: VeilDissolveController class, VeilDissolve.shader, CharacterSelectManager auto-wiring
provides:
  - VeilDissolvePlaceholder material with real shader reference
  - VeilDissolveController.Init(Renderer) called on every hero model swap
  - Dissolve animation chain fully wired (no longer silently no-ops)
affects: [character-select, hero-switch, dissolve-vfx]

# Tech tracking
tech-stack:
  added: []
  patterns: [controller-to-renderer wiring via SetDissolveController, MaterialPropertyBlock dissolve on hero swap]

key-files:
  created: []
  modified:
    - Assets/Resources/CharacterSelect/HeroThemes/VeilDissolvePlaceholder.mat
    - Assets/Scripts/UI/CharacterSelect/HeroStageController.cs
    - Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs

key-decisions:
  - "Used white noise texture default (fileID: 0) since VeilDissolve shader falls back to white identity noise for clean linear dissolve"
  - "Init(Renderer) placed after model instantiation and layer setup but before camera/lighting config in SwapHeroModel"

patterns-established:
  - "SetDissolveController injection pattern: CharacterSelectManager wires dissolve controller into HeroStageController via public setter, which then calls Init(renderer) per model swap"

requirements-completed: [CHARSEL-05]

# Metrics
duration: 3min
completed: 2026-03-31
---

# Phase 6 Plan 02: Dissolve Controller Wiring Summary

**Fixed VeilDissolvePlaceholder material shader reference and wired VeilDissolveController.Init(Renderer) into the hero model swap pipeline so dissolve animations activate on hero switch**

## Performance

- **Duration:** 3 min
- **Started:** 2026-03-31T13:43:10Z
- **Completed:** 2026-03-31T13:45:51Z
- **Tasks:** 1
- **Files modified:** 3

## Accomplishments
- Assigned VeilDissolve shader (GUID 3280572c942554b42bcd25098ca87124) to VeilDissolvePlaceholder material (was null/missing)
- Added _dissolveController field and SetDissolveController() injection to HeroStageController
- Wired Init(Renderer) call in SwapHeroModel after model instantiation, so every hero swap activates the dissolve system
- Connected CharacterSelectManager.EnsureCharSelectComponents() to pass dissolve controller to stage controller

## Task Commits

Each task was committed atomically:

1. **Task 1: Fix VeilDissolvePlaceholder material and wire dissolve controller to model renderer** - `ee413e2` (feat)

## Files Created/Modified
- `Assets/Resources/CharacterSelect/HeroThemes/VeilDissolvePlaceholder.mat` - Assigned VeilDissolve shader reference (m_Shader was fileID: 0, now references real shader)
- `Assets/Scripts/UI/CharacterSelect/HeroStageController.cs` - Added _dissolveController field, SetDissolveController() method, and Init(renderer) call in SwapHeroModel
- `Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs` - Added stageCtrl.SetDissolveController(dissolveCtrl) wiring in EnsureCharSelectComponents()

## Decisions Made
- Used white noise texture default (fileID: 0) since VeilDissolve shader's Properties block declares `_NoiseTexture ("Noise Texture", 2D) = "white" {}` which produces a clean linear dissolve. A Perlin noise texture can be added later for more organic dissolve patterns.
- Placed Init(Renderer) call after model instantiation + layer/scale/rotation setup but before camera/lighting config, ensuring the renderer is fully configured before dissolve properties are applied.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Dissolve system fully wired: HeroThemeTransitioner -> HeroSwitchAnimator -> VeilDissolveController now has a real Renderer and real material
- CHARSEL-05 requirement satisfied
- Phase 6 plan 01 (VolumeProfile assets) is the remaining incomplete plan in this phase

---
*Phase: 06-character-select-aaa-rebuild*
*Completed: 2026-03-31*

## Self-Check: PASSED

- FOUND: VeilDissolvePlaceholder.mat
- FOUND: HeroStageController.cs
- FOUND: CharacterSelectManager.cs
- FOUND: ee413e2 (task commit)
