# Codebase Structure

**Analysis Date:** 2026-03-30

## Directory Layout

```
VeilBreakers3DCurrent/
├── Assets/
│   ├── Adaptive Performance/   # Unity Adaptive Performance package config
│   ├── AddressableAssetsData/  # Addressable asset system data
│   ├── Art/                    # Art assets (sprites, textures, models)
│   ├── Audio/                  # Audio clips and configs
│   ├── Characters/             # Character-related assets
│   ├── Data/                   # ScriptableObject data containers (mostly .gitkeep placeholders)
│   ├── Editor/                 # Editor-only scripts and tools
│   ├── Prefabs/                # Prefab assets (Characters, Monsters, UI, VFX)
│   ├── Resources/              # Runtime-loadable assets (JSON data, UI templates, audio, sprites)
│   ├── Scenes/                 # Unity scenes (Bootstrap, MainMenu, CharacterSelect, Battle, Overworld, TestArena)
│   ├── Scripts/                # All C# source code (primary development area)
│   ├── Settings/               # Render pipeline and quality settings
│   ├── Shaders/                # Custom shaders (VeilCrack, VeilDissolve)
│   ├── StreamingAssets/        # Streaming assets
│   ├── Tests/                  # Unity Test Framework tests
│   ├── TextMesh Pro/           # TextMeshPro assets
│   ├── UI/                     # UI Toolkit assets (UXML templates, USS stylesheets)
│   ├── XR/                     # XR/VR settings
│   ├── _Archive/               # Archived/deprecated assets
│   └── _Recovery/              # Recovery backups
├── Docs/                       # Design documents, lore, migration plans, art references
├── Packages/                   # Unity Package Manager manifests
├── ProjectSettings/            # Unity project settings
├── Tools/                      # Build tools, MCP servers, DCC bridge, CI scripts
├── _Archive/                   # Top-level archive (3D models, animations, generated cleanup)
├── gemini-skills/              # Gemini AI skill definitions
├── screenshots/                # Screenshot captures for visual QA
├── test-results/               # Test result output
├── CLAUDE.md                   # Claude AI configuration and project rules
└── VEILBREAKERS.md             # Master game design document and memory
```

## Scripts Directory (Primary Code)

