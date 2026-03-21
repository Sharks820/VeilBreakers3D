# VB-Toolkit Analysis, Scan Results & Fix Prompt

**Date:** 2026-03-22
**Author:** Claude Opus 4.6
**Purpose:** Complete analysis for another Claude Code agent to fix/improve the VB-Toolkit

---

## Part 1: Scan Results (What We Actually Found)

### 1.1 `unity_qa analyze_code` — 16 Files Scanned

The tool performs Python-side regex static analysis checking 5 patterns:
1. `GameObject.Find()` / `FindObjectOfType()` in Update
2. `Camera.main` in Update
3. LINQ (`System.Linq`) in Update
4. `new` allocations in Update
5. `GetComponent` in Update

**Files scanned:**
| File | Findings |
|------|----------|
| BattleManager.cs | 0 |
| Combatant.cs | 0 |
| DamageCalculator.cs | 0 |
| GameManager.cs | 0 |
| GameDatabase.cs | 0 |
| EventBus.cs | 0 |
| BrandSystem.cs | 0 |
| SynergySystem.cs | 0 |
| VERASystem.cs | 0 |
| CaptureManager.cs | 0 |
| QTEController.cs | 0 |
| GambitController.cs | 0 |
| GambitEvaluator.cs | 0 |
| TitleScreenVFX.cs | 0 |
| AudioManager.cs | 0 |
| SaveData.cs | 0 |

**Result: 0 findings.** The codebase is already clean of these 5 patterns. Verified the tool works by feeding it a deliberately bad file — it correctly caught all 5 anti-patterns with line numbers and fix suggestions.

**Verdict: Tool works, but too shallow.** These 5 patterns are the bare minimum. The actual bugs we found and fixed this session (synergy math errors, cross-layer dependencies, deprecated API usage, event subscriber leaks, breathing animation stacking, closure allocations) are ALL invisible to this tool.

### 1.2 `unity_quality aaa_audit`

**What happened:** Generated `Assets/Editor/Generated/Quality/AAAQualityAudit.cs` — a C# editor script.
**What it would check:** Polygon budgets per asset type, texture quality (texel density, normal maps, channel packing), material standards.
**Result:** Script generated successfully but **cannot execute without Unity Editor open.**
**Generated script location:** `Assets/Editor/Generated/Quality/AAAQualityAudit.cs`
**Unity menu path:** VeilBreakers > Quality > Full AAA Audit

### 1.3 `unity_performance audit_assets`

**What happened:** Generated `Assets/Editor/Generated/Performance/VeilBreakers_AssetAudit.cs`
**What it would check:** Oversized textures (>2048px), uncompressed audio, unused assets.
**Result:** Script generated, **cannot execute without Unity.**
**Unity menu path:** VeilBreakers > Performance > Audit Assets

### 1.4 `unity_prefab validate_project`

**What happened:** Generated `Assets/Editor/Generated/Prefab/VeilBreakers_ValidateProject.cs`
**What it would check:** Missing references, broken prefab connections, orphaned assets.
**Result:** Script generated, **cannot execute without Unity.**

### 1.5 `unity_ui validate_layout` — 6 UXML Files Checked

**Result:** 5 passed, 1 failed.
**Failure:** `MonsterCollection.uxml` has 2 issues:
- `corruption-bar-fill` has zero width (never gets sized)
- Height overflow in corruption bar container

**This is a legitimate bug** that should be fixed in the UXML.

### 1.6 `unity_ui check_contrast` — CharacterSelect USS

**Result:** 49/56 elements pass WCAG AA, 7 fail.
**Assessment:** The 7 failures are **likely false positives** — the tool defaults to white background when it cannot resolve inherited dark backgrounds. Our UI uses dark fantasy theming (near-black backgrounds), so light-on-dark text that looks fine visually gets flagged as "insufficient contrast against white."

### 1.7 Codex CLI Review (External)

