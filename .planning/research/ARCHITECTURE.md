# Architecture Research

**Domain:** Unity 6 RPG Character Select Screen (UI Toolkit)
**Researched:** 2026-02-21
**Confidence:** HIGH

## System Overview

```
+-------------------------------------------------------------------+
|                    CHARACTER SELECT SCREEN                          |
+-------------------------------------------------------------------+
|                                                                    |
|  +-----------------------+    +-----------------------------+      |
|  | CharacterSelectManager|    |     CharSelectEvents        |      |
|  | (Orchestrator)        |--->|     (Scoped Event Bus)      |      |
|  +-----------+-----------+    +----+--------+--------+------+      |
|              |                     |        |        |             |
|  +-----------v-----------+         v        v        v             |
|  |  Data Loading         |    +---------+ +------+ +----------+   |
|  |  GameDatabase ------->|    |Carousel | |Hero  | |Hero      |   |
|  |  HeroDisplayConfig    |    |Ctrl     | |Data  | |Stats     |   |
|  +---+---+---+-----------+    |         | |Panel | |Panel     |   |
|      |   |   |                +---------+ +------+ +----------+   |
|      |   |   |                     |        |        |            |
|      v   v   v                     v        v        v            |
|  +--------+ +--------+ +-------+  +----------------------------+  |
|  |HeroStage| |Environ | |Trans. |  |     UXML / USS             |  |
|  |Ctrl     | |Ctrl    | |Ctrl   |  |     (Visual Layer)         |  |
|  |(3D/RT)  | |(BG/FX) | |(stub) |  +----------------------------+  |
|  +----+----+ +---+----+ +---+---+                                  |
|       |          |           |                                     |
+-------+----------+-----------+-------------------------------------+
        |          |
   +----v----+ +---v---+
   |Preview  | |Parallax|    <-- 3D World / GPU
   |Camera   | |Input   |
   |RT 1024x | |Legacy! |
   +---------+ +--------+

EXTERNAL INTEGRATIONS:
  GameDatabase (Singleton) -- JSON hero/monster/skill data
  SaveManager (Singleton)  -- Save creation on embark
  ThemeManager (Singleton)  -- Per-hero USS theme classes
  ScreenTransition (Singleton) -- Fade overlay for scene changes
  EventBus (Global Static) -- NOT used by CharSelect (uses CharSelectEvents instead)
```

### Component Responsibilities

| Component | Responsibility | Communicates With |
|-----------|----------------|-------------------|
| **CharacterSelectManager** | Orchestrator: loads data, wires buttons, manages hero navigation index, drives embark flow, applies theme classes | CharSelectEvents (raises all events), GameDatabase, SaveManager, ScreenTransition |
| **CharSelectEvents** | Scoped static event bus for character select only. 8 events with null-safe raise helpers and ClearAll() for cleanup | All controllers subscribe; only Manager raises events |
| **CarouselController** | Generates hero cards dynamically, handles card click selection, updates hero index label | CharSelectEvents (subscribes OnScreenReady, OnHeroChanged), GameDatabase, Manager (direct ref for NavigateToHero) |
| **HeroDataPanelController** | Left panel: hero name, title, quote, path, role, synergy, starter stats, champion monster info | CharSelectEvents (subscribes OnHeroChanged), GameDatabase (monster lookup) |
| **HeroStatsPanelController** | Right panel: D&D attribute bars (STR/DEX/CON/INT/WIS/CHA) with animated fills, abilities list | CharSelectEvents (subscribes OnHeroChanged), GameDatabase (skill lookup) |
| **HeroStageController** | 3D hero preview: RenderTexture pipeline, camera, 5-light rig, model swap, drag rotation, placeholder capsules | CharSelectEvents (subscribes OnHeroChanged, OnScreenExiting), HeroDisplayConfig |
| **CharSelectEnvironmentController** | Background parallax layers, procedural nebula generation, fog tinting per hero | CharSelectEvents (subscribes OnHeroChanged, OnScreenReady), uses legacy Input.mousePosition |
| **TransitionController** | STUB: Subscribes to OnHeroChanged but body is empty. Reserved for future veil tear effects | CharSelectEvents (subscribes OnHeroChanged) |
| **HeroDisplayConfig** | ScriptableObject: per-hero visual config (camera, lighting, colors, model prefabs, animations, audio, VFX, champion) | Read by Manager, HeroStageController, HeroDataPanelController |

