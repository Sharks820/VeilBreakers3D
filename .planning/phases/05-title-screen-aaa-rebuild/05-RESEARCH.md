# Phase 5: Title Screen AAA Rebuild - Research

**Researched:** 2026-03-31
**Domain:** Unity 6 UI Toolkit VFX, runtime texture management, procedural audio, class decomposition
**Confidence:** HIGH

## Summary

Phase 5 rebuilds the title screen to AAA quality by decomposing the 3146-line TitleScreenVFX god class into focused components, implementing a UITextureRegistry for leak-free texture lifecycle, testing and integrating native `filter:blur()` for panel glows, and rewiring VERA audio from sequential pattern cycling to randomized interactions with cooldowns. The codebase already has ~70% of the infrastructure (UIGradientHelper, TitleScreenAudio, ButtonVFXHelper, particle systems, video background). The primary work is structural (decomposition + registry) and enhancement (blur + audio randomization), not greenfield development.

**Primary recommendation:** Decompose TitleScreenVFX first (it blocks everything else), then create UITextureRegistry as a standalone utility, test native blur, and finally rewire VERA title audio. Each of these four pillars can be planned as separate waves.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
All implementation choices are at Claude's discretion -- discuss phase was skipped per user setting.
Use ROADMAP phase goal, success criteria, and codebase conventions to guide decisions.

Key guidelines from CLAUDE.md:
- Use Context7 for PrimeTween/UI Toolkit/URP/Cinemachine APIs before writing code
- Visual QA pipeline: design -> spec -> implement -> screenshot -> compare
- Read before edit, test every 3-5 changes
- UI Toolkit only (NOT IMGUI)
- Runtime Texture2D generation for gradients (USS cannot do gradients)

### Claude's Discretion
All implementation choices are at Claude's discretion.

### Deferred Ideas (OUT OF SCOPE)
None -- discuss phase skipped.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| TITLE-01 | Zero Texture2D leaks via UITextureRegistry pattern | UITextureRegistry design in Architecture research; CharSelectVisualEnhancer DestroyTex pattern as reference; MainMenuController 7-field cleanup pattern as reference |
| TITLE-02 | UIGradientHelper extended with cache and blur integration | Static utility with dictionary cache keyed on color+size; CreateGlowOverlay must return Texture2D for tracking |
| TITLE-03 | TitleScreenVFX decomposed into focused components | 3146-line god class analysis: 6 subsystems identified (particles, lightning, logo, fog, video, audio) |
| TITLE-04 | Decomposition preserves all existing visual behavior | Each subsystem's public API is narrow: StartVFX/StopVFX/SetIntensity/OnButtonHovered/SparkBurst |
| TITLE-05 | VFX container z-order management via UIVFXContainer | 7-layer stack pattern from architecture research; eliminates fragile root.Insert(0,...) pattern |
| TITLE-06 | VERA audio plays randomized interactions with cooldowns, not looping | Current VERAInteractions() cycles 4 patterns sequentially; needs weighted random pool + per-pattern cooldown + recently-played exclusion |
| TITLE-07 | Native filter:blur tested and used for panel glows | Unity 6000.3.6f1 supports `style.filter = new FilterFunction[] { FilterFunction.Blur(8f) }` natively; GPU-accelerated; replaces radial gradient textures for small-to-medium glow effects |
| TITLE-08 | Runtime gradient textures tracked and destroyed via registry | All new Texture2D allocations routed through UITextureRegistry.Register(); DestroyAll() in OnDisable/OnDestroy |
| TITLE-09 | Glow overlays use native blur where cheaper than texture approach | Performance rule: blur for small-to-medium elements; keep texture-based for full-screen vignettes and multi-stop gradients |
| TITLE-10 | Button VFX integrated with new blur-based glow system | ButtonVFXHelper.SetupHoverGlow currently uses inline styles; can adopt FilterFunction.Blur for halo effect |
</phase_requirements>

