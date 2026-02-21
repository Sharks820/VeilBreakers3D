# Testing Patterns

**Analysis Date:** 2026-02-21

## Test Framework

**Runner:**
- Custom MonoBehaviour-based test harness (NOT NUnit, NOT Unity Test Framework)
- Tests are MonoBehaviour scripts attached to GameObjects in test scenes or triggered via Inspector
- No `Assets/Tests/` assembly definition -- tests live alongside production code in `Assets/Scripts/Test/`

**Assertion Library:**
- Custom per-file assertion helpers (no shared assertion base class)
- Two assertion patterns coexist:
  1. **Throw-on-failure:** `Assert(bool, string)` throws `Exception` on failure (caught by test runner)
  2. **Log-and-count:** `AssertTrue(bool, string)` / `AssertFalse(bool, string)` logs pass/fail and increments counters

**Run Commands:**
```bash
# No CLI test runner -- tests run inside Unity Editor
# Option 1: Set _runOnStart = true in Inspector, enter Play Mode
# Option 2: Right-click component → Context Menu → "Run All Tests"
# Option 3: Call RunAllTests() from another script
```

## Test File Organization

**Location:**
- Main test directory: `Assets/Scripts/Test/`
- Domain-specific tests live with their domain: `Assets/Scripts/Audio/AudioTests.cs`

**Naming:**
- Suffix `Tests`: `CaptureTests.cs`, `GambitTests.cs`, `SaveSystemTests.cs`
- Exception: `CombatTestSetup.cs` (setup + tests combined)

**Files:**
```
Assets/Scripts/Test/
├── CaptureTests.cs          # Monster capture system tests (689 lines)
├── CombatTestSetup.cs       # Brand, synergy, damage, combatant tests (411 lines)
├── CombatUITests.cs         # Visual UI validation tests (247 lines)
├── GambitTests.cs           # AI gambit system tests (687 lines)
├── QuickCommandTests.cs     # Command system tests (620 lines)
├── SaveSystemTests.cs       # Save/load cycle tests (527 lines)
└── StatusEffectTests.cs     # Status effect system tests (677 lines)

Assets/Scripts/Audio/
└── AudioTests.cs            # Audio system tests (464 lines)
```

## Test Structure

**Suite Organization (Synchronous Pattern):**
```csharp
// From Assets/Scripts/Test/CaptureTests.cs
namespace VeilBreakers.Test
{
    public class CaptureTests : MonoBehaviour
    {
        [SerializeField] private bool _runOnStart = true;

        private int _passed;
        private int _failed;
        private List<string> _failures = new();

        private void Start()
        {
            if (_runOnStart)
                StartCoroutine(RunTestsDelayed());
        }

        [ContextMenu("Run All Tests")]
        public void RunAllTests()
        {
            _passed = 0; _failed = 0; _failures.Clear();
            Debug.Log("=== TEST SUITE STARTING ===");

            Test_SpecificBehavior();
            Test_AnotherBehavior();
            // ... more tests

            Debug.Log($"Passed: {_passed} | Failed: {_failed}");
        }

        private IEnumerator RunTestsDelayed()
        {
            yield return new WaitForSeconds(0.1f); // Let managers initialize
            RunAllTests();
        }
    }
}
```

**Suite Organization (Async Pattern):**
```csharp
// From Assets/Scripts/Test/SaveSystemTests.cs
namespace VeilBreakers.Test
{
    public class SaveSystemTests : MonoBehaviour
    {
        [SerializeField] private bool _runOnStart = false;
        [SerializeField] private bool _cleanupAfterTests = true;
        [SerializeField] private int _testsPassed;
        [SerializeField] private int _testsFailed;
        [SerializeField] private List<string> _failedTests = new List<string>();

        private async void Start()
        {
            if (_runOnStart)
            {
                await Task.Delay(500); // Wait for managers
                await RunAllTestsAsync();
            }
        }

        public async Task RunAllTestsAsync()
        {
            _testsPassed = 0; _testsFailed = 0; _failedTests.Clear();
            Debug.Log("=== SAVE SYSTEM TESTS STARTING ===");

            await Test_SaveData_CreateNew();
            await Test_SaveManager_Load();
            // ... more tests

            Debug.Log($"Passed: {_testsPassed} | Failed: {_testsFailed}");
            if (_cleanupAfterTests) CleanupTestFiles();
        }
    }
}
```

**Individual Test Method Pattern (Throw-on-failure):**
```csharp
// From Assets/Scripts/Test/SaveSystemTests.cs
private async Task Test_SaveData_CreateNew()
{
    string testName = "SaveData.CreateNew";
    try
    {
        var data = SaveData.CreateNew("vex", "TestHero", GamePath.IRONBOUND);

        Assert(data != null, "Data should not be null");
        Assert(data.version == SaveVersion.CURRENT, "Version should match current");
        Assert(data.heroId == "vex", "HeroId should match");

        Pass(testName);
    }
    catch (Exception ex)
    {
        Fail(testName, ex.Message);
    }
}
```

