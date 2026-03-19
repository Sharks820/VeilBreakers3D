# Phase 4: Visual Amplification - Research

**Researched:** 2026-03-19
**Domain:** Unity UI Toolkit animation (PrimeTween), URP post-processing, HLSL shaders, cinematic orchestration
**Confidence:** HIGH

## Summary

Phase 4 transforms the character select screen from a functional interface into a cinematic, AAA-quality visual experience. The phase involves seven interlocking technical domains: PrimeTween-driven UI animation sequences, URP Volume Profile runtime interpolation for per-hero post-processing, custom HLSL dissolve and crack shaders, 3D particle systems, adaptive music parameter driving, cinematic embark choreography, and unified data-driven theming via HeroThemeConfig ScriptableObjects.

The critical integration challenge is orchestrating all visual systems from a single HeroThemeTransitioner that reads HeroThemeConfig data and drives animations, post-processing, lighting, particles, overlays, and music in a tightly choreographed timeline. PrimeTween's Sequence API with Insert(atTime) is purpose-built for this kind of multi-system timeline orchestration.

**Primary recommendation:** Install PrimeTween 1.3.8 via OpenUPM. Use `Sequence.Create().Insert(atTime, ...)` for all choreographed timelines (hero switch, screen entry, embark cinematic). Use `Tween.Custom()` for shader property animation and volume profile lerping. Add `Unity.RenderPipelines.Universal.Runtime` to the Runtime asmdef for volume scripting access.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- PrimeTween is the animation library (installed via Package Manager)
- Three animation systems with clear responsibilities: PrimeTween (orchestrated sequences), USS transitions (state changes), ButtonVFXHelper.schedule.Execute() (continuous VFX loops)
- Full staggered hero switch sequence (~1.2s) with specific timeline: t=0ms info fade + music, t=100ms veil pulse, t=200ms dissolve + post-process, t=400ms materialize, t=600ms info fade in, t=800ms stat cascade + glitch text
- Screen entry staggered panel entrance with specific timeline
- Direct stat bar fill (no overshoot/bounce), staggered 100ms cascade
- Glitch text reveal with unicode block chars, ~250ms resolve, per-hero speed
- Carousel card animation: selected 1.15x scale, unselected 0.9x, breathing on active only
- VeilDissolve shader: URP Lit-based, threshold 0-1, HDR edge glow, per-hero MaterialPropertyBlock
- Per-hero URP Volume Profiles (4 assets) lerped via VolumeProfileTransitioner over 0.8s
- Per-hero stage lighting: fill + rim + ambient, tinted per hero, Nyx rim flickers
- Per-hero environment particles: 3D stage background, max 200 particles, GPU instanced
- Music parameter-driven crossfade via MusicManager.SetParameter()
- HeroThemeConfig ScriptableObject: unified data source for all visual identity per hero
- Embark cinematic: veil shatter with VeilCrack shader, procedural crack spread, 1.2-1.5s
- VeilTransitionController: reusable system with PlayShatter/PlayMaterialize/PlayCrackSpread API
- Overlay system: scanlines (USS gradient), vignette, veil glow with per-hero intensity values
- Layered parallax: mouse/stick input drives per-layer translate offsets at different rates
- All breathing effects synchronized: carousel card + rim light + veil glow
- Tab switching: crossfade transition (200ms), intentionally lighter than hero switch

### Claude's Discretion
- Monster brand aura visual style (ground ring + wisps vs outline glow)
- Exact PrimeTween easing curves for each animation segment
- Placeholder capsule idle animation specifics (rotation speed, bob amplitude)
- Procedural noise parameters for veil crack patterns
- Embark hero select quote text per hero
- Exact VeilDissolve particle emission rate and lifetime
- How embark power pose looks on placeholder capsules
- Exact parallax sensitivity curve for mouse vs stick input

