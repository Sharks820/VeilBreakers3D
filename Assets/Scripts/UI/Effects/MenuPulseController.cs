using System;
using UnityEngine;

namespace VeilBreakers.UI.Effects
{
    /// <summary>
    /// GLOBAL PULSE CONTROLLER - The brain of all title menu VFX.
    ///
    /// Pulse represents pressure from the Veil:
    /// - Pulse ≈ 0 → calm / restrained
    /// - Pulse ≈ 1 → surge / overpressure
    ///
    /// Every effect must react to Pulse, not run independently.
    /// Long calm periods, short violent surges, NO constant looping chaos.
    /// </summary>
    public class MenuPulseController : MonoBehaviour
    {
        // =============================================================================
        // SINGLETON
        // =============================================================================

        public static MenuPulseController Instance { get; private set; }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Current Pulse value [0-1]. All effects should read this.
        /// </summary>
        public float CurrentPulse { get; private set; }

        /// <summary>
        /// Normalized pulse for smooth transitions (0-1, no overshoot).
        /// </summary>
        public float NormalizedPulse => Mathf.Clamp01(CurrentPulse);

        /// <summary>
        /// Fired when Pulse value changes. Subscribe to react to pressure changes.
        /// </summary>
        public event Action<float> OnPulseChanged;

        /// <summary>
        /// Fired when a rare Crimson Rupture event triggers.
        /// </summary>
        public event Action OnCrimsonRupture;

        /// <summary>
        /// True when a pulse surge is actively happening.
        /// </summary>
        public bool IsPulsing => _isPulsing;

        // =============================================================================
        // CONFIGURATION (Per Codex spec)
        // =============================================================================

        [Header("Pulse Values")]
        [SerializeField] private float _idleIntensity = 0.15f;
        [SerializeField] private float _peakIntensity = 1.25f;

        [Header("Pulse Timing")]
        [SerializeField] private float _pulseDuration = 0.35f;
        [SerializeField] private float _minDelayBetweenPulses = 6f;
        [SerializeField] private float _maxDelayBetweenPulses = 10f;

        [Header("Idle Variation")]
        [SerializeField] private float _idleNoiseSpeed = 0.3f;
        [SerializeField] private float _idleNoiseAmplitude = 0.05f;

        [Header("Crimson Rupture (Rare Event)")]
        [SerializeField] private float _minRuptureInterval = 20f;
        [SerializeField] private float _maxRuptureInterval = 30f;
        [SerializeField] private float _ruptureDuration = 0.3f;

        // =============================================================================
        // PRIVATE STATE
        // =============================================================================

        private float _nextPulseTime;
        private float _nextRuptureTime;
        private bool _isPulsing;
        private bool _isRupturing;
        private float _pulseStartTime;
        private float _ruptureStartTime;
        private float _noiseSeed;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Initialize
            _noiseSeed = UnityEngine.Random.Range(0f, 1000f);
            ScheduleNextPulse();
            ScheduleNextRupture();
            CurrentPulse = _idleIntensity;
        }