```
Assets/Scripts/
├── AI/                         # AI decision-making (Gambit system)
│   ├── AIPersonality.cs        # AI behavior weight profiles
│   ├── GambitAction.cs         # Action definitions for AI rules
│   ├── GambitCondition.cs      # Condition evaluation for AI rules
│   ├── GambitController.cs     # Main AI controller (attached to Combatant)
│   ├── GambitEvaluator.cs      # Rule evaluation and action selection
│   └── GambitRule.cs           # Individual AI rule definitions
├── Audio/                      # Sound systems
│   ├── AudioBattleIntegration.cs  # Bridge between combat events and audio
│   ├── AudioConfig.cs          # Audio configuration ScriptableObject
│   ├── AudioManager.cs         # Master audio singleton (SFX, volume)
│   ├── AudioTriggers.cs        # Event-based audio trigger helpers
│   ├── LowHealthAudio.cs       # Low HP warning audio singleton
│   ├── MusicManager.cs         # Music playback singleton
│   ├── VB_UISoundManager.cs    # UI-specific sound effects
│   └── VERAVoiceController.cs  # VERA AI companion voice playback
├── Battle/                     # (Empty - combat logic is in Combat/)
├── Capture/                    # Monster capture system
│   ├── BindThresholdCalculator.cs  # Calculates when bind becomes available
│   ├── CaptureData.cs          # Capture-related data types and enums
│   ├── CaptureFormulaCalculator.cs # Capture success probability math
│   ├── CaptureManager.cs       # Capture lifecycle singleton (scene-scoped)
│   └── QTEController.cs        # Quick-time event for capture bonus
├── Characters/                 # (Empty placeholder)
├── Combat/                     # Core combat system
│   ├── BattleManager.cs        # Combat lifecycle singleton (scene-scoped)
│   ├── Combatant.cs            # Base combat participant component
│   └── DamageCalculator.cs     # Static damage/heal formula calculations
├── Commands/                   # Quick command system
│   ├── QuickCommand.cs         # Individual command definition
│   ├── QuickCommandManager.cs  # Command execution manager
│   ├── RadialMenuController.cs # Radial menu UI for commands
│   └── TimeSlowController.cs   # Time-slow effect during command selection
├── Core/                       # Application infrastructure
│   ├── Constants.cs            # All magic numbers and global constants
│   ├── ErrorLogger.cs          # Structured logging utility
│   ├── EventBus.cs             # Static event system (50+ events)
│   ├── GameBootstrap.cs        # System initialization orchestrator
│   ├── GameDataAssets.cs       # ScriptableObject holding JSON TextAsset refs
│   ├── GameDataTypes.cs        # JSON wrapper classes for deserialization
│   ├── GameDatabase.cs         # Central data repository singleton
│   ├── GameManager.cs          # Game state + party management singleton
│   ├── InputManager.cs         # Input system singleton (new Input System)
│   ├── SingletonMonoBehaviour.cs  # Generic persistent singleton base class
│   └── VeilBreakersInputActions.cs # Auto-generated Input System actions
├── Data/                       # Data model definitions
│   ├── AbilityData.cs          # Ability/skill slot data structures
│   ├── Enums.cs                # ALL game enumerations (Brand, Path, SkillType, StatusEffect, etc.)
│   ├── HeroData.cs             # Hero champion data (JSON-loaded)
│   ├── HeroDisplayConfig.cs    # Hero visual configuration for CharSelect
│   ├── ItemData.cs             # Item data (JSON-loaded)
│   ├── MonsterData.cs          # Monster data (JSON-loaded)
│   ├── SaveData.cs             # Save file structure + SavedMonster + SaveSlotMetadata
│   ├── ShrineData.cs           # Shrine/checkpoint data
│   ├── SkillData.cs            # Skill data (JSON-loaded)
│   └── StatusEffectData.cs     # Status effect definition data
├── Editor/                     # Editor-only utilities
│   ├── TestArenaSetup.cs       # Test arena scene setup helper
│   ├── UITextSettingsSetup.cs  # UI text default settings
│   └── Unity6SetupWizard.cs    # Unity 6 project setup wizard
├── Managers/                   # Persistent service managers
│   ├── AutoSaveManager.cs      # Automatic save trigger logic
│   ├── MigrationRunner.cs      # Save file version migration
│   ├── SaveFileHandler.cs      # File I/O, encryption, backup rotation
│   ├── SaveManager.cs          # Save/load orchestrator singleton
│   ├── SettingsManager.cs      # Player settings (audio, graphics, etc.)
│   ├── ShrineManager.cs        # Shrine discovery and interaction
│   ├── StatusEffectManager.cs  # Status effect lifecycle singleton
│   └── VBSceneManager.cs       # Scene loading with fade transitions
├── Monsters/                   # (Empty placeholder)
├── Runtime/                    # (Empty placeholder)
├── Systems/                    # Pure game rule systems (static)
│   ├── BrandSystem.cs          # 10-brand effectiveness matrix
│   ├── CorruptionSystem.cs     # Corruption tiers and stat modifiers
│   ├── PathSystem.cs           # Path bonuses and progression
│   ├── StatusEffectInstance.cs # Runtime status effect instance data
│   ├── SynergySystem.cs        # Party synergy tier calculations
│   └── VERASystem.cs           # VERA AI companion behavior system
├── Test/                       # Test helpers (in-game testing)
├── UI/                         # UI controllers
│   ├── CharacterSelect/        # Character select screen (21 files)
│   │   ├── CarouselController.cs           # Hero carousel navigation
│   │   ├── CharSelectEnvironmentController.cs  # 3D environment control
│   │   ├── CharSelectEvents.cs             # Scene-scoped event hub
│   │   ├── CharSelectFocusManager.cs       # Keyboard/gamepad focus
│   │   ├── CharSelectUIUtils.cs            # Shared UI utilities
│   │   ├── CharSelectVisualEnhancer.cs     # Visual polish effects
│   │   ├── CharacterSelectManager.cs       # Main orchestrator
│   │   ├── EmbarkCinematicController.cs    # Embark cinematic sequence
│   │   ├── GlitchTextEffect.cs             # Glitch text animation
│   │   ├── HeroDataPanelController.cs      # Hero info panel population
│   │   ├── HeroStageController.cs          # 3D hero model stage
│   │   ├── HeroStatsPanelController.cs     # Stats display panel
│   │   ├── HeroSwitchAnimator.cs           # Hero switch animation
│   │   ├── HeroThemeConfig.cs              # Per-hero theme ScriptableObject
│   │   ├── HeroThemeTransitioner.cs        # Theme color transitions
│   │   ├── HoldToEmbarkController.cs       # Hold-to-confirm embark button
│   │   ├── OverlayController.cs            # Overlay effect management
│   │   ├── ScreenEntryAnimator.cs          # Screen entry animation
│   │   ├── StatNumberAnimator.cs           # Stat number roll-up animation
│   │   ├── VeilDissolveController.cs       # Veil dissolve shader control
│   │   └── VeilTransitionController.cs     # Veil transition effects
│   ├── Combat/                 # Combat HUD (10 files)
│   │   ├── AllyPanelController.cs          # Individual ally status panel
│   │   ├── CaptureBannerController.cs      # Capture availability banner
│   │   ├── CombatHUD.cs                    # Main combat HUD orchestrator
│   │   ├── CombatUIConfig.cs               # Combat UI configuration
│   │   ├── EnemyPanelController.cs         # Enemy target info panel
│   │   ├── HealthBarController.cs          # Animated health bar
│   │   ├── PlayerPanelController.cs        # Player status panel
│   │   ├── SkillBarController.cs           # Skill slot bar (6 slots)
│   │   └── SkillSlotController.cs          # Individual skill slot
│   ├── Controls/               # Reusable UI controls
│   │   ├── AnimatedBar.cs                  # Generic animated progress bar
│   │   ├── ButtonVFXHelper.cs              # Button visual effects helper
│   │   └── VBDropdownField.cs              # Custom dropdown field
│   ├── Core/                   # UI infrastructure
│   │   ├── FPSCounter.cs                   # FPS overlay display
│   │   ├── MenuBootstrap.cs                # Menu scene initialization
│   │   ├── MenuVFXController.cs            # Menu VFX management
│   │   ├── MoltenButtonVFX.cs              # Molten button visual effect
│   │   ├── MoltenVeinVFX.cs                # Molten vein background effect
│   │   ├── ParallaxBackground.cs           # Parallax scrolling background
│   │   ├── ScreenTransition.cs             # Screen transition effects
│   │   ├── SoulSwarmVFX.cs                 # Soul particle swarm effect
│   │   ├── ThemeManager.cs                 # Brand/corruption color theming
│   │   ├── TitleScreenAudio.cs             # Title screen music/SFX
│   │   ├── TitleScreenVFX.cs               # Title screen visual effects
│   │   ├── UIAnimationController.cs        # Centralized UI animation runner
│   │   ├── UIAssets.cs                     # UI asset reference holder
│   │   ├── UIAutoSetup.cs                  # Automatic UI setup helpers
│   │   └── UIGradientHelper.cs             # Runtime gradient texture generation
│   ├── Effects/                # UI visual effects
│   │   └── MainMenuVFXOverlayController.cs # Main menu VFX overlay
│   └── Menus/                  # Menu screen controllers
│       ├── HeroMonsterPairPreview.cs       # Hero+monster preview display
│       ├── InventoryController.cs          # Inventory screen
│       ├── MainMenuBootstrap.cs            # Main menu scene bootstrap
│       ├── MainMenuController.cs           # Main menu UI controller
│       ├── MainMenuVFXController.cs        # Main menu VFX
│       ├── MonsterCollectionController.cs  # Monster collection browser
│       ├── SaveSlotBrowserController.cs    # Save slot selection UI
│       ├── SettingsPanelController.cs      # Settings panel UI
│       └── VERADialogueController.cs       # VERA dialogue display
├── Utils/                      # Generic utilities
│   ├── Extensions.cs           # C# extension methods
│   └── ObjectPool.cs           # Generic object pooling
└── VFX/                        # Brand-specific VFX scripts
    ├── VB_AoEVFX_ground_circle_RUIN.cs # RUIN brand AOE VFX
    ├── VB_HitVFX_SAVAGE.cs             # SAVAGE brand hit VFX
    └── VB_StatusVFX_SURGE.cs           # SURGE brand status VFX
```

