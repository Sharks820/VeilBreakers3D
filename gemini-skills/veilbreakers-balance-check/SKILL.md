---
name: veilbreakers-balance-check
description: Use when modifying game balance numbers (damage, capture rates, synergy values) in VeilBreakers3D. Validates changes against brand effectiveness, synergy tiers, and corruption states.
---

# VeilBreakers Balance Check

## Overview

Validate balance changes BEFORE they break the game. One wrong number can ruin months of work.

## The Balance Systems

### Brand Effectiveness (10 Brands)
- 2x damage to 2 brands (strong against)
- 0.5x damage to 2 brands (weak against)
- 1x damage to 6 brands (neutral)

**Verify:** Every brand has exactly 2 strong, 2 weak, 6 neutral.

### Synergy Tiers
| Tier | Match | Damage | Defense | Corruption |
|------|-------|--------|---------|------------|
| FULL | 3/3 | +8% | +8% | 0.5x |
| PARTIAL | 2/3 | +5% | +5% | 0.75x |
| NEUTRAL | 0-1/3 | +0% | +0% | 1.0x |
| ANTI | Weak brand | +0% | +0% | 1.5x |

**Verify:** Bonuses stack additively, not multiplicatively.

### Corruption States
| Range | State | Modifier |
|-------|-------|----------|
| 0-10% | ASCENDED | +25% |
| 11-25% | Purified | +10% |
| 26-50% | Unstable | +0% |
| 51-75% | Corrupted | -10% |
| 76-100% | Abyssal | -20% |

### Ability Cooldowns
| Slot | Cooldown Range |
|------|----------------|
| Basic | None |
| Defend | None |
| Skill 1 | 4-6 seconds |
| Skill 2 | 10-15 seconds |
| Skill 3 | 18-25 seconds |
| Ultimate | 45-90 seconds |

## The Check Process

1. **Document the Change**: Exact number, old value, new value, reasoning.
2. **Edge Case Analysis**:
   - **Best case**: Max synergy + ASCENDED + type advantage
   - **Worst case**: ANTI synergy + Abyssal + type disadvantage
   - **Average case**: NEUTRAL + Unstable + neutral type
3. **Check for Red Flags**:
   - One-shot potential.
   - Unkillable builds.
   - Changes > ±20% from current.

## Quick Formulas

**Damage dealt:** `base_damage * brand_multiplier * synergy_bonus * corruption_modifier`
**Capture chance:** `base_rate + health_modifier + corruption_modifier + method_bonus + item_bonus`
**Effective stats:** `base_stat * (1 + synergy_bonus) * corruption_modifier`