# VeilBreakers 3D - Full Codebase Deep Scan Report

**Date:** 2026-03-21
**Scanned by:** Claude Opus 4.6 (ultrathink) + 6 parallel bug-hunter agents
**Files scanned:** 128 C# files + 2 shaders + UXML/USS across 18 directories
**Previous CharSelect scan:** 28 findings (18 still unfixed)

## Summary

| Severity | CharSelect (unfixed) | Core/Data | Managers/Systems | AI/Combat/Commands | Audio/Capture | UI (non-CS) | Utils/Test/Editor | **Total** |
|----------|---------------------|-----------|------------------|-------------------|---------------|-------------|-------------------|-----------|
| CRITICAL | 2 | 2 | 2 | 2 | 2 | 1 | 0 | **11** |
| HIGH | 8 | 8 | 10 | 8 | 4 | 8 | 2 | **48** |
| MEDIUM | 10 | 10 | 9 | 12 | 5 | 8 | 4 | **58** |
| LOW | 5 | 6 | 8 | 8 | 3 | 5 | 2 | **37** |
| **Total** | **25** | **26** | **29** | **30** | **14** | **22** | **8** | **154** |

---

## REPO & GITHUB CLEANUP ISSUES

### REPO-001: 685MB of Binary Files Without Git LFS (CRITICAL)
Git LFS is NOT installed. 155 PNGs (585MB), 3 MP4s (23MB), 5 MP3s (2.8MB), 1 FBX (1.2MB) are tracked as raw git objects. This bloats clone time, wastes CI bandwidth, and makes the repo difficult to fork.

**Fix:** Install Git LFS, migrate binary files: `git lfs install && git lfs migrate import --include="*.png,*.mp4,*.mp3,*.fbx,*.jpg,*.wav" --everything`

### REPO-002: Duplicate Files Between Assets/ and Assets/Resources/ (HIGH)
Exact duplicates found:
- `Assets/Data/items.json` = `Assets/Resources/Data/items.json` (identical)
- `Assets/Data/skills.json` = `Assets/Resources/Data/skills.json` (identical)
- `Assets/UI/Templates/Dialogue.uxml` = `Assets/Resources/UI/Templates/Dialogue.uxml` (identical)
- `Assets/UI/Templates/MainMenu.uxml` = `Assets/Resources/UI/Templates/MainMenu.uxml` (identical)
- `Assets/Art/VFX/ParticleTextures/dust.png` = `Assets/Resources/VFX/ParticleTextures/dust.png` (identical)
- `Assets/Art/VFX/ParticleTextures/smoke.png` = `Assets/Resources/VFX/ParticleTextures/smoke.png` (identical)
- `CRITICAL_FIXES_NEEDED.md` = `Docs/CRITICAL_FIXES_NEEDED.md` (identical)

Diverged copies (DANGEROUS):
- `Assets/Data/heroes.json` != `Assets/Resources/Data/heroes.json` (different hashes!)
- `Assets/Data/monsters.json` != `Assets/Resources/Data/monsters.json` (different hashes!)
- `Assets/UI/Styles/VeilBreakers.uss` != `Assets/Resources/UI/Styles/VeilBreakers.uss` (different hashes!)

**Fix:** Pick ONE canonical location (Resources/ for runtime-loaded, Assets/ for editor-only). Delete the other. The diverged heroes.json and monsters.json need manual reconciliation — one is stale.

### REPO-003: 8 Redundant Scan/Audit Reports in Docs/ (MEDIUM)
- `Docs/BUG_AND_OPTIMIZATION_REPORT.md` (11KB)
- `Docs/BUG_SCAN_REPORT.md` (21KB)
- `Docs/CODEBASE_AUDIT_REPORT.md` (10KB)
- `Docs/CODE_AUDIT_REPORT.md` (8KB)
- `Docs/CRITICAL_FIXES_NEEDED.md` (2KB, duplicated at root)
- `Docs/FINAL_SCAN_REPORT.md` (9KB)
- `Docs/CHARSELECT_DEEP_SCAN.md` (13KB)
- `Docs/CHARSELECT_FIX_PROMPT.md` (12KB)

