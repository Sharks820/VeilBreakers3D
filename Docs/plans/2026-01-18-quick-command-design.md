# VeilBreakers 3D - Quick Command System Design

**Version:** 1.0
**Date:** 2026-01-18
**Status:** APPROVED

---

## Overview

Radial wheel menu system for issuing tactical commands to allies during real-time combat. Time slows to 25% while menu is open. Click or Enter to confirm selections.

**Key Points:**
- Q key opens Quick Menu (ally commands only)
- Player skills remain in bottom UI bar (Q, E, 1-4, R)
- 12 second cooldown PER ALLY after commanding
- Can command different allies immediately

---

## Controls

### Keybinds

| Key | Function |
|-----|----------|
| Q | Open Quick Command Menu |
| TAB | Cycle enemy targets |
| CTRL | Cycle ally targets |
| Arrow Keys / WASD | Navigate wheel options |
| Click / Enter | Confirm selection |
| Right-Click / ESC / Q | Cancel and close |

### Input Methods

Both mouse and keyboard fully supported:

**Mouse Flow:**
1. Press Q → Wheel appears
2. Move cursor toward option → Highlights
3. Click → Confirms

**Keyboard Flow:**
1. Press Q → Wheel appears
2. Arrow keys to highlight
3. Enter → Confirms

---

## Quick Command Flow

### Step 1: Open Menu
- Press Q
- Time slows to 25% speed
- Radial wheel appears with 3 ally portraits

### Step 2: Select Ally
- Move toward ally portrait OR press F1/F2/F3
- Click or Enter to confirm
- **Grayed out** if ally is on 12s cooldown

### Step 3: Select Command
- Second wheel appears with command options
- Move toward command
- Click or Enter to confirm

### Step 4: Target Selection (if needed)
- Some commands require target
- TAB cycles enemies, CTRL cycles allies
- Click or Enter to confirm target
- **Reposition** shows ground cursor for placement

### Step 5: Execute
- Ally executes command
- **12 second cooldown** starts on that ally
- Menu closes, time resumes normal speed

---

## Ally Command Options

### Direct Commands

| Command | Behavior | Targeting |
|---------|----------|-----------|
| **Attack Target** | Attack player's current target | Auto (current target) |
| **Defend Target** | Guard a specific ally | CTRL to cycle allies |
| **Defend Player** | Guard the player | Auto |
| **On Me** | Come to player, auto-defend, attack threats in range, reform after | Auto |
| **Fall Back** | Retreat from current position | Auto |
| **Reposition / Go Here** | Move to specific location | Ground cursor click |
| **Return to Formation** | Go back to default position | Auto |

### Tactical Presets

| Preset | Behavior |
|--------|----------|
| **Aggressive** | Prioritize damage, chase targets |
| **Defensive** | Prioritize survival, stay close |
| **Support** | Prioritize healing/buffs |
| **Focus Target** | All attacks on player's target |
| **Protect Player** | Stay near player, intercept threats |

---

## "On Me" Command Detail

Special command that chains multiple behaviors:

1. Ally moves to player's position
2. Enters "Defend Player" stance
3. Auto-attacks any enemy entering player's defense range
4. After threat eliminated/leaves range:
   - If enemy dies → Reform to original formation
   - If enemy bound for capture → Stay alert
   - If enemy leaves range → Reform to original formation

---

## Cooldown System

### Per-Ally Cooldown
- 12 seconds after commanding an ally
- Cannot command SAME ally again until cooldown expires
- CAN command DIFFERENT allies immediately
- Visual: Grayed portrait + timer overlay

### No Global Cooldown
- Rapid commands to different allies allowed
- Encourages tactical variety
- Prevents spam on single ally

---

## Visual Design

### Radial Wheel Style
- Circle around cursor when Q pressed
- 3 ally portraits on first wheel (equal spacing)
- 7-10 command options on second wheel
- Highlight glow on hover/selection
- Semi-transparent background (see combat behind)

### Cooldown Display
- Ally portrait grayed out when on cooldown
- Radial timer sweep showing remaining time
- Cannot select grayed allies

### Time Slow Indicator
- Screen edge vignette (subtle)
- Slight desaturation
- "TACTICAL" text indicator (optional)

---

## AOE Ground Targeting

For **Reposition** command:

1. Select Reposition from command wheel
2. Enemy target deselects
3. Ground cursor appears
4. Move cursor to desired location
5. AOE indicator shows ally's "arrive zone"
6. Click to confirm placement
7. Ally moves to location and holds

---

## Edge Cases

### Ally Incapacitated
- Dead/stunned ally portrait shows X
- Cannot select incapacitated allies
- Cooldown pauses while incapacitated

### Combat Ends During Menu
- Menu auto-closes
- No command executes
- Cooldowns reset

### Multiple Commands Queued
- Each ally can only have ONE active command
- New command overwrites previous
- No command stacking

---

## Implementation Priority

### Phase 1: Core Menu
1. Q key time slow trigger
2. Radial wheel rendering
3. Ally portrait display
4. Click/Enter confirmation

### Phase 2: Commands
5. Attack Target execution
6. Defend Target/Player execution
7. On Me full behavior chain
8. Reposition ground targeting

### Phase 3: Polish
9. Cooldown system + visuals
10. Tactical presets
11. Visual feedback (highlights, vignette)
12. Sound effects

---

## Notes

- Player skills stay in bottom bar - Quick Menu is ALLY ONLY
- 12 second cooldown prevents ally spam
- Both mouse and keyboard fully supported
- Time slow gives tactical breathing room without pausing
- "On Me" is most complex command - full behavior chain

