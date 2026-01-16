# VeilBreakers3D - Real-Time Tactical Combat System Design

**Version:** 1.0
**Date:** 2026-01-15
**Status:** Approved Design

---

## Overview

VeilBreakers3D features Dragon Age: Inquisition-style action-forward real-time combat with tactical depth. Players command a party of captured monsters while their Veilbreaker champion provides support and direction.

---

## Core Combat Philosophy

- **Action-Forward:** Combat flows in real-time with tactical pausing for bosses only
- **Monster Focus:** Monsters are the primary combatants; champion provides buffs/commands
- **Brand Mastery:** 10 distinct monster types with rock-paper-scissors effectiveness
- **Corruption Stakes:** Player choices affect monster loyalty and power

---

## The 10-Brand System

### Brand Definitions

| Brand | Role | Archetype | Primary Stat |
|-------|------|-----------|--------------|
| **IRON** | Tank | Defensive Wall | Defense |
| **SAVAGE** | Melee Burst | Berserker | Attack |
| **SURGE** | Ranged DPS | Artillery | Attack |
| **VENOM** | DoT/Debuff | Poison Master | Effect Power |
| **DREAD** | CC/Terror | Fear Mage | Control |
| **LEECH** | Drain Tank | Lifesteal Bruiser | Sustain |
| **GRACE** | Battle Healer | Combat Medic | Healing |
| **MEND** | Ward Healer | Shield Support | Healing |
| **RUIN** | AOE Devastator | Explosion Mage | AOE Damage |
| **VOID** | Chaos Mage | Reality Warper | Chaos/Random |

### Brand Effectiveness Matrix

Each brand deals **2x damage** to 2 brands, **0.5x damage** to 2 brands, and **1x damage** to 6 brands.

| Attacker | Strong Against (2x) | Weak Against (0.5x) |
|----------|---------------------|---------------------|
| IRON | SURGE, DREAD | SAVAGE, RUIN |
| SAVAGE | IRON, MEND | LEECH, GRACE |
| SURGE | VENOM, LEECH | IRON, VOID |
| VENOM | GRACE, MEND | SURGE, RUIN |
| DREAD | SAVAGE, GRACE | IRON, VOID |
| LEECH | SAVAGE, RUIN | SURGE, VENOM |
| GRACE | VOID, RUIN | SAVAGE, VENOM |
| MEND | VOID, LEECH | SAVAGE, VENOM |
| RUIN | IRON, VENOM | LEECH, GRACE |
| VOID | SURGE, DREAD | GRACE, MEND |

---

## Universal Monster Actions

All monsters have access to these actions regardless of Brand:

### Basic Attack
- Brand-flavored auto-attack
- No cooldown
- Damage scales with monster stats

### Defend Self
- 50% damage reduction
- Monster hunkers down
- No cooldown
- Cannot attack while defending

### Guard Ally
- Monster intercepts attacks targeting an ally
- 75% of damage redirected to guarding monster
- Requires positioning (must be near ally)
- No cooldown

### Guard Champion
- Monster fully shields the Veilbreaker
- 100% of damage redirected to monster
- Full damage taken (no reduction)
- Priority targeting for enemies

### Body Shield Priority
When multiple monsters attempt to guard:
1. Guard Champion takes priority
2. Then Guard Ally by proximity
3. Monsters cannot stack guards on same target

---

## 5-Slot Ability Structure

Every monster has exactly 5 ability slots:

| Slot | Type | Cooldown | Notes |
|------|------|----------|-------|
| 1 | Basic Attack | None | Brand-flavored, always available |
| 2 | Defend/Guard | None | Universal defensive action |
| 3 | Skill 1 | 3-5 seconds | Light ability |
| 4 | Skill 2 | 8-12 seconds | Medium ability |
| 5 | Ultimate | 30-60 seconds | Powerful signature move |

---

## Party Structure

### Active Party
- **3 Active Monsters:** In combat, controllable
- **3 Backpack Monsters:** Swappable during combat
- **Unlimited Storage:** At base/between missions

### Swap Mechanics
- Swap cooldown: **3-5 seconds** for abilities
- Basic attacks and defense available immediately on swap
- Swapped-out monster retains cooldown progress

---

## Command Hierarchy

### 5 Layers of Control

1. **Behavior Presets** (AI Modes)
   - Aggressive / Defensive / Balanced / Support
   - Set before or during combat

2. **Quick Command Wheel** (Hold Button)
   - Fast access to common commands
   - Target selection

3. **Hotkey Commands** (Direct Input)
   - Bound abilities for instant activation
   - Power users

4. **Ping System** (Contextual)
   - Mark targets for focus
   - Mark positions for movement

5. **Tactical Pause** (Boss Fights Only)
   - Full pause for strategic planning
   - Reserved for major encounters

---

## Veilbreaker Paths

### The Four Paths

