# CHARACTER SELECT BUG FIX PROMPT

Copy everything below this line and paste it into a new Claude Code terminal session.

---

You are fixing 28 bugs in the VeilBreakers3D character select screen. All bugs were identified by a deep scan. Fix them in priority order (Tier 1 first, then Tier 2, then Tier 3). Commit after each tier is complete.

**Project:** Unity 3D (C#, UI Toolkit). Code style: `_privateField`, `kConstant`, `PascalProperty`, `OnEvent` prefix.
**Branch:** Work on the current branch. Commit with descriptive messages.

Read `Docs/CHARSELECT_DEEP_SCAN.md` for the full scan report with code snippets and context.

## TIER 1 — CRITICAL GAMEPLAY BLOCKERS (Fix First)

### FIX 1: `_isEmbarking` not reset on cancellation
**File:** `Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs`
**Lines:** ~694-703 (the `TriggerEmbark` method's try/catch)
**Fix:** Replace the separate catch blocks with a `finally` block:
```csharp
finally
{
    _isEmbarking = false;
}
```
Remove `_isEmbarking = false;` from the inner catch(Exception) block since `finally` covers it.

### FIX 2: Dual NavigationMoveEvent handlers cause double hero navigation
**File:** `Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs`
**Lines:** ~814-828 (`OnNavigationMove` method) and ~522 (RegisterCallback line)
**Fix:** Remove the `OnNavigationMove` method entirely. Remove the `RegisterCallback<NavigationMoveEvent>` line from `BindUI()` and the matching `UnregisterCallback` from `UnbindUI()`. The `CharSelectFocusManager` already handles all D-pad navigation including Left/Right hero switching in the Carousel zone via `CharSelectEvents.RaiseNavigationRequested`.

### FIX 3: Gamepad hold bypasses embark focus check
**File:** `Assets/Scripts/UI/CharacterSelect/HoldToEmbarkController.cs`
**Lines:** ~197 (Update method where `gamepadHold` is computed)
**Fix:** The `CharSelectFocusManager` needs to expose the current zone. Add a public property to `CharSelectFocusManager`:
```csharp
public int CurrentZoneIndex => (int)_currentZone;
```
Then in `HoldToEmbarkController`, gate the gamepad hold on the Embark zone (index 2):
```csharp
bool gamepadHold = InputManager.HasInstance
    && InputManager.Instance.GetAction(InputManager.GameAction.Confirm)
    && _focusManager != null && _focusManager.CurrentZoneIndex == 2; // FocusZone.Embark
```

### FIX 4: Duplicate callback registration on repeated OnScreenReady
**File:** `Assets/Scripts/UI/CharacterSelect/HoldToEmbarkController.cs`
**Lines:** ~154-156 (`HandleScreenReady` method)
**Fix:** Unregister before registering to prevent stacking:
```csharp
// Defensive unregister to prevent stacking on repeated OnScreenReady
_btnEmbark.UnregisterCallback<PointerDownEvent>(OnPointerDown);
_btnEmbark.UnregisterCallback<PointerUpEvent>(OnPointerUp);
_btnEmbark.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);

_btnEmbark.RegisterCallback<PointerDownEvent>(OnPointerDown);
_btnEmbark.RegisterCallback<PointerUpEvent>(OnPointerUp);
_btnEmbark.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
```

---

## TIER 2 — MEMORY LEAKS & VISUAL GLITCHES

### FIX 5: VolumeProfile SO leaked every scene load
**File:** `Assets/Scripts/UI/CharacterSelect/VolumeProfileTransitioner.cs`
**Lines:** ~45-69 (`AutoWireVolume`) and ~142-145 (`OnDestroy`)
**Fix:** Add a field `private VolumeProfile _runtimeSharedProfile;` to track the runtime-created profile. In `AutoWireVolume`, after `var profile = ScriptableObject.CreateInstance<VolumeProfile>();`, assign `_runtimeSharedProfile = profile;`. In `OnDestroy`, add:
```csharp
if (_runtimeSharedProfile != null) Destroy(_runtimeSharedProfile);
```

### FIX 6: Rim flicker tweens not stopped in CleanupStage
**File:** `Assets/Scripts/UI/CharacterSelect/HeroStageController.cs`
**Lines:** Find the `CleanupStage()` method
**Fix:** At the TOP of `CleanupStage()`, before any `Destroy()` calls, add:
```csharp
StopRimFlicker();
if (_lightLerpTween.isAlive) _lightLerpTween.Stop();
```
Also verify `StopRimFlicker` sets `_isRimFlickering = false` and stops `_rimFlickerTween`.

### FIX 7: Breathing animation stacking on carousel cards
**File:** `Assets/Scripts/UI/CharacterSelect/CarouselController.cs`
**Lines:** ~190 (`UpdateSelection` method where `ButtonVFXHelper.AddBreathing` is called)
**Fix:** Track the breathing schedule item. Add a field:
```csharp
private IVisualElementScheduledItem _activeBreathingItem;
```
Before calling `AddBreathing`, cancel the previous one:
```csharp
_activeBreathingItem?.Pause();
_activeBreathingItem = null;
```
Note: This requires `ButtonVFXHelper.AddBreathing` to return its `IVisualElementScheduledItem`. If it doesn't, modify it to do so, OR alternatively, remove the `AddBreathing` call from `UpdateSelection` and only call it once during `BuildCarousel` for the initially selected card.

### FIX 8: Entry sequence not tracked + duplicate heroStage parameter
**File:** `Assets/Scripts/UI/CharacterSelect/HeroThemeTransitioner.cs`
**Lines:** ~153 (the `BuildScreenEntrySequence` call in `HandleScreenReady`)
**Fix TWO things:**
1. Assign the result: `_activeSequence = _entryAnimator.BuildScreenEntrySequence(...)`;
2. Fix the duplicate parameter. Find what element should be `leftPanel` — likely the hero stage's parent or a different UI panel on the left side. If no distinct left panel exists, remove the `leftPanel` parameter from the animator and adjust `ScreenEntryAnimator.BuildScreenEntrySequence` accordingly. The hero stage should only get an opacity fade, NOT a translate(-300,0).

### FIX 9: OnTransitionComplete can fire multiple frames
**File:** `Assets/Scripts/UI/CharacterSelect/VeilTransitionController.cs`
**Lines:** ~203-206 (inside a tween callback)
**Fix:** Add a `private bool _hasInvokedComplete;` field. In the tween callback:
```csharp
if (val <= 0.01f && !ctrl._hasInvokedComplete)
{
    ctrl._hasInvokedComplete = true;
    ctrl.HideQuad();
    ctrl.OnTransitionComplete?.Invoke();
}
```
Reset `_hasInvokedComplete = false;` at the start of each new transition (e.g., in `PlayMaterialize` or `PlayDissolve`).

### FIX 10: OnCinematicComplete event never cleared
**File:** `Assets/Scripts/UI/CharacterSelect/EmbarkCinematicController.cs`
**Lines:** `OnDisable()` method
**Fix:** Add `OnCinematicComplete = null;` in `OnDisable()`.

### FIX 11: Stat cascade sequence not stopped on disable
**File:** `Assets/Scripts/UI/CharacterSelect/HeroStatsPanelController.cs`
**Lines:** `OnDisable()` method
**Fix:** Add before the event unsubscription:
```csharp
if (_statCascadeSequence.isAlive) _statCascadeSequence.Stop();
```

### FIX 12: Incomplete stat validation
**File:** `Assets/Scripts/Data/HeroData.cs` (find the `Validate()` method)
**Lines:** ~133-134 (only `strength` is clamped)
**Fix:** Clamp all 6 stats:
```csharp
if (base_stats != null)
{
    base_stats.strength = Mathf.Clamp(base_stats.strength, 1, 20);
    base_stats.dexterity = Mathf.Clamp(base_stats.dexterity, 1, 20);
    base_stats.constitution = Mathf.Clamp(base_stats.constitution, 1, 20);
    base_stats.intelligence = Mathf.Clamp(base_stats.intelligence, 1, 20);
    base_stats.wisdom = Mathf.Clamp(base_stats.wisdom, 1, 20);
    base_stats.charisma = Mathf.Clamp(base_stats.charisma, 1, 20);
}
```

---

## TIER 3 — POLISH & CLEANUP

### FIX 13: Remove dead `_holdDroneClip` allocation
**File:** `Assets/Scripts/UI/CharacterSelect/HoldToEmbarkController.cs`
**Lines:** ~56 (field declaration) and ~76 (generation in OnEnable) and ~94 (destruction in OnDisable)
**Fix:** Remove the `_holdDroneClip` field, its generation line (`_holdDroneClip = GenerateRisingTone(...)`), and its destruction line. Remove `GenerateRisingTone` method if it's only used here.

### FIX 14: Restore RenderSettings.ambientLight on scene exit
**File:** `Assets/Scripts/UI/CharacterSelect/HeroStageController.cs`
**Lines:** `OnEnable` and `OnDisable` (or `CleanupStage`)
**Fix:** Add field `private Color _originalAmbientLight;`. In `OnEnable`: `_originalAmbientLight = RenderSettings.ambientLight;`. In `CleanupStage` or `OnDisable`: `RenderSettings.ambientLight = _originalAmbientLight;`.

### FIX 15: ClearAll() safety
**File:** `Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs`
**Lines:** ~183 (`OnDestroy`)
**Fix:** Move `CharSelectEvents.ClearAll()` from `OnDestroy` to `OnSceneUnloaded` only, or remove it since individual `OnDisable` handlers already unsubscribe.

### FIX 16: Synergy label display logic
**File:** `Assets/Scripts/UI/CharacterSelect/HeroDataPanelController.cs`
**Lines:** ~110-115
**Fix:** Remove the overwrite. The overview tab should show short brand tags (e.g., "IRON / DREAD"). Only the lore tab should show `data.synergy_explanation`. Change:
```csharp
// Overview tab: show short brand list
string synergy = data.GetPrimaryBrand().ToString().ToUpper();
// Don't overwrite with synergy_explanation here
CharSelectUIUtils.SetLabel(_heroSynergy, synergy);
```
Keep the lore tab's `_heroSynergyDetail` using `data.synergy_explanation`.

### FIX 17: Sort modifies shared GameDatabase list
**File:** `Assets/Scripts/UI/CharacterSelect/CarouselController.cs`
**Lines:** ~64-65
**Fix:** Copy before sorting:
```csharp
var heroes = new List<HeroData>(GameDatabase.Instance.GetAllHeroes());
heroes.Sort((a, b) => string.Compare(a.hero_id, b.hero_id, StringComparison.Ordinal));
```

### FIX 18: Cinematic scheduled callback null guard
**File:** `Assets/Scripts/UI/CharacterSelect/EmbarkCinematicController.cs`
**Lines:** ~193-196
**Fix:**
```csharp
_cinematicNameLabel.schedule.Execute(() =>
{
    if (_cinematicNameLabel != null)
        _cinematicNameLabel.style.opacity = 1f;
}).ExecuteLater(10);
```

### FIX 19: Empty HandleScreenExiting dead code
**File:** `Assets/Scripts/UI/CharacterSelect/HeroStageController.cs`
**Lines:** Find `HandleScreenExiting` (empty method) and its subscription in `OnEnable`/`OnDisable`
**Fix:** Remove the method, remove `CharSelectEvents.OnScreenExiting += HandleScreenExiting;` from `OnEnable`, and remove the matching unsubscription from `OnDisable`.

### FIX 20: Screen center stale on resize
**File:** `Assets/Scripts/UI/CharacterSelect/OverlayController.cs`
**Lines:** ~207 (`UpdateParallax` method)
**Fix:** Always recompute screen center:
```csharp
_screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
```
Remove the `if (_screenCenter.x <= 0f)` guard that prevents recomputation.

### FIX 21: Dolly camera tween not tracked
**File:** `Assets/Scripts/UI/CharacterSelect/EmbarkCinematicController.cs`
**Lines:** ~273-275 (`DollyCamera` method)
**Fix:** Store the tween: `_dollyTween = Tween.Custom(...)`. Add field `private Tween _dollyTween;`. In `OnDisable`, add `if (_dollyTween.isAlive) _dollyTween.Stop();`.

### FIX 22: Default struct .Stop() calls
**File:** `Assets/Scripts/UI/CharacterSelect/EmbarkCinematicController.cs` and `OverlayController.cs`
**Fix:** Guard all `.Stop()` calls on PrimeTween Sequence/Tween structs with `.isAlive`:
```csharp
if (_cinematicSequence.isAlive) _cinematicSequence.Stop();
```

---

## VERIFICATION

After all fixes, verify:
1. No new compiler errors (`dotnet build` or Unity compilation)
2. The CharacterSelect scene still loads
3. Hero switching works with D-pad (no double-skip)
4. Embark button only activates when focused
5. Embark timeout doesn't lock the button permanently
6. No errors in console on scene exit/reload

Commit each tier separately with messages like:
- "fix(charselect): tier 1 critical bugs — embark lockout, dual nav handlers, gamepad hold check"
- "fix(charselect): tier 2 memory leaks and visual glitches — VolumeProfile leak, rim flicker, breathing stack"
- "fix(charselect): tier 3 polish — dead code removal, render settings restore, null guards"
