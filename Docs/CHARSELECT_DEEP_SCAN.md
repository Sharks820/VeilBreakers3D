# Character Select Screen - Deep Bug Scan Report

**Date:** 2026-03-21
**Scanned by:** Claude Opus 4.6 (ultrathink mode) + 4 parallel bug-hunter agents
**Files scanned:** 20 C# files + 1 UXML + 1 USS in `Assets/Scripts/UI/CharacterSelect/`

## Summary

| Severity | Count |
|----------|-------|
| CRITICAL | 3 |
| HIGH | 10 |
| MEDIUM | 10 |
| LOW | 5 |
| **Total** | **28** |

---

## CRITICAL (3)

### CS-C1. `_isEmbarking` Not Reset on Timeout/Cancellation (Embark Lockout)
**File:** `CharacterSelectManager.cs:694-697`
**Category:** Logic Error / Permanent State Lockout

```csharp
catch (OperationCanceledException)
{
    Debug.Log("[CharSelectManager] Embark cancelled (destroyed or timed out).");
    // _isEmbarking is NEVER reset here
}
```

If the save times out (10s) or `destroyCancellationToken` fires, `_isEmbarking` stays `true`. The embark button is permanently dead until `OnDisable` runs. If the timeout happens without a scene change, the user is stuck.

**Fix:** Add `_isEmbarking = false;` in the catch, or use `finally { _isEmbarking = false; }`.

---

### CS-C2. VolumeProfile ScriptableObject Leaked Every Scene Load
**File:** `VolumeProfileTransitioner.cs:45-69`
**Category:** Memory Leak (GPU)

```csharp
// AutoWireVolume creates a runtime VolumeProfile SO:
var profile = ScriptableObject.CreateInstance<VolumeProfile>();
_volume.sharedProfile = profile;

// Awake clones it:
_volume.profile = Instantiate(_volume.sharedProfile); // clone

// OnDestroy only destroys the clone:
if (_volume.profile != null) Destroy(_volume.profile);
// The original runtime SO is NEVER destroyed
```

The original `ScriptableObject.CreateInstance<VolumeProfile>()` leaks every scene reload, along with all its `VolumeComponent` children. This accumulates GPU memory.

**Fix:** Track the runtime-created profile and destroy it in `OnDestroy`.

---

### CS-C3. Dual NavigationMoveEvent Handlers Fight Over Same Root
**File:** `CharacterSelectManager.cs:522,814` + `CharSelectFocusManager.cs:156,230`
**Category:** Input Conflict / Double-Handling

Both the Manager and FocusManager register `NavigationMoveEvent` on the **same** `_root` VisualElement:
- **Manager** (line 814): Handles Left/Right for hero navigation. Does NOT call `PreventDefault()`.
- **FocusManager** (line 230): Handles all 4 directions for zone navigation. Calls both `StopPropagation()` AND `PreventDefault()`.

Execution order depends on registration timing. When in Carousel zone, FocusManager intercepts Left/Right for hero switching AND the Manager also processes Left/Right for `NavigatePrev/Next`. This causes **double hero navigation** (skipping a hero) or **conflicting index jumps**.

**Fix:** Remove the Left/Right handler from `CharacterSelectManager.OnNavigationMove` entirely -- FocusManager already handles hero switching in Carousel zone via `CharSelectEvents.RaiseNavigationRequested`.

---

## HIGH (10)

### CS-H1. Duplicate Callback Registration on Repeated OnScreenReady
**File:** `HoldToEmbarkController.cs:154-156`

`HandleScreenReady()` registers `PointerDownEvent`, `PointerUpEvent`, `PointerLeaveEvent` on `_btnEmbark` every time `OnScreenReady` fires. Callbacks stack on repeat events (hot reload, UI rebuild). `OnDisable` only unregisters one set.

**Fix:** Unregister-before-register pattern, or guard with `_isInitialized`.

---

### CS-H2. Gamepad Hold Bypasses Embark Button Focus Check
**File:** `HoldToEmbarkController.cs:197`

```csharp
bool gamepadHold = InputManager.HasInstance && InputManager.Instance.GetAction(InputManager.GameAction.Confirm);
bool wantsHold = mouseHold || gamepadHold;  // No focus check for gamepad
```

Holding A/Confirm while navigating the carousel or any other zone still accumulates embark hold progress. Player can accidentally trigger embark from any zone.

**Fix:** Gate gamepad hold on the FocusManager reporting Embark zone as active.

---

### CS-H3. OnCinematicComplete Event Never Cleared on Disable
**File:** `EmbarkCinematicController.cs:50-51`

