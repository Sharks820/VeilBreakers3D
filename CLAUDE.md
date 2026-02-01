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

## 3. Commit When It Makes Sense
- Commit after completing logical units of work
- Don't interrupt mid-task for arbitrary time-based commits
- Version updates in VEILBREAKERS.md track progress

---

# TOOL GUIDANCE (Not Mandates)

## Prefer These Tools When Appropriate

| Situation | Recommended Tool | Why |
|-----------|------------------|-----|
| Exploring unfamiliar code | Serena `get_symbols_overview` | Faster than reading whole file |
| Finding symbol usage | Serena `find_referencing_symbols` | Semantic, not text search |
| Unity API questions | Context7 `query-docs` | Up-to-date documentation |
| Complex analysis | `sequential-thinking` | Structured breakdown |
| Unity Editor control | `mcp-unity` | Direct editor access |
| Second opinion | Gemini CLI | Different perspective |
| 3D modeling | Blender MCP | Direct Blender control |

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

*Configuration v5.0 - Lean, flexible, reasoning-first*
