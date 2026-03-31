# Codebase Concerns

**Analysis Date:** 2026-03-30

## Summary

128 C# scripts (55,233 lines), 18 test files (6,894 lines). Three prior deep scans identified 154+ issues. Approximately 16 of 39 prioritized fixes are done (41%). The codebase has strong foundational patterns (EventBus, SingletonMonoBehaviour, ErrorLogger) but carries significant tech debt in memory management, data validation, and repository hygiene.

---

## CRITICAL Severity

### CRIT-01: Git LFS Not Installed (Repository Bloat)

- **Issue:** 222+ binary files (184 PNGs, 24 GLBs, 3 MP4s, audio) are committed as raw git objects. The GLB models alone total ~610MB. `.gitattributes` marks binaries as `binary` but has no LFS filter lines, despite the CI workflow referencing `lfs: true`.
- **Files:** `.gitattributes` (no LFS filter rules), `.github/workflows/unity-ci.yml`
- **Impact:** Clone time is extreme. Every git operation touches hundreds of MB. Forking is impractical. CI builds fail silently (binary files not fetched via LFS pointer).
- **Fix approach:** Install Git LFS, add filter rules to `.gitattributes`, run `git lfs migrate import --include="*.png,*.mp4,*.mp3,*.fbx,*.glb,*.wav" --everything`. This is a destructive history rewrite -- coordinate with all contributors.

### CRIT-02: Multiple GLB Model Versions Bloating Assets (~500MB wasted)

- **Issue:** Each hero/monster has 3-4 versioned GLB models (model_v1_pbr.glb through model_v4_pbr.glb) all committed. Each is ~27-29MB. Only the latest version is needed at runtime.
- **Files:** `Assets/Art/Models/Heroes/Nyx/model_v1_pbr.glb`, `Assets/Art/Models/Heroes/Orion/model_v{1,2,3}_pbr.glb`, `Assets/Art/Models/Heroes/Seraphina/model_v1_pbr.glb`, `Assets/Art/Models/Monsters/Bloodshade/model_v{1,2,3}_pbr.glb`, `Assets/Art/Models/Monsters/Grimthorn/model_v{1,2,3}_pbr.glb`, `Assets/Art/Models/Monsters/Voltgeist/model_v{1,2,3}_pbr.glb`
- **Impact:** ~500MB of dead model iterations in the build. Increases build size and asset import time.
- **Fix approach:** Keep only the latest `model_v4_pbr.glb` per character. Delete older versions. Rename surviving file to `model.glb` for consistency.

### CRIT-03: Save Encryption Key Fragility

- **Issue:** The save encryption key is derived via PBKDF2 with a hardcoded salt (`VeilBreakers_SaveKeySalt_v1`). The device-specific component lives in PlayerPrefs. If PlayerPrefs is cleared (app data clear, reinstall, platform migration), all saves become permanently undecryptable.
- **Files:** `Assets/Scripts/Managers/SaveFileHandler.cs:46-47`
- **Impact:** Data loss on reinstall or platform migration. Players lose all progress.
- **Fix approach:** SESSION_HANDOFF.md marks this as fixed (commit d77ec1b) with a backup file. Verify the recovery flow actually works: clear PlayerPrefs, attempt to load a save, confirm the file-based fallback key is used.

### CRIT-04: OnApplicationQuit Does Not Persist Playtime to Disk

- **Issue:** `SaveManager.OnApplicationQuit()` calls `UpdatePlaytime()` which updates the in-memory `_currentSave.playtimeSeconds` but never writes to disk. Playtime accumulated since last save is lost on every quit.
- **Files:** `Assets/Scripts/Managers/SaveManager.cs:104-109`
- **Impact:** Playtime tracking is inaccurate. Repeated short sessions lose all time data.
- **Fix approach:** Call a synchronous quick-save in `OnApplicationQuit()` after updating playtime, or persist just the playtime delta to a lightweight sidecar file.

### CRIT-05: Brand Matrix Asymmetry Violations

- **Issue:** The brand effectiveness matrix in `BrandSystem.cs` has bidirectional inconsistencies. Per the spec, if A is strong against B, B must be weak against A. Verified violations:
  - DREAD strong vs GRACE, but GRACE weak list is `[SAVAGE, VENOM]` -- DREAD not listed. GRACE takes neutral damage from DREAD instead of resisted.
  - MEND strong vs LEECH, but LEECH weak list is `[SURGE, VENOM]` -- MEND not listed. LEECH takes neutral damage from MEND instead of resisted.