## Recommended Architecture

### Current Pattern: Event-Driven Sub-Controller Composition

The existing architecture follows a sound pattern that should be preserved and refined:

**Orchestrator + Sub-Controllers + Scoped Events**

This is effectively a variant of **MVP (Model-View-Presenter)** adapted for Unity:
- **Model**: `HeroData` (from GameDatabase JSON) + `HeroDisplayConfig` (ScriptableObject)
- **View**: UXML layout + USS styles (the visual elements)
- **Presenter**: `CharacterSelectManager` as orchestrator, sub-controllers as focused presenters per UI region

**Why this pattern is correct for this project:**
1. Each controller owns one visual region -- no god-class problem
2. The scoped `CharSelectEvents` bus prevents pollution of the global `EventBus`
3. Controllers subscribe/unsubscribe in OnEnable/OnDisable -- proper lifecycle
4. Manager raises events, controllers react -- unidirectional data flow
5. UXML defines structure, USS defines style, C# manages behavior -- clean separation

**Confidence: HIGH** -- This aligns with Unity's official MVC/MVP guidance for UI Toolkit and matches the established codebase patterns.

### Architecture Adjustments for Rebuild

The pattern is right. The problems are in execution detail, not architecture. Specific fixes needed:

1. **TransitionController is a dead stub** -- Either fill it with the coordination logic it was designed for (centralized panel slide-in/out sequencing) or delete it. Currently it wastes a MonoBehaviour and an event subscription doing nothing.

2. **Panel controllers duplicate animation logic** -- Both `HeroDataPanelController.AnimatePanel()` and `HeroStatsPanelController.AnimatePanel()` do identical class-toggle-with-schedule patterns. This should be centralized in TransitionController (its stated purpose) or extracted to a shared utility.

3. **CarouselController has a direct reference to Manager** -- It holds `[SerializeField] private CharacterSelectManager _manager` for `NavigateToHero()` calls. This breaks the event-driven pattern. Instead, CarouselController should raise a navigation request event, and Manager should handle it.

4. **Environment controller uses legacy Input API** -- `Input.mousePosition` on line 88 violates the project constraint of routing all input through `InputManager`. Must use the new Input System.

5. **Nebula generation is expensive and blocks main thread** -- `GenerateNebula()` allocates a `Color[]` array of 65,536 elements every hero switch. This should either be pre-baked per hero or run on a background thread using `Unity.Jobs`.

## Data Flow

### Hero Selection Flow (Primary)

