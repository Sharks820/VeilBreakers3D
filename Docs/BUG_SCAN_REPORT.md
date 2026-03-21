# VeilBreakers 3D - Comprehensive Bug Scan & Optimization Report
**Date:** 2026-03-20 (Updated: 2026-03-21)
**Scanned:** 128 C# files across Assets/Scripts/
**Method:** 9 parallel scanning agents (2 passes) + manual deep-dive of 15+ critical files

---

## Executive Summary

The codebase is **well-architected overall** - combat systems avoid LINQ in hot paths, singletons have proper cleanup, event subscriptions are mostly paired. However, several systemic issues were identified that will impact production performance and maintainability.

| Severity | Count | Category |
|----------|-------|----------|
| CRITICAL | 4 | Performance/Production readiness/Design |
| HIGH | 9 | Performance/Memory/Lifecycle |
| MEDIUM | 8 | Code quality/Risk/Threading |
| LOW | 6 | Style/Minor/Documentation |
| OPTIMIZATION | 7 | Upgrade opportunities |

---

## CRITICAL Issues

### C1. Debug.Log Spam in Production Paths
**Impact:** GC pressure, frame drops in release builds
**Files:** 30 files, 263 total Debug.Log calls

Many Debug.Log calls are NOT gated behind `#if UNITY_EDITOR || DEVELOPMENT_BUILD`:
- `SaveManager.cs` - 23 unconditional calls
- `MigrationRunner.cs` - 11 unconditional calls
- `GameDatabase.cs` - 12 unconditional calls
- `AudioManager.cs` - 7 unconditional calls

Each Debug.Log allocates strings via interpolation (`$"..."`) even if nobody is listening.

**Fix:** Wrap ALL Debug.Log calls in conditional compilation:
```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
Debug.Log($"[SaveManager] Saved slot {slot}");
#endif
```
Or use a centralized logging utility with [Conditional("DEBUG")] attribute.

---

### C2. StatusEffectManager._debugLogging Defaults to TRUE
**File:** `Assets/Scripts/Managers/StatusEffectManager.cs:27`
```csharp
private bool _debugLogging = true; // <-- Should be false
```

Every status effect apply/remove/tick generates a Debug.Log in production. During combat with 10+ effects ticking, this creates massive GC pressure every frame.

**Fix:** Change default to `false`:
```csharp
private bool _debugLogging = false;
```

---

### C3. Resources.Load Proliferation (~40+ calls)
**Impact:** Synchronous loading causes frame hitches, memory not managed
**Worst offenders:**

| File | Calls | Issue |
|------|-------|-------|
| `TitleScreenVFX.cs` | ~15 | Sequential texture loads |
| `MoltenButtonVFX.cs` | ~7 | Cascading fallback chains |
| `MainMenuController.cs` | 3 | Audio clips |
| `CharSelectEnvironmentController.cs` | 1 | `Resources.LoadAll` |
| `CharacterSelectManager.cs` | 1 | `Resources.LoadAll` |

The project already has `UIAssets` and `GameDataAssets` ScriptableObjects for centralized references, but many VFX/UI components still bypass them.

**Fix:** Migrate all Resources.Load calls to use UIAssets/GameDataAssets SO references. Priority: VFX files first (most calls).

---

## HIGH Severity Issues

### H1. Uncached Camera.main Fallbacks
**Files:**
- `EmbarkCinematicController.cs:269` - `Camera.main` in fallback
- `VeilTransitionController.cs:86` - `Camera.main` in fallback

```csharp
var cam = _dollyCamera != null ? _dollyCamera : Camera.main; // Uncached fallback
```

**Fix:** Cache Camera.main in Start/Awake:
```csharp
private Camera _cachedMainCamera;
private void Start() { _cachedMainCamera = Camera.main; }
```

---

### H2. List Allocation in StatusEffectManager Query Methods
**File:** `Assets/Scripts/Managers/StatusEffectManager.cs`

- Line 376: `StealBuffs()` - `new List<StatusEffectInstance>(maxSteal)` every call
- Line 473: `GetEffectsByCategory()` - `return new List<StatusEffectInstance>()` (empty)
- Line 476: `GetEffectsByCategory()` - `new List<StatusEffectInstance>()` for results

