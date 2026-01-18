# VeilBreakers 3D - Gambits AI System Design

**Version:** 1.0
**Date:** 2026-01-18
**Status:** PENDING APPROVAL

---

## Overview

Utility-based AI system for ally combat behavior. Inspired by Final Fantasy XII Gambits and Dragon Age Tactics, with brand-specific intelligence that makes each monster type feel unique.

**Core Philosophy:**
- Presets that work well out of the box
- Customizable thresholds for advanced players
- Brand-dependent smart defaults
- Execute low HP enemies (prioritize kills)
- Focus fire on debuffed targets
- Attackers avoid tanks (unless armor-shredded)

---

## Architecture: Utility Scoring

### Why Utility Over IF-THEN

Traditional gambits: "IF ally HP < 50% THEN Heal" - rigid, predictable, exploitable.

**Utility Scoring:** Calculate numerical value for EVERY possible action, pick highest.

```
ActionScore = BaseValue × SituationMultipliers × BrandModifiers
```

### The Bucket System (from The Sims)

Before scoring, categorize into priority buckets:

| Priority | Bucket | Example |
|----------|--------|---------|
| 1 | CRITICAL | Self about to die, ally critical |
| 2 | HIGH | Execute opportunity, interrupt cast |
| 3 | STANDARD | Normal damage rotation, healing |
| 4 | LOW | Buff refresh, positioning |

Handle CRITICAL bucket completely before evaluating STANDARD.

---

## Universal Multipliers (All Brands)

These form the BASE that all brands build upon:

### Target Selection Multipliers

| Condition | Multiplier | Reasoning |
|-----------|------------|-----------|
| Target HP < 25% | × 2.5-3.0 | **EXECUTE** - finish kills |
| Target HP 25-50% | × 1.3 | Weakened, worth focusing |
| Target HP > 75% | × 0.9 | Fresh target, less urgent |
| Target has ANY debuff | × 1.5 | Focus fire on debuffed |
| Target has ARMOR SHRED | × 2.0 | Maximum vulnerability |
| Target is HEALER/SQUISHY | × 1.5-1.8 | High value target |
| Target is TANK (no shred) | × 0.3-0.5 | Avoid, waste of damage |
| Target is TANK (WITH shred) | × 1.2 | Now viable target |
| Target is casting | × 1.8 | Interrupt opportunity |

### Self/Ally Multipliers

| Condition | Multiplier | Reasoning |
|-----------|------------|-----------|
| Self HP < 20% | × 3.0 survival | Desperation mode |
| Ally HP < 30% | × 2.0 to help | Team preservation |
| Ultimate ready | × 1.5 to use | Don't sit on power |

---

## Brand-Specific AI Systems

### IRON (Defensive Tank)

**Role:** Protect allies, absorb damage, control threat.

**Category Weights:**
- Survival: 40%
- Team Value: 35%
- Positioning: 15%
- Damage: 10%

**IRON-Specific Multipliers:**

| Condition | Multiplier | Action |
|-----------|------------|--------|
| Squishy ally under attack | × 2.5 | Guard/intercept |
| Taunt off cooldown + enemy on ally | × 2.0 | Taunt |
| Self HP < 30% | × 1.8 | Defensive cooldowns |
| Self HP > 70% | × 1.5 | Aggressive positioning |
| Multiple enemies on one ally | × 3.0 | AOE taunt/guard |

**Decision Buckets:**
1. EMERGENCY GUARD: Ally under 20% HP being attacked → Intercept
2. TAUNT MANAGEMENT: Enemy hitting squishies → Taunt rotation
3. SELF SUSTAIN: Own HP dropping → Defensive abilities
4. POSITIONING: Move between threats and allies
5. CHIP DAMAGE: Safe to attack → Basic attacks

**Unique Mechanics:**
- THREAT MEMORY: Track which enemies have been taunted recently
- GUARD PRIORITY: Healers > DPS > Self
- COOLDOWN STAGGER: Don't blow all defensives at once

---

### SAVAGE (Melee Burst DPS)

**Role:** Maximum single-target damage, execute kills, snowball momentum.