```
User clicks Nav Arrow / Carousel Card / Gamepad
    |
    v
CharacterSelectManager.NavigateToHero(index)
    |
    +-- Validates: not transitioning, valid index, different hero
    |
    +-- Sets _currentIndex, _isTransitioning = true
    |
    +-- ApplyThemeClass(heroId)   // Swaps USS theme on root
    |
    +-- CharSelectEvents.RaiseHeroChanged(index, heroData, config)
    |       |
    |       +-> CarouselController.HandleHeroChanged()
    |       |       Updates card selection highlight, hero index label
    |       |
    |       +-> HeroDataPanelController.HandleHeroChanged()
    |       |       Populates left panel text, looks up champion monster
    |       |       Triggers panel slide-in via USS class toggle
    |       |
    |       +-> HeroStatsPanelController.HandleHeroChanged()
    |       |       Updates stat bar widths, abilities list
    |       |       Triggers panel slide-in via USS class toggle
    |       |
    |       +-> HeroStageController.HandleHeroChanged()
    |       |       Destroys old model, instantiates new (or placeholder)
    |       |       Applies camera/lighting config from HeroDisplayConfig
    |       |
    |       +-> CharSelectEnvironmentController.HandleHeroChanged()
    |       |       Tints fog particles, generates procedural nebula texture
    |       |
    |       +-> TransitionController.HandleHeroChanged()
    |               (STUB: does nothing)
    |
    +-- CharSelectEvents.RaiseHeroDataLoaded(heroData)
    |       (Currently no subscribers for this event)
    |
    +-- CharSelectEvents.RaiseHeroSelected()
    |       (Currently no subscribers for this event)
    |
    +-- UpdateEmbarkText()   // Updates embark button text
    |
    +-- StartCoroutine(EndTransitionAfterDelay(0.15f))
            Sets _isTransitioning = false after 150ms
```

### Embark Flow (Secondary)

```
User clicks Embark button
    |
    v
ShowConfirmPopup()
    +-- Removes "hidden" class from confirm-overlay
    +-- RaiseEmbarkRequested()
    |
User clicks Confirm
    |
    v
ExecuteEmbark()
    +-- RaiseEmbarkConfirmed()
    +-- RaiseScreenExiting()
    |       +-> HeroStageController.HandleScreenExiting() (no-op)
    |
    +-- StartCoroutine(EmbarkSequence)
            |
            +-- CreateOrRotateNewGameSave(hero)
            |       +-- SaveManager.GetBestNewGameSlotAsync()  (busy-wait poll)
            |       +-- SaveManager.CreateNewSaveAsync()       (busy-wait poll)
            |       +-- SaveManager.SaveAsync()                (busy-wait poll)
            |
            +-- ScreenTransition.Transition(() => LoadScene("Overworld"))
                    OR direct SceneManager.LoadScene() if no transition
```

### Initialization Flow

```
Scene loads -> OnEnable()
    |
    +-- EnsureFullScreenLayout()     // Walk tree setting flexGrow
    +-- EnsureCriticalManagers()     // Create singletons if missing
    +-- StartCoroutine(InitializeWhenReady)
            |
            +-- Poll GameDatabase.IsReady (10s timeout)
            +-- LoadHeroData()           // Sort heroes, reorder configs
            +-- CacheUIReferences()      // Q<> all buttons/elements
            +-- BindUI()                 // Register click/nav callbacks
            +-- ApplyInitialState()      // Index 0, theme, raise events
            +-- RaiseScreenReady()
                    |
                    +-> CarouselController.HandleScreenReady()
                    |       BuildCarousel() -- creates all hero cards
                    |
                    +-> CharSelectEnvironmentController.HandleScreenReady()
                            Reset parallax position
```

### Data Sources

| Data | Source | Format | Access Pattern |
|------|--------|--------|----------------|
| Hero identity (name, stats, skills) | `GameDatabase.GetAllHeroes()` | JSON -> `HeroData` | Loaded once at init, sorted by hero_id |
| Hero visuals (camera, lighting, colors) | `HeroDisplayConfig` ScriptableObjects | Asset | Serialized array on Manager, reordered to match hero sort |
| Monster info (champion display) | `GameDatabase.GetMonster(id)` | JSON -> `MonsterData` | Looked up per hero switch |
| Skill names | `GameDatabase.GetSkill(id)` | JSON -> `SkillData` | Looked up per hero switch |
| Save slot | `SaveManager.GetBestNewGameSlotAsync()` | Task<int> | Called once on embark, busy-wait polled |

## Architectural Patterns

### Pattern 1: Scoped Event Bus (CharSelectEvents)

**What:** A static class with typed events scoped to one screen, separate from the global EventBus. Events are null-safe to raise. `ClearAll()` nulls all delegates on scene exit.

