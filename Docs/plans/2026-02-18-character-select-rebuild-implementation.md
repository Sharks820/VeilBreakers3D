# Character Select Screen Rebuild - Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Complete deletion and rebuild of the character selection screen with sub-controller architecture, ScriptableObject configuration, and AAA visual presentation.

**Architecture:** 8 focused sub-controllers orchestrated by a central manager via events. HeroDisplayConfig ScriptableObjects drive per-hero visuals with zero code changes to add heroes. UI Toolkit (UXML + USS) with CSS variable theming and GPU-accelerated transitions.

**Tech Stack:** Unity 6, UI Toolkit (UXML/USS), RenderTexture pipeline, AnimatorOverrideController, ScriptableObjects, C# events

**Design Document:** `docs/plans/2026-02-18-character-select-rebuild-design.md`

---

## Pre-Implementation Notes

### Files to PRESERVE (DO NOT TOUCH)
- `Assets/Resources/Data/heroes.json` -- hero data (4 heroes: vex, seraphina, orion, nyx)
- `Assets/Scripts/Data/HeroData.cs` -- data model with BaseStats, ColorData, LearnableSkillEntry
- `Assets/Scripts/Core/GameDatabase.cs` -- singleton data provider (`GameDatabase.Instance.Heroes`)
- `Assets/Scripts/Data/Enums.cs` -- Brand, Path, HeroRole, ResourceType enums

### Key Data Patterns
- `GameDatabase.Instance.Heroes` returns `IReadOnlyDictionary<string, HeroData>`
- `GameDatabase.Instance.GetHero("vex")` returns `HeroData`
- `GameDatabase.Instance.GetAllHeroes()` returns `List<HeroData>`
- `GameDatabase.Instance.IsReady` -- true when data loaded successfully
- Hero IDs: `"vex"`, `"seraphina"`, `"orion"`, `"nyx"`
- HeroData fields: `hero_id`, `display_name`, `title`, `quote`, `role`, `resource_type`, `primary_brand`, `primary_path`, `starter_monster_id`, `base_hp/attack/defense/speed`, `base_stats.strength/dexterity/constitution/intelligence/wisdom/charisma`, `innate_skills[]`, `color_palette`

### Code Style (VeilBreakers)
```csharp
namespace VeilBreakers.[Category]
{
    public class Example : MonoBehaviour
    {
        private const int kMaxValue = 10;      // Constants: k prefix
        [SerializeField] private int _value;   // Private: _ prefix
        public int Value => _value;            // Properties: PascalCase
        public event Action<int> OnChanged;    // Events: On prefix
    }
}
```

### Existing Integration Points
- `ScreenTransition.Instance.Transition(Action callback)` -- fade transition
- `SaveManager.Instance.GetBestNewGameSlotAsync()` -- find save slot
- `SaveManager.Instance.CreateNewSaveAsync(slot, heroId, heroName, path)` -- create save
- `SaveManager.Instance.SetCurrentLocation("StarterTown")` -- set spawn
- `SaveManager.Instance.SaveAsync(slot)` -- persist save
- Target scene on embark: `"Overworld"`

### Branch Strategy
- `feature/character-select-rebuild` -- main feature branch (created from master)
- `feature/cs-phase-N` -- per-phase branches (merged into feature branch)

---

## Phase 1: Data Foundation

> Creates the ScriptableObject configuration and shared event definitions.
> These are the bedrock -- everything else depends on them.

### Task 1.1: Create HeroDisplayConfig ScriptableObject

**Files:**
- Create: `Assets/Scripts/Data/HeroDisplayConfig.cs`

**Step 1: Create the ScriptableObject**

```csharp
using System;
using UnityEngine;

namespace VeilBreakers.Data
{
    [CreateAssetMenu(fileName = "NewHeroDisplayConfig", menuName = "VeilBreakers/Hero Display Config")]
    public class HeroDisplayConfig : ScriptableObject
    {
        // =============================================================================
        // IDENTITY
        // =============================================================================

        [Header("Identity")]
        [Tooltip("Must match hero_id in heroes.json")]
        public string heroId;

        // =============================================================================
        // CAMERA
        // =============================================================================

        [Header("Camera")]
        public Vector3 cameraOffset = new Vector3(0f, 1.2f, -3f);
        [Range(15f, 60f)]
        public float cameraFOV = 30f;
        [Range(0f, 0.5f)]
        public float cameraFramePadding = 0.15f;

        // =============================================================================
        // LIGHTING
        // =============================================================================

        [Header("Lighting - Key")]
        public Color keyLightColor = Color.white;
        [Range(0f, 3f)]
        public float keyLightIntensity = 1.2f;

        [Header("Lighting - Fill")]
        public Color fillLightColor = new Color(0.4f, 0.5f, 0.6f);
        [Range(0f, 2f)]
        public float fillLightIntensity = 0.6f;

        [Header("Lighting - Rim")]
        public Color rimLightColor = Color.cyan;
        [Range(0f, 3f)]
        public float rimLightIntensity = 1.5f;

        // =============================================================================
        // THEME COLORS
        // =============================================================================

        [Header("Theme Colors")]
        [Tooltip("Main brand color (panels, stat bars, particles)")]
        public Color primaryColor;
        [Tooltip("Supporting color (backgrounds, fills)")]
        public Color secondaryColor;
        [Tooltip("Highlights, glows, accents")]
        public Color accentColor;

        // =============================================================================
        // MODEL
        // =============================================================================

        [Header("Model")]
        [Tooltip("null = use brand-colored placeholder capsule")]
        public GameObject modelPrefab;

        // =============================================================================
        // ANIMATIONS
        // =============================================================================

        [Header("Animations")]
        public AnimationClip idleClip;
        [Tooltip("Random selection pool for idle variety")]
        public AnimationClip[] idleVariantClips;
        public AnimationClip selectedClip;
        public AnimationClip showcaseClip;
        public AnimationClip embarkClip;

        [Header("Animation Timing")]
        [Range(5f, 30f)]
        public float idleVariantMinDelay = 10f;
        [Range(5f, 30f)]
        public float idleVariantMaxDelay = 16f;
        [Range(0.5f, 5f)]
        public float selectedToShowcaseDelay = 2f;
        [Range(1f, 8f)]
        public float showcaseToIdleDelay = 4f;
        [Range(0.05f, 1f)]
        public float crossfadeDuration = 0.25f;

        // =============================================================================
        // AUDIO
        // =============================================================================

        [Header("Audio")]
        [Tooltip("Played when hero is selected in carousel")]
        public AudioClip selectionSFX;
        [Tooltip("Played on embark confirmation")]
        public AudioClip embarkSFX;
        [Tooltip("Background atmosphere loop per hero")]
        public AudioClip ambientLoop;

        // =============================================================================
        // CHAMPION MONSTER
        // =============================================================================

        [Header("Champion Monster")]
        [Tooltip("null = data-only display in left panel")]
        public GameObject championModelPrefab;
        public Vector3 championOffset = new Vector3(0.5f, 0f, 0.3f);
        [Range(0.1f, 1f)]
        public float championScale = 0.35f;
        public AnimationClip championIdleClip;

        // =============================================================================
        // VFX
        // =============================================================================

        [Header("VFX")]
        public Color particleColor;
        [Tooltip("null = skip selection VFX")]
        public GameObject selectionVFXPrefab;
    }
}
```

**Step 2: Verify Unity compilation**

Run: Open Unity Editor, wait for domain reload, check Console for errors.
Expected: Zero compilation errors. `HeroDisplayConfig` appears in Create Asset menu.

**Step 3: Commit**

```bash
git checkout -b feature/character-select-rebuild
git checkout -b feature/cs-phase-1
git add Assets/Scripts/Data/HeroDisplayConfig.cs
git add Assets/Scripts/Data/HeroDisplayConfig.cs.meta
git commit -m "feat(charselect): add HeroDisplayConfig ScriptableObject

Data foundation for character select rebuild. Each hero gets a
ScriptableObject defining camera, lighting, theme colors, animations,
audio, champion monster, and VFX configuration. Adding a new hero
requires zero code changes."
```

---

### Task 1.2: Create CharSelectEvents Shared Event Definitions

**Files:**
- Create: `Assets/Scripts/UI/CharacterSelect/CharSelectEvents.cs`

**Step 1: Create the events file**

```csharp
using System;
using VeilBreakers.Data;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Shared event definitions for the character select screen.
    /// All events are raised by CharacterSelectManager.
    /// Controllers subscribe in OnEnable, unsubscribe in OnDisable.
    /// </summary>
    public static class CharSelectEvents
    {
        /// <summary>Hero index changed. Args: index, HeroData, HeroDisplayConfig</summary>
        public static event Action<int, HeroData, HeroDisplayConfig> OnHeroChanged;

        /// <summary>Hero JSON data finished loading for current hero.</summary>
        public static event Action<HeroData> OnHeroDataLoaded;

        /// <summary>A hero has been actively selected (embark button should start breathing).</summary>
        public static event Action OnHeroSelected;

        /// <summary>Embark button was clicked -- show confirmation popup.</summary>
        public static event Action OnEmbarkRequested;

        /// <summary>Player confirmed embark -- proceed to gameplay.</summary>
        public static event Action OnEmbarkConfirmed;

        /// <summary>Player cancelled embark -- dismiss popup, return to browsing.</summary>
        public static event Action OnEmbarkCancelled;

        /// <summary>Screen is fully initialized and ready for interaction.</summary>
        public static event Action OnScreenReady;

        /// <summary>Screen is about to exit (transition starting).</summary>
        public static event Action OnScreenExiting;

        // =========================================================================
        // INVOCATION HELPERS (null-safe)
        // =========================================================================

        public static void RaiseHeroChanged(int index, HeroData data, HeroDisplayConfig config)
        {
            OnHeroChanged?.Invoke(index, data, config);
        }

        public static void RaiseHeroDataLoaded(HeroData data)
        {
            OnHeroDataLoaded?.Invoke(data);
        }

        public static void RaiseHeroSelected() => OnHeroSelected?.Invoke();
        public static void RaiseEmbarkRequested() => OnEmbarkRequested?.Invoke();
        public static void RaiseEmbarkConfirmed() => OnEmbarkConfirmed?.Invoke();
        public static void RaiseEmbarkCancelled() => OnEmbarkCancelled?.Invoke();
        public static void RaiseScreenReady() => OnScreenReady?.Invoke();
        public static void RaiseScreenExiting() => OnScreenExiting?.Invoke();

        /// <summary>
        /// Clears ALL event subscribers. Call on scene unload to prevent leaks.
        /// </summary>
        public static void ClearAll()
        {
            OnHeroChanged = null;
            OnHeroDataLoaded = null;
            OnHeroSelected = null;
            OnEmbarkRequested = null;
            OnEmbarkConfirmed = null;
            OnEmbarkCancelled = null;
            OnScreenReady = null;
            OnScreenExiting = null;
        }
    }
}
```

**Step 2: Verify Unity compilation**

Run: Unity domain reload. Check Console.
Expected: Zero errors.

**Step 3: Commit**

```bash
git add Assets/Scripts/UI/CharacterSelect/CharSelectEvents.cs
git add Assets/Scripts/UI/CharacterSelect/CharSelectEvents.cs.meta
git commit -m "feat(charselect): add CharSelectEvents static event bus

Null-safe event invocation helpers. ClearAll() prevents leaks on
scene unload. All controllers subscribe in OnEnable, unsubscribe
in OnDisable."
```

---

### Task 1.3: Phase 1 Review & Merge

**Step 1: Opus code review**

