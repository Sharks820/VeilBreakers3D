# Hero & Character System Design

> **Status:** IN PROGRESS | **Version:** 1.1 | **Date:** 2026-01-19
> **v1.1:** Restored exciting ability values, balanced through gating (cooldowns/costs) not nerfing

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
├── Spent on: Hero abilities (cost 15-30 each)
└── Purpose: Prevents ability spam, forces engagement, rewards good play
```

### Balance Philosophy

**Balance through GATING, not NERFING:**
- Keep abilities powerful and exciting
- Gate with cooldowns (12-55s)
- Gate with Command Gauge costs (15-30)
- Gate with conditions (requires setup, positioning, synergy)

| Wrong | Right |
|-------|-------|
| Reduce 50% buff to 15% | Keep 50%, add longer cooldown |
| Remove cool effect | Keep effect, require setup |
| Make everything small | Make it BIG but earned |

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
| **Aegis Dome** | 25 CG | 25s | AoE bubble - allies take 70% reduced damage for 5s |
| **Rallying Presence** | Passive | - | Nearby monsters gain +15% damage when above 70% HP |
| **Guardian's Mark** | 20 CG | 18s | Mark monster - 50% damage redirects to you, monster heals 15% of damage dealt |
| **Fortress Stance** | 30 CG | 45s | Root yourself - allies gain +30% defense for 4s |

**Healing Mechanics:** 1 (Guardian's Mark)

### WARDEN - Retribution Commander

**Fantasy:** "Every attack on my team is punished tenfold"

| Ability | Cost | Cooldown | Effect |
|---------|------|----------|--------|
| **Thorned Bond** | 20 CG | 20s | Link to monster 8s - attackers take 40% reflected damage |
| **Vengeance Mark** | 15 CG | 15s | Mark enemy 10s - allies heal 12% of damage dealt to it |
| **Retribution Aura** | 25 CG | 25s | 8s - when ally is hit, your next attack deals +100% damage |
| **Sentinel's Judgment** | 30 CG | 35s | Counter-stance 5s - next enemy attacker is stunned 3s |

**Healing Mechanics:** 1 (Vengeance Mark)

---

## FANGBORN - The Pack Leaders

*Spectrum Position: Most Fighter-like (hunts alongside monsters)*

### REND - Pack Alpha

**Fantasy:** "We hunt together, we kill together"

| Ability | Cost | Cooldown | Effect |
|---------|------|----------|--------|
| **Blood Frenzy** | 25 CG | 30s | 8s buff - allies gain +5% damage per hit landed, max 50% |
| **Alpha Strike** | 20 CG | 18s | Dash + attack, all monsters immediately attack same target |
| **Predator's Mark** | 15 CG | 12s | Mark 10s - monsters deal +30% to target, kills reset ALL monster cooldowns |
| **Unleash the Pack** | 30 CG | 55s | Full ability reset for all monsters, +40% attack speed for 6s |

**Healing Mechanics:** 1 (Predator's Mark on kill heals team 15%)

### VEX - Kill Coordinator

**Fantasy:** "I set them up, my monsters knock them down"

| Ability | Cost | Cooldown | Effect |
|---------|------|----------|--------|
| **Expose Weakness** | 15 CG | 15s | Debuff 8s - target takes +50% damage from next 3 attacks |
| **Crippling Poison** | 20 CG | 20s | AoE 6s - enemies deal 35% less damage |
| **Execute Order** | 25 CG | 22s | Mark enemy below 30% HP - 3x damage, kill heals executing monster 25% |
| **Shadow Coordination** | Passive | - | Your attacks have 40% chance for a monster to also attack |

**Healing Mechanics:** 1 (Execute Order on kill)

---

## VOIDTOUCHED - The Manipulators

*Spectrum Position: Balanced Commander (drain/debuff enabler)*

### MARROW - Life Weaver (Primary Healer)

**Fantasy:** "Your pain fuels our survival"

| Ability | Cost | Cooldown | Effect |
|---------|------|----------|--------|
| **Siphon Link** | 15 CG | 15s | Link 10s - monster heals 30% of damage dealt to linked enemy |
| **Life Tide** | 25 CG | 30s | AoE 8s - monster attacks heal nearby allies for 15% of damage |
| **Sacrifice Pact** | 20 CG | 22s | Spend 20% of monster's HP, they gain +50% damage for 10s |
| **Communion** | 30 CG | 55s | 10s - all ally HP pools linked, damage is split evenly |

**Healing Mechanics:** 3 (Siphon Link, Life Tide, Communion)

### SHADE - Entropy Caster (Debuff + Heal Hybrid)

**Fantasy:** "Watch them wither while we thrive"

| Ability | Cost | Cooldown | Effect |
|---------|------|----------|--------|
| **Creeping Doom** | 20 CG | 18s | AoE DoT 10s - stacking damage +5% per tick (max 50%) |
| **Entropic Curse** | 25 CG | 25s | Debuff 8s - +25% damage taken, spreads on death, allies heal 8% on spread |
| **Void Embrace** | 20 CG | 22s | Buff monster 10s - immune to debuffs, attacks heal 12% dealt |
| **Decay Field** | 30 CG | 40s | Zone 8s - enemies lose 3% max HP/s, allies heal that amount |

**Healing Mechanics:** 2 (Entropic Curse spread, Void Embrace)

---

## UNCHAINED - The Wild Cards

*Spectrum Position: Utility Commander (enable through chaos/tricks)*

### MIRAGE - Master of Misdirection

**Fantasy:** "They can't hit what they can't find"

| Ability | Cost | Cooldown | Effect |
|---------|------|----------|--------|
| **Phantom Veil** | 20 CG | 20s | Create illusion 6s - enemies target it, real monster heals 20% |
| **Displacement** | 15 CG | 16s | Swap positions of any two units (ally or enemy) |
| **Mass Confusion** | 30 CG | 35s | AoE 6s - enemies have 50% chance to attack each other |
| **Hall of Mirrors** | 30 CG | 55s | 6s - all monsters gain a clone that copies attacks at 50% damage |

**Healing Mechanics:** 1 (Phantom Veil)

### FLUX - Chaos Catalyst

**Fantasy:** "Every fight is a roll of the dice - and I loaded them"

| Ability | Cost | Cooldown | Effect |
|---------|------|----------|--------|
| **Chaos Surge** | 20 CG | 18s | Random buff: +50% damage OR 40% heal OR 3s invulnerability OR full cooldown reset |
| **Reality Warp** | 25 CG | 28s | All cooldowns shift by -5s to +5s randomly (weighted toward allies) |
| **Probability Field** | 25 CG | 25s | Zone 6s - +30% crit chance, +25% dodge chance |
| **Jackpot** | 30 CG | 50s | Roll 3 random powerful buffs for team (1 always includes 30% heal) |

**Healing Mechanics:** 1 (Chaos Surge chance, Jackpot guaranteed)

---

## Balance Summary

| Mechanic | Value |
|----------|-------|
| Damage buffs | +15% to +100% (gated by cooldowns) |
| Damage reduction | 70% max |
| Duration | 5-10s |
| CG Costs | 15-30 |
| Cooldowns | 12-55s |
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