### Deferred Ideas (OUT OF SCOPE)
- Full game cinematic after embark (AI-generated cinematic, separate phase)
- Per-hero bespoke 3D environments (art pipeline, v2 ART phase)
- Real voice acting (production concern, not code)
- Film grain post-process (Phase 5 polish pass)
- Motion blur on hero switch (nice-to-have)
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| VISUAL-01 | PrimeTween 1.3.7+ installed and integrated for orchestrated animation sequences | PrimeTween 1.3.8 available via OpenUPM, Sequence.Create()/Chain()/Group()/Insert() API verified, UI Toolkit methods confirmed (StyleOpacity, StyleTranslate, StyleScale, StyleRotate) |
| VISUAL-02 | Coordinated panel transitions (left slides from left, right from right, staggered timing) | PrimeTween Sequence.Insert(atTime) enables precise timeline offsets; Tween.StyleTranslate() for panel slides |
| VISUAL-03 | Staggered stat bar fill animations on hero switch | Tween.Custom() with target pattern for allocation-free bar width animation; ChainDelay(0.1f) for 100ms stagger |
| VISUAL-04 | Per-hero URP Volume Profiles configured (Bloom, DoF, Vignette, Color Adjustments) | VolumeProfile.TryGet<T>() API verified; per-property overrideState + value pattern; Tween.Custom() drives lerp between cached override values |
| VISUAL-05 | Cinematic overlays active (scanlines, vignette, veil glow) via USS | Overlay elements already exist in UXML + USS; per-hero intensity values from HeroThemeConfig; parallax via Tween.StyleTranslate() |
| VISUAL-06 | Embark sequence cinematic (1-2s animation before scene transition) | VeilCrack shader (fullscreen Voronoi noise), VeilTransitionController orchestrates via PrimeTween Sequence, Full Screen Pass Renderer Feature for crack overlay |
| VISUAL-07 | USS filters applied for visual effects (blur on inactive panels, tint, contrast) | USS opacity + background-color manipulation for dim/desaturate; PrimeTween StyleOpacity for transitions |
| VISUAL-08 | Embark button breathing glow animation | ButtonVFXHelper.AddBreathing() already exists; extend with hero-accent-colored glow overlay |
| VISUAL-09 | Per-hero ambient music crossfade via MusicManager | MusicManager.SetParameter() already implemented with _parameterLerpSpeed=3f; add hero-specific parameter sets (warmth, synth, perc, pad, filter, tension) |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| PrimeTween | 1.3.8 | Orchestrated UI + shader + property animation | Allocation-free, UI Toolkit native support (StyleTranslate/StyleScale/StyleOpacity/StyleRotate), Sequence API with Insert(atTime) for precise timelines, Tween.Custom() for arbitrary property animation |
| Unity URP | 17.3.0 | Post-processing Volume Profiles | Already installed; Volume scripting API for Bloom/DoF/Vignette/ColorAdjustments/ChromaticAberration |
| Unity Particle System | built-in | Per-hero 3D environment particles + dissolve edge emission | GPU instanced particles, max 200 per hero, built-in to Unity |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Unity.RenderPipelines.Universal.Runtime | 17.3.0 | C# access to Volume override types | Must add to VeilBreakers.Runtime.asmdef for VolumeProfileTransitioner |
| PrimeTween (PRIME_TWEEN_EXPERIMENTAL) | 1.3.8 | GroupCallback() for synchronized callbacks | Use for embark cinematic SFX sync; requires scripting define |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| PrimeTween | DOTween | DOTween is more established but has allocations; PrimeTween is allocation-free and has native UI Toolkit methods |
| HLSL dissolve shader | Shader Graph dissolve | Shader Graph is visual but less control; HLSL gives exact edge emission math and MaterialPropertyBlock compatibility |
| Full Screen Pass for VeilCrack | UI Toolkit overlay element | Full Screen Pass integrates with URP rendering pipeline; UI overlay would miss 3D content |

**Installation:**
```bash
# PrimeTween via OpenUPM (recommended for UPM integration)
openupm add com.kyrylokuzyk.primetween

# Or via Unity Package Manager git URL
# In manifest.json, add:
# "com.kyrylokuzyk.primetween": "https://github.com/KyryloKuzyk/PrimeTween.git"
```

**Assembly Reference Update (CRITICAL):**
```json
// VeilBreakers.Runtime.asmdef - must add URP reference
{
  "references": [
    "Unity.InputSystem",
    "Unity.TextMeshPro",
    "Unity.RenderPipelines.Universal.Runtime",
    "PrimeTween"
  ]
}
```

**Version verification:** PrimeTween 1.3.8 confirmed via npm registry (2026-02-09). URP 17.3.0 confirmed in project manifest.json.

## Architecture Patterns

### Recommended Project Structure
```
Assets/
  Scripts/
    UI/
      CharacterSelect/
        HeroThemeConfig.cs              # ScriptableObject: unified per-hero visual data
        HeroThemeTransitioner.cs        # Reads theme config, drives ALL visual systems
        VolumeProfileTransitioner.cs    # Lerps URP Volume overrides
        VeilDissolveController.cs       # Drives VeilDissolve shader on 3D models
        GlitchTextEffect.cs            # Text scramble -> resolve animation
        OverlayController.cs           # Manages scanline/vignette/glow + parallax
        ScreenEntryAnimator.cs         # One-shot screen entrance sequence
        HeroSwitchAnimator.cs          # Hero switch choreography
        EmbarkCinematicController.cs   # Embark sequence orchestration
      Controls/
        ButtonVFXHelper.cs             # (existing) extend with breathing glow
  Shaders/
    VeilDissolve.shader                # URP Lit dissolve with HDR edge + particle emission
    VeilCrack.shader                   # Full-screen procedural crack for embark cinematic
  Resources/
    CharacterSelect/
      HeroThemes/
        HeroTheme_Vex.asset            # HeroThemeConfig ScriptableObject
        HeroTheme_Seraphina.asset
        HeroTheme_Orion.asset
        HeroTheme_Nyx.asset
        VolumeProfile_Vex.asset        # URP Volume Profile per hero
        VolumeProfile_Seraphina.asset
        VolumeProfile_Orion.asset
        VolumeProfile_Nyx.asset
      Particles/
        FX_Vex_ForgeEmbers.prefab      # Per-hero particle prefabs
        FX_Seraphina_VoidCrystals.prefab
        FX_Orion_BloodMoonHaze.prefab
        FX_Nyx_DigitalRain.prefab
      Textures/
        noise_perlin_256.png           # Dissolve noise texture
```

