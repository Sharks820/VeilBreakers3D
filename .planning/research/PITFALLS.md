# Domain Pitfalls: VeilBreakers v6.0 Bug Fixes, Code Quality Hardening & UI Rebuild

**Domain:** Unity 6 UI Toolkit game -- mass bug fixing, runtime texture generation, 3D model display, logging migration, singleton refactoring
**Researched:** 2026-03-30
**Overall confidence:** HIGH (verified against existing codebase, Unity 6 documentation, project history, and community reports)

---

## Critical Pitfalls

Mistakes that cause rewrites, cascading regressions, or data loss. Each of these has already manifested in this project's history or is highly likely given the v6.0 scope.

---

### CRIT-1: Mass Bug Fixing Without Isolation Causes Cascade Regressions

**What goes wrong:**
Fixing 73+ bugs across combat, capture, UI, and core systems in rapid succession introduces new bugs faster than old ones are resolved. A fix to `DamageCalculator` changes the synergy multiplier order, which breaks `BattleManager` combat flow, which causes `CaptureManager` to see impossible HP values, which crashes the capture QTE. Each "simple fix" touches shared state that other systems depend on. This project has already experienced this -- blind-editing caused regressions that cost entire sessions (documented in CLAUDE.md anti-regression protocol).

**Why it happens:**
The codebase has 128 C# scripts with deep cross-system coupling. `EventBus` has 65+ static event fields connecting 20+ files. `BattleManager` interacts with `DamageCalculator`, `SynergySystem`, `BrandSystem`, `StatusEffectManager`, `CombatAI`, and `CaptureManager` through a web of events and direct calls. Changing behavior in one system has non-obvious effects on others. Without test coverage (only 18 test files, RuntimeTests not runnable by Unity Test Runner), regressions are invisible until someone plays the game.

**Consequences:**
- Fix A breaks system B. Fix B breaks system C. Three sessions later, the codebase is worse than when you started.
- Loss of confidence in the codebase -- developers stop trusting that anything works.
- Phase deadlines slip as "simple" fixes spiral into multi-day debugging sessions.

**Prevention:**
1. **Fix in priority-tier batches, not all at once.** Phase A (5 critical bugs) ships and stabilizes before Phase B (11 high-priority bugs) begins. Never mix tiers.
2. **Read before every edit.** The CLAUDE.md anti-regression protocol exists because this exact problem happened. Reading a file costs ~500 tokens. Blind-editing then fixing regressions costs ~50,000.
3. **Compile-check after every 3-5 changes.** Do not accumulate 20 changes before testing. Catch breaks immediately while the cause is obvious.
4. **Max 2 attempts per approach.** If a fix attempt fails twice, stop. Re-read the context. Try a fundamentally different approach. Do not loop.
5. **Group fixes by system, not by severity.** Fix all `DamageCalculator` + `BattleManager` issues together, then all `CharSelect` issues together. Cross-system context is expensive to load and easy to lose.
6. **Each fix gets its own commit.** If a fix introduces a regression, `git revert` undoes exactly that fix and nothing else.

**Warning signs:**
- "I'll just fix this one more thing while I'm here" -- scope creep within a bug fix.
- Editing a file without reading it first.
- Fixing file B as a side-effect of fixing file A in the same edit.
- More than 5 files changed in a single commit labeled "bug fix."

**Detection:**
Track the ratio of bugs-fixed to bugs-introduced per session. If it drops below 3:1, the approach is wrong.

**Phase to address:** Phase A through Phase D. Every phase that touches bug fixes must follow isolation protocol.

---

### CRIT-2: Texture2D Memory Leaks from Runtime Gradient Generation

**What goes wrong:**
`UIGradientHelper` creates `Texture2D` objects at runtime for gradients and glow effects. These are native GPU resources -- they are NOT garbage-collected by C#. If the calling code does not explicitly call `Object.Destroy(texture)` when the VisualElement is removed or the screen transitions, the textures leak. This project already has an identified leak: `MainMenuBootstrap` and `MenuBootstrap` have Texture2D memory leaks (Phase B bug list). The `UIGradientHelper.CreateGlowOverlay()` method creates a radial gradient texture and assigns it to a child VisualElement but returns only the VisualElement, not the texture -- the caller has no reference to destroy the texture when done.

**Why it happens:**
UI Toolkit has no `OnDestroy` callback for VisualElements. Unlike MonoBehaviour (which has `OnDestroy`), a VisualElement can be removed from the tree at any time with no lifecycle notification. When a VisualElement with a runtime `background-image` texture is removed, the texture remains in GPU memory. The `UIGradientHelper` API makes this easy to get wrong -- `ApplyVerticalGradient` returns the texture (good), but `CreateGlowOverlay` and `CreateTopHighlight` do not return the texture they create (bad).

**Consequences:**
- Each screen transition that uses gradients leaks 4-20 textures (4x64 bytes for simple gradients, up to 128x128 for radials).
- Over 10+ screen transitions: 40-200 leaked textures. Not catastrophic for short sessions but accumulates.
- In the Title Screen UI Rebuild (Phase E), the AAA effects will create 20+ gradient textures per load. If leaked, this becomes a real memory problem.
- Unity Profiler shows increasing "Texture2D" count that never decreases.

