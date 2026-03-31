# Testing Patterns

**Analysis Date:** 2026-03-30

## Test Framework

**Runner:**
- Unity Test Framework (NUnit-based) for structured EditMode and PlayMode tests
- Assembly: `Assets/Tests/EditMode/` and `Assets/Tests/PlayMode/`
- Legacy MonoBehaviour-based test harness still exists in `Assets/Scripts/Test/` (being superseded)

**Assertion Library:**
- NUnit (`nunit.framework.dll`) via Unity Test Runner
- `Assert.AreEqual()`, `Assert.IsTrue()`, `Assert.Greater()`, `Assert.Less()`, etc.

**Run Commands:**
```bash
# Unity CLI (batch mode)
Unity.exe -batchmode -runTests -testPlatform EditMode -projectPath .
Unity.exe -batchmode -runTests -testPlatform PlayMode -projectPath .

# In Unity Editor
# Window > General > Test Runner > EditMode tab > Run All
# Window > General > Test Runner > PlayMode tab > Run All
```

## Test File Organization

**Location:**
- EditMode tests: `Assets/Tests/EditMode/` (separate from production code)
- PlayMode tests: `Assets/Tests/PlayMode/` (separate from production code)
- Legacy tests: `Assets/Scripts/Test/` (MonoBehaviour-based, pre-NUnit migration)

**Naming:**
- EditMode: `{SystemName}_EditModeTests.cs`
- PlayMode: `{SystemName}_PlayModeTests.cs`
- Class name matches filename exactly

**Structure:**
```
Assets/Tests/
├── EditMode/
│   ├── VeilBreakers.Tests.EditMode.asmdef
│   ├── BrandSystem_EditModeTests.cs
│   ├── CaptureSystem_EditModeTests.cs
│   ├── CorruptionSystem_EditModeTests.cs
│   ├── DamageCalculator_EditModeTests.cs
│   ├── HeroThemeConfig_EditModeTests.cs
│   ├── MainMenuAssets_EditModeTests.cs
│   ├── PrimeTween_Integration_EditModeTests.cs
│   ├── SceneIntegrity_EditModeTests.cs
│   └── SynergySystem_EditModeTests.cs
└── PlayMode/
    ├── VeilBreakers.Tests.PlayMode.asmdef
    ├── CharacterSelect_PlayModeTests.cs
    └── MainMenuOverlay_PlayModeTests.cs
```

## Assembly Definitions

**EditMode test assembly (`Assets/Tests/EditMode/VeilBreakers.Tests.EditMode.asmdef`):**
```json
{
  "name": "VeilBreakers.Tests.EditMode",
  "rootNamespace": "VeilBreakers.Tests.EditMode",
  "references": [
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner",
    "VeilBreakers.Runtime",
    "PrimeTween.Runtime"
  ],
  "includePlatforms": ["Editor"],
  "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll"],
  "defineConstraints": ["UNITY_INCLUDE_TESTS"]
}
```

**PlayMode test assembly (`Assets/Tests/PlayMode/VeilBreakers.Tests.PlayMode.asmdef`):**
```json
{
  "name": "VeilBreakers.Tests.PlayMode",
  "rootNamespace": "VeilBreakers.Tests.PlayMode",
  "references": [
    "VeilBreakers.Runtime",
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner"
  ],
  "includePlatforms": [],
  "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll"],
  "defineConstraints": ["UNITY_INCLUDE_TESTS"]
}
```

## Test Structure

**EditMode Test Pattern (pure logic, no scene required):**
```csharp
// From Assets/Tests/EditMode/BrandSystem_EditModeTests.cs
using NUnit.Framework;
using VeilBreakers.Data;
using VeilBreakers.Systems;

namespace VeilBreakers.Tests.EditMode
{
    public class BrandSystem_EditModeTests
    {
        [Test]
        [Category("Suite.Core")]
        [Category("System.Brand")]
        public void Iron_SuperEffective_Against_Surge_And_Dread()
        {
            Assert.AreEqual(2.0f, BrandSystem.GetEffectiveness(Brand.IRON, Brand.SURGE));
            Assert.AreEqual(2.0f, BrandSystem.GetEffectiveness(Brand.IRON, Brand.DREAD));
        }
    }
}
```

