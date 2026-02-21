# Codebase Concerns

**Analysis Date:** 2026-02-21

## Tech Debt

**Duplicate / Stale USS Stylesheets:**
- Issue: Four separate Character Select stylesheets exist, causing confusion about which is canonical.
- Files:
  - `Assets/UI/Styles/CharacterSelect.uss` (active, referenced by UXML)
  - `Assets/UI/Styles/CharacterSelectAAA.uss` (stale, not referenced -- identical structure but different glass-bg and easing values)
  - `Assets/UI/Styles/CharacterSelect_Backup.uss` (explicit backup file)
  - `Assets/UI/Screens/CharacterSelect_Backup.uxml` (backup UXML)
- Impact: Developers may edit the wrong file. The AAA variant uses proper `cubic-bezier()` easing values while the active file uses `ease-out-back` fallbacks, suggesting the AAA version was the intended canonical file but got superseded.
- Fix approach: Delete `CharacterSelectAAA.uss` and `*_Backup.*` files. If the AAA easing functions are desired, port them into the active `CharacterSelect.uss`.

**Duplicate Main Stylesheets:**
- Issue: Two overlapping global stylesheets with near-identical purposes.
- Files:
  - `Assets/UI/Styles/VeilBreakers.uss` -- "Main Stylesheet, Dark Fantasy Horror Theme"
  - `Assets/UI/Styles/VeilBreakersUI.uss` -- "AAA UI Stylesheet, The Ultimate Dark Fantasy Horror UI System"
- Impact: Both set `*` font definitions and `:root` CSS variables. Which is canonical is unclear. Conflicting `transition-duration` defaults on `*` selector in `VeilBreakersUI.uss` could cause unexpected animation behavior.
- Fix approach: Consolidate into a single `VeilBreakers.uss` and update all UXML `@import` references.

**Legacy/Deprecated EventBus Events:**
- Issue: `EventBus` contains deprecated `OnStatusApplied` and `OnStatusRemoved` events with `[Obsolete]` attributes and `#pragma warning disable` blocks.
- Files: `Assets/Scripts/Core/EventBus.cs` (lines 86-97)
- Impact: Dead code that increases maintenance burden. The replacement `OnStatusEffectApplied` / `OnStatusEffectRemoved` events exist and are used.
- Fix approach: Search for any remaining subscribers to the deprecated events. If none, remove the deprecated events and the `#pragma` blocks.

**TransitionController is a Stub:**
- Issue: `TransitionController.HandleHeroChanged()` has an empty body with a comment "reserved for future veil tear effects." The class subscribes to `OnHeroChanged` but does nothing.
- Files: `Assets/Scripts/UI/CharacterSelect/TransitionController.cs` (lines 41-50)
- Impact: Unnecessary event subscription and MonoBehaviour overhead. Wasted slot on the CharacterSelectSystem GameObject.
- Fix approach: Remove the component entirely until veil tear effects are implemented. If the scaffolding is desired, keep the file but do not add the component to the scene.

**Massive EventBus Without Cleanup Guarantees:**
- Issue: `EventBus` has 50+ static events with no automatic subscriber cleanup. `ClearAllListeners()` exists but must be called manually. If a scene-scoped MonoBehaviour forgets to unsubscribe and is destroyed, the static event retains a reference to the dead object.
- Files: `Assets/Scripts/Core/EventBus.cs` (370 lines, 50+ events)
- Impact: Memory leaks from dangling event subscriptions. Potential `MissingReferenceException` if a callback fires after its owner is destroyed. The pattern of `subscribers clean up in their own OnDestroy` (seen in comments across audio managers) is fragile.
- Fix approach: Consider a weak-reference event pattern, or add automatic cleanup via a scene-unload hook that calls `ClearAllListeners()`. Alternatively, move to a channel-based system where each scene registers/unregisters its own scope.

**Vex-Only Editor Tooling:**
- Issue: `VexAnimatorControllerBuilder.cs` and `MixamoAnimationImporter.cs` are hardcoded for the Vex character only. No equivalent tooling exists for Seraphina, Orion, or Nyx.
- Files:
  - `Assets/Editor/VexAnimatorControllerBuilder.cs` (references hardcoded paths like `Assets/Art/Animations/Characters/Vex/`)
  - `Assets/Editor/MixamoAnimationImporter.cs` (checks only `kVexAnimationPath`)
- Impact: When other hero models are added, there is no generalized pipeline. The Vex-specific code will need to be refactored into a generic `HeroAnimatorControllerBuilder` parameterized by hero ID.
- Fix approach: Refactor to accept a hero ID parameter. Make the animation folder path, avatar source paths, and controller output path all derive from the hero ID.

