# Requirements: VeilBreakers 3D - Character Select Rebuild

**Defined:** 2026-02-21
**Core Value:** The game flow from title screen through character selection to gameplay must work flawlessly with AAA-quality visuals at every step.

## v1 Requirements

Requirements for initial release. Each maps to roadmap phases.

### Infrastructure (INFRA)

- [ ] **INFRA-01**: USS stylesheets consolidated from 6 files to 2 (global + CharacterSelect), eliminating conflicting selectors and duplicate `:root` variables
- [ ] **INFRA-02**: TransitionController stub deleted or replaced with functional implementation
- [ ] **INFRA-03**: Unused CharSelectEvents removed (OnHeroDataLoaded, OnHeroSelected if confirmed unused)
- [ ] **INFRA-04**: Legacy `Input.mousePosition` calls replaced with InputManager in CharSelectEnvironmentController and ParallaxBackground
- [ ] **INFRA-05**: `SceneManager.sceneUnloaded` safety net added for `CharSelectEvents.ClearAll()` to prevent memory leaks
- [ ] **INFRA-06**: All `Q()` queries validated with Debug.Assert; element name constants defined to prevent silent null returns
- [ ] **INFRA-07**: `TemplateContainer` flex-grow set correctly in global USS to prevent layout collapse
- [ ] **INFRA-08**: CarouselController direct Manager reference removed; OnNavigationRequested event added for decoupling
- [ ] **INFRA-09**: Duplicate AnimatePanel() logic extracted from both panel controllers into shared utility

### Layout (LAYOUT)

- [ ] **LAYOUT-01**: Hero info panel text overlap fixed (name, title, quote, path, role display correctly without collision)
- [ ] **LAYOUT-02**: Champion/starter monster area displays correctly with name, brand, and role tags
- [ ] **LAYOUT-03**: Hero stories and synergy/brands populate correctly for all 4 heroes
- [ ] **LAYOUT-04**: Panel sizing correct at 1920x1080 with no overflow or clipping
- [ ] **LAYOUT-05**: `EnsureFullScreenLayout()` hack removed by fixing UXML root configuration
- [ ] **LAYOUT-06**: All USS transitions audited to animate only transform/color properties (no width/height/margin)
- [ ] **LAYOUT-07**: `UsageHints.DynamicTransform` set on all animated VisualElements at creation time

### Controller Behavior (CTRL)

- [ ] **CTRL-01**: All 6 buttons functional (Back, Prev, Next, Embark, Confirm, Cancel) with both mouse and gamepad
- [ ] **CTRL-02**: Gamepad focus ring visible with clear highlight on focused element
- [ ] **CTRL-03**: Audio feedback wired for navigation clicks, hero switch, and embark confirmation
- [ ] **CTRL-04**: Embark coroutine-Task bridge replaced with async/await + timeout + user-facing error feedback
- [ ] **CTRL-05**: Nebula texture generation pre-baked (eliminate per-switch `Color[65536]` allocation)
- [ ] **CTRL-06**: All VisualElement Q() queries cached; zero Q() calls in Update/hot paths
- [ ] **CTRL-07**: Panel exit-then-enter choreography implemented (slide-out before slide-in)
- [ ] **CTRL-08**: Confirm overlay focus trap working for gamepad navigation
- [ ] **CTRL-09**: Loading state feedback shown during GameDatabase initialization (skeleton/shimmer)

### Visual Polish (VISUAL)

- [ ] **VISUAL-01**: PrimeTween 1.3.7 installed and integrated for orchestrated animation sequences
- [ ] **VISUAL-02**: Coordinated panel transitions (left slides from left, right from right, staggered timing)
- [ ] **VISUAL-03**: Staggered stat bar fill animations on hero switch
- [ ] **VISUAL-04**: Per-hero URP Volume Profiles configured (Bloom, DoF, Vignette, Color Adjustments)
- [ ] **VISUAL-05**: Cinematic overlays active (scanlines, vignette, veil glow) via USS
- [ ] **VISUAL-06**: Embark sequence cinematic (1-2s animation before scene transition)
- [ ] **VISUAL-07**: USS filters applied for visual effects (blur on inactive panels, tint, contrast)
- [ ] **VISUAL-08**: Embark button breathing glow animation
- [ ] **VISUAL-09**: Per-hero ambient music crossfade via MusicManager

### Game Flow (FLOW)

- [ ] **FLOW-01**: Title screen loads first without battle screen flash (loading order fixed)
- [ ] **FLOW-02**: Settings button wired and functional from main menu
- [ ] **FLOW-03**: Game flow verified end-to-end: Bootstrap -> MainMenu -> CharacterSelect -> Overworld
- [ ] **FLOW-04**: Title screen visual amplification (AAA quality without code degradation)

### Code Quality (QUAL)

