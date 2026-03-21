# VEILBREAKERS 3D - CLAUDE CONFIGURATION

## Mission
Build an AAA-quality 3D monster RPG using Unity. Quality over speed, but don't overthink simple tasks.

**Engine:** Unity 3D (UI Toolkit)
**Memory:** `VEILBREAKERS.md` (read at session start)
**Migration:** `Docs/MIGRATION_PLAN.md`

---

# CORE PRINCIPLES (Not Rules)

## 1. Trust Your Reasoning
- Use tools when they genuinely help, not because a rule says so
- Simple questions get simple answers
- Complex tasks benefit from structured approaches
- You decide what's appropriate for each situation

## 2. Context Efficiency
- Don't read entire files when you only need a symbol
- Use Serena for semantic code navigation (it saves tokens)
- Ask Gemini for second opinions on complex decisions
- Keep responses focused and concise

## 3. Reasoning Budget (Power Without Bloat)
- **Default mode:** 2-pass reasoning
  - Pass 1: quick hypothesis from local code/context
  - Pass 2: targeted verification (tests/logs/docs) before final claims
- **Deep mode (only when needed):** use `sequential-thinking` if any are true:
  - High-risk change (data loss, save format, core combat/math)
  - 3+ interacting systems
  - Repro is unclear after one focused debug pass
- **Token guardrails:**
  - Prefer symbol-level/file-slice reads over full-file dumps
  - Use at most one external “second-opinion” round (Gemini) per decision
  - Summarize findings before continuing broad exploration
- **Stop conditions:** when confidence is high and tests/validation pass, ship instead of over-analyzing

## 4. Commit When It Makes Sense
- Commit after completing logical units of work
- Don't interrupt mid-task for arbitrary time-based commits
- Version updates in VEILBREAKERS.md track progress

---

# TOOL GUIDANCE (Smart Judgment)

## Serena - Use Judgment, Not Defaults

Serena is powerful but **not always needed**. Use your judgment:

| Task | Use Serena? | Instead |
|------|-------------|---------|
| Understand unfamiliar file structure | YES - `get_symbols_overview` | - |
| Find where a method is called | YES - `find_referencing_symbols` | - |
| Refactor a symbol | YES - `replace_symbol_body` | - |
| Read a file you know the path to | **NO** | Just use `Read` tool |
| Quick text search | **NO** | Just use `Grep` tool |
| Small edit to known code | **NO** | Just use `Edit` tool |
| Navigate to line you already know | **NO** | Just use `Read` tool |

**Serena saves tokens when:** You don't know what's in a file, or need semantic understanding.
**Serena wastes tokens when:** You already know what you need and where it is.

## Superpowers Skills - Use When Valuable

| Skill | USE for | SKIP for |
|-------|---------|----------|
| brainstorming | New systems, unclear requirements | Simple additions, bug fixes |
| writing-plans | Multi-file implementations | Single-file changes |
| systematic-debugging | Complex/mysterious bugs | Obvious errors |
| verification-before-completion | Major changes, PRs | Quick fixes |

**Default:** Skip skills for simple tasks. Use skills when complexity justifies structure.

## VB-Toolkit (PREFER for game dev tasks)

The VeilBreakers GameDev Toolkit has **37 compound tools with 330+ actions**. Use these FIRST for any game development task — they generate production-quality C# scripts, Blender operations, and asset pipeline automation tailored to VeilBreakers.

### VB-Unity (22 tools — generates C# scripts to `Assets/Editor/Generated/`)

