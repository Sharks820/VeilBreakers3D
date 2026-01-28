using UnityEngine;
using UnityEngine.UI;

namespace VeilBreakers.UI.Effects
{
    /// <summary>
    /// EFFECT 1: LOGO "MOLTEN CORE" PULSE
    ///
    /// Makes the logo feel like it contains heat and pressure, not a cheap glow.
    /// - Logo mostly dark
    /// - Internal cracks glow like lava
    /// - Glow surges irregularly (tied to global Pulse)
    /// - Color shifts from deep crimson → white-hot → back
    ///
    /// Hierarchy:
    /// Title_Logo_Root
    ///  ├─ Logo_Glow (behind base, scale 1.02-1.05)
    ///  └─ Logo_Base (main logo image)
    /// </summary>
    public class LogoMoltenEffect : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Logo References")]
        [SerializeField] private Image _logoBase;
        [SerializeField] private Image _logoGlow;
        [SerializeField] private SpriteRenderer _logoBaseSR;
        [SerializeField] private SpriteRenderer _logoGlowSR;

        [Header("Glow Scale")]
        [SerializeField] private float _glowMinScale = 1.02f;
        [SerializeField] private float _glowMaxScale = 1.08f;

        [Header("Intensity")]
        [SerializeField] private float _idleGlowIntensity = 0.15f;
        [SerializeField] private float _peakGlowIntensity = 1.5f;

        [Header("Colors (Crimson to White-Hot)")]
        [SerializeField] private Color _idleColor = new Color(0.4f, 0.08f, 0.08f, 0.3f);      // Deep crimson, low alpha
        [SerializeField] private Color _warmColor = new Color(0.9f, 0.25f, 0.1f, 0.7f);        // Orange-red
        [SerializeField] private Color _hotColor = new Color(1f, 0.6f, 0.2f, 0.9f);            // Yellow-orange
        [SerializeField] private Color _whiteHotColor = new Color(1f, 0.95f, 0.85f, 1f);       // Near-white

        [Header("Animation")]
        [SerializeField] private float _colorLerpSpeed = 8f;
        [SerializeField] private float _scaleLerpSpeed = 6f;

        // =============================================================================
        // PRIVATE STATE
        // =============================================================================

        private Transform _glowTransform;
        private float _currentIntensity;
        private Color _currentColor;
        private Vector3 _baseScale;
        private bool _useImage; // True for UI Image, false for SpriteRenderer

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            // Determine which rendering mode to use
            _useImage = _logoGlow != null;

            if (_useImage && _logoGlow != null)
            {
                _glowTransform = _logoGlow.transform;
                _baseScale = _glowTransform.localScale;
            }
            else if (_logoGlowSR != null)
            {
                _glowTransform = _logoGlowSR.transform;
                _baseScale = _glowTransform.localScale;
            }