Review `HeroDisplayConfig.cs` and `CharSelectEvents.cs` for:
- SOLID principles compliance
- Correct namespace (`VeilBreakers.Data` and `VeilBreakers.UI.CharacterSelect`)
- Code style (k prefix constants, _ prefix privates, PascalCase properties)
- No security issues, no unsafe patterns
- Event lifecycle safety (ClearAll method exists)

**Step 2: CLI reviewer validation**

Run Gemini, Codex, and Kimi against both files. At least 2/3 must approve.

**Step 3: Memory saves**

- Save to episodic memory: "Phase 1 complete - HeroDisplayConfig SO + CharSelectEvents"
- Save to AIM memory: phase 1 completion details
- Save to Serena memory: implementation notes

**Step 4: Merge**

```bash
git checkout feature/character-select-rebuild
git merge feature/cs-phase-1 --no-ff -m "merge: phase 1 - data foundation (HeroDisplayConfig + CharSelectEvents)"
```

---

## Phase 2: UI Skeleton

> Creates the UXML structure and USS styling.
> The visual foundation that all controllers will populate.

### Task 2.1: Create CharacterSelect.uxml

**Files:**
- Create: `Assets/UI/Screens/CharacterSelect.uxml`

**Step 1: Create the UXML document**

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements"
          editor-extension-mode="False">

    <!-- ROOT: Full-screen container -->
    <ui:VisualElement name="character-select-root" class="character-select-root">

        <!-- BACKGROUND LAYER -->
        <ui:VisualElement name="background-layer" class="background-layer">
            <ui:VisualElement name="background-gradient" class="background-gradient" />
            <ui:VisualElement name="background-vignette" class="background-vignette" />
            <ui:VisualElement name="background-particles" class="background-particles" />
        </ui:VisualElement>

        <!-- MAIN CONTENT LAYER -->
        <ui:VisualElement name="content-layer" class="content-layer">

            <!-- LEFT PANEL: Hero Info -->
            <ui:VisualElement name="hero-info-panel" class="glass-panel hero-info-panel">
                <ui:Label name="hero-name" class="hero-name" text="VEX" />
                <ui:Label name="hero-title" class="hero-title" text="THE WARDEN" />
                <ui:Label name="hero-quote" class="hero-quote" text="&quot;I hold the line.&quot;" />

                <ui:VisualElement name="hero-class-info" class="hero-class-info">
                    <ui:VisualElement name="path-row" class="info-row">
                        <ui:Label class="info-label" text="PATH" />
                        <ui:Label name="hero-path" class="info-value" text="IRONBOUND" />
                    </ui:VisualElement>
                    <ui:VisualElement name="role-row" class="info-row">
                        <ui:Label class="info-label" text="ROLE" />
                        <ui:Label name="hero-role" class="info-value" text="TANK" />
                    </ui:VisualElement>
                    <ui:VisualElement name="resource-row" class="info-row">
                        <ui:Label class="info-label" text="RESOURCE" />
                        <ui:Label name="hero-resource" class="info-value" text="GUARD" />
                    </ui:VisualElement>
                </ui:VisualElement>

                <!-- Starter Stats 2x2 Grid -->
                <ui:VisualElement name="starter-stats-grid" class="starter-stats-grid">
                    <ui:VisualElement class="stat-chip">
                        <ui:Label class="stat-chip-label" text="HP" />
                        <ui:Label name="stat-hp" class="stat-chip-value" text="68" />
                    </ui:VisualElement>
                    <ui:VisualElement class="stat-chip">
                        <ui:Label class="stat-chip-label" text="ATK" />
                        <ui:Label name="stat-atk" class="stat-chip-value" text="10" />
                    </ui:VisualElement>
                    <ui:VisualElement class="stat-chip">
                        <ui:Label class="stat-chip-label" text="DEF" />
                        <ui:Label name="stat-def" class="stat-chip-value" text="20" />
                    </ui:VisualElement>
                    <ui:VisualElement class="stat-chip">
                        <ui:Label class="stat-chip-label" text="SPD" />
                        <ui:Label name="stat-spd" class="stat-chip-value" text="5" />
                    </ui:VisualElement>
                </ui:VisualElement>

                <!-- Champion Monster Section -->
                <ui:VisualElement name="champion-section" class="champion-section">
                    <ui:Label class="section-header" text="CHAMPION MONSTER" />
                    <ui:Label name="champion-name" class="champion-name" text="Skitter-Teeth" />
                    <ui:VisualElement class="champion-tags">
                        <ui:Label name="champion-brand" class="tag" text="IRON" />
                        <ui:Label name="champion-role" class="tag" text="TANK" />
                    </ui:VisualElement>
                </ui:VisualElement>
            </ui:VisualElement>

            <!-- CENTER: 3D Hero Stage (RenderTexture target) -->
            <ui:VisualElement name="hero-stage" class="hero-stage">
                <ui:VisualElement name="hero-render-target" class="hero-render-target" />
                <ui:Label name="drag-hint" class="drag-hint" text="DRAG TO ROTATE" />
            </ui:VisualElement>

            <!-- RIGHT PANEL: Attributes & Abilities -->
            <ui:VisualElement name="stats-panel" class="glass-panel stats-panel">
                <ui:Label class="section-header" text="ATTRIBUTES" />

                <ui:VisualElement name="attribute-bars" class="attribute-bars">
                    <!-- 6 stat bars: STR, DEX, CON, INT, WIS, CHA -->
                    <ui:VisualElement name="bar-str" class="stat-bar-row">
                        <ui:Label class="stat-bar-label" text="STR" />
                        <ui:VisualElement class="stat-bar-track">
                            <ui:VisualElement name="bar-str-fill" class="stat-bar-fill" />
                        </ui:VisualElement>
                        <ui:Label name="bar-str-value" class="stat-bar-value" text="14" />
                    </ui:VisualElement>
                    <ui:VisualElement name="bar-dex" class="stat-bar-row">
                        <ui:Label class="stat-bar-label" text="DEX" />
                        <ui:VisualElement class="stat-bar-track">
                            <ui:VisualElement name="bar-dex-fill" class="stat-bar-fill" />
                        </ui:VisualElement>
                        <ui:Label name="bar-dex-value" class="stat-bar-value" text="10" />
                    </ui:VisualElement>
                    <ui:VisualElement name="bar-con" class="stat-bar-row">
                        <ui:Label class="stat-bar-label" text="CON" />
                        <ui:VisualElement class="stat-bar-track">
                            <ui:VisualElement name="bar-con-fill" class="stat-bar-fill" />
                        </ui:VisualElement>
                        <ui:Label name="bar-con-value" class="stat-bar-value" text="14" />
                    </ui:VisualElement>
                    <ui:VisualElement name="bar-int" class="stat-bar-row">
                        <ui:Label class="stat-bar-label" text="INT" />
                        <ui:VisualElement class="stat-bar-track">
                            <ui:VisualElement name="bar-int-fill" class="stat-bar-fill" />
                        </ui:VisualElement>
                        <ui:Label name="bar-int-value" class="stat-bar-value" text="10" />
                    </ui:VisualElement>
                    <ui:VisualElement name="bar-wis" class="stat-bar-row">
                        <ui:Label class="stat-bar-label" text="WIS" />
                        <ui:VisualElement class="stat-bar-track">
                            <ui:VisualElement name="bar-wis-fill" class="stat-bar-fill" />
                        </ui:VisualElement>
                        <ui:Label name="bar-wis-value" class="stat-bar-value" text="12" />
                    </ui:VisualElement>
                    <ui:VisualElement name="bar-cha" class="stat-bar-row">
                        <ui:Label class="stat-bar-label" text="CHA" />
                        <ui:VisualElement class="stat-bar-track">
                            <ui:VisualElement name="bar-cha-fill" class="stat-bar-fill" />
                        </ui:VisualElement>
                        <ui:Label name="bar-cha-value" class="stat-bar-value" text="10" />
                    </ui:VisualElement>
                </ui:VisualElement>

                <ui:Label class="section-header abilities-header" text="ABILITIES" />

                <ui:VisualElement name="abilities-list" class="abilities-list">
                    <ui:Label name="ability-0" class="ability-slot" text="Shield Wall" />
                    <ui:Label name="ability-1" class="ability-slot" text="Iron Guard" />
                    <ui:Label name="ability-2" class="ability-slot" text="Veil Strike" />
                    <ui:Label name="ability-3" class="ability-slot" text="Fortify" />
                    <ui:Label name="ability-4" class="ability-slot" text="Last Stand" />
                </ui:VisualElement>
            </ui:VisualElement>

        </ui:VisualElement>

        <!-- BOTTOM LAYER: Carousel + Embark -->
        <ui:VisualElement name="bottom-layer" class="bottom-layer">

            <!-- Navigation: Prev Arrow -->
            <ui:Button name="btn-prev" class="nav-arrow nav-arrow-left" text="&#x276E;" />

            <!-- Hero Carousel -->
            <ui:VisualElement name="carousel-container" class="carousel-container">
                <ui:VisualElement name="carousel-strip" class="carousel-strip">
                    <!-- Slots generated dynamically by CarouselController -->
                </ui:VisualElement>
            </ui:VisualElement>

            <!-- Navigation: Next Arrow -->
            <ui:Button name="btn-next" class="nav-arrow nav-arrow-right" text="&#x276F;" />

            <!-- Hero Index -->
            <ui:Label name="hero-index" class="hero-index" text="HERO 1 / 4" />

        </ui:VisualElement>

        <!-- EMBARK LAYER -->
        <ui:VisualElement name="embark-layer" class="embark-layer">
            <ui:Button name="btn-back" class="btn-back" text="&#x276E; BACK" />

            <ui:Button name="btn-embark" class="btn-embark">
                <ui:VisualElement name="embark-glow" class="embark-glow" />
                <ui:Label name="embark-text" class="embark-text" text="EMBARK AS VEX" />
                <ui:Label class="embark-subtitle" text="Begin Your Journey" />
            </ui:Button>
        </ui:VisualElement>

        <!-- CONFIRM POPUP OVERLAY (hidden by default) -->
        <ui:VisualElement name="confirm-overlay" class="confirm-overlay hidden">
            <ui:VisualElement name="confirm-dim" class="confirm-dim" />
            <ui:VisualElement name="confirm-popup" class="glass-panel confirm-popup">
                <ui:Label class="confirm-title" text="CONFIRM YOUR CHAMPION" />
                <ui:Label name="confirm-description" class="confirm-description"
                          text="You will begin your journey as VEX, THE WARDEN" />
                <ui:VisualElement class="confirm-buttons">
                    <ui:Button name="btn-confirm" class="btn-confirm" text="CONFIRM" />
                    <ui:Button name="btn-cancel" class="btn-cancel" text="CANCEL" />
                </ui:VisualElement>
            </ui:VisualElement>
        </ui:VisualElement>

    </ui:VisualElement>
</ui:UXML>
```

**Step 2: Verify Unity compilation**

Run: Unity domain reload. Open UXML in UI Builder.
Expected: Document loads without errors. Element tree visible in UI Builder.

**Step 3: Commit**

```bash
git checkout -b feature/cs-phase-2
git add Assets/UI/Screens/CharacterSelect.uxml
git add Assets/UI/Screens/CharacterSelect.uxml.meta
git commit -m "feat(charselect): add UXML skeleton with all named elements

