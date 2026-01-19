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
        [SerializeField] private string _monsterId;  // Audio bank identifier
        [SerializeField] private string _displayName;
        [SerializeField] private Brand _brand = Brand.NONE;
        [SerializeField] private bool _isPlayerControlled = true;
        [SerializeField] private bool _isPlayer = false;
        [SerializeField] private bool _isBoss = false;
        [SerializeField] private int _level = 1;
        [SerializeField] private MonsterRarity _rarity = MonsterRarity.COMMON;
        [SerializeField] private float _corruption = 0f;

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
        public string MonsterId => _monsterId;  // Audio bank identifier
        public string DisplayName => _displayName;
        public Brand Brand => _brand;
        public Brand PrimaryBrand => _brand; // Alias for capture system
        public bool IsPlayerControlled => _isPlayerControlled;
        public bool IsPlayer => _isPlayer;
        public bool IsBoss => _isBoss;
        public bool IsAlive => _isAlive;
        public bool IsDefending => _isDefending;
        public Combatant GuardTarget => _guardTarget;

        public int Level => _level;
        public MonsterRarity Rarity => _rarity;
        public float Corruption => _corruption;

        public int MaxHp => _maxHp;
        public int CurrentHp => _currentHp;
        public int MaxHP => _maxHp; // Alias for capture system
        public int CurrentHP => _currentHp; // Alias for capture system
        public int MaxMp => _maxMp;
        public int CurrentMp => _currentMp;
        public int Attack => _attack;
        public int Defense => _defense;
        public int Magic => _magic;
        public int Resistance => _resistance;
        public int Speed => _speed;

        public float HpPercent => _maxHp > 0 ? (float)_currentHp / _maxHp : 0f;
        public float HealthPercent => HpPercent; // Alias for audio system
        public float MpPercent => _maxMp > 0 ? (float)_currentMp / _maxMp : 0f;

        // Damage modifiers
        private float _damageBuffMultiplier = 1f;
        public float DamageMultiplier => _damageBuffMultiplier;

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

        // =============================================================================
        // CAPTURE SYSTEM SUPPORT
        // =============================================================================

        /// <summary>
        /// Set corruption level (0-100).
        /// </summary>
        public void SetCorruption(float corruption)
        {
            _corruption = Mathf.Clamp(corruption, 0f, 100f);
        }

        /// <summary>
        /// Set combatant level.
        /// </summary>
        public void SetLevel(int level)
        {
            _level = Mathf.Max(1, level);
        }

        /// <summary>
        /// Set monster rarity.
        /// </summary>
        public void SetRarity(MonsterRarity rarity)
        {
            _rarity = rarity;
        }

        /// <summary>
        /// Set whether this is the player character.
        /// </summary>
        public void SetPlayer(bool isPlayer)
        {
            _isPlayer = isPlayer;
        }

        /// <summary>
        /// Set monster ID for audio bank loading.
        /// </summary>
        public void SetMonsterId(string monsterId)
        {
            _monsterId = monsterId;
        }

        /// <summary>
        /// Set whether this is a boss monster.
        /// </summary>
        public void SetBoss(bool isBoss)
        {
            _isBoss = isBoss;
        }

        /// <summary>
        /// Apply a damage buff multiplier.
        /// </summary>
        public void ApplyDamageBuff(float multiplier)
        {
            _damageBuffMultiplier = 1f + multiplier;
        }

        /// <summary>
        /// Reset damage buff to normal.
        /// </summary>
        public void ClearDamageBuff()
        {
            _damageBuffMultiplier = 1f;
        }

        /// <summary>
        /// Apply a status effect.
        /// </summary>
        public void ApplyStatus(StatusEffectType type, float duration, Combatant source)
        {
            var instance = new StatusEffectInstance
            {
                effectType = type,
                duration = duration,
                stacks = 1,
                source = source
            };
            _statusEffects.Add(instance);
        }

        /// <summary>
        /// Remove a status effect by type.
        /// </summary>
        public void RemoveStatus(StatusEffectType type)
        {
            _statusEffects.RemoveAll(s => s.effectType == type);
        }

        /// <summary>
        /// Check if has a specific status effect.
        /// </summary>
        public bool HasStatus(StatusEffectType type)
        {
            foreach (var s in _statusEffects)
            {
                if (s.effectType == type) return true;
            }
            return false;
        }

        /// <summary>
        /// Get all active status effects.
        /// </summary>
        public IReadOnlyList<StatusEffectInstance> GetStatusEffects()
        {
            return _statusEffects;
        }
    }

    /// <summary>
    /// Runtime status effect instance
    /// </summary>
    [Serializable]
    public class StatusEffectInstance
    {
        public StatusEffectType effectType;
        public StatusEffectData effectData; // Optional: full ScriptableObject data
        public float duration;
        public float tickInterval;
        public float lastTickTime;
        public int stacks;
        public Combatant source;
    }
}
