using System;
using UnityEngine;
using VeilBreakers.Data;

namespace VeilBreakers.AI
{
    /// <summary>
    /// ScriptableObject defining AI personality and behavior weights.
    /// Each brand has a unique personality that shapes decision-making.
    /// </summary>
    [CreateAssetMenu(fileName = "AIPersonality", menuName = "VeilBreakers/AI Personality")]
    public class AIPersonality : ScriptableObject
    {
        // =============================================================================
        // IDENTIFICATION
        // =============================================================================

        [Header("Identification")]
        [Tooltip("Brand this personality is designed for")]
        public Brand targetBrand;

        [Tooltip("Display name for this personality")]
        public string personalityName;

        [Tooltip("Description of this AI's behavior")]
        [TextArea(2, 4)]
        public string description;

        // =============================================================================
        // CATEGORY WEIGHTS (Must sum to 100)
        // =============================================================================

        [Header("Category Weights (Must Sum to 100)")]
        [Tooltip("Weight for damage-dealing actions")]
        [Range(0, 100)]
        public int damageWeight = 25;

        [Tooltip("Weight for survival/defensive actions")]
        [Range(0, 100)]
        public int survivalWeight = 25;

        [Tooltip("Weight for team support actions")]
        [Range(0, 100)]
        public int teamValueWeight = 25;

        [Tooltip("Weight for positioning/utility actions")]
        [Range(0, 100)]
        public int positioningWeight = 15;

        [Tooltip("Weight for control/CC actions")]
        [Range(0, 100)]
        public int controlWeight = 10;

        // =============================================================================
        // TARGET SELECTION MULTIPLIERS
        // =============================================================================

        [Header("Target Selection - Enemy HP")]
        [Tooltip("Multiplier for targets below 25% HP (EXECUTE)")]
        [Range(1f, 4f)]
        public float lowHpTargetMultiplier = 2.5f;

        [Tooltip("Multiplier for targets at 25-50% HP (Weakened)")]
        [Range(1f, 2f)]
        public float midHpTargetMultiplier = 1.3f;

        [Tooltip("Multiplier for targets above 75% HP (Fresh)")]
        [Range(0.5f, 1f)]
        public float highHpTargetMultiplier = 0.9f;

        [Header("Target Selection - Special")]
        [Tooltip("Multiplier for debuffed targets (focus fire)")]
        [Range(1f, 3f)]
        public float debuffedTargetMultiplier = 1.5f;

        [Tooltip("Multiplier for armor-shredded targets")]
        [Range(1f, 4f)]
        public float armorShredTargetMultiplier = 2.0f;

        [Tooltip("Multiplier for healer targets")]
        [Range(1f, 3f)]
        public float healerTargetMultiplier = 1.5f;

        [Tooltip("Multiplier for tank targets (usually low)")]
        [Range(0.1f, 1f)]
        public float tankTargetMultiplier = 0.4f;

        [Tooltip("Multiplier for tanks WITH armor shred")]
        [Range(0.8f, 2f)]
        public float shredTankMultiplier = 1.2f;

        [Tooltip("Multiplier for casting targets (interrupt opportunity)")]
        [Range(1f, 3f)]
        public float castingTargetMultiplier = 1.8f;

        // =============================================================================
        // SELF/ALLY MULTIPLIERS
        // =============================================================================

        [Header("Self Condition Multipliers")]
        [Tooltip("Multiplier when self HP < 20% (desperation)")]
        [Range(1f, 5f)]
        public float selfCriticalMultiplier = 3.0f;

        [Tooltip("Multiplier when self HP < 50% (caution)")]
        [Range(1f, 3f)]
        public float selfLowHpMultiplier = 1.5f;

        [Tooltip("Multiplier for damage actions when self low HP")]
        [Range(0.3f, 1f)]
        public float lowHpDamageModifier = 0.7f;

        [Header("Ally Condition Multipliers")]
        [Tooltip("Multiplier when ally HP < 25% (emergency help)")]
        [Range(1f, 5f)]
        public float allyCriticalMultiplier = 2.5f;