## UI Toolkit Assets

```
Assets/UI/
├── Screens/
│   └── CharacterSelect.uxml       # Character select screen layout
├── Styles/
│   ├── CharacterSelect.uss        # Character select styles
│   └── VeilBreakers.uss           # Global shared styles
└── Templates/
    ├── Dialogue.uxml              # Dialogue box template
    ├── Inventory.uxml             # Inventory screen template
    ├── MainMenu.uxml              # Main menu template
    ├── MonsterCollection.uxml     # Monster collection template
    └── SettingsPanel.uxml         # Settings panel template

Assets/Resources/UI/Templates/
├── CharacterSelect.uxml           # CharacterSelect (Resources copy for runtime loading)
└── MainMenu.uxml                  # MainMenu (Resources copy for runtime loading)
```

## Scenes

```
Assets/Scenes/
├── Bootstrap.unity         # First scene loaded; contains GameBootstrap
├── MainMenu.unity          # Title screen with menu UI
├── CharacterSelect.unity   # Hero selection screen with 3D stage
├── Battle.unity            # Combat arena
├── Overworld.unity         # Exploration/overworld
└── TestArena.unity         # Development testing scene
```

## JSON Data Files

```
Assets/Resources/Data/
├── GameDataAssets.asset    # ScriptableObject referencing JSON TextAssets
├── heroes.json             # 4 hero definitions (Vex, Seraphina, Orion, Nyx)
├── items.json              # Item definitions
├── monsters.json           # Monster definitions
└── skills.json             # Skill/ability definitions
```

