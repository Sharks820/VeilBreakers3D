# Character Select Screen Revolution - Design Document

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Transform the character select screen into an AAA-quality, animated experience that showcases hero + starter monster pairings with Path-themed environments.

**Architecture:** Dynamic backdrop system with dual 3D model preview, particle effects, and smooth transitions.

**Tech Stack:** Unity UI Toolkit, URP, VFX Graph (future), C# animation controllers

---

## Visual Layout

```
┌─────────────────────────────────────────────────────────────────┐
│                    ENVIRONMENTAL BACKDROP                        │
│            (Path biome - Ironmaw, Ravaged, etc.)                │
│                                                                  │
│  ┌──────┐                                                       │
│  │ HERO │     ╔════════════════════════════════╗               │
│  │ CARD │     ║                                ║  ┌───────────┐│
│  │  1   │     ║   3D HERO        3D STARTER   ║  │ HERO NAME ││
│  └──────┘     ║   MODEL          MONSTER      ║  │ Path Badge││
│  ┌──────┐     ║                               ║  ├───────────┤│
│  │ HERO │     ║   [Bastion]  +   [Ironjaw]   ║  │STAT BARS  ││
│  │ CARD │     ║                               ║  │(colored)  ││
│  │  2   │     ║    Hero Particles + Monster   ║  ├───────────┤│
│  └──────┘     ╚════════════════════════════════╝  │ STARTER   ││
│  ┌──────┐                                         │ MONSTER   ││
│  │ HERO │            [CONFIRM]                    │ INFO      ││
│  │ CARD │                                         │ -Name     ││
│  │  3   │                                         │ -Brand    ││
│  └──────┘                                         │ -Preview  ││
│   ...                                             └───────────┘│
└─────────────────────────────────────────────────────────────────┘
```

---

## Path-Aligned Starter Pairings

| Path | Hero | Starter Monster | Brand | Backdrop Theme |
|------|------|-----------------|-------|----------------|
| **IRONBOUND** | Bastion | Ironjaw | IRON | Rusted fortress, amber light |
| **FANGBORN** | Rend | Mawling | SAVAGE | Primal grounds, toxic fog |
| **VOIDTOUCHED** | Marrow | Hollow | VOID | Reality tears, nightmare |
| **UNCHAINED** | Mirage | Flicker | SURGE | Unstable crossing, electric |

---

## Animation Flow

### Hero Selection Transition (Total: ~1.2s)

```
User clicks hero card
    │
    ├─► Hero card PULSES with Path color (0.15s)
    │
    ├─► Backdrop CROSSFADES to new biome (0.4s)
    │
    ├─► Current models FADE OUT + slide back (0.3s)
    │
    ├─► New Hero model EMERGES from veil energy (0.5s)
    │   - Particles coalesce into form
    │   - Red veil wisps dissipate
    │
    ├─► Starter Monster MATERIALIZES beside hero (0.4s delay)
    │   - Brand-colored particles swirl in
    │   - Idle animation begins
    │
    └─► Stats/details SLIDE IN from right (0.3s)
        - Bars animate to values
        - Monster info fades in below
```

### Idle Animations (Continuous)

- **Hero:** Breathing, subtle weight shift, occasional glance at monster
- **Monster:** Brand-specific idle
  - IRON: Stoic, grounded
  - SAVAGE: Prowling, hungry
  - VOID: Flickering, unstable
  - SURGE: Crackling, energetic
- **Particles:** Gentle float around both characters
- **Backdrop:** Subtle parallax, atmospheric effects (fog, embers, rifts)

---

## Stat Bar Colors (Bug Fix)

**Remove `ApplyHeroColorToStatBars()` - use these fixed colors:**

| Stat | Color Name | Hex | RGB |
|------|------------|-----|-----|
| Health | Red | `#DC6464` | `rgb(220, 100, 100)` |
| MP | Cyan | `#64B4DC` | `rgb(100, 180, 220)` |
| Attack | Orange | `#E6A064` | `rgb(230, 160, 100)` |
| Defense | Steel Blue | `#82AAC8` | `rgb(130, 170, 200)` |
| Speed | Green | `#78C88C` | `rgb(120, 200, 140)` |

---

## Path Visual Themes

### IRONBOUND (Bastion + Ironjaw)
- **Backdrop:** Rusted fortress walls, crumbling battlements
- **Lighting:** Dim amber, torch flicker
- **Particles:** Metallic bronze sparks, dust motes
- **Mood:** Stalwart, defensive, enduring
- **Colors:** Bronze, rust, steel gray