**When to use:** Any screen with 3+ controllers that need to react to shared state changes without coupling to each other.

**Trade-offs:**
- PRO: Prevents global event namespace pollution
- PRO: ClearAll() prevents cross-scene leaks
- CON: Static events can still leak if OnDisable is not called (e.g., disabled GameObject)
- CON: No event ordering guarantees

**Example (current, correct):**
```csharp
// Controller subscribes
private void OnEnable()
{
    CharSelectEvents.OnHeroChanged += HandleHeroChanged;
}
private void OnDisable()
{
    CharSelectEvents.OnHeroChanged -= HandleHeroChanged;
}

// Manager raises
CharSelectEvents.RaiseHeroChanged(index, heroData, config);
```

### Pattern 2: USS Class-Toggle Animation

**What:** Animate UI elements by adding/removing USS classes that define transition properties. The USS handles interpolation; C# just toggles the trigger class.

**When to use:** All panel transitions, hero selection highlighting, confirm popup show/hide.

**Trade-offs:**
- PRO: Animation logic lives in USS, not C# -- designers can tune without recompiling
- PRO: Leverages GPU-accelerated USS transitions
- CON: Same-frame class add after element creation skips the transition (must use schedule.Execute)
- CON: Limited to animatable USS properties (no arbitrary curves)

**Critical implementation detail:**
```csharp
// WRONG: Adding and removing in same scope skips transition
panel.AddToClassList("panel-hidden");
panel.RemoveFromClassList("panel-hidden"); // No animation!

// CORRECT: Defer the removal to next frame
panel.AddToClassList("panel-hidden");
panel.schedule.Execute(() => panel.RemoveFromClassList("panel-hidden")).ExecuteLater(50);
```

**USS side (required for transitions to work):**
```css
.glass-panel {
    transition-property: translate, opacity;
    transition-duration: 0.4s;
    transition-timing-function: ease-out-back;
    translate: 0 0;
    opacity: 1;
}

.panel-hidden {
    translate: -40px 0;
    opacity: 0;
}
```

**Confidence: HIGH** -- Verified against Unity 6 official USS transition documentation.

### Pattern 3: RenderTexture 3D Preview Pipeline

**What:** Dedicated camera renders hero model to a RenderTexture, which is displayed as a background-image on a VisualElement. Separate layer (31) isolates preview from game rendering.

**When to use:** Any 3D model display within UI Toolkit screens.

**Trade-offs:**
- PRO: Full 3D rendering with custom lighting in UI context
- PRO: Supports drag rotation and animations
- CON: Extra camera + RT = GPU cost (mitigated by being the only active scene content)
- CON: RT resolution must balance quality vs. memory (current 1024x1536 is reasonable)

**Current implementation is sound.** The 5-light rig with per-hero config is AAA-quality thinking. Key improvement: when real model prefabs replace placeholders, the animation system needs to drive Animator states from HeroDisplayConfig clips.

### Pattern 4: Coroutine-Task Bridge (Busy-Wait Polling)

**What:** Unity coroutines poll `Task.IsCompleted` in a while loop to bridge async/await with Unity's coroutine system.

**When to use:** When calling async methods (SaveManager) from MonoBehaviour contexts that must yield.

**Trade-offs:**
- PRO: Works without UniTask dependency
- CON: Busy-wait is wasteful (checks every frame)
- CON: No cancellation token support
- CON: Error handling is manual (`IsFaulted` checks)

**This is flagged as "[!] Revisit" in PROJECT.md.** For the rebuild, the existing pattern is functional and low-risk. A UniTask migration would be an improvement but is out of scope.

## Anti-Patterns to Avoid

### Anti-Pattern 1: Direct Manager Reference from Sub-Controllers

**What people do:** Serialize a direct reference from CarouselController to CharacterSelectManager and call `_manager.NavigateToHero(index)` directly.

