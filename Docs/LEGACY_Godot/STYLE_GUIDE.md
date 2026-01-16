# VEILBREAKERS - Art Style Guide

> **DEFINITIVE ART BIBLE** | Version: 1.0 | Last Updated: 2026-01-02

---

## Visual Identity Summary

**Genre:** Dark Fantasy Horror RPG
**Style:** Dark silhouette creatures with glowing focal points, atmospheric painterly backgrounds
**Mood:** Ominous, dramatic, supernatural, unsettling
**Inspiration:** Ethereal horror, shadow creatures, atmospheric darkness (NOT comic book)

---

## Art Direction

### Core Visual Pillars

| Pillar | Description |
|--------|-------------|
| **Dark Fantasy** | Corrupted worlds, demonic influence, fallen creatures |
| **Horror Elements** | Unsettling details, glowing eyes, body horror acceptable |
| **High Quality Painterly** | Maximum detail, professional game quality |
| **Dramatic Lighting** | Glowing elements against deep shadows |
| **Atmospheric** | Dark, ominous mood throughout |

### Trained Models (Scenario.gg)

| Model | ID | Purpose |
|-------|-----|---------|
| **VeilBreakersV1** | `model_fmiEeZzx1zUMf4dhKeo16nqK` | Core style - dark fantasy, spectral |
| **Dark Fantasy V2** | `model_Wp9nDSoMoAfnAzmhYF4N2GnZ` | Creatures and UI (training) |

---

## Color Palette

### Primary Colors

| Name | Hex | RGB | Usage |
|------|-----|-----|-------|
| Veil Black | #0d0d14 | 13, 13, 20 | Deep shadows, backgrounds |
| Midnight Blue | #1a1a2e | 26, 26, 46 | Primary dark tone |
| Corruption Purple | #4a0e4e | 74, 14, 78 | Corruption effects |
| Blood Red | #8b0000 | 139, 0, 0 | SAVAGE brand, damage |
| Bone White | #f0e6d3 | 240, 230, 211 | Skeletal, highlights |

### Accent Colors

| Name | Hex | RGB | Usage |
|------|-----|-----|-------|
| Fire Orange | #ff6b35 | 255, 107, 53 | Fire, energy, warmth |
| Toxic Green | #7fff00 | 127, 255, 0 | VENOM brand, poison |
| Lightning Blue | #00bfff | 0, 191, 255 | SURGE brand, electricity |
| Ethereal Purple | #9932cc | 153, 50, 204 | DREAD brand, specters |
| Rust Orange | #b7410e | 183, 65, 14 | IRON brand, decay |

### UI Colors

| Element | Hex | Purpose |
|---------|-----|---------|
| Panel Background | #141419 | Dark UI panels |
| Border Normal | #665940 | Muted gold borders |
| Border Hover | #99804D | Bright gold hover |
| Font Primary | #E6D9B3 | Warm ivory text |
| Font Hover | #FFF2CC | Bright gold hover text |
| HP Bar | #4a8f4a | Green health |
| MP Bar | #4a6a8f | Blue mana |
| Enemy HP | #8f4a4a | Red enemy health |

---

## Brand Visual Language

### Pure Brands

| Brand | Color Accent | Texture | Visual Motifs |
|-------|--------------|---------|---------------|
| **SAVAGE** | Deep reds, bone white | Rough, torn, bloody | Fangs, claws, primal fury |
| **IRON** | Gunmetal, rust orange | Metallic, pitted, corroded | Armor plates, chains, fortresses |
| **VENOM** | Sickly green, purple | Wet, dripping, organic | Oozing wounds, spore clouds |
| **SURGE** | Electric blue, white | Crackling, bright, energized | Lightning arcs, speed lines |
| **DREAD** | Deep purple, void black | Ethereal, smoky, wispy | Multiple eyes, shadows, whispers |
| **LEECH** | Dark red, pale flesh | Organic, pulsing, veiny | Parasites, draining tendrils |

### Hybrid Brand Combinations
Hybrids blend their parent brand visuals 70/30:
- **BLOODIRON:** Armored berserker (SAVAGE primary)
- **CORROSIVE:** Toxic tank (IRON primary)
- **VENOMSTRIKE:** Speed assassin (VENOM primary)
- **TERRORFLUX:** Lightning specter (SURGE primary)
- **NIGHTLEECH:** Shadow vampire (DREAD primary)
- **RAVENOUS:** Hungry beast (LEECH primary)

---

## Asset Specifications

### Resolution Standards

