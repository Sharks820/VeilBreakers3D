# VeilBreakers 3D - Combat UI Design Document

**Version:** 2.0
**Date:** 2026-01-18
**Status:** FINAL APPROVED

---

## Overview

Real-time tactical combat UI for VeilBreakers 3D. Game-style floating HUD with maximum combat visibility (85%+). All elements are floating with transparent backgrounds - NO boxed/webpage-style layouts.

## Combat Philosophy

- **Real-time continuous** - No turn-based, combat flows constantly
- **Smart AI rules** (Gambits) - Allies auto-execute based on conditions
- **Quick commands** - Player can override with hold-button system
- **Tactical pause** - ONLY for bosses/mini-bosses (rare)
- **Ally ultimate control** - Player can trigger ally ultimates manually

---

## HUD Layout (1920x1080) - FINAL

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
│  YOU                                    ENEMY                    [🎒][📈][⚙️]│
│ ┌────┐ ████████████░░░ HP              ┌────┐ ██████████░░░ HP               │
│ │ ◯  │ ██████████░░░░░ MP              │ ◯  │ Corruption: 45%                │
│ └────┘ [b][b][d][d]                    └────┘                                │
│ PLAYER                                 TARGET                       MENU     │
│ (attached to top-left)                (centered at top)      (top-right)    │
│                                                                              │
│                                                    ◯ A1  ████████░░          │
│                                                    [b][d][s1][s2][★]         │
│                              COMBAT                                          │
│                               AREA                 ◯ A2  ██████░░░           │
│                             (85%+)                 [b][d][s1][s2][★]         │
│                                                                              │
│                                                    ◯ A3  █████░░░░           │
│                                                    [b][d][s1][s2][★]         │
│                                                                              │
│  [⚔️]  [🛡️]  [🔥]  [❄️]  [⚡]  [💀]  [💥]       ══╣ C ╠══[ CAPTURE ]══════   │
│   Q     E     1     2     3     4     R                                      │
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

---

## Component Specifications

### 1. Player Panel (Top-Left - Attached to Edge)

**Position:** Top-left, attached to screen edge (no gap)
**Size:** 220px × 90px
**Style:** Floating, semi-transparent background

**Layout:**
```
YOU (name label)
┌────┐ ████████████░░░ HP (full width bar)
│ ◯  │ ██████████░░░░░ MP (full width bar)
└────┘ [b][b][d][d] (buff/debuff icons)
```

**Contents:**
- Name Label: Above portrait
- Portrait: 48×48px, positioned under name, adjacent to HP bar
- HP Bar: Full width, 12px height, red gradient
- MP Bar: Full width, 10px height, blue gradient
- Status Icons: 20×20px each, max 8 visible (buffs + debuffs)

**Visual Style:**
- Background: rgba(0, 0, 0, 0.6) - semi-transparent
- NO border boxes - floating appearance
- HP gradient: #8B0000 → #FF0000
- MP gradient: #00008B → #4169E1

---

### 2. Enemy Panel (Top-Center - Centered)

**Position:** Top-center, horizontally centered, attached to top edge
**Size:** 280px × 70px
**Style:** Floating, semi-transparent background

**Layout:**
```
ENEMY NAME (centered)
┌────┐ ██████████░░░ HP (bar)
│ ◯  │ Corruption: 45%
└────┘
```