**Fix:** Use ListPool<T> or pre-allocated buffers like the _temp lists already used elsewhere in the class.

---

### H3. List.Contains in Hot Path (BattleManager.cs)
**File:** `Assets/Scripts/Combat/BattleManager.cs:662`
```csharp
private bool IsSelectableAlly(Combatant combatant)
{
    return combatant != null
           && combatant.IsAlive
           && combatant != _player
           && _playerParty.Contains(combatant); // O(n) every call
}
```

Called from `HandleCombatantDeath` -> `EnsureActiveAllyValid` -> `SelectFirstLivingAlly`, which iterates the party list and calls IsSelectableAlly per member (O(n^2)).

**Fix:** Add a `HashSet<Combatant> _playerPartySet` maintained alongside `_playerParty`.

---

### H4. SaveManager.OnApplicationPause Blocking Async
**File:** `Assets/Scripts/Managers/SaveManager.cs:97`
```csharp
AutoSaveAsync("app_pause").GetAwaiter().GetResult(); // Blocks main thread!
```

While guarded by `_isSaving || _isLoading` check, this pattern can still deadlock if timing is unlucky. `GetAwaiter().GetResult()` on async code that acquires a SemaphoreSlim will deadlock if the semaphore is contended.

**Fix:** Use fire-and-forget with error catching, or use synchronous save path for pause:
```csharp
_ = AutoSaveAsync("app_pause").ContinueWith(t => {
    if (t.IsFaulted) Debug.LogError($"Auto-save failed: {t.Exception}");
}, TaskScheduler.FromCurrentSynchronizationContext());
```

---

### H5. Closure Allocations in BattleManager.StartBattle
**File:** `Assets/Scripts/Combat/BattleManager.cs:121,130`
```csharp
Action handler = () => HandleCombatantDeath(c); // Closure allocation per combatant
```

Creates N closure allocations (one per combatant) every battle start. While not catastrophic, it's avoidable.

**Fix:** Use a single handler method with sender pattern, or use a dictionary lookup in a shared handler.

---

### H6. MoltenButtonVFX Cascading Resource Fallbacks
**File:** `Assets/Scripts/UI/Core/MoltenButtonVFX.cs:149-181`
```csharp
_lavaBubbleTexture = Resources.Load<Texture2D>("VFX/ParticleTextures/dirt 2");
if (_lavaBubbleTexture == null)
    _lavaBubbleTexture = Resources.Load<Texture2D>("VFX/ParticleTextures/ink splat");
```

7 cascading fallback chains. Each failed Resources.Load scans the entire Resources folder tree.

**Fix:** Use SerializeField references or UIAssets SO.

---

## MEDIUM Severity Issues

### M1. No Save Data Range Validation
**File:** `Assets/Scripts/Data/SaveData.cs:164-170`

`SaveData.Validate()` only null-coalesces lists. No validation of value ranges:
- heroLevel could be negative or impossibly high
- corruption could be outside 0-100
- playtimeSeconds could be negative
- currency could be negative

**Fix:** Add range clamping in Validate():
```csharp
heroLevel = Mathf.Clamp(heroLevel, 1, 99);
corruption = Mathf.Clamp(corruption, 0, 100);
playtimeSeconds = Mathf.Max(playtimeSeconds, 0);
currency = Mathf.Max(currency, 0);
```

---

### M2. Hardcoded Encryption Salt
**File:** `Assets/Scripts/Managers/SaveFileHandler.cs:45`
```csharp
private static readonly byte[] PBKDF2_SALT = Encoding.UTF8.GetBytes("VeilBreakers_SaveKeySalt_v1");
```

The PBKDF2 salt for save encryption is hardcoded. Anyone with the binary can derive the encryption key and tamper with save files. Not a security issue for a single-player game, but worth noting for leaderboard/achievement integrity.

---

