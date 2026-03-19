# Roadmap: VeilBreakers 3D - Character Select Rebuild

## Overview

Rebuild the Character Select screen from a functional-but-buggy state to AAA visual quality, following a strict bottom-up dependency chain: clean the foundation (USS conflicts, dead code, legacy APIs), fix layout, wire controller behavior, layer visual polish, then verify end-to-end game flow. Each phase delivers a coherent, verifiable capability that unblocks the next. The project has 43 v1 requirements across 6 categories mapped to 5 phases.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [x] **Phase 1: Foundation Cleanup** - Eliminate dead code, consolidate stylesheets, fix legacy APIs, and harden event safety
- [x] **Phase 2: Layout & Structure** - Fix all panel sizing, text overlap, and content rendering at target resolution
- [ ] **Phase 3: Controller Behavior** - Wire all interactions, fix performance, add gamepad support and audio feedback
- [ ] **Phase 4: Visual Amplification** - Install PrimeTween, add cinematic animations, post-processing, and atmospheric effects
- [ ] **Phase 5: Game Flow & Quality** - Verify end-to-end flow, polish title screen, pass full quality audit

## Phase Details

### Phase 1: Foundation Cleanup
**Goal**: Clean, stable codebase foundation with no dead code, no legacy API conflicts, no stylesheet conflicts, and safe event lifecycle
**Depends on**: Nothing (first phase)
**Requirements**: INFRA-01, INFRA-02, INFRA-03, INFRA-04, INFRA-05, INFRA-06, INFRA-07, INFRA-08, INFRA-09
**Success Criteria** (what must be TRUE):
  1. Project compiles with zero errors after all infrastructure changes; only 2 USS files remain (global + CharacterSelect) with no duplicate `:root` variables or conflicting `*` selectors
  2. Character Select screen loads and displays hero data correctly using only New Input System APIs -- no `Input.mousePosition` or other legacy Input calls exist anywhere in CharacterSelect controllers
  3. Navigating away from Character Select and returning does not leak event subscriptions -- `CharSelectEvents.ClearAll()` fires reliably on scene unload
  4. All `Q()` element queries are backed by named constants with `Debug.Assert` validation -- a typo in an element name produces an immediate, obvious error instead of silent null
  5. CarouselController communicates navigation intent through events, not direct Manager references; TransitionController is either functional or deleted; duplicate `AnimatePanel()` logic exists in one shared location
**Plans**: 3 plans

Plans:
- [x] 01-01: USS Consolidation (INFRA-01)
- [x] 01-02: Foundation Cleanup (INFRA-02 through INFRA-09)
- [x] 01-03: 19-Bug Fix Audit (CLI Reviewer Findings)

### Phase 2: Layout & Structure
**Goal**: Pixel-correct UI at 1920x1080 with proper panel sizing, readable text, correct content population, and no layout hacks
**Depends on**: Phase 1 (requires consolidated USS to prevent style conflicts)
**Requirements**: LAYOUT-01, LAYOUT-02, LAYOUT-03, LAYOUT-04, LAYOUT-05, LAYOUT-06, LAYOUT-07
**Success Criteria** (what must be TRUE):
  1. Hero info panel displays name, title, quote, path, and role for all 4 heroes without any text overlapping or clipping at 1920x1080
  2. Champion/starter monster area shows monster name, brand icon, and role tag correctly for all 4 heroes
  3. Hero stories text and synergy/brands section populate with correct data for each hero when navigating through all 4 characters
  4. No panel content overflows or clips at 1920x1080; `EnsureFullScreenLayout()` hack is deleted and UXML root configuration handles full-screen layout natively
  5. All USS transitions animate only transform and color properties (no width, height, margin, left, top); all animated VisualElements have `UsageHints.DynamicTransform` set at creation time
**Plans**: 2 plans

Plans:
- [x] 02-01-PLAN.md -- Fix UXML structure, content population, panel sizing, and remove EnsureFullScreenLayout hack
- [x] 02-02-PLAN.md -- Rewrite USS transition declarations and set UsageHints.DynamicTransform on animated elements

