using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VeilBreakers.Combat;
using VeilBreakers.Core;
using VeilBreakers.Data;

namespace VeilBreakers.UI.Combat
{
    /// <summary>
    /// Controls a single ally panel on the right side of the screen.
    /// </summary>
    public class AllyPanelController : MonoBehaviour
    {
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
        [SerializeField] private Color _selectedBackgroundColor = new Color(0.12f, 0.10f, 0.04f, 0.78f);

        [Header("Ultimate Glow")]
        [SerializeField] private Color _ultimateReadyColor = new Color(1f, 0.84f, 0f, 1f);
        [SerializeField] private Color _normalBorderColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color _selectedBorderColor = new Color(1f, 0.60f, 0.20f, 1f);
        [SerializeField] private float _glowPulseDuration = 2f;

        private Combatant _ally;
        private readonly List<GameObject> _statusIcons = new List<GameObject>();
        private readonly List<SkillSlotController> _skillSlots = new List<SkillSlotController>();
        private bool _isUltimateReady;
        private bool _isSelected;
        private Coroutine _glowCoroutine;

        public int AllyIndex => _allyIndex;
        public Combatant Ally => _ally;
        public bool IsUltimateReady => _isUltimateReady;
        public bool IsSelected => _isSelected;

        public event Action<int> OnUltimateTriggered; // AllyIndex
        public event Action<int> OnAllyDeath;

        private void Awake()
        {
            ApplyVisualState();
        }

        private void Update()
        {
            if (!InputManager.HasInstance || !_isUltimateReady) return;

            InputManager.GameAction action = _allyIndex switch
            {
                0 => InputManager.GameAction.Ally1,
                1 => InputManager.GameAction.Ally2,
                2 => InputManager.GameAction.Ally3,
                _ => (InputManager.GameAction)(-1)
            };

            if (action != (InputManager.GameAction)(-1) && InputManager.Instance.GetActionDown(action))
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
                _glowCoroutine = null;
            }
        }

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

            UpdateName(_ally.DisplayName);
            UpdateHP(_ally.CurrentHp, _ally.MaxHp, false);

            _ally.OnHpChanged += HandleHPChanged;
            _ally.OnDeath += HandleDeath;

            InitializeSkillSlots();
            RefreshStatusEffects();

            SetVisible(true);
            ApplyVisualState();
        }

        public void SetPortrait(Sprite portrait)
        {
            if (_portraitImage != null)
            {
                _portraitImage.sprite = portrait;
            }
        }

        public void UpdateHP(int current, int max, bool animate = true)
        {
            if (_hpBar != null)
            {
                float percent = max > 0 ? (float)current / max : 0f;
                _hpBar.SetValue(percent, animate);
            }
        }

        public void UpdateName(string name)
        {
            if (_nameLabel != null)
            {
                _nameLabel.text = name;
            }
        }

        public void RefreshStatusEffects()
        {
            ClearStatusIcons();

            if (_ally == null || _statusIconContainer == null) return;

            var effects = _ally.GetStatusEffects();
            int count = Mathf.Min(effects.Count, _maxStatusIcons);

            for (int i = 0; i < count; i++)
            {
                if (effects[i] == null) continue;
                AddStatusIcon(effects[i].EffectType);
            }
        }

        public void UpdateSkillCooldown(int skillIndex, float remaining, float total)
        {
            if (skillIndex >= 0 && skillIndex < _skillSlots.Count)
            {
                _skillSlots[skillIndex].SetCooldown(remaining, total);
            }
        }

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

        public void TriggerUltimate()
        {
            if (!_isUltimateReady) return;
            OnUltimateTriggered?.Invoke(_allyIndex);
        }

        public void SetSelected(bool selected)
        {
            if (_isSelected == selected) return;
            _isSelected = selected;
            ApplyVisualState();
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        private void InitializeSkillSlots()
        {
            ClearSkillSlots();

            if (_skillIconContainer == null || _skillIconPrefab == null) return;

            for (int i = 0; i < _skillCount; i++)
            {
                var slotObj = Instantiate(_skillIconPrefab, _skillIconContainer);
                if (slotObj == null) continue;

                var slot = slotObj.GetComponent<SkillSlotController>();
                if (slot == null) continue;

                bool isUltimate = i == _skillCount - 1;
                slot.Initialize(i, isUltimate);
                _skillSlots.Add(slot);
            }
        }

        private void ClearSkillSlots()
        {
            for (int i = 0; i < _skillSlots.Count; i++)
            {
                var slot = _skillSlots[i];
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }
            _skillSlots.Clear();
        }

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
            for (int i = 0; i < _statusIcons.Count; i++)
            {
                var icon = _statusIcons[i];
                if (icon != null)
                {
                    Destroy(icon);
                }
            }
            _statusIcons.Clear();
        }

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

            ApplyVisualState();
        }

        private IEnumerator UltimateGlowCoroutine()
        {
            while (_isUltimateReady)
            {
                float t = (Mathf.Sin(Time.time * Mathf.PI * 2f / _glowPulseDuration) + 1f) * 0.5f;
                Color baseBorder = _isSelected ? _selectedBorderColor : _normalBorderColor;
                Color glowColor = Color.Lerp(baseBorder, _ultimateReadyColor, t);

                if (_portraitBorder != null)
                {
                    _portraitBorder.color = glowColor;
                }

                yield return null;
            }
        }

        private void ApplyVisualState()
        {
            if (_panelBackground != null)
            {
                _panelBackground.color = _isSelected ? _selectedBackgroundColor : _backgroundColor;
            }

            if (_portraitBorder != null && !_isUltimateReady)
            {
                _portraitBorder.color = _isSelected ? _selectedBorderColor : _normalBorderColor;
            }
        }

        private void HandleHPChanged(int current, int max)
        {
            UpdateHP(current, max, true);
        }

        private void HandleDeath()
        {
            OnAllyDeath?.Invoke(_allyIndex);
        }

        /// <summary>
        /// Public cleanup for CombatHUD to call when combat ends.
        /// Unbinds from combatant events and clears references.
        /// </summary>
        public void Cleanup()
        {
            UnbindAlly();
            ClearStatusIcons();
            ClearSkillSlots();
            _isUltimateReady = false;
            _isSelected = false;
            if (_glowCoroutine != null)
            {
                StopCoroutine(_glowCoroutine);
                _glowCoroutine = null;
            }
        }

        private void UnbindAlly()
        {
            if (_ally != null)
            {
                _ally.OnHpChanged -= HandleHPChanged;
                _ally.OnDeath -= HandleDeath;
            }
            _ally = null;
        }

        // Buff range boundaries matching StatusEffectType enum layout:
        // Buffs span ATTACK_UP(30) through UNDYING(72), debuffs start at ATTACK_DOWN(80)
        private const int kBuffRangeStart = (int)StatusEffectType.ATTACK_UP;   // 30
        private const int kBuffRangeEnd = (int)StatusEffectType.ATTACK_DOWN;   // 80

        private static bool IsBuff(StatusEffectType effectType)
        {
            int value = (int)effectType;
            return value >= kBuffRangeStart && value < kBuffRangeEnd;
        }
    }
}
