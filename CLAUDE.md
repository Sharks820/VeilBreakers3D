# VEILBREAKERS 3D - UNITY PROJECT

## Mission: BUILD AN AAA GAME STUDIO. NO COMPROMISES. NO RIVALS.

**The Vision:** Claude + Unity = Unstoppable game development. We're not just making a game, we're building a studio.

**Engine:** Unity (migrated from Godot)
**Project Path:** `C:/Users/Conner/Downloads/VeilBreakers3D`
**Migration Status:** See `Docs/MIGRATION_PLAN.md` for detailed progress

---

# CRITICAL: MANDATORY RULES (READ FIRST)

## THE THREE ABSOLUTES - NEVER SKIP

### 1. SAVE MEMORIES EVERY 15 MINUTES
- Update `VEILBREAKERS.md` with any new decisions, values, or lessons
- This is the SINGLE SOURCE OF TRUTH across sessions
- If you learned something, WRITE IT DOWN

### 2. COMMIT EVERY 15 MINUTES
- `git add -A && git commit -m "descriptive message" && git push`
- NO EXCEPTIONS. Losing work is UNACCEPTABLE.
- Increment version in VEILBREAKERS.md header before each commit

### 3. ORGANIZE FILES INTO CORRECT GIT LOCATIONS
- EVERY file goes to its designated folder
- EVERY commit goes to the correct branch
- NEVER dump files randomly

---

# MANDATORY: SESSION PROTOCOLS

## 1. Memory Protocol
**EVERY SESSION MUST:**
1. **READ `VEILBREAKERS.md`** at the start of every conversation
2. **READ `Docs/MIGRATION_PLAN.md`** to know current migration status
3. **ACKNOWLEDGE** current project state before taking any action
4. **UPDATE** both files when making significant changes

> **VEILBREAKERS.md is THE SINGLE SOURCE OF TRUTH for cross-session memory.**

## 2. Auto-Save Protocol (EVERY 15 MINUTES - NO EXCEPTIONS)

**COMMIT AND PUSH EVERY 15 MINUTES. PERIOD.**

1. Increment version in VEILBREAKERS.md header (v1.44 → v1.45...)
2. `git add -A`
3. `git commit -m "v1.X: [brief description]"`
4. `git push`

### Commit Message Rules
- **NO** "Generated with Claude Code" tags
- **NO** "Co-Authored-By: Claude" tags
- **NO** mentions of Claude or AI in commits

## 3. File Naming Rules (MANDATORY)

**NEVER create files with Windows reserved names:**
- `NUL`, `CON`, `PRN`, `AUX`, `COM1`-`COM9`, `LPT1`-`LPT9`

## 4. Screenshot Protocol (MANDATORY)

**ALL screenshots MUST go to: `screenshots/`**

## 5. Migration Tracking Protocol (MANDATORY)

**CHECK `Docs/MIGRATION_PLAN.md` BEFORE ANY WORK**

When completing migration tasks:
1. Update task status (❌ → ✅) in MIGRATION_PLAN.md
2. Recalculate category percentage
3. Recalculate overall percentage
4. Update "Last Updated" date

**Migration is complete when overall = 100%**

## 6. High-Risk Items (MUST ASK USER)

- Change Brand/Path system design
- Modify save file format
- Remove or rename core classes
- Change corruption philosophy
- Major UI flow changes
- Game function/story/big script changes
- Delete ANY file (archive only, never delete)

## 7. Git Organization Protocol (MANDATORY)

### Branch Naming Convention
| Prefix | Purpose | Example |
|--------|---------|---------|
| `feature/` | New features | `feature/battle-system` |
| `bugfix/` | Bug fixes | `bugfix/hp-display` |
| `docs/` | Documentation only | `docs/api-reference` |
| `hotfix/` | Urgent production fixes | `hotfix/crash-on-load` |
| `refactor/` | Code cleanup | `refactor/manager-classes` |

### File Organization Rules
| File Type | Location | Branch |
|-----------|----------|--------|
| C# Scripts | `Assets/Scripts/[category]/` | `feature/*` or `bugfix/*` |
| Documentation | `Docs/` | `docs/*` or current feature branch |
| Art Assets | `Assets/Art/[category]/` | `feature/*` |
| Prefabs | `Assets/Prefabs/[category]/` | `feature/*` |
| Scenes | `Assets/Scenes/` | `feature/*` |
| Config/Data | `Assets/Data/` | `feature/*` |

