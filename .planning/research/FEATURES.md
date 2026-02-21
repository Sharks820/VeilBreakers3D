# Feature Research: Character Select Screen

**Domain:** AAA RPG Character Selection Screen (Monster RPG, Dark Fantasy)
**Researched:** 2026-02-21
**Confidence:** MEDIUM-HIGH (based on analysis of existing AAA RPGs: Pokemon, Persona, Final Fantasy, Genshin Impact, Monster Hunter, Fire Emblem; cross-referenced with game UI databases, UX case studies, and current codebase state)

## Feature Landscape

### Table Stakes (Users Expect These)

Features users assume exist. Missing any of these makes the screen feel broken, unfinished, or amateurish.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| **Hero navigation (prev/next cycling)** | Every character select screen has this; players need to browse all options | LOW | Already implemented via `NavigatePrev()`/`NavigateNext()` with wrap-around. Working. |
| **Hero identity display (name, title, role)** | Players need to know who they are picking; Pokemon shows species/type, FF shows job/class | LOW | Already implemented in `HeroDataPanelController`. Populates name, title, quote, path, role, synergy. |
| **Stat preview (comparative data)** | Players make informed decisions by comparing stats; every RPG shows base stats at selection | MEDIUM | Partially implemented. Left panel shows HP/ATK/DEF/SPD chips. Right panel shows D&D attribute bars (STR/DEX/CON/INT/WIS/CHA). Both update on hero change. Bar fill animation works via USS width transitions. |
| **Starter monster/companion preview** | In monster RPGs (Pokemon, Monster Hunter) the starter creature is the real decision point; it must be prominent | MEDIUM | Partially implemented. Champion section exists in left panel (name + brand/role tags). No visual preview of the monster -- only text. 3D champion model support exists in `HeroStageController` but all `championModelPrefab` fields are null. |
| **Confirm before committing** | Accidental selection leads to frustration; every RPG has "Are you sure?" before locking a choice | LOW | Already implemented. Confirm overlay with CONFIRM/CANCEL buttons. Updates description dynamically. |
| **Back navigation to main menu** | Players must be able to go back without penalty; universal pattern | LOW | Already implemented. Back button + NavigationCancel event both route to MainMenu scene. |
| **Gamepad/keyboard navigation** | Console-quality RPGs require controller support; PC gamers increasingly use controllers | MEDIUM | Partially implemented. `NavigationMoveEvent` (left/right), `NavigationSubmitEvent`, `NavigationCancelEvent` are handled. No focus ring visual or d-pad feedback for button highlighting. |
| **Visual hero differentiation** | Each character must feel distinct at a glance -- different colors, silhouettes, energy | MEDIUM | Theme class system exists (`theme-vex`, `theme-seraphina`, etc.) applied to root. Per-hero lighting rig in `HeroStageController`. Per-hero fog tinting in `CharSelectEnvironmentController`. Good foundation but depends on USS rules being complete. |
| **Smooth transitions between heroes** | Snappy, not jerky. Persona 5 set the bar: sub-200ms transitions with style | MEDIUM | Partially implemented. 150ms transition lock exists. USS class toggle for `panel-hidden` with 50ms delay creates slide-in. But no exit animation -- panels just snap to hidden then slide back. Need enter AND exit choreography. |
| **Loading feedback / perceived performance** | Screen must never feel stuck; skeleton/shimmer during data load | LOW | Not implemented. `InitializeWhenReady()` waits for GameDatabase with no visual feedback. Screen is blank or shows stale defaults during the 0-10s load window. |
| **Abilities/skills preview** | Players want to know what their hero can do before committing; Pokemon shows move list, FF shows abilities | LOW | Already implemented. `HeroStatsPanelController` shows 5 ability slots populated from `innate_skills[]` with display name lookup from GameDatabase. |
| **Audio feedback on navigation** | Every button press, hero switch, and embark action needs sound. Silence = broken. Genshin Impact character switch has layered sound design with whoosh + chime + impact | MEDIUM | Not implemented in character select controllers. `AudioManager` and `MusicManager` exist globally but no SFX triggers are wired for hero switching, button clicks, or embark sequence. |

### Differentiators (Competitive Advantage)

