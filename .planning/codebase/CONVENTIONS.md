# Coding Conventions

**Analysis Date:** 2026-02-21

## Naming Patterns

**Files:**
- PascalCase for all C# files: `GameManager.cs`, `BrandSystem.cs`, `MonsterData.cs`
- Test files use suffix `Tests`: `CaptureTests.cs`, `GambitTests.cs`, `SaveSystemTests.cs`
- ScriptableObject data files use suffix `Data`: `StatusEffectData.cs`, `ShrineData.cs`, `MonsterData.cs`
- Config ScriptableObjects use suffix `Config`: `HeroDisplayConfig.cs`, `CombatUIConfig.cs`, `AudioConfig.cs`

**Namespaces:**
- Root: `VeilBreakers`
- Pattern: `VeilBreakers.[Category]`
- Categories: `Core`, `Data`, `Systems`, `Managers`, `Combat`, `AI`, `Capture`, `Commands`, `Audio`, `Utils`, `Test`
- UI uses dot-separated sub-namespaces: `VeilBreakers.UI.Core`, `VeilBreakers.UI.Combat`, `VeilBreakers.UI.Controls`, `VeilBreakers.UI.Menus`, `VeilBreakers.UI.CharacterSelect`
- One namespace per file, matching directory structure

**Constants:**
- String/path constants use `k` prefix with PascalCase: `kGameScene`, `kMainMenuScene`, `kConfigPath`, `kPrefix`, `kCombatPrefix`
  - Example from `Assets/Scripts/Core/ErrorLogger.cs`: `private const string kPrefix = "[VB]";`
  - Example from `Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs`: `private const string kGameScene = "Overworld";`
- USS class name constants use `k` prefix: `private const string kBaseClass = "animated-bar";`
  - Example from `Assets/Scripts/UI/Controls/AnimatedBar.cs`
- Numeric/game-balance constants use SCREAMING_SNAKE_CASE: `BASE_CRIT_RATE`, `MAX_LEVEL`, `SUPER_EFFECTIVE`
  - Example from `Assets/Scripts/Core/Constants.cs`: `public const float BASE_CRIT_RATE = 0.05f;`
  - Example from `Assets/Scripts/Systems/BrandSystem.cs`: `public const float SUPER_EFFECTIVE = 2.0f;`
- Static readonly values (Color, Vector3) use SCREAMING_SNAKE with type prefix: `COLOR_GOLD`, `COLOR_HEALTH_GREEN`
  - Example from `Assets/Scripts/Core/Constants.cs`: `public static readonly Color COLOR_GOLD = new Color(1f, 0.84f, 0f);`

**Private Fields:**
- Underscore prefix: `_currentIndex`, `_heroList`, `_isTransitioning`, `_uiDocument`
- Always `[SerializeField]` when exposed to Inspector: `[SerializeField] private UIDocument _uiDocument;`
- Never public fields -- use `[SerializeField] private` with a property if external access needed

**Properties:**
- PascalCase: `CurrentIndex`, `CurrentHero`, `HeroCount`, `IsTransitioning`
- Prefer expression-bodied: `public int CurrentIndex => _currentIndex;`
- Complex properties use expression body too:
  ```csharp
  public HeroData CurrentHero => _heroList != null && _currentIndex >= 0 && _currentIndex < _heroList.Count
      ? _heroList[_currentIndex] : null;
  ```

**Events:**
- Static events use `On` prefix with PascalCase: `OnGameStarted`, `OnBattleEnded`, `OnDamageDealt`
- Fire methods omit `On` prefix: `GameStarted()`, `BattleEnded(bool victory)`, `DamageDealt(...)`
- Type: `public static event Action<TArgs>` (not UnityEvent, not custom delegate)
- Example from `Assets/Scripts/Core/EventBus.cs`:
  ```csharp
  public static event Action<string, string, int, bool> OnDamageDealt;
  public static void DamageDealt(string source, string target, int amount, bool isCrit)
      => OnDamageDealt?.Invoke(source, target, amount, isCrit);
  ```

**Methods:**
- PascalCase: `NavigateToHero()`, `LoadHeroData()`, `ApplyThemeClass()`
- Private helpers prefixed with verb: `EnsureCriticalManagers()`, `CacheUIReferences()`, `UpdateEmbarkText()`
- Event handlers use `On` prefix: `OnPrevClicked()`, `OnNavigationMove()`, `OnConfirmClicked()`
- Callback methods use expression body for simple delegation:
  ```csharp
  private void OnPrevClicked(ClickEvent evt) => NavigatePrev();
  ```

