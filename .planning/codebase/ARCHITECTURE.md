# Architecture

**Analysis Date:** 2026-02-21

## Pattern Overview

**Overall:** Singleton-based Service Locator with Event-Driven Communication

**Key Characteristics:**
- Persistent singleton managers initialized via a Bootstrap scene in phased order
- Static `EventBus` class for decoupled game-wide communication (~50+ events)
- Instance-level C# events on components for local communication (e.g., `Combatant.OnDeath`)
- JSON data loaded asynchronously into in-memory dictionaries via `GameDatabase`
- UI Toolkit (UXML/USS) for all menu screens; programmatic C# for animations
- Real-time tactical combat (cooldown-based, not turn-based)
- Scene-based game flow with fade transitions managed by `VBSceneManager`

## Layers

**Core Layer:**
- Purpose: Application lifecycle, singleton infrastructure, input, event bus, constants, data asset references
- Location: `Assets/Scripts/Core/`
- Contains: `GameBootstrap`, `GameManager`, `GameDatabase`, `SingletonMonoBehaviour<T>`, `EventBus`, `InputManager`, `Constants`, `GameDataAssets`, `GameDataTypes`
- Depends on: `VeilBreakers.Data` (enums, data classes)
- Used by: Every other layer

**Data Layer:**
- Purpose: Data structures, enumerations, JSON-serializable models
- Location: `Assets/Scripts/Data/`
- Contains: `Enums.cs` (Brand, Path, CorruptionState, SkillType, StatusEffectType, etc.), `MonsterData.cs`, `HeroData.cs`, `SkillData.cs`, `ItemData.cs`, `SaveData.cs`
- Depends on: UnityEngine only
- Used by: Core, Systems, Combat, UI, Managers

**Systems Layer:**
- Purpose: Pure game logic systems (stateless static classes for combat math)
- Location: `Assets/Scripts/Systems/`
- Contains: `BrandSystem`, `PathSystem`, `CorruptionSystem`, `SynergySystem`, `VERASystem`
- Depends on: `VeilBreakers.Data`
- Used by: Combat, UI, Managers

**Combat Layer:**
- Purpose: Battle orchestration, combatant state, damage calculation, AI
- Location: `Assets/Scripts/Combat/`
- Contains: `BattleManager`, `Combatant`, `DamageCalculator`, `CombatAI`, skill executors, guard system
- Depends on: Core, Data, Systems
- Used by: UI (combat HUD), Managers (scene transitions)

**Managers Layer:**
- Purpose: Persistent services (save/load, scene management, audio, settings, status effects, shrines)
- Location: `Assets/Scripts/Managers/`
- Contains: `SaveManager`, `VBSceneManager`, `AudioManager`, `MusicManager`, `SettingsManager`, `AutoSaveManager`, `StatusEffectManager`, `ShrineManager`
- Depends on: Core, Data
- Used by: Combat, UI

**UI Layer:**
- Purpose: All user interface screens and components using UI Toolkit
- Location: `Assets/Scripts/UI/`
- Contains: Screen controllers, sub-controllers, effects, shared controls
- Depends on: Core, Data, Systems (for display values)
- Used by: Nothing (leaf layer)

**AI Layer:**
- Purpose: Monster combat AI behavior
- Location: `Assets/Scripts/AI/`
- Contains: AI decision-making for enemy combatants
- Depends on: Combat, Data

**Capture Layer:**
- Purpose: Monster capture mechanics
- Location: `Assets/Scripts/Capture/`
- Contains: Capture logic and calculations
- Depends on: Combat, Data, Systems

## Data Flow

**Game Initialization (Bootstrap):**