Codex reviewed the 5 commits from this session:
- `d886b2c` (Tier 2-4 fixes): Not reviewed in detail
- `6754590` (HIGH fixes): **FAIL** — flagged BrandSystem color palette drift (fixed), breathing Pause() vs unschedule concern (acceptable)
- `49535f2` (MEDIUM/LOW): **PASS** — deprecated API migration correct
- `249cf3f` (Tier 5 cleanup): Not reviewed (data-only)
- `a6cb510` (Toolkit report): Not reviewed (docs-only)

---

## Part 2: Gap Analysis — What the Tools Miss

### 2.1 `analyze_code` Gaps (CRITICAL)

The tool checks 5 regex patterns. Here's what it SHOULD check but doesn't:

| Missing Check | Severity | Example from our codebase |
|---------------|----------|--------------------------|
| **Cross-layer dependencies** | HIGH | `BrandSystem` (Systems layer) imported `ThemeManager` (UI.Core layer) — breaks assembly separation |
| **Event subscriber leaks** | HIGH | `GeometryChangedEvent` not unregistered in OnDisable across 6 VFX files |
| **Deprecated API usage** | HIGH | 19 uses of `unityBackgroundScaleMode` (deprecated in Unity 6) |
| **Synergy/balance math errors** | HIGH | `damage /= 1.08` instead of `damage *= 0.92` — mathematically different results |
| **Singleton access without HasInstance** | MEDIUM | `BattleManager.Instance?.CurrentTarget` without checking `HasInstance` first |
| **Closure allocations in PrimeTween** | MEDIUM | `Tween.Custom(... val => ...)` captures outer variables, allocates delegate |
| **Dead code / unused fields** | MEDIUM | `_soundCallbacks` field in MainMenuBootstrap never populated |
| **Missing null guards on SO access** | MEDIUM | `Resources.Load` without "tried and failed" guard causes repeated load attempts |
| **God class detection** | LOW | TitleScreenVFX.cs is 2800+ lines — should be split |
| **Exception type misuse** | LOW | `InvalidDataException` (System.IO) used for data validation instead of `FormatException` |
| **String allocations in hot paths** | LOW | `$"FPS: {fpsInt}"` on every FPS change (was optimized with cache) |
| **WaitForSeconds not cached** | LOW | `new WaitForSecondsRealtime(0.5f)` allocated per coroutine start |

### 2.2 Unity-Dependent Tools (Can't Run Autonomously)

These tools generate C# editor scripts but can't execute them:

| Tool | Action | What it generates | Why it needs Unity |
|------|--------|-------------------|-------------------|
| `unity_quality` | `aaa_audit` | Poly/texture/material validator | Uses `AssetDatabase`, `MeshFilter`, `TextureImporter` |
| `unity_performance` | `profile_scene` | Frame time/draw call profiler | Uses `ProfilerRecorder` (runtime only) |
| `unity_performance` | `audit_assets` | Oversized texture/audio finder | Uses `AssetDatabase.FindAssets`, `TextureImporter` |
| `unity_qa` | `run_tests` | Test runner invocation | Uses `TestRunnerApi` |
| `unity_qa` | `profile_scene` | CPU/GPU profiler | Uses `ProfilerRecorder` |
| `unity_qa` | `detect_memory_leaks` | Memory growth tracker | Uses `Profiler.GetTotalAllocatedMemoryLong` |
| `unity_qa` | `check_compile_status` | Compile error check | Queries Unity bridge TCP socket |
| `unity_editor` | All 7 actions | Editor automation | Needs TCP bridge running inside Unity |
| `unity_prefab` | `validate_project` | Reference integrity check | Uses `AssetDatabase`, `PrefabUtility` |

---

## Part 3: How to Make Unity-Dependent Tools Work Autonomously

### The Core Problem

Claude Code runs as a CLI process. Unity Editor is a GUI application with its own C# runtime. There's no built-in way for Claude Code to execute C# inside Unity.

### Solution Architecture: 3-Tier Approach

#### Tier 1: Unity Batch Mode (Best for CI/CD and autonomous runs)

Unity can run headlessly from the command line:

