---
phase: 03-controller-behavior
plan: 03
subsystem: ui
tags: [gamepad, focus-manager, hold-to-confirm, nebula-cache, zero-gc, audio-feedback, ui-toolkit]

# Dependency graph
requires:
  - phase: 03-controller-behavior (plan 01)
    provides: CharSelectEvents scoped event bus, UXML structure with tab system
  - phase: 03-controller-behavior (plan 02)
    provides: Updated CharacterSelectManager with sub-controller wiring pattern
provides:
  - CharSelectFocusManager with 4-zone gamepad navigation and visible focus ring
  - HoldToEmbarkController with 1.5s hold-to-confirm embark flow
  - Pre-baked nebula texture cache for zero-GC hero switching
  - Placeholder audio feedback on navigation, hero switch, and embark
  - Event-driven embark pipeline (HoldToEmbarkController triggers, CharacterSelectManager executes)
affects: [04-visual-polish, 05-integration-testing]

# Tech tracking
tech-stack:
  added: [AudioClip.Create runtime tone generation]
  patterns: [zone-based focus navigation, hold-to-confirm gesture, texture pre-bake cache, shared pixel buffer reuse]

key-files:
  created:
    - Assets/Scripts/UI/CharacterSelect/CharSelectFocusManager.cs
    - Assets/Scripts/UI/CharacterSelect/HoldToEmbarkController.cs
  modified:
    - Assets/Scripts/UI/CharacterSelect/CharSelectEnvironmentController.cs
    - Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs
    - Assets/Scripts/UI/CharacterSelect/CharSelectEvents.cs

key-decisions:
  - "Zone navigation graph uses static 4x4 FocusZone array for deterministic D-pad traversal between Back/InfoTabs/Embark/Carousel zones"
  - "Placeholder audio tones generated via AudioClip.Create at runtime (sine waves with fade-out envelopes) to avoid silent interactions before real audio assets exist"
  - "Nebula pre-bake uses heroId.GetHashCode() for deterministic noise offset instead of Random.Range, ensuring consistent visuals across sessions"
  - "HoldToEmbarkController creates progress ring element dynamically if not found in UXML, ensuring backward compatibility"
  - "Confirm popup flow (btn-confirm, btn-cancel, confirm-overlay) fully removed from CharacterSelectManager -- replaced by hold-to-confirm pattern"
  - "NavigationMoveEvent/NavigationSubmitEvent handling moved from CharacterSelectManager to CharSelectFocusManager for proper zone-based navigation"

patterns-established:
  - "Zone-based focus navigation: FocusZone enum + static navigation graph + MoveFocusToZone with CSS class toggle"
  - "Hold-to-confirm gesture: Update polling both PointerDown and InputManager.GetAction for mouse/gamepad unified hold detection"
  - "Texture pre-bake cache: Dictionary<string,Texture2D> with shared Color[] buffer, generated once at init, assigned by TryGetValue on switch"
  - "Placeholder audio via AudioClip.Create: Generate sine wave tones at init, play via PlayOneShot, destroy in OnDisable"

requirements-completed: [CTRL-01, CTRL-02, CTRL-03, CTRL-05, CTRL-08]

# Metrics
duration: 13min
completed: 2026-03-18
---

# Phase 3 Plan 3: Controller Behavior Summary

**Zone-based gamepad navigation (4-zone FocusManager), 1.5s hold-to-embark with progress ring, pre-baked nebula texture cache for zero-GC hero switching, and placeholder audio feedback on all interactions**

## Performance

- **Duration:** 13 min
- **Started:** 2026-03-18T23:55:23Z
- **Completed:** 2026-03-19T00:09:12Z
- **Tasks:** 2
- **Files modified:** 5

## Accomplishments
- Created CharSelectFocusManager with 4-zone D-pad navigation, L1/R1 hero switch + tab cycle, visible focus ring, and placeholder audio tones
- Created HoldToEmbarkController with 1.5s hold-to-confirm tracking both mouse and gamepad, progress ring visual, and focus trap during hold
- Refactored CharSelectEnvironmentController to pre-bake all nebula textures at init with shared pixel buffer -- zero Color[] allocation on hero switch
- Wired CharacterSelectManager to receive embark trigger via event subscription, removing entire confirm popup flow
- Extended CharSelectEvents with 7 new events (OnTabChanged, OnEmbarkHoldProgress, OnEmbarkTriggered, OnErrorOccurred, OnLoadingStarted, OnLoadingComplete, OnFocusZoneChanged)

## Task Commits

Each task was committed atomically:

