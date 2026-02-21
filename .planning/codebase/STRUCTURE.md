# Codebase Structure

**Analysis Date:** 2026-02-21

## Directory Layout

```
VeilBreakers3DCurrent/
├── Assets/
│   ├── Art/                        # 3D models, textures, materials, VFX
│   ├── Editor/                     # Unity Editor extensions (not in builds)
│   ├── Prefabs/                    # Reusable GameObject prefabs
│   ├── Resources/                  # Runtime-loadable assets (JSON data, UI)
│   │   ├── Data/                   # Game data JSON files
│   │   ├── UI/                     # UI Toolkit assets (duplicated from Assets/UI/)
│   │   │   ├── Styles/             # USS stylesheets (runtime copies)
│   │   │   └── Templates/          # UXML templates (runtime copies)
│   │   └── CharacterSelect/        # Character select ScriptableObjects
│   │       └── HeroDisplayConfigs/ # Per-hero display config assets
│   ├── Scenes/                     # Unity scene files
│   │   └── Test/                   # Test scenes
│   ├── Scripts/                    # All C# source code
│   │   ├── AI/                     # Monster combat AI
│   │   ├── Audio/                  # Audio system scripts
│   │   ├── Capture/                # Monster capture mechanics
│   │   ├── Combat/                 # Battle system, combatants, damage
│   │   ├── Commands/               # Command pattern implementations
│   │   ├── Core/                   # Singletons, bootstrap, event bus, input
│   │   ├── Data/                   # Enums, data models (Monster, Hero, Skill, Item, Save)
│   │   ├── Editor/                 # Editor-only scripts (separate assembly)
│   │   ├── Managers/               # Persistent service managers
│   │   ├── Systems/                # Pure game logic (Brand, Path, Corruption, Synergy, VERA)
│   │   ├── Test/                   # Test helper scripts
│   │   ├── UI/                     # UI controllers and components
│   │   │   ├── CharacterSelect/    # Character select screen controllers
│   │   │   ├── Combat/             # Battle HUD controllers
│   │   │   ├── Controls/           # Reusable UI controls
│   │   │   ├── Core/               # Shared UI infrastructure (ThemeManager, ScreenTransition)
│   │   │   ├── Effects/            # UI visual effects
│   │   │   └── Menus/              # Menu screen controllers (MainMenu, Settings, etc.)
│   │   └── Utils/                  # Shared utility classes
│   ├── Settings/                   # Unity project settings assets
│   ├── Tests/                      # Unit and integration tests
│   │   ├── EditMode/               # Edit-mode tests (no scene required)
│   │   └── PlayMode/               # Play-mode tests (scene required)
│   ├── UI/                         # UI Toolkit source files
│   │   ├── Screens/                # Full-screen UXML layouts
│   │   ├── Styles/                 # USS stylesheets (source of truth)
│   │   └── Templates/              # Reusable UXML templates
│   └── _Archive/                   # Deprecated/archived code (do NOT use)
│       └── OldCharacterSelect/     # Previous character select UXML files
├── Docs/                           # Project documentation
├── screenshots/                    # Visual reference screenshots
├── .planning/                      # GSD planning documents
│   └── codebase/                   # Codebase analysis documents
├── CLAUDE.md                       # Claude Code project instructions
└── VEILBREAKERS.md                 # Project state and memory document
```

## Directory Purposes

**`Assets/Scripts/Core/`:**
- Purpose: Application foundation -- bootstrapping, singleton infrastructure, global systems
- Contains: Singleton base class, game bootstrap, game manager, database, event bus, input, constants
- Key files:
  - `GameBootstrap.cs`: First code to run; creates all managers in phased order
  - `GameManager.cs`: Central game state (GameState enum), party management, hero/monster runtime data
  - `GameDatabase.cs`: Async JSON data loader; query methods for monsters/heroes/skills/items
  - `SingletonMonoBehaviour.cs`: Generic persistent singleton base class
  - `EventBus.cs`: Static event system (~50+ events for decoupled communication)
  - `InputManager.cs`: Unity Input System wrapper with `GameAction` enum and polling API
  - `Constants.cs`: All magic numbers, colors, resource paths, tags/layers
  - `GameDataAssets.cs`: ScriptableObject holding references to all JSON TextAssets
  - `GameDataTypes.cs`: JSON wrapper classes for Unity's `JsonUtility`

