# Architecture

**Analysis Date:** 2026-03-30

## Pattern Overview

**Overall:** Singleton Manager + Event Bus + ScriptableObject Data Architecture

**Key Characteristics:**
- Persistent singleton managers survive scene transitions via `DontDestroyOnLoad`
- Static `EventBus` provides decoupled game-wide communication (50+ events)
- JSON-backed data layer with `GameDatabase` dictionary lookups
- Scene-scoped singletons for battle and capture systems
- Pure static systems for game rules (no MonoBehaviour, no state)
- UI Toolkit (UXML/USS/C#) for all UI, animated with PrimeTween

## Layers

**Core Layer:**
- Purpose: Application lifecycle, singletons, event bus, constants, input
- Location: `Assets/Scripts/Core/`
- Contains: `GameManager`, `GameBootstrap`, `EventBus`, `SingletonMonoBehaviour<T>`, `GameDatabase`, `GameDataAssets`, `InputManager`, `Constants`, `ErrorLogger`
- Depends on: `VeilBreakers.Data` (enums, data types)
- Used by: Every other layer

**Data Layer:**
- Purpose: Game data definitions, enums, serialization structures
- Location: `Assets/Scripts/Data/`
- Contains: `HeroData`, `MonsterData`, `SkillData`, `ItemData`, `SaveData`, `Enums`, `StatusEffectData`, `HeroDisplayConfig`, `ShrineData`, `AbilityData`
- Depends on: Nothing (leaf layer)
- Used by: Core, Combat, Systems, Managers, UI

**Systems Layer:**
- Purpose: Pure game rule calculations with no mutable state
- Location: `Assets/Scripts/Systems/`
- Contains: `BrandSystem`, `SynergySystem`, `CorruptionSystem`, `PathSystem`, `VERASystem`, `StatusEffectInstance`
- Depends on: `VeilBreakers.Data`
- Used by: Combat, Managers, UI (for display helpers)

**Combat Layer:**
- Purpose: Real-time tactical combat execution
- Location: `Assets/Scripts/Combat/`
- Contains: `BattleManager`, `Combatant`, `DamageCalculator`
- Depends on: Core, Data, Systems
- Used by: UI.Combat, AI, Capture, Audio

**Managers Layer:**
- Purpose: Persistent services (save/load, settings, scenes, effects)
- Location: `Assets/Scripts/Managers/`
- Contains: `SaveManager`, `AutoSaveManager`, `SaveFileHandler`, `MigrationRunner`, `VBSceneManager`, `SettingsManager`, `StatusEffectManager`, `ShrineManager`
- Depends on: Core, Data, Systems
- Used by: UI, Combat (for StatusEffectManager)

**AI Layer:**
- Purpose: Autonomous combatant decision-making (Gambit system)
- Location: `Assets/Scripts/AI/`
- Contains: `GambitController`, `GambitEvaluator`, `GambitRule`, `GambitCondition`, `GambitAction`, `AIPersonality`
- Depends on: Combat, Core, Data
- Used by: Attached to enemy/ally Combatant GameObjects via `[RequireComponent(typeof(Combatant))]`

**Capture Layer:**
- Purpose: Monster capture mechanics (mark, bind, QTE, capture)
- Location: `Assets/Scripts/Capture/`
- Contains: `CaptureManager`, `CaptureFormulaCalculator`, `BindThresholdCalculator`, `QTEController`, `CaptureData`
- Depends on: Combat, Core, Data
- Used by: UI.Combat (CombatHUD triggers capture)

**Audio Layer:**
- Purpose: Sound effects, music, voice, battle audio integration
- Location: `Assets/Scripts/Audio/`
- Contains: `AudioManager`, `MusicManager`, `AudioConfig`, `AudioTriggers`, `AudioBattleIntegration`, `LowHealthAudio`, `VB_UISoundManager`, `VERAVoiceController`
- Depends on: Core (EventBus, SingletonMonoBehaviour)
- Used by: Listens to EventBus events

**UI Layer:**
- Purpose: All user interface screens and HUD elements
- Location: `Assets/Scripts/UI/`
- Contains: Sub-folders for CharacterSelect, Combat, Controls, Core, Effects, Menus
- Depends on: Core, Data, Combat, Managers, Systems
- Used by: Scene GameObjects (MonoBehaviour attached to UIDocument holders)

**Commands Layer:**
- Purpose: Quick command / radial menu system for combat
- Location: `Assets/Scripts/Commands/`
- Contains: `QuickCommand`, `QuickCommandManager`, `RadialMenuController`, `TimeSlowController`
- Depends on: Combat, Core
- Used by: UI.Combat

**Utils Layer:**
- Purpose: Generic utilities and extensions
- Location: `Assets/Scripts/Utils/`
- Contains: `Extensions`, `ObjectPool`
- Depends on: Nothing
- Used by: Any layer

**VFX Layer:**
- Purpose: Visual effects scripts (brand-specific particle systems)
- Location: `Assets/Scripts/VFX/`
- Contains: `VB_AoEVFX_ground_circle_RUIN`, `VB_HitVFX_SAVAGE`, `VB_StatusVFX_SURGE`
- Depends on: Core (brand data)
- Used by: Combat scene GameObjects

## Data Flow

**Bootstrap Flow (Application Start):**

1. `Bootstrap` scene loads with `GameBootstrap` GameObject
2. `GameBootstrap.OnSingletonAwake()` calls `Initialize()`
3. Phase 1: Creates `GameManager`, `GameDatabase`, `InputManager` singletons
4. Phase 2: Creates `SettingsManager`, `VBSceneManager`, `SaveManager`, `AutoSaveManager`
5. Phase 3: Creates `AudioManager`, `MusicManager`, `VERAVoiceController`, `LowHealthAudio`
6. Phase 4: Creates `StatusEffectManager`, `ShrineManager`, `FPSCounter`
7. `GameDatabase.OnSingletonAwake()` triggers async JSON loading (monsters, skills, heroes, items in parallel)
8. After splash delay, `VBSceneManager.LoadSceneWithFade("MainMenu")` transitions to main menu

**Scene Flow:**

```
Bootstrap -> MainMenu -> CharacterSelect -> Overworld / Battle / TestArena
                 ^                                        |
                 |________________________________________|
```

Scenes: `Bootstrap`, `MainMenu`, `CharacterSelect`, `Battle`, `Overworld`, `TestArena`
Scene names are constants in `VBSceneManager.Scenes` static class at `Assets/Scripts/Managers/VBSceneManager.cs`.

**New Game Flow:**

1. `MainMenuController` presents New Game / Continue / Settings / Exit
2. Player selects New Game -> `VBSceneManager.GoToCharacterSelect()`
3. `CharacterSelectManager` loads hero list from `GameDatabase.Heroes`
4. Player browses heroes (Vex, Seraphina, Orion, Nyx) via carousel
5. Player holds Embark button -> `HoldToEmbarkController` triggers
6. `SaveManager.CreateNewSaveAsync()` creates save in best available slot
7. `GameManager.StartNewGame(heroId, starterMonsterId)` initializes party
8. `EmbarkCinematicController` plays transition animation
9. `VBSceneManager` loads game scene (`Overworld`)

**Combat Flow:**

1. Scene loads `Battle` scene with `BattleManager` (scene-scoped singleton, `IsPersistent = false`)
2. `BattleManager.StartBattle(players, enemies, championPath)` initializes combatants
3. `CombatHUD.Initialize(player, allies, enemies)` sets up UI panels
4. Real-time Update loop: `BattleManager.Update()` ticks cooldowns, checks win/lose
5. Player input -> `CombatHUD` -> `BattleManager.ExecuteAbility(user, slot, target)`
6. `DamageCalculator.Calculate()` computes damage with brand/synergy/corruption modifiers
7. `Combatant.TakeDamage()` / `Combatant.Heal()` mutates HP, fires events
8. `EventBus` broadcasts damage/heal/death events to Audio, UI, VFX listeners
9. `BattleManager.CheckBattleEnd()` triggers victory/defeat when party wiped

**Damage Calculation Pipeline:**

```
BasePower * (ATK/DEF ratio, clamped 0.5-2.0)
  * attacker.DamageMultiplier (buffs/debuffs)
  * BrandSystem.GetEffectiveness(attacker.Brand, defender.Brand) [0.5x, 1.0x, 2.0x]
  * SynergySystem.GetDamageBonus(tier) [1.0, 1.05, 1.08]
  * (2.0 - defenderSynergyDefense) [defender synergy reduction]
  * (1.0 + CorruptionModifier) [-0.20 to +0.25]
  * Random.Range(0.9, 1.1) [variance]
  * CritMultiplier [1.0 or 1.5]
  = finalDamage (min 1)
```

Implementation: `Assets/Scripts/Combat/DamageCalculator.cs` (static class)

**Capture Flow:**

1. Player marks target via `CaptureManager.MarkTarget()` (multiple marks allowed)
2. Allies approach marked target; `BindThresholdCalculator` computes bind threshold from HP%, corruption%, rarity
3. When target HP drops below threshold, bind window opens
4. Ally executes bind -> `CaptureManager` applies `BOUND` status (can't act, can't die)
5. Capture phase: battle pauses, player selects capture item tier
6. `CaptureFormulaCalculator` computes base capture rate from HP%, corruption%, item tier
7. `QTEController` runs quick-time event for bonus
8. Success: monster added to party via `GameManager.AddToParty()`; Fail: monster goes berserk, battle resumes

**Save/Load Flow:**

1. `SaveManager.SaveAsync(slot)` acquires `SemaphoreSlim` mutex (5s timeout)
2. Updates playtime and timestamp on `SaveData`
3. `SaveFileHandler.RotateBackups(path)` creates .bak1/.bak2 backup chain
4. `SaveFileHandler.SerializeToBytes()` serializes with JSON + GZip + AES-CBC + HMAC-SHA256
5. `SaveFileHandler.WriteFileAtomicAsync()` writes to temp file, then atomic rename
6. Read-back verification confirms byte-level match
7. `EventBus.SaveCompleted(slot)` broadcasts success
8. Load reverses: read bytes -> HMAC verify -> AES decrypt -> GZip decompress -> JSON deserialize
9. `MigrationRunner.MigrateToLatest()` upgrades old save versions (current: v3)

**State Management:**

- `GameManager.CurrentState` enum: `MainMenu`, `Exploring`, `InBattle`, `InDialogue`, `InMenu`, `Paused`, `Loading`
- `GameManager.ChangeState()` handles transitions (e.g., pause sets `Time.timeScale = 0`)
- `BattleManager._state` enum: `INITIALIZING`, `PLAYER_TURN`, `ENEMY_TURN`, `ANIMATING`, `VICTORY`, `DEFEAT`, `ESCAPED`, `CAPTURE`
- Party data lives in `GameManager` at runtime: `CurrentHero` (ActiveHero) + `Party` (List<PartyMember>)
- Persistent data lives in `SaveData` serialized to disk via `SaveManager`

## Key Abstractions

**SingletonMonoBehaviour<T>:**
- Purpose: Generic base for persistent singletons with DontDestroyOnLoad
- Location: `Assets/Scripts/Core/SingletonMonoBehaviour.cs`
- Examples: `GameManager`, `GameDatabase`, `SaveManager`, `InputManager`, `AudioManager`, `VBSceneManager`, `StatusEffectManager`, `ShrineManager`
- Pattern: `SingletonResetHelper` handles domain reload cleanup for all closed generic types via `[RuntimeInitializeOnLoadMethod]`
- Override `IsPersistent => false` for scene-scoped singletons (e.g., `BattleManager`, `CaptureManager`)
- Access: `T.Instance` (returns null if quitting), `T.HasInstance` for null-safe checks

**EventBus:**
- Purpose: Static event hub for decoupled cross-system communication
- Location: `Assets/Scripts/Core/EventBus.cs`
- Pattern: Static `Action` delegates with static fire methods (e.g., `EventBus.BattleStarted()`)
- Categories: Game State (4), Battle (5), Combat Actions (4), Skill Types (4), Status Effects (6), Synergy (3), Monster (4), Hero (3), Inventory (4), UI (4), Audio (3), Save/Load (8), Shrine (3), Progression (4), Scene (8)
- Cleanup: `ClearAllListeners()` nulls all delegates; called on domain reload via `[RuntimeInitializeOnLoadMethod]`
- Subscribe in `OnEnable`/`Start`, unsubscribe in `OnDisable`/`OnDestroy` for scene-scoped objects

**GameDatabase:**
- Purpose: Central read-only data repository loaded from JSON
- Location: `Assets/Scripts/Core/GameDatabase.cs`
- Pattern: Async parallel load on singleton init; `Dictionary<string, T>` lookups
- Data sources: `Resources/Data/monsters.json`, `heroes.json`, `skills.json`, `items.json` via `GameDataAssets` ScriptableObject
- Access: `GameDatabase.Instance.GetMonster("id")`, `GetSkill()`, `GetHero()`, `GetItem()`
- Readiness: check `GameDatabase.Instance.IsReady` before querying; `InitializationTask` can be awaited

**Combatant:**
- Purpose: Unified combat participant (hero or monster) with stats, abilities, status effects
- Location: `Assets/Scripts/Combat/Combatant.cs`
- Pattern: MonoBehaviour component on battle scene GameObjects
- Events: `OnHpChanged`, `OnMpChanged`, `OnDamageReceived`, `OnHealed`, `OnDeath`, `OnRevive`
- Owns: `AbilityLoadout` (6 slots: BASIC_ATTACK, DEFEND, SKILL_1, SKILL_2, SKILL_3, ULTIMATE), local status effect list (fallback), casting state, defend/guard state

**Static Rule Systems (all in `Assets/Scripts/Systems/`):**
- `BrandSystem`: 10-brand effectiveness matrix (2x strong, 0.5x weak, 1x neutral), hybrid brand resolution via `GetCoreBrand()`, brand colors and display names
- `SynergySystem`: Party synergy tiers (FULL 3/3: +8%, PARTIAL 2/3: +5%, NEUTRAL: +0%, ANTI: +0% with 1.5x corruption) based on champion Path + party brands
- `CorruptionSystem`: 5-tier corruption state (ASCENDED 0-10%: +25%, Purified 11-25%: +10%, Unstable 26-50%: +0%, Corrupted 51-75%: -10%, Abyssal 76-100%: -20%)
- `PathSystem`: 4-path stat bonuses (IRONBOUND=Defense, FANGBORN=Attack, VOIDTOUCHED=Magic, UNCHAINED=Balanced) with 0.5% per path level scaling

**AbilityLoadout (6-slot system):**
- Slot 0: BASIC_ATTACK (no cooldown)
- Slot 1: DEFEND (no cooldown)
- Slot 2: SKILL_1 (4-6s cooldown)
- Slot 3: SKILL_2 (10-15s cooldown)
- Slot 4: SKILL_3 (18-25s cooldown)
- Slot 5: ULTIMATE (45-90s cooldown)

## Entry Points

**GameBootstrap:**
- Location: `Assets/Scripts/Core/GameBootstrap.cs`
- Triggers: Attached to GameObject in `Bootstrap` scene (first scene loaded)
- Responsibilities: Creates all persistent singletons in dependency order, runs health checks, loads first scene after splash delay

**MainMenuController:**
- Location: `Assets/Scripts/UI/Menus/MainMenuController.cs`
- Triggers: `MainMenu` scene loads, `UIDocument` provides UXML root
- Responsibilities: New Game / Continue / Settings / Exit navigation, title screen VFX, animated transitions

**CharacterSelectManager:**
- Location: `Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs`
- Triggers: `CharacterSelect` scene loads
- Responsibilities: Hero browsing, tab switching (Overview/Abilities/Lore), embark flow with timeout/error handling, save creation, theme transitions
- Delegates to sub-controllers: `HeroDataPanelController`, `HeroStatsPanelController`, `CarouselController`, `HoldToEmbarkController`, `EmbarkCinematicController`, `HeroThemeTransitioner`, `VeilTransitionController`

**BattleManager:**
- Location: `Assets/Scripts/Combat/BattleManager.cs`
- Triggers: `Battle` scene loads, external code calls `StartBattle()`
- Responsibilities: Combat lifecycle, ability execution, synergy tracking, party swaps, victory/defeat, event cleanup

**CombatHUD:**
- Location: `Assets/Scripts/UI/Combat/CombatHUD.cs`
- Triggers: `Battle` scene loads with HUD prefab
- Responsibilities: Player/enemy/ally panels, skill bar, target cycling, ally selection, capture banner, bidirectional sync with BattleManager

## Error Handling

**Strategy:** Defensive programming with Debug.Log/LogWarning/LogError + null guards

**Patterns:**
- Singleton access: always check `T.HasInstance` before `.Instance` to avoid null references
- `GameDatabase` query methods return null if `!IsReady`; callers must null-check
- `SaveManager` uses `try/catch` with typed exception filters (`IOException`, `InvalidDataException`)
- `SaveManager` uses `SemaphoreSlim` with 5-second timeout to prevent deadlocks
- `SaveData.ValidateAndRepair()` clamps all values to valid ranges and initializes null lists
- `EventBus` handlers use null-conditional invocation (`OnEvent?.Invoke()`)
- Conditional compilation: `#if UNITY_EDITOR || DEVELOPMENT_BUILD` for verbose combat logging
- `ErrorLogger` static class for consistent structured logging

## Cross-Cutting Concerns

**Logging:**
- `Debug.Log/LogWarning/LogError` with `[ClassName]` prefix convention
- `ErrorLogger` static helper at `Assets/Scripts/Core/ErrorLogger.cs`
- Verbose combat logs gated behind `UNITY_EDITOR || DEVELOPMENT_BUILD` defines

**Validation:**
- `SaveData.ValidateAndRepair()` for save file integrity (clamps, null-init, size limits)
- `DamageCalculator` clamps stat ratios to [0.5, 2.0] and enforces minimum 1 damage
- `CorruptionSystem` clamps corruption to [0, 100] via `Mathf.Clamp`
- Brand effectiveness resolves hybrid brands via `BrandSystem.GetCoreBrand()`

**Save Security:**
- AES-CBC encryption + HMAC-SHA256 verification on all save files
- Atomic file writes (write to temp, rename) prevent corruption on crash
- Backup rotation (.bak1, .bak2) for recovery from corrupted saves
- Orphaned temp file cleanup on initialization
- Save version migration via `MigrationRunner` (current version: 3)

**Performance Conventions:**
- No LINQ in hot paths (Update loops use `for` with index)
- Pre-allocated buffers: `Brand[] _brandBuffer` in BattleManager, `List<T>` temp buffers in StatusEffectManager
- `HashSet<Combatant>` for O(1) party membership lookups in BattleManager
- Cached `WaitForSeconds`/`WaitForSecondsRealtime` as `static readonly` fields
- `[ThreadStatic]` reusable buffers for pure systems (`PathSystem._bonusBuffer`)
- `Dictionary` caches for computed values (brand display names, brand colors)

---

*Architecture analysis: 2026-03-30*
