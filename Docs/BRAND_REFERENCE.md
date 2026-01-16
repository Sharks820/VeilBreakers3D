# VEILBREAKERS - Brand Reference (v5.0)

> Quick reference for creating monsters with the 12-brand system.

---

## Brand Tiers Overview

| Tier | Count | Max Level | Evolution Stages | Bonus Power |
|------|-------|-----------|------------------|-------------|
| **PURE** | 6 brands | 100 | 3 (Birth → Evo2 → Evo3) | 100% (120% at Evo 3) |
| **HYBRID** | 6 brands | 100 | 3 (Birth → Evo2 → Evo3) | 70% Primary + 30% Secondary |
| **PRIMAL** | Any 2 brands | 120 | 2 (Birth → Evolved) | 50% + 50% (assigned at evo) |

---

## Pure Brands (6)

| Brand | Enum Value | Primary Stat Bonus | Theme | Best For |
|-------|------------|-------------------|-------|----------|
| **SAVAGE** | `Enums.Brand.SAVAGE` | +25% ATK | Raw destruction | Pure damage dealers |
| **IRON** | `Enums.Brand.IRON` | +30% HP | Unyielding defense | Tanks, bruisers |
| **VENOM** | `Enums.Brand.VENOM` | +20% Crit, +15% Status | Precision poison | Assassins, debuffers |
| **SURGE** | `Enums.Brand.SURGE` | +25% SPD | Lightning speed | Speed demons, first strikers |
| **DREAD** | `Enums.Brand.DREAD` | +20% Evasion, +15% Fear | Terror incarnate | Evasion tanks, disruptors |
| **LEECH** | `Enums.Brand.LEECH` | +20% Lifesteal | Life drain | Sustain fighters |

---

## Hybrid Brands (6)

| Hybrid | Enum Value | Primary (70%) | Secondary (30%) | Effective Bonus |
|--------|------------|---------------|-----------------|-----------------|
| **BLOODIRON** | `Enums.Brand.BLOODIRON` | SAVAGE | IRON | +17.5% ATK, +9% HP |
| **CORROSIVE** | `Enums.Brand.CORROSIVE` | IRON | VENOM | +21% HP, +6% Crit, +4.5% Status |
| **VENOMSTRIKE** | `Enums.Brand.VENOMSTRIKE` | VENOM | SURGE | +14% Crit, +10.5% Status, +7.5% SPD |
| **TERRORFLUX** | `Enums.Brand.TERRORFLUX` | SURGE | DREAD | +17.5% SPD, +6% Eva, +4.5% Fear |
| **NIGHTLEECH** | `Enums.Brand.NIGHTLEECH` | DREAD | LEECH | +14% Eva, +10.5% Fear, +6% Lifesteal |
| **RAVENOUS** | `Enums.Brand.RAVENOUS` | LEECH | SAVAGE | +14% Lifesteal, +7.5% ATK |

---

## Brand Effectiveness Wheel

```
SAVAGE ──► IRON ──► VENOM ──► SURGE ──► DREAD ──► LEECH ──┐
   ▲                                                       │
   └───────────────────────────────────────────────────────┘
```

### Damage Multipliers

| Attacker | Strong Against (1.5x) | Weak Against (0.67x) |
|----------|----------------------|---------------------|
| SAVAGE | IRON | LEECH |
| IRON | VENOM | SAVAGE |
| VENOM | SURGE | IRON |
| SURGE | DREAD | VENOM |
| DREAD | LEECH | SURGE |
| LEECH | SAVAGE | DREAD |

**Hybrid Brands:** Use PRIMARY brand for effectiveness calculations.

---

## Hero Path Interactions

| Hero Path | Strong vs Brand (1.35x) | Weak vs Brand (0.70x) |
|-----------|------------------------|----------------------|
| IRONBOUND | SAVAGE | VENOM |
| FANGBORN | LEECH | IRON |
| VOIDTOUCHED | SURGE | DREAD |
| UNCHAINED | DREAD | LEECH |

---

## Monster Data Template

```gdscript
# For MonsterData.tres files:

@export_group("Brand System (v5.0)")
@export var brand_tier: Enums.MonsterBrandTier = Enums.MonsterBrandTier.PURE
@export var brand: Enums.Brand = Enums.Brand.SAVAGE  # Primary brand
@export var secondary_brand: Enums.Brand = Enums.Brand.NONE  # Only for PRIMAL
@export var evolution_stage: Enums.EvolutionStage = Enums.EvolutionStage.BIRTH
```

### Example: Pure Brand Monster
```gdscript
brand_tier = Enums.MonsterBrandTier.PURE
brand = Enums.Brand.SAVAGE
secondary_brand = Enums.Brand.NONE  # Ignored for Pure
```

### Example: Hybrid Brand Monster
```gdscript
brand_tier = Enums.MonsterBrandTier.HYBRID
brand = Enums.Brand.BLOODIRON  # Primary is SAVAGE, Secondary is IRON
secondary_brand = Enums.Brand.NONE  # Hybrid already has built-in secondary
```

### Example: PRIMAL Monster
```gdscript
brand_tier = Enums.MonsterBrandTier.PRIMAL
brand = Enums.Brand.NONE  # Assigned at evolution
secondary_brand = Enums.Brand.NONE  # Assigned at evolution
# Both brands assigned based on monster behavior during Birth stage
```

---

## Recommended Brand by Monster Role

| Role | Best Pure | Best Hybrid | Reasoning |
|------|-----------|-------------|-----------|
| **Striker (DPS)** | SAVAGE | BLOODIRON, RAVENOUS | ATK bonus + sustain |
| **Tank** | IRON | BLOODIRON, CORROSIVE | HP bonus + utility |
| **Assassin** | VENOM | VENOMSTRIKE | Crit + status procs |
| **Speedster** | SURGE | TERRORFLUX, VENOMSTRIKE | SPD priority |
| **Evasion Tank** | DREAD | NIGHTLEECH, TERRORFLUX | Evasion + sustain |
| **Sustain DPS** | LEECH | RAVENOUS, NIGHTLEECH | Lifesteal focus |

---

## Constants Reference

```gdscript
# From constants.gd
const BRAND_STRONG: float = 1.5      # Strong vs weak: 150% damage
const BRAND_WEAK: float = 0.67       # Weak vs strong: 67% damage
const BRAND_NEUTRAL: float = 1.0     # Neutral matchup

const PATH_STRONG_MULTIPLIER: float = 1.35  # Heroes vs weak brands
const PATH_WEAK_MULTIPLIER: float = 0.70    # Heroes vs strong brands

const EVO3_BRAND_BONUS_MULTIPLIER: float = 1.20  # 120% at Evo 3
const PRIMAL_BRAND_SCALE: float = 0.50  # 50% each for PRIMAL dual brands
```

---

## Quick Design Checklist

When creating a new monster:

1. **Choose Tier**: PURE (specialist), HYBRID (versatile), or PRIMAL (endgame)
2. **Select Brand(s)**: Match brand to monster's visual/thematic design
3. **Consider Weaknesses**: Check effectiveness wheel for counters
4. **Set Base Stats**: Higher in brand's focus area (ATK for SAVAGE, HP for IRON, etc.)
5. **Create Skills**: Skills should synergize with brand bonus

---

*v5.0 - Brand System LOCKED*