| Asset Type | Resolution | Format | Alpha |
|------------|------------|--------|-------|
| Monster sprites | 1024-2048px | PNG | Required |
| Hero characters | 2048px tall | PNG | Required |
| VERA stages | 2048x2048 | PNG | Required |
| Battle backgrounds | 1920x1080 | PNG | None |
| Title background | 2752x1536 | PNG | None |
| Title logo | 2048x2048 | PNG | Required |
| UI buttons | 260x290 base | PNG | Required |
| Icons | 128x128 | PNG | Required |
| Sprite sheets | Varies | PNG | Required |

### In-Game Scaling

| Asset | Scale Factor | Notes |
|-------|--------------|-------|
| Character sprites | 0.08 | Battle scene |
| Monster sprites | 0.08 | Battle scene |
| Title logo | 0.11 | Main menu |
| Eye animation | 0.5 | Main menu |

---

## Style Rules

### DO's

- Use glowing elements (eyes, runes, flames)
- Create dramatic lighting with deep shadows
- Include rich textures and painterly details
- Make monsters unsettling but readable
- Use saturated fantasy colors
- Include brand-specific visual elements
- Maintain transparency for sprites

### DON'Ts

- ❌ NO "Battle Chasers" or "Joe Madureira" style references
- ❌ NO comic book ink outlines or thick linework
- ❌ NO halftone/Ben-Day dot patterns
- ❌ NO pixel art aesthetic
- ❌ NO clean/bright fantasy look
- ❌ NO cartoony proportions
- ❌ NO flat comic colors
- ❌ NO speed lines or motion blur
- ❌ NO checker pattern fake transparency

---

## Prompt Templates

### Monster Generation
```
[monster name], dark fantasy horror creature, [brand] brand,
glowing [color] eyes, dramatic shadows, ominous atmosphere,
[brand-specific elements], high detail, painterly quality,
2D game sprite, transparent background
```

**IMPORTANT:** Always reference existing canon art in `assets/sprites/monsters/` before generating.

### Environment Generation
```
[location name], dark fantasy landscape, [biome type],
[brand] brand corruption visible, dramatic lighting,
atmospheric fog, rich detail, painterly style,
game background, 1920x1080
```

### UI Element Generation
```
[element type], dark fantasy UI, ornate frame,
gold and dark metal accents, glowing runes optional,
game interface element, transparent background
```

---

## Evolution Stage Visuals

### Stage Progression

| Stage | Size Change | Visual Complexity | Aura/Effects |
|-------|-------------|-------------------|--------------|
| Birth | 100% base | Simple, core features | None |
| Evo 2 | 120% base | Added details, developed | Subtle glow |
| Evo 3 | 140% base | Full complexity, legendary | Strong aura |

### Corruption State Visuals

| State | Corruption % | Visual Changes |
|-------|--------------|----------------|
| ASCENDED | 0-10% | Glowing aura, purified colors, serene |
| Purified | 11-25% | Soft light, healthy appearance |
| Unstable | 26-50% | Flickering, uncertain |
| Corrupted | 51-75% | Dark veins, pained expression |
| Abyssal | 76-100% | Full corruption, dark abilities visible |

---

## Canon Asset References

### Definitive Style Examples

**Monsters (assets/sprites/monsters/):**
- `hollow.png` - Base creature style
- `the_congregation.png` - **FIRST BOSS** - Multi-entity horror
- `the_weeping.png` - Emotional horror, grief incarnate
- `the_vessel.png` - Possessed/container creature
- `ravener.png` - Aggressive predator
- `mawling.png` - Hungry beast
- `gluttony_polyp.png` - Consuming horror
- `skitter_teeth.png` - Fast, toothy swarm creature
- `flicker.png` - Ethereal, phasing creature

**Heroes (assets/characters/heroes/):**
- `bastion.png` - Armored tank style
- `rend.png` - Aggressive DPS style
- `marrow.png` - Dark healer style
- `mirage.png` - Mysterious illusionist

**VERA (assets/characters/vera/):**
- `vera_interface.png` - Human appearance
- `vera_emergence.png` - Early transformation
- `vera_fracture.png` - Mid transformation
- `vera_apotheosis.png` - Full VERATH form

**Environments (assets/environments/battles/):**
- `corrupted_grove.png` - Forest biome
- `boss_arena.png` - Epic scale
- `veil_approach.png` - Endgame atmosphere

---

## File Naming Convention

```
[category]_[descriptor]_[variant].png

Examples:
- hollow.png (monster sprite)
- btn_new_game.png (UI button)
- corrupted_grove.png (environment)
- vera_apotheosis.png (character variant)
```

### Archive Naming
```
[original_name]_old_[YYYYMMDD].png

Example:
- bastion_old_20251227.png
```

---

*This style guide is the definitive reference for all VEILBREAKERS visual assets.*
*Managed by Scenario/Assets Agent. Last updated: v0.60*
