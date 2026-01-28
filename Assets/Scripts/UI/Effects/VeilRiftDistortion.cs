using UnityEngine;
using UnityEngine.UI;

namespace VeilBreakers.UI.Effects
{
    /// <summary>
    /// EFFECT 2: VEIL RIFT DISTORTION
    ///
    /// Makes the space behind the logo warp, as if reality is bending.
    /// - Sits behind the logo
    /// - Barely visible at rest
    /// - Expands outward during Pulse
    /// - Looks like heat haze / dimensional pressure
    /// - NOT lightning, NOT particles
    ///
    /// Implementation Notes:
    /// - Built-in RP: Uses scale + overlay for distortion illusion
    /// - URP (future): Will use Shader Graph with _CameraOpaqueTexture UV offset
    ///
    /// Distortion strength is proportional to Pulse² (exponential response).
    /// </summary>
    public class VeilRiftDistortion : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Rift Visuals")]
        [SerializeField] private Transform _riftQuad;
        [SerializeField] private SpriteRenderer _riftRenderer;
        [SerializeField] private Image _riftImage;

        [Header("Scale")]
        [SerializeField] private float _baseScale = 1.0f;
        [SerializeField] private float _maxScale = 1.08f;

        [Header("Distortion (UV Offset Simulation)")]
        [SerializeField] private float _distortionStrength = 0.02f;
        [SerializeField] private float _distortionSpeed = 1.5f;

        [Header("Alpha")]
        [SerializeField] private float _idleAlpha = 0.02f;
        [SerializeField] private float _peakAlpha = 0.15f;

        [Header("Colors")]
        [SerializeField] private Color _riftColor = new Color(0.8f, 0.2f, 0.3f, 0.1f);

        [Header("Animation")]
        [SerializeField] private float _lerpSpeed = 4f;

        // =============================================================================
        // PRIVATE STATE
        // =============================================================================

        private Vector3 _originalScale;
        private Vector3 _originalPosition;
        private float _currentAlpha;
        private float _noiseOffset;
        private Material _materialInstance;
        private bool _useImage;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            _noiseOffset = Random.Range(0f, 1000f);

            if (_riftQuad != null)
            {
                _originalScale = _riftQuad.localScale;
                _originalPosition = _riftQuad.localPosition;
            }

            _useImage = _riftImage != null;

            // Create material instance to avoid modifying shared material
            if (_riftRenderer != null && _riftRenderer.sharedMaterial != null)
            {
                _materialInstance = new Material(_riftRenderer.sharedMaterial);
                _riftRenderer.material = _materialInstance;
            }

            _currentAlpha = _idleAlpha;
        }

        private void OnEnable()
        {
            if (MenuPulseController.Instance != null)
            {
                MenuPulseController.Instance.OnPulseChanged += OnPulseChanged;
                MenuPulseController.Instance.OnCrimsonRupture += OnCrimsonRupture;
            }
        }

        private void OnDisable()
        {
            if (MenuPulseController.Instance != null)
            {
                MenuPulseController.Instance.OnPulseChanged -= OnPulseChanged;
                MenuPulseController.Instance.OnCrimsonRupture -= OnCrimsonRupture;
            }
        }

        private void Update()
        {
            if (_riftQuad == null) return;

            float pulseSquared = MenuPulseController.Instance != null
                ? MenuPulseController.Instance.GetPulseSquared()
                : 0f;

            UpdateScale(pulseSquared);
            UpdateDistortion(pulseSquared);
            UpdateAlpha(pulseSquared);
        }

        private void OnDestroy()
        {
            // Cleanup material instance
            if (_materialInstance != null)
            {
                Destroy(_materialInstance);
            }
        }

        // =============================================================================
        // PULSE RESPONSE
        // =============================================================================

        private void OnPulseChanged(float pulse)
        {
            // Handled in Update() for smooth interpolation
        }

        private void OnCrimsonRupture()
        {
            // Flash the rift during rupture
            _currentAlpha = _peakAlpha * 2f;
        }

        // =============================================================================
        // VISUAL UPDATES
        // =============================================================================

        private void UpdateScale(float pulseSquared)
        {
            // Scale expands based on pulse squared (exponential response)
            float targetScale = Mathf.Lerp(_baseScale, _maxScale, pulseSquared);
            Vector3 targetScaleVec = _originalScale * targetScale;

            _riftQuad.localScale = Vector3.Lerp(
                _riftQuad.localScale,
                targetScaleVec,
                Time.unscaledDeltaTime * _lerpSpeed
            );
        }

        private void UpdateDistortion(float pulseSquared)
        {
            // Simulated UV distortion via position jitter
            // In URP, this would be proper shader-based distortion
            float time = Time.unscaledTime * _distortionSpeed;
            float strength = _distortionStrength * pulseSquared;

            // Use Perlin noise for organic movement
            float offsetX = (Mathf.PerlinNoise(_noiseOffset, time) - 0.5f) * strength;
            float offsetY = (Mathf.PerlinNoise(time, _noiseOffset) - 0.5f) * strength;

            Vector3 jitter = new Vector3(offsetX, offsetY, 0f);
            _riftQuad.localPosition = _originalPosition + jitter;

            // If we have a material with distortion support, update it
            if (_materialInstance != null)
            {
                // For custom distortion shaders (future URP implementation)
                _materialInstance.SetFloat("_DistortionStrength", strength * 10f);
                _materialInstance.SetFloat("_DistortionTime", time);
            }
        }

        private void UpdateAlpha(float pulseSquared)
        {
            // Alpha increases with pulse squared
            float targetAlpha = Mathf.Lerp(_idleAlpha, _peakAlpha, pulseSquared);

            _currentAlpha = Mathf.Lerp(
                _currentAlpha,
                targetAlpha,
                Time.unscaledDeltaTime * _lerpSpeed
            );

            // Apply to renderer
            Color color = _riftColor;
            color.a = _currentAlpha;

            if (_useImage && _riftImage != null)
            {
                _riftImage.color = color;
            }
            else if (_riftRenderer != null)
            {
                _riftRenderer.color = color;
            }
        }

        // =============================================================================
        // PUBLIC SETUP
        // =============================================================================

        /// <summary>
        /// Setup at runtime with a SpriteRenderer.
        /// </summary>
        public void Setup(Transform riftQuad, SpriteRenderer riftRenderer)
        {
            _riftQuad = riftQuad;
            _riftRenderer = riftRenderer;
            _useImage = false;

            if (_riftQuad != null)
            {
                _originalScale = _riftQuad.localScale;
                _originalPosition = _riftQuad.localPosition;
            }

            if (_riftRenderer != null && _riftRenderer.sharedMaterial != null)
            {
                _materialInstance = new Material(_riftRenderer.sharedMaterial);
                _riftRenderer.material = _materialInstance;
            }
        }

        /// <summary>
        /// Setup at runtime with a UI Image.
        /// </summary>
        public void Setup(Transform riftQuad, Image riftImage)
        {
            _riftQuad = riftQuad;
            _riftImage = riftImage;
            _useImage = true;

            if (_riftQuad != null)
            {
                _originalScale = _riftQuad.localScale;
                _originalPosition = _riftQuad.localPosition;
            }
        }
    }
}
