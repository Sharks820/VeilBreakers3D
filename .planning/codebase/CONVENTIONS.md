# Coding Conventions

**Analysis Date:** 2026-03-30

## Namespace Conventions

**Root namespace:** `VeilBreakers`

**Sub-namespaces map to script directories:**
- `VeilBreakers.Core` - `Assets/Scripts/Core/`
- `VeilBreakers.Combat` - `Assets/Scripts/Combat/`
- `VeilBreakers.Data` - `Assets/Scripts/Data/`
- `VeilBreakers.Systems` - `Assets/Scripts/Systems/`
- `VeilBreakers.Managers` - `Assets/Scripts/Managers/`
- `VeilBreakers.Audio` - `Assets/Scripts/Audio/`
- `VeilBreakers.AI` - `Assets/Scripts/AI/`
- `VeilBreakers.Capture` - `Assets/Scripts/Capture/`
- `VeilBreakers.Commands` - `Assets/Scripts/Commands/`
- `VeilBreakers.UI.CharacterSelect` - `Assets/Scripts/UI/CharacterSelect/`
- `VeilBreakers.UI.Combat` - `Assets/Scripts/UI/Combat/`
- `VeilBreakers.UI.Controls` - `Assets/Scripts/UI/Controls/`
- `VeilBreakers.UI.Core` - `Assets/Scripts/UI/Core/`
- `VeilBreakers.UI.Effects` - `Assets/Scripts/UI/Effects/`
- `VeilBreakers.UI.Menus` - `Assets/Scripts/UI/Menus/`
- `VeilBreakers.Utils` - `Assets/Scripts/Utils/`
- `VeilBreakers.VFX` - `Assets/Scripts/VFX/`

**Assembly definitions:**
- Runtime: `VeilBreakers.Runtime` (`Assets/Scripts/VeilBreakers.Runtime.asmdef`) - root namespace `VeilBreakers`
- EditMode Tests: `VeilBreakers.Tests.EditMode` (`Assets/Tests/EditMode/VeilBreakers.Tests.EditMode.asmdef`)
- PlayMode Tests: `VeilBreakers.Tests.PlayMode` (`Assets/Tests/PlayMode/VeilBreakers.Tests.PlayMode.asmdef`)

**Rule:** One namespace per file, matching directory structure. When creating a new file in `Assets/Scripts/Combat/`, use namespace `VeilBreakers.Combat`.

## Naming Patterns

**Files:**
- One class per file, filename matches class name: `BrandSystem.cs` contains `BrandSystem`
- PascalCase for all files: `GameManager.cs`, `DamageCalculator.cs`
- ScriptableObject data files use suffix `Data`: `MonsterData.cs`, `StatusEffectData.cs`
- ScriptableObject configs use suffix `Config`: `HeroThemeConfig.cs`, `AudioConfig.cs`, `HeroDisplayConfig.cs`

**Constants:**
- `k` prefix with PascalCase for private/local string constants:
  ```csharp
  // From Assets/Scripts/Core/ErrorLogger.cs
  private const string kPrefix = "[VB]";
  private const string kCombatPrefix = "[VB:Combat]";

  // From Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs
  private const string kGameScene = "Overworld";
  private const string kBtnPrev = "btn-prev";
  private const int kTabOverview = 0;
  ```
- `k` prefix for numeric private constants:
  ```csharp
  // From Assets/Scripts/Combat/DamageCalculator.cs
  private const float kVarianceMin = 0.9f;
  private const float kVarianceMax = 1.1f;
  private const int kMaxPartySize = 6;
  ```
- UPPER_CASE for public game-balance constants:
  ```csharp
  // From Assets/Scripts/Systems/BrandSystem.cs
  public const float SUPER_EFFECTIVE = 2.0f;
  public const float NOT_EFFECTIVE = 0.5f;

  // From Assets/Scripts/Core/Constants.cs
  public const int MAX_PARTY_SIZE = 3;
  public const float BASE_CRIT_RATE = 0.05f;
  ```
