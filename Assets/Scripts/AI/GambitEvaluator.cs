using System;
using System.Collections.Generic;
using UnityEngine;
using VeilBreakers.Combat;
using VeilBreakers.Data;
using VeilBreakers.Systems;

namespace VeilBreakers.AI
{
    /// <summary>
    /// Core utility scoring system for gambit AI.
    /// Calculates numerical value for every possible action and selects the highest.
    /// Formula: ActionScore = BaseValue × SituationMultipliers × BrandModifiers
    ///
    /// INTELLIGENCE SCALING:
    /// Monster smarts scale with Rarity + Level + Evolution
    /// - Common/Low Level: Basic tactics only (HP checks, basic targeting)
    /// - Uncommon/Mid Level: Threat assessment, basic combos
    /// - Rare/High Level: Overkill prevention, ultimate timing
    /// - Epic/Max Level: Full advanced AI (all features)
    /// - Legendary: Perfect decision making
    /// </summary>
    public class GambitEvaluator
    {
        // =============================================================================
        // CONSTANTS
        // =============================================================================

        private const float MIN_SCORE = 0.01f;
        private const float MAX_SCORE = 1000f;

        // Intelligence tier thresholds (combined ADDITIVE score)
        // A max-level evolved Common = base Legendary = both can reach Master tier!
        // Formula: IntelligenceScore = RarityBase + (Level * 0.5) + (Evolution * 15)
        private const int INTELLIGENCE_TIER_BASIC = 0;      // New wild monsters
        private const int INTELLIGENCE_TIER_TACTICAL = 15;  // Some training
        private const int INTELLIGENCE_TIER_STRATEGIC = 35; // Well-trained
        private const int INTELLIGENCE_TIER_ADVANCED = 55;  // Elite training
        private const int INTELLIGENCE_TIER_MASTER = 75;    // Maximum intelligence

        // Rarity provides BASE intelligence (head start, NOT a cap)
        // Common: 0, Uncommon: 10, Rare: 20, Epic: 35, Legendary: 50
        // Level adds: Level * 0.5 (so level 50 = +25)
        // Evolution adds: EvolutionStage * 15 (0, 15, 30 for 3 stages)

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

        // Rarity to base intelligence mapping (matches MonsterRarity enum order)
        private static readonly int[] _rarityBaseIntelligence = { 0, 10, 20, 35, 50 };
        // COMMON=0, UNCOMMON=10, RARE=20, EPIC=35, LEGENDARY=50

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
        // INTELLIGENCE SCALING SYSTEM
        // =============================================================================

        /// <summary>
        /// Calculate intelligence score for a combatant.
        /// Higher score = smarter AI decisions.
        /// Any monster can reach max intelligence through training!
        /// </summary>
        /// <returns>Intelligence score (0-100+)</returns>
        public static int CalculateIntelligenceScore(Combatant combatant)
        {
            if (combatant == null) return 0;

            int score = 0;

            // Rarity provides base intelligence (head start, not cap)
            int rarityIndex = (int)combatant.Rarity;
            if (rarityIndex >= 0 && rarityIndex < _rarityBaseIntelligence.Length)
            {
                score += _rarityBaseIntelligence[rarityIndex];
            }

            // Level adds intelligence (level 1-100 → 0.5-50 points)
            score += Mathf.RoundToInt(combatant.Level * 0.5f);

            // Evolution adds significant intelligence (stages 0, 1, 2 → 0, 15, 30)
            // TODO: Add evolution_stage to Combatant if not present
            // For now, use level brackets as proxy for evolution
            if (combatant.Level >= 30) score += 15; // First evolution
            if (combatant.Level >= 60) score += 15; // Second evolution

            // Boss monsters are always smart
            if (combatant.IsBoss)
            {
                score = Mathf.Max(score, INTELLIGENCE_TIER_ADVANCED);
            }

            return score;
        }

        /// <summary>
        /// Check if combatant has reached a specific intelligence tier.
        /// </summary>
        private static bool HasIntelligenceTier(int intelligenceScore, int tierThreshold)
        {
            return intelligenceScore >= tierThreshold;
        }

