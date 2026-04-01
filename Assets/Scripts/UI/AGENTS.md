# VeilBreakers UI System Rules

## UI Framework
- USE UI Toolkit (UXML + USS + C#) — NEVER IMGUI
- USE PrimeTween for animations — NEVER DOTween or coroutines for UI animation
- USE Context7 BEFORE writing any UI Toolkit or PrimeTween code: resolve `/needle-mirror/com.unity.ui` and `/kyrylokuzyk/primetween`

## Visual QA Pipeline
1. Design → mockup or reference screenshot
2. Extract spec → zai ui_to_artifact (output_type=spec)
3. Implement → UXML + USS + C#/PrimeTween
4. Capture → unity_editor action=screenshot
5. Compare → zai ui_diff_check (expected vs actual)
6. Iterate → fix gaps until pass

## PrimeTween Rules (CRITICAL)
- NEVER guess PrimeTween API names — hallucinated APIs have cost entire sessions
- ALWAYS verify against package source or Context7 before writing
- VisualElementOpacity, VisualElementPosition, VisualElementScale (NOT Style*)
- Callback comes BEFORE ease in Custom tween constructor

## USS Pitfalls (CRITICAL)
- USS has NO gradients, NO box-shadow, NO blur filters — use C# for these
- If USS `background-color` is set, it OVERRIDES runtime `Texture2D`
- No `style.gap` (unavailable on IStyle in Unity 6) — use child margins instead
- Use target-based PrimeTween overloads (no closures/allocations), not closure-based

## Responsive Testing
- Test UI at 1920x1080 minimum, responsive at 1280x720 and 2560x1440

## Theme
- Dark fantasy aesthetic
- Brand colors are canonical — never modify without user approval
- See project memory for hero color RGB values

## Code Style
- Namespace: VeilBreakers.UI
- kConstant, _private, PascalProperty, OnEvent
