# CRITICAL FIXES - DO THESE NOW

## ✅ DONE - Lightning Visibility Fixed
- **Changed:** Lightning lifetime: 0.15s → 0.6s (4x longer)
- **Changed:** Outer glow opacity: 15% → 50% (3.3x brighter)
- **Changed:** Middle glow opacity: 40% → 80% (2x brighter)
- **Committed:** v2.62

**Test:** Run game, lightning should now be clearly visible every 2.5-5 seconds

---

## ⚠️ CRITICAL - Unity Editor Fix Required

### Problem: "No Theme Style Sheet set to PanelSettings"

This error means UI won't render properly. You need to **manually set the theme in Unity Editor**:

### Fix Steps:
1. In Unity, go to Project window
2. Navigate to: `Assets/UI/` or `Assets/Resources/UI/`
3. Find: `VeilBreakersPanelSettings.asset`
4. Click on it to select
5. In Inspector panel, find "Theme Style Sheet" field
6. Click the circle button next to it
7. Select: `VeilBreakersTheme` (from `Assets/UI/Styles/VeilBreakersTheme.uss`)
8. Click Apply or save scene

**Alternative Path:**
- If VeilBreakersTheme is in `Assets/Resources/UI/Styles/`, use that one instead

### Why This Happened:
- Theme Style Sheet reference was lost (possibly during Unity 6 migration)
- This is a Unity asset setting, cannot be fixed via code
- Must be set manually in Unity Editor

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

