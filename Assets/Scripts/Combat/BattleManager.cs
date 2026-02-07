using System;
using System.Collections.Generic;
using UnityEngine;
using VeilBreakers.Core;
using VeilBreakers.Data;
using VeilBreakers.Systems;

namespace VeilBreakers.Combat
{
    /// <summary>
    /// Manages real-time tactical combat
    /// </summary>
    public class BattleManager : SingletonMonoBehaviour<BattleManager>
    {
        // This is a scene-specific singleton
        protected override bool IsPersistent => false;

        [Header("Battle State")]
        [SerializeField] private BattleState _state = BattleState.INITIALIZING;

        [Header("Combatants")]
        [SerializeField] private Combatant _player; // The human player's character
        [SerializeField] private Combatant _currentTarget; // Currently targeted enemy
        [SerializeField] private Combatant _activeAlly; // Currently selected allied monster
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
        public Combatant ActiveAlly => _activeAlly;
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
        public event Action<Combatant> OnActiveAllyChanged;

        // Track death event handlers for proper cleanup
        private Dictionary<Combatant, Action> _deathHandlers = new Dictionary<Combatant, Action>();

        // Pre-allocated buffers to avoid GC allocations in Update
        private const int kMaxPartySize = 6;
        private Brand[] _brandBuffer = new Brand[kMaxPartySize];

        protected override void OnDestroy()
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
            
            base.OnDestroy();
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

            // Default ally selection to the first living non-player party member.
            SelectFirstLivingAlly();

            _state = BattleState.PLAYER_TURN; // Real-time, so this just means "active"
            OnBattleStart?.Invoke();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[BattleManager] Battle started! Synergy: {_currentSynergyTier}");
#endif
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
        /// Set active ally by reference.
        /// </summary>
        public void SetActiveAlly(Combatant ally)
        {
            if (ally == _activeAlly) return;
            if (!IsSelectableAlly(ally)) return;

            _activeAlly = ally;
            OnActiveAllyChanged?.Invoke(_activeAlly);
        }

        /// <summary>
        /// Select the next living ally (non-player) in party order.
        /// </summary>
        public bool SelectNextAlly()
        {
            return SelectAdjacentAlly(1);
        }

        /// <summary>
        /// Select the previous living ally (non-player) in party order.
        /// </summary>
        public bool SelectPreviousAlly()
        {
            return SelectAdjacentAlly(-1);
        }

        /// <summary>
        /// Select ally by visible ally index (0 = first non-player living ally).
        /// </summary>
        public bool SelectAllyByVisibleIndex(int visibleIndex)
        {
            if (visibleIndex < 0) return false;

            int found = 0;
            for (int i = 0; i < _playerParty.Count; i++)
            {
                var c = _playerParty[i];
                if (!IsSelectableAlly(c)) continue;

                if (found == visibleIndex)
                {
                    SetActiveAlly(c);
                    return true;
                }

                found++;
            }

            return false;
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[BattleManager] Not enough MP for {ability.skillId}");
#endif
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[BattleManager] {user.DisplayName} used {skillData.display_name}");
#endif
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
            // Debuffs bypass guards - guards only intercept damage, not status effects
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
            // Perform guard interception BEFORE attack so status effects also hit the redirected target
            var interceptor = GetGuardInterceptor(target);
            var damageTarget = interceptor ?? target;

            if (skill.base_power > 0 && skill.GetDamageType() != DamageType.NONE)
            {
                // Ultimate with damage component - pass damageTarget (ExecuteAttack will re-check guard,
                // but since interceptor is already guarding damageTarget, it won't double-redirect)
                ExecuteAttack(caster, damageTarget, skill);
            }

            // Status effects from ultimates should also hit the redirected target
            ApplySkillStatusEffects(caster, damageTarget, skill);

            // Fire ultimate event
            EventBus.UltimateUsed(caster.gameObject, skill.skill_id);
        }

        /// <summary>
        /// Apply all status effects from a skill to target
        /// </summary>
        private void ApplySkillStatusEffects(Combatant caster, Combatant target, SkillData skill)
        {
            if (caster == null || target == null) return;
            if (skill == null || skill.status_effects == null || skill.status_effects.Count == 0)
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
                if (effectEntry == null) continue;

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

            var oldCombatant = _playerParty[activeIndex];
            _playerParty[activeIndex] = _backupMonsters[backupIndex];
            _backupMonsters[backupIndex] = oldCombatant;

            // Unsubscribe old combatant's death handler
            if (oldCombatant != null && _deathHandlers.TryGetValue(oldCombatant, out var oldHandler))
            {
                oldCombatant.OnDeath -= oldHandler;
                _deathHandlers.Remove(oldCombatant);
            }

            // Subscribe new combatant's death handler
            var newCombatant = _playerParty[activeIndex];
            if (newCombatant != null)
            {
                var c = newCombatant; // Capture for closure
                Action newHandler = () => HandleCombatantDeath(c);
                newCombatant.OnDeath += newHandler;
                _deathHandlers[newCombatant] = newHandler;
            }

            // Recalculate synergy
            RecalculateSynergy();
            EnsureActiveAllyValid();

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

            // Use buffer directly (no allocation)
            var oldTier = _currentSynergyTier;
            _currentSynergyTier = SynergySystem.GetSynergyTier(_championPath, _brandBuffer, brandCount);

            if (oldTier != _currentSynergyTier)
            {
                OnSynergyChanged?.Invoke(_currentSynergyTier);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[BattleManager] Synergy changed: {oldTier} -> {_currentSynergyTier}");
#endif
            }
        }

        /// <summary>
        /// Handle combatant death
        /// </summary>
        private void HandleCombatantDeath(Combatant combatant)
        {
            OnCombatantDeath?.Invoke(combatant);
            RecalculateSynergy();
            if (combatant == _activeAlly)
            {
                EnsureActiveAllyValid();
            }
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
            _activeAlly = null;

            OnBattleEnd?.Invoke();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[BattleManager] Battle ended: {endState}");
#endif
        }

        private bool SelectAdjacentAlly(int direction)
        {
            if (_playerParty == null || _playerParty.Count == 0) return false;

            int currentIndex = _playerParty.IndexOf(_activeAlly);
            if (currentIndex < 0)
            {
                SelectFirstLivingAlly();
                return _activeAlly != null;
            }

            int total = _playerParty.Count;
            int scanIndex = currentIndex;
            for (int hops = 0; hops < total; hops++)
            {
                scanIndex = (scanIndex + direction + total) % total;
                var candidate = _playerParty[scanIndex];
                if (!IsSelectableAlly(candidate)) continue;

                SetActiveAlly(candidate);
                return true;
            }

            return false;
        }

        private void SelectFirstLivingAlly()
        {
            _activeAlly = null;
            for (int i = 0; i < _playerParty.Count; i++)
            {
                var c = _playerParty[i];
                if (!IsSelectableAlly(c)) continue;

                _activeAlly = c;
                break;
            }

            OnActiveAllyChanged?.Invoke(_activeAlly);
        }

        private void EnsureActiveAllyValid()
        {
            if (IsSelectableAlly(_activeAlly)) return;
            SelectFirstLivingAlly();
        }

        private bool IsSelectableAlly(Combatant combatant)
        {
            return combatant != null
                   && combatant.IsAlive
                   && combatant != _player
                   && _playerParty.Contains(combatant);
        }
    }
}