**Prevention:**
1. **Every method that creates a Texture2D must return it.** Refactor `CreateGlowOverlay` and `CreateTopHighlight` to either return the texture or accept a `List<Texture2D>` disposal bag parameter.
2. **Track textures in the MonoBehaviour that owns the UI.** Maintain a `List<Texture2D> _runtimeTextures` field. In `OnDisable`/`OnDestroy`, iterate and `Destroy()` each one.
3. **Call `tex.Apply(false, true)` with `makeNoLongerReadable = true`** on textures that will not be modified after creation. This frees the CPU-side copy, halving memory per texture. The current code passes `false, false` (keeps readable).
4. **Use the 8-texture batch limit wisely.** UI Toolkit batches up to 8 textures per draw call. If every gradient is a unique texture, you blow the batch limit and increase draw calls. Consider a shared gradient atlas texture with UV-mapped regions.
5. **Disable mipmaps (already done) and use smallest viable resolution.** Current defaults (4x64 for linear, 128x128 for radial) are appropriate. Do not increase without profiling.

**Warning signs:**
- `Texture2D.Apply()` called with `(false, false)` on textures that never change.
- No `Destroy()` call matching a `new Texture2D()` in the same class.
- `UIGradientHelper.CreateGlowOverlay()` called without tracking the returned VisualElement's internal texture.

**Detection:**
Unity Profiler > Memory > Texture2D count. Take a snapshot at startup, transition screens 5 times, take another snapshot. If Texture2D count increased, you have a leak.

**Phase to address:** Phase E (Title Screen Rebuild) and Phase F (Character Select Rebuild). Fix the API in Phase C (Code Quality) to prevent leaks in the rebuild phases.

---

### CRIT-3: RenderTexture Display in UI Toolkit Has Race Conditions on Cleanup

**What goes wrong:**
`HeroStageController` correctly creates a `RenderTexture`, binds it to a VisualElement via `Background.FromRenderTexture(_renderTexture)`, and releases it in `CleanupStage()`. But the cleanup order matters critically: if you destroy the RenderTexture while the UI is still rendering from it, you get a one-frame pink/black flash, or worse, a crash. The existing code already handles this by clearing the background-image BEFORE releasing the texture and calling `MarkDirtyRepaint()`. This pattern must be followed everywhere.

The deeper risk is in Phase G (3D Model Integration) where multiple models may need RenderTexture displays (hero preview + champion monster + monster collection cards). Each additional RenderTexture consumes significant GPU memory (1024x1536x4 bytes x MSAA = ~24MB per texture with 4x MSAA).

**Why it happens:**
UI Toolkit renders asynchronously from the main thread in some configurations. The render pass reads the RenderTexture reference stored in the VisualElement's resolved style. If you destroy the RT between the moment the UI reads the reference and the moment it samples the texture, you get undefined behavior. Unity 6's UI Toolkit has improved this (the internal texture binding is more robust), but the timing window still exists during scene transitions.

**Consequences:**
- One-frame black/pink flash during character select transitions (cosmetic but unprofessional).
- Potential editor crash in development if RenderTexture is released mid-render.
- GPU memory exhaustion if multiple 24MB RenderTextures accumulate.

**Prevention:**
1. **Always clear the VisualElement's background-image BEFORE releasing the RenderTexture.** The existing `HeroStageController.CleanupStage()` pattern is correct -- replicate it everywhere:
   ```csharp
   _renderTarget.style.backgroundImage = new StyleBackground(StyleKeyword.None);
   _renderTarget.MarkDirtyRepaint();
   // Now safe to release
   _renderTexture.Release();
   Object.Destroy(_renderTexture);
   ```
2. **Detach the camera from the RenderTexture BEFORE destroying it.** Set `_previewCamera.targetTexture = null` first. The existing code does this correctly.
3. **Budget RenderTextures.** At 1024x1536 with 4x MSAA, each RT is ~24MB. Budget maximum 3 active RenderTextures (hero preview, one monster card, one particle target). Total: ~72MB, acceptable for a 1920x1080 desktop target.
4. **Use a lower resolution for secondary RTs.** Monster collection card previews do not need 1024x1536. Use 512x768 (6MB each).
5. **Pool RenderTextures instead of creating/destroying.** If character select swaps models frequently, reuse the same RT instead of creating a new one each swap.

**Warning signs:**
- `RenderTexture.Release()` or `Destroy(renderTexture)` called WITHOUT first clearing the VisualElement's `backgroundImage`.
- Multiple `new RenderTexture(...)` calls without corresponding `Release()` + `Destroy()` calls.
- RenderTexture resolution matches display resolution (1920x1080) instead of the VisualElement's actual size.

**Detection:**
Unity Profiler > Memory > RenderTexture count and total size. Should never exceed 3 active RTs outside of rendering pipeline internals.

**Phase to address:** Phase G (3D Model Integration). The pattern exists in HeroStageController -- enforce it as a standard for all new RT-based UI.

---

### CRIT-4: USS background-color Overrides Runtime Texture2D Silently

**What goes wrong:**
This is a learned-the-hard-way rule documented in the project's `.claude/rules/ui/toolkit.md`: if a USS stylesheet sets `background-color` on an element, it OVERRIDES the runtime `Texture2D` applied via C#. The gradient becomes invisible. The developer sees a flat color and assumes the gradient code is broken, spends hours debugging C# texture generation, when the fix is removing one line of USS.

**Why it happens:**
USS `background-color` and C# `style.backgroundImage` are different properties, but `background-color` paints over the background image in Unity's UI Toolkit rendering pipeline. Unlike web CSS where `background-image` layers on top of `background-color`, Unity's implementation treats `background-color` as an opaque fill that obscures the image. This is not documented in Unity's official USS property reference -- it was discovered empirically.

**Consequences:**
- Hours of debugging gradient generation code that is actually working correctly.
- Regression during UI rebuild: a new USS class added for layout accidentally includes `background-color`, killing all gradients on that element.
- The failure is silent -- no error, no warning. The element just shows the wrong color.