Complete UI structure: hero info panel, 3D stage, stats panel,
carousel strip, embark button with glow, confirm popup overlay.
All elements named for C# Q<T>() queries."
```

---

### Task 2.2: Create CharacterSelect.uss

**Files:**
- Create: `Assets/UI/Styles/CharacterSelect.uss`

**Step 1: Create the USS stylesheet**

```css
/* =============================================================================
   CHARACTER SELECT - MASTER STYLESHEET
   VeilBreakers 3D - "The Veil Tear Showcase"
   ============================================================================= */

/* =============================================================================
   CSS VARIABLES / THEME SYSTEM
   Per-hero theme classes set on root. USS transitions handle crossfade.
   ============================================================================= */

.theme-vex {
    --hero-primary: rgb(200, 60, 60);
    --hero-secondary: rgb(255, 100, 80);
    --hero-accent: rgb(255, 180, 120);
}

.theme-seraphina {
    --hero-primary: rgb(60, 180, 60);
    --hero-secondary: rgb(120, 80, 200);
    --hero-accent: rgb(220, 200, 60);
}

.theme-orion {
    --hero-primary: rgb(60, 60, 200);
    --hero-secondary: rgb(80, 120, 255);
    --hero-accent: rgb(60, 220, 255);
}

.theme-nyx {
    --hero-primary: rgb(180, 30, 50);
    --hero-secondary: rgb(40, 10, 20);
    --hero-accent: rgb(160, 60, 200);
}

/* =============================================================================
   ROOT / LAYOUT
   ============================================================================= */

.character-select-root {
    width: 100%;
    height: 100%;
    flex-direction: column;
    -unity-overflow: hidden;
}

.background-layer {
    position: absolute;
    width: 100%;
    height: 100%;
}

.background-gradient {
    position: absolute;
    width: 100%;
    height: 100%;
    background-color: rgb(8, 8, 12);
}

.background-vignette {
    position: absolute;
    width: 100%;
    height: 100%;
    opacity: 0.6;
}

.background-particles {
    position: absolute;
    width: 100%;
    height: 100%;
}

.content-layer {
    flex-grow: 1;
    flex-direction: row;
    padding: 24px;
    padding-bottom: 0;
}

/* =============================================================================
   GLASS PANEL BASE
   ============================================================================= */

.glass-panel {
    background-color: rgba(10, 10, 15, 0.88);
    border-width: 1px;
    border-color: var(--hero-accent, rgba(120, 120, 140, 0.3));
    border-radius: 4px;
    transition-property: border-color;
    transition-duration: 0.6s;
    transition-timing-function: ease-out;
}

/* =============================================================================
   LEFT PANEL: HERO INFO
   ============================================================================= */

.hero-info-panel {
    width: 22%;
    min-width: 200px;
    max-width: 320px;
    padding: 20px;
    flex-direction: column;
    translate: 0 0;
    opacity: 1;
    transition-property: translate, opacity, border-color;
    transition-duration: 0.4s, 0.3s, 0.6s;
    transition-timing-function: ease-out;
}

.hero-info-panel.panel-hidden {
    translate: -40px 0;
    opacity: 0;
}

.hero-name {
    font-size: 28px;
    color: var(--hero-primary, rgb(220, 220, 220));
    -unity-font-style: bold;
    letter-spacing: 4px;
    margin-bottom: 2px;
    transition-property: color;
    transition-duration: 0.6s;
}

.hero-title {
    font-size: 13px;
    color: var(--hero-accent, rgb(180, 180, 190));
    letter-spacing: 2px;
    margin-bottom: 12px;
    transition-property: color;
    transition-duration: 0.6s;
}

.hero-quote {
    font-size: 11px;
    color: rgba(200, 200, 210, 0.7);
    -unity-font-style: italic;
    margin-bottom: 16px;
    white-space: normal;
}

.hero-class-info {
    margin-bottom: 16px;
}

.info-row {
    flex-direction: row;
    justify-content: space-between;
    margin-bottom: 4px;
}

.info-label {
    font-size: 10px;
    color: rgba(160, 160, 170, 0.7);
    letter-spacing: 2px;
}

.info-value {
    font-size: 12px;
    color: var(--hero-accent, rgb(200, 200, 210));
    -unity-font-style: bold;
    letter-spacing: 1px;
    transition-property: color;
    transition-duration: 0.6s;
}

/* Starter Stats 2x2 Grid */
.starter-stats-grid {
    flex-direction: row;
    flex-wrap: wrap;
    margin-bottom: 16px;
}

.stat-chip {
    width: 48%;
    flex-direction: row;
    justify-content: space-between;
    padding: 6px 8px;
    margin: 1%;
    background-color: rgba(30, 30, 40, 0.6);
    border-radius: 3px;
    border-width: 1px;
    border-color: rgba(80, 80, 100, 0.2);
}

.stat-chip-label {
    font-size: 10px;
    color: rgba(160, 160, 170, 0.7);
    letter-spacing: 1px;
}

.stat-chip-value {
    font-size: 13px;
    color: rgb(220, 220, 230);
    -unity-font-style: bold;
}

/* Champion Monster */
.champion-section {
    padding-top: 12px;
    border-top-width: 1px;
    border-top-color: rgba(80, 80, 100, 0.2);
}

.section-header {
    font-size: 10px;
    color: rgba(160, 160, 170, 0.5);
    letter-spacing: 3px;
    margin-bottom: 8px;
}

.champion-name {
    font-size: 14px;
    color: var(--hero-primary, rgb(200, 200, 210));
    -unity-font-style: bold;
    margin-bottom: 4px;
    transition-property: color;
    transition-duration: 0.6s;
}

.champion-tags {
    flex-direction: row;
}

.tag {
    font-size: 9px;
    color: rgba(200, 200, 210, 0.8);
    background-color: rgba(40, 40, 55, 0.8);
    padding: 2px 8px;
    margin-right: 6px;
    border-radius: 2px;
    letter-spacing: 1px;
}

/* =============================================================================
   CENTER: HERO STAGE
   ============================================================================= */

.hero-stage {
    flex-grow: 1;
    margin: 0 16px;
    align-items: center;
    justify-content: center;
}

.hero-render-target {
    width: 100%;
    height: 100%;
    -unity-background-scale-mode: scale-to-fit;
}

.drag-hint {
    position: absolute;
    bottom: 8px;
    font-size: 9px;
    color: rgba(160, 160, 170, 0.3);
    letter-spacing: 2px;
}

/* =============================================================================
   RIGHT PANEL: STATS & ABILITIES
   ============================================================================= */

.stats-panel {
    width: 22%;
    min-width: 200px;
    max-width: 300px;
    padding: 20px;
    flex-direction: column;
    translate: 0 0;
    opacity: 1;
    transition-property: translate, opacity, border-color;
    transition-duration: 0.4s, 0.3s, 0.6s;
    transition-timing-function: ease-out;
}

.stats-panel.panel-hidden {
    translate: 40px 0;
    opacity: 0;
}

.attribute-bars {
    margin-bottom: 16px;
}

.stat-bar-row {
    flex-direction: row;
    align-items: center;
    margin-bottom: 6px;
}

.stat-bar-label {
    width: 32px;
    font-size: 10px;
    color: rgba(160, 160, 170, 0.7);
    letter-spacing: 2px;
}

.stat-bar-track {
    flex-grow: 1;
    height: 8px;
    background-color: rgba(30, 30, 40, 0.8);
    border-radius: 4px;
    margin: 0 8px;
    overflow: hidden;
}

.stat-bar-fill {
    height: 100%;
    width: 50%;
    background-color: var(--hero-primary, rgb(100, 100, 120));
    border-radius: 4px;
    scale: 1 1;
    transform-origin: left center;
    transition-property: width, background-color;
    transition-duration: 0.6s;
    transition-timing-function: ease-out-cubic;
}

.stat-bar-value {
    width: 24px;
    font-size: 11px;
    color: rgb(200, 200, 210);
    -unity-text-align: middle-right;
}

.abilities-header {
    margin-top: 8px;
    padding-top: 12px;
    border-top-width: 1px;
    border-top-color: rgba(80, 80, 100, 0.2);
}

.abilities-list {
    flex-direction: column;
}

.ability-slot {
    font-size: 12px;
    color: rgba(200, 200, 210, 0.9);
    padding: 6px 8px;
    margin-bottom: 3px;
    background-color: rgba(30, 30, 40, 0.4);
    border-radius: 2px;
    border-left-width: 2px;
    border-left-color: var(--hero-primary, rgba(100, 100, 120, 0.5));
    transition-property: border-left-color;
    transition-duration: 0.6s;
}

/* =============================================================================
   BOTTOM LAYER: CAROUSEL + NAVIGATION
   ============================================================================= */

.bottom-layer {
    flex-direction: row;
    align-items: center;
    justify-content: center;
    padding: 12px 24px;
    height: 120px;
}

.nav-arrow {
    width: 40px;
    height: 40px;
    font-size: 20px;
    color: rgba(200, 200, 210, 0.6);
    background-color: rgba(20, 20, 30, 0.6);
    border-width: 1px;
    border-color: rgba(80, 80, 100, 0.3);
    border-radius: 20px;
    -unity-text-align: middle-center;
    transition-property: color, border-color, background-color;
    transition-duration: 0.2s;
}

.nav-arrow:hover {
    color: rgb(255, 255, 255);
    border-color: var(--hero-accent, rgba(160, 160, 180, 0.6));
    background-color: rgba(40, 40, 55, 0.8);
}

.carousel-container {
    margin: 0 16px;
}

.carousel-strip {
    flex-direction: row;
    align-items: center;
}

.hero-card {
    width: 80px;
    height: 100px;
    margin: 0 6px;
    background-color: rgba(20, 20, 30, 0.7);
    border-width: 2px;
    border-color: rgba(60, 60, 80, 0.4);
    border-radius: 4px;
    align-items: center;
    justify-content: center;
    scale: 1;
    opacity: 0.7;
    transition-property: scale, opacity, border-color, background-color;
    transition-duration: 0.25s;
    transition-timing-function: ease-out;
}

.hero-card:hover {
    opacity: 0.9;
    border-color: rgba(140, 140, 160, 0.6);
}

.hero-card.selected {
    scale: 1.15 1.15;
    opacity: 1;
    border-color: var(--hero-primary, rgb(200, 200, 220));
    background-color: rgba(30, 30, 45, 0.9);
}

.hero-card-name {
    font-size: 10px;
    color: rgba(200, 200, 210, 0.8);
    letter-spacing: 1px;
    -unity-text-align: middle-center;
}

.hero-card.teaser {
    opacity: 0.35;
    border-style: dashed;
}

.hero-card.teaser .hero-card-name {
    font-size: 8px;
    color: rgba(160, 160, 170, 0.5);
}

.hero-index {
    position: absolute;
    right: 80px;
    font-size: 10px;
    color: rgba(160, 160, 170, 0.5);
    letter-spacing: 2px;
}

/* =============================================================================
   EMBARK LAYER
   ============================================================================= */

.embark-layer {
    flex-direction: row;
    align-items: center;
    justify-content: center;
    padding: 8px 24px 20px 24px;
    height: 80px;
}

.btn-back {
    position: absolute;
    left: 24px;
    font-size: 12px;
    color: rgba(180, 180, 190, 0.6);
    background-color: transparent;
    border-width: 0;
    letter-spacing: 1px;
    transition-property: color;
    transition-duration: 0.2s;
}

.btn-back:hover {
    color: rgb(255, 255, 255);
}

.btn-embark {
    width: 320px;
    height: 56px;
    background-color: rgba(20, 20, 30, 0.85);
    border-width: 2px;
    border-color: var(--hero-accent, rgba(120, 120, 140, 0.4));
    border-radius: 4px;
    align-items: center;
    justify-content: center;
    overflow: hidden;
    transition-property: border-color;
    transition-duration: 0.6s;
}