- **Files:** `Assets/Scripts/Systems/BrandSystem.cs:31-44`
- **Impact:** Combat damage calculations are incorrect for affected brand matchups. The entire 10-brand balance is compromised.
- **Fix approach:** Audit every pair in the matrix. For each `{A strong vs B}`, verify `{B weak vs A}`. Correct the matrix. Run `Assets/Tests/EditMode/BrandSystem_EditModeTests.cs` to validate all 100 matchups. Add a test that programmatically verifies bidirectionality.

---

## HIGH Severity

### HIGH-01: TitleScreenVFX is a 3,145-Line God Class

- **Issue:** `TitleScreenVFX.cs` is by far the largest file (3,145 lines). It creates 140+ embers, 40 micro-sparks, 16 ash particles, smoke wisps, lightning, grunge overlays, and a portal background -- all as individual `VisualElement` instances. It makes 11+ `Resources.Load` calls. Each button click spawns 96 new elements.
- **Files:** `Assets/Scripts/UI/Core/TitleScreenVFX.cs`
- **Impact:** Heavy GC pressure on scene load. Potential frame spikes on low-end hardware. Extremely difficult to maintain or modify.
- **Fix approach:** Extract particle systems into separate classes (EmberSystem, SmokeSystem, etc.). Pool VisualElements. Replace `Resources.Load` calls with UIAssets ScriptableObject references. Cap total element count.

### HIGH-02: Resources.Load Used Extensively Instead of Asset References

- **Issue:** 37+ `Resources.Load` calls across the codebase despite having `UIAssets` and `GameDataAssets` ScriptableObjects designed to eliminate them. The worst offenders are `TitleScreenVFX.cs` (11 calls), `MoltenButtonVFX.cs` (5+ calls), `MainMenuController.cs` (4 calls).
- **Files:** `Assets/Scripts/UI/Core/TitleScreenVFX.cs:334-379,881,933,1089`, `Assets/Scripts/UI/Core/MoltenButtonVFX.cs:152-184,459`, `Assets/Scripts/UI/Menus/MainMenuController.cs:693-695,1147`, `Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs:295`, `Assets/Scripts/UI/CharacterSelect/CharSelectEnvironmentController.cs:175`
- **Impact:** Assets in `Resources/` cannot be stripped by Unity's build pipeline. Increases build size. Load-time spikes on first access.
- **Fix approach:** Migrate all `Resources.Load` calls to reference assets through `UIAssets` or `GameDataAssets` ScriptableObjects. Remove files from `Resources/` once no longer needed there.

### HIGH-03: Duplicate Type Definitions (SkillData, ItemData)

- **Issue:** `SkillData` and `ItemData` are defined in two places: `Assets/Scripts/Core/GameDataTypes.cs` (as nested classes inside `GameDataTypes`) and `Assets/Scripts/Data/SkillData.cs` / `Assets/Scripts/Data/ItemData.cs` (as standalone classes in `VeilBreakers.Data` namespace). `GameDatabase.cs` also has private wrapper classes.
- **Files:** `Assets/Scripts/Core/GameDataTypes.cs:47,68`, `Assets/Scripts/Data/SkillData.cs:12`, `Assets/Scripts/Data/ItemData.cs:12`, `Assets/Scripts/Core/GameDatabase.cs:603,615`
- **Impact:** Ambiguous type resolution. JSON deserialization may target the wrong type. Confusing for developers.
- **Fix approach:** Remove the nested `GameDataTypes.SkillData` and `GameDataTypes.ItemData` classes. Keep only the standalone `VeilBreakers.Data` versions. Update `GameDataTypes` wrapper lists to reference the canonical types.

### HIGH-04: PathSystem Returns Shared Mutable Buffer

- **Issue:** `PathSystem.GetPathBonuses()` returns a `[ThreadStatic]` shared `Dictionary<Stat, float>` that is cleared and repopulated on every call. Any caller that caches the reference gets corrupted data on the next call.
- **Files:** `Assets/Scripts/Systems/PathSystem.cs:27-73`
- **Impact:** Latent bug. Currently `ApplyPathBonus` calls it inline and reads immediately, so it works -- but any refactor that stores the reference triggers data corruption.
- **Fix approach:** Return a new dictionary, or change the API to `TryGetBonus(Path, Stat, float pathLevel, out float bonus)` to avoid allocation entirely.

