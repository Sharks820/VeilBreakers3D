# Feature Landscape: VeilBreakers v6.0 AAA UI Rebuild

**Domain:** AAA dark fantasy RPG UI (title screen, character selection, 3D model integration)
**Researched:** 2026-03-30
**Confidence:** MEDIUM-HIGH
**Reference Games:** Diablo IV, Baldur's Gate 3, Elden Ring, Monster Hunter: World, Dark Souls III

---

## Scope

This research covers three pillars of VeilBreakers v6.0:

1. **Title Screen AAA Polish** -- atmospheric effects, visual premium quality, audio design
2. **Character Selection AAA Rebuild** -- 3D model display, hero presentation, interaction design
3. **3D Model Integration** -- GLB model pipeline, RenderTexture display, material/lighting setup

Analysis is grounded in what the existing codebase already has (TitleScreenVFX, MoltenButtonVFX, HeroStageController, CarouselController, HeroThemeConfig, UIGradientHelper) and what AAA reference games do to achieve premium feel.

---

## Table Stakes (Must Have or Screen Feels Incomplete)

Features players expect from any dark fantasy RPG with AAA aspirations. Missing these makes the product feel unfinished or amateur.

### Title Screen -- Table Stakes

| Feature | Why Expected | Complexity | Dependencies | Notes |
|---------|--------------|------------|--------------|-------|
| **Animated background (not static image)** | Every AAA RPG since 2015 uses either a 3D scene, looping video, or parallax layers behind the title. Static backgrounds read as mobile-quality. Diablo IV uses a campfire 3D scene; Elden Ring uses a foggy Erdtree panorama; BG3 uses a nautiloid crash cinematic. | Med | Video player or 3D camera setup | VeilBreakers ALREADY HAS THIS via `TitleScreenVFX` with video background. Validated. |
| **Atmospheric particle effects** | Embers, ash, dust motes, or fog wisps floating across the screen. Creates depth and life. Dark Souls III, Elden Ring, and Diablo IV all layer particles over their title screens. Without them the screen feels flat. | Med | Particle system or UI Toolkit VisualElement-based particles | ALREADY BUILT. `TitleScreenVFX` has embers (140), ash (16), micro-sparks (40). Smoke disabled (looked bad). Validated but may need polish. |
| **Logo with glow/VFX treatment** | The game logo must feel like part of the world, not a flat PNG slapped on screen. Dark Souls III logo has ember particles settling on it; Elden Ring logo glows softly. At minimum: subtle glow, breathing animation, or edge lighting. | Low | Logo texture asset, UIGradientHelper for glow | Partially built. Logo exists but needs VFX treatment verification. |
| **Menu buttons with hover/press feedback** | Every option (New Game, Continue, Settings, Exit) needs visual + audio feedback on hover and click. Diablo IV uses glowing runes; Dark Souls uses a subtle gold highlight; BG3 uses soft light sweeps. | Med | PrimeTween, AudioManager | ALREADY BUILT. `MoltenButtonVFX` provides molten highlight, hover lava fill, button sheet skins. Validated. |
| **Ambient audio (drone + environmental)** | A dark fantasy title screen without ambient audio is immediately noticeable. Low drone, distant wind, occasional creature sounds, or crackling fire. BG3 uses nautiloid creaking; Diablo IV uses campfire ambience; Elden Ring uses ethereal wind. | Med | AudioManager, TitleScreenAudio | ALREADY BUILT. `TitleScreenAudio.cs` exists with procedural audio. Needs verification of quality. |
| **Fade-in on load** | Screen must fade from black, not pop in. Every AAA game does this. Players notice when it's missing because it feels jarring. | Low | ScreenTransition.cs | Likely exists via `ScreenTransition`. Verify. |
| **Gamepad navigation** | All title screen options must be navigable via gamepad (D-pad, A to select, B to go back). Monster Hunter, Elden Ring, and all console RPGs require this. Missing it locks out controller users entirely. | Med | InputManager, FocusManager | Partially built. `InputManager` exists with gamepad support. Verify title screen focus management. |
| **Continue/Load game option** | If the player has save data, "Continue" must appear prominently (not buried in a submenu). Diablo IV, Elden Ring, BG3 all show this as the first option when saves exist. | Low | SaveManager integration | `SaveSlotBrowserController` exists. Verify title screen integration. |

