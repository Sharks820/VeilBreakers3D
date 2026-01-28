using UnityEngine;
using UnityEngine.UI;

namespace VeilBreakers.UI.Effects
{
    /// <summary>
    /// EFFECT 3: DEMON CHEST "BREATHING" LIGHT
    ///
    /// Makes the demon feel like the source of power for the menu.
    /// This effect is the HEARTBEAT everything else reacts to.
    ///
    /// Visual Rules:
    /// - Chest glow slowly expands/contracts
    /// - Very slow rhythm (3-5s inhale/exhale)
    /// - During Pulse spike → chest briefly overpressures
    /// - NO fast flashing
    /// - Subtle but HEAVY
    ///
    /// Implementation:
    /// - Emissive mask or separate additive sprite over chest area
    /// - Breathing rhythm is INDEPENDENT of Pulse (it's the heartbeat)
    /// - But overpressures when Pulse spikes
    /// </summary>
    public class DemonChestBreathing : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Chest Glow References")]
        [SerializeField] private Transform _chestGlowTransform;
        [SerializeField] private SpriteRenderer _chestGlowRenderer;
        [SerializeField] private Image _chestGlowImage;

        [Header("Breathing Rhythm")]
        [SerializeField] private float _breathCycleMin = 3f;
        [SerializeField] private float _breathCycleMax = 5f;

        [Header("Scale")]
        [SerializeField] private float _baseScale = 1.0f;
        [SerializeField] private float _inhaleScale = 1.08f;
        [SerializeField] private float _overpressureScale = 1.2f;

        [Header("Intensity")]
        [SerializeField] private float _baseIntensity = 0.3f;
        [SerializeField] private float _inhaleIntensity = 0.6f;
        [SerializeField] private float _overpressureIntensity = 1.2f;

        [Header("Colors")]
        [SerializeField] private Color _coreColor = new Color(1f, 0.5f, 0.2f, 0.8f);     // Orange glow
        [SerializeField] private Color _breathColor = new Color(1f, 0.7f, 0.3f, 0.9f);   // Brighter orange
        [SerializeField] private Color _overpressureColor = new Color(1f, 0.9f, 0.6f, 1f); // Near-white

        [Header("Animation")]
        [SerializeField] private float _breathLerpSpeed = 2f;
        [SerializeField] private float _overpressureLerpSpeed = 8f;

        // =============================================================================
        // PRIVATE STATE
        // =============================================================================

        private Vector3 _originalScale;
        private float _currentBreathCycle;
        private float _breathTimer;
        private float _breathPhase; // 0-1, where 0.5 is peak inhale
        private float _currentIntensity;
        private Color _currentColor;
        private bool _isOverpressuring;
        private float _overpressureTimer;
        private const float kOverpressureDuration = 0.5f;
        private bool _useImage;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            if (_chestGlowTransform != null)
            {
                _originalScale = _chestGlowTransform.localScale;
            }

            _useImage = _chestGlowImage != null;

            // Initialize breathing
            _currentBreathCycle = Random.Range(_breathCycleMin, _breathCycleMax);
            _breathTimer = Random.Range(0f, _currentBreathCycle); // Start at random phase
            _currentIntensity = _baseIntensity;
            _currentColor = _coreColor;
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
            UpdateBreathingCycle();

            if (_isOverpressuring)
            {
                UpdateOverpressure();
            }
            else
            {
                UpdateNormalBreathing();
            }