### Pattern 1: Unified Theme Data (HeroThemeConfig ScriptableObject)
**What:** Single ScriptableObject per hero containing ALL visual identity data -- colors, volume profile ref, lighting, particles, music params, overlay intensities, dissolve settings, glitch speed.
**When to use:** Any system that needs per-hero visual customization reads from this one data source.
**Example:**
```csharp
// Source: Project convention (ScriptableObject pattern from HeroDisplayConfig)
[CreateAssetMenu(fileName = "NewHeroTheme", menuName = "VeilBreakers/Hero Theme Config")]
public class HeroThemeConfig : ScriptableObject
{
    // COLORS
    [Header("Colors")]
    public Color primaryColor;
    public Color glowColor;
    public Color darkColor;
    public Color dissolveEdgeColor;

    // POST-PROCESSING
    [Header("Post-Processing")]
    public VolumeProfile volumeProfile;
    [Range(0f, 5f)]
    public float chromaticAberrationIntensity = 0f; // Only Nyx > 0

    // LIGHTING
    [Header("Lighting")]
    public Color fillLightColor;
    [Range(0f, 2f)]
    public float fillLightIntensity = 0.8f;
    public Color rimLightColor;
    [Range(0f, 3f)]
    public float rimLightIntensity = 1.2f;
    public Color ambientColor;
    public bool rimFlicker = false; // Only Nyx

    // MUSIC
    [Header("Music Parameters")]
    [Range(0f, 1f)] public float musicIntensity;
    [Range(0f, 1f)] public float musicWarmth;
    [Range(0f, 1f)] public float musicTension;
    [Range(0f, 1f)] public float musicSynth;
    [Range(0f, 1f)] public float musicPerc;
    [Range(0f, 1f)] public float musicPad;
    [Range(0f, 1f)] public float musicFilter;

    // PARTICLES
    [Header("Particles")]
    public GameObject particlePrefab;
    public int maxParticleCount = 200;

    // OVERLAYS
    [Header("Overlays")]
    [Range(0f, 0.2f)] public float scanlineOpacity = 0.05f;
    [Range(0f, 0.5f)] public float vignetteIntensity = 0.25f;
    [Range(0f, 0.2f)] public float veilGlowOpacity = 0.08f;

    // DISSOLVE
    [Header("Dissolve")]
    [Range(0.1f, 2f)] public float dissolveNoiseScale = 1f;
    [Range(0.1f, 1f)] public float dissolveDuration = 0.4f;

    // GLITCH TEXT
    [Header("Glitch Text")]
    [Range(20f, 80f)] public float glitchResolveSpeed = 50f;

    // MONSTER
    [Header("Monster")]
    public Color monsterAuraColor;
}
```

### Pattern 2: PrimeTween Sequence Timeline Orchestration
**What:** Use Sequence.Create() with Insert(atTime) for precise multi-system choreography.
**When to use:** Hero switch, screen entry, embark cinematic -- any animation involving multiple systems at specific time offsets.
**Example:**
```csharp
// Source: PrimeTween docs - Sequence.Insert(atTime) pattern
private Sequence BuildHeroSwitchSequence(HeroThemeConfig newTheme)
{
    return Sequence.Create()
        // t=0ms: Info panel fade out + music crossfade
        .Insert(0f, Tween.StyleOpacity(_infoPanel, 0f, 0.2f))
        .InsertCallback(0f, () => ApplyMusicParameters(newTheme))
        // t=100ms: Veil pulse flash
        .Insert(0.1f, BuildVeilPulseFlash(newTheme.primaryColor))
        // t=200ms: Dissolve out + post-process lerp + old particles fade
        .Insert(0.2f, Tween.Custom(this, 0f, 1f, 0.4f,
            (target, val) => target._dissolveController.SetDissolveThreshold(val)))
        .InsertCallback(0.2f, () => _volumeTransitioner.LerpTo(newTheme.volumeProfile, 0.8f))
        // t=400ms: New hero materializes
        .Insert(0.4f, Tween.Custom(this, 1f, 0f, 0.4f,
            (target, val) => target._dissolveController.SetDissolveThreshold(val)))
        // t=600ms: Info panel fades in
        .Insert(0.6f, Tween.StyleOpacity(_infoPanel, 1f, 0.3f))
        // t=800ms: Stat bar cascade
        .Insert(0.8f, BuildStatBarCascade(newTheme))
        // t=800ms: Glitch text reveal
        .Insert(0.8f, BuildGlitchTextReveal(newTheme));
}
```