**Why it's wrong:** Breaks the event-driven architecture. Creates a bidirectional coupling: Manager raises events to controllers, but one controller also calls back directly. This makes CarouselController untestable in isolation and creates a hidden dependency.

**Do this instead:** Add a navigation request event to CharSelectEvents:
```csharp
// In CharSelectEvents:
public static event Action<int> OnNavigationRequested;
public static void RaiseNavigationRequested(int index) => OnNavigationRequested?.Invoke(index);

// In CarouselController:
private void OnCardClicked(int index) => CharSelectEvents.RaiseNavigationRequested(index);

// In CharacterSelectManager:
CharSelectEvents.OnNavigationRequested += NavigateToHero;
```

### Anti-Pattern 2: Legacy Input API in New Input System Project

**What people do:** Use `Input.mousePosition` directly instead of going through InputManager.

**Why it's wrong:** Breaks gamepad support, violates the project's input routing constraint, and mixes two input APIs creating maintenance confusion.

**Do this instead:** Read pointer/mouse position through the new Input System:
```csharp
var pointer = Pointer.current;
if (pointer != null)
{
    Vector2 mousePos = pointer.position.ReadValue();
    // ...
}
```
Or better: have InputManager expose a `GetPointerPosition()` method.

### Anti-Pattern 3: Per-Switch Texture Generation on Main Thread

**What people do:** `CharSelectEnvironmentController.GenerateNebula()` allocates 65K Color array and runs nested loop with Perlin noise on every hero switch.

**Why it's wrong:** Allocates ~1MB per switch (GC pressure), blocks main thread during what should be a smooth transition, and the texture is low-res enough that pre-baking per hero would be trivial.

**Do this instead:** Pre-generate 4 nebula textures at screen init (or bake them as assets), then swap by reference. If dynamic generation is desired, use `Unity.Jobs` + `NativeArray` for zero-GC generation on a worker thread.

### Anti-Pattern 4: Duplicate Animation Logic Across Controllers

**What people do:** Both panel controllers have identical `AnimatePanel()` methods that toggle the same USS class pattern.

**Why it's wrong:** Violates DRY. If timing or approach changes, two files must be updated. TransitionController exists explicitly for this purpose but is an empty stub.

**Do this instead:** Either:
- (A) Have TransitionController coordinate all panel transitions centrally, or
- (B) Extract a static helper: `UIPanelAnimator.SlideIn(VisualElement panel, string hiddenClass, int delayMs)`

### Anti-Pattern 5: Raising Redundant Events

**What people do:** Manager raises `OnHeroChanged`, `OnHeroDataLoaded`, AND `OnHeroSelected` on every hero switch. Currently nothing subscribes to `OnHeroDataLoaded` or `OnHeroSelected`.

**Why it's wrong:** Three events that always fire together create confusion about which to subscribe to. Subscribers must guess which event carries the data they need.

**Do this instead:** `OnHeroChanged` already carries index + HeroData + HeroDisplayConfig. Remove `OnHeroDataLoaded` and `OnHeroSelected` unless they serve a genuinely distinct lifecycle purpose (e.g., data loaded vs. visual transition complete).

## Build Order Implications

Based on dependency analysis, the rebuild should follow this order:

### Phase 1: Infrastructure Cleanup (Foundation)

Fix the plumbing before the visuals. Everything downstream depends on this.

1. **Delete or fix TransitionController** -- It's dead code consuming an event subscription
2. **Fix CharSelectEnvironmentController Input.mousePosition** -- Replace with New Input System
3. **Add OnNavigationRequested event** -- Remove direct Manager reference from CarouselController
4. **Consolidate USS files** -- 4 duplicate/stale stylesheets (CharacterSelect.uss, CharacterSelectAAA.uss, CharacterSelect_Backup.uss, plus VeilBreakers.uss vs VeilBreakersUI.uss) must be resolved to one authoritative file per scope
5. **Remove unused events** -- Clean up OnHeroDataLoaded, OnHeroSelected if truly unused