.btn-embark:hover {
    border-color: var(--hero-primary, rgba(200, 200, 220, 0.8));
}

.embark-glow {
    position: absolute;
    width: 100%;
    height: 100%;
    background-color: var(--hero-accent, rgba(255, 200, 100, 0.05));
    opacity: 0;
    transition-property: opacity, background-color;
    transition-duration: 0.6s;
}

.embark-glow.breathing {
    opacity: 0.15;
}

.embark-text {
    font-size: 16px;
    color: var(--hero-accent, rgb(220, 220, 230));
    -unity-font-style: bold;
    letter-spacing: 3px;
    transition-property: color;
    transition-duration: 0.6s;
}

.embark-subtitle {
    font-size: 10px;
    color: rgba(180, 180, 190, 0.5);
    letter-spacing: 1px;
    margin-top: 2px;
}

/* =============================================================================
   CONFIRM POPUP
   ============================================================================= */

.confirm-overlay {
    position: absolute;
    width: 100%;
    height: 100%;
    align-items: center;
    justify-content: center;
}

.confirm-overlay.hidden {
    display: none;
}

.confirm-dim {
    position: absolute;
    width: 100%;
    height: 100%;
    background-color: rgba(0, 0, 0, 0.4);
}

.confirm-popup {
    width: 420px;
    padding: 32px;
    align-items: center;
    background-color: rgba(10, 10, 15, 0.95);
    border-color: var(--hero-accent, rgba(160, 160, 180, 0.4));
    border-width: 1px;
    border-radius: 6px;
}

.confirm-title {
    font-size: 16px;
    color: rgb(220, 220, 230);
    -unity-font-style: bold;
    letter-spacing: 3px;
    margin-bottom: 16px;
}

.confirm-description {
    font-size: 13px;
    color: rgba(200, 200, 210, 0.8);
    -unity-text-align: middle-center;
    margin-bottom: 24px;
    white-space: normal;
}

.confirm-buttons {
    flex-direction: row;
}

.btn-confirm {
    width: 140px;
    height: 40px;
    font-size: 14px;
    color: rgb(220, 220, 230);
    background-color: var(--hero-primary, rgba(60, 120, 200, 0.6));
    border-width: 1px;
    border-color: var(--hero-accent, rgba(100, 160, 255, 0.4));
    border-radius: 4px;
    -unity-font-style: bold;
    letter-spacing: 2px;
    margin-right: 12px;
    transition-property: background-color;
    transition-duration: 0.2s;
}

.btn-confirm:hover {
    background-color: var(--hero-primary, rgba(80, 140, 220, 0.8));
}

.btn-cancel {
    width: 140px;
    height: 40px;
    font-size: 14px;
    color: rgba(180, 180, 190, 0.7);
    background-color: rgba(40, 40, 50, 0.6);
    border-width: 1px;
    border-color: rgba(80, 80, 100, 0.3);
    border-radius: 4px;
    letter-spacing: 2px;
    transition-property: color, background-color;
    transition-duration: 0.2s;
}

.btn-cancel:hover {
    color: rgb(220, 220, 230);
    background-color: rgba(60, 60, 70, 0.8);
}

/* =============================================================================
   UTILITY CLASSES
   ============================================================================= */

.hidden {
    display: none;
}

.panel-entering-left {
    translate: -40px 0;
    opacity: 0;
}

.panel-entering-right {
    translate: 40px 0;
    opacity: 0;
}
```

**Step 2: Verify Unity compilation**

Run: Unity domain reload. Open CharacterSelect.uxml in UI Builder, attach stylesheet.
Expected: Styles applied visually. Glass panels, stat bars, carousel slots visible.

**Step 3: Commit**

```bash
git add Assets/UI/Styles/CharacterSelect.uss
git add Assets/UI/Styles/CharacterSelect.uss.meta
git commit -m "feat(charselect): add USS stylesheet with theme system

Glass panel aesthetic, per-hero CSS variable themes, GPU-friendly
transitions (translate/opacity/scale). Stat bar fills, carousel
cards, embark breathing glow, confirm popup styling."
```

---

### Task 2.3: Phase 2 Review & Merge

Same review process as Phase 1:
- Opus code review (UXML element naming, USS class consistency, no typos)
- CLI reviewers validate (2/3 approve)
- Memory saves (episodic, AIM, Serena)
- Merge into feature branch

```bash
git checkout feature/character-select-rebuild
git merge feature/cs-phase-2 --no-ff -m "merge: phase 2 - UI skeleton (UXML + USS)"
```

---

## Phase 3: Orchestrator

> The CharacterSelectManager wires everything together.
> It owns the event lifecycle and coordinates all sub-controllers.

### Task 3.1: Create CharacterSelectManager

**Files:**
- Create: `Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs`

**Step 1: Create the orchestrator**

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using VeilBreakers.Core;
using VeilBreakers.Data;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Orchestrator for the character select screen.
    /// Wires sub-controllers, manages hero navigation, and handles scene lifecycle.
    /// Does NOT directly manipulate UI -- delegates to focused controllers.
    /// </summary>
    public class CharacterSelectManager : MonoBehaviour
    {
        // =============================================================================
        // CONSTANTS
        // =============================================================================

        private const string kGameScene = "Overworld";
        private const string kMainMenuScene = "MainMenu";
        private const string kStarterTownLocation = "StarterTown";
        private const string kConfigPath = "CharacterSelect/HeroDisplayConfigs/";

        // =============================================================================
        // SERIALIZED FIELDS
        // =============================================================================

        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private HeroDisplayConfig[] _heroConfigs;

        // =============================================================================
        // STATE
        // =============================================================================

        private List<HeroData> _heroList;
        private int _currentIndex;
        private bool _isTransitioning;
        private bool _isInitialized;
        private VisualElement _root;

        // =============================================================================
        // CACHED UI REFERENCES
        // =============================================================================

        private Button _btnPrev;
        private Button _btnNext;
        private Button _btnBack;
        private Button _btnEmbark;
        private Button _btnConfirm;
        private Button _btnCancel;
        private VisualElement _confirmOverlay;
        private Label _embarkText;
        private Label _confirmDescription;
        private VisualElement _embarkGlow;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        public int CurrentIndex => _currentIndex;
        public HeroData CurrentHero => _heroList != null && _currentIndex >= 0 && _currentIndex < _heroList.Count
            ? _heroList[_currentIndex] : null;
        public HeroDisplayConfig CurrentConfig => _heroConfigs != null && _currentIndex >= 0 && _currentIndex < _heroConfigs.Length
            ? _heroConfigs[_currentIndex] : null;
        public int HeroCount => _heroList?.Count ?? 0;
        public bool IsTransitioning => _isTransitioning;

        // =============================================================================
        // LIFECYCLE
        // =============================================================================

        private void OnEnable()
        {
            StartCoroutine(InitializeWhenReady());
        }

        private void OnDisable()
        {
            UnbindUI();
            CharSelectEvents.ClearAll();
        }

        private IEnumerator InitializeWhenReady()
        {
            // Wait for GameDatabase
            while (!GameDatabase.Instance.IsReady)
            {
                yield return null;
            }

            LoadHeroData();
            CacheUIReferences();
            BindUI();
            ApplyInitialState();

            _isInitialized = true;
            CharSelectEvents.RaiseScreenReady();
        }

        // =============================================================================
        // DATA LOADING
        // =============================================================================

        private void LoadHeroData()
        {
            _heroList = GameDatabase.Instance.GetAllHeroes();

            if (_heroList == null || _heroList.Count == 0)
            {
                Debug.LogError("[CharSelectManager] No heroes found in GameDatabase!");
                return;
            }

            // Sort by hero_id for consistent ordering
            _heroList.Sort((a, b) => string.Compare(a.hero_id, b.hero_id, StringComparison.Ordinal));

            // Validate configs match hero count
            if (_heroConfigs == null || _heroConfigs.Length == 0)
            {
                Debug.LogWarning("[CharSelectManager] No HeroDisplayConfigs assigned. Using defaults.");
                _heroConfigs = new HeroDisplayConfig[_heroList.Count];
            }

            // Reorder configs to match hero list order
            ReorderConfigsToMatchHeroes();
        }

        private void ReorderConfigsToMatchHeroes()
        {
            var ordered = new HeroDisplayConfig[_heroList.Count];
            for (int i = 0; i < _heroList.Count; i++)
            {
                ordered[i] = FindConfigForHero(_heroList[i].hero_id);
            }
            _heroConfigs = ordered;
        }

        private HeroDisplayConfig FindConfigForHero(string heroId)
        {
            if (_heroConfigs == null) return null;

            for (int i = 0; i < _heroConfigs.Length; i++)
            {
                if (_heroConfigs[i] != null && _heroConfigs[i].heroId == heroId)
                {
                    return _heroConfigs[i];
                }
            }

            Debug.LogWarning($"[CharSelectManager] No config found for hero '{heroId}'");
            return null;
        }

        // =============================================================================
        // UI BINDING
        // =============================================================================

        private void CacheUIReferences()
        {
            _root = _uiDocument.rootVisualElement;

            _btnPrev = _root.Q<Button>("btn-prev");
            _btnNext = _root.Q<Button>("btn-next");
            _btnBack = _root.Q<Button>("btn-back");
            _btnEmbark = _root.Q<Button>("btn-embark");
            _btnConfirm = _root.Q<Button>("btn-confirm");
            _btnCancel = _root.Q<Button>("btn-cancel");
            _confirmOverlay = _root.Q<VisualElement>("confirm-overlay");
            _embarkText = _root.Q<Label>("embark-text");
            _confirmDescription = _root.Q<Label>("confirm-description");
            _embarkGlow = _root.Q<VisualElement>("embark-glow");
        }

        private void BindUI()
        {
            _btnPrev?.RegisterCallback<ClickEvent>(OnPrevClicked);
            _btnNext?.RegisterCallback<ClickEvent>(OnNextClicked);
            _btnBack?.RegisterCallback<ClickEvent>(OnBackClicked);
            _btnEmbark?.RegisterCallback<ClickEvent>(OnEmbarkClicked);
            _btnConfirm?.RegisterCallback<ClickEvent>(OnConfirmClicked);
            _btnCancel?.RegisterCallback<ClickEvent>(OnCancelClicked);

            // Keyboard / gamepad navigation
            _root?.RegisterCallback<NavigationMoveEvent>(OnNavigationMove);
            _root?.RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);
            _root?.RegisterCallback<NavigationCancelEvent>(OnNavigationCancel);
        }

        private void UnbindUI()
        {
            _btnPrev?.UnregisterCallback<ClickEvent>(OnPrevClicked);
            _btnNext?.UnregisterCallback<ClickEvent>(OnNextClicked);
            _btnBack?.UnregisterCallback<ClickEvent>(OnBackClicked);
            _btnEmbark?.UnregisterCallback<ClickEvent>(OnEmbarkClicked);
            _btnConfirm?.UnregisterCallback<ClickEvent>(OnConfirmClicked);
            _btnCancel?.UnregisterCallback<ClickEvent>(OnCancelClicked);

            _root?.UnregisterCallback<NavigationMoveEvent>(OnNavigationMove);
            _root?.UnregisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);
            _root?.UnregisterCallback<NavigationCancelEvent>(OnNavigationCancel);
        }

        // =============================================================================
        // INITIAL STATE
        // =============================================================================

        private void ApplyInitialState()
        {
            _currentIndex = 0;
            _confirmOverlay?.AddToClassList("hidden");

            if (_heroList != null && _heroList.Count > 0)
            {
                ApplyThemeClass(_heroList[0].hero_id);
                CharSelectEvents.RaiseHeroChanged(0, _heroList[0], CurrentConfig);
                CharSelectEvents.RaiseHeroDataLoaded(_heroList[0]);
                CharSelectEvents.RaiseHeroSelected();
                UpdateEmbarkText();
            }
        }

        // =============================================================================
        // HERO NAVIGATION
        // =============================================================================

        public void NavigateToHero(int index)
        {
            if (_isTransitioning || _heroList == null || _heroList.Count == 0) return;

            index = Mathf.Clamp(index, 0, _heroList.Count - 1);
            if (index == _currentIndex) return;

            _isTransitioning = true;
            int prevIndex = _currentIndex;
            _currentIndex = index;

            ApplyThemeClass(_heroList[_currentIndex].hero_id);
            CharSelectEvents.RaiseHeroChanged(_currentIndex, _heroList[_currentIndex], CurrentConfig);
            CharSelectEvents.RaiseHeroDataLoaded(_heroList[_currentIndex]);
            CharSelectEvents.RaiseHeroSelected();
            UpdateEmbarkText();

            // Transition completes after USS animations finish
            StartCoroutine(EndTransitionAfterDelay(0.8f));
        }

        private IEnumerator EndTransitionAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            _isTransitioning = false;
        }

        public void NavigatePrev()
        {
            if (_heroList == null || _heroList.Count == 0) return;
            int newIndex = _currentIndex - 1;
            if (newIndex < 0) newIndex = _heroList.Count - 1; // Wrap around
            NavigateToHero(newIndex);
        }

        public void NavigateNext()
        {
            if (_heroList == null || _heroList.Count == 0) return;
            int newIndex = (_currentIndex + 1) % _heroList.Count;
            NavigateToHero(newIndex);
        }

        // =============================================================================
        // THEME MANAGEMENT
        // =============================================================================

        private static readonly string[] kThemeClasses = { "theme-vex", "theme-seraphina", "theme-orion", "theme-nyx" };

        private void ApplyThemeClass(string heroId)
        {
            if (_root == null) return;

            // Remove all theme classes
            for (int i = 0; i < kThemeClasses.Length; i++)
            {
                _root.RemoveFromClassList(kThemeClasses[i]);
            }

            // Add new theme class
            string themeClass = $"theme-{heroId}";
            _root.AddToClassList(themeClass);
        }

        // =============================================================================
        // EMBARK FLOW
        // =============================================================================

        private void UpdateEmbarkText()
        {
            var hero = CurrentHero;
            if (hero == null) return;

            string name = string.IsNullOrEmpty(hero.display_name) ? hero.hero_id.ToUpper() : hero.display_name.ToUpper();
            if (_embarkText != null) _embarkText.text = $"EMBARK AS {name}";
            if (_confirmDescription != null)
            {
                string title = string.IsNullOrEmpty(hero.title) ? "" : $", {hero.title.ToUpper()}";
                _confirmDescription.text = $"You will begin your journey as {name}{title}";
            }

            // Start breathing glow
            _embarkGlow?.AddToClassList("breathing");
        }

        private void ShowConfirmPopup()
        {
            _confirmOverlay?.RemoveFromClassList("hidden");
            CharSelectEvents.RaiseEmbarkRequested();
        }

        private void HideConfirmPopup()
        {
            _confirmOverlay?.AddToClassList("hidden");
            CharSelectEvents.RaiseEmbarkCancelled();
        }

        private void ExecuteEmbark()
        {
            var hero = CurrentHero;
            if (hero == null) return;

            CharSelectEvents.RaiseEmbarkConfirmed();
            CharSelectEvents.RaiseScreenExiting();

            StartCoroutine(EmbarkSequence(hero));
        }

        private IEnumerator EmbarkSequence(HeroData hero)
        {
            // Create save file
            yield return StartCoroutine(CreateOrRotateNewGameSave(hero));

            // Transition to gameplay
            if (ScreenTransition.HasInstance)
            {
                ScreenTransition.Instance.Transition(() => SceneManager.LoadScene(kGameScene));
            }
            else
            {
                SceneManager.LoadScene(kGameScene);
            }
        }

        private IEnumerator CreateOrRotateNewGameSave(HeroData hero)
        {
            if (!SaveManager.HasInstance || hero == null)
            {
                yield break;
            }

            var saveManager = SaveManager.Instance;
            var slotTask = saveManager.GetBestNewGameSlotAsync();
            while (!slotTask.IsCompleted) yield return null;

            if (slotTask.IsFaulted || slotTask.IsCanceled)
            {
                Debug.LogWarning("[CharSelectManager] Failed to resolve save slot.");
                yield break;
            }

            int slot = slotTask.Result;
            string heroName = string.IsNullOrEmpty(hero.display_name) ? hero.hero_id : hero.display_name;

            var createTask = saveManager.CreateNewSaveAsync(slot, hero.hero_id, heroName, hero.GetPrimaryPath());
            while (!createTask.IsCompleted) yield return null;

            if (createTask.IsFaulted || createTask.IsCanceled || !createTask.Result)
            {
                Debug.LogWarning($"[CharSelectManager] Failed to create save in slot {slot}.");
                yield break;
            }

            saveManager.SetCurrentLocation(kStarterTownLocation);
            var saveTask = saveManager.SaveAsync(slot);
            while (!saveTask.IsCompleted) yield return null;
        }

        // =============================================================================
        // UI EVENT HANDLERS
        // =============================================================================

        private void OnPrevClicked(ClickEvent evt) => NavigatePrev();
        private void OnNextClicked(ClickEvent evt) => NavigateNext();

        private void OnBackClicked(ClickEvent evt)
        {
            CharSelectEvents.RaiseScreenExiting();
            if (ScreenTransition.HasInstance)
            {
                ScreenTransition.Instance.Transition(() => SceneManager.LoadScene(kMainMenuScene));
            }
            else
            {
                SceneManager.LoadScene(kMainMenuScene);
            }
        }

        private void OnEmbarkClicked(ClickEvent evt) => ShowConfirmPopup();
        private void OnConfirmClicked(ClickEvent evt) => ExecuteEmbark();
        private void OnCancelClicked(ClickEvent evt) => HideConfirmPopup();

        // =============================================================================
        // NAVIGATION EVENTS (KEYBOARD / GAMEPAD)
        // =============================================================================

        private void OnNavigationMove(NavigationMoveEvent evt)
        {
            switch (evt.direction)
            {
                case NavigationMoveEvent.Direction.Left:
                    NavigatePrev();
                    evt.StopPropagation();
                    break;
                case NavigationMoveEvent.Direction.Right:
                    NavigateNext();
                    evt.StopPropagation();
                    break;
            }
        }

        private void OnNavigationSubmit(NavigationSubmitEvent evt)
        {
            if (!_confirmOverlay.ClassListContains("hidden"))
            {
                ExecuteEmbark();
            }
            else
            {
                ShowConfirmPopup();
            }
            evt.StopPropagation();
        }

        private void OnNavigationCancel(NavigationCancelEvent evt)
        {
            if (!_confirmOverlay.ClassListContains("hidden"))
            {
                HideConfirmPopup();
            }
            else
            {
                OnBackClicked(null);
            }
            evt.StopPropagation();
        }
    }
}
```