### Pattern 3: Volume Profile Lerping via Tween.Custom
**What:** Manually interpolate between cached VolumeOverride values using Tween.Custom() and per-property lerp.
**When to use:** Hero switch post-processing transition.
**Example:**
```csharp
// Source: URP Volume scripting API + PrimeTween Tween.Custom
public class VolumeProfileTransitioner : MonoBehaviour
{
    [SerializeField] private Volume _volume;

    private Bloom _bloom;
    private Vignette _vignette;
    private DepthOfField _dof;
    private ColorAdjustments _colorAdj;
    private ChromaticAberration _chromatic;

    // Cached source values for lerp
    private float _srcBloomIntensity, _dstBloomIntensity;
    private Color _srcBloomTint, _dstBloomTint;
    // ... more cached pairs per property

    private void Awake()
    {
        var profile = _volume.profile;
        profile.TryGet(out _bloom);
        profile.TryGet(out _vignette);
        profile.TryGet(out _dof);
        profile.TryGet(out _colorAdj);
        profile.TryGet(out _chromatic);
    }

    public Tween LerpTo(VolumeProfile targetProfile, float duration)
    {
        CacheSourceValues();
        CacheTargetValues(targetProfile);

        return Tween.Custom(this, 0f, 1f, duration,
            ease: Ease.InOutSine,
            onValueChange: (target, t) => target.ApplyLerp(t));
    }

    private void ApplyLerp(float t)
    {
        _bloom.intensity.overrideState = true;
        _bloom.intensity.value = Mathf.Lerp(_srcBloomIntensity, _dstBloomIntensity, t);
        _bloom.tint.overrideState = true;
        _bloom.tint.value = Color.Lerp(_srcBloomTint, _dstBloomTint, t);
        // ... repeat for all properties
    }
}
```

### Pattern 4: MaterialPropertyBlock for Per-Hero Shader Customization
**What:** Use MaterialPropertyBlock instead of material instances to customize VeilDissolve shader per hero without creating material copies.
**When to use:** VeilDissolve threshold, edge color, noise scale -- all vary per hero.
**Example:**
```csharp
// Source: Unity best practice for per-object shader customization
private MaterialPropertyBlock _mpb;

private void SetDissolveProperties(HeroThemeConfig theme, float threshold)
{
    _mpb ??= new MaterialPropertyBlock();
    _mpb.SetFloat("_DissolveThreshold", threshold);
    _mpb.SetColor("_DissolveEdgeColor", theme.dissolveEdgeColor);
    _mpb.SetFloat("_NoiseScale", theme.dissolveNoiseScale);
    _renderer.SetPropertyBlock(_mpb);
}
```

### Anti-Patterns to Avoid
- **Animating non-GPU properties in USS:** Never animate width, height, margin, padding via PrimeTween or USS transitions -- these trigger layout recalculation. Only animate translate, scale, rotate, opacity, color.
- **Creating new Materials per hero switch:** Use MaterialPropertyBlock for per-object shader values, not `new Material()` which leaks if not manually destroyed.
- **Tween allocation in hot paths:** Always use the `Tween.Custom(target, ...)` overload with explicit target parameter to avoid closure allocations.
- **Modifying shared VolumeProfile assets at runtime:** Changes to the shared profile persist in the Editor. Use a runtime-instantiated profile or a dedicated scene Volume with weight=1 and modify its profile instance.
- **Missing UsageHints on animated VisualElements:** Always set `UsageHints.DynamicTransform | UsageHints.DynamicColor` at creation time for any element PrimeTween will animate.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Animation sequencing | Manual coroutine timelines with WaitForSeconds | PrimeTween Sequence.Create().Insert(atTime) | Coroutines can't pause/resume/kill cleanly; PrimeTween sequences are allocation-free and interruptible |
| Easing curves | Manual Mathf.Lerp with custom curve math | PrimeTween Ease enum (30+ options) | Battle-tested easing; parametric Overshoot/Bounce/Elastic built in |
| UI Toolkit opacity/translate/scale animation | schedule.Execute() with manual lerp | Tween.StyleOpacity/StyleTranslate/StyleScale | PrimeTween handles the update loop, duration, easing, and cancellation |
| Volume profile interpolation framework | Custom MonoBehaviour with per-frame Mathf.Lerp for each property | Tween.Custom() driving a single lerp factor | Single tween drives all properties via one callback; killable, easable |
| Dissolve shader | Shader Graph node network | Hand-written URP HLSL shader | Full control over clip(), edge emission math, HDR glow, particle emission hooks; MaterialPropertyBlock compatible |