Features that make VeilBreakers' character select feel AAA rather than indie. These are what players remember and screenshot.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| **3D hero model with drag-to-rotate** | Persona 5 proved menus can be the star. A rotating 3D hero in a lit stage makes the screen feel cinematic. Players engage longer. | HIGH | Foundation exists in `HeroStageController`: RenderTexture, 5-light rig, drag rotation, placeholder capsule fallback. All `modelPrefab` fields are currently null (placeholder mode). Getting actual 3D models working is the key differentiator unlock. |
| **Per-hero dynamic backgrounds** | Genshin Impact changes backgrounds per region. VeilBreakers already generates per-hero procedural nebula textures and tints fog. When polished, this creates a "whole world changes" feeling on each hero switch. | MEDIUM | Implemented but unpolished. `GenerateNebula()` creates per-hero Perlin noise textures on CPU (GC pressure). Parallax background with 3 layers (deep void, fog, vignette) responds to mouse. Needs: pre-baked textures per hero, smoother color transitions between heroes. |
| **Cinematic overlay system (scanlines, vignette, veil glow)** | Dark Souls, Bloodborne, and Persona 5 all use post-processing-style overlays on menus to create atmosphere. VeilBreakers has a dedicated overlay layer for this. | MEDIUM | UXML structure exists (`cinematic-overlays` with scanlines, vignette, veil-glow elements). Implementation depends on USS styling. Low CPU cost since these are static/animated CSS elements. |
| **Per-hero themed music/ambience** | Each hero having their own musical motif during selection creates emotional connection. Genshin does this with region music; Persona 5's menu music is iconic. | MEDIUM | Not implemented. `MusicManager` has crossfading support. Need: 4 ambient tracks or musical stems that crossfade on hero switch. Could use existing AudioManager infrastructure. |
| **Animated stat bars with stagger** | Stat bars that fill sequentially (like a cascade) rather than all at once feel more polished. Persona 5 uses staggered animations everywhere. | LOW | Stat bars exist with width transitions via USS. Adding stagger is ~50 lines: schedule bar fill updates with increasing delays (50ms, 100ms, 150ms...). Pure USS + schedule approach. |
| **Hero lore/backstory panel** | Pokemon shows Pokedex entries; Fire Emblem shows character bios. A scrollable or paginated lore section deepens engagement. | LOW | `hero-quote` label exists. Could expand to multi-paragraph backstory from HeroData. UXML has room in left panel. Data needs `backstory` field in hero JSON. |
| **Starter monster 3D preview alongside hero** | Showing the champion monster next to the hero in the 3D stage (like Pokemon showing the starter) makes the pair feel like a team. | HIGH | `HeroStageController` already has `championModelPrefab` and `championOffset`/`championScale` support. Needs actual 3D monster models to work. Infrastructure is ready. |
| **Embark sequence cinematic** | Instead of a hard scene cut, play a brief cinematic: screen darkens, hero silhouette illuminates, particles converge, then fade to loading. Persona 5's "Take Your Time" loading screen is legendary. | HIGH | Not implemented. Current embark flow: confirm -> create save -> `SceneManager.LoadScene()`. Could add: USS animation sequence (1-2 seconds) before scene transition using `ScreenTransition`. |
| **Particle effects on hero switch** | Subtle particles (embers, motes, energy wisps) that burst on hero change and settle into the hero's theme color. | MEDIUM | Not implemented. Would need either UI Toolkit particle simulation (custom) or a world-space particle system composited via additional RenderTexture. CSS-only shimmer/glow effects are easier and still impactful. |
| **Glass-morphism panel styling** | Frosted glass panels with backdrop blur create depth and premium feel. Already in the UXML class naming convention (`glass-panel`). | LOW | UXML uses `glass-panel` class on both info panels and confirm popup. USS implementation determines quality. UI Toolkit supports `backdrop-filter` in newer Unity versions -- needs verification for Unity 6000.3.6f1. |
| **Teaser slot for upcoming heroes** | The carousel already has a "?" / "COMING SOON" teaser card. This builds anticipation and signals the game is alive. | LOW | Already implemented in `CarouselController.CreateTeaserCard()`. Just needs visual polish in USS. |

### Anti-Features (Commonly Requested, Often Problematic)