**Prevention:**
1. **Audit all USS files for `background-color` on elements that will receive runtime gradients.** Search for `background-color` in all `.uss` files and cross-reference against elements targeted by `UIGradientHelper`.
2. **When applying a gradient, explicitly clear `background-color`:**
   ```csharp
   element.style.backgroundColor = new StyleColor(StyleKeyword.None);
   UIGradientHelper.ApplyGradient(element, gradientTexture);
   ```
3. **Add this to `UIGradientHelper.ApplyGradient()` itself** so it is impossible to forget.
4. **Document in USS files:** Add comments above any element that receives runtime textures: `/* NOTE: No background-color -- runtime gradient applied via C# */`

**Warning signs:**
- Gradient code runs without error but the element shows a flat color.
- Adding a new USS class to a gradient element "breaks" the gradient.
- Developer says "gradients stopped working after I changed the styling."

**Detection:**
After applying gradients, take a Unity screenshot and verify visually. If automated, compare expected gradient colors at top/bottom of element.

**Phase to address:** Phase E and Phase F (UI Rebuilds). Must be enforced from the first gradient application.

---

### CRIT-5: Static Event Fields Persist Across Scene Loads (17 Known Instances)

**What goes wrong:**
`EventBus` has 65+ `static event Action<...>` fields. `CharSelectEvents` has 11 more. Static events survive scene loads and `DontDestroyOnLoad` boundaries. If a MonoBehaviour subscribes in `OnEnable` but the scene is unloaded (destroying the MonoBehaviour) before `OnDisable` fires, the static event holds a reference to the destroyed object. Next time the event fires, it invokes a delegate on a destroyed MonoBehaviour, causing a `MissingReferenceException` or silent null behavior. The CONCERNS.md identifies 17 instances of this pattern.

**Why it happens:**
Unity's scene unload destroys GameObjects but does NOT call `OnDisable` on MonoBehaviours that are destroyed as part of scene unload in all circumstances (particularly during `LoadSceneMode.Single` which destroys the old scene). The static event delegate still holds a reference. When `EventBus.ClearAllListeners()` exists but is not called at the right time, leaked delegates accumulate.

**Consequences:**
- `MissingReferenceException` in production after scene transitions.
- Silent logic bugs: an event fires, the old subscriber "handles" it on a destroyed object, the new subscriber in the current scene never gets the event.
- Memory leaks: destroyed MonoBehaviours cannot be GC'd because static events hold references.
- Intermittent -- depends on exact timing of scene load vs. object destruction.

**Prevention:**
1. **Call `EventBus.ClearAllListeners()` at the START of every scene load**, not just at specific transition points. The `VBSceneManager` scene loading flow must include this.
2. **Every MonoBehaviour that subscribes to static events must unsubscribe in `OnDisable`, not `OnDestroy`.** `OnDisable` is called before the object is destroyed. `OnDestroy` may be too late for scene-unload scenarios.
3. **Use weak references or a subscriber registry** that automatically prunes dead subscribers. This is a larger refactor but eliminates the class of bugs entirely.
4. **Add a domain-reload reset** (already present on `SingletonMonoBehaviour` via `SingletonResetHelper`) -- extend to `EventBus` and `CharSelectEvents`.

**Warning signs:**
- Subscribe in `Awake` or `Start` instead of `OnEnable`.
- Unsubscribe in `OnDestroy` instead of `OnDisable`.
- No `ClearAllListeners()` call in scene transition flow.
- `MissingReferenceException` in console after scene transitions.

**Detection:**
After every scene transition in play mode, check the Unity Console for `MissingReferenceException`. If any appear with `EventBus` in the stack trace, you have leaked subscribers.

**Phase to address:** Phase B (fix static event persistence) and Phase C (standardize subscribe/unsubscribe patterns). Must be done BEFORE Phase E/F UI rebuilds add new subscribers.

---

## High Pitfalls

Mistakes that cause significant debugging time or feature regression.

---

### HIGH-1: Debug.Log Replacement Changes Semantic Behavior

**What goes wrong:**
Replacing `Debug.Log(message)` with `ErrorLogger.Log(message)` seems like a simple find-and-replace. But `ErrorLogger.Log` is decorated with `[Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]`, which means the **entire call is compiled out** in release builds -- including the evaluation of its arguments. If a `Debug.Log` call has side effects in its argument (unlikely but possible: `Debug.Log($"Count: {list.Count}")` where `list.Count` triggers lazy initialization), removing the call changes runtime behavior.

More commonly, the issue is that `Debug.LogWarning` and `Debug.LogError` are used in production-critical paths. Replacing a `Debug.LogError("Save failed!")` with `ErrorLogger.Error("Save failed!")` is safe because `ErrorLogger.Error` is NOT conditional -- it always executes. But replacing a `Debug.LogWarning` used as a user-facing indicator with `ErrorLogger.Warn` (also not conditional) changes nothing. The real risk is accidentally replacing a `Debug.Log` that should have been `Debug.LogWarning` with `ErrorLogger.Log` (conditional), silently removing a warning the developer intended to keep.

**Why it happens:**
Batch find-and-replace treats all `Debug.Log*` calls as equivalent. They are not:
- `Debug.Log` -> `ErrorLogger.Log` (conditional -- stripped in release)
- `Debug.LogWarning` -> `ErrorLogger.Warn` (NOT conditional -- always runs)
- `Debug.LogError` -> `ErrorLogger.Error` (NOT conditional -- always runs)
- `Debug.LogException` -> `ErrorLogger.Exception` (NOT conditional -- always runs)

