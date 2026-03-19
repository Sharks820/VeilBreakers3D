# Phase 4: Visual Amplification - Context

**Gathered:** 2026-03-19
**Status:** Ready for planning
**Quality Bar:** AAA — cinematic, premium-feeling character select

<domain>
## Phase Boundary

Deliver cinematic visual polish to the character select screen: orchestrated PrimeTween animations, per-hero URP post-processing, atmospheric overlays, dissolve/materialize shader, embark cinematic sequence, per-hero music parameter crossfade, and polished micro-interactions. All visual systems are driven by a unified HeroThemeConfig ScriptableObject per hero.

**Core principle:** Hero model + starter monster are ALWAYS the primary visual focus. Every other system (particles, post-process, overlays, lighting) exists to amplify them, never compete.

This phase delivers VISUAL POLISH on top of Phase 3's working behavior. No new functionality — only visual enhancement of existing interactions.

</domain>

<decisions>
## Implementation Decisions

### Animation Library — PrimeTween
- Install PrimeTween 1.3.7 via Package Manager (VISUAL-01)
- PrimeTween drives orchestrated sequences: panel slides, stat bar cascades, embark cinematic, card snaps
- USS transitions handle state changes: color/opacity swaps, theme class toggling, hover/focus
- ButtonVFXHelper.schedule.Execute() handles continuous VFX loops: breathing, shimmer, pulse
- Three animation systems, clear responsibilities, no overlap

### Hero Switch — Full Staggered Sequence (~1.2s)
- **Screen load:** Dramatic panel entrance animation (staggered slide-in). Plays ONCE on first visit.
- **Hero switch:** Lighter content swap — no panel re-entrance. Staggered timeline:
  - t=0ms: Info panel fades out + music crossfade starts
  - t=100ms: Veil pulse flash (full-screen, hero accent color, 0.1 opacity, 200ms fade)
  - t=200ms: 3D model dissolve begins + post-process lerp starts (delayed 200ms for drama) + old particles fade
  - t=400ms: New hero materializes from veil energy + new particles begin spawning
  - t=600ms: Info panel fades in with new hero data + overlays shift to new hero values
  - t=800ms: Stat bars cascade fill (HP → ATK → DEF → SPD, 100ms apart, direct fill to stat value)
  - t=800ms: Hero name/title glitch text reveal (scramble → resolve, ~250ms)
- Post-process lerp: 0.8s duration, starts at t=200ms (delayed from hero switch trigger)
- Music crossfade: starts at t=0ms, ~2s natural duration via MusicManager parameter lerp

### Screen Entry — Staggered Panel Entrance
- Screen starts neutral/dark (no hero profile active)
- t=0ms: 3D stage fades in (center)
- t=100ms: Left panel (3D stage frame) slides from left edge
- t=200ms: Right info panel slides from right edge
- t=350ms: Carousel rises from bottom
- t=500ms: Overlays fade in + post-process lerps to first hero's (Vex) profile
- Entrance plays ONCE. Hero switching uses the lighter sequence above.

### Stat Bar Animation
- Direct fill to hero's stat value (HP fills to 80%, ATK to 65%, etc.)
- Staggered cascade: each bar starts 100ms after the previous
- No overshoot/bounce — clean, informative, readable

### Glitch Text Reveal
- Hero name and subtitle scramble through glitch characters (unicode block chars) then resolve character-by-character
- ~250ms total resolve time, ~50ms per character
- Per-hero flavor: Nyx resolves slower (more glitch frames), Vex resolves faster (cleaner)
- Glitch charset: block drawing characters for VeilBreakers veil-corruption theme
- Applied to hero name + title on hero switch AND to centered name flash during embark cinematic

### Carousel Card Animation
- Selected card: scale 1.15x, rises slightly (translateY -5px), border glow + shadow, breathing animation
- Unselected cards: scale 0.9x, dimmed (opacity 0.6), static (no breathing)
- Selection transition: 300ms PrimeTween with OutCubic ease for grow, InCubic for shrink
- Only the active hero card breathes — draws attention to selection

