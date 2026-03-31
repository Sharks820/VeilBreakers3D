# External Integrations

**Analysis Date:** 2026-03-30

## APIs & External Services

**None in production.** VeilBreakers is a fully offline single-player game with zero runtime network dependencies. All game data is bundled locally.

**Development-Time AI/Tool Pipeline:**

### MCP Server Integrations (`.mcp.json`)

The project uses Model Context Protocol (MCP) servers to enable AI agent interaction with the development toolchain. Two profiles exist:

**Core Profile (`.mcp.json`):**

| MCP Server | Command | Purpose |
|------------|---------|---------|
| `sequential-thinking` | `npx @modelcontextprotocol/server-sequential-thinking` | Complex problem breakdown, multi-step reasoning |
| `serena` | `uvx serena start-mcp-server` | LSP-backed C# symbol intelligence (OmniSharp), 28+ tools |
| `gemini-cli` | `node mcp-gemini-cli` | Google Gemini CLI for second opinions, web research |
| `codex-cli` | `node mcp-codex-cli` | OpenAI Codex CLI for code review, analysis |
| `github` | `node Tools/mcp/github-mcp-launcher.js` | GitHub operations (PRs, issues, CI). Token auto-sourced from `gh auth token` |
| `vb-blender` | `uv run vb-blender-mcp` | 16 compound Blender tools (162 actions) for 3D asset creation |
| `vb-unity` | `uv run vb-unity-mcp` | 22 compound Unity tools (258 actions) for gameplay, VFX, audio, UI |
| `unity-mcp` | `unity-mcp-server` | IvanMurzak Unity-MCP for runtime debugging, Roslyn execute, editor control (port 8080) |

**Full Profile (`.mcp.full.json`) -- additional servers:**

| MCP Server | Command | Purpose |
|------------|---------|---------|
| `memory-graph` | `npx mcp-knowledge-graph` | Episodic memory / knowledge graph |
| `image-process` | `npx image-process-mcp-server` | Crop, resize, rotate, convert image formats |
| `notion` | `npx @notionhq/notion-mcp-server` | Project management (monster database, feature backlog) |

### Windows Compatibility Patch

- `Tools/mcp/spawn-patch.cjs` — CJS preload that patches `child_process.spawn` for Windows `.cmd` support. Required for `gemini-cli` and `codex-cli` MCP servers which use ESM modules.

## VB Toolkit (External Companion Project)

**Location:** `C:/Users/Conner/OneDrive/Documents/veilbreakers-gamedev-toolkit/Tools/mcp-toolkit`

**vb-blender (16 compound tools, 162 actions):**
- `blender_object` — Create/manipulate 3D objects
- `blender_mesh` — Mesh repair, game-ready validation
- `blender_uv` — UV unwrapping (xatlas, smart project)
- `blender_texture` — PBR texturing, baking, validation
- `blender_rig` — Humanoid/quadruped rigging
- `blender_animation` — Walk/attack/idle/reaction generation
- `blender_export` — FBX/glTF export
- `blender_quality` — 32 AAA procedural generators (weapons, armor, creatures, props)
- `blender_worldbuilding` — Dungeons, caves, towns, castles, ruins, boss arenas
- `asset_pipeline` — Full orchestration (compose_map, compose_interior, generate_3d via Tripo AI)
- `blender_viewport` — Visual QA (contact sheets, screenshots)
- `blender_execute` — Direct Blender Python (bpy/bmesh/mathutils)

**vb-unity (22 compound tools, 258 actions):**
- `unity_editor` — Recompile, play mode, screenshot, console logs, load scene
- `unity_vfx` — Particle systems, brand VFX, shaders, post-processing
- `unity_audio` — AI SFX (ElevenLabs), music, ambient, spatial audio
- `unity_ui` — Screen generation, WCAG contrast, icons, tooltips, notifications
- `unity_scene` — Terrain, lighting, animators, blend trees
- `unity_gameplay` — Mob controllers, spawn systems, behavior trees, AI director
- `unity_game` — Save, health, character controller, abilities, synergy, corruption
- `unity_content` — Inventory, dialogue, quests, loot, crafting, skill trees
- `unity_world` — Scene transitions, weather, day/night, fast travel, WFC dungeons
- `unity_camera` — Cinemachine virtual cameras, shake, cutscenes, lock-on
- `unity_performance` — Scene profiling, LOD, lightmap baking
- `unity_qa` — Code analysis, code review

