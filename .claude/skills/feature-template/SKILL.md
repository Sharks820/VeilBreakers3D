---
name: feature-template
description: Structured template for new feature requests. Use to give Claude+Gemini maximum context for implementation.
---

# Feature Request Template

Use this structure when requesting new features. Copy and fill in:

```markdown
## Feature: [Name]

### Description
[What the feature does in 2-3 sentences]

### Key Components
- **Scripts to Modify:** [List existing files]
- **Scripts to Create:** [List new files needed]
- **Assets Required:** [Art, audio, prefabs needed]

### Acceptance Criteria
1. [Specific, testable requirement]
2. [Another requirement]
3. [Measurable outcome]

### VeilBreakers Context
- **Brands Affected:** [If combat-related]
- **Systems Touched:** [Combat, UI, Save, etc.]
- **Balance Considerations:** [Any numbers that need checking]
```

## Example: Poison Status Effect

```markdown
## Feature: Poison Status Effect

### Description
Monsters with VENOM brand can inflict poison on targets. Poison deals damage over time and reduces healing effectiveness.

### Key Components
- **Scripts to Modify:** Combatant.cs, DamageCalculator.cs, StatusEffectSystem.cs
- **Scripts to Create:** PoisonEffect.cs (inherits from StatusEffect)
- **Assets Required:** Poison VFX particles, poison icon for UI

### Acceptance Criteria
1. VENOM abilities have 30% chance to apply poison
2. Poison deals 5% max HP per second for 5 seconds
3. Poisoned targets receive 50% reduced healing
4. Poison effect stacks duration, not damage
5. UI shows poison icon with remaining duration

### VeilBreakers Context
- **Brands Affected:** VENOM (applicator), MEND/GRACE (counters)
- **Systems Touched:** Combat, StatusEffects, UI
- **Balance Considerations:** Check VENOM vs MEND matchup isn't broken
```

## Workflow After Template

1. **Claude analyzes** existing code patterns using Serena
2. **Gemini validates** design doesn't break balance
3. **Claude implements** following project conventions
4. **Gemini reviews** for edge cases
5. **Claude commits** with proper message