## 8. Serena Code Intelligence Protocol (MANDATORY)

**USE SERENA FOR ALL CODE OPERATIONS TO SAVE TOKENS**

| Task | DON'T DO THIS | DO THIS INSTEAD |
|------|---------------|-----------------|
| Understand file structure | `Read` entire file | `get_symbols_overview` |
| Find class/method | `Grep "class Foo"` | `find_symbol("Foo")` |
| Find where used | Multiple `Grep` queries | `find_referencing_symbols` |
| Replace function | `Read` + `Edit` | `replace_symbol_body` |
| Rename across codebase | Find/replace | `rename_symbol` |

### Serena Workflow
1. **First contact with file** → `get_symbols_overview`
2. **Need specific symbol** → `find_symbol` with `include_body=true`
3. **Need to understand usage** → `find_referencing_symbols`
4. **Need to edit** → `replace_symbol_body` or `replace_content`

### Project Activation (Session Start)
```
mcp__plugin_serena_serena__activate_project("VeilBreakers3D")
```

## 9. Superpowers Workflow Protocol (MANDATORY)

**USE SUPERPOWERS SKILLS FOR ALL PLANNING AND EXECUTION**

### The Three-Phase Workflow
```
BRAINSTORM → WRITE PLAN → EXECUTE PLAN
```

| Phase | Skill | When |
|-------|-------|------|
| 1. Brainstorm | `superpowers:brainstorming` | Before ANY creative work |
| 2. Write Plan | `superpowers:writing-plans` | After brainstorm approved |
| 3. Execute | `superpowers:executing-plans` | After plan approved |

### When Required
| Trigger | Required? |
|---------|-----------|
| "Add a feature" | YES - Full 3-phase |
| "Implement X system" | YES - Full 3-phase |
| "Fix this bug" | MAYBE - If complex |
| "Quick question" | NO |

---

# 10. COMPREHENSIVE TOOL PROTOCOLS (MANDATORY)

**EVERY TOOL MUST BE USED FOR ITS INTENDED PURPOSE**

## Active Plugins (17 Total)

### CODE INTELLIGENCE
| Plugin | Trigger | Usage |
|--------|---------|-------|
| **serena** | ANY code operation | Symbols, refs, edits - see Protocol 8 |
| **context7** | BEFORE writing Unity/C# code | Query Unity API docs first |
| **csharp-lsp** | AFTER code changes | Run diagnostics, catch errors |
| **greptile** | "Where is X used?" | Cross-repo semantic search |

### WORKFLOW
| Plugin | Trigger | Usage |
|--------|---------|-------|
| **superpowers** | ANY feature/implementation | 3-phase workflow - see Protocol 9 |
| **superpowers-lab** | Code audits | `finding-duplicate-functions` skill |
| **feature-dev** | Complex multi-file features | Architecture-first development |
| **double-shot-latte** | Auto-enabled | Prevents "continue?" interruptions |

### CODE QUALITY
| Plugin | Trigger | Usage |
|--------|---------|-------|
| **code-review** | Before merges/PRs | Structured code review |
| **pr-review-toolkit** | PR creation/review | Multi-agent specialized review |
| **security-guidance** | Network/save code | Security vulnerability check |

### GIT & COMMITS
| Plugin | Trigger | Usage |
|--------|---------|-------|
| **commit-commands** | Git operations | `/commit`, `/clean_gone` |

### MEMORY & CONTEXT
| Plugin | Trigger | Usage |
|--------|---------|-------|
| **episodic-memory** | SESSION START | Search past conversations |

### CONTENT & DESIGN
| Plugin | Trigger | Usage |
|--------|---------|-------|
| **frontend-design** | UI/UX design | Design patterns for Unity UI |
| **elements-of-style** | Documentation | Clear writing for docs, commits |
| **superpowers-chrome** | Web research | Documentation browsing |

### DEVELOPMENT
| Plugin | Trigger | Usage |
|--------|---------|-------|
| **agent-sdk-dev** | AI-powered features | Build Claude agents for VERA/testing |

## Active MCP Servers (8 Local + 5 Plugin-Provided)

### CRITICAL: MCP USAGE RULES - NO TOOL SITS STAGNANT

**EVERY MCP MUST BE USED WHEN ITS TRIGGER CONDITION IS MET. NO EXCEPTIONS.**

