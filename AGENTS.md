# VeilBreakers3D - Agent/MCP Setup

This repo uses MCP servers via `.mcp.json`.

## One-time prerequisites

- Install **Node.js** (so `node` + `npx` work).
- Install **uv** (so `uvx` works) if you use Serena.
- Open the project in **Unity 6000.3.6f1** (or the project's configured Unity version).

## Using MCPs in Codex

1) Start Unity and open `VeilBreakers3DCurrent` (keep Unity running).
2) Close any existing Codex session for this repo.
3) Re-open Codex from the repo root:

`C:\Users\Conner\OneDrive\Documents\VeilBreakers3DCurrent`

Codex should read `.mcp.json` and start MCP servers automatically.

## MCP profiles (project scope)

- **Default (`.mcp.json`):** Full stack including VB-Blender + VB-Unity (~23KB tool descriptions)
- **Lean (`.mcp.lean.json`):** No VB tools. Switch when doing code-only work to save ~23KB/msg
- Switch: `copy /Y .mcp.lean.json .mcp.json` (lean) or `copy /Y .mcp.full.json .mcp.json` (full)

## Core MCP servers (default)

- `mcp-unity` (Unity Editor control)
  - Uses `Tools/mcp/launch-unity-mcp.js`, which auto-resolves the current `Library/PackageCache/com.gamelovers.mcp-unity@.../Server~/build/index.js`.
  - No manual hash edits should be needed after Unity package updates.

- `gemini-cli` (Gemini tools via Gemini CLI)
  - Runs: `npx -y mcp-gemini-cli --allow-npx`
  - Authentication (recommended, no API keys):
    - Run `npx @google/gemini-cli` once and choose "Login with Google" (OAuth). After that, the MCP wrapper uses your saved login.
  - Core machine-local template: `Tools/mcp/gemini.settings.example.json`
  - Full machine-local template: `Tools/mcp/gemini.settings.full.example.json`

- `sequential-thinking` (structured decomposition)
- `memory-graph` (session memory context)
- `github` (PR/issues/CI context)

## Optional MCP servers (full profile only)

- `blender` (requires active Blender addon bridge on configured port)
- `image-process` (asset manipulation)
- `notion` (requires `NOTION_API_KEY`)

## How we use Gemini (planning + iteration)

- Planning: ask `gemini-cli.chat` for alternate VFX concepts + implementation checklists.
- Review: ask `gemini-cli.analyzeFile` to critique the latest title screen screenshot and suggest parameter tweaks.
  - Tip: press `F9` in Play Mode to write a screenshot into `screenshots/`.

## Gemini planning entrypoint (recommended prompts)

- Full project scope: `Docs/plans/GEMINI_FULL_SCOPE_PROMPT.md`
- Phase-based testing scope: `Docs/plans/GEMINI_PHASE_TEST_PLAN_PROMPT.md`

## Quality Gates

1. Before ANY UI Toolkit code: resolve Context7 library `/needle-mirror/com.unity.ui`
2. Before ANY PrimeTween code: resolve Context7 library `/kyrylokuzyk/primetween`
3. Before ANY Cinemachine code: resolve Context7 library `/websites/unity3d_packages_com_unity_cinemachine_3_1`
4. Before ANY URP code: resolve Context7 library `/unity-technologies/graphics`
5. If Context7 has no answer, read `Packages/` source — NEVER guess.

## Tool Usage

- **Context7** (`resolve-library-id` + `query-docs`): Use BEFORE writing any framework/library code
- **Serena** (`find_symbol`, `get_symbols_overview`): Use for code navigation, prefer over full file reads
- **Gemini CLI** (`chat`, `analyzeFile`): Use for second opinions, code review, visual analysis
- **Sequential Thinking**: Use for complex multi-step problems, game balance calculations

## Loop Prevention

- 3+ failed attempts on same approach → STOP, summarize what failed, try fundamentally different approach
- Never re-read a file you already have in context
- Never retry the same command hoping for different results
- If confused about project state, check `git log --oneline -10` and `git status`

## Agent Coordination

- **Claude (Opus)**: Architecture decisions, autonomous execution, final sign-off
- **Codex (GPT-5)**: Code generation, parallel implementation, analysis
- **Gemini**: Visual QA, research, second opinions, code review
- Shared state: git commits. Always check `git log` before starting work.
- Never duplicate work another agent may be doing. Check branch state first.

## Task Execution Standards

### Done-Criteria Template
Every task must include explicit completion criteria:
```
Done when:
- [ ] [specific measurable condition 1]
- [ ] [specific measurable condition 2]
- [ ] [specific measurable condition 3]
- [ ] No compile errors
- [ ] No regressions in existing functionality
```

### Step Limits
- Max 5 tool calls without visible progress → report status
- Max 3 failed attempts on same approach → STOP, try fundamentally different approach
- Max 2 fundamentally different approaches failed → escalate to user
- Test after every 3-5 file changes

### Validation Checkpoints
- After editing 3 files: run compile check
- After completing a feature: run relevant tests
- Before committing: verify no regressions
- After auto-compaction: re-read project config to restore context

### Escalation Triggers
Escalate to user immediately when:
- 2 fundamentally different approaches both fail
- Unclear which of multiple valid approaches to use
- High-risk changes detected (brand system, save format, corruption thresholds)
- Ambiguous requirements that could affect game balance

## GLM-Specific Rules (for Codex running GLM models)

### Anti-Hallucination
- Respond exclusively in English. Keep language consistent throughout.
- Complete current approach fully before reconsidering alternatives.
- Use only parameters that exactly match tool definitions. Validate before executing.
- Maintain consistent format throughout the entire response.

### Anti-Loop
- Stop immediately if you notice yourself repeating content or approaches.
- Max 5 tool calls without progress → report status and wait.
- If same command fails twice → change approach entirely.
- If confused about state → run git log + git status first.

### One-Shot Completion
- Read all relevant files before starting edits.
- Plan the complete change set before writing any code.
- Execute all changes, then test once, then report.
- Include done-criteria in every task prompt.

## Codex Configuration (config.toml)

Recommended settings for this project:
```toml
model_verbosity = "low"
model_reasoning_effort = "high"
plan_mode_reasoning_effort = "high"

compact_prompt = "Preserve: VeilBreakers brand system rules, corruption thresholds (80% UNTAMED), code style (kConstant, _private, PascalProperty), high-risk change list. Discard: exploration steps, failed attempts, file listings."

model_auto_compact_token_limit = 150000
tool_output_token_limit = 8000
```

## If MCP tools still don't appear

- Verify you launched Codex from the repo root (so it can find `.mcp.json`).
- Verify `node` is on PATH: `node -v`
- Verify Unity is running and the project is open (for `mcp-unity`).
- Run health check: `powershell -ExecutionPolicy Bypass -File .\Tools\mcp\check_reasoning_stack.ps1`