The developer needs to decide, for each of the 146+ `Debug.Log` calls across 30+ files, whether the message is:
- A development-only debug trace (use conditional `ErrorLogger.Log`/subsystem methods)
- A warning about unexpected but recoverable state (use `ErrorLogger.Warn`)
- An error that indicates a bug (use `ErrorLogger.Error`)

**Consequences:**
- Important warnings silently disappear in release builds.
- Release builds behave differently from editor builds in subtle ways.
- Error messages that developers relied on for diagnosing player-reported bugs are gone.

**Prevention:**
1. **Classify each Debug.Log call individually.** Do NOT batch-replace. Read the context of each call and decide the appropriate severity level.
2. **Use subsystem-specific methods for traces:** `ErrorLogger.Combat()`, `ErrorLogger.Save()`, `ErrorLogger.UI()`, etc. These are all conditional and include subsystem prefixes.
3. **Keep `Debug.LogWarning` calls as `ErrorLogger.Warn`** (not conditional) unless the warning is purely development-time.
4. **Keep `Debug.LogError` calls as `ErrorLogger.Error`** (not conditional) always.
5. **Never replace a log call in a catch block with a conditional method.** Error handling logs must survive release builds.

**Warning signs:**
- A PR that replaces 30 `Debug.Log` calls in one commit with no per-call analysis.
- `ErrorLogger.Log()` used inside a `catch` block.
- `ErrorLogger.Combat()` used for a message that should be a warning.

**Detection:**
After replacement, build in release mode and verify that important log messages still appear. Specifically test: save failure, missing asset, null reference recovery paths.

**Phase to address:** Phase C (Code Quality Hardening).

---

### HIGH-2: Singleton Migration Breaks Initialization Order

**What goes wrong:**
Converting `VERASystem`, `FPSCounter`, and others from hand-rolled singletons to `SingletonMonoBehaviour<T>` changes the `Awake` behavior. The existing `VERASystem.Awake()` sets `_instance` and calls `DontDestroyOnLoad` with custom logic. `SingletonMonoBehaviour<T>.Awake()` does the same but also calls `OnSingletonAwake()`. If the migrated class still has its own `Awake()` and forgets to call `base.Awake()`, the singleton infrastructure breaks silently -- `Instance` returns null, `HasInstance` returns false, and all callers that depend on the singleton get `NullReferenceException`.

**Why it happens:**
C# method hiding. If the subclass declares `private void Awake()` instead of `protected override void Awake()`, it hides the base class `Awake` -- Unity calls the subclass version, the base version never runs, and the singleton is never registered. This is not a compile error. C# emits a warning (CS0114) but many Unity projects suppress it or miss it in the noise.

**Consequences:**
- `VERASystem.Instance` returns null after migration.
- `GameBootstrap` which creates singletons in sequence fails silently.
- 13+ downstream managers that depend on singleton ordering may break.
- Symptom appears as `NullReferenceException` far from the actual bug, making diagnosis difficult.

**Prevention:**
1. **Migrate one singleton at a time.** Do VERASystem first (simplest, least dependencies). Verify in play mode. Then FPSCounter. Then others. Never batch.
2. **Follow a strict migration checklist per class:**
   - Change `class X : MonoBehaviour` to `class X : SingletonMonoBehaviour<X>`
   - Remove the private `_instance` field and `Instance` property
   - Remove the `_isQuitting` field and `OnApplicationQuit` handler
   - Remove the `ResetStatics` method
   - Remove `DontDestroyOnLoad(gameObject)` call
   - Rename `Awake()` to `protected override void OnSingletonAwake()`
   - Remove the duplicate-instance check (base class handles it)
   - Verify `OnDestroy` calls `base.OnDestroy()` if overridden
3. **Verify compilation with warnings-as-errors** for CS0114 (method hides inherited member).
4. **Test in play mode after each migration:** enter play mode, check that `X.Instance` is not null, check that `X.HasInstance` is true.

**Warning signs:**
- `Awake()` exists on a class that inherits from `SingletonMonoBehaviour<T>` without `override` keyword.
- `DontDestroyOnLoad` called in a subclass of `SingletonMonoBehaviour<T>` (the base already handles this).
- `_instance` field still present after migration (shadows the base class field).

**Detection:**
Grep for `void Awake()` in any file that inherits `SingletonMonoBehaviour`. If found without `override`, it is hiding the base.

**Phase to address:** Phase C (Code Quality Hardening). Must be done carefully and one-at-a-time.

---

### HIGH-3: Collection Modification During Iteration (10 Known Instances)

**What goes wrong:**
The CONCERNS.md identifies 10 instances across 20 files where collections are modified during iteration. A `foreach` loop over a list, combined with an event callback that removes from that list, throws `InvalidOperationException` in the best case or silently skips/double-processes elements in the worst case. The `StatusEffectManager` is the highest-risk: removing status effects during iteration can skip effects or cause the `_tempEffectList` shared buffer to produce incorrect results if a removal triggers a callback that re-enters the manager.

**Why it happens:**
Event-driven architecture. A `foreach` loop processes a collection and fires an event for each element. A subscriber to that event modifies the same collection. The loop iterator is now invalid. This is especially common with:
- `BattleManager` processing combatants and removing dead ones mid-loop.
- `StatusEffectManager` applying tick damage and removing expired effects.
- `CharSelectEvents` firing change events that modify subscriber lists.