## Standard Stack

### Core (Already in Project)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Unity UI Toolkit | 6000.3.6f1 | UI framework | Project engine, only UI system used |
| PrimeTween | 1.3.8 | Tween animations | Project standard, target-based overloads (no GC) |
| UIGradientHelper | (project code) | Runtime gradient textures | Only way to do gradients in UI Toolkit |
| TitleScreenAudio | (project code) | Procedural audio | 655 lines, 10 procedural clips already working |
| VERASystem | (project code) | VERA personality singleton | SingletonMonoBehaviour, manages integrity/glitches |
| VERAVoiceController | (project code) | Voice processing singleton | Handles glitch/dual-voice effects |

### Supporting (To Create)
| Library | Purpose | When to Use |
|---------|---------|-------------|
| UITextureRegistry | Texture lifecycle tracking | Every MonoBehaviour that creates runtime Texture2D |
| UIVFXContainer | Z-order layer management | Title screen VFX layering (replaces root.Insert hacks) |
| VERATitleAudio | Title-specific VERA audio | Replaces VERAInteractions() coroutine in TitleScreenAudio |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| UITextureRegistry (List-based) | Dictionary-based tracking | Dictionary overkill for <50 textures per screen; List is simpler and sufficient |
| Native filter:blur | Pre-blurred texture atlas | Blur is GPU-accelerated and cheaper for small elements; atlas is better for full-screen effects |
| Decomposition into MonoBehaviours | Decomposition into pure C# classes | MonoBehaviours allow Inspector configuration and coroutine usage; pure C# would need explicit lifecycle management |

## Architecture Patterns

### Recommended Project Structure
```
Assets/Scripts/UI/Core/
    UITextureRegistry.cs         -- NEW: Texture lifecycle tracker
    UIVFXContainer.cs            -- NEW: Z-order layer manager
    UIGradientHelper.cs          -- EXTEND: Add cache, fix leaky API
    TitleScreenVFX.cs            -- REFACTOR: Slim orchestrator only
Assets/Scripts/UI/VFX/
    ParticleSystem.cs            -- NEW: Ember/ash/spark/micro-spark management
    LightningSystem.cs           -- NEW: Lightning strikes + flash overlay
    LogoVFXSystem.cs             -- NEW: Logo breathing, pulse, glow, aura
    AtmosphericSystem.cs         -- NEW: Fog, vignette, grunge, atmosphere gradient
    VideoBackgroundSystem.cs     -- NEW: Ping-pong video loop
Assets/Scripts/Audio/
    VERATitleAudio.cs            -- NEW: Randomized VERA interactions with cooldowns
Assets/Scripts/UI/Core/
    TitleScreenAudio.cs          -- MODIFY: Remove VERAInteractions, delegate to VERATitleAudio
```

### Pattern 1: UITextureRegistry (Texture Lifecycle)
**What:** Centralized tracking of all runtime-generated Texture2D instances with guaranteed cleanup.
**When to use:** Every MonoBehaviour that creates Texture2D via UIGradientHelper or direct `new Texture2D()`.
**Example:**
```csharp
// Source: Architecture research + CharSelectVisualEnhancer pattern
namespace VeilBreakers.UI.Core
{
    public class UITextureRegistry
    {
        private readonly List<Texture2D> _textures = new();

        public Texture2D Register(Texture2D tex)
        {
            if (tex != null) _textures.Add(tex);
            return tex;
        }

        public void DestroyAll()
        {
            foreach (var tex in _textures)
            {
                if (tex != null) UnityEngine.Object.Destroy(tex);
            }
            _textures.Clear();
        }
    }
}

// Usage in a MonoBehaviour:
private readonly UITextureRegistry _registry = new();

void ApplyGradient()
{
    var tex = _registry.Register(UIGradientHelper.CreateVerticalGradient(top, bottom));
    UIGradientHelper.ApplyGradient(element, tex);
}

void OnDisable() => _registry.DestroyAll();
```