- [ ] **QUAL-01**: Full codebase debug pass (zero compilation errors)
- [ ] **QUAL-02**: Warning count minimized (zero warnings where possible)
- [ ] **QUAL-03**: No security vulnerabilities (no injection vectors, no unsafe deserialization, proper encryption maintained)
- [ ] **QUAL-04**: No allocations in Update/hot paths across all modified files
- [ ] **QUAL-05**: All modified code follows VeilBreakers conventions (_private, kConstant, PascalProperty, OnEvent)

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### 3D Art Integration (ART)

- **ART-01**: Placeholder capsules replaced with real hero model prefabs
- **ART-02**: HeroDisplayConfig per hero configured (camera position, lighting, champion offset/scale)
- **ART-03**: Champion monster 3D models placed alongside heroes
- **ART-04**: Hero-specific idle animations via Animator controllers
- **ART-05**: Per-hero lighting rigs fine-tuned for final models

### Extended Polish (XPOL)

- **XPOL-01**: Particle effects on hero switch (energy burst, color-themed motes)
- **XPOL-02**: Advanced camera choreography (zoom, pan, dolly on hero switch)
- **XPOL-03**: Hero lore expansion (backstory text in left panel from hero JSON)
- **XPOL-04**: Starter monster 2D sprite preview in champion section

## Out of Scope

| Feature | Reason |
|---------|--------|
| Character creation/customization | Breaks authored hero identities; 4 heroes are designed characters |
| Difficulty selection on CharSelect | Clutters flow; belongs in Settings menu |
| Auto-play/demo mode | RPG context, not arcade; parallax provides ambient interest |
| Real-time multiplayer hero locking | Single-player game; no server infrastructure |
| Detailed damage calculator | Overwhelms casual players; belongs in-game strategy guide |
| Mobile/console ports | Windows standalone is target platform |
| New hero characters | Focus on polishing existing 4 heroes |
| New monster species | Existing data sufficient for current milestone |
| Inventory/MonsterCollection save integration | TODOs exist but not part of this rebuild milestone |
| Overworld gameplay implementation | Scene exists but out of scope |
| Combat ability/balance changes | Combat system validated; don't modify |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| INFRA-01 | Phase 1 | Pending |
| INFRA-02 | Phase 1 | Pending |
| INFRA-03 | Phase 1 | Pending |
| INFRA-04 | Phase 1 | Pending |
| INFRA-05 | Phase 1 | Pending |
| INFRA-06 | Phase 1 | Pending |
| INFRA-07 | Phase 1 | Pending |
| INFRA-08 | Phase 1 | Pending |
| INFRA-09 | Phase 1 | Pending |
| LAYOUT-01 | Phase 2 | Pending |
| LAYOUT-02 | Phase 2 | Pending |
| LAYOUT-03 | Phase 2 | Pending |
| LAYOUT-04 | Phase 2 | Pending |
| LAYOUT-05 | Phase 2 | Pending |
| LAYOUT-06 | Phase 2 | Pending |
| LAYOUT-07 | Phase 2 | Pending |
| CTRL-01 | Phase 3 | Pending |
| CTRL-02 | Phase 3 | Pending |
| CTRL-03 | Phase 3 | Pending |
| CTRL-04 | Phase 3 | Pending |
| CTRL-05 | Phase 3 | Pending |
| CTRL-06 | Phase 3 | Pending |
| CTRL-07 | Phase 3 | Pending |
| CTRL-08 | Phase 3 | Pending |
| CTRL-09 | Phase 3 | Pending |
| VISUAL-01 | Phase 4 | Pending |
| VISUAL-02 | Phase 4 | Pending |
| VISUAL-03 | Phase 4 | Pending |
| VISUAL-04 | Phase 4 | Pending |
| VISUAL-05 | Phase 4 | Pending |
| VISUAL-06 | Phase 4 | Pending |
| VISUAL-07 | Phase 4 | Pending |
| VISUAL-08 | Phase 4 | Pending |
| VISUAL-09 | Phase 4 | Pending |
| FLOW-01 | Phase 5 | Pending |
| FLOW-02 | Phase 5 | Pending |
| FLOW-03 | Phase 5 | Pending |
| FLOW-04 | Phase 5 | Pending |
| QUAL-01 | Phase 5 | Pending |
| QUAL-02 | Phase 5 | Pending |
| QUAL-03 | Phase 5 | Pending |
| QUAL-04 | All (verified Phase 5) | Pending |
| QUAL-05 | All (verified Phase 5) | Pending |

**Coverage:**
- v1 requirements: 43 total
- Mapped to phases: 43 (41 phase-specific + 2 cross-cutting)
- Unmapped: 0

---
*Requirements defined: 2026-02-21*
*Last updated: 2026-02-21 after roadmap creation*
