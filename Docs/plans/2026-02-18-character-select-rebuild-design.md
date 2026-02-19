# Character Select Screen: Complete Rebuild Design
**Date:** 2026-02-18
**Status:** APPROVED
**Author:** Claude (Head Software Engineer)
**Research Contributors:** Animation Research Agent, UI Toolkit Research Agent, AAA Game Analysis Agent

---

## 1. Executive Summary

Complete deletion and rebuild of the character selection screen. The current implementation spans ~5,578 lines across 8 bloated files with god-class anti-patterns. The rebuild targets ~2,660 lines across 12 focused files -- a 52% reduction with MORE functionality.

**Design Name:** "The Veil Tear Showcase"
**Visual Direction:** Gothic Fantasy + Stylish Modern hybrid
**Architecture:** Sub-controller pattern with ScriptableObject configuration

### Priority Order
1. **CODE STRENGTH** -- Clean, robust, testable, SOLID principles
2. **EASY TO EXTEND** -- Adding a new hero = JSON entry + ScriptableObject asset. Zero code changes.
3. **AAA VISUALS** -- Cinematic character presentation with dynamic lighting, veil tear transitions, glass panel UI
4. **FULLY FUNCTIONAL** -- Complete end-to-end flow from hero browsing to embark

---

## 2. Visual Concept: "The Veil Tear Showcase"

The screen feels like peering through a dimensional rift. Each hero exists in their own pocket of the Veil -- selecting them tears open a window to their world. No other game does this. It's uniquely VeilBreakers.

### 2.1 Screen Layout (16:9)

```
+----------------------------------------------------------------------+
|  ░░░░░░░░░░ ATMOSPHERIC BACKGROUND (path-themed gradient) ░░░░░░░░  |
|  ░░░░░░░░░░░░░░░ ambient particles in brand color ░░░░░░░░░░░░░░░░  |
|                                                                      |
|  +---------------+  +----------------------------+  +--------------+ |
|  |  HERO INFO    |  |                            |  |  ATTRIBUTES  | |
|  |  ------------ |  |      3D HERO MODEL         |  |  ----------  | |
|  |  VEX          |  |     (RenderTexture)        |  |  STR ██████ | |
|  |  The Warden   |  |                            |  |  DEX ███    | |
|  |  "I hold..."  |  |         / \                |  |  CON ██████ | |
|  |               |  |        |   |               |  |  INT ███    | |
|  |  PATH: IRON   |  |        |   |   <- 55%      |  |  WIS ████   | |
|  |  ROLE: TANK   |  |        |   |               |  |  CHA ███    | |
|  |  RES:  GUARD  |  |         \ /                |  |              | |
|  |               |  |                            |  |  ABILITIES   | |
|  |  +--HP--+ATK+ |  |  +----+                    |  |  ----------  | |
|  |  | 120  | 14 | |  |  |CHMP|  Champion Monster |  |  Shield Wall | |
|  |  +--DEF-+SPD+ |  |  |MON |  positioned near   |  |  Iron Guard  | |
|  |  |  18  |  8 | |  |  +----+  hero model        |  |  Veil Strike | |
|  |  +------+----+ |  |    [drag to rotate]       |  |  Fortify     | |
|  |               |  +----------------------------+  |  Last Stand  | |
|  |  CHAMPION     |                                   +--------------+ |
|  |  MONSTER      |                                                    |
|  |  Skitter-T.   |                                                    |
|  |  IRON / TANK  |                                                    |
|  +---------------+                                                    |
|                                                                      |
|             +------+------+------+------+------+                     |
|  < PREV     | VEX  |SERA  |ORION | NYX  |  ?   |     NEXT >         |
|             |  *   |      |      |      | SOON |  HERO 1 / 4        |
|             +------+------+------+------+------+                     |
|                                                                      |
|   < BACK         +============================+                      |
|                  |  ✦ EMBARK AS VEX ✦         |                      |
|                  | (breathing glow on select)  |                      |
|                  |  Begin Your Journey         |                      |
|                  +============================+                      |
+----------------------------------------------------------------------+
```

