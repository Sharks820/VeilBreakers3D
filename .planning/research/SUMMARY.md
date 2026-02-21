# Project Research Summary

**Project:** VeilBreakers 3D - Character Select Rebuild
**Domain:** AAA RPG Character Selection Screen (Unity 6 / UI Toolkit)
**Researched:** 2026-02-21
**Confidence:** MEDIUM-HIGH

## Executive Summary

The VeilBreakers Character Select rebuild is a UI-heavy milestone on an established Unity 6.3 codebase with solid architectural bones. The existing code follows an Orchestrator + Sub-Controller + Scoped Event Bus pattern (effectively MVP adapted for Unity), which is the correct architecture and should be preserved. The primary work is not rearchitecting -- it is cleaning up accumulated technical debt (duplicate stylesheets, dead code, legacy Input API), fixing execution-level bugs in the controllers, and then layering on AAA visual polish using Unity 6.3's native capabilities (USS filters, USS transitions, UI Shader Graph) plus one new dependency: PrimeTween for orchestrated animation sequences.

The recommended approach is a strict bottom-up build: fix infrastructure first (USS consolidation, event safety, Input API migration), then correct layout and controller behavior, and finally apply visual amplification. This order is driven by hard dependency chains discovered in the research -- every visual polish task depends on having a single canonical stylesheet, every controller fix depends on clean event wiring, and every animation depends on correct layout. Attempting polish before infrastructure cleanup will result in wasted work as styles conflict and animations fire on incorrectly-sized elements.

The key risks are threefold: (1) the 6 USS files with competing selectors will silently override any new styling work until consolidated, (2) the coroutine-Task bridge in the embark flow swallows exceptions leaving players stuck with no feedback, and (3) the per-hero-switch nebula texture generation allocates 256KB on the main thread causing GC pressure during what should be a smooth transition. All three have straightforward fixes identified in the research. The biggest external dependency is 3D art assets -- the entire hero model preview system is built and waiting but serving placeholder capsules. Art pipeline delivery gates the single largest visual upgrade but should not block the UI/UX rebuild.

## Key Findings

### Recommended Stack

The project is already on Unity 6000.3.6f1 (6.3 LTS) with URP 17.3.0 and Input System 1.18.0. Only one new package is needed: **PrimeTween 1.3.7** for zero-allocation UI animation sequences. Everything else leverages Unity 6.3's native capabilities that the project already has access to but is not yet using.

**Core technologies:**
- **PrimeTween 1.3.7** (NEW): Orchestrated multi-element animation sequences -- zero-allocation, native VisualElement support, replaces need for DOTween/Timeline/coroutine-based animation
- **USS Transitions** (EXISTING): Simple state-based transitions (hover, focus, panel slide) using 25+ built-in easing functions -- no dependency needed
- **USS Filters** (EXISTING, UNUSED): blur, grayscale, tint, hue-rotate, contrast on any VisualElement -- new in Unity 6.3, animatable via USS transitions
- **UI Shader Graph** (EXISTING, UNUSED): Custom material effects (glow, animated gradients, distortion) on VisualElements -- new in Unity 6.3
- **RenderTexture + Camera** (EXISTING): 3D hero model preview in UI -- infrastructure fully built, awaiting real model assets
- **URP Volume System** (EXISTING): Bloom (Kawase), Depth of Field (Bokeh), Vignette, Color Adjustments for cinematic hero presentation

**Critical version requirement:** Unity 6000.3.x is required for USS filters and UI Shader Graph. Do not downgrade.

### Expected Features

**Must have (table stakes) -- v1:**
- Hero navigation with prev/next cycling (DONE)
- Hero identity display: name, title, role, quote (DONE)
- Stat preview with dual panels (DONE)
- Confirm/cancel flow (DONE)
- Back navigation to main menu (DONE)
- USS stylesheet consolidation (4 files to 1) -- blocks all visual work
- Loading state feedback (skeleton/shimmer during GameDatabase load) -- prevents blank screen
- Audio feedback on navigation (SFX for click, switch, embark) -- silence feels broken
- Gamepad focus ring (visible highlight on focused element) -- controller unusable without it
- Legacy Input.mousePosition fix -- route through InputManager
- Panel exit animations (slide-out before slide-in) -- current snap-then-slide feels jerky

