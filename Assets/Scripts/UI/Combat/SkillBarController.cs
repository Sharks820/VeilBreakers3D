using System;
using System.Collections.Generic;
using UnityEngine;
using VeilBreakers.Combat;

namespace VeilBreakers.UI.Combat
{
    /// <summary>
    /// Controls the player's skill bar (bottom-center).
    /// 7 slots: Q (Basic), E (Defend), 1-4 (Skills), R (Ultimate)
    /// </summary>
    public class SkillBarController : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Components")]
        [SerializeField] private Transform _slotContainer;

        [Header("Prefab")]
        [SerializeField] private GameObject _skillSlotPrefab;

        [Header("Bindings")]
        [SerializeField] private SkillSlotBinding[] _bindings;

        // =============================================================================
        // STATE
        // =============================================================================

        private Combatant _player;
        private readonly List<SkillSlotController> _slots = new List<SkillSlotController>();

        // Default bindings
        private static readonly SkillSlotBinding[] DefaultBindings = new SkillSlotBinding[]
        {
            new SkillSlotBinding { keyCode = KeyCode.Q, displayText = "Q", slotIndex = 0 },      // Basic Attack
            new SkillSlotBinding { keyCode = KeyCode.E, displayText = "E", slotIndex = 1 },      // Defend
            new SkillSlotBinding { keyCode = KeyCode.Alpha1, displayText = "1", slotIndex = 2 }, // Skill 1
            new SkillSlotBinding { keyCode = KeyCode.Alpha2, displayText = "2", slotIndex = 3 }, // Skill 2
            new SkillSlotBinding { keyCode = KeyCode.Alpha3, displayText = "3", slotIndex = 4 }, // Skill 3
            new SkillSlotBinding { keyCode = KeyCode.Alpha4, displayText = "4", slotIndex = 5 }, // Skill 4
            new SkillSlotBinding { keyCode = KeyCode.R, displayText = "R", slotIndex = 6 }       // Ultimate
        };

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        public IReadOnlyList<SkillSlotController> Slots => _slots;
        public int SlotCount => _slots.Count;

        // =============================================================================
        // EVENTS
        // =============================================================================

        public event Action<int> OnSkillActivated;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            if (_bindings == null || _bindings.Length == 0)
            {
                _bindings = DefaultBindings;
            }

            InitializeSlots();
        }

        private void OnDestroy()
        {
            foreach (var slot in _slots)
            {
                if (slot != null)
                {
                    slot.OnActivated -= HandleSlotActivated;
                }
            }
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Initialize with player data.
        /// </summary>
        public void Initialize(Combatant player)
        {
            _player = player;

            // Reset all slots
            foreach (var slot in _slots)
            {
                slot.SetState(SkillSlotState.READY);
            }

            // Sync cooldowns if player has abilities
            if (_player?.Abilities != null)
            {
                SyncCooldowns();
            }
        }

        /// <summary>
        /// Set skill icon for a slot.
        /// </summary>
        public void SetSkillIcon(int slotIndex, Sprite icon)
        {
            if (slotIndex >= 0 && slotIndex < _slots.Count)
            {
                _slots[slotIndex].SetIcon(icon);
            }
        }

        /// <summary>
        /// Start cooldown on a slot.
        /// </summary>
        public void StartCooldown(int slotIndex, float duration)
        {
            if (slotIndex >= 0 && slotIndex < _slots.Count)
            {
                _slots[slotIndex].StartCooldown(duration);
            }
        }

        /// <summary>
        /// Set cooldown remaining on a slot.
        /// </summary>
        public void SetCooldown(int slotIndex, float remaining, float total)
        {
            if (slotIndex >= 0 && slotIndex < _slots.Count)
            {
                _slots[slotIndex].SetCooldown(remaining, total);
            }
        }

        /// <summary>
        /// Clear cooldown on a slot.
        /// </summary>
        public void ClearCooldown(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < _slots.Count)
            {
                _slots[slotIndex].ClearCooldown();
            }
        }

        /// <summary>
        /// Set low MP state on skill slots.
        /// </summary>
        public void SetLowMP(bool isLow)
        {
            // Skills (not basic attack/defend) can be affected by low MP
            for (int i = 2; i < _slots.Count; i++)
            {
                _slots[i].SetLowMP(isLow);
            }
        }

        /// <summary>
        /// Get a slot by index.
        /// </summary>
        public SkillSlotController GetSlot(int index)
        {
            return (index >= 0 && index < _slots.Count) ? _slots[index] : null;
        }

        /// <summary>
        /// Sync cooldowns from player abilities.
        /// </summary>
        public void SyncCooldowns()
        {
            if (_player?.Abilities == null) return;

            // 7-slot UI ability bar mapping:
            // UI 0 -> Ability 0 (BASIC_ATTACK)
            // UI 1 -> Ability 1 (DEFEND)
            // UI 2 -> Ability 2 (SKILL_1)
            // UI 3 -> Ability 3 (SKILL_2)
            // UI 4 -> Ability 4 (SKILL_3)
            // UI 5 -> (Optional Skill 4, not in current enum)
            // UI 6 -> Ability 5 (ULTIMATE)

            // Update standard skills (2-4)
            for (int i = 2; i <= 4 && i < _slots.Count; i++)
            {
                int abilityIndex = i;
                float remaining = _player.Abilities.GetCooldownRemaining(abilityIndex);
                float total = _player.Abilities.GetCooldownDuration(abilityIndex);
                _slots[i].SetCooldown(remaining, total);
            }

            // Update ultimate (6)
            if (_slots.Count > 6)
            {
                int abilityIndex = 5; // ULTIMATE enum value
                float remaining = _player.Abilities.GetCooldownRemaining(abilityIndex);
                float total = _player.Abilities.GetCooldownDuration(abilityIndex);
                _slots[6].SetCooldown(remaining, total);
            }
        }

        /// <summary>
        /// Show or hide the skill bar.
        /// </summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        private void InitializeSlots()
        {
            ClearSlots();

            if (_slotContainer == null || _skillSlotPrefab == null) return;

            foreach (var binding in _bindings)
            {
                var slotObj = Instantiate(_skillSlotPrefab, _slotContainer);
                if (slotObj == null) continue;

                var slot = slotObj.GetComponent<SkillSlotController>();
                if (slot != null)
                {
                    bool isUltimate = (binding.slotIndex == 6); // R key = ultimate
                    slot.Initialize(binding.slotIndex, isUltimate);
                    slot.OnActivated += HandleSlotActivated;
                    _slots.Add(slot);
                }
            }
        }

        private void ClearSlots()
        {
            foreach (var slot in _slots)
            {
                if (slot != null)
                {
                    slot.OnActivated -= HandleSlotActivated;
                    Destroy(slot.gameObject);
                }
            }
            _slots.Clear();
        }

        // =============================================================================
        // EVENT HANDLERS
        // =============================================================================

        private void HandleSlotActivated(int slotIndex)
        {
            OnSkillActivated?.Invoke(slotIndex);
        }
    }
}
