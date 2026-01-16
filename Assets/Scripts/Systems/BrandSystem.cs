using System;
using System.Collections.Generic;
using UnityEngine;
using VeilBreakers.Data;

namespace VeilBreakers.Systems
{
    /// <summary>
    /// Brand System - Handles all brand effectiveness calculations
    /// 12 Brands: 6 Pure + 6 Hybrid
    /// Effectiveness wheel: SAVAGE > IRON > VENOM > SURGE > DREAD > LEECH > SAVAGE
    /// </summary>
    public static class BrandSystem
    {
        // =============================================================================
        // CONSTANTS
        // =============================================================================

        public const float SUPER_EFFECTIVE = 1.5f;
        public const float NOT_EFFECTIVE = 0.5f;
        public const float NEUTRAL = 1.0f;
        public const float IMMUNE = 0.0f;

        // =============================================================================
        // EFFECTIVENESS LOOKUP
        // =============================================================================

        /// <summary>
        /// Brand effectiveness wheel (who beats who)
        /// SAVAGE > IRON > VENOM > SURGE > DREAD > LEECH > SAVAGE
        /// </summary>
        private static readonly Dictionary<Brand, Brand> BrandStrength = new Dictionary<Brand, Brand>
        {
            { Brand.SAVAGE, Brand.IRON },
            { Brand.IRON, Brand.VENOM },
            { Brand.VENOM, Brand.SURGE },
            { Brand.SURGE, Brand.DREAD },
            { Brand.DREAD, Brand.LEECH },
            { Brand.LEECH, Brand.SAVAGE }
        };

        private static readonly Dictionary<Brand, Brand> BrandWeakness = new Dictionary<Brand, Brand>
        {
            { Brand.SAVAGE, Brand.LEECH },
            { Brand.IRON, Brand.SAVAGE },
            { Brand.VENOM, Brand.IRON },
            { Brand.SURGE, Brand.VENOM },
            { Brand.DREAD, Brand.SURGE },
            { Brand.LEECH, Brand.DREAD }
        };

        /// <summary>
        /// Hybrid brand composition (Primary + Secondary)
        /// </summary>
        private static readonly Dictionary<Brand, (Brand primary, Brand secondary)> HybridComposition =
            new Dictionary<Brand, (Brand, Brand)>
        {
            { Brand.BLOODIRON, (Brand.SAVAGE, Brand.IRON) },
            { Brand.CORROSIVE, (Brand.SAVAGE, Brand.VENOM) },
            { Brand.VENOMSTRIKE, (Brand.VENOM, Brand.SURGE) },
            { Brand.TERRORFLUX, (Brand.DREAD, Brand.SURGE) },
            { Brand.NIGHTLEECH, (Brand.DREAD, Brand.LEECH) },
            { Brand.RAVENOUS, (Brand.LEECH, Brand.SAVAGE) }
        };

        // =============================================================================
        // EFFECTIVENESS CALCULATIONS
        // =============================================================================

        /// <summary>
        /// Calculate damage multiplier between attacker and defender brands
        /// </summary>
        public static float GetEffectiveness(Brand attacker, Brand defender)
        {
            // None or Primal have neutral effectiveness
            if (attacker == Brand.NONE || defender == Brand.NONE ||
                attacker == Brand.PRIMAL || defender == Brand.PRIMAL)
            {
                return NEUTRAL;
            }

            // Get base brands for hybrids
            Brand attackerBase = GetPrimaryComponent(attacker);
            Brand defenderBase = GetPrimaryComponent(defender);

            // Check if attacker is strong against defender
            if (BrandStrength.TryGetValue(attackerBase, out Brand strongAgainst))
            {
                if (strongAgainst == defenderBase)
                {
                    return SUPER_EFFECTIVE;
                }
            }

            // Check if attacker is weak against defender
            if (BrandWeakness.TryGetValue(attackerBase, out Brand weakAgainst))
            {
                if (weakAgainst == defenderBase)
                {
                    return NOT_EFFECTIVE;
                }
            }

            return NEUTRAL;
        }

        /// <summary>
        /// Get the primary component of a brand (self for pure, primary for hybrid)
        /// </summary>
        public static Brand GetPrimaryComponent(Brand brand)
        {
            if (HybridComposition.TryGetValue(brand, out var composition))
            {
                return composition.primary;
            }
            return brand;
        }

        /// <summary>
        /// Get the secondary component of a hybrid brand (NONE for pure)
        /// </summary>
        public static Brand GetSecondaryComponent(Brand brand)
        {
            if (HybridComposition.TryGetValue(brand, out var composition))
            {
                return composition.secondary;
            }
            return Brand.NONE;
        }

