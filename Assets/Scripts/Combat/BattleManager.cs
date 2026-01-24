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

        // Pre-allocated buffers to avoid GC allocations in Update
        private const int kMaxPartySize = 6;
        private Brand[] _brandBuffer = new Brand[kMaxPartySize];

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
            _playerParty = players ?? new List<Combatant>();
            _enemyParty = enemies ?? new List<Combatant>();
            _championPath = championPath;

            // Set player (first in party who is marked as player)
            _player = null;
            for (int i = 0; i < _playerParty.Count; i++)
            {
                if (_playerParty[i]?.IsPlayer == true)
                {
                    _player = _playerParty[i];
                    break;
                }
            }
            if (_player == null && _playerParty.Count > 0 && _playerParty[0] != null)
            {
                _player = _playerParty[0];
            }

            // Set initial target (first living enemy)
            _currentTarget = null;
            for (int i = 0; i < _enemyParty.Count; i++)
            {
                if (_enemyParty[i]?.IsAlive == true)
                {
                    _currentTarget = _enemyParty[i];
                    break;
                }
            }

            // Subscribe to death events (store handlers for proper cleanup)
            _deathHandlers.Clear();
            for (int i = 0; i < _playerParty.Count; i++)
            {
                var combatant = _playerParty[i];
                if (combatant == null) continue;
                var c = combatant; // Capture for closure
                Action handler = () => HandleCombatantDeath(c);
                _deathHandlers[combatant] = handler;
                combatant.OnDeath += handler;
            }
            for (int i = 0; i < _enemyParty.Count; i++)
            {
                var combatant = _enemyParty[i];
                if (combatant == null) continue;
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

            // Update all combatant cooldowns (no LINQ to avoid allocations)
            for (int i = 0; i < _playerParty.Count; i++)
            {
                var combatant = _playerParty[i];
                if (combatant != null && combatant.IsAlive)
                {
                    combatant.UpdateCooldowns(dt);
                }
            }

            for (int i = 0; i < _enemyParty.Count; i++)
            {
                var combatant = _enemyParty[i];
                if (combatant != null && combatant.IsAlive)
                {
                    combatant.UpdateCooldowns(dt);
                }
            }

            // Check victory/defeat conditions
            CheckBattleEnd();
        }

        /// <summary>
        /// Execute an ability from a combatant
        /// </summary>
        public void ExecuteAbility(Combatant user, AbilitySlot slot, Combatant target)
        {
            if (user == null || !user.IsAlive) return;
            if (user.Abilities == null) return;
            if (target == null) return;

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
                case SkillType.BUFF:
                    ExecuteBuff(user, target, skillData);
                    break;
                case SkillType.DEBUFF:
                    ExecuteDebuff(user, target, skillData);
                    break;
                case SkillType.UTILITY:
                    ExecuteUtility(user, target, skillData);
                    break;
                case SkillType.ULTIMATE:
                    ExecuteUltimate(user, target, skillData);
                    break;
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
        /// Execute buff ability (apply beneficial status effects to ally)
        /// </summary>
        private void ExecuteBuff(Combatant caster, Combatant target, SkillData skill)
        {
            ApplySkillStatusEffects(caster, target, skill);
            EventBus.BuffApplied(target.gameObject, skill.skill_id);
        }

        /// <summary>
        /// Execute debuff ability (apply harmful status effects to enemy)
        /// </summary>
        private void ExecuteDebuff(Combatant caster, Combatant target, SkillData skill)
        {
            // Check for guard intercept on debuffs too
            var interceptor = GetGuardInterceptor(target);
            if (interceptor != null)
            {
                target = interceptor;
            }

            ApplySkillStatusEffects(caster, target, skill);
            EventBus.DebuffApplied(target.gameObject, skill.skill_id);
        }

        /// <summary>
        /// Execute utility ability (movement, positioning, special actions)
        /// </summary>
        private void ExecuteUtility(Combatant caster, Combatant target, SkillData skill)
        {
            // Apply any status effects the utility skill may have
            ApplySkillStatusEffects(caster, target, skill);

            // Fire utility event for special handling
            EventBus.UtilityUsed(caster.gameObject, skill.skill_id);
        }

        /// <summary>
        /// Execute ultimate ability (powerful fight-changing skill)
        /// </summary>
        private void ExecuteUltimate(Combatant caster, Combatant target, SkillData skill)
        {
            // Ultimates can be attack, heal, buff, or debuff based on their effects
            if (skill.base_power > 0 && skill.GetDamageType() != DamageType.NONE)
            {
                // Ultimate with damage component
                ExecuteAttack(caster, target, skill);
            }

            // Apply any status effects
            ApplySkillStatusEffects(caster, target, skill);

            // Fire ultimate event
            EventBus.UltimateUsed(caster.gameObject, skill.skill_id);
        }

        /// <summary>
        /// Apply all status effects from a skill to target
        /// </summary>
        private void ApplySkillStatusEffects(Combatant caster, Combatant target, SkillData skill)
        {
            if (skill.status_effects == null || skill.status_effects.Count == 0)
                return;

            var statusManager = Managers.StatusEffectManager.Instance;
            if (statusManager == null)
            {
                Debug.LogWarning("[BattleManager] StatusEffectManager not available");
                return;
            }

            for (int i = 0; i < skill.status_effects.Count; i++)
            {
                var effectEntry = skill.status_effects[i];

                // Check chance to apply
                if (effectEntry.chance < 1f && UnityEngine.Random.value > effectEntry.chance)
                    continue;

                // Apply the effect
                statusManager.ApplyEffect(
                    (Data.StatusEffectType)effectEntry.effect,
                    caster.gameObject,
                    target.gameObject,
                    caster.GetMagic(), // stat modifier
                    1f, // skill rank
                    BrandSystem.GetEffectiveness(caster.Brand, target.Brand)
                );
            }
        }

        /// <summary>
        /// Find any combatant guarding the target
        /// </summary>
        private Combatant GetGuardInterceptor(Combatant target)
        {
            if (target == null) return null;
            
            // Check player party (no LINQ to avoid allocations)
            for (int i = 0; i < _playerParty.Count; i++)
            {
                var combatant = _playerParty[i];
                if (combatant != null && combatant.IsAlive && combatant.IsDefending && combatant.GuardTarget == target)
                {
                    return combatant;
                }
            }

            // Check enemy party
            for (int i = 0; i < _enemyParty.Count; i++)
            {
                var combatant = _enemyParty[i];
                if (combatant != null && combatant.IsAlive && combatant.IsDefending && combatant.GuardTarget == target)
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
            // Use pre-allocated buffer to avoid GC allocations
            int brandCount = 0;
            for (int i = 0; i < _playerParty.Count && brandCount < _brandBuffer.Length; i++)
            {
                var combatant = _playerParty[i];
                if (combatant != null && combatant.IsAlive)
                {
                    _brandBuffer[brandCount++] = combatant.Brand;
                }
            }

            // Create array segment for synergy calculation
            var partyBrands = new Brand[brandCount];
            System.Array.Copy(_brandBuffer, partyBrands, brandCount);

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
        /// Check if battle should end (no LINQ to avoid allocations)
        /// </summary>
        private void CheckBattleEnd()
        {
            bool anyPlayerAlive = false;
            for (int i = 0; i < _playerParty.Count; i++)
            {
                if (_playerParty[i] != null && _playerParty[i].IsAlive)
                {
                    anyPlayerAlive = true;
                    break;
                }
            }

            bool anyEnemyAlive = false;
            for (int i = 0; i < _enemyParty.Count; i++)
            {
                if (_enemyParty[i] != null && _enemyParty[i].IsAlive)
                {
                    anyEnemyAlive = true;
                    break;
                }
            }

            if (!anyPlayerAlive)
            {
                EndBattle(BattleState.DEFEAT);
            }
            else if (!anyEnemyAlive)
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