### Character Selection -- Table Stakes

| Feature | Why Expected | Complexity | Dependencies | Notes |
|---------|--------------|------------|--------------|-------|
| **3D character model display** | The single most important visual element. BG3, Diablo IV, and Monster Hunter all display full 3D character models that the player can rotate/inspect. A character select screen without 3D models is a deal-breaker for AAA perception. | High | RenderTexture, dedicated camera, GLB model import, materials, lighting rig | Infrastructure ALREADY EXISTS in `HeroStageController` (RenderTexture 1024x1536, 4x MSAA, preview layer 31, 5-light rig). Models are currently placeholder capsules (`modelPrefab` is null on all 4 configs). GLB files exist in `Assets/Art/Models/Heroes/`. Integration is the gap. |
| **Per-hero visual theming** | When switching heroes, the entire screen mood must shift -- colors, lighting, particles, audio. BG3 does this with environmental changes per race; Diablo IV shifts campfire positioning; Monster Hunter changes the gathering hall lighting. Without it, heroes feel interchangeable. | High | HeroThemeConfig, HeroThemeTransitioner, post-processing volumes | ALREADY BUILT. `HeroThemeConfig` has 15+ visual parameters per hero (colors, lighting, particles, overlays, dissolve, glitch text, music). `HeroThemeTransitioner` handles animated transitions. Validated -- this is comprehensive. |
| **Hero name and class/path display** | Player must immediately see who they're selecting. Name, path, and a brief tagline. Every character select screen in every RPG does this. | Low | HeroData from GameDatabase, HeroDataPanelController | ALREADY BUILT. `HeroDataPanelController` and `HeroStatsPanelController` exist. |
| **Stat overview panel** | Base stats (HP, ATK, DEF, etc.) displayed visually (bars or numbers). Players need to compare heroes before committing. BG3 shows ability scores; Diablo IV shows class power fantasy stats; Monster Hunter shows weapon stats. | Low | HeroStatsPanelController, AnimatedBar | ALREADY BUILT with `StatNumberAnimator` and `AnimatedBar` components. |
| **Navigation between heroes** | Left/right arrows, carousel strip, or tab navigation to browse available heroes. Must feel smooth, not jarring. BG3 uses a horizontal list; Diablo IV uses a campfire circle; Monster Hunter uses a list. | Med | CarouselController, CharSelectFocusManager | ALREADY BUILT. `CarouselController` generates hero cards dynamically, `CharSelectFocusManager` handles input. |
| **Selection confirmation with feedback** | Clear "this is your hero" moment. BG3 uses a cinematic handoff; Diablo IV plays a class animation; Monster Hunter plays a horn. The confirm action must feel weighty. | Med | HoldToEmbarkController, EmbarkCinematicController | ALREADY BUILT. `HoldToEmbarkController` (2.5s hold with energy fill animation), `EmbarkCinematicController` for cinematic transition. This is actually a differentiator-level feature already implemented. |
| **Back button to title** | Always provide an escape. Every character select has this. | Low | CharacterSelectManager | ALREADY BUILT. `btn-back` navigates to MainMenu scene. |
| **Loading/skeleton state** | If data is loading asynchronously, show skeleton placeholders, not blank space or frozen UI. Players notice empty screens. | Med | CharacterSelectManager (kSkeletonOverlay) | ALREADY BUILT. `skeleton-overlay` element exists in CharacterSelectManager. |

### 3D Model Integration -- Table Stakes