**Step 2: Verify Unity compilation**

Run: Unity domain reload. Check Console.
Expected: Zero errors. Note: `SaveManager.HasInstance` and `ScreenTransition.HasInstance` must exist as static properties on those singletons. If compilation fails on these, check the SingletonMonoBehaviour base class for the correct static access pattern and adjust.

**Step 3: Commit**

```bash
git checkout -b feature/cs-phase-3
git add Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs
git add Assets/Scripts/UI/CharacterSelect/CharacterSelectManager.cs.meta
git commit -m "feat(charselect): add CharacterSelectManager orchestrator

Central coordinator. Loads hero data from GameDatabase, manages
navigation with wrap-around, applies USS theme classes, handles
embark confirmation flow, saves game on confirm. All UI callbacks
bound in OnEnable, unbound in OnDisable."
```

---

### Task 3.2: Phase 3 Review & Merge

- Opus code review (event lifecycle, null safety, coroutine cleanup)
- CLI reviewers validate (2/3 approve)
- Memory saves
- Merge: `git merge feature/cs-phase-3 --no-ff`

---

## Phase 4: 3D Rendering Pipeline

> Camera, lighting rig, RenderTexture setup, model loading.

### Task 4.1: Create HeroStageController

**Files:**
- Create: `Assets/Scripts/UI/CharacterSelect/HeroStageController.cs`