### HIGH-05: Cleanse Sort Direction May Be Incorrect

- **Issue:** `StatusEffectManager.Cleanse()` sorts debuffs by `cleansePriority` using an insertion sort. The comment says "descending" but the sort compares `(compareItem.priority <= keyPriority)`, producing ascending order. If priority 0 = most important to cleanse, the sort is wrong.
- **Files:** `Assets/Scripts/Managers/StatusEffectManager.cs:312-327`
- **Impact:** Cleanse abilities (GRACE brand) may remove the wrong debuffs.
- **Fix approach:** Clarify the priority convention in `StatusEffectData`. Add a unit test that verifies the highest-priority debuff is cleansed first.

### HIGH-06: GameManager.ChangeState(Paused) Bypasses _stateBeforePause

- **Issue:** `PauseGame()` correctly saves `_stateBeforePause` before transitioning. But calling `ChangeState(GameState.Paused)` directly does NOT save the previous state. `ResumeGame()` would then restore the wrong state (defaults to `Exploring`).
- **Files:** `Assets/Scripts/Core/GameManager.cs:108-156`
- **Impact:** Any code path that calls `ChangeState(GameState.Paused)` directly corrupts the pause/resume flow.
- **Fix approach:** Save `_stateBeforePause` inside `ChangeState()` when transitioning to `Paused`, or restrict `ChangeState(Paused)` to only be callable via `PauseGame()`.

### HIGH-07: GameDatabase Returns Mutable Data References

- **Issue:** `GameDatabase` methods like `GetHero()`, `GetMonster()`, `GetSkill()` return direct references to the cached data objects. Callers can mutate template data that persists across the session.
- **Files:** `Assets/Scripts/Core/GameDatabase.cs:329-355`
- **Impact:** A single rogue mutation (e.g., modifying a HeroData stat during combat) permanently corrupts that hero's data until the database is reloaded.
- **Fix approach:** Return deep copies, or make the data classes immutable (readonly properties, no public setters).

### HIGH-08: MonsterData Unsafe Enum Casts from JSON

- **Issue:** `MonsterData` casts integer values from JSON directly to enum types (e.g., `(Brand)intValue`) without `Enum.IsDefined` validation. Malformed or out-of-range values produce invalid enum states.
- **Files:** `Assets/Scripts/Data/MonsterData.cs:108-116`
- **Impact:** Corrupted save files or hand-edited JSON could crash the brand/synergy system.
- **Fix approach:** Validate all enum casts with `Enum.IsDefined()`. Return a default value (e.g., `Brand.NONE`) for invalid inputs.

### HIGH-09: SaveData.ValidateAndRepair Has Validation Gaps

- **Issue:** `ValidateAndRepair()` validates heroId, heroLevel, and path enum, but does NOT validate: individual monster level ranges, monster brand validity, monster monsterId existence, HP/MP bounds, or inventory item quantity ranges.
- **Files:** `Assets/Scripts/Data/SaveData.cs:229-316`
- **Impact:** Corrupted or tampered save data passes validation and creates runtime errors or exploitable states.
- **Fix approach:** Add per-monster validation in `ValidateMonsterList()`: clamp level to [1, 100], validate monsterId format, ensure HP > 0, validate brand enum. Add per-item validation for quantity > 0.

### HIGH-10: Audio Source Pool Never Shrinks

- **Issue:** `AudioManager` grows its AudioSource pool but never reclaims sources after heavy combat. `MusicManager` creates a new AudioSource for each crossfade transition and never destroys old ones.
- **Files:** `Assets/Scripts/Audio/AudioManager.cs`, `Assets/Scripts/Audio/MusicManager.cs`
- **Impact:** Memory leak proportional to combat duration. Extended play sessions accumulate orphaned AudioSources.
- **Fix approach:** Implement pool shrinking with a high-water mark. MusicManager should reuse two AudioSources for crossfading (A/B pattern).

### HIGH-11: Remaining Unfixed Issues from Prior Audit (23 of 39)