- Static readonly for non-primitive public constants (Color, Vector2):
  ```csharp
  // From Assets/Scripts/Core/Constants.cs
  public static readonly Color COLOR_GOLD = new Color(1f, 0.84f, 0f);
  public static readonly Vector2 BUTTON_MEDIUM = new Vector2(200, 50);
  ```

**Private Fields:**
- `_` prefix always: `_currentIndex`, `_heroList`, `_isTransitioning`, `_party`
- Always `[SerializeField] private` when exposed to Inspector -- never public fields
  ```csharp
  [SerializeField] private UIDocument _uiDocument;
  [SerializeField] private BattleState _state = BattleState.INITIALIZING;
  ```

**Properties:**
- PascalCase: `CurrentHp`, `IsAlive`, `Brand`, `DisplayName`
- Expression-bodied for simple getters:
  ```csharp
  public bool IsAlive => _isAlive;
  public BattleState State => _state;
  public IReadOnlyList<Combatant> PlayerParty => _playerParty;
  ```

**Events:**
- `On` prefix for event declarations: `OnBattleStart`, `OnDamageDealt`, `OnCombatantDeath`
- Static events in `EventBus` follow same pattern: `OnGameStarted`, `OnBattleEnded`
- Fire methods match event name without `On`: `GameStarted()`, `BattleEnded()`
- Type: `event Action<TArgs>` (not UnityEvent, not custom delegates)
  ```csharp
  // From Assets/Scripts/Core/EventBus.cs
  public static event Action<string, string, int, bool> OnDamageDealt;  // source, target, amount, isCrit
  public static void DamageDealt(string source, string target, int amount, bool isCrit)
      => OnDamageDealt?.Invoke(source, target, amount, isCrit);
  ```

**Enums:**
- PascalCase type name: `Brand`, `BattleState`, `DamageType`
- UPPER_CASE values with explicit integer assignments:
  ```csharp
  public enum Brand { NONE = 0, IRON = 1, SAVAGE = 2, SURGE = 3, ... }
  ```
- All game enums centralized in `Assets/Scripts/Data/Enums.cs`

**Methods:**
- PascalCase: `Calculate()`, `AddToParty()`, `GetEffectiveness()`
- Boolean methods use `Is`/`Has`/`Can` prefix: `IsHybridBrand()`, `HasAdvantage()`, `IsItemEffective()`
- Event handlers use `On` prefix: `OnPrevClicked()`, `OnNavigationMove()`

**Local variables and parameters:** camelCase: `heroId`, `heroName`, `prevIndex`, `effectType`

## Code Organization Within Files

**Section headers use 77-char `=` banner comments:**
```csharp
// =============================================================================
// SECTION NAME
// =============================================================================
```

**Standard section order in a MonoBehaviour:**
1. CONSTANTS
2. CONFIGURATION / SERIALIZED FIELDS (grouped by `[Header("...")]`)
3. STATE (private fields)
4. PROPERTIES
5. UNITY LIFECYCLE (Awake/OnSingletonAwake, Start, Update, OnDestroy)
6. INITIALIZATION
7. PUBLIC API METHODS
8. PRIVATE HELPERS
9. EVENT HANDLERS
10. LOGGING

**Example from `Assets/Scripts/Core/GameManager.cs`:**
```csharp
namespace VeilBreakers.Core
{
    public class GameManager : SingletonMonoBehaviour<GameManager>
    {
        // =============================================================================
        // GAME STATE
        // =============================================================================
        public enum GameState { MainMenu, Exploring, InBattle, ... }

        // =============================================================================
        // PARTY DATA
        // =============================================================================
        [Serializable]
        public class PartyMember { ... }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================
        protected override void OnSingletonAwake() { ... }

        // =============================================================================
        // STATE MANAGEMENT
        // =============================================================================
        public void ChangeState(GameState newState) { ... }
    }
}
```