        private void Update()
        {
            float time = Time.unscaledTime;

            // Handle rupture (overrides normal pulse)
            if (_isRupturing)
            {
                UpdateRupture(time);
                return;
            }

            // Handle normal pulse
            if (_isPulsing)
            {
                UpdatePulse(time);
            }
            else
            {
                UpdateIdle(time);

                // Check if it's time for a pulse
                if (time >= _nextPulseTime)
                {
                    StartPulse(time);
                }
            }

            // Check for rupture trigger
            if (time >= _nextRuptureTime && !_isPulsing)
            {
                TriggerCrimsonRupture();
            }

            // Notify listeners
            OnPulseChanged?.Invoke(CurrentPulse);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // =============================================================================
        // PULSE LOGIC
        // =============================================================================

        private void UpdateIdle(float time)
        {
            // Subtle Perlin noise variation around idle intensity
            // Makes the menu feel alive but restrained
            float noise = Mathf.PerlinNoise(_noiseSeed, time * _idleNoiseSpeed);
            noise = (noise - 0.5f) * 2f * _idleNoiseAmplitude;

            CurrentPulse = _idleIntensity + noise;
        }

        private void StartPulse(float time)
        {
            _isPulsing = true;
            _pulseStartTime = time;
        }

        private void UpdatePulse(float time)
        {
            float elapsed = time - _pulseStartTime;
            float t = elapsed / _pulseDuration;

            if (t >= 1f)
            {
                // Pulse complete
                _isPulsing = false;
                CurrentPulse = _idleIntensity;
                ScheduleNextPulse();
                return;
            }

            // Sharp attack, exponential decay
            // This creates the "contained pressure" feel Codex describes
            if (t < 0.15f)
            {
                // Fast attack (0 to 0.15 of duration)
                float attackT = t / 0.15f;
                CurrentPulse = Mathf.Lerp(_idleIntensity, _peakIntensity, EaseOutExpo(attackT));
            }
            else
            {
                // Slow decay (0.15 to 1.0 of duration)
                float decayT = (t - 0.15f) / 0.85f;
                CurrentPulse = Mathf.Lerp(_peakIntensity, _idleIntensity, EaseOutExpo(decayT));
            }
        }

        private void ScheduleNextPulse()
        {
            float delay = UnityEngine.Random.Range(_minDelayBetweenPulses, _maxDelayBetweenPulses);
            _nextPulseTime = Time.unscaledTime + delay;
        }

        // =============================================================================
        // CRIMSON RUPTURE (RARE EVENT)
        // =============================================================================

        private void ScheduleNextRupture()
        {
            float delay = UnityEngine.Random.Range(_minRuptureInterval, _maxRuptureInterval);
            _nextRuptureTime = Time.unscaledTime + delay;
        }

        /// <summary>
        /// Triggers the rare Crimson Rupture event.
        /// This is a "threat reminder" - makes players stop skipping the menu.
        /// </summary>
        public void TriggerCrimsonRupture()
        {
            _isRupturing = true;
            _ruptureStartTime = Time.unscaledTime;
            CurrentPulse = _peakIntensity; // Force maximum

            OnCrimsonRupture?.Invoke();

            Debug.Log("[VB:Pulse] CRIMSON RUPTURE triggered!");
        }

        private void UpdateRupture(float time)
        {
            float elapsed = time - _ruptureStartTime;
            float t = elapsed / _ruptureDuration;

            if (t >= 1f)
            {
                // Rupture complete
                _isRupturing = false;
                _isPulsing = false;
                CurrentPulse = _idleIntensity;
                ScheduleNextPulse();
                ScheduleNextRupture();
                return;
            }

            // Rupture: Hold at peak briefly, then sharp decay
            if (t < 0.3f)
            {
                // Hold at peak with slight flicker
                float flicker = 1f + Mathf.Sin(time * 50f) * 0.1f;
                CurrentPulse = _peakIntensity * flicker;
            }
            else
            {
                // Sharp decay
                float decayT = (t - 0.3f) / 0.7f;
                CurrentPulse = Mathf.Lerp(_peakIntensity, _idleIntensity, EaseOutExpo(decayT));
            }
        }

        // =============================================================================
        // PUBLIC METHODS
        // =============================================================================

        /// <summary>
        /// Force a pulse immediately. Useful for testing or events.
        /// </summary>
        public void ForcePulse()
        {
            if (!_isPulsing && !_isRupturing)
            {
                StartPulse(Time.unscaledTime);
            }
        }

        /// <summary>
        /// Get pulse value squared (for effects that need exponential response).
        /// Used by VeilRiftDistortion per Codex spec.
        /// </summary>
        public float GetPulseSquared()
        {
            float normalized = NormalizedPulse;
            return normalized * normalized;
        }

        // =============================================================================
        // EASING FUNCTIONS
        // =============================================================================

        private static float EaseOutExpo(float t)
        {
            return t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
        }
    }
}