**Category Weights:**
- Damage Efficiency: 50%
- Execute Priority: 25%
- Survival: 15%
- Team Value: 10%

**SAVAGE-Specific Multipliers:**

| Condition | Multiplier | Action |
|-----------|------------|--------|
| Target HP < 25% | × 3.0 | EXECUTE - all cooldowns |
| Target HP < 50% + debuffed | × 2.5 | Focus burst |
| Target is HEALER | × 1.8 | Priority target |
| Target is TANK (no shred) | × 0.3 | AVOID |
| Target is TANK (WITH shred) | × 1.5 | Now viable |
| Just got a kill | × 1.3 | Frenzy momentum |
| Self HP < 30% | × 0.7 damage, × 2.0 survive | Back off |

**Decision Buckets:**
1. EXECUTE: Enemy under 25% → All-in burst
2. KILL SECURED: Enemy will die to current combo → Finish
3. FOCUS FIRE: Ally debuffed target → Pile on
4. STANDARD DPS: Highest value squishy target
5. SURVIVAL: Self low → Disengage/defensive

**Unique Mechanics:**
- FRENZY STACKING: Kills grant temporary damage boost
- OVERKILL AVOIDANCE: Don't waste big CD on 5% HP target
- MOMENTUM TRACKING: Chain kills for bonuses

---

### SURGE (Ranged Artillery DPS)

**Role:** Safe damage from range, kiting, AOE when clustered.

**Category Weights:**
- Damage Efficiency: 40%
- Positioning: 30%
- Survival: 20%
- Team Value: 10%

**SURGE-Specific Multipliers:**

| Condition | Multiplier | Action |
|-----------|------------|--------|
| At optimal range (far) | × 1.5 | Full damage rotation |
| Enemy closing distance | × 2.0 | KITE - move away |
| Enemy in melee range | × 0.5 damage, × 2.5 escape | Disengage priority |
| Multiple enemies clustered | × 2.0 | AOE skills |
| Clear line of sight | × 1.3 | Take the shot |
| Ally blocking shot | × 0.6 | Reposition |

**Decision Buckets:**
1. ESCAPE: Enemy in melee → Dash/kite away
2. AOE OPPORTUNITY: 3+ enemies clustered → Big AOE
3. EXECUTE: Ranged finish on low HP target
4. STANDARD: Maintain range, DPS rotation
5. REPOSITION: Bad angle → Move for clear shot

**Unique Mechanics:**
- DISTANCE SCORING: Further = safer = more damage
- KITE PATHING: Smart movement away from threats
- CHARGE SHOT: Hold for bigger damage when safe

---

### VENOM (DoT/Debuff Specialist)

**Role:** Apply debuffs, heal reduction on healers, maximize DoT uptime.

**Category Weights:**
- Debuff Coverage: 45%
- Team Value: 25%
- Damage Efficiency: 20%
- Survival: 10%

**VENOM-Specific Multipliers:**

| Condition | Multiplier | Action |
|-----------|------------|--------|
| Target is HEALER (no heal reduction) | × 3.0 | Apply heal reduction FIRST |
| Target has no DoTs | × 2.0 | Fresh DoT application |
| Target has 1 DoT | × 1.5 | Stack more DoTs |
| Target is TANK | × 0.5 | DoTs inefficient on high HP |
| Enemy just cleansed | × 2.5 | Reapply immediately |
| Multiple targets no DoTs | × 2.0 | Spread DoTs |

**Decision Buckets:**
1. HEAL SHUT DOWN: Enemy healer → Heal reduction priority
2. COUNTER CLEANSE: Enemy cleansed → Reapply DoTs
3. SPREAD DOTS: Fresh targets → Apply DoTs
4. MAINTAIN: Refresh expiring DoTs
5. DIRECT DAMAGE: All DoTs applied → Filler damage

**Unique Mechanics:**
- DOT TRACKING: Know exactly what's applied where
- PANDEMIC LOGIC: Refresh DoTs before they fall off
- HEAL REDUCTION PRIORITY: Always before other DoTs on healers

---

### DREAD (CC Controller)

**Role:** Crowd control, interrupts, lock down key threats.

