using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VeilBreakers.Core;
using VeilBreakers.Data;
using VeilBreakers.Systems;

namespace VeilBreakers.Combat
{
    /// <summary>
    /// Manages real-time tactical combat
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }

        [Header("Battle State")]
        [SerializeField] private BattleState _state = BattleState.INITIALIZING;

        [Header("Combatants")]
        [SerializeField] private Combatant _player; // The human player's character
        [SerializeField] private Combatant _currentTarget; // Currently targeted enemy
        [SerializeField] private List<Combatant> _playerParty = new List<Combatant>();
        [SerializeField] private List<Combatant> _enemyParty = new List<Combatant>();
        [SerializeField] private List<Combatant> _backupMonsters = new List<Combatant>();

        [Header("Synergy")]
        [SerializeField] private Path _championPath = Path.NONE;
        [SerializeField] private SynergySystem.SynergyTier _currentSynergyTier;

        // Properties
        public BattleState State => _state;
        public Combatant Player => _player;
        public Combatant CurrentTarget => _currentTarget;
        public IReadOnlyList<Combatant> PlayerParty => _playerParty;
        public IReadOnlyList<Combatant> EnemyParty => _enemyParty;
        public SynergySystem.SynergyTier SynergyTier => _currentSynergyTier;
        public bool IsComboAvailable => SynergySystem.IsComboUnlocked(_currentSynergyTier);

        // Events
        public event Action OnBattleStart;
        public event Action OnBattleEnd;
        public event Action<Combatant, Combatant, DamageResult> OnDamageDealt;
        public event Action<Combatant, int> OnHealApplied;
        public event Action<Combatant> OnCombatantDeath;
        public event Action<SynergySystem.SynergyTier> OnSynergyChanged;
        public event Action<Combatant> OnTargetChanged;

        // Track death event handlers for proper cleanup
        private Dictionary<Combatant, Action> _deathHandlers = new Dictionary<Combatant, Action>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            // Clean up event subscriptions if destroyed mid-battle
            foreach (var kvp in _deathHandlers)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.OnDeath -= kvp.Value;
                }
            }
            _deathHandlers.Clear();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Initialize and start battle
        /// </summary>
        public void StartBattle(List<Combatant> players, List<Combatant> enemies, Path championPath)
        {
            _playerParty = players;
            _enemyParty = enemies;
            _championPath = championPath;

            // Set player (first in party who is marked as player)
            _player = _playerParty.FirstOrDefault(c => c.IsPlayer) ?? _playerParty.FirstOrDefault();

            // Set initial target (first living enemy)
            _currentTarget = _enemyParty.FirstOrDefault(c => c.IsAlive);

            // Subscribe to death events (store handlers for proper cleanup)
            _deathHandlers.Clear();
            foreach (var combatant in _playerParty.Concat(_enemyParty))
            {
                var c = combatant; // Capture for closure
                Action handler = () => HandleCombatantDeath(c);
                _deathHandlers[combatant] = handler;
                combatant.OnDeath += handler;
            }

            // Calculate initial synergy
            RecalculateSynergy();

            _state = BattleState.PLAYER_TURN; // Real-time, so this just means "active"
            OnBattleStart?.Invoke();

            Debug.Log($"[BattleManager] Battle started! Synergy: {_currentSynergyTier}");
        }

        /// <summary>
        /// Set the current target enemy
        /// </summary>
        public void SetCurrentTarget(Combatant target)
        {
            if (target == _currentTarget) return;
            if (target != null && !_enemyParty.Contains(target)) return;

            _currentTarget = target;
            OnTargetChanged?.Invoke(_currentTarget);
        }

        /// <summary>
        /// Update loop for real-time combat
        /// </summary>
        private void Update()
        {
            if (_state != BattleState.PLAYER_TURN && _state != BattleState.ENEMY_TURN)
                return;

            float dt = Time.deltaTime;

            // Update all combatant cooldowns
            foreach (var combatant in _playerParty.Where(c => c.IsAlive))
            {
                combatant.UpdateCooldowns(dt);
            }

            foreach (var combatant in _enemyParty.Where(c => c.IsAlive))
            {
                combatant.UpdateCooldowns(dt);
            }

            // Check victory/defeat conditions
            CheckBattleEnd();
        }

        /// <summary>
        /// Execute an ability from a combatant
        /// </summary>
        public void ExecuteAbility(Combatant user, AbilitySlot slot, Combatant target)
        {
            if (!user.IsAlive) return;

            var ability = user.Abilities.GetAbility(slot);
            if (ability == null || !ability.isReady) return;

            // Get skill data
            var skillData = GameDatabase.Instance?.GetSkill(ability.skillId);
            if (skillData == null)
            {
                Debug.LogWarning($"[BattleManager] Skill not found: {ability.skillId}");
                return;
            }

            // Check MP cost
            if (!user.UseMp(skillData.mp_cost))
            {
                Debug.Log($"[BattleManager] Not enough MP for {ability.skillId}");
                return;
            }

            // Trigger cooldown
            ability.TriggerCooldown();

            // Execute based on skill type
            switch (skillData.GetSkillType())
            {
                case SkillType.ATTACK:
                    ExecuteAttack(user, target, skillData);
                    break;
                case SkillType.HEAL:
                    ExecuteHeal(user, target, skillData);
                    break;
                case SkillType.DEFENSE:
                    user.StartDefend(user.Abilities.currentDefenseAction, target);
                    break;
                // TODO: Buff, Debuff, Utility
            }

            Debug.Log($"[BattleManager] {user.DisplayName} used {skillData.display_name}");
        }

        /// <summary>
        /// Execute attack ability
        /// </summary>
        private void ExecuteAttack(Combatant attacker, Combatant defender, SkillData skill)
        {
            // Check for guard intercept
            var interceptor = GetGuardInterceptor(defender);
            if (interceptor != null)
            {
                defender = interceptor;
            }

            // Calculate damage
            var result = DamageCalculator.Calculate(
                attacker, defender,
                skill.base_power,
                skill.GetDamageType(),
                _currentSynergyTier
            );

            // Apply damage
            defender.TakeDamage(result.finalDamage, result.isCritical);

            OnDamageDealt?.Invoke(attacker, defender, result);
        }

        /// <summary>
        /// Execute heal ability
        /// </summary>
        private void ExecuteHeal(Combatant healer, Combatant target, SkillData skill)
        {
            int healAmount = DamageCalculator.CalculateHeal(healer, skill.base_power);
            target.Heal(healAmount);
            OnHealApplied?.Invoke(target, healAmount);
        }

        /// <summary>
        /// Find any combatant guarding the target
        /// </summary>
        private Combatant GetGuardInterceptor(Combatant target)
        {
            // Check player party
            foreach (var combatant in _playerParty.Where(c => c.IsAlive && c.IsDefending))
            {
                if (combatant.GuardTarget == target)
                {
                    return combatant;
                }
            }

            // Check enemy party
            foreach (var combatant in _enemyParty.Where(c => c.IsAlive && c.IsDefending))
            {
                if (combatant.GuardTarget == target)
                {
                    return combatant;
                }
            }

            return null;
        }

        /// <summary>
        /// Swap a party member with a backup
        /// </summary>
        public bool SwapPartyMember(int activeIndex, int backupIndex)
        {
            if (activeIndex < 0 || activeIndex >= _playerParty.Count) return false;
            if (backupIndex < 0 || backupIndex >= _backupMonsters.Count) return false;

            var temp = _playerParty[activeIndex];
            _playerParty[activeIndex] = _backupMonsters[backupIndex];
            _backupMonsters[backupIndex] = temp;

            // Recalculate synergy
            RecalculateSynergy();

            return true;
        }

        /// <summary>
        /// Recalculate synergy tier based on current party
        /// </summary>
        private void RecalculateSynergy()
        {
            var partyBrands = _playerParty
                .Where(c => c.IsAlive)
                .Select(c => c.Brand)
                .ToArray();

            var oldTier = _currentSynergyTier;
            _currentSynergyTier = SynergySystem.GetSynergyTier(_championPath, partyBrands);

            if (oldTier != _currentSynergyTier)
            {
                OnSynergyChanged?.Invoke(_currentSynergyTier);
                Debug.Log($"[BattleManager] Synergy changed: {oldTier} -> {_currentSynergyTier}");
            }
        }

        /// <summary>
        /// Handle combatant death
        /// </summary>
        private void HandleCombatantDeath(Combatant combatant)
        {
            OnCombatantDeath?.Invoke(combatant);
            RecalculateSynergy();
        }

        /// <summary>
        /// Check if battle should end
        /// </summary>
        private void CheckBattleEnd()
        {
            bool allPlayersDead = _playerParty.All(c => !c.IsAlive);
            bool allEnemiesDead = _enemyParty.All(c => !c.IsAlive);

            if (allPlayersDead)
            {
                EndBattle(BattleState.DEFEAT);
            }
            else if (allEnemiesDead)
            {
                EndBattle(BattleState.VICTORY);
            }
        }

        /// <summary>
        /// End the battle
        /// </summary>
        private void EndBattle(BattleState endState)
        {
            _state = endState;

            // Unsubscribe from death events to prevent memory leaks
            foreach (var kvp in _deathHandlers)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.OnDeath -= kvp.Value;
                }
            }
            _deathHandlers.Clear();

            OnBattleEnd?.Invoke();
            Debug.Log($"[BattleManager] Battle ended: {endState}");
        }
    }
}
