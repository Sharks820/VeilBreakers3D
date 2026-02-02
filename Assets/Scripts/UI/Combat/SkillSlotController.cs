using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VeilBreakers.Core;

namespace VeilBreakers.UI.Combat
{
    /// <summary>
    /// State of a skill slot.
    /// </summary>
    public enum SkillSlotState
    {
        READY,
        COOLDOWN,
        IN_USE,
        LOW_MP,
        DISABLED
    }

    /// <summary>
    /// Controls a single skill slot in the combat UI.
    /// </summary>
    public class SkillSlotController : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Components")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _cooldownOverlay;
        [SerializeField] private Image _borderImage;
        [SerializeField] private TextMeshProUGUI _keybindText;
        [SerializeField] private TextMeshProUGUI _cooldownText;

        [Header("Settings")]
        [SerializeField] private int _slotIndex;
        [SerializeField] private KeyCode _keyCode = KeyCode.Alpha1;
        [SerializeField] private string _keybindDisplay = "1";

        [Header("Colors")]
        [SerializeField] private Color _readyColor = Color.white;
        [SerializeField] private Color _cooldownColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color _lowMPColor = new Color(0.5f, 0.5f, 1f, 1f);
        [SerializeField] private Color _inUseColor = new Color(1f, 1f, 0.8f, 1f);
        [SerializeField] private Color _disabledColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color _ultimateBorderColor = new Color(1f, 0.84f, 0f, 1f); // Gold

        [Header("Animation")]
        [SerializeField] private float _glowPulseDuration = 1f;
        [SerializeField] private float _glowIntensity = 0.3f;

        // =============================================================================
        // STATE
        // =============================================================================
        
        private SkillSlotState _currentState = SkillSlotState.READY;
        private float _cooldownTotal = 0f;
        private float _cooldownRemaining = 0f;
        private int _cachedCooldownValue = -1;
        private bool _isUltimate = false;
        private bool _isGlowing = false;
        private Coroutine _glowCoroutine;
        private Coroutine _activationFlashCoroutine;
        // =============================================================================
        // PROPERTIES
        // =============================================================================

        public int SlotIndex => _slotIndex;
        public KeyCode KeyCode => _keyCode;
        public SkillSlotState State => _currentState;
        public float CooldownRemaining => _cooldownRemaining;
        public float CooldownPercent => _cooldownTotal > 0 ? _cooldownRemaining / _cooldownTotal : 0f;
        public bool IsReady => _currentState == SkillSlotState.READY;
        public bool IsUltimate => _isUltimate;

        // =============================================================================
        // EVENTS
        // =============================================================================