### Pattern 2: Native Filter Blur (Unity 6000.3)
**What:** GPU-accelerated blur filter applied to VisualElements via `style.filter`.
**When to use:** Small-to-medium glow halos, button glow effects, panel soft edges. NOT for full-screen vignettes.
**Example:**
```csharp
// Source: Unity 6000.3 Architecture research
// Replace radial gradient texture glow with native blur:
glowElement.style.filter = new FilterFunction[] { FilterFunction.Blur(8f) };
glowElement.style.backgroundColor = glowColor;

// For button hover glow:
private void SetupHoverGlow(Button button)
{
    var glow = new VisualElement();
    glow.pickingMode = PickingMode.Ignore;
    glow.style.position = Position.Absolute;
    glow.style.left = -6; glow.style.top = -6;
    glow.style.right = -6; glow.style.bottom = -6;
    glow.style.backgroundColor = new Color(1f, 0.4f, 0.12f, 0.15f);
    glow.style.filter = new FilterFunction[] { FilterFunction.Blur(12f) };
    glow.style.opacity = 0;
    button.Add(glow);
    // Animate opacity on hover via PrimeTween or style.transition
}
```

### Pattern 3: VERA Audio Randomization with Cooldowns
**What:** Weighted random pool of VERA interactions with per-pattern cooldown and recently-played exclusion.
**When to use:** Title screen VERA voice interactions (replacing sequential pattern cycling).
**Example:**
```csharp
// Source: feedback_vera_title_audio + existing VERAInteractions pattern
// CURRENT (bad): sequential cycling through 4 fixed patterns
// int pattern = _interactionIndex % 4;

// NEW: weighted random with cooldown
private struct VeraInteraction
{
    public string Id;
    public AudioClip VeraClip;
    public AudioClip DemonClip;  // nullable
    public float Weight;
    public float CooldownSeconds;
    public float LastPlayedTime;
}

private List<VeraInteraction> _interactions;
private float _globalCooldown = 8f; // minimum gap between ANY interaction
private float _lastInteractionTime;

VeraInteraction SelectInteraction()
{
    float now = Time.unscaledTime;
    var pool = _interactions.Where(i =>
        i.Weight > 0 &&
        now - i.LastPlayedTime > i.CooldownSeconds &&
        now - _lastInteractionTime > _globalCooldown
    ).ToList();

    if (pool.Count == 0) return null;
    return pool[WeightedRandom(pool.Select(i => i.Weight).ToList())];
}
```

### Pattern 4: God Class Decomposition Strategy
**What:** TitleScreenVFX (3146 lines) decomposed into focused subsystems, each managing one visual domain.
**When to use:** Any time a MonoBehaviour exceeds ~500 lines with multiple visual domains.
**Decomposition targets for TitleScreenVFX:**

| Subsystem | Lines (est.) | Methods to Extract | Dependencies |
|-----------|-------------|---------------------|-------------|
| ParticleSystem | ~600 | CreateEmber, CreateAsh, CreateSpark, CreateMicroSpark, UpdateEmber/Ash/Spark, ResetPositions | Particle configs, wind/turbulence state |
| LightningSystem | ~400 | CreateLightningLayer, CreateLightningStrikeElement, UpdateLightning, TriggerLightningStrike, ScheduleNextLightning | Lightning configs, lightning layer VE |
| LogoVFXSystem | ~350 | SetupLogoInteractions, UpdateLogo, TriggerLogoPulse, CreateLogoAura, SpawnLogoSmokeBurst | Logo VEs, logo configs, aura state |
| AtmosphericSystem | ~250 | CreateAtmosphereLayer, CreateVignetteLayer, CreateVignetteCorner, CreateGrungeOverlay, CreateFogLayers | Atmospheric VEs, configs |
| VideoBackgroundSystem | ~400 | SetupVideoBackground, SetupPingPongEvents, video prepared/ended callbacks, ApplyBackground | VideoPlayers, RenderTextures |

