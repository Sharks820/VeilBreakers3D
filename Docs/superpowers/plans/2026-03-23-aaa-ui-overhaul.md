# AAA UI Overhaul Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Transform VeilBreakers main menu and character select screens from prototype-quality to AAA RPG standard, matching the approved V5 mockup.

**Architecture:** Pure USS/UXML visual overhaul for most changes. C# modifications for stat number animations, embark hold effects, and champion model viewer. Title screen button layout changes from horizontal to vertical. Character select stat system redesigned (SPD→STAMINA, individual stat colors, D&D attributes as numbers).

**Tech Stack:** Unity UI Toolkit (USS/UXML), C# MonoBehaviour controllers, PrimeTween for animations, existing TitleScreenVFX system.

**Spec:** `docs/superpowers/specs/2026-03-23-aaa-ui-overhaul-design.md`
**Mockup:** `.superpowers/brainstorm/stable/aaa-v5.html`

---

## Chunk 1: Title Screen — Button Layout & Styling

### Task 1: Change MainMenu UXML from horizontal to vertical button layout

**Files:**
- Modify: `Assets/UI/Templates/MainMenu.uxml`
- Modify: `Assets/Resources/UI/Templates/MainMenu.uxml` (keep in sync)

- [ ] **Step 1: Read both UXML files to confirm current state**

Both files should be identical. The `button-container` currently uses `flex-direction: row`.

- [ ] **Step 2: Update button-container to vertical right-aligned layout**

Change the `button-container` element in both files:

```xml
<ui:VisualElement name="button-container" style="position: absolute; right: 5%; bottom: 7%; flex-direction: column; align-items: flex-end; padding: 0; background-color: transparent;">
    <ui:Button text="NEW GAME" name="btn-new-game" class="vb-button vb-menu-btn" style="width: 300px; height: 56px; font-size: 18px; margin-bottom: 9px;" />
    <ui:Button text="CONTINUE" name="btn-continue" class="vb-button vb-menu-btn" style="display: none; width: 300px; height: 56px; font-size: 18px; margin-bottom: 9px;" />
    <ui:Button text="SETTINGS" name="btn-settings" class="vb-button vb-menu-btn" style="width: 300px; height: 56px; font-size: 18px; margin-bottom: 9px;" />
    <ui:Button text="CREDITS" name="btn-credits" class="vb-button vb-menu-btn" style="width: 300px; height: 56px; font-size: 18px; margin-bottom: 9px;" />
    <ui:Button text="EXIT" name="btn-exit" class="vb-button vb-menu-btn" style="width: 300px; height: 56px; font-size: 18px;" />
</ui:VisualElement>
```

Key changes: `flex-direction: column`, `right: 5%; bottom: 7%`, all buttons same width 300px/height 56px, unified class `vb-menu-btn`, removed per-button margin-right, added margin-bottom gap.

- [ ] **Step 3: Verify both UXML files are identical**
- [ ] **Step 4: Commit**

```
feat: change main menu buttons from horizontal to vertical layout
```

---

### Task 2: Restyle main menu buttons in USS — dark base, orange on hover

**Files:**
- Modify: `Assets/UI/Styles/VeilBreakers.uss` (lines ~2301-2369, the `#menu-root` button rules)

- [ ] **Step 1: Read current button styles (lines 2290-2400)**
- [ ] **Step 2: Replace ALL `#menu-root` button rules with unified style**

Delete the separate primary/secondary/active rule blocks (lines ~2301-2369) and replace with:

