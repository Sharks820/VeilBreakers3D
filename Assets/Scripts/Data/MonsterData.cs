using System;
using System.Collections.Generic;
using UnityEngine;

namespace VeilBreakers.Data
{
    /// <summary>
    /// Monster data structure - loaded from JSON
    /// Represents all stats, skills, and metadata for a monster type
    /// </summary>
    [Serializable]
    public class MonsterData
    {
        // =============================================================================
        // IDENTITY
        // =============================================================================

        public string monster_id;
        public string display_name;
        public string description;
        public int tier;
        public int brand;
        public int[] brands;  // Array for multi-brand monsters
        public int rarity;

        // =============================================================================
        // BRAND CONFIGURATION
        // =============================================================================

        public int brand_tier;
        public int secondary_brand;
        public int evolution_stage;

        // =============================================================================
        // VISUALS
        // =============================================================================

        public string sprite_path;
        public string portrait_path;
        public ColorData color_palette;

        // =============================================================================
        // BASE STATS
        // =============================================================================

        public int base_hp;
        public int base_mp;
        public int base_attack;
        public int base_defense;
        public int base_magic;
        public int base_resistance;
        public int base_speed;
        public int base_luck;

        // =============================================================================
        // GROWTH RATES (multipliers per level)
        // =============================================================================

        public float hp_growth;
        public float mp_growth;
        public float attack_growth;
        public float defense_growth;
        public float magic_growth;
        public float resistance_growth;
        public float speed_growth;

        // =============================================================================
        // SKILLS
        // =============================================================================

        public string[] innate_skills;
        public List<LearnableSkillEntry> learnable_skills_list;  // Serializable list instead of Dictionary

        // =============================================================================
        // AI CONFIGURATION
        // =============================================================================

        public string ai_pattern;
        public List<SkillWeightEntry> skill_weights_list;  // Serializable list instead of Dictionary

        // =============================================================================
        // CORRUPTION
        // =============================================================================

        public float base_corruption;
        public float corruption_resistance;

        // =============================================================================
        // REWARDS
        // =============================================================================

        public int base_experience;
        public int base_currency;
        public List<DropEntry> drop_table;

        // =============================================================================
        // LORE
        // =============================================================================

        public string habitat;
        public string behavior_notes;
        public string purification_hint;

        // =============================================================================
        // HELPER METHODS
        // =============================================================================

        public Brand GetPrimaryBrand()
        {
            return Enum.IsDefined(typeof(Brand), brand) ? (Brand)brand : Brand.NONE;
        }

        public Brand GetSecondaryBrand()
        {
            return Enum.IsDefined(typeof(Brand), secondary_brand) ? (Brand)secondary_brand : Brand.NONE;
        }

        public BrandTier GetBrandTier()
        {
            return Enum.IsDefined(typeof(BrandTier), brand_tier) ? (BrandTier)brand_tier : BrandTier.MINOR;
        }

#pragma warning disable CS0618 // Rarity is obsolete but still in use
        public Rarity GetRarity()
        {
            return Enum.IsDefined(typeof(Rarity), rarity) ? (Rarity)rarity : Rarity.COMMON;
        }
#pragma warning restore CS0618

        public AIPattern GetAIPattern()
        {
            return ai_pattern switch
            {
                "aggressive" => AIPattern.AGGRESSIVE,
                "defensive" => AIPattern.DEFENSIVE,
                "support" => AIPattern.SUPPORT,
                "balanced" => AIPattern.BALANCED,
                "berserker" => AIPattern.BERSERKER,
                "opportunist" => AIPattern.OPPORTUNIST,
                _ => AIPattern.BALANCED
            };
        }

        /// <summary>
        /// Calculate stat at given level
        /// </summary>
        public int GetStatAtLevel(Stat stat, int level)
        {
            // Validate level input (minimum 1, maximum 100 for safety)
            level = Mathf.Clamp(level, 1, 100);

            int baseStat = stat switch
            {
                Stat.HP => base_hp,
                Stat.MP => base_mp,
                Stat.ATTACK => base_attack,
                Stat.DEFENSE => base_defense,
                Stat.MAGIC => base_magic,
                Stat.RESISTANCE => base_resistance,
                Stat.SPEED => base_speed,
                Stat.LUCK => base_luck,
                _ => 0
            };

            // Guard against zero/negative base stat
            if (baseStat <= 0) return 1;

            float growth = stat switch
            {
                Stat.HP => hp_growth,
                Stat.MP => mp_growth,
                Stat.ATTACK => attack_growth,
                Stat.DEFENSE => defense_growth,
                Stat.MAGIC => magic_growth,
                Stat.RESISTANCE => resistance_growth,
                Stat.SPEED => speed_growth,
                _ => 1.0f
            };

            // Clamp growth rate to prevent exponential overflow (0.5 to 1.5 range)
            growth = Mathf.Clamp(growth, 0.5f, 1.5f);

            // Calculate with overflow protection
            float growthMultiplier = Mathf.Pow(growth, level - 1);

            // Clamp multiplier to prevent overflow (max 1000x base stat)
            growthMultiplier = Mathf.Min(growthMultiplier, 1000f);

            float result = baseStat * growthMultiplier;

            // Clamp final result to valid int range with minimum of 1
            return Mathf.Clamp(Mathf.RoundToInt(result), 1, int.MaxValue / 2);
        }
    }

    [Serializable]
    public class DropEntry
    {
        public string item_id;
        public float chance;
        public int quantity;
    }

    [Serializable]
    public class SkillWeightEntry
    {
        public string skill_id;
        public int weight;
    }
}