**Individual Test Method Pattern (Log-and-count):**
```csharp
// From Assets/Scripts/Test/CombatTestSetup.cs
private void Test_BrandEffectiveness()
{
    // Super effective
    float ironVsSurge = BrandSystem.GetEffectiveness(Brand.IRON, Brand.SURGE);
    AssertEqual(ironVsSurge, 2.0f, "IRON vs SURGE should be 2x");

    // Not effective
    float ironVsSavage = BrandSystem.GetEffectiveness(Brand.IRON, Brand.SAVAGE);
    AssertEqual(ironVsSavage, 0.5f, "IRON vs SAVAGE should be 0.5x");
}
```

**Startup Patterns:**
- Coroutine delay: `yield return new WaitForSeconds(0.1f);` for sync tests
- Task delay: `await Task.Delay(500);` for async tests
- Both ensure singleton managers are initialized before tests run

## Mocking

**Framework:** None -- no mocking framework used

**Patterns:**
- Tests create real GameObjects and components, then destroy them:
  ```csharp
  // From Assets/Scripts/Test/CaptureTests.cs
  private GameObject CreateTestCombatant(string id, Brand brand, int level)
  {
      var go = new GameObject($"TestCombatant_{id}");
      var combatant = go.AddComponent<Combatant>();
      // ... configure ...
      return go;
  }
  ```
- Cleanup via `DestroyImmediate()` in `finally` blocks:
  ```csharp
  GameObject testObj = null;
  try
  {
      testObj = CreateTestCombatant("test", Brand.IRON, 10);
      // ... assertions ...
  }
  finally
  {
      if (testObj != null) DestroyImmediate(testObj);
  }
  ```
- ScriptableObjects created with `ScriptableObject.CreateInstance<T>()`:
  ```csharp
  // From Assets/Scripts/Test/StatusEffectTests.cs
  var effectData = ScriptableObject.CreateInstance<StatusEffectData>();
  effectData.effectName = "Test Poison";
  effectData.effectType = StatusEffectType.POISON;
  // ... test ... then DestroyImmediate(effectData);
  ```

**What to Mock (by creating real instances):**
- GameObjects and MonoBehaviour components
- ScriptableObject data definitions
- Singleton managers (create if not present)

**What NOT to Mock:**
- Static systems (BrandSystem, EventBus) -- test directly
- Math/utility functions -- test directly
- File I/O in save tests -- uses real save files with cleanup

## Fixtures and Factories

**Test Data:**
```csharp
// Factory pattern from Assets/Scripts/Test/CaptureTests.cs
private GameObject CreateTestCombatant(string id, Brand brand, int level)
{
    var go = new GameObject($"TestCombatant_{id}");
    var combatant = go.AddComponent<Combatant>();
    // Configure combatant with test data
    return go;
}

// Inline creation from Assets/Scripts/Test/SaveSystemTests.cs
var data = SaveData.CreateNew("vex", "TestHero", GamePath.IRONBOUND);
data.heroLevel = 42;
data.currency = 9999;
data.party.Add(SavedMonster.Create("monster_hollow", 15, 25.5f));
```

**Location:**
- No shared fixtures directory
- Factory methods are private within each test class
- Test data created inline or via domain constructors (`SaveData.CreateNew()`, `SavedMonster.Create()`)

**Singleton Setup in Tests:**
```csharp
// Ensure manager exists for tests (from Assets/Scripts/Test/SaveSystemTests.cs)
if (SaveManager.Instance == null)
{
    var go = new GameObject("SaveManager");
    go.AddComponent<SaveManager>();
    await Task.Delay(100); // Let it initialize
}
```

## Coverage

**Requirements:** None enforced -- no coverage tooling configured

**View Coverage:**
```bash
# No coverage tooling available
# Manual inspection required
```

## Test Types

**Unit Tests:**
- Pure logic tests (brand effectiveness, damage formulas, synergy calculations)
- Located in: `Assets/Scripts/Test/CombatTestSetup.cs`, `Assets/Scripts/Test/GambitTests.cs`
- Test static systems directly: `BrandSystem.GetEffectiveness()`, `DamageCalculator.Calculate()`

**Integration Tests:**
- Multi-system tests (save create → modify → save → load → verify)
- Located in: `Assets/Scripts/Test/SaveSystemTests.cs` (`Test_FullSaveCycle`)
- Test singleton manager interactions: SaveManager + GameDatabase + ShrineManager

**Runtime Tests:**
- Tests that verify behavior in Play Mode with real components
- Located in: `Assets/Scripts/Test/CaptureTests.cs`, `Assets/Scripts/Test/StatusEffectTests.cs`
- Create real GameObjects, apply effects, verify state changes