        /// <summary>
        /// Check if brand is a pure (single) brand
        /// </summary>
        public static bool IsPureBrand(Brand brand)
        {
            return brand >= Brand.SAVAGE && brand <= Brand.LEECH;
        }

        /// <summary>
        /// Check if brand is a hybrid brand
        /// </summary>
        public static bool IsHybridBrand(Brand brand)
        {
            return HybridComposition.ContainsKey(brand);
        }

        /// <summary>
        /// Get brand tier
        /// </summary>
        public static BrandTier GetBrandTier(Brand brand)
        {
            if (brand == Brand.PRIMAL)
                return BrandTier.PRIMAL;
            if (IsHybridBrand(brand))
                return BrandTier.HYBRID;
            return BrandTier.PURE;
        }

        // =============================================================================
        // DISPLAY HELPERS
        // =============================================================================

        /// <summary>
        /// Get color associated with brand
        /// </summary>
        public static Color GetBrandColor(Brand brand)
        {
            return brand switch
            {
                Brand.SAVAGE => new Color(0.8f, 0.2f, 0.2f),      // Red
                Brand.IRON => new Color(0.7f, 0.7f, 0.75f),       // Silver
                Brand.VENOM => new Color(0.2f, 0.7f, 0.3f),       // Green
                Brand.SURGE => new Color(0.2f, 0.5f, 0.9f),       // Blue
                Brand.DREAD => new Color(0.5f, 0.2f, 0.7f),       // Purple
                Brand.LEECH => new Color(0.7f, 0.1f, 0.2f),       // Crimson
                Brand.BLOODIRON => new Color(0.75f, 0.35f, 0.35f),
                Brand.CORROSIVE => new Color(0.5f, 0.45f, 0.25f),
                Brand.VENOMSTRIKE => new Color(0.2f, 0.6f, 0.6f),
                Brand.TERRORFLUX => new Color(0.4f, 0.35f, 0.8f),
                Brand.NIGHTLEECH => new Color(0.5f, 0.15f, 0.4f),
                Brand.RAVENOUS => new Color(0.75f, 0.15f, 0.15f),
                Brand.PRIMAL => new Color(1f, 0.85f, 0.4f),       // Gold
                _ => Color.white
            };
        }

        /// <summary>
        /// Get display name for brand
        /// </summary>
        public static string GetBrandName(Brand brand)
        {
            return brand switch
            {
                Brand.NONE => "None",
                Brand.SAVAGE => "Savage",
                Brand.IRON => "Iron",
                Brand.VENOM => "Venom",
                Brand.SURGE => "Surge",
                Brand.DREAD => "Dread",
                Brand.LEECH => "Leech",
                Brand.BLOODIRON => "Bloodiron",
                Brand.CORROSIVE => "Corrosive",
                Brand.VENOMSTRIKE => "Venomstrike",
                Brand.TERRORFLUX => "Terrorflux",
                Brand.NIGHTLEECH => "Nightleech",
                Brand.RAVENOUS => "Ravenous",
                Brand.PRIMAL => "Primal",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Get brand description
        /// </summary>
        public static string GetBrandDescription(Brand brand)
        {
            return brand switch
            {
                Brand.SAVAGE => "Raw damage, berserk rage, critical strikes",
                Brand.IRON => "Unyielding defense, armor, resilience",
                Brand.VENOM => "Poison, decay, damage over time",
                Brand.SURGE => "Lightning speed, tempo control",
                Brand.DREAD => "Fear, debuffs, battlefield control",
                Brand.LEECH => "Lifesteal, drain, sustain",
                Brand.BLOODIRON => "Tank that rages (Savage + Iron)",
                Brand.CORROSIVE => "Poison rampage (Savage + Venom)",
                Brand.VENOMSTRIKE => "Fast poison (Venom + Surge)",
                Brand.TERRORFLUX => "Lightning fear (Dread + Surge)",
                Brand.NIGHTLEECH => "Terrifying drain (Dread + Leech)",
                Brand.RAVENOUS => "Hungry berserker (Leech + Savage)",
                Brand.PRIMAL => "Ascended power - 0% corruption achieved",
                _ => "Unknown brand"
            };
        }

        /// <summary>
        /// Get effectiveness description string
        /// </summary>
        public static string GetEffectivenessText(float multiplier)
        {
            if (multiplier >= SUPER_EFFECTIVE)
                return "Super Effective!";
            if (multiplier <= NOT_EFFECTIVE && multiplier > IMMUNE)
                return "Not Very Effective...";
            if (multiplier <= IMMUNE)
                return "No Effect!";
            return "";
        }
    }
}