**Test files use `====` banners (68-char) for test groupings:**
```csharp
// ====================================================================
// EFFECTIVENESS MATRIX - SUPER EFFECTIVE (2x)
// ====================================================================
```

## SerializeField Patterns

**Group Inspector fields with `[Header("Section")]`:**
```csharp
// From Assets/Scripts/Combat/Combatant.cs
[Header("Identity")]
[SerializeField] private string _combatantId;
[SerializeField] private Brand _brand = Brand.NONE;

[Header("Stats")]
[SerializeField] private int _maxHp = 100;
[SerializeField] private int _currentHp = 100;

[Header("State")]
[SerializeField] private bool _isAlive = true;
```

**Always provide default values:**
```csharp
[SerializeField] private BattleState _state = BattleState.INITIALIZING;
[SerializeField] private bool _initializeOnAwake = true;
[SerializeField] private float _minimumSplashTime = 1.0f;
```

## Singleton Pattern

**Use `SingletonMonoBehaviour<T>` from `Assets/Scripts/Core/SingletonMonoBehaviour.cs`:**
```csharp
public class GameManager : SingletonMonoBehaviour<GameManager>
{
    protected override void OnSingletonAwake()
    {
        // Initialize here instead of Awake()
    }
}
```

**Key rules:**
- Access via `GameManager.Instance`
- Null-safe check via `GameManager.HasInstance`
- Automatic `DontDestroyOnLoad` (override `IsPersistent => false` for scene-specific singletons)
- Domain-reload safe via `SingletonResetHelper`
- Duplicate instances are auto-destroyed with a warning

**Scene-specific singletons:**
```csharp
// From Assets/Scripts/Combat/BattleManager.cs
public class BattleManager : SingletonMonoBehaviour<BattleManager>
{
    protected override bool IsPersistent => false;
}
```

**Bootstrap initialization order in `Assets/Scripts/Core/GameBootstrap.cs`:**
1. Core: `GameManager`, `GameDatabase`, `InputManager`
2. Persistence: `SettingsManager`, `VBSceneManager`, `SaveManager`, `AutoSaveManager`
3. Audio: `AudioManager`, `MusicManager`, `VERAVoiceController`, `LowHealthAudio`
4. Gameplay: `StatusEffectManager`, `ShrineManager`, `FPSCounter`

## Event Architecture

**Global events via static `EventBus` (`Assets/Scripts/Core/EventBus.cs`):**
```csharp
// Subscribe
EventBus.OnBattleStarted += HandleBattleStarted;
// Publish
EventBus.BattleStarted();
```

**Component-level events via standard C# events:**
```csharp
// From Assets/Scripts/Combat/BattleManager.cs
public event Action OnBattleStart;
public event Action<Combatant, Combatant, DamageResult> OnDamageDealt;
public event Action<Combatant> OnCombatantDeath;
```

**Domain reload safety (MANDATORY for all static state):**
```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
private static void ResetStatics()
{
    _minLevel = LogLevel.Debug;
}
```

Every class with static fields must implement this pattern. `EventBus.ClearAllListeners()` and `SingletonResetHelper` handle their respective resets.

## Error Handling

**Use `ErrorLogger` from `Assets/Scripts/Core/ErrorLogger.cs` for subsystem-tagged logging:**
```csharp
ErrorLogger.Log("General info");          // [VB] - stripped in release
ErrorLogger.Warn("Something unexpected");  // [VB] - kept in release
ErrorLogger.Error("Something broke");      // [VB] - kept in release
ErrorLogger.Combat("Damage calculated");   // [VB:Combat] - stripped in release
ErrorLogger.Save("Slot saved");            // [VB:Save]
ErrorLogger.UI("Panel opened");            // [VB:UI]
ErrorLogger.AI("Decision made");           // [VB:AI]
ErrorLogger.Capture("Monster bound");      // [VB:Capture]
ErrorLogger.Settings("Volume changed");    // [VB:Settings]
```

**Debug-only methods use `[Conditional]` attributes** -- auto-stripped from release builds:
```csharp
[Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
public static void Log(string message) { ... }
```

