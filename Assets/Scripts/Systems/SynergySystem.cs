using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VeilBreakers.Data;

namespace VeilBreakers.Systems
{
    /// <summary>
    /// Tiered Synergy System
    /// Full (3/3): +8%/+8%, 0.5x corruption, combo unlocked
    /// Partial (2/3): +5%/+5%, 0.75x corruption
    /// Neutral (0-1/3): No bonus
    /// Anti (weak brands): 1.5x corruption per weak brand
    /// </summary>
    public static class SynergySystem
    {
        // Synergy tier definitions
        public enum SynergyTier
        {
            NONE,
            ANTI,
            NEUTRAL,
            PARTIAL,
            FULL
        }

        // Path -> Strong synergy brands
        private static readonly Dictionary<Path, Brand[]> PathSynergyBrands = new Dictionary<Path, Brand[]>
        {
            { Path.IRONBOUND,   new[] { Brand.IRON, Brand.MEND, Brand.LEECH } },
            { Path.FANGBORN,    new[] { Brand.SAVAGE, Brand.VENOM, Brand.RUIN } },
            { Path.VOIDTOUCHED, new[] { Brand.VOID, Brand.DREAD, Brand.SURGE } },
            { Path.UNCHAINED,   new Brand[] { } }  // All neutral
        };

        // Path -> Weak synergy brands (cause faster corruption)
        private static readonly Dictionary<Path, Brand[]> PathWeakBrands = new Dictionary<Path, Brand[]>
        {
            { Path.IRONBOUND,   new[] { Brand.VOID, Brand.SAVAGE, Brand.RUIN } },
            { Path.FANGBORN,    new[] { Brand.GRACE, Brand.MEND, Brand.IRON } },
            { Path.VOIDTOUCHED, new[] { Brand.IRON, Brand.GRACE, Brand.MEND } },
            { Path.UNCHAINED,   new Brand[] { } }  // No weakness
        };

        /// <summary>
        /// Calculate synergy tier for a party composition
        /// </summary>
        public static SynergyTier GetSynergyTier(Path championPath, Brand[] partyBrands)
        {
            if (championPath == Path.NONE || partyBrands == null || partyBrands.Length == 0)
                return SynergyTier.NONE;

            // UNCHAINED path is always neutral (flex path)
            if (championPath == Path.UNCHAINED)
                return SynergyTier.NEUTRAL;

            // Check for weak brands (anti-synergy)
            if (PathWeakBrands.TryGetValue(championPath, out var weakBrands))
            {
                foreach (var brand in partyBrands)
                {
                    if (weakBrands.Contains(brand))
                        return SynergyTier.ANTI;
                }
            }

            // Count strong synergy matches
            int matchCount = 0;
            if (PathSynergyBrands.TryGetValue(championPath, out var strongBrands))
            {
                foreach (var brand in partyBrands)
                {
                    if (strongBrands.Contains(brand))
                        matchCount++;
                }
            }

            // Determine tier based on match count
            return matchCount switch
            {
                3 => SynergyTier.FULL,
                2 => SynergyTier.PARTIAL,
                _ => SynergyTier.NEUTRAL
            };
        }

        /// <summary>
        /// Get damage bonus multiplier for synergy tier
        /// </summary>
        public static float GetDamageBonus(SynergyTier tier)
        {
            return tier switch
            {
                SynergyTier.FULL => 1.08f,     // +8%
                SynergyTier.PARTIAL => 1.05f,  // +5%
                _ => 1.0f
            };
        }

        /// <summary>
        /// Get defense bonus multiplier for synergy tier
        /// </summary>
        public static float GetDefenseBonus(SynergyTier tier)
        {
            return tier switch
            {
                SynergyTier.FULL => 1.08f,     // +8%
                SynergyTier.PARTIAL => 1.05f,  // +5%
                _ => 1.0f
            };
        }

        /// <summary>
        /// Get corruption rate multiplier for synergy tier
        /// </summary>
        public static float GetCorruptionRateMultiplier(SynergyTier tier)
        {
            return tier switch
            {
                SynergyTier.FULL => 0.5f,      // Half corruption gain
                SynergyTier.PARTIAL => 0.75f,  // 75% corruption gain
                SynergyTier.ANTI => 1.5f,      // 150% corruption gain
                _ => 1.0f
            };
        }

        /// <summary>
        /// Check if combo ability is available
        /// </summary>
        public static bool IsComboUnlocked(SynergyTier tier)
        {
            return tier == SynergyTier.FULL;
        }

        /// <summary>
        /// Get synergy tier display name
        /// </summary>
        public static string GetTierName(SynergyTier tier)
        {
            return tier switch
            {
                SynergyTier.FULL => "Full Synergy",
                SynergyTier.PARTIAL => "Partial Synergy",
                SynergyTier.ANTI => "Anti-Synergy",
                SynergyTier.NEUTRAL => "Neutral",
                _ => "None"
            };
        }

        /// <summary>
        /// Get synergy tier color for UI
        /// </summary>
        public static Color GetTierColor(SynergyTier tier)
        {
            return tier switch
            {
                SynergyTier.FULL => new Color(0.2f, 0.9f, 0.3f),    // Green
                SynergyTier.PARTIAL => new Color(0.9f, 0.8f, 0.2f), // Yellow
                SynergyTier.ANTI => new Color(0.9f, 0.2f, 0.2f),    // Red
                _ => Color.gray
            };
        }
    }
}