**`Assets/Scripts/Data/`:**
- Purpose: Data models and enumerations used across all layers
- Contains: JSON-serializable data classes, game enums
- Key files:
  - `Enums.cs`: All game enumerations (Brand, Path, CorruptionState, SkillType, StatusEffectType with 60+ effects, ItemCategory, MonsterRarity, AIPattern, HeroRole, etc.)
  - `MonsterData.cs`: Monster identity, brands, stats, growth rates, skills, AI config, corruption, rewards, lore
  - `HeroData.cs`: 4 heroes (Vex, Seraphina, Orion, Nyx) with D&D-style BaseStats, ResourceType per hero
  - `SkillData.cs`: Skill definitions with types, targets, power, cooldowns
  - `ItemData.cs`: Item definitions with categories and effects
  - `SaveData.cs`: Serializable save game state

**`Assets/Scripts/Systems/`:**
- Purpose: Pure game logic -- stateless static classes that calculate game mechanics
- Contains: Brand effectiveness, path bonuses, corruption tiers, synergy tiers, VERA AI
- Key files:
  - `BrandSystem.cs`: 10-brand effectiveness matrix (2x/0.5x/1x) + hybrid brand resolution
  - `PathSystem.cs`: 4 path stat bonus profiles with `[ThreadStatic]` buffer optimization
  - `CorruptionSystem.cs`: 5-tier corruption state with stat multipliers (inverted: lower = stronger)
  - `SynergySystem.cs`: Party composition synergy tiers (FULL/PARTIAL/NEUTRAL/ANTI)
  - `VERASystem.cs`: AI companion with Veil Integrity, personality states, glitch text, dialogue queue

**`Assets/Scripts/Combat/`:**
- Purpose: Real-time battle system orchestration and combat entities
- Contains: Battle manager, combatant component, damage formulas, skill execution, guard system, AI
- Key files:
  - `BattleManager.cs`: Scene-specific singleton; combat loop in `Update()`, ability dispatch, synergy recalculation
  - `Combatant.cs`: MonoBehaviour for all combat entities; stats, events, casting system, status effects
  - `DamageCalculator.cs`: Static damage formula: `BasePower * (ATK/DEF) * BrandMult * SynergyMult * Variance * CritMult`

**`Assets/Scripts/Managers/`:**
- Purpose: Persistent service singletons that manage cross-scene concerns
- Contains: Save/load, scene transitions, audio, settings, status effects, shrines, auto-save
- Key files:
  - `SaveManager.cs`: 3 manual + 2 auto-save slots, async atomic writes, SemaphoreSlim mutex, backup rotation
  - `VBSceneManager.cs`: Scene loading with programmatic fade overlay, async loading with progress
  - `AudioManager.cs`: Sound effect playback
  - `MusicManager.cs`: Background music management
  - `SettingsManager.cs`: Player settings persistence
  - `AutoSaveManager.cs`: Automatic save triggers on progression events
  - `StatusEffectManager.cs`: Global status effect tick management
  - `ShrineManager.cs`: Shrine discovery and interaction tracking

**`Assets/Scripts/UI/`:**
- Purpose: All user interface controllers using UI Toolkit (UXML/USS)
- Contains: Per-screen controller hierarchies, shared UI infrastructure, effects
- Key subdirectories:
  - `CharacterSelect/`: `CharacterSelectManager.cs` (orchestrator) + sub-controllers (hero info, ability display, model viewer, etc.)
  - `Combat/`: Battle HUD controllers
  - `Controls/`: Reusable custom UI controls
  - `Core/`: `ThemeManager.cs` (centralized colors), `ScreenTransition.cs` (fade effects)
  - `Effects/`: UI visual effect scripts (particles, glow, etc.)
  - `Menus/`: `MainMenuController.cs` (893 lines, entrance animations, async save detection), settings panels