| Feature | Why Expected | Complexity | Dependencies | Notes |
|---------|--------------|------------|--------------|-------|
| **GLB import with correct materials** | Models must display with correct PBR materials (albedo, normal, metallic/roughness). Broken materials = amateur look. | High | Unity GLB importer, URP material setup, texture settings | 24 GLB files exist across 3 heroes (Nyx missing from Vex set) + 3 monsters. All are `_pbr` suffixed. Unity should auto-import but material assignment needs verification. |
| **Proper lighting on 3D models** | Three-point lighting minimum (key + fill + rim). Models must not look flat or over-exposed. BG3's character creation uses dramatic rim lighting with warm fill. | Med | HeroStageController (already has 5-light rig) | ALREADY BUILT. `HeroStageController` has key, fill, rim, face, and ground lights with per-hero color/intensity from `HeroDisplayConfig`. Need to tune for actual models vs placeholder capsules. |
| **Model fits viewport correctly** | Character must be framed properly -- head near top, feet visible or artistically cropped. No clipping, no floating, no awkward empty space. | Med | HeroDisplayConfig camera offset/FOV/padding | Config exists with `cameraOffset`, `cameraFOV`, `cameraFramePadding`. Values currently tuned for placeholder capsules. Must be re-tuned per hero model. |
| **Idle animation** | Static T-pose is unacceptable. Characters must have at minimum a breathing idle. BG3 has subtle weight-shifting; Diablo IV has class-specific idle stances; Monster Hunter has weapon-held idles. | High | AnimationClip assets, Animator setup, HeroDisplayConfig animation slots | `HeroDisplayConfig` has slots for `idleClip`, `idleVariantClips`, `selectedClip`, `showcaseClip`, `embarkClip`. All null currently. Need animation assets (from Mixamo, Cascadeur, or manual). This is the hardest table-stake to fill. |

---

## Differentiators (Features That Elevate Beyond Competent to Premium)

These are not expected but create the "wow factor" that makes players screenshot and share.

### Title Screen Differentiators

| Feature | Value Proposition | Complexity | Dependencies | Notes |
|---------|-------------------|------------|--------------|-------|
| **3D scene background (not video)** | Real-time 3D scene reacts to input (camera sway on mouse move, particles respond to cursor). Elden Ring's Erdtree glows dynamically; this goes beyond static video loops. Creates living world feeling. | High | 3D scene, URP camera, subtle parallax | Currently using video background. Upgrading to 3D scene would be premium but is a major scope increase. RECOMMEND: Keep video for v6.0, plan 3D scene for v7.0. |
| **VERA glitch text on title** | VERA AI companion manifests on the title screen with glitch text artifacts, creating narrative intrigue before the game starts. Unique to VeilBreakers -- no reference game does this because VERA is unique IP. | Med | VERASystem integration, GlitchTextEffect | `GlitchTextEffect.cs` already exists in CharacterSelect. Could be adapted for title screen. Per-hero `glitchResolveSpeed` already configured in `HeroThemeConfig`. |
| **Demon laugh / voice tease audio** | A rare, randomized demonic laugh or whisper that plays occasionally on the title screen. Unsettling. Creates atmosphere that players talk about. Dark Souls III has subtle environmental moans; this would be VeilBreakers' equivalent. | Low | AudioManager, random timer | Procedural audio system exists in `TitleScreenAudio`. Adding a rare demon laugh trigger is straightforward. |
| **Veil energy effects on logo** | Red veil energy wisps curling around the VeilBreakers logo, pulsing subtly. Ties the logo to the game's core corruption mechanic. No reference game does this because it's IP-specific. | Med | Custom VisualElement particles around logo bounds | Would use same particle technique as `TitleScreenVFX` embers but constrained to logo area. |
| **Dynamic menu button VFX per hover target** | Different buttons trigger different atmospheric shifts -- "New Game" makes embers flare, "Continue" makes the veil pulse, "Settings" dims the atmosphere. Creates discovery and delight. | Med | TitleScreenVFX + MoltenButtonVFX integration per button | Both systems exist. Need event bridge between button hover state and VFX parameters. |
| **Corruption meter teaser** | A subtle, almost-hidden corruption meter or veil integrity indicator on the title screen that hints at the game's systems before the player even starts. Creates mystery. | Low | UI element + UIGradientHelper | Simple visual element. Narratively powerful if tied to save data (shows highest corruption across saves). |