- **Issue:** The SESSION_HANDOFF.md tracks 39 prioritized fixes. Only 16 are complete. Key unfixed items include:
  - FIX 12: SoulSwarmVFX double mouse callback (memory leak)
  - FIX 13-17: 5x GeometryChangedEvent leaks (partially fixed -- investigation shows most now unregister in their callbacks, but registration in OnEnable without unregistration in OnDisable is still a concern for components that toggle)
  - FIX 18: SaveData validation gaps
  - FIX 21: MonsterData unsafe enum casts
  - FIX 22: Mutable Party list
  - FIX 25-26: AI null target issues
  - FIX 29: Combatant events not cleared
  - FIX 31-33, 36: CharSelect polish items
  - FIX 37-39: Repo cleanup
- **Files:** `.planning/SESSION_HANDOFF.md` (full tracker)
- **Impact:** Known bugs remain in production code.
- **Fix approach:** Continue working through the fix list by tier priority. Tier 2 (memory leaks) and Tier 3 (robustness) are the next targets.

---

## MEDIUM Severity

### MED-01: Video Files Duplicated in StreamingAssets

- **Issue:** `background_video.mp4` exists in both `Assets/Art/UI/MainMenu/` (6MB) and `Assets/StreamingAssets/` (15MB), plus a reversed version (18MB). Total: 39MB of video for a single background.
- **Files:** `Assets/Art/UI/MainMenu/background_video.mp4`, `Assets/StreamingAssets/background_video.mp4`, `Assets/StreamingAssets/background_video_reversed.mp4`
- **Impact:** Build size inflation. The Art/UI version may be unused.
- **Fix approach:** Determine which path is used at runtime. Delete the unused copy. Consider whether the reversed video can be played in reverse programmatically.

### MED-02: 8 Redundant Audit/Scan Reports in Docs/

- **Issue:** Docs/ contains 8 overlapping scan/audit reports from prior sessions, plus 7 overlapping CharSelect design docs. Many are superseded by `FULL_CODEBASE_DEEP_SCAN.md`.
- **Files:** `Docs/BUG_AND_OPTIMIZATION_REPORT.md`, `Docs/BUG_SCAN_REPORT.md`, `Docs/CODEBASE_AUDIT_REPORT.md`, `Docs/CODE_AUDIT_REPORT.md`, `Docs/CRITICAL_FIXES_NEEDED.md`, `Docs/FINAL_SCAN_REPORT.md`, `Docs/CHARSELECT_DEEP_SCAN.md`, `Docs/CHARSELECT_FIX_PROMPT.md`
- **Impact:** Confusing for developers. Contradictory fix statuses across documents.
- **Fix approach:** Archive old reports to `Docs/archive/scans/`. Keep only `FULL_CODEBASE_DEEP_SCAN.md` and `FULL_CODEBASE_FIX_PROMPT.md` as canonical references.

### MED-03: No Automated Test Runner in CI

- **Issue:** 18 test files exist (9 EditMode, 2 PlayMode, 7 RuntimeTests) totaling 6,894 lines. But RuntimeTests are MonoBehaviour-based (e.g., `SaveSystemTests : MonoBehaviour`) rather than using NUnit/UnityTest attributes, so they cannot be run by Unity Test Runner automatically.
- **Files:** `Assets/Tests/RuntimeTests/*.cs`, `Assets/Tests/EditMode/*.cs`, `Assets/Tests/PlayMode/*.cs`
- **Impact:** Tests exist but are not enforced. Regressions can be committed without detection.
- **Fix approach:** Convert RuntimeTests to proper NUnit-based PlayMode tests using `[UnityTest]` attribute. Verify CI workflow runs Unity Test Runner.

### MED-04: StatusEffectManager Shared Temp Buffer Re-entrance Risk

- **Issue:** `_tempEffectList` is reused across `RemoveEffectsOfType()`, `RemoveAllEffects()`, `Cleanse()`, and shield/break-on-damage logic. If any removal triggers a callback that calls back into the manager, the buffer is corrupted.
- **Files:** `Assets/Scripts/Managers/StatusEffectManager.cs:42-45`
- **Impact:** Re-entrant calls during effect removal could produce incorrect behavior (wrong effects removed, effects skipped).
- **Fix approach:** Use separate buffers for each operation, or add a re-entrance guard flag.