**`Assets/Scripts/AI/`:**
- Purpose: Monster combat AI decision-making
- Contains: AI behavior patterns for enemy combatants during battle

**`Assets/Scripts/Capture/`:**
- Purpose: Monster capture mechanics and calculations
- Contains: Capture rate formulas and capture flow logic

**`Assets/Scripts/Utils/`:**
- Purpose: Shared utility functions used across multiple layers
- Contains: Extension methods, helper classes, common algorithms

**`Assets/UI/`:**
- Purpose: UI Toolkit source files (UXML layouts and USS stylesheets)
- Contains: Screen layouts, reusable templates, theme and component styles
- Key files:
  - `Screens/CharacterSelect.uxml`: Character select screen layout
  - `Templates/MainMenu.uxml`: Main menu layout template
  - `Templates/Dialogue.uxml`, `Inventory.uxml`, `MonsterCollection.uxml`, `SettingsPanel.uxml`
  - `Styles/VeilBreakers.uss`: Base stylesheet
  - `Styles/VeilBreakersTheme.uss`: Theme variables and tokens
  - `Styles/VeilBreakersUI.uss`: Component styles
  - `Styles/CharacterSelect.uss`, `CharacterSelectAAA.uss`: Character select specific styles

**`Assets/Resources/Data/`:**
- Purpose: JSON game data files loaded at runtime by `GameDatabase`
- Contains:
  - `monsters.json`: All monster definitions
  - `heroes.json`: All hero definitions
  - `skills.json`: All skill definitions
  - `items.json`: All item definitions
  - `monsters_archive_v1.json`, `skills_archive_v1.json`: Archived data versions

**`Assets/Scenes/`:**
- Purpose: Unity scene files defining the game flow
- Contains:
  - `Bootstrap.unity`: First scene; runs `GameBootstrap` to initialize all managers
  - `MainMenu.unity`: Main menu screen
  - `CharacterSelect.unity`: Hero selection before starting a new game
  - `Overworld.unity`: Exploration/overworld gameplay
  - `Battle.unity`: Combat encounters
  - `TestArena.unity`: Development test scene
  - `Test/TestArena.unity`: Additional test scene

**`Assets/Editor/`:**
- Purpose: Unity Editor extensions and tools (excluded from builds)
- Contains: Texture generators, import postprocessors, DCC bridge editors, scene wiring utilities, `SceneAuditor.cs`

**`Assets/_Archive/`:**
- Purpose: Deprecated code kept for reference (do NOT import or use)
- Contains: `OldCharacterSelect/` with previous character select UXML files

## Key File Locations

**Entry Points:**
- `Assets/Scripts/Core/GameBootstrap.cs`: Application entry point; creates all managers
- `Assets/Scripts/Combat/BattleManager.cs`: Battle scene entry point
- `Assets/Scripts/UI/Menus/MainMenuController.cs`: Main menu screen controller
- `Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs`: Character select orchestrator

**Configuration:**
- `Assets/Scripts/Core/Constants.cs`: All game constants, magic numbers, resource paths
- `Assets/Scripts/Core/GameDataAssets.cs`: ScriptableObject referencing JSON data TextAssets
- `Assets/Resources/Data/*.json`: Game data (monsters, heroes, skills, items)
- `CLAUDE.md`: Project-level Claude Code instructions
- `VEILBREAKERS.md`: Project state and memory document

