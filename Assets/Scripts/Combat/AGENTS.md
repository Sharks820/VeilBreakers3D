# VeilBreakers Combat System Rules

## Brand System (CRITICAL - Do Not Modify Without User Approval)
- 10 Brands: IRON, SAVAGE, SURGE, VENOM, DREAD, LEECH, GRACE, MEND, RUIN, VOID
- Each brand: 2x vs 2 brands, 0.5x vs 2 brands, 1x vs 6 brands
- Effectiveness is BIDIRECTIONAL: if IRON is 2x vs SURGE, SURGE must be 0.5x vs IRON
- Changing one brand relationship cascades across ALL brand matchups

## Corruption Tiers (HARD BOUNDARIES)
- 0-10%: ASCENDED (+25%) | 11-25%: Purified (+10%) | 26-50%: Unstable (0%)
- 51-75%: Corrupted (-10%) | 76-79%: Abyssal (-20%) | 80-100%: UNTAMED (uncontrollable)
- 80% UNTAMED is a hard game state boundary - never change without user approval

## Synergy Tiers
- FULL (3/3): +8% dmg/def, 0.5x corruption scaling
- PARTIAL (2/3): +5%
- NEUTRAL: +0%
- ANTI (any weak brand present): +0% bonus, 1.5x corruption multiplier per weak brand

## Party Slots
- 3 Active + 3 Backpack (hard constraint)
- Swap cooldown: 3-5s abilities, instant basic

## Damage Calculation
- Deterministic: HP% + Corruption% + Item Tier + QTE
- No RNG in capture formula

## Balance Verification
- Use `/veilbreakers-balance-check` before committing ANY brand/synergy/corruption changes
- Changing one brand relationship cascades across ALL matchups — always verify full matrix

## Code Style
- Namespace: VeilBreakers.Combat
- kConstant, _private, PascalProperty, OnEvent