**`_Recovery` Directory in Assets:**
- Issue: A recovery scene file exists at `Assets/_Recovery/0.unity` -- appears to be a Unity crash recovery artifact.
- Files: `Assets/_Recovery/0.unity`
- Impact: Junk file taking up space in the project and potentially confusing asset searches.
- Fix approach: Delete `Assets/_Recovery/` and its `.meta` file.

**20+ TODO Comments Across Codebase:**
- Issue: Over 20 TODO comments indicate unfinished integration points.
- Files (major clusters):
  - `Assets/Scripts/UI/Menus/InventoryController.cs` (6 TODOs: lines 315, 332, 531, 832, 856, 937)
  - `Assets/Scripts/UI/Menus/MonsterCollectionController.cs` (5 TODOs: lines 293, 329, 770, 808, 813)
  - `Assets/Scripts/UI/Menus/MainMenuController.cs` (3 TODOs: lines 389, 395, 473)
  - `Assets/Scripts/UI/Menus/VERADialogueController.cs` (2 TODOs: lines 691, 727)
  - `Assets/Scripts/AI/GambitController.cs` (line 256)
  - `Assets/Scripts/AI/GambitEvaluator.cs` (line 115)
  - `Assets/Scripts/Combat/DamageCalculator.cs` (line 82)
- Impact: Features marked TODO will silently fail or use placeholder values. The inventory system does not load real data ("TODO: Load from SaveManager/GameManager"), currencies display 0, and confirmation dialogs are missing.
- Fix approach: Prioritize by system. Inventory and Monster Collection TODOs block save/load integration. AI TODOs are lower priority.

## Known Bugs

**Nebula Texture Regenerated on Every Hero Switch:**
- Symptoms: A 256x256 `Texture2D` with per-pixel Perlin noise is generated synchronously on the main thread each time the hero changes.
- Files: `Assets/Scripts/UI/CharacterSelect/CharSelectEnvironmentController.cs` (lines 133-177)
- Trigger: Switch heroes in the Character Select screen.
- Workaround: The 256x256 size is small enough to avoid visible hitching, but the allocation of `Color[65536]` on every hero switch generates GC pressure.

**FPSCounter Uses Legacy Singleton Pattern:**
- Symptoms: `FPSCounter` implements its own singleton pattern instead of extending `SingletonMonoBehaviour<T>`. `GameBootstrap.EnsureFPSCounter()` has special handling because `FPSCounter.Instance` is a different pattern.
- Files:
  - `Assets/Scripts/UI/Core/FPSCounter.cs` (custom `_instance` field, lines 11-12)
  - `Assets/Scripts/Core/GameBootstrap.cs` (special `EnsureFPSCounter` method, lines 230-239)
- Trigger: Any time FPSCounter instance is checked.
- Workaround: Works, but inconsistent with the rest of the codebase.

**LowHealthAudio Uses Inconsistent Singleton Pattern:**
- Symptoms: Similar to FPSCounter, `LowHealthAudio` has a custom singleton pattern (seen in its `_isQuitting` guard at line 23) and `GameBootstrap` has a special `EnsureLowHealthAudio()` method.
- Files:
  - `Assets/Scripts/Audio/LowHealthAudio.cs` (line 23: custom Instance getter)
  - `Assets/Scripts/Core/GameBootstrap.cs` (lines 212-228)
- Trigger: Edge cases around application quit timing.
- Workaround: Functional but adds unnecessary code paths.

## Security Considerations

**Save File Encryption Key Derivation:**
- Risk: `SaveFileHandler` uses PBKDF2 with a hardcoded salt (`VeilBreakers_SaveKeySalt_v1`). The actual key/passphrase source is not visible in the handler itself, but the salt is embedded in the binary.
- Files: `Assets/Scripts/Managers/SaveFileHandler.cs` (line 45)
- Current mitigation: This is a single-player game, so the risk is limited to save file tampering by the player themselves. AES-CBC encryption with HMAC-SHA256 integrity checking is implemented.
- Recommendations: For a single-player RPG, this level of encryption is reasonable. If competitive features are added later, the key derivation should use a device-specific component.

**SceneAuditor Editor Script Not Namespaced:**
- Risk: `SceneAuditor` class at `Assets/Editor/SceneAuditor.cs` is not in a namespace. This could conflict with third-party packages.
- Files: `Assets/Editor/SceneAuditor.cs` (line 6: `public class SceneAuditor`)
- Current mitigation: Editor-only, low risk.
- Recommendations: Move into `VeilBreakers.Editor` namespace for consistency.

## Performance Bottlenecks