### Tab Switching (L1/R1)
- Crossfade transition (200ms opacity fade out/in)
- Tab header highlight swaps
- Does not compete with hero switch animation — intentionally lighter

### 3D Model Dissolve/Materialize — VeilDissolve Shader
- Universal URP Lit-based shader: "VeilBreakers/VeilDissolve"
- Properties: _DissolveThreshold (animated 0→1 or 1→0), _DissolveEdgeColor (hero accent from HeroThemeConfig), _DissolveEdgeWidth, _NoiseTexture, _NoiseScale, _EmissionIntensity
- Dissolve OUT: threshold 0→1 over 400ms (hero disappears into veil energy)
- Dissolve IN: threshold 1→0 over 400ms (hero materializes from veil energy)
- HDR glow on dissolve edge + particle emission from dissolving edges (hero accent color)
- Per-hero customization via MaterialPropertyBlock: Vex=amber/medium noise, Seraphina=violet/fine noise, Orion=crimson/coarse noise, Nyx=cyan/chaotic high noise
- Reusable for monsters, items, any future veil-themed VFX

### Per-Hero Idle Animations (Placeholder Capsules)
- Set up Animator controllers + idle clips for current capsule placeholders
- Per-hero personality: Vex=confident/grounded, Seraphina=ethereal float/sway, Orion=predatory weight shift, Nyx=restless fidget with glitch-flicker
- Real model idle clips slot in later (v2 ART-04). System works regardless of model geometry.
- Power pose animation per hero triggers on embark completion (before cinematic)

### Subtle Panel Parallax
- UI panels shift 3-5px based on mouse position / right stick
- Overlay layers shift at different rates for depth (see Overlay section)
- ParallaxBackground already exists in codebase — extend to UI panels
- 3D stage uses camera orbit (right stick, already implemented in Phase 3)

### Post-Processing — Strong Per-Hero Identity

**VEX (Ironbound):** Forge heat, molten determination
- Bloom: warm amber, intensity 1.2
- Color: warm shift (+0.15 temp), shadows bronze, highlights gold
- Vignette: heavy (0.4), warm brown edge
- DoF: medium background blur

**SERAPHINA (Voidtouched):** Ethereal void, otherworldly calm
- Bloom: violet/white, intensity 1.5 (dreamier)
- Color: cool shift (-0.1 temp), shadows deep purple, highlights white-lavender
- Vignette: soft (0.25), violet-black edge
- DoF: shallow — soft dreamy background

**ORION (Fangborn):** Predatory intensity, blood moon hunt
- Bloom: crimson/red, intensity 1.0 (controlled)
- Color: warm-red shift, deep shadows, highlights scarlet
- Vignette: tight (0.5), dark red-black (predator tunnel vision)
- DoF: deep — sharp foreground, blurred background

**NYX (Unchained):** Digital chaos, veil corruption
- Bloom: cyan/electric, intensity 1.3 (harsh digital glow)
- Color: desaturated base (-0.2 sat), cyan highlights, dark shadows
- Vignette: glitchy (0.35), pulsing edge
- DoF: pulsing — focus shifts subtly (unstable reality)
- Chromatic aberration: subtle (2-3px RGB offset), UNIQUE to Nyx

- 4 separate VolumeProfile ScriptableObject assets (one per hero in Resources/CharacterSelect/HeroThemes/)
- Lerped via VolumeProfileTransitioner over 0.8s
- Per-hero DoF included in lerp
- Screen entry: brief neutral state, then lerp to first hero's profile during entrance animation