### Character Selection Differentiators

| Feature | Value Proposition | Complexity | Dependencies | Notes |
|---------|-------------------|------------|--------------|-------|
| **Champion monster alongside hero** | Display the hero's starter monster next to them on the 3D stage. BG3 doesn't do this. Monster Hunter Wilds shows the Seikret companion. This is a monster RPG -- the monster IS the draw alongside the hero. | High | Champion model prefab, offset/scale config, animation | `HeroDisplayConfig` already has `championModelPrefab`, `championOffset`, `championScale`, `championIdleClip`. Monster GLB files exist (Bloodshade, Grimthorn, Voltgeist). Need to wire models and animations. |
| **Veil dissolve transitions between heroes** | Instead of a simple crossfade when switching heroes, use a custom dissolve shader that tears reality apart and reforms it with the new hero. Unique to VeilBreakers' world fiction. | Med | VeilDissolveController, dissolve shader, noise texture | ALREADY BUILT. `VeilDissolveController.cs` exists with per-hero dissolve config (noiseScale, duration, edgeColor). `VeilDissolvePlaceholder.mat` and `noise_perlin_256.png` in Resources. |
| **Hero-specific particle systems on stage** | Each hero's 3D stage has unique particles: Vex has embers, Seraphina has spectral wisps, Orion has arcane motes, Nyx has void tears. Reinforces visual identity. | Med | Per-hero particle prefab, HeroThemeConfig.particlePrefab | Config field exists (`particlePrefab`, `maxParticleCount`). Need actual particle system prefabs per hero. |
| **Interactive model rotation (mouse drag + stick)** | Player can rotate the 3D hero model to inspect from all angles. Monster Hunter does this extensively. BG3 allows camera orbit in character creation. | Med | Mouse drag + right stick input | ALREADY BUILT in `HeroStageController` (isDragging, dragStartX, modelRotationY, kStickRotationSpeed). Verified in code. |
| **Scanline/CRT overlay per hero** | Nyx gets heavy scanlines (reality distortion); Vex gets minimal. Creates atmosphere and hero-specific visual identity layers. | Low | USS overlay element, HeroThemeConfig.scanlineOpacity | ALREADY BUILT. `scanlineOpacity` configured per hero in `HeroThemeConfig`. |
| **Tab system (Overview / Abilities / Lore)** | Multiple info tabs let the player deep-dive into hero details without cluttering the screen. BG3 has multiple tabs in character creation. Diablo IV shows different class info panels. | Med | CharacterSelectManager tab switching (kTabOverview, kTabAbilities, kTabLore) | ALREADY BUILT. Three tabs with content switching. Tab buttons and content panels wired. |
| **Toast error handling with retry** | If save/load fails during embark, show a styled toast notification with retry/back options instead of crashing or hanging. No reference game shows this because they don't surface errors -- but graceful error handling IS premium. | Low | CharacterSelectManager (kToastContainer, kToastMessage, kBtnToastRetry, kBtnToastBack) | ALREADY BUILT. Toast system with retry/back buttons. |
| **Volume profile transitions per hero** | URP post-processing (bloom, vignette, color grading, depth of field) shifts when switching heroes. Orion gets tighter vignette; Nyx gets chromatic aberration. Creates cinematic atmosphere. | Med | VolumeProfileTransitioner, per-hero VolumeProfile assets | ALREADY BUILT. `VolumeProfileTransitioner.cs` exists. `HeroThemeConfig` has `volumeProfile` and `chromaticAberrationIntensity` fields. Need actual VolumeProfile assets tuned per hero. |

### 3D Model Integration Differentiators