```bash
# Execute a static method in batch mode (no GUI)
"C:\Program Files\Unity\Hub\Editor\6000.0.38f1\Editor\Unity.exe" \
  -batchmode \
  -projectPath "C:\Users\Conner\OneDrive\Documents\VeilBreakers3DCurrent" \
  -executeMethod VeilBreakers.Editor.ToolkitRunner.RunAudit \
  -logFile Temp/unity_batch.log \
  -quit
```

**Implementation needed:**

1. Create a dispatcher class that the VB-Toolkit tools can target:

```csharp
// Assets/Editor/VBToolkitRunner.cs
using UnityEditor;
using UnityEngine;
using System.IO;

namespace VeilBreakers.Editor
{
    public static class VBToolkitRunner
    {
        // Called via: -executeMethod VeilBreakers.Editor.VBToolkitRunner.RunFromCommandLine
        public static void RunFromCommandLine()
        {
            // Read command from file (Claude writes this before launching Unity)
            string commandPath = Path.Combine(Application.dataPath, "..", "Temp", "vb_command.json");
            if (!File.Exists(commandPath))
            {
                Debug.LogError("[VBToolkitRunner] No command file found");
                EditorApplication.Exit(1);
                return;
            }

            string commandJson = File.ReadAllText(commandPath);
            var command = JsonUtility.FromJson<VBCommand>(commandJson);

            // Dispatch to appropriate handler
            string resultJson = command.action switch
            {
                "audit_assets" => RunAssetAudit(),
                "aaa_audit" => RunAAAQualityAudit(),
                "validate_project" => RunProjectValidation(),
                "check_compile" => RunCompileCheck(),
                "profile_scene" => RunSceneProfile(command.sceneName),
                _ => "{\"error\": \"Unknown action\"}"
            };

            // Write results for Claude to read
            string resultPath = Path.Combine(Application.dataPath, "..", "Temp", "vb_result.json");
            File.WriteAllText(resultPath, resultJson);

            EditorApplication.Exit(0);
        }

        private static string RunCompileCheck()
        {
            // Check if there are any compile errors
            // UnityEditor.Compilation.CompilationPipeline has this info
            var assemblyErrors = UnityEditor.Compilation.CompilationPipeline
                .GetPrecompiledAssemblyPaths(UnityEditor.Compilation.CompilationPipeline.PrecompiledAssemblySources.All);

            // Use EditorUtility to check for script errors
            bool hasErrors = EditorUtility.scriptCompilationFailed;
            return JsonUtility.ToJson(new CompileResult { hasErrors = hasErrors });
        }

        // ... other dispatch methods that wrap the generated toolkit scripts
    }

    [System.Serializable]
    public class VBCommand
    {
        public string action;
        public string sceneName;
        public string targetPath;
    }

    [System.Serializable]
    public class CompileResult
    {
        public bool hasErrors;
        public string[] errors;
    }
}
```

2. Modify the VB-Toolkit MCP server to:
   - Write `Temp/vb_command.json` with the action + params
   - Launch Unity in batch mode with `-executeMethod`
   - Wait for Unity to exit (timeout: 120s)
   - Read `Temp/vb_result.json` for results
   - Return results to Claude

3. Add a helper script Claude Code can call directly:

```bash
# vb-unity-batch.sh — wrapper for Claude Code to call
#!/bin/bash
UNITY_PATH="C:\Program Files\Unity\Hub\Editor\6000.0.38f1\Editor\Unity.exe"
PROJECT_PATH="C:\Users\Conner\OneDrive\Documents\VeilBreakers3DCurrent"

# Write command
echo "$1" > "$PROJECT_PATH/Temp/vb_command.json"

# Run Unity batch mode
"$UNITY_PATH" -batchmode -projectPath "$PROJECT_PATH" \
  -executeMethod VeilBreakers.Editor.VBToolkitRunner.RunFromCommandLine \
  -logFile "$PROJECT_PATH/Temp/unity_batch.log" -quit

# Return results
cat "$PROJECT_PATH/Temp/vb_result.json"
```