### Phase 3: Controller Behavior
**Goal**: Rule-of-thirds layout with tabbed info panel, hold-to-embark replacing confirm popup, zone-based gamepad navigation with visible focus ring, per-hero audio feedback, async embark with timeout and error toast, skeleton loading states, and zero-GC hero switching with pre-baked nebula textures
**Depends on**: Phase 2 (controllers animate panels -- layout must be correct first)
**Requirements**: CTRL-01, CTRL-02, CTRL-03, CTRL-04, CTRL-05, CTRL-06, CTRL-07, CTRL-08, CTRL-09
**Success Criteria** (what must be TRUE):
  1. All interaction points (Back, Prev, Next, Embark hold, Tab switch, Carousel card click) respond to both mouse click and gamepad input; L1/R1 switches heroes and cycles tabs
  2. Gamepad navigation shows a visible focus ring highlight on the currently focused zone; during embark hold, navigation is blocked (focus trap)
  3. Navigation clicks, hero switches, and embark confirmation each play distinct audio feedback (placeholder tones if no audio assets exist)
  4. Holding Embark for 1.5s initiates an async flow with 10s timeout and user-facing error toast if SaveManager operations fail -- no silent hang, no swallowed exceptions
  5. Hero switch completes with zero per-frame GC allocations: nebula textures are pre-baked for all 4 heroes at init, all VisualElement queries are cached, and panel transitions use exit-then-enter choreography
**Plans**: 3 plans

Plans:
- [ ] 03-01-PLAN.md -- Restructure UXML to rule-of-thirds with tabbed info, veil-torn dark fantasy USS, and extend CharSelectEvents
- [ ] 03-02-PLAN.md -- Rewrite controllers for async embark, tab management, toast, skeleton loading, remove confirm overlay
- [ ] 03-03-PLAN.md -- Create CharSelectFocusManager (gamepad zones), HoldToEmbarkController (hold-to-confirm), pre-bake nebula cache, audio feedback