| Feature | Value Proposition | Complexity | Dependencies | Notes |
|---------|-------------------|------------|--------------|-------|
| **Brand-colored aura/glow on models** | Each hero's model has a subtle colored aura matching their brand identity. Creates visual cohesion between the UI theme colors and the 3D model. | Med | URP shader with emission, HeroThemeConfig.monsterAuraColor | `monsterAuraColor` field exists in `HeroThemeConfig`. Need shader setup on models. |
| **Idle animation variety with random delays** | Instead of one looping idle, heroes occasionally shift weight, adjust stance, or perform a personality animation. Creates life. BG3 does this excellently -- characters fidget, look around, react. | Med | Multiple AnimationClip assets per hero | `idleVariantClips[]` array and `idleVariantMinDelay`/`idleVariantMaxDelay` already in HeroDisplayConfig. Need animation assets. |
| **Model quality LOD selection** | Multiple model variants (v1-v4) exist per hero. Select highest quality that maintains target framerate. Graceful degradation on lower hardware. | Low | Multiple GLB imports, quality setting check | 4 variants per hero already exist (model_v1_pbr through model_v4_pbr). Pick best quality and fallback. |
| **Selection VFX on hero pick** | Burst of brand-colored particles when a hero is selected in the carousel. Creates a satisfying "I chose this" moment. | Med | VFX prefab, HeroDisplayConfig.selectionVFXPrefab | Config slot exists. Need VFX prefab per hero. |

---

## Anti-Features (Do NOT Build These)

Features that seem appealing but would hurt the product, waste time, or violate project constraints.

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| **Full 3D character creator (sliders, customization)** | VeilBreakers has 4 fixed heroes with defined identities. A character creator destroys their narrative identity, adds massive scope, and is explicitly out of scope per PROJECT.md. BG3 can do this because it's a tabletop RPG -- VeilBreakers is hero-driven. | Keep the 4 hero selection with deep per-hero theming. The constraint IS the identity. |
| **Cinematic cutscene on title screen** | Pre-rendered cinematics are expensive, take storage, and only impress once. Real-time atmospheric effects (which VeilBreakers already has) are replayable and cheaper. | Continue investing in real-time VFX (embers, particles, video backgrounds). These age better. |
| **UGUI overlay for VFX** | Mixing UGUI and UI Toolkit creates two rendering pipelines, z-order conflicts, and doubles the maintenance surface. `MainMenuVFXOverlayController` is already disabled with comment "orange bar columns detract from AAA quality." | Keep all UI in UI Toolkit. Use VisualElement-based particles (as TitleScreenVFX already does) or 3D scene particles rendered by the world camera. |
| **Multiplayer lobby / online features** | Explicitly out of scope. Single-player RPG. Any online feature adds server infrastructure, authentication, and networking complexity. | Focus on single-player polish. The game's value is in the RPG experience, not social features. |
| **New heroes or monsters for v6.0** | Scope creep. 4 heroes and existing monsters are enough content. Adding new heroes means new models, animations, configs, balancing, and lore. | Polish the 4 existing heroes to perfection. Quality over quantity. |
| **Complex shader effects in UI Toolkit** | UI Toolkit has no custom shader support for VisualElements. Trying to add blur, custom materials, or shader-based effects fights the framework. | Use UIGradientHelper for gradients, layered VisualElements for glow/depth, and RenderTexture for anything 3D. These are proven patterns in the codebase. |
| **Animated transitions via Unity Timeline** | UI Toolkit has no Timeline support. Attempting it creates dependency on UGUI or custom bridges that are fragile. | Use PrimeTween for all UI animations (already the standard in the codebase). CSS transitions for simple hover/focus states. |
| **Mobile-responsive layouts** | Windows standalone is the target platform (1920x1080 minimum). Designing for mobile adds responsive breakpoints and touch targets that are unnecessary. | Design for 1920x1080 primary, verify at 1280x720 and 2560x1440 as documented in toolkit rules. |
| **Parallax scrolling on character select** | Adds visual noise to a screen that needs to focus attention on the 3D hero model. The background should be atmospheric but not distracting. | Use subtle gradient backgrounds with per-hero color themes (already implemented in CharSelectVisualEnhancer). Let the 3D model and lighting do the heavy lifting. |

