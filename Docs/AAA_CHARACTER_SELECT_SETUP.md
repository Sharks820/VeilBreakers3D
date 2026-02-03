# AAA Character Select Screen - Setup Guide

## Overview

The AAA Character Select screen provides a cinematic, immersive hero selection experience with:

- 5-layer parallax atmospheric backgrounds
- Animated hero silhouettes with brand-colored auras
- Monster companions that orbit heroes
- Vertical stat pillar animations
- Glassmorphism UI panels
- Hexagonal "ritual" embark button
- Smooth corruption-style transitions between heroes

## Files Created

### UI Layout
- `Assets/UI/Screens/CharacterSelectAAA.uxml` - Main layout file

### Styling  
- `Assets/UI/Styles/CharacterSelectAAA.uss` - Complete stylesheet
- `Assets/UI/Styles/VeilBreakersTheme.uss` - Updated with hero color tokens

### Scripts
- `Assets/Scripts/UI/CharacterSelect/CharacterSelectControllerAAA.cs` - Main controller
- `Assets/Scripts/UI/CharacterSelect/CharacterSelectVFXController.cs` - VFX manager

## Setup Instructions

### 1. Create the UI Document GameObject

1. In Unity, create an empty GameObject named `CharacterSelectScreen`
2. Add a **UI Document** component
3. Set the Source Asset to `CharacterSelectAAA.uxml`
4. Set the Sorting Order to ensure it's above other UI

### 2. Add Controllers

Add both controller scripts to the same GameObject:
- `CharacterSelectControllerAAA`
- `CharacterSelectVFXController`

### 3. Configure Data References

On **CharacterSelectControllerAAA**:
- Assign `heroes.json` to the Heroes Json field
- Assign `monsters.json` to the Monsters Json field
- Assign audio clips for ambient loop, select SFX, and confirm SFX

### 4. Configure VFX Controller

On **CharacterSelectVFXController**:
- Assign a Render Camera (can be a separate camera for 3D models)
- Create and assign ParticleSystems for each hero:
  - `vexParticles` - Iron filings, metallic sparks
  - `seraphinaParticles` - Poison mist, green petals
  - `orionParticles` - Lightning arcs, static electricity
  - `nyxParticles` - Shadow wisps, memory fragments
- Assign lighting (Rim Light, Fill Light)

### 5. Create Required Textures

Create the following textures in `Assets/UI/Textures/`:

#### Background
- `void_stars.png` - Starfield for deep background
- `veil_cracks.png` - Veil crack overlays
- `fog_particles.png` - Fog/mist texture (tiling)
- `aura_base.png` - Base aura circle
- `aura_particles.png` - Particle field texture
- `hex_button.png` - Hexagonal button frame
- `corruption_noise.png` - Noise texture for transition effect

#### Hero Silhouettes (600x800px recommended)
- `hero_vex_silhouette.png` - Vex full body
- `hero_seraphina_silhouette.png` - Seraphina full body  
- `hero_orion_silhouette.png` - Orion full body
- `hero_nyx_silhouette.png` - Nyx full body

#### Monster Thumbnails (128x128px)
- `monster_skitter_teeth.png`
- `monster_grimthorn.png`
- `monster_voltgeist.png`
- `monster_bloodshade.png`

#### Icons
- `star_empty.png` / `star_filled.png` - Synergy rating
- `arrow.png` - Carousel navigation
- `arrow_left.png` - Back button
- `settings.png` - Settings button
- Path icons for IRONBOUND, FANGBORN, VOIDTOUCHED, UNCHAINED

### 6. Create Ability Icons

Create 3 icons per hero in `Assets/UI/Icons/Abilities/`:
- `{hero_id}_ability_1.png`
- `{hero_id}_ability_2.png`  
- `{hero_id}_ultimate.png`

### 7. Scene Setup

1. Create a new scene called `CharacterSelect`
2. Add the `CharacterSelectScreen` GameObject
3. Add a background camera with:
   - Clear Flags: Solid Color (dark)
   - Culling Mask: Nothing (or specific layers)
4. Add a UI camera if using 3D model renders

### 8. Build Settings

