# Technology Stack: Character Select Rebuild

**Project:** VeilBreakers 3D - Character Select Rebuild Milestone
**Researched:** 2026-02-21
**Overall Confidence:** MEDIUM-HIGH (verified against Unity 6.3 official docs, some areas training-data-only)

## Context

This stack research is scoped to the Character Select rebuild milestone in an existing Unity 6000.3.6f1 (Unity 6.3 LTS) project. The core engine, combat systems, save system, and data pipeline are already built and validated. This document covers what additional packages, techniques, and patterns are needed specifically for AAA-quality character selection UI, visual polish, game flow fixes, and performance optimization.

The project already uses UI Toolkit (UXML/USS), URP 17.3.0, and the New Input System 1.18.0.

---

## Recommended Stack Additions

### Animation & Tweening

| Technology | Version | Purpose | Why | Confidence |
|------------|---------|---------|-----|------------|
| PrimeTween | 1.3.7 | Complex UI animations, sequences, transitions beyond USS capabilities | Zero-allocation, direct VisualElement support (opacity, color, backgroundColor), sequence chaining, Inspector-configurable TweenSettings. DOTween is legacy; PrimeTween is purpose-built for modern Unity with explicit UI Toolkit APIs. | HIGH |
| USS Transitions (built-in) | Unity 6.3 native | Simple state-based transitions (hover, focus, panel slide) | No additional dependency. Supports 25+ easing functions (ease-in-elastic, ease-in-bounce, etc.). Use for CSS-like hover effects and pseudo-state changes. Reserve PrimeTween for orchestrated multi-element sequences. | HIGH |

**Why PrimeTween over DOTween:** DOTween allocates memory on every tween creation. PrimeTween is zero-allocation by design, supports UI Toolkit VisualElement natively (Tween.VisualElementOpacity, Tween.VisualElementColor, Tween.VisualElementBackgroundColor), and has a direct DOTween migration path. PrimeTween 1.3.7 supports sequences, delays, and Inspector-configured TweenSettings<T> without code changes.

**Why PrimeTween over LitMotion:** LitMotion (5x faster than DOTween) is DOTS-based and optimized for ECS workloads. VeilBreakers uses MonoBehaviour architecture with no DOTS. PrimeTween is the right tool for this codebase -- zero allocation without requiring an architectural shift.

**Why NOT just USS Transitions:** USS transitions only interpolate between two states of a CSS property. They cannot orchestrate multi-element sequences (panel A slides out, THEN panel B slides in, THEN particles play), cannot animate along paths, and have no Timeline/Animator integration. The Character Select rebuild needs coordinated sequences for hero switching, which requires a tweening library.

**Installation:**
```
# Via Unity Package Manager > Add package by name
com.kyrylokuzyk.primetween
# Or via Asset Store import (includes demo scene)
```

### Visual Effects (USS Filters -- Unity 6.3 Native)

