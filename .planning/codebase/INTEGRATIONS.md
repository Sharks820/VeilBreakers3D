# External Integrations

**Analysis Date:** 2026-02-21

## APIs & External Services

**None in production.** VeilBreakers is a fully offline single-player game with no external API dependencies at runtime. All game data is bundled locally.

**Development-Time Integrations (AI/Tool Pipeline):**

- **MCP Unity** - AI agent control of Unity Editor
  - Package: `com.gamelovers.mcp-unity` (git)
  - Config: `ProjectSettings/McpUnitySettings.json` (port 8090)
  - Purpose: Allows Claude/AI agents to take screenshots, manipulate scenes, run builds
  - Runtime impact: None (editor-only)

- **Tripo3D Bridge** - AI 3D model generation
  - Implementation: `Assets/Editor/Tripo3DBridgeEditor.cs`
  - Connection: WebSocket on port 60600 to Blender addon
  - Import path: `Assets/Resources/Art/3D_Models/Tripo/`
  - Runtime impact: None (editor-only)

- **Blender DCC Bridge** - Unity-Blender workflow
  - Implementation: `Assets/Editor/DCCBridgeEditor.cs`
  - Addon: `Tools/DCC_Bridge/BlenderAddon/veilbreakers_dcc_bridge.py`
  - Target: Blender 4.4 (`C:\Program Files\Blender Foundation\Blender 4.4\blender.exe`)
  - Runtime impact: None (editor-only)

- **Mixamo** - Character animation import
  - Implementation: `Assets/Editor/MixamoAnimationImporter.cs`
  - Purpose: Import and configure humanoid animations from Mixamo
  - Runtime impact: None (editor-only, animations are baked into assets)

## Data Storage

**Databases:**
- None. All game data is JSON files loaded via `Resources.Load()` at startup.

**Game Data (Read-Only, Bundled):**
- `Assets/Resources/Data/monsters.json` - Monster definitions (stats, brands, descriptions)
- `Assets/Resources/Data/skills.json` - Skill definitions (damage, effects, costs)
- `Assets/Resources/Data/items.json` - Item definitions (consumables, equipment)
- `Assets/Resources/Data/heroes.json` - Hero definitions (existence indicated by meta file)
- Loaded by: `Assets/Scripts/Core/GameDatabase.cs` (singleton, async loading)
- Data types defined in: `Assets/Scripts/Data/` (MonsterData.cs, SkillData.cs, ItemData.cs, HeroData.cs)

**Save Data (Read/Write, Local Filesystem):**
- Location: `Application.persistentDataPath` (platform-specific)
- Format: Custom binary with "VEIL" magic header
- Serialization pipeline: JSON -> GZip -> AES-CBC encryption -> HMAC-SHA256 integrity
- Key derivation: PBKDF2 with static salt
- Handler: `Assets/Scripts/Managers/SaveFileHandler.cs`
- Manager: `Assets/Scripts/Managers/SaveManager.cs`
- Auto-save: `Assets/Scripts/Managers/AutoSaveManager.cs` (event-driven, debounced at 30s intervals)
- Migration: `Assets/Scripts/Managers/MigrationRunner.cs` (version-chained migrations, currently v1->v2)

**File Storage:**
- Local filesystem only (no cloud saves)
- Settings stored via `PlayerPrefs` (see `Assets/Scripts/Managers/SettingsManager.cs`)

**Caching:**
- In-memory dictionary caches in `GameDatabase` for all game data
- Audio bank memory management with LRU eviction (`Assets/Scripts/Audio/AudioManager.cs`)
- No external cache service

## Authentication & Identity

**Auth Provider:**
- None. Offline single-player game.
- No user accounts, no login, no network authentication.

## Audio System

**Current Implementation:**
- Unity native `AudioMixer` for volume control
- Custom bank management system prepared for FMOD (stubs in place)
- Volume channels: Master, Music, SFX, Voice, Ambient
- Bank types: Core (always loaded), Zone, Monster, NPC, Encounter

**Planned FMOD Integration:**
- FMOD event paths configured in `Assets/Scripts/Audio/AudioConfig.cs` (e.g., `event:/Music/Exploration`, `event:/SFX/Combat/Hit_`)
- FMOD API calls are present as comments throughout `Assets/Scripts/Audio/AudioManager.cs`
- FMOD package NOT currently installed - all FMOD calls are commented out stubs
- Current sound playback: `Debug.Log()` placeholder only