**Key layout changes from v1:**
- Carousel is **centered directly above** the Embark button (vertically aligned)
- Embark button **breathes a highlight confirmation** when a character is selected
- Champion Monster's 3D model appears **near the hero** in the RenderTexture stage
- Companion section renamed to **"CHAMPION MONSTER"**

### 2.2 Visual Hierarchy

1. **CENTER (55%):** 3D hero model dominates -- this is what players look at
2. **CHAMPION MONSTER:** 3D model near hero feet/side in the RenderTexture (smaller scale)
3. **LEFT PANEL:** Hero identity, starter stats, champion info -- glass panel, slides in
4. **RIGHT PANEL:** D&D attributes with animated bars, abilities -- glass panel, slides in
5. **BOTTOM STRIP:** Hero carousel -- centered directly above embark button
6. **BOTTOM CENTER:** Embark button -- breathing highlight on select, intensifies on hover
7. **BACKGROUND:** Path-themed gradient with ambient brand-colored particles

### 2.3 Embark Confirmation Flow

Selecting a hero triggers a multi-stage confirmation:

```
1. Player selects hero via carousel/arrows
   -> Embark button begins BREATHING GLOW animation
      (opacity pulses 0.6 -> 1.0 in hero accent color, 1.5s cycle)
   -> Button text updates: "EMBARK AS [HERO_NAME]"

2. Player clicks Embark button
   -> Confirmation popup slides in (glass panel centered)
      +================================+
      |     CONFIRM YOUR CHAMPION      |
      |                                |
      |   You will begin your journey  |
      |   as VEX, THE WARDEN           |
      |                                |
      |   [  CONFIRM  ]  [ CANCEL ]    |
      +================================+
   -> Background dims slightly (40% black overlay)
   -> Hero model plays "selected" animation hold

3a. Player clicks CONFIRM
    -> OnEmbarkConfirmed fires
    -> Hero plays embark animation
    -> Screen transitions to gameplay

3b. Player clicks CANCEL
    -> Popup slides out
    -> Background undims
    -> Hero returns to showcase/idle
    -> Embark button continues breathing
```

### 2.4 Champion Monster Display

The companion section is renamed to **"CHAMPION MONSTER"** throughout.

**3D Model Display Strategy:**
- The champion monster's 3D model is loaded into the SAME RenderTexture scene as the hero
- Positioned at the hero's feet, slightly to the side (offset configurable per hero in HeroDisplayConfig)
- Scaled to ~30-40% of hero height for visual balance
- Has its own simple idle animation (breathing/bobbing)
- If no monster model prefab exists, falls back to data-only display in the left panel (name, brand, role, stats)

**Why in-scene rather than a separate panel image:**
- Looks more alive and connected to the hero
- Shares the same lighting rig for visual consistency
- Avoids the "small 3D model in a tiny box" problem
- Players see their hero + monster as a team

### 2.5 Per-Hero Visual Theming

Each hero gets a distinct color palette driven by their Path and Brand:

| Hero | Path | Primary Color | Secondary | Accent | Mood |
|------|------|---------------|-----------|--------|------|
| Vex | IRONBOUND | Deep crimson | Ember orange | Warm gold | Forge heat, determination |
| Seraphina | FANGBORN | Toxic green | Swamp purple | Sickly yellow | Primal, venomous |
| Orion | VOIDTOUCHED | Deep indigo | Cosmic blue | Electric cyan | Otherworldly, arcane |
| Nyx | UNCHAINED | Blood red | Shadow black | Violet | Dark freedom, rebellion |

Colors affect: lighting rig, panel borders, stat bar fills, carousel selection glow, ambient particles, vignette tint, embark button glow.

### 2.4 Transition Sequence (Hero Switching)