## Directory Purposes

**`Assets/Scripts/Core/`:**
- Purpose: Foundation infrastructure that everything depends on
- Contains: Singletons, event bus, game state, input, data loading
- Key files: `GameBootstrap.cs` (init order), `EventBus.cs` (all events), `Constants.cs` (all magic numbers)

**`Assets/Scripts/Data/`:**
- Purpose: Pure data definitions with no behavior
- Contains: Serializable classes for JSON data, all enums, save file structures
- Key files: `Enums.cs` (ALL enums in one file), `SaveData.cs` (save format with validation)

**`Assets/Scripts/Systems/`:**
- Purpose: Stateless game rule calculations
- Contains: Static classes with pure math functions
- Key files: `BrandSystem.cs` (10x10 effectiveness matrix), `SynergySystem.cs` (party tier calc)

**`Assets/Scripts/Combat/`:**
- Purpose: Combat runtime logic
- Contains: Battle lifecycle, combatant state, damage formulas
- Key files: `BattleManager.cs` (combat orchestrator), `Combatant.cs` (universal combat entity)

**`Assets/Scripts/UI/CharacterSelect/`:**
- Purpose: Character selection screen (most complex UI in the project)
- Contains: 21 controller files following orchestrator + sub-controller delegation pattern
- Key files: `CharacterSelectManager.cs` (orchestrator), `CharSelectEvents.cs` (scene-scoped events)

**`Assets/Scripts/UI/Combat/`:**
- Purpose: Combat HUD panels
- Contains: Panel controllers for player, enemy, allies, skill bar, capture
- Key files: `CombatHUD.cs` (orchestrator), `SkillBarController.cs` (6-slot skill bar)

**`Assets/Resources/`:**
- Purpose: Runtime-loadable assets (loaded via `Resources.Load`)
- Contains: JSON game data, UI templates, audio clips, sprites, hero configs
- Generated: No
- Committed: Yes

**`Assets/Data/`:**
- Purpose: ScriptableObject data containers (currently placeholder .gitkeep files)
- Contains: Empty subdirectories for Brands, Items, Monsters, Skills
- Note: Actual JSON data lives in `Assets/Resources/Data/`

**`Docs/`:**
- Purpose: Design documents, lore, migration plans, art references
- Contains: `MIGRATION_PLAN.md`, art references, lore archive, legacy Godot docs, superpowers brainstorms

**`Tools/`:**
- Purpose: Build automation, MCP servers, DCC bridge, CI pipelines
- Contains: Python scripts, Git hooks, CI configs, MCP server definitions