**Orchestrator (slim TitleScreenVFX):** ~200 lines, holds references to subsystems, exposes public API (StartVFX, StopVFX, SetIntensity, OnButtonHovered, SparkBurst), manages lifecycle.

### Anti-Patterns to Avoid
- **Sequential VERA patterns:** Cycling through interactions in order (`index % 4`) is predictable and not organic. Use weighted random.
- **Texture leak in CreateGlowOverlay:** UIGradientHelper.CreateGlowOverlay creates an internal Texture2D that no caller can destroy. Either return the texture or accept a registry parameter.
- **Root.Insert(0, ...) for z-ordering:** Six independent VFX controllers inserting at position 0 creates unpredictable z-ordering. Use UIVFXContainer with named layers.
- **Blur on full-screen elements:** filter:blur is GPU-accelerated but expensive on large surfaces. Full-screen vignettes should stay texture-based.
- **Keeping Texture2D readable after creation:** `tex.Apply(false, false)` keeps the CPU-side copy. Use `tex.Apply(false, true)` (makeNoLongerReadable) on textures that won't be modified.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Glow/blur effects | Layered radial gradient textures | Native `FilterFunction.Blur()` | GPU-accelerated, zero allocation, cheaper than texture approach for small elements |
| Gradient textures | CSS gradient in USS | UIGradientHelper.CreateVerticalGradient() | USS does not support CSS gradients -- silent parse failure |
| Texture cleanup | Individual field tracking per texture | UITextureRegistry list pattern | 7-15 fields per class is error-prone; centralized list is simpler and safer |
| VFX z-ordering | Manual `root.Insert(index, child)` calls | UIVFXContainer named layers | Predictable ordering, eliminates fragile index math across 6 controllers |
| Tween animations | DOTween or hand-rolled coroutines | PrimeTween target-based overloads | Project standard, no GC allocations, already integrated |

**Key insight:** The codebase already has 70% of the infrastructure. This phase is structural refactoring (decomposition + registry) and targeted enhancement (blur + audio randomization), not greenfield development.

## Common Pitfalls

### Pitfall 1: UIGradientHelper.CreateGlowOverlay Leaks Textures
**What goes wrong:** `CreateGlowOverlay()` creates a radial gradient Texture2D internally but does not return it. The caller receives only the VisualElement and has no way to destroy the texture.
**Why it happens:** API was designed before the UITextureRegistry pattern existed.
**How to avoid:** Refactor CreateGlowOverlay to return the texture, or accept a UITextureRegistry parameter. For Phase 5, the simplest fix: return the Texture2D as an out parameter.
**Warning signs:** UIGradientHelper.CreateGlowOverlay called in a loop or on scene transitions without cleanup.

### Pitfall 2: Decomposition Breaks Coroutines
**What goes wrong:** TitleScreenVFX uses coroutines extensively (InitializeDeferred, InitializeStaggered, UpdateParticles, video setup). Moving methods to non-MonoBehaviour classes breaks StartCoroutine calls.
**Why it happens:** Coroutines require a MonoBehaviour context. Pure C# classes cannot start coroutines.
**How to avoid:** Either (a) make subsystems MonoBehaviours on child GameObjects, or (b) have the orchestrator own all coroutines and delegate update logic to subsystem methods. Approach (a) is simpler for this codebase.
**Warning signs:** NullReferenceException on StartCoroutine after decomposition.

### Pitfall 3: Native Blur Not Supported in All Contexts
**What goes wrong:** `style.filter` with `FilterFunction.Blur()` may not render correctly in all UI Toolkit contexts (e.g., inside certain layout containers or when combined with certain other style properties).
**Why it happens:** FilterFunction is a relatively new API in Unity 6000.3. Edge cases may exist.
**How to avoid:** TEST EARLY. Create a simple test element in the title scene, apply blur, screenshot, verify. Do this before building any production blur effects. Keep texture-based glow as fallback.
**Warning signs:** Blurred element renders as invisible, or blur has no visual effect.

