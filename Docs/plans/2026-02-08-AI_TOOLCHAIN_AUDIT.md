# AI Toolchain Audit (Codex + Claude + Gemini/Kimi)

Date: 2026-02-08

## Goal

Maximize reasoning quality while keeping token/tool overhead low.

## Final Profile Strategy

- **Default profile:** `.mcp.json` (core-only, lean reasoning stack)
- **Full profile:** `.mcp.full.json` (optional heavy tools for focused sessions)
- **Gemini core template:** `Tools/mcp/gemini.settings.example.json`
- **Gemini full template:** `Tools/mcp/gemini.settings.full.example.json`

## Core Stack (Always On)

- `mcp-unity`
- `gemini-cli`
- `sequential-thinking`
- `memory-graph`
- `github`

Why: highest reasoning leverage per token/tool surface area.

## Optional Stack (On-Demand)

- `blender`
- `image-process`
- `notion`

Why: useful, but noisy or irrelevant outside specific asset/project workflows.

## Verified Working

- `mcp-unity` launcher resolves current package hash via `Tools/mcp/launch-unity-mcp.js`.
- Core MCP JSONs parse cleanly.
- Unity discovery script resolves Unity `6000.3.6f1`.

## Refactors Applied

1. MCP profile split
- Created `.mcp.full.json` (full profile).
- Reduced `.mcp.json` to core profile only.

2. Gemini config hardening
- Updated `.gemini/settings.json` to core profile only.
- Added env placeholder mapping for GitHub/Notion templates.

3. Docs/skills alignment
- Updated `AGENTS.md` with core vs full profile guidance.
- Updated `CLAUDE.md` with explicit reasoning budget policy.
- Updated `.claude/skills/unity-editor-control/SKILL.md` with current Unity MCP launcher/package details.
- Updated `Docs/plans/CODEX_SESSION_HANDOFF.md` to launcher-based Unity MCP.

4. CI signal cleanup
- Updated `Tools/ci/find_unity.ps1`:
  - Default version: `6000.3.6f1`
  - Deduped/fixed candidate paths.

5. Diagnostic automation
- Added `Tools/mcp/check_reasoning_stack.ps1` for repeatable health checks.

## Claude Plugin Normalization Results

Before normalization:
- Duplicate plugin ids: 24
- Duplicate-enabled plugins: 7

After normalization:
- Duplicate plugin ids: 17
- Duplicate-enabled plugins: 0

## Known Blocker (Local Claude Plugin Cache)

- `episodic-memory@superpowers-marketplace` reinstall currently fails with:
  - `EPERM ... better_sqlite3.node` (locked file in plugin cache)
- Current fallback: use MCP `memory-graph` for memory continuity.
- Recovery: restart Claude/Codex processes (or reboot), then reinstall `episodic-memory`.

## High-ROI Unity Additions Still Missing

- `com.unity.memoryprofiler`
- `com.unity.performance.profile-analyzer`
- `com.unity.testtools.codecoverage`
- `com.unity.ai.navigation` (if 3D nav scope is active)

## Kimi Readiness

- No repo-local Kimi config found.
- Use the same core/full MCP strategy from `Tools/mcp/` when wiring Kimi.
