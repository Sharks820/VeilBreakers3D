# Phase 3: Controller Behavior - Context

**Gathered:** 2026-03-18
**Status:** Ready for planning
**Quality Bar:** AAA — absolute code strength, flawless functionality, zero compromise

<domain>
## Phase Boundary

Wire all character select interactions to be fully functional with both mouse and gamepad input. Implement hold-to-embark flow, per-hero audio feedback, skeleton loading states, and zero-GC hero switching. Restructure the screen layout from symmetric dual-panel to rule-of-thirds composition with shared hero+monster 3D stage. Replace glass-morphism with veil-torn dark fantasy materials.

This phase delivers BEHAVIOR + LAYOUT RESTRUCTURE. Visual polish animations (PrimeTween, post-processing, cinematic overlays) remain in Phase 4.

</domain>

<decisions>
## Implementation Decisions

### Screen Layout — Rule of Thirds Composition
- Hero 3D stage takes **50% screen width, full height** (left half)
- Starter monster appears **in the same 3D stage** beside/behind the hero (shared camera)
- Hero info floats on the **right half** with layered tab system
- Embark button positioned **bottom-right** of info area
- Carousel strip remains at **screen bottom**
- Back button stays **top-left**
- Replaces the current symmetric left-panel / center-hero / right-panel layout
- Composition reference: Destiny 2 character select, BG3 companion preview

### Info Panel — Tabbed Layered Hierarchy
- Right-side info uses **L1/R1 tabbed sections** (not all info at once)
- **Tab 1 — Overview (default):** Hero name, title, quote, path, role, game stat bars (HP/ATK/DEF/SPD), starter monster name + brand + synergy tier
- **Tab 2 — Abilities:** 5 ability slots with names and brief descriptions
- **Tab 3 — Lore:** Hero backstory, synergy explanation, brands detail
- Tabs switch with L1/R1 on gamepad, clickable tab headers on mouse
- Reduces "wall of text" problem identified by Gemini review

### Panel Materials — Veil-Torn Dark Fantasy
- Replace glass-morphism panels with **obsidian/iron textured borders**
- Veil cracks in borders glow with **per-hero accent colors:**
  - Vex = amber/iron glow
  - Seraphina = violet/void glow
  - Orion = crimson/fang glow
  - Nyx = cyan/chaos glow
- Panels have subtle weathered stone/leather texture backgrounds
- Reference: Diablo IV menus, Elden Ring UI materials

### Gamepad Navigation — Linear Snap
- **D-pad up/down:** Moves focus between zones: Back → Info tabs → Embark → Carousel
- **D-pad left/right:** Navigates within carousel (select hero cards)
- **L1/R1 (shoulder buttons):** Switch hero directly (bypasses carousel), also cycles info tabs when info area is focused
- **Right Stick:** Rotates 3D hero model (always active regardless of focus)
- **A/Cross (hold 1.5s):** Embark with circular progress fill
- **B/Circle:** Back to main menu (always active)
- No virtual cursor — pure snap navigation
- Visible **focus ring** with high-contrast soul flame / gold outline on focused element
- Reference: BG3, Destiny 2 snap navigation

### Embark Flow — Hold-to-Confirm
- **Replaces** the current click → confirm popup flow entirely
- Player **holds A/Cross for 1.5 seconds** with visible circular progress fill
- Release before completion = cancel, progress bar resets
- On completion: cinematic transition begins (audio swell + visual flare)
- Mouse equivalent: click and hold embark button
- Confirm popup overlay (`confirm-overlay`) is **removed from UXML**
- btn-confirm and btn-cancel buttons are **removed**
- Reference: Overwatch 2 ready-up, fighting game countdowns

### Audio Feedback — Per-Hero Themed SFX
- Each hero switch plays **brand-specific audio:**
  - Vex → iron clank + chain rattle
  - Seraphina → void shimmer + crystal hum
  - Orion → fang slash + predator growl
  - Nyx → glitch static + chaos pulse
