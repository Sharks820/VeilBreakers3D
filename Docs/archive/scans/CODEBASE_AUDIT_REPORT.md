# VeilBreakers 3D - Complete Codebase Audit Report

**Date:** 2026-02-01
**Audited By:** Claude (Opus 4.5) + Gemini Cross-Validation
**Unity Version:** 2022.3.62f3
**Target:** Unity 6 Migration Readiness

---

## Executive Summary

This audit identified **29 issues** across the VeilBreakers 3D codebase. The project has strong coding conventions but faces significant architectural barriers to Unity 6 migration, primarily the absence of URP and heavy reliance on `Resources.Load`.

| Severity | Count | Key Issues |
|----------|-------|------------|
| Critical/Blocker | 6 | No URP, Resources.Load, Singletons, Missing serialization safety |
| High Priority | 8 | Memory leaks, duplicated code, outdated packages |
| Architectural | 9 | JSON fragility, state decentralization, input system mixing |
| Code Quality | 6 | async void, magic strings, editor code |

---

## Critical Issues (Must Fix Before Unity 6)

### 1. No URP Package - BLOCKER
- **Location:** `Packages/manifest.json`
- **Problem:** Project uses Built-in Render Pipeline, which is deprecated in Unity 6
- **Impact:** Cannot migrate to Unity 6 without full URP conversion
- **Action:** Add `com.unity.render-pipelines.universal` and migrate all shaders/materials

### 2. Resources.Load Anti-Pattern (23 Uses) - CRITICAL
- **Locations:**
  - `GameDatabase.cs` - Core game data
  - `UIAutoSetup.cs` - UI templates and styles
  - `MenuBootstrap.cs` - Menu assets
  - `StatusEffectManager.cs` - All status effects
- **Problem:** Conflicts with Addressables, causes memory bloat and slow startup
- **Action:** Migrate all Resources folder assets to Addressables

### 3. Inconsistent Singletons (18+ Instances) - CRITICAL
- **Problem:** No standardized pattern, mixed naming (`Instance` vs `_instance`), none thread-safe
- **Impact:** Race conditions, cognitive overhead, maintenance difficulty
- **Action:** Create `SingletonMonoBehaviour<T>` base class, refactor all singletons

### 4. No FormerlySerializedAs Usage - CRITICAL
- **Problem:** Zero uses of `[FormerlySerializedAs]` attribute in entire codebase
- **Impact:** ANY field rename will break all prefab and scene references
- **Action:** Add `[FormerlySerializedAs("oldName")]` before any field renames during migration

### 5. Missing VFX Shaders - TEST FAILURE
- **Location:** `Assets/Tests/EditMode/MainMenuAssets_EditModeTests.cs`
- **Problem:** v4.45 deleted 9 VFX shaders but test still expects them
- **Missing Shaders:**
  - VeilBreakers/VFX/BackGlow
  - VeilBreakers/VFX/EnergyPulse
  - VeilBreakers/VFX/CorruptionDrip
  - VeilBreakers/VFX/VeilShimmer
  - VeilBreakers/VFX/SpiritWisp
  - VeilBreakers/VFX/RunicGlow
  - VeilBreakers/VFX/DarkMist
  - VeilBreakers/VFX/HolyLight
  - VeilBreakers/VFX/VoidRift
- **Action:** Either recreate shaders or update test to match v4.45 reality

### 6. Input System Outdated - HIGH
- **Location:** `manifest.json` - `com.unity.inputsystem: 1.7.0`
- **Problem:** Unity 6 requires Input System 1.8+, also has dual input stack (legacy + new)
- **Action:** Upgrade package, fully migrate to new Input System

---

## High-Priority Bugs & Performance

### 7. Memory Leak Risk - EventBus
- **Location:** `EventBus.cs`, all panel controllers
- **Problem:** Static event subscriptions may not be cleaned up on object destruction
- **Action:** Audit all `+=` subscriptions, ensure matching `-=` in `OnDestroy`

### 8. Memory Leak - UI Panels
- **Locations:** `EnemyPanelController.cs`, `AllyPanelController.cs`
- **Problem:** Subscribe to Combatant events but may not unsubscribe before destruction
- **Action:** Add robust unsubscription in `OnDestroy`

### 9. GetComponent in Update
- **Location:** `MainMenuVFXController.cs:24`
- **Code:** `if (_uiDocument == null) _uiDocument = GetComponent<UIDocument>();`
- **Problem:** Uses caching pattern but still called every frame until cached
- **Action:** Move to `Start()` or `OnEnable()`

### 10. GameObject.FindWithTag Repeated
- **Location:** `AudioTriggers.cs`
- **Problem:** Searches for "Player" tag repeatedly instead of caching
- **Action:** Cache reference in `Start()`

### 11. Allocation in Hot Path
- **Location:** `GambitCondition.cs` - `UpdateThreatScores()`
- **Code:** `var keys = new List<Combatant>(_threatScores.Keys);`
- **Problem:** Allocates new List every call, causes GC pressure
- **Action:** Reuse pooled list or iterate differently

### 12. Duplicated UI Panel Code
- **Locations:** `EnemyPanelController`, `AllyPanelController`, `PlayerPanelController`
- **Duplicated Methods:** `IsBuff()`, `SetVisible()`, `SetPortrait()`, `UpdateName()`, `UpdateHP()`, `ClearStatusIcons()`
- **Action:** Extract to base class `CombatantPanelController`

