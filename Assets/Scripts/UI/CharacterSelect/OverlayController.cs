using UnityEngine;
using UnityEngine.UIElements;
using PrimeTween;
using VeilBreakers.Core;

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
                seq.Group(Tween.VisualElementOpacity(_scanlines, theme.scanlineOpacity, duration));
            }

            // Vignette: animate border-color alpha for intensity.
            // NOTE: border-width is NOT a GPU-safe animation property. Instead, keep
            // border-width static and animate border-color alpha for intensity control.
            if (_vignette != null)
            {
                float targetAlpha = theme.vignetteIntensity;
                float startAlpha = _currentVignetteAlpha;

                seq.Group(Tween.Custom(this, startAlpha, targetAlpha, duration,
                    onValueChange: (ctrl, val) =>
                    {
                        ctrl._currentVignetteAlpha = val;
                        var borderColor = new Color(0f, 0f, 0f, val);
                        ctrl._vignette.style.borderTopColor = borderColor;
                        ctrl._vignette.style.borderBottomColor = borderColor;
                        ctrl._vignette.style.borderLeftColor = borderColor;
                        ctrl._vignette.style.borderRightColor = borderColor;
                    }));
            }

            // Veil glow opacity transition
            if (_veilGlow != null)
            {
                seq.Group(Tween.VisualElementOpacity(_veilGlow, theme.veilGlowOpacity, duration));
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
        // PARALLAX
        // =============================================================================

        private const float kVeilGlowParallax = 0.5f;
        private const float kScanlineParallax = 1.0f;
        private const float kVignetteParallax = 2.0f;
        private const float kPanelParallax = 4.0f;

        private VisualElement _leftPanel;
        private VisualElement _rightPanel;
        private VisualElement _carousel;
        private Vector2 _currentParallaxOffset;
        private Vector2 _previousParallaxOffset;
        private Vector2 _screenCenter;
        private const float kParallaxSmoothing = 5f;
        private const float kParallaxDirtyThreshold = 0.05f;

        /// <summary>
        /// Registers UI panels for parallax movement.
        /// Call after Init(root) setup.
        /// </summary>
        public void InitParallax(VisualElement leftPanel, VisualElement rightPanel, VisualElement carousel)
        {
            _leftPanel = leftPanel;
            _rightPanel = rightPanel;
            _carousel = carousel;
            _screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            if (leftPanel != null) leftPanel.usageHints |= UsageHints.DynamicTransform;
            if (rightPanel != null) rightPanel.usageHints |= UsageHints.DynamicTransform;
            if (carousel != null) carousel.usageHints |= UsageHints.DynamicTransform;
        }

        /// <summary>
        /// Updates parallax offsets based on mouse/stick position.
        /// Call from MonoBehaviour.LateUpdate() via HeroThemeTransitioner.
        /// UI panels shift 3-5px, overlay layers at different rates for depth.
        /// </summary>
        public void UpdateParallax()
        {
            Vector2 mousePos = InputManager.HasInstance
                ? InputManager.Instance.MousePosition
                : Vector2.zero;

            if (_screenCenter.x <= 0f) _screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            Vector2 normalized = new Vector2(
                (mousePos.x - _screenCenter.x) / Mathf.Max(_screenCenter.x, 1f),
                (mousePos.y - _screenCenter.y) / Mathf.Max(_screenCenter.y, 1f)
            );

            _currentParallaxOffset = Vector2.Lerp(
                _currentParallaxOffset,
                normalized,
                Time.unscaledDeltaTime * kParallaxSmoothing
            );

            if (Mathf.Abs(_currentParallaxOffset.x - _previousParallaxOffset.x) < kParallaxDirtyThreshold &&
                Mathf.Abs(_currentParallaxOffset.y - _previousParallaxOffset.y) < kParallaxDirtyThreshold)
                return;
            _previousParallaxOffset = _currentParallaxOffset;

            ApplyTranslateOffset(_veilGlow, kVeilGlowParallax);
            ApplyTranslateOffset(_scanlines, kScanlineParallax);
            ApplyTranslateOffset(_vignette, kVignetteParallax);
            ApplyTranslateOffset(_leftPanel, kPanelParallax);
            ApplyTranslateOffset(_rightPanel, kPanelParallax);
            ApplyTranslateOffset(_carousel, kPanelParallax * 0.8f);
        }

        private void ApplyTranslateOffset(VisualElement element, float multiplier)
        {
            if (element == null) return;
            float x = _currentParallaxOffset.x * multiplier;
            float y = _currentParallaxOffset.y * multiplier;
            element.style.translate = new Translate(x, y);
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