**Rationale:** Every subsequent phase will touch these controllers and styles. Fixing infrastructure first prevents cascading bugs.

### Phase 2: Layout and Structure (UXML/USS)

Fix the visual structure so content displays correctly.

1. **Fix text overlap and panel sizing** -- Known layout bugs in hero info panel
2. **Fix monster area display** -- Champion section positioning
3. **Fix hero stories and synergy/brands display** -- Content rendering issues
4. **Ensure full-screen layout works without the `EnsureFullScreenLayout()` hack** -- The walk-up-tree-setting-flexGrow pattern suggests the UXML root isn't configured correctly

**Rationale:** Layout must be correct before animations and effects can be tuned.

### Phase 3: Controllers (Behavior)

Fix functional issues in the controller logic.

1. **Fix all 6 button interactions** (Back, Prev, Next, Embark, Confirm, Cancel)
2. **Fix carousel card generation and selection** -- Verify dynamic card creation works with theme changes
3. **Fix embark flow** -- Save creation + scene transition sequence
4. **Performance: eliminate GenerateNebula allocation** -- Pre-bake or jobify
5. **Performance: cache all VisualElement queries** -- Ensure no repeated Q<> calls

**Rationale:** Behavior must work before visual polish. Performance fixes here prevent GC spikes during hero transitions.

### Phase 4: Visual Amplification (AAA Polish)

Add the visual quality layer.

1. **Implement USS transitions on all animated elements** -- Use `transition-property: translate, opacity, scale; transition-duration: 0.4s;`
2. **Implement coordinated panel transitions** -- Left panel slides from left, right panel slides from right, staggered timing
3. **Add embark button breathing glow animation** -- USS @keyframes or scheduled opacity cycling
4. **Enhance hero stage lighting** -- Per-hero lighting configs already exist in HeroDisplayConfig
5. **Add cinematic overlay effects** -- Scanlines, vignette, veil glow (UXML elements exist, need USS activation)
6. **Use `usageHints = UsageHints.DynamicTransform`** on all animated elements (partially done, needs audit)

**Rationale:** Visual polish is the final layer. Must not be attempted until layout and behavior are solid.

## Integration Points

### Existing Singleton Integrations

| Singleton | Integration Pattern | Usage in CharSelect | Considerations |
|-----------|---------------------|---------------------|----------------|
| **GameDatabase** | Poll `IsReady`, then call typed getters | `GetAllHeroes()`, `GetMonster()`, `GetSkill()` | 10s timeout if not ready; data is immutable after load |
| **SaveManager** | Async Task methods, busy-wait polled via coroutine | `GetBestNewGameSlotAsync()`, `CreateNewSaveAsync()`, `SaveAsync()` | Must handle `IsFaulted`/`IsCanceled`; slot rotation logic |
| **ThemeManager** | `ApplyThemeClass()` on root VisualElement | Per-hero USS class (theme-vex, theme-seraphina, etc.) | Manager applies directly via USS class list, not through ThemeManager singleton |
| **ScreenTransition** | `Transition(Action duringBlack)` | Scene exit fade to Overworld or MainMenu | Optional: falls back to direct `SceneManager.LoadScene` if not present |
| **InputManager** | Should wrap all input; currently bypassed | NOT USED by CharSelect (bug) | EnvironmentController uses `Input.mousePosition` directly |
| **AudioManager** | Not currently integrated | HeroDisplayConfig has `selectionSFX`, `embarkSFX`, `ambientLoop` fields | Audio integration is prepared in data but not wired in code |

### Internal Component Boundaries