        public event Action<int> OnActivated;
        public event Action<int> OnCooldownComplete;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            if (_keybindText != null)
            {
                _keybindText.text = _keybindDisplay;
            }
            SetState(SkillSlotState.READY);
        }

        private void Update()
        {
            UpdateCooldown();
            CheckInput();
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Initialize the skill slot.
        /// </summary>
        public void Initialize(int slotIndex, bool isUltimate = false)
        {
            _slotIndex = slotIndex;
            _isUltimate = isUltimate;

            // Update keybind display based on slot index
            if (_keybindText != null)
            {
                _keybindText.text = slotIndex == 5 ? "R" : (slotIndex + 1).ToString();
            }

            // Ultimate slots get gold border
            if (_borderImage != null && isUltimate)
            {
                _borderImage.color = _ultimateBorderColor;
            }
        }

        /// <summary>
        /// Set the skill icon sprite.
        /// </summary>
        public void SetIcon(Sprite icon)
        {
            if (_iconImage != null)
            {
                _iconImage.sprite = icon;
            }
        }

        /// <summary>
        /// Set the current state of the slot.
        /// </summary>
        public void SetState(SkillSlotState state)
        {
            _currentState = state;
            UpdateVisuals();
        }

        /// <summary>
        /// Start cooldown with specified duration.
        /// </summary>
        public void StartCooldown(float duration)
        {
            _cooldownTotal = duration;
            _cooldownRemaining = duration;
            _cachedCooldownValue = -1; // Invalidate cache
            SetState(SkillSlotState.COOLDOWN);
        }

        /// <summary>
        /// Set cooldown remaining (for syncing with combat system).
        /// </summary>
        public void SetCooldown(float remaining, float total)
        {
            _cooldownTotal = total;
            _cooldownRemaining = remaining;
            _cachedCooldownValue = -1; // Invalidate cache

            if (remaining > 0)
            {
                SetState(SkillSlotState.COOLDOWN);
            }
            else if (_currentState == SkillSlotState.COOLDOWN)
            {
                SetState(SkillSlotState.READY);
            }
        }

        /// <summary>
        /// Clear cooldown immediately.
        /// </summary>
        public void ClearCooldown()
        {
            _cooldownRemaining = 0f;
            _cooldownTotal = 0f;
            SetState(SkillSlotState.READY);
        }

        /// <summary>
        /// Mark as low MP.
        /// </summary>
        public void SetLowMP(bool isLow)
        {
            if (isLow && _currentState == SkillSlotState.READY)
            {
                SetState(SkillSlotState.LOW_MP);
            }
            else if (!isLow && _currentState == SkillSlotState.LOW_MP)
            {
                SetState(SkillSlotState.READY);
            }
        }

        /// <summary>
        /// Start the ready glow animation.
        /// </summary>
        public void StartReadyGlow()
        {
            if (_glowCoroutine != null)
            {
                StopCoroutine(_glowCoroutine);
            }
            _isGlowing = true;
            _glowCoroutine = StartCoroutine(GlowAnimation());
        }

        /// <summary>
        /// Stop the ready glow animation.
        /// </summary>
        public void StopReadyGlow()
        {
            _isGlowing = false;
            if (_glowCoroutine != null)
            {
                StopCoroutine(_glowCoroutine);
                _glowCoroutine = null;
            }
        }

        /// <summary>
        /// Trigger activation effect.
        /// </summary>
        public void TriggerActivation()
        {
            if (_currentState != SkillSlotState.READY && _currentState != SkillSlotState.LOW_MP)
                return;

            SetState(SkillSlotState.IN_USE);
            OnActivated?.Invoke(_slotIndex);

            // Cancel any existing activation flash
            if (_activationFlashCoroutine != null)
            {
                StopCoroutine(_activationFlashCoroutine);
            }

            // Brief flash then return to appropriate state
            _activationFlashCoroutine = StartCoroutine(ActivationFlash());
        }

        // =============================================================================
        // COOLDOWN
        // =============================================================================

        private void UpdateCooldown()
        {
            if (_currentState != SkillSlotState.COOLDOWN) return;

            _cooldownRemaining -= Time.deltaTime;

            if (_cooldownRemaining <= 0f)
            {
                _cooldownRemaining = 0f;
                _cooldownTotal = 0f;
                SetState(SkillSlotState.READY);
                OnCooldownComplete?.Invoke(_slotIndex);

                // Start ready glow when cooldown finishes
                if (_isUltimate)
                {
                    StartReadyGlow();
                }
            }

            UpdateCooldownVisuals();
        }

        private void UpdateCooldownVisuals()
        {
            if (_cooldownOverlay != null)
            {
                _cooldownOverlay.fillAmount = CooldownPercent;
            }

            if (_cooldownText != null)
            {
                if (_cooldownRemaining > 0)
                {
                    _cooldownText.gameObject.SetActive(true);
                    if (_cooldownRemaining >= 1f)
                    {
                        int cooldownValue = Mathf.CeilToInt(_cooldownRemaining);
                        if (_cachedCooldownValue != cooldownValue)
                        {
                            _cooldownText.text = cooldownValue.ToString();
                            _cachedCooldownValue = cooldownValue;
                        }
                    }
                    else
                    {
                        // This still causes allocation, but it's only for the final second of the cooldown.
                        // A zero-allocation solution would require a more complex string building/caching strategy.
                        _cooldownText.text = _cooldownRemaining.ToString("F1");
                    }
                }
                else
                {
                    _cooldownText.gameObject.SetActive(false);
                }
            }
        }

        // =============================================================================
        // INPUT
        // =============================================================================

        private void CheckInput()
        {
            // Map slot index to GameAction
            // 0 -> Basic Attack (Q)
            // 1 -> Defend (E)
            // 2 -> Skill 1 (1)
            // 3 -> Skill 2 (2)
            // 4 -> Skill 3 (3)
            // 5 -> Skill 4 (4)
            // 6 -> Ultimate (R)
            InputManager.GameAction action = _slotIndex switch
            {
                0 => InputManager.GameAction.BasicAttack,
                1 => InputManager.GameAction.Defend,
                2 => InputManager.GameAction.Skill1,
                3 => InputManager.GameAction.Skill2,
                4 => InputManager.GameAction.Skill3,
                5 => InputManager.GameAction.Skill4,
                6 => InputManager.GameAction.Ultimate,
                _ => (InputManager.GameAction)(-1)
            };

            if (action != (InputManager.GameAction)(-1) && InputManager.Instance.GetActionDown(action))
            {
                TriggerActivation();
            }
        }

        // =============================================================================
        // VISUALS
        // =============================================================================

        private void UpdateVisuals()
        {
            Color iconColor = _currentState switch
            {
                SkillSlotState.READY => _readyColor,
                SkillSlotState.COOLDOWN => _cooldownColor,
                SkillSlotState.IN_USE => _inUseColor,
                SkillSlotState.LOW_MP => _lowMPColor,
                SkillSlotState.DISABLED => _disabledColor,
                _ => _readyColor
            };

            if (_iconImage != null)
            {
                _iconImage.color = iconColor;
            }

            // Show/hide cooldown overlay
            if (_cooldownOverlay != null)
            {
                _cooldownOverlay.gameObject.SetActive(_currentState == SkillSlotState.COOLDOWN);
            }

            // Update cooldown text visibility
            if (_cooldownText != null)
            {
                _cooldownText.gameObject.SetActive(_currentState == SkillSlotState.COOLDOWN && _cooldownRemaining > 0);
            }
        }

        // =============================================================================
        // ANIMATIONS
        // =============================================================================

        private IEnumerator GlowAnimation()
        {
            while (_isGlowing && _currentState == SkillSlotState.READY)
            {
                float t = (Mathf.Sin(Time.time * Mathf.PI * 2f / _glowPulseDuration) + 1f) / 2f;
                Color glowColor = Color.Lerp(_readyColor, Color.white, t * _glowIntensity);

                if (_iconImage != null)
                {
                    _iconImage.color = glowColor;
                }

                yield return null;
            }

            // Reset to normal color
            if (_iconImage != null)
            {
                _iconImage.color = _readyColor;
            }
        }

        private IEnumerator ActivationFlash()
        {
            yield return new WaitForSeconds(0.1f);

            // Return to ready or cooldown state will be set by external system
            if (_currentState == SkillSlotState.IN_USE)
            {
                SetState(SkillSlotState.READY);
            }
        }
    }
}