        /// <summary>
        /// Get a descriptive name for the intelligence tier.
        /// </summary>
        public static string GetIntelligenceTierName(Combatant combatant)
        {
            int score = CalculateIntelligenceScore(combatant);

            if (score >= INTELLIGENCE_TIER_MASTER) return "Master Tactician";
            if (score >= INTELLIGENCE_TIER_ADVANCED) return "Elite";
            if (score >= INTELLIGENCE_TIER_STRATEGIC) return "Strategic";
            if (score >= INTELLIGENCE_TIER_TACTICAL) return "Tactical";
            return "Instinctive";
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
            if (enemies == null) return; // Guard against null enemy array

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
            if (allies == null) return; // Guard against null ally array

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
            // AI prioritizes targets they have type advantage against
            float brandEffectiveness = BrandSystem.GetEffectiveness(self.Brand, target.Brand);
            if (brandEffectiveness >= BrandSystem.SUPER_EFFECTIVE)
            {
                multiplier *= 1.5f; // Heavily prioritize 2x damage targets
            }
            else if (brandEffectiveness <= BrandSystem.NOT_EFFECTIVE)
            {
                multiplier *= 0.6f; // Deprioritize 0.5x damage targets
            }

            // =================================================================
            // INTELLIGENCE-SCALED AI FEATURES
            // Higher rarity/level/evolution = smarter tactics!
            // Any monster can reach max intelligence through training!
            // =================================================================

            int intelligenceScore = CalculateIntelligenceScore(self);

            // TACTICAL TIER (15+): Basic combo awareness
            if (HasIntelligenceTier(intelligenceScore, INTELLIGENCE_TIER_TACTICAL))
            {
                // Combo detection - bonus for following up on ally debuffs
                if (context.HasRecentAllyDebuff(target) && IsAttackAction(rule.action.actionType))
                {
                    multiplier *= 1.3f; // 30% bonus for combo attacks
                }
            }

            // STRATEGIC TIER (35+): Threat assessment
            if (HasIntelligenceTier(intelligenceScore, INTELLIGENCE_TIER_STRATEGIC))
            {
                // Threat-based targeting - prioritize high-damage enemies
                if (context.IsHighThreat(target))
                {
                    multiplier *= 1.25f; // 25% bonus for high-threat targets
                }

                // Better combo detection at this tier
                if (context.HasRecentAllyDebuff(target) && IsAttackAction(rule.action.actionType))
                {
                    multiplier *= 1.1f; // Additional 10% (stacks with tactical)
                }
            }

            // ADVANCED TIER (55+): Resource optimization
            if (HasIntelligenceTier(intelligenceScore, INTELLIGENCE_TIER_ADVANCED))
            {
                // Overkill prevention - don't waste big abilities on near-dead targets
                if (context.WouldBeOverkill(self, target, rule.action.actionType))
                {
                    multiplier *= 0.3f; // Heavy penalty for wasting abilities
                }

                // Ultimate timing optimization
                if (rule.action.actionType == GambitAction.ActionType.USE_ULTIMATE)
                {
                    float ultScore = context.GetUltimateTimingScore(self);
                    if (ultScore < 60f)
                    {
                        multiplier *= 0.5f; // Delay ult if not a good time
                    }
                    else if (ultScore > 100f)
                    {
                        multiplier *= 1.5f; // Boost ult priority if perfect time
                    }
                }
            }

            // MASTER TIER (75+): Perfect decision making
            if (HasIntelligenceTier(intelligenceScore, INTELLIGENCE_TIER_MASTER))
            {
                // Perfect overkill prevention (even stricter)
                if (context.WouldBeOverkill(self, target, rule.action.actionType))
                {
                    multiplier *= 0.1f; // Near-zero for wasteful actions
                }

                // Synergy with team - huge combo bonus
                if (context.HasRecentAllyDebuff(target) && IsAttackAction(rule.action.actionType))
                {
                    multiplier *= 1.2f; // Additional 20% (stacks with others)
                }

                // Brand exploitation bonus - masters fully utilize type advantage
                if (brandEffectiveness >= BrandSystem.SUPER_EFFECTIVE)
                {
                    multiplier *= 1.2f; // Additional 20% for super effective
                }
            }

            // Priority bonus
            multiplier *= (1f + (rule.priority / 100f));

            return Mathf.Clamp(baseScore * multiplier, MIN_SCORE, MAX_SCORE);
        }