**Contents:**
- Name Label: Centered above portrait
- Portrait: 48×48px, positioned under name
- HP Bar: 180px wide, 12px height
- Corruption %: Text display (NO status icons - can't see enemy buffs)

**Important:** Enemy does NOT show buffs/debuffs - player can't see those.

**Visual Style:**
- Background: rgba(0, 0, 0, 0.6)
- NO border boxes
- Corruption text color changes based on level (green→yellow→red)

---

### 3. Menu Icons (Top-Right Corner)

**Position:** Top-right, tight to corner
**Size:** 3 icons × 32px each

**Icons:**
- 🎒 Inventory
- 📈 Stats/Menu
- ⚙️ Settings

**Style:** Small floating icons, glow on hover

---

### 4. Ally Panel (Right Side - Floating Vertical)

**Position:** Right side, vertically centered (NOT in corner)
**Size:** 180px × 45px per ally (3 allies stacked)
**Style:** Floating rows, semi-transparent

**Per Ally Row:**
```
◯ AllyName  ████████░░ HP
[b][d][s1][s2][★]
```

**Contents Per Ally:**
- Portrait: 32×32px (GLOWS GOLD when ultimate ready)
- Name: Short, next to portrait
- HP Bar: 80px wide, 8px height
- Status Icons: 16×16px (buffs/debuffs row)
- Skill Icons: 20×20px each - Skills 1, 2, 3, Ultimate (★)
- NO basic attack/defend icons (always available, no cooldown)

**Skill Icon States:**
| State | Visual |
|-------|--------|
| Ready | Full color, subtle glow |
| Cooldown | Grayscale + clock sweep overlay |
| In Use | Pulsing bright |
| Low MP | Blue tint |

**Ultimate Indicator:**
- Portrait border glows GOLD when ultimate ready
- Subtle shimmer/particle effect
- ★ icon glows gold

---

### 5. Player Skills (Bottom Center - Floating Icons)

**Position:** Bottom center, sitting ON bottom edge (no gap)
**Size:** 7 icons × 48px each with keybinds below

**Layout:**
```
[⚔️]  [🛡️]  [🔥]  [❄️]  [⚡]  [💀]  [💥]
 Q     E     1     2     3     4     R
```

**Slot Assignments:**
| Key | Slot | Purpose |
|-----|------|---------|
| Q | Basic Attack | Always available |
| E | Defend | Always available |
| 1-4 | Skills 1-4 | Cooldown-based |
| R | Ultimate | Long cooldown (45-90s) |

**Visual Style:**
- Icons: 48×48px with subtle drop shadow
- Keybinds: 10px text below each icon
- Ultimate (R): Gold border, shows countdown when on CD
- Ready skills glow slightly
- Cooldown: Dark sweep overlay + seconds remaining

---

### 6. Capture Banner (Bottom-Right - Contextual)

**Position:** Bottom-right area, appears when enemy capturable
**Style:** Breathing pulse animation when active

**Design:**
```
══╣ C ╠══[ CAPTURE ]══════
```

**Capture Mechanic (Single C Button):**
1. **Press C during combat** → Mark target for capture (indicator appears on enemy)
2. **Combat continues** → Deal damage to reach capture threshold
3. **Banner flashes/breathes** → Enemy is now capturable
4. **Press C again** → Execute capture (post-combat or instant for special abilities)

**Banner States:**
| State | Visual |
|-------|--------|
| Hidden | Not visible (no valid target) |
| Marked | Dim banner, target has capture indicator |
| Ready | GLOWING + breathing pulse animation |
| Capturing | Flash animation during capture attempt |

**Visual Style:**
- Banner uses parchment/gold theme
- "C" keybind integrated into banner design
- Breathing animation: 1.5s loop, subtle scale pulse (1.0 → 1.03)

---

## Interaction System

### Target Cycling
| Input | Action |
|-------|--------|
| TAB | Cycle to next enemy |
| Shift+TAB | Cycle to previous enemy |
| Mouse Click | Direct target selection |

### Skill Activation
| Input | Action |
|-------|--------|
| Q | Basic attack (no cooldown) |
| E | Defend (no cooldown) |
| 1-4 | Activate skill 1-4 |
| R | Activate Ultimate |

### Ally Ultimate Trigger
| Input | Action |
|-------|--------|
| F1 | Trigger Ally 1's Ultimate |
| F2 | Trigger Ally 2's Ultimate |
| F3 | Trigger Ally 3's Ultimate |

**Targeting Logic:**
- Damage Ultimate → Current enemy target
- Healing Ultimate → Lowest HP ally or player
- Buff Ultimate → Self or contextual

### Quick Command (Slow-Mo)
| Input | Action |
|-------|--------|
| Hold Shift | Enter command mode (25% time speed) |
| + Direction | Select command type |
| + TAB | Cycle targets |
| Release | Execute command |

### Capture System
| Input | Action |
|-------|--------|
| C | Mark target / Execute capture (context-dependent) |

---

## Visual Style Guide

### Design Philosophy
- **Game-style floating elements** - NOT webpage-style boxes
- **Maximum combat visibility** - HUD takes minimal screen real estate
- **Semi-transparent backgrounds** - See action through HUD elements
- **NO heavy borders** - Subtle shadows for depth
- **Parchment/gold theme** - Matches VeilBreakers dark fantasy aesthetic

### Color Palette
| Element | Color | Hex |
|---------|-------|-----|
| Panel Background | Semi-transparent black | rgba(0,0,0,0.6) |
| HP Bar (full) | Bright Red | #FF0000 |
| HP Bar (empty) | Dark Red | #8B0000 |
| MP Bar (full) | Royal Blue | #4169E1 |
| MP Bar (empty) | Dark Blue | #00008B |
| Ultimate Ready | Gold Glow | #FFD700 |
| Cooldown Overlay | Dark Gray | rgba(0,0,0,0.7) |
| Text Primary | Cream/Parchment | #F5E6D3 |
| Text Secondary | Light Gray | #C0C0C0 |
| Danger/Low HP | Pulsing Red | #FF0000 (animate) |
| Capture Ready | Gold Pulse | #FFD700 (animate) |
| Corruption Low | Green | #4CAF50 |
| Corruption Mid | Yellow | #FFEB3B |
| Corruption High | Red | #F44336 |

### Typography
- **Primary Font:** Clean sans-serif (Unity UI default or custom fantasy)
- **Sizes:**
  - Names: 14px
  - Numbers: 12px
  - Labels: 10px
  - Keybinds: 10px

### Animations
| Animation | Duration | Easing |
|-----------|----------|--------|
| Cooldown Sweep | Matches cooldown time | Linear |
| HP Change | 0.3s | Ease-out |
| Ultimate Glow | 2s loop | Sine wave |
| Low HP Pulse | 1.5s loop | Ease-in-out |
| Capture Banner Breathe | 1.5s loop | Ease-in-out |
| Skill Ready Glow | 1s loop | Sine wave |
| Target Popup | 0.15s | Ease-out |
| Ally Ultimate Ready | 2s loop | Gold pulse |

---

## Responsive Scaling

| Resolution | Scale Factor | Notes |
|------------|--------------|-------|
| 1920×1080 | 1.0x | Base design |
| 2560×1440 | 1.33x | Proportional scale |
| 3840×2160 | 2.0x | 4K support |
| 1280×720 | 0.67x | Minimum supported |

---

## Implementation Priority

### Phase 1: Core HUD
1. Player HP/MP panel (top-left)
2. Enemy target panel (top-center)
3. Player skill bar with keybinds (bottom-center)
4. Cooldown system

### Phase 2: Ally System
5. Ally panel with portraits (right side, floating)
6. Ally skill display (timed skills only)
7. Ally buff/debuff icons
8. Ultimate ready indicator (gold glow)

### Phase 3: Interaction
9. Target cycling (TAB/Shift+TAB)
10. Capture system (C key, banner)
11. Quick command slow-mo (Hold Shift)
12. Ally ultimate triggers (F1-F3)

### Phase 4: Polish
13. Animations (breathing, glows, pulses)
14. Sound feedback
15. Visual effects (particles on ultimate ready)
16. Menu icons (top-right)

---

## Notes

- **NO boxes/borders** - Use shadows and transparency for floating effect
- **Enemy shows Corruption %, NOT buffs/debuffs** - Player can't see enemy status
- **Ally skills shown: Skills 1-3 + Ultimate** - NO basic attack/defend (always available)
- **Portrait glows GOLD when ultimate ready** - No separate button needed
- **Capture uses single C button** - Context-dependent (mark → capture)
- **Tactical pause only for bosses/mini-bosses** - Rare occurrence
- **AI behavior via Gambits system** - Separate configuration UI
- **All top bars attached to screen edge** - No floating gap
- **All bottom elements sit on bottom line** - Grounded appearance