| Technology | Version | Purpose | Why | Confidence |
|------------|---------|---------|-----|------------|
| USS Filters (built-in) | Unity 6.3 native | Blur, grayscale, sepia, tint, hue-rotate, contrast, invert, opacity effects on VisualElements | New in Unity 6.3 -- CSS-style filter pipeline on any VisualElement subtree. Enables blur(20px), grayscale(100%), tint(#color), hue-rotate(90deg), contrast(200%), and combinations. Filters are animatable via USS transitions. No shader code needed. | HIGH |

**Available built-in filters (Unity 6.3):**
```
filter: blur(<length>)         -- Gaussian blur, e.g. blur(20px)
filter: grayscale(<number>)    -- 0% original, 100% fully grayscale
filter: invert(<number>)       -- Color inversion
filter: opacity(<number>)      -- Transparency
filter: sepia(<number>)        -- Warm brownish tone
filter: tint(<color>)          -- Color overlay (Unity-specific, not in CSS)
filter: hue-rotate(<angle>)    -- Color wheel rotation
filter: contrast(<number>)     -- Contrast adjustment
```

**Combinable:** `filter: blur(5px) tint(#ff000080) contrast(120%);`
**Animatable via USS transitions:** `transition: filter 0.5s ease-in-out;`

**Limitation:** USS filters affect only the UI element and its children -- they do NOT blur the 3D scene behind UI. For scene-behind-UI blur (glassmorphism), use a URP full-screen render pass or the Unified-Universal-Blur package.

### Visual Effects (UI Shader Graph -- Unity 6.3 Native)

| Technology | Version | Purpose | Why | Confidence |
|------------|---------|---------|-----|------------|
| UI Shader Graph (built-in) | Unity 6.3 native | Custom material effects on UI elements: glow, animated gradients, distortion, color processing | New in Unity 6.3 -- Create > Shader Graph > URP > UI Shader Graph. Enables custom visual effects directly on VisualElements via material assignment. Shader effects cascade to all child elements. Requires URP (already in project). | MEDIUM |

**What you can build:**
- Animated gradient backgrounds (brand-colored energy flows per hero)
- Glow/pulse effects on buttons and panels
- UV distortion for "veil tear" effects
- Color processing (grayscale inactive heroes, sepia flashbacks)
- Dynamic text effects via `TextElement.PostProcessTextVertices` (glyph-level animation)

**Key constraint:** UI Shader Graph creates shaders that render UI mesh elements directly. It CANNOT create post-processing filters that apply to rendered subtrees. For that, use USS filters.

**How to apply:**
1. Create UI Shader Graph asset
2. Create Material using that shader
3. In UI Builder, assign Material to VisualElement's Material dropdown
4. Shader affects element AND all children

### 3D Character Preview

| Technology | Version | Purpose | Why | Confidence |
|------------|---------|---------|-----|------------|
| Render Texture (built-in) | Unity 6.3 native | Display 3D hero model in character select UI | Standard approach: dedicated Camera renders hero model on isolated layer to RenderTexture, displayed as background-image on VisualElement. Used in Unity's Dragon Crashers sample. No alternative for mixing 3D content into UI Toolkit panels. | HIGH |

**Implementation approach:**
1. Create RenderTexture asset (1024x1024, ARGB32, 16-bit depth minimum)
2. Add Camera to CharacterSelect scene rendering only the "HeroPreview" layer
3. Position hero model prefab on that layer with appropriate lighting
4. Assign RenderTexture as Camera's Target Texture
5. In USS: `background-image: url("path/to/RenderTexture");` or set via C# `style.backgroundImage`
6. Use URP Volume with Bloom + Depth of Field on preview camera for cinematic hero presentation

**Performance note:** RenderTextures are expensive. For a character select screen (not a hot gameplay loop), this is acceptable. Profile to ensure the preview camera is only active when the CharacterSelect scene is loaded.

### URP Post-Processing (Scene-Level Visual Polish)

| Technology | Version | Purpose | Why | Confidence |
|------------|---------|---------|-----|------------|
| URP Volume System | 17.3.0 | Scene-level visual effects for character select environment | Already in project. Configure a Volume Profile on the CharacterSelect scene camera with Bloom, Vignette, Depth of Field, Color Adjustments, and Tonemapping for cinematic presentation. | HIGH |

**Recommended Volume Overrides for Character Select:**

| Effect | Purpose | Settings Guidance |
|--------|---------|-------------------|
| Bloom | Hero model glow, brand-energy effects, ambient light bleed | Intensity 0.5-2.0, Threshold 0.8-1.2. Unity 6.3 adds Kawase/Dual filtering (faster than Gaussian). Use Kawase for best quality/perf balance. |
| Depth of Field (Bokeh) | Cinematic hero focus, background blur | Use Bokeh mode for AAA quality. Gaussian is faster but less convincing. Focus on hero model distance. |
| Vignette | Frame darkening for dramatic character presentation | Intensity 0.2-0.4, Smoothness 0.3. Subtle -- do not overdo. |
| Color Adjustments | Per-hero color grading, mood shifts on hero switch | Post Exposure, Saturation, Contrast. Swap profiles per hero for thematic color shifts. |
| Tonemapping | Consistent exposure across hero environments | ACES mode for cinematic look. |
| Film Grain | Subtle texture for AAA feel | Intensity 0.1-0.2 maximum. Optional. |

**Per-hero Volume switching:** Create separate Volume Profile assets per hero (or use Volume blending with multiple Volumes at different priorities). Swap/blend on hero change for dramatic mood shifts.

### Input & Navigation

| Technology | Version | Purpose | Why | Confidence |
|------------|---------|---------|-----|------------|
| Input System | 1.18.0 (already installed) | Gamepad/keyboard navigation of character select | Already in project. From Unity 2023.2+, Input System and UI Toolkit are fully integrated -- no EventSystem/InputSystemUIInputModule needed for UI Toolkit panels. Project-wide input actions map directly to UI Toolkit events. | HIGH |

**Critical pattern for gamepad support:**
- `ClickEvent` (mouse) and `NavigationSubmitEvent` (gamepad A/Enter) are DISTINCT events in UI Toolkit
- Button handlers must register for BOTH or use `Clickable` manipulator (which handles both)
- Use `tabIndex` and `focusable` properties to control navigation order
- `NavigationMoveEvent` handles D-pad/stick navigation between elements
- Set `delegatesFocus = true` on container elements to auto-pass focus to children

**Existing issue in codebase:** Legacy `Input.mousePosition` mixed with New Input System. All mouse queries must route through InputManager.

---

## Existing Stack (No Changes Needed)

These are already in the project and require no version changes or additions for this milestone.

| Technology | Version | Status |
|------------|---------|--------|
| Unity Engine | 6000.3.6f1 (6.3 LTS) | Locked. Do not upgrade. |
| URP | 17.3.0 | Use existing. Configure Volume Profiles per scene. |
| Input System | 1.18.0 | Use existing. Fix legacy Input.* calls. |
| UI Toolkit (UIElements) | 1.0.0 (Unity module) | Use existing. Leverage new 6.3 features (USS filters, UI Shader Graph). |
| TextMeshPro | Unity module | Use existing for text rendering. |
| Addressables | 2.8.0 | Use existing for asset loading if hero model prefabs become complex. |
| Newtonsoft.Json | 3.2.1 (transitive) | Use existing. No changes. |

---

## Alternatives Considered

| Category | Recommended | Alternative | Why Not |
|----------|-------------|-------------|---------|
| Tweening | PrimeTween 1.3.7 | DOTween Pro | DOTween allocates on every tween. Legacy API. PrimeTween has native VisualElement support. |
| Tweening | PrimeTween 1.3.7 | LitMotion | DOTS-based architecture; overkill for MonoBehaviour project. Excellent perf but wrong paradigm. |
| Tweening | PrimeTween 1.3.7 | USS Transitions only | Cannot orchestrate multi-element sequences, no code-driven control, limited to two-state interpolation. |
| UI Blur | USS filter: blur() | Unified-Universal-Blur package | USS filter handles UI-element blur natively. Only need the package if scene-behind-UI glassmorphism is required. |
| UI Effects | UI Shader Graph | Custom ShaderLab | UI Shader Graph uses visual node editor, integrates with UI Toolkit material system, no raw HLSL needed. |
| UI Effects | USS Filters | Custom render passes | USS filters cover 90% of UI visual effects without shader code. Only use render passes for scene-level effects. |
| 3D Preview | RenderTexture + Camera | Screen Space Overlay model | UI Toolkit does not support 3D objects in its render tree. RenderTexture is the only viable approach. |
| Particles in UI | RenderTexture approach | UI Toolkit Particles (Asset Store) | RenderTexture particles are consistent with hero preview approach. Asset Store plugin adds dependency risk. If budget allows, evaluate com.kamgam.uitoolkitparticles. |
| Animation framework | PrimeTween sequences | Unity Timeline | Timeline is scene-bound, not code-driven, harder to trigger dynamically. PrimeTween sequences are code-first, zero-alloc, and composable. |

---

## Performance Optimization Techniques

### UI Toolkit Specific

| Technique | What | Why | Confidence |
|-----------|------|-----|------------|
| `UsageHints.DynamicTransform` | Set on any VisualElement that animates position/rotation/scale | Pushes transform updates directly to GPU, bypasses CPU mesh recalculation. Already used in TransitionController.cs. Apply to ALL animated elements. | HIGH |
| `UsageHints.DynamicColor` | Set on elements with animated colors/opacity | Optimizes color-change rendering path (fast pass). Apply to elements that fade, pulse, or change color. | MEDIUM |
| `style.translate` over `style.left/top` | Use translate for position animations | translate operates at transform stage without triggering layout recalculation. left/top/width/height trigger cascading layout updates across siblings. | HIGH |
| Pre-create all elements | Build full UI tree at init, show/hide via Display.None | Avoids runtime VisualElement instantiation and GC pressure. Never create/destroy elements during hero switching. | HIGH |
| Cache all Q<> references | Query VisualElements once in OnEnable, store in fields | Q<> and Q() traverse the visual tree. Calling in Update or per-frame callbacks causes unnecessary work. | HIGH |
| Avoid data binding for animated properties | Do not use runtime binding on properties that change every frame | UI Toolkit's binding system causes large GC allocations per frame (reported community issue). Set animated properties directly via style. | HIGH |
| Dynamic atlas awareness | Keep UI textures under 8-texture limit per batch | UI Toolkit uses an uber shader with 8-texture slots. Exceeding this breaks batches and increases draw calls. Atlas small textures. | MEDIUM |
| Profile with UI Toolkit Debugger | Use Window > UI Toolkit > Debugger at runtime | Shows visual tree, computed styles, layout boxes, and helps identify layout thrashing. | HIGH |

### General Unity Performance

| Technique | What | Why |
|-----------|------|-----|
| Incremental GC (already enabled) | Spread GC across frames | Already configured in ProjectSettings. Maintain. |
| No allocations in Update/callbacks | Use cached references, pre-allocated lists, avoid string concatenation | Core project constraint. Applies to all new character select code. |
| Profile with Memory Profiler 1.1.9 | Verify zero per-frame allocations on character select screen | Already in project. Use to validate rebuild. |
| Adaptive Performance 6.0.0 | Runtime performance scaling | Already in project. Useful if character select has heavy effects on low-end hardware. |

---

## What NOT to Use

| Technology | Why Not |
|------------|---------|
| Legacy UGUI for new UI | Project constraint: UI Toolkit only. UGUI exists only for VBSceneManager fade canvas (legacy). |
| IMGUI | Editor-only. Never for runtime UI. |
| DOTween | Allocates memory. Legacy design. No native UI Toolkit support. |
| Unity Animator/Animation for UI | Overkill for UI state transitions. USS transitions + PrimeTween cover all cases. Animator adds component overhead. |
| Timeline for UI sequences | Scene-bound, not composable in code, harder to trigger dynamically. Use PrimeTween sequences. |
| Custom singleton patterns | Project uses SingletonMonoBehaviour<T>. Do not introduce alternative patterns. |
| Direct Input.* calls | Must route through InputManager. Legacy Input API mixed in codebase is a known bug to fix. |
| RuntimeAnimatorController on UI | Heavy. Use USS transitions for simple states, PrimeTween for complex sequences. |
| Coroutine-based animation | Project already has a coroutine-Task bridge marked for revisit. PrimeTween's sequence API is cleaner and allocation-free. |

---

## Package Installation Plan

### New Package (1 addition only)

```
# Via Package Manager > Add package by name:
com.kyrylokuzyk.primetween

# Or via manifest.json:
"com.kyrylokuzyk.primetween": "1.3.7"
```

### Existing Packages to Configure (not install)

| Package | Configuration Needed |
|---------|---------------------|
| URP 17.3.0 | Create CharacterSelect-specific Volume Profile with Bloom (Kawase), DoF (Bokeh), Vignette, Color Adjustments |
| UI Toolkit module | Leverage new USS filters (blur, tint, contrast) and UI Shader Graph for custom effects |
| Input System 1.18.0 | Verify NavigationSubmitEvent handlers alongside ClickEvent on all character select buttons |
| Addressables 2.8.0 | Consider for hero model prefab loading if models become large assets |

### No New Heavy Dependencies

The character select rebuild should add exactly ONE new package (PrimeTween). Everything else leverages Unity 6.3's native capabilities -- USS filters, UI Shader Graph, Volume system, and Input System integration are all built-in and ready to use at the project's current version.

---

## Version Compatibility Matrix

| Component | Required Version | Project Version | Status |
|-----------|-----------------|----------------|--------|
| Unity Engine | 6000.3.x (6.3 LTS) for USS filters, UI Shader Graph | 6000.3.6f1 | COMPATIBLE |
| URP | 17.x for Kawase bloom, alpha processing | 17.3.0 | COMPATIBLE |
| Input System | 1.7+ for full UI Toolkit integration | 1.18.0 | COMPATIBLE |
| PrimeTween | 1.3.7 (latest) | Not installed | TO INSTALL |
| UI Toolkit module | Built into Unity 6.3 | 1.0.0 | COMPATIBLE |

---

## Sources

### Official Documentation (HIGH confidence)
- [USS Transitions - Unity 6.3 Manual](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-Transitions.html)
- [USS Filters - Unity 6.3 Manual](https://docs.unity3d.com/6000.3/Documentation/Manual/ui-systems/uss-filter.html)
- [Built-in Filters - Unity 6.3 Manual](https://docs.unity3d.com/Manual/ui-systems/built-in-filters.html)
- [UI Shader Graph Introduction - Unity 6.3 Manual](https://docs.unity3d.com/6000.3/Documentation/Manual/ui-systems/introduction-to-ui-shader-graph.html)
- [UI Shader Graph Getting Started - Unity 6.3 Manual](https://docs.unity3d.com/6000.3/Documentation/Manual/ui-systems/get-started-with-ui-shader-graph.html)
- [Focus System - Unity 6.3 Manual](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-focus-order.html)
- [Performance Considerations - Unity 6.3 Manual](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-performance-consideration-runtime.html)
- [UsageHints Optimization - Unity 6.0 Manual](https://docs.unity3d.com/6000.0/Documentation/Manual/UIE-use-usage-hints-to-reduce-draw-calls-and-geometry-regeneration.html)
- [URP Post-Processing - Unity 6.3 Manual](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/post-processing-and-full-screen-effects-urp.html)
- [URP Volumes - Unity 6.3 Manual](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/Volumes.html)
- [What's New in Unity 6.3 - Unity 6.3 Manual](https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html)
- [Unity 6.3 LTS Release](https://unity.com/releases/editor/whats-new/6000.3.6f1)

### Community / Verified Sources (MEDIUM confidence)
- [PrimeTween GitHub](https://github.com/KyryloKuzyk/PrimeTween) - Zero-allocation tween library with UI Toolkit support
- [PrimeTween UI Animations - DeepWiki](https://deepwiki.com/KyryloKuzyk/PrimeTween/4.2-ui-animations) - API details for VisualElement animation
- [UI Toolkit Advanced E-Book (Unity 6 2025)](https://github.com/unity-e-book/UIToolkit/blob/main/UI_Toolkit_for_advanced_Unity_developers_Unity_6_2025.md) - Official Unity e-book with optimization patterns
- [Dragon Crashers Sample](https://assetstore.unity.com/packages/essentials/tutorial-projects/dragon-crashers-ui-toolkit-sample-project-231178) - Reference implementation with render texture character preview
- [Unity 6 UI Toolkit Updates Blog](https://unity.com/blog/unity-6-ui-toolkit-updates) - Feature announcements
- [Scalable Performant UI in Unity 6](https://unity.com/resources/scalable-performant-ui-uitoolkit-unity-6) - Performance e-book

### Community Discussion (LOW confidence -- validate before using)
- [UI Toolkit Binding GC Allocations](https://discussions.unity.com/t/ui-toolkit-binding-system-causing-large-gc-allocations/1697259) - Binding system GC issue reports
- [Glassmorphism Request](https://discussions.unity.com/t/suggesting-native-glassmorphism-support-in-ui-toolkit-for-modern-transparent-ui-designs/1694475) - Background blur not natively supported
- [Unified-Universal-Blur](https://github.com/lukakldiashvili/Unified-Universal-Blur) - Third-party scene blur if needed
- [UI Toolkit Roadmap Discussion](https://discussions.unity.com/t/ui-toolkits-missing-roadmap-from-unite-2025-whats-coming-in-6-4-6-6/1696853) - Future UI Toolkit plans