        /// <summary>
        /// Calculate score for ally-targeted action (heal, buff, guard, cleanse).
        /// Healer AI scales with intelligence - trained healers make smarter choices!
        /// </summary>
        private float CalculateAllyScore(Combatant self, Combatant ally, BattleContext context, GambitRule rule)
        {
            float baseScore = rule.baseUtility;
            float multiplier = 1f;
            int intelligenceScore = CalculateIntelligenceScore(self);

            // =================================================================
            // BASIC HEALER AI (all tiers)
            // =================================================================

            // HP-based urgency
            if (ally.HpPercent < _personality.criticalHpThreshold)
            {
                multiplier *= _personality.allyCriticalMultiplier;
            }
            else if (ally.HpPercent < _personality.lowHpThreshold)
            {
                multiplier *= _personality.allyLowHpMultiplier;
            }

            // Don't overheal (basic check)
            if (rule.action.actionType == GambitAction.ActionType.HEAL_ALLY && ally.HpPercent > 90f)
            {
                multiplier *= 0.3f;
            }

            // =================================================================
            // TACTICAL HEALER (15+): Role awareness
            // =================================================================
            if (HasIntelligenceTier(intelligenceScore, INTELLIGENCE_TIER_TACTICAL))
            {
                // Prioritize healing other healers (keep support chain alive)
                if (context.IsHealer(ally) && ally != self)
                {
                    multiplier *= 1.3f;
                }

                // Cleanse priority for CC
                if (rule.action.actionType == GambitAction.ActionType.CLEANSE_ALLY)
                {
                    if (context.HasStatus(ally, StatusEffectType.STUN) ||
                        context.HasStatus(ally, StatusEffectType.CHARM))
                    {
                        multiplier *= 2.0f;
                    }
                }
            }

            // =================================================================
            // STRATEGIC HEALER (35+): Triage and danger awareness
            // =================================================================
            if (HasIntelligenceTier(intelligenceScore, INTELLIGENCE_TIER_STRATEGIC))
            {
                // Check if ally is being targeted (heal before they get hit!)
                Combatant optimalHealTarget = context.GetOptimalHealTarget(self);
                if (ally == optimalHealTarget)
                {
                    multiplier *= 1.4f; // Bonus for smart target selection
                }

                // Cleanse DOOM/CONDEMNED immediately
                if (rule.action.actionType == GambitAction.ActionType.CLEANSE_ALLY)
                {
                    if (context.HasStatus(ally, StatusEffectType.DOOM) ||
                        context.HasStatus(ally, StatusEffectType.CONDEMNED))
                    {
                        multiplier *= 5.0f; // Life-saving cleanse!
                    }
                }

                // Don't heal someone about to die anyway (triage)
                if (ally.HpPercent < 10f && rule.action.actionType == GambitAction.ActionType.HEAL_ALLY)
                {
                    // Estimate if heal will save them
                    float healEstimate = self.Magic * 2f;
                    if (ally.CurrentHp + healEstimate < ally.MaxHp * 0.2f)
                    {
                        multiplier *= 0.5f; // Deprioritize if heal won't help much
                    }
                }
            }

            // =================================================================
            // ADVANCED HEALER (55+): Team composition awareness
            // =================================================================
            if (HasIntelligenceTier(intelligenceScore, INTELLIGENCE_TIER_ADVANCED))
            {
                // Prioritize damage dealers for buffs
                if (rule.action.actionType == GambitAction.ActionType.BUFF_ALLY)
                {
                    Combatant optimalBuffTarget = context.GetOptimalBuffTarget(self);
                    if (ally == optimalBuffTarget)
                    {
                        multiplier *= 1.5f;
                    }
                }

                // Smart cleanse target selection
                if (rule.action.actionType == GambitAction.ActionType.CLEANSE_ALLY)
                {
                    Combatant optimalCleanseTarget = context.GetOptimalCleanseTarget(self);
                    if (ally == optimalCleanseTarget)
                    {
                        multiplier *= 1.5f;
                    }
                }

                // DOT awareness - heal DOT'd allies before damage ticks
                if (context.HasStatus(ally, StatusEffectType.POISON) ||
                    context.HasStatus(ally, StatusEffectType.BURN) ||
                    context.HasStatus(ally, StatusEffectType.BLEED))
                {
                    multiplier *= 1.3f;
                }
            }

            // =================================================================
            // MASTER HEALER (75+): Perfect support
            // =================================================================
            if (HasIntelligenceTier(intelligenceScore, INTELLIGENCE_TIER_MASTER))
            {
                // Perfect triage - never waste heals
                if (rule.action.actionType == GambitAction.ActionType.HEAL_ALLY)
                {
                    // Don't heal full HP allies at all
                    if (ally.HpPercent > 95f)
                    {
                        multiplier *= 0.1f;
                    }
                    // Perfect heal timing bonus
                    else if (ally.HpPercent < 50f && ally.HpPercent > 20f)
                    {
                        multiplier *= 1.3f; // Ideal heal range
                    }
                }

                // Guard the right ally
                if (rule.action.actionType == GambitAction.ActionType.GUARD_ALLY)
                {
                    Combatant optimalGuardTarget = context.GetOptimalGuardTarget(self);
                    if (ally == optimalGuardTarget)
                    {
                        multiplier *= 1.5f;
                    }
                }
            }

            // Category weight for team value
            multiplier *= (_personality.teamValueWeight / 25f);

            // Priority bonus
            multiplier *= (1f + (rule.priority / 100f));

            return Mathf.Clamp(baseScore * multiplier, MIN_SCORE, MAX_SCORE);
        }

