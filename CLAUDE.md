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
  - Use at most one external "second-opinion" round (Gemini) per decision
  - Summarize findings before continuing broad exploration
- **Stop conditions:** when confidence is high and tests/validation pass, ship instead of over-analyzing

## 4. Commit When It Makes Sense
- Commit after completing logical units of work
- Don't interrupt mid-task for arbitrary time-based commits
- Version updates in VEILBREAKERS.md track progress

---

# TOOL GUIDANCE (Smart Judgment)

## Serena - Use Judgment, Not Defaults

| Task | Use Serena? | Instead |
|------|-------------|---------|
| Understand unfamiliar file structure | YES - `get_symbols_overview` | - |
| Find where a method is called | YES - `find_referencing_symbols` | - |
| Refactor a symbol | YES - `replace_symbol_body` | - |
| Read a file you know the path to | **NO** | Just use `Read` tool |
| Quick text search | **NO** | Just use `Grep` tool |
| Small edit to known code | **NO** | Just use `Edit` tool |

## Superpowers Skills - Use When Valuable

| Skill | USE for | SKIP for |
|-------|---------|----------|
| brainstorming | New systems, unclear requirements | Simple additions, bug fixes |
| writing-plans | Multi-file implementations | Single-file changes |
| systematic-debugging | Complex/mysterious bugs | Obvious errors |
| verification-before-completion | Major changes, PRs | Quick fixes |

**Default:** Skip skills for simple tasks. Use skills when complexity justifies structure.

## VB-Toolkit (PREFER for game dev tasks)

37 compound tools (22 Unity + 15 Blender) with 330+ actions. Use these FIRST for game dev tasks.
**Full reference:** `Docs/TOOLKIT_REFERENCE.md`

## Other Tools

| Situation | Recommended Tool | Why |
|-----------|------------------|-----|
| Unity API questions | Context7 `query-docs` | Up-to-date documentation |
| Complex analysis | `sequential-thinking` | Structured breakdown |
| Second opinion | `gemini-cli` or `codex-cli` MCP | Different AI perspectives |
| C# code intelligence | csharp-lsp plugin | Real-time diagnostics |

## Model Routing (Token Optimization)

| Task Type | Model | Why |
|-----------|-------|-----|
| Read files, find patterns, verify claims | **Haiku** | Cheapest, fast, accurate for factual checks |
| Write code, fix bugs, implement features | **Sonnet** | Good quality-to-cost ratio |
| Architecture decisions, code review, sign-off | **Opus** | Only when judgment matters |
| Simple git ops, file creation | **Haiku** | Don't waste Sonnet on routine work |

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

# CLAUDE + GEMINI + CODEX HYBRID APPROACH

| Situation | Tool | Action |
|-----------|------|--------|
| Second opinion on architecture | Gemini CLI | `gemini -p "Analyze..."` or `mcp__gemini-cli__chat` |
| Code review | Codex CLI | `codex -p "Review..."` or `mcp__codex-cli__chat` |
| Complex debugging stuck | Either | Get a different perspective |
| Research/web search | Gemini CLI | Gemini has web access |

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

# GIT WORKFLOW

**Branch Structure:**
- `master` — production truth
- `develop` — integration branch, mirrors master after merges
- `feature/<name>` — feature branches from master

**After every commit:** sync branches with `git branch -f develop master`
**Before ending session:** verify all active branches point to same commit

---

*Configuration v6.0 - Token-Optimized*
