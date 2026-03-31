---
paths:
  - "Assets/Scripts/Combat/**/*.cs"
  - "Assets/Scripts/Data/**/*.cs"
  - "Assets/Scripts/Capture/**/*.cs"
---

# Brand & Synergy System Rules

## 10 Brands
IRON, SAVAGE, SURGE, VENOM, DREAD, LEECH, GRACE, MEND, RUIN, VOID

## Effectiveness Matrix Invariant
- Each brand: 2x to 2 brands, 0.5x to 2 brands, 1x to 6 brands
- **Bidirectional rule:** If A is 2x vs B, then B MUST be 0.5x vs A
- Any change to one entry requires the reciprocal change
- Use `/veilbreakers-balance-check` before committing brand changes

## Synergy Tiers
- FULL (3/3): +8% damage/defense + 0.5x corruption scaling (stability bonus)
- PARTIAL (2/3): +5% damage/defense
- NEUTRAL (1/3 or 0/3): +0%
- ANTI (any weak brand present): +0% bonus, 1.5x corruption multiplier per weak brand

## 4 Paths
- IRONBOUND, FANGBORN, VOIDTOUCHED, UNCHAINED
- UNCHAINED is the "flex" path (all Neutral, no weak synergies)
- Paths unlock specific brand combinations for synergy calculation

## Never Break
- DPS curves must stay within +/-5% of baseline after changes
- Test all 10 brands x 4 paths after any modification
- Verify `BrandSystem.cs` and `SynergySystem.cs` match VEILBREAKERS.md spec
