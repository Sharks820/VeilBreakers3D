using System;
using System.Collections;
using UnityEngine;

namespace VeilBreakers.Audio
{
    /// <summary>
    /// Controls low health audio feedback including heartbeat, muffled mix, and urgent music.
    /// </summary>
    public class LowHealthAudio : MonoBehaviour
    {
        // =============================================================================
        // SINGLETON
        // =============================================================================

        private static LowHealthAudio _instance;
        public static LowHealthAudio Instance
        {
            get
            {
                if (_instance == null && !_isQuitting)
                {
                    Debug.LogError("[LowHealthAudio] Instance is null.");
                }
                return _instance;
            }
        }

        private static bool _isQuitting = false;

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Configuration")]
        [SerializeField] private AudioConfig _config;

        [Header("Thresholds")]
        [SerializeField] private float _lowHealthThreshold = 0.25f;
        [SerializeField] private float _mediumThreshold = 0.15f;
        [SerializeField] private float _criticalThreshold = 0.05f;

        [Header("Heartbeat Settings")]
        [SerializeField] private float _baseHeartbeatRate = 1f;    // BPM factor
        [SerializeField] private float _maxHeartbeatRate = 2.5f;   // BPM factor at critical
        [SerializeField] private float _heartbeatFadeTime = 0.5f;

        [Header("Audio Ducking")]
        [SerializeField] private float _maxDucking = 0.4f;         // How much to duck other audio
        [SerializeField] private float _lowPassStart = 8000f;      // Hz at threshold
        [SerializeField] private float _lowPassEnd = 2000f;        // Hz at critical

        // =============================================================================
        // STATE
        // =============================================================================

        private bool _isActive = false;
        private float _currentHealthPercent = 1f;
        private float _intensity = 0f;

        // Heartbeat state
        private bool _heartbeatPlaying = false;
        private float _currentHeartbeatRate = 1f;

        // Mix state
        private float _currentDucking = 0f;
        private float _currentLowPass = 22000f;

        // Heavy breathing
        private bool _heavyBreathingActive = false;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        public bool IsActive => _isActive;
        public float Intensity => _intensity;
        public float CurrentHealthPercent => _currentHealthPercent;

        // =============================================================================
        // EVENTS
        // =============================================================================

