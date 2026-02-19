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

## Other Tools

| Situation | Recommended Tool | Why |
|-----------|------------------|-----|
| Unity API questions | Context7 `query-docs` | Up-to-date documentation |
| Complex analysis | `sequential-thinking` | Structured breakdown |
| Unity Editor control | `mcp-unity` | Direct editor access |
| Second opinion | Gemini CLI | Different perspective |
| 3D modeling session | Blender MCP (optional) | Use only when Blender bridge is active |

### MCP Loading Strategy
- Default to **core MCP profile** (`.mcp.json`) for best reasoning signal-to-noise.
- Use **full MCP profile** (`.mcp.full.json`) only for focused tool-heavy sessions (Blender/image/notion).

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
- **Gemini CLI:** Senior reviewer. Validates each phase output.
- **Codex CLI:** Senior reviewer. Validates each phase output.
- **Kimi CLI:** Senior reviewer. Validates each phase output.

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
  4. Gemini + Codex + Kimi validate via bash CLI
  5. Opus gives FINAL SIGN-OFF
  6. On approval:
     a. Save to episodic memory (conversation search)
     b. Save to local memory (Serena/AIM)
     c. Commit to feature branch
     d. Merge to develop branch
  7. Move to next phase
```

### Branch Strategy
- `master` -- production, untouched during rebuild
- `feature/character-select-rebuild` -- main feature branch
- `feature/cs-phase-N` -- per-phase branches (merged into feature branch on approval)

### Escalation Rule
If Sonnet fails on a task after 2 attempts, Opus takes over implementation immediately. No negotiation.

### Testing Requirements Per Phase
- Unity compilation: MUST pass (zero errors)
- Code review: Opus sign-off required
- External review: At least 2/3 CLI reviewers must approve
- Security scan: No hardcoded secrets, no unsafe deserialization, no injection vectors
- Performance check: No allocations in hot paths, cached references, proper disposal

### Memory Protocol Per Phase
After each phase approval:
1. `episodic-memory` -- save phase completion with key decisions
2. `aim_memory_store` -- save to project context
3. `serena write_memory` -- save implementation notes
4. `git commit` -- commit with descriptive message
5. `git merge` -- merge phase branch into feature branch

---

*Configuration v5.2 - Priority Path: Character Select Rebuild Protocol*
