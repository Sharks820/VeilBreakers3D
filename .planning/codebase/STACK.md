# Technology Stack

**Analysis Date:** 2026-03-30

## Languages

**Primary:**
- C# (Unity 6 / .NET Standard) — All game logic, editor tools, tests (128 scripts in `Assets/Scripts/`)
- HLSL/ShaderLab — Custom shaders (`Assets/Shaders/VeilCrack.shader`, `Assets/Shaders/VeilDissolve.shader`)

**Secondary:**
- USS (Unity Style Sheets) — UI Toolkit styling (`Assets/UI/Styles/CharacterSelect.uss`, `Assets/UI/Styles/VeilBreakers.uss`)
- UXML (Unity XML) — UI layout markup (`Assets/UI/Templates/*.uxml`, `Assets/UI/Screens/CharacterSelect.uxml`)
- JSON — Game data definitions (`Assets/Resources/Data/heroes.json`, `monsters.json`, `skills.json`, `items.json`)
- PowerShell — CI/build scripts (`Tools/ci/run_unity_tests.ps1`, `Tools/ci/check_coverage.ps1`, `Tools/ci/find_unity.ps1`, `Tools/ci/verify_phase.ps1`)
- Python 3.12 — Blender helper tools and model inspection (`Tools/*.py`, 12+ scripts)
- JavaScript (CJS/ESM) — MCP server launchers (`Tools/mcp/github-mcp-launcher.js`, `Tools/mcp/spawn-patch.cjs`, `Tools/mcp/launch-unity-mcp.js`)

## Runtime

**Engine:**
- Unity 6000.3.6f1 (Unity 6 LTS) — `ProjectSettings/ProjectVersion.txt`
- .NET Standard / Mono scripting runtime

**Render Pipeline:**
- Universal Render Pipeline (URP) 17.3.0 — `Packages/manifest.json`
- Global settings: `Assets/UniversalRenderPipelineGlobalSettings.asset`
- Default volume profile: `Assets/DefaultVolumeProfile.asset`
- Volume profile transitions used in CharacterSelect: `Assets/Scripts/UI/CharacterSelect/VolumeProfileTransitioner.cs`

**Package Manager:**
- Unity Package Manager (UPM)
- Lockfile: `Packages/packages-lock.json` (present)
- Custom scoped registry: `npmjs` (registry.npmjs.org) for `com.kyrylokuzyk` scope (PrimeTween)

## Frameworks

**Core:**
- Unity UI Toolkit — Primary UI system (41 scripts use `UnityEngine.UIElements`)
- TextMeshPro (via URP bundle) — Legacy text rendering in combat UI (6 scripts: `SkillSlotController.cs`, `AllyPanelController.cs`, `CaptureBannerController.cs`, `PlayerPanelController.cs`, `EnemyPanelController.cs`)
- Unity Input System 1.18.0 — Player input via `Assets/Settings/VeilBreakersInput.inputactions`
  - Used in: `Assets/Scripts/Core/InputManager.cs`, `Assets/Scripts/Core/VeilBreakersInputActions.cs`

**Animation:**
- PrimeTween 1.3.8 — Tween animations (15 scripts, primarily UI transitions in `Assets/Scripts/UI/CharacterSelect/` and `Assets/Scripts/UI/Core/`)
- Unity Animation system — Character/monster animators and blend trees