**Performance timing:**
```csharp
var sw = ErrorLogger.BeginTiming("LoadDatabase");
// ... work ...
ErrorLogger.EndTiming(sw, "LoadDatabase", warnThresholdMs: 16.67f);
```

**Direct `Debug.Log` acceptable in core singletons with `[ClassName]` prefix:**
```csharp
Debug.Log("[GameManager] State changed: MainMenu -> Exploring");
Debug.LogError("[GameBootstrap] Failed to initialize AudioManager");
```

**Null guard pattern at public API boundaries:**
```csharp
// From Assets/Scripts/Combat/DamageCalculator.cs
if (attacker == null || defender == null)
{
    Debug.LogWarning("[DamageCalculator] Null combatant in damage calculation");
    result.finalDamage = basePower > 0 ? basePower : 1;
    return result;
}
```

**Assertions (debug-only):**
```csharp
ErrorLogger.Assert(hp >= 0, "HP should never be negative");
ErrorLogger.AssertNotNull(combatant, "combatant");
```

## Import Organization

**Order:**
1. `System` and `System.*` namespaces
2. `UnityEngine` and `UnityEngine.*` namespaces
3. `UnityEditor` namespaces (Editor-only files)
4. Third-party packages (`PrimeTween`, `Unity.Profiling`, etc.)
5. `VeilBreakers.*` project namespaces

**Example from `Assets/Scripts/Combat/BattleManager.cs`:**
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using VeilBreakers.Core;
using VeilBreakers.Data;
using VeilBreakers.Systems;
```

**Type aliases for name conflicts:**
```csharp
// From Assets/Scripts/Managers/SaveManager.cs
using IOPath = System.IO.Path;  // Avoids conflict with VeilBreakers.Data.Path
```

**No barrel files or index files.** Each file imports its dependencies explicitly.

## Data Patterns

**JSON-loaded data uses `snake_case` fields for serialization compatibility:**
```csharp
// From Assets/Scripts/Data/MonsterData.cs
[Serializable]
public class MonsterData
{
    public string monster_id;
    public string display_name;
    public int base_hp;
    public float hp_growth;
    public string[] innate_skills;
    public List<LearnableSkillEntry> learnable_skills_list;
}
```

**ScriptableObjects use standard C# camelCase fields:**
```csharp
// From Assets/Scripts/UI/CharacterSelect/HeroThemeConfig.cs
public class HeroThemeConfig : ScriptableObject
{
    public Color primaryColor;
    public Color glowColor;
    public float musicIntensity;
    public float scanlineOpacity;
}
```

**Constants centralized in `Assets/Scripts/Core/Constants.cs`:**
- All magic numbers belong here
- `static readonly` for non-primitive types (`Vector2`, `Color`)
- `const` for primitive types
- Helper methods for computed values: `GetHPColor()`, `GetExpForLevel()`

## Game System Design Patterns

**Pure static classes for stateless game logic (no MonoBehaviour):**
- `Assets/Scripts/Systems/BrandSystem.cs` - brand effectiveness matrix
- `Assets/Scripts/Systems/CorruptionSystem.cs` - corruption state/modifier calculations
- `Assets/Scripts/Systems/SynergySystem.cs` - synergy tier calculations
- `Assets/Scripts/Systems/PathSystem.cs` - path bonus calculations
- `Assets/Scripts/Combat/DamageCalculator.cs` - damage formula

**Manager singletons for stateful runtime systems:**
- `Assets/Scripts/Core/GameManager.cs` - game state, party management
- `Assets/Scripts/Managers/SaveManager.cs` - save/load operations
- `Assets/Scripts/Audio/AudioManager.cs` - audio playback, bank management
- `Assets/Scripts/Combat/BattleManager.cs` - battle orchestration

## Performance Conventions

**Pre-allocate buffers to avoid GC in Update:**
```csharp
// From Assets/Scripts/Combat/BattleManager.cs
private const int kMaxPartySize = 6;
private Brand[] _brandBuffer = new Brand[kMaxPartySize];
```

**Cache WaitForSeconds:**
```csharp
// From Assets/Scripts/Core/GameBootstrap.cs
private static readonly WaitForSeconds kTestDelayWait = new WaitForSeconds(0.5f);
```

**Use HashSet for O(1) membership lookups:**
```csharp
// From Assets/Scripts/Combat/BattleManager.cs
private readonly HashSet<Combatant> _playerPartySet = new HashSet<Combatant>();
```

**Expose read-only collections:**
```csharp
private readonly List<PartyMember> _party = new List<PartyMember>();
public IReadOnlyList<PartyMember> Party => _party;
```

**Forbidden in Update loops** (enforced by `unity-antipattern-guard.js` hook):
- `Find()`, `FindObjectOfType()`, `FindObjectsByType()`
- `GetComponent()` (must cache in Awake/Start)
- `Camera.main` (cache the reference)
- LINQ queries (allocate enumerators)
- `new List/Dictionary/string[]` heap allocations
- `Resources.Load()` (cache loaded resources)

## VB-IGNORE Comment Pattern

**Suppress automated code review warnings:**
```csharp
// Format: // VB-IGNORE {CODE} -- {justification}
private Dictionary<string, float> _bankLastUsed = new Dictionary<string, float>(); // VB-IGNORE BUG-34 -- not serialized, runtime-only tracking
```

Known code prefixes:
- `BUG-{N}` - Specific bug pattern detector
- `SEC-{N}` - Security warning
- `DEEP-{N}` - Deep analysis warning
- `UNITY-{N}` - Unity-specific warning
- `TASK-{N}` - Task-related warning

Always include a justification after `--`.

## Deprecation Pattern

**Use `[Obsolete]` with migration guidance:**
```csharp
// From Assets/Scripts/Core/EventBus.cs
[Obsolete("Use StatusEffectApplied(GameObject, StatusEffectType) instead")]
public static event Action<string, StatusEffect, int> OnStatusApplied;
```

**Suppress obsolete warnings at usage sites:**
```csharp
#pragma warning disable CS0618
public static void StatusApplied(string target, StatusEffect effect, int duration)
    => OnStatusApplied?.Invoke(target, effect, duration);