**Core Logic:**
- `Assets/Scripts/Systems/BrandSystem.cs`: Brand effectiveness matrix
- `Assets/Scripts/Systems/SynergySystem.cs`: Party synergy calculations
- `Assets/Scripts/Systems/CorruptionSystem.cs`: Corruption tier logic
- `Assets/Scripts/Systems/PathSystem.cs`: Path stat bonuses
- `Assets/Scripts/Combat/DamageCalculator.cs`: Damage formula
- `Assets/Scripts/Combat/Combatant.cs`: Combat entity base class

**Persistence:**
- `Assets/Scripts/Managers/SaveManager.cs`: Save/load with atomic writes and backup rotation
- `Assets/Scripts/Data/SaveData.cs`: Serializable save data structure
- `Assets/Scripts/Managers/AutoSaveManager.cs`: Auto-save triggers

**Testing:**
- `Assets/Tests/EditMode/`: Edit-mode unit tests
- `Assets/Tests/PlayMode/`: Play-mode integration tests
- `Assets/Scripts/Test/`: Test helper scripts

## Assembly Definitions

**`Assets/Scripts/VeilBreakers.Runtime.asmdef`:**
- Main runtime assembly containing all game code
- Referenced by Editor and Test assemblies

**`Assets/Scripts/Editor/VeilBreakers.Editor.asmdef`:**
- Editor-only scripts; excluded from builds
- References: VeilBreakers.Runtime

**`Assets/Tests/PlayMode/VeilBreakers.Tests.PlayMode.asmdef`:**
- Play-mode tests requiring scene and MonoBehaviour lifecycle
- References: VeilBreakers.Runtime

**`Assets/Tests/EditMode/VeilBreakers.Tests.EditMode.asmdef`:**
- Edit-mode tests for pure logic (no scene required)
- References: VeilBreakers.Runtime

## Naming Conventions

**Files:**
- PascalCase for all C# files: `BattleManager.cs`, `DamageCalculator.cs`, `MonsterData.cs`
- Suffix pattern: `*Manager` (persistent singleton), `*System` (static logic), `*Controller` (UI), `*Data` (data model)
- Scene files: PascalCase (`MainMenu.unity`, `CharacterSelect.unity`)
- UXML: PascalCase (`CharacterSelect.uxml`, `MainMenu.uxml`)
- USS: PascalCase (`VeilBreakers.uss`, `CharacterSelect.uss`)
- JSON data: lowercase (`monsters.json`, `heroes.json`)

**Directories:**
- PascalCase: `Combat/`, `Core/`, `Systems/`, `UI/`, `Data/`, `Managers/`
- Map to namespace segments: `Assets/Scripts/Combat/` -> `namespace VeilBreakers.Combat`

**C# Naming:**
- Namespaces: `VeilBreakers.[Category]` (e.g., `VeilBreakers.Combat`, `VeilBreakers.UI.CharacterSelect`)
- Classes: PascalCase (`BattleManager`, `DamageCalculator`)
- Constants: `k` prefix (`kVarianceMin`, `kMaxPartySize`, `kGameScene`)
- Private fields: `_` prefix (`_heroList`, `_currentIndex`, `_isTransitioning`)
- Serialized fields: `[SerializeField] private Type _name`
- Properties: PascalCase (`CurrentHero`, `IsTransitioning`, `HeroCount`)
- Events: `On` prefix (`OnDeath`, `OnHpChanged`, `OnGameStarted`)
- Static event fire methods: PascalCase verb (`GameStarted()`, `DamageDealt()`, `MonsterCaptured()`)
- Enums: UPPER_SNAKE_CASE values (`IRONBOUND`, `SUPER_EFFECTIVE`, `IN_BATTLE`)

**UXML Element IDs:**
- kebab-case: `btn-prev`, `btn-embark`, `confirm-overlay`, `embark-text`, `embark-glow`

**USS Classes:**
- kebab-case: `theme-vex`, `theme-seraphina`, `hidden`, `breathing`

## Where to Add New Code

