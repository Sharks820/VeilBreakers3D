# VeilBreakers 3D - Monster Capture System Design

**Version:** 1.0
**Date:** 2026-01-18
**Status:** APPROVED

---

## Overview

Two-phase capture system: Mark and Bind during combat, then Capture attempt post-battle. Corruption affects difficulty (low = easier, high = harder). Items increase chances but never guarantee capture.

**Key Points:**
- C key marks target for capture during combat
- Allies switch to Attack & Bind mode
- Post-battle QTE capture phase
- Items INCREASE chance, never GUARANTEE

---

## Phase 1: During Combat (Mark & Bind)

### Marking Target

| Action | Result |
|--------|--------|
| Target enemy | Enemy highlighted |
| Press C | Capture Tag applied |
| Visual | Capture icon appears over enemy |
| Ally AI | Switches from Kill → Bind mode |

### Attack & Bind Mode

When enemy is marked for capture:
- Allies continue attacking normally
- At Bind Threshold HP%, allies use **Bind** ability
- Bound enemy cannot act but is not killed
- Combat continues against other enemies
- Battle ends when all enemies dead or bound

### Generic Bind Ability

All allied monsters have access to a generic Bind ability:
- Only usable when enemy reaches Bind Threshold
- Applies "Bound" status (cannot act, cannot die)
- Ally then moves to next target
- Multiple enemies can be bound simultaneously

---

## Bind Threshold System

### Base Threshold
Default: 25% HP

Monster becomes bindable when HP drops to threshold.

### Threshold Modifiers

| Factor | Effect | Reasoning |
|--------|--------|-----------|
| **LOW Corruption** | Higher threshold (+10-15%) | Stable, cooperative |
| **HIGH Corruption** | Lower threshold (-10-15%) | Chaotic, resists binding |
| **Higher Rarity** | Lower threshold (-5-15%) | Stronger will |
| **High Speed Stat** | Lower threshold (-5-10%) | Harder to pin down |
| **Brand WEAK to attacker** | Higher threshold (+5-10%) | Easier to overpower |
| **Brand STRONG vs attacker** | Lower threshold (-5-10%) | Resists binding |

### Intimidation Mechanic

If ally monster has significant advantage:
- Much higher level (5+ levels)
- Much higher rarity
- Strong brand advantage

**Effect:** Applies "Intimidated" debuff to enemy
- +10-15% to bind threshold
- Enemy binds sooner (at higher HP%)
- Visual: Enemy cowers/shakes

### Example Calculations

**Easy Bind (Low Corruption Common):**
```
Base: 25%
Low Corruption: +15%
Common Rarity: +0%
Intimidated: +10%
= 50% HP threshold (binds at half health)
```

**Hard Bind (High Corruption Legendary):**
```
Base: 25%
High Corruption: -15%
Legendary Rarity: -15%
High Speed: -10%
= -15% → Minimum 5% HP threshold
```

---

## Phase 2: Post-Battle Capture

### Capture Phase Trigger
- All enemies defeated or bound
- At least one enemy is bound (marked with C)
- Capture phase UI appears

### Capture Attempt Flow

1. **Select Bound Monster** (if multiple)
2. **Select Capture Item** (Shard/Crystal/Core/Heart)
3. **QTE Begins** (Quick Time Event)
4. **Calculate Final Chance**
5. **Success or Failure**

---

## Capture Formula

```
Capture Chance = Base(50%)
                 + HP Modifier
                 + Corruption Modifier
                 + Rarity Modifier
                 + Level Modifier
                 + Item Modifier
                 + QTE Bonus
```

### HP Modifier
Lower HP at bind = easier capture

| HP at Bind | Modifier |
|------------|----------|
| 5-10% | +15% |
| 11-20% | +10% |
| 21-30% | +5% |
| 31-40% | +0% |
| 41-50% | -5% |

### Corruption Modifier

| Corruption Level | Status | Modifier |
|------------------|--------|----------|
| 0-10% | ASCENDED | +20% (cooperative) |
| 11-25% | Purified | +10% |
| 26-50% | Unstable | +0% |
| 51-75% | Corrupted | -10% |
| 76-100% | Abyssal | -20% (chaotic) |

### Rarity Modifier

| Rarity | Modifier |
|--------|----------|
| Common | 0% |
| Uncommon | -10% |
| Rare | -20% |
| Epic | -35% to -40% |
| Legendary | -75% |

### Level Difference Modifier

| Player vs Monster | Modifier |
|-------------------|----------|
| Per level BELOW player | +3% (max +15%) |
| Equal level | +0% |
| Per level ABOVE player | -5% (max -25%) |

### Item Modifier by Monster Rarity

Items are EXPENSIVE - must be highly effective on low rarity to justify cost.

**Common / Uncommon Monsters:**

| Item | Capture Chance |
|------|----------------|
| Veil Shard | ~60-70% |
| Veil Crystal | ~80-85% |
| Veil Core | ~75-85% |
| Veil Heart | ~99% (near guaranteed) |