**Category Weights:**
- Control Value: 45%
- Team Value: 30%
- Positioning: 15%
- Damage: 10%

**DREAD-Specific Multipliers:**

| Condition | Multiplier | Action |
|-----------|------------|--------|
| Enemy casting big ability | × 3.0 | INTERRUPT |
| Enemy about to heal | × 2.5 | Interrupt heal |
| Target CC will expire soon | × 2.0 | Chain CC |
| Target has CC immunity | × 0.1 | Don't waste CC |
| Target fresh (no recent CC) | × 1.5 | Good CC target |
| Multiple enemies, one un-CC'd | × 2.0 | Lock down loose threat |

**Decision Buckets:**
1. INTERRUPT: Enemy casting → Stop them NOW
2. CC CHAIN: Existing CC expiring → Extend lockdown
3. THREAT LOCKDOWN: Dangerous enemy → Prioritize CC
4. GROUP CONTROL: Multiple enemies → AOE CC
5. CHIP DAMAGE: All CC'd → Safe damage

**Unique Mechanics:**
- DIMINISHING RETURNS: Track CC history, don't waste on immune
- CC CHAIN TIMING: Don't overlap, chain as one ends
- INTERRUPT PRIORITY: Heals > Big damage > Buffs
- LOCKDOWN TARGET: Mark one enemy as "keep CC'd"

---

### LEECH (Drain Tank)

**Role:** Sustain through damage, outlast enemies, drain to survive.

**Category Weights:**
- Sustain Value: 45%
- Survival: 30%
- Damage Efficiency: 15%
- Team Value: 10%

**LEECH-Specific Multipliers:**

| Condition | Multiplier | Action |
|-----------|------------|--------|
| Self HP < 50% | × 2.5 | DRAIN PRIORITY (heal self) |
| Self HP < 25% | × 3.5 | DESPERATE DRAIN |
| Self HP > 80% | × 0.8 drain, × 1.5 damage | More aggressive |
| Target has high HP | × 1.5 | Better drain target |
| Target low HP | × 0.8 | Less drain value |
| Multiple enemies | × 1.3 | AOE drain value |

**Decision Buckets:**
1. DESPERATE: Self under 25% → Biggest drain ability
2. SUSTAIN: Self 25-50% → Prioritize drain skills
3. AGGRESSIVE: Self healthy → Damage rotation
4. TEAM DRAIN: Can drain to heal ally → Do it
5. BASIC: Maintain sustain, chip damage

**Unique Mechanics:**
- DRAIN = SURVIVAL: Damage dealt = Health restored (unique math)
- SUSTAIN CALCULATION: Track HP gain per ability
- HIGH HP TARGETS: More HP to drain = better target
- NO TANK AVOIDANCE: LEECH can fight tanks (sustained battle)

---

### GRACE (Battle Healer)

**Role:** Reactive healing, cleanse debuffs, emergency saves.

**Category Weights:**
- Team Value: 55%
- Survival: 25%
- Mana Efficiency: 15%
- Damage: 5%

**GRACE-Specific Multipliers:**

| Condition | Multiplier | Action |
|-----------|------------|--------|
| Ally HP < 25% | × 4.0 | EMERGENCY HEAL |
| Ally HP < 50% | × 2.5 | Priority heal |
| Ally HP 50-75% | × 1.2 | Light healing |
| Ally HP > 90% | × 0.3 | Don't overheal |
| Ally has dangerous debuff | × 3.0 | CLEANSE |
| Multiple allies damaged | × 2.0 | AOE heal |
| Self mana < 20% | × 0.5 healing | Conserve mana |

**Decision Buckets:**
1. EMERGENCY: Ally under 25% → Biggest heal NOW
2. CLEANSE: Dangerous debuff → Remove immediately
3. TRIAGE: Multiple wounded → Heal lowest first
4. MAINTENANCE: Light healing to top off
5. DAMAGE: Everyone healthy → Contribute damage

**Unique Mechanics:**
- TRIAGE SYSTEM: Calculate who needs healing most
- CLEANSE PRIORITY: DOTs > CC > Debuffs
- MANA MANAGEMENT: Don't go OOM, reserve for emergency
- OVERHEAL AVOIDANCE: Don't waste healing on full HP