### Pitfall 4: Sequential VERA Interactions After "Randomization"
**What goes wrong:** Using `Random.Range(0, patterns.Length)` without cooldown or exclusion still produces repetitive-feeling interactions if the random happens to pick the same pattern 2-3 times in a row.
**Why it happens:** True randomness clusters; human perception expects even distribution.
**How to avoid:** Implement weighted random pool with per-pattern cooldown AND recently-played exclusion (do not repeat the last 2 patterns). The user explicitly requested "each interaction should be unique and feel organic."
**Warning signs:** VERA plays the same interaction type twice in a row within 30 seconds.

### Pitfall 5: Texture Registry Not Called for Blur Transition
**What goes wrong:** When replacing radial gradient textures with native blur, the old textures might still be generated by other code paths and not cleaned up because the registry was bypassed.
**Why it happens:** Mixed codebase where some paths create textures directly and others go through the registry.
**How to avoid:** Audit ALL Texture2D creation paths after introducing UITextureRegistry. Every `new Texture2D()` and every `UIGradientHelper.Create*()` call must route through `_registry.Register()`.
**Warning signs:** Unity Profiler shows Texture2D count increasing across scene transitions.

### Pitfall 6: DestroyAll During Active Animation
**What goes wrong:** Calling UITextureRegistry.DestroyAll() while PrimeTween is still animating a texture's opacity causes visual glitches or null reference errors.
**Why it happens:** PrimeTween holds a reference to the target element, which holds a reference to the texture via backgroundImage.
**How to avoid:** Cancel all active tweens before destroying textures. Stop VFX before cleanup. The existing pattern (StopVFX -> OnDisable -> DestroyAll) is correct; ensure decomposition preserves this order.
**Warning signs:** Pink/missing texture flashes during scene transitions.

## Code Examples

### UITextureRegistry Integration with UIGradientHelper
```csharp
// Source: Project architecture research + CharSelectVisualEnhancer pattern
// BEFORE (leak-prone, 7+ individual fields):
private Texture2D _btnBaseGradient;
private Texture2D _btnHoverGradient;
private Texture2D _logoBacking;
// ... 4 more fields
void OnDisable()
{
    if (_btnBaseGradient != null) { Destroy(_btnBaseGradient); _btnBaseGradient = null; }
    if (_btnHoverGradient != null) { Destroy(_btnHoverGradient); _btnHoverGradient = null; }
    // ... 4 more cleanup lines
}

// AFTER (registry-based, single cleanup):
private readonly UITextureRegistry _textures = new();
void ApplyGradients()
{
    _textures.Register(UIGradientHelper.CreateVerticalGradient(top, bottom));
    // Apply to elements...
}
void OnDisable() => _textures.DestroyAll();
```

### Decomposed TitleScreenVFX Orchestrator Pattern
```csharp
// Source: TitleScreenVFX.cs analysis
// The orchestrator holds subsystems and exposes the narrow public API
public class TitleScreenVFX : MonoBehaviour
{
    [SerializeField] private ParticleSystem _particles;
    [SerializeField] private LightningSystem _lightning;
    [SerializeField] private LogoVFXSystem _logo;
    [SerializeField] private AtmosphericSystem _atmosphere;
    [SerializeField] private VideoBackgroundSystem _video;

    private readonly UITextureRegistry _textures = new();

    public void StartVFX()
    {
        _video?.StartPlayback();
        _particles?.StartEmission();
        _lightning?.Enable();
        _logo?.StartBreathing();
    }

    public void StopVFX()
    {
        _particles?.StopEmission();
        _lightning?.Disable();
        _logo?.StopBreathing();
    }

    public void SetIntensity(float intensity)
    {
        _particles?.SetIntensity(intensity);
    }

    void OnDisable()
    {
        StopVFX();
        _textures.DestroyAll();
    }
}
```

