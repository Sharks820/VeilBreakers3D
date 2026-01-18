# VeilBreakers 3D - Status Effects System Design

**Version:** 1.0
**Date:** 2026-01-18
**Status:** APPROVED

---

## Overview

Medium-complexity status effect system with brand-based resistance, effect type categories, and comprehensive visual feedback. Balanced for tactical depth without overwhelming interactions.

**Core Principles:**
- No stacking same effect (different effects coexist freely)
- Brand effectiveness affects effect potency
- Fixed duration timers OR cleanse/potion removal
- Rich visual feedback (damage counters, animations, particles)

---

## Effect Categories

### Dual Category System

Every effect has TWO categories:

1. **Brand** (IRON, SAVAGE, VENOM, etc.)
   - Determines resistance calculation
   - Uses existing 2x/0.5x brand effectiveness
   - VENOM DoT resisted by brands strong vs VENOM

2. **Type** (Damage, Control, Buff, Debuff)
   - Determines mechanical behavior
   - Different rules per type

---

## Effect Data Structure

```csharp
public class StatusEffect
{
    public string Id;                    // "poison_venom_1"
    public string DisplayName;           // "Venom Poison"
    public Brand SourceBrand;            // Brand.VENOM
    public EffectType Type;              // EffectType.Damage
    public float BaseValue;              // Base potency
    public float BaseDuration;           // Base duration in seconds
    public DurationTier DurationTier;    // Short/Medium/Long/Extended
    public StatType[] ScalingStats;      // Which stats affect potency
    public Sprite Icon;                  // UI icon
    public GameObject ParticleEffect;    // Visual effect prefab
    public string AnimationTrigger;      // Animation to play
}
```

---

## The No-Stack Rule

**Core Rule:** One instance of each unique effect per target.

| Scenario | Result |
|----------|--------|
| Target has Poison, apply Poison | BLOCKED - AI won't even target |
| Target has Poison, apply Burn | ALLOWED - different effects |
| Target has Stun, apply Stun | BLOCKED - must wait for expiry |
| Target has Attack Up, apply Attack Up | BLOCKED - same buff |
| Target has Attack Up, apply Defense Up | ALLOWED - different buffs |

**AI Integration:**
- Gambits system checks existing effects before targeting
- VENOM's DoT tracking = knowing who DOESN'T have which DoT
- Prevents wasted abilities, forces target variety

---

## Effect Scaling Formula

```
Final Potency = Base × (1 + StatMod) × SkillRank × BrandEffectiveness

Where:
- Base = Effect's base value
- StatMod = Caster's relevant stat modifier (0.0 to 1.0+)
- SkillRank = 1.0 / 1.2 / 1.4 / 1.6 / 2.0 (ranks 1-5)
- BrandEffectiveness = 0.5x / 1.0x / 2.0x based on brand matchup
```

```
Final Duration = BaseDuration × (1 + PotencyStat × 0.1)
```

**Example:**
- Level 5 VENOM skill (rank 2.0)
- High potency stat (+0.5 modifier)
- Target is brand-weak to VENOM (2.0x)
- Base poison: 10 damage/tick

Final: 10 × 1.5 × 2.0 × 2.0 = **60 damage/tick** (devastating)

---

## Duration Tiers

| Tier | Duration | Typical Use |
|------|----------|-------------|
| Short | 3-5 seconds | Quick CCs, burst windows |
| Medium | 8-12 seconds | Standard buffs/debuffs |
| Long | 15-20 seconds | Powerful DoTs, major buffs |
| Extended | 30+ seconds | Ultimate effects, Doom |

**Removal Methods:**
1. Timer expires naturally
2. Allied cleanse ability removes it
3. Potion removes it

---

## Effect Limits

**Maximum Effects:** 8-10 per target (safety cap)

In practice, rarely reached because:
- Same effect can't stack
- Battles end before accumulating 10 unique effects
- Effects expire on timers

---

## Effect Type: Damage

### DoT Effects (Damage Over Time)

