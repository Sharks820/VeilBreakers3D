# Unity 6 Migration & Character Select Rebuild Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Migrate VeilBreakers to Unity 6 with new Input System, URP rendering, rebuilt Character Select screen, and 3D hero assets.

**Architecture:** Phase-gated migration - fix critical UI bugs first, then migrate Input System (enables new hardware support), then Character Select rebuild with 3D assets, then URP migration. Each phase is independently testable.

**Tech Stack:** Unity 6, New Input System, URP, UI Toolkit, C# 12

---

## Phase 0: Critical Bug Fixes (Pre-Migration)

### Task 0.1: Fix Settings Dropdown Positioning Bug

**Problem:** Dropdowns in Settings menu position incorrectly (off-screen left) on first open, then correct themselves on subsequent interactions.

**Files:**
- Modify: `Assets/Scripts/UI/Controls/VBDropdownField.cs:677-715`

**Step 1: Analyze the bug**

The `PositionPopupIfOpen()` method uses `WorldToLocal` conversion but the popup layer may not have valid layout bounds on first open. The popup is positioned using `translate` which can fail if layout isn't complete.

```csharp
// Current problematic code at line 677-715
private void PositionPopupIfOpen()
{
    // Wait for a valid layout before positioning
    if (_popup.resolvedStyle.height <= 0 && _positionAttempts < 3)
    {
        _positionAttempts++;
        schedule.Execute(PositionPopupIfOpen).ExecuteLater(0);
        return;
    }
    // ... rest uses WorldToLocal which can return wrong values
}
```

**Step 2: Fix the positioning logic**

Replace the positioning to use `schedule.Execute` with a proper delay to ensure layout is valid:

```csharp
private void PositionPopupIfOpen()
{
    if (!_isOpen || _popup == null || _panelRoot == null || _popupLayer == null) return;

    var fieldBounds = _display.worldBound;

    // If layout not ready, retry with actual delay (not 0)
    if (fieldBounds.height <= 0 || fieldBounds.width <= 0 || _popupLayer.resolvedStyle.width <= 0)
    {
        if (_positionAttempts < 5)
        {
            _positionAttempts++;
            schedule.Execute(PositionPopupIfOpen).ExecuteLater(16); // One frame at 60fps
            return;
        }
        // Fallback: position at display element's local position
        _popup.style.left = 0;
        _popup.style.top = resolvedStyle.height;
        _popup.style.translate = StyleKeyword.None;
        return;
    }

    // Convert to popup-layer local space
    var localTopLeft = _popupLayer.WorldToLocal(fieldBounds.position);
    var localBottomLeft = _popupLayer.WorldToLocal(new Vector2(fieldBounds.xMin, fieldBounds.yMax));

    float popupHeight = _popup.resolvedStyle.height;
    if (popupHeight <= 0) popupHeight = 200;

    float targetY = localBottomLeft.y;
    float layerHeight = _popupLayer.resolvedStyle.height > 0 ? _popupLayer.resolvedStyle.height : Screen.height;

    // Flip above if overflow
    if (targetY + popupHeight > layerHeight && (localTopLeft.y - popupHeight) >= 0)
    {
        targetY = localTopLeft.y - popupHeight;
    }

    float targetX = Mathf.Max(0, localTopLeft.x);

    // Use left/top instead of translate for more reliable positioning
    _popup.style.width = fieldBounds.width;
    _popup.style.translate = StyleKeyword.None;
    _popup.style.left = targetX;
    _popup.style.top = targetY;
}
```

**Step 3: Run tests**

Run: Open Settings, click dropdowns, verify they appear correctly on first click.

**Step 4: Commit**

```bash
git add Assets/Scripts/UI/Controls/VBDropdownField.cs
git commit -m "fix: dropdown positioning on first open"
```

---

## Phase 1: Input System Migration

### Task 1.1: Create Input Actions Asset

**Files:**
- Create: `Assets/Settings/VeilBreakersInput.inputactions`
- Create: `Assets/Scripts/Core/VeilBreakersInputActions.cs` (generated)

**Step 1: Create Input Actions via Unity Editor**

In Unity 6:
1. Right-click in `Assets/Settings/` > Create > Input Actions
2. Name it `VeilBreakersInput`
3. Add action maps matching GameAction enum

**Action Maps to create:**