---

### MEND (Shield Support)

**Role:** Proactive shields, damage prevention, team protection.

**Category Weights:**
- Team Value: 50%
- Prediction: 25%
- Survival: 15%
- Damage: 10%

**MEND-Specific Multipliers:**

| Condition | Multiplier | Action |
|-----------|------------|--------|
| Ally about to be attacked | × 3.0 | SHIELD before hit |
| Ally has no shield | × 2.0 | Apply shield |
| Ally shield about to expire | × 1.8 | Refresh shield |
| Ally already has strong shield | × 0.3 | Don't stack |
| Big enemy attack incoming | × 3.5 | Preemptive shield |
| Team grouped | × 2.5 | AOE shield value |

**Decision Buckets:**
1. INCOMING DAMAGE: Big hit coming → Shield target
2. SHIELD COVERAGE: Unshielded allies → Apply shields
3. REFRESH: Expiring shields → Maintain uptime
4. BURST PROTECTION: Boss ability → Team shield
5. DAMAGE: Shields stable → Contribute damage

**Unique Mechanics:**
- PROACTIVE: Shields BEFORE damage, not after
- INCOMING DAMAGE PREDICTION: Watch enemy cast bars
- SHIELD VS HEAL: If ally has healer, shield more; if not, limited heal
- STACK PREVENTION: Same shield doesn't stack, track coverage

---

### RUIN (AOE Devastator)

**Role:** Maximum area damage, cluster punishment, wave clear.

**Category Weights:**
- Damage Efficiency: 45%
- Team Value: 25%
- Survival: 20%
- Positioning: 10%

**RUIN-Specific Multipliers:**

| Condition | Multiplier | Action |
|-----------|------------|--------|
| 4+ enemies clustered | × 2.5 | BIG AOE - maximum value |
| 3 enemies clustered | × 1.8 | Worth AOE |
| 2 enemies clustered | × 1.0 | Acceptable AOE |
| 1 enemy only | × 0.6 | Single-target instead |
| Ally just CC'd group | × 2.5 | COMBO - AOE now |
| Multiple enemies under 30% | × 3.0 | CLEANUP SWEEP |
| Enemies scattered | × 0.4 | Don't waste AOE |

**Decision Buckets:**
1. CLUSTERED EXECUTE: 3+ enemies under 50% → Nuke
2. CC COMBO: Ally grouped enemies → Immediate AOE
3. STANDARD AOE: 3+ targets → AOE rotation
4. SINGLE TARGET: Only 1 enemy → Basic attacks
5. SURVIVAL: Self threatened → Kite, request peel

**Unique Mechanics:**
- TARGET COUNT: AOE value scales exponentially with targets
- COMBO AWARENESS: Watch for ally CC setup
- OVERKILL PREVENTION: Don't waste big CD on dying group
- WAVE AWARENESS: Save cooldowns for incoming adds

---

### VOID (Chaos Mage)

**Role:** Reality warping, high variance, desperate situation specialist.

**Category Weights:**
- Chaos Value: 40%
- Team Value: 30%
- Damage Efficiency: 20%
- Survival: 10%

**VOID-Specific Multipliers:**

| Condition | Multiplier | Action |
|-----------|------------|--------|
| Team disadvantage (<40% HP avg) | × 1.8 | Unleash chaos |
| Near wipe (ally under 15%) | × 2.5 | Desperate measures |
| Team at advantage (>70% HP) | × 0.7 | Hold back |
| Enemy casting big ability | × 2.0 | Disrupt/reflect |
| Enemy heavily buffed | × 1.8 | Steal/dispel |
| Target strongest enemy | × 1.5 | Chaos hurts them most |
| Target low HP enemy | × 0.8 | Doesn't care about executes |

**Decision Buckets:**
1. DESPERATE SAVE: Near wipe → Biggest chaos ability
2. DISRUPT CAST: Enemy charging → Interrupt/reflect
3. BUFF WARFARE: Steal/dispel valuable buffs
4. REALITY WARP: Apply VOID debuffs
5. OPPORTUNISTIC: Random effects when nothing critical