### M3. Resources.LoadAll in Character Select
**Files:**
- `CharSelectEnvironmentController.cs:175` - `Resources.LoadAll<HeroDisplayConfig>()`
- `CharacterSelectManager.cs:277` - `Resources.LoadAll<HeroThemeConfig>()`

LoadAll is synchronous and loads every asset in the folder. Could cause visible hitches when entering character select.

**Fix:** Use explicit ScriptableObject array references.

---

### M4. SemaphoreSlim Leak in SaveManager
**File:** `Assets/Scripts/Managers/SaveManager.cs:45`

SemaphoreSlim implements IDisposable but is never disposed. The OnDestroy comment explains why (operations in flight), but on the singleton's actual destruction, the semaphore should be disposed.

**Fix:** Add conditional dispose in a deferred cleanup or accept the minor leak.

---

### M5. Event Handler Pattern Inconsistency
Some systems use ClearAll() to bulk-unsubscribe (AudioManager, MusicManager, VERAVoiceController), while others unsubscribe individually (BattleManager, TestArenaManager). The ClearAll() approach is fragile - if a new subscriber is added but the ClearAll caller doesn't know about it, that subscriber becomes orphaned.

**Fix:** Standardize on individual unsubscription. Document the pattern in coding guidelines.

---

### M6. Missing [RequireComponent] Attributes
Multiple MonoBehaviours call `GetComponent<T>()` in Start/Awake but don't declare dependencies:
- `CharSelectFocusManager.cs:104` - needs AudioSource
- `HoldToEmbarkController.cs:70` - needs AudioSource
- `GambitController.cs:77` - needs Combatant

**Fix:** Add `[RequireComponent(typeof(AudioSource))]` etc.

---

## LOW Severity Issues

### L1. Singleton _isQuitting Reset Order
**File:** `SingletonMonoBehaviour.cs:45`

`_isQuitting = false` is reset in Awake() of every singleton. Safe for Editor but could theoretically race during quit if multiple singletons are being destroyed and recreated.

---

### L2. foreach Over _tempTargetList (StatusEffectManager.cs:665)
Uses `foreach` instead of `for` loop. While List<T>.Enumerator is a struct and doesn't allocate, `for` loop is marginally faster and consistent with the rest of the file's style.

---

### L3. AudioTests.cs Has 91 Debug.Log Calls
**File:** `Assets/Scripts/Audio/AudioTests.cs`

Test class with excessive logging. Not a production issue since tests don't ship, but clutters test output.

---

### L4. Naming Convention Minor Deviations
- `SLOT_PREFIX`, `AUTO_FILENAME` in SaveManager.cs use SCREAMING_SNAKE instead of kCamelCase convention
- Some constants use `k` prefix correctly, others don't

---

---

## SECOND PASS - Additional Findings

### C4. Dual StatusEffect Systems (Design Bug)
**File:** `Assets/Scripts/Combat/Combatant.cs` + `Assets/Scripts/Managers/StatusEffectManager.cs`
**Severity:** CRITICAL

Combatant has its own `_statusEffects` list (lines 50, 387-424), AND StatusEffectManager tracks effects separately in `_effectsByTarget`. These two systems are **not synchronized**:

- `Combatant.ApplyStatus()` adds to local `_statusEffects` list
- `StatusEffectManager.ApplyEffect()` adds to its own `_effectsByTarget` dictionary
- Neither system knows about the other's state

This means effects can be "applied" in one system but invisible to the other, causing:
- Ghost effects (showing in UI but not ticking)
- Missed effects (ticking but not queryable)
- Double effects if both paths are used

**Fix:** Deprecate `Combatant.ApplyStatus/RemoveStatus/HasStatus` and route ALL status effect operations through `StatusEffectManager`. Or make Combatant's methods delegate to StatusEffectManager.

---

### H7. GeometryChangedEvent Callbacks Never Unregistered (5 files)
**Files:** MoltenButtonVFX.cs, ParallaxBackground.cs, MenuVFXController.cs, MoltenVeinVFX.cs, SoulSwarmVFX.cs

All register `GeometryChangedEvent` callbacks on `_uiDocument.rootVisualElement` but never unregister them. Only `TitleScreenVFX.cs` properly unregisters (line 1179).

