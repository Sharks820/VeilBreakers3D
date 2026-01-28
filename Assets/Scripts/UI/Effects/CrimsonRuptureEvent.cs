using UnityEngine;
using UnityEngine.UI;

namespace VeilBreakers.UI.Effects
{
    /// <summary>
    /// EFFECT 6: CRIMSON RUPTURE (RARE EVENT)
    ///
    /// A "threat reminder" that makes players stop skipping the menu.
    /// - Happens once every 20-30 seconds
    /// - All other effects spike simultaneously (handled by MenuPulseController)
    /// - Brief red screen edge pulse (vignette)
    /// - Sound cue (future audio integration)
    ///
    /// This component handles the VISUAL overlay for the rupture.
    /// The timing and event triggering is in MenuPulseController.
    /// </summary>
    public class CrimsonRuptureEvent : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Vignette Overlay")]
        [SerializeField] private Image _vignetteImage;
        [SerializeField] private SpriteRenderer _vignetteRenderer;

        [Header("Colors")]
        [SerializeField] private Color _ruptureColor = new Color(0.8f, 0.1f, 0.1f, 0.4f);
        [SerializeField] private Color _fadeColor = new Color(0.8f, 0.1f, 0.1f, 0f);

        [Header("Animation")]
        [SerializeField] private float _flashDuration = 0.15f;
        [SerializeField] private float _fadeDuration = 0.4f;

        [Header("Screen Shake (Optional)")]
        [SerializeField] private bool _enableScreenShake = true;
        [SerializeField] private float _shakeIntensity = 0.03f;
        [SerializeField] private float _shakeDuration = 0.25f;

        // =============================================================================
        // PRIVATE STATE
        // =============================================================================

        private bool _isRupturing;
        private float _ruptureTimer;
        private RupturePhase _currentPhase;
        private Color _currentColor;
        private bool _useImage;
        private Vector3 _originalCameraPosition;
        private Camera _mainCamera;

        private enum RupturePhase
        {
            None,
            Flash,
            Hold,
            Fade
        }

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            _useImage = _vignetteImage != null;
            _mainCamera = Camera.main;

            // Start invisible
            SetVignetteAlpha(0f);
        }

        private void OnEnable()
        {
            if (MenuPulseController.Instance != null)
            {
                MenuPulseController.Instance.OnCrimsonRupture += OnCrimsonRupture;
            }
        }

        private void OnDisable()
        {
            if (MenuPulseController.Instance != null)
            {
                MenuPulseController.Instance.OnCrimsonRupture -= OnCrimsonRupture;
            }

            // Reset camera position if disabled mid-shake
            if (_mainCamera != null && _enableScreenShake)
            {
                _mainCamera.transform.localPosition = _originalCameraPosition;
            }
        }

        private void Update()
        {
            if (!_isRupturing) return;

            _ruptureTimer += Time.unscaledDeltaTime;
            UpdateRupturePhase();
            UpdateScreenShake();
        }

        // =============================================================================
        // RUPTURE HANDLING
        // =============================================================================

        private void OnCrimsonRupture()
        {
            StartRupture();
            Debug.Log("[VB:Rupture] CRIMSON RUPTURE visual triggered!");
        }

        private void StartRupture()
        {
            _isRupturing = true;
            _ruptureTimer = 0f;
            _currentPhase = RupturePhase.Flash;
            _currentColor = _fadeColor;

            if (_mainCamera != null)
            {
                _originalCameraPosition = _mainCamera.transform.localPosition;
            }
        }

        private void UpdateRupturePhase()
        {
            float flashEnd = _flashDuration;
            float holdEnd = flashEnd + 0.1f; // Brief hold at peak
            float fadeEnd = holdEnd + _fadeDuration;

            if (_ruptureTimer < flashEnd)
            {
                // FLASH: Quick ramp up
                _currentPhase = RupturePhase.Flash;
                float t = _ruptureTimer / flashEnd;
                t = EaseOutQuad(t);
                _currentColor = Color.Lerp(_fadeColor, _ruptureColor, t);
            }
            else if (_ruptureTimer < holdEnd)
            {
                // HOLD: Stay at peak briefly
                _currentPhase = RupturePhase.Hold;
                _currentColor = _ruptureColor;
            }
            else if (_ruptureTimer < fadeEnd)
            {
                // FADE: Slow decay
                _currentPhase = RupturePhase.Fade;
                float t = (_ruptureTimer - holdEnd) / _fadeDuration;
                t = EaseOutExpo(t);
                _currentColor = Color.Lerp(_ruptureColor, _fadeColor, t);
            }
            else
            {
                // Complete
                _currentPhase = RupturePhase.None;
                _isRupturing = false;
                _currentColor = _fadeColor;

                // Reset camera
                if (_mainCamera != null)
                {
                    _mainCamera.transform.localPosition = _originalCameraPosition;
                }
            }

            ApplyVignetteColor();
        }

        private void UpdateScreenShake()
        {
            if (!_enableScreenShake || _mainCamera == null) return;
            if (_ruptureTimer > _shakeDuration) return;

            // Shake intensity decreases over time
            float shakeProgress = _ruptureTimer / _shakeDuration;
            float intensity = _shakeIntensity * (1f - shakeProgress);

            // Random offset
            float offsetX = (Random.value - 0.5f) * 2f * intensity;
            float offsetY = (Random.value - 0.5f) * 2f * intensity;

            _mainCamera.transform.localPosition = _originalCameraPosition + new Vector3(offsetX, offsetY, 0f);
        }

        // =============================================================================
        // VISUAL APPLICATION
        // =============================================================================

        private void ApplyVignetteColor()
        {
            if (_useImage && _vignetteImage != null)
            {
                _vignetteImage.color = _currentColor;
            }
            else if (_vignetteRenderer != null)
            {
                _vignetteRenderer.color = _currentColor;
            }
        }

        private void SetVignetteAlpha(float alpha)
        {
            Color color = _ruptureColor;
            color.a = alpha;

            if (_useImage && _vignetteImage != null)
            {
                _vignetteImage.color = color;
            }
            else if (_vignetteRenderer != null)
            {
                _vignetteRenderer.color = color;
            }
        }

        // =============================================================================
        // EASING FUNCTIONS
        // =============================================================================

        private static float EaseOutQuad(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }

        private static float EaseOutExpo(float t)
        {
            return t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
        }

        // =============================================================================
        // PUBLIC SETUP
        // =============================================================================

        /// <summary>
        /// Setup at runtime with UI Image vignette.
        /// </summary>
        public void Setup(Image vignetteImage)
        {
            _vignetteImage = vignetteImage;
            _useImage = true;
            SetVignetteAlpha(0f);
        }

        /// <summary>
        /// Setup at runtime with SpriteRenderer vignette.
        /// </summary>
        public void Setup(SpriteRenderer vignetteRenderer)
        {
            _vignetteRenderer = vignetteRenderer;
            _useImage = false;
            SetVignetteAlpha(0f);
        }

        /// <summary>
        /// Manually trigger a rupture (for testing or special events).
        /// </summary>
        public void TriggerRupture()
        {
            StartRupture();
        }
    }
}
