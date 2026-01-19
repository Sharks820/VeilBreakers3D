using System;
using UnityEngine;
using VeilBreakers.Combat;
using VeilBreakers.Data;

namespace VeilBreakers.AI
{
    /// <summary>
    /// Defines a condition that can be evaluated in a gambit rule.
    /// Conditions are the IF part of "IF condition THEN action".
    /// </summary>
    [Serializable]
    public class GambitCondition
    {
        // =============================================================================
        // CONDITION TYPES
        // =============================================================================

        public enum ConditionType
        {
            // Self conditions
            SELF_HP_BELOW,              // Self HP < threshold
            SELF_HP_ABOVE,              // Self HP > threshold
            SELF_MP_BELOW,              // Self MP < threshold
            SELF_MP_ABOVE,              // Self MP > threshold
            SELF_HAS_STATUS,            // Self has specific status effect
            SELF_NOT_HAS_STATUS,        // Self doesn't have status effect

            // Ally conditions
            ALLY_HP_BELOW,              // Any ally HP < threshold
            ALLY_HP_ABOVE,              // Any ally HP > threshold
            ALLY_CRITICAL,              // Any ally HP < 25%
            ALLY_HAS_STATUS,            // Any ally has status effect
            ALLY_BEING_ATTACKED,        // Any ally is being targeted

            // Enemy conditions
            ENEMY_HP_BELOW,             // Target enemy HP < threshold
            ENEMY_HP_ABOVE,             // Target enemy HP > threshold
            ENEMY_IS_CASTING,           // Target enemy is casting
            ENEMY_HAS_STATUS,           // Target enemy has status effect
            ENEMY_NOT_HAS_STATUS,       // Target enemy doesn't have status
            ENEMY_IS_HEALER,            // Target enemy is healer type
            ENEMY_IS_TANK,              // Target enemy is tank type
            ENEMY_LOWEST_HP,            // Target is lowest HP enemy
            ENEMY_HIGHEST_HP,           // Target is highest HP enemy

            // Combat state conditions
            ENEMIES_CLUSTERED,          // Multiple enemies close together
            ENEMY_COUNT_ABOVE,          // More than X enemies alive
            ALLY_COUNT_BELOW,           // Less than X allies alive

            // Ability conditions
            ABILITY_OFF_COOLDOWN,       // Specific ability is ready
            ULTIMATE_READY,             // Ultimate is available

            // Always conditions
            ALWAYS,                     // Always true
            NEVER                       // Always false (disabled)
        }

        // =============================================================================
        // FIELDS
        // =============================================================================

        [Tooltip("Type of condition to evaluate")]
        public ConditionType conditionType = ConditionType.ALWAYS;

        [Tooltip("Threshold value (0-100 for percentages)")]
        [Range(0f, 100f)]
        public float threshold = 50f;

        [Tooltip("Status effect type (for status conditions)")]
        public StatusEffectType statusEffectType = StatusEffectType.NONE;

        [Tooltip("Ability slot (for ability conditions)")]
        public AbilitySlot abilitySlot = AbilitySlot.SKILL_1;

        [Tooltip("Integer count (for count-based conditions)")]
        public int countValue = 3;

        [Tooltip("Negate this condition (NOT)")]
        public bool negate = false;

        // =============================================================================
        // EVALUATION
        // =============================================================================

        /// <summary>
        /// Evaluates this condition for the given combatant.
        /// Returns true if the condition is met.
        /// </summary>
        public bool Evaluate(Combatant self, Combatant target, BattleContext context)
        {
            bool result = EvaluateInternal(self, target, context);
            return negate ? !result : result;
        }