### FANGBORN (Rend + Mawling)
- **Backdrop:** Primal hunting ground, bone totems
- **Lighting:** Blood red sunset, toxic green pools
- **Particles:** Crimson blood mist, green venom wisps
- **Mood:** Savage, hungry, relentless
- **Colors:** Crimson, toxic green, bone white

### VOIDTOUCHED (Marrow + Hollow)
- **Backdrop:** Reality tears, impossible geometry
- **Lighting:** Deep purple, red veil glow
- **Particles:** Purple void rifts, red veil energy streams
- **Mood:** Unsettling, chaotic, powerful
- **Colors:** Deep purple, crimson, black

### UNCHAINED (Mirage + Flicker)
- **Backdrop:** Unstable crossing, shifting terrain
- **Lighting:** Electric blue, neutral white
- **Particles:** Electric arcs, prismatic shimmer
- **Mood:** Unpredictable, free, adaptive
- **Colors:** Electric blue, silver, shifting hues

---

## Implementation Tasks

### Task 1: Fix Stat Bar Colors (Quick Win)
**Files:**
- Modify: `Assets/Scripts/UI/Menus/CharacterSelectController.cs`

**Steps:**
1. Remove or comment out `ApplyHeroColorToStatBars()` call at line 794
2. Remove the method itself (lines 797-807)
3. Verify UXML colors are applied correctly
4. Test all heroes to confirm individual stat colors work

---

### Task 2: Add Starter Monster Data
**Files:**
- Modify: `Assets/Scripts/Data/HeroData.cs` (add starterMonsterId field)
- Modify: `Assets/Data/heroes.json` (add starter monster references)

**Steps:**
1. Add `starter_monster_id` field to HeroData
2. Update JSON with monster pairings:
   - Bastion → Ironjaw
   - Rend → Mawling
   - Marrow → Hollow
   - Mirage → Flicker
3. Test data loads correctly

---

### Task 3: Create Dual Model Preview System
**Files:**
- Create: `Assets/Scripts/UI/Menus/HeroMonsterPairPreview.cs`
- Modify: `CharacterSelectController.cs`

**Steps:**
1. Create new component to manage two 3D models
2. Position hero on left, monster on right (slightly back)
3. Add spawn/despawn methods with animation hooks
4. Integrate with existing preview system

---

### Task 4: Create Backdrop System
**Files:**
- Create: `Assets/Scripts/UI/Menus/CharacterSelectBackdropController.cs`
- Create: `Assets/Art/Textures/Backdrops/` (4 backdrop images)

**Steps:**
1. Create backdrop controller with crossfade capability
2. Map Path → backdrop texture
3. Trigger backdrop change on hero selection
4. Implement smooth 0.4s crossfade transition

---

### Task 5: Add Monster Info Panel
**Files:**
- Modify: `Assets/UI/Templates/CharacterSelect.uxml`
- Modify: `Assets/UI/Styles/CharacterSelect.uss`
- Modify: `CharacterSelectController.cs`

**Steps:**
1. Add monster info section to UXML below stats
2. Style with USS (name, brand icon, flavor text)
3. Populate from monster data on hero selection
4. Animate slide-in with stats panel

---

### Task 6: Implement Selection Animations
**Files:**
- Modify: `CharacterSelectController.cs`
- Modify: `HeroMonsterPairPreview.cs`

**Steps:**
1. Add hero card pulse animation on click
2. Implement model fade out/in transitions
3. Add particle "materialize" effect (placeholder until VFX Graph)
4. Add stat bar value animation (fill from 0 to value)

---

### Task 7: Add Path-Themed Particles (Future - Requires URP)
**Files:**
- Create: `Assets/Art/VFX/CharacterSelect/` (4 particle prefabs)

**Steps:**
1. Create simple particle systems per Path
2. Spawn around hero/monster on selection
3. Match Path color palette
4. (Later: Convert to VFX Graph for quality)

---

## Testing Checklist

- [ ] All 4 heroes selectable
- [ ] Correct starter monster appears with each hero
- [ ] Backdrop changes per Path
- [ ] Stat bars show individual colors (not hero color)
- [ ] Monster info displays correctly
- [ ] Transitions are smooth (~1.2s total)
- [ ] No visual glitches on rapid selection changes
- [ ] Confirm button works with correct hero+monster pair
- [ ] Back button returns to main menu

---

## Future Enhancements (Post-URP Upgrade)

- Full VFX Graph particles per Path
- Real-time lighting changes with backdrop
- Hero/monster look-at behaviors
- Voice lines on selection
- Dynamic camera movement
- Screen-space distortion effects

---

*Design approved: 2026-01-27*
*Ready for implementation*