        [Tooltip("Multiplier when ally HP < 50% (help needed)")]
        [Range(1f, 3f)]
        public float allyLowHpMultiplier = 1.5f;

        // =============================================================================
        // SPECIAL BEHAVIOR FLAGS
        // =============================================================================

        [Header("Special Behaviors")]
        [Tooltip("Can this AI auto-defend? (Only IRON, LEECH, MEND)")]
        public bool canAutoDefend = false;

        [Tooltip("Minimum HP% to trigger auto-defend")]
        [Range(5, 30)]
        public int autoDefendThreshold = 15;

        [Tooltip("Should this AI kite (stay at range)?")]
        public bool prefersRange = false;

        [Tooltip("Should this AI prefer AOE over single target?")]
        public bool prefersAOE = false;

        [Tooltip("Does this AI get stronger when team is losing?")]
        public bool desperationBonus = false;

        [Tooltip("Does this AI track and chain kills for momentum?")]
        public bool tracksMomentum = false;

        [Tooltip("Does this AI prioritize drain abilities for sustain?")]
        public bool prioritizesDrain = false;

        // =============================================================================
        // THRESHOLDS
        // =============================================================================

        [Header("Thresholds")]
        [Tooltip("HP% to consider 'low' for survival checks")]
        [Range(20, 70)]
        public int lowHpThreshold = 50;

        [Tooltip("HP% to consider 'critical' for emergency actions")]
        [Range(10, 40)]
        public int criticalHpThreshold = 25;

        [Tooltip("MP% to start conserving resources")]
        [Range(10, 40)]
        public int manaConserveThreshold = 20;

        [Tooltip("Enemy HP% to trigger execute priority")]
        [Range(15, 35)]
        public int executeThreshold = 25;

        // =============================================================================
        // ULTIMATE TARGETING
        // =============================================================================

        [Header("Ultimate Auto-Targeting")]
        [Tooltip("How to auto-select ultimate target if player doesn't override")]
        public UltimateTargetMode ultimateTargetMode = UltimateTargetMode.LOWEST_HP_ENEMY;

        public enum UltimateTargetMode
        {
            LOWEST_HP_ENEMY,        // SAVAGE, DREAD
            HIGHEST_HP_ENEMY,       // LEECH
            ENEMY_CLUSTER,          // SURGE, RUIN
            ENEMY_HEALER,           // VENOM
            MOST_ENEMIES_ON_ALLY,   // IRON
            LOWEST_HP_ALLY,         // GRACE
            MOST_DAMAGE_INCOMING,   // MEND
            STRONGEST_OR_ALLY       // VOID
        }

        // =============================================================================
        // METHODS
        // =============================================================================

        /// <summary>
        /// Get the weight multiplier for a category.
        /// </summary>
        public float GetCategoryWeight(ActionCategory category)
        {
            return category switch
            {
                ActionCategory.DAMAGE => damageWeight / 100f,
                ActionCategory.SURVIVAL => survivalWeight / 100f,
                ActionCategory.TEAM_VALUE => teamValueWeight / 100f,
                ActionCategory.POSITIONING => positioningWeight / 100f,
                ActionCategory.CONTROL => controlWeight / 100f,
                _ => 0.25f
            };
        }

        /// <summary>
        /// Calculate target selection multiplier based on target state.
        /// </summary>
        public float GetTargetMultiplier(float targetHpPercent, bool isDebuffed, bool isArmorShred,
            bool isHealer, bool isTank, bool isCasting)
        {
            float multiplier = 1f;

            // HP-based multiplier
            if (targetHpPercent < executeThreshold)
                multiplier *= lowHpTargetMultiplier;
            else if (targetHpPercent < lowHpThreshold)
                multiplier *= midHpTargetMultiplier;
            else if (targetHpPercent > 75f)
                multiplier *= highHpTargetMultiplier;

            // Status multipliers (additive then multiply)
            if (isArmorShred)
                multiplier *= armorShredTargetMultiplier;
            else if (isDebuffed)
                multiplier *= debuffedTargetMultiplier;

            // Role multipliers
            if (isTank && !isArmorShred)
                multiplier *= tankTargetMultiplier;
            else if (isTank && isArmorShred)
                multiplier *= shredTankMultiplier;
            else if (isHealer)
                multiplier *= healerTargetMultiplier;

            // Casting multiplier
            if (isCasting)
                multiplier *= castingTargetMultiplier;

            return multiplier;
        }

