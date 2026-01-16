# VEILBREAKERS - Evolution & Leveling Reference (v5.0)

> Quick reference for monster evolution, leveling speeds, and stat growth.

---

## Evolution Paths by Tier

### Pure/Hybrid Monsters (3 Stages)

```
BIRTH ──────► EVO_2 ──────► EVO_3
  │             │             │
Level 1-25   Level 26-50   Level 51-100
```

| Stage | Level Range | Evolution Trigger | XP Multiplier | Stat Growth |
|-------|-------------|-------------------|---------------|-------------|
| **Birth** | 1-25 | Starting stage | 1.0x (normal) | Normal |
| **Evo 2** | 26-50 | Reach level 26 | 1.5x (slower) | +15% per level |
| **Evo 3** | 51-100 | Reach level 51 | 2.5x (much slower) | +30% per level |

**Evo 3 Bonus for Pure Brands:** Brand bonus increases to 120% (from 100%)

---

### PRIMAL Monsters (2 Stages)

```
BIRTH ──────────► EVOLVED ──────────► OVERFLOW
  │                   │                   │
Level 1-35        Level 36-109       Level 110-120
```

| Stage | Level Range | Evolution Trigger | XP Multiplier | Stat Growth |
|-------|-------------|-------------------|---------------|-------------|
| **Birth** | 1-35 | Starting stage | 0.6x (very fast) | Normal |
| **Evolved** | 36-109 | Reach level 36 | 0.8x (fast) | +10% per level |
| **Overflow** | 110-120 | Reach level 110 | 1.0x (normal) | +5% ALL stats/level |

**PRIMAL Advantages:**
- Faster leveling throughout
- Max level 120 (vs 100 for others)
- No Path weakness (neutral to all paths)
- Both brands count as PRIMARY for skill scaling (50%+50%)
- Overflow stats Lv110-120: +50% all stats total over 10 levels

---

## Level Cap Comparison

| Tier | Max Level | Stages | Endgame Power |
|------|-----------|--------|---------------|
| PURE | 100 | 3 | MASSIVE stats, ultimate skills, specialist |
| HYBRID | 100 | 3 | Strong stats, versatile coverage |
| PRIMAL | 120 | 2 | Highest level, no weakness, flexible |

---

## XP Requirements

Base formula: `BASE_XP * (XP_GROWTH_RATE ^ (level - 1)) * XP_MULTIPLIER`

```gdscript
const BASE_XP_REQUIREMENT: int = 100
const XP_GROWTH_RATE: float = 1.15
```

### XP Multiplier by Tier & Stage

| Tier | Birth | Evo 2 | Evo 3 | Evolved | Overflow |
|------|-------|-------|-------|---------|----------|
| **PURE** | 1.0x | 1.5x | 2.5x | - | - |
| **HYBRID** | 1.0x | 1.5x | 2.5x | - | - |
| **PRIMAL** | 0.6x | - | - | 0.8x | 1.0x |

**Lower multiplier = faster leveling**

### Example XP to Level

| Level | Base XP | Pure Birth | Pure Evo3 | PRIMAL Birth | PRIMAL Evolved |
|-------|---------|------------|-----------|--------------|----------------|
| 2 | 100 | 100 | 250 | 60 | 80 |
| 10 | 352 | 352 | 880 | 211 | 282 |
| 26 | 2,876 | 2,876 | - | 1,726 | 2,301 |
| 51 | 86,736 | 86,736 | 216,840 | - | 69,389 |
| 100 | 14.8M | - | 37M | - | 11.8M |
| 110 | 59.9M | - | - | - | 59.9M |
| 120 | 242.6M | - | - | - | 242.6M |

---

## Stat Growth Multipliers

Applied per level based on evolution stage:

