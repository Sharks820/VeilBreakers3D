# AAA Character Select Screen - Design Document

## Vision: "The Summoning"

When players enter character select, they don't "pick a hero"—they **summon a Veilbreaker** from the darkness. The screen should feel like a ritual, with each hero emerging from the Veil as you focus on them.

## Core Pillars

1. **Cinematic Presence** - Heroes are life-sized, dramatically lit
2. **Atmospheric Depth** - Background changes per hero (5+ parallax layers)
3. **Living Bond** - Starter monster visible, orbiting hero
4. **Tactile Feedback** - Every interaction has weight and response
5. **Seamless Flow** - No jarring transitions, everything flows

## Layout Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│  [ATMOSPHERIC BACKGROUND - 5 parallax layers]                               │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  Layer 1: Deepest (void, stars, distant Veil cracks)               │    │
│  │  Layer 2: Far atmosphere (fog, mist, energy particles)             │    │
│  │  Layer 3: Midground (hero-specific environment elements)           │    │
│  │  Layer 4: Near atmosphere (glow, lens flare, particles)            │    │
│  │  Layer 5: Foreground vignette (heavy, focuses attention)           │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                             │
│                                                                             │
│                    ┌─────────────────────────────┐                          │
│                    │                             │     ┌─────────────────┐  │
│                    │      HERO DISPLAY           │     │   STAT PILLARS  │  │
│                    │                             │     │   (Vertical)    │  │
│                    │   • Full-body silhouette    │     │                 │  │
│                    │   • Dramatic rim lighting   │     │   STR  ████░    │  │
│                    │   • Brand-colored aura      │     │   DEX  ███░░    │  │
│                    │   • Breathing animation     │     │   CON  █████    │  │
│                    │   • Eye glow effect         │     │   INT  ███░░    │  │
│                    │                             │     │   WIS  ████░    │  │
│                    │   MONSTER COMPANION         │     │   CHA  ███░░    │  │
│                    │   • Orbits slowly           │     │                 │  │
│                    │   • Synergy tether glow     │     └─────────────────┘  │
│                    │   • Reactive to selection   │                          │
│                    │                             │     ┌─────────────────┐  │
│                    └─────────────────────────────┘     │  ABILITY SHOW   │  │
│                                                        │  (3 holographic │  │
│                                                        │   cards)        │  │
│                                                        └─────────────────┘  │
│                                                                             │
│  ╔═══════════════════════════════════════════════════════════════════════╗  │
│  ║                                                                       ║  │
│  ║    VEX              SERAPHINA           ORION            NYX          ║  │
│  ║    ┌───┐            ┌───┐               ┌───┐            ┌───┐        ║  │
│  ║    │   │            │   │               │   │            │   │        ║  │
│  ║    └───┘            └───┘               └───┘            └───┘        ║  │
│  ║   [Selected]       [Next]              [Next]          [Next]         ║  │
│  ║                                                                       ║  │
│  ╚═══════════════════════════════════════════════════════════════════════╝  │
│                                                                             │
│                                                                             │
│     THE WARDEN                    The last warden of the Black Iron...    │
│                                                                             │
│     ┌─────────────────────────────────────────────────────────────────┐    │
│     │  ◄  EMBARK AS VEX  ►                                            │    │
│     │     "I don't kill monsters. I break them."                      │    │
│     └─────────────────────────────────────────────────────────────────┘    │
│                                                                             │
│  [BACK]                                                        [SETTINGS]  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Component Specifications

### 1. Atmospheric Background System

**5-Layer Parallax:**
- Layer 1 (Deepest): Static void with subtle Veil crack textures
- Layer 2 (Far): Slow-moving fog particles, color-tinted per hero
- Layer 3 (Midground): Hero-specific environmental elements
  - Vex: Prison ruins, chains, bars
  - Seraphina: Thorn vines, carnivorous plants
  - Orion: Storm clouds, lightning flashes
  - Nyx: Shadow tendrils, memory fragments
- Layer 4 (Near): Atmospheric glow, lens flare effects
- Layer 5 (Foreground): Heavy vignette (60% opacity at edges)

**Transition Effect:**
When switching heroes, background "dissolves" via pixelated corruption effect over 0.8s

### 2. Hero Display (Center Stage)

**Dimensions:**
- Position: Center-screen, 60% height
- Size: ~500px width, full height
- Z-Depth: Floats above background, below UI