**Consequences:**
- `InvalidOperationException: Collection was modified; enumeration operation may not complete.`
- Skipped elements: removing item at index 3 causes item 4 to shift to index 3, the iterator advances to index 4, and the original item 4 is never processed.
- Combat logic errors: a status effect tick kills a combatant, the combatant is removed, remaining effects on that combatant are not processed (or worse, are processed on the wrong target).

**Prevention:**
1. **Iterate over a snapshot.** Before the loop, copy to a temporary array: `var snapshot = myList.ToArray();` then iterate `snapshot`. Modifications to the original list are safe.
2. **Use reverse iteration for removal.** `for (int i = list.Count - 1; i >= 0; i--)` with `list.RemoveAt(i)` is safe because removals only affect indices below the current position.
3. **Defer modifications.** Collect items to remove in a separate list during iteration, then process removals after the loop completes.
4. **For `StatusEffectManager` specifically:** The `_tempEffectList` shared buffer pattern is already a deferred-snapshot approach, but re-entrance corrupts it. Add a re-entrance guard flag: `if (_isProcessingEffects) throw new InvalidOperationException("Re-entrant status effect modification");`

**Warning signs:**
- `foreach` loop over a collection that is also exposed to event callbacks.
- `list.Remove()` or `list.Add()` inside a `foreach` over the same list.
- A method that iterates a collection AND fires events that might modify it.

**Detection:**
Static analysis: search for `foreach.*_` patterns where the iterated field is also passed to or accessible by event handlers. In play mode, run extended combat sessions and check console for `InvalidOperationException`.

**Phase to address:** Phase A and Phase B (bug fixes). Fix the combat-critical instances first.

---

### HIGH-4: UIAnimationController DontDestroyOnLoad Without SingletonMonoBehaviour

**What goes wrong:**
`UIAnimationController` uses a manual `DontDestroyOnLoad` pattern (identified in Phase B bug list) without proper duplicate checking. If the Bootstrap scene creates one instance and a direct-entry scene creates another, two instances persist and fight over animation control, causing double-speed animations, cancelled tweens, or visual artifacts. Six instances of this pattern exist across the codebase.

**Why it happens:**
Each developer who needed persistence implemented their own variant of the singleton pattern. `SingletonMonoBehaviour<T>` exists and handles all edge cases (duplicate detection, domain reload, DontDestroyOnLoad, quitting flag), but older code predates it. The manual implementations miss at least one edge case each:
- `VERASystem`: No `HasInstance` check. No `IsPersistent` override option.
- `FPSCounter`: Uses `[DisallowMultipleComponent]` but does not prevent cross-scene duplicates.
- `ThemeManager`: Creates via `new GameObject()` -- if called twice, two GameObjects exist before duplicate check.
- `UIAnimationController`: Creates via `new GameObject()` with same race condition.

**Prevention:**
- Addressed by the singleton migration (HIGH-2). But the migration itself is risky. Do them one at a time.

**Phase to address:** Phase C (Code Quality Hardening).

---

### HIGH-5: Character Select Has 4 Duplicate/Stale USS Stylesheets

**What goes wrong:**
The PROJECT.md documents "4 duplicate/stale CharacterSelect USS stylesheets causing confusion" and "2 overlapping global USS files (VeilBreakers.uss vs VeilBreakersUI.uss)." During the UI Rebuild (Phases E and F), adding new styles to the wrong stylesheet or duplicating a selector across stylesheets produces CSS-like specificity conflicts. A style defined in file A is overridden by a more specific rule in file B, but the developer is editing file C and cannot understand why their changes have no effect.

**Why it happens:**
Multiple development sessions, each creating their own USS file for the same screen. No cleanup between sessions. Unity UI Toolkit loads all USS files attached to a UXML document and merges them with cascading specificity rules. Unlike web CSS, there is no browser dev-tools inspector to show which USS rule won -- the only way to debug is to remove stylesheets one by one.

**Consequences:**
- Styles that work in one context but not another.
- "I changed this style but nothing happened" -- because a higher-specificity rule in another file overrides it.
- Merge conflicts when multiple developers edit different copies of what should be the same stylesheet.

**Prevention:**
1. **Consolidate USS files BEFORE the UI rebuild.** Identify the canonical stylesheet per screen, merge rules from duplicates, delete the duplicates. This is Phase D cleanup work.
2. **Naming convention:** One USS per screen: `TitleScreen.uss`, `CharacterSelect.uss`, `Inventory.uss`. One global `VeilBreakers.uss` for shared styles. Delete `VeilBreakersUI.uss` after merging its unique rules.
3. **Comment USS sections:** Group rules by component within the file. Mark overrides with `/* OVERRIDE: reason */`.

**Phase to address:** Phase D (before UI rebuild begins).

---

## Moderate Pitfalls

Issues that cause friction, debugging time, or suboptimal results.

---

### MOD-1: UI Toolkit Has No CSS Gradients, Box-Shadow, or Blur

**What goes wrong:**
Developers accustomed to web CSS attempt to use `linear-gradient()`, `box-shadow`, or `backdrop-filter: blur()` in USS files. These do not exist in Unity's USS. The USS parser silently ignores unknown properties -- no error, no warning. The developer writes a full USS rule, it compiles without error, and nothing renders.

This project learned this the hard way: "USS-only approach failed" is documented as a project lesson. The entire `UIGradientHelper.cs` system exists because USS gradients do not work.

**Why it happens:**
USS looks like CSS but is NOT CSS. Only a subset of CSS properties are supported. Unity's documentation lists supported properties but does not list unsupported ones. Developers (including AI assistants) assume CSS knowledge transfers.