These overlap heavily and many are superseded. This report replaces ALL of them.

**Fix:** Archive old reports to `Docs/archive/` and keep only this one + the fix prompt.

### REPO-004: 7 Overlapping CharSelect Design Docs (LOW)
- `Docs/plans/2026-01-27-character-select-design.md`
- `Docs/plans/2026-02-05-character-select-redesign.md`
- `Docs/plans/2026-02-18-character-select-rebuild-design.md` (31KB!)
- `Docs/plans/2026-02-18-character-select-rebuild-implementation.md` (102KB!)
- `Docs/archive/AAA_CHARACTER_SELECT_DESIGN.md`
- `Docs/archive/AAA_CHARACTER_SELECT_SETUP.md`
- `Docs/archive/AAA_CHARACTER_SELECT_SUMMARY.md`

**Fix:** Keep only the latest rebuild implementation doc. Archive the rest.

### REPO-005: Archive Data Files Still in Resources/ (LOW)
- `Assets/Data/heroes_archived_v1.json`
- `Assets/Resources/Data/monsters_archive_v1.json` (48KB)
- `Assets/Resources/Data/skills_archive_v1.json` (141KB)

These ship with the build and waste 190KB+ in the player.

**Fix:** Move to `Docs/archive/` or `_Archive/data/`.

### REPO-006: CI Workflow References LFS But LFS Not Installed (HIGH)
`.github/workflows/unity-ci.yml` uses `lfs: true` in checkout steps, but Git LFS is not set up. Builds will checkout without binary files, causing missing texture/audio errors.

### REPO-007: No Branch Protection Enforcement (MEDIUM)
Only 2 branches exist (master + this scan branch). No `develop` branch despite CLAUDE.md mandating one. No tags for releases. The git workflow documented in CLAUDE.md (master/develop/feature) is not being followed.

---

## CRITICAL BUGS (11)

### CRIT-01: EventBus ClearAllListeners Misses 4 Events
**File:** `Assets/Scripts/Core/EventBus.cs:296-370`
**Impact:** Memory leak + MissingReferenceException crashes after scene transitions
`OnBuffApplied`, `OnDebuffApplied`, `OnUtilityUsed`, `OnUltimateUsed` never nullified in `ClearAllListeners()`.
**Fix:** Add 4 lines: `OnBuffApplied = null; OnDebuffApplied = null; OnUtilityUsed = null; OnUltimateUsed = null;`

### CRIT-02: Captured Monster TakeDamage Triggers Death Events + XP Rewards
**File:** `Assets/Scripts/Capture/CaptureManager.cs:727-729`
**Impact:** Players get XP for capturing (double reward), death audio plays for captured monsters
```csharp
monster.TakeDamage(monster.MaxHP + 1); // Fires OnCombatantDeath, grants XP, plays death audio
```
**Fix:** Add `RemoveFromBattle()` method on Combatant that marks dead without triggering death pipeline. Or add `skipEvents` parameter.

### CRIT-03: `_isEmbarking` Not Reset on Timeout (Embark Lockout)
**File:** `Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs:694-697`
**Impact:** Embark button permanently dead after save timeout
**Fix:** Add `finally { _isEmbarking = false; }` block.

### CRIT-04: Dual NavigationMoveEvent Handlers (Double Hero Skip)
**File:** `Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs:522,814` + `CharSelectFocusManager.cs:156,230`
**Impact:** D-pad Left/Right fires both handlers, skipping heroes
**Fix:** Remove `OnNavigationMove` from CharacterSelectManager entirely.

### CRIT-05: VolumeProfile ScriptableObject Leaked Every Scene Load
**File:** `Assets/Scripts/UI/CharacterSelect/VolumeProfileTransitioner.cs:45-69`
**Impact:** GPU memory leak accumulates on each scene reload
**Fix:** Track runtime profile, destroy in OnDestroy.

