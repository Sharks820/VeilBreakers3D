# VEILBREAKERS - Project Memory

> **SINGLE SOURCE OF TRUTH** | Version: **v5.1** | Last updated: 2026-03-22

---

## Project Overview

| Field | Value |
|-------|-------|
| Engine | **Unity 3D** (UI Toolkit) |
| Genre | AAA 3D Real-Time Tactical Monster RPG |
| Combat Style | Dragon Age: Inquisition action-forward |
| Art Style | Dark Fantasy Horror |
| GitHub | Sharks820/VeilBreakers3D |
| Branch | `master` |
| Canonical Path | `C:\Users\Conner\OneDrive\Documents\VeilBreakers3DCurrent` |
| Local Backup | `C:\Users\Conner\VeilBreakers3DCurrent_BACKUP` |
| Archive | `C:\Users\Conner\Archive\VeilBreakers_Archive_2026-02-01` |

---

## Core Game Systems

### 10-Brand Combat System

| Brand | Role | Strong Against (2x) | Weak Against (0.5x) |
|-------|------|---------------------|---------------------|
| **IRON** | Tank | SURGE, DREAD | SAVAGE, RUIN |
| **SAVAGE** | Melee Burst | IRON, MEND | LEECH, GRACE |
| **SURGE** | Ranged DPS | VENOM, LEECH | IRON, VOID |
| **VENOM** | DoT/Debuff | GRACE, MEND | SURGE, RUIN |
| **DREAD** | CC/Terror | SAVAGE, GRACE | IRON, VOID |
| **LEECH** | Drain Tank | SAVAGE, RUIN | SURGE, VENOM |
| **GRACE** | Battle Healer | VOID, RUIN | SAVAGE, VENOM |
| **MEND** | Ward Healer | VOID, LEECH | SAVAGE, VENOM |
| **RUIN** | AOE Devastator | IRON, VENOM | LEECH, GRACE |
| **VOID** | Chaos Mage | SURGE, DREAD | GRACE, MEND |

### 4 Veilbreaker Paths

| Path | Strong Synergy Brands | Weak Synergy Brands | Starter Hero | Locked Hero |
|------|----------------------|---------------------|--------------|-------------|
| IRONBOUND | IRON, MEND, LEECH | VOID, SAVAGE, RUIN | Vex | Warden |
| FANGBORN | SAVAGE, VENOM, RUIN | GRACE, MEND, IRON | Seraphina | Vex |
| VOIDTOUCHED | VOID, DREAD, SURGE | IRON, GRACE, MEND | Orion | Shade |
| UNCHAINED | All Neutral | None (flex) | Nyx | Flux |

### Path-Brand Synergy (Tiered)

| Tier | Requirement | Damage | Defense | Corruption | Combo? |
|------|-------------|--------|---------|------------|--------|
| **FULL** | 3/3 match | +8% | +8% | 0.5x | YES |
| **PARTIAL** | 2/3 match | +5% | +5% | 0.75x | NO |
| **NEUTRAL** | 0-1/3 match | +0% | +0% | 1.0x | NO |
| **ANTI** | Any Weak brand | +0% | +0% | 1.5x each | NO |

### Corruption System

| Range | Status | Effect |
|-------|--------|--------|
| 0-10% | ASCENDED | +25% all stats |
| 11-25% | Purified | +10% all stats |
| 26-50% | Unstable | Normal |
| 51-75% | Corrupted | -10% all stats |
| 76-79% | Abyssal | -20% all stats |
| **80-100%** | **UNTAMED** | Monster uncontrollable |

### Monster Evolution

**Pure/Hybrid:** 3 stages (Birth 1-25 → Evo2 26-50 → Evo3 51-100)
**PRIMAL:** 2 stages (Birth 1-35 → Evolved 36-109 → Overflow 110-120)

### Party Structure
- **3 Active** + **3 Backpack** + Unlimited Storage
- Swap cooldown: 3-5s for abilities, instant for basic attacks

### 6-Slot Ability Structure

| Slot | Type | Cooldown |
|------|------|----------|
| 1 | Basic Attack | None |
| 2 | Defend/Guard | None |
| 3 | Skill 1 | 4-6s |
| 4 | Skill 2 | 10-15s |
| 5 | Skill 3 | 18-25s |
| 6 | Ultimate | 45-90s |