**Visual Elements:**
- **Base:** Hero silhouette/model with dramatic rim lighting
- **Aura:** Brand-colored particle emission from hero
  - Vex: Iron filings, steel sparks
  - Seraphina: Poison mist, thorn petals
  - Orion: Lightning arcs, static electricity
  - Nyx: Shadow wisps, memory fragments
- **Eyes:** Glowing eye effect (intensity varies by hero)
- **Breathing:** Subtle 2% scale pulse every 4 seconds

**Monster Companion:**
- Position: Orbits hero in elliptical path (12 second loop)
- Visual: 30% size of hero, positioned at "shoulder"
- Synergy Tether: Glowing line connecting monster to hero
  - Strong synergy = bright, steady glow
  - Weak synergy = flickering, dim line

### 3. Stat Pillars (Right Side)

**Layout:**
- Position: Right side, vertically stacked
- Size: 80px width, 300px total height
- Style: Vertical bars with "fill" animation

**Animation:**
- On hero select: Bars animate from 0 to value over 1.2s
- Highest stat glows with brand color
- Bars have subtle "pulse" when maxed

**Visual Style:**
- Glass/crystal texture
- Background: Dark with grid lines
- Fill: Brand-colored gradient
- Numbers: Float at top of each bar

### 4. Hero Carousel (Bottom)

**Layout:**
- Position: Bottom-center, 100% width
- Height: 150px
- Style: 3D perspective carousel

**Behavior:**
- Selected hero: Center, 1.3x scale, full color, glow effect
- Adjacent heroes: 1.0x scale, 80% opacity, slight blur
- Far heroes: 0.8x scale, 50% opacity, heavy blur

**Interaction:**
- Mouse wheel: Scroll carousel
- Arrow keys: Navigate
- Click: Select hero
- Hover: Preview (subtle glow, name appears)

**Visual Design:**
- Each hero card: 120px x 160px
- Background: Glass panel with hero color tint
- Content: Portrait, name, path icon
- Border: 2px with brand color

### 5. Hero Information (Bottom Center)

**Elements:**
- **Hero Name:** 48px, bold, brand-colored glow
- **Title:** 18px, uppercase, tracking 4px
- **Quote:** Italic, 14px, fades in after selection

**Animation:**
- Name: Slides up with fade (0.4s)
- Title: Types out character by character (0.8s)
- Quote: Fades in last (1.0s delay)

### 6. Confirm Button (Bottom)

**Style:**
- Size: 300px x 60px
- Shape: Hexagonal (fitting the "ritual" theme)
- Default: Dark with subtle border
- Hover: Brand-colored glow, scale 1.05
- Active: Full brand color, particle burst

**Text:**
- "EMBARK AS [HERO NAME]"
- Changes dynamically with selection

### 7. Ability Showcase (Right, below stats)

**Layout:**
- 3 holographic cards
- 200px x 80px each
- Stacked vertically with 10px gap

**Visual:**
- Glass/card hologram effect
- Ability icon (left)
- Name + brief description (right)
- Hover: Expands to show full description

## Animation Timeline

### On Screen Open (2.5s total)
```
0.0s - Background fades in (0.5s)
0.3s - Carousel slides up (0.6s, ease-out-back)
0.5s - First hero (Vex) auto-selected
0.8s - Hero display fades in with particles (0.8s)
1.2s - Stat pillars animate up (0.6s, staggered 0.1s each)
1.5s - Hero name/title appears (0.4s)
2.0s - Monster companion fades in (0.5s)
2.2s - Confirm button becomes active
```

### On Hero Switch (1.2s total)
```
0.0s - Current hero begins dissolve (0.3s)
0.1s - Background starts transition (0.8s)
0.3s - New hero begins form (0.5s)
0.4s - Monster companion switches (0.4s)
0.6s - Stats animate to new values (0.4s)
0.8s - Name/title updates (0.3s)
1.0s - Quote fades in (0.3s)
```

## Technical Implementation

### Required Files

1. **CharacterSelect.uxml** - New layout structure
2. **CharacterSelectAAA.uss** - All styling
3. **CharacterSelectController.cs** - Logic (updated)
4. **CharacterSelectVFXController.cs** - Visual effects
5. **ParallaxBackground.cs** - Background layers

### UXML Structure

