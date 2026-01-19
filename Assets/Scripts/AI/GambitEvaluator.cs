using System;
using System.Collections.Generic;
using UnityEngine;
using VeilBreakers.Combat;
using VeilBreakers.Data;

namespace VeilBreakers.AI
{
    /// <summary>
    /// Core utility scoring system for gambit AI.
    /// Calculates numerical value for every possible action and selects the highest.
    /// Formula: ActionScore = BaseValue × SituationMultipliers × BrandModifiers
    /// </summary>
    public class GambitEvaluator
    {
        // =============================================================================
        // CONSTANTS
        // =============================================================================

        private const float MIN_SCORE = 0.01f;
        private const float MAX_SCORE = 1000f;

        // Cached enum values to avoid allocation on each evaluation
        private static readonly PriorityBucket[] _bucketOrder =
            { PriorityBucket.CRITICAL, PriorityBucket.HIGH, PriorityBucket.STANDARD, PriorityBucket.LOW };

        // Cached comparer delegate to avoid allocation during sort
        private static readonly Comparison<ScoredAction> _scoreComparer =
            (a, b) => b.score.CompareTo(a.score);

        // Cached fallback rule to avoid allocation
        private static readonly GambitRule _fallbackRule = GambitRule.CreateAlways(
            "Fallback Attack",
            GambitAction.Create(GambitAction.ActionType.BASIC_ATTACK),
            PriorityBucket.LOW,
            1
        );

        // =============================================================================
        // CACHED DATA
        // =============================================================================

        private readonly AIPersonality _personality;
        private readonly GambitRuleSet _ruleSet;

        // Reusable lists for zero-allocation evaluation
        private readonly List<ScoredAction> _scoredActions = new List<ScoredAction>(32);
        private readonly List<GambitRule> _bucketRules = new List<GambitRule>(16);

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        public GambitEvaluator(AIPersonality personality, GambitRuleSet ruleSet)
        {
            _personality = personality ?? throw new ArgumentNullException(nameof(personality));
            _ruleSet = ruleSet ?? throw new ArgumentNullException(nameof(ruleSet));
            _ruleSet.SortByPriority();
        }

        // =============================================================================
        // MAIN EVALUATION
        // =============================================================================

        /// <summary>
        /// Evaluates all possible actions and returns the best one.
        /// Uses bucket system: CRITICAL bucket evaluated completely before STANDARD.
        /// </summary>
        public ScoredAction EvaluateBestAction(Combatant self, BattleContext context)
        {
            _scoredActions.Clear();

            // Evaluate buckets in order (CRITICAL → HIGH → STANDARD → LOW)
            // Uses cached bucket array to avoid Enum.GetValues allocation
            for (int i = 0; i < _bucketOrder.Length; i++)
            {
                EvaluateBucket(self, context, _bucketOrder[i]);

                // If we found valid actions in a higher priority bucket, use them
                if (_scoredActions.Count > 0)
                {
                    break;
                }
            }

            // If no valid actions, return fallback basic attack
            if (_scoredActions.Count == 0)
            {
                return CreateFallbackAction(self, context);
            }

            // Sort by score and return best (uses cached comparer)
            _scoredActions.Sort(_scoreComparer);
            return _scoredActions[0];
        }

        /// <summary>
        /// Evaluates all rules in a specific bucket.
        /// </summary>
        private void EvaluateBucket(Combatant self, BattleContext context, PriorityBucket bucket)
        {
            var enemies = context.GetEnemies();

            // Find all matching rules in this bucket
            _bucketRules.Clear();
            _ruleSet.FindMatchingRulesInBucket(self, null, context, bucket, _bucketRules);

            // Score each matching rule against potential targets
            for (int i = 0; i < _bucketRules.Count; i++)
            {
                var rule = _bucketRules[i];
                ScoreRule(self, context, rule, enemies);
            }
        }

