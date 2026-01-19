using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VeilBreakers.Combat;
using VeilBreakers.Capture;

namespace VeilBreakers.UI.Combat
{
    /// <summary>
    /// Controls the enemy target panel (top-center) showing name, HP, corruption.
    /// </summary>
    public class EnemyPanelController : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Components")]
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private Image _portraitImage;
        [SerializeField] private HealthBarController _hpBar;
        [SerializeField] private TextMeshProUGUI _corruptionText;
        [SerializeField] private Image _panelBackground;
        [SerializeField] private Image _captureMarkerIcon;

        [Header("Settings")]
        [SerializeField] private Color _backgroundColor = new Color(0f, 0f, 0f, 0.6f);

        [Header("HP Colors")]
        [SerializeField] private Color _hpFullColor = new Color(0.8f, 0f, 0f, 1f);
        [SerializeField] private Color _hpEmptyColor = new Color(0.4f, 0f, 0f, 1f);

        [Header("Corruption Colors")]
        [SerializeField] private Color _corruptionLow = new Color(0.30f, 0.69f, 0.31f, 1f);
        [SerializeField] private Color _corruptionMid = new Color(1f, 0.92f, 0.23f, 1f);
        [SerializeField] private Color _corruptionHigh = new Color(0.96f, 0.26f, 0.21f, 1f);

        [Header("Animation")]
        [SerializeField] private float _popupDuration = 0.15f;

        // =============================================================================
        // STATE
        // =============================================================================

        private Combatant _currentTarget;
        private float _corruption;
        private bool _isMarkedForCapture;
        private Vector3 _baseScale;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        public Combatant CurrentTarget => _currentTarget;
        public bool HasTarget => _currentTarget != null;
        public bool IsMarkedForCapture => _isMarkedForCapture;

        // =============================================================================
        // EVENTS
        // =============================================================================

        public event Action<Combatant> OnTargetChanged;
        public event Action OnTargetDefeated;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            _baseScale = transform.localScale;

            if (_panelBackground != null)
            {
                _panelBackground.color = _backgroundColor;
            }

            if (_hpBar != null)
            {
                _hpBar.SetColors(_hpFullColor, _hpEmptyColor, _backgroundColor);
            }

            if (_captureMarkerIcon != null)
            {
                _captureMarkerIcon.gameObject.SetActive(false);
            }

            SetVisible(false);
        }

        private void OnDestroy()
        {
            UnbindTarget();
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Set the current target enemy.
        /// </summary>
        public void SetTarget(Combatant target)
        {
            if (_currentTarget == target) return;

            UnbindTarget();
            _currentTarget = target;

            if (_currentTarget == null)
            {
                SetVisible(false);
                return;
            }

            // Set initial values
            UpdateName(_currentTarget.DisplayName);
            UpdateHP(_currentTarget.CurrentHp, _currentTarget.MaxHp, false);
            UpdateCorruption(_currentTarget.Corruption);

            // Check capture mark
            if (CaptureManager.Instance != null)
            {
                SetMarkedForCapture(CaptureManager.Instance.IsMarkedForCapture(_currentTarget));
            }

            // Bind events
            _currentTarget.OnHpChanged += HandleHPChanged;
            _currentTarget.OnDeath += HandleTargetDeath;

            SetVisible(true);
            PlayPopupAnimation();

            OnTargetChanged?.Invoke(_currentTarget);
        }

        /// <summary>
        /// Clear the current target.
        /// </summary>
        public void ClearTarget()
        {
            UnbindTarget();
            _currentTarget = null;
            SetVisible(false);
        }

        /// <summary>
        /// Set the portrait sprite.
        /// </summary>
        public void SetPortrait(Sprite portrait)
        {
            if (_portraitImage != null)
            {
                _portraitImage.sprite = portrait;
            }
        }

        /// <summary>
        /// Update HP display.
        /// </summary>
        public void UpdateHP(int current, int max, bool animate = true)
        {
            if (_hpBar != null)
            {
                float percent = max > 0 ? (float)current / max : 0f;
                _hpBar.SetValue(percent, animate);
            }
        }

        /// <summary>
        /// Update corruption display.
        /// </summary>
        public void UpdateCorruption(float corruption)
        {
            _corruption = corruption;

            if (_corruptionText != null)
            {
                _corruptionText.text = $"Corruption: {corruption:F0}%";
                _corruptionText.color = GetCorruptionColor(corruption);
            }
        }

        /// <summary>
        /// Update name display.
        /// </summary>
        public void UpdateName(string name)
        {
            if (_nameLabel != null)
            {
                _nameLabel.text = name;
            }
        }

        /// <summary>
        /// Set capture mark state.
        /// </summary>
        public void SetMarkedForCapture(bool marked)
        {
            _isMarkedForCapture = marked;

            if (_captureMarkerIcon != null)
            {
                _captureMarkerIcon.gameObject.SetActive(marked);
            }
        }

        /// <summary>
        /// Show or hide the panel.
        /// </summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        // =============================================================================
        // EVENT HANDLERS
        // =============================================================================

        private void HandleHPChanged(int current, int max)
        {
            UpdateHP(current, max, true);
        }

        private void HandleTargetDeath()
        {
            OnTargetDefeated?.Invoke();
            // Don't clear immediately - let combat system handle target cycling
        }

        // =============================================================================
        // HELPERS
        // =============================================================================

        private void UnbindTarget()
        {
            if (_currentTarget != null)
            {
                _currentTarget.OnHpChanged -= HandleHPChanged;
                _currentTarget.OnDeath -= HandleTargetDeath;
            }
        }

        private Color GetCorruptionColor(float corruption)
        {
            if (corruption <= 25f) return _corruptionLow;
            if (corruption <= 50f) return Color.Lerp(_corruptionLow, _corruptionMid, (corruption - 25f) / 25f);
            if (corruption <= 75f) return Color.Lerp(_corruptionMid, _corruptionHigh, (corruption - 50f) / 25f);
            return _corruptionHigh;
        }

        private void PlayPopupAnimation()
        {
            // Simple scale popup animation
            StartCoroutine(PopupCoroutine());
        }

        private System.Collections.IEnumerator PopupCoroutine()
        {
            float elapsed = 0f;
            Vector3 startScale = _baseScale * 0.8f;

            transform.localScale = startScale;

            while (elapsed < _popupDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _popupDuration;
                t = 1f - (1f - t) * (1f - t); // Ease out

                transform.localScale = Vector3.Lerp(startScale, _baseScale, t);
                yield return null;
            }

            transform.localScale = _baseScale;
        }
    }
}
