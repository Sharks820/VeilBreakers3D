# Pitfalls Research

**Domain:** Unity 6 UI Toolkit -- Character Select Screen Rebuild & Game Flow
**Researched:** 2026-02-21
**Confidence:** HIGH (codebase-verified issues combined with official docs and community reports)

## Critical Pitfalls

### Pitfall 1: TemplateContainer Breaks Full-Screen Layout

**What goes wrong:**
Unity wraps every instantiated UXML in a `TemplateContainer` with `flex-grow: 0` by default. The UI renders at minimum content size instead of filling the viewport. The CharacterSelectManager already has a workaround (`EnsureFullScreenLayout()` walking up the visual tree setting `flex-grow: 1`), but this is fragile -- if a new UIDocument is added or the hierarchy changes, the screen collapses.

**Why it happens:**
Unity's design decision: TemplateContainers are layout-neutral by default. This contradicts developer expectation that a "full-screen UI" should fill the screen. Every Unity forum thread on this topic confirms it as a universal gotcha (Unity Discussions: "Why isn't TemplateContainer flex-grow 1 by default?").

**How to avoid:**
- Set `flex-grow: 1` on the root `<ui:VisualElement>` in UXML AND on the TemplateContainer via C# after instantiation.
- In the rebuilt CharacterSelect, set this in a single place during initialization rather than walking the tree.
- Add a USS rule: `TemplateContainer { flex-grow: 1; }` in the global stylesheet to prevent this class-wide.

**Warning signs:**
- UI appears as a thin strip at the top or collapses to zero height.
- `EnsureFullScreenLayout()` is called but UI still doesn't fill screen after scene reload.
- Adding a new child UXML template that doesn't inherit the fix.

**Phase to address:**
Phase 1 (Foundation / Cleanup) -- fix globally in the single canonical stylesheet before any new layout work begins.

---

### Pitfall 2: Duplicate USS Stylesheets Cause Silent Style Conflicts

**What goes wrong:**
The project has 6 USS files for what should be 2 concerns (global theme + character select). Active files: `CharacterSelect.uss` (referenced in UXML), `VeilBreakers.uss`, `VeilBreakersUI.uss`. Stale files: `CharacterSelectAAA.uss`, `CharacterSelect_Backup.uss`, `VeilBreakersTheme.uss`. Both global stylesheets define `*` selectors and `:root` CSS variables. When both load, specificity conflicts cause unpredictable style merging. The `VeilBreakersUI.uss` sets `transition-duration` on `*` which silently adds transitions to every element in the tree.

**Why it happens:**
Iterative development creates backup/variant files that accumulate. USS has CSS-like specificity rules but no warning when two stylesheets compete. Unity's USS specificity follows: inline > #id > .class > type, and conflicting `*` selectors resolve by load order, which is non-obvious.

**How to avoid:**
- Delete `CharacterSelectAAA.uss`, `CharacterSelect_Backup.uss`, and merge desired AAA easing values into the canonical `CharacterSelect.uss`.
- Consolidate `VeilBreakers.uss` and `VeilBreakersUI.uss` into a single `VeilBreakers.uss`. Remove the duplicate `*` and `:root` definitions.
- Delete `VeilBreakersTheme.uss` if it duplicates theme variables already in the canonical file.
- Adopt BEM naming convention per Unity's official recommendation to avoid complex selector hierarchies.

**Warning signs:**
- Styles change unexpectedly when a new stylesheet is imported.
- `transition-duration` on elements that shouldn't animate (caused by `* { transition-duration: ... }`).
- Editing one USS file has no visible effect because another file's rule takes precedence.

**Phase to address:**
Phase 1 (Cleanup) -- must happen BEFORE any style changes. Editing styles against the wrong file wastes all effort.

---

### Pitfall 3: Static Event Bus Memory Leaks and MissingReferenceException

**What goes wrong:**
`CharSelectEvents` is a static class with 8 `Action` events. If any MonoBehaviour subscriber is destroyed without unsubscribing (scene transition, rapid toggle, error during coroutine), the static delegate retains a reference to the dead object. Next event invocation throws `MissingReferenceException` or silently invokes a callback on a destroyed MonoBehaviour. The global `EventBus` (50+ events) has the same risk at larger scale.

