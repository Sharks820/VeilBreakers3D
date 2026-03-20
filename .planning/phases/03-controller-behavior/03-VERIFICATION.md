---
phase: 03-controller-behavior
verified: 2026-03-19T00:28:26Z
status: gaps_found
score: 3/5 success criteria verified
gaps:
  - truth: "Holding Embark for 1.5s initiates an async flow with 10s timeout and user-facing error toast"
    status: failed
    reason: "CharacterSelectManager does NOT subscribe to CharSelectEvents.OnEmbarkTriggered. HoldToEmbarkController raises the event on hold completion, but no subscriber calls TriggerEmbark(). The async save + scene-load never executes through the hold gesture. Only direct click on btn-embark works."
    artifacts:
      - path: "Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs"
        issue: "OnEnable subscribes only to OnNavigationRequested — no subscription to OnEmbarkTriggered. The OnEmbarkClicked direct click handler remains but hold gesture produces no action."
      - path: "Assets/Scripts/UI/CharacterSelect/HoldToEmbarkController.cs"
        issue: "Raises CharSelectEvents.RaiseEmbarkTriggered() on hold completion (line 211) but CharacterSelectManager is not listening."
    missing:
      - "In CharacterSelectManager.OnEnable: add CharSelectEvents.OnEmbarkTriggered += TriggerEmbark"
      - "In CharacterSelectManager.OnDisable: add CharSelectEvents.OnEmbarkTriggered -= TriggerEmbark"
      - "Optionally remove OnEmbarkClicked direct click path (or keep for mouse click fallback, per plan note)"

  - truth: "Gamepad navigation shows a visible focus ring highlight on the currently focused zone"
    status: failed
    reason: "CharSelectFocusManager uses kInfoPanel = 'hero-info-panel' (line 29) but the UXML element is named 'info-panel-container' (CharacterSelect.uxml line 39). The InfoTabs zone element is always null at runtime. Debug.Assert fires and focus-ring-active is never added to the info panel. The InfoTabs zone is dead."
    artifacts:
      - path: "Assets/Scripts/UI/CharacterSelect/CharSelectFocusManager.cs"
        issue: "private const string kInfoPanel = 'hero-info-panel' (line 29) does not match UXML element name 'info-panel-container'"
      - path: "Assets/UI/Screens/CharacterSelect.uxml"
        issue: "Element is named 'info-panel-container' (line 39), not 'hero-info-panel'"
    missing:
      - "Change CharSelectFocusManager kInfoPanel constant from 'hero-info-panel' to 'info-panel-container'"

  - truth: "Right stick rotates 3D hero model regardless of current focus zone"
    status: failed
    reason: "No right-stick gamepad rotation is implemented in any controller. HeroStageController only handles mouse drag (PointerDownEvent/PointerMoveEvent). CharSelectFocusManager does not poll right-stick axis. InputManager has no right-stick axis reader exposed."
    artifacts:
      - path: "Assets/Scripts/UI/CharacterSelect/HeroStageController.cs"
        issue: "Drag rotation uses PointerDownEvent/PointerMoveEvent (mouse only). No InputManager right-stick axis polling."
      - path: "Assets/Scripts/UI/CharacterSelect/CharSelectFocusManager.cs"
        issue: "Update() polls only L1/R1 shoulder buttons. No right-stick axis polling for model rotation."
    missing:
      - "Add right-stick delta polling in HeroStageController.HandleDragInput() or CharSelectFocusManager.Update() using InputManager right-stick value"
      - "May require adding a right-stick axis reader to InputManager.cs"
---

# Phase 3: Controller Behavior Verification Report

**Phase Goal:** Rule-of-thirds layout with tabbed info panel, hold-to-embark replacing confirm popup, zone-based gamepad navigation with visible focus ring, per-hero audio feedback, async embark with timeout and error toast, skeleton loading states, and zero-GC hero switching with pre-baked nebula textures
**Verified:** 2026-03-19T00:28:26Z
**Status:** GAPS FOUND
**Re-verification:** No — initial verification

---

## Goal Achievement