Features that seem good but create complexity without proportional value, or actively harm the experience.

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| **Character creation/customization** | Players want to "make it their own" | VeilBreakers has 4 authored heroes with specific identities, backstories, and game balance. Custom characters would break narrative coherence, require massive art/animation pipelines, and dilute brand identity. Pokemon doesn't let you customize starters; Persona doesn't let you redesign Joker. | Polish the 4 heroes to be memorable. Customization comes through monster team composition and corruption choices, not hero appearance. |
| **Difficulty selection on this screen** | "AAA games have difficulty settings" | Clutters the character select flow. Difficulty is a game-wide setting, not a per-hero decision. Adding it here creates decision paralysis (hero choice + difficulty = 4x3 = 12 combinations to evaluate). | Put difficulty in Settings menu (accessible from MainMenu). Keep character select focused on hero identity. |
| **Detailed damage calculator/theory-crafting** | Min-maxers want to see exact formulas | Overwhelms casual players. The character select is about first impressions and identity, not spreadsheets. Showing too much math makes the game feel like homework. | Show simple stat comparisons (bar charts). Save detailed math for in-game character sheets and strategy guides. |
| **Auto-play/demo mode if idle** | "Fighting games do this" | Fighting games cycle through characters to attract quarters. An RPG character select is visited intentionally, not idled at. Auto-cycling heroes would be disorienting. The parallax background already provides ambient visual interest. | Subtle idle animations on the 3D model (breathing, head turn, idle stance) -- already partially implemented with procedural breathing on placeholder models. |
| **Mini-tutorial or guided selection** | "Help new players choose" | Patronizing for experienced RPG players. If the hero information (stats, abilities, champion, lore) is well-presented, players can make informed decisions without hand-holding. | Add brief, optional tooltip on first visit: "Each hero follows a different Path. Choose based on your playstyle." Dismissible, never forced. |
| **Real-time multiplayer hero locking** | "What if two players pick the same hero" | VeilBreakers is explicitly single-player (stated in PROJECT.md scope). Building multiplayer hero-locking UI is wasted effort. | Out of scope per project constraints. |
| **Excessive screen shake/juice** | "Make it more dynamic" | Constant motion causes fatigue and accessibility issues (motion sensitivity). Persona 5 is stylish but controlled -- every animation has purpose. Random shaking is noise. | Use motion purposefully: slide-in for panels, pulse for embark glow, subtle parallax. Reserve strong effects for the embark confirmation moment. |
| **Loading screen between MainMenu and CharSelect** | "Show a loading bar" | The scene is lightweight (UI + data lookup). Adding a dedicated loading screen for <1s loads makes the game feel slower, not faster. | Use `ScreenTransition` fade (already supported). If data takes >500ms, show a skeleton/shimmer state rather than a separate loading screen. |

## Feature Dependencies

```
[GameDatabase Ready]
    |
    +--requires--> [Hero Data Population]
    |                   |
    |                   +--requires--> [Stat Bars / Abilities Display]
    |                   +--requires--> [Champion Monster Info]
    |                   +--requires--> [Embark Flow (save creation)]
    |
    +--requires--> [Carousel Generation]
                        |
                        +--requires--> [Hero Navigation]
                                            |
                                            +--requires--> [Theme Switching]
                                            +--requires--> [3D Model Swap]
                                            +--requires--> [Background Change]
                                            +--requires--> [Audio Crossfade]

[USS Stylesheet Consolidation]
    |
    +--requires--> [Glass Panel Styling]
    +--requires--> [Transition Animations]
    +--requires--> [Cinematic Overlays]
    +--requires--> [Stat Bar Animations]

[3D Hero Models (Art Asset)]
    |
    +--requires--> [3D Model Preview] (currently placeholder capsules)
    +--requires--> [Champion Monster 3D Preview]
    +--requires--> [Drag-to-Rotate Interaction]
    +--requires--> [Hero-Specific Idle Animations]

[Audio Assets (SFX + Music)]
    |
    +--requires--> [Navigation SFX]
    +--requires--> [Per-Hero Ambience/Music]
    +--requires--> [Embark Sequence Audio]

[InputManager Integration]
    |
    +--requires--> [Gamepad Focus Ring]
    +--requires--> [Parallax via InputManager] (fix legacy Input.mousePosition)

[Transition Animation System]
    |
    +--enhances--> [Hero Switch Feel]
    +--enhances--> [Embark Sequence Cinematic]
    +--enhances--> [Panel Slide Choreography]

[Loading State Feedback] --enhances--> [Perceived Performance]

[Per-Hero Music] --conflicts--> [Single Background Track] (must choose one approach)
```

### Dependency Notes

