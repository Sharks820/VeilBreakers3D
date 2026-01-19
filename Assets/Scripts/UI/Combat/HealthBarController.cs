using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace VeilBreakers.UI.Combat
{
    /// <summary>
    /// Reusable health/MP bar controller with smooth animations.
    /// </summary>
    public class HealthBarController : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Components")]
        [SerializeField] private Image _fillImage;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _damageGhostImage; // Shows damage before fade

        [Header("Settings")]
        [SerializeField] private float _animationDuration = 0.3f;
        [SerializeField] private float _ghostFadeDelay = 0.5f;
        [SerializeField] private float _ghostFadeDuration = 0.3f;

        [Header("Colors")]
        [SerializeField] private Color _fullColor = Color.red;
        [SerializeField] private Color _emptyColor = new Color(0.55f, 0f, 0f, 1f);
        [SerializeField] private Color _backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        [SerializeField] private Color _ghostColor = new Color(1f, 0.8f, 0f, 0.6f);

        [Header("Low Health Warning")]
        [SerializeField] private bool _enableLowHealthPulse = true;
        [SerializeField] private float _lowHealthThreshold = 0.25f;
        [SerializeField] private float _pulseSpeed = 3f;

        // =============================================================================
        // STATE
        // =============================================================================

        private float _currentValue = 1f;
        private float _targetValue = 1f;
        private float _displayedValue = 1f;
        private float _ghostValue = 1f;

        private Coroutine _animationCoroutine;
        private Coroutine _ghostCoroutine;
        private bool _isPulsing = false;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        public float CurrentValue => _currentValue;
        public float DisplayedValue => _displayedValue;
        public bool IsLowHealth => _currentValue <= _lowHealthThreshold;

        // =============================================================================
        // EVENTS
        // =============================================================================

        public event Action<float> OnValueChanged;
        public event Action OnLowHealth;
        public event Action OnEmpty;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            if (_backgroundImage != null)
            {
                _backgroundImage.color = _backgroundColor;
            }
        }

        private void Update()
        {
            // Handle low health pulse
            if (_enableLowHealthPulse && IsLowHealth && _fillImage != null)
            {
                float pulse = Mathf.Sin(Time.time * _pulseSpeed) * 0.5f + 0.5f;
                _fillImage.color = Color.Lerp(_fullColor, Color.white, pulse * 0.3f);
                _isPulsing = true;
            }
            else if (_isPulsing && _fillImage != null)
            {
                UpdateFillColor();
                _isPulsing = false;
            }
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Set the bar value immediately without animation.
        /// </summary>
        public void SetValueImmediate(float value)
        {
            value = Mathf.Clamp01(value);
            _currentValue = value;
            _targetValue = value;
            _displayedValue = value;
            _ghostValue = value;

            UpdateFillAmount(value);
            UpdateGhostAmount(value);
            UpdateFillColor();
        }

        /// <summary>
        /// Set the bar value with smooth animation.
        /// </summary>
        public void SetValue(float value, bool animate = true)
        {
            value = Mathf.Clamp01(value);
            float previousValue = _currentValue;
            _currentValue = value;
            _targetValue = value;

            if (!animate)
            {
                SetValueImmediate(value);
                return;
            }

            // Start animation
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }
            _animationCoroutine = StartCoroutine(AnimateToValue(value));

            // Handle damage ghost (only on damage, not healing)
            if (value < previousValue && _damageGhostImage != null)
            {
                if (_ghostCoroutine != null)
                {
                    StopCoroutine(_ghostCoroutine);
                }
                _ghostCoroutine = StartCoroutine(AnimateGhost(previousValue, value));
            }

            // Fire events
            OnValueChanged?.Invoke(value);

            if (value <= _lowHealthThreshold && previousValue > _lowHealthThreshold)
            {
                OnLowHealth?.Invoke();
            }

            if (value <= 0f && previousValue > 0f)
            {
                OnEmpty?.Invoke();
            }
        }

        /// <summary>
        /// Set colors for the bar.
        /// </summary>
        public void SetColors(Color full, Color empty, Color background)
        {
            _fullColor = full;
            _emptyColor = empty;
            _backgroundColor = background;

            if (_backgroundImage != null)
            {
                _backgroundImage.color = _backgroundColor;
            }
            UpdateFillColor();
        }

        /// <summary>
        /// Configure the bar.
        /// </summary>
        public void Configure(float animDuration, float lowHealthThreshold, bool enablePulse)
        {
            _animationDuration = animDuration;
            _lowHealthThreshold = lowHealthThreshold;
            _enableLowHealthPulse = enablePulse;
        }

        // =============================================================================
        // ANIMATION
        // =============================================================================

        private IEnumerator AnimateToValue(float target)
        {
            float startValue = _displayedValue;
            float elapsed = 0f;

            while (elapsed < _animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _animationDuration;
                t = EaseOut(t);

                _displayedValue = Mathf.Lerp(startValue, target, t);
                UpdateFillAmount(_displayedValue);
                UpdateFillColor();

                yield return null;
            }

            _displayedValue = target;
            UpdateFillAmount(target);
            UpdateFillColor();
        }

        private IEnumerator AnimateGhost(float from, float to)
        {
            // Set ghost to previous value
            _ghostValue = from;
            UpdateGhostAmount(from);
            if (_damageGhostImage != null)
            {
                _damageGhostImage.color = _ghostColor;
            }

            // Wait before fading
            yield return new WaitForSeconds(_ghostFadeDelay);

            // Fade ghost to new value
            float elapsed = 0f;
            Color startColor = _ghostColor;
            Color endColor = new Color(_ghostColor.r, _ghostColor.g, _ghostColor.b, 0f);

            while (elapsed < _ghostFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _ghostFadeDuration;

                _ghostValue = Mathf.Lerp(from, to, t);
                UpdateGhostAmount(_ghostValue);

                if (_damageGhostImage != null)
                {
                    _damageGhostImage.color = Color.Lerp(startColor, endColor, t);
                }

                yield return null;
            }

            _ghostValue = to;
            UpdateGhostAmount(to);
        }

        // =============================================================================
        // HELPERS
        // =============================================================================

        private void UpdateFillAmount(float value)
        {
            if (_fillImage != null)
            {
                _fillImage.fillAmount = value;
            }
        }

        private void UpdateGhostAmount(float value)
        {
            if (_damageGhostImage != null)
            {
                _damageGhostImage.fillAmount = value;
            }
        }

        private void UpdateFillColor()
        {
            if (_fillImage != null && !_isPulsing)
            {
                _fillImage.color = Color.Lerp(_emptyColor, _fullColor, _displayedValue);
            }
        }

        private static float EaseOut(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }
    }
}