**Key insight:** PrimeTween's Tween.Custom() is the universal adapter -- it can drive any float/Color/Vector3 over time. Use it for shader properties (material.SetFloat), volume overrides (.value), light intensities, and anything else that needs smooth interpolation.

## Common Pitfalls

### Pitfall 1: VolumeProfile Asset Mutation in Editor
**What goes wrong:** Modifying VolumeOverride values via `profile.TryGet<Bloom>(out bloom); bloom.intensity.value = X;` permanently changes the ScriptableObject asset in Editor.
**Why it happens:** `Volume.profile` returns the shared asset reference in Editor, not a runtime copy.
**How to avoid:** Use `Volume.profile` (which creates a runtime instance automatically when you access it at runtime) OR explicitly instantiate: `_volume.profile = Instantiate(_volume.sharedProfile);` in Awake. Clean up the instantiated profile in OnDestroy.
**Warning signs:** Post-processing values "stick" between play sessions in Editor.

### Pitfall 2: Missing overrideState on Volume Properties
**What goes wrong:** Setting `bloom.intensity.value = 1.5f` has no effect because `bloom.intensity.overrideState` is still false.
**Why it happens:** URP Volume overrides have a two-part property: overrideState (bool) + value. Both must be set.
**How to avoid:** Always set `property.overrideState = true` before setting `.value`. Create a helper method.
**Warning signs:** Post-processing changes don't appear despite correct values in debugger.

### Pitfall 3: Assembly Definition Missing URP Reference
**What goes wrong:** Compilation errors when using `UnityEngine.Rendering.Universal` types (Bloom, Vignette, etc.) in runtime scripts.
**Why it happens:** `VeilBreakers.Runtime.asmdef` currently only references `Unity.InputSystem` and `Unity.TextMeshPro`. URP types live in `Unity.RenderPipelines.Universal.Runtime`.
**How to avoid:** Add `"Unity.RenderPipelines.Universal.Runtime"` and `"Unity.RenderPipelines.Core.Runtime"` to the asmdef references before any volume scripting code.
**Warning signs:** "The type or namespace 'Universal' does not exist in the namespace 'UnityEngine.Rendering'"

### Pitfall 4: PrimeTween Sequence Killed by Destroyed Target
**What goes wrong:** Sequence completes partially or throws errors when a VisualElement target is removed from the hierarchy.
**Why it happens:** Hero switch may destroy/recreate elements while a sequence is running.
**How to avoid:** Store the Sequence in a field. Call `sequence.Stop()` before starting a new hero switch sequence. PrimeTween safely handles stopped sequences.
**Warning signs:** Partial animations, elements stuck at intermediate states.

### Pitfall 5: Shader Not Found at Runtime
**What goes wrong:** `Shader.Find("VeilBreakers/VeilDissolve")` returns null in builds.
**Why it happens:** Unity strips shaders not directly referenced by materials in the build. `Shader.Find()` only finds shaders included in "Always Included Shaders" or referenced by a material in Resources.
**How to avoid:** Create a placeholder material using the shader and place it in Resources, or add the shader to Project Settings > Graphics > Always Included Shaders.
**Warning signs:** Pink/magenta models in builds but works in Editor.

### Pitfall 6: Particle System Performance with GPU Instancing
**What goes wrong:** Particle systems cause frame drops on hero switch.
**Why it happens:** CPU-rendered particles with complex meshes at high counts.
**How to avoid:** Enable GPU Instancing on Particle System Renderer. Use simple quad/billboard meshes. Cap at 200 particles per system. Use burst emission (not constant) for spawn-in, then steady state.
**Warning signs:** Profiler shows ParticleSystem.Update taking > 2ms.

### Pitfall 7: Dissolve Shader Z-Fighting with Transparent Pixels
**What goes wrong:** Visual artifacts at dissolve edges where partially transparent pixels overlap.
**Why it happens:** Alpha clip with soft edges can cause z-fighting.
**How to avoid:** Use hard `clip()` in the fragment shader (binary discard, not alpha blend). The HDR emission edge provides the visual softness without transparency.
**Warning signs:** Flickering pixels along dissolve boundaries.

## Code Examples

Verified patterns from official sources:

