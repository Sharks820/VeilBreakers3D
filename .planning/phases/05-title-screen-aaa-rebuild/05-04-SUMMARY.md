---
phase: 05-title-screen-aaa-rebuild
plan: 04
subsystem: ui
tags: [unity, ui-toolkit, vfx, audio, z-order, randomized-weighted, texture-glow]

# Dependency graph
requires:
  - phase: 05-01
    provides: UITextureRegistry for texture tracking
  - phase: 05-02
    provides: Blur test result (inconclusive -- texture-based glow confirmed)
  - phase: 05-03
    provides: Decomposed TitleScreenVFX with 5 subsystems
provides:
  - UIVFXContainer for named z-order layer management
  - VERATitleAudio with weighted random selection, per-pattern cooldowns, history exclusion
  - Texture-based glow with UITextureRegistry tracking in ButtonVFXHelper
  - TitleScreenLogoVFX with UITextureRegistry integration and Cleanup method
affects: [title-screen-aaa-rebuild, character-select-aaa-rebuild]

# Tech tracking
tech-stack:
  added: []
patterns: [named-vfx-layers, weighted-random-interactions, texture-registry-tracking]

key-files:
  created:
    - Assets/Scripts/UI/Core/UIVFXContainer.cs
    - Assets/Scripts/Audio/VERATitleAudio.cs
  modified:
    - Assets/Scripts/UI/Core/TitleScreenAudio.cs
    - Assets/Scripts/UI/Core/TitleScreenVFX.cs
    - Assets/Scripts/UI/Controls/ButtonVFXHelper.cs
    - Assets/Scripts/UI/VFX/TitleScreenLogoVFX.cs

key-decisions:
  - "FilterFunction.Blur() remains unused -- Plan 02 test was inconclusive, texture-based glow approach documented"
  - "VERA interactions use weighted random with per-pattern cooldowns (25-45s) and history exclusion (last 2) instead of sequential cycling"
  - "UIVFXContainer manages 6 named layers: background, atmosphere, particles, lightning, logo, ui"
  - "TitleScreenAudio generates clips and passes them to VERATitleAudio via PopulateInteractions runtime method"

patterns-established:
  - "UIVFXContainer pattern: named z-order layer management replaces fragile root.Insert() calls"
  - "VERATitleAudio pattern: weighted random selection + per-pattern cooldown + history exclusion for organic audio"
  - "Texture glow pattern: UITextureRegistry parameter on glow methods for leak-free cleanup"

requirements-completed: [TITLE-05, TITLE-06, TITLE-09, TITLE-10]

# Metrics
duration: 15min
completed: 2026-03-31
---

# Phase 05: Title Screen AAA Rebuild -- Plan 04 Summary

**UIVFXContainer for named z-order layer management, VERATitleAudio with weighted random interactions, texture-based button glow with UITextureRegistry tracking**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-03-31T12:57:02Z
- **Completed:** 2026-03-31T13:11:45Z
- **Tasks:** 2
- **Files modified:** 6

## Accomplishments
- Created UIVFXContainer: plain C# utility with named layer lookup and bottom-to-top z-ordering (6 layers)
- Created VERATitleAudio: MonoBehaviour replacing sequential VERAInteractions with weighted random selection, per-pattern cooldowns (25-45s), and history exclusion (last 2 interactions)
- Removed sequential VERAInteractions() from TitleScreenAudio, wired delegation to VERATitleAudio
- Added texture-based glow methods to ButtonVFXHelper with UITextureRegistry tracking for leak-free cleanup
- Documented FilterFunction.Blur limitation in ButtonVFXHelper class header

## Task Commits

Each task was committed atomically:

1. **Task 1: Create UIVFXContainer and VERATitleAudio, integrate into title screen** - `e779293` (feat)
2. **Task 2: Integrate glow into ButtonVFXHelper and TitleScreenLogoVFX** - `353a742` (feat)

## Files Created/Modified
- `Assets/Scripts/UI/Core/UIVFXContainer.cs` - Named z-order layer management utility (6 layers: background, atmosphere, particles, lightning, logo, ui)
- `Assets/Scripts/Audio/VERATitleAudio.cs` - Weighted random VERA interaction selection with cooldowns and history exclusion
- `Assets/Scripts/UI/Core/TitleScreenAudio.cs` - Removed sequential VERAInteractions, delegates to VERATitleAudio
- `Assets/Scripts/UI/Core/TitleScreenVFX.cs` - Integrated UIVFXContainer for named layer management
- `Assets/Scripts/UI/Controls/ButtonVFXHelper.cs` - Added SetupTextureGlow with UITextureRegistry, documented blur limitation
- `Assets/Scripts/UI/VFX/TitleScreenLogoVFX.cs` - Added UITextureRegistry support and Cleanup method

## Decisions Made
- **Texture-based glow only:** FilterFunction.Blur() was tested in Plan 02 but results were inconclusive (test file deleted). Documenting limitation in ButtonVFXHelper class header. Native blur can be re-evaluated later.
- **Runtime clip population:** VERATitleAudio.PopulateInteractions() receives procedurally generated clips from TitleScreenAudio at runtime, keeping audio generation centralized.
- **Interaction weights:** "whisper_comment" weight=1.0/cooldown=25s, "demon_cackle" weight=0.7/cooldown=35s (rare), "mysterious_hint" weight=0.5/cooldown=45s (sparse), "vera_reacts" weight=1.0/cooldown=20s (frequent).

## Deviations from Plan

None - plan executed exactly as written. The blur path was correctly determined from Plan 02 summary (inconclusive -> texture-based).

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- UIVFXContainer ready for use in Phase 6 Character Select if needed
- VERATitleAudio pattern ready for reuse in other audio contexts
- All 4 plans in Phase 5 complete -- ready for Phase 6 (Character Select AAA Rebuild)

---
*Phase: 05-title-screen-aaa-rebuild*
*Completed: 2026-03-31*

## Self-Check: PASSED
- All 6 created/modified files verified present on disk
- Both task commits (e779293, 353a742) found in git history
