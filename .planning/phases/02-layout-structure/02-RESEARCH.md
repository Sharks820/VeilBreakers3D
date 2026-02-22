# Phase 2: Layout & Structure - Research

**Researched:** 2026-02-22
**Domain:** Unity UI Toolkit layout, USS transitions, UXML structure, panel sizing at 1920x1080
**Confidence:** HIGH

## Summary

Phase 2 focuses on fixing pixel-correct layout at 1920x1080 for the Character Select screen. The codebase has a well-structured UXML with left panel (hero info), right panel (stats/abilities), center stage (3D render), bottom carousel, and embark button. However, several issues need resolution: the `EnsureFullScreenLayout()` hack walks the visual tree setting flex-grow imperatively; the USS contains 10+ `transition: all` declarations that animate layout-triggering properties (width, height, margin, left, top); the `stat-bar-fill` explicitly transitions `width`; and the hero backstory/description data exists in JSON but has no UXML section to display it.

The core challenge is surgical: fix the UXML root to handle full-screen natively (eliminating the C# hack), audit and rewrite all USS transitions to only animate transform/color/opacity properties, ensure `UsageHints.DynamicTransform` is set on every animated element at creation time, and verify that all panel content renders correctly for all 4 heroes at 1920x1080 without text overlapping or clipping.

**Primary recommendation:** Fix the UXML root configuration and TemplateContainer USS rule to make `EnsureFullScreenLayout()` unnecessary, then systematically replace every `transition: all` with explicit property lists limited to `translate, scale, rotate, opacity, color, background-color, border-color`, and add the missing hero backstory/synergy display section to the left panel UXML.

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| LAYOUT-01 | Hero info panel text overlap fixed (name, title, quote, path, role display correctly without collision) | USS analysis shows fixed `width: 280px` left panel with correct padding/margins but potential overlap from long synergy_explanation text (e.g., Seraphina's is 130+ chars). Fix via `white-space: normal`, `overflow: hidden`, and proper `min-height` on content sections. Panel sizing verified in current USS. |
| LAYOUT-02 | Champion/starter monster area displays correctly with name, brand, and role tags | C# code in `HeroDataPanelController.PopulateChampion()` already populates champion name, brand (`GetPrimaryBrand()`), and role (`GetAIPattern()`). Verify USS `.champion-section` and `.tag` styles render correctly at target resolution. Current UXML structure is correct. |
| LAYOUT-03 | Hero stories and synergy/brands populate correctly for all 4 heroes | **GAP FOUND:** Hero backstory exists in `HeroData.backstory` (rich text, 200-400 chars per hero) but NO UXML section displays it. The `synergy_explanation` field IS displayed via `hero-synergy` label but text is uppercased and may clip. Need to add a backstory/lore section to left panel or create a scrollable area. |
| LAYOUT-04 | Panel sizing correct at 1920x1080 with no overflow or clipping | Left panel: `width: 280px`, `padding: 24px`, `position: absolute`, `top/left: 40px`. Right panel: identical dimensions, `right: 40px`. At 1920x1080, available center space is 1920 - 280*2 - 40*2 = 1240px. Confirm no overflow by ensuring `overflow: hidden` on panels and `white-space: normal` on long text labels. |
| LAYOUT-05 | `EnsureFullScreenLayout()` hack removed by fixing UXML root configuration | Current hack walks visual tree setting `flexGrow=1` on every ancestor. Fix: the USS already has `TemplateContainer { flex-grow: 1; }` in both VeilBreakers.uss (line 16-18) and CharacterSelect.uss (line 81-83). The `.character-select-root` uses `position: absolute; width: 100%; height: 100%`. The hack may be redundant if the USS loads correctly. Test removal; if layout collapses, the root VisualElement of UIDocument may need explicit USS targeting. |
| LAYOUT-06 | All USS transitions audited to animate only transform/color properties (no width/height/margin) | **10 violations found in CharacterSelect.uss:** Lines 160, 197, 444, 479, 507, 531, 589, 604, 649 use `transition: all`. Line 416 uses `transition: width`. **6 violations in VeilBreakers.uss:** Lines 1018, 1581, 2435, 2445, 2760 use `transition-property: width` or include width/height. All must be rewritten to explicit property lists. |
| LAYOUT-07 | `UsageHints.DynamicTransform` set on all animated VisualElements at creation time | **Partial compliance:** `hero-info-panel`, `stats-panel`, carousel cards, parallax layers, render target already have hints set in C# `CacheReferences()`. **Gaps:** Elements with `transition: all` or `:hover` scale/translate effects (`.nav-arrow`, `.btn-back`, `.btn-embark`, `.ability-slot`, `.hero-card`, `.embark-hex-bg`, `.confirm-overlay`) do NOT have `UsageHints` set. All hover-animated elements need hints set at creation time. |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Unity UI Toolkit | 6000.3.6f1 (built-in) | USS/UXML layout system | Native Unity 6 UI framework, Flexbox-based layout via Yoga engine |
| USS (Unity Style Sheets) | Built-in | Styling and transitions | CSS-like styling with Unity-specific properties and transition support |
| UXML | Built-in | Declarative UI structure | XML-based UI element declaration with style class binding |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| UIDocument | Built-in | Runtime UXML hosting | Attaches UXML to GameObjects; rootVisualElement is the entry point |
| VisualElement.usageHints | Built-in | GPU optimization flags | Set `DynamicTransform` on ANY element that animates translate/scale/rotate |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| USS transitions for stat bars | PrimeTween (Phase 4) | USS `transition: width` triggers layout recalc; Phase 4 will replace with PrimeTween `scaleX` animation. For Phase 2, use `scaleX` transform instead of `width` to avoid layout recalc. |
| C# `style.width` for bar fills | USS `transform: scaleX()` | ScaleX avoids layout recalculation but requires the bar to be full-width with scale applied. Simpler to keep `style.width` but remove the USS `transition: width` and let PrimeTween handle animation in Phase 4. |

## Architecture Patterns

### Recommended Project Structure (existing, no changes needed)
```
Assets/
  UI/
    Screens/
      CharacterSelect.uxml     # Main screen layout
    Styles/
      VeilBreakers.uss          # Global styles + variables
      CharacterSelect.uss       # Screen-specific styles
  Scripts/
    UI/CharacterSelect/
      CharacterSelectManager.cs      # Orchestrator
      HeroDataPanelController.cs     # Left panel population
      HeroStatsPanelController.cs    # Right panel population
      CarouselController.cs          # Bottom carousel
      CharSelectEnvironmentController.cs  # Background/parallax
      CharSelectEvents.cs            # Event bus
      CharSelectUIUtils.cs           # Shared utilities
```

### Pattern 1: USS Transition Property Whitelist
**What:** Replace all `transition: all` declarations with explicit property lists limited to GPU-accelerated properties.
**When to use:** Every USS rule that currently uses `transition: all` or transitions layout-triggering properties.
**Example:**
```css
/* Source: Unity 6000.3 docs - Optimizing Performance */

/* BAD - triggers layout recalculation */
.glass-panel {
    transition: all 0.4s ease-out-back;
}

/* GOOD - only GPU-accelerated properties */
.glass-panel {
    transition: translate 0.4s ease-out-back,
                opacity 0.4s ease-out-back,
                border-color 0.4s ease-out-back;
}
```

### Pattern 2: TemplateContainer Full-Screen Fix
**What:** Ensure the UXML root fills the viewport without C# hacks by relying on USS rules and proper UXML structure.
**When to use:** Replacing `EnsureFullScreenLayout()` in CharacterSelectManager.
**Example:**
```css
/* Source: Unity docs - Position element with layout engine */

/* Already exists in both USS files - verify it loads */
TemplateContainer {
    flex-grow: 1;
}

/* Root element uses absolute positioning to fill viewport */
.character-select-root {
    position: absolute;
    left: 0;
    top: 0;
    width: 100%;
    height: 100%;
}
```

### Pattern 3: UsageHints at Creation Time
**What:** Set `UsageHints.DynamicTransform` on elements BEFORE they are added to a panel or BEFORE transitions fire, not after.
**When to use:** Every element that will have translate, scale, rotate, or opacity transitions.
**Example:**
```csharp
// Source: Unity 6000.3 docs - Move elements at runtime
// Set BEFORE adding to panel or triggering transitions
var card = new VisualElement();
card.usageHints = UsageHints.DynamicTransform;
card.AddToClassList("hero-card");
_parent.Add(card); // Now the hint is active when transitions fire
```

### Pattern 4: Text Overflow Prevention
**What:** Use `white-space: normal` for multi-line text and `overflow: hidden` on containers to prevent content bleeding.
**When to use:** Long text like synergy explanations, backstories, quotes.
**Example:**
```css
/* Source: Unity USS Common Properties */

.hero-synergy-text {
    white-space: normal;
    overflow: hidden;
    -unity-text-align: upper-left;
    font-size: 11px;
    max-height: 60px; /* Prevent excessive expansion */
}
```

### Anti-Patterns to Avoid
- **`transition: all`:** Animates EVERY property including width, height, margin, padding -- triggers full layout recalculation every frame. Always list specific properties.
- **`style.width` for animated bar fills:** Changing width triggers layout recalc. Use `scaleX` transform or defer animation to PrimeTween (Phase 4). For Phase 2, remove the USS `transition: width` from `.stat-bar-fill` and set width immediately (no animation) -- Phase 4 will add proper PrimeTween animation.
- **Imperative flex-grow walking:** The `EnsureFullScreenLayout()` hack sets `flexGrow=1` on every ancestor. This is fragile and masks the real problem (missing USS rule or USS load order). Fix the USS, don't work around it.
- **Class toggling on large hierarchies:** The Unity docs warn that switching CSS classes triggers "extensive style recalculations." For simple state changes like showing/hiding, prefer `style.display = DisplayStyle.None` over adding `.hidden` class when performance matters. (However, the current `.hidden` class approach is acceptable for infrequent operations like show/hide confirm overlay.)

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Full-screen layout | C# tree walking setting flexGrow | USS `TemplateContainer { flex-grow: 1; }` | The USS rule is declarative, always applied, and doesn't depend on C# execution timing |
| Stat bar animation | Custom width lerp in Update | Remove USS `transition: width`; defer animation to PrimeTween in Phase 4 | Width transitions cause layout recalc; PrimeTween scaleX avoids this |
| Text clipping prevention | C# string truncation with "..." | USS `text-overflow: ellipsis` or `white-space: normal; overflow: hidden;` | USS handles this declaratively without C# string manipulation |
| Panel slide animations | Custom translate lerp in Update | USS `transition: translate` with class toggling | Already working via `panel-hidden` class; just needs proper `transition-property` list |

**Key insight:** Layout problems in UI Toolkit are almost always solved in USS, not C#. The C# code should only set data (text, visibility); the USS should handle all sizing, positioning, and transitions.

## Common Pitfalls

### Pitfall 1: `transition: all` Performance Death
**What goes wrong:** Using `transition: all` means that when a theme class changes (e.g., `theme-vex` to `theme-nyx`), EVERY CSS property on the element transitions -- including `width`, `height`, `margin`, `padding`, `border-width`. Each of these triggers a full layout recalculation PER FRAME of the transition.
**Why it happens:** CSS muscle memory from web development where `transition: all` is common and GPUs handle it. Unity's UI Toolkit layout engine (Yoga) is more expensive to recalculate.
**How to avoid:** Always list specific transition properties. Only use `translate`, `scale`, `rotate`, `opacity`, `color`, `background-color`, `border-color`, `tint-color` in transition lists.
**Warning signs:** Stuttery animations, frame drops during hero switch, profiler showing Yoga layout calls during transitions.

### Pitfall 2: USS Rule Load Order
**What goes wrong:** Removing `EnsureFullScreenLayout()` causes the UI to collapse to zero height because the `TemplateContainer { flex-grow: 1; }` rule is in CharacterSelect.uss but hasn't loaded yet when the UIDocument initializes.
**Why it happens:** USS sheets load asynchronously or in a different order than expected. The UXML references CharacterSelect.uss via `<Style src="..."/>` but the global VeilBreakers.uss is loaded via the PanelSettings default style.
**How to avoid:** Verify that both the PanelSettings default stylesheet AND the UXML `<Style>` reference are working. After removing the hack, test that the layout works on a fresh scene load (not just domain reload).
**Warning signs:** Layout collapses on first load but works after a script recompile/domain reload.

### Pitfall 3: Long Text Breaking Panel Layout
**What goes wrong:** Seraphina's `synergy_explanation` is 131 characters: "Grimthorn's SAVAGE aggression and paralytic toxins amplify Seraphina's FANGBORN hunting instincts. Together they are death by a thousand cuts -- and a single scratch." When uppercased and displayed in a 280px panel, this text can overflow the container.
**Why it happens:** The `info-value` class has no `white-space: normal` or `max-height` constraint, so text either clips silently or expands the panel beyond the viewport.
**How to avoid:** Set `white-space: normal` on the synergy label to allow wrapping. Add `overflow: hidden` on the info panel. Consider reducing font size for synergy text or abbreviating.
**Warning signs:** Panel extends below the bottom of the screen for heroes with long synergy explanations.

### Pitfall 4: Backstory Section Missing from UXML
**What goes wrong:** LAYOUT-03 requires "Hero stories text" to populate correctly, but the current UXML has NO section for displaying `HeroData.backstory` (200-400 characters per hero). The backstory field exists in the JSON data model.
**Why it happens:** The original UXML was designed for compact info display; backstory was not included in the initial layout.
**How to avoid:** Add a scrollable or fixed-height backstory section to the left panel UXML. Given the 280px width and 1080px height constraint, the backstory needs either a ScrollView or a fixed-height container with overflow hidden and a "read more" pattern.
**Warning signs:** LAYOUT-03 success criteria mentions "Hero stories text" but there's nowhere to display it.

### Pitfall 5: Scale Transitions Without UsageHints
**What goes wrong:** Multiple elements use `scale: 1.1` or `scale: 1.25` on hover/selected states (`.nav-arrow:hover`, `.hero-card:hover`, `.hero-card.selected`, `.btn-embark:hover .embark-hex-bg`) but don't have `UsageHints.DynamicTransform` set.
**Why it happens:** These elements are defined in UXML, not created in C#. The C# code only sets hints on panels and carousel cards, missing the statically-defined UXML elements.
**How to avoid:** In the relevant controller's `CacheReferences()`, query these elements and set `usageHints = UsageHints.DynamicTransform`. Alternatively, set hints on the CharacterSelectManager level for all interactive elements.
**Warning signs:** One-frame performance penalty each time a hover transition starts on an element without the hint pre-set.

## Code Examples

Verified patterns from official sources:

### Rewriting `transition: all` to Explicit Property List
```css
/* Source: Unity 6000.3 docs - USS transition + Optimizing Performance */

/* BEFORE (CharacterSelect.uss line 160) */
.glass-panel {
    transition: all var(--transition-normal) var(--ease-out-expo);
}

/* AFTER */
.glass-panel {
    transition: border-color var(--transition-normal) var(--ease-out-expo);
}

/* BEFORE (CharacterSelect.uss line 444) */
.ability-slot {
    transition: all var(--transition-fast) var(--ease-out-expo);
}

/* AFTER */
.ability-slot {
    transition: translate var(--transition-fast) var(--ease-out-expo),
                background-color var(--transition-fast) var(--ease-out-expo),
                border-left-color var(--transition-fast) var(--ease-out-expo),
                border-left-width var(--transition-fast) var(--ease-out-expo);
}
/* NOTE: border-left-width is a layout property but it's a very small change (4px->6px)
   and only on hover, so acceptable. If performance is a concern, remove it and keep
   border-left-width static. */
```

### Removing stat-bar-fill Width Transition
```css
/* Source: Unity 6000.3 docs - Optimizing Performance */

/* BEFORE (CharacterSelect.uss line 416) */
.stat-bar-fill {
    transition: width var(--transition-slow) var(--ease-out-expo);
}

/* AFTER - remove transition, set width immediately */
/* Phase 4 will add PrimeTween scaleX animation */
.stat-bar-fill {
    /* No transition - width is set instantly in C# */
    /* PrimeTween will animate this in Phase 4 via scaleX */
}
```

### Setting UsageHints on UXML-Defined Elements
```csharp
// Source: Unity 6000.3 docs - Move elements at runtime
// In CharacterSelectManager.CacheUIReferences() or a new method:

private void SetAnimationHints()
{
    // All elements that have USS transitions need DynamicTransform
    SetHint(_root.Q<VisualElement>("hero-info-panel"));
    SetHint(_root.Q<VisualElement>("stats-panel"));
    SetHint(_root.Q<Button>("btn-back"));
    SetHint(_root.Q<Button>("btn-embark"));
    SetHint(_root.Q<VisualElement>("embark-hex-bg"));
    SetHint(_root.Q<VisualElement>("embark-glow"));
    SetHint(_root.Q<Button>("btn-prev"));
    SetHint(_root.Q<Button>("btn-next"));
    SetHint(_root.Q<VisualElement>("confirm-overlay"));

    // Ability slots
    for (int i = 0; i < 5; i++)
    {
        SetHint(_root.Q<Label>($"ability-{i}"));
    }
}

private static void SetHint(VisualElement el)
{
    if (el != null) el.usageHints = UsageHints.DynamicTransform;
}
```

### Adding Backstory Section to UXML (for LAYOUT-03)
```xml
<!-- Add inside hero-info-panel, after champion-section -->
<ui:VisualElement name="hero-lore-section" class="hero-lore-section">
    <ui:Label class="section-header" text="LORE" />
    <ui:Label name="hero-backstory" class="hero-backstory"
              text="Hero backstory text..." />
</ui:VisualElement>
```

```css
/* USS for backstory section */
.hero-lore-section {
    margin-top: 16px;
    padding-top: 12px;
    border-top-width: 1px;
    border-top-color: rgba(255, 255, 255, 0.08);
    max-height: 120px;
    overflow: hidden;
}

.hero-backstory {
    font-size: 10px;
    color: rgba(200, 200, 215, 0.6);
    white-space: normal;
    -unity-font-style: italic;
}
```

## Inventory of All Transition Violations

### CharacterSelect.uss - MUST FIX
| Line | Selector | Current | Should Be |
|------|----------|---------|-----------|
| 136 | `.fog-particles` | `transition: background-color ...` | OK (color only) -- KEEP |
| 160 | `.glass-panel` | `transition: all ...` | `transition: border-color ...` |
| 180-181 | `.hero-info-panel` | `transition: translate ..., opacity ...` | OK -- KEEP |
| 197 | `.hero-name` | `transition: all ...` | `transition: color ...` (only color changes with theme) |
| 331 | `.drag-hint` | `transition: opacity 0.6s` | OK -- KEEP |
| 368-369 | `.stats-panel` | `transition: translate ..., opacity ...` | OK -- KEEP |
| 416 | `.stat-bar-fill` | `transition: width ...` | REMOVE (layout property) |
| 444 | `.ability-slot` | `transition: all ...` | `transition: translate ..., background-color ..., border-left-color ...` |
| 479 | `.nav-arrow` | `transition: all ...` | `transition: color ..., border-color ..., scale ...` |
| 507 | `.hero-card` | `transition: all ...` | `transition: scale ..., opacity ..., border-color ...` |
| 531 | `.hero-card-initial` | `transition: all ...` | `transition: opacity ...` |
| 589 | `.embark-hex-bg` | `transition: all ...` | `transition: border-color ..., background-color ..., scale ...` |
| 604 | `.embark-text` | `transition: all ...` | `transition: color ..., scale ...` |
| 625 | `.embark-glow` | `transition: opacity ...` | OK -- KEEP |
| 649 | `.btn-back` | `transition: all ...` | `transition: color ..., border-color ...` |
| 678 | `.confirm-overlay` | `transition: opacity ...` | OK -- KEEP |

### VeilBreakers.uss - Should audit but lower priority (Phase 2 focus is CharacterSelect)
| Line | Selector | Issue |
|------|----------|-------|
| 200 | `.vb-button` | Uses explicit list: `scale, background-color, border-color, translate, opacity` -- OK |
| 1018 | (combat stat bar) | `transition-property: width` -- not CharSelect, defer |
| 1581 | (combat element) | `transition-property: width, background-color` -- not CharSelect, defer |
| 1844 | (unknown) | `transition: all 0.2s ease` -- audit but likely not CharSelect |

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `transition: all` for convenience | Explicit property lists only | Unity 6 perf docs (2024) | Prevents layout recalculation during transitions |
| C# `style.top`/`style.left` for movement | `style.translate` + `UsageHints.DynamicTransform` | Unity 6000.x (2024) | GPU-accelerated transform, no layout dirtying |
| Manual `flexGrow` walking in C# | USS `TemplateContainer { flex-grow: 1; }` | Always available but under-documented | Declarative, load-order safe |
| `style.width` for stat bar animation | `scaleX` transform or PrimeTween | Unity 6 perf guidance (2024) | Width changes trigger expensive Yoga layout recalc |

**Deprecated/outdated:**
- `EnsureFullScreenLayout()` pattern: was needed when USS load order was unreliable; now the `TemplateContainer` USS rule is the standard fix
- `transition: all`: explicitly discouraged in Unity 6 performance docs; always use specific properties

## Open Questions

1. **TemplateContainer USS Load Order**
   - What we know: Both USS files define `TemplateContainer { flex-grow: 1; }`. The UXML references CharacterSelect.uss. The PanelSettings should reference VeilBreakers.uss as default.
   - What's unclear: Whether removing `EnsureFullScreenLayout()` will cause a layout collapse on fresh scene load (as opposed to domain reload). The C# hack may be masking a real USS loading issue.
   - Recommendation: Remove the hack, test on fresh scene load. If layout collapses, check PanelSettings configuration and ensure VeilBreakers.uss is set as the default stylesheet on the PanelSettings asset.

2. **Backstory Display Strategy**
   - What we know: LAYOUT-03 mentions "Hero stories text." Each hero has a 200-400 character backstory in JSON. The left panel is 280px wide.
   - What's unclear: Whether the requirement means adding the full backstory or just a brief summary. Adding a scrollable section increases complexity.
   - Recommendation: Add a fixed-height (100-120px) backstory section with `overflow: hidden` and `white-space: normal`. Truncate with CSS, no ScrollView needed at this phase. If full backstory display is needed, Phase 4 can add a "Read More" interaction.

3. **border-left-width Transition on .ability-slot**
   - What we know: `.ability-slot:hover` changes `border-left-width: 4px -> 6px`. This IS a layout property. But it's a hover-only microinteraction.
   - What's unclear: Whether this 2px change causes noticeable layout recalculation.
   - Recommendation: Keep it for now; the layout change is minimal (2px on a single element). If profiler shows issues, change to a non-layout approach (e.g., fixed 6px border with color change on hover).

4. **VeilBreakers.uss Width Transitions**
   - What we know: Lines 1018, 1581, 2435, 2445, 2760 in VeilBreakers.uss transition width. These are likely for combat UI stat bars, not CharacterSelect.
   - What's unclear: Whether these fire during CharacterSelect (VeilBreakers.uss is global).
   - Recommendation: For Phase 2, only fix CharacterSelect.uss violations. Audit VeilBreakers.uss width transitions in Phase 5 (QUAL-04).

## Sources

### Primary (HIGH confidence)
- [Unity 6000.3 - Optimizing Performance](https://docs.unity3d.com/6000.3/Documentation/Manual/best-practice-guides/ui-toolkit-for-advanced-unity-developers/optimizing-performance.html) - UsageHints, DynamicTransform, transform vs layout property animation
- [Unity 6000.3 - Move Elements at Runtime](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-move-elements-at-runtime.html) - style.translate best practice, DynamicTransform recommendation
- [Unity - USS Transitions](https://docs.unity3d.com/Manual/UIE-Transitions.html) - Transition syntax, `all` keyword behavior, performance caveats
- [Unity 6000.3 - Layouts](https://docs.unity3d.com/6000.3/Documentation/Manual/best-practice-guides/ui-toolkit-for-advanced-unity-developers/layouts.html) - Flexbox, flex-grow, full-screen layout
- Codebase inspection - CharacterSelect.uxml, CharacterSelect.uss, VeilBreakers.uss, all CharSelect C# controllers

### Secondary (MEDIUM confidence)
- [Unity Discussions - TemplateContainer flex-grow](https://forum.unity.com/threads/why-isnt-templatecontainer-flex-grow-1-by-default.613324/) - Community confirmation of TemplateContainer pattern
- [Unity Discussions - TemplateContainer layout impact](https://discussions.unity.com/t/opinion-templatecontainers-should-have-no-impact-on-the-layout/872477) - Known issue discussion

### Tertiary (LOW confidence)
- None -- all findings verified against official docs and codebase

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Unity UI Toolkit is native, versions verified against project
- Architecture: HIGH - Existing codebase structure is clear, patterns well-documented
- Pitfalls: HIGH - All transition violations enumerated from actual USS files, verified against Unity docs
- LAYOUT-03 backstory gap: MEDIUM - Requirement text says "Hero stories" but could mean synergy explanation alone; interpreted as needing backstory display

**Research date:** 2026-02-22
**Valid until:** 2026-03-22 (Unity UI Toolkit APIs are stable in Unity 6)
