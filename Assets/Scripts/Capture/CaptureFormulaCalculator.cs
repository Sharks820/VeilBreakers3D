using UnityEngine;
using VeilBreakers.Data;

namespace VeilBreakers.Capture
{
    /// <summary>
    /// Calculates final capture chance based on all factors.
    /// </summary>
    public static class CaptureFormulaCalculator
    {
        // =============================================================================
        // MAIN CALCULATION
        // =============================================================================

        /// <summary>
        /// Calculate the final capture chance with full breakdown.
        /// </summary>
        public static CaptureCalculationResult Calculate(
            BoundMonsterData monster,
            CaptureItem item,
            int playerLevel,
            QTEResult qteResult)
        {
            var result = new CaptureCalculationResult();

            // Get item effectiveness as base (already includes rarity factor)
            result.itemModifier = GetItemModifier(item, monster.rarity);

            // HP modifier (lower HP at bind = bonus)
            result.hpModifier = GetHPModifier(monster.boundAtHPPercent);

            // Corruption modifier
            result.corruptionModifier = GetCorruptionModifier(monster.currentCorruption);

            // Rarity modifier (applied on top of item effectiveness)
            result.rarityModifier = GetRarityModifier(monster.rarity);

            // Level difference modifier
            result.levelModifier = GetLevelModifier(playerLevel, monster.monsterLevel);

            // QTE bonus
            result.qteModifier = GetQTEModifier(qteResult);

            // Calculate final chance
            // Item modifier is the BASE (already factored for rarity)
            // Other modifiers are ADDITIVE bonuses/penalties
            result.baseChance = result.itemModifier / 100f;

            float totalModifiers = result.hpModifier + result.corruptionModifier +
                                   result.levelModifier + result.qteModifier;

            result.finalChance = result.baseChance + totalModifiers;

            // Clamp to valid range
            result.finalChance = Mathf.Clamp(
                result.finalChance,
                CaptureFormulaConfig.MIN_CHANCE,
                CaptureFormulaConfig.MAX_CHANCE
            );

            return result;
        }

        /// <summary>
        /// Quick calculation without breakdown.
        /// </summary>
        public static float CalculateQuick(
            BoundMonsterData monster,
            CaptureItem item,
            int playerLevel,
            QTEResult qteResult)
        {
            return Calculate(monster, item, playerLevel, qteResult).finalChance;
        }

        // =============================================================================
        // INDIVIDUAL MODIFIERS
        // =============================================================================

        /// <summary>
        /// Get item effectiveness as percentage (already accounts for rarity).
        /// </summary>
        private static float GetItemModifier(CaptureItem item, MonsterRarity rarity)
        {
            return CaptureItemConfig.GetEffectiveness(item, rarity);
        }

        /// <summary>
        /// Get HP modifier based on HP% at which monster was bound.
        /// Lower HP = better bonus.
        /// </summary>
        private static float GetHPModifier(float hpPercent)
        {
            if (hpPercent <= 0.10f) return CaptureFormulaConfig.HPBonuses[0];      // 5-10%: +15%
            if (hpPercent <= 0.20f) return CaptureFormulaConfig.HPBonuses[1];      // 11-20%: +10%
            if (hpPercent <= 0.30f) return CaptureFormulaConfig.HPBonuses[2];      // 21-30%: +5%
            if (hpPercent <= 0.40f) return CaptureFormulaConfig.HPBonuses[3];      // 31-40%: +0%
            return CaptureFormulaConfig.HPBonuses[4];                              // 41-50%: -5%
        }

        /// <summary>
        /// Get corruption modifier.
        /// Low corruption = cooperative = bonus.
        /// High corruption = chaotic = penalty.
        /// </summary>
        private static float GetCorruptionModifier(float corruption)
        {
            if (corruption <= 10f) return CaptureFormulaConfig.ASCENDED_BONUS;     // ASCENDED
            if (corruption <= 25f) return CaptureFormulaConfig.PURIFIED_BONUS;     // Purified
            if (corruption <= 50f) return CaptureFormulaConfig.UNSTABLE_BONUS;     // Unstable
            if (corruption <= 75f) return CaptureFormulaConfig.CORRUPTED_PENALTY;  // Corrupted
            return CaptureFormulaConfig.ABYSSAL_PENALTY;                           // Abyssal
        }

        /// <summary>
        /// Get rarity modifier (additional penalty beyond item effectiveness).
        /// Note: Most of rarity factor is already in item effectiveness.
        /// This is minimal additional penalty for formula balancing.
        /// </summary>
        private static float GetRarityModifier(MonsterRarity rarity)
        {
            // Rarity is already heavily factored into item effectiveness
            // This is kept minimal to avoid double-penalizing
            return 0f;
        }

