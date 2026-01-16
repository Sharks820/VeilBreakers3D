using System;
using UnityEngine;
using VeilBreakers.Data;

namespace VeilBreakers.Systems
{
    /// <summary>
    /// Corruption System - Handles corruption state and stat modifications
    /// CORE PHILOSOPHY: Lower corruption = STRONGER (inverts typical dark power tropes)
    /// Goal is ASCENSION (0% corruption)
    /// </summary>
    public static class CorruptionSystem
    {
        // =============================================================================
        // CORRUPTION THRESHOLDS
        // =============================================================================

        public const float ASCENDED_THRESHOLD = 10f;     // 0-10%
        public const float PURIFIED_THRESHOLD = 25f;     // 11-25%
        public const float UNSTABLE_THRESHOLD = 50f;     // 26-50%
        public const float CORRUPTED_THRESHOLD = 75f;    // 51-75%
        // ABYSSAL = 76-100%

        // =============================================================================
        // STAT MULTIPLIERS
        // =============================================================================

        public const float ASCENDED_BONUS = 1.25f;      // +25% stats
        public const float PURIFIED_BONUS = 1.10f;      // +10% stats
        public const float UNSTABLE_BONUS = 1.00f;      // Normal stats
        public const float CORRUPTED_BONUS = 0.90f;     // -10% stats
        public const float ABYSSAL_BONUS = 0.80f;       // -20% stats

        // =============================================================================
        // STATE CALCULATIONS
        // =============================================================================

        /// <summary>
        /// Get corruption state from percentage (0-100)
        /// </summary>
        public static CorruptionState GetCorruptionState(float corruptionPercent)
        {
            if (corruptionPercent <= ASCENDED_THRESHOLD)
                return CorruptionState.ASCENDED;
            if (corruptionPercent <= PURIFIED_THRESHOLD)
                return CorruptionState.PURIFIED;
            if (corruptionPercent <= UNSTABLE_THRESHOLD)
                return CorruptionState.UNSTABLE;
            if (corruptionPercent <= CORRUPTED_THRESHOLD)
                return CorruptionState.CORRUPTED;
            return CorruptionState.ABYSSAL;
        }

        /// <summary>
        /// Get stat multiplier based on corruption level
        /// </summary>
        public static float GetStatMultiplier(float corruptionPercent)
        {
            var state = GetCorruptionState(corruptionPercent);
            return GetStatMultiplier(state);
        }

        /// <summary>
        /// Get stat multiplier for corruption state
        /// </summary>
        public static float GetStatMultiplier(CorruptionState state)
        {
            return state switch
            {
                CorruptionState.ASCENDED => ASCENDED_BONUS,
                CorruptionState.PURIFIED => PURIFIED_BONUS,
                CorruptionState.UNSTABLE => UNSTABLE_BONUS,
                CorruptionState.CORRUPTED => CORRUPTED_BONUS,
                CorruptionState.ABYSSAL => ABYSSAL_BONUS,
                _ => UNSTABLE_BONUS
            };
        }

        /// <summary>
        /// Apply corruption modifier to a stat value
        /// </summary>
        public static int ApplyCorruptionModifier(int baseStat, float corruptionPercent)
        {
            float multiplier = GetStatMultiplier(corruptionPercent);
            return Mathf.RoundToInt(baseStat * multiplier);
        }

        // =============================================================================
        // CORRUPTION CHANGES
        // =============================================================================

        /// <summary>
        /// Calculate corruption change from purification action
        /// </summary>
        public static float CalculatePurificationAmount(float currentCorruption, float purifyPower, float resistance)
        {
            // More corrupt = easier to purify initially, harder near ascension
            float difficultyMultiplier = currentCorruption > 50f ? 1.0f :
                                         currentCorruption > 25f ? 0.8f :
                                         currentCorruption > 10f ? 0.5f : 0.25f;

            float amount = purifyPower * difficultyMultiplier * (1f - resistance);
            return Mathf.Max(amount, 0.1f);  // Minimum purification
        }

        /// <summary>
        /// Calculate corruption gain from corrupting action
        /// </summary>
        public static float CalculateCorruptionGain(float currentCorruption, float corruptPower, float resistance)
        {
            // Already corrupted = less additional corruption
            float saturationMultiplier = currentCorruption < 50f ? 1.0f :
                                         currentCorruption < 75f ? 0.7f : 0.4f;

            float amount = corruptPower * saturationMultiplier * (1f - resistance);
            return Mathf.Max(amount, 0.1f);
        }

        // =============================================================================
        // DISPLAY HELPERS
        // =============================================================================

        /// <summary>
        /// Get color for corruption state
        /// </summary>
        public static Color GetCorruptionColor(CorruptionState state)
        {
            return state switch
            {
                CorruptionState.ASCENDED => new Color(1f, 0.9f, 0.4f),     // Golden
                CorruptionState.PURIFIED => new Color(0.6f, 0.9f, 0.7f),   // Light green
                CorruptionState.UNSTABLE => new Color(0.8f, 0.8f, 0.5f),   // Yellow
                CorruptionState.CORRUPTED => new Color(0.7f, 0.4f, 0.6f),  // Purple
                CorruptionState.ABYSSAL => new Color(0.3f, 0.1f, 0.3f),    // Dark purple
                _ => Color.white
            };
        }

        /// <summary>
        /// Get color for corruption bar/display based on percentage
        /// </summary>
        public static Color GetCorruptionBarColor(float corruptionPercent)
        {
            return GetCorruptionColor(GetCorruptionState(corruptionPercent));
        }

        /// <summary>
        /// Get display name for corruption state
        /// </summary>
        public static string GetCorruptionStateName(CorruptionState state)
        {
            return state switch
            {
                CorruptionState.ASCENDED => "Ascended",
                CorruptionState.PURIFIED => "Purified",
                CorruptionState.UNSTABLE => "Unstable",
                CorruptionState.CORRUPTED => "Corrupted",
                CorruptionState.ABYSSAL => "Abyssal",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Get description for corruption state
        /// </summary>
        public static string GetCorruptionDescription(CorruptionState state)
        {
            return state switch
            {
                CorruptionState.ASCENDED => "Fully purified! +25% to all stats.",
                CorruptionState.PURIFIED => "Mostly pure. +10% to all stats.",
                CorruptionState.UNSTABLE => "Balanced between light and dark. Normal stats.",
                CorruptionState.CORRUPTED => "Tainted by darkness. -10% to all stats.",
                CorruptionState.ABYSSAL => "Consumed by corruption. -20% to all stats.",
                _ => "Unknown corruption state"
            };
        }

        /// <summary>
        /// Get bonus/penalty string for display
        /// </summary>
        public static string GetStatBonusText(CorruptionState state)
        {
            float mult = GetStatMultiplier(state);
            if (mult > 1f)
                return $"+{(mult - 1f) * 100:F0}%";
            if (mult < 1f)
                return $"{(mult - 1f) * 100:F0}%";
            return "±0%";
        }
    }
}