```xml
<ui:UXML>
  <!-- Background Layers -->
  <ui:VisualElement name="bg-layer-1" class="parallax-bg deep"/>
  <ui:VisualElement name="bg-layer-2" class="parallax-bg far"/>
  <ui:VisualElement name="bg-layer-3" class="parallax-bg mid"/>
  <ui:VisualElement name="bg-layer-4" class="parallax-bg near"/>
  <ui:VisualElement name="vignette-overlay"/>
  
  <!-- Main Content -->
  <ui:VisualElement name="main-stage">
    <!-- Hero Display -->
    <ui:VisualElement name="hero-display">
      <ui:VisualElement name="hero-aura"/>
      <ui:VisualElement name="hero-model"/>
      <ui:VisualElement name="hero-eyes"/>
    </ui:VisualElement>
    
    <!-- Monster Companion -->
    <ui:VisualElement name="monster-orbit">
      <ui:VisualElement name="monster-model"/>
      <ui:VisualElement name="synergy-tether"/>
    </ui:VisualElement>
  </ui:VisualElement>
  
  <!-- Right Panel -->
  <ui:VisualElement name="right-panel">
    <ui:VisualElement name="stat-pillars">
      <StatPillar name="str" label="STR"/>
      <StatPillar name="dex" label="DEX"/>
      <StatPillar name="con" label="CON"/>
      <StatPillar name="int" label="INT"/>
      <StatPillar name="wis" label="WIS"/>
      <StatPillar name="cha" label="CHA"/>
    </ui:VisualElement>
    
    <ui:VisualElement name="ability-cards">
      <AbilityCard name="ability-1"/>
      <AbilityCard name="ability-2"/>
      <AbilityCard name="ability-3"/>
    </ui:VisualElement>
  </ui:VisualElement>
  
  <!-- Bottom Carousel -->
  <ui:VisualElement name="hero-carousel">
    <ui:VisualElement name="carousel-container">
      <HeroCard hero-id="vex"/>
      <HeroCard hero-id="seraphina"/>
      <HeroCard hero-id="orion"/>
      <HeroCard hero-id="nyx"/>
    </ui:VisualElement>
  </ui:VisualElement>
  
  <!-- Hero Info -->
  <ui:VisualElement name="hero-info">
    <ui:Label name="hero-name"/>
    <ui:Label name="hero-title"/>
    <ui:Label name="hero-quote"/>
  </ui:VisualElement>
  
  <!-- Confirm Button -->
  <ui:Button name="btn-embark" text="EMBARK"/>
  
  <!-- Navigation -->
  <ui:Button name="btn-back" text="BACK"/>
</ui:UXML>
```

## USS Styling Highlights

### Glassmorphism Panels
```css
.glass-panel {
    background-color: rgba(15, 12, 20, 0.75);
    backdrop-filter: blur(12px);
    border-width: 1px;
    border-color: rgba(255, 255, 255, 0.1);
    border-radius: 8px;
}
```

### Glowing Text
```css
.hero-name {
    font-size: 48px;
    -unity-font-style: bold;
    color: rgb(235, 225, 215);
    text-shadow: 0 0 20px rgba(180, 165, 145, 0.8),
                 0 0 40px rgba(180, 165, 145, 0.4);
}
```

### Animated Aura
```css
.hero-aura {
    position: absolute;
    background-image: url('particles/aura_base.png');
    animation: aura-pulse 4s ease-in-out infinite;
}

@keyframes aura-pulse {
    0%, 100% { opacity: 0.6; scale: 1; }
    50% { opacity: 0.9; scale: 1.05; }
}
```

### Stat Pillar Fill
```css
.stat-fill {
    background: linear-gradient(180deg, 
        rgba(180, 165, 145, 0.9) 0%,
        rgba(180, 165, 145, 0.4) 100%);
    transition: height 0.6s cubic-bezier(0.4, 0, 0.2, 1);
}
```

## Performance Considerations

1. **Object Pooling:** Reuse particle systems between hero switches
2. **Texture Atlasing:** Hero portraits in single atlas
3. **LOD System:** Lower-res hero models for background carousel
4. **VFX Batching:** Group similar particle effects
5. **Async Loading:** Load hero assets in background

## Responsive Design

**16:9 (Standard)**
- Full layout as designed

**21:9 (Ultrawide)**
- Hero display shifts left
- More space for stat/ability panels
- Carousel spreads out

**16:10 (Older)**
- Slight compression of vertical elements
- Hero display 55% height instead of 60%

## Accessibility

1. **High Contrast Mode:** Increase UI element contrast
2. **Reduced Motion:** Disable particle effects, instant transitions
3. **Screen Reader:** Full labels for all interactive elements
4. **Color Blind:** Pattern overlays on stat bars

## Next Steps

1. Create base UXML layout
2. Implement USS styling
3. Build CharacterSelectController
4. Add VFX controller
5. Test all hero transitions
6. Polish animations and timings