        /// <summary>
        /// Get level difference modifier.
        /// Higher player level = bonus.
        /// Lower player level = penalty.
        /// </summary>
        private static float GetLevelModifier(int playerLevel, int monsterLevel)
        {
            int difference = playerLevel - monsterLevel;

            if (difference > 0)
            {
                // Player higher level - bonus
                float bonus = difference * CaptureFormulaConfig.PER_LEVEL_BELOW_BONUS;
                return Mathf.Min(bonus, CaptureFormulaConfig.MAX_LEVEL_BONUS);
            }
            else if (difference < 0)
            {
                // Player lower level - penalty
                float penalty = difference * CaptureFormulaConfig.PER_LEVEL_ABOVE_PENALTY;
                return Mathf.Max(penalty, CaptureFormulaConfig.MAX_LEVEL_PENALTY);
            }

            return 0f;
        }

        /// <summary>
        /// Get QTE bonus based on timing result.
        /// </summary>
        private static float GetQTEModifier(QTEResult result)
        {
            return result switch
            {
                QTEResult.PERFECT => CaptureFormulaConfig.QTE_PERFECT_BONUS,
                QTEResult.GOOD => CaptureFormulaConfig.QTE_GOOD_BONUS,
                QTEResult.OKAY => CaptureFormulaConfig.QTE_OKAY_BONUS,
                _ => CaptureFormulaConfig.QTE_MISS_BONUS
            };
        }

        // =============================================================================
        // FAILURE OUTCOME CALCULATION
        // =============================================================================

        /// <summary>
        /// Determine failure outcome (Flee vs Berserk).
        /// </summary>
        public static CaptureOutcome DetermineFailureOutcome(BoundMonsterData monster, int playerLevel)
        {
            float berserkChance = GetBerserkChance(monster, playerLevel);

            // Random roll
            float roll = Random.value;
            return roll < berserkChance ? CaptureOutcome.BERSERK : CaptureOutcome.FLEE;
        }

        /// <summary>
        /// Get the chance of berserk (vs flee) on capture failure.
        /// </summary>
        public static float GetBerserkChance(BoundMonsterData monster, int playerLevel)
        {
            // Base berserk chance based on corruption
            float baseBerserk;
            if (monster.currentCorruption <= 25f)
            {
                baseBerserk = CaptureFailureConfig.BerserkChance[0]; // Low: 30%
            }
            else if (monster.currentCorruption <= 50f)
            {
                baseBerserk = CaptureFailureConfig.BerserkChance[1]; // Mid: 50%
            }
            else
            {
                baseBerserk = CaptureFailureConfig.BerserkChance[2]; // High: 70%
            }

            // Modifiers for high rarity
            if (monster.rarity >= MonsterRarity.EPIC)
            {
                baseBerserk += CaptureFailureConfig.EPIC_LEGENDARY_BERSERK_BONUS;
            }

            // Modifiers for high level difference
            int levelDiff = monster.monsterLevel - playerLevel;
            if (levelDiff >= CaptureFailureConfig.HIGH_LEVEL_THRESHOLD)
            {
                baseBerserk += CaptureFailureConfig.HIGH_LEVEL_BERSERK_BONUS;
            }

            return Mathf.Clamp01(baseBerserk);
        }

        /// <summary>
        /// Get the damage buff multiplier for a berserk monster.
        /// </summary>
        public static float GetBerserkDamageBuff()
        {
            return Random.Range(
                CaptureFailureConfig.BERSERK_DAMAGE_BUFF_MIN,
                CaptureFailureConfig.BERSERK_DAMAGE_BUFF_MAX
            );
        }

        // =============================================================================
        // PREVIEW CALCULATIONS
        // =============================================================================

        /// <summary>
        /// Preview capture chance before QTE (for UI display).
        /// </summary>
        public static CaptureCalculationResult PreviewChance(
            BoundMonsterData monster,
            CaptureItem item,
            int playerLevel)
        {
            // Calculate with no QTE bonus for preview
            var result = Calculate(monster, item, playerLevel, QTEResult.MISS);

            // Show potential range with QTE
            return result;
        }

        /// <summary>
        /// Get the potential range of capture chance (min-max with QTE).
        /// </summary>
        public static (float min, float max) GetChanceRange(
            BoundMonsterData monster,
            CaptureItem item,
            int playerLevel)
        {
            float withoutQTE = CalculateQuick(monster, item, playerLevel, QTEResult.MISS);
            float withPerfectQTE = CalculateQuick(monster, item, playerLevel, QTEResult.PERFECT);

            return (withoutQTE, withPerfectQTE);
        }

        /// <summary>
        /// Check if an item is effective against this monster rarity.
        /// </summary>
        public static bool IsItemEffective(CaptureItem item, MonsterRarity rarity)
        {
            return CaptureItemConfig.GetEffectiveness(item, rarity) > 0;
        }
    }
}