## External AI Services (Development-Time Only)

These services are used through MCP tool servers during development, never at runtime:

| Service | Env Var | Used By | Purpose |
|---------|---------|---------|---------|
| ElevenLabs | `ELEVENLABS_API_KEY` | `vb-blender`, `vb-unity` | AI SFX generation, voice synthesis |
| Google Gemini | `GEMINI_API_KEY` | `vb-blender`, `vb-unity`, `gemini-cli` | Visual analysis, code review, research |
| Tripo3D | `TRIPO_API_KEY` | `vb-blender` | AI 3D model generation |
| Fal.ai | `FAL_KEY` | `vb-blender` | Image generation / concept art |
| OpenAI | (via `codex-cli` auth) | `codex-cli` | Code review, analysis |
| GitHub | (via `gh auth token`) | `github` MCP | PR management, issues, CI status |
| Notion | `NOTION_API_KEY` | `notion` MCP (full profile only) | Project management |

**Environment variable storage:** `.env` file (in `.gitignore`, never committed)

## Data Storage

**Databases:**
- None. All game data is JSON files loaded via `Resources.Load()` at startup.

**Game Data (Read-Only, Bundled):**
- `Assets/Resources/Data/monsters.json` — Monster definitions (stats, brands, descriptions)
- `Assets/Resources/Data/skills.json` — Skill definitions (damage, effects, costs)
- `Assets/Resources/Data/items.json` — Item definitions (consumables, equipment)
- `Assets/Resources/Data/heroes.json` — Hero definitions (stats, paths, brands)
- Loader: `Assets/Scripts/Core/GameDataAssets.cs` (singleton ScriptableObject, Addressable-ready)
- Parser: `Assets/Scripts/Core/GameDatabase.cs` (uses `JsonUtility.FromJson` with wrapper pattern)
- Data types: `Assets/Scripts/Data/MonsterData.cs`, `SkillData.cs`, `HeroData.cs`, `ItemData.cs`

**Save Data (Read/Write, Local Filesystem):**
- Location: `Application.persistentDataPath`
- Format: Custom binary — JSON -> GZip -> AES-CBC encryption -> HMAC-SHA256 integrity
- Key management: PBKDF2-derived key stored in PlayerPrefs with file fallback
- Handler: `Assets/Scripts/Managers/SaveFileHandler.cs`
- Data model: `Assets/Scripts/Data/SaveData.cs`

**Settings:**
- `PlayerPrefs` + `JsonUtility` via `Assets/Scripts/Managers/SettingsManager.cs`

**File Storage:**
- Local filesystem only (no cloud saves)

**Caching:**
- In-memory dictionary caches in `GameDatabase` for all game data
- No external cache service

## Authentication & Identity

**Auth Provider:**
- None. Offline single-player game. No user accounts, no login, no network authentication.

## Audio System

**Implementation:**
- Unity native AudioSource/AudioClip for playback
- Audio files in `Assets/Resources/Audio/` (Music, SFX, Ambient subdirectories)
- Config: `Assets/Scripts/Audio/AudioConfig.cs` (ScriptableObject)

## Monitoring & Observability

**Error Tracking:**
- Custom `ErrorLogger` utility with subsystem-specific prefixes (`[VB:Combat]`, `[VB:UI]`, etc.)
- Debug/Info logs stripped from release builds
- No external error tracking service (no Sentry, no Crashlytics)

**Unity Services (ALL DISABLED):**
- Unity Analytics: disabled
- Unity Ads: disabled
- Unity Purchasing: disabled
- Unity Cloud Build: disabled
- Crash Report API: disabled

## CI/CD & Deployment

**Hosting:**
- GitHub repository (private)
- Target platform: StandaloneWindows64

**CI Pipeline (`.github/workflows/unity-ci.yml`):**