**Navigation:**
- Unity AI Navigation 2.0.12 — NavMesh pathfinding (package present, not yet referenced in C# scripts)

**3D Asset Loading:**
- glTFast 6.14.1 — GLB model import (24 GLB files across `Assets/Art/Models/Heroes/` and `Assets/Art/Models/Monsters/`)
- Addressables 2.8.1 — Asset management (referenced in `Assets/Scripts/Core/GameDataAssets.cs`, `Assets/Scripts/UI/Core/UIAssets.cs`)

**Testing:**
- Unity Test Framework 1.6.0 — EditMode and PlayMode tests (20 test files)
- NUnit (via Unity) — Test assertions
- Unity Code Coverage 1.2.7 — Coverage reporting and gating (35% minimum line coverage enforced in CI)

**Performance:**
- Unity Adaptive Performance 6.0.0 — Runtime performance scaling
- Unity Memory Profiler 1.1.12 — Memory analysis
- Unity Profile Analyzer 1.3.3 — CPU profiling

**Build/Dev:**
- Unity Editor 6000.3.6f1 — Development environment
- game-ci/unity-builder@v4 — CI build (GitHub Actions)
- game-ci/unity-test-runner@v4 — CI test runner (GitHub Actions)
- Serena (LSP-backed, OmniSharp) — C# symbol intelligence for AI agents
- unity-mcp-server (IvanMurzak) — Runtime debugging, Roslyn execute, editor control

## Key Dependencies

**Critical (from `Packages/manifest.json`):**

| Package | Version | Purpose |
|---------|---------|---------|
| `com.kyrylokuzyk.primetween` | 1.3.8 | UI tween animations (buttons, transitions, panels) |
| `com.unity.render-pipelines.universal` | 17.3.0 | URP rendering pipeline |
| `com.unity.inputsystem` | 1.18.0 | New Input System (action maps) |
| `com.unity.cloud.gltfast` | 6.14.1 | glTF/GLB model import at editor/runtime |
| `com.unity.addressables` | 2.8.1 | Addressable asset system |
| `com.unity.ai.navigation` | 2.0.12 | NavMesh navigation |
| `com.unity.ugui` | 2.0.0 | Legacy uGUI (TextMeshPro dependency, combat UI) |

**Infrastructure (built-in Unity modules used):**

| Module | Purpose |
|--------|---------|
| `com.unity.modules.physics` | 3D physics |
| `com.unity.modules.animation` | Animator/animation clips |
| `com.unity.modules.audio` | Audio playback |
| `com.unity.modules.particlesystem` | VFX particle systems |
| `com.unity.modules.video` | Video playback (menu background) |
| `com.unity.modules.terrain` | Terrain system |
| `com.unity.modules.ai` | NavMesh agents |
| `com.unity.modules.uielements` | UI Toolkit core |
| `com.unity.modules.unitywebrequest` | HTTP requests (asset loading) |

## Assembly Definitions

**Runtime:**
- `Assets/Scripts/VeilBreakers.Runtime.asmdef`
- Root namespace: `VeilBreakers`
- References: `Unity.InputSystem`, `Unity.TextMeshPro`, `Unity.RenderPipelines.Universal.Runtime`, `Unity.RenderPipelines.Core.Runtime`, `PrimeTween.Runtime`

**Editor:**
- `Assets/Scripts/Editor/VeilBreakers.Editor.asmdef`
- Root namespace: `VeilBreakers.Editor`
- References: `VeilBreakers.Runtime`, `VeilBreakers.Tests.Runtime`, `Unity.RenderPipelines.Universal.Runtime`, `Unity.RenderPipelines.GPUDriven.Runtime`
- Platform: Editor only

**Tests:**
- `Assets/Tests/EditMode/` — EditMode test assembly (11 test files)
- `Assets/Tests/PlayMode/` — PlayMode test assembly (2 test files)
- `Assets/Tests/RuntimeTests/` — Runtime test assembly (8 test files)

## Data Serialization

**Game Data (read-only, bundled):**
- JSON files loaded via `Resources.Load<TextAsset>()` with fallback candidates
- Loader: `Assets/Scripts/Core/GameDataAssets.cs` (singleton ScriptableObject, Addressable-ready)
- Parser: `Assets/Scripts/Core/GameDatabase.cs` (uses `JsonUtility.FromJson` with wrapper pattern for arrays)
- Data types in `Assets/Scripts/Data/`: `MonsterData.cs`, `SkillData.cs`, `HeroData.cs`, `ItemData.cs`

**Save System:**
- Handler: `Assets/Scripts/Managers/SaveFileHandler.cs`
- Serialization: `JsonUtility.ToJson()` -> GZip compression -> AES-CBC encryption -> HMAC-SHA256 integrity
- Encryption key: PBKDF2-derived, stored in PlayerPrefs with file fallback
- Location: `Application.persistentDataPath`

**Settings:**
- Manager: `Assets/Scripts/Managers/SettingsManager.cs`
- Storage: `PlayerPrefs` + `JsonUtility` for `GameSettings` object
- No third-party serializers (no Newtonsoft.Json, no MessagePack)

## Configuration

**Environment:**
- `.env` file present (contains API keys for external AI services -- existence noted only, NEVER read)
- Environment variables referenced by MCP servers: `ELEVENLABS_API_KEY`, `GEMINI_API_KEY`, `TRIPO_API_KEY`, `FAL_KEY`
- These are development-time only; the game has zero runtime network dependencies

**Build:**
- `ProjectSettings/ProjectSettings.asset` — Unity player settings
- `ProjectSettings/QualitySettings.asset` — Quality tiers
- `ProjectSettings/GraphicsSettings.asset` — Rendering config
- `.gitattributes` — LF line endings for source, binary tracking for assets, UnityYAMLMerge for scene/prefab files

**Input:**
- `Assets/Settings/VeilBreakersInput.inputactions` — Input action definitions

**Data Files:**
- `Assets/Resources/Data/heroes.json` — Hero stat/brand/path definitions
- `Assets/Resources/Data/monsters.json` — Monster definitions
- `Assets/Resources/Data/skills.json` — Skill definitions
- `Assets/Resources/Data/items.json` — Item definitions

## Git Configuration

- `.gitattributes` — Comprehensive cross-platform normalization (LF for source, binary for assets, UnityYAMLMerge for Unity serialized files)
- `.gitignore` — Standard Unity gitignore
- No Git LFS configured (binary assets stored directly in repo)

## Platform Requirements

**Development:**
- Windows 11 (primary development OS)
- Unity 6000.3.6f1 installed
- Node.js (via nvm4w at `C:/nvm4w/nodejs`)
- Python 3.12+ (for Blender tools)
- uv (Python package manager, for MCP toolkit servers)
- gh CLI authenticated (for GitHub MCP)
- Blender (for 3D asset pipeline, connects on localhost:9876)

**Production:**
- Target: StandaloneWindows64 (per CI build config in `.github/workflows/unity-ci.yml`)
- Offline single-player (no network dependencies)
- 1920x1080 minimum resolution

## Scenes

| Scene | Path | Purpose |
|-------|------|---------|
| Bootstrap | `Assets/Scenes/Bootstrap.unity` | App entry point, manager initialization |
| MainMenu | `Assets/Scenes/MainMenu.unity` | Title screen, menu navigation |
| CharacterSelect | `Assets/Scenes/CharacterSelect.unity` | Hero selection carousel |
| Battle | `Assets/Scenes/Battle.unity` | Turn-based combat encounters |
| Overworld | `Assets/Scenes/Overworld.unity` | Exploration/map |
| TestArena | `Assets/Scenes/TestArena.unity` | Development testing |

## Fonts

- `Assets/UI/Fonts/Cinzel-Variable.ttf` — Primary display font
- `Assets/UI/Fonts/CinzelDecorative-Bold.ttf` — Decorative headers
- `Assets/UI/Fonts/CinzelDecorative-Regular.ttf` — Decorative text
- `Assets/UI/Fonts/Rajdhani-Bold.ttf` — UI body text (bold)
- `Assets/UI/Fonts/Rajdhani-SemiBold.ttf` — UI body text (semi-bold)
- `Assets/UI/Fonts/Arial.ttf` — Fallback font
- `Assets/TextMesh Pro/Fonts/LiberationSans.ttf` — TMP default

## Media Assets

**Video:**
- `Assets/StreamingAssets/background_video.mp4` — Main menu background
- `Assets/StreamingAssets/background_video_reversed.mp4` — Reversed variant

**Audio:**
- `Assets/Resources/Audio/Music/` — Music tracks (MP3)
- `Assets/Resources/Audio/SFX/` — Sound effects (MP3)
- `Assets/Resources/Audio/Ambient/` — Ambient layers (WAV)

**3D Models (24 GLB files):**
- `Assets/Art/Models/Heroes/Nyx/model_v{1-4}_pbr.glb`
- `Assets/Art/Models/Heroes/Orion/model_v{1-4}_pbr.glb`
- `Assets/Art/Models/Heroes/Seraphina/model_v{1-4}_pbr.glb`
- `Assets/Art/Models/Monsters/Bloodshade/model_v{1-4}_pbr.glb`
- `Assets/Art/Models/Monsters/Grimthorn/model_v{1-4}_pbr.glb`
- `Assets/Art/Models/Monsters/Voltgeist/model_v{1-4}_pbr.glb`

---

*Stack analysis: 2026-03-30*