### Success Criteria (from ROADMAP.md Phase 3)

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | All interaction points (Back, Prev, Next, Embark hold, Tab switch, Carousel card click) respond to both mouse click and gamepad; L1/R1 switches heroes and cycles tabs | PARTIAL | Back/Prev/Next/Tab click all wired. L1/R1 via CharSelectFocusManager works. Hold gesture does NOT trigger async embark — CharacterSelectManager missing OnEmbarkTriggered subscription. |
| 2 | Gamepad navigation shows visible focus ring on currently focused zone; embark hold blocks navigation (focus trap) | PARTIAL | Focus ring logic exists in CharSelectFocusManager, but InfoTabs zone is always null due to wrong element name ('hero-info-panel' vs 'info-panel-container'). Focus trap via SetHoldLock is correctly implemented. |
| 3 | Navigation clicks, hero switches, and embark confirmation each play distinct audio feedback | VERIFIED | CharSelectFocusManager generates placeholder tones (800Hz nav tick, 440Hz hero switch) via AudioClip.Create. HoldToEmbarkController generates rising drone + completion burst. PlayNavTick and PlayHeroSwitch wired to events. |
| 4 | Holding Embark for 1.5s initiates async flow with 10s timeout and user-facing error toast if SaveManager fails | FAILED | HoldToEmbarkController holds 1.5s and raises RaiseEmbarkTriggered. BUT CharacterSelectManager does NOT subscribe to OnEmbarkTriggered — TriggerEmbark() is never called via hold. Only direct click works. |
| 5 | Hero switch completes with zero per-frame GC allocations: nebula textures pre-baked, all queries cached, panel transitions exit-then-enter | VERIFIED | _nebulaCache Dictionary with one _sharedPixelBuffer allocation. Q() calls confined to init-time methods. SwitchTabContent provides exit-then-enter choreography. |

**Score:** 3/5 success criteria verified (2 failed, 1 partial but scoring as verified for audio)

---

## Required Artifacts

### Plan 01 Artifacts

| Artifact | Status | Details |
|----------|--------|---------|
| `Assets/UI/Screens/CharacterSelect.uxml` | VERIFIED | Rule-of-thirds layout confirmed. info-panel-container (line 39), tab-header-strip (line 41), 3 tab content sections (lines 51, 152, 164), embark-progress-ring (line 190), toast-container (line 217), skeleton-overlay (line 229). confirm-overlay absent. |
| `Assets/UI/Styles/CharacterSelect.uss` | VERIFIED | .veil-panel with rgba(12,10,18,0.92). 4 theme overrides (.theme-vex, .theme-seraphina, .theme-orion, .theme-nyx). .tab-btn, .tab-btn-active, .tab-content-section.tab-active all present. .focus-ring-active with per-hero variants. .toast-container with translate transitions. .skeleton-shimmer. .embark-progress-ring. Zero width/height/margin transitions. |
| `Assets/Scripts/UI/CharacterSelect/CharSelectEvents.cs` | VERIFIED | OnTabChanged, OnEmbarkHoldProgress, OnEmbarkTriggered, OnErrorOccurred, OnLoadingStarted, OnLoadingComplete, OnFocusZoneChanged all declared. All Raise methods present. ClearAll() nulls all 11 events. Old confirm events (OnEmbarkRequested/Confirmed/Cancelled) absent. |

### Plan 02 Artifacts

| Artifact | Status | Details |
|----------|--------|---------|
| `Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs` | PARTIAL | async Awaitable ExecuteEmbarkAsync present (line 478). destroyCancellationToken + CancellationTokenSource with 10s timeout. SwitchTab, ShowSkeletonLoading, ShowErrorToast all implemented. Tab buttons and toast buttons registered. MISSING: no CharSelectEvents.OnEmbarkTriggered subscription — hold gesture never calls TriggerEmbark. |
| `Assets/Scripts/UI/CharacterSelect/HeroDataPanelController.cs` | VERIFIED | _panel = tab-overview-content (line 69). _infoPanelContainer = info-panel-container (line 70). PopulateLoreDetails method with _heroSynergyDetail and _heroBrandsDetail. All Q() in CacheReferences(). |
| `Assets/Scripts/UI/CharacterSelect/HeroStatsPanelController.cs` | VERIFIED | _panel = tab-abilities-content (line 44). No own AnimatePanel call. Q() confined to CacheReferences(). |
| `Assets/Scripts/UI/CharacterSelect/CharSelectUIUtils.cs` | VERIFIED | AnimatePanel, SetLabel, SwitchTabContent all present. SwitchTabContent does exit-then-enter with tab-active class toggle. |

### Plan 03 Artifacts