```
UI (ActionMap)
├── Navigate (Value, Vector2) - D-Pad, Left Stick, WASD
├── Submit (Button) - A/Enter/Space
├── Cancel (Button) - B/Escape
├── Point (PassThrough, Vector2) - Mouse position
├── Click (PassThrough, Button) - Mouse left
├── ScrollWheel (PassThrough, Vector2)
└── MiddleClick (PassThrough, Button)

Gameplay (ActionMap)
├── Confirm (Button) - Space/Enter/A
├── Cancel (Button) - Escape/B
├── Skill1-6 (Button) - 1-6 keys, Face buttons
├── Capture (Button) - C/Y
├── Mark (Button) - Tab/LB
├── AllyUltimate (Button) - Q/RB
├── Pause (Button) - Escape/Start
├── TargetNext (Button) - Tab/RB
├── TargetPrev (Button) - Shift+Tab/LB
├── DialogueAdvance (Button) - Space/A/Mouse0
└── DialogueSkip (Button) - Hold Space/X
```

**Step 2: Generate C# wrapper**

In Input Actions asset inspector, click "Generate C# Class" with:
- Class Name: `VeilBreakersInputActions`
- Namespace: `VeilBreakers.Core`
- Output: `Assets/Scripts/Core/VeilBreakersInputActions.cs`

**Step 3: Commit**

```bash
git add Assets/Settings/VeilBreakersInput.inputactions
git add Assets/Scripts/Core/VeilBreakersInputActions.cs
git commit -m "feat: add Unity Input System actions asset"
```

---

### Task 1.2: Update InputManager to Use New Input System

**Files:**
- Modify: `Assets/Scripts/Core/InputManager.cs`

**Step 1: Read current InputManager**

The current InputManager wraps legacy `Input.*` calls. Update it to use `VeilBreakersInputActions`.