### CRIT-06: SaveManager Deadlock on Application Pause
**File:** `Assets/Scripts/Managers/SaveManager.cs:97`
**Impact:** `.GetAwaiter().GetResult()` blocks main thread up to 5s; ANR kill on mobile
```csharp
AutoSaveAsync("app_pause").GetAwaiter().GetResult(); // Blocks main thread
```
**Fix:** Use `_saveMutex.Wait(0)` (no-wait) or fire-and-forget with short timeout.

### CRIT-07: Save Encryption Key Lost on PlayerPrefs Clear
**File:** `Assets/Scripts/Managers/SaveFileHandler.cs:591-609`
**Impact:** All saves permanently undecryptable after app data clear/reinstall
**Fix:** Persist device key to a file alongside saves as fallback.

### CRIT-08: AIPersonality Static Cache Leaks ScriptableObjects
**File:** `Assets/Scripts/AI/AIPersonality.cs:292-293`
**Impact:** Runtime SOs never destroyed, leak across scene loads
**Fix:** Add static cleanup on scene unload or `[RuntimeInitializeOnLoadMethod]` reset.

### CRIT-09: Corruption Modifier Inverted for Defenders
**File:** `Assets/Scripts/Combat/DamageCalculator.cs:72-76`
**Impact:** ASCENDED defenders get 25% damage reduction (unintended per spec); Abyssal defenders take 20% MORE damage
**Fix:** Remove defender corruption line (corruption should only affect outgoing damage) or clarify design intent.

### CRIT-10: Berserk Monster Fires BattleStarted Event (Re-initializes All Systems)
**File:** `Assets/Scripts/Capture/CaptureManager.cs:672`
**Impact:** Audio re-subscribes, UI re-inits, music restarts mid-battle
```csharp
EventBus.BattleStarted(); // Should be EventBus.BattleResumed()
```
**Fix:** Create distinct `BattleResumed` event.

### CRIT-11: SoulSwarmVFX Double Mouse Callback Registration
**File:** `Assets/Scripts/UI/Core/SoulSwarmVFX.cs:82,100-103,159-160`
**Impact:** After disable/enable cycle, mouse callbacks fire twice causing doubled VFX
**Fix:** Unregister in OnDisable; move mouse registration exclusively to OnEnable.

---

## HIGH BUGS (48)

### Core/Data (8)
| ID | File | Issue |
|----|------|-------|
| HIGH-01 | `GameManager.cs:88` | Mutable `Party` list exposed publicly; bypasses AddToParty validation |
| HIGH-02 | `GameDatabase.cs:329-355` | Returns mutable data references; callers can corrupt template data |
| HIGH-03 | `GameManager.cs:363` | `AddCurrency` allows negative amounts, bypasses SpendCurrency checks |
| HIGH-04 | `GameManager.cs:107-132` | `ChangeState(Paused)` doesn't set `_stateBeforePause`; ResumeGame restores wrong state |
| HIGH-05 | `GameManager.cs:145-152` | `ResumeGame` bypasses `ChangeState`, skips hooks |
| HIGH-06 | `HeroData.cs:174-205` | `GetStatAtLevel` no upper bound clamp; integer overflow at high levels |
| HIGH-07 | `MonsterData.cs:108-116` | Unsafe enum cast from JSON int; no Enum.IsDefined validation |
| HIGH-08 | `SaveData.cs:153-190` | `ValidateAndRepair` skips monster.level, monsterId, HP, item quantity |

### AI/Combat/Commands (8)
| ID | File | Issue |
|----|------|-------|
| HIGH-09 | `GambitEvaluator.cs:197` | Rules evaluated with null target; enemy-targeted conditions silently skipped |
| HIGH-10 | `GambitEvaluator.cs:752-757` | Fallback action returns null target; NRE in ExecuteAction |
| HIGH-11 | `GambitController.cs:414` | `HasMomentum` null dereference on `_personality` |
| HIGH-12 | `Combatant.cs:371-374` | `ApplyDamageBuff` overwrites instead of stacking |
| HIGH-13 | `BattleManager.cs:260` | `GameDatabase.Instance` null access with no error path |
| HIGH-14 | `RadialMenuController.cs:97` | `Camera.main` in Start; null forever if camera spawns later |
| HIGH-15 | `QuickCommandManager.cs:55` | Static cache survives scene loads with stale data |
| HIGH-16 | `QuickCommandManager.cs:326` | Ground position validation rejects world origin (0,0,0) |

