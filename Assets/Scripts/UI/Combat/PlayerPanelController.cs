using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VeilBreakers.Combat;
using VeilBreakers.Data;

namespace VeilBreakers.UI.Combat
{
    /// <summary>
    /// Controls the player panel (top-left) showing HP, MP, and status effects.
    /// </summary>
    public class PlayerPanelController : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Components")]
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private Image _portraitImage;
        [SerializeField] private HealthBarController _hpBar;
        [SerializeField] private HealthBarController _mpBar;
        [SerializeField] private Transform _statusIconContainer;
        [SerializeField] private Image _panelBackground;

        [Header("Prefabs")]
        [SerializeField] private GameObject _statusIconPrefab;

        [Header("Settings")]
        [SerializeField] private int _maxStatusIcons = 8;
        [SerializeField] private Color _backgroundColor = new Color(0f, 0f, 0f, 0.6f);

        [Header("HP/MP Colors")]
        [SerializeField] private Color _hpFullColor = new Color(1f, 0f, 0f, 1f);
        [SerializeField] private Color _hpEmptyColor = new Color(0.55f, 0f, 0f, 1f);
        [SerializeField] private Color _mpFullColor = new Color(0.25f, 0.41f, 0.88f, 1f);
        [SerializeField] private Color _mpEmptyColor = new Color(0f, 0f, 0.55f, 1f);

        // =============================================================================
        // STATE
        // =============================================================================

        private Combatant _player;
        private readonly List<GameObject> _statusIcons = new List<GameObject>();
        private int _currentHP;
        private int _maxHP;
        private int _currentMP;
        private int _maxMP;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        public Combatant Player => _player;
        public float HPPercent => _maxHP > 0 ? (float)_currentHP / _maxHP : 0f;
        public float MPPercent => _maxMP > 0 ? (float)_currentMP / _maxMP : 0f;

        // =============================================================================
        // EVENTS
        // =============================================================================

        public event Action OnPlayerDeath;
        public event Action OnLowHP;
        public event Action OnLowMP;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            if (_panelBackground != null)
            {
                _panelBackground.color = _backgroundColor;
            }

            // Configure bars
            if (_hpBar != null)
            {
                _hpBar.SetColors(_hpFullColor, _hpEmptyColor, _backgroundColor);
                _hpBar.OnLowHealth += HandleLowHP;
                _hpBar.OnEmpty += HandleDeath;
            }

            if (_mpBar != null)
            {
                _mpBar.SetColors(_mpFullColor, _mpEmptyColor, _backgroundColor);
                _mpBar.OnLowHealth += HandleLowMP;
            }
        }

        private void OnDestroy()
        {
            UnbindPlayer();

            if (_hpBar != null)
            {
                _hpBar.OnLowHealth -= HandleLowHP;
                _hpBar.OnEmpty -= HandleDeath;
            }

            if (_mpBar != null)
            {
                _mpBar.OnLowHealth -= HandleLowMP;
            }
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Initialize the panel with player data.
        /// </summary>
        public void Initialize(Combatant player)
        {
            UnbindPlayer();
            _player = player;

            if (_player == null)
            {
                SetVisible(false);
                return;
            }

            // Set initial values
            UpdateName(_player.DisplayName);
            UpdateHP(_player.CurrentHp, _player.MaxHp, false);
            UpdateMP(_player.CurrentMp, _player.MaxMp, false);

            // Bind events
            _player.OnHpChanged += HandleHPChanged;
            _player.OnMpChanged += HandleMPChanged;

            // Update status effects
            RefreshStatusEffects();

            SetVisible(true);
        }

        /// <summary>
        /// Set the player portrait sprite.
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
            _currentHP = current;
            _maxHP = max;

            if (_hpBar != null)
            {
                _hpBar.SetValue(HPPercent, animate);
            }
        }

        /// <summary>
        /// Update MP display.
        /// </summary>
        public void UpdateMP(int current, int max, bool animate = true)
        {
            _currentMP = current;
            _maxMP = max;

            if (_mpBar != null)
            {
                _mpBar.SetValue(MPPercent, animate);
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
        /// Refresh status effect icons.
        /// </summary>
        public void RefreshStatusEffects()
        {
            if (_player == null || _statusIconContainer == null) return;

            // Clear existing icons
            ClearStatusIcons();

            // Get active status effects
            var effects = _player.GetStatusEffects();
            int count = Mathf.Min(effects.Count, _maxStatusIcons);

            for (int i = 0; i < count; i++)
            {
                var effect = effects[i];
                AddStatusIcon(effect.effectType, IsBuff(effect.effectType));
            }
        }

        /// <summary>
        /// Add a status icon.
        /// </summary>
        public void AddStatusIcon(StatusEffectType effectType, bool isBuff)
        {
            if (_statusIconPrefab == null || _statusIconContainer == null) return;
            if (_statusIcons.Count >= _maxStatusIcons) return;

            var iconObj = Instantiate(_statusIconPrefab, _statusIconContainer);
            _statusIcons.Add(iconObj);

            // Configure icon appearance based on effect type
            var iconImage = iconObj.GetComponent<Image>();
            if (iconImage != null)
            {
                // Color based on buff/debuff
                iconImage.color = isBuff
                    ? new Color(0.2f, 0.8f, 0.2f, 1f)  // Green for buffs
                    : new Color(0.8f, 0.2f, 0.2f, 1f); // Red for debuffs
            }
        }

        /// <summary>
        /// Clear all status icons.
        /// </summary>
        public void ClearStatusIcons()
        {
            foreach (var icon in _statusIcons)
            {
                if (icon != null)
                {
                    Destroy(icon);
                }
            }
            _statusIcons.Clear();
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

        private void HandleMPChanged(int current, int max)
        {
            UpdateMP(current, max, true);
        }

        private void HandleLowHP()
        {
            OnLowHP?.Invoke();
        }

        private void HandleLowMP()
        {
            OnLowMP?.Invoke();
        }

        private void HandleDeath()
        {
            OnPlayerDeath?.Invoke();
        }

        // =============================================================================
        // HELPERS
        // =============================================================================

        private void UnbindPlayer()
        {
            if (_player != null)
            {
                _player.OnHpChanged -= HandleHPChanged;
                _player.OnMpChanged -= HandleMPChanged;
            }
            _player = null;
        }

        private bool IsBuff(StatusEffectType effectType)
        {
            // Status effects >= 30 and < 80 are buffs
            int value = (int)effectType;
            return value >= 30 && value < 80;
        }
    }
}
