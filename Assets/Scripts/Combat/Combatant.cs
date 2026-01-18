using System;
using System.Collections.Generic;
using UnityEngine;
using VeilBreakers.Data;
using VeilBreakers.Systems;

namespace VeilBreakers.Combat
{
    /// <summary>
    /// Base class for all combat participants (monsters, heroes)
    /// </summary>
    public class Combatant : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string _combatantId;
        [SerializeField] private string _displayName;
        [SerializeField] private Brand _brand = Brand.NONE;
        [SerializeField] private bool _isPlayerControlled = true;

        [Header("Stats")]
        [SerializeField] private int _maxHp = 100;
        [SerializeField] private int _currentHp = 100;
        [SerializeField] private int _maxMp = 50;
        [SerializeField] private int _currentMp = 50;
        [SerializeField] private int _attack = 10;
        [SerializeField] private int _defense = 10;
        [SerializeField] private int _magic = 10;
        [SerializeField] private int _resistance = 10;
        [SerializeField] private int _speed = 10;

        [Header("State")]
        [SerializeField] private bool _isAlive = true;
        [SerializeField] private bool _isDefending = false;
        [SerializeField] private Combatant _guardTarget = null;

        // Abilities
        public AbilityLoadout Abilities { get; private set; }

        // Status effects
        private List<StatusEffectInstance> _statusEffects = new List<StatusEffectInstance>();

        // Properties
        public string CombatantId => _combatantId;
        public string DisplayName => _displayName;
        public Brand Brand => _brand;
        public bool IsPlayerControlled => _isPlayerControlled;
        public bool IsAlive => _isAlive;
        public bool IsDefending => _isDefending;
        public Combatant GuardTarget => _guardTarget;

        public int MaxHp => _maxHp;
        public int CurrentHp => _currentHp;
        public int MaxMp => _maxMp;
        public int CurrentMp => _currentMp;
        public int Attack => _attack;
        public int Defense => _defense;
        public int Magic => _magic;
        public int Resistance => _resistance;
        public int Speed => _speed;

        public float HpPercent => _maxHp > 0 ? (float)_currentHp / _maxHp : 0f;
        public float MpPercent => _maxMp > 0 ? (float)_currentMp / _maxMp : 0f;

        // Events
        public event Action<int, int> OnHpChanged;          // current, max
        public event Action<int, int> OnMpChanged;          // current, max
        public event Action<int, bool> OnDamageReceived;    // amount, isCritical
        public event Action<int> OnHealed;                   // amount
        public event Action OnDeath;
        public event Action OnRevive;

        /// <summary>
        /// Initialize combatant with data
        /// </summary>
        public void Initialize(string id, string name, Brand brand, int maxHp, int maxMp,
            int atk, int def, int mag, int res, int spd, bool isPlayer)
        {
            _combatantId = id;
            _displayName = name;
            _brand = brand;
            _maxHp = maxHp;
            _currentHp = maxHp;
            _maxMp = maxMp;
            _currentMp = maxMp;
            _attack = atk;
            _defense = def;
            _magic = mag;
            _resistance = res;
            _speed = spd;
            _isPlayerControlled = isPlayer;
            _isAlive = true;

            // Initialize default abilities (will be set properly by spawner)
            Abilities = new AbilityLoadout();
        }

        /// <summary>
        /// Set ability loadout
        /// </summary>
        public void SetAbilities(AbilityLoadout loadout)
        {
            Abilities = loadout;
        }

        /// <summary>
        /// Take damage (after all calculations)
        /// </summary>
        public void TakeDamage(int amount, bool isCritical = false)
        {
            if (!_isAlive) return;

            // Apply defense reduction if defending
            if (_isDefending)
            {
                amount = Mathf.RoundToInt(amount * 0.5f);
            }

            _currentHp = Mathf.Max(0, _currentHp - amount);
            OnDamageReceived?.Invoke(amount, isCritical);
            OnHpChanged?.Invoke(_currentHp, _maxHp);

            if (_currentHp <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Heal HP
        /// </summary>
        public void Heal(int amount)
        {
            if (!_isAlive) return;

            int previousHp = _currentHp;
            _currentHp = Mathf.Min(_maxHp, _currentHp + amount);
            int actualHeal = _currentHp - previousHp;

            if (actualHeal > 0)
            {
                OnHealed?.Invoke(actualHeal);
                OnHpChanged?.Invoke(_currentHp, _maxHp);
            }
        }

        /// <summary>
        /// Use MP
        /// </summary>
        public bool UseMp(int amount)
        {
            if (_currentMp < amount) return false;

            _currentMp -= amount;
            OnMpChanged?.Invoke(_currentMp, _maxMp);
            return true;
        }

        /// <summary>
        /// Restore MP
        /// </summary>
        public void RestoreMp(int amount)
        {
            _currentMp = Mathf.Min(_maxMp, _currentMp + amount);
            OnMpChanged?.Invoke(_currentMp, _maxMp);
        }

        /// <summary>
        /// Start defending
        /// </summary>
        public void StartDefend(DefenseAction action, Combatant guardTarget = null)
        {
            _isDefending = true;
            _guardTarget = action != DefenseAction.DEFEND_SELF ? guardTarget : null;
            Abilities.currentDefenseAction = action;
        }

        /// <summary>
        /// Stop defending
        /// </summary>
        public void StopDefend()
        {
            _isDefending = false;
            _guardTarget = null;
        }

        /// <summary>
        /// Handle death
        /// </summary>
        private void Die()
        {
            _isAlive = false;
            _isDefending = false;
            _guardTarget = null;
            OnDeath?.Invoke();
        }

        /// <summary>
        /// Revive combatant
        /// </summary>
        public void Revive(float hpPercent = 0.5f)
        {
            _isAlive = true;
            _currentHp = Mathf.RoundToInt(_maxHp * hpPercent);
            OnRevive?.Invoke();
            OnHpChanged?.Invoke(_currentHp, _maxHp);
        }

        /// <summary>
        /// Update cooldowns (called each frame during combat)
        /// </summary>
        public void UpdateCooldowns(float deltaTime)
        {
            Abilities?.UpdateAllCooldowns(deltaTime);
        }
    }

    /// <summary>
    /// Runtime status effect instance
    /// </summary>
    [Serializable]
    public class StatusEffectInstance
    {
        public StatusEffect effect;
        public float duration;
        public float tickInterval;
        public float lastTickTime;
        public int stacks;
        public Combatant source;
    }
}
