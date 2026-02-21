# Technology Stack

**Analysis Date:** 2026-02-21

## Languages

**Primary:**
- C# (latest supported by Unity 6) - All game logic, editor tools, tests
- USS (Unity Style Sheets) - UI styling (`Assets/UI/Styles/*.uss`)
- UXML (Unity XML) - UI layout definitions (`Assets/UI/Screens/*.uxml`, `Assets/UI/Templates/*.uxml`)

**Secondary:**
- JSON - Game data files (`Assets/Resources/Data/*.json`)
- YAML - Unity serialized assets (`ProjectSettings/*.asset`)

## Runtime

**Environment:**
- Unity 6000.3.6f1 (Unity 6 LTS track) - `ProjectSettings/ProjectVersion.txt`
- .NET Standard / Mono scripting runtime
- Active Input Handling: Input System (New) - `activeInputHandler: 2` in `ProjectSettings/ProjectSettings.asset`

**Graphics Pipeline:**
- Universal Render Pipeline (URP) 17.3.0 - `Packages/manifest.json`
- Global settings: `Assets/UniversalRenderPipelineGlobalSettings.asset`
- Volume profile: `Assets/DefaultVolumeProfile.asset`
- Graphics APIs (Windows): D3D11 + Vulkan (auto-detection disabled) - `m_BuildTargetGraphicsAPIs` in ProjectSettings

**Package Manager:**
- Unity Package Manager (UPM)
- Lockfile: `Packages/packages-lock.json` (present)

## Frameworks

**Core:**
- Unity Engine 6000.3.6f1 - Game engine
- Universal Render Pipeline 17.3.0 - Rendering
- Input System 1.18.0 - Player input handling
- UI Toolkit (UIElements module) - All UI (NOT legacy UGUI for new code)
- TextMesh Pro - Text rendering within UI Toolkit

**Testing:**
- Unity Test Framework 1.6.0 - Test runner
- NUnit (via Unity) - Assertions
- Code Coverage 1.2.7 - Coverage reports

**Build/Dev:**
- Addressables 2.8.0 - Asset management and loading
- Burst Compiler 1.8.27 (transitive dep) - Performance-critical math
- Unity Mathematics 1.2.1 (transitive dep) - Math library
- Memory Profiler 1.1.9 - Memory analysis
- Profile Analyzer 1.2.3 - Performance profiling
- Adaptive Performance 6.0.0 - Runtime performance scaling

**Navigation:**
- AI Navigation 2.0.9 - NavMesh pathfinding

**3D Content Pipeline:**
- glTFast 5.2.0 - glTF/GLB model import
- Blender 4.4 integration via DCC Bridge (`Assets/Editor/DCCBridgeEditor.cs`)
- Tripo3D Bridge for AI model generation (`Assets/Editor/Tripo3DBridgeEditor.cs`)
- Mixamo animation import (`Assets/Editor/MixamoAnimationImporter.cs`)

**Editor Integration:**
- Rider IDE 3.0.38 - Primary IDE support
- MCP Unity plugin (`com.gamelovers.mcp-unity`) - AI agent control of Unity Editor

## Key Dependencies

**Critical (game breaks without these):**
- `com.unity.inputsystem` 1.18.0 - All player input (referenced in `VeilBreakers.Runtime.asmdef`)
- `com.unity.render-pipelines.universal` 17.3.0 - All rendering
- `com.unity.modules.uielements` 1.0.0 - UI Toolkit (all menus, HUD, screens)
- `com.unity.ugui` 2.0.0 - Scene transition fade canvas (legacy usage in `VBSceneManager.cs`)
- Unity.TextMeshPro - Text rendering (referenced in `VeilBreakers.Runtime.asmdef`)
- `com.unity.addressables` 2.8.0 - Asset loading strategy

**Infrastructure:**
- `com.unity.nuget.newtonsoft-json` 3.2.1 - JSON serialization (transitive via MCP Unity)
- `com.unity.ai.navigation` 2.0.9 - AI pathfinding for overworld
- `com.unity.cloud.gltfast` 5.2.0 - 3D model import pipeline

**Dev/Debug Only:**
- `com.unity.memoryprofiler` 1.1.9 - Memory debugging
- `com.unity.performance.profile-analyzer` 1.2.3 - Performance debugging
- `com.unity.testtools.codecoverage` 1.2.7 - Test coverage
- `com.gamelovers.mcp-unity` (git) - AI agent Unity Editor control

## Assembly Definitions