### PrimeTween: UI Toolkit VisualElement Animation
```csharp
// Source: PrimeTween README + DeepWiki docs
using PrimeTween;
using UnityEngine.UIElements;

// Slide panel from off-screen
Tween.StyleTranslate(panel, new Translate(-300, 0), new Translate(0, 0),
    duration: 0.4f, ease: Ease.OutCubic);

// Fade opacity
Tween.StyleOpacity(element, endValue: 1f, duration: 0.3f);

// Scale card
Tween.StyleScale(card, new Scale(new Vector2(1.15f, 1.15f)),
    duration: 0.3f, ease: Ease.OutCubic);

// Animate background color
Tween.VisualElementBackgroundColor(element, Color.clear, duration: 0.2f);
```

### PrimeTween: Sequence with Insert(atTime)
```csharp
// Source: PrimeTween DeepWiki - Sequences
var seq = Sequence.Create()
    .Insert(0f, Tween.StyleOpacity(panelA, 0f, 0.2f))
    .Insert(0.1f, Tween.StyleTranslate(panelB, startPos, endPos, 0.4f, Ease.OutCubic))
    .Insert(0.2f, Tween.StyleTranslate(panelC, startPos2, endPos2, 0.4f, Ease.OutCubic))
    .InsertCallback(0.5f, () => PlaySound("whoosh"))
    .Insert(0.5f, Tween.StyleOpacity(panelA, 1f, 0.3f));
```

### PrimeTween: Tween.Custom for Shader Properties
```csharp
// Source: PrimeTween README - Tween.Custom() + Material.SetFloat
Tween.Custom(target: _renderer, startValue: 0f, endValue: 1f,
    duration: 0.4f, ease: Ease.InQuad,
    onValueChange: (renderer, val) =>
    {
        _mpb.SetFloat("_DissolveThreshold", val);
        renderer.SetPropertyBlock(_mpb);
    });
```

### URP Volume Override Access Pattern
```csharp
// Source: Unity docs - Volume scripting API
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[SerializeField] private Volume _volume;

private void CacheOverrides()
{
    var profile = _volume.profile; // Runtime instance (safe to modify)
    profile.TryGet(out Bloom bloom);
    profile.TryGet(out Vignette vignette);
    profile.TryGet(out DepthOfField dof);
    profile.TryGet(out ColorAdjustments colorAdj);
    profile.TryGet(out ChromaticAberration chromatic);

    // Enable overrides and set values
    bloom.intensity.overrideState = true;
    bloom.intensity.value = 1.2f;
    bloom.tint.overrideState = true;
    bloom.tint.value = new Color(1f, 0.8f, 0.4f); // Warm amber for Vex
}
```

### VeilDissolve Shader Structure (URP Lit-Based)
```hlsl
// Source: Dissolve shader breakdown (cyanilux.com) + URP shader code patterns
Shader "VeilBreakers/VeilDissolve"
{
    Properties
    {
        // Standard URP Lit properties
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)

        // Dissolve properties
        _DissolveThreshold ("Dissolve Threshold", Range(0, 1)) = 0
        _NoiseTexture ("Noise Texture", 2D) = "white" {}
        _NoiseScale ("Noise Scale", Float) = 1.0
        _DissolveEdgeWidth ("Edge Width", Range(0, 0.1)) = 0.05
        _DissolveEdgeColor ("Edge Color", Color) = (1, 0.5, 0, 1)
        _EmissionIntensity ("Emission Intensity", Float) = 3.0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }

        Pass
        {
            // URP Lit Forward pass structure
            // In fragment shader:
            // float noise = tex2D(_NoiseTexture, uv * _NoiseScale).r;
            // clip(noise - _DissolveThreshold);
            // float edge = smoothstep(_DissolveThreshold, _DissolveThreshold + _DissolveEdgeWidth, noise);
            // float3 edgeEmission = _DissolveEdgeColor.rgb * _EmissionIntensity * (1 - edge);
            // color.rgb += edgeEmission;
        }
    }
}
```