```css
/* =========================================================================
   MAIN MENU BUTTONS — Unified dark base, orange on hover (AAA V5)
   ========================================================================= */

#menu-root .vb-menu-btn,
#menu-root #btn-new-game,
#menu-root #btn-continue,
#menu-root #btn-settings,
#menu-root #btn-credits,
#menu-root #btn-exit {
    -unity-font-definition: url('project://database/Assets/UI/Fonts/Cinzel-Variable.ttf?fileID=12800000&guid=c7b3e9f2a4d816543e8c1a0b5f927d4e&type=3');
    -unity-text-align: middle-center;
    width: 300px;
    height: 56px;
    font-size: 18px;
    -unity-font-style: bold;
    letter-spacing: 5px;
    /* Dark ornate base */
    background-color: rgba(38, 28, 18, 0.97);
    border-width: 1px;
    border-radius: 3px;
    border-color: rgba(140, 100, 50, 0.30);
    border-top-color: rgba(180, 140, 70, 0.20);
    color: rgba(210, 180, 130, 0.85);
    text-shadow: 0px 1px 4px rgba(0, 0, 0, 0.7);
    transition-property: background-color, border-color, color, scale, translate;
    transition-duration: 0.25s, 0.25s, 0.25s, 0.15s, 0.15s;
    transition-timing-function: ease-out;
}

#menu-root .vb-menu-btn:hover,
#menu-root #btn-new-game:hover,
#menu-root #btn-continue:hover,
#menu-root #btn-settings:hover,
#menu-root #btn-credits:hover,
#menu-root #btn-exit:hover {
    /* Orange gradient on hover */
    background-color: rgb(200, 95, 22);
    border-color: rgba(255, 180, 80, 0.70);
    border-top-color: rgba(255, 220, 140, 0.50);
    color: rgb(255, 248, 232);
    text-shadow: 0px 0px 12px rgba(255, 200, 100, 0.5), 0px 2px 4px rgba(0, 0, 0, 0.6);
    scale: 1.04 1.04;
    translate: 0 -2px;
}

#menu-root .vb-menu-btn:active,
#menu-root #btn-new-game:active,
#menu-root #btn-continue:active,
#menu-root #btn-settings:active,
#menu-root #btn-credits:active,
#menu-root #btn-exit:active {
    background-color: rgb(160, 70, 12);
    scale: 0.97 0.97;
    translate: 0 0;
    transition-duration: 0.08s;
}
```

Note: Unity UI Toolkit doesn't support CSS `linear-gradient()` or `box-shadow`. The gradient and glow effects will be handled via C# in ButtonVFXHelper (Task 3). The USS provides the color transitions which ARE supported.

- [ ] **Step 3: Verify no duplicate/conflicting rules remain for menu buttons**
- [ ] **Step 4: Commit**

```
feat: restyle main menu buttons — dark base, orange on hover
```

---

### Task 3: Update ButtonVFXHelper for title screen button effects

**Files:**
- Modify: `Assets/Scripts/UI/Controls/ButtonVFXHelper.cs`
- Modify: `Assets/Scripts/UI/Menus/MainMenuController.cs`

- [ ] **Step 1: Read ButtonVFXHelper.cs to understand current ApplyEffects method**
- [ ] **Step 2: Read MainMenuController.cs PlayEntranceAnimation and button setup (lines ~800-860)**
- [ ] **Step 3: Add a decorative top-line element to each button in ButtonVFXHelper**

In `ButtonVFXHelper.cs`, add a method:

```csharp
/// <summary>
/// Adds a decorative gradient highlight line at the top of a button.
/// Creates the "ornate" look for main menu buttons.
/// </summary>
public static void AddTopHighlight(VisualElement button)
{
    if (button == null) return;

    var highlight = new VisualElement();
    highlight.name = "btn-top-highlight";
    highlight.pickingMode = PickingMode.Ignore;
    highlight.style.position = Position.Absolute;
    highlight.style.top = 0;
    highlight.style.left = Length.Percent(15);
    highlight.style.right = Length.Percent(15);
    highlight.style.height = 1;
    highlight.style.backgroundColor = new Color(0.78f, 0.59f, 0.27f, 0.3f);

    button.Add(highlight);
}
```

- [ ] **Step 4: In MainMenuController's button setup, call AddTopHighlight on each menu button**

In the `PlayEntranceAnimation` method (or the button VFX section), after applying effects to each button, add:

```csharp
ButtonVFXHelper.AddTopHighlight(button);
```

- [ ] **Step 5: Commit**

```
feat: add decorative top-line highlight to main menu buttons
```

---

### Task 4: Improve logo visibility with dark backing

**Files:**
- Modify: `Assets/UI/Templates/MainMenu.uxml`
- Modify: `Assets/Resources/UI/Templates/MainMenu.uxml`

- [ ] **Step 1: Add a dark radial backing element behind the logo in UXML**

Inside the `logo-container`, insert a backing element BEFORE the logo-image:

