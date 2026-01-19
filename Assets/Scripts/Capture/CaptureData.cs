using System;
using UnityEngine;
using VeilBreakers.Data;

namespace VeilBreakers.Capture
{
    // =============================================================================
    // ENUMS
    // =============================================================================

    /// <summary>
    /// Items used for capture attempts.
    /// </summary>
    public enum CaptureItem
    {
        NONE = 0,
        VEIL_SHARD = 1,     // Cheap, low effectiveness
        VEIL_CRYSTAL = 2,   // Mid-tier
        VEIL_CORE = 3,      // Expensive, reliable
        VEIL_HEART = 4      // Very expensive, best odds
    }

    /// <summary>
    /// State of a monster during capture flow.
    /// </summary>
    public enum CaptureState
    {
        NONE,               // Not in capture flow
        MARKED,             // Tagged for capture (C key)
        BINDING,            // Being bound by ally
        BOUND,              // Successfully bound
        CAPTURE_PHASE,      // Post-battle capture attempt
        CAPTURED,           // Successfully captured
        ESCAPED,            // Fled after failed capture
        BERSERK             // Broke free, combat resumed
    }

    /// <summary>
    /// Result of a capture attempt.
    /// </summary>
    public enum CaptureOutcome
    {
        SUCCESS,            // Monster captured
        FLEE,               // Monster escapes
        BERSERK             // Monster breaks free, combat resumes
    }

    /// <summary>
    /// QTE timing result quality.
    /// </summary>
    public enum QTEResult
    {
        MISS = 0,           // No bonus
        OKAY = 1,           // +5%
        GOOD = 2,           // +10%
        PERFECT = 3         // +15%
    }

    // =============================================================================
    // DATA STRUCTURES
    // =============================================================================

    /// <summary>
    /// Configuration for capture item effectiveness by monster rarity.
    /// </summary>
    [Serializable]
    public class CaptureItemConfig
    {
        // Base effectiveness percentages by rarity (index = MonsterRarity enum)
        // Format: [Common, Uncommon, Rare, Epic, Legendary]

        public static readonly int[] ShardEffectiveness = { 40, 30, 20, 10, 0 };
        public static readonly int[] CrystalEffectiveness = { 55, 45, 30, 15, 0 };
        public static readonly int[] CoreEffectiveness = { 85, 75, 65, 45, 15 };
        public static readonly int[] HeartEffectiveness = { 99, 95, 90, 75, 25 };

        /// <summary>
        /// Gets the base effectiveness of an item against a monster rarity.
        /// </summary>
        public static int GetEffectiveness(CaptureItem item, MonsterRarity rarity)
        {
            int rarityIndex = (int)rarity;

            // Clamp to valid range
            rarityIndex = Mathf.Clamp(rarityIndex, 0, 4);

            return item switch
            {
                CaptureItem.VEIL_SHARD => ShardEffectiveness[rarityIndex],
                CaptureItem.VEIL_CRYSTAL => CrystalEffectiveness[rarityIndex],
                CaptureItem.VEIL_CORE => CoreEffectiveness[rarityIndex],
                CaptureItem.VEIL_HEART => HeartEffectiveness[rarityIndex],
                _ => 0
            };
        }

        /// <summary>
        /// Gets the display name of a capture item.
        /// </summary>
        public static string GetDisplayName(CaptureItem item)
        {
            return item switch
            {
                CaptureItem.VEIL_SHARD => "Veil Shard",
                CaptureItem.VEIL_CRYSTAL => "Veil Crystal",
                CaptureItem.VEIL_CORE => "Veil Core",
                CaptureItem.VEIL_HEART => "Veil Heart",
                _ => "None"
            };
        }

        /// <summary>
        /// Gets the description of a capture item.
        /// </summary>
        public static string GetDescription(CaptureItem item)
        {
            return item switch
            {
                CaptureItem.VEIL_SHARD => "Basic capture item. Low effectiveness but cheap.",
                CaptureItem.VEIL_CRYSTAL => "Moderate capture item. Decent chance on common monsters.",
                CaptureItem.VEIL_CORE => "Advanced capture item. High effectiveness, worth the cost.",
                CaptureItem.VEIL_HEART => "Premium capture item. Best odds possible.",
                _ => "No item selected."
            };
        }
    }

    /// <summary>
    /// Configuration for bind threshold modifiers.
    /// </summary>
    [Serializable]
    public class BindThresholdConfig
    {
        // Base threshold (HP% at which monster becomes bindable)
        public const float BASE_THRESHOLD = 0.25f; // 25%

        // Corruption modifiers
        public const float LOW_CORRUPTION_BONUS = 0.15f;      // +15% (easier bind)
        public const float HIGH_CORRUPTION_PENALTY = -0.15f;  // -15% (harder bind)

        // Rarity modifiers
        public static readonly float[] RarityModifiers = { 0f, -0.05f, -0.10f, -0.15f, -0.15f };

        // Speed modifier (per 10 points above base)
        public const float HIGH_SPEED_PENALTY = -0.05f;

        // Brand advantage modifiers
        public const float BRAND_WEAK_BONUS = 0.10f;          // +10% (easier when weak)
        public const float BRAND_STRONG_PENALTY = -0.10f;     // -10% (harder when strong)