#pragma warning restore CS0618
```

## Claude Hook Quality Gates

**Active hooks in `.claude/hooks/` that enforce conventions automatically:**

| Hook | Trigger | Purpose |
|------|---------|---------|
| `blind-edit-guard.js` | PreToolUse (Edit) | Warns when editing a .cs file without reading it first |
| `unity-antipattern-guard.js` | PreToolUse (Edit/Write) | Blocks Find/GetComponent/LINQ/Camera.main/heap allocs/Resources.Load in files with Update loops |
| `protect-critical-files.js` | PreToolUse (Edit/Write) | Warns before modifying brand/path/corruption/synergy/save/combat/capture system files |
| `guard-destructive.js` | PreToolUse | Guards against destructive git operations |
| `track-cs-edits.js` | PreToolUse | Tracks C# file edits for session awareness |

## XML Documentation

**Required on all public methods and classes:**
```csharp
/// <summary>
/// Calculate damage for an attack
/// </summary>
public static DamageResult Calculate(Combatant attacker, Combatant defender, ...)
```

**Inline event parameter docs via trailing comments:**
```csharp
public static event Action<string, string, int, bool> OnDamageDealt;  // source, target, amount, isCrit
```

## Code Style

**Formatting:**
- No `.editorconfig` or Roslyn analyzers configured
- 4-space indentation (standard C#)
- Allman-style braces for classes/methods, K&R-style for control flow
- No strict line length limit; lines occasionally exceed 120 chars for long signatures

**Linting:**
- Unity compiler warnings only
- `#pragma warning disable/restore` used selectively for intentional warnings
- Hook-based enforcement for Unity anti-patterns (see Quality Gates above)

---

*Convention analysis: 2026-03-30*