```
Frame 0-10:   Current hero fades, veil crack lines appear at edges
Frame 10-20:  Screen tears briefly, flash of new hero's brand color
Frame 20-30:  New hero fades in, lighting crossfades to new palette
Frame 30-50:  Info panels slide in with new data, stat bars fill with animation
```

Total transition: ~0.8-1.2 seconds. Fast enough to feel responsive, slow enough to feel cinematic.

### 2.5 USS Visual Style

- **Glass panels:** `rgba(10, 10, 15, 0.88)` backgrounds with 1px luminous borders
- **Border glow:** Per-hero brand color at 30% opacity, transitioning smoothly
- **Typography:** Clean sans-serif, ALL CAPS for labels with letter-spacing, mixed case for descriptions
- **Stat bars:** 8px tracks with brand-colored gradient fills, `ease-out-cubic` 0.6s transitions
- **Carousel cards:** 80x100px, selected card scales 1.15x with brand-colored border
- **Embark button:** Breathing highlight confirmation glow (opacity 0.6-1.0, 1.5s cycle in hero accent color). Activates on hero selection. Intensifies on hover. Visual signal: "a character has been selected, click to embark"
- **Confirm popup:** Glass panel centered, `rgba(10, 10, 15, 0.92)` bg, 40% dim overlay behind it
- **Vignette:** Dark edges that pulse subtly with corruption-themed energy

---

## 3. Architecture

### 3.1 Controller Hierarchy

```
CharacterSelectManager (MonoBehaviour) -- Orchestrator (~300 lines)
|
+-- HeroStageController             -- Camera, lighting, RenderTexture, model loading (~350 lines)
+-- HeroAnimationController          -- Animation state machine per hero (~250 lines)
+-- HeroDataPanelController          -- Left panel: name/title/stats/companion (~200 lines)
+-- HeroStatsPanelController         -- Right panel: attribute bars/abilities (~150 lines)
+-- CarouselController               -- Bottom hero selection strip (~200 lines)
+-- TransitionController             -- Veil tear effects, hero switch sequences (~200 lines)
+-- CharSelectEnvironmentController  -- Background gradients, ambient particles (~150 lines)
+-- CharSelectInputHandler           -- Keyboard/gamepad/mouse routing (~150 lines)
```

**Communication:** Events on the CharacterSelectManager. Controllers subscribe to what they need. No direct sibling-to-sibling references.

### 3.2 Event System

```csharp
// On CharacterSelectManager:
public event Action<int, HeroData, HeroDisplayConfig> OnHeroChanged;
public event Action<HeroData> OnHeroDataLoaded;
public event Action OnHeroSelected;           // Triggers embark button breathing glow
public event Action OnEmbarkRequested;         // Embark button clicked -> show confirm popup
public event Action OnEmbarkConfirmed;         // Confirm clicked -> proceed to gameplay
public event Action OnEmbarkCancelled;         // Cancel clicked -> dismiss popup
public event Action OnScreenReady;
public event Action OnScreenExiting;
```

Flow:
1. User navigates carousel -> CarouselController fires `OnSlotSelected`
2. Manager receives it, loads data, fires `OnHeroChanged`
3. All controllers react: stage swaps model, panels update data, environment shifts colors
4. TransitionController orchestrates the visual sequence timing
5. `OnHeroSelected` fires -> Embark button starts breathing highlight animation
6. User clicks Embark -> `OnEmbarkRequested` fires -> Confirm popup appears
7. User clicks Confirm -> `OnEmbarkConfirmed` fires -> Embark animation + scene transition
8. User clicks Cancel -> `OnEmbarkCancelled` fires -> Popup dismisses, return to browsing

### 3.3 Data Flow

```
heroes.json --> GameDatabase.Instance --> HeroData objects
                                              |
                                              v
                              CharacterSelectManager
                              distributes to controllers
                                              |
                        +-----+-----+----+----+----+----+
                        |     |     |    |    |    |    |
                      Stage Anim  Data Stats Carousel Trans Env
```

