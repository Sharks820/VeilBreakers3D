using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VeilBreakers.Combat;
using VeilBreakers.Data;

namespace VeilBreakers.UI.Combat
{
    /// <summary>
    /// Controls a single ally panel on the right side of the screen.
    /// </summary>
    public class AllyPanelController : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Components")]
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private Image _portraitImage;
        [SerializeField] private Image _portraitBorder;
        [SerializeField] private HealthBarController _hpBar;
        [SerializeField] private Transform _statusIconContainer;
        [SerializeField] private Transform _skillIconContainer;
        [SerializeField] private Image _panelBackground;

        [Header("Prefabs")]
        [SerializeField] private GameObject _statusIconPrefab;
        [SerializeField] private GameObject _skillIconPrefab;

        [Header("Settings")]
        [SerializeField] private int _allyIndex;
        [SerializeField] private int _maxStatusIcons = 4;
        [SerializeField] private int _skillCount = 4; // Skills 1-3 + Ultimate
        [SerializeField] private Color _backgroundColor = new Color(0f, 0f, 0f, 0.5f);

        [Header("Ultimate Glow")]
        [SerializeField] private Color _ultimateReadyColor = new Color(1f, 0.84f, 0f, 1f);
        [SerializeField] private Color _normalBorderColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        [SerializeField] private float _glowPulseDuration = 2f;

        // =============================================================================
        // STATE
        // =============================================================================

        private Combatant _ally;
        private readonly System.Collections.Generic.List<GameObject> _statusIcons =
            new System.Collections.Generic.List<GameObject>();
        private readonly System.Collections.Generic.List<SkillSlotController> _skillSlots =
            new System.Collections.Generic.List<SkillSlotController>();
        private bool _isUltimateReady = false;
        private Coroutine _glowCoroutine;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        public int AllyIndex => _allyIndex;
        public Combatant Ally => _ally;
        public bool IsUltimateReady => _isUltimateReady;

        // =============================================================================
        // EVENTS
        // =============================================================================

        public event Action<int> OnUltimateTriggered; // AllyIndex
        public event Action<int> OnAllyDeath;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            if (_panelBackground != null)
            {
                _panelBackground.color = _backgroundColor;
            }

