# Hero Swap Complete - v2.0

## Summary
Successfully swapped the 4 original heroes (Bastion, Marrow, Mirage, Rend) with 4 new heroes (Vex, Seraphina, Orion, Nyx).

## Archived
- Original heroes saved to: `Assets/Data/heroes_archived_v1.json`

## New Hero Roster

| Hero | Gender | Path | Brand | Role | Starter Monster |
|------|--------|------|-------|------|-----------------|
| **Vex** | Male | IRONBOUND | IRON | Tank | skitter_teeth |
| **Seraphina** | Female | FANGBORN | SAVAGE | DPS | grimthorn |
| **Orion** | Male | VOIDTOUCHED | RUIN | Mage | voltgeist |
| **Nyx** | Female | UNCHAINED | VOID | Hybrid | bloodshade |

## Files Modified

### Data Files
1. ✅ `Assets/Data/heroes.json` - New heroes active
2. ✅ `Assets/Resources/Data/heroes.json` - New heroes active
3. ✅ `Assets/Data/heroes_archived_v1.json` - Old heroes archived

### Documentation
4. ✅ `VEILBREAKERS.md` - Updated path table with new starter heroes
5. ✅ `Assets/Scripts/Data/HeroData.cs` - Updated comment

### UI Styles (Both Assets/ and Assets/Resources/)
6. ✅ `UI/Styles/VeilBreakersTheme.uss` - Added new hero color variables
7. ✅ `UI/Styles/VeilBreakers.uss` - Added new hero card styles
8. ✅ `Resources/UI/Styles/VeilBreakersTheme.uss` - Added new hero color variables
9. ✅ `Resources/UI/Styles/VeilBreakers.uss` - Added new hero card styles

### Test Files
10. ✅ `Assets/Scripts/Test/SaveSystemTests.cs` - Updated hero references
11. ✅ `Assets/Scripts/Test/TestArenaManager.cs` - Updated hero references

## CSS Classes Available

For character selection UI, use these classes:
- `.hero-vex` - Steel warden theming
- `.hero-seraphina` - Poison rose theming
- `.hero-orion` - Lightning blue theming
- `.hero-nyx` - Void purple theming

## Color Variables

Each hero has CSS variables:
- `--hero-{name}` - Primary color
- `--hero-{name}-glow` - Highlight/glow color
- `--hero-{name}-dark` - Dark/shadow color

## Known Issues / TODO

1. **Art Assets Needed** - New heroes need portrait sprites:
   - `assets/characters/heroes/vex_portrait.png`
   - `assets/characters/heroes/seraphina_portrait.png`
   - `assets/characters/heroes/orion_portrait.png`
   - `assets/characters/heroes/nyx_portrait.png`

2. **Item Descriptions** - Some items still reference old heroes:
   - "Rend's signature weapons" (Bone Axes)
   - "Mirage's focus of power" (Eye Staff)
   - "Marrow's channeling focus" (Heart Staff)
   - "Bastion's wall" (Tower Shield)
   - These are flavor text and can be updated as needed

3. **Skills** - Some skills share names with old heroes:
   - `rend` (monster skill) - This is a verb/action, not a reference
   - `last_bastion` - Could rename to `eternal_imprisonment` for Vex
   - `bastion_form` - Monster skill, could rename

## Next Steps

1. Create portrait art for new heroes
2. Design AAA character selection screen
3. Test game flow with new heroes
4. Update item descriptions if desired