- Navigation actions have **consistent SFX:**
  - Nav arrow click → soft tick
  - Card hover → subtle whoosh
  - Embark hold → building tension drone with rising pitch
  - Embark complete → epic stinger + hero select quote
  - Back → reverse whoosh
  - Error/deny → low deny buzz
- Missing audio assets: use **placeholder tones** (generate simple synth sounds), never silent
- Audio plays through AudioManager with SettingsManager volume respect

### Loading & Error States
- During GameDatabase initialization: **skeleton shimmer** on stat bars and text fields
- Stat bars show as animated placeholder shapes with shimmer sweep
- On async embark failure: **dark toast notification** slides up from bottom
- Toast contains error message + [Retry] + [Back to Menu] buttons
- Never show a blank/frozen screen — always provide visual feedback
- Embark timeout: 10 seconds, then auto-show error toast

### Performance — Zero-GC Hero Switch
- All VisualElement Q() queries cached at initialization
- Zero Q() calls in Update or any per-frame hot path
- Panel transitions use exit-then-enter choreography (slide-out completes before slide-in begins)
- Pre-bake nebula textures (eliminate per-switch Color[65536] allocation)
- Reuse pre-allocated lists/buffers for any iteration
- All WaitForSeconds cached as instance fields
- No LINQ in any controller code

### Code Quality — AAA Standard
- Every public method has XML doc comments
- Every field follows VeilBreakers conventions (_private, kConstant, PascalProperty, OnEvent)
- Every event subscription has a matching unsubscription (cached ref pattern for singleton safety)
- Every coroutine is tracked and stopped in OnDisable/OnDestroy
- Zero compiler warnings in Phase 3 modified files
- All input goes through InputManager (no direct Input System or legacy Input calls)
- Defensive null checks on all VisualElement queries with Debug.Assert validation