**Why it happens:**
Static events outlive scene-scoped MonoBehaviours. The `ClearAll()` method exists but is only called in `CharacterSelectManager.OnDestroy()`. If the manager itself is destroyed before its sub-controllers, or if `ClearAll()` fires during an active coroutine callback chain, events silently stop working. Unity community reports confirm this is the most common memory leak pattern in Unity projects.

**How to avoid:**
- Every controller MUST unsubscribe in `OnDisable()` (not just `OnDestroy()`), which the current code already does correctly for sub-controllers. Verify this discipline is maintained during rebuild.
- Add a scene-unload hook: `SceneManager.sceneUnloaded += _ => CharSelectEvents.ClearAll()` as a safety net.
- For the global EventBus, add `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` to reset all static events on domain reload (important for Enter Play Mode settings).
- Consider moving to `EventChannel<T>` ScriptableObjects for new events to get automatic lifecycle management.

**Warning signs:**
- `MissingReferenceException` in console after scene transition.
- UI stops responding after navigating away and back to CharacterSelect.
- Memory profiler shows MonoBehaviour instances surviving scene unload.

**Phase to address:**
Phase 1 (Foundation) -- establish the event safety pattern before adding any new event subscribers.

---

### Pitfall 4: Coroutine-Task Bridge Swallows Exceptions and Blocks UX Feedback

**What goes wrong:**
`CharacterSelectManager.CreateOrRotateNewGameSave()` uses a busy-wait pattern (`while (!task.IsCompleted) yield return null`) to bridge `SaveManager` async Tasks to Unity coroutines. If the Task faults, the coroutine silently bails via `yield break` with only a `Debug.LogWarning`. The player sees no feedback -- the embark flow just stops. If the Task hangs (network timeout, file lock), the coroutine spins forever.

**Why it happens:**
Unity's coroutines cannot natively `await` C# Tasks. The common workaround is busy-wait polling, but this loses exception context and provides no timeout mechanism. Unity 6 provides `Awaitable` as a native async primitive, making this pattern unnecessary.

**How to avoid:**
- Replace the coroutine-Task bridge with Unity 6's `Awaitable` pattern or use `async void` with `try/catch` and proper exception surfacing.
- Add a timeout to any async operation (the initialization already has a 10s timeout for GameDatabase, but the save operations do not).
- Surface errors to the player via UI feedback (error toast, retry button) rather than silent `yield break`.
- If keeping coroutines, wrap the busy-wait in a helper: `yield return WaitForTask(task, timeoutSeconds: 5f, onError: ShowErrorUI)`.

**Warning signs:**
- Player clicks "Confirm" and nothing happens -- no scene transition, no error.
- Console shows `LogWarning` for failed save but player has no visible indication.
- Save file corruption goes undetected because the error was swallowed.

**Phase to address:**
Phase 2 (Core Interactions) -- when rebuilding the embark flow, replace the bridge pattern entirely.

---

### Pitfall 5: USS Transition Performance -- Layout Properties vs Transform Properties

**What goes wrong:**
Animating USS layout properties (`width`, `height`, `margin`, `padding`, `flex-grow`) causes full layout recalculation on every frame of the transition. This triggers geometry regeneration for the element AND all descendants, causing visible frame drops especially during hero switch transitions where multiple elements animate simultaneously. Additionally, the first time a transition fires on an element, Unity auto-adds `UsageHints` which causes a one-frame performance penalty as rendering data regenerates.

**Why it happens:**
USS transitions use the Yoga layout engine. Any property change that affects element size/position triggers a full layout pass. Unity's official docs explicitly warn: "value changes on these properties might cause layout recalculations, which can slow down the frame rate of your transition animation." Only `translate`, `rotate`, `scale`, and color properties bypass layout.

**How to avoid:**
- Only animate `translate`, `rotate`, `scale`, `opacity`, `color`, `background-color`, and `tint` in USS transitions.
- Never animate `width`, `height`, `margin`, `padding`, `left`, `top`, `right`, `bottom` via USS transitions. Use `translate` instead of `left`/`top`.
- Set `UsageHints.DynamicTransform` on elements BEFORE they enter the panel (at creation time, not first transition). The current `CharSelectEnvironmentController` does this correctly -- maintain this pattern.
- Set `UsageHints.DynamicColor` on elements whose background-color or opacity will animate.