**Prevention:**
1. **Never use USS for visual effects.** USS for layout (flexbox, position, margin, padding, display, flex-grow). C# for visuals (gradients, glows, shadows, animations).
2. **Do not use `style.gap`** -- it does not exist on `IStyle` in Unity 6. Use child margins instead.
3. **For shadows:** Create a separate VisualElement behind the target with a blurred radial gradient texture (via `UIGradientHelper.CreateRadialGradient`).
4. **For blur:** Not possible in UI Toolkit without custom shader. Accept this limitation or use a pre-blurred background texture.
5. **Verify every USS property against Context7** (`/needle-mirror/com.unity.ui`) before using it.

**Phase to address:** Phase E and Phase F (UI Rebuilds). Enforce from session start.

---

### MOD-2: PrimeTween API Hallucination

**What goes wrong:**
AI assistants (and developers working from memory) write PrimeTween API calls that do not exist. The existing `HeroStageController` correctly uses `Tween.Custom(this, ...)` with the target-based overload. But it is easy to write closure-based overloads (which allocate GC) or non-existent method signatures. Phase C explicitly calls out "Convert closure-based PrimeTween to target-based" for `StatNumberAnimator` and `ScreenEntryAnimator`.

**Why it happens:**
PrimeTween's API has changed across versions. The closure-based `Tween.Custom(0f, 1f, 1f, val => { })` exists but allocates. The target-based `Tween.Custom(target, 0f, 1f, 1f, (tgt, val) => { })` is allocation-free but has a different signature. AI assistants frequently hallucinate non-existent overloads.

**Prevention:**
1. **ALWAYS check Context7 (`/kyrylokuzyk/primetween`) before writing ANY PrimeTween code.** This is a CLAUDE.md HARD RULE.
2. **Use target-based overloads exclusively.** The pattern from `HeroStageController` is the canonical reference:
   ```csharp
   Tween.Custom(this, startValue, endValue, duration,
       onValueChange: (ctrl, val) => ctrl.DoSomething(val));
   ```
3. **Never use closure captures for MonoBehaviour fields** in tween callbacks. The MonoBehaviour may be destroyed during the tween's lifetime.

**Phase to address:** Phase C (Code Quality) and Phase E/F (UI Rebuilds).

---

### MOD-3: CancellationToken Missing from Async Methods

**What goes wrong:**
Phase C includes "Add CancellationToken to MonoBehaviour async methods." Unity's `async void Start()` and `async Task` methods on MonoBehaviours continue executing after the MonoBehaviour is destroyed. Without a `CancellationToken` tied to the MonoBehaviour's lifecycle, `await` operations resume on a destroyed object, causing `MissingReferenceException` or operating on stale state.

The `EmbarkCinematicController` is a known example: it has an event-nulling bug that "hangs async flow" (Phase B bug list) -- likely because an async method is awaiting a Task and the event it depends on is nulled during scene transition.

**Prevention:**
1. **Use `destroyCancellationToken`** (available in Unity 6 on MonoBehaviour). This token is automatically cancelled when the MonoBehaviour is destroyed.
2. **Pass the token to all awaitable operations:**
   ```csharp
   await Task.Delay(1000, destroyCancellationToken);
   ```
3. **Wrap async methods with try/catch for `OperationCanceledException`:**
   ```csharp
   try { await DoWork(destroyCancellationToken); }
   catch (OperationCanceledException) { /* Expected on destroy */ }
   ```

**Phase to address:** Phase C (Code Quality Hardening).

---

### MOD-4: 3D Model Import Quality Varies Wildly Across 28 GLBs

**What goes wrong:**
Phase G requires auditing 28 GLB models. The models were generated by AI (Tripo/Hyper3D) and have varying quality: some may have inverted normals, non-manifold geometry, missing UVs, excessive polycount, or broken rigs. Importing them into Unity without validation will surface problems late -- materials show pink, meshes appear inside-out, animations do not play, or performance drops due to 500K-poly models in a UI preview.

**Prevention:**
1. **Run a quality check on each GLB before importing to Unity.** Use `blender_mesh action=game_check` from VB-Toolkit.
2. **Set polycount budgets:** Hero models max 50K tris, monster models max 30K tris. Models exceeding these need decimation.
3. **Validate in Unity after import:** Check material assignment, normal orientation (no inside-out faces), UV coverage.
4. **Delete old model versions first** (CRIT-02 in CONCERNS.md: ~500MB of dead model iterations).

**Phase to address:** Phase G (3D Model Quality Audit). Must be completed before Phase F integration.

---

### MOD-5: TitleScreenVFX God Class (3,145 Lines) Resists Incremental Fixes

**What goes wrong:**
`TitleScreenVFX.cs` is 3,145 lines with 140+ embers, 40 micro-sparks, 16 ash particles, 11+ `Resources.Load` calls, and 196+ VisualElements created on scene load. Any Phase E change to the title screen's visual effects requires editing this file, and any edit risks breaking unrelated effects. The class has no test coverage and is fragile by nature.

**Prevention:**
1. **Phase E should start by decomposing this file**, not by adding more effects to it.
2. **Extract subsystems:** `EmberParticleSystem`, `SmokeWispSystem`, `PortalBackgroundSystem`, `LightningSystem`, `ButtonVFXSystem`.
3. **Pool VisualElements** instead of creating 196 new ones on load.
4. **Replace `Resources.Load` with UIAssets ScriptableObject** references (HIGH-02 in CONCERNS.md).
5. **Cap total element count** per quality tier.

**Phase to address:** Phase E (Title Screen Rebuild). Decompose first, enhance second.

---

## Minor Pitfalls

Issues that cause friction but are easily fixable once identified.

---