| When you need to... | Use this tool | Action |
|---------------------|---------------|--------|
| **Create a monster AI** | `unity_gameplay` | `create_mob_controller` (FSM: Patrol/Chase/Attack/Flee) |
| **Create a boss** | `unity_gameplay` | `create_boss_ai` (multi-phase hierarchical FSM) |
| **Set up combat** | `unity_game` | `create_player_combat`, `create_ability_system`, `create_synergy_engine` |
| **Brand damage system** | `unity_game` | `create_damage_types` (all 10 brands pre-configured) |
| **Corruption mechanics** | `unity_game` | `create_corruption_gameplay` (0-100% with tier thresholds) |
| **VFX for brands** | `unity_vfx` | `create_brand_vfx` (unique VFX per brand) |
| **Particle effects** | `unity_vfx` | `create_particle_vfx`, `create_environmental_vfx`, `create_aura_vfx` |
| **Shaders** | `unity_vfx` | `create_shader` (dissolve/force field/water/foliage/outline/damage) |
| **Post-processing** | `unity_vfx` | `setup_post_processing` (bloom/vignette/AO/DOF) |
| **UI screens** | `unity_ui` | `generate_ui_screen` (UXML+USS with dark fantasy theming) |
| **Damage numbers** | `unity_ux` | `create_damage_numbers` (PrimeTween + ObjectPool) |
| **Minimap** | `unity_ux` | `create_minimap` (orthographic camera + markers) |
| **Character select** | `unity_ux` | `create_character_select` (hero path carousel) |
| **Inventory** | `unity_content` | `create_inventory_system` (grid + equipment + UI) |
| **Dialogue** | `unity_content` | `create_dialogue_system` (branching, YarnSpinner-compatible) |
| **Quests** | `unity_content` | `create_quest_system` (state machine + tracker + UI) |
| **Loot tables** | `unity_content` | `create_loot_table` (weighted with brand affinity) |
| **Save system** | `unity_game` | `create_save_system` (JSON + AES encryption + migration) |
| **Scene lighting** | `unity_scene` | `setup_lighting` (time-of-day presets) |
| **NavMesh** | `unity_scene` | `bake_navmesh` (agent settings) |
| **Animator** | `unity_scene` | `create_animator` (states/transitions/blend trees) |
| **Terrain** | `unity_scene` | `setup_terrain` (from heightmap + splatmaps) |
| **Camera system** | `unity_camera` | `create_virtual_camera` (Cinemachine 3.x) |
| **Cutscenes** | `unity_camera` | `create_cutscene` (PlayableDirector) |
| **Prefabs** | `unity_prefab` | `create` (auto-wire: hero/monster/prop/ui profiles) |
| **C# classes** | `unity_code` | `generate_class` (any: MonoBehaviour, SO, interface, enum) |
| **State machine** | `unity_code` | `state_machine` (IState/StateMachine/BaseState) |
| **Event system** | `unity_code` | `event_channel` (SO event channels) |
| **Object pool** | `unity_code` | `object_pool` (ObjectPool<T>) |
| **FBX import** | `unity_assets` | `configure_fbx`, `remap_materials`, `auto_materials` |
| **Code quality** | `unity_qa` | `analyze_code` (anti-pattern detection, no Unity needed) |
| **Performance** | `unity_performance` | `profile_scene`, `audit_assets` |
| **AAA audit** | `unity_quality` | `aaa_audit` (combined quality check) |
| **Build** | `unity_build` | `build_multi_platform` (6 platforms + IL2CPP) |
| **Weather/day-night** | `unity_world` | `create_weather`, `create_day_night` |

### VB-Blender (15 tools — requires Blender running with addon)

| When you need to... | Use this tool | Action |
|---------------------|---------------|--------|
| **Generate 3D model from text** | `asset_pipeline` | `generate_3d` (Tripo3D API) |
| **Clean up AI model** | `asset_pipeline` | `cleanup` (repair→UV→PBR pipeline) |
| **Generate LODs** | `asset_pipeline` | `generate_lods` (LOD0-LOD3 decimation) |
| **Generate weapons** | `asset_pipeline` | `generate_weapon` (parametric mesh) |
| **Split character for equipment** | `asset_pipeline` | `split_character` (modular parts) |
| **Concept art** | `concept_art` | `generate` (fal.ai FLUX, needs FAL_KEY) |
| **Color palette** | `concept_art` | `extract_palette` (from image) |
| **Rig a creature** | `blender_rig` | `apply_template` (humanoid/quadruped/bird/etc.) |
| **Auto weight paint** | `blender_rig` | `auto_weight` |
| **Walk/run cycle** | `blender_animation` | `generate_walk` (biped/quadruped/hexapod/serpent) |
| **Attack animations** | `blender_animation` | `generate_attack` (8 types) |
| **Idle/death/spawn** | `blender_animation` | `generate_idle`, `generate_reaction` |
| **Batch export FBX** | `blender_animation` | `batch_export` (Unity-ready clips) |
| **Generate terrain** | `blender_environment` | `generate_terrain` (noise + erosion) |
| **Scatter vegetation** | `blender_environment` | `scatter_vegetation` (biome-aware Poisson) |
| **Generate dungeon** | `blender_worldbuilding` | `generate_dungeon` (BSP rooms + corridors) |
| **Generate town** | `blender_worldbuilding` | `generate_town` (Voronoi districts) |
| **Generate castle** | `blender_worldbuilding` | `generate_castle` (walls/towers/keep) |
| **Boss arena** | `blender_worldbuilding` | `generate_boss_arena` (cover/hazards/phases) |
| **UV unwrap** | `blender_uv` | `unwrap` (xatlas high-quality) |
| **Texture bake** | `blender_texture` | `bake` (normal/AO/combined maps) |
| **Mesh analysis** | `blender_mesh` | `analyze` (topology grading A-F) |
| **Retopology** | `blender_mesh` | `retopo` (Quadriflow) |
| **Export for Unity** | `blender_export` | FBX with Unity-compatible settings |

