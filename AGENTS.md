# VeilBreakers3D - Agent/MCP Setup

This repo uses MCP servers via `.mcp.json`.

## One-time prerequisites

- Install **Node.js** (so `node` + `npx` work).
- Install **uv** (so `uvx` works) if you use Serena.
- Open the project in **Unity 6000.3.6f1** (or the project’s configured Unity version).

## Using MCPs in Codex

1) Start Unity and open `VeilBreakers3DCurrent` (keep Unity running).
2) Close any existing Codex session for this repo.
3) Re-open Codex from the repo root:

`C:\Users\Conner\OneDrive\Documents\VeilBreakers3DCurrent`

Codex should read `.mcp.json` and start MCP servers automatically.

## MCP profiles (project scope)

- **Default (lean):** `.mcp.json` (core reasoning stack)
- **Optional full profile:** `.mcp.full.json` (enable only when needed for asset/project ops)

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

## If MCP tools still don’t appear

- Verify you launched Codex from the repo root (so it can find `.mcp.json`).
- Verify `node` is on PATH: `node -v`
- Verify Unity is running and the project is open (for `mcp-unity`).
- Run health check: `powershell -ExecutionPolicy Bypass -File .\Tools\mcp\check_reasoning_stack.ps1`