1. **Task 1: CharSelectFocusManager** - `2e96e09` (feat) -- Zone-based gamepad navigation with focus ring and audio
2. **Task 2: HoldToEmbarkController + Nebula Cache + Manager Wiring** - `20ea901` (feat) -- Hold-to-embark, pre-baked nebula cache, event-driven embark

## Files Created/Modified
- `Assets/Scripts/UI/CharacterSelect/CharSelectFocusManager.cs` - Zone-based gamepad navigation with 4 zones, L1/R1 handling, visible focus ring, placeholder audio
- `Assets/Scripts/UI/CharacterSelect/HoldToEmbarkController.cs` - 1.5s hold-to-confirm with mouse + gamepad tracking, progress ring, focus trap, audio
- `Assets/Scripts/UI/CharacterSelect/CharSelectEnvironmentController.cs` - Pre-baked nebula cache with shared pixel buffer, zero-alloc hero switch
- `Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs` - Event-driven embark, removed confirm popup, simplified navigation
- `Assets/Scripts/UI/CharacterSelect/CharSelectEvents.cs` - 7 new events for tab/hold/error/loading/focus zone communication

## Decisions Made
- Used static FocusZone[4,4] navigation graph instead of dynamic spatial resolution -- simpler, deterministic, matches the fixed 4-zone layout
- Generated placeholder audio via AudioClip.Create at init rather than shipping WAV files -- allows immediate feedback without art pipeline dependency
- Used heroId.GetHashCode() for deterministic noise offset in nebula generation instead of Random.Range -- ensures identical textures across sessions
- Created progress ring element dynamically in HoldToEmbarkController if not found in UXML -- ensures the controller works with both old and new UXML layouts
- Removed entire confirm popup flow (ShowConfirmPopup, HideConfirmPopup, btn-confirm, btn-cancel, confirm-overlay) from CharacterSelectManager -- hold-to-embark replaces the popup pattern completely

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Extended CharSelectEvents with new events before creating controllers**
- **Found during:** Task 1 (CharSelectFocusManager creation)
- **Issue:** Plan referenced events (RaiseTabChanged, RaiseFocusZoneChanged, RaiseEmbarkHoldProgress, RaiseEmbarkTriggered) that did not exist in CharSelectEvents
- **Fix:** Added all 7 new events with Raise methods and ClearAll cleanup before creating the controllers
- **Files modified:** Assets/Scripts/UI/CharacterSelect/CharSelectEvents.cs
- **Verification:** All new events compile and are referenceable from controllers
- **Committed in:** 2e96e09 (Task 1 commit)

**2. [Rule 1 - Bug] Adapted element names to match actual UXML**
- **Found during:** Task 1 (CharSelectFocusManager creation)
- **Issue:** Plan referenced "info-panel-container" element name but UXML has "hero-info-panel"
- **Fix:** Used "hero-info-panel" in CharSelectFocusManager to match actual UXML element name
- **Files modified:** Assets/Scripts/UI/CharacterSelect/CharSelectFocusManager.cs
- **Verification:** Element name matches CharacterSelect.uxml
- **Committed in:** 2e96e09 (Task 1 commit)

**3. [Rule 3 - Blocking] Created progress ring element dynamically**
- **Found during:** Task 2 (HoldToEmbarkController creation)
- **Issue:** UXML does not contain an "embark-progress-ring" element
- **Fix:** HoldToEmbarkController creates the progress ring dynamically in HandleScreenReady if not found via Q()
- **Files modified:** Assets/Scripts/UI/CharacterSelect/HoldToEmbarkController.cs
- **Verification:** Progress ring created with correct styling, pickingMode, and UsageHints
- **Committed in:** 20ea901 (Task 2 commit)

---

**Total deviations:** 3 auto-fixed (1 bug, 2 blocking)
**Impact on plan:** All auto-fixes were necessary for correctness. No scope creep.

## Issues Encountered
None beyond the deviations documented above.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- All 6 interaction points are now wired: Back (click), Prev/Next (click + D-pad), Embark (hold-to-confirm), Tab switch (L1/R1 + click), Carousel (card click + D-pad)
- Gamepad navigation fully functional with visible focus ring
- Zero-GC hero switching achieved via nebula texture cache
- Audio feedback in place (placeholder tones -- real assets can replace via HeroDisplayConfig.selectionSFX)
- Ready for Phase 4: PrimeTween animations, post-processing, cinematic embark sequence
- Blocker: Audio assets still needed (placeholder tones are functional but not AAA quality)

## Self-Check: PASSED

All 6 files verified present. Both task commits (2e96e09, 20ea901) verified in git log.

---
*Phase: 03-controller-behavior*
*Completed: 2026-03-18*