**Should have (differentiators) -- v1.x:**
- Animated stat bar stagger (cascading fill on hero switch)
- Per-hero dynamic background polish (pre-baked nebula textures)
- Cinematic overlay tuning (scanlines, vignette, veil glow)
- Embark sequence cinematic (1-2s animation before scene transition)
- Per-hero ambient music crossfade
- Starter monster visual preview (2D sprite minimum)
- Glass-morphism panel styling

**Defer (v2+):**
- 3D hero models (blocked on art pipeline)
- 3D champion monster models (blocked on art pipeline)
- Hero-specific idle animations (requires rigged models)
- Particle effects on hero switch

**Anti-features (do NOT build):**
- Character creation/customization (breaks authored hero identities)
- Difficulty selection on this screen (clutters flow, belongs in Settings)
- Auto-play/demo mode (RPG context, not arcade)
- Real-time multiplayer hero locking (single-player game)

### Architecture Approach

The existing Orchestrator + Sub-Controller + Scoped Event Bus pattern is correct and should be preserved. `CharacterSelectManager` acts as the sole event source, raising events through `CharSelectEvents` (a scoped static bus with 8 events and `ClearAll()` for cleanup). Six sub-controllers each own one UI region and subscribe/unsubscribe in `OnEnable`/`OnDisable`. Data flows unidirectionally from Manager to controllers. UXML defines structure, USS defines style, C# manages behavior.

**Major components:**
1. **CharacterSelectManager** -- Orchestrator: loads data, manages hero index, drives embark flow, raises all events
2. **CharSelectEvents** -- Scoped event bus: 8 typed events with null-safe raise helpers, isolated from global EventBus
3. **CarouselController** -- Hero card strip: generates cards, handles click selection (needs direct Manager ref removed)
4. **HeroDataPanelController** -- Left info panel: name, title, quote, path, role, synergy, champion info
5. **HeroStatsPanelController** -- Right stats panel: D&D attribute bars with animated fills, ability slots
6. **HeroStageController** -- 3D preview: RenderTexture pipeline, 5-light rig, drag rotation, model swap
7. **CharSelectEnvironmentController** -- Background: parallax layers, procedural nebula, fog tinting (has legacy Input bug)
8. **TransitionController** -- DEAD STUB: subscribes to events but does nothing (fix or delete)
9. **HeroDisplayConfig** -- ScriptableObject: per-hero visual config (camera, lighting, colors, model prefabs, audio, VFX)

**Architectural fixes needed (execution, not pattern):**
- Remove direct Manager reference from CarouselController (add OnNavigationRequested event)
- Fill or delete TransitionController stub
- Extract duplicate AnimatePanel() logic from both panel controllers
- Remove unused OnHeroDataLoaded and OnHeroSelected events

### Critical Pitfalls

1. **Duplicate USS stylesheets cause silent style conflicts** -- 6 USS files compete with `*` selectors and `:root` variables. The `VeilBreakersUI.uss` sets `transition-duration` on `*`, adding transitions to every element. Consolidate to 2 files (global + CharacterSelect) before any style work begins.

2. **Static event bus memory leaks** -- `CharSelectEvents` is static and outlives scene-scoped MonoBehaviours. If any subscriber is destroyed without unsubscribing, subsequent event invocations throw `MissingReferenceException`. Add `SceneManager.sceneUnloaded` safety net and verify all controllers unsubscribe in `OnDisable()`.

3. **Coroutine-Task bridge swallows exceptions** -- The embark flow busy-waits on SaveManager Tasks with no timeout and silently `yield break`s on failure. Player sees nothing. Replace with Unity 6 `Awaitable` or add explicit timeout + UI error feedback.