**New Game System (e.g., crafting, questing):**
- Pure logic: `Assets/Scripts/Systems/NewSystem.cs` (static class in `VeilBreakers.Systems`)
- Persistent manager: `Assets/Scripts/Managers/NewManager.cs` (extends `SingletonMonoBehaviour<T>`)
- Register new manager in `Assets/Scripts/Core/GameBootstrap.cs` in the appropriate phase
- Add events to `Assets/Scripts/Core/EventBus.cs` for cross-system communication
- Add relevant enums to `Assets/Scripts/Data/Enums.cs`

**New Data Type:**
- Data model: `Assets/Scripts/Data/NewData.cs` (JSON-serializable class in `VeilBreakers.Data`)
- JSON file: `Assets/Resources/Data/newdata.json`
- Add TextAsset reference to `Assets/Scripts/Core/GameDataAssets.cs`
- Add loading logic to `Assets/Scripts/Core/GameDatabase.cs`
- Add wrapping type to `Assets/Scripts/Core/GameDataTypes.cs` if needed

**New UI Screen:**
- UXML layout: `Assets/UI/Screens/NewScreen.uxml`
- USS styles: `Assets/UI/Styles/NewScreen.uss`
- Controller: `Assets/Scripts/UI/[Category]/NewScreenController.cs` (in `VeilBreakers.UI.[Category]`)
- Sub-controllers: `Assets/Scripts/UI/[Category]/NewScreen[Part]Controller.cs`
- Scene: `Assets/Scenes/NewScreen.unity` (if dedicated scene) or add to existing scene
- Add scene constant to `Assets/Scripts/Managers/VBSceneManager.cs` if new scene
- Use UI Toolkit patterns: `UIDocument`, `rootVisualElement.Q<T>("element-id")`, callback registration

**New Combat Feature:**
- Combat logic: `Assets/Scripts/Combat/NewFeature.cs` (in `VeilBreakers.Combat`)
- Integrate with `Assets/Scripts/Combat/BattleManager.cs` for battle loop
- Add damage/effect types to `Assets/Scripts/Data/Enums.cs`
- Add events to `Assets/Scripts/Core/EventBus.cs`

**New Monster/Hero:**
- Add entry to `Assets/Resources/Data/monsters.json` or `Assets/Resources/Data/heroes.json`
- No code changes needed if data schema is unchanged
- For new hero: add theme class in USS, add `HeroDisplayConfig` ScriptableObject

**New Tests:**
- Edit-mode: `Assets/Tests/EditMode/NewTest.cs` (pure logic, no scene)
- Play-mode: `Assets/Tests/PlayMode/NewTest.cs` (requires MonoBehaviour lifecycle)
- Test helpers: `Assets/Scripts/Test/`

**Utilities:**
- Shared helpers: `Assets/Scripts/Utils/`

## Special Directories

**`Assets/Resources/`:**
- Purpose: Assets loadable at runtime via `Resources.Load()`
- Generated: No (manually managed)
- Committed: Yes
- Note: Contains duplicate copies of UI Toolkit files from `Assets/UI/`. The source of truth for UXML/USS is `Assets/UI/`; `Assets/Resources/UI/` exists for runtime loading fallback.

**`Assets/_Archive/`:**
- Purpose: Deprecated code preserved for reference
- Generated: No
- Committed: Yes
- Note: Do NOT reference or use archived code in new work. It exists only for historical context.

**`Assets/Editor/`:**
- Purpose: Editor-only scripts (texture generators, import postprocessors, DCC bridge, scene auditors)
- Generated: No
- Committed: Yes
- Note: Excluded from runtime builds. Has its own assembly definition.

**`Assets/Settings/`:**
- Purpose: Unity project settings assets (render pipeline, quality, input)
- Generated: Partially (some by Unity, some manual)
- Committed: Yes

**`.planning/`:**
- Purpose: GSD workflow planning documents and codebase analysis
- Generated: By GSD tooling
- Committed: Yes

---

*Structure analysis: 2026-02-21*