**Step 1: Create the 3D stage controller**

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Data;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Manages the 3D hero preview: camera, lighting rig, RenderTexture,
    /// model instantiation, and champion monster display.
    /// </summary>
    public class HeroStageController : MonoBehaviour
    {
        // =============================================================================
        // CONSTANTS
        // =============================================================================

        private const int kRenderTextureWidth = 1024;
        private const int kRenderTextureHeight = 1536;
        private const int kMSAASamples = 4;
        private const int kPreviewLayer = 31; // "CharacterPreview" layer
        private const string kPreviewLayerName = "CharacterPreview";

        // =============================================================================
        // SERIALIZED FIELDS
        // =============================================================================

        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private Camera _previewCamera;

        // =============================================================================
        // RUNTIME STATE
        // =============================================================================

        private RenderTexture _renderTexture;
        private GameObject _currentModel;
        private GameObject _currentChampion;
        private Light _keyLight;
        private Light _fillLight;
        private Light _rimLight;
        private Light _faceLight;
        private Light _groundLight;
        private VisualElement _renderTarget;
        private Transform _stageRoot;
        private HeroDisplayConfig _currentConfig;

        // Drag rotation
        private bool _isDragging;
        private float _dragStartX;
        private float _modelRotationY;

        // Placeholder material
        private static Material _placeholderMaterial;

        // =============================================================================
        // LIFECYCLE
        // =============================================================================

        private void OnEnable()
        {
            CharSelectEvents.OnHeroChanged += HandleHeroChanged;
            CharSelectEvents.OnScreenExiting += HandleScreenExiting;

            InitializeStage();
        }

        private void OnDisable()
        {
            CharSelectEvents.OnHeroChanged -= HandleHeroChanged;
            CharSelectEvents.OnScreenExiting -= HandleScreenExiting;

            CleanupStage();
        }

        private void Update()
        {
            HandleDragInput();
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        private void InitializeStage()
        {
            // Create stage root
            _stageRoot = new GameObject("CharSelectStage").transform;
            _stageRoot.position = new Vector3(100f, 0f, 0f); // Off-screen position

            // Create RenderTexture
            _renderTexture = new RenderTexture(kRenderTextureWidth, kRenderTextureHeight, 24, RenderTextureFormat.ARGB32);
            _renderTexture.antiAliasing = kMSAASamples;
            _renderTexture.filterMode = FilterMode.Bilinear;
            _renderTexture.Create();

            // Setup camera
            if (_previewCamera == null)
            {
                var camObj = new GameObject("PreviewCamera");
                camObj.transform.SetParent(_stageRoot);
                _previewCamera = camObj.AddComponent<Camera>();
            }

            _previewCamera.targetTexture = _renderTexture;
            _previewCamera.fieldOfView = 30f;
            _previewCamera.clearFlags = CameraClearFlags.SolidColor;
            _previewCamera.backgroundColor = Color.clear;
            _previewCamera.cullingMask = 1 << kPreviewLayer;
            _previewCamera.nearClipPlane = 0.1f;
            _previewCamera.farClipPlane = 50f;

            // Create lighting rig
            CreateLightingRig();

            // Bind RenderTexture to UI
            BindRenderTextureToUI();
        }

        private void CreateLightingRig()
        {
            _keyLight = CreateLight("KeyLight", LightType.Directional, Color.white, 1.2f);
            _keyLight.transform.rotation = Quaternion.Euler(35f, -30f, 0f);

            _fillLight = CreateLight("FillLight", LightType.Point, new Color(0.4f, 0.5f, 0.6f), 0.6f);
            _fillLight.transform.localPosition = new Vector3(-2f, 1.5f, 1f);
            _fillLight.range = 10f;

            _rimLight = CreateLight("RimLight", LightType.Point, Color.cyan, 1.5f);
            _rimLight.transform.localPosition = new Vector3(0f, 2f, 2f);
            _rimLight.range = 8f;

            _faceLight = CreateLight("FaceLight", LightType.Spot, new Color(1f, 0.95f, 0.9f), 0.4f);
            _faceLight.transform.localPosition = new Vector3(0f, 2f, -1.5f);
            _faceLight.transform.rotation = Quaternion.Euler(20f, 0f, 0f);
            _faceLight.spotAngle = 45f;
            _faceLight.range = 5f;

            _groundLight = CreateLight("GroundLight", LightType.Point, new Color(0.3f, 0.3f, 0.4f), 0.3f);
            _groundLight.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            _groundLight.range = 5f;
        }

        private Light CreateLight(string name, LightType type, Color color, float intensity)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(_stageRoot);
            obj.layer = kPreviewLayer;

            var light = obj.AddComponent<Light>();
            light.type = type;
            light.color = color;
            light.intensity = intensity;
            light.cullingMask = 1 << kPreviewLayer;

            return light;
        }

        private void BindRenderTextureToUI()
        {
            if (_uiDocument == null) return;

            _renderTarget = _uiDocument.rootVisualElement.Q<VisualElement>("hero-render-target");
            if (_renderTarget != null)
            {
                _renderTarget.style.backgroundImage = Background.FromRenderTexture(_renderTexture);
                _renderTarget.usageHints = UsageHints.DynamicTransform;

                // Register drag events
                _renderTarget.RegisterCallback<PointerDownEvent>(OnPointerDown);
                _renderTarget.RegisterCallback<PointerMoveEvent>(OnPointerMove);
                _renderTarget.RegisterCallback<PointerUpEvent>(OnPointerUp);
            }
        }

        // =============================================================================
        // HERO SWITCHING
        // =============================================================================

        private void HandleHeroChanged(int index, HeroData data, HeroDisplayConfig config)
        {
            _currentConfig = config;
            StartCoroutine(SwapHeroModel(data, config));
        }

        private IEnumerator SwapHeroModel(HeroData data, HeroDisplayConfig config)
        {
            // Destroy previous model
            if (_currentModel != null)
            {
                Destroy(_currentModel);
                _currentModel = null;
            }
            if (_currentChampion != null)
            {
                Destroy(_currentChampion);
                _currentChampion = null;
            }

            // Load new model
            GameObject prefab = config?.modelPrefab;
            if (prefab != null)
            {
                _currentModel = Instantiate(prefab, _stageRoot);
            }
            else
            {
                // Placeholder: brand-colored capsule
                _currentModel = CreatePlaceholderModel(data, config);
            }

            SetLayerRecursive(_currentModel, kPreviewLayer);
            _currentModel.transform.localPosition = Vector3.zero;
            _currentModel.transform.localRotation = Quaternion.identity;
            _modelRotationY = 0f;

            // Load champion monster
            if (config?.championModelPrefab != null)
            {
                _currentChampion = Instantiate(config.championModelPrefab, _stageRoot);
                SetLayerRecursive(_currentChampion, kPreviewLayer);
                _currentChampion.transform.localPosition = config.championOffset;
                _currentChampion.transform.localScale = Vector3.one * config.championScale;
            }

            // Apply camera config
            ApplyCameraConfig(config);

            // Apply lighting config
            ApplyLightingConfig(config);

            yield return null;
        }

        private GameObject CreatePlaceholderModel(HeroData data, HeroDisplayConfig config)
        {
            var placeholder = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            placeholder.transform.SetParent(_stageRoot);
            placeholder.transform.localPosition = new Vector3(0f, 1f, 0f);
            placeholder.transform.localScale = new Vector3(0.6f, 1f, 0.6f);
            placeholder.name = $"Placeholder_{data?.hero_id ?? "unknown"}";

            // Destroy collider (not needed for display)
            var collider = placeholder.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            // Apply brand-colored emissive material
            var renderer = placeholder.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (_placeholderMaterial == null)
                {
                    _placeholderMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                }

                var mat = new Material(_placeholderMaterial);
                Color brandColor = config?.primaryColor ?? (data?.color_palette?.ToColor() ?? Color.gray);
                mat.color = brandColor;
                mat.SetColor("_EmissionColor", brandColor * 0.3f);
                mat.EnableKeyword("_EMISSION");
                renderer.material = mat;
            }

            return placeholder;
        }

        // =============================================================================
        // CAMERA & LIGHTING
        // =============================================================================

        private void ApplyCameraConfig(HeroDisplayConfig config)
        {
            if (_previewCamera == null) return;

            Vector3 offset = config?.cameraOffset ?? new Vector3(0f, 1.2f, -3f);
            float fov = config?.cameraFOV ?? 30f;

            _previewCamera.transform.localPosition = offset;
            _previewCamera.transform.LookAt(_stageRoot.position + Vector3.up * 1.2f);
            _previewCamera.fieldOfView = fov;
        }

        private void ApplyLightingConfig(HeroDisplayConfig config)
        {
            if (config == null) return;

            // Key light
            if (_keyLight != null)
            {
                _keyLight.color = config.keyLightColor;
                _keyLight.intensity = config.keyLightIntensity;
            }

            // Fill light
            if (_fillLight != null)
            {
                _fillLight.color = config.fillLightColor;
                _fillLight.intensity = config.fillLightIntensity;
            }

            // Rim light (brand accent)
            if (_rimLight != null)
            {
                _rimLight.color = config.rimLightColor;
                _rimLight.intensity = config.rimLightIntensity;
            }

            // Ground light (brand secondary)
            if (_groundLight != null)
            {
                _groundLight.color = config.secondaryColor;
            }
        }

        // =============================================================================
        // DRAG ROTATION
        // =============================================================================

        private void OnPointerDown(PointerDownEvent evt)
        {
            _isDragging = true;
            _dragStartX = evt.position.x;
            _renderTarget?.CapturePointer(evt.pointerId);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging || _currentModel == null) return;

            float deltaX = evt.position.x - _dragStartX;
            _dragStartX = evt.position.x;
            _modelRotationY += deltaX * 0.5f;
            _currentModel.transform.localRotation = Quaternion.Euler(0f, _modelRotationY, 0f);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            _isDragging = false;
            _renderTarget?.ReleasePointer(evt.pointerId);
        }

        private void HandleDragInput()
        {
            // Procedural idle for placeholder models (no Animator)
            if (_currentModel != null && _currentModel.GetComponent<Animator>() == null)
            {
                float breath = 1f + Mathf.Sin(Time.time * 1.2f) * 0.005f;
                _currentModel.transform.localScale = new Vector3(0.6f, 1f, 0.6f) * breath;

                if (!_isDragging)
                {
                    _modelRotationY += 5f * Time.deltaTime;
                    _currentModel.transform.localRotation = Quaternion.Euler(0f, _modelRotationY, 0f);
                }
            }
        }

        // =============================================================================
        // CLEANUP
        // =============================================================================

        private void HandleScreenExiting()
        {
            // Nothing to do -- cleanup happens in OnDisable
        }

        private void CleanupStage()
        {
            if (_renderTarget != null)
            {
                _renderTarget.UnregisterCallback<PointerDownEvent>(OnPointerDown);
                _renderTarget.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
                _renderTarget.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            }

            if (_currentModel != null) Destroy(_currentModel);
            if (_currentChampion != null) Destroy(_currentChampion);

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
            }

            if (_stageRoot != null) Destroy(_stageRoot.gameObject);
        }

        // =============================================================================
        // HELPERS
        // =============================================================================

        private static void SetLayerRecursive(GameObject obj, int layer)
        {
            if (obj == null) return;
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }
    }
}
```

**Step 2: Verify Unity compilation**

Expected: Zero errors. The shader `"Universal Render Pipeline/Lit"` must exist (URP must be installed).

**Step 3: Commit**

```bash
git checkout -b feature/cs-phase-4
git add Assets/Scripts/UI/CharacterSelect/HeroStageController.cs
git add Assets/Scripts/UI/CharacterSelect/HeroStageController.cs.meta
git commit -m "feat(charselect): add HeroStageController 3D pipeline

RenderTexture 1024x1536 4xMSAA, 5-light rig, per-hero camera/lighting
config from ScriptableObject, model instantiation with placeholder
fallback (brand-colored capsule), champion monster display, drag-to-
rotate input, procedural idle for models without Animator."
```

---

### Task 4.2: Phase 4 Review & Merge

Same process. Merge into feature branch.

---

## Phase 5: UI Panel Controllers

> Left panel (hero data) and right panel (stats + abilities).

### Task 5.1: Create HeroDataPanelController

**Files:**
- Create: `Assets/Scripts/UI/CharacterSelect/HeroDataPanelController.cs`

**Step 1: Create the left panel controller**

```csharp
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Core;
using VeilBreakers.Data;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Populates the left info panel: name, title, quote, path/role/resource,
    /// starter stats grid, and champion monster info.
    /// </summary>
    public class HeroDataPanelController : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;

        // Cached references
        private VisualElement _panel;
        private Label _heroName;
        private Label _heroTitle;
        private Label _heroQuote;
        private Label _heroPath;
        private Label _heroRole;
        private Label _heroResource;
        private Label _statHp;
        private Label _statAtk;
        private Label _statDef;
        private Label _statSpd;
        private Label _championName;
        private Label _championBrand;
        private Label _championRole;
        private VisualElement _championSection;

        private void OnEnable()
        {
            CacheReferences();
            CharSelectEvents.OnHeroChanged += HandleHeroChanged;
        }

        private void OnDisable()
        {
            CharSelectEvents.OnHeroChanged -= HandleHeroChanged;
        }

        private void CacheReferences()
        {
            var root = _uiDocument.rootVisualElement;
            _panel = root.Q<VisualElement>("hero-info-panel");
            _heroName = root.Q<Label>("hero-name");
            _heroTitle = root.Q<Label>("hero-title");
            _heroQuote = root.Q<Label>("hero-quote");
            _heroPath = root.Q<Label>("hero-path");
            _heroRole = root.Q<Label>("hero-role");
            _heroResource = root.Q<Label>("hero-resource");
            _statHp = root.Q<Label>("stat-hp");
            _statAtk = root.Q<Label>("stat-atk");
            _statDef = root.Q<Label>("stat-def");
            _statSpd = root.Q<Label>("stat-spd");
            _championName = root.Q<Label>("champion-name");
            _championBrand = root.Q<Label>("champion-brand");
            _championRole = root.Q<Label>("champion-role");
            _championSection = root.Q<VisualElement>("champion-section");
        }

        private void HandleHeroChanged(int index, HeroData data, HeroDisplayConfig config)
        {
            if (data == null) return;

            // Identity
            SetLabel(_heroName, data.display_name?.ToUpper() ?? data.hero_id.ToUpper());
            SetLabel(_heroTitle, data.title?.ToUpper() ?? "");
            SetLabel(_heroQuote, !string.IsNullOrEmpty(data.quote) ? $"\"{data.quote}\"" : "");

            // Class info
            SetLabel(_heroPath, data.GetPrimaryPath().ToString());
            SetLabel(_heroRole, data.role?.ToUpper() ?? "");
            SetLabel(_heroResource, data.resource_type?.ToUpper() ?? "");

            // Starter stats
            SetLabel(_statHp, data.base_hp.ToString());
            SetLabel(_statAtk, data.base_attack.ToString());
            SetLabel(_statDef, data.base_defense.ToString());
            SetLabel(_statSpd, data.base_speed.ToString());

            // Champion monster
            PopulateChampion(data);

            // Panel slide-in animation
            AnimatePanel();
        }

        private void PopulateChampion(HeroData data)
        {
            if (string.IsNullOrEmpty(data.starter_monster_id))
            {
                _championSection?.AddToClassList("hidden");
                return;
            }

            _championSection?.RemoveFromClassList("hidden");

            var monster = GameDatabase.Instance.GetMonster(data.starter_monster_id);
            if (monster == null)
            {
                SetLabel(_championName, data.starter_monster_id);
                SetLabel(_championBrand, "");
                SetLabel(_championRole, "");
                return;
            }

            SetLabel(_championName, monster.display_name ?? data.starter_monster_id);
            SetLabel(_championBrand, monster.GetPrimaryBrand().ToString());
            SetLabel(_championRole, monster.role?.ToUpper() ?? "");
        }

        private void AnimatePanel()
        {
            if (_panel == null) return;

            // Trigger slide-in by toggling class
            _panel.AddToClassList("panel-hidden");
            _panel.schedule.Execute(() => _panel.RemoveFromClassList("panel-hidden")).ExecuteLater(50);
        }

        private static void SetLabel(Label label, string text)
        {
            if (label != null) label.text = text;
        }
    }
}
```

**Step 2: Verify compilation, commit**

```bash
git add Assets/Scripts/UI/CharacterSelect/HeroDataPanelController.cs
git commit -m "feat(charselect): add HeroDataPanelController left panel

