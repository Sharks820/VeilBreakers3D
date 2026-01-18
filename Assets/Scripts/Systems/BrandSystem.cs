using System;
using System.Collections.Generic;
using UnityEngine;
using VeilBreakers.Data;

namespace VeilBreakers.Systems
{
    /// <summary>
    /// Brand System - 10 Brands with 2x/0.5x effectiveness matrix
    /// Each brand is strong against 2, weak against 2, neutral against 6
    /// </summary>
    public static class BrandSystem
    {
        public const float SUPER_EFFECTIVE = 2.0f;
        public const float NOT_EFFECTIVE = 0.5f;
        public const float NEUTRAL = 1.0f;

        // Effectiveness matrix: Attacker -> (Strong against, Weak against)
        private static readonly Dictionary<Brand, (Brand[] strong, Brand[] weak)> EffectivenessMatrix =
            new Dictionary<Brand, (Brand[], Brand[])>
        {
            { Brand.IRON,   (new[] { Brand.SURGE, Brand.DREAD }, new[] { Brand.SAVAGE, Brand.RUIN }) },
            { Brand.SAVAGE, (new[] { Brand.IRON, Brand.MEND },   new[] { Brand.LEECH, Brand.GRACE }) },
            { Brand.SURGE,  (new[] { Brand.VENOM, Brand.LEECH }, new[] { Brand.IRON, Brand.VOID }) },
            { Brand.VENOM,  (new[] { Brand.GRACE, Brand.MEND },  new[] { Brand.SURGE, Brand.RUIN }) },
            { Brand.DREAD,  (new[] { Brand.SAVAGE, Brand.GRACE },new[] { Brand.IRON, Brand.VOID }) },
            { Brand.LEECH,  (new[] { Brand.SAVAGE, Brand.RUIN }, new[] { Brand.SURGE, Brand.VENOM }) },
            { Brand.GRACE,  (new[] { Brand.VOID, Brand.RUIN },   new[] { Brand.SAVAGE, Brand.VENOM }) },
            { Brand.MEND,   (new[] { Brand.VOID, Brand.LEECH },  new[] { Brand.SAVAGE, Brand.VENOM }) },
            { Brand.RUIN,   (new[] { Brand.IRON, Brand.VENOM },  new[] { Brand.LEECH, Brand.GRACE }) },
            { Brand.VOID,   (new[] { Brand.SURGE, Brand.DREAD }, new[] { Brand.GRACE, Brand.MEND }) }
        };

        /// <summary>
        /// Get damage multiplier between attacker and defender brands
        /// </summary>
        public static float GetEffectiveness(Brand attacker, Brand defender)
        {
            if (attacker == Brand.NONE || defender == Brand.NONE)
                return NEUTRAL;

            if (!EffectivenessMatrix.TryGetValue(attacker, out var matrix))
                return NEUTRAL;

            foreach (var strong in matrix.strong)
            {
                if (strong == defender) return SUPER_EFFECTIVE;
            }

            foreach (var weak in matrix.weak)
            {
                if (weak == defender) return NOT_EFFECTIVE;
            }

            return NEUTRAL;
        }

        /// <summary>
        /// Check if attacker has advantage over defender
        /// </summary>
        public static bool HasAdvantage(Brand attacker, Brand defender)
        {
            return GetEffectiveness(attacker, defender) >= SUPER_EFFECTIVE;
        }

        /// <summary>
        /// Check if attacker has disadvantage against defender
        /// </summary>
        public static bool HasDisadvantage(Brand attacker, Brand defender)
        {
            return GetEffectiveness(attacker, defender) <= NOT_EFFECTIVE;
        }

        /// <summary>
        /// Get brand color for UI
        /// </summary>
        public static Color GetBrandColor(Brand brand)
        {
            return brand switch
            {
                Brand.IRON =>   new Color(0.6f, 0.6f, 0.7f),    // Steel gray
                Brand.SAVAGE => new Color(0.9f, 0.2f, 0.1f),    // Blood red
                Brand.SURGE =>  new Color(0.2f, 0.6f, 0.95f),   // Electric blue
                Brand.VENOM =>  new Color(0.3f, 0.8f, 0.2f),    // Toxic green
                Brand.DREAD =>  new Color(0.5f, 0.2f, 0.6f),    // Dark purple
                Brand.LEECH =>  new Color(0.6f, 0.1f, 0.3f),    // Crimson
                Brand.GRACE =>  new Color(1f, 0.95f, 0.7f),     // Warm gold
                Brand.MEND =>   new Color(0.4f, 0.9f, 0.9f),    // Cyan
                Brand.RUIN =>   new Color(0.95f, 0.5f, 0.1f),   // Orange flame
                Brand.VOID =>   new Color(0.2f, 0.1f, 0.3f),    // Deep void
                _ => Color.white
            };
        }

        /// <summary>
        /// Get brand display name
        /// </summary>
        public static string GetBrandName(Brand brand)
        {
            return brand.ToString().Substring(0, 1) + brand.ToString().Substring(1).ToLower();
        }

        /// <summary>
        /// Get brand archetype description
        /// </summary>
        public static string GetBrandArchetype(Brand brand)
        {
            return brand switch
            {
                Brand.IRON =>   "Defensive Wall",
                Brand.SAVAGE => "Berserker",
                Brand.SURGE =>  "Artillery",
                Brand.VENOM =>  "Poison Master",
                Brand.DREAD =>  "Fear Mage",
                Brand.LEECH =>  "Lifesteal Bruiser",
                Brand.GRACE =>  "Combat Medic",
                Brand.MEND =>   "Shield Support",
                Brand.RUIN =>   "Explosion Mage",
                Brand.VOID =>   "Reality Warper",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Get effectiveness text for UI
        /// </summary>
        public static string GetEffectivenessText(float multiplier)
        {
            if (multiplier >= SUPER_EFFECTIVE) return "Super Effective! (2x)";
            if (multiplier <= NOT_EFFECTIVE) return "Not Very Effective... (0.5x)";
            return "";
        }
    }
}