### MIN-1: SharedAudioSource Conflict Between Components

**What goes wrong:**
`HoldToEmbark` and `CharSelectFocusManager` both try to play audio through the same `AudioSource` on the same frame. One clip cuts off the other. This is a Phase B bug (already identified) but representative of a pattern: multiple components assuming they have exclusive access to a shared resource.

**Prevention:**
- Give each component its own `AudioSource` or use `AudioManager` to mediate.

**Phase to address:** Phase B.

---

### MIN-2: Enum.IsDefined Guards Missing on JSON Deserialization

**What goes wrong:**
`HeroData`, `SkillData`, and `ItemData` cast raw integers from JSON to enum types without validation. Invalid values create enum instances with no matching case in switch statements, falling through to default cases (or throwing if no default exists).

**Prevention:**
- Wrap every `(EnumType)intValue` cast with `Enum.IsDefined(typeof(EnumType), intValue)`.
- Return a safe default (`Brand.IRON`, `SkillType.ATTACK`) for invalid values.
- Log a warning via `ErrorLogger.Warn` for invalid values to catch bad data.

**Phase to address:** Phase B.

---

### MIN-3: FPSCounter Uses IMGUI (OnGUI) Instead of UI Toolkit

**What goes wrong:**
`FPSCounter` uses `OnGUI()` for rendering, which is the legacy immediate-mode GUI system. In a project that exclusively uses UI Toolkit, this is inconsistent and may conflict with UI Toolkit's event system. Not a bug per se, but a code quality issue that becomes relevant if FPSCounter is migrated to `SingletonMonoBehaviour<T>`.

**Prevention:**
- Accept IMGUI for FPSCounter (it is lightweight and always-visible, which IMGUI handles well).
- Do NOT attempt to convert to UI Toolkit unless specifically requested -- the IMGUI implementation is simpler and more performant for this use case.

**Phase to address:** Phase C (acknowledge as acceptable tech debt).

---

## Phase-Specific Warnings

| Phase | Likely Pitfall | Mitigation |
|-------|---------------|------------|
| **A: Critical Bug Fixes** | Cascade regressions from combat system fixes (CRIT-1) | Fix in isolation, one commit per fix, compile-check after each |
| **A: Critical Bug Fixes** | Brand matrix fix breaks existing tests (CRIT-05 in CONCERNS.md) | Run `BrandSystem_EditModeTests.cs` after every matrix change |
| **B: High-Priority Bug Fixes** | Static event leaks (CRIT-5) cause MissingReferenceException | Standardize subscribe in OnEnable, unsubscribe in OnDisable |
| **B: High-Priority Bug Fixes** | Collection modification crashes in StatusEffectManager (HIGH-3) | Iterate snapshots, add re-entrance guard |
| **C: Code Quality** | Debug.Log replacement changes behavior (HIGH-1) | Classify each call individually, not batch replace |
| **C: Code Quality** | Singleton migration breaks initialization (HIGH-2) | Migrate one at a time, test after each |
| **C: Code Quality** | Missing CancellationToken on async methods (MOD-3) | Use `destroyCancellationToken` on Unity 6 MonoBehaviours |
| **D: Title/CharSelect Bug Fixes** | USS stylesheet conflicts (HIGH-5) | Consolidate to one USS per screen before editing |
| **E: Title Screen Rebuild** | Texture2D leaks (CRIT-2), USS override kills gradients (CRIT-4) | Track all textures in disposal list, clear background-color before applying gradients |
| **E: Title Screen Rebuild** | TitleScreenVFX god class (MOD-5) | Decompose before enhancing |
| **F: CharSelect Rebuild** | RenderTexture race condition on cleanup (CRIT-3) | Follow HeroStageController's cleanup pattern exactly |
| **F: CharSelect Rebuild** | PrimeTween API hallucination (MOD-2) | Always verify via Context7 before writing |
| **G: 3D Model Integration** | Bad geometry/materials from AI-generated GLBs (MOD-4) | Run quality check per model before import |
| **G: 3D Model Integration** | GPU memory exhaustion from multiple RenderTextures (CRIT-3) | Budget max 3 active RTs, use lower resolution for secondary previews |
| **H: End-to-End Verification** | Regression in a system fixed in Phase A/B discovered late | Run full flow test after each phase, not just at the end |

## Domain-Specific Anti-Patterns

| Anti-Pattern | Why It Seems Right | Why It Fails | Instead |
|-------------|-------------------|--------------|---------|
| Batch find-and-replace Debug.Log | Efficient, 30 files done in 5 minutes | Changes semantic behavior, strips important warnings | Classify each call individually |
| Fix all 73 bugs in one long session | Get it all done at once | Cascade regressions, context loss, fatigue errors | Fix in tier batches (A->B->C->D) |
| USS gradients/shadows | Works in CSS, USS looks like CSS | USS is not CSS, properties silently ignored | C# runtime Texture2D generation |
| Large RenderTexture matching screen resolution | "Maximum quality" | 24MB+ per RT, GPU memory exhaustion | Match VisualElement size, not screen size |
| Singleton migration in one PR | "Same pattern everywhere" | One broken migration breaks the entire boot chain | One singleton per commit, test after each |
| Creating Texture2D without tracking for disposal | "It's a small texture" | No GC for native resources, leaks accumulate | Maintain `List<Texture2D>` per MonoBehaviour for cleanup |
| Keeping readable textures (`Apply(false, false)`) | "Might need it later" | Doubles CPU memory per texture | `Apply(false, true)` for textures that never change |

## "Looks Working But Isn't" Checklist

Verification items that pass basic testing but fail in real workflows.

