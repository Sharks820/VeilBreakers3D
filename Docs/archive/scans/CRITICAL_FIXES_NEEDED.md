# CRITICAL FIXES - DO THESE NOW

## ✅ DONE - Lightning Visibility Fixed
- **Changed:** Lightning lifetime: 0.15s → 0.6s (4x longer)
- **Changed:** Outer glow opacity: 15% → 50% (3.3x brighter)
- **Changed:** Middle glow opacity: 40% → 80% (2x brighter)
- **Committed:** v2.62

**Test:** Run game, lightning should now be clearly visible every 2.5-5 seconds

---

## ⚠️ CRITICAL - Unity Editor Manual Fix REQUIRED (Purple Screen)

### Problem: "No Theme Style Sheet set to PanelSettings" + Purple Screen

The purple screen is Unity's debug visualization for broken UI panels.
**I updated the .asset file but Unity won't reload it while running.**

### YOU MUST DO THIS MANUALLY IN UNITY EDITOR:

1. **STOP Play Mode** (if running)
2. In Unity, go to Project window
3. Navigate to: `Assets/UI/`
4. Find: `VeilBreakersPanelSettings.asset`
5. **Right-click → Reimport** (forces Unity to reload the file)
6. OR: Click on it → Inspector → Theme USS field should now show `VeilBreakersTheme`
7. If field is still empty, click circle button and select `VeilBreakersTheme.uss`
8. Save (Ctrl+S)

**Why Manual Fix Required:**
- Unity caches asset references and won't reload .asset files during Play Mode
- Code changes updated the file, but Unity needs to be told to reimport it
- This MUST be done in Editor, not via code

---

## ✅ DONE - All Other UI Polish
- Font readability improved (+78% brightness on labels)
- Hero card orbs match hero colors (Bastion blue, Rend red)
- Signature monster section matches hero theme
- Stat bar fills match stat value colors exactly
- Brand affinity display shows both Brand and Path

---

## ⏳ STILL BROKEN

### Settings Dropdown Positioning (4 failed attempts)
- **Status:** Pending for next session
- **File:** `Assets/Scripts/UI/Menus/SettingsPanelController.cs`
- **Problem:** Dropdown appears at bottom instead of below button
- **Next Approach:** Try using `worldBound` + `ChangeCoordinatesTo()`

---

## 📋 REMAINING TASKS

1. ⏳ Task #2: Fix settings dropdown (attempt #5 needed)
2. ⏳ Task #3: Audit all menu screens
3. ⏳ Task #7: Rebalance hero stats
4. ⏳ Task #9: Add VeilBringer eyes to main menu

---

## 🚀 NEXT SESSION SHOULD:
1. Verify lightning is now visible (test in play mode)
2. Verify PanelSettings theme is set (check Unity Inspector)
3. Fix settings dropdown (attempt #5)
4. Add VeilBringer eyes to main menu

**See `Docs/SHIFT_HANDOFF.md` for comprehensive details.**

