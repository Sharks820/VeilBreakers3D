using System;
using System.Collections.Generic;
using UnityEngine;
using VeilBreakers.Data;

namespace VeilBreakers.Systems
{
    /// <summary>
    /// Path System - Handles path bonuses and progression
    /// 4 Paths: IRONBOUND (Tank), FANGBORN (Attack), VOIDTOUCHED (Magic), UNCHAINED (Hybrid)
    /// </summary>
    public static class PathSystem
    {
        // =============================================================================
        // CONSTANTS
        // =============================================================================

        public const float MAX_PATH_LEVEL = 100f;
        public const float PATH_BONUS_PER_LEVEL = 0.005f;  // 0.5% per level, max 50%

        // =============================================================================
        // PATH STAT BONUSES
        // =============================================================================

        /// <summary>
        /// Get primary stat bonus for a path at given level
        /// </summary>
        public static Dictionary<Stat, float> GetPathBonuses(Path path, float pathLevel)
        {
            var bonuses = new Dictionary<Stat, float>();
            float multiplier = Mathf.Clamp(pathLevel * PATH_BONUS_PER_LEVEL, 0f, 0.5f);

            switch (path)
            {
                case Path.IRONBOUND:
                    // Tank path - Defense, HP, Resistance
                    bonuses[Stat.DEFENSE] = multiplier * 1.5f;   // Primary
                    bonuses[Stat.HP] = multiplier * 1.2f;
                    bonuses[Stat.RESISTANCE] = multiplier * 1.0f;
                    break;

                case Path.FANGBORN:
                    // Attacker path - Attack, Speed, Crit
                    bonuses[Stat.ATTACK] = multiplier * 1.5f;    // Primary
                    bonuses[Stat.SPEED] = multiplier * 1.0f;
                    bonuses[Stat.CRIT_RATE] = multiplier * 0.5f;
                    break;

                case Path.VOIDTOUCHED:
                    // Mage path - Magic, MP, Resistance
                    bonuses[Stat.MAGIC] = multiplier * 1.5f;     // Primary
                    bonuses[Stat.MP] = multiplier * 1.2f;
                    bonuses[Stat.RESISTANCE] = multiplier * 0.8f;
                    break;

                case Path.UNCHAINED:
                    // Hybrid path - Balanced bonuses
                    bonuses[Stat.ATTACK] = multiplier * 0.8f;
                    bonuses[Stat.DEFENSE] = multiplier * 0.8f;
                    bonuses[Stat.MAGIC] = multiplier * 0.8f;
                    bonuses[Stat.SPEED] = multiplier * 0.8f;
                    bonuses[Stat.LUCK] = multiplier * 1.0f;      // Primary
                    break;
            }

            return bonuses;
        }

        /// <summary>
        /// Calculate modified stat value with path bonus
        /// </summary>
        public static int ApplyPathBonus(int baseStat, Stat statType, Path path, float pathLevel)
        {
            var bonuses = GetPathBonuses(path, pathLevel);

            if (bonuses.TryGetValue(statType, out float bonus))
            {
                return Mathf.RoundToInt(baseStat * (1f + bonus));
            }

            return baseStat;
        }

        /// <summary>
        /// Get the primary stat for a path
        /// </summary>
        public static Stat GetPrimaryStat(Path path)
        {
            return path switch
            {
                Path.IRONBOUND => Stat.DEFENSE,
                Path.FANGBORN => Stat.ATTACK,
                Path.VOIDTOUCHED => Stat.MAGIC,
                Path.UNCHAINED => Stat.LUCK,
                _ => Stat.HP
            };
        }

        // =============================================================================
        // PATH PROGRESSION
        // =============================================================================

        /// <summary>
        /// Calculate experience needed to reach next path level
        /// </summary>
        public static int GetExpForPathLevel(float currentLevel)
        {
            // Exponential scaling
            return Mathf.RoundToInt(100 * Mathf.Pow(1.15f, currentLevel));
        }

        /// <summary>
        /// Calculate path level from total experience
        /// </summary>
        public static float CalculatePathLevel(int totalExp)
        {
            float level = 0f;
            int expRequired = 100;
            int expSpent = 0;

            while (expSpent + expRequired <= totalExp && level < MAX_PATH_LEVEL)
            {
                expSpent += expRequired;
                level += 1f;
                expRequired = GetExpForPathLevel(level);
            }

            // Calculate partial progress to next level
            if (level < MAX_PATH_LEVEL)
            {
                int remainingExp = totalExp - expSpent;
                level += (float)remainingExp / expRequired;
            }

            return Mathf.Min(level, MAX_PATH_LEVEL);
        }

        // =============================================================================
        // DISPLAY HELPERS
        // =============================================================================

        /// <summary>
        /// Get color associated with path
        /// </summary>
        public static Color GetPathColor(Path path)
        {
            return path switch
            {
                Path.IRONBOUND => new Color(0.6f, 0.6f, 0.7f),    // Steel gray
                Path.FANGBORN => new Color(0.8f, 0.3f, 0.2f),     // Blood red
                Path.VOIDTOUCHED => new Color(0.4f, 0.2f, 0.7f),  // Void purple
                Path.UNCHAINED => new Color(0.9f, 0.7f, 0.2f),    // Wild gold
                _ => Color.white
            };
        }

        /// <summary>
        /// Get display name for path
        /// </summary>
        public static string GetPathName(Path path)
        {
            return path switch
            {
                Path.NONE => "None",
                Path.IRONBOUND => "Ironbound",
                Path.FANGBORN => "Fangborn",
                Path.VOIDTOUCHED => "Voidtouched",
                Path.UNCHAINED => "Unchained",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Get path description
        /// </summary>
        public static string GetPathDescription(Path path)
        {
            return path switch
            {
                Path.IRONBOUND => "The path of iron will. Masters of defense, shields, and endurance.",
                Path.FANGBORN => "The path of the hunter. Masters of offense, speed, and critical strikes.",
                Path.VOIDTOUCHED => "The path of the void. Masters of magic, corruption, and arcane power.",
                Path.UNCHAINED => "The path of freedom. Versatile fighters who excel at adaptability.",
                _ => "No path chosen"
            };
        }

        /// <summary>
        /// Get role description for path
        /// </summary>
        public static string GetPathRole(Path path)
        {
            return path switch
            {
                Path.IRONBOUND => "Tank / Defender",
                Path.FANGBORN => "DPS / Attacker",
                Path.VOIDTOUCHED => "Mage / Caster",
                Path.UNCHAINED => "Hybrid / Wildcard",
                _ => "None"
            };
        }

        /// <summary>
        /// Get recommended brands for a path
        /// </summary>
        public static Brand[] GetRecommendedBrands(Path path)
        {
            return path switch
            {
                Path.IRONBOUND => new[] { Brand.IRON, Brand.BLOODIRON, Brand.LEECH },
                Path.FANGBORN => new[] { Brand.SAVAGE, Brand.RAVENOUS, Brand.CORROSIVE },
                Path.VOIDTOUCHED => new[] { Brand.DREAD, Brand.VENOM, Brand.TERRORFLUX },
                Path.UNCHAINED => new[] { Brand.SURGE, Brand.VENOMSTRIKE, Brand.NIGHTLEECH },
                _ => Array.Empty<Brand>()
            };
        }
    }
}
