using UnityEngine;

namespace VeilBreakers.Data
{
    /// <summary>
    /// ScriptableObject defining a status effect's base data.
    /// Each effect has a Brand (for resistance), Category (for behavior), and Type.
    /// </summary>
    [CreateAssetMenu(fileName = "StatusEffect", menuName = "VeilBreakers/Status Effect Data")]
    public class StatusEffectData : ScriptableObject
    {
        // =============================================================================
        // IDENTIFICATION
        // =============================================================================

        [Header("Identification")]
        [Tooltip("Unique identifier for this effect")]
        public string effectId;

        [Tooltip("Display name shown to player")]
        public string displayName;

        [Tooltip("Description of what this effect does")]
        [TextArea(2, 4)]
        public string description;

        // =============================================================================
        // CLASSIFICATION
        // =============================================================================

        [Header("Classification")]
        [Tooltip("The specific effect type")]
        public StatusEffectType effectType;

        [Tooltip("Category determines behavior rules (Damage, Control, Buff, Debuff)")]
        public EffectCategory category;

        [Tooltip("Source brand - used for resistance calculation")]
        public Brand sourceBrand;

        // =============================================================================
        // TIMING
        // =============================================================================

        [Header("Timing")]
        [Tooltip("Duration tier (Short/Medium/Long/Extended)")]
        public DurationTier durationTier;

        [Tooltip("Base duration in seconds")]
        [Range(1f, 120f)]
        public float baseDuration = 8f;

        [Tooltip("Tick interval for DoT/HoT effects (0 = no ticking)")]
        [Range(0f, 5f)]
        public float tickInterval = 1f;

        // =============================================================================
        // POTENCY
        // =============================================================================

        [Header("Potency")]
        [Tooltip("Base value (damage per tick, stat modifier %, etc.)")]
        public float baseValue = 10f;

        [Tooltip("Which stats affect this effect's potency (caster's stats)")]
        public Stat[] scalingStats;

        [Tooltip("Scaling coefficient per stat point (0.01 = 1% per point)")]
        [Range(0f, 0.1f)]
        public float statScaling = 0.01f;

        // =============================================================================
        // STACKING RULES
        // =============================================================================

        [Header("Stacking")]
        [Tooltip("Can this effect stack with itself? (Default NO per design)")]
        public bool canStack = false;

        [Tooltip("Maximum stacks if stacking is enabled")]
        [Range(1, 10)]
        public int maxStacks = 1;

        [Tooltip("If true, reapplying refreshes duration instead of blocking")]
        public bool refreshOnReapply = true;

        // =============================================================================
        // SPECIAL BEHAVIORS
        // =============================================================================

        [Header("Special Behaviors")]
        [Tooltip("Effect breaks when target takes damage (like Sleep)")]
        public bool breaksOnDamage = false;

        [Tooltip("Effect grants damage resistance while active (like Petrify)")]
        [Range(0f, 1f)]
        public float damageResistance = 0f;

        [Tooltip("Effect consumes on trigger (like Empower, Undying)")]
        public bool consumeOnTrigger = false;

        [Tooltip("Target stat for stat-modifying effects")]
        public Stat targetStat;

        [Tooltip("Is this a percentage modifier (true) or flat value (false)?")]
        public bool isPercentage = true;

        // =============================================================================
        // VISUALS
        // =============================================================================

        [Header("Visuals")]
        [Tooltip("Icon shown in UI")]
        public Sprite icon;

        [Tooltip("Color tint for UI elements")]
        public Color uiColor = Color.white;

        [Tooltip("Particle effect prefab to play on target")]
        public GameObject particleEffect;

        [Tooltip("Animation trigger to set on target")]
        public string animationTrigger;

        [Tooltip("Sound effect to play on application")]
        public AudioClip applySound;

        [Tooltip("Sound effect to play each tick")]
        public AudioClip tickSound;

        [Tooltip("Sound effect to play on removal")]
        public AudioClip removeSound;

        // =============================================================================
        // CLEANSE PRIORITY
        // =============================================================================

        [Header("Cleanse Priority")]
        [Tooltip("Priority for AI cleanse targeting (higher = cleansed first)")]
        [Range(0, 100)]
        public int cleansePriority = 50;

        // =============================================================================
        // METHODS
        // =============================================================================

