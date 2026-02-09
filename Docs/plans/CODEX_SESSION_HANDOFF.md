# Codex Session Handoff (VeilBreakers3DCurrent)

Purpose: capture the current state so a fresh Codex session can continue with full context (MCP-enabled).

## Immediate blocker we addressed

- Title screen VFX was not visible because UI Toolkit background images render above camera output.
- Implemented a UGUI ScreenSpaceOverlay VFX stack to render above UI Toolkit.

## Latest issue (from screenshot 2026-01-31)

- VFX looked frozen and extremely bright (logo unreadable, buttons effectively gone).
- Root causes:
  - Many shaders animated using `_Time` (stops if `Time.timeScale == 0`).
  - Too many additive full-screen layers stacked with high alpha/intensity.

## Fixes implemented (current working state)

1) Unfreeze menu VFX even when timeScale is 0
- Overlay now pushes `Time.unscaledTime` into materials each frame via `_UnscaledTime`.
- Shaders updated to use `_UnscaledTime` when present:
  - `Assets/Art/VFX/Shaders/VFX_BackGlow.shader`
  - `Assets/Art/VFX/Shaders/VFX_LightRays.shader`
  - `Assets/Art/VFX/Shaders/VFX_HeatShimmer.shader`
  - `Assets/Art/VFX/Shaders/VFX_ScrollingSmoke.shader`
  - `Assets/Art/VFX/Shaders/VFX_FloatingParticles.shader`
  - `Assets/Art/VFX/Shaders/VFX_EnergyPulse.shader`
  - `Assets/Art/VFX/Shaders/VFX_LogoShimmer.shader`
  - `Assets/Art/VFX/Shaders/VFX_Vignette.shader`
  - `Assets/Art/VFX/Shaders/UI_RisingEmbers.shader`
  - `Assets/Art/VFX/Shaders/UI_FloatingAsh.shader`

2) Keep logo + buttons readable
- Added a "UI clear mask" concept to full-screen shaders (fade VFX near top/bottom).
- Lowered default alphas/intensities in:
  - `Assets/Scripts/UI/Effects/MainMenuVFXOverlayController.cs`

3) Add intimidating motion (not just shine)
- Added dark moving tendrils layer:
  - `Assets/Art/VFX/Shaders/VFX_VeilWisps.shader`
- Enabled/tuned via:
  - `Assets/Scripts/UI/Effects/MainMenuVFXOverlayController.cs`

4) Screenshot loop for iteration
- Press `F9` in Play Mode to save a screenshot into `screenshots/`.

## Tests/CI scaffolding added (phase-based quality)

- Docs:
  - `Docs/plans/PHASED_TEST_STRATEGY.md`
  - `Docs/plans/GEMINI_PHASE_TEST_PLAN_PROMPT.md`
- Unity tests (new):
  - `Assets/Tests/EditMode/MainMenuAssets_EditModeTests.cs`
  - `Assets/Tests/EditMode/SceneIntegrity_EditModeTests.cs`
  - `Assets/Tests/PlayMode/MainMenuOverlay_PlayModeTests.cs`
- Batch runners (new):
  - `Tools/ci/verify_phase.ps1`
  - `Tools/ci/run_unity_tests.ps1`
  - `Tools/ci/find_unity.ps1`
- Note: batchmode tests require Unity to be closed for this project (Unity locks the project).

## Compile errors encountered and fixed

- Fixed test assembly references and missing-script scan:
  - `Assets/Tests/PlayMode/VeilBreakers.Tests.PlayMode.asmdef`
  - `Assets/Tests/EditMode/VeilBreakers.Tests.EditMode.asmdef`
  - `Assets/Tests/EditMode/SceneIntegrity_EditModeTests.cs`

## MCP setup

- Project MCP config: `.mcp.json`
  - `mcp-unity` configured as `type: stdio` via `Tools/mcp/launch-unity-mcp.js` (auto-resolves package hash).
  - `gemini-cli` via `npx -y mcp-gemini-cli --allow-npx` (use Gemini CLI OAuth login).
- Docs: `AGENTS.md`

## Next step after reopening (MCP live)

1) Open Unity `VeilBreakers3DCurrent`.
2) Reopen Codex from repo root so MCP loads.
3) Hit Play on MainMenu and press `F9`.
4) Feed that screenshot to Gemini `analyzeFile` for critique, then iterate:
   - reduce/raise `_globalAlpha`
   - tighten/loosen UI clear mask values
   - tune smoke/wisps threshold for “scary” vs “muddy”