        private bool EvaluateInternal(Combatant self, Combatant target, BattleContext context)
        {
            switch (conditionType)
            {
                // Self conditions
                case ConditionType.SELF_HP_BELOW:
                    return self.HpPercent < threshold;

                case ConditionType.SELF_HP_ABOVE:
                    return self.HpPercent > threshold;

                case ConditionType.SELF_MP_BELOW:
                    return self.MpPercent < threshold;

                case ConditionType.SELF_MP_ABOVE:
                    return self.MpPercent > threshold;

                case ConditionType.SELF_HAS_STATUS:
                    return context.HasStatus(self, statusEffectType);

                case ConditionType.SELF_NOT_HAS_STATUS:
                    return !context.HasStatus(self, statusEffectType);

                // Ally conditions
                case ConditionType.ALLY_HP_BELOW:
                    return context.AnyAllyHpBelow(self, threshold);

                case ConditionType.ALLY_HP_ABOVE:
                    return context.AnyAllyHpAbove(self, threshold);

                case ConditionType.ALLY_CRITICAL:
                    return context.AnyAllyHpBelow(self, 25f);

                case ConditionType.ALLY_HAS_STATUS:
                    return context.AnyAllyHasStatus(self, statusEffectType);

                case ConditionType.ALLY_BEING_ATTACKED:
                    return context.IsAllyBeingAttacked(self);

                // Enemy conditions
                case ConditionType.ENEMY_HP_BELOW:
                    return target != null && target.HpPercent < threshold;

                case ConditionType.ENEMY_HP_ABOVE:
                    return target != null && target.HpPercent > threshold;

                case ConditionType.ENEMY_IS_CASTING:
                    return target != null && context.IsCasting(target);

                case ConditionType.ENEMY_HAS_STATUS:
                    return target != null && context.HasStatus(target, statusEffectType);

                case ConditionType.ENEMY_NOT_HAS_STATUS:
                    return target != null && !context.HasStatus(target, statusEffectType);

                case ConditionType.ENEMY_IS_HEALER:
                    return target != null && context.IsHealer(target);

                case ConditionType.ENEMY_IS_TANK:
                    return target != null && context.IsTank(target);

                case ConditionType.ENEMY_LOWEST_HP:
                    return target != null && context.IsLowestHpEnemy(self, target);

                case ConditionType.ENEMY_HIGHEST_HP:
                    return target != null && context.IsHighestHpEnemy(self, target);

                // Combat state conditions
                case ConditionType.ENEMIES_CLUSTERED:
                    return context.GetClusteredEnemyCount(self) >= countValue;

                case ConditionType.ENEMY_COUNT_ABOVE:
                    return context.GetEnemyCount(self) > countValue;

                case ConditionType.ALLY_COUNT_BELOW:
                    return context.GetAllyCount(self) < countValue;

                // Ability conditions
                case ConditionType.ABILITY_OFF_COOLDOWN:
                    return context.IsAbilityReady(self, abilitySlot);

                case ConditionType.ULTIMATE_READY:
                    return context.IsAbilityReady(self, AbilitySlot.ULTIMATE);

                // Always conditions
                case ConditionType.ALWAYS:
                    return true;

                case ConditionType.NEVER:
                    return false;

                default:
                    Debug.LogWarning($"Unknown condition type: {conditionType}");
                    return false;
            }
        }

        /// <summary>
        /// Creates a condition programmatically.
        /// </summary>
        public static GambitCondition Create(ConditionType type, float threshold = 50f)
        {
            return new GambitCondition
            {
                conditionType = type,
                threshold = threshold
            };
        }

        /// <summary>
        /// Creates a status condition.
        /// </summary>
        public static GambitCondition CreateStatusCondition(ConditionType type, StatusEffectType status)
        {
            return new GambitCondition
            {
                conditionType = type,
                statusEffectType = status
            };
        }

        public override string ToString()
        {
            string negation = negate ? "NOT " : "";
            return conditionType switch
            {
                ConditionType.SELF_HP_BELOW => $"{negation}Self HP < {threshold}%",
                ConditionType.SELF_HP_ABOVE => $"{negation}Self HP > {threshold}%",
                ConditionType.ALLY_HP_BELOW => $"{negation}Ally HP < {threshold}%",
                ConditionType.ALLY_CRITICAL => $"{negation}Ally Critical",
                ConditionType.ENEMY_HP_BELOW => $"{negation}Enemy HP < {threshold}%",
                ConditionType.ENEMY_LOWEST_HP => $"{negation}Lowest HP Enemy",
                ConditionType.ULTIMATE_READY => $"{negation}Ultimate Ready",
                ConditionType.ALWAYS => "Always",
                _ => $"{negation}{conditionType}"
            };
        }
    }

    /// <summary>
    /// Battle context provides information about the current combat state
    /// that conditions need to evaluate.
    /// </summary>
    public class BattleContext
    {
        private readonly Combatant[] _allies;
        private readonly Combatant[] _enemies;
        private readonly Managers.StatusEffectManager _statusManager;
        private Combatant _currentAttackTarget;

        public BattleContext(Combatant[] allies, Combatant[] enemies, Managers.StatusEffectManager statusManager)
        {
            _allies = allies ?? Array.Empty<Combatant>();
            _enemies = enemies ?? Array.Empty<Combatant>();
            _statusManager = statusManager;
        }

        public void SetCurrentAttackTarget(Combatant target)
        {
            _currentAttackTarget = target;
        }

        public bool HasStatus(Combatant combatant, StatusEffectType effectType)
        {
            if (_statusManager == null || combatant == null) return false;
            return _statusManager.HasEffect(combatant.gameObject, effectType);
        }

        public bool AnyAllyHpBelow(Combatant self, float threshold)
        {
            for (int i = 0; i < _allies.Length; i++)
            {
                var ally = _allies[i];
                if (ally != null && ally != self && ally.IsAlive && ally.HpPercent < threshold)
                    return true;
            }
            return false;
        }

