using System;
using System.Collections.Generic;
using UnityEngine;

namespace VeilBreakers.Data
{
    /// <summary>
    /// Hero/Champion data structure - loaded from JSON
    /// The 4 playable champions: Bastion, Rend, Marrow, Mirage
    /// </summary>
    [Serializable]
    public class HeroData
    {
        // =============================================================================
        // IDENTITY
        // =============================================================================

        public string hero_id;
        public string display_name;
        public string title;
        public string description;
        public string backstory;

        // =============================================================================
        // CLASS CONFIGURATION
        // =============================================================================

        public int primary_brand;
        public int primary_path;
        public string role;
        public string hero_class;
        public string[] recommended_monsters;
        public string synergy_explanation;

        // =============================================================================
        // VISUALS
        // =============================================================================

        public string sprite_path;
        public string portrait_path;
        public string battle_sprite_path;
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
        // GROWTH RATES
        // =============================================================================

        public float hp_growth;
        public float mp_growth;
        public float attack_growth;
        public float defense_growth;
        public float magic_growth;
        public float resistance_growth;
        public float speed_growth;
        public float luck_growth;

        // =============================================================================
        // SKILLS
        // =============================================================================

        public string[] innate_skills;
        public Dictionary<string, string> learnable_skills;
        public string ultimate_skill;

        // =============================================================================
        // COMBAT INFO
        // =============================================================================

        public string combat_description;
        public string preferred_targets;

        // =============================================================================
        // HELPER METHODS
        // =============================================================================

        public Brand GetPrimaryBrand()
        {
            return (Brand)primary_brand;
        }

        public Path GetPrimaryPath()
        {
            return (Path)primary_path;
        }

        public HeroRole GetRole()
        {
            return role?.ToLower() switch
            {
                "tank" => HeroRole.TANK,
                "dps" => HeroRole.DPS,
                "support" => HeroRole.SUPPORT,
                "hybrid" => HeroRole.HYBRID,
                _ => HeroRole.HYBRID
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
                Stat.LUCK => luck_growth,
                _ => 1.0f
            };

            // Heroes use additive growth per level
            return baseStat + Mathf.RoundToInt(baseStat * growth * (level - 1));
        }
    }
}
