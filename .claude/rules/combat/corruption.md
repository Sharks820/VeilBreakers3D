---
paths:
  - "Assets/Scripts/Combat/**/*.cs"
  - "Assets/Scripts/Capture/**/*.cs"
  - "Assets/Scripts/Data/**/*.cs"
---

# Corruption System Rules

## Tiers (0-100%, per-character, persists across battles)
| Range | Tier | Effect |
|-------|------|--------|
| 0-10% | ASCENDED | +25% all stats |
| 11-25% | Purified | +10% all stats |
| 26-50% | Unstable | +0% (neutral) |
| 51-75% | Corrupted | -10% all stats |
| 76-79% | Abyssal | -20% all stats |
| **80-100%** | **UNTAMED** | **Monster uncontrollable** |

## Critical Rules
- 80% is a HARD game state boundary (not just a stat modifier — changes control)
- Values clamp to [0, 100] via `Mathf.Clamp`
- GRACE brand reduces corruption (scales with synergy: 2x synergy = 2x reduction)
- Tier transitions are exact at boundaries (test 10->11, 79->80 specifically)
- Capture formula uses corruption: `f(HP%, Corruption%, Item Tier) + QTE Bonus`

## Testing Checklist
- Each tier boundary produces correct modifier
- Values never exceed [0, 100]
- 80% threshold triggers UNTAMED state change
- GRACE reduction respects synergy multiplier