**CharSelectEnvironmentController Per-Frame Allocations:**
- Problem: `ApplyParallax()` runs every frame, calling `Input.mousePosition` (legacy Input API) and creating `new Vector2()` values. While not heap-allocating, it uses the legacy Input system instead of the New Input System (`VeilBreakersInputActions`) used elsewhere.
- Files: `Assets/Scripts/UI/CharacterSelect/CharSelectEnvironmentController.cs` (lines 83-104)
- Cause: Direct `Input.mousePosition` access instead of using `InputManager`.
- Improvement path: Route mouse position through `InputManager` for consistency. Also applies to `Assets/Scripts/UI/Core/ParallaxBackground.cs` (line 130).

**Nebula Texture Generation is CPU-Bound:**
- Problem: `GenerateNebula()` allocates a `Color[65536]` array and runs nested loops with `Mathf.PerlinNoise()` calls per pixel, executing on the main thread.
- Files: `Assets/Scripts/UI/CharacterSelect/CharSelectEnvironmentController.cs` (lines 133-177)
- Cause: Synchronous per-pixel computation on main thread during hero transitions.
- Improvement path: Pre-generate nebula textures at build time (one per hero theme), or use a compute shader. At 256x256, frame impact is small but GC allocation is unnecessary.

**TitleScreenVFX is Very Large (2000+ lines estimated):**
- Problem: `TitleScreenVFX.cs` is one of the largest files in the codebase with extensive per-frame particle management, lightning effects, ember attraction, mouse interactions, and logo animations.
- Files: `Assets/Scripts/UI/Core/TitleScreenVFX.cs`
- Cause: Monolithic VFX controller handling all title screen effects in one class.
- Improvement path: Extract subsystems (embers, ash, lightning, logo effects) into separate components. Consider object pooling for particle elements.

**StatusEffectManager Update Loop:**
- Problem: `UpdateAllEffects()` runs every frame iterating over all active effects across all targets.
- Files: `Assets/Scripts/Managers/StatusEffectManager.cs` (line 72-75)
- Cause: Per-frame iteration with potential dictionary enumeration.
- Improvement path: The pre-allocated temp lists (`_tempTargetList`, `_tempEffectList`, `_tempRemoveList`) help, but the outer dictionary iteration creates enumerator allocations. Consider using a flat list of active effects instead.

## Fragile Areas

**CharSelectEvents Static Event System:**
- Files: `Assets/Scripts/UI/CharacterSelect/CharSelectEvents.cs`
- Why fragile: All 8 character select controllers rely on this static event bus. If `ClearAll()` is called at the wrong time (e.g., during a transition coroutine), callbacks silently stop. If a new controller is added and forgets to unsubscribe in `OnDisable`, it leaks.
- Safe modification: Always unsubscribe in `OnDisable()`. Always subscribe in `OnEnable()`. Never cache event results across frames.
- Test coverage: No tests exist for the character select event flow.

**CharacterSelectManager Coroutine-Based Async:**
- Files: `Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs` (lines 407-455)
- Why fragile: The embark flow uses coroutines to bridge C# `Task` (from `SaveManager`) to Unity coroutines via busy-wait (`while (!task.IsCompleted) yield return null`). If the `Task` throws, the exception is swallowed by `IsFaulted` check but the player sees no feedback.
- Safe modification: Add user-facing error feedback when save creation fails. Consider using `async void` with `try/catch` or UniTask instead of coroutine-Task bridging.
- Test coverage: No tests for save creation during embark.

**Bootstrap Scene Load Order:**
- Files:
  - `Assets/Scripts/Core/GameBootstrap.cs` (lines 59-89)
  - `Assets/Scripts/Managers/VBSceneManager.cs`
- Why fragile: `GameBootstrap` creates 13+ singleton managers in `Initialize()`, then loads the first scene after a splash delay. If any singleton fails to initialize, downstream managers may access null instances. The `EnsureCriticalManagers()` pattern in `CharacterSelectManager` (lines 124-138) is a workaround for direct scene entry, but creates managers without full `GameBootstrap` initialization (no AudioConfig, no test arena, no FPSCounter).
- Safe modification: Always test both paths: Bootstrap -> scene, and direct scene entry. Add integration tests for manager dependency ordering.
- Test coverage: `GameBootstrap.RunSystemTests()` checks manager presence but not initialization correctness.