If you catch yourself NOT using an MCP when its trigger applies, STOP and USE IT.

### Local MCPs (.mcp.json) - 8 TOTAL

#### CORE DEVELOPMENT (Always Available)
| MCP | Trigger Keywords | MANDATORY Usage |
|-----|------------------|-----------------|
| **sequential-thinking** | "ultrathink", "complex", "analyze", "balance", "design system" | Break down ANY multi-step problem. Use for game balance, architecture decisions, debugging complex issues |
| **mcp-unity** | "Unity", "scene", "GameObject", "build", "compile", "screenshot", "console" | Control Unity Editor directly. Take screenshots, check compile errors, manipulate scenes, run builds |
| **github** | "PR", "pull request", "issue", "commit", "CI", "merge" | All GitHub operations - create PRs, check CI status, manage issues |

#### ASSET CREATION (Use Proactively)
| MCP | Trigger Keywords | MANDATORY Usage |
|-----|------------------|-----------------|
| **mcp-hfspace** | "generate image", "create sprite", "monster art", "UI art", "concept art" | AI image generation via FLUX. Use for ALL 2D art needs - monsters, UI, concepts |
| **blender** | "3D model", "mesh", "render", "Blender", "sculpt", "material", "texture 3D" | Create/edit 3D models directly in Blender. Use for converting 2D art to 3D |
| **image-process** | "crop", "resize", "rotate", "convert format", "sprite sheet" | Process existing images - resize for Unity, crop sprites, convert formats |

