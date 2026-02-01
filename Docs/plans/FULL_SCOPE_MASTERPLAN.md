# VeilBreakers3D - Full Scope Masterplan (Working)

This is the "source of truth" plan we use to drive phases, builds, and weekly execution. It is designed to be refined by Gemini (via MCP) and then kept in sync with `Docs/ROADMAP.md`.

## Vision (v1)

- VeilBreakers is a high-contrast dark-fantasy monster RPG where "the Veil" is a living system: it corrupts the world, your monsters, and the UI presentation.
- The game wins on atmosphere + readable combat + addicting capture/progression, not sheer open-world size.

## Pillars (non-negotiable)

1) Atmosphere: the world feels cursed, alive, and reactive (VFX/audio/UI feedback).
2) Combat: fast, readable, satisfying impacts; minimal downtime; clear status effects.
3) Capture: unique capture fantasy (not a reskinned Pokeball) with tension + mastery.
4) Progression: constant rewards (monsters, evolutions, skills, crafting) without grind cliffs.
5) Presentation: menus, transitions, and VFX feel premium (motion, depth, polish).

## Scope: Epics (initial)

1) Title/Menu Presentation (AAA first impression)
   - Goal: title screen + UI feels alive (motion layers, audio stingers, responsive buttons).
   - DoD:
     - Title screen VFX renders above UI Toolkit and is readable at 1080p+1440p.
     - Animation is visible but does not wash out logo/buttons.
     - One-click screenshot capture for iteration.
   - Key code: `Assets/Scripts/UI/Effects/MainMenuVFXOverlayController.cs`

2) Core Combat Loop (vertical slice ready)
   - Goal: 1 battle type that feels shippable.
   - DoD:
     - Start battle -> pick actions -> resolve -> rewards -> return.
     - At least 5 skills with distinct VFX/sfx and clear telegraphs.
     - No frame spikes > 16ms on target scene (PC baseline).

3) Capture System Overhaul (v0.70)
   - Goal: 4 capture methods, each readable and balanced.
   - DoD:
     - All methods implemented end-to-end with UI + VFX + feedback.
     - Telemetry logging (rates, misses, fail reasons).

4) Monster Data + Progression Pipeline
   - Goal: add monsters safely, quickly, consistently.
   - DoD:
     - One data source of truth (JSON or ScriptableObjects) with validation.
     - Import/validation tooling to prevent broken assets in builds.

5) First Zone (world slice)
   - Goal: a single zone with hub + encounter loop + boss arena.
   - DoD:
     - Traversal, encounter triggers, loot drop, exit back to hub.
     - Lighting + fog + VFX conveys Veil corruption.

6) Audio Framework (premium feedback)
   - Goal: dynamic music + layered SFX, including menu/battle transitions.
   - DoD:
     - Music manager + snapshot mixing; per-scene routing.
     - No missing references; fallback rules.

7) Save/Load + Settings + Options
   - Goal: stable persistence for v1.
   - DoD:
     - Save slots, auto-save, settings persisted.
     - Backward-compatible save migrations for at least 2 versions.

## Phases + Gates (recommended)

Pre-production (1-2 weeks)
- Lock pillars, target platform, performance budgets, build pipeline.
- Gate: CI builds a playable menu scene on every push to `master`.

Vertical Slice (3-6 weeks)
- 1 zone + 1 battle + capture + 5 monsters + full UI loop.
- Gate: 30-min play session, no blockers, consistent FPS, passable audio/VFX.

Alpha (6-10 weeks)
- Full game loop, more monsters/zones, systems "feature complete".
- Gate: complete run from new game to end of Act 1 without dev tools.

Beta (4-6 weeks)
- Content complete, fix-only, performance and balance.
- Gate: crash rate near zero; stable saves; consistent performance.

RC (2-4 weeks)
- Release candidate hardening, store pages, achievements, final QA.
- Gate: no known critical bugs; deterministic builds; signed artifacts.

## Build / CI (Unity + GitHub Actions)

- Branching:
  - `master`: always shippable (menu launches, no missing refs).
  - `feat/*`: feature work; merge only with passing CI.
- CI steps (baseline):
  - Lint/compile (fast): `dotnet build` as a sanity gate.
  - Unity batchmode build: Windows player + artifact upload.
  - Optional: Unity Test Runner (EditMode + targeted PlayMode smoke tests).
  - Artifact naming: `VeilBreakers3D_{semver}_{gitsha}_{date}.zip`
- Quality budgets:
  - Menu scene: 120 FPS on mid-tier PC; 0 GC spikes > 1ms in idle.
  - Combat: cap overdraw; limit full-screen alpha layers.

## AI generation pipeline (safe defaults)

- Good candidates (fast iteration):
  - Concept art and paintovers (then hand-tune composition).
  - VFX textures (noise masks, embers, smoke), with compression rules.
  - UI decoration layers (but keep text/buttons human-controlled).
  - SFX sweeteners (layered), then normalize and mix.
- High risk / needs heavy review:
  - Core UI readability, gameplay balance, narrative continuity.
  - Anything that becomes player-critical feedback (telegraphs).
- “Human gate” rules:
  - Every AI asset must have: source prompt, license notes, and a reviewer tag.
  - No direct-to-master for AI assets without a quick in-engine validation.

## Next 14 days (execution)

1) Title screen: iterate VFX to "premium" using screenshot loop + Gemini critique.
2) Add a vertical-slice “play loop” scene flow (Menu -> Battle -> Rewards -> Menu).
3) Implement capture overhaul scaffold + telemetry for balancing.
4) Establish CI Windows build artifact pipeline.
5) Add 5 monsters with consistent data validation + error reporting.