Populates hero identity, path/role/resource, starter stats 2x2 grid,
and champion monster info from GameDatabase. Slide-in animation on
hero switch."
```

---

### Task 5.2: Create HeroStatsPanelController

**Files:**
- Create: `Assets/Scripts/UI/CharacterSelect/HeroStatsPanelController.cs`

**Step 1: Create the right panel controller**

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Core;
using VeilBreakers.Data;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Populates the right panel: D&D attribute bars (STR/DEX/CON/INT/WIS/CHA)
    /// with animated fills, and the abilities list.
    /// </summary>
    public class HeroStatsPanelController : MonoBehaviour
    {
        private const float kMaxStatValue = 20f; // D&D max for percentage calc

        [SerializeField] private UIDocument _uiDocument;

        // Cached stat bar references
        private VisualElement _panel;
        private readonly VisualElement[] _barFills = new VisualElement[6];
        private readonly Label[] _barValues = new Label[6];
        private readonly Label[] _abilitySlots = new Label[5];

        private static readonly string[] kStatNames = { "str", "dex", "con", "int", "wis", "cha" };

        private void OnEnable()
        {
            CacheReferences();
            CharSelectEvents.OnHeroChanged += HandleHeroChanged;
        }

        private void OnDisable()
        {
            CharSelectEvents.OnHeroChanged -= HandleHeroChanged;
        }

        private void CacheReferences()
        {
            var root = _uiDocument.rootVisualElement;
            _panel = root.Q<VisualElement>("stats-panel");

            for (int i = 0; i < kStatNames.Length; i++)
            {
                _barFills[i] = root.Q<VisualElement>($"bar-{kStatNames[i]}-fill");
                _barValues[i] = root.Q<Label>($"bar-{kStatNames[i]}-value");
            }

            for (int i = 0; i < _abilitySlots.Length; i++)
            {
                _abilitySlots[i] = root.Q<Label>($"ability-{i}");
            }
        }

        private void HandleHeroChanged(int index, HeroData data, HeroDisplayConfig config)
        {
            if (data == null) return;

            UpdateStatBars(data);
            UpdateAbilities(data);
            AnimatePanel();
        }

        private void UpdateStatBars(HeroData data)
        {
            var stats = data.base_stats;
            if (stats == null) return;

            int[] values = {
                stats.strength, stats.dexterity, stats.constitution,
                stats.intelligence, stats.wisdom, stats.charisma
            };

            for (int i = 0; i < values.Length && i < _barFills.Length; i++)
            {
                float pct = Mathf.Clamp01(values[i] / kMaxStatValue) * 100f;

                if (_barFills[i] != null)
                {
                    _barFills[i].style.width = new StyleLength(new Length(pct, LengthUnit.Percent));
                }

                if (_barValues[i] != null)
                {
                    _barValues[i].text = values[i].ToString();
                }
            }
        }

        private void UpdateAbilities(HeroData data)
        {
            string[] skills = data.innate_skills;

            for (int i = 0; i < _abilitySlots.Length; i++)
            {
                if (_abilitySlots[i] == null) continue;

                if (skills != null && i < skills.Length)
                {
                    string skillId = skills[i];

                    // Try to get skill display name from database
                    var skillData = GameDatabase.Instance.GetSkill(skillId);
                    string displayName = skillData?.display_name ?? FormatSkillId(skillId);

                    _abilitySlots[i].text = displayName;
                    _abilitySlots[i].style.display = DisplayStyle.Flex;
                }
                else
                {
                    _abilitySlots[i].style.display = DisplayStyle.None;
                }
            }
        }

        private static string FormatSkillId(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return "";

            // Convert snake_case to Title Case: "shackle_strike" -> "Shackle Strike"
            var parts = skillId.Split('_');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
                }
            }
            return string.Join(" ", parts);
        }

        private void AnimatePanel()
        {
            if (_panel == null) return;
            _panel.AddToClassList("panel-hidden");
            _panel.schedule.Execute(() => _panel.RemoveFromClassList("panel-hidden")).ExecuteLater(50);
        }
    }
}
```

**Step 2: Verify compilation, commit**

```bash
git add Assets/Scripts/UI/CharacterSelect/HeroStatsPanelController.cs
git commit -m "feat(charselect): add HeroStatsPanelController right panel

6 D&D attribute bars with animated percentage fills (USS transitions),
5 ability slots populated from innate_skills with display name lookup.
Snake_case to Title Case fallback formatting."
```

---

### Task 5.3: Phase 5 Review & Merge

Same process. Merge into feature branch.

---

## Phase 6: Carousel & Navigation

> Hero selection strip with dynamic slot generation.

### Task 6.1: Create CarouselController

**Files:**
- Create: `Assets/Scripts/UI/CharacterSelect/CarouselController.cs`

**Step 1: Create the carousel controller**

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Data;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Manages the hero carousel strip at the bottom.
    /// Dynamically generates hero cards from data, handles selection highlighting,
    /// and updates hero index label.
    /// </summary>
    public class CarouselController : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private CharacterSelectManager _manager;

        private VisualElement _carouselStrip;
        private Label _heroIndex;
        private readonly List<VisualElement> _heroCards = new List<VisualElement>();
        private int _selectedIndex = -1;

        private void OnEnable()
        {
            CacheReferences();
            CharSelectEvents.OnScreenReady += HandleScreenReady;
            CharSelectEvents.OnHeroChanged += HandleHeroChanged;
        }

        private void OnDisable()
        {
            CharSelectEvents.OnScreenReady -= HandleScreenReady;
            CharSelectEvents.OnHeroChanged -= HandleHeroChanged;
        }

        private void CacheReferences()
        {
            var root = _uiDocument.rootVisualElement;
            _carouselStrip = root.Q<VisualElement>("carousel-strip");
            _heroIndex = root.Q<Label>("hero-index");
        }

        private void HandleScreenReady()
        {
            BuildCarousel();
        }

        private void BuildCarousel()
        {
            if (_carouselStrip == null || _manager == null) return;

            _carouselStrip.Clear();
            _heroCards.Clear();

            var heroes = _manager.HeroCount;

            for (int i = 0; i < heroes; i++)
            {
                var card = CreateHeroCard(i);
                _carouselStrip.Add(card);
                _heroCards.Add(card);
            }

            // Add teaser slot
            var teaser = CreateTeaserCard();
            _carouselStrip.Add(teaser);

            // Select first
            if (_heroCards.Count > 0)
            {
                UpdateSelection(0);
            }
        }

        private VisualElement CreateHeroCard(int index)
        {
            var card = new VisualElement();
            card.AddToClassList("hero-card");
            card.usageHints = UsageHints.DynamicTransform | UsageHints.DynamicColor;

            // Get hero name from manager
            var heroes = VeilBreakers.Core.GameDatabase.Instance.GetAllHeroes();
            heroes.Sort((a, b) => string.Compare(a.hero_id, b.hero_id, System.StringComparison.Ordinal));

            string name = index < heroes.Count ? heroes[index].display_name?.ToUpper() ?? "???" : "???";

            var label = new Label(name);
            label.AddToClassList("hero-card-name");
            card.Add(label);

            // Click handler (capture index)
            int capturedIndex = index;
            card.RegisterCallback<ClickEvent>(_ => OnCardClicked(capturedIndex));

            return card;
        }

        private VisualElement CreateTeaserCard()
        {
            var card = new VisualElement();
            card.AddToClassList("hero-card");
            card.AddToClassList("teaser");

            var label = new Label("?");
            label.AddToClassList("hero-card-name");
            card.Add(label);

            var subLabel = new Label("COMING SOON");
            subLabel.AddToClassList("hero-card-name");
            subLabel.style.fontSize = 7;
            subLabel.style.opacity = 0.5f;
            card.Add(subLabel);

            return card;
        }

        private void OnCardClicked(int index)
        {
            _manager?.NavigateToHero(index);
        }

        private void HandleHeroChanged(int index, HeroData data, HeroDisplayConfig config)
        {
            UpdateSelection(index);
            UpdateHeroIndex(index);
        }

        private void UpdateSelection(int index)
        {
            // Remove previous selection
            if (_selectedIndex >= 0 && _selectedIndex < _heroCards.Count)
            {
                _heroCards[_selectedIndex].RemoveFromClassList("selected");
            }

            // Apply new selection
            _selectedIndex = index;
            if (_selectedIndex >= 0 && _selectedIndex < _heroCards.Count)
            {
                _heroCards[_selectedIndex].AddToClassList("selected");
            }
        }

        private void UpdateHeroIndex(int index)
        {
            if (_heroIndex != null)
            {
                _heroIndex.text = $"HERO {index + 1} / {_manager.HeroCount}";
            }
        }
    }
}
```

**Step 2: Verify compilation, commit**

```bash
git checkout -b feature/cs-phase-6
git add Assets/Scripts/UI/CharacterSelect/CarouselController.cs
git commit -m "feat(charselect): add CarouselController hero strip