        /// <summary>
        /// Calculate self-condition multiplier for survival actions.
        /// </summary>
        public float GetSelfSurvivalMultiplier(float selfHpPercent)
        {
            if (selfHpPercent < criticalHpThreshold)
                return selfCriticalMultiplier;
            if (selfHpPercent < lowHpThreshold)
                return selfLowHpMultiplier;
            return 1f;
        }

        /// <summary>
        /// Calculate multiplier for damage actions based on self HP.
        /// </summary>
        public float GetSelfDamageMultiplier(float selfHpPercent)
        {
            if (selfHpPercent < criticalHpThreshold)
                return lowHpDamageModifier;
            return 1f;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Warn if weights don't sum to 100
            int total = damageWeight + survivalWeight + teamValueWeight + positioningWeight + controlWeight;
            if (total != 100)
            {
                Debug.LogWarning($"[AIPersonality] {personalityName}: Category weights sum to {total}, should be 100");
            }

            // Auto-set personality name from brand if empty
            if (string.IsNullOrEmpty(personalityName) && targetBrand != Brand.NONE)
            {
                personalityName = $"{targetBrand} Personality";
            }
        }
#endif

        // =============================================================================
        // PRESET FACTORY
        // =============================================================================

        // Cache for runtime-created personalities to prevent memory leaks
        private static readonly System.Collections.Generic.Dictionary<Brand, AIPersonality> _cachedDefaults =
            new System.Collections.Generic.Dictionary<Brand, AIPersonality>();