4. **USS transitions on layout properties cause frame drops** -- Animating `width`, `height`, `margin`, `left`, `top` triggers full Yoga layout recalculation per frame. Only animate `translate`, `rotate`, `scale`, `opacity`, `color`. Set `UsageHints.DynamicTransform` at element creation time.

5. **Per-switch nebula generation allocates 256KB on main thread** -- `GenerateNebula()` creates a `Color[65536]` array with Perlin noise on every hero switch. Pre-bake 4 textures at init or as assets.

## Implications for Roadmap

Based on research, the rebuild should follow 5 phases in strict dependency order.

### Phase 1: Foundation Cleanup
**Rationale:** Every subsequent phase touches these systems. Fixing infrastructure first prevents cascading bugs and wasted effort. The USS consolidation alone blocks all visual work.
**Delivers:** Clean, stable foundation with no dead code, no legacy API conflicts, no style conflicts
**Addresses features:** USS consolidation (P1), Legacy Input fix (P1), Loading state feedback (P1)
**Avoids pitfalls:** Duplicate USS conflicts (Pitfall 2), Static event leaks (Pitfall 3), Legacy Input API (Pitfall 7), TemplateContainer layout (Pitfall 1), Q() null returns (Pitfall 8), Bootstrap load order (Pitfall 9)
**Scope:**
- Consolidate 6 USS files down to 2 (global + CharacterSelect)
- Delete or fill TransitionController stub
- Add OnNavigationRequested event, remove CarouselController direct Manager reference
- Remove unused events (OnHeroDataLoaded, OnHeroSelected)
- Fix Input.mousePosition calls to use New Input System
- Define element name constants for all Q() queries with Debug.Assert validation
- Fix TemplateContainer flex-grow globally
- Add SceneManager.sceneUnloaded safety net for CharSelectEvents.ClearAll()

### Phase 2: Layout and Structure
**Rationale:** Layout must be correct before animations can be tuned. Styling on broken layout is wasted work.
**Delivers:** Pixel-correct UI at all resolutions with proper panel sizing, text rendering, and content structure
**Addresses features:** Visual hero differentiation (table stakes), Stat preview (table stakes), Starter monster info display
**Avoids pitfalls:** USS transition performance (Pitfall 5) -- establish correct animate-only-transform-properties pattern from the start
**Scope:**
- Fix text overlap and panel sizing in hero info panel
- Fix champion/monster area display
- Fix hero stories and synergy/brands rendering
- Remove EnsureFullScreenLayout() hack by fixing UXML root configuration
- Audit all USS transitions to use only transform/color properties
- Set UsageHints.DynamicTransform on all animated elements

### Phase 3: Controller Behavior
**Rationale:** Behavior must work correctly before visual polish. Performance fixes here prevent GC spikes during hero transitions that would undermine Phase 4 animations.
**Delivers:** All interactions working (6 buttons, carousel, gamepad, embark flow), zero GC allocations during hero switch
**Addresses features:** Gamepad focus ring (P1), Audio feedback (P1), Panel exit animations (P1), Smooth transitions (table stakes), Confirm flow
**Avoids pitfalls:** Coroutine-Task bridge (Pitfall 4), Nebula GC pressure (Performance trap), RenderTexture lifecycle (Pitfall 6)
**Uses stack:** Input System 1.18.0 (gamepad NavigationSubmitEvent + NavigationMoveEvent), AudioManager (SFX wiring)
**Scope:**
- Fix all 6 button interactions with both mouse and gamepad support
- Implement gamepad focus ring with visual feedback
- Wire AudioManager SFX triggers for navigation, hero switch, embark
- Replace embark coroutine-Task bridge with async/await + timeout + error UI
- Pre-bake nebula textures (eliminate GenerateNebula per-switch allocation)
- Cache all VisualElement queries, verify no Q() calls in hot paths
- Implement panel exit-then-enter choreography
- Fix confirm overlay focus trap for gamepad