### 3.4 ScriptableObject Configuration

```csharp
[CreateAssetMenu(menuName = "VeilBreakers/Hero Display Config")]
public class HeroDisplayConfig : ScriptableObject
{
    [Header("Identity")]
    public string heroId;                    // Must match heroes.json id

    [Header("Camera")]
    public Vector3 cameraOffset = new Vector3(0, 1.2f, -3f);
    public float cameraFOV = 30f;
    public float cameraFramePadding = 0.15f;

    [Header("Lighting")]
    public Color keyLightColor = Color.white;
    public float keyLightIntensity = 1.2f;
    public Color fillLightColor = new Color(0.4f, 0.5f, 0.6f);
    public float fillLightIntensity = 0.6f;
    public Color rimLightColor = Color.cyan;
    public float rimLightIntensity = 1.5f;

    [Header("Environment / Theme Colors")]
    public Color primaryColor;               // Main brand color
    public Color secondaryColor;             // Supporting color
    public Color accentColor;                // Highlights, glows

    [Header("Model")]
    public GameObject modelPrefab;           // null = use placeholder

    [Header("Animations")]
    public AnimationClip idleClip;
    public AnimationClip[] idleVariantClips; // Random selection pool
    public AnimationClip selectedClip;
    public AnimationClip showcaseClip;
    public AnimationClip embarkClip;

    [Header("Animation Timing")]
    public float idleVariantMinDelay = 10f;  // FFXIV-proven cadence
    public float idleVariantMaxDelay = 16f;
    public float selectedToShowcaseDelay = 2f;
    public float showcaseToIdleDelay = 4f;
    public float crossfadeDuration = 0.25f;

    [Header("Audio")]
    public AudioClip selectionSFX;            // Played on hero selected
    public AudioClip embarkSFX;               // Played on embark confirm
    public AudioClip ambientLoop;             // Background atmosphere per hero

    [Header("Champion Monster")]
    public GameObject championModelPrefab;    // null = data-only display
    public Vector3 championOffset = new Vector3(0.5f, 0f, 0.3f);
    public float championScale = 0.35f;       // Relative to hero
    public AnimationClip championIdleClip;    // Simple idle/bob

    [Header("VFX")]
    public Color particleColor;
    public GameObject selectionVFXPrefab;     // null = skip VFX
}
```

**Adding a new hero requires ZERO code changes:**
1. Add entry to `heroes.json`
2. Create `HeroDisplayConfig` ScriptableObject in Unity Editor
3. Assign model prefab + animation clips (or leave null for placeholders)
4. Carousel auto-generates from hero count

### 3.5 USS Theme System

