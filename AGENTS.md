# VeilBreakers3D - Agent/MCP Setup

This repo uses MCP servers via `.mcp.json`.

## One-time prerequisites

- Install **Node.js** (so `node` + `npx` work).
- Install **uv** (so `uvx` works) if you use Serena.
- Open the project in **Unity 2022.3.62f3** (or the project’s configured Unity version).

## Using MCPs in Codex

1) Start Unity and open `VeilBreakers3DCurrent` (keep Unity running).
2) Close any existing Codex session for this repo.
3) Re-open Codex from the repo root:

`C:\Users\Conner\OneDrive\Documents\VeilBreakers3DCurrent`

Codex should read `.mcp.json` and start MCP servers automatically.

## Configured MCP servers (project scope)

- `mcp-unity` (Unity Editor control)
  - Uses the server bundled by the Unity package in `Library/PackageCache/.../Server~/build/index.js`.
  - If the package hash changes, update the path in `.mcp.json`.

- `gemini-cli` (Gemini tools via Gemini CLI)
  - Runs: `npx -y mcp-gemini-cli --allow-npx`
  - Authentication (recommended, no API keys):
    - Run `npx @google/gemini-cli` once and choose "Login with Google" (OAuth). After that, the MCP wrapper uses your saved login.

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