### MED-05: Deprecated Legacy Status Effect Events Still in EventBus

- **Issue:** `OnStatusApplied` and `OnStatusRemoved` (legacy events using `StatusEffect` enum) are marked `[Obsolete]` but still exist with fire methods and `#pragma warning` suppression. The new system uses `OnStatusEffectApplied`/`OnStatusEffectRemoved` with `StatusEffectType`.
- **Files:** `Assets/Scripts/Core/EventBus.cs:92-103`
- **Impact:** Confusion about which events to subscribe to. Dead code clutter.
- **Fix approach:** Search for any remaining subscribers to `OnStatusApplied`/`OnStatusRemoved`. If none, remove them. If some remain, migrate them.

### MED-06: VERASystem Uses Manual Singleton Instead of SingletonMonoBehaviour

- **Issue:** `VERASystem` implements its own singleton pattern instead of inheriting from `SingletonMonoBehaviour<T>`, which handles thread safety, DontDestroyOnLoad, and duplicate detection.
- **Files:** `Assets/Scripts/Systems/VERASystem.cs:95-104`
- **Impact:** OnDestroy may not unsubscribe events properly. Potential for duplicate instances.
- **Fix approach:** Refactor to inherit from `SingletonMonoBehaviour<VERASystem>`.

### MED-07: Large Files Indicating Complexity Issues

- **Issue:** 9 files exceed 800 lines, suggesting single-responsibility violations:
  - `TitleScreenVFX.cs`: 3,145 lines
  - `MainMenuController.cs`: 1,529 lines
  - `SettingsPanelController.cs`: 1,453 lines
  - `CharacterSelectManager.cs`: 1,307 lines
  - `GambitCondition.cs`: 1,194 lines
  - `MoltenButtonVFX.cs`: 1,169 lines
  - `InventoryController.cs`: 1,125 lines
  - `MonsterCollectionController.cs`: 1,025 lines
  - `CharSelectVisualEnhancer.cs`: 972 lines
  - (Note: `VeilBreakersInputActions.cs` at 1,351 lines is auto-generated, acceptable)
- **Files:** Listed above
- **Impact:** Hard to maintain, test, and debug. Higher risk of regressions.
- **Fix approach:** Extract sub-responsibilities into helper classes. VFX files should use component composition. UI controllers should delegate to sub-controllers.

### MED-08: SynergySystem Anti-Synergy Threshold May Be Too Punishing

- **Issue:** Anti-synergy triggers when `weakCount > strongCount`. With a party of 3 monsters, having just 1 weak brand and 0 strong brands triggers ANTI (1.5x corruption gain, ANTI defense penalty of -8%).
- **Files:** `Assets/Scripts/Systems/SynergySystem.cs:80-81`
- **Impact:** Players who don't min-max party composition are penalized. Casual play feels punishing.
- **Fix approach:** Consider requiring `weakCount >= 2` for ANTI, or scaling the penalty proportionally. Flag for game design review.

---

## LOW Severity

### LOW-01: Camera.main Without Lazy Caching

- **Issue:** `RadialMenuController.cs:97` caches `Camera.main` in `Start()` only. If the camera spawns later, it remains null.
- **Files:** `Assets/Scripts/Commands/RadialMenuController.cs:97`
- **Impact:** Radial menu breaks if camera is created after RadialMenuController.
- **Fix approach:** Use lazy caching: `_mainCamera ??= Camera.main` at point of use.

### LOW-02: FPSCounter String Allocation on Every Update

- **Issue:** String interpolation allocates on every FPS change.
- **Files:** `Assets/Scripts/Utils/FPSCounter.cs:73`
- **Impact:** Minor GC pressure in debug builds.
- **Fix approach:** Pre-bake a string array for common FPS values (0-999).

### LOW-03: ObjectPool Has No Maximum Size

- **Issue:** `ObjectPool<T>` grows unboundedly. A runaway spawn loop could exhaust memory.
- **Files:** `Assets/Scripts/Utils/ObjectPool.cs:150`
- **Impact:** Theoretical memory exhaustion in pathological cases.
- **Fix approach:** Add a configurable `maxSize` parameter.

### LOW-04: Dead Code in MainMenuBootstrap

