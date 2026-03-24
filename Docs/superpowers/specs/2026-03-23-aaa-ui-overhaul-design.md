# AAA UI Overhaul — Design Spec

**Date:** 2026-03-23
**Status:** APPROVED
**Mockup:** `.superpowers/brainstorm/stable/aaa-v5.html`

---

## 1. Title Screen

### Layout
- Video background (seamless loop, no glitch jump) with fallback PNG
- Demon figure: center, idle breathing animation (subtle float), interactive hover glow
- VeilBreakers logo: top-center, 80%+ width, dark radial backing for visibility, clickable (pulse + smoke burst), pulsing glow animation
- Veo/AI watermark: hidden with corner overlay (existing `corner-overlay-br`)
- Buttons: vertical stack, right-aligned, bottom 8%

### Buttons (ALL same size)
- **Size:** 300px wide x 56px tall
- **Base state:** Dark gradient (`rgba(50,38,28,0.96)` → `rgba(25,18,10,0.98)`), 1px border `rgba(140,100,50,0.3)`, Cinzel font 18px bold, letter-spacing 5px, color `rgba(210,180,130,0.85)`
- **Decorative:** Inner top highlight line (gradient), shine sweep on hover
- **Hover:** Orange gradient (`rgba(230,120,35)` → `rgba(145,55,10)`), border brightens to `rgba(255,180,80,0.7)`, text goes white-gold, box-shadow glow 35px, scale 1.04, translateY -2px
- **Active:** scale 0.97, inset shadow
- **Order:** NEW GAME, CONTINUE (hidden if no save), SETTINGS, CREDITS, EXIT

### VFX Layer
- Ember particles: 14+ CSS-animated floating upward with drift
- Heat haze: radial gradient behind demon, pulsing opacity
- Vignette overlay: heavy (transparent center 30%, black edge 80%)
- Scanlines: 3px repeating, 3% opacity
- Ash flakes: larger, slower floating particles

### Audio (implementation-time)
- Ambient music fade-in on scene load
- Random demon laugh sounds
- Girl crying softly (ambient layer)
- Button hover/click SFX (existing system)

### Interactions
- Logo: click triggers pulse scale + smoke burst (existing TitleScreenVFX)
- Demon: hover brightens + increases glow drop-shadow
- Buttons: hover → orange + sweep, click → press feedback

---

## 2. Character Select

### Layout (unchanged structure)
- Left 55%: Hero stage with 3D model (full spotlight, no champion on stage)
- Right 42%: Info panel (tabbed)
- Bottom 16%: Carousel cards + embark button aligned
- Top: HERO 1/4 indicator centered

### Hero Stage
- Hero glow: radial gradient centered BEHIND hero model (moved up from bottom)
- Hero model: rotatable via drag, idle animation when not interacting
- Hero name watermark: 54px Cinzel Decorative, 6% opacity, below model
- Hero title subtitle: 11px, hero color, letter-spacing 6px
- NO champion model on stage — hero gets full spotlight

### Info Panel
- **Background:** Multi-stop gradient (not flat), `rgba(18,14,26,0.96)` → `rgba(6,4,12,0.98)`
- **Border:** 1px subtle + 2px top border in hero color (50% opacity)
- **Top glow line:** Gradient fade centered on top edge in hero color
- **Box-shadow:** Layered — 60px dark outer, 30px dark, inset top highlight, inset dark inner, 15px hero-color glow
- **Tabs:** Underline style with hero-color active indicator, letter-spacing 3px

### Hero Identity
- Name: Cinzel Decorative, 28-32px, 900 weight, text-shadow glow in hero color
- Title ("THE WARDEN"): Cinzel, 11-12px, hero color, left border accent 3px
- Quote: Segoe UI, 10-11px, italic, muted purple `rgba(180,170,200,0.3)`
- Info rows: compact inline — PATH, ROLE, SYNERGY with values

### Ornamental Dividers
- Thin gradient line (fade in/out from edges)
- Centered diamond `◆` in hero color at 35% opacity
- Between each major section

### Combat Stats (BARS) — 2x2 Grid
| Position | Stat | Color | Bright | Glow |
|----------|------|-------|--------|------|
| Top-left | HP | `#cc3838` | `#ff5555` | red 30% |
| Top-right | STAMINA | `#38a855` | `#55dd70` | green 30% |
| Bottom-left | ATK | `#cc8820` | `#ffaa33` | orange 30% |
| Bottom-right | DEF | `#3878cc` | `#55aaff` | blue 30% |

