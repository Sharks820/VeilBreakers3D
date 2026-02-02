# Unity 6.3 AAA Upgrade Plan - VeilBreakers

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Transform VeilBreakers into a AAA-quality experience by leveraging Unity 6.3's cutting-edge features for rendering, UI, and VFX.

**Architecture:** Three-pillar approach - Performance (GPU optimization), Visual Polish (UI/VFX), and Modern Features (Unity 6 exclusives). Each pillar is independently implementable.

**Tech Stack:** Unity 6.3 LTS, URP 17+, VFX Graph, UI Toolkit with Custom Shaders, GPU Resident Drawer

---

## Research Summary

### Sources Referenced
- [Unity 6 Features Announcement](https://unity.com/blog/unity-6-features-announcement)
- [What's New in URP 17](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/whats-new/urp-whats-new.html)
- [GPU Resident Drawer Setup](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/gpu-resident-drawer.html)
- [UI Toolkit Transitions](https://docs.unity3d.com/6000.0/Documentation/Manual/UIE-Transitions)
- [UI Shader Graph](https://docs.unity3d.com/6000.3/Documentation/Manual/ui-systems/get-started-with-ui-shader-graph.html)
- [UnityUIGlass for Blur Effects](https://github.com/digorlima/UnityUIGlass)
- [Unity 6.3 LTS Features](https://www.cgchannel.com/2025/12/unity-6-3-lts-is-out-see-5-key-features-for-games-artists/)

---

## PILLAR 1: Performance Optimization

### Task 1.1: Enable GPU Resident Drawer

**Impact:** Up to 50% CPU reduction, massive draw call batching

**Files:**
- Modify: `ProjectSettings/GraphicsSettings.asset`
- Modify: `Assets/Settings/VeilBreakersURP.asset`

**Step 1: Configure Shader Stripping**

1. Open Project Settings > Graphics
2. In Shader Stripping section, set **BatchRendererGroup Variants** to **Keep All**

**Step 2: Enable in URP Asset**

1. Open `Assets/Settings/VeilBreakersURP.asset`
2. Enable **SRP Batcher** (if not already)
3. Set **GPU Resident Drawer** to **Instanced Drawing**

**Step 3: Configure Forward+ Rendering**

1. Double-click the renderer in Renderer List to open Universal Renderer
2. Set **Rendering Path** to **Forward+**

**Step 4: Enable GPU Occlusion Culling**

In URP Asset, after enabling GPU Resident Drawer, enable **GPU Occlusion Culling**

**Step 5: Optimize Settings**

1. Project Settings > Player > Other Settings: Disable **Static Batching** (conflicts with GPU Resident Drawer)
2. Window > Panels > Lighting > Lightmapping Settings: Enable **Fixed Lightmap Size**, disable **Use Mipmap Limits**

**Step 6: Commit**

```bash
git add ProjectSettings/GraphicsSettings.asset Assets/Settings/
git commit -m "perf: enable GPU Resident Drawer and Forward+ rendering"
```

---

### Task 1.2: Enable STP Upscaling

**Impact:** Better visual quality at lower render resolution, GPU performance boost

**Files:**
- Modify: `Assets/Settings/VeilBreakersURP.asset`

**Step 1: Configure STP**

1. Open URP Asset
2. Navigate to Quality > Upscaling Filter
3. Select **Spatial Temporal Post-Processing (STP)**
4. Set Render Scale to 0.75-0.85 for performance boost with minimal quality loss

**Step 2: Commit**

```bash
git add Assets/Settings/VeilBreakersURP.asset
git commit -m "perf: enable STP upscaling for GPU optimization"
```

---

### Task 1.3: Configure Render Graph Optimizations

**Impact:** Reduced memory usage, automatic pass merging on mobile TBDR

**Files:**
- Modify: `Assets/Settings/VeilBreakersURP.asset`

**Step 1: Enable Render Graph Caching**

In URP Asset, enable **Cache Render Graph Compilation** (Unity 6.3 feature)

**Step 2: Configure Depth Priming**

Set **Depth Priming Mode** to **Auto** to reduce overdraw with Forward+

**Step 3: Commit**

```bash
git add Assets/Settings/VeilBreakersURP.asset
git commit -m "perf: enable render graph caching and depth priming"
```

---

## PILLAR 2: AAA UI Polish

### Task 2.1: Create Glassmorphism Blur System

**Impact:** Modern AAA frosted glass effect for menus

**Files:**
- Create: `Assets/Scripts/UI/Effects/BlurPanel.cs`
- Create: `Assets/Shaders/UI/UIBlur.shadergraph`
- Create: `Assets/UI/Styles/blur-effects.uss`

**Step 1: Create UI Shader Graph for Blur**

1. Assets > Create > Shader Graph > URP > UI Shader Graph
2. Name it `UIBlur`
3. Add Gaussian blur using scene color sampling

**Implementation Approach:**

Use the [UnityUIGlass](https://github.com/digorlima/UnityUIGlass) approach:
- Create a blur material using Unity 6's Render Graph API
- Expose Size (kernel size) and Strength (blur intensity) parameters
- Apply via custom UI element that extends VisualElement

**Step 2: Create BlurPanel.cs**

```csharp
using UnityEngine;
using UnityEngine.UIElements;

namespace VeilBreakers.UI.Effects
{
    [UxmlElement]
    public partial class BlurPanel : VisualElement
    {
        [UxmlAttribute]
        public float BlurStrength { get; set; } = 5f;

        [UxmlAttribute]
        public float BlurSize { get; set; } = 3f;

        public BlurPanel()
        {
            AddToClassList("blur-panel");
        }
    }
}
```

**Step 3: Create blur-effects.uss**

```uss
.blur-panel {
    background-color: rgba(20, 15, 30, 0.7);
    border-radius: 12px;
    border-width: 1px;
    border-color: rgba(255, 255, 255, 0.1);
}

.blur-panel--strong {
    background-color: rgba(20, 15, 30, 0.85);
}
```

**Step 4: Commit**

```bash
git add Assets/Scripts/UI/Effects/ Assets/Shaders/UI/ Assets/UI/Styles/blur-effects.uss
git commit -m "feat: add glassmorphism blur panel system"
```

---

### Task 2.2: Implement AAA Button Transitions

**Impact:** Smooth, satisfying button interactions

**Files:**
- Modify: `Assets/UI/Styles/vb-buttons.uss`
- Modify: `Assets/UI/Styles/vb-theme-dark.uss`

**Step 1: Add transition properties to buttons**

```uss
/* VeilBreakers AAA Button Styling */
.vb-button {
    /* Transition setup - animate multiple properties */
    transition-property: scale, background-color, border-color, translate;
    transition-duration: 0.15s, 0.2s, 0.2s, 0.1s;
    transition-timing-function: ease-out-cubic;

    /* Base state */
    scale: 1 1;
    translate: 0 0;
    background-color: rgba(30, 25, 45, 0.9);
    border-color: rgba(100, 80, 140, 0.6);
    border-width: 2px;
    border-radius: 8px;
}

.vb-button:hover {
    scale: 1.02 1.02;
    background-color: rgba(50, 40, 70, 0.95);
    border-color: rgba(140, 100, 180, 0.8);
}

.vb-button:active {
    scale: 0.98 0.98;
    translate: 0 2px;
    background-color: rgba(25, 20, 40, 1);
}

.vb-button:focus {
    border-color: rgba(180, 140, 220, 1);
    border-width: 3px;
}

/* Glow effect for selected/important buttons */
.vb-button--primary {
    border-color: rgba(160, 80, 200, 0.8);
}

.vb-button--primary:hover {
    border-color: rgba(200, 100, 255, 1);
    /* Use USS variables for glow via background-image gradient */
}
```

**Step 2: Add menu item transitions**

```uss
.menu-item {
    transition-property: translate, opacity, background-color;
    transition-duration: 0.2s;
    transition-timing-function: ease-out-back;

    translate: 0 0;
    opacity: 1;
}

.menu-item:hover {
    translate: 8px 0;
    background-color: rgba(80, 60, 120, 0.3);
}

.menu-item--entering {
    translate: -50px 0;
    opacity: 0;
}
```

**Step 3: Commit**

```bash
git add Assets/UI/Styles/
git commit -m "ui: add AAA button transitions and hover effects"
```

---

### Task 2.3: Implement SVG Icons (Unity 6.3 Feature)

**Impact:** Crisp icons at any resolution, smaller file sizes

**Files:**
- Create: `Assets/Art/UI/Icons/*.svg`
- Modify: UI templates to use SVG

**Step 1: Create SVG icons for brands**

Create vector icons for:
- IRON, SAVAGE, SURGE, VENOM, DREAD, LEECH, GRACE, MEND, RUIN, VOID brands
- Path icons (IRONBOUND, FANGBORN, VOIDTOUCHED, UNCHAINED)
- Status icons (corruption levels, buffs, debuffs)

**Step 2: Import and configure**

Unity 6.3's integrated Vector Graphics package handles SVG import automatically.

**Step 3: Update templates**

Replace PNG references with SVG in UXML files.

**Step 4: Commit**

```bash
git add Assets/Art/UI/Icons/
git commit -m "art: add vector SVG icons for brands and paths"
```

---

### Task 2.4: Create Animated Stat Bars

**Impact:** Satisfying visual feedback for HP/MP/Resource changes

**Files:**
- Create: `Assets/Scripts/UI/Controls/AnimatedBar.cs`
- Modify: `Assets/UI/Styles/combat-bars.uss`

**Step 1: Create AnimatedBar control**

```csharp
using UnityEngine;
using UnityEngine.UIElements;

namespace VeilBreakers.UI.Controls
{
    [UxmlElement]
    public partial class AnimatedBar : VisualElement
    {
        private VisualElement _fillBar;
        private VisualElement _ghostBar;
        private float _currentValue = 1f;
        private float _targetValue = 1f;

        [UxmlAttribute]
        public float Value
        {
            get => _targetValue;
            set => SetValue(value);
        }

        [UxmlAttribute]
        public Color FillColor { get; set; } = new Color(0.2f, 0.8f, 0.3f);

        [UxmlAttribute]
        public Color GhostColor { get; set; } = new Color(0.8f, 0.2f, 0.2f, 0.5f);

        public AnimatedBar()
        {
            AddToClassList("animated-bar");

            _ghostBar = new VisualElement();
            _ghostBar.AddToClassList("animated-bar__ghost");
            Add(_ghostBar);

            _fillBar = new VisualElement();
            _fillBar.AddToClassList("animated-bar__fill");
            Add(_fillBar);
        }

        private void SetValue(float value)
        {
            _targetValue = Mathf.Clamp01(value);
            // Ghost bar follows with delay via USS transitions
            _fillBar.style.width = Length.Percent(_targetValue * 100f);

            // Delay ghost bar
            schedule.Execute(() =>
            {
                _ghostBar.style.width = Length.Percent(_targetValue * 100f);
            }).ExecuteLater(300);
        }
    }
}
```

**Step 2: Create USS styling**

```uss
.animated-bar {
    height: 12px;
    background-color: rgba(0, 0, 0, 0.6);
    border-radius: 6px;
    overflow: hidden;
    flex-direction: row;
}

.animated-bar__fill {
    position: absolute;
    height: 100%;
    background-color: var(--vb-health-color);
    border-radius: 6px;
    transition-property: width;
    transition-duration: 0.3s;
    transition-timing-function: ease-out-cubic;
}

.animated-bar__ghost {
    position: absolute;
    height: 100%;
    background-color: var(--vb-damage-color);
    border-radius: 6px;
    transition-property: width;
    transition-duration: 0.5s;
    transition-timing-function: ease-in-cubic;
    transition-delay: 0.3s;
}
```

**Step 3: Commit**

```bash
git add Assets/Scripts/UI/Controls/AnimatedBar.cs Assets/UI/Styles/combat-bars.uss
git commit -m "ui: add animated stat bars with ghost damage effect"
```

---

## PILLAR 3: VFX Overhaul

### Task 3.1: Create Menu Particle Systems

**Impact:** Ambient atmosphere for title screen and menus

**Files:**
- Create: `Assets/Art/VFX/UI/MenuParticles.vfx`
- Create: `Assets/Art/VFX/UI/VeilEnergyAmbient.vfx`

**Step 1: Create floating particle VFX**

Using VFX Graph:
- Spawn rate: 5-15 particles/second
- Lifetime: 3-8 seconds
- Movement: Gentle float upward with noise
- Color: Veil energy gradient (deep purple → red based on corruption)
- Size: Screen-space size for consistent appearance

**VFX Graph Setup:**
1. Spawn: Constant Rate (10/s)
2. Initialize: Random position in viewport bounds, random size 2-8px
3. Update: Add Turbulence noise, gentle upward velocity
4. Output: Screen Space particles, additive blending

**Step 2: Commit**

```bash
git add Assets/Art/VFX/UI/
git commit -m "vfx: add menu ambient particle systems"
```

---

### Task 3.2: Create Button Interaction VFX

**Impact:** Satisfying feedback on button clicks

**Files:**
- Create: `Assets/Art/VFX/UI/ButtonClick.vfx`
- Create: `Assets/Art/VFX/UI/ButtonHover.vfx`

**Step 1: Create click burst VFX**

- Trigger: One-shot burst on click
- Particles: 8-15 small energy wisps
- Movement: Radial outward from button center
- Color: Brand-appropriate or neutral purple
- Lifetime: 0.3-0.5 seconds

**Step 2: Create hover glow VFX**

- Continuous subtle glow effect
- Edge particles that follow button bounds

**Step 3: Commit**

```bash
git add Assets/Art/VFX/UI/
git commit -m "vfx: add button interaction particle effects"
```

---

### Task 3.3: Create Corruption Overlay VFX

**Impact:** Visual representation of corruption levels

**Files:**
- Create: `Assets/Art/VFX/UI/CorruptionOverlay.vfx`
- Create: `Assets/Shaders/UI/CorruptionDistortion.shadergraph`

**Step 1: Create screen-edge corruption effect**

Based on corruption level (0-100%):
- 0-25%: Subtle purple wisps at screen edges
- 26-50%: More intense, occasional flickers
- 51-75%: Red veil energy, screen distortion
- 76-100%: Heavy corruption, reality tears

**Step 2: Create distortion shader**

Unity 6.3 VFX Graph + UI Shader Graph for screen-space distortion:
- Normal map UV offset for "heat wave" effect
- Intensity controlled by corruption level

**Step 3: Commit**

```bash
git add Assets/Art/VFX/UI/ Assets/Shaders/UI/
git commit -m "vfx: add corruption overlay system"
```

---

## PILLAR 4: Modern Menu Architecture

### Task 4.1: Create Parallax Background System

**Impact:** Depth and immersion in menus

**Files:**
- Create: `Assets/Scripts/UI/Menus/ParallaxBackground.cs`

**Step 1: Implement parallax layers**

```csharp
using UnityEngine;
using UnityEngine.UIElements;

namespace VeilBreakers.UI.Menus
{
    public class ParallaxBackground : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;
        [SerializeField] private float _intensity = 0.02f;

        private VisualElement _layer1, _layer2, _layer3;
        private Vector2 _basePosition;

        private void Start()
        {
            var root = _document.rootVisualElement;
            _layer1 = root.Q("parallax-bg");
            _layer2 = root.Q("parallax-mid");
            _layer3 = root.Q("parallax-fg");
        }

        private void Update()
        {
            var mousePos = Input.mousePosition;
            var screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            var offset = (new Vector2(mousePos.x, mousePos.y) - screenCenter) / screenCenter;

            ApplyParallax(_layer1, offset, 0.3f);
            ApplyParallax(_layer2, offset, 0.6f);
            ApplyParallax(_layer3, offset, 1.0f);
        }

        private void ApplyParallax(VisualElement layer, Vector2 offset, float multiplier)
        {
            if (layer == null) return;
            var translate = offset * _intensity * multiplier * 100f;
            layer.style.translate = new Translate(translate.x, -translate.y);
        }
    }
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/UI/Menus/ParallaxBackground.cs
git commit -m "ui: add parallax background system for menus"
```

---

### Task 4.2: Implement Screen Transition System

**Impact:** Smooth scene transitions, professional feel

**Files:**
- Create: `Assets/Scripts/UI/Core/ScreenTransition.cs`
- Create: `Assets/UI/Templates/ScreenTransition.uxml`

**Step 1: Create transition controller**

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace VeilBreakers.UI.Core
{
    public class ScreenTransition : MonoBehaviour
    {
        private VisualElement _overlay;
        private const float kTransitionDuration = 0.4f;

        public IEnumerator FadeOut()
        {
            _overlay.style.opacity = 0;
            _overlay.style.display = DisplayStyle.Flex;

            float elapsed = 0;
            while (elapsed < kTransitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _overlay.style.opacity = elapsed / kTransitionDuration;
                yield return null;
            }
            _overlay.style.opacity = 1;
        }

        public IEnumerator FadeIn()
        {
            _overlay.style.opacity = 1;

            float elapsed = 0;
            while (elapsed < kTransitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _overlay.style.opacity = 1 - (elapsed / kTransitionDuration);
                yield return null;
            }

            _overlay.style.opacity = 0;
            _overlay.style.display = DisplayStyle.None;
        }
    }
}
```

**Step 2: Commit**

```bash
git add Assets/Scripts/UI/Core/ScreenTransition.cs Assets/UI/Templates/ScreenTransition.uxml
git commit -m "ui: add screen transition system"
```

---

## Implementation Priority

### Phase 1: Performance Foundation (Do First)
1. Task 1.1: GPU Resident Drawer
2. Task 1.2: STP Upscaling
3. Task 1.3: Render Graph Optimizations

### Phase 2: Core UI Polish
4. Task 2.2: AAA Button Transitions
5. Task 2.4: Animated Stat Bars
6. Task 4.2: Screen Transitions

### Phase 3: Visual Effects
7. Task 3.1: Menu Particles
8. Task 3.2: Button Interaction VFX
9. Task 2.1: Glassmorphism Blur

### Phase 4: Advanced Features
10. Task 2.3: SVG Icons
11. Task 3.3: Corruption Overlay
12. Task 4.1: Parallax Background

---

## Completion Checklist

### Performance
- [ ] GPU Resident Drawer enabled
- [ ] Forward+ rendering path active
- [ ] GPU Occlusion Culling enabled
- [ ] STP upscaling configured
- [ ] Render Graph caching enabled

### UI Polish
- [ ] Button transitions implemented
- [ ] Animated stat bars working
- [ ] Screen transitions smooth
- [ ] Blur panels available
- [ ] SVG icons integrated

### VFX
- [ ] Menu ambient particles
- [ ] Button click/hover effects
- [ ] Corruption overlay system

### Modern Features
- [ ] Parallax backgrounds
- [ ] World-space UI ready (for future 3D integration)

---

*Plan created: 2026-02-01*
*Target: AAA Visual Quality + 50% CPU Performance Improvement*
