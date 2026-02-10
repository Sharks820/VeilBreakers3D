# TASK: Add IDLE Animations to Vex (Character Select Menu)

## Status: PENDING (animations to be created by user)

## Goal
Make Vex animated on the character select menu screen with intelligent idle animation cycling.

## Requirements

### Idle Animations
1. **Primary Idle** - Standard breathing/weight-shift idle loop
2. **Sweat Wipe Idle** - Vex wipes sweat from brow (already have `Wiping Sweat.fbx`)
3. **Idle Look Variations** - Subtle head/eye movement variations (several exist in MeleeAxe pack)

### Intelligent Playback
- Animations should cycle automatically over time without user interaction
- Use the existing `_animIdleTimer` / `IdleTimer` system in `HeroStageController`
- Primary idle plays as the default loop
- After a random interval (8-15 seconds), blend into a secondary idle (sweat wipe, look around)
- Secondary idles play once, then return to primary idle
- Transitions should be smooth crossfades (0.2-0.3s blend time)

## Available Animation Assets

### Already Imported (Art/Animations/Characters/Vex/)
- `ActionAdventure/idle.fbx` through `idle (5).fbx` - Various idle variants
- `Standalone/Vex_for_mixamo@Wiping Sweat.fbx` - Sweat wipe animation
- `MeleeAxe/standing idle.fbx` - Standing idle
- `MeleeAxe/standing idle looking ver. 1.fbx` and `ver. 2.fbx` - Idle look variations
- `MeleeAxe/unarmed idle.fbx` and looking variants

### Animator Controller
- `Resources/Art/Animations/Controllers/VexAnimatorController.controller`
- Already has `Idle` state and `IdleTimer` float parameter
- `HeroStageController` increments `IdleTimer` and resets at 12s

## Implementation Plan

1. **Select best idle clips** from existing FBX imports
2. **Update VexAnimatorController**:
   - Set primary idle as default state
   - Add secondary idle states (sweat wipe, look around)
   - Add transitions with `IdleTimer`-based conditions
   - Use `HasExitTime` for natural blend points
3. **Update HeroStageController** (if needed):
   - Adjust idle timer range for better variety
   - Add randomization to prevent predictable pattern
4. **Test** on character select screen with Vex model

## Notes
- User will create/refine animations first, then implementation follows
- The existing `_animIdleTimer` infrastructure handles the timing
- No code changes needed until animations are ready for integration