            // Initialize
            _currentIntensity = _idleGlowIntensity;
            _currentColor = _idleColor;
        }

        private void OnEnable()
        {
            // Subscribe to pulse events
            if (MenuPulseController.Instance != null)
            {
                MenuPulseController.Instance.OnPulseChanged += OnPulseChanged;
                MenuPulseController.Instance.OnCrimsonRupture += OnCrimsonRupture;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe
            if (MenuPulseController.Instance != null)
            {
                MenuPulseController.Instance.OnPulseChanged -= OnPulseChanged;
                MenuPulseController.Instance.OnCrimsonRupture -= OnCrimsonRupture;
            }
        }

        private void Update()
        {
            if (_glowTransform == null) return;

            float pulse = MenuPulseController.Instance != null
                ? MenuPulseController.Instance.NormalizedPulse
                : _idleGlowIntensity;

            UpdateGlowIntensity(pulse);
            UpdateGlowColor(pulse);
            UpdateGlowScale(pulse);
        }

        // =============================================================================
        // PULSE RESPONSE
        // =============================================================================

        private void OnPulseChanged(float pulse)
        {
            // Effect is updated in Update() for smooth interpolation
        }

        private void OnCrimsonRupture()
        {
            // During rupture, we overexpose momentarily
            // This is handled by the pulse value hitting max
        }

        // =============================================================================
        // VISUAL UPDATES
        // =============================================================================

        private void UpdateGlowIntensity(float pulse)
        {
            // Map pulse [0-1] to intensity [idle-peak]
            float targetIntensity = Mathf.Lerp(_idleGlowIntensity, _peakGlowIntensity, pulse);

            // Smooth interpolation
            _currentIntensity = Mathf.Lerp(_currentIntensity, targetIntensity, Time.unscaledDeltaTime * _colorLerpSpeed);
        }

        private void UpdateGlowColor(float pulse)
        {
            // Multi-stage color ramp: idle → warm → hot → white-hot
            Color targetColor;

            if (pulse < 0.25f)
            {
                // Idle to warm
                float t = pulse / 0.25f;
                targetColor = Color.Lerp(_idleColor, _warmColor, t);
            }
            else if (pulse < 0.6f)
            {
                // Warm to hot
                float t = (pulse - 0.25f) / 0.35f;
                targetColor = Color.Lerp(_warmColor, _hotColor, t);
            }
            else
            {
                // Hot to white-hot
                float t = (pulse - 0.6f) / 0.4f;
                targetColor = Color.Lerp(_hotColor, _whiteHotColor, t);
            }

            // Apply intensity to alpha
            targetColor.a *= _currentIntensity;

            // Smooth color transition
            _currentColor = Color.Lerp(_currentColor, targetColor, Time.unscaledDeltaTime * _colorLerpSpeed);

            // Apply to renderer
            if (_useImage && _logoGlow != null)
            {
                _logoGlow.color = _currentColor;
            }
            else if (_logoGlowSR != null)
            {
                _logoGlowSR.color = _currentColor;
            }
        }

        private void UpdateGlowScale(float pulse)
        {
            // Scale glow layer based on pulse
            float targetScale = Mathf.Lerp(_glowMinScale, _glowMaxScale, pulse);

            // Smooth scale transition
            Vector3 targetScaleVec = _baseScale * targetScale;
            _glowTransform.localScale = Vector3.Lerp(
                _glowTransform.localScale,
                targetScaleVec,
                Time.unscaledDeltaTime * _scaleLerpSpeed
            );
        }

        // =============================================================================
        // PUBLIC SETUP
        // =============================================================================

        /// <summary>
        /// Setup references at runtime.
        /// </summary>
        public void Setup(Image logoBase, Image logoGlow)
        {
            _logoBase = logoBase;
            _logoGlow = logoGlow;
            _useImage = true;

            if (_logoGlow != null)
            {
                _glowTransform = _logoGlow.transform;
                _baseScale = _glowTransform.localScale;
            }
        }

        /// <summary>
        /// Setup references at runtime (SpriteRenderer version).
        /// </summary>
        public void Setup(SpriteRenderer logoBase, SpriteRenderer logoGlow)
        {
            _logoBaseSR = logoBase;
            _logoGlowSR = logoGlow;
            _useImage = false;

            if (_logoGlowSR != null)
            {
                _glowTransform = _logoGlowSR.transform;
                _baseScale = _glowTransform.localScale;
            }
        }


        /// <summary>
        /// Setup at runtime with only SpriteRenderer glow (for hybrid UI Toolkit approach).
        /// When logo base is in UI Toolkit, we only need the glow effect.
        /// </summary>
        public void Setup(SpriteRenderer logoGlow)
        {
            _logoGlowSR = logoGlow;
            _logoBaseSR = null;
            _useImage = false;

            if (_logoGlowSR != null)
            {
                _glowTransform = _logoGlowSR.transform;
                _baseScale = _glowTransform.localScale;
            }
        }

        /// <summary>
        /// Force an overexposure effect (used during Crimson Rupture).
        /// </summary>
        public void TriggerOverexposure()
        {
            _currentColor = _whiteHotColor;
            _currentIntensity = _peakGlowIntensity * 1.5f;

            if (_useImage && _logoGlow != null)
            {
                _logoGlow.color = _currentColor;
            }
            else if (_logoGlowSR != null)
            {
                _logoGlowSR.color = _currentColor;
            }
        }
    }
}
