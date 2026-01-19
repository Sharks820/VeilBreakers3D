using System;
using UnityEngine;
using VeilBreakers.Core;

namespace VeilBreakers.Commands
{
    /// <summary>
    /// Controls time slow effect when quick command menu is open.
    /// Time slows to 25% speed during tactical command selection.
    /// </summary>
    public class TimeSlowController : MonoBehaviour
    {
        // =============================================================================
        // SINGLETON
        // =============================================================================

        private static TimeSlowController _instance;
        public static TimeSlowController Instance
        {
            get
            {
                if (_instance == null && !_isQuitting)
                {
                    Debug.LogError("[TimeSlowController] Instance is null. Ensure TimeSlowController exists in scene.");
                }
                return _instance;
            }
        }

        private static bool _isQuitting = false;

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Time Slow Settings")]
        [Tooltip("Time scale when tactical menu is open (0.25 = 25% speed)")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _slowTimeScale = 0.25f;

        [Tooltip("Duration to transition to slow time")]
        [SerializeField] private float _transitionDuration = 0.15f;

        [Tooltip("Should audio pitch also slow down?")]
        [SerializeField] private bool _slowAudioPitch = true;

        [Header("Visual Feedback")]
        [Tooltip("Vignette intensity during time slow")]
        [Range(0f, 1f)]
        [SerializeField] private float _vignetteIntensity = 0.3f;

        [Tooltip("Desaturation amount during time slow")]
        [Range(0f, 1f)]
        [SerializeField] private float _desaturation = 0.2f;

        // =============================================================================
        // STATE
        // =============================================================================

        private float _targetTimeScale = 1f;
        private float _currentTransition = 1f;
        private bool _isSlowed = false;
        private int _slowRequestCount = 0;
        private float _originalFixedDeltaTime;

        // Events
        public event Action OnTimeSlowStarted;
        public event Action OnTimeSlowEnded;
        public event Action<float> OnTimeScaleChanged;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        public bool IsSlowed => _isSlowed;
        public float CurrentTimeScale => Time.timeScale;
        public float SlowTimeScale => _slowTimeScale;
        public float VignetteIntensity => _isSlowed ? _vignetteIntensity : 0f;
        public float Desaturation => _isSlowed ? _desaturation : 0f;

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

            // Cache original fixed delta time to avoid hardcoding
            _originalFixedDeltaTime = Time.fixedDeltaTime;
        }

        private void OnDestroy()
        {
            // Ensure time scale and fixed delta time are reset
            Time.timeScale = 1f;
            if (_originalFixedDeltaTime > 0)
            {
                Time.fixedDeltaTime = _originalFixedDeltaTime;
            }

            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        private void Update()
        {
            // Smoothly transition time scale
            if (_transitionDuration > 0 && Mathf.Abs(Time.timeScale - _targetTimeScale) > 0.01f)
            {
                float delta = Time.unscaledDeltaTime / _transitionDuration;
                Time.timeScale = Mathf.MoveTowards(Time.timeScale, _targetTimeScale, delta);
                OnTimeScaleChanged?.Invoke(Time.timeScale);

                // Adjust fixed delta time to maintain physics consistency
                Time.fixedDeltaTime = _originalFixedDeltaTime * Time.timeScale;

                // Adjust audio pitch if enabled
                if (_slowAudioPitch)
                {
                    AudioListener.volume = Mathf.Lerp(AudioListener.volume, _isSlowed ? 0.7f : 1f, delta);
                }
            }
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Request time slow (can be called multiple times - uses reference counting).
        /// </summary>
        public void RequestTimeSlow()
        {
            _slowRequestCount++;

            if (!_isSlowed)
            {
                _isSlowed = true;
                _targetTimeScale = _slowTimeScale;

                OnTimeSlowStarted?.Invoke();

                Debug.Log($"[TimeSlowController] Time slow started (scale: {_slowTimeScale})");
            }
        }

        /// <summary>
        /// Release time slow request (time resumes when all requests released).
        /// </summary>
        public void ReleaseTimeSlow()
        {
            _slowRequestCount = Mathf.Max(0, _slowRequestCount - 1);

            if (_slowRequestCount == 0 && _isSlowed)
            {
                _isSlowed = false;
                _targetTimeScale = 1f;

                OnTimeSlowEnded?.Invoke();

                Debug.Log("[TimeSlowController] Time slow ended");
            }
        }

        /// <summary>
        /// Force release all time slow requests.
        /// </summary>
        public void ForceResumeTime()
        {
            _slowRequestCount = 0;
            _isSlowed = false;
            _targetTimeScale = 1f;

            OnTimeSlowEnded?.Invoke();

            Debug.Log("[TimeSlowController] Time slow force released");
        }

        /// <summary>
        /// Set time slow scale (for settings adjustment).
        /// </summary>
        public void SetSlowTimeScale(float scale)
        {
            _slowTimeScale = Mathf.Clamp(scale, 0.1f, 1f);

            // Update target if currently slowed
            if (_isSlowed)
            {
                _targetTimeScale = _slowTimeScale;
            }
        }

        /// <summary>
        /// Pause game completely (time scale 0).
        /// </summary>
        public void PauseGame()
        {
            _targetTimeScale = 0f;
            Time.timeScale = 0f;
            OnTimeScaleChanged?.Invoke(0f);
        }

        /// <summary>
        /// Resume from pause.
        /// </summary>
        public void UnpauseGame()
        {
            _targetTimeScale = _isSlowed ? _slowTimeScale : 1f;
            Time.timeScale = _targetTimeScale;
            OnTimeScaleChanged?.Invoke(_targetTimeScale);
        }

        // =============================================================================
        // UTILITY
        // =============================================================================

        /// <summary>
        /// Get time delta that ignores time scale (for UI updates during slow-mo).
        /// </summary>
        public static float GetUnscaledDeltaTime()
        {
            return Time.unscaledDeltaTime;
        }

        /// <summary>
        /// Converts game time to real time at current time scale.
        /// </summary>
        public float GameTimeToRealTime(float gameTime)
        {
            if (Time.timeScale <= 0) return float.MaxValue;
            return gameTime / Time.timeScale;
        }
    }
}
