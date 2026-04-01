# VEILBREAKERS 3D — AI AGENT CONFIGURATION

> **Read this file top-to-bottom on every session start and after every compaction.**
> Critical rules are placed first. Both Claude and GLM must follow every instruction exactly.

**Engine:** Unity 3D (UI Toolkit) | **Game Bible:** `VEILBREAKERS.md` | **Migration:** `Docs/MIGRATION_PLAN.md`
**Mission:** Build an AAA-quality 3D monster RPG. Quality over speed, but don't overthink simple tasks.

---

# §1 — UNIVERSAL RULES (All Agents)

## 1.1 Anti-Regression Protocol ⛔ MANDATORY
1. **Read before edit.** Read every file before modifying it. No exceptions.
2. **Test every 3-5 changes.** Compile check or run relevant tests.
3. **Max 2 attempts per approach.** If fix #2 fails → re-read context → try a fundamentally different approach.
4. **Never guess API signatures.** Use Context7, Serena, or read source. Guessing has cost entire sessions.
5. **If you break X while fixing Y → revert Y immediately.** Don't stack fixes on a broken base.

## 1.2 Loop Prevention ⛔ MANDATORY
1. **3 failed attempts at the same thing → FULL STOP.** Summarize what failed and why.
2. **Never retry the same command/fix hoping for different results.**
3. **2 fundamentally different approaches both fail → STOP and escalate to user.**
4. **Max 5 tool calls without visible progress → report status to user before continuing.**
5. If confused about project state: `git log --oneline -10` + `git status` before any action.
6. **If an MCP tool fails or times out → do NOT retry more than once.** Fall back to built-in tools (Grep/Glob/Read/Bash). Never enter a retry loop on a broken MCP connection.

## 1.3 Context7 — HARD RULE
Before writing ANY code using these libraries, call `resolve-library-id` then `query-docs`:
- PrimeTween → `/kyrylokuzyk/primetween`
- UI Toolkit → `/needle-mirror/com.unity.ui`
- Cinemachine → `/websites/unity3d_packages_com_unity_cinemachine_3_1`
- URP → `/unity-technologies/graphics`

If Context7 returns nothing → read `Packages/` source. **NEVER guess or hallucinate an API.**

## 1.4 MCP Tool Discipline
**Tool selection priority** (use the highest-ranked tool that fits):
1. **Context7** — library/framework API questions (not web search)
2. **Grep/Glob/Read** — local codebase navigation (always available, zero failure risk)
3. **Serena** — symbol-aware code navigation and scoped edits
4. **zread** — public GitHub repo structure/files/docs (NOT for web pages)
5. **web-reader** — fetch+extract a specific known URL (NOT for discovery)
6. **web-search-prime** — broad web discovery, recent info (NOT for known URLs)
7. **zai visual tools** — screenshots, UI diff, diagrams, OCR
8. **gemini-cli / codex-cli** — second opinions, code review

**Failure modes that cause loops — avoid these:**
- Using zread on non-GitHub URLs → fails silently, agent retries
- Using web-reader for discovery instead of web-search-prime → empty results, agent retries
- Using web-search-prime when Context7 has the answer → wastes context, often wrong
- MCP tool returns empty/error → agent retries 5+ times → context bloat → compaction spiral
- Large image/screenshot from MCP (>2MB) → "image too large" → infinite retry loop

**Rule:** If any MCP tool fails → use built-in equivalent (Read/Grep/Bash/WebSearch). Do not persist.

## 1.5 Token Efficiency
- **Use Explore subagent** for broad searches — preserves main context, cheaper model.
- **Read with offset/limit** for large files. Don't read 2000 lines when you need 50.
- **Parallel tool calls** for independent operations — never serialize what can run concurrently.
- **Don't re-read files** already in context. Edit directly if you have the content.
- **Delegate research to subagents** — separate context windows.
- **MCP profiles:** Core (`.mcp.json`) = daily driver. Full (`.mcp.full.json`) = heavy tool sessions. Lean (`.mcp.lean.json`) = code-only, saves ~23KB/msg.

---

# §2 — CLAUDE-SPECIFIC OPTIMIZATION

## 2.1 Reasoning Protocol
- **Default:** 2-pass (hypothesis → targeted verification). Sufficient for most tasks.
- **Deep mode:** Use `sequential-thinking` MCP only for: high-risk changes, 3+ interacting systems, unclear repro steps, or game balance calculations.
- **Pre-commit verification:** Before any commit, mentally diff what changed vs. what was intended. Catch scope creep.

## 2.2 Compaction Recovery
After any auto-compaction:
1. Re-read this file (`CLAUDE.md`)
2. Check `MEMORY.md` for preserved session context
3. Run `git log --oneline -5` + `git status` to re-anchor
4. Resume from the last completed task, don't restart

## 2.3 Subagent Strategy
- **Explore agent** (quick/medium): file discovery, keyword search, codebase questions
- **Plan agent**: architecture decisions, implementation strategy before coding
- **General agent**: multi-step research, complex investigations
- Launch multiple agents in parallel when tasks are independent
- **Sequential for edits** — never have two agents editing the same file

