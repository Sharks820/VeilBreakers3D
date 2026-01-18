# VEILBREAKERS 3D - UNITY PROJECT

## Mission: AAA 3D Real-Time Tactical Monster RPG. NO COMPROMISES. NO RIVALS.

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

## Active MCP Servers (2 Local + 5 Plugin-Provided)

### Local MCPs (.mcp.json)
| MCP | Trigger | Usage |
|-----|---------|-------|
| **sequential-thinking** | Complex problems | Break down systems, balance calculations |
| **image-process** | Asset manipulation | Crop, resize, format conversion |

### Plugin-Provided MCPs
| MCP | Trigger | Usage |
|-----|---------|-------|
| **Context7** | Unity/C# questions | Query up-to-date API documentation |
| **Serena** | Code operations | Semantic code intelligence |
| **Greptile** | Codebase search | Cross-repo understanding |
| **Episodic Memory** | Past context | Search conversation history |
| **Chrome** | Web access | Research, documentation |

## Tool Usage Matrix - When to Use What

| Task | Primary Tool | Secondary |
|------|--------------|-----------|
| Explore C# file | Serena `get_symbols_overview` | - |
| Find method definition | Serena `find_symbol` | Greptile |
| Find all usages | Serena `find_referencing_symbols` | Greptile |
| Unity API question | Context7 `query-docs` | WebSearch |
| Complex planning | Superpowers brainstorm/plan | sequential-thinking |
| Refactor code | Serena `rename_symbol` | csharp-lsp verify |
| Check for errors | csharp-lsp | Serena |
| Before commit | code-review | - |
| PR creation | pr-review-toolkit | commit-commands |
| Remember past work | episodic-memory | VEILBREAKERS.md |
| Image processing | image-process MCP | - |
| Find duplicates | superpowers-lab skill | Serena |

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