### Per-Hero Stage Lighting
- Fill light (front-left) + rim light (back-right) + ambient, all tinted per hero
- Vex: warm white fill (0.8), amber rim (1.2), dark bronze ambient
- Seraphina: cool white fill (0.7), violet rim (1.4), deep purple ambient
- Orion: warm red-white fill (0.9), crimson rim (1.0), dark red ambient
- Nyx: cool blue-white fill (0.6), cyan rim (1.5, FLICKERING), near-black w/cyan ambient
- Nyx's rim light flickers: random 0.8-1.5 intensity every 0.1-0.3s (unstable reality)
- Rim light has synced breathing pulse (95-105% intensity) matching carousel card + veil glow — all heroes
- Light colors lerp over 0.6s on hero switch

### Per-Hero Environment Particles (3D Stage Background)
- URP particle systems in the 3D hero stage (real 3D, not 2D overlays)
- Vex: forge embers (orange/amber particles, slow upward drift, subtle heat shimmer, warm floor fog)
- Seraphina: void crystals (violet/white shards, slow rotate/float, faint inner glow, mist at base)
- Orion: blood moon haze (red fog/mist, faint red moon, drifting particles, dark ground fog)
- Nyx: digital rain (cyan falling characters, matrix-style columns, glitch distortion, occasional flash)
- Max 200 particles per hero, GPU instanced for performance
- Gradual spawn-in: particles reach full density over 0.5s (synced with dissolve-in)
- Old particles fade out as new system activates on hero switch
- Hero + monster remain the PRIMARY visual — particles are atmospheric backdrop only

### Starter Monster Brand Aura
- Subtle particle/glow effect in monster's brand color around its body
- Reinforces the brand system visually without competing with hero (hero is taller, more lit)
- Style: Claude's discretion (ground ring + wisps or outline glow — whatever reads best at monster scale)

### Music — Parameter-Driven Per-Hero Variations
- One adaptive base track: "CharacterSelect_Ambient" (layered/adaptive)
- MusicManager.SetParameter() shifts instrument layers per hero on switch
- Vex: boost percussion + warmth (forge rhythm)
- Seraphina: boost pads + filter (ethereal wash)
- Orion: boost percussion + tension (tribal intensity)
- Nyx: boost synth + filter (electronic glitch feel)
- Lerp speed: existing _parameterLerpSpeed = 3f (~2s natural crossfade)
- No audio glitch effects for Nyx — music stays clean. Visual systems carry her glitch identity.

### HeroThemeConfig ScriptableObject (Unified Data Source)
- One ScriptableObject per hero bundling ALL visual identity:
  - Colors (primary, glow, dark, dissolve edge)
  - Volume Profile reference
  - Chromatic aberration intensity (0 for all except Nyx)
  - Lighting (fill/rim/ambient colors and intensities, rim flicker bool)
  - Music parameter set (intensity, warmth, tension, synth, perc, pad, filter)
  - Background particle prefab reference + max particle count
  - Monster aura color
  - Dissolve noise scale + duration
  - Overlay intensity values (scanline opacity, vignette intensity, veil glow opacity)
  - Glitch text resolve speed
- Location: Assets/Resources/CharacterSelect/HeroThemes/
- Consumer: HeroThemeTransitioner.cs reads active theme and drives ALL visual systems
- All systems pull from this single data source = guaranteed cross-system consistency

### Embark Flow — Veil Shatter Cinematic

**Hold Phase (1.5s, Phase 3 built the hold mechanism):**
- Embark button has continuous breathing glow while idle (VISUAL-08)
- During hold: border glow intensifies, color shifts toward hero accent
- At 80%: micro-shake begins (1-2px — subliminal, not gamey)
- Overlays intensify in parallel (scanlines +50%, vignette tightens, veil glow +50%, bloom +40%)
- On release before 100%: quick deflate with spark scatter (accumulated energy scatters from button, 200ms)