**Warning signs:**
- FPS drops during hero transitions visible in FPSCounter.
- Profiler shows `UIR.UIRenderDevice` or `VisualElement.IncrementVersion` spikes during transitions.
- Smooth animations in editor but choppy in builds (editor hides layout cost).

**Phase to address:**
Phase 2 (Layout & Styling) -- audit all USS transitions to ensure only transform/color properties are animated.

---

### Pitfall 6: RenderTexture Lifecycle Leaks on Rapid Scene Transitions

**What goes wrong:**
`HeroStageController` creates a 1024x1536 MSAA RenderTexture, a Camera, 5 Lights, and a stage root in `OnEnable()`. If the component is rapidly toggled (e.g., during scene transition race, or navigating back/forward quickly), objects may leak because `CleanupStage()` runs in `OnDisable()` but `StopAllCoroutines()` may not catch all pending work. The static `_placeholderMaterial` survives across scenes and is never cleaned up, creating a permanent memory allocation.

**Why it happens:**
Unity's `OnEnable`/`OnDisable` lifecycle is synchronous but the content being managed (coroutines, RenderTextures, GameObjects) has asynchronous destruction. `Destroy()` is deferred to end-of-frame, so if `OnEnable` fires again before the deferred destroy executes, duplicate resources exist briefly. RenderTextures not explicitly `Release()`-d before `Destroy()` can leak GPU memory.

**How to avoid:**
- Always call `_renderTexture.Release()` before `Destroy(_renderTexture)` (the current code does this correctly -- maintain it).
- Add null guards in `InitializeStage()`: if `_renderTexture != null`, clean up first before creating new one.
- Clear the static `_placeholderMaterial` with `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` for domain reload safety.
- Add a guard `if (_stageRoot != null) return;` at the start of `InitializeStage()` to prevent double-initialization.
- Consider using `DestroyImmediate` in editor context to avoid deferred-destroy timing issues.

**Warning signs:**
- GPU memory steadily increases when toggling scenes (visible in Profiler > Memory).
- Multiple "PreviewCamera" or "CharSelectStage" GameObjects in hierarchy after scene reload.
- Console warnings about destroying objects during OnEnable.

**Phase to address:**
Phase 2 (3D Preview) -- when rebuilding the hero stage, implement guard-first initialization.

---

### Pitfall 7: Legacy Input API Mixed with New Input System

**What goes wrong:**
`CharSelectEnvironmentController.ApplyParallax()` uses `Input.mousePosition` (legacy API) while the rest of the codebase routes through `InputManager` wrapping `VeilBreakersInputActions` (New Input System). If the project disables the legacy Input Manager in Player Settings (which is the recommended setup for New Input System), the parallax code throws `InvalidOperationException`. Same issue in `ParallaxBackground.cs`.

**Why it happens:**
Legacy Input API is easier to write (`Input.mousePosition` vs setting up an action map). Developers use it for "quick" implementations and never migrate. The two systems coexist in Unity's "Both" input mode, but this doubles input processing overhead.

**How to avoid:**
- Route all mouse/pointer input through `InputManager` or read from the current `PointerEventData` in UI Toolkit callbacks.
- For parallax, use `Pointer.current.position.ReadValue()` from the New Input System, or better, use `PointerMoveEvent` from UI Toolkit itself since the parallax affects UI elements.
- Set Player Settings > Active Input Handling to "Input System Package (New)" to catch any remaining legacy calls as compile errors.

**Warning signs:**
- `Input.mousePosition` appears in any file via grep search.
- Console shows `InvalidOperationException: You are trying to read Input using the UnityEngine.Input class, but you have switched active input handling to Input System package`.
- Parallax stops working after Input System upgrade.

**Phase to address:**
Phase 1 (Cleanup) -- simple find-and-replace before any new code uses the wrong API.

---

### Pitfall 8: Q() Queries Return Null Silently When Element Names Change

