# Hero & Character System Design

> **Status:** IN PROGRESS | **Version:** 1.0 | **Date:** 2026-01-19

---

## Overview

8 playable heroes (2 per Path), with 4 unlocked at start and 4 unlockable through gameplay. Players are **Commanders** who support and amplify their monster teams, not primary damage dealers.

---

## Core Design Philosophy

### Player Role: Commander, Not Carry

| Old Thinking | New Thinking |
|--------------|--------------|
| Player = Party member who fights | Player = Commander who enables |
| Heroes have DPS/Tank/Healer roles | Heroes have Command/Buff/Control styles |
| Monsters support the player | **Player supports the monsters** |
| Hero abilities deal damage | Hero abilities amplify, position, protect monsters |

### The Commander ↔ Fighter Spectrum

```
COMMANDER ←――――――――――――――――――――――――――――→ FIGHTER
    │                                        │
IRONBOUND ―― VOIDTOUCHED ―― UNCHAINED ―― FANGBORN
 (Protect)     (Manipulate)    (Enable)     (Hunt)
```

### Fun = Impact Formula

```
Fun = (Setup) + (Execution) + (Payoff)

Tank player → Shields team → Team survives nuke → Counter-attack
Illusionist → Confuses enemies → Monsters attack freely → Massacre

Every hero needs a clear "When I do X, my team does Y, and Z happens" loop.
```

---

## Command Gauge System

```
COMMAND GAUGE (0-100)
├── Built by: Light attacks (+8), Successful commands (+12), Monster kills (+20)
├── Spent on: Hero abilities (cost 10-25 each)
└── Purpose: Prevents ability spam, forces engagement, rewards good play
```

---

## Hero Roster

### Unlock Status

| Path | Starter (Unlocked) | Locked Hero |
|------|-------------------|-------------|
| IRONBOUND | Bastion | Warden |
| FANGBORN | Rend | Vex |
| VOIDTOUCHED | Marrow | Shade |
| UNCHAINED | Mirage | Flux |

---

## IRONBOUND - The Bulwarks

*Spectrum Position: Full Commander (enables through protection)*

### BASTION - Fortress Commander

**Fantasy:** "Behind my shield, my monsters are unstoppable"

| Ability | Cost | Cooldown | Effect |
|---------|------|----------|--------|
| **Aegis Dome** | 20 CG | 20s | AoE bubble - allies take 40% reduced damage for 4s |
| **Rallying Presence** | Passive | - | Nearby monsters gain +8% damage when above 70% HP |
| **Guardian's Mark** | 15 CG | 15s | Mark monster - 30% damage redirects to you, monster heals 10% of damage dealt |
| **Fortress Stance** | 25 CG | 40s | Root yourself - allies gain +15% defense for 3s |

