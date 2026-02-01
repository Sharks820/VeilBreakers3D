using System;
using System.Collections.Generic;
using UnityEngine;
using VeilBreakers.Combat;
using VeilBreakers.Data;
using VeilBreakers.Systems;

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
        public static GambitCondition Create(ConditionType type, float threshold = 50f, int countValue = 3)
        {
            return new GambitCondition
            {
                conditionType = type,
                threshold = threshold,
                countValue = countValue
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
            if (target == null) return false;
            return target.IsCasting;
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

        // Clustering radius for AOE targeting (in world units)
        private const float CLUSTER_RADIUS = 5f;

        public int GetClusteredEnemyCount(Combatant self)
        {
            if (_enemies == null || _enemies.Length == 0) return 0;

            // Find the position that would hit the most enemies
            int maxCluster = 0;

            for (int i = 0; i < _enemies.Length; i++)
            {
                var center = _enemies[i];
                if (center == null || !center.IsAlive) continue;

                // Count how many enemies are within cluster radius of this enemy
                int clusterCount = 0;
                for (int j = 0; j < _enemies.Length; j++)
                {
                    var other = _enemies[j];
                    if (other == null || !other.IsAlive) continue;

                    float distance = Vector3.Distance(center.transform.position, other.transform.position);
                    if (distance <= CLUSTER_RADIUS)
                    {
                        clusterCount++;
                    }
                }

                if (clusterCount > maxCluster)
                {
                    maxCluster = clusterCount;
                }
            }

            return maxCluster;
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
            if (self == null || self.Abilities == null) return false;

            var ability = self.Abilities.GetAbility(slot);
            return ability?.isReady ?? false;
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

        /// <summary>
        /// Gets the best target for an AOE attack (center of largest cluster).
        /// </summary>
        public Combatant GetBestClusterTarget()
        {
            if (_enemies == null || _enemies.Length == 0) return null;

            Combatant bestCenter = null;
            int maxCluster = 0;

            for (int i = 0; i < _enemies.Length; i++)
            {
                var center = _enemies[i];
                if (center == null || !center.IsAlive) continue;

                int clusterCount = 0;
                for (int j = 0; j < _enemies.Length; j++)
                {
                    var other = _enemies[j];
                    if (other == null || !other.IsAlive) continue;

                    float distance = Vector3.Distance(center.transform.position, other.transform.position);
                    if (distance <= CLUSTER_RADIUS)
                    {
                        clusterCount++;
                    }
                }

                if (clusterCount > maxCluster)
                {
                    maxCluster = clusterCount;
                    bestCenter = center;
                }
            }

            return bestCenter;
        }

        /// <summary>
        /// Gets all enemies within cluster radius of a target.
        /// </summary>
        public void GetEnemiesInCluster(Combatant center, List<Combatant> results)
        {
            results.Clear();
            if (center == null || _enemies == null) return;

            for (int i = 0; i < _enemies.Length; i++)
            {
                var enemy = _enemies[i];
                if (enemy == null || !enemy.IsAlive) continue;

                float distance = Vector3.Distance(center.transform.position, enemy.transform.position);
                if (distance <= CLUSTER_RADIUS)
                {
                    results.Add(enemy);
                }
            }
        }

        /// <summary>
        /// Checks if target has brand weakness to attacker.
        /// </summary>
        public bool HasBrandAdvantage(Combatant attacker, Combatant defender)
        {
            if (attacker == null || defender == null) return false;
            return BrandSystem.HasAdvantage(attacker.Brand, defender.Brand);
        }

        /// <summary>
        /// Checks if target has brand resistance to attacker.
        /// </summary>
        public bool HasBrandDisadvantage(Combatant attacker, Combatant defender)
        {
            if (attacker == null || defender == null) return false;
            return BrandSystem.HasDisadvantage(attacker.Brand, defender.Brand);
        }

        // =============================================================================
        // ADVANCED AI INTELLIGENCE FEATURES
        // =============================================================================

        // Threat tracking data
        private readonly Dictionary<Combatant, float> _threatScores = new Dictionary<Combatant, float>();
        private readonly Dictionary<Combatant, float> _damageDealtRecently = new Dictionary<Combatant, float>();
        private readonly List<Combatant> _keysToRemove = new List<Combatant>(); // Reused to avoid allocations
        private float _lastThreatUpdate = 0f;
        private const float THREAT_DECAY_RATE = 0.9f; // 10% decay per update

        /// <summary>
        /// Record damage dealt by an enemy for threat tracking.
        /// Call this from the combat system when enemies deal damage.
        /// </summary>
        public void RecordEnemyDamage(Combatant enemy, float damageAmount)
        {
            if (enemy == null) return;

            if (_damageDealtRecently.ContainsKey(enemy))
                _damageDealtRecently[enemy] += damageAmount;
            else
                _damageDealtRecently[enemy] = damageAmount;

            UpdateThreatScores();
        }

        /// <summary>
        /// Update threat scores based on recent damage dealt.
        /// </summary>
        private void UpdateThreatScores()
        {
            float currentTime = Time.time;

            // Decay old threat scores
            if (currentTime - _lastThreatUpdate > 1f)
            {
                // First pass: identify keys to remove (reuse list to avoid allocation)
                _keysToRemove.Clear();
                foreach (var kvp in _threatScores)
                {
                    float decayed = kvp.Value * THREAT_DECAY_RATE;
                    if (decayed < 1f)
                        _keysToRemove.Add(kvp.Key);
                }
                // Remove expired entries
                foreach (var key in _keysToRemove)
                {
                    _threatScores.Remove(key);
                }
                // Decay remaining entries (copy keys to list first to avoid iteration issues)
                _keysToRemove.Clear();
                _keysToRemove.AddRange(_threatScores.Keys);
                foreach (var key in _keysToRemove)
                {
                    _threatScores[key] *= THREAT_DECAY_RATE;
                }
                _lastThreatUpdate = currentTime;
            }

            // Add recent damage to threat
            foreach (var kvp in _damageDealtRecently)
            {
                if (_threatScores.ContainsKey(kvp.Key))
                    _threatScores[kvp.Key] += kvp.Value;
                else
                    _threatScores[kvp.Key] = kvp.Value;
            }
            _damageDealtRecently.Clear();
        }

        /// <summary>
        /// Get the highest threat enemy (most damage dealer).
        /// </summary>
        public Combatant GetHighestThreatEnemy()
        {
            if (_enemies == null || _enemies.Length == 0) return null;

            Combatant highestThreat = null;
            float highestScore = 0f;

            for (int i = 0; i < _enemies.Length; i++)
            {
                var enemy = _enemies[i];
                if (enemy == null || !enemy.IsAlive) continue;

                float threatScore = GetThreatScore(enemy);
                if (threatScore > highestScore)
                {
                    highestScore = threatScore;
                    highestThreat = enemy;
                }
            }

            // Fall back to highest HP if no threat data
            return highestThreat ?? GetHighestHpEnemy();
        }

        /// <summary>
        /// Get threat score for a specific combatant.
        /// </summary>
        public float GetThreatScore(Combatant combatant)
        {
            if (combatant == null) return 0f;

            // Base threat from damage dealt
            float baseThreat = _threatScores.TryGetValue(combatant, out float score) ? score : 0f;

            // Bonus threat from dangerous brands
            if (combatant.Brand == Brand.SAVAGE || combatant.Brand == Brand.RUIN)
                baseThreat += 50f; // High damage dealers

            // Bonus threat if casting
            if (combatant.IsCasting)
                baseThreat += 100f; // Casting = imminent big damage

            return baseThreat;
        }

        /// <summary>
        /// Check if an enemy is a high priority threat.
        /// </summary>
        public bool IsHighThreat(Combatant enemy)
        {
            if (enemy == null) return false;
            float threshold = 100f; // Arbitrary threshold
            return GetThreatScore(enemy) >= threshold;
        }

        /// <summary>
        /// Estimate damage an action would deal to a target.
        /// Used for overkill prevention.
        /// </summary>
        public float EstimateDamage(Combatant attacker, Combatant target, GambitAction.ActionType actionType)
        {
            if (attacker == null || target == null) return 0f;

            // Base damage estimation from attack stat
            float baseDamage = attacker.Attack;

            // Action type multipliers
            float actionMultiplier = actionType switch
            {
                GambitAction.ActionType.BASIC_ATTACK => 1.0f,
                GambitAction.ActionType.USE_ABILITY => 1.5f,
                GambitAction.ActionType.USE_ULTIMATE => 3.0f,
                GambitAction.ActionType.EXECUTE => 2.5f,
                _ => 1.0f
            };

            // Brand effectiveness
            float brandMult = BrandSystem.GetEffectiveness(attacker.Brand, target.Brand);

            // Estimate defense reduction
            float defenseReduction = target.Defense > 0
                ? Mathf.Min((float)attacker.Attack / target.Defense, 2f)
                : 2f;

            return baseDamage * actionMultiplier * brandMult * defenseReduction;
        }

        /// <summary>
        /// Check if using a big ability would be overkill on the target.
        /// Returns true if basic attack would likely kill.
        /// </summary>
        public bool WouldBeOverkill(Combatant attacker, Combatant target, GambitAction.ActionType actionType)
        {
            if (target == null) return false;

            float estimatedDamage = EstimateDamage(attacker, target, GambitAction.ActionType.BASIC_ATTACK);
            float targetCurrentHp = target.CurrentHp;

            // If basic attack would kill, using ability is overkill
            if (estimatedDamage >= targetCurrentHp &&
                (actionType == GambitAction.ActionType.USE_ABILITY ||
                 actionType == GambitAction.ActionType.USE_ULTIMATE))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if any ally recently applied a debuff to the target.
        /// Used for combo detection.
        /// </summary>
        public bool HasRecentAllyDebuff(Combatant target)
        {
            if (target == null) return false;

            // Check for key debuffs that enable combos
            return HasStatus(target, StatusEffectType.ARMOR_SHRED) ||
                   HasStatus(target, StatusEffectType.STUN) ||
                   HasStatus(target, StatusEffectType.SLOW) ||
                   HasStatus(target, StatusEffectType.DEFENSE_DOWN);
        }

        /// <summary>
        /// Get the optimal ally to guard based on multiple factors.
        /// </summary>
        public Combatant GetOptimalGuardTarget(Combatant self)
        {
            Combatant bestTarget = null;
            float bestScore = 0f;

            for (int i = 0; i < _allies.Length; i++)
            {
                var ally = _allies[i];
                if (ally == null || ally == self || !ally.IsAlive) continue;

                float score = 0f;

                // Lower HP = higher priority
                score += (100f - ally.HpPercent) * 2f;

                // Healer ally = higher priority (keep them alive)
                if (IsHealer(ally))
                    score += 50f;

                // Being attacked = higher priority
                if (_currentAttackTarget == ally)
                    score += 100f;

                // Low defense ally = higher priority
                score += (50f - Mathf.Min(ally.Defense, 50)) * 0.5f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = ally;
                }
            }

            return bestTarget;
        }

        /// <summary>
        /// Check MP efficiency - should we save this ability for later?
        /// </summary>
        public bool ShouldConserveMp(Combatant self, int abilityCost)
        {
            if (self == null || self.MaxMp == 0) return false;

            float mpPercent = self.MpPercent;

            // If MP is low, only use abilities on critical moments
            if (mpPercent < 0.2f) // Below 20%
                return true;

            // If the ability would use more than 25% of remaining MP, consider conserving
            float mpRatio = (float)abilityCost / self.CurrentMp;
            if (mpRatio > 0.25f && mpPercent < 0.5f)
                return true;

            return false;
        }

        /// <summary>
        /// Get value score for using ultimate now vs waiting.
        /// Higher score = better time to ultimate.
        /// </summary>
        public float GetUltimateTimingScore(Combatant self)
        {
            float score = 50f; // Base score

            // Check for clustered enemies (AOE value)
            int clusterSize = GetClusteredEnemyCount(self);
            score += clusterSize * 15f;

            // Check for low HP enemies (execute potential)
            int lowHpEnemies = 0;
            for (int i = 0; i < _enemies.Length; i++)
            {
                if (_enemies[i] != null && _enemies[i].IsAlive && _enemies[i].HpPercent < 30f)
                    lowHpEnemies++;
            }
            score += lowHpEnemies * 20f;

            // Check for critical allies (healing ult value)
            int criticalAllies = 0;
            for (int i = 0; i < _allies.Length; i++)
            {
                if (_allies[i] != null && _allies[i].IsAlive && _allies[i].HpPercent < 25f)
                    criticalAllies++;
            }
            score += criticalAllies * 25f;

            // Penalty if self is about to die (might waste ult)
            if (self.HpPercent < 15f)
                score -= 30f;

            return score;
        }

        /// <summary>
        /// Check if it's a good time to use ultimate based on brand.
        /// </summary>
        public bool IsGoodUltimateTime(Combatant self, float threshold = 80f)
        {
            return GetUltimateTimingScore(self) >= threshold;
        }

        /// <summary>
        /// Get all enemies that we have brand advantage against.
        /// Useful for target prioritization.
        /// </summary>
        public void GetVulnerableEnemies(Combatant self, List<Combatant> results)
        {
            results.Clear();
            if (self == null || _enemies == null) return;

            for (int i = 0; i < _enemies.Length; i++)
            {
                var enemy = _enemies[i];
                if (enemy == null || !enemy.IsAlive) continue;

                if (BrandSystem.HasAdvantage(self.Brand, enemy.Brand))
                    results.Add(enemy);
            }
        }

        // =============================================================================
        // HEALER AI INTELLIGENCE
        // =============================================================================

        /// <summary>
        /// Get the optimal heal target using smart triage logic.
        /// Considers HP, incoming damage, role importance, and survival chance.
        /// </summary>
        public Combatant GetOptimalHealTarget(Combatant healer)
        {
            if (_allies == null || _allies.Length == 0) return null;

            Combatant bestTarget = null;
            float bestScore = float.MinValue;

            for (int i = 0; i < _allies.Length; i++)
            {
                var ally = _allies[i];
                if (ally == null || !ally.IsAlive) continue;

                // Skip if at full HP (don't overheal)
                if (ally.HpPercent >= 95f) continue;

                float score = 0f;

                // =================================================================
                // HP-BASED URGENCY (primary factor)
                // =================================================================
                float hpDeficit = 100f - ally.HpPercent;
                score += hpDeficit * 3f; // Higher weight for lower HP

                // Critical threshold bonus (below 25% HP)
                if (ally.HpPercent < 25f)
                    score += 100f;
                // Low threshold bonus (below 50% HP)
                else if (ally.HpPercent < 50f)
                    score += 50f;

                // =================================================================
                // TRIAGE: Can we actually save them?
                // =================================================================
                // Don't waste heal on someone who will die to next hit anyway
                if (ally.HpPercent < 10f && ally != healer)
                {
                    // Check if any enemy is about to attack them
                    if (_currentAttackTarget == ally)
                    {
                        // If they'll die even after heal, lower priority
                        float estimatedHeal = healer.Magic * 2f; // Rough heal estimate
                        float estimatedIncoming = GetHighestEnemyDamageEstimate();
                        if (estimatedIncoming > ally.CurrentHp + estimatedHeal)
                        {
                            score -= 80f; // Deprioritize if heal won't save them
                        }
                    }
                }

                // =================================================================
                // ROLE IMPORTANCE
                // =================================================================
                // Keep damage dealers alive for DPS checks
                if (ally.Brand == Brand.SAVAGE || ally.Brand == Brand.RUIN ||
                    ally.Brand == Brand.SURGE || ally.Brand == Brand.VENOM)
                {
                    score += 25f; // DPS priority
                }

                // Don't let the only healer die (includes self)
                if (ally.Brand == Brand.GRACE || ally.Brand == Brand.MEND)
                {
                    score += 40f; // Healer priority
                }

                // Protect allies that are defending/guarding others
                if (ally.IsDefending)
                {
                    score += 20f; // Active tank priority
                }

                // =================================================================
                // INCOMING DAMAGE AWARENESS
                // =================================================================
                if (_currentAttackTarget == ally)
                {
                    score += 60f; // About to be hit - heal now!
                }

                // Debuffed allies (damage over time, etc.) need heals more
                if (HasStatus(ally, StatusEffectType.POISON) ||
                    HasStatus(ally, StatusEffectType.BURN) ||
                    HasStatus(ally, StatusEffectType.BLEED))
                {
                    score += 30f; // Taking DOT damage
                }

                // =================================================================
                // EFFICIENCY: Don't overheal
                // =================================================================
                if (ally.HpPercent > 75f)
                {
                    score -= 40f; // Reduce priority for high HP allies
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = ally;
                }
            }

            // Fallback to lowest HP ally if no smart choice
            return bestTarget ?? GetLowestHpAlly(healer);
        }

        /// <summary>
        /// Estimate the highest damage any enemy could deal.
        /// Used for triage decisions.
        /// </summary>
        private float GetHighestEnemyDamageEstimate()
        {
            float highest = 0f;
            for (int i = 0; i < _enemies.Length; i++)
            {
                var enemy = _enemies[i];
                if (enemy == null || !enemy.IsAlive) continue;

                float estimate = enemy.Attack * 1.5f; // Assume ability-level damage
                if (estimate > highest)
                    highest = estimate;
            }
            return highest;
        }

        /// <summary>
        /// Get optimal buff target for support abilities.
        /// Prioritizes damage dealers and those about to act.
        /// </summary>
        public Combatant GetOptimalBuffTarget(Combatant self)
        {
            Combatant bestTarget = null;
            float bestScore = 0f;

            for (int i = 0; i < _allies.Length; i++)
            {
                var ally = _allies[i];
                if (ally == null || ally == self || !ally.IsAlive) continue;

                float score = 0f;

                // Damage dealers benefit most from buffs
                if (ally.Brand == Brand.SAVAGE || ally.Brand == Brand.RUIN ||
                    ally.Brand == Brand.SURGE)
                {
                    score += 60f;
                }

                // High attack stat = high buff value
                score += ally.Attack * 0.5f;

                // Healthy allies = better buff targets (they'll survive to use it)
                score += ally.HpPercent * 0.3f;

                // Already buffed = lower priority (don't stack same buffs)
                if (HasStatus(ally, StatusEffectType.ATTACK_UP) ||
                    HasStatus(ally, StatusEffectType.CRIT_RATE_UP))
                {
                    score -= 50f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = ally;
                }
            }

            return bestTarget;
        }

        /// <summary>
        /// Get optimal cleanse target.
        /// Prioritizes dangerous debuffs and important roles.
        /// </summary>
        public Combatant GetOptimalCleanseTarget(Combatant self)
        {
            Combatant bestTarget = null;
            float bestScore = 0f;

            for (int i = 0; i < _allies.Length; i++)
            {
                var ally = _allies[i];
                if (ally == null || !ally.IsAlive) continue;

                float score = 0f;

                // Critical debuffs (MUST cleanse)
                if (HasStatus(ally, StatusEffectType.DOOM) ||
                    HasStatus(ally, StatusEffectType.CONDEMNED))
                {
                    score += 200f; // Maximum priority
                }

                // Crowd control (high priority)
                if (HasStatus(ally, StatusEffectType.STUN) ||
                    HasStatus(ally, StatusEffectType.CHARM) ||
                    HasStatus(ally, StatusEffectType.CONFUSE))
                {
                    score += 100f;
                }

                // Damage debuffs (medium priority)
                if (HasStatus(ally, StatusEffectType.POISON) ||
                    HasStatus(ally, StatusEffectType.BURN) ||
                    HasStatus(ally, StatusEffectType.BLEED))
                {
                    score += 50f;
                }

                // Stat debuffs (lower priority)
                if (HasStatus(ally, StatusEffectType.DEFENSE_DOWN) ||
                    HasStatus(ally, StatusEffectType.ARMOR_SHRED))
                {
                    score += 30f;
                }

                // Role importance bonus
                if (ally.Brand == Brand.GRACE || ally.Brand == Brand.MEND)
                    score += 20f; // Keep healers functional

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = ally;
                }
            }

            return bestTarget;
        }

        /// <summary>
        /// Find the best enemy to focus (considering all factors).
        /// </summary>
        public Combatant GetOptimalAttackTarget(Combatant self)
        {
            if (_enemies == null || _enemies.Length == 0) return null;

            Combatant bestTarget = null;
            float bestScore = float.MinValue;

            for (int i = 0; i < _enemies.Length; i++)
            {
                var enemy = _enemies[i];
                if (enemy == null || !enemy.IsAlive) continue;

                float score = 0f;

                // Low HP priority (execute potential)
                score += (100f - enemy.HpPercent) * 2f;

                // Brand advantage bonus
                float effectiveness = BrandSystem.GetEffectiveness(self.Brand, enemy.Brand);
                if (effectiveness >= BrandSystem.SUPER_EFFECTIVE)
                    score += 50f;
                else if (effectiveness <= BrandSystem.NOT_EFFECTIVE)
                    score -= 30f;

                // Healer priority
                if (IsHealer(enemy))
                    score += 30f;

                // Debuffed enemy priority (combo potential)
                if (HasStatus(enemy, StatusEffectType.ARMOR_SHRED))
                    score += 40f;

                // Casting enemy priority (interrupt or burst)
                if (enemy.IsCasting)
                    score += 35f;

                // Threat priority
                score += GetThreatScore(enemy) * 0.5f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = enemy;
                }
            }

            return bestTarget;
        }
    }
}