| Path | Philosophy | Playstyle |
|------|------------|-----------|
| **IRONBOUND** | Protection & Endurance | Tank-focused, defensive |
| **FANGBORN** | Aggression & Dominance | Damage-focused, aggressive |
| **VOIDTOUCHED** | Control & Manipulation | CC-focused, tactical |
| **UNCHAINED** | Freedom & Adaptability | Flex, no restrictions |

---

## Path/Brand Synergy System

### Core Philosophy
**Synergy = BUFF. Non-synergy = NEUTRAL.** No stat penalties for mismatched teams.

### Synergy Tiers

| Tier | Requirement | Effect |
|------|-------------|--------|
| **Strong Synergy** | Brand aligns with Path | +10% damage, +5% defense, slower corruption |
| **Neutral Synergy** | Brand doesn't align or oppose | No bonus, no penalty, normal corruption |
| **Weak Synergy** | Brand opposes Path | No bonus, faster corruption, no combo access |

### Path Alignments

| Path | Strong Synergy | Weak Synergy |
|------|---------------|--------------|
| IRONBOUND | IRON, MEND, LEECH | VOID, SAVAGE, RUIN |
| FANGBORN | SAVAGE, VENOM, RUIN | GRACE, MEND, IRON |
| VOIDTOUCHED | VOID, DREAD, SURGE | IRON, GRACE, MEND |
| UNCHAINED | All Neutral | None |

### Combo Abilities

**Requirement:** ALL 3 active party monsters must share Strong Synergy with Champion's Path.

| Path | Combo | Effect | Cooldown |
|------|-------|--------|----------|
| IRONBOUND | Bulwark Formation | +15% party defense, 8 sec | 45s |
| FANGBORN | Blood Frenzy | +12% party damage, 6 sec | 45s |
| VOIDTOUCHED | Reality Fracture | 20% chance reset ally cooldown | 60s |
| UNCHAINED | Adaptive Surge | Copy ally ability at 50% power | 60s |

### Corruption Rate Modifiers

| Synergy | Corruption Speed |
|---------|-----------------|
| Strong | 0.5x (slower) |
| Neutral | 1.0x (normal) |
| Weak | 1.5x (faster) |

---

## Corruption System

### Overview
Player choices corrupt MONSTERS, not the player. Corruption affects monster obedience and power.

### Corruption Sources
- Conversational choices: 1-3 points
- Quest decisions: 8-10 points
- Major story choices: 25+ points

### Corruption Thresholds

| Corruption % | Status | Effect |
|--------------|--------|--------|
| 0-10% | ASCENDED | +25% all stats |
| 11-25% | Purified | +10% all stats |
| 26-50% | Unstable | Normal stats |
| 51-75% | Corrupted | -10% all stats |
| 76-79% | Abyssal | -20% all stats |
| **80-100%** | **UNTAMED** | Monster becomes uncontrollable |

### Untamed State (80%+)
- Monster no longer obeys commands
- May attack allies or flee
- Must be purified or released

---

## Capture System

### Post-Battle Capture Phase
Capturing occurs AFTER combat ends, not during.

### Capture Formula
```
Capture Chance = f(HP%, Corruption%, Item Tier) + QTE Bonus
```

### Capture Items

| Tier | Name | Base Chance Modifier |
|------|------|---------------------|
| Basic | Veil Shard | +0% |
| Strong | Veil Crystal | +15% |
| Master | Veil Core | +30% |
| Legendary | Veil Heart | +50% |

### Capture Failure Outcomes
- **Low Corruption Monster:** Escapes (flees the area)
- **High Corruption Monster:** Goes Berserk (+30-50% damage, must defeat again)

### QTE Bonus
Successful Quick Time Event adds +5-15% to capture chance.

---

## Implementation Notes

### Priority Order
1. Core combat loop (attacks, damage, death)
2. Brand effectiveness system
3. Universal actions (defend, guard)
4. 5-slot ability structure
5. Party swapping
6. Command hierarchy
7. Synergy system
8. Corruption mechanics
9. Capture system

### Testing Focus
- Brand matchup balance
- Cooldown timing
- Synergy bonus tuning
- Combo ability balance
- Corruption rate tuning

---

## Appendix: Quick Reference

### Effectiveness Memory Aid
```
IRON walls SURGE artillery, fears DREAD
SAVAGE breaks IRON walls, heals MEND
SURGE pierces VENOM clouds, drains LEECH
VENOM poisons GRACE healers, shields MEND
DREAD terrifies SAVAGE berserkers, calms GRACE
LEECH drains SAVAGE life, ruins RUIN
GRACE purifies VOID chaos, devastates RUIN
MEND wards VOID chaos, drains LEECH
RUIN explodes IRON walls, melts VENOM
VOID warps SURGE precision, fears DREAD
```

---

*Document generated from brainstorming session 2026-01-15*