- **Hero Data Population requires GameDatabase Ready:** All downstream UI population waits on `InitializeWhenReady()` coroutine. Without data, nothing renders.
- **USS Consolidation is a prerequisite for all visual polish:** Currently 4 duplicate stylesheets exist (CONCERNS.md). Any visual feature work will be confused by conflicting styles until consolidated to a single canonical file.
- **3D Models are an art pipeline dependency:** The entire 3D preview differentiator is blocked on having actual hero and monster models. All code infrastructure exists but serves capsule placeholders. This is the single biggest art dependency.
- **Audio is a separate asset pipeline:** Navigation SFX and per-hero music require audio assets that don't exist yet. The code integration (wiring `AudioManager.PlaySFX()` calls) is straightforward once assets exist.
- **Per-Hero Music conflicts with Single Background Track:** Must decide: one ambient track for the whole screen (simpler) or crossfading per-hero themes (more premium). Recommend per-hero themes because the `MusicManager` already supports crossfading.

## MVP Definition

### Launch With (v1) -- Functional & Clean

Minimum viable character select that works correctly and feels intentional.

- [x] Hero navigation (prev/next/carousel click) -- already working
- [x] Hero identity display (name/title/role/quote) -- already working
- [x] Stat preview (both panels) -- already working
- [x] Confirm/cancel flow -- already working
- [x] Back to main menu -- already working
- [ ] **Fix: USS stylesheet consolidation** (4 files -> 1 canonical) -- blocks all visual work
- [ ] **Fix: Loading state feedback** (shimmer/skeleton while GameDatabase loads) -- prevents blank screen on slow load
- [ ] **Fix: Audio feedback on navigation** (click SFX, hero switch whoosh, embark confirm sound) -- silence feels broken
- [ ] **Fix: Gamepad focus ring** (visible highlight on focused button) -- controller users can't see what's selected
- [ ] **Fix: Legacy Input.mousePosition** (route through InputManager) -- consistency, future-proofing
- [ ] **Fix: Panel exit animations** (slide-out before slide-in, not snap-then-slide) -- polish

### Add After Validation (v1.x) -- Premium Feel

Features to add once the foundation is solid and clean.

- [ ] **Animated stat bar stagger** -- trigger when hero changes, cascading fill delays
- [ ] **Per-hero dynamic background polish** -- pre-bake nebula textures (eliminate GC allocation), smooth color transitions
- [ ] **Cinematic overlay tuning** -- scanline opacity, vignette animation, veil glow pulse
- [ ] **Glass-morphism panel blur** -- if Unity 6000.3.6f1 supports `backdrop-filter` in USS
- [ ] **Embark sequence cinematic** -- 1-2 second animation before scene transition
- [ ] **Per-hero ambient music** -- 4 tracks that crossfade on hero switch via MusicManager
- [ ] **Hero lore expansion** -- backstory text in left panel, loaded from hero JSON data
- [ ] **Starter monster visual preview** -- even a 2D sprite/icon in the champion section improves it

### Future Consideration (v2+) -- Full AAA

Features to defer until 3D art pipeline is active.

- [ ] **3D hero models replacing placeholders** -- the single biggest visual upgrade, blocked on art
- [ ] **3D champion monster models** -- infrastructure ready, needs monster model assets
- [ ] **Hero-specific idle animations** -- requires rigged models + Animator controllers per hero
- [ ] **Particle effects on hero switch** -- energy burst, color-themed motes
- [ ] **Advanced camera choreography** -- zoom, pan, or dolly on hero switch for cinematic feel

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|---------------------|----------|
| USS consolidation (4->1) | HIGH (unblocks everything) | LOW | **P1** |
| Loading state feedback | HIGH (prevents blank screen) | LOW | **P1** |
| Audio feedback (SFX) | HIGH (silence = broken) | LOW (wiring existing system) | **P1** |
| Gamepad focus ring | HIGH (controller unusable without it) | LOW | **P1** |
| Legacy Input fix | MEDIUM (consistency) | LOW | **P1** |
| Panel exit animations | MEDIUM (polish) | LOW | **P1** |
| Stat bar stagger animation | MEDIUM (premium feel) | LOW | **P2** |
| Per-hero background polish | MEDIUM (atmosphere) | MEDIUM (pre-bake textures) | **P2** |
| Cinematic overlays tuning | MEDIUM (atmosphere) | LOW | **P2** |
| Per-hero ambient music | MEDIUM (emotional connection) | MEDIUM (needs audio assets) | **P2** |
| Glass-morphism blur | LOW-MEDIUM (visual depth) | LOW (if USS supports it) | **P2** |
| Embark sequence cinematic | MEDIUM (memorable moment) | MEDIUM | **P2** |
| Hero lore expansion | LOW-MEDIUM (engagement) | LOW (data + layout) | **P2** |
| Starter monster 2D preview | MEDIUM (monster RPG identity) | LOW (sprite in champion section) | **P2** |
| 3D hero models | HIGH (game-changing) | HIGH (art pipeline) | **P3** |
| 3D champion monsters | MEDIUM (team preview) | HIGH (art pipeline) | **P3** |
| Hero idle animations | MEDIUM (life) | HIGH (rigging + animation) | **P3** |
| Hero switch particles | LOW (juice) | MEDIUM | **P3** |