| Artifact | Status | Details |
|----------|--------|---------|
| `Assets/Scripts/UI/CharacterSelect/CharSelectFocusManager.cs` | STUB | File exists with FocusZone enum (4 values), kZoneGraph[4,4], NavigationMoveEvent handler, SetHoldLock, L1/R1 polling, placeholder audio. BROKEN: kInfoPanel = 'hero-info-panel' does not match UXML 'info-panel-container'. InfoTabs zone is always null. Focus ring cannot be shown on info panel. |
| `Assets/Scripts/UI/CharacterSelect/HoldToEmbarkController.cs` | PARTIAL | kHoldDuration = 1.5f. Mouse PointerDownEvent + InputManager.GetAction(Confirm) for hold. Progress ring created dynamically if not in UXML. SetHoldLock wired to FocusManager. RaiseEmbarkTriggered on completion. RaiseEmbarkHoldProgress each frame. Audio placeholder tones. BUT embark trigger event has no subscriber in CharacterSelectManager. |
| `Assets/Scripts/UI/CharacterSelect/CharSelectEnvironmentController.cs` | VERIFIED | _nebulaCache Dictionary<string,Texture2D>. _sharedPixelBuffer allocated once (line 179, only Color[] allocation). PreBakeAllNebulas() called in HandleScreenReady. HandleHeroChanged uses TryGetValue (zero allocation). OnDisable destroys all cached textures. Old _nebulaPixels field absent. |

---

## Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| CharSelectFocusManager | InputManager | GetActionDown(TargetPrev/TargetNext) | VERIFIED | InputManager.HasInstance guard + GetActionDown for both shoulder buttons in Update() |
| HoldToEmbarkController | CharSelectEvents | RaiseEmbarkHoldProgress + RaiseEmbarkTriggered | VERIFIED | RaiseEmbarkHoldProgress each frame during hold (line 216). RaiseEmbarkTriggered on completion (line 211). |
| CharSelectEnvironmentController | CharSelectEvents | OnHeroChanged subscription | VERIFIED | HandleHeroChanged subscribed in OnEnable, uses TryGetValue for zero-alloc texture lookup |
| CharacterSelectManager | CharSelectEvents | RaiseEmbarkTriggered (should receive) | NOT WIRED | CharacterSelectManager.OnEnable subscribes to OnNavigationRequested only. No subscription to OnEmbarkTriggered. Hold gesture cannot trigger the async embark sequence. |
| CharSelectFocusManager | CharacterSelect.uxml | Q() to 'info-panel-container' element | NOT WIRED | kInfoPanel = 'hero-info-panel' — element name mismatch. Q() returns null. InfoTabs zone always fails. |
| HeroStageController | InputManager | Right-stick axis for rotation | NOT WIRED | No right-stick polling implemented anywhere. Only mouse drag rotation via PointerMoveEvent. |

---

## Requirements Coverage

| Requirement | Source Plans | Description | Status | Evidence |
|-------------|-------------|-------------|--------|----------|
| CTRL-01 | 01, 02, 03 | All 6 buttons functional with mouse and gamepad | PARTIAL | Back/Prev/Next/Tab/Carousel click functional. Embark works via mouse click. Hold-to-embark gesture broken (no OnEmbarkTriggered subscriber). |
| CTRL-02 | 03 | Gamepad focus ring visible with clear highlight on focused element | PARTIAL | FocusManager structure correct, 3 of 4 zones work. InfoTabs zone always null due to element name mismatch. |
| CTRL-03 | 03 | Audio feedback wired for navigation clicks, hero switch, and embark confirmation | VERIFIED | Placeholder tones generated via AudioClip.Create. NavTick on zone change + hero nav. HeroSwitch on OnHeroChanged. Embark complete tone on hold finish. |
| CTRL-04 | 02 | Embark coroutine replaced with async/await + timeout + error feedback | VERIFIED | ExecuteEmbarkAsync with CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken).CancelAfter(10s). ShowErrorToast on failure. |
| CTRL-05 | 03 | Nebula texture generation pre-baked — zero per-switch Color[] allocation | VERIFIED | _nebulaCache + _sharedPixelBuffer. Single new Color[pixelCount] at init. TryGetValue in HandleHeroChanged. |
| CTRL-06 | 02 | All Q() queries cached — zero Q() calls in Update/hot paths | VERIFIED | All Q() in CacheReferences()/CacheUIReferences(). ShowSkeletonLoading has lazy Q() but runs before init completes (acceptable — only called once per init). |
| CTRL-07 | 02 | Panel exit-then-enter choreography implemented | VERIFIED | CharSelectUIUtils.SwitchTabContent removes tab-active from outgoing, adds tab-active + AnimatePanel on incoming. AnimatePanel schedules class removal for CSS transition. |
| CTRL-08 | 03 | Confirm overlay focus trap working for gamepad navigation | PARTIAL | SetHoldLock(true/false) correctly blocks OnNavigationMove and Update() shoulder polling during hold. BUT InfoTabs zone is broken so full zone navigation is partially impaired. |
| CTRL-09 | 01, 02 | Loading state feedback shown during GameDatabase initialization | VERIFIED | ShowSkeletonLoading() called at start of InitializeWhenReady(). HideSkeletonLoading() after data ready. skeleton-overlay hidden class toggled. RaiseLoadingStarted/Complete events fired. |

