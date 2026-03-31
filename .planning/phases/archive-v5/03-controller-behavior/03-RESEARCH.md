# Phase 3: Controller Behavior - Research

**Researched:** 2026-03-18
**Domain:** Unity UI Toolkit gamepad navigation, async embark flow, zero-GC hero switching, audio feedback, layout restructure
**Confidence:** HIGH

## Summary

Phase 3 is the largest phase yet: it restructures the screen layout (symmetric -> rule-of-thirds), replaces the confirm popup with hold-to-embark, adds tabbed info panels with L1/R1 cycling, wires all gamepad navigation with visible focus ring, adds per-hero audio feedback, implements skeleton loading states, and eliminates all GC allocations from the hero switch path. The existing codebase is well-structured for this work -- CharSelectEvents provides decoupled communication, InputManager wraps the Input System with gamepad detection, and the sub-controller pattern allows incremental modification.

The biggest technical risks are: (1) UI Toolkit's focus system has known limitations for custom gamepad navigation -- the engine's default tab-order traversal does not match the zone-based snap navigation required, so a custom `CharSelectFocusManager` must intercept NavigationMoveEvents and manually call `element.Focus()` on the correct targets; (2) the hold-to-embark requires a custom radial/linear progress VisualElement driven by continuous PointerDown tracking or InputManager.GetAction polling in Update; (3) the nebula texture pre-bake for zero-GC must move from per-switch generation to a one-time-per-hero cached dictionary of Texture2D.

**Primary recommendation:** Build a dedicated `CharSelectFocusManager` MonoBehaviour that owns all gamepad navigation logic (zone traversal, focus ring display, L1/R1 tab switching) and a `HoldToEmbarkController` that replaces the confirm overlay entirely. Restructure UXML to rule-of-thirds layout with tabbed info system before wiring behavior.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Screen layout: Rule-of-thirds composition -- hero 3D stage 50% left, info panel right half with tabbed sections, embark bottom-right, carousel at screen bottom, back button top-left
- Info panel: L1/R1 tabbed sections (Overview, Abilities, Lore) -- not all info at once
- Panel materials: Veil-torn dark fantasy (obsidian/iron borders, per-hero accent glow) replacing glass-morphism
- Gamepad navigation: Linear snap with D-pad zones, L1/R1 hero switch + tab cycle, right stick hero rotation, hold A for 1.5s embark
- Embark flow: Hold-to-confirm replacing click->popup. Confirm overlay and btn-confirm/btn-cancel REMOVED from UXML
- Audio: Per-hero themed SFX on switch, navigation ticks, embark tension drone, placeholder tones for missing assets
- Loading states: Skeleton shimmer during GameDatabase init, error toast on embark failure with 10s timeout
- Performance: All Q() cached, zero Q() in Update, exit-then-enter panel choreography, pre-baked nebula textures, cached WaitForSeconds, no LINQ
- Code quality: XML doc comments on all public methods, VeilBreakers conventions, paired event subscribe/unsubscribe, tracked coroutines, zero warnings