### VeilBreakers-Specific Pipelines

**Monster Creation:** `concept_art generate` → `asset_pipeline generate_3d` → `asset_pipeline cleanup` → `blender_rig apply_template` → `blender_animation generate_walk/attack/idle` → `blender_animation batch_export` → `unity_assets configure_fbx` → `unity_prefab create` (monster profile) → `unity_gameplay create_mob_controller` → `unity_vfx create_brand_vfx`

**Level Building:** `blender_environment generate_terrain` → `blender_environment paint_terrain` → `blender_worldbuilding generate_dungeon` → `blender_environment export_heightmap` → `unity_scene setup_terrain` → `unity_scene scatter_objects` → `unity_scene setup_lighting` → `unity_scene bake_navmesh`

**Combat System:** `unity_game create_damage_types` → `unity_game create_player_combat` → `unity_game create_ability_system` → `unity_game create_synergy_engine` → `unity_game create_corruption_gameplay` → `unity_content create_loot_table` → `unity_content create_skill_tree`

**Note:** 2 stubs exist: `blender_texture inpaint` and `blender_animation generate_ai_motion`. ElevenLabs/Gemini/Tripo/fal.ai gracefully degrade to stubs when API keys are missing.

## Other Tools

| Situation | Recommended Tool | Why |
|-----------|------------------|-----|
| Unity API questions | Context7 `query-docs` | Up-to-date documentation |
| Complex analysis | `sequential-thinking` | Structured breakdown |
| Second opinion | `gemini-cli` or `codex-cli` MCP | Different AI perspectives |
| C# code intelligence | csharp-lsp plugin | Real-time diagnostics |
| Security scanning | Semgrep plugin (auto) | Catches issues on Edit/Write |
| Frontend/UI design | frontend-design plugin | Production-grade UI generation |

### MCP Loading Strategy
- Default to **core MCP profile** (`.mcp.json`) for best reasoning signal-to-noise.
- Use **full MCP profile** (`.mcp.full.json`) for image-process/notion sessions.

## Workflows (Use When Helpful)

### For Complex Features
Consider the brainstorm → plan → execute workflow for:
- New game systems (combat, capture, AI)
- Multi-file refactors
- Architecture decisions

Skip it for:
- Bug fixes
- Simple additions
- Quick questions

### For UI Work
ASCII mockups before code can help:
- Clarify layout with user
- Catch issues early
- But skip for trivial changes

---

# PROJECT CONTEXT

## Key Systems (Don't Break These)

### 10-Brand Combat
IRON, SAVAGE, SURGE, VENOM, DREAD, LEECH, GRACE, MEND, RUIN, VOID
- Each: 2x to 2 brands, 0.5x to 2 brands, 1x to 6 brands

### 4 Paths
IRONBOUND, FANGBORN, VOIDTOUCHED, UNCHAINED

### Corruption (0-100%)
0-10% ASCENDED (+25%), 11-25% Purified (+10%), 26-50% Unstable, 51-75% Corrupted (-10%), 76-100% Abyssal (-20%)

### Synergy Tiers
FULL (3/3): +8% damage/defense | PARTIAL (2/3): +5% | NEUTRAL: +0% | ANTI: +0%

## Code Style

```csharp
namespace VeilBreakers.[Category]
{
    public class Example : MonoBehaviour
    {
        private const int kMaxValue = 10;      // Constants: k prefix
        [SerializeField] private int _value;   // Private: _ prefix
        public int Value => _value;            // Properties: PascalCase
        public event Action<int> OnChanged;    // Events: On prefix
    }
}
```

## Project Structure
- Scripts: `Assets/Scripts/[Combat|Core|Systems|UI|Data]/`
- Art: `Assets/Art/`
- Docs: `Docs/`
- Screenshots: `screenshots/`

---

# CLAUDE + GEMINI HYBRID APPROACH

## When to Use Gemini

| Situation | Action |
|-----------|--------|
| Need second opinion on architecture | `gemini -p "Analyze this design..."` |
| Complex debugging stuck | Get Gemini's perspective |
| Research/web search | Gemini has web access |
| Balance validation | Ask Gemini to review calculations |
| Code review | Cross-check with Gemini |

## Gemini Strengths to Leverage
- Web search and current information
- Alternative reasoning approaches
- Code analysis from different angle
- Validation of complex logic

## Claude Strengths to Use
- Deep codebase understanding via Serena
- Unity/C# expertise via Context7
- Direct tool execution
- Conversation continuity

---

