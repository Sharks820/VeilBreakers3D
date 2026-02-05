# Character Select Screen - Full Redesign

> **Status:** APPROVED | **Version:** 1.0 | **Date:** 2026-02-05
> **Reference:** Baldur's Gate 3 character select
> **Replaces:** `Docs/AAA_CHARACTER_SELECT_DESIGN.md` (to be archived)

---

## Vision: Cinematic Experience

The player doesn't open a menu — they enter a **scene**. Each hero is living in their world, bonded with their starter monster, telling a micro-story. The UI is nearly invisible. The 3D models and environments are the star.

## Core Pillars

1. **Cinematic, not menu** — Camera moves, heroes breathe, environments live
2. **Personality through action** — Each hero is DOING something, not posing
3. **Bond on display** — Hero and monster interact naturally
4. **Minimal UI** — Information appears contextually, not permanently
5. **Environment as distraction** — Rich themed worlds draw the eye (covers AI model imperfections)

---

## Flow

```
Main Menu → Character Select (Carousel) → Embark → Game
```

No character editor. Preset models per hero. Customization is a future feature.

---

## Screen Layout

### Mode: Hero Select (Carousel)

```
┌──────────────────────────────────────────────────────────────────┐
│  [FULL 3D THEMED ENVIRONMENT - fills entire screen]               │
│                                                                   │
│                                                                   │
│              Hero name etched/burned/grown into                   │
│              the environment itself                               │
│                                                                   │
│                    ╔══════════════╗                               │
│                    ║              ║      Starter monster          │
│                    ║   3D HERO    ║      interacting with         │
│                    ║   IN SCENE   ║      hero naturally           │
│                    ║              ║                               │
│                    ╚══════════════╝                               │
│                                                                   │
│         Stats panel ──► appears on hover/keypress only            │
│                                                                   │
│    ┌───┐  ┌───┐  ┌════════┐  ┌───┐  ┌─────┐                    │
│    │VEX│  │SER│  │SELECTED│  │NYX│  │ ??? │                     │
│    └───┘  └───┘  └════════┘  └───┘  └─────┘                    │
│              ◄── Minimal carousel portraits ──►                   │
│                                                                   │
│  [BACK]              [ EMBARK AS VEX ]              [☀/🌙]      │
└──────────────────────────────────────────────────────────────────┘
```

---

## Hero Roster

### Active Heroes (4)

| Hero | Path | Brand | Role | Resource | Color RGB |
|------|------|-------|------|----------|-----------|
| Vex | IRONBOUND | IRON | Tank | GUARD | `rgb(140, 150, 165)` |
| Seraphina | FANGBORN | VENOM | DPS | FURY | `rgb(165, 50, 90)` |
| Orion | VOIDTOUCHED | RUIN | Mage | MANA | `rgb(38, 166, 242)` |
| Nyx | UNCHAINED | VOID | Hybrid | CHAOS | `rgb(64, 26, 77)` |

### Teaser Slot (5th)
Mystery slot — no model, just fractured void environment and the text: *"More will answer the call."*

---

## Per-Hero Scenes

Each hero exists in a fully themed 3D environment. They are not posed — they are **living** in their world.

### Vex — Ruined Iron Prison

- **Scene:** Standing in a collapsed prison corridor, one boot on a fallen pillar, war hammer resting on shoulder
- **Idle behavior:** Skitter-Teeth crawls between his boots, up his leg, perches on his shoulder. Vex occasionally glances at it with grudging respect.
- **Atmosphere:** Forge embers drifting, heat haze, dust motes in shafts of warm light
- **Lighting:** Warm forge glow from below-left, steel-blue rim light
- **Name display:** Burned/branded into the iron wall behind him

### Seraphina — The Thornwaste

- **Scene:** Crouched examining a poisonous flower, surrounded by breathing carnivorous plants
- **Idle behavior:** Grimthorn circles her slowly, snapping at floating spores. She feeds it something. Her barb-crown catches the light.
- **Atmosphere:** Bioluminescent spores floating, pollen particles, vine shadows on ground
- **Lighting:** Sickly green bioluminescence from plants, magenta moonlight from above
- **Name display:** Grows in glowing vine-text from the ground

### Orion — Storm Spire Peak