## Key File Locations

**Entry Points:**
- `Assets/Scripts/Core/GameBootstrap.cs`: Application startup, system initialization
- `Assets/Scripts/UI/Menus/MainMenuController.cs`: Main menu screen controller
- `Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs`: Character select orchestrator
- `Assets/Scripts/Combat/BattleManager.cs`: Combat lifecycle manager

**Configuration:**
- `Assets/Scripts/Core/Constants.cs`: All magic numbers, timing, colors, resource paths
- `Assets/Scripts/Data/Enums.cs`: ALL game enumerations (Brand, Path, SkillType, StatusEffectType, BattleState, etc.)
- `Assets/Scripts/Audio/AudioConfig.cs`: Audio configuration ScriptableObject
- `Assets/Scripts/UI/Combat/CombatUIConfig.cs`: Combat UI configuration

**Core Logic:**
- `Assets/Scripts/Core/GameManager.cs`: Game state, party management, hero/monster stats
- `Assets/Scripts/Core/EventBus.cs`: All game events (50+ static Action delegates)
- `Assets/Scripts/Core/GameDatabase.cs`: Central data repository (async JSON loading)
- `Assets/Scripts/Combat/DamageCalculator.cs`: Damage formula implementation
- `Assets/Scripts/Systems/BrandSystem.cs`: Brand effectiveness matrix
- `Assets/Scripts/Systems/SynergySystem.cs`: Synergy tier calculation
- `Assets/Scripts/Systems/CorruptionSystem.cs`: Corruption state and modifiers
- `Assets/Scripts/Systems/PathSystem.cs`: Path bonuses and progression

**Persistence:**
- `Assets/Scripts/Managers/SaveManager.cs`: Save/load orchestrator (async, encrypted)
- `Assets/Scripts/Managers/SaveFileHandler.cs`: File I/O, encryption, backup rotation
- `Assets/Scripts/Managers/MigrationRunner.cs`: Save version migration
- `Assets/Scripts/Managers/AutoSaveManager.cs`: Auto-save trigger logic
- `Assets/Scripts/Data/SaveData.cs`: Save file data structure

**Testing:**
- `Assets/Tests/`: Unity Test Framework tests
- `Assets/Scripts/Editor/TestArenaSetup.cs`: Test arena setup utility

## Naming Conventions

**Files:**
- PascalCase for all C# files: `GameManager.cs`, `BrandSystem.cs`
- Prefix `VB_` for brand-specific VFX: `VB_HitVFX_SAVAGE.cs`
- Prefix `VB` for project-specific managers: `VBSceneManager.cs`, `VBDropdownField.cs`
- JSON data files: lowercase with underscores: `monsters.json`, `skills.json`

**Directories:**
- PascalCase for script folders: `CharacterSelect/`, `Combat/`, `Core/`
- Lowercase for asset folders: `saves/` (runtime), `screenshots/`

**Namespaces:**
- Root: `VeilBreakers`
- Pattern: `VeilBreakers.[Folder]` matching directory structure:
  - `VeilBreakers.Core` -> `Assets/Scripts/Core/`
  - `VeilBreakers.Data` -> `Assets/Scripts/Data/`
  - `VeilBreakers.Combat` -> `Assets/Scripts/Combat/`
  - `VeilBreakers.Systems` -> `Assets/Scripts/Systems/`
  - `VeilBreakers.Managers` -> `Assets/Scripts/Managers/`
  - `VeilBreakers.AI` -> `Assets/Scripts/AI/`
  - `VeilBreakers.Capture` -> `Assets/Scripts/Capture/`
  - `VeilBreakers.Audio` -> `Assets/Scripts/Audio/`
  - `VeilBreakers.Commands` -> `Assets/Scripts/Commands/`
  - `VeilBreakers.UI.Core` -> `Assets/Scripts/UI/Core/`
  - `VeilBreakers.UI.Menus` -> `Assets/Scripts/UI/Menus/`
  - `VeilBreakers.UI.Combat` -> `Assets/Scripts/UI/Combat/`
  - `VeilBreakers.UI.CharacterSelect` -> `Assets/Scripts/UI/CharacterSelect/`
  - `VeilBreakers.UI.Controls` -> `Assets/Scripts/UI/Controls/`
  - `VeilBreakers.UI.Effects` -> `Assets/Scripts/UI/Effects/`