1. `GameBootstrap.OnEnable()` runs in the Bootstrap scene
2. Phase 1: Creates `GameManager`, `GameDatabase`, `InputManager` singletons
3. `GameDatabase.InitializeAsync()` loads all JSON files (`monsters.json`, `heroes.json`, `skills.json`, `items.json`) in parallel via `Task.WhenAll`
4. JSON parsed via `JsonUtility` with `WrapJsonArray()` helper (Unity can't parse bare arrays)
5. Data stored in `Dictionary<string, T>` lookups (e.g., `_monsters`, `_skills`)
6. Phase 2-4: Creates remaining managers (Save, Audio, Scene, etc.)
7. After splash delay, `VBSceneManager.LoadScene("MainMenu")` transitions to the main menu

**Scene Flow:**
```
Bootstrap -> MainMenu -> CharacterSelect -> Overworld <-> Battle
                ^                                          |
                |__________________________________________|
```

**Character Select -> Gameplay:**

1. `CharacterSelectManager.OnEnable()` waits for `GameDatabase.IsReady` via coroutine
2. Loads hero data via `GameDatabase.Instance.GetAllHeroes()`
3. User navigates heroes and clicks Embark
4. `SaveManager.CreateNewSaveAsync()` creates a new save file (async with atomic writes)
5. `VBSceneManager` or `ScreenTransition` fades to Overworld scene

**Combat Flow:**

1. `BattleManager.StartBattle()` initializes combatants and synergy calculations
2. Each frame: `BattleManager.Update()` ticks cooldowns, checks victory/defeat
3. Player selects ability -> `BattleManager.ExecuteAbility()` dispatches by `SkillType`
4. `DamageCalculator.Calculate()` computes: `BasePower * (ATK/DEF) * BrandMult * SynergyMult * Variance * CritMult`
5. `Combatant.TakeDamage()` applies result, fires `OnDamageReceived` and `OnHpChanged` events
6. `EventBus.DamageDealt()` notifies UI and other listeners
7. On death: `Combatant.OnDeath` event fires, `BattleManager` checks win/lose conditions

**State Management:**
- `GameManager.GameState` enum: MainMenu, Exploring, InBattle, InDialogue, InMenu, Paused, Loading
- `GameManager` holds runtime party state: 1 `ActiveHero` + up to 3 `PartyMember` monsters
- `SaveManager` persists state to disk with `SaveData` serialization (3 manual + 2 auto-save slots)
- No external state management library; state lives in singleton instances

## Key Abstractions

**SingletonMonoBehaviour<T>:**
- Purpose: Base class for all persistent manager singletons
- Location: `Assets/Scripts/Core/SingletonMonoBehaviour.cs`
- Pattern: Generic MonoBehaviour with `DontDestroyOnLoad`, `HasInstance` check, duplicate destruction
- Override `IsPersistent => false` for scene-specific singletons (e.g., `BattleManager`)
- Used by: `GameManager`, `GameDatabase`, `SaveManager`, `VBSceneManager`, `AudioManager`, `InputManager`, `BattleManager`, `ThemeManager`, and all other managers

**EventBus:**
- Purpose: Decoupled game-wide event communication (replaces Godot signals from migration)
- Location: `Assets/Scripts/Core/EventBus.cs`
- Pattern: Static class with `public static event Action<T>` fields and static fire methods
- Categories: Game State, Battle, Status Effects, Combat Synergy, Monster, Hero, Inventory, UI, Audio, Save/Load, Shrine, Progression, Scene
- `ClearAllListeners()` nulls all delegates during cleanup
- Has deprecated `StatusEffect` events alongside new `StatusEffectType` events

**CharSelectEvents (Screen-local events):**
- Purpose: Scoped event bus for the Character Select screen only
- Location: `Assets/Scripts/UI/CharacterSelect/CharSelectEvents.cs`
- Pattern: Same static event pattern as EventBus but with `ClearAll()` on scene exit
- Used by: `CharacterSelectManager` and its sub-controllers

**Combatant:**
- Purpose: Base class for all entities in combat (heroes and monsters)
- Location: `Assets/Scripts/Combat/Combatant.cs`
- Pattern: MonoBehaviour with serialized stats, instance events (`OnHpChanged`, `OnDeath`, etc.), casting system, status effect management
- Uses manual iteration (no LINQ) in hot paths

**GameDataAssets (ScriptableObject):**
- Purpose: Centralized reference to all JSON TextAsset files, eliminating scattered `Resources.Load` calls
- Location: `Assets/Scripts/Core/GameDataAssets.cs`
- Pattern: ScriptableObject singleton with `[CreateAssetMenu]`, fallback path resolution
- Referenced by: `GameDatabase` during initialization

**DamageResult (struct):**
- Purpose: Value type carrying all damage calculation outputs
- Location: `Assets/Scripts/Combat/DamageCalculator.cs`
- Contains: `finalDamage`, `brandMultiplier`, `synergyMultiplier`, `variance`, `isCritical`, `wasBlocked`, `wasDodged`
- Computed properties: `IsSuperEffective`, `IsNotEffective`

## Entry Points

**GameBootstrap:**
- Location: `Assets/Scripts/Core/GameBootstrap.cs`
- Triggers: Scene load of Bootstrap scene (first scene in build settings)
- Responsibilities: Creates all singleton managers in 4 phases, waits for `GameDatabase` readiness, loads MainMenu scene after splash delay

**BattleManager:**
- Location: `Assets/Scripts/Combat/BattleManager.cs`
- Triggers: Battle scene load (scene-specific singleton, `IsPersistent => false`)
- Responsibilities: Initializes combatants, runs combat loop in `Update()`, dispatches abilities, checks win/lose

**MainMenuController:**
- Location: `Assets/Scripts/UI/Menus/MainMenuController.cs`
- Triggers: MainMenu scene load
- Responsibilities: Renders main menu UI, handles New Game / Continue / Settings, async save detection

**CharacterSelectManager:**
- Location: `Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs`
- Triggers: CharacterSelect scene load
- Responsibilities: Hero navigation, embark flow (save creation + scene transition), delegates to sub-controllers

## Error Handling

**Strategy:** Defensive programming with Debug.Log warnings, graceful fallbacks, and null-safe navigation

**Patterns:**
- Null guards at method entry with `Debug.LogWarning/LogError` and early return (see `DamageCalculator.Calculate()`, `GameDatabase` queries)
- `?.` null-conditional operator throughout UI binding code (e.g., `_btnPrev?.RegisterCallback<ClickEvent>`)
- Coroutine timeout patterns: `InitializeWhenReady()` in `CharacterSelectManager` has a 10-second timeout waiting for `GameDatabase`
- Async error handling: `Task.IsFaulted`/`Task.IsCanceled` checks after awaiting save operations (see `CreateOrRotateNewGameSave`)
- `SemaphoreSlim` with 5-second timeout in `SaveManager` to prevent deadlocks on concurrent save/load
- Atomic file writes in `SaveFileHandler` to prevent save corruption
- Backup rotation in `SaveManager` for save recovery
- `_isQuitting` flag in `SingletonMonoBehaviour` prevents errors during application shutdown
- No try/catch in most game logic; exceptions are rare by design (defensive checks prevent them)

## Cross-Cutting Concerns

**Logging:**
- `Debug.Log/LogWarning/LogError` with `[ClassName]` prefix convention (e.g., `[CharSelectManager]`, `[DamageCalculator]`)
- No structured logging framework; Unity console output only
- Logging level controlled by message type (Log = info, LogWarning = recoverable, LogError = broken)

**Validation:**
- Data classes have `Validate()` methods with fallback defaults (see `HeroData.Validate()`)
- `GameDatabase` validates loaded data counts after JSON parsing
- `Constants.cs` centralizes all magic numbers to prevent scattered literals
- No runtime schema validation on JSON data

**Authentication:**
- Not applicable (single-player offline game)

**Input:**
- `InputManager` wraps Unity's new Input System via generated `VeilBreakersInputActions`
- `GameAction` enum with 25+ actions mapping to input bindings
- Polling API: `GetActionDown()`, `GetAction()`, `GetActionUp()`
- Device detection: `IsUsingGamepad` flag for UI adaptation
- Action map switching: `EnableGameplay()` / `EnableUI()` / `DisableAll()`

**Theming:**
- `ThemeManager` singleton provides centralized color lookups for brands, corruption states, rarities, health, surfaces
- Array-based O(1) color lookup by enum index
- Per-hero USS theme classes applied via `ApplyThemeClass()` on the root `VisualElement`

**Scene Management:**
- `VBSceneManager` handles all scene transitions with fade overlay
- Scene constants defined in `VBSceneManager`: Bootstrap, MainMenu, CharacterSelect, TestArena, Battle, Overworld
- `ScreenTransition` provides alternative transition mechanism used by some controllers
- Async loading with `SceneManager.LoadSceneAsync` and progress tracking via `EventBus`

## Key Game Systems

**10-Brand Combat System:**
- Location: `Assets/Scripts/Systems/BrandSystem.cs`
- 10 core brands: IRON, SAVAGE, SURGE, VENOM, DREAD, LEECH, GRACE, MEND, RUIN, VOID
- 6 hybrid brands: IRON_SAVAGE, VOID_SURGE, MEND_GRACE, VENOM_DREAD, LEECH_RUIN, SURGE_IRON
- Each brand is 2x effective against 2 brands, 0.5x against 2 brands, 1x against 6
- Hybrid brands resolve to core via `GetCoreBrand()` for effectiveness lookup
- Constants: `SUPER_EFFECTIVE = 2.0f`, `NOT_EFFECTIVE = 0.5f`, `NEUTRAL = 1.0f`

**4-Path System:**
- Location: `Assets/Scripts/Systems/PathSystem.cs`
- Paths: IRONBOUND, FANGBORN, VOIDTOUCHED, UNCHAINED
- Each path provides stat bonus multipliers (e.g., IRONBOUND: Defense 1.5x, HP 1.2x)
- `[ThreadStatic]` bonus buffer avoids allocation in queries
- Exponential path progression scaling

**Corruption System (0-100%):**
- Location: `Assets/Scripts/Systems/CorruptionSystem.cs`
- Inverted trope: lower corruption = stronger
- 5 tiers: ASCENDED (0-10%, +25%), PURIFIED (11-25%, +10%), UNSTABLE (26-50%, 0%), CORRUPTED (51-75%, -10%), ABYSSAL (76-100%, -20%)
- Affects stat multipliers on monsters
- Purification difficulty scales (harder near Ascension)

**Synergy System:**
- Location: `Assets/Scripts/Systems/SynergySystem.cs`
- Party composition grants tier bonuses: FULL (3/3 matching brands): +8% damage/defense, 0.5x corruption rate, combo unlocked; PARTIAL (2/3): +5%, 0.75x corruption; ANTI (weak brands): 1.5x corruption
- UNCHAINED path always NEUTRAL (flex path)
- Recalculated when party composition changes

**VERA AI Companion:**
- Location: `Assets/Scripts/Systems/VERASystem.cs`
- Veil Integrity (0-100%) drives personality degradation
- 4 states: Normal, Corrupted, Critical, Abyssal
- Glitch system with Zalgo text effects at low integrity
- Priority-based dialogue queue
- Subscribes to EventBus for reactive dialogue triggers

---

*Architecture analysis: 2026-02-21*
