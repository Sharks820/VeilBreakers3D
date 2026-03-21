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

        // Pre-computed brand display names to avoid repeated ToString + string manipulation
        private static readonly Dictionary<Brand, string> _brandDisplayNames = new Dictionary<Brand, string>();

        static BrandSystem()
        {
            foreach (Brand brand in Enum.GetValues(typeof(Brand)))
            {
                string name = brand.ToString();
                _brandDisplayNames[brand] = name.Substring(0, 1) + name.Substring(1).ToLower();
            }
        }

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


        // Hybrid brands map to their primary parent brand for effectiveness calculations
        private static readonly Dictionary<Brand, Brand> HybridToParentBrand = new Dictionary<Brand, Brand>
        {
            { Brand.BLOODIRON, Brand.IRON },      // Iron + blood aspects
            { Brand.RAVENOUS, Brand.SAVAGE },     // Savage + hunger aspects
            { Brand.CORROSIVE, Brand.VENOM },     // Venom + decay aspects
            { Brand.TERRORFLUX, Brand.DREAD },    // Dread + chaos aspects
            { Brand.VENOMSTRIKE, Brand.VENOM },   // Venom + strike aspects
            { Brand.NIGHTLEECH, Brand.LEECH }     // Leech + darkness aspects
        };

        /// <summary>
        /// Resolve a brand to its core type (hybrid brands return their parent).
        /// </summary>
        public static Brand GetCoreBrand(Brand brand)
        {
            if (HybridToParentBrand.TryGetValue(brand, out var parent))
                return parent;
            return brand;
        }

        /// <summary>
        /// Check if a brand is a hybrid brand.
        /// </summary>
        public static bool IsHybridBrand(Brand brand)
        {
            return HybridToParentBrand.ContainsKey(brand);
        }

        /// <summary>
        /// Get damage multiplier between attacker and defender brands
        /// </summary>
        public static float GetEffectiveness(Brand attacker, Brand defender)
        {
            if (attacker == Brand.NONE || defender == Brand.NONE)
                return NEUTRAL;

            // Resolve hybrid brands to their parent brands for effectiveness lookup
            var coreAttacker = GetCoreBrand(attacker);
            var coreDefender = GetCoreBrand(defender);

            if (!EffectivenessMatrix.TryGetValue(coreAttacker, out var matrix))
                return NEUTRAL;

            foreach (var strong in matrix.strong)
            {
                if (strong == coreDefender) return SUPER_EFFECTIVE;
            }

            foreach (var weak in matrix.weak)
            {
                if (weak == coreDefender) return NOT_EFFECTIVE;
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

        // Pre-computed brand colors — must match ThemeManager.InitializeColors() primary values
        private static readonly Dictionary<Brand, Color> _brandColors = new Dictionary<Brand, Color>
        {
            { Brand.IRON,    new Color(0.55f, 0.59f, 0.65f) },   // Steel gray
            { Brand.SAVAGE,  new Color(0.71f, 0.18f, 0.18f) },   // Blood red
            { Brand.SURGE,   new Color(0.24f, 0.55f, 0.86f) },   // Electric blue
            { Brand.VENOM,   new Color(0.31f, 0.71f, 0.24f) },   // Toxic green
            { Brand.DREAD,   new Color(0.47f, 0.24f, 0.63f) },   // Deep purple
            { Brand.LEECH,   new Color(0.55f, 0.16f, 0.31f) },   // Dark crimson
            { Brand.GRACE,   new Color(0.86f, 0.86f, 0.94f) },   // Holy silver
            { Brand.MEND,    new Color(0.78f, 0.67f, 0.31f) },   // Healing gold
            { Brand.RUIN,    new Color(0.86f, 0.47f, 0.16f) },   // Flame orange
            { Brand.VOID,    new Color(0.16f, 0.08f, 0.24f) },   // Void dark
        };

        /// <summary>
        /// Get brand color for UI. Self-contained lookup (no UI layer dependency).
        /// </summary>
        public static Color GetBrandColor(Brand brand)
        {
            var coreBrand = GetCoreBrand(brand);
            return _brandColors.TryGetValue(coreBrand, out var color) ? color : Color.white;
        }

        /// <summary>
        /// Get brand display name
        /// </summary>
        public static string GetBrandName(Brand brand)
        {
            return _brandDisplayNames.TryGetValue(brand, out string name) ? name : brand.ToString();
        }

        /// <summary>
        /// Get brand archetype description
        /// </summary>
        public static string GetBrandArchetype(Brand brand)
        {
            // Handle hybrid brands with specific archetypes
            if (IsHybridBrand(brand))
            {
                return brand switch
                {
                    Brand.BLOODIRON => "Blood Knight",
                    Brand.RAVENOUS => "Devourer",
                    Brand.CORROSIVE => "Decay Bringer",
                    Brand.TERRORFLUX => "Nightmare Weaver",
                    Brand.VENOMSTRIKE => "Toxic Assassin",
                    Brand.NIGHTLEECH => "Shadow Drainer",
                    _ => "Unknown"
                };
            }
            
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