### 13. Duplicated Corruption Presentation Logic
- **Locations:** `EnemyPanelController`, `CombatUIConfig`, `ThemeManager`, `MonsterCollectionController`
- **Problem:** Color/style logic for corruption tiers duplicated in 4+ files
- **Action:** Centralize in `CorruptionUIHelper` or `ThemeManager`

### 14. TextMeshPro Package Deprecated
- **Location:** `manifest.json` - `com.unity.textmeshpro: 3.0.6`
- **Problem:** Unity 6 has TMP built-in, separate package deprecated
- **Action:** Remove package after Unity 6 migration

---

## Architectural Concerns

### 15. Fragile JSON Loading
- **Location:** `GameDatabase.cs`
- **Code:** `"{\"monsters\":" + jsonAsset.text + "}"`
- **Problem:** String concatenation hack, fragile and error-prone
- **Action:** Define proper serializable wrapper classes

### 16. VERASystem State Decentralization
- **Location:** `VERASystem.cs`
- **Problem:** Not in GameBootstrap, manages own state separately from SaveData
- **Action:** Centralize state ownership in GameManager

### 17. Mixed Input Systems
- **Problem:** Project has new Input System package but uses legacy `Input.GetKey` etc.
- **Action:** Full migration to new Input System

### 18. UI Toolkit Workflow Issues
- **Source:** `CRITICAL_FIXES_NEEDED.md`
- **Problem:** Purple screen errors, manual reimports needed
- **Action:** Investigate asset pipeline, eliminate manual workarounds

### 19. Heavy Coroutine Usage
- **Count:** 112 occurrences across 24 files
- **Consideration:** Review for async/await migration opportunities where appropriate

### 20. Instantiate/Destroy Pattern
- **Count:** 174 occurrences across 42 files
- **Consideration:** Implement object pooling for frequently spawned objects

### 21. com.gamelovers.mcp-unity (Custom Git Dependency)
- **Risk:** HIGH - Requires manual Unity 6 compatibility verification
- **Action:** Contact package maintainer or prepare fallback

### 22. com.unity.ugui 1.0.0 (Legacy)
- **Risk:** MEDIUM - Legacy UI system
- **Action:** Migrate to UI Toolkit (already partially done)

### 23. com.unity.xr.management
- **Risk:** MEDIUM - May have breaking changes in Unity 6
- **Action:** Test XR functionality after migration

---

## Code Quality Issues

### 24. async void in Tests
- **Location:** Test scripts
- **Problem:** Should be `async Task` for proper exception handling
- **Severity:** Low (test code only)

### 25. Layer/Tag Magic Strings
- **Count:** 4 occurrences
- **Action:** Define constants

### 26. ExecuteAlways Attribute
- **Location:** `MainMenuVFXController.cs`
- **Impact:** Runs in editor, verify performance is acceptable

### 27. OnValidate/OnDrawGizmos
- **Count:** 9 occurrences
- **Action:** Verify editor-only code excluded from builds

### 28. Rigidbody Interpolation Not Configured
- **Problem:** Default `None` setting may cause jittery movement
- **Action:** Review Rigidbody components, set interpolation for player-visible objects

### 29. Collision Detection Mode Default
- **Problem:** Default `Discrete` mode risks tunneling for fast objects
- **Action:** Review fast-moving Rigidbodies, consider `Continuous` mode

---

## Confirmed Working Well

The following areas were audited and found to be well-implemented:

- SerializeField convention - Correctly used on private fields throughout
- GetComponent patterns - Use null checks and `GetOrAddComponent`
- RequireComponent usage - Properly applied (e.g., `GambitController.cs`)
- No uninitialized public fields - Strong Inspector discipline
- No SendMessage usage - Modern event patterns used
- Core corruption logic - Centralized in `CorruptionSystem.cs`
- ScriptableObjects - Appropriately used for data

---

## Recommended Fix Order

### Phase 1: Immediate (Before Any Migration Work)
1. Fix failing VFX shader test (update test or recreate shaders)
2. Audit all event subscriptions for proper cleanup
3. Cache GameObject.Find results

### Phase 2: Pre-Migration Preparation
1. Add URP package and begin shader migration
2. Upgrade Input System to 1.8+
3. Migrate Resources.Load to Addressables (23 files)
4. Standardize singleton pattern (18+ files)

### Phase 3: During Unity 6 Migration
1. Add `[FormerlySerializedAs]` to any field renames
2. Remove TMP package (use built-in)
3. Complete legacy input migration
4. Consolidate duplicate UI panel code

### Phase 4: Post-Migration Polish
1. Remove Addressables fallback code
2. Clean up deprecated API usage
3. Performance profiling
4. Verify XR functionality

---

## Appendix: Files Requiring Attention

### Critical Files
- `Packages/manifest.json` - Package updates
- `Assets/Scripts/Core/GameDatabase.cs` - Resources.Load, JSON fragility
- `Assets/Scripts/UI/UIAutoSetup.cs` - Resources.Load
- `Assets/Scripts/VERA/VERASystem.cs` - State management
- `Assets/Tests/EditMode/MainMenuAssets_EditModeTests.cs` - Failing test

### High-Priority Files
- `Assets/Scripts/UI/Combat/EnemyPanelController.cs` - Memory leak, duplication
- `Assets/Scripts/UI/Combat/AllyPanelController.cs` - Memory leak, duplication
- `Assets/Scripts/Audio/AudioTriggers.cs` - GameObject.Find caching
- `Assets/Scripts/AI/GambitCondition.cs` - Allocation in Update

---

*Report generated by Claude Code (Opus 4.5) with Gemini cross-validation*