```xml
<ui:VisualElement name="logo-container" picking-mode="Ignore" style="position: absolute; left: 0; right: 0; top: -60px; height: 480px; justify-content: flex-start; align-items: center; background-color: rgba(0, 0, 0, 0);">
    <ui:VisualElement name="logo-backing" picking-mode="Ignore" style="position: absolute; width: 100%; height: 100%; background-color: rgba(0, 0, 0, 0.35);" />
    <ui:VisualElement name="logo-image" style="width: 1600px; height: 400px; background-image: url('project://database/Assets/Art/UI/MainMenu/logo_veilbreakers.png?fileID=2800000&amp;guid=068214e22c8853d4c8fe90c83ab65bbc&amp;type=3'); -unity-background-scale-mode: scale-to-fit; background-color: rgba(0, 0, 0, 0); background-position-x: center; background-position-y: center;" />
</ui:VisualElement>
```

The `logo-backing` provides a subtle dark wash behind the logo to improve readability against bright background art.

- [ ] **Step 2: Update both UXML files identically**
- [ ] **Step 3: Commit**

```
feat: add dark backing behind logo for visibility
```

---

## Chunk 2: Character Select — Stat System Redesign

### Task 5: Rename SPD to STAMINA in data and UI

**Files:**
- Modify: `Assets/UI/Screens/CharacterSelect.uxml` (stat-spd element)
- Modify: `Assets/Scripts/UI/CharacterSelect/HeroDataPanelController.cs`

- [ ] **Step 1: Read HeroDataPanelController.cs to find where stat-spd is populated**

The `_statSpd` label is set via `CharSelectUIUtils.SetLabel(_statSpd, data.base_speed.ToString())`.

- [ ] **Step 2: In CharacterSelect.uxml, rename the stat chip**

Change:
```xml
<ui:VisualElement class="stat-chip">
    <ui:Label class="stat-chip-label" text="SPD" />
    <ui:Label name="stat-spd" class="stat-chip-value" text="5" />
</ui:VisualElement>
```
To:
```xml
<ui:VisualElement class="stat-chip stat-chip-stamina">
    <ui:Label class="stat-chip-label" text="STAMINA" />
    <ui:Label name="stat-stamina" class="stat-chip-value" text="45" />
</ui:VisualElement>
```

- [ ] **Step 3: Update HeroDataPanelController.cs — rename field and cache reference**

Change `_statSpd` to `_statStamina`, update the Q() query to `"stat-stamina"`, and keep populating with `data.base_speed` (the underlying data field stays the same — just the display name changes).

- [ ] **Step 4: Commit**

```
feat: rename SPD to STAMINA in character select UI
```

---

### Task 6: Add individual stat colors to USS

**Files:**
- Modify: `Assets/UI/Styles/CharacterSelect.uss`

- [ ] **Step 1: Read current stat-chip styles (around line 458-498)**
- [ ] **Step 2: Add per-stat color classes after the existing stat-chip styles**

```css
/* =============================================================================
   PER-STAT COLORS (AAA RPG standard)
   ============================================================================= */

.stat-chip-hp {
    border-left-color: rgb(204, 56, 56);
}
.stat-chip-hp .stat-chip-value {
    color: rgb(255, 85, 85);
    text-shadow: 0px 0px 6px rgba(204, 56, 56, 0.3);
}

.stat-chip-stamina {
    border-left-color: rgb(56, 168, 85);
}
.stat-chip-stamina .stat-chip-value {
    color: rgb(85, 221, 112);
    text-shadow: 0px 0px 6px rgba(56, 168, 85, 0.3);
}

.stat-chip-atk {
    border-left-color: rgb(204, 136, 32);
}
.stat-chip-atk .stat-chip-value {
    color: rgb(255, 170, 51);
    text-shadow: 0px 0px 6px rgba(204, 136, 32, 0.3);
}

.stat-chip-def {
    border-left-color: rgb(56, 120, 204);
}
.stat-chip-def .stat-chip-value {
    color: rgb(85, 170, 255);
    text-shadow: 0px 0px 6px rgba(56, 120, 204, 0.3);
}

/* Stat chip hover — glow in stat color */
.stat-chip:hover {
    background-color: rgba(0, 0, 0, 0.5);
    border-color: var(--border-subtle);
    translate: 0 -1px;
}
```