**Pros:** Fully autonomous, no GUI needed, works in CI/CD
**Cons:** Cold start takes 30-60s per invocation, must close Unity Editor first (can't have two instances on same project)

#### Tier 2: File-Watch Bridge (Best for interactive development)

Create a Unity Editor script that watches a file for commands:

```csharp
// Assets/Editor/VBToolkitBridge.cs
using UnityEditor;
using UnityEngine;
using System.IO;

[InitializeOnLoad]
public static class VBToolkitBridge
{
    private static FileSystemWatcher _watcher;
    private static readonly string kCommandPath = Path.Combine(
        Application.dataPath, "..", "Temp", "vb_command.json");
    private static readonly string kResultPath = Path.Combine(
        Application.dataPath, "..", "Temp", "vb_result.json");

    static VBToolkitBridge()
    {
        string dir = Path.GetDirectoryName(kCommandPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        _watcher = new FileSystemWatcher(dir, "vb_command.json");
        _watcher.Changed += OnCommandFileChanged;
        _watcher.EnableRaisingEvents = true;

        Debug.Log("[VBToolkitBridge] Watching for commands...");
    }

    private static void OnCommandFileChanged(object sender, FileSystemEventArgs e)
    {
        // Must dispatch to main thread
        EditorApplication.delayCall += () =>
        {
            try
            {
                string json = File.ReadAllText(kCommandPath);
                string result = VBToolkitRunner.ExecuteCommand(json);
                File.WriteAllText(kResultPath, result);
            }
            catch (System.Exception ex)
            {
                File.WriteAllText(kResultPath,
                    JsonUtility.ToJson(new { error = ex.Message }));
            }
        };
    }
}
```

**Claude Code workflow:**
1. Write JSON command to `Temp/vb_command.json`
2. Poll `Temp/vb_result.json` for response (or use FileSystemWatcher)
3. Read result JSON

**Pros:** Fast (no cold start), works while Unity is open, real-time
**Cons:** Requires Unity Editor to be running

#### Tier 3: Wrap Generated Scripts as NUnit Tests (Best for CI/CD + GitHub Actions)

Convert the audit scripts to NUnit EditMode tests so they run via Unity's standard test runner:

```csharp
// Assets/Tests/Editor/VBToolkitTests.cs
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace VeilBreakers.Tests.Editor
{
    [TestFixture]
    public class VBToolkitTests
    {
        [Test]
        public void AssetAudit_NoOversizedTextures()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                var settings = importer.GetDefaultPlatformTextureSettings();
                Assert.LessOrEqual(settings.maxTextureSize, 4096,
                    $"Texture too large: {path} ({settings.maxTextureSize}px)");
            }
        }

        [Test]
        public void CompileCheck_NoErrors()
        {
            Assert.IsFalse(EditorUtility.scriptCompilationFailed,
                "Project has compile errors");
        }

        [Test]
        public void PolyBudget_HeroesUnder30K()
        {
            var heroGuids = AssetDatabase.FindAssets("t:GameObject",
                new[] { "Assets/Art/Heroes" });
            foreach (var guid in heroGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                int totalVerts = 0;
                foreach (var mf in prefab.GetComponentsInChildren<MeshFilter>())
                {
                    if (mf.sharedMesh != null)
                        totalVerts += mf.sharedMesh.vertexCount;
                }

                Assert.LessOrEqual(totalVerts, 30000,
                    $"Hero {path} exceeds 30K vertex budget ({totalVerts})");
            }
        }
    }
}
```

Then run from CLI:
```bash
Unity.exe -batchmode -runTests -testPlatform EditMode \
  -testResults Temp/test_results.xml -projectPath .
```

Claude Code reads the XML results.

**Pros:** Standard Unity testing infrastructure, works in GitHub Actions, composable
**Cons:** Must restructure existing audit scripts as test assertions

### Recommended Implementation Order

1. **Immediate (no Unity changes needed):** Enhance `analyze_code` with 12+ new regex patterns from our gap analysis above
2. **Short-term:** Create `VBToolkitRunner.cs` + batch mode wrapper — gives Claude Code compile checking and asset auditing
3. **Medium-term:** Add `VBToolkitBridge.cs` file-watch for interactive sessions
4. **Long-term:** Convert all audits to NUnit tests for CI/CD pipeline

---

## Part 4: Specific Fix Recommendations for the VB-Toolkit

### Fix 1: Enhance `analyze_code` (Python-side, no Unity needed)

Add these regex patterns to the QA analyzer:

```python
# New patterns to add to qa_templates.py analyze_code
NEW_PATTERNS = [
    # Cross-layer dependency detection
    {
        "name": "cross_layer_import",
        "pattern": r"using VeilBreakers\.UI.*;\s*\n.*namespace VeilBreakers\.(Systems|Combat|Core|Data)",
        "severity": "warning",
        "message": "Systems/Core/Data layer imports UI layer — cross-layer dependency",
        "fix": "Move shared types to Core or create an interface"
    },
    # Event subscriber without unsubscribe
    {
        "name": "event_subscribe_without_unsubscribe",
        "pattern": r"\.RegisterCallback<(\w+)>",
        "severity": "info",
        "message": "RegisterCallback found — verify matching UnregisterCallback in OnDisable/OnDestroy",
        "fix": "Add corresponding UnregisterCallback in cleanup"
    },
    # Deprecated Unity APIs
    {
        "name": "deprecated_unity_api",
        "pattern": r"unityBackgroundScaleMode|OnPostRender|OnPreRender",
        "severity": "warning",
        "message": "Deprecated Unity API usage",
        "fix": "Use backgroundSize for scale mode; use RenderPipelineManager callbacks"
    },
    # Singleton access without HasInstance
    {
        "name": "singleton_without_hasinstance",
        "pattern": r"(\w+)\.Instance\??\.",
        "severity": "info",
        "message": "Singleton access — consider using HasInstance guard first",
        "fix": "if (TypeName.HasInstance) { var inst = TypeName.Instance; ... }"
    },
    # God class (file too long)
    {
        "name": "god_class",
        "check_type": "line_count",
        "threshold": 500,
        "severity": "warning",
        "message": "File exceeds 500 lines — consider splitting into smaller classes",
        "fix": "Extract logical sections into separate classes"
    },
    # Resources.Load without caching
    {
        "name": "resources_load_uncached",
        "pattern": r"Resources\.Load[<\(]",
        "severity": "warning",
        "message": "Resources.Load call — ensure result is cached and not called repeatedly",
        "fix": "Cache the loaded resource in a field; add a 'tried' guard"
    },
    # new WaitForSeconds not cached
    {
        "name": "wait_allocation",
        "pattern": r"new WaitForSeconds(Realtime)?\(",
        "severity": "info",
        "message": "WaitForSeconds allocation — cache as static readonly if used repeatedly",
        "fix": "private static readonly WaitForSeconds kWait = new WaitForSeconds(N);"
    },
    # String interpolation in Update/hot path
    {
        "name": "string_interpolation_hot_path",
        "pattern": r"void (Update|LateUpdate|FixedUpdate)[\s\S]*?\$\"",
        "severity": "warning",
        "message": "String interpolation in Update loop — allocates on heap",
        "fix": "Use pre-built strings or StringBuilder"
    },
    # Missing OnDestroy event cleanup
    {
        "name": "event_without_cleanup",
        "pattern": r"public event Action",
        "severity": "info",
        "message": "Public event declared — ensure nulled in OnDestroy to prevent leaks",
        "fix": "Add OnEventName = null; in OnDestroy()"
    },
    # Division for percentage reduction (common math bug)
    {
        "name": "division_for_reduction",
        "pattern": r"damage\s*/=\s*\d+\.\d+f?;",
        "severity": "warning",
        "message": "Division for damage reduction — consider multiplication for symmetric bonuses",
        "fix": "damage *= (2f - modifier) for symmetric +/- percentages"
    },
    # Obsolete attribute usage
    {
        "name": "obsolete_usage",
        "pattern": r"\[Obsolete",
        "severity": "info",
        "message": "Obsolete API found — plan migration path",
        "fix": "Replace with the recommended alternative"
    },
    # InvalidDataException misuse
    {
        "name": "wrong_exception_type",
        "pattern": r"throw new InvalidDataException",
        "severity": "info",
        "message": "InvalidDataException (System.IO) used for data validation — use FormatException",
        "fix": "Replace with System.FormatException for data format errors"
    }
]
```

### Fix 2: Add Batch Mode Execution to VB-Toolkit MCP Server

In the VB-Unity MCP server Python code, add a new action to `unity_editor`:

```python
# New action: execute_batch
# Launches Unity in batch mode, executes a method, reads results
async def execute_batch(self, method_name: str, timeout: int = 120) -> dict:
    """Execute a static C# method via Unity batch mode."""
    unity_path = self._find_unity_executable()
    project_path = self._get_project_path()
    result_path = os.path.join(project_path, "Temp", "vb_result.json")
    log_path = os.path.join(project_path, "Temp", "unity_batch.log")

    # Clean previous results
    if os.path.exists(result_path):
        os.remove(result_path)

    cmd = [
        unity_path, "-batchmode",
        "-projectPath", project_path,
        "-executeMethod", method_name,
        "-logFile", log_path,
        "-quit"
    ]

    proc = await asyncio.create_subprocess_exec(*cmd)
    try:
        await asyncio.wait_for(proc.wait(), timeout=timeout)
    except asyncio.TimeoutError:
        proc.kill()
        return {"error": f"Unity batch mode timed out after {timeout}s"}

    if os.path.exists(result_path):
        with open(result_path) as f:
            return json.load(f)

    return {"error": "No result file produced", "log": self._tail_log(log_path)}
```

### Fix 3: Add Compile Verification Without Unity

Use `dotnet build` or `msbuild` to verify C# compilation without Unity:

```python
# In unity_qa, add action: verify_compile
async def verify_compile(self, project_path: str) -> dict:
    """Verify C# scripts compile using dotnet/msbuild (no Unity needed)."""
    # Find .csproj files generated by Unity
    csproj_files = glob.glob(os.path.join(project_path, "*.csproj"))

    if not csproj_files:
        return {"error": "No .csproj files found. Open Unity once to generate them."}

    # Try Assembly-CSharp.csproj (main project)
    main_csproj = os.path.join(project_path, "Assembly-CSharp.csproj")
    if not os.path.exists(main_csproj):
        main_csproj = csproj_files[0]

    result = subprocess.run(
        ["dotnet", "build", main_csproj, "--no-restore", "-v", "quiet"],
        capture_output=True, text=True, timeout=60
    )

    errors = [line for line in result.stderr.split('\n') if 'error CS' in line]
    warnings = [line for line in result.stderr.split('\n') if 'warning CS' in line]

    return {
        "success": result.returncode == 0,
        "errors": errors,
        "warnings": warnings[:20],  # Cap at 20
        "error_count": len(errors),
        "warning_count": len(warnings)
    }
```

### Fix 4: UI Contrast Checker Should Respect Dark Backgrounds

The `check_contrast` tool assumes white (#FFFFFF) background by default. For dark-themed games like VeilBreakers, it produces false positives.

```python
# Fix in ui_templates.py check_contrast
# Add parameter: default_background_color
# When resolving background color, check USS for:
#   1. Explicit background-color on element
#   2. Explicit background-color on parent chain
#   3. Use the provided default_background_color (not white)
#   4. Only fall back to white if truly unresolvable

# The tool should also:
# - Parse USS class definitions to resolve inherited styles
# - Support RGBA with alpha compositing
# - Output "confidence: low" when background is inferred vs explicit
```

### Fix 5: Add VB-Specific Validators

Create new actions in `unity_qa`:

```python
# New action: validate_vb_systems
# Checks VeilBreakers-specific invariants:
VB_VALIDATORS = [
    {
        "name": "brand_matrix_symmetry",
        "description": "Verify brand effectiveness matrix has exactly 2 strong + 2 weak per brand",
        "check": "Parse BrandSystem.cs EffectivenessMatrix and count entries"
    },
    {
        "name": "corruption_threshold_consistency",
        "description": "Verify corruption thresholds match across all files",
        "check": "Grep for corruption threshold values in DamageCalculator, CaptureFormula, SynergySystem"
    },
    {
        "name": "eventbus_completeness",
        "description": "Verify ClearAllListeners nulls every event",
        "check": "Count 'public static event' declarations vs null assignments in ClearAllListeners"
    },
    {
        "name": "singleton_pattern_consistency",
        "description": "Verify all singletons use SingletonMonoBehaviour<T> or have HasInstance",
        "check": "Find classes with _instance pattern, verify HasInstance exists"
    }
]
```

---

## Part 5: VB-Blender Tools Assessment

### Working Tools (Require Blender Running)

| Tool | Actions | Assessment |
|------|---------|------------|
| `blender_worldbuilding` | 15 | **Excellent** — BSP dungeons, Voronoi towns, boss arenas. Highest value. |
| `blender_animation` | 12 | **Excellent** — 5 gait types, 8 attack types, batch FBX export |
| `blender_rig` | 13 | **Very Good** — Rigify templates, spring bones, ragdoll auto-gen |
| `blender_environment` | 10 | **Very Good** — Terrain gen, biome painting, vegetation scatter |
| `blender_mesh` | 8 | **Good** — Topology grading, Quadriflow retopo, repair pipeline |
| `blender_uv` | 9 | **Good** — xatlas unwrap, texel density equalization |
| `blender_texture` | 12 | **Good** — PBR creation, baking. AI features (inpaint/upscale) need API keys |
| `blender_export` | 2 | **Good** — FBX/glTF with Unity-compatible settings |
| `asset_pipeline` | 12 | **Good** — LOD gen, weapon gen, character split. AI gen needs Tripo3D key |
| `concept_art` | 4 | **Limited** — AI gen needs fal.ai key. Palette extraction works locally |
| `blender_material` | 4 | **Basic** — CRUD operations, sufficient |
| `blender_scene` | 4 | **Basic** — Scene inspection and config |
| `blender_object` | 5 | **Basic** — Object CRUD with viewport capture |
| `blender_viewport` | 4 | **Good** — Contact sheets are valuable for visual verification |
| `blender_execute` | 1 | **Powerful** — Raw Python execution with security whitelist |

### Blender Gaps

| Gap | Fix |
|-----|-----|
| All tools need Blender running | No fix possible — Blender operations require Blender's Python runtime |
| No auto-chain to Unity import | Add `blender_export` → `unity_assets configure_fbx` pipeline action |
| `generate_ai_motion` is a stub | Implement when a suitable AI motion API becomes available |
| `texture inpaint` is a stub | Requires fal.ai key — document in setup guide |
| No art style consistency check | Add `validate_palette` with VeilBreakers dark fantasy color rules |

---

## Part 6: Summary for Fix Agent

### Priority 1 (Do First — Python-only, no Unity)
1. Add 12 new regex patterns to `analyze_code` (see Fix 1 above)
2. Add `verify_compile` action using `dotnet build` (see Fix 3)
3. Fix `check_contrast` to respect dark backgrounds (see Fix 4)

### Priority 2 (Needs Unity Integration)
4. Create `VBToolkitRunner.cs` dispatcher for batch mode (see Tier 1 in Part 3)
5. Create `VBToolkitBridge.cs` file-watch for interactive mode (see Tier 2)
6. Add `execute_batch` action to `unity_editor` (see Fix 2)

### Priority 3 (VB-Specific)
7. Add VB validators (brand matrix, corruption thresholds, EventBus completeness)
8. Wrap audits as NUnit tests for CI/CD (see Tier 3)
9. Add Blender → Unity auto-chain pipeline

### What's Working Well (Don't Break)
- Script generation quality is high (proper namespaces, VB conventions)
- 37 tools with 330+ actions is comprehensive coverage
- VeilBreakers-specific configs (10 brands, 4 paths, corruption) are correct
- Graceful degradation when API keys are missing
- Security whitelist on `blender_execute`

---

*Report generated 2026-03-22 by Claude Opus 4.6 for VeilBreakers quality pass v5.2*