            ApplyVisuals();
        }

        // =============================================================================
        // PULSE RESPONSE
        // =============================================================================

        private void OnPulseChanged(float pulse)
        {
            // Trigger overpressure when pulse spikes above threshold
            if (pulse > 0.7f && !_isOverpressuring)
            {
                TriggerOverpressure();
            }
        }

        private void OnCrimsonRupture()
        {
            // Strong overpressure during rupture
            TriggerOverpressure();
            _overpressureTimer = kOverpressureDuration * 1.5f; // Longer during rupture
        }

        private void TriggerOverpressure()
        {
            _isOverpressuring = true;
            _overpressureTimer = kOverpressureDuration;
        }

        // =============================================================================
        // BREATHING LOGIC
        // =============================================================================

        private void UpdateBreathingCycle()
        {
            _breathTimer += Time.unscaledDeltaTime;

            // Calculate breath phase (0-1, using smoothed sine for organic feel)
            _breathPhase = (_breathTimer / _currentBreathCycle) % 1f;

            // When cycle completes, randomize next cycle duration
            if (_breathTimer >= _currentBreathCycle)
            {
                _breathTimer = 0f;
                _currentBreathCycle = Random.Range(_breathCycleMin, _breathCycleMax);
            }
        }

        private void UpdateNormalBreathing()
        {
            // Convert phase to smooth inhale/exhale curve
            // Using cosine so peak is at 0.5 phase
            float breathCurve = (1f - Mathf.Cos(_breathPhase * Mathf.PI * 2f)) * 0.5f;

            // Target scale based on breath curve
            float targetScale = Mathf.Lerp(_baseScale, _inhaleScale, breathCurve);

            // Target intensity based on breath curve
            float targetIntensity = Mathf.Lerp(_baseIntensity, _inhaleIntensity, breathCurve);

            // Target color based on breath curve
            Color targetColor = Color.Lerp(_coreColor, _breathColor, breathCurve);

            // Smooth interpolation
            float currentScale = _chestGlowTransform != null
                ? _chestGlowTransform.localScale.x / _originalScale.x
                : _baseScale;

            float newScale = Mathf.Lerp(currentScale, targetScale, Time.unscaledDeltaTime * _breathLerpSpeed);
            _currentIntensity = Mathf.Lerp(_currentIntensity, targetIntensity, Time.unscaledDeltaTime * _breathLerpSpeed);
            _currentColor = Color.Lerp(_currentColor, targetColor, Time.unscaledDeltaTime * _breathLerpSpeed);

            if (_chestGlowTransform != null)
            {
                _chestGlowTransform.localScale = _originalScale * newScale;
            }
        }

        private void UpdateOverpressure()
        {
            _overpressureTimer -= Time.unscaledDeltaTime;

            // Overpressure decay curve
            float t = 1f - (_overpressureTimer / kOverpressureDuration);
            t = Mathf.Clamp01(t);

            // Ease out for organic feel
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            // During overpressure, push toward max then decay
            if (t < 0.2f)
            {
                // Quick push to overpressure
                float pushT = t / 0.2f;
                float targetScale = Mathf.Lerp(_inhaleScale, _overpressureScale, pushT);
                float targetIntensity = Mathf.Lerp(_inhaleIntensity, _overpressureIntensity, pushT);
                Color targetColor = Color.Lerp(_breathColor, _overpressureColor, pushT);

                if (_chestGlowTransform != null)
                {
                    _chestGlowTransform.localScale = Vector3.Lerp(
                        _chestGlowTransform.localScale,
                        _originalScale * targetScale,
                        Time.unscaledDeltaTime * _overpressureLerpSpeed
                    );
                }
                _currentIntensity = Mathf.Lerp(_currentIntensity, targetIntensity, Time.unscaledDeltaTime * _overpressureLerpSpeed);
                _currentColor = Color.Lerp(_currentColor, targetColor, Time.unscaledDeltaTime * _overpressureLerpSpeed);
            }
            else
            {
                // Decay back to normal breathing
                float decayT = (t - 0.2f) / 0.8f;
                float targetScale = Mathf.Lerp(_overpressureScale, _baseScale, decayT);
                float targetIntensity = Mathf.Lerp(_overpressureIntensity, _baseIntensity, decayT);
                Color targetColor = Color.Lerp(_overpressureColor, _coreColor, decayT);

                if (_chestGlowTransform != null)
                {
                    _chestGlowTransform.localScale = Vector3.Lerp(
                        _chestGlowTransform.localScale,
                        _originalScale * targetScale,
                        Time.unscaledDeltaTime * _breathLerpSpeed
                    );
                }
                _currentIntensity = Mathf.Lerp(_currentIntensity, targetIntensity, Time.unscaledDeltaTime * _breathLerpSpeed);
                _currentColor = Color.Lerp(_currentColor, targetColor, Time.unscaledDeltaTime * _breathLerpSpeed);
            }

            // End overpressure
            if (_overpressureTimer <= 0f)
            {
                _isOverpressuring = false;
            }
        }

        private void ApplyVisuals()
        {
            // Apply color with intensity
            Color finalColor = _currentColor;
            finalColor.a *= _currentIntensity;

            if (_useImage && _chestGlowImage != null)
            {
                _chestGlowImage.color = finalColor;
            }
            else if (_chestGlowRenderer != null)
            {
                _chestGlowRenderer.color = finalColor;
            }
        }

        // =============================================================================
        // PUBLIC SETUP
        // =============================================================================

        /// <summary>
        /// Setup at runtime with SpriteRenderer.
        /// </summary>
        public void Setup(Transform glowTransform, SpriteRenderer glowRenderer)
        {
            _chestGlowTransform = glowTransform;
            _chestGlowRenderer = glowRenderer;
            _useImage = false;

            if (_chestGlowTransform != null)
            {
                _originalScale = _chestGlowTransform.localScale;
            }
        }

        /// <summary>
        /// Setup at runtime with UI Image.
        /// </summary>
        public void Setup(Transform glowTransform, Image glowImage)
        {
            _chestGlowTransform = glowTransform;
            _chestGlowImage = glowImage;
            _useImage = true;

            if (_chestGlowTransform != null)
            {
                _originalScale = _chestGlowTransform.localScale;
            }
        }
    }
}