Dynamic slot generation from hero count, click-to-select, USS class
toggling for selection highlight (scale 1.15x + brand border),
teaser slot, hero index label update."
```

---

### Task 6.2: Phase 6 Review & Merge

Same process.

---

## Phase 7: Environment & Transitions

> Background atmosphere and hero-switch visual transitions.

### Task 7.1: Create CharSelectEnvironmentController

**Files:**
- Create: `Assets/Scripts/UI/CharacterSelect/CharSelectEnvironmentController.cs`

**Step 1: Create the environment controller**

```csharp
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Data;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Manages background gradients and ambient atmosphere.
    /// Changes background tint based on hero's theme colors.
    /// </summary>
    public class CharSelectEnvironmentController : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;

        private VisualElement _backgroundGradient;

        private void OnEnable()
        {
            CacheReferences();
            CharSelectEvents.OnHeroChanged += HandleHeroChanged;
        }

        private void OnDisable()
        {
            CharSelectEvents.OnHeroChanged -= HandleHeroChanged;
        }

        private void CacheReferences()
        {
            var root = _uiDocument.rootVisualElement;
            _backgroundGradient = root.Q<VisualElement>("background-gradient");

            if (_backgroundGradient != null)
            {
                _backgroundGradient.usageHints = UsageHints.DynamicColor;
            }
        }

        private void HandleHeroChanged(int index, HeroData data, HeroDisplayConfig config)
        {
            if (config == null || _backgroundGradient == null) return;

            // Dark tinted background based on hero's secondary color
            Color bgColor = config.secondaryColor;
            bgColor.r *= 0.15f;
            bgColor.g *= 0.15f;
            bgColor.b *= 0.15f;
            bgColor.a = 1f;

            _backgroundGradient.style.backgroundColor = bgColor;
        }
    }
}
```

**Step 2: Commit**

```bash
git checkout -b feature/cs-phase-7
git add Assets/Scripts/UI/CharacterSelect/CharSelectEnvironmentController.cs
git commit -m "feat(charselect): add CharSelectEnvironmentController

Background tint from hero secondary color. UsageHints.DynamicColor
for GPU-accelerated color transitions."
```

---

### Task 7.2: Create TransitionController

**Files:**
- Create: `Assets/Scripts/UI/CharacterSelect/TransitionController.cs`

**Step 1: Create the transition controller**

```csharp
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Data;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Orchestrates visual transition sequences during hero switching.
    /// Manages panel slide-in/out timing and USS class toggling.
    /// </summary>
    public class TransitionController : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;

        private VisualElement _heroInfoPanel;
        private VisualElement _statsPanel;

        private void OnEnable()
        {
            CacheReferences();
            CharSelectEvents.OnHeroChanged += HandleHeroChanged;
        }

        private void OnDisable()
        {
            CharSelectEvents.OnHeroChanged -= HandleHeroChanged;
        }

        private void CacheReferences()
        {
            var root = _uiDocument.rootVisualElement;
            _heroInfoPanel = root.Q<VisualElement>("hero-info-panel");
            _statsPanel = root.Q<VisualElement>("stats-panel");

            // Set usage hints for animated panels
            if (_heroInfoPanel != null) _heroInfoPanel.usageHints = UsageHints.DynamicTransform;
            if (_statsPanel != null) _statsPanel.usageHints = UsageHints.DynamicTransform;
        }

        private void HandleHeroChanged(int index, HeroData data, HeroDisplayConfig config)
        {
            // Panels slide out then back in
            // The individual panel controllers handle their own slide-in
            // This controller coordinates the timing if needed

            // Currently, each panel controller does its own animate.
            // This controller is reserved for future veil tear effects
            // and more complex multi-element sequencing.
        }
    }
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/UI/CharacterSelect/TransitionController.cs
git commit -m "feat(charselect): add TransitionController skeleton

Reserved for veil tear transition effects and multi-element
sequencing. Panel controllers currently self-animate."
```

---

### Task 7.3: Phase 7 Review & Merge

Same process. Merge into feature branch.

---

## Phase 8: Delete Old Files & Scene Wiring

> Remove legacy code and wire everything up in the Unity scene.

### Task 8.1: Delete Old Character Select Files

**Files to delete:**
- `Assets/Scripts/UI/CharacterSelect/CharacterSelectController.cs`
- `Assets/Scripts/UI/CharacterSelect/HeroVFXController.cs`
- `Assets/Scripts/UI/CharacterSelect/CharacterSelectVFXController.cs`
- `Assets/Scripts/UI/CharacterSelect/EnvironmentController.cs`
- `Assets/Scripts/UI/CharacterSelect/VeilTearTransition.cs`
- `Assets/Scripts/UI/CharacterSelect/CharacterSelectControllerAAA.cs` (if exists)
- `Assets/UI/Screens/CharacterSelectAAA.uxml` (if exists)

**IMPORTANT:** Do NOT delete:
- The OLD `HeroStageController.cs` -- already replaced by our new one
- The OLD `CharacterSelect.uxml` -- already replaced by our new one
- The OLD `CharacterSelect.uss` -- already replaced by our new one

**Step 1: Delete old files via git**

```bash
git checkout -b feature/cs-phase-8
git rm Assets/Scripts/UI/CharacterSelect/CharacterSelectController.cs
git rm Assets/Scripts/UI/CharacterSelect/HeroVFXController.cs
git rm Assets/Scripts/UI/CharacterSelect/CharacterSelectVFXController.cs
git rm Assets/Scripts/UI/CharacterSelect/EnvironmentController.cs
git rm Assets/Scripts/UI/CharacterSelect/VeilTearTransition.cs
# Only if they exist:
git rm Assets/Scripts/UI/CharacterSelect/CharacterSelectControllerAAA.cs 2>/dev/null || true
git rm Assets/UI/Screens/CharacterSelectAAA.uxml 2>/dev/null || true
```

**Step 2: Verify Unity compilation**

Run: Unity domain reload.
Expected: Compilation errors from missing references in scene. These will be fixed in Task 8.2.

**Step 3: Commit**

```bash
git commit -m "chore(charselect): delete legacy character select files

Removed 7 old files (~5,578 lines of god-class code).
Preserved: HeroData.cs, GameDatabase.cs, heroes.json, Enums.cs"
```

---

### Task 8.2: Scene Wiring

This step must be done via Unity Editor (MCP Unity tools or manually):

1. Open the CharacterSelect scene
2. Create a new empty GameObject named "CharacterSelectSystem"
3. Add components to it:
   - `CharacterSelectManager`
   - `HeroStageController`
   - `HeroDataPanelController`
   - `HeroStatsPanelController`
   - `CarouselController`
   - `CharSelectEnvironmentController`
   - `TransitionController`
4. Assign the UIDocument reference on each component (same UIDocument)
5. Assign HeroDisplayConfig ScriptableObjects to the Manager's `_heroConfigs` array
6. Set the UIDocument's source asset to `CharacterSelect.uxml`
7. Add `CharacterSelect.uss` as a stylesheet
8. Create 4 HeroDisplayConfig ScriptableObject assets:
   - `Assets/Resources/CharacterSelect/HeroDisplayConfigs/VexDisplayConfig.asset`
   - `Assets/Resources/CharacterSelect/HeroDisplayConfigs/SeraphinaDisplayConfig.asset`
   - `Assets/Resources/CharacterSelect/HeroDisplayConfigs/OrionDisplayConfig.asset`
   - `Assets/Resources/CharacterSelect/HeroDisplayConfigs/NyxDisplayConfig.asset`
9. Configure each with:
   - `heroId`: matching hero_id from JSON
   - Theme colors from design doc (Section 2.5)
   - Leave modelPrefab null (placeholder capsules)
   - Leave animation clips null (procedural idle)

**Step 1: Create ScriptableObject assets and wire scene**

Use MCP Unity tools or manual Unity Editor work.

**Step 2: Verify**

Play mode: Character select screen loads, shows placeholder capsule, left/right panels populated, carousel works, embark flow works.

**Step 3: Commit**

```bash
git add -A Assets/Resources/CharacterSelect/
git add Assets/Scenes/CharacterSelect.unity
git commit -m "feat(charselect): wire scene with new controllers + SO configs

4 HeroDisplayConfig assets created. All controllers wired to
UIDocument. Scene loads with placeholder capsules and full UI."
```

---

### Task 8.3: Phase 8 Review & Merge

- Opus code review (all files compile, scene works)
- CLI reviewers validate
- **Integration test in Play mode**
- Memory saves
- Merge into feature branch

---

## Phase 9: Final Integration & Polish

> End-to-end testing, edge cases, and quality pass.

### Task 9.1: Integration Testing Checklist

Manually verify in Unity Play mode:

- [ ] Screen loads without errors
- [ ] First hero (alphabetical) displayed by default
- [ ] Left panel shows: name, title, quote, path, role, resource, HP/ATK/DEF/SPD
- [ ] Right panel shows: 6 attribute bars with correct values, abilities list
- [ ] Carousel shows 4 hero cards + 1 teaser
- [ ] Click carousel card -> hero switches
- [ ] Left/Right arrows -> hero navigates with wrap-around
- [ ] Arrow keys / gamepad -> hero navigates
- [ ] Enter/Space -> shows confirm popup
- [ ] Escape -> closes confirm popup or goes back
- [ ] Embark button text updates with hero name
- [ ] Embark glow activates
- [ ] Confirm popup shows correct hero name + title
- [ ] Confirm -> creates save + loads Overworld scene
- [ ] Cancel -> returns to browsing
- [ ] Back button -> returns to MainMenu
- [ ] Placeholder capsule rotates on drag
- [ ] Placeholder capsule has procedural breathing
- [ ] Theme colors change per hero
- [ ] Glass panels have correct styling
- [ ] No console errors or warnings
- [ ] Champion monster section populated from starter_monster_id

### Task 9.2: Final Merge to Feature Branch

```bash
git checkout feature/character-select-rebuild
git merge feature/cs-phase-8 --no-ff -m "merge: phase 8 - scene wiring and old file deletion"
```

### Task 9.3: Final Review & Memory

- Full Opus sign-off on entire feature
- CLI reviewers validate complete implementation
- Save comprehensive memory: all phases, decisions, architecture
- Merge feature branch to master (after user approval)

```bash
git checkout master
git merge feature/character-select-rebuild --no-ff -m "feat: complete character select screen rebuild

Deleted ~5,578 lines of legacy god-class code.
Created ~2,660 lines across 12 focused files.
Sub-controller architecture with event-driven communication.
ScriptableObject configuration for zero-code hero addition.
RenderTexture 3D pipeline with 5-light rig.
USS theme system with GPU-accelerated transitions.
Glass panel aesthetic with per-hero color theming.
Embark confirmation flow with breathing glow.
Champion monster display in shared RenderTexture.
Full keyboard/gamepad navigation support."
```

---

## Summary of All Phases

| Phase | Files | Purpose | Lines |
|-------|-------|---------|-------|
| 1 | HeroDisplayConfig.cs, CharSelectEvents.cs | Data foundation | ~110 |
| 2 | CharacterSelect.uxml, CharacterSelect.uss | UI skeleton | ~800 |
| 3 | CharacterSelectManager.cs | Orchestrator | ~300 |
| 4 | HeroStageController.cs | 3D pipeline | ~350 |
| 5 | HeroDataPanelController.cs, HeroStatsPanelController.cs | UI panels | ~350 |
| 6 | CarouselController.cs | Hero selection strip | ~200 |
| 7 | CharSelectEnvironmentController.cs, TransitionController.cs | Atmosphere | ~150 |
| 8 | Delete old files + scene wiring | Integration | -5,578 |
| 9 | Testing + final merge | QA | 0 |

**Total new code: ~2,260 lines across 10 C# files + 2 UI files**
**Total deleted: ~5,578 lines across 10 legacy files**
**Net reduction: ~3,318 lines (60% reduction)**

---

*End of implementation plan.*