Add the `CharacterSelect` scene to Build Settings (index 1 recommended, after TitleScreen).

## Runtime Color System

The controller sets CSS variables at runtime to theme the UI:

```css
--hero-r: 140;  /* Red component */
--hero-g: 150;  /* Green component */  
--hero-b: 165;  /* Blue component */
--hero-color-primary: rgb(140, 150, 165);
--hero-color-secondary: rgb(180, 190, 205);
```

These are applied to:
- Hero name glow
- Stat bar fills
- Ability card highlights
- Button borders
- Ambient lighting

## Hero Data Requirements

Each hero in `heroes.json` needs:

```json
{
  "hero_id": "vex",
  "display_name": "Vex",
  "title": "The Warden",
  "quote": "I don't kill monsters. I break them.",
  "primary_brand": 0,
  "primary_path": 1,
  "starter_monster_id": "skitter_teeth",
  "base_stats": {
    "strength": 14,
    "dexterity": 10,
    "constitution": 14,
    "intelligence": 10,
    "wisdom": 12,
    "charisma": 10
  },
  "innate_skills": [
    {"skill_name": "Chained Strike", "description": "..."},
    {"skill_name": "Iron Will", "description": "..."}
  ]
}
```

## Animation Timing

### Hero Switch Sequence (1.2s total)
1. 0.0s - Transition overlay fade in
2. 0.1s - Hero-specific effects (lightning for Orion)
3. 0.3s - Update all visual elements
4. 0.5s - Transition overlay fade out
5. 0.6s - Stat bars animate (0.8s duration)
6. 1.0s - Quote fades in

### Initial Reveal Sequence (2.5s total)
1. 0.0s - Background fades in
2. 0.3s - Carousel slides up
3. 0.5s - First hero auto-selected
4. 0.8s - Hero display fades in
5. 1.2s - Stat pillars animate
6. 1.5s - Monster fades in
7. 2.0s - Embark button activates

## Customization

### Changing Hero Colors

Edit `heroColors` dictionary in `CharacterSelectControllerAAA.cs`:

```csharp
private readonly Dictionary<string, Color> heroColors = new Dictionary<string, Color>
{
    { "vex", new Color(0.55f, 0.59f, 0.65f) },
    // ... etc
};
```

### Changing Environment Backgrounds

Edit `heroEnvironments` dictionary:

```csharp
private readonly Dictionary<string, string> heroEnvironments = new Dictionary<string, string>
{
    { "vex", "prison_ruins" },
    { "seraphina", "poison_garden" },
    // ... etc
};
```

### Adjusting Animation Speeds

Modify these fields in the Inspector:
- `heroSwitchDuration` - How long hero transitions take
- `statAnimationDuration` - How fast stat bars fill
- `quoteDelay` - Delay before showing hero quote

## Accessibility Features

The system supports:
- **Reduced Motion**: Disables particle animations
- **High Contrast**: Increases panel opacity and border widths
- **Keyboard Navigation**: Arrow keys to navigate, Enter to confirm

Add to player settings:
```csharp
PlayerPrefs.SetInt("ReducedMotion", 1);
PlayerPrefs.SetInt("HighContrast", 1);
```

## Troubleshooting

### Stats not animating
- Verify `heroes.json` has valid `base_stats` for all 6 attributes
- Check that stat pillar elements are named correctly: `stat-str`, `stat-dex`, etc.

### Hero colors not applying
- Ensure CSS variables are being set: Check browser/Unity console for USS errors
- Verify color values are 0-255 range when setting via `style.SetProperty()`

### Carousel not responding
- Verify hero cards have `data-hero` attributes matching hero IDs
- Check that click event handlers are registered in `SetupEventHandlers()`

### Background layers not visible
- Ensure all background layers are in the UXML file
- Check z-index ordering in USS
- Verify textures exist at the paths specified in USS

## Future Enhancements

- [ ] 3D Model integration via RenderTexture
- [ ] Dynamic particle systems per hero
- [ ] Voiceover quotes on selection
- [ ] Custom entrance animations per hero
- [ ] Interactive ability card expansion
- [ ] Monster interaction on hover