        /// <summary>
        /// Gets the typical duration for this tier (deterministic midpoint).
        /// Use GetRandomizedDuration() for actual application with variance.
        /// </summary>
        public float GetTierDuration()
        {
            return durationTier switch
            {
                DurationTier.SHORT => 4f,       // Midpoint of 3-5
                DurationTier.MEDIUM => 10f,     // Midpoint of 8-12
                DurationTier.LONG => 17.5f,     // Midpoint of 15-20
                DurationTier.EXTENDED => 37.5f, // Midpoint of 30-45
                _ => baseDuration
            };
        }

        /// <summary>
        /// Gets a randomized duration within the tier's range.
        /// Call this once when applying the effect.
        /// </summary>
        public float GetRandomizedDuration()
        {
            return durationTier switch
            {
                DurationTier.SHORT => Random.Range(3f, 5f),
                DurationTier.MEDIUM => Random.Range(8f, 12f),
                DurationTier.LONG => Random.Range(15f, 20f),
                DurationTier.EXTENDED => Random.Range(30f, 45f),
                _ => baseDuration
            };
        }

        /// <summary>
        /// Calculates final potency based on caster stats and skill rank.
        /// Formula: Base * (1 + StatMod) * SkillRank * BrandEffectiveness
        /// </summary>
        public float CalculatePotency(float statModifier, float skillRank, float brandEffectiveness)
        {
            return baseValue * (1f + statModifier) * skillRank * brandEffectiveness;
        }

        /// <summary>
        /// Calculates final duration based on caster's potency stat.
        /// Formula: BaseDuration * (1 + PotencyStat * 0.1)
        /// </summary>
        public float CalculateDuration(float potencyStat)
        {
            return baseDuration * (1f + potencyStat * 0.1f);
        }

        /// <summary>
        /// Returns true if this is a harmful effect (debuff or damage).
        /// </summary>
        public bool IsHarmful => category == EffectCategory.DAMAGE ||
                                  category == EffectCategory.CONTROL ||
                                  category == EffectCategory.DEBUFF;

        /// <summary>
        /// Returns true if this is a beneficial effect (buff).
        /// </summary>
        public bool IsBeneficial => category == EffectCategory.BUFF;

        /// <summary>
        /// Returns true if this effect ticks over time.
        /// </summary>
        public bool IsTicking => tickInterval > 0f;

        /// <summary>
        /// Returns true if this is a control effect (CC).
        /// </summary>
        public bool IsControlEffect => category == EffectCategory.CONTROL;

        /// <summary>
        /// Returns true if this effect prevents the target from acting.
        /// </summary>
        public bool PreventsAction => effectType == StatusEffectType.STUN ||
                                       effectType == StatusEffectType.SLEEP ||
                                       effectType == StatusEffectType.PETRIFY ||
                                       effectType == StatusEffectType.CHARM ||
                                       effectType == StatusEffectType.FEAR;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-generate ID from name if empty
            if (string.IsNullOrEmpty(effectId) && !string.IsNullOrEmpty(displayName))
            {
                effectId = displayName.ToLower().Replace(" ", "_");
            }

            // Auto-set category based on effect type
            if (effectType != StatusEffectType.NONE)
            {
                int typeValue = (int)effectType;
                if (typeValue >= 1 && typeValue < 10)
                    category = EffectCategory.DAMAGE;
                else if (typeValue >= 10 && typeValue < 30)
                    category = EffectCategory.CONTROL;
                else if (typeValue >= 30 && typeValue < 80)
                    category = EffectCategory.BUFF;
                else if (typeValue >= 80)
                    category = EffectCategory.DEBUFF;
            }

            // Set default cleanse priority based on category
            if (cleansePriority == 50)
            {
                cleansePriority = category switch
                {
                    EffectCategory.DAMAGE => 70, // DoTs medium-high
                    EffectCategory.CONTROL => 80, // CC high
                    EffectCategory.DEBUFF => 60,  // Debuffs medium
                    _ => 50
                };

                // Death sentences get highest priority
                if (effectType == StatusEffectType.DOOM ||
                    effectType == StatusEffectType.MARKED_DEATH ||
                    effectType == StatusEffectType.CONDEMNED)
                {
                    cleansePriority = 100;
                }
            }
        }
#endif
    }
}
