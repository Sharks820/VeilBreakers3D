---
paths:
  - "Assets/UI/**/*.uxml"
  - "Assets/UI/**/*.uss"
  - "Assets/Scripts/UI/**/*.cs"
---

# UI Toolkit Rules

## Context7 Mandatory
Before ANY UXML/USS/VisualElement code:
1. `resolve-library-id /needle-mirror/com.unity.ui`
2. `query-docs` for the specific API
If not found, read `Packages/com.unity.ui/` source. NEVER guess.

## USS Limitations (Learned the Hard Way)
- USS has NO gradients, NO box-shadow, NO blur filters
- For AAA visuals: generate `Texture2D` gradients at runtime via C#
- Use layered `VisualElements` for glow/depth effects
- If USS `background-color` is set, it OVERRIDES runtime `Texture2D` — remove it

## C# Patterns
- Query by name: `root.Q<VisualElement>("element-name")`
- Cache Q<T>() results in OnEnable, never in Update
- State changes via `AddToClassList`/`RemoveFromClassList`
- Use `display: none` instead of destroying elements

## PrimeTween
- ALWAYS verify API via Context7 `/kyrylokuzyk/primetween` before writing
- Use target-based overload (no closures/allocations), not closure-based

## Performance
- No nested QuerySelector in Update
- No `style.gap` (unavailable on IStyle in Unity 6 — use child margins)
- Batch style changes where possible
- Test UI at 1920x1080 minimum, responsive at 1280x720 and 2560x1440