**Healing Mechanics:** 1 (Guardian's Mark)

### WARDEN - Retribution Commander

**Fantasy:** "Every attack on my team is punished tenfold"

| Ability | Cost | Cooldown | Effect |
|---------|------|----------|--------|
| **Thorned Bond** | 15 CG | 18s | Link to monster 6s - attackers take 25% reflected damage |
| **Vengeance Mark** | 10 CG | 12s | Mark enemy 8s - allies heal 8% of damage dealt to it |
| **Retribution Aura** | 20 CG | 20s | 6s - when ally is hit, your next attack deals +50% damage |
| **Sentinel's Judgment** | 25 CG | 30s | Counter-stance 4s - next enemy attacker is stunned 2s |

**Healing Mechanics:** 1 (Vengeance Mark)

---

## FANGBORN - The Pack Leaders

*Spectrum Position: Most Fighter-like (hunts alongside monsters)*

### REND - Pack Alpha

**Fantasy:** "We hunt together, we kill together"

| Ability | Cost | Cooldown | Effect |
|---------|------|----------|--------|
| **Blood Frenzy** | 20 CG | 25s | 6s buff - allies gain +3% damage per hit landed, max 30% |
| **Alpha Strike** | 15 CG | 15s | Dash + attack, all monsters immediately attack same target |
| **Predator's Mark** | 10 CG | 10s | Mark 8s - monsters deal +20% to target, kills heal all monsters 10% HP |
| **Unleash the Pack** | 25 CG | 50s | Reset monster cooldowns, +25% attack speed for 5s |

**Healing Mechanics:** 1 (Predator's Mark on kill)

### VEX - Kill Coordinator

**Fantasy:** "I set them up, my monsters knock them down"

| Ability | Cost | Cooldown | Effect |
|---------|------|----------|--------|
| **Expose Weakness** | 10 CG | 12s | Debuff 6s - target takes +30% damage from next 3 attacks |
| **Crippling Poison** | 15 CG | 18s | AoE 5s - enemies deal 20% less damage |
| **Execute Order** | 20 CG | 18s | Mark enemy below 25% HP - guaranteed crit, kill heals monster 20% |
| **Shadow Coordination** | Passive | - | Your attacks have 30% chance for a monster to also attack |

**Healing Mechanics:** 1 (Execute Order on kill)

---

## VOIDTOUCHED - The Manipulators

*Spectrum Position: Balanced Commander (drain/debuff enabler)*

### MARROW - Life Weaver (Primary Healer)

**Fantasy:** "Your pain fuels our survival"

| Ability | Cost | Cooldown | Effect |
|---------|------|----------|--------|
| **Siphon Link** | 10 CG | 12s | Link 8s - monster heals 20% of damage dealt to linked enemy |
| **Life Tide** | 20 CG | 25s | AoE 6s - monster attacks heal nearby allies for 10% of damage |
| **Sacrifice Pact** | 15 CG | 18s | Spend 15% of monster's HP, they gain +30% damage for 8s |
| **Communion** | 25 CG | 50s | 8s - all ally HP pools linked, damage is split evenly |

**Healing Mechanics:** 3 (Siphon Link, Life Tide, Communion)

### SHADE - Entropy Caster (Debuff + Heal Hybrid)

**Fantasy:** "Watch them wither while we thrive"

| Ability | Cost | Cooldown | Effect |
|---------|------|----------|--------|
| **Creeping Doom** | 15 CG | 15s | AoE DoT 8s - stacking damage +3% per tick |
| **Entropic Curse** | 20 CG | 22s | Debuff 6s - +15% damage taken, spreads on death, allies heal 5% on spread |
| **Void Embrace** | 15 CG | 18s | Buff monster 8s - immune to debuffs, attacks heal 8% dealt |
| **Decay Field** | 25 CG | 35s | Zone 6s - enemies lose 2% max HP/s, allies heal that amount |

**Healing Mechanics:** 2 (Entropic Curse spread, Void Embrace)

---

## UNCHAINED - The Wild Cards

*Spectrum Position: Utility Commander (enable through chaos/tricks)*

### MIRAGE - Master of Misdirection

**Fantasy:** "They can't hit what they can't find"

| Ability | Cost | Cooldown | Effect |
|---------|------|----------|--------|
| **Phantom Veil** | 15 CG | 18s | Create illusion 4s - enemies target it, real monster heals 15% |
| **Displacement** | 10 CG | 14s | Swap positions of any two units (ally or enemy) |
| **Mass Confusion** | 25 CG | 30s | AoE 4s - enemies have 35% chance to attack each other |
| **Hall of Mirrors** | 25 CG | 50s | 5s - all monsters gain a clone that copies attacks at 25% damage |

**Healing Mechanics:** 1 (Phantom Veil)

### FLUX - Chaos Catalyst

**Fantasy:** "Every fight is a roll of the dice - and I loaded them"

| Ability | Cost | Cooldown | Effect |
|---------|------|----------|--------|
| **Chaos Surge** | 15 CG | 15s | Random buff: +30% damage OR 25% heal OR 4s shield OR cooldown reset |
| **Reality Warp** | 20 CG | 25s | All cooldowns shift by -3s to +3s randomly |
| **Probability Field** | 20 CG | 22s | Zone 5s - +20% crit chance, +15% dodge chance |
| **Jackpot** | 25 CG | 45s | Roll 2 random buffs for team (1 always includes heal) |

**Healing Mechanics:** 1 (Chaos Surge chance, Jackpot guaranteed)

---

## Balance Summary

| Mechanic | Value |
|----------|-------|
| Damage buffs | +8% to +30% |
| Damage reduction | 40% max |
| Duration | 3-8s |
| CG Costs | 10-25 |
| Cooldowns | 10-50s |
| Healing per non-healer | 1 mechanic |
| Healing for healers | 2-3 mechanics |

---

## Healing Distribution

| Hero | Heals | Type |
|------|-------|------|
| Bastion | 1 | Damage dealt → heal |
| Warden | 1 | Damage dealt → heal |
| Rend | 1 | Kill → team heal |
| Vex | 1 | Kill → self heal |
| **Marrow** | 3 | Primary healer |
| **Shade** | 2 | Debuff + heal hybrid |
| Mirage | 1 | Illusion window heal |
| Flux | 1 | Random/guaranteed |

---

## Next Steps

1. Start Menu UI design
2. Character Select screen design
3. Character Customization system (cosmetic)
4. Path selection flow
5. Unity scene integration
6. VERA system implementation
7. Vertical slice

---

*Design complete - 2026-01-19*