**Cinematic Sequence (1.2-1.5s, Phase 4 builds this):**
- t=0ms: Full-screen hero accent flash (0.8 opacity, 150ms fade) + epic stinger + hero select quote (placeholder synth TTS) + hero power pose animation triggers
- t=100ms: Camera dollies into hero (FOV narrows 60→40) + bloom cranks up
- t=300ms: All UI panels dismiss (slide off-screen in natural directions, 200ms PrimeTween)
- t=500ms: Procedural veil cracks spread from screen center (VeilCrack shader, noise-based unique pattern each time) + cracks glow in hero accent HDR color
- t=500ms: Per-hero variation: Nyx=crack flicker, Vex=ember particles on crack edges
- t=600ms: Hero name + title flash large and centered (glitch text reveal) in hero accent glow
- t=800ms: Cracks explode outward into bright fragments + shattering veil tear SFX + name text shatters with veil
- t=800ms: Monster's dissolve particles flow TOWARD the hero (absorbed into hero, reinforcing bond)
- t=1000ms: Pure white overlay (opacity 1.0)
- t=1200ms: VBSceneManager.LoadScene("Overworld") — white overlay fades during load
- Hero select quotes: Claude writes thematic lines per hero personality. Placeholder synth TTS voices.

### Reusable VeilTransition System
- VeilTransitionController : MonoBehaviour with public API:
  - PlayShatter(Color accentColor, float duration)
  - PlayMaterialize(Color accentColor, float duration)
  - PlayCrackSpread(float progress, Color color)
  - OnTransitionComplete event
- Embark uses it in Phase 4. Title screen can reuse it in Phase 5.
- Internally: full-screen quad with VeilCrack shader + particle system + post-process override + PrimeTween orchestration

### Cinematic Overlays — Per-Hero Intensity

**VEX:** Clean, grounded (40% intensity)
- Scanlines: 0.05 opacity (subtle)
- Vignette: medium (border 80px, 0.25 intensity)
- Veil glow: warm amber wash (0.08 opacity)

**SERAPHINA:** Ethereal, clean (30% intensity)
- Scanlines: 0.02 opacity (barely there)
- Vignette: soft (border 60px, 0.15 intensity)
- Veil glow: violet mist (0.10 opacity)

**ORION:** Gritty, raw (50% intensity)
- Scanlines: 0.06 opacity (noticeable)
- Vignette: tight (border 100px, 0.40 intensity)
- Veil glow: blood red wash (0.06 opacity)

**NYX:** Digital/broken (80% intensity)
- Scanlines: 0.12 opacity (prominent)
- Vignette: heavy (border 90px, 0.35 intensity)
- Veil glow: cyan static (0.12, pulsing)
- Special: scanline "jump" glitch every 4-8s (random interval, 2px vertical shift for 100ms)

- Overlay values stored in HeroThemeConfig SO (same data source as everything else)
- Scanlines: USS repeating-linear-gradient (no texture file needed)
- All overlays: pointer-events: none, z-index 90-95 above UI content
- Overlays fade in with screen entrance animation (t=500ms in entrance sequence)
- Veil glow has subtle breathing pulse (synced with carousel card + rim light)
- Inactive panels get dim + desaturate (neutral, not hero-tinted) for VISUAL-07 depth hierarchy
- Overlays intensify during embark hold (proportional to hold progress 0-100%)

### Layered Parallax Depth
- Mouse/right stick input drives per-layer translate offsets:
  - Veil glow: 0.5px (deepest, barely moves)
  - Scanlines: 1.0px (far)
  - Vignette: 2.0px (mid)
  - UI panels: 3-5px (near, most movement)
  - 3D stage: camera orbit (separate, right stick)
- Creates "looking through layers of glass in the veil" effect
- Very subtle — enhances 3D feel without being gimmicky

### Claude's Discretion
- Monster brand aura visual style (ground ring + wisps vs outline glow)
- Exact PrimeTween easing curves for each animation segment
- Placeholder capsule idle animation specifics (rotation speed, bob amplitude)
- Procedural noise parameters for veil crack patterns
- Embark hero select quote text per hero
- Exact VeilDissolve particle emission rate and lifetime
- How embark power pose looks on placeholder capsules
- Exact parallax sensitivity curve for mouse vs stick input

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase Requirements
- `.planning/REQUIREMENTS.md` — VISUAL-01 through VISUAL-09 define acceptance criteria
- `.planning/ROADMAP.md` Phase 4 section — Goal, dependencies, success criteria