**Rare Monsters:**

| Item | Capture Chance |
|------|----------------|
| Veil Shard | ~40-50% |
| Veil Crystal | ~55-65% |
| Veil Core | ~65-75% |
| Veil Heart | ~90% |

**Epic Monsters:**

| Item | Capture Chance |
|------|----------------|
| Veil Shard | ~15-25% |
| Veil Crystal | ~30-40% |
| Veil Core | ~50-60% |
| Veil Heart | ~75-80% |

**Legendary Monsters:**

| Item | Capture Chance |
|------|----------------|
| Veil Shard | 0% (ineffective) |
| Veil Crystal | 0% (ineffective) |
| Veil Core | ~10-15% |
| Veil Heart | ~20-25% |

**Key Points:**
- Low rarity + high tier item = near guaranteed (Heart on Common = 99%)
- Legendary monsters REQUIRE Core or Heart - all others are 0% effective
- Items are expensive, so they must deliver results on lower rarities

### QTE Bonus

| QTE Result | Bonus |
|------------|-------|
| Perfect | +15% |
| Good | +10% |
| Okay | +5% |
| Miss | +0% |

---

## Capture Outcomes

### Success
- Monster joins player's party
- Starts at current corruption level
- Full stats/abilities retained
- Added to monster inventory

### Failure

Both outcomes possible for ANY corruption level, but weighted:

| Corruption | Flee Chance | Berserk Chance |
|------------|-------------|----------------|
| LOW (0-25%) | 70% | 30% |
| MID (26-50%) | 50% | 50% |
| HIGH (51-100%) | 30% | 70% |

**High Rarity/Level Override:**
- Epic/Legendary monsters: +20% Berserk chance
- 5+ levels above player: +10% Berserk chance
- Stacks with corruption modifier

### Flee Outcome
- Monster escapes battle
- May be encountered again later
- No penalty to player

### Berserk Outcome
- Monster breaks free of bind
- Gains +30-50% damage buff
- Combat resumes
- Must defeat or re-bind
- Can attempt capture again if re-bound

---

## Example Capture Scenarios

### Easy Capture: Low Corruption Common + Veil Heart

```
Item Base (Heart on Common): 99%
HP Bonus (15%):              +0% (already near max)
Corruption (10% ASCENDED):   +0% (already favorable)
QTE (Good):                  +0% (capped)
─────────────────────────────
TOTAL:                       99%
```
Near-guaranteed success with expensive item.

### Medium Capture: Mid Corruption Rare + Veil Crystal

```
Item Base (Crystal on Rare): 60%
HP Bonus (20%):              +5%
Corruption (40% Unstable):   +0%
Level (-1 below):            +3%
QTE (Good):                  +5%
─────────────────────────────
TOTAL:                       73%
```
Good chance with mid-tier item.

### Hard Capture: High Corruption Legendary + Veil Heart

```
Item Base (Heart on Legendary): 25%
HP Bonus (10%):                 +5%
Corruption (80% Abyssal):       -10%
Level (+3 above):               -15%
QTE (Perfect):                  +10%
─────────────────────────────────
TOTAL:                          15%
```
Even with best items, very low chance on Legendary.

---

## UI Elements

### During Combat

| Element | Description |
|---------|-------------|
| Capture Tag Icon | Appears over marked enemy |
| Bind Progress | Shows when enemy approaching threshold |
| "BOUND" Status | Clear indicator when bound |
| C Key Prompt | Shows in Capture Banner when enemy bindable |

### Post-Battle Capture Phase

| Element | Description |
|---------|-------------|
| Monster Display | Shows bound monster stats |
| Item Selection | Grid of available capture items |
| Chance Preview | Shows calculated capture chance |
| QTE Interface | Timing-based minigame |
| Result Screen | Success/Failure with outcome |

---

## Implementation Priority

### Phase 1: Core Binding
1. C key marking system
2. Ally AI "Bind Mode" switch
3. Bind threshold calculation
4. Bound status effect

### Phase 2: Capture Phase
5. Post-battle capture UI
6. Item selection
7. Capture formula calculation
8. Success/Failure outcomes

### Phase 3: QTE System
9. QTE minigame design
10. QTE bonus calculation
11. Visual feedback

### Phase 4: Polish
12. Intimidation debuff
13. Berserk combat resume
14. Visual effects (binding particles, etc.)
15. Sound effects

---

## Notes

- Items INCREASE chance, never GUARANTEE - even Veil Heart on Common isn't 100%
- Low corruption = EASIER to bind AND capture (cooperative)
- High corruption = HARDER to bind AND capture (chaotic, may berserk)
- Legendary monsters require top-tier items - Shard/Crystal useless
- Both Flee and Berserk possible at any corruption, just weighted differently
- High rarity/level monsters more likely to Berserk regardless of corruption