| Job | Runner | Purpose |
|-----|--------|---------|
| `build-windows` | `ubuntu-latest` | Build Windows x64 player via `game-ci/unity-builder@v4` |
| `test-editmode` | `ubuntu-latest` | Run EditMode tests via `game-ci/unity-test-runner@v4` |
| `test-playmode-smoke` | `ubuntu-latest` | Run PlayMode smoke tests (category `Suite.Smoke`) |
| `test-editmode-coverage` | `ubuntu-latest` | EditMode tests + code coverage gate (35% minimum line coverage) |

**Triggers:**
- Pull requests to `develop` or `master` branches
- Path filters: `Assets/**`, `Packages/**`, `ProjectSettings/**`, `.github/workflows/unity-ci.yml`
- Manual dispatch (`workflow_dispatch`)
- Concurrency: cancel-in-progress per branch

**Required Secrets:**
- `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`, `UNITY_SERIAL` — Unity activation
- `GITHUB_TOKEN` — Automatic, for test result reporting

**Local CI Tools:**
- `Tools/ci/run_unity_tests.ps1` — Run EditMode/PlayMode tests locally
- `Tools/ci/find_unity.ps1` — Auto-detect Unity installation
- `Tools/ci/check_coverage.ps1` — Enforce coverage gate
- `Tools/ci/verify_phase.ps1` — Phase verification

**Build Artifacts:**
- Player build: `build/StandaloneWindows64`
- Test results: `artifacts/` (XML + coverage reports)

## Serena Code Intelligence

**Config:** `.serena/project.yml`
- Language: C# (OmniSharp language server)
- Project: `VeilBreakers3DCurrent`
- Encoding: UTF-8
- Read-only: false
- Respects `.gitignore`
- Requires `.sln` file: `VeilBreakers3DCurrent.sln` (present)

**Capabilities (28+ tools):**
- Symbol overview, find references, rename symbols
- Replace symbol bodies, insert before/after symbols
- Pattern search across codebase
- Memory store for cross-session knowledge
- Shell command execution

## DCC Bridge (Blender Integration)

**Tools:**
- `Tools/DCC_Bridge/install_blender_addon.bat` — Install Blender addon
- `Tools/DCC_Bridge/install_tripo3d_bridge.bat` — Install Tripo3D bridge
- `Tools/DCC_Bridge/start_tripo_server.py` — Start Tripo server
- `Tools/DCC_Bridge/check_port.bat` — Check Blender connection
- Connection: TCP on `localhost:9876`

## Webhooks & Callbacks

**Incoming:**
- None

**Outgoing:**
- None

## GitHub Integration

**Repository features:**
- `CODEOWNERS` file: `.github/CODEOWNERS`
- Issue templates: Bug report (`.github/ISSUE_TEMPLATE/bug_report.yml`), Feature request (`.github/ISSUE_TEMPLATE/feature_request.yml`)
- PR template: `.github/pull_request_template.md`
- CI workflow: `.github/workflows/unity-ci.yml`

## ScriptableObject Configuration Assets

**Game Config:**
- `AudioConfig` — Audio budgets, volumes, voice thresholds (`Assets/Scripts/Audio/AudioConfig.cs`)
- `UIAssets` — Centralized UI template/style references (`Assets/Scripts/UI/Core/UIAssets.cs`)
- `GameDataAssets` — Centralized game data asset references (`Assets/Scripts/Core/GameDataAssets.cs`)
- `HeroThemeConfig` — Per-hero visual theme configuration (`Assets/Scripts/UI/CharacterSelect/HeroThemeConfig.cs`)
- `HeroDisplayConfig` — Per-hero character select display settings (`Assets/Scripts/Data/HeroDisplayConfig.cs`)
- `CombatUIConfig` — Combat UI configuration (`Assets/Scripts/UI/Combat/CombatUIConfig.cs`)
- `StatusEffectData` — Status effect definitions (`Assets/Scripts/Data/StatusEffectData.cs`)
- `ShrineData` — Shrine definitions (`Assets/Scripts/Data/ShrineData.cs`)
- `AIPersonality` — AI behavior personality configs (`Assets/Scripts/AI/AIPersonality.cs`)
- `ScreenTransition` — Screen transition settings (`Assets/Scripts/UI/Core/ScreenTransition.cs`)

---

*Integration audit: 2026-03-30*
