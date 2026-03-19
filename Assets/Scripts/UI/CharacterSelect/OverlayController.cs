using UnityEngine;
using UnityEngine.UIElements;
using PrimeTween;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Manages scanline/vignette/veil-glow overlay VisualElements with per-hero intensity
    /// and embark hold intensification. Pure C# class (not MonoBehaviour) -- instantiated
    /// by the character select manager and operates on VisualElements directly.
    /// </summary>
    public class OverlayController
    {
        // =============================================================================
        // CONSTANTS
        // =============================================================================

        private const string kScanlineClass = "overlay-scanlines";
        private const string kVignetteClass = "overlay-vignette";
        private const string kVeilGlowClass = "overlay-veil-glow";
        private const string kInactiveDimClass = "panel-inactive-dim";

        // =============================================================================
        // CACHED ELEMENTS
        // =============================================================================

        private VisualElement _scanlines;
        private VisualElement _vignette;
        private VisualElement _veilGlow;

        // =============================================================================
        // STATE
        // =============================================================================

        private HeroThemeConfig _currentTheme;
        private Sequence _transitionSequence;

        // Cached current border alpha for vignette animation
        private float _currentVignetteAlpha;

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        /// <summary>
        /// Queries overlay elements from the root and configures them for animation.
        /// Must be called once during character select screen setup.
        /// </summary>
        public void Init(VisualElement root)
        {
            _scanlines = root.Q<VisualElement>(className: kScanlineClass);
            _vignette = root.Q<VisualElement>(className: kVignetteClass);
            _veilGlow = root.Q<VisualElement>(className: kVeilGlowClass);

            // Set UsageHints for GPU-optimized animation on all overlay elements
            ConfigureElement(_scanlines);
            ConfigureElement(_vignette);
            ConfigureElement(_veilGlow);
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Transitions overlay intensities to match the given hero theme over the specified duration.
        /// Animates scanline opacity, vignette border-color alpha, and veil glow opacity.
        /// Returns a PrimeTween Sequence for composition into larger timelines.
        /// </summary>
        public Sequence TransitionTo(HeroThemeConfig theme, float duration = 0.5f)
        {
            _transitionSequence.Stop();
            _currentTheme = theme;

            var seq = Sequence.Create();

            // Scanline opacity transition
            if (_scanlines != null)
            {
                seq.Group(Tween.StyleOpacity(_scanlines, theme.scanlineOpacity, duration));
            }

            // Vignette: animate border-color alpha for intensity.
            // NOTE: border-width is NOT a GPU-safe animation property. Instead, keep
            // border-width static and animate border-color alpha for intensity control.
            if (_vignette != null)
            {
                float targetAlpha = theme.vignetteIntensity;
                float startAlpha = _currentVignetteAlpha;

                seq.Group(Tween.Custom(startAlpha, targetAlpha, duration,
                    onValueChange: val =>
                    {
                        _currentVignetteAlpha = val;
                        var borderColor = new Color(0f, 0f, 0f, val);
                        _vignette.style.borderTopColor = borderColor;
                        _vignette.style.borderBottomColor = borderColor;
                        _vignette.style.borderLeftColor = borderColor;
                        _vignette.style.borderRightColor = borderColor;
                    }));
            }

            // Veil glow opacity transition
            if (_veilGlow != null)
            {
                seq.Group(Tween.StyleOpacity(_veilGlow, theme.veilGlowOpacity, duration));
            }

            _transitionSequence = seq;
            return seq;
        }

        /// <summary>
        /// Intensifies overlays proportional to embark hold progress (0.0 to 1.0).
        /// Called per-frame during embark hold via OnEmbarkHoldProgress event.
        /// Uses Mathf.Lerp from base theme values to intensified values -- no tween, direct set.
        /// </summary>
        public void SetEmbarkIntensification(float progress)
        {
            if (_currentTheme == null) return;

            // Scanlines: base -> 1.5x intensity
            if (_scanlines != null)
            {
                float baseOpacity = _currentTheme.scanlineOpacity;
                float intensified = baseOpacity * 1.5f;
                _scanlines.style.opacity = Mathf.Lerp(baseOpacity, intensified, progress);
            }

            // Vignette: border-color alpha from base -> 1.3x intensity
            if (_vignette != null)
            {
                float baseAlpha = _currentTheme.vignetteIntensity;
                float intensified = Mathf.Clamp01(baseAlpha * 1.3f);
                float alpha = Mathf.Lerp(baseAlpha, intensified, progress);
                _currentVignetteAlpha = alpha;
                var borderColor = new Color(0f, 0f, 0f, alpha);
                _vignette.style.borderTopColor = borderColor;
                _vignette.style.borderBottomColor = borderColor;
                _vignette.style.borderLeftColor = borderColor;
                _vignette.style.borderRightColor = borderColor;
            }

            // Veil glow: base -> 1.5x intensity
            if (_veilGlow != null)
            {
                float baseOpacity = _currentTheme.veilGlowOpacity;
                float intensified = baseOpacity * 1.5f;
                _veilGlow.style.opacity = Mathf.Lerp(baseOpacity, intensified, progress);
            }
        }

        /// <summary>
        /// Applies dim/desaturate to inactive panels for visual depth hierarchy (VISUAL-07).
        /// Adds or removes the panel-inactive-dim CSS class which applies opacity reduction.
        /// </summary>
        public void SetPanelInactive(VisualElement panel, bool inactive)
        {
            if (panel == null) return;

            if (inactive)
            {
                panel.AddToClassList(kInactiveDimClass);
            }
            else
            {
                panel.RemoveFromClassList(kInactiveDimClass);
            }
        }

        /// <summary>
        /// Stops any active overlay transition. Call during cleanup or scene exit.
        /// </summary>
        public void StopTransitions()
        {
            _transitionSequence.Stop();
        }

        // =============================================================================
        // HELPERS
        // =============================================================================

        private static void ConfigureElement(VisualElement element)
        {
            if (element == null) return;

            // GPU optimization hints for animated overlays
            element.usageHints = UsageHints.DynamicTransform | UsageHints.DynamicColor;

            // Reinforce pointer passthrough (already in USS, but defensive in code)
            element.pickingMode = PickingMode.Ignore;
        }
    }
}