---

## Feature Dependencies

```
Critical Path (must be done in order):
  GLB Model Import & Material Fix  -->  HeroDisplayConfig Model Assignment
  HeroDisplayConfig Model Assignment  -->  3D Model Display in HeroStageController
  3D Model Display  -->  Camera/Lighting Tuning Per Hero
  Camera/Lighting Tuning  -->  Animation Integration
  Animation Integration  -->  Champion Monster Display

Parallel Work (can happen alongside critical path):
  Title Screen VFX Polish (independent)
  Bug Fixes Phase A-C (independent, should be FIRST)
  Gamepad Navigation Fixes (independent)
  Audio Polish (independent)
  VolumeProfile Tuning (after model display works)

Dependencies on Existing Systems:
  3D Model Display  -->  HeroStageController (exists, needs real model data)
  Per-Hero Theming  -->  HeroThemeConfig + HeroThemeTransitioner (exists, validated)
  Hold-to-Embark    -->  HoldToEmbarkController (exists, validated)
  Dissolve Transitions  -->  VeilDissolveController (exists, needs shader)
  Carousel Navigation  -->  CarouselController (exists, validated)
  Visual Enhancement  -->  CharSelectVisualEnhancer + UIGradientHelper (exists, validated)
```

---

## MVP Recommendation for v6.0

The codebase is remarkably complete in infrastructure. The gap is not "build new systems" but "connect real assets to existing systems."

### Priority 1: Fix bugs first (Phase A-C from PROJECT.md)
Without fixing the defender synergy, brand matrix, and div-by-zero bugs, no amount of visual polish matters. The game must work correctly before it looks beautiful.

- Defender synergy defense never applied
- Brand effectiveness matrix bidirectional violations
- CharSelectFocusManager div-by-zero
- CharSelectVisualEnhancer callback leak
- 11 additional high-priority bugs

### Priority 2: 3D model integration (highest visual impact)
Replacing placeholder capsules with real 3D hero models is the single biggest visual upgrade possible. Everything else is incremental polish on top of already-good infrastructure.

1. Import and verify all GLB hero models (pick best variant per hero from v1-v4)
2. Set up URP materials correctly (verify PBR maps)
3. Assign modelPrefab on each HeroDisplayConfig
4. Tune camera offset, FOV, and lighting per hero
5. Add basic idle animation (Mixamo is fastest path for humanoid idle)

### Priority 3: Title screen polish verification
The title screen already has AAA-level VFX. Verify everything works correctly after bug fixes, then add:

1. VERA glitch text teaser (reuse existing GlitchTextEffect)
2. Logo veil energy effect (low complexity, high impact)
3. Audio verification and polish pass

### Priority 4: Character select final polish
After 3D models are in:

1. Champion monster display (wire existing config)
2. VolumeProfile assets per hero (tune bloom, vignette, color grading)
3. Hero-specific particle prefabs for 3D stage
4. Selection VFX burst on hero pick

### Defer to v7.0:
- 3D scene title screen (replace video with real-time 3D)
- Full animation suite per hero (showcase, embark cinematics)
- Complex selection VFX prefabs
- Overworld gameplay (out of scope per PROJECT.md)

---

## What AAA Dark Fantasy Games Actually Do (Pattern Analysis)

### Diablo IV Pattern: "Campfire Circle"
- **Title:** 3D scene of a campfire with atmospheric fog, embers, distant structures. Minimal UI -- just the logo and menu options.
- **Character Select:** Heroes sit around the campfire. Selecting a class focuses the camera on that character. The environment doesn't change, but camera angle and lighting emphasis shift.
- **Lesson for VeilBreakers:** The campfire is a single coherent scene that both title and character select share. VeilBreakers' approach of separate scenes with their own VFX is more flexible but loses that continuity. Not a problem -- the per-hero theming (which Diablo IV doesn't do) compensates.