Per-hero CSS class toggling (since USS variables can't be set from C# at runtime):

```css
/* Root-level theme classes */
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

/* Elements reference variables */
.stat-bar-fill   { background-color: var(--hero-primary); }
.panel-border    { border-color: var(--hero-accent); }
.carousel-glow   { border-color: var(--hero-primary); }
.embark-glow     { background-color: var(--hero-accent); }
```

Hero switch in C#: remove old theme class, add new one. USS transitions handle the smooth color crossfade.

---

## 4. Animation System

### 4.1 Phase State Machine

```
                    +-----------+
                    |   Entry   |
                    +-----+-----+
                          |
                          v
                  +-------+-------+
          +------>|     Idle      |<------+
          |       |  (looping)   |       |
          |       +---+------+---+       |
          |           |      |           |
          |   10-16s  |      | "Select"  |
          |   timer   |      | trigger   |
          |           v      v           |
          |  +--------+-+ +-+--------+  |
          |  |IdleVariant| | Selected |  |
          |  |(play once)| |(play once)| |
          |  +--------+--+ +-+--------+ |
          |           |       |          |
          |  exit time|  exit time       |
          |           |       v          |
          +-----------+  +----+------+   |
                         | Showcase  |   |
                         | (looping) +---+
                         +----+------+ "Deselect"
                              |        trigger
                         "Embark"
                         trigger
                              v
                        +-----+-----+
                        |  Embark   |
                        |(play once)|
                        +-----------+
```

### 4.2 Phase Definitions

| Phase | Type | Duration | Description |
|-------|------|----------|-------------|
| **Idle** | Loop | 10-16s before variant | Base breathing/standing. Home state. |
| **IdleVariant** | Play once | Clip length (~2-4s) | Personality animation: adjust gear, look around, crack knuckles |
| **Selected** | Play once | ~1-2s | Hero acknowledges selection: nod, weapon draw, confident stance |
| **Showcase** | Loop | Until deselect/embark | Signature pose: Vex arms crossed, Seraphina predatory crouch |
| **Embark** | Play once | ~2-3s | Dramatic exit: weapon flourish, veil tear behind them |

### 4.3 Implementation Details

- **One shared base AnimatorController** with named states matching phases above
- **Per-hero AnimatorOverrideController** that swaps in specific clips
- **`CrossFadeInFixedTime`** (not `CrossFade`) for consistent transition durations regardless of clip length
- **`ApplyOverrides` batch method** on AnimatorOverrideController for efficient multi-clip swapping
- **`StateMachineBehaviour`** callbacks on states for VFX timing hooks
- **Animator Parameters:** `PlayVariant` (Trigger), `Select` (Trigger), `Deselect` (Trigger), `Embark` (Trigger), `Reset` (Trigger)

### 4.4 Crossfade Durations

| Transition | Duration | Rationale |
|-----------|----------|-----------|
| Idle -> IdleVariant | 0.20s | Fast, subtle personality shift |
| IdleVariant -> Idle | 0.25s | Smooth return |
| Idle -> Selected | 0.15s | Snappy response to player input |
| Selected -> Showcase | Exit time @90% | Natural flow from selected clip |
| Showcase -> Idle | 0.30s | Smooth, unhurried deselect |
| Showcase -> Embark | 0.10s | Dramatic, immediate |

### 4.5 Placeholder Fallback Chain

Since ALL models are placeholders right now:

1. **If AnimatorOverrideController has clips** -> Use them normally
2. **If no clips assigned** -> Generic humanoid idle (retargets universally via Mixamo)
3. **If no animator at all** -> Procedural gentle idle:
   ```csharp
   // Subtle breathing scale pulse + slow Y rotation
   float breath = 1f + Mathf.Sin(Time.time * 1.2f) * 0.005f;
   model.localScale = baseScale * breath;
   model.Rotate(Vector3.up, 5f * Time.deltaTime);
   ```
4. **Never show a T-pose.** The system always has a visual fallback.

---

## 5. Graceful Degradation

Every visual feature degrades cleanly (critical since everything starts as placeholder):

| Missing Asset | Fallback Behavior |
|---|---|
| No model prefab in config | Brand-colored capsule with emissive material |
| No animation clips | Procedural breathing + slow rotation |
| No VFX prefab | Skip VFX silently, no errors |
| No lighting config | White key + neutral fill + subtle rim defaults |
| No companion data in JSON | Hide companion section entirely |
| No HeroDisplayConfig SO | Auto-generate defaults from HeroData color fields |
| No portrait image | Solid brand-color fill with hero initial |

---

## 6. File Structure

### 6.1 Files to DELETE (10 files, ~5,578 lines)

```
Assets/Scripts/UI/CharacterSelect/
  CharacterSelectController.cs       (2674 lines) -- god class
  HeroStageController.cs             (1892 lines) -- god class
  HeroVFXController.cs               (381 lines)
  CharacterSelectVFXController.cs    (507 lines)
  EnvironmentController.cs           (137 lines)
  VeilTearTransition.cs              (167 lines)
  CharacterSelectControllerAAA.cs    (unused)

Assets/UI/
  Screens/CharacterSelect.uxml       (275 lines)
  Screens/CharacterSelectAAA.uxml    (unused)
  Styles/CharacterSelect.uss         (820 lines)
```

### 6.2 Files to PRESERVE

```
Assets/Resources/Data/heroes.json    -- hero data source (DO NOT TOUCH)
Assets/Scripts/Data/HeroData.cs      -- data model class (DO NOT TOUCH)
Assets/Scripts/Data/GameDatabase.cs   -- data provider singleton (DO NOT TOUCH)
```

### 6.3 Files to CREATE (12 files, ~2,660 lines)

```
Assets/Scripts/UI/CharacterSelect/
  CharacterSelectManager.cs              (~300 lines) Orchestrator
  HeroStageController.cs                 (~350 lines) 3D rendering pipeline
  HeroAnimationController.cs             (~250 lines) Animation state machine
  HeroDataPanelController.cs             (~200 lines) Left info panel
  HeroStatsPanelController.cs            (~150 lines) Right stats/abilities panel
  CarouselController.cs                  (~200 lines) Hero selection strip
  TransitionController.cs                (~200 lines) Veil tear transitions
  CharSelectEnvironmentController.cs     (~150 lines) Background atmosphere
  CharSelectInputHandler.cs              (~150 lines) Unified input routing
  CharSelectEvents.cs                    (~30 lines)  Shared event definitions

Assets/Scripts/Data/
  HeroDisplayConfig.cs                   (~80 lines)  ScriptableObject definition

Assets/UI/
  Screens/CharacterSelect.uxml          (~200 lines) UI structure
  Styles/CharacterSelect.uss            (~600 lines) Styling + themes + transitions
```

### 6.4 Assets to CREATE

```
Assets/Resources/CharacterSelect/
  HeroDisplayConfigs/
    VexDisplayConfig.asset
    SeraphinaDisplayConfig.asset
    OrionDisplayConfig.asset
    NyxDisplayConfig.asset

  Prefabs/
    PlaceholderHeroModel.prefab          Simple capsule with brand-colored material

  Animations/
    CharSelectBaseController.controller  Shared animator with phase states
```

---

## 7. Technical Specifications

### 7.1 RenderTexture Pipeline

- **Resolution:** 1024x1536 (portrait aspect matching hero display area)
- **Format:** ARGB32 with transparency
- **MSAA:** 4x anti-aliasing
- **Depth Buffer:** 24-bit
- **Camera:** Dedicated, FOV 30, renders only "CharacterPreview" layer
- **Filter Mode:** Bilinear
- Assigned via `style.backgroundImage = Background.FromRenderTexture(rt)`

### 7.2 Lighting Rig (5 lights)

| Light | Type | Purpose | Default |
|-------|------|---------|---------|
| Key | Directional | Main illumination | White, intensity 1.2 |
| Fill | Point | Shadow softening | Neutral warm, intensity 0.6 |
| Rim | Point | Silhouette separation | Brand accent color, intensity 1.5 |
| Face | Spot | Face detail | Warm white, intensity 0.4 |
| Ground | Point | Ground plane reflection | Brand secondary, intensity 0.3 |

All light colors crossfade via `Color.Lerp` during hero transitions.

### 7.3 UI Performance Rules

- Pre-set `UsageHints.DynamicTransform` on all elements that translate/scale/rotate
- Pre-set `UsageHints.DynamicColor` on all elements that change color/opacity
- Use `DisplayStyle.None` for fully hidden elements (cheaper than `opacity: 0`)
- Prefer `translate`/`scale`/`opacity` transitions over `width`/`margin` (GPU vs layout recalc)
- Batch all data updates in a single frame during hero switch
- Cache all `Q<T>()` element references in initialization, never re-query

### 7.4 Input Handling

| Input | Action |
|-------|--------|
| Left Arrow / A / Gamepad Left | Previous hero |
| Right Arrow / D / Gamepad Right | Next hero |
| Enter / Space / Gamepad A | Embark (if hero selected) |
| Escape / Backspace / Gamepad B | Back to main menu |
| Mouse drag on hero stage | Rotate model |
| Mouse click on carousel slot | Select hero |
| `NavigationMoveEvent` | UI Toolkit built-in focus navigation |
| `NavigationSubmitEvent` | UI Toolkit built-in submit |

### 7.5 Custom VisualElement Components

Three reusable `[UxmlElement]` components:

1. **`StatBar`** -- Reused for 6 attribute bars (STR/DEX/CON/INT/WIS/CHA)
   - Properties: `StatName`, `FillPercent`, `FillColor`
   - Animated fill via USS `width` transition (acceptable for 6 bars)

2. **`HeroCard`** -- Reused per hero in carousel
   - Properties: `HeroId`, `HeroName`, `IsSelected`, `IsTeaser`
   - Scale/opacity/border transitions on selection

3. **`AbilitySlot`** -- Reused for 5 ability displays
   - Properties: `AbilityName`, `Description`

---

## 8. Data Preservation Checklist

All data currently displayed MUST appear in the new screen:

- [x] Hero name (e.g., "VEX")
- [x] Hero title (e.g., "THE WARDEN")
- [x] Hero quote
- [x] Path (IRONBOUND, FANGBORN, VOIDTOUCHED, UNCHAINED)
- [x] Role (TANK, DPS, MAGE, HYBRID)
- [x] Resource type (GUARD, FURY, MANA, SHADOW)
- [x] Starter stats: HP, ATK, DEF, SPD in chip grid
- [x] D&D attributes: STR, DEX, CON, INT, WIS, CHA with animated bar fills
- [x] 5 abilities with name + description
- [x] Champion Monster: monster name, brand badge, role, stats (HP/ATK/DEF/SPD)
- [x] Hero carousel with dynamic slot count (currently 4 heroes + 1 teaser)
- [x] Hero index indicator ("HERO 1 / 4")
- [x] Embark button with dynamic hero name ("EMBARK AS VEX")
- [x] Embark button breathing highlight animation on hero selection
- [x] Confirm/Cancel popup on embark click before proceeding
- [x] Champion monster 3D model near hero in RenderTexture (with fallback)
- [x] Back button to MainMenu scene
- [x] Save system integration on embark (CreateOrRotateNewGameSave)

---

## 9. Implementation Order

1. `HeroDisplayConfig.cs` -- Data foundation (ScriptableObject definition)
2. `CharSelectEvents.cs` -- Shared event type definitions
3. `CharacterSelect.uxml` -- UI skeleton with all named elements
4. `CharacterSelect.uss` -- Complete styling with theme classes and transitions
5. `CharacterSelectManager.cs` -- Orchestrator (wiring, lifecycle, events)
6. `HeroStageController.cs` -- 3D rendering pipeline (camera, lighting, RenderTexture, model loading)
7. `HeroAnimationController.cs` -- Animation state machine with AnimatorOverrideController
8. `HeroDataPanelController.cs` -- Left info panel population
9. `HeroStatsPanelController.cs` -- Right stats/abilities panel with animated bars
10. `CarouselController.cs` -- Hero selection strip with navigation
11. `TransitionController.cs` -- Veil tear transition effects
12. `CharSelectEnvironmentController.cs` -- Background atmosphere
13. `CharSelectInputHandler.cs` -- Unified input routing
14. Placeholder model prefab + 4 ScriptableObject config assets
15. Base AnimatorController asset with phase states
16. Delete old files
17. Integration testing + scene wiring

---

## 10. Research Sources

### Games Analyzed
- Genshin Impact (character wish/preview, elemental backgrounds)
- Monster Hunter (3D preview rotation, lighting rigs)
- Persona 5 (bold UI design, stylish transitions)
- Final Fantasy XIV/XVI (job animations, idle cycling 10-30s cadence)
- Pokemon (type-based theming, starter selection ritual)
- Honkai Star Rail / Wuthering Waves (cinematic introductions)
- League of Legends (champion spotlight, timeline sequencing)

### Technical References
- Unity AnimatorOverrideController docs (batch ApplyOverrides pattern)
- Unity USS Transitions (animatable properties, timing functions)
- Unity UI Toolkit Performance Guide (UsageHints, DynamicTransform)
- Unity NavigationMoveEvent/SubmitEvent (built-in gamepad support)
- Unity [UxmlElement] attribute (Unity 6 custom control pattern)
- Game Programming Patterns: State (pushdown automaton for animation FSM)
- Riot Games tech blog (timeline-based animation sequencing)
- FFXIV idle animation cadence (10-30s variant frequency)

---

## 11. Risk Assessment

| Risk | Mitigation |
|------|------------|
| Placeholder models look bad | Procedural idle animation + brand-colored emissive material makes capsules look intentional |
| Animation clips not yet created | Full fallback chain: clips -> generic idle -> procedural animation -> never T-pose |
| Performance on hero switch | Batch UI updates, use USS transitions (GPU-accelerated), pre-set UsageHints |
| RenderTexture visual quality | 1024x1536 with 4x MSAA, proper lighting rig, bilinear filtering |
| Extending to 5+ heroes | Carousel auto-generates from data, ScriptableObject configs per hero, zero code changes |
| Future maintainability | 12 focused files under 350 lines each, consistent patterns, clear naming, event-driven decoupling |

---

## 12. External Review Sign-Off

### Reviewers
- **Gemini** (Google AI) -- **APPROVED**
- **Codex** (OpenAI GPT-5.3) -- **CONDITIONAL APPROVE** (lifecycle/validation/QA gates)
- **Kimi** (Moonshot AI) -- **APPROVED**

### Consensus Items Incorporated

**High-Priority (all 3 flagged):**
1. **Event lifecycle contracts** -- All controllers must subscribe in `OnEnable`, unsubscribe in `OnDisable`. Add null guards on event invocations. Document teardown order.
2. **Audio specification** -- Add to HeroDisplayConfig: `AudioClip selectionSFX`, `AudioClip embarkSFX`, `AudioClip ambientLoop`. Each hero gets distinct audio identity.
3. **Localization readiness** -- All displayed strings routed through localization key system. No hardcoded UI text in C# controllers.
4. **Config validation** -- Create `HeroDisplayConfigValidator` editor script that validates `heroId` matches `heroes.json`, logs warnings for missing assets. Runs on domain reload.

**Medium-Priority (2/3 flagged):**
5. **RenderTexture quality tiers** -- Expose resolution/MSAA as quality settings. Default: 1024x1536/4x MSAA. Low: 512x768/2x. Mobile: 256x384/none.
6. **Accessibility** -- Contrast ratio checks for per-hero themes. Non-color-only selection cues (border shape, icon badges). Focus ring states for keyboard/gamepad navigation. Reduced motion option (instant transitions).
7. **Interruptible transitions** -- Hero switch transitions can be interrupted/snapped by new selection. Never queue transitions.
8. **Champion monster offset** -- Per-monster positioning data in addition to per-hero champion offset. Monster data gets optional `Vector3 displayOffset` and `float displayScale`.

**Code Implementation Tips (consensus):**
- Cache all `Animator.StringToHash()` results as static readonly ints
- Use `TransitionEndEvent` for USS animation sequencing (not Coroutines)
- Async model loading via `Resources.LoadAsync` to prevent frame hitches
- Use `scaleX` + `transform-origin: left` for stat bar fills (GPU, not layout recalc)
- Centralize fallback decisions in a single resolver service
- Avoid `backdrop-filter: blur()` on glass panels -- use pre-blurred textures

---

*End of design document.*