### Phase 4: Visual Amplification
**Goal**: Cinematic, premium-feeling character select with orchestrated PrimeTween animations, per-hero post-processing, atmospheric overlays, and polished micro-interactions
**Depends on**: Phase 3 (visual polish amplifies working behavior -- animations on broken interactions are wasted work)
**Requirements**: VISUAL-01, VISUAL-02, VISUAL-03, VISUAL-04, VISUAL-05, VISUAL-06, VISUAL-07, VISUAL-08, VISUAL-09
**Success Criteria** (what must be TRUE):
  1. PrimeTween 1.3.7 is installed and driving coordinated panel transitions: left panel slides from left edge, right panel slides from right edge, with staggered timing that feels intentional
  2. Switching heroes triggers staggered stat bar fill animations (bars cascade rather than all filling simultaneously) and per-hero URP Volume Profile changes (Bloom, DoF, Vignette, Color Adjustments shift to match the selected hero's visual identity)
  3. Cinematic overlays (scanlines, vignette, veil glow) are visible and enhance atmosphere; USS filters (blur on inactive panels, tint, contrast) create visual depth hierarchy between active and inactive elements
  4. Clicking Embark plays a 1-2 second cinematic sequence (animation, effects) before the scene transition begins; the Embark button has a continuous breathing glow animation while idle
  5. Selecting a different hero crossfades ambient music to the new hero's theme via MusicManager -- no hard audio cuts
**Plans**: 4 plans

Plans:
- [ ] 04-01-PLAN.md -- Install PrimeTween, update asmdef references, create HeroThemeConfig ScriptableObject, write VeilDissolve shader
- [ ] 04-02-PLAN.md -- Build visual subsystems: VolumeProfileTransitioner, OverlayController, GlitchTextEffect, VeilDissolveController, extend MusicManager
- [ ] 04-03-PLAN.md -- Wire choreography: HeroThemeTransitioner, HeroSwitchAnimator, ScreenEntryAnimator, stat cascade, carousel animations, lighting lerp
- [ ] 04-04-PLAN.md -- Embark cinematic: VeilCrack shader, VeilTransitionController, EmbarkCinematicController, visual verification checkpoint

### Phase 5: Game Flow & Quality
**Goal**: End-to-end game flow verified from Bootstrap through Overworld, title screen polished to AAA standard, full quality audit with zero errors and clean conventions
**Depends on**: Phase 4 (final verification after all Character Select changes are complete)
**Requirements**: FLOW-01, FLOW-02, FLOW-03, FLOW-04, QUAL-01, QUAL-02, QUAL-03, QUAL-04, QUAL-05
**Success Criteria** (what must be TRUE):
  1. Game launches to title screen without any flash of battle screen or other scenes; loading order is deterministic and correct
  2. Settings button on main menu is wired, clickable, and opens a functional settings panel
  3. Full game flow works end-to-end: Bootstrap initializes managers, MainMenu loads, CharacterSelect works with all Phase 1-4 functionality, selecting Embark transitions to Overworld scene
  4. Title screen has AAA visual quality (effects, animations, polish) without degrading code architecture or performance
  5. Full codebase compiles with zero errors, minimized warnings, no security vulnerabilities (injection, unsafe deserialization), zero allocations in Update/hot paths across all modified files, and all code follows VeilBreakers conventions (`_private`, `kConstant`, `PascalProperty`, `OnEvent`)
**Plans**: TBD

Plans:
- [ ] 05-01: TBD
- [ ] 05-02: TBD

---

## Milestone: MCP Blender Server Architecture

### Overview

Build a custom MCP Python server + Blender TCP socket bridge for AI-assisted 3D asset pipeline automation. Replaces the third-party blender-mcp with a custom veilbreakers-blender server using compound action tools, mandatory visual verification, and AST-validated code execution.

### Phase 1: Foundation Server Architecture
**Goal**: End-to-end communication between Claude (MCP host) and Blender: a Python MCP server dispatching validated commands over TCP to a Blender addon, receiving structured responses, capturing viewport screenshots, and composing multi-angle contact sheets
**Depends on**: Nothing (independent track)
**Requirements**: ARCH-01, ARCH-02, ARCH-03, ARCH-04, ARCH-05, ARCH-06, ARCH-07, ARCH-08
**Success Criteria** (what must be TRUE):
  1. Claude can invoke 6 compound tools (blender_scene, blender_object, blender_material, blender_viewport, blender_execute, blender_export) that dispatch operations to Blender
  2. Every mutation tool returns a viewport screenshot alongside structured results -- visual verification is non-negotiable
  3. Malformed or dangerous Python code is rejected by AST validation before reaching Blender -- no raw exec() without security checks
  4. Contact sheet system renders multi-angle composite images via Pillow grid composition
  5. Blender addon survives rapid sequential tool calls via queue+timer pattern with zero deadlocks
  6. TCP socket bridge handles connect/reconnect/timeout with structured error responses
**Plans**: 3 plans

Plans:
- [ ] 01-01-PLAN.md -- Scaffold uv project, TCP socket client, pydantic models, config, MCP server entry point
- [ ] 01-02-PLAN.md -- Blender addon socket server with queue+timer dispatch, command handlers, AST security validator
- [ ] 01-03-PLAN.md -- 6 compound MCP tools with visual verification, contact sheet composition, .mcp.json integration

## Progress

**Execution Order:**
Phases execute in numeric order: 1 -> 2 -> 3 -> 4 -> 5

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Foundation Cleanup | 3/3 | Complete | 2026-02-22 |
| 2. Layout & Structure | 2/2 | Complete | 2026-02-22 |
| 3. Controller Behavior | 0/3 | Not started | - |
| 4. Visual Amplification | 0/4 | Not started | - |
| 5. Game Flow & Quality | 0/2 | Not started | - |

**MCP Server Architecture:**

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Foundation Server Architecture | 0/3 | Not started | - |
