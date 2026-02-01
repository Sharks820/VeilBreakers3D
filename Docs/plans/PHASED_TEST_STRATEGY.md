# VeilBreakers3D - Phased Test Strategy (5x Quality)

Goal: treat every phase as a shippable milestone. Tests are **phase-gated**, **repeatable**, and designed to catch regressions the moment we touch unrelated systems.

This strategy assumes:
- Unity 2022.3 LTS
- Unity Test Framework available (`com.unity.test-framework`)
- Automated verification runs locally and can be promoted to CI later.

## Core loop (what you asked for)

1) Test
2) Scan for errors (logs + missing refs + missing scripts)
3) Optimize (only if it does not change intended behavior)
4) Re-test
5) Repeat until:
   - No errors
   - No missing refs/scripts
   - Performance budgets met for the phase
   - Behavior matches expectations

## Test tiers (run order)

1) Fast checks (seconds)
- `dotnet build` (sanity compile gate)
- EditMode validation tests (assets/scenes/shaders/resources)

2) PlayMode smoke (minutes)
- Scene load + critical objects exist
- No missing shaders/materials
- No UI input blocked by overlays
- Basic performance sanity (GC spikes)

3) Full regression (phase dependent)
- Scenario tests (combat loop, capture methods, save/load)
- Scene sweep (load each scene, validate no missing scripts)

## Phase gates (what “5x” means in practice)

Pre-Production Gate (always green)
- 100% pass: Fast checks + Menu PlayMode smoke.
- No console errors.

Vertical Slice Gate
- Everything in Pre-Production
- Plus: combat loop smoke, capture loop smoke, save/load smoke.
- Performance: menu idle should have near-zero per-frame allocations after warmup.

Alpha Gate (feature complete)
- Everything in Vertical Slice
- Plus: full regression suites by system (combat/capture/save/ui/audio).
- Scene sweep: every scene loads without missing scripts.

Beta Gate (content complete, fix-only)
- Everything in Alpha
- Plus: performance budgets and load-time budgets; memory spikes are tracked.

RC Gate (release candidate)
- Everything in Beta
- Plus: build verification and deterministic artifacts (no missing resources).

## Current categories (NUnit)

- `Suite.Smoke` (minimal always-run)
- `Suite.Integrity` (assets/scenes/shaders)
- `Suite.Perf` (lightweight profiler-based checks)
- `Phase.PreProd`
- `Phase.VerticalSlice`
- `Phase.Alpha`
- `Phase.Beta`
- `Phase.RC`

## How to run

Use the PowerShell runners in `Tools/ci/`:
- `Tools/ci/verify_phase.ps1 -Phase PreProd`
- `Tools/ci/verify_phase.ps1 -Phase VerticalSlice`