# HIGH-RISK CHANGES (Ask User First)

- Brand/Path system design changes
- Save file format modifications
- Core class renames/removals
- Major architectural changes
- Deleting files (archive instead)

---

# LESSONS LEARNED

## Don't Repeat
- Windows reserved filenames (nul, con, aux)
- `Find()` in Update loops
- Allocations in Update
- Missing font references (UI disappears)
- Disabled components = silent failures

## What Works
- ScriptableObjects for game data
- Event-driven architecture
- Serena for semantic code ops
- Visual verification via Unity screenshots

---

# PHILOSOPHY

1. **Reasoning over rules** - Think, don't just follow checklists
2. **Tools serve you** - Use them when helpful, not because mandated
3. **Quality matters** - But don't overthink simple things
4. **User decides** - When in doubt, ask
5. **Gemini partnership** - Two AIs are better than one

---

# PRIORITY PATH: CHARACTER SELECT REBUILD

## Execution Protocol (MANDATORY)

### Model Roles
- **Opus (Claude Opus 4.6):** Head Software Engineer. Signs off on ALL phases. Handles implementation when Sonnet gets stuck. Performs testing, security review, and code strength verification.
- **Sonnet (Claude Sonnet 4.6):** Implementation engineer. Writes code phase-by-phase. Escalates to Opus when blocked.
- **Gemini CLI:** Senior reviewer. Validates each phase output. (MCP: `mcp__gemini-cli__chat`)
- **Codex CLI:** Senior reviewer. Validates each phase output. (MCP: `mcp__codex-cli__chat`)

### Phase Execution Flow
```
For each phase:
  1. Sonnet implements the phase code
  2. Sonnet runs Unity compilation check
  3. Opus reviews ALL code for:
     - Code strength (SOLID, patterns, no god-classes)
     - Security (no injection, no unsafe patterns)
     - Performance (no Update allocations, cached queries)
     - Architecture (event lifecycle, proper teardown)
  4. Gemini + Codex validate via MCP or bash CLI
  5. Opus gives FINAL SIGN-OFF
  6. On approval:
     a. Save to episodic memory (conversation search)
     b. Save to local memory (Serena/AIM)
     c. Commit to feature branch
     d. Merge to develop branch
  7. Move to next phase
```

### Git Management Framework (MANDATORY)

**Branch Structure:**
- `master` -- production truth, always deployable
- `develop` -- integration branch, mirrors master after each feature merge
- `feature/<name>` -- feature branches (created from master, merged back to master)
- `feature/cs-phase-N` -- per-phase branches (optional, for large phases)

**Rules (Non-Negotiable):**
1. **Never commit directly to master in isolation** -- all branches must be synced after every commit
2. **After EVERY commit**, run: `git branch -f develop master && git branch -f feature/<active> master`
3. **After EVERY push**, push ALL active branches: `git push origin master develop feature/<active>`
4. **Delete stale branches immediately** -- no orphan branches sitting around
5. **Clean up remote branches** that are merged or abandoned
6. **Verify branch alignment** before ending any session: all active branches must point to same commit

**Session Start Checklist:**
```bash
# 1. Verify clean state
git status -s
# 2. Verify branch alignment
git branch -v
# 3. If misaligned, fix immediately with git branch -f
```

**Session End Checklist:**
```bash
# 1. Commit all pending work
# 2. Sync all branches to master
git branch -f develop master
git branch -f feature/<active> master
# 3. Push everything
git push origin master develop feature/<active>
# 4. Verify clean
git status -s  # must be empty
git branch -v  # all active branches same commit
```

**Stale Branch Cleanup:**
- Old feature branches that are fully merged → delete local + remote
- Phase branches after phase completion → delete
- Claude review branches → delete after review complete

### Escalation Rule
If Sonnet fails on a task after 2 attempts, Opus takes over implementation immediately. No negotiation.

### Testing Requirements Per Phase
- Unity compilation: MUST pass (zero errors)
- Code review: Opus sign-off required
- External review: Both CLI reviewers (Gemini + Codex) should approve
- Security scan: Semgrep plugin auto-scans on Edit/Write; no hardcoded secrets, no unsafe deserialization, no injection vectors
- Performance check: No allocations in hot paths, cached references, proper disposal
- C# diagnostics: csharp-lsp plugin provides real-time error detection

### Memory Protocol Per Phase
After each phase approval:
1. `episodic-memory` -- save phase completion with key decisions
2. `aim_memory_store` -- save to project context
3. `serena write_memory` -- save implementation notes
4. `git commit` -- commit with descriptive message
5. `git merge` -- merge phase branch into feature branch

---

*Configuration v5.2 - Priority Path: Character Select Rebuild Protocol*