### Phase 4: Visual Amplification (AAA Polish)
**Rationale:** Visual polish is the final layer. Only possible after layout is correct and behavior is solid. This is where the screen goes from "functional" to "AAA."
**Delivers:** Cinematic, premium-feeling character select with coordinated animations, post-processing, and atmospheric effects
**Addresses features:** Animated stat bar stagger (P2), Per-hero background polish (P2), Cinematic overlays (P2), Embark sequence (P2), Glass-morphism panels (P2)
**Uses stack:** PrimeTween 1.3.7 (install here), USS Filters (blur, tint, contrast), URP Volume System (Bloom, DoF, Vignette, Color Adjustments)
**Scope:**
- Install PrimeTween 1.3.7
- Implement coordinated panel transitions with PrimeTween sequences (left slides from left, right from right, staggered timing)
- Add staggered stat bar fill animations
- Configure per-hero URP Volume Profiles (Bloom Kawase, Bokeh DoF, Vignette, Color Adjustments, Tonemapping)
- Activate cinematic overlays (scanlines, vignette, veil glow) via USS
- Implement embark sequence cinematic (1-2s animation before scene transition)
- Apply USS filters for visual effects (blur on inactive panels, tint, contrast)
- Add embark button breathing glow animation
- Per-hero ambient music crossfade via MusicManager

### Phase 5: 3D Integration and Art Pipeline
**Rationale:** Gated on external art asset delivery. All infrastructure is built and tested with placeholders. When models arrive, this phase is primarily configuration, not code.
**Delivers:** Full 3D hero and champion monster previews, replacing placeholder capsules
**Addresses features:** 3D hero model with drag-to-rotate (P3), Starter monster 3D preview (P3), Hero-specific idle animations (P3)
**Uses stack:** RenderTexture pipeline (existing), HeroDisplayConfig ScriptableObjects (existing), Animator controllers (new per model)
**Scope:**
- Replace placeholder capsules with real hero model prefabs
- Configure HeroDisplayConfig per hero (camera position, lighting, champion offset/scale)
- Add champion monster models alongside heroes
- Implement hero-specific idle animations via Animator
- Fine-tune per-hero lighting rigs

### Phase Ordering Rationale

- **Phase 1 before all else:** USS consolidation is a hard gate. Editing styles against competing 6-file specificity is wasted effort. Event safety and Input API fixes prevent bugs that would confuse debugging in later phases.
- **Phase 2 before Phase 3:** Controllers animate panels -- if panel layout is wrong, animations look broken even when code is correct. Fix the target before fixing the motion.
- **Phase 3 before Phase 4:** Visual polish (PrimeTween sequences, Volume effects) amplifies existing behavior. If buttons do not work or gamepad navigation fails, polish is meaningless.
- **Phase 4 before Phase 5:** Polish can be completed with placeholder models. 3D assets arriving should slot into an already-polished screen, not an unfinished one.
- **Phase 5 is independent:** Can run whenever art delivers. No code dependency on earlier phases except that HeroStageController cleanup (Phase 1) and RenderTexture lifecycle fixes (Phase 3) are complete.

### Research Flags

Phases likely needing deeper research during planning:
- **Phase 3 (Controller Behavior):** Gamepad focus management in UI Toolkit has known edge cases (confirm overlay focus trap, NavigationSubmitEvent vs ClickEvent distinction). Needs phase-level research on UI Toolkit focus system and `delegatesFocus` behavior.
- **Phase 4 (Visual Amplification):** PrimeTween integration with UI Toolkit VisualElements is documented but the project has not used it before. Glass-morphism via USS `backdrop-filter` needs verification on Unity 6000.3.6f1 specifically. USS Filters (blur, tint) are new in 6.3 and may have edge cases.

