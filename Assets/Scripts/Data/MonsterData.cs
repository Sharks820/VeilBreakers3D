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
        public Dictionary<string, string> learnable_skills;

        // =============================================================================
        // AI CONFIGURATION
        // =============================================================================

        public string ai_pattern;
        public Dictionary<string, int> skill_weights;

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
            return (Brand)brand;
        }

        public Brand GetSecondaryBrand()
        {
            return (Brand)secondary_brand;
        }

        public BrandTier GetBrandTier()
        {
            return (BrandTier)brand_tier;
        }

        public Rarity GetRarity()
        {
            return (Rarity)rarity;
        }

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

            return Mathf.RoundToInt(baseStat * Mathf.Pow(growth, level - 1));
        }
    }

    [Serializable]
    public class ColorData
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public Color ToColor()
        {
            return new Color(r, g, b, a);
        }
    }

    [Serializable]
    public class DropEntry
    {
        public string item_id;
        public float chance;
        public int quantity;
    }
}