- [ ] **Step 3: Update CharacterSelect.uxml to add per-stat classes to each chip**

Add class `stat-chip-hp` to the HP chip, `stat-chip-atk` to ATK, `stat-chip-def` to DEF, `stat-chip-stamina` to STAMINA.

- [ ] **Step 4: Rearrange stat grid order: HP | STAMINA on top, ATK | DEF on bottom**

In `CharacterSelect.uxml`, reorder the stat chips inside `starter-stats-grid`:
1. HP (stat-chip-hp)
2. STAMINA (stat-chip-stamina)
3. ATK (stat-chip-atk)
4. DEF (stat-chip-def)

- [ ] **Step 5: Commit**

```
feat: add individual RPG colors to stat bars (HP=red, STAMINA=green, ATK=orange, DEF=blue)
```

---

### Task 7: Convert D&D attribute bars to number-only display

**Files:**
- Modify: `Assets/UI/Screens/CharacterSelect.uxml`
- Modify: `Assets/UI/Styles/CharacterSelect.uss`
- Modify: `Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs` (if it populates bar fills)

- [ ] **Step 1: Read the attribute-bars-section in CharacterSelect.uxml (lines 108-155)**
- [ ] **Step 2: Replace bar rows with number-only grid**

Replace the entire `attribute-bars` VisualElement with:

```xml
<ui:VisualElement name="attribute-bars-section" class="attribute-section">
    <ui:Label name="attribute-bars-header" class="section-header" text="ATTRIBUTES" />
    <ui:VisualElement name="attribute-grid" class="attribute-grid">
        <ui:VisualElement class="attr-chip">
            <ui:Label class="attr-chip-label" text="STR" />
            <ui:Label name="bar-str-value" class="attr-chip-value" text="14" />
        </ui:VisualElement>
        <ui:VisualElement class="attr-chip">
            <ui:Label class="attr-chip-label" text="DEX" />
            <ui:Label name="bar-dex-value" class="attr-chip-value" text="10" />
        </ui:VisualElement>
        <ui:VisualElement class="attr-chip">
            <ui:Label class="attr-chip-label" text="CON" />
            <ui:Label name="bar-con-value" class="attr-chip-value" text="14" />
        </ui:VisualElement>
        <ui:VisualElement class="attr-chip">
            <ui:Label class="attr-chip-label" text="INT" />
            <ui:Label name="bar-int-value" class="attr-chip-value" text="10" />
        </ui:VisualElement>
        <ui:VisualElement class="attr-chip">
            <ui:Label class="attr-chip-label" text="WIS" />
            <ui:Label name="bar-wis-value" class="attr-chip-value" text="12" />
        </ui:VisualElement>
        <ui:VisualElement class="attr-chip">
            <ui:Label class="attr-chip-label" text="CHA" />
            <ui:Label name="bar-cha-value" class="attr-chip-value" text="10" />
        </ui:VisualElement>
    </ui:VisualElement>
</ui:VisualElement>
```

- [ ] **Step 3: Add attribute grid USS styles**

```css
/* =============================================================================
   ATTRIBUTE GRID — Numbers only, no bars (D&D style)
   ============================================================================= */

.attribute-section {
    margin-top: 14px;
    padding-top: 12px;
    border-top-width: 1px;
    border-top-color: var(--border-subtle);
}

.attribute-grid {
    flex-direction: row;
    flex-wrap: wrap;
    justify-content: space-between;
}

.attr-chip {
    width: 15%;
    align-items: center;
    background-color: rgba(0, 0, 0, 0.25);
    border-width: 1px;
    border-color: rgba(80, 65, 100, 0.15);
    border-radius: 4px;
    padding: 6px 2px;
    margin-bottom: 4px;
    transition-property: background-color, border-color;
    transition-duration: 0.2s, 0.2s;
}

.attr-chip:hover {
    background-color: rgba(0, 0, 0, 0.4);
    border-color: rgba(80, 65, 100, 0.3);
}

.attr-chip-label {
    font-size: 8px;
    letter-spacing: 2px;
    -unity-font-style: bold;
    color: rgba(140, 130, 160, 0.5);
    margin-bottom: 2px;
    -unity-text-align: middle-center;
}

.attr-chip-value {
    font-size: 20px;
    -unity-font-style: bold;
    color: rgba(240, 235, 250, 0.85);
    -unity-text-align: middle-center;
}
```