- **Scene:** Floating slightly off the ground at a lightning-struck tower peak, energy arcing between his fingers
- **Idle behavior:** Voltgeist orbits him erratically, their lightning connecting and separating. Rain falls but evaporates before hitting him. His eyes flicker.
- **Atmosphere:** Volumetric rain, mist, ozone shimmer, intermittent lightning strikes illuminating the scene
- **Lighting:** Lightning strikes as key light (intermittent), constant electric blue crackling glow
- **Name display:** Crackles in lightning-text, flickers

### Nyx — Void Rift

- **Scene:** Sitting on nothing in void space, legs crossed, shadow tendrils pooling beneath her
- **Idle behavior:** Bloodshade IS her shadow — it moves independently, occasionally forming faces. She reads a memory fragment that glows, then crumbles to dust.
- **Atmosphere:** Shadow particles drifting upward (reversed gravity), reality distortion at edges, floating memory fragments
- **Lighting:** No clear source — she glows faintly. Deep purple edge light from the void.
- **Name display:** Fades in and out like a memory, never fully solid

### Teaser Slot — Fractured Void

- **Scene:** Pure fractured void — all four hero environments visible through cracks in reality, overlapping impossibly
- **Idle behavior:** A Veil scar pulses in the center. Distorted whisper audio.
- **Text:** *"More will answer the call."*
- **No model.** Just atmosphere and promise.

---

## Neutral Base (Toggle)

A toggle button (bottom-right) switches to a **neutral viewing mode**:
- Clean dark stone circular platform
- Soft, even directional lighting from all sides
- No environment, no particles, no distractions
- Hero and monster still present with idle animations
- Purpose: Get a clean look at the 3D model without environment camouflage

---

## Camera System

### Idle
- Camera slowly orbits the hero at ~15° arc over 30 seconds
- Subtle depth of field blurs background edges
- Slight vertical bob for cinematic feel

### Player Rotation
- Click-drag on model overrides auto-orbit
- Smooth response with slight momentum on release
- Scroll wheel zooms (limited range — no clipping into model)