### Baldur's Gate 3 Pattern: "Cinematic Stage"
- **Title:** Dramatic cinematic (nautiloid crash) that transitions into the menu. After first viewing, a more subdued background with the BG3 logo.
- **Character Select:** Full character creator with the model on a lit stage. Dramatic rim lighting, warm fill, environmental backdrop matching selected race/class. Subsurface scattering on skin. Full model rotation.
- **Lesson for VeilBreakers:** BG3's character creation lighting is the gold standard. VeilBreakers' 5-light rig with per-hero color/intensity in HeroStageController is the right approach. The key differentiator is per-hero atmosphere shifts, which BG3 does subtly but VeilBreakers does aggressively (veil dissolve, theme transitions, chromatic aberration for Nyx).

### Elden Ring Pattern: "Atmospheric Minimalism"
- **Title:** Fog, a softly glowing Erdtree in the distance, minimal particle effects. The title card appears with a subtle shine. Almost nothing moves, but everything feels alive because of the fog layering and depth of field.
- **Character Select:** Simple class selection with a 3D model on a stone slab. Minimal VFX. The premium feel comes from the model quality and lighting, not from effects.
- **Lesson for VeilBreakers:** Restraint is power. Not every element needs VFX. VeilBreakers should ensure the 3D model and lighting carry the weight, with VFX as accent not foundation. The existing smoke wisps were disabled because they "looked like shadow spheres" -- that's the right instinct.

### Dark Souls III Pattern: "Embers and Decay"
- **Title:** Burning embers drift upward from a dark background. The logo has particles settling on it. Ambient drone with distant choir.
- **Character Select:** Basic class icons with stat descriptions. No 3D preview in class selection (only in character creation). The premium feel is entirely in the audio and the weight of the UI transitions.
- **Lesson for VeilBreakers:** Audio sells atmosphere as much as visuals. The procedural title audio system is on the right track. Ensure every UI interaction (hover, click, navigate, embark) has a sound that fits the dark fantasy aesthetic.

### Monster Hunter: World Pattern: "Living Hub"
- **Title:** 3D hub environment (Astera/Seliana) with NPCs and activity. Rich ambient audio.
- **Character Select:** Full character creator with turntable rotation, dramatic three-point lighting, close-up detail views. Strong emphasis on model quality because the character is viewed frequently in cutscenes.
- **Lesson for VeilBreakers:** The turntable rotation is already implemented in HeroStageController. Monster Hunter's strength is that the character creator model matches exactly what you see in gameplay -- ensure VeilBreakers' hero models look good both on the select screen AND in the eventual combat/overworld.

---

## UI Toolkit Constraints and Workarounds (Verified)

These constraints affect all visual feature implementation:

| Constraint | Impact | Verified Workaround |
|------------|--------|-------------------|
| **No CSS gradients in USS** | Cannot use `linear-gradient()` | `UIGradientHelper.CreateVerticalGradient()` generates Texture2D at runtime. Already used extensively in `CharSelectVisualEnhancer`. Verified in codebase. |
| **No box-shadow in USS** | Cannot add shadows to elements | Layer multiple VisualElements with decreasing opacity for depth. Use dark gradient textures underneath panels. |
| **No blur filter in USS** | Cannot blur backgrounds | Use pre-blurred background textures or dark overlays with opacity. Not critical for dark fantasy aesthetic (darkness IS the blur). |
| **No custom shaders on VisualElements** | Cannot add shader-based effects to UI | Use 3D scene rendering (RenderTexture) for anything that needs shaders. Keep UI effects to color, opacity, scale, and texture manipulation. |
| **No Timeline support** | Cannot drive UI animations from Timeline | PrimeTween is the standard. Target-based overloads (no closures) for performance. Already used throughout codebase. |
| **RenderTexture is expensive** | One RT per 3D preview consumes GPU memory | Use single RT for hero preview (already: 1024x1536, 4x MSAA). Don't add more RTs without profiling. Champion monster could share the same RT with offset positioning. |
| **No `style.gap` on IStyle** | USS gap property unavailable in Unity 6 | Use child margins for spacing. Documented in project toolkit rules. |