**VERA Voice System:**
- Dynamic voice processing based on "Veil Integrity" percentage
- Stages: Clean (>80%), Mild Glitch (>60%), Distortion (>40%), Dual Voice (>20%), Full Corruption (<20%)
- Implementation: `Assets/Scripts/Audio/VERAVoiceController.cs`
- Config thresholds in: `Assets/Scripts/Audio/AudioConfig.cs`

## Monitoring & Observability

**Error Tracking:**
- Custom `ErrorLogger` utility (`Assets/Scripts/Core/ErrorLogger.cs`)
- Log levels: Verbose, Debug, Info, Warning, Error, Critical
- Subsystem-specific prefixes: `[VB:Combat]`, `[VB:UI]`, `[VB:Audio]`, `[VB:Save]`, `[VB:AI]`, `[VB:Capture]`
- Debug/Info logs stripped from release builds via `[Conditional]` attributes
- Performance timing via `Stopwatch` with configurable warning thresholds

**Unity Services (ALL DISABLED):**
- Unity Analytics: disabled
- Unity Ads: disabled
- Unity Purchasing: disabled
- Unity Cloud Build: disabled
- Crash Report API: disabled (`enableCrashReportAPI: 0`)
- No Firebase, no PlayFab, no Steamworks

**Logs:**
- `UnityEngine.Debug.Log*` via `ErrorLogger` wrapper
- Player log enabled (`usePlayerLog: 1`)
- No external log aggregation service

## CI/CD & Deployment

**Hosting:**
- Not yet deployed. Development builds only.
- Target: Windows Standalone

**CI Pipeline:**
- None configured
- GitHub repository present (`.mcp.json` has GitHub MCP server configured)
- No GitHub Actions, no automated builds

**Build System:**
- Unity built-in build pipeline
- Addressables build pipeline configured (`Assets/AddressableAssetsData/`)
- Build scripts: Fast Mode, Virtual Mode, Packed Mode, Packed Play Mode

## Environment Configuration

**Required env vars:**
- None for runtime
- `GITHUB_TOKEN` - GitHub MCP server authentication (development only, via `.mcp.json`)

**Secrets location:**
- `.env` is in `.gitignore` (no `.env` file currently exists)
- No external secrets required for the game
- Save file encryption key derived from PBKDF2 with embedded salt (in `SaveFileHandler.cs`)

## Webhooks & Callbacks

**Incoming:**
- None

**Outgoing:**
- None

## Unity Cloud Services

**Project Registration:**
- Cloud Project ID: `3ba56aa7-39e3-4fb1-8c28-536b72fb73d0`
- Organization: `twotoedtimmy_unity`
- All cloud services disabled (`cloudEnabled: 0`)

## ScriptableObject Configuration Assets

**Game Config:**
- `AudioConfig` - Audio budgets, volumes, FMOD paths, VERA voice thresholds (`Assets/Scripts/Audio/AudioConfig.cs`)
- `UIAssets` - Centralized UI template/style references (`Assets/Scripts/UI/Core/UIAssets.cs`)
- `HeroDisplayConfig` - Per-hero character select display settings (4 heroes: Nyx, Orion, Seraphina, Vex at `Assets/Resources/CharacterSelect/HeroDisplayConfigs/`)

**Unity Pipeline Config:**
- `Assets/UniversalRenderPipelineGlobalSettings.asset` - URP global settings
- `Assets/DefaultVolumeProfile.asset` - Post-processing volume
- `Assets/UI/VeilBreakersPanelSettings.asset` - UI Toolkit panel settings
- `Assets/AddressableAssetsData/AddressableAssetSettings.asset` - Addressables configuration
- `Assets/TextMesh Pro/Resources/TMP Settings.asset` - TextMesh Pro defaults

## Network/Multiplayer

**Status:** Not implemented, not planned for current milestone.
- `com.unity.multiplayer.center` 1.0.1 is installed (Unity default package) but unused
- `com.unity.xr.management` 4.5.4 is installed (Unity default package) but unused
- No networking code exists in `Assets/Scripts/`

---

*Integration audit: 2026-02-21*