        // Intimidation bonus
        public const float INTIMIDATION_BONUS = 0.10f;        // +10%

        // Minimum threshold (can't go below this)
        public const float MIN_THRESHOLD = 0.05f; // 5%

        // Maximum threshold (can't go above this)
        public const float MAX_THRESHOLD = 0.50f; // 50%

        // Bound status duration (essentially permanent until capture/release)
        public const float BIND_STATUS_DURATION = float.MaxValue;
    }

    /// <summary>
    /// Configuration for capture formula modifiers.
    /// </summary>
    [Serializable]
    public class CaptureFormulaConfig
    {
        // Base capture chance
        public const float BASE_CHANCE = 0.50f; // 50%

        // HP modifiers (bonus for lower HP at bind)
        public static readonly float[] HPBonuses = { 0.15f, 0.10f, 0.05f, 0f, -0.05f };
        public static readonly float[] HPThresholds = { 0.10f, 0.20f, 0.30f, 0.40f, 0.50f };

        // Corruption modifiers
        public const float ASCENDED_BONUS = 0.20f;    // 0-10%
        public const float PURIFIED_BONUS = 0.10f;    // 11-25%
        public const float UNSTABLE_BONUS = 0f;       // 26-50%
        public const float CORRUPTED_PENALTY = -0.10f; // 51-75%
        public const float ABYSSAL_PENALTY = -0.20f;  // 76-100%

        // Rarity modifiers
        public static readonly float[] RarityModifiers = { 0f, -0.10f, -0.20f, -0.375f, -0.75f };

        // Level difference modifiers
        public const float PER_LEVEL_BELOW_BONUS = 0.03f;
        public const float PER_LEVEL_ABOVE_PENALTY = -0.05f;
        public const float MAX_LEVEL_BONUS = 0.15f;
        public const float MAX_LEVEL_PENALTY = -0.25f;

        // QTE bonuses
        public const float QTE_PERFECT_BONUS = 0.15f;
        public const float QTE_GOOD_BONUS = 0.10f;
        public const float QTE_OKAY_BONUS = 0.05f;
        public const float QTE_MISS_BONUS = 0f;

        // Minimum/Maximum capture chance
        public const float MIN_CHANCE = 0.01f; // 1%
        public const float MAX_CHANCE = 0.99f; // 99%
    }

    /// <summary>
    /// Configuration for capture failure outcomes.
    /// </summary>
    [Serializable]
    public class CaptureFailureConfig
    {
        // Base flee/berserk chances by corruption
        public static readonly float[] FleeChance = { 0.70f, 0.50f, 0.30f }; // Low, Mid, High
        public static readonly float[] BerserkChance = { 0.30f, 0.50f, 0.70f };

        // Modifiers for high rarity/level
        public const float EPIC_LEGENDARY_BERSERK_BONUS = 0.20f;
        public const float HIGH_LEVEL_BERSERK_BONUS = 0.10f;
        public const int HIGH_LEVEL_THRESHOLD = 5; // Levels above player

        // Berserk combat buff
        public const float BERSERK_DAMAGE_BUFF_MIN = 0.30f;
        public const float BERSERK_DAMAGE_BUFF_MAX = 0.50f;
    }

    /// <summary>
    /// Result of a capture calculation with breakdown.
    /// </summary>
    [Serializable]
    public class CaptureCalculationResult
    {
        public float finalChance;
        public float baseChance;
        public float itemModifier;
        public float hpModifier;
        public float corruptionModifier;
        public float rarityModifier;
        public float levelModifier;
        public float qteModifier;

        public override string ToString()
        {
            return $"Capture Chance: {finalChance:P0}\n" +
                   $"  Base: {baseChance:P0}\n" +
                   $"  Item: {itemModifier:+0%;-0%}\n" +
                   $"  HP: {hpModifier:+0%;-0%}\n" +
                   $"  Corruption: {corruptionModifier:+0%;-0%}\n" +
                   $"  Rarity: {rarityModifier:+0%;-0%}\n" +
                   $"  Level: {levelModifier:+0%;-0%}\n" +
                   $"  QTE: {qteModifier:+0%;-0%}";
        }
    }

    /// <summary>
    /// Data for a bound monster awaiting capture attempt.
    /// </summary>
    [Serializable]
    public class BoundMonsterData
    {
        public Combat.Combatant combatant;
        public CaptureState state;
        public float boundAtHPPercent;
        public float currentCorruption;
        public MonsterRarity rarity;
        public int monsterLevel;
        public Brand primaryBrand;
        public bool wasIntimidated;
        public float bindThreshold;

        public static BoundMonsterData Create(Combat.Combatant monster, float hpPercent, float threshold)
        {
            return new BoundMonsterData
            {
                combatant = monster,
                state = CaptureState.BOUND,
                boundAtHPPercent = hpPercent,
                currentCorruption = monster.Corruption,
                rarity = monster.Rarity,
                monsterLevel = monster.Level,
                primaryBrand = monster.PrimaryBrand,
                wasIntimidated = false, // Set by bind system if applicable
                bindThreshold = threshold
            };
        }
    }
}