            if (_portraitBorder != null)
            {
                _portraitBorder.color = _normalBorderColor;
            }
        }

        private void Update()
        {
            // F1-F3 triggers ally ultimates
            KeyCode ultimateKey = GetUltimateKey();
            if (Input.GetKeyDown(ultimateKey) && _isUltimateReady)
            {
                TriggerUltimate();
            }
        }

        private void OnDestroy()
        {
            UnbindAlly();
            if (_glowCoroutine != null)
            {
                StopCoroutine(_glowCoroutine);
            }
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Initialize with ally data.
        /// </summary>
        public void Initialize(Combatant ally, int index)
        {
            UnbindAlly();

            _allyIndex = index;
            _ally = ally;

            if (_ally == null)
            {
                SetVisible(false);
                return;
            }

            // Set initial values
            UpdateName(_ally.DisplayName);
            UpdateHP(_ally.CurrentHp, _ally.MaxHp, false);

            // Bind events
            _ally.OnHpChanged += HandleHPChanged;
            _ally.OnDeath += HandleDeath;

            // Initialize skill slots
            InitializeSkillSlots();

            // Update status effects
            RefreshStatusEffects();

            SetVisible(true);
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
            ClearStatusIcons();

            if (_ally == null || _statusIconContainer == null) return;

            var effects = _ally.GetStatusEffects();
            int count = Mathf.Min(effects.Count, _maxStatusIcons);

            for (int i = 0; i < count; i++)
            {
                AddStatusIcon(effects[i].effectType);
            }
        }

        /// <summary>
        /// Update skill cooldown display.
        /// </summary>
        public void UpdateSkillCooldown(int skillIndex, float remaining, float total)
        {
            if (skillIndex >= 0 && skillIndex < _skillSlots.Count)
            {
                _skillSlots[skillIndex].SetCooldown(remaining, total);
            }
        }

        /// <summary>
        /// Set ultimate ready state.
        /// </summary>
        public void SetUltimateReady(bool ready)
        {
            if (_isUltimateReady == ready) return;

            _isUltimateReady = ready;

            if (ready)
            {
                StartUltimateGlow();
            }
            else
            {
                StopUltimateGlow();
            }

            // Update ultimate skill slot
            if (_skillSlots.Count > 0)
            {
                var ultimateSlot = _skillSlots[_skillSlots.Count - 1];
                if (ready)
                {
                    ultimateSlot.StartReadyGlow();
                }
                else
                {
                    ultimateSlot.StopReadyGlow();
                }
            }
        }

        /// <summary>
        /// Trigger the ally's ultimate.
        /// </summary>
        public void TriggerUltimate()
        {
            if (!_isUltimateReady) return;

            OnUltimateTriggered?.Invoke(_allyIndex);

            // The combat system will handle actual ultimate execution
            // and call SetUltimateReady(false) + set cooldown
        }

        /// <summary>
        /// Show or hide the panel.
        /// </summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        // =============================================================================
        // SKILL SLOTS
        // =============================================================================

        private void InitializeSkillSlots()
        {
            ClearSkillSlots();

            if (_skillIconContainer == null || _skillIconPrefab == null) return;

            // Skills: 1, 2, 3, Ultimate (4 total for allies)
            // Allies don't show Basic Attack or Defend (always available)
            for (int i = 0; i < _skillCount; i++)
            {
                var slotObj = Instantiate(_skillIconPrefab, _skillIconContainer);
                var slot = slotObj.GetComponent<SkillSlotController>();

                if (slot != null)
                {
                    bool isUltimate = (i == _skillCount - 1);
                    string display = isUltimate ? "\u2605" : (i + 1).ToString(); // Star for ultimate

                    slot.Initialize(i, KeyCode.None, display, isUltimate);
                    _skillSlots.Add(slot);
                }
            }
        }

        private void ClearSkillSlots()
        {
            foreach (var slot in _skillSlots)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }
            _skillSlots.Clear();
        }

        // =============================================================================
        // STATUS ICONS
        // =============================================================================

        private void AddStatusIcon(StatusEffectType effectType)
        {
            if (_statusIconPrefab == null || _statusIconContainer == null) return;

            var iconObj = Instantiate(_statusIconPrefab, _statusIconContainer);
            _statusIcons.Add(iconObj);

            var iconImage = iconObj.GetComponent<Image>();
            if (iconImage != null)
            {
                bool isBuff = IsBuff(effectType);
                iconImage.color = isBuff
                    ? new Color(0.2f, 0.8f, 0.2f, 1f)
                    : new Color(0.8f, 0.2f, 0.2f, 1f);
            }
        }

        private void ClearStatusIcons()
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

        // =============================================================================
        // ULTIMATE GLOW
        // =============================================================================

        private void StartUltimateGlow()
        {
            if (_glowCoroutine != null)
            {
                StopCoroutine(_glowCoroutine);
            }
            _glowCoroutine = StartCoroutine(UltimateGlowCoroutine());
        }

        private void StopUltimateGlow()
        {
            if (_glowCoroutine != null)
            {
                StopCoroutine(_glowCoroutine);
                _glowCoroutine = null;
            }

            if (_portraitBorder != null)
            {
                _portraitBorder.color = _normalBorderColor;
            }
        }

        private IEnumerator UltimateGlowCoroutine()
        {
            while (_isUltimateReady)
            {
                float t = (Mathf.Sin(Time.time * Mathf.PI * 2f / _glowPulseDuration) + 1f) / 2f;
                Color glowColor = Color.Lerp(_normalBorderColor, _ultimateReadyColor, t);

                if (_portraitBorder != null)
                {
                    _portraitBorder.color = glowColor;
                }

                yield return null;
            }
        }

        // =============================================================================
        // EVENT HANDLERS
        // =============================================================================

        private void HandleHPChanged(int current, int max)
        {
            UpdateHP(current, max, true);
        }

        private void HandleDeath()
        {
            OnAllyDeath?.Invoke(_allyIndex);
        }

        // =============================================================================
        // HELPERS
        // =============================================================================

        private void UnbindAlly()
        {
            if (_ally != null)
            {
                _ally.OnHpChanged -= HandleHPChanged;
                _ally.OnDeath -= HandleDeath;
            }
            _ally = null;
        }

        private bool IsBuff(StatusEffectType effectType)
        {
            int value = (int)effectType;
            return value >= 30 && value < 80;
        }

        private KeyCode GetUltimateKey()
        {
            return _allyIndex switch
            {
                0 => KeyCode.F1,
                1 => KeyCode.F2,
                2 => KeyCode.F3,
                _ => KeyCode.None
            };
        }
    }
}