| Effect | Brand | Behavior | Visual |
|--------|-------|----------|--------|
| Poison | VENOM | Ticking damage | Green drips, damage numbers |
| Burn | RUIN | Ticking damage | Flames, orange numbers |
| Bleed | SAVAGE | Ticking damage | Red slashes, blood particles |

### Burst Effects

| Effect | Brand | Behavior | Visual |
|--------|-------|----------|--------|
| Marked | Multiple | Bonus damage on next hit | Glowing target reticle |
| Cursed | DREAD/VOID | Damage when acting | Dark pulse on action |

**Visual Requirements:**
- Floating damage numbers for active DoTs
- Animated effects on character model
- Clear particle systems

---

## Effect Type: Control (11 Types)

| Effect | Behavior | Duration | Visual |
|--------|----------|----------|--------|
| Stun | Can't act | Short | Stars/daze over head |
| Slow | Reduced speed | Medium | Sluggish animation, blue tint |
| Root | Can't move, can act | Medium | Vines/chains on feet |
| Silence | Can't use skills | Medium | Muted icon over head |
| Blind | Attacks miss | Medium | Dark shroud on eyes |
| Taunt | Forced to attack taunter | Short | Red line to taunter |
| Fear | Forced to flee | Short | Shaking, dark wisps |
| Charm | Fights for enemy | Short | Pink/heart aura |
| Confuse | Random targets | Medium | Spiral over head |
| Sleep | CC, breaks on damage | Medium | Zzz particles |
| Petrify | Long stun + damage resist | Long | Stone texture overlay |

**No Diminishing Returns on Same Effect** - Can't reapply until expired anyway.

---

## Effect Type: Buffs (20+)

### Stat Buffs
| Effect | Behavior |
|--------|----------|
| Attack Up | Increased attack damage |
| Defense Up | Reduced damage taken |
| Speed Up | Faster action/movement |
| Accuracy Up | Higher hit chance |
| Evasion Up | Higher dodge chance |
| Crit Rate Up | Higher critical chance |
| Crit Damage Up | Stronger critical hits |

### Defensive Buffs
| Effect | Behavior |
|--------|----------|
| Shield | Absorbs X damage before HP |
| Barrier | Caps damage per hit |
| Regen | Healing over time |
| Fortify | CC resistance |
| Thorns | Damages attackers |

### Offensive Buffs
| Effect | Behavior |
|--------|----------|
| Lifesteal | Heal on damage dealt |
| Empower | Next skill deals bonus damage |
| Focus | Increased accuracy + crit |
| Berserk | Damage up, AI uncontrollable |

### Utility Buffs
| Effect | Behavior |
|--------|----------|
| Haste | Faster cooldowns |
| Immunity | Blocks next debuff application |
| Stealth | Untargetable (breaks on action) |
| Reflect | Returns portion of damage |

### Emergency Buffs
| Effect | Behavior |
|--------|----------|
| Second Wind | Auto-revive at 1 HP once |
| Quicken | Instant cooldown reset |
| Undying | Survive next lethal hit at 1 HP |

---

## Effect Type: Debuffs (20+)

### Stat Downs
| Effect | Behavior |
|--------|----------|
| Attack Down | Reduced attack damage |
| Defense Down | Increased damage taken |
| Speed Down | Slower action/movement |
| Accuracy Down | Lower hit chance |
| Evasion Down | Lower dodge chance |
| Crit Rate Down | Lower critical chance |
| Crit Damage Down | Weaker critical hits |

### Vulnerabilities
| Effect | Behavior |
|--------|----------|
| Expose | Take +X% damage from all sources |
| Fragile | Increased crit damage taken |
| Armor Shred | Defense stat ignored |
| Brand Weakness | Takes 2x from specific brand |

### Restrictions
| Effect | Behavior |
|--------|----------|
| Exhausted | Can't receive buffs |
| Sealed | Can't use ultimate |
| Grounded | Can't use movement skills |

### Anti-Sustain
| Effect | Behavior |
|--------|----------|
| Heal Block | Cannot receive healing |
| Cursed | Healing reduced by X% |
| Decay | Stats drop over time |
| Wither | Regen effects deal damage instead |