### Architecture & Patterns
- `.planning/codebase/CONVENTIONS.md` — Naming (k prefix, _ private, On events), section separators, ScriptableObject patterns
- `.planning/codebase/STACK.md` — Unity 6000.3.6f1, URP 17.3.0, Input System 1.18.0, UI Toolkit
- `.planning/codebase/ARCHITECTURE.md` — System architecture, singleton patterns, event bus

### Prior Phase Work
- `.planning/phases/03-controller-behavior/03-CONTEXT.md` — Phase 3 decisions: rule-of-thirds layout, tabbed info panel, hold-to-embark, per-hero accent colors (Vex=amber, Seraphina=violet, Orion=crimson, Nyx=cyan), veil-torn dark fantasy materials

### Key Source Files (Reuse Targets)
- `Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs` — Main orchestrator, hero switch flow
- `Assets/Scripts/UI/CharacterSelect/HoldToEmbarkController.cs` — Hold-to-embark mechanism (Phase 3)
- `Assets/Scripts/UI/CharacterSelect/HeroStageController.cs` — 3D render texture stage, camera, model positioning
- `Assets/Scripts/UI/CharacterSelect/HeroDataPanelController.cs` — Info panel population
- `Assets/Scripts/UI/CharacterSelect/HeroStatsPanelController.cs` — Stats display (stat bars)
- `Assets/Scripts/UI/CharacterSelect/CarouselController.cs` — Hero card carousel
- `Assets/Scripts/UI/CharacterSelect/CharSelectEvents.cs` — Scoped event bus
- `Assets/Scripts/UI/Controls/ButtonVFXHelper.cs` — AddBreathing(), AddShimmer(), AddFocusEffect(), AddChargeEffect()
- `Assets/Scripts/UI/Core/ThemeManager.cs` — Brand colors, GetBrandColors()
- `Assets/Scripts/Audio/MusicManager.cs` — SetMusicState(), SetParameter(), parameter lerp
- `Assets/Scripts/Audio/AudioManager.cs` — Audio playback, zone-based banks
- `Assets/Scripts/Core/InputManager.cs` — Mouse/gamepad input for parallax
- `Assets/Scripts/Managers/VBSceneManager.cs` — Scene transitions
- `Assets/UI/Styles/CharacterSelect.uss` — Existing overlay classes (.overlay-scanlines, .overlay-vignette, .overlay-veil-glow), theme CSS variables (--hero-primary, --hero-glow)
- `Assets/DefaultVolumeProfile.asset` — Base URP Volume Profile

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ButtonVFXHelper.cs` — AddBreathing(element, amplitude, periodMs) for carousel card + embark button idle glow. AddShimmer(), AddChargeEffect() for hold progression.
- `MusicManager.cs` — SetParameter(name, value) with _parameterLerpSpeed=3f. Ready for per-hero music parameter driving.
- `ThemeManager.cs` — GetBrandColors(Brand) returns primary/glow/dark. Extend with hero-specific theme lookups.
- `CharSelectEvents.cs` — Scoped event bus with OnHeroChanged. Use for triggering visual systems.
- `HeroStageController.cs` — 3D render texture camera. Extend for camera zoom (embark) and lighting rig.
- `HoldToEmbarkController.cs` — Hold progress (0-1). Hook overlay intensification + button VFX to this value.
- `CharacterSelect.uss` — .overlay-scanlines, .overlay-vignette, .overlay-veil-glow already defined. Theme CSS vars (--hero-primary, --hero-glow) already wired to panel borders and text.

### Established Patterns
- **Scoped Event Bus:** CharSelectEvents for all character select communication — use for visual system triggers
- **ScriptableObject data:** HeroDisplayConfig pattern exists. HeroThemeConfig follows same pattern with [CreateAssetMenu]
- **USS transitions:** GPU-safe properties (translate, scale, rotate, opacity, color). No width/height/margin.
- **UsageHints:** DynamicTransform | DynamicColor on animated VisualElements at creation time
- **Singleton access:** SingletonMonoBehaviour<T>.HasInstance check before .Instance

### Integration Points
- `CharSelectEvents.OnHeroChanged` — triggers hero switch animation sequence
- `HoldToEmbarkController` — provides hold progress (0.0-1.0) for embark VFX + overlay intensification
- `VBSceneManager` — scene transition after embark cinematic white-out
- `HeroDisplayConfig` SOs — existing per-hero data. HeroThemeConfig is the visual companion.
- `InputManager` — mouse/stick position for parallax

### New Systems to Build
- `HeroThemeConfig.cs` — ScriptableObject with all per-hero visual identity data
- `HeroThemeTransitioner.cs` — Reads HeroThemeConfig, drives all visual systems on hero switch
- `VolumeProfileTransitioner.cs` — Lerps between URP Volume Profiles
- `VeilDissolveController.cs` — Drives VeilDissolve shader on 3D models
- `VeilTransitionController.cs` — Reusable screen-level transition (cracks, shatter, white-out)
- `GlitchTextEffect.cs` — Text scramble → resolve for hero names
- `OverlayController.cs` — Manages scanline/vignette/glow intensity + parallax offsets
- `VeilDissolve.shader` — URP Lit-based dissolve with HDR edge + particle emission
- `VeilCrack.shader` — Full-screen procedural crack pattern for embark cinematic
- 4x VolumeProfile assets — Per-hero post-processing configurations
- 4x HeroThemeConfig assets — Per-hero visual identity bundles
- 4x Particle system prefabs — Per-hero background environments

</code_context>

<specifics>
## Specific Ideas

- "Our entire character selection + start screen is rather rushed and lackluster for a game of our caliber" — user wants AAA-tier visual quality
- "Make sure the character model and the monster are still the primary visual" — everything amplifies, nothing competes
- "If you can do epic shader work then we stick with it" — user explicitly endorsed custom VeilDissolve + VeilCrack shaders
- Per-hero UI personality: Nyx = glitch in EVERY visual layer (chromatic aberration, rim flicker, scanline jump, heavier overlays, longer text resolve). Vex = clean, warm, grounded. Seraphina = dreamy, soft, ethereal. Orion = intense, tight, predatory.
- Monster particles flowing INTO the hero during embark = "absorbed into hero, reinforcing the bond"
- All breathing effects synchronized: carousel card + rim light + veil glow overlay = "living veil" feel
- References: Destiny 2 character select (snap navigation), BG3 companion preview (shared 3D stage), Diablo IV menus (dark fantasy materials), Cyberpunk 2077 menus (overlay atmosphere)
- "Absolute amazing code strength, amazing design, AAA quality, and total and pure functionality" — quality bar from Phase 3 carries forward

</specifics>

<deferred>
## Deferred Ideas

- **Full Game Cinematic After Embark** — 10-30s AI-generated or in-engine cinematic showing selected hero + monster entering the overworld. Research needed on AI cinematic tools (Kling, Runway, Sora for game characters). Phase 4 builds the hookpoint (white-out leads to a CinematicController entry point). Deferred to its own dedicated phase.
- **Per-hero bespoke 3D environments** — Full environment scenes instead of particle backgrounds. Requires art pipeline. Deferred to v2 (ART phase).
- **Real voice acting** — Replace placeholder synth TTS with recorded voice lines. Separate production concern.
- **Film grain post-process** — Subtle film grain for cinematic feel. Could add in Phase 5 polish pass.
- **Motion blur on hero switch** — Brief motion blur during dissolve. Deferred as nice-to-have.

</deferred>

---

*Phase: 04-visual-amplification*
*Context gathered: 2026-03-19*