### Capture System (Post-Battle)
```
Capture Chance = f(HP%, Corruption%, Item Tier) + QTE Bonus
```
Items: Veil Shard (+0%) → Crystal (+15%) → Core (+30%) → Heart (+50%)
Failure: Flee (low corrupt) or Berserk (high corrupt)

---

## Project Structure

```
Assets/
├── Scripts/           # C# code (VeilBreakers.* namespaces)
│   ├── Core/          # GameManager, EventBus, Constants, GameBootstrap, InputManager
│   ├── Combat/        # BattleManager, DamageCalculator, Combatant
│   ├── Systems/       # BrandSystem, SynergySystem, CorruptionSystem, VERASystem
│   ├── AI/            # GambitController, GambitEvaluator, AIPersonality
│   ├── Capture/       # CaptureManager, QTEController, CaptureFormulaCalculator
│   ├── Commands/      # QuickCommandManager, RadialMenuController, TimeSlowController
│   ├── Managers/      # SaveManager, SettingsManager, AutoSaveManager, ShrineManager
│   ├── Audio/         # AudioManager, MusicManager, VERAVoiceController
│   ├── UI/            # UI controllers (Menus/, Combat/, Core/, Controls/, Effects/)
│   ├── Data/          # Enums, ScriptableObjects (MonsterData, HeroData, etc.)
│   ├── Utils/         # ObjectPool, Extensions, SingletonMonoBehaviour
│   ├── Editor/        # Editor scripts (TestArenaSetup, UITextSettingsSetup)
│   └── Test/          # Test scripts (CombatTests, SaveTests, etc.)
├── Art/               # Visual assets (3D_Models/, Textures/, VFX/)
├── Audio/             # Music/, SFX/, Voice/
├── Data/              # JSON data, ScriptableObjects
├── Prefabs/           # Prefab assets
├── Scenes/            # Unity scenes
└── UI/                # UI Toolkit (Styles/, Templates/)
Docs/                  # Documentation (plans/, MIGRATION_PLAN.md)
screenshots/           # Debug screenshots only
```

### Naming Conventions
- 3D Models: `[type]_[name]_[variant].fbx` (e.g., `monster_hollow_base.fbx`)
- Animations: `[character]@[action]_[variant].anim` (e.g., `hollow@attack_slash.anim`)
- Namespaces: `VeilBreakers.Core`, `.Combat`, `.Systems`, `.UI`, `.Data`

---

## C# Code Standards

```csharp
namespace VeilBreakers.Combat
{
    public class BattleManager : MonoBehaviour
    {
        private const int kMaxPartySize = 3;        // Constants: k prefix
        [SerializeField] private int _currentTurn;  // Private: _ prefix
        public int CurrentTurn => _currentTurn;     // Properties: PascalCase
        public event Action<int> OnTurnChanged;     // Events: On prefix
    }
}
```

**Patterns:**
- Singletons: `Instance` property with `DontDestroyOnLoad`
- Data: ScriptableObjects for monsters, skills, items
- Events: C# `Action<T>` or `UnityEvent` for Inspector binding
- Pooling: `ObjectPool<T>` for frequently spawned objects

---

## UI System (UI Toolkit)

| Screen | Controller | Status |
|--------|------------|--------|
| Main Menu | MainMenuController.cs + TitleScreenVFX + MoltenButtonVFX | ✅ AAA (video bg, lightning, music) |
| Settings | SettingsPanelController.cs | ✅ |
| Character Select | CharacterSelectController.cs (BG3-inspired redesign) | ✅ AAA |
| Inventory | InventoryController.cs | ✅ |
| Monster Collection | MonsterCollectionController.cs | ✅ |
| VERA Dialogue | VERADialogueController.cs | ✅ |

### Character Select System (BG3-Inspired) - NEW Feb 2026
5-component cinematic character selection:
- `CharacterSelectController.cs` - Orchestrator
- `HeroStageController.cs` - Hero management and animation
- `EnvironmentController.cs` - Themed backgrounds per hero
- `HeroVFXController.cs` - Per-hero visual effects
- `VeilTearTransition.cs` - Scene transition effects
- Design doc: `Docs/plans/2026-02-05-character-select-redesign.md`

**USS Classes:** `.vb-` (core), `.menu-`, `.dialogue-`, `.vera-`, `.inventory-`, `.monster-`, `.rarity-`, `.corruption-`