**What goes wrong:**
All CharacterSelect controllers use `root.Q<T>("element-name")` to find UI elements. If a UXML element is renamed, moved, or its type changes, the query returns `null` silently. The code uses null-conditional (`?.`) to avoid crashes, which means functionality silently disappears. For example, if `"btn-embark"` is renamed to `"embark-button"` in UXML but not in `CharacterSelectManager.CacheUIReferences()`, the embark button stops working with no error.

**Why it happens:**
UXML names are strings with no compile-time validation. Refactoring UXML in UI Builder does not update C# references. Unity's `Q()` returns null rather than throwing, which is safe but hides bugs.

**How to avoid:**
- Define all element names as constants in a shared static class (e.g., `CharSelectElements.BtnEmbark = "btn-embark"`) and reference them from both UXML-generating code and query code.
- Add debug-only validation: after `CacheUIReferences()`, assert that critical elements are non-null with `Debug.Assert(_btnEmbark != null, "Missing btn-embark in UXML")`.
- Consider a `[PostProcessBuild]` or editor-time validation script that checks all Q() target names exist in the referenced UXML.
- When renaming elements in UI Builder, grep the codebase for the old name immediately.

**Warning signs:**
- A button or label stops appearing/working after UXML changes with no console error.
- `NullReferenceException` in a path that previously worked after a "simple" UXML rename.
- Unit test for "element exists" would catch this but no tests exist currently.

**Phase to address:**
Phase 1 (Foundation) -- define element name constants before any UXML restructuring begins.

---

### Pitfall 9: Bootstrap Load Order Causes Wrong Scene Flash

**What goes wrong:**
`GameBootstrap` initializes 13+ singleton managers then loads the first gameplay scene. If the Bootstrap scene isn't Scene 0 in Build Settings, or if a developer plays a scene directly in editor, the player sees a flash of the wrong scene (e.g., battle HUD) before the correct scene loads. The `CharacterSelectManager.EnsureCriticalManagers()` workaround creates managers without full initialization (no AudioConfig, no FPSCounter), causing subtle downstream failures.

**Why it happens:**
Unity always loads Scene 0 from Build Settings first. There is no way to intercept this before code runs. Editor play testing bypasses Bootstrap entirely. The `EnsureCriticalManagers()` pattern creates managers with default state rather than Bootstrap-configured state.

**How to avoid:**
- Use `EditorSceneManager.playModeStartScene` to force Bootstrap scene in editor (via an editor script).
- Add `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]` to a bootstrapper that ensures managers exist before any scene Awake/OnEnable fires.
- Remove `EnsureCriticalManagers()` from individual screens -- use the centralized bootstrap path exclusively.
- If direct scene entry must work for testing, make `EnsureCriticalManagers()` replicate the FULL Bootstrap initialization, not just the minimum singletons.

**Warning signs:**
- Flash of wrong UI when pressing Play in editor.
- `NullReferenceException` on AudioManager or ThemeManager when entering CharacterSelect directly.
- Manager state differs between "via Bootstrap" and "direct scene" paths.

**Phase to address:**
Phase 1 (Foundation) -- fix the bootstrap path before testing any other phase, since broken bootstrap undermines all testing.

---

## Technical Debt Patterns

Shortcuts that seem reasonable but create long-term problems.

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Inline styles via C# (`element.style.X = Y`) | Quick visual tweaks | Per-element memory overhead, bypasses USS specificity, invisible to UI Builder | Only for truly dynamic values (procedural colors, runtime-computed positions). Never for static layout. |
| `StopAllCoroutines()` in OnDisable | Prevents orphaned coroutines | Kills coroutines from OTHER systems if they run on this MonoBehaviour. Hides bugs where coroutine cleanup should be explicit. | Acceptable for leaf components. Never for managers that host shared coroutines. |
| `null` return from `Q()` without assertion | No crash on missing element | Silent feature loss, bugs hide for weeks | Never for critical interactive elements (buttons, overlays). Acceptable for optional decorative elements. |
| Busy-wait Task bridge (`while (!task.IsCompleted) yield return null`) | Works without UniTask dependency | No timeout, no error propagation, spins every frame, exception swallowed | Never in Unity 6 -- use `Awaitable` instead. |
| Static `_placeholderMaterial` | Avoids recreation per hero switch | Never garbage collected, survives domain reload, accumulates in editor | Acceptable only if cleaned up via `RuntimeInitializeOnLoadMethod`. |
| `WaitForSeconds` as transition guard | Simple delay-based transition lock | Not tied to actual USS transition completion. If transition duration changes in USS, code delay becomes wrong. | Prefer `TransitionEndEvent` callback from UI Toolkit. |

