using System;
using System.Collections.Generic;
using UnityEngine;

namespace VeilBreakers.Data
{
    /// <summary>
    /// Skill data structure - loaded from JSON
    /// Represents all properties of a usable skill
    /// </summary>
    [Serializable]
    public class SkillData
    {
        // =============================================================================
        // IDENTITY
        // =============================================================================

        public string skill_id;
        public string display_name;
        public string description;
        public string icon_path;

        // =============================================================================
        // TYPE CONFIGURATION
        // =============================================================================

        public int skill_type;
        public int element;
        public int damage_type;
        public int target_type;
        public int brand_requirement;

        // =============================================================================
        // COSTS
        // =============================================================================

        public int mp_cost;
        public int hp_cost;
        public int cooldown_turns;

        // =============================================================================
        // DAMAGE CALCULATION
        // =============================================================================

        public int base_power;
        public int scaling_stat;
        public float scaling_ratio;
        public int hit_count;
        public float accuracy_modifier;
        public float crit_modifier;

        // =============================================================================
        // EFFECTS
        // =============================================================================

        public List<StatusEffectEntry> status_effects;
        public List<StatModifierEntry> stat_modifiers;
        public string[] special_effects;

        // =============================================================================
        // VISUALS
        // =============================================================================

        public string animation_id;
        public string sound_effect;
        public string particle_effect;
        public bool screen_shake;

        // =============================================================================
        // REQUIREMENTS
        // =============================================================================

        public int level_requirement;
        public float path_requirement;
        public string[] prerequisite_skills;

        // =============================================================================
        // FLAGS
        // =============================================================================

        public bool is_monster_skill;
        public bool is_hero_skill;

        // =============================================================================
        // HELPER METHODS
        // =============================================================================

        public SkillType GetSkillType()
        {
            return (SkillType)skill_type;
        }

        public DamageType GetDamageType()
        {
            return (DamageType)damage_type;
        }

        public TargetType GetTargetType()
        {
            return (TargetType)target_type;
        }

        public Brand GetBrandRequirement()
        {
            if (brand_requirement < 0) return Brand.NONE;
            return (Brand)brand_requirement;
        }

        public Stat GetScalingStat()
        {
            return (Stat)scaling_stat;
        }

        /// <summary>
        /// Calculate damage at given stat value
        /// </summary>
        public int CalculateDamage(int statValue)
        {
            return Mathf.RoundToInt(base_power + (statValue * scaling_ratio));
        }

        /// <summary>
        /// Check if skill targets enemies
        /// </summary>
        public bool IsOffensive()
        {
            var tt = GetTargetType();
            return tt == TargetType.SINGLE_ENEMY || tt == TargetType.ALL_ENEMIES;
        }

        /// <summary>
        /// Check if skill targets allies
        /// </summary>
        public bool IsSupportive()
        {
            var tt = GetTargetType();
            return tt == TargetType.SELF || tt == TargetType.SINGLE_ALLY || tt == TargetType.ALL_ALLIES;
        }
    }

    [Serializable]
    public class StatusEffectEntry
    {
        public int effect;
        public float chance;
        public int duration;

        public StatusEffect GetStatusEffect()
        {
            return (StatusEffect)effect;
        }
    }

    [Serializable]
    public class StatModifierEntry
    {
        public int stat;
        public int amount;
        public int duration;

        public Stat GetStat()
        {
            return (Stat)stat;
        }
    }
}