**Rarity Colors:** Common (#555), Uncommon (#40ff40), Rare (#4080ff), Epic (#a040ff), Legendary (#ffa040), Mythic (#ff4080)

---

## MCP & Tools

### Project MCPs (.mcp.json) - Optimized v4.43
| MCP | Purpose | Notes |
|-----|---------|-------|
| sequential-thinking | Complex problem solving | Always on |
| memory-graph | Episodic memory | Uses mcp-knowledge-graph |
| mcp-unity | Unity Editor control | Unity package server |
| gemini-cli | Claude+Gemini collaboration | Second opinions, web research |
| github | PRs, issues, CI | Requires GITHUB_TOKEN |
| blender | 3D modeling | Blender MCP |
| image-process | Image manipulation | Asset prep |
| notion | Project management | Requires NOTION_API_KEY |

**Plugin-Provided MCPs (auto-loaded):** Serena, Context7, Greptile, Episodic Memory, Chrome
**Removed (v4.43):** serena, context7, greptile (duplicates of plugins), figma, figma-local, atlassian (unused)

### Claude+Gemini Collaboration Skills (NEW v4.43)
| Skill | Purpose |
|-------|---------|
| gemini-collab | 5 collaboration modes: Second Opinion, Parallel Analysis, Research, Adversarial Review, Balance Validation |
| feature-template | Structured template for feature requests with acceptance criteria |
| refactor-template | Structured template for refactoring with constraints |
| tdd-loop | Automated TDD with Gemini debugging assistance |

### Custom Agents
unity-architect, unity-code-reviewer, unity-debugger, unity-performance-profiler, balance-analyzer, vera-dialogue-tester, bug-hunter, asset-generator, commit-helper, documentation-writer

---

## Work-Branch Architecture (Multi-Agent Workflow)

To ensure maximum efficiency and prevent technical debt, development is divided into functional branches utilized by specialized agents. Every branch is synchronized with the latest `master` optimizations.

| Branch | Primary Agent | Responsibility |
| :--- | :--- | :--- |
| `master` | **SYSTEM OVERSEER** | Pristine, Unity 6-ready production source. |
| `feature/ai-behavior` | `balance-analyzer` | Gambit logic, AI personalities, and combat fairness. |
| `feature/combat-system` | `unity-architect` | Core battle loop, status effects, and damage math. |
| `feature/ui-system` | `documentation-writer` | UI Toolkit controllers, styling, and navigation. |
| `feature/capture-system` | `bug-hunter` | Monster binding mechanics and QTE precision. |
| `feature/champion-system` | `balance-analyzer` | Hero progression, path bonuses, and stat scaling. |
| `feature/dialogue-vera` | `vera-dialogue-tester` | Dialogue branching, VERA personalities, and narrative. |
| `feature/monster-system` | `asset-generator` | Monster data, evolution trees, and 3D asset integration. |
| `feature/world-terrain` | `asset-generator` | Map generation, environment props, and spatial layout. |

---

## Current Status (v5.1 — AAA Quality Pass, 2026-03-21)

- **Main Menu:** AAA-quality with video background, lightning overlay, music, molten button VFX, ember/ash particles. Orange bar transition glitch fixed.
- **Character Select:** BG3-inspired cinematic redesign with 5-component architecture. Comprehensive 42-bug scan complete — 28+ fixes applied across Tier 0 (security), Tier 1 (gameplay), and Tier 2-3 (polish). `_isEmbarking` finally block, dual NavigationMove removed, VolumeProfile leak fixed, OnTransitionComplete multi-fire fixed, gamepad zone gating added.
- **Core Systems:** Modernized (Input System, URP-ready, ThemeManager v6 APIs, PrimeTween 1.3.8). EventBus fully cleaned (4 missing events added to ClearAllListeners). BattleResumed event added for berserk flow.
- **Combat:** DamageCalculator corruption modifier fixed (defender double-dip removed). Combatant.RemoveFromBattle() added for clean capture removal. ApplyDamageBuff now stacks multiplicatively. BattleManager uses HashSet for O(1) party lookups.
- **Save System:** Encryption key file backup (survives PlayerPrefs clear). Non-blocking pause save (no more deadlock risk).
- **Performance:** Debug.Log migrated to ErrorLogger (stripped in release builds). Hot-path allocations fixed (StatusEffectManager buffers, BattleManager HashSet). Camera.main cached. VERASystem glitch chars static.
- **CI/CD:** Unity PR CI via GitHub Actions (`.github/workflows/unity-ci.yml`)
- **Git Workflow:** Formalized branch model (master/develop/feature) - see `Docs/plans/GIT_WORKFLOW_AAA.md`
- **GitHub Templates:** PR template, bug report, feature request issue templates
- **Monster Redesign:** v2 specs complete, v1 data archived
- **AAA Features:**
  - TitleScreenVFX (embers, ash, sparks, smoke, lightning, video bg)
  - MoltenButtonVFX (art-style buttons with hover/press effects)
  - AnimatedBar, ScreenTransition, ParallaxBackground
  - VfxTextureImportPostprocessor (auto-import settings for VFX textures)
- **Next:** 3D model integration, combat system with 3D, monster redesign implementation

---

## AI Development Toolkit (v5.0 — 2026-03-21)

### MCP Servers (10 active)

| Server | Purpose | Best For |
|--------|---------|----------|
| **vb-unity** | VeilBreakers Unity toolkit — VFX, audio, UI, scene, gameplay scripts | Scene setup, lighting, terrain, animator creation, NavMesh baking |
| **vb-blender** | VeilBreakers Blender toolkit — mesh, UV, texture, rig, animation | 3D modeling, asset pipeline, concept art, environment building |
| **gemini-cli** | Google Gemini chat/search/file analysis | Second opinions, web research, adversarial code review |
| **codex-cli** | OpenAI Codex chat/analysis | Senior code review, alternative perspectives on architecture |
| **github** | GitHub API — PRs, issues, commits | CI status, PR creation, issue tracking |
| **serena** | Symbol-aware C# code navigation | Find references, rename symbols, understand unfamiliar code |
| **sequential-thinking** | Structured multi-step reasoning | Balance calculations, complex debugging, architecture decisions |
| **memory-graph** | Knowledge graph with episodic memory | Cross-session project knowledge, persistent context |
| **desktop-commander** | Terminal process management | Long-running builds, background tasks, process monitoring |
| **claude-in-chrome** | Browser automation via Chrome DevTools | Visual testing, web research, UI screenshot capture |

### Enabled Plugins (16)

| Plugin | Purpose | When to Use |
|--------|---------|-------------|
| **csharp-lsp** | C# language server diagnostics (csharp-ls v0.22.0) | Real-time error detection, symbol navigation in .cs files |
| **frontend-design** | Production-grade UI design skill | UI Toolkit layouts, character select screens, menus |
| **superpowers** | Core skills: brainstorming, debugging, plans, TDD, verification | Every major feature — brainstorm first, then plan, then implement |
| **code-review** | Structured code review | After major implementations, before merging |
| **code-simplifier** | Code cleanup and simplification | After completing features, reduce complexity |
| **semgrep** | Static analysis (auto-scans on Edit/Write) | Always active — catches security issues, anti-patterns |
| **context7** | Up-to-date library documentation | Unity API questions, package docs |
| **episodic-memory** | Cross-session conversation history | Remembering past decisions, debugging approaches |
| **commit-commands** | Git commit/push/PR workflows | Structured commits, PR creation |
| **security-guidance** | Secure-by-default coding | When implementing any system boundary code |
| **superpowers-lab** | Semantic duplicate detection | Finding redundant code across the codebase |
| **double-shot-latte** | Episodic memory + git integration | Enhanced memory with git context |
| **claude-code-setup** | Automation recommendations | Optimizing Claude Code workflow |
| **claude-md-management** | CLAUDE.md maintenance | Keeping project instructions current |
| **dx** | GitHub Actions analysis | CI failure diagnosis |

### CLI Tools

| Tool | Version | Purpose |
|------|---------|---------|
| **Claude Code** | 2.1.x (auto-updates) | Primary AI coding agent |
| **Gemini CLI** | 0.34.0 | Google AI — web search, second opinions |
| **Codex CLI** | 0.116.0 | OpenAI AI — code review, alternative perspectives |
| **GSD** | 1.27.0 | Get Shit Done — project management framework |
| **.NET SDK** | 10.0.104 | C# compilation and tooling |
| **csharp-ls** | 0.22.0 | C# language server for IDE diagnostics |
| **uv** | 0.10.12 | Fast Python package manager (runs vb-blender/vb-unity MCPs) |
| **Node.js** | 25.8.0 | JavaScript runtime (MCP servers, hooks) |

### Tool Selection Guide

| Task | Primary Tool | Backup |
|------|-------------|--------|
| Understand unfamiliar C# code | Serena `find_symbol` + `get_symbols_overview` | Read tool |
| Find where method is called | Serena `find_referencing_symbols` | Grep tool |
| Quick code search | Grep tool directly | Serena `search_for_pattern` |
| Unity API question | Context7 `query-docs` | Gemini web search |
| Balance/math verification | Sequential-thinking | Gemini review |
| Code review | Semgrep (auto) + code-review skill | Gemini + Codex CLIs |
| UI layout design | frontend-design skill + mockup-ui | ASCII mockup first |
| New feature design | brainstorming skill → writing-plans | Gemini collaboration |
| Complex debugging | systematic-debugging skill | Gemini second opinion |
| Scene/lighting setup | vb-unity `unity_scene` | Manual C# scripts |
| 3D model work | vb-blender tools | Blender direct |
| Git operations | commit-commands skill | gh CLI |

---

## Technical Debt Clearance (2026-02-01)

### Unity Gotchas
- **NEVER** create files named `nul`, `con`, `prn`, `aux`, `com1-9`, `lpt1-9` (Windows reserved - causes infinite import loop)
- **NEVER** reference fonts that don't exist in project (UI disappears)
- **NEVER** use `Find()` or `FindObjectOfType()` in Update - cache references
- **NEVER** allocate in Update (no `new`, no LINQ, no string concat)
- Delete `Library/` folder to fix import loops (Unity rebuilds it)
- **CHECK** if Bootstrap/Manager components are ENABLED in scene (`m_Enabled: 1`) - disabled components = silent failure
- **AFTER deleting scripts**, check scene for orphan GameObjects with missing script references (causes NullReferenceException on UXML live reload)

### UI Toolkit Lessons
- Scrollbar positioning buggy in programmatic popups - hide scrollbar, use mouse wheel
- UXML files need valid asset GUIDs, not placeholders
- Set `alphaIsTransparency: 1` in texture .meta files for transparency
- Set `overflow: visible` on particle containers (not hidden)
- **C# inline styles override USS** - `VBDropdownField.cs` uses hardcoded colors; theme changes need BOTH USS + C# updates
- Title screen VFX should use readability masks to protect logo + demon focal area
- UI RawImage/Graphic materials must expose `_MainTex` to avoid runtime error logs during PlayMode tests

### Save System
- Use `Path.ChangeExtension()` not `.Replace()` for file paths
- Add timeout to mutex operations (prevent silent failures)

### Namespace Conflicts
- `System.Diagnostics.Debug` vs `UnityEngine.Debug` - Use fully qualified `UnityEngine.Debug.Log()` when `[Conditional]` attributes import System.Diagnostics
- `VeilBreakers.Data.Path` vs `System.IO.Path` - Use alias `using IOPath = System.IO.Path;` then `IOPath.Combine()`

---

## User Preferences

- Visual verification with screenshots
- Commit on logical completion (not arbitrary time intervals)
- AAA quality, pixel-perfect alignment
- Fresh Unity UI (don't port Godot patterns)
- **Use Sonnet** for primary work, research tasks, and agent spawns (Opus wastes tokens)
- **Art Style:** Grimdark Painterly / Dark Fantasy Stylized Realism
- Keep only the OneDrive canonical project + one local backup; archive older copies
- Git hook runs `Tools/sync_backup.bat` on every commit (core.hooksPath = .githooks, uses cygpath)
- Pre-commit hook blocks commit if backup sync fails
- **Claude+Gemini hybrid approach:** Use Gemini for second opinions, research, adversarial review (v4.43)

---

## Technical Pipelines

### Render Pipeline Upgrade
| Current | Target | When |
|---------|--------|------|
| Built-in (2022.3 LTS) | **URP** (Unity 6) | Before heavy VFX work |

### Asset Pipeline
```
Scenario (2D concept) → Tripo (2D→3D + auto-rig) → Cascadeur (physics animation) → Blender (polish) → Unity
```

### VFX Pipeline
- **Primary:** Unity VFX Graph (requires URP)
- **AI Assist:** God Mode AI
- **Docs:** `Docs/VFX_AI_TOOLS_2026.md`

### Map/Environment Pipeline
- **Terrain:** Gaea (FREE→$99 Indie) for 8K heightmaps
- **In-Engine:** MapMagic 2 (FREE) for procedural refinement
- **Props/Landmarks:** Scenario→Tripo (your existing pipeline)
- **Atmosphere:** Unity VFX Graph + URP fog/lighting
- **Docs:** `Docs/MAP_TERRAIN_AI_TOOLS_2026.md`

### 6 Biomes (Light → Red Veil Progression)

| # | Biome | Path | Brands | Theme |
|---|-------|------|--------|-------|
| 1 | **The Waking Shore** | Start | Mixed | Last light, fading hope |
| 2 | **The Ironmaw** | IRONBOUND | IRON, MEND, LEECH | Rusted fortresses, parasitic growth |
| 3 | **The Ravaged** | FANGBORN | SAVAGE, VENOM, RUIN | Primal carnage, toxic wastelands |
| 4 | **The Unbound** | UNCHAINED | GRACE + flex | Contested twilight, unstable |
| 5 | **The Hollowing** | VOIDTOUCHED | VOID, DREAD, SURGE | Reality tears, nightmare realm |
| 6 | **The Bleeding Veil** | Endgame | UNTAMED | Pure red veil energy, max corruption |

**Visual Progression:** Red veil energy intensifies from subtle hints (Biome 1) to overwhelming crimson (Biome 6)

---

## Design Documents

| Document | Location |
|----------|----------|
| Combat System | Docs/plans/2026-01-15-combat-system-design.md |
| Combat Implementation | Docs/plans/2026-01-17-combat-implementation-plan.md |
| Combat UI | Docs/plans/2026-01-17-combat-ui-design.md |
| MCP Arsenal | Docs/plans/2026-01-17-mcp-arsenal-design.md |
| Rigging/Animation | Docs/plans/2026-01-17-rigging-animation-facial-design.md |
| Gambits AI | Docs/plans/2026-01-18-gambits-ai-design.md |
| Status Effects | Docs/plans/2026-01-18-status-effects-design.md |
| Quick Commands | Docs/plans/2026-01-18-quick-command-design.md |
| Monster Capture | Docs/plans/2026-01-18-monster-capture-design.md |
| Save/Load | Docs/plans/2026-01-19-save-load-system-design.md |
| Audio System | Docs/plans/2026-01-19-audio-system-design.md |
| Hero Design | Docs/plans/2026-01-19-hero-character-design.md |
| Implementation | Docs/plans/2026-01-19-implementation-strategy.md |
| Migration | Docs/MIGRATION_PLAN.md |
| Character Select (v1) | Docs/plans/2026-01-27-character-select-design.md |
| Character Select (v2 BG3) | Docs/plans/2026-02-05-character-select-redesign.md |
| Monster Redesign v2 | Docs/MonsterRedesign_Specification_v2.md |
| Monster Skills v3 | Docs/MonsterSkill_Specification_v3.md |
| Git Workflow | Docs/plans/GIT_WORKFLOW_AAA.md |
| Unity 6 Prep | Docs/UNITY6_PREP.md |
| Unity 6 Migration | Docs/plans/2026-02-01-unity6-migration.md |

---

## Current Status

- **UI System:** Complete (all 6 screens) - Main Menu + Character Select at AAA quality
- **Core Systems:** Implemented (Brand, Synergy, Corruption, EventBus, InputManager)
- **Combat:** Framework ready, needs 3D integration
- **Main Menu:** AAA - video bg, lightning, embers/ash/sparks VFX, molten buttons, music
- **Character Select:** BG3-inspired cinematic redesign (5-component system, 4 heroes + mystery slot)
- **Monster Redesign:** v2 specifications complete, v1 data archived, implementation pending
- **Migration:** 100% complete per `Docs/MIGRATION_PLAN.md`
- **Unity 6 Prep:** ✅ COMPLETE - Running on Unity 6.3
- **CI/CD:** GitHub Actions Unity CI on PRs
- **Git Workflow:** Formalized (master/develop/feature branches)
- **Next:** 3D model integration, combat system 3D, monster redesign implementation

---

---

## Configuration Lessons (v4.43)

### Claude Code Optimization
- **Duplicate MCPs waste context:** Plugin-provided MCPs (Serena, Context7, Greptile) don't need local duplicates
- **Rigid mandates hurt reasoning:** "1% = MUST invoke" rules consume context and reduce flexibility
- **Lean CLAUDE.md is better:** 150 lines of flexible principles > 650 lines of rigid rules
- **Claude+Gemini hybrid:** Use Gemini for fresh perspective, web research, adversarial review

### What Was Removed (v4.43)
- Duplicate MCPs: serena, context7, greptile (plugins provide these)
- Unused MCPs: figma, figma-local, atlassian
- Rigid time-based commits (now commit on completion)
- "NO EXCEPTIONS" / "MANDATORY" language in CLAUDE.md

*Memory optimized v4.0 - Reduced from 1300+ lines to essential reference*