**Enums:**
- Enum type names: PascalCase (`Brand`, `StatusEffectType`, `AIPattern`)
- Enum values: SCREAMING_SNAKE_CASE with explicit integer assignments:
  ```csharp
  public enum Brand { NONE = 0, IRON = 1, SAVAGE = 2, SURGE = 3, ... }
  ```
- Defined centrally in `Assets/Scripts/Data/Enums.cs`

**Local Variables:**
- camelCase: `heroName`, `prevIndex`, `newIndex`, `themeClass`

**Parameters:**
- camelCase: `heroId`, `effectType`, `monsterId`

## Code Style

**Formatting:**
- No `.editorconfig` or `.prettierrc` detected -- relies on IDE defaults (likely Rider or Visual Studio)
- 4-space indentation (standard C#)
- Opening brace on same line for methods/classes (Allman style for namespace/class, K&R for control flow)
- Single blank line between methods
- No trailing whitespace enforced

**Linting:**
- No Roslyn analyzers or StyleCop detected
- Relies on Unity compiler warnings
- `#pragma warning disable CS0618` used selectively for obsolete API calls
  - Example from `Assets/Scripts/Core/EventBus.cs` lines 92-97

**Section Organization:**
- Files use prominent section separators (81-character line of `=`):
  ```csharp
  // =============================================================================
  // SECTION NAME
  // =============================================================================
  ```
- Standard section order in MonoBehaviour:
  1. CONSTANTS
  2. SERIALIZED FIELDS
  3. STATE (private fields)
  4. CACHED UI REFERENCES (if UI class)
  5. PROPERTIES
  6. LIFECYCLE (OnEnable, OnDisable, OnDestroy)
  7. DATA LOADING / INITIALIZATION
  8. PUBLIC API
  9. PRIVATE HELPERS
  10. EVENT HANDLERS

**Line Length:**
- No strict limit enforced; lines occasionally exceed 120 characters for long signatures

## Import Organization

**Order:**
1. System namespaces (`System`, `System.Collections`, `System.Collections.Generic`, `System.IO`, `System.Threading.Tasks`)
2. Unity namespaces (`UnityEngine`, `UnityEngine.SceneManagement`, `UnityEngine.UIElements`)
3. Project namespaces (`VeilBreakers.Core`, `VeilBreakers.Data`, `VeilBreakers.Managers`, `VeilBreakers.UI.Core`)

**Path Aliases:**
- Type aliasing used to resolve conflicts:
  ```csharp
  using GamePath = VeilBreakers.Data.Path;  // Avoid conflict with System.IO.Path
  ```
  Example from `Assets/Scripts/Test/SaveSystemTests.cs` line 9

**No barrel files or index files used** -- each file imports what it needs directly.

## Error Handling

**Patterns:**
- Early return with null checks:
  ```csharp
  if (_uiDocument == null) { Debug.LogError("[CharacterSelectManager] UIDocument not assigned!"); return; }
  ```
- Null-conditional operator for optional references: `_btnPrev?.RegisterCallback<ClickEvent>(OnPrevClicked);`
- Try/catch in test code only -- production code uses guard clauses
- Coroutine timeout pattern:
  ```csharp
  float timeout = 10f;
  float elapsed = 0f;
  while (!ready) {
      elapsed += Time.deltaTime;
      if (elapsed > timeout) { Debug.LogError("Timed out"); yield break; }
      yield return null;
  }
  ```
  Example from `Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs` lines 143-154
- Task-to-coroutine bridge (polling `IsCompleted`):
  ```csharp
  var task = manager.SomeAsyncMethod();
  while (!task.IsCompleted) yield return null;
  if (task.IsFaulted || task.IsCanceled) { Debug.LogWarning("Failed"); yield break; }
  ```

**Error Severity:**
- `Debug.LogError()` for critical failures (missing references, system init failures)
- `Debug.LogWarning()` for recoverable issues (missing configs, fallback used)
- `Debug.Log()` for informational/diagnostic output

## Logging

**Framework:** `UnityEngine.Debug` with centralized `ErrorLogger` wrapper

**Patterns:**
- Direct logging uses bracketed class/system prefix: `[CharSelectManager]`, `[EventBus]`, `[GameBootstrap]`
- Centralized logging via `Assets/Scripts/Core/ErrorLogger.cs`:
  ```csharp
  ErrorLogger.Combat("Damage calculated", source, target);
  ErrorLogger.UI("Screen loaded");
  ErrorLogger.AI("Decision made");
  ```
- Subsystem prefixes: `[VB]`, `[VB:Combat]`, `[VB:UI]`, `[VB:AI]`, `[VB:Capture]`, `[VB:Settings]`
- Conditional compilation: logging methods marked `[Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]`
- Performance timing:
  ```csharp
  ErrorLogger.BeginTiming("operation_name");
  // ... work ...
  ErrorLogger.EndTiming("operation_name"); // Logs elapsed ms
  ```

## Comments

**When to Comment:**
- XML doc comments (`///`) on all public methods and classes
- Inline comments for non-obvious logic (e.g., `// Wrap around`, `// Walk up the visual tree`)
- Section separators for file organization (see Code Style above)
- Parameter documentation via inline comments on event declarations:
  ```csharp
  public static event Action<string, string, int, bool> OnDamageDealt;  // source, target, amount, isCrit
  ```

**XML Doc:**
- `<summary>` tags on classes and public methods
- Used consistently in core infrastructure, less consistently in UI controllers
- Example from `Assets/Scripts/Systems/BrandSystem.cs`:
  ```csharp
  /// <summary>
  /// Get damage multiplier between attacker and defender brands
  /// </summary>
  public static float GetEffectiveness(Brand attacker, Brand defender)
  ```

**Deprecation:**
- Use `[Obsolete("Use X instead")]` attribute with migration guidance
- Example from `Assets/Scripts/Core/EventBus.cs`:
  ```csharp
  [Obsolete("Use StatusEffectApplied(GameObject, StatusEffectType) instead")]
  public static event Action<string, StatusEffect, int> OnStatusApplied;
  ```

## Function Design

**Size:** Methods are generally 5-30 lines. Longer methods are split by section separators within the class.

**Parameters:**
- Prefer primitives and data objects over raw tuples
- Use string IDs extensively: `heroId`, `monsterId`, `skillId`, `shrineId`
- Avoid `out` parameters; return values or data objects instead

**Return Values:**
- Void for fire-and-forget operations and event handlers
- Bool for success/failure: `bool success = await SaveAsync(slot);`
- Nullable references for "not found": `return null;`
- Expression-bodied for simple returns

## Module Design

**Exports:**
- One primary class per file
- Supporting types (nested classes, small data structs) may live in the same file
- Example: `GameManager.cs` contains nested `PartyMember` and `ActiveHero` classes

**Barrel Files:**
- Not used. Each file imports its dependencies explicitly.

**Static Utility Classes:**
- Used for stateless systems: `BrandSystem`, `EventBus`, `ErrorLogger`, `Constants`
- Pattern: `public static class SystemName { ... }`

**Singleton Pattern:**
- Base class: `Assets/Scripts/Core/SingletonMonoBehaviour.cs`
- Used by: `GameManager`, `GameDatabase`, `SaveManager`, `ShrineManager`, `ThemeManager`, `ScreenTransition`
- Access: `GameManager.Instance`, `SaveManager.HasInstance`
- Lifecycle: `_isQuitting` guard prevents recreation during application quit
- Override `IsPersistent` to control DontDestroyOnLoad behavior

**ScriptableObject Pattern:**
- Used for: game data configs, display configs, audio configs, status effect definitions
- Always use `[CreateAssetMenu]` attribute:
  ```csharp
  [CreateAssetMenu(fileName = "NewStatusEffect", menuName = "VeilBreakers/Status Effect")]
  public class StatusEffectData : ScriptableObject
  ```
- Heavy use of Inspector attributes: `[Header]`, `[Tooltip]`, `[Range]`, `[TextArea]`
- `#if UNITY_EDITOR` guard on `OnValidate()` methods

**UI Toolkit Custom Elements:**
- Use `[UxmlElement]` attribute with `partial class`:
  ```csharp
  [UxmlElement]
  public partial class AnimatedBar : VisualElement
  ```
- Bindable properties use `[UxmlAttribute]`
- USS class constants use `k` prefix: `private const string kBaseClass = "animated-bar";`

**Event-Driven Communication:**
- Global events: `Assets/Scripts/Core/EventBus.cs` (static Action events)
- Scoped events: `CharSelectEvents` (screen-specific static events with `ClearAll()`)
- UI binding: `RegisterCallback<ClickEvent>()` / `UnregisterCallback<ClickEvent>()` paired in `BindUI()`/`UnbindUI()`
- Always unsubscribe in `OnDisable()` or `OnDestroy()`

**Extension Methods:**
- Centralized in `Assets/Scripts/Utils/Extensions.cs`
- Namespace: `VeilBreakers.Utils`
- Organized by target type with section separators
- Include safety guards (null checks, division-by-zero protection)

---

*Convention analysis: 2026-02-21*
