# Unity 6 Migration Prep

> **Status:** IN PROGRESS | **Current Unity:** 2022.3.62f3 LTS | **Target:** Unity 6

---

## Overview

This document tracks deprecated APIs and patterns that need attention before migrating to Unity 6.

---

## Priority 1: Input System Migration

**Impact:** HIGH - Unity 6 strongly recommends new Input System package

| File | Usage Count | Status |
|------|-------------|--------|
| RadialMenuController.cs | 21 | :x: |
| QTEController.cs | 1 | :x: |
| CaptureManager.cs | 2 | :x: |
| VERADialogueController.cs | 2 | :x: |
| AllyPanelController.cs | 1 | :x: |
| CaptureBannerController.cs | 1 | :x: |
| CombatHUD.cs | 2 | :x: |
| SkillSlotController.cs | 1 | :x: |

**Total:** 8 files, ~32 usages

### Migration Strategy
1. Create `InputManager` abstraction layer
2. Use Input Actions asset for bindings
3. Support both keyboard/mouse and gamepad
4. Allow rebinding in settings

### APIs to Replace
| Old API | New API |
|---------|---------|
| `Input.GetKeyDown()` | `InputAction.triggered` |
| `Input.GetKey()` | `InputAction.IsPressed()` |
| `Input.GetMouseButtonDown()` | Mouse click action |
| `Input.mousePosition` | `Mouse.current.position` |

---

## Priority 2: Resources.Load Usage

**Impact:** MEDIUM - Still works but Addressables recommended for larger projects

| File | Usage |
|------|-------|
| GameDatabase.cs | Data loading |
| MainMenuBootstrap.cs | UI setup |
| StatusEffectManager.cs | Effect prefabs |
| MenuBootstrap.cs | UI setup |
| UIAutoSetup.cs | UI setup |

### Decision
- Keep Resources.Load for now (5 files)
- Consider Addressables for asset bundles later
- Not blocking for Unity 6

---

## Priority 3: PlayerPrefs Usage

**Impact:** LOW - Works fine, but game data should use SaveManager

| File | Purpose |
|------|---------|
| SettingsPanelController.cs | UI settings |
| SaveFileHandler.cs | Save metadata |
| SettingsManager.cs | Game settings |

### Status
- :white_check_mark: Game saves use SaveManager (binary, not PlayerPrefs)
- :white_check_mark: Settings appropriately use PlayerPrefs
- No action needed

---

## Priority 4: Material Access

**Impact:** LOW - Current usage is correct

| File | Usage |
|------|-------|
| TitleScreenVFXController.cs | Runtime material instances |
| TitleScreenVFXSetup.cs | Editor script |
| TestArenaSetup.cs | Editor script |

### Status
- :white_check_mark: Using `.material` for runtime instances (correct)
- :white_check_mark: Editor scripts can use Shader.Find
- No action needed

---

## Not Found (Good!)

These deprecated APIs are NOT used in the codebase:

- :white_check_mark: No `WWW` class usage
- :white_check_mark: No `Application.LoadLevel()`
- :white_check_mark: No legacy IMGUI in game code (`OnGUI`, `GUILayout`)
- :white_check_mark: No `FindObjectOfType` in Update loops
- :white_check_mark: No `.sharedMaterial` misuse

---

## Render Pipeline Notes

**Current:** Built-in Render Pipeline
**Target:** URP (Universal Render Pipeline)

### Shader Migration Required
- All custom shaders need URP versions
- VFX materials use standard shaders (should auto-convert)
- Test VFX after migration

---

## Pre-Migration Checklist

Before starting Unity 6 migration:

- [x] Input System abstraction layer created (`InputManager.cs`)
- [ ] All Input.* calls go through InputManager (8 files need update)
- [ ] Input Actions asset created with all bindings
- [ ] Test on both keyboard and gamepad
- [x] Backup project (already on Git)
- [ ] Document any Unity 2022 specific workarounds

---

## Safe to Do Now (Pure C# Logic)

These systems can be developed before Unity 6 since they don't depend on Unity APIs:

- Combat math/damage calculations
- AI/Gambit decision logic
- Data structures and game state
- Brand/Synergy/Corruption formulas
- Monster/Hero stat systems

---

*Last Updated: 2026-01-28*
