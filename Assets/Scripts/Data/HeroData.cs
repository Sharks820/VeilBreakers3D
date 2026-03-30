using System;
using System.Collections.Generic;
using UnityEngine;

namespace VeilBreakers.Data
{
    /// <summary>
    /// Hero/Champion data structure - loaded from JSON
    /// The 4 playable champions: Vex, Seraphina, Orion, Nyx
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
        public string quote;
        public string description;
        public string backstory;

        // =============================================================================
        // CLASS CONFIGURATION
        // =============================================================================

        public int primary_brand;
        public int primary_path;
        public string role;
        public string hero_class;
        public string resource_type;  // MANA, GUARD, or FURY - hero's secondary resource
        public string starter_monster_id;  // The monster that starts with this hero
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
        // BASE STATS (Game stats)
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
        // D&D STYLE BASE STATS (for character display)
        // =============================================================================

        public BaseStats base_stats;

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
        // Note: learnable_skills uses List<LearnableSkillEntry> for JSON serialization
        // Unity's JsonUtility cannot serialize Dictionary
        // =============================================================================

        public string[] innate_skills;
        public List<LearnableSkillEntry> learnable_skills_list;
        public string ultimate_skill;

        // =============================================================================
        // COMBAT INFO
        // =============================================================================

        public string combat_description;
        public string preferred_targets;

        // =============================================================================
        // VALIDATION
        // =============================================================================
        
        /// <summary>
        /// Validates hero data integrity
        /// </summary>
        public bool Validate()
        {
            if (string.IsNullOrEmpty(hero_id))
            {
                Debug.LogError("[HeroData] Hero ID is null or empty!"); // VB-IGNORE BUG-10 -- string literal, not pattern match on UnityEngine.Object
                return false;
            }
            
            if (string.IsNullOrEmpty(display_name))
            {
                Debug.LogWarning($"[HeroData] Hero {hero_id} has no display_name");
                display_name = hero_id; // Fallback
            }
            
            // Initialize base_stats if null
            if (base_stats == null)
            {
                Debug.LogWarning($"[HeroData] Hero {hero_id} has no base_stats, using defaults");
                base_stats = new BaseStats { strength = 10, dexterity = 10, constitution = 10, 
                                             intelligence = 10, wisdom = 10, charisma = 10 };
            }
            
            // Initialize innate_skills if null
            if (innate_skills == null)
            {
                innate_skills = Array.Empty<string>();
            }
            
            // Validate stat ranges
            if (base_hp <= 0) base_hp = 1;
            base_stats.strength = Mathf.Clamp(base_stats.strength, 1, 20);
            base_stats.dexterity = Mathf.Clamp(base_stats.dexterity, 1, 20);
            base_stats.constitution = Mathf.Clamp(base_stats.constitution, 1, 20);
            base_stats.intelligence = Mathf.Clamp(base_stats.intelligence, 1, 20);
            base_stats.wisdom = Mathf.Clamp(base_stats.wisdom, 1, 20);
            base_stats.charisma = Mathf.Clamp(base_stats.charisma, 1, 20);

            return true;
        }

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
            if (string.Equals(role, "tank", StringComparison.OrdinalIgnoreCase)) return HeroRole.TANK;
            if (string.Equals(role, "dps", StringComparison.OrdinalIgnoreCase)) return HeroRole.DPS;
            if (string.Equals(role, "support", StringComparison.OrdinalIgnoreCase)) return HeroRole.SUPPORT;
            if (string.Equals(role, "hybrid", StringComparison.OrdinalIgnoreCase)) return HeroRole.HYBRID;
            return HeroRole.HYBRID;
        }

        public ResourceType GetResourceType()
        {
            if (string.Equals(resource_type, "MANA", StringComparison.OrdinalIgnoreCase)) return ResourceType.MANA;
            if (string.Equals(resource_type, "GUARD", StringComparison.OrdinalIgnoreCase)) return ResourceType.GUARD;
            if (string.Equals(resource_type, "FURY", StringComparison.OrdinalIgnoreCase)) return ResourceType.FURY;
            if (string.Equals(resource_type, "CHAOS", StringComparison.OrdinalIgnoreCase)) return ResourceType.CHAOS;
            return ResourceType.MANA;
        }

        /// <summary>
        /// Calculate stat at given level
        /// </summary>
        public int GetStatAtLevel(Stat stat, int level)
        {
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
        
        /// <summary>
        /// Get skill ID for a specific level if exists
        /// </summary>
        public string GetLearnableSkillAtLevel(int level)
        {
            if (learnable_skills_list == null) return null;

            for (int i = 0; i < learnable_skills_list.Count; i++)
            {
                if (learnable_skills_list[i].level == level)
                {
                    return learnable_skills_list[i].skill_id;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// D&D-style ability scores for character display
    /// </summary>
    [Serializable]
    public class BaseStats
    {
        public int strength = 10;
        public int dexterity = 10;
        public int constitution = 10;
        public int intelligence = 10;
        public int wisdom = 10;
        public int charisma = 10;
    }
    
    /// <summary>
    /// Serializable entry for learnable skills
    /// Unity's JsonUtility cannot serialize Dictionary
    /// </summary>
    [Serializable]
    public class LearnableSkillEntry
    {
        public int level;
        public string skill_id;
    }
    
    /// <summary>
    /// Serializable color data for JSON
    /// </summary>
    [Serializable]
    public class ColorData
    {
        public float r;
        public float g;
        public float b;
        public float a = 1f;
        
        public Color ToColor()
        {
            return new Color(r, g, b, a);
        }
    }
}