**Step 2: Update InputManager implementation**

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace VeilBreakers.Core
{
    public class InputManager : SingletonMonoBehaviour<InputManager>
    {
        private VeilBreakersInputActions _inputActions;
        private bool _isGamepad = false;

        public bool IsGamepad => _isGamepad;
        public event System.Action<bool> OnInputDeviceChanged;

        protected override void OnSingletonAwake()
        {
            _inputActions = new VeilBreakersInputActions();
            _inputActions.Enable();

            // Track input device changes
            InputSystem.onActionChange += OnActionChange;
        }

        private void OnDestroy()
        {
            InputSystem.onActionChange -= OnActionChange;
            _inputActions?.Dispose();
        }

        private void OnActionChange(object obj, InputActionChange change)
        {
            if (change == InputActionChange.ActionPerformed && obj is InputAction action)
            {
                var device = action.activeControl?.device;
                bool wasGamepad = _isGamepad;
                _isGamepad = device is Gamepad;

                if (wasGamepad != _isGamepad)
                {
                    OnInputDeviceChanged?.Invoke(_isGamepad);
                }
            }
        }

        public bool GetActionDown(GameAction action)
        {
            return action switch
            {
                GameAction.Confirm => _inputActions.Gameplay.Confirm.WasPressedThisFrame(),
                GameAction.Cancel => _inputActions.Gameplay.Cancel.WasPressedThisFrame(),
                GameAction.Skill1 => _inputActions.Gameplay.Skill1.WasPressedThisFrame(),
                GameAction.Skill2 => _inputActions.Gameplay.Skill2.WasPressedThisFrame(),
                GameAction.Skill3 => _inputActions.Gameplay.Skill3.WasPressedThisFrame(),
                GameAction.Skill4 => _inputActions.Gameplay.Skill4.WasPressedThisFrame(),
                GameAction.Skill5 => _inputActions.Gameplay.Skill5.WasPressedThisFrame(),
                GameAction.Skill6 => _inputActions.Gameplay.Skill6.WasPressedThisFrame(),
                GameAction.Capture => _inputActions.Gameplay.Capture.WasPressedThisFrame(),
                GameAction.Mark => _inputActions.Gameplay.Mark.WasPressedThisFrame(),
                GameAction.AllyUltimate => _inputActions.Gameplay.AllyUltimate.WasPressedThisFrame(),
                GameAction.Pause => _inputActions.Gameplay.Pause.WasPressedThisFrame(),
                GameAction.TargetNext => _inputActions.Gameplay.TargetNext.WasPressedThisFrame(),
                GameAction.TargetPrev => _inputActions.Gameplay.TargetPrev.WasPressedThisFrame(),
                GameAction.DialogueAdvance => _inputActions.Gameplay.DialogueAdvance.WasPressedThisFrame(),
                GameAction.DialogueSkip => _inputActions.Gameplay.DialogueSkip.WasPressedThisFrame(),
                _ => false
            };
        }

        public bool GetAction(GameAction action)
        {
            return action switch
            {
                GameAction.Confirm => _inputActions.Gameplay.Confirm.IsPressed(),
                GameAction.Cancel => _inputActions.Gameplay.Cancel.IsPressed(),
                GameAction.DialogueSkip => _inputActions.Gameplay.DialogueSkip.IsPressed(),
                // Add others as needed
                _ => false
            };
        }

        public bool GetMouseButtonDown(int button)
        {
            return button == 0 && _inputActions.UI.Click.WasPressedThisFrame();
        }

        public Vector2 GetMousePosition()
        {
            return _inputActions.UI.Point.ReadValue<Vector2>();
        }

        // Enable/disable action maps
        public void EnableGameplay() => _inputActions.Gameplay.Enable();
        public void DisableGameplay() => _inputActions.Gameplay.Disable();
        public void EnableUI() => _inputActions.UI.Enable();
        public void DisableUI() => _inputActions.UI.Disable();
    }
}
```

**Step 3: Run tests**

Run: Open game, test keyboard and mouse input works, test gamepad if available.

**Step 4: Commit**

```bash
git add Assets/Scripts/Core/InputManager.cs
git commit -m "feat: migrate InputManager to new Input System"
```

---

### Task 1.3: Migrate Remaining Legacy Input.* Calls

**Files to migrate (7 total):**
1. `Assets/Scripts/UI/Menus/VERADialogueController.cs` - lines 126, 132
2. `Assets/Scripts/Combat/QTEController.cs` - line 279
3. `Assets/Scripts/Capture/CaptureManager.cs` - lines 150, 160
4. `Assets/Scripts/UI/Combat/SkillSlotController.cs` - line 320
5. `Assets/Scripts/UI/Combat/CombatHUD.cs` - lines 332, 334
6. `Assets/Scripts/UI/Combat/CaptureBannerController.cs` - line 110
7. `Assets/Scripts/UI/Combat/AllyPanelController.cs` - line 92

**Pattern for each file:**

Replace `Input.GetKeyDown(KeyCode.X)` with `InputManager.Instance.GetActionDown(GameAction.X)`
Replace `Input.GetMouseButtonDown(0)` with `InputManager.Instance.GetMouseButtonDown(0)`

**Step 1: Migrate VERADialogueController.cs**

```csharp
// Line 126: Replace
// OLD: if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
// NEW:
if (InputManager.Instance.GetMouseButtonDown(0) || InputManager.Instance.GetActionDown(GameAction.DialogueAdvance))
```

**Step 2: Migrate each remaining file following same pattern**

Each file: Read, find Input.* calls, replace with InputManager equivalent, save.

**Step 3: Run build to verify no compile errors**

Run: `Unity -batchmode -nographics -projectPath . -buildTarget StandaloneWindows64 -quit`

**Step 4: Commit all migrations**

```bash
git add Assets/Scripts/UI/Menus/VERADialogueController.cs
git add Assets/Scripts/Combat/QTEController.cs
git add Assets/Scripts/Capture/CaptureManager.cs
git add Assets/Scripts/UI/Combat/SkillSlotController.cs
git add Assets/Scripts/UI/Combat/CombatHUD.cs
git add Assets/Scripts/UI/Combat/CaptureBannerController.cs
git add Assets/Scripts/UI/Combat/AllyPanelController.cs
git commit -m "refactor: migrate all legacy Input.* to InputManager"
```

---

## Phase 2: Character Select Screen Rebuild

### Task 2.1: Design New Hero Data

**Files:**
- Modify: `Assets/Resources/Data/heroes.json`

**New Heroes (keeping 4, refreshing data for Unity 6 look):**

| Hero | Brand | Path | Role | Resource | Starter Monster |
|------|-------|------|------|----------|-----------------|
| **Bastion** | IRON | IRONBOUND | Tank | GUARD | Ironjaw |
| **Rend** | SAVAGE | FANGBORN | DPS | FURY | Mawling |
| **Marrow** | LEECH | VOIDTOUCHED | Healer | MANA | Hollow |
| **Mirage** | DREAD | UNCHAINED | Controller | MANA | Flicker |

**Step 1: Update heroes.json with fresh data**

Ensure all fields are populated, particularly:
- `resource_type` field (MANA, GUARD, or FURY)
- `color_palette` with distinctive colors
- Updated `backstory` and `description`

**Step 2: Verify GameDataAssets has HeroesJson assigned**

Check `Assets/Data/GameDataAssets.asset` in Unity Inspector.

**Step 3: Commit**

```bash
git add Assets/Resources/Data/heroes.json
git commit -m "data: refresh hero data for Unity 6"
```

---

### Task 2.2: Fix GameDatabase Hero Loading Race Condition

**Problem:** CharacterSelectController.OnEnable() calls PopulateHeroList() which queries GameDatabase, but GameDatabase.LoadAllDataAsync() may not be complete yet.

**Files:**
- Modify: `Assets/Scripts/UI/Menus/CharacterSelectController.cs`

**Step 1: Add async wait for GameDatabase**

```csharp
private async void OnEnable()
{
    InitializeUI();

    // Wait for GameDatabase to finish loading
    while (GameDatabase.Instance == null || !GameDatabase.Instance.IsLoaded)
    {
        await System.Threading.Tasks.Task.Delay(50);
    }

    PopulateHeroList();
    PlayEntranceAnimation();
}
```

**Step 2: Run in Unity**

Open CharacterSelect scene, verify heroes appear.

**Step 3: Commit**

```bash
git add Assets/Scripts/UI/Menus/CharacterSelectController.cs
git commit -m "fix: wait for GameDatabase before populating hero list"
```

---

### Task 2.3: Rebuild Character Select UI Template

**Files:**
- Modify: `Assets/UI/Templates/CharacterSelect.uxml`

**Step 1: Redesign for cleaner Unity 6 look**

Key changes:
- Larger hero portraits (96x96 instead of 48x48)
- Animated selection indicator
- Brand/Path orbs with glow effects
- Stats displayed as radial bars or simplified bars
- 3D model preview area with proper lighting

**Step 2: Test in UI Builder**

Open UI Builder, load CharacterSelect.uxml, verify layout.

**Step 3: Commit**

```bash
git add Assets/UI/Templates/CharacterSelect.uxml
git commit -m "ui: rebuild CharacterSelect for Unity 6"
```

---

## Phase 3: 3D Asset Generation

### Task 3.1: Generate Hero Concept Art

**Pipeline:** Use AI tools to generate concept art, then convert to 3D.

**Step 1: Create hero concept prompts**

For each hero, generate concept art using Scenario/Midjourney:

**Bastion (Tank):**
```
Dark fantasy armored warrior, bulky defensive stance,
tower shield with iron motifs, steel blue and gray color scheme,
worn battle damage, glowing rune etchings, grimdark style
```

**Rend (DPS):**
```
Feral beast hunter, scarred and primal, leather and bone armor,
dual curved blades, crimson red accents, tribal markings,
crouched predatory pose, grimdark dark fantasy
```

**Marrow (Healer):**
```
Ethereal drain healer, gaunt and pale, flowing dark robes,
purple and void energy wisps, skeletal hand motifs,
glowing eyes, mysterious and haunting, dark fantasy style
```

**Mirage (Controller):**
```
Reality-bending illusionist, shifting form, green energy fractals,
tattered cloak with impossible patterns, multiple fading silhouettes,
eerie smile, otherworldly presence, dark fantasy horror
```

**Step 2: Save concepts**

Save to: `Assets/Art/Concepts/Heroes/`

**Step 3: Commit**

```bash
git add Assets/Art/Concepts/Heroes/
git commit -m "art: add hero concept art"
```

---

### Task 3.2: Convert Concepts to 3D Models

**Pipeline:** Tripo AI (2D to 3D) or manual Blender modeling

**Step 1: Generate base 3D models**

Using Tripo or similar:
1. Upload concept art
2. Generate 3D mesh with auto-rigging
3. Export as FBX

**Step 2: Polish in Blender**

For each model:
1. Import FBX
2. Clean up topology
3. Add materials
4. Verify rig weights
5. Export as FBX to `Assets/Art/3D_Models/Heroes/`

**Step 3: Import to Unity**

- Set humanoid rig
- Configure materials for URP (after Phase 4)

**Step 4: Commit**

```bash
git add Assets/Art/3D_Models/Heroes/
git commit -m "art: add hero 3D models"
```

---

### Task 3.3: Generate Starter Monster Models

Repeat Task 3.2 for starter monsters:
- Ironjaw (IRON tank beast)
- Mawling (SAVAGE feral creature)
- Hollow (VOID ethereal entity)
- Flicker (SURGE lightning elemental)

---

### Task 3.4: Create Hero Animations

**Pipeline:** Cascadeur for physics-based animation or Mixamo

**Animations needed per hero:**
1. Idle (looping)
2. Walk
3. Run
4. Attack_Basic
5. Skill_Cast
6. Hit_React
7. Death
8. Victory

**Step 1: Generate/create animations**

**Step 2: Import to Unity**

Configure animation clips, create Animator Controller.

**Step 3: Commit**

```bash
git add Assets/Art/Animations/Heroes/
git commit -m "anim: add hero animations"
```

---

## Phase 4: URP Migration

### Task 4.1: Install URP Package

**Step 1: Add URP via Package Manager**

```
Window > Package Manager > Unity Registry > Universal RP > Install
```

**Step 2: Create URP Asset**

```
Assets > Create > Rendering > URP Asset (with Universal Renderer)
```

Save as: `Assets/Settings/VeilBreakersURP.asset`

**Step 3: Configure Project Settings**

```
Edit > Project Settings > Graphics > Scriptable Render Pipeline Settings
```

Assign: `VeilBreakersURP`

**Step 4: Commit**

```bash
git add Assets/Settings/VeilBreakersURP.asset
git add ProjectSettings/GraphicsSettings.asset
git commit -m "feat: install and configure URP"
```

---

### Task 4.2: Upgrade Materials to URP

**Step 1: Run material upgrader**

```
Edit > Rendering > Materials > Convert All Built-in Materials to URP
```

**Step 2: Fix any materials that didn't convert**

Manually assign URP/Lit shader to problem materials.

**Step 3: Test all scenes**

Open each scene, verify materials render correctly.

**Step 4: Commit**

```bash
git add Assets/Art/Materials/
git commit -m "refactor: upgrade materials to URP"
```

---

### Task 4.3: Configure URP Rendering Features

**Step 1: Add post-processing**

- Bloom (for UI glow effects)
- Vignette (for low health effect)
- Color grading (dark fantasy look)

**Step 2: Add screen-space effects**

- Ambient occlusion
- Screen-space reflections (if performance allows)

**Step 3: Commit**

```bash
git add Assets/Settings/VeilBreakersURP.asset
git commit -m "feat: configure URP post-processing"
```

---

## Phase 5: Unity 6 Upgrade

### Task 5.1: Backup and Upgrade

**Step 1: Create backup**

```bash
cd C:\Users\Conner\OneDrive\Documents
xcopy /E /I VeilBreakers3DCurrent VeilBreakers3D_PreUnity6_Backup
```

**Step 2: Open project in Unity 6**

Unity Hub > Open > Navigate to project > Select Unity 6

**Step 3: Let Unity upgrade project**

Accept all upgrade prompts.

**Step 4: Fix any compilation errors**

Unity 6 may require API updates.

**Step 5: Commit**

```bash
git add -A
git commit -m "chore: upgrade to Unity 6"
```

---

### Task 5.2: Test Full Game Loop

**Step 1: Test Main Menu**

- All buttons work
- Settings dropdowns position correctly
- Transitions work

**Step 2: Test Character Select**

- All 4 heroes display
- Hero data loads correctly
- 3D preview shows models
- Selection works

**Step 3: Test Combat (if available)**

- Input System works with keyboard
- Input System works with gamepad
- Skills activate correctly

**Step 4: Document any issues**

Create issues for any problems found.

---

## Completion Checklist

- [ ] Phase 0: Dropdown positioning fix
- [ ] Phase 1.1: Input Actions asset created
- [ ] Phase 1.2: InputManager updated to new Input System
- [ ] Phase 1.3: All legacy Input.* calls migrated
- [ ] Phase 2.1: Hero data refreshed
- [ ] Phase 2.2: GameDatabase race condition fixed
- [ ] Phase 2.3: Character Select UI rebuilt
- [ ] Phase 3.1: Hero concept art generated
- [ ] Phase 3.2: Hero 3D models created
- [ ] Phase 3.3: Starter monster models created
- [ ] Phase 3.4: Hero animations added
- [ ] Phase 4.1: URP installed and configured
- [ ] Phase 4.2: Materials upgraded to URP
- [ ] Phase 4.3: Post-processing configured
- [ ] Phase 5.1: Unity 6 upgrade complete
- [ ] Phase 5.2: Full game loop tested

---

*Plan created: 2026-02-01*
*Estimated completion: 3-5 development sessions*