- [ ] **Step 4: Remove old .stat-bar-row, .stat-bar-track, .stat-bar-fill, .stat-bar-label, .stat-bar-value styles from USS** (keep the .attribute-bars-section rename)

- [ ] **Step 5: Update any C# code that sets bar fill widths** — search for references to `bar-str-fill`, `bar-dex-fill`, etc. These elements no longer exist; only the `-value` labels remain. The CharacterSelectManager likely sets these in a method — remove the fill width logic, keep the value text setting.

- [ ] **Step 6: Commit**

```
feat: convert D&D attributes from bars to number-only display
```

---

## Chunk 3: Character Select — Panel Depth & Champion Redesign

### Task 8: Upgrade info panel visual depth in USS

**Files:**
- Modify: `Assets/UI/Styles/CharacterSelect.uss`

- [ ] **Step 1: Read current .veil-panel and .info-panel-container styles**
- [ ] **Step 2: Enhance .info-panel-container with layered depth**

Update the existing `.info-panel-container` rule:

```css
.info-panel-container {
    position: absolute;
    right: 2%;
    top: 5%;
    width: 42%;
    bottom: 20%;
    z-index: 20;
    padding: 18px 22px;
    overflow: hidden;
    flex-direction: column;
    /* Layered gradient background — NOT flat */
    background-color: rgba(14, 11, 22, 0.97);
    /* Ornate border with hero-color top accent */
    border-width: 1px;
    border-color: rgba(80, 65, 100, 0.25);
    border-top-width: 2px;
    border-top-color: var(--hero-primary, rgba(200, 160, 60, 0.5));
    border-radius: 6px 6px 4px 4px;
    /* Transitions for hero switch */
    transition-property: opacity, translate, border-color, background-color, border-top-color;
    transition-duration: 0.3s, 0.3s, 0.6s, 0.6s, 0.6s;
    transition-timing-function: ease-out, ease-out, ease-in-out, ease-in-out, ease-in-out;
}
```

Key changes: `bottom: 20%` instead of fixed height (panel extends to carousel area), `border-top-width: 2px` with hero-color.

- [ ] **Step 3: Add ornamental divider styles**

```css
/* =============================================================================
   ORNAMENTAL DIVIDERS
   ============================================================================= */

.ornamental-divider {
    height: 16px;
    margin: 6px 0;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
}

.divider-line {
    position: absolute;
    left: 0;
    right: 0;
    height: 1px;
    background-color: rgba(80, 65, 100, 0.3);
}

.divider-diamond {
    font-size: 6px;
    color: var(--hero-primary, rgba(200, 160, 60, 0.35));
    background-color: rgba(14, 11, 22, 0.98);
    padding: 0 8px;
    -unity-text-align: middle-center;
}
```

- [ ] **Step 4: Commit**

```
feat: upgrade info panel with layered depth and ornamental dividers
```

---

### Task 9: Redesign champion monster section — 50/50 split layout

**Files:**
- Modify: `Assets/UI/Screens/CharacterSelect.uxml`
- Modify: `Assets/UI/Styles/CharacterSelect.uss`
- Modify: `Assets/Scripts/UI/CharacterSelect/HeroDataPanelController.cs`

- [ ] **Step 1: Read current champion-section in UXML and its USS styles**
- [ ] **Step 2: Replace champion-section in UXML with 50/50 split layout**

```xml
<ui:VisualElement name="champion-section" class="champion-section-v2">
    <!-- Left: 3D Model Render Target (RenderTexture) -->
    <ui:VisualElement name="champion-model-viewer" class="champion-model-viewer" />
    <!-- Right: Info -->
    <ui:VisualElement name="champion-info-panel" class="champion-info-panel">
        <ui:Label name="champion-name" class="champion-name-v2" text="SKITTER-TEETH" />
        <ui:Label name="champion-brand-label" class="champion-brand-label" text="⬥ IRON BRAND" />
        <ui:VisualElement class="champion-tags">
            <ui:Label name="champion-brand" class="tag tag-brand" text="IRON" />
            <ui:Label name="champion-role" class="tag" text="TANK" />
        </ui:VisualElement>
        <ui:Label name="champion-desc" class="champion-desc" text="Your starting companion." />
    </ui:VisualElement>
</ui:VisualElement>
```