## Where to Add New Code

**New Game System (e.g., crafting, quest, loot):**
- Pure rules: `Assets/Scripts/Systems/NewSystem.cs` (static class, namespace `VeilBreakers.Systems`)
- Manager singleton: `Assets/Scripts/Managers/NewManager.cs` (extends `SingletonMonoBehaviour<T>`, namespace `VeilBreakers.Managers`)
- Data model: `Assets/Scripts/Data/NewData.cs` (serializable class, namespace `VeilBreakers.Data`)
- Register singleton in `GameBootstrap.Initialize()` at `Assets/Scripts/Core/GameBootstrap.cs`
- Add events to `EventBus.cs` and add cleanup to `ClearAllListeners()`

**New UI Screen:**
- Controller: `Assets/Scripts/UI/Menus/NewScreenController.cs` (MonoBehaviour, namespace `VeilBreakers.UI.Menus`)
- UXML template: `Assets/UI/Templates/NewScreen.uxml`
- USS styles: `Assets/UI/Styles/NewScreen.uss` (or extend `VeilBreakers.uss`)
- If needs runtime loading: also place UXML in `Assets/Resources/UI/Templates/`
- Scene: Add UIDocument component with UXML reference to scene GameObject

**New Combat Feature (e.g., new skill type, combo system):**
- Logic: Add to `Assets/Scripts/Combat/BattleManager.cs` or create new file in `Assets/Scripts/Combat/`
- UI: Add panel controller in `Assets/Scripts/UI/Combat/`
- Wire events through `CombatHUD.cs` -> `BattleManager`

**New Monster / Hero / Skill / Item:**
- JSON data: Edit `Assets/Resources/Data/monsters.json` (or heroes/skills/items.json)
- Sprite: `Assets/Resources/Art/Sprites/monsters/` (or heroes/)
- No C# changes needed (data-driven via GameDatabase)

**New Brand VFX:**
- Script: `Assets/Scripts/VFX/VB_[Type]VFX_[BRAND].cs`
- Particle prefab: `Assets/Prefabs/VFX/`

**New AI Behavior:**
- Personality: Create `AIPersonality` ScriptableObject asset
- Custom rules: Create `GambitRuleSetAsset` ScriptableObject
- Attach `GambitController` component to Combatant GameObject

**New Status Effect:**
- Add enum value to `StatusEffectType` in `Assets/Scripts/Data/Enums.cs`
- Add `StatusEffectData` entry in `GameDataAssets` StatusEffects array
- `StatusEffectManager` handles application/tick/removal automatically

**New Save Data Field:**
- Add field to `SaveData` in `Assets/Scripts/Data/SaveData.cs`
- Increment `SaveVersion.CURRENT`
- Add migration in `MigrationRunner`
- Add validation in `SaveData.ValidateAndRepair()`

**Utilities:**
- Shared helpers: `Assets/Scripts/Utils/Extensions.cs` or new file in `Assets/Scripts/Utils/`
- Object pooling: Use `ObjectPool` at `Assets/Scripts/Utils/ObjectPool.cs`

## Special Directories

**`Assets/_Archive/`:**
- Purpose: Archived assets no longer in active use
- Generated: No (manually moved)
- Committed: Yes

**`Assets/_Recovery/`:**
- Purpose: Recovery backups from broken states
- Generated: No (manually created)
- Committed: Yes

**`Assets/Resources/`:**
- Purpose: Assets loadable via `Resources.Load()` at runtime
- Generated: No
- Committed: Yes
- Note: Keep minimal; prefer direct references via SerializeField or GameDataAssets

**`Library/`, `Temp/`, `Logs/`:**
- Purpose: Unity-generated caches and logs
- Generated: Yes
- Committed: No (gitignored)

**`TempCompileCheck/`:**
- Purpose: Temporary compilation verification project
- Generated: Yes (by tooling)
- Committed: Partially

**`Tools/`:**
- Purpose: External tooling (MCP servers, DCC bridge, CI, Git hooks)
- Generated: No
- Committed: Yes

---

*Structure analysis: 2026-03-30*