If the component is disabled mid-cinematic, `_cinematicSequence.Stop()` prevents the completion callback from firing. Any subscriber that registered (like `CharacterSelectManager.ExecuteEmbarkAsync`'s `OnComplete` closure) remains attached forever.

**Fix:** Add `OnCinematicComplete = null;` in `OnDisable()`.

---

### CS-H4. Rim Flicker Tweens Continue After CleanupStage
**File:** `HeroStageController.cs:448-453`

```csharp
_rimFlickerTween = Tween.Custom(...)
    .OnComplete(() => DoFlickerCycle());  // Recursive chain
```

`CleanupStage()` destroys `_stageRoot.gameObject` but never calls `StopRimFlicker()` or `_lightLerpTween.Stop()`. The recursive tween chain continues firing on zombie objects for one frame.

**Fix:** Add `StopRimFlicker(); _lightLerpTween.Stop();` at top of `CleanupStage()`.

---

### CS-H5. Breathing Animation Stacking on Carousel Cards
**File:** `CarouselController.cs:190`

Every time `UpdateSelection()` selects a card, `ButtonVFXHelper.AddBreathing()` registers a NEW `schedule.Execute().Every(50)` handler. The old one is never cancelled. After N selections, N breathing schedulers fight over `style.scale`, causing jitter.

**Fix:** Store `IVisualElementScheduledItem` per card; cancel on deselect.

---

### CS-H6. Entry Sequence Not Tracked or Stoppable
**File:** `HeroThemeTransitioner.cs:153`

```csharp
var entrySequence = _entryAnimator.BuildScreenEntrySequence(...);
// Never assigned to _activeSequence!
```

If the user switches heroes during the entry animation, `_activeSequence.Stop()` won't stop the entry animation. Two concurrent sequences fight over the same elements.

**Fix:** Assign `_activeSequence = entrySequence;`.

---

### CS-H7. Duplicate heroStage Passed as leftPanel
**File:** `HeroThemeTransitioner.cs:153`

```csharp
var entrySequence = _entryAnimator.BuildScreenEntrySequence(
    _heroStage,    // heroStage
    _heroStage,    // leftPanel -- SAME ELEMENT!
    _infoPanel, _carousel, ...
```

`ScreenEntryAnimator` applies `translate(-300, 0)` to `leftPanel`, which IS the hero stage. The hero stage gets pushed 300px left AND gets two conflicting opacity tweens.

**Fix:** Pass the correct distinct element for `leftPanel`.

---

### CS-H8. OnTransitionComplete Can Multi-Fire
**File:** `VeilTransitionController.cs:203-206`

```csharp
if (val <= 0.01f) {
    ctrl.HideQuad();
    ctrl.OnTransitionComplete?.Invoke();  // Fires every frame val < 0.01
}
```

No `_hasInvoked` guard prevents the event from firing multiple frames in a row.

**Fix:** Add `bool _hasInvokedComplete` guard.

---

### CS-H9. Stat Bar Cascade Sequence Not Stopped on Disable
**File:** `HeroStatsPanelController.cs:OnDisable()`

`OnDisable` unsubscribes from events but never calls `_statCascadeSequence.Stop()`. Tween callbacks fire on stale VisualElements after disable.

**Fix:** Add `_statCascadeSequence.Stop();` to `OnDisable()`.

---

### CS-H10. Incomplete Stat Validation (Only Strength Clamped)
**File:** `HeroStatsPanelController.cs:93` via `HeroData.Validate():133-134`

`HeroData.Validate()` only clamps `strength` to 1-20. The other 5 stats (dex, con, int, wis, cha) are unclamped. If JSON data has values > 20, the bar shows 100% but the numeric label shows the raw value (e.g., "25"), creating a visual mismatch.

**Fix:** Clamp all 6 stats in `HeroData.Validate()`.

---

## MEDIUM (10)

### CS-M1. ClearAll() on OnDestroy Wipes All Static Event Subscribers
**File:** `CharacterSelectManager.cs:183` via `CharSelectEvents.ClearAll()`

`OnDestroy` calls `CharSelectEvents.ClearAll()` which nulls ALL static event delegates. If any other subscriber in the scene registered (e.g., from a DontDestroyOnLoad object), their subscriptions are silently destroyed.

**Fix:** Individual unsubscription in `OnDisable` is already correct. Remove `ClearAll()` from `OnDestroy` or make it only callable from scene-unload handler.

---

### CS-M2. Dolly Camera Tween Not Tracked for Cleanup
**File:** `EmbarkCinematicController.cs:273-275`

`Tween.Custom` in `DollyCamera` is fire-and-forget. Not added to `_cinematicSequence`. If `OnDisable` fires mid-cinematic, this tween keeps modifying Camera.fieldOfView on a potentially destroyed camera.

---

### CS-M3. Dead `_holdDroneClip` Allocation (Never Played)
**File:** `HoldToEmbarkController.cs:56,76`

`_holdDroneClip` is generated (66,150-sample float[] allocation) in every `OnEnable` but is never played anywhere.

**Fix:** Remove generation entirely, or implement hold drone audio.

---

### CS-M4. Audio Clips Regenerated Every OnEnable
**File:** `HoldToEmbarkController.cs:76-77`, `CharSelectFocusManager.cs:110-111`

Both controllers regenerate identical placeholder AudioClips on every `OnEnable`. For FocusManager: 800Hz + 440Hz tones. For HoldToEmbark: rising tone + completion tone.

**Fix:** Generate once (in `Awake` with guard), or share via static cache.

---

### CS-M5. Global RenderSettings.ambientLight Modified Without Restore
**File:** `HeroStageController.cs:428`

```csharp
RenderSettings.ambientLight = Color.Lerp(_srcAmbientColor, _dstAmbientColor, t);
```

Global render state is modified per-hero but never restored on scene exit. Pollutes the next scene's ambient lighting.

**Fix:** Cache original in `OnEnable`, restore in `OnDisable`.

---

### CS-M6. Dissolve Tween Race Condition
**File:** `HeroSwitchAnimator.cs:63,76`

Both `AnimateDissolveOut` and `AnimateDissolveIn` are called at sequence BUILD time (not insert time). Both tweens are created immediately and start animating `_DissolveThreshold`. Depending on PrimeTween's deferred-start behavior, they may conflict.

---

### CS-M7. Synergy Label Overwrites Brand Tag with Full Sentence
**File:** `HeroDataPanelController.cs:110-115`

Overview tab's "SYNERGY" label intended for a short tag like "IRON / DREAD" gets overwritten with `data.synergy_explanation` (a full paragraph). The lore tab shows the exact same text, making both redundant.

**Fix:** Keep overview as short brand tags; use explanation only in lore tab.

---

### CS-M8. BuildStatBarCascade is Public But Never Called
**File:** `HeroStatsPanelController.cs:117`

The animated stat bar cascade was implemented but `HandleHeroChanged` only calls `UpdateStatBars` (instant). The cascade animation is dead code.

---

### CS-M9. Screen Center Stale on Window Resize
**File:** `OverlayController.cs:207`

`_screenCenter` computed once, only refreshed if `x <= 0`. Window resize makes parallax drift off-center.

---

### CS-M10. Scheduled Callback Null Reference Race in Cinematic
**File:** `EmbarkCinematicController.cs:193-196`

```csharp
_cinematicNameLabel.schedule.Execute(() =>
{
    _cinematicNameLabel.style.opacity = 1f;  // May be null if disabled in <10ms
}).ExecuteLater(10);
```

**Fix:** Add null guard inside closure.

---

## LOW (5)

### CS-L1. Sort Modifies Shared GameDatabase List In-Place
**File:** `CarouselController.cs:64-65`

`heroes.Sort(...)` mutates `GameDatabase.Instance.GetAllHeroes()` directly if it returns a reference (not a copy).

**Fix:** `var heroes = new List<HeroData>(GameDatabase.Instance.GetAllHeroes());`

---

### CS-L2. Glitch Text Reads Label Text at Build Time, Not Execution Time
**File:** `HeroSwitchAnimator.cs:93-103`

If labels haven't been updated to the new hero's name when the sequence is built, the glitch reveals the old hero's name.

---

### CS-L3. Empty HandleScreenExiting (Dead Code)
**File:** `HeroStageController.cs:545-548`

Subscribed to `OnScreenExiting` but handler body is empty. Wastes an event invocation slot.

---

### CS-L4. Gamepad Rotation Uses MousePosition as Stick Proxy
**File:** `HeroStageController.cs:512-517`

Compares `MousePosition.x` against `Screen.width * 0.6f` as a dead-zone heuristic. Produces only 3 discrete values instead of smooth analog rotation.

---

### CS-L5. Brands Detail Shows Only Primary Brand
**File:** `HeroDataPanelController.cs:158-159`

The "BRANDS" section in the lore tab shows a single brand name. Users expect matchup detail or at least "Primary: IRON / Secondary: DREAD".

---

## Priority Fix Order

### Tier 1: Fix Today (blocks normal gameplay)
1. **CS-C1** - `_isEmbarking` reset (1-line fix, embark lockout)
2. **CS-C3** - Remove duplicate NavigationMoveEvent handler (double hero skip)
3. **CS-H2** - Gamepad hold focus check (accidental embark from wrong zone)
4. **CS-H1** - Callback stacking on repeated OnScreenReady

### Tier 2: Fix This Sprint (causes visual glitches or leaks)
5. **CS-C2** - VolumeProfile leak (GPU memory accumulates)
6. **CS-H4** - Rim flicker cleanup (post-destroy exceptions)
7. **CS-H5** - Breathing animation stacking (carousel jitter)
8. **CS-H6** + **CS-H7** - Entry sequence tracking + correct leftPanel element
9. **CS-H8** - OnTransitionComplete multi-fire guard
10. **CS-H9** - Stat cascade sequence cleanup on disable

### Tier 3: Polish (dead code, minor UX)
11. **CS-M3** - Remove dead _holdDroneClip
12. **CS-M5** - Restore RenderSettings on exit
13. **CS-M7** - Fix synergy label display logic
14. **CS-M8** - Wire up or remove BuildStatBarCascade
15. Remaining MEDIUM and LOW items