**Runtime:**
- `Assets/Scripts/VeilBreakers.Runtime.asmdef` - Root namespace `VeilBreakers`, references: `Unity.InputSystem`, `Unity.TextMeshPro`

**Editor:**
- `Assets/Scripts/Editor/VeilBreakers.Editor.asmdef` - Root namespace `VeilBreakers.Editor`, references: `VeilBreakers.Runtime`, `Unity.RenderPipelines.Universal.Runtime`, `Unity.RenderPipelines.GPUDriven.Runtime`

**Tests:**
- `Assets/Tests/EditMode/VeilBreakers.Tests.EditMode.asmdef` - References: `VeilBreakers.Runtime`, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`, NUnit
- `Assets/Tests/PlayMode/VeilBreakers.Tests.PlayMode.asmdef` - References: `Assembly-CSharp`

## Data Serialization

**Game Data (read-only):**
- JSON files loaded via `Resources.Load<TextAsset>()` at runtime
- Paths defined in `Assets/Scripts/Core/Constants.cs`:
  - `Data/monsters` -> `Assets/Resources/Data/monsters.json`
  - `Data/skills` -> `Assets/Resources/Data/skills.json`
  - `Data/heroes` -> `Assets/Resources/Data/heroes.json` (meta only, no file)
  - `Data/items` -> `Assets/Resources/Data/items.json`
- Data types: `MonsterData`, `SkillData`, `HeroData`, `ItemData` in `Assets/Scripts/Data/`
- Archive versions maintained: `monsters_archive_v1.json`, `skills_archive_v1.json`

**Save Data:**
- Custom binary format with header: magic bytes "VEIL" + version + flags + HMAC
- Serialization chain: `JsonUtility.ToJson()` -> GZip compression -> AES encryption -> HMAC-SHA256 integrity
- Save version: 2 (defined in `Assets/Scripts/Data/SaveData.cs`)
- Migration system: `Assets/Scripts/Managers/MigrationRunner.cs` with `ISaveMigration` interface
- File handler: `Assets/Scripts/Managers/SaveFileHandler.cs`
- Stored at: `Application.persistentDataPath`

**ScriptableObjects:**
- `AudioConfig` (`Assets/Scripts/Audio/AudioConfig.cs`) - Audio system configuration
- `UIAssets` (`Assets/Scripts/UI/Core/UIAssets.cs`) - Centralized UI asset references
- `HeroDisplayConfig` - Per-hero character select configurations (`Assets/Resources/CharacterSelect/HeroDisplayConfigs/*.asset`)

## Configuration

**Environment:**
- No `.env` files (offline game, no external services)
- Unity Cloud Project ID: `3ba56aa7-39e3-4fb1-8c28-536b72fb73d0` (cloud services disabled)
- Organization: `twotoedtimmy_unity`
- Cloud services all disabled: Build, Game Performance, Legacy Analytics, Purchasing, UDP, Unity Ads

**Build:**
- Product: `VeilBreakers3D` v4.30 Alpha
- Company: `VeilBreakers`
- Application ID: `com.VeilBreakers.VeilBreakers3D`
- Default resolution: 1920x1080, fullscreen borderless
- Target platform: Windows Standalone (primary)
- Color space: Gamma (`m_ActiveColorSpace: 0`)
- Incremental GC enabled
- Frame timing stats enabled

**MCP (AI Agent) Configuration:**
- Core profile: `.mcp.json` - sequential-thinking, memory-graph, serena, mcp-unity, gemini-cli, github, blender
- Full profile: `.mcp.full.json` - Extended tool set
- MCP Unity server: port 8090, auto-start enabled (`ProjectSettings/McpUnitySettings.json`)

## Platform Requirements

**Development:**
- Windows 11 (primary development platform)
- Unity 6000.3.6f1
- Rider IDE (configured via `com.unity.ide.rider`)
- Blender 4.4 (optional, for 3D content pipeline)
- Node.js (for MCP server tools)

**Production:**
- Windows Standalone (primary target)
- 1920x1080 minimum resolution
- D3D11 or Vulkan GPU

## Scenes

Registered scenes (`Assets/Scenes/`):
- `Bootstrap` - Initial load, manager initialization
- `MainMenu` - Title screen, menu navigation
- `CharacterSelect` - Hero selection (current rebuild focus)
- `TestArena` - Development testing scene
- `Battle` - Combat encounters
- `Overworld` - Exploration/map

Scene management: `Assets/Scripts/Managers/VBSceneManager.cs` (singleton, async loading with fade transitions)

---

*Stack analysis: 2026-02-21*