**Visual Validation Tests:**
- UI appearance/behavior tests (not automated pass/fail)
- Located in: `Assets/Scripts/Test/CombatUITests.cs`
- Triggered via `[ContextMenu]`, visually inspected by developer

**E2E Tests:**
- Not used -- no automated end-to-end framework

## Common Patterns

**Assertion Helpers (Throw Pattern):**
```csharp
// From Assets/Scripts/Test/SaveSystemTests.cs
private void Assert(bool condition, string message)
{
    if (!condition)
        throw new Exception($"Assertion failed: {message}");
}

private void Pass(string testName)
{
    _testsPassed++;
    Debug.Log($"<color=green>[PASS]</color> {testName}");
}

private void Fail(string testName, string error)
{
    _testsFailed++;
    _failedTests.Add($"{testName}: {error}");
    Debug.LogError($"<color=red>[FAIL]</color> {testName}: {error}");
}
```

**Assertion Helpers (Count Pattern):**
```csharp
// From Assets/Scripts/Test/CaptureTests.cs
private void Assert(bool condition, string message, string testName)
{
    if (condition) { _passed++; Debug.Log($"  [PASS] {message}"); }
    else { _failed++; _failures.Add($"{testName}: {message}"); Debug.LogError($"  [FAIL] {message}"); }
}

// From Assets/Scripts/Test/GambitTests.cs (generic equality)
private void AssertEqual<T>(T actual, T expected, string message)
{
    bool pass = EqualityComparer<T>.Default.Equals(actual, expected);
    Assert(pass, pass ? message : $"{message} (expected={expected}, actual={actual})", "");
}

// Approximate float comparison
private void AssertApprox(float actual, float expected, float tolerance, string message)
{
    bool pass = Mathf.Abs(actual - expected) <= tolerance;
    Assert(pass, pass ? message : $"{message} (expected~{expected}, actual={actual}, tol={tolerance})", "");
}
```

**Async Testing:**
```csharp
// From Assets/Scripts/Test/SaveSystemTests.cs
private async Task Test_SaveManager_CreateAndSave()
{
    string testName = "SaveManager.CreateAndSave";
    try
    {
        bool success = await SaveManager.Instance.CreateNewSaveAsync(
            TEST_SLOT, "vex", "SaveManagerTest", GamePath.IRONBOUND
        );
        Assert(success, "Create and save should succeed");
        Assert(SaveManager.Instance.HasActiveSave, "Should have active save");
        Pass(testName);
    }
    catch (Exception ex)
    {
        Fail(testName, ex.Message);
    }
}
```

**Error Testing:**
```csharp
// From Assets/Scripts/Test/SaveSystemTests.cs - Corruption detection
private async Task Test_SaveFileHandler_Checksum()
{
    string testName = "SaveFileHandler.Checksum";
    try
    {
        var data = SaveData.CreateNew("nyx", "ChecksumTest", GamePath.UNCHAINED);
        byte[] bytes = SaveFileHandler.SerializeToBytes(data);

        // Corrupt the data
        byte[] corrupted = (byte[])bytes.Clone();
        corrupted[50] = (byte)(corrupted[50] ^ 0xFF);

        // Corrupted data should fail
        var corruptedResult = SaveFileHandler.DeserializeFromBytes(corrupted);
        Assert(corruptedResult == null, "Corrupted data should fail checksum");

        Pass(testName);
    }
    catch (Exception ex)
    {
        Fail(testName, ex.Message);
    }
}
```

**Cleanup Pattern:**
```csharp
// Per-object cleanup (from CaptureTests)
finally
{
    if (testObj != null) DestroyImmediate(testObj);
}

// Suite-level cleanup (from SaveSystemTests)
private void CleanupTestFiles()
{
    try
    {
        SaveManager.Instance?.DeleteSlot(TEST_SLOT);
        Debug.Log("[Test] Cleanup complete");
    }
    catch (Exception ex)
    {
        Debug.LogWarning($"[Test] Cleanup failed: {ex.Message}");
    }
}
```

## Writing New Tests

**Follow this pattern when adding tests:**

1. Create a MonoBehaviour in `Assets/Scripts/Test/` with `[Namespace].Test` namespace
2. Add `[SerializeField] private bool _runOnStart` and pass/fail counters
3. Add `[ContextMenu("Run All Tests")]` on the public entry point
4. Use try/catch per test method with `Pass()`/`Fail()` reporting
5. For async operations, use `async Task` methods with the throw-on-failure Assert pattern
6. Create real GameObjects/ScriptableObjects; destroy them in `finally` blocks
7. Ensure singletons exist before testing (create if needed)
8. Log a summary: `Debug.Log($"Passed: {_passed} | Failed: {_failed}");`
9. Add `_cleanupAfterTests` flag if tests create persistent state (files, saves)

**Test naming:** `Test_SystemName_BehaviorUnderTest` (e.g., `Test_SaveData_CreateNew`, `Test_BrandEffectiveness`)

---

*Testing analysis: 2026-02-21*