### Glitch Text Effect Pattern
```csharp
// Source: Project convention + CONTEXT.md spec
private static readonly char[] kGlitchChars = new char[]
{
    '\u2588', '\u2591', '\u2592', '\u2593', '\u2580', '\u2584',
    '\u258C', '\u2590', '\u25A0', '\u25B2', '\u2666', '\u00A8'
};

public Sequence BuildGlitchReveal(Label label, string targetText, float resolveSpeed)
{
    var seq = Sequence.Create();
    float charDelay = 1f / resolveSpeed; // ms per character

    for (int i = 0; i < targetText.Length; i++)
    {
        int charIndex = i;
        seq.Insert(charIndex * charDelay,
            Tween.Custom(target: label, startValue: 0f, endValue: 1f,
                duration: charDelay * 3f, // Glitch frames before resolve
                onValueChange: (lbl, t) =>
                {
                    char[] chars = lbl.text.ToCharArray();
                    if (t < 0.8f)
                        chars[charIndex] = kGlitchChars[UnityEngine.Random.Range(0, kGlitchChars.Length)];
                    else
                        chars[charIndex] = targetText[charIndex];
                    lbl.text = new string(chars);
                }));
    }

    return seq;
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| DOTween for all Unity animation | PrimeTween for allocation-free animation | 2023-2024 | Zero GC allocations, native UI Toolkit support |
| schedule.Execute() for UI animation | PrimeTween Tween.StyleX() methods | PrimeTween 1.3.1 (2025-04) | Proper easing, sequencing, cancellation for UI Toolkit |
| Post-processing Stack v2 | URP Volume framework | Unity 2019+ | Integrated into render pipeline, per-volume weight blending |
| Surface Shader dissolve | URP HLSL dissolve with clip() | URP adoption | Must use URP shader structure (Tags, HLSLPROGRAM, Core.hlsl) |
| Shader Graph for all effects | HLSL for performance-critical/complex shaders | Ongoing | Shader Graph for prototyping, HLSL for production control |
| Full Screen Pass via script | Full Screen Pass Renderer Feature | URP 14+ (Unity 2022.2+) | Built-in support, no custom ScriptableRendererFeature needed |

**Deprecated/outdated:**
- `PostProcessVolume` (Post-processing Stack v2): Replaced by URP `Volume` component
- `schedule.Execute()` for complex animation: Still valid for simple loops but PrimeTween is superior for sequenced/eased animation
- `VolumeOverride.Interp()` for manual blending: Does not work as expected; use per-property lerp instead

## Open Questions

1. **PrimeTween asmdef name for VeilBreakers.Runtime reference**
   - What we know: PrimeTween installs as a package and should have an assembly like `PrimeTween` or `com.kyrylokuzyk.primetween`
   - What's unclear: Exact assembly name to reference in asmdef
   - Recommendation: After installing, check the PrimeTween package folder for its .asmdef and use that name. Likely `PrimeTween`.

2. **Full Screen Pass Renderer Feature for VeilCrack shader**
   - What we know: URP 17.3 supports Full Screen Pass Renderer Feature natively. VeilCrack needs to render over the entire screen including 3D content.
   - What's unclear: Whether to use Full Screen Pass Renderer Feature (GPU) or a full-screen UI Toolkit element with a RenderTexture approach.
   - Recommendation: Use Full Screen Pass Renderer Feature for proper 3D integration. The VeilCrack material gets assigned to it. Enable/disable the feature via script during embark.

3. **MusicManager parameter extension**
   - What we know: Current MusicManager has 4 parameters (intensity, tension, lowhealth, bossphase). Phase 4 needs hero-specific parameters (warmth, synth, perc, pad, filter).
   - What's unclear: Whether to add named parameters to MusicManager or use a dictionary approach.
   - Recommendation: Add the new named parameters following the existing pattern (target + lerp). Keep the string-match SetParameter() API but add the new parameter names. This preserves backward compatibility.

4. **Noise texture for dissolve**
   - What we know: Any grayscale noise texture works (Perlin, Simplex, etc.). Per-hero noise scale is configurable.
   - What's unclear: Whether to use a single shared noise texture with different scales or per-hero textures.
   - Recommendation: Single 256x256 Perlin noise texture shared by all heroes. `_NoiseScale` property in shader controls per-hero variation. Simpler, fewer assets, same visual result.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Unity Test Framework 1.6.0 + NUnit |
| Config file | Assets/Tests/EditMode/VeilBreakers.Tests.EditMode.asmdef, Assets/Tests/PlayMode/VeilBreakers.Tests.PlayMode.asmdef |
| Quick run command | `Unity.exe -runTests -testPlatform EditMode -testFilter "VeilBreakers" -batchmode -nographics` |
| Full suite command | `Unity.exe -runTests -testPlatform PlayMode -batchmode` |

### Phase Requirements to Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| VISUAL-01 | PrimeTween installed and usable | unit | Verify PrimeTween assembly reference resolves; create simple tween in EditMode | Wave 0 |
| VISUAL-02 | Panel transitions staggered correctly | manual-only | Visual verification in Play mode -- panel positions at each timeline point | N/A |
| VISUAL-03 | Stat bar cascade with 100ms stagger | manual-only | Visual verification; could smoke test that stat bar widths change after hero switch | N/A |
| VISUAL-04 | Volume profiles lerp on hero switch | smoke | Load CharacterSelect scene, switch hero, verify Volume.profile is not null | Wave 0 |
| VISUAL-05 | Overlays render with correct opacity | smoke | Verify overlay elements exist in UXML and have non-zero opacity after screen ready | Existing partial |
| VISUAL-06 | Embark cinematic plays without errors | smoke | Trigger embark, verify no exceptions, scene loads within timeout | Existing partial |
| VISUAL-07 | Inactive panel dim/desaturate | manual-only | Visual verification | N/A |
| VISUAL-08 | Embark button breathing glow | manual-only | Visual verification | N/A |
| VISUAL-09 | Music parameters change on hero switch | unit | Verify MusicManager.SetParameter called with correct values per hero | Wave 0 |

### Sampling Rate
- **Per task commit:** Visual inspection in Unity Editor Play mode
- **Per wave merge:** Full compilation check (zero errors) + existing PlayMode smoke tests
- **Phase gate:** Full suite green + visual verification screenshots before `/gsd:verify-work`

### Wave 0 Gaps
- [ ] `Assets/Tests/EditMode/PrimeTween_Integration_EditModeTests.cs` -- Verify PrimeTween installs correctly and assembly resolves
- [ ] `Assets/Tests/EditMode/HeroThemeConfig_EditModeTests.cs` -- Verify all 4 HeroThemeConfig assets load, have non-null volume profiles
- [ ] Update `VeilBreakers.Tests.EditMode.asmdef` to reference `PrimeTween` assembly
- [ ] Framework install: PrimeTween via OpenUPM (`openupm add com.kyrylokuzyk.primetween`)

## Sources

### Primary (HIGH confidence)
- [PrimeTween GitHub](https://github.com/KyryloKuzyk/PrimeTween) - Installation, API reference, changelog (1.3.8)
- [PrimeTween DeepWiki - UI Animations](https://deepwiki.com/KyryloKuzyk/PrimeTween/4.2-ui-animations) - StyleOpacity, StyleTranslate, StyleScale, StyleRotate, VisualElementColor, VisualElementBackgroundColor
- [PrimeTween DeepWiki - Sequences](https://deepwiki.com/KyryloKuzyk/PrimeTween/4.5-sequences) - Chain(), Group(), Insert(atTime), ChainCallback(), InsertCallback(), GroupCallback()
- [PrimeTween README](https://github.com/KyryloKuzyk/PrimeTween/blob/main/README.md) - Tween.Custom() API, shader property animation
- [Unity Manual - URP Volumes](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/Volumes.html) - Volume component, VolumeProfile, interpolation
- [Unity Manual - Full Screen Pass Renderer Feature](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/renderer-features/renderer-feature-full-screen-pass.html) - VeilCrack shader integration
- [Unity Docs - Bloom Volume Override](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/post-processing-bloom.html) - Bloom properties reference

### Secondary (MEDIUM confidence)
- [Cyanilux - Dissolve Shader Breakdown](https://www.cyanilux.com/tutorials/dissolve-shader-breakdown/) - Noise + step + emission edge technique
- [Daniel Ilett - Dissolve Effect in URP](https://danielilett.com/2020-04-15-tut5-4-urp-dissolve/) - URP-specific dissolve implementation
- [Febucci - Dissolve Shader Tutorial](https://blog.febucci.com/2018/09/dissolve-shader/) - HLSL clip() + edge glow technique
- [Cyanilux - Writing Shader Code in URP](https://www.cyanilux.com/tutorials/urp-shader-code/) - URP HLSL shader structure
- [Unity Discussions - URP Volume Override scripting](https://discussions.unity.com/t/urp-volume-cs-how-to-access-the-override-settings-at-runtime-via-script/773268) - TryGet pattern, overrideState

### Tertiary (LOW confidence)
- [Unity Discussions - Voronoi Noise for Procedural Cracks](https://discussions.unity.com/t/voronoi-noise-shader-issue-to-make-procedural-cracks/937392) - Community approach to crack patterns (needs validation against URP 17)
- [Omitram - DOTween vs LeanTween vs PrimeTween 2026](https://omitram.com/unity-tweening-guide-dotween-leantween-primetween/) - Comparison article, verify PrimeTween claims independently

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - PrimeTween 1.3.8 verified on npm, URP 17.3.0 in project manifest, all APIs verified via official docs and DeepWiki
- Architecture: HIGH - Patterns follow existing project conventions (ScriptableObject, event bus, section separators), PrimeTween Sequence API well-documented
- Pitfalls: HIGH - Volume profile mutation, overrideState, asmdef references, shader stripping are all well-known Unity pitfalls with documented solutions
- Shader implementation: MEDIUM - VeilDissolve pattern is well-established; VeilCrack (fullscreen Voronoi procedural) is more custom and may need iteration
- Music integration: HIGH - MusicManager.SetParameter() already exists with lerp; extension is straightforward

**Research date:** 2026-03-19
**Valid until:** 2026-04-19 (30 days -- PrimeTween and URP are stable)

---
*Phase: 04-visual-amplification*
*Research completed: 2026-03-19*
