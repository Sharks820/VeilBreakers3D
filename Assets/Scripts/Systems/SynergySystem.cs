using System;
using System.Collections.Generic;
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
        /// Get synergy tier with explicit brand count (avoids allocation).
        /// </summary>
        public static SynergyTier GetSynergyTier(Path championPath, Brand[] partyBrands, int count)
        {
            if (championPath == Path.NONE || partyBrands == null || count == 0)
                return SynergyTier.NONE;

            // UNCHAINED path is always neutral (flex path)
            if (championPath == Path.UNCHAINED)
                return SynergyTier.NEUTRAL;

            int safeCount = Mathf.Min(count, partyBrands.Length);

            // Count both strong and weak brands first
            int weakCount = 0;
            if (PathWeakBrands.TryGetValue(championPath, out var weakBrands))
            {
                for (int i = 0; i < safeCount; i++)
                {
                    if (System.Array.IndexOf(weakBrands, partyBrands[i]) >= 0)
                        weakCount++;
                }
            }

            int strongCount = 0;
            if (PathSynergyBrands.TryGetValue(championPath, out var strongBrands))
            {
                for (int i = 0; i < safeCount; i++)
                {
                    if (System.Array.IndexOf(strongBrands, partyBrands[i]) >= 0)
                        strongCount++;
                }
            }

            // Determine tier: weak brands only override when they outnumber strong brands
            if (weakCount > strongCount) return SynergyTier.ANTI;
            if (strongCount >= 3) return SynergyTier.FULL;
            if (strongCount >= 2) return SynergyTier.PARTIAL;
            return SynergyTier.NEUTRAL;
        }

        /// <summary>
        /// Get synergy tier (convenience method)
        /// </summary>
        public static SynergyTier GetSynergyTier(Path championPath, Brand[] partyBrands)
        {
            if (championPath == Path.NONE || partyBrands == null || partyBrands.Length == 0)
                return SynergyTier.NONE;

            // UNCHAINED path is always neutral (flex path)
            if (championPath == Path.UNCHAINED)
                return SynergyTier.NEUTRAL;

            // Count both strong and weak brands — Array.IndexOf to avoid LINQ allocation
            int weakCount = 0;
            if (PathWeakBrands.TryGetValue(championPath, out var weakBrands))
            {
                foreach (var brand in partyBrands)
                {
                    if (System.Array.IndexOf(weakBrands, brand) >= 0)
                        weakCount++;
                }
            }

            int strongCount = 0;
            if (PathSynergyBrands.TryGetValue(championPath, out var strongBrands))
            {
                foreach (var brand in partyBrands)
                {
                    if (System.Array.IndexOf(strongBrands, brand) >= 0)
                        strongCount++;
                }
            }

            // Determine tier: weak brands only override when they outnumber strong brands
            if (weakCount > strongCount) return SynergyTier.ANTI;
            if (strongCount >= 3) return SynergyTier.FULL;
            if (strongCount >= 2) return SynergyTier.PARTIAL;
            return SynergyTier.NEUTRAL;
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
                SynergyTier.FULL => 1.08f,     // +8% damage reduction
                SynergyTier.PARTIAL => 1.05f,  // +5% damage reduction
                SynergyTier.ANTI => 0.92f,     // 8% MORE damage taken (anti-synergy penalty)
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