- [ ] **Step 3: Add champion v2 USS styles**

```css
/* =============================================================================
   CHAMPION MONSTER — 50/50 Split (model left, info right)
   ============================================================================= */

.champion-section-v2 {
    flex: 1;
    min-height: 100px;
    flex-direction: row;
    background-color: rgba(0, 0, 0, 0.25);
    border-width: 1px;
    border-color: var(--hero-primary, rgba(200, 160, 60, 0.15));
    border-radius: 6px;
    padding: 10px;
    margin-top: 8px;
    overflow: hidden;
}

.champion-model-viewer {
    width: 48%;
    background-color: rgba(0, 0, 0, 0.2);
    border-width: 1px;
    border-color: var(--hero-primary, rgba(200, 160, 60, 0.10));
    border-radius: 5px;
    -unity-background-scale-mode: scale-to-fit;
}

.champion-info-panel {
    flex: 1;
    padding-left: 12px;
    justify-content: center;
}

.champion-name-v2 {
    font-size: 16px;
    -unity-font-style: bold;
    color: var(--hero-accent, rgb(255, 210, 112));
    letter-spacing: 1px;
    margin-bottom: 4px;
}

.champion-brand-label {
    font-size: 12px;
    -unity-font-style: bold;
    letter-spacing: 3px;
    color: var(--hero-primary, rgba(200, 160, 60, 0.8));
    margin-bottom: 6px;
    padding-bottom: 4px;
    border-bottom-width: 1px;
    border-bottom-color: var(--hero-primary, rgba(200, 160, 60, 0.15));
}

.tag-brand {
    border-color: var(--hero-primary, rgba(200, 160, 60, 0.35));
    color: var(--hero-primary, rgba(200, 160, 60, 0.75));
    background-color: rgba(200, 160, 60, 0.06);
}

.champion-desc {
    font-size: 10px;
    color: rgba(180, 170, 200, 0.4);
    white-space: normal;
    margin-top: 8px;
}
```

- [ ] **Step 4: Update HeroDataPanelController to populate champion brand label and description**

Add cache references for `champion-brand-label` and `champion-desc`, populate them in `PopulateChampion()`.

- [ ] **Step 5: Remove old .champion-section styles from USS (keep .champion-tags and .tag)**
- [ ] **Step 6: Commit**

```
feat: redesign champion section with 50/50 split — model viewer + info panel
```

---

## Chunk 4: Character Select — Embark, Carousel, Bottom Bar

### Task 10: Restyle embark button with breathing glow and hold feedback

**Files:**
- Modify: `Assets/UI/Styles/CharacterSelect.uss`
- Modify: `Assets/Scripts/UI/CharacterSelect/HoldToEmbarkController.cs`

- [ ] **Step 1: Read current embark styles and HoldToEmbarkController**
- [ ] **Step 2: Update embark USS for gold gradient look + visible subtitle**

```css
.btn-embark {
    width: 240px;
    height: 54px;
    background-color: rgb(200, 160, 60);
    border-width: 2px;
    border-color: rgba(255, 215, 90, 0.55);
    border-top-color: rgba(255, 230, 140, 0.35);
    border-radius: 4px;
    overflow: visible;
    align-items: center;
    justify-content: center;
    transition-property: background-color, border-color, scale;
    transition-duration: 0.2s, 0.2s, 0.15s;
}

.btn-embark:hover {
    border-color: rgba(255, 230, 120, 0.70);
    scale: 1.04 1.04;
}

.btn-embark:active {
    background-color: rgb(240, 200, 80);
    border-color: rgba(255, 240, 150, 0.90);
    scale: 0.96 0.96;
}

.embark-text {
    font-size: 14px;
    -unity-font-style: bold;
    color: rgba(15, 10, 5, 0.90);
    letter-spacing: 4px;
}

.embark-subtitle {
    font-size: 9px;
    -unity-font-style: bold;
    color: rgba(15, 10, 5, 0.65);
    letter-spacing: 4px;
    margin-top: 2px;
}
```