        /// <summary>
        /// Calculate score for self-targeted action (defend, wait).
        /// Tank/Defender AI scales with intelligence!
        /// </summary>
        private float CalculateSelfScore(Combatant self, BattleContext context, GambitRule rule)
        {
            float baseScore = rule.baseUtility;
            float multiplier = 1f;
            int intelligenceScore = CalculateIntelligenceScore(self);

            // =================================================================
            // BASIC DEFENSIVE AI (all tiers)
            // =================================================================

            // Survival urgency
            multiplier *= _personality.GetSelfSurvivalMultiplier(self.HpPercent);

            // Auto-defend check (basic)
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

            // =================================================================
            // TACTICAL DEFENDER (15+): Basic timing awareness
            // =================================================================
            if (HasIntelligenceTier(intelligenceScore, INTELLIGENCE_TIER_TACTICAL))
            {
                // Defend when low on MP (conserve resources)
                if (rule.action.actionType == GambitAction.ActionType.DEFEND_SELF)
                {
                    if (self.MpPercent < 20f)
                    {
                        multiplier *= 1.3f; // Conserve MP with defend
                    }
                }

                // Slightly smarter wait - only when situation is stable
                if (rule.action.actionType == GambitAction.ActionType.WAIT)
                {
                    if (self.HpPercent > 80f && context.GetAllyCount(self) > 0)
                    {
                        multiplier *= 1.2f; // Wait when healthy and allies present
                    }
                }
            }

            // =================================================================
            // STRATEGIC DEFENDER (35+): Anticipation
            // =================================================================
            if (HasIntelligenceTier(intelligenceScore, INTELLIGENCE_TIER_STRATEGIC))
            {
                // Defend when enemies are clustering (AOE incoming!)
                if (rule.action.actionType == GambitAction.ActionType.DEFEND_SELF)
                {
                    int clusteredEnemies = context.GetClusteredEnemyCount(self);
                    if (clusteredEnemies >= 2)
                    {
                        multiplier *= 1.4f; // Brace for multi-hit
                    }
                }

                // Smart defend when casting enemies present
                var enemies = context.GetEnemies();
                bool enemyCasting = false;
                for (int i = 0; i < enemies.Length; i++)
                {
                    if (enemies[i] != null && enemies[i].IsCasting)
                    {
                        enemyCasting = true;
                        break;
                    }
                }
                if (enemyCasting && rule.action.actionType == GambitAction.ActionType.DEFEND_SELF)
                {
                    multiplier *= 1.5f; // Brace for big hit
                }
            }

            // =================================================================
            // ADVANCED DEFENDER (55+): Team awareness
            // =================================================================
            if (HasIntelligenceTier(intelligenceScore, INTELLIGENCE_TIER_ADVANCED))
            {
                // Don't defend if allies need help more
                if (rule.action.actionType == GambitAction.ActionType.DEFEND_SELF)
                {
                    if (context.AnyAllyHpBelow(self, 25f))
                    {
                        multiplier *= 0.6f; // Should be helping allies, not defending
                    }
                }

                // Wait strategically to observe enemy patterns
                if (rule.action.actionType == GambitAction.ActionType.WAIT)
                {
                    if (context.GetEnemyCount(self) >= 3 && self.HpPercent > 70f)
                    {
                        multiplier *= 1.3f; // Observe before committing
                    }
                }
            }

            // =================================================================
            // MASTER DEFENDER (75+): Perfect defensive timing
            // =================================================================
            if (HasIntelligenceTier(intelligenceScore, INTELLIGENCE_TIER_MASTER))
            {
                // Never defend unnecessarily
                if (rule.action.actionType == GambitAction.ActionType.DEFEND_SELF)
                {
                    if (self.HpPercent > 90f && !context.IsAllyBeingAttacked(self))
                    {
                        multiplier *= 0.3f; // Don't waste turn defending when healthy
                    }
                }

                // Perfect resource management
                if (self.MpPercent < 10f)
                {
                    multiplier *= 1.5f; // Preserve turns for MP regen
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
            if (allies == null || allies.Length == 0)
            {
                // No allies, use only self for desperation calculation
                return 1f;
            }

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