- **Issue:** `_soundCallbacks` list is dead code -- audio was moved to MainMenuController.
- **Files:** `Assets/Scripts/UI/Menus/MainMenuBootstrap.cs:83`
- **Impact:** Code clutter.
- **Fix approach:** Remove the dead field and any references.

### LOW-05: TODO Comments Indicating Incomplete Features

- **Issue:** 3 TODO comments in production code:
  - `GambitEvaluator.cs:115`: "Add evolution_stage to Combatant if not present"
  - `GambitController.cs:270`: "Integrate with ability system when available"
  - `DamageCalculator.cs:81`: "Add luck stat influence" to crit chance
- **Files:** Listed above
- **Impact:** Missing features that may be expected by game design.
- **Fix approach:** Track in issue tracker. Implement or document as "not planned."

### LOW-06: Test Framework Inconsistency

- **Issue:** EditMode tests use NUnit (`[Test]`, `[TestCase]`). PlayMode tests use `[UnityTest]`. RuntimeTests use MonoBehaviour with manual orchestration (`RunAllTestsAsync()`). Three different patterns.
- **Files:** `Assets/Tests/EditMode/*.cs`, `Assets/Tests/PlayMode/*.cs`, `Assets/Tests/RuntimeTests/*.cs`
- **Impact:** RuntimeTests cannot be run from Unity Test Runner.
- **Fix approach:** Standardize on NUnit + `[UnityTest]` for all tests.

---

## Security Considerations

### SEC-01: Save File Tampering

- **Risk:** Save files use AES-CBC + HMAC-SHA256, which is solid. However, the PBKDF2 salt is hardcoded and the device key source (PlayerPrefs) is accessible on rooted devices.
- **Files:** `Assets/Scripts/Managers/SaveFileHandler.cs:46-47`
- **Current mitigation:** HMAC verification prevents casual tampering. Encryption prevents casual reading.
- **Recommendations:** Accept that determined attackers can extract the key. Focus validation in `ValidateAndRepair()` to catch impossible values (negative stats, out-of-range corruption, impossible item quantities) even from tampered saves.

### SEC-02: Input Validation Gaps in GameManager

- **Risk:** `AddCurrency` guards `amount <= 0` (fixed). `SpendCurrency` returns false for `amount <= 0` silently.
- **Files:** `Assets/Scripts/Core/GameManager.cs:376-393`
- **Current mitigation:** Guards exist but are silent.
- **Recommendations:** Add `ErrorLogger.Warn` for unexpected negative amounts to catch bugs during development.

---

## Performance Bottlenecks

### PERF-01: TitleScreenVFX Element Count

- **Problem:** Creates 196+ VisualElements (140 embers + 40 sparks + 16 ash) on scene load, each with per-frame style updates.
- **Files:** `Assets/Scripts/UI/Core/TitleScreenVFX.cs`
- **Cause:** No pooling, no LOD, no screen-space culling.
- **Improvement path:** Pool elements. Reduce count on lower quality settings. Cull off-screen particles.

### PERF-02: WaitForSeconds Allocations in Coroutines

- **Problem:** Several coroutines allocate `new WaitForSecondsRealtime()` on each start. TitleScreenAudio.cs has 10+ such allocations with random durations.
- **Files:** `Assets/Scripts/UI/Core/TitleScreenAudio.cs:160-271`, `Assets/Scripts/UI/Core/ScreenTransition.cs:201-207`
- **Cause:** Random duration waits cannot be cached, but some fixed-duration waits should be.
- **Improvement path:** Cache `WaitForSeconds` for fixed durations. For random durations, consider `yield return null` with manual time tracking in tight loops.

---

## Fragile Areas

### FRAG-01: CharSelect Event System

- **Files:** `Assets/Scripts/UI/CharacterSelect/CharSelectEvents.cs`, `Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs`
- **Why fragile:** 8+ controllers communicate via static events. If `ClearAll()` is called during a coroutine transition, callbacks silently stop. Embark flow bridges Task-based async (SaveManager) to coroutines via busy-wait polling.
- **Safe modification:** Always unsubscribe in `OnDisable()`, subscribe in `OnEnable()`. Test both Bootstrap->scene and direct scene entry paths.
- **Test coverage:** 2 PlayMode tests exist (`CharacterSelect_PlayModeTests.cs`, 147 lines) but coverage is minimal.