## 2.4 Visual QA Pipeline
1. Design → brainstorm / HTML mockup / reference screenshot
2. Extract spec → `zai ui_to_artifact` (output_type=spec)
3. Implement → Unity UI Toolkit (UXML + USS + C#/PrimeTween)
4. Capture → `unity_editor action=screenshot`
5. Compare → `zai ui_diff_check` (expected=mockup, actual=screenshot)
6. Iterate until passes

---

# §3 — GLM-SPECIFIC OPTIMIZATION

> GLM: You are running inside Claude Code. These rules override any conflicting defaults.
> Your two biggest failure modes are **looping** and **context drift**. Follow these rules exactly.

## 3.1 Language & Output
- **Respond exclusively in English.** Never mix languages. Never output Chinese.
- Maintain consistent output format throughout your entire response. Never drift from structured to conversational mid-output.
- Complete your current approach fully before reconsidering alternatives. Never restart mid-output.

## 3.2 Thinking & Reasoning
- **Think before acting.** On every task, state your plan in 1-3 sentences before making any tool call.
- For complex tasks (3+ files, system interactions): think step-by-step explicitly.
- For simple tasks (single file, clear intent): act directly, no ceremony.
- Every task needs explicit done-criteria: "Done when: [specific measurable conditions]"

## 3.3 Loop Circuit-Breakers (YOUR #1 FAILURE MODE)
1. **Track your iteration count.** If you've attempted the same category of fix 3 times → STOP.
2. **Every step must introduce new logic or progress.** If your next action is identical to a previous one → STOP.
3. **If a tool call returns an error or empty result → do NOT call it again with the same parameters.**
4. **If you notice you're repeating yourself → STOP immediately and summarize what's stuck.**
5. **MCP tool fails → fall back to Bash/Read/Grep.** One retry max. Never loop on MCP failures.
6. **Max 5 consecutive tool calls without reporting progress to the user.** After 5, summarize status.

## 3.4 Context Window Management
- Your effective context is ~128K-200K tokens, but Claude Code's system prompt consumes a large portion.
- **Front-load critical information** in your responses — conclusions first, details second.
- **Don't echo back large file contents** in your responses. Reference by path and line numbers.
- After compaction: re-read CLAUDE.md, re-read MEMORY.md, run `git status`. Do this before ANY other action.
- **Avoid unnecessary tool calls** that return large outputs (full file reads, broad searches). Be surgical.

## 3.5 Tool Call Discipline
- Validate all function call parameters exactly match the tool schema before executing. Never fabricate parameters.
- **One tool, one purpose:** zread=GitHub repos, web-reader=specific URLs, web-search-prime=web discovery, Context7=API docs.
- If a tool isn't responding → switch to built-in alternatives. Don't wait.
- Prefer built-in tools (Read/Grep/Glob/Bash) over MCP tools when both can accomplish the task. Built-ins never fail silently.

---

# §4 — PROJECT CONTEXT

## Key Systems (Domain rules in `.claude/rules/` — loaded automatically per path)
- **10-Brand Combat** → `.claude/rules/combat/brands-synergy.md`
- **Corruption (0-100%)** → `.claude/rules/combat/corruption.md`
- **UI Toolkit** → `.claude/rules/ui/toolkit.md`
- **Save System** → `.claude/rules/systems/save-system.md`
- **Audio** → `.claude/rules/systems/audio.md`
- **3D Pipeline** → `.claude/rules/systems/3d-pipeline.md`

**Quick reference (don't modify without reading the full rule file):**
- 4 Paths: IRONBOUND, FANGBORN, VOIDTOUCHED, UNCHAINED
- Party: 3 Active + 3 Backpack (hard constraint)
- 80% corruption = UNTAMED = uncontrollable (hard game state boundary)
- Brand effectiveness is bidirectional (if A→B is 2x, then B→A must be 0.5x)

## Code Style
```csharp
namespace VeilBreakers.[Category] {
    private const int kMaxValue = 10;      // Constants: k prefix
    [SerializeField] private int _value;   // Private: _ prefix
    public int Value => _value;            // Properties: PascalCase
    public event Action<int> OnChanged;    // Events: On prefix
}
```

## Project Structure
Scripts: `Assets/Scripts/[Combat|Core|Systems|UI|Data]/` | Art: `Assets/Art/` | Docs: `Docs/`

---

# §5 — HIGH-RISK CHANGES (Ask User First)
Brand/Path design changes · Save format modifications · Core class renames/removals · Major architecture changes · Corruption tier thresholds · Capture formula modifications · Party slot structure · Synergy multiplier adjustments · File deletions (archive instead)

# §6 — SECURITY
- SaveManager: AES-CBC + HMAC-SHA256 — maintain on all format changes
- Validate deserialized saves (corruption 0-100, multipliers 0.5-2x, party max 6)
- No `Path.Combine` with user input, no `JSON.Parse` of untrusted strings
- Event unsubscription on cleanup (memory leak vector)

# §7 — LESSONS LEARNED
**Don't:** `Find()` in Update · allocations in Update · missing font refs · disabled components · editing without reading · guessing APIs · retrying same broken approach 5+ times · stacking fixes on broken base · Windows reserved filenames (nul/con/aux)
**Do:** ScriptableObjects for data · event-driven architecture · visual verification · read before edit · test every 3-5 changes · parallel agents for research / sequential for edits

# §8 — GIT WORKFLOW
`master` = production truth | `develop` = mirrors master | `feature/<name>` = from master
After every commit: `git branch -f develop master` | Before ending session: verify branches synced

---
*Configuration v10.0 — Dual-model optimization (Claude + GLM). Restructured for instruction priority, expanded loop prevention, MCP failure handling, tool discipline. Path-scoped domain rules in .claude/rules/*