- [ ] **Step 3: In HoldToEmbarkController, add C# code for embark glow breathing animation**

Use PrimeTween to pulse the embark button's border opacity/color on a loop while idle.

- [ ] **Step 4: Commit**

```
feat: restyle embark button with gold gradient and visible hold-to-confirm
```

---

### Task 11: Realign bottom bar — carousel cards + embark in same row

**Files:**
- Modify: `Assets/UI/Screens/CharacterSelect.uxml`
- Modify: `Assets/UI/Styles/CharacterSelect.uss`

- [ ] **Step 1: Read current bottom-layer and embark-area positioning**
- [ ] **Step 2: Restructure UXML to combine carousel and embark in one bottom bar**

Move the embark button INTO the bottom-layer area, creating a unified bottom bar:

```xml
<!-- BOTTOM BAR: Carousel + Embark aligned -->
<ui:VisualElement name="bottom-layer" class="bottom-bar">
    <ui:VisualElement name="carousel-section" class="carousel-section">
        <ui:Button name="btn-prev" class="nav-arrow nav-arrow-left" text="◀" />
        <ui:VisualElement name="carousel-container" class="carousel-container">
            <ui:VisualElement name="carousel-strip" class="carousel-strip" />
        </ui:VisualElement>
        <ui:Button name="btn-next" class="nav-arrow nav-arrow-right" text="▶" />
    </ui:VisualElement>

    <ui:VisualElement name="embark-area" class="embark-area-inline">
        <ui:Button name="btn-embark" class="btn-embark">
            <ui:VisualElement name="embark-hex-bg" class="embark-hex-bg" />
            <ui:VisualElement name="embark-glow" class="embark-glow" />
            <ui:Label name="embark-text" class="embark-text" text="EMBARK AS VEX" />
            <ui:Label name="embark-subtitle" class="embark-subtitle" text="HOLD TO CONFIRM" />
        </ui:Button>
        <ui:VisualElement name="embark-progress-ring" class="embark-progress-ring" />
    </ui:VisualElement>
</ui:VisualElement>

<!-- HERO INDEX moved to top -->
<ui:Label name="hero-index" class="hero-index-top" text="HERO 1 / 4" picking-mode="Ignore" />
```

- [ ] **Step 3: Add USS for unified bottom bar layout**

```css
.bottom-bar {
    position: absolute;
    bottom: 0;
    left: 0;
    right: 0;
    height: 16%;
    flex-direction: row;
    align-items: center;
    justify-content: center;
    padding: 10px 4% 8px;
    z-index: 30;
    background-color: rgba(0, 0, 0, 0.5);
}

.carousel-section {
    flex: 1;
    flex-direction: row;
    align-items: center;
    justify-content: center;
}

.embark-area-inline {
    margin-left: 24px;
    flex-shrink: 0;
    align-items: center;
}

.hero-index-top {
    position: absolute;
    top: 12px;
    left: 50%;
    translate: -50% 0;
    font-size: 10px;
    color: rgba(190, 180, 210, 0.30);
    letter-spacing: 5px;
    z-index: 30;
}
```

- [ ] **Step 4: Remove old .bottom-layer and .embark-area positioning styles**
- [ ] **Step 5: Verify C# code that queries btn-embark, embark-text, etc. still finds elements** (they haven't moved in the hierarchy significantly, just re-parented)
- [ ] **Step 6: Commit**

```
feat: align carousel cards and embark button in unified bottom bar
```

---

### Task 12: Increase hero card size to 80x100px

**Files:**
- Modify: `Assets/UI/Styles/CharacterSelect.uss`

- [ ] **Step 1: Update .hero-card dimensions and related styles**

```css
.hero-card {
    width: 80px;
    height: 100px;
    margin: 0 6px;
    /* rest of existing styles stay the same */
}

.hero-card-initial {
    font-size: 30px;
    /* rest stays */
}
```

- [ ] **Step 2: Commit**

```
feat: increase hero carousel cards to 80x100px
```

---

## Chunk 5: Character Select — Animations & Polish