```gdscript
const STAT_GROWTH_NORMAL: float = 1.0        # Birth stage
const STAT_GROWTH_EVO2: float = 1.15         # +15% per level
const STAT_GROWTH_EVO3: float = 1.30         # +30% per level
const STAT_GROWTH_PRIMAL_EVOLVED: float = 1.10   # +10% per level
const STAT_GROWTH_PRIMAL_OVERFLOW: float = 1.05  # +5% ALL stats/level
```

### Stat Calculation

```gdscript
func get_stat_at_level(stat: String, level: int, evo_stage: EvolutionStage) -> int:
    var raw_stat := base_value * pow(growth_rate, level - 1)
    var evo_multiplier := _get_evolution_stat_multiplier(evo_stage, level)
    return int(raw_stat * evo_multiplier)
```

---

## Endgame Power Comparison (~150 Hours)

| Monster Type | Likely Level | Relative Power | Notes |
|--------------|--------------|----------------|-------|
| Pure Evo 3 | ~85 | 95% | Specialist, ultimate skills |
| Hybrid Evo 3 | ~88 | 90% | Versatile, good coverage |
| PRIMAL Evolved | ~115 | 100% | Highest stats, no weakness |

---

## Evolution Level Thresholds

```gdscript
# From constants.gd
const EVO_2_LEVEL: int = 26           # Pure/Hybrid → Evo 2
const EVO_3_LEVEL: int = 51           # Pure/Hybrid → Evo 3
const PRIMAL_EVO_LEVEL: int = 36      # PRIMAL → Evolved
const PRIMAL_OVERFLOW_LEVEL: int = 110  # PRIMAL overflow starts
```

---

## Monster Data Methods

### Check If Can Evolve
```gdscript
func can_evolve(current_level: int, current_stage: EvolutionStage) -> bool
```

### Get Next Evolution Stage
```gdscript
func get_next_evolution_stage(current_stage: EvolutionStage) -> EvolutionStage
```

### Get Max Level
```gdscript
func get_max_level() -> int
# Returns 120 for PRIMAL, 100 for others
```

### Get XP Multiplier
```gdscript
func get_xp_multiplier(evo_stage: EvolutionStage) -> float
# Returns stage-appropriate multiplier
```

---

## PRIMAL Brand Assignment

PRIMAL monsters start with `brand = NONE` and `secondary_brand = NONE`.

At evolution (level 36):
1. System analyzes monster's behavior during Birth stage
2. Two brands are assigned based on fighting style
3. Both brands count as PRIMARY (50% each = 100% total)
4. Effectiveness uses a random one of the two brands

**Design Tip:** When creating PRIMAL monsters, design their base skills to hint at which brands they might receive.

---

## Base Stat Recommendations

### By Tier

| Tier | HP | ATK | DEF | MAG | RES | SPD |
|------|-----|-----|-----|-----|-----|-----|
| PURE | 40-60 | 8-12 | 6-10 | 6-10 | 6-10 | 8-12 |
| HYBRID | 45-55 | 9-11 | 7-9 | 7-9 | 7-9 | 9-11 |
| PRIMAL | 35-50 | 7-10 | 5-8 | 5-8 | 5-8 | 7-10 |

**Note:** PRIMAL starts weaker but catches up via faster leveling.

### Growth Rates

| Speed | Value | Best For |
|-------|-------|----------|
| Fast | 1.20+ | Primary stat for brand |
| Normal | 1.10-1.15 | Secondary stats |
| Slow | 1.05-1.10 | Dump stats |

---

## Quick Design Checklist

When setting up evolution:

1. **Choose Tier**: PURE/HYBRID (3 stages) or PRIMAL (2 stages)
2. **Set Base Stats**: Lower for PRIMAL, higher for PURE
3. **Set Growth Rates**: Favor brand's primary stat
4. **Create Stage Skills**:
   - Birth: Basic abilities
   - Evo 2: Intermediate skills
   - Evo 3/Evolved: Ultimate skills

---

*v5.0 - Evolution System LOCKED*
