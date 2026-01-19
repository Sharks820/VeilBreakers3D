using System;
using UnityEngine;
using VeilBreakers.Combat;
using VeilBreakers.Data;

namespace VeilBreakers.AI
{
    /// <summary>
    /// Priority bucket for gambit rules (from The Sims bucket system).
    /// Higher priority buckets are evaluated completely before lower ones.
    /// </summary>
    public enum PriorityBucket
    {
        CRITICAL = 0,       // Self about to die, ally critical
        HIGH = 1,           // Execute opportunity, interrupt cast
        STANDARD = 2,       // Normal damage rotation, healing
        LOW = 3             // Buff refresh, positioning
    }

    /// <summary>
    /// A complete gambit rule that combines conditions with actions.
    /// Format: IF [conditions] THEN [action] WITH [priority]
    /// </summary>
    [Serializable]
    public class GambitRule
    {
        // =============================================================================
        // IDENTIFICATION
        // =============================================================================

        [Tooltip("Name of this rule for debugging")]
        public string ruleName = "New Rule";

        [Tooltip("Is this rule enabled?")]
        public bool enabled = true;

        // =============================================================================
        // CONDITIONS
        // =============================================================================

        [Tooltip("All conditions that must be true (AND logic)")]
        public GambitCondition[] conditions;

        [Tooltip("If true, ANY condition being true is enough (OR logic)")]
        public bool useOrLogic = false;

        // =============================================================================
        // ACTION
        // =============================================================================

        [Tooltip("Action to perform when conditions are met")]
        public GambitAction action;

        // =============================================================================
        // PRIORITY
        // =============================================================================

        [Tooltip("Priority bucket (CRITICAL evaluated before STANDARD)")]
        public PriorityBucket bucket = PriorityBucket.STANDARD;

        [Tooltip("Priority within bucket (higher = evaluated first)")]
        [Range(1, 100)]
        public int priority = 50;

        // =============================================================================
        // BASE UTILITY SCORE
        // =============================================================================

        [Tooltip("Base utility score for this rule")]
        [Range(0f, 100f)]
        public float baseUtility = 50f;

        // =============================================================================
        // EVALUATION
        // =============================================================================

        /// <summary>
        /// Evaluates whether this rule should be considered.
        /// Returns true if all conditions are met (or any for OR logic).
        /// </summary>
        public bool Evaluate(Combatant self, Combatant target, BattleContext context)
        {
            if (!enabled) return false;
            if (conditions == null || conditions.Length == 0) return true;

            if (useOrLogic)
            {
                // OR logic: any condition being true is enough
                for (int i = 0; i < conditions.Length; i++)
                {
                    if (conditions[i].Evaluate(self, target, context))
                        return true;
                }
                return false;
            }
            else
            {
                // AND logic: all conditions must be true
                for (int i = 0; i < conditions.Length; i++)
                {
                    if (!conditions[i].Evaluate(self, target, context))
                        return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Gets the combined priority value (bucket * 1000 + priority).
        /// Lower value = higher priority (CRITICAL bucket = 0xxx).
        /// </summary>
        public int GetCombinedPriority()
        {
            return ((int)bucket * 1000) + (100 - priority);
        }

        /// <summary>
        /// Execute this rule's action.
        /// </summary>
        public bool Execute(Combatant self, BattleContext context, out ActionResult result)
        {
            return action.Execute(self, context, out result);
        }

        // =============================================================================
        // FACTORY METHODS
        // =============================================================================

        /// <summary>
        /// Creates a simple rule with one condition.
        /// </summary>
        public static GambitRule Create(string name, GambitCondition condition, GambitAction action,
            PriorityBucket bucket = PriorityBucket.STANDARD, int priority = 50)
        {
            return new GambitRule
            {
                ruleName = name,
                enabled = true,
                conditions = new[] { condition },
                action = action,
                bucket = bucket,
                priority = priority,
                baseUtility = 50f
            };
        }

        /// <summary>
        /// Creates a rule with multiple conditions (AND logic).
        /// </summary>
        public static GambitRule CreateMultiCondition(string name, GambitCondition[] conditions,
            GambitAction action, PriorityBucket bucket = PriorityBucket.STANDARD, int priority = 50)
        {
            return new GambitRule
            {
                ruleName = name,
                enabled = true,
                conditions = conditions,
                useOrLogic = false,
                action = action,
                bucket = bucket,
                priority = priority,
                baseUtility = 50f
            };
        }

        /// <summary>
        /// Creates an always-active rule.
        /// </summary>
        public static GambitRule CreateAlways(string name, GambitAction action,
            PriorityBucket bucket = PriorityBucket.LOW, int priority = 10)
        {
            return new GambitRule
            {
                ruleName = name,
                enabled = true,
                conditions = new[] { GambitCondition.Create(GambitCondition.ConditionType.ALWAYS) },
                action = action,
                bucket = bucket,
                priority = priority,
                baseUtility = 10f
            };
        }

        public override string ToString()
        {
            string condStr = conditions != null && conditions.Length > 0
                ? string.Join(useOrLogic ? " OR " : " AND ", System.Array.ConvertAll(conditions, c => c.ToString()))
                : "Always";
            return $"[{bucket}:{priority}] IF {condStr} THEN {action}";
        }
    }

    /// <summary>
    /// Collection of gambit rules that form an AI behavior set.
    /// </summary>
    [Serializable]
    public class GambitRuleSet
    {
        public string setName = "Default";
        public GambitRule[] rules = Array.Empty<GambitRule>();

        /// <summary>
        /// Sort rules by priority (called once on initialization).
        /// </summary>
        public void SortByPriority()
        {
            if (rules == null || rules.Length == 0) return;

            System.Array.Sort(rules, (a, b) => a.GetCombinedPriority().CompareTo(b.GetCombinedPriority()));
        }

        /// <summary>
        /// Find the first rule that evaluates to true.
        /// </summary>
        public GambitRule FindMatchingRule(Combatant self, Combatant target, BattleContext context)
        {
            if (rules == null) return null;

            for (int i = 0; i < rules.Length; i++)
            {
                if (rules[i].Evaluate(self, target, context))
                    return rules[i];
            }

            return null;
        }

        /// <summary>
        /// Find all rules in a specific bucket that evaluate to true.
        /// </summary>
        public void FindMatchingRulesInBucket(Combatant self, Combatant target, BattleContext context,
            PriorityBucket bucket, System.Collections.Generic.List<GambitRule> results)
        {
            results.Clear();
            if (rules == null) return;

            for (int i = 0; i < rules.Length; i++)
            {
                if (rules[i].bucket == bucket && rules[i].Evaluate(self, target, context))
                    results.Add(rules[i]);
            }
        }
    }
}