        public bool AnyAllyHpAbove(Combatant self, float threshold)
        {
            for (int i = 0; i < _allies.Length; i++)
            {
                var ally = _allies[i];
                if (ally != null && ally != self && ally.IsAlive && ally.HpPercent > threshold)
                    return true;
            }
            return false;
        }

        public bool AnyAllyHasStatus(Combatant self, StatusEffectType effectType)
        {
            for (int i = 0; i < _allies.Length; i++)
            {
                var ally = _allies[i];
                if (ally != null && ally != self && ally.IsAlive && HasStatus(ally, effectType))
                    return true;
            }
            return false;
        }

        public bool IsAllyBeingAttacked(Combatant self)
        {
            if (_currentAttackTarget == null) return false;

            for (int i = 0; i < _allies.Length; i++)
            {
                var ally = _allies[i];
                if (ally != null && ally != self && ally == _currentAttackTarget)
                    return true;
            }
            return false;
        }

        public bool IsCasting(Combatant target)
        {
            // TODO: Implement casting detection when cast system is added
            return false;
        }

        public bool IsHealer(Combatant target)
        {
            if (target == null) return false;
            return target.Brand == Brand.GRACE || target.Brand == Brand.MEND;
        }

        public bool IsTank(Combatant target)
        {
            if (target == null) return false;
            return target.Brand == Brand.IRON || target.Brand == Brand.LEECH;
        }

        public bool IsLowestHpEnemy(Combatant self, Combatant target)
        {
            if (target == null) return false;

            float lowestHp = float.MaxValue;
            Combatant lowestEnemy = null;

            for (int i = 0; i < _enemies.Length; i++)
            {
                var enemy = _enemies[i];
                if (enemy != null && enemy.IsAlive && enemy.HpPercent < lowestHp)
                {
                    lowestHp = enemy.HpPercent;
                    lowestEnemy = enemy;
                }
            }

            return lowestEnemy == target;
        }

        public bool IsHighestHpEnemy(Combatant self, Combatant target)
        {
            if (target == null) return false;

            float highestHp = float.MinValue;
            Combatant highestEnemy = null;

            for (int i = 0; i < _enemies.Length; i++)
            {
                var enemy = _enemies[i];
                if (enemy != null && enemy.IsAlive && enemy.HpPercent > highestHp)
                {
                    highestHp = enemy.HpPercent;
                    highestEnemy = enemy;
                }
            }

            return highestEnemy == target;
        }

        public int GetClusteredEnemyCount(Combatant self)
        {
            // TODO: Implement spatial clustering when positioning system is added
            // For now, return total enemy count
            return GetEnemyCount(self);
        }

        public int GetEnemyCount(Combatant self)
        {
            int count = 0;
            for (int i = 0; i < _enemies.Length; i++)
            {
                if (_enemies[i] != null && _enemies[i].IsAlive)
                    count++;
            }
            return count;
        }

        public int GetAllyCount(Combatant self)
        {
            int count = 0;
            for (int i = 0; i < _allies.Length; i++)
            {
                var ally = _allies[i];
                if (ally != null && ally != self && ally.IsAlive)
                    count++;
            }
            return count;
        }

        public bool IsAbilityReady(Combatant self, AbilitySlot slot)
        {
            // TODO: Implement cooldown check when ability system is integrated
            return true;
        }

        public Combatant[] GetAllies() => _allies;
        public Combatant[] GetEnemies() => _enemies;

        /// <summary>
        /// Gets the ally with the lowest HP (excluding self).
        /// </summary>
        public Combatant GetLowestHpAlly(Combatant self)
        {
            Combatant lowest = null;
            float lowestHp = float.MaxValue;

            for (int i = 0; i < _allies.Length; i++)
            {
                var ally = _allies[i];
                if (ally != null && ally != self && ally.IsAlive && ally.HpPercent < lowestHp)
                {
                    lowestHp = ally.HpPercent;
                    lowest = ally;
                }
            }

            return lowest;
        }

        /// <summary>
        /// Gets the enemy with the lowest HP.
        /// </summary>
        public Combatant GetLowestHpEnemy()
        {
            Combatant lowest = null;
            float lowestHp = float.MaxValue;

            for (int i = 0; i < _enemies.Length; i++)
            {
                var enemy = _enemies[i];
                if (enemy != null && enemy.IsAlive && enemy.HpPercent < lowestHp)
                {
                    lowestHp = enemy.HpPercent;
                    lowest = enemy;
                }
            }

            return lowest;
        }

        /// <summary>
        /// Gets the enemy with the highest HP.
        /// </summary>
        public Combatant GetHighestHpEnemy()
        {
            Combatant highest = null;
            float highestHp = float.MinValue;

            for (int i = 0; i < _enemies.Length; i++)
            {
                var enemy = _enemies[i];
                if (enemy != null && enemy.IsAlive && enemy.HpPercent > highestHp)
                {
                    highestHp = enemy.HpPercent;
                    highest = enemy;
                }
            }

            return highest;
        }
    }
}
