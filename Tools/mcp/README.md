# MCP Profiles for VeilBreakers

## Core Principle

Keep always-on MCPs lean and reliable. Add tool-specific servers (for example Blender) only when their external app bridge is running.

## Files

- `.mcp.json`
  - Core profile loaded by default (recommended for day-to-day coding/reasoning).
- `.mcp.full.json`
  - Full profile with optional heavy MCPs.
- `Tools/mcp/launch-unity-mcp.js`
  - Stable Unity MCP launcher that auto-resolves the current `PackageCache` hash path.
- `Tools/mcp/gemini.settings.example.json`
  - Core machine-local template for Gemini CLI MCP setup.
- `Tools/mcp/gemini.settings.full.example.json`
  - Full machine-local template for Gemini CLI MCP setup.
- `Tools/mcp/check_reasoning_stack.ps1`
  - Health check: validates JSON configs, profile counts, and Claude plugin duplication metrics.

## Recommended Core Servers

- `mcp-unity`
- `gemini-cli`
- `sequential-thinking`
- `memory-graph`
- `github`

## Optional Servers

- `blender` (requires Blender addon bridge running on configured port)
- `notion` (requires `NOTION_API_KEY`)
- `image-process` (use when doing sprite/texture prep)

## Switching Profiles

- Codex/Claude project default: keep `.mcp.json` as core.
- If you need optional tools for a focused session, temporarily use `.mcp.full.json` as your active MCP config.
- Gemini local config: copy either `gemini.settings.example.json` (core) or `gemini.settings.full.example.json` (full) to `.gemini/settings.json`.

## Health Check

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\mcp\check_reasoning_stack.ps1
```

## Notes for Other Agents (Claude/Kimi)

- Claude: project config is in `.mcp.json`; plugin-provided MCPs (Context7/Serena/Greptile) may already be active.
- Kimi: no repo-local config is tracked; use this folder as the canonical MCP reference and mirror `mcp-unity` launcher usage.