### Claude's Discretion
- Tab switching animation technique (slide vs crossfade)
- Skeleton shimmer implementation technique (USS animation vs C# coroutine)
- Hold-to-confirm progress visual shape (circular, linear bar, or hex fill)
- Monster positioning in shared 3D stage
- Toast notification styling and animation
- Focus ring exact visual treatment (glow intensity, animation)

### Deferred Ideas (OUT OF SCOPE)
- PrimeTween orchestrated animations -- Phase 4
- Per-hero URP post-processing profiles -- Phase 4
- Per-hero ambient music crossfade -- Phase 4
- Cinematic embark sequence -- Phase 4
- Title screen transitions -- Phase 5
- Title screen audio-reactive logo pulse -- Phase 5
- Per-hero bespoke 3D environments -- v2
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| CTRL-01 | All 6 buttons functional (Back, Prev, Next, Embark, Confirm, Cancel) with both mouse and gamepad | **Note:** Confirm/Cancel buttons are REMOVED per CONTEXT.md. Embark becomes hold-to-confirm. Back/Prev/Next remain. New: L1/R1 tab switch buttons, carousel card click. Gamepad via NavigationMoveEvent + custom focus manager |
| CTRL-02 | Gamepad focus ring visible with clear highlight on focused element | USS `:focus` pseudo-class + custom `focused-zone` class toggled by CharSelectFocusManager. Border-based ring (box-shadow not available in UI Toolkit) |
| CTRL-03 | Audio feedback wired for navigation clicks, hero switch, and embark confirmation | AudioManager.PlayOneShot() with per-hero event paths from HeroDisplayConfig.selectionSFX. Placeholder Debug.Log already in place |
| CTRL-04 | Embark coroutine-Task bridge replaced with async/await + timeout + user-facing error feedback | Unity 6.3 Awaitable + destroyCancellationToken available. Replace coroutine polling with async/await + CancellationTokenSource for 10s timeout. Toast notification on failure |
| CTRL-05 | Nebula texture generation pre-baked (eliminate per-switch Color[65536] allocation) | Cache Dictionary<string,Texture2D> keyed by heroId, generate all 4 on screen init, assign from cache on switch |
| CTRL-06 | All VisualElement Q() queries cached; zero Q() calls in Update/hot paths | Audit all controllers -- current code already caches most references. New tab system needs cached references. Environment controller ApplyParallax() is clean |
| CTRL-07 | Panel exit-then-enter choreography (slide-out before slide-in) | CharSelectUIUtils.AnimatePanel uses schedule.Execute with 50ms delay. Extend to: add `panel-exit` class -> wait transition duration -> swap content -> remove `panel-exit` + add `panel-enter` -> schedule remove `panel-enter` |
| CTRL-08 | Confirm overlay focus trap working for gamepad navigation | Confirm overlay REMOVED per decisions. Replaced by hold-to-embark. Focus trap concept moves to: during embark hold, block all other navigation |
| CTRL-09 | Loading state feedback shown during GameDatabase initialization (skeleton/shimmer) | USS @keyframes animation on placeholder elements with shimmer gradient sweep. Show skeleton during InitializeWhenReady, hide on data populated |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Unity Engine | 6000.3.6f1 (Unity 6.3) | Runtime, UI Toolkit, Input System | Project engine |
| UI Toolkit | Built-in | All UI rendering (UXML + USS) | Project standard, no UGUI |
| Input System | Built-in (new) | Gamepad/keyboard/mouse via VeilBreakersInputActions | Already wired through InputManager |
| Awaitable | Built-in (Unity 6+) | Async/await without UniTask dependency | Native, zero allocation, destroyCancellationToken support |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| AudioManager (custom) | N/A | Sound playback via PlayOneShot() | All audio feedback. Currently logs only (no FMOD) |
| ThemeManager (custom) | N/A | Brand color lookup via GetBrandColor/GetBrandGlow | Per-hero accent colors for focus ring, veil-torn borders |
| HeroDisplayConfig (SO) | N/A | Per-hero visual config (colors, SFX clips, camera) | Hero switch configuration source |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Awaitable | UniTask | UniTask is more feature-rich but adds dependency. Awaitable is built-in Unity 6+, sufficient for this use case |
| Custom focus manager | UI Toolkit default focus | Default tab-order doesn't support zone-based snap navigation. Custom is required |
| USS :focus border | Box-shadow outline | Box-shadow not available in UI Toolkit. Must use border properties for focus ring |

## Architecture Patterns

### UXML Restructure (Rule-of-Thirds)
```
character-select-root
  parallax-bg/                    # Existing background layers
  hero-stage/                     # 50% left -- shared hero+monster 3D viewport
    hero-render-target
  info-panel-container/           # Right half -- tabbed info
    tab-header-strip/             # L1/R1 tab buttons: Overview | Abilities | Lore
      tab-overview
      tab-abilities
      tab-lore
    tab-content/                  # Swappable content area
      tab-overview-content/       # Name, title, quote, path, role, synergy, stats, champion
      tab-abilities-content/      # 5 ability slots with descriptions
      tab-lore-content/           # Backstory, synergy detail, brands
  embark-area/                    # Bottom-right of info area
    btn-embark
    embark-progress-ring          # Hold-to-confirm radial/linear progress
  btn-back                        # Top-left
  carousel-strip/                 # Screen bottom
  toast-container/                # Error notification overlay (hidden by default)
```

### Pattern 1: Zone-Based Focus Navigation
**What:** Custom `CharSelectFocusManager` that divides the screen into navigable zones, intercepts NavigationMoveEvents at the root, and manually calls `element.Focus()` on the target element.
**When to use:** When UI Toolkit's default tab-order traversal doesn't match the desired gamepad UX.
**Example:**
```csharp
// Zone enum defines navigation graph
private enum FocusZone { Back, InfoTabs, Embark, Carousel }

// D-pad up/down moves between zones
private void OnNavigationMove(NavigationMoveEvent evt)
{
    if (evt.direction == NavigationMoveEvent.Direction.Down)
    {
        MoveFocusToZone(GetNextZone(_currentZone, Direction.Down));
        evt.StopPropagation();
        evt.PreventDefault();
    }
}

// Each zone knows its focusable elements
private void MoveFocusToZone(FocusZone zone)
{
    _currentZone = zone;
    VisualElement target = GetDefaultElementForZone(zone);
    target?.Focus();
    UpdateFocusRingVisual(target);
}
```

### Pattern 2: Hold-to-Confirm via Update Polling
**What:** A `HoldToEmbarkController` that tracks pointer-down state and gamepad Confirm hold, advances a progress timer, and triggers embark on completion.
**When to use:** Replace popup-based confirmation with pressure-sensitive commitment gesture.
**Example:**
```csharp
private float _holdProgress; // 0..1
private bool _isHolding;
private const float kHoldDuration = 1.5f;

private void Update()
{
    bool wantsHold = IsEmbarkHeld(); // check pointer + gamepad
    if (wantsHold && !_isEmbarking)
    {
        _holdProgress += Time.deltaTime / kHoldDuration;
        if (_holdProgress >= 1f)
        {
            _holdProgress = 1f;
            TriggerEmbark();
        }
        UpdateProgressVisual(_holdProgress);
    }
    else if (!wantsHold && _holdProgress > 0f)
    {
        _holdProgress = 0f;
        ResetProgressVisual();
    }
}
```

### Pattern 3: Async Embark with Timeout
**What:** Replace the coroutine-based Task polling in `CreateOrRotateNewGameSave` with native async/await using `destroyCancellationToken`.
**When to use:** Any async operation that currently uses coroutine polling of `Task.IsCompleted`.
**Example:**
```csharp
private async Awaitable ExecuteEmbarkAsync()
{
    var cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
    cts.CancelAfter(TimeSpan.FromSeconds(10)); // 10s timeout

    try
    {
        int slot = await SaveManager.Instance.GetBestNewGameSlotAsync();
        bool created = await SaveManager.Instance.CreateNewSaveAsync(slot, heroId, heroName, path);
        if (!created) throw new OperationCanceledException("Save creation failed");
        await SaveManager.Instance.SaveAsync(slot);
        // Transition to overworld
    }
    catch (OperationCanceledException)
    {
        ShowErrorToast("Embark failed. Please try again.");
    }
    finally
    {
        cts.Dispose();
    }
}
```

### Pattern 4: Pre-Baked Nebula Cache
**What:** Generate all 4 hero nebula textures during screen initialization, store in a Dictionary, and assign from cache on hero switch.
**When to use:** Eliminate the per-switch 256x256 Color[] allocation in CharSelectEnvironmentController.
**Example:**
```csharp
private readonly Dictionary<string, Texture2D> _nebulaCache = new Dictionary<string, Texture2D>(4);
private Color[] _sharedPixelBuffer; // allocated once

private void PreBakeAllNebulas(List<HeroData> heroes, HeroDisplayConfig[] configs)
{
    int pixelCount = _textureSize * _textureSize;
    _sharedPixelBuffer = new Color[pixelCount]; // one allocation

    for (int i = 0; i < heroes.Count; i++)
    {
        var tex = GenerateNebula(configs[i].secondaryColor, configs[i].primaryColor);
        _nebulaCache[heroes[i].hero_id] = tex;
    }
}

// On hero switch: zero-alloc texture assignment
private void HandleHeroChanged(int index, HeroData data, HeroDisplayConfig config)
{
    if (_nebulaCache.TryGetValue(data.hero_id, out var cached))
    {
        _parallaxDeep.style.backgroundImage = new StyleBackground(cached);
    }
}
```

### Pattern 5: Skeleton Shimmer Loading State
**What:** USS-driven shimmer animation on placeholder elements during async data loading.
**When to use:** While GameDatabase.InitializationTask is pending.
**Example USS:**
```css
.skeleton-shimmer {
    background-color: rgba(40, 40, 50, 0.6);
    overflow: hidden;
}

.skeleton-shimmer::after {
    /* Note: ::after not available in UI Toolkit.
       Use a child VisualElement with translate animation instead */
}

/* Alternative: Use USS transition on a child overlay element */
.shimmer-overlay {
    position: absolute;
    width: 60px;
    height: 100%;
    background-color: rgba(255, 255, 255, 0.08);
    translate: -60px 0;
    transition: translate 1.2s ease-in-out;
    rotate: 15deg;
}

.shimmer-overlay.shimmer-active {
    translate: 400px 0;
}
```

### Anti-Patterns to Avoid
- **Q() in Update/hot paths:** Every VisualElement query must be cached at init time. Zero exceptions.
- **LINQ anywhere in controller code:** Use manual iteration with for loops. No `.Where()`, `.Select()`, `.FirstOrDefault()`.
- **`new Color[]` per hero switch:** Pre-allocate once and reuse the buffer.
- **Direct `Input.GetKey`/`Input.mousePosition`:** All input through `InputManager` singleton.
- **Confirm overlay approach:** The old click->popup pattern is REMOVED. Do not recreate it.
- **`new WaitForSeconds()` in loops:** Cache all WaitForSeconds as static readonly or instance fields.
- **Subscribing without unsubscribing:** Every += must have a matching -= in OnDisable/OnDestroy.
- **Animating width/height/margin in USS:** Only GPU-safe properties (translate, scale, rotate, opacity, color).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Gamepad input detection | Custom device polling | `InputManager.IsGamepad` + `OnInputDeviceChanged` event | Already implemented, handles device switching |
| Brand color lookup | Inline Color switch blocks | `ThemeManager.GetBrandColor(brand)` / `HeroDisplayConfig.primaryColor` | Centralized, O(1) array lookup |
| Scene transitions | Custom fade coroutines | `ScreenTransition.Instance.Transition(callback)` | Existing singleton with fade overlay |
| Event communication | Direct controller references | `CharSelectEvents` static event bus | Decoupled pattern established in Phase 1 |
| Audio volume respect | Manual volume multiplication | `AudioManager.PlayOneShot(path)` | Respects AudioMixer channel volumes |
| Element name strings | Inline strings | `private const string kElementName` | Prevents silent null from typos, caught by Debug.Assert |

**Key insight:** The existing architecture has solid abstractions for input, audio, theming, events, and transitions. Phase 3 should wire these together, not rebuild them.

## Common Pitfalls

### Pitfall 1: UI Toolkit Focus Does Not Auto-Navigate on Gamepad
**What goes wrong:** Developers expect D-pad input to automatically move focus between elements like UGUI EventSystem.
**Why it happens:** UI Toolkit's focus system uses tab-index ordering, not spatial navigation. NavigationMoveEvent fires but does not automatically change focus.
**How to avoid:** Build a custom `CharSelectFocusManager` that handles NavigationMoveEvent, determines the correct target element based on a zone graph, and explicitly calls `element.Focus()`.
**Warning signs:** Focus appears stuck on one element, or jumps to unexpected elements across the visual tree.

### Pitfall 2: PointerDown Does Not Fire on Gamepad Submit Hold
**What goes wrong:** The hold-to-embark progress only works with mouse, not gamepad.
**Why it happens:** Gamepad A button triggers NavigationSubmitEvent (a single fire), not PointerDownEvent (continuous). There's no built-in "hold" detection for NavigationSubmit.
**How to avoid:** Use `InputManager.GetAction(GameAction.Confirm)` in Update to detect continuous hold state on gamepad. Use PointerDownEvent/PointerUpEvent for mouse. Both feed the same hold progress state.
**Warning signs:** Embark works on mouse click-hold but not on gamepad A-hold.

### Pitfall 3: Focus Ring Border Breaks Layout
**What goes wrong:** Adding border width for focus ring causes adjacent elements to shift/resize.
**Why it happens:** UI Toolkit borders are part of the box model. Adding 2px border on focus pushes content.
**How to avoid:** Always have a border present (e.g., `border-width: 2px; border-color: transparent;`) and only change the color on `:focus`. Or use an absolutely-positioned overlay element for the ring.
**Warning signs:** UI elements jitter/shift when focus moves between them.

### Pitfall 4: Nebula Texture Leaks on Scene Reload
**What goes wrong:** Texture2D objects from the nebula cache are not destroyed, accumulating on scene re-entry.
**Why it happens:** Dictionary values are UnityEngine.Objects that need explicit `Destroy()`.
**How to avoid:** In `OnDisable()`, iterate the cache and `Destroy()` each Texture2D, then clear the dictionary.
**Warning signs:** Rising texture memory in Profiler on repeated scene loads.

### Pitfall 5: Async/Await Continues After MonoBehaviour Destroyed
**What goes wrong:** Embark async method continues executing after CharacterSelectManager is destroyed (e.g., rapid scene switch), accessing destroyed singleton references.
**Why it happens:** C# Tasks don't respect Unity object lifecycle.
**How to avoid:** Use `destroyCancellationToken` as the base cancellation token. Create a linked token source that also has the 10s timeout. Catch `OperationCanceledException` and exit gracefully.
**Warning signs:** NullReferenceException in console after scene transitions, "destroyed MonoBehaviour" warnings.

### Pitfall 6: L1/R1 Shoulder Buttons Are in Gameplay Action Map
**What goes wrong:** L1/R1 (TargetPrev/TargetNext) don't fire when in the CharacterSelect scene.
**Why it happens:** The input bindings for shoulder buttons are in the Gameplay action map, which may not be enabled if the UI map is active.
**How to avoid:** Either add shoulder button bindings to the UI action map, or ensure both Gameplay and UI maps are enabled during CharacterSelect. Or read raw gamepad buttons: `Gamepad.current.leftShoulder.wasPressedThisFrame`.
**Warning signs:** L1/R1 work in combat but not on the character select screen.

### Pitfall 7: Tab Content Swap Causes Layout Flash
**What goes wrong:** When switching tabs, content briefly shows both old and new content, or there's a frame of empty space.
**Why it happens:** Removing old content and adding new content happens in the same frame, but USS transitions on the new content haven't started yet.
**How to avoid:** Use exit-then-enter choreography: (1) add `panel-exit` class to current tab, (2) after transition completes (via `schedule.Execute().ExecuteLater(transitionDuration)`), swap display style, (3) add `panel-enter` class to new tab.
**Warning signs:** Visual flash/pop during tab switches.

## Code Examples

### Custom Focus Ring via USS
```css
/* Base state: transparent border always present to prevent layout shift */
.focusable-zone {
    border-width: 2px;
    border-color: rgba(0, 0, 0, 0);
    border-radius: 4px;
    transition-property: border-color, scale;
    transition-duration: 0.15s, 0.15s;
    transition-timing-function: ease-out, ease-out;
}

/* Focus state: glow border with slight scale bump */
.focusable-zone:focus {
    border-color: rgba(255, 180, 60, 0.9);
    scale: 1.02 1.02;
}

/* Alternative: C#-driven focus class for zone-level highlighting */
.focus-ring-active {
    border-color: rgba(255, 180, 60, 0.9);
    scale: 1.02 1.02;
}
```

### Toast Notification Pattern
```csharp
/// <summary>
/// Shows an error toast that slides up from the bottom.
/// Auto-dismisses after duration or on user interaction.
/// </summary>
private void ShowErrorToast(string message)
{
    if (_toastContainer == null) return;

    _toastMessage.text = message;
    _toastContainer.RemoveFromClassList("toast-hidden");
    _toastContainer.AddToClassList("toast-visible");

    // Auto-dismiss after 5 seconds
    _toastContainer.schedule.Execute(() =>
    {
        DismissToast();
    }).ExecuteLater(5000);
}

private void DismissToast()
{
    _toastContainer?.RemoveFromClassList("toast-visible");
    _toastContainer?.AddToClassList("toast-hidden");
}
```

### Skeleton Shimmer via C# Schedule (Recommended for Claude's Discretion)
```csharp
/// <summary>
/// Starts a shimmer animation loop on a placeholder element.
/// Uses schedule.Execute for frame-independent timing.
/// </summary>
private void StartShimmer(VisualElement element)
{
    var overlay = new VisualElement();
    overlay.AddToClassList("shimmer-overlay");
    overlay.pickingMode = PickingMode.Ignore;
    element.Add(overlay);

    void DoShimmer()
    {
        float width = element.resolvedStyle.width;
        overlay.style.translate = new Translate(-60, 0);
        element.schedule.Execute(() =>
        {
            overlay.style.translate = new Translate(width + 60, 0);
        }).ExecuteLater(50);
    }

    element.schedule.Execute(DoShimmer).Every(1500);
}
```

### CharSelectEvents Extensions Needed
```csharp
// New events needed for Phase 3
public static event Action<int> OnTabChanged;           // tab index changed
public static event Action<float> OnEmbarkHoldProgress; // 0..1 hold progress
public static event Action<string> OnErrorOccurred;     // error message for toast
public static event Action OnLoadingStarted;            // skeleton shimmer on
public static event Action OnLoadingComplete;           // skeleton shimmer off

public static void RaiseTabChanged(int tabIndex) => OnTabChanged?.Invoke(tabIndex);
public static void RaiseEmbarkHoldProgress(float progress) => OnEmbarkHoldProgress?.Invoke(progress);
public static void RaiseErrorOccurred(string message) => OnErrorOccurred?.Invoke(message);
public static void RaiseLoadingStarted() => OnLoadingStarted?.Invoke();
public static void RaiseLoadingComplete() => OnLoadingComplete?.Invoke();
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Coroutine Task polling | async/await with Awaitable + destroyCancellationToken | Unity 2022.2+ (native), Unity 6 (Awaitable) | Eliminates polling loops, proper cancellation |
| transition: all in USS | Explicit GPU-safe property lists | Phase 2 (already done) | No width/height animation, no layout recalc |
| Confirm popup overlay | Hold-to-confirm gesture | Phase 3 (this phase) | Removes 2 buttons + overlay from UXML |
| Per-switch nebula generation | Pre-baked texture cache | Phase 3 (this phase) | Eliminates 65536-element Color[] per switch |
| Glass-morphism panels | Veil-torn dark fantasy materials | Phase 3 (this phase) | Dark fantasy aesthetic, per-hero accent glow |

**Deprecated/outdated:**
- `confirm-overlay`, `btn-confirm`, `btn-cancel`: Removed from UXML per user decision. Hold-to-embark replaces the popup flow.
- Coroutine Task polling pattern in `CreateOrRotateNewGameSave`: Replace with async/await.
- Symmetric dual-panel layout: Replace with rule-of-thirds composition.

## Open Questions

1. **L1/R1 Action Map Scope**
   - What we know: TargetPrev/TargetNext (L1/R1) are bound in the Gameplay action map. InputManager has separate EnableGameplay()/EnableUI() methods.
   - What's unclear: Whether both Gameplay and UI action maps are enabled simultaneously during CharacterSelect.
   - Recommendation: Check InputManager's default state on CharacterSelect entry. If only UI is enabled, add L1/R1 bindings to the UI action map or enable both maps. Alternatively, read directly from `Gamepad.current.leftShoulder` as a fallback.

2. **Audio Asset Availability**
   - What we know: STATE.md flags "Audio assets (navigation SFX, per-hero ambient tracks) do not exist yet" as a blocker concern.
   - What's unclear: Whether any AudioClip assets exist at all in the project, or if AudioManager is purely Debug.Log placeholder.
   - Recommendation: Use placeholder tones (simple synth beeps) as decided in CONTEXT.md. Generate via `AudioClip.Create()` at runtime or ship minimal .wav files. Never allow silent interactions.

3. **SaveManager Async API**
   - What we know: SaveManager methods (`GetBestNewGameSlotAsync`, `CreateNewSaveAsync`, `SaveAsync`) return `Task<T>`.
   - What's unclear: Whether these Tasks can be directly awaited in an async method on the main thread, or if they use background threads that require `await Task.Run(...)` bridging.
   - Recommendation: Test direct `await` first. If SaveManager uses `SemaphoreSlim` (confirmed in ARCHITECTURE.md), the Tasks should be awaitable. If they block, wrap in `Awaitable.MainThreadAsync()`.

## Sources

### Primary (HIGH confidence)
- Project source code: `Assets/Scripts/UI/CharacterSelect/*.cs` -- All 8 controllers fully analyzed
- Project source code: `Assets/Scripts/Core/InputManager.cs` -- GameAction enum, action maps, device detection
- Project source code: `Assets/Settings/VeilBreakersInput.inputactions` -- Binding structure (L1/R1 in Gameplay map)
- Project source code: `Assets/Scripts/Audio/AudioManager.cs` -- PlayOneShot API, Debug.Log placeholder
- Project source code: `Assets/Scripts/Data/HeroDisplayConfig.cs` -- selectionSFX, embarkSFX, ambientLoop AudioClip fields
- `.planning/codebase/CONVENTIONS.md` -- Naming patterns, event patterns, section organization
- `.planning/codebase/ARCHITECTURE.md` -- Singleton patterns, data flow, error handling

### Secondary (MEDIUM confidence)
- [Unity Manual: Focus system in UI Toolkit](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-focus-order.html) -- tabIndex, delegatesFocus, FocusController
- [Unity Manual: Navigation events](https://docs.unity3d.com/Manual/UIE-Navigation-Events.html) -- NavigationMoveEvent direction, NavigationSubmitEvent, NavigationCancelEvent
- [Unity Manual: Radial progress indicator](https://docs.unity3d.com/Manual/UIE-radial-progress.html) -- Mesh API custom VisualElement for progress ring
- [Unity API: MonoBehaviour.destroyCancellationToken](https://docs.unity3d.com/6000.2/Documentation/ScriptReference/MonoBehaviour-destroyCancellationToken.html) -- Cache before destroy, CancellationToken type
- [Unity Discussions: Focus ring styling](https://discussions.unity.com/t/correct-way-to-style-focus-ring-in-ui-toolkit/1682798) -- Border-based focus ring (box-shadow unavailable)

### Tertiary (LOW confidence)
- [Unity Discussions: NavigationMoveEvent not changing focus](https://discussions.unity.com/t/navigationmoveevent-not-changing-focus-automatically/930212) -- Confirms manual focus management needed
- [Unity Discussions: Button hold in UI Toolkit](https://discussions.unity.com/t/button-hold-in-ui-toolkit/937270) -- Community patterns for long-press

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- All libraries are already in the project and verified via source code analysis
- Architecture: HIGH -- Patterns derived from existing codebase patterns + official Unity docs
- Pitfalls: HIGH -- Derived from direct code analysis of current implementation + known UI Toolkit limitations
- Audio: MEDIUM -- AudioManager API is confirmed but actual asset availability is unclear
- Async/await migration: MEDIUM -- Unity 6 Awaitable is confirmed but SaveManager interop needs runtime testing

**Research date:** 2026-03-18
**Valid until:** 2026-04-18 (stable project, no fast-moving external dependencies)