### Claude's Discretion
- Exact tab switching animation (slide vs crossfade)
- Skeleton shimmer implementation technique (USS animation vs C# coroutine)
- Exact hold-to-confirm progress visual (circular, linear bar, or hex fill)
- How to position monster relative to hero in shared 3D stage
- Toast notification exact styling and animation
- Focus ring exact visual treatment (glow intensity, animation)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase Requirements
- `.planning/REQUIREMENTS.md` — CTRL-01 through CTRL-09 define acceptance criteria
- `.planning/ROADMAP.md` §Phase 3 — Goal, dependencies, success criteria

### Architecture & Patterns
- `.planning/codebase/CONVENTIONS.md` — Naming, event patterns, field prefixes
- `.planning/codebase/ARCHITECTURE.md` — System architecture, singleton patterns
- `.planning/codebase/INTEGRATIONS.md` — How systems connect

### Prior Phase Work
- `.planning/phases/01-foundation-cleanup/01-01-SUMMARY.md` — USS consolidation decisions
- `.planning/phases/01-foundation-cleanup/01-02-SUMMARY.md` — Foundation cleanup, event lifecycle
- `.planning/phases/02-layout-structure/02-01-SUMMARY.md` — UXML structure, content population
- `.planning/phases/02-layout-structure/02-02-SUMMARY.md` — USS transitions, UsageHints

### Key Source Files
- `Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs` — Main orchestrator (has btn constants, click handlers)
- `Assets/Scripts/UI/CharacterSelect/CarouselController.cs` — Hero card carousel
- `Assets/Scripts/UI/CharacterSelect/CharSelectEvents.cs` — Scoped event bus
- `Assets/Scripts/UI/CharacterSelect/HeroStageController.cs` — 3D stage rendering
- `Assets/Scripts/UI/CharacterSelect/HeroDataPanelController.cs` — Info panel population
- `Assets/Scripts/UI/CharacterSelect/HeroStatsPanelController.cs` — Stats display
- `Assets/Scripts/Core/InputManager.cs` — Input handling (gamepad + keyboard + mouse)
- `Assets/Scripts/Audio/AudioManager.cs` — Audio playback system
- `Assets/UI/Screens/CharacterSelect.uxml` — Current UXML layout (will be restructured)
- `Assets/UI/Styles/CharacterSelect.uss` — Character select styles

### Gemini UI Review (2026-03-18)
- Gemini recommended: rule-of-thirds composition, material shift from glass to diegetic dark fantasy, layered stat hierarchy, per-hero micro-transitions, hold-to-embark pattern
- Full review captured in this context file's decisions

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `CharacterSelectManager.cs` — Already has all 6 button constants (kBtnPrev/Next/Back/Embark/Confirm/Cancel) and click handler wiring with proper register/unregister
- `CharSelectEvents.cs` — Scoped event bus with OnEmbarkRequested/Confirmed/Cancelled, OnHeroChanged, OnNavigationRequested. ClearAll() cleanup on scene unload
- `InputManager.cs` — Full gamepad support with action map switching, OnActionTriggered event, GetActionDown/GetMouseButtonDown helpers
- `HeroStageController.cs` — 3D render texture stage with camera, already handles model positioning. Can be extended for monster co-placement
- `ButtonVFXHelper.cs` — AAA button effects (click burst, shimmer, charge, focus, breathing) ready to apply
- `AudioManager.cs` — Singleton with zone-based bank loading, volume control, battle integration pattern to follow

### Established Patterns
- **Scoped Event Bus:** CharSelectEvents for decoupled controller communication — use this, don't add direct references
- **Singleton access:** `SingletonMonoBehaviour<T>.HasInstance` check before `.Instance` access — mandatory pattern
- **Event cleanup:** Cache singleton reference at subscribe time, use cached ref for unsubscribe (prevents leak when singleton destroyed first)
- **USS transitions:** GPU-safe properties only (translate, scale, rotate, opacity, color). No width/height/margin animations
- **UsageHints:** DynamicTransform | DynamicColor on all animated VisualElements at creation time

### Integration Points
- `GameDatabase.Instance.InitializationTask` — Must be awaited before populating hero data
- `SaveManager.Instance.LoadAsync()` — For embark flow game state setup
- `ThemeManager.Instance.GetBrandColor()` — For per-hero accent colors on veil-torn borders
- `VBSceneManager` — For scene transitions after embark
- `HeroDisplayConfig` ScriptableObjects in `Resources/CharacterSelect/` — Per-hero visual configuration

</code_context>

<specifics>
## Specific Ideas

- "I feel our entire character selection + start screen is rather rushed and lackluster for a game of our caliber" — user wants AAA-tier visual and interaction quality, not just functional
- Hero and starter monster sharing one 3D stage like BG3 companion preview — feels like a team selection, not just a character picker
- Hold-to-embark with rising audio tension creates a "moment of commitment" (Gemini's phrase)
- Per-hero UI personality: when switching to Nyx, scanlines should glitch. When switching to Vex, borders should feel heavier/iron
- The layout shift from symmetric to rule-of-thirds is the single biggest visual upgrade
- "Absolute amazing code strength, amazing design, AAA quality, and total and pure functionality" — every line must be production-grade

</specifics>

<deferred>
## Deferred Ideas

- **PrimeTween orchestrated animations** — Phase 4 (requires PrimeTween installation)
- **Per-hero URP post-processing profiles** — Phase 4 (Bloom, DoF, Vignette per hero)
- **Per-hero ambient music crossfade** — Phase 4 (MusicManager integration)
- **Cinematic embark sequence** — Phase 4 (1-2s animation before scene transition)
- **Title screen "zoom into portal" transition** — Phase 5 (seamless loading)
- **Title screen audio-reactive logo pulse** — Phase 5 (music sync)
- **Per-hero bespoke 3D environments** — v2 (art pipeline dependency)

</deferred>

---

*Phase: 03-controller-behavior*
*Context gathered: 2026-03-18*
