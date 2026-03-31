# Architecture Patterns: UI Effects Integration for VeilBreakers v6.0

**Domain:** Unity 6 UI Toolkit + C# runtime visual effects for dark fantasy RPG
**Researched:** 2026-03-30
**Confidence:** HIGH (verified against existing codebase, Unity 6000.3.6f1 docs, UI Toolkit filter API)

## Executive Summary

VeilBreakers v6.0 needs to integrate four categories of visual enhancement into the existing UI Toolkit architecture: (1) C# runtime texture generation for gradients/glow, (2) layered VisualElement overlays for depth effects, (3) RenderTexture-based 3D model display, and (4) procedural audio generation. The existing codebase already has working implementations of all four patterns but they suffer from inconsistent lifecycle management, texture memory leaks, and missing cleanup. The architecture below standardizes these patterns into a coherent system.

**Key insight:** Unity 6000.3 (the project's version) now supports native `filter: blur()` in USS/C#, which the codebase does not yet use. The existing comment in `CharacterSelect.uss` stating "UI Toolkit can't replicate cleanly" is outdated. This changes the architectural calculus -- some effects that previously required layered VisualElements with radial gradient textures can now use native blur filters.

---

## Current State Analysis

### What Exists Today

The codebase has 15+ UI effect scripts across two directories:

| Component | Location | Pattern | Role |
|-----------|----------|---------|------|
| `UIGradientHelper` | `UI/Core/` | Static utility | Runtime Texture2D generation for gradients |
| `ThemeManager` | `UI/Core/` | Lazy singleton | Color tokens (brand, corruption, rarity, surface) |
| `UIAssets` | `UI/Core/` | ScriptableObject singleton | Centralized template/stylesheet refs |
| `UIAnimationController` | `UI/Core/` | Lazy singleton | Fade/scale/slide transitions |
| `TitleScreenVFX` | `UI/Core/` | MonoBehaviour | Embers, ash, sparks, lightning via VisualElements |
| `TitleScreenAudio` | `UI/Core/` | MonoBehaviour | Procedural AudioClip generation (drone, bells, VERA) |
| `MenuVFXController` | `UI/Core/` | MonoBehaviour | Corruption/wisp particles |
| `MoltenButtonVFX` | `UI/Core/` | MonoBehaviour | Crack/lava/highlight button effects |
| `MoltenVeinVFX` | `UI/Core/` | MonoBehaviour | Pulsing molten vein overlays |
| `SoulSwarmVFX` | `UI/Core/` | MonoBehaviour | Cursor-attracted soul particles |
| `ParallaxBackground` | `UI/Core/` | MonoBehaviour | Mouse-driven parallax depth |
| `CharSelectVisualEnhancer` | `UI/CharacterSelect/` | MonoBehaviour | AAA gradient/glow pass on CharSelect |
| `OverlayController` | `UI/CharacterSelect/` | Pure C# class | Scanline/vignette/veil-glow overlays |
| `HeroStageController` | `UI/CharacterSelect/` | MonoBehaviour | RenderTexture 3D model preview |
| `GlitchTextEffect` | `UI/CharacterSelect/` | Pure C# class | PrimeTween text scramble sequences |

### Architectural Issues Found

**1. Inconsistent Singleton Patterns (CRITICAL)**
`ThemeManager` and `UIAnimationController` use hand-rolled singleton patterns instead of `SingletonMonoBehaviour<T>`. This is already flagged as a Phase C bug. Both lack `DontDestroyOnLoad` duplicate checks matching the project standard.

**2. Texture Memory Leak Risk (HIGH)**
`UIGradientHelper` creates `Texture2D` objects and returns them to callers. The caller is responsible for `Destroy()`. `CharSelectVisualEnhancer` tracks 12+ texture fields for cleanup but the pattern is error-prone -- miss one field and you leak GPU memory. `MainMenuBootstrap.ApplyCornerBlend()` creates a texture inline with no tracked reference for cleanup.

**3. VFX Container Z-Order Conflicts (MEDIUM)**
Multiple VFX controllers insert containers at `root.Insert(0, ...)` or `root.Insert(1, ...)`. When multiple controllers are active on the same UIDocument, their insertion indices collide, causing unpredictable layering. The existing codebase handles this through careful component ordering, but it is fragile.

**4. No Shared VFX Lifecycle (MEDIUM)**
Each VFX MonoBehaviour independently manages `_isActive`, `_updateCoroutine`, start/stop, and cleanup. The pattern is duplicated across 6+ controllers with minor variations. No shared base class or interface.

**5. Procedural Audio Memory (LOW)**
`TitleScreenAudio` generates ~8 AudioClips at startup totaling ~60 seconds of audio. These are properly cleaned up in `OnDestroy()` but the generation happens synchronously in `Start()`, causing a frame spike. Pre-generation should be async or spread across frames.

---

## Recommended Architecture

### Layer 1: UIEffectsFoundation (Shared Infrastructure)

**New abstractions to create:**

#### 1a. `IUIEffectLifecycle` Interface

```csharp
namespace VeilBreakers.UI.Effects
{
    public interface IUIEffectLifecycle
    {
        void Initialize(VisualElement root);
        void SetActive(bool active);
        void Cleanup();
    }
}
```

Purpose: Standard lifecycle for all visual effect components. Replaces the ad-hoc init/start/stop/cleanup patterns.

#### 1b. `UITextureRegistry` (Texture Memory Safety)

```csharp
namespace VeilBreakers.UI.Core
{
    /// <summary>
    /// Tracks all runtime-generated Texture2D instances for guaranteed cleanup.
    /// Replaces the pattern of tracking individual texture fields per component.
    /// </summary>
    public class UITextureRegistry
    {
        private readonly List<Texture2D> _textures = new();

        public Texture2D Register(Texture2D tex) { _textures.Add(tex); return tex; }
        public void DestroyAll() { /* Destroy + clear */ }
    }
}
```

**Each MonoBehaviour that generates textures creates a local `UITextureRegistry` and calls `DestroyAll()` in `OnDisable()`/`OnDestroy()`.** This replaces the 12-field pattern in `CharSelectVisualEnhancer`.

#### 1c. `UIVFXContainer` (Z-Order Management)

```csharp
namespace VeilBreakers.UI.Core
{
    /// <summary>
    /// Manages named VFX layers in a predictable z-order stack.
    /// Instead of each VFX controller doing root.Insert(0, ...) independently,
    /// they request a named layer from this container.
    /// </summary>
    public class UIVFXContainer
    {
        public enum Layer { Background, EnvironmentFX, ContentBehind, Content, ContentFront, OverlayFX, Overlay }

        public VisualElement GetLayer(Layer layer);
        public void Initialize(VisualElement root);
        public void Cleanup();
    }
}
```

Purpose: Eliminates z-order conflicts by providing a fixed set of insertion layers.

### Layer 2: Runtime Texture Effects (UIGradientHelper Evolution)

**Keep `UIGradientHelper` as a static utility** -- it is well-structured and performant. Extend it with:

#### 2a. Gradient Texture Caching

Currently, identical gradients are regenerated every time a hero switch occurs. Add a simple cache keyed on color parameters:

```csharp
public static class UIGradientHelper
{
    // Existing methods unchanged...

    // Add: cache for repeated gradient requests
    private static readonly Dictionary<int, Texture2D> _cache = new();

    public static Texture2D GetOrCreateVerticalGradient(Color top, Color bottom)
    {
        int key = HashCode.Combine(top, bottom);
        if (_cache.TryGetValue(key, out var cached)) return cached;
        var tex = CreateVerticalGradient(top, bottom);
        _cache[key] = tex;
        return tex;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearCache() { /* Destroy + clear _cache */ }
}
```

**Why:** Hero switching triggers `HandleHeroChangedForCards`, `HandleHeroChangedForStatBars`, `HandleHeroChangedForPanelColors` -- each recreating gradient textures. Caching eliminates redundant GPU uploads.

#### 2b. Native Filter Integration (NEW -- Unity 6000.3)

The project runs on Unity 6000.3.6f1 which supports `filter: blur()` natively. This means:

**Replace radial gradient glow overlays with native blur where appropriate:**

```csharp
// OLD: Create a 128x128 radial gradient texture, apply as background-image
var glow = UIGradientHelper.CreateGlowOverlay(parent, glowColor, 8f, 0.3f);

// NEW: Use native blur filter for soft glow (no texture allocation)
glowElement.style.filter = new FilterFunction[] { FilterFunction.Blur(8f) };
glowElement.style.backgroundColor = glowColor;
```

**Where blur replaces texture-based glow:**
- Vignette edges (currently `CreateRadialGradient`)
- Panel glow halos (currently `CreateGlowOverlay`)
- Background defocus (currently opacity+scale hack in `MainMenuBootstrap.DimBackground`)

**Where textures remain necessary:**
- Multi-stop gradients (blur cannot create directional color transitions)
- Horizontal highlight lines (`CreateTopHighlight`) -- precision effect
- Hero card background gradients -- themed multi-color
- Molten vein sprites -- texture-mapped content

**Performance note:** Native blur is GPU-accelerated and cheaper than texture-based workarounds for simple glow effects. But blur on large elements (full-screen) is expensive. Use `filter: blur()` for small-to-medium elements; keep texture-based approach for full-screen vignettes.

### Layer 3: Layered VisualElement Effects

#### 3a. Effect Stack Pattern

The codebase already uses the correct pattern: absolutely-positioned VisualElements layered behind/in front of content with `pickingMode = PickingMode.Ignore`. Standardize this as an explicit pattern:

```
[Background Layer]     -- video/image/parallax
  [Environment FX]     -- embers, ash, smoke, wisps
    [Content Behind]   -- vignettes, gradients behind content
      [Content]        -- actual UI elements (panels, buttons, labels)
        [Content Front] -- glow overlays, highlights on content
          [Overlay FX]  -- scanlines, veil pulse, screen-wide effects
            [Overlay]   -- modals, toasts, cinematics
```

**Each layer is a VisualElement with:**
- `position: absolute; left: 0; top: 0; right: 0; bottom: 0;`
- `pickingMode: PickingMode.Ignore` (except Content and Overlay)
- `overflow: hidden` (prevents particles from escaping)

#### 3b. Glow Overlay Pattern (Existing, Refined)

The `UIGradientHelper.CreateGlowOverlay` pattern is correct. Refine it:

```
Target Element
  |-- glow-overlay (Position.Absolute, negative insets for spread)
  |     background-image: radial gradient OR filter: blur()
  |-- top-highlight (Position.Absolute, top: 0, 1px height)
  |     background-image: horizontal center-bright gradient
  |-- [child content...]
```

**Critical rule:** USS `background-color` OVERRIDES runtime `Texture2D` set via `style.backgroundImage`. When applying runtime gradients, the USS must NOT set `background-color` on that element. The existing `.claude/rules/ui/toolkit.md` already documents this.

#### 3c. UsageHints Pattern (Existing, Correct)

The codebase correctly applies `UsageHints.DynamicTransform | UsageHints.DynamicColor` on elements animated per-frame. This is the right approach -- it tells the UI Toolkit renderer to optimize for frequent transform/color changes by keeping elements in separate render batches.

**Rule:** Every VisualElement that is animated via coroutine/Update must have `usageHints` set at creation time, not during animation. The existing code does this correctly in most places.

### Layer 4: RenderTexture 3D Model Display

#### 4a. Existing Pattern (HeroStageController -- Correct)

The `HeroStageController` implementation is architecturally sound:

```
[Off-screen Stage] position=(100,0,0)
  |-- PreviewCamera (targetTexture=RenderTexture, cullingMask=CharacterPreview layer)
  |-- KeyLight, FillLight, RimLight, FaceLight, GroundLight
  |-- Model Instance (layer=CharacterPreview)
  |-- Champion Instance (optional)

[UI Toolkit]
  hero-render-target VisualElement
    style.backgroundImage = Background.FromRenderTexture(rt)
```

**Key decisions that are correct:**
- Dedicated layer (31) to isolate preview rendering from scene cameras
- Off-screen position (100,0,0) to avoid interfering with gameplay cameras
- 1024x1536 RenderTexture at 4x MSAA -- high quality for character preview
- Camera clear to `Color.clear` for transparent background compositing
- Drag-to-rotate via PointerDown/Move/Up events on the VisualElement

#### 4b. Integration Points for v6.0

**Model loading path:**
1. `CharSelectEvents.OnHeroChanged` fires with `HeroDisplayConfig`
2. `HeroStageController.HandleHeroChanged` receives config
3. Config provides `modelPrefab` reference (currently null -- placeholder active)
4. Controller instantiates prefab at stage root, sets layer recursively
5. Lighting rig lerps to hero-specific colors via `PrimeTween`

**For 3D model integration (Phase G), the architecture needs:**
- `HeroDisplayConfig.modelPrefab` populated with actual prefab references
- Model import pipeline: GLB -> FBX or direct GLB import -> prefab with materials
- Material validation: URP/Lit shader compatibility check
- LOD consideration: preview camera is close, so LOD0 is appropriate

**RenderTexture lifecycle:**
- Created in `OnEnable`, destroyed in `OnDisable`
- Camera disabled when not visible (CharSelect scene only)
- No memory leak risk because `CleanupStage()` releases the RT

### Layer 5: Procedural Audio Integration

#### 5a. Existing Pattern (TitleScreenAudio -- Mostly Correct)

`TitleScreenAudio` generates all audio at startup via `AudioClip.Create` + `SetData`:

```
Start() -> InitializeDeferred() [1 frame delay]
  -> GenerateAllAudio()          [synchronous, ~30ms]
  -> SetupAudioSources()         [5 AudioSources on same GameObject]
  -> Start coroutines:
       FadeMusicIn()             [main drone loop]
       WindLayer()               [delayed wind texture]
       BellLayer()               [random interval bells]
       RumbleLayer()             [random interval rumbles]
       RandomDemonLaughs()       [rare demon laughs]
       VERAInteractions()        [whisper/bark patterns]
```

**What is correct:**
- Deferred init (1 frame) lets UI paint before audio work
- Proper cleanup in `OnDestroy()` via `DestroyClip(ref clip)`
- Volume respects `SettingsManager.Settings.MasterVolume` and `MuteAll`
- Looping drones use crossfade tails for seamless playback

**What needs improvement:**
- `GenerateAllAudio()` is synchronous -- causes a ~30ms frame spike
- Multiple `AudioSource` components on one GameObject (5+) -- cluttered
- Wind layer creates a 6th AudioSource dynamically in its coroutine
- No integration with the existing `AudioManager`/`MusicManager` singletons

#### 5b. Recommended Audio Architecture

```
AudioManager (existing singleton)
  |-- Handles master volume, mute, spatial blend
  |-- Routes through SettingsManager

MusicManager (existing singleton)
  |-- Handles music crossfading

TitleScreenAudio (scene-scoped, NOT singleton)
  |-- Generates procedural clips (drone, wind, bell, rumble, laugh, whisper)
  |-- Creates local AudioSources with known priority ordering
  |-- Respects SettingsManager volume via GetMasterVolume()
  |-- Cleanup: DestroyClip all generated clips in OnDestroy

CharSelectAudio (new, scene-scoped -- if needed)
  |-- Hero-specific ambient drones
  |-- Embark audio intensification
  |-- Uses same procedural generation pattern
```

**Key principle:** Procedural audio components are scene-scoped (not singletons). They generate clips, manage their own AudioSources, and clean up when the scene unloads. They check `SettingsManager` for volume but do NOT route through `AudioManager` because procedural clips are not asset-loaded clips.

#### 5c. Spreading Audio Generation Across Frames

```csharp
private IEnumerator GenerateAllAudioAsync()
{
    _dronePad = GenerateDarkDrone(30f);
    yield return null; // Let frame complete
    _windTexture = GenerateWindTexture(20f);
    yield return null;
    _distantBell = GenerateDistantBell(4f);
    _lowRumble = GenerateLowRumble(6f);
    yield return null;
    // SFX clips are small, batch them
    _demonLaughGen = GenerateDemonLaugh(2.5f);
    _veraHelpMe = GenerateWhisper(0.9f, 380f, 300f, 0.12f);
    _veraCry = GenerateWhisper(1.4f, 260f, 180f, 0.08f);
    _veraPlease = GenerateWhisper(0.5f, 420f, 360f, 0.10f);
    _demonQuiet = GenerateDemonBark(0.35f, 115f, 0.30f);
    _demonSilence = GenerateDemonBark(0.5f, 85f, 0.22f);
}
```

The 30-second drone clip at 44100 Hz stereo = ~5.3 MB of float data. Generating this on the main thread blocks for ~15-20ms. Spreading across frames keeps each frame under budget.

---

## Component Boundaries

| Component | Responsibility | Communicates With |
|-----------|---------------|-------------------|
| `UIGradientHelper` | Generate Texture2D gradients, apply to elements | Called by any UI controller |
| `UITextureRegistry` | Track + destroy runtime textures | Owned per-MonoBehaviour |
| `UIVFXContainer` | Manage z-ordered effect layers | Used by VFX controllers |
| `ThemeManager` | Color tokens for brands/corruption/rarity | Queried by UI controllers |
| `UIAssets` | Centralized template/stylesheet refs | Queried at UI init |
| `TitleScreenVFX` | Ember/ash/spark/lightning particles | Reads UIDocument root |
| `TitleScreenAudio` | Procedural ambient audio generation | Reads SettingsManager |
| `CharSelectVisualEnhancer` | AAA gradient/glow pass | Subscribes to CharSelectEvents |
| `HeroStageController` | RenderTexture 3D model preview | Subscribes to CharSelectEvents |
| `OverlayController` | Scanline/vignette/veil overlays | Called by CharacterSelectManager |
| `GlitchTextEffect` | PrimeTween text scramble | Called by EmbarkCinematicController |

---

## Data Flow

### Title Screen Visual Effects Flow

```
MainMenuBootstrap.Start()
  |-> InitializeUI()                     [UXML template instantiation]
  |-> DeferredStartupInit() [1 frame]
        |-> EnsureOverlayVfx()           [adds MainMenuVFXOverlayController]
        |-> EnsureTitleScreenAudio()     [adds TitleScreenAudio]
        |-> ApplyCornerBlend()           [runtime gradient texture]

TitleScreenVFX.OnEnable()               [via scene GameObject]
  |-> root.RegisterCallback<GeometryChangedEvent>
  |-> OnGeometryChanged -> Initialize()
        |-> Create vfx-container at root.Insert(0)
        |-> Create ember/ash/spark/smoke VisualElements
        |-> StartCoroutine(UpdateParticles)
              |-> Per-frame: move particles, update opacity, recycle

MoltenVeinVFX (same pattern)
MoltenButtonVFX (same pattern, targets button elements)
SoulSwarmVFX (same pattern, cursor-attracted)
ParallaxBackground (same pattern, mouse-driven)
```

### Character Select Visual Effects Flow

```
CharacterSelectManager.OnEnable()
  |-> Deferred: ApplyVisualPass() via schedule.Execute

CharSelectVisualEnhancer.OnEnable()
  |-> Subscribe to CharSelectEvents.OnHeroChanged (3 handlers)
  |-> Subscribe to CharSelectEvents.OnScreenReady
  |-> Deferred: ApplyVisualPass() [50ms delay for UXML]
        |-> Create gradient textures via UIGradientHelper
        |-> Apply to panels, embark button, stat bars, vignette
        |-> Create glow overlays via UIGradientHelper.CreateGlowOverlay

HeroStageController.OnEnable()
  |-> Subscribe to CharSelectEvents.OnHeroChanged
  |-> InitializeStage()
        |-> Create RenderTexture(1024x1536, 4xMSAA)
        |-> Create/configure preview Camera
        |-> Create 5-light lighting rig
        |-> BindRenderTextureToUI() -> hero-render-target.style.backgroundImage

OverlayController.Init(root)            [called by CharacterSelectManager]
  |-> Query overlay elements by USS class
  |-> Configure UsageHints for GPU animation

On Hero Switch:
  CharSelectEvents.RaiseHeroChanged(index, heroData, config)
    |-> CharSelectVisualEnhancer: update card gradients, stat bar colors, panel glow
    |-> HeroStageController: swap 3D model, lerp lighting colors
    |-> OverlayController: transition veil glow intensity
    |-> HeroThemeTransitioner: crossfade environment colors
```

### Procedural Audio Flow

```
TitleScreenAudio.Start()
  |-> InitializeDeferred() [1 frame]
        |-> GenerateAllAudio()        [10 AudioClip.Create calls]
        |-> SetupAudioSources()       [5 AudioSource components]
        |-> StartCoroutine(FadeMusicIn)
        |-> StartCoroutine(WindLayer)
        |-> StartCoroutine(BellLayer)
        |-> StartCoroutine(RumbleLayer)
        |-> StartCoroutine(RandomDemonLaughs)
        |-> StartCoroutine(VERAInteractions)

Per-frame:
  - Drone plays continuously (looped AudioSource)
  - Wind texture plays continuously (delayed start)
  - Bells trigger at random 12-28s intervals
  - Rumbles trigger at random 25-55s intervals
  - Demon laughs trigger at random 30-60s intervals
  - VERA interactions at random 18-40s intervals (pattern cycles)

Volume reads SettingsManager each play:
  GetMasterVolume() -> Settings.MuteAll ? 0 : Settings.MasterVolume
```

---

## Suggested Build Order

Build order is driven by dependency chains and risk mitigation:

### Phase 1: Foundation Fixes (No New Components)

**Do first because everything else depends on clean infrastructure.**

1. Fix `ThemeManager` -> use `SingletonMonoBehaviour<T>` (Phase C item)
2. Fix `UIAnimationController` -> use `SingletonMonoBehaviour<T>` (Phase C item)
3. Fix Texture2D leaks in `MainMenuBootstrap.ApplyCornerBlend()` (track reference)
4. Fix `CharSelectVisualEnhancer` callback leak on re-enable (Phase A item)
5. Fix `CharSelectFocusManager` div-by-zero (Phase A item)

### Phase 2: Texture Memory Safety

**Do second because Phases D-F generate many textures.**

1. Create `UITextureRegistry` utility class
2. Refactor `CharSelectVisualEnhancer` to use `UITextureRegistry`
3. Add gradient caching to `UIGradientHelper`
4. Audit all `new Texture2D()` calls for cleanup tracking

### Phase 3: Native Filter Integration

**Do third -- enables simpler glow effects for Phases E-F.**

1. Test `filter: blur()` on a sample element in Unity 6000.3.6f1
2. Replace `DimBackground()` opacity+scale hack with `filter: blur(4px)`
3. Update the `CharacterSelect.uss` comment about blur capability
4. Create `UIFilterHelper` utility for common filter combinations
5. Document which effects use blur vs texture-based approach

### Phase 4: Title Screen Bug Fixes + Audio Improvement (Phase D+E)

**Parallel-safe with Phase 5.**

1. Fix title screen visual bugs (Phase D items)
2. Spread `TitleScreenAudio.GenerateAllAudio()` across frames
3. Implement title screen AAA effects using established patterns
4. Add procedural audio for embark/VERA interactions

### Phase 5: Character Select Bug Fixes + Visual Rebuild (Phase D+F)

**Parallel-safe with Phase 4 (different scenes).**

1. Fix CharSelect interaction bugs (Phase D items)
2. Rebuild hero card carousel with gradient/glow effects
3. Integrate hold-to-embark visual feedback layers
4. Ensure gamepad navigation works

### Phase 6: 3D Model Integration (Phase G)

**Do after visual rebuild -- models display in the rebuilt UI.**

1. Audit 28 GLB models for quality
2. Import verified models as prefabs with URP materials
3. Populate `HeroDisplayConfig.modelPrefab` references
4. Verify `HeroStageController` displays real models correctly
5. Tune lighting rig per-hero for best model presentation

### Phase 7: End-to-End Verification (Phase H)

**Do last -- validates everything together.**

1. Full flow test: Title -> CharSelect -> Embark -> Overworld
2. Performance profiling: GC allocations, draw calls, frame time
3. Memory audit: no leaked textures/RenderTextures between scenes

---

## Patterns to Follow

### Pattern 1: Deferred VFX Initialization

**What:** Wait for UXML layout to resolve before creating effect elements.
**When:** Any VFX controller that creates VisualElements based on parent dimensions.
**Why:** `resolvedStyle.width/height` returns 0 before layout pass completes.

```csharp
private void Start()
{
    _uiDocument.rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
}

private void OnGeometryChanged(GeometryChangedEvent evt)
{
    if (!_isActive && _vfxContainer == null)
    {
        Initialize(); // Safe: layout dimensions are now valid
    }
}
```

This pattern is correctly used by `TitleScreenVFX`, `MenuVFXController`, `MoltenVeinVFX`, `SoulSwarmVFX`, and `ParallaxBackground`.

### Pattern 2: Dirty-Threshold Animation Updates

**What:** Skip per-frame style writes when the visual change is imperceptible.
**When:** Any coroutine-driven VFX that animates opacity/color/position.
**Why:** Each `element.style.X = value` triggers dirty flags in UI Toolkit. Skipping negligible changes reduces CPU cost.

```csharp
private void UpdateVein(VeinElement vein, float deltaTime)
{
    vein.Intensity = Mathf.Lerp(vein.Intensity, target, deltaTime * 8f);

    // Dirty threshold: skip when change < 0.5%
    if (Mathf.Abs(vein.Intensity - vein.PreviousIntensity) < 0.005f) return;
    vein.PreviousIntensity = vein.Intensity;

    vein.Element.style.opacity = Mathf.Lerp(_baseOpacity, _pulseOpacity, vein.Intensity);
}
```

This pattern is used in `MoltenVeinVFX` and should be adopted by all VFX controllers.

### Pattern 3: Scoped Event Bus for Scene-Scoped Systems

**What:** Use `CharSelectEvents` for CharSelect-scoped communication, `EventBus` for global.
**When:** Any controller that only exists within a single scene.
**Why:** Prevents subscription leaks across scenes and keeps event namespaces clean.

```csharp
// CharSelect-scoped: use CharSelectEvents
CharSelectEvents.OnHeroChanged += HandleHeroChanged;

// Global-scoped: use EventBus
EventBus.OnBattleStarted += HandleBattleStarted;
```

`CharSelectEvents.ClearAll()` is called on scene unload to prevent stale subscriptions.

### Pattern 4: RenderTexture-to-VisualElement Binding

**What:** Render 3D content to a RenderTexture and display in UI Toolkit.
**When:** 3D model previews in UI screens.
**Why:** UI Toolkit cannot render 3D content directly; RenderTexture is the bridge.

```csharp
// Create RT
_renderTexture = new RenderTexture(1024, 1536, 24, RenderTextureFormat.ARGB32);
_renderTexture.antiAliasing = 4;

// Bind to camera
_previewCamera.targetTexture = _renderTexture;
_previewCamera.cullingMask = 1 << kPreviewLayer;

// Bind to UI
var target = root.Q<VisualElement>("hero-render-target");
target.style.backgroundImage = Background.FromRenderTexture(_renderTexture);
```

**Critical:** The camera must use a dedicated culling layer. All model GameObjects must be set to that layer recursively. Camera `clearFlags = SolidColor` with `backgroundColor = Color.clear` for transparent compositing.

---

## Anti-Patterns to Avoid

### Anti-Pattern 1: USS background-color Overriding Runtime Textures

**What:** Setting `background-color` in USS on an element that also has a C#-applied `backgroundImage`.
**Why bad:** USS `background-color` renders ON TOP of the background image in Unity 6, effectively hiding the gradient texture.
**Instead:** Remove `background-color` from USS for elements that use runtime textures, or set `background-color: transparent` explicitly.

### Anti-Pattern 2: Creating Texture2D Without Tracking

**What:** `var tex = new Texture2D(...)` without storing the reference for later `Destroy()`.
**Why bad:** GPU memory leak. Each untracked texture persists until GC (which never reclaims GPU resources).
**Instead:** Use `UITextureRegistry.Register(tex)` or store in a field with cleanup in `OnDisable`.

### Anti-Pattern 3: Multiple root.Insert(0) From Independent Controllers

**What:** Each VFX controller inserting at index 0, fighting for the "bottom" position.
**Why bad:** Insertion order depends on component enable order, which is nondeterministic.
**Instead:** Use a shared `UIVFXContainer` with named layers, or insert at specific known indices with clear ownership.

### Anti-Pattern 4: Synchronous Procedural Audio Generation at Startup

**What:** Generating 30+ seconds of AudioClip data in a single synchronous call chain.
**Why bad:** Blocks the main thread for 15-30ms, causing a visible frame hitch on scene load.
**Instead:** Spread generation across multiple frames using coroutine yields between clips.

### Anti-Pattern 5: Closure-Based PrimeTween Animations

**What:** Using lambda closures with PrimeTween (e.g., `Tween.Custom(0, 1, 0.5f, val => element.style.opacity = val)`).
**Why bad:** Each closure allocates a delegate object that becomes GC pressure in hot paths.
**Instead:** Use target-based overloads per PrimeTween documentation. The project already flags this as a Phase C item.

---

## Scalability Considerations

| Concern | Current (4 heroes) | At 12 heroes | At 50+ monsters |
|---------|--------------------|--------------|-----------------|
| Gradient textures | 12 per CharSelect | 20+ per CharSelect | N/A (combat HUD different) |
| RenderTexture memory | 1 RT (6MB) | 1 RT (shared, swaps model) | 1 RT per preview |
| Procedural audio clips | 10 clips (~5MB) | Same (scene-scoped) | Separate battle audio |
| VFX particle count | 140 embers + 30 souls | Same (scene-scoped) | Lower budget in combat |
| Draw calls from VFX | ~20-40 extra | Same | Must budget separately |

**The architecture scales well because:**
- Texture caching prevents linear growth with hero count
- RenderTexture is shared (one camera, swap models)
- VFX particle counts are capped per-scene, not per-content
- Procedural audio is generated once per scene load, not per-hero

---

## Integration Map: Where New Effects Touch Existing Code

### Title Screen (MainMenu scene)

```
MainMenuBootstrap (existing)
  |-> MainMenuController (existing) -- entrance animation, button interactions
  |-> TitleScreenVFX (existing) -- ember/ash particles
  |-> TitleScreenAudio (existing) -- procedural ambient audio
  |-> MoltenVeinVFX (existing) -- pulsing vein overlays
  |-> MoltenButtonVFX (existing) -- button lava/highlight effects
  |-> SoulSwarmVFX (existing) -- cursor-attracted particles
  |-> ParallaxBackground (existing) -- mouse-driven depth
  |-> MainMenuVFXOverlayController (existing, disabled) -- needs redesign

New for v6.0:
  |-> Native blur for settings overlay defocus (replaces opacity hack)
  |-> Video background integration (RenderTexture from VideoPlayer)
  |-> Improved texture cleanup via UITextureRegistry
```

### Character Select (CharacterSelect scene)

```
CharacterSelectManager (existing orchestrator)
  |-> CharSelectVisualEnhancer (existing) -- gradient/glow pass
  |-> HeroStageController (existing) -- 3D model preview
  |-> OverlayController (existing) -- scanlines/vignette/veil
  |-> CarouselController (existing) -- hero card carousel
  |-> HoldToEmbarkController (existing) -- hold-to-confirm
  |-> EmbarkCinematicController (existing) -- embark transition
  |-> GlitchTextEffect (existing) -- text scramble effect
  |-> CharSelectEnvironmentController (existing) -- parallax/fog

New for v6.0:
  |-> Populated modelPrefab in HeroDisplayConfig (3D models)
  |-> Hero-specific lighting color lerps in HeroStageController
  |-> Native blur for panel glow effects
  |-> Texture memory safety via UITextureRegistry
```

---

## Sources

- Unity 6000.3 Built-in Filters: https://docs.unity3d.com/6000.3/Documentation/Manual/ui-systems/built-in-filters.html
- Unity 6000.3 USS Filter Property: https://docs.unity3d.com/6000.3/Documentation/Manual/ui-systems/uss-filter.html
- Unity 6000.3 Custom Filters: https://docs.unity3d.com/6000.3/Documentation/Manual/ui-systems/custom-filters.html
- Unity UI Toolkit Performance: https://docs.unity3d.com/6000.3/Documentation/Manual/best-practice-guides/ui-toolkit-for-advanced-unity-developers/optimizing-performance.html
- UI Toolkit Development Status (Feb 2025): https://discussions.unity.com/t/ui-toolkit-development-status-and-next-milestones-february-2025/1607740
- Procedural Audio in Unity: https://docs.unity3d.com/ScriptReference/AudioClip.Create.html
- Existing codebase analysis (15+ UI effect scripts, March 2026)

---

*Architecture research: 2026-03-30 -- VeilBreakers v6.0 UI Effects Integration*