        /// <summary>
        /// Scores a single rule against all valid targets.
        /// </summary>
        private void ScoreRule(Combatant self, BattleContext context, GambitRule rule, Combatant[] enemies)
        {
            var action = rule.action;

            // Determine target candidates based on action type
            if (IsAllyTargetedAction(action.actionType))
            {
                ScoreAllyTargetedRule(self, context, rule);
            }
            else if (IsSelfTargetedAction(action.actionType))
            {
                ScoreSelfTargetedRule(self, context, rule);
            }
            else
            {
                // Enemy-targeted actions
                for (int i = 0; i < enemies.Length; i++)
                {
                    var enemy = enemies[i];
                    if (enemy != null && enemy.IsAlive)
                    {
                        // Re-evaluate rule with specific target
                        if (rule.Evaluate(self, enemy, context))
                        {
                            float score = CalculateScore(self, enemy, context, rule);
                            if (score >= MIN_SCORE)
                            {
                                _scoredActions.Add(new ScoredAction(rule, enemy, score));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Score ally-targeted actions (heal, buff, guard).
        /// </summary>
        private void ScoreAllyTargetedRule(Combatant self, BattleContext context, GambitRule rule)
        {
            var allies = context.GetAllies();

            for (int i = 0; i < allies.Length; i++)
            {
                var ally = allies[i];
                if (ally != null && ally.IsAlive)
                {
                    if (rule.Evaluate(self, ally, context))
                    {
                        float score = CalculateAllyScore(self, ally, context, rule);
                        if (score >= MIN_SCORE)
                        {
                            _scoredActions.Add(new ScoredAction(rule, ally, score));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Score self-targeted actions (defend, wait).
        /// </summary>
        private void ScoreSelfTargetedRule(Combatant self, BattleContext context, GambitRule rule)
        {
            if (rule.Evaluate(self, self, context))
            {
                float score = CalculateSelfScore(self, context, rule);
                if (score >= MIN_SCORE)
                {
                    _scoredActions.Add(new ScoredAction(rule, self, score));
                }
            }
        }

        // =============================================================================
        // SCORE CALCULATION
        // =============================================================================

        /// <summary>
        /// Calculate score for enemy-targeted action.
        /// </summary>
        private float CalculateScore(Combatant self, Combatant target, BattleContext context, GambitRule rule)
        {
            float baseScore = rule.baseUtility;
            float multiplier = 1f;

            // Target selection multipliers
            bool isDebuffed = context.HasStatus(target, StatusEffectType.POISON) ||
                             context.HasStatus(target, StatusEffectType.BURN) ||
                             context.HasStatus(target, StatusEffectType.BLEED);
            bool isArmorShred = context.HasStatus(target, StatusEffectType.ARMOR_SHRED);
            bool isHealer = context.IsHealer(target);
            bool isTank = context.IsTank(target);
            bool isCasting = context.IsCasting(target);

            multiplier *= _personality.GetTargetMultiplier(
                target.HpPercent,
                isDebuffed,
                isArmorShred,
                isHealer,
                isTank,
                isCasting
            );

            // Self-condition multipliers
            if (IsAttackAction(rule.action.actionType))
            {
                multiplier *= _personality.GetSelfDamageMultiplier(self.HpPercent);

                // Category weight for damage
                multiplier *= (_personality.damageWeight / 25f); // Normalize to 1.0 at 25
            }

            // Execute bonus
            if (target.HpPercent < _personality.executeThreshold)
            {
                multiplier *= 1.5f;
            }

            // Brand effectiveness bonus (from brand system)
            // TODO: Integrate with BrandSystem when available
            // float brandEffectiveness = BrandSystem.GetEffectiveness(self.Brand, target.Brand);
            // multiplier *= brandEffectiveness;

            // Priority bonus
            multiplier *= (1f + (rule.priority / 100f));

            return Mathf.Clamp(baseScore * multiplier, MIN_SCORE, MAX_SCORE);
        }

        /// <summary>
        /// Calculate score for ally-targeted action.
        /// </summary>
        private float CalculateAllyScore(Combatant self, Combatant ally, BattleContext context, GambitRule rule)
        {
            float baseScore = rule.baseUtility;
            float multiplier = 1f;

            // HP-based urgency
            if (ally.HpPercent < _personality.criticalHpThreshold)
            {
                multiplier *= _personality.allyCriticalMultiplier;
            }
            else if (ally.HpPercent < _personality.lowHpThreshold)
            {
                multiplier *= _personality.allyLowHpMultiplier;
            }

            // Don't overheal
            if (rule.action.actionType == GambitAction.ActionType.HEAL_ALLY && ally.HpPercent > 90f)
            {
                multiplier *= 0.3f;
            }

            // Cleanse priority
            if (rule.action.actionType == GambitAction.ActionType.CLEANSE_ALLY)
            {
                // Check for dangerous debuffs
                if (context.HasStatus(ally, StatusEffectType.DOOM) ||
                    context.HasStatus(ally, StatusEffectType.CONDEMNED))
                {
                    multiplier *= 3.0f;
                }
                else if (context.HasStatus(ally, StatusEffectType.STUN) ||
                         context.HasStatus(ally, StatusEffectType.CHARM))
                {
                    multiplier *= 2.5f;
                }
            }

            // Category weight for team value
            multiplier *= (_personality.teamValueWeight / 25f);

            // Priority bonus
            multiplier *= (1f + (rule.priority / 100f));

            return Mathf.Clamp(baseScore * multiplier, MIN_SCORE, MAX_SCORE);
        }

        /// <summary>
        /// Calculate score for self-targeted action.
        /// </summary>
        private float CalculateSelfScore(Combatant self, BattleContext context, GambitRule rule)
        {
            float baseScore = rule.baseUtility;
            float multiplier = 1f;

            // Survival urgency
            multiplier *= _personality.GetSelfSurvivalMultiplier(self.HpPercent);

            // Auto-defend check
            if (rule.action.actionType == GambitAction.ActionType.DEFEND_SELF)
            {
                if (!_personality.canAutoDefend)
                {
                    // Only allow defend if critically low
                    if (self.HpPercent > _personality.autoDefendThreshold)
                    {
                        return MIN_SCORE;
                    }
                }
            }

            // Category weight for survival
            multiplier *= (_personality.survivalWeight / 25f);

            return Mathf.Clamp(baseScore * multiplier, MIN_SCORE, MAX_SCORE);
        }

        // =============================================================================
        // HELPER METHODS
        // =============================================================================

        private bool IsAllyTargetedAction(GambitAction.ActionType type)
        {
            return type == GambitAction.ActionType.HEAL_ALLY ||
                   type == GambitAction.ActionType.BUFF_ALLY ||
                   type == GambitAction.ActionType.CLEANSE_ALLY ||
                   type == GambitAction.ActionType.GUARD_ALLY;
        }

        private bool IsSelfTargetedAction(GambitAction.ActionType type)
        {
            return type == GambitAction.ActionType.DEFEND_SELF ||
                   type == GambitAction.ActionType.WAIT;
        }

        private bool IsAttackAction(GambitAction.ActionType type)
        {
            return type == GambitAction.ActionType.BASIC_ATTACK ||
                   type == GambitAction.ActionType.USE_ABILITY ||
                   type == GambitAction.ActionType.USE_ULTIMATE ||
                   type == GambitAction.ActionType.EXECUTE ||
                   type == GambitAction.ActionType.DEBUFF_ENEMY;
        }

        private ScoredAction CreateFallbackAction(Combatant self, BattleContext context)
        {
            // Fallback to basic attack on lowest HP enemy
            // Uses cached rule to avoid allocation
            var target = context.GetLowestHpEnemy();
            return new ScoredAction(_fallbackRule, target, MIN_SCORE);
        }

        // =============================================================================
        // DESPERATION BONUS (VOID special)
        // =============================================================================

        /// <summary>
        /// Calculate desperation bonus for VOID brand.
        /// Gets stronger when team is losing.
        /// </summary>
        public float CalculateDesperationBonus(Combatant self, BattleContext context)
        {
            if (!_personality.desperationBonus) return 1f;

            var allies = context.GetAllies();
            float totalHp = 0f;
            float totalMaxHp = 0f;

            for (int i = 0; i < allies.Length; i++)
            {
                var ally = allies[i];
                if (ally != null && ally.IsAlive)
                {
                    totalHp += ally.CurrentHp;
                    totalMaxHp += ally.MaxHp;
                }
            }

            // Add self
            totalHp += self.CurrentHp;
            totalMaxHp += self.MaxHp;

            float teamHpPercent = totalMaxHp > 0 ? (totalHp / totalMaxHp) * 100f : 100f;

            // Desperation bonus scales inversely with team HP
            // At 100% team HP: 1.0x
            // At 50% team HP: 1.5x
            // At 25% team HP: 2.0x
            if (teamHpPercent > 70f)
                return 1f;
            else if (teamHpPercent > 40f)
                return 1.5f;
            else
                return 2.0f;
        }
    }

    /// <summary>
    /// Represents a scored action candidate.
    /// </summary>
    public struct ScoredAction
    {
        public readonly GambitRule rule;
        public readonly Combatant target;
        public readonly float score;

        public ScoredAction(GambitRule rule, Combatant target, float score)
        {
            this.rule = rule;
            this.target = target;
            this.score = score;
        }

        public bool IsValid => rule != null && score > 0;

        public override string ToString()
        {
            string targetName = target != null ? target.DisplayName : "None";
            return $"[{score:F1}] {rule?.ruleName ?? "None"} -> {targetName}";
        }
    }
}