**Fix:** Add cleanup in OnDisable/OnDestroy:
```csharp
_uiDocument?.rootVisualElement?.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
```

---

### H8. ButtonVFXHelper - 15+ RegisterCallback Without Cleanup
**File:** `Assets/Scripts/UI/Controls/ButtonVFXHelper.cs`

~15 `RegisterCallback` calls using lambdas across various methods (PointerDown, PointerUp, MouseEnter, Click, Focus, etc.). No corresponding `UnregisterCallback` anywhere. Lambda captures prevent GC if buttons are recreated.

**Fix:** Store callback references and unregister on element detach, or use non-lambda methods.

---

### H9. Uncached WaitForSeconds in Coroutines
**Files:**
- `VERAVoiceController.cs:327` - `new WaitForSeconds(_glitchDuration)` (per-glitch allocation)
- `AudioManager.cs:260,603` - `new WaitForSeconds(delay)` (per-fade allocation)
- `HeroMonsterPairPreview.cs:166,177,186` - 3 separate `new WaitForSeconds` per preview cycle

**Fix:** Cache as static/instance fields where duration is constant. For variable durations, use a WaitForSeconds cache dictionary or accept the allocation.

---

### M7. CaptureManager.RemoveMonsterFromBattle - Re-entrant Event Risk
**File:** `Assets/Scripts/Capture/CaptureManager.cs:728`
```csharp
monster.TakeDamage(monster.MaxHP + 1); // Force death
```

Forces death by dealing MaxHP+1 damage. This triggers `OnDeath` events on the Combatant, which triggers `HandleCombatantDeath` in BattleManager, which fires `OnCombatantDeath` events. If CaptureManager also listens to death events, this creates a re-entrant callback chain that could corrupt state.

**Fix:** Add a `_removingMonster` guard flag, or use a dedicated `ForceRemove()` method that bypasses the damage/death event chain.

---

### M8. EventBus.ClearAllListeners Race with Subscribers
**File:** `Assets/Scripts/Core/EventBus.cs:15`

`[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` calls `ClearAllListeners()` during domain reload. ClearAllListeners is also public. If called during scene transitions, subscribers that haven't re-subscribed yet via OnEnable lose their registrations silently.

**Fix:** ClearAllListeners should only be called via the static initializer attribute. Remove or restrict public access, or add a warning log if called at unexpected times.

---

### L5. SynergySystem ANTI Tier is All-or-Nothing
**File:** `Assets/Scripts/Systems/SynergySystem.cs:64-68`

A single weak-brand party member forces the entire party to ANTI synergy tier. This is by design but creates a cliff effect in party building. Worth considering a graduated approach for balance.

---

### L6. DamageCalculator Corruption Logic is Non-Obvious
**File:** `Assets/Scripts/Combat/DamageCalculator.cs:74-75`
```csharp
damage *= (1f + attackerCorruptionMod);
damage *= (1f - defenderCorruptionMod);
```

When `defenderCorruptionMod` is negative (Corrupted=-0.10, Abyssal=-0.20), the formula becomes:
- Abyssal defender: `damage *= (1 - (-0.20)) = 1.20` (takes 20% MORE damage)

This is **correct per spec** (Abyssal has -20% stat penalty), but the double-negative makes the code confusing. Should add a comment explaining this.

---

## Optimization Opportunities

### O1. Centralize All Resource Loading
**Priority: HIGH | Effort: MEDIUM**

Migrate remaining ~40 Resources.Load calls to UIAssets/GameDataAssets. This eliminates:
- Synchronous loading hitches
- Hard-to-track resource dependencies
- Build size bloat from Resources folder

### O2. Conditional Compilation for All Logging
**Priority: HIGH | Effort: LOW**

Create a `VBDebug` utility class:
```csharp
public static class VBDebug
{
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Log(string message) => Debug.Log(message);
}
```

Replace all 263 Debug.Log calls. Eliminates ALL string allocation in release builds.

### O3. ListPool for StatusEffect Queries
**Priority: MEDIUM | Effort: LOW**