### Task 13: Add stat number lerp animation on hero switch

**Files:**
- Create: `Assets/Scripts/UI/CharacterSelect/StatNumberAnimator.cs`
- Modify: `Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs`

- [ ] **Step 1: Create StatNumberAnimator.cs**

```csharp
using UnityEngine;
using UnityEngine.UIElements;
using PrimeTween;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Animates stat numbers (HP, ATK, DEF, STAMINA) with a counting/lerp effect
    /// when switching heroes. Each stat ticks independently at slightly different speeds.
    /// </summary>
    public static class StatNumberAnimator
    {
        /// <summary>
        /// Animates a label from its current displayed number to a new value.
        /// </summary>
        public static Tween AnimateValue(Label label, int fromValue, int toValue, float duration = 0.4f)
        {
            if (label == null) return default;
            if (fromValue == toValue) { label.text = toValue.ToString(); return default; }

            return Tween.Custom(fromValue, toValue, duration,
                onValueChange: val => { if (label != null) label.text = Mathf.RoundToInt(val).ToString(); },
                ease: Ease.OutQuad);
        }
    }
}
```

- [ ] **Step 2: In HeroDataPanelController, use StatNumberAnimator when setting stat values**

Instead of directly setting `_statHp.text = data.base_hp.ToString()`, call `StatNumberAnimator.AnimateValue(_statHp, _prevHp, data.base_hp)` and cache the previous values.

- [ ] **Step 3: Commit**

```
feat: add slot-machine style number animation on hero switch
```

---

### Task 14: Add embark voice line on confirm

**Files:**
- Modify: `Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs`
- Modify: `Assets/Scripts/Data/HeroDisplayConfig.cs`

- [ ] **Step 1: Read HeroDisplayConfig.cs to see current fields**
- [ ] **Step 2: Add an AudioClip field for embark voice line**

```csharp
[Header("Audio")]
public AudioClip embarkVoiceLine;
```

- [ ] **Step 3: In CharacterSelectManager.TriggerEmbark(), play the voice line**

```csharp
if (CurrentConfig?.embarkVoiceLine != null)
{
    AudioSource.PlayClipAtPoint(CurrentConfig.embarkVoiceLine, Camera.main.transform.position, 0.8f);
}
```

- [ ] **Step 4: Commit**

```
feat: play character voice line on embark confirm
```

---

### Task 15: Remove champion model from 3D stage

**Files:**
- Modify: `Assets/Scripts/UI/CharacterSelect/HeroStageController.cs`

- [ ] **Step 1: Read the SwapHeroModel coroutine (around line 225-277)**
- [ ] **Step 2: Comment out or remove the champion model instantiation block**

Remove/comment the section that instantiates `config.championModelPrefab` on the stage (lines ~262-268). The champion model now lives in the panel's RenderTexture viewer, not on the hero stage.

- [ ] **Step 3: Commit**

```
feat: remove champion from hero stage — hero gets full spotlight
```

---

### Task 16: Add ornamental dividers to UXML

**Files:**
- Modify: `Assets/UI/Screens/CharacterSelect.uxml`

- [ ] **Step 1: Insert ornamental divider elements between major sections**

Add between hero-quote and hero-class-info, between starter-stats-grid and attribute-section, and between attribute-section and champion-section:

```xml
<ui:VisualElement class="ornamental-divider">
    <ui:VisualElement class="divider-line" />
    <ui:Label class="divider-diamond" text="◆" />
</ui:VisualElement>
```

- [ ] **Step 2: Commit**

```
feat: add ornamental diamond dividers between panel sections
```

---

### Task 17: Final compile check and visual verification

**Files:** None (verification only)

- [ ] **Step 1: Run compile check via unity_qa**

```
mcp__vb-unity__unity_qa action=check_compile_status
```

- [ ] **Step 2: Take screenshot of main menu**

```
mcp__vb-unity__unity_editor action=screenshot
```

- [ ] **Step 3: Take screenshot of character select**

Load CharacterSelect scene and screenshot.

- [ ] **Step 4: Compare screenshots against V5 mockup, note any discrepancies**
- [ ] **Step 5: Commit all remaining changes**

```
feat: AAA UI overhaul — main menu + character select visual upgrade
```