### Hero Switch Transition (~0.6s)
1. Camera pulls back slightly
2. Veil **tears across the screen** — jagged reality crack, not a fade
3. New environment bleeds through the tear
4. Camera pushes into new hero as they finish entrance animation
5. Branded color flash at the tear point (hero's brand color)

### Carousel Hover (Preview)
- Camera subtly shifts toward hovered hero's direction
- Whisper of their environment bleeds at screen edges
- Their musical motif fades in quietly

---

## UI Design — Minimal & Contextual

### Hero Name & Title
- **Not a UI element.** Integrated into the environment:
  - Vex: Branded/burned into iron wall
  - Seraphina: Grows as glowing vine-text
  - Orion: Crackles as lightning-text
  - Nyx: Fades in/out like a memory
- Path, role, and quote appear as subtle overlay text below the name (glass panel, appears on selection, fades after 4 seconds)

### Stats Panel
- **Only appears on hover or keypress** (Tab or similar)
- Slides in as sleek glass panel (right side)
- 5 stat bars: HP, ATK, DEF, MAG, SPD
- Bars fill with brand color, staggered animation (0.6s)
- Highest stat subtly pulses
- Fades out after 3 seconds of no interaction

### Carousel (Bottom)
- Minimal horizontal strip at very bottom
- Small circular portrait frames with brand-colored rings
- Selected: 1.3x scale, brand glow, fully lit
- Unselected: 1.0x scale, slightly dimmed
- Teaser slot: Dark circle with faint "?" pulse
- Navigation: Click, arrow keys, scroll wheel

### Embark Button (Bottom Center)
- Text: "EMBARK AS [HERO NAME]"
- Idle: Dark glass with subtle brand-color border
- Hover: Brand color fills in, scale 1.05x
- Click: Burst of brand-colored particles, dramatic swell of hero's theme, scene transition

### Back Button (Bottom Left)
- Simple, understated "BACK" text
- Returns to main menu

### Environment Toggle (Bottom Right)
- Small sun/moon icon toggle
- Switches between themed environment and neutral base
- Smooth 0.5s crossfade transition

---

## Animation Timeline

### Screen Entry (3.0s)
```
0.0s  Camera pushes through Veil tear into first hero's environment
0.5s  Environment fully visible, atmospheric effects active
0.8s  Hero entrance animation begins (hero-specific)
1.5s  Hero settles into idle stance
1.8s  Starter monster appears/enters (hero-specific interaction)
2.2s  Hero name appears in environment
2.5s  Carousel slides up from bottom
2.8s  Embark button fades in
3.0s  Fully interactive
```

### Hero Switch (0.6s)
```
0.0s  Veil tear rips across screen (brand-colored)
0.1s  Current environment/hero begins dissolving behind tear
0.3s  New environment visible, new hero entrance animation starts
0.5s  Hero settles, monster appears
0.6s  Name appears in new environment, fully interactive
```

### Embark (1.5s)
```
0.0s  Button press — particle burst in brand color
0.2s  Hero's musical theme swells
0.5s  Camera pushes toward hero, depth of field intensifies
0.8s  Veil gate forms behind/around hero
1.0s  Screen engulfed in brand-colored energy
1.5s  Transition to game scene
```

---

## Sound Design

| Element | Audio |
|---------|-------|
| **Per hero** | Unique ambient loop + musical motif, fades in on selection |
| **Transition** | Veil-tear sound: glass cracking + deep bass + energy rush |
| **Carousel hover** | Soft whisper of that hero's motif |
| **Embark** | Dramatic swell of selected hero's full theme |
| **Teaser slot** | Discordant tones, reversed audio, unease |
| **Environment** | Per-hero ambient: forge hammering, jungle insects, thunder, void hum |
| **Monster** | Subtle creature sounds: skittering, rustling, crackling, whispering |

---

## 3D Assets Required

### Hero Models (4) — Animation-Safe

All models: **No cloth simulation, no chains, no capes, no flowing elements.** All dynamic effects (lightning, shadows, vines, embers) are Unity VFX overlays, not model geometry.

| Model | Description |
|-------|-------------|
| Vex | Heavy rigid plate armor, war hammer, static iron shackles as belt detail, scars/sigils on skin, thick build |
| Seraphina | Rigid thorn armor plating, static barb crown, poison vials/pouches, vine patterns etched into armor, lean athletic |
| Orion | Fitted rigid arcane coat, static arcane symbol engravings, lean build, intense expression |
| Nyx | Fitted rigid hooded outfit, solid body silhouette, pale skin, glowing eyes |

### Starter Monster Models (4)

| Model | Description |
|-------|-------------|
| Skitter-Teeth | Animated ribcage crawling on bone finger-legs, teeth in chest cavity, small (knee-height), iron-grey bone |
| Grimthorn | Carnivorous plant creature, venus flytrap head, thorny vine body, barbed tendrils, medium-sized |
| Voltgeist | Spectral electric humanoid, translucent blue-white, visible skeletal structure, floating, medium-sized |
| Bloodshade | Amorphous shadow mass, crimson eyes, semi-liquid body, dark tendrils reaching upward, small-medium |

### Teaser Slot
- No model needed — pure VFX (Veil cracks, void particles, distortion shader)

### Environment Assets (4 + 1 neutral)

| Environment | Key Elements |
|-------------|-------------|
| Iron Prison (Vex) | Broken walls, fallen pillars, forge glow source, iron debris, chain textures on walls (static) |
| Thornwaste (Seraphina) | Twisted plants, carnivorous flora, bioluminescent mushrooms/flowers, overgrown ruins |
| Storm Spire (Orion) | Tower peak platform, broken spire elements, storm clouds (skybox), rain particle system |
| Void Rift (Nyx) | Floating fragments, void skybox, shadow pool ground plane, memory fragment particles |
| Neutral Base | Dark stone circular platform, 3-point studio lighting setup, no props |

**Total: 8 character/creature models + 5 environment sets**

---

## Tripo 3D Prompts

### Hero Models

**Vex — The Warden (IRONBOUND Tank)**
```
Dark fantasy male warrior, heavy rigid plate armor with no flowing
parts, large war hammer weapon, iron shackles as static belt
accessories, sigil engravings on armor and skin, battle scars on
face, thick muscular build, steel-grey and warm bronze color palette,
stoic intimidating expression, no cape no chains no cloth physics,
all armor pieces rigid and solid, game-ready character model,
PBR textures, T-pose or A-pose
```

**Seraphina — The Thornspeaker (FANGBORN DPS)**
```
Dark fantasy female nature assassin, rigid thorn-plated armor with
sharp barb details sculpted into surface, static crown of thorns on
head, poison vials and pouches on belt, vine patterns etched and
carved into armor not separate geometry, lean athletic build,
confident predatory expression, deep crimson and poison green color
palette, no flowing cloth no vines no cape no physics elements,
all armor rigid and solid, game-ready character model, PBR textures,
T-pose or A-pose
```

**Orion — The Conductor (VOIDTOUCHED Mage)**
```
Dark fantasy male storm mage, fitted rigid arcane coat that is stiff
and form-fitting not flowing, arcane symbols engraved into the coat
surface, glowing blue vein-like lines on skin, wild but solid
sculpted hair with static electricity look, intense glowing eyes,
electric blue and dark purple color palette, lean build, no robes
no flowing cloth no cape no loose elements, all clothing rigid and
fitted, game-ready character model, PBR textures, T-pose or A-pose
```

**Nyx — The Shadow That Drinks (UNCHAINED Hybrid)**
```
Dark fantasy female shadow entity in humanoid form, fitted rigid
hooded outfit with solid edges, pale ghostly skin contrasting with
dark clothing, deep purple and black color palette with crimson
accents, sculpted hood that is rigid not draped, haunting beautiful
face with glowing violet eyes, solid body silhouette with clean
edges, no flowing cloth no dissolving edges no cape no physics
elements, all clothing rigid and form-fitting, game-ready character
model, PBR textures, T-pose or A-pose
```

### Starter Monster Models

**Skitter-Teeth (Vex's Starter — IRON)**
```
Dark fantasy horror creature, animated ribcage skeleton crawling on
bone finger-legs like a spider, rows of sharp teeth growing from
inside the chest cavity, small grotesque creature about knee height,
iron-grey bone color with rust and metal accents, rigid bone
structure no soft tissue, unsettling skittering pose, game-ready
creature model, PBR textures
```

**Grimthorn (Seraphina's Starter — SAVAGE)**
```
Dark fantasy carnivorous plant monster, large venus flytrap head on
a twisted thorny body, multiple rigid barbed limbs, bioluminescent
poison-green sap details painted on surface, bark-like hard textured
skin with embedded thorns, medium sized aggressive creature, no
soft flexible parts all rigid plant matter, game-ready creature
model, PBR textures
```

**Voltgeist (Orion's Starter — RUIN)**
```
Dark fantasy electric ghost creature, spectral humanoid figure with
solid translucent body, glowing blue-white coloring with visible
skeletal structure painted on surface, energy arc details sculpted
into form, distorted face frozen in a scream, medium sized ethereal
creature, solid mesh with translucent material not actual transparency,
game-ready creature model, PBR textures
```

**Bloodshade (Nyx's Starter — VOID)**
```
Dark fantasy shadow creature, amorphous dark mass with solid mesh
form, glowing crimson eyes, tendril shapes sculpted upward from body
as rigid geometry, occasional face-like shapes sculpted into surface,
deep purple-black with crimson accent details, small to medium sized
horror creature, solid mesh not transparent, game-ready creature
model, PBR textures
```

### Tripo Settings (All Models)
- **Mode:** Text to 3D
- **Style:** Realistic or Dark Fantasy
- **Quality:** High
- **Output:** GLB/GLTF (Unity-compatible)
- **Key instruction:** Emphasize "rigid, solid, no cloth physics, game-ready topology" in all prompts

---

## Technical Architecture

### Scripts (New)

| File | Purpose |
|------|---------|
| `Assets/Scripts/UI/CharacterSelect/CharacterSelectController.cs` | Main controller — carousel logic, hero switching, embark |
| `Assets/Scripts/UI/CharacterSelect/HeroStageController.cs` | Per-hero 3D scene management, model loading, idle animations |
| `Assets/Scripts/UI/CharacterSelect/CarouselController.cs` | Bottom carousel navigation, input handling |
| `Assets/Scripts/UI/CharacterSelect/CameraController.cs` | Cinematic camera orbiting, transitions, zoom |
| `Assets/Scripts/UI/CharacterSelect/EnvironmentController.cs` | Environment swapping, Veil-tear transitions, neutral toggle |
| `Assets/Scripts/UI/CharacterSelect/StatsPanel.cs` | Contextual stats display (hover/keypress) |

### UI Files

| File | Purpose |
|------|---------|
| `Assets/UI/Screens/CharacterSelect.uxml` | Minimal UI overlay (carousel, embark, back, toggle) |
| `Assets/UI/Styles/CharacterSelect.uss` | Styling for glass panels, carousel, buttons |

### Scene Structure

```
CharacterSelect (Scene)
├── CameraRig
│   └── Main Camera (CameraController)
├── Environments
│   ├── IronPrison (SetActive per selection)
│   ├── Thornwaste
│   ├── StormSpire
│   ├── VoidRift
│   ├── NeutralBase
│   └── TeaserVoid
├── HeroStages
│   ├── VexStage (hero model + Skitter-Teeth + positions)
│   ├── SeraphinaStage
│   ├── OrionStage
│   └── NyxStage
├── VFX
│   ├── VeilTearTransition
│   ├── PerHeroParticles (ember/spore/lightning/shadow)
│   └── EmbarkTransitionVFX
├── Audio
│   ├── AmbientMixer
│   └── HeroMotifs (4 audio sources)
├── UI
│   └── UIDocument (CharacterSelect.uxml)
└── CharacterSelectManager (main controller)
```

### Data Flow

```
heroes.json → HeroData[] → CharacterSelectController
                              │
                              ├── CarouselController (thumbnails, navigation)
                              ├── HeroStageController (load model, idle, monster)
                              ├── EnvironmentController (swap environment)
                              ├── CameraController (orbit, transition, zoom)
                              ├── StatsPanel (on-demand display)
                              └── Embark → PlayerPrefs → SceneManager.LoadScene("MainGame")
```

### Key Implementation Notes

1. **Environment toggle** saves preference to PlayerPrefs so it persists between visits
2. **Hero models** loaded async on scene start, cached — no loading hitch on carousel switch
3. **Veil tear transition** is a fullscreen shader effect, not geometry
4. **Monster idle animations** are simple procedural (bobbing, orbiting, perching) — not complex skeletal animation
5. **Stats panel** reads from heroes.json BaseStats — same data source as combat
6. **Audio** uses Unity's AudioMixer with snapshot blending for smooth motif transitions
7. **All VFX** (lightning, embers, spores, shadows) are Unity Particle System or VFX Graph — separate from 3D models

---

## Files to Archive (Old Design)

These files are from the previous character select implementation and should be moved to `Docs/archive/`:

- `Docs/AAA_CHARACTER_SELECT_DESIGN.md`
- `Docs/AAA_CHARACTER_SELECT_SETUP.md`
- `Docs/AAA_CHARACTER_SELECT_SUMMARY.md`
- `Assets/Scripts/UI/CharacterSelect/CharacterSelectControllerAAA.cs`
- `Assets/Scripts/UI/CharacterSelect/CharacterSelectVFXController.cs`
- `Assets/UI/Screens/CharacterSelectAAA.uxml`
- `Assets/UI/Styles/CharacterSelectAAA.uss`
- `Assets/Scripts/UI/Menus/CharacterSelectController.cs`
- `Assets/Scripts/UI/Menus/CharacterSelectBackdropController.cs`
- `Docs/plans/2026-01-19-hero-character-design.md` (old hero roster — Bastion/Rend/Marrow/Mirage)

---

## Next Steps

1. **Generate 3D models** using Tripo prompts above (user handles this)
2. **Create environment assets** (AI-generated skyboxes + props, or asset store)
3. **Build Unity scene** structure per architecture above
4. **Implement carousel controller** with keyboard/mouse navigation
5. **Implement camera system** with auto-orbit and player rotation
6. **Implement Veil-tear transition** shader
7. **Wire up hero data** from heroes.json
8. **Add VFX overlays** per hero (particles, lighting)
9. **Add sound design** (ambient loops, motifs, UI sounds)
10. **Polish** — timing, easing, feel

---

*Design approved — 2026-02-05*