Implement simple ListPool<T> and use in GetEffectsByCategory, StealBuffs. Eliminates per-call allocations.

### O4. HashSet for Party Membership Checks
**Priority: MEDIUM | Effort: LOW**

Add `HashSet<Combatant>` to BattleManager for O(1) Contains checks.

### O5. Async Resource Loading for VFX
**Priority: MEDIUM | Effort: MEDIUM**

Use Addressables or async Resources.LoadAsync for VFX textures. Eliminates frame hitches during initialization.

### O6. Object Pooling for Combat Closures
**Priority: LOW | Effort: LOW**

Replace per-combatant closure allocations in BattleManager.StartBattle with a single shared handler using Dictionary lookup.

### O7. Batch Save Metadata Loading
**Priority: LOW | Effort: LOW**

`GetBestNewGameSlotAsync()` (SaveManager.cs:414) loads metadata sequentially:
```csharp
for (int i = 0; i < kSlotCount; i++)
    manual[i] = await GetSlotMetadataAsync(i);
```
Should use `Task.WhenAll` like `GetAllSlotsMetadataAsync` already does.

---

## What's Already Done Well

The codebase demonstrates several strong patterns that should be maintained:

1. **Manual loops over LINQ in hot paths** - BattleManager, StatusEffectManager consistently use `for` loops with explicit null checks
2. **Pre-allocated temp lists** - StatusEffectManager uses `_tempTargetList`, `_tempEffectList`, `_tempRemoveList` to avoid GC
3. **Pre-allocated brand buffer** - BattleManager uses `_brandBuffer` array for synergy calculation
4. **Proper event cleanup** - BattleManager tracks handlers in `_deathHandlers` dictionary
5. **Atomic file writes** - SaveFileHandler uses temp-file-rename pattern
6. **Rolling backup saves** - Two auto-save slots prevent single-point-of-failure
7. **HMAC integrity checks** - Save files validated before deserialization
8. **Conditional debug logging** - BattleManager uses `#if UNITY_EDITOR || DEVELOPMENT_BUILD` (should be expanded to all files)
9. **Early-out patterns** - StatusEffectManager checks `_effectsByTarget.Count == 0` before iterating
10. **No empty catch blocks** - Zero instances found across entire codebase

---

## Recommended Priority Actions

### Immediate (Before Next Build)
1. **C4** - Unify dual StatusEffect systems (Combatant._statusEffects vs StatusEffectManager)
2. **C2** - Change `StatusEffectManager._debugLogging` default to `false`
3. **C1** - Gate SaveManager/GameDatabase/MigrationRunner Debug.Logs behind conditional compilation

### Short-Term (This Sprint)
4. **C1** - Create `VBDebug` utility and migrate all 263 Debug.Log calls
5. **C3** - Migrate TitleScreenVFX and MoltenButtonVFX Resources.Load calls to UIAssets
6. **H7** - Unregister GeometryChangedEvent callbacks in 5 VFX controllers
7. **H1** - Cache Camera.main in EmbarkCinematicController and VeilTransitionController
8. **M1** - Add save data range validation
9. **M7** - Add re-entrancy guard in CaptureManager.RemoveMonsterFromBattle

### Medium-Term (Next Sprint)
10. **C3** - Migrate ALL remaining Resources.Load calls to SO references
11. **H8** - Fix ButtonVFXHelper callback leak pattern
12. **O3** - Implement ListPool for StatusEffectManager queries
13. **O4** - Add HashSet for BattleManager party membership
14. **H4** - Fix SaveManager.OnApplicationPause blocking pattern
15. **M6** - Add [RequireComponent] attributes where missing

---

---

## Master Branch Safety Assessment

This report is **documentation only** (no code changes). Safe to merge to master:
- No C# file modifications
- No compilation risk
- No runtime behavior changes
- No breaking changes

---

*Report generated by Claude Opus 4.6 - 9 parallel scanning agents (2 passes) + manual deep-dive of 15+ critical files*
*Total findings: 4 Critical, 9 High, 8 Medium, 6 Low, 7 Optimization Opportunities*
*Session: claude/bug-scan-optimization-LJVRn*