#### AUDIO & VOICE (FREE Stack)
| MCP | Trigger Keywords | MANDATORY Usage |
|-----|------------------|-----------------|
| **fish-audio** | "voice", "VERA voice", "dialogue", "narration", "character voice" | Generate voice lines via Fish Audio (FREE tier, #1 TTS quality). USE FOR ALL VOICE |

**Note:** Music and SFX have no MCP - use directly:
- **Music:** [Udio](https://udio.com) - 1,200 free songs/month, commercial OK
- **SFX:** [SFX Engine](https://sfxengine.com) - unlimited free, commercial OK

#### PROJECT MANAGEMENT (Use for Tracking & Documentation)
| MCP | Trigger Keywords | MANDATORY Usage |
|-----|------------------|-----------------|
| **notion** | "track", "backlog", "database", "document", "PRD", "spec", "game bible", "monster list", "feature list" | Read/write Notion pages for project management. Track monsters, features, design docs. USE INSTEAD OF JUST MEMORY |

### Plugin-Provided MCPs (Auto-loaded)
| MCP | Trigger | Usage |
|-----|---------|-------|
| **Context7** | Unity/C# API questions | Query up-to-date documentation BEFORE writing code |
| **Serena** | ANY code operation | Semantic code intelligence - see Protocol 8 |
| **Greptile** | Cross-repo search, PR reviews | Find code patterns across repos |
| **Episodic Memory** | SESSION START, "remember", "past conversation" | Search conversation history for context |
| **Chrome** | Web research, documentation | Browse web when MCPs don't have info |

---

## MCP DECISION TREE - FOLLOW THIS EXACTLY

```
USER REQUEST RECEIVED
        │
        ▼
┌───────────────────────────────────────────────────────────┐
│ STEP 1: CHECK FOR MCP TRIGGERS                            │
│                                                           │
│ Does request mention ANY of these?                        │
│ • "3D model/mesh/Blender" → USE blender MCP               │
│ • "voice/dialogue/VERA" → USE fish-audio MCP              │
│ • "generate image/sprite/art" → USE mcp-hfspace           │
│ • "Unity/scene/build/compile" → USE mcp-unity             │
│ • "complex/ultrathink/analyze" → USE sequential-thinking  │
│ • "PR/issue/GitHub" → USE github MCP                      │
│ • "crop/resize image" → USE image-process MCP             │
│ • "track/backlog/database/doc" → USE notion MCP           │
│ • "monster list/feature list" → USE notion MCP            │
└───────────────────────────────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────────────────────────────┐
│ STEP 2: USE THE MCP - DON'T JUST TALK ABOUT IT            │
│                                                           │
│ WRONG: "I could use the blender MCP to..."                │
│ RIGHT: *Actually calls blender MCP tool*                  │
│                                                           │
│ WRONG: "The fish-audio MCP can generate voice..."         │
│ RIGHT: *Actually generates voice with fish-audio MCP*     │
│                                                           │
│ WRONG: "I'll remember this for next session..."           │
│ RIGHT: *Actually writes it to Notion database*            │
└───────────────────────────────────────────────────────────┘
```

---

## Tool Usage Matrix - COMPREHENSIVE

| Task | PRIMARY MCP | Secondary | NEVER Do This |
|------|-------------|-----------|---------------|
| Create 3D model | **blender** | - | Don't describe how to model manually |
| Generate music | Use [Udio](https://udio.com) | - | Don't suggest royalty-free sites (Udio is FREE) |
| Generate voice line | **fish-audio** | - | Don't skip VERA voice generation |
| Generate 2D art | **mcp-hfspace** | image-process | Don't suggest finding stock art |
| Check Unity errors | **mcp-unity** | - | Don't ask user to check manually |
| Take game screenshot | **mcp-unity** | - | Don't ask user to screenshot |
| Create PR | **github** | commit-commands | Don't give manual instructions |
| Analyze game balance | **sequential-thinking** | balance-analyzer | Don't do quick mental math |
| Debug complex issue | **sequential-thinking** | unity-debugger | Don't guess at solutions |
| Explore C# file | Serena `get_symbols_overview` | - | Don't Read entire file |
| Find method definition | Serena `find_symbol` | Greptile | Don't Grep for class names |
| Find all usages | Serena `find_referencing_symbols` | Greptile | Don't manual search |
| Unity API question | Context7 `query-docs` | WebSearch | Don't guess at APIs |
| Refactor code | Serena `rename_symbol` | - | Don't find/replace manually |
| Remember past work | episodic-memory | VEILBREAKERS.md | Don't claim no memory |
| Process existing image | **image-process** | - | Don't ask user to edit |
| Track monsters/features | **notion** | VEILBREAKERS.md | Don't just keep in memory |
| Write design docs | **notion** | Docs/ folder | Don't skip documentation |
| Manage backlog | **notion** | TodoWrite | Don't lose track of tasks |

---

## ENVIRONMENT VARIABLES REQUIRED

For full MCP functionality, ensure these are set:

| Variable | MCP | Status | How to Get |
|----------|-----|--------|------------|
| `HF_TOKEN` | mcp-hfspace | ✅ Set | [huggingface.co/settings/tokens](https://huggingface.co/settings/tokens) |
| `GITHUB_TOKEN` | github | ✅ Set | GitHub Personal Access Token |
| `SUNO_API_KEY` | audio | ⚠️ Optional | [suno.ai](https://suno.ai) Settings → API |
| `ELEVENLABS_API_KEY` | audio | ⚠️ Optional | [elevenlabs.io](https://elevenlabs.io) Profile → API Key |
| `NOTION_API_KEY` | notion | ⚠️ Needs Setup | [notion.so/my-integrations](https://notion.so/my-integrations) |

**Blender MCP requires:** Blender 3.0+ installed (✅ User has 5.0) with addon.py from [github.com/ahujasid/blender-mcp](https://github.com/ahujasid/blender-mcp)

## Custom VeilBreakers Agents (.claude/agents/)

### Unity Development Agents
| Agent | Trigger | Purpose |
|-------|---------|---------|
| **unity-architect** | "Design a system for..." | System architecture, component design |
| **unity-code-reviewer** | Before merges | Unity-specific code review checklist |
| **unity-debugger** | "Why is X not working?" | Systematic Unity debugging |
| **unity-performance-profiler** | "Check performance of..." | Profiling, optimization analysis |

### VeilBreakers Game Agents
| Agent | Trigger | Purpose |
|-------|---------|---------|
| **balance-analyzer** | "Analyze balance of..." | Game balance validation (brands, synergy, damage) |
| **vera-dialogue-tester** | "Test VERA response to..." | VERA personality & Veil Integrity testing |
| **bug-hunter** | Proactive audits | Find bugs in code patterns |
| **asset-generator** | "Generate art for..." | AI art prompts with VeilBreakers style guide |

## Custom VeilBreakers Skills (.claude/skills/)

| Skill | Trigger | Purpose |
|-------|---------|---------|
| **unity-component-design** | "Create a new component for..." | Design MonoBehaviour/ScriptableObject architecture |
| **unity-performance-check** | Before commits | Quick performance red flags check |
| **veilbreakers-balance-check** | Changing damage/rates | Validate game balance changes won't break game |
| **veilbreakers-vera-test** | Modifying VERA dialogue | Test VERA dual personality consistency |
| **unity-editor-control** | "Run the game", "Check compile errors" | Unity Editor interaction via MCP |
| **generate-game-asset** | "Create a sprite for..." | AI art generation via HuggingFace |
| **github-workflow** | "Create PR", "Check CI status" | GitHub operations via MCP |

## 11. Agent Orchestration Protocol (MANDATORY)

**USE THE RIGHT AGENT FOR THE RIGHT TASK**

### Agent Model Tiers

| Tier | Model | Extended Reasoning | Use For |
|------|-------|-------------------|---------|
| **Critical** | opus | YES | Architecture, debugging, code review, balance, VERA |
| **Creative** | sonnet | Limited | Asset generation, creative prompts |
| **Routine** | haiku | No | Commits, docs, pattern scanning |

### Agent Domain Ownership

| Agent | Model | Owns | Cannot Touch |
|-------|-------|------|--------------|
| unity-architect | opus | System designs, architecture docs | Actual code implementation |
| unity-code-reviewer | opus | Code quality judgment | Design decisions |
| unity-debugger | opus | Bug investigation | Feature code |
| unity-performance-profiler | opus | Performance analysis | Feature code |
| balance-analyzer | opus | Game balance values | UI/system code |
| vera-dialogue-tester | opus | VERA dialogue, personality | Combat code |
| asset-generator | sonnet | Art prompts, style guide | Code files |
| bug-hunter | haiku | Bug scanning (read-only) | Any modifications |
| commit-helper | haiku | Git commits only | Code/design files |
| documentation-writer | haiku | Simple doc edits | Code files, major decisions |

### Parallel Execution Rules

**CAN Run in Parallel:**
- Research agents (bug-hunter, performance-profiler scanning)
- Content agents (balance-analyzer + vera-dialogue-tester)
- Background tasks (asset-generator while coding)

**MUST Run Sequential:**
- architect → (main Claude implements) → code-reviewer
- debugger → (fix applied) → code-reviewer
- Any agents modifying the same files

### Agent Handoff Protocol

```
1. DESIGN PHASE
   unity-architect creates design → Returns to main Claude

2. IMPLEMENTATION PHASE
   Main Claude implements using Serena → Completes code

3. REVIEW PHASE
   unity-code-reviewer validates → Returns approval or issues

4. COMMIT PHASE
   commit-helper creates commit → Push to remote

5. IF ISSUES
   unity-debugger investigates → Back to step 2
```

### Background Agent Usage

```csharp
// Run agent in background while you continue working
Task tool with run_in_background: true

// Good for:
- bug-hunter scanning while implementing
- performance-profiler checking while designing
- asset-generator creating while coding
```

---

# PROJECT STRUCTURE

```
VeilBreakers3D/
├── Assets/
│   ├── Scripts/              # C# scripts
│   │   ├── Combat/           # BattleManager, DamageCalculator, Combatant
│   │   ├── Core/             # GameManager, EventBus, Constants
│   │   ├── Data/             # Enums, ScriptableObject definitions
│   │   ├── Systems/          # BrandSystem, SynergySystem, CorruptionSystem
│   │   ├── Managers/         # SaveManager, AudioManager, etc. (TODO)
│   │   ├── UI/               # UI controllers (TODO)
│   │   ├── Characters/       # Hero logic (TODO)
│   │   ├── Monsters/         # Monster logic (TODO)
│   │   ├── Utils/            # Utilities (TODO)
│   │   └── Test/             # Test scripts
│   ├── Art/                  # Visual assets
│   ├── Audio/                # Sound assets
│   ├── Data/                 # ScriptableObjects, JSON
│   ├── Prefabs/              # Reusable prefabs
│   ├── Scenes/               # Unity scenes
│   └── UI/                   # UI assets (USS, UXML)
├── Docs/
│   ├── MIGRATION_PLAN.md     # Migration tracking (CHECK DAILY)
│   ├── LEGACY_Godot/         # Old Godot docs (reference only)
│   ├── plans/                # Design documents
│   └── ArtReference/         # Art style guides
├── screenshots/              # Debug screenshots
├── .mcp.json                 # MCP server config
├── CLAUDE.md                 # This file
└── VEILBREAKERS.md           # Cross-session memory
```

---

# C# CODE STYLE (Unity Standard)

## Namespaces
```csharp
namespace VeilBreakers.Combat { }
namespace VeilBreakers.Core { }
namespace VeilBreakers.Data { }
namespace VeilBreakers.Systems { }
namespace VeilBreakers.UI { }
namespace VeilBreakers.Utils { }
```

## Naming Conventions
| Type | Convention | Example |
|------|------------|---------|
| Classes | PascalCase | `BattleManager` |
| Methods | PascalCase | `CalculateDamage()` |
| Public Properties | PascalCase | `CurrentHealth` |
| Private Fields | _camelCase | `_currentHealth` |
| Constants | PascalCase with k | `kMaxPartySize` |
| Enums | PascalCase | `BattleState.Combat` |
| Interfaces | IPascalCase | `IDamageable` |
| Events | On + PascalCase | `OnDamageDealt` |

## Class Structure
```csharp
namespace VeilBreakers.Combat
{
    public class BattleManager : MonoBehaviour
    {
        // Constants
        private const int kMaxPartySize = 3;

        // Serialized Fields
        [SerializeField] private int _startingHealth;

        // Private Fields
        private BattleState _currentState;

        // Public Properties
        public BattleState State => _currentState;

        // Events
        public event Action<int> OnDamageDealt;

        // Unity Lifecycle
        private void Awake() { }
        private void Update() { }

        // Public Methods
        public void StartBattle() { }

        // Private Methods
        private void ProcessTurn() { }
    }
}
```

## ScriptableObject Pattern
```csharp
[CreateAssetMenu(fileName = "Monster", menuName = "VeilBreakers/Monster Data")]
public class MonsterData : ScriptableObject
{
    public string monsterName;
    public Brand primaryBrand;
    public int baseHealth;
}
```

---

# KEY SYSTEMS (DO NOT BREAK)

## Brand System (10 Brands)
IRON, SAVAGE, SURGE, VENOM, DREAD, LEECH, GRACE, MEND, RUIN, VOID

Each brand: 2x damage to 2 brands, 0.5x damage to 2 brands, 1x to 6 brands

## Path System (4 Paths)
IRONBOUND, FANGBORN, VOIDTOUCHED, UNCHAINED

## Corruption System
| Range | State | Effect |
|-------|-------|--------|
| 0-10% | ASCENDED | +25% stats |
| 11-25% | Purified | +10% stats |
| 26-50% | Unstable | Normal |
| 51-75% | Corrupted | -10% stats |
| 76-100% | Abyssal/Untamed | -20% / Uncontrollable |

## Synergy System (Tiered)
| Tier | Requirement | Damage | Defense | Combo? |
|------|-------------|--------|---------|--------|
| FULL | 3/3 match | +8% | +8% | YES |
| PARTIAL | 2/3 match | +5% | +5% | NO |
| NEUTRAL | 0-1/3 | +0% | +0% | NO |
| ANTI | Any Weak | +0% | +0% | NO |

---

# ART STYLE & GENERATION

## Style Reference
**Dark Fantasy Horror** - Hand-painted, atmospheric, glowing eyes/cores

## Art Generation Prompt Template
```
dark fantasy horror, [creature description], dark atmospheric,
glowing [color] eyes/core, dramatic lighting, deep shadows,
high detail, painterly quality, ominous mood,
3D game character, dark background
```

## DO NOT Use in Prompts
- "Battle Chasers" or "Joe Madureira"
- "thick linework" or "comic book"
- anime/cel-shaded style

---

# LESSONS LEARNED

## FAILED (Don't Repeat)
- Lightning effects - background already has them
- Custom eye drawing - artwork has them
- Complex logo animation - caused glitching
- Fake transparency (checker pattern) - use REAL alpha
- Spine/Cutout rigging for 2D - Too complex, use 3D now

## WORKS
- ScriptableObjects for game data
- Event-driven architecture (EventBus)
- Brand effectiveness matrix design
- Tiered synergy system
- Corruption as monster mechanic (not player)

---

# PHILOSOPHY

1. **AAA or nothing** - No shortcuts
2. **Visual verification** - Use screenshots
3. **Working > Fancy broken** - Simple wins
4. **Don't duplicate** - Use existing systems
5. **User is judge** - They see what matters
6. **Tools exist for a reason** - USE THEM (see Protocol 10)