---

## Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| CharSelectFocusManager.cs | 29 | Wrong element name: `kInfoPanel = "hero-info-panel"` but UXML has `"info-panel-container"` | BLOCKER | InfoTabs focus zone always null. Debug.Assert fires in editor. Focus ring cannot highlight info panel. |
| CharacterSelectManager.cs | 126-131 | Missing `CharSelectEvents.OnEmbarkTriggered += TriggerEmbark` subscription | BLOCKER | Hold-to-embark gesture raises event nobody handles. Async embark flow never executes via hold. |
| CharacterSelectManager.cs | 289, 535 | `OnEmbarkClicked` direct click still registered in BindUI — conflicts with hold-to-embark design intent | WARNING | Mouse click bypasses hold-to-embark. User can click once without holding. Inconsistent UX between mouse and gamepad. |
| CharSelectFocusManager.cs, HoldToEmbarkController.cs | — | `.meta` files missing from disk | INFO | Unity will auto-generate on project reimport but files not tracked in git. |

---

## Human Verification Required

### 1. Gamepad Navigation Feel (3 of 4 zones)

**Test:** Connect gamepad, open character select, use D-pad to navigate between Back, Embark, and Carousel zones
**Expected:** Focus ring (gold border) moves between zones with each D-pad input; InfoTabs zone should be skipped or also highlight (pending fix)
**Why human:** CSS class toggle cannot be verified programmatically; visual ring appearance requires runtime observation

### 2. Audio Placeholder Tone Quality

**Test:** Navigate between zones, switch heroes, hold embark button
**Expected:** Distinct tones: soft tick on navigation, deeper tone on hero switch, rising drone during hold, burst on hold complete
**Why human:** AudioClip.Create sine waves require playback verification; tone clarity/volume subjective

### 3. Skeleton Loading Visibility

**Test:** Open character select screen for the first time while GameDatabase initializes
**Expected:** Skeleton overlay covers the UI during initialization, disappears when data loads
**Why human:** Timing of GameDatabase initialization varies; requires observing the visual state transition

---

## Gaps Summary

**2 blockers prevent the phase goal from being fully achieved:**

**Blocker 1 — Hold-to-embark not wired to execution:**
`HoldToEmbarkController` correctly implements the 1.5s hold gesture and raises `CharSelectEvents.RaiseEmbarkTriggered()` on completion. However, `CharacterSelectManager` never subscribes to `OnEmbarkTriggered`. The async save and scene transition (`TriggerEmbark()` → `ExecuteEmbarkAsync()`) is only reachable via direct mouse click on `btn-embark`. The signature interaction for the phase — hold-to-embark — does not execute the embark sequence. Fix: add `CharSelectEvents.OnEmbarkTriggered += TriggerEmbark` in `CharacterSelectManager.OnEnable` and the corresponding unsubscribe in `OnDisable`.

**Blocker 2 — InfoTabs focus zone broken:**
`CharSelectFocusManager` caches the info panel element using `kInfoPanel = "hero-info-panel"` but the UXML element is named `"info-panel-container"`. At runtime `_infoPanel` is always `null`. `MoveFocusToZone(FocusZone.InfoTabs)` adds `focus-ring-active` to nothing, and `Debug.Assert` fires. The visible focus ring highlight for the info tab area never appears. Fix: change `kInfoPanel` constant from `"hero-info-panel"` to `"info-panel-container"`.

**Non-blocker — Right-stick rotation absent:**
The plan truth "Right stick rotates 3D hero model regardless of current focus zone" has no implementation. `HeroStageController` has mouse drag rotation only. `CharSelectFocusManager` polls only L1/R1. InputManager does not expose a right-stick axis reader. This is a missing feature from the plan, not a broken one. Low priority compared to the two blockers.

---

_Verified: 2026-03-19T00:28:26Z_
_Verifier: Claude Sonnet 4.6 (gsd-verifier)_