### Managers/Systems (12)
| ID | File | Issue |
|----|------|-------|
| HIGH-17 | `AutoSaveManager.cs:172` | Fire-and-forget async; unobserved exceptions crash Mono runtime |
| HIGH-18 | `SaveFileHandler.cs:296` | `ReadAsync` may not read all bytes (partial read) |
| HIGH-19 | `BrandSystem.cs:32-45` | **Brand matrix has 4+ asymmetry violations** (SAVAGE/GRACE mutual weakness, MEND/LEECH not reciprocal, LEECH/VENOM not reciprocal, DREAD/GRACE not reciprocal) |
| HIGH-20 | `SynergySystem.cs:61-69` | **Anti-synergy short-circuits**: one weak brand in party negates all strong matches, returns ANTI immediately |
| HIGH-21 | `PathSystem.cs:26-36` | Shared static buffer mutated across calls; callers who cache reference get corrupted data |
| HIGH-22 | `StatusEffectManager.cs:296-337` | **Cleanse sort direction backwards**: ascending when comment says descending; lowest priority cleansed first |
| HIGH-23 | `StatusEffectManager.cs:572-623` | Shared `_tempEffectList` used for shields AND break-on-damage in same call; reentrant corruption risk |
| HIGH-24 | `VERASystem.cs:586-592` | `LoadSaveData` sets veil integrity and personality without validation or consistency check |
| HIGH-25 | `VERASystem.cs:95-104` | Manual singleton pattern; OnDestroy doesn't unsubscribe events (only OnDisable does) |
| HIGH-26 | `AutoSaveManager.cs:68-82` | Double unsubscribe in OnDisable + OnDestroy; destroyed instance handlers stay on EventBus |
| HIGH-27 | `VBSceneManager.cs:180-203` | Synchronous LoadScene mid-coroutine; no re-entry protection during fade-in |
| HIGH-28 | `SaveManager.cs:515-518` | `OnApplicationQuit` updates playtime but never saves to disk |

### Audio/Capture (4)
| ID | File | Issue |
|----|------|-------|
| HIGH-27 | `AudioManager.cs` | AudioSource pool never shrinks; leaked sources after heavy combat |
| HIGH-28 | `MusicManager.cs` | Crossfade creates new AudioSource per transition; never cleaned |
| HIGH-29 | `VERAVoiceController.cs` | Coroutine not stopped on disable; plays audio after scene exit |
| HIGH-30 | `CaptureManager.cs:365` | Bind duration uses `Time.time` (exploitable via timeScale) |

### UI Non-CharSelect (8)
| ID | File | Issue |
|----|------|-------|
| HIGH-31 | `MoltenVeinVFX.cs:75` | GeometryChangedEvent never unregistered |
| HIGH-32 | `MenuVFXController.cs:57` | GeometryChangedEvent never unregistered |
| HIGH-33 | `ParallaxBackground.cs:63` | GeometryChangedEvent never unregistered |
| HIGH-34 | `MoltenButtonVFX.cs:113` | GeometryChangedEvent never unregistered |
| HIGH-35 | `TitleScreenVFX.cs:322` | GeometryChangedEvent never unregistered |
| HIGH-36 | `MainMenuController.cs:161` | `InputManager.Instance` without `HasInstance` guard |
| HIGH-37 | `CombatHUD.cs` | Combatant reference not nulled on death; stale UI updates |
| HIGH-38 | `HealthBarController.cs` | Division by zero if maxHP = 0 |

