# FULL CODEBASE FIX PROMPT

Copy everything below this line and paste into a new Claude Code terminal session.

---

You are fixing bugs across the entire VeilBreakers3D codebase. 154 bugs were found by a deep scan (11 CRITICAL, 48 HIGH, 58 MEDIUM, 37 LOW). Fix in tier order. Commit after each tier.

**Project:** Unity 3D (C#, UI Toolkit). Code style: `_privateField`, `kConstant`, `PascalProperty`, `OnEvent`.
**Full report:** Read `Docs/FULL_CODEBASE_DEEP_SCAN.md` for context. Read each file before editing.

---

## TIER 0 — DATA LOSS / SECURITY (3 fixes)

### FIX 1: Save encryption key lost on PlayerPrefs clear
**File:** `Assets/Scripts/Managers/SaveFileHandler.cs`
**Lines:** ~591-609 (`GetOrCreateDeviceKey`)
**Fix:** After generating or retrieving the key from PlayerPrefs, also persist it to a file. On key lookup, check the file as fallback:
```csharp
private static readonly string kKeyFilePath = Path.Combine(Application.persistentDataPath, ".vb_device_key");

private static byte[] GetOrCreateDeviceKey()
{
    const string prefsKey = "vb_device_key";

    // Try PlayerPrefs first
    if (PlayerPrefs.HasKey(prefsKey))
    {
        var key = Convert.FromBase64String(PlayerPrefs.GetString(prefsKey));
        PersistKeyToFile(key); // Ensure file backup exists
        return key;
    }

    // Try file fallback
    if (File.Exists(kKeyFilePath))
    {
        try
        {
            var key = Convert.FromBase64String(File.ReadAllText(kKeyFilePath));
            PlayerPrefs.SetString(prefsKey, Convert.ToBase64String(key));
            PlayerPrefs.Save();
            return key;
        }
        catch { /* Fall through to generate new */ }
    }

    // Generate new key (existing logic)
    string id = SystemInfo.deviceUniqueIdentifier;
    // ... rest of existing generation code ...
    PersistKeyToFile(raw);
    return raw;
}

private static void PersistKeyToFile(byte[] key)
{
    try { File.WriteAllText(kKeyFilePath, Convert.ToBase64String(key)); }
    catch (Exception ex) { Debug.LogWarning($"[SaveFileHandler] Could not persist key file: {ex.Message}"); }
}
```

### FIX 2: SaveManager deadlock on Application Pause
**File:** `Assets/Scripts/Managers/SaveManager.cs`
**Lines:** ~93-101 (`OnApplicationPause`)
**Fix:** Replace blocking `.GetAwaiter().GetResult()` with non-blocking attempt:
```csharp
private void OnApplicationPause(bool pauseStatus)
{
    if (pauseStatus && HasActiveSave)
    {
        if (_isSaving || _isLoading) return;
        // Non-blocking: try acquire mutex with zero timeout
        if (_saveMutex.CurrentCount > 0)
        {
            _ = AutoSaveAsync("app_pause"); // Fire and forget; method has internal try/catch
        }
        else
        {
            Debug.LogWarning("[SaveManager] Save mutex contended during pause, skipping auto-save.");
        }
    }
}
```

### FIX 3: Captured monster triggers death events + XP rewards
**File:** `Assets/Scripts/Capture/CaptureManager.cs`
**Lines:** ~726-729 (`RemoveMonsterFromBattle`)
**File also:** `Assets/Scripts/Combat/Combatant.cs`
**Fix:** Add a method to Combatant:
```csharp
public void RemoveFromBattle()
{
    _currentHp = 0;
    _isAlive = false;
    // Do NOT fire OnDeath or damage events
}
```
Then in CaptureManager.RemoveMonsterFromBattle, replace:
```csharp
// OLD: monster.TakeDamage(monster.MaxHP + 1);
// NEW:
monster.RemoveFromBattle();
```

---

## TIER 1 — GAMEPLAY-BREAKING (5 fixes)

### FIX 4: Corruption modifier inverted for defenders
**File:** `Assets/Scripts/Combat/DamageCalculator.cs`
**Lines:** ~72-76
**Fix:** Remove the defender corruption line. Corruption should only affect outgoing damage (attacker), not incoming damage (defender):
```csharp
float attackerCorruptionMod = GetCorruptionModifier(attacker.Corruption);
damage *= (1f + attackerCorruptionMod);
// REMOVED: float defenderCorruptionMod = GetCorruptionModifier(defender.Corruption);
// REMOVED: damage *= (1f - defenderCorruptionMod);
```

### FIX 5: `_isEmbarking` not reset on timeout
**File:** `Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs`
**Lines:** ~690-705 (TriggerEmbark try/catch)
**Fix:** Add `finally { _isEmbarking = false; }` wrapping the try/catch blocks. Remove the `_isEmbarking = false;` from inside the general catch since finally covers it.

### FIX 6: Dual NavigationMoveEvent handlers
**File:** `Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs`
**Lines:** ~522 (RegisterCallback in BindUI) and ~814-828 (OnNavigationMove method)
**Fix:** Delete the `OnNavigationMove` method. Remove `_root.RegisterCallback<NavigationMoveEvent>(OnNavigationMove)` from `BindUI()` and the matching `UnregisterCallback` from `UnbindUI()`.

### FIX 7: Berserk fires BattleStarted event
**File:** `Assets/Scripts/Capture/CaptureManager.cs`
**Lines:** ~672
**File also:** `Assets/Scripts/Core/EventBus.cs`
**Fix:** Add a new event to EventBus:
```csharp
public static event Action OnBattleResumed;
public static void BattleResumed() => OnBattleResumed?.Invoke();
// Add to ClearAllListeners: OnBattleResumed = null;
```
Then in CaptureManager, replace `EventBus.BattleStarted()` with `EventBus.BattleResumed()`.
Update any listeners that need to respond to battle resume (AudioBattleIntegration, BattleManager) to subscribe to `OnBattleResumed` separately from `OnBattleStarted`.

### FIX 8: Damage buff overwrites instead of stacking
**File:** `Assets/Scripts/Combat/Combatant.cs`
**Lines:** ~371-374 (`ApplyDamageBuff`)
**Fix:** Make it multiplicative:
```csharp
public void ApplyDamageBuff(float multiplier)
{
    _damageBuffMultiplier *= (1f + multiplier); // Stack multiplicatively
}

public void RemoveDamageBuff(float multiplier)
{
    float factor = 1f + multiplier;
    if (factor > 0.001f)
        _damageBuffMultiplier /= factor;
}
```

---

## TIER 2 — MEMORY LEAKS (9 fixes)

### FIX 9: EventBus 4 missing event clears
**File:** `Assets/Scripts/Core/EventBus.cs`
**Lines:** Find `ClearAllListeners()`, add after the last existing null assignment:
```csharp
OnBuffApplied = null;
OnDebuffApplied = null;
OnUtilityUsed = null;
OnUltimateUsed = null;
```

### FIX 10: VolumeProfile ScriptableObject leak
**File:** `Assets/Scripts/UI/CharacterSelect/VolumeProfileTransitioner.cs`
**Fix:** Add field `private VolumeProfile _runtimeSharedProfile;`. In `AutoWireVolume`, after creating the SO, assign `_runtimeSharedProfile = profile;`. In `OnDestroy`, add `if (_runtimeSharedProfile != null) Destroy(_runtimeSharedProfile);`.

### FIX 11: AIPersonality static SO cache leak
**File:** `Assets/Scripts/AI/AIPersonality.cs`
**Lines:** ~292-293
**Fix:** Add a static cleanup method and call it on scene unload:
```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
private static void ClearCache()
{
    foreach (var kvp in _cachedDefaults)
    {
        if (kvp.Value != null) DestroyImmediate(kvp.Value);
    }
    _cachedDefaults.Clear();
}
```

### FIX 12: SoulSwarmVFX double mouse callback registration
**File:** `Assets/Scripts/UI/Core/SoulSwarmVFX.cs`
**Fix:** In `Initialize()` (~line 159-160), REMOVE the mouse callback registrations. Keep them ONLY in `OnEnable()`. In `OnDisable()`, unregister both mouse callbacks AND the GeometryChangedEvent:
```csharp
private void OnDisable()
{
    if (_uiDocument?.rootVisualElement != null)
    {
        _uiDocument.rootVisualElement.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
        _uiDocument.rootVisualElement.UnregisterCallback<MouseLeaveEvent>(OnMouseLeave);
        _uiDocument.rootVisualElement.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }
}
```

### FIX 13-17: 5x GeometryChangedEvent leaks (BATCH FIX)
**Files:**
- `Assets/Scripts/UI/Core/MoltenVeinVFX.cs`
- `Assets/Scripts/UI/Core/MenuVFXController.cs`
- `Assets/Scripts/UI/Core/ParallaxBackground.cs`
- `Assets/Scripts/UI/Core/MoltenButtonVFX.cs`
- `Assets/Scripts/UI/Core/TitleScreenVFX.cs`

**Fix for ALL:** Add to each file's `OnDisable()` or `OnDestroy()`:
```csharp
if (_uiDocument?.rootVisualElement != null)
    _uiDocument.rootVisualElement.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
```

---

## TIER 3 — ROBUSTNESS (12 fixes)

### FIX 18: SaveData validation gaps
**File:** `Assets/Scripts/Data/SaveData.cs`
**Lines:** ~153-190 (`ValidateAndRepair`)
**Fix:** After the corruption clamp, add:
```csharp
if (monster.level < 1) monster.level = 1;
if (monster.level > 100) monster.level = 100;
if (monster.currentHp < 0) monster.currentHp = 0;
if (string.IsNullOrEmpty(monster.monsterId)) { Debug.LogError("[SaveData] Monster with null ID found"); continue; }
monster.learnedSkills ??= new List<string>();
```

### FIX 19: HeroData stat overflow
**File:** `Assets/Scripts/Data/HeroData.cs`
**Lines:** ~174-205 (`GetStatAtLevel`)
**Fix:** Add `level = Mathf.Clamp(level, 1, 100);` at the start of the method.

### FIX 20: HeroData.Validate clamp all 6 stats
**File:** `Assets/Scripts/Data/HeroData.cs`
**Lines:** ~131-136 (`Validate`)
**Fix:** Clamp all 6:
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

### FIX 21: Unsafe enum casts in MonsterData
**File:** `Assets/Scripts/Data/MonsterData.cs`
**Lines:** ~108-116
**Fix:** Validate all enum casts:
```csharp
public Brand GetPrimaryBrand() => Enum.IsDefined(typeof(Brand), brand) ? (Brand)brand : Brand.NONE;
public Brand GetSecondaryBrand() => Enum.IsDefined(typeof(Brand), secondary_brand) ? (Brand)secondary_brand : Brand.NONE;
```
Apply same pattern to `GetBrandTier()`, `GetRarity()`, and similar casts in `ItemData.GetEquipmentSlot()` and `SkillData`.

### FIX 22: Mutable Party list
**File:** `Assets/Scripts/Core/GameManager.cs`
**Lines:** ~88
**Fix:**
```csharp
private readonly List<PartyMember> _party = new List<PartyMember>();
public IReadOnlyList<PartyMember> Party => _party;
```
Update all internal references from `Party` to `_party`. Fix any external code that calls `Party.Add()` etc. to use `AddToParty()`.

### FIX 23: AddCurrency negative amount bypass
**File:** `Assets/Scripts/Core/GameManager.cs`
**Lines:** ~363-367
**Fix:**
```csharp
public void AddCurrency(int amount)
{
    if (amount <= 0) return;
    Currency = (int)Math.Min((long)Currency + amount, int.MaxValue);
    EventBus.CurrencyChanged(Currency);
}
```

### FIX 24: PauseGame state machine inconsistency
**File:** `Assets/Scripts/Core/GameManager.cs`
**Lines:** ~107-152
**Fix:** In `ChangeState`, under `case GameState.Paused:`, add `_stateBeforePause = oldState;`. In `ResumeGame`, call `ChangeState(_stateBeforePause)` instead of directly setting `CurrentState`.

### FIX 25: AI null target issues
**File:** `Assets/Scripts/AI/GambitEvaluator.cs`
**Lines:** ~197 and ~752-757
**Fix 25a:** In `CreateFallbackAction`, add null check:
```csharp
var target = context.GetLowestHpEnemy();
if (target == null) return null;
```
**Fix 25b:** In `EvaluateBucket`, the null-target pre-filter is by design (conditions check `target != null`). Document this behavior with a comment.

### FIX 26: GambitController null personality check
**File:** `Assets/Scripts/AI/GambitController.cs`
**Lines:** ~414-417
**Fix:** Add `if (_personality == null) return false;` at the start of `HasMomentum()` and `return 0f;` at start of `GetMomentumBonus()`.

### FIX 27: Gamepad hold bypasses embark focus check
**File:** `Assets/Scripts/UI/CharacterSelect/HoldToEmbarkController.cs`
**Lines:** ~197
**Fix:** Find the CharSelectFocusManager reference and gate gamepad hold:
```csharp
bool gamepadHold = InputManager.HasInstance
    && InputManager.Instance.GetAction(InputManager.GameAction.Confirm)
    && _focusManager != null && _focusManager.CurrentZoneIndex == 2;
```
Add `public int CurrentZoneIndex => (int)_currentZone;` to `CharSelectFocusManager` if it doesn't exist.

### FIX 28: HoldToEmbark duplicate callback registration
**File:** `Assets/Scripts/UI/CharacterSelect/HoldToEmbarkController.cs`
**Lines:** ~154-156
**Fix:** Unregister before registering in `HandleScreenReady()`:
```csharp
_btnEmbark.UnregisterCallback<PointerDownEvent>(OnPointerDown);
_btnEmbark.UnregisterCallback<PointerUpEvent>(OnPointerUp);
_btnEmbark.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
_btnEmbark.RegisterCallback<PointerDownEvent>(OnPointerDown);
_btnEmbark.RegisterCallback<PointerUpEvent>(OnPointerUp);
_btnEmbark.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
```

### FIX 29: Combatant events not cleared
**File:** `Assets/Scripts/Combat/Combatant.cs`
**Lines:** ~98-103 (event declarations)
**Fix:** Add to `OnDestroy`:
```csharp
OnHpChanged = null; OnMpChanged = null; OnDamageReceived = null;
OnHealed = null; OnRevive = null; OnDeath = null;
```

---

## TIER 4 — CHARSELECT REMAINING (7 fixes)

### FIX 30: OnCinematicComplete not cleared
**File:** `Assets/Scripts/UI/CharacterSelect/EmbarkCinematicController.cs`
**Fix:** Add `OnCinematicComplete = null;` in `OnDisable()`.

### FIX 31: Rim flicker tweens in CleanupStage
**File:** `Assets/Scripts/UI/CharacterSelect/HeroStageController.cs`
**Fix:** At top of `CleanupStage()`: `StopRimFlicker(); if (_lightLerpTween.isAlive) _lightLerpTween.Stop();`

### FIX 32: Breathing animation stacking
**File:** `Assets/Scripts/UI/CharacterSelect/CarouselController.cs`
**Fix:** Track `IVisualElementScheduledItem _activeBreathingItem;` Cancel before each new `AddBreathing()`.

### FIX 33: Entry sequence not tracked + wrong leftPanel
**File:** `Assets/Scripts/UI/CharacterSelect/HeroThemeTransitioner.cs`
**Fix:** Assign `_activeSequence = entrySequence;`. Fix duplicate heroStage parameter — find correct leftPanel element.

### FIX 34: OnTransitionComplete multi-fire
**File:** `Assets/Scripts/UI/CharacterSelect/VeilTransitionController.cs`
**Fix:** Add `bool _hasInvokedComplete;` guard. Reset at start of each transition.

### FIX 35: Stat cascade not stopped on disable
**File:** `Assets/Scripts/UI/CharacterSelect/HeroStatsPanelController.cs`
**Fix:** Add `if (_statCascadeSequence.isAlive) _statCascadeSequence.Stop();` in `OnDisable()`.

### FIX 36: RenderSettings.ambientLight not restored
**File:** `Assets/Scripts/UI/CharacterSelect/HeroStageController.cs`
**Fix:** Cache in OnEnable: `_originalAmbientLight = RenderSettings.ambientLight;`. Restore in CleanupStage/OnDisable.

---

## TIER 5 — REPO CLEANUP

### FIX 37: Consolidate duplicate files
Delete these duplicates (keep the Resources/ versions since they're runtime-loaded):
```bash
rm Assets/Data/items.json Assets/Data/skills.json
rm Assets/UI/Templates/Dialogue.uxml Assets/UI/Templates/MainMenu.uxml
rm Assets/Art/VFX/ParticleTextures/dust.png Assets/Art/VFX/ParticleTextures/smoke.png
rm CRITICAL_FIXES_NEEDED.md
```
For the DIVERGED files (`heroes.json`, `monsters.json`, `VeilBreakers.uss`), compare and keep the newer/correct version.

### FIX 38: Move archive data out of Resources/
```bash
mkdir -p Docs/archive/data
mv Assets/Data/heroes_archived_v1.json Docs/archive/data/
mv Assets/Resources/Data/monsters_archive_v1.json Docs/archive/data/
mv Assets/Resources/Data/skills_archive_v1.json Docs/archive/data/
```

### FIX 39: Archive old scan reports
```bash
mkdir -p Docs/archive/scans
mv Docs/BUG_AND_OPTIMIZATION_REPORT.md Docs/archive/scans/
mv Docs/BUG_SCAN_REPORT.md Docs/archive/scans/
mv Docs/CODEBASE_AUDIT_REPORT.md Docs/archive/scans/
mv Docs/CODE_AUDIT_REPORT.md Docs/archive/scans/
mv Docs/CRITICAL_FIXES_NEEDED.md Docs/archive/scans/
mv Docs/FINAL_SCAN_REPORT.md Docs/archive/scans/
```

---

## COMMIT MESSAGES

```
Tier 0: "fix(critical): save key persistence, pause deadlock, capture death events"
Tier 1: "fix(gameplay): corruption calc, embark lockout, dual nav, berserk event, buff stacking"
Tier 2: "fix(memory): EventBus clears, VolumeProfile leak, AIPersonality cache, 6x GeometryChanged leaks"
Tier 3: "fix(robustness): save validation, enum safety, mutable collections, state machine, AI null guards"
Tier 4: "fix(charselect): remaining 7 unfixed bugs from deep scan"
Tier 5: "chore(repo): consolidate duplicates, archive old reports, move archive data"
```

## VERIFICATION
After all fixes:
1. No compiler errors
2. CharSelect loads and hero switching works
3. Embark button works and recovers from timeout
4. Combat damage feels correct (no defender corruption reduction)
5. Save/load works after clearing PlayerPrefs
6. No console errors on scene transitions