| Boundary | Communication | Direction | Notes |
|----------|---------------|-----------|-------|
| Manager -> All Controllers | CharSelectEvents | Unidirectional (Raise) | Manager is sole event source (correct) |
| Controllers -> Manager | Should be CharSelectEvents | Currently mixed: events + direct ref | CarouselController has direct `_manager` reference (fix) |
| Controllers -> UI | VisualElement Q<> queries | Direct manipulation | All controllers cache references in OnEnable (correct) |
| Controllers -> GameDatabase | Singleton.Instance | Direct read-only | Multiple controllers query independently (acceptable for read-only) |
| Manager -> SaveManager | Async Task polling | Coroutine bridge | Busy-wait pattern is functional but wasteful |
| Manager -> Scene System | ScreenTransition or SceneManager | Direct call | Dual-path (with/without transition) adds complexity |

### USS/UXML Integration

| File | Purpose | Status |
|------|---------|--------|
| `CharacterSelect.uxml` | Screen layout structure | Active, well-structured |
| `CharacterSelect.uss` | Master stylesheet with theme variables | Active, AAA quality |
| `CharacterSelectAAA.uss` | Unknown -- likely a previous iteration | STALE: consolidate or delete |
| `CharacterSelect_Backup.uss` | Backup of previous styles | STALE: delete |
| `VeilBreakers.uss` | Global game styles | Potentially overlapping |
| `VeilBreakersUI.uss` | Global UI styles | Potentially overlapping with above |
| `VeilBreakersTheme.uss` | Theme variables (imported by CharacterSelect.uss) | Active, correct |

## Performance Optimization Checklist

Based on Unity's official UI Toolkit performance guidance:

| Optimization | Current Status | Action |
|--------------|----------------|--------|
| `UsageHints.DynamicTransform` on animated elements | Partial (CarouselController, EnvironmentController) | Audit all animated panels, add to hero-info-panel, stats-panel |
| `UsageHints.GroupTransform` on parallax parent | Missing | Add to parallax-bg container |
| Use `translate` instead of `left/top` for movement | Correct in EnvironmentController | Verify all panel animations use translate |
| No `Q<>` calls in Update/hot paths | Correct (all cached in OnEnable) | Maintain during rebuild |
| No allocations in Update | Violation: `GenerateNebula()` per hero switch | Pre-bake textures or use NativeArray |
| `DisplayStyle.None` for hidden elements | Used for confirm-overlay | Verify all hidden elements use this pattern |
| Minimize style class changes in large hierarchies | Theme class changes on root propagate to all children | Monitor perf; consider scoping theme to specific panels |

## Sources

- [Unity 6 UI Toolkit Performance Guide](https://docs.unity3d.com/6000.3/Documentation/Manual/best-practice-guides/ui-toolkit-for-advanced-unity-developers/optimizing-performance.html) -- HIGH confidence (official docs)
- [USS Transitions Documentation](https://docs.unity3d.com/Manual//UIE-Transitions.html) -- HIGH confidence (official docs)
- [Unity MVC/MVP Patterns Learn Course](https://learn.unity.com/course/design-patterns-unity-6/tutorial/build-a-modular-codebase-with-mvc-and-mvp-programming-patterns) -- HIGH confidence (official Unity Learn)
- [Unity MVVM Pattern Tutorial](https://learn.unity.com/tutorial/model-view-viewmodel-pattern) -- HIGH confidence (official Unity Learn)
- [UsageHints DynamicTransform Documentation](https://docs.unity3d.com/6000.0/Documentation/Manual/UIE-use-usage-hints-to-reduce-draw-calls-and-geometry-regeneration.html) -- HIGH confidence (official docs)
- [UI Toolkit Scalable & Performant Guide](https://unity.com/resources/scalable-performant-ui-uitoolkit-unity-6) -- MEDIUM confidence (Unity marketing/resource page)
- [Unity Discussions: USS Transition Issues](https://discussions.unity.com/t/issue-with-ui-toolkit-transition-animations/1513067) -- MEDIUM confidence (community verified)

---
*Architecture research for: VeilBreakers 3D Character Select Rebuild*
*Researched: 2026-02-21*