---

## Complexity Estimates Summary

| Feature Category | Already Built | Needs Work | New Build | Total |
|-----------------|---------------|------------|-----------|-------|
| Title Screen Table Stakes | 6 of 8 | 2 (verify) | 0 | 8 |
| CharSelect Table Stakes | 7 of 8 | 0 | 1 (3D models) | 8 |
| 3D Integration Table Stakes | 1 of 4 | 1 (tune) | 2 (import, animate) | 4 |
| Title Screen Differentiators | 0 | 0 | 6 | 6 |
| CharSelect Differentiators | 7 of 10 | 3 (need assets) | 0 | 10 |
| 3D Integration Differentiators | 0 | 2 (wire configs) | 2 | 4 |

**Key insight:** The codebase has approximately 70% of the feature infrastructure already built. The primary gap is real 3D model assets (not placeholder capsules) and animation clips. The code architecture is sound and well-designed -- the v6.0 milestone is an asset integration and polish effort, not a systems engineering effort.

---

## Sources

**Game UI References:**
- [Game UI Database](https://gameuidatabase.com/) -- 1,300+ games, 55,000+ UI screenshots
- [Game UI Database - Diablo IV](https://www.gameuidatabase.com/gameData.php?id=1783)
- [Game UI Database - Elden Ring](https://www.gameuidatabase.com/gameData.php?id=1371)
- [Game UI Database - Monster Hunter: World](https://www.gameuidatabase.com/gameData.php?id=291)

**Design Analysis:**
- [Diablo IV Art Preview - Fernando Forero (ArtStation)](https://www.artstation.com/artwork/m80nnd)
- [Improving Diablo 4 Experience: A UI Redesign (Medium)](https://medium.com/@panchogonzales433/ui-redesign-for-improving-diablo-4-experience-e7d6d3aa4c00)
- [Building an Inclusive Character Creator for BG3 (GameDeveloper.com)](https://www.gamedeveloper.com/art/building-an-inclusive-character-creator-for-the-fantasy-world-of-baldur-s-gate-3)
- [Creating Characters for BG3 (80.lv)](https://80.lv/articles/creating-characters-for-baldur-s-gate-3)

**Unity UI Toolkit:**
- [Unity UI Toolkit vs UGUI: 2025 Guide (Medium)](https://medium.com/@studio.angry.shark/unity-ui-toolkit-vs-ugui-2025-developer-guide-8407312c91ed)
- [UI Toolkit Development Status (Unity Discussions)](https://discussions.unity.com/t/ui-toolkit-development-status-and-next-milestones-november-2025/1698009)
- [How to Display 3D Models as UI Elements (Unity Discussions)](https://discussions.unity.com/t/what-is-the-best-way-to-display-3d-models-as-ui-elements/590769)
- [Render Camera to RenderTexture (Unity Manual)](https://docs.unity3d.com/6000.0/Documentation/Manual/output-to-render-texture.html)
- [3D Model UI Preview (LlamAcademy GitHub)](https://github.com/llamacademy/3d-model-ui-preview)

**AAA Game Design:**
- [From Art Direction to UI Design (iABDI)](https://www.iabdi.com/designblog/2024/12/28/unxk2uqg0vetfooxlcey90up7addu2)
- [Elden Ring GUI Redesign (ArtStation)](https://spacejim.artstation.com/projects/elo5D3)
- [Complete Guide to AAA Game Development 2026 (JuegoStudio)](https://www.juegostudio.com/blog/guide-to-aaa-game-development-and-studio-strategies)

**Confidence Notes:**
- Codebase analysis: HIGH (direct file reads of 20+ source files)
- AAA RPG patterns: MEDIUM (synthesized from multiple sources, game databases, and direct game experience knowledge)
- UI Toolkit constraints: HIGH (verified against existing workarounds in codebase + official Unity discussions)
- 3D model integration: MEDIUM (RenderTexture pattern is standard; specific GLB import quality unverified until models are tested in editor)