**Unique Mechanics:**
- DESPERATION SCALING: Gets STRONGER as team loses
- BUFF THEFT: Steals buffs, doesn't just dispel
- RANDOM EMBRACE: AI accepts variance, prefers big swings
- MOMENTUM INVERSION: Can flip damage/healing

---

## Ultimate Override Window

### Flow
1. Ultimate becomes ready → Portrait glows gold, breathing animation
2. **5 second window** for player input (F1/F2/F3)
3. If player triggers → Player controls target
4. If no input → AI auto-targets based on brand logic

### Brand Ultimate Targeting

| Brand | Auto-Target Logic |
|-------|-------------------|
| IRON | Ally with most enemies on them |
| SAVAGE | Lowest HP enemy (execute) |
| SURGE | Largest enemy cluster |
| VENOM | Enemy healer or highest value target |
| DREAD | Most dangerous un-CC'd enemy |
| LEECH | Highest HP enemy (max drain) |
| GRACE | Lowest HP ally |
| MEND | Ally about to take most damage |
| RUIN | Largest enemy cluster |
| VOID | Strongest enemy OR desperate save ally |

---

## Preset System

### Quick Presets (Per Brand)
Each brand has 4 quick presets players can toggle:

| Preset | Effect |
|--------|--------|
| Focus Attack | Increase damage weights +30% |
| Focus Defend | Increase survival weights +30%, guard allies more |
| Focus Heal | (Support only) Prioritize healing over damage |
| Protect [Ally] | Designate one ally as protection priority |

### Threshold Customization
Players can adjust default thresholds:

| Setting | Default | Range |
|---------|---------|-------|
| Low HP Threshold | 50% | 20-70% |
| Critical HP Threshold | 25% | 10-40% |
| Mana Conservation | 20% | 10-40% |
| Execute Priority | 25% | 15-35% |

### Advanced Rules (Optional)
For advanced players, expose full gambit editor:

```
IF [Condition] THEN [Action] WITH [Target]
Priority: 1-10
Enabled: Yes/No
```

Example:
```
IF ally HP < 30% THEN Heal WITH lowest HP ally
Priority: 1
Enabled: Yes
```

---

## Defend Mechanics

### Defend-Only Brands
Only these brands auto-defend:
- IRON (primary function)
- LEECH (when drain on CD)
- MEND (when shields on CD)

### Non-Defensive Brands
These brands ONLY defend when:
- Player explicitly commands
- Self HP critically low (<15%) AND no escape

**NO DEFEND SPAM** - Defend has internal cooldown of 3 seconds between uses.

---

## Buff/Debuff Intelligence

### Buff Rules
- Don't buff ally who already has that buff
- Different buffs CAN stack
- Same buff refreshes duration, doesn't stack
- Prioritize buffing allies about to engage

### Debuff Removal
- Healers/Support auto-cleanse dangerous debuffs
- Priority: DOTs > CC > Stat debuffs
- Don't waste cleanse on minor debuffs

### Potion System
- Game PAUSES when using potion
- 3-5 second cooldown between potion uses
- Maximum 3 potions per combat session
- AI will suggest potion use, player confirms

---

## Implementation Priority

### Phase 1: Core Framework
1. Utility scoring system
2. Universal multipliers
3. Basic bucket system
4. Action selection loop

### Phase 2: Brand Implementation
5. IRON and SAVAGE (tank + DPS templates)
6. GRACE and MEND (healer templates)
7. Remaining 6 brands

### Phase 3: Player Interface
8. Quick preset toggles
9. Threshold sliders
10. Ultimate override window

### Phase 4: Advanced
11. Full gambit editor (optional)
12. AI behavior tuning
13. Edge case handling

---

## Notes

- **Execute enemies** - Low HP = high priority, not low priority
- **Focus fire on debuffed** - Armor shred means everyone piles on
- **Tanks are avoided** - Unless armor-shredded, then they're viable
- **Each brand feels unique** - Different weights = different behavior
- **Presets work out of box** - No configuration required for casual play
- **Advanced customization available** - For players who want control

