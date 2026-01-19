using UnityEngine;
using VeilBreakers.Combat;
using VeilBreakers.Data;
using VeilBreakers.Systems;

namespace VeilBreakers.Capture
{
    /// <summary>
    /// Calculates the bind threshold for monsters based on various factors.
    /// Lower threshold = harder to bind (must reduce HP more).
    /// Higher threshold = easier to bind (binds at higher HP).
    /// </summary>
    public static class BindThresholdCalculator
    {
        // =============================================================================
        // MAIN CALCULATION
        // =============================================================================

        /// <summary>
        /// Calculate the bind threshold HP percentage for a monster.
        /// </summary>
        /// <param name="target">Monster being bound</param>
        /// <param name="attacker">Ally attempting to bind (for brand/level checks)</param>
        /// <returns>HP percentage (0-1) at which monster can be bound</returns>
        public static float CalculateThreshold(Combatant target, Combatant attacker)
        {
            if (target == null) return BindThresholdConfig.BASE_THRESHOLD;

            float threshold = BindThresholdConfig.BASE_THRESHOLD;

            // Apply corruption modifier
            threshold += GetCorruptionModifier(target.Corruption);

            // Apply rarity modifier
            threshold += GetRarityModifier(target.Rarity);

            // Apply speed modifier (if monster has high speed)
            threshold += GetSpeedModifier(target);

            // Apply brand advantage modifier (if attacker provided)
            if (attacker != null)
            {
                threshold += GetBrandModifier(target.PrimaryBrand, attacker.PrimaryBrand);

                // Apply intimidation bonus
                threshold += GetIntimidationModifier(target, attacker);
            }

            // Clamp to valid range
            return Mathf.Clamp(threshold, BindThresholdConfig.MIN_THRESHOLD, BindThresholdConfig.MAX_THRESHOLD);
        }

        /// <summary>
        /// Quick check if a monster can currently be bound.
        /// </summary>
        public static bool CanBind(Combatant target, Combatant attacker)
        {
            if (target == null || !target.IsAlive) return false;

            float threshold = CalculateThreshold(target, attacker);
            float currentHPPercent = target.CurrentHP / (float)target.MaxHP;

            return currentHPPercent <= threshold;
        }

        // =============================================================================
        // INDIVIDUAL MODIFIERS
        // =============================================================================

        /// <summary>
        /// Get threshold modifier based on corruption level.
        /// Low corruption = easier to bind (+bonus).
        /// High corruption = harder to bind (-penalty).
        /// </summary>
        private static float GetCorruptionModifier(float corruption)
        {
            // ASCENDED/Purified (0-25%): +15% threshold
            if (corruption <= 25f)
            {
                return BindThresholdConfig.LOW_CORRUPTION_BONUS;
            }
            // Unstable (26-50%): No modifier
            if (corruption <= 50f)
            {
                return 0f;
            }
            // Corrupted/Abyssal (51-100%): -15% threshold
            return BindThresholdConfig.HIGH_CORRUPTION_PENALTY;
        }

        /// <summary>
        /// Get threshold modifier based on monster rarity.
        /// Higher rarity = harder to bind.
        /// </summary>
        private static float GetRarityModifier(MonsterRarity rarity)
        {
            int index = (int)rarity;
            if (index >= 0 && index < BindThresholdConfig.RarityModifiers.Length)
            {
                return BindThresholdConfig.RarityModifiers[index];
            }
            return 0f;
        }

        /// <summary>
        /// Get threshold modifier based on monster speed.
        /// Very fast monsters are harder to pin down.
        /// </summary>
        private static float GetSpeedModifier(Combatant target)
        {
            // Base speed threshold for "high speed"
            const int HIGH_SPEED_THRESHOLD = 80;
            const int SPEED_STEP = 10;

            if (target.Speed <= HIGH_SPEED_THRESHOLD)
            {
                return 0f;
            }

            // -5% per 10 speed above threshold
            int stepsAbove = (target.Speed - HIGH_SPEED_THRESHOLD) / SPEED_STEP;
            return stepsAbove * BindThresholdConfig.HIGH_SPEED_PENALTY;
        }

        /// <summary>
        /// Get threshold modifier based on brand matchup.
        /// Weak against attacker = easier to bind.
        /// Strong against attacker = harder to bind.
        /// </summary>
        private static float GetBrandModifier(Brand targetBrand, Brand attackerBrand)
        {
            float effectiveness = BrandSystem.GetEffectiveness(attackerBrand, targetBrand);

            // Attacker strong against target (2x) = easier bind
            if (effectiveness >= 2f)
            {
                return BindThresholdConfig.BRAND_WEAK_BONUS;
            }
            // Attacker weak against target (0.5x) = harder bind
            if (effectiveness <= 0.5f)
            {
                return BindThresholdConfig.BRAND_STRONG_PENALTY;
            }
            // Neutral
            return 0f;
        }

        /// <summary>
        /// Get threshold modifier based on intimidation.
        /// Significantly stronger attacker intimidates target.
        /// </summary>
        private static float GetIntimidationModifier(Combatant target, Combatant attacker)
        {
            if (IsIntimidated(target, attacker))
            {
                return BindThresholdConfig.INTIMIDATION_BONUS;
            }
            return 0f;
        }

        /// <summary>
        /// Check if attacker intimidates target.
        /// </summary>
        public static bool IsIntimidated(Combatant target, Combatant attacker)
        {
            if (target == null || attacker == null) return false;

            // Level difference (5+ levels higher)
            bool levelAdvantage = attacker.Level >= target.Level + 5;

            // Rarity difference (attacker is higher rarity)
            bool rarityAdvantage = attacker.Rarity > target.Rarity;

            // Strong brand advantage
            float brandEffectiveness = BrandSystem.GetEffectiveness(attacker.PrimaryBrand, target.PrimaryBrand);
            bool brandAdvantage = brandEffectiveness >= 2f;

            // Need at least 2 of 3 advantages for intimidation
            int advantages = 0;
            if (levelAdvantage) advantages++;
            if (rarityAdvantage) advantages++;
            if (brandAdvantage) advantages++;

            return advantages >= 2;
        }

        // =============================================================================
        // DEBUG/UTILITY
        // =============================================================================

        /// <summary>
        /// Get a detailed breakdown of threshold calculation.
        /// </summary>
        public static string GetThresholdBreakdown(Combatant target, Combatant attacker)
        {
            if (target == null) return "Invalid target";

            float baseThreshold = BindThresholdConfig.BASE_THRESHOLD;
            float corruptionMod = GetCorruptionModifier(target.Corruption);
            float rarityMod = GetRarityModifier(target.Rarity);
            float speedMod = GetSpeedModifier(target);
            float brandMod = attacker != null ? GetBrandModifier(target.PrimaryBrand, attacker.PrimaryBrand) : 0f;
            float intimidationMod = attacker != null ? GetIntimidationModifier(target, attacker) : 0f;

            float total = CalculateThreshold(target, attacker);

            return $"Bind Threshold Breakdown for {target.DisplayName}:\n" +
                   $"  Base: {baseThreshold:P0}\n" +
                   $"  Corruption ({target.Corruption}%): {corruptionMod:+0%;-0%}\n" +
                   $"  Rarity ({target.Rarity}): {rarityMod:+0%;-0%}\n" +
                   $"  Speed ({target.Speed}): {speedMod:+0%;-0%}\n" +
                   $"  Brand: {brandMod:+0%;-0%}\n" +
                   $"  Intimidation: {intimidationMod:+0%;-0%}\n" +
                   $"  ─────────────────\n" +
                   $"  TOTAL: {total:P0}";
        }
    }
}