### Death Sentences
| Effect | Behavior |
|--------|----------|
| Doom | Death when timer expires |
| Marked for Death | Execute threshold raised (die at 30% HP) |
| Condemned | Die if not cleansed before expiry |

---

## Brand-to-Effect Mapping

| Brand | Primary Effects | Secondary Effects |
|-------|----------------|-------------------|
| IRON | Taunt, Fortify (self) | Defense Up, Barrier |
| SAVAGE | Bleed, Berserk | Attack Up, Expose |
| SURGE | Slow, Accuracy Down | Speed Up, Haste |
| VENOM | Poison, Heal Block | Armor Shred, Decay |
| DREAD | Fear, Stun, Silence | Doom, Confuse |
| LEECH | Lifesteal, Wither | Regen (self), Drain effects |
| GRACE | Regen, Cleanse | All stat buffs, Immunity |
| MEND | Shield, Barrier | Defense Up, Fortify |
| RUIN | Burn, Expose | Armor Shred, Fragile |
| VOID | Charm, Confuse | Buff Steal, Silence |

---

## Cleanse System

### Cleanse Abilities (Remove Debuffs)

**Removal Count:** Based on skill rank
| Skill Rank | Debuffs Removed |
|------------|-----------------|
| Rank 1 | 1 debuff |
| Rank 2 | 1 debuff |
| Rank 3 | 2 debuffs |
| Rank 4 | 2 debuffs |
| Rank 5 | 3 debuffs |
| Ultimate | All debuffs |

**AI Triage Priority:**
1. Doom/Death timers (immediate death threat)
2. Control effects (can't act)
3. DoTs (ongoing damage)
4. Stat debuffs (lowest priority)

GRACE's Gambit AI automatically cleanses highest priority debuffs first.

### Dispel Abilities (Remove Buffs from Enemies)

**Standard Dispel:** Remove 1-3 buffs (same scaling as cleanse)

**VOID Buff Steal:** Instead of removing, VOID gains the buff
- Steals 1-3 buffs based on skill rank
- VOID now has the buff with remaining duration
- Creates unique counterplay (protect buffs from VOID)

---

## UI Display

### Effect Icons
- **Size:** 16-20px icons
- **Location:** Row under HP bar (per Combat UI design)
- **Timer:** Radial sweep countdown on each icon
- **Tooltip:** Hover shows effect name, remaining duration, potency

### Visual Priority
If more than 8 effects, show:
1. Death sentences (Doom, Condemned)
2. Control effects (Stun, Charm)
3. DoTs (Poison, Burn, Bleed)
4. Debuffs
5. Buffs (lowest priority for enemy display)

### On-Character Visuals
Every effect has visible feedback:
- DoTs: Particle effects + damage numbers
- CC: Animation changes + overhead indicators
- Buffs: Aura/glow effects
- Debuffs: Dark particles/visual distortion

---

## Implementation Priority

### Phase 1: Core Framework
1. StatusEffect data structure
2. StatusEffectManager (apply, tick, remove)
3. No-stack validation
4. Duration timer system

### Phase 2: Effect Types
5. Damage effects (DoTs)
6. Control effects (basic CC)
7. Stat buffs/debuffs

### Phase 3: Advanced Effects
8. Complex buffs (Shield, Lifesteal, etc.)
9. Death sentence effects
10. Cleanse/Dispel system

### Phase 4: Visuals
11. Effect icons + timers in UI
12. Particle effects per effect
13. Animation triggers
14. Damage number popups

### Phase 5: Integration
15. Gambits AI integration (no-stack targeting)
16. Brand effectiveness calculation
17. Skill rank scaling

---

## Notes

- **No stacking** - Core rule, AI enforces this
- **Brand matters** - Effectiveness makes brand choice tactical
- **Visual clarity** - Every effect must be visible
- **Cleanse triage** - AI prioritizes dangerous debuffs
- **VOID steals** - Unique identity for VOID brand
- **Balance through testing** - Adjust numbers as we play

