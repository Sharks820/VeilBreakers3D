using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Data;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Per-hero colored background environment controller.
    /// Currently a placeholder for future 3D environments - applies hero color
    /// as a tinted overlay with crossfade transitions on hero switch.
    /// Supports neutral base toggle for inspecting models without color tint.
    /// </summary>
    public class EnvironmentController : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Crossfade")]
        [SerializeField] private float _crossfadeDuration = 0.4f;
        [SerializeField] private float _colorOverlayAlpha = 0.15f;

        // =============================================================================
        // STATE
        // =============================================================================

        private VisualElement _envLayerBase;
        private VisualElement _envLayerColor;
        private VisualElement _envVignette;
        private bool _neutralMode;
        private bool _isDestroyed;
        private Coroutine _crossfadeCoroutine;
        private Color _currentColor = Color.clear;   // Target/intended color
        private Color _displayedColor = Color.clear;  // Actual on-screen color (tracks mid-crossfade)

        // =============================================================================
        // LIFECYCLE
        // =============================================================================

        public void Initialize(VisualElement root)
        {
            _envLayerBase = root.Q("env-layer-base");
            _envLayerColor = root.Q("env-layer-color");
            _envVignette = root.Q("env-vignette");
        }

        private void OnDestroy()
        {
            _isDestroyed = true;
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Crossfade to the given hero's environment color.
        /// </summary>
        public void SetHeroEnvironment(HeroData hero)
        {
            if (_isDestroyed || hero == null) return;

            Color target = hero.color_palette != null ? hero.color_palette.ToColor() : Color.white;

            if (_crossfadeCoroutine != null) StopCoroutine(_crossfadeCoroutine);

            if (_neutralMode)
            {
                // Store the color but don't apply
                _currentColor = target;
                return;
            }

            _currentColor = target;
            _crossfadeCoroutine = StartCoroutine(CrossfadeColor(_displayedColor, target));
        }

        /// <summary>
        /// Toggle neutral (untinted) background mode.
        /// </summary>
        public void ToggleNeutral()
        {
            _neutralMode = !_neutralMode;

            if (_crossfadeCoroutine != null) StopCoroutine(_crossfadeCoroutine);

            if (_neutralMode)
            {
                // Fade to clear
                _crossfadeCoroutine = StartCoroutine(CrossfadeColor(_displayedColor, Color.clear));
            }
            else
            {
                // Restore hero color
                _crossfadeCoroutine = StartCoroutine(CrossfadeColor(_displayedColor, _currentColor));
            }
        }

        public bool IsNeutralMode => _neutralMode;

        // =============================================================================
        // CROSSFADE
        // =============================================================================

        private IEnumerator CrossfadeColor(Color from, Color to)
        {
            float elapsed = 0f;

            while (elapsed < _crossfadeDuration)
            {
                if (_isDestroyed) yield break;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _crossfadeDuration);
                // Ease out quad
                float eased = 1f - (1f - t) * (1f - t);

                _displayedColor = Color.Lerp(from, to, eased);
                ApplyColor(_displayedColor);

                yield return null;
            }

            _displayedColor = to;
            ApplyColor(to);
        }

        private void ApplyColor(Color color)
        {
            if (_envLayerColor == null) return;

            _envLayerColor.style.backgroundColor = new Color(
                color.r, color.g, color.b, _colorOverlayAlpha);
        }
    }
}