**HeroStageController RenderTexture Lifecycle:**
- Files: `Assets/Scripts/UI/CharacterSelect/HeroStageController.cs` (lines 90-127, 389-412)
- Why fragile: Creates a `RenderTexture`, camera, 5 lights, and stage root dynamically in `OnEnable`, destroys them in `OnDisable`. If the component is rapidly toggled (e.g., scene transition race), objects may leak. The static `_placeholderMaterial` survives across scenes and is never explicitly cleaned up.
- Safe modification: Add null guards in `CleanupStage()`. Set `_placeholderMaterial = null` on domain reload. Consider using `[RuntimeInitializeOnLoadMethod]` to clear statics.
- Test coverage: None.

## Scaling Limits

**EventBus Static Events:**
- Current capacity: 50+ events, all global static.
- Limit: No scoping mechanism. As features grow, the EventBus becomes a god-object for cross-system communication. Adding events requires modifying a single 370-line file and maintaining `ClearAllListeners()`.
- Scaling path: Introduce typed event channels (e.g., `EventChannel<T>` ScriptableObjects) or a pub/sub system with automatic scope management.

**GameDatabase In-Memory Data:**
- Current capacity: All monsters, skills, heroes, and items loaded into dictionaries at startup.
- Limit: For the current game scope (4 heroes, ~dozens of monsters), this is fine. At hundreds of monsters with full data, memory usage grows linearly.
- Scaling path: Lazy loading or pagination for collection screens. Currently `GetAllHeroes()` returns a new `List<HeroData>` on each call.

## Dependencies at Risk

**Legacy Input System Usage:**
- Risk: `Input.mousePosition` is used directly in `CharSelectEnvironmentController` and `ParallaxBackground` while the rest of the codebase uses the New Input System (`VeilBreakersInputActions`).
- Impact: Inconsistent input handling. Will break if the legacy Input Manager is disabled in Player Settings.
- Migration plan: Route all input through `InputManager` which wraps `VeilBreakersInputActions`.

**OnGUI-Based FPSCounter:**
- Risk: `FPSCounter` uses `OnGUI()` for rendering, which is the legacy IMGUI system. The rest of the UI uses UI Toolkit.
- Impact: Minor -- it works, but it's inconsistent. OnGUI has higher overhead than UI Toolkit text rendering.
- Migration plan: Rewrite as a UI Toolkit overlay element.

## Missing Critical Features

**No Character Select Tests:**
- Problem: The entire character select system (8 controllers, 1 event bus, 1 ScriptableObject type) has zero test coverage.
- Blocks: Confident refactoring of the character select rebuild.

**No Integration Between Inventory/MonsterCollection and SaveManager:**
- Problem: `InventoryController` and `MonsterCollectionController` use placeholder data (hardcoded lists, `maxCapacity = 100`). They do not load from or save to the `SaveManager`.
- Blocks: Functional inventory and collection screens. Save/load roundtrip for player inventory.

**Settings Panel Not Wired to Main Menu:**
- Problem: `MainMenuController.OnSettingsClicked()` has a `TODO: Show settings panel overlay` comment. Settings panel exists as a separate controller but is not displayed.
- Blocks: Players cannot change settings from the main menu.

## Test Coverage Gaps

**Character Select System:**
- What's not tested: Hero navigation, theme switching, embark flow, save creation, carousel generation, stat population, all event-driven controller communication.
- Files: `Assets/Scripts/UI/CharacterSelect/` (8 files, 0 tests)
- Risk: Regression during the planned Character Select Rebuild.
- Priority: High (active development target)

**Save System Integration:**
- What's not tested: End-to-end save/load cycle through `SaveManager` -> `SaveFileHandler` with encryption, compression, and atomic writes. `SaveSystemTests.cs` exists but scope is unclear.
- Files: `Assets/Scripts/Managers/SaveManager.cs`, `Assets/Scripts/Managers/SaveFileHandler.cs`
- Risk: Data loss or corruption. The encryption/compression pipeline is complex.
- Priority: High

**UI Controllers (Inventory, MonsterCollection, Dialogue):**
- What's not tested: All UI controller interactions, data binding, search/filter, item usage flow.
- Files: `Assets/Scripts/UI/Menus/InventoryController.cs`, `Assets/Scripts/UI/Menus/MonsterCollectionController.cs`, `Assets/Scripts/UI/Menus/VERADialogueController.cs`
- Risk: Silent failures when these screens are eventually wired to real data.
- Priority: Medium

**Audio System:**
- What's not tested: `AudioBattleIntegration`, `AudioTriggers`, `MusicManager` crossfading, `VERAVoiceController` playback. `AudioTests.cs` exists but scope is unclear.
- Files: `Assets/Scripts/Audio/` (8 files)
- Risk: Audio glitches, missing sounds, memory leaks from unclipped audio sources.
- Priority: Low

---

*Concerns audit: 2026-02-21*