        /// <summary>
        /// Creates default personality settings for a brand.
        /// Caches instances to prevent memory leaks from runtime ScriptableObject creation.
        /// </summary>
        public static AIPersonality CreateDefault(Brand brand)
        {
            // Return cached instance if available
            if (_cachedDefaults.TryGetValue(brand, out var cached))
                return cached;

            var personality = CreateInstance<AIPersonality>();
            personality.targetBrand = brand;
            personality.personalityName = $"{brand} Personality";

            switch (brand)
            {
                case Brand.IRON:
                    personality.damageWeight = 10;
                    personality.survivalWeight = 40;
                    personality.teamValueWeight = 35;
                    personality.positioningWeight = 15;
                    personality.controlWeight = 0;
                    personality.canAutoDefend = true;
                    personality.description = "Defensive tank focused on protecting allies and absorbing damage.";
                    personality.ultimateTargetMode = UltimateTargetMode.MOST_ENEMIES_ON_ALLY;
                    break;

                case Brand.SAVAGE:
                    personality.damageWeight = 50;
                    personality.survivalWeight = 15;
                    personality.teamValueWeight = 10;
                    personality.positioningWeight = 0;
                    personality.controlWeight = 25;
                    personality.lowHpTargetMultiplier = 3.0f;
                    personality.tracksMomentum = true;
                    personality.description = "Melee burst DPS focused on executing low HP targets.";
                    personality.ultimateTargetMode = UltimateTargetMode.LOWEST_HP_ENEMY;
                    break;

                case Brand.SURGE:
                    personality.damageWeight = 40;
                    personality.survivalWeight = 20;
                    personality.teamValueWeight = 10;
                    personality.positioningWeight = 30;
                    personality.controlWeight = 0;
                    personality.prefersRange = true;
                    personality.prefersAOE = true;
                    personality.description = "Ranged artillery focused on safe damage and kiting.";
                    personality.ultimateTargetMode = UltimateTargetMode.ENEMY_CLUSTER;
                    break;

                case Brand.VENOM:
                    personality.damageWeight = 20;
                    personality.survivalWeight = 10;
                    personality.teamValueWeight = 25;
                    personality.positioningWeight = 0;
                    personality.controlWeight = 45;
                    personality.healerTargetMultiplier = 3.0f;
                    personality.description = "DoT/Debuff specialist focused on heal reduction and spread damage.";
                    personality.ultimateTargetMode = UltimateTargetMode.ENEMY_HEALER;
                    break;

                case Brand.DREAD:
                    personality.damageWeight = 10;
                    personality.survivalWeight = 15;
                    personality.teamValueWeight = 30;
                    personality.positioningWeight = 0;
                    personality.controlWeight = 45;
                    personality.castingTargetMultiplier = 3.0f;
                    personality.description = "CC controller focused on interrupts and lockdown.";
                    personality.ultimateTargetMode = UltimateTargetMode.LOWEST_HP_ENEMY;
                    break;

                case Brand.LEECH:
                    personality.damageWeight = 15;
                    personality.survivalWeight = 45;
                    personality.teamValueWeight = 10;
                    personality.positioningWeight = 0;
                    personality.controlWeight = 30;
                    personality.canAutoDefend = true;
                    personality.prioritizesDrain = true;
                    personality.tankTargetMultiplier = 0.8f; // Doesn't avoid tanks as much
                    personality.description = "Drain tank focused on sustain through lifesteal.";
                    personality.ultimateTargetMode = UltimateTargetMode.HIGHEST_HP_ENEMY;
                    break;

                case Brand.GRACE:
                    personality.damageWeight = 5;
                    personality.survivalWeight = 25;
                    personality.teamValueWeight = 55;
                    personality.positioningWeight = 15;
                    personality.controlWeight = 0;
                    personality.allyCriticalMultiplier = 4.0f;
                    personality.description = "Battle healer focused on reactive healing and cleanse.";
                    personality.ultimateTargetMode = UltimateTargetMode.LOWEST_HP_ALLY;
                    break;

                case Brand.MEND:
                    personality.damageWeight = 10;
                    personality.survivalWeight = 15;
                    personality.teamValueWeight = 50;
                    personality.positioningWeight = 25;
                    personality.controlWeight = 0;
                    personality.canAutoDefend = true;
                    personality.description = "Shield support focused on proactive damage prevention.";
                    personality.ultimateTargetMode = UltimateTargetMode.MOST_DAMAGE_INCOMING;
                    break;

                case Brand.RUIN:
                    personality.damageWeight = 45;
                    personality.survivalWeight = 20;
                    personality.teamValueWeight = 25;
                    personality.positioningWeight = 10;
                    personality.controlWeight = 0;
                    personality.prefersAOE = true;
                    personality.description = "AOE devastator focused on cluster damage.";
                    personality.ultimateTargetMode = UltimateTargetMode.ENEMY_CLUSTER;
                    break;

                case Brand.VOID:
                    personality.damageWeight = 20;
                    personality.survivalWeight = 10;
                    personality.teamValueWeight = 30;
                    personality.positioningWeight = 0;
                    personality.controlWeight = 40;
                    personality.desperationBonus = true;
                    personality.description = "Chaos mage that gets stronger when team is losing.";
                    personality.ultimateTargetMode = UltimateTargetMode.STRONGEST_OR_ALLY;
                    break;

                default:
                    personality.damageWeight = 25;
                    personality.survivalWeight = 25;
                    personality.teamValueWeight = 25;
                    personality.positioningWeight = 15;
                    personality.controlWeight = 10;
                    personality.description = "Balanced personality with no specialization.";
                    break;
            }

            // Cache the personality to prevent memory leaks
            _cachedDefaults[brand] = personality;
            return personality;
        }
    }

    /// <summary>
    /// Action categories for weight calculation.
    /// </summary>
    public enum ActionCategory
    {
        DAMAGE,
        SURVIVAL,
        TEAM_VALUE,
        POSITIONING,
        CONTROL
    }
}