        public event Action OnLowHealthTriggered;
        public event Action OnLowHealthEnded;
        public event Action<float> OnIntensityChanged;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            // Load thresholds from config
            if (_config != null)
            {
                _lowHealthThreshold = _config.lowHealthThreshold;
                _mediumThreshold = _config.mediumHealthThreshold;
                _criticalThreshold = _config.criticalHealthThreshold;
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void OnDisable()
        {
            OnLowHealthTriggered = null;
            OnLowHealthEnded = null;
            OnIntensityChanged = null;

            StopAllCoroutines();
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Update with current health percentage (0-1).
        /// </summary>
        public void UpdateHealth(float healthPercent)
        {
            healthPercent = Mathf.Clamp01(healthPercent);
            _currentHealthPercent = healthPercent;

            // Check threshold
            if (healthPercent <= _lowHealthThreshold && !_isActive)
            {
                ActivateLowHealth();
            }
            else if (healthPercent > _lowHealthThreshold && _isActive)
            {
                DeactivateLowHealth();
            }

            // Update intensity if active
            if (_isActive)
            {
                UpdateIntensity();
            }
        }

        /// <summary>
        /// Force activate low health effects (for testing).
        /// </summary>
        public void ForceActivate(float intensity = 0.5f)
        {
            if (!_isActive)
            {
                ActivateLowHealth();
            }
            _intensity = Mathf.Clamp01(intensity);
            ApplyEffects();
        }

        /// <summary>
        /// Force deactivate low health effects.
        /// </summary>
        public void ForceDeactivate()
        {
            if (_isActive)
            {
                DeactivateLowHealth();
            }
        }

        // =============================================================================
        // ACTIVATION
        // =============================================================================

        private void ActivateLowHealth()
        {
            _isActive = true;

            // Start heartbeat
            StartHeartbeat();

            // Notify music system
            MusicManager.Instance?.SetParameter("LowHealth", 1f);

            OnLowHealthTriggered?.Invoke();
            Debug.Log("[LowHealthAudio] Low health audio activated");

            UpdateIntensity();
        }

        private void DeactivateLowHealth()
        {
            _isActive = false;
            _intensity = 0f;

            // Stop heartbeat
            StopHeartbeat();

            // Stop heavy breathing
            StopHeavyBreathing();

            // Reset music system
            MusicManager.Instance?.SetParameter("LowHealth", 0f);

            // Reset mix
            ResetAudioMix();

            OnLowHealthEnded?.Invoke();
            Debug.Log("[LowHealthAudio] Low health audio deactivated");
        }

        // =============================================================================
        // INTENSITY
        // =============================================================================

        private void UpdateIntensity()
        {
            float oldIntensity = _intensity;

            // Calculate intensity based on health
            if (_currentHealthPercent <= _criticalThreshold)
            {
                _intensity = 1f; // Maximum
            }
            else if (_currentHealthPercent <= _mediumThreshold)
            {
                // Medium to critical (0.7 - 1.0)
                float t = Mathf.InverseLerp(_mediumThreshold, _criticalThreshold, _currentHealthPercent);
                _intensity = Mathf.Lerp(0.7f, 1f, t);
            }
            else
            {
                // Low to medium (0 - 0.7)
                float t = Mathf.InverseLerp(_lowHealthThreshold, _mediumThreshold, _currentHealthPercent);
                _intensity = Mathf.Lerp(0f, 0.7f, t);
            }

            if (!Mathf.Approximately(oldIntensity, _intensity))
            {
                ApplyEffects();
                OnIntensityChanged?.Invoke(_intensity);
            }
        }

        private void ApplyEffects()
        {
            // Update heartbeat rate
            UpdateHeartbeatRate();

            // Update audio mix
            UpdateAudioMix();

            // Update heavy breathing
            UpdateHeavyBreathing();

            // Update music intensity
            MusicManager.Instance?.UpdatePlayerHealth(_currentHealthPercent);
        }

        // =============================================================================
        // HEARTBEAT
        // =============================================================================

        private void StartHeartbeat()
        {
            if (_heartbeatPlaying) return;

            _heartbeatPlaying = true;
            _currentHeartbeatRate = _baseHeartbeatRate;

            // FMOD Integration:
            // string heartbeatPath = _config?.heartbeatPath ?? "event:/SFX/Player/Heartbeat";
            // _heartbeatInstance = FMODUnity.RuntimeManager.CreateInstance(heartbeatPath);
            // _heartbeatInstance.start();

            Debug.Log("[LowHealthAudio] Heartbeat started");
        }

        private void StopHeartbeat()
        {
            if (!_heartbeatPlaying) return;

            _heartbeatPlaying = false;

            // FMOD Integration:
            // _heartbeatInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            // _heartbeatInstance.release();

            Debug.Log("[LowHealthAudio] Heartbeat stopped");
        }

        private void UpdateHeartbeatRate()
        {
            if (!_heartbeatPlaying) return;

            _currentHeartbeatRate = Mathf.Lerp(_baseHeartbeatRate, _maxHeartbeatRate, _intensity);

            // FMOD Integration:
            // _heartbeatInstance.setParameterByName("Rate", _currentHeartbeatRate);
            // _heartbeatInstance.setParameterByName("Intensity", _intensity);
        }

        // =============================================================================
        // HEAVY BREATHING
        // =============================================================================

        private void UpdateHeavyBreathing()
        {
            // Start heavy breathing at medium intensity
            if (_intensity >= 0.7f && !_heavyBreathingActive)
            {
                StartHeavyBreathing();
            }
            else if (_intensity < 0.6f && _heavyBreathingActive)
            {
                StopHeavyBreathing();
            }

            if (_heavyBreathingActive)
            {
                // FMOD Integration:
                // _breathingInstance.setParameterByName("Intensity", _intensity);
            }
        }

        private void StartHeavyBreathing()
        {
            if (_heavyBreathingActive) return;

            _heavyBreathingActive = true;

            // FMOD Integration:
            // _breathingInstance = FMODUnity.RuntimeManager.CreateInstance("event:/SFX/Player/HeavyBreathing");
            // _breathingInstance.start();

            Debug.Log("[LowHealthAudio] Heavy breathing started");
        }

        private void StopHeavyBreathing()
        {
            if (!_heavyBreathingActive) return;

            _heavyBreathingActive = false;

            // FMOD Integration:
            // _breathingInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            // _breathingInstance.release();

            Debug.Log("[LowHealthAudio] Heavy breathing stopped");
        }

        // =============================================================================
        // AUDIO MIX
        // =============================================================================

        private void UpdateAudioMix()
        {
            // Calculate ducking
            _currentDucking = _intensity * _maxDucking;

            // Calculate low pass filter
            _currentLowPass = Mathf.Lerp(_lowPassStart, _lowPassEnd, _intensity);

            // FMOD Integration:
            // FMODUnity.RuntimeManager.GetBus("bus:/SFX").setVolume(1f - _currentDucking);
            // FMODUnity.RuntimeManager.GetBus("bus:/Ambient").setVolume(1f - _currentDucking);
            // Set global low pass parameter or bus effect
        }

        private void ResetAudioMix()
        {
            _currentDucking = 0f;
            _currentLowPass = 22000f;

            // FMOD Integration:
            // FMODUnity.RuntimeManager.GetBus("bus:/SFX").setVolume(1f);
            // FMODUnity.RuntimeManager.GetBus("bus:/Ambient").setVolume(1f);
        }

        // =============================================================================
        // DEBUG
        // =============================================================================

        /// <summary>
        /// Get state string for debugging.
        /// </summary>
        public string GetStateString()
        {
            return $"Active: {_isActive} | HP: {_currentHealthPercent:P0} | " +
                   $"Int: {_intensity:F2} | HB Rate: {_currentHeartbeatRate:F1}x | " +
                   $"Duck: {_currentDucking:F2}";
        }
    }
}