### VERA Title Audio Randomization
```csharp
// Source: feedback_vera_title_audio + existing VERAInteractions() pattern
// Weighted random selection with cooldown and history exclusion
public class VERATitleAudio : MonoBehaviour
{
    [System.Serializable]
    private class InteractionDef
    {
        public string id;
        public AudioClip veraClip;
        public AudioClip demonClip;     // may be null
        public float weight = 1f;
        public float cooldown = 30f;
        [System.NonSerialized] public float lastPlayedTime = -999f;
    }

    [SerializeField] private InteractionDef[] _interactions;
    [SerializeField] private float _globalCooldown = 12f;
    [SerializeField] private int _historySize = 2;

    private List<string> _recentHistory = new();
    private float _lastPlayTime;

    public InteractionDef SelectNext()
    {
        float now = Time.unscaledTime;
        var eligible = new List<int>();
        var weights = new List<float>();

        for (int i = 0; i < _interactions.Length; i++)
        {
            var def = _interactions[i];
            if (now - def.lastPlayedTime < def.cooldown) continue;
            if (now - _lastPlayTime < _globalCooldown) continue;
            if (_recentHistory.Contains(def.id)) continue;

            eligible.Add(i);
            weights.Add(def.weight);
        }

        if (eligible.Count == 0) return null;

        int chosen = eligible[WeightedRandom(weights)];
        var selected = _interactions[chosen];
        selected.lastPlayedTime = now;
        _lastPlayTime = now;

        _recentHistory.Add(selected.id);
        if (_recentHistory.Count > _historySize) _recentHistory.RemoveAt(0);

        return selected;
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Sequential VERA pattern cycling (index % 4) | Weighted random pool with cooldowns | Phase 5 | Interactions feel organic, not scripted |
| Radial gradient textures for glow overlays | Native `FilterFunction.Blur()` | Unity 6000.3 | GPU-accelerated, zero Texture2D allocation for glow |
| Individual Texture2D fields + manual cleanup | UITextureRegistry list pattern | Phase 5 | Single DestroyAll() call, no missed textures |
| root.Insert(0, child) for z-ordering | UIVFXContainer named layers | Phase 5 | Predictable z-order across multiple VFX controllers |
| 3146-line TitleScreenVFX god class | Decomposed subsystems | Phase 5 | Each system is testable, configurable, and maintainable |

**Deprecated/outdated:**
- CharacterSelect.uss comment "UI Toolkit can't replicate blur cleanly": outdated as of Unity 6000.3
- TitleScreenAudio.VERAInteractions() sequential pattern: being replaced with randomized approach
- UIGradientHelper.CreateGlowOverlay() texture leak: being fixed with registry integration

## Open Questions

1. **FilterFunction.Blur() rendering verification needed**
   - What we know: Architecture research confirms Unity 6000.3 supports it, API is `style.filter = new FilterFunction[] { FilterFunction.Blur(8f) }`
   - What's unclear: Whether it renders correctly in all UI Toolkit contexts (nested containers, specific layout modes)
   - Recommendation: Create a test element early in Plan 1. Screenshot and verify visually before building production blur effects.

2. **Subsystem architecture: MonoBehaviour vs pure C#**
   - What we know: TitleScreenVFX uses coroutines heavily. Subsystems need access to StartCoroutine and StopCoroutine.
   - What's unclear: Whether subsystems should be MonoBehaviours on child GameObjects (simpler, Inspector-configurable) or pure C# classes with coroutine delegates (lighter weight, no GO overhead).
   - Recommendation: Use MonoBehaviours for subsystems that use coroutines (ParticleSystem, VideoBackgroundSystem, LightningSystem). Pure C# classes for stateless helpers (AtmosphericSystem). The orchestrator creates and wires them in Awake.

3. **Title-05 through Title-10 requirement definitions**
   - What we know: ROADMAP names TITLE-01 through TITLE-10 but only 4 are detailed in success criteria. Phase E in REQUIREMENTS.md says "requirements will be defined during planning."
   - What's unclear: Exact scope of TITLE-02, TITLE-03, TITLE-05, TITLE-08, TITLE-09, TITLE-10.
   - Recommendation: Planner should define these based on the research findings in this document. The TITLE-* IDs in the `<phase_requirements>` section above are inferred from success criteria and codebase analysis.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Unity Play Mode Tests (NUnit) |
| Config file | None -- tests run via Unity Editor |
| Quick run command | `unity_editor action=run_tests` (via vb-unity MCP) |
| Full suite command | `unity_qa action=analyze_code` + visual screenshot verification |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| TITLE-01 | UITextureRegistry tracks and destroys all textures | unit | Verify DestroyAll destroys all registered textures | Wave 0 |
| TITLE-04 | TitleScreenVFX decomposed, public API unchanged | smoke | Compile check + screenshot comparison pre/post decomposition | Wave 0 |
| TITLE-06 | VERA audio randomization with cooldowns | unit | Verify weighted random excludes recent history and respects cooldowns | Wave 0 |
| TITLE-07 | Native filter:blur renders visually | manual + screenshot | Apply blur to test element, screenshot, verify | Wave 0 |

### Sampling Rate
- **Per task commit:** Compile check via `unity_editor action=recompile`
- **Per wave merge:** Visual screenshot comparison via `unity_editor action=screenshot`
- **Phase gate:** Full visual QA pipeline (design -> screenshot -> diff check)

### Wave 0 Gaps
- [ ] UITextureRegistry unit tests -- covers TITLE-01
- [ ] VERATitleAudio weighted random tests -- covers TITLE-06
- [ ] FilterFunction.Blur visual test element -- covers TITLE-07
- [ ] Decomposition smoke test (compile + screenshot before/after) -- covers TITLE-04

## Sources

### Primary (HIGH confidence)
- Project source code: TitleScreenVFX.cs (3146 lines), MainMenuController.cs (1604 lines), UIGradientHelper.cs (212 lines), TitleScreenAudio.cs (655 lines), CharSelectVisualEnhancer.cs (DestroyTex pattern), MenuBootstrap.cs
- Architecture research (.planning/research/ARCHITECTURE.md) -- UITextureRegistry, UIVFXContainer, FilterFunction.Blur designs
- Pitfalls research (.planning/research/PITFALLS.md) -- CRIT-2: Texture2D memory leaks
- Features research (.planning/research/FEATURES.md) -- UI Toolkit constraints, AAA game patterns

### Secondary (MEDIUM confidence)
- Project rules: .claude/rules/ui/toolkit.md -- USS limitations, C# patterns, PrimeTween guidelines
- Project rules: .claude/rules/systems/audio.md -- Audio system known issues, clip standards
- VERA skill: .claude/skills/veilbreakers-vera-test/SKILL.md -- VERA dual nature, glitch system, personality layers
- Context7: /needle-mirror/com.unity.ui -- UI Toolkit API verification (required before any code)

### Tertiary (LOW confidence)
- FilterFunction.Blur performance on large elements -- architecture research claims "GPU-accelerated" but not profiled in this project
- Exact filter:blur behavior with nested UI Toolkit containers -- needs runtime verification

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- all libraries already in project, versions confirmed
- Architecture: HIGH -- decomposition targets identified from line-by-line analysis of TitleScreenVFX
- Pitfalls: HIGH -- verified against existing codebase patterns and previous phase fixes
- Blur API: MEDIUM -- documented in Unity 6000.3 research but not yet tested in this project runtime
- VERA audio randomization: HIGH -- pattern is straightforward, existing code provides all needed infrastructure

**Research date:** 2026-03-31
**Valid until:** 2026-04-30 (stable -- no fast-moving dependencies)