### FRAG-02: Bootstrap Scene Load Order

- **Files:** `Assets/Scripts/Core/GameBootstrap.cs`, `Assets/Scripts/Managers/VBSceneManager.cs`
- **Why fragile:** `GameBootstrap` creates 13+ singleton managers in sequence. If any fails, downstream managers access null instances. `CharacterSelectManager` has a workaround (`EnsureCriticalManagers`) for direct scene entry.
- **Safe modification:** Test both Bootstrap->scene and direct scene entry paths. Integration tests should verify manager dependency ordering.
- **Test coverage:** `GameBootstrap.RunSystemTests()` checks manager presence but not initialization correctness.

---

## Test Coverage Gaps

### GAP-01: No Tests for SaveData.ValidateAndRepair

- **What's not tested:** The validation and repair logic that guards against corrupted saves.
- **Files:** `Assets/Scripts/Data/SaveData.cs:229-316`
- **Risk:** Validation gaps (HIGH-09) go undetected. Corrupted saves could crash the game.
- **Priority:** HIGH

### GAP-02: No Tests for EventBus Subscribe/Unsubscribe Lifecycle

- **What's not tested:** Whether `ClearAllListeners()` clears all events. Whether new events added to EventBus are included in the clear method.
- **Files:** `Assets/Scripts/Core/EventBus.cs:298-381`
- **Risk:** Memory leaks from missed event clears after scene transitions.
- **Priority:** HIGH

### GAP-03: No Integration Test for Full Combat Loop

- **What's not tested:** A complete combat flow from BattleStarted through ability execution, status effects, synergy calculation, capture attempt, to BattleEnded.
- **Files:** All files in `Assets/Scripts/Combat/`, `Assets/Scripts/Capture/`, `Assets/Scripts/Systems/`
- **Risk:** Cross-system interactions (brand + synergy + corruption + status effects) may produce unexpected results.
- **Priority:** HIGH

### GAP-04: No Tests for GameManager State Machine

- **What's not tested:** PauseGame/ResumeGame transitions, ChangeState(Paused) without PauseGame, state recovery after multiple pause/resume cycles.
- **Files:** `Assets/Scripts/Core/GameManager.cs:108-156`
- **Risk:** State corruption during gameplay.
- **Priority:** MEDIUM

### GAP-05: No Tests for Audio Memory Management

- **What's not tested:** AudioSource pool growth/shrink behavior, MusicManager crossfade cleanup.
- **Files:** `Assets/Scripts/Audio/AudioManager.cs`, `Assets/Scripts/Audio/MusicManager.cs`
- **Risk:** Slow memory leak during extended play sessions.
- **Priority:** MEDIUM

---

## Fix Priority Summary

| Priority | Category | Item | Effort |
|----------|----------|------|--------|
| P0 | Repo | Git LFS migration (CRIT-01) | 2h |
| P0 | Repo | Remove old model versions (CRIT-02) | 1h |
| P0 | Data | Brand matrix asymmetry fix (CRIT-05) | 1h |
| P1 | Save | OnApplicationQuit save (CRIT-04) | 30m |
| P1 | Save | Verify encryption key recovery (CRIT-03) | 1h |
| P1 | Data | Duplicate type definitions (HIGH-03) | 1h |
| P1 | Combat | Cleanse sort verification (HIGH-05) | 30m |
| P1 | Core | GameManager ChangeState guard (HIGH-06) | 30m |
| P1 | Data | SaveData validation gaps (HIGH-09) | 1h |
| P2 | Perf | TitleScreenVFX refactor (HIGH-01) | 4h |
| P2 | Perf | Resources.Load migration (HIGH-02) | 2h |
| P2 | Audio | AudioSource pool management (HIGH-10) | 1h |
| P2 | Test | Convert RuntimeTests to proper PlayMode (MED-03) | 2h |
| P2 | Test | Add SaveData validation tests (GAP-01) | 1h |
| P2 | Test | Add brand matrix bidirectionality test (GAP-02) | 30m |
| P3 | Cleanup | Archive old scan reports (MED-02) | 30m |
| P3 | Cleanup | Deduplicate video files (MED-01) | 30m |
| P3 | Code | Refactor large files (MED-07) | 8h+ |

---

*Concerns audit: 2026-03-30*