- [ ] **Texture leak test:** Load title screen, go to character select, go back to title, repeat 10 times. Check Profiler for Texture2D count growth.
- [ ] **RenderTexture cleanup:** Switch heroes in character select 20 times rapidly. No pink flashes. RenderTexture count stable.
- [ ] **Scene transition events:** Go Title -> CharSelect -> back to Title -> CharSelect. No `MissingReferenceException` in console.
- [ ] **Singleton persistence:** Enter play mode, load Bootstrap scene, transition to CharSelect. Verify all 13+ singletons have non-null `Instance`.
- [ ] **Debug.Log in release:** Build standalone player. Trigger save failure. Verify error message appears in Player.log.
- [ ] **Brand matrix symmetry:** Run `BrandSystem_EditModeTests`. All 100 matchups pass. Add a bidirectionality test.
- [ ] **Combat flow after fixes:** Start combat, use all 10 brands, apply status effects, capture a monster. No crashes, correct damage numbers.
- [ ] **Async cancellation:** Start embark cinematic, interrupt by pressing back. No hanging coroutines, no orphaned Tasks.
- [ ] **USS override check:** Apply gradient to an element, add a USS class with background-color. Verify the gradient is still visible (it should not be -- this tests that the prevention is in place).
- [ ] **Collection iteration safety:** Run 50 combat rounds with status effects. No `InvalidOperationException`.

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|--------------|----------------|
| Cascade regressions (CRIT-1) | HIGH | `git log --oneline` to find last good commit, `git revert` bad commits one by one |
| Texture2D leaks (CRIT-2) | LOW | Add disposal tracking to UIGradientHelper, audit all callers, add Destroy() in OnDisable |
| RenderTexture race condition (CRIT-3) | LOW | Follow existing HeroStageController cleanup pattern, add to code review checklist |
| USS overrides gradients (CRIT-4) | LOW | Add `backgroundColor = StyleKeyword.None` to UIGradientHelper.ApplyGradient |
| Static event leaks (CRIT-5) | MEDIUM | Add ClearAllListeners to scene transition, audit all subscribe/unsubscribe pairs |
| Debug.Log semantic change (HIGH-1) | MEDIUM | Re-audit all replacements, compare against original severity intent |
| Singleton migration break (HIGH-2) | HIGH | Revert the broken migration commit, re-do with checklist |
| Collection modification crash (HIGH-3) | LOW | Add `.ToArray()` snapshot before foreach, per-instance fix |
| TitleScreenVFX decomposition (MOD-5) | HIGH | Multi-session refactor, extract one subsystem at a time, test after each extraction |

## Sources

### Project-Specific (HIGH confidence)
- `.planning/PROJECT.md` -- v6.0 milestone definition, 8 phases, 73+ bugs
- `.planning/codebase/CONCERNS.md` -- 128 C# scripts, 154+ identified issues, fix priority matrix
- `.claude/rules/ui/toolkit.md` -- Learned USS limitations, Context7 mandate, PrimeTween rules
- `CLAUDE.md` -- Anti-regression protocol, read-before-edit mandate, loop detection
- `Assets/Scripts/UI/Core/UIGradientHelper.cs` -- Runtime Texture2D gradient generation (current implementation)
- `Assets/Scripts/UI/CharacterSelect/HeroStageController.cs` -- RenderTexture-to-UI-Toolkit pattern (reference implementation)
- `Assets/Scripts/Core/SingletonMonoBehaviour.cs` -- Canonical singleton base class
- `Assets/Scripts/Core/ErrorLogger.cs` -- Conditional logging system (target for Debug.Log migration)
- `Assets/Scripts/Systems/VERASystem.cs` -- Hand-rolled singleton to be migrated

### Unity Documentation (HIGH confidence)
- [Unity 6 UI Toolkit Performance Optimization](https://docs.unity3d.com/6000.3/Documentation/Manual/best-practice-guides/ui-toolkit-for-advanced-unity-developers/optimizing-performance.html) -- 8-texture batch limit, dynamic atlas, vertex budget
- [Unity Debug Class Manual](https://docs.unity3d.com/6000.3/Documentation/Manual/class-Debug.html) -- Debug.Log behavior in release builds
- [Unity Memory Management](https://learn.unity.com/tutorial/memory-management-in-unity) -- Native resource lifecycle, Destroy() requirements

### Community & Technical References (MEDIUM confidence)
- [Debug.Log Performance Impact](https://unity3dperformance.com/index.php/2024/10/02/debug-log-performance-optimization/) -- 10-30% FPS drop from unguarded Debug.Log
- [Conditional Attribute for Logging](https://gamedevbeginner.com/how-to-use-debug-log-in-unity-without-affecting-performance/) -- [Conditional] strips call AND argument evaluation
- [RenderTexture Memory Leaks](https://discussions.unity.com/t/in-62760-graphics-memory-leak-related-to-rendertextures/318747) -- Known Unity issue tracker entries
- [Singleton Migration Best Practices](https://gamedevbeginner.com/singletons-in-unity-the-right-way/) -- DontDestroyOnLoad patterns, duplicate detection
- [JetBrains Unity Debug.Log Guidance](https://github.com/JetBrains/resharper-unity/wiki/Avoid-usage-of-Debug.Log-methods-in-performance-critical-context) -- Performance-critical context avoidance

---
*Pitfalls research for: VeilBreakers v6.0 Bug Fixes, Code Quality Hardening & UI Rebuild*
*Researched: 2026-03-30*
*Confidence: HIGH overall -- critical pitfalls verified against existing codebase patterns, project history, and Unity documentation*