**EditMode Test with SetUp/TearDown (requires GameObjects):**
```csharp
// From Assets/Tests/EditMode/DamageCalculator_EditModeTests.cs
public class DamageCalculator_EditModeTests
{
    private GameObject _attackerGO;
    private GameObject _defenderGO;
    private Combatant _attacker;
    private Combatant _defender;

    [SetUp]
    public void SetUp()
    {
        _attackerGO = new GameObject("TestAttacker");
        _defenderGO = new GameObject("TestDefender");
        _attacker = _attackerGO.AddComponent<Combatant>();
        _attacker.Initialize("test_attacker", "Test Attacker", Brand.IRON, 100, 50, 20, 10, 10, 10, 10, true);
        _defender = _defenderGO.AddComponent<Combatant>();
        _defender.Initialize("test_defender", "Test Defender", Brand.SURGE, 100, 50, 10, 15, 10, 10, 10, false);
    }

    [TearDown]
    public void TearDown()
    {
        if (_attackerGO != null) Object.DestroyImmediate(_attackerGO);
        if (_defenderGO != null) Object.DestroyImmediate(_defenderGO);
    }
}
```

**PlayMode Test Pattern (requires scene and coroutine):**
```csharp
// From Assets/Tests/PlayMode/CharacterSelect_PlayModeTests.cs
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace VeilBreakers.Tests.PlayMode
{
    public class CharacterSelect_PlayModeTests
    {
        private const int kMaxPollFrames = 600; // ~10 seconds at 60fps

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return null;
        }

        [UnityTest]
        [Category("Suite.Smoke")]
        [Category("Phase.PreProd")]
        public IEnumerator CharacterSelect_DisplaysHeroIdentityAndStats()
        {
            yield return SceneManager.LoadSceneAsync("CharacterSelect", LoadSceneMode.Single);

            UIDocument doc = null;
            for (int i = 0; i < kMaxPollFrames; i++)
            {
                doc = Object.FindFirstObjectByType<UIDocument>();
                if (doc != null && doc.rootVisualElement != null) break;
                yield return null;
            }

            Assert.NotNull(doc, "UIDocument not found in CharacterSelect.");
            // ... validate UI element content ...
        }
    }
}
```

## Test Categories

**Use NUnit `[Category]` attributes for filtering. Two-axis categorization:**

**Suite categories (test scope/criticality):**
- `Suite.Core` - Core game system logic (brand, synergy, corruption, damage)
- `Suite.Smoke` - Quick sanity checks (scene loads, assets exist, UI renders)
- `Suite.Integration` - Cross-system integration (PrimeTween assembly resolves)
- `Suite.Integrity` - Structural integrity (no missing scripts, resources exist)
- `Suite.Perf` - Performance benchmarks (GC allocation budgets)