Phases with standard patterns (skip research-phase):
- **Phase 1 (Foundation Cleanup):** All fixes are straightforward code cleanup with well-documented patterns (USS specificity rules, event lifecycle, Input System migration).
- **Phase 2 (Layout and Structure):** Standard UI Toolkit layout work with USS Flexbox. Extensively documented in Unity's official guides.
- **Phase 5 (3D Integration):** RenderTexture + Camera pipeline is a well-established Unity pattern. Dragon Crashers sample project provides reference implementation.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | Only 1 new dependency (PrimeTween). All other tech is already in the project or native to Unity 6.3. Official docs verified for USS filters, UI Shader Graph, URP Volume. |
| Features | MEDIUM-HIGH | Based on analysis of 6+ AAA RPGs (Pokemon, Persona 5, FF, Genshin, Monster Hunter, Fire Emblem) and direct codebase review. Feature priorities are clear. Art pipeline dependency for v2+ features introduces uncertainty in timeline. |
| Architecture | HIGH | Existing pattern is verified correct against Unity's official MVC/MVP guidance. All recommended fixes are execution-level, not architectural. Every component boundary is documented from code review. |
| Pitfalls | HIGH | All 9 pitfalls are codebase-verified (not theoretical). 7 of 9 are confirmed by Unity community reports. Recovery strategies are documented with cost estimates. |

**Overall confidence:** MEDIUM-HIGH

### Gaps to Address

- **Glass-morphism / backdrop-filter support in Unity 6000.3.6f1:** USS `backdrop-filter` support is uncertain for this specific version. Research indicates USS filters (foreground) are available but backdrop blur (behind the element) may require a URP render pass or third-party package. Validate during Phase 4 planning.
- **PrimeTween + UI Toolkit VisualElement interaction model:** PrimeTween documents `Tween.VisualElementOpacity()` and similar APIs, but real-world sequencing with USS class-toggle animations (the existing pattern) needs validation. Determine whether PrimeTween replaces or complements USS transitions.
- **Audio asset availability:** Navigation SFX, hero switch sounds, per-hero ambient tracks, and embark sequence audio do not exist yet. Phase 3 and Phase 4 features depend on audio assets. Code wiring is trivial but assets are an external dependency.
- **3D model delivery timeline:** The single largest visual differentiator (3D hero preview) is completely blocked on art pipeline. All code infrastructure is ready. Timeline for model delivery is unknown and gates Phase 5 entirely.
- **UI Toolkit data binding GC allocations:** Community reports indicate the binding system causes large GC allocations per frame. Research recommends avoiding data binding for animated properties and setting them directly via `style`. This needs profiling validation during Phase 3.

## Sources

### Primary (HIGH confidence)
- Unity 6.3 Official Manual: USS Transitions, USS Filters, UI Shader Graph, URP Volumes, UsageHints, Performance Considerations, Focus System
- Unity MVC/MVP Patterns (Unity Learn Course)
- Unity 6.3 LTS Release Notes (6000.3.6f1)
- Xbox Accessibility Guideline 107 (Microsoft)
- Game Accessibility Guidelines (gameaccessibilityguidelines.com)
- Direct codebase review of 8 CharacterSelect controller files, UXML, and USS

### Secondary (MEDIUM confidence)
- PrimeTween GitHub + DeepWiki API documentation
- UI Toolkit Advanced E-Book (Unity 6 2025)
- Dragon Crashers UI Toolkit Sample Project
- Persona 5 UI/UX analysis (Ridwan Khan, Design Bootcamp)
- Game UI Database (1,300+ games reference)
- Unity Forum discussions: TemplateContainer, USS transitions, focus issues, binding GC

### Tertiary (LOW confidence -- validate before using)
- Glass-morphism / backdrop-filter availability in Unity 6000.3.6f1 (community request, unconfirmed)
- UI Toolkit Binding GC allocation severity (community reports, not officially acknowledged)
- Unified-Universal-Blur package stability (third-party, unvetted)

---
*Research completed: 2026-02-21*
*Ready for roadmap: yes*
