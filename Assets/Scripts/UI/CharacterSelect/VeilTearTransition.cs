using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Hero switch transition effect: fullscreen dark flash + animated crack
    /// VisualElements in the hero's color (~0.6s total). Follows the TitleScreenVFX
    /// pattern of using VisualElements for effects rather than particle systems.
    /// </summary>
    public class VeilTearTransition : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Timing")]
        [SerializeField] private float _flashDuration = 0.15f;
        [SerializeField] private float _crackDuration = 0.4f;
        [SerializeField] private float _totalDuration = 0.6f;

        [Header("Visuals")]
        [SerializeField] private float _flashAlpha = 0.6f;
        [SerializeField] private float _crackWidth = 6f;

        // =============================================================================
        // STATE
        // =============================================================================

        private VisualElement _overlay;
        private VisualElement _flash;
        private VisualElement _crackLeft;
        private VisualElement _crackRight;
        private bool _isDestroyed;
        private Coroutine _playCoroutine;

        // =============================================================================
        // LIFECYCLE
        // =============================================================================

        public void Initialize(VisualElement root)
        {
            _overlay = root.Q("veil-tear-overlay");
            _flash = root.Q("veil-tear-flash");
            _crackLeft = root.Q("veil-tear-crack-left");
            _crackRight = root.Q("veil-tear-crack-right");
        }

        private void OnDestroy()
        {
            _isDestroyed = true;
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Play the veil tear transition with the given hero color.
        /// </summary>
        public void Play(Color heroColor)
        {
            if (_isDestroyed || _overlay == null) return;

            if (_playCoroutine != null) StopCoroutine(_playCoroutine);
            _playCoroutine = StartCoroutine(TearSequence(heroColor));
        }

        // =============================================================================
        // ANIMATION
        // =============================================================================

        private IEnumerator TearSequence(Color color)
        {
            // Show overlay
            _overlay.RemoveFromClassList("hidden");

            // Phase 1: Flash
            if (_flash != null)
            {
                _flash.style.backgroundColor = new Color(color.r, color.g, color.b, _flashAlpha);
                _flash.style.opacity = 1;
            }

            // Crack setup - center-emanating vertical lines
            Color crackColor = new Color(color.r, color.g, color.b, 0.9f);
            SetupCrack(_crackLeft, crackColor, -_crackWidth);
            SetupCrack(_crackRight, crackColor, _crackWidth);

            // Flash in
            float elapsed = 0f;
            while (elapsed < _flashDuration)
            {
                if (_isDestroyed) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _flashDuration);

                // Flash peaks then fades
                float flashAlpha = t < 0.5f
                    ? Mathf.Lerp(0, _flashAlpha, t * 2f)
                    : Mathf.Lerp(_flashAlpha, 0, (t - 0.5f) * 2f);

                if (_flash != null) _flash.style.opacity = flashAlpha;

                yield return null;
            }

            if (_flash != null) _flash.style.opacity = 0;

            // Phase 2: Cracks spread and fade
            elapsed = 0f;
            while (elapsed < _crackDuration)
            {
                if (_isDestroyed) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _crackDuration);

                // Cracks expand outward from center then fade
                float spread = EaseOutQuad(t);
                float alpha = 1f - EaseInQuad(t);

                if (_crackLeft != null)
                {
                    _crackLeft.style.translate = new Translate(-spread * 100f, 0);
                    _crackLeft.style.opacity = alpha;
                }
                if (_crackRight != null)
                {
                    _crackRight.style.translate = new Translate(spread * 100f, 0);
                    _crackRight.style.opacity = alpha;
                }

                yield return null;
            }

            // Hide overlay
            _overlay.AddToClassList("hidden");
            ResetCracks();
        }

        // =============================================================================
        // HELPERS
        // =============================================================================

        private void SetupCrack(VisualElement crack, Color color, float initialOffset)
        {
            if (crack == null) return;

            crack.style.backgroundColor = color;
            crack.style.width = _crackWidth;
            crack.style.opacity = 1;
            crack.style.translate = new Translate(initialOffset, 0);
        }

        private void ResetCracks()
        {
            if (_crackLeft != null) { _crackLeft.style.opacity = 0; _crackLeft.style.translate = StyleKeyword.None; }
            if (_crackRight != null) { _crackRight.style.opacity = 0; _crackRight.style.translate = StyleKeyword.None; }
            if (_flash != null) _flash.style.opacity = 0;
        }

        private static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
        private static float EaseInQuad(float t) => t * t;
    }
}