**Priority key:**
- **P1:** Must fix for functional, clean character select (bugs + baseline UX)
- **P2:** Should add for premium feel (polish + atmosphere)
- **P3:** Future when 3D art pipeline delivers assets

## Competitor Feature Analysis

| Feature | Pokemon (SV) | Persona 5 | Final Fantasy (XVI) | Genshin Impact | VeilBreakers (Current) | VeilBreakers (Target) |
|---------|-------------|-----------|-------------------|----------------|----------------------|---------------------|
| Hero/starter visual | 3D model + animation | 2D art with motion | 3D cinematic | 3D model + idle anim | Placeholder capsule | 3D model (v2+), 2D art (v1.x) |
| Stat preview | Type + nature hints | Full stat sheet | Job abilities | Full stat page | Dual stat panels | Keep dual panels, add stagger |
| Theme per character | Type-colored UI | Red+black universal | Class-themed | Element-colored | Per-hero theme class | Polish theme transitions |
| Transition style | Slide/fade | Flashy angular wipes | Cinematic cuts | Card flip | 150ms lock + slide-in | Enter+exit choreography |
| Audio on switch | Type chime | Menu percussion | Orchestral sting | Element whoosh | None | Layered SFX + music crossfade |
| Confirmation | "Are you sure?" | - | - | - | Overlay popup | Keep, add animation |
| Background change | Static | Animated abstract | Static scene | Region-based | Procedural nebula + parallax | Pre-baked + smooth transitions |
| Companion preview | Full 3D starter | Persona preview | Summon preview | - | Text only | 2D sprite (v1.x), 3D (v2+) |
| Controller support | Full | Full | Full | Full (console) | Partial (no focus ring) | Full with visual feedback |
| Lore/backstory | Pokedex entry | Social links context | - | Character story | Quote only | Expandable lore section |

## Sources

- [Game UI Database - Character Select](https://www.gameuidatabase.com/index.php?scrn=41) -- Comprehensive visual reference for 1,300+ games (HIGH confidence)
- [Persona 5 UI/UX Analysis - Ridwan Khan](https://ridwankhan.com/the-ui-and-ux-of-persona-5-183180eb7cce) -- Design breakdown of what makes P5 menus premium (MEDIUM confidence)
- [Persona 5 UI Style & Substance - Design Bootcamp](https://medium.com/design-bootcamp/how-persona-5s-ui-balances-both-style-and-substance-de8cb1b807ef) -- How P5 balances flash with readability (MEDIUM confidence)
- [Xbox Accessibility Guideline 107 - Microsoft](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/107) -- Controller navigation and accessibility standards (HIGH confidence)
- [Game Accessibility Guidelines](https://gameaccessibilityguidelines.com/full-list/) -- Comprehensive accessibility checklist (HIGH confidence)
- [Unity USS Transitions](https://docs.unity3d.com/6000.2/Documentation/Manual/UIE-Transitions.html) -- Official docs for UI Toolkit animation (HIGH confidence)
- [Genshin Impact Character Switch Sound Design](https://www.daviddumaisaudio.com/genshin-impact-character-switch-sound-design-tutorial/) -- Layered audio design for character switching (MEDIUM confidence)
- [Adaptive Audio for Game Designers - Gamasutra](https://www.gamedeveloper.com/audio/design-with-music-in-mind-a-guide-to-adaptive-audio-for-game-designers) -- Music transition techniques (MEDIUM confidence)
- [Skeleton Screens - NNGroup](https://www.nngroup.com/articles/skeleton-screens/) -- Perceived performance through loading states (HIGH confidence)
- [Glassmorphism UI Best Practices](https://uxpilot.ai/blogs/glassmorphism-ui) -- Glass panel design patterns (MEDIUM confidence)
- Existing codebase analysis: `Assets/Scripts/UI/CharacterSelect/` (8 files), `Assets/UI/Screens/CharacterSelect.uxml`, `Assets/UI/Styles/CharacterSelect.uss` (HIGH confidence -- direct code review)

---
*Feature research for: VeilBreakers 3D Character Select Screen*
*Researched: 2026-02-21*