### CharSelect (still unfixed) (8)
| ID | File | Issue |
|----|------|-------|
| HIGH-39 | `HoldToEmbarkController.cs:154` | Duplicate callback registration on repeated OnScreenReady |
| HIGH-40 | `HoldToEmbarkController.cs:197` | Gamepad hold bypasses embark focus check |
| HIGH-41 | `EmbarkCinematicController.cs:50` | OnCinematicComplete event never cleared on disable |
| HIGH-42 | `HeroStageController.cs:448` | Rim flicker tweens continue after CleanupStage |
| HIGH-43 | `CarouselController.cs:190` | Breathing animation stacking on card selection |
| HIGH-44 | `HeroThemeTransitioner.cs:153` | Entry sequence not tracked + heroStage passed as leftPanel |
| HIGH-45 | `VeilTransitionController.cs:203` | OnTransitionComplete fires every frame below threshold |
| HIGH-46 | `HeroStatsPanelController.cs` | Stat cascade sequence not stopped on disable |

### UI Combat (1 additional)
| ID | File | Issue |
|----|------|-------|
| HIGH-47 | `SkillSlotController.cs:124` | Ultimate keybind label shows "7" instead of "R" |

### Utils/Test/Editor (2)
| ID | File | Issue |
|----|------|-------|
| HIGH-48 | `ObjectPool.cs:150` | Pool growth unbounded; no max size limit |
| HIGH-49 | `CombatTestSetup.cs:242` | Test arena creates combatants with null MonsterData references |

---

## MEDIUM BUGS (58) — Abbreviated

Key patterns found across the codebase:

1. **6x GeometryChangedEvent leaks** — All VFX controllers register in Start(), never unregister
2. **8x Debug.Log in production** — Not wrapped in `#if UNITY_EDITOR`
3. **5x Unsafe enum casts** — `(Brand)intValue` without Enum.IsDefined across Data/ classes
4. **4x Static caches never cleared** — QuickCommandManager, AIPersonality, GameDataAssets, EventBus
5. **3x Audio clip regeneration per OnEnable** — HoldToEmbarkController, CharSelectFocusManager
6. **3x Mutable collections returned** — GameDatabase heroes/monsters/skills lists
7. **2x Duplicate type definitions** — GameDataTypes shadows SkillData/ItemData
8. **2x Hardcoded cooldowns** — AbilityData ignores SkillData values
9. **1x RenderSettings.ambientLight not restored** — HeroStageController pollutes next scene
10. **1x Screen center not recomputed on resize** — OverlayController parallax drift

---

## LOW BUGS (37) — Abbreviated

Key patterns:
1. Dead code (empty handlers, unused methods, unreachable paths)
2. Minor enum/constant inconsistencies
3. Test assertions missing or incomplete
4. Editor scripts with hardcoded paths
5. Cosmetic: sort modifies shared list, glitch text reads at build time

---

## PRIORITY FIX ORDER

### Tier 0: Data Loss / Security (Fix Immediately)
1. **CRIT-07** — Save key lost on PlayerPrefs clear (data loss)
2. **CRIT-06** — SaveManager deadlock on pause (ANR kill)
3. **CRIT-02** — Captured monster triggers death events + double XP

### Tier 1: Gameplay-Breaking (Fix Today)
4. **CRIT-09** — Corruption modifier inverted for defenders
5. **CRIT-03** — Embark lockout after timeout
6. **CRIT-04** — Double hero navigation
7. **CRIT-10** — Berserk BattleStarted re-init
8. **HIGH-12** — Damage buff overwrites instead of stacking

### Tier 2: Memory Leaks (Fix This Sprint)
9. **CRIT-01** — EventBus 4 missing clears
10. **CRIT-05** — VolumeProfile GPU leak
11. **CRIT-08** — AIPersonality static SO cache leak
12. **CRIT-11** — SoulSwarmVFX double registration
13. **HIGH-31 thru 35** — 5x GeometryChangedEvent leaks (batch fix)

### Tier 3: Robustness (Fix This Sprint)
14. **HIGH-08** — SaveData validation gaps
15. **HIGH-06** — HeroData stat overflow
16. **HIGH-07** — Unsafe enum casts
17. **HIGH-01** — Mutable Party list
18. **HIGH-02** — Mutable GameDatabase references
19. **HIGH-09/10** — AI null target issues

### Tier 4: Polish & Cleanup
20. All MEDIUM items (batch by pattern)
21. All LOW items
22. Repo cleanup (LFS, duplicates, docs)
