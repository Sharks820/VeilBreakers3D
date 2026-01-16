# Battle Chasers Style LoRA Training Dataset
## Optimized for scenario.gg - VEILBREAKERS Game

---

## STYLE TRAINING FOLDER: `/style/` (USE THIS FOR LoRA)

**41 Images | 20 MB | Curated for Best Results**

### Image Breakdown:

| Category | Count | Description |
|----------|-------|-------------|
| Character Concepts | 12 | Full-color heroes, enemies, monsters |
| NPC Vendors | 7 | Joe Mad character designs (colorful) |
| Vendor + Backgrounds | 10 | Characters WITH environment (best for style) |
| Combat Environments | 4 | Forest/bandit camp battle backgrounds |
| Key Art | 2 | Group hero compositions |
| Burst Banners | 2 | Character portrait compilations |
| Comic Covers | 2 | BC #10, #11, #12 (premium Joe Mad art) |
| Colored Vignettes | 1 | Multi-character scene |
| Misc Creatures | 1 | Slime, robotic enemies |

### Artists Represented:
- **Joe Madureira** - Original creator, linework master
- **Grace Liu** - Lead coloring, environments, character concepts
- **Billy Garretsen** - Cover colors, vendor coloring

---

## WHAT'S INCLUDED (STYLE CHARACTERISTICS):

✅ **Vibrant, saturated colors** - Bold comic book palette
✅ **Strong linework** - Joe Mad's signature thick outlines
✅ **Dynamic poses** - Action-ready character stances
✅ **Fantasy RPG aesthetic** - Armor, weapons, magic effects
✅ **Warm lighting** - Golden/amber ambient tones
✅ **Detailed environments** - Taverns, forests, dungeons
✅ **Character variety** - Heroes, NPCs, monsters, bosses

---

## WHAT'S EXCLUDED (Kept in `/excluded/` and `/ui/`):

❌ B&W Inktober sketches (different style, would dilute color training)
❌ UI screens (train separately if needed)
❌ Weapon icons (train separately if needed)

---

## SCENARIO.GG TRAINING TIPS:

1. **Use ONLY the `/style/` folder** for your style LoRA
2. **Recommended settings:**
   - Steps: 1500-2500
   - Learning rate: 1e-4 to 5e-5
   - Batch size: 1-2
3. **Trigger word suggestion:** `battle_chasers_style` or `bcnw_style`
4. **Best for:** Character art, RPG illustrations, fantasy scenes

---

## FILE INVENTORY:

### Character Art (bc_001 - bc_024):
- bc_001-007: NPC Vendors (Joe Mad designs)
- bc_008: Alumon (dark paladin hero)
- bc_009-019: Enemy/creature concepts
- bc_020: Key Art (all heroes)
- bc_021-022: Burst Banners (hero portraits)
- bc_023-024: BC #10 Cover art

### Environment + Character (style_vendor_*):
- Vendor scenes with full backgrounds
- Best for learning color/lighting style

### Pure Environments (style_bg_*):
- Forest combat backgrounds
- Good for scene composition style

### Premium Covers (style_cover_*):
- BC #11, #12 comic covers
- Highest quality Joe Mad art

---

## CREATED FOR: VEILBREAKERS Game
**Art Style:** Battle Chasers: Nightwar inspired
**Engine:** Godot 4.3+
**Date:** December 26, 2025

---

*Dataset curated by Claude Code for optimal LoRA training results.*