- Each stat card: dark bg, 1px top accent line in stat color
- Bar: gradient fill (stat color → bright), glowing leading edge (3px bright + shadow)
- Value: Cinzel 14px bold, text-shadow in stat glow color
- **Hover animation:** Card lifts 1px, bg darkens, border glows in stat color, box-shadow 12px, label brightens to full opacity, value gets stronger text-shadow
- **Number animation:** Values count up/down (lerp) when switching heroes — slot-machine style ticking

### Attributes (NUMBERS only) — 6-column Grid
- STR, DEX, CON, INT, WIS, CHA
- Cinzel 18-20px, 900 weight, no bars, no modifiers
- Dark bg chip with subtle border, hover darkens + border brightens
- Label: 7-8px uppercase, muted

### Champion Monster — 50/50 Split Layout
- **Left half (48%):** 3D model viewer with spotlight gradient, rotatable (drag), border in hero color
- **Right half (52%):**
  - Name: Cinzel Decorative 16px, hero gold color, text-shadow glow
  - Brand: Cinzel 12px, "⬥ IRON BRAND", hero color, bottom border separator
  - Tags: IRON (highlighted in hero color bg), TANK (neutral)
  - Description: Segoe UI 10px, muted purple, 1.6 line-height
- **Area fills ALL remaining panel space** below attributes

### Carousel Cards — 80x100px
- Per-hero colors (gold/purple/red/cyan) via CSS variables
- Large initial letter: Cinzel Decorative 30px
- Hero name: Cinzel 8px
- **Active:** Lifted 6px, scale 1.08, glowing border in hero color, box-shadow 18px, selection dot below
- **Hover (inactive):** Lifted 3px, border brightens, letter opacity increases to 0.45
- Cards change color when hero is selected (theme system applies)

### Nav Arrows
- 34px circular gradient buttons
- SVG chevron icons (not unicode)
- Hover: border brightens, glow 12px, scale 1.1, icon brightens

### Embark Button
- 240px wide, 54px tall
- Gradient gold fill with decorative top highlight line
- Breathing glow animation (2.5s cycle)
- Shine sweep animation (3.5s cycle)
- **"HOLD TO CONFIRM":** 9px bold, letter-spacing 4px, pulsing opacity animation
- **Hold press:** Scale 0.96, background brightens to white-gold, border brightens, 60px glow
- **On confirm:** Plays unique character voice line (laugh/cheer/voice line per hero)

### Theme System (per-hero, applied to root)
| Hero | Primary | Secondary | Accent | Glow |
|------|---------|-----------|--------|------|
| Vex | `rgb(200,160,60)` | `rgb(180,140,40)` | `#ffd270` | 20% alpha |
| Seraphina | `rgb(150,90,210)` | `rgb(130,70,190)` | `#d2aaff` | 20% alpha |
| Orion | `rgb(200,60,60)` | `rgb(180,45,45)` | `#ff8866` | 20% alpha |
| Nyx | `rgb(70,190,210)` | `rgb(50,170,190)` | `#82e6fa` | 20% alpha |

Theme affects: panel border, tab underline, stat left-accents, embark button color, card colors, hero glow, watermark subtitle, champion brand highlight.

---

## 3. Font Strategy

| Context | Font | Weight | Usage |
|---------|------|--------|-------|
| Hero names, card letters | Cinzel Decorative | 900 | Display only |
| Titles, stat values, buttons, champion name | Cinzel | 700 | Semi-display |
| Body text, labels, descriptions, tags | Segoe UI | 400/700 | Readability |

---

## 4. Audio Requirements (New)

### Title Screen
- Ambient dark music (fade-in on load)
- Random demon laugh (low frequency, random interval)
- Girl crying softly (ambient layer, very quiet)

### Character Select
- Per-hero voice line on embark confirm
- Navigation tick on hero switch
- Hero switch audio feedback (existing)

---

## 5. Video Requirements

- Seamless loop of title background video (fix current jump-cut)
- Remove Veo watermark from video
- Consider AI-generating a better background video

---

## 6. Implementation Priority

1. **USS overhaul** — Buttons, panel depth, stat colors, champion layout, embark styling
2. **UXML updates** — Button layout vertical, champion 50/50, stat rename SPD→STAMINA
3. **C# animations** — Number lerp on hero switch, stat bar hover, embark hold effect
4. **VFX polish** — Ember count, demon hover interaction, logo visibility
5. **Audio** — Voice lines, ambient sounds (can be placeholder initially)
6. **Video** — Seamless loop fix (separate task)