## Integration Gotchas

Common mistakes when connecting CharacterSelect subsystems.

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| UIDocument + MonoBehaviour | Querying `rootVisualElement` in `Awake()` before the panel is built. UIDocument may not have its UXML instantiated yet. | Query in `OnEnable()` or later. Use `RegisterCallback<AttachToPanelEvent>` for guaranteed readiness. |
| RenderTexture + UI Toolkit | Setting `backgroundImage = Background.FromRenderTexture(rt)` but not updating when RT is recreated. | Re-bind after any RenderTexture recreation. Always `Release()` before `Destroy()`. |
| SaveManager async + Coroutine | Assuming `Task.Result` is safe after `IsCompleted` check without checking `IsFaulted` first. | Check `IsFaulted` and `IsCanceled` before accessing `.Result`. Or use `await` with try/catch. |
| CharSelectEvents + SceneManager | Raising events after `SceneManager.LoadScene()` is called but before scene actually unloads. Subscribers may be in torn-down state. | Raise `OnScreenExiting` BEFORE calling `LoadScene`. Unsubscribe in `OnDisable` (which fires during unload). |
| ThemeManager + USS classes | Adding theme class (`theme-vex`) to root but child elements don't inherit because they have more specific USS selectors. | Use USS descendant selectors: `.theme-vex .hero-name { color: ... }`. Don't rely on inheritance for non-inherited properties (most USS properties don't inherit). |
| Gamepad navigation + ClickEvent | Registering only `ClickEvent` handlers on buttons. Gamepad submit action generates `NavigationSubmitEvent`, not `ClickEvent`. | Register both `ClickEvent` AND `NavigationSubmitEvent`, or use `Button.clicked` which handles both. The current code handles NavigationSubmitEvent at the root level, which is correct but coarse. |

## Performance Traps

Patterns that work at small scale but fail as complexity grows.

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Nebula texture generation on every hero switch | GC allocation of `Color[65536]` (256KB) per switch, main thread Perlin noise computation | Pre-generate textures at build time (one per hero theme) or use a compute shader. Cache in ScriptableObject. | Noticeable with > 4 heroes or on lower-end hardware. Currently 4 heroes = tolerable but wasteful. |
| `*` USS selector with transition-duration | Every element in the visual tree gets a transition, even labels and containers that never animate. Extra style merging cost scales with element count. | Remove `* { transition-duration }` from global USS. Apply transitions only to elements that need them via specific class selectors. | UI with > 100 elements starts showing measurable overhead in style resolution. |
| `GetAllHeroes()` returns new `List<HeroData>` per call | Allocation per call. Called by both `CharacterSelectManager` and `CarouselController` independently. | Cache the hero list once at initialization. Share via `CharacterSelectManager.HeroList` property. | Minor now (2 calls), but would compound if more controllers are added. |
| Per-frame `ApplyParallax()` in Update | Runs every frame even when character select is obscured by confirm overlay or during scene transition. | Guard with `if (!_isInitialized || _isTransitioning) return;` or use `enabled = false` to disable the component when parallax is not visible. | Wastes CPU on occluded UI. |
| Dynamic VisualElement creation in `BuildCarousel()` without pooling | Creates new elements every time carousel rebuilds. If `BuildCarousel()` is called multiple times (e.g., data reload), old elements are cleared but GC must collect them. | Build carousel once, update content via data binding. Use `_carouselStrip.Clear()` only when hero count actually changes. | Fine for 4 heroes. Would matter with dynamic hero roster. |

## Security Mistakes

Domain-specific security issues for this game.

| Mistake | Risk | Prevention |
|---------|------|------------|
| Hardcoded encryption salt in SaveFileHandler | Player can extract salt from binary, derive key, modify save files | Acceptable for single-player RPG. If competitive features added later, use device-specific salt component. |
| Save creation during embark has no integrity verification | If save write is interrupted (crash, power loss), atomic write may produce corrupt file | `SaveFileHandler` uses atomic writes (write-to-temp then rename). Verify this pattern is maintained during rebuild. |
| No validation of HeroDisplayConfig data | A corrupted or tampered ScriptableObject could inject unexpected values (extreme colors, negative scales) | Add `OnValidate()` to HeroDisplayConfig clamping values to sane ranges. |

## UX Pitfalls

Common user experience mistakes in character select screens.

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| No loading indicator during save creation | Player clicks Confirm, nothing happens for 0.5-2s, clicks again | Show a spinner or disable the Confirm button immediately. Re-enable on failure with error message. |
| Hard-coded 0.15s transition guard | If USS transition takes longer than 0.15s, user can spam-navigate mid-transition creating visual glitches | Listen for `TransitionEndEvent` instead of using `WaitForSeconds(0.15f)`. Or lock navigation until the USS transition actually completes. |
| No keyboard shortcut hints | Gamepad/keyboard users don't know they can use Left/Right to navigate or Enter to select | Show contextual input hints that update based on active input device ("A to Select" vs "Enter to Select"). |
| Confirm popup has no focus trap | Gamepad user can navigate "behind" the confirm popup to carousel buttons | When confirm overlay is visible, set focus to the confirm/cancel buttons and prevent focus from escaping the overlay. |
| Silent failure on embark | Save creation fails, player is stuck on character select with no feedback | Show an error toast: "Failed to create save. Please try again." with a retry option. |
| No visual feedback for hero switch | Hero data changes but there's no visual transition acknowledging the change | Add a brief cross-fade or slide animation on the info panel content during hero switch. |

## "Looks Done But Isn't" Checklist

Things that appear complete but are missing critical pieces.

- [ ] **Button bindings:** All 6 buttons (Prev, Next, Back, Embark, Confirm, Cancel) respond to BOTH mouse click AND gamepad/keyboard -- verify with controller plugged in.
- [ ] **Theme switching:** Verify all 4 hero themes visually differ AND that switching doesn't leave stale CSS classes on the root -- inspect with UI Debugger.
- [ ] **RenderTexture cleanup:** After navigating away from CharacterSelect and back 5 times, verify GPU memory hasn't grown -- check Profiler > Memory > RenderTexture.
- [ ] **Event unsubscription:** Set breakpoint in `CharSelectEvents.ClearAll()` and verify it fires exactly once per scene exit, not during active transitions.
- [ ] **Confirm overlay focus:** With gamepad, open confirm popup and try navigating Left/Right -- should NOT change hero behind popup.
- [ ] **Error feedback:** Disconnect save file access (read-only folder), attempt embark -- should show error, not silently fail.
- [ ] **Full-screen layout:** Test at 1920x1080, 2560x1440, and 1280x720 -- UI should fill screen at all resolutions without collapse or overflow.
- [ ] **Direct scene entry:** Enter CharacterSelect scene directly (not via Bootstrap) -- should initialize and function, not crash on null managers.
- [ ] **USS transitions:** Open Profiler during hero switch -- no frame drops below 55fps during transition animations.
- [ ] **Stale stylesheets:** Verify `CharacterSelectAAA.uss` and `*_Backup.*` are deleted and the canonical `CharacterSelect.uss` contains all desired styles.

## Recovery Strategies

When pitfalls occur despite prevention, how to recover.

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| TemplateContainer layout collapse | LOW | Add `TemplateContainer { flex-grow: 1; }` to global USS. One-line fix. |
| Duplicate USS causing wrong styles | MEDIUM | Diff all USS files, identify canonical rules, consolidate. Test every screen after merge. |
| Static event memory leak | MEDIUM | Add `ClearAll()` call in scene unload hook. Profile to find remaining leaks. May need to audit all 50+ EventBus subscribers. |
| Coroutine-Task bridge exception swallowed | LOW | Replace with `async/await` pattern. Add try/catch with UI error feedback. Single-method refactor. |
| USS transition animating layout property | MEDIUM | Audit all USS files for `transition-property` declarations. Replace layout animations with transform-based equivalents. May need UXML restructuring if layout-dependent animations were intentional. |
| RenderTexture leak | HIGH | Profile GPU memory, identify leak source. May require rearchitecting the stage lifecycle if rapid toggle is the cause. Add guard-first initialization. |
| Legacy Input API breaks | LOW | Global find-replace `Input.mousePosition` with InputManager equivalent. 2-3 files affected. |
| Q() returns null after UXML rename | LOW | Grep for the old element name, update C# references. Add Debug.Assert for critical elements. |
| Bootstrap flash | MEDIUM | Create editor script setting `playModeStartScene`. Add `RuntimeInitializeOnLoadMethod` bootstrap. Test both code paths. |

## Pitfall-to-Phase Mapping

How roadmap phases should address these pitfalls.

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| TemplateContainer layout | Phase 1: Foundation | UI fills viewport at 3 test resolutions |
| Duplicate USS files | Phase 1: Cleanup | Only 2 USS files remain: global + CharacterSelect |
| Static event leaks | Phase 1: Foundation | No `MissingReferenceException` after 10 scene transitions |
| Coroutine-Task bridge | Phase 2: Core Interactions | Embark flow uses async/await, error shows in UI |
| USS transition performance | Phase 2: Layout & Styling | Profiler shows < 1ms USS transition overhead per frame |
| RenderTexture lifecycle | Phase 2: 3D Preview | GPU memory stable after 10 hero switches |
| Legacy Input API | Phase 1: Cleanup | Zero `Input.mousePosition` calls in codebase |
| Q() null returns | Phase 1: Foundation | Debug.Assert passes for all 6 buttons + critical elements |
| Bootstrap load order | Phase 1: Foundation | No scene flash in editor, direct entry works |

## Sources

- [Unity Manual: USS Transitions](https://docs.unity3d.com/6000.2/Documentation/Manual/UIE-Transitions.html) -- HIGH confidence, official docs on transition performance
- [Unity Manual: Best practices for USS](https://docs.unity3d.com/Manual/UIE-USS-WritingStyleSheets.html) -- HIGH confidence, BEM methodology, selector performance
- [Unity Discussions: TemplateContainer flex-grow](https://discussions.unity.com/t/why-isnt-templatecontainer-flex-grow-1-by-default/728653) -- HIGH confidence, confirmed by multiple community reports and Unity staff responses
- [Unity Discussions: UI Toolkit Filters VRAM Leak](https://discussions.unity.com/t/ui-toolkit-filters-severe-vulkan-vram-leak/1672822) -- MEDIUM confidence, Vulkan-specific but relevant to RenderTexture concerns
- [Unity Discussions: Focus issue with UI Toolkit and New Input System](https://discussions.unity.com/t/focus-issue-with-ui-toolkit-and-new-input-system/869660) -- MEDIUM confidence, gamepad navigation issues
- [Unity Discussions: Lag spike when triggering transition first time](https://discussions.unity.com/t/lag-spike-when-triggering-a-transition-for-the-first-time/1579625) -- MEDIUM confidence, first-transition penalty confirmed
- [Unity Discussions: UIDocument inspector destroys runtime UI](https://discussions.unity.com/t/in-127759-uidocument-inspector-destroys-runtime-ui/1699889) -- MEDIUM confidence, UIDocument lifecycle gotcha
- [Unity Discussions: Q() returns null](https://discussions.unity.com/t/cannot-query-for-an-element-returns-null/880360) -- MEDIUM confidence, common developer pain point
- [Unity Discussions: Scene Bootstrapper Architecture](https://discussions.unity.com/t/scene-bootstrapper-architecture/831630) -- MEDIUM confidence, bootstrap pattern best practices
- [UniTask GitHub](https://github.com/Cysharp/UniTask) -- HIGH confidence, established async alternative for Unity
- Codebase analysis: `CharacterSelectManager.cs`, `CharSelectEvents.cs`, `CharSelectEnvironmentController.cs`, `HeroStageController.cs`, `CarouselController.cs` -- HIGH confidence, direct code review
- `.planning/codebase/CONCERNS.md` -- HIGH confidence, prior codebase audit

---
*Pitfalls research for: Unity 6 UI Toolkit Character Select Rebuild*
*Researched: 2026-02-21*