**System categories (what's being tested):**
- `System.Brand` - Brand effectiveness matrix
- `System.Combat` - Damage calculator, combatant lifecycle
- `System.Corruption` - Corruption tiers, stat modifiers
- `System.Synergy` - Synergy tier calculations, bonuses
- `System.Capture` - Capture formula, item effectiveness, bind thresholds
- `System.Theme` - HeroThemeConfig fields, ScriptableObject creation
- `System.Animation` - PrimeTween integration

**Phase categories (project milestone gates):**
- `Phase.PreProd` - Must pass before pre-production milestone

**Example usage:**
```csharp
[Test]
[Category("Suite.Core")]
[Category("System.Brand")]
public void Iron_SuperEffective_Against_Surge_And_Dread() { ... }

[UnityTest]
[Category("Suite.Smoke")]
[Category("Phase.PreProd")]
public IEnumerator CharacterSelect_DisplaysHeroIdentityAndStats() { ... }
```

## Test Naming Convention

**Format:** `{SystemUnderTest}_{BehaviorDescription}`

**Examples from the codebase:**
```
Iron_SuperEffective_Against_Surge_And_Dread
Every_Core_Brand_Has_Exactly_Two_Strengths_And_Two_Weaknesses
Corruption_0_Is_Ascended
ApplyCorruptionModifier_Ascended_Increases_Stat
SuperEffective_Brand_Gives_2x_Multiplier
Null_Combatant_Returns_Fallback_Damage
Best_Case_Capture_Over_90_Percent
Combatant_Starts_Alive
Heal_Capped_At_Max_HP
PrimeTween_AssemblyResolves
MainMenu_OverlayVfxRendersAndDoesNotBlockInput
```

Use underscores to separate words (not camelCase). Be descriptive about the expected behavior.

## Mocking

**Framework:** None -- no mocking framework used

**Patterns:**
- Create real GameObjects for MonoBehaviour tests, destroy in TearDown:
  ```csharp
  // From Assets/Tests/EditMode/DamageCalculator_EditModeTests.cs
  [SetUp]
  public void SetUp()
  {
      _attackerGO = new GameObject("TestAttacker");
      _attacker = _attackerGO.AddComponent<Combatant>();
      _attacker.Initialize("test_attacker", "Test Attacker", Brand.IRON, ...);
  }

  [TearDown]
  public void TearDown()
  {
      if (_attackerGO != null) Object.DestroyImmediate(_attackerGO);
  }
  ```

- Create temporary GameObjects in individual tests with try/finally cleanup:
  ```csharp
  // From Assets/Tests/EditMode/DamageCalculator_EditModeTests.cs
  var surgeGO = new GameObject("SurgeAttacker");
  try
  {
      var surgeAttacker = surgeGO.AddComponent<Combatant>();
      // ... assertions ...
  }
  finally
  {
      Object.DestroyImmediate(surgeGO);
  }
  ```

- ScriptableObjects created with `ScriptableObject.CreateInstance<T>()`:
  ```csharp
  // From Assets/Tests/EditMode/HeroThemeConfig_EditModeTests.cs
  var config = ScriptableObject.CreateInstance<HeroThemeConfig>();
  Assert.IsNotNull(config);
  Object.DestroyImmediate(config);
  ```

**What to test directly (no mocks needed):**
- Static systems: `BrandSystem`, `CorruptionSystem`, `SynergySystem`, `DamageCalculator`
- Data structures: `CaptureItemConfig`, `CaptureFormulaCalculator`, `BindThresholdConfig`
- ScriptableObject field existence (via reflection)

**What needs real instances:**
- `Combatant` (MonoBehaviour -- requires GameObject)
- Scene-level tests (load real scenes in PlayMode)

## Fixtures and Factories

**Test Data:**
```csharp
// Inline struct creation for capture tests
// From Assets/Tests/EditMode/CaptureSystem_EditModeTests.cs
var monster = new BoundMonsterData
{
    boundAtHPPercent = 0.25f,
    currentCorruption = 35f,
    rarity = MonsterRarity.COMMON,
    monsterLevel = 10
};
```

**No shared fixtures directory.** Test data is created inline or via `[SetUp]` methods.

## Coverage

**Requirements:** None enforced -- no coverage tooling or thresholds configured

**Current coverage by system (based on test file analysis):**

| System | Test Count | Coverage |
|--------|-----------|----------|
| BrandSystem | ~25 tests | High - full matrix, hybrid brands, display names |
| CorruptionSystem | ~20 tests | High - all tiers, boundaries, edge cases, modifiers |
| SynergySystem | ~18 tests | High - all paths, tiers, bonuses, corruption rates |
| DamageCalculator | ~10 tests | Medium - brand mult, synergy, true damage, null safety |
| Combatant | ~14 tests | Medium - lifecycle, HP/MP, defend, revive |
| CaptureSystem | ~25 tests | High - items, formula, QTE, level, corruption, berserk |
| HeroThemeConfig | ~4 tests | Low - field existence only |
| Scene integrity | ~2 tests | Low - missing scripts, main menu loads |
| MainMenu assets | ~2 tests | Low - resource existence, shader existence |
| PrimeTween | ~2 tests | Low - assembly resolution only |
| CharacterSelect (PlayMode) | ~2 tests | Low - UI renders, model spawns |
| MainMenu overlay (PlayMode) | ~2 tests | Low - VFX renders, GC budget |

## Test Types

**Unit Tests (EditMode):**
- Pure logic tests requiring no scene or runtime
- Located in: `Assets/Tests/EditMode/`
- Test static systems directly
- Fast execution, no coroutines
- Examples: brand effectiveness, corruption tiers, damage formulas, capture rates

**Integration Tests (EditMode):**
- Verify cross-system contracts
- Located in: `Assets/Tests/EditMode/PrimeTween_Integration_EditModeTests.cs`
- Example: PrimeTween assembly resolves correctly

**Integrity Tests (EditMode):**
- Structural validation of project assets
- Located in: `Assets/Tests/EditMode/SceneIntegrity_EditModeTests.cs`, `MainMenuAssets_EditModeTests.cs`
- Examples: no missing scripts in scenes, required resources exist

**PlayMode Tests:**
- Require Unity runtime and real scenes
- Located in: `Assets/Tests/PlayMode/`
- Use `[UnityTest]` attribute, return `IEnumerator`
- Poll for conditions with frame-count timeout: `kMaxPollFrames = 600`
- Examples: character select UI populates, overlay VFX renders

**Performance Tests (PlayMode):**
- GC allocation budgets measured via `ProfilerRecorder`
- Only run in batch mode: `Assert.Ignore("GC allocation test only reliable in batchmode.")`
- Located in: `Assets/Tests/PlayMode/MainMenuOverlay_PlayModeTests.cs`
- Budget: `kEditorBatchGcBudget = 12288f` (12KB/frame)

## Common Patterns

**Design Doc Validation Pattern:**
Test that constants match the game design document:
```csharp
// From Assets/Tests/EditMode/CorruptionSystem_EditModeTests.cs
[Test]
public void Thresholds_Match_Design_Values()
{
    Assert.AreEqual(10f, CorruptionSystem.ASCENDED_THRESHOLD);
    Assert.AreEqual(25f, CorruptionSystem.PURIFIED_THRESHOLD);
    Assert.AreEqual(50f, CorruptionSystem.UNSTABLE_THRESHOLD);
    Assert.AreEqual(75f, CorruptionSystem.CORRUPTED_THRESHOLD);
}
```

**Matrix Completeness Pattern:**
Verify a system's invariants hold across all inputs:
```csharp
// From Assets/Tests/EditMode/BrandSystem_EditModeTests.cs
[Test]
public void Every_Core_Brand_Has_Exactly_Two_Strengths_And_Two_Weaknesses()
{
    Brand[] coreBrands = { Brand.IRON, Brand.SAVAGE, ... };
    foreach (var attacker in coreBrands)
    {
        int strengths = 0, weaknesses = 0, neutrals = 0;
        foreach (var defender in coreBrands)
        {
            float eff = BrandSystem.GetEffectiveness(attacker, defender);
            if (eff >= 2.0f) strengths++;
            else if (eff <= 0.5f) weaknesses++;
            else neutrals++;
        }
        Assert.AreEqual(2, strengths, $"{attacker} should have exactly 2 super-effective matchups");
        Assert.AreEqual(2, weaknesses, $"{attacker} should have exactly 2 not-effective matchups");
        Assert.AreEqual(6, neutrals, $"{attacker} should have exactly 6 neutral matchups");
    }
}
```

**Boundary Testing Pattern:**
Test exact tier boundaries:
```csharp
// From Assets/Tests/EditMode/CorruptionSystem_EditModeTests.cs
[Test] public void Corruption_10_Is_Ascended()
    => Assert.AreEqual(CorruptionState.ASCENDED, CorruptionSystem.GetCorruptionState(10f));
[Test] public void Corruption_11_Is_Purified()
    => Assert.AreEqual(CorruptionState.PURIFIED, CorruptionSystem.GetCorruptionState(10.01f));
```

**Ordering/Comparison Pattern:**
Verify relative ordering:
```csharp
// From Assets/Tests/EditMode/CaptureSystem_EditModeTests.cs
[Test]
public void Better_QTE_Gives_Higher_Chance()
{
    float miss = CaptureFormulaCalculator.CalculateQuick(monster, item, 10, QTEResult.MISS);
    float okay = CaptureFormulaCalculator.CalculateQuick(monster, item, 10, QTEResult.OKAY);
    float good = CaptureFormulaCalculator.CalculateQuick(monster, item, 10, QTEResult.GOOD);
    float perfect = CaptureFormulaCalculator.CalculateQuick(monster, item, 10, QTEResult.PERFECT);

    Assert.Greater(okay, miss, "Okay > Miss");
    Assert.Greater(good, okay, "Good > Okay");
    Assert.Greater(perfect, good, "Perfect > Good");
}
```

**Null Safety Pattern:**
Test graceful handling of null inputs:
```csharp
// From Assets/Tests/EditMode/DamageCalculator_EditModeTests.cs
[Test]
public void Null_Combatant_Returns_Fallback_Damage()
{
    var result = DamageCalculator.Calculate(null, _defender, 50, DamageType.PHYSICAL);
    Assert.Greater(result.finalDamage, 0);
}

// From Assets/Tests/EditMode/CaptureSystem_EditModeTests.cs
[Test]
public void Null_Monster_Returns_Zero_Chance()
{
    var result = CaptureFormulaCalculator.Calculate(null, CaptureItem.VEIL_CRYSTAL, 10, QTEResult.GOOD);
    Assert.AreEqual(0f, result.finalChance);
}
```

**PlayMode Polling Pattern:**
Wait for async scene content with frame-count timeout:
```csharp
// From Assets/Tests/PlayMode/CharacterSelect_PlayModeTests.cs
private const int kMaxPollFrames = 600; // ~10 seconds at 60fps

UIDocument doc = null;
for (int i = 0; i < kMaxPollFrames; i++)
{
    doc = Object.FindFirstObjectByType<UIDocument>();
    if (doc != null && doc.rootVisualElement != null) break;
    yield return null;
}
Assert.NotNull(doc, "UIDocument not found in CharacterSelect.");
```

**Reflection-based Field Existence Pattern:**
Validate ScriptableObject schema:
```csharp
// From Assets/Tests/EditMode/HeroThemeConfig_EditModeTests.cs
[Test]
public void HeroThemeConfig_HasRequiredColorFields()
{
    var config = ScriptableObject.CreateInstance<HeroThemeConfig>();
    Assert.IsNotNull(config.GetType().GetField("primaryColor"));
    Assert.IsNotNull(config.GetType().GetField("glowColor"));
    Assert.IsNotNull(config.GetType().GetField("darkColor"));
    Object.DestroyImmediate(config);
}
```

**Runtime-safe Type Lookup (avoids compile-time assembly dependency):**
```csharp
// From Assets/Tests/PlayMode/MainMenuOverlay_PlayModeTests.cs
private static Type FindType(string fullName)
{
    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
    {
        var t = asm.GetType(fullName, throwOnError: false);
        if (t != null) return t;
    }
    return null;
}
```

## Writing New Tests

**For a new game system (EditMode):**
1. Create `Assets/Tests/EditMode/{SystemName}_EditModeTests.cs`
2. Use namespace `VeilBreakers.Tests.EditMode`
3. Add `[Category("Suite.Core")]` and `[Category("System.{Name}")]` on every `[Test]`
4. Use `[SetUp]`/`[TearDown]` for GameObject lifecycle
5. Group tests with `====` comment banners
6. Test: design doc constants, boundary values, null safety, matrix completeness

**For a new scene/UI feature (PlayMode):**
1. Create `Assets/Tests/PlayMode/{Feature}_PlayModeTests.cs`
2. Use namespace `VeilBreakers.Tests.PlayMode`
3. Use `[UnityTest]` returning `IEnumerator`
4. Add `[Category("Suite.Smoke")]` and `[Category("Phase.PreProd")]`
5. Load scene via `SceneManager.LoadSceneAsync()`
6. Poll for conditions with `kMaxPollFrames` timeout
7. Use `[UnityTearDown]` for cleanup

**For a performance gate (PlayMode):**
1. Add to existing PlayMode test file or create new one
2. Use `[Category("Suite.Perf")]`
3. Guard with `Assert.Ignore()` for non-batchmode
4. Use `ProfilerRecorder` for measurement
5. Set explicit budget thresholds as `const` fields

## Legacy Test System

**The `Assets/Scripts/Test/` directory contains MonoBehaviour-based tests** that predate the NUnit migration. These use a custom assertion framework with pass/fail counters and are triggered via Inspector `[ContextMenu]` or `_runOnStart` flags. When adding new tests, use the NUnit pattern in `Assets/Tests/` instead.

Legacy files (for reference, not for new test authoring):
- `Assets/Scripts/Test/CaptureTests.cs`
- `Assets/Scripts/Test/CombatTestSetup.cs`
- `Assets/Scripts/Test/CombatUITests.cs`
- `Assets/Scripts/Test/GambitTests.cs`
- `Assets/Scripts/Test/QuickCommandTests.cs`
- `Assets/Scripts/Test/SaveSystemTests.cs`
- `Assets/Scripts/Test/StatusEffectTests.cs`

---

*Testing analysis: 2026-03-30*
